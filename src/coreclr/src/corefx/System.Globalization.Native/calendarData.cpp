//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <string.h>

#include "locale.hpp"
#include "holders.h"

#include <unicode/dtfmtsym.h>
#include <unicode/smpdtfmt.h>
#include <unicode/dtptngen.h>
#include <unicode/locdspnm.h>

#define GREGORIAN_NAME "gregorian"
#define JAPANESE_NAME "japanese"
#define BUDDHIST_NAME "buddhist"
#define HEBREW_NAME "hebrew"
#define DANGI_NAME "dangi"
#define PERSIAN_NAME "persian"
#define ISLAMIC_NAME "islamic"
#define ISLAMIC_UMALQURA_NAME "islamic-umalqura"
#define ROC_NAME "roc"

#define JAPANESE_LOCALE_AND_CALENDAR "ja_JP@calendar=japanese"

const UChar UDAT_MONTH_DAY_UCHAR[] = {'M', 'M', 'M', 'M', 'd', '\0'};
const UChar UDAT_YEAR_NUM_MONTH_DAY_UCHAR[] = {'y', 'M', 'd', '\0'};
const UChar UDAT_YEAR_MONTH_UCHAR[] = {'y', 'M', 'M', 'M', 'M', '\0'};

/*
* These values should be kept in sync with System.Globalization.CalendarId
*/
enum CalendarId : int16_t
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

/*
* These values should be kept in sync with System.Globalization.CalendarDataType
*/
enum CalendarDataType : int32_t
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
};

/*
* These values should be kept in sync with
* System.Globalization.CalendarDataResult
*/
enum CalendarDataResult : int32_t
{
    Success = 0,
    UnknownError = 1,
    InsufficentBuffer = 2,
};

// the function pointer definition for the callback used in EnumCalendarInfo
typedef void (*EnumCalendarInfoCallback)(const UChar*, const void*);

/*
Function:
GetCalendarDataResult

Converts a UErrorCode to a CalendarDataResult.
*/
CalendarDataResult GetCalendarDataResult(UErrorCode err)
{
    if (U_SUCCESS(err))
    {
        return Success;
    }

    if (err == U_BUFFER_OVERFLOW_ERROR)
    {
        return InsufficentBuffer;
    }

    return UnknownError;
}

/*
Function:
GetCalendarName

Gets the associated ICU calendar name for the CalendarId.
*/
const char* GetCalendarName(CalendarId calendarId)
{
    switch (calendarId)
    {
        case JAPAN:
            return JAPANESE_NAME;
        case THAI:
            return BUDDHIST_NAME;
        case HEBREW:
            return HEBREW_NAME;
        case KOREA:
            return DANGI_NAME;
        case PERSIAN:
            return PERSIAN_NAME;
        case HIJRI:
            return ISLAMIC_NAME;
        case UMALQURA:
            return ISLAMIC_UMALQURA_NAME;
        case TAIWAN:
            return ROC_NAME;
        case GREGORIAN:
        case GREGORIAN_US:
        case GREGORIAN_ARABIC:
        case GREGORIAN_ME_FRENCH:
        case GREGORIAN_XLIT_ENGLISH:
        case GREGORIAN_XLIT_FRENCH:
        case JULIAN:
        case LUNAR_ETO_CHN:
        case LUNAR_ETO_KOR:
        case LUNAR_ETO_ROKUYOU:
        case SAKA:
        // don't support the lunisolar calendars until we have a solid understanding
        // of how they map to the ICU/CLDR calendars
        case CHINESELUNISOLAR:
        case KOREANLUNISOLAR:
        case JAPANESELUNISOLAR:
        case TAIWANLUNISOLAR:
        default:
            return GREGORIAN_NAME;
    }
}

