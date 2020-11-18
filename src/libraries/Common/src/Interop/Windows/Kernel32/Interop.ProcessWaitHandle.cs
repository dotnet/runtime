// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

internal partial class Interop
{
    internal partial class Kernel32
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
