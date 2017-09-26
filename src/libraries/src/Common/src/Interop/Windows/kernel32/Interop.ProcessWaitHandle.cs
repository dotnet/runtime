// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
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
                SafeWaitHandle waitHandle = null;
                SafeProcessHandle currentProcHandle = Interop.Kernel32.GetCurrentProcess();
                bool succeeded = Interop.Kernel32.DuplicateHandle(
                    currentProcHandle,
                    processHandle,
                    currentProcHandle,
                    out waitHandle,
                    0,
                    false,
                    Interop.Kernel32.HandleOptions.DUPLICATE_SAME_ACCESS);

                if (!succeeded)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                this.SetSafeWaitHandle(waitHandle);
            }
        }
    }
}
