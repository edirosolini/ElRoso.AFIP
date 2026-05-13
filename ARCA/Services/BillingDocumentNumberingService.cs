// <copyright file="BillingDocumentNumberingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Services;

using ElRoso.ARCA.Caching;
using ElRoso.ARCA.Commons;
using ElRoso.ARCA.Domains.Enums;
using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Responses;
using ElRoso.ARCA.Domains.Services;
using ElRoso.ARCA.Exceptions;
using ElRoso.ARCA.Options;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Globalization;

internal sealed class BillingDocumentNumberingService : IBillingDocumentNumberingService
{
    private readonly ILoginTicketService loginTicketService;
    private readonly ITokenCache tokenCache;
    private readonly ARCAOptions options;
    private readonly ILogger<BillingDocumentNumberingService> logger;
    private readonly IValidator<BillingDocumentNumberingRequest> validator;

    public BillingDocumentNumberingService(
        ILogger<BillingDocumentNumberingService> logger,
        ILoginTicketService loginTicketService,
        ITokenCache tokenCache,
        ARCAOptions options,
        IValidator<BillingDocumentNumberingRequest> validator)
    {
        this.logger = logger;
        this.loginTicketService = loginTicketService;
        this.tokenCache = tokenCache;
        this.options = options;
        this.validator = validator;
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
            var padronClient = new Padron.PersonaServiceA5Client(
                Padron.PersonaServiceA5Client.EndpointConfiguration.PersonaServiceA5Port,
                options.PadronUrl);
            padronClient.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(options.SoapTimeoutSeconds);

            var personaResp = await padronClient.getPersonaAsync(new Padron.getPersona
            {
                sign = padronTicket.Sign,
                token = padronTicket.Token,
                cuitRepresentada = request.IssuingCompany.DocumentNumber,
                idPersona = request.Client.DocumentNumber,
            });

            if (personaResp.personaReturn.errorConstancia != null)
            {
                var errors = personaResp.personaReturn.errorConstancia.error
                    .Select(e => $"{e} - CUIT: {request.Client.DocumentNumber}")
                    .ToList();
                return new BillingDocumentNumberingResponse { Errors = errors };
            }

            var datos = personaResp.personaReturn.datosGenerales;
            var condition = personaResp.personaReturn.datosMonotributo != null
                ? VATConditionARCAEnum.MONOTRIBUTO
                : VATConditionARCAEnum.RESPONSABLE_INSCRIPTO;

            var name = datos.tipoPersona == "FISICA"
                ? $"{datos.apellido} {datos.nombre}"
                : datos.razonSocial;

            request.Client.SetCondition(condition);
            request.Client.SetClientName(name);
        }
        else
        {
            request.Client.SetCondition(VATConditionARCAEnum.CONSUMIDOR_FINAL);
        }

        var ticket = await GetOrRefreshTokenAsync("wsfe", request.IssuingCompany.DocumentNumber, ct);
        var wsfeClient = new WSFEv1.ServiceSoapClient(
            WSFEv1.ServiceSoapClient.EndpointConfiguration.ServiceSoap12,
            options.WsfeUrl);
        wsfeClient.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(options.SoapTimeoutSeconds);

        var auth = new WSFEv1.FEAuthRequest
        {
            Sign = ticket.Sign,
            Token = ticket.Token,
            Cuit = request.IssuingCompany.DocumentNumber,
        };

