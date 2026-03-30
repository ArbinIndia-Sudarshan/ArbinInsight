using ArbinInsight.Models.Dashboard;

namespace ArbinInsight.Services
{
    public interface IDashboardQueryService
    {
        Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DashboardMachineCard>> GetMachinesAsync(CancellationToken cancellationToken = default);
        Task<DashboardReportSummaryResponse> GetReportSummaryAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? machineCode,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DashboardTestReportItem>> GetTestReportsAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            string? machineCode,
            string? result,
            CancellationToken cancellationToken = default);
    }
}
