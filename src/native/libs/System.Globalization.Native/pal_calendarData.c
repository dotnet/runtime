// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>
#include <string.h>
#include "pal_locale_internal.h"
#include "pal_errors_internal.h"
#include "pal_calendarData.h"

#if defined(TARGET_UNIX) || defined(TARGET_WASI)
#include <strings.h>

#define STRING_COPY(destination, numberOfElements, source) \
    strncpy(destination, source, numberOfElements); \
    destination[numberOfElements - 1] = 0;

#elif defined(TARGET_WINDOWS)
#define strcasecmp _stricmp
#define STRING_COPY(destination, numberOfElements, source) strncpy_s(destination, numberOfElements, source, _TRUNCATE);
#endif

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

static const UChar UDAT_MONTH_DAY_UCHAR[] = {'M', 'M', 'M', 'M', 'd', '\0'};
static const UChar UDAT_YEAR_NUM_MONTH_DAY_UCHAR[] = {'y', 'M', 'd', '\0'};
static const UChar UDAT_YEAR_MONTH_UCHAR[] = {'y', 'M', 'M', 'M', 'M', '\0'};

/*
Function:
GetCalendarName

Gets the associated ICU calendar name for the CalendarId.
*/
static const char* GetCalendarName(CalendarId calendarId)
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
static CalendarId GetCalendarId(const char* calendarName)
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
int32_t GlobalizationNative_GetCalendars(
    const UChar* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);
    UEnumeration* pEnum = ucal_getKeywordValuesForLocale("calendar", locale, true, &err);
    int stringEnumeratorCount = uenum_count(pEnum, &err);
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
    uenum_close(pEnum);
    return calendarsReturned;
}

/*
Function:
GetMonthDayPattern

Gets the Month-Day DateTime pattern for the specified locale.
*/
static ResultCode GetMonthDayPattern(const char* locale,
                                     UChar* sMonthDay,
                                     int32_t stringCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateTimePatternGenerator* pGenerator = udatpg_open(locale, &err);
    udatpg_getBestPattern(pGenerator, UDAT_MONTH_DAY_UCHAR, -1, sMonthDay, stringCapacity, &err);
    udatpg_close(pGenerator);
    return GetResultCode(err);
}

/*
Function:
GetNativeCalendarName

Gets the native calendar name.
*/
static ResultCode GetNativeCalendarName(const char* locale,
                                        CalendarId calendarId,
                                        UChar* nativeName,
                                        int32_t stringCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    ULocaleDisplayNames* pDisplayNames = uldn_open(locale, ULDN_STANDARD_NAMES, &err);

    uldn_keyValueDisplayName(pDisplayNames, "calendar", GetCalendarName(calendarId), nativeName, stringCapacity, &err);

    uldn_close(pDisplayNames);
    return GetResultCode(err);
}

