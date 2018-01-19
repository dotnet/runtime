// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32.SafeHandles
{
    sealed internal class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLibraryHandle() : base(true) { }

        override protected bool ReleaseHandle()
        {
            return Interop.Kernel32.FreeLibrary(handle);
        }
    }
}
