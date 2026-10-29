// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
#if INPROC_SCENARIO_ONDEMAND
using System.Collections.Generic;
#endif
using System.Diagnostics;
using System.Globalization;
using System.IO;
#if INPROC_ANDROID
using System.IO.Compression;
#endif
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
#if INPROC_SCENARIO_ONDEMAND
using System.Threading;
using System.Threading.Tasks;
#endif

public static class Program
{
#if INPROC_SCENARIO_ABORT
    private const int ScenarioId = 1;
#elif INPROC_SCENARIO_STACKOVERFLOW
    private const int ScenarioId = 2;
#elif INPROC_SCENARIO_CONSOLEONLY
    private const int ScenarioId = 3;
#elif INPROC_SCENARIO_RICHSIGSEGV || INPROC_SCENARIO_ONDEMAND
    private const int ScenarioId = 0;
#else
#error Define an INPROC_SCENARIO_* symbol for this test.
#endif

#if INPROC_ANDROID
    private const string NativeLib = "libmonodroid";
#else
    private const string NativeLib = "InProcCrashReportNative";
#endif

    private const string ModuleGuid = "{11111111-2222-3333-4455-66778899aabb}";
    private const string Separator = "*** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***";

    [DllImport(NativeLib)]
    private static extern int InProcCrashReportTest_DriveScenario(
        int scenario, string reportRootPath, string consoleCapturePath);

#if INPROC_SCENARIO_ONDEMAND
    private static readonly TimeSpan s_concurrencyTimeout = TimeSpan.FromSeconds(60);

    // Matches InProcCrashReportOutputFormat in the native reporter.
    private enum ReportFormat : uint
    {
        Json = 0,
        Log = 1,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ReportCallback();

    [DllImport(NativeLib)]
    private static extern int InProcCrashReportTest_DriveOnDemand(
        string reportRootPath, string firstJsonPath, string secondJsonPath,
        string firstLogPath, string secondLogPath);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int InProcCrashReportTest_CreateOnDemandReport(
        ReportFormat format, int signal, string outputPath, ReportCallback beforeWrite);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int InProcCrashReportTest_CreateSignalReport(
        string consolePath, ReportCallback beforeEnumerate);
#endif

#if INPROC_ANDROID || INPROC_APPLE
    public static int Main()
#else
    [Xunit.Fact]
    public static int TestEntryPoint()
#endif
    {
#if INPROC_SCENARIO_ONDEMAND
        return RunTest(RunOnDemand);
#else
        return RunTest(RunScenario);
#endif
    }

    private static int RunTest(Action<string> scenario)
    {
#if INPROC_ANDROID || INPROC_APPLE
        string outputRoot = Path.GetTempPath();
#else
        string? outputRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
        if (string.IsNullOrEmpty(outputRoot))
        {
            outputRoot = Path.GetTempPath();
        }
#endif
#if INPROC_ANDROID
        string? archivePath = null;
#endif
        string outputDirectory = Directory.CreateDirectory(
            Path.Combine(outputRoot, $"inproccrashreport-{Guid.NewGuid():N}")).FullName;
        Stopwatch timer = Stopwatch.StartNew();
        Console.WriteLine($"InProcCrashReport: starting {typeof(Program).Assembly.GetName().Name}; output={outputDirectory}");
        Console.Out.Flush();

        try
        {
#if INPROC_ANDROID
            archivePath = Environment.GetEnvironmentVariable("DOTNET_InProcCrashReportTestArchive") ??
                throw new InvalidOperationException("DOTNET_InProcCrashReportTestArchive was not configured");
            // XHarness expects the configured artifact to exist even on success.
            WriteArtifactArchive(archivePath);
#endif
            scenario(outputDirectory);
            Directory.Delete(outputDirectory, recursive: true);
            Console.WriteLine($"PASS: crash report assertions completed in {timer.ElapsedMilliseconds} ms");
            return 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL after {timer.ElapsedMilliseconds} ms: {ex}");
            DumpOutputs(outputDirectory);
#if INPROC_ANDROID
            if (archivePath is not null)
            {
                try
                {
                    WriteArtifactArchive(archivePath, outputDirectory);
                    Console.WriteLine($"Android failure artifacts: {archivePath}");
                }
                catch (Exception artifactException) when (artifactException is IOException or UnauthorizedAccessException)
                {
                    Console.WriteLine($"Could not archive failure artifacts: {artifactException}");
                }
            }
#endif
            Console.WriteLine($"Retained crash report outputs: {outputDirectory}");
            return 1;
        }
    }

#if INPROC_ANDROID
    private static void WriteArtifactArchive(string archivePath, string? outputDirectory = null)
    {
        using ZipArchive archive = new ZipArchive(File.Create(archivePath), ZipArchiveMode.Create);
        if (outputDirectory is not null)
        {
            foreach (string path in Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories))
            {
                string entryName = Path.GetRelativePath(outputDirectory, path).Replace(Path.DirectorySeparatorChar, '/');
                archive.CreateEntryFromFile(path, entryName);
            }
        }
    }
#endif

