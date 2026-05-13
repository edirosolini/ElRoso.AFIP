// <copyright file="ConceptTypeARCAEnum.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

[JsonConverter(typeof(StringEnumConverter))]
public enum ConceptTypeARCAEnum
{
    /// <summary>Productos.</summary>
    [Display(Name = "Productos")]
    Products = 1,

    /// <summary>Servicios.</summary>
    [Display(Name = "Servicios")]
    Services = 2,

    /// <summary>Otros (productos y servicios).</summary>
    [Display(Name = "Otros")]
    Otros = 4,
}
