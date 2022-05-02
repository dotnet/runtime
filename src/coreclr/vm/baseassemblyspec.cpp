// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// BaseAssemblySpec.cpp
//
// Implements the BaseAssemblySpec class
//


// ============================================================

#include "common.h"
#include "thekey.h"

#include "strongnameinternal.h"
#include "strongnameholders.h"

BOOL BaseAssemblySpec::IsCoreLib()
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

#define CORELIB_PUBLICKEY g_rbTheSilverlightPlatformKey


// A satellite assembly for CoreLib is named "System.Private.CoreLib.resources" or
// System.Private.CoreLib.debug.resources.dll and uses the same public key as CoreLib.
// It does not necessarily have the same version, and the Culture will
// always be set to something like "jp-JP".
BOOL BaseAssemblySpec::IsCoreLibSatellite() const
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
        return FALSE;
    }

    _ASSERTE(strlen(g_psBaseLibrarySatelliteAssemblyName) == CoreLibSatelliteNameLen);

    // <TODO>More of bug 213471</TODO>
    size_t iNameLen = strlen(m_pAssemblyName);

    // we allow name to be of the form System.Private.CoreLib.resources.dll only
    BOOL r = ( (m_cbPublicKeyOrToken == sizeof(CORELIB_PUBLICKEY)) &&
             (iNameLen >= CoreLibSatelliteNameLen) &&
             (!SString::_strnicmp(m_pAssemblyName, g_psBaseLibrarySatelliteAssemblyName, CoreLibSatelliteNameLen)) &&
             ( (iNameLen == CoreLibSatelliteNameLen) || (m_pAssemblyName[CoreLibSatelliteNameLen] == ',') ) );

    r = r && ( memcmp(m_pbPublicKeyOrToken,CORELIB_PUBLICKEY,sizeof(CORELIB_PUBLICKEY)) == 0);

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
