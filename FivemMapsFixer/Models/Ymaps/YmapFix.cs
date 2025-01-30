using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeWalker.GameFiles;
using FivemMapsFixer.Tools;
using SharpDX;

namespace FivemMapsFixer.Models.Ymaps;

public abstract class YmapFix
{
    private static bool ComparePosition(Vector3 a, Vector3 b)
    {
        return Math.Abs(a.X - b.X) < 0.05 && Math.Abs(a.Y - b.Y) < 0.05 && Math.Abs(a.Z - b.Z) < 0.05;
    }
    
    private static void FixFromPosition<T>(YmapFile mainFile, List<YmapFile> ymapFiles, 
        Func<YmapFile, IEnumerable<T>> getEntities, 
        Func<T, Vector3> getPosition,
        Action<YmapFile, T[]> setEntities,
        string entityName)
    {
        List<T> filteredEntities = [];
        IEnumerable<T> newEntities = getEntities(mainFile);
        T[] mainEntities = newEntities == null ? [] : newEntities.ToArray();
        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Starting {entityName} fixes for main file with {mainEntities.Length} entities");
        
        foreach (T entity in mainEntities)
        {
            bool isRemovedSomewhere = false;
            bool isPresentSomewhere = false;
            Vector3 position = getPosition(entity);

            foreach (bool foundInFile in ymapFiles.Select(file => getEntities(file).Any(otherEntity => ComparePosition(position, getPosition(otherEntity)))))
            {
                if (foundInFile)
                {
                    isPresentSomewhere = true;
                }
                else
                {
                    isRemovedSomewhere = true;
                }
            }

            if (!isRemovedSomewhere || (isPresentSomewhere && !isRemovedSomewhere))
            {
                filteredEntities.Add(entity);
            }
            else
            {
                Logger.Log(LogSeverity.INFO, LogType.YMAP, $"{entityName} at {position} removed from at least one file, filtering out.");
            }
        }
        
        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Filtering complete. Remaining {entityName}s: {filteredEntities.Count}");
        setEntities(mainFile, filteredEntities.ToArray());
    }
    
    private static YmapEntityDef[] FilterEntitiesByGuids(YmapEntityDef[] entities, IEnumerable<YmapFile> ymapFiles)
    {
        List<YmapEntityDef> filteredEntities = [];

        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Starting entity filtering. Total entities in mainFile: {entities.Length}");

        foreach (YmapEntityDef entity in entities)
        {
            bool isRemovedSomewhere = false;
            bool isPresentSomewhere = false;

            foreach (YmapFile file in ymapFiles)
            {
                bool foundInFile = file.AllEntities.Any(otherEntity => entity.CEntityDef.guid == otherEntity.CEntityDef.guid);

                if (foundInFile)
                {
                    isPresentSomewhere = true;
                }
                else
                {
                    isRemovedSomewhere = true;
                }
            }

            if (!isRemovedSomewhere || (isPresentSomewhere && !isRemovedSomewhere))
            {
                filteredEntities.Add(entity);
            }
            else
            {
                Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Entity GUID {entity.CEntityDef.guid} removed from at least one file, filtering out.");
            }
        }

        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Filtering complete. Remaining entities: {filteredEntities.Count}");

        return filteredEntities.ToArray();
    }
    
    private static void UpdateEntityPositions(YmapEntityDef[] entities, IEnumerable<YmapFile> ymapFiles)
    {
        Logger.Log(LogSeverity.INFO, LogType.YMAP, "Building entity lookup dictionary from YMAP files");
        Dictionary<uint, YmapEntityDef> entityLookup = ymapFiles
            .SelectMany(file => file.AllEntities)
            .GroupBy(e => e.CEntityDef.guid)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(e => e.Position.Z).First()
            );
        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Created lookup dictionary with {entityLookup.Count} entries");

