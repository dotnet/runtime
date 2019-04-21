// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// AssemblyName.cpp
//


//
// Implements the AssemblyName class
//
// ============================================================

#define DISABLE_BINDER_DEBUG_LOGGING

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

        BINDER_LOG_ENTER(L"AssemblyName::Init(IMetaDataAssemblyImport)");

        if (fIsDefinition)
        {
            // Get the assembly token
            IF_FAIL_GO(pIMetaDataAssemblyImport->GetAssemblyFromScope(&mda));
        }

        BINDER_LOG(L"Have mda scope!");

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

        BINDER_LOG(L"Have props!");

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
        else if (IsAfContentType_WindowsRuntime(dwRefOrDefFlags))
        {
            SetContentType(AssemblyContentType_WindowsRuntime);
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
        BINDER_LOG_LEAVE_HR(L"AssemblyName::Init(IMetaDataAssemblyImport)", hr);
        return hr;
    }

    HRESULT AssemblyName::Init(SString &assemblyDisplayName)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"AssemblyName::Init(assemblyDisplayName)");

        BINDER_LOG_STRING(L"assemblyDisplayName", assemblyDisplayName);

        IF_FAIL_GO(TextualIdentityParser::Parse(assemblyDisplayName, this));
        
    Exit:
        BINDER_LOG_LEAVE_HR(L"AssemblyName::Init(assemblyDisplayName)", hr);
        return hr;
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
    
    HRESULT AssemblyName::CreateFusionName(IAssemblyName **ppIAssemblyName)
    {
        HRESULT hr = S_OK;
        ReleaseHolder<IAssemblyName> pIAssemblyName;

        IF_FAIL_GO(CreateAssemblyNameObject(&pIAssemblyName, NULL, 0, NULL));

        IF_FAIL_GO(LegacyFusion::SetStringProperty(pIAssemblyName, ASM_NAME_NAME, GetSimpleName()));

        if (Have(AssemblyIdentity::IDENTITY_FLAG_VERSION))
        {
            AssemblyVersion *pAssemblyVersion = GetVersion();

            IF_FAIL_GO(LegacyFusion::SetWordProperty(pIAssemblyName,
                                       ASM_NAME_MAJOR_VERSION,
                                       pAssemblyVersion->GetMajor()));
            IF_FAIL_GO(LegacyFusion::SetWordProperty(pIAssemblyName,
                                       ASM_NAME_MINOR_VERSION,
                                       pAssemblyVersion->GetMinor()));
            IF_FAIL_GO(LegacyFusion::SetWordProperty(pIAssemblyName,
                                       ASM_NAME_BUILD_NUMBER,
                                       pAssemblyVersion->GetBuild()));
            IF_FAIL_GO(LegacyFusion::SetWordProperty(pIAssemblyName,
                                       ASM_NAME_REVISION_NUMBER,
                                       pAssemblyVersion->GetRevision()));
        }

        if (Have(AssemblyIdentity::IDENTITY_FLAG_CULTURE))
        {
            IF_FAIL_GO(LegacyFusion::SetStringProperty(pIAssemblyName, ASM_NAME_CULTURE, GetCulture()));
        }

        if (Have(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY))
        {
            // GetPublicKeyTokenBLOB contains either PK or PKT.
            IF_FAIL_GO(LegacyFusion::SetBufferProperty(pIAssemblyName,
                                         ASM_NAME_PUBLIC_KEY,
                                         GetPublicKeyTokenBLOB()));
        }
        else if (Have(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN))
        {
            // GetPublicKeyTokenBLOB contains either PK or PKT.
            IF_FAIL_GO(LegacyFusion::SetBufferProperty(pIAssemblyName,
                                         ASM_NAME_PUBLIC_KEY_TOKEN,
                                         GetPublicKeyTokenBLOB()));
        }

        if (Have(AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE))
        {
            IF_FAIL_GO(LegacyFusion::SetDwordProperty(pIAssemblyName,
                                        ASM_NAME_ARCHITECTURE,
                                        static_cast<DWORD>(GetArchitecture())));
        }

        if (Have(AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE))
        {
            IF_FAIL_GO(LegacyFusion::SetDwordProperty(pIAssemblyName,
                                        ASM_NAME_CONTENT_TYPE,
                                        GetContentType()));
        }
        
        *ppIAssemblyName = pIAssemblyName.Extract();

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

    SString &AssemblyName::GetDeNormalizedCulture()
    {
        SString &culture = GetCulture();

        if (EqualsCaseInsensitive(culture, g_BinderVariables->cultureNeutral))
        {
            culture = g_BinderVariables->emptyString;
        }

        return culture;
    }

    BOOL AssemblyName::IsStronglyNamed()
    {
        return Have(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
    }
    
    BOOL AssemblyName::IsMscorlib()
    {
        // TODO: Is this simple comparison enough?
        return EqualsCaseInsensitive(GetSimpleName(), g_BinderVariables->mscorlib);
    }

    HRESULT AssemblyName::SetArchitecture(SString &architecture)
    {
        HRESULT hr = S_OK;

        if (architecture.IsEmpty())
        {
            SetArchitecture(peNone);
        }
        else if (EqualsCaseInsensitive(architecture, g_BinderVariables->architectureMSIL))
        {
            SetArchitecture(peMSIL);
        }
        else if (EqualsCaseInsensitive(architecture, g_BinderVariables->architectureX86))
        {
            SetArchitecture(peI386);
        }
        else if (EqualsCaseInsensitive(architecture, g_BinderVariables->architectureAMD64))
        {
            SetArchitecture(peAMD64);
        }
        else if (EqualsCaseInsensitive(architecture, g_BinderVariables->architectureARM))
        {
            SetArchitecture(peARM);
        }
        else if (EqualsCaseInsensitive(architecture, g_BinderVariables->architectureARM64))
        {
            SetArchitecture(peARM64);
        }
        else
        {
            hr = FUSION_E_MANIFEST_PARSE_ERROR;
        }

        return hr;
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

    BOOL AssemblyName::RefEqualsDef(AssemblyName *pAssemblyNameDef,
                                    BOOL          fInspectionOnly)
    {
        BOOL fEquals = FALSE;
        
        if (GetContentType() == AssemblyContentType_WindowsRuntime)
        {   // Assembly is meaningless for WinRT, all assemblies form one joint type namespace
            return (GetContentType() == pAssemblyNameDef->GetContentType());
        }
        
        if (EqualsCaseInsensitive(GetSimpleName(), pAssemblyNameDef->GetSimpleName()) &&
            EqualsCaseInsensitive(GetNormalizedCulture(),
                                  pAssemblyNameDef->GetNormalizedCulture()) &&
            GetPublicKeyTokenBLOB().Equals(pAssemblyNameDef->GetPublicKeyTokenBLOB()) && 
            (GetContentType() == pAssemblyNameDef->GetContentType()))
        {
            PEKIND kRefArchitecture = GetArchitecture();
            PEKIND kDefArchitecture = pAssemblyNameDef->GetArchitecture();

            if (kRefArchitecture == peNone)
            {
                fEquals = (fInspectionOnly || 
                           (kDefArchitecture == peNone) ||
                           (kDefArchitecture == peMSIL) ||
                           (kDefArchitecture == Assembly::GetSystemArchitecture()));
            }
            else
            {
                fEquals = (kRefArchitecture == kDefArchitecture);
            }
        }

        return fEquals;
    }

    HRESULT AssemblyName::Clone(AssemblyName **ppAssemblyName)
    {
        HRESULT hr = S_OK;
        AssemblyName *pClonedAssemblyName = NULL;

        SAFE_NEW(pClonedAssemblyName, AssemblyName);
        CloneInto(pClonedAssemblyName);
        pClonedAssemblyName->m_dwNameFlags = m_dwNameFlags;

        *ppAssemblyName = pClonedAssemblyName;

    Exit:
        return hr;
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

    SString &AssemblyName::ArchitectureToString(PEKIND kArchitecture)
    {
            switch (kArchitecture)
            {
            case peNone:
                return g_BinderVariables->emptyString;
            case peMSIL:
                return g_BinderVariables->architectureMSIL;
            case peI386:
                return g_BinderVariables->architectureX86;
            case peAMD64:
                return g_BinderVariables->architectureAMD64;
            case peARM:
                return g_BinderVariables->architectureARM;
            case peARM64:
                return g_BinderVariables->architectureARM64;
            default:
                _ASSERTE(0);
                return g_BinderVariables->emptyString;
            }
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
