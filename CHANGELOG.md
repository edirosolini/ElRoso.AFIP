# Changelog

Todos los cambios notables a este proyecto se documentan acá.

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

## [1.0.0-preview.1] — 2026-05-13

Primer release público en NuGet.org. ¡Bienvenidos! 🇦🇷

### Funcionalidad de la librería

- Cliente .NET 9 para facturación electrónica con ARCA (ex-AFIP)
- Autenticación **WSAA** completa: firma PKCS#7, NTP cacheado, cache de tokens en disco cifrado con DPAPI (Windows) / JSON plano (Linux/macOS)
- Emisión de comprobantes **domésticos (WSFEv1)**: FA, FB, FC + Notas de Débito y Crédito A/B/C
- Emisión de comprobantes de **exportación (WSFEXv1)**: Factura, Nota de Débito, Nota de Crédito
- Resolución automática del cliente vía **Padrón A5** (ws_sr_constancia_inscripcion)
- Generación de **URL del QR** oficial de ARCA para imprimir en el comprobante
- Excepciones tipadas: `ARCAAuthException`, `ARCAServiceException`, `ARCAValidationException`
- Configuración via `ARCAOptions` con switch homologación / producción
- Extensión `AddARCAClient()` para DI con Microsoft.Extensions.DependencyInjection
- Validación FluentValidation completa de los inputs
- Soporte para Concepto Productos / Servicios / Otros
- Soporte para impuestos múltiples (IVA + otros impuestos: nacionales, provinciales, municipales, internos)
- Soporte para múltiples monedas (Pesos, Dolares, Euro)

### Infraestructura y publicación

- Suite de tests xUnit + Moq + FluentAssertions 7.x + Coverlet
- Cobertura inicial 25% (Validators, Dictionaries, Options, DTOs, Exceptions al 100%)
- Workflow `ci.yml`: build + test + coverage gate ≥ 25% + verify NuGet pack
- Workflow `release.yml`: publica `.nupkg` + `.snupkg` a NuGet.org en tag SemVer y crea GitHub Release con notas auto-generadas
- Dependabot configurado con grouping minor/patch + bloqueos para FluentAssertions v8+ (licencia comercial) y bumps majors riesgosos
- `LICENSE` MIT
- `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md` (vulnerability reporting)
- README profesional con badges + ToC + recetario por escenario
- `docs/COOKBOOK.md` con pitfalls comunes de ARCA (certificados, WSAA, cache de tokens, códigos de error, Docker, concurrencia)
- Sample runnable `samples/QuickStart` que emite una FA en homologación
- SourceLink habilitado — debug into source de la lib desde el consumidor
- Symbols `.snupkg` publicados junto al `.nupkg`
- README packaged dentro del nupkg (se muestra en NuGet.org)
- Sponsors: GitHub Sponsors + [Cafecito](https://cafecito.app/edirosolini)

### Seguridad

- Reset del git history para purgar claves privadas que habían quedado en commits iniciales. Si tenés un fork anterior a este release, **borralo y volvé a clonar**.
