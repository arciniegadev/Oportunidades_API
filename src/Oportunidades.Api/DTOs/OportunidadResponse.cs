using Oportunidades.Api.Domain;

namespace Oportunidades.Api.DTOs;


public sealed record OportunidadResponse(
    int Id,
    string Nombre,
    string Cliente,
    decimal MontoEstimado,
    EstadoOportunidad Estado,
    DateTime FechaCreacionUtc);

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
