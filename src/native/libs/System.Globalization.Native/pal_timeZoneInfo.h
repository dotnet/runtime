// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"
#include "pal_errors.h"

PALEXPORT int32_t GlobalizationNative_WindowsIdToIanaId(const UChar* windowsId, const char* region, UChar* ianaId, int32_t ianaIdLength);
PALEXPORT int32_t GlobalizationNative_IanaIdToWindowsId(const UChar* ianaId, UChar* windowsId, int32_t windowsIdLength);
PALEXPORT ResultCode GlobalizationNative_GetTimeZoneDisplayName(const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength);
