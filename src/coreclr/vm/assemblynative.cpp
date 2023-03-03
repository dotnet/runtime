// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  AssemblyNative.cpp
**
** Purpose: Implements AssemblyNative (loader domain) architecture
**
**


**
===========================================================*/

#include "common.h"

#include <stdlib.h>
#include "assemblynative.hpp"
#include "dllimport.h"
#include "field.h"
#include "eeconfig.h"
#include "interoputil.h"
#include "frames.h"
#include "typeparse.h"
#include "encee.h"
#include "threadsuspend.h"

#include "appdomainnative.hpp"
#include "../binder/inc/bindertracing.h"
#include "../binder/inc/defaultassemblybinder.h"

extern "C" void QCALLTYPE AssemblyNative_InternalLoad(NativeAssemblyNameParts* pAssemblyNameParts,
                                                      QCall::ObjectHandleOnStack requestingAssembly,
                                                      QCall::StackCrawlMarkHandle stackMark,
                                                      BOOL fThrowOnFileNotFound,
                                                      QCall::ObjectHandleOnStack assemblyLoadContext,
                                                      QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    DomainAssembly * pParentAssembly = NULL;
    Assembly * pRefAssembly = NULL;
    AssemblyBinder *pBinder = NULL;

    {
        GCX_COOP();

        if (assemblyLoadContext.Get() != NULL)
        {
            INT_PTR nativeAssemblyBinder = ((ASSEMBLYLOADCONTEXTREF)assemblyLoadContext.Get())->GetNativeAssemblyBinder();
            pBinder = reinterpret_cast<AssemblyBinder*>(nativeAssemblyBinder);
        }

        // Compute parent assembly
        if (requestingAssembly.Get() != NULL)
        {
            pRefAssembly = ((ASSEMBLYREF)requestingAssembly.Get())->GetAssembly();
        }
        else if (pBinder == NULL)
        {
            pRefAssembly = SystemDomain::GetCallersAssembly(stackMark);
        }
        if (pRefAssembly)
        {
            pParentAssembly = pRefAssembly->GetDomainAssembly();
        }
    }

    AssemblySpec spec;

    if (pAssemblyNameParts->_pName == NULL)
        COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));

    StackSString ssName;
    ssName.SetAndConvertToUTF8(pAssemblyNameParts->_pName);

    AssemblyMetaDataInternal asmInfo;

    asmInfo.usMajorVersion = pAssemblyNameParts->_major;
    asmInfo.usMinorVersion = pAssemblyNameParts->_minor;
    asmInfo.usBuildNumber = pAssemblyNameParts->_build;
    asmInfo.usRevisionNumber = pAssemblyNameParts->_revision;

    SmallStackSString ssLocale;
    if (pAssemblyNameParts->_pCultureName != NULL)
        ssLocale.SetAndConvertToUTF8(pAssemblyNameParts->_pCultureName);
    asmInfo.szLocale = (pAssemblyNameParts->_pCultureName != NULL) ? ssLocale.GetUTF8() : NULL;

    // Initialize spec
    spec.Init(ssName.GetUTF8(), &asmInfo,
        pAssemblyNameParts->_pPublicKeyOrToken, pAssemblyNameParts->_cbPublicKeyOrToken, pAssemblyNameParts->_flags);

    if (pParentAssembly != NULL)
        spec.SetParentAssembly(pParentAssembly);

    // Have we been passed the reference to the binder against which this load should be triggered?
    // If so, then use it to set the fallback load context binder.
    if (pBinder != NULL)
    {
        spec.SetFallbackBinderForRequestingAssembly(pBinder);
        spec.SetPreferFallbackBinder();
    }
    else if (pRefAssembly != NULL)
    {
        // If the requesting assembly has Fallback LoadContext binder available,
        // then set it up in the AssemblySpec.
        PEAssembly *pRefAssemblyManifestFile = pRefAssembly->GetPEAssembly();
        spec.SetFallbackBinderForRequestingAssembly(pRefAssemblyManifestFile->GetFallbackBinder());
    }

    Assembly *pAssembly = spec.LoadAssembly(FILE_LOADED, fThrowOnFileNotFound);

    if (pAssembly != NULL)
    {
        GCX_COOP();
        retAssembly.Set(pAssembly->GetExposedObject());
    }

    END_QCALL;
}

/* static */
Assembly* AssemblyNative::LoadFromPEImage(AssemblyBinder* pBinder, PEImage *pImage, bool excludeAppPaths)
{
    CONTRACT(Assembly*)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pBinder));
        PRECONDITION(pImage != NULL);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    Assembly *pLoadedAssembly = NULL;
    ReleaseHolder<BINDER_SPACE::Assembly> pAssembly;

    DWORD dwMessageID = IDS_EE_FILELOAD_ERROR_GENERIC;

    // Set the caller's assembly to be CoreLib
    DomainAssembly *pCallersAssembly = SystemDomain::System()->SystemAssembly()->GetDomainAssembly();

    // Initialize the AssemblySpec
    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(1, mdtAssembly), pImage->GetMDImport(), pCallersAssembly);
    spec.SetBinder(pBinder);

    BinderTracing::AssemblyBindOperation bindOperation(&spec, pImage->GetPath());

    HRESULT hr = S_OK;
    PTR_AppDomain pCurDomain = GetAppDomain();
    hr = pBinder->BindUsingPEImage(pImage, excludeAppPaths, &pAssembly);

    if (hr != S_OK)
    {
        // Give a more specific message for the case when we found the assembly with the same name already loaded.
        if (hr == COR_E_FILELOAD)
        {
            dwMessageID = IDS_HOST_ASSEMBLY_RESOLVER_ASSEMBLY_ALREADY_LOADED_IN_CONTEXT;
        }

        StackSString name;
        spec.GetDisplayName(0, name);
        COMPlusThrowHR(COR_E_FILELOAD, dwMessageID, name);
    }

    PEAssemblyHolder pPEAssembly(PEAssembly::Open(pAssembly->GetPEImage(), pAssembly));
    bindOperation.SetResult(pPEAssembly.GetValue());

    DomainAssembly *pDomainAssembly = pCurDomain->LoadDomainAssembly(&spec, pPEAssembly, FILE_LOADED);
    RETURN pDomainAssembly->GetAssembly();
}

