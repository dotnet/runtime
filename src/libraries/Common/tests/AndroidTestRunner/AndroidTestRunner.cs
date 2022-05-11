// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Microsoft.DotNet.XHarness.TestRunners.Xunit;

public class SimpleAndroidTestRunner : AndroidApplicationEntryPoint, IDevice
{
    private static List<string> s_testLibs = new List<string>();
    private static string? s_MainTestName;

    public static async Task<int> Main(string[] args)
    {
        s_testLibs = Directory.GetFiles(Environment.CurrentDirectory, "*.Tests.dll").ToList();
        if (s_testLibs.Count < 1)
        {
            Console.WriteLine($"Test libs were not found (*.Tests.dll was not found in {Environment.CurrentDirectory})");
            return -1;
        }
        int exitCode = 0;
        s_MainTestName = Path.GetFileNameWithoutExtension(s_testLibs[0]);
        string? verbose = Environment.GetEnvironmentVariable("XUNIT_VERBOSE")?.ToLower();
        bool enableMaxThreads = (Environment.GetEnvironmentVariable("XUNIT_SINGLE_THREADED") != "1");
        var simpleTestRunner = new SimpleAndroidTestRunner(verbose == "true" || verbose == "1", enableMaxThreads);
        simpleTestRunner.TestsCompleted += (e, result) => 
        {
            if (result.FailedTests > 0)
                exitCode = 1;
        };

        await simpleTestRunner.RunAsync();
        Console.WriteLine("----- Done -----");
        return exitCode;
    }

    public SimpleAndroidTestRunner(bool verbose, bool enableMaxThreads)
    {
        MinimumLogLevel = (verbose) ? MinimumLogLevel.Verbose : MinimumLogLevel.Info;
        _maxParallelThreads = (enableMaxThreads) ? Environment.ProcessorCount : 1;

        if (!enableMaxThreads)
        {
            Console.WriteLine("XUNIT: SINGLE THREADED MODE ENABLED");
        }
    }

    protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies()
    {
        foreach (string file in s_testLibs)
        {
            yield return new TestAssemblyInfo(Assembly.LoadFrom(file), file);
        }
    }

    protected override void TerminateWithSuccess() {}

    private int? _maxParallelThreads;

    protected override int? MaxParallelThreads => _maxParallelThreads;

    protected override IDevice Device => this;

    protected override string? IgnoreFilesDirectory => null;

    protected override string IgnoredTraitsFilePath => "xunit-excludes.txt";

    public string BundleIdentifier => "net.dot." + s_MainTestName;

    public string? UniqueIdentifier { get; }

    public string? Name { get; }

    public string? Model { get; }

    public string? SystemName { get; }

    public string? SystemVersion { get; }

    public string? Locale { get; }

    public override TextWriter? Logger => null;

    public override string TestsResultsFinalPath
    {
        get
        {
            string? testResultsDir = Environment.GetEnvironmentVariable("TEST_RESULTS_DIR");
            if (string.IsNullOrEmpty(testResultsDir))
                throw new ArgumentException("TEST_RESULTS_DIR should not be empty");

            return Path.Combine(testResultsDir, "testResults.xml");
        }
    }
}
