using System.Text.Json.Serialization;

namespace MyZabbix.Core.Models;

// ── Hosts ──────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a Zabbix monitored host as returned by the <c>host.get</c> API method.
/// Provides computed properties for availability status and Bootstrap badge styles.
/// </summary>
public class ZabbixHost
{
    /// <summary>Gets or sets the unique host identifier assigned by Zabbix.</summary>
    [JsonPropertyName("hostid")]   public string HostId    { get; set; } = "";

    /// <summary>Gets or sets the technical host name used internally in Zabbix.</summary>
    [JsonPropertyName("host")]     public string Host      { get; set; } = "";

    /// <summary>Gets or sets the visible/display name of the host.</summary>
    [JsonPropertyName("name")]     public string Name      { get; set; } = "";

    /// <summary>Gets or sets the host status flag. <c>"0"</c> means enabled; <c>"1"</c> means disabled.</summary>
    [JsonPropertyName("status")]   public string Status    { get; set; } = "0";

    /// <summary>Gets or sets the host availability flag. <c>"0"</c> = unknown, <c>"1"</c> = available, <c>"2"</c> = unavailable.</summary>
    [JsonPropertyName("available")]public string Available { get; set; } = "0";

    /// <summary>Gets a value indicating whether the host is enabled (Status == "0").</summary>
    [JsonIgnore] public bool IsEnabled    => Status    == "0";

    /// <summary>Gets a value indicating whether the host is currently reachable by Zabbix.</summary>
    [JsonIgnore] public bool IsAvailable  => Available == "1";

    /// <summary>Gets a value indicating whether the host is currently reported as unavailable.</summary>
    [JsonIgnore] public bool IsDown       => Available == "2";

    /// <summary>Gets the Bootstrap contextual colour name appropriate for this host's availability state.</summary>
    [JsonIgnore] public string StatusBadge => Available switch
    {
        "1" => "success",
        "2" => "danger",
        _   => "secondary"
    };

    /// <summary>Gets a human-readable availability label for display in the UI.</summary>
    [JsonIgnore] public string StatusLabel => Available switch
    {
        "1" => "Available",
        "2" => "Unavailable",
        _   => "Unknown"
    };
}

// ── Problems / Events ─────────────────────────────────────────────────────

/// <summary>
/// Represents an active problem (event) as returned by the Zabbix <c>problem.get</c> API method.
/// Exposes computed properties for severity labels, badge colours, and the time the problem occurred.
/// </summary>
public class ZabbixProblem
{
    /// <summary>Gets or sets the unique event identifier for this problem.</summary>
    [JsonPropertyName("eventid")]   public string EventId   { get; set; } = "";

    /// <summary>Gets or sets the human-readable problem name or description.</summary>
    [JsonPropertyName("name")]      public string Name      { get; set; } = "";

    /// <summary>
    /// Gets or sets the numeric severity level as a string.
    /// Values range from <c>"0"</c> (Not classified) to <c>"5"</c> (Disaster).
    /// </summary>
    [JsonPropertyName("severity")]  public string Severity  { get; set; } = "0";

    /// <summary>Gets or sets the Unix timestamp (seconds) at which the problem was detected.</summary>
    [JsonPropertyName("clock")]     public string Clock     { get; set; } = "0";

    /// <summary>Gets or sets whether the problem has been acknowledged. <c>"1"</c> means acknowledged.</summary>
    [JsonPropertyName("acknowledged")] public string Acknowledged { get; set; } = "0";

    /// <summary>Gets or sets the list of hosts associated with this problem.</summary>
    [JsonPropertyName("hosts")]     public List<ZabbixHostRef> Hosts { get; set; } = [];

    /// <summary>Gets the local date and time at which the problem occurred, converted from the Unix timestamp.</summary>
    [JsonIgnore] public DateTime OccurredAt => DateTimeOffset
        .FromUnixTimeSeconds(long.TryParse(Clock, out var t) ? t : 0).LocalDateTime;

    /// <summary>Gets a value indicating whether this problem has been acknowledged by an operator.</summary>
    [JsonIgnore] public bool IsAcknowledged => Acknowledged == "1";

    /// <summary>Gets a human-readable severity label (e.g. "Warning", "Disaster") for display purposes.</summary>
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

    /// <summary>Gets the Bootstrap contextual colour name that corresponds to this problem's severity level.</summary>
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

