// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// BaseAssemblySpec.inl
//


//
// Implements the BaseAssemblySpec class
//
// ============================================================

#ifndef __BASE_ASSEMBLY_SPEC_INL__
#define __BASE_ASSEMBLY_SPEC_INL__

inline int BaseAssemblySpec::CompareStrings(LPCUTF8 string1, LPCUTF8 string2)
{
    WRAPPER_NO_CONTRACT;
    SString s1;
    SString s2;
    s1.SetUTF8(string1);
    s2.SetUTF8(string2);
    return s1.CompareCaseInsensitive(s2);
}


inline BaseAssemblySpec::BaseAssemblySpec()
{
    LIMITED_METHOD_CONTRACT;
    ZeroMemory(this, sizeof(*this));
    m_context.usMajorVersion = (USHORT) -1;
    m_context.usMinorVersion = (USHORT) -1;
    m_context.usBuildNumber = (USHORT) -1;
    m_context.usRevisionNumber = (USHORT) -1;
};

inline BaseAssemblySpec::~BaseAssemblySpec()
{
    WRAPPER_NO_CONTRACT;
    if (m_ownedFlags & NAME_OWNED)
        delete [] m_pAssemblyName;
    if (m_ownedFlags & PUBLIC_KEY_OR_TOKEN_OWNED)
        delete [] m_pbPublicKeyOrToken;
    if (m_ownedFlags & LOCALE_OWNED)
        delete [] m_context.szLocale;
}

inline void BaseAssemblySpec::Init(LPCSTR pAssemblyName,
                         const AssemblyMetaDataInternal* pContext,
                         const BYTE * pbPublicKeyOrToken, DWORD cbPublicKeyOrToken,
                         DWORD dwFlags)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pContext);

    m_pAssemblyName = pAssemblyName;
    m_pbPublicKeyOrToken = const_cast<BYTE *>(pbPublicKeyOrToken);
    m_cbPublicKeyOrToken = cbPublicKeyOrToken;
    m_dwFlags = dwFlags;
    m_ownedFlags = 0;

    m_context = *pContext;
}

inline VOID BaseAssemblySpec::CloneFields()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END

#if _DEBUG
    DWORD hash = Hash();
#endif

    if ((~m_ownedFlags & NAME_OWNED) &&
        m_pAssemblyName) {
        size_t len = strlen(m_pAssemblyName) + 1;
        LPSTR temp = new char [len];
        strcpy_s(temp, len, m_pAssemblyName);
        m_pAssemblyName = temp;
        m_ownedFlags |= NAME_OWNED;
    }

    if ((~m_ownedFlags & PUBLIC_KEY_OR_TOKEN_OWNED) &&
        m_pbPublicKeyOrToken) {
        BYTE *temp = new BYTE [m_cbPublicKeyOrToken];
        memcpy(temp, m_pbPublicKeyOrToken, m_cbPublicKeyOrToken);
        m_pbPublicKeyOrToken = temp;
        m_ownedFlags |= PUBLIC_KEY_OR_TOKEN_OWNED;
    }

    if ((~m_ownedFlags & LOCALE_OWNED) &&
        m_context.szLocale) {
        size_t len = strlen(m_context.szLocale) + 1;
        LPSTR temp = new char [len];
        strcpy_s(temp, len, m_context.szLocale);
        m_context.szLocale = temp;
        m_ownedFlags |= LOCALE_OWNED;
    }

    _ASSERTE(hash == Hash());
}

