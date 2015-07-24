// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLocalAllocHandle : SafeBuffer, IDisposable
    {
        private SafeLocalAllocHandle() : base(true) { }

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

        override protected bool ReleaseHandle()
        {
            return Interop.mincore_obsolete.LocalFree(handle) == IntPtr.Zero;
        }
    }
}