/*
Function:
GetCalendarId

Gets the associated CalendarId for the ICU calendar name.
*/
CalendarId GetCalendarId(const char* calendarName)
{
    if (strcasecmp(calendarName, GREGORIAN_NAME) == 0)
        // TODO: what about the other gregorian types?
        return GREGORIAN;
    else if (strcasecmp(calendarName, JAPANESE_NAME) == 0)
        return JAPAN;
    else if (strcasecmp(calendarName, BUDDHIST_NAME) == 0)
        return THAI;
    else if (strcasecmp(calendarName, HEBREW_NAME) == 0)
        return HEBREW;
    else if (strcasecmp(calendarName, DANGI_NAME) == 0)
        return KOREA;
    else if (strcasecmp(calendarName, PERSIAN_NAME) == 0)
        return PERSIAN;
    else if (strcasecmp(calendarName, ISLAMIC_NAME) == 0)
        return HIJRI;
    else if (strcasecmp(calendarName, ISLAMIC_UMALQURA_NAME) == 0)
        return UMALQURA;
    else if (strcasecmp(calendarName, ROC_NAME) == 0)
        return TAIWAN;
    else
        return UNINITIALIZED_VALUE;
}

/*
Function:
GetCalendars

Returns the list of CalendarIds that are available for the specified locale.
*/
extern "C" int32_t GetCalendars(const UChar* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
        return 0;

    UErrorCode err = U_ZERO_ERROR;
    UEnumeration* pEnum = ucal_getKeywordValuesForLocale("calendar", locale.getName(), TRUE, &err);
    UEnumerationHolder enumHolder(pEnum, err);

    if (U_FAILURE(err))
        return 0;

    int stringEnumeratorCount = uenum_count(pEnum, &err);
    if (U_FAILURE(err))
        return 0;

    int calendarsReturned = 0;
    for (int i = 0; i < stringEnumeratorCount && calendarsReturned < calendarsCapacity; i++)
    {
        int32_t calendarNameLength = 0;
        const char* calendarName = uenum_next(pEnum, &calendarNameLength, &err);
        if (U_SUCCESS(err))
        {
            CalendarId calendarId = GetCalendarId(calendarName);
            if (calendarId != UNINITIALIZED_VALUE)
            {
                calendars[calendarsReturned] = calendarId;
                calendarsReturned++;
            }
        }
    }

    return calendarsReturned;
}

/*
Function:
GetMonthDayPattern

Gets the Month-Day DateTime pattern for the specified locale.
*/
CalendarDataResult GetMonthDayPattern(Locale& locale, UChar* sMonthDay, int32_t stringCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateTimePatternGenerator* pGenerator = udatpg_open(locale.getName(), &err);
    UDateTimePatternGeneratorHolder generatorHolder(pGenerator, err);

    if (U_FAILURE(err))
        return GetCalendarDataResult(err);

    udatpg_getBestPattern(pGenerator, UDAT_MONTH_DAY_UCHAR, -1, sMonthDay, stringCapacity, &err);

    return GetCalendarDataResult(err);
}

/*
Function:
GetNativeCalendarName

Gets the native calendar name.
*/
CalendarDataResult
GetNativeCalendarName(Locale& locale, CalendarId calendarId, UChar* nativeName, int32_t stringCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    ULocaleDisplayNames* pDisplayNames = uldn_open(locale.getName(), ULDN_STANDARD_NAMES, &err);
    ULocaleDisplayNamesHolder displayNamesHolder(pDisplayNames, err);

    uldn_keyValueDisplayName(pDisplayNames, "calendar", GetCalendarName(calendarId), nativeName, stringCapacity, &err);

    return GetCalendarDataResult(err);
}

/*
Function:
GetCalendarInfo

Gets a single string of calendar information by filling the result parameter
with the requested value.
*/
extern "C" CalendarDataResult GetCalendarInfo(
    const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, UChar* result, int32_t resultCapacity)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
        return UnknownError;

    switch (dataType)
    {
        case NativeName:
            return GetNativeCalendarName(locale, calendarId, result, resultCapacity);
        case MonthDay:
            return GetMonthDayPattern(locale, result, resultCapacity);
        default:
            assert(false);
            return UnknownError;
    }
}

