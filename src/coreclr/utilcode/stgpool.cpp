// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgPool.cpp
//

//
// Pools are used to reduce the amount of data actually required in the database.
// This allows for duplicate string and binary values to be folded into one
// copy shared by the rest of the database.  Strings are tracked in a hash
// table when insert/changing data to find duplicates quickly.  The strings
// are then persisted consecutively in a stream in the database format.
//
//*****************************************************************************
#include "stdafx.h"                     // Standard include.
#include <stgpool.h>                    // Our interface definitions.
#include <posterror.h>                  // Error handling.
#include <safemath.h>                   // CLRSafeInt integer overflow checking
#include "../md/inc/streamutil.h"
#include "../md/errors_metadata.h"

#include "ex.h"

using namespace StreamUtil;

#define MAX_CHAIN_LENGTH 20             // Max chain length before rehashing.

//
//
// StgPool
//
//


//*****************************************************************************
// Free any memory we allocated.
//*****************************************************************************
StgPool::~StgPool()
{
    WRAPPER_NO_CONTRACT;

    Uninit();
} // StgPool::~StgPool()


//*****************************************************************************
// Init the pool for use.  This is called for both the create empty case.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::InitNew(
    ULONG cbSize,       // Estimated size.
    ULONG cItems)       // Estimated item count.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END

    // Make sure we aren't stomping anything and are properly initialized.
    _ASSERTE(m_pSegData == m_zeros);
    _ASSERTE(m_pNextSeg == 0);
    _ASSERTE(m_pCurSeg == this);
    _ASSERTE(m_cbCurSegOffset == 0);
    _ASSERTE(m_cbSegSize == 0);
    _ASSERTE(m_cbSegNext == 0);

    m_bReadOnly = false;
    m_bFree = false;

    return S_OK;
} // StgPool::InitNew

//*****************************************************************************
// Init the pool from existing data.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::InitOnMem(
    void *pData,        // Predefined data.
    ULONG iSize,        // Size of data.
    int   bReadOnly)    // true if append is forbidden.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    // Make sure we aren't stomping anything and are properly initialized.
    _ASSERTE(m_pSegData == m_zeros);
    _ASSERTE(m_pNextSeg == 0);
    _ASSERTE(m_pCurSeg == this);
    _ASSERTE(m_cbCurSegOffset == 0);

    // Create case requires no further action.
    if (!pData)
        return (E_INVALIDARG);

    // Might we be extending this heap?
    m_bReadOnly = bReadOnly;


    m_pSegData = reinterpret_cast<BYTE*>(pData);
    m_cbSegSize = iSize;
    m_cbSegNext = iSize;

    m_bFree = false;

    return (S_OK);
} // StgPool::InitOnMem

//*****************************************************************************
// Called when the pool must stop accessing memory passed to InitOnMem().
//*****************************************************************************
__checkReturn
HRESULT
StgPool::TakeOwnershipOfInitMem()
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    // If the pool doesn't have a pointer to non-owned memory, done.
    if (m_bFree)
        return (S_OK);

    // If the pool doesn't have a pointer to memory at all, done.
    if (m_pSegData == m_zeros)
    {
        _ASSERTE(m_cbSegSize == 0);
        return (S_OK);
    }

    // Get some memory to keep.
    BYTE *pData = new (nothrow) BYTE[m_cbSegSize+4];
    if (pData == 0)
        return (PostError(OutOfMemory()));

    // Copy the old data to the new memory.
    memcpy(pData, m_pSegData, m_cbSegSize);
    m_pSegData = pData;
    m_bFree = true;

    return (S_OK);
} // StgPool::TakeOwnershipOfInitMem

//*****************************************************************************
// Clear out this pool.  Cannot use until you call InitNew.
//*****************************************************************************
void StgPool::Uninit()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // Free base segment, if appropriate.
    if (m_bFree && (m_pSegData != m_zeros))
    {
        delete [] m_pSegData;
        m_bFree = false;
    }

    // Free chain, if any.
    StgPoolSeg  *pSeg = m_pNextSeg;
    while (pSeg)
    {
        StgPoolSeg *pNext = pSeg->m_pNextSeg;
        delete [] (BYTE*)pSeg;
        pSeg = pNext;
    }

    // Clear vars.
    m_pSegData = (BYTE*)m_zeros;
    m_cbSegSize = m_cbSegNext = 0;
    m_pNextSeg = 0;
    m_pCurSeg = this;
    m_cbCurSegOffset = 0;
} // StgPool::Uninit

//*****************************************************************************
// Called to copy the pool to writable memory, reset the r/o bit.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::ConvertToRW()
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT     hr;                     // A result.
    IfFailRet(TakeOwnershipOfInitMem());

    IfFailRet(SetHash(true));

    m_bReadOnly = false;

    return S_OK;
} // StgPool::ConvertToRW

//*****************************************************************************
// Turn hashing off or on.  Real implementation as required in subclass.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::SetHash(int bHash)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    return S_OK;
} // StgPool::SetHash

//*****************************************************************************
// Trim any empty final segment.
//*****************************************************************************
void StgPool::Trim()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // If no chained segments, nothing to do.
    if (m_pNextSeg == 0)
        return;

    // Handle special case for a segment that was completely unused.
    if (m_pCurSeg->m_cbSegNext == 0)
    {
        // Find the segment which points to the empty segment.
        StgPoolSeg *pPrev;
        for (pPrev = this; pPrev && pPrev->m_pNextSeg != m_pCurSeg; pPrev = pPrev->m_pNextSeg);
        _ASSERTE(pPrev && pPrev->m_pNextSeg == m_pCurSeg);

        // Free the empty segment.
        delete [] (BYTE*) m_pCurSeg;

        // Fix the pCurSeg pointer.
        pPrev->m_pNextSeg = 0;
        m_pCurSeg = pPrev;

        // Adjust the base offset, because the PREVIOUS seg is now current.
        _ASSERTE(m_pCurSeg->m_cbSegNext <= m_cbCurSegOffset);
        m_cbCurSegOffset = m_cbCurSegOffset - m_pCurSeg->m_cbSegNext;
    }
} // StgPool::Trim

//*****************************************************************************
// Allocate memory if we don't have any, or grow what we have.  If successful,
// then at least iRequired bytes will be allocated.
//*****************************************************************************
bool StgPool::Grow(         // true if successful.
    ULONG iRequired)        // Min required bytes to allocate.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return FALSE;);
    }
    CONTRACTL_END

    ULONG       iNewSize;               // New size we want.
    StgPoolSeg  *pNew;                  // Temp pointer for malloc.

    _ASSERTE(!m_bReadOnly);

    // Would this put the pool over 2GB?
    if ((m_cbCurSegOffset + iRequired) > INT_MAX)
        return (false);

    // Adjust grow size as a ratio to avoid too many reallocs.
    if ((m_pCurSeg->m_cbSegNext + m_cbCurSegOffset) / m_ulGrowInc >= 3)
        m_ulGrowInc *= 2;

    // NOTE: MD\DataSource\RemoteMDInternalRWSource has taken a dependency that there
    // won't be more than 1000 segments. Given the current exponential growth algorithm
    // we'll never get anywhere close to that, but if the algorithm changes to allow for
    // many segments, please update that source as well.

    // If first time, handle specially.
    if (m_pSegData == m_zeros)
    {
        // Allocate the buffer.
        iNewSize = max(m_ulGrowInc, iRequired);
        BYTE *pSegData = new (nothrow) BYTE[iNewSize + 4];
        if (pSegData == NULL)
            return false;
        m_pSegData = pSegData;

        // Will need to delete it.
        m_bFree = true;

        // How big is this initial segment?
        m_cbSegSize = iNewSize;

        // Do some validation of var fields.
        _ASSERTE(m_cbSegNext == 0);
        _ASSERTE(m_pCurSeg == this);
        _ASSERTE(m_pNextSeg == NULL);

        return true;
    }

    // Allocate the new space enough for header + data.
    iNewSize = (ULONG)(max(m_ulGrowInc, iRequired) + sizeof(StgPoolSeg));
    pNew = (StgPoolSeg *)new (nothrow) BYTE[iNewSize+4];
    if (pNew == NULL)
        return false;

    // Set the fields in the new segment.
    pNew->m_pSegData = reinterpret_cast<BYTE*>(pNew) + sizeof(StgPoolSeg);
    _ASSERTE(ALIGN4BYTE(reinterpret_cast<ULONG_PTR>(pNew->m_pSegData)) == reinterpret_cast<ULONG_PTR>(pNew->m_pSegData));
    pNew->m_pNextSeg = 0;
    pNew->m_cbSegSize = iNewSize - sizeof(StgPoolSeg);
    pNew->m_cbSegNext = 0;

    // Calculate the base offset of the new segment.
    m_cbCurSegOffset = m_cbCurSegOffset + m_pCurSeg->m_cbSegNext;

    // Handle special case for a segment that was completely unused.
    //<TODO>@todo: Trim();</TODO>
    if (m_pCurSeg->m_cbSegNext == 0)
    {
        // Find the segment which points to the empty segment.
        StgPoolSeg *pPrev;
        for (pPrev = this; pPrev && pPrev->m_pNextSeg != m_pCurSeg; pPrev = pPrev->m_pNextSeg);
        _ASSERTE(pPrev && pPrev->m_pNextSeg == m_pCurSeg);

        // Free the empty segment.
        delete [] (BYTE *) m_pCurSeg;

        // Link in the new segment.
        pPrev->m_pNextSeg = pNew;
        m_pCurSeg = pNew;

        return true;
    }

    // Fix the size of the old segment.
    m_pCurSeg->m_cbSegSize = m_pCurSeg->m_cbSegNext;

    // Link the new segment into the chain.
    m_pCurSeg->m_pNextSeg = pNew;
    m_pCurSeg = pNew;

    return true;
} // StgPool::Grow

