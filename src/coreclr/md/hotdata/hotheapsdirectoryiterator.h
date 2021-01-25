// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: HotHeapsDirectoryIterator.h
//

//
// Class code:MetaData::HotHeapsDirectoryIterator represents an iterator through hot heaps directory
// (code:HotHeapsDirectory).
//
// ======================================================================================

#pragma once

#include "external.h"
#include "heapindex.h"
#include "hotheap.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class represents an iterator through hot heaps directory (code:HotHeapsDirectory), i.e. through an
// array of code:HotHeapsDirectoryEntry.
//
class HotHeapsDirectoryIterator
{
private:
    //
    // Private data
    //

    // Remaining data from the heaps directory. On each iteration this will be shrinked (the
    // code:HotHeapsDirectoryEntry will be skipped).
    DataBuffer m_RemainingHeapsDirectoryData;
    // Data for the hot heaps. It has to end exactly where heaps directory starts.
    DataBuffer m_HotHeapsData;

private:
    //
    // Operations with restricted access
    //

    // code:HotMetaData is the only class allowed to create this iteration.
    friend class HotMetaData;

    // Initialize iteration on heaps directory (hotHeapsDirectoryData) with heap hot data (hotHeapsData).
    // The caller guarantees that the heap hot data end where heaps directory beggins.
    void Initialize(
        DataBuffer hotHeapsDirectoryData,
        DataBuffer hotHeapsData);

public:
    //
    // Operations
    //

    // Creates empty iterator.
    HotHeapsDirectoryIterator();

    // S_OK, S_FALSE, error code (clears the HotHeap if not S_OK)
    __checkReturn
    HRESULT GetNext(
        HotHeap   *pHotHeap,
        HeapIndex *pHotHeapIndex);

};  // class HotHeapsDirectoryIterator

};  // namespace MetaData
