// <copyright file="AuthorizedBillingDocumentResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Billing
{
    using ElRoso.ARCA.Core;

    /// <summary>
    /// EN: A voucher ARCA has already authorized, as returned by WSFEv1 FECompConsultar.
    ///     Use it to reconcile a document that ARCA approved but the caller never persisted
    ///     (e.g. the local save failed after the CAE was granted) — re-authorizing instead
    ///     would burn a second CAE for the same sale.
    /// ES: Un comprobante que ARCA ya autorizó, tal como lo devuelve WSFEv1 FECompConsultar.
    ///     Sirve para reconciliar un comprobante que ARCA aprobó pero el llamador nunca
    ///     persistió (p. ej. el guardado local falló después de otorgado el CAE) —
    ///     re-autorizar en lugar de reconciliar quema un segundo CAE por la misma venta.
    /// </summary>
    public sealed record AuthorizedBillingDocumentResponse
    {
        /// <summary>
        /// EN: True when ARCA reports the voucher as approved (Resultado == "A").
        /// ES: True cuando ARCA reporta el comprobante como aprobado (Resultado == "A").
        /// </summary>
        public bool IsApproved { get; init; }

        /// <summary>
        /// EN: Authorization code (CAE) ARCA granted to this voucher.
        /// ES: Código de autorización (CAE) que ARCA otorgó a este comprobante.
        /// </summary>
        public string? CAE { get; init; }

        /// <summary>
        /// EN: CAE expiration date.
        /// ES: Fecha de vencimiento del CAE.
        /// </summary>
        public DateTime? CAEExpirationDate { get; init; }

        /// <summary>
        /// EN: Date ARCA processed the authorization.
        /// ES: Fecha en que ARCA procesó la autorización.
        /// </summary>
        public DateTime? ProcessedDate { get; init; }

        /// <summary>
        /// EN: Point of sale the voucher belongs to.
        /// ES: Punto de venta al que pertenece el comprobante.
        /// </summary>
        public int BillingDocumentBookPrefix { get; init; }

        /// <summary>
        /// EN: Voucher type as reported by ARCA.
        /// ES: Tipo de comprobante informado por ARCA.
        /// </summary>
        public BillingDocumentTypeARCAEnum BillingDocumentType { get; init; }

        /// <summary>
        /// EN: Voucher number that was queried.
        /// ES: Número de comprobante consultado.
        /// </summary>
        public long BillingDocumentNumber { get; init; }

        /// <summary>
        /// EN: Observations ARCA attached to the voucher, if any.
        /// ES: Observaciones que ARCA adjuntó al comprobante, si hay.
        /// </summary>
        public IReadOnlyList<string> Observations { get; init; } = [];

        /// <summary>
        /// EN: Errors returned by the query itself (not by the voucher).
        /// ES: Errores devueltos por la consulta en sí (no por el comprobante).
        /// </summary>
        public IReadOnlyList<string> Errors { get; init; } = [];
    }
}
