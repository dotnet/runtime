// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// StgPool.h
//

//
// Pools are used to reduce the amount of data actually required in the database.
// This allows for duplicate string and binary values to be folded into one
// copy shared by the rest of the database.  Strings are tracked in a hash
// table when insert/changing data to find duplicates quickly.  The strings
// are then persisted consecutively in a stream in the database format.
//
//*****************************************************************************

#ifndef __StgPool_h__
#define __StgPool_h__

#ifdef _MSC_VER
#pragma warning (disable : 4355)        // warning C4355: 'this' : used in base member initializer list
#endif

#include "stgpooli.h"                   // Internal helpers.
#include "corerror.h"                   // Error codes.
#include "metadatatracker.h"
#include "metamodelpub.h"
#include "ex.h"
#include "sarray.h"
#include "memoryrange.h"
#include "../md/hotdata/hotheap.h"

//*****************************************************************************
// NOTE:
// One limitation with the pools, we have no way to removing strings from
// the pool.  To remove, you need to know the ref count on the string, and
// need the ability to compact the pool and reset all references.
//*****************************************************************************

//********** Constants ********************************************************
const int DFT_STRING_HEAP_SIZE = 1024;
const int DFT_GUID_HEAP_SIZE = 32;
const int DFT_BLOB_HEAP_SIZE = 1024;
const int DFT_VARIANT_HEAP_SIZE = 512;
const int DFT_CODE_HEAP_SIZE = 8192;



// Forwards.
class StgStringPool;
class StgBlobPool;
class StgCodePool;
class CorProfileData;

//  Perform binary search on index table.
//
class RIDBinarySearch : public CBinarySearch<UINT32>
{
public:
    RIDBinarySearch(const UINT32 *pBase, int iCount) : CBinarySearch<UINT32>(pBase, iCount)
    {
        LIMITED_METHOD_CONTRACT;
    } // RIDBinarySearch::RIDBinarySearch

    int Compare(UINT32 const *pFirst, UINT32 const *pSecond)
    {
        LIMITED_METHOD_CONTRACT;

        if (*pFirst < *pSecond)
            return -1;

        if (*pFirst > *pSecond)
            return 1;

        return 0;
    } // RIDBinarySearch::Compare

};  // class RIDBinarySearch

//*****************************************************************************
// This class provides common definitions for heap segments.  It is both the
//  base class for the heap, and the class for heap extensions (additional
//  memory that must be allocated to grow the heap).
//*****************************************************************************
class StgPoolSeg
{
    friend class VerifyLayoutsMD;
public:
    StgPoolSeg() :
        m_pSegData((BYTE*)m_zeros),
        m_pNextSeg(NULL),
        m_cbSegSize(0),
        m_cbSegNext(0)
    {LIMITED_METHOD_CONTRACT;  }
    ~StgPoolSeg()
    { LIMITED_METHOD_CONTRACT; _ASSERTE(m_pSegData == m_zeros);_ASSERTE(m_pNextSeg == NULL); }
protected:
    BYTE       *m_pSegData;     // Pointer to the data.
    StgPoolSeg *m_pNextSeg;     // Pointer to next segment, or NULL.
    // Size of the segment buffer. If this is last segment (code:m_pNextSeg is NULL), then it's the
    // allocation size. If this is not the last segment, then this is shrinked to segment data size
    // (code:m_cbSegNext).
    ULONG       m_cbSegSize;
    ULONG       m_cbSegNext;    // Offset of next available byte in segment.
                                //  Segment relative.

    friend class StgPool;
    friend class StgStringPool;
    friend class StgGuidPool;
    friend class StgBlobPool;
    friend class RecordPool;

public:
    const BYTE *GetSegData() const { LIMITED_METHOD_CONTRACT; return m_pSegData; }
    const StgPoolSeg* GetNextSeg() const { LIMITED_METHOD_CONTRACT; return m_pNextSeg; }
    // Returns size of the segment. It can be bigger than the size of represented data by this segment if
    // this is the last segment.
    ULONG GetSegSize() const { LIMITED_METHOD_CONTRACT; return m_cbSegSize; }
    // Returns size of represented data in this segment.
    ULONG GetDataSize() const { LIMITED_METHOD_CONTRACT; return m_cbSegNext; }

    static const BYTE m_zeros[64];          // array of zeros for "0" indices.
                // The size should be at least maximum of all MD table record sizes
                // (MD\Runtime\MDColumnDescriptors.cpp) which is currently 28 B.
};  // class StgPoolSeg

namespace MetaData
{
    // Forward declarations
    class StringHeapRO;
    class StringHeapRW;
    class BlobHeapRO;
};  // namespace MetaData

//
//
// StgPoolReadOnly
//
//
//*****************************************************************************
// This is the read only StgPool class
//*****************************************************************************
class StgPoolReadOnly : public StgPoolSeg
{
friend class CBlobPoolHash;
friend class MetaData::StringHeapRO;
friend class MetaData::StringHeapRW;
friend class MetaData::BlobHeapRO;
friend class VerifyLayoutsMD;

public:
    StgPoolReadOnly()
    { LIMITED_METHOD_CONTRACT; };

