# 08 Verificar el proyecto completo y preparar su uso local

**Tipo:** spec técnica de integración. **Estado:** lista para planificar; la base tiene scripts, ejemplos y CI, pero la comprobación final depende de implementar disponibilidad y filtro. **Dependencias:** creación, catálogo, reservas, cancelación, concurrencia, [disponibilidad](06-disponibilidad.md) y [filtro](07-filtro-de-disponibilidad.md), enumeradas en el [índice](README.md).

## Objetivo y alcance

Como instructor o desarrollador que recibe el proyecto, quiero levantarlo y reproducir sus casos de uso con comandos y pruebas, para comprobar que los incrementos funcionan juntos.

Este incremento agrega o completa pruebas del flujo conjunto, peticiones de ejemplo y verificación local. No agrega capacidades de negocio, una interfaz web, despliegue ni conexión de cuentas. El [protocolo multiagente](../docs/flujo-multiagente.md) organiza el trabajo; la [guía de validación integral](../curso/validacion-integral.md) explica cómo mostrarlo en clase.

## Resultado técnico

| Pieza | Comportamiento requerido |
|---|---|
| `tests/Talleres.Api.Tests/EndToEndTests.cs` | Ejercita la secuencia completa por HTTP en un mismo host nuevo, con IDs realmente devueltos por POST. |
| `src/Talleres.Api/Talleres.Api.http` | Ejemplos ejecutables de salud, catálogo, detalle, reserva, cancelación, disponibilidad y filtro; variables claras para ID y URL. |
| `scripts/verify.ps1` | Restaura, compila Release, prueba el estado integrado y ejecuta un smoke test con una API temporal en puerto local libre. Propaga fallos y cierra exclusivamente su proceso, incluso al fallar. |
| `scripts/smoke-test.ps1` | Comprueba la base de reservas en una instancia vacía, registra los IDs creados y limpia solo esas reservas. Si hay reservas ajenas, aborta antes de escribir. La disponibilidad y el filtro quedan cubiertos por los tests de integración y el recorrido final. |
| `.github/workflows/ci.yml` | Configuración versionada que restaura, compila y prueba con .NET 10 en push a la rama principal y pull request. No requiere secretos ni despliegue. |
| `.github/pull_request_template.md` | Guía para describir comportamiento, spec y resultados de validación; no publica un PR. |
| README | Requisitos, comandos, todos los endpoints realmente implementados, datos semilla y límites locales. |

En la ruta desde cero deben crearse las piezas que falten. En la copia del curso se conservan scripts y CI correctos. No hace falta modificar el smoke test para duplicar toda la cobertura HTTP; un smoke aprobado por sí solo no demuestra disponibilidad ni filtro.

## Recorrido final de negocio

Cada recorrido comienza con el taller de capacidad 2 y cero reservas; no reiniciar entre pasos. Todas las consultas correctas son 200 y los errores usan los contratos de sus specs.

| ID | Acción | Resultado verificable |
|---|---|---|
| INT-01 | Consultar salud, catálogo, detalle, disponibilidad y filtro `true`. | Salud `ok`; taller inicial visible; capacidad 2, activas 0, disponibles 2, `isFull=false`. |
| INT-02 | Enviar nombre vacío y correo inválido. | 400 con errores; disponibles sigue en 2. |
| INT-03 | Reservar con `"  Ana  "`, `" ANA@EXAMPLE.COM "`. | 201, nombre y correo normalizados, `Location`; lecturas pasan a 1 activa y 1 lugar. |
| INT-04 | Repetir `ana@example.com`. | 409 `duplicate_reservation`; no cambia el estado. |
| INT-05 | Reservar para Luis con otro correo y luego para Sofía con un tercero. | Luis 201; Sofía 409 `workshop_full`; activas 2, disponibles 0, `isFull=true`. |
| INT-06 | Consultar filtro `true`, sin parámetro y `false`. | `true` es `[]`; los otros dos contienen el taller lleno con sus cinco campos. |
| INT-07 | Cancelar el ID de Ana. | 204 vacío; lecturas 1/1, `isFull=false`; el taller reaparece con `true`. |
| INT-08 | Repetir DELETE del ID de Ana y después reservar el mismo correo. | DELETE 404 `reservation_not_found`; POST 201 con nuevo ID; Luis conserva su reserva y el taller vuelve a estar lleno. |
| INT-09 | Consultar GUID desconocido en detalle y disponibilidad; reservar en ese GUID con DTO válido. | 404 `workshop_not_found` en los tres casos; no cambia el taller inicial. |
| INT-10 | Consultar `onlyAvailable=quizas` y `onlyAvailable=`. | 400 ProblemDetails en ambos; el estado permanece igual. |
| INT-11 | Inspeccionar catálogo, detalle y disponibilidad tras las reservas. | Solo los campos de sus contratos, sin datos de participantes; dentro de cada respuesta las cuentas son coherentes. |
| INT-12 | Crear otro host independiente al terminar el recorrido. | Reinicia en 0 activas y 2 lugares; se documenta la pérdida deliberada de datos al reiniciar. |

