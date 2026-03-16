// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
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
    private static readonly Stopwatch s_mainStopwatch = new Stopwatch();

    public static async Task<int> Main(string[] args)
    {
        s_mainStopwatch.Start();
        Console.WriteLine($"[TIMING] Main() entered at {DateTime.UtcNow:HH:mm:ss.fff}");
        LogSystemInfo();

        s_testLibs = Directory.GetFiles(Environment.CurrentDirectory, "*.Tests.dll").ToList();
        if (s_testLibs.Count < 1)
        {
            Console.WriteLine($"Test libs were not found (*.Tests.dll was not found in {Environment.CurrentDirectory})");
            return -1;
        }

        Console.WriteLine($"[TIMING] Found {s_testLibs.Count} test assemblies at +{s_mainStopwatch.ElapsedMilliseconds}ms");
        foreach (string lib in s_testLibs)
        {
            Console.WriteLine($"[TIMING]   - {Path.GetFileName(lib)} ({new FileInfo(lib).Length / 1024}KB)");
        }

        int exitCode = 0;
        s_MainTestName = Path.GetFileNameWithoutExtension(s_testLibs[0]);
        string? verbose = Environment.GetEnvironmentVariable("XUNIT_VERBOSE")?.ToLower();
        bool enableMaxThreads = (Environment.GetEnvironmentVariable("XUNIT_SINGLE_THREADED") != "1");
        var simpleTestRunner = new SimpleAndroidTestRunner(verbose == "true" || verbose == "1", enableMaxThreads);

        bool firstTestLogged = false;
        simpleTestRunner.TestsStarted += (sender, _) =>
        {
            Console.WriteLine($"[TIMING] TestsStarted (discovery beginning) at +{s_mainStopwatch.ElapsedMilliseconds}ms");
        };
        simpleTestRunner.TestStarted += (sender, testName) =>
        {
            if (!firstTestLogged)
            {
                firstTestLogged = true;
                Console.WriteLine($"[TIMING] First test starting at +{s_mainStopwatch.ElapsedMilliseconds}ms: {testName}");
            }
        };
        simpleTestRunner.TestsCompleted += (sender, result) =>
        {
            Console.WriteLine($"[TIMING] TestsCompleted at +{s_mainStopwatch.ElapsedMilliseconds}ms — passed:{result.PassedTests} failed:{result.FailedTests} skipped:{result.SkippedTests}");
            if (result.FailedTests > 0)
                exitCode = 1;
        };

        Console.WriteLine($"[TIMING] Starting RunAsync() at +{s_mainStopwatch.ElapsedMilliseconds}ms");
        await simpleTestRunner.RunAsync();
        Console.WriteLine($"[TIMING] RunAsync() returned at +{s_mainStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("----- Done -----");
        return exitCode;
    }

    private static void LogSystemInfo()
    {
        Console.WriteLine($"[DIAG] ProcessorCount={Environment.ProcessorCount}");
        Console.WriteLine($"[DIAG] GC.GetTotalMemory={GC.GetTotalMemory(forceFullCollection: false) / 1024}KB");
        Console.WriteLine($"[DIAG] WorkingSet64={Environment.WorkingSet / (1024 * 1024)}MB");

        // Read /proc/meminfo for total/available RAM (works on Android/Linux)
        try
        {
            if (File.Exists("/proc/meminfo"))
            {
                foreach (string line in File.ReadLines("/proc/meminfo").Take(5))
                {
                    Console.WriteLine($"[DIAG] {line.Trim()}");
                }
            }
        }
        catch { }

        // Read /proc/cpuinfo summary
        try
        {
            if (File.Exists("/proc/cpuinfo"))
            {
                int cpuCount = 0;
                string? modelName = null;
                foreach (string line in File.ReadLines("/proc/cpuinfo"))
                {
                    if (line.StartsWith("processor"))
                        cpuCount++;
                    if (modelName is null && line.StartsWith("model name"))
                        modelName = line;
                    if (modelName is null && line.StartsWith("Hardware"))
                        modelName = line;
                }
                Console.WriteLine($"[DIAG] CPUs={cpuCount} {modelName?.Trim()}");
            }
        }
        catch { }
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
            Console.WriteLine($"[TIMING] Loading assembly {Path.GetFileName(file)} at +{s_mainStopwatch.ElapsedMilliseconds}ms");
            var sw = Stopwatch.StartNew();
            var assembly = Assembly.LoadFrom(file);
            sw.Stop();
            Console.WriteLine($"[TIMING] Loaded {Path.GetFileName(file)} in {sw.ElapsedMilliseconds}ms ({assembly.GetTypes().Length} types)");
            yield return new TestAssemblyInfo(assembly, file);
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
