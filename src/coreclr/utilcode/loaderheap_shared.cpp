#include "stdafx.h"                     // Precompiled header key.
#include "loaderheap.h"
#include "loaderheap_shared.h"
#include "ex.h"
#include "pedecoder.h"

#ifdef RANDOMIZE_ALLOC
RandomForLoaderHeap s_randomForLoaderHeap;
#endif

#ifndef DACCESS_COMPILE
void ReleaseReservedMemory(BYTE* value)
{
    if (value)
    {
        ExecutableAllocator::Instance()->Release(value);
    }
}
#endif // DACCESS_COMPILE

#ifdef _DEBUG
void LoaderHeapEvent::Describe(SString *pSString)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        DISABLED(NOTHROW);
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    pSString->AppendASCII("\n");

    {
        StackSString buf;
        if (m_allocationType == kFreedMem)
        {
            buf.Printf("    Freed at:         %s (line %d)\n", m_szFile, m_lineNum);
            buf.Printf("       (block originally allocated at %s (line %d)\n", m_szAllocFile, m_allocLineNum);
        }
        else
        {
            buf.Printf("    Allocated at:     %s (line %d)\n", m_szFile, m_lineNum);
        }
        pSString->Append(buf);
    }

    if (!QuietValidate())
    {
        pSString->AppendASCII("    *** THIS BLOCK HAS BEEN CORRUPTED ***\n");
    }



    {
        StackSString buf;
        buf.Printf("    Type:          ");
        switch (m_allocationType)
        {
            case kAllocMem:
                buf.AppendASCII("AllocMem()\n");
                break;
            case kFreedMem:
                buf.AppendASCII("Free\n");
                break;
            default:
                break;
        }
        pSString->Append(buf);
    }


    {
        StackSString buf;
        buf.Printf("    Start of block:       0x%p\n", m_pMem);
        pSString->Append(buf);
    }

    {
        StackSString buf;
        buf.Printf("    End of block:         0x%p\n", ((BYTE*)m_pMem) + m_dwSize - 1);
        pSString->Append(buf);
    }

    {
        StackSString buf;
        buf.Printf("    Requested size:       %lu (0x%lx)\n", (ULONG)m_dwRequestedSize, (ULONG)m_dwRequestedSize);
        pSString->Append(buf);
    }

    {
        StackSString buf;
        buf.Printf("    Actual size:          %lu (0x%lx)\n", (ULONG)m_dwSize, (ULONG)m_dwSize);
        pSString->Append(buf);
    }

    pSString->AppendASCII("\n");
}

BOOL LoaderHeapEvent::QuietValidate()
{
    WRAPPER_NO_CONTRACT;

    if (m_allocationType == kAllocMem)
    {
        LoaderHeapValidationTag *pTag = AllocMem_GetTag(m_pMem, m_dwRequestedSize);
        return (pTag->m_allocationType == m_allocationType && pTag->m_dwRequestedSize == m_dwRequestedSize);
    }
    else
    {
        // We can't easily validate freed blocks.
        return TRUE;
    }
}

/*static*/ DWORD LoaderHeapSniffer::InitDebugFlags()
{
    WRAPPER_NO_CONTRACT;

    DWORD dwDebugFlags = 0;
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_LoaderHeapCallTracing))
    {
        dwDebugFlags |= UnlockedLoaderHeap::kCallTracing;
    }
    return dwDebugFlags;
}

