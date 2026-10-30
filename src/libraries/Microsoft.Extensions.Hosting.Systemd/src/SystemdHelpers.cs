// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;

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

            int processId = Environment.ProcessId;

            // Preferred detection method: compare SYSTEMD_EXEC_PID to the current PID.
            // Works even when ProtectProc=invisible hides /proc entries.
            string? systemdExecPid = Environment.GetEnvironmentVariable(SystemdConstants.SystemdExecPid);
            if (!string.IsNullOrEmpty(systemdExecPid))
            {
                if (int.TryParse(systemdExecPid, NumberStyles.None, CultureInfo.InvariantCulture, out int execPid))
                {
                    if (execPid == processId)
                    {
                        return true;
                    }
                    // Mismatch: fall through to PID 1 / legacy checks
                }
                // Malformed value: don't trust it, fall through to legacy detection.
            }

            // To support containerized systemd services (e.g. Podman with --sdnotify=container)
            if (processId == 1)
            {
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SystemdConstants.NotifySocket)) ||
                       !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SystemdConstants.ListenPid));
            }

            // Legacy detection for systemd < 248 (e.g. Ubuntu 20.04, Debian 11).
            // Note: silently returns false if /proc cannot be read, such as when ProtectProc=invisible is set.
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

        private static readonly bool _isSystemdNotify = GetIsSystemdNotify();

        /// <summary>
        /// Checks if the current process has systemd notify enabled.
        /// </summary>
        private static bool IsSystemdNotify() => _isSystemdNotify;

        private static bool GetIsSystemdNotify()
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return false;
            }
            // Checks whether NOTIFY_SOCKET is set, indicating the service manager expects sd_notify notifications.
            string? socketPath = Environment.GetEnvironmentVariable(SystemdConstants.NotifySocket);
            return !string.IsNullOrEmpty(socketPath);
        }

        /// <summary>
        /// Checks if the systemd journal log formatter should be enabled.
        /// </summary>
        // TODO: #127218
        internal static bool IsSystemdLogger() => IsSystemdService();

        /// <summary>
        /// Checks if <see cref="SystemdLifetime"/> and <see cref="SystemdNotifier"/> should be registered.
        /// </summary>
        /// <remarks><see cref="SystemdNotifier"/> is a noop when <c>NOTIFY_SOCKET</c> is absent.</remarks>
        internal static bool IsSystemdLifetime() => IsSystemdService() || IsSystemdNotify();
    }
}
