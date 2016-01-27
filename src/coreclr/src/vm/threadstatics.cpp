// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// ThreadStatics.cpp
// 

// 
// 


#include "common.h"

#include "threadstatics.h"
#include "field.h"


#ifndef DACCESS_COMPILE

void ThreadLocalBlock::FreeTLM(SIZE_T i)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE(m_pTLMTable != NULL);

    PTR_ThreadLocalModule pThreadLocalModule = m_pTLMTable[i].pTLM;
    m_pTLMTable[i].pTLM = NULL;

    if (pThreadLocalModule != NULL)
    {
        if (pThreadLocalModule->m_pDynamicClassTable != NULL)
        {
            for (DWORD k = 0; k < pThreadLocalModule->m_aDynamicEntries; ++k)
            {
                if (pThreadLocalModule->m_pDynamicClassTable[k].m_pDynamicEntry != NULL)
                {
                    delete pThreadLocalModule->m_pDynamicClassTable[k].m_pDynamicEntry;
                    pThreadLocalModule->m_pDynamicClassTable[k].m_pDynamicEntry = NULL;
                }
            }
            delete pThreadLocalModule->m_pDynamicClassTable;
            pThreadLocalModule->m_pDynamicClassTable = NULL;
        }

        delete pThreadLocalModule;
    }
}

void ThreadLocalBlock::FreeTable()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    // Free the TLM table
    if (m_pTLMTable != NULL)
    {
        // Iterate over the table and free each TLM
        for (SIZE_T i = 0; i < m_TLMTableSize; ++i)
        {
            if (m_pTLMTable[i].pTLM != NULL)
            {
                FreeTLM(i);
            }
        }

        // Free the table itself
        delete m_pTLMTable;
        m_pTLMTable = NULL;
    }

    // Set table size to zero
    m_TLMTableSize = 0;

    // Free the ThreadStaticHandleTable
    if (m_pThreadStaticHandleTable != NULL)
    {
        delete m_pThreadStaticHandleTable;
        m_pThreadStaticHandleTable = NULL;
    }

    // Free any pinning handles we may have created
    FreePinningHandles();
}

void ThreadLocalBlock::EnsureModuleIndex(ModuleIndex index)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (m_TLMTableSize > index.m_dwIndex)
    {
        _ASSERTE(m_pTLMTable != NULL);
        return;
    }

    SIZE_T aModuleIndices = max(16, m_TLMTableSize);
    while (aModuleIndices <= index.m_dwIndex)
    {
        aModuleIndices *= 2;
    }

    // If this allocation fails, we will throw. If it succeeds,
    // then we are good to go
    PTR_TLMTableEntry pNewModuleSlots = (PTR_TLMTableEntry) (void*) new BYTE[sizeof(TLMTableEntry) * aModuleIndices];

    // Zero out the new TLM table
    memset(pNewModuleSlots, 0 , sizeof(TLMTableEntry) * aModuleIndices);

    if (m_pTLMTable != NULL)
    {
        memcpy(pNewModuleSlots, m_pTLMTable, sizeof(TLMTableEntry) * m_TLMTableSize);
    }
    else
    {
        _ASSERTE(m_TLMTableSize == 0);
    }

    PTR_TLMTableEntry pOldModuleSlots = m_pTLMTable;
    
    m_pTLMTable = pNewModuleSlots;
    m_TLMTableSize = aModuleIndices;

    if (pOldModuleSlots != NULL)
        delete pOldModuleSlots;
}

#endif

void ThreadLocalBlock::SetModuleSlot(ModuleIndex index, PTR_ThreadLocalModule pLocalModule) 
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    // This method will not grow the table. You need to grow
    // the table explicitly before calling SetModuleSlot()

    _ASSERTE(index.m_dwIndex < m_TLMTableSize);

    m_pTLMTable[index.m_dwIndex].pTLM = pLocalModule;
}

#ifdef DACCESS_COMPILE

