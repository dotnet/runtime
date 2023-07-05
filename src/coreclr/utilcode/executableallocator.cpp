// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pedecoder.h"
#include "executableallocator.h"

#if USE_LAZY_PREFERRED_RANGE
// Preferred region to allocate the code in.
BYTE * ExecutableAllocator::g_lazyPreferredRangeStart;
// Next address to try to allocate for code in the preferred region.
BYTE * ExecutableAllocator::g_lazyPreferredRangeHint;
#endif // USE_LAZY_PREFERRED_RANGE

BYTE * ExecutableAllocator::g_preferredRangeMin;
BYTE * ExecutableAllocator::g_preferredRangeMax;

bool ExecutableAllocator::g_isWXorXEnabled = false;

ExecutableAllocator::FatalErrorHandler ExecutableAllocator::g_fatalErrorHandler = NULL;
ExecutableAllocator* ExecutableAllocator::g_instance = NULL;

#ifndef VARIABLE_SIZED_CACHEDMAPPING_SIZE
#define EXECUTABLE_ALLOCATOR_CACHE_SIZE ARRAY_SIZE(m_cachedMapping)
#else
int ExecutableAllocator::g_cachedMappingSize = 0;

#define EXECUTABLE_ALLOCATOR_CACHE_SIZE ExecutableAllocator::g_cachedMappingSize
#endif

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
int64_t ExecutableAllocator::g_mapTimeSum = 0;
int64_t ExecutableAllocator::g_mapTimeWithLockSum = 0;
int64_t ExecutableAllocator::g_unmapTimeSum = 0;
int64_t ExecutableAllocator::g_unmapTimeWithLockSum = 0;
int64_t ExecutableAllocator::g_mapFindRXTimeSum = 0;
int64_t ExecutableAllocator::g_mapCreateTimeSum = 0;
int64_t ExecutableAllocator::g_releaseCount = 0;
int64_t ExecutableAllocator::g_reserveCount = 0;
int64_t ExecutableAllocator::g_MapRW_Calls = 0;
int64_t ExecutableAllocator::g_MapRW_CallsWithCacheMiss = 0;
int64_t ExecutableAllocator::g_MapRW_LinkedListWalkDepth = 0;
int64_t ExecutableAllocator::g_LinkedListTotalDepth = 0;

ExecutableAllocator::LogEntry ExecutableAllocator::s_usageLog[256];
int ExecutableAllocator::s_logMaxIndex = 0;
CRITSEC_COOKIE ExecutableAllocator::s_LoggerCriticalSection;

class StopWatch
{
    LARGE_INTEGER m_start;
    int64_t* m_accumulator;

public:
    StopWatch(int64_t* accumulator) : m_accumulator(accumulator)
    {
        QueryPerformanceCounter(&m_start);
    }

    ~StopWatch()
    {
        LARGE_INTEGER end;
        QueryPerformanceCounter(&end);

        InterlockedExchangeAdd64(m_accumulator, end.QuadPart - m_start.QuadPart);
    }
};

void ExecutableAllocator::LogUsage(const char* source, int line, const char* function)
{
    CRITSEC_Holder csh(s_LoggerCriticalSection);

    for (int i = 0; i < s_logMaxIndex; i++)
    {
        if (s_usageLog[i].source == source && s_usageLog[i].line == line)
        {
            s_usageLog[i].count++;
            return;
        }
    }

    int i = s_logMaxIndex;
    s_logMaxIndex++;
    s_usageLog[i].source = source;
    s_usageLog[i].function = function;
    s_usageLog[i].line = line;
    s_usageLog[i].count = 1;
}

