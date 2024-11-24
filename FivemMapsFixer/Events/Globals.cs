using System;
using FivemMapsFixer.Models;
using FivemMapsFixer.Models.Ymaps;
using FivemMapsFixer.Models.Ytd;

namespace FivemMapsFixer.Events;

public static class Globals
{
    public static EventHandler? MainPageRequested { get; set; }
    public static EventHandler? RestoreBackupPageRequested { get; set; }
    public static EventHandler<FixPageRequestedEventArgs>? FixPageRequested { get; set; }
    public static EventHandler<ConflictEventArgs>? CleanEntitiesPageRequested { get; set; }
    public static EventHandler<ConflictEventArgs>? CleanOcclusionsPageRequested { get; set; }
    public static EventHandler<ConflictEventArgs>? CleanLodLightsPageRequested { get; set; }
    public static EventHandler<ConflictEventArgs>? CleanYtdPageRequested { get; set; }

    public static void InvokeFixPage(object? sender, FileType type)
    {
        FixPageRequested?.Invoke(sender, new FixPageRequestedEventArgs(type));
    }

    public static void InvokeBackupPage(object? sender)
    {
        RestoreBackupPageRequested?.Invoke(sender, EventArgs.Empty);
    }

    public static void InvokeMainPage(object? sender)
    {
        MainPageRequested?.Invoke(sender, EventArgs.Empty);
    }
    
    public static void InvokeEntitiesPage(object? sender,YmapIssue ymapIssue)
    {
        CleanEntitiesPageRequested?.Invoke(sender, new ConflictEventArgs(ymapIssue));
    }
    
    public static void InvokeOcclusionsPage(object? sender,YmapIssue ymapIssue)
    {
        CleanOcclusionsPageRequested?.Invoke(sender, new ConflictEventArgs(ymapIssue));
    }
    
    public static void InvokeLodLightsPage(object? sender,YmapIssue ymapIssue)
    {
        CleanLodLightsPageRequested?.Invoke(sender, new ConflictEventArgs(ymapIssue));
    }
    
    public static void InvokeYtdPage(object? sender,YtdIssue ytdIssue)
    {
        CleanYtdPageRequested?.Invoke(sender, new ConflictEventArgs(ytdIssue));
    }
}