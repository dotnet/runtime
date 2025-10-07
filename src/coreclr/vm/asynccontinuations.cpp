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
}

template<typename TFunc>
static void EnumerateRunsOfObjRefs(unsigned size, const bool* objRefs, TFunc func)
{
    const bool* objRefsEnd = objRefs + (size + (TARGET_POINTER_SIZE - 1)) / TARGET_POINTER_SIZE;
    const bool* start = objRefs;
    while (start < objRefsEnd)
    {
        while (start < objRefsEnd && !*start)
            start++;

        if (start >= objRefsEnd)
            return;

        const bool* end = start;
        while (end < objRefsEnd && *end)
            end++;

        func((start - objRefs) * TARGET_POINTER_SIZE, end - start);
        start = end;
    }
}

MethodTable* AsyncContinuationsManager::CreateNewContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod, AllocMemTracker* pamTracker)
{
    STANDARD_VM_CONTRACT;

    MethodTable* pParentClass = CoreLibBinder::GetClass(CLASS__CONTINUATION);

    DWORD numVirtuals = pParentClass->GetNumVirtuals();

    size_t cbMT = sizeof(MethodTable);
    cbMT += MethodTable::GetNumVtableIndirections(numVirtuals) * sizeof(MethodTable::VTableIndir_t);

    unsigned numObjRefRuns = 0;
    EnumerateRunsOfObjRefs(dataSize, objRefs, [&](size_t start, size_t count)
    {
        numObjRefRuns++;
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

    // Offsets passed in are relative to the data chunk, fix that up now to be
    // relative to the start of the instance data.
    if (allocatedDataOffsets->Result != UINT_MAX)
        allocatedDataOffsets->Result += startOfDataInInstance;
    if (allocatedDataOffsets->Exception != UINT_MAX)
        allocatedDataOffsets->Exception += startOfDataInInstance;
    if (allocatedDataOffsets->ContinuationContext != UINT_MAX)
        allocatedDataOffsets->ContinuationContext += startOfDataInInstance;
    if (allocatedDataOffsets->KeepAlive != UINT_MAX)
        allocatedDataOffsets->KeepAlive += startOfDataInInstance;

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
            };
        EnumerateRunsOfObjRefs(dataSize, objRefs, writeSeries);

        _ASSERTE(curIndex == 0);
    }

#ifdef DEBUG
    size_t numObjRefs = 0;
    EnumerateRunsOfObjRefs(dataSize, objRefs, [&](size_t start, size_t count) {
        numObjRefs += count;
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

MethodTable* AsyncContinuationsManager::LookupOrCreateContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod, AllocMemTracker* pamTracker)
{
    STANDARD_VM_CONTRACT;

    // TODO: table to share/deduplicate these.
    return CreateNewContinuationMethodTable(dataSize, objRefs, dataOffsets, asyncMethod, pamTracker);
}

#endif
