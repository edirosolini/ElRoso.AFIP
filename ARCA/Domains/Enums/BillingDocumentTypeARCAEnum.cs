// <copyright file="BillingDocumentTypeARCAEnum.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

[JsonConverter(typeof(StringEnumConverter))]
public enum BillingDocumentTypeARCAEnum
{
    /// <summary>Factura A.</summary>
    [Display(Name = "Facturas A")]
    FA = 1,

    /// <summary>Nota de Débito A.</summary>
    [Display(Name = "Notas de Débito A")]
    NDA = 2,

    /// <summary>Nota de Crédito A.</summary>
    [Display(Name = "Notas de Crédito A")]
    NCA = 3,

    /// <summary>Factura B.</summary>
    [Display(Name = "Facturas B")]
    FB = 6,

    /// <summary>Nota de Débito B.</summary>
    [Display(Name = "Notas de Débito B")]
    NDB = 7,

    /// <summary>Nota de Crédito B.</summary>
    [Display(Name = "Notas de Crédito B")]
    NCB = 8,

    /// <summary>Factura C.</summary>
    [Display(Name = "Facturas C")]
    FC = 11,

    /// <summary>Nota de Débito C.</summary>
    [Display(Name = "Notas de Débito C")]
    NDC = 12,

    /// <summary>Nota de Crédito C.</summary>
    [Display(Name = "Notas de Crédito C")]
    NCC = 13,

    /// <summary>Factura de Exportación.</summary>
    [Display(Name = "Facturas de Exportación")]
    InvoiceExport = 19,

    /// <summary>Nota de Débito por Operaciones con el Exterior.</summary>
    [Display(Name = "Notas de Débito por Operaciones con el Exterior")]
    DebitNoteExport = 20,

    /// <summary>Nota de Crédito por Operaciones con el Exterior.</summary>
    [Display(Name = "Notas de Crédito por Operaciones con el Exterior")]
    CreditNoteExport = 21,

    /// <summary>Remito R.</summary>
    [Display(Name = "Remitos R")]
    Remittances = 91,
}
