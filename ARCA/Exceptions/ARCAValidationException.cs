// <copyright file="ARCAValidationException.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Exceptions;

/// <summary>
/// Thrown when the billing document request fails local FluentValidation rules.
/// Se lanza cuando el request no pasa las validaciones locales.
/// </summary>
public class ARCAValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ARCAValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.ToList().AsReadOnly();
    }
}
