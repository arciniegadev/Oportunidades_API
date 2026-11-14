using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Oportunidades.Api.Security;
using Oportunidades.IntegrationTests.Infrastructure;

namespace Oportunidades.IntegrationTests;

public sealed class OportunidadesEndpointsTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public OportunidadesEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => TestDatabase.CleanAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Post_Valido_Devuelve201_Y_Get_Devuelve200()
    {
        var token = CreateToken(Roles.Editor);
        var nombre = $"Prueba integracion {Guid.NewGuid():N}";

        var request = new
        {
            nombre = $"  {nombre}  ",
            cliente = "  Acme S.A.  ",
            montoEstimado = 125000.50m
        };

        using var postRequest = CreateRequest(HttpMethod.Post, "/api/oportunidades", token, request);
        using var postResponse = await _client.SendAsync(postRequest);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        Assert.NotNull(postResponse.Headers.Location);

        var creada = await postResponse.Content.ReadFromJsonAsync<OportunidadDto>(JsonOptions);
        Assert.NotNull(creada);
        Assert.True(creada.Id > 0);
        Assert.Equal(nombre, creada.Nombre);
        Assert.Equal("Acme S.A.", creada.Cliente);
        Assert.Equal(125000.50m, creada.MontoEstimado);
        Assert.Equal("Nueva", creada.Estado);
        Assert.Contains($"/api/oportunidades/{creada.Id}", postResponse.Headers.Location!.ToString());

        using var getRequest = CreateRequest(HttpMethod.Get, $"/api/oportunidades/{creada.Id}", token);
        using var getResponse = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var obtenida = await getResponse.Content.ReadFromJsonAsync<OportunidadDto>(JsonOptions);
        Assert.NotNull(obtenida);
        Assert.Equal(creada.Id, obtenida.Id);
        Assert.Equal(nombre, obtenida.Nombre);
        Assert.Equal(creada.Cliente, obtenida.Cliente);
        Assert.Equal(creada.MontoEstimado, obtenida.MontoEstimado);
        Assert.Equal("Nueva", obtenida.Estado);
    }

    [Fact]
    public async Task Post_Invalido_Devuelve400()
    {
        var token = CreateToken(Roles.Editor);

        var request = new
        {
            nombre = "",
            cliente = "Acme",
            montoEstimado = 10.999m
        };

        using var postRequest = CreateRequest(HttpMethod.Post, "/api/oportunidades", token, request);
        using var response = await _client.SendAsync(postRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("nombre", out _));
        Assert.True(errors.TryGetProperty("montoEstimado", out _));
    }

    [Fact]
    public async Task Get_SinToken_Devuelve401()
    {
        using var request = CreateRequest(HttpMethod.Get, "/api/oportunidades/1", token: null);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_ConTokenConsulta_Devuelve403()
    {
        var token = CreateToken(Roles.Consulta);

        var request = new
        {
            nombre = "Intento sin permiso",
            cliente = "Acme",
            montoEstimado = 100m
        };

        using var postRequest = CreateRequest(HttpMethod.Post, "/api/oportunidades", token, request);
        using var response = await _client.SendAsync(postRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patch_TransicionInvalida_Devuelve409()
    {
        var token = CreateToken(Roles.Editor);
        var creada = await CrearOportunidadAsync(token);

        using var patchRequest = CreateRequest(
            HttpMethod.Patch,
            $"/api/oportunidades/{creada.Id}/estado",
            token,
            new { estado = "Ganada" });

        using var response = await _client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "Cambio de estado no permitido",
            body.RootElement.GetProperty("title").GetString());
    }

    private string CreateToken(string role) =>
        TokenFactory.Create(role, _factory.SigningKey, ApiFactory.Issuer, ApiFactory.Audience);

    private async Task<OportunidadDto> CrearOportunidadAsync(string token)
    {
        var request = new
        {
            nombre = $"Oportunidad {Guid.NewGuid():N}",
            cliente = "Cliente de prueba",
            montoEstimado = 250m
        };

        using var postRequest = CreateRequest(HttpMethod.Post, "/api/oportunidades", token, request);
        using var response = await _client.SendAsync(postRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var creada = await response.Content.ReadFromJsonAsync<OportunidadDto>(JsonOptions);
        Assert.NotNull(creada);

        return creada;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        string? token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }
}