    ~StgPoolReadOnly();


//*****************************************************************************
// Init the pool from existing data.
//*****************************************************************************
    __checkReturn
    HRESULT InitOnMemReadOnly(                // Return code.
        void        *pData,                    // Predefined data.
        ULONG        iSize);                    // Size of data.

//*****************************************************************************
// Prepare to shut down or reinitialize.
//*****************************************************************************
    virtual    void Uninit();

//*****************************************************************************
// Return the size of the pool.
//*****************************************************************************
    virtual UINT32 GetPoolSize() const
    { LIMITED_METHOD_CONTRACT; return m_cbSegSize; }

//*****************************************************************************
// Indicate if heap is empty.
//*****************************************************************************
    virtual int IsEmpty()                    // true if empty.
    { LIMITED_METHOD_CONTRACT; _ASSERTE(!"This implementation should never be called!!!"); return FALSE; }

//*****************************************************************************
// true if the heap is read only.
//*****************************************************************************
    virtual int IsReadOnly() { LIMITED_METHOD_CONTRACT; return true ;};

//*****************************************************************************
// Is the given cookie a valid offset, index, etc?
//*****************************************************************************
    virtual int IsValidCookie(UINT32 nCookie)
    { WRAPPER_NO_CONTRACT; return (IsValidOffset(nCookie)); }


#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:6387) // Suppress PREFast warning: '*pszString' might be '0': this does not adhere to the specification for the function
        // *pszString may be NULL only if method fails, but warning 6387 doesn't respect __success(SUCCEEDED(return)) which is part of HRESULT definition
#endif
//*****************************************************************************
// Return a pointer to a null terminated string given an offset previously
// handed out by AddString or FindString.
//*****************************************************************************
    __checkReturn
    inline HRESULT GetString(
                    UINT32  nIndex,
        __deref_out LPCSTR *pszString)
    {
        HRESULT hr;

        // Size of the data in the heap will be ignored, because we have verified during creation of the string
        // heap (code:Initialize) and when adding new strings (e.g. code:AddString,
        // code:AddTemporaryStringBuffer), that the heap is null-terminated, therefore we don't have to check it
        // for each string in the heap
        MetaData::DataBlob stringData;

        // Get data from the heap (clears stringData on error)
        IfFailGo(GetData(
            nIndex,
            &stringData));
        _ASSERTE(hr == S_OK);
        // Raw data are always at least 1 byte long, otherwise it would be invalid offset and hr != S_OK
        PREFAST_ASSUME(stringData.GetDataPointer() != NULL);
        // Fills output string
        *pszString = reinterpret_cast<LPSTR>(stringData.GetDataPointer());
        //_ASSERTE(stringData.GetSize() > strlen(*pszString));

        return hr;
    ErrExit:
        // Clears output string on error
        *pszString = NULL;

        return hr;
    }

//*****************************************************************************
// Return a pointer to a null terminated string given an offset previously
// handed out by AddString or FindString. Only valid for use if the Storage pool is actuall ReadOnly, and not derived
//*****************************************************************************
    __checkReturn
    inline HRESULT GetStringReadOnly(
                    UINT32  nIndex,
        __deref_out LPCSTR *pszString)
    {
        HRESULT hr;

        // Size of the data in the heap will be ignored, because we have verified during creation of the string
        // heap (code:Initialize) and when adding new strings (e.g. code:AddString,
        // code:AddTemporaryStringBuffer), that the heap is null-terminated, therefore we don't have to check it
        // for each string in the heap
        MetaData::DataBlob stringData;

        // Get data from the heap (clears stringData on error)
        IfFailGo(GetDataReadOnly(
            nIndex,
            &stringData));
        _ASSERTE(hr == S_OK);
        // Raw data are always at least 1 byte long, otherwise it would be invalid offset and hr != S_OK
        PREFAST_ASSUME(stringData.GetDataPointer() != NULL);
        // Fills output string
        *pszString = reinterpret_cast<LPSTR>(stringData.GetDataPointer());
        //_ASSERTE(stringData.GetSize() > strlen(*pszString));

        return hr;
    ErrExit:
        // Clears output string on error
        *pszString = NULL;

        return hr;
    }
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//*****************************************************************************
// Convert a string to UNICODE into the caller's buffer.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetStringW(                         // Return code.
        ULONG                          iOffset,         // Offset of string in pool.
        __out_ecount(cchBuffer) LPWSTR szOut,           // Output buffer for string.
        int                            cchBuffer);      // Size of output buffer.

//*****************************************************************************
// Copy a GUID into the caller's buffer.
//*****************************************************************************
    __checkReturn
    HRESULT GetGuid(
        UINT32           nIndex,        // 1-based index of Guid in pool.
        GUID UNALIGNED **ppGuid)        // Output buffer for Guid.
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        HRESULT hr;
        MetaData::DataBlob heapData;

        if (nIndex == 0)
        {
            *ppGuid = (GUID *)m_zeros;
            return S_OK;
        }

        S_UINT32 nOffset = S_UINT32(nIndex - 1) * S_UINT32(sizeof(GUID));
        if (nOffset.IsOverflow() || !IsValidOffset(nOffset.Value()))
        {
            Debug_ReportError("Invalid index passed - integer overflow.");
            IfFailGo(CLDB_E_INDEX_NOTFOUND);
        }
        if (FAILED(GetData(nOffset.Value(), &heapData)))
        {
            if (nOffset.Value() == 0)
            {
                Debug_ReportError("Invalid index 0 passed.");
                IfFailGo(CLDB_E_INDEX_NOTFOUND);
            }
            Debug_ReportInternalError("Invalid index passed.");
            IfFailGo(CLDB_E_INTERNALERROR);
        }
        _ASSERTE(heapData.GetSize() >= sizeof(GUID));

        *ppGuid = (GUID UNALIGNED *)heapData.GetDataPointer();
        return S_OK;

    ErrExit:
        *ppGuid = (GUID *)m_zeros;
        return hr;
    } // StgPoolReadOnly::GetGuid

//*****************************************************************************
// Return a pointer to a null terminated blob given an offset previously
// handed out by Addblob or Findblob.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetBlob(
        UINT32              nOffset,    // Offset of blob in pool.
        MetaData::DataBlob *pData);

protected:

//*****************************************************************************
// Check whether a given offset is valid in the pool.
//*****************************************************************************
    virtual int IsValidOffset(UINT32 nOffset)
    {LIMITED_METHOD_CONTRACT;  return (nOffset == 0) || ((m_pSegData != m_zeros) && (nOffset < m_cbSegSize)); }

//*****************************************************************************
// Get a pointer to an offset within the heap.  Inline for base segment,
//  helper for extension segments.
//*****************************************************************************
    __checkReturn
    FORCEINLINE HRESULT GetDataReadOnly(UINT32 nOffset, __inout MetaData::DataBlob *pData)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsReadOnly());

        // If off the end of the heap, return the 'nul' item from the beginning.
        if (nOffset >= m_cbSegSize)
        {
            Debug_ReportError("Invalid offset passed.");
            pData->Clear();
            return CLDB_E_INDEX_NOTFOUND;
        }


        pData->Init(m_pSegData + nOffset, m_cbSegSize - nOffset);

        METADATATRACKER_ONLY(MetaDataTracker::NoteAccess((void *)pData->GetDataPointer()));

        return S_OK;
    } // StgPoolReadOnly::GetDataReadOnly