extern "C" void QCALLTYPE AssemblyNative_LoadFromPath(INT_PTR ptrNativeAssemblyBinder, LPCWSTR pwzILPath, LPCWSTR pwzNIPath, QCall::ObjectHandleOnStack retLoadedAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    PTR_AppDomain pCurDomain = GetAppDomain();

    // Get the binder context in which the assembly will be loaded.
    AssemblyBinder *pBinder = reinterpret_cast<AssemblyBinder*>(ptrNativeAssemblyBinder);
    _ASSERTE(pBinder != NULL);

    // Form the PEImage for the ILAssembly. In case of an exception, the holder will ensure
    // the release of the image.
    PEImageHolder pILImage;

    if (pwzILPath != NULL)
    {
        pILImage = PEImage::OpenImage(pwzILPath,
                                      MDInternalImport_Default,
                                      BundleFileLocation::Invalid());

        // Need to verify that this is a valid CLR assembly.
        if (!pILImage->CheckILFormat())
            THROW_BAD_FORMAT(BFA_BAD_IL, pILImage.GetValue());

        LoaderAllocator* pLoaderAllocator = pBinder->GetLoaderAllocator();
        if (pLoaderAllocator && pLoaderAllocator->IsCollectible() && !pILImage->IsILOnly())
        {
            // Loading IJW assemblies into a collectible AssemblyLoadContext is not allowed
            THROW_BAD_FORMAT(BFA_IJW_IN_COLLECTIBLE_ALC, pILImage.GetValue());
        }
    }

    Assembly *pLoadedAssembly = AssemblyNative::LoadFromPEImage(pBinder, pILImage);

    {
        GCX_COOP();
        retLoadedAssembly.Set(pLoadedAssembly->GetExposedObject());
    }

    LOG((LF_CLASSLOADER,
            LL_INFO100,
            "\tLoaded assembly from a file\n"));

    END_QCALL;
}

/*static */
extern "C" void QCALLTYPE AssemblyNative_LoadFromStream(INT_PTR ptrNativeAssemblyBinder, INT_PTR ptrAssemblyArray,
                                              INT32 cbAssemblyArrayLength, INT_PTR ptrSymbolArray, INT32 cbSymbolArrayLength,
                                              QCall::ObjectHandleOnStack retLoadedAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Ensure that the invariants are in place
    _ASSERTE(ptrNativeAssemblyBinder != NULL);
    _ASSERTE((ptrAssemblyArray != NULL) && (cbAssemblyArrayLength > 0));
    _ASSERTE((ptrSymbolArray == NULL) || (cbSymbolArrayLength > 0));

    PEImageHolder pILImage(PEImage::CreateFromByteArray((BYTE*)ptrAssemblyArray, (COUNT_T)cbAssemblyArrayLength));

    // Need to verify that this is a valid CLR assembly.
    if (!pILImage->CheckILFormat())
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);

    // Get the binder context in which the assembly will be loaded
    AssemblyBinder *pBinder = reinterpret_cast<AssemblyBinder*>(ptrNativeAssemblyBinder);

    LoaderAllocator* pLoaderAllocator = pBinder->GetLoaderAllocator();
    if (pLoaderAllocator && pLoaderAllocator->IsCollectible() && !pILImage->IsILOnly())
    {
        // Loading IJW assemblies into a collectible AssemblyLoadContext is not allowed
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_IJW_IN_COLLECTIBLE_ALC);
    }

    // Pass the stream based assembly as IL in an attempt to bind and load it
    Assembly* pLoadedAssembly = AssemblyNative::LoadFromPEImage(pBinder, pILImage);
    {
        GCX_COOP();
        retLoadedAssembly.Set(pLoadedAssembly->GetExposedObject());
    }

    LOG((LF_CLASSLOADER,
            LL_INFO100,
            "\tLoaded assembly from a file\n"));

    // In order to assign the PDB image (if present),
    // the resulting assembly's image needs to be exactly the one
    // we created above. We need pointer comparison instead of pe image equivalence
    // to avoid mixed binaries/PDB pairs of other images.
    // This applies to both Desktop CLR and CoreCLR, with or without fusion.
    BOOL fIsSameAssembly = (pLoadedAssembly->GetPEAssembly()->GetPEImage() == pILImage);

    // Setting the PDB info is only applicable for our original assembly.
    // This applies to both Desktop CLR and CoreCLR, with or without fusion.
    if (fIsSameAssembly)
    {
#ifdef DEBUGGING_SUPPORTED
        // If we were given symbols, save a copy of them.
        if (ptrSymbolArray != NULL)
        {
            PBYTE pSymbolArray = reinterpret_cast<PBYTE>(ptrSymbolArray);
            pLoadedAssembly->GetModule()->SetSymbolBytes(pSymbolArray, (DWORD)cbSymbolArrayLength);
        }
#endif // DEBUGGING_SUPPORTED
    }

    END_QCALL;
}

