// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_locale_hg.h"
#include "pal_compiler.h"
#include "pal_errors.h"

PALEXPORT int32_t GlobalizationNative_CompareStringNative(const uint16_t* localeName,
                                                          int32_t lNameLength,
                                                          const uint16_t* lpTarget,
                                                          int32_t cwTargetLength,
                                                          const uint16_t* lpSource,
                                                          int32_t cwSourceLength,
                                                          int32_t options);

PALEXPORT Range GlobalizationNative_IndexOfNative(const uint16_t* localeName,
                                                  int32_t lNameLength,
                                                  const uint16_t* lpTarget,
                                                  int32_t cwTargetLength,
                                                  const uint16_t* lpSource,
                                                  int32_t cwSourceLength,
                                                  int32_t options,
                                                  int32_t fromBeginning);   

PALEXPORT int32_t GlobalizationNative_StartsWithNative(const uint16_t* localeName,
                                                       int32_t lNameLength,
                                                       const uint16_t* lpPrefix,
                                                       int32_t cwPrefixLength,
                                                       const uint16_t* lpSource,
                                                       int32_t cwSourceLength,
                                                       int32_t options);

PALEXPORT int32_t GlobalizationNative_EndsWithNative(const uint16_t* localeName,
                                                     int32_t lNameLength,
                                                     const uint16_t* lpSuffix,
                                                     int32_t cwSuffixLength,
                                                     const uint16_t* lpSource,
                                                     int32_t cwSourceLength,
                                                     int32_t options);

PALEXPORT int32_t GlobalizationNative_GetSortKeyNative(const uint16_t* localeName,
                                                       int32_t lNameLength,
                                                       const UChar* lpStr,
                                                       int32_t cwStrLength,
                                                       uint8_t* sortKey,
                                                       int32_t cbSortKeyLength,
                                                       int32_t options);

