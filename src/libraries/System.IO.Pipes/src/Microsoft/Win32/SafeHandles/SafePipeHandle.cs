// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        /// <summary>
        /// Creates a SafePipeHandle around a Windows pipe handle.
        /// </summary>
        /// <param name="preexistingHandle">Handle to wrap</param>
        /// <param name="ownsHandle">Whether to control the handle lifetime</param>
        public SafePipeHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(preexistingHandle, ownsHandle);
        }
    }
}
