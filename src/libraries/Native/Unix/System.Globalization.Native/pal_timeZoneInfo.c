// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdint.h>

#include "pal_errors_internal.h"
#include "pal_locale_internal.h"
#include "pal_timeZoneInfo.h"

/*
Gets the localized display name for the specified time zone.
*/
ResultCode GlobalizationNative_GetTimeZoneDisplayName(const UChar* localeName,
                                                      const UChar* timeZoneId,
                                                      TimeZoneDisplayNameType type,
                                                      UChar* result,
                                                      int32_t resultLength)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, FALSE, &err);

    int32_t timeZoneIdLength = -1; // timeZoneId is NULL-terminated
    UCalendar* calendar = ucal_open(timeZoneId, timeZoneIdLength, locale, UCAL_DEFAULT, &err);

    // TODO (https://github.com/dotnet/runtime/issues/16232): need to support Generic names, but ICU "C" api
    // has no public option for this. For now, just use the ICU standard name for both Standard and Generic
    // (which is the same behavior on Windows with the mincore TIME_ZONE_INFORMATION APIs).
    ucal_getTimeZoneDisplayName(
        calendar,
        type == TimeZoneDisplayName_DaylightSavings ? UCAL_DST : UCAL_STANDARD,
        locale,
        result,
        resultLength,
        &err);

    ucal_close(calendar);
    return GetResultCode(err);
}
