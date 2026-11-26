using ChatOps.Model;
using ChatOps.Services;

namespace ChatOps.ViewModel;

public abstract class PageViewModel(MainWindowViewModel shell, SectionId id, string title) : Screen(shell.Api)
{
    protected MainWindowViewModel Shell { get; } = shell;
    public SectionId Id { get; } = id;
    public string Title { get; } = title;
    public Command Refresh => Work(_ => Load(), _ => Api.Session != null);
    public abstract Task Load();
}
public sealed class OverviewViewModel(MainWindowViewModel shell) : PageViewModel(shell, SectionId.Overview,"Tổng quan")
{
    public Overview? Data { get; private set; }
    public Command Open => Work(p => Shell.ShowIncident((Incident)p!), p => p is Incident);
    public Command Filter => new(p => Shell.Filter((string)p!));
    public override async Task Load()
    {
        Data = await Api.Get<Overview>("overview"); Notify(nameof(Data));
        if (Data.Queue.Length == 0) Message = "Không có sự cố đang mở.";
    }
}
public abstract class ListPage<T>(MainWindowViewModel shell, SectionId id, string title) : PageViewModel(shell,id,title)
{
    public T[] Items { get; private set; } = [];
    private T? selected;
    public T? Selected { get => selected; set => Set(ref selected,value); }
    public string Query { get; set; } = "";
    public int Number { get; private set; } = 1;
    public bool HasMore { get; private set; }
    public Command Search => Work(_ => Fetch(1));
    public Command Next => Work(_ => Fetch(Number+1), _ => HasMore);
    public Command Previous => Work(_ => Fetch(Number-1), _ => Number>1);
    public void FirstPage(){Number=1; Notify(nameof(Number));}
    protected abstract string Route { get; }
    public override Task Load() => Fetch(Number);
    public async Task Fetch(int page)
    {
        var data = await Api.Get<Page<T>>($"{Route}{(Route.Contains('?') ? "&" : "?")}q={GuardianApi.Id(Query)}&page={page}");
        Items = data.Items; HasMore = data.HasMore; Number = page; Selected = default;
        Notify(nameof(Items)); Notify(nameof(Number)); Notify(nameof(HasMore));
        Message = Items.Length == 0 ? "Không có kết quả phù hợp." : "";
    }
}
public sealed class DevicesViewModel(MainWindowViewModel shell) : ListPage<Device>(shell,SectionId.Devices,"Thiết bị")
{
    private bool onlineOnly;
    public bool OnlineOnly { get=>onlineOnly; set=>Set(ref onlineOnly,value); }
    protected override string Route => OnlineOnly ? "devices?online=true" : "devices";
    public Command Open => Work(p => Shell.ShowDevice((Device)p!),p => p is Device);
    public Command Edit => Work(async p => {
        var editor = new DeviceEditor(Api, p as Device, false);
        Shell.Dialog(editor); if (editor.Saved) await Fetch(1);
    },p => p is Device d ? d.AllowedActions?.Contains("update")==true : Api.Session?.AllowedActions?.Contains("createDevice")==true);
    public Command EditSelected => Work(async p => {
        var editor=new DeviceEditor(Api,Selected!,false); Shell.Dialog(editor); if(editor.Saved) await Load();
    },p=>Selected?.AllowedActions?.Contains("update")==true);
    public Command Remove => Work(async p => {
        var editor = new DeviceEditor(Api,(Device)p!,true);
        Shell.Dialog(editor); if (editor.Saved) await Fetch(1);
    },p => p is Device d && d.AllowedActions?.Contains("delete")==true);
}
public sealed class IncidentsViewModel(MainWindowViewModel shell) : ListPage<Incident>(shell,SectionId.Incidents,"Sự cố")
{
    public string[] Severities { get; } = ["Tất cả","Cao","Trung bình","Thấp"];
    public string[] States { get; } = ["Tất cả","Chờ phê duyệt","Đang xem xét","Đang xử lý","Đang xác minh","Đã phục hồi"];
    private string state="Tất cả";
    public string State { get=>state; set=>Set(ref state,value); }
    public string Severity { get; set; } = "Tất cả";
    private bool openOnly;
    public bool OpenOnly { get=>openOnly; set=>Set(ref openOnly,value); }
    protected override string Route => $"incidents?openOnly={OpenOnly.ToString().ToLowerInvariant()}&severity={Encode(Severity)}&workflowState={Encode(State)}";
    private static string Encode(string s) => s=="Tất cả" ? "" : GuardianApi.Id(s);
    public Command Open => Work(p => Shell.ShowIncident((Incident)p!),p => p is Incident);
    public Command ReviewApprove => Work(p=>Shell.ShowIncident((Incident)p!),p=>p is Incident i && i.AllowedActions?.Contains("approve")==true);
    public Command ReviewReject => Work(p=>Shell.ShowIncident((Incident)p!),p=>p is Incident i && i.AllowedActions?.Contains("reject")==true);
}
public sealed class ReportsViewModel(MainWindowViewModel shell) : PageViewModel(shell,SectionId.Reports,"Báo cáo")
{
    public DateTime? Week { get; set; } = DateTime.Today;
    public Weekly? Data { get; private set; }
    public override async Task Load()
    {
        Data=null; Notify(nameof(Data));
        var date= Week ?? throw new InvalidOperationException("Hãy chọn tuần.");
        var monday=date.AddDays(-(((int)date.DayOfWeek+6)%7));
        Data=await Api.Get<Weekly>($"reports/weekly?weekStart={monday:yyyy-MM-dd}"); Notify(nameof(Data));
    }
}
public sealed class ReplayViewModel(MainWindowViewModel shell) : PageViewModel(shell,SectionId.Replay,"Replay")
{
    private string incidentId="";
    public string IncidentId { get=>incidentId; set=>Set(ref incidentId,value); }
    public string Pc { get; set; }="";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public TimelineEvent[] Events { get; private set; }=[];
    private TimelineEvent? current;
    public TimelineEvent? Current { get=>current; set {Set(ref current,value);Notify(nameof(Index));} }
    private bool playing;
    private int playVersion;
    public bool Playing { get=>playing; private set {Set(ref playing,value); Notify(nameof(PlayLabel));} }
    public string PlayLabel=>Playing?"Tạm dừng":"Phát";
    public double[] Speeds {get;}=[0.5,1,2,4];
    public double Speed {get;set;}=1;
    public int LastIndex=>Math.Max(0,Events.Length-1);
    public int Index {
        get=>Math.Max(0,Array.IndexOf(Events,Current));
        set { if(value>=0 && value<Events.Length)Current=Events[value]; }
    }
    public Command Step=> new(p=>Index+=int.Parse((string)p!),p=>Current!=null &&
        Index+int.Parse((string)p!)>=0 && Index+int.Parse((string)p!)<Events.Length);
    public Command Play=>new(_=>{if(Playing)Stop();else _=PlayAsync();},_=>Events.Length>1);
    public void Stop(){playVersion++;Playing=false;}
    private async Task PlayAsync()
    {
        int version=++playVersion;Playing=true;
        if(Index==LastIndex) Index=0;
        while(version==playVersion && Index<LastIndex){
            int before=Index;
            var gap=(Events[before+1].OccurredAt-Events[before].OccurredAt).TotalMilliseconds;
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Clamp(gap/Speed,100,60000)));
            if(version!=playVersion)return;
            if(Index==before)Index++;
        }
        if(version==playVersion)Playing=false;
    }
    public override async Task Load()
    {
        Stop();
        Events=[]; Current=null; Notify(nameof(Events));
        if (IncidentId.Trim()=="" && Pc.Trim()=="") {Message="Nhập mã sự cố hoặc tên PC để tìm diễn biến."; return;}
        if (From>To) throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");
        string route=IncidentId.Trim()=="" ? "replay" : $"incidents/{GuardianApi.Id(IncidentId.Trim())}/replay";
        var r=await Api.Get<ReplayResult>($"{route}?pc={GuardianApi.Id(Pc)}&from={Stamp(From)}&to={Stamp(To?.Date.AddDays(1))}");
        Events=r.Events.OrderBy(e=>e.OccurredAt).ToArray(); Notify(nameof(Events)); Notify(nameof(LastIndex)); Current=Events.FirstOrDefault();
        if(Current==null) Message="Không có mốc trong khoảng đã chọn.";
    }
    private static string Stamp(DateTime? d)=>d.HasValue ? GuardianApi.Id(new DateTimeOffset(d.Value.Date).ToString("O")) : "";
}