/*static*/ VOID LoaderHeapSniffer::RecordEvent(UnlockedLoaderHeap *pHeap,
                                               AllocationType allocationType,
                                               _In_ const char *szFile,
                                               int            lineNum,
                                               _In_ const char *szAllocFile,
                                               int            allocLineNum,
                                               void          *pMem,
                                               size_t         dwRequestedSize,
                                               size_t         dwSize
                                              )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;  //If we OOM in here, we just throw the event away.
    }
    CONTRACTL_END

    LoaderHeapEvent *pNewEvent;
    {
        {
            FAULT_NOT_FATAL();
            pNewEvent = new (nothrow) LoaderHeapEvent;
        }
        if (!pNewEvent)
        {
            if (!(pHeap->m_dwDebugFlags & pHeap->kEncounteredOOM))
            {
                pHeap->m_dwDebugFlags |= pHeap->kEncounteredOOM;
                _ASSERTE(!"LOADERHEAPSNIFFER: Failed allocation of LoaderHeapEvent. Call tracing information will be incomplete.");
            }
        }
        else
        {
            pNewEvent->m_allocationType     = allocationType;
            pNewEvent->m_szFile             = szFile;
            pNewEvent->m_lineNum            = lineNum;
            pNewEvent->m_szAllocFile        = szAllocFile;
            pNewEvent->m_allocLineNum       = allocLineNum;
            pNewEvent->m_pMem               = pMem;
            pNewEvent->m_dwRequestedSize    = dwRequestedSize;
            pNewEvent->m_dwSize             = dwSize;

            pNewEvent->m_pNext              = pHeap->m_pEventList;
            pHeap->m_pEventList             = pNewEvent;
        }
    }
}

/*static*/ VOID LoaderHeapSniffer::ClearEvents(UnlockedLoaderHeap *pHeap)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    LoaderHeapEvent *pEvent = pHeap->m_pEventList;
    while (pEvent)
    {
        LoaderHeapEvent *pNext = pEvent->m_pNext;
        delete pEvent;
        pEvent = pNext;
    }
    pHeap->m_pEventList = NULL;
}

/*static*/ VOID LoaderHeapSniffer::CompactEvents(UnlockedLoaderHeap *pHeap)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    LoaderHeapEvent **ppEvent = &(pHeap->m_pEventList);
    while (*ppEvent)
    {
        LoaderHeapEvent *pEvent = *ppEvent;
        if (pEvent->m_allocationType != kFreedMem)
        {
            ppEvent = &(pEvent->m_pNext);
        }
        else
        {
            LoaderHeapEvent **ppWalk = &(pEvent->m_pNext);
            BOOL fMatchFound = FALSE;
            while (*ppWalk && !fMatchFound)
            {
                LoaderHeapEvent *pWalk = *ppWalk;
                if (pWalk->m_allocationType  != kFreedMem &&
                    pWalk->m_pMem            == pEvent->m_pMem &&
                    pWalk->m_dwRequestedSize == pEvent->m_dwRequestedSize)
                {
                    // Delete matched pairs

                    // Order is important here - updating *ppWalk may change pEvent->m_pNext, and we want
                    // to get the updated value when we unlink pEvent.
                    *ppWalk = pWalk->m_pNext;
                    *ppEvent = pEvent->m_pNext;

                    delete pEvent;
                    delete pWalk;
                    fMatchFound = TRUE;
                }
                else
                {
                    ppWalk = &(pWalk->m_pNext);
                }
            }

            if (!fMatchFound)
            {
                ppEvent = &(pEvent->m_pNext);
            }
        }
    }
}

/*static*/ VOID LoaderHeapSniffer::PrintEvents(UnlockedLoaderHeap *pHeap)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    printf("\n------------- LoaderHeapEvents (in reverse time order!) --------------------");

    LoaderHeapEvent *pEvent = pHeap->m_pEventList;
    while (pEvent)
    {
        printf("\n");
        switch (pEvent->m_allocationType)
        {
            case kAllocMem:         printf("AllocMem        "); break;
            case kFreedMem:         printf("BackoutMem      "); break;

        }
        printf(" ptr = 0x%-8p", pEvent->m_pMem);
        printf(" rqsize = 0x%-8x", (DWORD)pEvent->m_dwRequestedSize);
        printf(" actsize = 0x%-8x", (DWORD)pEvent->m_dwSize);
        printf(" (at %s@%d)", pEvent->m_szFile, pEvent->m_lineNum);
        if (pEvent->m_allocationType == kFreedMem)
        {
            printf(" (original allocation at %s@%d)", pEvent->m_szAllocFile, pEvent->m_allocLineNum);
        }

        pEvent = pEvent->m_pNext;

    }
    printf("\n------------- End of LoaderHeapEvents --------------------------------------");
    printf("\n");

}


