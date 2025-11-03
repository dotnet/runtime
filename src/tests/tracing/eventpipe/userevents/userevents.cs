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
        private static readonly string trace = "trace.nettrace";
        private const int SIGINT = 2;

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);

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
            string traceFilePath = Path.Combine(appBaseDir, trace);

            if (!File.Exists(recordTracePath) || !File.Exists(scriptFilePath))
            {
                Console.WriteLine("record-trace or dotnet-common.script not found. Test cannot run.");
                return -1;
            }

            ProcessStartInfo traceeStartInfo = new();
            traceeStartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            traceeStartInfo.Arguments = $"{typeof(UserEventsTest).Assembly.Location} tracee";
            traceeStartInfo.WorkingDirectory = appBaseDir;

            ProcessStartInfo recordTraceStartInfo = new();
            recordTraceStartInfo.FileName = recordTracePath;
            recordTraceStartInfo.Arguments = $"--script-file {scriptFilePath}";
            recordTraceStartInfo.WorkingDirectory = appBaseDir;
            recordTraceStartInfo.RedirectStandardOutput = true;
            recordTraceStartInfo.RedirectStandardError = true;

            using Process traceeProcess = Process.Start(traceeStartInfo);
            using Process recordTraceProcess = Process.Start(recordTraceStartInfo);
            recordTraceProcess.OutputDataReceived += (_, args) => Console.WriteLine($"[record-trace] {args.Data}");
            recordTraceProcess.BeginOutputReadLine();
            recordTraceProcess.ErrorDataReceived += (_, args) => Console.Error.WriteLine($"[record-trace] {args.Data}");
            recordTraceProcess.BeginErrorReadLine();

            if (!traceeProcess.HasExited && !traceeProcess.WaitForExit(15000))
            {
                traceeProcess.Kill();
            }

            // Until record-trace supports duration, the only way to stop it is to send SIGINT (ctrl+c)
            kill(recordTraceProcess.Id, SIGINT);
            if (!recordTraceProcess.HasExited && !recordTraceProcess.WaitForExit(20000))
            {
                // record-trace needs to stop gracefully to generate the trace file
                recordTraceProcess.Kill();
            }

            if (!File.Exists(traceFilePath))
            {
                Console.Error.WriteLine($"Expected trace file not found at `{traceFilePath}`");
                return -1;
            }

            if (!ValidateTraceeEvents(traceFilePath))
            {
                Console.Error.WriteLine($"Trace file `{traceFilePath}` does not contain expected events.");
                return -1;
            }

            return 100;
        }

        private static bool ValidateTraceeEvents(string traceFilePath)
        {
            string etlxPath = TraceLog.CreateFromEventPipeDataFile(traceFilePath);
            using TraceLog log = new(etlxPath);
            using TraceLogEventSource source = log.Events.GetSource();
            bool startEventFound = false;
            bool stopEventFound = false;

            source.AllEvents += (TraceEvent e) =>
            {
                if (e.ProviderName == "Microsoft-Windows-DotNETRuntime")
                {
                    if (e.EventName == "GC/Start")
                    {
                        startEventFound = true;
                    }
                    else if (e.EventName == "GC/Stop")
                    {
                        stopEventFound = true;
                    }
                }
            };

            source.Process();
            return startEventFound && stopEventFound;
        }
    }
}
