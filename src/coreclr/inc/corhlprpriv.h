// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** Corhlprpriv.h -                                                                 **
 **                                                                         **
 *****************************************************************************/

#ifndef __CORHLPRPRIV_H__
#define __CORHLPRPRIV_H__

#include "corhlpr.h"
#include "fstring.h"

#if defined(_MSC_VER) && defined(HOST_X86)
#pragma optimize("y", on)		// If routines don't get inlined, don't pay the EBP frame penalty
#endif

//*****************************************************************************
//
//***** Utility helpers
//
//*****************************************************************************

#ifndef SOS_INCLUDE

//*****************************************************************************
//
// **** CQuickBytes
// This helper class is useful for cases where 90% of the time you allocate 512
// or less bytes for a data structure.  This class contains a 512 byte buffer.
// Alloc() will return a pointer to this buffer if your allocation is small
// enough, otherwise it asks the heap for a larger buffer which is freed for
// you.  No mutex locking is required for the small allocation case, making the
// code run faster, less heap fragmentation, etc...  Each instance will allocate
// 520 bytes, so use accordinly.
//
//*****************************************************************************
namespace NSQuickBytesHelper
{
    template <BOOL bThrow>
    struct _AllocBytes;

    template <>
    struct _AllocBytes<TRUE>
    {
        static BYTE *Invoke(SIZE_T iItems)
        {
            return NEW_THROWS(iItems);
        }
    };

    template <>
    struct _AllocBytes<FALSE>
    {
        static BYTE *Invoke(SIZE_T iItems)
        {
            return NEW_NOTHROW(iItems);
        }
    };
};

void DECLSPEC_NORETURN ThrowHR(HRESULT hr);

template <SIZE_T SIZE, SIZE_T INCREMENT>
class CQuickMemoryBase
{
protected:
    template <typename ELEM_T>
    static ELEM_T Min(ELEM_T a, ELEM_T b)
        { return a < b ? a : b; }

    template <typename ELEM_T>
    static ELEM_T Max(ELEM_T a, ELEM_T b)
        { return a < b ? b : a; }

    // bGrow  - indicates that this is a resize and that the original data
    //          needs to be copied over.
    // bThrow - indicates whether or not memory allocations will throw.
    template <BOOL bGrow, BOOL bThrow>
    void *_Alloc(SIZE_T iItems)
    {
#if defined(_BLD_CLR) && defined(_DEBUG)
        {  // Exercise heap for OOM-fault injection purposes
            BYTE * pb = NSQuickBytesHelper::_AllocBytes<bThrow>::Invoke(iItems);
            _ASSERTE(!bThrow || pb != NULL); // _AllocBytes would have thrown if bThrow == TRUE
            if (pb == NULL) return NULL; // bThrow == FALSE and we failed to allocate memory
            delete [] pb; // Success, delete allocated memory.
        }
#endif
        if (iItems <= cbTotal)
        {   // Fits within existing memory allocation
            iSize = iItems;
        }
        else if (iItems <= SIZE)
        {   // Will fit in internal buffer.
            if (pbBuff == NULL)
            {   // Any previous allocation is in the internal buffer and the new
                // allocation fits in the internal buffer, so just update the size.
                iSize = iItems;
                cbTotal = SIZE;
            }
            else
            {   // There was a previous allocation, sitting in pbBuff
                if (bGrow)
                {   // If growing, need to copy any existing data over.
                    memcpy(&rgData[0], pbBuff, Min(cbTotal, SIZE));
                }

                delete [] pbBuff;
                pbBuff = NULL;
                iSize = iItems;
                cbTotal = SIZE;
            }
        }
        else
        {   // Need to allocate a new buffer
            SIZE_T cbTotalNew = iItems + (bGrow ? INCREMENT : 0);
            BYTE * pbBuffNew = NSQuickBytesHelper::_AllocBytes<bThrow>::Invoke(cbTotalNew);

            if (!bThrow && pbBuffNew == NULL)
            {   // Allocation failed. Zero out structure.
                if (pbBuff != NULL)
                {   // Delete old buffer
                    delete [] pbBuff;
                }
                pbBuff = NULL;
                iSize = 0;
                cbTotal = 0;
                return NULL;
            }

            if (bGrow && cbTotal > 0)
            {   // If growing, need to copy any existing data over.
                memcpy(pbBuffNew, (BYTE *)Ptr(), Min(cbTotal, cbTotalNew));
            }

            if (pbBuff != NULL)
            {   // Delete old pre-existing buffer
                delete [] pbBuff;
                pbBuff = NULL;
            }

            pbBuff = pbBuffNew;
            cbTotal = cbTotalNew;
            iSize = iItems;
        }

        return Ptr();
    }

public:
    void Init()
    {
        pbBuff = 0;
        iSize = 0;
        cbTotal = SIZE;
    }

