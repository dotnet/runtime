// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
//
//
// Implements Microsoft.Win32.SafeHandles.SafeRegistryHandle
//
// ======================================================================================
namespace Microsoft.Win32.SafeHandles {
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;

    [System.Security.SecurityCritical]
    public sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid {
        [System.Security.SecurityCritical]
        internal SafeRegistryHandle() : base(true) {}

        [System.Security.SecurityCritical]
        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
            SetHandle(preexistingHandle);
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle() {
            return (RegCloseKey(handle) == Win32Native.ERROR_SUCCESS);
        }

        [DllImport(Win32Native.ADVAPI32)]
        [SuppressUnmanagedCodeSecurity]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int RegCloseKey(IntPtr hKey);
    }
}

