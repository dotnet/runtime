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

INDEBUG(DWORD UnlockedLoaderHeap::s_dwNumInstancesOfLoaderHeaps = 0;)

namespace
{
#if !defined(SELF_NO_HOST) // ETW available only in the runtime
    inline void EtwAllocRequest(UnlockedLoaderHeap * const pHeap, void* ptr, size_t dwSize)
    {
        FireEtwAllocRequest(pHeap, ptr, static_cast<unsigned int>(dwSize), 0, 0, GetClrInstanceId());
    }
#else
#define EtwAllocRequest(pHeap, ptr, dwSize) ((void)0)
#endif // SELF_NO_HOST
}

#endif // #ifndef DACCESS_COMPILE

//=====================================================================================
// These helpers encapsulate the actual layout of a block allocated by AllocMem
// and UnlockedAllocMem():
//
// ==> Starting address is always pointer-aligned.
//
//   - x  bytes of user bytes        (where "x" is the actual dwSize passed into AllocMem)
//
//   - y  bytes of "EE" (DEBUG-ONLY) (where "y" == LOADER_HEAP_DEBUG_BOUNDARY (normally 0))
//   - z  bytes of pad  (DEBUG-ONLY) (where "z" is just enough to pointer-align the following byte)
//   - a  bytes of tag  (DEBUG-ONLY) (where "a" is sizeof(LoaderHeapValidationTag)
//
//   - b  bytes of pad               (where "b" is just enough to pointer-align the following byte)
//
// ==> Following address is always pointer-aligned
//=====================================================================================

// Convert the requested size into the total # of bytes we'll actually allocate (including padding)
size_t UnlockedLoaderHeap::AllocMem_TotalSize(size_t dwRequestedSize)
{
    LIMITED_METHOD_CONTRACT;

    size_t dwSize = dwRequestedSize;

    // Interleaved heap cannot ad any extra to the requested size
    if (!IsInterleaved())
    {
#ifdef _DEBUG
        dwSize += LOADER_HEAP_DEBUG_BOUNDARY;
        dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
#endif

#ifdef _DEBUG
        dwSize += sizeof(LoaderHeapValidationTag);
#endif
        dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
    }

    return dwSize;
}

//=====================================================================================
// UnlockedLoaderHeap methods
//=====================================================================================

#ifndef DACCESS_COMPILE

UnlockedLoaderHeap::UnlockedLoaderHeap(DWORD dwReserveBlockSize,
                                       DWORD dwCommitBlockSize,
                                       const BYTE* dwReservedRegionAddress,
                                       SIZE_T dwReservedRegionSize,
                                       RangeList *pRangeList,
                                       HeapKind kind,
                                       void (*codePageGenerator)(BYTE* pageBase, BYTE* pageBaseRX, SIZE_T size),
                                       DWORD dwGranularity)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_pFirstBlock                = NULL;

    m_dwReserveBlockSize         = dwReserveBlockSize;
    m_dwCommitBlockSize          = dwCommitBlockSize;

    m_pPtrToEndOfCommittedRegion = NULL;
    m_pEndReservedRegion         = NULL;
    m_pAllocPtr                  = NULL;

    m_pRangeList                 = pRangeList;

    // Round to VIRTUAL_ALLOC_RESERVE_GRANULARITY
    m_dwTotalAlloc               = 0;

    _ASSERTE((GetStubCodePageSize() % GetOsPageSize()) == 0); // Stub code page size MUST be in increments of the page size. (Really it must be a power of 2 as well, but this is good enough)
    m_dwGranularity = dwGranularity;

#ifdef _DEBUG
    m_dwDebugWastedBytes         = 0;
    s_dwNumInstancesOfLoaderHeaps++;
    m_pEventList                 = NULL;
    m_dwDebugFlags               = LoaderHeapSniffer::InitDebugFlags();
    m_fPermitStubsWithUnwindInfo = FALSE;
    m_fStubUnwindInfoUnregistered= FALSE;
#endif

    m_kind = kind;

    _ASSERTE((kind != HeapKind::Interleaved) || (codePageGenerator != NULL));
    m_codePageGenerator = codePageGenerator;

    m_pFirstFreeBlock            = NULL;

    if (dwReservedRegionAddress != NULL && dwReservedRegionSize > 0)
    {
        m_reservedBlock.Init((void *)dwReservedRegionAddress, dwReservedRegionSize, FALSE);
    }
}

