using Azunt.ReasonManagement;
using Microsoft.AspNetCore.Mvc;

namespace Azunt.Web.Components.Pages.Reasons.Apis;

[ApiController]
[Route("api/[controller]")]
public class ReasonApiController : ControllerBase
{
    private readonly IReasonRepository _repository;

    public ReasonApiController(IReasonRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Reason>>> GetReasons()
    {
        return Ok(await _repository.GetAllAsync());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Reason>> GetReason(long id)
    {
        var model = await _repository.GetByIdAsync(id);

        if (model.Id == 0)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<Reason>> PostReason(Reason model)
    {
        model.CreatedAt = DateTimeOffset.UtcNow;
        var result = await _repository.AddAsync(model);
        return CreatedAtAction(nameof(GetReason), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> PutReason(long id, Reason model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var success = await _repository.UpdateAsync(model);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteReason(long id)
    {
        var success = await _repository.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}
