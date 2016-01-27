// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: HotHeapWriter.h
// 

// 
// Class code:HotHeapWriter represents a writer of hot heap into MetaData hot stream (collected by IBC).
// 
// ======================================================================================

#pragma once

#include "external.h"
#include "heapindex.h"

// Forward declarations
class CorProfileData;
struct IStream;

namespace MetaData
{

// Forward declarations
class StringHeapRW;
class BlobHeapRW;
class GuidHeapRW;

// --------------------------------------------------------------------------------------
// 
// This class represents a writer of hot heap into MetaData hot stream (collected by IBC).
// 
class HotHeapWriter
{
private:
    // Index of the represented heap (type of the heap).
    HeapIndex m_HeapIndex;
    union
    {
        const StringHeapRW *m_pStringHeap;
        // Both #Blob and #US heaps are represented as code:BlobHeapRW.
        const BlobHeapRW   *m_pBlobHeap;
        const GuidHeapRW   *m_pGuidHeap;
    };
    
public:
    // Creates writer for #String heap.
    HotHeapWriter(const StringHeapRW *pStringHeap);
    // Creates writer for #Blob or #US heap (if fUserStringHeap is TRUE).
    HotHeapWriter(
        const BlobHeapRW *pBlobHeap, 
        BOOL              fUserStringHeap);
    // Creates writer for #GUID heap.
    HotHeapWriter(const GuidHeapRW *pGuidHeap);
    
    // Destroys the writer of hot heap.
    void Delete();
    
    // Stores hot data reported by IBC in profile data (code:CorProfileData) to a stream.
    // Aligns output stream to 4-bytes.
    __checkReturn 
    HRESULT SaveToStream(
        IStream        *pStream, 
        CorProfileData *pProfileData, 
        UINT32         *pnSavedSize) const;
    
    // Returns index of the heap as table index used by IBC (code:CorProfileData).
    UINT32 GetTableIndex() const;
    
private:
    // 
    // Helpers
    // 
    
    // Returns heap data at index (nIndex).
    __checkReturn 
    HRESULT GetData(
        UINT32    nIndex, 
        DataBlob *pData) const;
    
};  // class HotHeapWriter

};  // namespace MetaData
