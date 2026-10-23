# 01 Crear la solución y la API de talleres

**Tipo:** spec técnica de creación. **Estado:** lista para planificar; la estructura ya existe en la copia del curso. **Dependencia:** decisiones de [visión y alcance](00-vision-y-alcance.md) revisadas; ninguna funcionalidad de código previa.

## Objetivo

Como equipo que construye la API de talleres, queremos una solución .NET que arranque, exponga su estado y pueda probarse por HTTP, para desarrollar los casos de uso sobre una base verificable.

Esta spec permite comenzar sin código. En un proyecto existente, el coordinador compara los criterios con lo que encuentra y conserva las piezas correctas. No vuelve a generar proyectos sobre archivos existentes.

## Alcance y exclusiones

Crear `CursoCursor.slnx`, una Minimal API `Talleres.Api`, pruebas xUnit, configuración local y convenciones básicas. Incluye `/health`, OpenAPI y Swagger UI; catálogo y reservas llegan en los incrementos siguientes. No incorpora controladores MVC, frontend de negocio, base de datos, autenticación, Docker, despliegue ni agentes dentro de la aplicación.

La organización se define en el [ADR 0001](../docs/adr/0001-estructura-del-proyecto.md). Si se solicita únicamente la estructura inicial, crear el esqueleto y comprobar compilación; dejar explícitamente pendientes el host completo, Swagger y las pruebas funcionales. Swagger UI aún no está implementado en la copia base del curso.

## Entrada para empezar desde cero

Abrir una carpeta nueva destinada al proyecto con el contexto de desarrollo: este archivo, `docs/flujo-multiagente.md`, `.cursor/rules/curso.mdc` y los cuatro roles de `.cursor/agents/`. Pueden copiarse del paquete de specs. Esos archivos son instrucciones para los agentes, no una API ya creada. Un README ausente se crea durante este incremento; el código y los tests ausentes son el estado esperado.

Aplicar el [protocolo multiagente](../docs/flujo-multiagente.md). El coordinador tiene la propiedad explícita del esqueleto y de los archivos de raíz; termina esa tarea antes de abrir la escritura paralela de `src/` y `tests/`.

## Contrato técnico

| Pieza | Resultado requerido |
|---|---|
| Solución | `CursoCursor.slnx` incluye `src/Talleres.Api/Talleres.Api.csproj` y `tests/Talleres.Api.Tests/Talleres.Api.Tests.csproj`. |
| API | `Microsoft.NET.Sdk.Web`, `net10.0`, nullable e implicit usings activados. Minimal APIs con `WebApplication`. |
| SDK | `global.json`: versión `10.0.100`, `rollForward: latestFeature`, `allowPrerelease: false`, como en la base del curso. |
| Paquete API | `Microsoft.AspNetCore.OpenApi` versión `10.0.13`, fijada por este proyecto. No significa «la última versión». |
| Interfaz OpenAPI | Agregar `Swashbuckle.AspNetCore.SwaggerUI`, comprobar compatibilidad con .NET 10 y fijar una versión explícita; usar `/openapi/v1.json` como contrato, sin otro generador OpenAPI. |
| Pruebas | `net10.0`; referencia al proyecto de API; `Microsoft.AspNetCore.Mvc.Testing` `10.0.13`, `Microsoft.NET.Test.Sdk` `17.14.1`, `xunit` `2.9.3`, `xunit.runner.visualstudio` `3.1.4`, `coverlet.collector` `6.0.4`; using global de Xunit. |
| Host de pruebas | `public partial class Program` accesible a `WebApplicationFactory<Program>`; cada escenario dispone de su propio host. |
| Ejecución local | Perfil `http`, `http://localhost:5080`, entorno Development, sin abrir navegador automáticamente. |
| Errores | `AddProblemDetails`, `UseExceptionHandler` y `UseStatusCodePages`; errores de enlace devuelven 400 también en Development con `ThrowOnBadRequest=false`. |
| Validación | Registrar `AddValidation` para los futuros DTO de reservas; las reglas concretas pertenecen a la spec de reservas. |
| Convenciones | `.editorconfig` UTF-8, espacios y salto final; `.gitignore` excluye `bin`, `obj`, `.vs`, `TestResults`, `.build`, logs y secretos locales. |

No registrar `ReservationStore` ni mapear extensiones de talleres antes de crearlas. Retirar el endpoint de ejemplo de clima si la plantilla lo genera. La app de esta etapa debe compilar por sí sola.

### Contrato HTTP inicial

| Petición | Respuesta |
|---|---|
| `GET /health` | 200 `application/json`, `{"status":"ok"}`. No comprueba dependencias externas. |
| `GET /openapi/v1.json` en Development | 200; documento OpenAPI JSON que contiene `/health`. |
| `GET /openapi/v1.json` en Production | 404 ProblemDetails. |
| `GET /swagger` en Development | Acceso a Swagger UI; puede redirigir a `/swagger/index.html`, que devuelve 200 HTML. La interfaz carga `/openapi/v1.json`. |
| `GET /swagger` y `/swagger/index.html` en Production | 404; tampoco se sirven los recursos de la interfaz. |
| `GET /ruta-inexistente` | 404 `application/problem+json`, `status:404`, título no vacío, sin stack trace. |