#ifdef TARGET_WINDOWS
/*static */
extern "C" void QCALLTYPE AssemblyNative_LoadFromInMemoryModule(INT_PTR ptrNativeAssemblyBinder, INT_PTR hModule, QCall::ObjectHandleOnStack retLoadedAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Ensure that the invariants are in place
    _ASSERTE(ptrNativeAssemblyBinder != NULL);
    _ASSERTE(hModule != NULL);

    PEImageHolder pILImage(PEImage::CreateFromHMODULE((HMODULE)hModule));

    // Need to verify that this is a valid CLR assembly.
    if (!pILImage->HasCorHeader())
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);

    // Get the binder context in which the assembly will be loaded
    AssemblyBinder *pBinder = reinterpret_cast<AssemblyBinder*>(ptrNativeAssemblyBinder);

    // Pass the in memory module as IL in an attempt to bind and load it
    Assembly* pLoadedAssembly = AssemblyNative::LoadFromPEImage(pBinder, pILImage);
    {
        GCX_COOP();
        retLoadedAssembly.Set(pLoadedAssembly->GetExposedObject());
    }

    LOG((LF_CLASSLOADER,
            LL_INFO100,
            "\tLoaded assembly from pre-loaded native module\n"));

    END_QCALL;
}
#endif

extern "C" void QCALLTYPE AssemblyNative_GetLocation(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    {
        retString.Set(pAssembly->GetPEAssembly()->GetPath());
    }

    END_QCALL;
}

extern "C" void QCALLTYPE AssemblyNative_GetType(QCall::AssemblyHandle pAssembly,
                                       LPCWSTR wszName,
                                       BOOL bThrowOnError,
                                       BOOL bIgnoreCase,
                                       QCall::ObjectHandleOnStack retType,
                                       QCall::ObjectHandleOnStack keepAlive,
                                       QCall::ObjectHandleOnStack pAssemblyLoadContext)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(wszName));
    }
    CONTRACTL_END;

    TypeHandle retTypeHandle;

    BEGIN_QCALL;

    BOOL prohibitAsmQualifiedName = TRUE;

    AssemblyBinder * pBinder = NULL;

    if (*pAssemblyLoadContext.m_ppObject != NULL)
    {
        GCX_COOP();
        ASSEMBLYLOADCONTEXTREF * pAssemblyLoadContextRef = reinterpret_cast<ASSEMBLYLOADCONTEXTREF *>(pAssemblyLoadContext.m_ppObject);

        INT_PTR nativeAssemblyBinder = (*pAssemblyLoadContextRef)->GetNativeAssemblyBinder();

        pBinder = reinterpret_cast<AssemblyBinder *>(nativeAssemblyBinder);
    }

    // Load the class from this assembly (fail if it is in a different one).
    retTypeHandle = TypeName::GetTypeManaged(wszName, pAssembly, bThrowOnError, bIgnoreCase, prohibitAsmQualifiedName, pAssembly->GetAssembly(), (OBJECTREF*)keepAlive.m_ppObject, pBinder);

    if (!retTypeHandle.IsNull())
    {
         GCX_COOP();
         retType.Set(retTypeHandle.GetManagedClassObject());
    }

    END_QCALL;

    return;
}

extern "C" void QCALLTYPE AssemblyNative_GetForwardedType(QCall::AssemblyHandle pAssembly, mdToken mdtExternalType, QCall::ObjectHandleOnStack retType)
{
    CONTRACTL
    {
        QCALL_CHECK;
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    LPCSTR pszNameSpace;
    LPCSTR pszClassName;
    mdToken mdImpl;

    Assembly * pAsm = pAssembly->GetAssembly();
    Module *pManifestModule = pAsm->GetModule();
    IfFailThrow(pManifestModule->GetMDImport()->GetExportedTypeProps(mdtExternalType, &pszNameSpace, &pszClassName, &mdImpl, NULL, NULL));
    if (TypeFromToken(mdImpl) == mdtAssemblyRef)
    {
        NameHandle typeName(pszNameSpace, pszClassName);
        typeName.SetTypeToken(pManifestModule, mdtExternalType);
        TypeHandle typeHnd = pAsm->GetLoader()->LoadTypeHandleThrowIfFailed(&typeName);
        {
            GCX_COOP();
            retType.Set(typeHnd.GetManagedClassObject());
        }
    }

    END_QCALL;

    return;
}

FCIMPL1(FC_BOOL_RET, AssemblyNative::IsDynamic, AssemblyBaseObject* pAssemblyUNSAFE)
{
    FCALL_CONTRACT;

    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);

    if (refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(refAssembly->GetDomainAssembly()->GetPEAssembly()->IsDynamic());
}
FCIMPLEND

extern "C" void QCALLTYPE AssemblyNative_GetVersion(QCall::AssemblyHandle pAssembly, INT32* pMajorVersion, INT32* pMinorVersion, INT32*pBuildNumber, INT32* pRevisionNumber)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    UINT16 major=0xffff, minor=0xffff, build=0xffff, revision=0xffff;

    pAssembly->GetPEAssembly()->GetVersion(&major, &minor, &build, &revision);

    *pMajorVersion = major;
    *pMinorVersion = minor;
    *pBuildNumber = build;
    *pRevisionNumber = revision;

    END_QCALL;
}

