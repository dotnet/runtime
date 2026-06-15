// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCENV_WINDOWS_INL__
#define __GCENV_WINDOWS_INL__

#include "gcenv.os.h"

#include <minipal/ospagesize.h>

#define OS_PAGE_SIZE GCToOSInterface::GetPageSize()

FORCEINLINE size_t GCToOSInterface::GetPageSize()
{
    return minipal_getpagesize();
}

#endif // __GCENV_WINDOWS_INL__
