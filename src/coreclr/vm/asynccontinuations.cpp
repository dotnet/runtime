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

MethodTable* AsyncContinuationsManager::CreateNewContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod, AllocMemTracker* pamTracker)
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

    BYTE* pMemory = (BYTE*)pamTracker->Track(m_allocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(CORINFO_CONTINUATION_DATA_OFFSETS)) + S_SIZE_T(cbGC) + S_SIZE_T(cbMT)));

    CORINFO_CONTINUATION_DATA_OFFSETS* allocatedDataOffsets = new (pMemory) CORINFO_CONTINUATION_DATA_OFFSETS(dataOffsets);

    unsigned startOfDataInInstance = AlignUp(pParentClass->GetNumInstanceFieldBytes(), TARGET_POINTER_SIZE);
    unsigned startOfDataInObject = OBJECT_SIZE + startOfDataInInstance;

    MethodTable* pMT = (MethodTable*)(pMemory + sizeof(CORINFO_CONTINUATION_DATA_OFFSETS) + cbGC);
    pMT->AllocateAuxiliaryData(m_allocator, asyncMethod->GetLoaderModule(), pamTracker, MethodTableStaticsFlags::None);
    pMT->SetParentMethodTable(pParentClass);
    pMT->SetContinuationDataOffsets(allocatedDataOffsets);
    pMT->SetLoaderAllocator(m_allocator);
    pMT->SetModule(pParentClass->GetModule());
    pMT->SetNumVirtuals(static_cast<WORD>(numVirtuals));
    pMT->SetClass(pParentClass->GetClass()); // EEClass of System.Runtime.CompilerServices.Continuation
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

#ifdef DEBUG
    size_t numObjRefs = 0;
    EnumerateRunsOfObjRefs(dataSize, objRefs, [&](size_t start, size_t count) {
        numObjRefs += count;
        return true;
        });
    StackSString debugName;
    debugName.Printf("Continuation_%s_%u_%zu", asyncMethod->m_pszDebugMethodName, dataSize, numObjRefs);
    const char* debugNameUTF8 = debugName.GetUTF8();
    size_t len = strlen(debugNameUTF8) + 1;
    char* name = (char*)pamTracker->Track(m_allocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(len)));
    strcpy_s(name, len, debugNameUTF8);
    pMT->SetDebugClassName(name);
#endif

    pMT->SetClassInited();

    return pMT;
}

MethodTable* AsyncContinuationsManager::LookupOrCreateContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod)
{
    STANDARD_VM_CONTRACT;

    // The API we expose has all offsets relative to the data, but we prefer to
    // have offsets relative to the start of instance data so that
    // RuntimeHelpers.GetRawData(obj) + offset returns the right offset. Adjust
    // it here.
    CORINFO_CONTINUATION_DATA_OFFSETS adjustedDataOffsets = dataOffsets;
    const uint32_t startOfDataInInstance = OFFSETOF__CORINFO_Continuation__data - SIZEOF__CORINFO_Object;
    if (dataOffsets.Result != UINT_MAX)
        adjustedDataOffsets.Result += startOfDataInInstance;
    if (dataOffsets.Exception != UINT_MAX)
        adjustedDataOffsets.Exception += startOfDataInInstance;
    if (dataOffsets.ContinuationContext != UINT_MAX)
        adjustedDataOffsets.ContinuationContext += startOfDataInInstance;
    if (dataOffsets.KeepAlive != UINT_MAX)
        adjustedDataOffsets.KeepAlive += startOfDataInInstance;

    ContinuationLayoutKeyData keyData(dataSize, objRefs, adjustedDataOffsets);
    {
        CrstHolder lock(&m_layoutsLock);
        ContinuationLayoutKey key(&keyData);
        MethodTable* lookupResult;
        if (m_layouts.GetValue(key, (HashDatum*)&lookupResult))
        {
            return lookupResult;
        }
    }

    AllocMemTracker amTracker;
    MethodTable* result = CreateNewContinuationMethodTable(dataSize, objRefs, adjustedDataOffsets, asyncMethod, &amTracker);
    {
        CrstHolder lock(&m_layoutsLock);
        ContinuationLayoutKey key(&keyData);
        MethodTable* lookupResult;
        if (m_layouts.GetValue(key, (HashDatum*)&lookupResult))
        {
            result = lookupResult;
        }
        else
        {
            m_layouts.InsertValue(ContinuationLayoutKey(result), result);
            amTracker.SuppressRelease();
        }
    }

    return result;
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
    if (!pEntry)
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

    const CORINFO_CONTINUATION_DATA_OFFSETS& leftOffsets = *left->GetContinuationOffsets();
    const CORINFO_CONTINUATION_DATA_OFFSETS& rightOffsets = right->DataOffsets;
    if (leftOffsets.Result != rightOffsets.Result ||
        leftOffsets.Exception != rightOffsets.Exception ||
        leftOffsets.ContinuationContext != rightOffsets.ContinuationContext ||
        leftOffsets.KeepAlive != rightOffsets.KeepAlive)
    {
        return FALSE;
    }

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

    const CORINFO_CONTINUATION_DATA_OFFSETS* dataOffsets;
    if ((key.Data & 1) != 0)
    {
        MethodTable* pMT = reinterpret_cast<MethodTable*>(key.Data ^ 1);
        dataOffsets = pMT->GetContinuationOffsets();

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
        dataOffsets = &keyData->DataOffsets;

        dwHash = ((dwHash << 5) + dwHash) ^ keyData->DataSize;
        EnumerateRunsOfObjRefs(keyData->DataSize, keyData->ObjRefs, [&dwHash](size_t offset, size_t count){
            dwHash = ((dwHash << 5) + dwHash) ^ (unsigned)offset;
            dwHash = ((dwHash << 5) + dwHash) ^ (unsigned)count;
            return true;
            });
    }

    dwHash = ((dwHash << 5) + dwHash) ^ dataOffsets->Result;
    dwHash = ((dwHash << 5) + dwHash) ^ dataOffsets->Exception;
    dwHash = ((dwHash << 5) + dwHash) ^ dataOffsets->ContinuationContext;
    dwHash = ((dwHash << 5) + dwHash) ^ dataOffsets->KeepAlive;

    return dwHash;
}

void ContinuationLayoutKeyHashTableHelper::ReplaceKey(EEHashEntry_t *pEntry, ContinuationLayoutKey newKey)
{
    memcpy(pEntry->Key, &newKey, sizeof(ContinuationLayoutKey));
}

#endif