//*****************************************************************************
// Add a segment to the chain of segments.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::AddSegment(
    const void *pData,      // The data.
    ULONG       cbData,     // Size of the data.
    bool        bCopy)      // If true, make a copy of the data.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    StgPoolSeg *pNew;   // Temp pointer for malloc.


    // If we need to copy the data, just grow the heap by enough to take the
    //  the new data, and copy it in.
    if (bCopy)
    {
        void *pDataToAdd = new (nothrow) BYTE[cbData];
        IfNullRet(pDataToAdd);
        memcpy(pDataToAdd, pData, cbData);
        pData = pDataToAdd;
    }

    // If first time, handle specially.
    if (m_pSegData == m_zeros)
    {   // Data was passed in.
        m_pSegData = reinterpret_cast<BYTE*>(const_cast<void*>(pData));
        m_cbSegSize = cbData;
        m_cbSegNext = cbData;
        _ASSERTE(m_pNextSeg == NULL);

        // Will not delete it.
        m_bFree = false;

        return S_OK;
    }

    // Not first time.  Handle a completely empty tail segment.
    Trim();

    // Abandon any space past the end of the current live data.
    _ASSERTE(m_pCurSeg->m_cbSegSize >= m_pCurSeg->m_cbSegNext);
    m_pCurSeg->m_cbSegSize = m_pCurSeg->m_cbSegNext;

    // Allocate a new segment header.
    pNew = (StgPoolSeg *) new (nothrow) BYTE[sizeof(StgPoolSeg)];
    IfNullRet(pNew);

    // Set the fields in the new segment.
    pNew->m_pSegData = reinterpret_cast<BYTE*>(const_cast<void*>(pData));
    pNew->m_pNextSeg = NULL;
    pNew->m_cbSegSize = cbData;
    pNew->m_cbSegNext = cbData;

    // Calculate the base offset of the new segment.
    m_cbCurSegOffset = m_cbCurSegOffset + m_pCurSeg->m_cbSegNext;

    // Link the segment into the chain.
    _ASSERTE(m_pCurSeg->m_pNextSeg == NULL);
    m_pCurSeg->m_pNextSeg = pNew;
    m_pCurSeg = pNew;

    return S_OK;
} // StgPool::AddSegment

#ifndef DACCESS_COMPILE
//*****************************************************************************
// The entire string pool is written to the given stream. The stream is aligned
// to a 4 byte boundary.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::PersistToStream(
    IStream *pIStream)      // The stream to write to.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END

    HRESULT     hr = S_OK;
    ULONG       cbTotal;                // Total bytes written.
    StgPoolSeg  *pSeg;                  // A segment being written.

    _ASSERTE(m_pSegData != m_zeros);

    // Start with the base segment.
    pSeg = this;
    cbTotal = 0;

    EX_TRY
    {
        // As long as there is data, write it.
        while (pSeg != NULL)
        {
            // If there is data in the segment . . .
            if (pSeg->m_cbSegNext)
            {   // . . . write and count the data.
                if (FAILED(hr = pIStream->Write(pSeg->m_pSegData, pSeg->m_cbSegNext, 0)))
                    break;
                cbTotal += pSeg->m_cbSegNext;
            }

            // Get the next segment.
            pSeg = pSeg->m_pNextSeg;
        }

        if (SUCCEEDED(hr))
        {
            // Align to variable (0-4 byte) boundary.
            UINT32 cbTotalAligned;
            if (FAILED(Align(cbTotal, &cbTotalAligned)))
            {
                hr = COR_E_BADIMAGEFORMAT;
            }
            else
            {
                if (cbTotalAligned > cbTotal)
                {
                    _ASSERTE(sizeof(hr) >= 3);
                    hr = 0;
                    hr = pIStream->Write(&hr, cbTotalAligned - cbTotal, 0);
                }
            }
        }
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
} // StgPool::PersistToStream
#endif //!DACCESS_COMPILE

//*****************************************************************************
// The entire string pool is written to the given stream. The stream is aligned
// to a 4 byte boundary.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::PersistPartialToStream(
    IStream *pIStream,      // The stream to write to.
    ULONG    iOffset)       // Starting offset.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END

    HRESULT     hr = S_OK;              // A result.
    ULONG       cbTotal;                // Total bytes written.
    StgPoolSeg  *pSeg;                  // A segment being written.

    _ASSERTE(m_pSegData != m_zeros);

    // Start with the base segment.
    pSeg = this;
    cbTotal = 0;

    // As long as there is data, write it.
    while (pSeg != NULL)
    {
        // If there is data in the segment . . .
        if (pSeg->m_cbSegNext)
        {   // If this data should be skipped...
            if (iOffset >= pSeg->m_cbSegNext)
            {   // Skip it
                iOffset -= pSeg->m_cbSegNext;
            }
            else
            {   // At least some data should be written, so write and count the data.
                IfFailRet(pIStream->Write(pSeg->m_pSegData+iOffset, pSeg->m_cbSegNext-iOffset, 0));
                cbTotal += pSeg->m_cbSegNext-iOffset;
                iOffset = 0;
            }
        }

        // Get the next segment.
        pSeg = pSeg->m_pNextSeg;
    }

    // Align to variable (0-4 byte) boundary.
    UINT32 cbTotalAligned;
    if (FAILED(Align(cbTotal, &cbTotalAligned)))
    {
        return COR_E_BADIMAGEFORMAT;
    }
    if (cbTotalAligned > cbTotal)
    {
        _ASSERTE(sizeof(hr) >= 3);
        hr = 0;
        hr = pIStream->Write(&hr, cbTotalAligned - cbTotal, 0);
    }

    return hr;
} // StgPool::PersistPartialToStream

