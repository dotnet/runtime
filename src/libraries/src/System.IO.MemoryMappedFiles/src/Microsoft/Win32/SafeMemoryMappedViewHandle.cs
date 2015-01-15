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
#pragma warning disable 0618    // SafeBuffer is obsolete
    public sealed class SafeMemoryMappedViewHandle : SafeBuffer
#pragma warning restore
    {
        internal SafeMemoryMappedViewHandle() : base(true) { }

        internal SafeMemoryMappedViewHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            base.SetHandle(handle);
        }

        override protected bool ReleaseHandle()
        {
            if (Interop.mincore.UnmapViewOfFile(handle) != 0)
            {
                handle = IntPtr.Zero;
                return true;
            }
            return false;
        }
    }
}
