// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>

#include "pal_errors_internal.h"
#include "pal_locale_internal.h"
#include "pal_timeZoneInfo.h"

#define DISPLAY_NAME_LENGTH 256  // arbitrarily large, to be safe
#define TZID_LENGTH 64           // arbitrarily large, to be safe

// For descriptions of the following patterns, see https://unicode-org.github.io/icu/userguide/format_parse/datetime/#date-field-symbol-table
static const UChar GENERIC_PATTERN_UCHAR[] = {'v', 'v', 'v', 'v', '\0'};           // u"vvvv"
static const UChar GENERIC_LOCATION_PATTERN_UCHAR[] = {'V', 'V', 'V', 'V', '\0'};  // u"VVVV"
static const UChar EXEMPLAR_CITY_PATTERN_UCHAR[] = {'V', 'V', 'V', '\0'};          // u"VVV"

/*
Convert Windows Time Zone Id to IANA Id
*/
int32_t GlobalizationNative_WindowsIdToIanaId(const UChar* windowsId, const char* region, UChar* ianaId, int32_t ianaIdLength)
{
    UErrorCode status = U_ZERO_ERROR;

    if (ucal_getTimeZoneIDForWindowsID_ptr != NULL)
    {
        int32_t ianaIdFilledLength = ucal_getTimeZoneIDForWindowsID(windowsId, -1, region, ianaId, ianaIdLength, &status);
        if (U_SUCCESS(status))
        {
            return ianaIdFilledLength;
        }
    }

    // Failed
    return 0;
}

/*
Convert IANA Time Zone Id to Windows Id
*/
int32_t GlobalizationNative_IanaIdToWindowsId(const UChar* ianaId, UChar* windowsId, int32_t windowsIdLength)
{
    UErrorCode status = U_ZERO_ERROR;

    if (ucal_getWindowsTimeZoneID_ptr != NULL)
    {
        int32_t windowsIdFilledLength = ucal_getWindowsTimeZoneID(ianaId, -1, windowsId, windowsIdLength, &status);

        if (U_SUCCESS(status))
        {
            return windowsIdFilledLength;
        }
    }

    // Failed
    return 0;
}

/*
Private function to get the standard and daylight names from the ICU Calendar API.
*/
static void GetTimeZoneDisplayName_FromCalendar(const char* locale, const UChar* timeZoneId, const UDate timestamp, UCalendarDisplayNameType type, UChar* result, int32_t resultLength, UErrorCode* err)
{
    // Examples: "Pacific Standard Time"  (standard)
    //           "Pacific Daylight Time"  (daylight)

    // (-1 == timeZoneId is null terminated)
    UCalendar* calendar = ucal_open(timeZoneId, -1, locale, UCAL_DEFAULT, err);
    if (U_SUCCESS(*err))
    {
        ucal_setMillis(calendar, timestamp, err);
        if (U_SUCCESS(*err))
        {
           ucal_getTimeZoneDisplayName(calendar, type, locale, result, resultLength, err);
        }

        ucal_close(calendar);
    }
}

/*
Private function to get the various forms of generic time zone names using patterns with the ICU Date Formatting API.
*/
static void GetTimeZoneDisplayName_FromPattern(const char* locale, const UChar* timeZoneId, const UDate timestamp, const UChar* pattern, UChar* result, int32_t resultLength, UErrorCode* err)
{
    // (-1 == timeZoneId and pattern are null terminated)
    UDateFormat* dateFormatter = udat_open(UDAT_PATTERN, UDAT_PATTERN, locale, timeZoneId, -1, pattern, -1, err);
    if (U_SUCCESS(*err))
    {
        udat_format(dateFormatter, timestamp, result, resultLength, NULL, err);
        udat_close(dateFormatter);
    }
}

