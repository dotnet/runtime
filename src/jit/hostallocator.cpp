// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "hostallocator.h"

HostAllocator HostAllocator::s_hostAllocator;

void* HostAllocator::Alloc(size_t size)
{
    assert(g_jitHost != nullptr);
    return g_jitHost->allocateMemory(size, false);
}

void* HostAllocator::ArrayAlloc(size_t elemSize, size_t numElems)
{
    assert(g_jitHost != nullptr);

    ClrSafeInt<size_t> safeElemSize(elemSize);
    ClrSafeInt<size_t> safeNumElems(numElems);
    ClrSafeInt<size_t> size = safeElemSize * safeNumElems;
    if (size.IsOverflow())
    {
        return nullptr;
    }

    return g_jitHost->allocateMemory(size.Value(), false);
}

void HostAllocator::Free(void* p)
{
    assert(g_jitHost != nullptr);
    g_jitHost->freeMemory(p, false);
}

HostAllocator* HostAllocator::getHostAllocator()
{
    return &s_hostAllocator;
}
