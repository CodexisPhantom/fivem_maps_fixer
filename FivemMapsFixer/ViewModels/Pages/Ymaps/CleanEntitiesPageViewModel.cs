using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models.Ymaps;

namespace FivemMapsFixer.ViewModels.Pages.Ymaps;

public partial class CleanEntitiesPageViewModel(YmapIssue ymapIssue) : CleanPageViewModel(ymapIssue)
{
    [ObservableProperty] private YmapIssue _issue = ymapIssue;
    
    public override void Next()
    {
        Issue.FixEntitiesConflicts();
        Issue.SavePrincipal();
        Globals.InvokeOcclusionsPage(this, Issue);
    }
}