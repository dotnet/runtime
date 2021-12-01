// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // Under debug mode only, we'll want to check the error codes
        // of some of the p/invokes we make.

#if DEBUG
        private const bool SetLastErrorForDebug = true;
#else
        private const bool SetLastErrorForDebug = false;
#endif

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
        internal const uint LOCALE_ICONSTRUCTEDLOCALE   = 0x0000007d;
        internal const uint LOCALE_STIMEFORMAT          = 0x00001003;
        internal const uint LOCALE_IFIRSTDAYOFWEEK      = 0x0000100C;
        internal const uint LOCALE_RETURN_NUMBER        = 0x20000000;
        internal const uint LOCALE_NOUSEROVERRIDE       = 0x80000000;

        internal const uint LCMAP_SORTHANDLE            = 0x20000000;
        internal const uint LCMAP_HASH                  = 0x00040000;

        internal const int  COMPARE_STRING              = 0x0001;

        internal const uint TIME_NOSECONDS = 0x00000002;

        internal const int GEOCLASS_NATION       = 16;
        internal const int GEO_ISO2              =  4;
        internal const int GEOID_NOT_AVAILABLE   = -1;

        internal const string LOCALE_NAME_USER_DEFAULT = null;
        internal const string LOCALE_NAME_SYSTEM_DEFAULT = "!x-sys-default-locale";

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial int LCIDToLocaleName(int locale, char* pLocaleName, int cchName, uint dwFlags);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static partial int LocaleNameToLCID(string lpName, uint dwFlags);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial int LCMapStringEx(
                    string? lpLocaleName,
                    uint dwMapFlags,
                    char* lpSrcStr,
                    int cchSrc,
                    void* lpDestStr,
                    int cchDest,
                    void* lpVersionInformation,
                    void* lpReserved,
                    IntPtr sortHandle);

        [GeneratedDllImport("kernel32.dll", EntryPoint = "FindNLSStringEx", SetLastError = SetLastErrorForDebug)]
        internal static unsafe partial int FindNLSStringEx(
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

        [GeneratedDllImport("kernel32.dll", EntryPoint = "CompareStringEx")]
        internal static unsafe partial int CompareStringEx(
                    char* lpLocaleName,
                    uint dwCmpFlags,
                    char* lpString1,
                    int cchCount1,
                    char* lpString2,
                    int cchCount2,
                    void* lpVersionInformation,
                    void* lpReserved,
                    IntPtr lParam);

        [GeneratedDllImport("kernel32.dll", EntryPoint = "CompareStringOrdinal")]
        internal static unsafe partial int CompareStringOrdinal(
                    char* lpString1,
                    int cchCount1,
                    char* lpString2,
                    int cchCount2,
                    bool bIgnoreCase);

        [GeneratedDllImport("kernel32.dll", EntryPoint = "FindStringOrdinal", SetLastError = SetLastErrorForDebug)]
        internal static unsafe partial int FindStringOrdinal(
                    uint dwFindStringOrdinalFlags,
                    char* lpStringSource,
                    int cchSource,
                    char* lpStringValue,
                    int cchValue,
                    BOOL bIgnoreCase);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial bool IsNLSDefinedString(
                    int Function,
                    uint dwFlags,
                    IntPtr lpVersionInformation,
                    char* lpString,
                    int cchStr);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static unsafe partial Interop.BOOL GetUserPreferredUILanguages(uint dwFlags, uint* pulNumLanguages, char* pwszLanguagesBuffer, uint* pcchLanguagesBuffer);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial int GetLocaleInfoEx(string lpLocaleName, uint LCType, void* lpLCData, int cchData);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial bool EnumSystemLocalesEx(delegate* unmanaged<char*, uint, void*, BOOL> lpLocaleEnumProcEx, uint dwFlags, void* lParam, IntPtr reserved);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial bool EnumTimeFormatsEx(delegate* unmanaged<char*, void*, BOOL> lpTimeFmtEnumProcEx, string lpLocaleName, uint dwFlags, void* lParam);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static partial int GetCalendarInfoEx(string? lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, out int lpValue);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static partial int GetCalendarInfoEx(string? lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, IntPtr lpValue);

        [GeneratedDllImport("kernel32.dll")]
        internal static partial int GetUserGeoID(int geoClass);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial int GetGeoInfo(int location, int geoType, char* lpGeoData, int cchData, int LangId);

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial bool EnumCalendarInfoExEx(delegate* unmanaged<char*, uint, IntPtr, void*, BOOL> pCalInfoEnumProcExEx, string lpLocaleName, uint Calendar, string? lpReserved, uint CalType, void* lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct NlsVersionInfoEx
        {
            internal int dwNLSVersionInfoSize;
            internal int dwNLSVersion;
            internal int dwDefinedVersion;
            internal int dwEffectiveId;
            internal Guid guidCustomVersion;
        }

        [GeneratedDllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static unsafe partial bool GetNLSVersionEx(int function, string localeName, NlsVersionInfoEx* lpVersionInformation);
    }
}
