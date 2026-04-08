using ArbinInsight.Models.Dashboard;
using ArbinInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbinInsight.Controllers
{
    [Route("api/dashboard/ui")]
    [ApiController]
    public class DashboardUiController : ControllerBase
    {
        private readonly IDashboardUiService _dashboardUiService;

        public DashboardUiController(IDashboardUiService dashboardUiService)
        {
            _dashboardUiService = dashboardUiService;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardUiResponse>> GetDashboard([FromQuery] DashboardTimeFilter period = DashboardTimeFilter.Weekly, CancellationToken cancellationToken = default)
        {
            var result = await _dashboardUiService.GetDashboardAsync(period, cancellationToken);
            return Ok(result);
        }
    }
}
