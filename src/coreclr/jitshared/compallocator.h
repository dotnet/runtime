// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COMPALLOCATOR_H_
#define _COMPALLOCATOR_H_

#include <cstddef>
#include <cstdint>
#include <cassert>

#include "jitshared.h"
#include "arenaallocator.h"
#include "memstats.h"

// Forward declaration for IAllocator interface (from coreclr/inc/iallocator.h)
#ifndef _IALLOCATOR_DEFINED_
class IAllocator
{
public:
    virtual void* Alloc(size_t sz) = 0;
    virtual void* ArrayAlloc(size_t elems, size_t elemSize) = 0;
    virtual void Free(void* p) = 0;
};
#endif

// CompAllocator is a wrapper around ArenaAllocator that tracks allocations
// by memory kind for profiling purposes.
//
// Template parameters:
//   TMemKindTraits - A traits struct that must provide:
//     - MemKind: The enum type for memory kinds
//     - Count: A static constexpr int giving the number of enum values
//     - Names: A static const char* const[] array of names for each kind
//
// When MEASURE_MEM_ALLOC is enabled, CompAllocator tracks allocation statistics
// per memory kind. When disabled, it's a thin wrapper with no overhead.

template <typename TMemKindTraits>
class CompAllocator
{
    using MemKind = typename TMemKindTraits::MemKind;

    ArenaAllocator* m_arena;
#if MEASURE_MEM_ALLOC
    MemKind m_kind;
    MemStats<TMemKindTraits>* m_stats;
#endif

public:
#if MEASURE_MEM_ALLOC
    CompAllocator(ArenaAllocator* arena, MemKind kind, MemStats<TMemKindTraits>* stats = nullptr)
        : m_arena(arena)
        , m_kind(kind)
        , m_stats(stats)
    {
    }
#else
    CompAllocator(ArenaAllocator* arena, MemKind kind)
        : m_arena(arena)
    {
        (void)kind; // Suppress unused parameter warning
    }
#endif

    // Allocate a block of memory suitable to store `count` objects of type `T`.
    // Zero-length allocations are not allowed.
    template <typename T>
    T* allocate(size_t count)
    {
        // Ensure that count * sizeof(T) does not overflow.
        if (count > (SIZE_MAX / sizeof(T)))
        {
            // This should call NOMEM() through the arena's config
            return nullptr;
        }

        size_t sz = count * sizeof(T);

#if MEASURE_MEM_ALLOC
        if (m_stats != nullptr)
        {
            m_stats->AddAlloc(sz, m_kind);
        }
#endif

        void* p = m_arena->allocateMemory(sz);

        // Ensure that the allocator returned sizeof(size_t) aligned memory.
        assert((reinterpret_cast<size_t>(p) & (sizeof(size_t) - 1)) == 0);

        return static_cast<T*>(p);
    }

    // Deallocate a block of memory previously allocated by `allocate`.
    // The arena allocator does not release memory so this doesn't do anything.
    void deallocate(void* p)
    {
        (void)p; // Arena allocators don't support individual deallocation
    }
};

// Global operator new overloads that work with CompAllocator

template <typename TMemKindTraits>
inline void* __cdecl operator new(size_t n, CompAllocator<TMemKindTraits> alloc)
{
    return alloc.template allocate<char>(n);
}

template <typename TMemKindTraits>
inline void* __cdecl operator new[](size_t n, CompAllocator<TMemKindTraits> alloc)
{
    return alloc.template allocate<char>(n);
}

// CompIAllocator is a CompAllocator wrapper that implements the IAllocator interface.
// It allows zero-length memory allocations (which the arena allocator does not support).
//
// This is primarily used for GCInfoEncoder integration.

template <typename TMemKindTraits>
class CompIAllocator : public IAllocator
{
    CompAllocator<TMemKindTraits> m_alloc;
    char m_zeroLenAllocTarg;

public:
    CompIAllocator(CompAllocator<TMemKindTraits> alloc)
        : m_alloc(alloc)
    {
    }

    // Allocates a block of memory at least `sz` in size.
    virtual void* Alloc(size_t sz) override
    {
        if (sz == 0)
        {
            return &m_zeroLenAllocTarg;
        }
        else
        {
            return m_alloc.template allocate<char>(sz);
        }
    }

    // Allocates a block of memory at least `elems * elemSize` in size.
    virtual void* ArrayAlloc(size_t elems, size_t elemSize) override
    {
        if ((elems == 0) || (elemSize == 0))
        {
            return &m_zeroLenAllocTarg;
        }
        else
        {
            // Ensure that elems * elemSize does not overflow.
            if (elems > (SIZE_MAX / elemSize))
            {
                return nullptr; // Should call NOMEM()
            }

            return m_alloc.template allocate<char>(elems * elemSize);
        }
    }

    // Frees the block of memory pointed to by p.
    virtual void Free(void* p) override
    {
        if (p == &m_zeroLenAllocTarg)
        {
            return;
        }

        m_alloc.deallocate(p);
    }
};

#endif // _COMPALLOCATOR_H_
