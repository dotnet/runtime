// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef QUICKBYTES_H_
#define QUICKBYTES_H_

#include "cor.h"
#include <cassert>
#include <cstring>
#include <new>

// Keep the buffer layout shared with CoreCLR; runtime-dependent operations
// are defined in corhlprpriv.h rather than required by portable users.
namespace NSQuickBytesHelper
{
    template <BOOL bThrow>
    struct _AllocBytes;

    template <>
    struct _AllocBytes<TRUE>
    {
        static BYTE* Invoke(SIZE_T iItems)
        {
            return new BYTE[iItems];
        }
    };

    template <>
    struct _AllocBytes<FALSE>
    {
        static BYTE* Invoke(SIZE_T iItems)
        {
            return new (std::nothrow) BYTE[iItems];
        }
    };
}

template <SIZE_T SIZE, SIZE_T INCREMENT>
class CQuickMemoryBase
{
protected:
    template <typename ELEM_T>
    static ELEM_T Min(ELEM_T a, ELEM_T b)
    {
        return a < b ? a : b;
    }

    template <typename ELEM_T>
    static ELEM_T Max(ELEM_T a, ELEM_T b)
    {
        return a < b ? b : a;
    }

    template <BOOL bGrow, BOOL bThrow>
    void* _Alloc(SIZE_T iItems)
    {
#if defined(_DEBUG)
        {
            BYTE* pb = NSQuickBytesHelper::_AllocBytes<bThrow>::Invoke(iItems);
#ifdef _ASSERTE
            _ASSERTE(!bThrow || pb != nullptr);
#else
            assert(!bThrow || pb != nullptr);
#endif
            if (pb == nullptr)
                return nullptr;
            delete[] pb;
        }
#endif
        if (iItems <= cbTotal)
        {
            iSize = iItems;
        }
        else if (iItems <= SIZE)
        {
            if (pbBuff == nullptr)
            {
                iSize = iItems;
                cbTotal = SIZE;
            }
            else
            {
                if (bGrow)
                    std::memcpy(&rgData[0], pbBuff, Min(cbTotal, SIZE));

                delete[] pbBuff;
                pbBuff = nullptr;
                iSize = iItems;
                cbTotal = SIZE;
            }
        }
        else
        {
            SIZE_T cbTotalNew = iItems + (bGrow ? INCREMENT : 0);
            BYTE* pbBuffNew = NSQuickBytesHelper::_AllocBytes<bThrow>::Invoke(cbTotalNew);

            if (!bThrow && pbBuffNew == nullptr)
            {
                delete[] pbBuff;
                pbBuff = nullptr;
                iSize = 0;
                cbTotal = 0;
                return nullptr;
            }

            if (bGrow && cbTotal > 0)
                std::memcpy(pbBuffNew, Ptr(), Min(cbTotal, cbTotalNew));

            delete[] pbBuff;
            pbBuff = pbBuffNew;
            cbTotal = cbTotalNew;
            iSize = iItems;
        }

        return Ptr();
    }

public:
    void Init()
    {
        pbBuff = nullptr;
        iSize = 0;
        cbTotal = SIZE;
    }

    void Destroy()
    {
        delete[] pbBuff;
        pbBuff = nullptr;
    }

    void* AllocThrows(SIZE_T iItems)
    {
        return _Alloc<FALSE, TRUE>(iItems);
    }

    void* AllocNoThrow(SIZE_T iItems)
    {
        return _Alloc<FALSE, FALSE>(iItems);
    }

    void ReSizeThrows(SIZE_T iItems)
    {
        _Alloc<TRUE, TRUE>(iItems);
    }

    HRESULT ReSizeNoThrow(SIZE_T iItems);

    void Shrink(SIZE_T iItems)
    {
#ifdef _ASSERTE
        _ASSERTE(iItems <= cbTotal);
#else
        assert(iItems <= cbTotal);
#endif
        iSize = iItems;
    }

    operator PVOID()
    {
        return Ptr();
    }

    void* Ptr()
    {
        return pbBuff != nullptr ? pbBuff : static_cast<void*>(&rgData[0]);
    }

    const void* Ptr() const
    {
        return pbBuff != nullptr ? pbBuff : static_cast<const void*>(&rgData[0]);
    }

    SIZE_T Size() const
    {
        return iSize;
    }

    SIZE_T MaxSize() const
    {
        return cbTotal;
    }

    void Maximize()
    {
        iSize = cbTotal;
    }

    HRESULT ConvertUtf8_UnicodeNoThrow(const char* utf8str);
    void ConvertUtf8_Unicode(const char* utf8str);
    void ConvertUnicode_Utf8(const WCHAR* pString);

    const char* SetString(const char* pStr, SIZE_T len)
    {
        char* buffer = static_cast<char*>(AllocThrows(len + 1));
        std::memcpy(buffer, pStr, len);
        buffer[len] = 0;
        return buffer;
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

    BYTE* pbBuff;
    SIZE_T iSize;
    SIZE_T cbTotal;
    UINT64 rgData[(SIZE + sizeof(UINT64) - 1) / sizeof(UINT64)];
};

#define CQUICKBYTES_BASE_SIZE 512
#define CQUICKBYTES_INCREMENTAL_SIZE 128

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

#endif // QUICKBYTES_H_
