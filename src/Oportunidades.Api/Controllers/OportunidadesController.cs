using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oportunidades.Api.DTOs;
using Oportunidades.Api.Domain;
using Oportunidades.Api.Security;
using Oportunidades.Api.Services;

namespace Oportunidades.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/oportunidades")]
[Produces("application/json")]
public sealed class OportunidadesController(IOportunidadService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.ConsultaOEditor)]
    [ProducesResponseType(typeof(IReadOnlyList<OportunidadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<OportunidadResponse>>> Listar(
        [FromQuery] ListarOportunidadesQuery query,
        CancellationToken cancellationToken = default)
    {
        var estado = ParsearEstado(query.Estado);
        var listado = await service.ListarAsync(estado, cancellationToken);
        return Ok(listado.Select(o => o.ToResponse()).ToList());
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.ConsultaOEditor)]
    [ProducesResponseType(typeof(OportunidadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OportunidadResponse>> ObtenerPorId(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return IdInvalido();
        }

        var oportunidad = await service.ObtenerPorIdAsync(id, cancellationToken);

        if (oportunidad is null)
        {
            return NoEncontrada(id);
        }

        return Ok(oportunidad.ToResponse());
    }

    [HttpPost]
    [Authorize(Roles = Roles.Editor)]
    [ProducesResponseType(typeof(OportunidadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OportunidadResponse>> Crear(
        [FromBody] CrearOportunidadRequest request,
        CancellationToken cancellationToken)
    {
        var oportunidad = await service.CrearAsync(
            request.Nombre!,
            request.Cliente!,
            request.MontoEstimado!.Value,
            cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = oportunidad.Id },
            oportunidad.ToResponse());
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = Roles.Editor)]
    [ProducesResponseType(typeof(OportunidadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OportunidadResponse>> ActualizarEstado(
        int id,
        [FromBody] ActualizarEstadoRequest request,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return IdInvalido();
        }

        var nuevoEstado = ParsearEstado(request.Estado)!;

        var (resultado, oportunidad) = await service.ActualizarEstadoAsync(
            id,
            nuevoEstado.Value,
            cancellationToken);

        return resultado switch
        {
            ResultadoCambioEstado.NoEncontrada => NoEncontrada(id),
            ResultadoCambioEstado.Conflicto => ConflictoEstado(oportunidad!),
            _ => Ok(oportunidad!.ToResponse())
        };
    }

    private static EstadoOportunidad? ParsearEstado(string? estado) =>
        string.IsNullOrWhiteSpace(estado)
            ? null
            : Enum.Parse<EstadoOportunidad>(estado.Trim(), ignoreCase: true);

    private ObjectResult NoEncontrada(int id) =>
        Problem(
            title: "Oportunidad no encontrada",
            detail: $"No existe una oportunidad con el id {id}.",
            statusCode: StatusCodes.Status404NotFound);

    private ActionResult IdInvalido()
    {
        ModelState.AddModelError(
            "id",
            "El id debe ser un número entero mayor que cero.");

        return ValidationProblem(ModelState);
    }

    private ObjectResult ConflictoEstado(Oportunidad oportunidad) =>
        Problem(
            title: "Cambio de estado no permitido",
            detail: $"La oportunidad está en estado '{oportunidad.Estado}' y el cambio solicitado no se pudo aplicar. " +
                    "Verifica la transición o si otro proceso modificó el estado.",
            statusCode: StatusCodes.Status409Conflict);
}
