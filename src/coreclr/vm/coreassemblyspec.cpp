// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// CoreAssemblySpec.cpp
//


//
// CoreCLR specific implementation of AssemblySpec and BaseAssemblySpec
// ============================================================

#include "common.h"
#include "peimage.h"
#include "appdomain.inl"
#include <peimage.h>
#include "peimagelayout.inl"
#include "domainassembly.h"
#include "holder.h"
#include "bundle.h"
#include "strongnameinternal.h"
#include "strongnameholders.h"

#include "../binder/inc/textualidentityparser.hpp"
#include "../binder/inc/assemblyidentity.hpp"
#include "../binder/inc/assembly.hpp"
#include "../binder/inc/assemblyname.hpp"

#include "../binder/inc/assemblybindercommon.hpp"
#include "../binder/inc/applicationcontext.hpp"

static VOID ThrowLoadError(AssemblySpec * pSpec, HRESULT hr)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    StackSString name;
    pSpec->GetDisplayName(0, name);
    EEFileLoadException::Throw(name, hr);
}

HRESULT  AssemblySpec::Bind(AppDomain *pAppDomain, BINDER_SPACE::Assembly** ppAssembly)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(ppAssembly));
        PRECONDITION(CheckPointer(pAppDomain));
        PRECONDITION(IsCoreLib() == FALSE); // This should never be called for CoreLib (explicit loading)
    }
    CONTRACTL_END;

    HRESULT hr=S_OK;

    // Have a default binding context setup
    AssemblyBinder *pBinder = GetBinderFromParentAssembly(pAppDomain);

    ReleaseHolder<BINDER_SPACE::Assembly> pPrivAsm;
    _ASSERTE(pBinder != NULL);

    if (IsCoreLibSatellite())
    {
        StackSString sSystemDirectory(SystemDomain::System()->SystemDirectory());
        StackSString tmpString;
        StackSString sSimpleName;
        StackSString sCultureName;

        tmpString.SetUTF8(m_pAssemblyName);
        tmpString.ConvertToUnicode(sSimpleName);

        tmpString.Clear();
        if ((m_context.szLocale != NULL) && (m_context.szLocale[0] != 0))
        {
            tmpString.SetUTF8(m_context.szLocale);
            tmpString.ConvertToUnicode(sCultureName);
        }

        hr = BINDER_SPACE::AssemblyBinderCommon::BindToSystemSatellite(sSystemDirectory, sSimpleName, sCultureName, &pPrivAsm);
    }
    else
    {
        AssemblyNameData assemblyNameData = { 0 };
        PopulateAssemblyNameData(assemblyNameData);
        hr = pBinder->BindAssemblyByName(&assemblyNameData, &pPrivAsm);
    }

    if (SUCCEEDED(hr))
    {
        _ASSERTE(pPrivAsm != nullptr);
        *ppAssembly = pPrivAsm.Extract();
    }

    return hr;
}


STDAPI BinderAcquirePEImage(LPCWSTR             wszAssemblyPath,
                            PEImage           **ppPEImage,
                            BundleFileLocation  bundleFileLocation)
{
    HRESULT hr = S_OK;

    _ASSERTE(ppPEImage != NULL);

    EX_TRY
    {
        PEImageHolder pImage = PEImage::OpenImage(wszAssemblyPath, MDInternalImport_Default, bundleFileLocation);

        // Make sure that the IL image can be opened.
        hr=pImage->TryOpenFile();
        if (FAILED(hr))
        {
            goto Exit;
        }

        if (pImage)
            *ppPEImage = pImage.Extract();
    }
    EX_CATCH_HRESULT(hr);

 Exit:
    return hr;
}

STDAPI BinderAcquireImport(PEImage                  *pPEImage,
                           IMDInternalImport       **ppIAssemblyMetaDataImport,
                           DWORD                    *pdwPAFlags)
{
    HRESULT hr = S_OK;

    _ASSERTE(pPEImage != NULL);
    _ASSERTE(ppIAssemblyMetaDataImport != NULL);
    _ASSERTE(pdwPAFlags != NULL);

    EX_TRY
    {
        PEImageLayout* pLayout = pPEImage->GetOrCreateLayout(PEImageLayout::LAYOUT_ANY);

        // CheckCorHeader includes check of NT headers too
        if (!pLayout->CheckCorHeader())
            IfFailGo(COR_E_ASSEMBLYEXPECTED);

        if (!pLayout->CheckFormat())
            IfFailGo(COR_E_BADIMAGEFORMAT);

        pPEImage->GetPEKindAndMachine(&pdwPAFlags[0], &pdwPAFlags[1]);

        *ppIAssemblyMetaDataImport = pPEImage->GetMDImport();
        if (!*ppIAssemblyMetaDataImport)
        {
            IfFailGo(COR_E_BADIMAGEFORMAT);
        }
        else
            (*ppIAssemblyMetaDataImport)->AddRef();
    }
    EX_CATCH_HRESULT(hr);
ErrExit:
    return hr;
}