// Copies data from pSourcePool starting at index nStartSourceIndex.
__checkReturn
HRESULT
StgPool::CopyPool(
    UINT32         nStartSourceIndex,
    const StgPool *pSourcePool)
{
    HRESULT hr;
    UINT32 cbDataSize;
    BYTE  *pbData = NULL;

    if (nStartSourceIndex == pSourcePool->GetRawSize())
    {   // There's nothing to copy
        return S_OK;
    }
    if (nStartSourceIndex > pSourcePool->GetRawSize())
    {   // Invalid input
        Debug_ReportInternalError("The caller should not pass invalid start index in the pool.");
        IfFailGo(METADATA_E_INDEX_NOTFOUND);
    }

    // Allocate new segment
    cbDataSize = pSourcePool->GetRawSize() - nStartSourceIndex;
    pbData = new (nothrow) BYTE[cbDataSize];
    IfNullGo(pbData);

    // Copy data to the new segment
    UINT32 cbCopiedDataSize;
    IfFailGo(pSourcePool->CopyData(
        nStartSourceIndex,
        pbData,
        cbDataSize,
        &cbCopiedDataSize));
    // Check that we copied everything
    if (cbDataSize != cbCopiedDataSize)
    {
        Debug_ReportInternalError("It is expected to copy everything from the source pool.");
        IfFailGo(E_FAIL);
    }

    // Add the newly allocated segment to the pool
    IfFailGo(AddSegment(
        pbData,
        cbDataSize,
        false));        // fCopyData

ErrExit:
    if (FAILED(hr))
    {
        if (pbData != NULL)
        {
            delete [] pbData;
        }
    }
    return hr;
} // StgPool::CopyPool

// Copies data from the pool into a buffer. It will correctly walk all segments for the copy.
__checkReturn
HRESULT
StgPool::CopyData(
    UINT32  nOffset,
    BYTE   *pBuffer,
    UINT32  cbBuffer,
    UINT32 *pcbWritten) const
{
    CONTRACTL
    {
        NOTHROW;
        PRECONDITION(CheckPointer(pBuffer));
        PRECONDITION(CheckPointer(pcbWritten));
    }
    CONTRACTL_END

    HRESULT           hr = S_OK;
    const StgPoolSeg *pSeg;     // A segment being written.

    _ASSERTE(m_pSegData != m_zeros);

    // Start with the base segment.
    pSeg = this;
    *pcbWritten = 0;

    // As long as there is data, write it.
    while (pSeg != NULL)
    {
        // If there is data in the segment . . .
        if (pSeg->m_cbSegNext)
        {   // If this data should be skipped...
            if (nOffset >= pSeg->m_cbSegNext)
            {   // Skip it
                nOffset -= pSeg->m_cbSegNext;
            }
            else
            {
                ULONG nNumBytesToCopy = pSeg->m_cbSegNext - nOffset;
                if (nNumBytesToCopy > (cbBuffer - *pcbWritten))
                {
                    _ASSERTE(!"Buffer isn't big enough to copy everything!");
                    nNumBytesToCopy = cbBuffer - *pcbWritten;
                }

                memcpy(pBuffer + *pcbWritten, pSeg->m_pSegData+nOffset, nNumBytesToCopy);

                *pcbWritten += nNumBytesToCopy;
                nOffset = 0;
            }
        }

        // Get the next segment.
        pSeg = pSeg->m_pNextSeg;
    }

    return hr;
} // StgPool::CopyData

//*****************************************************************************
// Get a pointer to the data at some offset.  May require traversing the
//  chain of extensions.  It is the caller's responsibility not to attempt
//  to access data beyond the end of a segment.
// This is an internal accessor, and should only be called when the data
//  is not in the base segment.
//*****************************************************************************
__checkReturn
HRESULT
StgPool::GetData_i(
    UINT32              nOffset,
    MetaData::DataBlob *pData)
{
    LIMITED_METHOD_CONTRACT;

    // Shouldn't be called on base segment.
    _ASSERTE(nOffset >= m_cbSegNext);
    StgPoolSeg *pSeg = this;

    while ((nOffset > 0) && (nOffset >= pSeg->m_cbSegNext))
    {
        // On to next segment.
        nOffset -= pSeg->m_cbSegNext;
        pSeg = pSeg->m_pNextSeg;

        // Is there a next?
        if (pSeg == NULL)
        {
            Debug_ReportError("Invalid offset passed - reached end of pool.");
            pData->Clear();
            return CLDB_E_INDEX_NOTFOUND;
        }
    }

    // For the case where we want to read the first item and the pool is empty.
    if (nOffset == pSeg->m_cbSegNext)
    {   // Can only be if both == 0
        Debug_ReportError("Invalid offset passed - it is at the end of pool.");
        pData->Clear();
        return CLDB_E_INDEX_NOTFOUND;
    }

    pData->Init(pSeg->m_pSegData + nOffset, pSeg->m_cbSegNext - nOffset);

    return S_OK;
} // StgPool::GetData_i

//
//
// StgStringPool
//
//


//*****************************************************************************
// Create a new, empty string pool.
//*****************************************************************************
__checkReturn
HRESULT
StgStringPool::InitNew(
    ULONG cbSize,       // Estimated size.
    ULONG cItems)       // Estimated item count.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END

    HRESULT hr;
    UINT32  nEmptyStringOffset;

    // Let base class intialize.
    IfFailRet(StgPool::InitNew());

    // Set initial table sizes, if specified.
    if (cbSize > 0)
    {
        if (!Grow(cbSize))
        {
            return E_OUTOFMEMORY;
        }
    }
    if (cItems > 0)
    {
        m_Hash.SetBuckets(cItems);
    }

    // Init with empty string.
    IfFailRet(AddString("", &nEmptyStringOffset));
    // Empty string had better be at offset 0.
    _ASSERTE(nEmptyStringOffset == 0);

    return hr;
} // StgStringPool::InitNew

//*****************************************************************************
// Load a string heap from persisted memory.  If a copy of the data is made
// (so that it may be updated), then a new hash table is generated which can
// be used to elminate duplicates with new strings.
//*****************************************************************************
__checkReturn
HRESULT
StgStringPool::InitOnMem(
    void *pData,        // Predefined data.
    ULONG iSize,        // Size of data.
    int   bReadOnly)    // true if append is forbidden.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END

    HRESULT hr = S_OK;

    // There may be up to three extra '\0' characters appended for padding.  Trim them.
    char *pchData = reinterpret_cast<char*>(pData);
    while (iSize > 1 && pchData[iSize-1] == 0 && pchData[iSize-2] == 0)
        --iSize;

    // Let base class init our memory structure.
    IfFailRet(StgPool::InitOnMem(pData, iSize, bReadOnly));

    //<TODO>@todo: defer this until we hand out a pointer.</TODO>
    if (!bReadOnly)
    {
        IfFailRet(TakeOwnershipOfInitMem());
        IfFailRet(RehashStrings());
    }

    return hr;
} // StgStringPool::InitOnMem

//*****************************************************************************
// Clears the hash table then calls the base class.
//*****************************************************************************
void StgStringPool::Uninit()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // Clear the hash table.
    m_Hash.Clear();

    // Let base class clean up.
    StgPool::Uninit();
} // StgStringPool::Uninit

//*****************************************************************************
// Turn hashing off or on.  If you turn hashing on, then any existing data is
// thrown away and all data is rehashed during this call.
//*****************************************************************************
__checkReturn
HRESULT
StgStringPool::SetHash(int bHash)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT     hr = S_OK;

    // If turning on hash again, need to rehash all strings.
    if (bHash)
        hr = RehashStrings();

    m_bHash = bHash;
    return (hr);
} // StgStringPool::SetHash

//*****************************************************************************
// The string will be added to the pool.  The offset of the string in the pool
// is returned in *piOffset.  If the string is already in the pool, then the
// offset will be to the existing copy of the string.
//*****************************************************************************
__checkReturn
HRESULT
StgStringPool::AddString(
    LPCSTR  szString,       // The string to add to pool.
    UINT32 *pnOffset)       // Return offset of string here.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    STRINGHASH *pHash;                  // Hash item for add.
    ULONG       iLen;                   // To handle non-null strings.
    LPSTR       pData;                  // Pointer to location for new string.
    HRESULT     hr;

    _ASSERTE(!m_bReadOnly);

    // Null pointer is an error.
    if (szString == 0)
        return (PostError(E_INVALIDARG));

    // Find the real length we need in buffer.
    iLen = (ULONG)(strlen(szString) + 1);

    // Where to put the new string?
    if (iLen > GetCbSegAvailable())
    {
        if (!Grow(iLen))
            return (PostError(OutOfMemory()));
    }
    pData = reinterpret_cast<LPSTR>(GetNextLocation());

    // Copy the data into the buffer.
    strcpy_s(pData, iLen, szString);

    // If the hash table is to be kept built (default).
    if (m_bHash)
    {
        // Find or add the entry.
        pHash = m_Hash.Find(pData, true);
        if (!pHash)
            return (PostError(OutOfMemory()));

        // If the entry was new, keep the new string.
        if (pHash->iOffset == 0xffffffff)
        {
            *pnOffset = pHash->iOffset = GetNextOffset();
            SegAllocate(iLen);

            // Check for hash chains that are too long.
            if (m_Hash.MaxChainLength() > MAX_CHAIN_LENGTH)
            {
                IfFailRet(RehashStrings());
            }
        }
        // Else use the old one.
        else
        {
            *pnOffset = pHash->iOffset;
        }
    }
    // Probably an import which defers the hash table for speed.
    else
    {
        *pnOffset = GetNextOffset();
        SegAllocate(iLen);
    }
    return S_OK;
} // StgStringPool::AddString

