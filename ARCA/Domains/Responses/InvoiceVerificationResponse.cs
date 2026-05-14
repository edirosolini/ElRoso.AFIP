// <copyright file="InvoiceVerificationResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Responses;

/// <summary>
/// EN: Result of validating a received voucher with ARCA WSCDC.
/// ES: Resultado de validar un comprobante recibido contra WSCDC de ARCA.
/// </summary>
public class InvoiceVerificationResponse
{
    /// <summary>True when ARCA confirms the voucher is authorized (Resultado == "A").</summary>
    public bool IsAuthorized { get; set; }

    /// <summary>Raw ARCA result code: "A" approved, "R" rejected. / Código crudo: "A" aprobado, "R" rechazado.</summary>
    public string? Resultado { get; set; }

    /// <summary>Date when ARCA processed the verification. / Fecha en que ARCA procesó la verificación.</summary>
    public DateTime? ProcessedDate { get; set; }

    /// <summary>Observations returned by ARCA (rejection reasons). / Observaciones (motivos de rechazo).</summary>
    public List<string> Observations { get; set; } = [];

    /// <summary>Errors returned by ARCA (request-level problems). / Errores devueltos por ARCA.</summary>
    public List<string> Errors { get; set; } = [];
}
