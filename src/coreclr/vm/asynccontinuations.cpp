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

        func((start - objRefs) * TARGET_POINTER_SIZE, (end - start) * TARGET_POINTER_SIZE);
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
    pMT->SetIsContinuation(allocatedDataOffsets);
    pMT->AllocateAuxiliaryData(m_allocator, asyncMethod->GetLoaderModule(), pamTracker, MethodTableStaticsFlags::None);
    pMT->SetLoaderAllocator(m_allocator);
    pMT->SetModule(asyncMethod->GetModule());
    pMT->SetNumVirtuals(static_cast<WORD>(numVirtuals));
    pMT->SetParentMethodTable(pParentClass);
    pMT->SetClass(pParentClass->GetClass()); // TODO: needs its own?
    pMT->SetBaseSize(OBJECT_BASESIZE + startOfDataInInstance + dataSize);

    if (numPointerSeries > 0)
    {
        pMT->SetContainsGCPointers();
        CGCDesc::Init(pMT, numPointerSeries);
        CGCDescSeries* pSeries = CGCDesc::GetCGCDescFromMT(pMT)->GetLowestSeries();

        CGCDescSeries* pParentSeries = CGCDesc::GetCGCDescFromMT(pParentClass)->GetLowestSeries();
        for (unsigned i = 0; i < numParentPointerSeries; i++)
        {
            pSeries->SetSeriesSize((pParentSeries->GetSeriesSize() + pParentClass->GetBaseSize()) - pMT->GetBaseSize());
            pSeries->SetSeriesOffset(pParentSeries->GetSeriesOffset());
            pSeries++;
            pParentSeries++;
        }

        auto writeSeries = [&](size_t start, size_t length) {
            pSeries->SetSeriesSize(length - pMT->GetBaseSize());
            pSeries->SetSeriesOffset(startOfDataInObject + start);
            pSeries++;
            };
        EnumerateRunsOfObjRefs(dataSize, objRefs, writeSeries);
        _ASSERTE(pSeries == CGCDesc::GetCGCDescFromMT(pMT)->GetLowestSeries() + numPointerSeries);
    }

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
