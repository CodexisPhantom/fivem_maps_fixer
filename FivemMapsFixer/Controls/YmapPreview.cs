using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using FivemMapsFixer.Models.Ymaps;

namespace FivemMapsFixer.Controls;

public class YmapPreview:Grid
{
    public static readonly StyledProperty<YmapIssue> IssueProperty =
        AvaloniaProperty.Register<YmapPreview, YmapIssue>(nameof(Issue));
    
    public YmapIssue Issue
    {
        get => GetValue(IssueProperty);
        set => SetValue(IssueProperty, value);
    }
    
    private readonly ListBox _listBox = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        SelectionMode = SelectionMode.Single,
    };
    
    private readonly Button _button = new()
    {
        Foreground = Brushes.Red,
        Background = Brushes.Chartreuse,
        Content = "Fix Issues",
    };
    
    public YmapPreview()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Margin = new Thickness(5);
        ColumnDefinitions = new ColumnDefinitions("*,10,Auto");
        
        Children.Add(_listBox);
        Children.Add(_button);
        
        SetColumn(_listBox, 0);
        SetColumn(_button, 2);
        
        PropertyChanged += (_, e) =>
        {
            if(Issue == null){return;}
            if(e.Property != IssueProperty){return;}
            _listBox.Bind(ItemsControl.ItemsSourceProperty, new Binding
            {
                Path = "Issue.YmapFilesPath",
                Source = this
            });
            _listBox.Bind(SelectingItemsControl.SelectedIndexProperty, new Binding
            {
                Path = "Issue.SelectedIndex",
                Source = this
            });
            _button.Bind(Button.CommandProperty, new Binding
            {
                Path = "Issue.OpenEntitiesPage",
                Source = this
            });
        };
    }
}