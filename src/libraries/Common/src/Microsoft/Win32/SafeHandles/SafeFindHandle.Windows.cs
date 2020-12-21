// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeFindHandle : SafeHandle
    {
        internal SafeFindHandle() : base(IntPtr.Zero, true) { }

        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.FindClose(handle);
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero || handle == new IntPtr(-1);
            }
        }
    }
}
