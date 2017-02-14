// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  File:    newapis.h
//


//
//  Purpose:  functions that need to be emulated on downlevel platforms.
//
////////////////////////////////////////////////////////////////////////////

#ifndef MUI_LANGUAGE_NAME
#define MUI_LANGUAGE_NAME 0
#endif

#ifndef CALINFO_ENUMPROCEXEX
typedef BOOL (CALLBACK* CALINFO_ENUMPROCEXEX)(LPWSTR, CALID, LPWSTR, LPARAM);
#endif

#ifndef DATEFMT_ENUMPROCEXEX
typedef BOOL (CALLBACK* DATEFMT_ENUMPROCEXEX)(LPWSTR, CALID, LPARAM);
#endif

#ifndef TIMEFMT_ENUMPROCEX
typedef BOOL (CALLBACK* TIMEFMT_ENUMPROCEX)(LPWSTR, LPARAM);
#endif

#ifndef LOCALE_ENUMPROCEX
typedef BOOL (CALLBACK* LOCALE_ENUMPROCEX)(LPWSTR, DWORD, LPARAM);
#endif

#if !defined(LPNLSVERSIONINFOEX)
#define LPNLSVERSIONINFOEX LPNLSVERSIONINFO
#endif

#ifndef COMPARE_OPTIONS_ORDINAL
#define COMPARE_OPTIONS_ORDINAL            0x40000000
#endif

#ifndef LINGUISTIC_IGNORECASE
#define LINGUISTIC_IGNORECASE 0x00000010  // linguistically appropriate 'ignore case'
#endif

#ifndef FIND_STARTSWITH
#define FIND_STARTSWITH  0x00100000       // see if value is at the beginning of source
#endif

#ifndef FIND_ENDSWITH
#define FIND_ENDSWITH   0x00200000        // see if value is at the end of source
#endif

#ifndef FIND_FROMSTART
#define FIND_FROMSTART  0x00400000       // look for value in source, starting at the beginning
#endif

#ifndef FIND_FROMEND
#define FIND_FROMEND  0x00800000        // look for value in source, starting at the end
#endif

#ifndef NORM_LINGUISTIC_CASING
#define NORM_LINGUISTIC_CASING    0x08000000  // use linguistic rules for casing
#endif

#ifndef LCMAP_LINGUISTIC_CASING
#define LCMAP_LINGUISTIC_CASING   0x01000000  // use linguistic rules for casing
#endif

#ifndef LCMAP_TITLECASE
#define LCMAP_TITLECASE           0x00000300  // Title Case Letters
#endif

#ifndef LCMAP_SORTHANDLE
#define LCMAP_SORTHANDLE   0x20000000
#endif

#ifndef LCMAP_HASH
#define LCMAP_HASH   0x00040000
#endif

#ifndef LOCALE_ALL
#define LOCALE_ALL                0                     // enumerate all named based locales
#endif // LOCALE_ALL

#ifndef LOCALE_WINDOWS
#define LOCALE_WINDOWS            0x00000001            // shipped locales and/or replacements for them
#endif // LOCALE_WINDOWS

#ifndef LOCALE_SUPPLEMENTAL
#define LOCALE_SUPPLEMENTAL       0x00000002            // supplemental locales only
#endif // LOCALE_SUPPLEMENTAL

#ifndef LOCALE_ALTERNATE_SORTS
#define LOCALE_ALTERNATE_SORTS    0x00000004            // alternate sort locales
#endif // LOCALE_ALTERNATE_SORTS

#ifndef LOCALE_NEUTRALDATA
#define LOCALE_NEUTRALDATA        0x00000010            // Locales that are "neutral" (language only, region data is default)
#endif // LOCALE_NEUTRALDATA

#ifndef LOCALE_SPECIFICDATA
#define LOCALE_SPECIFICDATA       0x00000020            // Locales that contain language and region data
#endif // LOCALE_SPECIFICDATA

#ifndef LOCALE_INEUTRAL
#define LOCALE_INEUTRAL           0x00000071            // Returns 0 for specific cultures, 1 for neutral cultures.
#endif // LOCALE_INEUTRAL

