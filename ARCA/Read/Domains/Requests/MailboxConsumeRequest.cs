// <copyright file="MailboxConsumeRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Read;

using ElRoso.ARCA.Core;

/// <summary>
/// EN: Request to consume (read + mark as read) a notification from ARCA's e-Ventanilla.
/// ES: Request para consumir (leer + marcar como leída) una notificación del e-Ventanilla.
/// </summary>
public class MailboxConsumeRequest
{
    public IssuingCompanyRequest IssuingCompany { get; set; } = new();

    /// <summary>ID of the message to consume (from MailboxMessageSummary.Id).</summary>
    public long MessageId { get; set; }
}
