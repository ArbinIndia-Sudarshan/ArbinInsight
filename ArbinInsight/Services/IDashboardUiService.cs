using ArbinInsight.Models.Dashboard;

namespace ArbinInsight.Services
{
    public interface IDashboardUiService
    {
        Task<DashboardUiResponse> GetDashboardAsync(DashboardTimeFilter timeFilter, CancellationToken cancellationToken = default);
    }
}