Configurar Swagger UI dentro de la misma condición Development que `MapOpenApi`. La URL local será `http://localhost:5080/swagger`; permite ejecutar `/health` desde Try it out. Los ejemplos posteriores usarán el mismo puerto y JSON camelCase.

## Creación reproducible

El coordinador ejecuta estos comandos únicamente dentro de la carpeta destino y solo para piezas que aún no existan. Cada comando debe terminar correctamente antes de continuar. No usar `--force`.

```powershell
dotnet --version
dotnet new sln --name CursoCursor --format slnx
dotnet new webapi --name Talleres.Api --output src/Talleres.Api --framework net10.0 --no-restore
dotnet new xunit --name Talleres.Api.Tests --output tests/Talleres.Api.Tests --framework net10.0 --no-restore
dotnet sln CursoCursor.slnx add src/Talleres.Api/Talleres.Api.csproj tests/Talleres.Api.Tests/Talleres.Api.Tests.csproj
dotnet add tests/Talleres.Api.Tests/Talleres.Api.Tests.csproj reference src/Talleres.Api/Talleres.Api.csproj
```

Después, ajustar las versiones y propiedades al contrato anterior, crear `global.json` y las convenciones. Los valores que genere una plantilla pueden diferir; el contrato de esta spec prevalece. Si falta el SDK o no se puede restaurar un paquete, registrar el bloqueo sin cambiar de framework silenciosamente. Las [plantillas oficiales de .NET](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates) documentan estas opciones; la [validación de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-10.0) describe `AddValidation`.

## Criterios de aceptación

| ID | Dado / cuando | Resultado observable |
|---|---|---|
| PROY-01 | Partir de una carpeta con solo el contexto y crear el esqueleto. | Solución con los dos proyectos y referencia tests → API; no hay archivos ajenos sobrescritos. |
| PROY-02 | Restaurar y compilar. | Ambos comandos terminan con código 0 sobre `net10.0`. |
| PROY-03 | Ejecutar pruebas del host. | Hay pruebas reales descubiertas y aprobadas; no basta la prueba vacía de la plantilla ni una ejecución con cero tests. |
| PROY-04 | Consultar `/health`. | 200 y `status=ok`. |
| PROY-05 | Consultar OpenAPI en Development. | 200 JSON con la operación `/health`. |
| PROY-06 | Consultar OpenAPI en Production y una ruta inexistente. | 404 ProblemDetails en ambos casos, sin excepción expuesta. |
| PROY-07 | Arrancar con el perfil local documentado. | Responde en `http://localhost:5080`; no requiere BD ni claves de IA. |
| PROY-08 | Revisar convenciones y README. | Están documentados requisitos, arranque, pruebas y alcance de esta etapa; los artefactos temporales quedan ignorados. |
| PROY-09 | Reanudar sobre una solución existente. | Se identifican criterios satisfechos y pendientes sin recrear ni borrar archivos correctos. |
| PROY-10 | Abrir Swagger UI en Development y ejecutar `/health`; repetir el acceso a UI, documento y un recurso estático de Swagger en Production. | UI carga y muestra el contrato; `/health` responde 200; en Production esos recursos de documentación devuelven 404. Combinar pruebas HTTP con comprobación real del navegador. |

## Reparto y dependencias de agentes

| Responsable | Archivos y tarea |
|---|---|
| Planificador | Solo lectura del destino y el contrato; identifica qué falta y el orden de creación. |
| Coordinador, primero | Esqueleto mediante CLI; solución, `global.json`, `.gitignore`, `.editorconfig`, ambos `.csproj` y README mínimo. Copia el contexto si hace falta. |
| Implementador, después del esqueleto | `src/Talleres.Api/Program.cs`, `Properties/launchSettings.json`, `appsettings.json` y `appsettings.Development.json`. |
| QA, después del esqueleto | `tests/Talleres.Api.Tests/ProjectBootstrapTests.cs`; retira únicamente la prueba vacía de la plantilla recién creada. |
| Revisor | Solo lectura del resultado, criterios y salidas reales. |

Implementador y QA pueden escribir en paralelo tras recibir el esqueleto y el contrato. El coordinador deja de modificar sus proyectos durante esa fase. Las compilaciones se ejecutan al terminar ambas entregas.

## Verificación y salida

```powershell
dotnet restore CursoCursor.slnx
dotnet build CursoCursor.slnx --no-restore
dotnet test CursoCursor.slnx --no-build
dotnet run --project src/Talleres.Api --launch-profile http
```

Los tres primeros comandos son secuenciales. El último inicia la comprobación manual y se detiene al terminar. Registrar criterio, comprobación y resultado real. La siguiente spec es [catálogo de talleres](02-catalogo-de-talleres.md); no se inicia sin autorización para esa etapa o para la ruta completa. **Decisiones abiertas:** ninguna.

## Prompt de entrada

```text
Desarrolla specs/01-creacion-del-proyecto.md siguiendo docs/flujo-multiagente.md.
Actúa como coordinador y usa planificador, implementador, qa y revisor.
Inspecciona el destino: el código puede no existir. Presenta el plan para
mi revisión; después de aceptarlo, crea primero el esqueleto bajo tu
propiedad y delega src/ y tests/ a sus responsables. Verifica PROY-01 a
PROY-10 y entrega evidencia real. Conserva los archivos existentes.
```