/*
Function:
InvokeCallbackForDatePattern

Gets the ICU date pattern for the specified locale and EStyle and invokes the
callback with the result.
*/
bool InvokeCallbackForDatePattern(Locale& locale,
                                  UDateFormatStyle style,
                                  EnumCalendarInfoCallback callback,
                                  const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateFormat* pFormat = udat_open(UDAT_NONE, style, locale.getName(), nullptr, 0, nullptr, 0, &err);
    UDateFormatHolder formatHolder(pFormat, err);

    if (U_FAILURE(err))
        return false;

    UErrorCode ignore = U_ZERO_ERROR;
    int32_t patternLen = udat_toPattern(pFormat, false, nullptr, 0, &ignore);

    UChar* pattern = (UChar*)calloc(patternLen + 1, sizeof(UChar));

    if (pattern == nullptr)
        return false;

    udat_toPattern(pFormat, false, pattern, patternLen + 1, &err);

    if (U_SUCCESS(err))
    {
        callback(pattern, context);
    }

    free(pattern);

    return U_SUCCESS(err);
}

/*
Function:
InvokeCallbackForDateTimePattern

Gets the DateTime pattern for the specified skeleton and invokes the callback
with the retrieved value.
*/
bool InvokeCallbackForDateTimePattern(Locale& locale,
                                      const UChar* patternSkeleton,
                                      EnumCalendarInfoCallback callback,
                                      const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateTimePatternGenerator* pGenerator = udatpg_open(locale.getName(), &err);
    UDateTimePatternGeneratorHolder generatorHolder(pGenerator, err);

    if (U_FAILURE(err))
        return false;

    UErrorCode ignore = U_ZERO_ERROR;
    int32_t patternLen = udatpg_getBestPattern(pGenerator, patternSkeleton, -1, nullptr, 0, &ignore);

    UChar* bestPattern = (UChar*)calloc(patternLen + 1, sizeof(UChar));

    if (bestPattern == nullptr)
    {
        return false;
    }

    udatpg_getBestPattern(pGenerator, patternSkeleton, -1, bestPattern, patternLen + 1, &err);

    if (U_SUCCESS(err))
    {
        callback(bestPattern, context);
    }

    free(bestPattern);

    return U_SUCCESS(err);
}

/*
Function:
EnumCalendarArray

Enumerates an array of strings and invokes the callback for each value.
*/
bool EnumCalendarArray(const UnicodeString* srcArray,
                       int32_t srcArrayCount,
                       EnumCalendarInfoCallback callback,
                       const void* context)
{
    for (int i = 0; i < srcArrayCount; i++)
    {
        UnicodeString src = srcArray[i];
        callback(src.getTerminatedBuffer(), context);
    }

    return true;
}

/*
Function:
EnumWeekdays

Enumerates all the weekday names of the specified context and width, invoking
the callback function
for each weekday name.
*/
bool EnumWeekdays(Locale& locale,
                  CalendarId calendarId,
                  DateFormatSymbols::DtContextType dtContext,
                  DateFormatSymbols::DtWidthType dtWidth,
                  EnumCalendarInfoCallback callback,
                  const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    DateFormatSymbols dateFormatSymbols(locale, GetCalendarName(calendarId), err);
    if (U_FAILURE(err))
        return false;

    int32_t daysCount;
    const UnicodeString* dayNames = dateFormatSymbols.getWeekdays(daysCount, dtContext, dtWidth);

    // ICU returns an empty string for the first/zeroth element in the weekdays
    // array.
    // So skip the first element.
    dayNames++;
    daysCount--;

    return EnumCalendarArray(dayNames, daysCount, callback, context);
}

