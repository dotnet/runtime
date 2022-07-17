// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Text;
#if !NETCOREAPP
using System.Diagnostics;
#endif

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Helper methods for systemd Services.
    /// </summary>
    public static partial class SystemdHelpers
    {
        private static bool? _isSystemdService;

        /// <summary>
        /// Check if the current process is hosted as a systemd Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a systemd Service, otherwise <c>false</c>.</returns>
        public static bool IsSystemdService()
            => _isSystemdService ??= GetIsSystemdService();

        private static bool GetIsSystemdService()
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return false;
            }

            // To support containerized systemd services, check if we're the main process (PID 1)
            // and if there are systemd environment variables defined for notifying the service
            // manager, or passing listen handles.
#if NETCOREAPP
            int processId = Environment.ProcessId;
#else
            int processId = Process.GetCurrentProcess().Id;
#endif

            if (processId == 1)
            {
                return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NOTIFY_SOCKET")) ||
                       !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LISTEN_PID"));
            }

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
