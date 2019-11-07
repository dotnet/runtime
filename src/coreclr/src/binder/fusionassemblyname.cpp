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
// CAssemblyName::SetPropertyInternal
// ---------------------------------------------------------------------------
HRESULT CAssemblyName::SetPropertyInternal(DWORD  PropertyId,
                                           LPCVOID pvProperty,
                                           DWORD  cbProperty)
{
    HRESULT hr = S_OK;
    LPBYTE pbSN = NULL;
    DWORD  cbSN = 0;

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
// CreateAssemblyNameObject
// ---------------------------------------------------------------------------

// This is not external for CoreCLR
STDAPI
CreateAssemblyNameObject(
    LPASSEMBLYNAME    *ppAssemblyName,
    LPCOLESTR          szAssemblyName,
    bool               parseDisplayName)
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

    if (parseDisplayName)
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
    ASSEMBLYMETADATA  *pamd)
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

