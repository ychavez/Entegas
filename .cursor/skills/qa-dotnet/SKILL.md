---
name: qa-dotnet
description: >-
  Revisa una solución .NET, identifica clases sin pruebas, genera tests con
  xUnit y entrega un reporte de calidad. Se aplica al revisar cobertura de
  tipos, clases sin pruebas, tests xUnit o un reporte de calidad .NET.
  No modifica código de producción sin aprobación.
disable-model-invocation: true
---

# QA .NET

No modificar código de producción sin aprobación.

Código de producción es todo lo que no sea un proyecto de pruebas: `src/`, proyectos web o de librería, `Program.cs` y sus `.csproj`. Escribir o corregir tests no requiere otra aprobación. Proponer un cambio de producción dentro del reporte tampoco lo modifica.

## Alcance

1. Localiza la solución (`.slnx` o `.sln`) y sus proyectos.
2. Inventaría tipos con comportamiento en proyectos de producción.
3. Marca los que ninguna prueba ejercita.
4. Genera pruebas xUnit solo en el proyecto de tests existente.
5. Ejecuta `dotnet test` y entrega el reporte con el resultado real.

En este repositorio la solución es `CursoCursor.slnx`, la API está en `src/CursoCursor.Api` y las pruebas en `tests/CursoCursor.Api.Tests` (xUnit, `WebApplicationFactory`). Sigue los nombres y el estilo de los tests ya presentes.

## Inventario

Incluye `class` y `record` de producción con métodos, ramas o reglas observables. Excluye `bin/`, `obj/`, código generado y atributos de compilador.

Separa en el reporte:

- **Sin prueba:** ninguna prueba provoca un resultado observable de ese tipo (método público, ruta HTTP, estado o campo significativo). Que el nombre aparezca en un using o en un comentario no cuenta.
- **Contrato de datos:** records o DTOs sin lógica. Menciónalos; no generes un test salvo que el usuario lo pida.
- **Cubierta:** indica qué prueba ejercita el comportamiento.

## Generar pruebas

- Usa xUnit (`[Fact]` o `[Theory]`). No cambies a otro framework ni añadas paquetes.
- Escribe en el proyecto de tests que ya referencia ese proyecto. Si no existe, detente y pídelo; no crees un proyecto nuevo por tu cuenta.
- Comprueba el comportamiento observable. En esta API, prefiere HTTP con el factory de pruebas ya existente. Cada caso debe poder fallar si la implementación está mal.
- No relajes aserciones ni edites una prueba para ocultar un fallo.
- No abras `internal` ni cambies visibilidad en producción para poder probar.
- Si un caso solo puede existir cambiando producción, déjalo en el reporte como pendiente y espera aprobación.

## Ejecución

Después de escribir pruebas, ejecuta una sola vez:

```powershell
dotnet test CursoCursor.slnx
```

Si la solución detectada tiene otro nombre, usa ese archivo. No lances dos compilaciones a la vez sobre el mismo checkout. Copia el comando, el código de salida y el conteo. Si no llegaste a ejecutarlo, dilo; no inventes un resultado verde.

Si una prueba falla por un defecto de producción, consérvala y descríbela. No corrijas `src/` hasta recibir aprobación explícita.

## Reporte

Responde con este reporte:

```markdown
# Reporte de calidad .NET

## Alcance
- Solución y proyectos revisados

## Clases sin pruebas
- Tipo, archivo, comportamiento sin cubrir y prueba propuesta

## Contratos de datos
- Tipos sin lógica, solo como inventario

## Pruebas generadas
- Archivo y comportamiento que cubre cada caso

## Ejecución
- Comando, exit code y conteo real (o "no ejecutado")

## Producción
- Sin cambios. Si hace falta un cambio, archivo, motivo y la pregunta de aprobación

## Pendientes
- Huecos que no se cubrieron y por qué
```
