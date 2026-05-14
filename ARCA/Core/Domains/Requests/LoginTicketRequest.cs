// <copyright file="LoginTicketRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Core;

/// <summary>
/// Internal WSAA request state. Not part of the public API.
/// Estado interno del request al WSAA. No es parte de la API pública.
/// </summary>
internal sealed class LoginTicketRequest
{
    public string XmlTemplate { get; } =
        "<loginTicketRequest>" +
          "<header>" +
            "<uniqueId></uniqueId>" +
            "<generationTime></generationTime>" +
            "<expirationTime></expirationTime>" +
          "</header>" +
          "<service></service>" +
        "</loginTicketRequest>";

    public string Service { get; set; } = string.Empty;
}
