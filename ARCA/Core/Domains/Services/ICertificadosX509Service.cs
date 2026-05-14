// <copyright file="ICertificadosX509Service.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace ElRoso.ARCA.Core;

using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Handles X.509 certificate loading and PKCS#7 message signing.
/// Maneja la carga de certificados X.509 y la firma PKCS#7 de mensajes.
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// Signs a byte array with the given certificate (PKCS#7 / CMS).
    /// Firma un array de bytes con el certificado dado (PKCS#7 / CMS).
    /// </summary>
    byte[] SignMessage(byte[] messageBytes, X509Certificate2 signingCert);

    /// <summary>
    /// Loads an X.509 certificate (with private key) from a .pfx/.p12 file.
    /// Carga un certificado X.509 (con clave privada) desde un archivo .pfx/.p12.
    /// </summary>
    X509Certificate2 LoadCertificate(string filePath, string? password);
}
