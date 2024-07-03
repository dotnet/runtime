// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "threadstatics.h"

struct InFlightTLSData
{
#ifndef DACCESS_COMPILE
    InFlightTLSData(TLSIndex index) : pNext(NULL), tlsIndex(index), hTLSData(0) { }
    ~InFlightTLSData()
    {
        if (!IsHandleNullUnchecked(hTLSData))
        {
            DestroyTypedHandle(hTLSData);
        }
    }
#endif // !DACCESS_COMPILE
    PTR_InFlightTLSData pNext; // Points at the next in-flight TLS data
    TLSIndex tlsIndex; // The TLS index for the static
    OBJECTHANDLE hTLSData; // The TLS data for the static
};


struct ThreadLocalLoaderAllocator
{
    ThreadLocalLoaderAllocator* pNext; // Points at the next thread local loader allocator
    LoaderAllocator* pLoaderAllocator; // The loader allocator that has a TLS used in this thread
};
typedef DPTR(ThreadLocalLoaderAllocator) PTR_ThreadLocalLoaderAllocator;

#ifndef DACCESS_COMPILE
static TLSIndexToMethodTableMap *g_pThreadStaticCollectibleTypeIndices;
static TLSIndexToMethodTableMap *g_pThreadStaticNonCollectibleTypeIndices;
static PTR_MethodTable g_pMethodTablesForDirectThreadLocalData[offsetof(ThreadLocalData, ExtendedDirectThreadLocalTLSData) - offsetof(ThreadLocalData, ThreadBlockingInfo_First) + EXTENDED_DIRECT_THREAD_LOCAL_SIZE];

static Volatile<uint8_t> s_GCsWhichDoRelocateAndCanEmptyOutTheTLSIndices = 0;
static uint32_t g_NextTLSSlot = 1;
static uint32_t g_NextNonCollectibleTlsSlot = NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY;
static uint32_t g_directThreadLocalTLSBytesAvailable = EXTENDED_DIRECT_THREAD_LOCAL_SIZE;

static CrstStatic g_TLSCrst;
#endif

// This can be used for out of thread access to TLS data.
PTR_VOID GetThreadLocalStaticBaseNoCreate(Thread* pThread, TLSIndex index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    TADDR pTLSBaseAddress = (TADDR)NULL;

    // The only lock we take here is this spin lock, which is safe to take even when the GC is running.
    // The only time it isn't safe is if target thread is suspended by an OS primitive without
    // going through the suspend thread routine of the runtime itself.
    BEGIN_CONTRACT_VIOLATION(TakesLockViolation);
#ifndef DACCESS_COMPILE
    // Since this api can be used from a different thread, we need a lock to keep it all safe
    SpinLockHolder spinLock(&pThread->m_TlsSpinLock);
#endif

    PTR_ThreadLocalData pThreadLocalData = pThread->GetThreadLocalDataPtr();
    if (pThreadLocalData != NULL)
    {
        if (index.GetTLSIndexType() == TLSIndexType::NonCollectible)
        {
            PTR_ArrayBase tlsArray = (PTR_ArrayBase)pThreadLocalData->pNonCollectibleTlsArrayData;
            if (pThreadLocalData->cNonCollectibleTlsData > index.GetIndexOffset())
            {
                size_t arrayIndex = index.GetIndexOffset() - NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY;
                TADDR arrayTargetAddress = dac_cast<TADDR>(tlsArray) + offsetof(PtrArray, m_Array);
#ifdef DACCESS_COMPILE
                __ArrayDPtr<_UNCHECKED_OBJECTREF> targetArray = dac_cast< __ArrayDPtr<_UNCHECKED_OBJECTREF> >(arrayTargetAddress);
#else
                _UNCHECKED_OBJECTREF* targetArray = reinterpret_cast<_UNCHECKED_OBJECTREF*>(arrayTargetAddress);
#endif
                pTLSBaseAddress = dac_cast<TADDR>(targetArray[arrayIndex]);
            }
        }
        else if (index.GetTLSIndexType() == TLSIndexType::DirectOnThreadLocalData)
        {
            pTLSBaseAddress = dac_cast<TADDR>((dac_cast<TADDR>(pThreadLocalData)) + index.GetIndexOffset());
        }
        else
        {
            int32_t cCollectibleTlsData = pThreadLocalData->cCollectibleTlsData;
            if (cCollectibleTlsData > index.GetIndexOffset())
            {
                TADDR pCollectibleTlsArrayData = dac_cast<TADDR>(pThreadLocalData->pCollectibleTlsArrayData);
                pCollectibleTlsArrayData += index.GetIndexOffset() * sizeof(TADDR);
                OBJECTHANDLE objHandle = *dac_cast<DPTR(OBJECTHANDLE)>(pCollectibleTlsArrayData);
                if (!IsHandleNullUnchecked(objHandle))
                {
                    pTLSBaseAddress = dac_cast<TADDR>(ObjectFromHandleUnchecked(objHandle));
                }
            }
        }
        if (pTLSBaseAddress == (TADDR)NULL)
        {
            // Maybe it is in the InFlightData
            PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
            while (pInFlightData != NULL)
            {
                if (pInFlightData->tlsIndex == index)
                {
                    pTLSBaseAddress = dac_cast<TADDR>(ObjectFromHandleUnchecked(pInFlightData->hTLSData));
                    break;
                }
                pInFlightData = pInFlightData->pNext;
            }
        }
    }

    END_CONTRACT_VIOLATION;

    return dac_cast<PTR_VOID>(pTLSBaseAddress);
}

