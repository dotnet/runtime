// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        internal const int MMIO_FINDRIFF = 0x00000020;

        [LibraryImport(Libraries.WinMM)]
        internal static unsafe partial int mmioDescend(
            IntPtr hMIO,
            MMCKINFO* lpck,
            MMCKINFO* lcpkParent,
            int flags);
    }
}
