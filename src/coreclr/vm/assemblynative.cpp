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

#include <shlwapi.h>
#include <stdlib.h>
#include "assemblynative.hpp"
#include "dllimport.h"
#include "field.h"
#include "assemblyname.hpp"
#include "eeconfig.h"
#include "interoputil.h"
#include "frames.h"
#include "typeparse.h"
#include "encee.h"
#include "threadsuspend.h"

#include "appdomainnative.hpp"
#include "../binder/inc/bindertracing.h"
#include "../binder/inc/clrprivbindercoreclr.h"

/* static */
void QCALLTYPE AssemblyNative::InternalLoad(QCall::ObjectHandleOnStack assemblyName,
                                            QCall::ObjectHandleOnStack requestingAssembly,
                                            QCall::StackCrawlMarkHandle stackMark,
                                            BOOL fThrowOnFileNotFound,
                                            QCall::ObjectHandleOnStack assemblyLoadContext,
                                            QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    if (assemblyName.Get() == NULL)
    {
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_AssemblyName"));
    }
    ACQUIRE_STACKING_ALLOCATOR(pStackingAllocator);

    DomainAssembly * pParentAssembly = NULL;
    Assembly * pRefAssembly = NULL;
    ICLRPrivBinder *pBinderContext = NULL;

    if (assemblyLoadContext.Get() != NULL)
    {
        INT_PTR nativeAssemblyLoadContext = ((ASSEMBLYLOADCONTEXTREF)assemblyLoadContext.Get())->GetNativeAssemblyLoadContext();
        pBinderContext = reinterpret_cast<ICLRPrivBinder*>(nativeAssemblyLoadContext);
    }

    AssemblySpec spec;
    ASSEMBLYNAMEREF assemblyNameRef = NULL;

    GCPROTECT_BEGIN(assemblyNameRef);
    assemblyNameRef = (ASSEMBLYNAMEREF)assemblyName.Get();
    if (assemblyNameRef->GetSimpleName() == NULL)
    {
        COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));
    }

    // Compute parent assembly
    if (requestingAssembly.Get() != NULL)
    {
        pRefAssembly = ((ASSEMBLYREF)requestingAssembly.Get())->GetAssembly();
    }
    else if (pBinderContext == NULL)
    {
        pRefAssembly = SystemDomain::GetCallersAssembly(stackMark);
    }
    if (pRefAssembly)
    {
        pParentAssembly = pRefAssembly->GetDomainAssembly();
    }

    // Initialize spec
    spec.InitializeSpec(pStackingAllocator,
                        &assemblyNameRef,
                        FALSE);
    GCPROTECT_END();

    spec.SetCodeBase(NULL);

    if (pParentAssembly != NULL)
        spec.SetParentAssembly(pParentAssembly);

    // Have we been passed the reference to the binder against which this load should be triggered?
    // If so, then use it to set the fallback load context binder.
    if (pBinderContext != NULL)
    {
        spec.SetFallbackLoadContextBinderForRequestingAssembly(pBinderContext);
        spec.SetPreferFallbackLoadContextBinder();
    }
    else if (pRefAssembly != NULL)
    {
        // If the requesting assembly has Fallback LoadContext binder available,
        // then set it up in the AssemblySpec.
        PEFile *pRefAssemblyManifestFile = pRefAssembly->GetManifestFile();
        spec.SetFallbackLoadContextBinderForRequestingAssembly(pRefAssemblyManifestFile->GetFallbackLoadContextBinder());
    }

    Assembly *pAssembly;
    {
        GCX_PREEMP();
        pAssembly = spec.LoadAssembly(FILE_LOADED, fThrowOnFileNotFound);
    }

    if (pAssembly != NULL)
    {
        retAssembly.Set(pAssembly->GetExposedObject());
    }

    END_QCALL;
}

