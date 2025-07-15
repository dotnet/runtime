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
    inline void EtwAllocRequest(UnlockedLoaderHeap * const pHeap, void* ptr, size_t dwSize)
    {
        FireEtwAllocRequest(pHeap, ptr, static_cast<unsigned int>(dwSize), 0, 0, GetClrInstanceId());
    }
#else
#define EtwAllocRequest(pHeap, ptr, dwSize) ((void)0)
#endif // SELF_NO_HOST
}

#ifdef _DEBUG
#define LOADER_HEAP_BEGIN_TRAP_FAULT BOOL __faulted = FALSE; EX_TRY {
#define LOADER_HEAP_END_TRAP_FAULT   } EX_CATCH {__faulted = TRUE; } EX_END_CATCH if (__faulted) UnlockedLoaderHeap::WeGotAFaultNowWhat(pHeap);
#else
#define LOADER_HEAP_BEGIN_TRAP_FAULT
#define LOADER_HEAP_END_TRAP_FAULT
#endif

#endif // #ifndef DACCESS_COMPILE

//=====================================================================================
// UnlockedLoaderHeap methods
//=====================================================================================

#ifndef DACCESS_COMPILE

UnlockedLoaderHeap::UnlockedLoaderHeap(DWORD dwReserveBlockSize,
                                       DWORD dwCommitBlockSize,
                                       const BYTE* dwReservedRegionAddress,
                                       SIZE_T dwReservedRegionSize,
                                       RangeList *pRangeList,
                                       LoaderHeapImplementationKind kind) :
    UnlockedLoaderHeapBase(kind)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_dwReserveBlockSize         = dwReserveBlockSize;
    m_dwCommitBlockSize          = dwCommitBlockSize;

    m_pEndReservedRegion         = NULL;

    m_pRangeList                 = pRangeList;

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

    return TRUE;
}

#ifdef FEATURE_PERFMAP
bool PerfMapLowGranularityStubs();
#endif // FEATURE_PERFMAP
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
        dwSizeToReserve = max<size_t>(dwSizeToCommit, m_dwReserveBlockSize);

#ifdef FEATURE_PERFMAP // Perfmap requires that the memory assigned to stub generated regions be allocated only via fully commited memory
        if (!IsInterleaved() || !PerfMapLowGranularityStubs())
#endif // FEATURE_PERFMAP
        {
            // Round to VIRTUAL_ALLOC_RESERVE_GRANULARITY
            dwSizeToReserve = ALIGN_UP(dwSizeToReserve, VIRTUAL_ALLOC_RESERVE_GRANULARITY);
        }

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

            LoaderHeapValidationTag *pTag = AllocMem_GetTag(pAllocatedBytes, dwRequestedSize);
            pTag->m_allocationType  = kAllocMem;
            pTag->m_dwRequestedSize = dwRequestedSize;
            pTag->m_szFile          = szFile;
            pTag->m_lineNum         = lineNum;

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
    if (m_dwDebugFlags & kCallTracing)
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

    if (dwRequestedSize != 0)
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

    LoaderHeapValidationTag *pTag = AllocMem_GetTag(pAllocatedBytes - extra, dwRequestedSize + extra);
    pTag->m_allocationType  = kAllocMem;
    pTag->m_dwRequestedSize = dwRequestedSize + extra;
    pTag->m_szFile          = szFile;
    pTag->m_lineNum         = lineNum;
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


#ifdef _DEBUG

void UnlockedLoaderHeap::DumpFreeList()
{
    LIMITED_METHOD_CONTRACT;
    if (m_pFirstFreeBlock == NULL)
    {
        minipal_log_print_info("FREEDUMP: FreeList is empty\n");
    }
    else
    {
        InlineSString<128> buf;
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

            buf.Printf("Addr = %pxh, Size = %xh", pBlock, ((ULONG)dwsize));
            if (ccbad) buf.AppendUTF8(" *** ERROR: NOT CC'd ***");
            if (sizeunaligned) buf.AppendUTF8(" *** ERROR: size not a multiple of ALLOC_ALIGN_CONSTANT ***");
            buf.AppendUTF8("\n");

            minipal_log_print_info(buf.GetUTF8());
            buf.Clear();

            pBlock = pBlock->m_pNext;
        }
    }
}

