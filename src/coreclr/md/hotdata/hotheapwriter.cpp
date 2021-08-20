// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