inline VOID BaseAssemblySpec::CloneFieldsToLoaderHeap(LoaderHeap *pHeap, AllocMemTracker *pamTracker)
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

    if ((~m_ownedFlags & NAME_OWNED) &&
        m_pAssemblyName) {
        size_t len = strlen(m_pAssemblyName) + 1;
        LPSTR temp = (LPSTR)pamTracker->Track( pHeap->AllocMem(S_SIZE_T (len)) );
        strcpy_s(temp, len, m_pAssemblyName);
        m_pAssemblyName = temp;
    }

    if ((~m_ownedFlags & PUBLIC_KEY_OR_TOKEN_OWNED) &&
        m_pbPublicKeyOrToken && m_cbPublicKeyOrToken > 0) {
        BYTE *temp = (BYTE *)pamTracker->Track( pHeap->AllocMem(S_SIZE_T (m_cbPublicKeyOrToken)) );
        memcpy(temp, m_pbPublicKeyOrToken, m_cbPublicKeyOrToken);
        m_pbPublicKeyOrToken = temp;
    }

    if ((~m_ownedFlags & LOCALE_OWNED) &&
        m_context.szLocale) {
        size_t len = strlen(m_context.szLocale) + 1;
        LPSTR temp = (char *)pamTracker->Track( pHeap->AllocMem(S_SIZE_T (len)) );
        strcpy_s(temp, len, m_context.szLocale);
        m_context.szLocale = temp;
    }

    _ASSERTE(hash == Hash());

}


inline void BaseAssemblySpec::CopyFrom(const BaseAssemblySpec *pSpec)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END

    m_pAssemblyName = pSpec->m_pAssemblyName;

    m_pbPublicKeyOrToken = pSpec->m_pbPublicKeyOrToken;
    m_cbPublicKeyOrToken = pSpec->m_cbPublicKeyOrToken;
    m_dwFlags = pSpec->m_dwFlags;
    m_ownedFlags = 0;

    m_context = pSpec->m_context;

    if ((pSpec->m_ownedFlags & BAD_NAME_OWNED) != 0)
    {
        m_ownedFlags |= BAD_NAME_OWNED;
    }


    m_pBinder = pSpec->m_pBinder;

}


inline DWORD BaseAssemblySpec::Hash()
{
    CONTRACTL {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    // Hash fields.
    DWORD hash = 0;

    if (m_pAssemblyName)
        hash ^= HashStringA(m_pAssemblyName);
    hash = _rotl(hash, 4);

    hash ^= HashBytes(m_pbPublicKeyOrToken, m_cbPublicKeyOrToken);
    hash = _rotl(hash, 4);

    hash ^= m_dwFlags;
    hash = _rotl(hash, 4);


    hash ^= m_context.usMajorVersion;
    hash = _rotl(hash, 8);

    if (m_context.usMajorVersion != (USHORT) -1) {
        hash ^= m_context.usMinorVersion;
        hash = _rotl(hash, 8);

        if (m_context.usMinorVersion != (USHORT) -1) {
            hash ^= m_context.usBuildNumber;
            hash = _rotl(hash, 8);

            if (m_context.usBuildNumber != (USHORT) -1) {
                hash ^= m_context.usRevisionNumber;
                hash = _rotl(hash, 8);
            }
        }
    }

    if (m_context.szLocale)
        hash ^= HashStringA(m_context.szLocale);
    hash = _rotl(hash, 4);

    return hash;
}


inline BOOL BaseAssemblySpec::CompareEx(BaseAssemblySpec *pSpec, DWORD dwCompareFlags)
{
    WRAPPER_NO_CONTRACT;

    // Compare fields

    if (m_pAssemblyName != pSpec->m_pAssemblyName
        && (m_pAssemblyName == NULL || pSpec->m_pAssemblyName == NULL
            || strcmp(m_pAssemblyName, pSpec->m_pAssemblyName)))
        return FALSE;

    if (m_cbPublicKeyOrToken != pSpec->m_cbPublicKeyOrToken
        || memcmp(m_pbPublicKeyOrToken, pSpec->m_pbPublicKeyOrToken, m_cbPublicKeyOrToken))
        return FALSE;


    if (m_dwFlags != pSpec->m_dwFlags)
        return FALSE;

    if (m_context.usMajorVersion != pSpec->m_context.usMajorVersion)
        return FALSE;

    if (m_context.usMajorVersion != (USHORT) -1) {
        if (m_context.usMinorVersion != pSpec->m_context.usMinorVersion)
            return FALSE;

        if (m_context.usMinorVersion != (USHORT) -1) {
            if (m_context.usBuildNumber != pSpec->m_context.usBuildNumber)
                return FALSE;

            if (m_context.usBuildNumber != (USHORT) -1) {
                if (m_context.usRevisionNumber != pSpec->m_context.usRevisionNumber)
                    return FALSE;
            }
        }
    }

    if (m_context.szLocale != pSpec->m_context.szLocale
        && (m_context.szLocale == NULL || pSpec->m_context.szLocale == NULL
            || strcmp(m_context.szLocale, pSpec->m_context.szLocale)))
        return FALSE;


    // If the assemblySpec contains the binding context, then check if they match.
    if (pSpec->m_pBinder != m_pBinder)
    {
        return FALSE;
    }

    return TRUE;
}


inline HRESULT BaseAssemblySpec::Init(mdToken kAssemblyToken,
                                  IMDInternalImport *pImport)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr;
    if (TypeFromToken(kAssemblyToken) == mdtAssembly) {

        IfFailRet(pImport->GetAssemblyProps(kAssemblyToken,
                                  (const void **) &m_pbPublicKeyOrToken,
                                  &m_cbPublicKeyOrToken,
                                  NULL,
                                  &m_pAssemblyName,
                                  &m_context,
                                  &m_dwFlags));

        if (m_cbPublicKeyOrToken != 0)
            m_dwFlags |= afPublicKey;
    }
    else
        IfFailRet(pImport->GetAssemblyRefProps(kAssemblyToken,
                                     (const void**) &m_pbPublicKeyOrToken,
                                     &m_cbPublicKeyOrToken,
                                     &m_pAssemblyName,
                                     &m_context,
                                     NULL,
                                     NULL,
                                     &m_dwFlags));

    // When m_cbPublicKeyOrToken is 0, a NULL in m_pbPublicKeyOrToken indicates that public key or token
    // is not specified, while a non-NULL in m_pbPublicKeyOrToken indicates an empty public key (i.e.,
    // a non-strongnamed assembly).  However, the MetaData API puts a random value in m_pbPublicKeyOrToken
    // when m_cbPublicKeyOrToken is 0.  Since AssemblyDef or AssemblyRef can't using partial name, we
    // always ensure that m_pbPublicKeyOrToken is not NULL.
    if (m_cbPublicKeyOrToken == 0)
        m_pbPublicKeyOrToken = (PBYTE)1;

    return S_OK;
}