/* static */
Assembly* AssemblyNative::LoadFromPEImage(ICLRPrivBinder* pBinderContext, PEImage *pILImage, PEImage *pNIImage)
{
    CONTRACT(Assembly*)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pBinderContext));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    Assembly *pLoadedAssembly = NULL;

    ReleaseHolder<ICLRPrivAssembly> pAssembly;

    // Get the correct PEImage to work with.
    BOOL fIsNativeImage = TRUE;
    PEImage *pImage = pNIImage;
    if (pNIImage == NULL)
    {
        // Since we do not have a NI image, we are working with IL assembly
        pImage = pILImage;
        fIsNativeImage = FALSE;
    }
    _ASSERTE(pImage != NULL);

    BOOL fHadLoadFailure = FALSE;

    // Force the image to be loaded and mapped so that subsequent loads do not
    // map a duplicate copy.
    if (pImage->IsFile())
    {
        pImage->Load();
    }
    else
    {
        pImage->LoadNoFile();
    }

    DWORD dwMessageID = IDS_EE_FILELOAD_ERROR_GENERIC;

    // Set the caller's assembly to be CoreLib
    DomainAssembly *pCallersAssembly = SystemDomain::System()->SystemAssembly()->GetDomainAssembly();
    PEAssembly *pParentAssembly = pCallersAssembly->GetFile();

    // Initialize the AssemblySpec
    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(1, mdtAssembly), pImage->GetMDImport(), pCallersAssembly);
    spec.SetBindingContext(pBinderContext);

    BinderTracing::AssemblyBindOperation bindOperation(&spec, pImage->GetPath());

    HRESULT hr = S_OK;
    PTR_AppDomain pCurDomain = GetAppDomain();
    CLRPrivBinderCoreCLR *pTPABinder = pCurDomain->GetTPABinderContext();
    if (!AreSameBinderInstance(pTPABinder, pBinderContext))
    {
        // We are working with custom Assembly Load Context so bind the assembly using it.
        CLRPrivBinderAssemblyLoadContext *pBinder = reinterpret_cast<CLRPrivBinderAssemblyLoadContext *>(pBinderContext);
        hr = pBinder->BindUsingPEImage(pImage, fIsNativeImage, &pAssembly);
    }
    else
    {
        // Bind the assembly using TPA binder
        hr = pTPABinder->BindUsingPEImage(pImage, fIsNativeImage, &pAssembly);
    }

    if (hr != S_OK)
    {
        // Give a more specific message for the case when we found the assembly with the same name already loaded.
        if (hr == COR_E_FILELOAD)
        {
            dwMessageID = IDS_HOST_ASSEMBLY_RESOLVER_ASSEMBLY_ALREADY_LOADED_IN_CONTEXT;
        }

        StackSString name;
        spec.GetFileOrDisplayName(0, name);
        COMPlusThrowHR(COR_E_FILELOAD, dwMessageID, name);
    }

    BINDER_SPACE::Assembly* assem;
    assem = BINDER_SPACE::GetAssemblyFromPrivAssemblyFast(pAssembly);

    PEAssemblyHolder pPEAssembly(PEAssembly::Open(pParentAssembly, assem->GetPEImage(), assem->GetNativePEImage(), pAssembly));
    bindOperation.SetResult(pPEAssembly.GetValue());

    DomainAssembly *pDomainAssembly = pCurDomain->LoadDomainAssembly(&spec, pPEAssembly, FILE_LOADED);
    RETURN pDomainAssembly->GetAssembly();
}

