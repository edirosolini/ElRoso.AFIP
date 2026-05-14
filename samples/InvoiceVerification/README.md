# InvoiceVerification Sample

Verifica una factura **recibida** de un proveedor contra ARCA usando el web service **WSCDC** (Constatación de Comprobantes).

## Qué demuestra

- Cómo validar que un CAE (o CAI / CAEA) impreso en un comprobante recibido es **auténtico y vigente** en ARCA
- Diferencia entre los tres modos de autorización: CAE / CAI / CAEA
- Manejo de respuestas: `Resultado = "A"` (autorizado) vs `"R"` (rechazado) + observaciones
- Manejo de excepciones tipadas

## Caso de uso real

Tu proveedor te manda un PDF de Factura A. Antes de pagar:
1. Leés del PDF: CUIT emisor, tipo (FA), punto de venta, número, fecha, importe total, CAE
2. Llamás a `IInvoiceVerificationService.VerifyAsync(...)` con esos datos
3. Si `IsAuthorized == true` → factura legítima, podés pagar
4. Si `IsAuthorized == false` → factura sospechosa, **revisá observaciones antes de pagar**

## Cómo correrlo

```bash
cp appsettings.json.example appsettings.json
# Editá appsettings.json con tu cert + datos del comprobante a validar
dotnet run
```

## Variables de entorno (alternativa al appsettings.json)

```bash
ARCA_Arca__CertificatePath=/tmp/cert.pfx \
ARCA_Sample__IssuerCuit=20111111111 \
ARCA_Sample__AuthorizationCode=75999... \
dotnet run
```

## Salida esperada (factura válida)

```
12:34:56 info: ElRoso.ARCA.Samples.InvoiceVerification.Program[0] Verifying voucher FA 1-42 from issuer 20123456789...
12:34:58 info: ElRoso.ARCA.Samples.InvoiceVerification.Program[0] ✅ Voucher is AUTHORIZED by ARCA (Resultado: A, Processed: 2026-05-14)
```

## Salida esperada (factura rechazada)

```
12:34:56 info: ElRoso.ARCA.Samples.InvoiceVerification.Program[0] Verifying voucher FA 1-99999 from issuer 20123456789...
12:34:58 warn: ElRoso.ARCA.Samples.InvoiceVerification.Program[0] ❌ Voucher NOT authorized (Resultado: R)
12:34:58 warn: ElRoso.ARCA.Samples.InvoiceVerification.Program[0]   Observation: 10048: El CAE no corresponde al comprobante
```

## Prerequisitos

- Servicio `wscdc` adherido a tu CUIT en el portal ARCA
- Certificado de homologación válido (mismo cert que para WSFEv1, distinto WSAA token)

Más info: [Cookbook → WSCDC](../../docs/COOKBOOK.md)
