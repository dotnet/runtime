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

namespace
{
#if !defined(SELF_NO_HOST) // ETW available only in the runtime
    inline void EtwAllocRequest(UnlockedInterleavedLoaderHeap * const pHeap, void* ptr, size_t dwSize)
    {
        FireEtwAllocRequest(pHeap, ptr, static_cast<unsigned int>(dwSize), 0, 0, GetClrInstanceId());
    }
#else
#define EtwAllocRequest(pHeap, ptr, dwSize) ((void)0)
#endif // SELF_NO_HOST
}

#endif // #ifndef DACCESS_COMPILE

//=====================================================================================
// UnlockedInterleavedLoaderHeap methods
//=====================================================================================

#ifndef DACCESS_COMPILE

UnlockedInterleavedLoaderHeap::UnlockedInterleavedLoaderHeap(
                                       RangeList *pRangeList,
                                       const InterleavedLoaderHeapConfig *pConfig) :
    UnlockedLoaderHeapBase(LoaderHeapImplementationKind::Interleaved),
    m_pEndReservedRegion(NULL),
    m_dwGranularity(pConfig->StubSize),
    m_pRangeList(pRangeList),
    m_pFreeListHead(NULL),
    m_pConfig(pConfig)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    _ASSERTE((GetStubCodePageSize() % GetOsPageSize()) == 0); // Stub code page size MUST be in increments of the page size. (Really it must be a power of 2 as well, but this is good enough)
}

UnlockedInterleavedLoaderHeap::~UnlockedInterleavedLoaderHeap()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    if (m_pRangeList != NULL)
        m_pRangeList->RemoveRanges((void *) this);

    LoaderHeapBlock *pSearch, *pNext;

    for (pSearch = m_pFirstBlock; pSearch; pSearch = pNext)
    {
        void *  pVirtualAddress;

        pVirtualAddress = pSearch->pVirtualAddress;
        pNext = pSearch->pNext;

        if (m_pConfig->Template != NULL)
        {
            ExecutableAllocator::Instance()->FreeThunksFromTemplate(pVirtualAddress, GetStubCodePageSize());
        }
        else
        {
            ExecutableAllocator::Instance()->Release(pVirtualAddress);
        }

        delete pSearch;
    }
}
#endif // #ifndef DACCESS_COMPILE

size_t UnlockedInterleavedLoaderHeap::GetBytesAvailReservedRegion()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pAllocPtr < m_pEndReservedRegion)
        return (size_t)(m_pEndReservedRegion- m_pAllocPtr);
    else
        return 0;
}

#ifndef DACCESS_COMPILE

BOOL UnlockedInterleavedLoaderHeap::CommitPages(void* pData, size_t dwSizeToCommitPart)
{
    _ASSERTE(m_pConfig->Template == NULL); // This path should only be used for LoaderHeaps which use the standard ExecutableAllocator functions

    {
        void *pTemp = ExecutableAllocator::Instance()->Commit((BYTE*)pData + dwSizeToCommitPart, dwSizeToCommitPart, FALSE);
        if (pTemp == NULL)
        {
            return FALSE;
        }
        // Fill in data pages with the initial state, do this before we map the executable pages in, so that
        // the executable pages cannot speculate into the data page at any time before they are initialized.
        if (m_pConfig->DataPageGenerator != NULL)
        {
            m_pConfig->DataPageGenerator((uint8_t*)pTemp, dwSizeToCommitPart);
        }
    }

    // Commit first set of pages, since it will contain the LoaderHeapBlock
    {
        void *pTemp = ExecutableAllocator::Instance()->Commit(pData, dwSizeToCommitPart, IsExecutable());
        if (pTemp == NULL)
        {
            return FALSE;
        }
    }

    _ASSERTE(dwSizeToCommitPart == GetStubCodePageSize());

    ExecutableWriterHolder<BYTE> codePageWriterHolder((BYTE*)pData, dwSizeToCommitPart, ExecutableAllocator::DoNotAddToCache);
    m_pConfig->CodePageGenerator(codePageWriterHolder.GetRW(), (BYTE*)pData, dwSizeToCommitPart);
    FlushInstructionCache(GetCurrentProcess(), pData, dwSizeToCommitPart);

    return TRUE;
}

