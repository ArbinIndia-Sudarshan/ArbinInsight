using ArbinInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbinInsight.Controllers
{
    [Route("api/remote-data")]
    [ApiController]
    public class RemoteDataController : ControllerBase
    {
        private readonly IRemoteDataService _remoteDataService;
        private readonly IRemoteDataPublisher _remoteDataPublisher;

        public RemoteDataController(IRemoteDataService remoteDataService, IRemoteDataPublisher remoteDataPublisher)
        {
            _remoteDataService = remoteDataService;
            _remoteDataPublisher = remoteDataPublisher;
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> Fetch(CancellationToken cancellationToken)
        {
            var result = await _remoteDataService.FetchAllAsync(cancellationToken);
            return Ok(result);
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish(CancellationToken cancellationToken)
        {
            var fetched = await _remoteDataService.FetchAllAsync(cancellationToken);
            var published = await _remoteDataPublisher.PublishAsync(fetched, cancellationToken);

            return Ok(new
            {
                fetched.FetchedAtUtc,
                published.PublishedAtUtc,
                published.PublishedMessageCount,
                published.PublishedConnections,
                Databases = fetched.Databases
            });
        }
    }
}
