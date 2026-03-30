using ArbinInsight.Models;

namespace ArbinInsight.Services
{
    public interface IMachineDataService
    {
        Task<IEnumerable<MachineData>> GetAllAsync();
        Task<MachineData?> GetByIdAsync(int id);
        Task<MachineData> CreateAsync(MachineData machineData);
        Task<bool> UpdateAsync(int id, MachineData machineData);
        Task<bool> DeleteAsync(int id);
    }
}
