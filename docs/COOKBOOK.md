# Cookbook — Pitfalls y soluciones

Cosas que en su momento me hicieron perder horas. Acá las anoto para que vos no las sufras.

> 💡 Si encontrás un pitfall que no está acá, abrí un PR. La idea es que este cookbook crezca con la comunidad.

---

## Tabla de contenidos

- [Certificados](#certificados)
- [Autenticación WSAA](#autenticación-wsaa)
- [Cache de tokens](#cache-de-tokens)
- [Cliente y Padrón](#cliente-y-padrón)
- [Códigos de error ARCA típicos](#códigos-de-error-arca-típicos)
- [Notas de Crédito y Débito](#notas-de-crédito-y-débito)
- [Factura de Exportación](#factura-de-exportación)
- [Docker y deploy](#docker-y-deploy)
- [Concurrencia y reintentos](#concurrencia-y-reintentos)

---

## Certificados

### "The certificate's CN must match the CUIT"

Cuando generás el CSR, el `CN` (Common Name) debe ser el CUIT exacto del emisor, sin guiones:

```bash
openssl req -new -key empresa.key \
  -subj "/C=AR/O=Mi Empresa/CN=20123456789/serialNumber=CUIT 20123456789" \
  -out empresa.csr
```

Si lo subiste mal al portal de ARCA, **revocalo y generá uno nuevo** — no se edita.

### "El mismo certificado funciona en homologación y producción"

**No.** Son dos certificados distintos, firmados por dos CA distintas. Subí dos CSRs separados:

- Homologación: portal con CUIT en ambiente "Testing"
- Producción: portal con CUIT en ambiente "Producción"

### "¿Puedo poner el .pfx en variables de entorno?"

Mejor montalo como archivo (volume en Docker, secret en Kubernetes). Si vas por env var, codificalo a base64:

```bash
ARCA_CERT_BASE64=$(base64 -w0 empresa.pfx)
```

Y al arrancar la app:

```csharp
var pfxBytes = Convert.FromBase64String(Environment.GetEnvironmentVariable("ARCA_CERT_BASE64")!);
var tempPath = Path.Combine(Path.GetTempPath(), "arca.pfx");
File.WriteAllBytes(tempPath, pfxBytes);
options.CertificatePath = tempPath;
```

### "El certificado se vence"

ARCA emite certificados con vencimiento de 2 años. Marcalo en el calendario. Cuando renueves:

1. Generá nuevo CSR
2. Subilo al portal
3. Descargá el nuevo `.crt`
4. Armá el nuevo `.pfx`
5. Reemplazá el archivo (rolling deploy)
6. La lib invalida el cache de tokens automáticamente porque la huella del cert cambia

---

## Autenticación WSAA

### "Error 0: Computador no autorizado a acceder al servicio"

Tu CUIT no tiene habilitado el servicio en cuestión. En el portal:

- **Administrador de Relaciones de Clave Fiscal → Adherir Servicio**
- Buscar: `Facturación Electrónica`, `wsfe`, `ws_sr_padron_a5`, etc.
- Vincular con el certificado (CSR uploadeado)

### "The TA token is invalid / expired"

Causas comunes:

1. **Tu reloj está desincronizado.** WSAA es estricto con el campo `GenerationTime`. La lib usa NTP cacheado, pero si NTP falla la lib cae a `DateTime.UtcNow`. Si tu server tiene drift > 5min, esto explota.
2. **Estás usando el TA de homologación contra producción.** Cada ambiente tiene su propio TA. La lib usa archivos separados (`ARCA_token_wsfe_20123456789.bin`).
3. **El TA expiró mientras lo usabas.** Tiene ~12 horas de vida. La lib refresca automáticamente, pero si tu request es exactamente en el borde…

### "PKCS#7 signing failed"

Si el cert se cargó bien pero falla la firma:

- Verificá que el `.pfx` tenga clave privada exportable
- Si lo generaste con `openssl pkcs12`, asegurate de pasar `-export` (no `-nokeys`)

---

## Cache de tokens

### "¿Dónde se guardan los TAs?"

En el directorio configurado en `TokenCacheDirectory` (default: `Path.GetTempPath()`). Cada combinación servicio + CUIT genera un archivo:

```
ARCA_token_wsfe_20123456789.bin
ARCA_token_ws_sr_constancia_inscripcion_20123456789.bin
ARCA_token_wsfex_20123456789.bin
```

### "Los archivos están cifrados"

- **En Windows:** sí, con DPAPI scope `CurrentUser`. Solo el mismo usuario del mismo equipo puede descifrarlo.
- **En Linux/macOS:** NO, son JSON plano. Si esto es problema, montá el directorio en una partición cifrada o usá un volumen de Docker dedicado.

### "Migré la app de un server a otro y los tokens no sirven"

Esperado en Windows (DPAPI los ata al usuario+equipo). Borrá el directorio y la próxima request va a refrescar contra WSAA. No es bug, es feature de seguridad.

### "Tengo concurrencia alta y se corrompen los archivos"

No debería: la lib usa `SemaphoreSlim` por clave para serializar escrituras/lecturas. Si ves corrupción, abrí un issue con repro.

---

## Cliente y Padrón

### "El nombre del cliente sale en blanco"

La lib consulta automáticamente el Padrón A5 cuando le pasás un `ClientRequest` con `DocumentType=CUIT`. Si el padrón devuelve vacío:

- Verificá que tu CUIT tenga adherido el servicio `ws_sr_constancia_inscripcion`
- El CUIT del cliente debe estar **inscripto** en AFIP (no de baja)
- Para consumidor final usá `DocumentType=SIN_IDENTIFICAR` y `DocumentNumber=0`

### "¿Tengo que pasar la Condición IVA del cliente?"

No — la lib la resuelve sola desde el Padrón cuando el cliente tiene CUIT. Las propiedades `Condition` y `ClientName` del `ClientRequest` son **read-only** justamente por eso.

Para consumidor final sin CUIT, la lib la fuerza a `CONSUMIDOR_FINAL` automáticamente.

---

## Códigos de error ARCA típicos

| Código | Significado | Solución habitual |
|--------|-------------|-------------------|
| 10015  | Fecha del comprobante inválida | La fecha debe estar entre hoy-10 días y hoy+10 días |
| 10016  | Fecha de servicio inválida | Para servicios, `DateOfServicesFrom <= DateOfServicesTo <= PaymentDue` |
| 10017  | Comprobante asociado inválido | Punto de venta + tipo + número de la nota debe existir y ser del mismo CUIT emisor |
| 10018  | Importes inconsistentes | `Total = AmountTax + AmountNotTax + TaxAmount + OtherTaxAmount` (validar redondeo) |
| 10048  | Cliente no inscripto en padrón | Para FA, el receptor debe ser Responsable Inscripto. Si no, usar FB |
| 10063  | CAE ya autorizado | El número de comprobante ya tiene CAE — estás re-enviando el mismo |

> **Lista completa:** [Manual del desarrollador WSFEv1](https://www.afip.gob.ar/ws/WSFEV1/manual_desarrollador_COMPG_v4_4_1.pdf) (sección "Códigos de errores").

---

## Notas de Crédito y Débito

### "Olvidé el `BillingDocumentNumberingAssociateds` y ARCA lo aceptó"

No siempre lo valida ARCA, pero **es obligatorio fiscalmente**. La lib lo valida con FluentValidation antes de mandar la request.

### "El comprobante asociado tiene fecha de hace 5 años"

ARCA permite asociar comprobantes viejos en notas — no hay límite de tiempo. Si la NC asocia una factura de 2020, está OK.

### "Una NC puede asociar comprobantes de distinto tipo?"

Sí. Una `NCA` puede asociar varias `FA`. Una `NCB` no puede asociar una `FA` (debe ser de la misma clase A/B/C).

---

## Factura de Exportación

### "¿Por qué `Items` es obligatorio solo en exportación?"

Porque WSFEXv1 requiere detalle de items, mientras que WSFEv1 acepta un solo importe total. La lib lo valida.

### "Currency = 'Dolares' pero el monto está en pesos"

`AmountTax` siempre va **en la moneda del comprobante** (no convertido). `ExchangeRate` se usa para informar la cotización al fisco — la convertibilidad la calcula AFIP.

### "Cómo armo el `BillingDocumentId`"

Es un ID único secuencial **tuyo** (no de ARCA). La lib no lo genera — tenés que llevar el contador en tu DB. Empezá en 1 e incrementá. Si dos exportaciones tienen el mismo ID, ARCA las rechaza.

---

## Docker y deploy

### "El TimeZone en Linux es UTC y los datos quedan corridos"

ARCA trabaja en UTC-3 (hora Argentina, sin DST desde 2009). En el container:

```dockerfile
ENV TZ=America/Argentina/Buenos_Aires
```

La lib internamente convierte a UTC-3 lo que necesita (la firma WSAA y el chequeo de expiración del TA), pero tu lógica de negocio (fechas de comprobante, vencimientos) **debe vivir en hora AR**.

### "Mi imagen Docker no encuentra el .pfx"

Volumen mal montado o path equivocado:

```yaml
volumes:
  - ./certs:/certs:ro                 # ojo con permisos
environment:
  Arca__CertificatePath: /certs/empresa.pfx
```

Verificá dentro del container con `ls -la /certs`.

---

## Concurrencia y reintentos

### "Dos requests concurrentes al mismo servicio refrescan el TA dos veces"

No deberían — la lib usa `SemaphoreSlim` por clave (servicio+CUIT). Solo una refresca, las demás esperan y usan el TA refrescado.

### "WSFEv1 devuelve timeout intermitente"

WSFEv1 puede tener latencia alta (>10s) en horas pico. La lib usa el timeout configurado en `SoapTimeoutSeconds` (default 30). Si tu app necesita más resilience:

- Subí `SoapTimeoutSeconds` a 60
- Implementá retry con backoff exponencial en tu lado (la lib no reintentea sola — el reintento lo dejamos al consumidor para evitar duplicar comprobantes)

### "Hice retry y emití el mismo comprobante dos veces"

Pasa. Por eso ARCA expone `FECompUltimoAutorizado` para consultar el último autorizado antes de re-emitir. Si vas a hacer retry, primero consultá. La lib hoy NO lo hace solo — está en el [roadmap de la v2.x](https://github.com/edirosolini/ElRoso.ARCA/issues).

---

¿Falta algo? [Abrí una Discussion](https://github.com/edirosolini/ElRoso.ARCA/discussions) o un PR a este archivo.