/*
Function:
GetCalendarInfo

Gets a single string of calendar information by filling the result parameter
with the requested value.
*/
ResultCode GlobalizationNative_GetCalendarInfo(
    const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, UChar* result, int32_t resultCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);

    if (U_FAILURE(err))
        return UnknownError;

    switch (dataType)
    {
        case CalendarData_NativeName:
            return GetNativeCalendarName(locale, calendarId, result, resultCapacity);
        case CalendarData_MonthDay:
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
static int InvokeCallbackForDatePattern(const char* locale,
                                        UDateFormatStyle style,
                                        EnumCalendarInfoCallback callback,
                                        const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateFormat* pFormat = udat_open(UDAT_NONE, style, locale, NULL, 0, NULL, 0, &err);

    if (U_FAILURE(err))
        return false;

    UErrorCode ignore = U_ZERO_ERROR;
    int32_t patternLen = udat_toPattern(pFormat, false, NULL, 0, &ignore) + 1;

    UChar* pattern = (UChar*)calloc((size_t)patternLen, sizeof(UChar));
    if (pattern == NULL)
    {
        udat_close(pFormat);
        return false;
    }

    udat_toPattern(pFormat, false, pattern, patternLen, &err);
    udat_close(pFormat);

    if (U_SUCCESS(err))
    {
        callback(pattern, context);
    }

    free(pattern);
    return UErrorCodeToBool(err);
}

/*
Function:
InvokeCallbackForDateTimePattern

Gets the DateTime pattern for the specified skeleton and invokes the callback
with the retrieved value.
*/
static int InvokeCallbackForDateTimePattern(const char* locale,
                                            const UChar* patternSkeleton,
                                            EnumCalendarInfoCallback callback,
                                            const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateTimePatternGenerator* pGenerator = udatpg_open(locale, &err);

    if (U_FAILURE(err))
        return false;

    UErrorCode ignore = U_ZERO_ERROR;
    int32_t patternLen = udatpg_getBestPattern(pGenerator, patternSkeleton, -1, NULL, 0, &ignore) + 1;

    UChar* bestPattern = (UChar*)calloc((size_t)patternLen, sizeof(UChar));
    if (bestPattern == NULL)
    {
        udatpg_close(pGenerator);
        return false;
    }

    udatpg_getBestPattern(pGenerator, patternSkeleton, -1, bestPattern, patternLen, &err);
    udatpg_close(pGenerator);

    if (U_SUCCESS(err))
    {
        callback(bestPattern, context);
    }

    free(bestPattern);
    return UErrorCodeToBool(err);
}

/*
Function:
EnumSymbols

Enumerates all of the symbols of a type for a locale and calendar and invokes a callback
for each value.
*/
static int32_t EnumSymbols(const char* locale,
                           CalendarId calendarId,
                           UDateFormatSymbolType type,
                           int32_t startIndex,
                           EnumCalendarInfoCallback callback,
                           const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateFormat* pFormat = udat_open(UDAT_DEFAULT, UDAT_DEFAULT, locale, NULL, 0, NULL, 0, &err);

    if (U_FAILURE(err))
        return false;

    char localeWithCalendarName[ULOC_FULLNAME_CAPACITY];
    STRING_COPY(localeWithCalendarName, sizeof(localeWithCalendarName), locale);

    uloc_setKeywordValue("calendar", GetCalendarName(calendarId), localeWithCalendarName, ULOC_FULLNAME_CAPACITY, &err);

    UCalendar* pCalendar = ucal_open(NULL, 0, localeWithCalendarName, UCAL_DEFAULT, &err);

    if (U_FAILURE(err))
    {
        udat_close(pFormat);
        return false;
    }

    udat_setCalendar(pFormat, pCalendar);

    int32_t symbolCount = udat_countSymbols(pFormat, type);
    UChar stackSymbolBuf[100];
    UChar* symbolBuf;

    for (int32_t i = startIndex; U_SUCCESS(err) && i < symbolCount; i++)
    {
        UErrorCode ignore = U_ZERO_ERROR;
        int symbolLen = udat_getSymbols(pFormat, type, i, NULL, 0, &ignore) + 1;

        if ((size_t)symbolLen <= sizeof(stackSymbolBuf) / sizeof(stackSymbolBuf[0]))
        {
            symbolBuf = stackSymbolBuf;
        }
        else
        {
            symbolBuf = (UChar*)calloc((size_t)symbolLen, sizeof(UChar));
            if (symbolBuf == NULL)
            {
                err = U_MEMORY_ALLOCATION_ERROR;
                break;
            }
        }

        udat_getSymbols(pFormat, type, i, symbolBuf, symbolLen, &err);

        if (U_SUCCESS(err))
        {
            callback(symbolBuf, context);
        }

        if (symbolBuf != stackSymbolBuf)
        {
            free(symbolBuf);
        }
    }

    udat_close(pFormat);
    ucal_close(pCalendar);
    return UErrorCodeToBool(err);
}

static void EnumUResourceBundle(const UResourceBundle* bundle,
                                EnumCalendarInfoCallback callback,
                                const void* context)
{
    int32_t eraNameCount = ures_getSize(bundle);

    for (int i = 0; i < eraNameCount; i++)
    {
        UErrorCode status = U_ZERO_ERROR;
        int32_t ignore; // We don't care about the length of the string as it is null terminated.
        const UChar* eraName = ures_getStringByIndex(bundle, i, &ignore, &status);

        if (U_SUCCESS(status))
        {
            callback(eraName, context);
        }
    }
}

static void CloseResBundle(UResourceBundle* rootResBundle,
                           UResourceBundle* calResBundle,
                           UResourceBundle* targetCalResBundle,
                           UResourceBundle* erasColResBundle,
                           UResourceBundle* erasResBundle)
{
    ures_close(rootResBundle);
    ures_close(calResBundle);
    ures_close(targetCalResBundle);
    ures_close(erasColResBundle);
    ures_close(erasResBundle);
}

/*
Function:
EnumAbbrevEraNames

Enumerates all the abbreviated era names of the specified locale and calendar, invoking the
callback function for each era name.
*/
static int32_t EnumAbbrevEraNames(const char* locale,
                                  CalendarId calendarId,
                                  EnumCalendarInfoCallback callback,
                                  const void* context)
{
    // The C-API for ICU provides no way to get at the abbreviated era names for a calendar (so we can't use EnumSymbols
    // here). Instead we will try to walk the ICU resource tables directly and fall back to regular era names if can't
    // find good data.
    char localeNameBuf[ULOC_FULLNAME_CAPACITY];
    char parentNameBuf[ULOC_FULLNAME_CAPACITY];

    STRING_COPY(localeNameBuf, sizeof(localeNameBuf), locale);

    char* localeNamePtr = localeNameBuf;
    char* parentNamePtr = parentNameBuf;

    while (true)
    {
        UErrorCode status = U_ZERO_ERROR;
        const char* name = GetCalendarName(calendarId);

        UResourceBundle* rootResBundle = ures_open(NULL, localeNamePtr, &status);
        UResourceBundle* calResBundle = ures_getByKey(rootResBundle, "calendar", NULL, &status);
        UResourceBundle* targetCalResBundle = ures_getByKey(calResBundle, name, NULL, &status);
        UResourceBundle* erasColResBundle = ures_getByKey(targetCalResBundle, "eras", NULL, &status);
        UResourceBundle* erasResBundle = ures_getByKey(erasColResBundle, "narrow", NULL, &status);

        if (U_SUCCESS(status))
        {
            EnumUResourceBundle(erasResBundle, callback, context);
            CloseResBundle(rootResBundle, calResBundle, targetCalResBundle, erasColResBundle, erasResBundle);
            return true;
        }

        // Couldn't find the data we need for this locale, we should fallback.
        if (localeNameBuf[0] == 0x0)
        {
            CloseResBundle(rootResBundle, calResBundle, targetCalResBundle, erasColResBundle, erasResBundle);
            // We are already at the root locale so there is nothing to fall back to, just use the regular eras.
            break;
        }

        uloc_getParent(localeNamePtr, parentNamePtr, ULOC_FULLNAME_CAPACITY, &status);

        if (U_FAILURE(status))
        {
            CloseResBundle(rootResBundle, calResBundle, targetCalResBundle, erasColResBundle, erasResBundle);
            // Something bad happened getting the parent name, bail out.
            break;
        }

        // Swap localeNamePtr and parentNamePtr, parentNamePtr is what we want to use on the next iteration
        // and we can use the current localeName as scratch space if we have to fall back on that
        // iteration.

        char* temp = localeNamePtr;
        localeNamePtr = parentNamePtr;
        parentNamePtr = temp;

        CloseResBundle(rootResBundle, calResBundle, targetCalResBundle, erasColResBundle, erasResBundle);
    }

    // Walking the resource bundles didn't work, just use the regular eras.
    return EnumSymbols(locale, calendarId, UDAT_ERAS, 0, callback, context);
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
int32_t GlobalizationNative_EnumCalendarInfo(EnumCalendarInfoCallback callback,
                                             const UChar* localeName,
                                             CalendarId calendarId,
                                             CalendarDataType dataType,
                                             const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);

    if (U_FAILURE(err))
        return false;

    switch (dataType)
    {
        case CalendarData_ShortDates:
            // ShortDates to map kShort and kMedium in ICU, but also adding the "yMd"
            // skeleton as well, as this closely matches what is used on Windows
            return InvokeCallbackForDatePattern(locale, UDAT_SHORT, callback, context) &&
                   InvokeCallbackForDatePattern(locale, UDAT_MEDIUM, callback, context) &&
                   InvokeCallbackForDateTimePattern(locale, UDAT_YEAR_NUM_MONTH_DAY_UCHAR, callback, context);
        case CalendarData_LongDates:
            // LongDates map to kFull and kLong in ICU.
            return InvokeCallbackForDatePattern(locale, UDAT_FULL, callback, context) &&
                   InvokeCallbackForDatePattern(locale, UDAT_LONG, callback, context);
        case CalendarData_YearMonths:
            return InvokeCallbackForDateTimePattern(locale, UDAT_YEAR_MONTH_UCHAR, callback, context);
        case CalendarData_DayNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_WEEKDAYS, 1, callback, context);
        case CalendarData_AbbrevDayNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_SHORT_WEEKDAYS, 1, callback, context);
        case CalendarData_MonthNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_MONTHS, 0, callback, context);
        case CalendarData_AbbrevMonthNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_SHORT_MONTHS, 0, callback, context);
        case CalendarData_SuperShortDayNames:
            // UDAT_STANDALONE_SHORTER_WEEKDAYS was added in ICU 51, and CentOS 7 currently uses ICU 50.
            // fallback to UDAT_STANDALONE_NARROW_WEEKDAYS in that case.
