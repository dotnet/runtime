// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeNativeMemoryHandle : SafeBuffer
    {
        public SafeNativeMemoryHandle() : base(true) { }

        internal static readonly SafeNativeMemoryHandle Zero = new SafeNativeMemoryHandle();

        internal static unsafe SafeNativeMemoryHandle Alloc(int cb)
        {
            var h = new SafeNativeMemoryHandle();
            h.SetHandle((nint)NativeMemory.Alloc((uint)cb));
            h.Initialize((ulong)cb);
            return h;
        }

        // 0 is an Invalid Handle
        private SafeNativeMemoryHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal static SafeNativeMemoryHandle InvalidHandle
        {
            get
            {
                return new SafeNativeMemoryHandle(IntPtr.Zero);
            }
        }

        protected override unsafe bool ReleaseHandle()
        {
            NativeMemory.Free((void*)(nint)handle);
            return true;
        }
    }
}
