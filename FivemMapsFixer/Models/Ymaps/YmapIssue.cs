using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CodeWalker.GameFiles;
using CommunityToolkit.Mvvm.ComponentModel;

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

     public YmapIssue(string basePath,ObservableCollection<string> ymapFilesPath,FileType type)
     {
          _basePath = basePath;
          YmapFilesPath = ymapFilesPath;
          Type = type;
     }
     
     public void FixYmapIssue()
     {
          string name = _ymapFilesPath.First().Split('\\').Last();
          
          List<YmapFile> ymapFiles = YmapSearch.Start(name).Result;
          List<YmapFile> files = YmapFilesPath.Select(YmapActions.OpenFile).ToList();
          
          YmapActions.CreateBackup(files);
          YmapFix.Start(ymapFiles.Count == 0 ? files[0] : ymapFiles.First(), files, _ymapFilesPath.First());
     }
}
