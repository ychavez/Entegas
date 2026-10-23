# Corrección de hallazgos de seguridad H1, H2 y H3

**Tipo:** técnica (habilitador de seguridad). **Estado:** lista para planificar. **Estado del código:** existente, por comprobar.

Esta spec sale de la auditoría de seguridad del 22 de octubre de 2026 sobre `src/CursoCursor.Api`. El proceso común está en [el protocolo multiagente](../flujo-multiagente.md).

## Historia de usuario u objetivo

Como responsable del proyecto, quiero eliminar el secreto expuesto en la configuración de Cursor, proteger los endpoints por defecto y exigir transporte cifrado fuera de desarrollo, para que la API pueda crecer (reservas, cancelación) sin heredar estos riesgos.

## Alcance y exclusiones

- Incluye:
  - **H1 (alta, confirmada):** quitar el token de GitHub escrito en texto plano en `.cursor/mcp.json` y leerlo desde una variable de entorno.
  - **H2 (media, potencial):** registrar autenticación y autorización con una política de reserva (fallback) que exija usuario autenticado; dejar públicos de forma explícita `/health`, el catálogo y OpenAPI en Development.
  - **H3 (media, potencial):** HSTS y redirección a HTTPS fuera de Development, y perfil `https` en `launchSettings.json`.
- Fuera de alcance: hallazgos H4 a H6, emisión de tokens o pantalla de inicio de sesión, roles y permisos finos, cambios de rutas o campos JSON del catálogo, despliegue.

## Comportamiento y contrato

### H1. Secreto del servidor MCP

- `.cursor/mcp.json` no contiene ningún valor de token. El encabezado queda como `"Authorization": "Bearer ${env:GITHUB_PAT}"`.
- El archivo sigue en `.gitignore`. Se agrega `.cursor/mcp.example.json` (versionado) con la misma forma y sin secreto.
- **Paso humano obligatorio, antes de cerrar la spec:** revocar el token actual en GitHub (Settings, Developer settings, Personal access tokens), crear uno fine-grained con alcance mínimo y expiración, y definir `GITHUB_PAT` como variable de usuario en Windows. Ningún agente ve, copia ni registra el valor.

### H2. Autenticación y autorización por defecto

- `Program.cs` registra `AddAuthentication` con esquema JWT Bearer y `AddAuthorizationBuilder().SetFallbackPolicy(...)` con `RequireAuthenticatedUser()`; el pipeline llama a `UseAuthentication()` y `UseAuthorization()` antes de mapear endpoints.
- Se marcan con `.AllowAnonymous()`: `GET /health`, `GET /api/workshops`, `GET /api/workshops/{id:guid}` y `MapOpenApi()` en Development.
- Cualquier endpoint nuevo sin `.AllowAnonymous()` responde `401` con ProblemDetails si no trae token válido.
- Se conservan rutas, campos JSON, códigos 200/404 y el `code` `workshop_not_found` del catálogo.
- En desarrollo los tokens se generan con `dotnet user-jwts create`; la configuración del emisor y audiencia vive en `appsettings.Development.json` o user-secrets, nunca con claves en archivos versionados.

### H3. Transporte cifrado

- Fuera de Development: `app.UseHsts()` y `app.UseHttpsRedirection()`.
- `launchSettings.json` agrega un perfil `https` con `https://localhost:7080;http://localhost:5080`; el perfil `http` actual se conserva.
- `scripts/run-smoke.ps1` arranca en Production sobre `http://127.0.0.1:<puerto>` sin puerto HTTPS: la redirección no debe romper la prueba de humo (ver riesgos).

## Criterios de aceptación

| ID | Dado / cuando | Resultado observable |
|---|---|---|
| SEG-01 | Se busca el patrón `github_pat_` o `ghp_` en todo el árbol de trabajo | Cero coincidencias. |
| SEG-02 | Se abre `.cursor/mcp.json` | El encabezado usa `${env:GITHUB_PAT}`; el archivo sigue ignorado por git y existe `.cursor/mcp.example.json` sin secreto. |
| SEG-03 | El humano revocó el token anterior | Confirmación del humano registrada en la entrega; el MCP de GitHub funciona con el token nuevo. |
| SEG-04 | Petición anónima a `GET /health` | `200` con `{"status":"ok"}`. |
| SEG-05 | Petición anónima a `GET /api/workshops` y a `GET /api/workshops/{id}` existente | `200` con el mismo contrato JSON de hoy. |
| SEG-06 | Petición anónima a `GET /api/workshops/{id}` inexistente | `404` con `code` `workshop_not_found`, sin cambios. |
| SEG-07 | Endpoint de prueba sin `AllowAnonymous`, registrado solo en el host de pruebas, sin token | `401` con ProblemDetails. |
| SEG-08 | El mismo endpoint con un token válido de prueba | `200`. |
| SEG-09 | Petición HTTP en entorno Production con puerto HTTPS configurado | Redirección `307` o `308` a HTTPS y encabezado `Strict-Transport-Security` en la respuesta HTTPS. |
| SEG-10 | Entorno Development | Sin HSTS; OpenAPI accesible de forma anónima. |
| SEG-11 | Se ejecuta `scripts/run-smoke.ps1` | Mismos resultados que antes: health 200, OpenAPI 404, catálogo 200. |
| SEG-12 | Se ejecutan `dotnet test` y la auditoría de NuGet del CI | Todo en verde; ningún paquete vulnerable nuevo. |

