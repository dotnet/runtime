// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  File:    downlevel.h
// 


// 
//  Purpose:  emulation on downlevel platforms.
//
////////////////////////////////////////////////////////////////////////////



// Vista LCTYPES
#ifndef LOCALE_SNAN
#define LOCALE_SNAN                   0x00000069   // Not a Number
#endif

#ifndef LOCALE_SPOSINFINITY
#define LOCALE_SPOSINFINITY           0x0000006a   // + Infinity
#endif

#ifndef LOCALE_SNEGINFINITY
#define LOCALE_SNEGINFINITY           0x0000006b   // - Infinity
#endif

#ifndef LOCALE_SPARENT
#define LOCALE_SPARENT                0x0000006d   // Fallback name for resources, eg "en" for "en-US"
#endif

// Win7 LCTYPES
#ifndef LOCALE_IREADINGLAYOUT
#define LOCALE_IREADINGLAYOUT         0x00000070   // Returns one of the following 4 reading layout values:
                                                   // 0 - Left to right (eg en-US)
                                                   // 1 - Right to left (eg arabic locales)
                                                   // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
                                                   // 3 - Vertical top to bottom with columns proceeding to the right
#endif

#ifndef LOCALE_INEUTRAL
#define LOCALE_INEUTRAL               0x00000071   // Returns 0 for specific cultures, 1 for neutral cultures.
#endif

// Win7 LCTypes
//
// These are the various forms of the name of the locale:
//
#define LOCALE_SLOCALIZEDDISPLAYNAME  0x00000002   // localized name of locale, eg "German (Germany)" in UI language
#define LOCALE_SENGLISHDISPLAYNAME    0x00000072   // Display name (language + country usually) in English, eg "German (Germany)"
#define LOCALE_SNATIVEDISPLAYNAME     0x00000073   // Display name in native locale language, eg "Deutsch (Deutschland)

#define LOCALE_SLOCALIZEDLANGUAGENAME 0x0000006f   // Language Display Name for a language, eg "German" in UI language
#define LOCALE_SENGLISHLANGUAGENAME   0x00001001   // English name of language, eg "German"
#define LOCALE_SNATIVELANGUAGENAME    0x00000004   // native name of language, eg "Deutsch"

#define LOCALE_SLOCALIZEDCOUNTRYNAME  0x00000006   // localized name of country, eg "Germany" in UI language
#define LOCALE_SENGLISHCOUNTRYNAME    0x00001002   // English name of country, eg "Germany"
#define LOCALE_SNATIVECOUNTRYNAME     0x00000008   // native name of country, eg "Deutschland"

#define LOCALE_INEGATIVEPERCENT       0x00000074   // Returns 0-11 for the negative percent format
#define LOCALE_IPOSITIVEPERCENT       0x00000075   // Returns 0-3 for the positive percent formatIPOSITIVEPERCENT
#define LOCALE_SPERCENT               0x00000076   // Returns the percent symbol
#define LOCALE_SPERMILLE              0x00000077   // Returns the permille (U+2030) symbol
#define LOCALE_SMONTHDAY              0x00000078   // Returns the preferred month/day format
#define LOCALE_SSHORTTIME             0x00000079   // Returns the preferred short time format (ie: no seconds, just h:mm)
#define LOCALE_SOPENTYPELANGUAGETAG   0x0000007a   // Open type language tag, eg: "latn" or "dflt"
#define LOCALE_SSORTLOCALE            0x0000007b   // Name of locale to use for sorting/collation/casing behavior.

// TODO: These didn't make it to windows
// const LCTYPE RESERVED_SADERA               = 0x000008b;   // Era name for gregorian calendar (ie: A.D.)
// const LCTYPE RESERVED_SABBREVADERA         = 0x000008c;   // Abbreviated era name for gregorian calendar (ie: AD)

// Vista CALTypes
#ifndef CAL_SSHORTESTDAYNAME1
#define CAL_SSHORTESTDAYNAME1 0x00000031
#define CAL_SSHORTESTDAYNAME2 0x00000032
#define CAL_SSHORTESTDAYNAME3 0x00000033
#define CAL_SSHORTESTDAYNAME4 0x00000034
#define CAL_SSHORTESTDAYNAME5 0x00000035
#define CAL_SSHORTESTDAYNAME6 0x00000036
#define CAL_SSHORTESTDAYNAME7 0x00000037
#endif

// Win7 CALTypes
#define CAL_SMONTHDAY             0x00000038  // Month/day format
#define CAL_SABBREVERASTRING      0x00000039  // Abbreviated era string (eg: AD)

// Vista linguistic comparison flags
#ifndef LINGUISTIC_IGNORECASE
#define LINGUISTIC_IGNORECASE      0x00000010  // linguistically appropriate 'ignore case'
#endif

#ifndef LINGUISTIC_IGNOREDIACRITIC
#define LINGUISTIC_IGNOREDIACRITIC 0x00000020  // linguistically appropriate 'ignore nonspace'
#endif

#ifndef NORM_LINGUISTIC_CASING
#define NORM_LINGUISTIC_CASING    0x08000000  // use linguistic rules for casing
#endif

#ifndef LCMAP_TITLECASE
#define LCMAP_TITLECASE 0x00000300     // reserved for title case behavior
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

#ifndef HIGH_SURROGATE_START
#define HIGH_SURROGATE_START  0xd800
#define HIGH_SURROGATE_END    0xdbff
#define LOW_SURROGATE_START   0xdc00
#define LOW_SURROGATE_END     0xdfff
#endif

#ifndef PRIVATE_USE_BEGIN
#define PRIVATE_USE_BEGIN     0xe000
#define PRIVATE_USE_END       0xf8ff
#endif

