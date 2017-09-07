// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Kernel32
    {
        internal const int  LOCALE_NAME_MAX_LENGTH      = 85;
        internal const uint LOCALE_ALLOW_NEUTRAL_NAMES  = 0x08000000; // Flag to allow returning neutral names/lcids for name conversion
        internal const uint LOCALE_SUPPLEMENTAL         = 0x00000002;
        internal const uint LOCALE_REPLACEMENT          = 0x00000008;
        internal const uint LOCALE_NEUTRALDATA          = 0x00000010;
        internal const uint LOCALE_SPECIFICDATA         = 0x00000020;
        internal const int  COMPARE_STRING              = 0x0001;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static unsafe int LCIDToLocaleName(int locale, char *pLocaleName, int cchName, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static int LocaleNameToLCID(string lpName, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static unsafe int LCMapStringEx(
                    string lpLocaleName,
                    uint dwMapFlags,
                    char* lpSrcStr,
                    int cchSrc,
                    void* lpDestStr,
                    int cchDest,
                    void* lpVersionInformation,
                    void* lpReserved,
                    IntPtr sortHandle);

        [DllImport("kernel32.dll", EntryPoint = "FindNLSStringEx")]
        internal extern static unsafe int FindNLSStringEx(
                    char* lpLocaleName,
                    uint dwFindNLSStringFlags,
                    char* lpStringSource,
                    int cchSource,
                    char* lpStringValue,
                    int cchValue,
                    int* pcchFound,
                    void* lpVersionInformation,
                    void* lpReserved,
                    IntPtr sortHandle);

        [DllImport("kernel32.dll", EntryPoint = "CompareStringEx")]
        internal extern static unsafe int CompareStringEx(
                    char* lpLocaleName,
                    uint dwCmpFlags,
                    char* lpString1,
                    int cchCount1,
                    char* lpString2,
                    int cchCount2,
                    void* lpVersionInformation,
                    void* lpReserved,
                    IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "CompareStringOrdinal")]
        internal extern static unsafe int CompareStringOrdinal(
                    char* lpString1,
                    int cchCount1,
                    char* lpString2,
                    int cchCount2,
                    bool bIgnoreCase);

        [DllImport("kernel32.dll", EntryPoint = "FindStringOrdinal")]
        internal extern static unsafe int FindStringOrdinal(
                    uint dwFindStringOrdinalFlags,
                    char* lpStringSource,
                    int cchSource,
                    char* lpStringValue,
                    int cchValue,
                    int bIgnoreCase);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static unsafe bool IsNLSDefinedString(
                    int  Function,
                    uint dwFlags,
                    IntPtr lpVersionInformation,
                    char* lpString,
                    int cchStr);

#if !PROJECTN
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern bool GetUserPreferredUILanguages(uint dwFlags, out uint pulNumLanguages, char [] pwszLanguagesBuffer, ref uint pcchLanguagesBuffer);
#endif //!PROJECTN

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetLocaleInfoEx(string lpLocaleName, uint LCType, void* lpLCData, int cchData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static bool EnumSystemLocalesEx(EnumLocalesProcEx lpLocaleEnumProcEx, uint dwFlags, void* lParam, IntPtr reserved);

        internal delegate BOOL EnumLocalesProcEx(char* lpLocaleString, uint dwFlags, void* lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static int ResolveLocaleName(string lpNameToResolve, char* lpLocaleName, int cchLocaleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static bool EnumTimeFormatsEx(EnumTimeFormatsProcEx lpTimeFmtEnumProcEx, string lpLocaleName, uint dwFlags, void* lParam);
  
        internal delegate BOOL EnumTimeFormatsProcEx(char* lpTimeFormatString, void* lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static int GetCalendarInfoEx(string lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, out int lpValue);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static int GetCalendarInfoEx(string lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, IntPtr lpValue);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static bool EnumCalendarInfoExEx(EnumCalendarInfoProcExEx pCalInfoEnumProcExEx, string lpLocaleName, uint Calendar, string lpReserved, uint CalType, void* lParam);
        
        internal delegate BOOL EnumCalendarInfoProcExEx(char* lpCalendarInfoString, uint Calendar, IntPtr lpReserved, void* lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct NlsVersionInfoEx
        {
            internal int dwNLSVersionInfoSize;
            internal int dwNLSVersion;
            internal int dwDefinedVersion;
            internal int dwEffectiveId;
            internal Guid guidCustomVersion;
        }
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal extern static unsafe bool GetNLSVersionEx(int function, string localeName, NlsVersionInfoEx* lpVersionInformation);
    }
}
