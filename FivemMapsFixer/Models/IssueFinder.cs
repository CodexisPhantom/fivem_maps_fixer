using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using FivemMapsFixer.Models.Ytd;

namespace FivemMapsFixer.Models;

public static class IssueFinder
{
    public static List<Ymaps.YmapIssue> FindDuplicatesYmaps(string path)
    {
        string[] files = Directory.GetFiles(path, "*.ymap", SearchOption.AllDirectories);
        
        IEnumerable<IGrouping<string?, string>> groupedFiles = files.GroupBy(Path.GetFileName)
            .Where(group => group.Count() > 1);

        return groupedFiles.Select(group => new Ymaps.YmapIssue(path,new ObservableCollection<string>(group),FileType.Ymap)).ToList();
    }
    
    public static List<YtdIssue> FindOversizedYtds(string path)
    {
        if(!Directory.Exists(path)){return []; }
        string[] files = Directory.GetFiles(path, "*.ytd", SearchOption.AllDirectories);

        List<YtdIssue> issues = [];
        foreach (string file in files)
        {
            YtdFile ytd = YtdActions.OpenFile(file);
            if (ytd.TextureDict.MemoryUsage > 16000000)
            {
                YtdIssue ytdIssue = new(ytd, FileType.Ytd);
                List<string> names = [];
                // ReSharper disable once ForCanBeConvertedToForeach
                // ReSharper disable once LoopCanBeConvertedToQuery
                // enumerator wasn't created
                for (int i = 0; i < ytd.TextureDict.Textures.Count; i++)
                {
                    names.Add(ytd.TextureDict.Textures[i].Name);
                }
                ytdIssue.Names = names.ToArray();
                Array.Sort(ytdIssue.Names);
                issues.Add(ytdIssue);
            }
        }
        return issues;
    }
}