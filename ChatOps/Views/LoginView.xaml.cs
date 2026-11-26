using System.Windows.Controls;
namespace ChatOps.Views;
public partial class LoginView : UserControl
{
    public LoginView() => InitializeComponent();
    private async void SignIn(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModel.LoginModel model && !model.Busy)
        {
            var password=Secret.Password;
            Secret.Clear();
            await model.Submit(password);
        }
    }
}