BOOL UnlockedInterleavedLoaderHeap::UnlockedReservePages(size_t dwSizeToCommit)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END;

    _ASSERTE(m_pConfig->Template == NULL); // This path should only be used for LoaderHeaps which use the standard ExecutableAllocator functions

    size_t dwSizeToReserve;

    // Round to page size again
    dwSizeToCommit = ALIGN_UP(dwSizeToCommit, GetOsPageSize());

    ReservedMemoryHolder pData = NULL;

    // Figure out how much to reserve
    dwSizeToReserve = dwSizeToCommit;

    // Round to VIRTUAL_ALLOC_RESERVE_GRANULARITY
    dwSizeToReserve = ALIGN_UP(dwSizeToReserve, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

    _ASSERTE(dwSizeToCommit <= dwSizeToReserve);

    //
    // Reserve pages
    //

    // Reserve the memory for even non-executable stuff close to the executable code, as it has profound effect
    // on e.g. a static variable access performance.
    pData = (BYTE *)ExecutableAllocator::Instance()->Reserve(dwSizeToReserve);
    if (pData == NULL)
    {
        _ASSERTE(!"Unable to reserve memory range for a loaderheap");
        return FALSE;
    }

    // When the user passes in the reserved memory, the commit size is 0 and is adjusted to be the sizeof(LoaderHeap).
    // If for some reason this is not true then we just catch this via an assertion and the dev who changed code
    // would have to add logic here to handle the case when committed mem is more than the reserved mem. One option
    // could be to leak the users memory and reserve+commit a new block, Another option would be to fail the alloc mem
    // and notify the user to provide more reserved mem.
    _ASSERTE((dwSizeToCommit <= dwSizeToReserve) && "Loaderheap tried to commit more memory than reserved by user");

    size_t dwSizeToCommitPart = dwSizeToCommit;

    // We perform two commits, each being half of the requested size
    dwSizeToCommitPart /= 2;

    if (!CommitPages(pData, dwSizeToCommitPart))
    {
        return FALSE;
    }

    NewHolder<LoaderHeapBlock> pNewBlock = new (nothrow) LoaderHeapBlock;
    if (pNewBlock == NULL)
    {
        return FALSE;
    }

    // Record reserved range in range list, if one is specified
    // Do this AFTER the commit - otherwise we'll have bogus ranges included.
    if (m_pRangeList != NULL)
    {
        if (!m_pRangeList->AddRange((const BYTE *) pData,
                                    ((const BYTE *) pData) + dwSizeToReserve,
                                    (void *) this))
        {
            return FALSE;
        }
    }

    m_dwTotalAlloc += dwSizeToCommit;

    pNewBlock.SuppressRelease();
    pData.SuppressRelease();

    pNewBlock->dwVirtualSize    = dwSizeToReserve;
    pNewBlock->pVirtualAddress  = pData;
    pNewBlock->pNext            = m_pFirstBlock;
    pNewBlock->m_fReleaseMemory = TRUE;

    // Add to the linked list
    m_pFirstBlock = pNewBlock;

    dwSizeToCommit /= 2;

    m_pPtrToEndOfCommittedRegion = (BYTE *) (pData) + (dwSizeToCommit);         \
    m_pAllocPtr                  = (BYTE *) (pData);                            \
    m_pEndReservedRegion         = (BYTE *) (pData) + (dwSizeToReserve);

    return TRUE;
}

void ReleaseAllocatedThunks(BYTE* thunks)
{
    ExecutableAllocator::Instance()->FreeThunksFromTemplate(thunks, GetStubCodePageSize());
}

using ThunkMemoryHolder = SpecializedWrapper<BYTE, ReleaseAllocatedThunks>;


