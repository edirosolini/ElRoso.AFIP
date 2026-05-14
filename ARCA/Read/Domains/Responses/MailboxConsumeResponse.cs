// <copyright file="MailboxConsumeResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Read;

/// <summary>
/// EN: Full message obtained after consuming a notification (marks it as read).
/// ES: Mensaje completo obtenido al consumir una notificación (la marca como leída).
/// </summary>
public class MailboxConsumeResponse
{
    public bool Success { get; set; }

    public MailboxMessage? Message { get; set; }

    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// EN: Full notification with body and attachment metadata.
/// ES: Notificación completa con cuerpo y metadata de adjuntos.
/// </summary>
public class MailboxMessage
{
    public long Id { get; set; }

    public long RecipientCuit { get; set; }

    public DateTime? PublishedDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public string PublishingSystem { get; set; } = string.Empty;

    public int StateId { get; set; }

    public string StateName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    /// <summary>Full message body (HTML or plain text). / Cuerpo completo del mensaje.</summary>
    public string Body { get; set; } = string.Empty;

    public int Priority { get; set; }

    public bool HasAttachment { get; set; }

    public List<MailboxAttachment> Attachments { get; set; } = [];
}

public class MailboxAttachment
{
    public string FileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    /// <summary>Attachment bytes (base64-decoded). / Bytes del adjunto (base64-decodificado).</summary>
    public byte[] Content { get; set; } = [];
}
