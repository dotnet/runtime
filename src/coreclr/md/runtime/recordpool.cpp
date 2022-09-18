// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RecordPool.cpp -- Implementation of record heaps.
//

//
//*****************************************************************************
#include "stdafx.h"
#include <recordpool.h>

#define RECORDPOOL_GROW_FACTOR 8
#define RECORDPOOL_GROW_MAX 2048
#define RECORDPOOL_GROW_MINROWS 2
#define RECORDPOOL_GROW_DEFAULTROWS 16

HRESULT
RecordPool::InitNew(
    UINT32 cbRec,       // Record size.
    UINT32 cRecsInit)   // Initial guess of count of record.
{
    HRESULT  hr;
    S_UINT32 cbGrow;    // Initial grow size of the pool.

    // Size of each record is fixed.
    m_cbRec = cbRec;

    if (cRecsInit > 0)
    {
        cbGrow = S_UINT32(cbRec) * S_UINT32(cRecsInit);
    }
    else
    {
        cbGrow = S_UINT32(cbRec) * S_UINT32(RECORDPOOL_GROW_DEFAULTROWS);
    }
    if (cbGrow.IsOverflow())
    {
        Debug_ReportInternalError("Growing record pool overflowed.");
        return CLDB_E_INTERNALERROR;
    }

    m_ulGrowInc = cbGrow.Value();

    IfFailRet(StgPool::InitNew());

    // If there is an initial size for the record pool, grow to that now.
    if (cRecsInit > 0)
    {
        if (!Grow(cbGrow.Value()))
            return E_OUTOFMEMORY;
    }

    return S_OK;
} // RecordPool::InitNew

//*****************************************************************************
// Load a Record heap from persisted memory.  If a copy of the data is made
// (so that it may be updated), then a new hash table is generated which can
// be used to eliminate duplicates with new Records.
//*****************************************************************************
HRESULT
RecordPool::InitOnMem(
    ULONG cbRec,        // Record size.
    void *pData,        // Predefined data.
    ULONG iSize,        // Size of data.
    BOOL  fReadOnly)    // true if append is forbidden.
{
    HRESULT hr;
    m_cbRec = cbRec;

    // Let base class init our memory structure.
    IfFailRet(StgPool::InitOnMem(pData, iSize, fReadOnly));

    // For init on existing mem case.
    if ((pData != NULL) && (iSize != 0))
    {
        // If we are doing an update in place don't make a copy
        // If we cannot update, then we don't need a hash table.
        if (fReadOnly)
            return S_OK;

        // Other wise copy the memory to do the update
        IfFailRet(TakeOwnershipOfInitMem());
    }

    return S_OK;
} // RecordPool::InitOnMem

//*****************************************************************************
// Allocate memory if we don't have any, or grow what we have.  If successful,
// then at least iRequired bytes will be allocated.
//*****************************************************************************
bool RecordPool::Grow(                 // true if successful.
    ULONG       iRequired)              // Min required bytes to allocate.
{
    // Allocate the memory.
    if (!StgPool::Grow(iRequired))
        return false;

    // Zero the new memory.
    memset(GetNextLocation(), 0, GetCbSegAvailable());

    return true;
} // bool RecordProol::Grow()

//*****************************************************************************
// The Record will be added to the pool.  The index of the Record in the pool
// is returned in *piIndex.  If the Record is already in the pool, then the
// index will be to the existing copy of the Record.
//*****************************************************************************
HRESULT
RecordPool::AddRecord(
    BYTE  **ppRecord,
    UINT32 *pnIndex)        // Return 1-based index of Record here.
{
    _ASSERTE(pnIndex != NULL);

    // Space on heap for new Record?
    if (m_cbRec > GetCbSegAvailable())
    {
        if (!Grow(m_cbRec))
        {
            *ppRecord = NULL;
            return E_OUTOFMEMORY;
        }
    }

    // Records should be aligned on record boundaries.
    _ASSERTE((GetNextOffset() % m_cbRec) == 0);

    // Copy the Record to the heap.
    *ppRecord = GetNextLocation();

    // Give the 1-based index back to caller.
    *pnIndex = (GetNextOffset() / m_cbRec) + 1;

    // Update heap counters.
    SegAllocate(m_cbRec);

    return S_OK;
} // RecordPool::AddRecord

