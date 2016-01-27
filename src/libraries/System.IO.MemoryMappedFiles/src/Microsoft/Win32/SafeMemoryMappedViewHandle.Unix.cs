// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return Interop.Sys.MUnmap(addr, base.ByteLength) == 0;
        }
    }
}
