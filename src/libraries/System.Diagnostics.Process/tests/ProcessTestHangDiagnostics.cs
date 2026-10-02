// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics.Tests;

internal static class ProcessTestHangDiagnostics
{
    private static readonly TimeSpan s_watchdogTimeout = TimeSpan.FromMinutes(8);
    private static readonly TimeSpan s_dumpTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan s_dumperExitTimeout = TimeSpan.FromSeconds(30);

    [ModuleInitializer]
    internal static void Initialize()
    {
        string? uploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
        if (string.IsNullOrEmpty(uploadRoot) ||
            !string.Equals(Path.GetFileName(Environment.GetCommandLineArgs()[0]), "xunit.console.dll", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string dumpTool = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "createdump.exe");
        var watchdog = new Thread(() => Watchdog(dumpTool, uploadRoot, s_watchdogTimeout, s_dumpTimeout))
        {
            // The watchdog belongs to the runner, not to RemoteExecutor children, and ends with the runner.
            IsBackground = true,
            Name = "Process tests hang watchdog"
        };
        watchdog.Start();
    }

    private static void Watchdog(string dumpTool, string uploadRoot, TimeSpan watchdogTimeout, TimeSpan dumpTimeout)
    {
        // Bypass xUnit's per-test output capture, including passing-output suppression.
        using TextWriter log = TextWriter.Synchronized(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true
        });
        log.WriteLine($"[Process hang diagnostics] Armed for {watchdogTimeout}; PID={Environment.ProcessId}; architecture={RuntimeInformation.ProcessArchitecture}; OS={Environment.OSVersion}; createdump={dumpTool}; upload={uploadRoot}");

        Thread.Sleep(watchdogTimeout);
        string dumpPath = Path.Combine(uploadRoot, $"ProcessTests.{Environment.ProcessId}.dmp");
        log.WriteLine($"[Process hang diagnostics] Snapshot time reached. Capturing full test-host dump to {dumpPath} without terminating tests.");
        try
        {
            Directory.CreateDirectory(uploadRoot);
            CaptureDump(dumpTool, dumpPath, dumpTimeout, log);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException or OperationCanceledException)
        {
            log.WriteLine($"[Process hang diagnostics] Dump capture failed: {e}");
        }
    }

    private static void CaptureDump(string dumpTool, string dumpPath, TimeSpan dumpTimeout, TextWriter log)
    {
        var startInfo = new ProcessStartInfo(dumpTool)
        {
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("--full");
        startInfo.ArgumentList.Add("--name");
        startInfo.ArgumentList.Add(dumpPath);

        using var dumper = new Process { StartInfo = startInfo };
        var exitLock = new object();
        bool started = false;
        bool finished = false;
        bool hostExiting = false;

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        try
        {
            lock (exitLock)
            {
                if (hostExiting)
                {
                    throw new OperationCanceledException("The test host exited before the snapshot could start.");
                }

                // Windows createdump targets its parent, so start it directly in the test host.
                started = dumper.Start();
                if (!started)
                {
                    throw new InvalidOperationException("Could not start createdump.");
                }
            }

            log.WriteLine($"[Process hang diagnostics] createdump PID={dumper.Id} started.");
            if (!dumper.WaitForExit((int)dumpTimeout.TotalMilliseconds))
            {
                throw new IOException($"createdump exceeded its {dumpTimeout} budget.");
            }

            if (dumper.ExitCode != 0 || !File.Exists(dumpPath) || new FileInfo(dumpPath).Length == 0)
            {
                throw new IOException($"createdump exited with code {dumper.ExitCode} without a successful nonempty dump at {dumpPath}.");
            }

            log.WriteLine($"[Process hang diagnostics] Full dump captured: {dumpPath} ({new FileInfo(dumpPath).Length} bytes). Test execution continues.");
        }
        finally
        {
            lock (exitLock)
            {
                finished = true;
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                if (started)
                {
                    StopDumper(dumper);
                }
            }
        }

        void OnProcessExit(object? sender, EventArgs args)
        {
            lock (exitLock)
            {
                if (finished)
                {
                    return;
                }

                hostExiting = true;
                if (started)
                {
                    try
                    {
                        StopDumper(dumper);
                    }
                    catch (Exception e) when (e is Win32Exception or InvalidOperationException)
                    {
                        log.WriteLine($"[Process hang diagnostics] Dump child cleanup failed during test-host exit: {e}");
                    }
                }
            }
        }
    }

    private static void StopDumper(Process dumper)
    {
        if (!dumper.HasExited)
        {
            dumper.Kill();
        }

        if (!dumper.WaitForExit((int)s_dumperExitTimeout.TotalMilliseconds))
        {
            throw new InvalidOperationException($"createdump PID {dumper.Id} did not exit after termination.");
        }
    }
}