void ExecutableAllocator::DumpHolderUsage()
{
    CRITSEC_Holder csh(s_LoggerCriticalSection);

    LARGE_INTEGER freq;
    QueryPerformanceFrequency(&freq);

    fprintf(stderr, "Map time with lock sum: %lldms\n", g_mapTimeWithLockSum / (freq.QuadPart / 1000));
    fprintf(stderr, "Map time sum: %lldms\n", g_mapTimeSum / (freq.QuadPart / 1000));
    fprintf(stderr, "Map find RX time sum: %lldms\n", g_mapFindRXTimeSum / (freq.QuadPart / 1000));
    fprintf(stderr, "Map create time sum: %lldms\n", g_mapCreateTimeSum / (freq.QuadPart / 1000));
    fprintf(stderr, "Unmap time with lock sum: %lldms\n", g_unmapTimeWithLockSum / (freq.QuadPart / 1000));
    fprintf(stderr, "Unmap time sum: %lldms\n", g_unmapTimeSum / (freq.QuadPart / 1000));

    fprintf(stderr, "Reserve count: %lld\n", g_reserveCount);
    fprintf(stderr, "Release count: %lld\n", g_releaseCount);

    fprintf(stderr, "g_MapRW_Calls: %lld\n", g_MapRW_Calls);
    fprintf(stderr, "g_MapRW_CallsWithCacheMiss: %lld\n", g_MapRW_CallsWithCacheMiss);
    fprintf(stderr, "g_MapRW_LinkedListWalkDepth: %lld\n", g_MapRW_LinkedListWalkDepth);
    fprintf(stderr, "g_MapRW_LinkedListAverageDepth: %f\n", (double)g_MapRW_LinkedListWalkDepth/(double)g_MapRW_CallsWithCacheMiss);
    fprintf(stderr, "g_LinkedListTotalDepth: %lld\n", g_LinkedListTotalDepth);

    fprintf(stderr, "ExecutableWriterHolder usage:\n");

    for (int i = 0; i < s_logMaxIndex; i++)
    {
        fprintf(stderr, "Count: %d at %s:%d in %s\n", s_usageLog[i].count, s_usageLog[i].source, s_usageLog[i].line, s_usageLog[i].function);
    }
}

#endif // LOG_EXECUTABLE_ALLOCATOR_STATISTICS

bool ExecutableAllocator::IsDoubleMappingEnabled()
{
    LIMITED_METHOD_CONTRACT;

#if defined(HOST_OSX) && defined(HOST_ARM64)
    return false;
#else
    return g_isWXorXEnabled;
#endif
}

bool ExecutableAllocator::IsWXORXEnabled()
{
    LIMITED_METHOD_CONTRACT;

#if defined(HOST_OSX) && defined(HOST_ARM64)
    return true;
#else
    return g_isWXorXEnabled;
#endif
}

extern SYSTEM_INFO g_SystemInfo;

size_t ExecutableAllocator::Granularity()
{
    LIMITED_METHOD_CONTRACT;

    return g_SystemInfo.dwAllocationGranularity;
}

void ExecutableAllocator::InitLazyPreferredRange(size_t base, size_t size, int randomPageOffset)
{
#if USE_LAZY_PREFERRED_RANGE

#ifdef _DEBUG
    // If GetForceRelocs is enabled we don't constrain the pMinAddr
    if (PEDecoder::GetForceRelocs())
        return;
#endif

    //
    // If we are using USE_LAZY_PREFERRED_RANGE then we try to allocate memory close
    // to coreclr.dll.  This avoids having to create jump stubs for calls to
    // helpers and R2R images loaded close to coreclr.dll.
    //
    SIZE_T reach = 0x7FFF0000u;

    // We will choose the preferred code region based on the address of coreclr.dll. The JIT helpers
    // in coreclr.dll are the most heavily called functions.
    g_preferredRangeMin = (base + size > reach) ? (BYTE *)(base + size - reach) : (BYTE *)0;
    g_preferredRangeMax = (base + reach > base) ? (BYTE *)(base + reach) : (BYTE *)-1;

    BYTE * pStart;

    if (base > UINT32_MAX)
    {
        // Try to occupy the space as far as possible to minimize collisions with other ASLR assigned
        // addresses. Do not start at g_codeMinAddr exactly so that we can also reach common native images
        // that can be placed at higher addresses than coreclr.dll.
        pStart = g_preferredRangeMin + (g_preferredRangeMax - g_preferredRangeMin) / 8;
    }
    else
    {
        // clr.dll missed the base address?
        // Try to occupy the space right after it.
        pStart = (BYTE *)(base + size);
    }

    // Randomize the address space
    pStart += GetOsPageSize() * randomPageOffset;

    g_lazyPreferredRangeStart = pStart;
    g_lazyPreferredRangeHint = pStart;
#endif
}

