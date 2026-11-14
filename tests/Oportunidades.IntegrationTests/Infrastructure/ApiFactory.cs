using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Oportunidades.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<global::Program>
{
    public const string Issuer = "OportunidadesApi";
    public const string Audience = "OportunidadesApiClientes";

    public ApiFactory()
    {
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // El Program de la API lee la configuración antes de que WebApplicationFactory
        // aplique ConfigureAppConfiguration, por eso estos valores también se publican
        // como variables de entorno del proceso de pruebas.
        Environment.SetEnvironmentVariable("ConnectionStrings__OportunidadesDb", TestDatabase.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);

        // La base de pruebas se crea una sola vez por proceso, antes de iniciar la API.
        Task.Run(TestDatabase.EnsureCreatedAsync).GetAwaiter().GetResult();
    }

    public string SigningKey { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OportunidadesDb"] = TestDatabase.ConnectionString,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:SigningKey"] = SigningKey,
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
            });
        });
    }
}
