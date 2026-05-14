// <copyright file="IElectronicMailboxOperations.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Services.Soap;

using ElRoso.ARCA.Domains.Requests;
using ElRoso.ARCA.Domains.Responses;

/// <summary>
/// EN: Thin wrapper over the ARCA WSCComu SOAP client (e-Ventanilla / DFE notifications).
/// ES: Wrapper fino sobre el cliente SOAP del WSCComu de ARCA.
/// </summary>
internal interface IElectronicMailboxOperations
{
    Task<MailboxQueryResponse> ListAsync(
        string sign,
        string token,
        long cuitRepresentada,
        MailboxQueryRequest request,
        CancellationToken ct);

    Task<MailboxConsumeResponse> ConsumeAsync(
        string sign,
        string token,
        long cuitRepresentada,
        long messageId,
        CancellationToken ct);
}