/*static*/ VOID LoaderHeapSniffer::PitchSniffer(SString *pSString)
{
    WRAPPER_NO_CONTRACT;
    pSString->AppendASCII("\n"
                        "\nBecause call-tracing wasn't turned on, we couldn't provide details about who last owned the affected memory block. To get more precise diagnostics,"
                        "\nset the following registry DWORD value:"
                        "\n"
                        "\n    HKLM\\Software\\Microsoft\\.NETFramework\\LoaderHeapCallTracing = 1"
                        "\n"
                        "\nand rerun the scenario that crashed."
                        "\n"
                        "\n");
}

/*static*/ LoaderHeapEvent *LoaderHeapSniffer::FindEvent(UnlockedLoaderHeap *pHeap, void *pAddr)
{
    LIMITED_METHOD_CONTRACT;

    LoaderHeapEvent *pEvent = pHeap->m_pEventList;
    while (pEvent)
    {
        if (pAddr >= pEvent->m_pMem && pAddr <= ( ((BYTE*)pEvent->m_pMem) + pEvent->m_dwSize - 1))
        {
            return pEvent;
        }
        pEvent = pEvent->m_pNext;
    }
    return NULL;

}
/*static*/
void LoaderHeapSniffer::ValidateFreeList(UnlockedLoaderHeap *pHeap)
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
            LoaderHeapEvent *pBadAddrEvent = FindEvent(pHeap, pBadAddr);

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

            LoaderHeapEvent *pPrevEvent = FindEvent(pHeap, ((BYTE*)pProbeThis) - 1);

            int count = 3;
            while (count-- &&
                   pPrevEvent != NULL &&
                   ( ((UINT_PTR)pProbeThis) - ((UINT_PTR)(pPrevEvent->m_pMem)) + pPrevEvent->m_dwSize ) < 1024)
            {
                message.AppendASCII("\nThis block is located close to the corruption point. ");
                if (!pHeap->IsInterleaved() && pPrevEvent->QuietValidate())
                {
                    message.AppendASCII("If it was overrun, it might have caused this.");
                }
                else
                {
                    message.AppendASCII("*** CORRUPTION DETECTED IN THIS BLOCK ***");
                }
                pPrevEvent->Describe(&message);
                pPrevEvent = FindEvent(pHeap, ((BYTE*)(pPrevEvent->m_pMem)) - 1);
            }


        }

        DbgAssertDialog(__FILE__, __LINE__, (char*) message.GetUTF8());
    }
}

/*static*/ void LoaderHeapSniffer::WeGotAFaultNowWhat(UnlockedLoaderHeap *pHeap)
{
    WRAPPER_NO_CONTRACT;
    ValidateFreeList(pHeap);

    //If none of the above popped up an assert, pop up a generic one.
    _ASSERTE(!("Unexpected AV inside LoaderHeap. The usual reason is that someone overwrote the end of a block or wrote into a freed block.\n"));

}

LoaderHeapValidationTag *AllocMem_GetTag(LPVOID pBlock, size_t dwRequestedSize)
{
    LIMITED_METHOD_CONTRACT;

    size_t dwSize = dwRequestedSize;
    dwSize += LOADER_HEAP_DEBUG_BOUNDARY;
    dwSize = ((dwSize + ALLOC_ALIGN_CONSTANT) & (~ALLOC_ALIGN_CONSTANT));
    return (LoaderHeapValidationTag *)( ((BYTE*)pBlock) + dwSize );
}

#endif // _DEBUG

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
        LoaderHeapSniffer::ValidateFreeList(pHeap);
        _ASSERTE(dwTotalSize >= pHeap->AllocMem_TotalSize(1));
    }

    if (!(0 == (dwTotalSize & ALLOC_ALIGN_CONSTANT)))
    {
        LoaderHeapSniffer::ValidateFreeList(pHeap);
        _ASSERTE(0 == (dwTotalSize & ALLOC_ALIGN_CONSTANT));
    }
#endif

#ifdef DEBUG
    if (!pHeap->IsInterleaved())
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
    else
    {
        memset((BYTE*)pMem + GetStubCodePageSize(), 0xcc, dwTotalSize);
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
#endif // DACCESS_COMPILE