void
ThreadLocalModule::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // Enumerate the ThreadLocalModule itself. TLMs are allocated to be larger than
    // sizeof(ThreadLocalModule) to make room for ClassInit flags and non-GC statics.
    // "DAC_ENUM_DTHIS()" probably does not account for this, so we might not enumerate
    // all of the ClassInit flags and non-GC statics.
    DAC_ENUM_DTHIS();

    if (m_pDynamicClassTable != NULL)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(m_pDynamicClassTable),
                            m_aDynamicEntries * sizeof(DynamicClassInfo));

        for (SIZE_T i = 0; i < m_aDynamicEntries; i++)
        {
            PTR_DynamicEntry entry = dac_cast<PTR_DynamicEntry>(m_pDynamicClassTable[i].m_pDynamicEntry);
            if (entry.IsValid())
            {
                entry.EnumMem();
            }
        }
    }
}

void
ThreadLocalBlock::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    // Enumerate the ThreadLocalBlock itself
    DAC_ENUM_DTHIS();

    if (m_pTLMTable.IsValid())
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(m_pTLMTable),
                            m_TLMTableSize * sizeof(TADDR));

        for (SIZE_T i = 0; i < m_TLMTableSize; i++)
        {
            PTR_ThreadLocalModule domMod = m_pTLMTable[i].pTLM;
            if (domMod.IsValid())
            {
                domMod->EnumMemoryRegions(flags);
            }
        }
    }
}

#endif

DWORD ThreadLocalModule::GetClassFlags(MethodTable* pMT, DWORD iClassIndex) // iClassIndex defaults to (DWORD)-1
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    } CONTRACTL_END;

    if (pMT->IsDynamicStatics())
    {
        DWORD dynamicClassID = pMT->GetModuleDynamicEntryID();
        if(m_aDynamicEntries <= dynamicClassID)
            return FALSE;
        return (m_pDynamicClassTable[dynamicClassID].m_dwFlags);
    }
    else
    {
        if (iClassIndex == (DWORD)-1)
            iClassIndex = pMT->GetClassIndex();
        return GetPrecomputedStaticsClassData()[iClassIndex];
    }
}

#ifndef DACCESS_COMPILE

void ThreadLocalModule::SetClassFlags(MethodTable* pMT, DWORD dwFlags)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (pMT->IsDynamicStatics())
    {
        DWORD dwID = pMT->GetModuleDynamicEntryID();
        EnsureDynamicClassIndex(dwID);
        m_pDynamicClassTable[dwID].m_dwFlags |= dwFlags;
    }
    else
    {
        GetPrecomputedStaticsClassData()[pMT->GetClassIndex()] |= dwFlags;
    }
}

void ThreadLocalBlock::AddPinningHandleToList(OBJECTHANDLE oh)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    ObjectHandleList::NodeType* pNewNode = new ObjectHandleList::NodeType(oh);
    m_PinningHandleList.LinkHead(pNewNode);
}

void ThreadLocalBlock::FreePinningHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    // Destroy all pinning handles in the list, and free the nodes
    ObjectHandleList::NodeType* pHandleNode; 
    while ((pHandleNode = m_PinningHandleList.UnlinkHead()) != NULL)
    {
        DestroyPinningHandle(pHandleNode->data);
        delete pHandleNode;
    }
}

void ThreadLocalBlock::AllocateThreadStaticHandles(Module * pModule, PTR_ThreadLocalModule pThreadLocalModule)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(pThreadLocalModule->GetPrecomputedGCStaticsBaseHandleAddress() != NULL);
    _ASSERTE(pThreadLocalModule->GetPrecomputedGCStaticsBaseHandle() == NULL);

    if (pModule->GetNumGCThreadStaticHandles() > 0)
    {
        AllocateStaticFieldObjRefPtrs(pModule->GetNumGCThreadStaticHandles(),
                                      pThreadLocalModule->GetPrecomputedGCStaticsBaseHandleAddress());

        // We should throw if we fail to allocate and never hit this assert
        _ASSERTE(pThreadLocalModule->GetPrecomputedGCStaticsBaseHandle() != NULL);
        _ASSERTE(pThreadLocalModule->GetPrecomputedGCStaticsBasePointer() != NULL);
    }
}

