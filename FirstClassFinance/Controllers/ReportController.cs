using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FirstClassFinance.Services;

namespace FirstClassFinance.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("trial-balance")]
        public async Task<IActionResult> GetTrialBalance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? email)
        {
            var report = await _reportService.GetTrialBalance(startDate, endDate);
            if (!string.IsNullOrEmpty(email))
            {
                await _reportService.SendReportNotification(email, "Trial Balance", endDate ?? DateTime.UtcNow);
            }
            return Ok(report);
        }

        [HttpGet("income-statement")]
        public async Task<IActionResult> GetIncomeStatement([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? email)
        {
            var report = await _reportService.GetIncomeStatement(startDate, endDate);
            if (!string.IsNullOrEmpty(email))
            {
                await _reportService.SendReportNotification(email, "Income Statement", endDate ?? DateTime.UtcNow);
            }
            return Ok(report);
        }

        [HttpGet("balance-sheet")]
        public async Task<IActionResult> GetBalanceSheet([FromQuery] DateTime? endDate, [FromQuery] string? email)
        {
            var report = await _reportService.GetBalanceSheet(endDate);
            if (!string.IsNullOrEmpty(email))
            {
                await _reportService.SendReportNotification(email, "Balance Sheet", endDate ?? DateTime.UtcNow);
            }
            return Ok(report);
        }

        [HttpGet("ratios")]
        public async Task<IActionResult> GetFinancialRatios([FromQuery] DateTime? endDate)
        {
            var ratios = await _reportService.GetFinancialRatios(endDate);
            return Ok(ratios);
        }
    }
}