/* static */
void QCALLTYPE AssemblyNative::LoadFromPath(INT_PTR ptrNativeAssemblyLoadContext, LPCWSTR pwzILPath, LPCWSTR pwzNIPath, QCall::ObjectHandleOnStack retLoadedAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    PTR_AppDomain pCurDomain = GetAppDomain();

    // Get the binder context in which the assembly will be loaded.
    ICLRPrivBinder *pBinderContext = reinterpret_cast<ICLRPrivBinder*>(ptrNativeAssemblyLoadContext);
    _ASSERTE(pBinderContext != NULL);

    // Form the PEImage for the ILAssembly. Incase of an exception, the holders will ensure
    // the release of the image.
    PEImageHolder pILImage, pNIImage;

    if (pwzILPath != NULL)
    {
        pILImage = PEImage::OpenImage(pwzILPath,
                                      MDInternalImport_Default,
                                      BundleFileLocation::Invalid());

        // Need to verify that this is a valid CLR assembly.
        if (!pILImage->CheckILFormat())
            THROW_BAD_FORMAT(BFA_BAD_IL, pILImage.GetValue());

        LoaderAllocator* pLoaderAllocator = NULL;
        if (SUCCEEDED(pBinderContext->GetLoaderAllocator((LPVOID*)&pLoaderAllocator)) && pLoaderAllocator->IsCollectible() && !pILImage->IsILOnly())
        {
            // Loading IJW assemblies into a collectible AssemblyLoadContext is not allowed
            THROW_BAD_FORMAT(BFA_IJW_IN_COLLECTIBLE_ALC, pILImage.GetValue());
        }
    }

#ifdef FEATURE_PREJIT
    // Form the PEImage for the NI assembly, if specified
    if (pwzNIPath != NULL)
    {
        pNIImage = PEImage::OpenImage(pwzNIPath,
                                      MDInternalImport_TrustedNativeImage,
                                      BundleFileLocation::Invalid());

        if (pNIImage->HasReadyToRunHeader())
        {
            // ReadyToRun images are treated as IL images by the rest of the system
            if (!pNIImage->CheckILFormat())
                THROW_BAD_FORMAT(COR_E_BADIMAGEFORMAT, pNIImage.GetValue());

            pILImage = pNIImage.Extract();
            pNIImage = NULL;
        }
        else
        {
            if (!pNIImage->CheckNativeFormat())
                THROW_BAD_FORMAT(COR_E_BADIMAGEFORMAT, pNIImage.GetValue());
        }
    }
#endif // FEATURE_PREJIT

    Assembly *pLoadedAssembly = AssemblyNative::LoadFromPEImage(pBinderContext, pILImage, pNIImage);

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
void QCALLTYPE AssemblyNative::LoadFromStream(INT_PTR ptrNativeAssemblyLoadContext, INT_PTR ptrAssemblyArray,
                                              INT32 cbAssemblyArrayLength, INT_PTR ptrSymbolArray, INT32 cbSymbolArrayLength,
                                              QCall::ObjectHandleOnStack retLoadedAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Ensure that the invariants are in place
    _ASSERTE(ptrNativeAssemblyLoadContext != NULL);
    _ASSERTE((ptrAssemblyArray != NULL) && (cbAssemblyArrayLength > 0));
    _ASSERTE((ptrSymbolArray == NULL) || (cbSymbolArrayLength > 0));

    // We must have a flat image stashed away since we need a private
    // copy of the data which we can verify before doing the mapping.
    PVOID pAssemblyArray = reinterpret_cast<PVOID>(ptrAssemblyArray);

    PEImageHolder pILImage(PEImage::LoadFlat(pAssemblyArray, (COUNT_T)cbAssemblyArrayLength));

    // Need to verify that this is a valid CLR assembly.
    if (!pILImage->CheckILFormat())
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);

    // Get the binder context in which the assembly will be loaded
    ICLRPrivBinder *pBinderContext = reinterpret_cast<ICLRPrivBinder*>(ptrNativeAssemblyLoadContext);

    LoaderAllocator* pLoaderAllocator = NULL;
    if (SUCCEEDED(pBinderContext->GetLoaderAllocator((LPVOID*)&pLoaderAllocator)) && pLoaderAllocator->IsCollectible() && !pILImage->IsILOnly())
    {
        // Loading IJW assemblies into a collectible AssemblyLoadContext is not allowed
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_IJW_IN_COLLECTIBLE_ALC);
    }

    // Pass the stream based assembly as IL and NI in an attempt to bind and load it
    Assembly* pLoadedAssembly = AssemblyNative::LoadFromPEImage(pBinderContext, pILImage, NULL);
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
    BOOL fIsSameAssembly = (pLoadedAssembly->GetManifestFile()->GetILimage() == pILImage);

    // Setting the PDB info is only applicable for our original assembly.
    // This applies to both Desktop CLR and CoreCLR, with or without fusion.
    if (fIsSameAssembly)
    {
#ifdef DEBUGGING_SUPPORTED
        // If we were given symbols, save a copy of them.
        if (ptrSymbolArray != NULL)
        {
            PBYTE pSymbolArray = reinterpret_cast<PBYTE>(ptrSymbolArray);
            pLoadedAssembly->GetManifestModule()->SetSymbolBytes(pSymbolArray, (DWORD)cbSymbolArrayLength);
        }
#endif // DEBUGGING_SUPPORTED
    }

    END_QCALL;
}

