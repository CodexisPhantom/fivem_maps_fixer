using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using FivemMapsFixer.Models.Ytd;

namespace FivemMapsFixer.Controls;

public class YtdPreview:Grid
{
    public static readonly StyledProperty<YtdIssue> IssueProperty =
        AvaloniaProperty.Register<YtdPreview, YtdIssue>(nameof(Issue));
    
    public YtdIssue Issue
    {
        get => GetValue(IssueProperty);
        set => SetValue(IssueProperty, value);
    }
    
    private readonly TextBlock _textBlockName = new()
    {
        Foreground = Brushes.GreenYellow,
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        TextAlignment = TextAlignment.Center
    };
    
    private readonly TextBlock _textBlockSize = new()
    {
        Foreground = Brushes.Red,
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        TextAlignment = TextAlignment.Center
    };
    
    private readonly Button _button = new()
    {
        Foreground =  Brushes.Chartreuse,
        Content = "Fix"
    };
    
    public YtdPreview()
    {
        ColumnDefinitions = new ColumnDefinitions("*,10,*,10,*");
        Margin = new Thickness(5);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        
        SetColumn(_textBlockName, 0);
        SetColumn(_textBlockSize, 2);
        SetColumn(_button, 4);
        Children.Add(_textBlockSize);
        Children.Add(_textBlockName);
        Children.Add(_button);
        
        PropertyChanged += (_, e) =>
        {
            _button.Bind(Button.CommandProperty, new Binding
            {
                Path = "Issue.OpenYtdPage",
                Source = this
            });
            
            if (e.Property != IssueProperty) return;
            if(Issue == null){return;}
            _textBlockSize.Text = Issue.Ytd.TextureDict.MemoryUsage+" MB";
            _textBlockName.Text = Issue.Ytd.Name;
        };
    }
}