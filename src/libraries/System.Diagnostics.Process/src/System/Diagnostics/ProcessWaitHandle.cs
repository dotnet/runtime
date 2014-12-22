// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics
{
    internal class ProcessWaitHandle : WaitHandle
    {
        internal ProcessWaitHandle(SafeProcessHandle processHandle)
            : base()
        {
            SafeWaitHandle waitHandle = null;
            SafeProcessHandle currentProcHandle = Interop.mincore.GetCurrentProcess();
            bool succeeded = Interop.mincore.DuplicateHandle(
                currentProcHandle,
                processHandle,
                currentProcHandle,
                out waitHandle,
                0,
                false,
                Interop.DUPLICATE_SAME_ACCESS);

            if (!succeeded)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            this.SetSafeWaitHandle(waitHandle);
        }
    }
}
