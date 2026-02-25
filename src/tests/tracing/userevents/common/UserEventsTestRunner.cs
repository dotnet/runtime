// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int DefaultTraceeDelayToSetupTracepointsMs = 200;

        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        private static extern int Kill(int pid, int sig);

        public static int Run(
            string[] args,
            string scenarioName,
            Action traceeAction,
            Func<EventPipeEventSource, bool> traceValidator,
            int traceeExitTimeout = DefaultTraceeExitTimeoutMs,
            int recordTraceExitTimeout = DefaultRecordTraceExitTimeoutMs,
            int traceeDelayToSetupTracepoints = DefaultTraceeDelayToSetupTracepointsMs)
        {
            if (args.Length > 0 && args[0].Equals("tracee", StringComparison.OrdinalIgnoreCase))
            {
                if (traceeDelayToSetupTracepoints > 0)
                {
                    Thread.Sleep(traceeDelayToSetupTracepoints);
                }

                traceeAction();
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
            Func<EventPipeEventSource, bool> traceValidator,
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

            ProcessStartInfo recordTraceStartInfo = new();
            recordTraceStartInfo.FileName = "sudo";
            recordTraceStartInfo.ArgumentList.Add("-n");
            recordTraceStartInfo.ArgumentList.Add(recordTracePath);
            recordTraceStartInfo.ArgumentList.Add("--script-file");
            recordTraceStartInfo.ArgumentList.Add(scriptFilePath);
            recordTraceStartInfo.ArgumentList.Add("--out");
            recordTraceStartInfo.ArgumentList.Add(traceFilePath);
            recordTraceStartInfo.WorkingDirectory = userEventsScenarioDir;
            recordTraceStartInfo.UseShellExecute = false;
            recordTraceStartInfo.RedirectStandardOutput = true;
            recordTraceStartInfo.RedirectStandardError = true;

            Console.WriteLine($"Starting record-trace: {recordTraceStartInfo.FileName} {string.Join(" ", recordTraceStartInfo.ArgumentList)}");
            using Process recordTraceProcess = Process.Start(recordTraceStartInfo);
            recordTraceProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine($"[record-trace] {args.Data}");
                }
            };
            recordTraceProcess.BeginOutputReadLine();
            recordTraceProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.Error.WriteLine($"[record-trace] {args.Data}");
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

            Console.WriteLine($"Starting tracee process: {traceeStartInfo.FileName} {string.Join(" ", traceeStartInfo.ArgumentList)}");
            using Process traceeProcess = Process.Start(traceeStartInfo);
            int traceePid = traceeProcess.Id;
            Console.WriteLine($"Tracee process started with PID: {traceePid}");
            traceeProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.WriteLine($"[tracee] {args.Data}");
                }
            };
            traceeProcess.BeginOutputReadLine();
            traceeProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Console.Error.WriteLine($"[tracee] {args.Data}");
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
                return -1;
            }

            using EventPipeEventSource source = new EventPipeEventSource(traceFilePath);
            if (!traceValidator(source))
            {
                Console.Error.WriteLine($"Trace file `{traceFilePath}` does not contain expected events.");
                UploadTraceFileFromHelix(traceFilePath, scenarioName);
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
                        Console.WriteLine($"Deleting zombie diagnostic port: {fi.FullName}");
                        fi.Delete();
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
                            Console.WriteLine($"Deleting duplicate diagnostic port: {fi.FullName}");
                            fi.Delete();
                        }
                    }
                }
            }
        }

        private static void UploadTraceFileFromHelix(string traceFilePath, string scenarioName)
        {
            var helixWorkItemDirectory = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (helixWorkItemDirectory != null && Directory.Exists(helixWorkItemDirectory))
            {
                var destPath = Path.Combine(helixWorkItemDirectory, $"{scenarioName}.nettrace");
                Console.WriteLine($"Uploading trace file to Helix work item directory: {destPath}");
                File.Copy(traceFilePath, destPath, overwrite: true);
            }
        }
    }
}