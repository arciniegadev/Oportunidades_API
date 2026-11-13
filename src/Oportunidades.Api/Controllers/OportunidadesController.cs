using Microsoft.AspNetCore.Mvc;
using Oportunidades.Api.Contracts;
using Oportunidades.Api.Domain;
using Oportunidades.Api.Services;

namespace Oportunidades.Api.Controllers;


[ApiController]
[Route("api/oportunidades")]
[Produces("application/json")]
public sealed class OportunidadesController(IOportunidadService service) : ControllerBase
{
    private const int TamanoPaginaPredeterminado = 20;

    [HttpGet]
    [ProducesResponseType(typeof(OportunidadListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OportunidadListResponse>> Listar(
        [FromQuery] EstadoOportunidad? estado,
        [FromQuery] string? cliente,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = TamanoPaginaPredeterminado,
        CancellationToken cancellationToken = default)
    {
        var listado = await service.ListarAsync(
            estado,
            cliente,
            page,
            pageSize,
            cancellationToken);

        var respuesta = new OportunidadListResponse(
            listado.Items.Select(o => o.ToResponse()).ToList(),
            listado.Page,
            listado.PageSize,
            listado.Total);

        return Ok(respuesta);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OportunidadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OportunidadResponse>> ObtenerPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var oportunidad = await service.ObtenerPorIdAsync(id, cancellationToken);

        if (oportunidad is null)
        {
            return NoEncontrada(id);
        }

        return Ok(oportunidad.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType(typeof(OportunidadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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
    [HttpPut("{id:int}/estado")]
    [ProducesResponseType(typeof(OportunidadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OportunidadResponse>> ActualizarEstado(
        int id,
        [FromBody] ActualizarEstadoRequest request,
        CancellationToken cancellationToken)
    {
        var nuevoEstado = Enum.Parse<EstadoOportunidad>(
            request.Estado!.Trim(),
            ignoreCase: true);

        var oportunidad = await service.ActualizarEstadoAsync(
            id,
            nuevoEstado,
            cancellationToken);

        if (oportunidad is null)
        {
            return NoEncontrada(id);
        }

        return Ok(oportunidad.ToResponse());
    }

    private ObjectResult NoEncontrada(int id) =>
        Problem(
            title: "Oportunidad no encontrada",
            detail: $"No existe una oportunidad con el id {id}.",
            statusCode: StatusCodes.Status404NotFound);
}