#if HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_SHORTER_WEEKDAYS, 1, callback, context);
#else
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_NARROW_WEEKDAYS, 1, callback, context);
#endif
        case CalendarData_MonthGenitiveNames:
            return EnumSymbols(locale, calendarId, UDAT_MONTHS, 0, callback, context);
        case CalendarData_AbbrevMonthGenitiveNames:
            return EnumSymbols(locale, calendarId, UDAT_SHORT_MONTHS, 0, callback, context);
        case CalendarData_EraNames:
            return EnumSymbols(locale, calendarId, UDAT_ERAS, 0, callback, context);
        case CalendarData_AbbrevEraNames:
            return EnumAbbrevEraNames(locale, calendarId, callback, context);
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
int32_t GlobalizationNative_GetLatestJapaneseEra(void)
{
    UErrorCode err = U_ZERO_ERROR;
    UCalendar* pCal = ucal_open(NULL, 0, JAPANESE_LOCALE_AND_CALENDAR, UCAL_TRADITIONAL, &err);

    if (U_FAILURE(err))
        return 0;

    ucal_set(pCal, UCAL_EXTENDED_YEAR, 9999);
    int32_t ret = ucal_get(pCal, UCAL_ERA, &err);

    ucal_close(pCal);
    return U_SUCCESS(err) ? ret : 0;
}

