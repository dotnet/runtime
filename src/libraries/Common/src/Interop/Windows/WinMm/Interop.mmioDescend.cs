// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        internal const int MMIO_FINDRIFF = 0x00000020;

        [DllImport(Libraries.WinMM)]
        internal static extern int mmioDescend(IntPtr hMIO,
                                               [MarshalAs(UnmanagedType.LPStruct)] MMCKINFO lpck,
                                               [MarshalAs(UnmanagedType.LPStruct)] MMCKINFO lcpkParent,
                                               int flags);
    }
}
