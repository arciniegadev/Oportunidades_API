using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Repositories;

/// <summary>
/// Acceso a datos de oportunidades. Todas las operaciones se ejecutan
/// mediante procedimientos almacenados con parámetros tipados.
/// </summary>
public interface IOportunidadRepository
{
    Task<IReadOnlyList<Oportunidad>> ListarAsync(
        EstadoOportunidad? estado,
        CancellationToken cancellationToken = default);

    Task<Oportunidad?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Oportunidad?> CrearAsync(
        string nombre,
        string cliente,
        decimal montoEstimado,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intenta cambiar el estado solo si el estado persistido sigue siendo
    /// <paramref name="estadoEsperado"/>. Devuelve la oportunidad actualizada
    /// o <c>null</c> si no se actualizó ninguna fila.
    /// </summary>
    Task<Oportunidad?> CambiarEstadoAsync(
        int id,
        EstadoOportunidad estadoEsperado,
        EstadoOportunidad nuevoEstado,
        CancellationToken cancellationToken = default);
}
