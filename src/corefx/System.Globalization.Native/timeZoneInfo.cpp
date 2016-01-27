// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <stdint.h>
#include <unistd.h>
#include <unicode/ucal.h>

#include "locale.hpp"
#include "holders.h"
#include "errors.h"

/*
Gets the symlink value for the path.
*/
extern "C" int32_t GlobalizationNative_ReadLink(const char* path, char* result, size_t resultCapacity)
{
    ssize_t r = readlink(path, result, resultCapacity - 1); // subtract one to make room for the NULL character

    if (r < 1 || r >= resultCapacity)
        return false;

    result[r] = '\0';
    return true;
}

/*
These values should be kept in sync with the managed Interop.GlobalizationInterop.TimeZoneDisplayNameType enum.
*/
enum TimeZoneDisplayNameType : int32_t
{
    Generic = 0,
    Standard = 1,
    DaylightSavings = 2,
};

/*
Gets the localized display name for the specified time zone.
*/
extern "C" ResultCode GlobalizationNative_GetTimeZoneDisplayName(
    const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);

    int32_t timeZoneIdLength = -1; // timeZoneId is NULL-terminated
    UCalendar* calendar = ucal_open(timeZoneId, timeZoneIdLength, locale, UCAL_DEFAULT, &err);
    UCalendarHolder calendarHolder(calendar, err);

    // TODO (https://github.com/dotnet/corefx/issues/5741): need to support Generic names, but ICU "C" api
    // has no public option for this. For now, just use the ICU standard name for both Standard and Generic
    // (which is the same behavior on Windows with the mincore TIME_ZONE_INFORMATION APIs).
    ucal_getTimeZoneDisplayName(
        calendar, type == DaylightSavings ? UCAL_DST : UCAL_STANDARD, locale, result, resultLength, &err);

    return GetResultCode(err);
}
