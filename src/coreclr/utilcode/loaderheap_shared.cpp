// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"                     // Precompiled header key.
#include "loaderheap.h"
#include "loaderheap_shared.h"
#include "ex.h"
#include "pedecoder.h"

#ifdef RANDOMIZE_ALLOC
RandomForLoaderHeap s_randomForLoaderHeap;
#endif

#ifndef DACCESS_COMPILE
INDEBUG(DWORD UnlockedLoaderHeapBase::s_dwNumInstancesOfLoaderHeaps = 0;)

UnlockedLoaderHeapBase::UnlockedLoaderHeapBase(LoaderHeapImplementationKind kind) : 
    m_kind(kind),
    m_dwTotalAlloc(0),
    m_pAllocPtr(NULL),
    m_pPtrToEndOfCommittedRegion(NULL)
#ifdef _DEBUG
    ,
    m_dwDebugFlags(LoaderHeapSniffer::InitDebugFlags()),
    m_pEventList(NULL),
    m_dwDebugWastedBytes(0)
#endif // _DEBUG
{
    LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
    s_dwNumInstancesOfLoaderHeaps++;
#endif
}

UnlockedLoaderHeapBase::~UnlockedLoaderHeapBase()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    INDEBUG(s_dwNumInstancesOfLoaderHeaps --;)
}


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

/*static*/ VOID LoaderHeapSniffer::RecordEvent(UnlockedLoaderHeapBase *pHeap,
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

/*static*/ VOID LoaderHeapSniffer::ClearEvents(UnlockedLoaderHeapBase *pHeap)
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

/*static*/ VOID LoaderHeapSniffer::CompactEvents(UnlockedLoaderHeapBase *pHeap)
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

/*static*/ VOID LoaderHeapSniffer::PrintEvents(UnlockedLoaderHeapBase *pHeap)
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
                        "\nset the following environment variable:"
                        "\n"
                        "\n    DOTNET_LoaderHeapCallTracing=1"
                        "\n"
                        "\nand rerun the scenario that crashed."
                        "\n"
                        "\n");
}

/*static*/ LoaderHeapEvent *LoaderHeapSniffer::FindEvent(UnlockedLoaderHeapBase *pHeap, void *pAddr)
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
size_t UnlockedLoaderHeapBase::GetBytesAvailCommittedRegion()
{
    LIMITED_METHOD_CONTRACT;
    
    if (m_pAllocPtr < m_pPtrToEndOfCommittedRegion)
    return (size_t)(m_pPtrToEndOfCommittedRegion - m_pAllocPtr);
    else
    return 0;
}
#endif // DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void UnlockedLoaderHeapBaseTraversable::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
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
        //   but it seems wasteful
        TADDR addr = dac_cast<TADDR>(block->pVirtualAddress);
        TSIZE_T size = block->dwVirtualSize;
        EMEM_OUT(("MEM: UnlockedLoaderHeap %p - %p\n", addr, addr + size));
        DacEnumMemoryRegion(addr, size, false);

        block = block->pNext;
    }
}

void UnlockedLoaderHeapBaseTraversable::EnumPageRegions (EnumPageRegionsCallback *pCallback, PTR_VOID pvArgs)
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
#endif // #ifdef DACCESS_COMPILE

#ifdef _DEBUG

void UnlockedLoaderHeapBase::UnlockedClearEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::ClearEvents(this);
}

void UnlockedLoaderHeapBase::UnlockedCompactEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::CompactEvents(this);
}

void UnlockedLoaderHeapBase::UnlockedPrintEvents()
{
    WRAPPER_NO_CONTRACT;
    LoaderHeapSniffer::PrintEvents(this);
}

#endif //_DEBUG
