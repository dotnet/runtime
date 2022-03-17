// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//


#include "common.h"
#include "dynamicmethod.h"
#include "object.h"
#include "method.hpp"
#include "comdelegate.h"
#include "field.h"
#include "contractimpl.h"
#include "nibblemapmacros.h"
#include "stringliteralmap.h"
#include "virtualcallstub.h"


#ifndef DACCESS_COMPILE

// get the method table for dynamic methods
DynamicMethodTable* DomainAssembly::GetDynamicMethodTable()
{
    CONTRACT (DynamicMethodTable*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(m_pDynamicMethodTable));
    }
    CONTRACT_END;

    if (!m_pDynamicMethodTable)
        DynamicMethodTable::CreateDynamicMethodTable(&m_pDynamicMethodTable, GetModule(), GetAppDomain());


    RETURN m_pDynamicMethodTable;
}

void ReleaseDynamicMethodTable(DynamicMethodTable *pDynMT)
{
    WRAPPER_NO_CONTRACT;
    if (pDynMT)
    {
        pDynMT->Destroy();
    }
}

void DynamicMethodTable::CreateDynamicMethodTable(DynamicMethodTable **ppLocation, Module *pModule, AppDomain *pDomain)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(ppLocation));
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(*ppLocation));
    }
    CONTRACT_END;

    AllocMemTracker amt;

    LoaderHeap* pHeap = pDomain->GetHighFrequencyHeap();
    _ASSERTE(pHeap);

    if (*ppLocation) RETURN;

    DynamicMethodTable* pDynMT = (DynamicMethodTable*)
            amt.Track(pHeap->AllocMem(S_SIZE_T(sizeof(DynamicMethodTable))));

    // Note: Memory allocated on loader heap is zero filled
    // memset((void*)pDynMT, 0, sizeof(DynamicMethodTable));

    if (*ppLocation) RETURN;

    LOG((LF_BCL, LL_INFO100, "Level2 - Creating DynamicMethodTable {0x%p}...\n", pDynMT));

    Holder<DynamicMethodTable*, DoNothing, ReleaseDynamicMethodTable> dynMTHolder(pDynMT);
    pDynMT->m_Crst.Init(CrstDynamicMT);
    pDynMT->m_Module = pModule;
    pDynMT->m_pDomain = pDomain;
    pDynMT->MakeMethodTable(&amt);

    if (*ppLocation) RETURN;

    if (FastInterlockCompareExchangePointer(ppLocation, pDynMT, NULL) != NULL)
    {
        LOG((LF_BCL, LL_INFO100, "Level2 - Another thread got here first - deleting DynamicMethodTable {0x%p}...\n", pDynMT));
        RETURN;
    }

    dynMTHolder.SuppressRelease();

    amt.SuppressRelease();
    LOG((LF_BCL, LL_INFO10, "Level1 - DynamicMethodTable created {0x%p}...\n", pDynMT));
    RETURN;
}

void DynamicMethodTable::MakeMethodTable(AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    m_pMethodTable = CreateMinimalMethodTable(m_Module, m_pDomain->GetHighFrequencyHeap(), pamTracker);
}

void DynamicMethodTable::Destroy()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#if _DEBUG
    // This method should be called only for collectible types or for non-collectible ones
    // at the construction time when there are no DynamicMethodDesc instances added to the
    // DynamicMethodTable yet (from the DynamicMethodTable::CreateDynamicMethodTable in case
    // there were two threads racing to construct the instance for the thread that lost
    // the race)
    if (m_pMethodTable != NULL && !m_pMethodTable->GetLoaderAllocator()->IsCollectible())
    {
        MethodTable::IntroducedMethodIterator it(m_pMethodTable);
        _ASSERTE(!it.IsValid());
    }
#endif

    m_Crst.Destroy();
    LOG((LF_BCL, LL_INFO10, "Level1 - DynamicMethodTable destroyed {0x%p}\n", this));
}

void DynamicMethodTable::AddMethodsToList()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACT_END;

    AllocMemTracker amt;

    LoaderHeap* pHeap = m_pMethodTable->GetLoaderAllocator()->GetHighFrequencyHeap();
    _ASSERTE(pHeap);

    //
    // allocate as many chunks as needed to hold the methods
    //
    MethodDescChunk* pChunk = MethodDescChunk::CreateChunk(pHeap, 0 /* one chunk of maximum size */,
        mcDynamic, TRUE /* fNonVtableSlot */, TRUE /* fNativeCodeSlot */, FALSE /* fComPlusCallInfo */, m_pMethodTable, &amt);
    if (m_DynamicMethodList) RETURN;

    int methodCount = pChunk->GetCount();

    BYTE* pResolvers = (BYTE*)amt.Track(pHeap->AllocMem(S_SIZE_T(sizeof(LCGMethodResolver)) * S_SIZE_T(methodCount)));
    if (m_DynamicMethodList) RETURN;

    DynamicMethodDesc *pNewMD = (DynamicMethodDesc *)pChunk->GetFirstMethodDesc();
    DynamicMethodDesc *pPrevMD = NULL;
    // now go through all the methods in the chunk and link them
    for(int i = 0; i < methodCount; i++)
    {
        _ASSERTE(pNewMD->GetClassification() == mcDynamic);

        pNewMD->SetMemberDef(0);
        pNewMD->SetSlot(MethodTable::NO_SLOT);       // we can't ever use the slot for dynamic methods
        pNewMD->SetStatic();
        pNewMD->InitializeFlags(DynamicMethodDesc::FlagPublic
                        | DynamicMethodDesc::FlagStatic
                        | DynamicMethodDesc::FlagIsLCGMethod);

        LCGMethodResolver* pResolver = new (pResolvers) LCGMethodResolver();
        pResolver->m_pDynamicMethod = pNewMD;
        pResolver->m_DynamicMethodTable = this;
        pNewMD->m_pResolver = pResolver;

        pNewMD->SetTemporaryEntryPoint(m_pDomain->GetLoaderAllocator(), &amt);

#ifdef _DEBUG
        pNewMD->m_pDebugMethodTable = m_pMethodTable;
#endif

        if (pPrevMD)
        {
            pPrevMD->GetLCGMethodResolver()->m_next = pNewMD;
        }
        pPrevMD = pNewMD;
        pNewMD = (DynamicMethodDesc *)(dac_cast<TADDR>(pNewMD) + pNewMD->SizeOf());

        pResolvers += sizeof(LCGMethodResolver);
    }

    if (m_DynamicMethodList) RETURN;

    {
        // publish method list and method table
        LockHolder lh(this);
        if (m_DynamicMethodList) RETURN;

        // publish the new method descs on the method table
        m_pMethodTable->GetClass()->AddChunk(pChunk);
        m_DynamicMethodList = (DynamicMethodDesc*)pChunk->GetFirstMethodDesc();
    }

    amt.SuppressRelease();
}

