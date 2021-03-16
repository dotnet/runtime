// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdbool.h>
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
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);

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

/*
Convert Windows Time Zone Id to IANA Id
*/
int32_t GlobalizationNative_WindowsIdToIanaId(const UChar* windowsId, UChar* ianaId, int32_t ianaIdLength)
{
    UErrorCode status = U_ZERO_ERROR;

    if (ucal_getTimeZoneIDForWindowsID_ptr != NULL)
    {
        int32_t ianaIdFilledLength = ucal_getTimeZoneIDForWindowsID(windowsId, -1, NULL, ianaId, ianaIdLength, &status);
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
