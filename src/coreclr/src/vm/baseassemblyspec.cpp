// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// BaseAssemblySpec.cpp
//
// Implements the BaseAssemblySpec class
//


// ============================================================

#include "common.h"
#include "thekey.h"

#include "../binder/inc/fusionassemblyname.hpp"

#include "strongnameinternal.h"
#include "strongnameholders.h"

VOID BaseAssemblySpec::CloneFieldsToStackingAllocator( StackingAllocator* alloc)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END

#if _DEBUG
    DWORD hash = Hash();
#endif

    if ((~m_ownedFlags & NAME_OWNED)  &&
        m_pAssemblyName) {
        S_UINT32 len = S_UINT32((DWORD) strlen(m_pAssemblyName)) + S_UINT32(1);
        if(len.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);
        LPSTR temp = (LPSTR)alloc->Alloc(len);
        strcpy_s(temp, len.Value(), m_pAssemblyName);
        m_pAssemblyName = temp;
    }

    if ((~m_ownedFlags & PUBLIC_KEY_OR_TOKEN_OWNED) &&
        m_pbPublicKeyOrToken && m_cbPublicKeyOrToken > 0) {
        BYTE *temp = (BYTE *)alloc->Alloc(S_UINT32(m_cbPublicKeyOrToken)) ;
        memcpy(temp, m_pbPublicKeyOrToken, m_cbPublicKeyOrToken);
        m_pbPublicKeyOrToken = temp;
    }

    if ((~m_ownedFlags & LOCALE_OWNED)  &&
        m_context.szLocale) {
        S_UINT32 len = S_UINT32((DWORD) strlen(m_context.szLocale)) + S_UINT32(1);
        if(len.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);
        LPSTR temp = (char *)alloc->Alloc(len) ;
        strcpy_s(temp, len.Value(), m_context.szLocale);
        m_context.szLocale = temp;
    }

    if ((~m_ownedFlags & CODEBASE_OWNED)  &&
        m_wszCodeBase) {
        S_UINT32 len = S_UINT32((DWORD) wcslen(m_wszCodeBase)) + S_UINT32(1);
        if(len.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);
        LPWSTR temp = (LPWSTR)alloc->Alloc(len*S_UINT32(sizeof(WCHAR)));
        wcscpy_s(temp, len.Value(), m_wszCodeBase);
        m_wszCodeBase = temp;
    }

    if ((~m_ownedFlags & WINRT_TYPE_NAME_OWNED)) {
        if (m_szWinRtTypeNamespace)
        {
            S_UINT32 len = S_UINT32((DWORD) strlen(m_szWinRtTypeNamespace)) + S_UINT32(1);
            if(len.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);
            LPSTR temp = (LPSTR)alloc->Alloc(len*S_UINT32(sizeof(CHAR)));
            strcpy_s(temp, len.Value(), m_szWinRtTypeNamespace);
            m_szWinRtTypeNamespace = temp;
        }

        if (m_szWinRtTypeClassName)
        {
            S_UINT32 len = S_UINT32((DWORD) strlen(m_szWinRtTypeClassName)) + S_UINT32(1);
            if(len.IsOverflow()) COMPlusThrowHR(COR_E_OVERFLOW);
            LPSTR temp = (LPSTR)alloc->Alloc(len*S_UINT32(sizeof(CHAR)));
            strcpy_s(temp, len.Value(), m_szWinRtTypeClassName);
            m_szWinRtTypeClassName = temp;
        }
    }

    _ASSERTE(hash == Hash());

}

