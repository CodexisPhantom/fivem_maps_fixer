using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.ViewModels.Pages;

public partial class MainPageViewModel:PageViewModel
{
    [ObservableProperty] private string _path = Settings.Path;
    [ObservableProperty] private FileType _ymaps = FileType.Ymap;
    [ObservableProperty] private FileType _ydrs = FileType.Ydr;
    [ObservableProperty] private FileType _ybns = FileType.Ybn;
    [ObservableProperty] private FileType _ymts = FileType.Ymt;
    [ObservableProperty] private FileType _ytds = FileType.Ytd;
    

    public MainPageViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(Path)) return;
            Settings.Path = Path;
        };
    }

    public void ChangeToFixPage(FileType type)
    {
        Globals.InvokeFixPage(this, type);
    }
    public void ChangeToRestoreBackupPage()
    {
        Globals.InvokeBackupPage(this);
    }
}