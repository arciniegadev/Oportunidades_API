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
        if (value is not string texto)
        {
            return false;
        }

        var normalizado = texto.Trim();
        return EstadosPermitidos.Any(estado =>
            string.Equals(estado, normalizado, StringComparison.OrdinalIgnoreCase));
    }
}
