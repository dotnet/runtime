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
#include "domainfile.h"
#include "holder.h"
#include "bundle.h"
#include "strongnameinternal.h"
#include "strongnameholders.h"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

#include "../binder/inc/textualidentityparser.hpp"
#include "../binder/inc/assemblyidentity.hpp"
#include "../binder/inc/assembly.hpp"
#include "../binder/inc/assemblyname.hpp"

#include "../binder/inc/coreclrbindercommon.h"
#include "../binder/inc/applicationcontext.hpp"
#ifndef DACCESS_COMPILE

STDAPI BinderAddRefPEImage(PEImage *pPEImage)
{
    HRESULT hr = S_OK;

    if (pPEImage != NULL)
    {
        pPEImage->AddRef();
    }

    return hr;
}

STDAPI BinderReleasePEImage(PEImage *pPEImage)
{
    HRESULT hr = S_OK;

    if (pPEImage != NULL)
    {
        pPEImage->Release();
    }

    return hr;
}

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
    pSpec->GetFileOrDisplayName(0, name);
    EEFileLoadException::Throw(name, hr);
}

// See code:BINDER_SPACE::AssemblyBinder::GetAssembly for info on fNgenExplicitBind
// and fExplicitBindToNativeImage, and see code:CEECompileInfo::LoadAssemblyByPath
// for an example of how they're used.
VOID  AssemblySpec::Bind(AppDomain      *pAppDomain,
                         BOOL            fThrowOnFileNotFound,
                         CoreBindResult *pResult,
                         BOOL fNgenExplicitBind /* = FALSE */,
                         BOOL fExplicitBindToNativeImage /* = FALSE */)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pResult));
        PRECONDITION(CheckPointer(pAppDomain));
        PRECONDITION(IsCoreLib() == FALSE); // This should never be called for CoreLib (explicit loading)
    }
    CONTRACTL_END;

    ReleaseHolder<BINDER_SPACE::Assembly> result;
    HRESULT hr=S_OK;

    pResult->Reset();

    // Have a default binding context setup
    ICLRPrivBinder *pBinder = GetBindingContextFromParentAssembly(pAppDomain);

    // Get the reference to the TPABinder context
    CLRPrivBinderCoreCLR *pTPABinder = pAppDomain->GetTPABinderContext();

    ReleaseHolder<ICLRPrivAssembly> pPrivAsm;
    _ASSERTE(pBinder != NULL);

    if (m_wszCodeBase == NULL && IsCoreLibSatellite())
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

        hr = CCoreCLRBinderHelper::BindToSystemSatellite(sSystemDirectory, sSimpleName, sCultureName, &pPrivAsm);
    }
    else if (m_wszCodeBase == NULL)
    {
        // For name based binding these arguments shouldn't have been changed from default
        _ASSERTE(!fNgenExplicitBind && !fExplicitBindToNativeImage);
        AssemblyNameData assemblyNameData = { 0 };
        PopulateAssemblyNameData(assemblyNameData);
        hr = pBinder->BindAssemblyByName(&assemblyNameData, &pPrivAsm);
    }
    else
    {
        hr = pTPABinder->Bind(m_wszCodeBase,
                              GetParentAssembly() ? GetParentAssembly()->GetFile() : NULL,
                              fNgenExplicitBind,
                              fExplicitBindToNativeImage,
                              &pPrivAsm);
    }

    pResult->SetHRBindResult(hr);
    if (SUCCEEDED(hr))
    {
        _ASSERTE(pPrivAsm != nullptr);

        result = BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(pPrivAsm.Extract());
        _ASSERTE(result != nullptr);

        pResult->Init(result);
    }
    else if (FAILED(hr) && (fThrowOnFileNotFound || (!Assembly::FileNotFound(hr))))
    {
        ThrowLoadError(this, hr);
    }
}