//*****************************************************************************
// Add a string to the pool with Unicode to UTF8 conversion.
//*****************************************************************************
__checkReturn
HRESULT
StgStringPool::AddStringW(
    LPCWSTR szString,           // The string to add to pool.
    UINT32 *pnOffset)           // Return offset of string here.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    STRINGHASH  *pHash;                 // Hash item for add.
    ULONG       iLen;                   // Correct length after conversion.
    LPSTR       pData;                  // Pointer to location for new string.

    _ASSERTE(!m_bReadOnly);

    // Null pointer is an error.
    if (szString == 0)
        return (PostError(E_INVALIDARG));

    // Special case empty string.
    if (*szString == '\0')
    {
        *pnOffset = 0;
        return (S_OK);
    }

    // How many bytes will be required in the heap?
    iLen = ::WszWideCharToMultiByte(
        CP_UTF8,
        0,
        szString,
        -1,     // null-terminated string
        NULL,
        0,
        NULL,
        NULL);
    // WCTMB includes trailing 0 if (when passing parameter #4 (length) -1.

    // Check for room.
    if (iLen > GetCbSegAvailable())
    {
        if (!Grow(iLen))
            return (PostError(OutOfMemory()));
    }
    pData = reinterpret_cast<LPSTR>(GetNextLocation());

    // Convert the data in place to the correct location.
    iLen = ::WszWideCharToMultiByte(
        CP_UTF8,
        0,
        szString,
        -1,
        pData,
        GetCbSegAvailable(),
        NULL,
        NULL);
    if (iLen == 0)
        return (BadError(HRESULT_FROM_NT(GetLastError())));

    // If the hash table is to be kept built (default).
    if (m_bHash)
    {
        // Find or add the entry.
        pHash = m_Hash.Find(pData, true);
        if (!pHash)
            return (PostError(OutOfMemory()));

        // If the entry was new, keep the new string.
        if (pHash->iOffset == 0xffffffff)
        {
            *pnOffset = pHash->iOffset = GetNextOffset();
            SegAllocate(iLen);
        }
        // Else use the old one.
        else
        {
            *pnOffset = pHash->iOffset;
        }
    }
    // Probably an import which defers the hash table for speed.
    else
    {
        *pnOffset = GetNextOffset();
        SegAllocate(iLen);
    }
    return (S_OK);
} // StgStringPool::AddStringW


//*****************************************************************************
// Clears out the existing hash table used to eliminate duplicates.  Then
// rebuilds the hash table from scratch based on the current data.
//*****************************************************************************
__checkReturn
HRESULT
StgStringPool::RehashStrings()
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    ULONG       iOffset;                // Loop control.
    ULONG       iMax;                   // End of loop.
    ULONG       iSeg;                   // Location within segment.
    StgPoolSeg  *pSeg = this;           // To loop over segments.
    STRINGHASH  *pHash;                 // Hash item for add.
    LPCSTR      pString;                // A string;
    ULONG       iLen;                   // The string's length.
    int         iBuckets;               // Buckets in the hash.
    int         iCount;                 // Items in the hash.
    int         iNewBuckets;            // New count of buckets in the hash.

    // Determine the new bucket size.
    iBuckets = m_Hash.Buckets();
    iCount = m_Hash.Count();
    iNewBuckets = max(iCount, iBuckets+iBuckets/2+1);

    // Remove any stale data.
    m_Hash.Clear();
    m_Hash.SetBuckets(iNewBuckets);

    // How far should the loop go.
    iMax = GetNextOffset();

    // Go through each string, skipping initial empty string.
    for (iSeg=iOffset=1;  iOffset < iMax;  )
    {
        // Get the string from the pool.
        pString = reinterpret_cast<LPCSTR>(pSeg->m_pSegData + iSeg);
        // Add the string to the hash table.
        if ((pHash = m_Hash.Add(pString)) == 0)
            return (PostError(OutOfMemory()));
        pHash->iOffset = iOffset;

        // Move to next string.
        iLen = (ULONG)(strlen(pString) + 1);
        iOffset += iLen;
        iSeg += iLen;
        if (iSeg >= pSeg->m_cbSegNext)
        {
            pSeg = pSeg->m_pNextSeg;
            iSeg = 0;
        }
    }
    return (S_OK);
} // StgStringPool::RehashStrings

//
//
// StgGuidPool
//
//

__checkReturn
HRESULT
StgGuidPool::InitNew(
    ULONG cbSize,       // Estimated size.
    ULONG cItems)       // Estimated item count.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT     hr;                     // A result.

    if (FAILED(hr = StgPool::InitNew()))
        return (hr);

    // Set initial table sizes, if specified.
    if (cbSize)
        if (!Grow(cbSize))
            return E_OUTOFMEMORY;
    if (cItems)
        m_Hash.SetBuckets(cItems);

    return (S_OK);
} // StgGuidPool::InitNew

//*****************************************************************************
// Load a Guid heap from persisted memory.  If a copy of the data is made
// (so that it may be updated), then a new hash table is generated which can
// be used to elminate duplicates with new Guids.
//*****************************************************************************
__checkReturn
HRESULT
StgGuidPool::InitOnMem(
    void *pData,        // Predefined data.
    ULONG iSize,        // Size of data.
    int   bReadOnly)    // true if append is forbidden.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT     hr;

    // Let base class init our memory structure.
    IfFailRet(StgPool::InitOnMem(pData, iSize, bReadOnly));

    // For init on existing mem case.
    if (pData && iSize)
    {
        // If we cannot update, then we don't need a hash table.
        if (bReadOnly)
            return S_OK;

        //<TODO>@todo: defer this until we hand out a pointer.</TODO>
        IfFailRet(TakeOwnershipOfInitMem());

        // Build the hash table on the data.
        if (FAILED(hr = RehashGuids()))
        {
            Uninit();
            return hr;
        }
    }

    return S_OK;
} // StgGuidPool::InitOnMem

//*****************************************************************************
// Clears the hash table then calls the base class.
//*****************************************************************************
void StgGuidPool::Uninit()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // Clear the hash table.
    m_Hash.Clear();

    // Let base class clean up.
    StgPool::Uninit();
} // StgGuidPool::Uninit

//*****************************************************************************
// Add a segment to the chain of segments.
//*****************************************************************************
__checkReturn
HRESULT
StgGuidPool::AddSegment(
    const void *pData,      // The data.
    ULONG       cbData,     // Size of the data.
    bool        bCopy)      // If true, make a copy of the data.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    // Want an integeral number of GUIDs.
    _ASSERTE((cbData % sizeof(GUID)) == 0);

    return StgPool::AddSegment(pData, cbData, bCopy);

} // StgGuidPool::AddSegment

//*****************************************************************************
// Turn hashing off or on.  If you turn hashing on, then any existing data is
// thrown away and all data is rehashed during this call.
//*****************************************************************************
__checkReturn
HRESULT
StgGuidPool::SetHash(int bHash)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT     hr = S_OK;

    // If turning on hash again, need to rehash all guids.
    if (bHash)
        hr = RehashGuids();

    m_bHash = bHash;
    return (hr);
} // StgGuidPool::SetHash