extern "C" void QCALLTYPE AssemblyNative_GetPublicKey(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retPublicKey)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    DWORD cbPublicKey = 0;
    const void *pbPublicKey = pAssembly->GetPEAssembly()->GetPublicKey(&cbPublicKey);
    retPublicKey.SetByteArray((BYTE *)pbPublicKey, cbPublicKey);

    END_QCALL;
}

extern "C" void QCALLTYPE AssemblyNative_GetSimpleName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retSimpleName)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    retSimpleName.Set(pAssembly->GetSimpleName());
    END_QCALL;
}

extern "C" void QCALLTYPE AssemblyNative_GetLocale(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    LPCUTF8 pLocale = pAssembly->GetPEAssembly()->GetLocale();
    if(pLocale)
    {
        retString.Set(pLocale);
    }

    END_QCALL;
}

extern "C" BOOL QCALLTYPE AssemblyNative_GetCodeBase(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BOOL ret = TRUE;

    BEGIN_QCALL;

    StackSString codebase;

    {
        ret = pAssembly->GetPEAssembly()->GetCodeBase(codebase);
    }

    retString.Set(codebase);
    END_QCALL;

    return ret;
}

extern "C" INT32 QCALLTYPE AssemblyNative_GetHashAlgorithm(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    INT32 retVal=0;
    BEGIN_QCALL;
    retVal = pAssembly->GetPEAssembly()->GetHashAlgId();
    END_QCALL;
    return retVal;
}

extern "C" INT32 QCALLTYPE AssemblyNative_GetFlags(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    INT32 retVal=0;
    BEGIN_QCALL;
    retVal = pAssembly->GetPEAssembly()->GetFlags();
    END_QCALL;
    return retVal;
}

extern "C" BYTE * QCALLTYPE AssemblyNative_GetResource(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, DWORD * length)
{
    QCALL_CONTRACT;

    PBYTE       pbInMemoryResource  = NULL;

    BEGIN_QCALL;

    if (wszName == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    // Get the name in UTF8
    StackSString name;
    name.SetAndConvertToUTF8(wszName);

    LPCUTF8 pNameUTF8 = name.GetUTF8();

    if (*pNameUTF8 == '\0')
        COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));

    pAssembly->GetResource(pNameUTF8, length,
                           &pbInMemoryResource, NULL, NULL,
                           NULL, FALSE);

    END_QCALL;

    // Can return null if resource file is zero-length
    return pbInMemoryResource;
}

extern "C" INT32 QCALLTYPE AssemblyNative_GetManifestResourceInfo(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, QCall::ObjectHandleOnStack retAssembly, QCall::StringHandleOnStack retFileName)
{
    QCALL_CONTRACT;

    INT32 rv = -1;

    BEGIN_QCALL;

    if (wszName == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    // Get the name in UTF8
    StackSString name;
    name.SetAndConvertToUTF8(wszName);
    LPCUTF8 pNameUTF8 = name.GetUTF8();

    if (*pNameUTF8 == '\0')
        COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));

    DomainAssembly * pReferencedAssembly = NULL;
    LPCSTR pFileName = NULL;
    DWORD dwLocation = 0;

    if (pAssembly->GetResource(pNameUTF8, NULL, NULL, &pReferencedAssembly, &pFileName,
                              &dwLocation, FALSE))
    {
        if (pFileName)
            retFileName.Set(pFileName);

        GCX_COOP();

        if (pReferencedAssembly)
            retAssembly.Set(pReferencedAssembly->GetExposedAssemblyObject());

        rv = dwLocation;
    }

    END_QCALL;

    return rv;
}

extern "C" void QCALLTYPE AssemblyNative_GetModules(QCall::AssemblyHandle pAssembly, BOOL fLoadIfNotFound, BOOL fGetResourceModules, QCall::ObjectHandleOnStack retModules)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    HENUMInternalHolder phEnum(pAssembly->GetMDImport());
    phEnum.EnumInit(mdtFile, mdTokenNil);

    InlineSArray<DomainAssembly *, 8> modules;

    modules.Append(pAssembly);

    mdFile mdFile;
    while (pAssembly->GetMDImport()->EnumNext(&phEnum, &mdFile))
    {
        if (fLoadIfNotFound)
        {
            DomainAssembly* pModule = pAssembly->GetModule()->LoadModule(mdFile);
            modules.Append(pModule);
        }
    }

    {
        GCX_COOP();

        PTRARRAYREF orModules = NULL;

        GCPROTECT_BEGIN(orModules);

        // Return the modules
        orModules = (PTRARRAYREF)AllocateObjectArray(modules.GetCount(), CoreLibBinder::GetClass(CLASS__MODULE));

        for(COUNT_T i = 0; i < modules.GetCount(); i++)
        {
            DomainAssembly * pModule = modules[i];

            OBJECTREF o = pModule->GetExposedModuleObject();
            orModules->SetAt(i, o);
        }

        retModules.Set(orModules);

        GCPROTECT_END();
    }

    END_QCALL;
}

