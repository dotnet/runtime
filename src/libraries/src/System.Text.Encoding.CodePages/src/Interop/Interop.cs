// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal unsafe static partial class Interop
{
    internal const uint CP_ACP = 0x0u;

    [global::System.Runtime.InteropServices.DllImport("api-ms-win-core-localization-l1-2-0.dll")]
    internal extern static int GetCPInfoExW(
               uint CodePage,
               uint dwFlags,
               CPINFOEXW* lpCPInfoEx);

    [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = global::System.Runtime.InteropServices.CharSet.Unicode)]
    internal unsafe partial struct CPINFOEXW
    {
        public uint MaxCharSize;
        public fixed byte DefaultChar[2];
        public fixed byte LeadByte[12];
        public char UnicodeDefaultChar;
        public uint CodePage;
        public fixed byte CodePageName[260];
    }
}
