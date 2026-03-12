// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.UserEvents.Tests.Common
{
    public class UserEventsTestRunner
    {
        private const int SIGINT = 2;
        private const int DefaultTraceeExitTimeoutMs = 5000;
        private const int DefaultRecordTraceExitTimeoutMs = 20000;

        // Timeout for record-trace to emit "Recording started" on stdout. record-trace's
        // startup has two phases: a /proc scan for existing processes, then enabling ring
        // buffers to capture live mmap events. If the tracee starts during the /proc scan,
        // record-trace may discover it and send IPC before ring buffers are active, causing
        // emitted events to be lost. If the tracee starts after the /proc scan but before
        // ring buffers are enabled, its mmap events are missed entirely and record-trace
        // never discovers it. By gating on "Recording started" (printed after enable), the
        // tracee is only discovered via live mmap events with ring buffers already active.
        // record-trace startup -> enable scales with system process count: averaged 113ms at
        // 126 processes and 253ms at 534 processes on a 2-core x64 system, but took ~1845ms
        // on a 2-core ARM64 CI machine.
        private const int RecordTraceSetupTimeoutMs = 10000;

        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        private static extern int Kill(int pid, int sig);

        public static int Run(
            string[] args,
            string scenarioName,
            Action traceeAction,
            Func<int, EventPipeEventSource, bool> traceValidator,
            EventSource traceeEventSource,
            int traceeExitTimeout = DefaultTraceeExitTimeoutMs,
            int recordTraceExitTimeout = DefaultRecordTraceExitTimeoutMs)
        {
            if (args.Length > 0 && args[0].Equals("tracee", StringComparison.OrdinalIgnoreCase))
            {
                using var enabledEvent = new ManualResetEventSlim(false);

                traceeEventSource.EventCommandExecuted += (sender, e) =>
                {
                    if (e.Command == EventCommand.Enable)
                    {
                        enabledEvent.Set();
                    }
                };

                if (traceeEventSource.IsEnabled())
                {
                    enabledEvent.Set();
                }

                Console.WriteLine("Tracee waiting for EventSource to be enabled via IPC...");
                enabledEvent.Wait();
                Console.WriteLine("Tracee EventSource enabled, emitting events.");

                traceeAction();
                Console.WriteLine("Tracee finished emitting events.");
                return 0;
            }

            return RunOrchestrator(
                scenarioName,
                traceValidator,
                traceeExitTimeout,
                recordTraceExitTimeout);
        }

        private static int RunOrchestrator(
            string scenarioName,
            Func<int, EventPipeEventSource, bool> traceValidator,
            int traceeExitTimeout,
            int recordTraceExitTimeout)
        {
            // Start with AppContext.BaseDirectory and determine if we're running NativeAOT
            string baseDir = AppContext.BaseDirectory;
            bool isNativeAot = false;
            string userEventsScenarioDir = baseDir;

            if (Path.GetFileName(baseDir.TrimEnd(Path.DirectorySeparatorChar)) == "native")
            {
                // NativeAOT places its compiled test executables under a 'native' subdirectory.
                isNativeAot = true;
                userEventsScenarioDir = Path.GetFullPath(Path.Combine(baseDir, ".."));
            }

            if (Path.GetFileName(userEventsScenarioDir.TrimEnd(Path.DirectorySeparatorChar)) != scenarioName)
            {
                Console.Error.WriteLine($"Could not resolve the userevents test scenario directory. Expected directory to end with '{scenarioName}', but got: {userEventsScenarioDir}");
                return -1;
            }

            string recordTracePath = ResolveRecordTracePath(userEventsScenarioDir);
            string scriptFilePath = Path.Combine(userEventsScenarioDir, $"{scenarioName}.script");

            if (!UserEventsRequirements.IsSupported())
            {
                Console.WriteLine("Skipping test: environment does not support user events.");
                return 100;
            }
            if (!File.Exists(recordTracePath))
            {
                Console.Error.WriteLine($"record-trace not found at `{recordTracePath}`. Test cannot run.");
                return -1;
            }
            if (!File.Exists(scriptFilePath))
            {
                Console.Error.WriteLine($"record-trace script-file not found at `{scriptFilePath}`. Test cannot run.");
                return -1;
            }

            string traceFilePath = Path.GetTempFileName();
            // In the past, it's been observed that record-trace has trouble overwriting newly created temp files 
            // in `/tmp` (e.g. mktemp or Path.GetTempFileName). It's suspected to be a permissions issue.
            // As a workaround, deleting the temp file and allowing record-trace to create it works reliably.
            File.Delete(traceFilePath);
            traceFilePath = Path.ChangeExtension(traceFilePath, ".nettrace");
            string recordTraceLogPath = Path.ChangeExtension(traceFilePath, ".log");

            ProcessStartInfo recordTraceStartInfo = new();
            recordTraceStartInfo.FileName = "sudo";
            recordTraceStartInfo.ArgumentList.Add("-n");
            recordTraceStartInfo.ArgumentList.Add(recordTracePath);
            recordTraceStartInfo.ArgumentList.Add("--script-file");
            recordTraceStartInfo.ArgumentList.Add(scriptFilePath);
            recordTraceStartInfo.ArgumentList.Add("--out");
            recordTraceStartInfo.ArgumentList.Add(traceFilePath);
            recordTraceStartInfo.ArgumentList.Add("--log-path");
            recordTraceStartInfo.ArgumentList.Add(recordTraceLogPath);
            recordTraceStartInfo.ArgumentList.Add("--log-filter");
            recordTraceStartInfo.ArgumentList.Add(
                "one_collect::helpers::dotnet=debug," +
                "one_collect::perf_event=debug," +
                "one_collect::perf_event::rb=info," +
                "one_collect::helpers::exporting::formats::nettrace=debug," +
                "one_collect::helpers::exporting::os=warn," +
                "ruwind=warn," +
                "one_collect::tracefs=warn," +
                "one_collect::scripting=warn," +
                "engine=warn");
            recordTraceStartInfo.WorkingDirectory = userEventsScenarioDir;
            recordTraceStartInfo.UseShellExecute = false;
            recordTraceStartInfo.RedirectStandardOutput = true;
            recordTraceStartInfo.RedirectStandardError = true;

            Console.WriteLine($"Starting record-trace: {recordTraceStartInfo.FileName} {string.Join(" ", recordTraceStartInfo.ArgumentList)}");
            using Process recordTraceProcess = Process.Start(recordTraceStartInfo);
            using var recordingStarted = new ManualResetEventSlim(false);
            recordTraceProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine($"[record-trace][stdout] {args.Data}");
                    if (args.Data.Contains("Recording started", StringComparison.Ordinal))
                    {
                        recordingStarted.Set();
                    }
                }
            };
            recordTraceProcess.BeginOutputReadLine();
            recordTraceProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine($"[record-trace][stderr] {args.Data}");
                }
            };
            recordTraceProcess.BeginErrorReadLine();
            Console.WriteLine($"record-trace started with PID: {recordTraceProcess.Id}");

            ProcessStartInfo traceeStartInfo = new();
            traceeStartInfo.FileName = Environment.ProcessPath
                ?? throw new InvalidOperationException("Environment.ProcessPath is null");

            // NativeAOT tests run the native executable directly.
            // CoreCLR tests run through corerun which loads the managed assembly.
            if (isNativeAot)
            {
                traceeStartInfo.ArgumentList.Add("tracee");
            }
            else
            {
                // For CoreCLR, construct the path to the assembly
                string assemblyPath = Path.Combine(userEventsScenarioDir, $"{scenarioName}.dll");
                traceeStartInfo.ArgumentList.Add(assemblyPath);
                traceeStartInfo.ArgumentList.Add("tracee");
            }

            traceeStartInfo.WorkingDirectory = userEventsScenarioDir;
            traceeStartInfo.RedirectStandardOutput = true;
            traceeStartInfo.RedirectStandardError = true;

            // record-trace currently only searches /tmp/ for diagnostic ports https://github.com/microsoft/one-collect/issues/183
            string diagnosticPortDir = "/tmp";
            traceeStartInfo.Environment["TMPDIR"] = diagnosticPortDir;

            // TMPDIR is configured on Helix, but the diagnostic port is created outside of Helix's default temp datadisk path.
            // The diagnostic port should be automatically cleaned up when the tracee shuts down, but zombie sockets can be left
            // behind after catastrophic exits. Clean them before launching the tracee to avoid deleting sockets from a reused PID.
            // When https://github.com/microsoft/one-collect/issues/183 is fixed, this and the above TMPDIR should be removed.
            EnsureCleanDiagnosticPorts(diagnosticPortDir);

            // Wait for record-trace to finish setup (capture_environment + enable ring buffers).
            // "Recording started" is printed after session.enable() succeeds, which means
            // the ring buffers are active and will capture the tracee's mmap events.
            Console.WriteLine("Waiting for record-trace to signal 'Recording started'...");
            if (!recordingStarted.Wait(RecordTraceSetupTimeoutMs))
            {
                if (recordTraceProcess.HasExited)
                {
                    Console.Error.WriteLine($"record-trace exited prematurely with code {recordTraceProcess.ExitCode}.");
                }
                else
                {
                    Console.Error.WriteLine($"record-trace did not emit 'Recording started' within {RecordTraceSetupTimeoutMs}ms.");
                    recordTraceProcess.Kill();
                }

                UploadArtifactsFromHelixOnFailure(scenarioName, recordTraceLogPath);
                return -1;
            }

            Console.WriteLine($"Starting tracee process: {traceeStartInfo.FileName} {string.Join(" ", traceeStartInfo.ArgumentList)}");
            using Process traceeProcess = Process.Start(traceeStartInfo);
            int traceePid = traceeProcess.Id;
            Console.WriteLine($"Tracee process started with PID: {traceePid}");
            traceeProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine($"[tracee][stdout] {args.Data}");
                }
            };
            traceeProcess.BeginOutputReadLine();
            traceeProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine($"[tracee][stderr] {args.Data}");
                }
            };
            traceeProcess.BeginErrorReadLine();

            Console.WriteLine($"Waiting for tracee process to exit...");
            if (!traceeProcess.HasExited && !traceeProcess.WaitForExit(traceeExitTimeout))
            {
                Console.WriteLine($"Tracee process did not exit within the {traceeExitTimeout}ms timeout, killing it.");
                traceeProcess.Kill();
            }
            traceeProcess.WaitForExit(); // flush async output
            Console.WriteLine($"Tracee process exited with code {traceeProcess.ExitCode}.");

            if (!recordTraceProcess.HasExited)
            {
                // Until record-trace supports duration, the only way to stop it is to send SIGINT (ctrl+c)
                Console.WriteLine($"Stopping record-trace with SIGINT.");
                Kill(recordTraceProcess.Id, SIGINT);
                Console.WriteLine($"Waiting for record-trace to exit...");
                if (!recordTraceProcess.WaitForExit(recordTraceExitTimeout))
                {
                    // record-trace needs to stop gracefully to generate the trace file
                    Console.WriteLine($"record-trace did not exit within the {recordTraceExitTimeout}ms timeout, killing it.");
                    recordTraceProcess.Kill();
                }
            }
            else
            {
                Console.WriteLine($"record-trace unexpectedly exited without SIGINT with code {recordTraceProcess.ExitCode}.");
            }
            recordTraceProcess.WaitForExit(); // flush async output

            if (!File.Exists(traceFilePath))
            {
                Console.Error.WriteLine($"Expected trace file not found at `{traceFilePath}`");
                UploadArtifactsFromHelixOnFailure(scenarioName, recordTraceLogPath);
                return -1;
            }

            using EventPipeEventSource source = new EventPipeEventSource(traceFilePath);
            if (!traceValidator(traceePid, source))
            {
                Console.Error.WriteLine($"Trace file `{traceFilePath}` does not contain expected events.");
                DumpTraceeEvents(traceFilePath, traceePid);
                UploadArtifactsFromHelixOnFailure(scenarioName, traceFilePath, recordTraceLogPath);
                return -1;
            }

            return 100;
        }

        private static string ResolveRecordTracePath(string userEventsScenarioDir)
        {
            // userEventsScenarioDir is .../tracing/userevents/<scenario>/<scenario>/ for both CoreCLR and NativeAOT
            // Navigate up two directories to reach userevents root, then into common/userevents_common
            string usereventsRoot = Path.GetFullPath(Path.Combine(userEventsScenarioDir, "..", ".."));
            string commonDir = Path.Combine(usereventsRoot, "common", "userevents_common");
            string recordTracePath = Path.Combine(commonDir, "record-trace");
            return recordTracePath;
        }

        // Similar to IpcTraceTest.EnsureCleanEnvironment, but scoped to the provided diagnosticPortDir.
        // Check for zombie diagnostic IPC sockets left behind by previous runs and remove them.
        // If multiple sockets exist for a running PID, delete all but the newest.
        private static void EnsureCleanDiagnosticPorts(string diagnosticPortDir)
        {
            if (!Directory.Exists(diagnosticPortDir))
            {
                return;
            }

            Func<(IEnumerable<IGrouping<int,FileInfo>>, List<int>)> GetPidsAndSockets = () =>
            {
                IEnumerable<IGrouping<int,FileInfo>> currentIpcs = Directory.GetFiles(diagnosticPortDir, "dotnet-diagnostic*")
                    .Select(filename =>
                    {
                        var match = Regex.Match(filename, @"dotnet-diagnostic-(?<pid>\d+)");
                        if (match.Success && match.Groups["pid"].Success && !string.IsNullOrEmpty(match.Groups["pid"].Value))
                        {
                            return new { pid = int.Parse(match.Groups["pid"].Value), fileInfo = new FileInfo(filename) };
                        }
                        return null;
                    })
                    .Where(fileInfoGroup => fileInfoGroup is not null)
                    .GroupBy(fileInfos => fileInfos.pid, fileInfos => fileInfos.fileInfo);
                List<int> currentPids = System.Diagnostics.Process.GetProcesses().Select(pid => pid.Id).ToList();
                return (currentIpcs, currentPids);
            };

            var (currentIpcs, currentPids) = GetPidsAndSockets();

            foreach (var ipc in currentIpcs)
            {
                if (!currentPids.Contains(ipc.Key))
                {
                    foreach (FileInfo fi in ipc)
                    {
                        try
                        {
                            Console.WriteLine($"Deleting zombie diagnostic port: {fi.FullName}");
                            fi.Delete();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.WriteLine($"Skipping zombie diagnostic port (permission denied): {fi.FullName}");
                        }
                        catch (IOException)
                        {
                            Console.WriteLine($"Skipping zombie diagnostic port (I/O error): {fi.FullName}");
                        }
                    }
                }
                else
                {
                    if (ipc.Count() > 1)
                    {
                        // delete zombied pipes except newest which is owned
                        var duplicates = ipc.OrderBy(fileInfo => fileInfo.CreationTime.Ticks).SkipLast(1);
                        foreach (FileInfo fi in duplicates)
                        {
                            try
                            {
                                Console.WriteLine($"Deleting duplicate diagnostic port: {fi.FullName}");
                                fi.Delete();
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Console.WriteLine($"Skipping duplicate diagnostic port (permission denied): {fi.FullName}");
                            }
                            catch (IOException)
                            {
                                Console.WriteLine($"Skipping duplicate diagnostic port (I/O error): {fi.FullName}");
                            }
                        }
                    }
                }
            }
        }

        private static void UploadArtifactsFromHelixOnFailure(string scenarioName, params string[] filePaths)
        {
            var helixWorkItemDirectory = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (helixWorkItemDirectory is null || !Directory.Exists(helixWorkItemDirectory))
                return;

            foreach (string filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Artifact not found at `{filePath}`, skipping upload.");
                    continue;
                }

                string extension = Path.GetExtension(filePath);
                string destPath = Path.Combine(helixWorkItemDirectory, $"{scenarioName}{extension}");
                Console.WriteLine($"Uploading artifact to Helix work item directory: {destPath}");
                File.Copy(filePath, destPath, overwrite: true);
            }
        }

        private static void DumpTraceeEvents(string traceFilePath, int traceePid)
        {
            try
            {
                using EventPipeEventSource diagSource = new EventPipeEventSource(traceFilePath);
                int traceeEventCount = 0;
                var eventSummary = new Dictionary<string, int>();

                diagSource.Dynamic.All += (TraceEvent e) =>
                {
                    if (e.ProcessID != traceePid)
                        return;

                    traceeEventCount++;
                    string key = $"{e.ProviderName}/{e.EventName}";
                    eventSummary[key] = eventSummary.GetValueOrDefault(key) + 1;
                };

                diagSource.Process();

                Console.Error.WriteLine($"Tracee PID {traceePid} had {traceeEventCount} event(s) in the trace:");
                foreach (var (key, count) in eventSummary)
                {
                    Console.Error.WriteLine($"  {key}: {count}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to dump tracee events: {ex}");
            }
        }
    }
}