#endif //_DEBUG

#ifndef DACCESS_COMPILE
/*static*/ void LoaderHeapFreeBlock::InsertFreeBlock(LoaderHeapFreeBlock **ppHead, void *pMem, size_t dwTotalSize, UnlockedLoaderHeap *pHeap)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    // The new "nothrow" below failure is handled in a non-fault way, so
    // make sure that callers with FORBID_FAULT can call this method without
    // firing the contract violation assert.
    PERMANENT_CONTRACT_VIOLATION(FaultViolation, ReasonContractInfrastructure);

    LOADER_HEAP_BEGIN_TRAP_FAULT

    // It's illegal to insert a free block that's smaller than the minimum sized allocation -
    // it may stay stranded on the freelist forever.
#ifdef _DEBUG
    if (!(dwTotalSize >= pHeap->AllocMem_TotalSize(1)))
    {
        UnlockedLoaderHeap::ValidateFreeList(pHeap);
        _ASSERTE(dwTotalSize >= pHeap->AllocMem_TotalSize(1));
    }

    if (!(0 == (dwTotalSize & ALLOC_ALIGN_CONSTANT)))
    {
        UnlockedLoaderHeap::ValidateFreeList(pHeap);
        _ASSERTE(0 == (dwTotalSize & ALLOC_ALIGN_CONSTANT));
    }
#endif

#ifdef DEBUG
    {
        void* pMemRW = pMem;
        ExecutableWriterHolderNoLog<void> memWriterHolder;
        if (pHeap->IsExecutable())
        {
            memWriterHolder.AssignExecutableWriterHolder(pMem, dwTotalSize);
            pMemRW = memWriterHolder.GetRW();
        }

        memset(pMemRW, 0xcc, dwTotalSize);
    }
#endif // DEBUG

    LoaderHeapFreeBlock *pNewBlock = new (nothrow) LoaderHeapFreeBlock;
    // If we fail allocating the LoaderHeapFreeBlock, ignore the failure and don't insert the free block at all.
    if (pNewBlock != NULL)
    {
        pNewBlock->m_pNext  = *ppHead;
        pNewBlock->m_dwSize = dwTotalSize;
        pNewBlock->m_pBlockAddress = pMem;
        *ppHead = pNewBlock;
        MergeBlock(pNewBlock, pHeap);
    }

    LOADER_HEAP_END_TRAP_FAULT
}

/*static*/ void *LoaderHeapFreeBlock::AllocFromFreeList(LoaderHeapFreeBlock **ppHead, size_t dwSize, UnlockedLoaderHeap *pHeap)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    INCONTRACT(_ASSERTE_IMPL(!ARE_FAULTS_FORBIDDEN()));

    void *pResult = NULL;
    LOADER_HEAP_BEGIN_TRAP_FAULT

    LoaderHeapFreeBlock **ppWalk = ppHead;
    while (*ppWalk)
    {
        LoaderHeapFreeBlock *pCur = *ppWalk;
        size_t dwCurSize = pCur->m_dwSize;
        if (dwCurSize == dwSize)
        {
            pResult = pCur->m_pBlockAddress;
            // Exact match. Hooray!
            *ppWalk = pCur->m_pNext;
            delete pCur;
            break;
        }
        else if (dwCurSize > dwSize && (dwCurSize - dwSize) >= pHeap->AllocMem_TotalSize(1))
        {
            // Partial match. Ok...
            pResult = pCur->m_pBlockAddress;
            *ppWalk = pCur->m_pNext;
            InsertFreeBlock(ppWalk, ((BYTE*)pCur->m_pBlockAddress) + dwSize, dwCurSize - dwSize, pHeap );
            delete pCur;
            break;
        }

        // Either block is too small or splitting the block would leave a remainder that's smaller than
        // the minimum block size. Onto next one.

        ppWalk = &( pCur->m_pNext );
    }

    if (pResult)
    {
        void *pResultRW = pResult;
        ExecutableWriterHolderNoLog<void> resultWriterHolder;
        if (pHeap->IsExecutable())
        {
            resultWriterHolder.AssignExecutableWriterHolder(pResult, dwSize);
            pResultRW = resultWriterHolder.GetRW();
        }
        // Callers of loaderheap assume allocated memory is zero-inited so we must preserve this invariant!
        memset(pResultRW, 0, dwSize);
    }
    LOADER_HEAP_END_TRAP_FAULT
    return pResult;
}