DynamicMethodDesc* DynamicMethodTable::GetDynamicMethod(BYTE *psig, DWORD sigSize, PTR_CUTF8 name)
{
    CONTRACT (DynamicMethodDesc*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(psig));
        PRECONDITION(sigSize > 0);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    LOG((LF_BCL, LL_INFO10000, "Level4 - Getting DynamicMethod\n"));

    DynamicMethodDesc *pNewMD = NULL;

    for (;;)
    {
        {
            LockHolder lh(this);
            pNewMD = m_DynamicMethodList;
            if (pNewMD)
            {
                m_DynamicMethodList = pNewMD->GetLCGMethodResolver()->m_next;
#ifdef _DEBUG
                m_Used++;
#endif
                break;
            }
        }

        LOG((LF_BCL, LL_INFO1000, "Level4 - DynamicMethod unavailable\n"));

        // need to create more methoddescs
        AddMethodsToList();
    }
    _ASSERTE(pNewMD != NULL);

    // Reset the method desc into pristine state

    // Note: Reset has THROWS contract since it may allocate jump stub. It will never throw here
    // since it will always reuse the existing jump stub.
    pNewMD->Reset();

    LOG((LF_BCL, LL_INFO1000, "Level3 - DynamicMethod obtained {0x%p} (used %d)\n", pNewMD, m_Used));

    // the store sig part of the method desc
    pNewMD->SetStoredMethodSig((PCCOR_SIGNATURE)psig, sigSize);
    // the dynamic part of the method desc
    pNewMD->m_pszMethodName = name;
    pNewMD->InitializeFlags(DynamicMethodDesc::FlagPublic
                    | DynamicMethodDesc::FlagStatic
                    | DynamicMethodDesc::FlagIsLCGMethod);

#ifdef _DEBUG
    pNewMD->m_pszDebugMethodName = name;
    pNewMD->m_pszDebugClassName  = (LPUTF8)"dynamicclass";
    pNewMD->m_pszDebugMethodSignature = "DynamicMethod Signature not available";
#endif // _DEBUG

#ifdef HAVE_GCCOVER
    pNewMD->m_GcCover = NULL;
#endif

    pNewMD->SetNotInline(TRUE);
    pNewMD->GetLCGMethodResolver()->Reset();

    RETURN pNewMD;
}

void DynamicMethodTable::LinkMethod(DynamicMethodDesc *pMethod)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMethod));
    }
    CONTRACT_END;

    LOG((LF_BCL, LL_INFO10000, "Level4 - Returning DynamicMethod to free list {0x%p} (used %d)\n", pMethod, m_Used));
    {
        LockHolder lh(this);
        pMethod->GetLCGMethodResolver()->m_next = m_DynamicMethodList;
        m_DynamicMethodList = pMethod;
#ifdef _DEBUG
        m_Used--;
#endif
    }

    RETURN;
}


//
// CodeHeap implementation
//
HeapList* HostCodeHeap::CreateCodeHeap(CodeHeapRequestInfo *pInfo, EEJitManager *pJitManager)
{
    CONTRACT (HeapList*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION((RETVAL != NULL) || !pInfo->getThrowOnOutOfMemoryWithinRange());
    }
    CONTRACT_END;

    NewHolder<HostCodeHeap> pCodeHeap(new HostCodeHeap(pJitManager));

    HeapList *pHp = pCodeHeap->InitializeHeapList(pInfo);
    if (pHp == NULL)
    {
        _ASSERTE(!pInfo->getThrowOnOutOfMemoryWithinRange());
        RETURN NULL;
    }

    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap creation {0x%p} - base addr 0x%p, size available 0x%p, nibble map ptr 0x%p\n",
                            (HostCodeHeap*)pCodeHeap, pCodeHeap->m_pBaseAddr, pCodeHeap->m_TotalBytesAvailable, pCodeHeap->m_pHeapList->pHdrMap));

    pCodeHeap.SuppressRelease();

    LOG((LF_BCL, LL_INFO10, "Level1 - CodeHeap created {0x%p}\n", (HostCodeHeap*)pCodeHeap));
    RETURN pHp;
}

HostCodeHeap::HostCodeHeap(EEJitManager *pJitManager)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    m_pBaseAddr = NULL;
    m_pLastAvailableCommittedAddr = NULL;
    m_TotalBytesAvailable = 0;
    m_ApproximateLargestBlock = 0;
    m_AllocationCount = 0;
    m_pHeapList = NULL;
    m_pJitManager = (PTR_EEJitManager)pJitManager;
    m_pFreeList = NULL;
    m_pAllocator = NULL;
    m_pNextHeapToRelease = NULL;
}

HostCodeHeap::~HostCodeHeap()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pHeapList != NULL && m_pHeapList->pHdrMap != NULL)
        delete[] m_pHeapList->pHdrMap;

    if (m_pBaseAddr)
        ExecutableAllocator::Instance()->Release(m_pBaseAddr);
    LOG((LF_BCL, LL_INFO10, "Level1 - CodeHeap destroyed {0x%p}\n", this));
}

HeapList* HostCodeHeap::InitializeHeapList(CodeHeapRequestInfo *pInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    size_t ReserveBlockSize = pInfo->getRequestSize();

    // Add TrackAllocation, HeapList and very conservative padding to make sure we have enough for the allocation
    ReserveBlockSize += sizeof(TrackAllocation) + HOST_CODEHEAP_SIZE_ALIGN + 0x100;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
    ReserveBlockSize += JUMP_ALLOCATE_SIZE;
#endif

    // reserve ReserveBlockSize rounded-up to VIRTUAL_ALLOC_RESERVE_GRANULARITY of memory
    ReserveBlockSize = ALIGN_UP(ReserveBlockSize, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

    if (pInfo->m_loAddr != NULL || pInfo->m_hiAddr != NULL)
    {
        m_pBaseAddr = (BYTE*)ExecutableAllocator::Instance()->ReserveWithinRange(ReserveBlockSize, pInfo->m_loAddr, pInfo->m_hiAddr);
        if (!m_pBaseAddr)
        {
            if (pInfo->getThrowOnOutOfMemoryWithinRange())
                ThrowOutOfMemoryWithinRange();
            return NULL;
        }
    }
    else
    {
        // top up the ReserveBlockSize to suggested minimum
        ReserveBlockSize = max(ReserveBlockSize, pInfo->getReserveSize());

        m_pBaseAddr = (BYTE*)ExecutableAllocator::Instance()->Reserve(ReserveBlockSize);
        if (!m_pBaseAddr)
            ThrowOutOfMemory();
    }

    m_pLastAvailableCommittedAddr = m_pBaseAddr;
    m_TotalBytesAvailable = ReserveBlockSize;
    m_ApproximateLargestBlock = ReserveBlockSize;
    m_pAllocator = pInfo->m_pAllocator;

    HeapList* pHp = new HeapList;

    TrackAllocation *pTracker = NULL;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)

    pTracker = AllocMemory_NoThrow(0, JUMP_ALLOCATE_SIZE, sizeof(void*), 0);
    if (pTracker == NULL)
    {
        // This should only ever happen with fault injection
        _ASSERTE(g_pConfig->ShouldInjectFault(INJECTFAULT_DYNAMICCODEHEAP));
        delete pHp;
        ThrowOutOfMemory();
    }

    pHp->CLRPersonalityRoutine = (BYTE *)(pTracker + 1);
#endif

    pHp->hpNext = NULL;
    pHp->pHeap = (PTR_CodeHeap)this;
    // wire it back
    m_pHeapList = (PTR_HeapList)pHp;

    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap creation {0x%p} - size available 0x%p, private data ptr [0x%p, 0x%p]\n",
        (HostCodeHeap*)this, m_TotalBytesAvailable, pTracker, pTracker->size));

    // It is important to exclude the CLRPersonalityRoutine from the tracked range
    pHp->startAddress = dac_cast<TADDR>(m_pBaseAddr) + (pTracker ? pTracker->size : 0);
    pHp->mapBase = ROUND_DOWN_TO_PAGE(pHp->startAddress);  // round down to next lower page align
    pHp->pHdrMap = NULL;
    pHp->endAddress = pHp->startAddress;

    pHp->maxCodeHeapSize = m_TotalBytesAvailable - (pTracker ? pTracker->size : 0);
    pHp->reserveForJumpStubs = 0;