//*****************************************************************************
// Get a pointer to an offset within the heap.  Inline for base segment,
//  helper for extension segments.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetData(UINT32 nOffset, __inout MetaData::DataBlob *pData)
    {
        WRAPPER_NO_CONTRACT;
        return GetDataReadOnly(nOffset, pData);
    } // StgPoolReadOnly::GetData

private:
#if !defined(FEATURE_UTILCODE_NO_DEPENDENCIES)
    // hot pool data
    MetaData::HotHeap m_HotHeap;
#endif //!(defined(FEATURE_UTILCODE_NO_DEPENDENCIES))

};  // class StgPoolReadOnly

//
//
// StgBlobPoolReadOnly
//
//
//*****************************************************************************
// This is the read only StgBlobPool class
//*****************************************************************************
class StgBlobPoolReadOnly : public StgPoolReadOnly
{
public:
//*****************************************************************************
// Return a pointer to a null terminated blob given an offset
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetBlob(
        UINT32              nOffset,    // Offset of blob in pool.
        MetaData::DataBlob *pData);

protected:

//*****************************************************************************
// Check whether a given offset is valid in the pool.
//*****************************************************************************
    virtual int IsValidOffset(UINT32 nOffset)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        MetaData::DataBlob data;
        return (StgBlobPoolReadOnly::GetBlob(nOffset, &data) == S_OK);
    }

};  // class StgBlobPoolReadOnly

//
//
// StgPool
//
//

//*****************************************************************************
// This base class provides common pool management code, such as allocation
// of dynamic memory.
//*****************************************************************************
class StgPool : public StgPoolReadOnly
{
friend class StgStringPool;
friend class StgBlobPool;
friend class RecordPool;
friend class CBlobPoolHash;
friend class VerifyLayoutsMD;

public:
    StgPool(ULONG ulGrowInc=512, UINT32 nAlignment=4) :
        m_ulGrowInc(ulGrowInc),
        m_pCurSeg(this),
        m_cbCurSegOffset(0),
        m_bFree(true),
        m_bReadOnly(false),
        m_nVariableAlignmentMask(nAlignment-1),
        m_cbStartOffsetOfEdit(0),
        m_fValidOffsetOfEdit(0)
    { LIMITED_METHOD_CONTRACT; }

    virtual ~StgPool();

protected:
    HRESULT Align(UINT32 nValue, UINT32 *pnAlignedValue) const
    {
        LIMITED_METHOD_CONTRACT;

        *pnAlignedValue = (nValue + m_nVariableAlignmentMask) & ~m_nVariableAlignmentMask;
        if (*pnAlignedValue < nValue)
        {
            return COR_E_OVERFLOW;
        }
        return S_OK;
    }

public:
//*****************************************************************************
// Init the pool for use.  This is called for the create empty case.
//*****************************************************************************
    __checkReturn
    virtual HRESULT InitNew(                 // Return code.
        ULONG        cbSize=0,                // Estimated size.
        ULONG        cItems=0);                // Estimated item count.

//*****************************************************************************
// Init the pool from existing data.
//*****************************************************************************
    __checkReturn
    virtual HRESULT InitOnMem(                // Return code.
        void        *pData,                    // Predefined data.
        ULONG        iSize,                    // Size of data.
        int            bReadOnly);                // true if append is forbidden.

//*****************************************************************************
// Called when the pool must stop accessing memory passed to InitOnMem().
//*****************************************************************************
    __checkReturn
    virtual HRESULT TakeOwnershipOfInitMem();

//*****************************************************************************
// Clear out this pool.  Cannot use until you call InitNew.
//*****************************************************************************
    virtual void Uninit();

//*****************************************************************************
// Called to copy the pool to writable memory, reset the r/o bit.
//*****************************************************************************
    __checkReturn
    virtual HRESULT ConvertToRW();

//*****************************************************************************
// Turn hashing off or on.  Implemented as required in subclass.
//*****************************************************************************
    __checkReturn
    virtual HRESULT SetHash(int bHash);

//*****************************************************************************
// Allocate memory if we don't have any, or grow what we have.  If successful,
// then at least iRequired bytes will be allocated.
//*****************************************************************************
    bool Grow(                                // true if successful.
        ULONG        iRequired);                // Min required bytes to allocate.

//*****************************************************************************
// Add a segment to the chain of segments.
//*****************************************************************************
    __checkReturn
    virtual HRESULT AddSegment(                // S_OK or error.
        const void    *pData,                    // The data.
        ULONG        cbData,                    // Size of the data.
        bool        bCopy);                    // If true, make a copy of the data.

//*****************************************************************************
// Trim any empty final segment.
//*****************************************************************************
    void Trim();                            //

//*****************************************************************************
// Return the size in bytes of the persistent version of this pool.  If
// PersistToStream were the next call, the amount of bytes written to pIStream
// has to be same as the return value from this function.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetSaveSize(
        UINT32 *pcbSaveSize) const
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        _ASSERTE(pcbSaveSize != NULL);
        // Size is offset of last seg + size of last seg.
        UINT32 cbSize = m_pCurSeg->m_cbSegNext + m_cbCurSegOffset;

        if (FAILED(Align(cbSize, pcbSaveSize)))
        {
            *pcbSaveSize = 0;
            Debug_ReportInternalError("Aligned size of string heap overflows - we should prevent creating such heaps.");
            return CLDB_E_INTERNALERROR;
        }
        return S_OK;
    }

