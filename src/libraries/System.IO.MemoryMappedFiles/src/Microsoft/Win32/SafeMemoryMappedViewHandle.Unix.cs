// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.MemoryMappedFiles;

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
            // An empty view maps one byte, but its reported length remains zero.
            return Interop.Sys.MUnmap(addr, Math.Max(MemoryMappedView.EmptyViewNativeLength, base.ByteLength)) == 0;
        }
    }
}