HRESULT BaseAssemblySpec::ParseName()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_TRIGGERS;
        NOTHROW;
        INJECT_FAULT(return E_OUTOFMEMORY;);
    }
    CONTRACTL_END;

    if (!m_pAssemblyName)
        return S_OK;

    HRESULT hr = S_OK;

    EX_TRY
    {
        BINDER_SPACE::AssemblyIdentityUTF8* pAssemblyIdentity;
        AppDomain *pDomain = ::GetAppDomain();
        _ASSERTE(pDomain);

        BINDER_SPACE::ApplicationContext *pAppContext = NULL;
        DefaultAssemblyBinder *pBinder = pDomain->GetDefaultBinder();

        hr = pBinder->GetAppContext()->GetAssemblyIdentity(m_pAssemblyName, &pAssemblyIdentity);

        if (FAILED(hr))
        {
            m_ownedFlags |= BAD_NAME_OWNED;
            IfFailThrow(hr);
        }

        // Name - does not copy the data
        SetName(pAssemblyIdentity->GetSimpleNameUTF8());

        // Culture - does not copy the data
        if (pAssemblyIdentity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE))
        {
            if (!pAssemblyIdentity->m_cultureOrLanguage.IsEmpty())
            {
                SetCulture(pAssemblyIdentity->GetCultureOrLanguageUTF8());
            }
            else
            {
                SetCulture("");
            }
        }

        InitializeWithAssemblyIdentity(pAssemblyIdentity);

        // Copy and own any fields we do not already own
        CloneFields();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

void BaseAssemblySpec::InitializeWithAssemblyIdentity(BINDER_SPACE::AssemblyIdentity *identity)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    // Version
    if (identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_VERSION))
    {
        m_context.usMajorVersion = (USHORT)identity->m_version.GetMajor();
        m_context.usMinorVersion = (USHORT)identity->m_version.GetMinor();
        m_context.usBuildNumber = (USHORT)identity->m_version.GetBuild();
        m_context.usRevisionNumber = (USHORT)identity->m_version.GetRevision();
    }

    // Public key or token - does not copy the data
    if (identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN)
        || identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY))
    {
        m_pbPublicKeyOrToken = const_cast<BYTE *>(static_cast<const BYTE*>(identity->m_publicKeyOrTokenBLOB));
        m_cbPublicKeyOrToken = identity->m_publicKeyOrTokenBLOB.GetSize();

        if (identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY))
        {
            m_dwFlags |= afPublicKey;
        }
    }
    else if (identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL))
    {
        m_pbPublicKeyOrToken = const_cast<BYTE *>(static_cast<const BYTE*>(identity->m_publicKeyOrTokenBLOB));
        m_cbPublicKeyOrToken = 0;
    }
    else
    {
        m_pbPublicKeyOrToken = NULL;
        m_cbPublicKeyOrToken = 0;
    }

    // Architecture
    if (identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE))
    {
        switch (identity->m_kProcessorArchitecture)
        {
        case peI386:
            m_dwFlags |= afPA_x86;
            break;
        case peIA64:
            m_dwFlags |= afPA_IA64;
            break;
        case peAMD64:
            m_dwFlags |= afPA_AMD64;
            break;
        case peARM:
            m_dwFlags |= afPA_ARM;
            break;
        case peMSIL:
            m_dwFlags |= afPA_MSIL;
            break;
        default:
            IfFailThrow(FUSION_E_INVALID_NAME);
        }
    }

    // Retargetable
    if (identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE))
    {
        m_dwFlags |= afRetargetable;
    }

    // Content type
    if (identity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE))
    {
        DWORD dwContentType = identity->m_kContentType;

        _ASSERTE((dwContentType == AssemblyContentType_Default) || (dwContentType == AssemblyContentType_WindowsRuntime));
        if (dwContentType == AssemblyContentType_WindowsRuntime)
        {
            m_dwFlags |= afContentType_WindowsRuntime;
        }
    }
}

namespace
{
    PEKIND GetProcessorArchitectureFromAssemblyFlags(DWORD flags)
    {
        if (flags & afPA_MSIL)
            return peMSIL;

        if (flags & afPA_x86)
            return peI386;

        if (flags & afPA_IA64)
            return peIA64;

        if (flags & afPA_AMD64)
            return peAMD64;

        if (flags & afPA_ARM64)
            return peARM64;

        return peNone;
    }
}

