using ArbinInsight.Models.Machines;

namespace ArbinInsight.Services
{
    public interface IMachineOverviewService
    {
        Task<IReadOnlyList<MachineDto>> GetMachinesAsync(CancellationToken cancellationToken = default);
    }
}
