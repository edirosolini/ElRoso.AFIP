# ElRoso.ARCA

Cliente .NET 9 para la facturación electrónica con **ARCA** (ex-AFIP). Soporta autenticación WSAA, emisión de comprobantes domésticos (WSFEv1) y de exportación (WSFEXv1), y validación de CUIT por padrón.

---

## Quick Start — Factura A en 5 líneas

```csharp
var response = await billingService.AuthorizeAsync(new BillingDocumentNumberingRequest
{
    BillingDocumentType = BillingDocumentTypeARCAEnum.FA,
    BillingDocumentBookPrefix = 1,
    BillingDocumentDate = DateTime.Today,
    Currency = "Pesos",
    ExchangeRate = 1,
    ConceptType = ConceptTypeARCAEnum.Products,
    AmountTax = 826.45,
    BillingDocumentNumberingTaxes = [new() { PercentageTax = "21.00", BaseAmount = 826.45, Amount = 173.55 }],
    IssuingCompany = new() { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 20123456789 },
    Client = new() { DocumentType = DocumentTypeARCAEnum.CUIT, DocumentNumber = 30987654321 },
});

Console.WriteLine(response.Result ? $"CAE: {response.CAE}" : string.Join('\n', response.Errors));
```

---

## Instalación

```bash
dotnet add package ElRoso.Arca
```

---

## Configuración

### `appsettings.json`

```json
{
  "Arca": {
    "IsProduction": false,
    "CertificatePath": "/certs/mi-empresa.pfx",
    "CertificatePassword": "",
    "TokenCacheDirectory": "/tmp/arca-tokens",
    "SoapTimeoutSeconds": 30
  }
}
```

> **Seguridad:** nunca commitees `CertificatePassword` en texto plano. Usá variables de entorno, Azure Key Vault, AWS Secrets Manager, o User Secrets en desarrollo.

### Registro en `Program.cs`

```csharp
builder.Services.AddARCAClient(options =>
{
    var cfg = builder.Configuration.GetSection("Arca");
    options.IsProduction        = cfg.GetValue<bool>("IsProduction");
    options.CertificatePath     = cfg["CertificatePath"]!;
    options.CertificatePassword = cfg["CertificatePassword"];
    options.TokenCacheDirectory = cfg["TokenCacheDirectory"] ?? Path.GetTempPath();
    options.SoapTimeoutSeconds  = cfg.GetValue<int>("SoapTimeoutSeconds", 30);
});
```

---

## Manejo de Certificados

ARCA requiere un certificado X.509 con clave privada (`.pfx` o `.p12`) para firmar los tokens de sesión (WSAA).

### Pasos para obtener el certificado

1. **Generar el par de claves** (si no tenés):

   ```bash
   openssl genrsa -out empresa.key 2048
   openssl req -new -key empresa.key -subj "/C=AR/O=Mi Empresa/CN=20123456789" -out empresa.csr
   ```

2. **Cargar el CSR en el portal de ARCA** en _Administración de Certificados Digitales_ y descargar el `.crt`.

3. **Combinar clave y certificado en un `.pfx`**:

   ```bash
   openssl pkcs12 -export -in empresa.crt -inkey empresa.key -out empresa.pfx -passout pass:miPassword
   ```

4. **Ubicar el `.pfx`** en la ruta configurada en `CertificatePath`. En contenedores Docker, montarlo como volumen:
   ```yaml
   volumes:
     - ./certs:/certs:ro
   ```

> **Importante:** El archivo `.pfx` contiene la clave privada. Nunca lo commitees al repositorio.

---

## Tipos de Comprobante Soportados

| Enum                                   | Valor ARCA | Descripción                        |
| -------------------------------------- | ---------- | ---------------------------------- |
| `FA`                                   | 1          | Factura A                          |
| `NDA` / `NCA`                          | 2 / 3      | Nota de Débito / Crédito A         |
| `FB`                                   | 6          | Factura B                          |
| `NDB` / `NCB`                          | 7 / 8      | Nota de Débito / Crédito B         |
| `FC`                                   | 11         | Factura C                          |
| `NDC` / `NCC`                          | 12 / 13    | Nota de Débito / Crédito C         |
| `InvoiceExport`                        | 19         | Factura de Exportación             |
| `DebitNoteExport` / `CreditNoteExport` | 20 / 21    | Notas Débito / Crédito Exportación |