inline HRESULT BaseAssemblySpec::Init(mdToken tkAssemblyRef,
                                  IMetaDataAssemblyImport  *pImport)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Retrieve size of assembly name
    ASSEMBLYMETADATA sContext;
    LPWSTR wszAssemblyName=NULL;
    ZeroMemory(&sContext, sizeof(ASSEMBLYMETADATA));
    HRESULT hr = S_OK;
    if(TypeFromToken(tkAssemblyRef) == mdtAssembly)
    {
        DWORD cchName;
        IfFailRet(pImport->GetAssemblyProps(tkAssemblyRef,    // [IN] The Assembly for which to get the properties.
                                            NULL,        // [OUT] Pointer to the public key or token.
                                            NULL,        // [OUT] Count of bytes in the public key or token.
                                            NULL,        // [OUT] Hash Algorithm
                                            NULL,        // [OUT] Buffer to fill with name.
                                            NULL,        // [IN] Size of buffer in wide chars.
                                            &cchName,    // [OUT] Actual # of wide chars in name.
                                            &sContext,   // [OUT] Assembly MetaData.
                                            NULL));       // [OUT] Flags.

        // Get the assembly name other naming properties
        wszAssemblyName = (LPWSTR)_alloca(cchName * sizeof(WCHAR));
        IfFailRet(pImport->GetAssemblyProps(tkAssemblyRef,
                                            (const void **)&m_pbPublicKeyOrToken,
                                            &m_cbPublicKeyOrToken,
                                            NULL,
                                            wszAssemblyName,
                                            cchName,
                                            &cchName,
                                            &sContext,
                                            &m_dwFlags));
    }
    else if(TypeFromToken(tkAssemblyRef) == mdtAssemblyRef)
    {
        DWORD cchName;
        IfFailRet(pImport->GetAssemblyRefProps(tkAssemblyRef, // [IN] The AssemblyRef for which to get the properties.
                                            NULL,        // [OUT] Pointer to the public key or token.
                                            NULL,        // [OUT] Count of bytes in the public key or token.
                                            NULL,        // [OUT] Buffer to fill with name.
                                            NULL,        // [IN] Size of buffer in wide chars.
                                            &cchName,    // [OUT] Actual # of wide chars in name.
                                            &sContext,   // [OUT] Assembly MetaData.
                                            NULL,        // [OUT] Hash blob.
                                            NULL,        // [OUT] Count of bytes in the hash blob.
                                            NULL));       // [OUT] Flags.

        // Get the assembly name other naming properties
        wszAssemblyName = (LPWSTR)_alloca(cchName * sizeof(WCHAR));
        IfFailRet(pImport->GetAssemblyRefProps(tkAssemblyRef,
                                            (const void **)&m_pbPublicKeyOrToken,
                                            &m_cbPublicKeyOrToken,
                                            wszAssemblyName,
                                            cchName,
                                            &cchName,
                                            &sContext,
                                            NULL,
                                            NULL,
                                            &m_dwFlags));
    }
    else
    {
        _ASSERTE(false && "unexpected token");
    }
    MAKE_UTF8PTR_FROMWIDE_NOTHROW(szAssemblyName,wszAssemblyName);
    IfNullRet(szAssemblyName);
    size_t len=strlen(szAssemblyName)+1;
    NewArrayHolder<char> assemblyName(new(nothrow) char[len]);
    IfNullRet(assemblyName);
    strcpy_s(assemblyName,len,szAssemblyName);

    m_pAssemblyName=assemblyName.Extract();
    SetContext(&sContext);
    return S_OK;
}

