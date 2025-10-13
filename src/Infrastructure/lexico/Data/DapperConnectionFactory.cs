using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Lexico.Infrastructure.Data
{
    /// <summary>
    /// Factory de conexiones para Dapper con compatibilidad retro:
    /// - Create()                  -> IDbConnection (cerrada)
    /// - CreateOpen()              -> IDbConnection (abierta)
    /// - CreateMySqlConnection()   -> MySqlConnection (cerrada)
    /// - CreateOpenMySqlConnectionAsync() -> MySqlConnection (abierta)
    /// - WithMySqlConnectionAsync() -> helpers para scope con conexión abierta
    /// </summary>
    public class DapperConnectionFactory
    {
        private readonly string _mysqlConnectionString;

        public DapperConnectionFactory(IConfiguration configuration)
        {
            _mysqlConnectionString =
                configuration.GetConnectionString("MySQLConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:MySQLConnection no está configurado.");
        }

        // ===================== Compatibilidad con repos existentes =====================

        /// <summary>
        /// COMPAT: varios repos llaman _factory.Create(). Devolvemos IDbConnection cerrada.
        /// </summary>
        public IDbConnection Create()
            => new MySqlConnection(_mysqlConnectionString);

        /// <summary>
        /// COMPAT: si algún repo llama _factory.CreateOpen() (sin async), abrimos y devolvemos IDbConnection.
        /// </summary>
        public IDbConnection CreateOpen()
        {
            var conn = new MySqlConnection(_mysqlConnectionString);
            conn.Open(); // sync open para máxima compatibilidad
            return conn;
        }

        // ===================== API explícita MySQL (la que agregamos) =====================

        /// <summary>
        /// Crea la conexión MySQL (cerrada).
        /// </summary>
        public MySqlConnection CreateMySqlConnection()
            => new MySqlConnection(_mysqlConnectionString);

        /// <summary>
        /// Crea y abre la conexión MySQL (async).
        /// </summary>
        public async Task<MySqlConnection> CreateOpenMySqlConnectionAsync(CancellationToken ct = default)
        {
            var conn = CreateMySqlConnection();
            await conn.OpenAsync(ct);
            return conn;
        }

        /// <summary>
        /// Helper con valor de retorno (usa conexión abierta y la dispone).
        /// </summary>
        public async Task<T> WithMySqlConnectionAsync<T>(Func<MySqlConnection, Task<T>> action, CancellationToken ct = default)
        {
            await using var conn = await CreateOpenMySqlConnectionAsync(ct);
            return await action(conn);
        }

        /// <summary>
        /// Helper sin valor de retorno (usa conexión abierta y la dispone).
        /// </summary>
        public async Task WithMySqlConnectionAsync(Func<MySqlConnection, Task> action, CancellationToken ct = default)
        {
            await using var conn = await CreateOpenMySqlConnectionAsync(ct);
            await action(conn);
        }
    }
}
