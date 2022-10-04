// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RecordPool.h -- header file for record heaps.
//

//
//*****************************************************************************
#ifndef _RECORDPOOL_H_
#define _RECORDPOOL_H_

#if _MSC_VER >= 1100
#pragma once
#endif

#include <stgpool.h>

//*****************************************************************************
// This Record pool class collects user Records into a big consecutive heap.
// The list of Records is kept in memory while adding, and
// finally flushed to a stream at the caller's request.
//*****************************************************************************
class RecordPool : public StgPool
{
    friend class VerifyLayoutsMD;

    using StgPool::InitNew;
    using StgPool::InitOnMem;

public:
    RecordPool() :
        StgPool(1024, 1)
    { }

//*****************************************************************************
// Init the pool for use.  This is called for the create empty case.
//*****************************************************************************
    __checkReturn
    HRESULT InitNew(
        UINT32 cbRec,                    // Record size.
        UINT32 cRecsInit);                // Initial guess of count of record.

//*****************************************************************************
// Load a Record heap from persisted memory.  If a copy of the data is made
// (so that it may be updated), then a new hash table is generated which can
// be used to eliminate duplicates with new Records.
//*****************************************************************************
    __checkReturn
    HRESULT InitOnMem(
        ULONG cbRec,            // Record size.
        void *pData,            // Predefined data.
        ULONG iSize,            // Size of data.
        BOOL  fReadOnly);       // true if append is forbidden.

//*****************************************************************************
// Allocate memory if we don't have any, or grow what we have.  If successful,
// then at least iRequired bytes will be allocated.
//*****************************************************************************
    bool Grow(                                // true if successful.
        ULONG        iRequired);                // Min required bytes to allocate.

//*****************************************************************************
// The Record will be added to the pool.  The index of the Record in the pool
// is returned in *piIndex.  If the Record is already in the pool, then the
// index will be to the existing copy of the Record.
//*****************************************************************************
    HRESULT AddRecord(
        BYTE  **ppRecord,
        UINT32 *pnIndex);       // Return 1-based index of Record here.

//*****************************************************************************
// Insert a Record into the pool.  The index of the Record before which to
// insert is specified.  Shifts all records down.  Return a pointer to the
// new record.
//*****************************************************************************
    HRESULT InsertRecord(
        UINT32 nIndex,          // [IN] Insert record before this.
        BYTE **ppRecord);

//*****************************************************************************
// Return a pointer to a Record given an index previously handed out by
// AddRecord or FindRecord.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetRecord(
        UINT32 nIndex,            // 1-based index of Record in pool.
        BYTE **ppRecord);

//*****************************************************************************
// Given a pointer to a record, determine the index corresponding to the
// record.
//*****************************************************************************
    virtual ULONG GetIndexForRecord(        // 1-based index of Record in pool.
        const void *pRecord);                // Pointer to Record in pool.

//*****************************************************************************
// Given a purported pointer to a record, determine if the pointer is valid.
//*****************************************************************************
    virtual int IsValidPointerForRecord(    // true or false.
        const void *pRecord);                // Pointer to Record in pool.

//*****************************************************************************
// How many objects are there in the pool?  If the count is 0, you don't need
// to persist anything at all to disk.
//*****************************************************************************
    UINT32 Count()
    { return GetNextOffset() / m_cbRec; }

//*****************************************************************************
// Indicate if heap is empty.  This has to be based on the size of the data
// we are keeping.  If you open in r/o mode on memory, there is no hash
// table.
//*****************************************************************************
    virtual int IsEmpty()                    // true if empty.
    { return (GetNextOffset() == 0); }

//*****************************************************************************
// Is the index valid for the Record?
//*****************************************************************************
    virtual int IsValidCookie(ULONG ulCookie)
    { return (ulCookie == 0 || IsValidOffset((ulCookie-1) * m_cbRec)); }

//*****************************************************************************
// Return the size of the heap.
//*****************************************************************************
    ULONG GetNextIndex()
    { return (GetNextOffset() / m_cbRec); }

//*****************************************************************************
// Replace the contents of this pool with those from another pool.  The other
//    pool loses ownership of the memory.
//*****************************************************************************
    __checkReturn
    HRESULT ReplaceContents(
        RecordPool *pOther);                // The other record pool.

//*****************************************************************************
// Return the first record in a pool, and set up a context for fast
//  iterating through the pool.  Note that this scheme does pretty minimal
//  error checking.
//*****************************************************************************
    void *GetFirstRecord(                    // Pointer to Record in pool.
        void        **pContext);            // Store context here.

//*****************************************************************************
// Given a pointer to a record, return a pointer to the next record.
//  Note that this scheme does pretty minimal error checking. In particular,
//  this will let the caller walk off of the end of valid data in the last
//  segment.
//*****************************************************************************
    void *GetNextRecord(                    // Pointer to Record in pool.
        void        *pRecord,                // Current record.
        void        **pContext);            // Stored context here.

private:
    DAC_ALIGNAS(StgPool) // Align first member to alignment of base class
    UINT32 m_cbRec;                // How large is each record?

};  // class RecordPool

#endif // _RECORDPOOL_H_
