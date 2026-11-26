using System.Windows;
using ChatOps.ViewModel;

namespace ChatOps.Views;
public partial class DialogHost : Window
{
    public DialogHost(DialogModel model)
    {
        InitializeComponent();
        bool compact=model is LoginModel or DeviceEditor;
        Width=compact?440:Math.Min(1180,SystemParameters.WorkArea.Width);
        Height=compact?390:Math.Min(730,SystemParameters.WorkArea.Height);
        if(compact) Reload.Visibility=Visibility.Collapsed;
        if(model is IncidentModel incident)
        {
            var timer=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromSeconds(1)};
            timer.Tick+=(_,_)=>incident.Tick();
            timer.Start();
            Closed+=(_,_)=>timer.Stop();
        }
        DataContext=model;
        Closing+=(_,args)=>args.Cancel=model.Busy && !allowClose;
        model.CloseRequested+=Finish;
        Closed+=(_,_)=>model.CloseRequested-=Finish;
    }
    private bool allowClose;
    public void Finish(){ allowClose=true;Close(); }
}
