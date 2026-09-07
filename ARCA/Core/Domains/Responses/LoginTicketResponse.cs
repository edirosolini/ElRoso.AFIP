// <copyright file="LoginTicketResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Core;

public class LoginTicketResponse
{
    /// <summary>
    /// EN: Ticket expiration as a <b>UTC</b> instant (<c>Kind = Utc</c>), whatever offset WSAA
    ///     wrote in the XML. Compare it against <see cref="DateTime.UtcNow"/>, never against a
    ///     local clock.
    /// ES: Vencimiento del ticket como instante <b>UTC</b> (<c>Kind = Utc</c>), sea cual sea el
    ///     offset que haya escrito WSAA en el XML. Compararlo contra <see cref="DateTime.UtcNow"/>,
    ///     nunca contra un reloj local.
    /// </summary>
    public DateTime ExpirationTime { get; set; }

    public string Sign { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}
