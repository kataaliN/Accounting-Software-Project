using System;
using System.Text.Json;
using FirstClassFinance.Data;
using FirstClassFinance.Models;

namespace FirstClassFinance.Services;

public class EventLogService
{
    private readonly AppDBContext _context;
    public EventLogService(AppDBContext context)
    {
        _context = context;
    }
    
    // logic for how logging should be done
    // it should account for additions, deactivations, etc
    public void LogEvent(string  entityName, int  entityId, int userId, string action, object beforeImg, object afterImg)
    {
        var logEntry = new EventLogModel
        {
            EntityName = entityName,
            EntityID = entityId,
            UserID = userId,
            ActionType = action,
            EntityBeforeState = beforeImg == null ? null : JsonSerializer.Serialize(beforeImg),
            EntityAfterState = afterImg ==  null ? null : JsonSerializer.Serialize(afterImg),
            ChangeTimeStamp =  DateTime.UtcNow
        };
        
        _context.EventLogs.Add(logEntry);
        _context.SaveChanges();
    }
}