#ifdef HOST_64BIT
    ExecutableWriterHolder<BYTE> personalityRoutineWriterHolder(pHp->CLRPersonalityRoutine, 12);
    emitJump(pHp->CLRPersonalityRoutine, personalityRoutineWriterHolder.GetRW(), (void *)ProcessCLRException);
#endif

    size_t nibbleMapSize = HEAP2MAPSIZE(ROUND_UP_TO_PAGE(pHp->maxCodeHeapSize));
    pHp->pHdrMap = new DWORD[nibbleMapSize / sizeof(DWORD)];
    ZeroMemory(pHp->pHdrMap, nibbleMapSize);

    return pHp;
}

HostCodeHeap::TrackAllocation* HostCodeHeap::AllocFromFreeList(size_t header, size_t size, DWORD alignment, size_t reserveForJumpStubs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pFreeList)
    {
        LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Alloc size corrected 0x%X for free list\n", this, size));
        // walk the list looking for a block with enough capacity
        TrackAllocation *pCurrent = m_pFreeList;
        TrackAllocation *pPrevious = NULL;
        while (pCurrent)
        {
            BYTE* pPointer = ALIGN_UP((BYTE*)(pCurrent + 1) + header, alignment);
            size_t realSize = ALIGN_UP(pPointer + size, sizeof(void*)) - (BYTE*)pCurrent;
            if (pCurrent->size >= realSize + reserveForJumpStubs)
            {
                // found a block
                LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Block found, size 0x%X\n", this, pCurrent->size));

                ExecutableWriterHolderNoLog<TrackAllocation> previousWriterHolder;
                if (pPrevious)
                {
                    previousWriterHolder.AssignExecutableWriterHolder(pPrevious, sizeof(TrackAllocation));
                }

                ExecutableWriterHolder<TrackAllocation> currentWriterHolder(pCurrent, sizeof(TrackAllocation));

                // The space left is not big enough for a new block, let's just
                // update the TrackAllocation record for the current block
                if (pCurrent->size - realSize < max(HOST_CODEHEAP_SIZE_ALIGN, sizeof(TrackAllocation)))
                {
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Item removed %p, size 0x%X\n", this, pCurrent, pCurrent->size));
                    // remove current
                    if (pPrevious)
                    {
                        previousWriterHolder.GetRW()->pNext = pCurrent->pNext;
                    }
                    else
                    {
                        m_pFreeList = pCurrent->pNext;
                    }
                }
                else
                {
                    // create a new TrackAllocation after the memory we just allocated and insert it into the free list
                    TrackAllocation *pNewCurrent = (TrackAllocation*)((BYTE*)pCurrent + realSize);

                    ExecutableWriterHolder<TrackAllocation> newCurrentWriterHolder(pNewCurrent, sizeof(TrackAllocation));
                    newCurrentWriterHolder.GetRW()->pNext = pCurrent->pNext;
                    newCurrentWriterHolder.GetRW()->size = pCurrent->size - realSize;

                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Item changed %p, new size 0x%X\n", this, pNewCurrent, pNewCurrent->size));
                    if (pPrevious)
                    {
                        previousWriterHolder.GetRW()->pNext = pNewCurrent;
                    }
                    else
                    {
                        m_pFreeList = pNewCurrent;
                    }

                    // We only need to update the size of the current block if we are creating a new block
                    currentWriterHolder.GetRW()->size = realSize;
                }

                currentWriterHolder.GetRW()->pHeap = this;

                LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Allocation returned %p, size 0x%X - data -> %p\n", this, pCurrent, pCurrent->size, pPointer));
                return pCurrent;
            }
            pPrevious = pCurrent;
            pCurrent = pCurrent->pNext;
        }
    }
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - No block in free list for size 0x%X\n", this, size));
    return NULL;
}

