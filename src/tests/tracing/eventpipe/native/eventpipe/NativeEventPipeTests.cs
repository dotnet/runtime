// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

public class NativeEventPipeTests
{
    public static bool IsPerformanceTestEnabled => Environment.GetEnvironmentVariable("DOTNET_TEST_NATIVE_EVENTPIPE_PERF") == "1";

    [Theory]
    [MemberData(nameof(TestGroups))]
    public static void RunSuite(string group)
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeEventPipeTestsDriver", group);
        Console.Write(output);
        Console.Error.Write(error);
        Assert.Equal(0, exitCode);
    }

    [ConditionalFact(nameof(IsPerformanceTestEnabled))]
    public static void RunPerformanceSuite()
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeEventPipeTestsDriver", TimeSpan.FromMinutes(15), "perf");
        Console.Write(output);
        Console.Error.Write(error);
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("--list", false)]
    [InlineData("--list-all", true)]
    public static void ListGroupsExcludesPerformanceUnlessRequested(string option, bool includesPerformance)
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeEventPipeTestsDriver", option);
        Assert.True(exitCode == 0, output + error);
        string[] groups = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("eventpipe", groups);
        Assert.Equal(includesPerformance, Array.IndexOf(groups, "perf") >= 0);
    }

    [Fact]
    public static void RunDefaultSuitesExcludesPerformance()
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeEventPipeTestsDriver");
        Assert.True(exitCode == 0, output + error);
        Assert.Contains("[eventpipe]", output);
        Assert.DoesNotContain("[perf]", output);
    }

    public static IEnumerable<object[]> TestGroups()
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeEventPipeTestsDriver", "--list");
        Assert.True(exitCode == 0, output + error);
        string[] groups = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(groups);

        foreach (string group in groups)
        {
            yield return new object[] { group };
        }
    }
}
