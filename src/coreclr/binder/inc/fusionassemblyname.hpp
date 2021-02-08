// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// FusionAssemblyName.hpp
//
// Defines the CAssemblyName class
//
// ============================================================

#ifndef __FUSION_ASSEMBLY_NAME_HPP__
#define __FUSION_ASSEMBLY_NAME_HPP__

#include "fusionhelpers.hpp"

struct FusionProperty
{
    union {
        LPVOID pv;
        WCHAR* asStr;        // For debugging.
    };
    DWORD  cb;
};

class CPropertyArray
{
    friend class CAssemblyName;
private:
    DWORD    _dwSig;
    FusionProperty _rProp[ASM_NAME_MAX_PARAMS];

public:
    CPropertyArray();
    ~CPropertyArray();

    inline HRESULT Set(DWORD PropertyId, LPCVOID pvProperty, DWORD  cbProperty);
    inline HRESULT Get(DWORD PropertyId, LPVOID pvProperty, LPDWORD pcbProperty);
    inline FusionProperty operator [] (DWORD dwPropId);
};

class CAssemblyName final : public IAssemblyName
{
private:
    DWORD        _dwSig;
    Volatile<LONG> _cRef;
    CPropertyArray _rProp;
    BOOL         _fPublicKeyToken;
    BOOL         _fCustom;

public:
    // IUnknown methods
    STDMETHODIMP            QueryInterface(REFIID riid,void ** ppv);
    STDMETHODIMP_(ULONG)    AddRef();
    STDMETHODIMP_(ULONG)    Release();

    // IAssemblyName methods
    STDMETHOD(SetProperty)(
        /* in */ DWORD  PropertyId,
        /* in */ LPCVOID pvProperty,
        /* in */ DWORD  cbProperty);

    STDMETHOD(GetProperty)(
        /* in      */  DWORD    PropertyId,
        /*     out */  LPVOID   pvProperty,
        /* in  out */  LPDWORD  pcbProperty);

    HRESULT SetPropertyInternal(/* in */ DWORD  PropertyId,
                                /* in */ LPCVOID pvProperty,
                                /* in */ DWORD  cbProperty);

    CAssemblyName();

    HRESULT Parse(LPCWSTR szDisplayName);
};

STDAPI
CreateAssemblyNameObject(
    LPASSEMBLYNAME    *ppAssemblyName,
    LPCOLESTR          szAssemblyName);

namespace fusion
{
    namespace util
    {
        // Fills the provided buffer with the contents of the property. pcbBuf is
        // set to be either the required buffer space when insufficient buffer is
        // provided, or the number of bytes written.
        //
        // Returns S_FALSE if the property has not been set, regardless of the values of pBuf and pcbBuf.
        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, PVOID pBuf, DWORD *pcbBuf);

        // Fills the provided buffer with the contents of the property. If no buffer is provided
        // (*ppBuf == nullptr), then a buffer is allocated for the caller and ppBuf is set to point
        // at the allocated buffer on return. pcbBuf is set to be either the required buffer space
        // when insufficient buffer is provided, or the number of bytes written.
        //
        // Returns S_FALSE if the property has not been set, regardless of the values of pBuf and pcbBuf.
        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, PBYTE * ppBuf, DWORD *pcbBuf);

        // Fills the provided SString with the contents of the property.
        //
        // Returns S_FALSE if the property has not been set.
        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, SString & ssVal);

        inline HRESULT GetSimpleName(IAssemblyName * pName, SString & ssName)
        { return GetProperty(pName, ASM_NAME_NAME, ssName); }
    } // namespace fusion.util
} // namespace fusion


#endif
