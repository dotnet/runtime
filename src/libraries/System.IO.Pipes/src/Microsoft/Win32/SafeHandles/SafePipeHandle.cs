// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePipeHandle()
            : this(new IntPtr(DefaultInvalidHandle), true)
        {
        }

        public SafePipeHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        internal void SetHandle(int descriptor)
        {
            base.SetHandle((IntPtr)descriptor);
        }
    }
}