## Dependencias y contexto técnico

- Leer: `src/CursoCursor.Api/Program.cs`, `Features/Workshops/WorkshopEndpoints.cs`, `Properties/launchSettings.json`, `appsettings*.json`, `.cursor/mcp.json` (solo la estructura, nunca el valor), `tests/CursoCursor.Api.Tests/*`, `scripts/run-smoke.ps1`, `.github/workflows/security.yml`.
- Paquete nuevo necesario para H2: `Microsoft.AspNetCore.Authentication.JwtBearer` en versión exacta de la línea 10.0. La regla del curso pide no agregar paquetes sin que la tarea los requiera; esta tarea lo requiere.
- `docs/specs/README.md` declara la autenticación fuera del alcance del curso; esta spec la incorpora como habilitador técnico y debe agregarse al catálogo.
- Observado en el código actual: solo existen `/health` y los dos GET del catálogo. Las reservas y cancelaciones de las specs 03 y 04 aún no están implementadas en esta copia, así que la política de reserva las protegerá cuando se agreguen.

## Verificación

- Propuesto: pruebas de integración con `WebApplicationFactory<Program>` para SEG-04 a SEG-10, usando un esquema de autenticación de prueba o un token generado en la prueba.
- Propuesto: `rg -n "github_pat_|ghp_" .` para SEG-01 (sin imprimir coincidencias en la entrega; reportar solo el conteo).
- Propuesto: `dotnet test CursoCursor.slnx`, `scripts/run-smoke.ps1` y `dotnet list CursoCursor.slnx package --vulnerable --include-transitive`.
- Separar en la entrega lo propuesto de lo ejecutado, con comando y resultado real.

## Decisiones abiertas

1. ¿Se acepta JWT Bearer con `dotnet user-jwts` en desarrollo, o se prefiere otro esquema (por ejemplo API key) para el curso?
2. ¿El catálogo debe seguir siendo público? Esta spec asume que sí.
3. ¿Se actualiza la línea de "no incluye autenticación" en `docs/specs/README.md`?

El coordinador debe resolverlas con el humano antes de implementar.

## Reparto y orden de integración

| Responsable | Archivos | Puede ir en paralelo |
|---|---|---|
| Coordinador | `.cursor/mcp.json`, `.cursor/mcp.example.json`, `CursoCursor.Api.csproj` (paquete), `Properties/launchSettings.json`, `appsettings.Development.json`, `docs/specs/README.md` | Va primero: el paquete y la configuración preceden al código. |
| Implementador | `src/CursoCursor.Api/Program.cs`, `Features/Workshops/WorkshopEndpoints.cs` | Sí, con QA, después del coordinador. |
| QA | `tests/CursoCursor.Api.Tests/SecurityTests.cs` (nuevo) | Sí, con el implementador. |
| Revisor | Solo lectura de todo el cambio | Al final, después de integrar y compilar. |
| Humano | Revocar y regenerar el token, definir `GITHUB_PAT` | Cuanto antes; no bloquea H2 ni H3. |

Riesgos:
- `UseHttpsRedirection` sin puerto HTTPS conocido solo registra una advertencia; comprobar que la prueba de humo en Production siga en verde (SEG-11).
- La política de reserva también afecta a `UseStatusCodePages` y a endpoints de OpenAPI: verificar que no devuelvan 401 por accidente.
- Ningún agente debe abrir el valor del token, copiarlo en logs ni incluirlo en commits.

## Prompt de entrada

```text
Desarrolla docs/specs/09-seguridad-h1-h3.md siguiendo docs/flujo-multiagente.md.
Actúa como coordinador y usa planificador, implementador, qa y revisor de
.cursor/agents/. Presenta el plan para mi revisión y resuelve conmigo las
decisiones abiertas antes de editar. Tras aceptarlo, completa el incremento
con evidencia por criterio SEG-01 a SEG-12. Nunca leas ni muestres el valor
del token de .cursor/mcp.json. No crees commits, publiques ni fusiones.
```