        var lastNumber = await GetLastDomesticNumberAsync(wsfeClient, auth, (int)request.BillingDocumentType, request.BillingDocumentBookPrefix);
        return await RequestCaeAsync(wsfeClient, auth, request, lastNumber);
    }

    private static async Task<int> GetLastDomesticNumberAsync(
        WSFEv1.ServiceSoapClient client, WSFEv1.FEAuthRequest auth, int docType, int bookPrefix)
    {
        try
        {
            var result = await client.FECompUltimoAutorizadoAsync(new WSFEv1.FECompUltimoAutorizadoRequest
            {
                Body = new WSFEv1.FECompUltimoAutorizadoRequestBody
                {
                    Auth = auth,
                    CbteTipo = docType,
                    PtoVta = bookPrefix,
                },
            });
            return result.Body.FECompUltimoAutorizadoResult.CbteNro;
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA WSFEv1 FECompUltimoAutorizado failed.", ex);
        }
    }

    private static async Task<BillingDocumentNumberingResponse> RequestCaeAsync(
        WSFEv1.ServiceSoapClient client,
        WSFEv1.FEAuthRequest auth,
        BillingDocumentNumberingRequest doc,
        int lastNumber)
    {
        var next = lastNumber + 1;
        var isTypeWithVAT = doc.BillingDocumentType is
            BillingDocumentTypeARCAEnum.FA or BillingDocumentTypeARCAEnum.NDA or BillingDocumentTypeARCAEnum.NCA or
            BillingDocumentTypeARCAEnum.FB or BillingDocumentTypeARCAEnum.NDB or BillingDocumentTypeARCAEnum.NCB;

        var condicionIva = doc.Client.Condition switch
        {
            VATConditionARCAEnum.RESPONSABLE_INSCRIPTO => 1,
            VATConditionARCAEnum.MONOTRIBUTO => 6,
            _ => 5,
        };

        var body = new WSFEv1.FECAESolicitarRequestBody
        {
            Auth = auth,
            FeCAEReq = new WSFEv1.FECAERequest
            {
                FeCabReq = new WSFEv1.FECAECabRequest
                {
                    CantReg = 1,
                    PtoVta = doc.BillingDocumentBookPrefix,
                    CbteTipo = (int)doc.BillingDocumentType,
                },
                FeDetReq =
                [
                    new()
                    {
                        Concepto = (int)doc.ConceptType,
                        CondicionIVAReceptorId = condicionIva,
                        DocTipo = (int)doc.Client.DocumentType,
                        DocNro = doc.Client.DocumentNumber,
                        CbteDesde = next,
                        CbteHasta = next,
                        CbteFch = doc.BillingDocumentDate.ToString("yyyyMMdd"),
                        CbtesAsoc = doc.BillingDocumentType is
                            BillingDocumentTypeARCAEnum.FA or
                            BillingDocumentTypeARCAEnum.FB or
                            BillingDocumentTypeARCAEnum.FC
                            ? null
                            : [.. doc.BillingDocumentNumberingAssociateds!.Select(x => new WSFEv1.CbteAsoc
                            {
                                CbteFch = x.BillingDocumentDate.ToString("yyyyMMdd"),
                                Nro = x.BillingDocumentNumber,
                                PtoVta = x.BillingDocumentBookPrefix,
                                Tipo = (int)x.BillingDocumentType,
                            })],
                        ImpTotal = Math.Round(doc.Total, 2),
                        ImpTotConc = isTypeWithVAT ? Math.Round(doc.AmountNotTax, 2) : 0,
                        ImpNeto = Math.Round(doc.AmountTax, 2),
                        ImpOpEx = 0,
                        ImpTrib = Math.Round(doc.BillingDocumentNumberingOtherTaxAmount, 2),
                        ImpIVA = isTypeWithVAT ? Math.Round(doc.BillingDocumentNumberingTaxAmount, 2) : 0,
                        FchServDesde = doc.ConceptType == ConceptTypeARCAEnum.Products ? string.Empty : doc.DateOfServicesFrom?.ToString("yyyyMMdd"),
                        FchServHasta = doc.ConceptType == ConceptTypeARCAEnum.Products ? string.Empty : doc.DateOfServicesTo?.ToString("yyyyMMdd"),
                        FchVtoPago = doc.ConceptType == ConceptTypeARCAEnum.Products ? string.Empty : doc.PaymentDue?.ToString("yyyyMMdd"),
                        MonId = DictionariesCommon.Currencies[doc.Currency],
                        MonCotiz = Math.Round(doc.ExchangeRate, 2),
                        Iva = isTypeWithVAT
                            ? [.. doc.BillingDocumentNumberingTaxes!.Select(x => new WSFEv1.AlicIva
                                {
                                    Id = DictionariesCommon.Tax[x.PercentageTax],
                                    BaseImp = Math.Round(x.BaseAmount, 2),
                                    Importe = Math.Round(x.Amount, 2),
                                })]
                            : null,
                        Tributos = doc.BillingDocumentNumberingOtherTaxes?
                            .Select(x => new WSFEv1.Tributo
                            {
                                Id = DictionariesCommon.OtherTax[x.Description],
                                Desc = x.Description,
                                BaseImp = Math.Round(x.BaseAmount, 2),
                                Alic = Math.Round(x.PercentageTax, 2),
                                Importe = Math.Round(x.Amount, 2),
                            }).ToArray(),
                    },
                ],
            },
        };

        try
        {
            var result = await client.FECAESolicitarAsync(new WSFEv1.FECAESolicitarRequest { Body = body });
            var wsResult = result.Body.FECAESolicitarResult;

            if (wsResult.Errors != null)
            {
                var errors = wsResult.Errors.Select(e => $"{e.Code}: {e.Msg}").ToList();
                return new BillingDocumentNumberingResponse { Errors = errors };
            }

            // Guard against malformed responses with missing or empty FeDetResp.
            // Protección contra respuestas malformadas con FeDetResp vacío o nulo.
            if (wsResult.FeDetResp is not { Length: > 0 })
                throw new ARCAServiceException("WSFEv1 returned an empty or null FeDetResp.");

            var det = wsResult.FeDetResp[0];
            if (det.Resultado == "A")
            {
                var response = BillingDocumentNumberingResponse.FromRequest(doc);
                response.Result = true;
                response.CAE = det.CAE;
                response.BillingDocumentNumber = next;
                response.BillingDocumentBookExpirationDate =
                    DateTime.ParseExact(det.CAEFchVto, "yyyyMMdd", CultureInfo.InvariantCulture);
                return response;
            }

            // Resultado == "R" — rejected with observations.
            // Resultado == "R" — rechazado con observaciones.
            // Use null-coalescing to guard against Observaciones being null.
            // Se usa null-coalescing para proteger contra Observaciones nulo.
            var rejErrors = wsResult.FeDetResp
                .SelectMany(d => d.Observaciones ?? [])
                .Select(o => $"{o.Code}: {o.Msg}")
                .ToList();
            return new BillingDocumentNumberingResponse { Errors = rejErrors };
        }
        catch (ARCAServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA WSFEv1 FECAESolicitar failed.", ex);
        }
    }

    // ------------------------------------------------------------------ //
    // Export invoicing — WSFEXv1
    // Facturación de exportación — WSFEXv1
    // ------------------------------------------------------------------ //

    private async Task<BillingDocumentNumberingResponse> AuthorizeExportAsync(
        BillingDocumentNumberingRequest request, CancellationToken ct)
    {
        var ticket = await GetOrRefreshTokenAsync("wsfex", request.IssuingCompany.DocumentNumber, ct);
        var wsfexClient = new WSFEXv1.ServiceSoapClient(
            WSFEXv1.ServiceSoapClient.EndpointConfiguration.ServiceSoap12,
            options.WsfexUrl);
        wsfexClient.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(options.SoapTimeoutSeconds);

        var auth = new WSFEXv1.ClsFEXAuthRequest
        {
            Sign = ticket.Sign,
            Token = ticket.Token,
            Cuit = request.IssuingCompany.DocumentNumber,
        };

        var lastNumber = await GetLastExportNumberAsync(wsfexClient, auth, (short)request.BillingDocumentType, request.BillingDocumentBookPrefix);
        return await RequestCaeExportAsync(wsfexClient, auth, request, lastNumber);
    }

    private static async Task<long> GetLastExportNumberAsync(
        WSFEXv1.ServiceSoapClient client, WSFEXv1.ClsFEXAuthRequest auth, short docType, int bookPrefix)
    {
        try
        {
            var result = await client.FEXGetLast_CMPAsync(new WSFEXv1.FEXGetLast_CMPRequest
            {
                Auth = new WSFEXv1.ClsFEX_LastCMP
                {
                    Token = auth.Token,
                    Sign = auth.Sign,
                    Cuit = auth.Cuit,
                    Cbte_Tipo = docType,
                    Pto_venta = bookPrefix,
                },
            });
            return result.FEXGetLast_CMPResult.FEXResult_LastCMP.Cbte_nro;
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA WSFEXv1 FEXGetLast_CMP failed.", ex);
        }
    }

    private static async Task<BillingDocumentNumberingResponse> RequestCaeExportAsync(
        WSFEXv1.ServiceSoapClient client,
        WSFEXv1.ClsFEXAuthRequest auth,
        BillingDocumentNumberingRequest doc,
        long lastNumber)
    {
        var next = lastNumber + 1;

        var request = new WSFEXv1.FEXAuthorizeRequest
        {
            Auth = auth,
            Cmp = new WSFEXv1.ClsFEXRequest
            {
                Id = (long)doc.BillingDocumentId!,
                Cbte_Tipo = (short)(int)doc.BillingDocumentType,
                Fecha_cbte = doc.BillingDocumentDate.ToString("yyyyMMdd"),
                Punto_vta = doc.BillingDocumentBookPrefix,
                Cbte_nro = next,
                Tipo_expo = (short)doc.ConceptType,
                Permiso_existente = string.Empty,
                Dst_cmp = doc.Client.CountryId,
                Cliente = doc.Client.ClientName,
                Cuit_pais_cliente = doc.Client.DocumentNumber,
                Domicilio_cliente = doc.Client.Address,
                Moneda_Id = DictionariesCommon.Currencies[doc.Currency],
                Moneda_ctz = (decimal)doc.ExchangeRate,
                Imp_total = (decimal)doc.Total,
                Idioma_cbte = DictionariesCommon.Language[doc.Client.ClientLanguage],
                Items = [.. doc.Items!.Select(i => new WSFEXv1.Item
                {
                    Pro_ds = i.ItemDescription,
                    Pro_umed = 0,
                    Pro_total_item = (decimal)i.Amount,
                })],
                Fecha_pago = doc.ConceptType == ConceptTypeARCAEnum.Products
                    ? string.Empty
                    : doc.PaymentDue?.ToString("yyyyMMdd"),
            },
        };

        try
        {
            var result = await client.FEXAuthorizeAsync(request);
            var fexResult = result.FEXAuthorizeResult;

            if (fexResult.FEXErr.ErrCode != 0)
            {
                return new BillingDocumentNumberingResponse
                {
                    Errors = [$"{fexResult.FEXErr.ErrCode}: {fexResult.FEXErr.ErrMsg}"],
                };
            }

            var response = BillingDocumentNumberingResponse.FromRequest(doc);
            response.Result = true;
            response.CAE = fexResult.FEXResultAuth.Cae;
            response.BillingDocumentNumber = (int)fexResult.FEXResultAuth.Cbte_nro;
            response.BillingDocumentBookExpirationDate =
                DateTime.ParseExact(fexResult.FEXResultAuth.Fch_venc_Cae, "yyyyMMdd", CultureInfo.InvariantCulture);
            return response;
        }
        catch (ARCAServiceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ARCAServiceException("ARCA WSFEXv1 FEXAuthorize failed.", ex);
        }
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
