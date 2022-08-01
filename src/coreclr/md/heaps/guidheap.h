// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: GuidHeap.h
//

//
// Classes code:MetaData::GuidHeapRO and code:MetaData::GuidHeapRW represent #GUID heap.
// The #GUID heap stores size-prefixed data chunks (as defined in CLI ECMA specification). Elements are
// indexed by code:#GuidHeapIndex.
//
//#GuidHeapIndex
// Guid heap indexes are 1-based and they are really indexes, not offsets (as in string heap).
// The indexes correspond to:
//  * 0 ... invalid index,
//  * 1 ... data offset 0,
//  * 2 ... data offset sizeof(GUID),
//  * n ... data offset (n-1)*sizeof(GUID).
// Note that this class provides only translation from 1-based index to 0-based index. The translation of
// 0-based index to data offset is done in code:GuidHeapStorage::GetGuid.
//
// ======================================================================================

#pragma once

#include "external.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class represents read-only #GUID heap with all utility methods.
//
class GuidHeapRO
{
    friend class GuidHeapRW;

private:
    //
    // Private data
    //

    // The storage of guids.
    StgPoolReadOnly m_GuidPool;

public:
    //
    // Initialization
    //

    __checkReturn
    inline HRESULT Initialize(
        DataBlob sourceData,
        BOOL     fCopyData)
    {
        _ASSERTE(!fCopyData);
        return m_GuidPool.InitOnMemReadOnly((void *)sourceData.GetDataPointer(), sourceData.GetSize());
    }

    // Destroys the guid heap and all its allocated data. Can run on uninitialized guid heap.
    inline void Delete()
    {
        return m_GuidPool.Uninit();
    }

public:
    //
    // Getters
    //

    // Gets pointer to guid (*ppGuid) at index (nIndex, see code:#GuidHeapIndex).
    // Returns error code for invalid index (0, or too large index) and sets *ppGuid to NULL.
    __checkReturn
    inline HRESULT GetGuid(
                    UINT32           nIndex,
        _Outptr_ GUID UNALIGNED **ppGuid)
    {
        return m_GuidPool.GetGuid(nIndex, ppGuid);
    }
    __checkReturn
    inline HRESULT GetGuid(
                    UINT32                 nIndex,
        _Outptr_ const GUID UNALIGNED **ppGuid) const
    {
        return const_cast<StgPoolReadOnly &>(m_GuidPool).GetGuid(nIndex, const_cast<GUID UNALIGNED **>(ppGuid));
    }

    inline UINT32 GetSize() const
    {
        return const_cast<StgPoolReadOnly &>(m_GuidPool).GetPoolSize();
    }

};  // class GuidHeapRO

// --------------------------------------------------------------------------------------
//
// This class represents read-write #GUID heap with all utility methods.
//
class GuidHeapRW
{
private:
    //
    // Private data
    //

    // The storage of guids.
    StgGuidPool m_GuidPool;

public:
    //
    // Initialization
    //

    __checkReturn
    inline HRESULT InitializeEmpty(
                         UINT32 cbAllocationSize
        COMMA_INDEBUG_MD(BOOL   debug_fIsReadWrite))
    {
        return m_GuidPool.InitNew(cbAllocationSize, 0);
    }
    __checkReturn
    inline HRESULT InitializeEmpty_WithItemsCount(
                         UINT32 cbAllocationSize,
                         UINT32 cItemsCount
        COMMA_INDEBUG_MD(BOOL   debug_fIsReadWrite))
    {
        return m_GuidPool.InitNew(cbAllocationSize, cItemsCount);
    }
    __checkReturn
    inline HRESULT Initialize(
        DataBlob sourceData,
        BOOL     fCopyData)
    {
        return m_GuidPool.InitOnMem((void *)sourceData.GetDataPointer(), sourceData.GetSize(), !fCopyData);
    }

    __checkReturn
    inline HRESULT InitializeFromGuidHeap(
        const GuidHeapRO *pSourceGuidHeap,
        BOOL              fCopyData)
    {
        return m_GuidPool.InitOnMem(
            (void *)pSourceGuidHeap->m_GuidPool.GetSegData(),
            pSourceGuidHeap->m_GuidPool.GetDataSize(),
            !fCopyData);
    }
    __checkReturn
    inline HRESULT InitializeFromGuidHeap(
        const GuidHeapRW *pSourceGuidHeap,
        BOOL              fCopyData)
    {
        return m_GuidPool.InitOnMem(
            (void *)pSourceGuidHeap->m_GuidPool.GetSegData(),
            pSourceGuidHeap->m_GuidPool.GetDataSize(),
            !fCopyData);
    }

    // Destroys the guid heap and all its allocated data. Can run on uninitialized guid heap.
    inline void Delete()
    {
        return m_GuidPool.Uninit();
    }

public:
    //
    // Getters
    //

    __checkReturn
    inline HRESULT GetGuid(
                    UINT32           nIndex,
        _Outptr_ GUID UNALIGNED **ppGuid)
    {
        return m_GuidPool.GetGuid(nIndex, ppGuid);
    }
    __checkReturn
    inline HRESULT GetGuid(
                    UINT32                 nIndex,
        _Outptr_ const GUID UNALIGNED **ppGuid) const
    {
        return const_cast<StgGuidPool &>(m_GuidPool).GetGuid(nIndex, const_cast<GUID UNALIGNED **>(ppGuid));
    }

    // Gets size (in bytes) of the represented guid data. Note: the size is everytime aligned.
    inline UINT32 GetSize() const
    {
        _ASSERTE(m_GuidPool.GetRawSize() % sizeof(GUID) == 0);
        return m_GuidPool.GetRawSize();
    }

    // Returns TRUE if the guid heap is empty.
    inline BOOL IsEmpty() const
    {
        return const_cast<StgGuidPool &>(m_GuidPool).IsEmpty();
    }

    // Returns TRUE if the guid index (nIndex, see code:#GuidHeapIndex) is valid (i.e. is in the guid
    // heap).
    // Note: index 0 is considered invalid.
    inline BOOL IsValidIndex(UINT32 nIndex) const
    {
        return const_cast<StgGuidPool &>(m_GuidPool).IsValidCookie(nIndex);
    }

    __checkReturn
    inline HRESULT SaveToStream(
        _In_ IStream *pStream) const
    {
        return const_cast<StgGuidPool &>(m_GuidPool).PersistToStream(pStream);
    }

public:
    //
    // Heap modifications
    //

    // Adds guid (*pGuid) to the end of the heap.
    // Returns S_OK and index (*pnIndex, see code:#GuidHeapIndex) of added GUID.
    // Returns error code otherwise (and fills *pnIndex with 0 - an invalid GUID index).
    __checkReturn
    inline HRESULT AddGuid(
        _In_  const GUID *pGuid,
        _Out_ UINT32     *pnIndex)
    {
        return m_GuidPool.AddGuid(pGuid, pnIndex);
    }

    // Adds data from *pSourceGuidHeap starting at index (nStartSourceIndex) to the guid heap.
    // Returns S_OK (even if the source is empty) or error code.
    __checkReturn
    HRESULT AddGuidHeap(
        const GuidHeapRW *pSourceGuidHeap,
        UINT32            nStartSourceIndex)
    {
        return m_GuidPool.CopyPool(
            nStartSourceIndex,
            &pSourceGuidHeap->m_GuidPool);
    } // GuidHeapRW::AddGuidHeap

    __checkReturn
    inline HRESULT MakeWritable()
    {
        return m_GuidPool.ConvertToRW();
    }

};  // class GuidHeapRW

};  // namespace MetaData
