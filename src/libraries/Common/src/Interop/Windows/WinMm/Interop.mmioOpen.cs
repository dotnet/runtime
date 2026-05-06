// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        internal const int MMIO_READ = 0x00000000;
        internal const int MMIO_ALLOCBUF = 0x00010000;

        [LibraryImport(Libraries.WinMM, EntryPoint = "mmioOpenW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr mmioOpen(string fileName, IntPtr not_used, int flags);
    }
}
