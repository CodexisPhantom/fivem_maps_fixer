using CodeWalker.GameFiles;
using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models;
using FivemMapsFixer.Models.Ymaps;
using FivemMapsFixer.Models.Ytd;
using FivemMapsFixer.ViewModels.Pages;
using FivemMapsFixer.ViewModels.Pages.Ymaps;
using FivemMapsFixer.ViewModels.Pages.Ytd;

namespace FivemMapsFixer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly MainPageViewModel _mainPageViewModel = new();
    
    private readonly RestoreBackupPageViewModel _restoreBackupPagePageViewModel = new();
    
    private readonly ShowDuplicatePageViewModel _showDuplicatePageViewModel = new();
    private CleanEntitiesPageViewModel? _cleanEntitiesPageViewModel;
    private CleanOcclusionsPageViewModel? _cleanOcclusionsPageViewModel;
    private CleanLodLightsPageViewModel? _cleanLodLightsPageViewModel;
    private CleanYtdPageViewModel? _cleanYtdPageViewModel;

    [ObservableProperty] private PageViewModel _currentPage;

    public MainWindowViewModel()
    {
        CurrentPage = _mainPageViewModel;
        Globals.MainPageRequested+= (_, _) => GoToMainPage();
        Globals.RestoreBackupPageRequested += (_, _) => GoToBackupPage();
        
        Globals.FixPageRequested += (_, e) => GoToFixPage(e.FileType);
        Globals.CleanEntitiesPageRequested+= (_, e) => GoToCleanEntitiesPage(e.Issue);
        Globals.CleanOcclusionsPageRequested+= (_, e) => GoToCleanOcclusionsPage(e.Issue);
        Globals.CleanLodLightsPageRequested+= (_, e) => GoToCleanLodLightsPage(e.Issue);
        Globals.CleanYtdPageRequested+= (_, e) => GoToCleanYtdPage(e.Issue);
        //_restoreBackupPageViewModel.GoBackEvent += (_, _) => GoToMainPage();
    }

    private void GoToCleanYtdPage(Issue issue)
    {
        if(issue is not YtdIssue ytdIssue){return;}
        _cleanYtdPageViewModel = new CleanYtdPageViewModel(ytdIssue);
        CurrentPage = _cleanYtdPageViewModel;
    }

    private void GoToFixPage(FileType type)
    {
        _showDuplicatePageViewModel.SetType(type);
        _showDuplicatePageViewModel.FindIssues();
        CurrentPage = _showDuplicatePageViewModel;
    }

    private void GoToBackupPage()
    {
        _restoreBackupPagePageViewModel.FindBackups();
        CurrentPage = _restoreBackupPagePageViewModel;
    }
    
    private void GoToCleanEntitiesPage(Issue issue)
    {
        if(issue is not YmapIssue ymapIssue){return;}
        if((ymapIssue.Files[0] as YmapFile).AllEntities == null)
        {
            Globals.InvokeOcclusionsPage(this, ymapIssue);
            return;
        }
        ymapIssue.FindEntitiesConflicts();
        _cleanEntitiesPageViewModel = new CleanEntitiesPageViewModel(ymapIssue);
        CurrentPage = _cleanEntitiesPageViewModel;
    }
    
    private void GoToCleanOcclusionsPage(Issue issue)
    {
        if(issue is not YmapIssue ymapIssue){return;}
        if((ymapIssue.Files[0] as YmapFile).BoxOccluders == null && (ymapIssue.Files[0] as YmapFile).OccludeModels == null)
        {
            Globals.InvokeLodLightsPage(this, ymapIssue);
            return;
        }
        ymapIssue.FindOcclusionsConflicts();
        _cleanOcclusionsPageViewModel = new CleanOcclusionsPageViewModel(ymapIssue);
        CurrentPage = _cleanOcclusionsPageViewModel;
    }
    
    private void GoToCleanLodLightsPage(Issue issue)
    {
        if(issue is not YmapIssue ymapIssue){return;}
        if((ymapIssue.Files[0] as YmapFile).LODLights == null)
        {
            Globals.InvokeMainPage(this);
            return;
        }
        _cleanLodLightsPageViewModel = new CleanLodLightsPageViewModel(ymapIssue);
        CurrentPage = _cleanLodLightsPageViewModel;
    }

    private void GoToMainPage()
    {
        CurrentPage = _mainPageViewModel;
    }
}