#ifndef DACCESS_COMPILE
BOOL BaseAssemblySpec::IsMscorlib()
{
    CONTRACTL
    {
        THROWS;
        INSTANCE_CHECK;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (m_pAssemblyName == NULL)
    {
        LPCWSTR file = GetCodeBase();
        if (file)
        {
            StackSString path(file);
            PEAssembly::UrlToPath(path);
            return SystemDomain::System()->IsBaseLibrary(path);
        }
        return FALSE;
    }

    _ASSERTE(strlen(g_psBaseLibraryName) == CoreLibNameLen);

    // <TODO>More of bug 213471</TODO>
    size_t iNameLen = strlen(m_pAssemblyName);
    return ( (iNameLen >= CoreLibNameLen) &&
             ( (!stricmpUTF8(m_pAssemblyName, g_psBaseLibrary)) ||
             ( (!SString::_strnicmp(m_pAssemblyName, g_psBaseLibraryName, CoreLibNameLen)) &&
               ( (iNameLen == CoreLibNameLen) || (m_pAssemblyName[CoreLibNameLen] == ',') ) ) ) );
}

BOOL BaseAssemblySpec::IsAssemblySpecForMscorlib()
{
    CONTRACTL
    {
        NOTHROW;
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(strlen(g_psBaseLibraryName) == CoreLibNameLen);
    }
    CONTRACTL_END;

    BOOL fIsAssemblySpecForMscorlib = FALSE;

    if (m_pAssemblyName)
    {
        size_t iNameLen = strlen(m_pAssemblyName);
        fIsAssemblySpecForMscorlib = ( (iNameLen >= CoreLibNameLen) &&
                 ( (!_stricmp(m_pAssemblyName, g_psBaseLibrary)) ||
                 ( (!_strnicmp(m_pAssemblyName, g_psBaseLibraryName, CoreLibNameLen)) &&
                   ( (iNameLen == CoreLibNameLen) || (m_pAssemblyName[CoreLibNameLen] == ',') ) ) ) );
    }

    return fIsAssemblySpecForMscorlib;
}

#define MSCORLIB_PUBLICKEY g_rbTheSilverlightPlatformKey


// A satellite assembly for mscorlib is named "mscorlib.resources" or
// mscorlib.debug.resources.dll and uses the same public key as mscorlib.
// It does not necessarily have the same version, and the Culture will
// always be set to something like "jp-JP".
BOOL BaseAssemblySpec::IsMscorlibSatellite() const
{
    CONTRACTL
    {
        THROWS;
        INSTANCE_CHECK;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_pAssemblyName == NULL)
    {
        LPCWSTR file = GetCodeBase();
        if (file)
        {
            StackSString path(file);
            PEAssembly::UrlToPath(path);
            return SystemDomain::System()->IsBaseLibrarySatellite(path);
        }
        return FALSE;
    }

    _ASSERTE(strlen(g_psBaseLibrarySatelliteAssemblyName) == CoreLibSatelliteNameLen);

    // <TODO>More of bug 213471</TODO>
    size_t iNameLen = strlen(m_pAssemblyName);

    // we allow name to be of the form mscorlib.resources.dll only
    BOOL r = ( (m_cbPublicKeyOrToken == sizeof(MSCORLIB_PUBLICKEY)) &&
             (iNameLen >= CoreLibSatelliteNameLen) &&
             (!SString::_strnicmp(m_pAssemblyName, g_psBaseLibrarySatelliteAssemblyName, CoreLibSatelliteNameLen)) &&
             ( (iNameLen == CoreLibSatelliteNameLen) || (m_pAssemblyName[CoreLibSatelliteNameLen] == ',') ) );

    r = r && ( memcmp(m_pbPublicKeyOrToken,MSCORLIB_PUBLICKEY,sizeof(MSCORLIB_PUBLICKEY)) == 0);

    return r;
}

VOID BaseAssemblySpec::ConvertPublicKeyToToken()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(HasPublicKey());
    }
    CONTRACTL_END;

    StrongNameBufferHolder<BYTE> pbPublicKeyToken;
    DWORD cbPublicKeyToken;
    IfFailThrow(StrongNameTokenFromPublicKey(m_pbPublicKeyOrToken,
        m_cbPublicKeyOrToken,
        &pbPublicKeyToken,
        &cbPublicKeyToken));

    BYTE *temp = new BYTE [cbPublicKeyToken];
    memcpy(temp, pbPublicKeyToken, cbPublicKeyToken);

    if (m_ownedFlags & PUBLIC_KEY_OR_TOKEN_OWNED)
        delete [] m_pbPublicKeyOrToken;
    else
        m_ownedFlags |= PUBLIC_KEY_OR_TOKEN_OWNED;

    m_pbPublicKeyOrToken = temp;
    m_cbPublicKeyOrToken = cbPublicKeyToken;
    m_dwFlags &= ~afPublicKey;
}

