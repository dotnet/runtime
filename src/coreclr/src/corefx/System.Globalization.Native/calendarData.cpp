// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <assert.h>
#include <string.h>
#include <vector>

#include "config.h"
#include "locale.hpp"
#include "holders.h"
#include "errors.h"

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

// the function pointer definition for the callback used in EnumCalendarInfo
typedef void (*EnumCalendarInfoCallback)(const UChar*, const void*);

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
extern "C" int32_t GlobalizationNative_GetCalendars(
    const UChar* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);

    if (U_FAILURE(err))
        return 0;

    UEnumeration* pEnum = ucal_getKeywordValuesForLocale("calendar", locale, TRUE, &err);
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
ResultCode GetMonthDayPattern(const char* locale, UChar* sMonthDay, int32_t stringCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateTimePatternGenerator* pGenerator = udatpg_open(locale, &err);
    UDateTimePatternGeneratorHolder generatorHolder(pGenerator, err);

    if (U_FAILURE(err))
        return GetResultCode(err);

    udatpg_getBestPattern(pGenerator, UDAT_MONTH_DAY_UCHAR, -1, sMonthDay, stringCapacity, &err);

    return GetResultCode(err);
}

/*
Function:
GetNativeCalendarName

Gets the native calendar name.
*/
ResultCode GetNativeCalendarName(const char* locale, CalendarId calendarId, UChar* nativeName, int32_t stringCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    ULocaleDisplayNames* pDisplayNames = uldn_open(locale, ULDN_STANDARD_NAMES, &err);
    ULocaleDisplayNamesHolder displayNamesHolder(pDisplayNames, err);

    uldn_keyValueDisplayName(pDisplayNames, "calendar", GetCalendarName(calendarId), nativeName, stringCapacity, &err);

    return GetResultCode(err);
}

/*
Function:
GetCalendarInfo

Gets a single string of calendar information by filling the result parameter
with the requested value.
*/
extern "C" ResultCode GlobalizationNative_GetCalendarInfo(
    const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, UChar* result, int32_t resultCapacity)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);

    if (U_FAILURE(err))
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
bool InvokeCallbackForDatePattern(const char* locale,
                                  UDateFormatStyle style,
                                  EnumCalendarInfoCallback callback,
                                  const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateFormat* pFormat = udat_open(UDAT_NONE, style, locale, nullptr, 0, nullptr, 0, &err);
    UDateFormatHolder formatHolder(pFormat, err);

    if (U_FAILURE(err))
        return false;

    UErrorCode ignore = U_ZERO_ERROR;
    int32_t patternLen = udat_toPattern(pFormat, false, nullptr, 0, &ignore);

    std::vector<UChar> pattern(patternLen + 1, '\0');

    udat_toPattern(pFormat, false, pattern.data(), patternLen + 1, &err);

    if (U_SUCCESS(err))
    {
        callback(pattern.data(), context);
    }

    return U_SUCCESS(err);
}

/*
Function:
InvokeCallbackForDateTimePattern

Gets the DateTime pattern for the specified skeleton and invokes the callback
with the retrieved value.
*/
bool InvokeCallbackForDateTimePattern(const char* locale,
                                      const UChar* patternSkeleton,
                                      EnumCalendarInfoCallback callback,
                                      const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateTimePatternGenerator* pGenerator = udatpg_open(locale, &err);
    UDateTimePatternGeneratorHolder generatorHolder(pGenerator, err);

    if (U_FAILURE(err))
        return false;

    UErrorCode ignore = U_ZERO_ERROR;
    int32_t patternLen = udatpg_getBestPattern(pGenerator, patternSkeleton, -1, nullptr, 0, &ignore);

    std::vector<UChar> bestPattern(patternLen + 1, '\0');

    udatpg_getBestPattern(pGenerator, patternSkeleton, -1, bestPattern.data(), patternLen + 1, &err);

    if (U_SUCCESS(err))
    {
        callback(bestPattern.data(), context);
    }

    return U_SUCCESS(err);
}

/*
Function:
EnumSymbols

Enumerates all of the symbols of a type for a locale and calendar and invokes a callback
for each value.
*/
bool EnumSymbols(const char* locale,
                 CalendarId calendarId,
                 UDateFormatSymbolType type,
                 int32_t startIndex,
                 EnumCalendarInfoCallback callback,
                 const void* context)
{
    UErrorCode err = U_ZERO_ERROR;
    UDateFormat* pFormat = udat_open(UDAT_DEFAULT, UDAT_DEFAULT, locale, nullptr, 0, nullptr, 0, &err);
    UDateFormatHolder formatHolder(pFormat, err);

    if (U_FAILURE(err))
        return false;

    char localeWithCalendarName[ULOC_FULLNAME_CAPACITY];
    strncpy(localeWithCalendarName, locale, ULOC_FULLNAME_CAPACITY);
    uloc_setKeywordValue("calendar", GetCalendarName(calendarId), localeWithCalendarName, ULOC_FULLNAME_CAPACITY, &err);

    if (U_FAILURE(err))
        return false;

    UCalendar* pCalendar = ucal_open(nullptr, 0, localeWithCalendarName, UCAL_DEFAULT, &err);
    UCalendarHolder calendarHolder(pCalendar, err);

    if (U_FAILURE(err))
        return false;

    udat_setCalendar(pFormat, pCalendar);

    int32_t symbolCount = udat_countSymbols(pFormat, type);

    for (int32_t i = startIndex; i < symbolCount; i++)
    {
        UErrorCode ignore = U_ZERO_ERROR;
        int symbolLen = udat_getSymbols(pFormat, type, i, nullptr, 0, &ignore);

        std::vector<UChar> symbolBuf(symbolLen + 1, '\0');

        udat_getSymbols(pFormat, type, i, symbolBuf.data(), symbolBuf.size(), &err);

        assert(U_SUCCESS(err));

        if (U_FAILURE(err))
            return false;

        callback(symbolBuf.data(), context);
    }

    return true;
}

