# Roadmap

> 📌 Este documento lista features futuros de `ElRoso.ARCA`. **Para reportar bugs o sugerir un feature concreto**, abrí un [Issue](https://github.com/edirosolini/edirosolini/ElRoso.ARCA/issues/new/choose) o una [Discussion](https://github.com/edirosolini/ElRoso.ARCA/discussions).

---

## Próximo release: `v1.1.0` — Read-side de ARCA

El salto importante de la lib. La `v1.0.x` cubre emisión (bien documentado por la comunidad). La `v1.1.0` apunta a lo que **nadie resuelve bien en .NET hoy**: lectura desde ARCA.

### Features confirmados (en desarrollo)

| Feature | WS | Estado |
|---------|----|----|
| **Validación de comprobantes recibidos** | `WSCDC` (Constatación) | 🔨 En desarrollo |
| **Notificaciones DFE / e-Ventanilla** | `WSCComu` | 📋 Pendiente |

### Bloqueado por falta de WS oficial

| Feature | Razón |
|---------|-------|
| **Mis Comprobantes Recibidos (listado)** | ARCA no expone WS oficial (SOAP ni REST) para **listar** compras de un CUIT. Las soluciones comerciales hoy scrapean el portal — fuera del scope de esta lib. Si aparece WS oficial, reactivar |
| **Libro IVA Digital (presentación)** | Hoy solo se opera vía portal. Si ARCA expone WS para presentación automática, sumarlo |

---

## `v1.2.0` — Antifraude + sincronización

Backlog post-v1.1.0, priorizado por valor a PyMEs y devs.

| Feature | WS | Por qué |
|---------|----|---------|
| **Padrón de Apócrifos** | `WSAPOC` | Verificar si un emisor de factura recibida está en lista de apócrifos. ORO antifraude para compras |
| **Tablas paramétricas dinámicas** | `ws_sr_padron_a100` | Hidratar dinámicamente `DictionariesCommon` (monedas, alícuotas IVA, otros impuestos) desde ARCA. Sin más releases por cambios de tabla |
| **Códigos de actividad económica** | `ws_sr_padron_a13` | Tabla maestra de actividades AFIP — útil para autocomplete al dar de alta clientes/proveedores |

Estimación: 25 – 40 hs total.

---

## `v1.3.0` y siguientes — Por demanda comunitaria

Implementación según pedido vía Issues / Discussions:

| Feature | WS | Cuándo |
|---------|----|--------|
| Comprobantes T (turismo / tax-free) | `WSCT` | Si la comunidad lo pide o el Facturador agrega vertical hotelero |
| CAE Anticipado | `WSCAEA` | Para emisores de alto volumen — feature pro |
| Factura con detalle de ítems | `WSMTXCA` | Si hay demanda real. Hoy `WSFEv1` cubre el 99% de casos |
| Padrón A4 detallado | `ws_sr_padron_a4` | Si el `ws_sr_constancia_inscripcion` (A5) actual queda corto |
| F.931 (sueldos) | `TRABAJO_F931` | Cuando alguien quiera levantar un Facturador-RRHH |
| SIRE retenciones | `SIRE` | Para agentes de retención / contadores |

---

## `v2.0.0` (futuro lejano, breaking)

- Refactor a sub-namespaces (`ElRoso.ARCA.Core.*`, `ElRoso.ARCA.Billing.*`, `ElRoso.ARCA.Read.*`) — limpia API pública con la cantidad de features acumulada.
- Considerar extraer paquete `ElRoso.ARCA.Pro` (privado) con features avanzados — modelo Open Core.

---

## Servicios ARCA que **NO** se implementarán

Por estar fuera del scope (sector específico, aduanas, beneficios sectoriales):

`WSLPG`, `WSLSP`, `WSLCA`, `WSLTV`, `WSLUM`, `WSREMHARINA`, `WSREMAZUCAR`, `WSREMCARNE`, `WSBFE`, `WSCTA`, `WSCREATEVEP`, `WSSEG`, `WSCPE`, `WSCES`, `WSSV`, `WdiaUtiDEs`, `WGESINV`, `wgestabref`, `wConsDepFiel`, `wgestiendaslibres`, `DigDepFiel`, `WutiGOPDeclaraciones`, `wdepmovimientos`, `wEnysa`, `sud_restricciones`, `sud_contrataciones`, `wscec`, `JAZA`, `Régimen Percepción IVA`, `WSPresentaciondeDDJJ`, `AGR`, `WSTABACO`, `WSICDB`.

Si tu caso de uso requiere alguno de estos, **abrí una [Discussion](https://github.com/edirosolini/ElRoso.ARCA/discussions/new?category=ideas)** explicando el contexto. Lo revaluamos.

---

## Cómo proponer features nuevos

1. Verificar primero que el WS de ARCA existe en https://www.afip.gob.ar/ws/documentacion/catalogo.asp
2. Abrir una [Discussion](https://github.com/edirosolini/ElRoso.ARCA/discussions/new?category=ideas) describiendo el caso de uso real
3. Si hay alineación → abrir un Issue para tracking

**Lo que NO se va a implementar nunca:**

- ❌ Scraping del portal ARCA (rompe la filosofía "SOAP oficial only" y depende del frontend ajeno)
- ❌ Bypass de WSAA o usos no autorizados de los WS
- ❌ Features que requieran credenciales fuera del estándar (cert X.509 + delegación)