void ExecutableAllocator::InitPreferredRange()
{
#ifdef TARGET_UNIX
    void *start, *end;
    PAL_GetExecutableMemoryAllocatorPreferredRange(&start, &end);
    g_preferredRangeMin = (BYTE *)start;
    g_preferredRangeMax = (BYTE *)end;
#endif
}

void ExecutableAllocator::ResetLazyPreferredRangeHint()
{
    LIMITED_METHOD_CONTRACT;
#if USE_LAZY_PREFERRED_RANGE
    g_lazyPreferredRangeHint = g_lazyPreferredRangeStart;
#endif
}
// Returns TRUE if p is located in the memory area where we prefer to put
// executable code and static fields. This area is typically close to the
// coreclr library.
bool ExecutableAllocator::IsPreferredExecutableRange(void * p)
{
    LIMITED_METHOD_CONTRACT;
    return g_preferredRangeMin <= (BYTE *)p && (BYTE *)p < g_preferredRangeMax;
}

ExecutableAllocator* ExecutableAllocator::Instance()
{
    LIMITED_METHOD_CONTRACT;
    return g_instance;
}

ExecutableAllocator::~ExecutableAllocator()
{
    if (IsDoubleMappingEnabled())
    {
        VMToOSInterface::DestroyDoubleMemoryMapper(m_doubleMemoryMapperHandle);
    }
}

HRESULT ExecutableAllocator::StaticInitialize(FatalErrorHandler fatalErrorHandler)
{
    LIMITED_METHOD_CONTRACT;

#ifdef VARIABLE_SIZED_CACHEDMAPPING_SIZE
    g_cachedMappingSize = ARRAY_SIZE(m_cachedMapping);
    auto envString = getenv("EXECUTABLE_ALLOCATOR_CACHE_SIZE");
    if (envString != NULL)
    {
        int customCacheSize = atoi(envString);
        if (customCacheSize != 0)
        {
            if ((customCacheSize > ARRAY_SIZE(m_cachedMapping)) || (customCacheSize <= 0))
            {
                printf("Invalid value in 'EXECUTABLE_ALLOCATOR_CACHE_SIZE' environment variable'\n");
                return E_FAIL;
            }
            
            g_cachedMappingSize = customCacheSize;
        }
    }
#endif

    g_fatalErrorHandler = fatalErrorHandler;
    g_isWXorXEnabled = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableWriteXorExecute) != 0;
    g_instance = new (nothrow) ExecutableAllocator();
    if (g_instance == NULL)
    {
        return E_OUTOFMEMORY;
    }

    if (!g_instance->Initialize())
    {
        return E_FAIL;
    }

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    s_LoggerCriticalSection = ClrCreateCriticalSection(CrstExecutableAllocatorLock, CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
#endif
    return S_OK;
}

bool ExecutableAllocator::Initialize()
{
    LIMITED_METHOD_CONTRACT;

    if (IsDoubleMappingEnabled())
    {
        if (!VMToOSInterface::CreateDoubleMemoryMapper(&m_doubleMemoryMapperHandle, &m_maxExecutableCodeSize))
        {
            g_isWXorXEnabled = false;
            return true;
        }

        m_CriticalSection = ClrCreateCriticalSection(CrstExecutableAllocatorLock,CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
    }

    return true;
}

#define ENABLE_CACHED_MAPPINGS

void ExecutableAllocator::RemoveCachedMapping(size_t index)
{
#ifdef ENABLE_CACHED_MAPPINGS
    if (index == 0)
        return;

    BlockRW* cachedMapping = m_cachedMapping[index - 1];

    if (cachedMapping == NULL)
        return;

    void* unmapAddress = NULL;
    size_t unmapSize;

    if (!RemoveRWBlock(cachedMapping->baseRW, &unmapAddress, &unmapSize))
    {
        g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("The RW block to unmap was not found"));
    }
    if (unmapAddress && !VMToOSInterface::ReleaseRWMapping(unmapAddress, unmapSize))
    {
        g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Releasing the RW mapping failed"));
    }

    m_cachedMapping[index - 1] = NULL;
#endif // ENABLE_CACHED_MAPPINGS
}

