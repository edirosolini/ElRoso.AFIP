// <copyright file="BillingDocumentNumberingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Services;

using ElRoso.ARCA.Caching;
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Responses;
using ElRoso.ARCA.Domains.Services;
using ElRoso.ARCA.Exceptions;
using ElRoso.ARCA.Options;
using ElRoso.ARCA.Services.Soap;
using FluentValidation;
using Microsoft.Extensions.Logging;

internal sealed class BillingDocumentNumberingService : IBillingDocumentNumberingService
{
    private readonly ILoginTicketService loginTicketService;
    private readonly ITokenCache tokenCache;
    private readonly ARCAOptions options;
    private readonly ILogger<BillingDocumentNumberingService> logger;
    private readonly IValidator<BillingDocumentNumberingRequest> validator;
    private readonly IPadronOperations padron;
    private readonly IWsfeOperations wsfe;
    private readonly IWsfexOperations wsfex;

    public BillingDocumentNumberingService(
        ILogger<BillingDocumentNumberingService> logger,
        ILoginTicketService loginTicketService,
        ITokenCache tokenCache,
        ARCAOptions options,
        IValidator<BillingDocumentNumberingRequest> validator,
        IPadronOperations padron,
        IWsfeOperations wsfe,
        IWsfexOperations wsfex)
    {
        this.logger = logger;
        this.loginTicketService = loginTicketService;
        this.tokenCache = tokenCache;
        this.options = options;
        this.validator = validator;
        this.padron = padron;
        this.wsfe = wsfe;
        this.wsfex = wsfex;
    }

    public async Task<BillingDocumentNumberingResponse> AuthorizeAsync(
        BillingDocumentNumberingRequest request,
        CancellationToken ct = default)
    {
        // Validate request before hitting the network.
        // Validar el request antes de tocar la red.
        var validation = this.validator.Validate(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => $"{e.ErrorCode}: {e.ErrorMessage}").ToList();
            throw new ARCAValidationException(errors);
        }

        return request.BillingDocumentType switch
        {
            BillingDocumentTypeARCAEnum.FA or BillingDocumentTypeARCAEnum.NDA or BillingDocumentTypeARCAEnum.NCA
            or BillingDocumentTypeARCAEnum.FB or BillingDocumentTypeARCAEnum.NDB or BillingDocumentTypeARCAEnum.NCB
            or BillingDocumentTypeARCAEnum.FC or BillingDocumentTypeARCAEnum.NDC or BillingDocumentTypeARCAEnum.NCC
                => await AuthorizeDomesticAsync(request, ct),

            BillingDocumentTypeARCAEnum.InvoiceExport
            or BillingDocumentTypeARCAEnum.DebitNoteExport
            or BillingDocumentTypeARCAEnum.CreditNoteExport
                => await AuthorizeExportAsync(request, ct),

            _ => throw new ARCAServiceException($"Unsupported document type: {request.BillingDocumentType}."),
        };
    }

    // ------------------------------------------------------------------ //
    // Domestic invoicing — WSFEv1
    // Facturación doméstica — WSFEv1
    // ------------------------------------------------------------------ //

    private async Task<BillingDocumentNumberingResponse> AuthorizeDomesticAsync(
        BillingDocumentNumberingRequest request, CancellationToken ct)
    {
        // Padron lookup: only for CUIT recipients (determines VAT condition + name).
        // Consulta al padrón: solo para destinatarios con CUIT.
        if (request.Client.DocumentType == DocumentTypeARCAEnum.CUIT)
        {
            var padronTicket = await GetOrRefreshTokenAsync("ws_sr_constancia_inscripcion", request.IssuingCompany.DocumentNumber, ct);
            var padronResult = await padron.GetPersonaAsync(
                padronTicket.Sign,
                padronTicket.Token,
                request.IssuingCompany.DocumentNumber,
                request.Client.DocumentNumber,
                ct);

            if (padronResult.Errors is { Count: > 0 })
                return new BillingDocumentNumberingResponse { Errors = [.. padronResult.Errors] };

            request.Client.SetCondition(padronResult.IsMonotributo
                ? VATConditionARCAEnum.MONOTRIBUTO
                : VATConditionARCAEnum.RESPONSABLE_INSCRIPTO);
            request.Client.SetClientName(padronResult.ClientName);
        }
        else
        {
            request.Client.SetCondition(VATConditionARCAEnum.CONSUMIDOR_FINAL);
        }

        var ticket = await GetOrRefreshTokenAsync("wsfe", request.IssuingCompany.DocumentNumber, ct);

        var lastNumber = await wsfe.GetLastNumberAsync(
            ticket.Sign,
            ticket.Token,
            request.IssuingCompany.DocumentNumber,
            (int)request.BillingDocumentType,
            request.BillingDocumentBookPrefix,
            ct);

        var next = lastNumber + 1;
        var caeResult = await wsfe.SolicitarCaeAsync(
            ticket.Sign,
            ticket.Token,
            request.IssuingCompany.DocumentNumber,
            request,
            next,
            ct);

        if (!caeResult.IsApproved)
            return new BillingDocumentNumberingResponse { Errors = [.. caeResult.Errors] };

        var response = BillingDocumentNumberingResponse.FromRequest(request);
        response.Result = true;
        response.CAE = caeResult.Cae;
        response.BillingDocumentNumber = next;
        response.BillingDocumentBookExpirationDate = caeResult.CaeExpiration;
        return response;
    }

    // ------------------------------------------------------------------ //
    // Export invoicing — WSFEXv1
    // Facturación de exportación — WSFEXv1
    // ------------------------------------------------------------------ //

    private async Task<BillingDocumentNumberingResponse> AuthorizeExportAsync(
        BillingDocumentNumberingRequest request, CancellationToken ct)
    {
        var ticket = await GetOrRefreshTokenAsync("wsfex", request.IssuingCompany.DocumentNumber, ct);

        var lastNumber = await wsfex.GetLastNumberAsync(
            ticket.Sign,
            ticket.Token,
            request.IssuingCompany.DocumentNumber,
            (short)request.BillingDocumentType,
            request.BillingDocumentBookPrefix,
            ct);

        var next = lastNumber + 1;
        var caeResult = await wsfex.AuthorizeAsync(
            ticket.Sign,
            ticket.Token,
            request.IssuingCompany.DocumentNumber,
            request,
            next,
            ct);

        if (!caeResult.IsApproved)
            return new BillingDocumentNumberingResponse { Errors = [.. caeResult.Errors] };

        var response = BillingDocumentNumberingResponse.FromRequest(request);
        response.Result = true;
        response.CAE = caeResult.Cae;
        response.BillingDocumentNumber = caeResult.DocumentNumber;
        response.BillingDocumentBookExpirationDate = caeResult.CaeExpiration;
        return response;
    }

    // ------------------------------------------------------------------ //
    // Token cache helper
    // Helper de caché de tokens
    // ------------------------------------------------------------------ //

    private async Task<LoginTicketResponse> GetOrRefreshTokenAsync(string service, long companyId, CancellationToken ct)
    {
        var cached = await tokenCache.GetAsync(service, companyId, ct);
        if (cached is not null)
        {
            logger.LogInformation("Token cache hit for {Service}/{CompanyId}.", service, companyId);
            return cached;
        }

        logger.LogInformation("Token cache miss for {Service}/{CompanyId}. Requesting WSAA.", service, companyId);
        var fresh = await loginTicketService.GetLoginTicketAsync(
            service, options.WsaaUrl, options.CertificatePath, options.CertificatePassword, ct);

        await tokenCache.SetAsync(service, companyId, fresh, ct);
        return fresh;
    }
}
