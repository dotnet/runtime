using System.Collections.Generic;
using AutoBisect;
using Xunit;

namespace AutoBisect.Tests;

public class TestDifferTests
{
    [Fact]
    public void ComputeDiff_IdenticalFailures_AllConsistent()
    {
        // Arrange - only failed tests are passed to the differ
        var goodFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "Test2", Outcome = TestOutcome.Failed },
        };

        var badFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "Test2", Outcome = TestOutcome.Failed },
        };

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Empty(diff.NewFailures);
        Assert.Single(diff.ConsistentFailures);
        Assert.Equal("Test2", diff.ConsistentFailures[0]);
    }

    [Fact]
    public void ComputeDiff_NewFailure_DetectedCorrectly()
    {
        // Arrange - good build has no failures, bad build has one
        var goodFailures = new List<TestResult>();

        var badFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "Test2", Outcome = TestOutcome.Failed },
        };

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Single(diff.NewFailures);
        Assert.Equal("Test2", diff.NewFailures[0]);
        Assert.Empty(diff.ConsistentFailures);
    }

    [Fact]
    public void ComputeDiff_FailureOnlyInGood_NotReported()
    {
        // Arrange - good build has a failure that bad build doesn't have
        // (test was fixed, but we can't report "fixed" without passed test data)
        var goodFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "Test1", Outcome = TestOutcome.Failed },
        };

        var badFailures = new List<TestResult>();

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Empty(diff.NewFailures);
        Assert.Empty(diff.ConsistentFailures);
    }

    [Fact]
    public void ComputeDiff_MultipleChanges_CategorizedCorrectly()
    {
        // Arrange - only failures are passed
        var goodFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "AlwaysFails", Outcome = TestOutcome.Failed },
            new() { AutomatedTestName = "GetsFixed", Outcome = TestOutcome.Failed },
        };

        var badFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "BecomesFailing", Outcome = TestOutcome.Failed },
            new() { AutomatedTestName = "AlwaysFails", Outcome = TestOutcome.Failed },
        };

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Single(diff.NewFailures);
        Assert.Contains("BecomesFailing", diff.NewFailures);

        Assert.Single(diff.ConsistentFailures);
        Assert.Contains("AlwaysFails", diff.ConsistentFailures);
    }

    [Fact]
    public void ComputeDiff_CaseInsensitive_TreatsAsSameTest()
    {
        // Arrange
        var goodFailures = new List<TestResult>();

        var badFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "MYNAMESPACE.MYTEST", Outcome = TestOutcome.Failed },
        };

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Single(diff.NewFailures);
    }

    [Fact]
    public void ComputeDiff_CaseInsensitive_ConsistentFailure()
    {
        // Arrange
        var goodFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "MyNamespace.MyTest", Outcome = TestOutcome.Failed },
        };

        var badFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "MYNAMESPACE.MYTEST", Outcome = TestOutcome.Failed },
        };

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Empty(diff.NewFailures);
        Assert.Single(diff.ConsistentFailures);
    }

    [Fact]
    public void ComputeDiff_EmptyResults_ReturnsEmptyDiff()
    {
        // Arrange
        var goodFailures = new List<TestResult>();
        var badFailures = new List<TestResult>();

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Empty(diff.NewFailures);
        Assert.Empty(diff.ConsistentFailures);
    }

    [Fact]
    public void ComputeDiff_OnlyGoodHasFailures_NoNewFailures()
    {
        // Arrange
        var goodFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "Test2", Outcome = TestOutcome.Failed },
        };
        var badFailures = new List<TestResult>();

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Empty(diff.NewFailures);
        Assert.Empty(diff.ConsistentFailures);
    }

    [Fact]
    public void ComputeDiff_ResultsAreSorted()
    {
        // Arrange
        var goodFailures = new List<TestResult>();

        var badFailures = new List<TestResult>
        {
            new() { AutomatedTestName = "Zebra", Outcome = TestOutcome.Failed },
            new() { AutomatedTestName = "Apple", Outcome = TestOutcome.Failed },
            new() { AutomatedTestName = "Mango", Outcome = TestOutcome.Failed },
        };

        // Act
        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        // Assert
        Assert.Equal(3, diff.NewFailures.Count);
        Assert.Equal("Apple", diff.NewFailures[0]);
        Assert.Equal("Mango", diff.NewFailures[1]);
        Assert.Equal("Zebra", diff.NewFailures[2]);
    }
}
