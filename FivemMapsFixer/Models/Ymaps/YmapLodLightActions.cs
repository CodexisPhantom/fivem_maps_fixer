using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;

namespace FivemMapsFixer.Models.Ymaps;

public abstract class YmapLodLightActions
{
    public static ObservableCollection<YmapFile> CleanDistLodLight(ObservableCollection<YmapFile> distLodLights,string[] distLodLightsPath)
    {
        if(distLodLights[0].DistantLODLights == null) {return distLodLights;}
        YmapFile[] lodLights = new YmapFile[distLodLights.Count];
        for (int i = 0; i < lodLights.Length; i++)
        {
            lodLights[i] = YmapActions.OpenFile(distLodLightsPath[i].Replace("distlodlights","lodlights"));
            lodLights[i].LODLights.BuildLodLights(distLodLights[i].DistantLODLights);
        }
        
        lodLights[0].LODLights.LodLights = lodLights[0].LODLights.LodLights.Where(entity =>
        {
            foreach (YmapFile file in lodLights.Skip(1))
            {
                YmapLODLight? other = file.LODLights.LodLights?.FirstOrDefault(e => e.Position == entity.Position);
                if(other == null) return false;
            }

            return true;
        }).ToArray();
        
        lodLights[0].LODLights.RebuildFromLodLights();
        distLodLights[0].DistantLODLights.RebuildFromLodLights(lodLights[0].LODLights.LodLights);
        SaveLodLights(lodLights[0],distLodLightsPath.Select(path => path.Replace("distlodlights","lodlights")).ToArray());
        return distLodLights;
    }
    
    private static void SaveLodLights(YmapFile ymap, string[] ymapFilesPath)
    {
        ymap.CalcExtents();
        ymap.CalcFlags();
        foreach (string file in ymapFilesPath)
        {
            File.Move(file, file+".backup");
        }
        byte[]? data =ymap.Save();
        File.WriteAllBytes(ymapFilesPath[0],data);
    }
}