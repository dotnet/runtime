// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Microsoft.DotNet.XHarness.TestRunners.Xunit;

public partial class SimpleTestRunner : iOSApplicationEntryPoint, IDevice
{
    // to be wired once https://github.com/dotnet/xharness/pull/46 is merged
    [LibraryImport("__Internal", EntryPoint = "mono_ios_append_output", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void mono_ios_append_output (string value);

    [LibraryImport("__Internal", EntryPoint = "mono_ios_set_summary", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void mono_ios_set_summary (string value);

    private static List<string> s_testLibs = new List<string>();
    private static List<Assembly>? s_testAssemblies;
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

        string cwd = AppContext.BaseDirectory;
        string maccatResources = Path.Combine(cwd, "..", "Resources");
        if (Directory.Exists(maccatResources))
        {
            cwd = Path.GetFullPath(maccatResources);
        }
        Directory.SetCurrentDirectory(cwd);

        foreach (string arg in args.Where(a => a.StartsWith("testlib:")))
        {
            s_testLibs.Add(arg.Remove(0, "testlib:".Length));
        }
        bool verbose = args.Contains("--verbose");

        if (s_testLibs.Count < 1)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                s_testAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name?.EndsWith(".Tests") == true)
                    .ToList();
                s_testLibs = s_testAssemblies.Select(a => a.GetName().Name!).ToList();
            }
            else
            {
                s_testLibs = Directory.GetFiles(Environment.CurrentDirectory, "*.Tests.dll").ToList();
            }
        }

        if (s_testLibs.Count < 1)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                Console.WriteLine("No assemblies ending with '.Tests' were found among loaded assemblies.");
            }
            else
            {
                Console.WriteLine($"Test libs were not found (*.Tests.dll was not found in {Environment.CurrentDirectory})");
            }

            return -1;
        }

        Console.Write("Test libs: ");
        foreach (string testLib in s_testLibs)
        {
            Console.WriteLine(testLib);
        }
        Console.WriteLine(".");
        s_MainTestName = s_testAssemblies is not null
            ? s_testAssemblies[0].GetName().Name
            : Path.GetFileNameWithoutExtension(s_testLibs[0]);

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

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "On NativeAOT s_testAssemblies is populated from AppDomain.CurrentDomain.GetAssemblies() and the Assembly.LoadFrom branch is never taken.")]
    protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies()
    {
        if (s_testAssemblies is not null)
        {
            foreach (Assembly assembly in s_testAssemblies)
            {
                yield return new TestAssemblyInfo(assembly, assembly.GetName().Name! + ".dll");
            }
        }
        else
        {
            foreach (string file in s_testLibs)
            {
                yield return new TestAssemblyInfo(Assembly.LoadFrom(file), file);
            }
        }
    }

    protected override void TerminateWithSuccess()
    {
        Console.WriteLine("[TerminateWithSuccess]");
    }

    private int? _maxParallelThreads;

    protected override int? MaxParallelThreads => _maxParallelThreads;

    protected override IDevice Device => this;

    protected override string? IgnoreFilesDirectory => null;

    protected override string IgnoredTraitsFilePath
    {
        get
        {
            string path = Path.Combine(AppContext.BaseDirectory, "xunit-excludes.txt");
            if (File.Exists(path))
            {
                return path;
            }

            string? appBase = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            if (appBase is not null)
            {
                string resourcesPath = Path.Combine(appBase, "Resources", "xunit-excludes.txt");
                if (File.Exists(resourcesPath))
                {
                    return resourcesPath;
                }
            }

            return string.Empty;
        }
    }

    public string BundleIdentifier => "net.dot." + s_MainTestName;

    public string? UniqueIdentifier { get; }

    public string? Name { get; }

    public string? Model { get; }

    public string? SystemName { get; }

    public string? SystemVersion { get; }

    public string? Locale { get; }
}