#ifndef DACCESS_COMPILE
int32_t IndexOffsetToDirectThreadLocalIndex(int32_t indexOffset)
{
    LIMITED_METHOD_CONTRACT;

    int32_t adjustedIndexOffset = indexOffset + OFFSETOF__CORINFO_Array__data;
    _ASSERTE(((uint32_t)adjustedIndexOffset) >= offsetof(ThreadLocalData, ThreadBlockingInfo_First));
    int32_t directThreadLocalIndex = adjustedIndexOffset - offsetof(ThreadLocalData, ThreadBlockingInfo_First);
    _ASSERTE(((uint32_t)directThreadLocalIndex) < (sizeof(g_pMethodTablesForDirectThreadLocalData) / sizeof(g_pMethodTablesForDirectThreadLocalData[0])));
    _ASSERTE(directThreadLocalIndex >= 0);
    return directThreadLocalIndex;
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
PTR_MethodTable LookupMethodTableForThreadStaticKnownToBeAllocated(TLSIndex index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (index.GetTLSIndexType() == TLSIndexType::NonCollectible)
    {
        return g_pThreadStaticNonCollectibleTypeIndices->LookupTlsIndexKnownToBeAllocated(index);
    }
    else if (index.GetTLSIndexType() == TLSIndexType::DirectOnThreadLocalData)
    {
        return VolatileLoadWithoutBarrier(&g_pMethodTablesForDirectThreadLocalData[IndexOffsetToDirectThreadLocalIndex(index.GetIndexOffset())]);
    }
    else
    {
        return g_pThreadStaticCollectibleTypeIndices->LookupTlsIndexKnownToBeAllocated(index);
    }
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
PTR_MethodTable LookupMethodTableAndFlagForThreadStatic(TLSIndex index, bool *pIsGCStatic, bool *pIsCollectible)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PTR_MethodTable retVal;
    if (index.GetTLSIndexType() == TLSIndexType::NonCollectible)
    {
        retVal = g_pThreadStaticNonCollectibleTypeIndices->Lookup(index, pIsGCStatic, pIsCollectible);
    }
    else if (index.GetTLSIndexType() == TLSIndexType::DirectOnThreadLocalData)
    {
        *pIsGCStatic = false;
        *pIsCollectible = false;
        retVal = g_pMethodTablesForDirectThreadLocalData[IndexOffsetToDirectThreadLocalIndex(index.GetIndexOffset())];
    }
    else
    {
        retVal = g_pThreadStaticCollectibleTypeIndices->Lookup(index, pIsGCStatic, pIsCollectible);
    }
    return retVal;
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
void ScanThreadStaticRoots(Thread* pThread, promote_func* fn, ScanContext* sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (pThread->m_ThreadLocalDataPtr == NULL)
        return;

    ThreadLocalData *pThreadLocalData = pThread->m_ThreadLocalDataPtr;

    // Report non-collectible object array
    fn(&pThreadLocalData->pNonCollectibleTlsArrayData, sc, 0);
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE

void TLSIndexToMethodTableMap::Set(TLSIndex index, PTR_MethodTable pMT, bool isGCStatic)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (index.GetIndexOffset() >= m_maxIndex)
    {
        int32_t newSize = max(m_maxIndex, 16);
        while (index.GetIndexOffset() >= newSize)
        {
            newSize *= 2;
        }
        TADDR *newMap = new TADDR[newSize];
        memset(newMap, 0, sizeof(TADDR) * newSize);
        if (pMap != NULL)
        {
            memcpy(newMap, pMap, m_maxIndex * sizeof(TADDR));
            // Don't delete the old map in case some other thread is reading from it, this won't waste significant amounts of memory, since this map cannot grow indefinitely
        }
        pMap = newMap;
        m_maxIndex = newSize;
    }

    TADDR rawValue = dac_cast<TADDR>(pMT);
    if (isGCStatic)
    {
        rawValue |= IsGCFlag();
    }
    if (pMT->Collectible())
    {
        rawValue |= IsCollectibleFlag();
        m_collectibleEntries++;
    }
    _ASSERTE(pMap[index.GetIndexOffset()] == 0 || IsClearedValue(pMap[index.GetIndexOffset()]));
    pMap[index.GetIndexOffset()] = rawValue;
}

void TLSIndexToMethodTableMap::Clear(TLSIndex index, uint8_t whenCleared)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(index.GetIndexOffset() < m_maxIndex);
    TADDR rawValue = pMap[index.GetIndexOffset()];
    _ASSERTE(rawValue & IsCollectibleFlag());
    if (rawValue & IsCollectibleFlag())
    {
        m_collectibleEntries--;
    }
    pMap[index.GetIndexOffset()] = (whenCleared << 2) | 0x3;
    _ASSERTE(GetClearedMarker(pMap[index.GetIndexOffset()]) == whenCleared);
    _ASSERTE(IsClearedValue(pMap[index.GetIndexOffset()]));
}

bool TLSIndexToMethodTableMap::FindClearedIndex(uint8_t whenClearedMarkerToAvoid, TLSIndex* pIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    for (const auto& entry : *this)
    {
        if (entry.IsClearedValue)
        {
            uint8_t whenClearedMarker = entry.ClearedMarker;
            if ((whenClearedMarker == whenClearedMarkerToAvoid) ||
                (whenClearedMarker == (whenClearedMarkerToAvoid - 1)) ||
                (whenClearedMarker == (whenClearedMarkerToAvoid - 2)))
            {
                // Make sure we are not within 2 of the marker we are trying to avoid
                // Use multiple compares instead of trying to fuss around with the overflow style comparisons
                continue;
            }
            *pIndex = entry.TlsIndex;
            return true;
        }
    }
    return false;
}

void InitializeThreadStaticData()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    g_pThreadStaticCollectibleTypeIndices = new TLSIndexToMethodTableMap(TLSIndexType::NonCollectible);
    g_pThreadStaticNonCollectibleTypeIndices = new TLSIndexToMethodTableMap(TLSIndexType::NonCollectible);
    g_TLSCrst.Init(CrstThreadLocalStorageLock, CRST_UNSAFE_ANYMODE);
}