extern "C" BOOL QCALLTYPE AssemblyNative_GetIsCollectible(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

    BEGIN_QCALL;

    retVal = pAssembly->IsCollectible();

    END_QCALL;

    return retVal;
}

extern volatile uint32_t g_cAssemblies;

extern "C" uint32_t QCALLTYPE AssemblyNative_GetAssemblyCount()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    return g_cAssemblies;
}

extern "C" void QCALLTYPE AssemblyNative_GetModule(QCall::AssemblyHandle pAssembly, LPCWSTR wszFileName, QCall::ObjectHandleOnStack retModule)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    Module * pModule = NULL;

    CQuickBytes qbLC;

    if (wszFileName == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_FileName"));
    if (wszFileName[0] == W('\0'))
        COMPlusThrow(kArgumentException, W("Argument_EmptyFileName"));


    MAKE_UTF8PTR_FROMWIDE(szModuleName, wszFileName);


    LPCUTF8 pModuleName = NULL;

    if SUCCEEDED(pAssembly->GetModule()->GetScopeName(&pModuleName))
    {
        if (::SString::_stricmp(pModuleName, szModuleName) == 0)
            pModule = pAssembly->GetModule();
    }


    if (pModule != NULL)
    {
        GCX_COOP();
        retModule.Set(pModule->GetExposedObject());
    }

    END_QCALL;

    return;
}

extern "C" void QCALLTYPE AssemblyNative_GetExportedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    InlineSArray<TypeHandle, 20> types;

    Assembly * pAsm = pAssembly->GetAssembly();

    IMDInternalImport *pImport = pAsm->GetMDImport();

    {
        HENUMTypeDefInternalHolder phTDEnum(pImport);
        phTDEnum.EnumTypeDefInit();

        mdTypeDef mdTD;
        while(pImport->EnumNext(&phTDEnum, &mdTD))
        {
            DWORD dwFlags;
            IfFailThrow(pImport->GetTypeDefProps(
                mdTD,
                &dwFlags,
                NULL));

            // nested type
            mdTypeDef mdEncloser = mdTD;
            while (SUCCEEDED(pImport->GetNestedClassProps(mdEncloser, &mdEncloser)) &&
                   IsTdNestedPublic(dwFlags))
            {
                IfFailThrow(pImport->GetTypeDefProps(
                    mdEncloser,
                    &dwFlags,
                    NULL));
            }

            if (IsTdPublic(dwFlags))
            {
                TypeHandle typeHnd = ClassLoader::LoadTypeDefThrowing(pAsm->GetModule(), mdTD,
                                                                      ClassLoader::ThrowIfNotFound,
                                                                      ClassLoader::PermitUninstDefOrRef);
                types.Append(typeHnd);
            }
        }
    }

    {
        HENUMInternalHolder phCTEnum(pImport);
        phCTEnum.EnumInit(mdtExportedType, mdTokenNil);

        // Now get the ExportedTypes that don't have TD's in the manifest file
        mdExportedType mdCT;
        while(pImport->EnumNext(&phCTEnum, &mdCT))
        {
            mdToken mdImpl;
            LPCSTR pszNameSpace;
            LPCSTR pszClassName;
            DWORD dwFlags;

            IfFailThrow(pImport->GetExportedTypeProps(
                mdCT,
                &pszNameSpace,
                &pszClassName,
                &mdImpl,
                NULL,           //binding
                &dwFlags));

            // nested type
            while ((TypeFromToken(mdImpl) == mdtExportedType) &&
                   (mdImpl != mdExportedTypeNil) &&
                   IsTdNestedPublic(dwFlags))
            {
                IfFailThrow(pImport->GetExportedTypeProps(
                    mdImpl,
                    NULL,       //namespace
                    NULL,       //name
                    &mdImpl,
                    NULL,       //binding
                    &dwFlags));
            }

            if ((TypeFromToken(mdImpl) == mdtFile) &&
                (mdImpl != mdFileNil) &&
                IsTdPublic(dwFlags))
            {
                NameHandle typeName(pszNameSpace, pszClassName);
                typeName.SetTypeToken(pAsm->GetModule(), mdCT);
                TypeHandle typeHnd = pAsm->GetLoader()->LoadTypeHandleThrowIfFailed(&typeName);

                types.Append(typeHnd);
            }
        }
    }

    {
        GCX_COOP();

        PTRARRAYREF orTypes = NULL;

        GCPROTECT_BEGIN(orTypes);

        // Return the types
        orTypes = (PTRARRAYREF)AllocateObjectArray(types.GetCount(), CoreLibBinder::GetClass(CLASS__TYPE));

        for(COUNT_T i = 0; i < types.GetCount(); i++)
        {
            TypeHandle typeHnd = types[i];

            OBJECTREF o = typeHnd.GetManagedClassObject();
            orTypes->SetAt(i, o);
        }

        retTypes.Set(orTypes);

        GCPROTECT_END();
    }

    END_QCALL;
}

