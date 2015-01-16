// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal unsafe static partial class Interop
{
    internal const uint CP_ACP = 0x0u;

    [DllImport("api-ms-win-core-localization-l1-2-0.dll")]
    internal extern static int GetCPInfoExW(uint CodePage, uint dwFlags, CPINFOEXW* lpCPInfoEx);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal unsafe struct CPINFOEXW
    {
        public uint MaxCharSize;
        public fixed byte DefaultChar[2];
        public fixed byte LeadByte[12];
        public char UnicodeDefaultChar;
        public uint CodePage;
        public fixed byte CodePageName[260];
    }
}
