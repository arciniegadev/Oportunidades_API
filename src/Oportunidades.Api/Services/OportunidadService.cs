using Microsoft.EntityFrameworkCore;
using Oportunidades.Api.Data;
using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Services;

/// <summary>
/// Implementación de los casos de uso sobre SQL Server mediante EF Core.
/// </summary>
public sealed class OportunidadService(
    OportunidadesDbContext db,
    ILogger<OportunidadService> logger) : IOportunidadService
{
    private const int TamanoPaginaMaximo = 100;

    public async Task<OportunidadListado> ListarAsync(
        EstadoOportunidad? estado,
        string? cliente,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanoPaginaMaximo);

        var query = db.Oportunidades.AsNoTracking();

        if (estado is not null)
        {
            query = query.Where(o => o.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(cliente))
        {
            var filtroCliente = cliente.Trim();
            query = query.Where(o => o.Cliente.Contains(filtroCliente));
        }

        var total = await query.CountAsync(cancellationToken);
        var desplazamiento = (long)(page - 1) * pageSize;

        if (desplazamiento > int.MaxValue)
        {
            return new OportunidadListado([], page, pageSize, total);
        }

        var oportunidades = await query
            .OrderByDescending(o => o.FechaCreacionUtc)
            .ThenByDescending(o => o.Id)
            .Skip((int)desplazamiento)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new OportunidadListado(oportunidades, page, pageSize, total);
    }

    public async Task<Oportunidad?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await db.Oportunidades
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<Oportunidad> CrearAsync(
        string nombre,
        string cliente,
        decimal montoEstimado,
        CancellationToken cancellationToken = default)
    {
        var oportunidad = new Oportunidad
        {
            Nombre = nombre.Trim(),
            Cliente = cliente.Trim(),
            MontoEstimado = montoEstimado,
            Estado = EstadoOportunidad.Nueva,
            FechaCreacionUtc = DateTime.UtcNow
        };

        db.Oportunidades.Add(oportunidad);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Oportunidad {OportunidadId} registrada para el cliente {Cliente}.",
            oportunidad.Id,
            oportunidad.Cliente);

        return oportunidad;
    }

    public async Task<Oportunidad?> ActualizarEstadoAsync(
        int id,
        EstadoOportunidad nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        var oportunidad = await db.Oportunidades
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (oportunidad is null)
        {
            return null;
        }

        var estadoAnterior = oportunidad.Estado;

        if (estadoAnterior != nuevoEstado)
        {
            oportunidad.Estado = nuevoEstado;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Oportunidad {OportunidadId}: estado {EstadoAnterior} -> {EstadoNuevo}.",
                oportunidad.Id,
                estadoAnterior,
                nuevoEstado);
        }

        return oportunidad;
    }
}
