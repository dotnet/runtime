// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"                     // Precompiled header key.
#include "loaderheap.h"
#include "loaderheap_shared.h"
#include "ex.h"
#include "pedecoder.h"
#define DONOT_DEFINE_ETW_CALLBACK
#include "eventtracebase.h"

#ifndef DACCESS_COMPILE

INDEBUG(DWORD ExplicitControlLoaderHeap::s_dwNumInstancesOfLoaderHeaps = 0;)

namespace
{
#if !defined(SELF_NO_HOST) // ETW available only in the runtime
    inline void EtwAllocRequest(ExplicitControlLoaderHeap * const pHeap, void* ptr, size_t dwSize)
    {
        FireEtwAllocRequest(pHeap, ptr, static_cast<unsigned int>(dwSize), 0, 0, GetClrInstanceId());
    }
#else
#define EtwAllocRequest(pHeap, ptr, dwSize) ((void)0)
#endif // SELF_NO_HOST
}
#endif // DACCESS_COMPILE

size_t ExplicitControlLoaderHeap::AllocMem_TotalSize(size_t dwRequestedSize)
{
    LIMITED_METHOD_CONTRACT;

    size_t dwSize = dwRequestedSize;

#ifdef _DEBUG
    dwSize += LOADER_HEAP_DEBUG_BOUNDARY;
    dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
#endif

    dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));

    return dwSize;
}

//=====================================================================================
// UnlockedLoaderHeap methods
//=====================================================================================

#ifndef DACCESS_COMPILE
ExplicitControlLoaderHeap::ExplicitControlLoaderHeap(bool fMakeExecutable) :
    m_pAllocPtr(NULL),
    m_dwTotalAlloc(0),
    m_fExecutableHeap(fMakeExecutable)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_pPtrToEndOfCommittedRegion = NULL;
    m_pEndReservedRegion         = NULL;
    m_pAllocPtr                  = NULL;

    m_dwCommitBlockSize          = GetOsPageSize();

#ifdef _DEBUG
    m_dwDebugWastedBytes         = 0;
    s_dwNumInstancesOfLoaderHeaps++;
#endif
}

ExplicitControlLoaderHeap::~ExplicitControlLoaderHeap()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    LoaderHeapBlock *pSearch, *pNext;

    for (pSearch = m_pFirstBlock; pSearch; pSearch = pNext)
    {
        void *  pVirtualAddress;
        BOOL    fReleaseMemory;

        pVirtualAddress = pSearch->pVirtualAddress;
        fReleaseMemory = pSearch->m_fReleaseMemory;
        pNext = pSearch->pNext;

        if (fReleaseMemory)
        {
            ExecutableAllocator::Instance()->Release(pVirtualAddress);
        }

        delete pSearch;
    }

    if (m_reservedBlock.m_fReleaseMemory)
    {
        ExecutableAllocator::Instance()->Release(m_reservedBlock.pVirtualAddress);
    }

    INDEBUG(s_dwNumInstancesOfLoaderHeaps --;)
}

void ExplicitControlLoaderHeap::SetReservedRegion(BYTE* dwReservedRegionAddress, SIZE_T dwReservedRegionSize, BOOL fReleaseMemory)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_reservedBlock.pVirtualAddress == NULL);
    m_reservedBlock.Init((void *)dwReservedRegionAddress, dwReservedRegionSize, fReleaseMemory);
}

#endif // #ifndef DACCESS_COMPILE

size_t ExplicitControlLoaderHeap::GetBytesAvailCommittedRegion()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pAllocPtr < m_pPtrToEndOfCommittedRegion)
        return (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);
    else
        return 0;
}

size_t ExplicitControlLoaderHeap::GetBytesAvailReservedRegion()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pAllocPtr < m_pEndReservedRegion)
        return (size_t)(m_pEndReservedRegion- m_pAllocPtr);
    else
        return 0;
}

#ifndef DACCESS_COMPILE

BOOL ExplicitControlLoaderHeap::CommitPages(void* pData, size_t dwSizeToCommitPart)
{
    // Commit first set of pages, since it will contain the LoaderHeapBlock
    void *pTemp = ExecutableAllocator::Instance()->Commit(pData, dwSizeToCommitPart, m_fExecutableHeap ? TRUE : FALSE);
    if (pTemp == NULL)
    {
        return FALSE;
    }

    return TRUE;
}

