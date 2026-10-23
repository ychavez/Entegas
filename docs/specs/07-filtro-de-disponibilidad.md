# 07 Filtrar el catálogo por lugares disponibles

**Tipo:** spec funcional. **Estado:** contrato definido; filtro y pruebas específicos pendientes de implementar en la base del curso.

## Historia de usuario

Como persona que busca un taller para inscribirse, quiero filtrar el catálogo por lugares disponibles, para encontrar talleres en los que todavía pueda reservar.

## Objetivo y límites

Añadir una opción para mostrar solo talleres con lugares, manteniendo el comportamiento de los clientes que consultan el catálogo sin filtro.

No se crean talleres adicionales para la demostración, nuevas rutas, nuevos campos, paginación, ordenamiento, persistencia, autenticación, paquetes o funciones de IA en la API. No se modifica la reserva.

**Dependencias:** las specs de [catálogo](02-catalogo-de-talleres.md) y [reservas](03-creacion-de-reservas.md), con la regla correcta de cupo y [cancelación](04-cancelacion-de-reservas.md) para recuperar lugares. En la secuencia del proyecto, [disponibilidad](06-disponibilidad.md) ya está integrada y debe conservarse como regresión. Si se realizó el [ejercicio didáctico de Bugbot](../curso/bugbot-y-cupo.md), restaurar el operador correcto `>=` antes de empezar. El desarrollo sigue el [protocolo multiagente](../docs/flujo-multiagente.md); los tiempos de la práctica están en [curso/README.md](../curso/README.md).

## Contrato HTTP

```http
GET /api/workshops?onlyAvailable=true
```

| Entrada | Resultado |
|---|---|
| Sin `onlyAvailable` | 200, catálogo completo. |
| `onlyAvailable=false` | 200, catálogo completo. |
| `onlyAvailable=true` | 200, solo talleres con `availableSeats > 0`. |
| `onlyAvailable=quizas` u otro valor no booleano | 400 ProblemDetails. |
| `onlyAvailable=` | 400 ProblemDetails; vacío no equivale a parámetro ausente. |

Las formas canónicas del curso son `true` y `false`. La conversión puede conservar la aceptación de mayúsculas y minúsculas del booleano del framework; no se introducen alias como `1`, `0`, `sí` o `no`. Las claves repetidas en la query quedan fuera del contrato de esta práctica; no hace falta implementar ni probar una política adicional para ellas.

La respuesta correcta sigue siendo un arreglo JSON de `WorkshopResponse`, con los campos existentes `id`, `title`, `capacity`, `activeReservations` y `availableSeats`. No se añade un envoltorio ni se renombran campos o se incorporan datos de participantes. Si no queda ningún taller con lugares, la respuesta es **200 y `[]`**, no 404 ni 204.

El filtro se aplica sobre una sola instantánea obtenida mediante `ReservationStore.GetWorkshops()` en cada petición. No se guarda el resultado ni se reutiliza una respuesta anterior después de reservar o cancelar. Consultar no cambia el estado ni garantiza un lugar para una solicitud posterior.

Un error de query devuelve 400 `application/problem+json`, con `status: 400` y título no vacío. No se requiere un código de negocio específico. El comportamiento de `GET /api/workshops/{id}` y `/availability` permanece igual.

## Criterios de aceptación

Cada prueba debe preparar su propio estado, usar el taller inicial de capacidad 2 y correos ficticios distintos. No depender de una API de demostración que pueda tener reservas previas.

