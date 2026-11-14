namespace Oportunidades.IntegrationTests.Infrastructure;

public sealed record OportunidadDto(
    int Id,
    string Nombre,
    string Cliente,
    decimal MontoEstimado,
    string Estado,
    DateTime FechaCreacionUtc);
