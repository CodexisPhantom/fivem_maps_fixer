using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CodeWalker.GameFiles;

namespace FivemMapsFixer.Models.Ymaps;

public abstract class YmapOcclusionsActions
{
    public static List<YmapBoxOccluder> FindBoxOccluders(ObservableCollection<GameFile> files)
    {
        List<YmapBoxOccluder> boxOccluders = [];
        
        if(files[0] is not YmapFile ymapFile) return boxOccluders;
        if (ymapFile.BoxOccluders == null) return boxOccluders;
        
        foreach (YmapBoxOccluder boxOccluder in ymapFile.BoxOccluders)
        {
            foreach (GameFile gameFile in files.Skip(1))
            {
                if(gameFile is not YmapFile file) continue;
                YmapBoxOccluder? other = file.BoxOccluders?.FirstOrDefault(e => e.Position == ymapFile.BoxOccluders[0].Position);
                if(other == null && !boxOccluders.Contains(boxOccluder)) boxOccluders.Add(boxOccluder);
            }
        }

        return boxOccluders;
    }

    public static List<YmapOccludeModel> FindOccludeModels(ObservableCollection<GameFile> files)
    {
        List<YmapOccludeModel> occludeModels = [];
        
        if(files[0] is not YmapFile ymapFile) return occludeModels;
        if (ymapFile.OccludeModels == null) return occludeModels;
        
        foreach (YmapOccludeModel occludeModel in ymapFile.OccludeModels)
        {
            foreach (GameFile gameFile in files.Skip(1))
            {
                if(gameFile is not YmapFile file) continue;
                YmapOccludeModel? other = file.OccludeModels?.FirstOrDefault(e => e.Vertices == occludeModel.Vertices);
                if(other == null && !occludeModels.Contains(occludeModel)) {occludeModels.Add(occludeModel);}
            }
        }

        return occludeModels;
    }
    
    public static GameFile RemoveBoxOcclusions(GameFile file,List<YmapBoxOccluder> occlusions)
    {
        if(file is not YmapFile ymapFile) return file;
        if (ymapFile.BoxOccluders == null) return file;
        List<YmapBoxOccluder> entities = ymapFile.BoxOccluders.ToList();
        foreach (YmapBoxOccluder entity in occlusions)
        {
            entities.Remove(entity);
        }
        ymapFile.BoxOccluders = entities.ToArray();

        return ymapFile;
    }
    
    public static GameFile RemoveModelOcclusions(GameFile file,List<YmapOccludeModel> occlusions)
    {
        if(file is not YmapFile ymapFile) return file;
        if (ymapFile.OccludeModels == null) return file;
        List<YmapOccludeModel> entities = ymapFile.OccludeModels.ToList();
        foreach (YmapOccludeModel entity in occlusions)
        {
            entities.Remove(entity);
        }
        ymapFile.OccludeModels = entities.ToArray();

        return ymapFile;
    }
}