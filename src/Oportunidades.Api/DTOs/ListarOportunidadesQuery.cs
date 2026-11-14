using Oportunidades.Api.Validation;

namespace Oportunidades.Api.DTOs;

/// <summary>
/// Parámetros de consulta del listado. El filtro por estado es opcional,
/// pero si se envía debe ser uno de los nombres admitidos.
/// </summary>
public sealed class ListarOportunidadesQuery
{
    [EstadoOportunidadValido(
        ErrorMessage = "El estado debe ser uno de: Nueva, EnSeguimiento, Ganada o Perdida.")]
    public string? Estado { get; set; }
}
