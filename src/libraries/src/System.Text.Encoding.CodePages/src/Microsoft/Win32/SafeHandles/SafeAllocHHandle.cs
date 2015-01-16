// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
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

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(handle);
            }

            return true;
        }
    }
}