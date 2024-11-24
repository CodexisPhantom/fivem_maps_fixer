using System;
using System.IO;
using Avalonia.Media.Imaging;
using CodeWalker.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using FivemMapsFixer.Events;
using FivemMapsFixer.Models.Ytd;
using ImageMagick;

namespace FivemMapsFixer.ViewModels.Pages.Ytd;

public partial class CleanYtdPageViewModel:CleanPageViewModel
{
    [ObservableProperty] private YtdIssue _issue;
    [ObservableProperty] private int _selectedIndex;
    private readonly Bitmap[] _images;
    [ObservableProperty] private Bitmap _selectedImage;
    [ObservableProperty] private string _selectedSize;
    
    public CleanYtdPageViewModel(YtdIssue issue):base(issue)
    {
        Issue = issue;
        _images = new Bitmap[Issue.Ytd.TextureDict.Textures.Count];
        for (int i = 0; i < Issue.Ytd.TextureDict.Textures.Count; i++)
        {
            byte[] bytes = DDSIO.GetDDSFile(Issue.Ytd.TextureDict.Textures[i]);
            MagickImage image = new(bytes);
            image.Format = MagickFormat.Jpeg;
            MemoryStream stream = new(image.ToByteArray());
            Bitmap map = new(stream);
            _images[i] = map;
        }
        SelectedIndex = 0;
        SelectedImage = _images[0];
        SelectedSize = $"{SelectedImage.PixelSize.Width}x{SelectedImage.PixelSize.Height}";
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedIndex))
            {
                SelectedImage = _images[SelectedIndex];
                SelectedSize = $"{SelectedImage.PixelSize.Width}x{SelectedImage.PixelSize.Height}";
            }
        };
    }

    public override void Next()
    {
        Issue.SavePrincipal();
        Globals.InvokeMainPage(this);
    }
}