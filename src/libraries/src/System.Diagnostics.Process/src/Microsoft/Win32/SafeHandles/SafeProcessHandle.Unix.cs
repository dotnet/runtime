// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  SafeProcessHandle 
**
** A wrapper for a process handle
**
** 
===========================================================*/

using System;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // On Windows, SafeProcessHandle represents the actual OS handle for the process.
        // On Unix, there's no such concept.  Instead, the implementation manufactures
        // a WaitHandle that it manually sets when the process completes; SafeProcessHandle
        // then just wraps that same WaitHandle instance.  This allows consumers that use
        // Process.{Safe}Handle to initalize and use a WaitHandle to successfull use it on
        // Unix as well to wait for the process to complete.

        private readonly SafeWaitHandle _handle;
        private readonly bool _releaseRef;

        internal SafeProcessHandle(int processId, SafeWaitHandle handle) : 
            this(handle.DangerousGetHandle(), ownsHandle: false)
        {
            ProcessId = processId;
            _handle = handle;
            handle.DangerousAddRef(ref _releaseRef);
        }

        internal int ProcessId { get; }

        protected override bool ReleaseHandle()
        {
            if (_releaseRef)
            {
                _handle.DangerousRelease();
            }
            return true;
        }
    }
}