// Similar to BaseAssemblySpec::CompareEx, but allows the ref to be partially specified
// Returns TRUE if ref matches def, FALSE otherwise.
//
// static
BOOL BaseAssemblySpec::CompareRefToDef(const BaseAssemblySpec *pRef, const BaseAssemblySpec *pDef)
{
    WRAPPER_NO_CONTRACT;

    if(pRef->m_wszCodeBase || pDef->m_wszCodeBase)
    {
        if(!pRef->m_wszCodeBase || !pDef->m_wszCodeBase)
            return FALSE;

        return wcscmp(pRef->m_wszCodeBase,(pDef->m_wszCodeBase)) == 0;
    }

    // Compare fields

    //
    // name is non-optional
    //
    if (pRef->m_pAssemblyName != pDef->m_pAssemblyName
        && (pRef->m_pAssemblyName == NULL || pDef->m_pAssemblyName == NULL
            || CompareStrings(pRef->m_pAssemblyName, pDef->m_pAssemblyName)))
    {
        return FALSE;
    }

    //
    // public key [token] is non-optional
    //
    if (pRef->m_cbPublicKeyOrToken != pDef->m_cbPublicKeyOrToken
        || memcmp(pRef->m_pbPublicKeyOrToken, pDef->m_pbPublicKeyOrToken, pRef->m_cbPublicKeyOrToken))
    {
        return FALSE;
    }

    //
    // flags are non-optional, except processor architecture, content type, and debuggable attribute bits
    //
    DWORD dwFlagsMask = ~(afPA_FullMask | afContentType_Mask | afDebuggableAttributeMask);
    if ((pRef->m_dwFlags & dwFlagsMask) != (pDef->m_dwFlags & dwFlagsMask))
        return FALSE;

    // To match Fusion behavior, we ignore processor architecture (GetAssemblyNameRefFromMDImport
    // does not look at architecture part of the flags, and having processor architecture in
    // InternalsVisibleTo attribute causess META_E_CA_BAD_FRIENDS_ARGS exception).
    // Content type is optional in pRef.
    if (!IsAfContentType_Default(pRef->m_dwFlags) && (pRef->m_dwFlags & afContentType_Mask) != (pDef->m_dwFlags & afContentType_Mask))
        return FALSE;


    //
    // version info is optional in the ref
    //
    if (pRef->m_context.usMajorVersion != (USHORT) -1)
    {
        if (pRef->m_context.usMajorVersion != pDef->m_context.usMajorVersion)
            return FALSE;

        if (pRef->m_context.usMinorVersion != (USHORT) -1)
        {
            if (pRef->m_context.usMinorVersion != pDef->m_context.usMinorVersion)
                return FALSE;

            if (pRef->m_context.usBuildNumber != (USHORT) -1)
            {
                if (pRef->m_context.usBuildNumber != pDef->m_context.usBuildNumber)
                    return FALSE;

                if (pRef->m_context.usRevisionNumber != (USHORT) -1)
                {
                    if (pRef->m_context.usRevisionNumber != pDef->m_context.usRevisionNumber)
                        return FALSE;
                }
            }
        }
    }

    //
    // locale info is optional in the ref
    //
    if ((pRef->m_context.szLocale != NULL)
        && (pRef->m_context.szLocale != pDef->m_context.szLocale)
        && strcmp(pRef->m_context.szLocale, pDef->m_context.szLocale))
    {
        return FALSE;
    }

    return TRUE;
}

// static
BOOL BaseAssemblySpec::RefMatchesDef(const BaseAssemblySpec* pRef, const BaseAssemblySpec* pDef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pRef->GetName()!=NULL && pDef->GetName()!=NULL);
    }
    CONTRACTL_END;

    if (pRef->IsStrongNamed())
    {
        if (!pDef->IsStrongNamed())
            return FALSE;

        if(pRef->HasPublicKey())
        {
            // cannot use pRef->CompareEx(pDef) here because it does a full comparison
            // and the ref may be partial.
            return CompareRefToDef(pRef, pDef);
        }
        else
        {
            BaseAssemblySpec defCopy;
            defCopy.CopyFrom(pDef);
            defCopy.ConvertPublicKeyToToken();

            return CompareRefToDef(pRef, &defCopy);
        }
    }
    else
    {
        return (CompareStrings(pRef->GetName(), pDef->GetName())==0);
    }
}

