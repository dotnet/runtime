// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeAllocHHandle : SafeBuffer
    {
        private SafeAllocHHandle() : base(true) { }

        internal SafeAllocHHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal static SafeAllocHHandle InvalidHandle
        {
            get { return new SafeAllocHHandle(IntPtr.Zero); }
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(handle);
            }

            return true;
        }
    }
}
