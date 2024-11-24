using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;

namespace FivemMapsFixer.ViewModels.Pages;

public class PageViewModel:ObservableObject
{
    public void ChangeToMainPage()
    {
        Globals.InvokeMainPage(this);
    }
}