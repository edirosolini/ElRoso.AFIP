// <copyright file="IBillingDocumentNumberingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Billing
{
    using ElRoso.ARCA.Core;
using ElRoso.ARCA.Billing;
using ElRoso.ARCA.Read;

    /// <summary>
    /// Requests a CAE (Electronic Authorization Code) from ARCA for a billing document.
    /// Solicita un CAE (Código de Autorización Electrónico) a ARCA para un comprobante.
    /// </summary>
    public interface IBillingDocumentNumberingService
    {
        /// <summary>
        /// Validates the request and obtains a CAE from ARCA.
        /// Valida el request y obtiene un CAE de ARCA.
        /// </summary>
        /// <param name="request">Billing document to authorize.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Response with CAE and document number, or error list.</returns>
        Task<BillingDocumentNumberingResponse> AuthorizeAsync(
            BillingDocumentNumberingRequest request,
            CancellationToken ct = default);
    }
}