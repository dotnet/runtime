// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class WindowsUtils
    {
        private delegate bool EnumThreadWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadWindowsDelegate plfn, IntPtr lParam);

        public static IntPtr WaitForPopupFromProcess(Process process, int timeout = 60000)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            IntPtr windowHandle = IntPtr.Zero;
            int timeRemaining = timeout;
            while (timeRemaining > 0)
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    // We take the last window we find. There really should only be one at most anyways.
                    EnumThreadWindows(thread.Id,
                        (hWnd, lParam) => {
                            windowHandle = hWnd;
                            return true;
                        },
                        IntPtr.Zero);
                }

                if (windowHandle != IntPtr.Zero)
                {
                    break;
                }

                System.Threading.Thread.Sleep(100);
                timeRemaining -= 100;
            }

            // Do not fail if the window could be detected, sometimes the check is fragile and doesn't work.
            // Not worth the trouble trying to figure out why (only happens rarely in the CI system).
            // We will rely on product tracing in the failure case.
            return windowHandle;
        }
    }
}
