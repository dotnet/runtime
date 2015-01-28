// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeFindHandle : SafeHandle
    {
        [System.Security.SecurityCritical]  // auto-generated_required
        internal SafeFindHandle() : base(IntPtr.Zero, true) { }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Interop.mincore.FindClose(handle);
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle == IntPtr.Zero || handle == new IntPtr(-1);
            }
        }
    }
}
