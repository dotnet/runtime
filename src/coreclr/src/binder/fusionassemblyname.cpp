// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// FusionAssemblyName.cpp
//
// Implements the CAssemblyName class
//
// ============================================================

#include <windows.h>
#include <winerror.h>
#include "strongname.h"

#include "fusionhelpers.hpp"
#include "fusionassemblyname.hpp"

#include <strsafe.h>
#include "shlwapi.h"

#include "binderinterface.hpp"
#include "assemblyidentity.hpp"
#include "textualidentityparser.hpp"

#define DISPLAY_NAME_DELIMITER  W(',')
#define DISPLAY_NAME_DELIMITER_STRING W(",")
#define VERSION_STRING_SEGMENTS 4
#define REMAINING_BUFFER_SIZE ((*pccDisplayName) - (pszBuf - szDisplayName))

// ---------------------------------------------------------------------------
// Private Helpers
// ---------------------------------------------------------------------------
namespace
{
    HRESULT GetPublicKeyTokenFromPKBlob(LPBYTE pbPublicKeyToken, DWORD cbPublicKeyToken,
                                        LPBYTE *ppbSN, LPDWORD pcbSN)
    {    
        HRESULT hr = S_OK;

        // Generate the hash of the public key.
        if (!StrongNameTokenFromPublicKey(pbPublicKeyToken, cbPublicKeyToken, ppbSN, pcbSN))
        {
            hr = StrongNameErrorInfo();
        }

        return hr;
    }
};

// ---------------------------------------------------------------------------
// CPropertyArray ctor
// ---------------------------------------------------------------------------
CPropertyArray::CPropertyArray()
{
    _dwSig = 0x504f5250; /* 'PORP' */
    memset(&_rProp, 0, ASM_NAME_MAX_PARAMS * sizeof(FusionProperty));
}

// ---------------------------------------------------------------------------
// CPropertyArray dtor
// ---------------------------------------------------------------------------
CPropertyArray::~CPropertyArray()
{
    for (DWORD i = 0; i < ASM_NAME_MAX_PARAMS; i++)
    {
        if (_rProp[i].cb > sizeof(DWORD))
        {
            if (_rProp[i].pv != NULL)
            {
                FUSION_DELETE_ARRAY((LPBYTE) _rProp[i].pv);
                _rProp[i].pv = NULL;
            }
        }
    }
}

// ---------------------------------------------------------------------------
// CPropertyArray::Set
// ---------------------------------------------------------------------------
HRESULT CPropertyArray::Set(DWORD PropertyId, 
    LPCVOID pvProperty, DWORD cbProperty)
{
    HRESULT hr = S_OK;
    FusionProperty *pItem = NULL;
        
    pItem = &(_rProp[PropertyId]);

    if (!cbProperty && !pvProperty)
    {
        if (pItem->cb > sizeof(DWORD))
        {
            if (pItem->pv != NULL)
                FUSION_DELETE_ARRAY((LPBYTE) pItem->pv);
        }
        pItem->pv = NULL;
    }
    else if (cbProperty > sizeof(DWORD))
    {
        LPBYTE ptr = NEW(BYTE[cbProperty]);
        if (!ptr)
        {
            hr = E_OUTOFMEMORY;
            goto exit;
        }
        
        if (pItem->cb > sizeof(DWORD))
            FUSION_DELETE_ARRAY((LPBYTE) pItem->pv);

            memcpy(ptr, pvProperty, cbProperty);
            pItem->pv = ptr;
        }
    else
    {
        if (pItem->cb > sizeof(DWORD))
            FUSION_DELETE_ARRAY((LPBYTE) pItem->pv);

        memcpy(&(pItem->pv), pvProperty, cbProperty);

#ifdef _DEBUG
        if (PropertyId == ASM_NAME_ARCHITECTURE) {
            PEKIND pe = * ((PEKIND *)pvProperty);        
            _ASSERTE(pe != peInvalid);
        }
#endif
    }
    pItem->cb = cbProperty;

exit:
    return hr;
}     

// ---------------------------------------------------------------------------
// CPropertyArray::Get
// ---------------------------------------------------------------------------
HRESULT CPropertyArray::Get(DWORD PropertyId, 
    LPVOID pvProperty, LPDWORD pcbProperty)
{
    HRESULT hr = S_OK;
    FusionProperty *pItem;

    _ASSERTE(pcbProperty);

    if (PropertyId >= ASM_NAME_MAX_PARAMS
        || (!pvProperty && *pcbProperty))
    {
        _ASSERTE(!"Invalid Argument! Passed in NULL buffer with size non-zero!");
        hr = E_INVALIDARG;
        goto exit;
    }        

    pItem = &(_rProp[PropertyId]);

    if (pItem->cb > *pcbProperty)
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    else if (pItem->cb)
        memcpy(pvProperty, (pItem->cb > sizeof(DWORD) ? 
            pItem->pv : (LPBYTE) &(pItem->pv)), pItem->cb);

    *pcbProperty = pItem->cb;
        
exit:
    return hr;
}     

// ---------------------------------------------------------------------------
// CPropertyArray::operator []
// Wraps DWORD optimization test.
// ---------------------------------------------------------------------------
FusionProperty CPropertyArray::operator [] (DWORD PropertyId)
{
    FusionProperty prop;

    prop.pv = _rProp[PropertyId].cb > sizeof(DWORD) ?
        _rProp[PropertyId].pv : &(_rProp[PropertyId].pv);

    prop.cb = _rProp[PropertyId].cb;

    return prop;
}

// ---------------------------------------------------------------------------
// CAssemblyName::AddRef
// ---------------------------------------------------------------------------
STDMETHODIMP_(ULONG)
CAssemblyName::AddRef()
{
    return InterlockedIncrement(&_cRef);
}

// ---------------------------------------------------------------------------
// CAssemblyName::Release
// ---------------------------------------------------------------------------
STDMETHODIMP_(ULONG)
CAssemblyName::Release()
{
    ULONG ulRef = InterlockedDecrement(&_cRef);
    if (ulRef == 0) 
    {
        delete this;
    }

    return ulRef;
}

