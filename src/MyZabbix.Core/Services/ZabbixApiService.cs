using Microsoft.Extensions.Logging;
using MyZabbix.Core.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyZabbix.Core.Services;

/// <summary>
/// Klient pro Zabbix JSON-RPC 2.0 API.
/// Registrovat jako Scoped + pojmenovaný HttpClient:
///   builder.Services.AddHttpClient("Zabbix");
///   builder.Services.AddScoped(sp => new ZabbixApiService(
///       sp.GetRequiredService&lt;IHttpClientFactory&gt;().CreateClient("Zabbix"),
///       sp.GetRequiredService&lt;ILogger&lt;ZabbixApiService&gt;&gt;()));
/// </summary>
public class ZabbixApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ZabbixApiService> _log;
    private string? _authToken;
    private int _requestId = 1;

    public bool IsAuthenticated => _authToken is not null;
    public string? ServerUrl { get; private set; }

    public ZabbixApiService(HttpClient http, ILogger<ZabbixApiService> log)
    {
        _http = http;
        _log  = log;
    }

    // ── Auth ──────────────────────────────────────────────────────────────

    public async Task<bool> LoginAsync(string url, string user, string password)
    {
        ServerUrl = url.TrimEnd('/');
        _authToken = null;
        _log.LogInformation("ZabbixApiService: Přihlašování na {Url} jako {User}", ServerUrl, user);

        var response = await SendAsync<string>("user.login", new
        {
            username = user,
            password = password
        }, requireAuth: false);

        if (response is not null)
        {
            _authToken = response;
            _log.LogInformation("ZabbixApiService: Přihlášení úspěšné na {Url}", ServerUrl);
            return true;
        }

        _log.LogWarning("ZabbixApiService: Přihlášení selhalo na {Url} (prázdný token)", ServerUrl);
        return false;
    }

    public async Task LogoutAsync()
    {
        if (!IsAuthenticated) return;
        _log.LogInformation("ZabbixApiService: Odhlašování ze {Url}", ServerUrl);
        await SendAsync<bool>("user.logout", new { });
        _authToken = null;
    }

    // ── Hosts ─────────────────────────────────────────────────────────────

    public async Task<List<ZabbixHost>> GetHostsAsync()
    {
        return await SendAsync<List<ZabbixHost>>("host.get", new
        {
            output = new[] { "hostid", "host", "name", "status", "available" },
            sortfield = "name",
            sortorder = "ASC"
        }) ?? [];
    }

    // ── Problems ──────────────────────────────────────────────────────────

    public async Task<List<ZabbixProblem>> GetProblemsAsync(int? severityMin = null)
    {
        var @params = new Dictionary<string, object>
        {
            ["output"]        = "extend",
            ["selectHosts"]   = new[] { "hostid", "name" },
            ["recent"]        = true,
            ["sortfield"]     = new[] { "severity", "clock" },
            ["sortorder"]     = new[] { "DESC", "DESC" },
            ["limit"]         = 500
        };

        if (severityMin.HasValue)
            @params["severities"] = Enumerable.Range(severityMin.Value, 6 - severityMin.Value).ToArray();

        return await SendAsync<List<ZabbixProblem>>("problem.get", @params) ?? [];
    }

    // ── Triggers ──────────────────────────────────────────────────────────

    public async Task<List<ZabbixTrigger>> GetTriggersAsync(string? hostId = null)
    {
        var @params = new Dictionary<string, object>
        {
            ["output"]       = new[] { "triggerid", "description", "priority", "status", "value", "lastchange" },
            ["selectHosts"]  = new[] { "hostid", "name" },
            ["only_true"]    = true,
            ["filter"]       = new { value = "1" },
            ["sortfield"]    = "priority",
            ["sortorder"]    = "DESC"
        };

        if (hostId is not null)
            @params["hostids"] = hostId;

        return await SendAsync<List<ZabbixTrigger>>("trigger.get", @params) ?? [];
    }

    // ── Items / Metrics ───────────────────────────────────────────────────

    public async Task<List<ZabbixItem>> GetItemsAsync(string hostId)
    {
        return await SendAsync<List<ZabbixItem>>("item.get", new
        {
            output       = new[] { "itemid", "name", "key_", "lastvalue", "units", "lastclock" },
            hostids      = hostId,
            monitored    = true,
            sortfield    = "name",
            sortorder    = "ASC",
            limit        = 100
        }) ?? [];
    }

    // ── Dashboard stats ───────────────────────────────────────────────────

    public async Task<ZabbixDashboardStats> GetDashboardStatsAsync()
    {
        var hostsTask    = GetHostsAsync();
        var problemsTask = GetProblemsAsync();
        await Task.WhenAll(hostsTask, problemsTask);

        var hosts    = hostsTask.Result;
        var problems = problemsTask.Result;

        return new ZabbixDashboardStats
        {
            TotalHosts       = hosts.Count,
            AvailableHosts   = hosts.Count(h => h.Available == "1"),
            UnavailableHosts = hosts.Count(h => h.Available == "2"),
            TotalProblems    = problems.Count,
            DisasterCount    = problems.Count(p => p.Severity == "5"),
            HighCount        = problems.Count(p => p.Severity == "4"),
            AverageCount     = problems.Count(p => p.Severity == "3"),
            WarningCount     = problems.Count(p => p.Severity == "2"),
            InfoCount        = problems.Count(p => p.Severity == "1"),
        };
    }

    // ── JSON-RPC core ─────────────────────────────────────────────────────

    private async Task<T?> SendAsync<T>(string method, object @params, bool requireAuth = true)
    {
        if (requireAuth && !IsAuthenticated)
        {
            _log.LogWarning("ZabbixApiService: Volání {Method} bez autentizace zamítnuto", method);
            throw new InvalidOperationException("Not authenticated. Call LoginAsync first.");
        }

        var payload = new JsonRpcRequest
        {
            Method = method,
            Params = @params,
            Auth   = requireAuth ? _authToken : null,
            Id     = _requestId++
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync(
                $"{ServerUrl}/api_jsonrpc.php", payload,
                new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _log.LogError(ex, "ZabbixApiService: HTTP chyba při {Method} na {Url} (status={Status})",
                method, ServerUrl, ex.StatusCode);
            throw;
        }

        var result = await response.Content.ReadFromJsonAsync<JsonRpcResponse<T>>();
        if (result is null)
        {
            _log.LogWarning("ZabbixApiService: Null odpověď při {Method}", method);
            return default;
        }

        if (result.Error is not null)
        {
            _log.LogError("ZabbixApiService: JSON-RPC error při {Method} — code={Code} message={Message} data={Data}",
                method, result.Error.Code, result.Error.Message, result.Error.Data);
            throw new InvalidOperationException(
                $"Zabbix API error [{result.Error.Code}]: {result.Error.Message} — {result.Error.Data}");
        }

        return result.Result;
    }

    // ── DTO ───────────────────────────────────────────────────────────────

    private class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("method")]  public string Method  { get; set; } = "";
        [JsonPropertyName("params")]  public object Params  { get; set; } = new { };
        [JsonPropertyName("auth")]    public string? Auth   { get; set; }
        [JsonPropertyName("id")]      public int Id         { get; set; }
    }

    private class JsonRpcResponse<T>
    {
        [JsonPropertyName("result")] public T Result { get; set; } = default!;
        [JsonPropertyName("error")]  public JsonRpcError? Error { get; set; }
    }

    private class JsonRpcError
    {
        [JsonPropertyName("code")]    public int    Code    { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("data")]    public string? Data  { get; set; }
    }
}
