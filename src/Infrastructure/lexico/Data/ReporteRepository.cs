using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Lexico.Domain.Entities;
using MySqlConnector;
using Lexico.Application.Contracts;

namespace Lexico.Infrastructure.Data
{
    public class ReporteRepository : IReporteRepository
    {
        private readonly DapperConnectionFactory _factory;

        public ReporteRepository(DapperConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> InsertAsync(Reporte reporte, CancellationToken ct = default)
        {
            // OJO con "tamaño_archivo": si la columna se creó como "tamano_archivo" (sin ñ),
            // cambia el nombre en el INSERT y en la entidad.
            const string sql = @"
INSERT INTO reportes
(analisis_id, usuario_id, tipo_reporte, ruta_archivo, `tamaño_archivo`, fecha_generacion, enviado, fecha_envio, metodo_envio, destinatario)
VALUES
(@AnalisisId, @UsuarioId, @TipoReporte, @RutaArchivo, @TamañoArchivo, @FechaGeneracion, @Enviado, @FechaEnvio, @MetodoEnvio, @Destinatario);
SELECT LAST_INSERT_ID();";

            return await _factory.WithMySqlConnectionAsync<int>(async conn =>
            {
                // LAST_INSERT_ID() devuelve BIGINT -> usamos long y casteamos a int
                var newId = await conn.ExecuteScalarAsync<long>(
                    new CommandDefinition(sql, reporte, cancellationToken: ct)
                );
                return checked((int)newId);
            }, ct);
        }

        public async Task<Reporte?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            const string sql = @"SELECT id, analisis_id AS AnalisisId, usuario_id AS UsuarioId,
 tipo_reporte AS TipoReporte, ruta_archivo AS RutaArchivo, `tamaño_archivo` AS TamañoArchivo,
 fecha_generacion AS FechaGeneracion, enviado AS Enviado, fecha_envio AS FechaEnvio,
 metodo_envio AS MetodoEnvio, destinatario AS Destinatario
 FROM reportes WHERE id = @id;";

            return await _factory.WithMySqlConnectionAsync(async conn =>
            {
                return await conn.QueryFirstOrDefaultAsync<Reporte>(
                    new CommandDefinition(sql, new { id }, cancellationToken: ct)
                );
            }, ct);
        }

        public async Task<Reporte?> GetLastByDocumentoAsync(int documentoId, CancellationToken ct = default)
        {
            const string sql = @"SELECT id, analisis_id AS AnalisisId, usuario_id AS UsuarioId,
 tipo_reporte AS TipoReporte, ruta_archivo AS RutaArchivo, `tamaño_archivo` AS TamañoArchivo,
 fecha_generacion AS FechaGeneracion, enviado AS Enviado, fecha_envio AS FechaEnvio,
 metodo_envio AS MetodoEnvio, destinatario AS Destinatario
 FROM reportes
 WHERE analisis_id IN (SELECT id FROM analisis WHERE documento_id = @documentoId)
 ORDER BY fecha_generacion DESC
 LIMIT 1;";

            return await _factory.WithMySqlConnectionAsync(async conn =>
            {
                return await conn.QueryFirstOrDefaultAsync<Reporte>(
                    new CommandDefinition(sql, new { documentoId }, cancellationToken: ct)
                );
            }, ct);
        }

        public async Task MarkAsSentAsync(int reporteId, string metodoEnvio, string destinatario, DateTime fechaEnvioUtc, CancellationToken ct = default)
        {
            const string sql = @"UPDATE reportes
 SET enviado = 1, fecha_envio = @fechaEnvioUtc, metodo_envio = @metodoEnvio, destinatario = @destinatario
 WHERE id = @reporteId;";

            await _factory.WithMySqlConnectionAsync(async conn =>
            {
                await conn.ExecuteAsync(
                    new CommandDefinition(sql, new { reporteId, fechaEnvioUtc, metodoEnvio, destinatario }, cancellationToken: ct)
                );
            }, ct);
        }
    }
}