void InitializeCurrentThreadsStaticData(Thread* pThread)
{
    LIMITED_METHOD_CONTRACT;

    t_ThreadStatics.pThread = pThread;
    t_ThreadStatics.pThread->m_ThreadLocalDataPtr = &t_ThreadStatics;
    t_ThreadStatics.pThread->m_TlsSpinLock.Init(LOCK_TLSDATA, FALSE);
}

void AllocateThreadStaticBoxes(MethodTable *pMT, PTRARRAYREF *ppRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pMT->HasBoxedThreadStatics());
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

            OBJECTREF obj = MethodTable::AllocateStaticBox(pFieldMT, pMT->HasFixedAddressVTStatics());
            uint8_t *pBase = (uint8_t*)OBJECTREFToObject(*ppRef);
            SetObjectReference((OBJECTREF*)(pBase + pField->GetOffset()), obj);
        }

        pField++;
    }
}

void FreeLoaderAllocatorHandlesForTLSData(Thread *pThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (pThread->cLoaderHandles > 0)
    {
        CrstHolder ch(&g_TLSCrst);
#ifdef _DEBUG
        bool allRemainingIndicesAreNotValid = false;
#endif
        for (const auto& entry : g_pThreadStaticCollectibleTypeIndices->CollectibleEntries())
        {
            _ASSERTE((entry.TlsIndex.GetIndexOffset() < pThread->cLoaderHandles) || allRemainingIndicesAreNotValid);
            if (entry.TlsIndex.GetIndexOffset() >= pThread->cLoaderHandles)
            {
#ifndef _DEBUG
                break;
#else
                allRemainingIndicesAreNotValid = true;
#endif
            }
            else
            {
                if (pThread->pLoaderHandles[entry.TlsIndex.GetIndexOffset()] != (LOADERHANDLE)NULL)
                {
                    entry.pMT->GetLoaderAllocator()->FreeHandle(pThread->pLoaderHandles[entry.TlsIndex.GetIndexOffset()]);
                    pThread->pLoaderHandles[entry.TlsIndex.GetIndexOffset()] = (LOADERHANDLE)NULL;
                }
            }
        }
    }
}

void AssertThreadStaticDataFreed()
{
    LIMITED_METHOD_CONTRACT;

    ThreadLocalData *pThreadLocalData = &t_ThreadStatics;

    _ASSERTE(pThreadLocalData->pThread == NULL);
    _ASSERTE(pThreadLocalData->pCollectibleTlsArrayData == NULL);
    _ASSERTE(pThreadLocalData->cCollectibleTlsData == 0);
    _ASSERTE(pThreadLocalData->pNonCollectibleTlsArrayData == NULL);
    _ASSERTE(pThreadLocalData->cNonCollectibleTlsData == 0);
    _ASSERTE(pThreadLocalData->pInFlightData == NULL);
}

