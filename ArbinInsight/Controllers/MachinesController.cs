using ArbinInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbinInsight.Controllers
{
    [Route("api/machines")]
    [ApiController]
    public class MachinesController : ControllerBase
    {
        private readonly IMachineOverviewService _machineOverviewService;

        public MachinesController(IMachineOverviewService machineOverviewService)
        {
            _machineOverviewService = machineOverviewService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMachines(CancellationToken cancellationToken)
        {
            var result = await _machineOverviewService.GetMachinesAsync(cancellationToken);
            return Ok(result);
        }
    }
}
