namespace Oportunidades.Api.Domain;


public  class Oportunidad
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Cliente { get; set; } = string.Empty;

    public decimal MontoEstimado { get; set; }

    public EstadoOportunidad Estado { get; set; } = EstadoOportunidad.Nueva;

    public DateTime FechaCreacionUtc { get; set; }
}
