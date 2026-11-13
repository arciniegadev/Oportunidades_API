using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Services;

/// <summary>
/// Casos de uso de oportunidades comerciales. El controlador depende de esta
/// abstracción y no de Entity Framework ni de la base de datos.
/// </summary>
public interface IOportunidadService
{
    Task<OportunidadListado> ListarAsync(
        EstadoOportunidad? estado,
        string? cliente,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Oportunidad?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Oportunidad> CrearAsync(
        string nombre,
        string cliente,
        decimal montoEstimado,
        CancellationToken cancellationToken = default);

    Task<Oportunidad?> ActualizarEstadoAsync(
        int id,
        EstadoOportunidad nuevoEstado,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado paginado del listado de oportunidades.
/// </summary>
public sealed record OportunidadListado(
    IReadOnlyList<Oportunidad> Items,
    int Page,
    int PageSize,
    int Total);
