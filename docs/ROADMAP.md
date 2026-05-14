# Roadmap

> 📌 Este documento lista features futuros de `ElRoso.ARCA`. **Para reportar bugs o sugerir un feature concreto**, abrí un [Issue](https://github.com/edirosolini/ElRoso.ARCA/issues/new/choose) o una [Discussion](https://github.com/edirosolini/ElRoso.ARCA/discussions).

---

## Próximo release: `v1.1.0` — Features de lectura (read-side)

El salto importante de la lib. Mientras la `v1.0.x` cubre emisión (bien documentado por la comunidad), `v1.1.0` apunta a lo que **nadie resuelve bien en .NET hoy**: lectura desde ARCA.

### En investigación

| Feature | Estado | Notas |
|---------|--------|-------|
| **Notificaciones DFE / e-Ventanilla** | 🔍 Investigando | Pendiente confirmar si existe WS SOAP oficial |
| **Libro IVA Digital (RG 4597)** | 🔍 Investigando | Per RG 4597 debería tener WS para presentación |

### Bloqueado por falta de WS oficial

| Feature | Estado | Razón |
|---------|--------|-------|
| **Mis Comprobantes Recibidos** | ⏸ Postergado | ARCA no expone WS oficial (SOAP ni REST) para **listar** compras de un CUIT. Las soluciones comerciales hoy scrapean el portal — fuera del scope de esta lib. Si aparece WS oficial, reactivar |

### Posibles features adicionales (no comprometidos)

- **WSCDC (Constatación)** — validación de comprobantes individuales que tenés en mano. Útil cuando recibís una factura electrónica manualmente y querés verificar autenticidad.
- **WSCT** (Consultas Tributarias) — si tiene capacidades nuevas que valgan la pena.
- **Padrones A100** — tabla completa de parámetros (currency, taxes, etc.) — útil para sincronización.

---

## `v2.0.0` (futuro, breaking)

- Refactor a sub-namespaces (`ElRoso.ARCA.Core.*`, `ElRoso.ARCA.Billing.*`) — limpia API pública para múltiples features.
- Considerar extraer paquete `ElRoso.ARCA.Pro` (privado) con features avanzados — modelo Open Core.

---

## Cómo proponer features nuevos

1. Verificar primero que el WS de ARCA existe en https://www.afip.gob.ar/ws/documentacion/catalogo.asp
2. Abrir una [Discussion](https://github.com/edirosolini/ElRoso.ARCA/discussions/new?category=ideas) describiendo el caso de uso real
3. Si hay alineación → abrir un Issue para tracking

**Lo que NO se va a implementar:**

- ❌ Scraping del portal ARCA (rompe la filosofía "SOAP oficial only" y depende del frontend ajeno)
- ❌ Bypass de WSAA o usos no autorizados de los WS
- ❌ Features que requieran credenciales fuera del estándar (cert X.509 + delegación)