void FreeThreadStaticData(Thread* pThread)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    SpinLockHolder spinLock(&pThread->m_TlsSpinLock);

    ThreadLocalData *pThreadLocalData = &t_ThreadStatics;

    for (int32_t iTlsSlot = 0; iTlsSlot < pThreadLocalData->cCollectibleTlsData; ++iTlsSlot)
    {
        if (!IsHandleNullUnchecked(pThreadLocalData->pCollectibleTlsArrayData[iTlsSlot]))
        {
            DestroyLongWeakHandle(pThreadLocalData->pCollectibleTlsArrayData[iTlsSlot]);
        }
    }

    delete[] (uint8_t*)pThreadLocalData->pCollectibleTlsArrayData;

    pThreadLocalData->pCollectibleTlsArrayData = 0;
    pThreadLocalData->cCollectibleTlsData = 0;
    pThreadLocalData->pNonCollectibleTlsArrayData = 0;
    pThreadLocalData->cNonCollectibleTlsData = 0;

    while (pThreadLocalData->pInFlightData != NULL)
    {
        InFlightTLSData* pInFlightData = pThreadLocalData->pInFlightData;
        pThreadLocalData->pInFlightData = pInFlightData->pNext;
        delete pInFlightData;
    }

    _ASSERTE(pThreadLocalData->pThread == pThread);
    pThreadLocalData->pThread = NULL;
}

void SetTLSBaseValue(TADDR *ppTLSBaseAddress, TADDR pTLSBaseAddress, bool useGCBarrierInsteadOfHandleStore)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (useGCBarrierInsteadOfHandleStore)
    {
        SetObjectReference((OBJECTREF *)ppTLSBaseAddress, (OBJECTREF)ObjectToOBJECTREF((Object*)pTLSBaseAddress));
    }
    else
    {
        OBJECTHANDLE objHandle = (OBJECTHANDLE)ppTLSBaseAddress;
        StoreObjectInHandle(objHandle, (OBJECTREF)ObjectToOBJECTREF((Object*)pTLSBaseAddress));
    }
}