extern "C" void QCALLTYPE AssemblyNative_GetForwardedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    InlineSArray<TypeHandle, 8> types;

    Assembly * pAsm = pAssembly->GetAssembly();

    IMDInternalImport *pImport = pAsm->GetMDImport();

    // enumerate the ExportedTypes table
    {
        HENUMInternalHolder phCTEnum(pImport);
        phCTEnum.EnumInit(mdtExportedType, mdTokenNil);

        // Now get the ExportedTypes that don't have TD's in the manifest file
        mdExportedType mdCT;
        while(pImport->EnumNext(&phCTEnum, &mdCT))
        {
            mdToken mdImpl;
            LPCSTR pszNameSpace;
            LPCSTR pszClassName;
            DWORD dwFlags;

            IfFailThrow(pImport->GetExportedTypeProps(mdCT,
                &pszNameSpace,
                &pszClassName,
                &mdImpl,
                NULL, //binding
                &dwFlags));

            if ((TypeFromToken(mdImpl) == mdtAssemblyRef) && (mdImpl != mdAssemblyRefNil))
            {
                NameHandle typeName(pszNameSpace, pszClassName);
                typeName.SetTypeToken(pAsm->GetModule(), mdCT);
                TypeHandle typeHnd = pAsm->GetLoader()->LoadTypeHandleThrowIfFailed(&typeName);

                types.Append(typeHnd);
            }
        }
    }

    // Populate retTypes
    {
        GCX_COOP();

        PTRARRAYREF orTypes = NULL;

        GCPROTECT_BEGIN(orTypes);

        // Return the types
        orTypes = (PTRARRAYREF)AllocateObjectArray(types.GetCount(), CoreLibBinder::GetClass(CLASS__TYPE));

        for(COUNT_T i = 0; i < types.GetCount(); i++)
        {
            TypeHandle typeHnd = types[i];

            OBJECTREF o = typeHnd.GetManagedClassObject();
            orTypes->SetAt(i, o);
        }

        retTypes.Set(orTypes);

        GCPROTECT_END();
    }

    END_QCALL;
}

FCIMPL1(Object*, AssemblyNative::GetManifestResourceNames, AssemblyBaseObject * pAssemblyUNSAFE)
{
    FCALL_CONTRACT;

    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);

    if (refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pAssembly = refAssembly->GetDomainAssembly();
    PTRARRAYREF rv = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_2(rv, refAssembly);

    IMDInternalImport *pImport = pAssembly->GetMDImport();

    HENUMInternalHolder phEnum(pImport);
    DWORD dwCount;

    phEnum.EnumInit(mdtManifestResource, mdTokenNil);
        dwCount = pImport->EnumGetCount(&phEnum);

    PTRARRAYREF ItemArray = (PTRARRAYREF) AllocateObjectArray(dwCount, g_pStringClass);

    mdManifestResource mdResource;

    GCPROTECT_BEGIN(ItemArray);
    for(DWORD i = 0;  i < dwCount; i++) {
        pImport->EnumNext(&phEnum, &mdResource);
        LPCSTR pszName = NULL;

        IfFailThrow(pImport->GetManifestResourceProps(
            mdResource,
            &pszName,   // name
            NULL,       // linkref
            NULL,       // offset
            NULL));     //flags

        OBJECTREF o = (OBJECTREF) StringObject::NewString(pszName);
        ItemArray->SetAt(i, o);
    }

    rv = ItemArray;
    GCPROTECT_END();

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(rv);
}
FCIMPLEND

FCIMPL1(Object*, AssemblyNative::GetReferencedAssemblies, AssemblyBaseObject * pAssemblyUNSAFE)
{
    FCALL_CONTRACT;

    struct {
        PTRARRAYREF ItemArray;
        ASSEMBLYNAMEREF pObj;
        ASSEMBLYREF refAssembly;
    } gc;
    gc.ItemArray = NULL;
    gc.pObj = NULL;
    gc.refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);

    if (gc.refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pAssembly = gc.refAssembly->GetDomainAssembly();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    IMDInternalImport *pImport = pAssembly->GetAssembly()->GetMDImport();

    MethodTable* pAsmNameClass = CoreLibBinder::GetClass(CLASS__ASSEMBLY_NAME);

    HENUMInternalHolder phEnum(pImport);
    DWORD dwCount = 0;

    phEnum.EnumInit(mdtAssemblyRef, mdTokenNil);

    dwCount = pImport->EnumGetCount(&phEnum);

    mdAssemblyRef mdAssemblyRef;

    gc.ItemArray = (PTRARRAYREF) AllocateObjectArray(dwCount, pAsmNameClass);

    for(DWORD i = 0; i < dwCount; i++)
    {
        pImport->EnumNext(&phEnum, &mdAssemblyRef);

        AssemblySpec spec;
        spec.InitializeSpec(mdAssemblyRef, pImport);

        gc.pObj = (ASSEMBLYNAMEREF) AllocateObject(pAsmNameClass);
        spec.AssemblyNameInit(&gc.pObj);

        gc.ItemArray->SetAt(i, (OBJECTREF) gc.pObj);
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.ItemArray);
}
FCIMPLEND

extern "C" void QCALLTYPE AssemblyNative_GetEntryPoint(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retMethod)
{
    QCALL_CONTRACT;

    MethodDesc* pMeth = NULL;

    BEGIN_QCALL;

    pMeth = pAssembly->GetAssembly()->GetEntryPoint();
    if (pMeth != NULL)
    {
        GCX_COOP();
        retMethod.Set(pMeth->GetStubMethodInfo());
    }

    END_QCALL;

    return;
}

//---------------------------------------------------------------------------------------
//
// Release QCALL for System.SafePEFileHandle
//
//