/*
Function:
EnumMonths

Enumerates all the month names of the specified context and width, invoking the
callback function
for each month name.
*/
bool EnumMonths(Locale& locale,
                CalendarId calendarId,
                DateFormatSymbols::DtContextType dtContext,
                DateFormatSymbols::DtWidthType dtWidth,
                EnumCalendarInfoCallback callback,
                const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    DateFormatSymbols dateFormatSymbols(locale, GetCalendarName(calendarId), err);
    if (U_FAILURE(err))
        return false;

    int32_t monthsCount;
    const UnicodeString* monthNames = dateFormatSymbols.getMonths(monthsCount, dtContext, dtWidth);
    return EnumCalendarArray(monthNames, monthsCount, callback, context);
}

/*
Function:
EnumEraNames

Enumerates all the era names of the specified locale and calendar, invoking the
callback function
for each era name.
*/
bool EnumEraNames(Locale& locale,
                  CalendarId calendarId,
                  CalendarDataType dataType,
                  EnumCalendarInfoCallback callback,
                  const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    const char* calendarName = GetCalendarName(calendarId);
    DateFormatSymbols dateFormatSymbols(locale, calendarName, err);
    if (U_FAILURE(err))
        return false;

    int32_t eraNameCount;
    const UnicodeString* eraNames;

    if (dataType == EraNames)
    {
        eraNames = dateFormatSymbols.getEras(eraNameCount);
    }
    else if (dataType == AbbrevEraNames)
    {
        eraNames = dateFormatSymbols.getNarrowEras(eraNameCount);
    }
    else
    {
        assert(false);
        return false;
    }

    return EnumCalendarArray(eraNames, eraNameCount, callback, context);
}

/*
Function:
EnumCalendarInfo

Retrieves a collection of calendar string data specified by the locale,
calendar, and data type.
Allows for a collection of calendar string data to be retrieved by invoking
the callback for each value in the collection.
The context parameter is passed through to the callback along with each string.
*/
extern "C" int32_t EnumCalendarInfo(EnumCalendarInfoCallback callback,
                                    const UChar* localeName,
                                    CalendarId calendarId,
                                    CalendarDataType dataType,
                                    const void* context)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
        return false;

    switch (dataType)
    {
        case ShortDates:
            // ShortDates to map kShort and kMedium in ICU, but also adding the "yMd"
            // skeleton as well, as this
            // closely matches what is used on Windows
            return InvokeCallbackForDateTimePattern(locale, UDAT_YEAR_NUM_MONTH_DAY_UCHAR, callback, context) &&
                   InvokeCallbackForDatePattern(locale, UDAT_SHORT, callback, context) &&
                   InvokeCallbackForDatePattern(locale, UDAT_MEDIUM, callback, context);
        case LongDates:
            // LongDates map to kFull and kLong in ICU.
            return InvokeCallbackForDatePattern(locale, UDAT_FULL, callback, context) &&
                   InvokeCallbackForDatePattern(locale, UDAT_LONG, callback, context);
        case YearMonths:
            return InvokeCallbackForDateTimePattern(locale, UDAT_YEAR_MONTH_UCHAR, callback, context);
        case DayNames:
            return EnumWeekdays(
                locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::WIDE, callback, context);
        case AbbrevDayNames:
            return EnumWeekdays(
                locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::ABBREVIATED, callback, context);
        case MonthNames:
            return EnumMonths(
                locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::WIDE, callback, context);
        case AbbrevMonthNames:
            return EnumMonths(
                locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::ABBREVIATED, callback, context);
        case SuperShortDayNames:
#ifdef HAVE_DTWIDTHTYPE_SHORT
            return EnumWeekdays(
                locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::SHORT, callback, context);
#else
            // Currently CentOS-7 uses ICU-50 and ::SHORT was added in ICU-51, so use
            // ::NARROW instead
            return EnumWeekdays(
                locale, calendarId, DateFormatSymbols::STANDALONE, DateFormatSymbols::NARROW, callback, context);
#endif
        case MonthGenitiveNames:
            return EnumMonths(
                locale, calendarId, DateFormatSymbols::FORMAT, DateFormatSymbols::WIDE, callback, context);
        case AbbrevMonthGenitiveNames:
            return EnumMonths(
                locale, calendarId, DateFormatSymbols::FORMAT, DateFormatSymbols::ABBREVIATED, callback, context);
        case EraNames:
        case AbbrevEraNames:
            return EnumEraNames(locale, calendarId, dataType, callback, context);
        default:
            assert(false);
            return false;
    }
}