/*
Private function to modify the generic display name to better suit our needs.
*/
static void FixupTimeZoneGenericDisplayName(const char* locale, const UChar* timeZoneId, const UDate timestamp, UChar* genericName, UErrorCode* err)
{
    // By default, some time zones will still give a standard name instead of the generic
    // non-location name.
    //
    // For example, given the following zones and their English results:
    //     America/Denver  => "Mountain Time"
    //     America/Phoenix => "Mountain Standard Time"
    //
    // We prefer that all time zones in the same metazone have the same generic name,
    // such that they are grouped together when combined with their base offset, location
    // and sorted alphabetically.  For example:
    //
    //     (UTC-07:00) Mountain Time (Denver)
    //     (UTC-07:00) Mountain Time (Phoenix)
    //
    // Without modification, they would show as:
    //
    //     (UTC-07:00) Mountain Standard Time (Phoenix)
    //     (UTC-07:00) Mountain Time (Denver)
    //
    // When combined with the rest of the time zones, having them not grouped together
    // makes it harder to locate the correct time zone from a list.
    //
    // The reason we get the standard name is because TR35 (LDML) defines a rule that
    // states that metazone generic names should use standard names if there is no DST
    // transition within a +/- 184 day range near the timestamp being translated.
    //
    // See the "Type Fallback" section in:
    // https://www.unicode.org/reports/tr35/tr35-dates.html#Using_Time_Zone_Names
    //
    // This might make sense when attached to an exact timestamp, but doesn't work well
    // when using the generic name to pick a time zone from a list.
    // Note that this test only happens when the generic name comes from a metazone.
    //
    // ICU implements this test in TZGNCore::formatGenericNonLocationName in
    // https://github.com/unicode-org/icu/blob/master/icu4c/source/i18n/tzgnames.cpp
    // (Note the kDstCheckRange 184-day constant.)
    //
    // The rest of the code below is a workaround for this issue.  When the generic
    // name and standard name match, we search through the other time zones for one
    // having the same base offset and standard name but a shorter generic name.
    // That will at least keep them grouped together, though note that if there aren't
    // any found that means all of them are using the standard name.
    //
    // If ICU ever adds an API to get a generic name that doesn't perform the
    // 184-day check on metazone names, then test for the existence of that new API
    // and use that instead of this workaround.  Keep the workaround for when the
    // new API is not available.

    // Get the standard name for this time zone.  (-1 == timeZoneId is null terminated)
    // Note that we leave the calendar open and close it later so we can also get the base offset.
    UChar standardName[DISPLAY_NAME_LENGTH];
    UCalendar* calendar = ucal_open(timeZoneId, -1, locale, UCAL_DEFAULT, err);
    if (U_FAILURE(*err))
    {
        return;
    }

    ucal_setMillis(calendar, timestamp, err);
    if (U_FAILURE(*err))
    {
        ucal_close(calendar);
        return;
    }

    ucal_getTimeZoneDisplayName(calendar, UCAL_STANDARD, locale, standardName, DISPLAY_NAME_LENGTH, err);
    if (U_FAILURE(*err))
    {
        ucal_close(calendar);
        return;
    }

    // Ensure the generic name is the same as the standard name.
    if (u_strcmp(genericName, standardName) != 0)
    {
        ucal_close(calendar);
        return;
    }

    // Get some details for later comparison.
    const int32_t originalGenericNameActualLength = u_strlen(genericName);
    const int32_t baseOffset = ucal_get(calendar, UCAL_ZONE_OFFSET, err);
    if (U_FAILURE(*err))
    {
        ucal_close(calendar);
        return;
    }

    // Allocate some additional strings for test values.
    UChar testTimeZoneId[TZID_LENGTH];
    UChar testDisplayName[DISPLAY_NAME_LENGTH];
    UChar testDisplayName2[DISPLAY_NAME_LENGTH];

    // Enumerate over all the time zones having the same base offset.
    UEnumeration* pEnum = ucal_openTimeZoneIDEnumeration(UCAL_ZONE_TYPE_CANONICAL_LOCATION, NULL, &baseOffset, err);
    if (U_FAILURE(*err))
    {
        uenum_close(pEnum);
        ucal_close(calendar);
        return;
    }

    int count = uenum_count(pEnum, err);
    if (U_FAILURE(*err))
    {
        uenum_close(pEnum);
        ucal_close(calendar);
        return;
    }

    for (int i = 0; i < count; i++)
    {
        // Get a time zone id from the enumeration to test with.
        int32_t testIdLength;
        const char* testId = uenum_next(pEnum, &testIdLength, err);
        if (U_FAILURE(*err))
        {
            // There shouldn't be a failure in enumeration, but if there was then exit.
            uenum_close(pEnum);
            ucal_close(calendar);
            return;
        }

        // Make a UChar[] version of the test time zone id for use in the API calls.
        u_uastrncpy(testTimeZoneId, testId, TZID_LENGTH);

        // Get the standard name from the test time zone.
        GetTimeZoneDisplayName_FromCalendar(locale, testTimeZoneId, timestamp, UCAL_STANDARD, testDisplayName, DISPLAY_NAME_LENGTH, err);
        if (U_FAILURE(*err))
        {
            // Failed, but keep trying through the rest of the loop in case the failure is specific to this test zone.
            continue;
        }

        // See if the test time zone has a different standard name.
        if (u_strcmp(testDisplayName, standardName) != 0)
        {
            // It has a different standard name. We can't use it.
            continue;
        }

        // Get the generic name from the test time zone.
        GetTimeZoneDisplayName_FromPattern(locale, testTimeZoneId, timestamp, GENERIC_PATTERN_UCHAR, testDisplayName, DISPLAY_NAME_LENGTH, err);
        if (U_FAILURE(*err))
        {
            // Failed, but keep trying through the rest of the loop in case the failure is specific to this test zone.
            continue;
        }

        // See if the test time zone has a longer (or same size) generic name.
        if (u_strlen(testDisplayName) >= originalGenericNameActualLength)
        {
            // The test time zone's generic name isn't any shorter than the one we already have.
            continue;
        }

        // We probably have found a better generic name.  But just to be safe, make sure the test zone isn't
        // using a generic name that is specific to a particular location.  For example, "Antarctica/Troll"
        // uses "Troll Time" as a generic name, but "Greenwich Mean Time" as a standard name.  We don't
        // want other zones that use "Greenwich Mean Time" to be labeled as "Troll Time".

        GetTimeZoneDisplayName_FromPattern(locale, testTimeZoneId, timestamp, GENERIC_LOCATION_PATTERN_UCHAR, testDisplayName2, DISPLAY_NAME_LENGTH, err);
        if (U_FAILURE(*err))
        {
            // Failed, but keep trying through the rest of the loop in case the failure is specific to this test zone.
            continue;
        }

        if (u_strcmp(testDisplayName, testDisplayName2) != 0)
        {
            // We have found a better generic name.  Use it.
            u_strcpy(genericName, testDisplayName);
            break;
        }
    }

    uenum_close(pEnum);
    ucal_close(calendar);
}

