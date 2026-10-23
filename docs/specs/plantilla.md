# [Nombre de la funcionalidad]

**Tipo:** funcional / técnica / calidad funcional. **Estado:** borrador / lista para planificar / en desarrollo / aceptada. **Estado del código:** inexistente / parcial / existente, por comprobar.

Esta plantilla se copia a un archivo nuevo. Sustituye todos los marcadores; no desarrolles la plantilla directamente. El proceso común está en [el protocolo multiagente](../docs/flujo-multiagente.md).

## Historia de usuario u objetivo

Como [persona o rol], quiero [capacidad observable], para [beneficio].

## Alcance y exclusiones

- Incluye: [comportamientos que agrega o cambia].
- Fuera de alcance: [lo que no forma parte de este incremento].

## Comportamiento y contrato

[Describe entradas, salidas, reglas de negocio, estados y errores. Si hay una API, indica método, ruta, campos, códigos HTTP y ejemplos. Identifica qué comportamiento existente debe conservarse.]

## Criterios de aceptación

Usa un prefijo propio y estable para los IDs. Incluye caso normal, errores y casos límite pertinentes.

| ID | Dado / cuando | Resultado observable |
|---|---|---|
| FUNC-01 | [Estado inicial y acción] | [Resultado esperado comprobable] |
| FUNC-02 | [Error o caso límite] | [Resultado esperado comprobable] |

## Dependencias y contexto técnico

[Comportamientos previos necesarios y archivos relevantes para leer. Indica restricciones de edición conocidas; el planificador propone el reparto concreto para esta funcionalidad.]

## Verificación

[Cómo observar cada criterio, qué regresiones conservar y comandos conocidos. Separar comprobaciones propuestas de resultados ejecutados.]

## Decisiones abiertas

[Preguntas que afecten al contrato; escribe «Ninguna» cuando estén resueltas. El coordinador debe resolverlas con el humano antes de implementar.]

## Reparto y orden de integración

[Archivos propuestos para implementador y QA, tareas de solo lectura de planificador y revisor, y archivos de raíz, scripts o configuración que deba poseer el coordinador. Indicar qué tareas pueden escribir en paralelo y qué entrega necesitan primero. En una creación desde cero, el esqueleto precede al trabajo paralelo. El plan confirma las rutas exactas; se sigue el protocolo común.]

## Prompt de entrada

```text
Desarrolla specs/[nombre-del-archivo].md siguiendo docs/flujo-multiagente.md.
Usa planificador, implementador, qa y revisor. Presenta el plan para revisión
y, una vez aceptado, completa el incremento con evidencia por criterio.
```
