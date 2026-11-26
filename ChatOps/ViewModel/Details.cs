using System.Net;
using System.Net.Http;
using ChatOps.Model;
using ChatOps.Services;

namespace ChatOps.ViewModel;

public abstract class DialogModel(GuardianApi api,string title) : Screen(api)
{
    public string Title { get; } = title;
    public event Action? CloseRequested;
    protected void Close() => CloseRequested?.Invoke();
}
public sealed class LoginModel(GuardianApi api) : DialogModel(api,"Đăng nhập")
{
    public string Username { get; set; }="";
    public Task Submit(string password) => Run(async () => {
        if(string.IsNullOrWhiteSpace(Username) || password.Length==0) throw new ArgumentException("Nhập tên đăng nhập và mật khẩu.");
        await Api.Login(Username.Trim(),password); Close();
    });
}
public sealed class DeviceEditor(GuardianApi api, Device? device, bool deleting) : DialogModel(api,deleting?"Xóa thiết bị":device==null?"Thêm thiết bị":"Sửa thiết bị")
{
    public string Name { get; set; }=deleting?"":device?.PcName??"";
    public string Ip { get; set; }=device?.Ip??"";
    public bool Deleting=>deleting;
    public string Prompt=>deleting?$"Nhập đúng tên {device!.PcName} để xác nhận xóa.":"Tên PC";
    public bool Saved { get; private set; }
    public Command Save => Work(async parameter=>{
        if(deleting && Name!=device!.PcName) throw new ArgumentException("Tên thiết bị chưa khớp.");
        if(!deleting && (string.IsNullOrWhiteSpace(Name)||!IPAddress.TryParse(Ip.Trim(),out _))) throw new ArgumentException("Kiểm tra tên PC và địa chỉ IP.");
        var route=device==null?"devices":"devices/"+GuardianApi.Id(device.DeviceId);
        await Api.Write(deleting?HttpMethod.Delete:device==null?HttpMethod.Post:HttpMethod.Patch,route,deleting?null:new {pcName=Name.Trim(),ip=Ip.Trim()});
        Saved=true; Close();
    });
}
public sealed class DeviceModel(MainWindowViewModel shell,Device device) : DialogModel(shell.Api,"Chi tiết thiết bị")
{
    public DeviceDetail? Data { get; private set; }
    public Command Open => Work(p=>shell.ShowIncident((Incident)p!),p=>p is Incident);
    public Command Refresh=>Work(_=>Load());
    public async Task Load() { Data=await Api.Get<DeviceDetail>("devices/"+GuardianApi.Id(device.DeviceId)); Notify(nameof(Data)); }
}
public sealed class IncidentModel(MainWindowViewModel shell, Incident row) : DialogModel(shell.Api,"Chi tiết sự cố")
{
    public IncidentDetail? Data { get; private set; }
    public ActionRequest? Request { get; private set; }
    private readonly Dictionary<(string,string),string> keys=new();
    public Command Refresh=>Work(_=>Load());
    public Command Replay=>new(_=>{shell.Replay(row.IncidentId);Close();});
    public async Task Load()
    {
        Request=null; Notify(nameof(Request)); Notify(nameof(DecisionHint));
        Data=await Api.Get<IncidentDetail>("incidents/"+GuardianApi.Id(row.IncidentId)); Notify(nameof(Data));
        if(Data.Incident.RequestId is string id)
        {
            var request=await Api.Get<ActionRequest>("requests/"+GuardianApi.Id(id));
            if(request.IncidentId!=row.IncidentId || request.RequestId!=id) throw new FormatException("Yêu cầu không khớp sự cố.");
            Request=request; Notify(nameof(Request)); Notify(nameof(DecisionHint));
        }
    }
    public string DecisionHint => Request switch
    {
        { Status:"pending" } r when r.ExpiresAt<=DateTimeOffset.UtcNow => "Yêu cầu đã hết hạn. Tải lại để xem trạng thái chuyển cấp.",
        { BlockedReason: { Length: >0 } reason } => reason,
        { Status:"pending" } r when r.AllowedActions is not { Length: >0 } => "Phiên hiện tại chỉ được xem yêu cầu.",
        _ => ""
    };
    public void Tick() => Notify(nameof(DecisionHint));
    private bool Allowed(string decision)=>Request is { Status:"pending" } r && r.ExpiresAt>DateTimeOffset.UtcNow && r.AllowedActions?.Contains(decision)==true;
    public Command Decide=>Work(async p=>{
        var decision=(string)p!;
        if(!Allowed(decision)) throw new InvalidOperationException("Hãy tải lại quyền phê duyệt.");
        var id=Request!.RequestId;
        if(!keys.TryGetValue((id,decision),out var key)) keys[(id,decision)]=key=Guid.NewGuid().ToString("N");
        Request=null; Notify(nameof(Request)); Notify(nameof(DecisionHint));
        string? error=null;
        try { await Api.Write(HttpMethod.Post,"requests/"+GuardianApi.Id(id)+"/decision",new {decision},key); }
        catch(Exception e) { error=e.Message; }
        await Load();
        Message=error??"Đã ghi nhận quyết định.";
    },p=>p is string decision && Allowed(decision));
}