    private static void RunScenario(string outputDirectory)
    {
        string consolePath = Path.Combine(outputDirectory, "console.txt");
        Stopwatch timer = Stopwatch.StartNew();
        int result = InProcCrashReportTest_DriveScenario(ScenarioId, outputDirectory, consolePath);
        Console.WriteLine($"Native scenario {ScenarioId} returned {result} in {timer.ElapsedMilliseconds} ms");
        Check(result == 0, "native driver failed; see its diagnostics");

        string reportDirectory = Path.Combine(outputDirectory, ".dotnet", "crash-reports");
#if INPROC_SCENARIO_CONSOLEONLY
        Check(!Directory.Exists(reportDirectory), "disabled lifecycle unexpectedly created a report directory");
#else
        string[] reports = Directory.GetFiles(reportDirectory);
        Check(reports.Length == 1 && reports[0].EndsWith(".crashreport.json", StringComparison.Ordinal),
            $"expected one completed report and no temporary files, found: {string.Join(", ", reports)}");
        ValidateJson(reports[0], ScenarioId);
#endif
        ValidateConsole(consolePath, ScenarioId);
    }

#if INPROC_SCENARIO_ONDEMAND
    private static void RunOnDemand(string outputDirectory)
    {
        string firstJson = Path.Combine(outputDirectory, "first.json");
        string secondJson = Path.Combine(outputDirectory, "second.json");
        string firstLog = Path.Combine(outputDirectory, "first.log");
        string secondLog = Path.Combine(outputDirectory, "second.log");
        Stopwatch timer = Stopwatch.StartNew();
        int result = InProcCrashReportTest_DriveOnDemand(outputDirectory, firstJson, secondJson, firstLog, secondLog);
        Console.WriteLine($"Native on-demand driver returned {result} in {timer.ElapsedMilliseconds} ms");
        Check(result == 0, "native on-demand driver failed; see its diagnostics");

        // Both requests use the rich fixture; the changed signal detects stale output.
        ValidateJson(firstJson, 0);
        ValidateConsole(firstLog, 0);
        ValidateJson(secondJson, 0, signal: "6");
        ValidateConsole(secondLog, 0, signal: "6 (SIGABRT)");

        foreach (ReportFormat format in new[] { ReportFormat.Json, ReportFormat.Log })
        {
            RunConcurrentOnDemand(outputDirectory, format, failOwner: false);
            RunConcurrentOnDemand(outputDirectory, format, failOwner: true);
        }

        string reportDirectory = Path.Combine(outputDirectory, ".dotnet", "crash-reports");
        Check(Directory.Exists(reportDirectory), "native driver did not initialize lifecycle services");
        Check(!Directory.EnumerateFileSystemEntries(reportDirectory).Any(),
            "on-demand requests unexpectedly changed the lifecycle report directory");

        // The signal path retains the guard, so no later request can generate a report.
        RunSignalOwnedContention(outputDirectory);
    }

