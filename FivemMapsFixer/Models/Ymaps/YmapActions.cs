using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using FivemMapsFixer.Tools;

namespace FivemMapsFixer.Models.Ymaps;

public abstract class YmapActions:FileActions
{
    public static YmapFile OpenFile(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        string name = new FileInfo(path).Name;
        RpfFileEntry fileEntry = CreateFileEntry(name, path, ref data);
        YmapFile? ymap = RpfFile.GetFile<YmapFile>(fileEntry, data);
        ymap.FilePath = path;
        return ymap;
    }
    
    public static void CreateBackup(List<YmapFile> files)
    {
        foreach (string path in files.Select(file => file.FilePath))
        {
            if(!File.Exists(path + ".backup")) File.Copy(path, path + ".backup");
            Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Backup created for {path}");
        }
    }
    
    public static void SaveFile(YmapFile file, string path)
    {
        file.CalcExtents();
        file.CalcFlags();
        byte[]? data = file.Save();
        File.Create(path).Close();
        if (data != null) File.WriteAllBytes(path, data);
        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"File saved to {path}");
    }
}