#ifdef ENABLE_CACHED_MAPPINGS
size_t ExecutableAllocator::FindOverlappingCachedMapping(BlockRX* pBlock)
{
    for (size_t index = 0; index < EXECUTABLE_ALLOCATOR_CACHE_SIZE; index++)
    {
        BlockRW* cachedMapping = m_cachedMapping[index];
        if (cachedMapping != NULL)
        {
            // In case the cached mapping maps the region being released, it needs to be removed
            if ((pBlock->baseRX <= cachedMapping->baseRX) && (cachedMapping->baseRX < ((BYTE*)pBlock->baseRX + pBlock->size)))
            {
                return index + 1;
            }
        }
    }
    return 0;
}
#endif

void ExecutableAllocator::UpdateCachedMapping(BlockRW* pBlock)
{
    LIMITED_METHOD_CONTRACT;
#ifdef ENABLE_CACHED_MAPPINGS
    for (size_t index = 0; index < EXECUTABLE_ALLOCATOR_CACHE_SIZE; index++)
    {
        if (pBlock == m_cachedMapping[index])
        {
            // Move the found mapping to the front - note the overlapping memory, use memmove.
            memmove(&m_cachedMapping[1], &m_cachedMapping[0], sizeof(m_cachedMapping[0]) * index);
            m_cachedMapping[0] = pBlock;
            return;
        }
    }

    // Must insert mapping in front - note the overlapping memory, use memmove.
    RemoveCachedMapping(EXECUTABLE_ALLOCATOR_CACHE_SIZE);
    memmove(&m_cachedMapping[1], &m_cachedMapping[0], sizeof(m_cachedMapping[0]) * (EXECUTABLE_ALLOCATOR_CACHE_SIZE - 1));
    m_cachedMapping[0] = pBlock;
    pBlock->refCount++;
#endif // ENABLE_CACHED_MAPPINGS
}

void* ExecutableAllocator::FindRWBlock(void* baseRX, size_t size, CacheableMapping cacheMapping)
{
    LIMITED_METHOD_CONTRACT;

    for (BlockRW* pBlock = m_pFirstBlockRW; pBlock != NULL; pBlock = pBlock->next)
    {
        if (pBlock->baseRX <= baseRX && ((size_t)baseRX + size) <= ((size_t)pBlock->baseRX + pBlock->size))
        {
#ifdef TARGET_64BIT
            InterlockedIncrement64((LONG64*)& pBlock->refCount);
#else
            InterlockedIncrement((LONG*)&pBlock->refCount);
#endif
            if (cacheMapping == AddToCache)
                UpdateCachedMapping(pBlock);

            return (BYTE*)pBlock->baseRW + ((size_t)baseRX - (size_t)pBlock->baseRX);
        }
    }

    return NULL;
}

bool ExecutableAllocator::AddRWBlock(void* baseRW, void* baseRX, size_t size, CacheableMapping cacheMapping)
{
    LIMITED_METHOD_CONTRACT;

    // The new "nothrow" below failure is handled as fail fast since it is not recoverable
    PERMANENT_CONTRACT_VIOLATION(FaultViolation, ReasonContractInfrastructure);

    BlockRW* pBlockRW = new (nothrow) BlockRW();
    if (pBlockRW == NULL)
    {
        g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("The RW block metadata cannot be allocated"));
        return false;
    }

    pBlockRW->baseRW = baseRW;
    pBlockRW->baseRX = baseRX;
    pBlockRW->size = size;
    pBlockRW->next = m_pFirstBlockRW;
    pBlockRW->refCount = 1;
    m_pFirstBlockRW = pBlockRW;

    if (cacheMapping == AddToCache)
        UpdateCachedMapping(pBlockRW);

    return true;
}

