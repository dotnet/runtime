// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;

#if REGISTRY_ASSEMBLY
namespace Microsoft.Win32.SafeHandles
#else
namespace Internal.Win32.SafeHandles
#endif
{
#if REGISTRY_ASSEMBLY
    public
#else
    internal
#endif
    sealed partial class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeRegistryHandle() : base(true) { }

        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }
    }
}
