// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: asynccontinuations.cpp
//

// ===========================================================================
// This file contains the manager for creating new dynamic async continuation types.
// ===========================================================================
//

#include "common.h"
#include "asynccontinuations.h"

#ifndef DACCESS_COMPILE

AsyncContinuationsManager::AsyncContinuationsManager(LoaderAllocator* allocator)
    : m_allocator(allocator)
{
    LIMITED_METHOD_CONTRACT;

    m_layoutsLock.Init(CrstLeafLock);
    LockOwner lock = {&m_layoutsLock, IsOwnerOfCrst};
    m_layouts.Init(16, &lock, m_allocator->GetLowFrequencyHeap());
}

void AsyncContinuationsManager::NotifyUnloadingClasses()
{
    if (!CORProfilerTrackClasses())
    {
        return;
    }

    EEHashTableIteration iter;
    m_layouts.IterateStart(&iter);
    while (m_layouts.IterateNext(&iter))
    {
        MethodTable* pMT = (MethodTable*)m_layouts.IterateGetValue(&iter);
        ClassLoader::NotifyUnload(pMT, true);
        ClassLoader::NotifyUnload(pMT, false);
    }
}

static EEClass* volatile g_singletonContinuationEEClass;

EEClass* AsyncContinuationsManager::GetOrCreateSingletonSubContinuationEEClass()
{
    if (g_singletonContinuationEEClass != NULL)
        return g_singletonContinuationEEClass;

    return CreateSingletonSubContinuationEEClass();
}

EEClass* AsyncContinuationsManager::CreateSingletonSubContinuationEEClass()
{
    AllocMemTracker amTracker;

    Module* spc = SystemDomain::SystemModule();
    LoaderAllocator* allocator = SystemDomain::GetGlobalLoaderAllocator();

    EEClass* pClass = EEClass::CreateMinimalClass(allocator->GetHighFrequencyHeap(), &amTracker);

    MethodTable* pMT = CreateNewContinuationMethodTable(0, NULL, pClass, allocator, spc, &amTracker);

    pClass->SetMethodTable(pMT);

#ifdef _DEBUG
    pClass->SetDebugClassName("Continuation");
    pMT->SetDebugClassName("Continuation");
#endif

    if (InterlockedCompareExchangeT(&g_singletonContinuationEEClass, pClass, NULL) == NULL)
    {
        amTracker.SuppressRelease();
    }

    return g_singletonContinuationEEClass;
}

template<typename TFunc>
static bool EnumerateRunsOfObjRefs(unsigned size, const bool* objRefs, TFunc func)
{
    const bool* objRefsEnd = objRefs + (size + (TARGET_POINTER_SIZE - 1)) / TARGET_POINTER_SIZE;
    const bool* start = objRefs;
    while (start < objRefsEnd)
    {
        while (start < objRefsEnd && !*start)
            start++;

        if (start >= objRefsEnd)
            return true;

        const bool* end = start;
        while (end < objRefsEnd && *end)
            end++;

        if (!func((start - objRefs) * TARGET_POINTER_SIZE, end - start))
            return false;

        start = end;
    }

    return true;
}

