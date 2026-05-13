// <copyright file="DocumentTypeARCAEnum.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Domains.Enums
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.ComponentModel.DataAnnotations;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum DocumentTypeARCAEnum
    {
        /// <summary>C.U.I.T. — Clave Única de Identificación Tributaria.</summary>
        [Display(Name = "C.U.I.T.")]
        CUIT = 80,

        /// <summary>D.N.I. — Documento Nacional de Identidad.</summary>
        [Display(Name = "D.N.I.")]
        DNI = 96,

        /// <summary>Sin identificar (consumidor final sin documento).</summary>
        [Display(Name = "Sin Identificar")]
        SIN_IDENTIFICAR = 99,
    }
}
