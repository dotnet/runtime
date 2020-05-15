// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.DotNet.XHarness.Tests.Runners;
using Microsoft.DotNet.XHarness.Tests.Runners.Core;

public class SimpleTestRunner : iOSApplicationEntryPoint, IDevice
{
    // to be wired once https://github.com/dotnet/xharness/pull/46 is merged
    [DllImport("__Internal")]
    public extern static void mono_ios_append_output (string value);

    [DllImport("__Internal")]
    public extern static void mono_ios_set_summary (string value);

    private static List<string> s_testLibs = new List<string>();
    private static string? s_MainTestName;

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"ProcessorCount = {Environment.ProcessorCount}");

        Console.Write("Args: ");
        foreach (string arg in args)
        {
            Console.Write(arg);
        }
        Console.WriteLine(".");

        foreach (string arg in args.Where(a => a.StartsWith("testlib:")))
        {
            s_testLibs.Add(arg.Remove(0, "testlib:".Length));
        }
        bool verbose = args.Contains("--verbose");

        if (s_testLibs.Count < 1)
        {
            // Look for *.Tests.dll files if target test suites are not set via "testlib:" arguments
            s_testLibs = Directory.GetFiles(Environment.CurrentDirectory, "*.Tests.dll").ToList();
        }

        if (s_testLibs.Count < 1)
        {
            Console.WriteLine($"Test libs were not found (*.Tests.dll was not found in {Environment.CurrentDirectory})");
            return -1;
        }

        Console.Write("Test libs: ");
        foreach (string testLib in s_testLibs)
        {
            Console.WriteLine(testLib);
        }
        Console.WriteLine(".");
        s_MainTestName = Path.GetFileNameWithoutExtension(s_testLibs[0]);

        mono_ios_set_summary($"Starting tests...");
        var simpleTestRunner = new SimpleTestRunner(verbose);
        simpleTestRunner.TestStarted += (target, e) =>
        {
            mono_ios_append_output($"[STARTING] {e}\n");
        };

        int failed = 0, passed = 0, skipped = 0;
        simpleTestRunner.TestCompleted += (target, e) =>
        {
            if (e.Item2 == TestResult.Passed)
            {
                passed++;
            }
            else if (e.Item2 == TestResult.Failed)
            {
                failed++;
            }
            else if (e.Item2 == TestResult.Skipped)
            {
                skipped++;
            }
            mono_ios_set_summary($"{s_MainTestName}\nPassed:{passed}, Failed: {failed}, Skipped:{skipped}");
        };

        await simpleTestRunner.RunAsync();
        mono_ios_append_output($"\nDone.\n");
        Console.WriteLine("----- Done -----");
        return 0;
    }

    public SimpleTestRunner(bool verbose)
    {
        if (verbose)
        {
            MinimumLogLevel = MinimumLogLevel.Verbose;
            _maxParallelThreads = 1;
        }
        else
        {
            MinimumLogLevel = MinimumLogLevel.Info;
            _maxParallelThreads = Environment.ProcessorCount;
        }
    }

    protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies()
    {
        foreach (string file in s_testLibs)
        {
            yield return new TestAssemblyInfo(Assembly.LoadFrom(file), file);
        }
    }

    protected override void TerminateWithSuccess()
    {
        Console.WriteLine("[TerminateWithSuccess]");
    }

    private int? _maxParallelThreads;

    protected override int? MaxParallelThreads => _maxParallelThreads;

    protected override IDevice Device => this;

    protected override TestRunnerType TestRunner => TestRunnerType.Xunit;

    protected override string? IgnoreFilesDirectory => null;

    protected override string IgnoredTraitsFilePath => "xunit-excludes.txt";

    public string BundleIdentifier => "net.dot." + s_MainTestName;

    public string? UniqueIdentifier { get; }

    public string? Name { get; }

    public string? Model { get; }

    public string? SystemName { get; }

    public string? SystemVersion { get; }

    public string? Locale { get; }
}
