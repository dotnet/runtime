// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <stdint.h>
#include <unistd.h>

#include "pal_locale_internal.h"
#include "pal_errors_internal.h"
#include "pal_timeZoneInfo.h"

/*
Gets the localized display name for the specified time zone.
*/
ResultCode GlobalizationNative_GetTimeZoneDisplayName(const uint16_t* localeName,
                                                      const uint16_t* timeZoneId,
                                                      TimeZoneDisplayNameType type,
                                                      uint16_t* result,
                                                      int32_t resultLength)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    UChar *resultTmp = (UChar*)result;
    GetLocale((UChar*)localeName, locale, ULOC_FULLNAME_CAPACITY, FALSE, &err);

    int32_t timeZoneIdLength = -1; // timeZoneId is NULL-terminated
    UCalendar* calendar = ucal_open((UChar*)timeZoneId, timeZoneIdLength, locale, UCAL_DEFAULT, &err);

    // TODO (https://github.com/dotnet/corefx/issues/5741): need to support Generic names, but ICU "C" api
    // has no public option for this. For now, just use the ICU standard name for both Standard and Generic
    // (which is the same behavior on Windows with the mincore TIME_ZONE_INFORMATION APIs).
    ucal_getTimeZoneDisplayName(
        calendar,
        type == TimeZoneDisplayName_DaylightSavings ? UCAL_DST : UCAL_STANDARD,
        locale,
        resultTmp,
        resultLength,
        &err);

    ucal_close(calendar);
    result = (uint16_t*)resultTmp;
    return GetResultCode(err);
}