    /// <summary>Gets the display name of the first host associated with this problem, or an empty string if none.</summary>
    [JsonIgnore] public string HostName => Hosts.FirstOrDefault()?.Name ?? "";
}

/// <summary>
/// A lightweight host reference embedded inside problems and triggers returned by the Zabbix API.
/// Contains only the host identifier and display name.
/// </summary>
public class ZabbixHostRef
{
    /// <summary>Gets or sets the unique Zabbix host identifier.</summary>
    [JsonPropertyName("hostid")] public string HostId { get; set; } = "";

    /// <summary>Gets or sets the display name of the referenced host.</summary>
    [JsonPropertyName("name")]   public string Name   { get; set; } = "";
}

// ── Triggers ──────────────────────────────────────────────────────────────

/// <summary>
/// Represents a Zabbix trigger as returned by the <c>trigger.get</c> API method.
/// Provides computed properties that indicate whether the trigger is currently in a problem state.
/// </summary>
public class ZabbixTrigger
{
    /// <summary>Gets or sets the unique trigger identifier.</summary>
    [JsonPropertyName("triggerid")]    public string TriggerId    { get; set; } = "";

    /// <summary>Gets or sets the human-readable description of the trigger.</summary>
    [JsonPropertyName("description")]  public string Description  { get; set; } = "";

    /// <summary>
    /// Gets or sets the trigger priority (severity) as a numeric string.
    /// Values range from <c>"0"</c> (Not classified) to <c>"5"</c> (Disaster).
    /// </summary>
    [JsonPropertyName("priority")]     public string Priority     { get; set; } = "0";

    /// <summary>Gets or sets the trigger status. <c>"0"</c> means enabled; <c>"1"</c> means disabled.</summary>
    [JsonPropertyName("status")]       public string Status       { get; set; } = "0";

    /// <summary>Gets or sets the current trigger value. <c>"0"</c> = OK, <c>"1"</c> = PROBLEM.</summary>
    [JsonPropertyName("value")]        public string Value        { get; set; } = "0";

    /// <summary>Gets or sets the Unix timestamp (seconds) of the most recent state change.</summary>
    [JsonPropertyName("lastchange")]   public string LastChange   { get; set; } = "0";

    /// <summary>Gets or sets the list of hosts to which this trigger belongs.</summary>
    [JsonPropertyName("hosts")]        public List<ZabbixHostRef> Hosts { get; set; } = [];

    /// <summary>Gets a value indicating whether the trigger is currently firing a problem.</summary>
    [JsonIgnore] public bool IsProblem => Value == "1";

    /// <summary>Gets the display name of the first host linked to this trigger, or an empty string if none.</summary>
    [JsonIgnore] public string HostName => Hosts.FirstOrDefault()?.Name ?? "";

    /// <summary>Gets the local date and time of the last state change, converted from the Unix timestamp.</summary>
    [JsonIgnore] public DateTime LastChangedAt => DateTimeOffset
        .FromUnixTimeSeconds(long.TryParse(LastChange, out var t) ? t : 0).LocalDateTime;
}

// ── Items / Metrics ───────────────────────────────────────────────────────

/// <summary>
/// Represents a Zabbix monitored item (metric) as returned by the <c>item.get</c> API method.
/// Provides a formatted display value that combines the last collected value with its unit of measurement.
/// </summary>
public class ZabbixItem
{
    /// <summary>Gets or sets the unique item identifier.</summary>
    [JsonPropertyName("itemid")]    public string ItemId    { get; set; } = "";

    /// <summary>Gets or sets the human-readable item name.</summary>
    [JsonPropertyName("name")]      public string Name      { get; set; } = "";

    /// <summary>Gets or sets the item key used to identify the metric type in Zabbix.</summary>
    [JsonPropertyName("key_")]      public string Key       { get; set; } = "";

    /// <summary>Gets or sets the most recently collected value for this item.</summary>
    [JsonPropertyName("lastvalue")] public string LastValue { get; set; } = "";

    /// <summary>Gets or sets the unit of measurement for this item's values (e.g. "MB", "%").</summary>
    [JsonPropertyName("units")]     public string Units     { get; set; } = "";

    /// <summary>Gets or sets the Unix timestamp (seconds) of the last collected value.</summary>
    [JsonPropertyName("lastclock")] public string LastClock { get; set; } = "0";

    /// <summary>
    /// Gets the last value formatted for display.
    /// Appends the unit of measurement when one is defined; otherwise returns the raw value string.
    /// </summary>
    [JsonIgnore] public string DisplayValue => string.IsNullOrEmpty(Units)
        ? LastValue
        : $"{LastValue} {Units}";