#ifndef TARGET_UNIX
/*static */
void QCALLTYPE AssemblyNative::LoadFromInMemoryModule(INT_PTR ptrNativeAssemblyLoadContext, INT_PTR hModule, QCall::ObjectHandleOnStack retLoadedAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Ensure that the invariants are in place
    _ASSERTE(ptrNativeAssemblyLoadContext != NULL);
    _ASSERTE(hModule != NULL);

    PEImageHolder pILImage(PEImage::LoadImage((HMODULE)hModule));

    // Need to verify that this is a valid CLR assembly.
    if (!pILImage->HasCorHeader())
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);

    // Get the binder context in which the assembly will be loaded
    ICLRPrivBinder *pBinderContext = reinterpret_cast<ICLRPrivBinder*>(ptrNativeAssemblyLoadContext);

    // Pass the in memory module as IL in an attempt to bind and load it
    Assembly* pLoadedAssembly = AssemblyNative::LoadFromPEImage(pBinderContext, pILImage, NULL);
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

void QCALLTYPE AssemblyNative::GetLocation(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    {
        retString.Set(pAssembly->GetFile()->GetPath());
    }

    END_QCALL;
}

void QCALLTYPE AssemblyNative::GetType(QCall::AssemblyHandle pAssembly,
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

    if (!wszName)
        COMPlusThrowArgumentNull(W("name"), W("ArgumentNull_String"));

    BOOL prohibitAsmQualifiedName = TRUE;

    ICLRPrivBinder * pPrivHostBinder = NULL;

    if (*pAssemblyLoadContext.m_ppObject != NULL)
    {
        GCX_COOP();
        ASSEMBLYLOADCONTEXTREF * pAssemblyLoadContextRef = reinterpret_cast<ASSEMBLYLOADCONTEXTREF *>(pAssemblyLoadContext.m_ppObject);

        INT_PTR nativeAssemblyLoadContext = (*pAssemblyLoadContextRef)->GetNativeAssemblyLoadContext();

        pPrivHostBinder = reinterpret_cast<ICLRPrivBinder *>(nativeAssemblyLoadContext);
    }

    // Load the class from this assembly (fail if it is in a different one).
    retTypeHandle = TypeName::GetTypeManaged(wszName, pAssembly, bThrowOnError, bIgnoreCase, prohibitAsmQualifiedName, pAssembly->GetAssembly(), (OBJECTREF*)keepAlive.m_ppObject, pPrivHostBinder);

    if (!retTypeHandle.IsNull())
    {
         GCX_COOP();
         retType.Set(retTypeHandle.GetManagedClassObject());
    }

    END_QCALL;

    return;
}

void QCALLTYPE AssemblyNative::GetForwardedType(QCall::AssemblyHandle pAssembly, mdToken mdtExternalType, QCall::ObjectHandleOnStack retType)
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
    Module *pManifestModule = pAsm->GetManifestModule();
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

    FC_RETURN_BOOL(refAssembly->GetDomainAssembly()->GetFile()->IsDynamic());
}
FCIMPLEND

void QCALLTYPE AssemblyNative::GetVersion(QCall::AssemblyHandle pAssembly, INT32* pMajorVersion, INT32* pMinorVersion, INT32*pBuildNumber, INT32* pRevisionNumber)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    UINT16 major=0xffff, minor=0xffff, build=0xffff, revision=0xffff;

    pAssembly->GetFile()->GetVersion(&major, &minor, &build, &revision);

    *pMajorVersion = major;
    *pMinorVersion = minor;
    *pBuildNumber = build;
    *pRevisionNumber = revision;

    END_QCALL;
}

void QCALLTYPE AssemblyNative::GetPublicKey(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retPublicKey)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    DWORD cbPublicKey = 0;
    const void *pbPublicKey = pAssembly->GetFile()->GetPublicKey(&cbPublicKey);
    retPublicKey.SetByteArray((BYTE *)pbPublicKey, cbPublicKey);

    END_QCALL;
}

void QCALLTYPE AssemblyNative::GetSimpleName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retSimpleName)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    retSimpleName.Set(pAssembly->GetSimpleName());
    END_QCALL;
}

void QCALLTYPE AssemblyNative::GetLocale(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    LPCUTF8 pLocale = pAssembly->GetFile()->GetLocale();
    if(pLocale)
    {
        retString.Set(pLocale);
    }

    END_QCALL;
}

BOOL QCALLTYPE AssemblyNative::GetCodeBase(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BOOL ret = TRUE;

    BEGIN_QCALL;

    StackSString codebase;

    {
        ret = pAssembly->GetFile()->GetCodeBase(codebase);
    }

    retString.Set(codebase);
    END_QCALL;

    return ret;
}

INT32 QCALLTYPE AssemblyNative::GetHashAlgorithm(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    INT32 retVal=0;
    BEGIN_QCALL;
    retVal = pAssembly->GetFile()->GetHashAlgId();
    END_QCALL;
    return retVal;
}

