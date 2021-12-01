// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_autoreleasepool.h"
#include <stdlib.h>
#include "pal_utilities.h"

#ifndef _MSC_VER
// Don't warning about not declaring a function with [[noreturn]] since it's only true in Debug mode.
#pragma GCC diagnostic ignored "-Wmissing-noreturn"
#endif

// These functions should not be used, but they need to be defined
// to satisfy the tooling we used to enable redirecting P/Invokes
// for the single file scenario.
void* SystemNative_CreateAutoreleasePool(void)
{
    assert_err(false, "Autorelease pools not supported on this platform.", EINVAL);
    return NULL;
}

void SystemNative_DrainAutoreleasePool(void* pool)
{
    (void)pool;
    assert_err(false, "Autorelease pools not supported on this platform.", EINVAL);
}