void* GetThreadLocalStaticBase(TLSIndex index)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    bool isGCStatic;
    bool isCollectible;
    bool staticIsNonCollectible = false;
    MethodTable *pMT = LookupMethodTableAndFlagForThreadStatic(index, &isGCStatic, &isCollectible);

    struct
    {
        TADDR *ppTLSBaseAddress = NULL;
        TADDR pTLSBaseAddress = (TADDR)NULL;
    } gcBaseAddresses;
    GCPROTECT_BEGININTERIOR(gcBaseAddresses);

    if (index.GetTLSIndexType() == TLSIndexType::NonCollectible)
    {
        PTRARRAYREF tlsArray = (PTRARRAYREF)UNCHECKED_OBJECTREF_TO_OBJECTREF(t_ThreadStatics.pNonCollectibleTlsArrayData);
        if (t_ThreadStatics.cNonCollectibleTlsData <= index.GetIndexOffset())
        {
            GCPROTECT_BEGIN(tlsArray);
            PTRARRAYREF tlsArrayNew = (PTRARRAYREF)AllocateObjectArray(index.GetIndexOffset() + 8, g_pObjectClass);
            if (tlsArray != NULL)
            {
                for (DWORD i = 0; i < tlsArray->GetNumComponents(); i++)
                {
                    tlsArrayNew->SetAt(i, tlsArray->GetAt(i));
                }
            }
            t_ThreadStatics.pNonCollectibleTlsArrayData = OBJECTREF_TO_UNCHECKED_OBJECTREF(tlsArrayNew);
            tlsArray = tlsArrayNew;
            t_ThreadStatics.cNonCollectibleTlsData = tlsArrayNew->GetNumComponents() + NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY;
            GCPROTECT_END();
        }
        gcBaseAddresses.ppTLSBaseAddress = (TADDR*)(tlsArray->GetDataPtr() + (index.GetIndexOffset() - NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY)) ;
        staticIsNonCollectible = true;
        gcBaseAddresses.pTLSBaseAddress = *gcBaseAddresses.ppTLSBaseAddress;
    }
    else if (index.GetTLSIndexType() == TLSIndexType::DirectOnThreadLocalData)
    {
        // All of the current cases are non GC static, non-collectible
        _ASSERTE(!isGCStatic);
        _ASSERTE(!isCollectible);
        gcBaseAddresses.pTLSBaseAddress = ((TADDR)&t_ThreadStatics) + index.GetIndexOffset();
    }
    else
    {
        int32_t cCollectibleTlsData = t_ThreadStatics.cCollectibleTlsData;
        if (cCollectibleTlsData <= index.GetIndexOffset())
        {
            // Grow the underlying TLS array
            SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
            int32_t newcCollectibleTlsData = index.GetIndexOffset() + 8; // Leave a bit of margin
            OBJECTHANDLE* pNewTLSArrayData = new OBJECTHANDLE[newcCollectibleTlsData];
            memset(pNewTLSArrayData, 0, newcCollectibleTlsData * sizeof(OBJECTHANDLE));
            if (cCollectibleTlsData > 0)
                memcpy(pNewTLSArrayData, (void*)t_ThreadStatics.pCollectibleTlsArrayData, cCollectibleTlsData * sizeof(OBJECTHANDLE));
            OBJECTHANDLE* pOldArray = (OBJECTHANDLE*)t_ThreadStatics.pCollectibleTlsArrayData;
            t_ThreadStatics.pCollectibleTlsArrayData = pNewTLSArrayData;
            cCollectibleTlsData = newcCollectibleTlsData;
            t_ThreadStatics.cCollectibleTlsData = cCollectibleTlsData;
            delete[] pOldArray;
        }

        if (isCollectible && t_ThreadStatics.pThread->cLoaderHandles <= index.GetIndexOffset())
        {
            // Grow the underlying TLS array
            SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
            int32_t cNewTLSLoaderHandles = index.GetIndexOffset() + 8; // Leave a bit of margin
            size_t cbNewTLSLoaderHandles = sizeof(LOADERHANDLE) * cNewTLSLoaderHandles;
            LOADERHANDLE* pNewTLSLoaderHandles = new LOADERHANDLE[cNewTLSLoaderHandles];
            memset(pNewTLSLoaderHandles, 0, cbNewTLSLoaderHandles);
            if (cCollectibleTlsData > 0)
                memcpy(pNewTLSLoaderHandles, (void*)t_ThreadStatics.pThread->pLoaderHandles, t_ThreadStatics.pThread->cLoaderHandles * sizeof(LOADERHANDLE));

            LOADERHANDLE* pOldArray = t_ThreadStatics.pThread->pLoaderHandles;
            t_ThreadStatics.pThread->pLoaderHandles = pNewTLSLoaderHandles;
            t_ThreadStatics.pThread->cLoaderHandles = cNewTLSLoaderHandles;
            delete[] pOldArray;
        }

        OBJECTHANDLE* pCollectibleTlsArrayData = t_ThreadStatics.pCollectibleTlsArrayData;
        pCollectibleTlsArrayData += index.GetIndexOffset();
        OBJECTHANDLE objHandle = *pCollectibleTlsArrayData;
        if (IsHandleNullUnchecked(objHandle))
        {
            objHandle = GetAppDomain()->CreateLongWeakHandle(NULL);
            *pCollectibleTlsArrayData = objHandle;
        }
        gcBaseAddresses.ppTLSBaseAddress = reinterpret_cast<TADDR*>(objHandle);
        gcBaseAddresses.pTLSBaseAddress = dac_cast<TADDR>(OBJECTREFToObject(ObjectFromHandle(objHandle)));
    }

    if (gcBaseAddresses.pTLSBaseAddress == (TADDR)NULL)
    {
        // Maybe it is in the InFlightData
        InFlightTLSData* pInFlightData = t_ThreadStatics.pInFlightData;
        InFlightTLSData** ppOldNextPtr = &t_ThreadStatics.pInFlightData;
        while (pInFlightData != NULL)
        {
            if (pInFlightData->tlsIndex == index)
            {
                gcBaseAddresses.pTLSBaseAddress = dac_cast<TADDR>(OBJECTREFToObject(ObjectFromHandle(pInFlightData->hTLSData)));
                if (pMT->IsClassInited())
                {
                    SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
                    SetTLSBaseValue(gcBaseAddresses.ppTLSBaseAddress, gcBaseAddresses.pTLSBaseAddress, staticIsNonCollectible);
                    *ppOldNextPtr = pInFlightData->pNext;
                    delete pInFlightData;
                }
                break;
            }
            ppOldNextPtr = &pInFlightData->pNext;
            pInFlightData = pInFlightData->pNext;
        }
        if (gcBaseAddresses.pTLSBaseAddress == (TADDR)NULL)
        {
            // Now we need to actually allocate the TLS data block
            struct 
            {
                PTRARRAYREF ptrRef;
                OBJECTREF tlsEntry;
            } gc;
            memset(&gc, 0, sizeof(gc));
            GCPROTECT_BEGIN(gc);
            if (isGCStatic)
            {
                gc.ptrRef = (PTRARRAYREF)AllocateObjectArray(pMT->GetClass()->GetNumHandleThreadStatics(), g_pObjectClass);
                if (pMT->HasBoxedThreadStatics())
                {
                    AllocateThreadStaticBoxes(pMT, &gc.ptrRef);
                }
                gc.tlsEntry = (OBJECTREF)gc.ptrRef;
            }
            else
            {
#ifndef TARGET_64BIT
                // On non 64 bit platforms, the static data may need to be 8 byte aligned to allow for good performance
                // for doubles and 64bit ints, come as close as possible, by simply allocating the data as a double array
                gc.tlsEntry = AllocatePrimitiveArray(ELEMENT_TYPE_R8, static_cast<DWORD>(AlignUp(pMT->GetClass()->GetNonGCThreadStaticFieldBytes(), 8)/8));
#else
                gc.tlsEntry = AllocatePrimitiveArray(ELEMENT_TYPE_I1, static_cast<DWORD>(pMT->GetClass()->GetNonGCThreadStaticFieldBytes()));
#endif
            }

            NewHolder<InFlightTLSData> pNewInFlightData = NULL;
            if (!pMT->IsClassInited() && pInFlightData == NULL)
            {
                pNewInFlightData = new InFlightTLSData(index);
                HandleType handleType = staticIsNonCollectible ? HNDTYPE_STRONG : HNDTYPE_WEAK_LONG;
                pNewInFlightData->hTLSData = GetAppDomain()->CreateTypedHandle(gc.tlsEntry, handleType);
                pInFlightData = pNewInFlightData;
            }

            if (isCollectible)
            {
                LOADERHANDLE *pLoaderHandle = t_ThreadStatics.pThread->pLoaderHandles + index.GetIndexOffset();
                // Note, that this can fail, but if it succeeds we don't have a holder in place to clean it up if future operations fail
                // Add such a holder if we ever add a possibly failing operation after this
                *pLoaderHandle = pMT->GetLoaderAllocator()->AllocateHandle(gc.tlsEntry);
            }

            // After this, we cannot fail
            pNewInFlightData.SuppressRelease();

            {
                GCX_FORBID();
                gcBaseAddresses.pTLSBaseAddress = (TADDR)OBJECTREFToObject(gc.tlsEntry);
                if (pInFlightData == NULL)
                {
                    SetTLSBaseValue(gcBaseAddresses.ppTLSBaseAddress, gcBaseAddresses.pTLSBaseAddress, staticIsNonCollectible);
                }
                else
                {
                    SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
                    pInFlightData->pNext = t_ThreadStatics.pInFlightData;
                    StoreObjectInHandle(pInFlightData->hTLSData, gc.tlsEntry);
                    t_ThreadStatics.pInFlightData = pInFlightData;
                }
            }
            GCPROTECT_END();
        }
    }
    GCPROTECT_END();
    _ASSERTE(gcBaseAddresses.pTLSBaseAddress != (TADDR)NULL);
    return reinterpret_cast<void*>(gcBaseAddresses.pTLSBaseAddress);
}

