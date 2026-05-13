// <copyright file="VATConditionARCAEnum.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

[JsonConverter(typeof(StringEnumConverter))]
public enum VATConditionARCAEnum
{
    /// <summary>Consumidor final — sin responsabilidad de IVA.</summary>
    [Display(Name = "Consumidor Final")]
    CONSUMIDOR_FINAL = 5,

    /// <summary>Monotributista.</summary>
    [Display(Name = "Monotributo")]
    MONOTRIBUTO = 6,

    /// <summary>Responsable Inscripto en IVA.</summary>
    [Display(Name = "Responsable Inscripto")]
    RESPONSABLE_INSCRIPTO,
}
