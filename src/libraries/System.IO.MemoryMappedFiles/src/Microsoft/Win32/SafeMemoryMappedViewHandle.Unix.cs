// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedViewHandle
    {
        internal SafeMemoryMappedViewHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            IntPtr addr = handle;
            handle = new IntPtr(-1);
            return Interop.libc.munmap(addr, (IntPtr)base.ByteLength) == 0;
        }
    }
}
