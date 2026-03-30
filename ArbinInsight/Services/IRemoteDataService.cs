using ArbinInsight.Models.RemoteData;

namespace ArbinInsight.Services
{
    public interface IRemoteDataService
    {
        Task<RemoteDataFetchResponse> FetchAllAsync(CancellationToken cancellationToken = default);
    }
}
