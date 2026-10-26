using App.Core.FoodCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers;

[ApiController]
[Route("store-aisles")]
[Authorize]
public sealed class StoreAislesController(IFoodCatalogService foodCatalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StoreAisleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoreAisles(CancellationToken cancellationToken)
    {
        var aisles = await foodCatalogService.GetStoreAislesAsync(cancellationToken);
        return Ok(aisles);
    }
}