void HostCodeHeap::AddToFreeList(TrackAllocation *pBlockToInsert, TrackAllocation *pBlockToInsertRW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Add to FreeList [%p, 0x%X]\n", this, pBlockToInsert, pBlockToInsert->size));

    // append to the list in the proper position and coalesce if needed
    if (m_pFreeList)
    {
        TrackAllocation *pCurrent = m_pFreeList;
        TrackAllocation *pPrevious = NULL;
        while (pCurrent)
        {
            if (pCurrent > pBlockToInsert)
            {
                // found the point of insertion
                pBlockToInsertRW->pNext = pCurrent;
                ExecutableWriterHolderNoLog<TrackAllocation> previousWriterHolder;

                if (pPrevious)
                {
                    previousWriterHolder.AssignExecutableWriterHolder(pPrevious, sizeof(TrackAllocation));
                    previousWriterHolder.GetRW()->pNext = pBlockToInsert;
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Insert block [%p, 0x%X] -> [%p, 0x%X] -> [%p, 0x%X]\n", this,
                                                                        pPrevious, pPrevious->size,
                                                                        pBlockToInsert, pBlockToInsert->size,
                                                                        pCurrent, pCurrent->size));
                }
                else
                {
                    m_pFreeList = pBlockToInsert;
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Insert block [%p, 0x%X] to head\n", this, pBlockToInsert, pBlockToInsert->size));
                }

                // check for coalescing
                if ((BYTE*)pBlockToInsert + pBlockToInsert->size == (BYTE*)pCurrent)
                {
                    // coalesce with next
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Coalesce block [%p, 0x%X] with [%p, 0x%X] - new size 0x%X\n", this,
                                                                        pBlockToInsert, pBlockToInsert->size,
                                                                        pCurrent, pCurrent->size,
                                                                        pCurrent->size + pBlockToInsert->size));
                    pBlockToInsertRW->pNext = pCurrent->pNext;
                    pBlockToInsertRW->size += pCurrent->size;
                }

                if (pPrevious && (BYTE*)pPrevious + pPrevious->size == (BYTE*)pBlockToInsert)
                {
                    // coalesce with previous
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Coalesce block [%p, 0x%X] with [%p, 0x%X] - new size 0x%X\n", this,
                                                                        pPrevious, pPrevious->size,
                                                                        pBlockToInsert, pBlockToInsert->size,
                                                                        pPrevious->size + pBlockToInsert->size));
                    previousWriterHolder.GetRW()->pNext = pBlockToInsert->pNext;
                    previousWriterHolder.GetRW()->size += pBlockToInsert->size;
                }

                return;
            }
            pPrevious = pCurrent;
            pCurrent = pCurrent->pNext;
        }
        _ASSERTE(pPrevious && pCurrent == NULL);
        pBlockToInsertRW->pNext = NULL;
        // last in the list
        ExecutableWriterHolder<TrackAllocation> previousWriterHolder2(pPrevious, sizeof(TrackAllocation));

        if ((BYTE*)pPrevious + pPrevious->size == (BYTE*)pBlockToInsert)
        {
            // coalesce with previous
            LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Coalesce block [%p, 0x%X] with [%p, 0x%X] - new size 0x%X\n", this,
                                                                pPrevious, pPrevious->size,
                                                                pBlockToInsert, pBlockToInsert->size,
                                                                pPrevious->size + pBlockToInsert->size));
            previousWriterHolder2.GetRW()->size += pBlockToInsert->size;
        }
        else
        {
            previousWriterHolder2.GetRW()->pNext = pBlockToInsert;
            LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Insert block [%p, 0x%X] to end after [%p, 0x%X]\n", this,
                                                                pBlockToInsert, pBlockToInsert->size,
                                                                pPrevious, pPrevious->size));
        }

        return;

    }
    // first in the list
    pBlockToInsertRW->pNext = m_pFreeList;
    m_pFreeList = pBlockToInsert;
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Insert block [%p, 0x%X] to head\n", this,
                                                        m_pFreeList, m_pFreeList->size));
}

void* HostCodeHeap::AllocMemForCode_NoThrow(size_t header, size_t size, DWORD alignment, size_t reserveForJumpStubs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(header == sizeof(CodeHeader));
    _ASSERTE(alignment <= HOST_CODEHEAP_SIZE_ALIGN);

    // The code allocator has to guarantee that there is only one entrypoint per nibble map entry.
    // It is guaranteed because of HostCodeHeap allocator always aligns the size up to HOST_CODEHEAP_SIZE_ALIGN,
    // and because the size of nibble map entries (BYTES_PER_BUCKET) is smaller than HOST_CODEHEAP_SIZE_ALIGN.
    // Assert the later fact here.
    _ASSERTE(HOST_CODEHEAP_SIZE_ALIGN >= BYTES_PER_BUCKET);

    header += sizeof(TrackAllocation*);

    TrackAllocation* pTracker = AllocMemory_NoThrow(header, size, alignment, reserveForJumpStubs);
    if (pTracker == NULL)
        return NULL;

    BYTE * pCode = ALIGN_UP((BYTE*)(pTracker + 1) + header, alignment);

    // Pointer to the TrackAllocation record is stored just before the code header
    CodeHeader * pHdr = (CodeHeader *)pCode - 1;
    ExecutableWriterHolder<TrackAllocation *> trackerWriterHolder((TrackAllocation **)(pHdr) - 1, sizeof(TrackAllocation *));
    *trackerWriterHolder.GetRW() = pTracker;

    _ASSERTE(pCode + size <= (BYTE*)pTracker + pTracker->size);

    // ref count the whole heap
    m_AllocationCount++;
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - ref count %d\n", this, m_AllocationCount));

    return pCode;
}

HostCodeHeap::TrackAllocation* HostCodeHeap::AllocMemory_NoThrow(size_t header, size_t size, DWORD alignment, size_t reserveForJumpStubs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    if (g_pConfig->ShouldInjectFault(INJECTFAULT_DYNAMICCODEHEAP))
    {
        char *a = new (nothrow) char;
        if (a == NULL)
            return NULL;
        delete a;
    }
#endif // _DEBUG

    // Skip walking the free list if the cached size of the largest block is not enough
    size_t totalRequiredSize = ALIGN_UP(sizeof(TrackAllocation) + header + size + (alignment - 1) + reserveForJumpStubs, sizeof(void*));
    if (totalRequiredSize > m_ApproximateLargestBlock)
        return NULL;

    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Allocation requested 0x%X\n", this, size));

    TrackAllocation* pTracker = AllocFromFreeList(header, size, alignment, reserveForJumpStubs);
    if (!pTracker)
    {
        // walk free list to end to find available space
        size_t availableInFreeList = 0;
        TrackAllocation *pCurrentBlock = m_pFreeList;
        TrackAllocation *pLastBlock = NULL;
        while (pCurrentBlock)
        {
            pLastBlock = pCurrentBlock;
            pCurrentBlock = pCurrentBlock->pNext;
        }
        if (pLastBlock && (BYTE*)pLastBlock + pLastBlock->size == m_pLastAvailableCommittedAddr)
        {
            availableInFreeList = pLastBlock->size;
        }

        _ASSERTE(totalRequiredSize > availableInFreeList);
        size_t sizeToCommit = totalRequiredSize - availableInFreeList;
        sizeToCommit = ROUND_UP_TO_PAGE(sizeToCommit);

        if (m_pLastAvailableCommittedAddr + sizeToCommit <= m_pBaseAddr + m_TotalBytesAvailable)
        {
            if (NULL == ExecutableAllocator::Instance()->Commit(m_pLastAvailableCommittedAddr, sizeToCommit, true /* isExecutable */))
            {
                LOG((LF_BCL, LL_ERROR, "CodeHeap [0x%p] - VirtualAlloc failed\n", this));
                return NULL;
            }

            TrackAllocation *pBlockToInsert = (TrackAllocation*)(void*)m_pLastAvailableCommittedAddr;
            ExecutableWriterHolder<TrackAllocation> blockToInsertWriterHolder(pBlockToInsert, sizeof(TrackAllocation));

            blockToInsertWriterHolder.GetRW()->pNext = NULL;
            blockToInsertWriterHolder.GetRW()->size = sizeToCommit;
            m_pLastAvailableCommittedAddr += sizeToCommit;
            AddToFreeList(pBlockToInsert, blockToInsertWriterHolder.GetRW());
            pTracker = AllocFromFreeList(header, size, alignment, reserveForJumpStubs);
            _ASSERTE(pTracker != NULL);
        }
        else
        {
            LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - allocation failed:\n\tm_pLastAvailableCommittedAddr: 0x%X\n\tsizeToCommit: 0x%X\n\tm_pBaseAddr: 0x%X\n\tm_TotalBytesAvailable: 0x%X\n", this, m_pLastAvailableCommittedAddr, sizeToCommit, m_pBaseAddr, m_TotalBytesAvailable));
            // Update largest available block size
            m_ApproximateLargestBlock = totalRequiredSize - 1;
        }
    }

    return pTracker;
}

