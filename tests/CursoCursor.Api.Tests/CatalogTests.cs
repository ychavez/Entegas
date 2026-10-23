using System.Net;
using System.Text.Json;
using Xunit;

namespace CursoCursor.Api.Tests;

/// <summary>
/// Consultas HTTP del catálogo de talleres y regresión mínima de salud, errores y OpenAPI.
/// El candado y la instantánea de CAT-07 los revisa el revisor; aquí solo se observa el cupo publicado.
/// </summary>
public sealed class CatalogTests
{
    private const string IdTallerInicial = "11111111-1111-1111-1111-111111111111";
    private const string IdInexistente = "22222222-2222-2222-2222-222222222222";
    private const string RutaCatalogo = "/api/workshops";

    /// <summary>CAT-01. Catálogo en un host nuevo.</summary>
    [Fact]
    public async Task Catalogo_EnHostNuevo_DevuelveElTallerInicial()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await ReadAsync(client, RutaCatalogo);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertJsonContentType(response);

        using var document = JsonDocument.Parse(response.Body);
        var workshops = document.RootElement;
        Assert.Equal(JsonValueKind.Array, workshops.ValueKind);
        Assert.Equal(1, workshops.GetArrayLength());
        AssertSeedWorkshop(workshops[0]);
    }

    /// <summary>CAT-02. Ficha del taller inicial.</summary>
    [Fact]
    public async Task Detalle_DelIdInicial_DevuelveLosMismosCincoValores()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await ReadAsync(client, RutaDetalle(IdTallerInicial));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertJsonContentType(response);

        using var document = JsonDocument.Parse(response.Body);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        AssertSeedWorkshop(document.RootElement);
    }

    /// <summary>CAT-03. GUID válido que no es el taller semilla.</summary>
    [Fact]
    public async Task Detalle_GuidInexistente_Devuelve404WorkshopNotFound()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await ReadAsync(client, RutaDetalle(IdInexistente));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemContentType(response);

        using var document = JsonDocument.Parse(response.Body);
        var problem = document.RootElement;
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Equal("workshop_not_found", problem.GetProperty("code").GetString());
        Assert.Equal("Recurso no encontrado", problem.GetProperty("title").GetString());
        Assert.Equal("No se encontró el taller.", problem.GetProperty("detail").GetString());
    }

    /// <summary>CAT-04. Id que no es GUID: 404 de enrutamiento, sin el código de negocio.</summary>
    [Fact]
    public async Task Detalle_IdNoGuid_Devuelve404YNo500()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await ReadAsync(client, "/api/workshops/no-es-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertProblemContentType(response);
        Assert.DoesNotContain("workshop_not_found", response.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", response.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", response.Body, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(response.Body);
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", document.RootElement.GetProperty("title").GetString());
        Assert.False(document.RootElement.TryGetProperty("code", out _));
    }

    /// <summary>CAT-05. Repetir catálogo y ficha no crea una reserva ni cambia el cuerpo.</summary>
    [Fact]
    public async Task CatalogoYDetalle_Repetidos_ConservanCuerpoYCeroReservas()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var catalogoPrimero = await ReadAsync(client, RutaCatalogo);
        var catalogoSegundo = await ReadAsync(client, RutaCatalogo);

        Assert.Equal(HttpStatusCode.OK, catalogoPrimero.StatusCode);
        Assert.Equal(HttpStatusCode.OK, catalogoSegundo.StatusCode);
        Assert.Equal(catalogoPrimero.Body, catalogoSegundo.Body);

        using var catalogo = JsonDocument.Parse(catalogoSegundo.Body);
        Assert.Equal(JsonValueKind.Array, catalogo.RootElement.ValueKind);
        Assert.Equal(1, catalogo.RootElement.GetArrayLength());
        AssertSeedWorkshop(catalogo.RootElement[0]);

        var rutaDetalle = RutaDetalle(IdTallerInicial);
        var detallePrimero = await ReadAsync(client, rutaDetalle);
        var detalleSegundo = await ReadAsync(client, rutaDetalle);

        Assert.Equal(HttpStatusCode.OK, detallePrimero.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detalleSegundo.StatusCode);
        Assert.Equal(detallePrimero.Body, detalleSegundo.Body);

        using var detalle = JsonDocument.Parse(detalleSegundo.Body);
        AssertSeedWorkshop(detalle.RootElement);
    }

    /// <summary>CAT-06. Cada taller publicado tiene exactamente cinco propiedades.</summary>
    [Fact]
    public async Task CatalogoYDetalle_TienenExactamenteCincoPropiedades()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var catalogoResponse = await ReadAsync(client, RutaCatalogo);
        Assert.Equal(HttpStatusCode.OK, catalogoResponse.StatusCode);
        using var catalogo = JsonDocument.Parse(catalogoResponse.Body);
        Assert.Equal(JsonValueKind.Array, catalogo.RootElement.ValueKind);
        Assert.NotEqual(0, catalogo.RootElement.GetArrayLength());
        foreach (var workshop in catalogo.RootElement.EnumerateArray())
            AssertExactlyFiveWorkshopProperties(workshop);

        var detalleResponse = await ReadAsync(client, RutaDetalle(IdTallerInicial));
        Assert.Equal(HttpStatusCode.OK, detalleResponse.StatusCode);
        using var detalle = JsonDocument.Parse(detalleResponse.Body);
        AssertExactlyFiveWorkshopProperties(detalle.RootElement);
    }

    /// <summary>CAT-07. Solo la igualdad publicada; el candado y la instantánea los revisa el revisor.</summary>
    [Fact]
    public async Task Cupo_DisponibleEsCapacidadMenosReservasActivas()
    {
        // HTTP no demuestra el candado ni la instantánea; eso lo revisa el revisor.
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var catalogoResponse = await ReadAsync(client, RutaCatalogo);
        Assert.Equal(HttpStatusCode.OK, catalogoResponse.StatusCode);
        using var catalogo = JsonDocument.Parse(catalogoResponse.Body);
        Assert.Equal(JsonValueKind.Array, catalogo.RootElement.ValueKind);
        Assert.NotEqual(0, catalogo.RootElement.GetArrayLength());
        foreach (var workshop in catalogo.RootElement.EnumerateArray())
            AssertAvailableSeatsFollowCapacity(workshop);

        var detalleResponse = await ReadAsync(client, RutaDetalle(IdTallerInicial));
        Assert.Equal(HttpStatusCode.OK, detalleResponse.StatusCode);
        using var detalle = JsonDocument.Parse(detalleResponse.Body);
        AssertAvailableSeatsFollowCapacity(detalle.RootElement);
    }

    /// <summary>CAT-08. Salud del host.</summary>
    [Fact]
    public async Task Health_Devuelve200YStatusOk()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await ReadAsync(client, "/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertJsonContentType(response);
        using var document = JsonDocument.Parse(response.Body);
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
    }

    /// <summary>CAT-08. Ruta inexistente, sin traza.</summary>
    [Fact]
    public async Task RutaInexistente_Devuelve404SinTraza()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await ReadAsync(client, "/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemContentType(response);
        Assert.DoesNotContain("   at ", response.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", response.Body, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(response.Body);
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", document.RootElement.GetProperty("title").GetString());
    }

    /// <summary>
    /// Production publica salud y catálogo. El documento OpenAPI sigue apagado.
    /// </summary>
    [Fact]
    public async Task Produccion_PublicaSaludYCatalogo_YOcultaOpenApi()
    {
        await using var factory = new ProductionApiFactory();
        using var client = factory.CreateClient();

        var health = await ReadAsync(client, "/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        AssertJsonContentType(health);
        using (var healthDocument = JsonDocument.Parse(health.Body))
            Assert.Equal("ok", healthDocument.RootElement.GetProperty("status").GetString());

        var catalogo = await ReadAsync(client, RutaCatalogo);
        Assert.Equal(HttpStatusCode.OK, catalogo.StatusCode);
        AssertJsonContentType(catalogo);
        using (var catalogoDocument = JsonDocument.Parse(catalogo.Body))
        {
            Assert.Equal(JsonValueKind.Array, catalogoDocument.RootElement.ValueKind);
            Assert.Equal(1, catalogoDocument.RootElement.GetArrayLength());
            AssertSeedWorkshop(catalogoDocument.RootElement[0]);
        }

        var detalle = await ReadAsync(client, RutaDetalle(IdTallerInicial));
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        using (var detalleDocument = JsonDocument.Parse(detalle.Body))
            AssertSeedWorkshop(detalleDocument.RootElement);

        var inexistente = await ReadAsync(client, RutaDetalle(IdInexistente));
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        AssertProblemContentType(inexistente);
        using (var inexistenteDocument = JsonDocument.Parse(inexistente.Body))
        {
            Assert.Equal(404, inexistenteDocument.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("workshop_not_found", inexistenteDocument.RootElement.GetProperty("code").GetString());
        }

        var rutaInexistente = await ReadAsync(client, "/no-existe");
        Assert.Equal(HttpStatusCode.NotFound, rutaInexistente.StatusCode);
        AssertProblemContentType(rutaInexistente);
        Assert.DoesNotContain("   at ", rutaInexistente.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", rutaInexistente.Body, StringComparison.OrdinalIgnoreCase);
        using (var rutaDocument = JsonDocument.Parse(rutaInexistente.Body))
            Assert.Equal("Not Found", rutaDocument.RootElement.GetProperty("title").GetString());

        var openApi = await ReadAsync(client, "/openapi/v1.json");
        Assert.Equal(HttpStatusCode.NotFound, openApi.StatusCode);
    }

    /// <summary>
    /// El catálogo no acepta escritura: POST responde 405 y el taller inicial sigue igual.
    /// </summary>
    [Fact]
    public async Task PostAlCatalogo_Devuelve405SinTrazaYNoCreaTaller()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, RutaCatalogo);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.StartsWith(
            "application/problem+json",
            response.Content.Headers.ContentType?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Content.Headers.TryGetValues("Allow", out var allow));
        Assert.NotNull(allow);
        Assert.Contains(allow, value => value.Contains("GET", StringComparison.OrdinalIgnoreCase));

        using (var document = JsonDocument.Parse(body))
        {
            Assert.Equal(405, document.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Method Not Allowed", document.RootElement.GetProperty("title").GetString());
            Assert.False(document.RootElement.TryGetProperty("code", out _));
        }

        var catalogo = await ReadAsync(client, RutaCatalogo);
        Assert.Equal(HttpStatusCode.OK, catalogo.StatusCode);
        using var catalogoDocument = JsonDocument.Parse(catalogo.Body);
        Assert.Equal(1, catalogoDocument.RootElement.GetArrayLength());
        AssertSeedWorkshop(catalogoDocument.RootElement[0]);
    }

    /// <summary>CAT-08. OpenAPI en Development incluye salud, catálogo y el 404 de la ficha.</summary>
    [Fact]
    public async Task OpenApi_EnDevelopment_IncluyeSaludCatalogoY404DeLaFicha()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();

        var response = await ReadAsync(client, "/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(response.Body);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/health", out _), "El documento OpenAPI no incluye /health.");
        Assert.True(paths.TryGetProperty("/api/workshops", out _), "El documento OpenAPI no incluye /api/workshops.");
        Assert.True(DetailDocumentsNotFound(paths), "El documento OpenAPI no incluye un 404 de la ficha.");
    }

    private static void AssertSeedWorkshop(JsonElement workshop)
    {
        Assert.Equal("11111111-1111-1111-1111-111111111111", workshop.GetProperty("id").GetString());
        Assert.Equal("Taller de Cursor", workshop.GetProperty("title").GetString());
        Assert.Equal(2, workshop.GetProperty("capacity").GetInt32());
        Assert.Equal(0, workshop.GetProperty("activeReservations").GetInt32());
        Assert.Equal(2, workshop.GetProperty("availableSeats").GetInt32());
    }

    private static void AssertExactlyFiveWorkshopProperties(JsonElement workshop)
    {
        Assert.Equal(JsonValueKind.Object, workshop.ValueKind);

        var names = new List<string>();
        foreach (var property in workshop.EnumerateObject())
            names.Add(property.Name);

        // Cuenta las propiedades presentes; no basta con leer las cinco esperadas.
        Assert.Equal(5, names.Count);
        names.Sort(StringComparer.Ordinal);
        Assert.Equal(
            new[] { "activeReservations", "availableSeats", "capacity", "id", "title" },
            names);
    }

    private static void AssertAvailableSeatsFollowCapacity(JsonElement workshop)
    {
        var capacity = workshop.GetProperty("capacity").GetInt32();
        var activeReservations = workshop.GetProperty("activeReservations").GetInt32();
        Assert.Equal(capacity - activeReservations, workshop.GetProperty("availableSeats").GetInt32());
    }

    private static void AssertJsonContentType(CapturedResponse response)
    {
        Assert.Equal("application/json", response.MediaType);
    }

    private static void AssertProblemContentType(CapturedResponse response)
    {
        Assert.StartsWith(
            "application/problem+json",
            response.ContentType,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool DetailDocumentsNotFound(JsonElement paths)
    {
        foreach (var path in paths.EnumerateObject())
        {
            if (!IsWorkshopDetailPath(path.Name) || path.Value.ValueKind != JsonValueKind.Object)
                continue;

            if (!path.Value.TryGetProperty("get", out var get) || get.ValueKind != JsonValueKind.Object)
                continue;

            if (get.TryGetProperty("responses", out var responses)
                && responses.ValueKind == JsonValueKind.Object
                && responses.TryGetProperty("404", out _))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWorkshopDetailPath(string path)
    {
        const string prefix = "/api/workshops/";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var remainder = path[prefix.Length..];
        return remainder.Length > 0 && !remainder.Contains('/');
    }

    private static string RutaDetalle(string id) => $"{RutaCatalogo}/{id}";

    private static async Task<CapturedResponse> ReadAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        return new CapturedResponse(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            response.Content.Headers.ContentType?.ToString() ?? string.Empty,
            body);
    }

    private sealed record CapturedResponse(
        HttpStatusCode StatusCode,
        string MediaType,
        string ContentType,
        string Body);
}
