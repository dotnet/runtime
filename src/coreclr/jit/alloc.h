// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ALLOC_H_
#define _ALLOC_H_

#if !defined(_HOST_H_)
#include "host.h"
#endif // defined(_HOST_H_)

// CompMemKind values are used to tag memory allocations performed via
// the compiler's allocator so that the memory usage of various compiler
// components can be tracked separately (when MEASURE_MEM_ALLOC is defined).

enum CompMemKind
{
#define CompMemKindMacro(kind) CMK_##kind,
#include "compmemkind.h"
    CMK_Count
};

class ArenaAllocator
{
private:
    ArenaAllocator(const ArenaAllocator& other) = delete;
    ArenaAllocator& operator=(const ArenaAllocator& other) = delete;
    ArenaAllocator& operator=(ArenaAllocator&& other) = delete;

    struct PageDescriptor
    {
        PageDescriptor* m_next;

        size_t m_pageBytes; // # of bytes allocated
        size_t m_usedBytes; // # of bytes actually used. (This is only valid when we've allocated a new page.)
                            // See ArenaAllocator::allocateNewPage.

        BYTE m_contents[];
    };

    enum
    {
        DEFAULT_PAGE_SIZE = 0x10000,
    };

    PageDescriptor* m_firstPage;
    PageDescriptor* m_lastPage;

    // These two pointers (when non-null) will always point into 'm_lastPage'.
    BYTE* m_nextFreeByte;
    BYTE* m_lastFreeByte;

    void* allocateNewPage(size_t size);

    static void* allocateHostMemory(size_t size, size_t* pActualSize);
    static void freeHostMemory(void* block, size_t size);

#if MEASURE_MEM_ALLOC
    struct MemStats
    {
        unsigned allocCnt;                 // # of allocs
        UINT64   allocSz;                  // total size of those alloc.
        UINT64   allocSzMax;               // Maximum single allocation.
        UINT64   allocSzByKind[CMK_Count]; // Classified by "kind".
        UINT64   nraTotalSizeAlloc;
        UINT64   nraTotalSizeUsed;

        static const char* s_CompMemKindNames[]; // Names of the kinds.

        void AddAlloc(size_t sz, CompMemKind cmk)
        {
            allocCnt += 1;
            allocSz += sz;
            if (sz > allocSzMax)
            {
                allocSzMax = sz;
            }
            allocSzByKind[cmk] += sz;
        }

        void Print(FILE* f);       // Print these stats to file.
        void PrintByKind(FILE* f); // Do just the by-kind histogram part.
    };

    struct AggregateMemStats : public MemStats
    {
        unsigned nMethods;

        void Add(const MemStats& ms)
        {
            nMethods++;
            allocCnt += ms.allocCnt;
            allocSz += ms.allocSz;
            allocSzMax = max(allocSzMax, ms.allocSzMax);
            for (int i = 0; i < CMK_Count; i++)
            {
                allocSzByKind[i] += ms.allocSzByKind[i];
            }
            nraTotalSizeAlloc += ms.nraTotalSizeAlloc;
            nraTotalSizeUsed += ms.nraTotalSizeUsed;
        }

        void Print(FILE* f); // Print these stats to file.
    };

public:
    struct MemStatsAllocator
    {
        ArenaAllocator* m_arena;
        CompMemKind     m_kind;

        void* allocateMemory(size_t sz)
        {
            m_arena->m_stats.AddAlloc(sz, m_kind);
            return m_arena->allocateMemory(sz);
        }
    };

private:
    static CritSecObject     s_statsLock; // This lock protects the data structures below.
    static MemStats          s_maxStats;  // Stats for the allocator with the largest amount allocated.
    static AggregateMemStats s_aggStats;  // Aggregates statistics for all allocators.

    MemStats          m_stats;
    MemStatsAllocator m_statsAllocators[CMK_Count];

public:
    MemStatsAllocator* getMemStatsAllocator(CompMemKind kind);
    void finishMemStats();
    void dumpMemStats(FILE* file);

    static void dumpMaxMemStats(FILE* file);
    static void dumpAggregateMemStats(FILE* file);
#endif // MEASURE_MEM_ALLOC

public:
    ArenaAllocator();