#ifndef LOCALE_SSORTLOCALE
#define LOCALE_SSORTLOCALE        0x0000007b            // Name of locale to use for sorting/collation/casing behavior.
#endif // LOCALE_SSORTLOCALE

#ifndef LOCALE_RETURN_NUMBER
#define LOCALE_RETURN_NUMBER      0x20000000            // return number instead of string
#endif // LOCALE_RETURN_NUMBER

#ifndef LOCALE_ALLOW_NEUTRAL_NAMES
#define LOCALE_ALLOW_NEUTRAL_NAMES    0x08000000   //Flag to allow returning neutral names/lcids for name conversion
#endif // LOCALE_ALLOW_NEUTRAL_NAMES

#ifndef LOCALE_SNAME
#define LOCALE_SNAME                0x0000005c
#endif

#define FIND_NLS_STRING_FLAGS_NEGATION (~(FIND_STARTSWITH | FIND_ENDSWITH | FIND_FROMSTART | FIND_FROMEND))
#define CASING_BITS                    (NORM_LINGUISTIC_CASING | LINGUISTIC_IGNORECASE | NORM_IGNORECASE)


#ifndef __out_xcount_opt
#define __out_xcount_opt(var)
#endif

 // TODO: NLS Arrowhead -This isn't really right, custom locales could start with en- and have different sort behavior
 // IS_FAST_COMPARE_LOCALE is used to do the fast ordinal index of when having string of Lower Ansi codepoints
 // that less than 0x80. There are some locales that we cannot do optimization with, like Turkish and Azeri
 // because of Turkish I problem and Humgerian because of lower Ansi compressions.
#define IS_FAST_COMPARE_LOCALE(loc) \
    (wcsncmp(loc,W("tr-"),3)!=0 && wcsncmp(loc,W("az-"),3)!=0 && wcsncmp(loc,W("hu-"),3)!=0)

#define TURKISH_LOCALE_NAME             W("tr-TR")
#define AZERBAIJAN_LOCALE_NAME          W("az-Latn-AZ")
#define TURKISH_SORTING_LOCALE_NAME     W("tr-TR_turkic")
#define AZERBAIJAN_SORTING_LOCALE_NAME  W("az-Latn-AZ_turkic")

#define MUI_MERGE_SYSTEM_FALLBACK            0x10
#define MUI_MERGE_USER_FALLBACK              0x20


namespace NewApis
{
#if defined(FEATURE_CORESYSTEM)
    __inline bool IsWindows7Platform()
    {
        return true;
    }

    __inline bool IsVistaPlatform()
    {
        return false;
    }

    __inline BOOL IsZhTwSku()
    {
        return false;
    }

#else
    // Return true if we're on Windows 7 or up (ie: if we have neutral native support and sorting knows about versions)
    __inline bool IsWindows7Platform()
    {
        static int isRunningOnWindows7 = -1; // -1 notinitialized, 0 not running on Windows 7, other value means running on Windows 7

        if (isRunningOnWindows7 == -1)
        {
            OSVERSIONINFOEX sVer;
            ZeroMemory(&sVer, sizeof(OSVERSIONINFOEX));
            sVer.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
            sVer.dwMajorVersion = 6;
            sVer.dwMinorVersion = 1;
            sVer.dwPlatformId = VER_PLATFORM_WIN32_NT;

            DWORDLONG dwlConditionMask = 0;
            VER_SET_CONDITION(dwlConditionMask, CLR_VER_MAJORVERSION, VER_GREATER_EQUAL);
            VER_SET_CONDITION(dwlConditionMask, CLR_VER_MINORVERSION, VER_GREATER_EQUAL);
            VER_SET_CONDITION(dwlConditionMask, CLR_VER_PLATFORMID, VER_EQUAL);

            if(VerifyVersionInfo(&sVer, CLR_VER_MAJORVERSION|CLR_VER_MINORVERSION|CLR_VER_PLATFORMID, dwlConditionMask))
            {
                isRunningOnWindows7 = 1;
            }
            else
            {
                isRunningOnWindows7 = 0;
            }
        }

        return isRunningOnWindows7 == 1;
    }