// ~LoaderHeap is not synchronised (obviously)
UnlockedLoaderHeap::~UnlockedLoaderHeap()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE(!m_fPermitStubsWithUnwindInfo || m_fStubUnwindInfoUnregistered);

    if (m_pRangeList != NULL)
        m_pRangeList->RemoveRanges((void *) this);

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

#endif // #ifndef DACCESS_COMPILE

#if 0
// Disables access to all pages in the heap - useful when trying to determine if someone is
// accessing something in the low frequency heap
void UnlockedLoaderHeap::DebugGuardHeap()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapBlock *pSearch, *pNext;

    for (pSearch = m_pFirstBlock; pSearch; pSearch = pNext)
    {
        void *  pResult;
        void *  pVirtualAddress;

        pVirtualAddress = pSearch->pVirtualAddress;
        pNext = pSearch->pNext;

        pResult = ClrVirtualAlloc(pVirtualAddress, pSearch->dwVirtualSize, MEM_COMMIT, PAGE_NOACCESS);
        _ASSERTE(pResult != NULL);
    }
}
#endif

size_t UnlockedLoaderHeap::GetBytesAvailCommittedRegion()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pAllocPtr < m_pPtrToEndOfCommittedRegion)
        return (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);
    else
        return 0;
}

size_t UnlockedLoaderHeap::GetBytesAvailReservedRegion()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pAllocPtr < m_pEndReservedRegion)
        return (size_t)(m_pEndReservedRegion- m_pAllocPtr);
    else
        return 0;
}

#ifndef DACCESS_COMPILE

BOOL UnlockedLoaderHeap::CommitPages(void* pData, size_t dwSizeToCommitPart)
{
    // Commit first set of pages, since it will contain the LoaderHeapBlock
    void *pTemp = ExecutableAllocator::Instance()->Commit(pData, dwSizeToCommitPart, IsExecutable());
    if (pTemp == NULL)
    {
        return FALSE;
    }

    if (IsInterleaved())
    {
        _ASSERTE(dwSizeToCommitPart == GetStubCodePageSize());

        void *pTemp = ExecutableAllocator::Instance()->Commit((BYTE*)pData + dwSizeToCommitPart, dwSizeToCommitPart, FALSE);
        if (pTemp == NULL)
        {
            return FALSE;
        }

        ExecutableWriterHolder<BYTE> codePageWriterHolder((BYTE*)pData, dwSizeToCommitPart, ExecutableAllocator::DoNotAddToCache);
        m_codePageGenerator(codePageWriterHolder.GetRW(), (BYTE*)pData, dwSizeToCommitPart);
        FlushInstructionCache(GetCurrentProcess(), pData, dwSizeToCommitPart);
    }

    return TRUE;
}

