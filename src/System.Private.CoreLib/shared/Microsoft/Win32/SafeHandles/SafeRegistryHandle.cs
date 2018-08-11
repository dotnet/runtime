// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;

namespace Microsoft.Win32.SafeHandles
{
#if REGISTRY_ASSEMBLY
    public
#else
    internal
#endif
    sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeRegistryHandle() : base(true) { }

        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return (Interop.Advapi32.RegCloseKey(handle) == Interop.Errors.ERROR_SUCCESS);
        }
    }
}
