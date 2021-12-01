// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeLibraryHandle() : base(true) { }

        internal SafeLibraryHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        protected override bool ReleaseHandle()
        {
            System.Runtime.InteropServices.NativeLibrary.Free(handle);
            return true;
        }
    }
}
