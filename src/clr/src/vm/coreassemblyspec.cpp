// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "../binder/inc/assemblybinder.hpp"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

#ifndef FEATURE_FUSION

#include "../binder/inc/textualidentityparser.hpp"
#include "../binder/inc/assemblyidentity.hpp"
#include "../binder/inc/assembly.hpp"
#include "../binder/inc/assemblyname.hpp"
#include "../binder/inc/fusionassemblyname.hpp"

#include "../binder/inc/coreclrbindercommon.h"
#include "../binder/inc/applicationcontext.hpp"
#ifndef DACCESS_COMPILE

STDAPI BinderGetImagePath(PEImage *pPEImage,
                          SString &imagePath)
{
    HRESULT hr = S_OK;

    _ASSERTE(pPEImage != NULL);

    imagePath.Set(pPEImage->GetPath());
    return hr;
}

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

STDAPI BinderGetDisplayName(PEAssembly *pAssembly,
                            SString    &displayName)
{
    HRESULT hr = S_OK;

    if (pAssembly != NULL)
    {
        pAssembly->GetDisplayName(displayName, ASM_DISPLAYF_FULL);
    }

    return hr;
}


#ifdef FEATURE_VERSIONING

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
                         BOOL fExplicitBindToNativeImage /* = FALSE */,
                         StackCrawlMark *pCallerStackMark /* = NULL */)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pResult));
        PRECONDITION(CheckPointer(pAppDomain));
        PRECONDITION(IsMscorlib() == FALSE); // This should never be called for MSCORLIB (explicit loading)
    }
    CONTRACTL_END;

    ReleaseHolder<BINDER_SPACE::Assembly> result;
    HRESULT hr=S_OK;
    
    SString assemblyDisplayName;

    pResult->Reset();    

    if (m_wszCodeBase==NULL)
    {
        GetFileOrDisplayName(0, assemblyDisplayName);
    }

    // Have a default binding context setup
    ICLRPrivBinder *pBinder = GetBindingContextFromParentAssembly(pAppDomain); 
     
    // Get the reference to the TPABinder context
    CLRPrivBinderCoreCLR *pTPABinder = pAppDomain->GetTPABinderContext();
    
    ReleaseHolder<ICLRPrivAssembly> pPrivAsm;
    _ASSERTE(pBinder != NULL);

    if (m_wszCodeBase==NULL && IsMscorlibSatellite())
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
    else if(m_wszCodeBase==NULL)
    {
        // For name based binding these arguments shouldnt have been changed from default
        _ASSERTE(!fNgenExplicitBind && !fExplicitBindToNativeImage);
        SafeComHolder<IAssemblyName> pName;
        hr = CreateAssemblyNameObject(&pName, assemblyDisplayName, CANOF_PARSE_DISPLAY_NAME, NULL);
        if (SUCCEEDED(hr))
        {
            hr = pBinder->BindAssemblyByName(pName, &pPrivAsm);
        }
    }
    else
    {
        hr = pTPABinder->Bind(assemblyDisplayName,
                           m_wszCodeBase,
                           GetParentAssembly()? GetParentAssembly()->GetFile():NULL,
                           fNgenExplicitBind,
                           fExplicitBindToNativeImage,
                          &pPrivAsm);
    }

    if(SUCCEEDED(hr))
    {
        _ASSERTE(pPrivAsm != nullptr);
        result = BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(pPrivAsm.Extract());
        _ASSERTE(result != nullptr);
    }
    
    pResult->SetHRBindResult(hr);
    if (SUCCEEDED(hr))
    {
        BOOL fIsInGAC = pAppDomain->IsImageFromTrustedPath(result->GetNativeOrILPEImage());
        BOOL fIsOnTpaList = FALSE;
        const SString &sImagePath = result->GetNativeOrILPEImage()->GetPath();
        if (pTPABinder->IsInTpaList(sImagePath))
        {
            fIsOnTpaList = TRUE;
        }
        pResult->Init(result,fIsInGAC, fIsOnTpaList);
    }
    else if (FAILED(hr) && (fThrowOnFileNotFound || (!Assembly::FileNotFound(hr))))
    {
        ThrowLoadError(this, hr);
    }
}

#endif // FEATURE_VERSIONING

