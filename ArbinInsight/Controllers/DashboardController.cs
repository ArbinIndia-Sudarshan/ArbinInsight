using ArbinInsight.Models.Dashboard;
using ArbinInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbinInsight.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("network")]
        public async Task<IActionResult> GetNetworkDashboard([FromQuery] DashboardTimeFilter timeFilter = DashboardTimeFilter.Weekly, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetNetworkDashboardAsync(timeFilter, cancellationToken);
            return Ok(result);
        }

        [HttpGet("machines/{machineId:int}")]
        public async Task<IActionResult> GetMachineDashboard(int machineId, [FromQuery] DashboardTimeFilter timeFilter = DashboardTimeFilter.Weekly, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardService.GetMachineDashboardAsync(machineId, timeFilter, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Machine with id {machineId} not found." });
            }

            return Ok(result);
        }
    }
}
