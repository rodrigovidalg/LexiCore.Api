using System.Threading;
using System.Threading.Tasks;

namespace Lexico.Application.Contracts
{
    public interface IReportService
    {
        /// Genera el PDF del análisis del documento (bytes del PDF).
        Task<byte[]> GenerarAnalisisPdfAsync(int documentoId, CancellationToken ct = default);
    }
}
