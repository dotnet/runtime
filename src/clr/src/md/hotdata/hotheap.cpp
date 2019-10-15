// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: HotHeap.cpp
// 

// 
// Class code:MetaData::HotHeap represents a hot heap in MetaData hot stream.
// 
// ======================================================================================

#include "external.h"

#include "hotheap.h"
#include "hotdataformat.h"
#include <utilcode.h>

namespace MetaData
{

// --------------------------------------------------------------------------------------
// 
// Initializes hot heap from its header and data.
// Provides limited debug-only validation of the structure.
// 
__checkReturn 
HRESULT 
HotHeap::Initialize(
    struct HotHeapHeader *pHotHeapHeader, 
    DataBuffer            hotHeapData)
{
    _ASSERTE(hotHeapData.GetDataPointerBehind() == reinterpret_cast<BYTE *>(pHotHeapHeader));
    
    UINT32 nMaximumNegativeOffset = hotHeapData.GetSize();
    if (pHotHeapHeader->m_nIndexTableStart_NegativeOffset > nMaximumNegativeOffset)
    {
        m_pHotHeapHeader = NULL;
        Debug_ReportError("Invalid hot heap header format - invalid index table offset.");
        return METADATA_E_INVALID_FORMAT;
    }
    if ((pHotHeapHeader->m_nIndexTableStart_NegativeOffset % 4) != 0)
    {
        m_pHotHeapHeader = NULL;
        Debug_ReportError("Invalid hot heap header format - index table offset is not aligned.");
        return METADATA_E_INVALID_FORMAT;
    }
    if (pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset > nMaximumNegativeOffset)
    {
        m_pHotHeapHeader = NULL;
        Debug_ReportError("Invalid hot heap header format - invalid value offset table offset.");
        return METADATA_E_INVALID_FORMAT;
    }
    if ((pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset % 4) != 0)
    {
        m_pHotHeapHeader = NULL;
        Debug_ReportError("Invalid hot heap header format - value offset table offset is not aligned.");
        return METADATA_E_INVALID_FORMAT;
    }
    // Index table has to be behind value offset table
    if (pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset < pHotHeapHeader->m_nIndexTableStart_NegativeOffset)
    {
        m_pHotHeapHeader = NULL;
        Debug_ReportError("Invalid hot heap header format - value offset table doesn't start before index table.");
        return METADATA_E_INVALID_FORMAT;
    }
    if (pHotHeapHeader->m_nValueHeapStart_NegativeOffset > nMaximumNegativeOffset)
    {
        m_pHotHeapHeader = NULL;
        Debug_ReportError("Invalid hot heap header format - invalid value heap offset.");
        return METADATA_E_INVALID_FORMAT;
    }
    m_pHotHeapHeader = pHotHeapHeader;
    return S_OK;
} // HotHeap::Initialize

#ifdef _DEBUG_METADATA
// --------------------------------------------------------------------------------------
// 
// Validates hot heap structure (extension of code:Initialize checks).
// 
__checkReturn 
HRESULT 
HotHeap::Debug_Validate()
{
    // Additional verification, more strict checks than in code:Initialize
    S_UINT32 nValueOffsetTableStart = 
        S_UINT32(2) * 
        S_UINT32(m_pHotHeapHeader->m_nIndexTableStart_NegativeOffset);
    if (nValueOffsetTableStart.IsOverflow() || 
        (nValueOffsetTableStart.Value() != m_pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset))
    {
        Debug_ReportError("Invalid hot heap header format.");
        return METADATA_E_INVALID_FORMAT;
    }
    if (m_pHotHeapHeader->m_nValueHeapStart_NegativeOffset <= m_pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset)
    {
        Debug_ReportError("Invalid hot heap header format.");
        return METADATA_E_INVALID_FORMAT;
    }
    
    // Already validated against underflow in code:Initialize
    BYTE   *pIndexTableStart = reinterpret_cast<BYTE *>(m_pHotHeapHeader) - m_pHotHeapHeader->m_nIndexTableStart_NegativeOffset;
    UINT32 *rgIndexTable = reinterpret_cast<UINT32 *>(pIndexTableStart);
    // Already validated against underflow in code:Initialize
    BYTE   *pValueOffsetTableStart = reinterpret_cast<BYTE *>(m_pHotHeapHeader) - m_pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset;
    UINT32 *rgValueOffsetTable = reinterpret_cast<UINT32 *>(pValueOffsetTableStart);
    // Already validated against underflow in code:Initialize
    BYTE *pValueHeapStart = reinterpret_cast<BYTE *>(m_pHotHeapHeader) - m_pHotHeapHeader->m_nValueHeapStart_NegativeOffset;
    DataBuffer valueHeap(
        pValueHeapStart, 
        m_pHotHeapHeader->m_nValueHeapStart_NegativeOffset - m_pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset);
    
    // Already validated for % 4 == 0 in code:Initialize
    UINT32 cIndexTableCount = m_pHotHeapHeader->m_nIndexTableStart_NegativeOffset / 4;
    UINT32 nPreviousValue = 0;
    for (UINT32 nIndex = 0; nIndex < cIndexTableCount; nIndex++)
    {
        if (nPreviousValue >= rgIndexTable[nIndex])
        {
            Debug_ReportError("Invalid hot heap header format.");
            return METADATA_E_INVALID_FORMAT;
        }
        UINT32 nValueOffset = rgValueOffsetTable[nIndex];
        if (nValueOffset >= valueHeap.GetSize())
        {
            Debug_ReportError("Invalid hot heap header format.");
            return METADATA_E_INVALID_FORMAT;
        }
        // TODO: Verify item (depends if it is string, blob, guid or user string)
    }
    return S_OK;
} // HotHeap::Debug_Validate
#endif //_DEBUG_METADATA

// --------------------------------------------------------------------------------------
// 
// Gets stored data at index.
// Returns S_FALSE if data index is not stored in hot heap.
// 
__checkReturn 
HRESULT 
HotHeap::GetData(
         UINT32    nDataIndex, 
    __in DataBlob *pData)
{
    // Already validated against underflow in code:Initialize
    BYTE *pIndexTableStart = reinterpret_cast<BYTE *>(m_pHotHeapHeader) - m_pHotHeapHeader->m_nIndexTableStart_NegativeOffset;
    // Already validated against underflow in code:Initialize
    BYTE *pValueOffsetTableStart = reinterpret_cast<BYTE *>(m_pHotHeapHeader) - m_pHotHeapHeader->m_nValueOffsetTableStart_NegativeOffset;
    // Already validated against underflow in code:Initialize
    BYTE *pValueHeapStart = reinterpret_cast<BYTE *>(m_pHotHeapHeader) - m_pHotHeapHeader->m_nValueHeapStart_NegativeOffset;
    
    const UINT32 *pnFoundDataIndex = BinarySearch<UINT32>(
        reinterpret_cast<UINT32 *>(pIndexTableStart),
        m_pHotHeapHeader->m_nIndexTableStart_NegativeOffset / sizeof(UINT32),
        nDataIndex);

    if (pnFoundDataIndex == NULL)
    {   // Index is not stored in hot data
        return S_FALSE;
    }
    _ASSERTE(((UINT32 *)pIndexTableStart <= pnFoundDataIndex) && 
        (pnFoundDataIndex + 1 <= (UINT32 *)m_pHotHeapHeader));
    
    // Index of found data index in the index table (note: it is not offset, but really index)
    UINT32 nIndexOfFoundDataIndex = (UINT32)(pnFoundDataIndex - (UINT32 *)pIndexTableStart);
    
    // Value offset contains positive offset to the ValueHeap start
    // Already validated against overflow in code:Initialize
    UINT32 nValueOffset_PositiveOffset = reinterpret_cast<UINT32 *>(pValueOffsetTableStart)[nIndexOfFoundDataIndex];
    if (nValueOffset_PositiveOffset >= m_pHotHeapHeader->m_nValueHeapStart_NegativeOffset)
    {
        pData->Clear();
        Debug_ReportError("Invalid hot data format - value offset reaches behind the hot heap data.");
        return METADATA_E_INVALID_FORMAT;
    }
    pData->Init(
        pValueHeapStart + nValueOffset_PositiveOffset, 
        m_pHotHeapHeader->m_nValueHeapStart_NegativeOffset - nValueOffset_PositiveOffset);
    
    return S_OK;
} // HotHeap::GetData

};  // namespace MetaData
