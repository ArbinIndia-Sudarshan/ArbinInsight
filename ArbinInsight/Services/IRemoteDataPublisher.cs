using ArbinInsight.Models.RemoteData;

namespace ArbinInsight.Services
{
    public interface IRemoteDataPublisher
    {
        Task<RemoteDataPublishResult> PublishAsync(RemoteDataFetchResponse payload, CancellationToken cancellationToken = default);
    }
}