//*****************************************************************************
// The Guid will be added to the pool.  The index of the Guid in the pool
// is returned in *piIndex.  If the Guid is already in the pool, then the
// index will be to the existing copy of the Guid.
//*****************************************************************************
__checkReturn
HRESULT
StgGuidPool::AddGuid(
    const GUID *pGuid,          // The Guid to add to pool.
    UINT32     *pnIndex)        // Return 1-based index of Guid here.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    GUIDHASH *pHash = NULL;                  // Hash item for add.

    GUID guid = *pGuid;
    SwapGuid(&guid);

    // Special case for GUID_NULL
    if (guid == GUID_NULL)
    {
        *pnIndex = 0;
        return S_OK;
    }

    // If the hash table is to be kept built (default).
    if (m_bHash)
    {
        // Find or add the entry.
        pHash = m_Hash.Find(&guid, true);
        if (!pHash)
            return (PostError(OutOfMemory()));

        // If the guid was found, just use it.
        if (pHash->iIndex != 0xffffffff)
        {   // Return 1-based index.
            *pnIndex = pHash->iIndex;
            return S_OK;
        }
    }

    // Space on heap for new guid?
    if (sizeof(GUID) > GetCbSegAvailable())
    {
        if (!Grow(sizeof(GUID)))
            return (PostError(OutOfMemory()));
    }

    // Copy the guid to the heap.
    *reinterpret_cast<GUID*>(GetNextLocation()) = guid;

    // Give the 1-based index back to caller.
    *pnIndex = (GetNextOffset() / sizeof(GUID)) + 1;

    // If hashing, save the 1-based index in the hash.
    if (m_bHash)
        pHash->iIndex = *pnIndex;

    // Update heap counters.
    SegAllocate(sizeof(GUID));

    return S_OK;
} // StgGuidPool::AddGuid

//*****************************************************************************
// Recompute the hashes for the pool.
//*****************************************************************************
__checkReturn
HRESULT
StgGuidPool::RehashGuids()
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    ULONG       iOffset;                // Loop control.
    ULONG       iMax;                   // End of loop.
    ULONG       iSeg;                   // Location within segment.
    StgPoolSeg  *pSeg = this;           // To loop over segments.
    GUIDHASH    *pHash;                 // Hash item for add.
    GUID        *pGuid;                 // A guid;

    // Remove any stale data.
    m_Hash.Clear();

    // How far should the loop go.
    iMax = GetNextOffset();

    // Go through each guid.
    for (iSeg=iOffset=0;  iOffset < iMax;  )
    {
        // Get a pointer to the guid.
        pGuid = reinterpret_cast<GUID*>(pSeg->m_pSegData + iSeg);
        // Add the guid to the hash table.
        if ((pHash = m_Hash.Add(pGuid)) == 0)
            return (PostError(OutOfMemory()));
        pHash->iIndex = iOffset / sizeof(GUID);

        // Move to next Guid.
        iOffset += sizeof(GUID);
        iSeg += sizeof(GUID);
        if (iSeg > pSeg->m_cbSegNext)
        {
            pSeg = pSeg->m_pNextSeg;
            iSeg = 0;
        }
    }
    return (S_OK);
} // StgGuidPool::RehashGuids

//
//
// StgBlobPool
//
//



//*****************************************************************************
// Create a new, empty blob pool.
//*****************************************************************************
__checkReturn
HRESULT
StgBlobPool::InitNew(
    ULONG cbSize,           // Estimated size.
    ULONG cItems,           // Estimated item count.
    BOOL  fAddEmptryItem)   // Should we add an empty item at offset 0
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT hr;

    // Let base class intialize.
    IfFailRet(StgPool::InitNew());

    // Set initial table sizes, if specified.
    if (cbSize > 0)
    {
        if (!Grow(cbSize))
            return E_OUTOFMEMORY;
    }
    if (cItems > 0)
        m_Hash.SetBuckets(cItems);

    // Init with empty blob.

    // Normally must do this, regardless if we currently have anything in the pool.
    // If we don't do this, the first blob that gets added to the pool will
    // have an offset of 0. This will cause this blob to have a token of
    // 0x70000000, which is considered a nil string token.
    //
    // By inserting a zero length blob into the pool the being with, we're
    // assured that the first blob added to the pool will have an offset
    // of 1 and a token of 0x70000001, which is a valid token.
    //
    // The only time we wouldn't want to do this is if we're reading in a delta metadata.
    // Then, we don't care if the first string is at offset 0... when the delta gets applied,
    // the string will get moved to the appropriate offset.
    if (fAddEmptryItem)
    {
        MetaData::DataBlob emptyBlob(NULL, 0);
        UINT32 nIndex_Ignore;
        IfFailRet(AddBlob(&emptyBlob, &nIndex_Ignore));
        // Empty blob better be at offset 0.
        _ASSERTE(nIndex_Ignore == 0);
    }
    return hr;
} // StgBlobPool::InitNew

//*****************************************************************************
// Init the blob pool for use.  This is called for both create and read case.
// If there is existing data and bCopyData is true, then the data is rehashed
// to eliminate dupes in future adds.
//*****************************************************************************
__checkReturn
HRESULT
StgBlobPool::InitOnMem(
    void *pBuf,             // Predefined data.
    ULONG iBufSize,         // Size of data.
    int   bReadOnly)        // true if append is forbidden.
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT hr;

    // Let base class init our memory structure.
    IfFailRet(StgPool::InitOnMem(pBuf, iBufSize, bReadOnly));

    // Init hash table from existing data.
    // If we cannot update, we don't need a hash table.
    if (bReadOnly)
    {
        return S_OK;
    }

    //<TODO>@todo: defer this until we hand out a pointer.</TODO>
    IfFailRet(TakeOwnershipOfInitMem());

    UINT32 nMaxOffset = GetNextOffset();
    for (UINT32 nOffset = 0; nOffset < nMaxOffset; )
    {
        MetaData::DataBlob blob;
        BLOBHASH          *pHash;

        IfFailRet(GetBlobWithSizePrefix(nOffset, &blob));

        // Add the blob to the hash table.
        if ((pHash = m_Hash.Add(blob.GetDataPointer())) == NULL)
        {
            Uninit();
            return E_OUTOFMEMORY;
        }
        pHash->iOffset = nOffset;

        nOffset += blob.GetSize();
    }
    return S_OK;
} // StgBlobPool::InitOnMem

//*****************************************************************************
// Clears the hash table then calls the base class.
//*****************************************************************************
void StgBlobPool::Uninit()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // Clear the hash table.
    m_Hash.Clear();

    // Let base class clean up.
    StgPool::Uninit();
} // StgBlobPool::Uninit


//*****************************************************************************
// The blob will be added to the pool.  The offset of the blob in the pool
// is returned in *piOffset.  If the blob is already in the pool, then the
// offset will be to the existing copy of the blob.
//*****************************************************************************
__checkReturn
HRESULT
StgBlobPool::AddBlob(
    const MetaData::DataBlob *pData,
    UINT32                   *pnOffset)  // Return offset of blob here.
{
    BLOBHASH *pHash;            // Hash item for add.
    void     *pBytes;           // Working pointer.
    BYTE     *pStartLoc;        // Location to write real blob
    ULONG     iRequired;        // How much buffer for this blob?
    ULONG     iFillerLen;       // space to fill to make byte-aligned
    HRESULT   hr;

    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    // Can we handle this blob?
    if (pData->GetSize() > CPackedLen::MAX_LEN)
        return (PostError(CLDB_E_TOO_BIG));

    // worst case is we need three more bytes to ensure byte-aligned, hence the 3
    iRequired = pData->GetSize() + CPackedLen::Size(pData->GetSize()) + 3;
    if (iRequired > GetCbSegAvailable())
    {
        if (!Grow(iRequired))
            return (PostError(OutOfMemory()));
    }

    // unless changed due to alignment, the location of the blob is just
    // the value returned by GetNextLocation(), which is also a iFillerLen of
    // 0

    pStartLoc = (BYTE *)GetNextLocation();
    iFillerLen = 0;

    // technichally, only the data portion must be DWORD-aligned.  So, if the
    // data length is zero, we don't need to worry about alignment.

    // Pack in the length at pStartLoc (the start location)
    pBytes = CPackedLen::PutLength(pStartLoc, pData->GetSize());

    // Put the bytes themselves.
    memcpy(pBytes, pData->GetDataPointer(), pData->GetSize());

    // Find or add the entry.
    if ((pHash = m_Hash.Find(GetNextLocation() + iFillerLen, true)) == NULL)
        return (PostError(OutOfMemory()));

    // If the entry was new, keep the new blob.
    if (pHash->iOffset == 0xffffffff)
    {
        // this blob's offset is increased by iFillerLen bytes
        pHash->iOffset = *pnOffset = GetNextOffset() + iFillerLen;
        // only SegAllocate what we actually used, rather than what we requested
        SegAllocate(pData->GetSize() + CPackedLen::Size(pData->GetSize()) + iFillerLen);

        // Check for hash chains that are too long.
        if (m_Hash.MaxChainLength() > MAX_CHAIN_LENGTH)
        {
            IfFailRet(RehashBlobs());
        }
    }
    // Else use the old one.
    else
    {
        *pnOffset = pHash->iOffset;
    }

    return S_OK;
} // StgBlobPool::AddBlob

