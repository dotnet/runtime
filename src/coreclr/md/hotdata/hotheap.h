// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: HotHeap.h
//

//
// Class code:MetaData::HotHeap represents a hot heap in MetaData hot stream.
//
// ======================================================================================

#pragma once

#include "external.h"

class VerifyLayoutsMD;

namespace MetaData
{

// Forward declarations
struct HotHeapHeader;

// --------------------------------------------------------------------------------------
//
// This class represents a hot heap in MetaData hot stream.
//
class HotHeap
{
    friend class ::VerifyLayoutsMD;
private:
    struct HotHeapHeader *m_pHotHeapHeader;

private:
    friend class HotHeapsDirectoryIterator;

    // Initializes hot heap from its header and data.
    __checkReturn
    HRESULT Initialize(struct HotHeapHeader *pHotHeapHeader, DataBuffer hotHeapData);

public:
    HotHeap()
        { m_pHotHeapHeader = NULL; }
    HotHeap(const HotHeap &source)
        { m_pHotHeapHeader = source.m_pHotHeapHeader; }

    void Clear()
        { m_pHotHeapHeader = NULL; }

    // Gets stored data at index.
    // Returns S_FALSE if data index is not stored in hot heap.
    __checkReturn
    HRESULT GetData(
             UINT32    nDataIndex,
        __out DataBlob *pData);

    inline BOOL IsEmpty() const
        { return m_pHotHeapHeader == NULL; }

#ifdef _DEBUG_METADATA
    // Validates hot heap structure (extension of code:Initialize checks).
    __checkReturn
    HRESULT Debug_Validate();
#endif //_DEBUG_METADATA

};  // class HotHeap

};  // namespace MetaData
