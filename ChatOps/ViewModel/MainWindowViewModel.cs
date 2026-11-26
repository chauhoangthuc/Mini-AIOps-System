using ChatOps.Model;
using ChatOps.Services;

namespace ChatOps.ViewModel;

public sealed class MainWindowViewModel : Bindable
{
    public GuardianApi Api { get; private set; } = new();
    public event Action<DialogModel>? ShowDialog;
    public event Action? SignedOut;
    public bool SignedIn=>Api.Session!=null;
    public string Identity=>Api.Session is {} s ? s.DisplayName+" · "+string.Join(", ",s.Roles??[]) : "";
    public string SessionAction=>SignedIn?"Đăng xuất":"Đăng nhập";
    public string Notice=>Api.Configured?"Đăng nhập để xem dữ liệu vận hành.":"Chưa cấu hình kết nối backend. Đặt CHATOPS_API_BASE_URL rồi mở lại ứng dụng.";
    public IReadOnlyList<PageViewModel> Pages { get; private set; }=[];
    private PageViewModel selected=null!;
    public PageViewModel SelectedPage {
        get=>selected;
        set { if(value==null || selected==value)return; if(selected is ReplayViewModel replay)replay.Stop(); Set(ref selected,value); if(SignedIn)_=value.Run(value.Load); }
    }
    public MainWindowViewModel()=>Initialize();
    private void Initialize()
    {
        Api.SessionExpired+=Logout;
        Pages=[new OverviewViewModel(this),new DevicesViewModel(this),new IncidentsViewModel(this),new ReplayViewModel(this),new ReportsViewModel(this)];
        Notify(nameof(Pages)); SelectedPage=Pages[0]; UpdateIdentity();
    }
    private void UpdateIdentity(){Notify(nameof(SignedIn));Notify(nameof(Identity));Notify(nameof(SessionAction));Notify(nameof(Notice));}
    private void Logout()
    {
        if(selected is ReplayViewModel replay)replay.Stop(); SignedOut?.Invoke(); Api.SessionExpired-=Logout; Api.Dispose(); Api=new();Initialize();
    }
    public Command Account=>new(_=>{
        if(SignedIn){Logout();return;}
        Dialog(new LoginModel(Api)); UpdateIdentity(); if(SignedIn)_=selected.Run(selected.Load);
    });
    public void Dialog(DialogModel model)=>ShowDialog?.Invoke(model);
    public async Task ShowDevice(Device row)
    {
        var model=new DeviceModel(this,row); _=model.Run(model.Load);Dialog(model);
        if(SignedIn && SelectedPage is DevicesViewModel)await SelectedPage.Load();
    }
    public async Task ShowIncident(Incident row)
    {
        var model=new IncidentModel(this,row); _=model.Run(model.Load);Dialog(model);
        if(SignedIn && SelectedPage is not ReplayViewModel)await SelectedPage.Load();
    }
    public void Replay(string id)
    {
        var page=Pages.OfType<ReplayViewModel>().Single();
        page.IncidentId=id;page.Pc="";page.From=page.To=null;SelectedPage=page;
    }
    public void Filter(string filter)
    {
        if(filter=="online"){
            var page=Pages.OfType<DevicesViewModel>().Single();page.Query="";page.FirstPage();page.OnlineOnly=true;SelectedPage=page;
        } else {
            var page=Pages.OfType<IncidentsViewModel>().Single();page.FirstPage();page.Query="";page.Severity="Tất cả";page.OpenOnly=filter=="open";
            page.State=filter=="pending"?"Chờ phê duyệt":"Tất cả";SelectedPage=page;
        }
    }
}
