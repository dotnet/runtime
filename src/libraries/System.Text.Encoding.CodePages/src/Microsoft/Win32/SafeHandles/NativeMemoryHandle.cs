// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class NativeMemoryHandle : SafeBuffer
    {
        public static unsafe NativeMemoryHandle Alloc(int length)
        {
            IntPtr memory = NativeMemoryHelper.Alloc(length);
            Debug.Assert((nint)memory != 0);
            return new NativeMemoryHandle(memory);
        }

        public NativeMemoryHandle() : base(true) { }

        private NativeMemoryHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal static NativeMemoryHandle InvalidHandle
        {
            get { return new NativeMemoryHandle(IntPtr.Zero); }
        }

        protected override unsafe bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                NativeMemoryHelper.Free(handle);
            }

            return true;
        }
    }
}
