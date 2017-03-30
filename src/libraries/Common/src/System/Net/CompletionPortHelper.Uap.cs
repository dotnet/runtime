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
            // SetFileCompletionNotificationModes is not supported on UAP.
            return false;
        }

        internal static readonly bool PlatformHasUdpIssue = false;
    }
}
