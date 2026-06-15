// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Internal
{
    internal static class ProcessExtensions
    {
        private const int ESRCH = 3;
#if NET
        private static readonly int s_sigint = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 2 : GetPlatformSignalNumber(PosixSignal.SIGINT);
        private static readonly int s_sigterm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 15 : GetPlatformSignalNumber(PosixSignal.SIGTERM);
#endif
        private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        internal static int SigIntSignalNumber
        {
            get
            {
#if NET
                return s_sigint;
#else
                return 2;
#endif
            }
        }

        internal static int SigTermSignalNumber
        {
            get
            {
#if NET
                return s_sigterm;
#else
                return 15;
#endif
            }
        }

        public static void KillTree(this Process process) => process.KillTree(_defaultTimeout);

        public static void KillTree(this Process process, TimeSpan timeout)
        {
            var pid = process.Id;
            if (_isWindows)
            {
                RunProcessAndWaitForExit(
                    "taskkill",
                    $"/T /F /PID {pid}",
                    timeout,
                    out var _);
            }
            else
            {
                var children = new HashSet<int>();
                GetAllChildIdsUnix(pid, children, timeout);
                foreach (var childId in children)
                {
                    KillProcessUnix(childId, timeout);
                }
                KillProcessUnix(pid, timeout);
            }
        }

        private static void GetAllChildIdsUnix(int parentId, ISet<int> children, TimeSpan timeout)
        {
            string stdout;
            try
            {
                RunProcessAndWaitForExit(
                    "pgrep",
                    $"-P {parentId}",
                    timeout,
                    out stdout);
            }
            catch (Win32Exception)
            {
                return;
            }

            if (!string.IsNullOrEmpty(stdout))
            {
                using (var reader = new StringReader(stdout))
                {
                    while (true)
                    {
                        var text = reader.ReadLine();
                        if (text == null)
                        {
                            return;
                        }

                        if (int.TryParse(text, out var id))
                        {
                            children.Add(id);
                            // Recursively get the children
                            GetAllChildIdsUnix(id, children, timeout);
                        }
                    }
                }
            }
        }

        private static void KillProcessUnix(int processId, TimeSpan timeout)
        {
            try
            {
                if (Kill(processId, SigTermSignalNumber) != 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != ESRCH)
                    {
                        KillProcessUnixHard(processId, timeout);
                        return;
                    }
                }

                using (Process process = Process.GetProcessById(processId))
                {
                    if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                    {
                        KillProcessUnixHard(processId, timeout);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Ignore if process has already exited.
            }
            catch (InvalidOperationException)
            {
                // Ignore if process has already exited.
            }
            catch (Win32Exception)
            {
                KillProcessUnixHard(processId, timeout);
            }
        }

        private static void KillProcessUnixHard(int processId, TimeSpan timeout)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    process.Kill();
                    process.WaitForExit((int)timeout.TotalMilliseconds);
                }
            }
            catch (ArgumentException)
            {
                // Ignore if process has already exited.
            }
            catch (InvalidOperationException)
            {
                // Ignore if process has already exited.
            }
            catch (Win32Exception)
            {
                // Ignore permission or process-not-found errors.
            }
        }

        private static void RunProcessAndWaitForExit(string fileName, string arguments, TimeSpan timeout, out string stdout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            var process = Process.Start(startInfo);

            stdout = null;
            if (process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                stdout = process.StandardOutput.ReadToEnd();
            }
            else
            {
                process.Kill();
            }
        }

        internal static void SendSignal(int pid, int signal)
        {
            if (_isWindows)
            {
                throw new PlatformNotSupportedException();
            }
            if (Kill(pid, signal) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        private static extern int Kill(int pid, int sig);

#if NET
        [DllImport("libSystem.Native", EntryPoint = "SystemNative_GetPlatformSignalNumber")]
        private static extern int GetPlatformSignalNumber(PosixSignal signal);
#endif
    }
}