    // NOTE: it would be nice to have a destructor on this type to ensure that any value that
    //       goes out of scope is either uninitialized or has been torn down via a call to
    //       destroy(), but this interacts badly in methods that use SEH. #3058 tracks
    //       revisiting EH in the JIT; such a destructor could be added if SEH is removed
    //       as part of that work.

    void destroy();

    inline void* allocateMemory(size_t sz);

    size_t getTotalBytesAllocated();
    size_t getTotalBytesUsed();

    static bool   bypassHostAllocator();
    static size_t getDefaultPageSize();
};

//------------------------------------------------------------------------
// ArenaAllocator::allocateMemory:
//    Allocates memory using an `ArenaAllocator`.
//
// Arguments:
//    size - The number of bytes to allocate.
//
// Return Value:
//    A pointer to the allocated memory.
//
// Note:
//    The DEBUG version of the method has some abilities that the release
//    version does not: it may inject faults into the allocator and
//    seeds all allocations with a specified pattern to help catch
//    use-before-init problems.
//
inline void* ArenaAllocator::allocateMemory(size_t size)
{
    assert(size != 0);

    // Ensure that we always allocate in pointer sized increments.
    size = roundUp(size, sizeof(size_t));

#if defined(DEBUG)
    if (JitConfig.ShouldInjectFault() != 0)
    {
        // Force the underlying memory allocator (either the OS or the CLR hoster)
        // to allocate the memory. Any fault injection will kick in.
        size_t size;
        void*  p = allocateHostMemory(1, &size);
        freeHostMemory(p, size);
    }
#endif

    void* block = m_nextFreeByte;
    m_nextFreeByte += size;

    if (m_nextFreeByte > m_lastFreeByte)
    {
        block = allocateNewPage(size);
    }

#if defined(DEBUG)
    memset(block, UninitializedWord<char>(nullptr), size);
#endif

    return block;
}

// Allows general purpose code (e.g. collection classes) to allocate
// memory of a pre-determined kind via an arena allocator.

class CompAllocator
{
#if MEASURE_MEM_ALLOC
    ArenaAllocator::MemStatsAllocator* m_arena;
#else
    ArenaAllocator* m_arena;
#endif

public:
    CompAllocator(ArenaAllocator* arena, CompMemKind cmk)
#if MEASURE_MEM_ALLOC
        : m_arena(arena->getMemStatsAllocator(cmk))
#else
        : m_arena(arena)
#endif
    {
    }

    // Allocate a block of memory suitable to store `count` objects of type `T`.
    // Zero-length allocations are not allowed.
    template <typename T>
    T* allocate(size_t count)
    {
        // Ensure that count * sizeof(T) does not overflow.
        if (count > (SIZE_MAX / sizeof(T)))
        {
            NOMEM();
        }

        void* p = m_arena->allocateMemory(count * sizeof(T));

        // Ensure that the allocator returned sizeof(size_t) aligned memory.
        assert((size_t(p) & (sizeof(size_t) - 1)) == 0);

        return static_cast<T*>(p);
    }

    // Deallocate a block of memory previously allocated by `allocate`.
    // The arena allocator does not release memory so this doesn't do anything.
    void deallocate(void* p)
    {
    }
};

// Global operator new overloads that work with CompAllocator

inline void* __cdecl operator new(size_t n, CompAllocator alloc)
{
    return alloc.allocate<char>(n);
}

inline void* __cdecl operator new[](size_t n, CompAllocator alloc)
{
    return alloc.allocate<char>(n);
}

// A CompAllocator wrapper that implements IAllocator and allows zero-length
// memory allocations (the arena allocator does not support zero-length
// allocation).

class CompIAllocator : public IAllocator
{
    CompAllocator m_alloc;
    char          m_zeroLenAllocTarg;

public:
    CompIAllocator(CompAllocator alloc) : m_alloc(alloc)
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
            return m_alloc.allocate<char>(sz);
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
                NOMEM();
            }

            return m_alloc.allocate<char>(elems * elemSize);
        }
    }

    // Frees the block of memory pointed to by p.
    virtual void Free(void* p) override
    {
        m_alloc.deallocate(p);
    }
};

#endif // _ALLOC_H_
