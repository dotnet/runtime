// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "threadstatics.h"

struct InFlightTLSData
{
#ifndef DACCESS_COMPILE
    InFlightTLSData(TLSIndex index, TADDR pTLSData) : pNext(NULL), tlsIndex(index), pTLSData(pTLSData) { }
#endif // !DACCESS_COMPILE
    PTR_InFlightTLSData pNext; // Points at the next in-flight TLS data
    TLSIndex tlsIndex; // The TLS index for the static
    TADDR pTLSData; // The TLS data for the static
};


struct ThreadLocalLoaderAllocator
{
    ThreadLocalLoaderAllocator* pNext; // Points at the next thread local loader allocator
    LoaderAllocator* pLoaderAllocator; // The loader allocator that has a TLS used in this thread
};
typedef DPTR(ThreadLocalLoaderAllocator) PTR_ThreadLocalLoaderAllocator;

// This can be used for out of thread access to TLS data.
PTR_VOID GetThreadLocalStaticBaseNoCreate(ThreadLocalData* pThreadLocalData, TLSIndex index)
{
    LIMITED_METHOD_CONTRACT;
#ifndef DACCESS_COMPILE
    // Since this api can be used from a different thread, we need a lock to keep it all safe
    SpinLockHolder spinLock(&pThreadLocalData->pThread->m_TlsSpinLock);
#endif
    TADDR pTLSBaseAddress = NULL;
    int32_t cTLSData = pThreadLocalData->cTLSData;
    if (cTLSData <= index.GetByteIndex())
    {
        return NULL;
    }

    TADDR pTLSArrayData = pThreadLocalData->pTLSArrayData;
    pTLSBaseAddress = *dac_cast<PTR_TADDR>(dac_cast<PTR_BYTE>(pTLSArrayData) + index.GetByteIndex());
    if (pTLSBaseAddress == NULL)
    {
        // Maybe it is in the InFlightData
        PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
        while (pInFlightData != NULL)
        {
            if (pInFlightData->tlsIndex == index)
            {
                pTLSBaseAddress = pInFlightData->pTLSData;
                break;
            }
            pInFlightData = pInFlightData->pNext;
        }
    }
    return dac_cast<PTR_VOID>(pTLSBaseAddress);
}

GPTR_IMPL(TLSIndexToMethodTableMap, g_pThreadStaticTypeIndices);

PTR_MethodTable LookupMethodTableForThreadStaticKnownToBeAllocated(TLSIndex index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // TODO, if and when we get array indices, we should be pickier.
    return g_pThreadStaticTypeIndices->LookupTlsIndexKnownToBeAllocated(index);
}

TADDR isGCFlag = 0x1;

PTR_MethodTable LookupMethodTableAndFlagForThreadStatic(TLSIndex index, bool *pIsGCStatic, bool *pIsCollectible)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    // TODO, if and when we get array indices, we should be pickier.
    PTR_MethodTable retVal = g_pThreadStaticTypeIndices->Lookup(index, pIsGCStatic, pIsCollectible);
    return retVal;
}


// Report a TLS index to the GC, but if it is no longer in use and should be cleared out, return false
bool ReportTLSIndexCarefully(TLSIndex index, int32_t cLoaderHandles, PTR_LOADERHANDLE pLoaderHandles, PTR_PTR_Object ppTLSBaseAddress, promote_func* fn, ScanContext* sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;
    bool isGCStatic;
    bool isCollectible;
    PTR_MethodTable pMT = LookupMethodTableAndFlagForThreadStatic(index, &isGCStatic, &isCollectible);
    if (pMT == NULL)
    {
        // The TLS index is not in use. This either means that the TLS index was never used, or that it was
        // used for a collectible assembly, and that assembly has been freed. In the latter case, we may need to
        // clean this entry up
        if (cLoaderHandles > index.GetByteIndex())
        {
            pLoaderHandles[index.GetByteIndex()] = NULL;
            *ppTLSBaseAddress = NULL;
        }
        return false;
    }

    if (isCollectible)
    {
        // Check to see if the associated loaderallocator is still live
        if (!pMT->GetLoaderAllocator()->IsExposedObjectLive())
        {
            if (cLoaderHandles > index.GetByteIndex())
            {
                uintptr_t indexIntoLoaderHandleTable = index.GetByteIndex();
                pLoaderHandles[indexIntoLoaderHandleTable] = NULL;
                *ppTLSBaseAddress = NULL;
            }
            return false;
        }
    }
    fn(ppTLSBaseAddress, sc, 0 /* could be GC_CALL_INTERIOR or GC_CALL_PINNED */);
    return true;
}

