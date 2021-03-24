// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeLibraryHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.FreeLibrary(handle);
        }
    }
}
