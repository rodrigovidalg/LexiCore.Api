using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Lexico.Application.Contracts;

namespace Lexico.API.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    public class ReportesController : ControllerBase
    {
        private readonly IReportService _reports;

        public ReportesController(IReportService reports)
        {
            _reports = reports;
        }

        /// <summary>
        /// Genera y descarga el PDF del análisis para el documento indicado.
        /// </summary>
        [HttpGet("analisis/{documentoId:int}")]
        public async Task<IActionResult> DescargarAnalisis(int documentoId)
        {
            var pdf = await _reports.GenerarAnalisisPdfAsync(documentoId, HttpContext.RequestAborted);
            var fileName = $"analisis_lexico_doc_{documentoId}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
    }
}
