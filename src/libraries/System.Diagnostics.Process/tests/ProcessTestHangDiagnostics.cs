// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Xunit.Sdk;

namespace System.Diagnostics.Tests
{
    internal sealed class ProcessTestHangDiagnosticsAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            ProcessTestHangDiagnostics.Log($"Starting {methodUnderTest.DeclaringType?.FullName}.{methodUnderTest.Name}.");
        }

        public override void After(MethodInfo methodUnderTest)
        {
            ProcessTestHangDiagnostics.Log($"Finished {methodUnderTest.DeclaringType?.FullName}.{methodUnderTest.Name}.");
        }
    }

    internal static class ProcessTestHangDiagnostics
    {
#if TargetsWindows
        private const string InstallationTypeKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        private static readonly TimeSpan WatchdogTimeout = TimeSpan.FromMinutes(3);
        private static readonly TextWriter s_log = TextWriter.Synchronized(
            new StreamWriter(Console.OpenStandardError(), Encoding.UTF8, bufferSize: 1024, leaveOpen: true) { AutoFlush = true });

        [ModuleInitializer]
        internal static void Initialize()
        {
            Log($"ProcessPath={Environment.ProcessPath}; OSVersion={Environment.OSVersion.Version}; Framework={RuntimeInformation.FrameworkDescription}");
            ConfigureWindowsErrorReporting();

            var watchdog = new Thread(Watchdog)
            {
                IsBackground = true,
                Name = "Process tests hang watchdog"
            };
            watchdog.Start();

            Log("Reading Windows InstallationType.");
            object? installationType = Registry.GetValue(InstallationTypeKey, "InstallationType", defaultValue: null);
            Log($"InstallationType={installationType ?? "<null>"}");

            Log("Evaluating PlatformDetection.IsWindowsNanoServer and IsWindowsServerCore.");
            bool isWindowsNanoServer = PlatformDetection.IsWindowsNanoServer;
            bool isWindowsServerCore = PlatformDetection.IsWindowsServerCore;
            Log($"IsWindowsNanoServer={isWindowsNanoServer}; IsWindowsServerCore={isWindowsServerCore}");
        }

        internal static void Log(string message)
        {
            s_log.WriteLine($"[Process test hang diagnostics] {message}");
        }

        private static void ConfigureWindowsErrorReporting()
        {
            string? dumpFolder = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");
            string? processPath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(dumpFolder) || string.IsNullOrEmpty(processPath))
            {
                Log($"WER LocalDumps not configured; HELIX_DUMP_FOLDER={dumpFolder ?? "<null>"}.");
                return;
            }

            string executableName = Path.GetFileName(processPath);
            string keyPath = $@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\{executableName}";

            try
            {
                using RegistryKey? key = Registry.LocalMachine.CreateSubKey(keyPath);
                if (key is null)
                {
                    Log($"Unable to create WER LocalDumps key HKLM\\{keyPath}.");
                    return;
                }

                key.SetValue("DumpCount", 2, RegistryValueKind.DWord);
                key.SetValue("DumpFolder", dumpFolder, RegistryValueKind.ExpandString);
                key.SetValue("DumpType", 2, RegistryValueKind.DWord);
                Log($"WER LocalDumps configured for {executableName} in {dumpFolder}.");
            }
            catch (Exception e) when (e is IOException or SecurityException or UnauthorizedAccessException)
            {
                Log($"WER LocalDumps configuration failed: {e}");
            }
        }

        private static void Watchdog()
        {
            Thread.Sleep(WatchdogTimeout);
            const string message = "System.Diagnostics.Process.Tests exceeded the diagnostic watchdog timeout.";
            Log(message);
            Environment.FailFast(message);
        }
#else
        internal static void Log(string message)
        {
        }
#endif
    }
}
