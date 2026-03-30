using ArbinInsight.Models;
using ArbinInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbinInsight.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArbinInsightController : ControllerBase
    {
        private readonly IMachineDataService _machineDataService;

        public ArbinInsightController(IMachineDataService machineDataService)
        {
            _machineDataService = machineDataService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _machineDataService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _machineDataService.GetByIdAsync(id);

            if (result == null)
            {
                return NotFound(new { message = $"Record with id {id} not found." });
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MachineData dto)
        {
            var created = await _machineDataService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MachineData dto)
        {
            var updated = await _machineDataService.UpdateAsync(id, dto);

            if (!updated)
            {
                return NotFound(new { message = $"Record with id {id} not found." });
            }

            return Ok(new { message = "Updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _machineDataService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new { message = $"Record with id {id} not found." });
            }

            return Ok(new { message = "Deleted successfully." });
        }
    }
}
