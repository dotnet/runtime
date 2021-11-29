// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pedecoder.h"
#include "executableallocator.h"

#if USE_UPPER_ADDRESS
// Preferred region to allocate the code in.
BYTE * ExecutableAllocator::g_codeMinAddr;
BYTE * ExecutableAllocator::g_codeMaxAddr;
BYTE * ExecutableAllocator::g_codeAllocStart;
// Next address to try to allocate for code in the preferred region.
BYTE * ExecutableAllocator::g_codeAllocHint;
#endif // USE_UPPER_ADDRESS

bool ExecutableAllocator::g_isWXorXEnabled = false;

ExecutableAllocator::FatalErrorHandler ExecutableAllocator::g_fatalErrorHandler = NULL;

ExecutableAllocator* ExecutableAllocator::g_instance = NULL;

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

// Use this function to initialize the g_codeAllocHint
// during startup. base is runtime .dll base address,
// size is runtime .dll virtual size.
void ExecutableAllocator::InitCodeAllocHint(size_t base, size_t size, int randomPageOffset)
{
#if USE_UPPER_ADDRESS

#ifdef _DEBUG
    // If GetForceRelocs is enabled we don't constrain the pMinAddr
    if (PEDecoder::GetForceRelocs())
        return;
#endif

    //
    // If we are using the UPPER_ADDRESS space (on Win64)
    // then for any code heap that doesn't specify an address
    // range using [pMinAddr..pMaxAddr] we place it in the
    // upper address space
    // This enables us to avoid having to use long JumpStubs
    // to reach the code for our ngen-ed images.
    // Which are also placed in the UPPER_ADDRESS space.
    //
    SIZE_T reach = 0x7FFF0000u;

    // We will choose the preferred code region based on the address of clr.dll. The JIT helpers
    // in clr.dll are the most heavily called functions.
    g_codeMinAddr = (base + size > reach) ? (BYTE *)(base + size - reach) : (BYTE *)0;
    g_codeMaxAddr = (base + reach > base) ? (BYTE *)(base + reach) : (BYTE *)-1;

    BYTE * pStart;

    if (g_codeMinAddr <= (BYTE *)CODEHEAP_START_ADDRESS &&
        (BYTE *)CODEHEAP_START_ADDRESS < g_codeMaxAddr)
    {
        // clr.dll got loaded at its preferred base address? (OS without ASLR - pre-Vista)
        // Use the code head start address that does not cause collisions with NGen images.
        // This logic is coupled with scripts that we use to assign base addresses.
        pStart = (BYTE *)CODEHEAP_START_ADDRESS;
    }
    else
    if (base > UINT32_MAX)
    {
        // clr.dll got address assigned by ASLR?
        // Try to occupy the space as far as possible to minimize collisions with other ASLR assigned
        // addresses. Do not start at g_codeMinAddr exactly so that we can also reach common native images
        // that can be placed at higher addresses than clr.dll.
        pStart = g_codeMinAddr + (g_codeMaxAddr - g_codeMinAddr) / 8;
    }
    else
    {
        // clr.dll missed the base address?
        // Try to occupy the space right after it.
        pStart = (BYTE *)(base + size);
    }

    // Randomize the address space
    pStart += GetOsPageSize() * randomPageOffset;

    g_codeAllocStart = pStart;
    g_codeAllocHint = pStart;
#endif
}

// Use this function to reset the g_codeAllocHint
// after unloading an AppDomain
void ExecutableAllocator::ResetCodeAllocHint()
{
    LIMITED_METHOD_CONTRACT;
#if USE_UPPER_ADDRESS
    g_codeAllocHint = g_codeAllocStart;
#endif
}

// Returns TRUE if p is located in near clr.dll that allows us
// to use rel32 IP-relative addressing modes.
bool ExecutableAllocator::IsPreferredExecutableRange(void * p)
{
    LIMITED_METHOD_CONTRACT;
#if USE_UPPER_ADDRESS
    if (g_codeMinAddr <= (BYTE *)p && (BYTE *)p < g_codeMaxAddr)
        return true;
#endif
    return false;
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

    return S_OK;
}

bool ExecutableAllocator::Initialize()
{
    LIMITED_METHOD_CONTRACT;

    if (IsDoubleMappingEnabled())
    {
        if (!VMToOSInterface::CreateDoubleMemoryMapper(&m_doubleMemoryMapperHandle, &m_maxExecutableCodeSize))
        {
            return false;
        }

        m_CriticalSection = ClrCreateCriticalSection(CrstExecutableAllocatorLock,CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
    }

    return true;
}

//#define ENABLE_CACHED_MAPPINGS

void ExecutableAllocator::UpdateCachedMapping(BlockRW* pBlock)
{
    LIMITED_METHOD_CONTRACT;
#ifdef ENABLE_CACHED_MAPPINGS
    if (m_cachedMapping == NULL)
    {
        m_cachedMapping = pBlock;
        pBlock->refCount++;
    }
    else if (m_cachedMapping != pBlock)
    {
        void* unmapAddress = NULL;
        size_t unmapSize;

        if (!RemoveRWBlock(m_cachedMapping->baseRW, &unmapAddress, &unmapSize))
        {
            g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("The RW block to unmap was not found"));
        }
        if (unmapAddress && !VMToOSInterface::ReleaseRWMapping(unmapAddress, unmapSize))
        {
            g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Releasing the RW mapping failed"));
        }
        m_cachedMapping = pBlock;
        pBlock->refCount++;
    }
#endif // ENABLE_CACHED_MAPPINGS    
}

