// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Allocator and holders for double mapped executable memory
//

#pragma once

#include "utilcode.h"
#include "ex.h"

#include "minipal.h"

#ifndef DACCESS_COMPILE

// This class is responsible for allocation of all the executable memory in the runtime.
class ExecutableAllocator
{
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

    // Instance of the allocator
    static ExecutableAllocator* g_instance;

    // Callback to the runtime to report fatal errors
    static FatalErrorHandler g_fatalErrorHandler;

#if USE_UPPER_ADDRESS
    // Preferred region to allocate the code in.
    static BYTE* g_codeMinAddr;
    static BYTE* g_codeMaxAddr;
    static BYTE* g_codeAllocStart;
    // Next address to try to allocate for code in the preferred region.
    static BYTE* g_codeAllocHint;
#endif // USE_UPPER_ADDRESS

    // Caches the COMPlus_EnableWXORX setting
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

    // Last RW mapping cached so that it can be reused for the next mapping
    // request if it goes into the same range.
    BlockRW* m_cachedMapping = NULL;

    // Synchronization of the public allocator methods
    CRITSEC_COOKIE m_CriticalSection;

    // Update currently cached mapping. If the passed in block is the same as the one
    // in the cache, it keeps it cached. Otherwise it destroys the currently cached one
    // and replaces it by the passed in one.
    void UpdateCachedMapping(BlockRW *pBlock);

    // Find existing RW block that maps the whole specified range of RX memory.
    // Return NULL if no such block exists.
    void* FindRWBlock(void* baseRX, size_t size);

    // Add RW block to the list of existing RW blocks
    bool AddRWBlock(void* baseRW, void* baseRX, size_t size);

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

public:

    // Return the ExecuteAllocator singleton instance
    static ExecutableAllocator* Instance();

    // Initialize the static members of the Executable allocator and allocate
    // and initialize the instance of it.
    static HRESULT StaticInitialize(FatalErrorHandler fatalErrorHandler);

    // Destroy the allocator
    ~ExecutableAllocator();

    // Return true if W^X is enabled
    static bool IsWXORXEnabled();

    // Use this function to initialize the g_codeAllocHint
    // during startup. base is runtime .dll base address,
    // size is runtime .dll virtual size.
    static void InitCodeAllocHint(size_t base, size_t size, int randomPageOffset);

    // Use this function to reset the g_codeAllocHint
    // after unloading an AppDomain
    static void ResetCodeAllocHint();

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
    void* MapRW(void* pRX, size_t size);

    // Unmap the RW mapping at the specified address
    void UnmapRW(void* pRW);
};

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

    ExecutableWriterHolder(T* addressRX, size_t size)
    {
        m_addressRX = addressRX;
#if defined(HOST_OSX) && defined(HOST_ARM64)
        m_addressRW = addressRX;
        PAL_JitWriteProtect(true);
#else
        m_addressRW = (T *)ExecutableAllocator::Instance()->MapRW((void*)addressRX, size);
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
};

#endif // !DACCESS_COMPILE
