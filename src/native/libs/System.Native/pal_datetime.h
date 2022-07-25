// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

PALEXPORT int64_t SystemNative_GetSystemTimeAsTicks(void);

#if defined(TARGET_ANDROID) || defined(__APPLE__)
PALEXPORT char* SystemNative_GetDefaultTimeZone(void);
#endif
