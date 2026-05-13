// <copyright file="LoginTicketService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Services;

using ElRoso.ARCA.Domains.Responses;
using ElRoso.ARCA.Domains.Services;
using ElRoso.ARCA.Exceptions;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Xml;

internal sealed class LoginTicketService : ILoginTicketService
{
    // Thread-safe counter for WSAA request unique IDs.
    // Contador thread-safe para los IDs únicos de request al WSAA.
    private static int uniqueIdCounter;

    // NTP response cache — avoids a UDP round-trip on every token refresh.
    // WSAA accepts timestamps within ±10 minutes of server time.
    // Caché de respuesta NTP — evita un round-trip UDP en cada renovación de token.
    // WSAA acepta timestamps dentro de ±10 minutos de la hora del servidor.
    private static DateTime s_cachedNtpTime;
    private static long s_cacheTimestampMs;
    private static readonly object s_ntpLock = new();
    private const long NtpCacheMs = 60_000;

    private readonly ILogger<LoginTicketService> logger;
    private readonly ICertificateService certificateService;

    private const string XmlTemplate =
        "<loginTicketRequest>" +
          "<header>" +
            "<uniqueId></uniqueId>" +
            "<generationTime></generationTime>" +
            "<expirationTime></expirationTime>" +
          "</header>" +
          "<service></service>" +
        "</loginTicketRequest>";

    public LoginTicketService(ILogger<LoginTicketService> logger, ICertificateService certificateService)
    {
        this.logger = logger;
        this.certificateService = certificateService;
    }

    public async Task<LoginTicketResponse> GetLoginTicketAsync(
        string service,
        string wsaaUrl,
        string certificatePath,
        string? certificatePassword,
        CancellationToken ct = default)
    {
        // PASO 1: Build the LoginTicketRequest XML.
        // PASO 1: Construir el XML del LoginTicketRequest.
        string signedBase64;
        try
        {
            var ARCATime = (await GetNetworkTimeAsync(ct)).AddHours(-3);
            var uniqueId = (uint)Interlocked.Increment(ref uniqueIdCounter);

            var doc = new XmlDocument();
            doc.LoadXml(XmlTemplate);

            doc.SelectSingleNode("//uniqueId")!.InnerText = uniqueId.ToString();
            doc.SelectSingleNode("//generationTime")!.InnerText = ARCATime.AddMinutes(-2).ToString("s");
            doc.SelectSingleNode("//expirationTime")!.InnerText = ARCATime.AddMinutes(+10).ToString("s");
            doc.SelectSingleNode("//service")!.InnerText = service;

            logger.LogInformation("LoginTicketRequest built for service '{Service}', uniqueId={UniqueId}.", service, uniqueId);

            // PASO 2: Sign with PKCS#7 using the X.509 certificate.
            // PASO 2: Firmar con PKCS#7 usando el certificado X.509.
            var cert = certificateService.LoadCertificate(certificatePath, certificatePassword);
            var msgBytes = Encoding.UTF8.GetBytes(doc.OuterXml);
            var signed = certificateService.SignMessage(msgBytes, cert);
            signedBase64 = Convert.ToBase64String(signed);
        }
        catch (ARCAAuthException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ARCAAuthException("Failed to build or sign the LoginTicketRequest.", ex);
        }

        // PASO 3: Call WSAA to get the Login Ticket.
        // PASO 3: Llamar al WSAA para obtener el Login Ticket.
        string ticketXml;
        try
        {
            logger.LogInformation("Calling WSAA at {Url}.", wsaaUrl);
            var client = new WSAA.LoginCMSClient(WSAA.LoginCMSClient.EndpointConfiguration.LoginCms, wsaaUrl);
            var response = await client.loginCmsAsync(new WSAA.loginCmsRequest
            {
                Body = new WSAA.loginCmsRequestBody { in0 = signedBase64 },
            });
            ticketXml = response.Body.loginCmsReturn;
            logger.LogInformation("WSAA responded successfully.");
        }
        catch (ARCAAuthException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ARCAAuthException("WSAA call failed.", ex);
        }

        // PASO 4: Parse the Login Ticket Response XML.
        // PASO 4: Parsear el XML de respuesta del WSAA.
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(ticketXml);

            return new LoginTicketResponse
            {
                Token = doc.SelectSingleNode("//token")!.InnerText,
                Sign = doc.SelectSingleNode("//sign")!.InnerText,

                // InvariantCulture ensures consistent parsing regardless of server locale.
                // InvariantCulture asegura parseo consistente sin importar el locale del servidor.
                ExpirationTime = DateTime.Parse(
                    doc.SelectSingleNode("//expirationTime")!.InnerText,
                    CultureInfo.InvariantCulture),
            };
        }
        catch (Exception ex)
        {
            throw new ARCAAuthException("Failed to parse the WSAA LoginTicketResponse XML.", ex);
        }
    }

    /// <summary>
    /// Fetches current time from ARCA's NTP server, cached for 60 s.
    /// Falls back to system clock if NTP is unreachable — WSAA tolerates ±10 min.
    /// Obtiene la hora del servidor NTP de ARCA, cacheada por 60 s.
    /// Usa el reloj del sistema como fallback — WSAA tolera ±10 min de desvío.
    /// </summary>
    private static async Task<DateTime> GetNetworkTimeAsync(CancellationToken ct)
    {
        long nowMs = Environment.TickCount64;
        lock (s_ntpLock)
        {
            if (s_cachedNtpTime != default && nowMs - s_cacheTimestampMs < NtpCacheMs)
                return s_cachedNtpTime;
        }

        const string ntpServer = "time.afip.gov.ar";
        const int ntpPort = 123;
        const int ntpDataLength = 48;

        DateTime result;
        try
        {
            var ntpData = new byte[ntpDataLength];
            ntpData[0] = 0x1B; // NTP LI=0, VN=3, Mode=3 (client)

            // Hard 5-second timeout so a slow/unresponsive NTP server doesn't block auth.
            // Timeout duro de 5 segundos para que un NTP lento/caído no bloquee la autenticación.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var udp = new UdpClient();
            await udp.SendAsync(ntpData, ntpDataLength, ntpServer, ntpPort).WaitAsync(cts.Token);
            var received = await udp.ReceiveAsync(cts.Token);
            var data = received.Buffer;

            if (data.Length < ntpDataLength)
                throw new InvalidOperationException($"NTP response too short: {data.Length} bytes.");

            ulong intPart = (ulong)data[40] << 24 | (ulong)data[41] << 16 | (ulong)data[42] << 8 | data[43];
            ulong fracPart = (ulong)data[44] << 24 | (ulong)data[45] << 16 | (ulong)data[46] << 8 | data[47];
            var ms = intPart * 1000 + fracPart * 1000 / 0x100000000L;
            result = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)ms);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // NTP timeout — fall back to system clock.
            // Timeout de NTP — se usa el reloj del sistema.
            result = DateTime.UtcNow;
        }
        catch
        {
            // Any NTP failure — fall back to system clock.
            // Cualquier falla de NTP — se usa el reloj del sistema.
            result = DateTime.UtcNow;
        }

        lock (s_ntpLock)
        {
            s_cachedNtpTime = result;
            s_cacheTimestampMs = Environment.TickCount64;
        }

        return result;
    }
}