MethodTable* AsyncContinuationsManager::CreateNewContinuationMethodTable(
    unsigned dataSize,
    const bool* objRefs,
    EEClass* pClass,
    LoaderAllocator* allocator,
    Module* loaderModule,
    AllocMemTracker* pamTracker)
{
    STANDARD_VM_CONTRACT;

    MethodTable* pParentClass = CoreLibBinder::GetClass(CLASS__CONTINUATION);

    if (g_pContinuationClassIfSubTypeCreated.Load() == NULL)
    {
        g_pContinuationClassIfSubTypeCreated.Store(pParentClass);
    }

    DWORD numVirtuals = pParentClass->GetNumVirtuals();

    size_t cbMT = sizeof(MethodTable);
    cbMT += MethodTable::GetNumVtableIndirections(numVirtuals) * sizeof(MethodTable::VTableIndir_t);

    unsigned numObjRefRuns = 0;
    EnumerateRunsOfObjRefs(dataSize, objRefs, [&](size_t start, size_t count)
    {
        numObjRefRuns++;
        return true;
    });

    unsigned numParentPointerSeries = 0;
    if (pParentClass->ContainsGCPointers())
        numParentPointerSeries = static_cast<unsigned>(CGCDesc::GetCGCDescFromMT(pParentClass)->GetNumSeries());

    unsigned numPointerSeries = numParentPointerSeries + numObjRefRuns;

    size_t cbGC = numPointerSeries == 0 ? 0 : CGCDesc::ComputeSize(numPointerSeries);

    BYTE* pMemory = (BYTE*)pamTracker->Track(allocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(cbGC) + S_SIZE_T(cbMT)));

    unsigned startOfDataInInstance = AlignUp(pParentClass->GetNumInstanceFieldBytes(), TARGET_POINTER_SIZE);
    unsigned startOfDataInObject = OBJECT_SIZE + startOfDataInInstance;

    MethodTable* pMT = (MethodTable*)(pMemory + cbGC);
    pMT->AllocateAuxiliaryData(allocator, loaderModule, pamTracker, MethodTableStaticsFlags::None);
    pMT->SetParentMethodTable(pParentClass);
    pMT->SetLoaderAllocator(allocator);
    pMT->SetModule(pParentClass->GetModule());
    pMT->SetNumVirtuals(static_cast<WORD>(numVirtuals));
    pMT->SetClass(pClass);
    pMT->SetBaseSize(OBJECT_BASESIZE + startOfDataInInstance + dataSize);

    if (numPointerSeries > 0)
    {
        pMT->SetContainsGCPointers();
        CGCDesc::Init(pMT, numPointerSeries);
        CGCDescSeries* pSeries = CGCDesc::GetCGCDescFromMT(pMT)->GetLowestSeries();

        // Write GC runs. In memory they must be descending by offset, so we
        // enumerate forwards but write them from the end.
        unsigned curIndex = numPointerSeries;
        CGCDescSeries* pParentSeries = CGCDesc::GetCGCDescFromMT(pParentClass)->GetLowestSeries();
        for (unsigned i = numParentPointerSeries; i--;)
        {
            curIndex--;
            pSeries[curIndex].SetSeriesSize((pParentSeries[i].GetSeriesSize() + pParentClass->GetBaseSize()) - pMT->GetBaseSize());
            pSeries[curIndex].SetSeriesOffset(pParentSeries[i].GetSeriesOffset());
        }

        auto writeSeries = [&](size_t start, size_t count) {
            curIndex--;
            pSeries[curIndex].SetSeriesSize((count * TARGET_POINTER_SIZE) - pMT->GetBaseSize());
            pSeries[curIndex].SetSeriesOffset(startOfDataInObject + start);
            return true;
            };
        EnumerateRunsOfObjRefs(dataSize, objRefs, writeSeries);

        _ASSERTE(curIndex == 0);
    }

    pMT->SetClassInited();
    INDEBUG(pMT->GetAuxiliaryDataForWrite()->SetIsPublished());

    return pMT;
}

MethodTable* AsyncContinuationsManager::LookupOrCreateContinuationMethodTable(unsigned dataSize, const bool* objRefs, Module* loaderModule)
{
    STANDARD_VM_CONTRACT;

    ContinuationLayoutKeyData keyData(dataSize, objRefs);
    {
        CrstHolder lock(&m_layoutsLock);
        MethodTable* lookupResult;
        if (m_layouts.GetValue(ContinuationLayoutKey(&keyData), (HashDatum*)&lookupResult))
        {
            return lookupResult;
        }
    }

#ifdef FEATURE_EVENT_TRACE
    UINT32 typeLoad = ETW::TypeSystemLog::TypeLoadBegin();
#endif

    AllocMemTracker amTracker;
    MethodTable* pNewMT = CreateNewContinuationMethodTable(
        dataSize,
        objRefs,
        GetOrCreateSingletonSubContinuationEEClass(),
        m_allocator,
        loaderModule,
        &amTracker);

#ifdef DEBUG
    StackSString debugName;
    PrintContinuationName(
        pNewMT,
        [&](LPCSTR str, LPCWSTR wstr) { debugName.AppendUTF8(str); },
        [&](unsigned num) { debugName.AppendPrintf("%u", num); });
    const char* debugNameUTF8 = debugName.GetUTF8();
    size_t len = strlen(debugNameUTF8) + 1;
    char* name = (char*)amTracker.Track(m_allocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(len)));
    strcpy_s(name, len, debugNameUTF8);
    pNewMT->SetDebugClassName(name);
#endif

    MethodTable* pReturnedMT = pNewMT;
    {
        CrstHolder lock(&m_layoutsLock);
        MethodTable* lookupResult;
        if (m_layouts.GetValue(ContinuationLayoutKey(&keyData), (HashDatum*)&lookupResult))
        {
            pReturnedMT = lookupResult;
        }
        else
        {
            m_layouts.InsertValue(ContinuationLayoutKey(pNewMT), pNewMT);
        }
    }

    if (pReturnedMT == pNewMT)
    {
        amTracker.SuppressRelease();

        ClassLoader::NotifyLoad(TypeHandle(pNewMT));
    }

#ifdef FEATURE_EVENT_TRACE
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TypeLoadStop))
    {
        ETW::TypeSystemLog::TypeLoadEnd(typeLoad, TypeHandle(pReturnedMT), CLASS_LOADED);
    }
#endif

    return pReturnedMT;
}

ContinuationLayoutKey::ContinuationLayoutKey(MethodTable* pMT)
    : Data(reinterpret_cast<uintptr_t>(pMT) | 1)
{
}

ContinuationLayoutKey::ContinuationLayoutKey(ContinuationLayoutKeyData* pData)
    : Data(reinterpret_cast<uintptr_t>(pData))
{
}

