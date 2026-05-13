// <copyright file="IssuingCompanyRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Requests;

using ElRoso.ARCA.Domains.Enums;

public class IssuingCompanyRequest
{
    public DocumentTypeARCAEnum DocumentType { get; set; }

    public long DocumentNumber { get; set; }

    public VATConditionARCAEnum VATCondition { get; set; }
}
