// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static int OverlappedSize = IntPtr.Size * 3 + 8;

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "LocalAlloc", SetLastError = true)]
        internal static extern SafeOverlappedFree LocalAlloc_SafeOverlappedFree(int uFlags, UIntPtr sizetdwBytes);
    }
}