// Try to merge pFreeBlock with its immediate successor. Return TRUE if a merge happened. FALSE if no merge happened.
/*static*/ BOOL LoaderHeapFreeBlock::MergeBlock(LoaderHeapFreeBlock *pFreeBlock, UnlockedLoaderHeap *pHeap)
{
    STATIC_CONTRACT_NOTHROW;

    BOOL result = FALSE;

    LOADER_HEAP_BEGIN_TRAP_FAULT

    LoaderHeapFreeBlock *pNextBlock = pFreeBlock->m_pNext;
    size_t               dwSize     = pFreeBlock->m_dwSize;

    if (pNextBlock == NULL || ((BYTE*)pNextBlock->m_pBlockAddress) != (((BYTE*)pFreeBlock->m_pBlockAddress) + dwSize))
    {
        result = FALSE;
    }
    else
    {
        size_t dwCombinedSize = dwSize + pNextBlock->m_dwSize;
        LoaderHeapFreeBlock *pNextNextBlock = pNextBlock->m_pNext;
        void *pMemRW = pFreeBlock->m_pBlockAddress;
        ExecutableWriterHolderNoLog<void> memWriterHolder;
        if (pHeap->IsExecutable())
        {
            memWriterHolder.AssignExecutableWriterHolder(pFreeBlock->m_pBlockAddress, dwCombinedSize);
            pMemRW = memWriterHolder.GetRW();
        }
        INDEBUG(memset(pMemRW, 0xcc, dwCombinedSize);)
        pFreeBlock->m_pNext  = pNextNextBlock;
        pFreeBlock->m_dwSize = dwCombinedSize;
        delete pNextBlock;

        result = TRUE;
    }

    LOADER_HEAP_END_TRAP_FAULT
    return result;
}
#endif // !DACCESS_COMPILE

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
#ifdef _DEBUG
    dwSize += LOADER_HEAP_DEBUG_BOUNDARY;
    dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
#endif

#ifdef _DEBUG
    dwSize += sizeof(LoaderHeapValidationTag);
#endif
    dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));

    return dwSize;
}

