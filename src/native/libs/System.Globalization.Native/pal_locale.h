// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"

PALEXPORT int32_t GlobalizationNative_GetLocales(UChar *value, int32_t valueLength);

PALEXPORT int32_t GlobalizationNative_GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength);

PALEXPORT int32_t GlobalizationNative_GetDefaultLocaleName(UChar* value, int32_t valueLength);

PALEXPORT int32_t GlobalizationNative_IsPredefinedLocale(const UChar* localeName);

PALEXPORT int32_t GlobalizationNative_GetLocaleTimeFormat(const UChar* localeName,
                                                          int shortFormat, UChar* value,
                                                          int32_t valueLength);

