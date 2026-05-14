// <copyright file="MailboxQueryRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Requests;

/// <summary>
/// EN: Request to query notifications from ARCA's e-Ventanilla (WSCComu).
/// Supports filtering by state, date range, system, attachment flag; paginated.
/// ES: Request para consultar notificaciones del e-Ventanilla de ARCA (WSCComu).
/// Soporta filtros por estado, rango de fechas, sistema, adjunto; paginado.
/// </summary>
public class MailboxQueryRequest
{
    /// <summary>Issuing company being queried (your CUIT or the delegating CUIT).</summary>
    public IssuingCompanyRequest IssuingCompany { get; set; } = new();

    /// <summary>State id from ARCA (use null for all). / ID de estado (null para todos).</summary>
    public int? StateId { get; set; }

    /// <summary>Filter by date range — from. / Filtro fecha desde.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Filter by date range — to. / Filtro fecha hasta.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Filter to messages that have attachments. / Filtra mensajes con adjunto.</summary>
    public bool? HasAttachment { get; set; }

    /// <summary>Filter by publishing system id. / Filtra por ID del sistema publicador.</summary>
    public long? PublishingSystemId { get; set; }

    /// <summary>1-based page number. Default 1. / Número de página (base 1). Default 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page (max varies per service). / Items por página.</summary>
    public int? PageSize { get; set; }

    /// <summary>Optional reference filter #1. / Filtro de referencia opcional #1.</summary>
    public string? Reference1 { get; set; }

    /// <summary>Optional reference filter #2. / Filtro de referencia opcional #2.</summary>
    public string? Reference2 { get; set; }
}