/*
Function:
GetJapaneseEraInfo

Gets the starting Gregorian date of the specified Japanese Era.
*/
int32_t GlobalizationNative_GetJapaneseEraStartDate(int32_t era,
                                                    int32_t* startYear,
                                                    int32_t* startMonth,
                                                    int32_t* startDay)
{
    *startYear = -1;
    *startMonth = -1;
    *startDay = -1;

    UErrorCode err = U_ZERO_ERROR;
    UCalendar* pCal = ucal_open(NULL, 0, JAPANESE_LOCALE_AND_CALENDAR, UCAL_TRADITIONAL, &err);

    if (U_FAILURE(err))
        return false;

    ucal_set(pCal, UCAL_ERA, era);
    ucal_set(pCal, UCAL_YEAR, 1);

    // UCAL_EXTENDED_YEAR is the gregorian year for the JapaneseCalendar
    *startYear = ucal_get(pCal, UCAL_EXTENDED_YEAR, &err);
    if (U_FAILURE(err))
    {
        ucal_close(pCal);
        return false;
    }

    // set the date to Jan 1
    ucal_set(pCal, UCAL_MONTH, 0);
    ucal_set(pCal, UCAL_DATE, 1);

    int32_t currentEra;
    for (int month = 0; U_SUCCESS(err) && month <= 12; month++)
    {
        currentEra = ucal_get(pCal, UCAL_ERA, &err);
        if (currentEra == era)
        {
            for (int day = 0; U_SUCCESS(err) && day < 31; day++)
            {
                // subtract 1 day at a time until we get out of the specified Era
                ucal_add(pCal, UCAL_DATE, -1, &err);
                currentEra = ucal_get(pCal, UCAL_ERA, &err);
                if (U_SUCCESS(err) && currentEra != era)
                {
                    // add back 1 day to get back into the specified Era
                    ucal_add(pCal, UCAL_DATE, 1, &err);
                    *startMonth =
                        ucal_get(pCal, UCAL_MONTH, &err) + 1; // ICU Calendar months are 0-based, but .NET is 1-based
                    *startDay = ucal_get(pCal, UCAL_DATE, &err);
                    ucal_close(pCal);

                    return UErrorCodeToBool(err);
                }
            }
        }

        // add 1 month at a time until we get into the specified Era
        ucal_add(pCal, UCAL_MONTH, 1, &err);
    }

    ucal_close(pCal);
    return false;
}
