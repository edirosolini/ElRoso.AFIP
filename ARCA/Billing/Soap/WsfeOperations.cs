// <copyright file="WsfeOperations.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Billing;

using System.Globalization;
using ElRoso.ARCA.Billing;
using ElRoso.ARCA.Core;
using ElRoso.ARCA.Read;

internal sealed class WsfeOperations : IWsfeOperations
{
    private readonly ARCAOptions options;

    public WsfeOperations(ARCAOptions options)
    {
        this.options = options;
    }

    public async Task<int> GetLastNumberAsync(
        string sign,
        string token,
        long cuit,
        int docType,
        int bookPrefix,
        CancellationToken ct)
    {
        try
        {
            var client = CreateClient();
            var result = await client.FECompUltimoAutorizadoAsync(new WSFEv1.FECompUltimoAutorizadoRequest
            {
                Body = new WSFEv1.FECompUltimoAutorizadoRequestBody
                {
                    Auth = new WSFEv1.FEAuthRequest { Sign = sign, Token = token, Cuit = cuit },
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

    public async Task<WsfeCaeResult> SolicitarCaeAsync(
        string sign,
        string token,
        long cuit,
        BillingDocumentNumberingRequest doc,
        int next,
        CancellationToken ct)
    {
        var auth = new WSFEv1.FEAuthRequest { Sign = sign, Token = token, Cuit = cuit };
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
            var client = CreateClient();
            var result = await client.FECAESolicitarAsync(new WSFEv1.FECAESolicitarRequest { Body = body });
            var wsResult = result.Body.FECAESolicitarResult;

            if (wsResult.Errors != null)
            {
                var errors = wsResult.Errors.Select(e => $"{e.Code}: {e.Msg}").ToList();
                return new WsfeCaeResult { IsApproved = false, Errors = errors };
            }

            // Guard against malformed responses with missing or empty FeDetResp.
            // Protección contra respuestas malformadas con FeDetResp vacío o nulo.
            if (wsResult.FeDetResp is not { Length: > 0 })
                throw new ARCAServiceException("WSFEv1 returned an empty or null FeDetResp.");

            var det = wsResult.FeDetResp[0];
            if (det.Resultado == "A")
            {
                return new WsfeCaeResult
                {
                    IsApproved = true,
                    Cae = det.CAE,
                    CaeExpiration = DateTime.ParseExact(det.CAEFchVto, "yyyyMMdd", CultureInfo.InvariantCulture),
                };
            }

            // Resultado == "R" — rejected with observations.
            // Resultado == "R" — rechazado con observaciones.
            var rejErrors = wsResult.FeDetResp
                .SelectMany(d => d.Observaciones ?? [])
                .Select(o => $"{o.Code}: {o.Msg}")
                .ToList();
            return new WsfeCaeResult { IsApproved = false, Errors = rejErrors };
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

    private WSFEv1.ServiceSoapClient CreateClient()
    {
        var client = new WSFEv1.ServiceSoapClient(
            WSFEv1.ServiceSoapClient.EndpointConfiguration.ServiceSoap12,
            options.WsfeUrl);
        client.InnerChannel.OperationTimeout = TimeSpan.FromSeconds(options.SoapTimeoutSeconds);
        return client;
    }
}