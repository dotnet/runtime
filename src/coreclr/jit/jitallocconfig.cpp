// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "jitallocconfig.h"

//------------------------------------------------------------------------
// JitAllocatorConfig::bypassHostAllocator:
//    Indicates whether or not the ArenaAllocator should bypass the JIT
//    host when allocating memory for arena pages.
//
// Return Value:
//    True if the JIT should bypass the JIT host; false otherwise.
bool JitAllocatorConfig::bypassHostAllocator()
{
#if defined(DEBUG)
    // When JitDirectAlloc is set, all JIT allocations requests are forwarded
    // directly to the OS. This allows taking advantage of pageheap and other gflag
    // knobs for ensuring that we do not have buffer overruns in the JIT.

    return JitConfig.JitDirectAlloc() != 0;
#else  // defined(DEBUG)
    return false;
#endif // !defined(DEBUG)
}

//------------------------------------------------------------------------
// JitAllocatorConfig::shouldInjectFault:
//    Indicates whether or not the ArenaAllocator should inject faults
//    for testing purposes.
//
// Return Value:
//    True if fault injection is enabled; false otherwise.
bool JitAllocatorConfig::shouldInjectFault()
{
#if defined(DEBUG)
    return JitConfig.ShouldInjectFault() != 0;
#else
    return false;
#endif
}

//------------------------------------------------------------------------
// JitAllocatorConfig::allocateHostMemory:
//    Allocates memory from the host (or the OS if `bypassHostAllocator()`
//    returns `true`).
//
// Arguments:
//    size - The number of bytes to allocate.
//    pActualSize - The number of bytes actually allocated.
//
// Return Value:
//    A pointer to the allocated memory.
void* JitAllocatorConfig::allocateHostMemory(size_t size, size_t* pActualSize)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        *pActualSize = size;
        if (size == 0)
        {
            size = 1;
        }
        void* p = malloc(size);
        if (p == nullptr)
        {
            NOMEM();
        }
        return p;
    }
#endif // !defined(DEBUG)

    return g_jitHost->allocateSlab(size, pActualSize);
}

//------------------------------------------------------------------------
// JitAllocatorConfig::freeHostMemory:
//    Frees memory allocated by a previous call to `allocateHostMemory`.
//
// Arguments:
//    block - A pointer to the memory to free.
//    size - The size of the block.
void JitAllocatorConfig::freeHostMemory(void* block, size_t size)
{
#if defined(DEBUG)
    if (bypassHostAllocator())
    {
        free(block);
        return;
    }
#endif // !defined(DEBUG)

    g_jitHost->freeSlab(block, size);
}

//------------------------------------------------------------------------
// JitAllocatorConfig::fillWithUninitializedPattern:
//    Fills a memory block with a pattern to help catch use-before-init bugs.
//
// Arguments:
//    block - Pointer to the memory to fill.
//    size - The size of the block in bytes.
void JitAllocatorConfig::fillWithUninitializedPattern(void* block, size_t size)
{
#if defined(DEBUG)
    memset(block, UninitializedWord<char>(nullptr), size);
#else
    (void)block;
    (void)size;
#endif
}
