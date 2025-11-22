// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <windows.h>
#include <inttypes.h>
#include <assert.h>
#include <malloc.h>
#include "minipal.h"

void* VMToOSInterface::AlignedAllocate(size_t alignment, size_t size)
{
    return _aligned_malloc(size, alignment);
}

void VMToOSInterface::AlignedFree(void *memblock)
{
    _aligned_free(memblock);
}
