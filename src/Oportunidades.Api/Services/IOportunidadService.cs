using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Services;

/// <summary>
/// Resultado de intentar cambiar el estado de una oportunidad.
/// </summary>
public enum ResultadoCambioEstado
{
    Actualizado,
    NoEncontrada,
    Conflicto
}

public interface IOportunidadService
{
    Task<IReadOnlyList<Oportunidad>> ListarAsync(
        EstadoOportunidad? estado,
        CancellationToken cancellationToken = default);

    Task<Oportunidad?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Oportunidad> CrearAsync(
        string nombre,
        string cliente,
        decimal montoEstimado,
        CancellationToken cancellationToken = default);

    Task<(ResultadoCambioEstado Resultado, Oportunidad? Oportunidad)> ActualizarEstadoAsync(
        int id,
        EstadoOportunidad nuevoEstado,
        CancellationToken cancellationToken = default);
}