        int updatedCount = 0;
        foreach (YmapEntityDef entity in entities)
        {
            if (entityLookup.TryGetValue(entity.CEntityDef.guid, out YmapEntityDef? matchingEntity))
            {
                if (entity.Position == matchingEntity.Position) continue;
                Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Updating position for entity GUID {entity.CEntityDef.guid} from {entity.Position} to {matchingEntity.Position}");
                entity.Position = matchingEntity.Position;
                updatedCount++;
            }
            else
            {
                Logger.Log(LogSeverity.WARNING, LogType.YMAP, $"No matching entity found for GUID {entity.CEntityDef.guid}");
            }
        }
        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Updated positions for {updatedCount} entities");
    }

    private static void FixProps(YmapFile mainFile, IEnumerable<YmapFile> ymapFiles)
    {
        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Starting prop fixes for main file with {mainFile.AllEntities.Length} entities");
        
        YmapEntityDef[] newEntities = FilterEntitiesByGuids(mainFile.AllEntities, ymapFiles);
        UpdateEntityPositions(newEntities, ymapFiles);

        Logger.Log(LogSeverity.INFO, LogType.YMAP, $"Updating main file entities from {mainFile.AllEntities.Length} to {newEntities.Length}");
        mainFile.AllEntities = newEntities;
        mainFile.RootEntities = newEntities;
        Logger.Log(LogSeverity.INFO, LogType.YMAP, "Prop fixes completed");
    }
    
    private static void FixOcclusionModels(YmapFile mainFile, List<YmapFile> ymapFiles)
    {
        mainFile.OccludeModels = null;
        // TODO: Better occlusion model handling
    }
    
    private static void FixBoxOcclusion(YmapFile mainFile, List<YmapFile> ymapFiles)
    {
        mainFile.BoxOccluders = null;
        // TODO: Better box occluder handling
    }

    private static void FixCarGenerators(YmapFile mainFile, List<YmapFile> ymapFiles)
    {
        FixFromPosition(
            mainFile, 
            ymapFiles, 
            file => file.CarGenerators, 
            carGen => carGen.Position,
            (file, entities) => file.CarGenerators = entities,
            "Car generator"
        );
    }

    private static void FixLodLights(YmapFile mainFile, List<YmapFile> ymapFiles)
    {
        FixFromPosition(
            mainFile,
            ymapFiles, 
            file => file.LODLights.LodLights, 
            lodLight => lodLight.Position,
            (file, entities) => file.LODLights.LodLights = entities,
            "LOD light"
        );
        mainFile.LODLights.RebuildFromLodLights();
    }
    
    private static void FixDistantLodLights(YmapFile mainFile, List<YmapFile> ymapFiles)
    {
        FixFromPosition(
            mainFile, 
            ymapFiles, 
            file => file.DistantLODLights.positions, 
            position => position.ToVector3(),
            (file, entities) => file.DistantLODLights.positions = entities,
            "Distant LOD light"
        );
        YmapFile copy = new();
        copy.LODLights.BuildLodLights(mainFile.DistantLODLights);
        mainFile.DistantLODLights.RebuildFromLodLights(copy.LODLights.LodLights);
    }
    
    public static void Start(YmapFile main, List<YmapFile> files, string name)
    {
        if (files[0].AllEntities != null)
        {
            FixProps(main, files);
        }
        
        if (files[0].OccludeModels != null)
        {
            FixOcclusionModels(main, files);
        }
        
        if (files[0].BoxOccluders != null)
        {
            FixBoxOcclusion(main, files);
        }
        
        if (files[0].CarGenerators != null)
        {
            FixCarGenerators(main, files);
        }
        
        if (files[0].LODLights != null)
        {
            FixLodLights(main, files);
        }
        
        if (files[0].DistantLODLights != null)
        {
            FixDistantLodLights(main, files);
        }

        foreach (var file in files.Where(file => File.Exists(file.FilePath)))
        {
            File.Delete(file.FilePath);
        }
        YmapActions.SaveFile(main, name);
    }
}