# 06 Consultar la disponibilidad de un taller

**Tipo:** spec funcional. **Estado:** contrato definido; endpoint y pruebas específicos pendientes de implementar en la base del curso.

## Historia de usuario

Como persona que quiere reservar un taller, quiero consultar cuántos lugares quedan, para decidir si puedo inscribirme.

## Objetivo y límites

Permitir consultar cuántos lugares quedan en un taller, sin mostrar datos personales ni modificar reservas.

No se añaden persistencia, autenticación, nuevos paquetes, otro almacén, otro mecanismo de bloqueo ni funciones de IA dentro de la API. No se cambia el contrato del catálogo, la creación o la cancelación. No se implementa todavía `onlyAvailable`.

**Dependencias:** la API base definida en las specs de [catálogo](02-catalogo-de-talleres.md), [reservas](03-creacion-de-reservas.md) y [cancelación](04-cancelacion-de-reservas.md), con la atomicidad correcta. En la ruta desde cero se comprueba antes [concurrencia y consistencia](05-concurrencia-y-consistencia.md). La [guía de exploración](../curso/base-y-exploracion.md) explica la copia resuelta. El desarrollo sigue el [protocolo multiagente](../docs/flujo-multiagente.md); la agenda de la práctica está en [curso/README.md](../curso/README.md).

## Contrato HTTP

```http
GET /api/workshops/{id}/availability
```

Para un taller existente, 200 `application/json` con **estos cinco campos**:

```json
{
  "workshopId": "11111111-1111-1111-1111-111111111111",
  "capacity": 2,
  "activeReservations": 0,
  "availableSeats": 2,
  "isFull": false
}
```

| Campo | Regla |
|---|---|
| `workshopId` | ID del taller consultado. |
| `capacity` | Capacidad del taller en la base existente. |
| `activeReservations` | Reservas activas de la instantánea consultada. |
| `availableSeats` | `capacity - activeReservations`, calculado a partir de la misma instantánea. |
| `isFull` | `availableSeats == 0`. |

Se reutiliza `ReservationStore.GetWorkshop(id)` para obtener una sola instantánea consistente. No se realizan lecturas separadas para construir los diferentes campos. Con las reglas correctas de la base, las reservas activas nunca superan la capacidad.

Para un GUID válido inexistente, 404 `application/problem+json` con `status: 404` y `code: "workshop_not_found"`, manteniendo el título y detalle usados por el detalle de taller. Un ID que no sea GUID conserva el 404 del enrutamiento mediante `:guid`; no se exige el código de negocio para ese caso.

La respuesta no incluye `participantName`, `email`, listas de reservas ni otros campos. Repetir la consulta no altera el estado. La disponibilidad es una observación del momento de la consulta y no garantiza un lugar para una reserva posterior.

## Criterios de aceptación

Cada escenario parte de una API de pruebas independiente. Para reservas usar nombres válidos y correos ficticios distintos, con dominio `example.com`.

| ID | Preparación y acción | Resultado esperado |
|---|---|---|
| S04-01 | Consultar sin reservas. | 200; capacidad 2, activas 0, disponibles 2, `isFull=false`, ID correcto. |
| S04-02 | Crear una reserva y consultar. | 200; activas 1, disponibles 1, `isFull=false`. |
| S04-03 | Crear dos reservas y consultar. | 200; activas 2, disponibles 0, `isFull=true`. |
| S04-04 | Con dos reservas, cancelar una y consultar. | 200; activas 1, disponibles 1, `isFull=false`. |
| S04-05 | Consultar un GUID válido inexistente. | 404 ProblemDetails con `code=workshop_not_found`. |
| S04-06 | Consultar un ID no GUID. | 404 de enrutamiento; no 500. |
| S04-07 | Consultar varias veces y volver a leer el detalle. | El número de reservas y la disponibilidad permanecen iguales. |
| S04-08 | Inspeccionar el JSON de una respuesta 200. | Solo los cinco campos acordados; no hay información de participantes. |
| S04-09 | Ejecutar la suite completa sobre el cambio integrado. | La base mantiene normalización, validación, duplicados, cupo, cancelación y concurrencia. |

## Contexto técnico y restricciones de archivos

| Trabajo | Archivos |
|---|---|
| Código | [Contracts.cs](../src/Talleres.Api/Features/Workshops/Contracts.cs) para el DTO y [WorkshopEndpoints.cs](../src/Talleres.Api/Features/Workshops/WorkshopEndpoints.cs) para la nueva ruta. |
| Pruebas | Crear `tests/Talleres.Api.Tests/AvailabilityTests.cs`, siguiendo los patrones de [WorkshopApiTests.cs](../tests/Talleres.Api.Tests/WorkshopApiTests.cs). |
| Contexto de lectura | [ReservationStore.cs](../src/Talleres.Api/Features/Workshops/ReservationStore.cs); su método `GetWorkshop(id)` es suficiente y no requiere cambios. |

El plan conserva este alcance. Si aparece una necesidad de modificar otro archivo, el especialista la comunica al coordinador antes de editar. El reparto de responsables y el orden de integración se rigen por el protocolo común.

## Verificación y terminado

Las pruebas de disponibilidad aún no existen al redactar esta spec. El punto de partida contiene **33 casos de prueba definidos**, identificados por lectura, sin afirmar aquí una ejecución. QA debe comprobar los estados HTTP y los campos observables; no basta con verificar que una respuesta no sea nula.

Después de terminar ambas tareas, el coordinador ejecutará desde la raíz:

```powershell
dotnet test CursoCursor.slnx
```

Si hace falta preparar dependencias o comprobar también la API temporal, puede utilizar el [script de verificación](../scripts/verify.ps1). El smoke test existente comprueba la base; no sustituye las nuevas pruebas de disponibilidad.

Para una comprobación manual, con la API iniciada:

```powershell
Invoke-RestMethod http://localhost:5080/api/workshops/11111111-1111-1111-1111-111111111111/availability
```

Se avanza con el desarrollo terminado cuando los criterios S04-01 a S04-09 se han contrastado, la suite integrada pasa y los hallazgos confirmados están resueltos. Si falta una comprobación o persiste un defecto, se explica el pendiente sin declarar completada la funcionalidad. Registrar en la conversación el comando y resultado real; si algo no se ejecutó, decirlo.

## Prompt de entrada

```text
Desarrolla specs/06-disponibilidad.md siguiendo docs/flujo-multiagente.md.
Actúa como coordinador y usa los especialistas de .cursor/agents/.
Presenta el plan para mi revisión antes de editar. Tras aceptarlo,
completa implementación, pruebas y revisión, conservando el contrato
y verificando S04-01 a S04-09. Entrega resultados reales y pendientes.
```

Para preparar únicamente el plan, sustituye «Desarrolla» por «Planifica sin editar ni ejecutar comandos que cambien el workspace». Si ya hay un plan aceptado en la conversación, continúa desde él conforme al protocolo.