// Get some more committed pages - either commit some more in the current reserved region, or, if it
// has run out, reserve another set of pages.
// Returns: FALSE if we can't get any more memory
// TRUE: We can/did get some more memory - check to see if it's sufficient for
//       the caller's needs (see UnlockedAllocMem for example of use)
BOOL UnlockedInterleavedLoaderHeap::GetMoreCommittedPages(size_t dwMinSize)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END;

    if (m_pConfig->Template != NULL)
    {
        ThunkMemoryHolder newAllocatedThunks = (BYTE*)ExecutableAllocator::Instance()->AllocateThunksFromTemplate(m_pConfig->Template, GetStubCodePageSize(), m_pConfig->DataPageGenerator);
        if (newAllocatedThunks == NULL)
        {
            return FALSE;
        }

        NewHolder<LoaderHeapBlock> pNewBlock = new (nothrow) LoaderHeapBlock;
        if (pNewBlock == NULL)
        {
            return FALSE;
        }

        size_t dwSizeToReserve = GetStubCodePageSize() * 2;
    
        // Record reserved range in range list, if one is specified
        // Do this AFTER the commit - otherwise we'll have bogus ranges included.
        if (m_pRangeList != NULL)
        {
            if (!m_pRangeList->AddRange((const BYTE *) newAllocatedThunks,
                                        ((const BYTE *) newAllocatedThunks) + dwSizeToReserve,
                                        (void *) this))
            {
                return FALSE;
            }
        }
    
        m_dwTotalAlloc += dwSizeToReserve;
    
        pNewBlock.SuppressRelease();
        newAllocatedThunks.SuppressRelease();
    
        pNewBlock->dwVirtualSize    = dwSizeToReserve;
        pNewBlock->pVirtualAddress  = newAllocatedThunks;
        pNewBlock->pNext            = m_pFirstBlock;
        pNewBlock->m_fReleaseMemory = TRUE;
    
        // Add to the linked list
        m_pFirstBlock = pNewBlock;
    
        m_pAllocPtr = (BYTE*)newAllocatedThunks;
        m_pPtrToEndOfCommittedRegion = m_pAllocPtr + GetStubCodePageSize();
        m_pEndReservedRegion = m_pAllocPtr + dwSizeToReserve; // For consistency with the non-template path m_pEndReservedRegion is after the end of the data area
        m_dwTotalAlloc += GetStubCodePageSize();

        return TRUE;
    }

    // From here, all work is only for the dynamically allocated InterleavedLoaderHeap path

    // If we have memory we can use, what are you doing here!
    _ASSERTE(dwMinSize > (SIZE_T)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr));

    // This mode interleaves data and code pages 1:1. So the code size is required to be smaller than
    // or equal to the page size to ensure that the code range is consecutive.
    _ASSERTE(dwMinSize <= GetStubCodePageSize());
    // We always get two memory pages - one for code and one for data
    dwMinSize = 2 * GetStubCodePageSize();

    // Does this fit in the reserved region?
    if (dwMinSize <= (size_t)(m_pEndReservedRegion - m_pAllocPtr))
    {
        SIZE_T dwSizeToCommit;

        // The allocation cannot cross page boundary since there are data and executable
        // pages interleaved in a 1:1 fashion.
        dwSizeToCommit = dwMinSize;

        size_t unusedRemainder = (size_t)((BYTE*)m_pPtrToEndOfCommittedRegion - m_pAllocPtr);

        PTR_BYTE pCommitBaseAddress = m_pPtrToEndOfCommittedRegion;

        // The end of committed region points to the end of the executable
        // page and the data pages goes right after that. So we skip the data page here.
        pCommitBaseAddress += GetStubCodePageSize();

        size_t dwSizeToCommitPart = dwSizeToCommit;
        // We perform two commits, each being half of the requested size
        dwSizeToCommitPart /= 2;

        if (!CommitPages(pCommitBaseAddress, dwSizeToCommitPart))
        {
            return FALSE;
        }

        INDEBUG(m_dwDebugWastedBytes += unusedRemainder;)

        // Further allocations will start from the newly committed page as they cannot
        // cross page boundary.
        m_pAllocPtr = (BYTE*)pCommitBaseAddress;

        m_pPtrToEndOfCommittedRegion += dwSizeToCommit;
        m_dwTotalAlloc += dwSizeToCommit;

        return TRUE;
    }

    // Keep track of the unused memory in the current reserved region.
    INDEBUG(m_dwDebugWastedBytes += (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);)

    // Note, there are unused reserved pages at end of current region -can't do much about that
    // Provide dwMinSize here since UnlockedReservePages will round up the commit size again
    // after adding in the size of the LoaderHeapBlock header.
    return UnlockedReservePages(dwMinSize);
}

#ifdef _DEBUG
static DWORD ShouldInjectFault()
{
    static DWORD fInjectFault = 99;

    if (fInjectFault == 99)
        fInjectFault = (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InjectFault) != 0);
    return fInjectFault;
}

#define SHOULD_INJECT_FAULT(return_statement)   \
    do {                                        \
        if (ShouldInjectFault() & 0x1)          \
        {                                       \
            char *a = new (nothrow) char;       \
            if (a == NULL)                      \
            {                                   \
                return_statement;               \
            }                                   \
            delete a;                           \
        }                                       \
    } while (FALSE)

#else

#define SHOULD_INJECT_FAULT(return_statement) do { (void)((void *)0); } while (FALSE)

#endif

void UnlockedInterleavedLoaderHeap::UnlockedBackoutStub(void *pMem
                                            COMMA_INDEBUG(_In_ const char *szFile)
                                            COMMA_INDEBUG(int  lineNum)
                                            COMMA_INDEBUG(_In_ const char *szAllocFile)
                                            COMMA_INDEBUG(int  allocLineNum))
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // Because the primary use of this function is backout, we'll be nice and
    // define Backout(NULL) be a legal NOP.
    if (pMem == NULL)
    {
        return;
    }

    size_t dwSize = m_dwGranularity;

    // Clear the RW page
    memset((BYTE*)pMem + GetStubCodePageSize(), 0x00, dwSize); // Fill freed region with 0

    if (m_pAllocPtr == ( ((BYTE*)pMem) + dwSize ))
    {
        m_pAllocPtr = (BYTE*)pMem;
    }
    else
    {
        InterleavedStubFreeListNode* newFreeNode = (InterleavedStubFreeListNode*)((BYTE*)pMem + GetStubCodePageSize());
        newFreeNode->m_pNext = m_pFreeListHead;
        m_pFreeListHead = newFreeNode;
    }
}