// ---------------------------------------------------------------------------
// CAssemblyName::QueryInterface
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::QueryInterface(REFIID riid, void** ppv)
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    if (!ppv)
    {
        hr = E_POINTER;
        goto Exit;
    }

    if (   IsEqualIID(riid, IID_IUnknown)
        || IsEqualIID(riid, IID_IAssemblyName)
       )
    {
        *ppv = static_cast<IAssemblyName*> (this);
        AddRef();
        hr = S_OK;
        goto Exit;
    }
    else
    {
        *ppv = NULL;
        hr = E_NOINTERFACE;
        goto Exit;
    }

 Exit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::SetProperty
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::SetProperty(DWORD PropertyId, 
                           LPCVOID pvProperty,
                           DWORD cbProperty)
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    hr = SetPropertyInternal(PropertyId, pvProperty, cbProperty);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::GetProperty
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::GetProperty(DWORD PropertyId, 
    LPVOID pvProperty, LPDWORD pcbProperty)
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    // Retrieve the property.
    switch(PropertyId)
    {
        case ASM_NAME_NULL_PUBLIC_KEY_TOKEN:
        case ASM_NAME_NULL_PUBLIC_KEY:
        {
            hr = (_fPublicKeyToken && !_rProp[PropertyId].cb) ? S_OK : S_FALSE;
            break;
        }
        case ASM_NAME_NULL_CUSTOM:
        {
            hr = (_fCustom && !_rProp[PropertyId].cb) ? S_OK : S_FALSE;
            break;
        }
        default:        
        {
            hr = _rProp.Get(PropertyId, pvProperty, pcbProperty);
            break;
        }
    }

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::Finalize
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::Finalize()
{
    BEGIN_ENTRYPOINT_NOTHROW;

    _fIsFinalized = TRUE;
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}
// ---------------------------------------------------------------------------
// CAssemblyName::GetDisplayName
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::GetDisplayName( __out_ecount_opt(*pccDisplayName) LPOLESTR  szDisplayName,
                               __inout LPDWORD pccDisplayName, 
                               DWORD dwDisplayFlags)
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    if (!dwDisplayFlags) {
        dwDisplayFlags = ASM_DISPLAYF_DEFAULT;
    }

    // Validate input buffer.
    if(!pccDisplayName || (!szDisplayName && *pccDisplayName)) {
        hr = E_INVALIDARG;
        goto exit;
    }

    EX_TRY
    {
        NewHolder<BINDER_SPACE::AssemblyIdentity> pAssemblyIdentity = new BINDER_SPACE::AssemblyIdentity();
        FusionProperty prop;
        StackSString textualIdentity;

        // Name required
        prop = _rProp[ASM_NAME_NAME];
        if (prop.cb == 0) {
            hr = FUSION_E_INVALID_NAME;
            goto exit;
        }
        else {
            _ASSERTE(prop.cb >= sizeof(WCHAR));

            pAssemblyIdentity->m_simpleName.Set((const WCHAR *) prop.pv,
                                                (prop.cb - sizeof(WCHAR)) / sizeof(WCHAR));
            pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_SIMPLE_NAME);
        }

        // Display version
        if (dwDisplayFlags & ASM_DISPLAYF_VERSION) {
            prop = _rProp[ASM_NAME_MAJOR_VERSION];

            // Set version if we have it
            if (prop.cb != 0) {
                DWORD dwVersionParts[4];

                for(DWORD i = 0; i < 4; i++) {
                    prop = _rProp[ASM_NAME_MAJOR_VERSION + i];

                    // Normalize non-existing version parts to zero
                    if (prop.cb == sizeof(WORD)) {
                        dwVersionParts[i] = (DWORD) (* ((WORD *) prop.pv));
                    }
                    else {
                        dwVersionParts[i] = 0;
                    }
                }

                pAssemblyIdentity->m_version.SetFeatureVersion(dwVersionParts[0], dwVersionParts[1]);
                pAssemblyIdentity->m_version.SetServiceVersion(dwVersionParts[2], dwVersionParts[3]);
                pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_VERSION);
            }
        }

        // Display culture
        if (dwDisplayFlags & ASM_DISPLAYF_CULTURE) {
            prop = _rProp[ASM_NAME_CULTURE];

            if (prop.cb != 0) {
                _ASSERTE(prop.cb >= sizeof(WCHAR));

                if (((const WCHAR *) prop.pv)[0] != 0x00) {
                    pAssemblyIdentity->m_cultureOrLanguage.
                        Set((const WCHAR *) prop.pv,
                            (prop.cb - sizeof(WCHAR)) / sizeof(WCHAR));
                }

                pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE);
            }
        }

        // Display public key token
        if ((dwDisplayFlags & ASM_DISPLAYF_PUBLIC_KEY_TOKEN) && _fPublicKeyToken) {
            prop = _rProp[ASM_NAME_PUBLIC_KEY_TOKEN];

            if (prop.cb != 0) {
                pAssemblyIdentity->m_publicKeyOrTokenBLOB.Set((const BYTE *) prop.pv, prop.cb);
                pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
            }
            else {
                pAssemblyIdentity->
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL);
            }
        }

        // Display processor architecture
        if (dwDisplayFlags & ASM_DISPLAYF_PROCESSORARCHITECTURE) {
            if (_rProp[ASM_NAME_ARCHITECTURE].cb != 0) {
                DWORD PeKind = *((LPDWORD)_rProp[ASM_NAME_ARCHITECTURE].pv);

                if (PeKind != peNone) {
                    pAssemblyIdentity->m_kProcessorArchitecture = (PEKIND) PeKind;
                    pAssemblyIdentity->
                        SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE);
                }
            }
        }

        // Display retarget flag
        if (dwDisplayFlags & ASM_DISPLAYF_RETARGET) {
            prop = _rProp[ASM_NAME_RETARGET];

            if (prop.cb != 0) {
                BOOL fRetarget = *((LPBOOL) prop.pv);

                if (fRetarget)
                {
                    pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE);
                }
            }
        }

        // Display content type
        if (dwDisplayFlags & ASM_DISPLAYF_CONTENT_TYPE)
        {
            prop = _rProp[ASM_NAME_CONTENT_TYPE];
            if (prop.cb != 0)
            {
                DWORD dwContentType = *((LPDWORD)prop.pv);
                if (dwContentType != AssemblyContentType_Default)
                {
                    pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE);
                    pAssemblyIdentity->m_kContentType = (AssemblyContentType)dwContentType;
                }
            }
        }

        // Display custom flag
        if ((dwDisplayFlags & ASM_DISPLAYF_CUSTOM) && _fCustom) {
            prop = _rProp[ASM_NAME_CUSTOM];

            if (prop.cb != 0) {
                pAssemblyIdentity->m_customBLOB.Set((const BYTE *) prop.pv, prop.cb);
                pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CUSTOM);
            }
            else {
                pAssemblyIdentity->SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CUSTOM_NULL);
            }
        }

        // Create the textual identity
        hr = BINDER_SPACE::TextualIdentityParser::ToString(pAssemblyIdentity,
                                                           pAssemblyIdentity->m_dwIdentityFlags,
                                                           textualIdentity);
        if (FAILED(hr)) {
            goto exit;
        }

        // Determine required buffer size
        DWORD dwGivenSize = *pccDisplayName;
        DWORD dwRequiredSize = textualIdentity.GetCount() + 1;

        *pccDisplayName = dwRequiredSize;

        if (dwRequiredSize > dwGivenSize) {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);

            if (szDisplayName) {
                szDisplayName[0] = 0x00;
            }
            goto exit;
        }
        else {
            hr = S_OK;
            memcpy(szDisplayName, textualIdentity.GetUnicode(), dwRequiredSize * sizeof(WCHAR));
        }
    }
    EX_CATCH_HRESULT(hr);

 exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::GetName
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::GetName(
        __inout LPDWORD lpcwBuffer,
        __out_ecount_opt(*lpcwBuffer) LPOLESTR pwzBuffer)
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    DWORD cbBuffer = *lpcwBuffer * sizeof(TCHAR);
    hr = GetProperty(ASM_NAME_NAME, pwzBuffer, &cbBuffer);
    *lpcwBuffer = cbBuffer / sizeof(TCHAR);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::GetVersion
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::GetVersion(
        /* [out] */ LPDWORD pdwVersionHi,
        /* [out] */ LPDWORD pdwVersionLow)
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    // Get Assembly Version
    hr = GetVersion( ASM_NAME_MAJOR_VERSION, pdwVersionHi, pdwVersionLow);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::IsEqual
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::IsEqual(LPASSEMBLYNAME pName, DWORD dwCmpFlags)
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    DWORD dwPartialCmpMask = 0;
    BOOL  fIsPartial = FALSE;
    CAssemblyName *pCName = static_cast<CAssemblyName *>(pName);

    const DWORD SIMPLE_VERSION_MASK = ASM_CMPF_VERSION;

    FusionProperty propThis;
    FusionProperty propPara;

    if(!pName) {
        hr = S_FALSE;
        goto Exit;
    }

    // Get the ref partial comparison mask, if any.    
    fIsPartial = CAssemblyName::IsPartial(this, &dwPartialCmpMask);

    if (dwCmpFlags == ASM_CMPF_DEFAULT) {
         // Set all comparison flags.
        dwCmpFlags = ASM_CMPF_IL_ALL | ASM_CMPF_ARCHITECTURE;

        // don't compare architecture if ref does not have architecture.
        if (!(dwPartialCmpMask & ASM_CMPF_ARCHITECTURE)) {
            dwCmpFlags &= ~ASM_CMPF_ARCHITECTURE;
        }

        // Otherwise, if ref is simple (possibly partial)
        // we mask off all version bits.
        if (!CAssemblyName::IsStronglyNamed(this)) 
        {
            // we don't have a public key token, but we don't know
            // it is because we are simply named assembly or we are
            // just partial on public key token.
            if (dwPartialCmpMask & ASM_CMPF_PUBLIC_KEY_TOKEN)
            {
                // now we know we are simply named assembly since we
                // have a public key token, but it is NULL.
                dwCmpFlags &= ~SIMPLE_VERSION_MASK;
            }
            // If neither of these two cases then public key token
            // is not set in ref , but def may be simple or strong.
            // The comparison mask is chosen based on def.
            else
            {
                if (!CAssemblyName::IsStronglyNamed(pName))
                    dwCmpFlags &= ~SIMPLE_VERSION_MASK;            
            }
        }
    }   

    // Mask off flags (either passed in or generated
    // by default flag with the comparison mask generated 
    // from the ref.
    dwCmpFlags &= dwPartialCmpMask;

    
    // The individual name fields can now be compared..

    // Compare name

    if (dwCmpFlags & ASM_CMPF_NAME) 
    {
        propThis = _rProp[ASM_NAME_NAME];
        propPara = pCName->_rProp[ASM_NAME_NAME];

        if (propThis.cb != propPara.cb) 
        {
            hr = S_FALSE;
            goto Exit;
        }
    
        if (propThis.cb && FusionCompareStringI((LPWSTR)propThis.pv, (LPWSTR)propPara.pv)) 
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // Compare version

    if (dwCmpFlags & ASM_CMPF_MAJOR_VERSION) 
    {
        propThis = _rProp[ASM_NAME_MAJOR_VERSION];
        propPara = pCName->_rProp[ASM_NAME_MAJOR_VERSION];

        if (*((LPWORD) propThis.pv) != *((LPWORD)propPara.pv))
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    if (dwCmpFlags & ASM_CMPF_MINOR_VERSION) 
    {
        propThis = _rProp[ASM_NAME_MINOR_VERSION];
        propPara = pCName->_rProp[ASM_NAME_MINOR_VERSION];

        if (*((LPWORD) propThis.pv) != *((LPWORD)propPara.pv))
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    if (dwCmpFlags & ASM_CMPF_REVISION_NUMBER) 
    {
        propThis = _rProp[ASM_NAME_REVISION_NUMBER];
        propPara = pCName->_rProp[ASM_NAME_REVISION_NUMBER];

        if (*((LPWORD) propThis.pv) != *((LPWORD)propPara.pv))
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    if (dwCmpFlags & ASM_CMPF_BUILD_NUMBER)
    {
        propThis = _rProp[ASM_NAME_BUILD_NUMBER];
        propPara = pCName->_rProp[ASM_NAME_BUILD_NUMBER];

        if (*((LPWORD) propThis.pv) != *((LPWORD)propPara.pv))
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // Compare public key token

    if (dwCmpFlags & ASM_CMPF_PUBLIC_KEY_TOKEN) 
    {
        // compare public key if both of them have public key set. 
        propThis = _rProp[ASM_NAME_PUBLIC_KEY];
        propPara = pCName->_rProp[ASM_NAME_PUBLIC_KEY];
        if (!propThis.cb || !propPara.cb) {
            // otherwise, compare public key token
            propThis = _rProp[ASM_NAME_PUBLIC_KEY_TOKEN];
            propPara = pCName->_rProp[ASM_NAME_PUBLIC_KEY_TOKEN];
        }
    
        if (propThis.cb != propPara.cb) {
            hr = S_FALSE;
            goto Exit; 
        }

        if (propThis.cb && memcmp(propThis.pv, propPara.pv, propThis.cb)) {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // Compare Culture
    
    if (dwCmpFlags & ASM_CMPF_CULTURE)
    {
        propThis = _rProp[ASM_NAME_CULTURE];
        propPara = pCName->_rProp[ASM_NAME_CULTURE];

        if (propThis.cb != propPara.cb) 
        {
            hr = S_FALSE;
            goto Exit;
        }
    
        if (propThis.cb && FusionCompareStringI((LPWSTR)propThis.pv, (LPWSTR)propPara.pv)) 
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // Compare Custom attribute.

    if (dwCmpFlags & ASM_CMPF_CUSTOM) 
    {
        propThis = _rProp[ASM_NAME_PUBLIC_KEY_TOKEN];
        propPara = pCName->_rProp[ASM_NAME_PUBLIC_KEY_TOKEN];
    
        if (propThis.cb != propPara.cb) {
            hr = S_FALSE;
            goto Exit; 
        }

        if (propThis.cb && memcmp(propThis.pv, propPara.pv, propThis.cb)) {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // Compare Retarget flag
    if (dwCmpFlags & ASM_CMPF_RETARGET)
    {
        propThis = _rProp[ASM_NAME_RETARGET];
        propPara = pCName->_rProp[ASM_NAME_RETARGET];

        if (*((LPDWORD) propThis.pv) != *((LPDWORD)propPara.pv))
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // compare config mask
    if (dwCmpFlags & ASM_CMPF_CONFIG_MASK) 
    {
        propThis = _rProp[ASM_NAME_CONFIG_MASK];
        propPara = pCName->_rProp[ASM_NAME_CONFIG_MASK];

        if (*((LPDWORD) propThis.pv) != *((LPDWORD)propPara.pv))
        {
            hr = S_FALSE;
            goto Exit;
        }

    }

    // compare architecture
    if (dwCmpFlags & ASM_CMPF_ARCHITECTURE) 
    {
        propThis = _rProp[ASM_NAME_ARCHITECTURE];
        propPara = pCName->_rProp[ASM_NAME_ARCHITECTURE];
    
        if (propThis.cb != propPara.cb) {
            hr = S_FALSE;
            goto Exit; 
        }

        if (propThis.cb) {
            if (*((LPDWORD) propThis.pv) != *((LPDWORD)propPara.pv)) {
                hr = S_FALSE;
                goto Exit;
            }
        }
    }

    // Compare content type
    if (dwCmpFlags & ASM_CMPF_CONTENT_TYPE)
    {
        propThis = _rProp[ASM_NAME_CONTENT_TYPE];
        propPara = pCName->_rProp[ASM_NAME_CONTENT_TYPE];

        if (*((LPDWORD)propThis.pv) != *((LPDWORD)propPara.pv))
        {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // compare MVID
    if (dwCmpFlags & ASM_CMPF_MVID) 
    {
        propThis = _rProp[ASM_NAME_MVID];
        propPara = pCName->_rProp[ASM_NAME_MVID];
    
        if (propThis.cb != propPara.cb) {
            hr = S_FALSE;
            goto Exit; 
        }

        if (propThis.cb && memcmp(propThis.pv, propPara.pv, propThis.cb)) {
            hr = S_FALSE;
            goto Exit;
        }
    }

    // compare Signature
    if (dwCmpFlags & ASM_CMPF_SIGNATURE) 
    {
        propThis = _rProp[ASM_NAME_SIGNATURE_BLOB];
        propPara = pCName->_rProp[ASM_NAME_SIGNATURE_BLOB];
    
        if (propThis.cb != propPara.cb) {
            hr = S_FALSE;
            goto Exit; 
        }

        if (propThis.cb && memcmp(propThis.pv, propPara.pv, propThis.cb)) {
            hr = S_FALSE;
            goto Exit;
        }
    }

    hr = S_OK;
Exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::Reserved
// ---------------------------------------------------------------------------
STDMETHODIMP
CAssemblyName::Reserved(
        /* in      */  REFIID               refIID,
        /* in      */  IUnknown            *pUnkBindSink,
        /* in      */  IUnknown            *pUnkAppCtx,
        /* in      */  LPCOLESTR            szCodebaseIn,
        /* in      */  LONGLONG             llFlags,
        /* in      */  LPVOID               pvReserved,
        /* in      */  DWORD                cbReserved,
        /*     out */  VOID               **ppv)
{
    return E_NOTIMPL;
}

// ---------------------------------------------------------------------------
// CAssemblyName::Clone
// ---------------------------------------------------------------------------
HRESULT CAssemblyName::Clone(IAssemblyName **ppName)
{
    HRESULT         hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CAssemblyName  *pClone = NULL;

    if (!ppName) {
        hr = E_INVALIDARG;
        goto Exit;
    }

    *ppName = NULL;

    pClone = NEW(CAssemblyName);
    if( !pClone ) {
        hr = E_OUTOFMEMORY;
        goto Exit;
    }

    hr = CopyProperties(this, pClone, NULL, 0);
    if (FAILED(hr)) {
        goto Exit;
    }
    
    *ppName = pClone;
    (*ppName)->AddRef();

Exit:
    SAFERELEASE(pClone);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::SetPropertyInternal
// ---------------------------------------------------------------------------
HRESULT CAssemblyName::SetPropertyInternal(DWORD  PropertyId, 
                                           LPCVOID pvProperty,
                                           DWORD  cbProperty)
{
    HRESULT hr = S_OK;
    LPBYTE pbSN = NULL;
    DWORD  cbSN = 0;

    // Fail if finalized.
    if (_fIsFinalized)
    {
        _ASSERTE(!"SetProperty on a IAssemblyName while the name is finalized!");
        hr = E_UNEXPECTED;
        goto exit;
    }

    if (PropertyId >= ASM_NAME_MAX_PARAMS
        || (!pvProperty && cbProperty))
    {
        _ASSERTE(!"Invalid Argument! Passed in NULL buffer with size non-zero!");
        hr = E_INVALIDARG;
        goto exit;
    }        

    // <REVISIT_TODO> - make this a switch statement.</REVISIT_TODO>
    if (PropertyId == ASM_NAME_MAJOR_VERSION ||
        PropertyId == ASM_NAME_MINOR_VERSION ||
        PropertyId == ASM_NAME_BUILD_NUMBER  ||
        PropertyId == ASM_NAME_REVISION_NUMBER)
    {
        if (cbProperty > sizeof(WORD)) {
            hr = E_INVALIDARG;
            goto exit;
        }
    }

    // Check if public key is being set and if so,
    // set the public key token if not already set.
    if (PropertyId == ASM_NAME_PUBLIC_KEY)
    {
        // If setting true public key, generate hash.
        if (pvProperty && cbProperty)
        {
            // Generate the public key token from the pk.
            if (FAILED(hr = GetPublicKeyTokenFromPKBlob((LPBYTE) pvProperty, cbProperty, &pbSN, &cbSN)))
                goto exit;

            // Set the public key token property.
            if (FAILED(hr = SetPropertyInternal(ASM_NAME_PUBLIC_KEY_TOKEN, pbSN, cbSN)))
                goto exit;        
        }
        // Otherwise expect call to reset property.
        else if (!cbProperty)
        {
            if (FAILED(hr = SetPropertyInternal(ASM_NAME_PUBLIC_KEY_TOKEN, pvProperty, cbProperty)))
                goto exit;
        }
            
    }
    // Setting NULL public key clears values in public key,
    // public key token and sets public key token flag.
    else if (PropertyId == ASM_NAME_NULL_PUBLIC_KEY)
    {
        pvProperty = NULL;
        cbProperty = 0;
        hr = SetPropertyInternal(ASM_NAME_NULL_PUBLIC_KEY_TOKEN, pvProperty, cbProperty);
        goto exit;
    }
    // Setting or clearing public key token.
    else if (PropertyId == ASM_NAME_PUBLIC_KEY_TOKEN)
    {
        // Defensive: invalid sized public key tokens should be avoided.
        if (cbProperty > PUBLIC_KEY_TOKEN_LEN) 
        {
            hr = SetPropertyInternal(ASM_NAME_NULL_PUBLIC_KEY_TOKEN, NULL, 0);
            hr = E_INVALIDARG;
            goto exit;
        }

        if (pvProperty && cbProperty)
            _fPublicKeyToken = TRUE;
        else if (!cbProperty)
            _fPublicKeyToken = FALSE;
    }
    // Setting NULL public key token clears public key token and
    // sets public key token flag.
    else if (PropertyId == ASM_NAME_NULL_PUBLIC_KEY_TOKEN)
    {
        _fPublicKeyToken = TRUE;
        pvProperty = NULL;
        cbProperty = 0;
        PropertyId = ASM_NAME_PUBLIC_KEY_TOKEN;
    }
    else if (PropertyId == ASM_NAME_CUSTOM)
    {
        if (pvProperty && cbProperty)
            _fCustom = TRUE;
        else if (!cbProperty)
            _fCustom = FALSE;
    }
    else if (PropertyId == ASM_NAME_NULL_CUSTOM)
    {
        _fCustom = TRUE;
        pvProperty = NULL;
        cbProperty = 0;
        PropertyId = ASM_NAME_CUSTOM;
    }

    // Setting "neutral" as the culture is the same as "" culture (meaning
    // culture-invariant).
    else if (PropertyId == ASM_NAME_CULTURE) {
        if (pvProperty && !FusionCompareStringI((LPWSTR)pvProperty, W("neutral"))) {
            pvProperty = (void *)W("");
            cbProperty = sizeof(W(""));
        }
    }

    // Set property on array.
    hr = _rProp.Set(PropertyId, pvProperty, cbProperty);

exit:
    if (SUCCEEDED(hr)) {
        LPWSTR              pwzOld;

        // Clear cache

        pwzOld = InterlockedExchangeT(&_pwzTextualIdentity, NULL);
        SAFEDELETEARRAY(pwzOld);
        pwzOld = InterlockedExchangeT(&_pwzTextualIdentityILFull, NULL);
        SAFEDELETEARRAY(pwzOld);
    }

    // Free memory allocated by crypto wrapper.
    if (pbSN) {
        StrongNameFreeBuffer(pbSN);
    }

    return hr;
}



// ---------------------------------------------------------------------------
// CheckFieldsForFriendAssembly
// ---------------------------------------------------------------------------
STDAPI
CheckFieldsForFriendAssembly(
    LPASSEMBLYNAME     pAssemblyName)
{
    HRESULT hr = S_OK;
    DWORD dwSize=0;

    // Let's look at the information they gave us in the friends declaration.
    // If they put in a Processor Architecture, Culture, or Version, then we'll return an error.

    if (FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_MAJOR_VERSION, NULL, &dwSize)) ||
        FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_MINOR_VERSION, NULL, &dwSize)) ||
        FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_BUILD_NUMBER, NULL, &dwSize)) ||
        FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_REVISION_NUMBER, NULL, &dwSize)) ||
        FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_CULTURE, NULL, &dwSize)) ||
        FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_ARCHITECTURE, NULL, &dwSize)))
        {
            // If any of these calls failed due to an insufficient buffer, then that means
            // the assembly name contained them
            if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                hr = META_E_CA_BAD_FRIENDS_ARGS;
    } else {
        if (FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, NULL, &dwSize))) {
                
            //
            // Public Key token should not be passed to InternalsVisibleTo 
            // attribute. This translates to the ASM_NAME_PUBLIC_KEY_TOKEN 
            // property being set, while the full public key is not.  
            //

            if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) {
                    
                dwSize = 0;    
                    
                if (FAILED(hr = pAssemblyName->GetProperty(ASM_NAME_PUBLIC_KEY, NULL, &dwSize))) {
                    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                        hr = S_OK;
                } else {
                    hr = META_E_CA_BAD_FRIENDS_ARGS;
                }
                    
            } 
        } else {
            hr = S_OK;
        }
    }
        

    return hr;
}

