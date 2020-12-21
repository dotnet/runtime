// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCENV_UNIX_INL__
#define __GCENV_UNIX_INL__

#include "gcenv.os.h"

extern uint32_t g_pageSizeUnixInl;

#define OS_PAGE_SIZE GCToOSInterface::GetPageSize()

#ifndef DACCESS_COMPILE
__forceinline size_t GCToOSInterface::GetPageSize()
{
    return g_pageSizeUnixInl;
}
#endif // DACCESS_COMPILE

#endif // __GCENV_UNIX_INL__
