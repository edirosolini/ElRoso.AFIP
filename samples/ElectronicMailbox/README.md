# ElectronicMailbox Sample

Lee notificaciones del **Domicilio Fiscal Electrónico (DFE) / e-Ventanilla** de ARCA usando el web service **WSCComu**.

## Qué demuestra

- Listado paginado de notificaciones del DFE
- Consumo (lectura + marca como leída) de una notificación específica
- Acceso al cuerpo completo + adjuntos del mensaje
- Configuración del URL del WS (homologación vs producción TBD por el usuario)

## Caso de uso real

Tu PyME tiene Domicilio Fiscal Electrónico habilitado en ARCA. Hoy:
- Hay que loguearse al portal cada N días para chequear si llegó algo
- Si se ignora 5 días hábiles → la notificación queda "tácitamente notificada" y empieza el plazo legal

Con `ElRoso.ARCA`:
- Tu app puede correr un job cada 1-6 horas que liste el DFE
- Para cada notificación nueva → push al usuario por email/Telegram/SignalR
- Cero riesgo de notificación tácita

## Cómo correrlo

```bash
cp appsettings.json.example appsettings.json
# Editá appsettings.json con tu cert + CUIT
dotnet run
```

Para consumir (leer + marcar como leído) la primera notificación del listado:

```bash
ARCA_Sample__ConsumeFirstUnread=true dotnet run
```

## Salida esperada

```
12:34:56 info: Listing notifications for CUIT 20123456789...
12:34:58 info: 📬 7 notifications total. Page 1/1, 7 shown:
12:34:58 info:   [9876541] 2026-05-12 (Nueva)       Vencimiento Monotributo cuatrimestre Abril
12:34:58 info:   [9876540] 2026-05-08 (Leída)       Recordatorio: presentación IIBB
12:34:58 info:   [9876520] 2026-05-01 (Leída)       Inscripción Padrón modificada
12:34:58 info: 📖 Consuming first message: [9876541] Vencimiento Monotributo...
12:34:59 info: --- BODY ---
12:34:59 info: <p>Estimado contribuyente, le recordamos que el día 22/05/2026 vence...</p>
12:34:59 info: --- END BODY ---
```

## URL de producción (importante)

ARCA solo publica el endpoint de homologación. El de producción debe configurarse explícitamente:

```json
{
  "Arca": {
    "WsccomuUrl": "https://[URL_PRODUCCION_ARCA]/ve-ws/services/veconsumer"
  }
}
```

Contactar `webservices-desa@arca.gob.ar` para confirmar el endpoint productivo de tu organismo/CUIT.

## Prerequisitos

- Servicio **`wsccomu`** adherido en el portal ARCA (Administrador de Relaciones → Adherir Servicio → "Consumir Comunicaciones de Ventanilla Electrónica")
- Tu CUIT debe tener Domicilio Fiscal Electrónico **constituido** (esto se hace una sola vez en el portal)

Más detalle: [Cookbook → WSCComu](../../docs/COOKBOOK.md)
