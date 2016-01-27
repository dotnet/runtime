// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//


#include "common.h"
#include "dynamicmethod.h"
#include "object.h"
#include "method.hpp"
#include "comdelegate.h"
#include "security.h"
#include "field.h"
#include "contractimpl.h"
#include "nibblemapmacros.h"
#include "stringliteralmap.h"
#include "virtualcallstub.h"


#ifndef DACCESS_COMPILE 

// get the method table for dynamic methods
DynamicMethodTable* DomainFile::GetDynamicMethodTable()
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

    // Go over all DynamicMethodDescs and make sure that they are destroyed

    if (m_pMethodTable != NULL)
    {
        MethodTable::IntroducedMethodIterator it(m_pMethodTable);
        for (; it.IsValid(); it.Next())
        {
            DynamicMethodDesc *pMD = (DynamicMethodDesc*)it.GetMethodDesc();
            pMD->Destroy(TRUE /* fDomainUnload */);
        }
    }

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

    LoaderHeap* pHeap = m_pDomain->GetHighFrequencyHeap();
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

        pNewMD->m_dwExtendedFlags = mdPublic | mdStatic | DynamicMethodDesc::nomdLCGMethod;

        LCGMethodResolver* pResolver = new (pResolvers) LCGMethodResolver();
        pResolver->m_pDynamicMethod = pNewMD;
        pResolver->m_DynamicMethodTable = this;
        pNewMD->m_pResolver = pResolver;

        pNewMD->SetTemporaryEntryPoint(m_pDomain->GetLoaderAllocator(), &amt);

#ifdef _DEBUG 
        pNewMD->m_pDebugMethodTable.SetValue(m_pMethodTable);
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

    pNewMD->m_dwExtendedFlags = mdPublic | mdStatic | DynamicMethodDesc::nomdLCGMethod;

#ifdef _DEBUG 
    pNewMD->m_pszDebugMethodName = name;
    pNewMD->m_pszDebugClassName  = (LPUTF8)"dynamicclass";
    pNewMD->m_pszDebugMethodSignature = "DynamicMethod Signature not available";
#endif // _DEBUG
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
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    size_t MaxCodeHeapSize  = pInfo->getRequestSize();
    size_t ReserveBlockSize = MaxCodeHeapSize + sizeof(HeapList);

    ReserveBlockSize += sizeof(TrackAllocation) + PAGE_SIZE; // make sure we have enough for the allocation
    // take a conservative size for the nibble map, we may change that later if appropriate
    size_t nibbleMapSize = ROUND_UP_TO_PAGE(HEAP2MAPSIZE(ROUND_UP_TO_PAGE(ALIGN_UP(ReserveBlockSize, VIRTUAL_ALLOC_RESERVE_GRANULARITY))));
    size_t heapListSize = (sizeof(HeapList) + CODE_SIZE_ALIGN - 1) & (~(CODE_SIZE_ALIGN - 1));
    size_t otherData = heapListSize;
    // make conservative estimate of the memory needed for otherData
    size_t reservedData = (otherData + HOST_CODEHEAP_SIZE_ALIGN - 1) & (~(HOST_CODEHEAP_SIZE_ALIGN - 1));
    
    NewHolder<HostCodeHeap> pCodeHeap(new HostCodeHeap(ReserveBlockSize + nibbleMapSize + reservedData, pJitManager, pInfo));
    LOG((LF_BCL, LL_INFO10, "Level2 - CodeHeap creation {0x%p} - requested 0x%p, size available 0x%p, private data 0x%p, nibble map 0x%p\n", 
                            (HostCodeHeap*)pCodeHeap, ReserveBlockSize, pCodeHeap->m_TotalBytesAvailable, reservedData, nibbleMapSize));

    BYTE *pBuffer = pCodeHeap->InitCodeHeapPrivateData(ReserveBlockSize, reservedData, nibbleMapSize);
    _ASSERTE(((size_t)pBuffer & PAGE_MASK) == 0);
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap creation {0x%p} - base addr 0x%p, size available 0x%p, nibble map ptr 0x%p\n",
                            (HostCodeHeap*)pCodeHeap, pCodeHeap->m_pBaseAddr, pCodeHeap->m_TotalBytesAvailable, pBuffer));

    void* pHdrMap = pBuffer;

    HeapList *pHp = (HeapList*)pCodeHeap->AllocMemory(otherData, 0);
    pHp->pHeap = (PTR_CodeHeap)pCodeHeap;
    // wire it back
    pCodeHeap->m_pHeapList = (PTR_HeapList)pHp;
    // assign beginning of nibble map
    pHp->pHdrMap = (PTR_DWORD)(DWORD*)pHdrMap;

    TrackAllocation *pTracker = *((TrackAllocation**)pHp - 1);
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap creation {0x%p} - size available 0x%p, private data ptr [0x%p, 0x%p]\n",
                            (HostCodeHeap*)pCodeHeap, pCodeHeap->m_TotalBytesAvailable, pTracker, pTracker->size));

    // need to update the reserved data
    pCodeHeap->m_ReservedData += pTracker->size;

    pHp->startAddress    = dac_cast<TADDR>(pCodeHeap->m_pBaseAddr) + pTracker->size;
    pHp->mapBase         = ROUND_DOWN_TO_PAGE(pHp->startAddress);  // round down to next lower page align
    pHp->endAddress      = pHp->startAddress;

    pHp->maxCodeHeapSize = pCodeHeap->m_TotalBytesAvailable - pTracker->size;
    _ASSERTE(pHp->maxCodeHeapSize >= MaxCodeHeapSize);

    // We do not need to memset this memory, since ClrVirtualAlloc() guarantees that the memory is zero.
    // Furthermore, if we avoid writing to it, these pages don't come into our working set

    pHp->bFull           = FALSE;
    pHp->cBlocks         = 0;
