// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"
#include "pal_errors.h"

/*
These values should be kept in sync with the managed Interop.GlobalizationInterop.TimeZoneDisplayNameType enum.
*/
typedef enum
{
    TimeZoneDisplayName_Generic = 0,
    TimeZoneDisplayName_Standard = 1,
    TimeZoneDisplayName_DaylightSavings = 2,
    TimeZoneDisplayName_GenericLocation = 3,
    TimeZoneDisplayName_ExemplarCity = 4,
    TimeZoneDisplayName_TimeZoneName = 5,
} TimeZoneDisplayNameType;
PALEXPORT int32_t GlobalizationNative_WindowsIdToIanaId(const UChar* windowsId, const char* region, UChar* ianaId, int32_t ianaIdLength);
PALEXPORT int32_t GlobalizationNative_IanaIdToWindowsId(const UChar* ianaId, UChar* windowsId, int32_t windowsIdLength);
PALEXPORT ResultCode GlobalizationNative_GetTimeZoneDisplayName(const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength);
#if defined(APPLE_HYBRID_GLOBALIZATION)
PALEXPORT int32_t GlobalizationNative_GetTimeZoneDisplayNameNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* timeZoneId, int32_t timeZoneIdLength,
                                                                   TimeZoneDisplayNameType type, uint16_t* result, int32_t resultLength);
#endif