#endif //!DACCESS_COMPILE

#ifdef DACCESS_COMPILE
void HostCodeHeap::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    DAC_ENUM_DTHIS();

    TADDR addr = dac_cast<TADDR>(m_pBaseAddr);
    size_t size = dac_cast<TADDR>(m_pLastAvailableCommittedAddr) - addr;

#if (_DEBUG)
    // Test hook: when testing on debug builds, we want an easy way to test that the while
    // correctly terminates in the face of ridiculous stuff from the target.
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DumpGeneration_IntentionallyCorruptDataFromTarget) == 1)
    {
        // Pretend the object is very large.
        size |= 0xf0000000;
    }
#endif // (_DEBUG)

    while (size)
    {
        ULONG32 enumSize;

        if (size > 0x80000000)
        {
            enumSize = 0x80000000;
        }
        else
        {
            enumSize = (ULONG32)size;
        }

        // If we can't read the target memory, stop immediately so we don't work
        // with broken data.
        if (!DacEnumMemoryRegion(addr, enumSize))
            break;

        addr += enumSize;
        size -= enumSize;
    }
}
#endif // DACCESS_COMPILE

// static
struct HostCodeHeap::TrackAllocation * HostCodeHeap::GetTrackAllocation(TADDR codeStart)
{
    LIMITED_METHOD_CONTRACT;

    CodeHeader * pHdr = dac_cast<PTR_CodeHeader>(PCODEToPINSTR(codeStart)) - 1;

    // Pointer to the TrackAllocation record is stored just before the code header
    return *((TrackAllocation **)(pHdr) - 1);
}

HostCodeHeap* HostCodeHeap::GetCodeHeap(TADDR codeStart)
{
    WRAPPER_NO_CONTRACT;
    return HostCodeHeap::GetTrackAllocation(codeStart)->pHeap;
}


#ifndef DACCESS_COMPILE

void HostCodeHeap::FreeMemForCode(void * codeStart)
{
    LIMITED_METHOD_CONTRACT;

    TrackAllocation *pTracker = HostCodeHeap::GetTrackAllocation((TADDR)codeStart);
    ExecutableWriterHolder<TrackAllocation> trackerWriterHolder(pTracker, sizeof(TrackAllocation));
    AddToFreeList(pTracker, trackerWriterHolder.GetRW());

    m_ApproximateLargestBlock += pTracker->size;

    m_AllocationCount--;
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap released [0x%p, vt(0x%x)] - ref count %d\n", this, *(size_t*)this, m_AllocationCount));

    if (m_AllocationCount == 0)
    {
        m_pJitManager->AddToCleanupList(this);
    }
}

//
// Implementation for DynamicMethodDesc declared in method.hpp
//
void DynamicMethodDesc::Destroy()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(IsDynamicMethod());
    LoaderAllocator *pLoaderAllocator = GetLoaderAllocator();
    LOG((LF_BCL, LL_INFO1000, "Level3 - Destroying DynamicMethod {0x%p}\n", this));

    // The m_pSig and m_pszMethodName need to be destroyed after the GetLCGMethodResolver()->Destroy() call
    // otherwise the EEJitManager::CodeHeapIterator could return DynamicMethodDesc with these members NULLed, but
    // the nibble map for the corresponding code memory indicating that this DynamicMethodDesc is still alive.
    PCODE pSig = m_pSig;
    PTR_CUTF8 pszMethodName = m_pszMethodName;

    GetLCGMethodResolver()->Destroy();
    // The current DynamicMethodDesc storage is destroyed at this point

    if (pszMethodName != NULL)
    {
        delete[] pszMethodName;
    }

    if (pSig != NULL)
    {
        delete[] (BYTE*)pSig;
    }

    if (pLoaderAllocator->IsCollectible())
    {
        if (pLoaderAllocator->Release())
        {
            GCX_PREEMP();
            LoaderAllocator::GCLoaderAllocators(pLoaderAllocator);
        }
    }
}

//
// The resolver object is reused when the method is destroyed,
// this will reset its state for the next use.
//
void LCGMethodResolver::Reset()
{
    m_DynamicStringLiterals = NULL;
    m_recordCodePointer     = NULL;
    m_UsedIndCellList       = NULL;
    m_pJumpStubCache        = NULL;
    m_next                  = NULL;
    m_Code                  = NULL;
}

//
// Recycle all the indcells in m_UsedIndCellList by adding them to the free list
//
void LCGMethodResolver::RecycleIndCells()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    // Append the list of indirection cells used by this dynamic method to the free list
    IndCellList * list = m_UsedIndCellList;
    if (list)
    {
        BYTE * cellhead = list->indcell;
        BYTE * cellprev = NULL;
        BYTE * cellcurr = NULL;

        // Build a linked list of indirection cells from m_UsedIndCellList.
        // No need to lock newlist because this method is only called during the finalization of
        // DynamicResolver.DestroyScout and at that time no one else should be modifying m_UsedIndCellList.
        while (list)
        {
            cellcurr = list->indcell;
            _ASSERTE(cellcurr != NULL);

            if (cellprev)
                *((BYTE**)cellprev) = cellcurr;

            list = list->pNext;
            cellprev = cellcurr;
        }

        // Insert the linked list to the free list of the VirtualCallStubManager of the current domain.
        // We should use GetLoaderAllocator because that is where the ind cell was allocated.
        LoaderAllocator *pLoaderAllocator = GetDynamicMethod()->GetLoaderAllocator();
        VirtualCallStubManager *pMgr = pLoaderAllocator->GetVirtualCallStubManager();
        pMgr->InsertIntoRecycledIndCellList_Locked(cellhead, cellcurr);
        m_UsedIndCellList = NULL;
    }
}