void* ExecutableAllocator::FindRWBlock(void* baseRX, size_t size)
{
    LIMITED_METHOD_CONTRACT;

    for (BlockRW* pBlock = m_pFirstBlockRW; pBlock != NULL; pBlock = pBlock->next)
    {
        if (pBlock->baseRX <= baseRX && ((size_t)baseRX + size) <= ((size_t)pBlock->baseRX + pBlock->size))
        {
            pBlock->refCount++;
            UpdateCachedMapping(pBlock);

            return (BYTE*)pBlock->baseRW + ((size_t)baseRX - (size_t)pBlock->baseRX);
        }
    }

    return NULL;
}

bool ExecutableAllocator::AddRWBlock(void* baseRW, void* baseRX, size_t size)
{
    LIMITED_METHOD_CONTRACT;

    for (BlockRW* pBlock = m_pFirstBlockRW; pBlock != NULL; pBlock = pBlock->next)
    {
        if (pBlock->baseRX <= baseRX && ((size_t)baseRX + size) <= ((size_t)pBlock->baseRX + pBlock->size))
        {
            break;
        }
    }

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

                break;
            }
            pPrevBlock = pBlock;
        }

        if (pBlock != NULL)
        {
            VMToOSInterface::ReleaseDoubleMappedMemory(m_doubleMemoryMapperHandle, pRX, pBlock->offset, pBlock->size);
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
    }
    else
    {
        ClrVirtualFree(pRX, 0, MEM_RELEASE);
    }
}

// Find a free block with the closest size >= the requested size.
// Returns NULL if no such block exists.
ExecutableAllocator::BlockRX* ExecutableAllocator::FindBestFreeBlock(size_t size)
{
    LIMITED_METHOD_CONTRACT;

    BlockRX* pPrevBlock = NULL;
    BlockRX* pPrevBestBlock = NULL;
    BlockRX* pBestBlock = NULL;
    BlockRX* pBlock = m_pFirstFreeBlockRX;

    while (pBlock != NULL)
    {
        if (pBlock->size >= size)
        {
            if (pBestBlock != NULL)
            {
                if (pBlock->size < pBestBlock->size)
                {
                    pPrevBestBlock = pPrevBlock;
                    pBestBlock = pBlock;
                }
            }
            else
            {
                pPrevBestBlock = pPrevBlock;
                pBestBlock = pBlock;
            }
        }
        pPrevBlock = pBlock;
        pBlock = pBlock->next;
    }

    if (pBestBlock != NULL)
    {
        if (pPrevBestBlock != NULL)
        {
            pPrevBestBlock->next = pBestBlock->next;
        }
        else
        {
            m_pFirstFreeBlockRX = pBestBlock->next;
        }

        pBestBlock->next = NULL;
    }

    return pBestBlock;
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

    _ASSERTE((size & (Granularity() - 1)) == 0);

    BYTE *result = NULL;

#if USE_UPPER_ADDRESS
    //
    // If we are using the UPPER_ADDRESS space (on Win64)
    // then for any heap that will contain executable code
    // we will place it in the upper address space
    //
    // This enables us to avoid having to use JumpStubs
    // to reach the code for our ngen-ed images on x64,
    // since they are also placed in the UPPER_ADDRESS space.
    //
    BYTE * pHint = g_codeAllocHint;

    if (size <= (SIZE_T)(g_codeMaxAddr - g_codeMinAddr) && pHint != NULL)
    {
        // Try to allocate in the preferred region after the hint
        result = (BYTE*)ReserveWithinRange(size, pHint, g_codeMaxAddr);
        if (result != NULL)
        {
            g_codeAllocHint = result + size;
        }
        else
        {
            // Try to allocate in the preferred region before the hint
            result = (BYTE*)ReserveWithinRange(size, g_codeMinAddr, pHint + size);

            if (result != NULL)
            {
                g_codeAllocHint = result + size;
            }

            g_codeAllocHint = NULL;
        }
    }

    // Fall through to
#endif // USE_UPPER_ADDRESS

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
void* ExecutableAllocator::MapRW(void* pRX, size_t size)
{
    LIMITED_METHOD_CONTRACT;

    if (!IsDoubleMappingEnabled())
    {
        return pRX;
    }

    CRITSEC_Holder csh(m_CriticalSection);

    void* result = FindRWBlock(pRX, size);
    if (result != NULL)
    {
        return result;
    }

    for (BlockRX* pBlock = m_pFirstBlockRX; pBlock != NULL; pBlock = pBlock->next)
    {
        if (pRX >= pBlock->baseRX && ((size_t)pRX + size) <= ((size_t)pBlock->baseRX + pBlock->size))
        {
            // Offset of the RX address in the originally allocated block
            size_t offset = (size_t)pRX - (size_t)pBlock->baseRX;
            // Offset of the RX address that will start the newly mapped block
            size_t mapOffset = ALIGN_DOWN(offset, Granularity());
            // Size of the block we will map
            size_t mapSize = ALIGN_UP(offset - mapOffset + size, Granularity());
            void* pRW = VMToOSInterface::GetRWMapping(m_doubleMemoryMapperHandle, (BYTE*)pBlock->baseRX + mapOffset, pBlock->offset + mapOffset, mapSize);

            if (pRW == NULL)
            {
                g_fatalErrorHandler(COR_E_EXECUTIONENGINE, W("Failed to create RW mapping for RX memory"));
            }

            AddRWBlock(pRW, (BYTE*)pBlock->baseRX + mapOffset, mapSize);

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

    if (!IsDoubleMappingEnabled())
    {
        return;
    }

    CRITSEC_Holder csh(m_CriticalSection);
    _ASSERTE(pRW != NULL);

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
