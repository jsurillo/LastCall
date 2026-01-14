using Microsoft.AspNetCore.Mvc;

namespace BrosCode.LastCall.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected ActionResult<T> OkOrNotFound<T>(T? value) where T : class
        => value is null ? NotFound() : Ok(value);
}
