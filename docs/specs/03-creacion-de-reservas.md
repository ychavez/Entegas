# 03 Reservar un lugar con validación y control de cupo

**Tipo:** spec funcional. **Estado:** lista para planificar; comportamiento base existente en la copia del curso. **Dependencia:** [catálogo y detalle](02-catalogo-de-talleres.md), su almacén y host de pruebas.

## Historia de usuario

Como persona que quiere asistir a un taller, quiero reservar un lugar con mi nombre y correo, para obtener una confirmación sin que se exceda el cupo.

## Alcance y contrato HTTP

Implementar validación, normalización, creación, detección de duplicados y límite de capacidad como un incremento completo. No entregar una reserva que omita estas reglas para corregirla después. La cancelación corresponde a la siguiente spec; no se añaden pagos, autenticación, correos reales, lista de reservas ni un endpoint GET de reserva.

```http
POST /api/workshops/11111111-1111-1111-1111-111111111111/reservations
Content-Type: application/json

{"participantName":"  Ana Pérez  ","email":" ANA@EXAMPLE.COM "}
```

Respuesta 201 `application/json`, encabezado `Location: /api/reservations/{id}` y estos cinco campos:

```json
{
  "id": "22222222-2222-4222-8222-222222222222",
  "workshopId": "11111111-1111-1111-1111-111111111111",
  "participantName": "Ana Pérez",
  "email": "ana@example.com",
  "createdAtUtc": "2026-10-20T18:00:00+00:00"
}
```

El ID y fecha del ejemplo son ilustrativos: generar GUID no vacío y único, y fecha UTC de la creación. `Location` identifica la reserva y será la ruta de cancelación; no promete un GET que no existe.

## Validaciones y reglas de negocio

| Entrada o condición | Regla |
|---|---|
| `participantName` | Obligatorio, string no nulo; aplicar `Trim` antes de validar; longitud entre 2 y 100 inclusive; conservar mayúsculas, acentos y espacios internos. |
| `email` | Obligatorio, string no nulo; `Trim().ToLowerInvariant()`; máximo 254 caracteres después de normalizar; formato válido según `EmailAddressAttribute`, sin comprobar existencia del buzón. |
| JSON | Rechazar JSON mal formado, cuerpo `null`, falta de campos obligatorios o tipo incompatible con 400; no mutar estado. |
| Duplicado | A lo sumo una reserva activa por pareja taller + correo normalizado, aunque el nombre cambie. |
| Cupo | Rechazar cuando `activeReservations >= capacity`. La tercera reserva válida con correo distinto falla para capacidad 2. |
| Orden de negocio | Para DTO válido: taller existente → correo no duplicado → cupo → registro. Si está lleno y el correo ya reservó, prevalece `duplicate_reservation`. |
| Atomicidad | Comprobar existencia, duplicado, cupo y registrar dentro del mismo bloqueo del almacén singleton. |
| Lecturas | Después de crear, catálogo y detalle reflejan inmediatamente el nuevo conteo; respuestas obtenidas antes no cambian. |

La validación HTTP ocurre antes del almacén. Un DTO inválido con GUID desconocido devuelve 400; no se define el orden entre errores de enrutamiento y cuerpo inválido. Un ID no GUID con cuerpo válido conserva 404 de ruta. La validación automática se configura con `AddValidation`; los métodos internos de `ReservationStore` reciben entradas ya válidas desde el endpoint.

| Error | Respuesta |
|---|---|
| Campos inválidos | 400 `application/problem+json`, `status:400`, título no vacío y `errors` no vacío. No fijar la capitalización de las claves de validación. |
| JSON no enlazable | 400 ProblemDetails también en Development; no se exige `errors` para fallos de JSON. |
| GUID válido de taller inexistente | 404, `code:workshop_not_found`, título `Recurso no encontrado`, detalle `No se encontró el taller.` |
| Correo duplicado | 409, `code:duplicate_reservation`, título `Conflicto de reserva`, detalle `Ese correo ya tiene una reserva activa en el taller.` |
| Taller lleno | 409, `code:workshop_full`, título `Conflicto de reserva`, detalle `El taller no tiene plazas disponibles.` |

Cada error de negocio es ProblemDetails con su `status`. No exige valores fijos para extensiones de diagnóstico como `traceId`. Ningún rechazo crea un registro ni consume un lugar.

