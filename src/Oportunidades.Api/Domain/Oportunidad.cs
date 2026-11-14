namespace Oportunidades.Api.Domain;


public  class Oportunidad
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Cliente { get; set; } = string.Empty;

    public decimal MontoEstimado { get; set; }

    public EstadoOportunidad Estado { get; set; } = EstadoOportunidad.Nueva;

    public DateTime FechaCreacionUtc { get; set; }


    /// <summary>
    /// Repetir el estado actual siempre es válido (no modifica el recurso).
    /// Las únicas transiciones permitidas son Nueva -> EnSeguimiento y
    /// EnSeguimiento -> Ganada o Perdida. Ganada y Perdida son finales.
    /// </summary>
    public bool PuedeCambiarEstado(EstadoOportunidad nuevoEstado) =>
        nuevoEstado == Estado ||
        (Estado, nuevoEstado) switch
        {
            (EstadoOportunidad.Nueva, EstadoOportunidad.EnSeguimiento) => true,
            (EstadoOportunidad.EnSeguimiento, EstadoOportunidad.Ganada) => true,
            (EstadoOportunidad.EnSeguimiento, EstadoOportunidad.Perdida) => true,
            _ => false
        };
}
