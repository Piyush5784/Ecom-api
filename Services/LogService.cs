﻿
using Ecom_api.Data;
using Ecom_api.Interfaces;
using Ecom_api.Models;

namespace Ecom_api.Services
{


public class LogService : ILogService
{
    private readonly ApplicationDbContext db;

    public LogService(ApplicationDbContext context)
    {
        db = context;
    }

    public async Task LogAsync(string Type, string message, string? controller = null, string? action = null,
                               string? stackTrace = null, string? path = null, string? userName = null)
    {
        var log = new Logs
        {
            Type = Type,
            Message = message,
            Controller = controller,
            Action = action,
            StackTrace = stackTrace,
            RequestPath = path,
            UserName = userName
        };

        db.Logs.Add(log);
        await db.SaveChangesAsync();
    }
}
}