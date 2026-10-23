# 02 Consultar el catálogo y el detalle de talleres

**Tipo:** spec funcional. **Estado:** lista para planificar; comportamiento base existente en la copia del curso. **Dependencia:** host, errores y pruebas de [creación del proyecto](01-creacion-del-proyecto.md).

## Historia de usuario

Como persona interesada en asistir, quiero consultar los talleres y sus lugares disponibles, para elegir en cuál reservar.

## Alcance y contrato

Crear el catálogo de solo lectura y el almacén en memoria que usarán después las reservas. La instancia inicial contiene exactamente un taller: ID `11111111-1111-1111-1111-111111111111`, título `Taller de Cursor`, capacidad 2. Al arrancar no hay reservas.

| Petición | Resultado |
|---|---|
| `GET /api/workshops` | 200 `application/json`, arreglo con el taller inicial. |
| `GET /api/workshops/{id}` con ID existente | 200 `application/json`, objeto del taller. |
| Detalle de un GUID válido inexistente | 404 ProblemDetails, `status:404`, `code:workshop_not_found`, título `Recurso no encontrado`, detalle `No se encontró el taller.` |
| Detalle con ID que no es GUID | 404 de enrutamiento mediante `:guid`; no se exige el código de negocio. |

Cada objeto tiene exactamente estos campos; el catálogo contiene objetos del mismo tipo:

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "title": "Taller de Cursor",
  "capacity": 2,
  "activeReservations": 0,
  "availableSeats": 2
}
```

`activeReservations` es el número de reservas activas y `availableSeats = capacity - activeReservations`. En esta etapa ambos parten de 0 y 2. Las operaciones de reserva y cancelación deberán actualizar estas lecturas cuando se integren; no se permite dejarlas codificadas como constantes.

No agregar CRUD de talleres, filtros, paginación, listas de participantes, base de datos ni reservas todavía. Consultar no modifica el estado. La consulta muestra una instantánea, no aparta un lugar.

## Diseño mínimo y reglas

- Crear `WorkshopResponse` con los cinco campos y `ReservationStore` como servicio singleton por host, con `SeedWorkshopId`, `GetWorkshops()` y `GetWorkshop(Guid)`.
- Mantener el estado en campos de instancia. El ID semilla puede ser estático; las reservas y colecciones mutables no deben compartirse entre hosts de pruebas.
- Los métodos producen instantáneas inmutables bajo un único bloqueo del almacén. No exponen diccionarios mutables ni enumeraciones diferidas fuera del bloqueo.
- Registrar el almacén en `Program.cs` y extraer las rutas a `Features/Workshops/WorkshopEndpoints.cs` mediante `MapWorkshopEndpoints()`.
- Mantener `/health` y OpenAPI, incluyendo metadatos de estas rutas y del 404 del detalle.

## Criterios de aceptación

| ID | Dado / cuando | Resultado observable |
|---|---|---|
| CAT-01 | Consultar el catálogo en un host nuevo. | 200; arreglo con un taller, ID y título acordados, capacidad 2, activas 0, disponibles 2. |
| CAT-02 | Consultar el detalle del ID inicial. | 200; los mismos cinco campos y valores del catálogo. |
| CAT-03 | Consultar un GUID válido inexistente. | 404 `application/problem+json` con estado, código, título y detalle acordados. |
| CAT-04 | Consultar `/api/workshops/no-es-guid`. | 404, nunca 500. |
| CAT-05 | Repetir catálogo y detalle. | Conservan valores y número de talleres; no se crea una reserva. |
| CAT-06 | Inspeccionar los JSON de ambas rutas. | Exactamente cinco campos por taller; sin nombres, correos ni colecciones de participantes. |
| CAT-07 | Revisar cómo se construyen las respuestas. | Cálculo de cupo sobre una misma instantánea, colección materializada y sin exponer estado mutable. |
| CAT-08 | Ejecutar regresión del host y consultar OpenAPI. | Salud y errores siguen correctos; aparecen las rutas del catálogo y detalle. |

## Reparto multiagente

Aplicar el [protocolo](../docs/flujo-multiagente.md). Planificador y revisor trabajan en solo lectura. Implementador es propietario de `src/Talleres.Api/Program.cs` y de `Features/Workshops/Contracts.cs`, `ReservationStore.cs`, `WorkshopEndpoints.cs`. QA es propietario de `tests/Talleres.Api.Tests/CatalogTests.cs` en una construcción nueva.

En la copia existente, QA puede ampliar `WorkshopApiTests.cs` si ya cubre el escenario, con asignación expresa del coordinador; no duplicar todos los tests solo para cambiar su nombre. Tras acordar el DTO, implementación y pruebas pueden escribirse en paralelo. La comprobación de snapshots después de mutaciones se completa en reservas y concurrencia.

## Verificación y terminado

Ejecutar `dotnet test CursoCursor.slnx` después de integrar. Para CAT-07 combinar revisión del código con comprobaciones de las respuestas; no afirmar que HTTP por sí solo demuestra el bloqueo. Con la API iniciada, comprobar catálogo, detalle y GUID inexistente.

La entrega vincula CAT-01 a CAT-08 con resultados reales y conserva la regresión del host. **Decisiones abiertas:** ninguna.

## Prompt de entrada

```text
Desarrolla specs/02-catalogo-de-talleres.md con docs/flujo-multiagente.md.
Usa planificador, implementador, qa y revisor. Comprueba la base del host,
presenta el plan para revisión y, una vez aceptado, implementa o completa
los criterios CAT-01 a CAT-08. Entrega evidencia real de cada criterio.
```
