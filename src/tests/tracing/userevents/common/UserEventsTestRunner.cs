// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.UserEvents.Tests.Common
{
    public class UserEventsTestRunner
    {
        private const int SIGINT = 2;
        private const int DefaultTraceeExitTimeoutMs = 5000;
        private const int DefaultRecordTraceExitTimeoutMs = 20000;

        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        private static extern int Kill(int pid, int sig);

        public static int Run(
            string scenarioName,
            string traceeAssemblyPath,
            Func<EventPipeEventSource, bool> traceValidator,
            int traceeExitTimeout = DefaultTraceeExitTimeoutMs,
            int recordTraceExitTimeout = DefaultRecordTraceExitTimeoutMs)
        {
            string userEventsScenarioDir = Path.GetDirectoryName(traceeAssemblyPath);
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
            recordTraceStartInfo.Arguments = $"-n {recordTracePath} --script-file {scriptFilePath} --out {traceFilePath}";
            recordTraceStartInfo.WorkingDirectory = userEventsScenarioDir;
            recordTraceStartInfo.UseShellExecute = false;
            recordTraceStartInfo.RedirectStandardOutput = true;
            recordTraceStartInfo.RedirectStandardError = true;

            Console.WriteLine($"Starting record-trace: {recordTraceStartInfo.FileName} {recordTraceStartInfo.Arguments}");
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
            traceeStartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            traceeStartInfo.Arguments = $"{traceeAssemblyPath} tracee";
            traceeStartInfo.WorkingDirectory = userEventsScenarioDir;
            traceeStartInfo.RedirectStandardOutput = true;
            traceeStartInfo.RedirectStandardError = true;

            // record-trace currently only searches /tmp/ for diagnostic ports https://github.com/microsoft/one-collect/issues/183
            string diagnosticPortDir = "/tmp/";
            traceeStartInfo.Environment["TMPDIR"] = diagnosticPortDir;

            Console.WriteLine($"Starting tracee process: {traceeStartInfo.FileName} {traceeStartInfo.Arguments}");
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
                Console.WriteLine($"Tracee process did not exit within the 5s timeout, killing it.");
                traceeProcess.Kill();
            }
            traceeProcess.WaitForExit(); // flush async output

            // TMPDIR is configured on Helix, but the diagnostic port was created outside of helix's default temp datadisk path.
            // The diagnostic port should be automatically cleaned up when the tracee shutsdown, but just in case of an
            // abrupt exit, ensure cleanup to avoid leaving artifacts on helix machines.
            // When https://github.com/microsoft/one-collect/issues/183 is fixed, this and the above TMPDIR should be removed.
            CleanupTraceeDiagnosticPorts(diagnosticPortDir, traceePid);

            if (!recordTraceProcess.HasExited)
            {
                // Until record-trace supports duration, the only way to stop it is to send SIGINT (ctrl+c)
                Console.WriteLine($"Stopping record-trace with SIGINT.");
                Kill(recordTraceProcess.Id, SIGINT);
                Console.WriteLine($"Waiting for record-trace to exit...");
                if (!recordTraceProcess.WaitForExit(recordTraceExitTimeout))
                {
                    // record-trace needs to stop gracefully to generate the trace file
                    Console.WriteLine($"record-trace did not exit within the 20s timeout, killing it.");
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
                UploadTraceFile(traceFilePath, scenarioName);
                return -1;
            }

            return 100;
        }

        private static string ResolveRecordTracePath(string userEventsScenarioDir)
        {
            // scenario dir: .../tracing/userevents/<scenario>/<scenario>
            string usereventsRoot = Path.GetFullPath(Path.Combine(userEventsScenarioDir, "..", ".."));
            // common dir: .../tracing/userevents/common/userevents_common
            string commonDir = Path.Combine(usereventsRoot, "common", "userevents_common");
            string recordTracePath = Path.Combine(commonDir, "record-trace");
            return recordTracePath;
        }

        private static void CleanupTraceeDiagnosticPorts(string diagnosticPortDir, int traceePid)
        {
            try
            {
                string[] udsFiles = Directory.GetFiles(diagnosticPortDir, $"dotnet-diagnostic-{traceePid}-*-socket");
                foreach (string udsFile in udsFiles)
                {
                    Console.WriteLine($"Deleting tracee diagnostic port UDS file: {udsFile}");
                    File.Delete(udsFile);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to cleanup tracee diagnostic ports: {ex}");
            }
        }

        private static void UploadTraceFile(string traceFilePath, string scenarioName)
        {
            var helixWorkItemDirectory = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (helixWorkItemDirectory != null && Directory.Exists(helixWorkItemDirectory))
            {
                var destPath = Path.Combine(helixWorkItemDirectory, $"{scenarioName}.nettrace");
                Console.WriteLine($"Uploading trace file to Helix work item directory: {destPath}");
                File.Copy(traceFilePath, destPath, overwrite: true);
            }
            else
            {
                Console.WriteLine($"Helix work item directory not found. Trace file remains at: {traceFilePath}");
            }
        }
    }
}