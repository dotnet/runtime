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

public class SimpleTestRunner : iOSApplicatonEntryPoint, IDevice
{
    private static List<string> testLibs = new List<string>();

    public static async Task<int> Main(string[] args)
    {
        // Redirect all Console.WriteLines to iOS UI
        Console.SetOut(new AppleConsole());
        Console.WriteLine($"ProcessorCount = {Environment.ProcessorCount}");

        Console.Write("Args: ");
        foreach (string arg in args)
        {
            Console.Write(arg);
        }
        Console.WriteLine(".");

        foreach (string arg in args.Where(a => a.StartsWith("testlib:")))
        {
            testLibs.Add(arg.Remove(0, "testlib:".Length));
        }

        if (testLibs.Count < 1)
        {
            // Look for *.Tests.dll files if target test suites are not set via "testlib:" arguments
            testLibs = Directory.GetFiles(Environment.CurrentDirectory, "*.Tests.dll").ToList();
        }

        if (testLibs.Count < 1)
        {
            Console.WriteLine($"Test libs were not found (*.Tests.dll was not found in {Environment.CurrentDirectory})");
            return -1;
        }

        Console.Write("Test libs: ");
        foreach (string testLib in testLibs)
        {
            Console.WriteLine(testLib);
        }
        Console.WriteLine(".");

        AppleConsole.mono_ios_set_summary($"Running\n{Path.GetFileName(testLibs[0])} tests...");
        var simpleTestRunner = new SimpleTestRunner();
        await simpleTestRunner.RunAsync();
        AppleConsole.mono_ios_set_summary("Done.");
        Console.WriteLine("----- Done -----");
        return 0;
    }

    protected override IEnumerable<TestAssemblyInfo> GetTestAssemblies()
    {
        foreach (string file in testLibs!)
        {
            yield return new TestAssemblyInfo(Assembly.LoadFrom(file), file);
        }
    }

    protected override void TerminateWithSuccess()
    {
        Console.WriteLine("[TerminateWithSuccess]");
    }

    protected override int? MaxParallelThreads => Environment.ProcessorCount;

    protected override IDevice Device => this;

    protected override TestRunnerType TestRunner => TestRunnerType.Xunit;

    protected override string? IgnoreFilesDirectory { get; }

    public string BundleIdentifier => "net.dot.test-runner";

    public string? UniqueIdentifier { get; }

    public string? Name { get; }

    public string? Model { get; }

    public string? SystemName { get; }

    public string? SystemVersion { get; }

    public string? Locale { get; }
}

internal class AppleConsole : TextWriter
{
    public override Encoding Encoding => Encoding.Default;

    [DllImport("__Internal")]
    public extern static void mono_ios_append_output (string value);

    [DllImport("__Internal")]
    public extern static void mono_ios_set_summary (string value);

    public override void Write(string? value)
    {
        if (value != null)
        {
            mono_ios_append_output(value);
        }
    }
}