// We use a scheme where the TLS data on each thread will be cleaned up within a GC promotion round or two.
#ifndef DACCESS_COMPILE
static Volatile<uint16_t> s_GCsWhichDoRelocateAndCanEmptyOutTheTLSIndices = 0;

void NotifyThreadStaticGCHappened()
{
    LIMITED_METHOD_CONTRACT;
    s_GCsWhichDoRelocateAndCanEmptyOutTheTLSIndices += 1;
}
#endif

void ScanThreadStaticRoots(ThreadLocalData *pThreadLocalData, bool forGC, promote_func* fn, ScanContext* sc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (pThreadLocalData == NULL)
        return;

    int32_t cLoaderHandles = forGC ? pThreadLocalData->cLoaderHandles : 0; // We can only need to pass this to ReportTLSIndexCarefully if we are permitted to to clean out this array
    PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
    while (pInFlightData != NULL)
    {
        if (!ReportTLSIndexCarefully(pInFlightData->tlsIndex, pThreadLocalData->cLoaderHandles, pThreadLocalData->pLoaderHandles, dac_cast<PTR_PTR_Object>(&pInFlightData->pTLSData), fn, sc))
        {
            // TLS index is now dead. We should delete it, as the ReportTLSIndexCarefully function will have already deleted any assocated LOADERHANDLE
#ifndef DACCESS_COMPILE
            if (forGC)
            {
                PTR_InFlightTLSData pNext = pInFlightData->pNext;
                delete pInFlightData;
                pInFlightData = pNext;
                continue;
            }
#endif
        }
        pInFlightData = pInFlightData->pNext;
    }
    PTR_BYTE pTLSArrayData = dac_cast<PTR_BYTE>(pThreadLocalData->pTLSArrayData);
    int32_t cTLSData = pThreadLocalData->cTLSData;
    for (int32_t i = 0; i < cTLSData; i += sizeof(TADDR))
    {
        TLSIndex index(i);
        TADDR *pTLSBaseAddress = dac_cast<PTR_TADDR>(pTLSArrayData + i);
        ReportTLSIndexCarefully(index, pThreadLocalData->cLoaderHandles, pThreadLocalData->pLoaderHandles, dac_cast<PTR_PTR_Object>(pTLSBaseAddress), fn, sc);
    }
}

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

    if (index.TLSIndexRawIndex >= m_maxIndex)
    {
        uint32_t newSize = max(m_maxIndex, 16);
        while (index.TLSIndexRawIndex >= newSize)
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
    _ASSERTE(pMap[index.TLSIndexRawIndex] == 0 || IsClearedValue(pMap[index.TLSIndexRawIndex]));
    pMap[index.TLSIndexRawIndex] = rawValue;
}

void TLSIndexToMethodTableMap::Clear(TLSIndex index, uint16_t whenCleared)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    _ASSERTE(index.TLSIndexRawIndex < m_maxIndex);
    TADDR rawValue = pMap[index.TLSIndexRawIndex];
    _ASSERTE(rawValue & IsCollectibleFlag());
    if (rawValue & IsCollectibleFlag())
    {
        m_collectibleEntries--;
    }
    pMap[index.TLSIndexRawIndex] = (whenCleared << 2) | 0x3;
    _ASSERTE(GetClearedMarker(pMap[index.TLSIndexRawIndex]) == whenCleared);
}

