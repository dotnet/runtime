// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#if defined(TARGET_UNIX)
#include <stdatomic.h>
#elif defined(TARGET_WINDOWS)
#include "windows.h"
#endif

// The comparand should be the address to the pointer containing the value to compare to.
// this matches Unix __atomic_compare_exchange_n, which compares the contents of the pointer.
static int pal_atomic_cas_ptr(void* volatile* dest, void* exchange, void* comparand)
{
#if defined(TARGET_UNIX)
    return __atomic_compare_exchange_n(dest, comparand, exchange, false, __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST);
#elif defined(TARGET_WINDOWS)
    void *value = *((void**)comparand);
    return InterlockedCompareExchangePointer(dest, exchange, value) == value;
#endif
}