bool ExecutableAllocator::RemoveRWBlock(void* pRW, void** pUnmapAddress, size_t* pUnmapSize)
{
    LIMITED_METHOD_CONTRACT;

    BlockRW* pPrevBlockRW = NULL;
    for (BlockRW* pBlockRW = m_pFirstBlockRW; pBlockRW != NULL; pBlockRW = pBlockRW->next)
    {
        if (pBlockRW->baseRW <= pRW && (size_t)pRW < ((size_t)pBlockRW->baseRW + pBlockRW->size))
        {
            // found
            pBlockRW->refCount--;
            if (pBlockRW->refCount != 0)
            {
                *pUnmapAddress = NULL;
                return true;
            }

            if (pPrevBlockRW == NULL)
            {
                m_pFirstBlockRW = pBlockRW->next;
            }
            else
            {
                pPrevBlockRW->next = pBlockRW->next;
            }

            *pUnmapAddress = pBlockRW->baseRW;
            *pUnmapSize = pBlockRW->size;

            delete pBlockRW;
            return true;
        }

        pPrevBlockRW = pBlockRW;
    }

    return false;
}

bool ExecutableAllocator::AllocateOffset(size_t* pOffset, size_t size)
{
    LIMITED_METHOD_CONTRACT;

    size_t offset = m_freeOffset;
    size_t newFreeOffset = offset + size;

    if (newFreeOffset > m_maxExecutableCodeSize)
    {
        return false;
    }

    m_freeOffset = newFreeOffset;

    *pOffset = offset;

    return true;
}

void ExecutableAllocator::AddRXBlock(BlockRX* pBlock)
{
    LIMITED_METHOD_CONTRACT;

    pBlock->next = m_pFirstBlockRX;
    m_pFirstBlockRX = pBlock;

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    ExecutableAllocator::g_LinkedListTotalDepth++;
#endif
}

void* ExecutableAllocator::Commit(void* pStart, size_t size, bool isExecutable)
{
    LIMITED_METHOD_CONTRACT;

    if (IsDoubleMappingEnabled())
    {
        return VMToOSInterface::CommitDoubleMappedMemory(pStart, size, isExecutable);
    }
    else
    {
        return ClrVirtualAlloc(pStart, size, MEM_COMMIT, isExecutable ? PAGE_EXECUTE_READWRITE : PAGE_READWRITE);
    }
}

void ExecutableAllocator::Release(void* pRX)
{
    LIMITED_METHOD_CONTRACT;

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    InterlockedIncrement64(&g_releaseCount);
#endif

    if (IsDoubleMappingEnabled())
    {
        CRITSEC_Holder csh(m_CriticalSection);

        // Locate the RX block corresponding to the pRX and remove it from the linked list
        BlockRX* pBlock;
        BlockRX* pPrevBlock = NULL;

        for (pBlock = m_pFirstBlockRX; pBlock != NULL; pBlock = pBlock->next)
        {
            if (pRX == pBlock->baseRX)
            {
                if (pPrevBlock == NULL)
                {
                    m_pFirstBlockRX = pBlock->next;
                }
                else
                {
                    pPrevBlock->next = pBlock->next;
                }

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
                ExecutableAllocator::g_LinkedListTotalDepth--;
#endif
                break;
            }
            pPrevBlock = pBlock;
        }

        if (pBlock != NULL)
        {
            size_t cachedMappingThatOverlaps = FindOverlappingCachedMapping(pBlock);
            while (cachedMappingThatOverlaps != 0)
            {
                RemoveCachedMapping(cachedMappingThatOverlaps);
                cachedMappingThatOverlaps = FindOverlappingCachedMapping(pBlock);
            }

            if (!VMToOSInterface::ReleaseDoubleMappedMemory(m_doubleMemoryMapperHandle, pRX, pBlock->offset, pBlock->size))
            {
                g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Releasing the double mapped memory failed"));
            }
            // Put the released block into the free block list
            pBlock->baseRX = NULL;
            pBlock->next = m_pFirstFreeBlockRX;
            m_pFirstFreeBlockRX = pBlock;
        }
        else
        {
            // The block was not found, which should never happen.
            g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("The RX block to release was not found"));
        }

        _ASSERTE(FindRWBlock(pRX, 1, CacheableMapping::DoNotAddToCache) == NULL);
    }
    else
    {
        ClrVirtualFree(pRX, 0, MEM_RELEASE);
    }
}