INT32 QCALLTYPE AssemblyNative::GetFlags(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    INT32 retVal=0;
    BEGIN_QCALL;
    retVal = pAssembly->GetFile()->GetFlags();
    END_QCALL;
    return retVal;
}

BYTE * QCALLTYPE AssemblyNative::GetResource(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, DWORD * length)
{
    QCALL_CONTRACT;

    PBYTE       pbInMemoryResource  = NULL;

    BEGIN_QCALL;

    if (wszName == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    // Get the name in UTF8
    SString name(SString::Literal, wszName);

    StackScratchBuffer scratch;
    LPCUTF8 pNameUTF8 = name.GetUTF8(scratch);

    if (*pNameUTF8 == '\0')
        COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));

    pAssembly->GetResource(pNameUTF8, length,
                           &pbInMemoryResource, NULL, NULL,
                           NULL, FALSE);

    END_QCALL;

    // Can return null if resource file is zero-length
    return pbInMemoryResource;
}

INT32 QCALLTYPE AssemblyNative::GetManifestResourceInfo(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, QCall::ObjectHandleOnStack retAssembly, QCall::StringHandleOnStack retFileName)
{
    QCALL_CONTRACT;

    INT32 rv = -1;

    BEGIN_QCALL;

    if (wszName == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));

    // Get the name in UTF8
    SString name(SString::Literal, wszName);

    StackScratchBuffer scratch;
    LPCUTF8 pNameUTF8 = name.GetUTF8(scratch);

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

void QCALLTYPE AssemblyNative::GetModules(QCall::AssemblyHandle pAssembly, BOOL fLoadIfNotFound, BOOL fGetResourceModules, QCall::ObjectHandleOnStack retModules)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    HENUMInternalHolder phEnum(pAssembly->GetMDImport());
    phEnum.EnumInit(mdtFile, mdTokenNil);

    InlineSArray<DomainFile *, 8> modules;

    modules.Append(pAssembly);

    mdFile mdFile;
    while (pAssembly->GetMDImport()->EnumNext(&phEnum, &mdFile))
    {
        DomainFile *pModule = pAssembly->GetModule()->LoadModule(GetAppDomain(), mdFile, fGetResourceModules, !fLoadIfNotFound);

        if (pModule) {
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
            DomainFile * pModule = modules[i];

            OBJECTREF o = pModule->GetExposedModuleObject();
            orModules->SetAt(i, o);
        }

        retModules.Set(orModules);

        GCPROTECT_END();
    }

    END_QCALL;
}

BOOL QCALLTYPE AssemblyNative::GetIsCollectible(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

    BEGIN_QCALL;

    retVal = pAssembly->IsCollectible();

    END_QCALL;

    return retVal;
}

extern volatile uint32_t g_cAssemblies;

FCIMPL0(uint32_t, AssemblyNative::GetAssemblyCount)
{
    FCALL_CONTRACT;

    return g_cAssemblies;
}
FCIMPLEND

void QCALLTYPE AssemblyNative::GetModule(QCall::AssemblyHandle pAssembly, LPCWSTR wszFileName, QCall::ObjectHandleOnStack retModule)
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

    if SUCCEEDED(pAssembly->GetDomainAssembly()->GetModule()->GetScopeName(&pModuleName))
    {
        if (::SString::_stricmp(pModuleName, szModuleName) == 0)
            pModule = pAssembly->GetDomainAssembly()->GetModule();
    }


    if (pModule != NULL)
    {
        GCX_COOP();
        retModule.Set(pModule->GetExposedObject());
    }

    END_QCALL;

    return;
}

