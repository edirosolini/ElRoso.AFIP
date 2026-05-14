// <copyright file="ARCAAuthException.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Core;

/// <summary>
/// Thrown when WSAA authentication fails (certificate, signing, or token retrieval).
/// Se lanza cuando falla la autenticación contra el WSAA.
/// </summary>
public class ARCAAuthException : Exception
{
    public ARCAAuthException(string message) : base(message) { }

    public ARCAAuthException(string message, Exception inner) : base(message, inner) { }
}
