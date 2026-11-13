using System.ComponentModel.DataAnnotations;

namespace Oportunidades.Api.Validation;

/// <summary>
/// Valida que un texto sea obligatorio (no vacío ni solo espacios) y que,
/// una vez eliminados los espacios del inicio y del final, no supere la
/// longitud máxima indicada.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TextoRequeridoAttribute : ValidationAttribute
{
    public TextoRequeridoAttribute(int longitudMaxima)
    {
        if (longitudMaxima <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(longitudMaxima));
        }

        LongitudMaxima = longitudMaxima;
    }

    public int LongitudMaxima { get; }

    public override bool IsValid(object? value)
    {
        if (value is not string texto)
        {
            return false;
        }

        var normalizado = texto.Trim();
        return normalizado.Length > 0 && normalizado.Length <= LongitudMaxima;
    }
}
