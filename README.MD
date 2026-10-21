# API de talleres (curso Cursor)

API local para consultar talleres y reservar lugares. Hoy existe solo la **estructura inicial** del proyecto: el host arranca y se puede comprobar con pruebas, pero **aún no hay catálogo ni reservas**.

El caso sirve para practicar planificación, implementación, pruebas y revisión con agentes. La API no ejecuta modelos de inteligencia artificial.

## Qué incluye ahora

- Una aplicación ASP.NET Core 10 (Minimal APIs) en .NET 10.
- Un almacén en memoria vacío, listo para reglas de cupo y duplicados más adelante.
- Documento OpenAPI en Development en `/openapi/v1.json` (sin interfaz Swagger).
- Pruebas con xUnit y `WebApplicationFactory`.
- Integración continua en GitHub Actions: restaurar, compilar y probar.

## Requisitos

- SDK de .NET 10

## Comandos

Desde la raíz del repositorio:

```powershell
dotnet restore CursoCursor.slnx
dotnet build CursoCursor.slnx
dotnet test CursoCursor.slnx
```

Para ejecutar el host en Development:

```powershell
dotnet run --project src/CursoCursor.Api/CursoCursor.Api.csproj
```

Con el host en marcha, `GET /openapi/v1.json` describe la API. Todavía no hay rutas de talleres.

## Solución

- `CursoCursor.slnx` — solución
- `src/CursoCursor.Api` — API
- `tests/CursoCursor.Api.Tests` — pruebas
