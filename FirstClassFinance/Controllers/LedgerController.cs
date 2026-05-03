using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Models;
using FirstClassFinance.Data;

namespace FirstClassFinance.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _ledgerService;
    private readonly AppDBContext _context;

    public LedgerController(ILedgerService ledgerService, AppDBContext context)
    {
        _ledgerService = ledgerService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLedgerEntries([FromQuery] int? accountId)
    {
        try
        {
            var entries = await _ledgerService.GetLedgerEntries(accountId);
            var result  = await EnrichEntries(entries);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("account/{accountId}")]
    public async Task<IActionResult> GetAccountLedger(int accountId)
    {
        try
        {
            var entries = await _ledgerService.GetLedgerEntries(accountId);
            var result  = await EnrichEntries(entries);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<List<object>> EnrichEntries(List<LedgerEntryModel> entries)
    {
        var journalIds = entries.Select(e => e.journalEntryId).Distinct().ToList();
        var journals   = await _context.JournalEntries
            .Where(j => journalIds.Contains(j.journalId))
            .ToDictionaryAsync(j => j.journalId, j => j.journalName ?? "");

        return entries.Select(e => (object)new
        {
            e.ledgerId,
            pr          = $"JE{e.journalEntryId:D4}",
            e.accountId,
            date        = e.date.ToString("yyyy-MM-dd"),
            debit       = e.debits,
            credit      = e.credits,
            e.balance,
            description = journals.GetValueOrDefault(e.journalEntryId, "")
        }).ToList();
    }
}