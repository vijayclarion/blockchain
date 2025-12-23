using ClaimsApi.Models;
using ClaimsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly IFabricService _fabricService;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(IFabricService fabricService, ILogger<ClaimsController> logger)
        {
            _fabricService = fabricService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var claims = await _fabricService.GetAllClaimsAsync();
                return Ok(claims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting claims");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                var claim = await _fabricService.GetClaimAsync(id);
                return Ok(claim);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClaimDto dto)
        {
            try
            {
                var result = await _fabricService.CreateClaimAsync(dto);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(string id)
        {
            try
            {
                var result = await _fabricService.ApproveClaimAsync(id);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/settle")]
        public async Task<IActionResult> Settle(string id)
        {
            try
            {
                var result = await _fabricService.SettleClaimAsync(id);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("init")]
        public async Task<IActionResult> InitLedger()
        {
            try
            {
                await _fabricService.InitLedgerAsync();
                return Ok(new { message = "Ledger Initialized" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
