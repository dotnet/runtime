// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdlib.h>

#include "pal_locale.h"
#include "pal_compiler.h"
#include "pal_errors.h"

/*
* These values should be kept in sync with System.Globalization.CalendarId
*/
enum
{
    UNINITIALIZED_VALUE = 0,
    GREGORIAN = 1,               // Gregorian (localized) calendar
    GREGORIAN_US = 2,            // Gregorian (U.S.) calendar
    JAPAN = 3,                   // Japanese Emperor Era calendar
                                 /* SSS_WARNINGS_OFF */
    TAIWAN = 4,                  // Taiwan Era calendar /* SSS_WARNINGS_ON */
    KOREA = 5,                   // Korean Tangun Era calendar
    HIJRI = 6,                   // Hijri (Arabic Lunar) calendar
    THAI = 7,                    // Thai calendar
    HEBREW = 8,                  // Hebrew (Lunar) calendar
    GREGORIAN_ME_FRENCH = 9,     // Gregorian Middle East French calendar
    GREGORIAN_ARABIC = 10,       // Gregorian Arabic calendar
    GREGORIAN_XLIT_ENGLISH = 11, // Gregorian Transliterated English calendar
    GREGORIAN_XLIT_FRENCH = 12,
    // Note that all calendars after this point are MANAGED ONLY for now.
    JULIAN = 13,
    JAPANESELUNISOLAR = 14,
    CHINESELUNISOLAR = 15,
    SAKA = 16,              // reserved to match Office but not implemented in our code
    LUNAR_ETO_CHN = 17,     // reserved to match Office but not implemented in our code
    LUNAR_ETO_KOR = 18,     // reserved to match Office but not implemented in our code
    LUNAR_ETO_ROKUYOU = 19, // reserved to match Office but not implemented in our code
    KOREANLUNISOLAR = 20,
    TAIWANLUNISOLAR = 21,
    PERSIAN = 22,
    UMALQURA = 23,
    LAST_CALENDAR = 23 // Last calendar ID
};
typedef uint16_t CalendarId;

/*
* These values should be kept in sync with System.Globalization.CalendarDataType
*/
typedef enum
{
    CalendarData_Uninitialized = 0,
    CalendarData_NativeName = 1,
    CalendarData_MonthDay = 2,
    CalendarData_ShortDates = 3,
    CalendarData_LongDates = 4,
    CalendarData_YearMonths = 5,
    CalendarData_DayNames = 6,
    CalendarData_AbbrevDayNames = 7,
    CalendarData_MonthNames = 8,
    CalendarData_AbbrevMonthNames = 9,
    CalendarData_SuperShortDayNames = 10,
    CalendarData_MonthGenitiveNames = 11,
    CalendarData_AbbrevMonthGenitiveNames = 12,
    CalendarData_EraNames = 13,
    CalendarData_AbbrevEraNames = 14,
} CalendarDataType;

// the function pointer definition for the callback used in EnumCalendarInfo
typedef void (*EnumCalendarInfoCallback)(const UChar*, const void*);

PALEXPORT int32_t GlobalizationNative_GetCalendars(const UChar* localeName,
                                                   CalendarId* calendars,
                                                   int32_t calendarsCapacity);

PALEXPORT ResultCode GlobalizationNative_GetCalendarInfo(const UChar* localeName,
                                                         CalendarId calendarId,
                                                         CalendarDataType dataType,
                                                         UChar* result,
                                                         int32_t resultCapacity);

PALEXPORT int32_t GlobalizationNative_EnumCalendarInfo(EnumCalendarInfoCallback callback,
                                                       const UChar* localeName,
                                                       CalendarId calendarId,
                                                       CalendarDataType dataType,
                                                       const void* context);

PALEXPORT int32_t GlobalizationNative_GetLatestJapaneseEra(void);

PALEXPORT int32_t GlobalizationNative_GetJapaneseEraStartDate(int32_t era,
                                                              int32_t* startYear,
                                                              int32_t* startMonth,
                                                              int32_t* startDay);
