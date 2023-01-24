// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: caparser.h
//


//

//
// ============================================================================

#ifndef __CAPARSER_H__
#define __CAPARSER_H__

#include "stgpooli.h"

class CustomAttributeParser {
public:
    CustomAttributeParser(              // Constructor for CustomAttributeParser.
        const void *pvBlob,             // Pointer to the CustomAttribute blob.
        ULONG   cbBlob)                 // Size of the CustomAttribute blob.
     :  m_pbCur(reinterpret_cast<const BYTE*>(pvBlob)),
        m_pbBlob(reinterpret_cast<const BYTE*>(pvBlob)),
        m_cbBlob(cbBlob)
    {
        LIMITED_METHOD_CONTRACT;
    }

private:
    signed __int8    GetI1()
    {
        LIMITED_METHOD_CONTRACT;
        signed __int8 tmp = *reinterpret_cast<const signed __int8*>(m_pbCur);
        m_pbCur += sizeof(signed __int8);
        return tmp;
    }
    unsigned __int8  GetU1()
    {
        LIMITED_METHOD_CONTRACT;
        unsigned __int8 tmp = *reinterpret_cast<const unsigned __int8*>(m_pbCur);
        m_pbCur += sizeof(unsigned __int8);
        return tmp;
    }

    signed __int16   GetI2()
    {
        LIMITED_METHOD_CONTRACT;
        signed __int16 tmp = GET_UNALIGNED_VAL16(m_pbCur);
        m_pbCur += sizeof(signed __int16);
        return tmp;
    }
    unsigned __int16 GetU2()
    {
        LIMITED_METHOD_CONTRACT;
        unsigned __int16 tmp = GET_UNALIGNED_VAL16(m_pbCur);
        m_pbCur += sizeof(unsigned __int16 );
        return tmp;
    }

    signed __int32   GetI4()
    {
        LIMITED_METHOD_CONTRACT;
        signed __int32 tmp = GET_UNALIGNED_VAL32(m_pbCur);
        m_pbCur += sizeof(signed __int32 );
        return tmp;
    }
    unsigned __int32 GetU4()
    {
        LIMITED_METHOD_CONTRACT;
        unsigned __int32 tmp = GET_UNALIGNED_VAL32(m_pbCur);
        m_pbCur += sizeof(unsigned __int32 );
        return tmp;
    }

    signed __int64   GetI8()
    {
        LIMITED_METHOD_CONTRACT;
        signed __int64 tmp = GET_UNALIGNED_VAL64(m_pbCur);
        m_pbCur += sizeof(signed __int64 );
        return tmp;
    }
    unsigned __int64 GetU8()
    {
        LIMITED_METHOD_CONTRACT;
        unsigned __int64 tmp = GET_UNALIGNED_VAL64(m_pbCur);
        m_pbCur += sizeof(unsigned __int64 );
        return tmp;
    }

public:
    float            GetR4()
    {
        LIMITED_METHOD_CONTRACT;
        __int32 tmp = GET_UNALIGNED_VAL32(m_pbCur);
        _ASSERTE(sizeof(__int32) == sizeof(float));
        m_pbCur += sizeof(float);
        return (float &)tmp;
    }

    double           GetR8()
    {
        LIMITED_METHOD_CONTRACT;
        __int64 tmp = GET_UNALIGNED_VAL64(m_pbCur);
        _ASSERTE(sizeof(__int64) == sizeof(double));
        m_pbCur += sizeof(double);
        return (double &)tmp;
    }

private:
    unsigned __int16 GetProlog()
    {
        WRAPPER_NO_CONTRACT;
        unsigned __int16 val;
        VERIFY(SUCCEEDED(GetProlog(&val)));
        return val;
    }