void LCGMethodResolver::Destroy()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    LOG((LF_BCL, LL_INFO100, "Level2 - Resolver - Destroying Resolver {0x%p}\n", this));
    if (m_Code)
    {
        delete[] m_Code;
        m_Code = NULL;
    }
    m_CodeSize = 0;
    if (!m_LocalSig.IsNull())
    {
        delete[] m_LocalSig.GetPtr();
        m_LocalSig = SigPointer();
    }

    // Get the global string literal interning map
    GlobalStringLiteralMap* pStringLiteralMap = SystemDomain::GetGlobalStringLiteralMapNoCreate();

    // release references to all the string literals used in this Dynamic Method
    if (pStringLiteralMap != NULL)
    {
        // lock the global string literal interning map
        // we cannot use GetGlobalStringLiteralMap() here because it might throw
        CrstHolder gch(pStringLiteralMap->GetHashTableCrstGlobal());

        // Access to m_DynamicStringLiterals doesn't need to be syncrhonized because
        // this can be run in only one thread: the finalizer thread.
        while (m_DynamicStringLiterals != NULL)
        {
            m_DynamicStringLiterals->m_pEntry->Release();
            m_DynamicStringLiterals = m_DynamicStringLiterals->m_pNext;
        }
    }

    // Note that we need to do this before m_jitTempData is deleted
    RecycleIndCells();

    m_jitMetaHeap.Delete();
    m_jitTempData.Delete();


    if (m_recordCodePointer)
    {
#if defined(TARGET_AMD64)
        // Remove the unwind information (if applicable)
        UnwindInfoTable::UnpublishUnwindInfoForMethod((TADDR)m_recordCodePointer);
#endif // defined(TARGET_AMD64)

        HostCodeHeap *pHeap = HostCodeHeap::GetCodeHeap((TADDR)m_recordCodePointer);
        LOG((LF_BCL, LL_INFO1000, "Level3 - Resolver {0x%p} - Release reference to heap {%p, vt(0x%x)} \n", this, pHeap, *(size_t*)pHeap));
        pHeap->m_pJitManager->FreeCodeMemory(pHeap, m_recordCodePointer);

        m_recordCodePointer = NULL;
    }

    if (m_pJumpStubCache != NULL)
    {
        JumpStubBlockHeader* current = m_pJumpStubCache->m_pBlocks;
        while (current)
        {
            JumpStubBlockHeader* next = current->m_next;

            HostCodeHeap *pHeap = current->GetHostCodeHeap();
            LOG((LF_BCL, LL_INFO1000, "Level3 - Resolver {0x%p} - Release reference to heap {%p, vt(0x%x)} \n", current, pHeap, *(size_t*)pHeap));
            pHeap->m_pJitManager->FreeCodeMemory(pHeap, current);

            current = next;
        }
        m_pJumpStubCache->m_pBlocks = NULL;

        delete m_pJumpStubCache;
        m_pJumpStubCache = NULL;
    }

    if (m_managedResolver)
    {
        ::DestroyLongWeakHandle(m_managedResolver);
        m_managedResolver = NULL;
    }

    m_DynamicMethodTable->LinkMethod(m_pDynamicMethod);
}

void LCGMethodResolver::FreeCompileTimeState()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    //m_jitTempData.Delete();
}



void LCGMethodResolver::GetJitContext(SecurityControlFlags * securityControlFlags,
                                      TypeHandle *typeOwner)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(securityControlFlags));
        PRECONDITION(CheckPointer(typeOwner));
    } CONTRACTL_END;

    GCX_COOP();

    MethodDescCallSite getJitContext(METHOD__RESOLVER__GET_JIT_CONTEXT, m_managedResolver);

    OBJECTREF resolver = ObjectFromHandle(m_managedResolver);
    _ASSERTE(resolver); // gc root must be up the stack

    ARG_SLOT args[] =
    {
        ObjToArgSlot(resolver),
        PtrToArgSlot(securityControlFlags),
    };

    REFLECTCLASSBASEREF refType = (REFLECTCLASSBASEREF)getJitContext.Call_RetOBJECTREF(args);
    *typeOwner = refType != NULL ? refType->GetType() : TypeHandle();

}

ChunkAllocator* LCGMethodResolver::GetJitMetaHeap()
{
    LIMITED_METHOD_CONTRACT;
    return &m_jitMetaHeap;
}

BYTE* LCGMethodResolver::GetCodeInfo(unsigned *pCodeSize, unsigned *pStackSize, CorInfoOptions *pOptions, unsigned *pEHSize)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pCodeSize);

    if (!m_Code)
    {
        GCX_COOP();

        LOG((LF_BCL, LL_INFO100000, "Level5 - DM-JIT: Getting CodeInfo on resolver 0x%p...\n", this));
        // get the code - Byte[] Resolver.GetCodeInfo(ref ushort stackSize, ref int EHCount)
        MethodDescCallSite getCodeInfo(METHOD__RESOLVER__GET_CODE_INFO, m_managedResolver);

        OBJECTREF resolver = ObjectFromHandle(m_managedResolver);
        VALIDATEOBJECTREF(resolver); // gc root must be up the stack

        int32_t stackSize = 0, initLocals = 0, EHSize = 0;
        ARG_SLOT args[] =
        {
            ObjToArgSlot(resolver),
            PtrToArgSlot(&stackSize),
            PtrToArgSlot(&initLocals),
            PtrToArgSlot(&EHSize),
        };
        U1ARRAYREF dataArray = (U1ARRAYREF) getCodeInfo.Call_RetOBJECTREF(args);
        DWORD codeSize = dataArray->GetNumComponents();
        NewArrayHolder<BYTE> code(new BYTE[codeSize]);
        memcpy(code, dataArray->GetDataPtr(), codeSize);
        m_CodeSize = codeSize;
        _ASSERTE(FitsIn<unsigned short>(stackSize));
        m_StackSize = static_cast<unsigned short>(stackSize);
        m_Options = (initLocals) ? CORINFO_OPT_INIT_LOCALS : (CorInfoOptions)0;
        _ASSERTE(FitsIn<unsigned short>(EHSize));
        m_EHSize = static_cast<unsigned short>(EHSize);
        m_Code = (BYTE*)code;
        code.SuppressRelease();
        LOG((LF_BCL, LL_INFO100000, "Level5 - DM-JIT: CodeInfo {0x%p} on resolver %p\n", m_Code, this));
    }

    *pCodeSize = m_CodeSize;
    if (pStackSize)
        *pStackSize = m_StackSize;
    if (pOptions)
        *pOptions = m_Options;
    if (pEHSize)
        *pEHSize = m_EHSize;
    return m_Code;

}

//---------------------------------------------------------------------------------------
//
SigPointer
LCGMethodResolver::GetLocalSig()
{
    STANDARD_VM_CONTRACT;

    if (m_LocalSig.IsNull())
    {
        GCX_COOP();

        LOG((LF_BCL, LL_INFO100000, "Level5 - DM-JIT: Getting LocalSig on resolver 0x%p...\n", this));

        MethodDescCallSite getLocalsSignature(METHOD__RESOLVER__GET_LOCALS_SIGNATURE, m_managedResolver);

        OBJECTREF resolver = ObjectFromHandle(m_managedResolver);
        VALIDATEOBJECTREF(resolver); // gc root must be up the stack

        ARG_SLOT args[] =
        {
            ObjToArgSlot(resolver)
        };
        U1ARRAYREF dataArray = (U1ARRAYREF) getLocalsSignature.Call_RetOBJECTREF(args);
        DWORD localSigSize = dataArray->GetNumComponents();
        NewArrayHolder<COR_SIGNATURE> localSig(new COR_SIGNATURE[localSigSize]);
        memcpy((void *)localSig, dataArray->GetDataPtr(), localSigSize);

        m_LocalSig = SigPointer((PCCOR_SIGNATURE)localSig, localSigSize);
        localSig.SuppressRelease();
        LOG((LF_BCL, LL_INFO100000, "Level5 - DM-JIT: LocalSig {0x%p} on resolver %p\n", m_LocalSig.GetPtr(), this));
    }

    return m_LocalSig;
} // LCGMethodResolver::GetLocalSig

