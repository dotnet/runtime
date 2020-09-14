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

#include "assemblyname.hpp"
#include "assembly.hpp"
#include "utils.hpp"
#include "variables.hpp"

#include "fusionassemblyname.hpp"

#include "textualidentityparser.hpp"

#include "corpriv.h"

#include "ex.h"

namespace BINDER_SPACE
{
    AssemblyName::AssemblyName()
    {
        m_cRef = 1;
        m_dwNameFlags = NAME_FLAG_NONE;
        // Default values present in every assembly name
        SetHave(AssemblyIdentity::IDENTITY_FLAG_CULTURE |
                AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL);
    }

    AssemblyName::~AssemblyName()
    {
        //  Nothing to do here
    }

    HRESULT AssemblyName::Init(IMDInternalImport       *pIMetaDataAssemblyImport,
                               PEKIND                   PeKind,
                               mdAssemblyRef            mdar /* = 0 */,
                               BOOL                     fIsDefinition /* = TRUE */)
    {
        HRESULT hr = S_OK;
        mdAssembly mda = 0;
        AssemblyMetaDataInternal amd = {0};
        CONST VOID *pvPublicKeyToken = NULL;
        DWORD dwPublicKeyToken = 0;
        LPCSTR pAssemblyName = NULL;
        DWORD dwRefOrDefFlags = 0;
        DWORD dwHashAlgId = 0;

        if (fIsDefinition)
        {
            // Get the assembly token
            IF_FAIL_GO(pIMetaDataAssemblyImport->GetAssemblyFromScope(&mda));
        }

        // Get name and metadata
        if (fIsDefinition)
        {
            IF_FAIL_GO(pIMetaDataAssemblyImport->GetAssemblyProps(
                            mda,            // [IN] The Assembly for which to get the properties.
                            &pvPublicKeyToken,  // [OUT] Pointer to the PublicKeyToken blob.
                            &dwPublicKeyToken,  // [OUT] Count of bytes in the PublicKeyToken Blob.
                            &dwHashAlgId,   // [OUT] Hash Algorithm.
                            &pAssemblyName, // [OUT] Name.
                            &amd,           // [OUT] Assembly MetaData.
                            &dwRefOrDefFlags // [OUT] Flags.
                            ));
        }
        else
        {
            IF_FAIL_GO(pIMetaDataAssemblyImport->GetAssemblyRefProps(
                            mdar,            // [IN] The Assembly for which to get the properties.
                            &pvPublicKeyToken,  // [OUT] Pointer to the PublicKeyToken blob.
                            &dwPublicKeyToken,  // [OUT] Count of bytes in the PublicKeyToken Blob.
                            &pAssemblyName, // [OUT] Name.
                            &amd,           // [OUT] Assembly MetaData.
                            NULL, // [OUT] Hash blob.
                            NULL, // [OUT] Count of bytes in hash blob.
                            &dwRefOrDefFlags // [OUT] Flags.
                            ));
        }

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
        if (pvPublicKeyToken && dwPublicKeyToken)
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

        SetArchitecture(PeKind);

    Exit:
        return hr;
    }

    HRESULT AssemblyName::Init(SString &assemblyDisplayName)
    {
        return TextualIdentityParser::Parse(assemblyDisplayName, this);
    }

    HRESULT AssemblyName::Init(IAssemblyName *pIAssemblyName)
    {
        HRESULT hr = S_OK;

        _ASSERTE(pIAssemblyName != NULL);

        EX_TRY
        {
            {
                // Set the simpleName
                StackSString simpleName;
                hr = fusion::util::GetSimpleName(pIAssemblyName, simpleName);
                IF_FAIL_GO(hr);
                SetSimpleName(simpleName);
                SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_SIMPLE_NAME);
            }

            // Display version
            DWORD dwVersionParts[4] = {0,0,0,0};
            DWORD cbVersionSize = sizeof(dwVersionParts[0]);
            hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_MAJOR_VERSION, static_cast<PVOID>(&dwVersionParts[0]), &cbVersionSize);
            IF_FAIL_GO(hr);
            if ((hr == S_OK) && (cbVersionSize != 0))
            {
                // Property is present - loop to get the individual version details
                for(DWORD i = 0; i < 4; i++)
                {
                    cbVersionSize = sizeof(dwVersionParts[i]);
                    hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_MAJOR_VERSION+i, static_cast<PVOID>(&dwVersionParts[i]), &cbVersionSize);
                    IF_FAIL_GO(hr);
                }

