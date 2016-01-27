// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  Class:    CalendarData
//

//
//  Purpose:  This module implements the methods of the CalendarData
//            class.  These methods are the helper functions for the
//            Locale class.
//
//  Date:     July 4, 2007
//
////////////////////////////////////////////////////////////////////////////

#ifndef _CALENDARDATA_H
#define _CALENDARDATA_H

//
// Data store for the calendar data.
//
class CalendarData : Object
{

    //
    // WARNING: These properties should stay in-sync with CalendarData.cs
    //
private:    
    // Identity
    STRINGREF   sNativeName               ; // Calendar Name for the locale (fallback for calendar only records)

    // Formats
    PTRARRAYREF saShortDates              ; // Short Data format, default first
    PTRARRAYREF saYearMonths              ; // Year/Month Data format, default first
    PTRARRAYREF saLongDates               ; // Long Data format, default first
    STRINGREF   sMonthDay                 ; // Month/Day format

    // Calendar Parts Names
    PTRARRAYREF saEraNames                ; // Names of Eras
    PTRARRAYREF saAbbrevEraNames          ; // Abbreviated Era Names
    PTRARRAYREF saAbbrevEnglishEraNames   ; // Abbreviated Era Names in English
    PTRARRAYREF saDayNames                ; // Day Names, null to use locale data, starts on Sunday
    PTRARRAYREF saAbbrevDayNames          ; // Abbrev Day Names, null to use locale data, starts on Sunday
    PTRARRAYREF saSuperShortDayNames      ; // Super short Day of week names
    PTRARRAYREF saMonthNames              ; // Month Names (13)
    PTRARRAYREF saAbbrevMonthNames        ; // Abbrev Month Names (13)
    PTRARRAYREF saMonthGenitiveNames      ; // Genitive Month Names (13)
    PTRARRAYREF saAbbrevMonthGenitiveNames; // Genitive Abbrev Month Names (13)
    PTRARRAYREF saLeapYearMonthNames      ; // Multiple strings for the month names in a leap year.

    // Year, digits have to be at end to make marshaller happy?
    INT32       iTwoDigitYearMax          ; // Max 2 digit year (for Y2K bug data entry)
    INT32       iCurrentEra               ; // our current era #

    // Use overrides?
    CLR_BOOL    bUseUserOverrides        ; // True if we want user overrides.

    //
    // Helpers
    //
    static BOOL CallGetCalendarInfoEx(LPCWSTR localeName, int calendar, int calType, STRINGREF* pOutputStrRef);
    static BOOL CallGetCalendarInfoEx(LPCWSTR localeName, int calendar, int calType, INT32* pOutputInt32);
    static BOOL GetCalendarDayInfo(LPCWSTR localeName, int calendar, int calType, PTRARRAYREF* pOutputStrings);
    static BOOL GetCalendarMonthInfo(LPCWSTR localeName, int calendar, int calType, PTRARRAYREF* pOutputStrings);
// TODO: NLS Arrowhead -Windows 7 If the OS had data this could use it, but Windows doesn't expose data for eras in enough detail    
//    static BOOL GetCalendarEraInfo(LPCWSTR localeName, int calendar, PTRARRAYREF* pOutputEras);
    static BOOL CallEnumCalendarInfo(__in_z LPCWSTR localeName, __in int calendar, __in int calType, 
                                        __in int lcType, __inout PTRARRAYREF* pOutputStrings);

    static void CheckSpecialCalendar(INT32* pCalendarInt, StackSString* pLocaleNameStackBuffer);
public:
    //
    //  ecall function for methods in CalendarData
    //
    static FCDECL1(INT32, nativeGetTwoDigitYearMax, INT32 nValue);
    static FCDECL3(FC_BOOL_RET, nativeGetCalendarData, CalendarData* calendarDataUNSAFE, StringObject* pLocaleNameUNSAFE, INT32 calendar);
    static FCDECL3(INT32, nativeGetCalendars, StringObject* pLocaleNameUNSAFE, CLR_BOOL bUseOverrides, I4Array* calendars);
    static FCDECL3(Object*, nativeEnumTimeFormats, StringObject* pLocaleNameUNSAFE, INT32 dwFlags, CLR_BOOL useUserOverride);
};

typedef CalendarData* CALENDARDATAREF;

#ifndef LOCALE_RETURN_GENITIVE_NAMES
#define LOCALE_RETURN_GENITIVE_NAMES    0x10000000   //Flag to return the Genitive forms of month names
#endif

#ifndef CAL_RETURN_GENITIVE_NAMES
#define CAL_RETURN_GENITIVE_NAMES       LOCALE_RETURN_GENITIVE_NAMES  // return genitive forms of month names
#endif

