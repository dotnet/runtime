// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Allocator and holders for double mapped executable memory
//

#pragma once

#include "utilcode.h"
#include "ex.h"

#include <minipal.h>

#ifndef DACCESS_COMPILE

//#define LOG_EXECUTABLE_ALLOCATOR_STATISTICS

// This class is responsible for allocation of all the executable memory in the runtime.
class ExecutableAllocator
{
public:

    enum CacheableMapping
    {
        AddToCache,
        DoNotAddToCache,
    };

private:

    // RX address range block descriptor
    struct BlockRX
    {
        // Next block in a linked list
        BlockRX* next;
        // Base address of the block
        void* baseRX;
        // Size of the block
        size_t size;
        // Offset of the block in the shared memory
        size_t offset;
    };

    // RW address range block descriptor
    struct BlockRW
    {
        // Next block in a linked list
        BlockRW* next;
        // Base address of the RW mapping of the block
        void* baseRW;
        // Base address of the RX mapping of the block
        void* baseRX;
        // Size of the block
        size_t size;
        // Usage reference count of the RW block. RW blocks can be reused
        // when multiple mappings overlap in the VA space at the same time
        // (even from multiple threads)
        size_t refCount;
    };

    typedef void (*FatalErrorHandler)(UINT errorCode, LPCWSTR pszMessage);
#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    static int64_t g_mapTimeSum;
    static int64_t g_mapTimeWithLockSum;
    static int64_t g_unmapTimeSum;
    static int64_t g_unmapTimeWithLockSum;
    static int64_t g_mapFindRXTimeSum;
    static int64_t g_mapCreateTimeSum;

    static int64_t g_releaseCount;
    static int64_t g_reserveCount;

    static int64_t g_MapRW_Calls;
    static int64_t g_MapRW_CallsWithCacheMiss;
    static int64_t g_MapRW_LinkedListWalkDepth;
    static int64_t g_LinkedListTotalDepth;
#endif
    // Instance of the allocator
    static ExecutableAllocator* g_instance;

    // Callback to the runtime to report fatal errors
    static FatalErrorHandler g_fatalErrorHandler;

#if USE_LAZY_PREFERRED_RANGE
    static BYTE* g_lazyPreferredRangeStart;
    // Next address to try to allocate for code in the lazy preferred region.
    static BYTE* g_lazyPreferredRangeHint;
#endif // USE_LAZY_PREFERRED_RANGE

    // For PAL, this region represents the area that is eagerly reserved on
    // startup where executable memory and static fields are preferrably kept.
    // For Windows, this is the region that we lazily reserve from.
    static BYTE* g_preferredRangeMin;
    static BYTE* g_preferredRangeMax;

    // Caches the DOTNET_EnableWXORX setting
    static bool g_isWXorXEnabled;

    // Head of the linked list of all RX blocks that were allocated by this allocator
    BlockRX* m_pFirstBlockRX = NULL;

    // Head of the linked list of free RX blocks that were allocated by this allocator and then backed out
    BlockRX* m_pFirstFreeBlockRX = NULL;

    // Head of the linked list of currently mapped RW blocks
    BlockRW* m_pFirstBlockRW = NULL;

    // Handle of the double mapped memory mapper
    void *m_doubleMemoryMapperHandle = NULL;

    // Maximum size of executable memory this allocator can allocate
    size_t m_maxExecutableCodeSize;

    // First free offset in the underlying shared memory. It is not used
    // for platforms that don't use shared memory.
    size_t m_freeOffset = 0;

// Uncomment these to gather information to better choose caching parameters
//#define VARIABLE_SIZED_CACHEDMAPPING_SIZE

    // Last RW mappings cached so that it can be reused for the next mapping
    // request if it goes into the same range.
    // This is handled as a 3 element cache with an LRU replacement policy
#ifdef VARIABLE_SIZED_CACHEDMAPPING_SIZE
    // If variable sized mappings enabled, make the cache physically big enough to cover all interesting sizes
    static int g_cachedMappingSize;
    BlockRW* m_cachedMapping[16] = { 0 };
#else
    BlockRW* m_cachedMapping[3] = { 0 };
#endif