#ifdef _DEBUG
/*static*/
void UnlockedLoaderHeap::ValidateFreeList(UnlockedLoaderHeap *pHeap)
{
    CANNOT_HAVE_CONTRACT;

    // No contract. This routine is only called if we've AV'd inside the
    // loaderheap. The system is already toast. We're trying to be a hero
    // and produce the best diagnostic info we can. Last thing we need here
    // is a secondary assert inside the contract stuff.
    //
    // This contract violation is permanent.
    CONTRACT_VIOLATION(ThrowsViolation|FaultViolation|GCViolation|ModeViolation);  // This violation won't be removed

    LoaderHeapFreeBlock *pFree     = pHeap->m_pFirstFreeBlock;
    LoaderHeapFreeBlock *pPrev     = NULL;


    void                *pBadAddr = NULL;
    LoaderHeapFreeBlock *pProbeThis = NULL;
    const char          *pExpected = NULL;

    while (pFree != NULL)
    {
        if ( 0 != ( ((ULONG_PTR)pFree) & ALLOC_ALIGN_CONSTANT ))
        {
            // Not aligned - can't be a valid freeblock. Most likely we followed a bad pointer from the previous block.
            pProbeThis = pPrev;
            pBadAddr = pPrev ? &(pPrev->m_pNext) : &(pHeap->m_pFirstFreeBlock);
            pExpected = "a pointer to a valid LoaderHeapFreeBlock";
            break;
        }

        size_t dwSize = pFree->m_dwSize;
        if (dwSize < pHeap->AllocMem_TotalSize(1) ||
            0 != (dwSize & ALLOC_ALIGN_CONSTANT))
        {
            // Size is not a valid value (out of range or unaligned.)
            pProbeThis = pFree;
            pBadAddr = &(pFree->m_dwSize);
            pExpected = "a valid block size (multiple of pointer size)";
            break;
        }

        size_t i;
        for (i = sizeof(LoaderHeapFreeBlock); i < dwSize; i++)
        {
            if ( ((BYTE*)pFree)[i] != 0xcc )
            {
                pProbeThis = pFree;
                pBadAddr = i + ((BYTE*)pFree);
                pExpected = "0xcc (our fill value for free blocks)";
                break;
            }
        }
        if (i != dwSize)
        {
            break;
        }



        pPrev = pFree;
        pFree = pFree->m_pNext;
    }

    if (pFree == NULL)
    {
        return; // No problems found
    }

    {
        StackSString message;

        message.Printf("A loaderheap freelist has been corrupted. The bytes at or near address 0x%p appears to have been overwritten. We expected to see %s here.\n"
                       "\n"
                       "    LoaderHeap:                 0x%p\n"
                       "    Suspect address at:         0x%p\n"
                       "    Start of suspect freeblock: 0x%p\n"
                       "\n"
                       , pBadAddr
                       , pExpected
                       , pHeap
                       , pBadAddr
                       , pProbeThis
                       );

        if (!(pHeap->m_dwDebugFlags & pHeap->kCallTracing))
        {
            message.AppendASCII("\nThe usual reason is that someone wrote past the end of a block or wrote into a block after freeing it."
                           "\nOf course, the culprit is long gone so it's probably too late to debug this now. Try turning on call-tracing"
                           "\nand reproing. We can attempt to find out who last owned the surrounding pieces of memory."
                           "\n"
                           "\nTo turn on call-tracing, set the following registry DWORD value:"
                           "\n"
                           "\n    HKLM\\Software\\Microsoft\\.NETFramework\\LoaderHeapCallTracing = 1"
                           "\n"
                           );

        }
        else
        {
            LoaderHeapEvent *pBadAddrEvent = LoaderHeapSniffer::FindEvent(pHeap, pBadAddr);

            message.AppendASCII("*** CALL TRACING ENABLED ***\n\n");

            if (pBadAddrEvent)
            {
                message.AppendASCII("\nThe last known owner of the corrupted address was:\n");
                pBadAddrEvent->Describe(&message);
            }
            else
            {
                message.AppendASCII("\nNo known owner of last corrupted address.\n");
            }

            LoaderHeapEvent *pPrevEvent = LoaderHeapSniffer::FindEvent(pHeap, ((BYTE*)pProbeThis) - 1);

            int count = 3;
            while (count-- &&
                   pPrevEvent != NULL &&
                   ( ((UINT_PTR)pProbeThis) - ((UINT_PTR)(pPrevEvent->m_pMem)) + pPrevEvent->m_dwSize ) < 1024)
            {
                message.AppendASCII("\nThis block is located close to the corruption point. ");
                if (pPrevEvent->QuietValidate())
                {
                    message.AppendASCII("If it was overrun, it might have caused this.");
                }
                else
                {
                    message.AppendASCII("*** CORRUPTION DETECTED IN THIS BLOCK ***");
                }
                pPrevEvent->Describe(&message);
                pPrevEvent = LoaderHeapSniffer::FindEvent(pHeap, ((BYTE*)(pPrevEvent->m_pMem)) - 1);
            }
        }

        DbgAssertDialog(__FILE__, __LINE__, (char*) message.GetUTF8());
    }
}
/*static*/ void UnlockedLoaderHeap::WeGotAFaultNowWhat(UnlockedLoaderHeap *pHeap)
{
    WRAPPER_NO_CONTRACT;
    ValidateFreeList(pHeap);

    //If none of the above popped up an assert, pop up a generic one.
    _ASSERTE(!("Unexpected AV inside LoaderHeap. The usual reason is that someone overwrote the end of a block or wrote into a freed block.\n"));

}
#endif // _DEBUG
