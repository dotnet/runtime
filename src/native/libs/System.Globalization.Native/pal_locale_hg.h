// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_common.h"

PALEXPORT int32_t GlobalizationNative_GetLocales(UChar *value, int32_t valueLength);

PALEXPORT int32_t GlobalizationNative_GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength);

PALEXPORT int32_t GlobalizationNative_GetDefaultLocaleName(UChar* value, int32_t valueLength);

PALEXPORT int32_t GlobalizationNative_IsPredefinedLocale(const UChar* localeName);

PALEXPORT int32_t GlobalizationNative_GetLocaleTimeFormat(const UChar* localeName,
                                                          int shortFormat, UChar* value,
                                                          int32_t valueLength);
PALEXPORT int32_t GlobalizationNative_GetLocaleInfoInt(const UChar* localeName,
                                                       LocaleNumberData localeNumberData,
                                                       int32_t* value);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(const UChar* localeName,
                                                                 LocaleNumberData localeGroupingData,
                                                                 int32_t* primaryGroupSize,
                                                                 int32_t* secondaryGroupSize);
PALEXPORT int32_t GlobalizationNative_GetLocaleInfoString(const UChar* localeName,
                                                          LocaleStringData localeStringData,
                                                          UChar* value,
                                                          int32_t valueLength,
                                                          const UChar* uiLocaleName);
#if defined(APPLE_HYBRID_GLOBALIZATION)
PALEXPORT const char* GlobalizationNative_GetLocaleInfoStringNative(const char* localeName, LocaleStringData localeStringData, const char* currentUILocaleName);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoIntNative(const char* localeName,
                                                             LocaleNumberData localeNumberData);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative(const char* localeName,
                                                                      LocaleNumberData localeGroupingData);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative(const char* localeName,
                                                                           LocaleNumberData localeGroupingData);

PALEXPORT const char* GlobalizationNative_GetDefaultLocaleNameNative(void);

PALEXPORT const char* GlobalizationNative_GetLocaleNameNative(const char* localeName);

PALEXPORT const char* GlobalizationNative_GetLocaleTimeFormatNative(const char* localeName, int shortFormat);

PALEXPORT int32_t GlobalizationNative_GetLocalesNative(UChar* locales, int32_t length);

PALEXPORT int32_t GlobalizationNative_IsPredefinedLocaleNative(const char* localeName);

#endif