void GetTLSIndexForThreadStatic(MethodTable* pMT, bool gcStatic, TLSIndex* pIndex, uint32_t bytesNeeded)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_COOP();
    CrstHolder ch(&g_TLSCrst);
    if (pIndex->IsAllocated())
    {
        return;
    }

    TLSIndex newTLSIndex = TLSIndex::Unallocated();

    if (!pMT->Collectible())
    {
        bool usedDirectOnThreadLocalDataPath = false;

        if (!gcStatic && ((pMT == CoreLibBinder::GetClassIfExist(CLASS__THREAD_BLOCKING_INFO)) || ((g_directThreadLocalTLSBytesAvailable >= bytesNeeded) && (!pMT->HasClassConstructor() || pMT->IsClassInited()))))
        {
            if (pMT == CoreLibBinder::GetClassIfExist(CLASS__THREAD_BLOCKING_INFO))
            {
                newTLSIndex = TLSIndex(TLSIndexType::DirectOnThreadLocalData, offsetof(ThreadLocalData, ThreadBlockingInfo_First) - OFFSETOF__CORINFO_Array__data);
                usedDirectOnThreadLocalDataPath = true;
            }
            else
            {
                // This is a top down bump allocator that aligns data at the largest alignment that might be needed
                uint32_t newBytesAvailable = g_directThreadLocalTLSBytesAvailable - bytesNeeded;
                uint32_t indexOffsetWithoutAlignment = offsetof(ThreadLocalData, ExtendedDirectThreadLocalTLSData) - OFFSETOF__CORINFO_Array__data + newBytesAvailable;
                uint32_t alignment;
                if (bytesNeeded >= 8)
                    alignment = 8;
                if (bytesNeeded >= 4)
                    alignment = 4;
                else if (bytesNeeded >= 2)
                    alignment = 2;
                else 
                    alignment = 1;
                
                uint32_t actualIndexOffset = AlignDown(indexOffsetWithoutAlignment, alignment);
                uint32_t alignmentAdjust = indexOffsetWithoutAlignment - actualIndexOffset;
                if (alignmentAdjust <= newBytesAvailable)
                {
                    g_directThreadLocalTLSBytesAvailable = newBytesAvailable - alignmentAdjust;
                    newTLSIndex = TLSIndex(TLSIndexType::DirectOnThreadLocalData, actualIndexOffset);
                }
                usedDirectOnThreadLocalDataPath = true;
            }
            if (usedDirectOnThreadLocalDataPath)
                VolatileStoreWithoutBarrier(&g_pMethodTablesForDirectThreadLocalData[IndexOffsetToDirectThreadLocalIndex(newTLSIndex.GetIndexOffset())], pMT);
        }

        if (!usedDirectOnThreadLocalDataPath)
        {
            uint32_t tlsRawIndex = g_NextNonCollectibleTlsSlot++;
            newTLSIndex = TLSIndex(TLSIndexType::NonCollectible, tlsRawIndex);
            g_pThreadStaticNonCollectibleTypeIndices->Set(newTLSIndex, pMT, gcStatic);
        }
    }
    else
    {
        if (!g_pThreadStaticCollectibleTypeIndices->FindClearedIndex(s_GCsWhichDoRelocateAndCanEmptyOutTheTLSIndices, &newTLSIndex))
        {
            uint32_t tlsRawIndex = g_NextTLSSlot;
            newTLSIndex = TLSIndex(TLSIndexType::Collectible, tlsRawIndex);
            g_NextTLSSlot += 1;
        }

        g_pThreadStaticCollectibleTypeIndices->Set(newTLSIndex, pMT, gcStatic);
        pMT->GetLoaderAllocator()->GetTLSIndexList().Append(newTLSIndex);
    }

    *pIndex = newTLSIndex;
}

