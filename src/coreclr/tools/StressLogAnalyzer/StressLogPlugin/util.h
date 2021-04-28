// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

template<typename T>
struct Volatile
{
    T t;
    T Load() { return t; }
};

typedef void* CRITSEC_COOKIE;

#define STRESS_LOG_ANALYZER

#include <malloc.h>
#include "staticcontract.h"

// This macro is used to standardize the wide character string literals between UNIX and Windows.
// Unix L"" is UTF32, and on windows it's UTF16.  Because of built-in assumptions on the size
// of string literals, it's important to match behaviour between Unix and Windows.  Unix will be defined
// as u"" (char16_t)
#ifdef PLATFORM_UNIX
#define W(str)  u##str
#else // PLATFORM_UNIX
#define W(str)  L##str
#endif // PLATFORM_UNIX

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
template <DWORD SIZE, DWORD INCREMENT>
class CQuickBytesBase
{
public:
    CQuickBytesBase() :
        pbBuff(0),
        iSize(0),
        cbTotal(SIZE)
    { }

    void Destroy()
    {
        if (pbBuff)
        {
            delete[](BYTE*)pbBuff;
            pbBuff = 0;
        }
    }

    void* Alloc(SIZE_T iItems)
    {
        iSize = iItems;
        if (iItems <= SIZE)
        {
            cbTotal = SIZE;
            return (&rgData[0]);
        }
        else
        {
            if (pbBuff)
                delete[](BYTE*)pbBuff;
            pbBuff = new BYTE[iItems];
            cbTotal = pbBuff ? iItems : 0;
            return (pbBuff);
        }
    }

    // This is for conformity to the CQuickBytesBase that is defined by the runtime so
    // that we can use it inside of some GC code that SOS seems to include as well.
    //
    // The plain vanilla "Alloc" version on this CQuickBytesBase doesn't throw either,
    // so we'll just forward the call.
    void* AllocNoThrow(SIZE_T iItems)
    {
        return Alloc(iItems);
    }

    HRESULT ReSize(SIZE_T iItems)
    {
        void* pbBuffNew;
        if (iItems <= cbTotal)
        {
            iSize = iItems;
            return NOERROR;
        }

        pbBuffNew = new BYTE[iItems + INCREMENT];
        if (!pbBuffNew)
            return E_OUTOFMEMORY;
        if (pbBuff)
        {
            memcpy(pbBuffNew, pbBuff, cbTotal);
            delete[](BYTE*)pbBuff;
        }
        else
        {
            _ASSERTE(cbTotal == SIZE);
            memcpy(pbBuffNew, rgData, SIZE);
        }
        cbTotal = iItems + INCREMENT;
        iSize = iItems;
        pbBuff = pbBuffNew;
        return NOERROR;

    }

    operator PVOID()
    {
        return ((pbBuff) ? pbBuff : &rgData[0]);
    }

    void* Ptr()
    {
        return ((pbBuff) ? pbBuff : &rgData[0]);
    }

    SIZE_T Size()
    {
        return (iSize);
    }

    SIZE_T MaxSize()
    {
        return (cbTotal);
    }

    void* pbBuff;
    SIZE_T      iSize;              // number of bytes used
    SIZE_T      cbTotal;            // total bytes allocated in the buffer
    // use UINT64 to enforce the alignment of the memory
    UINT64 rgData[(SIZE + sizeof(UINT64) - 1) / sizeof(UINT64)];
};

#define     CQUICKBYTES_BASE_SIZE           512
#define     CQUICKBYTES_INCREMENTAL_SIZE    128

class CQuickBytesNoDtor : public CQuickBytesBase<CQUICKBYTES_BASE_SIZE, CQUICKBYTES_INCREMENTAL_SIZE>
{
};

class CQuickBytes : public CQuickBytesNoDtor
{
public:
    CQuickBytes() { }

    ~CQuickBytes()
    {
        Destroy();
    }
};