STDAPI BinderAcquirePEImage(LPCWSTR             wszAssemblyPath,
                            PEImage           **ppPEImage,
                            PEImage           **ppNativeImage,
                            BOOL                fExplicitBindToNativeImage,
                            BundleFileLocation  bundleFileLocation)
{
    HRESULT hr = S_OK;

    _ASSERTE(ppPEImage != NULL);

    EX_TRY
    {
        PEImageHolder pImage = NULL;
        PEImageHolder pNativeImage = NULL;

#ifdef FEATURE_PREJIT
        // fExplicitBindToNativeImage is set on Phone when we bind to a list of native images and have no IL on device for an assembly
        if (fExplicitBindToNativeImage)
        {
            pNativeImage = PEImage::OpenImage(wszAssemblyPath, MDInternalImport_TrustedNativeImage, bundleFileLocation);

            // Make sure that the IL image can be opened if the native image is not available.
            hr=pNativeImage->TryOpenFile();
            if (FAILED(hr))
            {
                goto Exit;
            }
        }
        else
#endif
        {
            pImage = PEImage::OpenImage(wszAssemblyPath, MDInternalImport_Default, bundleFileLocation);

            // Make sure that the IL image can be opened if the native image is not available.
            hr=pImage->TryOpenFile();
            if (FAILED(hr))
            {
                goto Exit;
            }
        }

        if (pImage)
            *ppPEImage = pImage.Extract();

        if (ppNativeImage)
            *ppNativeImage = pNativeImage.Extract();
    }
    EX_CATCH_HRESULT(hr);

 Exit:
    return hr;
}

STDAPI BinderHasNativeHeader(PEImage *pPEImage, BOOL* result)
{
    HRESULT hr = S_OK;

    _ASSERTE(pPEImage != NULL);
    _ASSERTE(result != NULL);

    EX_TRY
    {
        *result = pPEImage->HasNativeHeader();
    }
    EX_CATCH_HRESULT(hr);

    if (FAILED(hr))
    {
        *result = false;

#if defined(TARGET_UNIX)
        // PAL_LOADLoadPEFile may fail while loading IL masquerading as NI.
        // This will result in a ThrowHR(E_FAIL).  Suppress the error.
        if(hr == E_FAIL)
        {
            hr = S_OK;
        }
#endif // defined(TARGET_UNIX)
    }

    return hr;
}

STDAPI BinderAcquireImport(PEImage                  *pPEImage,
                           IMDInternalImport       **ppIAssemblyMetaDataImport,
                           DWORD                    *pdwPAFlags,
                           BOOL                      bNativeImage)
{
    HRESULT hr = S_OK;

    _ASSERTE(pPEImage != NULL);
    _ASSERTE(ppIAssemblyMetaDataImport != NULL);
    _ASSERTE(pdwPAFlags != NULL);

    EX_TRY
    {
        PEImageLayoutHolder pLayout(pPEImage->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED));

        // CheckCorHeader includes check of NT headers too
        if (!pLayout->CheckCorHeader())
            IfFailGo(COR_E_ASSEMBLYEXPECTED);

        if (!pLayout->CheckFormat())
            IfFailGo(COR_E_BADIMAGEFORMAT);

#ifdef FEATURE_PREJIT
        if (bNativeImage && pPEImage->IsNativeILILOnly())
        {
            pPEImage->GetNativeILPEKindAndMachine(&pdwPAFlags[0], &pdwPAFlags[1]);
        }
        else
#endif
        {
            pPEImage->GetPEKindAndMachine(&pdwPAFlags[0], &pdwPAFlags[1]);
        }

        *ppIAssemblyMetaDataImport = pPEImage->GetMDImport();
        if (!*ppIAssemblyMetaDataImport)
        {
            // Some native images don't contain metadata, to reduce size
            if (!bNativeImage)
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
        NewHolder<BINDER_SPACE::AssemblyIdentityUTF8> pAssemblyIdentity;
        AppDomain *pDomain = ::GetAppDomain();
        _ASSERTE(pDomain);

        BINDER_SPACE::ApplicationContext *pAppContext = NULL;
        CLRPrivBinderCoreCLR *pBinder = pDomain->GetTPABinderContext();
        if (pBinder != NULL)
        {
            pAppContext = pBinder->GetAppContext();
        }

        hr = CCoreCLRBinderHelper::GetAssemblyIdentity(m_pAssemblyName, pAppContext, pAssemblyIdentity);

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

#endif // DACCESS_COMPILE

VOID BaseAssemblySpec::GetFileOrDisplayName(DWORD flags, SString &result) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckValue(result));
        PRECONDITION(result.IsEmpty());
    }
    CONTRACTL_END;

    if (m_wszCodeBase)
    {
        result.Set(m_wszCodeBase);
        return;
    }

    GetDisplayNameInternal(flags, result);
}

VOID BaseAssemblySpec::GetDisplayName(DWORD flags, SString &result) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckValue(result));
        PRECONDITION(result.IsEmpty());
    }
    CONTRACTL_END;

    GetDisplayNameInternal(flags, result);
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

VOID BaseAssemblySpec::GetDisplayNameInternal(DWORD flags, SString &result) const
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