    private static void RunConcurrentOnDemand(string outputDirectory, ReportFormat ownerFormat, bool failOwner)
    {
        const int OwnerSignal = 11;
        const int OtherSignal = 6;
        string caseName = $"concurrent-{ownerFormat}-{(failOwner ? "failure" : "success")}";
        string caseDirectory = Directory.CreateDirectory(Path.Combine(outputDirectory, caseName)).FullName;
        string ownerPath = Path.Combine(caseDirectory, $"owner.{ownerFormat}");
        Console.WriteLine($"Starting {caseName}: hold an on-demand owner against on-demand and signal requests");

        RunWithBlockedOwner(caseName,
            callback => StartOnDemandRequest(ownerFormat, OwnerSignal, ownerPath, callback),
            contenders =>
            {
                CheckRejectedOnDemandRequests(caseDirectory, contenders);

                string signalPath = Path.Combine(caseDirectory, "signal.log");
                int signalEnumerations = 0;
                Task<int> signal = StartSignalRequest(signalPath, () =>
                {
                    Interlocked.Increment(ref signalEnumerations);
                    return 1;
                });
                contenders.Add(signal);
                Check(signal.Wait(s_concurrencyTimeout), $"{caseName}: signal request did not return while on-demand owner was held");
                Check(signal.Result == 1, $"{caseName}: signal capture failed");
                Check(signalEnumerations == 0, $"{caseName}: rejected signal request enumerated threads");
                Check(new FileInfo(signalPath).Length == 0, $"{caseName}: rejected signal request wrote compact output");
                Check(!Directory.EnumerateFileSystemEntries(Path.Combine(outputDirectory, ".dotnet", "crash-reports")).Any(),
                    $"{caseName}: rejected signal request created lifecycle output");
            }, failOwner);

        if (failOwner)
        {
            Check(new FileInfo(ownerPath).Length == 0, $"{caseName}: failed owner wrote output");
        }
        else
        {
            ValidateOnDemandOutput(ownerPath, ownerFormat, OwnerSignal);
        }

        foreach (ReportFormat format in new[] { ReportFormat.Json, ReportFormat.Log })
        {
            string recoveryPath = Path.Combine(caseDirectory, $"recovery.{format}");
            int result = InProcCrashReportTest_CreateOnDemandReport(format, OtherSignal, recoveryPath, static () => 1);
            Check(result == 1, $"{caseName}: subsequent {format} request returned {result}");
            ValidateOnDemandOutput(recoveryPath, format, OtherSignal);
        }

        Console.WriteLine($"PASS: {caseName}; on-demand and signal contenders rejected without output, owner and recovery verified");
    }

    private static void RunSignalOwnedContention(string outputDirectory)
    {
        const string CaseName = "concurrent-signal-owner";
        string caseDirectory = Directory.CreateDirectory(Path.Combine(outputDirectory, CaseName)).FullName;
        string consolePath = Path.Combine(caseDirectory, "owner.log");
        Console.WriteLine($"Starting {CaseName}: hold signal report enumeration against JSON and log requests");
        RunWithBlockedOwner(CaseName,
            callback => StartSignalRequest(consolePath, callback),
            contenders => CheckRejectedOnDemandRequests(caseDirectory, contenders),
            failOwner: false);

        string[] reports = Directory.GetFiles(Path.Combine(outputDirectory, ".dotnet", "crash-reports"));
        Check(reports.Length == 1 && reports[0].EndsWith(".crashreport.json", StringComparison.Ordinal),
            $"{CaseName}: expected one completed signal report and no temporary files, found: {string.Join(", ", reports)}");
        ValidateJson(reports[0], 0);
        ValidateConsole(consolePath, 0);

        string afterDirectory = Directory.CreateDirectory(Path.Combine(caseDirectory, "after-completion")).FullName;
        CheckRejectedOnDemandRequests(afterDirectory, new List<Task<int>>());
        Console.WriteLine($"PASS: {CaseName}; both formats rejected during and after signal reporting, signal output verified");
    }