//===========================================================================================
// This function may embed additional information, if required.
//
// For WinRT (ContentType=WindowsRuntime) assembly specs, this will embed the type name in
// the IAssemblyName's ASM_NAME_NAME property; otherwise this just creates an IAssemblyName
// for the provided assembly spec.

void BaseAssemblySpec::GetEncodedName(SString & ssEncodedName) const
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END

#ifdef FEATURE_COMINTEROP
    if (IsContentType_WindowsRuntime() && GetWinRtTypeClassName() != NULL)
    {
        ssEncodedName.SetUTF8(GetName());
        ssEncodedName.Append(SL(W("!")));
        if (GetWinRtTypeNamespace() != NULL)
        {
            ssEncodedName.AppendUTF8(GetWinRtTypeNamespace());
            ssEncodedName.Append(SL(W(".")));
        }
        ssEncodedName.AppendUTF8(GetWinRtTypeClassName());
    }
    else
#endif
    {
        ssEncodedName.SetUTF8(m_pAssemblyName);
    }
}

VOID BaseAssemblySpec::SetName(SString const & ssName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        THROWS;
    }
    CONTRACTL_END;

    if (m_ownedFlags & NAME_OWNED)
    {
        delete [] m_pAssemblyName;
        m_ownedFlags &= ~NAME_OWNED;
    }

    m_pAssemblyName = NULL;

    IfFailThrow(FString::ConvertUnicode_Utf8(ssName.GetUnicode(), & ((LPSTR &) m_pAssemblyName)));

    m_ownedFlags |= NAME_OWNED;
}

