using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.ViewModels.Pages;

public class RestoreBackupPageViewModel:PageViewModel
{
    private ObservableCollection<Backup> _backups = [];
    public ObservableCollection<Backup> Backups
    {
        get => _backups;
        set => SetProperty(ref _backups, value);
    }
    
    public void FindBackups()
    {
        if(!Directory.Exists(Settings.Path)){return;}
        _backups.Clear();
        
        string[] backups = Directory.GetFiles(Settings.Path,"*.backup",SearchOption.AllDirectories);
        IEnumerable<IGrouping<string?, string>> groupedBackups = backups.GroupBy(Path.GetFileName);

        foreach (IGrouping<string?, string> group in groupedBackups)
        {
            Backup backup = new()
            {
                YmapFilesPath = new ObservableCollection<string>(group)
            };
            backup.Ended += (_,_) => { Backups.Remove(backup); };
            Backups.Add(backup);
        }
    }
}