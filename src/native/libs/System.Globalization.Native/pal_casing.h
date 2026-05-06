// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_locale.h"
#include "pal_compiler.h"

PALEXPORT void GlobalizationNative_InitOrdinalCasingPage(int32_t pageNumber, UChar* pTarget);

PALEXPORT void GlobalizationNative_ChangeCase(const UChar* lpSrc,
                                              int32_t cwSrcLength,
                                              UChar* lpDst,
                                              int32_t cwDstLength,
                                              int32_t bToUpper);

PALEXPORT void GlobalizationNative_ChangeCaseInvariant(const UChar* lpSrc,
                                                       int32_t cwSrcLength,
                                                       UChar* lpDst,
                                                       int32_t cwDstLength,
                                                       int32_t bToUpper);

PALEXPORT void GlobalizationNative_ChangeCaseTurkish(const UChar* lpSrc,
                                                     int32_t cwSrcLength,
                                                     UChar* lpDst,
                                                     int32_t cwDstLength,
                                                     int32_t bToUpper);
#if defined(APPLE_HYBRID_GLOBALIZATION)
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
#endif
