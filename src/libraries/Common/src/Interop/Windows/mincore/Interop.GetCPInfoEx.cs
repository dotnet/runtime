// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        private const uint CP_ACP = 0x0u;

        [DllImport(Libraries.Localization, CharSet = CharSet.Unicode, EntryPoint = "GetCPInfoExW")]
        private extern unsafe static int GetCPInfoExW(uint CodePage, uint dwFlags, CPINFOEXW* lpCPInfoEx);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct CPINFOEXW
        {
            public uint MaxCharSize;
            public fixed byte DefaultChar[2];
            public fixed byte LeadByte[12];
            public char UnicodeDefaultChar;
            public uint CodePage;
            public fixed byte CodePageName[260];
        }

        internal static bool TryGetACPCodePage(out int codePage)
        {
            codePage = 0;
            // Note: GetACP is not available in the Windows Store Profile, but calling
            // GetCPInfoEx with the value CP_ACP (0) yields the same result.
            CPINFOEXW _cpInfo;
            unsafe
            {
                CPINFOEXW* _lpCPInfoExPtr = &(_cpInfo);
                if (GetCPInfoExW(CP_ACP, 0, _lpCPInfoExPtr) != 0)
                {
                    codePage = (int)_cpInfo.CodePage;
                    return true;
                }
            }

            return false;
        }
    }
}
