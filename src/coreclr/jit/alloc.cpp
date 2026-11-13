// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#if defined(_MSC_VER)
#pragma hdrstop
#endif // defined(_MSC_VER)

// Define the names for JitMemKindTraits
const char* const JitMemKindTraits::Names[] = {
#define CompMemKindMacro(kind) #kind,
#include "compmemkind.h"
};

//------------------------------------------------------------------------
// JitMemKindTraits::allocateHostMemory:
//    Allocates memory from the host (or the OS if `bypassHostAllocator()`
//    returns `true`).
//
// Arguments:
//    size - The number of bytes to allocate.
//    pActualSize - The number of bytes actually allocated.
//
// Return Value:
//    A pointer to the allocated memory.
void* JitMemKindTraits::allocateHostMemory(size_t size, size_t* pActualSize)
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
// JitMemKindTraits::freeHostMemory:
//    Frees memory allocated by a previous call to `allocateHostMemory`.
//
// Arguments:
//    block - A pointer to the memory to free.
//    size - The size of the block.
void JitMemKindTraits::freeHostMemory(void* block, size_t size)
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

#if MEASURE_MEM_ALLOC
CritSecObject        JitMemStatsInfo::s_statsLock;
JitAggregateMemStats JitMemStatsInfo::s_aggStats;
JitMemStats          JitMemStatsInfo::s_maxStats;

void JitMemStatsInfo::finishMemStats(ArenaAllocator* arena)
{
    arena->finishMemStats();

    CritSecHolder statsLock(s_statsLock);
    JitMemStats&  stats = arena->getStats();
    s_aggStats.Add(stats);
    if (stats.allocSz > s_maxStats.allocSz)
    {
        s_maxStats = stats;
    }
}

void JitMemStatsInfo::dumpAggregateMemStats(FILE* file)
{
    s_aggStats.Print(file);
}

void JitMemStatsInfo::dumpMaxMemStats(FILE* file)
{
    s_maxStats.Print(file);
}
#endif // MEASURE_MEM_ALLOC

#ifdef JIT_STANDALONE_BUILD

// Note that code in libstdc++ (for example) may call the global new
// during thread initialization etc. so don't assert during init.

void* __cdecl operator new(std::size_t size)
{
    assert((g_jitHost == nullptr) || !"Global new called; use HostAllocator if long-lived allocation was intended");

    if (size == 0)
    {
        size++;
    }

    void* result = malloc(size);
    if (result == nullptr)
    {
        throw std::bad_alloc{};
    }

    return result;
}

void* __cdecl operator new[](std::size_t size)
{
    assert((g_jitHost == nullptr) || !"Global new called; use HostAllocator if long-lived allocation was intended");

    if (size == 0)
    {
        size++;
    }

    void* result = malloc(size);
    if (result == nullptr)
    {
        throw std::bad_alloc{};
    }

    return result;
}

void __cdecl operator delete(void* ptr) noexcept
{
    free(ptr);
}

void __cdecl operator delete[](void* ptr) noexcept
{
    free(ptr);
}

// Code linked into the JIT also uses the nothrow and sized forms: the PAL allocates its per-thread data and
// synchronization state with new (std::nothrow), at any time including thread exit, and frees it with delete.
// Override these forms as well so that every allocation and deallocation in the JIT pairs malloc with free.
// Otherwise the nothrow forms come from the C++ runtime library, and in a host that replaces the global
// operator new they resolve to the host's allocator while the deletes above call free(). They do not assert:
// these allocations are made by the PAL outside of compilation, not by the JIT.

void* __cdecl operator new(std::size_t size, const std::nothrow_t&) noexcept
{
    if (size == 0)
    {
        size++;
    }

    return malloc(size);
}

void* __cdecl operator new[](std::size_t size, const std::nothrow_t&) noexcept
{
    if (size == 0)
    {
        size++;
    }

    return malloc(size);
}

void __cdecl operator delete(void* ptr, const std::nothrow_t&) noexcept
{
    free(ptr);
}

void __cdecl operator delete[](void* ptr, const std::nothrow_t&) noexcept
{
    free(ptr);
}

void __cdecl operator delete(void* ptr, std::size_t) noexcept
{
    free(ptr);
}

void __cdecl operator delete[](void* ptr, std::size_t) noexcept
{
    free(ptr);
}

#endif