BOOL UnlockedLoaderHeap::UnlockedReservePages(size_t dwSizeToCommit)
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
        // Figure out how much to reserve
        dwSizeToReserve = max(dwSizeToCommit, m_dwReserveBlockSize);

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
    if (IsInterleaved())
    {
        // For interleaved heaps, we perform two commits, each being half of the requested size
        dwSizeToCommitPart /= 2;
    }

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
    pNewBlock->m_fReleaseMemory = fReleaseMemory;

    // Add to the linked list
    m_pFirstBlock = pNewBlock;

    if (IsInterleaved())
    {
        dwSizeToCommit /= 2;
    }

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
BOOL UnlockedLoaderHeap::GetMoreCommittedPages(size_t dwMinSize)
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

    if (IsInterleaved())
    {
        // This mode interleaves data and code pages 1:1. So the code size is required to be smaller than
        // or equal to the page size to ensure that the code range is consecutive.
        _ASSERTE(dwMinSize <= GetStubCodePageSize());
        // For interleaved heap, we always get two memory pages - one for code and one for data
        dwMinSize = 2 * GetStubCodePageSize();
    }

    // Does this fit in the reserved region?
    if (dwMinSize <= (size_t)(m_pEndReservedRegion - m_pAllocPtr))
    {
        SIZE_T dwSizeToCommit;

        if (IsInterleaved())
        {
            // For interleaved heaps, the allocation cannot cross page boundary since there are data and executable
            // pages interleaved in a 1:1 fashion.
            dwSizeToCommit = dwMinSize;
        }
        else
        {
            dwSizeToCommit = (m_pAllocPtr + dwMinSize) - m_pPtrToEndOfCommittedRegion;
        }

        size_t unusedRemainder = (size_t)((BYTE*)m_pPtrToEndOfCommittedRegion - m_pAllocPtr);

        PTR_BYTE pCommitBaseAddress = m_pPtrToEndOfCommittedRegion;
        if (IsInterleaved())
        {
            // The end of committed region for interleaved heaps points to the end of the executable
            // page and the data pages goes right after that. So we skip the data page here.
            pCommitBaseAddress += GetStubCodePageSize();
        }
        else
        {
            if (dwSizeToCommit < m_dwCommitBlockSize)
                dwSizeToCommit = min((SIZE_T)(m_pEndReservedRegion - m_pPtrToEndOfCommittedRegion), (SIZE_T)m_dwCommitBlockSize);

            // Round to page size
            dwSizeToCommit = ALIGN_UP(dwSizeToCommit, GetOsPageSize());
        }

        size_t dwSizeToCommitPart = dwSizeToCommit;
        if (IsInterleaved())
        {
            // For interleaved heaps, we perform two commits, each being half of the requested size
            dwSizeToCommitPart /= 2;
        }

        if (!CommitPages(pCommitBaseAddress, dwSizeToCommitPart))
        {
            return FALSE;
        }

        if (IsInterleaved())
        {
            // If the remaining bytes are large enough to allocate data of the allocation granularity, add them to the free
            // block list.
            // Otherwise the remaining bytes that are available will be wasted.
            if (unusedRemainder >= GetStubCodePageSize())
            {
                LoaderHeapFreeBlock::InsertFreeBlock(&m_pFirstFreeBlock, m_pAllocPtr, unusedRemainder, this);
            }
            else
            {
                INDEBUG(m_dwDebugWastedBytes += unusedRemainder;)
            }

            // For interleaved heaps, further allocations will start from the newly committed page as they cannot
            // cross page boundary.
            m_pAllocPtr = (BYTE*)pCommitBaseAddress;
        }

        m_pPtrToEndOfCommittedRegion += dwSizeToCommit;
        m_dwTotalAlloc += dwSizeToCommit;

        return TRUE;
    }

    // Need to allocate a new set of reserved pages that will be located likely at a nonconsecutive virtual address.
    // If the remaining bytes are large enough to allocate data of the allocation granularity, add them to the free
    // block list.
    // Otherwise the remaining bytes that are available will be wasted.
    size_t unusedRemainder = (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);
    if (unusedRemainder >= AllocMem_TotalSize(GetStubCodePageSize()))
    {
        LoaderHeapFreeBlock::InsertFreeBlock(&m_pFirstFreeBlock, m_pAllocPtr, unusedRemainder, this);
    }
    else
    {
        INDEBUG(m_dwDebugWastedBytes += (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);)
    }

    // Note, there are unused reserved pages at end of current region -can't do much about that
    // Provide dwMinSize here since UnlockedReservePages will round up the commit size again
    // after adding in the size of the LoaderHeapBlock header.
    return UnlockedReservePages(dwMinSize);
}