EEHashEntry_t* ContinuationLayoutKeyHashTableHelper::AllocateEntry(ContinuationLayoutKey key, BOOL bDeepCopy, AllocationHeap heap)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_NOTRIGGER);
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END

    EEHashEntry_t* pEntry = (EEHashEntry_t*)new (nothrow) BYTE[SIZEOF_EEHASH_ENTRY + sizeof(ContinuationLayoutKey)];
    if (pEntry == NULL)
        return NULL;
    memcpy(pEntry->Key, &key, sizeof(ContinuationLayoutKey));
    return pEntry;
}

void ContinuationLayoutKeyHashTableHelper::DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap heap)
{
    LIMITED_METHOD_CONTRACT;

    delete [] (BYTE*) pEntry;
}

static CGCDescSeries* NextSeriesInData(CGCDescSeries* curSeries, CGCDescSeries* lowestSeries)
{
    while (curSeries >= lowestSeries && curSeries->GetSeriesOffset() < OFFSETOF__CORINFO_Continuation__data)
    {
        curSeries--;
    }

    return curSeries;
}

BOOL ContinuationLayoutKeyHashTableHelper::CompareKeys(EEHashEntry_t *pEntry, ContinuationLayoutKey key)
{
    LIMITED_METHOD_CONTRACT;
    ContinuationLayoutKey storedKey;
    memcpy(&storedKey, pEntry->Key, sizeof(ContinuationLayoutKey));

    _ASSERTE((storedKey.Data & 1) != 0); // Always stores a MethodTable* as key
    _ASSERTE((key.Data & 1) == 0); // Always uses ContinuationLayoutKeyData* as lookup key

    MethodTable* left = reinterpret_cast<MethodTable*>(storedKey.Data ^ 1);
    ContinuationLayoutKeyData* right = reinterpret_cast<ContinuationLayoutKeyData*>(key.Data);
    if (left->GetBaseSize() != (OBJHEADER_SIZE + OFFSETOF__CORINFO_Continuation__data + right->DataSize))
        return FALSE;

    // Now compare GC pointer series.
    CGCDesc* gc = CGCDesc::GetCGCDescFromMT(left);
    CGCDescSeries* curSeries = gc->GetHighestSeries();
    CGCDescSeries* lowestSeries = gc->GetLowestSeries();

    // Skip ahead to first series inside the data region.
    curSeries = NextSeriesInData(curSeries, lowestSeries);

    // Now verify that the series in the data match the bitmap.
    auto compare = [=, &curSeries](size_t offset, size_t count)
        {
            if (curSeries < lowestSeries)
                return false;

            if (OFFSETOF__CORINFO_Continuation__data + offset != curSeries->GetSeriesOffset())
                return false;

            if (count * TARGET_POINTER_SIZE != curSeries->GetSeriesSize() + left->GetBaseSize())
                return false;

            curSeries--;
            return true;
        };

    if (!EnumerateRunsOfObjRefs(right->DataSize, right->ObjRefs, compare))
    {
        return false;
    }

    // Finally verify that we ran out of GC pointers simultaneously.
    return curSeries + 1 == lowestSeries;
}

DWORD ContinuationLayoutKeyHashTableHelper::Hash(ContinuationLayoutKey key)
{
    DWORD dwHash = 5381;

    if ((key.Data & 1) != 0)
    {
        MethodTable* pMT = reinterpret_cast<MethodTable*>(key.Data ^ 1);
        CGCDesc* gc = CGCDesc::GetCGCDescFromMT(pMT);
        CGCDescSeries* curSeries = gc->GetHighestSeries();
        CGCDescSeries* lowestSeries = gc->GetLowestSeries();

        // Skip ahead to first series inside the data region.
        curSeries = NextSeriesInData(curSeries, lowestSeries);

        dwHash = ((dwHash << 5) + dwHash) ^ (pMT->GetBaseSize() - (OBJHEADER_SIZE + OFFSETOF__CORINFO_Continuation__data));
        while (curSeries >= lowestSeries)
        {
            dwHash = ((dwHash << 5) + dwHash) ^ (unsigned)(curSeries->GetSeriesOffset() - OFFSETOF__CORINFO_Continuation__data);
            dwHash = ((dwHash << 5) + dwHash) ^ (unsigned)((curSeries->GetSeriesSize() + pMT->GetBaseSize()) / TARGET_POINTER_SIZE);
            curSeries--;
        }
    }
    else
    {
        ContinuationLayoutKeyData* keyData = reinterpret_cast<ContinuationLayoutKeyData*>(key.Data);

        dwHash = ((dwHash << 5) + dwHash) ^ keyData->DataSize;
        EnumerateRunsOfObjRefs(keyData->DataSize, keyData->ObjRefs, [&dwHash](size_t offset, size_t count){
            dwHash = ((dwHash << 5) + dwHash) ^ (unsigned)offset;
            dwHash = ((dwHash << 5) + dwHash) ^ (unsigned)count;
            return true;
            });
    }

    return dwHash;
}

void ContinuationLayoutKeyHashTableHelper::ReplaceKey(EEHashEntry_t *pEntry, ContinuationLayoutKey newKey)
{
    memcpy(pEntry->Key, &newKey, sizeof(ContinuationLayoutKey));
}

#endif