//---------------------------------------------------------------------------------------
//
OBJECTHANDLE
LCGMethodResolver::ConstructStringLiteral(mdToken metaTok)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    OBJECTHANDLE string = NULL;
    STRINGREF strRef = GetStringLiteral(metaTok);

    GCPROTECT_BEGIN(strRef);

    if (strRef != NULL)
    {
        // Instead of storing the string literal in the appdomain specific string literal map,
        // we store it in the dynamic method specific string liternal list
        // This way we can release it when the dynamic method is collected.
        string = (OBJECTHANDLE)GetOrInternString(&strRef);
    }

    GCPROTECT_END();

    return string;
}

//---------------------------------------------------------------------------------------
//
BOOL
LCGMethodResolver::IsValidStringRef(mdToken metaTok)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    return GetStringLiteral(metaTok) != NULL;
}

int
LCGMethodResolver::GetStringLiteralLength(mdToken metaTok)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    STRINGREF str = GetStringLiteral(metaTok);
    if (str != NULL)
    {
        return str->GetStringLength();
    }
    return -1;
}

//---------------------------------------------------------------------------------------
//
STRINGREF
LCGMethodResolver::GetStringLiteral(
    mdToken token)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    MethodDescCallSite getStringLiteral(METHOD__RESOLVER__GET_STRING_LITERAL, m_managedResolver);

    OBJECTREF resolver = ObjectFromHandle(m_managedResolver);
    VALIDATEOBJECTREF(resolver); // gc root must be up the stack

    ARG_SLOT args[] = {
        ObjToArgSlot(resolver),
        token,
    };
    return getStringLiteral.Call_RetSTRINGREF(args);
}

// This method will get the interned string by calling GetInternedString on the
// global string liternal interning map. It will also store the returned entry
// in m_DynamicStringLiterals
STRINGREF* LCGMethodResolver::GetOrInternString(STRINGREF *pProtectedStringRef)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pProtectedStringRef));
    } CONTRACTL_END;

    // Get the global string literal interning map
    GlobalStringLiteralMap* pStringLiteralMap = SystemDomain::GetGlobalStringLiteralMap();

    // Calculating the hash: EEUnicodeHashTableHelper::GetHash
    EEStringData StringData = EEStringData((*pProtectedStringRef)->GetStringLength(), (*pProtectedStringRef)->GetBuffer());
    DWORD dwHash = pStringLiteralMap->GetHash(&StringData);

    // lock the global string literal interning map
    CrstHolder gch(pStringLiteralMap->GetHashTableCrstGlobal());

    StringLiteralEntryHolder pEntry(pStringLiteralMap->GetInternedString(pProtectedStringRef, dwHash, /* bAddIfNotFound */ TRUE));

    DynamicStringLiteral* pStringLiteral = (DynamicStringLiteral*)m_jitTempData.New(sizeof(DynamicStringLiteral));
    pStringLiteral->m_pEntry = pEntry.Extract();

    // Add to m_DynamicStringLiterals:
    //  we don't need to check for duplicate because the string literal entries in
    //      the global string literal map are ref counted.
    pStringLiteral->m_pNext = m_DynamicStringLiterals;
    m_DynamicStringLiterals = pStringLiteral;

    return pStringLiteral->m_pEntry->GetStringObject();

}

// AddToUsedIndCellList adds a IndCellList link to the beginning of m_UsedIndCellList. It is called by
// code:CEEInfo::getCallInfo when a indirection cell is allocated for m_pDynamicMethod.
// All the indirection cells usded by m_pDynamicMethod will be recycled when this resolver
// is finalized, see code:LCGMethodResolver::RecycleIndCells
void LCGMethodResolver::AddToUsedIndCellList(BYTE * indcell)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(indcell));
    } CONTRACTL_END;

    IndCellList * link = (IndCellList *)m_jitTempData.New(sizeof(IndCellList));
    link->indcell = indcell;

    // Insert into m_UsedIndCellList
    while (true)
    {
        link->pNext = m_UsedIndCellList;
        if (InterlockedCompareExchangeT(&m_UsedIndCellList, link, link->pNext) == link->pNext)
            break;
    }

}

void LCGMethodResolver::ResolveToken(mdToken token, TypeHandle * pTH, MethodDesc ** ppMD, FieldDesc ** ppFD)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    PREPARE_SIMPLE_VIRTUAL_CALLSITE(METHOD__RESOLVER__RESOLVE_TOKEN, ObjectFromHandle(m_managedResolver));

    DECLARE_ARGHOLDER_ARRAY(args, 5);

    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(ObjectFromHandle(m_managedResolver));
    args[ARGNUM_1] = DWORD_TO_ARGHOLDER(token);
    args[ARGNUM_2] = pTH;
    args[ARGNUM_3] = ppMD;
    args[ARGNUM_4] = ppFD;

    CALL_MANAGED_METHOD_NORET(args);

    _ASSERTE(*ppMD == NULL || *ppFD == NULL);

    if (pTH->IsNull())
    {
        if (*ppMD != NULL) *pTH = (*ppMD)->GetMethodTable();
        else
        if (*ppFD != NULL) *pTH = (*ppFD)->GetEnclosingMethodTable();
    }

    _ASSERTE(!pTH->IsNull());
}

//---------------------------------------------------------------------------------------
//
SigPointer
LCGMethodResolver::ResolveSignature(
    mdToken token)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    U1ARRAYREF dataArray = NULL;

    PREPARE_SIMPLE_VIRTUAL_CALLSITE(METHOD__RESOLVER__RESOLVE_SIGNATURE, ObjectFromHandle(m_managedResolver));

    DECLARE_ARGHOLDER_ARRAY(args, 3);

    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(ObjectFromHandle(m_managedResolver));
    args[ARGNUM_1] = DWORD_TO_ARGHOLDER(token);
    args[ARGNUM_2] = DWORD_TO_ARGHOLDER(0);

    CALL_MANAGED_METHOD_RETREF(dataArray, U1ARRAYREF, args);

    if (dataArray == NULL)
        COMPlusThrow(kInvalidProgramException);

    DWORD cbSig = dataArray->GetNumComponents();
    PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)m_jitTempData.New(cbSig);
    memcpy((void *)pSig, dataArray->GetDataPtr(), cbSig);
    return SigPointer(pSig, cbSig);
} // LCGMethodResolver::ResolveSignature

