// <copyright file="ARCAServiceException.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Exceptions;

/// <summary>
/// Thrown when ARCA web services (WSFEv1, WSFEXv1, Padron) return an error code.
/// Se lanza cuando los web services de ARCA devuelven un código de error.
/// </summary>
public class ARCAServiceException : Exception
{
    public int? ErrorCode { get; }

    public ARCAServiceException(string message) : base(message) { }

    public ARCAServiceException(string message, Exception inner) : base(message, inner) { }

    public ARCAServiceException(int errorCode, string errorMessage)
        : base($"ARCA service error {errorCode}: {errorMessage}")
    {
        ErrorCode = errorCode;
    }
}
