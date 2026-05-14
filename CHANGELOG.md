# Changelog

Todos los cambios notables a este proyecto se documentan acá.

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

### Added

- Suite de tests xUnit + Moq + FluentAssertions con Coverlet (~25% inicial)
- Workflow CI en GitHub Actions (build + test + coverage gate + verify pack)
- Workflow Release en GitHub Actions (publica a NuGet.org en tag SemVer)
- `dependabot.yml` con updates semanales de NuGet y mensuales de GitHub Actions
- `FUNDING.yml` con GitHub Sponsors + Cafecito
- `LICENSE` MIT
- `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`
- SourceLink para debug into source
- Symbols `.snupkg` publicados con el `.nupkg`
- README packaged dentro del nupkg (se muestra en NuGet.org)

### Changed

- `RepositoryUrl` corregido a `https://github.com/edirosolini/ElRoso.ARCA`
- Metadata del package expandida (Title, PackageReadmeFile, ContinuousIntegrationBuild, etc.)
- Eliminado workflow legacy `publish.yml` que publicaba a GitHub Packages en cada push

### Security

- Reset del history git para purgar claves privadas que estaban en commits anteriores. **Si tenías un certificado anterior asociado a este proyecto, revocalo en ARCA.**

## [1.0.0-preview.1]

Primer release público en NuGet.org.

### Added

- Cliente .NET 9 para facturación electrónica con ARCA (ex-AFIP)
- Autenticación WSAA (LoginTicket via PKCS#7 + NTP cache + DPAPI token cache)
- Emisión de comprobantes domésticos (WSFEv1): FA, FB, FC, NDA/NCA, NDB/NCB, NDC/NCC
- Emisión de comprobantes de exportación (WSFEXv1): Factura, Nota de Débito, Nota de Crédito
- Resolución de cliente vía Padrón (ws_sr_constancia_inscripcion)
- Generación de QR code para impresión en comprobante
- Excepciones tipadas: `ARCAAuthException`, `ARCAServiceException`, `ARCAValidationException`
- Configuración via `ARCAOptions` con switch homologación / producción
- Extensión `AddARCAClient()` para DI con Microsoft.Extensions.DependencyInjection
- Validación FluentValidation de todos los inputs
- Soporte para Concepto Productos / Servicios / Otros
- Soporte para impuestos múltiples (IVA + otros)