extern "C" void QCALLTYPE AssemblyNative_GetFullName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    StackSString name;
    pAssembly->GetPEAssembly()->GetDisplayName(name);
    retString.Set(name);

    END_QCALL;
}

extern "C" void QCALLTYPE AssemblyNative_GetExecutingAssembly(QCall::StackCrawlMarkHandle stackMark, QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    DomainAssembly * pExecutingAssembly = NULL;

    BEGIN_QCALL;

    Assembly* pAssembly = SystemDomain::GetCallersAssembly(stackMark);
    if(pAssembly)
    {
        pExecutingAssembly = pAssembly->GetDomainAssembly();
        GCX_COOP();
        retAssembly.Set(pExecutingAssembly->GetExposedAssemblyObject());
    }

    END_QCALL;
    return;
}

extern "C" void QCALLTYPE AssemblyNative_GetEntryAssembly(QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    DomainAssembly * pRootAssembly = NULL;
    Assembly * pAssembly = GetAppDomain()->GetRootAssembly();

    if (pAssembly)
    {
        pRootAssembly = pAssembly->GetDomainAssembly();
        GCX_COOP();
        retAssembly.Set(pRootAssembly->GetExposedAssemblyObject());
    }

    END_QCALL;

    return;
}

extern "C" void QCALLTYPE AssemblyNative_GetImageRuntimeVersion(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Retrieve the PEAssembly from the assembly.
    PEAssembly* pPEAssembly = pAssembly->GetPEAssembly();
    PREFIX_ASSUME(pPEAssembly!=NULL);

    LPCSTR pszVersion = NULL;
    IfFailThrow(pPEAssembly->GetMDImport()->GetVersionString(&pszVersion));

    SString version(SString::Utf8, pszVersion);

    // Allocate a managed string that contains the version and return it.
    retString.Set(version);

    END_QCALL;
}

/*static*/

extern "C" INT_PTR QCALLTYPE AssemblyNative_InitializeAssemblyLoadContext(INT_PTR ptrManagedAssemblyLoadContext, BOOL fRepresentsTPALoadContext, BOOL fIsCollectible)
{
    QCALL_CONTRACT;

    INT_PTR ptrNativeAssemblyBinder = NULL;

    BEGIN_QCALL;

    // We do not need to take a lock since this method is invoked from the ctor of AssemblyLoadContext managed type and
    // only one thread is ever executing a ctor for a given instance.
    //

    // Initialize the assembly binder instance in the VM
    PTR_AppDomain pCurDomain = AppDomain::GetCurrentDomain();
    DefaultAssemblyBinder *pDefaultBinder = pCurDomain->GetDefaultBinder();
    if (!fRepresentsTPALoadContext)
    {
        // Initialize a custom assembly binder
        CustomAssemblyBinder *pCustomBinder = NULL;

        AssemblyLoaderAllocator* loaderAllocator = NULL;
        OBJECTHANDLE loaderAllocatorHandle = NULL;

        if (fIsCollectible)
        {
            // Create a new AssemblyLoaderAllocator for an AssemblyLoadContext
            loaderAllocator = new AssemblyLoaderAllocator();
            loaderAllocator->SetCollectible();

            GCX_COOP();
            LOADERALLOCATORREF pManagedLoaderAllocator = NULL;
            GCPROTECT_BEGIN(pManagedLoaderAllocator);
            {
                GCX_PREEMP();
                // Some of the initialization functions are not virtual. Call through the derived class
                // to prevent calling the base class version.
                loaderAllocator->Init(pCurDomain);
                loaderAllocator->InitVirtualCallStubManager(pCurDomain);

                // Setup the managed proxy now, but do not actually transfer ownership to it.
                // Once everything is setup and nothing can fail anymore, the ownership will be
                // atomically transferred by call to LoaderAllocator::ActivateManagedTracking().
                loaderAllocator->SetupManagedTracking(&pManagedLoaderAllocator);
            }

            // Create a strong handle to the LoaderAllocator
            loaderAllocatorHandle = pCurDomain->CreateHandle(pManagedLoaderAllocator);

            GCPROTECT_END();

            loaderAllocator->ActivateManagedTracking();
        }

        IfFailThrow(CustomAssemblyBinder::SetupContext(pDefaultBinder, loaderAllocator, loaderAllocatorHandle, ptrManagedAssemblyLoadContext, &pCustomBinder));
        ptrNativeAssemblyBinder = reinterpret_cast<INT_PTR>(pCustomBinder);
    }
    else
    {
        // We are initializing the managed instance of Assembly Load Context that would represent the TPA binder.
        // First, confirm we do not have an existing managed ALC attached to the TPA binder.
        _ASSERTE(pDefaultBinder->GetManagedAssemblyLoadContext() == NULL);

        // Attach the managed TPA binding context with the native one.
        pDefaultBinder->SetManagedAssemblyLoadContext(ptrManagedAssemblyLoadContext);
        ptrNativeAssemblyBinder = reinterpret_cast<INT_PTR>(pDefaultBinder);
    }

    END_QCALL;

    return ptrNativeAssemblyBinder;
}

/*static*/
extern "C" void QCALLTYPE AssemblyNative_PrepareForAssemblyLoadContextRelease(INT_PTR ptrNativeAssemblyBinder, INT_PTR ptrManagedStrongAssemblyLoadContext)
{
    QCALL_CONTRACT;

    BOOL fDestroyed = FALSE;

    BEGIN_QCALL;


    {
        GCX_COOP();
        reinterpret_cast<CustomAssemblyBinder *>(ptrNativeAssemblyBinder)->PrepareForLoadContextRelease(ptrManagedStrongAssemblyLoadContext);
    }

    END_QCALL;
}

