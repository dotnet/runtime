// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32)]
        internal static extern unsafe int WideCharToMultiByte(
            uint CodePage, uint dwFlags, 
            char* lpWideCharStr, int cchWideChar, 
            byte* lpMultiByteStr, int cbMultiByte, 
            IntPtr lpDefaultChar, IntPtr lpUsedDefaultChar);

        internal const uint CP_ACP = 0;
        internal const uint WC_NO_BEST_FIT_CHARS = 0x00000400;
    }
}
