using System.Collections.Generic;
using AutoBisect;
using Xunit;

namespace AutoBisect.Tests;

public class ModelsTests
{
    [Fact]
    public void TestResult_FullyQualifiedName_UsesAutomatedTestName()
    {
        // Arrange
        var result = new TestResult
        {
            Id = 1,
            AutomatedTestName = "MyNamespace.MyClass.MyTest",
            TestCaseTitle = "My Test Title",
        };

        // Assert
        Assert.Equal("MyNamespace.MyClass.MyTest", result.FullyQualifiedName);
    }

    [Fact]
    public void TestResult_FullyQualifiedName_FallsBackToTestCaseTitle()
    {
        // Arrange
        var result = new TestResult
        {
            Id = 1,
            AutomatedTestName = null,
            TestCaseTitle = "My Test Title",
        };

        // Assert
        Assert.Equal("My Test Title", result.FullyQualifiedName);
    }

    [Fact]
    public void TestResult_FullyQualifiedName_FallsBackToId()
    {
        // Arrange
        var result = new TestResult
        {
            Id = 42,
            AutomatedTestName = null,
            TestCaseTitle = null,
        };

        // Assert
        Assert.Equal("Unknown-42", result.FullyQualifiedName);
    }

    [Fact]
    public void Build_DefaultValues()
    {
        // Arrange
        var build = new Build();

        // Assert
        Assert.Equal(0, build.Id);
        Assert.Null(build.BuildNumber);
        Assert.Equal(BuildStatus.None, build.Status);
        Assert.Null(build.Result);
        Assert.Null(build.SourceVersion);
    }

    [Fact]
    public void TestFailureDiff_RequiredProperties()
    {
        // Arrange & Act
        var diff = new TestFailureDiff
        {
            NewFailures = new List<string> { "Test1", "Test2" },
            ConsistentFailures = new List<string>(),
        };

        // Assert
        Assert.Equal(2, diff.NewFailures.Count);
        Assert.Empty(diff.ConsistentFailures);
    }
}