//*****************************************************************************
// Insert a Record into the pool.  The index of the Record before which to
// insert is specified.  Shifts all records down.  Return a pointer to the
// new record.
//*****************************************************************************
HRESULT
RecordPool::InsertRecord(
    UINT32 nIndex,          // [IN] Insert record before this.
    BYTE **ppRecord)
{
    HRESULT      hr;
    StgPoolSeg *pCurSeg;                // Current segment.
    StgPoolSeg *pPrevSeg;                // Previous segment.
    BYTE        *pSegEnd;                // Last record in a segment.
    BYTE        *pFrom;                    // A copy/move source.
    ULONG        cbMove;                    // Bytes to move.
    BYTE        *pNew;                    // New record.

    // Notice the case of appending.
    if (nIndex == (Count() + 1))
    {
        UINT32 nNewIndex_Ignore;
        return AddRecord(ppRecord, &nNewIndex_Ignore);
    }

    // If past end or before beginning, invalid.
    if ((nIndex > Count()) || (nIndex == 0))
    {
        Debug_ReportError("Invalid index passed for inserting record.");
        return CLDB_E_INDEX_NOTFOUND;
    }

    // This code works by allocating a new record at the end.
    // The last record is moved to the new end record.
    // Working backwards through the chained segments,
    //     shift the segment by one record, so the empty record
    //      is at the start of the segment instead of the end.
    //        copy the last record of the previous segment to the
    //      newly emptied first record of the current segment.
    // When the segment containing the insert point is finally
    //  reached, its last record is empty (from above loop), so
    //  shift from the insertion point to the end-1 by one record.

    // Current last record.
    pCurSeg = m_pCurSeg;
    IfFailRet(GetRecord(Count(), &pSegEnd));
    _ASSERTE(hr == S_OK);

    // Add an empty record to the end of the heap.
    {
        UINT32 nLastRecordIndex_Ignore;
        IfFailRet(AddRecord(&pNew, &nLastRecordIndex_Ignore));
    }

    // Copy the current last record to the new record.
    memcpy(pNew, pSegEnd, m_cbRec);

    // While the insert location is prior to the current segment,
    while (nIndex < GetIndexForRecord(pCurSeg->m_pSegData))
    {
        // Shift the segment up by one record.
        cbMove = (ULONG)(pSegEnd - pCurSeg->m_pSegData);
        memmove(pCurSeg->m_pSegData + m_cbRec, pCurSeg->m_pSegData, cbMove);

        // Find the previous segment.
        pPrevSeg = this;
        while (pPrevSeg->m_pNextSeg != pCurSeg)
        {
            pPrevSeg = pPrevSeg->m_pNextSeg;
        }

        // Copy the final record of the previous segment to the start of this one.
        pSegEnd = pPrevSeg->m_pSegData+pPrevSeg->m_cbSegNext-m_cbRec;
        memcpy(pCurSeg->m_pSegData, pSegEnd, m_cbRec);

        // Make the previous segment the current segment.
        pCurSeg = pPrevSeg;
    }

    // Shift at the insert location, forward by one.
    IfFailRet(GetRecord(nIndex, &pFrom));
    _ASSERTE(hr == S_OK);

    cbMove = (ULONG)(pSegEnd - pFrom);
    memmove(pFrom + m_cbRec, pFrom, cbMove);

    *ppRecord = pFrom;

    return S_OK;
} // RecordPool::InsertRecord

//*****************************************************************************
// Return a pointer to a Record given an index previously handed out by
// AddRecord or FindRecord.
//*****************************************************************************
HRESULT
RecordPool::GetRecord(
    UINT32 nIndex,          // 1-based index of Record in pool.
    BYTE **ppRecord)
{
    MetaData::DataBlob record;

    if (nIndex == 0)
    {
        Debug_ReportError("Invalid index 0 passed.");
        *ppRecord = NULL;
        return CLDB_E_INDEX_NOTFOUND;
    }

    // Convert to 0-based internal form, defer to implementation.
    HRESULT hr = GetData((nIndex - 1) * m_cbRec, &record);
    if (FAILED(hr))
    {
        *ppRecord = NULL;
        return hr;
    }
    _ASSERTE(record.ContainsData(m_cbRec));
    *ppRecord = record.GetDataPointer();

    return hr;
} // RecordPool::GetRecord

//*****************************************************************************
// Return the first record in a pool, and set up a context for fast
//  iterating through the pool.  Note that this scheme does pretty minimal
//  error checking.
//*****************************************************************************
void *RecordPool::GetFirstRecord(        // Pointer to Record in pool.
    void        **pContext)                // Store context here.
{
    StgPoolSeg    **ppSeg = reinterpret_cast<StgPoolSeg**>(pContext);

    *ppSeg = static_cast<StgPoolSeg*>(this);
    return (*ppSeg)->m_pSegData;
} // void *RecordPool::GetFirstRecord()

