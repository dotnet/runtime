// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    internal static class CompletionPortHelper
    {
        internal static bool SkipCompletionPortOnSuccess(SafeHandle handle)
        {
            return Interop.Kernel32.SetFileCompletionNotificationModes(handle,
                Interop.Kernel32.FileCompletionNotificationModes.SkipCompletionPortOnSuccess |
                Interop.Kernel32.FileCompletionNotificationModes.SkipSetEventOnHandle);
        }

        // There's a bug with using SetFileCompletionNotificationModes with UDP on Windows 7 and before.
        // This check tells us if the problem exists on the platform we're running on.
        internal static readonly bool PlatformHasUdpIssue = CheckIfPlatformHasUdpIssue();

        private static bool CheckIfPlatformHasUdpIssue()
        {
            Version osVersion = Environment.OSVersion.Version;

            // 6.1 == Windows 7
            return (osVersion.Major < 6 ||
                    (osVersion.Major == 6 && osVersion.Minor <= 1));
        }
    }
}
