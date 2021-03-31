// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern UIntPtr VirtualQuery(SafeHandle lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, UIntPtr dwLength);
    }
}
