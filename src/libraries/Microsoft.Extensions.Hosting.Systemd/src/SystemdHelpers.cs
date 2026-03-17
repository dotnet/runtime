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

        // Environment variable set by systemd (v248+) to the PID of the main service process.
        // This is the most reliable way to detect if we're running as a systemd service, as it doesn't
        // require reading /proc and works even when ProtectProc=invisible is set.
        // See https://www.freedesktop.org/software/systemd/man/latest/systemd.exec.html#%24SYSTEMD_EXEC_PID for details.
        private const string SYSTEMD_EXEC_PID = "SYSTEMD_EXEC_PID";

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

            // Preferred detection method: compare SYSTEMD_EXEC_PID to the current PID.
            // Works even when ProtectProc=invisible hides /proc entries.
            string? systemdExecPid = Environment.GetEnvironmentVariable(SYSTEMD_EXEC_PID);
            if (!string.IsNullOrEmpty(systemdExecPid))
            {
                if (int.TryParse(systemdExecPid, NumberStyles.None, CultureInfo.InvariantCulture, out int execPid))
                {
                    return execPid == processId;
                }
                // Malformed value: don't trust it, fall through to legacy detection.
            }

            // To support containerized systemd services (e.g. Podman with --sdnotify=container),
            // check if we're the main process (PID 1) and if there are systemd environment
            // variables defined for notifying the service manager, or passing listen handles.
            if (processId == 1)
            {
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NOTIFY_SOCKET")) ||
                       !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LISTEN_PID"));
            }

            // Legacy detection for systemd < 248 (e.g. Ubuntu 20.04, Debian 11).
            // Note: silently returns false when ProtectProc=invisible is set.
            try
            {
                // Check whether our direct parent is 'systemd'.
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