bool EnumUResourceBundle(const UResourceBundle* bundle, EnumCalendarInfoCallback callback, const void* context)
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

    return true;
}

/*
Function:
EnumAbbrevEraNames

Enumerates all the abbreviated era names of the specified locale and calendar, invoking the
callback function for each era name.
*/
bool EnumAbbrevEraNames(const char* locale,
                        CalendarId calendarId,
                        EnumCalendarInfoCallback callback,
                        const void* context)
{
    // The C-API for ICU provides no way to get at the abbreviated era names for a calendar (so we can't use EnumSymbols
    // here). Instead we will try to walk the ICU resource tables directly and fall back to regular era names if can't
    // find good data.
    char localeNameBuf[ULOC_FULLNAME_CAPACITY];
    char parentNameBuf[ULOC_FULLNAME_CAPACITY];

    char* localeNamePtr = localeNameBuf;
    char* parentNamePtr = parentNameBuf;

    strncpy(localeNamePtr, locale, ULOC_FULLNAME_CAPACITY);

    while (true)
    {
        UErrorCode status = U_ZERO_ERROR;

        UResourceBundle* rootResBundle = ures_open(nullptr, localeNamePtr, &status);
        UResourceBundleHolder rootResBundleHolder(rootResBundle, status);

        UResourceBundle* calResBundle = ures_getByKey(rootResBundle, "calendar", nullptr, &status);
        UResourceBundleHolder calResBundleHolder(calResBundle, status);

        UResourceBundle* targetCalResBundle =
            ures_getByKey(calResBundle, GetCalendarName(calendarId), nullptr, &status);
        UResourceBundleHolder targetCalResBundleHolder(targetCalResBundle, status);

        UResourceBundle* erasColResBundle = ures_getByKey(targetCalResBundle, "eras", nullptr, &status);
        UResourceBundleHolder erasColResBundleHolder(erasColResBundle, status);

        UResourceBundle* erasResBundle = ures_getByKey(erasColResBundle, "narrow", nullptr, &status);
        UResourceBundleHolder erasResBundleHolder(erasResBundle, status);

        if (U_SUCCESS(status))
        {
            EnumUResourceBundle(erasResBundle, callback, context);
            return true;
        }

        // Couldn't find the data we need for this locale, we should fallback.
        if (localeNameBuf[0] == 0x0)
        {
            // We are already at the root locale so there is nothing to fall back to, just use the regular eras.
            break;
        }

        uloc_getParent(localeNamePtr, parentNamePtr, ULOC_FULLNAME_CAPACITY, &status);

        if (U_FAILURE(status))
        {
            // Something bad happened getting the parent name, bail out.
            break;
        }

        // Swap localeNamePtr and parentNamePtr, parentNamePtr is what we want to use on the next iteration
        // and we can use the current localeName as scratch space if we have to fall back on that
        // iteration.

        char* temp = localeNamePtr;
        localeNamePtr = parentNamePtr;
        parentNamePtr = temp;
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
extern "C" int32_t GlobalizationNative_EnumCalendarInfo(
                        EnumCalendarInfoCallback callback, 
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
        case ShortDates:
            // ShortDates to map kShort and kMedium in ICU, but also adding the "yMd"
            // skeleton as well, as this closely matches what is used on Windows
            return InvokeCallbackForDatePattern(locale, UDAT_SHORT, callback, context) &&
                   InvokeCallbackForDatePattern(locale, UDAT_MEDIUM, callback, context) &&
                   InvokeCallbackForDateTimePattern(locale, UDAT_YEAR_NUM_MONTH_DAY_UCHAR, callback, context);
        case LongDates:
            // LongDates map to kFull and kLong in ICU.
            return InvokeCallbackForDatePattern(locale, UDAT_FULL, callback, context) &&
                   InvokeCallbackForDatePattern(locale, UDAT_LONG, callback, context);
        case YearMonths:
            return InvokeCallbackForDateTimePattern(locale, UDAT_YEAR_MONTH_UCHAR, callback, context);
        case DayNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_WEEKDAYS, 1, callback, context);
        case AbbrevDayNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_SHORT_WEEKDAYS, 1, callback, context);
        case MonthNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_MONTHS, 0, callback, context);
        case AbbrevMonthNames:
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_SHORT_MONTHS, 0, callback, context);
        case SuperShortDayNames:
            // UDAT_STANDALONE_SHORTER_WEEKDAYS was added in ICU 51, and CentOS 7 currently uses ICU 50.
            // fallback to UDAT_STANDALONE_NARROW_WEEKDAYS in that case.
#if HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_SHORTER_WEEKDAYS, 1, callback, context);
#else
            return EnumSymbols(locale, calendarId, UDAT_STANDALONE_NARROW_WEEKDAYS, 1, callback, context);
#endif
        case MonthGenitiveNames:
            return EnumSymbols(locale, calendarId, UDAT_MONTHS, 0, callback, context);
        case AbbrevMonthGenitiveNames:
            return EnumSymbols(locale, calendarId, UDAT_SHORT_MONTHS, 0, callback, context);
        case EraNames:
            return EnumSymbols(locale, calendarId, UDAT_ERAS, 0, callback, context);
        case AbbrevEraNames:
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
extern "C" int32_t GlobalizationNative_GetLatestJapaneseEra()
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
extern "C" int32_t GlobalizationNative_GetJapaneseEraStartDate(
    int32_t era, int32_t* startYear, int32_t* startMonth, int32_t* startDay)
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