    // Synchronization of the public allocator methods
    CRITSEC_COOKIE m_CriticalSection;

    // Update currently cached mapping. If the passed in block is the same as the one
    // in the cache, it keeps it cached. Otherwise it destroys the currently cached one
    // and replaces it by the passed in one.
    void UpdateCachedMapping(BlockRW *pBlock);

    // Remove the cached mapping (1 based indexing)
    void RemoveCachedMapping(size_t indexToRemove);

    // Find an overlapped cached mapping with pBlock, or return 0
    size_t FindOverlappingCachedMapping(BlockRX* pBlock);

    // Find existing RW block that maps the whole specified range of RX memory.
    // Return NULL if no such block exists.
    void* FindRWBlock(void* baseRX, size_t size, CacheableMapping cacheMapping);

    // Add RW block to the list of existing RW blocks
    bool AddRWBlock(void* baseRW, void* baseRX, size_t size, CacheableMapping cacheMapping);

    // Remove RW block from the list of existing RW blocks and return the base
    // address and size the underlying memory was mapped at.
    // Return false if no existing RW block contains the passed in address.
    bool RemoveRWBlock(void* pRW, void** pUnmapAddress, size_t* pUnmapSize);

    // Find a free block with the closest size >= the requested size.
    // Returns NULL if no such block exists.
    BlockRX* FindBestFreeBlock(size_t size);

    // Return memory mapping granularity.
    static size_t Granularity();

    // Allocate a block of executable memory of the specified size.
    // It doesn't acquire the actual virtual memory, just the
    // range of the underlying shared memory.
    BlockRX* AllocateBlock(size_t size, bool* pIsFreeBlock);

    // Backout the block allocated by AllocateBlock in case of an
    // error.
    void BackoutBlock(BlockRX* pBlock, bool isFreeBlock);

    // Allocate range of offsets in the underlying shared memory
    bool AllocateOffset(size_t* pOffset, size_t size);

    // Add RX block to the linked list of existing blocks
    void AddRXBlock(BlockRX *pBlock);

    // Return true if double mapping is enabled.
    static bool IsDoubleMappingEnabled();

    // Initialize the allocator instance
    bool Initialize();

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    static CRITSEC_COOKIE s_LoggerCriticalSection;

    struct LogEntry
    {
        const char* source;
        const char* function;
        int line;
        int count;
    };

    static LogEntry s_usageLog[256];
    static int s_logMaxIndex;
#endif

public:

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    static void LogUsage(const char* source, int line, const char* function);
    static void DumpHolderUsage();
#endif

    // Return the ExecuteAllocator singleton instance
    static ExecutableAllocator* Instance();

    // Initialize the static members of the Executable allocator and allocate
    // and initialize the instance of it.
    static HRESULT StaticInitialize(FatalErrorHandler fatalErrorHandler);

    // Destroy the allocator
    ~ExecutableAllocator();

    // Return true if W^X is enabled
    static bool IsWXORXEnabled();

    // Use this function to initialize g_lazyPreferredRangeHint during startup.
    // base is runtime .dll base address, size is runtime .dll virtual size.
    static void InitLazyPreferredRange(size_t base, size_t size, int randomPageOffset);

    // Use this function to reset g_lazyPreferredRangeHint after unloading code.
    static void ResetLazyPreferredRangeHint();

    // Use this function to initialize the preferred range of executable memory
    // from PAL.
    static void InitPreferredRange();

    // Returns TRUE if p is located in near clr.dll that allows us
    // to use rel32 IP-relative addressing modes.
    static bool IsPreferredExecutableRange(void* p);

    // Reserve the specified amount of virtual address space for executable mapping.
    void* Reserve(size_t size);

    // Reserve the specified amount of virtual address space for executable mapping.
    // The reserved range must be within the loAddress and hiAddress. If it is not
    // possible to reserve memory in such range, the method returns NULL.
    void* ReserveWithinRange(size_t size, const void* loAddress, const void* hiAddress);

    // Reserve the specified amount of virtual address space for executable mapping
    // exactly at the given address.
    void* ReserveAt(void* baseAddressRX, size_t size);