// ---------------------------------------------------------------------------
// CreateAssemblyNameObject
// ---------------------------------------------------------------------------

// This is not external for CoreCLR
STDAPI
CreateAssemblyNameObject(
    LPASSEMBLYNAME    *ppAssemblyName,
    LPCOLESTR          szAssemblyName,
    DWORD              dwFlags,
    LPVOID             pvReserved)
{

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CAssemblyName *pName = NULL;

    if (!ppAssemblyName)
    {
        hr = E_INVALIDARG;
        goto exit;
    }

    pName = NEW(CAssemblyName);
    if (!pName)
    {
        hr = E_OUTOFMEMORY;
        goto exit;
    }

    if (dwFlags & CANOF_PARSE_DISPLAY_NAME)
    {
        hr = pName->Init(NULL, NULL);
        if (FAILED(hr)) {
            goto exit;
        }

        hr = pName->Parse((LPWSTR)szAssemblyName);
    }
    else
    {
        hr = pName->Init(szAssemblyName, NULL);
    }


    if (SUCCEEDED(hr) && ((dwFlags & CANOF_VERIFY_FRIEND_ASSEMBLYNAME)))
    {
        hr = CheckFieldsForFriendAssembly(pName);
    }


    if (FAILED(hr)) 
    {
        SAFERELEASE(pName);
        goto exit;
    }

    *ppAssemblyName = pName;

exit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// ---------------------------------------------------------------------------
// CreateAssemblyNameObjectFromMetaData
// ---------------------------------------------------------------------------
STDAPI
CreateAssemblyNameObjectFromMetaData(
    LPASSEMBLYNAME    *ppAssemblyName,
    LPCOLESTR          szAssemblyName,
    ASSEMBLYMETADATA  *pamd,
    LPVOID             pvReserved)
{

    HRESULT hr = S_OK;
    CAssemblyName *pName = NULL;

    pName = NEW(CAssemblyName);
    if (!pName)
    {
        hr = E_OUTOFMEMORY;
        goto exit;
    }

    hr = pName->Init(szAssemblyName, pamd);
        
    if (FAILED(hr)) 
    {
        SAFERELEASE(pName);
        goto exit;
    }

    *ppAssemblyName = pName;

exit:
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName constructor
// ---------------------------------------------------------------------------
CAssemblyName::CAssemblyName()
{
    _dwSig              = 0x454d414e; /* 'EMAN' */
    _fIsFinalized       = FALSE;
    _fPublicKeyToken    = FALSE;
    _fCustom            = TRUE;
    _cRef               = 1;
    _pwzPathModifier    = NULL;
    _pwzTextualIdentity = NULL;
    _pwzTextualIdentityILFull = NULL;
}

// ---------------------------------------------------------------------------
// CAssemblyName destructor
// ---------------------------------------------------------------------------
CAssemblyName::~CAssemblyName()
{
    SAFEDELETEARRAY(_pwzPathModifier);
    SAFEDELETEARRAY(_pwzTextualIdentity);
    SAFEDELETEARRAY(_pwzTextualIdentityILFull);
}

// ---------------------------------------------------------------------------
// CAssemblyName::IsStronglyNamed
// ---------------------------------------------------------------------------
BOOL CAssemblyName::IsStronglyNamed(IAssemblyName *pName)
{
    CAssemblyName *pCName = static_cast<CAssemblyName *> (pName);
    _ASSERTE(pCName);
    
    return (pCName->_rProp[ASM_NAME_PUBLIC_KEY_TOKEN].cb != 0);
}

// ---------------------------------------------------------------------------
// CAssemblyName::IsPartial
// ---------------------------------------------------------------------------
BOOL CAssemblyName::IsPartial(IAssemblyName *pIName,
                              LPDWORD pdwCmpMask)
{
    DWORD dwCmpMask = 0;
    BOOL fPartial    = FALSE;

    static const ASM_NAME rNameFlags[] ={ASM_NAME_NAME, 
                                         ASM_NAME_CULTURE, 
                                         ASM_NAME_PUBLIC_KEY_TOKEN, 
                                         ASM_NAME_MAJOR_VERSION, 
                                         ASM_NAME_MINOR_VERSION, 
                                         ASM_NAME_BUILD_NUMBER, 
                                         ASM_NAME_REVISION_NUMBER, 
                                         ASM_NAME_CUSTOM
                                        };

    static const ASM_CMP_FLAGS rCmpFlags[] = {ASM_CMPF_NAME, 
                                              ASM_CMPF_CULTURE, 
                                              ASM_CMPF_PUBLIC_KEY_TOKEN, 
                                              ASM_CMPF_MAJOR_VERSION, 
                                              ASM_CMPF_MINOR_VERSION, 
                                              ASM_CMPF_BUILD_NUMBER, 
                                              ASM_CMPF_REVISION_NUMBER, 
                                              ASM_CMPF_CUSTOM
                                             };

    CAssemblyName *pName = static_cast<CAssemblyName*> (pIName); // dynamic_cast
    _ASSERTE(pName);
    
    DWORD iNumOfComparison = sizeof(rNameFlags) / sizeof(rNameFlags[0]);
    
    for (DWORD i = 0; i < iNumOfComparison; i++)
    {
        if (pName->_rProp[rNameFlags[i]].cb 
            || (rNameFlags[i] == ASM_NAME_PUBLIC_KEY_TOKEN
                && pName->_fPublicKeyToken)
            || (rNameFlags[i] == ASM_NAME_CUSTOM 
                && pName->_fCustom))
        {
            dwCmpMask |= rCmpFlags[i];            
        }
        else {
            fPartial = TRUE;
        }
    }

    if(pName->_rProp[ASM_NAME_ARCHITECTURE].cb) {
        dwCmpMask |= ASM_CMPF_ARCHITECTURE;
    }

    if (pName->_rProp[ASM_NAME_RETARGET].cb) {
        dwCmpMask |= ASM_CMPF_RETARGET;
    }

    if (pName->_rProp[ASM_NAME_CONTENT_TYPE].cb != 0)
    {
        dwCmpMask |= ASM_CMPF_CONTENT_TYPE;
    }
    
    if (pName->_rProp[ASM_NAME_CONFIG_MASK].cb) {
        dwCmpMask |= ASM_CMPF_CONFIG_MASK;
    }

    if (pName->_rProp[ASM_NAME_MVID].cb) {
        dwCmpMask |= ASM_CMPF_MVID;
    }

    if (pName->_rProp[ASM_NAME_SIGNATURE_BLOB].cb) {
        dwCmpMask |= ASM_CMPF_SIGNATURE;
    }

    if (pdwCmpMask)
        *pdwCmpMask = dwCmpMask;

    return fPartial;
}

// ---------------------------------------------------------------------------
// CAssemblyName::Init
// ---------------------------------------------------------------------------
HRESULT
CAssemblyName::Init(LPCTSTR pszAssemblyName, ASSEMBLYMETADATA *pamd)
{
    HRESULT hr = S_OK;

    // Name
    if (pszAssemblyName) 
    {
        hr = SetProperty(ASM_NAME_NAME, (LPTSTR) pszAssemblyName, 
            (DWORD)((wcslen(pszAssemblyName)+1) * sizeof(TCHAR)));
        if (FAILED(hr))
            goto exit;
    }

    if (pamd) {
            // Major version
        if (FAILED(hr = SetProperty(ASM_NAME_MAJOR_VERSION,
                &pamd->usMajorVersion, sizeof(WORD)))
    
            // Minor version
            || FAILED(hr = SetProperty(ASM_NAME_MINOR_VERSION, 
                &pamd->usMinorVersion, sizeof(WORD)))
    
            // Revision number
            || FAILED(hr = SetProperty(ASM_NAME_REVISION_NUMBER, 
                &pamd->usRevisionNumber, sizeof(WORD)))
    
            // Build number
            || FAILED(hr = SetProperty(ASM_NAME_BUILD_NUMBER, 
                &pamd->usBuildNumber, sizeof(WORD)))
    
            // Culture
            || FAILED(hr = SetProperty(ASM_NAME_CULTURE,
                pamd->szLocale, pamd->cbLocale * sizeof(WCHAR)))
                )
            {
                goto exit;
            }
    }

exit:
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::Parse
// ---------------------------------------------------------------------------
HRESULT CAssemblyName::Parse(__in_z LPCWSTR szDisplayName)
{
    HRESULT hr = S_OK;

    if (!(szDisplayName && *szDisplayName))
    {
        hr = E_INVALIDARG;
        goto exit;
    }

    EX_TRY {
        BINDER_SPACE::AssemblyIdentity assemblyIdentity;
        SString displayName(szDisplayName);

        // Parse the textual identity
        hr = BINDER_SPACE::TextualIdentityParser::Parse(displayName, &assemblyIdentity);
        if (FAILED(hr)) {
            goto exit;
        }

        // Set name.
        hr = SetProperty(ASM_NAME_NAME,
                         (LPVOID) assemblyIdentity.m_simpleName.GetUnicode(),
                         (assemblyIdentity.m_simpleName.GetCount() + 1) * sizeof(WCHAR));
        if (FAILED(hr)) {
            goto exit;
        }

        // Set version.
        if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_VERSION)) {
            WORD wVersionPart = 0;

            wVersionPart = (WORD) assemblyIdentity.m_version.GetMajor();
            hr = SetProperty(ASM_NAME_MAJOR_VERSION, &wVersionPart, sizeof(WORD));
            if (FAILED(hr)) {
                goto exit;
            }

            wVersionPart = (WORD) assemblyIdentity.m_version.GetMinor();
            hr = SetProperty(ASM_NAME_MINOR_VERSION, &wVersionPart, sizeof(WORD));
            if (FAILED(hr)) {
                goto exit;
            }

            wVersionPart = (WORD) assemblyIdentity.m_version.GetBuild();
            hr = SetProperty(ASM_NAME_BUILD_NUMBER, &wVersionPart, sizeof(WORD));
            if (FAILED(hr)) {
                goto exit;
            }

            wVersionPart = (WORD) assemblyIdentity.m_version.GetRevision();
            hr = SetProperty(ASM_NAME_REVISION_NUMBER, &wVersionPart, sizeof(WORD));
            if (FAILED(hr)) {
                goto exit;
            }
        }

        // Set culture.
        if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE)) {
            hr = SetProperty(ASM_NAME_CULTURE,
                             (LPVOID) assemblyIdentity.m_cultureOrLanguage.GetUnicode(),
                             (assemblyIdentity.m_cultureOrLanguage.GetCount()+1) * sizeof(WCHAR));
            if (FAILED(hr)) {
                goto exit;
            }
        }

        // Set public key (token) or NULL flag.
        if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY)) {
            SBuffer &publicKeyBuffer = assemblyIdentity.m_publicKeyOrTokenBLOB;
            const void *pBytes = publicKeyBuffer;

            // This also computes and sets the public key token.
            hr = SetProperty(ASM_NAME_PUBLIC_KEY, (void *) pBytes, publicKeyBuffer.GetSize());
            if (FAILED(hr)) {
                goto exit;
            }
        }
        else if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN)) {
            SBuffer &publicKeyTokenBuffer = assemblyIdentity.m_publicKeyOrTokenBLOB;
            const void *pBytes = publicKeyTokenBuffer;

            hr = SetProperty(ASM_NAME_PUBLIC_KEY_TOKEN,
                             (LPVOID) pBytes,
                             publicKeyTokenBuffer.GetSize());
            if (FAILED(hr)) {
                goto exit;
            }
        }
        else if (assemblyIdentity.
                 Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL)) {
            hr = SetProperty(ASM_NAME_NULL_PUBLIC_KEY_TOKEN, NULL, 0);
            if (FAILED(hr)) {
                goto exit;
            }
        }

        // Set architecture.
        if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE)) {
            PEKIND peKind = assemblyIdentity.m_kProcessorArchitecture;

            hr = SetProperty(ASM_NAME_ARCHITECTURE, (LPVOID) &peKind, sizeof(PEKIND));
            if(FAILED(hr)) {
                goto exit;
            }
        }

        // Set retargetable flag.
        if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE)) {
            BOOL fRetarget = TRUE;

            if (FAILED(hr = SetProperty(ASM_NAME_RETARGET, &fRetarget, sizeof(BOOL)))) {
                goto exit;
            }
        }

        // Set content type.
        if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE)) {
            DWORD dwContentType = assemblyIdentity.m_kContentType;
            
            hr = SetProperty(ASM_NAME_CONTENT_TYPE, &dwContentType, sizeof(dwContentType));
            IfFailGoto(hr, exit);
        }

        // Set custom or NULL flag.
        if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CUSTOM)) {
            SBuffer &customBuffer = assemblyIdentity.m_customBLOB;
            const void *pBytes = customBuffer;

            hr = SetProperty(ASM_NAME_CUSTOM, (void *) pBytes, customBuffer.GetSize());
            if (FAILED(hr)) {
                goto exit;
            }
        }
        else if (assemblyIdentity.Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CUSTOM_NULL)) {
            hr = SetProperty(ASM_NAME_NULL_CUSTOM, NULL, 0);
            if (FAILED(hr)) {
                goto exit;
            }
        }
    }
    EX_CATCH_HRESULT(hr);

 exit:
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::GetVersion
// ---------------------------------------------------------------------------
HRESULT
CAssemblyName::GetVersion(
        /* [in]  */ DWORD   dwMajorVersionEnumValue,
        /* [out] */ LPDWORD pdwVersionHi,
        /* [out] */ LPDWORD pdwVersionLow)
{
    HRESULT     hr = S_OK;
    DWORD       cb = sizeof(WORD);
    WORD        wVerMajor = 0, wVerMinor = 0, wRevNo = 0, wBldNo = 0;

    if(!pdwVersionHi || !pdwVersionLow) {
        hr = E_INVALIDARG;
        goto Exit;
    }

    *pdwVersionHi = *pdwVersionLow = 0;

    if(FAILED( (hr = GetProperty(dwMajorVersionEnumValue, &wVerMajor, &(cb = sizeof(WORD))))))
        goto Exit;
    if (cb == 0) {
        hr = FUSION_E_INVALID_NAME;
        goto Exit;
    }

    if(FAILED( (hr = GetProperty(dwMajorVersionEnumValue+1, &wVerMinor, &(cb = sizeof(WORD))))))
        goto Exit;

    if (cb == 0) {
        hr = FUSION_E_INVALID_NAME;
        goto Exit;
    }

    if(FAILED( (hr = GetProperty(dwMajorVersionEnumValue+2, &wBldNo, &(cb = sizeof(WORD))))))
        goto Exit;
    if (cb == 0) {
        hr = FUSION_E_INVALID_NAME;
        goto Exit;
    }

    if(FAILED( (hr = GetProperty(dwMajorVersionEnumValue+3, &wRevNo, &(cb = sizeof(WORD))))))
        goto Exit;

    if (cb == 0) {
        hr = FUSION_E_INVALID_NAME;
        goto Exit;
    }

    *pdwVersionHi  = MAKELONG(wVerMinor, wVerMajor);
    *pdwVersionLow = MAKELONG(wRevNo, wBldNo);

Exit:
    return hr;
}

