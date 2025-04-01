using Microsoft.AspNetCore.Mvc;

namespace Eagle_web_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private const string predefinedCode = "123";

        [HttpPost("Auth")]
        public IActionResult ValidateCode([FromBody] string code)
        {
            if (code == predefinedCode)
            {
                return Ok("OK");
            }
            else
            {
                return Unauthorized("Unauthorized");
            }
        }
    }
}

