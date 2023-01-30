// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "numasupport.h"

#if HAVE_NUMA_H
#define PER_FUNCTION_BLOCK(fn) decltype(fn)* fn##_ptr = fn;
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

#endif // HAVE_NUMA_H

// The highest NUMA node available
int g_highestNumaNode = 0;
// Is numa available
bool g_numaAvailable = false;

void NUMASupportInitialize()
{
#if HAVE_NUMA_H
    if (numa_available() != -1)
    {
        g_numaAvailable = true;
        g_highestNumaNode = numa_max_node();
    }
#endif // HAVE_NUMA_H
}

void NUMASupportCleanup()
{
    // nop
}