OBJECTHANDLE ThreadLocalBlock::AllocateStaticFieldObjRefPtrs(int nRequested, OBJECTHANDLE * ppLazyAllocate)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION((nRequested > 0));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (ppLazyAllocate && *ppLazyAllocate)
    {
        // Allocation already happened
        return *ppLazyAllocate;
    }

    // Make sure the large heap handle table is initialized.
    if (!m_pThreadStaticHandleTable)
        InitThreadStaticHandleTable();

    // Allocate the handles.
    OBJECTHANDLE result = m_pThreadStaticHandleTable->AllocateHandles(nRequested);

    if (ppLazyAllocate)
    {
        *ppLazyAllocate = result;
    }

    return result;
}

void ThreadLocalBlock::InitThreadStaticHandleTable()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_pThreadStaticHandleTable==NULL);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    
    // If the allocation fails this will throw; callers need
    // to account for this possibility
    m_pThreadStaticHandleTable = new ThreadStaticHandleTable(GetAppDomain());
}

void ThreadLocalBlock::AllocateThreadStaticBoxes(MethodTable * pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pMT->GetNumBoxedThreadStatics() > 0);
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    FieldDesc *pField = pMT->HasGenericsStaticsInfo() ? 
        pMT->GetGenericsStaticFieldDescs() : (pMT->GetApproxFieldDescListRaw() + pMT->GetNumIntroducedInstanceFields());

    // Move pField to point to the list of thread statics
    pField += pMT->GetNumStaticFields() - pMT->GetNumThreadStaticFields();

    FieldDesc *pFieldEnd = pField + pMT->GetNumThreadStaticFields();

    while (pField < pFieldEnd)
    {
        _ASSERTE(pField->IsThreadStatic());

        // We only care about thread statics which are value types
        if (pField->IsByValue())
        {
            TypeHandle  th = pField->GetFieldTypeHandleThrowing();
            MethodTable* pFieldMT = th.GetMethodTable();

            // AllocateStaticBox will pin this object if this class is FixedAddressVTStatics.
            // We save this pinning handle in a list attached to the ThreadLocalBlock. When
            // the thread dies, we release all the pinning handles in the list.

            OBJECTHANDLE handle;
            OBJECTREF obj = MethodTable::AllocateStaticBox(pFieldMT, pMT->HasFixedAddressVTStatics(), &handle);

            PTR_BYTE pStaticBase = pMT->GetGCThreadStaticsBasePointer();
            _ASSERTE(pStaticBase != NULL);

            SetObjectReference( (OBJECTREF*)(pStaticBase + pField->GetOffset()), obj, GetAppDomain() );

            // If we created a pinning handle, save it to the list
            if (handle != NULL)
                AddPinningHandleToList(handle);
        }

        pField++;
    }
}

#endif

#ifndef DACCESS_COMPILE

void    ThreadLocalModule::EnsureDynamicClassIndex(DWORD dwID)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (dwID < m_aDynamicEntries)
    {
        _ASSERTE(m_pDynamicClassTable != NULL);
        return;
    }

    SIZE_T aDynamicEntries = max(16, m_aDynamicEntries);
    while (aDynamicEntries <= dwID)
    {
        aDynamicEntries *= 2;
    }

    DynamicClassInfo* pNewDynamicClassTable;

    // If this allocation fails, we throw. If it succeeds,
    // then we are good to go
    pNewDynamicClassTable = (DynamicClassInfo*)(void*)new BYTE[sizeof(DynamicClassInfo) * aDynamicEntries];

    // Zero out the dynamic class table
    memset(pNewDynamicClassTable, 0, sizeof(DynamicClassInfo) * aDynamicEntries);

    // We might always be guaranteed that this will be non-NULL, but just to be safe
    if (m_pDynamicClassTable != NULL)
    {
        memcpy(pNewDynamicClassTable, m_pDynamicClassTable, sizeof(DynamicClassInfo) * m_aDynamicEntries);
    }
    else
    {
        _ASSERTE(m_aDynamicEntries == 0);
    }

    _ASSERTE(m_aDynamicEntries%2 == 0);

    DynamicClassInfo* pOldDynamicClassTable = m_pDynamicClassTable;

    m_pDynamicClassTable = pNewDynamicClassTable;
    m_aDynamicEntries = aDynamicEntries;

    if (pOldDynamicClassTable != NULL)
        delete pOldDynamicClassTable;
}

