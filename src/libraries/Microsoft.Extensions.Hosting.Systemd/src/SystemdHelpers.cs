// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;
#if !NET
using System.Diagnostics;
#endif

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Helper methods for systemd Services.
    /// </summary>
    public static class SystemdHelpers
    {
        private static readonly bool _isSystemdService = GetIsSystemdService();

        /// <summary>
        /// Checks if the current process is hosted as a systemd Service.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the current process is hosted as a systemd Service; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsSystemdService() => _isSystemdService;

        private static bool GetIsSystemdService()
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return false;
            }

#if NET
            int processId = Environment.ProcessId;
#else
            int processId = Process.GetCurrentProcess().Id;
#endif

            // Preferred detection method: SYSTEMD_EXEC_PID is set by systemd (v248+) to the PID
            // of the main service process. Comparing it to the current PID lets us reliably
            // determine whether we are the direct service process, without reading /proc.
            // This also correctly handles ProtectProc=invisible, which hides /proc entries of
            // other users' processes and would cause the fallback below to silently fail.
            string? systemdExecPid = Environment.GetEnvironmentVariable("SYSTEMD_EXEC_PID");
            if (!string.IsNullOrEmpty(systemdExecPid))
            {
                if (int.TryParse(systemdExecPid, NumberStyles.None, CultureInfo.InvariantCulture, out int execPid))
                {
                    return execPid == processId;
                }
                // Malformed value: don't trust it, fall through to legacy detection.
            }

            // Legacy detection for systemd < 248 (e.g. Ubuntu 20.04, Debian 9/10).

            // To support containerized systemd services, check if we're the main process (PID 1)
            // and if there are systemd environment variables defined for notifying the service
            // manager, or passing listen handles.
            if (processId == 1)
            {
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NOTIFY_SOCKET")) ||
                       !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LISTEN_PID"));
            }

            try
            {
                // Check whether our direct parent is 'systemd'.
                // Note: this silently fails (returns false) when ProtectProc=invisible is set,
                // as /proc/{ppid}/comm becomes inaccessible. This is the bug fixed above for
                // systemd >= 248 via SYSTEMD_EXEC_PID.
                int parentPid = Interop.libc.GetParentPid();
                string ppidString = parentPid.ToString(NumberFormatInfo.InvariantInfo);
                byte[] comm = File.ReadAllBytes("/proc/" + ppidString + "/comm");
                return comm.AsSpan().SequenceEqual("systemd\n"u8);
            }
            catch
            {
            }

            return false;
        }
    }
}