//---------------------------------------------------------------------------------------
//
SigPointer
LCGMethodResolver::ResolveSignatureForVarArg(
    mdToken token)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    U1ARRAYREF dataArray = NULL;

    PREPARE_SIMPLE_VIRTUAL_CALLSITE(METHOD__RESOLVER__RESOLVE_SIGNATURE, ObjectFromHandle(m_managedResolver));

    DECLARE_ARGHOLDER_ARRAY(args, 3);

    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(ObjectFromHandle(m_managedResolver));
    args[ARGNUM_1] = DWORD_TO_ARGHOLDER(token);
    args[ARGNUM_2] = DWORD_TO_ARGHOLDER(1);

    CALL_MANAGED_METHOD_RETREF(dataArray, U1ARRAYREF, args);

    if (dataArray == NULL)
        COMPlusThrow(kInvalidProgramException);

    DWORD cbSig = dataArray->GetNumComponents();
    PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)m_jitTempData.New(cbSig);
    memcpy((void *)pSig, dataArray->GetDataPtr(), cbSig);
    return SigPointer(pSig, cbSig);
} // LCGMethodResolver::ResolveSignatureForVarArg

//---------------------------------------------------------------------------------------
//
void LCGMethodResolver::GetEHInfo(unsigned EHnumber, CORINFO_EH_CLAUSE* clause)
{
    STANDARD_VM_CONTRACT;

    GCX_COOP();

    // attempt to get the raw EHInfo first
    {
        U1ARRAYREF dataArray;

        PREPARE_SIMPLE_VIRTUAL_CALLSITE(METHOD__RESOLVER__GET_RAW_EH_INFO, ObjectFromHandle(m_managedResolver));

        DECLARE_ARGHOLDER_ARRAY(args, 1);

        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(ObjectFromHandle(m_managedResolver));

        CALL_MANAGED_METHOD_RETREF(dataArray, U1ARRAYREF, args);

        if (dataArray != NULL)
        {
            COR_ILMETHOD_SECT_EH* pEH = (COR_ILMETHOD_SECT_EH*)dataArray->GetDataPtr();

            COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehClause;
            const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;
            ehInfo = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)pEH->EHClause(EHnumber, &ehClause);

            clause->Flags = (CORINFO_EH_CLAUSE_FLAGS)ehInfo->GetFlags();
            clause->TryOffset = ehInfo->GetTryOffset();
            clause->TryLength = ehInfo->GetTryLength();
            clause->HandlerOffset = ehInfo->GetHandlerOffset();
            clause->HandlerLength = ehInfo->GetHandlerLength();
            clause->ClassToken = ehInfo->GetClassToken();
            clause->FilterOffset = ehInfo->GetFilterOffset();
            return;
        }
    }

    // failed, get the info off the ilgenerator
    {
        PREPARE_SIMPLE_VIRTUAL_CALLSITE(METHOD__RESOLVER__GET_EH_INFO, ObjectFromHandle(m_managedResolver));

        DECLARE_ARGHOLDER_ARRAY(args, 3);

        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(ObjectFromHandle(m_managedResolver));
        args[ARGNUM_1] = DWORD_TO_ARGHOLDER(EHnumber);
        args[ARGNUM_2] = PTR_TO_ARGHOLDER(clause);

        CALL_MANAGED_METHOD_NORET(args);
    }
}

#endif // !DACCESS_COMPILE


// Get the associated managed resolver. This method will be called during a GC so it should not throw, trigger a GC or cause the
// object in question to be validated.
OBJECTREF LCGMethodResolver::GetManagedResolver()
{
    LIMITED_METHOD_CONTRACT;
    return ObjectFromHandle(m_managedResolver);
}


//
// ChunkAllocator implementation
//
ChunkAllocator::~ChunkAllocator()
{
    LIMITED_METHOD_CONTRACT;
    Delete();
}

void ChunkAllocator::Delete()
{
    LIMITED_METHOD_CONTRACT;
    BYTE *next = NULL;
    LOG((LF_BCL, LL_INFO10, "Level1 - DM - Allocator [0x%p] - deleting...\n", this));
    while (m_pData)
    {
        LOG((LF_BCL, LL_INFO10, "Level1 - DM - Allocator [0x%p] - delete block {0x%p}\n", this, m_pData));
        next = ((BYTE**)m_pData)[0];
        delete[] m_pData;
        m_pData = next;
    }
}

void* ChunkAllocator::New(size_t size)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    // We need to align it, otherwise we might get DataMisalignedException on IA64
    size = ALIGN_UP(size, sizeof(void *));

    BYTE *pNewBlock = NULL;
    LOG((LF_BCL, LL_INFO100, "Level2 - DM - Allocator [0x%p] - allocation requested 0x%X, available 0x%X\n", this, size, (m_pData) ? ((size_t*)m_pData)[1] : 0));
    if (m_pData)
    {
        // we may have room available
        size_t available = ((size_t*)m_pData)[1];
        if (size <= available)
        {
            LOG((LF_BCL, LL_INFO100, "Level2 - DM - Allocator [0x%p] - reusing block {0x%p}\n", this, m_pData));
            ((size_t*)m_pData)[1] = available - size;
            pNewBlock = (m_pData + CHUNK_SIZE - available);
            LOG((LF_BCL, LL_INFO100, "Level2 - DM - Allocator [0x%p] - ptr -> 0x%p, available 0x%X\n", this, pNewBlock, ((size_t*)m_pData)[1]));
            return pNewBlock;
        }
    }

    // no available - need to allocate a new buffer
    if (size + (sizeof(void*) * 2) < CHUNK_SIZE)
    {
        // make the allocation
        NewArrayHolder<BYTE> newBlock(new BYTE[CHUNK_SIZE]);
        pNewBlock = (BYTE*)newBlock;
        ((size_t*)pNewBlock)[1] = CHUNK_SIZE - size - (sizeof(void*) * 2);
        LOG((LF_BCL, LL_INFO10, "Level1 - DM - Allocator [0x%p] - new block {0x%p}\n", this, pNewBlock));
        newBlock.SuppressRelease();
    }
    else
    {
        // request bigger than default size this is going to be a single block
        NewArrayHolder<BYTE> newBlock(new BYTE[size + (sizeof(void*) * 2)]);
        pNewBlock = (BYTE*)newBlock;
        ((size_t*)pNewBlock)[1] = 0; // no available bytes left
        LOG((LF_BCL, LL_INFO10, "Level1 - DM - Allocator [0x%p] - new BIG block {0x%p}\n", this, pNewBlock));
        newBlock.SuppressRelease();
    }

    // all we have left to do is to link the block.
    // We leave at the top the block with more bytes available
    if (m_pData)
    {
        if (((size_t*)pNewBlock)[1] > ((size_t*)m_pData)[1])
        {
            ((BYTE**)pNewBlock)[0] = m_pData;
            m_pData = pNewBlock;
        }
        else
        {
            ((BYTE**)pNewBlock)[0] = ((BYTE**)m_pData)[0];
            ((BYTE**)m_pData)[0] = pNewBlock;
        }
    }
    else
    {
        // this is the first allocation
        m_pData = pNewBlock;
        ((BYTE**)m_pData)[0] = NULL;
    }

    pNewBlock += (sizeof(void*) * 2);
    LOG((LF_BCL, LL_INFO100, "Level2 - DM - Allocator [0x%p] - ptr -> 0x%p, available 0x%X\n", this, pNewBlock, ((size_t*)m_pData)[1]));
    return pNewBlock;
}