#ifdef _WIN64
    emitJump(pHp->CLRPersonalityRoutine, (void *)ProcessCLRException);
#endif

    // zero the ref count as now starts the real counter
    pCodeHeap->m_AllocationCount = 0;

    pCodeHeap.SuppressRelease();

    LOG((LF_BCL, LL_INFO10, "Level1 - CodeHeap created {0x%p}\n", (HostCodeHeap*)pCodeHeap));
    RETURN pHp;
}

HostCodeHeap::HostCodeHeap(size_t ReserveBlockSize, EEJitManager *pJitManager, CodeHeapRequestInfo *pInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // reserve ReserveBlockSize rounded-up to VIRTUAL_ALLOC_RESERVE_GRANULARITY of memory
    ReserveBlockSize = ALIGN_UP(ReserveBlockSize, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

    if (pInfo->m_loAddr != NULL || pInfo->m_hiAddr != NULL)
    {
        m_pBaseAddr = ClrVirtualAllocWithinRange(pInfo->m_loAddr, pInfo->m_hiAddr,
                                                ReserveBlockSize, MEM_RESERVE, PAGE_NOACCESS);
        if (!m_pBaseAddr)
            ThrowOutOfMemoryWithinRange();
    }
    else
    {
        m_pBaseAddr = ClrVirtualAllocExecutable(ReserveBlockSize, MEM_RESERVE, PAGE_NOACCESS);
        if (!m_pBaseAddr)
            ThrowOutOfMemory();
    }

    m_pLastAvailableCommittedAddr = m_pBaseAddr;
    m_TotalBytesAvailable = ReserveBlockSize;
    m_AllocationCount = 0;
    m_ReservedData = 0;
    m_pJitManager = (PTR_EEJitManager)pJitManager;
    m_pFreeList = NULL;
    m_pAllocator = pInfo->m_pAllocator;
    m_pNextHeapToRelease = NULL;
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap creation {0x%p, vt(0x%x)} - base addr 0x%p, total size 0x%p\n", 
                            this, *(size_t*)this, m_pBaseAddr, m_TotalBytesAvailable));
}

