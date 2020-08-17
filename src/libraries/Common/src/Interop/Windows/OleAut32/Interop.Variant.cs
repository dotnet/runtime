// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class OleAut32
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct Variant
        {
            [FieldOffset(0)]
            public ushort varType;
            [FieldOffset(2)]
            public ushort reserved1;
            [FieldOffset(4)]
            public ushort reserved2;
            [FieldOffset(6)]
            public ushort reserved3;
            [FieldOffset(8)]
            public short boolvalue;
            [FieldOffset(8)]
            public IntPtr ptr1;
            [FieldOffset(12)]
            public IntPtr ptr2;
        }
    }
}