## Criterios de aceptación

Cada escenario usa su propio host o almacén, nombres válidos y correos de `example.com`.

| ID | Dado / cuando | Resultado observable |
|---|---|---|
| RES-01 | Reservar en un taller vacío con el cuerpo del ejemplo. | 201, JSON normalizado, GUID no vacío, taller correcto, fecha con offset UTC y `Location` coincidente. |
| RES-02 | Consultar catálogo y detalle después de reservar. | Activas 1 y disponibles 1 en ambos; el snapshot previo conserva 0 y 2. |
| RES-03 | Omitir nombre o correo; usar null, vacío o espacios. | 400 de validación con errores, activas 0. Probar cada campo por separado. |
| RES-04 | Nombres de longitud normalizada 1, 2, 100 y 101. | 1 y 101 fallan con 400; 2 y 100 se aceptan con 201, en escenarios independientes. |
| RES-05 | Correo `sin-arroba`, y correos sintácticamente válidos de longitudes 254 y 255. | Formato inválido y 255 fallan con 400; 254 se acepta. Validar los límites independientemente del nombre. |
| RES-06 | Nombre y correo exceden simultáneamente sus máximos. | 400 con errores de ambos campos; no se reserva. |
| RES-07 | Enviar `{`, `null`, `{}` o un número como nombre en JSON. | 400 ProblemDetails y estado sin cambios; nunca 500. |
| RES-08 | Repetir el correo variando mayúsculas, espacios externos y nombre. | 409 `duplicate_reservation`; sigue una sola activa. |
| RES-09 | Crear dos reservas con correos distintos y una tercera distinta. | Primeras dos 201 con IDs distintos; tercera 409 `workshop_full`; activas 2, disponibles 0. |
| RES-10 | Repetir el correo de una reserva cuando el taller está lleno. | 409 `duplicate_reservation`, conservando el motivo de duplicado. |
| RES-11 | Enviar cuerpo válido a un GUID desconocido o ID no GUID. | 404 de negocio para el GUID, 404 de ruta para el otro; taller inicial sin cambios. |
| RES-12 | Enviar DTO inválido a un GUID desconocido. | 400 de validación antes de consultar el almacén. |
| RES-13 | Inspeccionar todas las salidas de error de negocio. | Estado, media type, código, título y detalle acordados; no hay 201 parcial. |
| RES-14 | Revisar operación y ejecutar la suite integrada. | Comprobaciones y registro bajo el mismo bloqueo; salud, OpenAPI y catálogo conservados. |

## Reparto multiagente

Seguir el [protocolo](../docs/flujo-multiagente.md). Planificador define contrato y pruebas sin editar. Implementador es propietario de `Contracts.cs`, `ReservationStore.cs`, `WorkshopEndpoints.cs` bajo `src/Talleres.Api/Features/Workshops/` y, si falta configuración, `src/Talleres.Api/Program.cs`.

QA trabaja en `tests/Talleres.Api.Tests/ReservationTests.cs` y `ReservationStoreTests.cs` en la ruta nueva. En la base actual puede ampliar `WorkshopApiTests.cs` en lugar de duplicar casos. QA no modifica el código para hacer pasar pruebas. Ambas tareas de escritura pueden avanzar en paralelo con el contrato fijo; el coordinador ejecuta después y el revisor inspecciona el diff integrado.

## Verificación y terminado

Ejecutar `dotnet test CursoCursor.slnx`; revisar status, campos, cabecera y estado posterior. Para las fechas comparar con un intervalo UTC capturado alrededor de la petición, sin exigir una hora literal. La revisión de atomicidad es necesaria desde esta etapa; [concurrencia y consistencia](05-concurrencia-y-consistencia.md) agrega las pruebas de contención dedicadas.

La entrega relaciona RES-01 a RES-14 con pruebas y salidas reales. La [cancelación](04-cancelacion-de-reservas.md) podrá reutilizar los identificadores creados. **Decisiones abiertas:** ninguna.

## Prompt de entrada

```text
Desarrolla specs/03-creacion-de-reservas.md con docs/flujo-multiagente.md.
Usa planificador, implementador, qa y revisor. Presenta el plan para
revisión y, tras aceptarlo, completa reservas con validación, duplicados,
cupo y atomicidad. Verifica RES-01 a RES-14 y entrega resultados reales.
```