    //
    // IsVistaPlatform return true if running on Vista and false if running on pre or post Vista
    //
    __inline BOOL IsVistaPlatform()
    {
        static int isRunningOnVista = -1; // -1 notinitialized, 0 not running on Vista, other value meanse running on Vista

        if (isRunningOnVista == -1)
        {
            OSVERSIONINFOEX sVer;
            ZeroMemory(&sVer, sizeof(OSVERSIONINFOEX));
            sVer.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
            sVer.dwMajorVersion = 6;
            sVer.dwMinorVersion = 0;
            sVer.dwPlatformId = VER_PLATFORM_WIN32_NT;

            DWORDLONG dwlConditionMask = 0;
            VER_SET_CONDITION(dwlConditionMask, CLR_VER_MAJORVERSION, VER_EQUAL);
            VER_SET_CONDITION(dwlConditionMask, CLR_VER_MINORVERSION, VER_EQUAL);
            VER_SET_CONDITION(dwlConditionMask, CLR_VER_PLATFORMID, VER_EQUAL);

            if(VerifyVersionInfo(&sVer, CLR_VER_MAJORVERSION|CLR_VER_MINORVERSION|CLR_VER_PLATFORMID, dwlConditionMask))
            {
                isRunningOnVista = 1;
            }
            else
            {
                isRunningOnVista = 0;
            }
        }

        return isRunningOnVista == 1;
    }


    __inline BOOL IsZhTwSku()
    {
        const INT32 LANGID_ZH_TW = 0x0404;
        LPCWSTR DEFAULT_REGION_NAME_0404 = W("\x53f0\x7063");

        if(::GetSystemDefaultUILanguage() == LANGID_ZH_TW)
        {
            WCHAR wszBuffer[32];
            int result = ::GetLocaleInfoW(LANGID_ZH_TW, LOCALE_SNATIVECTRYNAME, wszBuffer, 32);
            if (result)
            {
                if (wcsncmp(wszBuffer, DEFAULT_REGION_NAME_0404, 3) != 0)
                {
                    return true;
                } 
            }
        }
        return false;
    }
#endif // FEATURE_CORESYSTEM

    __inline BOOL NotLeakingFrameworkOnlyCultures(__in LPCWSTR lpLocaleName)
    {
        return wcscmp(lpLocaleName, W("zh-CHS")) != 0
            && wcscmp(lpLocaleName, W("zh-CHT")) != 0;
    }

    // System/user defaults
    __success(return == TRUE) BOOL
    GetUserPreferredUILanguages (__in DWORD dwFlags, __out PULONG pulNumLanguages, __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer, __in PULONG pcchLanguagesBuffer);