## Criterios de integración y entrega

| ID | Comprobación | Resultado requerido |
|---|---|---|
| INT-13 | Ejecutar la suite de pruebas integrada y contrastar la visión aceptada. | Pasan los criterios técnicos de las etapas 01–08, incluyendo contención de reservas y cancelaciones, dentro del alcance de la 00; no hay pruebas omitidas para esconder errores. |
| INT-14 | Ejecutar `./scripts/verify.ps1` sin omitir smoke. | Exitosa la restauración, compilación, suite y comprobación HTTP; la API temporal termina. Registrar salida y conteo real. |
| INT-15 | Revisar scripts y comprobar sus rutas de error en un entorno temporal. | Fallos se propagan; cleanup en `finally`; ninguna API ajena se detiene. Smoke con estado ocupado aborta sin modificarlo. |
| INT-16 | Ejecutar arranque y peticiones según README. | Todos los ejemplos y endpoints corresponden al proyecto final; un nuevo usuario puede reproducirlos. |
| INT-17 | Inspeccionar CI y la plantilla de PR. | Configuración coherente con solución, SDK y comandos locales. Una inspección local no se reporta como ejecución de GitHub Actions. |
| INT-18 | Revisar diff final y evidencia con el revisor. | Hallazgos confirmados corregidos y comprobados; evidencia por criterio; aceptación humana registrada o claramente pendiente. |

INT-13 no obliga a crear un test por cada fila si un caso cubre varios criterios. Sí exige trazabilidad y todas las comprobaciones pertinentes. El número histórico de tests no es una meta de cobertura.

## Reparto multiagente

| Responsable | Propiedad y trabajo |
|---|---|
| Planificador | Solo lectura; verifica dependencias y faltantes de cobertura, documentación y scripts. |
| Coordinador | `scripts/verify.ps1`, `scripts/smoke-test.ps1`, `.github/workflows/ci.yml`, `.github/pull_request_template.md` y README, si requieren creación o ajuste; después integra y ejecuta. |
| Implementador | `src/Talleres.Api/Talleres.Api.http`. Para un defecto de negocio, recibe una corrección acotada y los archivos concretos del contrato afectado. |
| QA | `tests/Talleres.Api.Tests/EndToEndTests.cs`; otros archivos de tests solo por asignación expresa para una cobertura faltante. |
| Revisor | Solo lectura del diff final, contratos pertinentes, configuración y resultados. |

No compilar mientras algún responsable esté escribiendo, incluido el coordinador. Si una dependencia aún no existe, esta spec no la declara aprobada: en la ruta completa se vuelve a su etapa; al ejecutar solo esta spec se informa el impedimento sin ampliar automáticamente el alcance.

## Verificación y terminado

```powershell
./scripts/verify.ps1
```

Después del script, iniciar la API local para comprobar las peticiones documentadas. La matriz de entrega distingue `comprobado`, `falló` y `pendiente` por criterio, indicando prueba o comando y resultado real. `-SkipSmoke` es útil para un diagnóstico parcial, pero no satisface INT-14.

La API final sigue siendo local, sin autenticación ni persistencia y con bloqueo limitado a una instancia. Bugbot es una práctica opcional de revisión del curso, no un requisito para que funcione la API ni una revisión remota que pueda darse por ejecutada. Commits, publicación y fusiones requieren una instrucción específica. **Decisiones abiertas:** ninguna.

## Prompt de entrada

```text
Desarrolla specs/08-verificacion-final.md siguiendo docs/flujo-multiagente.md.
Usa planificador, implementador, qa y revisor. Confirma las dependencias
y presenta el plan para revisión. Tras aceptarlo, completa los faltantes,
ejecuta la verificación integrada y revisa el diff final. Entrega la matriz
INT-01 a INT-18 con evidencia real y pendientes para mi aceptación.
```