---

## Notas de Crédito/Débito

Deben incluir los comprobantes asociados:

```csharp
new BillingDocumentNumberingRequest
{
    BillingDocumentType = BillingDocumentTypeARCAEnum.NCA,
    BillingDocumentNumberingAssociateds =
    [
        new()
        {
            BillingDocumentType       = BillingDocumentTypeARCAEnum.FA,
            BillingDocumentNumber     = 42,
            BillingDocumentBookPrefix = 1,
            BillingDocumentDate       = new DateTime(2025, 1, 15),
        }
    ],
    // resto de campos...
}
```

---

## Factura de Servicios

Para concepto `Services`, agregar rango de fechas y vencimiento:

```csharp
ConceptType        = ConceptTypeARCAEnum.Services,
DateOfServicesFrom = new DateTime(2025, 3, 1),
DateOfServicesTo   = new DateTime(2025, 3, 31),
PaymentDue         = new DateTime(2025, 4, 10),
```

---

## Factura de Exportación

```csharp
new BillingDocumentNumberingRequest
{
    BillingDocumentType = BillingDocumentTypeARCAEnum.InvoiceExport,
    BillingDocumentId   = 1,           // ID único del comprobante de exportación
    Currency            = "Dolares",
    ExchangeRate        = 1050.50,
    ConceptType         = ConceptTypeARCAEnum.Products,
    AmountTax           = 500.00,
    Client = new()
    {
        DocumentType   = DocumentTypeARCAEnum.CUIT,
        DocumentNumber = 55123456789,
        CountryId      = 123,          // código de país ARCA
        Address        = "123 Main St",
        ClientLanguage = "Inglés",
    },
    Items =
    [
        new() { ItemDescription = "Software license", Amount = 500.00 },
    ],
    // ...
}
```

---

## Manejo de Errores

La librería lanza excepciones tipadas. Catcheá lo que necesitás:

```csharp
try
{
    var response = await billingService.AuthorizeAsync(request);
}
catch (ARCAValidationException ex)
{
    // Errores de validación local (FluentValidation) — no llegó a la red
    foreach (var err in ex.Errors) Console.WriteLine(err);
}
catch (ARCAAuthException ex)
{
    // Fallo de autenticación: certificado mal, WSAA caído, token inválido
    Console.WriteLine(ex.Message);
}
catch (ARCAServiceException ex)
{
    // ARCA devolvió error desde WSFEv1/WSFEXv1/Padrón
    Console.WriteLine(ex.Message);
}
```

---

## Ambientes

| Propiedad              | Homologación (default) | Producción               |
| ---------------------- | ---------------------- | ------------------------ |
| `IsProduction = false` | `wsaahomo.afip.gov.ar` | `wsaa.afip.gov.ar`       |
| WSFEv1                 | `wswhomo.afip.gov.ar`  | `servicios1.afip.gov.ar` |
| WSFEXv1                | `wswhomo.afip.gov.ar`  | `servicios1.afip.gov.ar` |
| Padrón                 | `awshomo.afip.gov.ar`  | `aws.afip.gov.ar`        |

---

## Código QR para Impresión

```csharp
var qr = response.QRCode();
// https://www.afip.gob.ar/fe/qr/?p=eyJ2ZXIiOjEsImZl...
```

---

## Stack

- **.NET 9**
- **System.ServiceModel** (WCF cliente — SOAP)
- **FluentValidation** — validaciones de request
- **System.Security.Cryptography.Pkcs** — firma PKCS#7 para WSAA
- **System.Security.Cryptography.ProtectedData** — cifrado DPAPI de la caché de tokens (Windows)
- **Newtonsoft.Json** — serialización de la caché de tokens

---

_ElRoso.Arca — Di Rosolini Ezequiel / El Roso_
