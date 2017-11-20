// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafePipeHandle()
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
