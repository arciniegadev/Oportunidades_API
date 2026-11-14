using Microsoft.Data.SqlClient;

namespace Oportunidades.Api.Data;

public interface IDbConnectionFactory
{
    SqlConnection Create();
}

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public SqlConnection Create() => new(connectionString);
}