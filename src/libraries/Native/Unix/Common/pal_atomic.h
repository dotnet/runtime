// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#if defined(TARGET_UNIX)
#include <stdatomic.h>
#elif defined(TARGET_WINDOWS)
#include "windows.h"
#endif

// The args passed in should match InterlockedCompareExchangePointer Windows API
static int pal_atomic_cas_ptr(void* volatile* dest, void* exchange, void* comparand)
{
#if defined(TARGET_UNIX)
    return __atomic_compare_exchange_n(dest, &comparand, exchange, false, __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST);
#elif defined(TARGET_WINDOWS)
    return InterlockedCompareExchangePointer(dest, exchange, comparand) == comparand;
#endif
}
