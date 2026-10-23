# 05 Mantener cupo y reservas consistentes con solicitudes simultáneas

**Tipo:** spec de calidad funcional. **Estado:** lista para planificar; la base ya tiene bloqueo y pruebas de sobreventa y duplicados concurrentes. Los escenarios adicionales deben contrastarse con la cobertura existente. **Dependencias:** [reservas](03-creacion-de-reservas.md), [cancelación](04-cancelacion-de-reservas.md) y sus lecturas de catálogo y detalle.

## Objetivo

Como organizador, quiero que las solicitudes simultáneas respeten la capacidad y los correos únicos, para que la cantidad de confirmaciones coincida con los lugares que realmente tengo.

Esta spec verifica y completa una garantía que ya se exige al crear reservas. No autoriza dejar operaciones sin protección hasta llegar aquí. No agrega endpoints ni modifica los contratos HTTP.

## Reglas e invariantes

Para un taller de capacidad 2, después de cualquier operación y en cada instantánea consistente:

```text
0 <= activeReservations <= capacity
availableSeats = capacity - activeReservations
0 <= availableSeats <= capacity
Una sola reserva activa por taller y correo normalizado.
```

El almacén singleton utiliza un único bloqueo para comprobar y escribir reservas, cancelar y obtener las instantáneas del catálogo y detalle. La operación de reservar es indivisible: existencia → duplicado → cupo → alta. No mover la comprobación de cupo al endpoint ni usar un bloqueo diferente para cada método.

La garantía corresponde a una instancia del proceso. No incorporar bases de datos, bloqueos distribuidos, reintentos, cachés, semáforos adicionales ni pruebas de rendimiento. Reiniciar crea un almacén vacío; los hosts de pruebas no comparten estado mutable.

## Criterios de aceptación

| ID | Preparación y acción | Resultado observable |
|---|---|---|
| CON-01 | Sobre un almacén nuevo, liberar juntas 40 tareas con correos distintos. | Exactamente 2 resultados Created y 38 WorkshopFull; activas 2, disponibles 0. |
| CON-02 | Sobre otro almacén nuevo, liberar juntas 20 tareas del mismo correo alternando espacios y mayúsculas. | Exactamente 1 Created y 19 DuplicateEmail; activas 1, disponibles 1. |
| CON-03 | En un host nuevo, enviar 25 POST concurrentes con correos distintos. | Exactamente 2 HTTP 201 y 23 HTTP 409 `workshop_full`; ningún 500; detalle final 2 y 0. |
| CON-04 | En otro host nuevo, enviar 20 POST concurrentes con variantes del mismo correo. | 1 HTTP 201 y 19 HTTP 409 `duplicate_reservation`; conteos 1 y 1. |
| CON-05 | Crear una reserva y lanzar 10 DELETE simultáneos del mismo ID. | 1 HTTP 204 y 9 HTTP 404 `reservation_not_found`; disponibles termina en 2. |
| CON-06 | Taller lleno con Ana y Luis; lanzar juntos DELETE de Ana y POST de Sofía. | DELETE es 204. POST es 201 si se procesa después del borrado, o 409 `workshop_full` si se procesa antes. Estado final respectivamente 2/0 o 1/1; ninguna tercera reserva activa. |
| CON-07 | Durante la carrera de CON-06, consultar varias veces catálogo y detalle. | Cada respuesta por separado cumple las invariantes; al terminar las escrituras, ambas rutas coinciden. No exigir igualdad entre peticiones tomadas en momentos diferentes. |
| CON-08 | Guardar una respuesta del almacén y después reservar o cancelar. | El objeto anterior conserva sus valores; la nueva consulta refleja el estado nuevo. |
| CON-09 | Crear una reserva en el host A y consultar un host B independiente. | A conserva su reserva; B inicia con activas 0 y disponibles 2. |
| CON-10 | Revisar la sección crítica y ejecutar la suite completa. | Todas las lecturas y mutaciones usan el bloqueo común; no hay sobreventa, duplicados ni liberación doble; se preservan contratos previos. |

## Diseño de las comprobaciones

QA usa barrera de inicio o `TaskCompletionSource` en las pruebas del almacén y `Task.WhenAll` para recopilar todos los resultados. No usar `Thread.Sleep` para forzar un orden ni considerar suficiente que una prueba concurrente haya enviado peticiones en un bucle con `await` una por una.

Las pruebas no fijan cuál participante gana. Para CON-06 contemplan ambos órdenes válidos y verifican que el estado final corresponda al resultado del POST. La prueba no debe declarar un fallo solo porque ganó un competidor diferente.

Los tests ejercitan contención, pero no demuestran matemáticamente todos los intercalados. La evidencia incluye revisión del bloqueo y comprobaciones de invariantes, además de resultados HTTP. No usar la ausencia de fallos en una sola corrida como prueba de corrección de un algoritmo sin exclusión mutua.

## Reparto multiagente

Seguir el [protocolo](../docs/flujo-multiagente.md). Planificador identifica cobertura existente y faltantes. QA es propietario de `tests/Talleres.Api.Tests/ReservationStoreTests.cs` y `ConcurrencyTests.cs`; puede ampliar los tests HTTP existentes si el coordinador los asigna. Implementador modifica `src/Talleres.Api/Features/Workshops/ReservationStore.cs` solo si hay un defecto confirmado; no sustituye un bloqueo correcto por otra arquitectura.

Si la revisión muestra que el código ya cumple, la tarea de implementación puede ser una inspección sin cambios, declarada como tal. QA agrega las comprobaciones necesarias y el revisor inspecciona código y evidencia de forma independiente. Todo cambio de código conserva el contrato acordado.

## Verificación y terminado

Ejecutar `dotnet test CursoCursor.slnx` tras finalizar las escrituras. Relacionar CON-01 a CON-10 con casos, revisión y salida real. Una falla intermitente debe investigarse; no se oculta mediante reintentos hasta obtener verde.

La regla final de cupo utiliza `>=`. El fallo intencional de Bugbot pertenece a una rama de laboratorio y debe estar corregido antes de aceptar esta spec. No se requiere un PR ni una revisión remota. **Decisiones abiertas:** ninguna.

## Prompt de entrada

```text
Desarrolla specs/05-concurrencia-y-consistencia.md siguiendo
docs/flujo-multiagente.md. Usa planificador, implementador, qa y revisor.
Presenta el plan para revisión. Tras aceptarlo, conserva lo que ya cumple,
completa las pruebas de contención y corrige solo defectos confirmados.
Verifica CON-01 a CON-10 con resultados reales y revisión del bloqueo.
```
