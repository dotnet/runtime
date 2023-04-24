// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        [LibraryImport(Libraries.WinMM)]
        internal static unsafe partial int mmioAscend(IntPtr hMIO, MMCKINFO* lpck, int flags);
    }
}
