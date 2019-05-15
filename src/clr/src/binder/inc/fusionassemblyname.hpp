// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
        wchar_t* asStr;        // For debugging.
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

class CAssemblyName : public IAssemblyName
{
private:
    DWORD        _dwSig;
    Volatile<LONG> _cRef;
    CPropertyArray _rProp;
    BOOL         _fIsFinalized;
    BOOL         _fPublicKeyToken;
    BOOL         _fCustom;
    LPWSTR       _pwzPathModifier;
    LPWSTR       _pwzTextualIdentity;
    LPWSTR       _pwzTextualIdentityILFull;

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

    STDMETHOD(Finalize)();

    STDMETHOD(GetDisplayName)(
        __out_ecount_opt(*pccDisplayName)  LPOLESTR  szDisplayName,
        __inout                            LPDWORD   pccDisplayName,
        __in                               DWORD     dwDisplayFlags);
   
    STDMETHOD(GetName)( 
        __inout  LPDWORD lpcwBuffer,
        __out_ecount_opt(*lpcwBuffer) LPOLESTR pwzBuffer);

    STDMETHOD(GetVersion)( 
        /* [out] */ LPDWORD pwVersionHi,
        /* [out] */ LPDWORD pwVersionLow);
    
    STDMETHOD (IsEqual)(
        /* [in] */ LPASSEMBLYNAME pName,
        /* [in] */ DWORD dwCmpFlags);
        
    STDMETHOD(Reserved)(
        /* in      */  REFIID               refIID,
        /* in      */  IUnknown            *pUnkBindSink,
        /* in      */  IUnknown            *pUnkAppCtx,
        /* in      */  LPCOLESTR            szCodebase,
        /* in      */  LONGLONG             llFlags,
        /* in      */  LPVOID               pvReserved,
        /* in      */  DWORD                cbReserved,
        /*     out */  VOID               **ppv);

    STDMETHODIMP Clone(IAssemblyName **ppName);

    HRESULT SetPropertyInternal(/* in */ DWORD  PropertyId,
                                /* in */ LPCVOID pvProperty,
                                /* in */ DWORD  cbProperty);

    CAssemblyName();
    virtual ~CAssemblyName();

    HRESULT Init(LPCTSTR pszAssemblyName, ASSEMBLYMETADATA *pamd);
    HRESULT Parse(LPCWSTR szDisplayName);

    static BOOL IsStronglyNamed(IAssemblyName *pName);
    static BOOL IsPartial(IAssemblyName *pName,
                          LPDWORD pdwCmpMask = NULL);

protected:
    HRESULT GetVersion(DWORD   dwMajorVersionEnumValue,
                       LPDWORD pwVersionHi,
                       LPDWORD pwVersionLow);

    HRESULT CopyProperties(CAssemblyName *pSource,
                           CAssemblyName *pTarget,
                           const DWORD properties[],
                           DWORD dwSize);
};

STDAPI
CreateAssemblyNameObject(
    LPASSEMBLYNAME    *ppAssemblyName,
    LPCOLESTR          szAssemblyName,
    DWORD              dwFlags,
    LPVOID             pvReserved);

STDAPI
CreateAssemblyNameObjectFromMetaData(
    LPASSEMBLYNAME    *ppAssemblyName,
    LPCOLESTR          szAssemblyName,
    ASSEMBLYMETADATA  *pamd,
    LPVOID             pvReserved);

namespace LegacyFusion
{
    HRESULT SetStringProperty(IAssemblyName *pIAssemblyName,
                              DWORD          dwPropertyId,
                              SString       &value);
    HRESULT SetBufferProperty(IAssemblyName *pIAssemblyName,
                              DWORD          dwPropertyId,
                              SBuffer       &value);
    HRESULT SetWordProperty(IAssemblyName *pIAssemblyName,
                            DWORD          dwPropertyId,
                            DWORD          dwValue);
    HRESULT SetDwordProperty(IAssemblyName *pIAssemblyName,
                             DWORD          dwPropertyId,
                             DWORD          dwValue);
};

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

        // Returns an allocated buffer with the contents of the property.
        //
        // Returns S_FALSE if the property has not been set.
        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, __deref_out LPWSTR * pwzVal);

        inline HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, LPCWSTR *pwzOut)
        { return GetProperty(pName, dwProperty, const_cast<LPWSTR*>(pwzOut)); }

        // Returns an allocated buffer with the contents of the property.
        //
        // Returns S_FALSE if the property has not been set.
        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, __deref_out LPSTR *pwzOut);

        inline HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, LPCSTR *pwzOut)
        { return GetProperty(pName, dwProperty, const_cast<LPSTR*>(pwzOut)); }

        template <typename T> inline
        typename std::enable_if<!std::is_pointer< typename std::remove_cv< T >::type >::value, HRESULT>::type
        GetProperty(IAssemblyName * pName, DWORD dwProperty, T * pVal)
        {
            DWORD cbBuf = sizeof(T);
            HRESULT hr = GetProperty(pName, dwProperty, pVal, &cbBuf);
            if (hr == S_OK && cbBuf != sizeof(T))
                hr = E_UNEXPECTED;
            return hr;
        }

        inline HRESULT GetSimpleName(IAssemblyName * pName, SString & ssName)
        { return GetProperty(pName, ASM_NAME_NAME, ssName); }
    } // namespace fusion.util
} // namespace fusion


#endif
