// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_locale.h"
#include "pal_compiler.h"

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
