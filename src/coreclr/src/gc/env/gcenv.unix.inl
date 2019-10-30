// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCENV_UNIX_INL__
#define __GCENV_UNIX_INL__

#include "gcenv.os.h"

extern uint32_t g_pageSizeUnixInl;

#define OS_PAGE_SIZE GCToOSInterface::GetPageSize()

__forceinline size_t GCToOSInterface::GetPageSize()
{
    return g_pageSizeUnixInl;
}

#endif // __GCENV_UNIX_INL__