HRESULT BaseAssemblySpec::Init(IAssemblyName *pName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pName);

    HRESULT hr;

    // Fill out info from name, if we have it.

    DWORD cbSize = 0;
    hr=pName->GetProperty(ASM_NAME_NAME, NULL, &cbSize);
    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) {
        hr=S_OK;
        CQuickBytes qb;
        LPWSTR pwName = (LPWSTR) qb.AllocNoThrow(cbSize);
        if (!pwName)
            return E_OUTOFMEMORY;

        IfFailRet(pName->GetProperty(ASM_NAME_NAME, pwName, &cbSize));

        m_pAssemblyName = NULL;

        hr = FString::ConvertUnicode_Utf8(pwName, & ((LPSTR &) m_pAssemblyName));

        if (FAILED(hr))
        {
            return hr;
        }

        m_ownedFlags |= NAME_OWNED;
    }
    IfFailRet(hr);

    // Note: cascade checks so we don't set lower priority version #'s if higher ones are missing
    cbSize = sizeof(m_context.usMajorVersion);
    hr=pName->GetProperty(ASM_NAME_MAJOR_VERSION, &m_context.usMajorVersion, &cbSize);

    if (hr!=S_OK || !cbSize)
        m_context.usMajorVersion = (USHORT) -1;
    else {
        cbSize = sizeof(m_context.usMinorVersion);
        hr=pName->GetProperty(ASM_NAME_MINOR_VERSION, &m_context.usMinorVersion, &cbSize);
    }

    if (hr!=S_OK || !cbSize)
        m_context.usMinorVersion = (USHORT) -1;
    else {
        cbSize = sizeof(m_context.usBuildNumber);
        pName->GetProperty(ASM_NAME_BUILD_NUMBER, &m_context.usBuildNumber, &cbSize);
    }

    if (hr!=S_OK || !cbSize)
        m_context.usBuildNumber = (USHORT) -1;
    else {
        cbSize = sizeof(m_context.usRevisionNumber);
        pName->GetProperty(ASM_NAME_REVISION_NUMBER, &m_context.usRevisionNumber, &cbSize);
    }

    if (hr!=S_OK || !cbSize)
        m_context.usRevisionNumber = (USHORT) -1;

    if (hr==E_INVALIDARG)
        hr=S_FALSE;

    IfFailRet(hr);

    cbSize = 0;
    hr = pName->GetProperty(ASM_NAME_CULTURE, NULL, &cbSize);

    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) {
        LPWSTR pwName = (LPWSTR) alloca(cbSize);
        IfFailRet(pName->GetProperty(ASM_NAME_CULTURE, pwName, &cbSize));

        hr = FString::ConvertUnicode_Utf8(pwName, & ((LPSTR &) m_context.szLocale));

        m_ownedFlags |= LOCALE_OWNED;
    }

    IfFailRet(hr);

    m_dwFlags = 0;

    cbSize = 0;
    hr=pName->GetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, NULL, &cbSize);
    if (hr== HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) {
        m_pbPublicKeyOrToken = new (nothrow) BYTE[cbSize];
        if (m_pbPublicKeyOrToken == NULL)
            return E_OUTOFMEMORY;
        m_cbPublicKeyOrToken = cbSize;
        m_ownedFlags |= PUBLIC_KEY_OR_TOKEN_OWNED;
        IfFailRet(pName->GetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, m_pbPublicKeyOrToken, &cbSize));
    }
    else {
        if (hr!=E_INVALIDARG)
            IfFailRet(hr);
        hr=pName->GetProperty(ASM_NAME_PUBLIC_KEY, NULL, &cbSize);
        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) {
            hr=S_OK;
            // <TODO>@todo: we need to normalize this into a public key token so
            // comparisons work correctly. But this involves binding to mscorsn.</TODO>
            m_pbPublicKeyOrToken = new (nothrow) BYTE[cbSize];
            if (m_pbPublicKeyOrToken == NULL)
                return E_OUTOFMEMORY;
            m_cbPublicKeyOrToken = cbSize;
            m_dwFlags |= afPublicKey;
            m_ownedFlags |= PUBLIC_KEY_OR_TOKEN_OWNED;
            IfFailRet(pName->GetProperty(ASM_NAME_PUBLIC_KEY, m_pbPublicKeyOrToken, &cbSize));
        }
        else {
            IfFailRet(hr);
            hr= pName->GetProperty(ASM_NAME_NULL_PUBLIC_KEY, NULL, &cbSize);
            if (hr!=S_OK)
                hr=pName->GetProperty(ASM_NAME_NULL_PUBLIC_KEY_TOKEN, NULL, &cbSize);
            if ( hr == S_OK ) {
                m_pbPublicKeyOrToken = new (nothrow) BYTE[0];
                if (m_pbPublicKeyOrToken == NULL)
                    return E_OUTOFMEMORY;
                m_cbPublicKeyOrToken = 0;
                m_ownedFlags |= PUBLIC_KEY_OR_TOKEN_OWNED;
            }
            if (hr==E_INVALIDARG)
                hr=S_FALSE;
            IfFailRet(hr);

        }
    }

    // Recover the afRetargetable flag
    BOOL bRetarget;
    cbSize = sizeof(bRetarget);
    hr = pName->GetProperty(ASM_NAME_RETARGET, &bRetarget, &cbSize);
    if (hr == S_OK && cbSize != 0 && bRetarget)
        m_dwFlags |= afRetargetable;

    // Recover the Processor Architecture flags
    PEKIND peKind;
    cbSize = sizeof(PEKIND);
    hr = pName->GetProperty(ASM_NAME_ARCHITECTURE, &peKind, &cbSize);
    if ((hr == S_OK) && (cbSize != 0) && (peKind < (afPA_NoPlatform >> afPA_Shift)) && (peKind >= (afPA_MSIL >> afPA_Shift)))
        m_dwFlags |= (((DWORD)peKind) << afPA_Shift);

    cbSize = 0;
    hr=pName->GetProperty(ASM_NAME_CODEBASE_URL, NULL, &cbSize);
    if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)) {
        m_wszCodeBase = new (nothrow) WCHAR [ cbSize/sizeof(WCHAR) ];
        if (m_wszCodeBase == NULL)
            return E_OUTOFMEMORY;
        m_ownedFlags |= CODE_BASE_OWNED;
        IfFailRet(pName->GetProperty(ASM_NAME_CODEBASE_URL,
                                    (void*)m_wszCodeBase, &cbSize));
    }
    else
        IfFailRet(hr);

    // Recover the Content Type enum
    DWORD dwContentType;
    cbSize = sizeof(dwContentType);
    hr = pName->GetProperty(ASM_NAME_CONTENT_TYPE, &dwContentType, &cbSize);
    if ((hr == S_OK) && (cbSize == sizeof(dwContentType)))
    {
        _ASSERTE((dwContentType == AssemblyContentType_Default) || (dwContentType == AssemblyContentType_WindowsRuntime));
        if (dwContentType == AssemblyContentType_WindowsRuntime)
        {
            m_dwFlags |= afContentType_WindowsRuntime;
        }
    }

    return S_OK;
}

