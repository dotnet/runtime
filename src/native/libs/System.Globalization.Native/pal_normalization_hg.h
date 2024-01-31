// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_compiler.h"
#include "pal_locale_hg.h"

PALEXPORT int32_t GlobalizationNative_IsNormalizedNative(NormalizationForm normalizationForm,
                                                         const uint16_t* lpStr,
                                                         int32_t cwStrLength);

PALEXPORT int32_t GlobalizationNative_NormalizeStringNative(NormalizationForm normalizationForm,
                                                            const uint16_t* lpSource,
                                                            int32_t cwSourceLength,
                                                            uint16_t* lpDst,
                                                            int32_t cwDstLength);

