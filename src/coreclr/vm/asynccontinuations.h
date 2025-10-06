// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: asynccontinuations.h
//
// ===========================================================================

#ifndef ASYNCCONTINUATIONS_H
#define ASYNCCONTINUATIONS_H

class AsyncContinuationsManager
{
    LoaderAllocator* m_allocator;

    MethodTable* CreateNewContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod, AllocMemTracker* pamTracker);

public:
    AsyncContinuationsManager(LoaderAllocator* allocator);
    MethodTable* LookupOrCreateContinuationMethodTable(unsigned dataSize, const bool* objRefs, const CORINFO_CONTINUATION_DATA_OFFSETS& dataOffsets, MethodDesc* asyncMethod, AllocMemTracker* pamTracker);
};

typedef DPTR(AsyncContinuationsManager) PTR_AsyncContinuationsManager;

#endif
