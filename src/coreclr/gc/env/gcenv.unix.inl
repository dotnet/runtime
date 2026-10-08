// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCENV_UNIX_INL__
#define __GCENV_UNIX_INL__

#include "gcenv.os.h"

#include <minipal/ospagesize.h>

#define OS_PAGE_SIZE GCToOSInterface::GetPageSize()

#ifndef DACCESS_COMPILE
FORCEINLINE size_t GCToOSInterface::GetPageSize()
{
#if defined(__wasm__)
    return minipal_getpagesize();
#else
    extern uint32_t g_pageSizeUnixInl;
    return g_pageSizeUnixInl;
#endif // defined(__wasm__)
}
#endif // DACCESS_COMPILE

#endif // __GCENV_UNIX_INL__
