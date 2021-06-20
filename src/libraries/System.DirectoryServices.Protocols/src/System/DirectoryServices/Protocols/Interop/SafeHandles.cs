// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.Protocols
{
    internal sealed class NativeMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static IntPtr _dummyPointer = new IntPtr(1);

        internal NativeMemoryHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        protected override bool ReleaseHandle()
        {
            if (handle != _dummyPointer)
            {
                NativeMemoryHelper.Free(handle);
            }
            return true;
        }
    }
}
