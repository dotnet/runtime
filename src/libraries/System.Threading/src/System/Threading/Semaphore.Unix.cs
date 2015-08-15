// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Semaphore : WaitHandle
    {
        private static void ValidateNewName(string name)
        {
            if (name != null)
                throw new PlatformNotSupportedException(SR.WaitHandle_NamesNotSupported);
        }

        private static void ValidateExistingName(string name)
        {
            throw new PlatformNotSupportedException(SR.WaitHandle_NamesNotSupported);
        }

        private static SafeWaitHandle CreateSemaphone(int initialCount, int maximumCount, string name)
        {
            Debug.Assert(initialCount >= 0);
            Debug.Assert(maximumCount >= 1);
            Debug.Assert(initialCount <= maximumCount);

            return Interop.libcoreclr.CreateSemaphore(null, initialCount, maximumCount, name);
        }

        private static SafeWaitHandle OpenSemaphore(string name)
        {
            Debug.Fail("This function should never be called.");
            throw new PlatformNotSupportedException(SR.WaitHandle_NamesNotSupported);
        }

        private static bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount)
        {
            return Interop.libcoreclr.ReleaseSemaphore(handle, releaseCount, out previousCount);
        }

        private static string GetMessage(int win32Error)
        {
            return Interop.libcoreclr.GetMessage(win32Error);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}