    void Destroy()
    {
        if (pbBuff)
        {
            delete [] pbBuff;
            pbBuff = 0;
        }
    }

    void *AllocThrows(SIZE_T iItems)
    {
        return _Alloc<FALSE /*bGrow*/, TRUE /*bThrow*/>(iItems);
    }

    void *AllocNoThrow(SIZE_T iItems)
    {
        return _Alloc<FALSE /*bGrow*/, FALSE /*bThrow*/>(iItems);
    }

    void ReSizeThrows(SIZE_T iItems)
    {
        _Alloc<TRUE /*bGrow*/, TRUE /*bThrow*/>(iItems);
    }

#ifdef __GNUC__
    // This makes sure that we will not get an undefined symbol
    // when building a release version of libcoreclr using LLVM/GCC.
    __attribute__((used))
#endif // __GNUC__
    HRESULT ReSizeNoThrow(SIZE_T iItems);

    void Shrink(SIZE_T iItems)
    {
        _ASSERTE(iItems <= cbTotal);
        iSize = iItems;
    }

    operator PVOID()
    {
        return ((pbBuff) ? pbBuff : (PVOID)&rgData[0]);
    }

    void *Ptr()
    {
        return ((pbBuff) ? pbBuff : (PVOID)&rgData[0]);
    }

    const void *Ptr() const
    {
        return ((pbBuff) ? pbBuff : (PVOID)&rgData[0]);
    }

    SIZE_T Size() const
    {
        return (iSize);
    }

    SIZE_T MaxSize() const
    {
        return (cbTotal);
    }

    void Maximize()
    {
        iSize = cbTotal;
    }


    // Convert UTF8 string to UNICODE string, optimized for speed
    HRESULT ConvertUtf8_UnicodeNoThrow(const char * utf8str)
    {
        bool allAscii;
        DWORD length;

        HRESULT hr = FString::Utf8_Unicode_Length(utf8str, & allAscii, & length);

        if (SUCCEEDED(hr))
        {
            LPWSTR buffer = (LPWSTR) AllocNoThrow((length + 1) * sizeof(WCHAR));

            if (buffer == NULL)
            {
                hr = E_OUTOFMEMORY;
            }
            else
            {
                hr = FString::Utf8_Unicode(utf8str, allAscii, buffer, length);
            }
        }

        return hr;
    }

    // Convert UTF8 string to UNICODE string, optimized for speed
    void ConvertUtf8_Unicode(const char * utf8str)
    {
        bool allAscii;
        DWORD length;

        HRESULT hr = FString::Utf8_Unicode_Length(utf8str, & allAscii, & length);

        if (SUCCEEDED(hr))
        {
            LPWSTR buffer = (LPWSTR) AllocThrows((length + 1) * sizeof(WCHAR));

            hr = FString::Utf8_Unicode(utf8str, allAscii, buffer, length);
        }

        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
    }

