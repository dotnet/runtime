// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;

namespace Microsoft.Win32.SafeHandles {
    internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    { 
        internal SafeLocalMemHandle() : base(true) {}
        
        internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle) {
            SetHandle(existingHandle);
        }

        override protected bool ReleaseHandle()
        {
            return Interop.Kernel32.LocalFree(handle) == IntPtr.Zero;
        }
    }
}