// Find a free block with the size == the requested size.
// Returns NULL if no such block exists.
ExecutableAllocator::BlockRX* ExecutableAllocator::FindBestFreeBlock(size_t size)
{
    LIMITED_METHOD_CONTRACT;

    BlockRX* pPrevBlock = NULL;
    BlockRX* pBlock = m_pFirstFreeBlockRX;

    while (pBlock != NULL)
    {
        if (pBlock->size == size)
        {
            break;
        }
        pPrevBlock = pBlock;
        pBlock = pBlock->next;
    }

    if (pBlock != NULL)
    {
        if (pPrevBlock != NULL)
        {
            pPrevBlock->next = pBlock->next;
        }
        else
        {
            m_pFirstFreeBlockRX = pBlock->next;
        }

        pBlock->next = NULL;
    }

    return pBlock;
}

// Allocate a new block of executable memory and the related descriptor structure.
// First try to get it from the free blocks and if there is no suitable free block,
// allocate a new one.
ExecutableAllocator::BlockRX* ExecutableAllocator::AllocateBlock(size_t size, bool* pIsFreeBlock)
{
    LIMITED_METHOD_CONTRACT;

    size_t offset;
    BlockRX* block = FindBestFreeBlock(size);
    *pIsFreeBlock = (block != NULL);

    if (block == NULL)
    {
        if (!AllocateOffset(&offset, size))
        {
            return NULL;
        }

        block = new (nothrow) BlockRX();
        if (block == NULL)
        {
            return NULL;
        }

        block->offset = offset;
        block->size = size;
    }

    return block;
}

// Backout a previously allocated block. The block is added to the free blocks list and
// reused for later allocation requests.
void ExecutableAllocator::BackoutBlock(BlockRX* pBlock, bool isFreeBlock)
{
    LIMITED_METHOD_CONTRACT;

    if (!isFreeBlock)
    {
        m_freeOffset -= pBlock->size;
        delete pBlock;
    }
    else
    {
        pBlock->next = m_pFirstFreeBlockRX;
        m_pFirstFreeBlockRX = pBlock;
    }
}

// Reserve executable memory within the specified virtual address space range. If it is not possible to
// reserve memory in that range, the method returns NULL and nothing is allocated.
void* ExecutableAllocator::ReserveWithinRange(size_t size, const void* loAddress, const void* hiAddress)
{
    LIMITED_METHOD_CONTRACT;

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    InterlockedIncrement64(&g_reserveCount);
#endif

    _ASSERTE((size & (Granularity() - 1)) == 0);
    if (IsDoubleMappingEnabled())
    {
        CRITSEC_Holder csh(m_CriticalSection);

        bool isFreeBlock;
        BlockRX* block = AllocateBlock(size, &isFreeBlock);
        if (block == NULL)
        {
            return NULL;
        }

        void *result = VMToOSInterface::ReserveDoubleMappedMemory(m_doubleMemoryMapperHandle, block->offset, size, loAddress, hiAddress);

        if (result != NULL)
        {
            block->baseRX = result;
            AddRXBlock(block);
        }
        else
        {
            BackoutBlock(block, isFreeBlock);
        }

        return result;
    }
    else
    {
        DWORD allocationType = MEM_RESERVE;
#ifdef HOST_UNIX
        // Tell PAL to use the executable memory allocator to satisfy this request for virtual memory.
        // This will allow us to place JIT'ed code close to the coreclr library
        // and thus improve performance by avoiding jump stubs in managed code.
        allocationType |= MEM_RESERVE_EXECUTABLE;
#endif
        return ClrVirtualAllocWithinRange((const BYTE*)loAddress, (const BYTE*)hiAddress, size, allocationType, PAGE_NOACCESS);
    }
}

