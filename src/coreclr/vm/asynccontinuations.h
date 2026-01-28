// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: asynccontinuations.h
//
// ===========================================================================

#ifndef ASYNCCONTINUATIONS_H
#define ASYNCCONTINUATIONS_H

struct ContinuationLayoutKeyData
{
    unsigned DataSize;
    const bool* ObjRefs;

    ContinuationLayoutKeyData(unsigned dataSize, const bool* objRefs)
        : DataSize(dataSize)
        , ObjRefs(objRefs)
    {
    }
};

struct ContinuationLayoutKey
{
    uintptr_t Data;
    ContinuationLayoutKey() : Data(0)
    {
    }
    ContinuationLayoutKey(MethodTable* pMT);
    ContinuationLayoutKey(ContinuationLayoutKeyData* pData);
};

struct ContinuationLayoutKeyHashTableHelper
{
    static EEHashEntry_t * AllocateEntry(ContinuationLayoutKey key, BOOL bDeepCopy, AllocationHeap heap);
    static void            DeleteEntry(EEHashEntry_t *pEntry, AllocationHeap heap);
    static BOOL            CompareKeys(EEHashEntry_t *pEntry, ContinuationLayoutKey key);
    static DWORD           Hash(ContinuationLayoutKey key);
    static void            ReplaceKey(EEHashEntry_t *pEntry, ContinuationLayoutKey newKey);
};

typedef EEHashTable<ContinuationLayoutKey, ContinuationLayoutKeyHashTableHelper, FALSE> ContinuationLayoutHashTable;

class AsyncContinuationsManager
{
    LoaderAllocator* m_allocator;
    CrstExplicitInit m_layoutsLock;
    ContinuationLayoutHashTable m_layouts;

    MethodTable* CreateNewContinuationMethodTable(unsigned dataSize, const bool* objRefs, MethodDesc* asyncMethod, AllocMemTracker* pamTracker);

    static MethodTable* CreateNewContinuationMethodTable(unsigned dataSize, const bool* objRefs, EEClass* eeClass, LoaderAllocator* allocator, Module* loaderModule, AllocMemTracker* pamTracker);
    static PTR_EEClass GetOrCreateSingletonSubContinuationEEClass();
    static PTR_EEClass CreateSingletonSubContinuationEEClass();
public:
    AsyncContinuationsManager(LoaderAllocator* allocator);
    MethodTable* LookupOrCreateContinuationMethodTable(unsigned dataSize, const bool* objRefs, MethodDesc* asyncMethod);
    void NotifyUnloadingClasses();

    template<typename AppendString, typename AppendNum>
    static void PrintContinuationName(MethodTable* pMT, AppendString append, AppendNum appendNum);
};

typedef DPTR(AsyncContinuationsManager) PTR_AsyncContinuationsManager;

template<typename AppendString, typename AppendNum>
void AsyncContinuationsManager::PrintContinuationName(MethodTable* pMT, AppendString append, AppendNum appendNum)
{
    append("Continuation_", W("Continuation_"));
    appendNum(pMT->GetBaseSize() - (OBJHEADER_SIZE + OFFSETOF__CORINFO_Continuation__data));
    PTR_CGCDesc desc = CGCDesc::GetCGCDescFromMT(pMT);
    PTR_CGCDescSeries lowestSeries = desc->GetLowestSeries();
    for (PTR_CGCDescSeries curSeries = desc->GetHighestSeries(); curSeries >= lowestSeries; curSeries--)
    {
        if (curSeries->GetSeriesOffset() < OFFSETOF__CORINFO_Continuation__data)
        {
            continue;
        }

        append("_", W("_"));
        appendNum((unsigned)(curSeries->GetSeriesOffset() - OFFSETOF__CORINFO_Continuation__data));
        append("_", W("_"));
        appendNum((unsigned)((curSeries->GetSeriesSize() + pMT->GetBaseSize()) / TARGET_POINTER_SIZE));
    }
}

#endif