//*****************************************************************************
// Return the size in bytes of the edits contained in the persistent version of this pool.
//*****************************************************************************
    __checkReturn
    HRESULT GetEditSaveSize(
        UINT32 *pcbSaveSize) const  // Return save size of this pool.
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        _ASSERTE(pcbSaveSize != NULL);
        UINT32 cbSize = 0;

        if (HaveEdits())
        {
            // Size is offset of last seg + size of last seg.

            // An offset of zero in the pool will give us a zero length blob. The first
            // "real" user string is at offset 1. Wherever this delta gets applied, it will
            // already have this zero length blob. Let's make sure we don't sent it another one.
#ifdef _DEBUG
            MetaData::DataBlob debug_data;
            HRESULT hr = const_cast<StgPool *>(this)->GetData(0, &debug_data);
            _ASSERTE(hr == S_OK);
            _ASSERTE(debug_data.ContainsData(1));
            _ASSERTE(*(debug_data.GetDataPointer()) == 0);
#endif //_DEBUG
            UINT32 nOffsetOfEdit = GetOffsetOfEdit();

            if (nOffsetOfEdit == 0)
                nOffsetOfEdit = 1;

            cbSize = m_pCurSeg->m_cbSegNext + m_cbCurSegOffset - nOffsetOfEdit;
        }

        if (FAILED(Align(cbSize, pcbSaveSize)))
        {
            *pcbSaveSize = 0;
            Debug_ReportInternalError("Aligned size of string heap overflows - we should prevent creating such heaps.");
            return CLDB_E_INTERNALERROR;
        }
        return S_OK;
    } // StgPool::GetEditSaveSize

//*****************************************************************************
// The entire pool is written to the given stream. The stream is aligned
// to a 4 byte boundary.
//*****************************************************************************
    __checkReturn
    virtual HRESULT PersistToStream(        // Return code.
        IStream        *pIStream)                 // The stream to write to.
        DAC_UNEXPECTED();

//*****************************************************************************
// A portion of the pool is written to the stream.  Must not be optimized.
//*****************************************************************************
    __checkReturn
    virtual HRESULT PersistPartialToStream(    // Return code.
        IStream        *pIStream,                // The stream to write to.
        ULONG        iOffset);                // Starting byte.

//*****************************************************************************
// Get the size of the data block obtained from the pool.
// Needed for generic persisting of data blocks.
// Override in concrete pool classes to return the correct size.
//*****************************************************************************
    virtual ULONG GetSizeOfData( void const * data )
    {
        LIMITED_METHOD_CONTRACT;
        return 0;
    }

//*****************************************************************************
// Return the size of the pool.
//*****************************************************************************
    virtual UINT32 GetPoolSize() const
    {LIMITED_METHOD_CONTRACT;  return m_pCurSeg->m_cbSegNext + m_cbCurSegOffset; }

//*****************************************************************************
// Indicate if heap is empty.
//*****************************************************************************
    virtual int IsEmpty()                    // true if empty.
    {LIMITED_METHOD_CONTRACT;  return (m_pSegData == m_zeros); }

//*****************************************************************************
// true if the heap is read only.
//*****************************************************************************
    int IsReadOnly()
    {LIMITED_METHOD_CONTRACT;  return (m_bReadOnly == false); }

//*****************************************************************************
// Is the given cookie a valid offset, index, etc?
//*****************************************************************************
    virtual int IsValidCookie(UINT32 nCookie)
    { WRAPPER_NO_CONTRACT; return (IsValidOffset(nCookie)); }

//*****************************************************************************
// Get a pointer to an offset within the heap.  Inline for base segment,
//  helper for extension segments.
//*****************************************************************************
    __checkReturn
    FORCEINLINE HRESULT GetData(UINT32 nOffset, MetaData::DataBlob *pData)
    {
        WRAPPER_NO_CONTRACT;
        if (nOffset < m_cbSegNext)
        {
            pData->Init(m_pSegData + nOffset, m_cbSegNext - nOffset);
            return S_OK;
        }
        else
        {
            return GetData_i(nOffset, pData);
        }
    } // StgPool::GetData

    // Copies data from pSourcePool starting at index nStartSourceIndex.
    __checkReturn
    HRESULT CopyPool(
        UINT32         nStartSourceIndex,
        const StgPool *pSourcePool);

//*****************************************************************************
// Copies data from the pool into a buffer. It will correctly walk the different
// segments for the copy
//*****************************************************************************
private:
    __checkReturn
    HRESULT CopyData(
        UINT32  nOffset,
        BYTE   *pBuffer,
        UINT32  cbBuffer,
        UINT32 *pcbWritten) const;

public:
//*****************************************************************************
// Helpers for dump utilities.
//*****************************************************************************
    UINT32 GetRawSize() const
    {
        LIMITED_METHOD_CONTRACT;

        // Size is offset of last seg + size of last seg.
        return m_pCurSeg->m_cbSegNext + m_cbCurSegOffset;
    }

    BOOL HaveEdits() const {LIMITED_METHOD_CONTRACT; return m_fValidOffsetOfEdit;}
    UINT32 GetOffsetOfEdit() const {LIMITED_METHOD_CONTRACT; return m_cbStartOffsetOfEdit;}
    void ResetOffsetOfEdit() {LIMITED_METHOD_CONTRACT; m_fValidOffsetOfEdit=FALSE;}

protected:

