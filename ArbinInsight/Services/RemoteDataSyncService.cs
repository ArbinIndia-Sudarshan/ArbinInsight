namespace ArbinInsight.Services
{
    public class RemoteDataSyncService : BackgroundService
    {
        private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(5);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RemoteDataSyncService> _logger;

        public RemoteDataSyncService(IServiceScopeFactory scopeFactory, ILogger<RemoteDataSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunSyncCycleAsync(stoppingToken);

            using var timer = new PeriodicTimer(SyncInterval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunSyncCycleAsync(stoppingToken);
            }
        }

        private async Task RunSyncCycleAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var remoteDataService = scope.ServiceProvider.GetRequiredService<IRemoteDataService>();
                await remoteDataService.FetchAllAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Remote data background sync cycle failed.");
            }
        }
    }
}
