// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Hosting.WindowsServices
{
    /// <summary>
    /// Helper methods for Windows Services.
    /// </summary>
    public static class WindowsServiceHelpers
    {
        /// <summary>
        /// Check if the current process is hosted as a Windows Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a Windows Service, otherwise <c>false</c>.</returns>
        [SupportedOSPlatformGuard("windows")]
        public static bool IsWindowsService()
        {
            if (
#if NETFRAMEWORK
                Environment.OSVersion.Platform != PlatformID.Win32NT
#else
                !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
#endif
                )
            {
                return false;
            }

            var parent = Internal.Win32.GetParentProcess();
            if (parent == null)
            {
                return false;
            }
            return string.Equals("services", parent.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