    // Convert UNICODE string to UTF8 string, optimized for speed
    void ConvertUnicode_Utf8(const WCHAR * pString)
    {
        bool allAscii;
        DWORD length;

        HRESULT hr = FString::Unicode_Utf8_Length(pString, & allAscii, & length);

        if (SUCCEEDED(hr))
        {
            LPSTR buffer = (LPSTR) AllocThrows((length + 1) * sizeof(char));

            hr = FString::Unicode_Utf8(pString, allAscii, buffer, length);
        }

        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
    }

    // Copy single byte string and hold it
    const char * SetStringNoThrow(const char * pStr, SIZE_T len)
    {
        LPSTR buffer = (LPSTR) AllocNoThrow(len + 1);

        if (buffer != NULL)
        {
            memcpy(buffer, pStr, len);
            buffer[len] = 0;
        }

        return buffer;
    }

#ifdef DACCESS_COMPILE
    void
    EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        // Assume that 'this' is enumerated, either explicitly
        // or because this class is embedded in another.
        DacEnumMemoryRegion(dac_cast<TADDR>(pbBuff), iSize);
    }
#endif // DACCESS_COMPILE

    BYTE       *pbBuff;
    SIZE_T      iSize;              // number of bytes used
    SIZE_T      cbTotal;            // total bytes allocated in the buffer
    // use UINT64 to enforce the alignment of the memory
    UINT64 rgData[(SIZE+sizeof(UINT64)-1)/sizeof(UINT64)];
};

// These should be multiples of 8 so that data can be naturally aligned.
#define     CQUICKBYTES_BASE_SIZE           512
#define     CQUICKBYTES_INCREMENTAL_SIZE    128

class CQuickBytesBase : public CQuickMemoryBase<CQUICKBYTES_BASE_SIZE, CQUICKBYTES_INCREMENTAL_SIZE>
{
};


class CQuickBytes : public CQuickBytesBase
{
public:
    CQuickBytes()
    {
        Init();
    }

    ~CQuickBytes()
    {
        Destroy();
    }
};

/* to be used as static variable - no constructor/destructor, assumes zero
   initialized memory */
class CQuickBytesStatic : public CQuickBytesBase
{
};

template <SIZE_T CQUICKBYTES_BASE_SPECIFY_SIZE>
class CQuickBytesSpecifySizeBase : public CQuickMemoryBase<CQUICKBYTES_BASE_SPECIFY_SIZE, CQUICKBYTES_INCREMENTAL_SIZE>
{
};

template <SIZE_T CQUICKBYTES_BASE_SPECIFY_SIZE>
class CQuickBytesSpecifySize : public CQuickBytesSpecifySizeBase<CQUICKBYTES_BASE_SPECIFY_SIZE>
{
public:
    CQuickBytesSpecifySize()
    {
        this->Init();
    }

    ~CQuickBytesSpecifySize()
    {
        this->Destroy();
    }
};

/* to be used as static variable - no constructor/destructor, assumes zero
   initialized memory */
template <SIZE_T CQUICKBYTES_BASE_SPECIFY_SIZE>
class CQuickBytesSpecifySizeStatic : public CQuickBytesSpecifySizeBase<CQUICKBYTES_BASE_SPECIFY_SIZE>
{
};

