using ArbinInsight.Models.Dashboard;

namespace ArbinInsight.Services
{
    public interface IDashboardService
    {
        Task<NetworkDashboardResponse> GetNetworkDashboardAsync(DashboardTimeFilter timeFilter, CancellationToken cancellationToken = default);
        Task<MachineDashboardResponse?> GetMachineDashboardAsync(int machineId, DashboardTimeFilter timeFilter, CancellationToken cancellationToken = default);
    }
}
