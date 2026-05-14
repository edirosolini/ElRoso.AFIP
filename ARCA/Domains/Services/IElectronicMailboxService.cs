// <copyright file="IElectronicMailboxService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Services;

using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Responses;

/// <summary>
/// EN: Reads notifications from ARCA's e-Ventanilla (Domicilio Fiscal Electrónico / DFE).
/// Replaces the need to log in to the ARCA portal to check the inbox.
/// ES: Lee notificaciones del e-Ventanilla de ARCA (Domicilio Fiscal Electrónico / DFE).
/// Reemplaza la necesidad de loguearse al portal de ARCA para chequear la bandeja.
/// </summary>
public interface IElectronicMailboxService
{
    /// <summary>
    /// EN: List notifications matching the given filter. Paginated.
    /// ES: Lista notificaciones que matcheen el filtro. Paginado.
    /// </summary>
    Task<MailboxQueryResponse> ListAsync(MailboxQueryRequest request, CancellationToken ct = default);

    /// <summary>
    /// EN: Consume (read + mark as read) a notification. Returns the full message with body and attachments.
    /// ES: Consume (lee + marca como leída) una notificación. Devuelve el mensaje completo con cuerpo y adjuntos.
    /// </summary>
    Task<MailboxConsumeResponse> ConsumeAsync(MailboxConsumeRequest request, CancellationToken ct = default);
}
