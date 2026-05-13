// <copyright file="BillingDocumentNumberingOtherTaxRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Requests;

public class BillingDocumentNumberingOtherTaxRequest
{
    private double baseAmount;
    private double percentageTax;
    private double amount;

    public string Description { get; set; } = string.Empty;

    public double BaseAmount { get => Math.Round(baseAmount, 2); set => baseAmount = Math.Round(value, 2); }

    public double PercentageTax { get => Math.Round(percentageTax, 2); set => percentageTax = Math.Round(value, 2); }

    public double Amount { get => Math.Round(amount, 2); set => amount = Math.Round(value, 2); }
}