    private static void RunWithBlockedOwner(
        string caseName, Func<ReportCallback, Task<int>> startOwner,
        Action<List<Task<int>>> checkContenders, bool failOwner)
    {
        TaskCompletionSource ownerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseOwner = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int ownerCallbacks = 0;
        int ownerTimedOut = 0;
        Task<int> owner = startOwner(() =>
        {
            if (Interlocked.Increment(ref ownerCallbacks) == 1)
            {
                // Keep the reporter's guard occupied until all contenders have returned.
                ownerEntered.SetResult();
                if (!releaseOwner.Task.Wait(s_concurrencyTimeout))
                {
                    Interlocked.Exchange(ref ownerTimedOut, 1);
                    return 0;
                }
            }

            return failOwner ? 0 : 1;
        });

        List<Task<int>> contenders = new();
        try
        {
            Check(ownerEntered.Task.Wait(s_concurrencyTimeout), $"{caseName}: owner never entered its callback");
            checkContenders(contenders);
            Check(!owner.IsCompleted, $"{caseName}: owner completed before being released");
        }
        finally
        {
            releaseOwner.TrySetResult();
            Check(Task.WhenAll(contenders.Append(owner)).Wait(s_concurrencyTimeout),
                $"{caseName}: reporting tasks did not finish after releasing the owner");
        }

        Check(ownerTimedOut == 0, $"{caseName}: owner timed out waiting for release");
        Check(owner.Result == (failOwner ? 0 : 1), $"{caseName}: owner returned {owner.Result}");
        if (failOwner)
        {
            Check(ownerCallbacks == 1, $"{caseName}: failed owner sink was invoked {ownerCallbacks} times");
        }
    }

    private static void CheckRejectedOnDemandRequests(string caseDirectory, List<Task<int>> contenders)
    {
        ReportFormat[] formats = [ReportFormat.Json, ReportFormat.Log];
        int[] writes = new int[formats.Length];
        Task<int>[] requests = new Task<int>[formats.Length];
        for (int i = 0; i < formats.Length; i++)
        {
            int index = i;
            requests[index] = StartOnDemandRequest(formats[index], 6,
                Path.Combine(caseDirectory, $"contender.{formats[index]}"), () =>
                {
                    Interlocked.Increment(ref writes[index]);
                    return 0;
                });
            contenders.Add(requests[index]);
        }

        Check(Task.WhenAll(requests).Wait(s_concurrencyTimeout),
            $"{caseDirectory}: on-demand contenders did not reject the occupied guard");
        for (int i = 0; i < requests.Length; i++)
        {
            Check(requests[i].Result == 0, $"{caseDirectory}: {formats[i]} contender returned {requests[i].Result}, expected rejection");
            Check(writes[i] == 0, $"{caseDirectory}: rejected {formats[i]} contender invoked its output callback");
            Check(new FileInfo(Path.Combine(caseDirectory, $"contender.{formats[i]}")).Length == 0,
                $"{caseDirectory}: rejected {formats[i]} contender wrote output");
        }
    }