// ---------------------------------------------------------------------------
// CAssemblyName::CopyProperties
// ---------------------------------------------------------------------------
HRESULT
CAssemblyName::CopyProperties(CAssemblyName *pSource,
                              CAssemblyName *pTarget,
                              const DWORD properties[],
                              DWORD dwSize)
{
    HRESULT         hr = S_OK;
    DWORD           i = 0;
    FusionProperty  prop;
    
    _ASSERTE(pSource && pTarget);

    if (!dwSize) {
        for( i = 0; i < ASM_NAME_MAX_PARAMS; i ++) {
            prop = pSource->_rProp[i];

            if (prop.cb) {
                if (FAILED(hr = pTarget->SetProperty(i, prop.pv, prop.cb))) {
                    goto Exit;
                }
            }
        }
    }
    else {
        for (i = 0; i<dwSize; i++) {
            _ASSERTE(properties[i] < ASM_NAME_MAX_PARAMS);
            prop = pSource->_rProp[properties[i]];
            if (prop.cb) {
                if (FAILED(hr = pTarget->SetProperty(properties[i], prop.pv, prop.cb))) {
                    goto Exit;
                }
            }   
        }
    }

    pTarget->_fPublicKeyToken = pSource->_fPublicKeyToken;
    pTarget->_fCustom = pSource->_fCustom;

    if (pSource->_pwzPathModifier) {
        pTarget->_pwzPathModifier = WSTRDupDynamic(pSource->_pwzPathModifier);
        if(!pTarget->_pwzPathModifier) {
            hr = E_OUTOFMEMORY;
            goto Exit;
        }
    }

Exit:
    return hr;
}

