// <copyright file="ItemRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Requests;

public class ItemRequest
{
    private double amount;

    public string ItemDescription { get; set; } = string.Empty;

    public double Amount { get => Math.Round(amount, 2); set => amount = Math.Round(value, 2); }
}
