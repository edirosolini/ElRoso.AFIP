// <copyright file="LoginTicketResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Core;

public class LoginTicketResponse
{
    public DateTime ExpirationTime { get; set; }

    public string Sign { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}
