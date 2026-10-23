# 04 Cancelar una reserva y recuperar el lugar

**Tipo:** spec funcional. **Estado:** lista para planificar; comportamiento base existente en la copia del curso. **Dependencia:** [creación de reservas](03-creacion-de-reservas.md) con cupo y duplicados correctos.

## Historia de usuario

Como persona que ya no puede asistir, quiero cancelar mi reserva, para liberar el lugar y poder reservar nuevamente si cambio de decisión.

## Alcance y contrato HTTP

```http
DELETE /api/reservations/{id}
```

| Condición | Resultado |
|---|---|
| Reserva activa con el ID indicado | 204 No Content, cuerpo vacío; se elimina la reserva activa. |
| GUID válido inexistente o de una reserva ya cancelada | 404 `application/problem+json`, `status:404`, `code:reservation_not_found`, título `Recurso no encontrado`, detalle `No se encontró una reserva activa con ese identificador.` |
| ID que no es GUID | 404 de enrutamiento con `:guid`; no se exige código de negocio. |

El ID es el de la reserva devuelto por el POST, no el del taller. No requiere cuerpo, nombre ni correo. En esta demostración local la operación no comprueba identidad; no se añade un modelo de permisos ni autenticación dentro de esta spec.

## Reglas de negocio

- Una cancelación exitosa elimina exactamente una reserva activa y libera exactamente un lugar de su taller.
- Repetir DELETE no vuelve a liberar capacidad. El estado es idempotente, aunque la primera respuesta sea 204 y las siguientes 404.
- La búsqueda y eliminación se realizan bajo el mismo bloqueo que protege reservas y lecturas.
- Catálogo y detalle reflejan inmediatamente el nuevo conteo. La capacidad del taller no cambia.
- El correo cancelado puede reservar otra vez si queda cupo. La nueva reserva tiene un ID diferente; el ID anterior sigue cancelado.
- Cancelar una reserva no elimina las demás ni permite cancelar por el ID del taller.

No agregar historial, estados adicionales, modificación de reservas, notificaciones, GET de reserva ni persistencia. La implementación mínima conserva únicamente reservas activas.

## Criterios de aceptación

| ID | Dado / cuando | Resultado observable |
|---|---|---|
| CAN-01 | Crear una reserva y cancelar su ID. | 204 sin cuerpo; catálogo y detalle pasan a activas 0, disponibles 2. |
| CAN-02 | Llenar el taller y cancelar una de las dos reservas. | 204; activas 1, disponibles 1, capacidad 2. |
| CAN-03 | Repetir la cancelación del mismo ID. | 404 `reservation_not_found`; conteos sin cambios. |
| CAN-04 | Cancelar un GUID desconocido o el ID del taller. | 404 de negocio; las reservas existentes permanecen. |
| CAN-05 | Cancelar con ID no GUID. | 404 de ruta, nunca 500. |
| CAN-06 | Tras cancelar, reservar con el mismo correo normalizado. | 201 con nuevo ID; se ocupa un solo lugar. |
| CAN-07 | Tras volver a reservar, cancelar de nuevo el ID antiguo. | 404; la nueva reserva sigue activa. |
| CAN-08 | Cancelar la reserva de Ana, conservando la de Luis. | Luis sigue provocando `duplicate_reservation` al repetir su correo; al cancelar el ID de Luis se libera su lugar. |
| CAN-09 | Cancelar todas las reservas y repetir sus DELETE. | Disponibles termina en 2 y nunca crece por encima de capacidad. |
| CAN-10 | Revisar JSON de errores y ejecutar regresión. | Media type, estado, título, detalle y código acordados; creación, normalización, duplicados y cupo siguen correctos. |

Cada prueba crea su propio estado y guarda los IDs reales del POST. No se fijan IDs de reserva manualmente.

## Reparto multiagente

Aplicar el [protocolo](../docs/flujo-multiagente.md). Implementador: `src/Talleres.Api/Features/Workshops/ReservationStore.cs` y `WorkshopEndpoints.cs`. QA: `tests/Talleres.Api.Tests/CancellationTests.cs` y los casos pertinentes de `ReservationStoreTests.cs`. En la base existente se pueden ampliar los casos de `WorkshopApiTests.cs` por asignación del coordinador.

Planificador y revisor no escriben. Implementador y QA pueden trabajar simultáneamente en sus archivos separados, una vez acordado el contrato. Ningún agente compila mientras otro edita; el coordinador integra y ejecuta.

## Verificación y terminado

Ejecutar `dotnet test CursoCursor.slnx` tras ambas entregas. Comprobar cuerpo vacío en 204, códigos de negocio en 404 y lecturas después de cada transición. Los casos de cancelaciones simultáneas se especifican en [concurrencia y consistencia](05-concurrencia-y-consistencia.md); la operación ya debe usar el bloqueo común en este incremento.

Entregar evidencia CAN-01 a CAN-10, revisión y pendientes. **Decisiones abiertas:** ninguna.

## Prompt de entrada

```text
Desarrolla specs/04-cancelacion-de-reservas.md siguiendo docs/flujo-multiagente.md.
Usa planificador, implementador, qa y revisor. Presenta el plan para revisión
y, tras aceptarlo, completa la cancelación sin liberar lugares dos veces.
Verifica CAN-01 a CAN-10 y conserva los contratos de reservas y catálogo.
```
