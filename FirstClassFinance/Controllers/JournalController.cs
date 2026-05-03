using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FirstClassFinance.Services;
using FirstClassFinance.Data;
using FirstClassFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FirstClassFinance.Controllers;

[Route("api/[controller]")]
[ApiController]

public class JournalController : ControllerBase
{
    private readonly JournalService _journalService;
    private readonly AppDBContext _context;

    public JournalController(JournalService service, AppDBContext context)
    {
        _journalService = service;
        _context = context;
    }
    
    [HttpGet("journal-entries")]
    public async Task<IActionResult> GetAllJournalEntries()
    {
        var entries = await _context.JournalEntries
            .Include(j => j.journalLines)
            .OrderByDescending(j => j.entryDate)
            .ToListAsync();
        return Ok(entries);
    }

    [HttpPost("journal-entries")]
    public async Task<IActionResult> AddJournalEntry([FromBody] JournalModel journalEntry)
    {
        try
        {
            var result = await _journalService.AddJournalEntry(journalEntry);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("journal-entries/{id}")]
    public async Task<IActionResult> GetJournalEntry(int id)
    {
        var result = await _journalService.GetJournalEntry(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPatch("journal-entries/{id}/status")]
    public async Task<IActionResult> UpdateJournalStatus(int id, [FromBody] StatusUpdateRequest update)
    {
        try
        {
            var result = await _journalService.UpdateJournalStatus(id, update.Status, update.ApproverId, update.ApproverName, update.Comment);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class StatusUpdateRequest
{
    public string Status { get; set; }
    public int ApproverId { get; set; }
    public string ApproverName { get; set; }
    public string? Comment { get; set; }
}