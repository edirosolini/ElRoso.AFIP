// <copyright file="IWsfeOperations.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Billing;

using ElRoso.ARCA.Core;
using ElRoso.ARCA.Billing;
using ElRoso.ARCA.Read;

/// <summary>
/// EN: Thin wrapper over the ARCA WSFEv1 SOAP client (domestic e-invoicing).
/// ES: Wrapper fino sobre el cliente SOAP del WSFEv1 de ARCA (facturación doméstica).
/// </summary>
internal interface IWsfeOperations
{
    /// <summary>
    /// EN: Returns the last authorized document number for the given type + book.
    /// ES: Devuelve el último número autorizado para el tipo + libro dados.
    /// </summary>
    Task<int> GetLastNumberAsync(
        string sign,
        string token,
        long cuit,
        int docType,
        int bookPrefix,
        CancellationToken ct);

    /// <summary>
    /// EN: Requests a CAE for one document. Returns POCO result with errors / observations / approval.
    /// ES: Solicita un CAE para un documento. Devuelve resultado POCO con errores / observaciones / aprobación.
    /// </summary>
    Task<WsfeCaeResult> SolicitarCaeAsync(
        string sign,
        string token,
        long cuit,
        BillingDocumentNumberingRequest doc,
        int next,
        CancellationToken ct);
}

/// <summary>
/// EN: POCO result of a CAE request to WSFEv1 — hides WCF types from the consumer.
/// ES: Resultado POCO de una solicitud de CAE a WSFEv1 — oculta los tipos WCF del consumidor.
/// </summary>
internal sealed record WsfeCaeResult
{
    /// <summary>True when CAE was granted (Resultado == "A"). / True cuando el CAE fue otorgado.</summary>
    public bool IsApproved { get; init; }

    /// <summary>The CAE code returned by ARCA. / Código CAE devuelto por ARCA.</summary>
    public string? Cae { get; init; }

    /// <summary>CAE expiration date. / Fecha de vencimiento del CAE.</summary>
    public DateTime? CaeExpiration { get; init; }

    /// <summary>Errors returned by WSFEv1 (header-level or detail observations on rejection).</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
