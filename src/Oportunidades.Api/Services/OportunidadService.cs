using Oportunidades.Api.Domain;
using Oportunidades.Api.Repositories;

namespace Oportunidades.Api.Services;

/// <summary>
/// Reglas de negocio de las oportunidades comerciales. El acceso a datos
/// queda delegado en <see cref="IOportunidadRepository"/>.
/// </summary>
public sealed class OportunidadService(
    IOportunidadRepository repository,
    ILogger<OportunidadService> logger) : IOportunidadService
{
    public Task<IReadOnlyList<Oportunidad>> ListarAsync(
        EstadoOportunidad? estado,
        CancellationToken cancellationToken = default) =>
        repository.ListarAsync(estado, cancellationToken);

    public Task<Oportunidad?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        repository.ObtenerPorIdAsync(id, cancellationToken);

    public async Task<Oportunidad> CrearAsync(
        string nombre,
        string cliente,
        decimal montoEstimado,
        CancellationToken cancellationToken = default)
    {
        var oportunidad = await repository.CrearAsync(
                nombre.Trim(),
                cliente.Trim(),
                montoEstimado,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "El procedimiento de alta no devolvió la oportunidad creada.");

        logger.LogInformation(
            "Oportunidad {OportunidadId} registrada para el cliente {Cliente}.",
            oportunidad.Id,
            oportunidad.Cliente);

        return oportunidad;
    }

    public async Task<(ResultadoCambioEstado Resultado, Oportunidad? Oportunidad)> ActualizarEstadoAsync(
        int id,
        EstadoOportunidad nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        var actual = await repository.ObtenerPorIdAsync(id, cancellationToken);

        if (actual is null)
        {
            return (ResultadoCambioEstado.NoEncontrada, null);
        }

        if (!actual.PuedeCambiarEstado(nuevoEstado))
        {
            return (ResultadoCambioEstado.Conflicto, actual);
        }

        // Repetir el estado actual devuelve la oportunidad sin modificarla.
        if (actual.Estado == nuevoEstado)
        {
            return (ResultadoCambioEstado.Actualizado, actual);
        }

        // El UPDATE solo se aplica si el estado persistido sigue siendo el validado.
        var actualizada = await repository.CambiarEstadoAsync(
            id,
            actual.Estado,
            nuevoEstado,
            cancellationToken);

        if (actualizada is not null)
        {
            logger.LogInformation(
                "Oportunidad {OportunidadId}: estado {EstadoAnterior} -> {EstadoNuevo}.",
                id,
                actual.Estado,
                nuevoEstado);

            return (ResultadoCambioEstado.Actualizado, actualizada);
        }

        // Modificación concurrente: se devuelve el estado vigente para el 409.
        var vigente = await repository.ObtenerPorIdAsync(id, cancellationToken);

        logger.LogWarning(
            "Oportunidad {OportunidadId}: conflicto al cambiar de {EstadoValidado} a {EstadoNuevo}.",
            id,
            actual.Estado,
            nuevoEstado);

        return vigente is null
            ? (ResultadoCambioEstado.NoEncontrada, null)
            : (ResultadoCambioEstado.Conflicto, vigente);
    }
}
