// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

bool InterfaceDispatch_InitializePal()
{
    return true;
}

// Allocate memory aligned at sizeof(void*)*2 boundaries
void *InterfaceDispatch_AllocDoublePointerAligned(size_t size)
{
    return (void*)SystemDomain::GetGlobalLoaderAllocator()->GetHighFrequencyHeap()->AllocAlignedMem(size, sizeof(TADDR) * 2);
}

// Allocate memory aligned at sizeof(void*) boundaries

void *InterfaceDispatch_AllocPointerAligned(size_t size)
{
    return (void*)SystemDomain::GetGlobalLoaderAllocator()->GetHighFrequencyHeap()->AllocAlignedMem(size, sizeof(TADDR));
}
