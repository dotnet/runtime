// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: BlobHeap.h
//

//
// Classes code:MetaData::BlobHeapRO and code:MetaData::BlobHeapRW represent #Blob heap.
// The #Blob heap stores size-prefixed data chunks (as defined in CLI ECMA specification). Elements are
// indexed by code:#BlobHeapIndex.
//
//#BlobHeapIndex
// Blob heap indexes are 0-based. They are stored the same way in the table columns (i.e. there is no
// 0-based vs. 1-based index difference as in table record indexes code:TableRecordStorage).
//
// ======================================================================================

#pragma once

#include "external.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class represents read-only #Blob heap with all utility methods.
//
class BlobHeapRO
{
    friend class BlobHeapRW;

private:
    //
    // Private data
    //

    // The storage of blobs.
    StgBlobPoolReadOnly m_BlobPool;

public:
    //
    // Initialization
    //

    __checkReturn
    HRESULT Initialize(
        DataBlob sourceData,
        BOOL     fCopyData)
    {
        _ASSERTE(!fCopyData);
        return m_BlobPool.InitOnMemReadOnly((void *)sourceData.GetDataPointer(), sourceData.GetSize());
    }

    inline void Delete()
    {
        return m_BlobPool.Uninit();
    }

public:
    //
    // Getters
    //

    __checkReturn
    inline HRESULT GetBlob(
              UINT32    nIndex,
        __out DataBlob *pData)
    {
        return m_BlobPool.GetBlob(nIndex, pData);
    }

    __checkReturn
    inline HRESULT GetAllData(
        __inout DataBlob *pData)
    {
        return m_BlobPool.GetDataReadOnly(0, pData);
    }

    // Gets raw size (in bytes) of the represented blob data.
    inline UINT32 GetUnalignedSize() const
    {
        return m_BlobPool.GetPoolSize();
    }

    // Returns TRUE if the blob index (nIndex, see code:#BlobHeapIndex) is valid (i.e. in the blob
    // heap).
    inline BOOL IsValidIndex(UINT32 nIndex) const
    {
        return const_cast<StgBlobPoolReadOnly &>(m_BlobPool).IsValidCookie(nIndex);
    }

};  // class BlobHeapRO

// --------------------------------------------------------------------------------------
//
// This class represents read-write #Blob heap with all utility methods.
//
class BlobHeapRW
{
private:
    //
    // Private data
    //

    // The storage of blobs.
    StgBlobPool m_BlobPool;

public:
    //
    // Initialization
    //

    __checkReturn
    HRESULT InitializeEmpty(
                         UINT32 cbAllocationSize
        COMMA_INDEBUG_MD(BOOL   debug_fIsReadWrite))
    {
        return m_BlobPool.InitNew(cbAllocationSize, 0, TRUE);
    }
    __checkReturn
    HRESULT InitializeEmpty_WithItemsCount(
                         UINT32 cbAllocationSize,
                         UINT32 cItemsCount
        COMMA_INDEBUG_MD(BOOL   debug_fIsReadWrite))
    {
        return m_BlobPool.InitNew(cbAllocationSize, cItemsCount, TRUE);
    }
    __checkReturn
    HRESULT InitializeEmpty_WithoutDefaultEmptyBlob(
                         UINT32 cbAllocationSize
        COMMA_INDEBUG_MD(BOOL   debug_fIsReadWrite))
    {
        return m_BlobPool.InitNew(cbAllocationSize, 0, FALSE);
    }

    __checkReturn
    HRESULT Initialize(
        DataBlob sourceData,
        BOOL     fCopyData)
    {
        return m_BlobPool.InitOnMem((void *)sourceData.GetDataPointer(), sourceData.GetSize(), !fCopyData);
    }
    __checkReturn
    HRESULT InitializeFromBlobHeap(
        const BlobHeapRO *pSourceBlobHeap,
        BOOL              fCopyData)
    {
        return m_BlobPool.InitOnMem(
            (void *)pSourceBlobHeap->m_BlobPool.GetSegData(),
            pSourceBlobHeap->m_BlobPool.GetDataSize(),
            !fCopyData);
    }
    __checkReturn
    HRESULT InitializeFromBlobHeap(
        const BlobHeapRW *pSourceBlobHeap,
        BOOL              fCopyData)
    {
        return m_BlobPool.InitOnMem(
            (void *)pSourceBlobHeap->m_BlobPool.GetSegData(),
            pSourceBlobHeap->m_BlobPool.GetDataSize(),
            !fCopyData);
    }

    // Destroys the blob heap and all its allocated data. Can run on uninitialized blob heap.
    inline void Delete()
    {
        return m_BlobPool.Uninit();
    }

public:
    //
    // Getters
    //

