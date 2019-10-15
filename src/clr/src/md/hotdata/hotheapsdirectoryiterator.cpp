// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: HotHeapsDirectoryIterator.h
// 

// 
// Class code:MetaData::HotHeapsDirectoryIterator represents an iterator through hot heaps directory 
// (code:HotHeapsDirectory).
// 
// ======================================================================================

#include "external.h"

#include "hotheapsdirectoryiterator.h"
#include "hotdataformat.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
// 
// Creates empty iterator.
// 
HotHeapsDirectoryIterator::HotHeapsDirectoryIterator()
{
    m_RemainingHeapsDirectoryData.Clear();
    m_HotHeapsData.Clear();
} // HotHeapsDirectoryIterator::HotHeapsDirectoryIterator

// --------------------------------------------------------------------------------------
// 
// Initialize iteration on heaps directory (hotHeapsDirectoryData) with heap hot data (hotHeapsData).
// The caller guarantees that the heap hot data end where heaps directory beggins.
// 
void 
HotHeapsDirectoryIterator::Initialize(
    DataBuffer hotHeapsDirectoryData, 
    DataBuffer hotHeapsData)
{
    _ASSERTE(hotHeapsData.GetDataPointerBehind() == hotHeapsDirectoryData.GetDataPointer());
    m_RemainingHeapsDirectoryData = hotHeapsDirectoryData;
    m_HotHeapsData = hotHeapsData;
} // HotHeapsDirectoryIterator::Initialize

// --------------------------------------------------------------------------------------
// 
// Gets next hot heap (*pHotHeap, of index *pHotHeapIndex) from the heaps directory.
// Returns S_OK and fills *pHotHeap and *pHotHeapIndex with the next code:HotHeap information.
// Returns S_FALSE, if the last hot heap was already returned. Clears *pHotHeap and *pHotHeapIndex in this 
// case.
// Returns error code if the format is invalid. Clears *pHotHeap and *pHotHeapIndex in this case.
// 
__checkReturn 
HRESULT 
HotHeapsDirectoryIterator::GetNext(
    HotHeap   *pHotHeap, 
    HeapIndex *pHotHeapIndex)
{
    HRESULT hr;
    DataBuffer hotHeapHeaderData;
    DataBuffer hotHeapData;
    
    struct HotHeapsDirectoryEntry *pEntry;
    if (!m_RemainingHeapsDirectoryData.GetData<struct HotHeapsDirectoryEntry>(
        &pEntry))
    {
        hr = S_FALSE;
        goto ErrExit;
    }
    
    if (!HeapIndex::IsValid(pEntry->m_nHeapIndex))
    {
        Debug_ReportError("Invalid hot heaps directory format - invalid heap index.");
        IfFailGo(METADATA_E_INVALID_FORMAT);
    }
    pHotHeapIndex->Set(pEntry->m_nHeapIndex);
    
    hotHeapHeaderData = m_HotHeapsData;
    if (!hotHeapHeaderData.SkipToExactSize(pEntry->m_nHeapHeaderStart_NegativeOffset))
    {
        Debug_ReportError("Invalid hot heaps directory format - heap header offset reaches in front of of hot heaps data.");
        IfFailGo(METADATA_E_INVALID_FORMAT);
    }
    
    struct HotHeapHeader *pHeader;
    if (!hotHeapHeaderData.PeekData<struct HotHeapHeader>(&pHeader))
    {
        Debug_ReportError("Invalid hot heaps directory format - heap header reaches behind hot heaps data.");
        IfFailGo(METADATA_E_INVALID_FORMAT);
    }
    
    hotHeapData = m_HotHeapsData;
    if (!hotHeapData.TruncateBySize(pEntry->m_nHeapHeaderStart_NegativeOffset))
    {
        Debug_ReportInternalError("There's a bug because previous call to SkipToExactSize succeeded.");
        IfFailGo(METADATA_E_INVALID_FORMAT);
    }
    
    IfFailGo(pHotHeap->Initialize(pHeader, hotHeapData));
    _ASSERTE(hr == S_OK);
    return hr;
ErrExit:
    pHotHeap->Clear();
    pHotHeapIndex->SetInvalid();
    return hr;
} // HotHeapsDirectoryIterator::GetNext

};  // namespace MetaData
