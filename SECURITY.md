# Política de seguridad

## Versiones soportadas

| Versión       | Soporte de seguridad   |
| ------------- | ---------------------- |
| 1.x (latest)  | ✅ Sí                  |
| Pre-releases  | ✅ Sí — última preview |
| < 1.0         | ❌ No                  |

## Reportar una vulnerabilidad

**No abrir un Issue público** para reportar problemas de seguridad. Esto incluye:

- Vulnerabilidades de autenticación / autorización
- Manejo inseguro de certificados, claves privadas, tokens
- Inyección, deserialización insegura, SSRF, etc.
- Cualquier cosa que comprometa el certificado, las credenciales ARCA o los datos fiscales de un consumidor de la lib

### Cómo reportar

Mandá un email a **ezequiel@elroso.ar** con:

1. Una descripción del problema
2. Pasos para reproducirlo
3. Versión afectada
4. Impacto potencial
5. (Opcional) Tu sugerencia de fix

Vamos a:

- Confirmar recepción dentro de **72 horas hábiles**
- Validar el reporte e investigar dentro de los **7 días**
- Coordinar con vos un disclosure responsable
- Publicar un fix + advisory en GitHub Security Advisories
- Acreditar tu contribución públicamente (si así lo querés)

### Qué considerar como problema de seguridad

Lo más sensible en esta lib son:

- El manejo del **certificado X.509** y la **clave privada** del usuario
- El cache de **tokens WSAA** (DPAPI en Windows, plano en Linux/Mac)
- La firma **PKCS#7** del LoginTicketRequest
- El **NTP cache** (un attacker que controla el tiempo puede invalidar / fabricar tokens)
- Cualquier path que loggee información sensible

Si dudás si tu hallazgo es relevante, reportalo igual. Mejor false positive que false negative.

¡Gracias por ayudar a que ElRoso.ARCA sea más segura para toda la comunidad!