| ID | Preparación y acción | Resultado esperado |
|---|---|---|
| S07-01 | Consultar sin parámetro, tanto vacío como lleno el taller. | 200; incluye el taller en ambos estados con sus valores actuales. |
| S07-02 | Consultar `onlyAvailable=false`, tanto vacío como lleno. | 200; mismo catálogo completo que sin filtro. |
| S07-03 | Consultar `onlyAvailable=true` con cero y después con una reserva. | 200; incluye el taller mientras queden 2 o 1 lugares, respectivamente. |
| S07-04 | Crear dos reservas y consultar `onlyAvailable=true`. | 200; arreglo vacío. |
| S07-05 | Partiendo del taller lleno, cancelar una reserva y repetir el filtro. | 200; el taller reaparece con activas 1 y disponibles 1. |
| S07-06 | Consultar `onlyAvailable=quizas`. | 400 ProblemDetails; no cambia las reservas. |
| S07-07 | Consultar `onlyAvailable=`. | 400 ProblemDetails; no se trata como ausencia del parámetro. |
| S07-08 | Comparar campos de un taller filtrado con el detalle del mismo estado. | Se conservan los cinco campos actuales y sus valores; no aparecen datos personales. |
| S07-09 | Repetir el filtro y volver a consultar el detalle. | No cambia el número de reservas. |
| S07-10 | Ejecutar la suite completa tras integrar. | Continúan correctas base y disponibilidad; no reaparece la sobreventa. |

## Contexto técnico y restricciones de archivos

| Trabajo | Archivos |
|---|---|
| Código | Modificar únicamente [WorkshopEndpoints.cs](../src/Talleres.Api/Features/Workshops/WorkshopEndpoints.cs), donde se define la consulta del catálogo. |
| Pruebas | Crear `tests/Talleres.Api.Tests/WorkshopFilterTests.cs`, usando los patrones de [WorkshopApiTests.cs](../tests/Talleres.Api.Tests/WorkshopApiTests.cs). |
| Contexto de lectura | [Contracts.cs](../src/Talleres.Api/Features/Workshops/Contracts.cs) y [ReservationStore.cs](../src/Talleres.Api/Features/Workshops/ReservationStore.cs); reutilizar `WorkshopResponse` y `GetWorkshops()` sin modificarlos. |

La base ya configura respuestas 400 para errores de enlace también en Development. Si la estrategia elegida necesita salir del reparto, debe explicarse al coordinador antes de editar; no se amplía por comodidad. El protocolo común se aplica con estos archivos y criterios. Las pruebas de disponibilidad ayudan a la regresión, pero no sustituyen las pruebas del filtro.

## Verificación y terminado

Al redactar esta spec no existen pruebas específicas del filtro. La base original contiene **33 casos definidos**, identificados por lectura; después de disponibilidad habrá casos adicionales. No se fija un nuevo total ni se afirma que se hayan ejecutado aquí.

Una vez terminadas implementación y QA, ejecutar desde la raíz:

```powershell
dotnet test CursoCursor.slnx
```

El [script de verificación](../scripts/verify.ps1) sigue siendo útil para la base, pero su smoke test no sustituye los casos del filtro. Para la demostración, iniciar la API después de las ediciones y consultar:

```powershell
Invoke-RestMethod 'http://localhost:5080/api/workshops'
Invoke-RestMethod 'http://localhost:5080/api/workshops?onlyAvailable=false'
Invoke-RestMethod 'http://localhost:5080/api/workshops?onlyAvailable=true'
```

Crear dos reservas válidas con correos diferentes, repetir `true`, cancelar una y volver a consultar. Usar las [peticiones de ejemplo](../src/Talleres.Api/Talleres.Api.http) como referencia para crear y cancelar. Comprobar las respuestas 400 de query inválida y vacía en las pruebas HTTP, sin confundir una excepción del cliente PowerShell con un error 500 del servidor.

La funcionalidad se considera terminada cuando se han contrastado S07-01 a S07-10, la suite integrada pasa y el humano revisa el diff, los resultados y las limitaciones. Las afirmaciones sobre pruebas o revisión deben indicar qué se ejecutó realmente. Publicar o fusionar no forma parte de esta spec; cualquier paso remoto requiere una instrucción explícita.

## Prompt de entrada

```text
Desarrolla specs/07-filtro-de-disponibilidad.md siguiendo
docs/flujo-multiagente.md. Actúa como coordinador y usa los especialistas
de .cursor/agents/. Confirma el estado de las dependencias.
Presenta el plan para mi revisión antes de editar. Tras aceptarlo,
completa implementación, pruebas y revisión, conservando el contrato
y verificando S07-01 a S07-10. Entrega resultados reales y pendientes.
```

Si ya hay un plan aceptado en la conversación, continúa desde él conforme al protocolo.