void *UnlockedLoaderHeap::UnlockedAllocMem(size_t dwSize
                                           COMMA_INDEBUG(_In_ const char *szFile)
                                           COMMA_INDEBUG(int  lineNum))
{
    CONTRACT(void*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(ThrowOutOfMemory(););
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    void *pResult = UnlockedAllocMem_NoThrow(
        dwSize COMMA_INDEBUG(szFile) COMMA_INDEBUG(lineNum));

    if (pResult == NULL)
        ThrowOutOfMemory();

    RETURN pResult;
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

void *UnlockedLoaderHeap::UnlockedAllocMem_NoThrow(size_t dwSize
                                                   COMMA_INDEBUG(_In_ const char *szFile)
                                                   COMMA_INDEBUG(int lineNum))
{
    CONTRACT(void*)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(CONTRACT_RETURN NULL;);
        PRECONDITION(dwSize != 0);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    SHOULD_INJECT_FAULT(RETURN NULL);

    INDEBUG(size_t dwRequestedSize = dwSize;)

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

#ifdef RANDOMIZE_ALLOC
    if (!IsInterleaved())
        dwSize += s_randomForLoaderHeap.Next() % 256;
#endif

    dwSize = AllocMem_TotalSize(dwSize);

again:

    {
        // Any memory available on the free list?
        void *pData = LoaderHeapFreeBlock::AllocFromFreeList(&m_pFirstFreeBlock, dwSize, this);
        if (!pData)
        {
            // Enough bytes available in committed region?
            if (dwSize <= GetBytesAvailCommittedRegion())
            {
                pData = m_pAllocPtr;
                m_pAllocPtr += dwSize;
            }
        }

        if (pData)
        {
#ifdef _DEBUG
            BYTE *pAllocatedBytes = (BYTE*)pData;
            ExecutableWriterHolderNoLog<void> dataWriterHolder;
            if (IsExecutable())
            {
                dataWriterHolder.AssignExecutableWriterHolder(pData, dwSize);
                pAllocatedBytes = (BYTE *)dataWriterHolder.GetRW();
            }

#if LOADER_HEAP_DEBUG_BOUNDARY > 0
            // Don't fill the memory we allocated - it is assumed to be zeroed - fill the memory after it
            memset(pAllocatedBytes + dwRequestedSize, 0xEE, LOADER_HEAP_DEBUG_BOUNDARY);
#endif
            if (dwRequestedSize > 0)
            {
                _ASSERTE_MSG(pAllocatedBytes[0] == 0 && memcmp(pAllocatedBytes, pAllocatedBytes + 1, dwRequestedSize - 1) == 0,
                    "LoaderHeap must return zero-initialized memory");
            }

            if (!IsInterleaved())
            {
                LoaderHeapValidationTag *pTag = AllocMem_GetTag(pAllocatedBytes, dwRequestedSize);
                pTag->m_allocationType  = kAllocMem;
                pTag->m_dwRequestedSize = dwRequestedSize;
                pTag->m_szFile          = szFile;
                pTag->m_lineNum         = lineNum;
            }

            if (m_dwDebugFlags & kCallTracing)
            {
                LoaderHeapSniffer::RecordEvent(this,
                                               kAllocMem,
                                               szFile,
                                               lineNum,
                                               szFile,
                                               lineNum,
                                               pData,
                                               dwRequestedSize,
                                               dwSize
                                               );
            }

#endif

            EtwAllocRequest(this, pData, dwSize);
            RETURN pData;
        }
    }

    // Need to commit some more pages in reserved region.
    // If we run out of pages in the reserved region, ClrVirtualAlloc some more pages
    if (GetMoreCommittedPages(dwSize))
        goto again;

    // We could not satisfy this allocation request
    RETURN NULL;
}

void UnlockedLoaderHeap::UnlockedBackoutMem(void *pMem,
                                            size_t dwRequestedSize
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

#ifdef _DEBUG
    if (!IsInterleaved())
    {
        DEBUG_ONLY_REGION();

        LoaderHeapValidationTag *pTag = AllocMem_GetTag(pMem, dwRequestedSize);

        if (pTag->m_dwRequestedSize != dwRequestedSize || pTag->m_allocationType != kAllocMem)
        {
            CONTRACT_VIOLATION(ThrowsViolation|FaultViolation); // We're reporting a heap corruption - who cares about violations

            StackSString message;
            message.Printf("HEAP VIOLATION: Invalid BackoutMem() call made at:\n"
                           "\n"
                           "     File: %s\n"
                           "     Line: %d\n"
                           "\n"
                           "Attempting to free block originally allocated at:\n"
                           "\n"
                           "     File: %s\n"
                           "     Line: %d\n"
                           "\n"
                           "The arguments to BackoutMem() were:\n"
                           "\n"
                           "     Pointer: 0x%p\n"
                           "     Size:    %lu (0x%lx)\n"
                           "\n"
                           ,szFile
                           ,lineNum
                           ,szAllocFile
                           ,allocLineNum
                           ,pMem
                           ,(ULONG)dwRequestedSize
                           ,(ULONG)dwRequestedSize
                          );


            if (m_dwDebugFlags & kCallTracing)
            {
                message.AppendASCII("*** CALLTRACING ENABLED ***\n");
                LoaderHeapEvent *pEvent = LoaderHeapSniffer::FindEvent(this, pMem);
                if (!pEvent)
                {
                    message.AppendASCII("This pointer doesn't appear to have come from this LoaderHeap.\n");
                }
                else
                {
                    message.AppendASCII(pMem == pEvent->m_pMem ? "We have the following data about this pointer:" : "This pointer points to the middle of the following block:");
                    pEvent->Describe(&message);
                }
            }

            if (pTag->m_dwRequestedSize != dwRequestedSize)
            {
                StackSString buf;
                buf.Printf(
                        "Possible causes:\n"
                        "\n"
                        "   - This pointer wasn't allocated from this loaderheap.\n"
                        "   - This pointer was allocated by AllocAlignedMem and you didn't adjust for the \"extra.\"\n"
                        "   - This pointer has already been freed.\n"
                        "   - You passed in the wrong size. You must pass the exact same size you passed to AllocMem().\n"
                        "   - Someone wrote past the end of this block making it appear as if one of the above were true.\n"
                        );
                message.Append(buf);

            }
            else
            {
                message.AppendASCII("This memory block is completely unrecognizable.\n");
            }


            if (!(m_dwDebugFlags & kCallTracing))
            {
                LoaderHeapSniffer::PitchSniffer(&message);
            }

            DbgAssertDialog(szFile, lineNum, (char*) message.GetUTF8());

        }
    }
#endif

    size_t dwSize = AllocMem_TotalSize(dwRequestedSize);

#ifdef _DEBUG
    if ((m_dwDebugFlags & kCallTracing) && !IsInterleaved())
    {
        DEBUG_ONLY_REGION();

        LoaderHeapValidationTag *pTag = AllocMem_GetTag(pMem, dwRequestedSize);


        LoaderHeapSniffer::RecordEvent(this,
                                       kFreedMem,
                                       szFile,
                                       lineNum,
                                       (pTag && (allocLineNum < 0)) ? pTag->m_szFile  : szAllocFile,
                                       (pTag && (allocLineNum < 0)) ? pTag->m_lineNum : allocLineNum,
                                       pMem,
                                       dwRequestedSize,
                                       dwSize
                                       );
    }
#endif

    if (m_pAllocPtr == ( ((BYTE*)pMem) + dwSize ))
    {
        if (IsInterleaved())
        {
            // Clear the RW page
            memset((BYTE*)pMem + GetStubCodePageSize(), 0x00, dwSize); // Fill freed region with 0
        }
        else
        {
            void *pMemRW = pMem;
            ExecutableWriterHolderNoLog<void> memWriterHolder;
            if (IsExecutable())
            {
                memWriterHolder.AssignExecutableWriterHolder(pMem, dwSize);
                pMemRW = memWriterHolder.GetRW();
            }

            // Cool. This was the last block allocated. We can just undo the allocation instead
            // of going to the freelist.
            memset(pMemRW, 0x00, dwSize); // Fill freed region with 0
        }
        m_pAllocPtr = (BYTE*)pMem;
    }
    else
    {
        LoaderHeapFreeBlock::InsertFreeBlock(&m_pFirstFreeBlock, pMem, dwSize, this);
    }
}


// Allocates memory aligned on power-of-2 boundary.
//
// The return value is a pointer that's guaranteed to be aligned.
//
// FREEING THIS BLOCK: Underneath, the actual block allocated may
// be larger and start at an address prior to the one you got back.
// It is this adjusted size and pointer that you pass to BackoutMem.
// The required adjustment is passed back thru the pdwExtra pointer.
//
// Here is how to properly backout the memory:
//
//   size_t dwExtra;
//   void *pMem = UnlockedAllocAlignedMem(dwRequestedSize, alignment, &dwExtra);
//   _ASSERTE( 0 == (pMem & (alignment - 1)) );
//   UnlockedBackoutMem( ((BYTE*)pMem) - dExtra, dwRequestedSize + dwExtra );
//
// If you use the AllocMemHolder or AllocMemTracker, all this is taken care of
// behind the scenes.
//
//
void *UnlockedLoaderHeap::UnlockedAllocAlignedMem_NoThrow(size_t  dwRequestedSize,
                                                          size_t  alignment,
                                                          size_t *pdwExtra
                                                          COMMA_INDEBUG(_In_ const char *szFile)
                                                          COMMA_INDEBUG(int  lineNum))
{
    CONTRACT(void*)
    {
        NOTHROW;

        // Macro syntax can't handle this INJECT_FAULT expression - we'll use a precondition instead
        //INJECT_FAULT( do{ if (*pdwExtra) {*pdwExtra = 0} RETURN NULL; } while(0) );

        PRECONDITION( alignment != 0 );
        PRECONDITION(0 == (alignment & (alignment - 1))); // require power of 2
        PRECONDITION((dwRequestedSize % m_dwGranularity) == 0);
        POSTCONDITION( (RETVAL) ?
                       (0 == ( ((UINT_PTR)(RETVAL)) & (alignment - 1))) : // If non-null, pointer must be aligned
                       (pdwExtra == NULL || 0 == *pdwExtra)    //   or else *pdwExtra must be set to 0
                     );
    }
    CONTRACT_END

    STATIC_CONTRACT_FAULT;

    // Set default value
            if (pdwExtra)
            {
                *pdwExtra = 0;
            }

    SHOULD_INJECT_FAULT(RETURN NULL);

    void *pResult;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    // Check for overflow if we align the allocation
    if (dwRequestedSize + alignment < dwRequestedSize)
    {
        RETURN NULL;
    }

    // We don't know how much "extra" we need to satisfy the alignment until we know
    // which address will be handed out which in turn we don't know because we don't
    // know whether the allocation will fit within the current reserved range.
    //
    // Thus, we'll request as much heap growth as is needed for the worst case (extra == alignment)
    size_t dwRoomSize = AllocMem_TotalSize(dwRequestedSize + alignment);
    if (dwRoomSize > GetBytesAvailCommittedRegion())
    {
        if (!GetMoreCommittedPages(dwRoomSize))
        {
            RETURN NULL;
        }
    }

    pResult = m_pAllocPtr;

    size_t extra = alignment - ((size_t)pResult & ((size_t)alignment - 1));
    if ((IsInterleaved()))
    {
        _ASSERTE(alignment == 1);
        extra = 0;
    }

// On DEBUG, we force a non-zero extra so people don't forget to adjust for it on backout
#ifndef _DEBUG
    if (extra == alignment)
    {
        extra = 0;
    }
#endif

    S_SIZE_T cbAllocSize = S_SIZE_T( dwRequestedSize ) + S_SIZE_T( extra );
    if( cbAllocSize.IsOverflow() )
    {
        RETURN NULL;
    }

    size_t dwSize = AllocMem_TotalSize( cbAllocSize.Value());
    m_pAllocPtr += dwSize;


    ((BYTE*&)pResult) += extra;

#ifdef _DEBUG
    BYTE *pAllocatedBytes = (BYTE *)pResult;
    ExecutableWriterHolderNoLog<void> resultWriterHolder;
    if (IsExecutable())
    {
        resultWriterHolder.AssignExecutableWriterHolder(pResult, dwSize - extra);
        pAllocatedBytes = (BYTE *)resultWriterHolder.GetRW();
    }

#if LOADER_HEAP_DEBUG_BOUNDARY > 0
    // Don't fill the entire memory - we assume it is all zeroed -just the memory after our alloc
    memset(pAllocatedBytes + dwRequestedSize, 0xee, LOADER_HEAP_DEBUG_BOUNDARY);
#endif

    if (dwRequestedSize != 0 && !IsInterleaved())
    {
        _ASSERTE_MSG(pAllocatedBytes[0] == 0 && memcmp(pAllocatedBytes, pAllocatedBytes + 1, dwRequestedSize - 1) == 0,
            "LoaderHeap must return zero-initialized memory");
    }

    if (m_dwDebugFlags & kCallTracing)
    {
        LoaderHeapSniffer::RecordEvent(this,
                                       kAllocMem,
                                       szFile,
                                       lineNum,
                                       szFile,
                                       lineNum,
                                       ((BYTE*)pResult) - extra,
                                       dwRequestedSize + extra,
                                       dwSize
                                       );
    }

    EtwAllocRequest(this, pResult, dwSize);

    if (!IsInterleaved())
    {
        LoaderHeapValidationTag *pTag = AllocMem_GetTag(pAllocatedBytes - extra, dwRequestedSize + extra);
        pTag->m_allocationType  = kAllocMem;
        pTag->m_dwRequestedSize = dwRequestedSize + extra;
        pTag->m_szFile          = szFile;
        pTag->m_lineNum         = lineNum;
    }
#endif //_DEBUG

    if (pdwExtra)
    {
        *pdwExtra = extra;
    }

    RETURN pResult;

}



void *UnlockedLoaderHeap::UnlockedAllocAlignedMem(size_t  dwRequestedSize,
                                                  size_t  dwAlignment,
                                                  size_t *pdwExtra
                                                  COMMA_INDEBUG(_In_ const char *szFile)
                                                  COMMA_INDEBUG(int  lineNum))
{
    CONTRACTL
    {
        THROWS;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END

    void *pResult = UnlockedAllocAlignedMem_NoThrow(dwRequestedSize,
                                                    dwAlignment,
                                                    pdwExtra
                                                    COMMA_INDEBUG(szFile)
                                                    COMMA_INDEBUG(lineNum));

    if (!pResult)
    {
        ThrowOutOfMemory();
    }

    return pResult;


}
#endif // #ifndef DACCESS_COMPILE

BOOL UnlockedLoaderHeap::IsExecutable()
{
    return (m_kind == HeapKind::Executable) || IsInterleaved();
}

BOOL UnlockedLoaderHeap::IsInterleaved()
{
    return m_kind == HeapKind::Interleaved;
}

#ifdef DACCESS_COMPILE

void UnlockedLoaderHeap::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    PTR_LoaderHeapBlock block = m_pFirstBlock;
    while (block.IsValid())
    {
        // All we know is the virtual size of this block.  We don't have any way to tell how
        // much of this space was actually comitted, so don't expect that this will always
        // succeed.
        // @dbgtodo : Ideally we'd reduce the risk of corruption causing problems here.
        //   We could extend LoaderHeapBlock to track a commit size,
        //   but it seems wasteful (eg. makes each AppDomain objects 32 bytes larger on x64).
        TADDR addr = dac_cast<TADDR>(block->pVirtualAddress);
        TSIZE_T size = block->dwVirtualSize;
        EMEM_OUT(("MEM: UnlockedLoaderHeap %p - %p\n", addr, addr + size));
        DacEnumMemoryRegion(addr, size, false);

        block = block->pNext;
    }
}

#endif // #ifdef DACCESS_COMPILE


void UnlockedLoaderHeap::EnumPageRegions (EnumPageRegionsCallback *pCallback, PTR_VOID pvArgs)
{
    WRAPPER_NO_CONTRACT;

    PTR_LoaderHeapBlock block = m_pFirstBlock;
    while (block)
    {
        if ((*pCallback)(pvArgs, block->pVirtualAddress, block->dwVirtualSize))
        {
            break;
        }

        block = block->pNext;
    }
}

#ifdef _DEBUG

void UnlockedLoaderHeap::DumpFreeList()
{
    LIMITED_METHOD_CONTRACT;
    if (m_pFirstFreeBlock == NULL)
    {
        printf("FREEDUMP: FreeList is empty\n");
    }
    else
    {
        LoaderHeapFreeBlock *pBlock = m_pFirstFreeBlock;
        while (pBlock != NULL)
        {
            size_t dwsize = pBlock->m_dwSize;
            BOOL ccbad = FALSE;
            BOOL sizeunaligned = FALSE;

            if ( 0 != (dwsize & ALLOC_ALIGN_CONSTANT) )
            {
                sizeunaligned = TRUE;
            }

            for (size_t i = sizeof(LoaderHeapFreeBlock); i < dwsize; i++)
            {
                if ( ((BYTE*)pBlock)[i] != 0xcc )
                {
                    ccbad = TRUE;
                    break;
                }
            }

            printf("Addr = %pxh, Size = %xh", pBlock, ((ULONG)dwsize));
            if (ccbad) printf(" *** ERROR: NOT CC'd ***");
            if (sizeunaligned) printf(" *** ERROR: size not a multiple of ALLOC_ALIGN_CONSTANT ***");
            printf("\n");

            pBlock = pBlock->m_pNext;
        }
    }
}


void UnlockedLoaderHeap::UnlockedClearEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::ClearEvents(this);
}

void UnlockedLoaderHeap::UnlockedCompactEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::CompactEvents(this);
}

void UnlockedLoaderHeap::UnlockedPrintEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::PrintEvents(this);
}


#endif //_DEBUG