void FreeTLSIndicesForLoaderAllocator(LoaderAllocator *pLoaderAllocator)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_COOPERATIVE;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    CrstHolder ch(&g_TLSCrst);

    const auto& tlsIndicesToCleanup = pLoaderAllocator->GetTLSIndexList();
    COUNT_T current = 0;
    COUNT_T end = tlsIndicesToCleanup.GetCount();

    while (current != end)
    {
        g_pThreadStaticCollectibleTypeIndices->Clear(tlsIndicesToCleanup[current], s_GCsWhichDoRelocateAndCanEmptyOutTheTLSIndices);
        ++current;
    }
}

static void* GetTlsIndexObjectAddress();

#if !defined(TARGET_OSX) && defined(TARGET_UNIX) && defined(TARGET_ARM64)
extern "C" size_t GetTLSResolverAddress();
#endif // !TARGET_OSX && TARGET_UNIX && TARGET_ARM64

bool CanJITOptimizeTLSAccess()
{
    LIMITED_METHOD_CONTRACT;

    bool optimizeThreadStaticAccess = false;
#if defined(TARGET_ARM)
    // Optimization is disabled for linux/windows arm
#elif !defined(TARGET_WINDOWS) && defined(TARGET_X86)
    // Optimization is disabled for linux/x86
#elif defined(TARGET_LINUX_MUSL) && defined(TARGET_ARM64)
    // Optimization is disabled for linux musl arm64
#elif defined(TARGET_FREEBSD) && defined(TARGET_ARM64)
    // Optimization is disabled for FreeBSD/arm64
#elif defined(FEATURE_INTERPRETER)
    // Optimization is disabled when interpreter may be used
#elif !defined(TARGET_OSX) && defined(TARGET_UNIX) && defined(TARGET_ARM64)
    // Optimization is enabled for linux/arm64 only for static resolver.
    // For static resolver, the TP offset is same for all threads.
    // For dynamic resolver, TP offset returned is that of a JIT thread and
    // will be different for the executing thread.
    uint32_t* resolverAddress = reinterpret_cast<uint32_t*>(GetTLSResolverAddress());
    if (
        // nop or hint 32
        ((resolverAddress[0] == 0xd503201f) || (resolverAddress[0] == 0xd503241f)) &&
        // ldr x0, [x0, #8]
        (resolverAddress[1] == 0xf9400400) &&
        // ret
        (resolverAddress[2] == 0xd65f03c0)
    )
    {
        optimizeThreadStaticAccess = true;
    }
#else
    optimizeThreadStaticAccess = true;
#if !defined(TARGET_OSX) && defined(TARGET_UNIX) && defined(TARGET_AMD64)
    // For linux/x64, check if compiled coreclr as .so file and not single file.
    // For single file, the `tls_index` might not be accurate.
    // Do not perform this optimization in such case.
    optimizeThreadStaticAccess = GetTlsIndexObjectAddress() != nullptr;
#endif // !TARGET_OSX && TARGET_UNIX && TARGET_AMD64
#endif
    return optimizeThreadStaticAccess;
}

#ifndef _MSC_VER
extern "C" void* __tls_get_addr(void* ti);
#endif // !_MSC_VER

#if defined(TARGET_WINDOWS)
EXTERN_C uint32_t _tls_index;
/*********************************************************************/
static uint32_t ThreadLocalOffset(void* p)
{
    LIMITED_METHOD_CONTRACT;

    PTEB Teb = NtCurrentTeb();
    uint8_t** pTls = (uint8_t**)Teb->ThreadLocalStoragePointer;
    uint8_t* pOurTls = pTls[_tls_index];
    return (uint32_t)((uint8_t*)p - pOurTls);
}
#elif defined(TARGET_OSX)
extern "C" void* GetThreadVarsAddress();

