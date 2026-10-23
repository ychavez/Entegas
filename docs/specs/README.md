# Specs para construir la API de talleres de principio a fin

Estas nueve specs describen el proyecto completo: acordar la idea y las tecnologías, crear la solución .NET, implementar los casos de uso de talleres y reservas, comprobar concurrencia y cerrar con una verificación integrada. Están numeradas del **00 al 08** para seguir su orden de trabajo. Cada una define el resultado esperado y criterios observables. El [protocolo multiagente](../docs/flujo-multiagente.md) define cómo coordinar planificador, implementador, QA y revisor.

**Alcance final:** API local .NET 10 con Minimal APIs, un taller de capacidad 2, almacén en memoria y pruebas xUnit. Incluye catálogo, detalle, reservas, cancelación, disponibilidad y filtro. No incluye interfaz web, base de datos, autenticación, pagos, CRUD de talleres ni despliegue.

## Orden de construcción

| Orden | Spec de entrada | Resultado | Dependencia | Estado en esta copia |
|---|---|---|---|---|
| 00 | [Visión y alcance](00-vision-y-alcance.md) | Idea inicial, usuarios, objetivos, C#/.NET, tecnologías, casos de uso y límites. | Ninguna. | Definición documentada para revisión. |
| 01 | [Creación del proyecto](01-creacion-del-proyecto.md) | Solución, API, pruebas, salud, OpenAPI, Swagger UI y configuración local. | Visión 00 revisada; ninguna funcionalidad previa. | Estructura y host existentes; Swagger UI pendiente. |
| 02 | [Catálogo de talleres](02-catalogo-de-talleres.md) | Almacén, catálogo y detalle con cupo. | 01. | Comportamiento existente. |
| 03 | [Creación de reservas](03-creacion-de-reservas.md) | Alta con validación, normalización, duplicados y cupo atómico. | 02. | Comportamiento existente. |
| 04 | [Cancelación de reservas](04-cancelacion-de-reservas.md) | Liberar un lugar una sola vez y permitir volver a reservar. | 03. | Comportamiento existente. |
| 05 | [Concurrencia y consistencia](05-concurrencia-y-consistencia.md) | Verificar contención, duplicados simultáneos, cancelaciones y snapshots. | 03 y 04. | Bloqueo y parte de las pruebas existentes; contrastar cobertura adicional. |
| 06 | [Disponibilidad](06-disponibilidad.md) | `GET /api/workshops/{id}/availability`. | Base correcta; en esta ruta, 01–05. | Endpoint y pruebas específicos pendientes. |
| 07 | [Filtro de disponibilidad](07-filtro-de-disponibilidad.md) | Catálogo con `onlyAvailable`. | Base correcta y 06 integrada. | Filtro y pruebas específicos pendientes. |
| 08 | [Verificación final](08-verificacion-final.md) | Casos de uso conectados, ejemplos, scripts, CI y evidencia final. | Visión y etapas 01–07. | Scripts y CI base existentes; cierre pendiente de las ampliaciones. |

Los estados se basan en la lectura de esta copia el **20 de octubre de 2026**; no equivalen a pruebas ejecutadas ni a aceptación de todos los criterios. Una spec puede requerir nuevas comprobaciones de una función que ya existe. El coordinador inspecciona el estado actual antes de editar.

Los IDs `S04-*` y `S07-*` de disponibilidad y filtro se conservan como referencias estables. No indican el orden actual ni fases del protocolo. Los prefijos restantes son `VIS`, `PROY`, `CAT`, `RES`, `CAN`, `CON` e `INT`.

## Cómo usar la ruta

**Desde cero:** abre una carpeta nueva de proyecto. Copia `specs/`, `docs/` y `.cursor/` de este material; el README del paquete puede servir como contexto, pero no prueba que exista código. Empieza por `00-vision-y-alcance.md` para revisar la idea y las tecnologías. Continúa con `01-creacion-del-proyecto.md`: el coordinador crea el esqueleto antes de repartir `src/` y `tests/`. Las guías de `curso/` pueden copiarse como apoyo; no son requisito para aplicar el protocolo.

**Sobre esta copia resuelta:** contrasta la visión 00 con el objetivo y conserva la base existente. Revisa los criterios de las etapas 01–05, completa solo los faltantes que se acuerden y continúa con disponibilidad, filtro y verificación. No borres `src/` ni `tests/` para simular un arranque desde cero.

**Una sola funcionalidad:** pasa únicamente su spec al coordinador. El protocolo sigue siendo independiente de este índice y no ejecuta otras specs automáticamente. Las dependencias se comprueban en código; se consulta otro contrato solo si hace falta. La ruta completa solo se recorre cuando se pide expresamente.

