// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale_hg.h"
#include "pal_compiler.h"
#include "pal_errors.h"

PALEXPORT int32_t GlobalizationNative_GetTimeZoneDisplayNameNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* timeZoneId, int32_t timeZoneIdLength,
                                                                   TimeZoneDisplayNameType type, uint16_t* result, int32_t resultLength);


