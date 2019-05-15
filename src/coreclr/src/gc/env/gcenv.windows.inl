// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GCENV_WINDOWS_INL__
#define __GCENV_WINDOWS_INL__

#include "gcenv.os.h"


#define OS_PAGE_SIZE GCToOSInterface::GetPageSize()

__forceinline size_t GCToOSInterface::GetPageSize()
{
    return 0x1000;
}

#endif // __GCENV_WINDOWS_INL__
