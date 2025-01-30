using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FivemMapsFixer.Models;

public class Backup:ObservableObject
{
    private ObservableCollection<string> _ymapFilesPath = [];
    public ObservableCollection<string> YmapFilesPath
    {
        get => _ymapFilesPath;
        set => SetProperty(ref _ymapFilesPath, value);
    }

    public EventHandler? Ended;
    
    public void RestoreBackup()
    {
        foreach (string backup in YmapFilesPath)
        {
            string file = backup.Replace(".backup", "");
            File.Move(backup, file, true);
        }
        Ended?.Invoke(this, EventArgs.Empty);
    }
}