//*****************************************************************************
// Return a pointer to a blob, and the size of the blob.
//*****************************************************************************
__checkReturn
HRESULT
StgBlobPool::GetBlob(
    UINT32              nOffset,    // Offset of blob in pool.
    MetaData::DataBlob *pData)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    HRESULT hr;

    if (nOffset == 0)
    {
        // TODO: It would be nice to remove it, but people read behind the end of buffer,
        // e.g. VBC reads 2 zeros even though the size is 0 when it's storing string in the blob.
        // Nice to have: Move this to the public API only as a compat layer.
        pData->Init((BYTE *)m_zeros, 0);
        return S_OK;
    }

    IfFailGo(StgPool::GetData(nOffset, pData));

    UINT32 cbBlobContentSize;
    if (!pData->GetCompressedU(&cbBlobContentSize))
    {
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }
    if (!pData->TruncateToExactSize(cbBlobContentSize))
    {
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }

    return S_OK;
ErrExit:
    pData->Clear();
    return hr;
} // StgBlobPool::GetBlob

//*****************************************************************************
// Return a pointer to a blob, and the size of the blob.
//*****************************************************************************
__checkReturn
HRESULT
StgBlobPool::GetBlobWithSizePrefix(
    UINT32              nOffset,    // Offset of blob in pool.
    MetaData::DataBlob *pData)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    HRESULT hr;

    if (nOffset == 0)
    {
        // TODO: Should be a static empty blob once we get rid of m_zeros
        pData->Init((BYTE *)m_zeros, 1);
        return S_OK;
    }

    IfFailGo(StgPool::GetData(nOffset, pData));

    UINT32  cbBlobContentSize;
    UINT32  cbBlobSizePrefixSize;
    if (!pData->PeekCompressedU(&cbBlobContentSize, &cbBlobSizePrefixSize))
    {
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }
    //_ASSERTE(cbBlobSizePrefixSize <= 4);
    //_ASSERTE(cbBlobContentSize <= CompressedInteger::const_Max);

    // Cannot overflow, because previous asserts hold (in comments)
    UINT32 cbBlobSize;
    cbBlobSize = cbBlobContentSize + cbBlobSizePrefixSize;
    if (!pData->TruncateToExactSize(cbBlobSize))
    {
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }

    return S_OK;
ErrExit:
    pData->Clear();
    return hr;
} // StgBlobPool::GetBlob

//*****************************************************************************
// Turn hashing off or on.  If you turn hashing on, then any existing data is
// thrown away and all data is rehashed during this call.
//*****************************************************************************
__checkReturn
HRESULT
StgBlobPool::SetHash(int bHash)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    HRESULT     hr = S_OK;

    // If turning on hash again, need to rehash all Blobs.
    if (bHash)
        hr = RehashBlobs();

    //<TODO>@todo: m_bHash = bHash;</TODO>
    return (hr);
} // StgBlobPool::SetHash

//*****************************************************************************
// Clears out the existing hash table used to eliminate duplicates.  Then
// rebuilds the hash table from scratch based on the current data.
//*****************************************************************************
__checkReturn
HRESULT
StgBlobPool::RehashBlobs()
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    void const  *pBlob;                 // Pointer to a given blob.
    ULONG       cbBlob;                 // Length of a blob.
    int         iSizeLen = 0;           // Size of an encoded length.
    ULONG       iOffset;                // Location within iteration.
    ULONG       iMax;                   // End of loop.
    ULONG       iSeg;                   // Location within segment.
    StgPoolSeg  *pSeg = this;           // To loop over segments.
    BLOBHASH    *pHash;                 // Hash item for add.
    int         iBuckets;               // Buckets in the hash.
    int         iCount;                 // Items in the hash.
    int         iNewBuckets;            // New count of buckets in the hash.

    // Determine the new bucket size.
    iBuckets = m_Hash.Buckets();
    iCount = m_Hash.Count();
    iNewBuckets = max(iCount, iBuckets+iBuckets/2+1);

    // Remove any stale data.
    m_Hash.Clear();
    m_Hash.SetBuckets(iNewBuckets);

    // How far should the loop go.
    iMax = GetNextOffset();

    // Go through each string, skipping initial empty string.
    for (iSeg=iOffset=0; iOffset < iMax; )
    {
        // Get the string from the pool.
        pBlob = pSeg->m_pSegData + iSeg;

        cbBlob = CPackedLen::GetLength(pBlob, &iSizeLen);
        if (cbBlob == (ULONG)-1)
        {   // Invalid blob size encoding

            //#GarbageInBlobHeap
            // Note that this is allowed in ECMA spec (see chapter "#US and #Blob heaps"):
            //     Both these heaps can contain garbage, as long as any part that is reachable from any of
            //     the tables contains a valid 'blob'.

            // The hash is incomplete, which means that we might emit duplicate blob entries ... that is fine
            return S_OK;
        }
        //_ASSERTE((iSizeLen >= 1) && (iSizeLen <= 4) && (cbBlob <= 0x1fffffff));

        // Make it blob size incl. its size encoding (cannot integer overflow)
        cbBlob += iSizeLen;
        // Check for integer overflow and that the entire blob entry is in this segment
        if ((iSeg > (iSeg + cbBlob)) || ((iSeg + cbBlob) > pSeg->m_cbSegNext))
        {   // Invalid blob size

            // See code:#GarbageInBlobHeap
            // The hash is incomplete, which means that we might emit duplicate blob entries ... that is fine
            return S_OK;
        }

        // Add the blob to the hash table.
        if ((pHash = m_Hash.Add(pBlob)) == 0)
        {
            Uninit();
            return (E_OUTOFMEMORY);
        }
        pHash->iOffset = iOffset;

        // Move to next blob.
        iOffset += cbBlob;
        iSeg += cbBlob;
        if (iSeg >= pSeg->m_cbSegNext)
        {
            pSeg = pSeg->m_pNextSeg;
            iSeg = 0;
        }
    }
    return (S_OK);
} // StgBlobPool::RehashBlobs


//
// CInMemoryStream
//