//*****************************************************************************
// Given a pointer to a record, return a pointer to the next record.
//  Note that this scheme does pretty minimal error checking. In particular,
//  this will let the caller walk off of the end of valid data in the last
//  segment.
//*****************************************************************************
void *RecordPool::GetNextRecord(        // Pointer to Record in pool.
    void        *pRecord,                // Current record.
    void        **pContext)                // Stored context here.
{
    BYTE        *pbRec = reinterpret_cast<BYTE*>(pRecord);
    StgPoolSeg    **ppSeg = reinterpret_cast<StgPoolSeg**>(pContext);

    // Get the next record.
    pbRec += m_cbRec;

    // Is the next record outside of the current segment?
    if (static_cast<ULONG>(pbRec - (*ppSeg)->m_pSegData) >= (*ppSeg)->m_cbSegSize)
    {
        // Better be exactly one past current segment.
        _ASSERTE(static_cast<ULONG>(pbRec - (*ppSeg)->m_pSegData) == (*ppSeg)->m_cbSegSize);
        // Switch the context pointer.
        *ppSeg = (*ppSeg)->m_pNextSeg;
        // Next record is start of next segment.
        if (*ppSeg)
            return (*ppSeg)->m_pSegData;
        else
            return 0;
    }

    return pbRec;
} // void *RecordPool::GetNextRecord()

//*****************************************************************************
// Given a pointer to a record, determine the index corresponding to the
// record.
//*****************************************************************************
ULONG RecordPool::GetIndexForRecord(    // 1-based index of Record in pool.
    const void *pvRecord)                // Pointer to Record in pool.
{
    ULONG        iPrev = 0;                // cumulative index of previous segments.
    const StgPoolSeg *pSeg = this;
    const BYTE  *pRecord = reinterpret_cast<const BYTE*>(pvRecord);
    const BYTE  *pSegData = NULL;
    ULONG       ulSegSize;
    for (;;)
    {    // Does the current segment contain the record?
        pSegData = pSeg->GetSegData();
        ulSegSize = pSeg->GetSegSize();
        if (pRecord >= pSegData && pRecord < pSegData + ulSegSize)
        {    // The pointer should be to the start of a record.
            _ASSERTE(((pRecord - pSegData) % m_cbRec) == 0);
            return (ULONG)(1 + iPrev + (pRecord - pSegData) / m_cbRec);
        }
        _ASSERTE((ulSegSize % m_cbRec) == 0);
        iPrev += ulSegSize / m_cbRec;
        pSeg = pSeg->GetNextSeg();
        // If out of data, didn't find the record.
        if (pSeg == 0)
            return 0;
    }
} // ULONG RecordPool::GetIndexForRecord()

//*****************************************************************************
// Given a purported pointer to a record, determine if the pointer is valid.
//*****************************************************************************
int RecordPool::IsValidPointerForRecord(// true or false.
    const void *pvRecord)                // Pointer to Record in pool.
{
    const StgPoolSeg *pSeg;
    const BYTE  *pRecord = reinterpret_cast<const BYTE*>(pvRecord);
    const BYTE  *pSegData = NULL;
    for (pSeg = this; (pSeg); pSeg = pSeg->GetNextSeg())
    {    // Does the current segment contain the record?
        pSegData = pSeg->GetSegData();
        if ((pRecord >= pSegData) && (pRecord < pSegData + pSeg->GetSegSize()))
        {    // The pointer should be to the start of a record.
            return (((pRecord - pSegData) % m_cbRec) == 0);
        }
        _ASSERTE((pSeg->GetSegSize() % m_cbRec) == 0);
    }
    return 0;
} // int RecordPool::IsValidPointerForRecord()

//*****************************************************************************
// Replace the contents of this pool with those from another pool.  The other
//    pool loses ownership of the memory.
//*****************************************************************************
HRESULT RecordPool::ReplaceContents(
    RecordPool *pOther)                    // The other record pool.
{
    // Release any memory currently held.
    Uninit();

    // Grab the new data.
    *this = *pOther;

    // If the other pool's curseg pointed to itself, make this pool point to itself.
    if (pOther->m_pCurSeg == pOther)
        m_pCurSeg = this;

    // Fix the other pool so it won't free the memory that this one
    //  just hijacked.
    pOther->m_pSegData = (BYTE*)m_zeros;
    pOther->m_pNextSeg = 0;
    pOther->Uninit();

    return S_OK;
} // HRESULT RecordPool::ReplaceContents()
