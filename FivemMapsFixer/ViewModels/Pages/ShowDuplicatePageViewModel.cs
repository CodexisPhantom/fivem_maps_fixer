using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FivemMapsFixer.Models;
using FivemMapsFixer.Models.Ymaps;
using FivemMapsFixer.Models.Ytd;

namespace FivemMapsFixer.ViewModels.Pages;

public class ShowDuplicatePageViewModel:PageViewModel
{
    private FileType _type;
    private ObservableCollection<Issue> _conflicts = [];
    public ObservableCollection<Issue> Conflicts
    {
        get => _conflicts;
        set => SetProperty(ref _conflicts, value);
    }
    
    public void SetType(FileType type)
    {
        _type = type;
    }
    
    public void FindIssues()
    {
        switch (_type)
        {
            case FileType.Ymap:
                FindYmapsIssues();
                break;
            case FileType.Ytd:
                FindYtdsIssues();
                break;
        }
    }

    private void FindYtdsIssues()
    {
        List<YtdIssue> duplicates = IssueFinder.FindOversizedYtds(Settings.Path);
        Conflicts = new ObservableCollection<Issue>(duplicates);
    }

    private void FindYmapsIssues()
    {
        if(!Directory.Exists(Settings.Path)){return;}
        _conflicts.Clear();

        List<YmapIssue> duplicates = IssueFinder.FindDuplicatesYmaps(Settings.Path);
        
        foreach (YmapIssue duplicate in duplicates.ToList())
        {
            if(duplicate.YmapFilesPath[0].Contains("lodlights") && !duplicate.YmapFilesPath[0].Contains("distlodlights"))
            {
                duplicates.Remove(duplicate);
                continue;
            }
            duplicate.Ended += (_,_) =>
            {
                Conflicts.Remove(duplicate);
            };
        }
        Conflicts = new ObservableCollection<Issue>(duplicates);
    }
}