static void* GetThreadVarsSectionAddressFromDesc(uint8_t* p)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERT(p[0] == 0x48 && p[1] == 0x8d && p[2] == 0x3d);

    // At this point, `p` contains the instruction pointer and is pointing to the above opcodes.
    // These opcodes are patched by the dynamic linker.
    // Move beyond the opcodes that we have already checked above.
    p += 3;

    // The descriptor address is located at *p at this point.
    // (p + 4) below skips the descriptor address bytes embedded in the instruction and
    // add it to the `instruction pointer` to find out the address.
    return *(uint32_t*)p + (p + 4);
}

static void* GetThreadVarsSectionAddress()
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_AMD64
    // On x64, the address is related to rip, so, disassemble the function,
    // read the offset, and then relative to the IP, find the final address of
    // __thread_vars section.
    uint8_t* p = reinterpret_cast<uint8_t*>(&GetThreadVarsAddress);
    return GetThreadVarsSectionAddressFromDesc(p);
#else
    return GetThreadVarsAddress();
#endif // TARGET_AMD64
}

#else

// Linux

#ifdef TARGET_AMD64

extern "C" void* GetTlsIndexObjectDescOffset();

static void* GetThreadStaticDescriptor(uint8_t* p)
{
    LIMITED_METHOD_CONTRACT;

    if (!(p[0] == 0x66 && p[1] == 0x48 && p[2] == 0x8d && p[3] == 0x3d))
    {
        // The optimization is disabled if coreclr is not compiled in .so format.
        _ASSERTE(false && "Unexpected code sequence");
        return nullptr;
    }

    // At this point, `p` contains the instruction pointer and is pointing to the above opcodes.
    // These opcodes are patched by the dynamic linker.
    // Move beyond the opcodes that we have already checked above.
    p += 4;

    // The descriptor address is located at *p at this point. Read that and add
    // it to the instruction pointer to locate the address of `ti` that will be used
    // to pass to __tls_get_addr during execution.
    // (p + 4) below skips the descriptor address bytes embedded in the instruction and
    // add it to the `instruction pointer` to find out the address.
    return *(uint32_t*)p + (p + 4);
}

static void* GetTlsIndexObjectAddress()
{
    LIMITED_METHOD_CONTRACT;

    uint8_t* p = reinterpret_cast<uint8_t*>(&GetTlsIndexObjectDescOffset);
    return GetThreadStaticDescriptor(p);
}

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

extern "C" size_t GetThreadStaticsVariableOffset();

#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64
#endif // TARGET_WINDOWS

void GetThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
{
    STANDARD_VM_CONTRACT;
    size_t threadStaticBaseOffset = 0;

#if defined(TARGET_WINDOWS)
    pInfo->tlsIndex.addr = (void*)static_cast<uintptr_t>(_tls_index);
    pInfo->tlsIndex.accessType = IAT_VALUE;

    pInfo->offsetOfThreadLocalStoragePointer = offsetof(_TEB, ThreadLocalStoragePointer);
    threadStaticBaseOffset = ThreadLocalOffset(&t_ThreadStatics);

#elif defined(TARGET_OSX)

    pInfo->threadVarsSection = GetThreadVarsSectionAddress();

#elif defined(TARGET_AMD64)

    // For Linux/x64, get the address of tls_get_addr system method and the base address
    // of struct that we will pass to it.
    pInfo->tlsGetAddrFtnPtr = reinterpret_cast<void*>(&__tls_get_addr);
    pInfo->tlsIndexObject = GetTlsIndexObjectAddress();

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

    // For Linux arm64/loongarch64/riscv64, just get the offset of thread static variable, and during execution,
    // this offset, arm64 taken from trpid_elp0 system register gives back the thread variable address.
    // this offset, loongarch64 taken from $tp register gives back the thread variable address.
    threadStaticBaseOffset = GetThreadStaticsVariableOffset();

#else
    _ASSERTE_MSG(false, "Unsupported scenario of optimizing TLS access on Linux Arm32/x86");
#endif // TARGET_WINDOWS

    pInfo->offsetOfMaxThreadStaticBlocks = (uint32_t)(threadStaticBaseOffset + offsetof(ThreadLocalData, cNonCollectibleTlsData));
    pInfo->offsetOfThreadStaticBlocks = (uint32_t)(threadStaticBaseOffset + offsetof(ThreadLocalData, pNonCollectibleTlsArrayData));
    pInfo->offsetOfBaseOfThreadLocalData = (uint32_t)threadStaticBaseOffset;
}
#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
void EnumThreadMemoryRegions(ThreadLocalData *pThreadLocalData, CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DacEnumMemoryRegion(dac_cast<TADDR>(pThreadLocalData->pCollectibleTlsArrayData), pThreadLocalData->cCollectibleTlsData, flags);
    PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
    while (pInFlightData != NULL)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(pInFlightData), sizeof(InFlightTLSData), flags);
        pInFlightData = pInFlightData->pNext;
    }
}
#endif // DACCESS_COMPILE