HostCodeHeap::~HostCodeHeap()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pBaseAddr)
        ClrVirtualFree(m_pBaseAddr, 0, MEM_RELEASE);
    LOG((LF_BCL, LL_INFO10, "Level1 - CodeHeap destroyed {0x%p}\n", this));
}

BYTE* HostCodeHeap::InitCodeHeapPrivateData(size_t ReserveBlockSize, size_t otherData, size_t nibbleMapSize)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    size_t nibbleNewSize = ROUND_UP_TO_PAGE(HEAP2MAPSIZE(ROUND_UP_TO_PAGE(m_TotalBytesAvailable)));
    if (m_TotalBytesAvailable - nibbleNewSize < ReserveBlockSize + otherData)
    {
        // the new allocation for the nibble map would notleave enough room for the requested memory, bail out
        nibbleNewSize = nibbleMapSize;
    }

    BYTE *pAddress = (BYTE*)ROUND_DOWN_TO_PAGE(dac_cast<TADDR>(m_pLastAvailableCommittedAddr) + 
                                               m_TotalBytesAvailable - nibbleNewSize);
    _ASSERTE(m_pLastAvailableCommittedAddr + m_TotalBytesAvailable >= pAddress + nibbleNewSize);
    if (NULL == ClrVirtualAlloc(pAddress, nibbleNewSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE))
        ThrowOutOfMemory();
    m_TotalBytesAvailable = pAddress - m_pLastAvailableCommittedAddr;
    _ASSERTE(m_TotalBytesAvailable >= ReserveBlockSize + otherData);
    return pAddress;
}

 // used to flag a block that is too small
#define UNUSABLE_BLOCK      ((size_t)-1)
 
size_t HostCodeHeap::GetPadding(TrackAllocation *pCurrent, size_t size, DWORD alignment)
{
    LIMITED_METHOD_CONTRACT;
    if (pCurrent->size < size)
        return UNUSABLE_BLOCK;
    size_t padding = 0;
    if (alignment)
    {
        size_t pointer = (size_t)((BYTE*)pCurrent + sizeof(TrackAllocation));
        padding = ((pointer + (size_t)alignment - 1) & (~((size_t)alignment - 1))) - pointer;
    }
    if (pCurrent->size < size + padding)
        return UNUSABLE_BLOCK;
    return padding;
}

void* HostCodeHeap::AllocFromFreeList(size_t size, DWORD alignment)
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
            // GetPadding will return UNUSABLE_BLOCK if the current block is not big enough
            size_t padding = GetPadding(pCurrent, size, alignment);
            if (UNUSABLE_BLOCK != padding)
            {
                // found a block
                LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Block found, size 0x%X\n", this, pCurrent->size));
                size_t realSize = size + padding;
                BYTE *pPointer = (BYTE*)pCurrent + sizeof(TrackAllocation) + padding;
                _ASSERTE((size_t)(pPointer - (BYTE*)pCurrent) >= sizeof(TrackAllocation));

                // The space left is not big enough for a new block, let's just
                // update the TrackAllocation record for the current block
                if (pCurrent->size - realSize <= sizeof(TrackAllocation))
                {
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Item removed %p, size 0x%X\n", this, pCurrent, pCurrent->size));
                    // remove current
                    if (pPrevious)
                    {
                        pPrevious->pNext = pCurrent->pNext;
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
                    pNewCurrent->pNext = pCurrent->pNext;
                    pNewCurrent->size = pCurrent->size - realSize;
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Item changed %p, new size 0x%X\n", this, pNewCurrent, pNewCurrent->size));
                    if (pPrevious)
                    {
                        pPrevious->pNext = pNewCurrent;
                    }
                    else
                    {
                        m_pFreeList = pNewCurrent;
                    }

                    // We only need to update the size of the current block if we are creating a new block
                    pCurrent->size = realSize;
                }

                // now fill all the padding data correctly
                pCurrent->pHeap = this;
                // store the location of the TrackAllocation record right before pPointer
                *((void**)pPointer - 1) = pCurrent;

                LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Allocation returned %p, size 0x%X - data -> %p\n", this, pCurrent, pCurrent->size, pPointer));
                return pPointer;
            }
            pPrevious = pCurrent;
            pCurrent = pCurrent->pNext;
        }
    }
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - No block in free list for size 0x%X\n", this, size));
    return NULL;
}

