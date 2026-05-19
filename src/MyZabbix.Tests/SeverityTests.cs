using FluentAssertions;
using MyZabbix.Core.Models;

namespace MyZabbix.Tests;

/// <summary>
/// Tests severity level parsing and mapping on ZabbixProblem.
/// Zabbix severity scale: 0=Not classified, 1=Information, 2=Warning, 3=Average, 4=High, 5=Disaster
/// </summary>
public class SeverityTests
{
    [Theory]
    [InlineData("5", "Disaster")]
    [InlineData("4", "High")]
    [InlineData("3", "Average")]
    [InlineData("2", "Warning")]
    [InlineData("1", "Information")]
    [InlineData("0", "Not classified")]
    public void ZabbixProblem_SeverityLabel_MapsCorrectly(string severityCode, string expectedLabel)
    {
        var problem = new ZabbixProblem { Severity = severityCode };

        problem.SeverityLabel.Should().Be(expectedLabel);
    }

    [Fact]
    public void ZabbixProblem_Severity5_IsDisaster()
    {
        var problem = new ZabbixProblem { Severity = "5" };

        problem.SeverityLabel.Should().Be("Disaster");
    }

    [Fact]
    public void ZabbixProblem_Severity4_IsHigh()
    {
        var problem = new ZabbixProblem { Severity = "4" };

        problem.SeverityLabel.Should().Be("High");
    }

    [Fact]
    public void ZabbixProblem_Severity3_IsAverage()
    {
        var problem = new ZabbixProblem { Severity = "3" };

        problem.SeverityLabel.Should().Be("Average");
    }

    [Fact]
    public void ZabbixProblem_Severity2_IsWarning()
    {
        var problem = new ZabbixProblem { Severity = "2" };

        problem.SeverityLabel.Should().Be("Warning");
    }

    [Fact]
    public void ZabbixProblem_Severity1_IsInformation()
    {
        var problem = new ZabbixProblem { Severity = "1" };

        problem.SeverityLabel.Should().Be("Information");
    }

    [Fact]
    public void ZabbixProblem_Severity0_IsNotClassified()
    {
        var problem = new ZabbixProblem { Severity = "0" };

        problem.SeverityLabel.Should().Be("Not classified");
    }

    [Theory]
    [InlineData("99")]
    [InlineData("abc")]
    [InlineData("")]
    public void ZabbixProblem_InvalidSeverity_ReturnsUnknown(string severity)
    {
        var problem = new ZabbixProblem { Severity = severity };

        problem.SeverityLabel.Should().Be("Unknown");
    }

    [Theory]
    [InlineData("5", "danger")]
    [InlineData("4", "danger")]
    [InlineData("3", "warning")]
    [InlineData("2", "warning")]
    [InlineData("1", "info")]
    [InlineData("0", "secondary")]
    public void ZabbixProblem_SeverityBadge_MapsToBootstrapColor(string severity, string expectedBadge)
    {
        var problem = new ZabbixProblem { Severity = severity };

        problem.SeverityBadge.Should().Be(expectedBadge);
    }

    [Fact]
    public void ZabbixTrigger_Priority_DefaultIsZero()
    {
        var trigger = new ZabbixTrigger();

        trigger.Priority.Should().Be("0");
    }
}
