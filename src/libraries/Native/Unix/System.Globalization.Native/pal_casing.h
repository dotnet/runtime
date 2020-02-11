// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

EXTERN_C DLLEXPORT void GlobalizationNative_ChangeCase(const uint16_t* lpSrc,
                                              int32_t cwSrcLength,
                                              uint16_t* lpDst,
                                              int32_t cwDstLength,
                                              int32_t bToUpper);

EXTERN_C DLLEXPORT void GlobalizationNative_ChangeCaseInvariant(const uint16_t* lpSrc,
                                                       int32_t cwSrcLength,
                                                       uint16_t* lpDst,
                                                       int32_t cwDstLength,
                                                       int32_t bToUpper);

EXTERN_C DLLEXPORT void GlobalizationNative_ChangeCaseTurkish(const uint16_t* lpSrc,
                                                     int32_t cwSrcLength,
                                                     uint16_t* lpDst,
                                                     int32_t cwDstLength,
                                                     int32_t bToUpper);