    // Commit the specified range of memory. The memory can be committed as executable (RX)
    // or non-executable (RW) based on the passed in isExecutable flag. The non-executable
    // allocations are used to allocate data structures that need to be close to the
    // executable code due to memory addressing performance related reasons.
    void* Commit(void* pStart, size_t size, bool isExecutable);

    // Release the executable memory block starting at the passed in address that was allocated
    // by one of the ReserveXXX methods.
    void Release(void* pRX);

    // Map the specified block of executable memory as RW
    void* MapRW(void* pRX, size_t size, CacheableMapping cacheMapping);

    // Unmap the RW mapping at the specified address
    void UnmapRW(void* pRW);
};

#define ExecutableWriterHolder ExecutableWriterHolderNoLog

// Holder class to map read-execute memory as read-write so that it can be modified without using read-write-execute mapping.
// At the moment the implementation is dummy, returning the same addresses for both cases and expecting them to be read-write-execute.
// The class uses the move semantics to ensure proper unmapping in case of re-assigning of the holder value.
template<typename T>
class ExecutableWriterHolder
{
    T *m_addressRX;
    T *m_addressRW;

    void Move(ExecutableWriterHolder& other)
    {
        m_addressRX = other.m_addressRX;
        m_addressRW = other.m_addressRW;
        other.m_addressRX = NULL;
        other.m_addressRW = NULL;
    }

    void Unmap()
    {
#if defined(HOST_OSX) && defined(HOST_ARM64) && !defined(DACCESS_COMPILE)
        if (m_addressRX != NULL)
        {
            PAL_JitWriteProtect(false);
        }
#else
        if (m_addressRX != m_addressRW)
        {
            ExecutableAllocator::Instance()->UnmapRW((void*)m_addressRW);
        }
#endif
    }

public:
    ExecutableWriterHolder(const ExecutableWriterHolder& other) = delete;
    ExecutableWriterHolder& operator=(const ExecutableWriterHolder& other) = delete;

    ExecutableWriterHolder(ExecutableWriterHolder&& other)
    {
        Move(other);
    }

    ExecutableWriterHolder& operator=(ExecutableWriterHolder&& other)
    {
        Unmap();
        Move(other);
        return *this;
    }

    ExecutableWriterHolder() : m_addressRX(nullptr), m_addressRW(nullptr)
    {
    }

    ExecutableWriterHolder(T* addressRX, size_t size, ExecutableAllocator::CacheableMapping cacheMapping = ExecutableAllocator::AddToCache)
    {
        m_addressRX = addressRX;
#if defined(HOST_OSX) && defined(HOST_ARM64)
        m_addressRW = addressRX;
        PAL_JitWriteProtect(true);
#else
        m_addressRW = (T *)ExecutableAllocator::Instance()->MapRW((void*)addressRX, size, cacheMapping);
#endif
    }

    ~ExecutableWriterHolder()
    {
        Unmap();
    }

    // Get the writeable address
    inline T *GetRW() const
    {
        return m_addressRW;
    }

    void AssignExecutableWriterHolder(T* addressRX, size_t size)
    {
        *this = ExecutableWriterHolder(addressRX, size);
    }
};

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
#undef ExecutableWriterHolder
#ifdef HOST_UNIX
#define ExecutableWriterHolder ExecutableAllocator::LogUsage(__FILE__, __LINE__, __PRETTY_FUNCTION__); ExecutableWriterHolderNoLog
#define AssignExecutableWriterHolder(addressRX, size) AssignExecutableWriterHolder(addressRX, size); ExecutableAllocator::LogUsage(__FILE__, __LINE__, __PRETTY_FUNCTION__);
#else
#define ExecutableWriterHolder ExecutableAllocator::LogUsage(__FILE__, __LINE__, __FUNCTION__); ExecutableWriterHolderNoLog
#define AssignExecutableWriterHolder(addressRX, size) AssignExecutableWriterHolder(addressRX, size); ExecutableAllocator::LogUsage(__FILE__, __LINE__, __FUNCTION__);
#endif
#else
#define ExecutableWriterHolder ExecutableWriterHolderNoLog
#endif

#endif // !DACCESS_COMPILE
