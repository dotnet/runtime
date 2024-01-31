// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoInt(const UChar* localeName,
                                                       LocaleNumberData localeNumberData,
                                                       int32_t* value);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(const UChar* localeName,
                                                                 LocaleNumberData localeGroupingData,
                                                                 int32_t* primaryGroupSize,
                                                                 int32_t* secondaryGroupSize);

