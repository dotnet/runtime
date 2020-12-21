// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLsaMemoryHandle : SafeBuffer
    {
        private SafeLsaMemoryHandle() : base(true) { }

        // 0 is an Invalid Handle
        internal SafeLsaMemoryHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return Interop.Advapi32.LsaFreeMemory(handle) == 0;
        }
    }
}
