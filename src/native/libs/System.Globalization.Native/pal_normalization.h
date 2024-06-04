// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"

/*
 * These values should be kept in sync with System.Text.NormalizationForm
 */
typedef enum
{
    FormC = 0x1,
    FormD = 0x2,
    FormKC = 0x5,
    FormKD = 0x6
} NormalizationForm;
PALEXPORT int32_t GlobalizationNative_IsNormalized(NormalizationForm normalizationForm,
                                                   const UChar* lpStr,
                                                   int32_t cwStrLength);

PALEXPORT int32_t GlobalizationNative_NormalizeString(NormalizationForm normalizationForm,
                                                      const UChar* lpSrc,
                                                      int32_t cwSrcLength,
                                                      UChar* lpDst,
                                                      int32_t cwDstLength);
#if defined(APPLE_HYBRID_GLOBALIZATION)
PALEXPORT int32_t GlobalizationNative_IsNormalizedNative(NormalizationForm normalizationForm,
                                                         const uint16_t* lpStr,
                                                         int32_t cwStrLength);

PALEXPORT int32_t GlobalizationNative_NormalizeStringNative(NormalizationForm normalizationForm,
                                                            const uint16_t* lpSource,
                                                            int32_t cwSourceLength,
                                                            uint16_t* lpDst,
                                                            int32_t cwDstLength);
#endif
