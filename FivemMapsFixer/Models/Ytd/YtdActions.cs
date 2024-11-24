using System;
using System.IO;
using CodeWalker.GameFiles;

namespace FivemMapsFixer.Models.Ytd;

public abstract class YtdActions:FileActions
{
    public static YtdFile OpenFile(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        string name = new FileInfo(path).Name;
        RpfFileEntry fileEntry = CreateFileEntry(name, path, ref data);
        YtdFile? ymap = RpfFile.GetFile<YtdFile>(fileEntry, data);
        ymap.FilePath = path;
        return ymap;
    }

    public static void SaveFile(YtdIssue issue)
    {
        Console.WriteLine("saving ytd not implemented");
    }
}