template <class T> class CQuickArrayBase : public CQuickBytesBase
{
public:
    T* AllocThrows(SIZE_T iItems)
    {
        CheckOverflowThrows(iItems);
        return (T*)CQuickBytesBase::AllocThrows(iItems * sizeof(T));
    }

    void ReSizeThrows(SIZE_T iItems)
    {
        CheckOverflowThrows(iItems);
        CQuickBytesBase::ReSizeThrows(iItems * sizeof(T));
    }

    T* AllocNoThrow(SIZE_T iItems)
    {
        if (!CheckOverflowNoThrow(iItems))
        {
            return NULL;
        }
        return (T*)CQuickBytesBase::AllocNoThrow(iItems * sizeof(T));
    }

    HRESULT ReSizeNoThrow(SIZE_T iItems)
    {
        if (!CheckOverflowNoThrow(iItems))
        {
            return E_OUTOFMEMORY;
        }
        return CQuickBytesBase::ReSizeNoThrow(iItems * sizeof(T));
    }

    void Shrink(SIZE_T iItems)
    {
        CQuickBytesBase::Shrink(iItems * sizeof(T));
    }

    T* Ptr()
    {
        return (T*) CQuickBytesBase::Ptr();
    }

    const T* Ptr() const
    {
        return (T*) CQuickBytesBase::Ptr();
    }

    SIZE_T Size() const
    {
        return CQuickBytesBase::Size() / sizeof(T);
    }

    SIZE_T MaxSize() const
    {
        return CQuickBytesBase::cbTotal / sizeof(T);
    }

    T& operator[] (SIZE_T ix)
    {
        _ASSERTE(ix < Size());
        return *(Ptr() + ix);
    }

    const T& operator[] (SIZE_T ix) const
    {
        _ASSERTE(ix < Size());
        return *(Ptr() + ix);
    }

private:
    inline
    BOOL CheckOverflowNoThrow(SIZE_T iItems)
    {
        SIZE_T totalSize = iItems * sizeof(T);

        if (totalSize / sizeof(T) != iItems)
        {
            return FALSE;
        }

        return TRUE;
    }

    inline
    void CheckOverflowThrows(SIZE_T iItems)
    {
        if (!CheckOverflowNoThrow(iItems))
        {
            THROW_OUT_OF_MEMORY();
        }
    }
};

template <class T> class CQuickArray : public CQuickArrayBase<T>
{
public:
    CQuickArray<T>()
    {
        this->Init();
    }

    ~CQuickArray<T>()
    {
        this->Destroy();
    }
};

// This is actually more of a stack with array access. Essentially, you can
// only add elements through Push and remove them through Pop, but you can
// access and modify any random element with the index operator. You cannot
// access elements that have not been added.

template <class T>
class CQuickArrayList : protected CQuickArray<T>
{
private:
    SIZE_T m_curSize;

public:
    // Make these specific functions public.
    using CQuickArray<T>::AllocThrows;
    using CQuickArray<T>::ReSizeThrows;
    using CQuickArray<T>::AllocNoThrow;
    using CQuickArray<T>::ReSizeNoThrow;
    using CQuickArray<T>::MaxSize;
    using CQuickArray<T>::Ptr;

    CQuickArrayList()
        : m_curSize(0)
    {
        this->Init();
    }

    ~CQuickArrayList()
    {
        this->Destroy();
    }

    // Can only access values that have been pushed.
    T& operator[] (SIZE_T ix)
    {
        _ASSERTE(ix < m_curSize);
        return CQuickArray<T>::operator[](ix);
    }

    // Can only access values that have been pushed.
    const T& operator[] (SIZE_T ix) const
    {
        _ASSERTE(ix < m_curSize);
        return CQuickArray<T>::operator[](ix);
    }

    // THROWS: Resizes if necessary.
    void Push(const T & value)
    {
        // Resize if necessary - thows.
        if (m_curSize + 1 >= CQuickArray<T>::Size())
            ReSizeThrows((m_curSize + 1) * 2);

        // Append element to end of array.
        _ASSERTE(m_curSize + 1 < CQuickArray<T>::Size());
        SIZE_T ix = m_curSize++;
        (*this)[ix] = value;
    }

    // NOTHROW: Resizes if necessary.
    BOOL PushNoThrow(const T & value)
    {
        // Resize if necessary - nothow.
        if (m_curSize + 1 >= CQuickArray<T>::Size()) {
            if (ReSizeNoThrow((m_curSize + 1) * 2) != NOERROR)
                return FALSE;
        }

        // Append element to end of array.
        _ASSERTE(m_curSize + 1 < CQuickArray<T>::Size());
        SIZE_T ix = m_curSize++;
        (*this)[ix] = value;
        return TRUE;
    }

