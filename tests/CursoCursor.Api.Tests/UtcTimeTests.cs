using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace CursoCursor.Api.Tests;

/// <summary>
/// Hora actual en UTC: la respuesta es un instante con desplazamiento cero.
/// </summary>
public sealed class UtcTimeTests
{
    [Fact]
    public async Task HoraUtc_Devuelve200ConInstanteActual()
    {
        await using var factory = new DevelopmentApiFactory();
        using var client = factory.CreateClient();
        var antes = DateTimeOffset.UtcNow;

        using var response = await client.GetAsync("/api/time");
        var despues = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", MediaTypeHeaderValue.Parse(response.Content.Headers.ContentType!.ToString()).MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var utc = document.RootElement.GetProperty("utc").GetDateTimeOffset();

        Assert.Equal(TimeSpan.Zero, utc.Offset);
        Assert.InRange(utc, antes.AddSeconds(-1), despues.AddSeconds(1));
    }
}
