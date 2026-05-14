// <copyright file="InvoiceVerificationOperations.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Services.Soap;

using System.Globalization;
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Exceptions;
using ElRoso.ARCA.Options;

internal sealed class InvoiceVerificationOperations : IInvoiceVerificationOperations
{
    private readonly ARCAOptions options;

    public InvoiceVerificationOperations(ARCAOptions options)
    {
        this.options = options;
    }

    public async Task<InvoiceVerificationOperationResult> VerifyAsync(
        string sign,
        string token,
        long issuingCuit,
        InvoiceVerificationRequest request,
        CancellationToken ct)
    {
        try
        {
            var client = new WSCDC.ServiceSoapClient(
                WSCDC.ServiceSoapClient.EndpointConfiguration.ServiceSoap12,
                options.WscdcUrl);
            client.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(options.SoapTimeoutSeconds);

            var auth = new WSCDC.CmpAuthRequest { Sign = sign, Token = token, Cuit = issuingCuit };
            var cmpDatos = BuildCmpDatos(request);

            var result = await client.ComprobanteConstatarAsync(auth, cmpDatos);
            var resp = result.Body.ComprobanteConstatarResult;

            if (resp.Errors is { Length: > 0 })
            {
                var errors = resp.Errors.Select(e => $"{e.Code}: {e.Msg}").ToList();
                return new InvoiceVerificationOperationResult { Errors = errors };
            }

            var observations = resp.Observaciones?
                .Select(o => $"{o.Code}: {o.Msg}")
                .ToList() ?? [];

            DateTime? processedDate = TryParseFchProceso(resp.FchProceso);

            return new InvoiceVerificationOperationResult
            {
                IsAuthorized = resp.Resultado == "A",
                Resultado = resp.Resultado,
                ProcessedDate = processedDate,
                Observations = observations,
            };
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA WSCDC ComprobanteConstatar failed.", ex);
        }
    }

    private static WSCDC.CmpDatos BuildCmpDatos(InvoiceVerificationRequest request) => new()
    {
        CbteModo = request.AuthorizationMode.ToString(),
        CuitEmisor = request.IssuingCompany.DocumentNumber,
        PtoVta = request.BillingDocumentBookPrefix,
        CbteTipo = (int)request.BillingDocumentType,
        CbteNro = request.BillingDocumentNumber,
        CbteFch = request.BillingDocumentDate.ToString("yyyyMMdd"),
        ImpTotal = Math.Round(request.TotalAmount, 2),
        CodAutorizacion = request.AuthorizationCode,
        DocTipoReceptor = request.Receiver is null
            ? null
            : ((int)request.Receiver.DocumentType).ToString(CultureInfo.InvariantCulture),
        DocNroReceptor = request.Receiver is null
            ? null
            : request.Receiver.DocumentNumber.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// EN: Parses the ARCA FchProceso field. Supports both yyyyMMdd and yyyyMMddHHmmss formats.
    /// ES: Parsea el campo FchProceso de ARCA. Soporta yyyyMMdd y yyyyMMddHHmmss.
    /// </summary>
    private static DateTime? TryParseFchProceso(string? fchProceso)
    {
        if (string.IsNullOrEmpty(fchProceso))
            return null;

        if (DateTime.TryParseExact(fchProceso, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParseExact(fchProceso, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        return null;
    }
}
