// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: Table.h
//

//
// Class code:MetaData::Table represents a MetaData table.
//
// ======================================================================================

#pragma once

#include "external.h"

namespace MetaData
{

// --------------------------------------------------------------------------------------
//
// This class represents a read-only MetaData table (a continuous chunk of data).
//
class TableRO
{
    friend class TableRW;

private:
    //
    // Private data
    //

    BYTE *m_pData;

public:
    //
    // Initialization
    //

    __checkReturn
    inline HRESULT Initialize(
        __range(2, UINT32_MAX) UINT32   cbRecordSize,
                               DataBlob sourceData,
                               BOOL     fCopyData)
    {
        _ASSERTE(!fCopyData);
        _ASSERTE((cbRecordSize == 0) || (sourceData.GetSize() % cbRecordSize == 0));
        m_pData = sourceData.GetDataPointer();
        return S_OK;
    }

    // Destroys the table and all its allocated data. Can run on uninitialized table.
    inline void Delete()
    {
        m_pData = NULL;
    }

public:
    //
    // Getters
    //

    __checkReturn
    inline HRESULT GetRecord(
                        UINT32 nRowIndex,
        _Outptr_opt_ BYTE **ppRecord,
                        UINT32 cbRecordSize,
                        UINT32 cRecordCount,
                        UINT32 nTableIndex)
    {
        if ((nRowIndex == 0) || (nRowIndex > cRecordCount))
        {
            Debug_ReportError("Invalid record index.");
            *ppRecord = NULL;
            return CLDB_E_INDEX_NOTFOUND;
        }
        *ppRecord = m_pData + (nRowIndex - 1) * cbRecordSize;
        return S_OK;
    } // TableRO::GetRecord

};  // class TableRO

// --------------------------------------------------------------------------------------
//
// This class represents a read-write MetaData table.
//
class TableRW
{
private:
    //
    // Private data
    //

    // The storage of table records.
    RecordPool m_RecordStorage;

public:
    //
    // Initialization
    //

    // Initializes (empty) table of record size (cbRecordSize) with new allocated data for cRecordCount
    // records.
    __checkReturn
    inline HRESULT InitializeEmpty_WithRecordCount(
        __range(2, UINT32_MAX) UINT32 cbRecordSize,
        __range(0, UINT32_MAX) UINT32 cRecordCount
        COMMA_INDEBUG_MD(      BOOL   debug_fIsReadWrite))
    {
        return m_RecordStorage.InitNew(cbRecordSize, cRecordCount);
    }

    __checkReturn
    inline HRESULT Initialize(
        __range(2, UINT32_MAX) UINT32   cbRecordSize,
                               DataBlob sourceData,
                               BOOL     fCopyData)
    {
        return m_RecordStorage.InitOnMem(cbRecordSize, sourceData.GetDataPointer(), sourceData.GetSize(), !fCopyData);
    }

    __checkReturn
    inline HRESULT InitializeFromTable(
        const TableRO *pSourceTable,
        UINT32         cbRecordSize,
        UINT32         cRecordCount,
        BOOL           fCopyData)
    {
        return m_RecordStorage.InitOnMem(cbRecordSize, pSourceTable->m_pData, cbRecordSize * cRecordCount, !fCopyData);
    }
    __checkReturn
    inline HRESULT InitializeFromTable(
        const TableRW *pSourceTable,
        BOOL           fCopyData)
    {
        _ASSERTE(fCopyData);
        return m_RecordStorage.ReplaceContents(const_cast<RecordPool *>(&pSourceTable->m_RecordStorage));
    }

    // Destroys the table and all its allocated data. Can run on uninitialized table.
    inline void Delete()
    {
        return m_RecordStorage.Uninit();
    }

public:
    //
    // Getters
    //

    inline UINT32 GetRecordCount() const
    {
        return const_cast<RecordPool &>(m_RecordStorage).Count();
    }
    inline HRESULT GetRecordsDataSize(UINT32 *pcbSize) const
    {
        return m_RecordStorage.GetSaveSize(pcbSize);
    }

    __checkReturn
    inline HRESULT GetRecord(
                        UINT32 nIndex,
        _Outptr_opt_ BYTE **ppRecord)
    {
        return m_RecordStorage.GetRecord(nIndex, ppRecord);
    }

    __checkReturn
    inline HRESULT SaveToStream(
        IStream *pStream) const
    {
        return const_cast<RecordPool &>(m_RecordStorage).PersistToStream(pStream);
    }

public:
    //
    // Setters
    //

    __checkReturn
    inline HRESULT AddRecord(
        _Out_                        BYTE  **ppbRecord,
        _Out_                        UINT32 *pnIndex)
    {
        return m_RecordStorage.AddRecord(ppbRecord, pnIndex);
    }
    __checkReturn
    inline HRESULT InsertRecord(
        UINT32 nIndex,
        BYTE **ppbRecord)
    {
        return m_RecordStorage.InsertRecord(nIndex, ppbRecord);
    }

    // Makes table data writable, i.e. copies them into newly allocated chunk of memory. The caller
    // guarantees that the table is not writable yet (otherwise this method asserts).
    //
    // Returns S_OK (even if the table is empty).
    // Returns METADATA_E_INTERNAL_ERROR error code if the table has more segments.
    __checkReturn
    inline HRESULT MakeWritable()
    {
        return m_RecordStorage.ConvertToRW();
    }

#ifdef _DEBUG_METADATA
    // Sets table information for debugging.
    void Debug_SetTableInfo(const char *szTableName, UINT32 nTableIndex)
    {
    }
#endif //_DEBUG_METADATA

};  // class TableRW

};  // namespace MetaData
