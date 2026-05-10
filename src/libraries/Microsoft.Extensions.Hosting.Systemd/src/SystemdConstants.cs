// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Hosting.Systemd
{
    internal static class SystemdConstants
    {
        /// <summary>
        /// Environment variable set by systemd (v229+) to the path of the notify socket for the service. If this variable is set, the service should send status notifications to systemd using this socket.
        /// </summary>
        /// <see href="https://www.freedesktop.org/software/systemd/man/latest/systemd.exec.html#%24NOTIFY_SOCKET" />
        internal const string NotifySocket = "NOTIFY_SOCKET";

        /// <summary>
        /// Environment variable set by systemd (v248+) to the PID of the main service process.
        /// This is the most reliable way to detect if we're running as a systemd service, as it doesn't
        /// require reading /proc and works even when ProtectProc=invisible is set.
        /// </summary>
        /// <see href="https://www.freedesktop.org/software/systemd/man/latest/systemd.exec.html#%24SYSTEMD_EXEC_PID" />
        internal const string SystemdExecPid = "SYSTEMD_EXEC_PID";

        /// <summary>
        /// Environment variable set by systemd for socket activation, indicating the PID
        /// that should receive the listen file descriptors.
        /// </summary>
        /// <see href="https://www.freedesktop.org/software/systemd/man/latest/systemd.exec.html#%24LISTEN_FDS" />
        internal const string ListenPid = "LISTEN_PID";
    }
}
