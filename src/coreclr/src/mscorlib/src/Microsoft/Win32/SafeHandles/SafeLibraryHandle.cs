// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32 {
    using Microsoft.Win32.SafeHandles;
    using System.Security.Permissions;

    sealed internal class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid {
        internal SafeLibraryHandle() : base(true) {}

        override protected bool ReleaseHandle()
        {
            return UnsafeNativeMethods.FreeLibrary(handle);
        }
    }
}