BOOL ExplicitControlLoaderHeap::ReservePages(size_t dwSizeToCommit)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END;

    size_t dwSizeToReserve;

    // Round to page size again
    dwSizeToCommit = ALIGN_UP(dwSizeToCommit, GetOsPageSize());

    ReservedMemoryHolder pData = NULL;
    BOOL fReleaseMemory = TRUE;

    // We were provided with a reserved memory block at instance creation time, so use it if it's big enough.
    if (m_reservedBlock.pVirtualAddress != NULL &&
        m_reservedBlock.dwVirtualSize >= dwSizeToCommit)
    {
        // Get the info out of the block.
        pData = (PTR_BYTE)m_reservedBlock.pVirtualAddress;
        dwSizeToReserve = m_reservedBlock.dwVirtualSize;
        fReleaseMemory = m_reservedBlock.m_fReleaseMemory;

        // Zero the block so this memory doesn't get used again.
        m_reservedBlock.Init(NULL, 0, FALSE);
    }
    // The caller is asking us to allocate the memory
    else
    {
        return FALSE;
    }

    // When the user passes in the reserved memory, the commit size is 0 and is adjusted to be the sizeof(LoaderHeap).
    // If for some reason this is not true then we just catch this via an assertion and the dev who changed code
    // would have to add logic here to handle the case when committed mem is more than the reserved mem. One option
    // could be to leak the users memory and reserve+commit a new block, Another option would be to fail the alloc mem
    // and notify the user to provide more reserved mem.
    _ASSERTE((dwSizeToCommit <= dwSizeToReserve) && "Loaderheap tried to commit more memory than reserved by user");

    if (!fReleaseMemory)
    {
        pData.SuppressRelease();
    }

    size_t dwSizeToCommitPart = dwSizeToCommit;

    if (!CommitPages(pData, dwSizeToCommitPart))
    {
        return FALSE;
    }

    NewHolder<LoaderHeapBlock> pNewBlock = new (nothrow) LoaderHeapBlock;
    if (pNewBlock == NULL)
    {
        return FALSE;
    }

    m_dwTotalAlloc += dwSizeToCommit;

    pNewBlock.SuppressRelease();
    pData.SuppressRelease();

    pNewBlock->dwVirtualSize    = dwSizeToReserve;
    pNewBlock->pVirtualAddress  = pData;
    pNewBlock->pNext            = m_pFirstBlock;
    pNewBlock->m_fReleaseMemory = fReleaseMemory;

    // Add to the linked list
    m_pFirstBlock = pNewBlock;

    m_pPtrToEndOfCommittedRegion = (BYTE *) (pData) + (dwSizeToCommit);         \
    m_pAllocPtr                  = (BYTE *) (pData);                            \
    m_pEndReservedRegion         = (BYTE *) (pData) + (dwSizeToReserve);

    return TRUE;
}

// Get some more committed pages - either commit some more in the current reserved region, or, if it
// has run out, reserve another set of pages.
// Returns: FALSE if we can't get any more memory
// TRUE: We can/did get some more memory - check to see if it's sufficient for
//       the caller's needs (see UnlockedAllocMem for example of use)
BOOL ExplicitControlLoaderHeap::GetMoreCommittedPages(size_t dwMinSize)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END;

    // If we have memory we can use, what are you doing here!
    _ASSERTE(dwMinSize > (SIZE_T)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr));

    // Does this fit in the reserved region?
    if (dwMinSize <= (size_t)(m_pEndReservedRegion - m_pAllocPtr))
    {
        SIZE_T dwSizeToCommit;

        dwSizeToCommit = (m_pAllocPtr + dwMinSize) - m_pPtrToEndOfCommittedRegion;

        size_t unusedRemainder = (size_t)((BYTE*)m_pPtrToEndOfCommittedRegion - m_pAllocPtr);

        PTR_BYTE pCommitBaseAddress = m_pPtrToEndOfCommittedRegion;

        if (dwSizeToCommit < m_dwCommitBlockSize)
            dwSizeToCommit = min((SIZE_T)(m_pEndReservedRegion - m_pPtrToEndOfCommittedRegion), (SIZE_T)m_dwCommitBlockSize);

        // Round to page size
        dwSizeToCommit = ALIGN_UP(dwSizeToCommit, GetOsPageSize());

        size_t dwSizeToCommitPart = dwSizeToCommit;

        if (!CommitPages(pCommitBaseAddress, dwSizeToCommitPart))
        {
            return FALSE;
        }

        m_pPtrToEndOfCommittedRegion += dwSizeToCommit;
        m_dwTotalAlloc += dwSizeToCommit;

        return TRUE;
    }

    // Need to allocate a new set of reserved pages that will be located likely at a nonconsecutive virtual address.
    // Waste the unused bytes
    INDEBUG(m_dwDebugWastedBytes += (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);)

    // Note, there are unused reserved pages at end of current region -can't do much about that
    // Provide dwMinSize here since UnlockedReservePages will round up the commit size again
    // after adding in the size of the LoaderHeapBlock header.
    return ReservePages(dwMinSize);
}

void *ExplicitControlLoaderHeap::AllocMemForCode_NoThrow(size_t dwHeaderSize, size_t dwCodeSize, DWORD dwCodeAlignment, size_t dwReserveForJumpStubs)
{
    CONTRACT(void*)
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(CONTRACT_RETURN NULL;);
        PRECONDITION(0 == (dwCodeAlignment & (dwCodeAlignment - 1))); // require power of 2
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    // We don't know how much "extra" we need to satisfy the alignment until we know
    // which address will be handed out which in turn we don't know because we don't
    // know whether the allocation will fit within the current reserved range.
    //
    // Thus, we'll request as much heap growth as is needed for the worst case (we request an extra dwCodeAlignment - 1 bytes)

    S_SIZE_T cbAllocSize = S_SIZE_T(dwHeaderSize) + S_SIZE_T(dwCodeSize) + S_SIZE_T(dwCodeAlignment - 1) + S_SIZE_T(dwReserveForJumpStubs);
    if( cbAllocSize.IsOverflow() )
    {
        RETURN NULL;
    }

    if (cbAllocSize.Value() > GetBytesAvailCommittedRegion())
    {
        if (GetMoreCommittedPages(cbAllocSize.Value()) == FALSE)
        {
            RETURN NULL;
        }
    }

    BYTE *pResult = (BYTE *)ALIGN_UP(m_pAllocPtr + dwHeaderSize, dwCodeAlignment);
    EtwAllocRequest(this, pResult, (pResult + dwCodeSize) - m_pAllocPtr);
    m_pAllocPtr = pResult + dwCodeSize;

    RETURN pResult;
}


#endif // #ifndef DACCESS_COMPILE
