// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Allocator and holders for runtime method and loader-heap storage.

#pragma once

#include "utilcode.h"
#include "ex.h"

#include <minipal.h>

#ifndef DACCESS_COMPILE

//#define LOG_EXECUTABLE_ALLOCATOR_STATISTICS

// Most targets allocate executable memory or map executable templates. Targets with portable
// entrypoints use read-write storage for loader and interpreter data.
class ExecutableAllocator
{
public:

    enum CacheableMapping
    {
        AddToCache,
        DoNotAddToCache,
    };

private:

    typedef void (*FatalErrorHandler)(UINT errorCode, LPCWSTR pszMessage);

    // Instance of the allocator
    static ExecutableAllocator* g_instance;

    // Callback to the runtime to report fatal errors
    static FatalErrorHandler g_fatalErrorHandler;

#ifndef FEATURE_PORTABLE_ENTRYPOINTS
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
#if USE_LAZY_PREFERRED_RANGE
    static BYTE* g_lazyPreferredRangeStart;
    // Next address to try to allocate for code in the lazy preferred region.
    static BYTE* g_lazyPreferredRangeHint;
#endif // USE_LAZY_PREFERRED_RANGE

    // For Unix, this region represents the area that is eagerly reserved on
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

    // Release memory allocated via DoubleMapping for either templates or normal double mapped data
    void ReleaseWorker(void* pRX, size_t size, bool releaseTemplate) noexcept;

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
#endif // !FEATURE_PORTABLE_ENTRYPOINTS

public:

#if defined(LOG_EXECUTABLE_ALLOCATOR_STATISTICS) && !defined(FEATURE_PORTABLE_ENTRYPOINTS)
    static void LogUsage(const char* source, int line, const char* function);
    static void DumpHolderUsage();
#endif

    // Return the ExecuteAllocator singleton instance
    static ExecutableAllocator* Instance() noexcept;

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
    // from minipal.
    static void InitPreferredRange();

    // Returns TRUE if p is located in near clr.dll that allows us
    // to use rel32 IP-relative addressing modes.
    static bool IsPreferredExecutableRange(void* p);

    // Reserve the specified amount of virtual address space for executable mapping.
    void* Reserve(size_t size);

    // Reserve the specified amount of virtual address space for executable mapping.
    // The reserved range must be within the loAddress and hiAddress. If it is not
    // possible to reserve memory in such range, the method returns NULL.
    // Targets with portable entrypoints ignore the address bounds and allocate read-write
    // storage anywhere.
    void* ReserveWithinRange(size_t size, const void* loAddress, const void* hiAddress);

    // Commit the specified range of memory. The memory can be committed as executable (RX)
    // or non-executable (RW) based on the passed in isExecutable flag. The non-executable
    // allocations are used to allocate data structures that need to be close to the
    // executable code due to memory addressing performance related reasons.
    // On targets with portable entrypoints, storage is already accessible, so Commit returns
    // pStart unchanged.
    void* Commit(void* pStart, size_t size, bool isExecutable);

    // Release the executable memory block starting at the passed in address that was allocated
    // by one of the ReserveXXX methods. The size must be the size originally requested from
    // that reserve call.
    void Release(void* pRX, size_t size) noexcept;

    // Map the specified block of executable memory as RW. Targets with portable entrypoints
    // return the original pointer.
    void* MapRW(void* pRX, size_t size, CacheableMapping cacheMapping);

    // Unmap the RW mapping at the specified address. This is a no-op on targets with portable
    // entrypoints.
    void UnmapRW(void* pRW) noexcept;

#ifndef FEATURE_PORTABLE_ENTRYPOINTS
    // Allocate thunks from a template. pTemplate is the return value from CreateTemplate
    void* AllocateThunksFromTemplate(void *pTemplate, size_t templateSize, void (*dataPageGenerator)(uint8_t* pageBase, size_t size));

    // Free a set of thunks allocated from templates. pThunks must have been returned from AllocateThunksFromTemplate
    void FreeThunksFromTemplate(void *pThunks, size_t templateSize);

    // Create a template
    // If templateInImage is not null, it will attempt to use it as the template, otherwise it will create an temporary in memory file to serve as the template
    // Some OS/Architectures may/may not be able to work with this, so this api is permitted to return NULL, and callers should have an alternate approach using
    // the codePageGenerator directly.
    void* CreateTemplate(void* templateInImage, size_t templateSize, void (*codePageGenerator)(uint8_t* pageBase, uint8_t* pageBaseRX, size_t size));
#endif // !FEATURE_PORTABLE_ENTRYPOINTS
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

    void Unmap() noexcept
    {
#if defined(HOST_APPLE) && defined(HOST_ARM64) && !defined(DACCESS_COMPILE)
        if (m_addressRX != NULL)
        {
            VMToOSInterface::JitWriteProtect(false);
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
#if defined(HOST_APPLE) && defined(HOST_ARM64)
        m_addressRW = addressRX;
        VMToOSInterface::JitWriteProtect(true);
#else
        m_addressRW = (T *)ExecutableAllocator::Instance()->MapRW((void*)addressRX, size, cacheMapping);
#endif
    }

    ~ExecutableWriterHolder() noexcept
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

#if defined(LOG_EXECUTABLE_ALLOCATOR_STATISTICS) && !defined(FEATURE_PORTABLE_ENTRYPOINTS)
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