inline void BaseAssemblySpec::SetName(LPCSTR szName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    if (m_pAssemblyName && (m_ownedFlags & NAME_OWNED))
        delete [] m_pAssemblyName;
    m_ownedFlags &= ~NAME_OWNED;
    m_pAssemblyName = szName;
}

inline void BaseAssemblySpec::SetCulture(LPCSTR szCulture)
{
    LIMITED_METHOD_CONTRACT;
    if (m_context.szLocale && (m_ownedFlags & LOCALE_OWNED))
        delete [] m_context.szLocale;
    m_ownedFlags &= ~LOCALE_OWNED;
    if (strcmp(szCulture,"neutral")==0)
        m_context.szLocale="";
    else
        m_context.szLocale=szCulture;
}

inline void BaseAssemblySpec::SetContext(ASSEMBLYMETADATA* assemblyData)
{
    LIMITED_METHOD_CONTRACT;
    m_context.usMajorVersion=assemblyData->usMajorVersion;
    m_context.usMinorVersion=assemblyData->usMinorVersion;
    m_context.usBuildNumber=assemblyData->usBuildNumber;
    m_context.usRevisionNumber=assemblyData->usRevisionNumber;
    m_context.szLocale="";
};

inline BOOL BaseAssemblySpec::IsStrongNamed() const
{
    LIMITED_METHOD_CONTRACT;
    return m_cbPublicKeyOrToken;
}

inline BOOL BaseAssemblySpec::HasPublicKey() const
{
    LIMITED_METHOD_CONTRACT;
    return IsAfPublicKey(m_dwFlags) && m_cbPublicKeyOrToken != 0;
}

inline BOOL BaseAssemblySpec::HasPublicKeyToken() const
{
    LIMITED_METHOD_CONTRACT;
    return IsAfPublicKeyToken(m_dwFlags) && m_cbPublicKeyOrToken != 0;
}

inline LPCSTR BaseAssemblySpec::GetName()  const
{
    LIMITED_METHOD_CONTRACT;
    return m_pAssemblyName;
}

#endif // __BASE_ASSEMBLY_SPEC_INL__
