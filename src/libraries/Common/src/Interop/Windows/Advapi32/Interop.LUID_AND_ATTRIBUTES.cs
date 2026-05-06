// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal partial struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }
    }
}
