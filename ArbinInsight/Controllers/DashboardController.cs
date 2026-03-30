using ArbinInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbinInsight.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardSyncService _dashboardSyncService;
        private readonly IDashboardQueryService _dashboardQueryService;

        public DashboardController(IDashboardSyncService dashboardSyncService, IDashboardQueryService dashboardQueryService)
        {
            _dashboardSyncService = dashboardSyncService;
            _dashboardQueryService = dashboardQueryService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromQuery] bool publishToQueue = true, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardSyncService.SyncAsync(publishToQueue, cancellationToken);
            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        {
            var result = await _dashboardQueryService.GetSummaryAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("machines")]
        public async Task<IActionResult> Machines(CancellationToken cancellationToken)
        {
            var result = await _dashboardQueryService.GetMachinesAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/summary")]
        public async Task<IActionResult> ReportSummary(
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? machineCode,
            CancellationToken cancellationToken)
        {
            var result = await _dashboardQueryService.GetReportSummaryAsync(fromUtc, toUtc, machineCode, cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/tests")]
        public async Task<IActionResult> TestReports(
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? machineCode,
            [FromQuery] string? result,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardQueryService.GetTestReportsAsync(fromUtc, toUtc, machineCode, result, cancellationToken);
            return Ok(response);
        }
    }
}
