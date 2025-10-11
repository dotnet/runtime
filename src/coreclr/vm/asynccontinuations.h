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
    const CORINFO_CONTINUATION_DATA_OFFSETS& DataOffsets;

    ContinuationLayoutKeyData(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets)
        : DataSize(dataSize)
        , ObjRefs(objRefs)
        , DataOffsets(dataOffsets)
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

    MethodTable* CreateNewContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod, AllocMemTracker* pamTracker);

public:
    AsyncContinuationsManager(LoaderAllocator* allocator);
    MethodTable* LookupOrCreateContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod);
};

typedef DPTR(AsyncContinuationsManager) PTR_AsyncContinuationsManager;

#endif
