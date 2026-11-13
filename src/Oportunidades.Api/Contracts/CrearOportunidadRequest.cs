using System.ComponentModel.DataAnnotations;
using Oportunidades.Api.Validation;

namespace Oportunidades.Api.Contracts;

public  class CrearOportunidadRequest
{
    [TextoRequerido(100, ErrorMessage = "El nombre es obligatorio y debe tener como máximo 100 caracteres después de quitar espacios al inicio y al final.")]
    public string? Nombre { get; set; }

    [TextoRequerido(120, ErrorMessage = "El cliente es obligatorio y debe tener como máximo 120 caracteres después de quitar espacios al inicio y al final.")]
    public string? Cliente { get; set; }

    [MontoEstimado(ErrorMessage = "El monto estimado debe ser mayor que cero, con un máximo de dos decimales y hasta 16 dígitos enteros.")]
    public decimal? MontoEstimado { get; set; }
}
