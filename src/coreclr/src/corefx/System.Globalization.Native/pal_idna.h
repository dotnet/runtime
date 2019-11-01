// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include "pal_compiler.h"
#include "pal_locale.h"

DLLEXPORT int32_t GlobalizationNative_ToAscii(uint32_t flags,
                                              const UChar* lpSrc,
                                              int32_t cwSrcLength,
                                              UChar* lpDst,
                                              int32_t cwDstLength);

DLLEXPORT int32_t GlobalizationNative_ToUnicode(int32_t flags,
                                                const UChar* lpSrc,
                                                int32_t cwSrcLength,
                                                UChar* lpDst,
                                                int32_t cwDstLength);