    __checkReturn
    inline HRESULT GetBlob(
              UINT32    nIndex,
        __out DataBlob *pData)
    {
        return m_BlobPool.GetBlob(nIndex, pData);
    }

    // Gets the blob with its size-prefix at index (nIndex, see code:#BlobHeapIndex), or returns error.
    //
    // Returns S_OK and the data (*pData) at index (nIndex). The end of the data marks the end of the blob.
    // Returns error code otherwise (and clears *pData).
    //
    // User of this API shouldn't access memory behind the data buffer (*pData).
    __checkReturn
    inline HRESULT GetBlobWithSizePrefix(
              UINT32    nIndex,
        __out DataBlob *pData)
    {
        return m_BlobPool.GetBlobWithSizePrefix(nIndex, pData);
    }

    // Gets raw size (in bytes) of the represented blob data. Doesn't align the size as code:GetAlignedSize.
    inline UINT32 GetUnalignedSize() const
    {
        return m_BlobPool.GetRawSize();
    }
    // Gets size (in bytes) aligned up to 4-bytes of the represented blob data.
    // Fills *pcbSize with 0 on error.
    __checkReturn
    inline HRESULT GetAlignedSize(
        __out UINT32 *pcbSize) const
    {
        return m_BlobPool.GetSaveSize(pcbSize);
    }
    // Returns TRUE if the blob heap is empty (even if it contains only default empty blob).
    inline BOOL IsEmpty() const
    {
        return const_cast<StgBlobPool &>(m_BlobPool).IsEmpty();
    }

    // Returns TRUE if the blob index (nIndex, see code:#BlobHeapIndex) is valid (i.e. in the blob
    // heap).
    inline BOOL IsValidIndex(UINT32 nIndex) const
    {
        return const_cast<StgBlobPool &>(m_BlobPool).IsValidCookie(nIndex);
    }

    __checkReturn
    HRESULT SaveToStream_Aligned(
             UINT32   nStartIndex,
        __in IStream *pStream) const
    {
        if (nStartIndex == 0)
        {
            return const_cast<StgBlobPool &>(m_BlobPool).PersistToStream(pStream);
        }

        if (nStartIndex == m_BlobPool.GetRawSize())
        {
            _ASSERTE(!m_BlobPool.HaveEdits());
            return S_OK;
        }
        _ASSERTE(m_BlobPool.HaveEdits());
        _ASSERTE(nStartIndex == m_BlobPool.GetOffsetOfEdit());
        return const_cast<StgBlobPool &>(m_BlobPool).PersistPartialToStream(pStream, nStartIndex);
    }

public:
    //
    // Heap modifications
    //

    __checkReturn
    inline HRESULT AddBlob(
              DataBlob data,
        __out UINT32  *pnIndex)
    {
        return m_BlobPool.AddBlob(&data, pnIndex);
    }

    __checkReturn
    HRESULT AddBlobHeap(
        const BlobHeapRW *pSourceBlobHeap,
        UINT32            nStartSourceIndex)
    {
        return m_BlobPool.CopyPool(
            nStartSourceIndex,
            &pSourceBlobHeap->m_BlobPool);
    } // BlobHeapRW::AddBlobHeap

    __checkReturn
    inline HRESULT MakeWritable()
    {
        return m_BlobPool.ConvertToRW();
    }

public:
    //
    // Tracking of heap modifications for EnC
    //

    //#EnCSessionTracking
    // EnC session starts automatically with initialization (code:Initialize or code:InitializeEmpty) or by
    // user's explicit call to code:StartNewEnCSession. The heap stores its actual data size, so we can find
    // out if some data were added later.

    // Gets heap size (in bytes) from the beginning of the last EnC session (code:#EnCSessionTracking).
    inline UINT32 GetEnCSessionStartHeapSize() const
    {
        if (m_BlobPool.HaveEdits())
        {
            return m_BlobPool.GetOffsetOfEdit();
        }

        return m_BlobPool.GetRawSize();
    }
    // Starts new EnC session (code:#EnCSessionTracking).
    inline void StartNewEnCSession()
    {
        m_BlobPool.ResetOffsetOfEdit();
    }
    // Gets size (in bytes) aligned to 4-bytes of adds made from the beginning of the last EnC session.
    __checkReturn
    inline HRESULT GetEnCSessionAddedHeapSize_Aligned(
        __out UINT32 *pcbSize) const
    {
        if (m_BlobPool.HaveEdits())
        {
            return m_BlobPool.GetEditSaveSize(pcbSize);
        }

        *pcbSize = 0;
        return S_OK;
    }

};  // class BlobHeapRW

};  // namespace MetaData
