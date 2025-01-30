using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using FivemMapsFixer.Models;

namespace FivemMapsFixer.Controls;

public class IssuePreview:Border
{
    public static readonly StyledProperty<Issue> IssueProperty =
        AvaloniaProperty.Register<IssuePreview, Issue>(nameof(Issue));
    public static readonly StyledProperty<FileType> FileTypeProperty =
        AvaloniaProperty.Register<IssuePreview, FileType>(nameof(FileType));

    public Issue Issue
    {
        get => GetValue(IssueProperty);
        set => SetValue(IssueProperty, value);
    }
    
    public FileType FileType
    {
        get => GetValue(FileTypeProperty);
        set => SetValue(FileTypeProperty, value);
    }

    public IssuePreview()
    {
        Margin = new Thickness(5, 5, 5, 5);
        Padding=new Thickness(2);
        BorderBrush = Brushes.Purple;
        BorderThickness=new Thickness(2);
        CornerRadius=new CornerRadius(4);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment=VerticalAlignment.Stretch;
        PropertyChanged += (_, e) =>
        {
            if(e.Property == FileTypeProperty){SetChild();}
            if(e.Property == IssueProperty){SetChild();}
        };
    }
    
    private void SetChild()
    {
        switch (FileType)
        {
            case FileType.Ymap:
                Child = new YmapPreview();
                Child.Bind(YmapPreview.IssueProperty, new Binding
                {
                    Path = "Issue",
                    Source = this
                });

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    } 
}