// Allocates memory for a single stub which is a pair of memory addresses
// The first address is the pointer at the stub code, and the second
// address is the data for the stub. These are separated by GetStubCodePageSize()
// bytes.
//
// Here is how to properly backout the memory:
//
//   void *pMem = UnlockedAllocStub(d);
//   UnlockedBackoutStub(pMem);
//
// If you use the AllocMemHolder or AllocMemTracker, all this is taken care of
// behind the scenes.
//
//
void *UnlockedInterleavedLoaderHeap::UnlockedAllocStub_NoThrow(
                                                          INDEBUG(_In_ const char *szFile)
                                                          COMMA_INDEBUG(int  lineNum))
{
    CONTRACT(void*)
    {
        NOTHROW;

        // Macro syntax can't handle this INJECT_FAULT expression - we'll use a precondition instead
        //INJECT_FAULT( do{ if (*pdwExtra) {*pdwExtra = 0} RETURN NULL; } while(0) );

    }
    CONTRACT_END

    size_t dwRequestedSize = m_dwGranularity;
    size_t alignment = 1;

    STATIC_CONTRACT_FAULT;

    SHOULD_INJECT_FAULT(RETURN NULL);

    void *pResult;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    _ASSERTE(m_dwGranularity >= sizeof(InterleavedStubFreeListNode));

    if (m_pFreeListHead != NULL)
    {
        // We have a free stub - use it
        InterleavedStubFreeListNode* pFreeStubData = m_pFreeListHead;
        m_pFreeListHead = pFreeStubData->m_pNext;
        pFreeStubData->m_pNext = NULL;
        pResult = ((BYTE*)pFreeStubData) - GetStubCodePageSize();
    }
    else
    {
        if (dwRequestedSize > GetBytesAvailCommittedRegion())
        {
            if (!GetMoreCommittedPages(dwRequestedSize))
            {
                RETURN NULL;
            }
        }

        pResult = m_pAllocPtr;

        m_pAllocPtr += dwRequestedSize;
    }

#ifdef _DEBUG
    // Check to ensure that the RW region of the allocated stub is zeroed out if there isn't a data page generator
    if (m_pConfig->DataPageGenerator == NULL)
    {
        BYTE *pAllocatedRWBytes = (BYTE*)pResult + GetStubCodePageSize();
        for (size_t i = 0; i < dwRequestedSize; i++)
        {
            _ASSERTE_MSG(pAllocatedRWBytes[i] == 0, "LoaderHeap must return zero-initialized memory");
        }
    }

    if (m_dwDebugFlags & kCallTracing)
    {
        LoaderHeapSniffer::RecordEvent(this,
                                       kAllocMem,
                                       szFile,
                                       lineNum,
                                       szFile,
                                       lineNum,
                                       pResult,
                                       dwRequestedSize,
                                       dwRequestedSize
                                       );
    }

    EtwAllocRequest(this, pResult, dwRequestedSize);
#endif //_DEBUG

    RETURN pResult;
}

void *UnlockedInterleavedLoaderHeap::UnlockedAllocStub(
                                                  INDEBUG(_In_ const char *szFile)
                                                  COMMA_INDEBUG(int  lineNum))
{
    CONTRACTL
    {
        THROWS;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END

    void *pResult = UnlockedAllocStub_NoThrow(INDEBUG(szFile)
                                              COMMA_INDEBUG(lineNum));

    if (!pResult)
    {
        ThrowOutOfMemory();
    }

    return pResult;
}

void InitializeLoaderHeapConfig(InterleavedLoaderHeapConfig *pConfig, size_t stubSize, void* templateInImage, void (*codePageGenerator)(uint8_t* pageBase, uint8_t* pageBaseRX, size_t size), void (*dataPageGenerator)(uint8_t* pageBase, size_t size))
{
    pConfig->StubSize = (uint32_t)stubSize;
    pConfig->Template = ExecutableAllocator::Instance()->CreateTemplate(templateInImage, GetStubCodePageSize(), codePageGenerator);
    pConfig->CodePageGenerator = codePageGenerator;
    pConfig->DataPageGenerator = dataPageGenerator;
}

#endif // #ifndef DACCESS_COMPILE

