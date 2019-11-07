// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: HotMetaData.cpp
//

//
// Class code:MetaData::HotMetaData represents a reader of hot data in MetaData hot stream.
//
// ======================================================================================

#include "external.h"

#include "hotmetadata.h"
#include "hotdataformat.h"
#include "hotheapsdirectoryiterator.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// Class code:MetaData::HotMetaData represents a reader of hot data in MetaData hot stream.
//
__checkReturn
HRESULT
HotMetaData::Initialize(
    DataBuffer data)
{
    m_Data = data;

    return S_OK;
} // HotMetaData::Initialize

// --------------------------------------------------------------------------------------
//
// Returns iterator of stored hot heaps (code:HotHeap).
//
__checkReturn
HRESULT
HotMetaData::GetHeapsDirectoryIterator(
    HotHeapsDirectoryIterator *pHeapsDirectoryIterator)
{
    if (m_Data.GetSize() < sizeof(struct HotMetaDataHeader))
    {
        Debug_ReportError("Invalid hot MetaData format - header doesn't fit into the hot data.");
        return METADATA_E_INVALID_FORMAT;
    }

    struct HotMetaDataHeader *pHeader;
    if (!m_Data.PeekDataAt<struct HotMetaDataHeader>(
        m_Data.GetSize() - sizeof(struct HotMetaDataHeader),
        &pHeader))
    {
        Debug_ReportInternalError("There's a bug, because previous size check succeeded.");
        return METADATA_E_INTERNAL_ERROR;
    }
    // Get rid of the read header
    DataBuffer heapsData = m_Data;
    if (!heapsData.TruncateBySize(sizeof(struct HotMetaDataHeader)))
    {
        Debug_ReportInternalError("There's a bug, because previous size check succeeded.");
        return METADATA_E_INTERNAL_ERROR;
    }
    DataBuffer heapsDirectoryData = heapsData;
    if (!heapsDirectoryData.SkipToExactSize(pHeader->m_nHeapsDirectoryStart_NegativeOffset))
    {
        Debug_ReportError("Invalid hot MetaData format - heaps directory offset reaches in front of hot data.");
        return METADATA_E_INVALID_FORMAT;
    }
    if (!heapsData.TruncateBySize(pHeader->m_nHeapsDirectoryStart_NegativeOffset))
    {
        Debug_ReportInternalError("There's a bug, because previous call to SkipToExactSize succeeded.");
        return METADATA_E_INVALID_FORMAT;
    }

    pHeapsDirectoryIterator->Initialize(
        heapsDirectoryData,
        heapsData);

    return S_OK;
} // HotMetaData::GetHeapsDirectoryIterator

};  // namespace MetaData