#ifndef CAL_SERASTRING
#define CAL_SERASTRING                  0x00000004  // era name for IYearOffsetRanges, eg A.D.
#endif

#ifndef CAL_SMONTHDAY
#define CAL_SMONTHDAY                   0x00000038  // Month/day pattern (reserve for potential inclusion in a future version)
#define CAL_SABBREVERASTRING            0x00000039  // Abbreviated era string (eg: AD)
#endif

#define RESERVED_CAL_JULIAN                 13  // Julian calendar (data looks like GREGORIAN_US)
#define RESERVED_CAL_JAPANESELUNISOLAR      14  // Japaenese Lunisolar calendar (data looks like CAL_JAPANESE)
#define RESERVED_CAL_CHINESELUNISOLAR       15  // Algorithmic
#define RESERVED_CAL_SAKA                   16  // reserved to match Office but not implemented in our code
#define RESERVED_CAL_LUNAR_ETO_CHN          17  // reserved to match Office but not implemented in our code
#define RESERVED_CAL_LUNAR_ETO_KOR          18  // reserved to match Office but not implemented in our code
#define RESERVED_CAL_LUNAR_ETO_ROKUYOU      19  // reserved to match Office but not implemented in our code
#define RESERVED_CAL_KOREANLUNISOLAR        20  // Algorithmic
#define RESERVED_CAL_TAIWANLUNISOLAR        21  // Algorithmic
#define RESERVED_CAL_PERSIAN                22  // Algorithmic

// These are vista properties
#ifndef CAL_UMALQURA
#define CAL_UMALQURA 23
#endif

#ifndef CAL_SSHORTESTDAYNAME1
#define CAL_SSHORTESTDAYNAME1 0x00000031
#define CAL_SSHORTESTDAYNAME2 0x00000032
#define CAL_SSHORTESTDAYNAME3 0x00000033
#define CAL_SSHORTESTDAYNAME4 0x00000034
#define CAL_SSHORTESTDAYNAME5 0x00000035
#define CAL_SSHORTESTDAYNAME6 0x00000036
#define CAL_SSHORTESTDAYNAME7 0x00000037
#endif

#ifndef CAL_ITWODIGITYEARMAX
    #define CAL_ITWODIGITYEARMAX    0x00000030  // two digit year max
#endif // CAL_ITWODIGITYEARMAX
#ifndef CAL_RETURN_NUMBER
    #define CAL_RETURN_NUMBER       0x20000000   // return number instead of string
#endif // CAL_RETURN_NUMBER

#ifndef LOCALE_SNAME
#define LOCALE_SNAME                  0x0000005c   // locale name (ie: en-us)
#define LOCALE_SDURATION              0x0000005d   // time duration format
#define LOCALE_SKEYBOARDSTOINSTALL    0x0000005e
#define LOCALE_SSHORTESTDAYNAME1      0x00000060   // Shortest day name for Monday
#define LOCALE_SSHORTESTDAYNAME2      0x00000061   // Shortest day name for Tuesday
#define LOCALE_SSHORTESTDAYNAME3      0x00000062   // Shortest day name for Wednesday
#define LOCALE_SSHORTESTDAYNAME4      0x00000063   // Shortest day name for Thursday
#define LOCALE_SSHORTESTDAYNAME5      0x00000064   // Shortest day name for Friday
#define LOCALE_SSHORTESTDAYNAME6      0x00000065   // Shortest day name for Saturday
#define LOCALE_SSHORTESTDAYNAME7      0x00000066   // Shortest day name for Sunday
#define LOCALE_SISO639LANGNAME2       0x00000067   // 3 character ISO abbreviated language name
#define LOCALE_SISO3166CTRYNAME2      0x00000068   // 3 character ISO country name
#define LOCALE_SNAN                   0x00000069   // Not a Number
#define LOCALE_SPOSINFINITY           0x0000006a   // + Infinity
#define LOCALE_SNEGINFINITY           0x0000006b   // - Infinity
#define LOCALE_SSCRIPTS               0x0000006c   // Typical scripts in the locale
#define LOCALE_SPARENT                0x0000006d   // Fallback name for resources
#define LOCALE_SCONSOLEFALLBACKNAME   0x0000006e   // Fallback name for within the console
#define LOCALE_SLANGDISPLAYNAME       0x0000006f   // Lanugage Display Name for a language
#endif  // LOCALE_SNAME
#ifndef LOCALE_SSHORTTIME
#define LOCALE_SSHORTTIME             0x00000079   // short time format (ie: no seconds, just h:mm)
#endif // LOCALE_SSHORTTIME

#ifndef TIME_NOSECONDS
#define TIME_NOSECONDS            0x00000002  // do not use seconds
#endif

#endif

