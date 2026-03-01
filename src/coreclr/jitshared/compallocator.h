// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COMPALLOCATOR_H_
#define _COMPALLOCATOR_H_

#include <stddef.h>
#include <stdint.h>
#include "minipal/utils.h"

#include "jitshared.h"
#include "arenaallocator.h"
#include "../inc/iallocator.h"

// CompAllocatorT is a wrapper around ArenaAllocatorT that tracks allocations
// by memory kind for profiling purposes.
//
// Template parameters:
//   TMemKindTraits - A traits struct that must provide:
//     - MemKind: The enum type for memory kinds
//     - Count: A static constexpr int giving the number of enum values
//     - Names: A static const char* const[] array of names for each kind
//
// When MEASURE_MEM_ALLOC is enabled, CompAllocatorT tracks allocation statistics
// per memory kind. When disabled, it's a thin wrapper with no overhead.

template <typename TMemKindTraits>
class CompAllocatorT
{
    using MemKind = typename TMemKindTraits::MemKind;
    using Arena = ArenaAllocatorT<TMemKindTraits>;

#if MEASURE_MEM_ALLOC
    typename Arena::MemStatsAllocator* m_statsAllocator;
#else
    Arena* m_arena;
#endif

public:
#if MEASURE_MEM_ALLOC
    CompAllocatorT(Arena* arena, MemKind kind)
        : m_statsAllocator(arena->getMemStatsAllocator(kind))
    {
    }
#else
    CompAllocatorT(Arena* arena, MemKind kind)
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
            TMemKindTraits::outOfMemory();
        }

        size_t sz = count * sizeof(T);

#if MEASURE_MEM_ALLOC
        void* p = m_statsAllocator->allocateMemory(sz);
#else
        void* p = m_arena->allocateMemory(sz);
#endif

        // Ensure that the allocator returned sizeof(size_t) aligned memory.
        assert((reinterpret_cast<size_t>(p) & (sizeof(size_t) - 1)) == 0);

        return static_cast<T*>(p);
    }

    // Allocate a zeroed block of memory suitable to store `count` objects of type `T`.
    // Zero-length allocations are not allowed.
    template <typename T>
    T* allocateZeroed(size_t count)
    {
        T* p = allocate<T>(count);
        memset(p, 0, count * sizeof(T));
        return p;
    }

    // Deallocate a block of memory previously allocated by `allocate`.
    // The arena allocator does not release memory so this doesn't do anything.
    void deallocate(void* p)
    {
        (void)p; // Arena allocators don't support individual deallocation
    }
};

// Global operator new overloads that work with CompAllocatorT

template <typename TMemKindTraits>
inline void* operator new(size_t n, CompAllocatorT<TMemKindTraits> alloc)
{
    return alloc.template allocate<int8_t>(n);
}

template <typename TMemKindTraits>
inline void* operator new[](size_t n, CompAllocatorT<TMemKindTraits> alloc)
{
    return alloc.template allocate<int8_t>(n);
}

// CompIAllocatorT is a CompAllocatorT wrapper that implements the IAllocator interface.
// It allows zero-length memory allocations (which the arena allocator does not support).
//
// This is primarily used for GCInfoEncoder integration.

template <typename TMemKindTraits>
class CompIAllocatorT : public IAllocator
{
    CompAllocatorT<TMemKindTraits> m_alloc;
    char m_zeroLenAllocTarg;

public:
    CompIAllocatorT(CompAllocatorT<TMemKindTraits> alloc)
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
            return m_alloc.template allocate<int8_t>(sz);
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
                TMemKindTraits::outOfMemory();
            }

            return m_alloc.template allocate<int8_t>(elems * elemSize);
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
