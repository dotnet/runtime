// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

public class NativeContainerTests
{
    public static bool IsPerformanceTestEnabled => Environment.GetEnvironmentVariable("DOTNET_TEST_NATIVE_CONTAINER_PERF") == "1";

    [Theory]
    [InlineData("vector")]
    [InlineData("ptr-vector")]
    [InlineData("fwd_list")]
    [InlineData("list")]
    [InlineData("queue")]
    [InlineData("umap")]
    [InlineData("simdhash")]
    public static void RunSuite(string group)
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeContainerTestsDriver", group);
        Console.Write(output);
        Console.Error.Write(error);
        Assert.Equal(0, exitCode);
    }

    [ConditionalFact(nameof(IsPerformanceTestEnabled))]
    public static void RunPerformanceSuite()
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeContainerTestsDriver", TimeSpan.FromMinutes(15), "perf");
        Console.Write(output);
        Console.Error.Write(error);
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("--list", false)]
    [InlineData("--list-all", true)]
    public static void ListGroupsExcludesPerformanceUnlessRequested(string option, bool includesPerformance)
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeContainerTestsDriver", option);
        Assert.True(exitCode == 0, output + error);
        string[] groups = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("simdhash", groups);
        Assert.Equal(includesPerformance, Array.IndexOf(groups, "perf") >= 0);
    }

    [Fact]
    public static void RunDefaultSuitesExcludesPerformance()
    {
        (int exitCode, string output, string error) = NativeTestProcess.Run("NativeContainerTestsDriver");
        Assert.True(exitCode == 0, output + error);
        Assert.Contains("[simdhash]", output);
        Assert.DoesNotContain("[perf]", output);
    }

    [Fact]
    public static void RunWithEmptyBaseDirectory()
    {
        string currentDirectory = Environment.CurrentDirectory;
        string baseDirectory = AppContext.BaseDirectory;
        object previousBaseDirectory = AppContext.GetData("APP_CONTEXT_BASE_DIRECTORY");
        try
        {
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                Environment.CurrentDirectory = baseDirectory;
            }
            AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", string.Empty);
            string directory = TestLibrary.Utilities.IsNativeAot ? ".." : ".";
            string fileName = "NativeContainerTestsDriver" + (OperatingSystem.IsWindows() ? ".exe" : "");
            string executablePath = Path.GetFullPath(Path.Combine(directory, fileName));
            (int exitCode, string output, string error) = NativeTestProcess.Run("NativeContainerTestsDriver", "--help");
            Assert.True(exitCode == 0, output + error);
            Assert.StartsWith($"Usage: {executablePath} ", output);
        }
        finally
        {
            AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", previousBaseDirectory);
            Environment.CurrentDirectory = currentDirectory;
        }
    }
}
