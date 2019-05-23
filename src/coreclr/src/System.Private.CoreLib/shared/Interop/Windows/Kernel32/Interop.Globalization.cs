// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Kernel32
    {
        internal const uint LOCALE_ALLOW_NEUTRAL_NAMES  = 0x08000000; // Flag to allow returning neutral names/lcids for name conversion
        internal const uint LOCALE_ILANGUAGE            = 0x00000001;
        internal const uint LOCALE_SUPPLEMENTAL         = 0x00000002;
        internal const uint LOCALE_REPLACEMENT          = 0x00000008;
        internal const uint LOCALE_NEUTRALDATA          = 0x00000010;
        internal const uint LOCALE_SPECIFICDATA         = 0x00000020;
        internal const uint LOCALE_SISO3166CTRYNAME     = 0x0000005A;
        internal const uint LOCALE_SNAME                = 0x0000005C;
        internal const uint LOCALE_INEUTRAL             = 0x00000071;
        internal const uint LOCALE_SSHORTTIME           = 0x00000079;
        internal const uint LOCALE_STIMEFORMAT          = 0x00001003;
        internal const uint LOCALE_IFIRSTDAYOFWEEK      = 0x0000100C;
        internal const uint LOCALE_RETURN_NUMBER        = 0x20000000;
        internal const uint LOCALE_NOUSEROVERRIDE       = 0x80000000;

        internal const int  COMPARE_STRING              = 0x0001;

        internal const uint TIME_NOSECONDS = 0x00000002;

        internal const string LOCALE_NAME_USER_DEFAULT = null;
        internal const string LOCALE_NAME_SYSTEM_DEFAULT = "!x-sys-default-locale";


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern unsafe int LCIDToLocaleName(int locale, char *pLocaleName, int cchName, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int LocaleNameToLCID(string lpName, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern unsafe int LCMapStringEx(
                    string? lpLocaleName,
                    uint dwMapFlags,
                    char* lpSrcStr,
                    int cchSrc,
                    void* lpDestStr,
                    int cchDest,
                    void* lpVersionInformation,
                    void* lpReserved,
                    IntPtr sortHandle);

        [DllImport("kernel32.dll", EntryPoint = "FindNLSStringEx")]
        internal static extern unsafe int FindNLSStringEx(
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
        internal static extern unsafe int CompareStringEx(
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
        internal static extern unsafe int CompareStringOrdinal(
                    char* lpString1,
                    int cchCount1,
                    char* lpString2,
                    int cchCount2,
                    bool bIgnoreCase);

        [DllImport("kernel32.dll", EntryPoint = "FindStringOrdinal")]
        internal static extern unsafe int FindStringOrdinal(
                    uint dwFindStringOrdinalFlags,
                    char* lpStringSource,
                    int cchSource,
                    char* lpStringValue,
                    int cchValue,
                    int bIgnoreCase);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern unsafe bool IsNLSDefinedString(
                    int  Function,
                    uint dwFlags,
                    IntPtr lpVersionInformation,
                    char* lpString,
                    int cchStr);

#if !ENABLE_WINRT
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern Interop.BOOL GetUserPreferredUILanguages(uint dwFlags, uint* pulNumLanguages, char* pwszLanguagesBuffer, uint* pcchLanguagesBuffer);
#endif //!ENABLE_WINRT

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetLocaleInfoEx(string lpLocaleName, uint LCType, void* lpLCData, int cchData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumSystemLocalesEx(EnumLocalesProcEx lpLocaleEnumProcEx, uint dwFlags, void* lParam, IntPtr reserved);

        internal delegate BOOL EnumLocalesProcEx(char* lpLocaleString, uint dwFlags, void* lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumTimeFormatsEx(EnumTimeFormatsProcEx lpTimeFmtEnumProcEx, string lpLocaleName, uint dwFlags, void* lParam);
  
        internal delegate BOOL EnumTimeFormatsProcEx(char* lpTimeFormatString, void* lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetCalendarInfoEx(string? lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, out int lpValue);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetCalendarInfoEx(string? lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, IntPtr lpValue);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumCalendarInfoExEx(EnumCalendarInfoProcExEx pCalInfoEnumProcExEx, string lpLocaleName, uint Calendar, string? lpReserved, uint CalType, void* lParam);
        
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
        internal static extern unsafe bool GetNLSVersionEx(int function, string localeName, NlsVersionInfoEx* lpVersionInformation);
    }
}