/*
Function:
GetLatestJapaneseEra

Gets the latest era in the Japanese calendar.
*/
extern "C" int32_t GetLatestJapaneseEra()
{
    UErrorCode err = U_ZERO_ERROR;
    UCalendar* pCal = ucal_open(nullptr, 0, JAPANESE_LOCALE_AND_CALENDAR, UCAL_TRADITIONAL, &err);
    UCalendarHolder calHolder(pCal, err);

    if (U_FAILURE(err))
        return 0;

    int32_t ret = ucal_getLimit(pCal, UCAL_ERA, UCAL_MAXIMUM, &err);

    return U_SUCCESS(err) ? ret : 0;
}

/*
Function:
GetJapaneseEraInfo

Gets the starting Gregorian date of the specified Japanese Era.
*/
extern "C" int32_t GetJapaneseEraStartDate(int32_t era, int32_t* startYear, int32_t* startMonth, int32_t* startDay)
{
    *startYear = -1;
    *startMonth = -1;
    *startDay = -1;

    UErrorCode err = U_ZERO_ERROR;
    UCalendar* pCal = ucal_open(nullptr, 0, JAPANESE_LOCALE_AND_CALENDAR, UCAL_TRADITIONAL, &err);
    UCalendarHolder calHolder(pCal, err);

    if (U_FAILURE(err))
        return false;

    ucal_set(pCal, UCAL_ERA, era);
    ucal_set(pCal, UCAL_YEAR, 1);

    // UCAL_EXTENDED_YEAR is the gregorian year for the JapaneseCalendar
    *startYear = ucal_get(pCal, UCAL_EXTENDED_YEAR, &err);
    if (U_FAILURE(err))
        return false;

    // set the date to Jan 1
    ucal_set(pCal, UCAL_MONTH, 0);
    ucal_set(pCal, UCAL_DATE, 1);

    int32_t currentEra;
    for (int i = 0; i <= 12; i++)
    {
        currentEra = ucal_get(pCal, UCAL_ERA, &err);
        if (U_FAILURE(err))
            return false;

        if (currentEra == era)
        {
            for (int i = 0; i < 31; i++)
            {
                // subtract 1 day at a time until we get out of the specified Era
                ucal_add(pCal, UCAL_DATE, -1, &err);
                if (U_FAILURE(err))
                    return false;

                currentEra = ucal_get(pCal, UCAL_ERA, &err);
                if (U_FAILURE(err))
                    return false;

                if (currentEra != era)
                {
                    // add back 1 day to get back into the specified Era
                    ucal_add(pCal, UCAL_DATE, 1, &err);
                    if (U_FAILURE(err))
                        return false;

                    *startMonth =
                        ucal_get(pCal, UCAL_MONTH, &err) + 1; // ICU Calendar months are 0-based, but .NET is 1-based
                    if (U_FAILURE(err))
                        return false;

                    *startDay = ucal_get(pCal, UCAL_DATE, &err);
                    if (U_FAILURE(err))
                        return false;

                    return true;
                }
            }
        }

        // add 1 month at a time until we get into the specified Era
        ucal_add(pCal, UCAL_MONTH, 1, &err);
        if (U_FAILURE(err))
            return false;
    }

    return false;
}