    /// <summary>Gets the local date and time at which the last value was collected, converted from the Unix timestamp.</summary>
    [JsonIgnore] public DateTime LastUpdatedAt => DateTimeOffset
        .FromUnixTimeSeconds(long.TryParse(LastClock, out var t) ? t : 0).LocalDateTime;
}

// ── Host Groups ───────────────────────────────────────────────────────────

/// <summary>Represents a Zabbix host group as returned by the <c>hostgroup.get</c> API method.</summary>
public class ZabbixHostGroup
{
    [JsonPropertyName("groupid")]  public string GroupId    { get; set; } = "";
    [JsonPropertyName("name")]     public string Name       { get; set; } = "";
    [JsonPropertyName("hosts")]    public List<ZabbixHostRef> Hosts { get; set; } = [];
    [JsonIgnore] public int HostCount => Hosts.Count;
}

// ── Events / Alerts ───────────────────────────────────────────────────────

/// <summary>Represents a resolved or historical Zabbix event as returned by <c>event.get</c>.</summary>
public class ZabbixEvent
{
    [JsonPropertyName("eventid")]     public string EventId     { get; set; } = "";
    [JsonPropertyName("name")]        public string Name        { get; set; } = "";
    [JsonPropertyName("severity")]    public string Severity    { get; set; } = "0";
    [JsonPropertyName("clock")]       public string Clock       { get; set; } = "0";
    [JsonPropertyName("r_clock")]     public string RClock      { get; set; } = "0";
    [JsonPropertyName("acknowledged")]public string Acknowledged{ get; set; } = "0";
    [JsonPropertyName("hosts")]       public List<ZabbixHostRef> Hosts { get; set; } = [];

    [JsonIgnore] public DateTime OccurredAt  => DateTimeOffset.FromUnixTimeSeconds(long.TryParse(Clock,  out var t) ? t : 0).LocalDateTime;
    [JsonIgnore] public DateTime? ResolvedAt => RClock != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.TryParse(RClock, out var t) ? t : 0).LocalDateTime : null;
    [JsonIgnore] public bool IsAcknowledged  => Acknowledged == "1";
    [JsonIgnore] public bool IsResolved      => RClock != "0";
    [JsonIgnore] public string HostName      => Hosts.FirstOrDefault()?.Name ?? "";
    [JsonIgnore] public string SeverityLabel => int.TryParse(Severity, out var s) ? s switch
    {
        0 => "Not classified", 1 => "Information", 2 => "Warning",
        3 => "Average", 4 => "High", 5 => "Disaster", _ => "Unknown"
    } : "Unknown";
    [JsonIgnore] public string SeverityBadge => int.TryParse(Severity, out var s) ? s switch
    {
        0 => "secondary", 1 => "info", 2 => "warning",
        3 => "warning",   4 => "danger", 5 => "danger", _ => "secondary"
    } : "secondary";
}

// ── Dashboard stats ───────────────────────────────────────────────────────

/// <summary>
/// Aggregated statistics for the main dashboard, computed from live Zabbix host and problem data.
/// All counts reflect the current state at the time the object was populated.
/// </summary>
public class ZabbixDashboardStats
{
    /// <summary>Gets or sets the total number of hosts known to Zabbix.</summary>
    public int TotalHosts      { get; set; }

    /// <summary>Gets or sets the number of hosts that are currently available (reachable).</summary>
    public int AvailableHosts  { get; set; }

    /// <summary>Gets or sets the number of hosts that are currently unavailable (unreachable).</summary>
    public int UnavailableHosts{ get; set; }

    /// <summary>Gets or sets the total number of active problems across all hosts.</summary>
    public int TotalProblems   { get; set; }

    /// <summary>Gets or sets the number of active problems with Disaster severity (level 5).</summary>
    public int DisasterCount   { get; set; }

    /// <summary>Gets or sets the number of active problems with High severity (level 4).</summary>
    public int HighCount       { get; set; }

    /// <summary>Gets or sets the number of active problems with Average severity (level 3).</summary>
    public int AverageCount    { get; set; }

    /// <summary>Gets or sets the number of active problems with Warning severity (level 2).</summary>
    public int WarningCount    { get; set; }

    /// <summary>Gets or sets the number of active problems with Information severity (level 1).</summary>
    public int InfoCount       { get; set; }
}
