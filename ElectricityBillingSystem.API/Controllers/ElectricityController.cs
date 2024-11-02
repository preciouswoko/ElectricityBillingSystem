using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Application.DTOs;
using ElectricityBillingSystem.Domain.Enums;
using ElectricityBillingSystem.Domain.Events;
using ElectricityBillingSystem.Domain.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectricityBillingSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ElectricityController : ControllerBase
    {
        private readonly IElectricityService _electricityService;
        private readonly ILogger<ElectricityController> _logger;

        public ElectricityController(Infrastructure.Data.AppDbContext @object, IElectricityService electricityService, ILogger<ElectricityController> logger)
        {
            _electricityService = electricityService;
            _logger = logger;
        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyBill([FromBody] BillVerificationRequest request)
        {
            _logger.LogInformation("Received request to verify bill for meter number {MeterNumber}", request.MeterNumber);
            try
            {
                var validationRef = await _electricityService.CreateBillAsync(request);
                return Ok(new { validationRef });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Verification failed for meter number {MeterNumber}", request.MeterNumber);
                return BadRequest(ex.Message);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("Vend/{validationRef}/pay")]
        public async Task<IActionResult> ProcessPayment(string validationRef)
        {
            _logger.LogInformation("Received request to process payment for bill with validation reference {ValidationRef}", validationRef);
            try
            {
                var walletIdClaim = User.Claims.FirstOrDefault(c => c.Type == "WalletId")?.Value;
                if (walletIdClaim == null)
                {
                    _logger.LogWarning("Logout attempt failed - no valid user ID found");
                    return Unauthorized("User not logged in");
                }
                if (!Guid.TryParse(walletIdClaim, out var walletId))
                {
                    _logger.LogWarning("Invalid Wallet ID format");
                    return BadRequest("Invalid Wallet ID format");
                }
                //  var walletId = Guid.Parse(Request.Headers["X-Wallet-Id"].ToString());
                var token = await _electricityService.ProcessPaymentAsync(validationRef, walletId);
                return Ok(new { token });
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                _logger.LogWarning(ex, "Payment failed for bill with validation reference {ValidationRef}", validationRef);
                return BadRequest(ex.Message);
            }
        }
    }


}
