using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

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
}