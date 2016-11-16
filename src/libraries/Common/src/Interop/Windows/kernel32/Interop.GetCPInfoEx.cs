// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        private const uint CP_ACP = 0x0u;

        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, EntryPoint = "GetCPInfoExW")]
        private extern unsafe static int GetCPInfoExW(uint CodePage, uint dwFlags, CPINFOEXW* lpCPInfoEx);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct CPINFOEXW
        {
            internal uint MaxCharSize;
            internal fixed byte DefaultChar[2];
            internal fixed byte LeadByte[12];
            internal char UnicodeDefaultChar;
            internal uint CodePage;
            internal fixed byte CodePageName[260];
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
