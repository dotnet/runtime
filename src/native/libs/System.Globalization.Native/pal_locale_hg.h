// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_hybrid.h"
#include "pal_common.h"

PALEXPORT const char* GlobalizationNative_GetLocaleInfoStringNative(const char* localeName, LocaleStringData localeStringData, const char* currentUILocaleName);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoIntNative(const char* localeName, LocaleNumberData localeNumberData);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData);

PALEXPORT const char* GlobalizationNative_GetDefaultLocaleNameNative(void);

PALEXPORT const char* GlobalizationNative_GetLocaleNameNative(const char* localeName);

PALEXPORT const char* GlobalizationNative_GetLocaleTimeFormatNative(const char* localeName, int shortFormat);

PALEXPORT int32_t GlobalizationNative_GetLocalesNative(UChar* locales, int32_t length);

PALEXPORT int32_t GlobalizationNative_IsPredefinedLocaleNative(const char* localeName);