//*****************************************************************************
// Check whether a given offset is valid in the pool.
//*****************************************************************************
    virtual int IsValidOffset(UINT32 nOffset)
    { WRAPPER_NO_CONTRACT; return (nOffset == 0) || ((m_pSegData != m_zeros) && (nOffset < GetNextOffset())); }

    // Following virtual because a) this header included outside the project, and
    //  non-virtual function call (in non-expanded inline function!!) generates
    //  an external def, which causes linkage errors.
    __checkReturn
    virtual HRESULT GetData_i(UINT32 nOffset, MetaData::DataBlob *pData);

    // Get pointer to next location to which to write.
    BYTE *GetNextLocation()
    {LIMITED_METHOD_CONTRACT;  return (m_pCurSeg->m_pSegData + m_pCurSeg->m_cbSegNext); }

    // Get pool-relative offset of next location to which to write.
    ULONG GetNextOffset()
    {LIMITED_METHOD_CONTRACT;  return (m_cbCurSegOffset + m_pCurSeg->m_cbSegNext); }

    // Get count of bytes available in tail segment of pool.
    ULONG GetCbSegAvailable()
    {LIMITED_METHOD_CONTRACT;  return (m_pCurSeg->m_cbSegSize - m_pCurSeg->m_cbSegNext); }

    // Allocate space from the segment.

    void SegAllocate(ULONG cb)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(cb <= GetCbSegAvailable());

        if (!m_fValidOffsetOfEdit)
        {
            m_cbStartOffsetOfEdit = GetNextOffset();
            m_fValidOffsetOfEdit = TRUE;
        }

        m_pCurSeg->m_cbSegNext += cb;
    }// SegAllocate



    ULONG        m_ulGrowInc;                // How many bytes at a time.
    StgPoolSeg    *m_pCurSeg;                    // Current seg for append -- end of chain.
    ULONG       m_cbCurSegOffset;           // Base offset of current seg.

    unsigned    m_bFree        : 1;            // True if we should free base data.
                                            //  Extension data is always freed.
    unsigned    m_bReadOnly    : 1;            // True if we shouldn't append.

    UINT32 m_nVariableAlignmentMask;    // Alignment mask (variable 0, 1 or 3).
    UINT32 m_cbStartOffsetOfEdit;       // Place in the pool where edits started
    BOOL   m_fValidOffsetOfEdit;        // Is the pool edit offset valid

};


//
//
// StgStringPool
//
//



//*****************************************************************************
// This string pool class collects user strings into a big consecutive heap.
// Internally it manages this data in a hash table at run time to help throw
// out duplicates.  The list of strings is kept in memory while adding, and
// finally flushed to a stream at the caller's request.
//*****************************************************************************
class StgStringPool : public StgPool
{
    friend class VerifyLayoutsMD;
public:
    StgStringPool() :
        StgPool(DFT_STRING_HEAP_SIZE),
        m_Hash(this),
        m_bHash(true)
    {
        LIMITED_METHOD_CONTRACT;
        // force some code in debug.
        _ASSERTE(m_bHash);
    }

//*****************************************************************************
// Create a new, empty string pool.
//*****************************************************************************
    __checkReturn
    HRESULT InitNew(                         // Return code.
        ULONG        cbSize=0,                // Estimated size.
        ULONG        cItems=0);                // Estimated item count.

//*****************************************************************************
// Load a string heap from persisted memory.  If a copy of the data is made
// (so that it may be updated), then a new hash table is generated which can
// be used to elminate duplicates with new strings.
//*****************************************************************************
    __checkReturn
    HRESULT InitOnMem(                        // Return code.
        void        *pData,                    // Predefined data.
        ULONG        iSize,                    // Size of data.
        int            bReadOnly);                // true if append is forbidden.

//*****************************************************************************
// Clears the hash table then calls the base class.
//*****************************************************************************
    void Uninit();

//*****************************************************************************
// Turn hashing off or on.  If you turn hashing on, then any existing data is
// thrown away and all data is rehashed during this call.
//*****************************************************************************
    __checkReturn
    virtual HRESULT SetHash(int bHash);

//*****************************************************************************
// The string will be added to the pool.  The offset of the string in the pool
// is returned in *piOffset.  If the string is already in the pool, then the
// offset will be to the existing copy of the string.
//
// The first version essentially adds a zero-terminated sequence of bytes
//  to the pool.  MBCS pairs will not be converted to the appropriate UTF8
//  sequence.  The second version converts from Unicode.
//*****************************************************************************
    __checkReturn
    HRESULT AddString(
        LPCSTR  szString,       // The string to add to pool.
        UINT32 *pnOffset);      // Return offset of string here.

    __checkReturn
    HRESULT AddStringW(
        LPCWSTR szString,       // The string to add to pool.
        UINT32 *pnOffset);      // Return offset of string here.

//*****************************************************************************
// Look for the string and return its offset if found.
//*****************************************************************************
    __checkReturn
    HRESULT FindString(                        // S_OK, S_FALSE.
        LPCSTR        szString,                // The string to find in pool.
        ULONG        *piOffset)                // Return offset of string here.
    {
        WRAPPER_NO_CONTRACT;

        STRINGHASH    *pHash;                    // Hash item for lookup.
        if ((pHash = m_Hash.Find(szString)) == 0)
            return (S_FALSE);
        *piOffset = pHash->iOffset;
        return (S_OK);
    }

//*****************************************************************************
// How many objects are there in the pool?  If the count is 0, you don't need
// to persist anything at all to disk.
//*****************************************************************************
    int Count()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_bHash);
        return (m_Hash.Count()); }

//*****************************************************************************
// String heap is considered empty if the only thing it has is the initial
// empty string, or if after organization, there are no strings.
//*****************************************************************************
    int IsEmpty()                        // true if empty.
    {
        WRAPPER_NO_CONTRACT;

        return (GetNextOffset() <= 1);
    }

//*****************************************************************************
// Return the size in bytes of the persistent version of this pool.  If
// PersistToStream were the next call, the amount of bytes written to pIStream
// has to be same as the return value from this function.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetSaveSize(
        UINT32 *pcbSaveSize) const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(pcbSaveSize != NULL);

        // Size is offset of last seg + size of last seg.
        S_UINT32 cbSize = S_UINT32(m_pCurSeg->m_cbSegNext + m_cbCurSegOffset);

        cbSize.AlignUp(4);

        if (cbSize.IsOverflow())
        {
            *pcbSaveSize = 0;
            Debug_ReportInternalError("Aligned size of string heap overflows - we should prevent creating such heaps.");
            return CLDB_E_INTERNALERROR;
        }
        *pcbSaveSize = cbSize.Value();
        return S_OK;
    }

//*****************************************************************************
// Get the size of the string obtained from the pool.
// Needed for generic persisting of data blocks.
//*****************************************************************************
    virtual ULONG GetSizeOfData( void const * data )
    {
        LIMITED_METHOD_CONTRACT;
        return ULONG( strlen( reinterpret_cast< LPCSTR >( data ) ) + 1 ); // using strlen since the string is UTF8
    }