void HostCodeHeap::AddToFreeList(TrackAllocation *pBlockToInsert)
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
                pBlockToInsert->pNext = pCurrent;
                if (pPrevious)
                {
                    pPrevious->pNext = pBlockToInsert;
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
                    pBlockToInsert->pNext = pCurrent->pNext;
                    pBlockToInsert->size += pCurrent->size;
                }

                if (pPrevious && (BYTE*)pPrevious + pPrevious->size == (BYTE*)pBlockToInsert)
                {
                    // coalesce with previous
                    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Coalesce block [%p, 0x%X] with [%p, 0x%X] - new size 0x%X\n", this,
                                                                        pPrevious, pPrevious->size,
                                                                        pBlockToInsert, pBlockToInsert->size,
                                                                        pPrevious->size + pBlockToInsert->size));
                    pPrevious->pNext = pBlockToInsert->pNext;
                    pPrevious->size += pBlockToInsert->size;
                }

                return;
            }
            pPrevious = pCurrent;
            pCurrent = pCurrent->pNext;
        }
        _ASSERTE(pPrevious && pCurrent == NULL);
        pBlockToInsert->pNext = NULL;
        // last in the list
        if ((BYTE*)pPrevious + pPrevious->size == (BYTE*)pBlockToInsert)
        {
            // coalesce with previous
            LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Coalesce block [%p, 0x%X] with [%p, 0x%X] - new size 0x%X\n", this,
                                                                pPrevious, pPrevious->size,
                                                                pBlockToInsert, pBlockToInsert->size,
                                                                pPrevious->size + pBlockToInsert->size));
            pPrevious->size += pBlockToInsert->size;
        }
        else
        {
            pPrevious->pNext = pBlockToInsert;
            LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Insert block [%p, 0x%X] to end after [%p, 0x%X]\n", this,
                                                                pBlockToInsert, pBlockToInsert->size,
                                                                pPrevious, pPrevious->size));
        }

        return;

    }
    // first in the list
    pBlockToInsert->pNext = m_pFreeList;
    m_pFreeList = pBlockToInsert;
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Insert block [%p, 0x%X] to head\n", this,
                                                        m_pFreeList, m_pFreeList->size));
}

void* HostCodeHeap::AllocMemForCode_NoThrow(size_t header, size_t size, DWORD alignment)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(header == sizeof(CodeHeader));

    // The code allocator has to guarantee that there is only one entrypoint per nibble map entry. 
    // It is guaranteed because of HostCodeHeap allocator always aligns the size up to HOST_CODEHEAP_SIZE_ALIGN,
    // and because the size of nibble map entries (BYTES_PER_BUCKET) is smaller than HOST_CODEHEAP_SIZE_ALIGN.
    // Assert the later fact here.
    _ASSERTE(HOST_CODEHEAP_SIZE_ALIGN >= BYTES_PER_BUCKET);

    BYTE * pMem = (BYTE *)AllocMemory_NoThrow(size + sizeof(CodeHeader) + (alignment - 1), sizeof(void *));
    if (pMem == NULL)
        return NULL;

    BYTE * pCode = (BYTE *)ALIGN_UP(pMem + sizeof(CodeHeader), alignment);

    // Update tracker to account for the alignment we have just added
    TrackAllocation *pTracker = *((TrackAllocation **)pMem - 1);

    CodeHeader * pHdr = dac_cast<PTR_CodeHeader>(pCode) - 1;
    *((TrackAllocation **)(pHdr) - 1) = pTracker;

    return pCode;
}

void* HostCodeHeap::AllocMemory(size_t size, DWORD alignment)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    void *pAllocation = AllocMemory_NoThrow(size, alignment);
    if (!pAllocation)
        ThrowOutOfMemory();
    return pAllocation;
}

