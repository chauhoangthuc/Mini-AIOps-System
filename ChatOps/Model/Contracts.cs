using System.Text.Json.Serialization;

namespace ChatOps.Model;

public sealed record Session(string AccessToken, string DisplayName, string[] Roles, string[] AllowedActions);
public sealed record Page<T>(T[] Items, bool HasMore);
public sealed record PointSample(DateTimeOffset At, double? Value);
public sealed record MetricSet(double? CpuPct, double? RamPct, double? DiskPct);
public sealed record Device(string DeviceId, string PcName, string Ip, bool Online,
    double? CpuPct, double? RamPct, DateTimeOffset? SampledAt, string? IncidentId, string[] AllowedActions)
{
    [JsonIgnore] public string Connection => Online ? "Online" : "Offline";
    [JsonIgnore] public string Freshness => Online ? "" : "Dữ liệu cũ";
}
public sealed record DeviceDetail(Device Device, DateTimeOffset? LastSeenAt, double? DiskPct,
    string AppHealth, PointSample[] CpuTrend, PointSample[] RamTrend, PointSample[] DiskTrend, Incident[] Incidents);
public sealed record Incident(string IncidentId, string Title, string DeviceId, string PcName,
    string Ip, string Severity, string WorkflowState, double? CpuPct, double? RamPct,
    DateTimeOffset DetectedAt, DateTimeOffset? ExpiresAt, string? RequestId, string[] AllowedActions);
public sealed record Overview(int OpenCount, int PendingRequestCount, int PendingIncidentCount, int OnlineCount, int DeviceCount,
    PointSample[] Trend, Incident[] Queue);
public sealed record Evidence(string EvidenceId, string Name, string? RedactedPreview, bool Expired)
{
    [JsonIgnore] public string Preview => Expired ? "Bằng chứng đã hết hạn." : RedactedPreview ?? "Chưa có bản xem trước.";
}
public sealed record TimelineEvent(DateTimeOffset OccurredAt, string Title, string? Description,
    MetricSet? Metrics, DateTimeOffset? SnapshotAt, Evidence[] EvidenceRefs, string? ScreenshotBase64);
public sealed record IncidentDetail(Incident Incident, TimelineEvent[] Events, Evidence[] EvidenceRefs,
    string? Diagnosis, string? ProposedAction, string? TaskStatus, string? ProbeResult);
public sealed record ActionRequest(string RequestId, string IncidentId, string Target, string Service,
    string Action, string Proposer, DateTimeOffset ExpiresAt, string Status, string[] AllowedActions, string? BlockedReason);
public sealed record ReplayResult(string IncidentId, string DeviceId, TimelineEvent[] Events);
public sealed record Weekly(DateTime WeekStart, DateTime WeekEnd, string Scope, string Audience, string Status,
    MetricSet? Baseline, MetricSet? Average, PointSample[] CpuTrend, PointSample[] RamTrend,
    string? DiskForecast, PointSample[] DiskTrend, DateTimeOffset? ForecastAt, DateTimeOffset? ReportAt,
    string[] Summary, string[] Insights);
