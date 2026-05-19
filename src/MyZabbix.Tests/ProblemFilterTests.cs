using FluentAssertions;
using MyZabbix.Core.Models;

namespace MyZabbix.Tests;

/// <summary>
/// Tests filtering of ZabbixProblem collections by severity level,
/// mirroring the logic used in ZabbixApiService.GetProblemsAsync(severityMin).
/// </summary>
public class ProblemFilterTests
{
    private static List<ZabbixProblem> BuildProblems() =>
    [
        new ZabbixProblem { EventId = "1", Name = "Not classified event", Severity = "0" },
        new ZabbixProblem { EventId = "2", Name = "Info event",           Severity = "1" },
        new ZabbixProblem { EventId = "3", Name = "Warning event",        Severity = "2" },
        new ZabbixProblem { EventId = "4", Name = "Average event",        Severity = "3" },
        new ZabbixProblem { EventId = "5", Name = "High event",           Severity = "4" },
        new ZabbixProblem { EventId = "6", Name = "Disaster event",       Severity = "5" },
    ];

    [Fact]
    public void FilterBySeverityMin_5_ReturnsOnlyDisaster()
    {
        var problems = BuildProblems();
        var filtered = problems.Where(p => int.TryParse(p.Severity, out var s) && s >= 5).ToList();

        filtered.Should().HaveCount(1);
        filtered.Single().SeverityLabel.Should().Be("Disaster");
    }

    [Fact]
    public void FilterBySeverityMin_4_ReturnsHighAndDisaster()
    {
        var problems = BuildProblems();
        var filtered = problems.Where(p => int.TryParse(p.Severity, out var s) && s >= 4).ToList();

        filtered.Should().HaveCount(2);
        filtered.Should().AllSatisfy(p =>
            new[] { "High", "Disaster" }.Should().Contain(p.SeverityLabel));
    }

    [Fact]
    public void FilterBySeverityMin_3_ReturnsAverageAndAbove()
    {
        var problems = BuildProblems();
        var filtered = problems.Where(p => int.TryParse(p.Severity, out var s) && s >= 3).ToList();

        filtered.Should().HaveCount(3);
    }

    [Fact]
    public void FilterBySeverityMin_0_ReturnsAllProblems()
    {
        var problems = BuildProblems();
        var filtered = problems.Where(p => int.TryParse(p.Severity, out var s) && s >= 0).ToList();

        filtered.Should().HaveCount(6);
    }

    [Fact]
    public void FilterDisasterOnly_CountIsCorrect()
    {
        var problems = BuildProblems();

        var disasterCount = problems.Count(p => p.Severity == "5");

        disasterCount.Should().Be(1);
    }

    [Fact]
    public void FilterHighSeverity_CountIsCorrect()
    {
        var problems = BuildProblems();

        var highCount = problems.Count(p => p.Severity == "4");

        highCount.Should().Be(1);
    }

    [Fact]
    public void FilterAcknowledgedProblems_ReturnsOnlyAcknowledged()
    {
        var problems = new List<ZabbixProblem>
        {
            new() { EventId = "1", Severity = "4", Acknowledged = "1" },
            new() { EventId = "2", Severity = "5", Acknowledged = "0" },
            new() { EventId = "3", Severity = "2", Acknowledged = "1" },
        };

        var acknowledged = problems.Where(p => p.IsAcknowledged).ToList();

        acknowledged.Should().HaveCount(2);
        acknowledged.Should().AllSatisfy(p => p.IsAcknowledged.Should().BeTrue());
    }

    [Fact]
    public void SortBySeverityDescending_OrderIsCorrect()
    {
        var problems = BuildProblems();
        var sorted = problems
            .Where(p => int.TryParse(p.Severity, out _))
            .OrderByDescending(p => int.Parse(p.Severity))
            .ToList();

        sorted.First().SeverityLabel.Should().Be("Disaster");
        sorted.Last().SeverityLabel.Should().Be("Not classified");
    }
}
