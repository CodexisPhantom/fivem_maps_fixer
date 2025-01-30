using System;
using CodeWalker.GameFiles;

namespace FivemMapsFixer.Models;

public class GTA
{
    public static bool IsLoaded { get; private set; }
    public static RpfManager Manager { get; } = new();
    
    public static void Load()
    {
        GTA5Keys.LoadFromPath(Settings.GTAPath);
        Manager.Init(Settings.GTAPath, Console.WriteLine, Console.WriteLine);
        IsLoaded = true;
    }
}