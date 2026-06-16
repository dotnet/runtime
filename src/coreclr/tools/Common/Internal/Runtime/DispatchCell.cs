// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using Internal.Runtime;

namespace System.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DispatchCell
    {
        public nint MethodTable;
        public nint Code;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DynamicDispatchCell
    {
        public DispatchCell Cell;
        public MethodTable* InterfaceType;
        public nint Slot;
    }
}
