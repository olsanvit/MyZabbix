using FluentAssertions;
using MyZabbix.Core.Models;

namespace MyZabbix.Tests;

public class ZabbixModelTests
{
    // ── ZabbixHost ────────────────────────────────────────────────────────────

    [Fact]
    public void ZabbixHost_DefaultProperties_AreInitialized()
    {
        var host = new ZabbixHost();

        host.HostId.Should().NotBeNull().And.BeEmpty();
        host.Host.Should().NotBeNull().And.BeEmpty();
        host.Name.Should().NotBeNull().And.BeEmpty();
        host.Status.Should().Be("0");
        host.Available.Should().Be("0");
    }

    [Fact]
    public void ZabbixHost_IsEnabled_WhenStatusIsZero()
    {
        var host = new ZabbixHost { Status = "0" };

        host.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ZabbixHost_IsNotEnabled_WhenStatusIsOne()
    {
        var host = new ZabbixHost { Status = "1" };

        host.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void ZabbixHost_IsAvailable_WhenAvailableIsOne()
    {
        var host = new ZabbixHost { Available = "1" };

        host.IsAvailable.Should().BeTrue();
        host.IsDown.Should().BeFalse();
        host.StatusBadge.Should().Be("success");
        host.StatusLabel.Should().Be("Available");
    }

    [Fact]
    public void ZabbixHost_IsDown_WhenAvailableIsTwo()
    {
        var host = new ZabbixHost { Available = "2" };

        host.IsDown.Should().BeTrue();
        host.IsAvailable.Should().BeFalse();
        host.StatusBadge.Should().Be("danger");
        host.StatusLabel.Should().Be("Unavailable");
    }

    [Fact]
    public void ZabbixHost_UnknownAvailability_WhenAvailableIsZero()
    {
        var host = new ZabbixHost { Available = "0" };

        host.IsAvailable.Should().BeFalse();
        host.IsDown.Should().BeFalse();
        host.StatusBadge.Should().Be("secondary");
        host.StatusLabel.Should().Be("Unknown");
    }

    // ── ZabbixProblem ─────────────────────────────────────────────────────────

    [Fact]
    public void ZabbixProblem_DefaultProperties_AreInitialized()
    {
        var problem = new ZabbixProblem();

        problem.EventId.Should().NotBeNull().And.BeEmpty();
        problem.Name.Should().NotBeNull().And.BeEmpty();
        problem.Severity.Should().Be("0");
        problem.Clock.Should().Be("0");
        problem.Acknowledged.Should().Be("0");
        problem.Hosts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ZabbixProblem_IsAcknowledged_WhenAcknowledgedIsOne()
    {
        var problem = new ZabbixProblem { Acknowledged = "1" };

        problem.IsAcknowledged.Should().BeTrue();
    }

    [Fact]
    public void ZabbixProblem_IsNotAcknowledged_WhenAcknowledgedIsZero()
    {
        var problem = new ZabbixProblem { Acknowledged = "0" };

        problem.IsAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void ZabbixProblem_OccurredAt_ParsesUnixTimestamp()
    {
        // Unix timestamp 0 → 1970-01-01 00:00:00 UTC
        var problem = new ZabbixProblem { Clock = "0" };

        problem.OccurredAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime);
    }

    [Fact]
    public void ZabbixProblem_HostName_ReturnsFirstHostName()
    {
        var problem = new ZabbixProblem
        {
            Hosts = [new ZabbixHostRef { HostId = "1", Name = "server-01" }]
        };

        problem.HostName.Should().Be("server-01");
    }

    [Fact]
    public void ZabbixProblem_HostName_ReturnsEmptyWhenNoHosts()
    {
        var problem = new ZabbixProblem();

        problem.HostName.Should().BeEmpty();
    }

    // ── ZabbixTrigger ─────────────────────────────────────────────────────────

    [Fact]
    public void ZabbixTrigger_DefaultProperties_AreInitialized()
    {
        var trigger = new ZabbixTrigger();

        trigger.TriggerId.Should().NotBeNull().And.BeEmpty();
        trigger.Description.Should().NotBeNull().And.BeEmpty();
        trigger.Priority.Should().Be("0");
        trigger.Status.Should().Be("0");
        trigger.Value.Should().Be("0");
        trigger.Hosts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ZabbixTrigger_IsProblem_WhenValueIsOne()
    {
        var trigger = new ZabbixTrigger { Value = "1" };

        trigger.IsProblem.Should().BeTrue();
    }

    [Fact]
    public void ZabbixTrigger_IsNotProblem_WhenValueIsZero()
    {
        var trigger = new ZabbixTrigger { Value = "0" };

        trigger.IsProblem.Should().BeFalse();
    }

    // ── ZabbixItem ────────────────────────────────────────────────────────────

    [Fact]
    public void ZabbixItem_DisplayValue_AppendsUnitsWhenPresent()
    {
        var item = new ZabbixItem { LastValue = "42", Units = "%" };

        item.DisplayValue.Should().Be("42 %");
    }

    [Fact]
    public void ZabbixItem_DisplayValue_ReturnsRawValueWhenNoUnits()
    {
        var item = new ZabbixItem { LastValue = "running", Units = "" };

        item.DisplayValue.Should().Be("running");
    }

    // ── ZabbixDashboardStats ──────────────────────────────────────────────────

    [Fact]
    public void ZabbixDashboardStats_DefaultCounts_AreZero()
    {
        var stats = new ZabbixDashboardStats();

        stats.TotalHosts.Should().Be(0);
        stats.AvailableHosts.Should().Be(0);
        stats.UnavailableHosts.Should().Be(0);
        stats.TotalProblems.Should().Be(0);
        stats.DisasterCount.Should().Be(0);
        stats.HighCount.Should().Be(0);
        stats.AverageCount.Should().Be(0);
        stats.WarningCount.Should().Be(0);
        stats.InfoCount.Should().Be(0);
    }

    [Fact]
    public void ZabbixDashboardStats_CanBePopulated()
    {
        var stats = new ZabbixDashboardStats
        {
            TotalHosts = 10,
            AvailableHosts = 8,
            UnavailableHosts = 2,
            TotalProblems = 5,
            DisasterCount = 1,
            HighCount = 2,
            AverageCount = 1,
            WarningCount = 1,
            InfoCount = 0
        };

        stats.TotalHosts.Should().Be(10);
        stats.AvailableHosts.Should().Be(8);
        stats.DisasterCount.Should().Be(1);
        (stats.DisasterCount + stats.HighCount + stats.AverageCount + stats.WarningCount + stats.InfoCount)
            .Should().Be(stats.TotalProblems);
    }

    // ── ZabbixHostRef ─────────────────────────────────────────────────────────

    [Fact]
    public void ZabbixHostRef_Properties_CanBeSet()
    {
        var hostRef = new ZabbixHostRef { HostId = "42", Name = "web-server" };

        hostRef.HostId.Should().Be("42");
        hostRef.Name.Should().Be("web-server");
    }
}
