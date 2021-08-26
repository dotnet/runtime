// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: StringHeap.h
//

//
// Classes code:MetaData::StringHeapRO and code:MetaData::StringHeapRW represent #String heap.
// The #String heap stores null-terminated UTF-8 strings (as defined in CLI ECMA specification). Elements
// are indexed by code:#StringHeapIndex.
//
//#StringHeapIndex
// String heap indexes are 0-based. They are stored the same way in the table columns (i.e. there is no
// 0-based vs. 1-based index difference as in table record indexes code:TableRecordStorage).
//
// ======================================================================================

#pragma once

#include "external.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class represents read-only #String heap with all utility methods.
//
class StringHeapRO
{
    friend class StringHeapRW;

private:
    //
    // Private data
    //

    // The storage of strings.
    StgPoolReadOnly m_StringPool;

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
        return m_StringPool.InitOnMemReadOnly((void *)sourceData.GetDataPointer(), sourceData.GetSize());
    }

    inline void Delete()
    {
        return m_StringPool.Uninit();
    }

public:
    //
    // Getters
    //

    __checkReturn
    inline HRESULT GetString(
                      UINT32  nIndex,
        __deref_out_z LPCSTR *pszString) const
    {
        return const_cast<StgPoolReadOnly &>(m_StringPool).GetStringReadOnly(
            nIndex,
            pszString);
    }

    // Gets raw size (in bytes) of the represented strings.
    inline UINT32 GetUnalignedSize() const
    {
        return m_StringPool.GetPoolSize();
    }

};  // class StringHeapRO

// --------------------------------------------------------------------------------------
//
// This class represents read-write #String heap with all utility methods.
//
class StringHeapRW
{
private:
    //
    // Private data
    //

    // The storage of strings.
    StgStringPool m_StringPool;

public:
    //
    // Initialization
    //

    __checkReturn
    inline HRESULT InitializeEmpty(
                         UINT32 cbAllocationSize
        COMMA_INDEBUG_MD(BOOL   debug_fIsReadWrite))
    {
        return m_StringPool.InitNew(cbAllocationSize, 0);
    }
    __checkReturn
    inline HRESULT InitializeEmpty_WithItemsCount(
                         UINT32 cbAllocationSize,
                         UINT32 cItemsCount
        COMMA_INDEBUG_MD(BOOL   debug_fIsReadWrite))
    {
        return m_StringPool.InitNew(cbAllocationSize, cItemsCount);
    }
    __checkReturn
    inline HRESULT Initialize(
        DataBlob sourceData,
        BOOL     fCopyData)
    {
        return m_StringPool.InitOnMem((void *)sourceData.GetDataPointer(), sourceData.GetSize(), !fCopyData);
    }
    __checkReturn
    inline HRESULT InitializeFromStringHeap(
        const StringHeapRO *pSourceStringHeap,
        BOOL                fCopyData)
    {
        return m_StringPool.InitOnMem(
            (void *)pSourceStringHeap->m_StringPool.GetSegData(),
            pSourceStringHeap->m_StringPool.GetDataSize(),
            !fCopyData);
    }
    __checkReturn
    inline HRESULT InitializeFromStringHeap(
        const StringHeapRW *pSourceStringHeap,
        BOOL                fCopyData)
    {
        return m_StringPool.InitOnMem(
            (void *)pSourceStringHeap->m_StringPool.GetSegData(),
            pSourceStringHeap->m_StringPool.GetDataSize(),
            !fCopyData);
    }

    // Destroys the string heap and all its allocated data. Can run on uninitialized string heap.
    inline void Delete()
    {
        return m_StringPool.Uninit();
    }

public:
    //
    // Getters
    //

    __checkReturn
    inline HRESULT GetString(
                      UINT32  nIndex,
        __deref_out_z LPCSTR *pszString) const
    {
        return const_cast<StgStringPool &>(m_StringPool).GetString(
            nIndex,
            pszString);
    }

