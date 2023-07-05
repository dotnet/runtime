// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32)]
        internal static unsafe partial int WideCharToMultiByte(
            uint CodePage, uint dwFlags,
            char* lpWideCharStr, int cchWideChar,
            byte* lpMultiByteStr, int cbMultiByte,
            byte* lpDefaultChar, BOOL* lpUsedDefaultChar);

        internal const uint CP_ACP = 0;
        internal const uint WC_NO_BEST_FIT_CHARS = 0x00000400;
    }
}
