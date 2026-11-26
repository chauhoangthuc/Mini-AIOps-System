using System.Windows.Controls;
namespace ChatOps.Views;
public partial class IncidentView : UserControl
{
    public IncidentView() => InitializeComponent();
    private void JumpToSection(object sender,System.Windows.RoutedEventArgs args)
    {
        if(sender is Button {Tag:string name} && FindName(name) is System.Windows.FrameworkElement section)
            section.BringIntoView();
    }
}