VOID BaseAssemblySpec::GetDisplayName(DWORD flags, SString &result) const
{
    if (flags==0)
        flags=ASM_DISPLAYF_FULL;

    BINDER_SPACE::AssemblyIdentity assemblyIdentity;
    SString tmpString;

    tmpString.SetUTF8(m_pAssemblyName);

    if ((m_ownedFlags & BAD_NAME_OWNED) != 0)
    {
        // Can't do anything with a broken name
        tmpString.ConvertToUnicode(result);
        return;
    }
    else
    {
        tmpString.ConvertToUnicode(assemblyIdentity.m_simpleName);
        assemblyIdentity.SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_SIMPLE_NAME);
    }

    if( flags & ASM_DISPLAYF_VERSION  &&  m_context.usMajorVersion != 0xFFFF)
    {
        assemblyIdentity.m_version.SetFeatureVersion(m_context.usMajorVersion,
                                                     m_context.usMinorVersion);
        assemblyIdentity.m_version.SetServiceVersion(m_context.usBuildNumber,
                                                     m_context.usRevisionNumber);
        assemblyIdentity.SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_VERSION);
    }

    if(flags & ASM_DISPLAYF_CULTURE)
    {
        assemblyIdentity.SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE);
        if ((m_context.szLocale != NULL) && (m_context.szLocale[0] != 0))
        {
            tmpString.SetUTF8(m_context.szLocale);
            tmpString.ConvertToUnicode(assemblyIdentity.m_cultureOrLanguage);
        }
    }

    if(flags & ASM_DISPLAYF_PUBLIC_KEY_TOKEN)
    {
        if (m_cbPublicKeyOrToken)
        {
            assemblyIdentity.SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
            if(IsAfPublicKeyToken(m_dwFlags))
            {
                assemblyIdentity.m_publicKeyOrTokenBLOB.Set(m_pbPublicKeyOrToken,
                                                            m_cbPublicKeyOrToken);
            }
            else
            {
                DWORD cbToken = 0;
                StrongNameBufferHolder<BYTE> pbToken;

                // Try to get the strong name
                IfFailThrow(StrongNameTokenFromPublicKey(m_pbPublicKeyOrToken,
                    m_cbPublicKeyOrToken,
                    &pbToken,
                    &cbToken));

                assemblyIdentity.m_publicKeyOrTokenBLOB.Set(pbToken, cbToken);
            }
        }
        else
        {
            assemblyIdentity.
                SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL);
        }
    }

    if ((flags & ASM_DISPLAYF_PROCESSORARCHITECTURE) && (m_dwFlags & afPA_Mask))
    {
        assemblyIdentity.
            SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE);

        assemblyIdentity.m_kProcessorArchitecture = GetProcessorArchitectureFromAssemblyFlags(m_dwFlags);
    }

    if ((flags & ASM_DISPLAYF_RETARGET) && (m_dwFlags & afRetargetable))
    {
        assemblyIdentity.SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE);
    }

    if ((flags & ASM_DISPLAYF_CONTENT_TYPE) && (m_dwFlags & afContentType_Mask) == afContentType_WindowsRuntime)
    {
        assemblyIdentity.SetHave(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE);
        assemblyIdentity.m_kContentType = AssemblyContentType_WindowsRuntime;
    }

    IfFailThrow(BINDER_SPACE::TextualIdentityParser::ToString(&assemblyIdentity,
                                                              assemblyIdentity.m_dwIdentityFlags,
                                                              result));
}

void BaseAssemblySpec::PopulateAssemblyNameData(AssemblyNameData &data) const
{
    data.Name = m_pAssemblyName;
    data.IdentityFlags = BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_SIMPLE_NAME;

    if (m_context.usMajorVersion != 0xFFFF)
    {
        data.MajorVersion = m_context.usMajorVersion;
        data.MinorVersion = m_context.usMinorVersion;
        data.BuildNumber = m_context.usBuildNumber;
        data.RevisionNumber = m_context.usRevisionNumber;
        data.IdentityFlags |= BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_VERSION;
    }

    if (m_context.szLocale != NULL && m_context.szLocale[0] != 0)
    {
        data.Culture = m_context.szLocale;
        data.IdentityFlags |= BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE;
    }

    data.PublicKeyOrTokenLength = m_cbPublicKeyOrToken;
    if (m_cbPublicKeyOrToken > 0)
    {
        data.PublicKeyOrToken = m_pbPublicKeyOrToken;
        data.IdentityFlags |= IsAfPublicKeyToken(m_dwFlags)
            ? BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN
            : BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY;
    }
    else
    {
        data.IdentityFlags |= BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL;
    }

    if ((m_dwFlags & afPA_Mask) != 0)
    {
        data.ProcessorArchitecture = GetProcessorArchitectureFromAssemblyFlags(m_dwFlags);
        data.IdentityFlags |= BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE;
    }

    if ((m_dwFlags & afRetargetable) != 0)
    {
        data.IdentityFlags |= BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE;
    }

    if ((m_dwFlags & afContentType_Mask) == afContentType_WindowsRuntime)
    {
        data.ContentType = AssemblyContentType_WindowsRuntime;
        data.IdentityFlags |= BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE;
    }
}