    __success(return > 0) int
    GetSystemDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName);

    __success(return != 0) int
    GetUserDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName);

    // Comparison functions (ala CompareInfo)
    int CompareStringEx(__in LPCWSTR lpLocaleName, __in DWORD dwCmpFlags, __in_ecount(cchCount1) LPCWSTR lpString1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR lpString2,
                                                __in int cchCount2, __in_opt LPNLSVERSIONINFO lpVersionInformation, __in_opt LPVOID lpReserved, __in_opt LPARAM lParam );

    int CompareStringOrdinal(__in_ecount(cchCount1) LPCWSTR lpString1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR lpString2, __in int cchCount2, __in BOOL bIgnoreCase);

    int LCMapStringEx (__in LPCWSTR lpLocaleName, __in DWORD dwMapFlags, __in_ecount(cchSrc) LPCWSTR lpSrcStr, __in int cchSrc,
                                          __out_xcount_opt(cchDest) LPWSTR lpDestStr, __in int cchDest, __in_opt LPNLSVERSIONINFO lpVersionInformation, __in_opt LPVOID lpReserved, __in_opt LPARAM lParam );

    LPWSTR GetLingusticLocaleName(__in LPWSTR pLocaleName, __in DWORD dwFlags);

    int IndexOfString(__in LPCWSTR lpLocaleName,
                      __in_ecount(cchCount1) LPCWSTR pString1,   // String to search in
                      __in int  cchCount1,                       // length of pString1
                      __in_ecount(cchCount2) LPCWSTR pString2,   // String we're looking for
                      __in int cchCount2,                        // length of pString2
                      __in DWORD dwFlags,                        // search flags
                      __in BOOL startWith,
                      __out_opt LPINT pcchFound);

    int LastIndexOfString(__in LPCWSTR lpLocaleName,
                          __in_ecount(cchCount1) LPCWSTR pString1,   // String to search in
                          __in int  cchCount1,                       // length of pString1
                          __in_ecount(cchCount2) LPCWSTR pString2,    // String we're looking for
                          __in int cchCount2,                        // length of pString2
                          __in DWORD dwFlags,
                          __in BOOL endWith,
                          __out_opt LPINT pcchFound);

    int FindNLSStringEx(__in LPCWSTR lpLocaleName,
                        __in DWORD dwFindNLSStringFlags,
                        __in_ecount(cchSource) LPCWSTR lpStringSource,
                        __in int cchSource,
                        __in_ecount(cchValue) LPCWSTR lpStringValue,
                        __in int cchValue,
                        __out_opt LPINT pcchFound,
                        __in_opt LPNLSVERSIONINFO lpVersionInformation,
                        __in_opt LPVOID lpReserved,
                        __in_opt LPARAM lParam);

    BOOL IsNLSDefinedString(__in NLS_FUNCTION Function, __in DWORD dwFlags, __in_opt LPNLSVERSIONINFOEX lpVersionInfo, __in LPCWSTR lpString, __in int cchStr );

    // Calendar and locale information
    __success(return != 0) int
    GetCalendarInfoEx(__in LPCWSTR lpLocaleName, __in CALID Calendar, __in_opt LPCWSTR pReserved, __in CALTYPE CalType, __out_ecount_opt(cchData) LPWSTR lpCalData, __in int cchData, __out_opt LPDWORD lpValue );

    __success(return != 0) int
    GetLocaleInfoEx (__in LPCWSTR lpLocaleName, __in LCTYPE LCType, __out_ecount_opt(cchData) LPWSTR lpLCData, __in int cchData);

    __success(return != 0) int
    GetDateFormatEx(__in LPCWSTR lpLocaleName, __in DWORD dwFlags, __in_opt CONST SYSTEMTIME* lpDate, __in_opt LPCWSTR lpFormat,
                                            __out_ecount(cchDate) LPWSTR lpDateStr, __in int cchDate, __in_opt LPCWSTR lpCalendar);

    // Enumeration functions
    __success(return != 0) BOOL
    EnumDateFormatsExEx (DATEFMT_ENUMPROCEXEX lpDateFmtEnumProcExEx, LPCWSTR lpLocaleName, DWORD dwFlags, LPARAM lParam);
    __success(return != 0)
    BOOL EnumTimeFormatsEx(TIMEFMT_ENUMPROCEX lpTimeFmtEnumProcEx, LPCWSTR lpLocaleName,  DWORD dwFlags, LPARAM lParam);
    __success(return != 0)
    BOOL EnumCalendarInfoExEx(CALINFO_ENUMPROCEXEX pCalInfoEnumProcExEx, LPCWSTR lpLocaleName, CALID Calendar, CALTYPE CalType, LPARAM lParam);

    int LCIDToLocaleName(__in LCID Locale, __out_ecount_opt(cchName) LPWSTR lpName, __in int cchName, __in DWORD dwFlags);
    LCID LocaleNameToLCID(__in LPCWSTR lpName , __in DWORD dwFlags);

    int ResolveLocaleName(__in LPCWSTR lpNameToResolve, __in_ecount_opt(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName);

    __success(return == TRUE) BOOL
    GetThreadPreferredUILanguages(__in DWORD dwFlags,
                                       __out PULONG pulNumLanguages,
                                       __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer,
                                       __inout PULONG pcchLanguagesBuffer);

    BOOL WINAPI EnumSystemLocalesEx(__in LOCALE_ENUMPROCEX lpLocaleEnumProc,
                                    __in DWORD dwFlags,
                                    __in LPARAM lParam,
                                    __in_opt LPVOID lpReserved);

};



