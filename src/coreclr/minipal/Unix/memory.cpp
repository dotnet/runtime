// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//

#include <inttypes.h>
#include <stdlib.h>
#include "minipal.h"

void* VMToOSInterface::AlignedAllocate(size_t alignment, size_t size)
{
    void* memptr;
    if (posix_memalign(&memptr, alignment, size) == 0)
    {
        return memptr;
    }
    return NULL;
}

void VMToOSInterface::AlignedFree(void* memblock)
{
    free(memblock);
}
