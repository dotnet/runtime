// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdlib.h>

#include "pal_compiler.h"
#include "pal_locale.h"
#include "pal_errors.h"

/*
* These values should be kept in sync with System.Globalization.CalendarId
*/
typedef enum
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
} CalendarId;

/*
* These values should be kept in sync with System.Globalization.CalendarDataType
*/
typedef enum
{
    Uninitialized = 0,
    NativeName = 1,
    MonthDay = 2,
    ShortDates = 3,
    LongDates = 4,
    YearMonths = 5,
    DayNames = 6,
    AbbrevDayNames = 7,
    MonthNames = 8,
    AbbrevMonthNames = 9,
    SuperShortDayNames = 10,
    MonthGenitiveNames = 11,
    AbbrevMonthGenitiveNames = 12,
    EraNames = 13,
    AbbrevEraNames = 14,
} CalendarDataType;

// the function pointer definition for the callback used in EnumCalendarInfo
typedef void (*EnumCalendarInfoCallback)(const UChar*, const void*);

DLLEXPORT int32_t GlobalizationNative_GetCalendars(const UChar* localeName,
                                                   CalendarId* calendars,
                                                   int32_t calendarsCapacity);

DLLEXPORT ResultCode GlobalizationNative_GetCalendarInfo(const UChar* localeName,
                                                         CalendarId calendarId,
                                                         CalendarDataType dataType,
                                                         UChar* result,
                                                         int32_t resultCapacity);

DLLEXPORT int32_t GlobalizationNative_EnumCalendarInfo(EnumCalendarInfoCallback callback,
                                                       const UChar* localeName,
                                                       CalendarId calendarId,
                                                       CalendarDataType dataType,
                                                       const void* context);

DLLEXPORT int32_t GlobalizationNative_GetLatestJapaneseEra(void);

DLLEXPORT int32_t GlobalizationNative_GetJapaneseEraStartDate(int32_t era,
                                                              int32_t* startYear,
                                                              int32_t* startMonth,
                                                              int32_t* startDay);
