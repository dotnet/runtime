// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    // Reliability notes:
    // ReleaseHandle has reliability guarantee of Cer.Success, as defined by SafeHandle.
    // It gets prepared as a CER at instance construction time.

    [SecurityCritical]
    public sealed class SafeMemoryMappedFileHandle : SafeHandle
    {
        internal SafeMemoryMappedFileHandle() : base(IntPtr.Zero, true) { }

        internal SafeMemoryMappedFileHandle(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(handle);
        }

        override protected bool ReleaseHandle()
        {
            return Interop.mincore.CloseHandle(handle);
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get
            {
                return handle == IntPtr.Zero || handle == new IntPtr(-1);
            }
        }
    }
}
