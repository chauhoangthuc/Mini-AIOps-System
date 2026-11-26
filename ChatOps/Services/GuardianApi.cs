using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ChatOps.Model;

namespace ChatOps.Services;

/// <summary>Adapter for the proposed /api/v1 contract. A new instance owns each sign-in session.</summary>
public sealed class GuardianApi : IDisposable
{
    private readonly HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        { Timeout = TimeSpan.FromSeconds(20) };
    private readonly CancellationTokenSource lifetime = new();
    public event Action? SessionExpired;
    public Session? Session { get; private set; }
    public bool Configured => client.BaseAddress != null;
    public GuardianApi()
    {
        var url = Environment.GetEnvironmentVariable("CHATOPS_API_BASE_URL");
        if (Uri.TryCreate(url?.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            && (uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback))
            client.BaseAddress = uri;
    }
    public static string Id(string id) => Uri.EscapeDataString(id);
    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null, string? key = null)
    {
        if (!Configured) throw new InvalidOperationException("Chưa cấu hình CHATOPS_API_BASE_URL.");
        using var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        if (key != null) request.Headers.Add("Idempotency-Key", key);
        var response = await client.SendAsync(request, lifetime.Token);
        if (response.IsSuccessStatusCode) return response;
        var status = response.StatusCode;
        response.Dispose();
        if (status == HttpStatusCode.Unauthorized && Session != null) SessionExpired?.Invoke();
        throw new HttpRequestException(status switch {
            HttpStatusCode.Unauthorized => "Thông tin đăng nhập không hợp lệ hoặc phiên đã hết hạn.",
            HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
            HttpStatusCode.Conflict => "Dữ liệu đã được người khác cập nhật.",
            HttpStatusCode.Gone => "Yêu cầu hoặc bằng chứng đã hết hạn.",
            HttpStatusCode.NotFound => "Không tìm thấy dữ liệu.",
            _ => $"Máy chủ trả về HTTP {(int)status}."
        }, null, status);
    }
    public async Task<T> Get<T>(string path)
    {
        using var r = await Send(HttpMethod.Get, path);
        return await r.Content.ReadFromJsonAsync<T>() ?? throw new FormatException("Phản hồi rỗng.");
    }
    public async Task Login(string username, string password)
    {
        using var r = await Send(HttpMethod.Post, "sessions", new { username, password });
        var session = await r.Content.ReadFromJsonAsync<Session>();
        if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
            throw new FormatException("Phản hồi đăng nhập thiếu accessToken.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        Session = session;
    }
    public async Task Write(HttpMethod method, string path, object? body = null, string? key = null)
    {
        using var response = await Send(method, path, body, key);
    }
    public void Dispose() { lifetime.Cancel(); client.Dispose(); lifetime.Dispose(); Session = null; }
}