bool TLSIndexToMethodTableMap::FindClearedIndex(uint16_t whenClearedMarkerToAvoid, TLSIndex* pIndex)
{
    for (const auto& entry : *this)
    {
        if (entry.IsClearedValue)
        {
            uint16_t whenClearedMarker = entry.ClearedMarker;
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

uint32_t g_NextTLSSlot = (uint32_t)sizeof(TADDR);
CrstStatic g_TLSCrst;

void InitializeThreadStaticData()
{
    g_pThreadStaticTypeIndices = new TLSIndexToMethodTableMap();
    g_TLSCrst.Init(CrstThreadLocalStorageLock, CRST_UNSAFE_ANYMODE);
}

void InitializeCurrentThreadsStaticData(Thread* pThread)
{
    t_ThreadStatics.pThread = pThread;
    t_ThreadStatics.pThread->m_ThreadLocalDataThreadObjectCopy = t_ThreadStatics;
    t_ThreadStatics.pThread->m_TlsSpinLock.Init(LOCK_TLSDATA, TRUE);
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

void FreeThreadStaticData(ThreadLocalData *pThreadLocalData)
{
    if (pThreadLocalData->cLoaderHandles > 0)
    {
        CrstHolder ch(&g_TLSCrst);
        for (const auto& entry : g_pThreadStaticTypeIndices->CollectibleEntries())
        {
            pThreadLocalData->pLoaderHandles[entry.TlsIndex.GetByteIndex()] = NULL;
        }
    }

    delete[] (uint8_t*)pThreadLocalData->pTLSArrayData;

    pThreadLocalData->pTLSArrayData = 0;

    while (pThreadLocalData->pInFlightData != NULL)
    {
        InFlightTLSData* pInFlightData = pThreadLocalData->pInFlightData;
        pThreadLocalData->pInFlightData = pInFlightData->pNext;
        delete pInFlightData;
    }

    pThreadLocalData->pThread = NULL;
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
    MethodTable *pMT = LookupMethodTableAndFlagForThreadStatic(index, &isGCStatic, &isCollectible);

    int32_t cTLSData = t_ThreadStatics.cTLSData;
    if (cTLSData <= index.GetByteIndex())
    {
        // Grow the underlying TLS array
        SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
        int32_t newcTLSData = index.GetByteIndex() + sizeof(TADDR) * 8; // Leave a bit of margin
        uint8_t* pNewTLSArrayData = new uint8_t[newcTLSData];
        memset(pNewTLSArrayData, 0, newcTLSData);
        if (cTLSData > 0)
            memcpy(pNewTLSArrayData, (void*)t_ThreadStatics.pTLSArrayData, cTLSData + 1);
        uint8_t* pOldArray = (uint8_t*)t_ThreadStatics.pTLSArrayData;
        t_ThreadStatics.pTLSArrayData = (TADDR)pNewTLSArrayData;
        cTLSData = newcTLSData - 1;
        t_ThreadStatics.cTLSData = cTLSData;
        delete[] pOldArray;
        t_ThreadStatics.pThread->m_ThreadLocalDataThreadObjectCopy = t_ThreadStatics;
    }

    if (isCollectible && t_ThreadStatics.cLoaderHandles <= index.GetByteIndex())
    {
        // Grow the underlying TLS array
        SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
        int32_t cNewTLSLoaderHandles = index.GetByteIndex() + sizeof(TADDR) * 8; // Leave a bit of margin
        size_t cbNewTLSLoaderHandles = sizeof(LOADERHANDLE) * cNewTLSLoaderHandles;
        LOADERHANDLE* pNewTLSLoaderHandles = new LOADERHANDLE[cNewTLSLoaderHandles];
        memset(pNewTLSLoaderHandles, 0, cbNewTLSLoaderHandles);
        if (cTLSData > 0)
            memcpy(pNewTLSLoaderHandles, (void*)t_ThreadStatics.pLoaderHandles, t_ThreadStatics.cLoaderHandles * sizeof(LOADERHANDLE));

        LOADERHANDLE* pOldArray = t_ThreadStatics.pLoaderHandles;
        t_ThreadStatics.pLoaderHandles = pNewTLSLoaderHandles;
        t_ThreadStatics.cLoaderHandles = cNewTLSLoaderHandles;
        delete[] pOldArray;
        t_ThreadStatics.pThread->m_ThreadLocalDataThreadObjectCopy = t_ThreadStatics;
    }

    TADDR pTLSArrayData = t_ThreadStatics.pTLSArrayData;
    TADDR *ppTLSBaseAddress = reinterpret_cast<TADDR*>(reinterpret_cast<uint8_t*>(pTLSArrayData) + index.GetByteIndex());
    TADDR pTLSBaseAddress = *ppTLSBaseAddress;

    if (pTLSBaseAddress == NULL)
    {
        // Maybe it is in the InFlightData
        InFlightTLSData* pInFlightData = t_ThreadStatics.pInFlightData;
        InFlightTLSData** ppOldNextPtr = &t_ThreadStatics.pInFlightData;
        while (pInFlightData != NULL)
        {
            if (pInFlightData->tlsIndex == index)
            {
                pTLSBaseAddress = pInFlightData->pTLSData;
                
                if (pMT->IsClassInited())
                {
                    SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
                    *ppTLSBaseAddress = pTLSBaseAddress;
                    *ppOldNextPtr = pInFlightData->pNext;
                    delete pInFlightData;
                    t_ThreadStatics.pThread->m_ThreadLocalDataThreadObjectCopy = t_ThreadStatics;
                }
                break;
            }
            ppOldNextPtr = &pInFlightData->pNext;
            pInFlightData = pInFlightData->pNext;
        }
        if (pTLSBaseAddress == NULL)
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
                gc.tlsEntry = AllocatePrimiteveArray(ELEMENT_TYPE_R8, static_cast<DWORD>(AlignUp(pMT->GetClass()->GetNonGCThreadStaticFieldBytes(), 8)/8));
#else
                gc.tlsEntry = AllocatePrimitiveArray(ELEMENT_TYPE_I1, static_cast<DWORD>(pMT->GetClass()->GetNonGCThreadStaticFieldBytes()));
#endif
            }

            NewHolder<InFlightTLSData> pInFlightData = NULL;
            if (!pMT->IsClassInited())
            {
                pInFlightData = new InFlightTLSData(index, pTLSBaseAddress);
            }

            if (isCollectible)
            {
                LOADERHANDLE *pLoaderHandle = reinterpret_cast<LOADERHANDLE*>(reinterpret_cast<uint8_t*>(pTLSArrayData) + index.GetByteIndex());
                // Note, that this can fail, but if it succeeds we don't have a holder in place to clean it up if future operations fail
                // Add such a holder if we ever add a possibly failing operation after this
                *pLoaderHandle = pMT->GetLoaderAllocator()->AllocateHandle(gc.tlsEntry);
            }

            // After this, we cannot fail
            pInFlightData.SuppressRelease();

            {
                GCX_FORBID();
                pTLSBaseAddress = (TADDR)OBJECTREFToObject(gc.tlsEntry);
                if (pInFlightData == NULL)
                {
                    *ppTLSBaseAddress = pTLSBaseAddress;
                }
                else
                {
                    SpinLockHolder spinLock(&t_ThreadStatics.pThread->m_TlsSpinLock);
                    pInFlightData->pNext = t_ThreadStatics.pInFlightData;
                    t_ThreadStatics.pInFlightData = pInFlightData;
                    t_ThreadStatics.pThread->m_ThreadLocalDataThreadObjectCopy = t_ThreadStatics;
                }
            }
            GCPROTECT_END();
        }
    }
    _ASSERTE(pTLSBaseAddress != NULL);
    return reinterpret_cast<void*>(pTLSBaseAddress);
}

void GetTLSIndexForThreadStatic(MethodTable* pMT, bool gcStatic, TLSIndex* pIndex)
{
    WRAPPER_NO_CONTRACT;

    GCX_COOP();
    CrstHolder ch(&g_TLSCrst);
    if (pIndex->IsAllocated())
    {
        return;
    }

    TLSIndex newTLSIndex = TLSIndex::Unallocated();
    if (!g_pThreadStaticTypeIndices->FindClearedIndex(s_GCsWhichDoRelocateAndCanEmptyOutTheTLSIndices, &newTLSIndex))
    {
        uint32_t tlsRawIndex = g_NextTLSSlot;
        newTLSIndex = TLSIndex(tlsRawIndex);
        g_NextTLSSlot += (uint32_t)sizeof(TADDR);
    }

    if (pMT->Collectible())
    {
        pMT->GetLoaderAllocator()->GetTLSIndexList().Append(newTLSIndex);
    }

    g_pThreadStaticTypeIndices->Set(newTLSIndex, pMT, (gcStatic ? isGCFlag : 0));

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
        g_pThreadStaticTypeIndices->Clear(tlsIndicesToCleanup[current], s_GCsWhichDoRelocateAndCanEmptyOutTheTLSIndices);
        ++current;
    }
}

static void* GetTlsIndexObjectAddress();

bool CanJITOptimizeTLSAccess()
{
    bool optimizeThreadStaticAccess = false;
#if defined(TARGET_ARM)
    // Optimization is disabled for linux/windows arm
#elif !defined(TARGET_WINDOWS) && defined(TARGET_X86)
    // Optimization is disabled for linux/x86
#elif defined(TARGET_LINUX_MUSL) && defined(TARGET_ARM64)
    // Optimization is disabled for linux musl arm64
#elif defined(TARGET_FREEBSD) && defined(TARGET_ARM64)
    // Optimization is disabled for FreeBSD/arm64
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
    PTEB Teb = NtCurrentTeb();
    uint8_t** pTls = (uint8_t**)Teb->ThreadLocalStoragePointer;
    uint8_t* pOurTls = pTls[_tls_index];
    return (uint32_t)((uint8_t*)p - pOurTls);
}
#elif defined(TARGET_OSX)
extern "C" void* GetThreadVarsAddress();

static void* GetThreadVarsSectionAddressFromDesc(uint8_t* p)
{
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

    pInfo->offsetOfThreadStaticBlocks = (uint32_t)threadStaticBaseOffset;
}
#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
void EnumThreadMemoryRegions(ThreadLocalData *pThreadLocalData, CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DacEnumMemoryRegion(dac_cast<TADDR>(pThreadLocalData->pTLSArrayData), pThreadLocalData->cTLSData, flags);
    PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
    while (pInFlightData != NULL)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(pInFlightData), sizeof(InFlightTLSData), flags);
        pInFlightData = pInFlightData->pNext;
    }
}
#endif // DACCESS_COMPILE
