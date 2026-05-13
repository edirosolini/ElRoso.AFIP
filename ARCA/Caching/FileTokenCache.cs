// <copyright file="FileTokenCache.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Caching;

using ElRoso.ARCA.Domains.Responses;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// File-based token cache. One encrypted binary file per service+company combination.
/// Uses per-key SemaphoreSlim to prevent concurrent read/write corruption.
/// Caché de tokens basada en archivos cifrados. Un archivo por combinación servicio+empresa.
/// </summary>
internal sealed class FileTokenCache : ITokenCache
{
    // Buffer before considering a token expired (avoids clock edge cases).
    // Margen antes de considerar expirado el token (evita casos borde de reloj).
    private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMinutes(2);

    private readonly string cacheDirectory;
    private readonly ILogger<FileTokenCache> logger;

    // Per-key semaphores — ConcurrentDictionary avoids a global lock on registration.
    // Semáforos por clave — ConcurrentDictionary evita un lock global al registrar.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();

    public FileTokenCache(string cacheDirectory, ILogger<FileTokenCache> logger)
    {
        this.cacheDirectory = cacheDirectory;
        this.logger = logger;
        Directory.CreateDirectory(cacheDirectory);
    }

    public async Task<LoginTicketResponse?> GetAsync(string service, long companyId, CancellationToken ct = default)
    {
        var key = BuildKey(service, companyId);
        var path = BuildPath(key);
        var sem = GetSemaphore(key);

        await sem.WaitAsync(ct);
        try
        {
            LoginTicketResponse? ticket;
            try
            {
                // Async read — avoids blocking a thread-pool thread with sync I/O.
                // Lectura asíncrona — evita bloquear un hilo del pool con I/O síncrona.
                var data = await File.ReadAllBytesAsync(path, ct);
                ticket = DeserializeToken(data);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (CryptographicException)
            {
                // Cache file written before encryption was introduced — treat as miss.
                // Archivo escrito antes de que se agregara cifrado — se trata como miss.
                logger.LogWarning("Token cache file appears unencrypted for {Service}/{CompanyId}. Discarding.", service, companyId);
                return null;
            }

            if (ticket == null)
                return null;

            // ExpirationTime comes from WSAA in Argentina local time (UTC-3, no DST since 2009).
            // ExpirationTime viene del WSAA en hora local argentina (UTC-3, sin horario de verano desde 2009).
            var nowAr = DateTime.UtcNow.AddHours(-3);
            if (ticket.ExpirationTime.Subtract(ExpirationBuffer) < nowAr)
            {
                logger.LogInformation("Token cache expired for {Service}/{CompanyId}.", service, companyId);
                return null;
            }

            logger.LogInformation("Token cache hit for {Service}/{CompanyId}.", service, companyId);
            return ticket;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not read token cache for {Service}/{CompanyId}.", service, companyId);
            return null;
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task SetAsync(string service, long companyId, LoginTicketResponse ticket, CancellationToken ct = default)
    {
        var key = BuildKey(service, companyId);
        var path = BuildPath(key);
        var sem = GetSemaphore(key);

        await sem.WaitAsync(ct);
        try
        {
            await File.WriteAllBytesAsync(path, SerializeToken(ticket), ct);
            logger.LogInformation("Token cache written for {Service}/{CompanyId}.", service, companyId);
        }
        finally
        {
            sem.Release();
        }
    }

    private static string BuildKey(string service, long companyId) =>
        $"{service}_{companyId}";

    private string BuildPath(string key) =>
        Path.Combine(cacheDirectory, $"ARCA_token_{key}.bin");

    private SemaphoreSlim GetSemaphore(string key) =>
        locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    /// <summary>
    /// Serializes and encrypts a token for disk storage.
    /// On Windows uses DPAPI (CurrentUser scope) — on other platforms writes plain JSON.
    /// Serializa y cifra un token para almacenamiento en disco.
    /// En Windows usa DPAPI (scope CurrentUser) — en otras plataformas escribe JSON plano.
    /// </summary>
    private static byte[] SerializeToken(LoginTicketResponse ticket)
    {
        var json = JsonConvert.SerializeObject(ticket, Formatting.Indented);
        var bytes = Encoding.UTF8.GetBytes(json);

        if (OperatingSystem.IsWindows())
            return ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);

        return bytes;
    }

    /// <summary>
    /// Decrypts and deserializes a token from disk.
    /// Descifra y deserializa un token desde disco.
    /// </summary>
    private static LoginTicketResponse? DeserializeToken(byte[] data)
    {
        var jsonBytes = OperatingSystem.IsWindows()
            ? ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser)
            : data;

        return JsonConvert.DeserializeObject<LoginTicketResponse>(Encoding.UTF8.GetString(jsonBytes));
    }
}
