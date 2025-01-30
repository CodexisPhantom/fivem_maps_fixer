using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models;
using FivemMapsFixer.ViewModels.Pages;

namespace FivemMapsFixer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly MainPageViewModel _mainPageViewModel = new();
    private readonly RestoreBackupPageViewModel _restoreBackupPagePageViewModel = new();
    private readonly ShowDuplicatePageViewModel _showDuplicatePageViewModel = new();
    
    [ObservableProperty] private PageViewModel _currentPage;

    public MainWindowViewModel()
    {
        CurrentPage = _mainPageViewModel;
        
        Globals.MainPageRequested+= (_, _) => GoToMainPage();
        Globals.RestoreBackupPageRequested += (_, _) => GoToBackupPage();
        Globals.FixPageRequested += (_, e) => GoToFixPage(e.FileType);
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

    private void GoToMainPage()
    {
        CurrentPage = _mainPageViewModel;
    }
}