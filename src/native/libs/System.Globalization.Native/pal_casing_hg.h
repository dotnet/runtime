// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_locale_hg.h"
#include "pal_compiler.h"

PALEXPORT void GlobalizationNative_InitOrdinalCasingPage(int32_t pageNumber, UChar* pTarget);

PALEXPORT int32_t GlobalizationNative_ChangeCaseNative(const uint16_t* localeName,
                                                       int32_t lNameLength,
                                                       const uint16_t* lpSrc,
                                                       int32_t cwSrcLength,
                                                       uint16_t* lpDst,
                                                       int32_t cwDstLength,
                                                       int32_t bToUpper);

PALEXPORT int32_t GlobalizationNative_ChangeCaseInvariantNative(const uint16_t* lpSrc,
                                                                int32_t cwSrcLength,
                                                                uint16_t* lpDst,
                                                                int32_t cwDstLength,
                                                                int32_t bToUpper);

