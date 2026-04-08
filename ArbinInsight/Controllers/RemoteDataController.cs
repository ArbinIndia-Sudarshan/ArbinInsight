using ArbinInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbinInsight.Controllers
{
    [Route("api/remote-data")]
    [ApiController]
    public class RemoteDataController : ControllerBase
    {
        private readonly IRemoteDataService _remoteDataService;

        public RemoteDataController(IRemoteDataService remoteDataService)
        {
            _remoteDataService = remoteDataService;
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> Fetch(CancellationToken cancellationToken)
        {
            var result = await _remoteDataService.FetchAllAsync(cancellationToken);
            return Ok(result);
        }
    }
}
