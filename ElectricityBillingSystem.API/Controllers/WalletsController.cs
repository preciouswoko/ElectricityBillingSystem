using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Application.DTOs;
using ElectricityBillingSystem.Domain.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityBillingSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletsService _walletsService;
        private readonly ILogger<WalletsController> _logger;

        public WalletsController(IWalletsService walletsService, ILogger<WalletsController> logger)
        {
            _walletsService = walletsService;
            _logger = logger;
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [HttpPost("{id}/add-funds")]
        public async Task<IActionResult> AddFunds(Guid id, [FromBody] WalletFundRequest request)
        {
            _logger.LogInformation("Received request to AddFunds for Amount {Amount}", request.Amount);
            try
            {
                var wallet = await _walletsService.GetWalletByIdAsync(id);
                if (wallet == null)
                    return NotFound();

                var success = await _walletsService.AddFundsAsync(id, request.Amount);
                if (!success)
                    return BadRequest("Failed to add funds.");

                return Ok(new { balance = wallet.Balance });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "AddFunds failed for Amountr {Amount}", request.Amount);
                return BadRequest(ex.Message);
            }
          
        }
    }

}