void QCALLTYPE AssemblyNative::GetExportedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    InlineSArray<TypeHandle, 20> types;

    Assembly * pAsm = pAssembly->GetAssembly();

    IMDInternalImport *pImport = pAsm->GetManifestImport();

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
                TypeHandle typeHnd = ClassLoader::LoadTypeDefThrowing(pAsm->GetManifestModule(), mdTD,
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
                typeName.SetTypeToken(pAsm->GetManifestModule(), mdCT);
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

void QCALLTYPE AssemblyNative::GetForwardedTypes(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retTypes)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    InlineSArray<TypeHandle, 8> types;

    Assembly * pAsm = pAssembly->GetAssembly();

    IMDInternalImport *pImport = pAsm->GetManifestImport();

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
                typeName.SetTypeToken(pAsm->GetManifestModule(), mdCT);
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

    struct _gc {
        PTRARRAYREF ItemArray;
        ASSEMBLYNAMEREF pObj;
        ASSEMBLYREF refAssembly;
    } gc;
    ZeroMemory(&gc, sizeof(gc));

    gc.refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);

    if (gc.refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pAssembly = gc.refAssembly->GetDomainAssembly();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    IMDInternalImport *pImport = pAssembly->GetAssembly()->GetManifestImport();

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
        spec.AssemblyNameInit(&gc.pObj,NULL);

        gc.ItemArray->SetAt(i, (OBJECTREF) gc.pObj);
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.ItemArray);
}
FCIMPLEND

void QCALLTYPE AssemblyNative::GetEntryPoint(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retMethod)
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

void QCALLTYPE AssemblyNative::GetFullName(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    StackSString name;
    pAssembly->GetFile()->GetDisplayName(name);
    retString.Set(name);

    END_QCALL;
}

void QCALLTYPE AssemblyNative::GetExecutingAssembly(QCall::StackCrawlMarkHandle stackMark, QCall::ObjectHandleOnStack retAssembly)
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

void QCALLTYPE AssemblyNative::GetEntryAssembly(QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    DomainAssembly * pRootAssembly = NULL;
    Assembly * pAssembly = GetAppDomain()->m_pRootAssembly;

    if (pAssembly)
    {
        pRootAssembly = pAssembly->GetDomainAssembly();
        GCX_COOP();
        retAssembly.Set(pRootAssembly->GetExposedAssemblyObject());
    }

    END_QCALL;

    return;
}

// return the in memory assembly module for reflection emit. This only works for dynamic assembly.
FCIMPL1(ReflectModuleBaseObject *, AssemblyNative::GetInMemoryAssemblyModule, AssemblyBaseObject* pAssemblyUNSAFE)
{
    FCALL_CONTRACT;


    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);

    if (refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pAssembly = refAssembly->GetDomainAssembly();

    FC_RETURN_MODULE_OBJECT(pAssembly->GetCurrentModule(), refAssembly);
}
FCIMPLEND

void QCALLTYPE AssemblyNative::GetImageRuntimeVersion(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // Retrieve the PEFile from the assembly.
    PEFile* pPEFile = pAssembly->GetFile();
    PREFIX_ASSUME(pPEFile!=NULL);

    LPCSTR pszVersion = NULL;
    IfFailThrow(pPEFile->GetMDImport()->GetVersionString(&pszVersion));

    SString version(SString::Utf8, pszVersion);

    // Allocate a managed string that contains the version and return it.
    retString.Set(version);

    END_QCALL;
}

/*static*/

INT_PTR QCALLTYPE AssemblyNative::InitializeAssemblyLoadContext(INT_PTR ptrManagedAssemblyLoadContext, BOOL fRepresentsTPALoadContext, BOOL fIsCollectible)
{
    QCALL_CONTRACT;

    INT_PTR ptrNativeAssemblyLoadContext = NULL;

    BEGIN_QCALL;

    // We do not need to take a lock since this method is invoked from the ctor of AssemblyLoadContext managed type and
    // only one thread is ever executing a ctor for a given instance.
    //

    // Initialize the assembly binder instance in the VM
    PTR_AppDomain pCurDomain = AppDomain::GetCurrentDomain();
    CLRPrivBinderCoreCLR *pTPABinderContext = pCurDomain->GetTPABinderContext();
    if (!fRepresentsTPALoadContext)
    {
        // Initialize a custom Assembly Load Context
        CLRPrivBinderAssemblyLoadContext *pBindContext = NULL;

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
                // atomically transfered by call to LoaderAllocator::ActivateManagedTracking().
                loaderAllocator->SetupManagedTracking(&pManagedLoaderAllocator);
            }

            // Create a strong handle to the LoaderAllocator
            loaderAllocatorHandle = pCurDomain->CreateHandle(pManagedLoaderAllocator);

            GCPROTECT_END();

            loaderAllocator->ActivateManagedTracking();
        }

        IfFailThrow(CLRPrivBinderAssemblyLoadContext::SetupContext(DefaultADID, pTPABinderContext, loaderAllocator, loaderAllocatorHandle, ptrManagedAssemblyLoadContext, &pBindContext));
        ptrNativeAssemblyLoadContext = reinterpret_cast<INT_PTR>(pBindContext);
    }
    else
    {
        // We are initializing the managed instance of Assembly Load Context that would represent the TPA binder.
        // First, confirm we do not have an existing managed ALC attached to the TPA binder.
        _ASSERTE(pTPABinderContext->GetManagedAssemblyLoadContext() == NULL);

        // Attach the managed TPA binding context with the native one.
        pTPABinderContext->SetManagedAssemblyLoadContext(ptrManagedAssemblyLoadContext);
        ptrNativeAssemblyLoadContext = reinterpret_cast<INT_PTR>(pTPABinderContext);
    }

    END_QCALL;

    return ptrNativeAssemblyLoadContext;
}