El curso de cinco sesiones conserva su base ya resuelta y sus dos ampliaciones guiadas. Esta ruta también permite reconstruir el proyecto fuera de esa agenda; no convierte las nueve etapas en entregas de alumnos ni modifica las 20 horas. Las guías didácticas están en [curso/](../curso/README.md).

## Casos de uso y dónde se especifican

| Caso | Spec y criterios |
|---|---|
| Acordar idea, lenguaje, tecnologías, objetivos y límites. | Visión: VIS-01 a VIS-08. |
| Crear y arrancar el proyecto; consultar salud, OpenAPI y Swagger UI. | Creación: PROY-01 a PROY-10. |
| Explorar talleres y consultar un detalle sin exponer participantes. | Catálogo: CAT-01 a CAT-08. |
| Reservar con nombre y correo normalizados. | Reservas: RES-01 y RES-02. |
| Rechazar campos inválidos, límites de longitud y JSON incorrecto. | Reservas: RES-03 a RES-07, RES-12. |
| Rechazar duplicados y falta de cupo, con errores definidos. | Reservas: RES-08 a RES-11, RES-13 y RES-14. |
| Cancelar, repetir cancelación y reservar otra vez. | Cancelación: CAN-01 a CAN-10. |
| Resolver solicitudes simultáneas sin sobreventa ni liberación doble. | Concurrencia: CON-01 a CON-10. |
| Consultar cupo vacío, parcial, lleno y liberado. | Disponibilidad: S04-01 a S04-09. |
| Filtrar, recuperar un taller al cancelar y rechazar query inválida. | Filtro: S07-01 a S07-10. |
| Recorrer todas las operaciones y comprobar la entrega local. | Integración: INT-01 a INT-18. |

Los criterios están escritos como comportamiento verificable, no como una lista de archivos a generar. Crear código o escribir tests no demuestra por sí solo su cumplimiento.

## Prompt para construir el proyecto completo

Copiar en el chat principal de Cursor, abierto en la carpeta destino:

```text
Construye el proyecto completo de la API de talleres siguiendo las nueve
specs de specs/README.md, en el orden de construcción indicado.
Actúa como coordinador y aplica docs/flujo-multiagente.md en cada etapa.
Usa los agentes planificador, implementador, qa y revisor de .cursor/agents/.

Primero inspecciona la carpeta y distingue lo existente de lo pendiente.
Presenta el plan de la ruta completa con dependencias, criterios y archivos
por responsable; espera mi aprobación antes de editar. Si ya está aprobado
en esta conversación, continúa dentro de ese alcance.

Empieza por specs/00-vision-y-alcance.md: revisa conmigo la idea, el lenguaje,
las tecnologías y el alcance; en esa etapa no crees código.
Después, en un destino sin código, aplica specs/01-creacion-del-proyecto.md:
crea primero el esqueleto bajo tu propiedad y después delega src/ y tests/.
En un proyecto existente, conserva las piezas correctas y completa lo que
falte. Para cada etapa entrega tareas autocontenidas con contrato y criterios.
Ejecuta las etapas en orden: comparten archivos de negocio y no deben ser
implementadas simultáneamente. Dentro de una etapa pueden escribir en
paralelo implementador y QA en los archivos distintos asignados.

Espera a que todos terminen de editar antes de compilar. Integra, verifica,
solicita revisión independiente y corrige los hallazgos confirmados. Presenta
el resultado de cada etapa para mi aceptación antes de avanzar a la siguiente,
salvo que ya haya autorizado expresamente continuar toda la ruta.
Finaliza con specs/08-verificacion-final.md y una matriz
criterio → comprobación → resultado real.

No inventes delegaciones, pruebas ni aceptaciones. Si faltan subagentes,
explica la modalidad disponible. No crees commits, publiques ni fusiones.
```

## Prompt para una sola spec

```text
Desarrolla specs/01-creacion-del-proyecto.md siguiendo docs/flujo-multiagente.md.
Actúa como coordinador y usa los especialistas de .cursor/agents/.
Presenta el plan para mi revisión. Tras aceptarlo, completa implementación,
pruebas, integración y revisión independiente. Entrega criterios comprobados,
resultados reales y pendientes para mi aceptación.
```

Cambia la ruta por la funcionalidad deseada. Guardar estos archivos no inicia el desarrollo de la API por sí solo.

## Agregar o cambiar un requisito

Copia [plantilla.md](plantilla.md), completa objetivo, contrato, dependencias y criterios con un prefijo único, y agrega la spec al catálogo. Indica si es una funcionalidad o un habilitador técnico. El planificador asigna archivos según esa necesidad; no copia automáticamente el reparto de disponibilidad.

Si cambia el requisito, actualiza primero la spec y el plan, y después código y pruebas. La plantilla, este índice, el protocolo y las guías del curso no son etapas adicionales del producto.