/*
Gets the localized display name that is currently in effect for the specified time zone.
*/
ResultCode GlobalizationNative_GetTimeZoneDisplayName(const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);
    if (U_FAILURE(err))
    {
        return GetResultCode(err);
    }

    // Note:  Due to how CLDR Metazones work, a past or future timestamp might use a different set of display names
    //        than are currently in effect.
    //
    //        See https://github.com/unicode-org/cldr/blob/master/common/supplemental/metaZones.xml
    //
    //        Example:  As of writing this, Africa/Algiers is in the Europe_Central metazone,
    //                  which has a standard-time name of "Central European Standard Time" (in English).
    //                  However, in some previous dates, it used the Europe_Western metazone,
    //                  having the standard-time name of "Western European Standard Time" (in English).
    //                  Only the *current* name will be returned.
    //
    //        TODO: Add a parameter for the timestamp that is used when getting the display names instead of
    //              getting "now" on the following line.  Everything else should be using this timestamp.
    //              For now, since TimeZoneInfo presently uses only a single set of display names, we will
    //              use the names associated with the *current* date and time.

    UDate timestamp = ucal_getNow();

    switch (type)
    {
        case TimeZoneDisplayName_Standard:
            GetTimeZoneDisplayName_FromCalendar(locale, timeZoneId, timestamp, UCAL_STANDARD, result, resultLength, &err);
            break;

        case TimeZoneDisplayName_DaylightSavings:
            GetTimeZoneDisplayName_FromCalendar(locale, timeZoneId, timestamp, UCAL_DST, result, resultLength, &err);
            break;

        case TimeZoneDisplayName_Generic:
            GetTimeZoneDisplayName_FromPattern(locale, timeZoneId, timestamp, GENERIC_PATTERN_UCHAR, result, resultLength, &err);
            if (U_SUCCESS(err))
            {
                FixupTimeZoneGenericDisplayName(locale, timeZoneId, timestamp, result, &err);
            }
            break;

        case TimeZoneDisplayName_GenericLocation:
            GetTimeZoneDisplayName_FromPattern(locale, timeZoneId, timestamp, GENERIC_LOCATION_PATTERN_UCHAR, result, resultLength, &err);
            break;

        case TimeZoneDisplayName_ExemplarCity:
            GetTimeZoneDisplayName_FromPattern(locale, timeZoneId, timestamp, EXEMPLAR_CITY_PATTERN_UCHAR, result, resultLength, &err);
            break;

        default:
            return UnknownError;
    }

    return GetResultCode(err);
}