// Reserve executable memory. On Windows it tries to use the allocation hints to
// allocate memory close to the previously allocated executable memory and loaded
// executable files.
void* ExecutableAllocator::Reserve(size_t size)
{
    LIMITED_METHOD_CONTRACT;

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    InterlockedIncrement64(&g_reserveCount);
#endif

    _ASSERTE((size & (Granularity() - 1)) == 0);

    BYTE *result = NULL;

#if USE_LAZY_PREFERRED_RANGE
    //
    // If we are using the UPPER_ADDRESS space (on Win64)
    // then for any heap that will contain executable code
    // we will place it in the upper address space
    //
    // This enables us to avoid having to use JumpStubs
    // to reach the code for our ngen-ed images on x64,
    // since they are also placed in the UPPER_ADDRESS space.
    //
    BYTE * pHint = g_lazyPreferredRangeHint;

    if (size <= (SIZE_T)(g_preferredRangeMax - g_preferredRangeMin) && pHint != NULL)
    {
        // Try to allocate in the preferred region after the hint
        result = (BYTE*)ReserveWithinRange(size, pHint, g_preferredRangeMax);
        if (result != NULL)
        {
            g_lazyPreferredRangeHint = result + size;
        }
        else
        {
            // Try to allocate in the preferred region before the hint
            result = (BYTE*)ReserveWithinRange(size, g_preferredRangeMin, pHint + size);

            if (result != NULL)
            {
                g_lazyPreferredRangeHint = result + size;
            }

            g_lazyPreferredRangeHint = NULL;
        }
    }

    // Fall through to
#endif // USE_LAZY_PREFERRED_RANGE

    if (result == NULL)
    {
        if (IsDoubleMappingEnabled())
        {
            CRITSEC_Holder csh(m_CriticalSection);

            bool isFreeBlock;
            BlockRX* block = AllocateBlock(size, &isFreeBlock);
            if (block == NULL)
            {
                return NULL;
            }

            result = (BYTE*)VMToOSInterface::ReserveDoubleMappedMemory(m_doubleMemoryMapperHandle, block->offset, size, 0, 0);

            if (result != NULL)
            {
                block->baseRX = result;
                AddRXBlock(block);
            }
            else
            {
                BackoutBlock(block, isFreeBlock);
            }
        }
        else
        {
            DWORD allocationType = MEM_RESERVE;
#ifdef HOST_UNIX
            // Tell PAL to use the executable memory allocator to satisfy this request for virtual memory.
            // This will allow us to place JIT'ed code close to the coreclr library
            // and thus improve performance by avoiding jump stubs in managed code.
            allocationType |= MEM_RESERVE_EXECUTABLE;
#endif
            result = (BYTE*)ClrVirtualAlloc(NULL, size, allocationType, PAGE_NOACCESS);
        }
    }

    return result;
}

// Reserve a block of executable memory at the specified virtual address. If it is not
// possible, the method returns NULL.
void* ExecutableAllocator::ReserveAt(void* baseAddressRX, size_t size)
{
    LIMITED_METHOD_CONTRACT;

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    InterlockedIncrement64(&g_reserveCount);
#endif

    _ASSERTE((size & (Granularity() - 1)) == 0);

    if (IsDoubleMappingEnabled())
    {
        CRITSEC_Holder csh(m_CriticalSection);

        bool isFreeBlock;
        BlockRX* block = AllocateBlock(size, &isFreeBlock);
        if (block == NULL)
        {
            return NULL;
        }

        void* result = VMToOSInterface::ReserveDoubleMappedMemory(m_doubleMemoryMapperHandle, block->offset, size, baseAddressRX, baseAddressRX);

        if (result != NULL)
        {
            block->baseRX = result;
            AddRXBlock(block);
        }
        else
        {
            BackoutBlock(block, isFreeBlock);
        }

        return result;
    }
    else
    {
        return VirtualAlloc(baseAddressRX, size, MEM_RESERVE, PAGE_NOACCESS);
    }
}