ULONG
STDMETHODCALLTYPE CInMemoryStream::Release()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END

    ULONG       cRef = InterlockedDecrement(&m_cRef);
    if (cRef == 0)
    {
        if (m_dataCopy != NULL)
            delete [] m_dataCopy;

        delete this;
    }
    return (cRef);
} // CInMemoryStream::Release

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::QueryInterface(REFIID riid, PVOID *ppOut)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    if (!ppOut)
    {
        return E_POINTER;
    }

    *ppOut = NULL;
    if (riid == IID_IStream || riid == IID_ISequentialStream || riid == IID_IUnknown)
    {
        *ppOut = this;
        AddRef();
        return (S_OK);
    }

    return E_NOINTERFACE;

} // CInMemoryStream::QueryInterface

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::Read(
    void  *pv,
    ULONG  cb,
    ULONG *pcbRead)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

    ULONG       cbRead = min(cb, m_cbSize - m_cbCurrent);

    if (cbRead == 0)
        return (S_FALSE);
    memcpy(pv, (void *) ((ULONG_PTR) m_pMem + m_cbCurrent), cbRead);
    if (pcbRead)
        *pcbRead = cbRead;
    m_cbCurrent += cbRead;
    return (S_OK);
} // CInMemoryStream::Read

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::Write(
    const void *pv,
    ULONG       cb,
    ULONG      *pcbWritten)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

    if (ovadd_gt(m_cbCurrent, cb, m_cbSize))
        return (OutOfMemory());

    memcpy((BYTE *) m_pMem + m_cbCurrent, pv, cb);
    m_cbCurrent += cb;
    if (pcbWritten) *pcbWritten = cb;
    return (S_OK);
} // CInMemoryStream::Write

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::Seek(
    LARGE_INTEGER   dlibMove,
    DWORD           dwOrigin,
    ULARGE_INTEGER *plibNewPosition)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

    _ASSERTE(dwOrigin == STREAM_SEEK_SET || dwOrigin == STREAM_SEEK_CUR);
    _ASSERTE(dlibMove.QuadPart <= static_cast<LONGLONG>(UINT32_MAX));

    if (dwOrigin == STREAM_SEEK_SET)
    {
        m_cbCurrent = (ULONG) dlibMove.QuadPart;
    }
    else
    if (dwOrigin == STREAM_SEEK_CUR)
    {
        m_cbCurrent+= (ULONG)dlibMove.QuadPart;
    }

    if (plibNewPosition)
    {
            plibNewPosition->QuadPart = m_cbCurrent;
    }

    return (m_cbCurrent < m_cbSize) ? (S_OK) : E_FAIL;
} // CInMemoryStream::Seek

HRESULT
STDMETHODCALLTYPE
CInMemoryStream::CopyTo(
    IStream        *pstm,
    ULARGE_INTEGER  cb,
    ULARGE_INTEGER *pcbRead,
    ULARGE_INTEGER *pcbWritten)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

    HRESULT     hr;
    // We don't handle pcbRead or pcbWritten.
    _ASSERTE(pcbRead == 0);
    _ASSERTE(pcbWritten == 0);

    _ASSERTE(cb.QuadPart <= UINT32_MAX);
    ULONG       cbTotal = min(static_cast<ULONG>(cb.QuadPart), m_cbSize - m_cbCurrent);
    ULONG       cbRead=min(1024, cbTotal);
    CQuickBytes rBuf;
    void        *pBuf = rBuf.AllocNoThrow(cbRead);
    if (pBuf == 0)
        return (PostError(OutOfMemory()));

    while (cbTotal)
        {
            if (cbRead > cbTotal)
                cbRead = cbTotal;
            if (FAILED(hr=Read(pBuf, cbRead, 0)))
                return (hr);
            if (FAILED(hr=pstm->Write(pBuf, cbRead, 0)))
                return (hr);
            cbTotal -= cbRead;
        }

    // Adjust seek pointer to the end.
    m_cbCurrent = m_cbSize;

    return (S_OK);
} // CInMemoryStream::CopyTo

HRESULT
CInMemoryStream::CreateStreamOnMemory(
    void     *pMem,                     // Memory to create stream on.
    ULONG     cbSize,                   // Size of data.
    IStream **ppIStream,                // Return stream object here.
    BOOL      fDeleteMemoryOnRelease)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    CInMemoryStream *pIStream;          // New stream object.
    if ((pIStream = new (nothrow) CInMemoryStream) == 0)
        return (PostError(OutOfMemory()));
    pIStream->InitNew(pMem, cbSize);
    if (fDeleteMemoryOnRelease)
    {
        // make sure this memory is allocated using new
        pIStream->m_dataCopy = (BYTE *)pMem;
    }
    *ppIStream = pIStream;
    return (S_OK);
} // CInMemoryStream::CreateStreamOnMemory

HRESULT
CInMemoryStream::CreateStreamOnMemoryCopy(
    void     *pMem,
    ULONG     cbSize,
    IStream **ppIStream)
{
    CONTRACTL
    {
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END

    CInMemoryStream *pIStream;          // New stream object.
    if ((pIStream = new (nothrow) CInMemoryStream) == 0)
        return (PostError(OutOfMemory()));

    // Init the stream.
    pIStream->m_cbCurrent = 0;
    pIStream->m_cbSize = cbSize;

    // Copy the data.
    pIStream->m_dataCopy = new (nothrow) BYTE[cbSize];

    if (pIStream->m_dataCopy == NULL)
    {
        delete pIStream;
        return (PostError(OutOfMemory()));
    }

    pIStream->m_pMem = pIStream->m_dataCopy;
    memcpy(pIStream->m_dataCopy, pMem, cbSize);

    *ppIStream = pIStream;
    return (S_OK);
} // CInMemoryStream::CreateStreamOnMemoryCopy

//---------------------------------------------------------------------------
// CGrowableStream is a simple IStream implementation that grows as
// its written to. All the memory is contigious, so read access is
// fast. A grow does a realloc, so be aware of that if you're going to
// use this.
//---------------------------------------------------------------------------

//Constructs a new GrowableStream
// multiplicativeGrowthRate - when the stream grows it will be at least this
//   multiple of its old size. Values greater than 1 ensure O(N) amortized
//   performance growing the stream to size N, 1 ensures O(N^2) amortized perf
//   but gives the tightest memory usage. Valid range is [1.0, 2.0].
// additiveGrowthRate - when the stream grows it will increase in size by at least
//   this number of bytes. Larger numbers cause fewer re-allocations at the cost of
//   increased memory usage.
CGrowableStream::CGrowableStream(float multiplicativeGrowthRate, DWORD additiveGrowthRate)
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    m_swBuffer = NULL;
    m_dwBufferSize = 0;
    m_dwBufferIndex = 0;
    m_dwStreamLength = 0;
    m_cRef = 1;

    // Lets make sure these values stay somewhat sane... if you adjust the limits
    // make sure you also write correct overflow checking code in EnsureCapcity
    _ASSERTE(multiplicativeGrowthRate >= 1.0F && multiplicativeGrowthRate <= 2.0F);
    m_multiplicativeGrowthRate = min(max(1.0F, multiplicativeGrowthRate), 2.0F);

    _ASSERTE(additiveGrowthRate >= 1);
    m_additiveGrowthRate = max(1, additiveGrowthRate);
} // CGrowableStream::CGrowableStream

#ifndef DACCESS_COMPILE

CGrowableStream::~CGrowableStream()
{
    CONTRACTL
    {
        NOTHROW;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // Destroy the buffer.
    if (m_swBuffer != NULL)
        delete [] m_swBuffer;

    m_swBuffer = NULL;
    m_dwBufferSize = 0;
} // CGrowableStream::~CGrowableStream

// Grows the stream and optionally the internal buffer to ensure it is at least
// newLogicalSize
HRESULT CGrowableStream::EnsureCapacity(DWORD newLogicalSize)
{
    _ASSERTE(m_dwBufferSize >= m_dwStreamLength);

    // If there is no enough space left in the buffer, grow it
    if (newLogicalSize > m_dwBufferSize)
    {
        // Grow to max of newLogicalSize, m_dwBufferSize*multiplicativeGrowthRate, and
        // m_dwBufferSize+m_additiveGrowthRate
        S_UINT32 addSize = S_UINT32(m_dwBufferSize) + S_UINT32(m_additiveGrowthRate);
        if (addSize.IsOverflow())
        {
            addSize = S_UINT32(UINT_MAX);
        }

        // this should have been enforced in the constructor too
        _ASSERTE(m_multiplicativeGrowthRate <= 2.0 && m_multiplicativeGrowthRate >= 1.0);

        // 2*UINT_MAX doesn't overflow a float so this certain to be safe
        float multSizeF = (float)m_dwBufferSize * m_multiplicativeGrowthRate;
        DWORD multSize;
        if(multSizeF > (float)UINT_MAX)
        {
            multSize = UINT_MAX;
        }
        else
        {
            multSize = (DWORD)multSizeF;
        }

        DWORD newBufferSize = max(max(newLogicalSize, multSize), addSize.Value());

        char *tmp = new (nothrow) char[newBufferSize];
        if(tmp == NULL)
        {
            return E_OUTOFMEMORY;
        }

        if (m_swBuffer) {
            memcpy (tmp, m_swBuffer, m_dwBufferSize);
            delete [] m_swBuffer;
        }
        m_swBuffer = (BYTE *)tmp;
        m_dwBufferSize = newBufferSize;
    }

    _ASSERTE(m_dwBufferSize >= newLogicalSize);
    // the internal buffer is big enough, might have to increase logical size
    // though
    if(newLogicalSize > m_dwStreamLength)
    {
        m_dwStreamLength = newLogicalSize;
    }

    _ASSERTE(m_dwBufferSize >= m_dwStreamLength);
    return S_OK;
}

ULONG
STDMETHODCALLTYPE
CGrowableStream::Release()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FORBID_FAULT;

    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
        delete this;

    return cRef;
} // CGrowableStream::Release

