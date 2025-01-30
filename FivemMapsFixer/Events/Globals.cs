using System;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.Events;

public static class Globals
{
    public static EventHandler? MainPageRequested { get; set; }
    public static EventHandler? RestoreBackupPageRequested { get; set; }
    public static EventHandler<FixPageRequestedEventArgs>? FixPageRequested { get; set; }

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
}