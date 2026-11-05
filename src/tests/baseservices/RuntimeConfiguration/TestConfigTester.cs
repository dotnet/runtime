// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;

using Xunit;
using TestLibrary;

public class TestConfigTester
{
    [ActiveIssue("Test expects being run with corerun", typeof(Utilities), nameof(Utilities.IsNativeAot))]
    [SkipOnCoreClr("This test provides no interesting scenarios for GC stress.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void RunTests()
    {
        // clear some environment variables that we will set during the test run
        Environment.SetEnvironmentVariable("DOTNET_gcServer", null);

        string testConfigApp = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestConfig.dll");

        MethodInfo[] infos = typeof(TestConfig).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        string corerunPath = GetCorerunPath();
        foreach (var mi in infos)
        {
            var configProperties = mi.GetCustomAttributes(typeof(TestConfig.ConfigPropertyAttribute));
            var envVariables = mi.GetCustomAttributes(typeof(TestConfig.EnvVarAttribute));

            if (configProperties.Count() == 0 && envVariables.Count() == 0)
            {
                continue;
            }

            ProcessStartInfo startInfo = new(corerunPath);
            startInfo.InheritedHandles = [];

            foreach (Attribute cp in configProperties)
            {
                TestConfig.ConfigPropertyAttribute configProp = (TestConfig.ConfigPropertyAttribute)cp;
                startInfo.ArgumentList.Add("-p");
                startInfo.ArgumentList.Add($"{configProp.Name}={configProp.Value}");
            }

            startInfo.ArgumentList.Add(testConfigApp);
            startInfo.ArgumentList.Add(mi.Name);

            Console.WriteLine($"Running: {string.Join(" ", startInfo.ArgumentList)}");
            foreach (Attribute ev in envVariables)
            {
                TestConfig.EnvVarAttribute envVar = (TestConfig.EnvVarAttribute)ev;
                startInfo.EnvironmentVariables[envVar.Name] = envVar.Value;
                Console.WriteLine($"    set {envVar.Name}={envVar.Value}");
            }

            ProcessExitStatus status = Process.Run(startInfo);
            if (status.ExitCode != TestConfig.Success)
            {
                throw new Exception($"Failed: {mi.Name}: exit code = {status.ExitCode}");
            }
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData("..", false)]
    [InlineData("..", true)]
    public static void PrecomputedResultsUsePlanPaths(string? testPathPrefix, bool useResultDirectory)
    {
        const string ResultTokenVariable = "__TestOutOfProcessResultToken";
        const string ResultDirectoryVariable = "__TestOutOfProcessResultDirectory";
        string previousToken = Environment.GetEnvironmentVariable(ResultTokenVariable);
        string previousDirectory = Environment.GetEnvironmentVariable(ResultDirectoryVariable);
        string directoryName = $"outofprocess-{Guid.NewGuid():N}";
        string directory = Path.Combine(Directory.GetCurrentDirectory(), directoryName);
        string assemblyPath = $"{directoryName}/test@name.dll";
        string resultFile = Path.Combine(directory, useResultDirectory
            ? $"{directoryName}@stest@aname.dll.outofprocess-result"
            : "test@name.dll.outofprocess-result");
        string token = Guid.NewGuid().ToString();

        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllLines(resultFile, ["2", token, "0", "Pass", "", "Precomputed test output."]);
            Environment.SetEnvironmentVariable(ResultTokenVariable, token);
            Environment.SetEnvironmentVariable(ResultDirectoryVariable, useResultDirectory ? directory : null);

            OutOfProcessTest.RunOutOfProcessTest(assemblyPath, testPathPrefix);

            Environment.SetEnvironmentVariable(ResultTokenVariable, "stale");
            Assert.Contains("is stale or belongs to another run",
                Assert.Throws<Xunit.Sdk.FailException>(() =>
                    OutOfProcessTest.RunOutOfProcessTest(assemblyPath, testPathPrefix)).Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ResultTokenVariable, previousToken);
            Environment.SetEnvironmentVariable(ResultDirectoryVariable, previousDirectory);
            Directory.Delete(directory, recursive: true);
        }
    }

    static string GetCorerunPath()
    {
        string corerunName = "corerun";
        if (TestLibrary.Utilities.IsWindows)
        {
            corerunName += ".exe";
        }
        return Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), corerunName);
    }
}