    T Pop()
    {
        _ASSERTE(m_curSize > 0);
        T retval = (*this)[m_curSize - 1];
        INDEBUG(ZeroMemory(&(this->Ptr()[m_curSize - 1]), sizeof(T));)
        --m_curSize;
        return retval;
    }

    SIZE_T Size() const
    {
        return m_curSize;
    }

    void Shrink()
    {
        CQuickArray<T>::Shrink(m_curSize);
    }
};


/* to be used as static variable - no constructor/destructor, assumes zero
   initialized memory */
template <class T> class CQuickArrayStatic : public CQuickArrayBase<T>
{
};

typedef CQuickArrayBase<WCHAR> CQuickWSTRBase;
typedef CQuickArray<WCHAR> CQuickWSTR;
typedef CQuickArrayStatic<WCHAR> CQuickWSTRStatic;

typedef CQuickArrayBase<CHAR> CQuickSTRBase;
typedef CQuickArray<CHAR> CQuickSTR;
typedef CQuickArrayStatic<CHAR> CQuickSTRStatic;

class RidBitmap
{
public:
    HRESULT InsertToken(mdToken token)
    {
        HRESULT  hr     = S_OK;
        mdToken  rid    = RidFromToken(token);
        SIZE_T   index  = rid / 8;
        BYTE     bit    = (BYTE)(1 << (rid % 8));

        if (index >= buffer.Size())
        {
            SIZE_T oldSize = buffer.Size();
            SIZE_T newSize = index+1+oldSize/8;
            IfFailRet(buffer.ReSizeNoThrow(newSize));
            memset(&buffer[oldSize], 0, newSize-oldSize);
        }

        buffer[index] |= bit;
        return hr;
    }

    bool IsTokenInBitmap(mdToken token)
    {
        mdToken rid   = RidFromToken(token);
        SIZE_T  index = rid / 8;
        BYTE    bit   = (BYTE)(1 << (rid % 8));

        return ((index < buffer.Size()) && (buffer[index] & bit));
    }

    void Reset()
    {
        if (buffer.Size())
        {
            memset(&buffer[0], 0, buffer.Size());
        }
    }

private:
    CQuickArray<BYTE> buffer;
};

//*****************************************************************************
//
//***** Signature helpers
//
//*****************************************************************************

HRESULT _CountBytesOfOneArg(
    PCCOR_SIGNATURE pbSig,
    ULONG       *pcbTotal);

HRESULT _GetFixedSigOfVarArg(           // S_OK or error.
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob of CLR signature
    ULONG   cbSigBlob,                  // [IN] size of signature
    CQuickBytes *pqbSig,                // [OUT] output buffer for fixed part of VarArg Signature
    ULONG   *pcbSigBlob);               // [OUT] number of bytes written to the above output buffer

#endif //!SOS_INCLUDE

#if defined(_MSC_VER) && defined(TARGET_X86)
#pragma optimize("", on)		// restore command line default optimizations
#endif


//---------------------------------------------------------------------------------------
//
// Reads compressed integer from buffer pData, fills the result to *pnDataOut. Advances buffer pointer.
// Doesn't read behind the end of the buffer (the end starts at pDataEnd).
//
inline
__checkReturn
HRESULT
CorSigUncompressData_EndPtr(
    PCCOR_SIGNATURE & pData,        // [IN,OUT] Buffer
    PCCOR_SIGNATURE   pDataEnd,     // End of buffer
    DWORD *           pnDataOut)    // [OUT] Compressed integer read from the buffer
{
    _ASSERTE(pData <= pDataEnd);
    HRESULT hr = S_OK;

    INT_PTR cbDataSize = pDataEnd - pData;
    if (cbDataSize > 4)
    {   // Compressed integer cannot be bigger than 4 bytes
        cbDataSize = 4;
    }
    DWORD dwDataSize = (DWORD)cbDataSize;

    ULONG cbDataOutLength;
    IfFailRet(CorSigUncompressData(
        pData,
        dwDataSize,
        pnDataOut,
        &cbDataOutLength));
    pData += cbDataOutLength;

    return hr;
} // CorSigUncompressData_EndPtr

