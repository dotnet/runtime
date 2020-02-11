// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#pragma once

#include "pal_compiler.h"

EXTERN_C DLLEXPORT int32_t GlobalizationNative_ToAscii(uint32_t flags,
                                              const uint16_t* lpSrc,
                                              int32_t cwSrcLength,
                                              uint16_t* lpDst,
                                              int32_t cwDstLength);

EXTERN_C DLLEXPORT int32_t GlobalizationNative_ToUnicode(uint32_t flags,
                                                const uint16_t* lpSrc,
                                                int32_t cwSrcLength,
                                                uint16_t* lpDst,
                                                int32_t cwDstLength);
