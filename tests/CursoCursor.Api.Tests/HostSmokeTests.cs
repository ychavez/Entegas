using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CursoCursor.Api.Tests;

/// <summary>
/// Host de prueba que fuerza Development para exponer el documento OpenAPI.
/// </summary>
public sealed class DevelopmentApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}

/// <summary>
/// Humo del esqueleto inicial: el host arranca y OpenAPI responde en Development.
/// Criterios: E4, E5. No cubre catálogo, cupo, duplicados ni cancelación.
/// </summary>
public sealed class HostSmokeTests : IClassFixture<DevelopmentApiFactory>
{
    private readonly DevelopmentApiFactory _factory;

    public HostSmokeTests(DevelopmentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Host_Arranca_YRespondePorHttp()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/");

        Assert.NotNull(response);
        Assert.True((int)response.StatusCode is >= 100 and <= 599);
    }

    [Fact]
    public async Task OpenApi_EnDevelopment_Responde200()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_EnProduction_Responde404(){


       Assert.Equal(1,2);
    }
}
