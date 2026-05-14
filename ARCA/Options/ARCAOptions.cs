// <copyright file="ARCAOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace ElRoso.ARCA.Options;

/// <summary>
/// Configuration options for the ARCA (ex-AFIP) client library.
/// Opciones de configuración para la librería cliente de ARCA (ex-AFIP).
/// </summary>
public class ARCAOptions
{
    /// <summary>
    /// Use production endpoints. Default: false (homologation/testing).
    /// Usar endpoints de producción. Por defecto: false (homologación).
    /// </summary>
    public bool IsProduction { get; set; } = false;

    /// <summary>
    /// Absolute path to the X.509 certificate file (.pfx or .p12).
    /// Ruta absoluta al archivo de certificado X.509 (.pfx o .p12).
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Certificate password. Null if the certificate has no password.
    /// Contraseña del certificado. Null si no tiene contraseña.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Directory where login ticket cache files are stored.
    /// Defaults to the system temp folder.
    /// Directorio donde se almacenan los archivos de caché de tokens.
    /// </summary>
    public string TokenCacheDirectory { get; set; } = Path.GetTempPath();

    /// <summary>
    /// SOAP timeout in seconds. Default: 30.
    /// Timeout de las llamadas SOAP en segundos.
    /// </summary>
    public int SoapTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// EN: WSCComu (e-Ventanilla / DFE) endpoint URL. Defaults to the homologation endpoint.
    /// ARCA has not publicly listed the production URL for this service — set this property
    /// explicitly to the production URL when you go live. Contact webservices-desa@arca.gob.ar
    /// if unsure.
    /// ES: URL del WSCComu. Default es homologación. ARCA no publica la URL de producción —
    /// configurar explícitamente al pasar a producción.
    /// </summary>
    public string WsccomuUrl { get; set; } = "https://stable-middleware-tecno-ext.afip.gob.ar/ve-ws/services/veconsumer";

    // --- Internal resolved URLs (set by SetIsProduction) ---

    internal string WsaaUrl => IsProduction
        ? "https://wsaa.afip.gov.ar/ws/services/LoginCms"
        : "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

    internal string WsfeUrl => IsProduction
        ? "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
        : "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";

    internal string WsfexUrl => IsProduction
        ? "https://servicios1.afip.gov.ar/WSFEXv1/service.asmx"
        : "https://wswhomo.afip.gov.ar/WSFEXv1/service.asmx";

    internal string PadronUrl => IsProduction
        ? "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5"
        : "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5";

    internal string WscdcUrl => IsProduction
        ? "https://servicios1.afip.gov.ar/WSCDC/service.asmx"
        : "https://wswhomo.afip.gov.ar/WSCDC/service.asmx";
}