/*static*/
void QCALLTYPE AssemblyNative::PrepareForAssemblyLoadContextRelease(INT_PTR ptrNativeAssemblyLoadContext, INT_PTR ptrManagedStrongAssemblyLoadContext)
{
    QCALL_CONTRACT;

    BOOL fDestroyed = FALSE;

    BEGIN_QCALL;


    {
        GCX_COOP();
        reinterpret_cast<CLRPrivBinderAssemblyLoadContext *>(ptrNativeAssemblyLoadContext)->PrepareForLoadContextRelease(ptrManagedStrongAssemblyLoadContext);
    }

    END_QCALL;
}

/*static*/
INT_PTR QCALLTYPE AssemblyNative::GetLoadContextForAssembly(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    INT_PTR ptrManagedAssemblyLoadContext = NULL;

    BEGIN_QCALL;

    _ASSERTE(pAssembly != NULL);

    AssemblyLoadContext* pAssemblyLoadContext = pAssembly->GetFile()->GetAssemblyLoadContext();

    if (pAssemblyLoadContext != AppDomain::GetCurrentDomain()->GetTPABinderContext())
    {
        // Only CLRPrivBinderAssemblyLoadContext instance contains the reference to its
        // corresponding managed instance.
        CLRPrivBinderAssemblyLoadContext* pBinder = (CLRPrivBinderAssemblyLoadContext*)(pAssemblyLoadContext);

        // Fetch the managed binder reference from the native binder instance
        ptrManagedAssemblyLoadContext = pBinder->GetManagedAssemblyLoadContext();
        _ASSERTE(ptrManagedAssemblyLoadContext != NULL);
    }

    END_QCALL;

    return ptrManagedAssemblyLoadContext;
}

// static
BOOL QCALLTYPE AssemblyNative::InternalTryGetRawMetadata(
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
    metadata = assembly->GetFile()->GetLoadedMetadata(reinterpret_cast<COUNT_T *>(lengthRef));
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
void QCALLTYPE AssemblyNative::TraceResolvingHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR alcName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FireEtwAssemblyLoadContextResolvingHandlerInvoked(GetClrInstanceId(), assemblyName, handlerName, alcName, resultAssemblyName, resultAssemblyPath);

    END_QCALL;
}

// static
void QCALLTYPE AssemblyNative::TraceAssemblyResolveHandlerInvoked(LPCWSTR assemblyName, LPCWSTR handlerName, LPCWSTR resultAssemblyName, LPCWSTR resultAssemblyPath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FireEtwAppDomainAssemblyResolveHandlerInvoked(GetClrInstanceId(), assemblyName, handlerName, resultAssemblyName, resultAssemblyPath);

    END_QCALL;
}

// static
void QCALLTYPE AssemblyNative::TraceAssemblyLoadFromResolveHandlerInvoked(LPCWSTR assemblyName, bool isTrackedAssembly, LPCWSTR requestingAssemblyPath, LPCWSTR requestedAssemblyPath)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    FireEtwAssemblyLoadFromResolveHandlerInvoked(GetClrInstanceId(), assemblyName, isTrackedAssembly, requestingAssemblyPath, requestedAssemblyPath);

    END_QCALL;
}

// static
void QCALLTYPE AssemblyNative::TraceSatelliteSubdirectoryPathProbed(LPCWSTR filePath, HRESULT hr)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    BinderTracing::PathProbed(filePath, BinderTracing::PathSource::SatelliteSubdirectory, hr);

    END_QCALL;
}

// static
void QCALLTYPE AssemblyNative::ApplyUpdate(
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
        Module* pModule = assembly->GetDomainAssembly()->GetModule();
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
