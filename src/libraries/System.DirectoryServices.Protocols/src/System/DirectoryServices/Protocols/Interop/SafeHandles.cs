// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.DirectoryServices.Protocols
{
    internal sealed class HGlobalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static IntPtr _dummyPointer = new IntPtr(1);

        internal HGlobalMemHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        protected override bool ReleaseHandle()
        {
            if (handle != _dummyPointer)
            {
                Marshal.FreeHGlobal(handle);
            }
            return true;
        }
    }
}
