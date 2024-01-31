// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_locale_hg.h"
#include "pal_compiler.h"
#include "pal_errors.h"

PALEXPORT ResultCode GlobalizationNative_GetSortHandle(const char* lpLocaleName, SortHandle** ppSortHandle);

PALEXPORT void GlobalizationNative_CloseSortHandle(SortHandle* pSortHandle);

// If we fail to get the sort version we will fallback to -1 as the sort version.
PALEXPORT int32_t GlobalizationNative_GetSortVersion(SortHandle* pSortHandle);

PALEXPORT int32_t GlobalizationNative_CompareString(SortHandle* pSortHandle,
                                                    const UChar* lpStr1,
                                                    int32_t cwStr1Length,
                                                    const UChar* lpStr2,
                                                    int32_t cwStr2Length,
                                                    int32_t options);

PALEXPORT int32_t GlobalizationNative_IndexOf(SortHandle* pSortHandle,
                                              const UChar* lpTarget,
                                              int32_t cwTargetLength,
                                              const UChar* lpSource,
                                              int32_t cwSourceLength,
                                              int32_t options,
                                              int32_t* pMatchedLength);

PALEXPORT int32_t GlobalizationNative_LastIndexOf(SortHandle* pSortHandle,
                                                  const UChar* lpTarget,
                                                  int32_t cwTargetLength,
                                                  const UChar* lpSource,
                                                  int32_t cwSourceLength,
                                                  int32_t options,
                                                  int32_t* pMatchedLength);

PALEXPORT int32_t GlobalizationNative_StartsWith(SortHandle* pSortHandle,
                                                 const UChar* lpTarget,
                                                 int32_t cwTargetLength,
                                                 const UChar* lpSource,
                                                 int32_t cwSourceLength,
                                                 int32_t options,
                                                 int32_t* pMatchedLength);

PALEXPORT int32_t GlobalizationNative_EndsWith(SortHandle* pSortHandle,
                                               const UChar* lpTarget,
                                               int32_t cwTargetLength,
                                               const UChar* lpSource,
                                               int32_t cwSourceLength,
                                               int32_t options,
                                               int32_t* pMatchedLength);

PALEXPORT int32_t GlobalizationNative_GetSortKey(SortHandle* pSortHandle,
                                                 const UChar* lpStr,
                                                 int32_t cwStrLength,
                                                 uint8_t* sortKey,
                                                 int32_t cbSortKeyLength,
                                                 int32_t options);
#if defined(APPLE_HYBRID_GLOBALIZATION)
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

#endif
