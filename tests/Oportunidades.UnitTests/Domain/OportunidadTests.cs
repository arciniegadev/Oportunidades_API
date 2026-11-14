using Oportunidades.Api.Domain;

namespace Oportunidades.UnitTests.Domain;

public sealed class OportunidadTests
{
    [Theory]
    // Repetir el mismo estado está permitido.
    [InlineData(EstadoOportunidad.Nueva, EstadoOportunidad.Nueva, true)]
    [InlineData(EstadoOportunidad.EnSeguimiento, EstadoOportunidad.EnSeguimiento, true)]
    [InlineData(EstadoOportunidad.Ganada, EstadoOportunidad.Ganada, true)]
    [InlineData(EstadoOportunidad.Perdida, EstadoOportunidad.Perdida, true)]
    // Transiciones permitidas.
    [InlineData(EstadoOportunidad.Nueva, EstadoOportunidad.EnSeguimiento, true)]
    [InlineData(EstadoOportunidad.EnSeguimiento, EstadoOportunidad.Ganada, true)]
    [InlineData(EstadoOportunidad.EnSeguimiento, EstadoOportunidad.Perdida, true)]
    // Transiciones rechazadas.
    [InlineData(EstadoOportunidad.Nueva, EstadoOportunidad.Ganada, false)]
    [InlineData(EstadoOportunidad.Nueva, EstadoOportunidad.Perdida, false)]
    [InlineData(EstadoOportunidad.EnSeguimiento, EstadoOportunidad.Nueva, false)]
    [InlineData(EstadoOportunidad.Ganada, EstadoOportunidad.Nueva, false)]
    [InlineData(EstadoOportunidad.Ganada, EstadoOportunidad.EnSeguimiento, false)]
    [InlineData(EstadoOportunidad.Ganada, EstadoOportunidad.Perdida, false)]
    [InlineData(EstadoOportunidad.Perdida, EstadoOportunidad.Nueva, false)]
    [InlineData(EstadoOportunidad.Perdida, EstadoOportunidad.EnSeguimiento, false)]
    [InlineData(EstadoOportunidad.Perdida, EstadoOportunidad.Ganada, false)]
    public void PuedeCambiarEstado_DevuelveElResultadoEsperado(
        EstadoOportunidad estadoActual,
        EstadoOportunidad estadoNuevo,
        bool esperado)
    {
        var oportunidad = new Oportunidad { Estado = estadoActual };

        Assert.Equal(esperado, oportunidad.PuedeCambiarEstado(estadoNuevo));
    }
}