    private static Task<int> StartOnDemandRequest(ReportFormat format, int signal, string path, ReportCallback beforeWrite) =>
        Task.Factory.StartNew(() => InProcCrashReportTest_CreateOnDemandReport(format, signal, path, beforeWrite),
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static Task<int> StartSignalRequest(string consolePath, ReportCallback beforeEnumerate) =>
        Task.Factory.StartNew(() => InProcCrashReportTest_CreateSignalReport(consolePath, beforeEnumerate),
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static void ValidateOnDemandOutput(string path, ReportFormat format, int signal)
    {
        if (format == ReportFormat.Json)
        {
            ValidateJson(path, 0, signal.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            ValidateConsole(path, 0, signal == 6 ? "6 (SIGABRT)" : "11 (SIGSEGV)");
        }
    }
#endif

    private static void ValidateJson(string path, int scenario, string? signal = null)
    {
        Console.WriteLine($"Validating JSON: {Path.GetFileName(path)}");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        JsonElement payload = root.GetProperty("payload");
        CheckString(payload, "protocol_version", "1.0.0");
        CheckString(payload.GetProperty("configuration"), "architecture", GetArchitecture());
        CheckString(payload, "pid", Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        Check(!string.IsNullOrEmpty(payload.GetProperty("process_name").GetString()), "missing process name");
        CheckString(root.GetProperty("parameters"), "signal", signal ?? (scenario == 1 ? "6" : "11"));

        JsonElement threads = GetArray(payload, "threads", scenario switch { 1 => 2, 2 => 1, _ => 3 });
        ulong crashTid = ReadHex(threads[0], "native_thread_id");
        Check(crashTid != 0, "crashing thread ID is zero");
        for (int i = 0; i < threads.GetArrayLength(); i++)
        {
            CheckString(threads[i], "crashed", i == 0 ? "true" : "false");
            CheckHex(threads[i], "native_thread_id", crashTid + (ulong)i);
            if (i != 0 || scenario == 1)
            {
                CheckAbsent(threads[i], "managed_exception_type");
                CheckAbsent(threads[i], "managed_exception_hresult");
            }
        }

        switch (scenario)
        {
            case 1:
                ValidateAbortJson(threads);
                break;
            case 2:
                ValidateStackOverflowJson(threads[0]);
                break;
            default:
                ValidateRichJson(threads);
                break;
        }
    }

    private static void ValidateRichJson(JsonElement threads)
    {
        JsonElement crashed = threads[0];
        CheckString(crashed, "managed_exception_type", "System.NullReferenceException");
        CheckString(crashed, "managed_exception_hresult", "0x80004003");
        CheckRegisters(crashed);
        JsonElement frames = GetArray(crashed, "stack_frames", 5);
        CheckContextFrame(frames[0]);
        CheckManagedFrame(frames[1], 0x40aaaa, "Synthetic.App.Worker`1[System.Int32].DoWork", 0x06000001);
        CheckNativeFrame(frames[2], 0x40bbbb, "libsynthetic.so");
        CheckManagedFrame(frames[3], 0x40cccc, "Synthetic.App.Dictionary`2[System.String,System.Int32].Insert", 0x06000002);
        CheckNativeFrame(frames[4], 0x40dddd, "libnative2.so");
        CheckManagedFrame(GetArray(threads[1], "stack_frames", 1)[0], 0x40eeee, "Synthetic.App.Server.Listen", 0x06000003);
        CheckNativeFrame(GetArray(threads[2], "stack_frames", 1)[0], 0x40ffff, "libsynthetic.so");
    }

    private static void ValidateAbortJson(JsonElement threads)
    {
        CheckRegisters(threads[0]);
        JsonElement frames = GetArray(threads[0], "stack_frames", 3);
        CheckContextFrame(frames[0]);
        CheckNativeFrame(frames[1], 0x40aaaa, "libsynthetic.so");
        CheckNativeFrame(frames[2], 0x40bbbb, "libnative2.so");
        CheckManagedFrame(GetArray(threads[1], "stack_frames", 1)[0], 0x40cccc, "Synthetic.App.Server.Listen", 0x06000001);
    }

    private static void ValidateStackOverflowJson(JsonElement crashed)
    {
        CheckString(crashed, "is_managed", "true");
        CheckString(crashed, "managed_exception_type", "System.StackOverflowException");
        CheckString(crashed, "managed_exception_hresult", "0x800703e9");
        CheckString(crashed, "stack_overflow_total_frames", "42");
        CheckAbsent(crashed, "stack_frames_unavailable_reason");
        CheckAbsent(crashed, "stack_overflow_trace_truncated_frames");
        JsonElement frames = GetArray(crashed, "stack_frames", 3);
        CheckString(frames[0], "method_name", "Synthetic.App.Program.Main");
        CheckString(frames[1], "method_name", "Synthetic.App.Recurse.Down");
        CheckString(frames[1], "stack_overflow_repeat_count", "40");
        CheckString(frames[1], "stack_overflow_repeat_sequence_length", "1");
        CheckString(frames[2], "method_name", "Synthetic.App.Recurse.Bottom");
        foreach (JsonElement frame in frames.EnumerateArray())
        {
            CheckString(frame, "is_managed", "true");
        }
        foreach (int index in new[] { 0, 2 })
        {
            CheckAbsent(frames[index], "stack_overflow_repeat_count");
            CheckAbsent(frames[index], "stack_overflow_repeat_sequence_length");
        }
    }

    private static void ValidateConsole(string path, int scenario, string? signal = null)
    {
        Console.WriteLine($"Validating compact report: {Path.GetFileName(path)}");
        string console = File.ReadAllText(path);
        string[] lines = console.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
        Check(lines.Length != 0 && lines[0] == Separator && lines[^1] == Separator, "missing report delimiters");
        Check(lines.Count(line => line == ".NET Crash Report v1.0.0") == 1, "expected one protocol header");
        Check(lines.Contains($"ABI: {GetArchitecture()}"), "incorrect ABI");
        Check(lines.Count(line => line.StartsWith("signal ", StringComparison.Ordinal)) == 1, "expected one signal line");
        Check(lines.Contains($"signal {signal ?? (scenario == 1 ? "6 (SIGABRT)" : "11 (SIGSEGV)")}"), "incorrect signal");
        Check(!lines.Any(line => line == "(no managed frames)" || line.StartsWith("... +", StringComparison.Ordinal)),
            "compact report unexpectedly omitted frames");

        string[][] expectedThreads = scenario switch
        {
            1 =>
            [
                ["#00 [0] 0x40aaaa (libsynthetic.so + 0x40)", "#01 [1] 0x40bbbb (libnative2.so + 0x40)"],
                ["#00 [2] Synthetic.App.Server.Listen + 0x10 (token=0x6000001)"],
            ],
            2 =>
            [
                [
                    "managed exception: System.StackOverflowException (0x800703e9)",
                    "stack overflow frames: 42",
                    "#00 Synthetic.App.Program.Main",
                    "repeated 40 times:",
                    "#01 Synthetic.App.Recurse.Down",
                    "#02 Synthetic.App.Recurse.Bottom",
                ],
            ],
            _ =>
            [
                [
                    "managed exception: System.NullReferenceException (0x80004003)",
                    "#00 [0] Synthetic.App.Worker`1[System.Int32].DoWork + 0x10 (token=0x6000001)",
                    "#01 [1] 0x40bbbb (libsynthetic.so + 0x40)",
                    "#02 [0] Synthetic.App.Dictionary`2[System.String,System.Int32].Insert + 0x10 (token=0x6000002)",
                    "#03 [2] 0x40dddd (libnative2.so + 0x40)",
                ],
                ["#00 [0] Synthetic.App.Server.Listen + 0x10 (token=0x6000003)"],
                ["#00 [1] 0x40ffff (libsynthetic.so + 0x40)"],
            ],
        };

        string[] blocks = console.Split("--- thread ", StringSplitOptions.None);
        Check(blocks.Length == expectedThreads.Length + 1, $"expected {expectedThreads.Length} console threads, got {blocks.Length - 1}");
        for (int i = 0; i < expectedThreads.Length; i++)
        {
            string[] blockLines = blocks[i + 1].Split('\n').Select(line => line.Trim()).ToArray();
            Check(blockLines[0].Contains("(crashed)", StringComparison.Ordinal) == (i == 0), $"thread {i}: incorrect crashed marker");
            string[] actual = blockLines.Skip(1).Where(line =>
                line.StartsWith('#') || line.StartsWith("managed exception:", StringComparison.Ordinal) ||
                line.StartsWith("stack overflow ", StringComparison.Ordinal) || line.StartsWith("repeated ", StringComparison.Ordinal)).ToArray();
            Check(actual.SequenceEqual(expectedThreads[i]), $"thread {i}: expected compact lines:\n{string.Join("\n", expectedThreads[i])}\nactual:\n{string.Join("\n", actual)}");
        }

        string[] modules = scenario switch
        {
            1 => ["libsynthetic.so", "libnative2.so", "synthetic.managed.dll"],
            2 => [],
            _ => ["synthetic.managed.dll", "libsynthetic.so", "libnative2.so"],
        };
        string[] actualModules = lines.Where(line => line.StartsWith('[')).ToArray();
        string[] expectedModules = modules.Select((module, index) => $"[{index}] {module} {ModuleGuid}").ToArray();
        Check(actualModules.SequenceEqual(expectedModules), "compact module table mismatch");
        Check(lines.Count(line => line == "modules:") == (modules.Length == 0 ? 0 : 1), "incorrect module table header count");
    }

    private static void CheckRegisters(JsonElement thread)
    {
        JsonElement context = thread.GetProperty("ctx");
        CheckHex(context, "IP", 0x40aaaa);
        CheckHex(context, "SP", IntPtr.Size == 8 ? 0x7fff0000aaaaUL : 0x7fffaaaaUL);
        CheckHex(context, "BP", IntPtr.Size == 8 ? 0x7fff0000aab0UL : 0x7fffaab0UL);
    }

    private static void CheckContextFrame(JsonElement frame)
    {
        CheckString(frame, "is_managed", "false");
        CheckHex(frame, "native_address", 0x40aaaa);
        CheckHex(frame, "stack_pointer", IntPtr.Size == 8 ? 0x7fff0000aaaaUL : 0x7fffaaaaUL);
    }

    private static void CheckManagedFrame(JsonElement frame, ulong ip, string method, uint token)
    {
        CheckString(frame, "is_managed", "true");
        CheckString(frame, "method_name", method);
        CheckString(frame, "filename", "synthetic.managed.dll");
        CheckString(frame, "guid", ModuleGuid);
        CheckHex(frame, "native_address", ip);
        CheckHex(frame, "stack_pointer", ip + 0x1000);
        CheckHex(frame, "native_offset", 0x20);
        CheckHex(frame, "token", token);
        CheckHex(frame, "il_offset", 0x10);
        CheckHex(frame, "timestamp", 0x600dcafe);
        CheckHex(frame, "sizeofimage", 0x10000);
    }

    private static void CheckNativeFrame(JsonElement frame, ulong ip, string module)
    {
        CheckString(frame, "is_managed", "false");
        CheckString(frame, "native_module", module);
        CheckHex(frame, "native_address", ip);
        CheckHex(frame, "stack_pointer", ip + 0x1000);
        CheckHex(frame, "native_offset", 0x40);
        CheckAbsent(frame, "method_name");
        CheckAbsent(frame, "token");
    }

    private static JsonElement GetArray(JsonElement parent, string property, int count)
    {
        JsonElement array = parent.GetProperty(property);
        Check(array.GetArrayLength() == count, $"{property}: expected {count} entries, actual {array.GetArrayLength()}");
        return array;
    }

    private static string GetArchitecture() =>
        RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "amd64" : RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

    private static ulong ReadHex(JsonElement parent, string property) =>
        ulong.Parse(parent.GetProperty(property).GetString()!.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

    private static void CheckHex(JsonElement parent, string property, ulong expected) =>
        CheckString(parent, property, $"0x{expected:x}");

    private static void CheckString(JsonElement parent, string property, string expected)
    {
        string? actual = parent.GetProperty(property).GetString();
        Check(actual == expected, $"{property}: expected '{expected}', actual '{actual}'");
    }

    private static void CheckAbsent(JsonElement parent, string property) =>
        Check(!parent.TryGetProperty(property, out _), $"unexpected property '{property}'");

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void DumpOutputs(string outputDirectory)
    {
        try
        {
            char[] buffer = new char[16 * 1024];
            foreach (string path in Directory.EnumerateFiles(outputDirectory, "*", SearchOption.AllDirectories).Take(8))
            {
                Console.WriteLine($"--- {path} ---");
                using StreamReader reader = File.OpenText(path);
                int length = reader.ReadBlock(buffer, 0, buffer.Length);
                Console.WriteLine(buffer.AsSpan(0, length));
                if (!reader.EndOfStream)
                {
                    Console.WriteLine("[diagnostic output truncated]");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Could not read diagnostic files: {ex}");
        }
    }
}