    LPCUTF8 GetString(ULONG *pcbString)
    {
        WRAPPER_NO_CONTRACT;
        LPCUTF8 val;
        VERIFY(SUCCEEDED(GetString(&val, pcbString)));
        return val;
    }

public:
    HRESULT GetI1(signed __int8 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(signed __int8))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetI1();
        return S_OK;
    }

    HRESULT GetTag(CorSerializationType *pVal)
    {
        WRAPPER_NO_CONTRACT;
        HRESULT hr;
        signed __int8 tmp;
        IfFailRet(GetI1(&tmp));
        *pVal = (CorSerializationType)((unsigned __int8)tmp);
        return hr;
    }

    HRESULT GetU1(unsigned __int8 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(unsigned __int8))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetU1();
        return S_OK;
    }

    HRESULT GetI2(signed __int16 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(signed __int16))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetI2();
        return S_OK;
    }
    HRESULT GetU2(unsigned __int16 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(unsigned __int16))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetU2();
        return S_OK;
    }

    HRESULT GetI4(signed __int32 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(signed __int32))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetI4();
        return S_OK;
    }
    HRESULT GetU4(unsigned __int32 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(unsigned __int32))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetU4();
        return S_OK;
    }

    HRESULT GetI8(signed __int64 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(signed __int64))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetI8();
        return S_OK;
    }
    HRESULT GetU8(unsigned __int64 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(unsigned __int64))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetU8();
        return S_OK;
    }

    HRESULT GetR4(float *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(float))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetR4();
        return S_OK;
    }
    HRESULT GetR8(double *pVal)
    {
        WRAPPER_NO_CONTRACT;

        if (BytesLeft() < (int) sizeof(double))
            return META_E_CA_INVALID_BLOB;
        *pVal = GetR8();
        return S_OK;
    }

    HRESULT GetProlog(unsigned __int16 *pVal)
    {
        WRAPPER_NO_CONTRACT;

        m_pbCur = m_pbBlob;

        if (BytesLeft() < (int)(sizeof(BYTE) * 2))
            return META_E_CA_INVALID_BLOB;

        return GetU2(pVal);
    }

    // Added for compatibility with anyone that may emit
    // blobs where the prolog is the only incorrect data.
    HRESULT SkipProlog()
    {
        unsigned __int16 val;
        return GetProlog(&val);
    }

    HRESULT ValidateProlog()
    {
        HRESULT hr;
        unsigned __int16 val;
        IfFailRet(GetProlog(&val));

        if (val != 0x0001)
            return META_E_CA_INVALID_BLOB;

        return hr;
    }

    //
    // IMPORTANT: the returned string is typically not null-terminated.
    //
    // This can return any of three distinct valid results:
    //   - NULL string, indicated by *pszString==NULL, *pcbString==0
    //   - empty string, indicated by *pszString!=NULL, *pcbString==0
    //   - non-empty string, indicated by *pdzString!=NULL, *pcbString!=0
    //  If you expect non-null or non-empty strings in your usage scenario,
    //  call the GetNonNullString and GetNonEmptyString helpers below.
    //
    HRESULT GetString(LPCUTF8 *pszString, ULONG *pcbString)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        HRESULT hr;

        if (BytesLeft() == 0)
        {   // Need to check for NULL string sentinel (see below),
            // so need to have at least one byte to read.
            IfFailRet(META_E_CA_INVALID_BLOB);
        }

        if (*m_pbCur == 0xFF)
        {   // 0xFF indicates the NULL string, which is semantically
            // different than the empty string.
            *pszString = NULL;
            *pcbString = 0;
            m_pbCur++;
            return S_OK;
        }

        // Get the length, pointer to data following the length.
        return GetData((BYTE const **)pszString, pcbString);
    }

    //
    // This can return any of two distinct valid results:
    //   - empty string, indicated by *pszString!=NULL, *pcbString==0
    //   - non-empty string, indicated by *pszString!=NULL, *pcbString!=0
    //  If you expect non-null or non-empty strings in your usage scenario,
    //  call the GetNonNullString and GetNonEmptyString helpers below.
    //
    HRESULT GetNonNullString(LPCUTF8 *pszString, ULONG *pcbString)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        HRESULT hr;

        IfFailRet(GetString(pszString, pcbString));

        if (*pszString == NULL)
        {
            return META_E_CA_INVALID_BLOB;
        }

        return S_OK;
    }

    //
    // This function will only return success if the string is valid,
    // non-NULL and non-empty; i.e., *pszString!=NULL, *pcbString!=0
    //
    HRESULT GetNonEmptyString(LPCUTF8 *pszString, ULONG *pcbString)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_FORBID_FAULT;

        HRESULT hr;

        IfFailRet(GetNonNullString(pszString, pcbString));

        if (*pcbString == 0)
        {
            return META_E_CA_INVALID_BLOB;
        }

        return S_OK;
    }

    // IMPORTANT: do not use with string fetching - use GetString instead.
    HRESULT GetData(BYTE const **ppbData, ULONG *pcbData)
    {
        HRESULT hr;
        IfFailRet(CPackedLen::SafeGetData(m_pbCur, m_pbBlob + m_cbBlob, pcbData, ppbData));
        // Move past the data we just recovered
        m_pbCur = *ppbData + *pcbData;

        return S_OK;
    }

    // IMPORTANT: do not use with string fetching - use GetString instead.
    HRESULT GetPackedValue(ULONG *pcbData)
    {
        return CPackedLen::SafeGetLength(m_pbCur, m_pbBlob + m_cbBlob, pcbData, &m_pbCur);
    }

    int BytesLeft()
    {
        LIMITED_METHOD_CONTRACT;
        return (int)(m_cbBlob - (m_pbCur - m_pbBlob));
    }

private:
    const BYTE  *m_pbCur;
    const BYTE  *m_pbBlob;
    ULONG       m_cbBlob;
};

#endif // __CAPARSER_H__