HRESULT
STDMETHODCALLTYPE
CGrowableStream::QueryInterface(
    REFIID riid,
    PVOID *ppOut)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY

    if (riid != IID_IUnknown && riid!=IID_ISequentialStream && riid!=IID_IStream)
        return E_NOINTERFACE;

    *ppOut = this;
    AddRef();
    return (S_OK);
} // CGrowableStream::QueryInterface

HRESULT
CGrowableStream::Read(
    void  *pv,
    ULONG  cb,
    ULONG *pcbRead)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY

    HRESULT hr = S_OK;
    DWORD dwCanReadBytes = 0;

    if (NULL == pv)
        return E_POINTER;

    // short-circuit a zero-length read or see if we are at the end
    if (cb == 0 || m_dwBufferIndex >= m_dwStreamLength)
    {
        if (pcbRead != NULL)
            *pcbRead = 0;

        return S_OK;
    }

    // Figure out if we have enough room in the stream (excluding any
    // unused space at the end of the buffer)
    dwCanReadBytes = cb;

    S_UINT32 dwNewIndex = S_UINT32(dwCanReadBytes) + S_UINT32(m_dwBufferIndex);
    if (dwNewIndex.IsOverflow() || (dwNewIndex.Value() > m_dwStreamLength))
    {
        // Only read whatever is left in the buffer (if any)
        dwCanReadBytes = (m_dwStreamLength - m_dwBufferIndex);
    }

    // copy from our buffer to caller's buffer
    memcpy(pv, &m_swBuffer[m_dwBufferIndex], dwCanReadBytes);

    // adjust our current position
    m_dwBufferIndex += dwCanReadBytes;

    // if they want the info, tell them how many byte we read for them
    if (pcbRead != NULL)
        *pcbRead = dwCanReadBytes;

    return hr;
} // CGrowableStream::Read

HRESULT
CGrowableStream::Write(
    const void *pv,
    ULONG       cb,
    ULONG      *pcbWritten)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY

    HRESULT hr = S_OK;
    DWORD dwActualWrite = 0;

    // avoid NULL write
    if (cb == 0)
    {
        hr = S_OK;
        goto Error;
    }

    // Check if our buffer is large enough
    _ASSERTE(m_dwBufferIndex <= m_dwStreamLength);
    _ASSERTE(m_dwStreamLength <= m_dwBufferSize);

    // If there is no enough space left in the buffer, grow it
    if (cb > (m_dwStreamLength - m_dwBufferIndex))
    {
        // Determine the new size needed
        S_UINT32 size = S_UINT32(m_dwBufferSize) + S_UINT32(cb);
        if (size.IsOverflow())
        {
            hr = HRESULT_FROM_WIN32(ERROR_ARITHMETIC_OVERFLOW);
            goto Error;
        }

        hr = EnsureCapacity(size.Value());
        if(FAILED(hr))
        {
            goto Error;
        }
    }

    if ((pv != NULL) && (cb > 0))
    {
        // write to current position in the buffer
        memcpy(&m_swBuffer[m_dwBufferIndex], pv, cb);

        // now update our current index
        m_dwBufferIndex += cb;

        // in case they want to know the number of bytes written
        dwActualWrite = cb;
    }

Error:
    if (pcbWritten)
        *pcbWritten = dwActualWrite;

    return hr;
} // CGrowableStream::Write

STDMETHODIMP
CGrowableStream::Seek(
    LARGE_INTEGER   dlibMove,
    DWORD           dwOrigin,
    ULARGE_INTEGER *plibNewPosition)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY

    // a Seek() call on STREAM_SEEK_CUR and a dlibMove == 0 is a
    // request to get the current seek position.
    if ((dwOrigin == STREAM_SEEK_CUR && dlibMove.u.LowPart == 0) &&
        (dlibMove.u.HighPart == 0) &&
        (NULL != plibNewPosition))
    {
        goto Error;
    }

    // we only support STREAM_SEEK_SET (beginning of buffer)
    if (dwOrigin != STREAM_SEEK_SET)
        return E_NOTIMPL;

    // did they ask to seek past end of stream?  If so we're supposed to
    // extend with zeros.  But we've never supported that.
    if (dlibMove.u.LowPart > m_dwStreamLength)
        return E_UNEXPECTED;

    // we ignore the high part of the large integer
    SIMPLIFYING_ASSUMPTION(dlibMove.u.HighPart == 0);
    m_dwBufferIndex = dlibMove.u.LowPart;

Error:
    if (NULL != plibNewPosition)
    {
        plibNewPosition->u.HighPart = 0;
        plibNewPosition->u.LowPart = m_dwBufferIndex;
    }

    return S_OK;
} // CGrowableStream::Seek

STDMETHODIMP
CGrowableStream::SetSize(
    ULARGE_INTEGER libNewSize)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY

    DWORD dwNewSize = libNewSize.u.LowPart;

    _ASSERTE(libNewSize.u.HighPart == 0);

    // we don't support large allocations
    if (libNewSize.u.HighPart > 0)
        return E_OUTOFMEMORY;

    HRESULT hr = EnsureCapacity(dwNewSize);
    if(FAILED(hr))
    {
        return hr;
    }

    // EnsureCapacity doesn't shrink the logicalSize if dwNewSize is smaller
    // and SetSize is allowed to shrink the stream too. Note that we won't
    // release physical memory here, we just appear to get smaller
    m_dwStreamLength = dwNewSize;

    return S_OK;
} // CGrowableStream::SetSize

STDMETHODIMP
CGrowableStream::Stat(
    STATSTG *pstatstg,
    DWORD    grfStatFlag)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY

    if (NULL == pstatstg)
        return E_POINTER;

    // this is the only useful information we hand out - the length of the stream
    pstatstg->cbSize.u.HighPart = 0;
    pstatstg->cbSize.u.LowPart = m_dwStreamLength;
    pstatstg->type = STGTY_STREAM;

    // we ignore the grfStatFlag - we always assume STATFLAG_NONAME
    pstatstg->pwcsName = NULL;

    pstatstg->grfMode = 0;
    pstatstg->grfLocksSupported = 0;
    pstatstg->clsid = CLSID_NULL;
    pstatstg->grfStateBits = 0;

    return S_OK;
} // CGrowableStream::Stat

//
// Clone - Make a deep copy of the stream into a new cGrowableStream instance
//
// Arguments:
//   ppStream - required output parameter for the new stream instance
//
// Returns:
//   S_OK on succeess, or an error code on failure.
//
HRESULT
CGrowableStream::Clone(
    IStream **ppStream)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY

    if (NULL == ppStream)
        return E_POINTER;

    // Copy our entire buffer into the new stream
    CGrowableStream * newStream = new (nothrow) CGrowableStream();
    if (newStream == NULL)
    {
        return E_OUTOFMEMORY;
    }

    HRESULT hr = newStream->Write(m_swBuffer, m_dwStreamLength, NULL);
    if (FAILED(hr))
    {
        delete newStream;
        return hr;
    }

    *ppStream = newStream;
    return S_OK;
} // CGrowableStream::Clone

#endif // !DACCESS_COMPILE
