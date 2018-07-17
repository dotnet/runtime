// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "hostallocator.h"

void* HostAllocator::allocateHostMemory(size_t size)
{
    assert(g_jitHost != nullptr);
    return g_jitHost->allocateMemory(size);
}

void HostAllocator::freeHostMemory(void* p)
{
    assert(g_jitHost != nullptr);
    g_jitHost->freeMemory(p);
}
