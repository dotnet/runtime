// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

namespace Tracing.Tests.UserEvents
{
    public class UserEventsTest
    {
        private const int SIGINT = 2;

        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        private static extern int Kill(int pid, int sig);

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "tracee")
            {
                UserEventsTracee.Run();
                return 0;
            }

            return TestEntryPoint();
        }

        public static int TestEntryPoint()
        {
            string appBaseDir = AppContext.BaseDirectory;
            string recordTracePath = Path.Combine(appBaseDir, "record-trace");
            string scriptFilePath = Path.Combine(appBaseDir, "dotnet-common.script");
            const string userEventsDataPath = "/sys/kernel/tracing/user_events_data";

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
                Console.Error.WriteLine($"dotnet-common.script not found at `{scriptFilePath}`. Test cannot run.");
                return -1;
            }

            string traceFilePath = Path.GetTempFileName();
            File.Delete(traceFilePath); // record-trace requires the output file to not exist
            traceFilePath = Path.ChangeExtension(traceFilePath, ".nettrace");

            ProcessStartInfo recordTraceStartInfo = new();
            recordTraceStartInfo.FileName = "sudo";
            recordTraceStartInfo.Arguments = $"-n {recordTracePath} --script-file {scriptFilePath} --out {traceFilePath}";
            recordTraceStartInfo.WorkingDirectory = appBaseDir;
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
            traceeStartInfo.Arguments = $"{typeof(UserEventsTest).Assembly.Location} tracee";
            traceeStartInfo.WorkingDirectory = appBaseDir;
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
            if (!traceeProcess.HasExited && !traceeProcess.WaitForExit(5000))
            {
                Console.WriteLine($"Tracee process did not exit within the 5s timeout, killing it.");
                traceeProcess.Kill();
            }
            traceeProcess.WaitForExit(); // flush async output

            // Since TMPDIR was configured, the diagnostic port was created outside of helix's default temp datadisk path.
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
                if (!recordTraceProcess.WaitForExit(20000))
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

            if (!ValidateTraceeEvents(traceFilePath))
            {
                Console.Error.WriteLine($"Trace file `{traceFilePath}` does not contain expected events.");
                UploadTraceFile(traceFilePath);
                return -1;
            }

            return 100;
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

        private static bool ValidateTraceeEvents(string traceFilePath)
        {
            using EventPipeEventSource source = new EventPipeEventSource(traceFilePath);
            bool allocationSampledEventFound = false;

            // TraceEvent's ClrTraceEventParser does not know about the AllocationSampled Event, so it shows up as "Unknown(303)"
            source.Dynamic.All += (TraceEvent e) =>
            {
                if (e.ProviderName == "Microsoft-Windows-DotNETRuntime")
                {
                    if (e.EventName == "AllocationSampled" || (e.ID == (TraceEventID)303 && e.EventName.StartsWith("Unknown")))
                    {
                        allocationSampledEventFound = true;
                    }
                }
            };

            source.Process();
            return allocationSampledEventFound;
        }

        private static void UploadTraceFile(string traceFilePath)
        {
            var helixWorkItemDirectory = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (helixWorkItemDirectory != null && Directory.Exists(helixWorkItemDirectory))
            {
                var destPath = Path.Combine(helixWorkItemDirectory, Path.GetFileName(traceFilePath));
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