#ifndef LCMAP_TITLECASE
#define LCMAP_TITLECASE       0x00000300  // Title Case Letters
#endif

#ifndef __out_xcount_opt
#define __out_xcount_opt(var) __out
#endif 

namespace DownLevel
{
    // User /system defaults
    // TODO: I don't think we need all of these.
    int GetSystemDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName);
    __success(return == 1) DWORD GetUserPreferredUILanguages (__in DWORD dwFlags, __out PULONG pulNumLanguages, __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer, __in PULONG pcchLanguagesBuffer);
    int GetUserDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName);

    // Locale and calendar information
    int GetLocaleInfoEx (__in LPCWSTR lpLocaleName, __in LCTYPE LCType, __out_ecount_opt(cchData) LPWSTR lpLCData, __in int cchData);    
    int GetDateFormatEx(__in LPCWSTR lpLocaleName, __in DWORD dwFlags, __in_opt CONST SYSTEMTIME* lpDate, __in_opt LPCWSTR lpFormat, 
                             __out_ecount(cchDate) LPWSTR lpDateStr, __in int cchDate, __in_opt LPCWSTR lpCalendar);    
    __success(return != 0)
    int GetCalendarInfoEx(__in LPCWSTR lpLocaleName,
                          __in CALID Calendar,
                          __in_opt LPCWSTR pReserved,
                          __in CALTYPE CalType,
                          __out_ecount_opt(cchData) LPWSTR lpCalData,
                          __in int cchData,
                          __out_opt LPDWORD lpValue);

    // Compareinfo type information
    int TurkishCompareStringIgnoreCase(LCID lcid, DWORD dwCmpFlags, LPCWSTR lpString1, int cchCount1, LPCWSTR lpString2, int cchCount2);

    int CompareStringEx(__in LPCWSTR lpLocaleName, __in DWORD dwCmpFlags, __in_ecount(cchCount1) LPCWSTR lpString1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR lpString2,
                                                __in int cchCount2, __in_opt LPNLSVERSIONINFO lpVersionInformation, __in_opt LPVOID lpReserved, __in_opt LPARAM lParam );

    int CompareStringOrdinal(__in_ecount(cchCount1) LPCWSTR string1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR string2, __in int cchCount2, __in BOOL bIgnoreCase);

    __success(return != 0)
    int LCMapStringEx(__in LPCWSTR lpLocaleName, 
                      __in DWORD dwMapFlags, 
                      __in_ecount(cchSrc) LPCWSTR lpSrcStr, 
                      __in int cchSrc, 
                      __out_xcount_opt(cchDest) LPWSTR lpDestStr,
                      __in int cchDest, 
                      __in_opt LPNLSVERSIONINFO lpVersionInformation, 
                      __in_opt LPVOID lpReserved, 
                      __in_opt LPARAM lParam);

    __success(return != -1)
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
    
    BOOL IsNLSDefinedString(NLS_FUNCTION Function, DWORD dwFlags, LPNLSVERSIONINFOEX lpVersionInfo, LPCWSTR lpString, int cchStr );

    // Enumerations
    namespace LegacyCallbacks
    {
        BOOL EnumDateFormatsExEx(DATEFMT_ENUMPROCEXEX lpDateFmtEnumProcExEx, LPCWSTR lpLocaleName, DWORD dwFlags, LPARAM lParam);
        BOOL EnumTimeFormatsEx(TIMEFMT_ENUMPROCEX lpTimeFmtEnumProcEx, LPCWSTR lpLocaleName,  DWORD dwFlags, LPARAM lParam);
        BOOL EnumCalendarInfoExEx(CALINFO_ENUMPROCEXEX pCalInfoEnumProcExEx, LPCWSTR lpLocaleName, CALID Calendar, LPCWSTR lpReserved, CALTYPE CalType, LPARAM lParam);
    }        

    // This is where we fudge data the OS doesn't know (even on Vista)
    namespace UplevelFallback
    {
        __success(return != 0)
        int LCMapStringEx(__in LPCWSTR lpLocaleName, 
                          __in DWORD dwMapFlags, 
                          __in_ecount(cchSrc) LPCWSTR lpSrcStr, 
                          __in int cchSrc, 
                          __out_xcount_opt(cchDest) LPWSTR lpDestStr,
                          __in int cchDest, 
                          __in_opt LPNLSVERSIONINFO lpVersionInformation, 
                          __in_opt LPVOID lpReserved, 
                          __in_opt LPARAM lParam);
        
        int GetLocaleInfoEx(__in LPCWSTR lpLocaleName, __in LCID lcid, __in LCTYPE LCType, __out_ecount_opt(cchData) LPWSTR lpLCData, __in int cchData);
        int GetCalendarInfoEx(__in LPCWSTR lpLocaleName,
                              __in CALID Calendar,
                              __in_opt LPCWSTR pReserved,
                              __in CALTYPE CalType,
                              __out_ecount_opt(cchData) LPWSTR lpCalData,
                              __in int cchData,
                              __out_opt LPDWORD lpValue);
    }   

    int LCIDToLocaleName(__in LCID Locale, __out_ecount_opt(cchName) LPWSTR lpName, __in int cchName, __in DWORD dwFlags);          
    LCID LocaleNameToLCID(__in LPCWSTR lpName , __in DWORD dwFlags);          
    
    int ResolveLocaleName(__in LPCWSTR lpNameToResolve, __in_ecount_opt(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName);    

    __success(return)
    BOOL GetThreadPreferredUILanguages( __in DWORD dwFlags,
                                        __out PULONG pulNumLanguages,
                                        __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer,
                                        __inout PULONG pcchLanguagesBuffer);

};


