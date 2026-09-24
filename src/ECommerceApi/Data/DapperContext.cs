using System.Data;
using Microsoft.Data.SqlClient;

namespace ECommerceApi.Data;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");
    }

    /// <summary>
    /// Crea y retorna una nueva instancia de IDbConnection para ser utilizada por Dapper.
    /// </summary>
    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}