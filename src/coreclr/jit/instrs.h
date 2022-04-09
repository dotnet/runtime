// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_XARCH)
#include "instrsxarch.h"
#elif defined(TARGET_ARM)
#include "instrsarm.h"
#elif defined(TARGET_ARM64)
#include "instrsarm64.h"
#elif defined(TARGET_LOONGARCH64)
#include "instrsloongarch64.h"
#else
#error Unsupported or unset target architecture
#endif // target type