STDAPI BinderAcquirePEImage(LPCWSTR   wszAssemblyPath,
                            PEImage **ppPEImage,
                            PEImage **ppNativeImage,
                            BOOL      fExplicitBindToNativeImage)
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
            pNativeImage = PEImage::OpenImage(wszAssemblyPath, MDInternalImport_TrustedNativeImage);

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
            pImage = PEImage::OpenImage(wszAssemblyPath, MDInternalImport_Default);

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
    *result = pPEImage->HasNativeHeader();
    return S_OK;
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

        if (bNativeImage && pPEImage->IsNativeILILOnly())
        {
            pPEImage->GetNativeILPEKindAndMachine(&pdwPAFlags[0], &pdwPAFlags[1]);
        }
        else
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
        IUnknown *pIUnknownBinder = pDomain->GetFusionContext();

        if (pIUnknownBinder != NULL)
        {
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
            if (pDomain->GetFusionContext() != pDomain->GetTPABinderContext())
            {
                pAppContext = (static_cast<CLRPrivBinderAssemblyLoadContext *>(pIUnknownBinder))->GetAppContext();
            }
            else
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
            {
                pAppContext = (static_cast<CLRPrivBinderCoreCLR *>(pIUnknownBinder))->GetAppContext();
            }
        }

        hr = CCoreCLRBinderHelper::GetAssemblyIdentity(m_pAssemblyName, pAppContext, pAssemblyIdentity);

        if (FAILED(hr))
        {
            m_ownedFlags |= BAD_NAME_OWNED;
            IfFailThrow(hr);
        }

        SetName(pAssemblyIdentity->GetSimpleNameUTF8());

        if (pAssemblyIdentity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_VERSION))
        {
            m_context.usMajorVersion = (USHORT)pAssemblyIdentity->m_version.GetMajor();
            m_context.usMinorVersion = (USHORT)pAssemblyIdentity->m_version.GetMinor();
            m_context.usBuildNumber = (USHORT)pAssemblyIdentity->m_version.GetBuild();
            m_context.usRevisionNumber = (USHORT)pAssemblyIdentity->m_version.GetRevision();
        }
            
        if (pAssemblyIdentity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CULTURE))
        {
            if (!pAssemblyIdentity->m_cultureOrLanguage.IsEmpty())
                SetCulture(pAssemblyIdentity->GetCultureOrLanguageUTF8());
            else
                SetCulture("");
        }

        if (pAssemblyIdentity->
            Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN) ||
            pAssemblyIdentity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY))
        {
            m_pbPublicKeyOrToken = const_cast<BYTE *>(pAssemblyIdentity->GetPublicKeyOrTokenArray());
            m_cbPublicKeyOrToken = pAssemblyIdentity->m_publicKeyOrTokenBLOB.GetSize();
            
            if (pAssemblyIdentity->Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY))
            {
                m_dwFlags |= afPublicKey;
            }
        }
        else if (pAssemblyIdentity->
                 Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL))
        {
            m_pbPublicKeyOrToken = const_cast<BYTE *>(pAssemblyIdentity->GetPublicKeyOrTokenArray());
            m_cbPublicKeyOrToken = 0;
        }
        else
        {
            m_pbPublicKeyOrToken = NULL;
            m_cbPublicKeyOrToken = 0;
        }

        if (pAssemblyIdentity->
            Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE))
        {
            switch (pAssemblyIdentity->m_kProcessorArchitecture)
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

        
        if (pAssemblyIdentity->
            Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE))
        {
            m_dwFlags |= afRetargetable;
        }

        if (pAssemblyIdentity->
            Have(BINDER_SPACE::AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE))
        {
            DWORD dwContentType = pAssemblyIdentity->m_kContentType;
            
            _ASSERTE((dwContentType == AssemblyContentType_Default) || (dwContentType == AssemblyContentType_WindowsRuntime));
            if (dwContentType == AssemblyContentType_WindowsRuntime)
            {
                m_dwFlags |= afContentType_WindowsRuntime;
            }
        }
        
        CloneFields();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
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
                if (!StrongNameTokenFromPublicKey(m_pbPublicKeyOrToken,
                                                  m_cbPublicKeyOrToken,
                                                  &pbToken,
                                                  &cbToken))
                {
                    // Throw an exception with details on what went wrong
                    COMPlusThrowHR(StrongNameErrorInfo());
                }

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

        if (m_dwFlags & afPA_MSIL)
            assemblyIdentity.m_kProcessorArchitecture = peMSIL;
        else if (m_dwFlags & afPA_x86)
            assemblyIdentity.m_kProcessorArchitecture = peI386; 
        else if (m_dwFlags & afPA_IA64)
            assemblyIdentity.m_kProcessorArchitecture = peIA64;
        else if (m_dwFlags & afPA_AMD64)
            assemblyIdentity.m_kProcessorArchitecture = peAMD64;
        else if (m_dwFlags & afPA_ARM)
            assemblyIdentity.m_kProcessorArchitecture = peARM;
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