void    ThreadLocalModule::AllocateDynamicClass(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    _ASSERTE(!pMT->IsSharedByGenericInstantiations());
    _ASSERTE(pMT->IsDynamicStatics());

    DWORD dwID = pMT->GetModuleDynamicEntryID();

    EnsureDynamicClassIndex(dwID);

    _ASSERTE(m_aDynamicEntries > dwID);

    EEClass *pClass = pMT->GetClass();
    DWORD dwStaticBytes = pClass->GetNonGCThreadStaticFieldBytes();
    DWORD dwNumHandleStatics = pClass->GetNumHandleThreadStatics();

    _ASSERTE(!IsClassAllocated(pMT));
    _ASSERTE(!IsClassInitialized(pMT));
    _ASSERTE(!IsClassInitError(pMT));

    DynamicEntry *pDynamicStatics = m_pDynamicClassTable[dwID].m_pDynamicEntry;

    // We need this check because maybe a class had a cctor but no statics
    if (dwStaticBytes > 0 || dwNumHandleStatics > 0)
    {
        // Collectible types do not support static fields yet
        if (pMT->Collectible())
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleNotYet"));

        if (pDynamicStatics == NULL)
        {            
            // If this allocation fails, we will throw
            pDynamicStatics = (DynamicEntry*)new BYTE[sizeof(DynamicEntry) + dwStaticBytes];

#ifdef FEATURE_64BIT_ALIGNMENT
            // The memory block has be aligned at MAX_PRIMITIVE_FIELD_SIZE to guarantee alignment of statics
            static_assert_no_msg(sizeof(DynamicEntry) % MAX_PRIMITIVE_FIELD_SIZE == 0);
            _ASSERTE(IS_ALIGNED(pDynamicStatics, MAX_PRIMITIVE_FIELD_SIZE));
#endif

            // Zero out the new DynamicEntry
            memset((BYTE*)pDynamicStatics, 0, sizeof(DynamicEntry) + dwStaticBytes);

            // Save the DynamicEntry in the DynamicClassTable
            m_pDynamicClassTable[dwID].m_pDynamicEntry = pDynamicStatics;
        }

        if (dwNumHandleStatics > 0)
        {
            PTR_ThreadLocalBlock pThreadLocalBlock = GetThread()->m_pThreadLocalBlock;
            _ASSERTE(pThreadLocalBlock != NULL);
            pThreadLocalBlock->AllocateStaticFieldObjRefPtrs(dwNumHandleStatics,
                                                             &pDynamicStatics->m_pGCStatics);
        }
    }
}

void ThreadLocalModule::PopulateClass(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    _ASSERTE(this != NULL);
    _ASSERTE(pMT != NULL);
    _ASSERTE(!IsClassAllocated(pMT));

    // If this is a dynamic class then we need to allocate
    // an entry in our dynamic class table
    if (pMT->IsDynamicStatics())
        AllocateDynamicClass(pMT);

    // We need to allocate boxes any value-type statics that are not
    // primitives or enums, because these statics may contain references
    // to objects on the GC heap
    if (pMT->GetNumBoxedThreadStatics() > 0)
    {
        PTR_ThreadLocalBlock pThreadLocalBlock = ThreadStatics::GetCurrentTLB();
        _ASSERTE(pThreadLocalBlock != NULL);
        pThreadLocalBlock->AllocateThreadStaticBoxes(pMT);
    }

    // Mark the class as allocated
    SetClassAllocated(pMT);
}

