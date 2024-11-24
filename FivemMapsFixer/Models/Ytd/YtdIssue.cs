using CodeWalker.GameFiles;
using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;

namespace FivemMapsFixer.Models.Ytd;

public partial class YtdIssue:Issue
{
    public YtdFile Ytd { get;set; }
    [ObservableProperty] private string[] _names;

    public YtdIssue(YtdFile ytd, FileType type)
    {
        Ytd = ytd;
        Type = type;
    }
    
    public void OpenYtdPage()
    {
        Globals.InvokeYtdPage(this,this);
    }
}