private:
    __checkReturn
    HRESULT RehashStrings();

private:
    CStringPoolHash m_Hash;                    // Hash table for lookups.
    int            m_bHash;                    // true to keep hash table.
};  // class StgStringPool

//
//
// StgGuidPool
//
//

//*****************************************************************************
// This Guid pool class collects user Guids into a big consecutive heap.
// Internally it manages this data in a hash table at run time to help throw
// out duplicates.  The list of Guids is kept in memory while adding, and
// finally flushed to a stream at the caller's request.
//*****************************************************************************
class StgGuidPool : public StgPool
{
    friend class VerifyLayoutsMD;
public:
    StgGuidPool() :
        StgPool(DFT_GUID_HEAP_SIZE),
        m_Hash(this),
        m_bHash(true)
    { LIMITED_METHOD_CONTRACT; }

//*****************************************************************************
// Init the pool for use.  This is called for the create empty case.
//*****************************************************************************
    __checkReturn
    HRESULT InitNew(                         // Return code.
        ULONG        cbSize=0,                // Estimated size.
        ULONG        cItems=0);                // Estimated item count.

//*****************************************************************************
// Load a Guid heap from persisted memory.  If a copy of the data is made
// (so that it may be updated), then a new hash table is generated which can
// be used to elminate duplicates with new Guids.
//*****************************************************************************
    __checkReturn
    HRESULT InitOnMem(                      // Return code.
        void        *pData,                    // Predefined data.
        ULONG        iSize,                    // Size of data.
        int         bReadOnly);             // true if append is forbidden.

//*****************************************************************************
// Clears the hash table then calls the base class.
//*****************************************************************************
    void Uninit();

//*****************************************************************************
// Add a segment to the chain of segments.
//*****************************************************************************
    __checkReturn
    virtual HRESULT AddSegment(                // S_OK or error.
        const void    *pData,                    // The data.
        ULONG        cbData,                    // Size of the data.
        bool        bCopy);                    // If true, make a copy of the data.

//*****************************************************************************
// Turn hashing off or on.  If you turn hashing on, then any existing data is
// thrown away and all data is rehashed during this call.
//*****************************************************************************
    __checkReturn
    virtual HRESULT SetHash(int bHash);

//*****************************************************************************
// The Guid will be added to the pool.  The index of the Guid in the pool
// is returned in *piIndex.  If the Guid is already in the pool, then the
// index will be to the existing copy of the Guid.
//*****************************************************************************
    __checkReturn
    HRESULT AddGuid(
        const GUID *pGuid,          // The Guid to add to pool.
        UINT32     *pnIndex);       // Return index of Guid here.

//*****************************************************************************
// Get the size of the GUID obtained from the pool.
// Needed for generic persisting of data blocks.
//*****************************************************************************
    virtual ULONG GetSizeOfData( void const * data )
    {
        LIMITED_METHOD_CONTRACT;
        return sizeof( GUID );
    }

//*****************************************************************************
// How many objects are there in the pool?  If the count is 0, you don't need
// to persist anything at all to disk.
//*****************************************************************************
    int Count()
    {
      WRAPPER_NO_CONTRACT;
      _ASSERTE(m_bHash);
        return (m_Hash.Count()); }

//*****************************************************************************
// Indicate if heap is empty.  This has to be based on the size of the data
// we are keeping.  If you open in r/o mode on memory, there is no hash
// table.
//*****************************************************************************
    virtual int IsEmpty()                    // true if empty.
    {
        WRAPPER_NO_CONTRACT;

        return (GetNextOffset() == 0);
    }

//*****************************************************************************
// Is the index valid for the GUID?
//*****************************************************************************
    virtual int IsValidCookie(UINT32 nCookie)
    { WRAPPER_NO_CONTRACT; return ((nCookie == 0) || IsValidOffset((nCookie - 1) * sizeof(GUID))); }

//*****************************************************************************
// Return the size of the heap.
//*****************************************************************************
    ULONG GetNextIndex()
    { LIMITED_METHOD_CONTRACT; return (GetNextOffset() / sizeof(GUID)); }

//*****************************************************************************
// Return the size in bytes of the persistent version of this pool.  If
// PersistToStream were the next call, the amount of bytes written to pIStream
// has to be same as the return value from this function.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetSaveSize(
        UINT32 *pcbSaveSize) const
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        _ASSERTE(pcbSaveSize != NULL);

        // Size is offset of last seg + size of last seg.
        *pcbSaveSize = m_pCurSeg->m_cbSegNext + m_cbCurSegOffset;

        // Should be aligned.
        _ASSERTE(*pcbSaveSize == ALIGN4BYTE(*pcbSaveSize));
        return S_OK;
    }

private:

    __checkReturn
    HRESULT RehashGuids();


private:
    DAC_ALIGNAS(StgPool) // Align first member to alignment of base class
    CGuidPoolHash m_Hash;                    // Hash table for lookups.
    int            m_bHash;                    // true to keep hash table.
};  // class StgGuidPool

//
//
// StgBlobPool
//
//

//*****************************************************************************
// Just like the string pool, this pool manages a list of items, throws out
// duplicates using a hash table, and can be persisted to a stream.  The only
// difference is that instead of saving null terminated strings, this code
// manages binary values of up to 64K in size.  Any data you have larger than
// this should be stored someplace else with a pointer in the record to the
// external source.
//*****************************************************************************
class StgBlobPool : public StgPool
{
    friend class VerifyLayoutsMD;

