using System;
using System.Collections.ObjectModel;
using CodeWalker.GameFiles;
using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Models.Ymaps;
using FivemMapsFixer.Models.Ytd;

namespace FivemMapsFixer.Models;

public partial class Issue:ObservableObject
{
    [ObservableProperty] private FileType _type;
    private protected string _basePath;
    
    protected ObservableCollection<GameFile> _files = null!;
    
    public EventHandler? Ended { get; set; }
    public ObservableCollection<GameFile> Files
    {
        get => _files;
        set => SetProperty(ref _files, value);
    }
    
    public void SavePrincipal()
    {
        switch (Type)
        {
            case FileType.Ymap:YmapActions.SaveFile(this as YmapIssue);
                break;
            case FileType.Ytd:YtdActions.SaveFile(this as YtdIssue);
                break;
        }
        
        Ended?.Invoke(this, EventArgs.Empty);
    }
}