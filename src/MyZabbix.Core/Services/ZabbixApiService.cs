using Microsoft.Extensions.Logging;
using MyZabbix.Core.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyZabbix.Core.Services;

/// <summary>
/// Client for the Zabbix JSON-RPC 2.0 API that wraps authentication and the most common
/// data-retrieval operations (hosts, problems, triggers, items, and dashboard statistics).
/// Should be registered as a scoped service backed by a named <see cref="HttpClient"/> named <c>"Zabbix"</c>.
/// </summary>
public class ZabbixApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<ZabbixApiService> _log;
    private string? _authToken;
    private int _requestId = 1;

    /// <summary>Gets a value indicating whether the service currently holds a valid authentication token.</summary>
    public bool IsAuthenticated => _authToken is not null;

    /// <summary>Gets the base URL of the Zabbix server that was supplied to the most recent <see cref="LoginAsync"/> call.</summary>
    public string? ServerUrl { get; private set; }

    /// <summary>
    /// Initialises a new instance of <see cref="ZabbixApiService"/> with the provided HTTP client and logger.
    /// </summary>
    /// <param name="http">The <see cref="HttpClient"/> used for all JSON-RPC requests. Should be pre-configured with appropriate timeouts.</param>
    /// <param name="log">The logger used to record authentication events, warnings, and errors.</param>
    public ZabbixApiService(HttpClient http, ILogger<ZabbixApiService> log)
    {
        _http = http;
        _log  = log;
    }

    // ── Auth ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Authenticates against the Zabbix server at <paramref name="url"/> and stores the resulting auth token.
    /// Clears any previously stored token before attempting the new login.
    /// </summary>
    /// <param name="url">The base URL of the Zabbix server (e.g. <c>https://zabbix.example.com</c>). Trailing slashes are trimmed automatically.</param>
    /// <param name="user">The Zabbix username to authenticate with.</param>
    /// <param name="password">The password for the specified user account.</param>
    /// <returns><see langword="true"/> if authentication succeeded and an auth token was received; otherwise <see langword="false"/>.</returns>
    public async Task<bool> LoginAsync(string url, string user, string password)
    {
        ServerUrl = url.TrimEnd('/');
        _authToken = null;
        _log.LogInformation("ZabbixApiService: Logging in to {Url} as {User}", ServerUrl, user);

        var response = await SendAsync<string>("user.login", new
        {
            username = user,
            password = password
        }, requireAuth: false);

        if (response is not null)
        {
            _authToken = response;
            _log.LogInformation("ZabbixApiService: Login successful at {Url}", ServerUrl);
            return true;
        }

        _log.LogWarning("ZabbixApiService: Login failed at {Url} (empty token)", ServerUrl);
        return false;
    }

    /// <summary>
    /// Logs out from the Zabbix server and discards the stored authentication token.
    /// Does nothing if the service is not currently authenticated.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (!IsAuthenticated) return;
        _log.LogInformation("ZabbixApiService: Logging out from {Url}", ServerUrl);
        await SendAsync<bool>("user.logout", new { });
        _authToken = null;
    }

    // ── Hosts ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves all hosts from Zabbix, sorted alphabetically by name.
    /// Returns an empty list if the API call returns no results or fails silently.
    /// </summary>
    /// <returns>A list of <see cref="ZabbixHost"/> objects representing all monitored hosts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not authenticated.</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the HTTP request fails.</exception>
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

    /// <summary>
    /// Retrieves recent active problems from Zabbix, optionally filtered to a minimum severity level.
    /// Results are sorted by severity (descending) then by occurrence time (descending), capped at 500 records.
    /// </summary>
    /// <param name="severityMin">
    /// Optional minimum Zabbix severity value (0–5). When specified, only problems at or above this
    /// severity are returned. Pass <see langword="null"/> to return problems of all severities.
    /// </param>
    /// <returns>A list of <see cref="ZabbixProblem"/> objects for the current active problems.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not authenticated.</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the HTTP request fails.</exception>
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

    /// <summary>
    /// Retrieves all triggers that are currently in a problem state, sorted by priority (descending).
    /// When <paramref name="hostId"/> is provided, only triggers belonging to that host are returned.
    /// </summary>
    /// <param name="hostId">
    /// Optional Zabbix host identifier. When supplied, restricts the result to triggers for that specific host.
    /// Pass <see langword="null"/> to retrieve triggers across all hosts.
    /// </param>
    /// <returns>A list of <see cref="ZabbixTrigger"/> objects that are currently firing.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not authenticated.</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the HTTP request fails.</exception>
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

    /// <summary>
    /// Retrieves up to 100 monitored items (metrics) for the specified host, sorted alphabetically by name.
    /// Only items that are actively monitored are included in the result.
    /// </summary>
    /// <param name="hostId">The Zabbix host identifier whose items should be fetched. Must not be null or empty.</param>
    /// <returns>A list of <see cref="ZabbixItem"/> objects representing the host's monitored metrics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not authenticated.</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when the HTTP request fails.</exception>
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

    /// <summary>
    /// Fetches host and problem data concurrently and computes aggregated statistics for the dashboard.
    /// Both API calls are made in parallel to minimise latency.
    /// </summary>
    /// <returns>
    /// A <see cref="ZabbixDashboardStats"/> object containing host availability counts and per-severity problem counts.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not authenticated.</exception>
    /// <exception cref="System.Net.Http.HttpRequestException">Thrown when either underlying HTTP request fails.</exception>
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
            _log.LogWarning("ZabbixApiService: Call to {Method} rejected — not authenticated", method);
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
            _log.LogError(ex, "ZabbixApiService: HTTP error during {Method} at {Url} (status={Status})",
                method, ServerUrl, ex.StatusCode);
            throw;
        }

        var result = await response.Content.ReadFromJsonAsync<JsonRpcResponse<T>>();
        if (result is null)
        {
            _log.LogWarning("ZabbixApiService: Null response for {Method}", method);
            return default;
        }

        if (result.Error is not null)
        {
            _log.LogError("ZabbixApiService: JSON-RPC error during {Method} — code={Code} message={Message} data={Data}",
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
