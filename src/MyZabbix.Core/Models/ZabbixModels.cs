using System.Text.Json.Serialization;

namespace MyZabbix.Core.Models;

// ── Hosts ──────────────────────────────────────────────────────────────────

public class ZabbixHost
{
    [JsonPropertyName("hostid")]   public string HostId    { get; set; } = "";
    [JsonPropertyName("host")]     public string Host      { get; set; } = "";
    [JsonPropertyName("name")]     public string Name      { get; set; } = "";
    [JsonPropertyName("status")]   public string Status    { get; set; } = "0";   // 0=enabled, 1=disabled
    [JsonPropertyName("available")]public string Available { get; set; } = "0";   // 0=unknown,1=available,2=unavailable

    [JsonIgnore] public bool IsEnabled    => Status    == "0";
    [JsonIgnore] public bool IsAvailable  => Available == "1";
    [JsonIgnore] public bool IsDown       => Available == "2";
    [JsonIgnore] public string StatusBadge => Available switch
    {
        "1" => "success",
        "2" => "danger",
        _   => "secondary"
    };
    [JsonIgnore] public string StatusLabel => Available switch
    {
        "1" => "Available",
        "2" => "Unavailable",
        _   => "Unknown"
    };
}

// ── Problems / Events ─────────────────────────────────────────────────────

public class ZabbixProblem
{
    [JsonPropertyName("eventid")]   public string EventId   { get; set; } = "";
    [JsonPropertyName("name")]      public string Name      { get; set; } = "";
    [JsonPropertyName("severity")]  public string Severity  { get; set; } = "0";
    [JsonPropertyName("clock")]     public string Clock     { get; set; } = "0";
    [JsonPropertyName("acknowledged")] public string Acknowledged { get; set; } = "0";
    [JsonPropertyName("hosts")]     public List<ZabbixHostRef> Hosts { get; set; } = [];

    [JsonIgnore] public DateTime OccurredAt => DateTimeOffset
        .FromUnixTimeSeconds(long.TryParse(Clock, out var t) ? t : 0).LocalDateTime;

    [JsonIgnore] public bool IsAcknowledged => Acknowledged == "1";

    [JsonIgnore] public string SeverityLabel => int.TryParse(Severity, out var s) ? s switch
    {
        0 => "Not classified",
        1 => "Information",
        2 => "Warning",
        3 => "Average",
        4 => "High",
        5 => "Disaster",
        _ => "Unknown"
    } : "Unknown";

    [JsonIgnore] public string SeverityBadge => int.TryParse(Severity, out var s) ? s switch
    {
        0 => "secondary",
        1 => "info",
        2 => "warning",
        3 => "warning",
        4 => "danger",
        5 => "danger",
        _ => "secondary"
    } : "secondary";

    [JsonIgnore] public string HostName => Hosts.FirstOrDefault()?.Name ?? "";
}

public class ZabbixHostRef
{
    [JsonPropertyName("hostid")] public string HostId { get; set; } = "";
    [JsonPropertyName("name")]   public string Name   { get; set; } = "";
}

// ── Triggers ──────────────────────────────────────────────────────────────

public class ZabbixTrigger
{
    [JsonPropertyName("triggerid")]    public string TriggerId    { get; set; } = "";
    [JsonPropertyName("description")]  public string Description  { get; set; } = "";
    [JsonPropertyName("priority")]     public string Priority     { get; set; } = "0";
    [JsonPropertyName("status")]       public string Status       { get; set; } = "0";
    [JsonPropertyName("value")]        public string Value        { get; set; } = "0";  // 0=OK, 1=PROBLEM
    [JsonPropertyName("lastchange")]   public string LastChange   { get; set; } = "0";
    [JsonPropertyName("hosts")]        public List<ZabbixHostRef> Hosts { get; set; } = [];

    [JsonIgnore] public bool IsProblem => Value == "1";
    [JsonIgnore] public string HostName => Hosts.FirstOrDefault()?.Name ?? "";
    [JsonIgnore] public DateTime LastChangedAt => DateTimeOffset
        .FromUnixTimeSeconds(long.TryParse(LastChange, out var t) ? t : 0).LocalDateTime;
}

// ── Items / Metrics ───────────────────────────────────────────────────────

public class ZabbixItem
{
    [JsonPropertyName("itemid")]    public string ItemId    { get; set; } = "";
    [JsonPropertyName("name")]      public string Name      { get; set; } = "";
    [JsonPropertyName("key_")]      public string Key       { get; set; } = "";
    [JsonPropertyName("lastvalue")] public string LastValue { get; set; } = "";
    [JsonPropertyName("units")]     public string Units     { get; set; } = "";
    [JsonPropertyName("lastclock")] public string LastClock { get; set; } = "0";

    [JsonIgnore] public string DisplayValue => string.IsNullOrEmpty(Units)
        ? LastValue
        : $"{LastValue} {Units}";

    [JsonIgnore] public DateTime LastUpdatedAt => DateTimeOffset
        .FromUnixTimeSeconds(long.TryParse(LastClock, out var t) ? t : 0).LocalDateTime;
}

// ── Dashboard stats ───────────────────────────────────────────────────────

public class ZabbixDashboardStats
{
    public int TotalHosts      { get; set; }
    public int AvailableHosts  { get; set; }
    public int UnavailableHosts{ get; set; }
    public int TotalProblems   { get; set; }
    public int DisasterCount   { get; set; }
    public int HighCount       { get; set; }
    public int AverageCount    { get; set; }
    public int WarningCount    { get; set; }
    public int InfoCount       { get; set; }
}
