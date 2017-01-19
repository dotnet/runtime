// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLocalAllocHandle : SafeBuffer, IDisposable
    {
        private SafeLocalAllocHandle() : base(true) { }

        internal static readonly SafeLocalAllocHandle Zero = new SafeLocalAllocHandle();

        internal static SafeLocalAllocHandle LocalAlloc(int cb)
        {
            SafeLocalAllocHandle result = Interop.Kernel32.LocalAlloc(Interop.Kernel32.LMEM_FIXED, (UIntPtr)cb);
            if (result.IsInvalid)
            {
                result.SetHandleAsInvalid();
                throw new OutOfMemoryException();
            }
            return result;
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

        override protected bool ReleaseHandle()
        {
            return Interop.Kernel32.LocalFree(handle) == IntPtr.Zero;
        }
    }
}
