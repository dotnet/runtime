// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal sealed class ProcessWaitHandle : WaitHandle
        {
            internal ProcessWaitHandle(SafeProcessHandle processHandle)
            {
                IntPtr currentProcHandle = GetCurrentProcess();
                bool succeeded = DuplicateHandle(
                    currentProcHandle,
                    processHandle,
                    currentProcHandle,
                    out SafeWaitHandle waitHandle,
                    0,
                    false,
                    HandleOptions.DUPLICATE_SAME_ACCESS);

                if (!succeeded)
                {
                    int error = Marshal.GetHRForLastWin32Error();
                    waitHandle.Dispose();
                    Marshal.ThrowExceptionForHR(error);
                }

                this.SetSafeWaitHandle(waitHandle);
            }
        }
    }
}
