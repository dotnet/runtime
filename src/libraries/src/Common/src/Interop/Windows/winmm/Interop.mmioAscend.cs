// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        [DllImport(Libraries.WinMM)]
        internal static extern int mmioAscend(IntPtr hMIO, MMCKINFO lpck, int flags);
    }
}
