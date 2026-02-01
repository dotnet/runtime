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

#if MEASURE_MEM_ALLOC
CritSecObject        JitMemStatsInfo::s_statsLock;
JitAggregateMemStats JitMemStatsInfo::s_aggStats;
JitMemStats          JitMemStatsInfo::s_maxStats;

void JitMemStatsInfo::finishMemStats(ArenaAllocator* arena)
{
    arena->finishMemStats();

    CritSecHolder statsLock(s_statsLock);
    JitMemStats& stats = arena->getStats();
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

void* __cdecl operator new(std::size_t size)
{
    assert(!"Global new called; use HostAllocator if long-lived allocation was intended");

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
    assert(!"Global new called; use HostAllocator if long-lived allocation was intended");

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

#endif
