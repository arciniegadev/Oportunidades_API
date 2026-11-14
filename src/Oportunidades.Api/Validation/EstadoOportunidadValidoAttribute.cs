using System.ComponentModel.DataAnnotations;
using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Validation;

/// <summary>
/// Valida que el texto corresponda a uno de los estados permitidos:
/// Nueva, EnSeguimiento, Ganada o Perdida (sin distinguir mayúsculas).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class EstadoOportunidadValidoAttribute : ValidationAttribute
{
    private static readonly string[] EstadosPermitidos = Enum.GetNames<EstadoOportunidad>();

    public override bool IsValid(object? value)
    {
        // El filtro es opcional: la ausencia de valor no es un estado inválido.
        if (value is null)
        {
            return true;
        }

        if (value is not string texto)
        {
            return false;
        }

        var normalizado = texto.Trim();

        return normalizado.Length == 0 ||
               EstadosPermitidos.Any(estado =>
                   string.Equals(estado, normalizado, StringComparison.OrdinalIgnoreCase));
    }
}
