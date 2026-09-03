// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.Tests.NonLossyBlockEnv
{
    public sealed class NonLossyBlockEnvEventSource : EventSource
    {
        private NonLossyBlockEnvEventSource() {}
        public static NonLossyBlockEnvEventSource Log = new NonLossyBlockEnvEventSource();
        public void BlockEnvEvent() { WriteEvent(1, "BlockEnvEvent"); }
    }

    // Validates the DOTNET_EventPipeBufferingMode startup-session opt-in (env-var path in
    // enable_default_session_via_env_variables). This complements the IPC coverage in nonlossyblock/ by exercising
    // the file/filestream startup session and the guards that reject unsupported configurations.
    //
    // The test re-launches itself as a child ("tracee") with a specific env-var configuration, then inspects
    // whether the runtime produced a trace file:
    //   * Block + non-streaming FILE        -> rejected (ep_session_alloc), no trace file, app still runs.
    //   * invalid buffering mode (e.g. 2)   -> rejected (ep.c reads Drop/Block only), no trace file, app still runs.
    //   * Block + streaming FILESTREAM      -> session starts and losslessly captures every event.
    public class NonLossyBlockEnv
    {
        private const string ProviderName = "NonLossyBlockEnvEventSource";
        private const int EventCount = 50_000;
        private const int CircularMB = 1;
        private const int TraceeExitTimeoutMs = 120_000;

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("tracee", StringComparison.OrdinalIgnoreCase))
            {
                return RunTracee();
            }

            return RunOrchestrator();
        }

        // Child process: any startup EventPipe session was created from the env vars before Main ran. Force the
        // EventSource to construct so an active session can enable it, wait briefly for that enablement, then emit.
        private static int RunTracee()
        {
            NonLossyBlockEnvEventSource log = NonLossyBlockEnvEventSource.Log;

            Stopwatch sw = Stopwatch.StartNew();
            while (!log.IsEnabled() && sw.ElapsedMilliseconds < 2000)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine($"[tracee] EventSource enabled = {log.IsEnabled()}; emitting {EventCount} events.");
            for (int i = 0; i < EventCount; i++)
            {
                log.BlockEnvEvent();
            }
            Console.WriteLine("[tracee] finished emitting events.");
            return 0;
        }

        private static int RunOrchestrator()
        {
            Console.WriteLine("==TEST STARTING==");

            try
            {
                bool passed =
                    RunRejectScenario("Block + non-streaming FILE", bufferingMode: "1", outputStreaming: "0") &&
                    RunRejectScenario("invalid buffering mode (2)", bufferingMode: "2", outputStreaming: "1") &&
                    RunPositiveScenario();

                Console.WriteLine(passed ? "==TEST FINISHED: PASSED!==" : "==TEST FINISHED: FAILED!==");
                return passed ? 100 : -1;
            }
            catch (PlatformNotSupportedException)
            {
                // Platforms that cannot launch child processes (e.g. iOS, tvOS) cannot exercise this test.
                Console.WriteLine("Skipping test: this platform does not support launching child processes.");
                return 100;
            }
        }

        // An unsupported configuration must fail to start the session (no trace file) without taking down the app.
        private static bool RunRejectScenario(string name, string bufferingMode, string outputStreaming)
        {
            Console.WriteLine($"-- Reject scenario: {name} --");
            string traceDir = CreateTraceDirectory();
            string traceFilePath = Path.Combine(traceDir, "trace.nettrace");

            try
            {
                int exitCode = LaunchTracee(bufferingMode, outputStreaming, traceFilePath);
                if (exitCode != 0)
                {
                    Console.WriteLine($"FAILED: tracee exited with {exitCode}; an invalid startup config must not crash the app.");
                    return false;
                }

                string[] produced = Directory.GetFiles(traceDir);
                if (produced.Length != 0)
                {
                    Console.WriteLine($"FAILED: expected no trace file, but found: {string.Join(", ", produced)}");
                    return false;
                }

                Console.WriteLine("PASSED: no startup session and no trace file were produced.");
                return true;
            }
            finally
            {
                TryDeleteDirectory(traceDir);
            }
        }

        // Block on a streaming FILESTREAM session must capture every event with no drops.
        private static bool RunPositiveScenario()
        {
            Console.WriteLine("-- Positive scenario: Block + streaming FILESTREAM --");
            string traceDir = CreateTraceDirectory();
            string traceFilePath = Path.Combine(traceDir, "trace.nettrace");

            try
            {
                int exitCode = LaunchTracee(bufferingMode: "1", outputStreaming: "1", traceFilePath);
                if (exitCode != 0)
                {
                    Console.WriteLine($"FAILED: tracee exited with {exitCode}, expected 0.");
                    return false;
                }
                if (!File.Exists(traceFilePath))
                {
                    Console.WriteLine($"FAILED: expected a trace file at {traceFilePath}, but none was produced.");
                    return false;
                }

                int actualCount = 0;
                long eventsLost;
                using (EventPipeEventSource source = new EventPipeEventSource(traceFilePath))
                {
                    source.Dynamic.All += (TraceEvent e) =>
                    {
                        if (e.ProviderName == ProviderName && (int)e.ID == 1)
                        {
                            actualCount++;
                        }
                    };
                    source.Process();
                    eventsLost = source.EventsLost;
                }

                Console.WriteLine($"Observed {actualCount} '{ProviderName}' events; EventsLost = {eventsLost}.");

                if (eventsLost != 0)
                {
                    Console.WriteLine($"FAILED: expected zero dropped events, but the trace reported {eventsLost}.");
                    return false;
                }
                if (actualCount != EventCount)
                {
                    Console.WriteLine($"FAILED: expected exactly {EventCount} events, but saw {actualCount}.");
                    return false;
                }

                Console.WriteLine("PASSED: the Block startup session delivered every event losslessly.");
                return true;
            }
            finally
            {
                TryDeleteDirectory(traceDir);
            }
        }

        private static int LaunchTracee(string bufferingMode, string outputStreaming, string traceFilePath)
        {
            ProcessStartInfo psi = new();
            psi.FileName = Environment.ProcessPath
                ?? throw new InvalidOperationException("Environment.ProcessPath is null");

            // NativeAOT runs the test as a native executable (Assembly.Location is empty); CoreCLR/Mono run it
            // through the host (e.g. corerun) with the managed assembly path as the first argument.
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                psi.ArgumentList.Add(assemblyLocation);
            }
            psi.ArgumentList.Add("tracee");

            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            psi.Environment["DOTNET_EnableEventPipe"] = "1";
            psi.Environment["DOTNET_EventPipeConfig"] = $"{ProviderName}:0:4";
            psi.Environment["DOTNET_EventPipeOutputPath"] = traceFilePath;
            psi.Environment["DOTNET_EventPipeCircularMB"] = CircularMB.ToString();
            psi.Environment["DOTNET_EventPipeBufferingMode"] = bufferingMode;
            psi.Environment["DOTNET_EventPipeOutputStreaming"] = outputStreaming;

            Console.WriteLine($"Launching tracee: {psi.FileName} {string.Join(" ", psi.ArgumentList)}");
            Console.WriteLine($"  DOTNET_EventPipeBufferingMode={bufferingMode} DOTNET_EventPipeOutputStreaming={outputStreaming} DOTNET_EventPipeCircularMB={CircularMB}");

            using Process process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start tracee process.");
            process.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[tracee][stdout] {e.Data}"); };
            process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[tracee][stderr] {e.Data}"); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(TraceeExitTimeoutMs))
            {
                Console.WriteLine($"Tracee did not exit within {TraceeExitTimeoutMs}ms; killing it.");
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                return -1;
            }
            process.WaitForExit(); // ensure async output is flushed
            return process.ExitCode;
        }

        private static string CreateTraceDirectory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "nonlossyblockenv-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void TryDeleteDirectory(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