namespace LegacyFusion
{
    HRESULT SetStringProperty(IAssemblyName *pIAssemblyName,
                              DWORD          dwPropertyId,
                              SString       &value)
    {
        CAssemblyName *pAssemblyName = static_cast<CAssemblyName *>(pIAssemblyName);
        const WCHAR *pValue = value.GetUnicode();
        DWORD dwCBValue = (value.GetCount() + 1) * sizeof(WCHAR);

        return pAssemblyName->SetPropertyInternal(dwPropertyId,
                                                  const_cast<WCHAR *>(pValue),
                                                  dwCBValue);
    }

    HRESULT SetBufferProperty(IAssemblyName *pIAssemblyName,
                              DWORD          dwPropertyId,
                              SBuffer       &value)
    {
        CAssemblyName *pAssemblyName = static_cast<CAssemblyName *>(pIAssemblyName);
        const BYTE *pValue = value; // special operator
        DWORD dwCBValue = value.GetSize() * sizeof(BYTE);

        return pAssemblyName->SetPropertyInternal(dwPropertyId,
                                                  const_cast<BYTE *>(pValue),
                                                  dwCBValue);
    }

    HRESULT SetWordProperty(IAssemblyName *pIAssemblyName,
                            DWORD          dwPropertyId,
                            DWORD          dwValue)
    {
        CAssemblyName *pAssemblyName = static_cast<CAssemblyName *>(pIAssemblyName);
        WORD wValue = static_cast<WORD>(dwValue);
        DWORD wCBValue = sizeof(WORD);

        // This file-internal function is and must be only used to set version fields
        PREFIX_ASSUME((dwPropertyId == ASM_NAME_MAJOR_VERSION) ||
                      (dwPropertyId == ASM_NAME_MINOR_VERSION) ||
                      (dwPropertyId == ASM_NAME_BUILD_NUMBER) ||
                      (dwPropertyId == ASM_NAME_REVISION_NUMBER));

        return pAssemblyName->SetPropertyInternal(dwPropertyId, &wValue, wCBValue);
    }

