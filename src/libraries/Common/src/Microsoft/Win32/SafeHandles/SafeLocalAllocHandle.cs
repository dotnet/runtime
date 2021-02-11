// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLocalAllocHandle : SafeBuffer
    {
        public SafeLocalAllocHandle() : base(true) { }

        internal static readonly SafeLocalAllocHandle Zero = new SafeLocalAllocHandle();

        internal static SafeLocalAllocHandle LocalAlloc(int cb)
        {
            var h = new SafeLocalAllocHandle();
            h.SetHandle(Marshal.AllocHGlobal(cb));
            h.Initialize((ulong)cb);
            return h;
        }

        // 0 is an Invalid Handle
        internal SafeLocalAllocHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal static SafeLocalAllocHandle InvalidHandle
        {
            get
            {
                return new SafeLocalAllocHandle(IntPtr.Zero);
            }
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
