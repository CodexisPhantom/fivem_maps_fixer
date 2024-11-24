using System.IO;
using CodeWalker.GameFiles;

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
    
    public static void SaveFile(YmapIssue ymapIssue)
    {
        YmapFile? ymap = ymapIssue.Files[0] as YmapFile;
        ymap?.CalcExtents();
        ymap?.CalcFlags();
        foreach (string file in ymapIssue.YmapFilesPath)
        {
            if (!File.Exists(file+".backup")) File.Move(file, file+".backup");
        }
        byte[]? data = ymap?.Save();
        File.WriteAllBytes(ymapIssue.YmapFilesPath[0],data);
    }
}