/*static*/
extern "C" INT_PTR QCALLTYPE AssemblyNative_GetLoadContextForAssembly(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    INT_PTR ptrManagedAssemblyLoadContext = NULL;

    BEGIN_QCALL;

    _ASSERTE(pAssembly != NULL);

    AssemblyBinder* pAssemblyBinder = pAssembly->GetPEAssembly()->GetAssemblyBinder();

    if (!pAssemblyBinder->IsDefault())
    {
        // Fetch the managed binder reference from the native binder instance
        ptrManagedAssemblyLoadContext = pAssemblyBinder->GetManagedAssemblyLoadContext();
        _ASSERTE(ptrManagedAssemblyLoadContext != NULL);
    }

    END_QCALL;

    return ptrManagedAssemblyLoadContext;
}

// static
extern "C" BOOL QCALLTYPE AssemblyNative_InternalTryGetRawMetadata(
    QCall::AssemblyHandle assembly,
    UINT8 **blobRef,
    INT32 *lengthRef)
{
    QCALL_CONTRACT;

    PTR_CVOID metadata = nullptr;

    BEGIN_QCALL;

    _ASSERTE(assembly != nullptr);
    _ASSERTE(blobRef != nullptr);
    _ASSERTE(lengthRef != nullptr);

    static_assert_no_msg(sizeof(*lengthRef) == sizeof(COUNT_T));
    metadata = assembly->GetPEAssembly()->GetLoadedMetadata(reinterpret_cast<COUNT_T *>(lengthRef));
    *blobRef = reinterpret_cast<UINT8 *>(const_cast<PTR_VOID>(metadata));
    _ASSERTE(*lengthRef >= 0);

    END_QCALL;

    return metadata != nullptr;
}

// static
FCIMPL0(FC_BOOL_RET, AssemblyNative::IsTracingEnabled)
{
    FCALL_CONTRACT;

    FC_RETURN_BOOL(BinderTracing::IsEnabled());
}
FCIMPLEND

// static
extern "C" void QCALLTYPE AssemblyNative_TraceResolvingHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR alcName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FireEtwAssemblyLoadContextResolvingHandlerInvoked(GetClrInstanceId(), assemblyName, handlerName, alcName, resultAssemblyName, resultAssemblyPath);

    END_QCALL;
}

// static
extern "C" void QCALLTYPE AssemblyNative_TraceAssemblyResolveHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FireEtwAppDomainAssemblyResolveHandlerInvoked(GetClrInstanceId(), assemblyName, handlerName, resultAssemblyName, resultAssemblyPath);

    END_QCALL;
}

// static
extern "C" void QCALLTYPE AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked(LPCWSTR assemblyName, bool isTrackedAssembly, LPCWSTR requestingAssemblyPath, LPCWSTR requestedAssemblyPath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FireEtwAssemblyLoadFromResolveHandlerInvoked(GetClrInstanceId(), assemblyName, isTrackedAssembly, requestingAssemblyPath, requestedAssemblyPath);

    END_QCALL;
}

// static
extern "C" void QCALLTYPE AssemblyNative_TraceSatelliteSubdirectoryPathProbed(LPCWSTR filePath, HRESULT hr)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    BinderTracing::PathProbed(filePath, BinderTracing::PathSource::SatelliteSubdirectory, hr);

    END_QCALL;
}

// static
extern "C" void QCALLTYPE AssemblyNative_ApplyUpdate(
    QCall::AssemblyHandle assembly,
    UINT8* metadataDelta,
    INT32 metadataDeltaLength,
    UINT8* ilDelta,
    INT32 ilDeltaLength,
    UINT8* pdbDelta,
    INT32 pdbDeltaLength)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    _ASSERTE(assembly != nullptr);
    _ASSERTE(metadataDelta != nullptr);
    _ASSERTE(metadataDeltaLength > 0);
    _ASSERTE(ilDelta != nullptr);
    _ASSERTE(ilDeltaLength > 0);

#ifdef EnC_SUPPORTED
    GCX_COOP();
    {
        if (CORDebuggerAttached())
        {
            COMPlusThrow(kNotSupportedException, W("NotSupported_DebuggerAttached"));
        }
        Module* pModule = assembly->GetModule();
        if (!pModule->IsEditAndContinueEnabled())
        {
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_AssemblyNotEditable"));
        }
        HRESULT hr = ((EditAndContinueModule*)pModule)->ApplyEditAndContinue(metadataDeltaLength, metadataDelta, ilDeltaLength, ilDelta);
        if (FAILED(hr))
        {
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_EditFailed"));
        }
        g_metadataUpdatesApplied = true;
    }
#else
    COMPlusThrow(kNotImplementedException);
#endif

    END_QCALL;
}

// static
extern "C" BOOL QCALLTYPE AssemblyNative_IsApplyUpdateSupported()
{
    QCALL_CONTRACT;

    BOOL result = false;

    BEGIN_QCALL;

#ifdef EnC_SUPPORTED
    result = CORDebuggerAttached() || g_pConfig->ForceEnc() || g_pConfig->DebugAssembliesModifiable();
#endif

    END_QCALL;

    return result;
}
