*Tipo:** spec de definición del proyecto. **Estado:** idea y decisiones iniciales documentadas, listas para revisar antes de construir. **Dependencias:** ninguna.

## Idea inicial

Construiremos una API para consultar talleres y reservar lugares. Una persona podrá encontrar un taller, consultar su disponibilidad, inscribirse con nombre y correo y cancelar su reserva. El sistema impedirá correos duplicados en un mismo taller y reservas por encima de su capacidad, incluso cuando lleguen solicitudes simultáneas.

El proyecto será el caso práctico del curso de Cursor e inteligencia artificial. Nos permitirá observar cómo un equipo de agentes transforma requisitos en código, pruebas y resultados comprobables. Los agentes ayudan a desarrollar el software; la API no ejecuta modelos de IA.

## Problema, usuarios y objetivo

El organizador necesita que las confirmaciones correspondan a lugares reales y que las cancelaciones recuperen capacidad. La persona interesada necesita saber si hay lugares y recibir una respuesta clara cuando su solicitud no puede aceptarse.

| Participante | Necesidad |
|---|---|
| Persona interesada | Consultar catálogo, detalle y disponibilidad; encontrar talleres con lugares. |
| Persona que reserva | Inscribirse con datos válidos, obtener un identificador y cancelar cuando lo necesite. |
| Organizador | Evitar sobreventa y duplicados; recuperar un lugar tras una cancelación. No se construye una interfaz de administración. |
| Instructor y alumnos | Practicar contexto, specs, planificación, implementación, pruebas y revisión con agentes. |

El resultado será una API local ejecutable y verificable. El éxito se observará en sus respuestas HTTP y en las pruebas, no en el número de archivos producidos ni en que un agente diga que terminó.

## Lenguaje y tecnologías que usaremos

| Área | Elección | Uso en este proyecto |
|---|---|---|
| Lenguaje principal | **C#** | Lógica de talleres y reservas, endpoints y pruebas. |
| Plataforma | **.NET 10**, destino `net10.0` | SDK, compilación, ejecución y herramientas CLI. |
| Framework web | **ASP.NET Core 10 con Minimal APIs** | API HTTP ligera con rutas agrupadas por funcionalidad. |
| Contratos | **HTTP y JSON**, DTO con campos camelCase | Comunicación con clientes; respuestas 200, 201, 204, 400, 404 y 409. |
| Estado | **Colecciones en memoria y servicio singleton por host** | Catálogo inicial y reservas activas, sin base de datos. |
| Concurrencia | **`lock` de C# sobre un único objeto del almacén** | Comprobar duplicados y cupo, registrar y cancelar de forma atómica en una instancia. |
| Validación | **DataAnnotations y `AddValidation`** | Reglas de nombre y correo antes de ejecutar una reserva. |
| Errores | **ProblemDetails de ASP.NET Core** | Respuestas de error estructuradas con códigos de negocio. |
| Descripción de API | **Microsoft.AspNetCore.OpenApi** | Documento `/openapi/v1.json` en Development. Sin interfaz Swagger adicional. |
| Pruebas | **xUnit y Microsoft.AspNetCore.Mvc.Testing** | Pruebas del almacén y peticiones HTTP con `WebApplicationFactory<Program>`. |
| Automatización local | **PowerShell y .NET CLI** | Restaurar, compilar, probar y ejecutar una comprobación HTTP temporal. |
| Control de versiones | **Git** | Inspección de cambios y ramas de trabajo. |
| Integración continua | **GitHub Actions** | Configuración para restaurar, compilar y probar cuando se utilice un repositorio remoto. |
| Desarrollo asistido | **Cursor**, rules y agentes personalizados | Coordinador en el chat principal; planificador, implementador, QA y revisor. |
| Revisión de práctica | **Bugbot**, cuando esté habilitado en el repositorio del instructor | Laboratorio de revisión de PR; no es una dependencia de ejecución de la API. |