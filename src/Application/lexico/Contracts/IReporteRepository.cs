using Lexico.Domain.Entities;

namespace Lexico.Application.Contracts
{
    public interface IReporteRepository
    {
        Task<int> InsertAsync(Reporte reporte, CancellationToken ct = default);
        Task<Reporte?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Reporte?> GetLastByDocumentoAsync(int documentoId, CancellationToken ct = default);
        Task MarkAsSentAsync(int reporteId, string metodoEnvio, string destinatario, DateTime fechaEnvioUtc, CancellationToken ct = default);
    }
}
