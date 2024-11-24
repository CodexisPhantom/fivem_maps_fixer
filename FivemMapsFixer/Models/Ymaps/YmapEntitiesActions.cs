using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CodeWalker.GameFiles;
using SharpDX;

namespace FivemMapsFixer.Models.Ymaps;

public abstract class YmapEntitiesActions
{
    public static (List<YmapsEntitiesToFix>,List<YmapsEntitiesToFix>) FindEntitiesConflicts(ObservableCollection<GameFile> files, string basePath)
    {
        if(files[0] is not YmapFile ymapFile) return ([], []);
        List<YmapsEntitiesToFix> entitiesToRemove = [];
        List<YmapsEntitiesToFix> entitiesToChange = [];
        
        foreach (YmapEntityDef entity in ymapFile.AllEntities)
        {
            entity.Position = new Vector3((float)Math.Round(entity.Position.X, 4),(float)Math.Round(entity.Position.Y, 4),(float)Math.Round(entity.Position.Z, 4));
            
            YmapEntityDef copyEntity = new()
            {
                Position = new Vector3(entity.Position.X,entity.Position.Y,entity.Position.Z),
                CEntityDef = entity.CEntityDef,
                Archetype = entity.Archetype,
                Extensions = entity.Extensions
            };
            
            foreach (GameFile gameFile in files.Skip(1))
            {
                if(gameFile is not YmapFile file) continue;
                YmapEntityDef? other = file.AllEntities.FirstOrDefault(e => e.CEntityDef.guid == entity.CEntityDef.guid);
                if (other == null)
                {
                    entitiesToRemove.Add(new YmapsEntitiesToFix(copyEntity,entity,file.FilePath, basePath));
                    continue;
                }
                other.Position = new Vector3((float)Math.Round(other.Position.X, 4),(float)Math.Round(other.Position.Y, 4),(float)Math.Round(other.Position.Z, 4));

                if (Math.Abs(entity.Position.X - other.Position.X) < 0.01f && Math.Abs(entity.Position.Y - other.Position.Y) < 0.01f && Math.Abs(entity.Position.Z - other.Position.Z) < 0.01f) continue;
                if (entity.Position.Z < other.Position.Z) continue;
                
                entity.SetPosition(new Vector3(other.Position.X,other.Position.Y,-200.0f));
                entitiesToChange.Add(new YmapsEntitiesToFix(copyEntity,entity,file.FilePath, basePath));
            }
        }
        
        entitiesToRemove = entitiesToRemove.DistinctBy(e => e.Entity.CEntityDef.guid).ToList();
        entitiesToChange = entitiesToChange.DistinctBy(e => e.Entity.CEntityDef.guid).ToList();
        
        foreach (YmapsEntitiesToFix toRemove in entitiesToRemove)
        {
            entitiesToChange.RemoveAll(e => e.Entity.CEntityDef.guid == toRemove.Entity.CEntityDef.guid);
        }
        
        return (entitiesToRemove,entitiesToChange);
    }
    
    public static GameFile RemoveEntities(GameFile file,List<YmapsEntitiesToFix> entitiesToRemove)
    {
        if(file is not YmapFile ymapFile) return file;
        List<YmapEntityDef> entities = ymapFile.AllEntities.ToList();
        foreach (YmapsEntitiesToFix entity in entitiesToRemove.Where(entity => entity.IsToFix))
        {
            entities.Remove(entity.Entity);
        }
        ymapFile.AllEntities = entities.ToArray();
        return ymapFile;
    }
    
    public static GameFile UpdateEntities(GameFile file,List<YmapsEntitiesToFix> entities)
    {
        if(file is not YmapFile ymapFile) return file;
        foreach (YmapsEntitiesToFix entity in entities.Where(entity => entity.IsToFix))
        {
            ymapFile.AllEntities.First(e => e.CEntityDef.guid == entity.Entity.CEntityDef.guid).SetPosition(new Vector3(entity.Entity.Position.X,entity.Entity.Position.Y,entity.Entity.Position.Z));
        }
        return ymapFile;
    }
}