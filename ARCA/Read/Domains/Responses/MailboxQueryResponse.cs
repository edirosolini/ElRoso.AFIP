// <copyright file="MailboxQueryResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Read;

/// <summary>
/// EN: Paginated list of notification summaries from ARCA's e-Ventanilla.
/// ES: Listado paginado de resúmenes de notificaciones del e-Ventanilla.
/// </summary>
public class MailboxQueryResponse
{
    public List<MailboxMessageSummary> Messages { get; set; } = [];

    public int Page { get; set; }

    public int TotalPages { get; set; }

    public int ItemsPerPage { get; set; }

    public int TotalItems { get; set; }

    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// EN: Notification summary (no body / attachments). Use ConsumeAsync to get the full message.
/// ES: Resumen de notificación (sin cuerpo ni adjuntos). Usar ConsumeAsync para el mensaje completo.
/// </summary>
public class MailboxMessageSummary
{
    public long Id { get; set; }

    public long RecipientCuit { get; set; }

    public DateTime? PublishedDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public long PublishingSystemId { get; set; }

    public string PublishingSystem { get; set; } = string.Empty;

    public int StateId { get; set; }

    public string StateName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public int Priority { get; set; }

    public bool HasAttachment { get; set; }

    public string? Reference1 { get; set; }

    public string? Reference2 { get; set; }
}
