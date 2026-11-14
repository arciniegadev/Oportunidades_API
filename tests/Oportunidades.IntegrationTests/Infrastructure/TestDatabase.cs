using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Oportunidades.IntegrationTests.Infrastructure;

internal static class TestDatabase
{
    private const string DatabaseName = "OportunidadesDb_Test";

    private static readonly string BaseConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__OportunidadesDb_Test")
        ?? "Server=localhost;Database=OportunidadesDb_Test;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";

    public static string ConnectionString { get; } = BuildConnectionString(DatabaseName);

    private static string MasterConnectionString => BuildConnectionString("master");

    public static async Task EnsureCreatedAsync()
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        foreach (var scriptName in new[] { "01_crear_base.sql", "02_stored_procedures.sql" })
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "database", scriptName);
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException(
                    $"No se encontró el script SQL de pruebas en '{scriptPath}'.",
                    scriptPath);
            }

            var script = AdaptScript(await File.ReadAllTextAsync(scriptPath));

            foreach (var batch in SplitBatches(script))
            {
                using var command = new SqlCommand(batch, connection)
                {
                    CommandTimeout = 120
                };

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public static async Task CleanAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("DELETE FROM dbo.Oportunidades;", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string BuildConnectionString(string database) =>
        new SqlConnectionStringBuilder(BaseConnectionString)
        {
            InitialCatalog = database
        }.ConnectionString;

    private static string AdaptScript(string script)
    {
        // Solo se cambia el nombre de la base de datos. No se toca el nombre de la
        // tabla ni el de los procedimientos, que también contienen "Oportunidades".
        script = Regex.Replace(
            script,
            @"DB_ID\(N'Oportunidades'\)",
            $"DB_ID(N'{DatabaseName}')",
            RegexOptions.IgnoreCase);

        script = Regex.Replace(
            script,
            @"CREATE\s+DATABASE\s+Oportunidades\b",
            $"CREATE DATABASE [{DatabaseName}]",
            RegexOptions.IgnoreCase);

        return Regex.Replace(
            script,
            @"^\s*USE\s+Oportunidades\s*;",
            $"USE [{DatabaseName}];",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
    }

    private static IEnumerable<string> SplitBatches(string script) =>
        Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch));
}
