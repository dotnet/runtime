// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#pragma once

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

EXTERN_C DLLEXPORT int32_t GlobalizationNative_IsNormalized(NormalizationForm normalizationForm,
                                                   const uint16_t* lpStr,
                                                   int32_t cwStrLength);

EXTERN_C DLLEXPORT int32_t GlobalizationNative_NormalizeString(NormalizationForm normalizationForm,
                                                      const uint16_t* lpSrc,
                                                      int32_t cwSrcLength,
                                                      uint16_t* lpDst,
                                                      int32_t cwDstLength);