// Map an executable memory block as writeable. If there is already a mapping
// covering the specified block, return that mapping instead of creating a new one.
// Return starting address of the writeable mapping.
void* ExecutableAllocator::MapRW(void* pRX, size_t size, CacheableMapping cacheMapping)
{
    LIMITED_METHOD_CONTRACT;

    if (!IsDoubleMappingEnabled())
    {
        return pRX;
    }

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    StopWatch swAll(&g_mapTimeWithLockSum);
#endif

    CRITSEC_Holder csh(m_CriticalSection);
#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    ExecutableAllocator::g_MapRW_Calls++;
#endif

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    StopWatch sw(&g_mapTimeSum);
#endif

    void* result = FindRWBlock(pRX, size, cacheMapping);
    if (result != NULL)
    {
        return result;
    }
#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    StopWatch sw2(&g_mapFindRXTimeSum);
#endif
#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    ExecutableAllocator::g_MapRW_CallsWithCacheMiss++;
#endif

    for (BlockRX** ppBlock = &m_pFirstBlockRX; *ppBlock != NULL; ppBlock = &((*ppBlock)->next))
    {
        BlockRX* pBlock = *ppBlock;
#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
        ExecutableAllocator::g_MapRW_LinkedListWalkDepth++;
#endif
        if (pRX >= pBlock->baseRX && ((size_t)pRX + size) <= ((size_t)pBlock->baseRX + pBlock->size))
        {
            // Move found block to the front of the singly linked list
            *ppBlock = pBlock->next;
            pBlock->next = m_pFirstBlockRX;
            m_pFirstBlockRX = pBlock;

            // Offset of the RX address in the originally allocated block
            size_t offset = (size_t)pRX - (size_t)pBlock->baseRX;
            // Offset of the RX address that will start the newly mapped block
            size_t mapOffset = ALIGN_DOWN(offset, Granularity());
            // Size of the block we will map
            size_t mapSize = ALIGN_UP(offset - mapOffset + size, Granularity());

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
            StopWatch sw2(&g_mapCreateTimeSum);
#endif
            void* pRW = VMToOSInterface::GetRWMapping(m_doubleMemoryMapperHandle, (BYTE*)pBlock->baseRX + mapOffset, pBlock->offset + mapOffset, mapSize);

            if (pRW == NULL)
            {
                g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Failed to create RW mapping for RX memory"));
            }

            AddRWBlock(pRW, (BYTE*)pBlock->baseRX + mapOffset, mapSize, cacheMapping);

            return (void*)((size_t)pRW + (offset - mapOffset));
        }
        else if (pRX >= pBlock->baseRX && pRX < (void*)((size_t)pBlock->baseRX + pBlock->size))
        {
            g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Attempting to RW map a block that crosses the end of the allocated RX range"));
        }
        else if (pRX < pBlock->baseRX && (void*)((size_t)pRX + size) > pBlock->baseRX)
        {
            g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Attempting to map a block that crosses the beginning of the allocated range"));
        }
    }

    // The executable memory block was not found, so we cannot provide the writeable mapping.
    g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("The RX block to map as RW was not found"));
    return NULL;
}

// Unmap writeable mapping at the specified address. The address must be an address
// returned by the MapRW method.
void ExecutableAllocator::UnmapRW(void* pRW)
{
    LIMITED_METHOD_CONTRACT;

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    StopWatch swAll(&g_unmapTimeWithLockSum);
#endif

    if (!IsDoubleMappingEnabled())
    {
        return;
    }

    CRITSEC_Holder csh(m_CriticalSection);
    _ASSERTE(pRW != NULL);

#ifdef LOG_EXECUTABLE_ALLOCATOR_STATISTICS
    StopWatch swNoLock(&g_unmapTimeSum);
#endif

    void* unmapAddress = NULL;
    size_t unmapSize;

    if (!RemoveRWBlock(pRW, &unmapAddress, &unmapSize))
    {
        g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("The RW block to unmap was not found"));
    }

    if (unmapAddress && !VMToOSInterface::ReleaseRWMapping(unmapAddress, unmapSize))
    {
        g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Releasing the RW mapping failed"));
    }
}