PTR_ThreadLocalModule ThreadStatics::AllocateAndInitTLM(ModuleIndex index, PTR_ThreadLocalBlock pThreadLocalBlock, Module * pModule) //static
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    pThreadLocalBlock->EnsureModuleIndex(index);

    _ASSERTE(pThreadLocalBlock != NULL);
    _ASSERTE(pModule != NULL);

    NewHolder<ThreadLocalModule> pThreadLocalModule = AllocateTLM(pModule);

    pThreadLocalBlock->AllocateThreadStaticHandles(pModule, pThreadLocalModule);

    pThreadLocalBlock->SetModuleSlot(index, pThreadLocalModule);
    pThreadLocalModule.SuppressRelease();

    return pThreadLocalModule;
}


PTR_ThreadLocalModule ThreadStatics::GetTLM(ModuleIndex index, Module * pModule) //static
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // Get the TLM if it already exists
    PTR_ThreadLocalModule pThreadLocalModule = ThreadStatics::GetTLMIfExists(index);

    // If the TLM does not exist, create it now
    if (pThreadLocalModule == NULL)
    {
        // Get the current ThreadLocalBlock
        PTR_ThreadLocalBlock pThreadLocalBlock = ThreadStatics::GetCurrentTLB();
        _ASSERTE(pThreadLocalBlock != NULL);

        // Allocate and initialize the TLM, and add it to the TLB's table
        pThreadLocalModule = AllocateAndInitTLM(index, pThreadLocalBlock, pModule);
    }

    return pThreadLocalModule;
}

PTR_ThreadLocalModule ThreadStatics::GetTLM(MethodTable * pMT) //static
{
    Module * pModule = pMT->GetModuleForStatics();
    return GetTLM(pModule->GetModuleIndex(), pModule);
}

PTR_ThreadLocalBlock ThreadStatics::AllocateTLB(PTR_Thread pThread, ADIndex index) //static
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE(pThread->m_pThreadLocalBlock == NULL);

    // Grow the TLB table so that it has room to store the newly allocated 
    // ThreadLocalBlock. If this growing the table fails we cannot proceed.
    ThreadStatics::EnsureADIndex(pThread, index);

    // Allocate a new TLB and update this Thread's pointer to the current 
    // ThreadLocalBlock. Constructor zeroes out everything for us.
    pThread->m_pThreadLocalBlock = new ThreadLocalBlock();

    // Store the newly allocated ThreadLocalBlock in the TLB table
    if (pThread->m_pThreadLocalBlock != NULL)
    {
        // We grew the TLB table earlier, so it should have room
        _ASSERTE(index.m_dwIndex >= 0 && index.m_dwIndex < pThread->m_TLBTableSize);
        pThread->m_pTLBTable[index.m_dwIndex] = pThread->m_pThreadLocalBlock;
    }

    return pThread->m_pThreadLocalBlock;
}

PTR_ThreadLocalModule ThreadStatics::AllocateTLM(Module * pModule)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    SIZE_T size = pModule->GetThreadLocalModuleSize();

    _ASSERTE(size >= ThreadLocalModule::OffsetOfDataBlob());

    PTR_ThreadLocalModule pThreadLocalModule = (ThreadLocalModule*)new BYTE[size];

    // We guarantee alignment for 64-bit regular thread statics on 32-bit platforms even without FEATURE_64BIT_ALIGNMENT for performance reasons.

    // The memory block has to be aligned at MAX_PRIMITIVE_FIELD_SIZE to guarantee alignment of statics
    _ASSERTE(IS_ALIGNED(pThreadLocalModule, MAX_PRIMITIVE_FIELD_SIZE));
    
    // Zero out the part of memory where the TLM resides
    memset(pThreadLocalModule, 0, size);
    
    return pThreadLocalModule;
}

#endif

PTR_ThreadLocalBlock ThreadStatics::GetTLBIfExists(PTR_Thread pThread, ADIndex index) //static
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // Check to see if we have a ThreadLocalBlock for the this AppDomain, 
    if (index.m_dwIndex < pThread->m_TLBTableSize)
    {
        return pThread->m_pTLBTable[index.m_dwIndex];
    }
    
    return NULL;
}
