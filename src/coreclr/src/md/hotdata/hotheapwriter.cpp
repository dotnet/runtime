// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: HotHeapWriter.cpp
// 

// 
// Class code:HotHeapWriter represents a writer of hot heap into MetaData hot stream (collected by IBC).
// 
// ======================================================================================

#include "external.h"

#include "hotheapwriter.h"
#include "../heaps/export.h"

#include <stgpool.h>
#include <metamodelpub.h>
#include <utilcode.h>
#include "../inc/streamutil.h"

#include "hotdataformat.h"

#ifdef FEATURE_PREJIT
// Cannot be included without FEATURE_PREJIT:
#include <corcompile.h>
#endif //FEATURE_PREJIT

namespace MetaData
{

// --------------------------------------------------------------------------------------
// 
// Creates writer for #String heap.
// 
HotHeapWriter::HotHeapWriter(
    const StringHeapRW *pStringHeap)
{
    m_HeapIndex = HeapIndex::StringHeapIndex;
    m_pStringHeap = pStringHeap;
} // HotHeapWriter::HotHeapWriter

// --------------------------------------------------------------------------------------
// 
// Creates writer for #Blob or #US heap (if fUserStringHeap is TRUE).
// 
HotHeapWriter::HotHeapWriter(
    const BlobHeapRW *pBlobHeap, 
    BOOL              fUserStringHeap)
{
    m_HeapIndex = fUserStringHeap ? HeapIndex::UserStringHeapIndex : HeapIndex::BlobHeapIndex;
    m_pBlobHeap = pBlobHeap;
} // HotHeapWriter::HotHeapWriter

// --------------------------------------------------------------------------------------
// 
// Creates writer for #GUID heap.
// 
HotHeapWriter::HotHeapWriter(
    const GuidHeapRW *pGuidHeap)
{
    m_HeapIndex = HeapIndex::GuidHeapIndex;
    m_pGuidHeap = pGuidHeap;
} // HotHeapWriter::HotHeapWriter

// --------------------------------------------------------------------------------------
// 
// Destroys the writer of hot heap.
// 
void 
HotHeapWriter::Delete()
{
} // HotHeapWriter::Delete

typedef struct _RidOffsetPair
{
    ULONG rid;
    ULONG offset;
    // compare function for qsort
    static int __cdecl Compare(void const *_x, void const *_y);
} RidOffsetPair;

// static 
int __cdecl 
RidOffsetPair::Compare(void const *_x, void const *_y)
{
    RidOffsetPair const *x = reinterpret_cast<RidOffsetPair const *>(_x);
    RidOffsetPair const *y = reinterpret_cast<RidOffsetPair const *>(_y);
    
    return x->rid - y->rid;
}

// --------------------------------------------------------------------------------------
// 
// Stores hot data reported by IBC in profile data (code:CorProfileData) to a stream.
// Aligns output stream to 4-bytes.
// 
__checkReturn 
HRESULT 
HotHeapWriter::SaveToStream(
    IStream        *pStream, 
    CorProfileData *pProfileData, 
    UINT32         *pnSavedSize) const
{
    _ASSERTE(pStream != NULL);
    _ASSERTE(pProfileData != NULL);
    _ASSERTE(pnSavedSize != NULL);
    
#ifdef FEATURE_PREJIT
    HRESULT hr = S_OK;
    UINT32 nOffset = 0;
    UINT32 nValueHeapStart_PositiveOffset;
    UINT32 nValueOffsetTableStart_PositiveOffset;
    UINT32 nIndexTableStart_PositiveOffset;
    
    // data
    //
    
    // number of hot tokens
    UINT32 nHotItemsCount = pProfileData->GetHotTokens(
        GetTableIndex(), 
        1 << ProfilingFlags_MetaData, 
        1 << ProfilingFlags_MetaData, 
        NULL, 
        0);
    CONSISTENCY_CHECK(nHotItemsCount != 0);
    
    NewArrayHolder<UINT32> hotItemArr = new (nothrow) UINT32[nHotItemsCount];
    IfNullRet(hotItemArr);
    
    // get hot tokens
    static_assert_no_msg(sizeof(UINT32) == sizeof(mdToken));
    pProfileData->GetHotTokens(
        GetTableIndex(), 
        1 << ProfilingFlags_MetaData, 
        1 << ProfilingFlags_MetaData, 
        reinterpret_cast<mdToken *>(&hotItemArr[0]), 
        nHotItemsCount);
    
    // convert tokens to rids
    for (UINT32 i = 0; i < nHotItemsCount; i++)
    {
        hotItemArr[i] = RidFromToken(hotItemArr[i]);
    }
    
    NewArrayHolder<RidOffsetPair> offsetMapping = new (nothrow) RidOffsetPair[nHotItemsCount];
    IfNullRet(offsetMapping);
    
    // write data
    nValueHeapStart_PositiveOffset = nOffset;
    
    // note that we write hot items in the order they appear in pProfileData->GetHotTokens
    // this is so that we preserve the ordering optimizations done by IbcMerge
    for (UINT32 i = 0; i < nHotItemsCount; i++)
    {
        DataBlob data;
        IfFailRet(GetData(
            hotItemArr[i], 
            &data));
        
        // keep track of the offset at which each hot item is written
        offsetMapping[i].rid = hotItemArr[i];
        offsetMapping[i].offset = nOffset;
        
        IfFailRet(StreamUtil::WriteToStream(
            pStream, 
            data.GetDataPointer(), 
            data.GetSize(), 
            &nOffset));
    }
    
    IfFailRet(StreamUtil::AlignDWORD(pStream, &nOffset));
    
    // sort by rid so that a hot rid can be looked up by binary search
    qsort(offsetMapping, nHotItemsCount, sizeof(RidOffsetPair), RidOffsetPair::Compare);
    
    // initialize table of offsets to data
    NewArrayHolder<UINT32> dataIndices = new (nothrow) UINT32[nHotItemsCount];
    IfNullRet(dataIndices);
    
    // fill in the hotItemArr (now sorted by rid) and dataIndices array with each offset
    for (UINT32 i = 0; i < nHotItemsCount; i++)
    {
        hotItemArr[i] = offsetMapping[i].rid;
        dataIndices[i] = offsetMapping[i].offset;
    }
    
    // table of offsets to data
    //
    
    nValueOffsetTableStart_PositiveOffset = nOffset;
    IfFailRet(StreamUtil::WriteToStream(pStream, &dataIndices[0], sizeof(UINT32) * nHotItemsCount, &nOffset));
    
    // rid table (sorted)
    //
    
    nIndexTableStart_PositiveOffset = nOffset;
    
    IfFailRet(StreamUtil::WriteToStream(pStream, &hotItemArr[0], nHotItemsCount * sizeof(UINT32), &nOffset));
    IfFailRet(StreamUtil::AlignDWORD(pStream, &nOffset));
    
    {
        // hot pool header
        struct HotHeapHeader header;
        
        // fix offsets
        header.m_nIndexTableStart_NegativeOffset = nOffset - nIndexTableStart_PositiveOffset;
        header.m_nValueOffsetTableStart_NegativeOffset = nOffset - nValueOffsetTableStart_PositiveOffset;
        header.m_nValueHeapStart_NegativeOffset = nOffset - nValueHeapStart_PositiveOffset;
        
        // write header
        IfFailRet(StreamUtil::WriteToStream(pStream, &header, sizeof(header), &nOffset));
    }
    
    *pnSavedSize = nOffset;
    
#endif //FEATURE_PREJIT
    
    return S_OK;
} // HotHeapWriter::PersistHotToStream

// --------------------------------------------------------------------------------------
// 
// Returns index of the heap as table index used by IBC (code:CorProfileData).
// 
UINT32 
HotHeapWriter::GetTableIndex() const
{
    return TBL_COUNT + m_HeapIndex.Get();
} // HotHeapWriter::GetTableIndex

// --------------------------------------------------------------------------------------
// 
// Returns heap data at index (nIndex).
// 
__checkReturn 
HRESULT 
HotHeapWriter::GetData(
    UINT32    nIndex, 
    DataBlob *pData) const
{
    HRESULT hr;
    
    switch (m_HeapIndex.Get())
    {
    case HeapIndex::StringHeapIndex:
        {
            LPCSTR szString;
            IfFailGo(m_pStringHeap->GetString(
                nIndex, 
                &szString));
            _ASSERTE(hr == S_OK);
            
            // This should not overflow, because we checked it before, but it doesn't hurt
            S_UINT32 cbStringSize = S_UINT32(strlen(szString)) + S_UINT32(1);
            if (cbStringSize.IsOverflow())
            {
                Debug_ReportInternalError("There's a bug in the string heap consistency - string is too long.");
                IfFailGo(METADATA_E_INTERNAL_ERROR);
            }
            
            pData->Init((BYTE *)szString, cbStringSize.Value());
            return S_OK;
        }
    case HeapIndex::GuidHeapIndex:
        {
            // The nIndex is in fact 0-based offset into GUID heap (0, 16, 32, ...), convert it to 1-based element index (1, 2, 3, ...) for GetGuid method
            if ((nIndex % sizeof(GUID)) != 0)
            {
                Debug_ReportInternalError("There's a bug in the caller/IBC - this should be GUID offset aligned to 16-B.");
                IfFailGo(METADATA_E_INTERNAL_ERROR);
            }
            nIndex = (nIndex / sizeof(GUID)) + 1;
            
            GUID UNALIGNED *pGuid;
            IfFailGo(const_cast<GuidHeapRW *>(m_pGuidHeap)->GetGuid(
                nIndex, 
                &pGuid));
            _ASSERTE(hr == S_OK);
            pData->Init((BYTE *)pGuid, sizeof(GUID));
            return S_OK;
        }
    case HeapIndex::BlobHeapIndex:
    case HeapIndex::UserStringHeapIndex:
        {
            IfFailGo(const_cast<BlobHeapRW *>(m_pBlobHeap)->GetBlobWithSizePrefix(
                nIndex, 
                pData));
            _ASSERTE(hr == S_OK);
            
            return S_OK;
        }
    default:
        Debug_ReportInternalError("There's a bug in the caller - this is wrong heap index.");
        IfFailGo(METADATA_E_INTERNAL_ERROR);
    }
    return S_OK;
    
ErrExit:
    pData->Clear();
    return hr;
} // HotHeapWriter::GetData

};  // namespace MetaData