    using StgPool::InitNew;
    using StgPool::InitOnMem;

public:
    StgBlobPool(ULONG ulGrowInc=DFT_BLOB_HEAP_SIZE) :
        StgPool(ulGrowInc),
        m_Hash(this)
    { LIMITED_METHOD_CONTRACT; }

//*****************************************************************************
// Init the pool for use.  This is called for the create empty case.
//*****************************************************************************
    __checkReturn
    HRESULT InitNew(                         // Return code.
        ULONG        cbSize=0,                // Estimated size.
        ULONG        cItems=0,               // Estimated item count.
        BOOL        fAddEmptryItem=TRUE);        // Should we add an empty item at offset 0

//*****************************************************************************
// Init the blob pool for use.  This is called for both create and read case.
// If there is existing data and bCopyData is true, then the data is rehashed
// to eliminate dupes in future adds.
//*****************************************************************************
    __checkReturn
    HRESULT InitOnMem(                      // Return code.
        void        *pData,                    // Predefined data.
        ULONG        iSize,                    // Size of data.
        int         bReadOnly);             // true if append is forbidden.

//*****************************************************************************
// Clears the hash table then calls the base class.
//*****************************************************************************
    void Uninit();

//*****************************************************************************
// The blob will be added to the pool.  The offset of the blob in the pool
// is returned in *piOffset.  If the blob is already in the pool, then the
// offset will be to the existing copy of the blob.
//*****************************************************************************
    __checkReturn
    HRESULT AddBlob(
        const MetaData::DataBlob *pData,
        UINT32                   *pnOffset);

//*****************************************************************************
// Return a pointer to a null terminated blob given an offset previously
// handed out by Addblob or Findblob.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetBlob(
        UINT32              nOffset,    // Offset of blob in pool.
        MetaData::DataBlob *pData);

    __checkReturn
    HRESULT GetBlobWithSizePrefix(
        UINT32              nOffset,    // Offset of blob in pool.
        MetaData::DataBlob *pData);

//*****************************************************************************
// Turn hashing off or on.  If you turn hashing on, then any existing data is
// thrown away and all data is rehashed during this call.
//*****************************************************************************
    __checkReturn
    virtual HRESULT SetHash(int bHash);

//*****************************************************************************
// Get the size of the blob obtained from the pool.
// Needed for generic persisting of data blocks.
//*****************************************************************************
    virtual ULONG GetSizeOfData( void const * data )
    {
        WRAPPER_NO_CONTRACT;

        void const * blobdata = 0;
        ULONG blobsize = CPackedLen::GetLength( data, & blobdata ); // the size is encoded at the beginning of the block
        return blobsize + static_cast< ULONG >( reinterpret_cast< BYTE const * >( blobdata ) - reinterpret_cast< BYTE const * >( data ) );
    }

//*****************************************************************************
// How many objects are there in the pool?  If the count is 0, you don't need
// to persist anything at all to disk.
//*****************************************************************************
    int Count()
    { WRAPPER_NO_CONTRACT; return (m_Hash.Count()); }

//*****************************************************************************
// String heap is considered empty if the only thing it has is the initial
// empty string, or if after organization, there are no strings.
//*****************************************************************************
    virtual int IsEmpty()                    // true if empty.
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        return (GetNextOffset() <= 1);
    }

//*****************************************************************************
// Return the size in bytes of the persistent version of this pool.  If
// PersistToStream were the next call, the amount of bytes written to pIStream
// has to be same as the return value from this function.
//*****************************************************************************
    __checkReturn
    virtual HRESULT GetSaveSize(
        UINT32 *pcbSaveSize) const
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        return StgPool::GetSaveSize(pcbSaveSize);
    }

protected:

//*****************************************************************************
// Check whether a given offset is valid in the pool.
//*****************************************************************************
    virtual int IsValidOffset(UINT32 nOffset)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        MetaData::DataBlob data;
        return (StgBlobPool::GetBlob(nOffset, &data) == S_OK);
    }

private:
    __checkReturn
    HRESULT RehashBlobs();

    DAC_ALIGNAS(StgPool) // Align first member to alignment of base class
    CBlobPoolHash m_Hash;                    // Hash table for lookups.
};  // class StgBlobPool

#ifdef _MSC_VER
#pragma warning (default : 4355)
#endif

//*****************************************************************************
// Unfortunately the CreateStreamOnHGlobal is a little too smart in that
// it gets its size from GlobalSize.  This means that even if you give it the
// memory for the stream, it has to be globally allocated.  We don't want this
// because we have the stream read only in the middle of a memory mapped file.
// CreateStreamOnMemory and the corresponding, internal only stream object solves
// that problem.
//*****************************************************************************
class CInMemoryStream : public IStream
{
public:
    CInMemoryStream() :
        m_pMem(0),
        m_cbSize(0),
        m_cbCurrent(0),
        m_cRef(1),
        m_dataCopy(NULL)
    { LIMITED_METHOD_CONTRACT; }

    virtual ~CInMemoryStream() {}

    void InitNew(
        void        *pMem,
        ULONG       cbSize)
    {
        LIMITED_METHOD_CONTRACT;

        m_pMem = pMem;
        m_cbSize = cbSize;
        m_cbCurrent = 0;
    }

    ULONG STDMETHODCALLTYPE AddRef() {
        LIMITED_METHOD_CONTRACT;
        return InterlockedIncrement(&m_cRef);
    }


    ULONG STDMETHODCALLTYPE Release();

    __checkReturn
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, PVOID *ppOut);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Read(void *pv, ULONG cb, ULONG *pcbRead);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Write(const void  *pv, ULONG cb, ULONG *pcbWritten);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Seek(LARGE_INTEGER dlibMove,DWORD dwOrigin, ULARGE_INTEGER *plibNewPosition);

    __checkReturn
    HRESULT STDMETHODCALLTYPE SetSize(ULARGE_INTEGER libNewSize)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

        return (BadError(E_NOTIMPL));
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE CopyTo(
        IStream     *pstm,
        ULARGE_INTEGER cb,
        ULARGE_INTEGER *pcbRead,
        ULARGE_INTEGER *pcbWritten);

    __checkReturn
    HRESULT STDMETHODCALLTYPE Commit(
        DWORD       grfCommitFlags)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

        return (BadError(E_NOTIMPL));
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE Revert()
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

        return (BadError(E_NOTIMPL));
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE LockRegion(
        ULARGE_INTEGER libOffset,
        ULARGE_INTEGER cb,
        DWORD       dwLockType)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

        return (BadError(E_NOTIMPL));
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE UnlockRegion(
        ULARGE_INTEGER libOffset,
        ULARGE_INTEGER cb,
        DWORD       dwLockType)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

        return (BadError(E_NOTIMPL));
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE Stat(
        STATSTG     *pstatstg,
        DWORD       grfStatFlag)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

        pstatstg->cbSize.QuadPart = m_cbSize;
        return (S_OK);
    }

    __checkReturn
    HRESULT STDMETHODCALLTYPE Clone(
        IStream     **ppstm)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FAULT; //E_OUTOFMEMORY;

        return (BadError(E_NOTIMPL));
    }

    __checkReturn
    static HRESULT CreateStreamOnMemory(           // Return code.
                                 void        *pMem,                  // Memory to create stream on.
                                 ULONG       cbSize,                 // Size of data.
                                 IStream     **ppIStream,            // Return stream object here.
                                 BOOL        fDeleteMemoryOnRelease = FALSE
                                 );

    __checkReturn
    static HRESULT CreateStreamOnMemoryCopy(
                                 void        *pMem,
                                 ULONG       cbSize,
                                 IStream     **ppIStream);

