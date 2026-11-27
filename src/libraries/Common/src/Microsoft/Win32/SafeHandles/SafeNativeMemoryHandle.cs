// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeNativeMemoryHandle : SafeBuffer
    {
        public SafeNativeMemoryHandle() : base(true) { }

        internal static unsafe SafeNativeMemoryHandle Alloc(nuint cb)
        {
            var h = new SafeNativeMemoryHandle();
            h.SetHandle((IntPtr)NativeMemory.Alloc(cb));
            h.Initialize(cb);
            return h;
        }

        protected override unsafe bool ReleaseHandle()
        {
            NativeMemory.Free((void*)handle);
            return true;
        }
    }
}