#ifndef FEATURE_CORECLR

//
// Trivial assembly binder for desktop crossgen
//

VOID  AssemblySpec::Bind(AppDomain      *pAppDomain,
                             BOOL            fThrowOnFileNotFound,
                             CoreBindResult *pResult,
                             BOOL fNgenExplicitBind /* = FALSE */,
                             BOOL fExplicitBindToNativeImage /* = FALSE */,
                             StackCrawlMark *pCallerStackMark /* = NULL */)
{
    PEImageHolder pImage;
    BOOL fNativeImage = FALSE;

    if (GetCodeBase() != NULL)
    {
        // Normalize the path to maintain identity
        SString sFullAssemblyPath;
        Clr::Util::Win32::GetFullPathName(GetCodeBase(), sFullAssemblyPath, NULL);

        pImage = PEImage::OpenImage(sFullAssemblyPath, MDInternalImport_Default);
    }
    else
    {
        SString sSimpleName(SString::Utf8, m_pAssemblyName);

        fNativeImage = !IsReadyToRunCompilation() && pAppDomain->ToCompilationDomain()->IsInHardBindList(sSimpleName);

        SString sFileName(sSimpleName, fNativeImage ? W(".ni.dll") : W(".dll"));

        if (!CompilationDomain::FindImage(sFileName,
                fNativeImage ?  MDInternalImport_TrustedNativeImage : MDInternalImport_Default,
                &pImage))
        {
            sFileName.Set(sSimpleName, fNativeImage ? W(".ni.exe") : W(".exe"));

            if (!CompilationDomain::FindImage(sFileName,
                    fNativeImage ?  MDInternalImport_TrustedNativeImage : MDInternalImport_Default,
                    &pImage))
            {
                EEFileLoadException::Throw(sSimpleName, COR_E_FILENOTFOUND);
            }
        }
    }

    GetSvcLogger()->Printf(W("Loading %s\n"), pImage->GetPath().GetUnicode());

    NewHolder<BINDER_SPACE::Assembly> pAssembly;
    pAssembly = new BINDER_SPACE::Assembly();

    pAssembly->m_assemblyPath.Set(pImage->GetPath());

    if (fNativeImage)
        pAssembly->SetNativePEImage(pImage.Extract());
    else
        pAssembly->SetPEImage(pImage.Extract());

    pResult->Init(pAssembly.Extract(), TRUE, TRUE);
}

VOID AssemblySpec::BindToSystem(BINDER_SPACE::Assembly** ppAssembly)
{
    PEImageHolder pImage;
    BOOL fNativeImage = FALSE;

    _ASSERTE(ppAssembly != nullptr);

    if (g_fAllowNativeImages)
    {
        if (CompilationDomain::FindImage(W("mscorlib.ni.dll"), MDInternalImport_TrustedNativeImage, &pImage))
            fNativeImage = TRUE;
    }

    if (!fNativeImage)
    {
        if (!CompilationDomain::FindImage(W("mscorlib.dll"), MDInternalImport_Default, &pImage))
        {
            EEFileLoadException::Throw(W("mscorlib.dll"), COR_E_FILENOTFOUND);
        }
    }

    GetSvcLogger()->Printf(W("Loading %s\n"), pImage->GetPath().GetUnicode());

    NewHolder<BINDER_SPACE::Assembly> pAssembly;
    pAssembly = new BINDER_SPACE::Assembly();

    pAssembly->m_assemblyPath.Set(pImage->GetPath());

    if (fNativeImage)
        pAssembly->SetNativePEImage(pImage.Extract());
    else
        pAssembly->SetPEImage(pImage.Extract());

    *ppAssembly = pAssembly.Extract();
}

#endif // !FEATURE_CORECLR

#endif // FEATURE_FUSION