    HRESULT SetDwordProperty(IAssemblyName *pIAssemblyName,
                             DWORD          dwPropertyId,
                             DWORD          dwValue)
    {
        CAssemblyName *pAssemblyName = static_cast<CAssemblyName *>(pIAssemblyName);
        DWORD dwCBValue = sizeof(DWORD);

        return pAssemblyName->SetPropertyInternal(dwPropertyId, &dwValue, dwCBValue);
    }
};
namespace fusion
{
    namespace util
    {
        namespace priv
        {
            inline bool IsNullProperty(DWORD dwProperty)
            {
                LIMITED_METHOD_CONTRACT;
                return dwProperty == ASM_NAME_NULL_PUBLIC_KEY_TOKEN ||
                       dwProperty == ASM_NAME_NULL_PUBLIC_KEY ||
                       dwProperty == ASM_NAME_NULL_CUSTOM;
            }

            HRESULT ConvertToUtf8(PCWSTR wzStr, __deref_out UTF8** pszStr)
            {
                HRESULT hr = S_OK;

                _ASSERTE(wzStr != nullptr && pszStr != nullptr);
                if (wzStr == nullptr || pszStr == nullptr)
                {
                    return E_INVALIDARG;
                }

                DWORD cbSize = WszWideCharToMultiByte(CP_UTF8, 0, wzStr, -1, NULL, 0, NULL, NULL);
                if(cbSize == 0)
                {
                    return SUCCEEDED(hr = HRESULT_FROM_GetLastError()) ? E_UNEXPECTED : hr;
                }

                NewArrayHolder<UTF8> szStr = new (nothrow) UTF8[cbSize];
                IfNullRet(szStr);

                cbSize = WszWideCharToMultiByte(CP_UTF8, 0, wzStr, -1, static_cast<LPSTR>(szStr), cbSize, NULL, NULL);
                if(cbSize == 0)
                {
                    return SUCCEEDED(hr = HRESULT_FROM_GetLastError()) ? E_UNEXPECTED : hr;
                }

                *pszStr = szStr.Extract();
                return S_OK;
            }
        }

