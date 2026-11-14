using System.ComponentModel.DataAnnotations;
using Oportunidades.Api.Validation;

namespace Oportunidades.Api.DTOs;


public  class ActualizarEstadoRequest
{
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [EstadoOportunidadValido(ErrorMessage = "El estado debe ser uno de: Nueva, EnSeguimiento, Ganada o Perdida.")]
    public string? Estado { get; set; }
}
