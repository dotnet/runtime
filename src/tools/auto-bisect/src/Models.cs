using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AutoBisect;

/// <summary>
/// Represents an Azure DevOps build.
/// </summary>
public class Build
{
    public int Id { get; set; }
    public string? BuildNumber { get; set; }
    public BuildStatus Status { get; set; }
    public BuildResult? Result { get; set; }
    public string? SourceVersion { get; set; }
    public string? SourceBranch { get; set; }
    public DateTime? QueueTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public BuildDefinition? Definition { get; set; }
    public string? Url { get; set; }

    [JsonPropertyName("_links")]
    public BuildLinks? Links { get; set; }
}

public class BuildDefinition
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class BuildLinks
{
    public Link? Web { get; set; }
}

public class Link
{
    public string? Href { get; set; }
}

public enum BuildStatus
{
    None,
    InProgress,
    Completed,
    Cancelling,
    Postponed,
    NotStarted,
    All,
}

public enum BuildResult
{
    None,
    Succeeded,
    PartiallySucceeded,
    Failed,
    Canceled,
}

/// <summary>
/// Represents a test result from Azure DevOps.
/// </summary>
public class TestResult
{
    public int Id { get; set; }
    public string? TestCaseTitle { get; set; }
    public string? AutomatedTestName { get; set; }
    public string? AutomatedTestStorage { get; set; }
    public TestOutcome Outcome { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public double DurationInMs { get; set; }

    /// <summary>
    /// Gets the fully qualified test name.
    /// </summary>
    public string FullyQualifiedName => AutomatedTestName ?? TestCaseTitle ?? $"Unknown-{Id}";
}

public enum TestOutcome
{
    Unspecified,
    None,
    Passed,
    Failed,
    Inconclusive,
    Timeout,
    Aborted,
    Blocked,
    NotExecuted,
    Warning,
    Error,
    NotApplicable,
    Paused,
    InProgress,
    NotImpacted,
}

/// <summary>
/// Represents the diff between test failures from two builds.
/// </summary>
public class TestFailureDiff
{
    /// <summary>
    /// Tests that failed in the bad build but not in the good build.
    /// </summary>
    public required List<string> NewFailures { get; init; }

    /// <summary>
    /// Tests that failed in both builds.
    /// </summary>
    public required List<string> ConsistentFailures { get; init; }
}

/// <summary>
/// Utility class for computing test failure diffs.
/// </summary>
public static class TestDiffer
{
    /// <summary>
    /// Computes the diff between failed tests from two builds.
    /// </summary>
    public static TestFailureDiff ComputeDiff(
        IEnumerable<TestResult> goodBuildFailures,
        IEnumerable<TestResult> badBuildFailures
    )
    {
        var goodFailures = goodBuildFailures
            .Select(t => t.FullyQualifiedName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var badFailures = badBuildFailures
            .Select(t => t.FullyQualifiedName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // New failures: failed in bad, but not failed in good
        var newFailures = badFailures
            .Where(t => !goodFailures.Contains(t))
            .OrderBy(t => t)
            .ToList();

        // Consistent failures: failed in both
        var consistentFailures = badFailures
            .Where(t => goodFailures.Contains(t))
            .OrderBy(t => t)
            .ToList();

        return new TestFailureDiff
        {
            NewFailures = newFailures,
            ConsistentFailures = consistentFailures,
        };
    }
}
