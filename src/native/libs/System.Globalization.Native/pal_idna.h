// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"

#if defined(__APPLE__) && !(defined(TARGET_OS_OSX) && !defined(TARGET_OS_IPHONE))
#include <unicode/utypes.h>
#include <unicode/uidna.h>
#endif

PALEXPORT int32_t GlobalizationNative_ToAscii(uint32_t flags,
                                              const UChar* lpSrc,
                                              int32_t cwSrcLength,
                                              UChar* lpDst,
                                              int32_t cwDstLength);

PALEXPORT int32_t GlobalizationNative_ToUnicode(uint32_t flags,
                                                const UChar* lpSrc,
                                                int32_t cwSrcLength,
                                                UChar* lpDst,
                                                int32_t cwDstLength);
