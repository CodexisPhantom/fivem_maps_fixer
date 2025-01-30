using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeWalker.GameFiles;

namespace FivemMapsFixer.Models.Ymaps;

public abstract class YmapSearch
{
    public static Task<List<YmapFile>> Start(string name)
    {
        Console.WriteLine(@$"Searching for {name} ...");
        return Task.Run(() =>
        {
            if (!GTA.IsLoaded)
            {
                Console.WriteLine(@"GTA is not loaded.");
                return [];
            }
            
            Console.WriteLine(@"Searching for ymaps...");
            List<YmapFile> results = [];

            foreach (RpfFile? rpf in GTA.Manager.AllRpfs)
            {
                if (rpf.Path.Contains("mods", StringComparison.CurrentCultureIgnoreCase)) continue;
                foreach (RpfEntry? entry in rpf.AllEntries)
                {
                    try
                    {
                        string nameLower = entry.NameLower;
                        if (!nameLower.EndsWith(".ymap")) continue;
                        if (nameLower != name) continue;
                        YmapFile ymap = RpfManager.GetFile<YmapFile>(entry);
                        if (ymap?.AllEntities == null) continue;
                        results.Add(ymap);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            
            Console.WriteLine(@"Ymaps found: " + results.Count);
            return results;
        });
    }
}