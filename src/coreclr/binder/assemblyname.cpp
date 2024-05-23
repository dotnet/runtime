// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyName.cpp
//


//
// Implements the AssemblyName class
//
// ============================================================

#include "common.h"
#include "assemblyname.hpp"
#include "assemblybindercommon.hpp"

#include "utils.hpp"

#include "textualidentityparser.hpp"

#include "corpriv.h"

#include "ex.h"

namespace
{
    // See https://docs.microsoft.com/dotnet/framework/reflection-and-codedom/specifying-fully-qualified-type-names#specifying-assembly-names
    const WCHAR* s_neutralCulture = W("neutral");
}

STDAPI BinderAcquireImport(PEImage* pPEImage,
    IMDInternalImport** pIMetaDataAssemblyImport,
    DWORD* pdwPAFlags);

namespace BINDER_SPACE
{
    AssemblyName::AssemblyName()
    {
        m_cRef = 1;
        m_isDefinition = false;
        // Default values present in every assembly name
        SetHave(AssemblyIdentity::IDENTITY_FLAG_CULTURE |
                AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL);
    }

    HRESULT AssemblyName::Init(PEImage* pPEImage)
    {
        HRESULT hr = S_OK;
        mdAssembly mda = 0;
        AssemblyMetaDataInternal amd = {0};
        CONST VOID *pvPublicKeyToken = NULL;
        DWORD dwPublicKeyToken = 0;
        LPCSTR pAssemblyName = NULL;
        DWORD dwRefOrDefFlags = 0;
        DWORD dwHashAlgId = 0;

        ReleaseHolder<IMDInternalImport> pIMetaDataAssemblyImport;

        PEKIND PeKind = peNone;
        DWORD dwPAFlags[2];
        IF_FAIL_GO(BinderAcquireImport(pPEImage, &pIMetaDataAssemblyImport, dwPAFlags));
        IF_FAIL_GO(AssemblyBinderCommon::TranslatePEToArchitectureType(dwPAFlags, &PeKind));
        SetArchitecture(PeKind);

        _ASSERTE(pIMetaDataAssemblyImport != NULL);

        // Get the assembly token
        IF_FAIL_GO(pIMetaDataAssemblyImport->GetAssemblyFromScope(&mda));

        // Get name and metadata
        IF_FAIL_GO(pIMetaDataAssemblyImport->GetAssemblyProps(
                        mda,            // [IN] The Assembly for which to get the properties.
                        &pvPublicKeyToken,  // [OUT] Pointer to the PublicKeyToken blob.
                        &dwPublicKeyToken,  // [OUT] Count of bytes in the PublicKeyToken Blob.
                        &dwHashAlgId,   // [OUT] Hash Algorithm.
                        &pAssemblyName, // [OUT] Name.
                        &amd,           // [OUT] Assembly MetaData.
                        &dwRefOrDefFlags // [OUT] Flags.
                        ));

        {
            StackSString culture;
            culture.SetUTF8(amd.szLocale);
            culture.Normalize();

            SString::CIterator itr = culture.Begin();
            if (culture.Find(itr, L';'))
            {
                culture = SString(culture, culture.Begin(), itr-1);
            }

            SetCulture(culture);
        }

        {
            StackSString assemblyName;
            assemblyName.SetUTF8(pAssemblyName);
            assemblyName.Normalize();

            COUNT_T assemblyNameLength = assemblyName.GetCount();
            if (assemblyNameLength == 0 || assemblyNameLength >= MAX_PATH_FNAME)
            {
                IF_FAIL_GO(FUSION_E_INVALID_NAME);
            }

            SetSimpleName(assemblyName);
        }

        // See if the assembly[def] is retargetable (ie, for a generic assembly).
        if (IsAfRetargetable(dwRefOrDefFlags))
        {
            SetIsRetargetable(TRUE);
        }

        // Set ContentType
        if (IsAfContentType_Default(dwRefOrDefFlags))
        {
            SetContentType(AssemblyContentType_Default);
        }
        else
        {
            IF_FAIL_GO(FUSION_E_INVALID_NAME);
        }

        // Set the assembly version
        {
            AssemblyVersion *pAssemblyVersion = GetVersion();

            pAssemblyVersion->SetFeatureVersion(amd.usMajorVersion, amd.usMinorVersion);
            pAssemblyVersion->SetServiceVersion(amd.usBuildNumber, amd.usRevisionNumber);
            SetHave(AssemblyIdentity::IDENTITY_FLAG_VERSION);
        }

        // Set public key and/or public key token (if we have it)
        if (dwPublicKeyToken && pvPublicKeyToken)
        {
            SBuffer publicKeyOrTokenBLOB((const BYTE *) pvPublicKeyToken, dwPublicKeyToken);

            if (IsAfPublicKey(dwRefOrDefFlags))
            {
                SBuffer publicKeyTokenBLOB;

                IF_FAIL_GO(GetTokenFromPublicKey(publicKeyOrTokenBLOB, publicKeyTokenBLOB));
                GetPublicKeyTokenBLOB().Set(publicKeyTokenBLOB);
            }
            else
            {
                GetPublicKeyTokenBLOB().Set(publicKeyOrTokenBLOB);
            }

            SetHave(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
        }

    Exit:
        return hr;
    }

    HRESULT AssemblyName::Init(const AssemblyNameData &data)
    {
        DWORD flags = data.IdentityFlags;
        m_simpleName.SetUTF8(data.Name);
        m_version.SetFeatureVersion(data.MajorVersion, data.MinorVersion);
        m_version.SetServiceVersion(data.BuildNumber, data.RevisionNumber);
        m_cultureOrLanguage.SetUTF8(data.Culture);

        m_publicKeyOrTokenBLOB.Set(data.PublicKeyOrToken, data.PublicKeyOrTokenLength);
        if ((flags & BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY) != 0)
        {
            // Convert public key to token
            SBuffer publicKeyToken;
            HRESULT hr = GetTokenFromPublicKey(m_publicKeyOrTokenBLOB, publicKeyToken);
            if (FAILED(hr))
                return hr;

            m_publicKeyOrTokenBLOB.Set(publicKeyToken);
            flags &= ~BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY;
            flags |= BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN;
        }

        m_kProcessorArchitecture = data.ProcessorArchitecture;
        m_kContentType = data.ContentType;

        SetHave(flags);

        return S_OK;
    }

    ULONG AssemblyName::AddRef()
    {
        return InterlockedIncrement(&m_cRef);
    }

    ULONG AssemblyName::Release()
    {
        ULONG ulRef = InterlockedDecrement(&m_cRef);
        if (ulRef == 0)
        {
            delete this;
        }
        return ulRef;
    }

    BOOL AssemblyName::IsCoreLib()
    {
        // TODO: Is this simple comparison enough?
        return SString::_wcsicmp(GetSimpleName().GetUnicode(), CoreLibName_W) == 0;
    }

    bool AssemblyName::IsNeutralCulture()
    {
        return m_cultureOrLanguage.IsEmpty()
            || SString::_wcsicmp(m_cultureOrLanguage.GetUnicode(), s_neutralCulture) == 0;
    }

    ULONG AssemblyName::Hash(DWORD dwIncludeFlags)
    {
        DWORD dwHash = 0;
        DWORD dwUseIdentityFlags = m_dwIdentityFlags;

        // Prune unwanted name parts
        if ((dwIncludeFlags & INCLUDE_VERSION) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_VERSION;
        }
        if ((dwIncludeFlags & INCLUDE_ARCHITECTURE) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE;
        }
        if ((dwIncludeFlags & INCLUDE_RETARGETABLE) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE;
        }
        if ((dwIncludeFlags & INCLUDE_CONTENT_TYPE) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE;
        }
        if ((dwIncludeFlags & INCLUDE_PUBLIC_KEY_TOKEN) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY;
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN;
        }
        if ((dwIncludeFlags & EXCLUDE_CULTURE) != 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_CULTURE;
        }

        dwHash ^= static_cast<DWORD>(GetSimpleName().HashCaseInsensitive());
        dwHash = _rotl(dwHash, 4);

        if (AssemblyIdentity::Have(dwUseIdentityFlags,
                                   AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY) ||
            AssemblyIdentity::Have(dwUseIdentityFlags,
                                   AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN))
        {
            const BYTE *pbPublicKeyOrToken = GetPublicKeyTokenBLOB();
            DWORD dwcbPublicKeyOrToken = GetPublicKeyTokenBLOB().GetSize();

            _ASSERTE(pbPublicKeyOrToken != NULL);

            dwHash ^= HashBytes(pbPublicKeyOrToken, dwcbPublicKeyOrToken);
            dwHash = _rotl(dwHash, 4);
        }

        if (AssemblyIdentity::Have(dwUseIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_VERSION))
        {
            AssemblyVersion *pAssemblyVersion = GetVersion();

            dwHash ^= pAssemblyVersion->GetMajor();
            dwHash = _rotl(dwHash, 8);
            dwHash ^= pAssemblyVersion->GetMinor();
            dwHash = _rotl(dwHash, 8);
            dwHash ^= pAssemblyVersion->GetBuild();
            dwHash = _rotl(dwHash, 8);
            dwHash ^= pAssemblyVersion->GetRevision();
            dwHash = _rotl(dwHash, 8);
        }

        if (AssemblyIdentity::Have(dwUseIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_CULTURE))
        {
            dwHash ^= static_cast<DWORD>(GetNormalizedCulture().HashCaseInsensitive());
            dwHash = _rotl(dwHash, 4);
        }

        if (AssemblyIdentity::Have(dwUseIdentityFlags,
                                   AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE))
        {
            dwHash ^= 1;
            dwHash = _rotl(dwHash, 4);
        }

        if (AssemblyIdentity::Have(dwUseIdentityFlags,
                                   AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE))
        {
            dwHash ^= static_cast<DWORD>(GetArchitecture());
            dwHash = _rotl(dwHash, 4);
        }

        if (AssemblyIdentity::Have(dwUseIdentityFlags,
                                   AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE))
        {
            dwHash ^= static_cast<DWORD>(GetContentType());
            dwHash = _rotl(dwHash, 4);
        }

        return static_cast<ULONG>(dwHash);
    }

    BOOL AssemblyName::Equals(AssemblyName *pAssemblyName,
                              DWORD         dwIncludeFlags)
    {
        BOOL fEquals = FALSE;

        if (GetContentType() == AssemblyContentType_WindowsRuntime)
        {   // Assembly is meaningless for WinRT, all assemblies form one joint type namespace
            return (GetContentType() == pAssemblyName->GetContentType());
        }

        if (GetSimpleName().EqualsCaseInsensitive(pAssemblyName->GetSimpleName()) &&
            (GetContentType() == pAssemblyName->GetContentType()))
        {
            fEquals = TRUE;

            if ((dwIncludeFlags & EXCLUDE_CULTURE) == 0)
            {
                fEquals = GetNormalizedCulture().EqualsCaseInsensitive(pAssemblyName->GetNormalizedCulture());
            }

            if (fEquals && (dwIncludeFlags & INCLUDE_PUBLIC_KEY_TOKEN) != 0)
            {
                fEquals = (GetPublicKeyTokenBLOB().Equals(pAssemblyName->GetPublicKeyTokenBLOB()));
            }

            if (fEquals && ((dwIncludeFlags & INCLUDE_ARCHITECTURE) != 0))
            {
                fEquals = (GetArchitecture() == pAssemblyName->GetArchitecture());
            }

            if (fEquals && ((dwIncludeFlags & INCLUDE_VERSION) != 0))
            {
                fEquals = GetVersion()->Equals(pAssemblyName->GetVersion());
            }

            if (fEquals && ((dwIncludeFlags & INCLUDE_RETARGETABLE) != 0))
            {
                fEquals = (GetIsRetargetable() == pAssemblyName->GetIsRetargetable());
            }
        }

        return fEquals;
    }

    void AssemblyName::GetDisplayName(PathString &displayName,
                                      DWORD       dwIncludeFlags)
    {
        DWORD dwUseIdentityFlags = m_dwIdentityFlags;

        // Prune unwanted name parts
        if ((dwIncludeFlags & INCLUDE_VERSION) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_VERSION;
        }
        if ((dwIncludeFlags & INCLUDE_ARCHITECTURE) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE;
        }
        if ((dwIncludeFlags & INCLUDE_RETARGETABLE) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE;
        }
        if ((dwIncludeFlags & INCLUDE_CONTENT_TYPE) == 0)
        {
            dwUseIdentityFlags &= ~AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE;
        }

        TextualIdentityParser::ToString(this, dwUseIdentityFlags, displayName);
    }

    SString &AssemblyName::GetNormalizedCulture()
    {
        SString &culture = GetCulture();

        if (culture.IsEmpty())
        {
            culture.SetLiteral(s_neutralCulture);
        }

        return culture;
    }
};  // namespace BINDER_SPACE
