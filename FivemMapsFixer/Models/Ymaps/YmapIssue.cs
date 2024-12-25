using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CodeWalker.GameFiles;
using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;

namespace FivemMapsFixer.Models.Ymaps;

public partial class YmapIssue:Issue
{
     [ObservableProperty] private int _selectedIndex;
     private readonly ObservableCollection<string> _ymapFilesPath = [];
     public ObservableCollection<string> YmapFilesPath
     {
          get => _ymapFilesPath;
          init => SetProperty(ref _ymapFilesPath, value);
     }
     
     private ObservableCollection<YmapsEntitiesToFix> _entitiesToRemove = null!;
     public ObservableCollection<YmapsEntitiesToFix> EntitiesToRemove
     {
          get => _entitiesToRemove;
          set => SetProperty(ref _entitiesToRemove, value);
     }
     
     private ObservableCollection<YmapsEntitiesToFix> _entitiesToChange = null!;
     public ObservableCollection<YmapsEntitiesToFix> EntitiesToChange
     {
          get => _entitiesToChange;
          set => SetProperty(ref _entitiesToChange, value);
     }
     
     private ObservableCollection<YmapBoxOccluder> _boxOcclusionsToRemove = null!;
     public ObservableCollection<YmapBoxOccluder> BoxOcclusionsToRemove
     {
          get => _boxOcclusionsToRemove;
          set => SetProperty(ref _boxOcclusionsToRemove, value);
     }
     
     private ObservableCollection<YmapOccludeModel> _modelOcclusionsToRemove = null!;
     public ObservableCollection<YmapOccludeModel> ModelOcclusionsToRemove
     {
          get => _modelOcclusionsToRemove;
          set => SetProperty(ref _modelOcclusionsToRemove, value);
     }

     public YmapIssue(string basePath,ObservableCollection<string> ymapFilesPath,FileType type)
     {
          _basePath = basePath;
          YmapFilesPath = ymapFilesPath;
          Type = type;
     }
     
     public void OpenEntitiesPage()
     {
          _ymapFilesPath.Move(SelectedIndex,0);
          SelectedIndex = 0;
          List<YmapFile> files = YmapFilesPath.Select(YmapActions.OpenFile).ToList();
          Files = new ObservableCollection<GameFile>(files);
          Globals.InvokeEntitiesPage(this,this);
     }
     
     public void FindEntitiesConflicts()
     {
          (List<YmapsEntitiesToFix> entitiesToRemoves, List<YmapsEntitiesToFix> entitiesToChanges) = YmapEntitiesActions.FindEntitiesConflicts(Files, _basePath);
          EntitiesToRemove = new ObservableCollection<YmapsEntitiesToFix>(entitiesToRemoves);
          EntitiesToChange = new ObservableCollection<YmapsEntitiesToFix>(entitiesToChanges);
          Task.Run(GetNames); 
     }

     private void GetNames()
     {
          EntitiesToRemove.ToList().ForEach(e => e.LoadName());
          EntitiesToChange.ToList().ForEach(e => e.LoadName());
     }

     public void FixEntitiesConflicts()
     {
          Files[0] = YmapEntitiesActions.RemoveEntities(Files[0],EntitiesToRemove.ToList());
          Files[0] = YmapEntitiesActions.UpdateEntities(Files[0],EntitiesToChange.ToList());
          Globals.InvokeOcclusionsPage(this,this);
     }
     
     public void FindOcclusionsConflicts()
     {
          BoxOcclusionsToRemove = new ObservableCollection<YmapBoxOccluder>(YmapOcclusionsActions.FindBoxOccluders(Files));
          ModelOcclusionsToRemove = new ObservableCollection<YmapOccludeModel>(YmapOcclusionsActions.FindOccludeModels(Files));
     }

     public void FixOcclusionsConflicts()
     {
          Files[0] = YmapOcclusionsActions.RemoveBoxOcclusions(Files[0],BoxOcclusionsToRemove.ToList());
          Files[0] = YmapOcclusionsActions.RemoveModelOcclusions(Files[0],ModelOcclusionsToRemove.ToList());
          Globals.InvokeOcclusionsPage(this,this);
     }
     
     public void FixLodLightsConflicts()
     {
           ObservableCollection<GameFile> ymaps  = new(YmapLodLightActions.CleanDistLodLight(new ObservableCollection<YmapFile>(Files.Select(e => (YmapFile)e)),YmapFilesPath.ToArray()));
           Files = ymaps;
     }
}
