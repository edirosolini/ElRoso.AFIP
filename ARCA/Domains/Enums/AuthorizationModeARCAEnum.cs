// <copyright file="AuthorizationModeARCAEnum.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// EN: Type of ARCA authorization code being verified.
/// ES: Tipo de código de autorización de ARCA que se valida.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AuthorizationModeARCAEnum
{
    /// <summary>Código de Autorización Electrónico (default — facturación electrónica).</summary>
    [Display(Name = "CAE")]
    CAE = 0,

    /// <summary>Código de Autorización de Impresión (controlador fiscal / manual).</summary>
    [Display(Name = "CAI")]
    CAI = 1,

    /// <summary>Código de Autorización Electrónico Anticipado.</summary>
    [Display(Name = "CAEA")]
    CAEA = 2,
}
