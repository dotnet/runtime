// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Semaphore : WaitHandle
    {
        private static SafeWaitHandle CreateSemaphone(int initialCount, int maximumCount, string name)
        {
            Debug.Assert(initialCount >= 0);
            Debug.Assert(maximumCount >= 1);
            Debug.Assert(initialCount <= maximumCount);
            Debug.Assert(name == null || name.Length <= MAX_PATH);

            return Interop.libcoreclrpal.CreateSemaphore(null, initialCount, maximumCount, name);
        }

        private static SafeWaitHandle OpenSemaphore(string name)
        {
            Debug.Assert(name != null);
            Debug.Assert(name.Length <= MAX_PATH);

            const int SYNCHRONIZE = 0x00100000;
            const int SEMAPHORE_MODIFY_STATE = 0x00000002;

            //Pass false to OpenSemaphore to prevent inheritedHandles
            return Interop.libcoreclrpal.OpenSemaphore(SEMAPHORE_MODIFY_STATE | SYNCHRONIZE, false, name);
        }

        private static bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount)
        {
            return Interop.libcoreclrpal.ReleaseSemaphore(handle, releaseCount, out previousCount);
        }

        private static string GetMessage(int win32Error)
        {
            return Interop.libcoreclrpal.GetMessage(win32Error);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}

