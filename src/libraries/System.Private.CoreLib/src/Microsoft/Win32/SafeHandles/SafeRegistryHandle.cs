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
        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeRegistryHandle" />.
        /// </summary>
        public SafeRegistryHandle() : base(true) { }

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeRegistryHandle" /> around a registry handle.
        /// </summary>
        /// <param name="preexistingHandle">Handle to wrap</param>
        /// <param name="ownsHandle">Whether to control the handle lifetime</param>
        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }
    }
}
