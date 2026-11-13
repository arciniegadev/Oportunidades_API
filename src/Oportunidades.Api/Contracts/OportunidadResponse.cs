using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Contracts;

/// <summary>
/// Representación de una oportunidad devuelta por la API.
/// </summary>
public sealed record OportunidadResponse(
    int Id,
    string Nombre,
    string Cliente,
    decimal MontoEstimado,
    EstadoOportunidad Estado,
    DateTime FechaCreacionUtc);

/// <summary>
/// Resultado paginado para el listado de oportunidades.
/// </summary>
public sealed record OportunidadListResponse(
    IReadOnlyList<OportunidadResponse> Items,
    int Page,
    int PageSize,
    int Total);

public static class OportunidadMappings
{
    public static OportunidadResponse ToResponse(this Oportunidad oportunidad) =>
        new(
            oportunidad.Id,
            oportunidad.Nombre,
            oportunidad.Cliente,
            oportunidad.MontoEstimado,
            oportunidad.Estado,
            oportunidad.FechaCreacionUtc);
}
