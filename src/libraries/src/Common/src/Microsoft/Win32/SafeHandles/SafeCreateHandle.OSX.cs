// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// This class is a wrapper around the Create pattern in OS X where
    /// if a Create* function is called, the caler must also CFRelease 
    /// on the same pointer in order to correctly free the memory.
    /// </summary>
    [System.Security.SecurityCritical]
    internal sealed partial class SafeCreateHandle : SafeHandle
    {
        internal SafeCreateHandle() : base(IntPtr.Zero, true) { }

        internal SafeCreateHandle(IntPtr ptr) : base(IntPtr.Zero, true)
        {
            this.SetHandle(ptr);
        }

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);
            
            return true;
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle == IntPtr.Zero;
            }
        }
    }
}