        // Non-allocating helper.
        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, PVOID pBuf, DWORD *pcbBuf)
        {
            LIMITED_METHOD_CONTRACT;
            HRESULT hr = S_OK;

            _ASSERTE(pName != nullptr && pcbBuf != nullptr);
            if (pName == nullptr || pcbBuf == nullptr)
            {
                return E_INVALIDARG;
            }

            hr = pName->GetProperty(dwProperty, pBuf, pcbBuf);
            IfFailRet(hr);

            // Zero-length non-null property means there is no value.
            if (hr == S_OK && *pcbBuf == 0 && !priv::IsNullProperty(dwProperty))
            {
                hr = S_FALSE;
            }

            return hr;
        }

        // Allocating helper.
        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, PBYTE * ppBuf, DWORD *pcbBuf)
        {
            LIMITED_METHOD_CONTRACT;
            HRESULT hr = S_OK;

            _ASSERTE(ppBuf != nullptr && (*ppBuf == nullptr || pcbBuf != nullptr));
            if (ppBuf == nullptr || (*ppBuf != nullptr && pcbBuf == nullptr))
            {
                return E_INVALIDARG;
            }

            DWORD cbBuf = 0;
            if (pcbBuf == nullptr)
                pcbBuf = &cbBuf;
    
            hr = GetProperty(pName, dwProperty, *ppBuf, pcbBuf);

            // No provided buffer constitutes a request for one to be allocated.
            if (*ppBuf == nullptr)
            {
                // If it's a null property, allocate a single-byte array to provide consistency.
                if (hr == S_OK && priv::IsNullProperty(dwProperty))
                {
                    *ppBuf = new (nothrow) BYTE[1];
                    IfNullRet(*ppBuf);
                }
                // Great, get the value.
                else if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
                {
                    NewArrayHolder<BYTE> pBuf = new (nothrow) BYTE[*pcbBuf];
                    IfNullRet(pBuf);
                    hr = pName->GetProperty(dwProperty, pBuf, pcbBuf);
                    IfFailRet(hr);
                    *ppBuf = pBuf.Extract();
                    hr = S_OK;
                }
            }

            return hr;
        }

        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, SString & ssVal)
        {
            LIMITED_METHOD_CONTRACT;
            HRESULT hr = S_OK;

            _ASSERTE(pName != nullptr);
            if (pName == nullptr)
            {
                return E_INVALIDARG;
            }

            DWORD cbSize = 0;
            hr = GetProperty(pName, dwProperty, static_cast<PBYTE>(nullptr), &cbSize);

            if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
            {
                EX_TRY
                {
                    PWSTR wzNameBuf = ssVal.OpenUnicodeBuffer(cbSize / sizeof(WCHAR) - 1);
                    hr = GetProperty(pName, dwProperty, reinterpret_cast<PBYTE>(wzNameBuf), &cbSize);
                    ssVal.CloseBuffer();
                    IfFailThrow(hr);
                    ssVal.Normalize();
                }
                EX_CATCH_HRESULT(hr);
                IfFailRet(hr);
            }

            return hr;
        }

        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, __deref_out WCHAR ** pwzVal)
        {
            LIMITED_METHOD_CONTRACT;
            HRESULT hr = S_OK;

            _ASSERTE(pName != nullptr && pwzVal != nullptr);
            if (pName == nullptr || pwzVal == nullptr)
            {
                return E_INVALIDARG;
            }

            DWORD cbSize = 0;
            hr = pName->GetProperty(dwProperty, NULL, &cbSize);

            if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
            {
                NewArrayHolder<WCHAR> wzVal = reinterpret_cast<PWSTR>(new (nothrow) BYTE[cbSize]);
                IfNullRet(wzVal);
                hr = pName->GetProperty(dwProperty, reinterpret_cast<PBYTE>(static_cast<PWSTR>(wzVal)), &cbSize);
                IfFailRet(hr);
                *pwzVal = wzVal.Extract();
            }

            return hr;
        }

        HRESULT GetProperty(IAssemblyName * pName, DWORD dwProperty, __deref_out UTF8 **pwzOut)
        {
            LIMITED_METHOD_CONTRACT;
            HRESULT hr = S_OK;

            if (pwzOut == nullptr)
                return E_INVALIDARG;

            SmallStackSString ssStr;
            hr = GetProperty(pName, dwProperty, ssStr);
            IfFailRet(hr);
            hr = priv::ConvertToUtf8(ssStr, pwzOut);
            IfFailRet(hr);
            return hr;
        }


    }
}

