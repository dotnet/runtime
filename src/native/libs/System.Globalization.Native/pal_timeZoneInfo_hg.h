// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale_hg.h"
#include "pal_compiler.h"
#include "pal_errors.h"

PALEXPORT int32_t GlobalizationNative_WindowsIdToIanaId(const UChar* windowsId, const char* region, UChar* ianaId, int32_t ianaIdLength);
PALEXPORT int32_t GlobalizationNative_IanaIdToWindowsId(const UChar* ianaId, UChar* windowsId, int32_t windowsIdLength);
PALEXPORT ResultCode GlobalizationNative_GetTimeZoneDisplayName(const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength);
#if defined(APPLE_HYBRID_GLOBALIZATION)
PALEXPORT int32_t GlobalizationNative_GetTimeZoneDisplayNameNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* timeZoneId, int32_t timeZoneIdLength,
                                                                   TimeZoneDisplayNameType type, uint16_t* result, int32_t resultLength);
#endif

