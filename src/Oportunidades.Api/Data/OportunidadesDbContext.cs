using Microsoft.EntityFrameworkCore;
using Oportunidades.Api.Domain;

namespace Oportunidades.Api.Data;

public sealed class OportunidadesDbContext(DbContextOptions<OportunidadesDbContext> options)
    : DbContext(options)
{
    public DbSet<Oportunidad> Oportunidades => Set<Oportunidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OportunidadesDbContext).Assembly);
    }
}