//---------------------------------------------------------------------------------------
//
// Reads CorElementType (1 byte) from buffer pData, fills the result to *pTypeOut. Advances buffer pointer.
// Doesn't read behind the end of the buffer (the end starts at pDataEnd).
//
inline
__checkReturn
HRESULT
CorSigUncompressElementType_EndPtr(
    PCCOR_SIGNATURE & pData,    // [IN,OUT] Buffer
    PCCOR_SIGNATURE   pDataEnd, // End of buffer
    CorElementType *  pTypeOut) // [OUT] ELEMENT_TYPE_* value read from the buffer
{
    _ASSERTE(pData <= pDataEnd);
    // We don't expect pData > pDataEnd, but the runtime check doesn't cost much and it is more secure in
    // case caller has a bug
    if (pData >= pDataEnd)
    {   // No data
        return META_E_BAD_SIGNATURE;
    }
    // Read 'type' as 1 byte
    *pTypeOut = (CorElementType)*pData;
    pData++;

    return S_OK;
} // CorSigUncompressElementType_EndPtr

//---------------------------------------------------------------------------------------
//
// Reads pointer (4/8 bytes) from buffer pData, fills the result to *ppvPointerOut. Advances buffer pointer.
// Doesn't read behind the end of the buffer (the end starts at pDataEnd).
//
inline
__checkReturn
HRESULT
CorSigUncompressPointer_EndPtr(
    PCCOR_SIGNATURE & pData,            // [IN,OUT] Buffer
    PCCOR_SIGNATURE   pDataEnd,         // End of buffer
    void **           ppvPointerOut)    // [OUT] Pointer value read from the buffer
{
    _ASSERTE(pData <= pDataEnd);
    // We could just skip this check as pointers should be only in trusted (and therefore correct)
    // signatures and we check for that on the caller side, but it won't hurt to have this check and it will
    // make it easier to catch invalid signatures in trusted code (e.g. IL stubs, NGEN images, etc.)
    if (pData + sizeof(void *) > pDataEnd)
    {   // Not enough data in the buffer
        _ASSERTE(!"This signature is invalid. Note that caller should check that it is not coming from untrusted source!");
        return META_E_BAD_SIGNATURE;
    }
    *ppvPointerOut = *(void * UNALIGNED *)pData;
    pData += sizeof(void *);

    return S_OK;
} // CorSigUncompressPointer_EndPtr

//---------------------------------------------------------------------------------------
//
// Reads compressed TypeDef/TypeRef/TypeSpec token, fills the result to *pnDataOut. Advances buffer pointer.
// Doesn't read behind the end of the buffer (the end starts at pDataEnd).
//
inline
__checkReturn
HRESULT
CorSigUncompressToken_EndPtr(
    PCCOR_SIGNATURE & pData,        // [IN,OUT] Buffer
    PCCOR_SIGNATURE   pDataEnd,     // End of buffer
    mdToken *         ptkTokenOut)  // [OUT] Token read from the buffer
{
    _ASSERTE(pData <= pDataEnd);
    HRESULT hr = S_OK;

    INT_PTR cbDataSize = pDataEnd - pData;
    if (cbDataSize > 4)
    {   // Compressed token cannot be bigger than 4 bytes
        cbDataSize = 4;
    }
    DWORD dwDataSize = (DWORD)cbDataSize;

    uint32_t cbTokenOutLength;
    IfFailRet(CorSigUncompressToken(
        pData,
        dwDataSize,
        ptkTokenOut,
        &cbTokenOutLength));
    pData += cbTokenOutLength;

    return hr;
} // CorSigUncompressToken_EndPtr

#endif // __CORHLPRPRIV_H__
