using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models;
using FivemMapsFixer.Models.Ymaps;

namespace FivemMapsFixer.ViewModels.Pages.Ymaps;

public partial class CleanLodLightsPageViewModel(YmapIssue ymapIssue) : CleanPageViewModel(ymapIssue)
{
    [ObservableProperty] private YmapIssue _issue = ymapIssue;
    
    public override void Next()
    {
        Issue.FixLodLightsConflicts();
        Issue.SavePrincipal();
        Globals.InvokeFixPage(this, FileType.Ymap);
    }
}