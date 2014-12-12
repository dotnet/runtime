// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCritical]
    public sealed class SafeRegistryHandle : SafeHandle
    {
        [System.Security.SecurityCritical]
        internal SafeRegistryHandle() : base(IntPtr.Zero, true) { }

        [System.Security.SecurityCritical]
        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return (Interop.mincore.RegCloseKey(handle) == Interop.ERROR_SUCCESS);
        }
    }
}

