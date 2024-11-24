using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.ViewModels.Pages;

public abstract partial class CleanPageViewModel(Issue Issue) : PageViewModel
{
    [ObservableProperty] private Issue _issue = Issue;

    public abstract void Next();
}