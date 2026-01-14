using BrosCode.LastCall.Business.Services.App;
using BrosCode.LastCall.Contracts.Dtos.App;
using Microsoft.AspNetCore.Mvc;

namespace BrosCode.LastCall.Api.Controllers;

[Route("api/drinks")]
public sealed class DrinkController : BaseController
{
    private readonly DrinkService _drinkService;

    public DrinkController(DrinkService drinkService)
    {
        _drinkService = drinkService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DrinkDto>>> GetAllAsync(CancellationToken ct)
    {
        var drinks = await _drinkService.ListAsync(ct);
        return Ok(drinks);
    }

    [HttpGet("{id:guid}", Name = "DrinkById")]
    public async Task<ActionResult<DrinkDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var drink = await _drinkService.GetByIdAsync(id, ct);
        return OkOrNotFound(drink);
    }

    [HttpPost]
    public async Task<ActionResult<DrinkDto>> CreateAsync([FromBody] DrinkDto dto, CancellationToken ct)
    {
        var created = await _drinkService.AddAsync(dto, ct);
        await _drinkService.SaveChangesAsync(ct);
        return CreatedAtRoute("DrinkById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] DrinkDto dto, CancellationToken ct)
    {
        try
        {
            await _drinkService.UpdateAsync(id, dto, ct);
            await _drinkService.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await _drinkService.SoftDeleteAsync(id, ct);
            await _drinkService.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
