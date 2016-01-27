// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File: CComBSTR.h 
// 
// ===========================================================================

/*++

Abstract:

    Stripped down and modified version of CComBSTR

--*/

#ifndef __CCOMBSTR_H__
#define __CCOMBSTR_H__

#ifdef __cplusplus

#ifndef AtlThrow
#define AtlThrow(a) RaiseException(STATUS_NO_MEMORY,EXCEPTION_NONCONTINUABLE,0,nullptr); 
#endif
#ifndef ATLASSERT
#define ATLASSERT(a) _ASSERTE(a)
#endif
#define MAX_SATELLITESTRING 1024

#include <safemath.h>

class CComBSTR
{
public:
    BSTR m_str;
    CComBSTR()
    {
        m_str = nullptr;
    }
    CComBSTR(int nSize)
    {
        if (nSize == 0)
            m_str = nullptr;
        else
        {
            m_str = ::SysAllocStringLen(nullptr, nSize);
            if (m_str == nullptr)
                AtlThrow(E_OUTOFMEMORY);
        }
    }
    CComBSTR(int nSize, LPCOLESTR sz)
    {
        if (nSize == 0)
            m_str = nullptr;
        else
        {
            m_str = ::SysAllocStringLen(sz, nSize);
            if (m_str == nullptr)
                AtlThrow(E_OUTOFMEMORY);
        }
    }
    CComBSTR(LPCOLESTR pSrc)
    {
        if (pSrc == nullptr)
            m_str = nullptr;
        else
        {
            m_str = ::SysAllocString(pSrc);
            if (m_str == nullptr)
                AtlThrow(E_OUTOFMEMORY);
        }
    }

    CComBSTR(const CComBSTR& src)
    {
        m_str = src.Copy();
        if (!!src && m_str == nullptr)
            AtlThrow(E_OUTOFMEMORY);

    }

    CComBSTR& operator=(const CComBSTR& src)
    {
        if (m_str != src.m_str)
        {
            ::SysFreeString(m_str);
            m_str = src.Copy();
            if (!!src && m_str == nullptr)
                AtlThrow(E_OUTOFMEMORY);
        }
        return *this;
    }

    CComBSTR& operator=(LPCOLESTR pSrc)
    {
        if (pSrc != m_str)
        {
            ::SysFreeString(m_str);
            if (pSrc != nullptr)
            {
                m_str = ::SysAllocString(pSrc);
                if (m_str == nullptr)
                    AtlThrow(E_OUTOFMEMORY);
            }
            else
                m_str = nullptr;
        }
        return *this;
    }

    ~CComBSTR()
    {
        ::SysFreeString(m_str);
    }
    unsigned int ByteLength() const
    {
        return (m_str == nullptr)? 0 : SysStringByteLen(m_str);
    }
    unsigned int Length() const
    {
        return (m_str == nullptr)? 0 : SysStringLen(m_str);
    }
    operator BSTR() const
    {
        return m_str;
    }
    BSTR* operator&()
    {
        return &m_str;
    }
    BSTR Copy() const
    {
        if (m_str == nullptr)
            return nullptr;
        return ::SysAllocStringLen(m_str, SysStringLen(m_str));
    }
    HRESULT CopyTo(BSTR* pbstr)
    {
        ATLASSERT(pbstr != nullptr);
        if (pbstr == nullptr)
            return E_POINTER;
        *pbstr = Copy();
        if ((*pbstr == nullptr) && (m_str != nullptr))
            return E_OUTOFMEMORY;
        return S_OK;
    }
    // copy BSTR to VARIANT
    HRESULT CopyTo(VARIANT *pvarDest)
    {
        ATLASSERT(pvarDest != nullptr);
        HRESULT hRes = E_POINTER;
        if (pvarDest != nullptr)
        {
            V_VT (pvarDest) = VT_BSTR;
            V_BSTR (pvarDest) = Copy();
            if (V_BSTR (pvarDest) == nullptr && m_str != nullptr)
                hRes = E_OUTOFMEMORY;
            else
                hRes = S_OK;
        }
        return hRes;
    }

    void Attach(BSTR src)
    {
        if (m_str != src)
        {
            ::SysFreeString(m_str);
            m_str = src;
        }
    }
    BSTR Detach()
    {
        BSTR s = m_str;
        m_str = nullptr;
        return s;
    }
    void Empty()
    {
        ::SysFreeString(m_str);
        m_str = nullptr;
    }
    HRESULT Append(LPCOLESTR lpsz)
    {
        return Append(lpsz, UINT(lstrlenW(lpsz)));
    }

    HRESULT Append(LPCOLESTR lpsz, int nLen)
    {
        if (lpsz == nullptr || (m_str != nullptr && nLen == 0))
            return S_OK;
        if (nLen < 0)
            return E_INVALIDARG;
        int n1 = Length();

        // Check for overflow
        size_t newSize;
        if (!ClrSafeInt<size_t>::addition(n1, nLen, newSize))
            return E_INVALIDARG;
        
        BSTR b;
        b = ::SysAllocStringLen(nullptr, newSize);
        if (b == nullptr)
            return E_OUTOFMEMORY;
        memcpy(b, m_str, n1*sizeof(OLECHAR));
        memcpy(b+n1, lpsz, nLen*sizeof(OLECHAR));
        b[n1+nLen] = 0;
        SysFreeString(m_str);
        m_str = b;
        return S_OK;
    }

    HRESULT AssignBSTR(const BSTR bstrSrc)
    {
        HRESULT hr = S_OK;
        if (m_str != bstrSrc)
        {
            ::SysFreeString(m_str);
            if (bstrSrc != nullptr)
            {
                m_str = SysAllocStringLen(bstrSrc, SysStringLen(bstrSrc));
                if (m_str == nullptr)
                    hr = E_OUTOFMEMORY; 
            }
            else
                m_str = nullptr;
        }
        
        return hr;
    }

    bool LoadString(HSATELLITE hInst, UINT nID)
    {
        ::SysFreeString(m_str);
        m_str = nullptr;
        WCHAR SatelliteString[MAX_SATELLITESTRING];
        if (PAL_LoadSatelliteStringW(hInst, nID, SatelliteString, MAX_SATELLITESTRING))
        {
            m_str = SysAllocString(SatelliteString);
        }
        return m_str != nullptr;
    }

    bool LoadString(PVOID hInst, UINT nID)
    {
        return LoadString ((HSATELLITE)hInst, nID);
    }

    CComBSTR& operator+=(LPCOLESTR pszSrc)
    {
        HRESULT hr;
        hr = Append(pszSrc);
        if (FAILED(hr))
            AtlThrow(hr);
        return *this;
    }

};
#endif // __cplusplus
#endif // __CCOMBSTR_H__
