using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models.Ymaps;

namespace FivemMapsFixer.ViewModels.Pages.Ymaps;

public partial class CleanOcclusionsPageViewModel(YmapIssue ymapIssue) : CleanPageViewModel(ymapIssue)
{
    [ObservableProperty] private YmapIssue _issue = ymapIssue;
    public override void Next()
    {
        Issue.FixOcclusionsConflicts();
        Issue.SavePrincipal();
        Globals.InvokeLodLightsPage(this, Issue);
    }
}