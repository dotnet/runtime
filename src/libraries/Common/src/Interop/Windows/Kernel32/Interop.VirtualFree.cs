// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, ExactSpelling = true)]
        internal static extern unsafe bool VirtualFree(void* lpAddress, UIntPtr dwSize, int dwFreeType);
    }
}