    // Gets raw size (in bytes) of the represented strings. Doesn't align the size as code:GetAlignedSize.
    inline UINT32 GetUnalignedSize() const
    {
        return m_StringPool.GetRawSize();
    }
    // Gets size (in bytes) aligned up to 4-bytes of the represented strings.
    // Fills *pcbSize with 0 on error.
    __checkReturn
    inline HRESULT GetAlignedSize(
        __out UINT32 *pcbSize) const
    {
        return m_StringPool.GetSaveSize(pcbSize);
    }
    // Returns TRUE if the string heap is empty (even if it contains only default empty string).
    inline BOOL IsEmpty() const
    {
        return const_cast<StgStringPool &>(m_StringPool).IsEmpty();
    }

    // Returns TRUE if the string index (nIndex, see code:#StringHeapIndex) is valid (i.e. in the string
    // heap).
    inline BOOL IsValidIndex(UINT32 nIndex) const
    {
        return const_cast<StgStringPool &>(m_StringPool).IsValidCookie(nIndex);
    }

    __checkReturn
    inline HRESULT SaveToStream_Aligned(
             UINT32   nStartIndex,
        __in IStream *pStream) const
    {
        if (nStartIndex == 0)
        {
            return const_cast<StgStringPool &>(m_StringPool).PersistToStream(pStream);
        }

        if (nStartIndex == m_StringPool.GetRawSize())
        {
            _ASSERTE(!m_StringPool.HaveEdits());
            return S_OK;
        }
        _ASSERTE(m_StringPool.HaveEdits());
        _ASSERTE(nStartIndex == m_StringPool.GetOffsetOfEdit());
        return const_cast<StgStringPool &>(m_StringPool).PersistPartialToStream(pStream, nStartIndex);
    }

public:
    //
    // Heap modifications
    //

    // Adds null-terminated UTF-8 string (szString) to the end of the heap (incl. its null-terminator).
    // Returns S_OK and index of added string (*pnIndex).
    // Returns error code otherwise (and fills *pnIndex with 0).
    __checkReturn
    inline HRESULT AddString(
        __in_z LPCSTR  szString,
        __out  UINT32 *pnIndex)
    {
        return m_StringPool.AddString(szString, pnIndex);
    }
    // Adds null-terminated UTF-16 string (wszString) to the end of the heap (incl. its null-terminator).
    // Returns S_OK and index of added string (*pnIndex).
    // Returns error code otherwise (and fills *pnIndex with 0).
    __checkReturn
    inline HRESULT AddStringW(
        __in_z LPCWSTR wszString,
        __out  UINT32 *pnIndex)
    {
        return m_StringPool.AddStringW(wszString, pnIndex);
    }

    // Adds data from *pSourceStringHeap starting at index (nStartSourceIndex) to the string heap.
    // Returns S_OK (even if the source is empty) or error code.
    __checkReturn
    inline HRESULT AddStringHeap(
        const StringHeapRW *pSourceStringHeap,
        UINT32              nStartSourceIndex)
    {
        return m_StringPool.CopyPool(
            nStartSourceIndex,
            &pSourceStringHeap->m_StringPool);
    } // StringHeapRW::AddStringHeap

    __checkReturn
    inline HRESULT MakeWritable()
    {
        return m_StringPool.ConvertToRW();
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
        if (m_StringPool.HaveEdits())
        {
            return m_StringPool.GetOffsetOfEdit();
        }

        return m_StringPool.GetRawSize();
    }
    // Starts new EnC session (code:#EnCSessionTracking).
    inline void StartNewEnCSession()
    {
        m_StringPool.ResetOffsetOfEdit();
    }
    // Gets size (in bytes) aligned to 4-bytes of adds made from the beginning of the last EnC session.
    __checkReturn
    inline HRESULT GetEnCSessionAddedHeapSize_Aligned(
        __out UINT32 *pcbSize) const
    {
        if (m_StringPool.HaveEdits())
        {
            return m_StringPool.GetEditSaveSize(pcbSize);
        }

        *pcbSize = 0;
        return S_OK;
    }

};  // class StringHeapRW

};  // namespace MetaData
