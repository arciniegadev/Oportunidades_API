using System.Data;
using Microsoft.Data.SqlClient;
using Oportunidades.Api.Data;
using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Repositories;

/// <summary>
/// Implementación ADO.NET del acceso a datos. Las cuatro operaciones se
/// ejecutan exclusivamente mediante procedimientos almacenados.
/// </summary>
public sealed class OportunidadRepository(IDbConnectionFactory connections) : IOportunidadRepository
{
    private const string SpCrear = "dbo.usp_Oportunidades_Crear";
    private const string SpObtenerPorId = "dbo.usp_Oportunidades_ObtenerPorId";
    private const string SpListar = "dbo.usp_Oportunidades_Listar";
    private const string SpCambiarEstado = "dbo.usp_Oportunidades_CambiarEstado";

    public async Task<IReadOnlyList<Oportunidad>> ListarAsync(
        EstadoOportunidad? estado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connections.Create();
        await using var command = CrearComando(connection, SpListar);

        command.Parameters.Add("@Estado", SqlDbType.NVarChar, 20).Value =
            estado is null ? DBNull.Value : estado.Value.ToString();

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var resultado = new List<Oportunidad>();
        while (await reader.ReadAsync(cancellationToken))
        {
            resultado.Add(Mapear(reader));
        }

        return resultado;
    }

    public async Task<Oportunidad?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connections.Create();
        await using var command = CrearComando(connection, SpObtenerPorId);

        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        return await LeerUnoAsync(connection, command, cancellationToken);
    }

    public async Task<Oportunidad?> CrearAsync(
        string nombre,
        string cliente,
        decimal montoEstimado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connections.Create();
        await using var command = CrearComando(connection, SpCrear);

        command.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = nombre.Trim();
        command.Parameters.Add("@Cliente", SqlDbType.NVarChar, 120).Value = cliente.Trim();
        command.Parameters.Add(new SqlParameter("@MontoEstimado", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = montoEstimado
        });

        return await LeerUnoAsync(connection, command, cancellationToken);
    }

    public async Task<Oportunidad?> CambiarEstadoAsync(
        int id,
        EstadoOportunidad estadoEsperado,
        EstadoOportunidad nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connections.Create();
        await using var command = CrearComando(connection, SpCambiarEstado);

        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@EstadoEsperado", SqlDbType.NVarChar, 20).Value = estadoEsperado.ToString();
        command.Parameters.Add("@NuevoEstado", SqlDbType.NVarChar, 20).Value = nuevoEstado.ToString();

        // Sin filas = el estado persistido ya no era el esperado (o ya no existe).
        return await LeerUnoAsync(connection, command, cancellationToken);
    }

    private static SqlCommand CrearComando(SqlConnection connection, string procedimiento) =>
        new(procedimiento, connection) { CommandType = CommandType.StoredProcedure };

    private static async Task<Oportunidad?> LeerUnoAsync(
        SqlConnection connection,
        SqlCommand command,
        CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? Mapear(reader) : null;
    }

    // Orden de columnas: Id, Nombre, Cliente, MontoEstimado, Estado, FechaCreacionUtc.
    private static Oportunidad Mapear(SqlDataReader reader) =>
        new()
        {
            Id = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            Cliente = reader.GetString(2),
            MontoEstimado = reader.GetDecimal(3),
            Estado = Enum.Parse<EstadoOportunidad>(reader.GetString(4)),
            FechaCreacionUtc = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)
        };
}
