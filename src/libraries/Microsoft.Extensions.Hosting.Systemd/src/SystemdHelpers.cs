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
    public static partial class SystemdHelpers
    {
        internal const string NOTIFY_SOCKET_ENVVAR_KEY = "NOTIFY_SOCKET";
        internal const string LISTEN_PID_ENVVAR_KEY = "LISTEN_PID";

        private static bool? _isSystemdService;

        /// <summary>
        /// Check if the current process is hosted as a systemd Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a systemd Service, otherwise <c>false</c>.</returns>
        public static bool IsSystemdService()
            => _isSystemdService ?? (bool)(_isSystemdService = CheckParentIsSystemd());

        private static bool CheckParentIsSystemd()
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return false;
            }

            try
            {
                // Check whether our direct parent is 'systemd'.
                // This will not work for containerised applications, since our direct parent will be some container runtime
                int parentPid = Interop.libc.GetParentPid();
                string ppidString = parentPid.ToString(NumberFormatInfo.InvariantInfo);
                byte[] comm = File.ReadAllBytes("/proc/" + ppidString + "/comm");
                if (comm.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes("systemd\n")))
                {
                    return true;
                }
            }
            catch
            {
            }

            // Convention for containers is to have the PID of the containerised process set to 1
            if (Environment.ProcessId == 1)
            {
                if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(NOTIFY_SOCKET_ENVVAR_KEY)))
                {
                    return true;
                }
                if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LISTEN_PID_ENVVAR_KEY)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
