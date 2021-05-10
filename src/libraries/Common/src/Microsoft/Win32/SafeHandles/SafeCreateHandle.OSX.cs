// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// This class is a wrapper around the Create pattern in OS X where
    /// if a Create* function is called, the caller must also CFRelease
    /// on the same pointer in order to correctly free the memory.
    /// </summary>
    internal sealed partial class SafeCreateHandle : SafeHandle
    {
        public SafeCreateHandle() : base(IntPtr.Zero, true) { }

        internal SafeCreateHandle(IntPtr ptr) : base(IntPtr.Zero, true)
        {
            this.SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);

            return true;
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }
    }
}
