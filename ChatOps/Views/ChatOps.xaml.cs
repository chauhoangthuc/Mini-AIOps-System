using System.Windows;
using ChatOps.ViewModel;

namespace ChatOps.Views;
public partial class ChatOpsWindow : Window
{
    private readonly List<DialogHost> dialogs=[];
    public ChatOpsWindow()
    {
        InitializeComponent();
        var shell=new MainWindowViewModel();
        shell.ShowDialog+=model=>{
            var dialog=new DialogHost(model){Owner=dialogs.Count==0?this:dialogs[^1]};
            dialogs.Add(dialog);
            try { dialog.ShowDialog(); } finally { dialogs.Remove(dialog); }
        };
        shell.SignedOut+=()=>{
            foreach(var dialog in dialogs.AsEnumerable().Reverse().ToArray())
                if(dialog.IsVisible)dialog.Finish();
        };
        DataContext=shell;
        Closed+=(_,_)=>shell.Api.Dispose();
        SizeChanged+=(_,args)=>SidebarMenu.IsCompact=args.NewSize.Width<1024;
    }
}
