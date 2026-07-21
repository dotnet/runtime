// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ALLOC_H_
#define _ALLOC_H_

#if !defined(_HOST_H_)
#include "host.h"
#endif // defined(_HOST_H_)

#include "arenaallocator.h"
#include "compallocator.h"

// CompMemKind values are used to tag memory allocations performed via
// the compiler's allocator so that the memory usage of various compiler
// components can be tracked separately (when MEASURE_MEM_ALLOC is defined).

enum CompMemKind
{
#define CompMemKindMacro(kind) CMK_##kind,
#include "compmemkind.h"
    CMK_Count
};

// JitMemKindTraits provides the traits required by ArenaAllocator and CompAllocator templates.
struct JitMemKindTraits
{
    using MemKind                  = CompMemKind;
    static constexpr int     Count = CMK_Count;
    static const char* const Names[];

    // Returns true if the allocator should bypass the host allocator and use direct malloc/free.
    static bool bypassHostAllocator()
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

    // Returns true if the allocator should inject faults for testing purposes.
    static bool shouldInjectFault()
    {
#if defined(DEBUG)
        return JitConfig.ShouldInjectFault() != 0;
#else
        return false;
#endif
    }

    // Allocates a block of memory from the host.
    static void* allocateHostMemory(size_t size, size_t* pActualSize);

    // Frees a block of memory previously allocated by allocateHostMemory.
    static void freeHostMemory(void* block, size_t size);

    // Fills a memory block with an uninitialized pattern (for DEBUG builds).
    static void fillWithUninitializedPattern(void* block, size_t size)
    {
#if defined(DEBUG)
        memset(block, UninitializedWord<char>(nullptr), size);
#else
        (void)block;
        (void)size;
#endif
    }

    // Called when allocation fails - calls NOMEM() which does not return.
    static void DECLSPEC_NORETURN outOfMemory()
    {
        NOMEM();
    }
};

// Type aliases for JIT-specific instantiations of the shared allocator templates.
// These are the allocator types used throughout the JIT.
using ArenaAllocator = ArenaAllocatorT<JitMemKindTraits>;
using CompAllocator  = CompAllocatorT<JitMemKindTraits>;
using CompIAllocator = CompIAllocatorT<JitMemKindTraits>;

// Type aliases for memory statistics
using JitMemStats          = MemStats<JitMemKindTraits>;
using JitAggregateMemStats = AggregateMemStats<JitMemKindTraits>;

#if MEASURE_MEM_ALLOC
// JitMemStatsInfo provides static aggregate statistics management for JIT compilations.
struct JitMemStatsInfo
{
    static CritSecObject        s_statsLock;
    static JitAggregateMemStats s_aggStats;
    static JitMemStats          s_maxStats;

    // Finishes per-method stats and adds to aggregate stats.
    static void finishMemStats(ArenaAllocator* arena);

    // Dumps aggregate stats to file.
    static void dumpAggregateMemStats(FILE* file);

    // Dumps max method stats to file.
    static void dumpMaxMemStats(FILE* file);
};
#endif // MEASURE_MEM_ALLOC

#endif // _ALLOC_H_
