// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPALLOC_H_
#define _INTERPALLOC_H_

// Include shared arena allocator infrastructure
#include "../jitshared/arenaallocator.h"
#include "../jitshared/compallocator.h"
#include "../jitshared/memstats.h"
#include "../jitshared/histogram.h"

// InterpMemKind values are used to tag memory allocations performed via
// the compiler's allocator so that the memory usage of various compiler
// components can be tracked separately (when MEASURE_MEM_ALLOC is defined).

enum InterpMemKind
{
#define InterpMemKindMacro(kind) IMK_##kind,
#include "interpmemkind.h"
    IMK_Count
};

// InterpMemKindTraits provides the traits required by MemStats and CompAllocator templates.
struct InterpMemKindTraits
{
    using MemKind = InterpMemKind;
    static constexpr int Count = IMK_Count;
    static const char* const Names[];

    // Returns true if the allocator should bypass the host allocator and use direct malloc/free.
    static bool bypassHostAllocator();

    // Returns true if the allocator should inject faults for testing purposes.
    static bool shouldInjectFault();

    // Allocates a block of memory from the host.
    static void* allocateHostMemory(size_t size, size_t* pActualSize);

    // Frees a block of memory previously allocated by allocateHostMemory.
    static void freeHostMemory(void* block, size_t size);

    // Fills a memory block with an uninitialized pattern (for DEBUG builds).
    static void fillWithUninitializedPattern(void* block, size_t size);

    // Called when allocation fails - calls NOMEM() which does not return.
    static void outOfMemory();
};

// InterpArenaAllocator is the arena allocator type used for interpreter compilations.
using InterpArenaAllocator = ArenaAllocatorT<InterpMemKindTraits>;

// InterpAllocator is the allocator type used for interpreter compilations.
// It wraps ArenaAllocator and tracks allocations by InterpMemKind.
using InterpAllocator = CompAllocatorT<InterpMemKindTraits>;

#endif // _INTERPALLOC_H_