void* HostCodeHeap::AllocMemory_NoThrow(size_t size, DWORD alignment)
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

    // honor alignment (should assert the value is proper)
    if (alignment)
        size = (size + (size_t)alignment - 1) & (~((size_t)alignment - 1));
    // align size to HOST_CODEHEAP_SIZE_ALIGN always
    size = (size + HOST_CODEHEAP_SIZE_ALIGN - 1) & (~(HOST_CODEHEAP_SIZE_ALIGN - 1));

    size += sizeof(TrackAllocation);

    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - Allocation requested 0x%X\n", this, size));

    void *pAddr = AllocFromFreeList(size, alignment);
    if (!pAddr)
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
        _ASSERTE(size > availableInFreeList);
        size_t sizeToCommit = size - availableInFreeList; 
        sizeToCommit = (size + PAGE_SIZE - 1) & (~(PAGE_SIZE - 1)); // round up to page

        if (m_pLastAvailableCommittedAddr + sizeToCommit <= m_pBaseAddr + m_TotalBytesAvailable)
        {
            if (NULL == ClrVirtualAlloc(m_pLastAvailableCommittedAddr, sizeToCommit, MEM_COMMIT, PAGE_EXECUTE_READWRITE))
            {
                LOG((LF_BCL, LL_ERROR, "CodeHeap [0x%p] - VirtualAlloc failed\n", this));
                return NULL;
            }

            TrackAllocation *pBlockToInsert = (TrackAllocation*)(void*)m_pLastAvailableCommittedAddr;
            pBlockToInsert->pNext = NULL;
            pBlockToInsert->size = sizeToCommit;
            m_pLastAvailableCommittedAddr += sizeToCommit;
            AddToFreeList(pBlockToInsert);
            pAddr = AllocFromFreeList(size, alignment);
        }
        else
        {
            LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - allocation failed:\n\tm_pLastAvailableCommittedAddr: 0x%X\n\tsizeToCommit: 0x%X\n\tm_pBaseAddr: 0x%X\n\tm_TotalBytesAvailable: 0x%X\n", this, m_pLastAvailableCommittedAddr, sizeToCommit, m_pBaseAddr, m_TotalBytesAvailable));
            return NULL;
        }
    }

    _ASSERTE(pAddr);
    // ref count the whole heap
    m_AllocationCount++;
    LOG((LF_BCL, LL_INFO100, "Level2 - CodeHeap [0x%p] - ref count %d\n", this, m_AllocationCount));
    return pAddr;
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

    CodeHeader * pHdr = dac_cast<PTR_CodeHeader>(codeStart) - 1;

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
    AddToFreeList(pTracker);

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
void DynamicMethodDesc::Destroy(BOOL fDomainUnload)
{
    CONTRACTL
    {
        if (fDomainUnload) NOTHROW; else THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(IsDynamicMethod());
    LoaderAllocator *pLoaderAllocator = GetLoaderAllocatorForCode();

    LOG((LF_BCL, LL_INFO1000, "Level3 - Destroying DynamicMethod {0x%p}\n", this));
    if (m_pSig)
    {
        delete[] (BYTE*)m_pSig;
        m_pSig = NULL;
    }
    m_cSig = 0;
    if (m_pszMethodName)
    {
        delete[] m_pszMethodName;
        m_pszMethodName = NULL;
    }

    GetLCGMethodResolver()->Destroy(fDomainUnload);

    if (pLoaderAllocator->IsCollectible() && !fDomainUnload)
    {
        if (pLoaderAllocator->Release())
        {
            GCX_PREEMP();
            LoaderAllocator::GCLoaderAllocators(pLoaderAllocator->GetDomain()->AsAppDomain());
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
    m_jumpStubBlock         = NULL;
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
        // We should use GetLoaderAllocatorForCode because that is where the ind cell was allocated.
        LoaderAllocator *pLoaderAllocator = GetDynamicMethod()->GetLoaderAllocatorForCode();
        VirtualCallStubManager *pMgr = pLoaderAllocator->GetVirtualCallStubManager();
        pMgr->InsertIntoRecycledIndCellList_Locked(cellhead, cellcurr);
        m_UsedIndCellList = NULL;
    }
}

void LCGMethodResolver::Destroy(BOOL fDomainUnload)
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


    if (!fDomainUnload)
    {
        // No need to recycle if the domain is unloading.
        // Note that we need to do this before m_jitTempData is deleted
        RecycleIndCells();
    }

    m_jitMetaHeap.Delete();
    m_jitTempData.Delete();


    // Per-appdomain resources has been reclaimed already if the appdomain is being unloaded. Do not try to
    // release them again.
    if (!fDomainUnload)
    {
        if (m_recordCodePointer)
        {
#if defined(_TARGET_AMD64_)
            // Remove the unwind information (if applicable)
            UnwindInfoTable::UnpublishUnwindInfoForMethod((TADDR)m_recordCodePointer);
#endif // defined(_TARGET_AMD64_)

            HostCodeHeap *pHeap = HostCodeHeap::GetCodeHeap((TADDR)m_recordCodePointer);
            LOG((LF_BCL, LL_INFO1000, "Level3 - Resolver {0x%p} - Release reference to heap {%p, vt(0x%x)} \n", this, pHeap, *(size_t*)pHeap));
            pHeap->m_pJitManager->FreeCodeMemory(pHeap, m_recordCodePointer);

            m_recordCodePointer = NULL;
        }

        JumpStubBlockHeader* current = m_jumpStubBlock;
        JumpStubBlockHeader* next;
        while (current)
        {
            next = current->m_next;

            HostCodeHeap *pHeap = current->GetHostCodeHeap();
            LOG((LF_BCL, LL_INFO1000, "Level3 - Resolver {0x%p} - Release reference to heap {%p, vt(0x%x)} \n", current, pHeap, *(size_t*)pHeap));
            pHeap->m_pJitManager->FreeCodeMemory(pHeap, current);

            current = next;
        }
        m_jumpStubBlock = NULL;

        if (m_managedResolver)
        {
            ::DestroyLongWeakHandle(m_managedResolver);
            m_managedResolver = NULL;
        }

        m_DynamicMethodTable->LinkMethod(m_pDynamicMethod);
    }
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
    GetJitContextCoop(securityControlFlags, typeOwner);
}

void LCGMethodResolver::GetJitContextCoop(SecurityControlFlags * securityControlFlags,
                                      TypeHandle *typeOwner)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(securityControlFlags));
        PRECONDITION(CheckPointer(typeOwner));
    } CONTRACTL_END;

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

        DWORD initLocals = 0, EHSize = 0;
        unsigned short stackSize = 0;
        ARG_SLOT args[] =
        {
            ObjToArgSlot(resolver),
            PtrToArgSlot(&stackSize),
            PtrToArgSlot(&initLocals),
            PtrToArgSlot(&EHSize),
        };
        U1ARRAYREF dataArray = (U1ARRAYREF) getCodeInfo.Call_RetOBJECTREF(args);
        DWORD codeSize = dataArray->GetNumComponents();
        NewHolder<BYTE> code(new BYTE[codeSize]);
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
        NewHolder<COR_SIGNATURE> localSig(new COR_SIGNATURE[localSigSize]);
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
// code:CEEInfo::getCallInfo when a indirection cell is alocated for m_pDynamicMethod.
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
        SO_TOLERANT;
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
        NewHolder<BYTE> newBlock(new BYTE[CHUNK_SIZE]);
        pNewBlock = (BYTE*)newBlock;
        ((size_t*)pNewBlock)[1] = CHUNK_SIZE - size - (sizeof(void*) * 2); 
        LOG((LF_BCL, LL_INFO10, "Level1 - DM - Allocator [0x%p] - new block {0x%p}\n", this, pNewBlock));
        newBlock.SuppressRelease();
    }
    else
    {
        // request bigger than default size this is going to be a single block
        NewHolder<BYTE> newBlock(new BYTE[size + (sizeof(void*) * 2)]);
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