                m_version.SetFeatureVersion(dwVersionParts[0], dwVersionParts[1]);
                m_version.SetServiceVersion(dwVersionParts[2], dwVersionParts[3]);
                SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_VERSION);
            }

            {
                // Display culture
                StackSString culture;
                hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_CULTURE, culture);
                IF_FAIL_GO(hr);
                if (hr == S_OK)
                {
                    SetCulture(culture);
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE);
                }
            }

            {
                // Display public key token
                NewArrayHolder<BYTE> pPublicKeyToken;
                DWORD cbPublicKeyToken = 0;
                hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_PUBLIC_KEY_TOKEN, static_cast<PBYTE*>(&pPublicKeyToken), &cbPublicKeyToken);
                IF_FAIL_GO(hr);
                if ((hr == S_OK) && (cbPublicKeyToken != 0))
                {
                    m_publicKeyOrTokenBLOB.Set(pPublicKeyToken, cbPublicKeyToken);
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
                }
                else
                {
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL);
                }
            }

            // Display processor architecture
            DWORD peKind = 0;
            DWORD cbPeKind = sizeof(peKind);
            hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_ARCHITECTURE, static_cast<PVOID>(&peKind), &cbPeKind);
            IF_FAIL_GO(hr);
            if ((hr == S_OK) && (cbPeKind != 0))
            {
                PEKIND PeKind = (PEKIND)peKind;
                if (PeKind != peNone)
                {
                    SetArchitecture(PeKind);
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE);
                }
            }

            // Display retarget flag
            BOOL fRetarget = FALSE;
            DWORD cbRetarget = sizeof(fRetarget);
            hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_RETARGET, static_cast<PVOID>(&fRetarget), &cbRetarget);
            IF_FAIL_GO(hr);
            if ((hr == S_OK) && (cbRetarget != 0))
            {
                if (fRetarget)
                {
                    SetIsRetargetable(fRetarget);
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE);
                }
            }

            // Display content type
            DWORD dwContentType = AssemblyContentType_Default;
            DWORD cbContentType = sizeof(dwContentType);
            hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_CONTENT_TYPE, static_cast<PVOID>(&dwContentType), &cbContentType);
            IF_FAIL_GO(hr);
            if ((hr == S_OK) && (cbContentType != 0))
            {
                if (dwContentType != AssemblyContentType_Default)
                {
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE);
                    SetContentType((AssemblyContentType)dwContentType);
                }
            }

            {
                // Display custom flag. Dont set it if it is not present since that will end up adding the "Custom=null" attribute
                // in the displayname of the assembly that maybe generated using this AssemblyName instance. This could create conflict when
                // the displayname is generated from the assembly directly as that will not have a "Custom" field set.
                NewArrayHolder<BYTE> pCustomBLOB;
                DWORD cbCustomBLOB = 0;
                hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_CUSTOM, static_cast<PBYTE*>(&pCustomBLOB), &cbCustomBLOB);
                IF_FAIL_GO(hr);
                if ((hr == S_OK) && (cbCustomBLOB != 0))
                {
                    m_customBLOB.Set(pCustomBLOB, cbCustomBLOB);
                    SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CUSTOM);
                }
            }
        }
        EX_CATCH_HRESULT(hr);
Exit:
        return hr;
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
        return EqualsCaseInsensitive(GetSimpleName(), g_BinderVariables->corelib);
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

        dwHash ^= static_cast<DWORD>(HashCaseInsensitive(GetSimpleName()));
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
            dwHash ^= static_cast<DWORD>(HashCaseInsensitive(GetNormalizedCulture()));
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

        if (EqualsCaseInsensitive(GetSimpleName(), pAssemblyName->GetSimpleName()) &&
            (GetContentType() == pAssemblyName->GetContentType()))
        {
            fEquals = TRUE;

            if ((dwIncludeFlags & EXCLUDE_CULTURE) == 0)
            {
                fEquals = EqualsCaseInsensitive(GetNormalizedCulture(), pAssemblyName->GetNormalizedCulture());
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
            culture = g_BinderVariables->cultureNeutral;
        }

        return culture;
    }
};  // namespace BINDER_SPACE
