using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.ViewModels.Pages;

public partial class MainPageViewModel:PageViewModel
{
    [ObservableProperty] private string _path = Settings.Path;
    [ObservableProperty] private string _gtapath = Settings.GTAPath;
    [ObservableProperty] private FileType _ymaps = FileType.Ymap;
    

    public MainPageViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Path):
                    Settings.Path = Path;
                    break;
                case nameof(Gtapath):
                    Settings.GTAPath = Gtapath;
                    break;
            }
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
    
    public void LoadGta()
    {
        GTA.Load();
    }
}