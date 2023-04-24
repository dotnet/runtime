// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetCPInfoExW", StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial Interop.BOOL GetCPInfoExW(uint CodePage, uint dwFlags, CPINFOEXW* lpCPInfoEx);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct CPINFOEXW
        {
            internal uint MaxCharSize;
            internal fixed byte DefaultChar[2];
            internal fixed byte LeadByte[12];
            internal char UnicodeDefaultChar;
            internal uint CodePage;
            internal fixed char CodePageName[MAX_PATH];
        }

        internal static unsafe int GetLeadByteRanges(int codePage, byte[] leadByteRanges)
        {
            int count = 0;
            CPINFOEXW cpInfo;
            if (GetCPInfoExW((uint)codePage, 0, &cpInfo) != BOOL.FALSE)
            {
                // we don't care about the last 2 bytes as those are nulls
                for (int i = 0; i < 10 && leadByteRanges[i] != 0; i += 2)
                {
                    leadByteRanges[i] = cpInfo.LeadByte[i];
                    leadByteRanges[i + 1] = cpInfo.LeadByte[i + 1];
                    count++;
                }
            }
            return count;
        }

        internal static unsafe bool TryGetACPCodePage(out int codePage)
        {
            // Note: GetACP is not available in the Windows Store Profile, but calling
            // GetCPInfoEx with the value CP_ACP (0) yields the same result.
            CPINFOEXW cpInfo;
            if (GetCPInfoExW(CP_ACP, 0, &cpInfo) != BOOL.FALSE)
            {
                codePage = (int)cpInfo.CodePage;
                return true;
            }
            else
            {
                codePage = 0;
                return false;
            }
        }
    }
}