private:
    void        *m_pMem;                // Memory for the read.
    ULONG       m_cbSize;               // Size of the memory.
    ULONG       m_cbCurrent;            // Current offset.
    LONG        m_cRef;                 // Ref count.
    BYTE       *m_dataCopy;             // Optional copy of the data.
};  // class CInMemoryStream

//*****************************************************************************
// CGrowableStream is a simple IStream implementation that grows as
// its written to. All the memory is contigious, so read access is
// fast. A grow does a realloc, so be aware of that if you're going to
// use this.
//*****************************************************************************

// DPTR instead of VPTR because we don't actually call any of the virtuals.
typedef DPTR(class CGrowableStream) PTR_CGrowableStream;

class CGrowableStream : public IStream
{
public:
    //Constructs a new GrowableStream
    // multiplicativeGrowthRate - when the stream grows it will be at least this
    //   multiple of its old size. Values greater than 1 ensure O(N) amortized
    //   performance growing the stream to size N, 1 ensures O(N^2) amortized perf
    //   but gives the tightest memory usage. Valid range is [1.0, 2.0].
    // additiveGrowthRate - when the stream grows it will increase in size by at least
    //   this number of bytes. Larger numbers cause fewer re-allocations at the cost of
    //   increased memory usage.
    CGrowableStream(float multiplicativeGrowthRate = 2.0, DWORD additiveGrowthRate = 4096);

#ifndef DACCESS_COMPILE
    virtual ~CGrowableStream();
#endif

    // Expose the total raw buffer.
    // This can be used by DAC to get the raw contents.
    // This becomes potentiallyinvalid on the next call on the class, because the underlying storage can be
    // reallocated.
    MemoryRange GetRawBuffer() const
    {
        SUPPORTS_DAC;
        PTR_VOID p = m_swBuffer;
        return MemoryRange(p, m_dwBufferSize);
    }

private:
    // Raw pointer to buffer. This may change as the buffer grows and gets reallocated.
    PTR_BYTE  m_swBuffer;

    // Total size of the buffer in bytes.
    DWORD   m_dwBufferSize;

    // Current index in the buffer. This can be moved around by Seek.
    DWORD   m_dwBufferIndex;

    // Logical length of the stream
    DWORD   m_dwStreamLength;

    // Reference count
    LONG    m_cRef;

    // growth rate parameters determine new stream size when it must grow
    float   m_multiplicativeGrowthRate;
    int     m_additiveGrowthRate;

    // Ensures the stream is physically and logically at least newLogicalSize
    // in size
    HRESULT EnsureCapacity(DWORD newLogicalSize);

    // IStream methods
public:

#ifndef DACCESS_COMPILE
    ULONG STDMETHODCALLTYPE AddRef() {
        LIMITED_METHOD_CONTRACT;
        return InterlockedIncrement(&m_cRef);
    }


    ULONG STDMETHODCALLTYPE Release();

    __checkReturn
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, PVOID *ppOut);

    STDMETHOD(Read)(
         void * pv,
         ULONG cb,
         ULONG * pcbRead);

    STDMETHOD(Write)(
         const void * pv,
         ULONG cb,
         ULONG * pcbWritten);

    STDMETHOD(Seek)(
         LARGE_INTEGER dlibMove,
         DWORD dwOrigin,
         ULARGE_INTEGER * plibNewPosition);

    STDMETHOD(SetSize)(ULARGE_INTEGER libNewSize);

    STDMETHOD(CopyTo)(
         IStream * pstm,
         ULARGE_INTEGER cb,
         ULARGE_INTEGER * pcbRead,
         ULARGE_INTEGER * pcbWritten) { STATIC_CONTRACT_NOTHROW; STATIC_CONTRACT_FAULT; return E_NOTIMPL; }

    STDMETHOD(Commit)(
         DWORD grfCommitFlags) { STATIC_CONTRACT_NOTHROW; STATIC_CONTRACT_FAULT; return NOERROR; }

    STDMETHOD(Revert)( void) {STATIC_CONTRACT_NOTHROW; STATIC_CONTRACT_FAULT;  return E_NOTIMPL; }

    STDMETHOD(LockRegion)(
         ULARGE_INTEGER libOffset,
         ULARGE_INTEGER cb,
         DWORD dwLockType) { STATIC_CONTRACT_NOTHROW; STATIC_CONTRACT_FAULT; return E_NOTIMPL; }

    STDMETHOD(UnlockRegion)(
         ULARGE_INTEGER libOffset,
         ULARGE_INTEGER cb,
         DWORD dwLockType) {STATIC_CONTRACT_NOTHROW; STATIC_CONTRACT_FAULT;   return E_NOTIMPL; }

    STDMETHOD(Stat)(
         STATSTG * pstatstg,
         DWORD grfStatFlag);

    // Make a deep copy of the stream into a new CGrowableStream instance
    STDMETHOD(Clone)(
         IStream ** ppstm);

#endif // DACCESS_COMPILE
}; // class CGrowableStream

#endif // __StgPool_h__
