using System;
using System.Collections.ObjectModel;
using CodeWalker.GameFiles;
using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Models.Ymaps;

namespace FivemMapsFixer.Models;

public partial class Issue:ObservableObject
{
    [ObservableProperty] private FileType _type;
    private protected string _basePath;

    private ObservableCollection<GameFile> _files = null!;
    
    public EventHandler? Ended { get; set; }
    public ObservableCollection<GameFile> Files
    {
        get => _files;
        protected set => SetProperty(ref _files, value);
    }
}