HRESULT BaseAssemblySpec::CreateFusionName(
    IAssemblyName **ppName,
    BOOL fIncludeCodeBase/*=TRUE*/,
    BOOL fMustBeBindable /*=FALSE*/) const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        IAssemblyName *pFusionAssemblyName = NULL;
        LPWSTR pwLocale = NULL;
        CQuickBytes qb;

        NonVMComHolder< IAssemblyName > holder(NULL);

        SmallStackSString ssAssemblyName;
        fMustBeBindable ? GetEncodedName(ssAssemblyName) : GetName(ssAssemblyName);

        IfFailGo(CreateAssemblyNameObject(&pFusionAssemblyName, ssAssemblyName.GetUnicode(), false /*parseDisplayName*/));

        holder = pFusionAssemblyName;

        if (m_context.usMajorVersion != (USHORT) -1) {
            IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_MAJOR_VERSION,
                                                      &m_context.usMajorVersion,
                                                      sizeof(USHORT)));

            if (m_context.usMinorVersion != (USHORT) -1) {
                IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_MINOR_VERSION,
                                                          &m_context.usMinorVersion,
                                                          sizeof(USHORT)));

                if (m_context.usBuildNumber != (USHORT) -1) {
                    IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_BUILD_NUMBER,
                                                              &m_context.usBuildNumber,
                                                              sizeof(USHORT)));

                    if (m_context.usRevisionNumber != (USHORT) -1)
                        IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_REVISION_NUMBER,
                                                                  &m_context.usRevisionNumber,
                                                                  sizeof(USHORT)));
                }
            }
        }

        if (m_context.szLocale) {
            int pwLocaleLen = WszMultiByteToWideChar(CP_UTF8, 0, m_context.szLocale, -1, 0, 0);
            if(pwLocaleLen == 0) {
                IfFailGo(HRESULT_FROM_GetLastError());
            } else if (pwLocaleLen > MAKE_MAX_LENGTH) {
                IfFailGo(COR_E_OVERFLOW);
            }
            pwLocale = (LPWSTR) qb.AllocNoThrow((pwLocaleLen + 1) *sizeof(WCHAR));
            if (!pwLocaleLen)
                IfFailGo(E_OUTOFMEMORY);
            if (!WszMultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS,
                                        m_context.szLocale, -1, pwLocale, pwLocaleLen))
                IfFailGo(HRESULT_FROM_GetLastError());
            pwLocale[pwLocaleLen] = 0;

            IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_CULTURE,
                                                      pwLocale,
                                                      (DWORD)(wcslen(pwLocale) + 1) * sizeof (WCHAR)));
        }

        if (m_pbPublicKeyOrToken) {
            if (m_cbPublicKeyOrToken) {
                if(m_dwFlags & afPublicKey) {
                    IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_PUBLIC_KEY,
                                                              m_pbPublicKeyOrToken, m_cbPublicKeyOrToken));
                }
                else {
                        IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_PUBLIC_KEY_TOKEN,
                                                                  m_pbPublicKeyOrToken, m_cbPublicKeyOrToken));
                }
            }
            else {
            }
        }


        // Set the Processor Architecture (if any)
        {
            DWORD dwPEkind = (DWORD)PAIndex(m_dwFlags);
            // Note: Value 0x07 = code:afPA_NoPlatform falls through
            if ((dwPEkind >= peMSIL) && (dwPEkind <= peARM))
            {
                PEKIND peKind = (PEKIND)dwPEkind;
                IfFailGo(pFusionAssemblyName->SetProperty(ASM_NAME_ARCHITECTURE,
                                                          &peKind, sizeof(peKind)));
            }
        }

        // Set the Content Type (if any)
        {
            if (IsAfContentType_WindowsRuntime(m_dwFlags))
            {
                DWORD dwContentType = AssemblyContentType_WindowsRuntime;
                IfFailGo(pFusionAssemblyName->SetProperty(
                        ASM_NAME_CONTENT_TYPE,
                        &dwContentType,
                        sizeof(dwContentType)));
            }
        }

        _ASSERTE(m_wszCodeBase == NULL);

        *ppName = pFusionAssemblyName;

        holder.SuppressRelease();
        hr = S_OK;

    ErrExit:
        ;
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

#endif // !DACCESS_COMPILE
