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
        if (!File.Exists(dumpTool))
        {
            throw new FileNotFoundException("The diagnostic watchdog requires the test runtime's Windows createdump.exe.", dumpTool);
        }

        Directory.CreateDirectory(uploadRoot);
        var watchdog = new Thread(() => Watchdog(dumpTool, uploadRoot, s_watchdogTimeout))
        {
            // The watchdog belongs to the runner, not to RemoteExecutor children, and ends with the runner.
            IsBackground = true,
            Name = "Process tests hang watchdog"
        };
        watchdog.Start();
    }

    private static void Watchdog(string dumpTool, string uploadRoot, TimeSpan watchdogTimeout)
    {
        // Bypass xUnit's per-test output capture, including passing-output suppression.
        using var log = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true
        };
        log.WriteLine($"[Process hang diagnostics] Armed for {watchdogTimeout}; PID={Environment.ProcessId}; architecture={RuntimeInformation.ProcessArchitecture}; OS={Environment.OSVersion}; createdump={dumpTool}; upload={uploadRoot}");

        Thread.Sleep(watchdogTimeout);
        string dumpPath = Path.Combine(uploadRoot, $"ProcessTests.{Environment.ProcessId}.dmp");
        log.WriteLine($"[Process hang diagnostics] Watchdog expired. Capturing full test-host dump to {dumpPath}.");
        try
        {
            CaptureDump(dumpTool, dumpPath, s_dumpTimeout, log);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        {
            log.WriteLine($"[Process hang diagnostics] Dump capture failed: {e}");
        }

        // Return a distinct failure before Helix's 900s kill, leaving time for artifact upload.
        log.WriteLine("[Process hang diagnostics] Ending the timed-out test host with exit code 124.");
        Environment.Exit(124);
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

        // Windows createdump targets its parent, so launch the matching runtime's tool directly from the test host.
        using Process dumper = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start createdump.");
        if (!dumper.WaitForExit((int)dumpTimeout.TotalMilliseconds))
        {
            dumper.Kill();
            if (!dumper.WaitForExit(30_000))
            {
                throw new InvalidOperationException($"createdump PID {dumper.Id} did not exit after termination.");
            }

            throw new IOException($"createdump exceeded its {dumpTimeout} budget.");
        }

        if (dumper.ExitCode != 0 || !File.Exists(dumpPath) || new FileInfo(dumpPath).Length == 0)
        {
            throw new IOException($"createdump exited with code {dumper.ExitCode} without a successful nonempty dump at {dumpPath}.");
        }

        log.WriteLine($"[Process hang diagnostics] Full dump captured: {dumpPath} ({new FileInfo(dumpPath).Length} bytes).");
    }
}
