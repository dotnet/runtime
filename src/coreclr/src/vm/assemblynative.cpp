// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "security.h"
#include "strongname.h"
#include "interoputil.h"
#include "frames.h"
#include "typeparse.h"
#include "stackprobe.h"

#include "appdomainnative.hpp"
#include "../binder/inc/clrprivbindercoreclr.h"



FCIMPL10(Object*, AssemblyNative::Load, AssemblyNameBaseObject* assemblyNameUNSAFE, 
        StringObject* codeBaseUNSAFE, 
        Object* securityUNSAFE, 
        AssemblyBaseObject* requestingAssemblyUNSAFE,
        StackCrawlMark* stackMark,
        ICLRPrivBinder * pPrivHostBinder,
        CLR_BOOL fThrowOnFileNotFound,
        CLR_BOOL fForIntrospection,
        CLR_BOOL fSuppressSecurityChecks,
        INT_PTR ptrLoadContextBinder)
{
    FCALL_CONTRACT;

    struct _gc
    {
        ASSEMBLYNAMEREF assemblyName;
        STRINGREF       codeBase;
        ASSEMBLYREF     requestingAssembly; 
        OBJECTREF       security;
        ASSEMBLYREF     rv;
    } gc;

    gc.assemblyName    = (ASSEMBLYNAMEREF) assemblyNameUNSAFE;
    gc.codeBase        = (STRINGREF)       codeBaseUNSAFE;
    gc.requestingAssembly    = (ASSEMBLYREF)     requestingAssemblyUNSAFE;
    gc.security        = (OBJECTREF)       securityUNSAFE;
    gc.rv              = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    if (gc.assemblyName == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_AssemblyName"));

    if (fForIntrospection)
    {
        if (!GetThread()->GetDomain()->IsVerificationDomain())
            GetThread()->GetDomain()->SetIllegalVerificationDomain();
    }

    Thread * pThread = GetThread();
    CheckPointHolder cph(pThread->m_MarshalAlloc.GetCheckpoint()); //hold checkpoint for autorelease

    DomainAssembly * pParentAssembly = NULL;
    Assembly * pRefAssembly = NULL;

    if(gc.assemblyName->GetSimpleName() == NULL)
    {
        if (gc.codeBase == NULL)
            COMPlusThrow(kArgumentException, W("Format_StringZeroLength"));
        if ((!fForIntrospection) && CorHost2::IsLoadFromBlocked())
            COMPlusThrow(kFileLoadException, FUSION_E_LOADFROM_BLOCKED);
    }
    else if (!fForIntrospection)
    {
        // name specified, if immersive ignore the codebase
        if (GetThread()->GetDomain()->HasLoadContextHostBinder())
            gc.codeBase = NULL;

        // Compute parent assembly
        if (gc.requestingAssembly == NULL)
        {
            pRefAssembly = SystemDomain::GetCallersAssembly(stackMark);
        
            // Cross-appdomain callers aren't allowed as the parent
            if (pRefAssembly &&
                (pRefAssembly->GetDomain() != pThread->GetDomain()))
            {
                pRefAssembly = NULL;
            }
        }
        else
            pRefAssembly = gc.requestingAssembly->GetAssembly();
        
        // Shared or collectible assemblies should not be used for the parent in the
        // late-bound case.
        if (pRefAssembly && (!pRefAssembly->IsDomainNeutral()) && (!pRefAssembly->IsCollectible()))
        {
            pParentAssembly= pRefAssembly->GetDomainAssembly();
        }

    }

    // Initialize spec
    AssemblySpec spec;
    spec.InitializeSpec(&(pThread->m_MarshalAlloc), 
                        &gc.assemblyName,
                        FALSE,
                        fForIntrospection);
    
    if (!spec.HasUniqueIdentity())
    {   // Insuficient assembly name for binding (e.g. ContentType=WindowsRuntime cannot bind by assembly name)
        EEFileLoadException::Throw(&spec, COR_E_NOTSUPPORTED);
    }
    
    if (pPrivHostBinder != NULL)
    {
        pParentAssembly = NULL;
        spec.SetHostBinder(pPrivHostBinder);
    }
    
    if (gc.codeBase != NULL)
        spec.SetCodeBase(&(pThread->m_MarshalAlloc), &gc.codeBase);

    if (pParentAssembly != NULL)
        spec.SetParentAssembly(pParentAssembly);

    // Have we been passed the reference to the binder against which this load should be triggered?
    // If so, then use it to set the fallback load context binder.
    if (ptrLoadContextBinder != NULL)
    {
        spec.SetFallbackLoadContextBinderForRequestingAssembly(reinterpret_cast<ICLRPrivBinder *>(ptrLoadContextBinder));
        spec.SetPreferFallbackLoadContextBinder();
    }
    else if (pRefAssembly != NULL)
    {
        // If the requesting assembly has Fallback LoadContext binder available,
        // then set it up in the AssemblySpec.
        PEFile *pRefAssemblyManifestFile = pRefAssembly->GetManifestFile();
        spec.SetFallbackLoadContextBinderForRequestingAssembly(pRefAssemblyManifestFile->GetFallbackLoadContextBinder());
    }

    AssemblyLoadSecurity loadSecurity;
    loadSecurity.m_pAdditionalEvidence = &gc.security;
    loadSecurity.m_fCheckLoadFromRemoteSource = !!(gc.codeBase != NULL);
    loadSecurity.m_fSuppressSecurityChecks = !!fSuppressSecurityChecks;

    // If we're in an APPX domain, then all loads from the application will find themselves within the APPX package
    // graph or from a trusted location.   However, assemblies within the package may have been marked by Windows as
    // not being from the MyComputer zone, which can trip the LoadFromRemoteSources check.  Since we do not need to
    // defend against accidental loads from HTTP for APPX applications, we simply suppress the remote load check.
    if (AppX::IsAppXProcess())
    {
        loadSecurity.m_fCheckLoadFromRemoteSource = false;
    }

    Assembly *pAssembly;
    
    {
        GCX_PREEMP();
        pAssembly = spec.LoadAssembly(FILE_LOADED, &loadSecurity, fThrowOnFileNotFound, FALSE /*fRaisePrebindEvents*/, stackMark);
    }

    if (pAssembly != NULL)
        gc.rv = (ASSEMBLYREF) pAssembly->GetExposedObject();
    
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.rv);
}
FCIMPLEND

Assembly* AssemblyNative::LoadFromBuffer(BOOL fForIntrospection, const BYTE* pAssemblyData,  UINT64 uAssemblyLength, const BYTE* pPDBData,  UINT64 uPDBLength, StackCrawlMark* stackMark, Object * securityUNSAFE, SecurityContextSource securityContextSource)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Assembly *pAssembly;
    
    struct _gc {
        OBJECTREF orefSecurity;
        OBJECTREF granted;
        OBJECTREF denied;
    } gc;
    
    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);
    
    gc.orefSecurity = (OBJECTREF) securityUNSAFE;

    if((!fForIntrospection) && CorHost2::IsLoadFromBlocked())
        COMPlusThrow(kFileLoadException, FUSION_E_LOADFROM_BLOCKED);

    if (pAssemblyData == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_Array"));

    if (fForIntrospection) {
        if (!GetThread()->GetDomain()->IsVerificationDomain())
            GetThread()->GetDomain()->SetIllegalVerificationDomain();
    }

    // Get caller's assembly so we can extract their codebase and propagate it
    // into the new assembly (which obviously doesn't have one of its own).

    AppDomain *pCallersDomain = NULL;
    MethodDesc* pCallerMD = SystemDomain::GetCallersMethod (stackMark, &pCallersDomain);
    Assembly *pCallersAssembly = (pCallerMD ? pCallerMD->GetAssembly() : NULL);
    BOOL fPropagateIdentity = ((!fForIntrospection) && (gc.orefSecurity == NULL));

    // Callers assembly can be null if caller is interop
    // @todo: we really don't want to call this assembly "mscorlib" to anyone who asks
    // for its code base.  But the required effect here is that it recieves full trust
    // as far as its codebase goes so this should be OK.  We really need to allow a 
    // "no code base" condition to avoid confusion
    if (pCallersAssembly == NULL) {
        pCallersAssembly = SystemDomain::System()->SystemAssembly();
    } else {
    }

    if ((COUNT_T)uAssemblyLength !=uAssemblyLength)  // overflow
        ThrowOutOfMemory();

    PEAssemblyHolder pFile;
    
    {
        GCX_PREEMP();

        CLRPrivBinderLoadFile* pBinderToUse = NULL;

        pFile = PEAssembly::OpenMemory(pCallersAssembly->GetManifestFile(),
                                                  pAssemblyData, (COUNT_T)uAssemblyLength, 
                                                  fForIntrospection,
                                                  pBinderToUse);
    }

    fPropagateIdentity = (fPropagateIdentity && pCallersDomain && pCallersAssembly);

    AssemblyLoadSecurity loadSecurity;
    loadSecurity.m_pEvidence = &gc.orefSecurity;
    if (fPropagateIdentity)
    {
        DWORD dwSpecialFlags = 0;

        {
            IApplicationSecurityDescriptor *pDomainSecDesc = pCallersDomain->GetSecurityDescriptor();



            gc.granted = pDomainSecDesc->GetGrantedPermissionSet();
            dwSpecialFlags = pDomainSecDesc->GetSpecialFlags();
        }


        // Instead of resolving policy, the loader should use an inherited grant set
        loadSecurity.m_pGrantSet = &gc.granted;
        loadSecurity.m_pRefusedSet = &gc.denied;
        loadSecurity.m_dwSpecialFlags = dwSpecialFlags;

        // if the caller is from another appdomain we wil not be able to get the ssembly's security descriptor
        // but that is ok, since getting a pointer to our AppDomain required full trust
        if (!pCallersDomain->GetSecurityDescriptor()->IsFullyTrusted() ||
            ( pCallersAssembly->FindDomainAssembly(::GetAppDomain()) != NULL && !pCallersAssembly->GetSecurityDescriptor()->IsFullyTrusted())   )
            pFile->VerifyStrongName();
    }
    pAssembly = GetPostPolicyAssembly(pFile, fForIntrospection, &loadSecurity, TRUE);

    // perform necessary Transparency checks for this Load(byte[]) call (based on the calling method).
    if (pCallerMD)
    {
        Security::PerformTransparencyChecksForLoadByteArray(pCallerMD, pAssembly->GetSecurityDescriptor());
    }

    // In order to assign the PDB image (if present),
    // the resulting assembly's image needs to be exactly the one
    // we created above. We need pointer comparison instead of pe image equivalence
    // to avoid mixed binaries/PDB pairs of other images.
    // This applies to both Desktop CLR and CoreCLR, with or without fusion.
    BOOL fIsSameAssembly = (pAssembly->GetManifestFile()->GetILimage() == pFile->GetILimage());


    LOG((LF_CLASSLOADER, 
         LL_INFO100, 
         "\tLoaded in-memory module\n"));

    // Setting the PDB info is only applicable for our original assembly.
    // This applies to both Desktop CLR and CoreCLR, with or without fusion.
    if (fIsSameAssembly)
    {
#ifdef DEBUGGING_SUPPORTED
        // If we were given symbols, save a copy of them.
                // the debugger, load them now).
        if (pPDBData != NULL)
        {
            GCX_PREEMP();
            if ((DWORD)uPDBLength != uPDBLength) // overflow
                ThrowOutOfMemory();
            pAssembly->GetManifestModule()->SetSymbolBytes(pPDBData, (DWORD)uPDBLength);
        }
#endif // DEBUGGING_SUPPORTED
    }

    GCPROTECT_END();

    return pAssembly;
}

FCIMPL6(Object*, AssemblyNative::LoadImage, U1Array* PEByteArrayUNSAFE,
        U1Array* SymByteArrayUNSAFE, Object* securityUNSAFE,
        StackCrawlMark* stackMark, CLR_BOOL fForIntrospection, SecurityContextSource securityContextSource)
{
    FCALL_CONTRACT;

    struct _gc
    {
        U1ARRAYREF PEByteArray;
        U1ARRAYREF SymByteArray;
        OBJECTREF  security;
        OBJECTREF Throwable;
        OBJECTREF refRetVal;
    } gc;

    gc.PEByteArray  = (U1ARRAYREF) PEByteArrayUNSAFE;
    gc.SymByteArray = (U1ARRAYREF) SymByteArrayUNSAFE;
    gc.security     = (OBJECTREF)  securityUNSAFE;
    gc.Throwable = NULL;
    gc.refRetVal = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);


    if (gc.PEByteArray == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_Array"));

    NewArrayHolder<BYTE> pbSyms;
    DWORD cbSyms = 0;

#ifdef DEBUGGING_SUPPORTED
    // If we were given symbols, save a copy of them.
            // the debugger, load them now).
    if (gc.SymByteArray != NULL)
    {
        Security::CopyByteArrayToEncoding(&gc.SymByteArray,
                                                    &pbSyms, &cbSyms);

    }
#endif // DEBUGGING_SUPPORTED

    Assembly* pAssembly = NULL;
    // Pin byte array for loading
    {
        Wrapper<OBJECTHANDLE, DoNothing, DestroyPinningHandle> handle(
            GetAppDomain()->CreatePinningHandle(gc.PEByteArray));

        const BYTE *pbImage = gc.PEByteArray->GetDirectConstPointerToNonObjectElements();
        DWORD cbImage = gc.PEByteArray->GetNumComponents();
        pAssembly = LoadFromBuffer(fForIntrospection, pbImage, cbImage, pbSyms, cbSyms, stackMark, OBJECTREFToObject(gc.security), securityContextSource);
    }


    if (pAssembly != NULL)
        gc.refRetVal = pAssembly->GetExposedObject();

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.refRetVal);
}
FCIMPLEND

FCIMPL2(Object*, AssemblyNative::LoadFile, StringObject* pathUNSAFE, Object* securityUNSAFE)
{
    FCALL_CONTRACT;

    struct _gc {
        OBJECTREF refRetVal;
        OBJECTREF refSecurity;
        STRINGREF strPath;
    } gc;
    
    gc.refRetVal = NULL;
    gc.refSecurity = ObjectToOBJECTREF(securityUNSAFE);
    gc.strPath = ObjectToSTRINGREF(pathUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);

    if(CorHost2::IsLoadFromBlocked())
        COMPlusThrow(kFileLoadException, FUSION_E_LOADFROM_BLOCKED);

    if (pathUNSAFE == NULL)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_Path"));

    StackSString path;
    gc.strPath->GetSString(path);

    Assembly *pAssembly = AssemblySpec::LoadAssembly(path);

    LOG((LF_CLASSLOADER, 
         LL_INFO100, 
         "\tLoaded assembly from a file\n"));


    if (pAssembly != NULL)
        gc.refRetVal = (ASSEMBLYREF) pAssembly->GetExposedObject();

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.refRetVal);
}
FCIMPLEND


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
    
    // Set the caller's assembly to be mscorlib
    DomainAssembly *pCallersAssembly = SystemDomain::System()->SystemAssembly()->GetDomainAssembly();
    PEAssembly *pParentAssembly = pCallersAssembly->GetFile();
    
    // Initialize the AssemblySpec
    AssemblySpec spec;
    spec.InitializeSpec(TokenFromRid(1, mdtAssembly), pImage->GetMDImport(), pCallersAssembly);
    spec.SetBindingContext(pBinderContext);
    
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
    
    PEAssemblyHolder pPEAssembly(PEAssembly::Open(pParentAssembly, assem->GetPEImage(), assem->GetNativePEImage(), pAssembly, FALSE));

    GCX_COOP();
    
    IApplicationSecurityDescriptor *pDomainSecDesc = pCurDomain->GetSecurityDescriptor();
    
    OBJECTREF refGrantedPermissionSet = NULL;
    AssemblyLoadSecurity loadSecurity;
    DomainAssembly *pDomainAssembly = NULL;
    
    // Setup the AssemblyLoadSecurity to perform the assembly load
    GCPROTECT_BEGIN(refGrantedPermissionSet);
    
    loadSecurity.m_dwSpecialFlags = pDomainSecDesc->GetSpecialFlags();
    refGrantedPermissionSet = pDomainSecDesc->GetGrantedPermissionSet();
    loadSecurity.m_pGrantSet = &refGrantedPermissionSet;
        
    pDomainAssembly = pCurDomain->LoadDomainAssembly(&spec, pPEAssembly, FILE_LOADED, &loadSecurity);
    pLoadedAssembly = pDomainAssembly->GetAssembly();

    GCPROTECT_END();
    
    RETURN pLoadedAssembly;
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
        pILImage = PEImage::OpenImage(pwzILPath);
        
        // Need to verify that this is a valid CLR assembly. 
        if (!pILImage->CheckILFormat())
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);
    }
    
    // Form the PEImage for the NI assembly, if specified
    if (pwzNIPath != NULL)
    {
        pNIImage = PEImage::OpenImage(pwzNIPath, MDInternalImport_TrustedNativeImage);

        if (pNIImage->HasReadyToRunHeader())
        {
            // ReadyToRun images are treated as IL images by the rest of the system
            if (!pNIImage->CheckILFormat())
                ThrowHR(COR_E_BADIMAGEFORMAT);

            pILImage = pNIImage.Extract();
            pNIImage = NULL;
        }
        else
        {
            if (!pNIImage->CheckNativeFormat())
                ThrowHR(COR_E_BADIMAGEFORMAT);
        }
    }
    
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

// static
INT_PTR QCALLTYPE AssemblyNative::InternalLoadUnmanagedDllFromPath(LPCWSTR unmanagedLibraryPath)
{
    QCALL_CONTRACT;

    HMODULE moduleHandle = nullptr;

    BEGIN_QCALL;

    moduleHandle = NDirect::LoadLibraryFromPath(unmanagedLibraryPath);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(moduleHandle);
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


/* static */
Assembly* AssemblyNative::GetPostPolicyAssembly(PEAssembly *pFile,
                                                BOOL fForIntrospection,
                                                AssemblyLoadSecurity *pLoadSecurity,
                                                BOOL fIsLoadByteArray /* = FALSE */)
{
    CONTRACT(Assembly*)
    {
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pFile));
        PRECONDITION(CheckPointer(pLoadSecurity));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    GCX_PREEMP();

    if (fIsLoadByteArray)
    {
        PEImage *pPEImage = pFile->GetILimage();
        HRESULT hr = S_OK;
        PTR_AppDomain pCurDomain = GetAppDomain();
        CLRPrivBinderCoreCLR *pTPABinder = pCurDomain->GetTPABinderContext();

        _ASSERTE(pCurDomain->GetFusionContext() == pTPABinder);
        hr = pTPABinder->PreBindByteArray(pPEImage, fForIntrospection);
        if (hr == S_OK)
        {
            AssemblySpec spec;
            spec.InitializeSpec(pFile);
            
            // Set the binder associated with the AssemblySpec
            spec.SetBindingContext(pTPABinder);
            RETURN spec.LoadAssembly(FILE_LOADED, pLoadSecurity);
        }
        else
        {
            _ASSERTE(hr != S_FALSE);
            ThrowHR(hr);
        }
    }   

    RETURN GetAppDomain()->LoadAssembly(NULL, pFile, FILE_LOADED, pLoadSecurity);
}


void QCALLTYPE AssemblyNative::GetLocation(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    {
        retString.Set(pAssembly->GetFile()->GetPath());
    }
    
    END_QCALL;
}

FCIMPL1(FC_BOOL_RET, AssemblyNative::IsReflectionOnly, AssemblyBaseObject *pAssemblyUNSAFE)
{
    FCALL_CONTRACT;

    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);
    
    if (refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(refAssembly->GetDomainAssembly()->IsIntrospectionOnly());
}
FCIMPLEND

void QCALLTYPE AssemblyNative::GetType(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, BOOL bThrowOnError, BOOL bIgnoreCase, QCall::ObjectHandleOnStack retType, QCall::ObjectHandleOnStack keepAlive)
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

    // Load the class from this assembly (fail if it is in a different one).
    retTypeHandle = TypeName::GetTypeManaged(wszName, pAssembly, bThrowOnError, bIgnoreCase, pAssembly->IsIntrospectionOnly(), prohibitAsmQualifiedName, NULL, FALSE, (OBJECTREF*)keepAlive.m_ppObject);

    if (!retTypeHandle.IsNull())
    {
         GCX_COOP();
         retType.Set(retTypeHandle.GetManagedClassObject());
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

void QCALLTYPE AssemblyNative::GetCodeBase(QCall::AssemblyHandle pAssembly, BOOL fCopiedName, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    StackSString codebase;

    {
        pAssembly->GetFile()->GetCodeBase(codebase);
    }

    retString.Set(codebase);

    END_QCALL;
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

BYTE * QCALLTYPE AssemblyNative::GetResource(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, UINT64 * length, QCall::StackCrawlMarkHandle stackMark, BOOL skipSecurityCheck)
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

    DWORD  cbResource;
    if (pAssembly->GetResource(pNameUTF8, &cbResource,
                               &pbInMemoryResource, NULL, NULL,
                               NULL, stackMark, skipSecurityCheck, FALSE))
    {
        *length = cbResource;
    }

    END_QCALL;

    // Can return null if resource file is zero-length
    return pbInMemoryResource;
}

INT32 QCALLTYPE AssemblyNative::GetManifestResourceInfo(QCall::AssemblyHandle pAssembly, LPCWSTR wszName, QCall::ObjectHandleOnStack retAssembly, QCall::StringHandleOnStack retFileName, QCall::StackCrawlMarkHandle stackMark)
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
                              &dwLocation, stackMark, FALSE, FALSE))
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

    ReflectionModule *pOnDiskManifest = NULL;
    if (pAssembly->GetAssembly()->NeedsToHideManifestForEmit())
        pOnDiskManifest = pAssembly->GetAssembly()->GetOnDiskManifestModule();

    mdFile mdFile;
    while (pAssembly->GetMDImport()->EnumNext(&phEnum, &mdFile))
    {
        DomainFile *pModule = pAssembly->GetModule()->LoadModule(GetAppDomain(), mdFile, fGetResourceModules, !fLoadIfNotFound);

        if (pModule && pModule->GetModule() != pOnDiskManifest) {
            modules.Append(pModule);
        }
    }
    
    {
        GCX_COOP();

        PTRARRAYREF orModules = NULL;
        
        GCPROTECT_BEGIN(orModules);

        // Return the modules
        orModules = (PTRARRAYREF)AllocateObjectArray(modules.GetCount(), MscorlibBinder::GetClass(CLASS__MODULE));

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

BOOL QCALLTYPE AssemblyNative::GetNeutralResourcesLanguageAttribute(QCall::AssemblyHandle pAssembly, QCall::StringHandleOnStack cultureName, INT16& outFallbackLocation)
{
    CONTRACTL {
        QCALL_CHECK;
    } CONTRACTL_END;

    BOOL retVal = FALSE;
    BEGIN_QCALL;

    _ASSERTE(pAssembly);
    Assembly * pAsm = pAssembly->GetAssembly();
    _ASSERTE(pAsm);
    Module * pModule = pAsm->GetManifestModule();
    _ASSERTE(pModule);

    LPCUTF8 pszCultureName = NULL;
    ULONG cultureNameLength = 0;
    INT16 fallbackLocation = 0;

    // find the attribute if it exists
    if (pModule->GetNeutralResourcesLanguage(&pszCultureName, &cultureNameLength, &fallbackLocation, FALSE)) {
        StackSString culture(SString::Utf8, pszCultureName, cultureNameLength);
        cultureName.Set(culture);
        outFallbackLocation = fallbackLocation;
        retVal = TRUE;
    }

    END_QCALL;

    return retVal;
}

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
        orTypes = (PTRARRAYREF)AllocateObjectArray(types.GetCount(), MscorlibBinder::GetClass(CLASS__TYPE));

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
        orTypes = (PTRARRAYREF)AllocateObjectArray(types.GetCount(), MscorlibBinder::GetClass(CLASS__TYPE));

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

    MethodTable* pAsmNameClass = MscorlibBinder::GetClass(CLASS__ASSEMBLY_NAME);

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
// Get the raw bytes making up this assembly
//
// Arguments:
//    pAssembly   - Assembly to get the data of
//    retRawBytes - [out] raw bytes of the assembly
//

// static
void QCALLTYPE AssemblyNative::GetRawBytes(QCall::AssemblyHandle pAssembly,
                                           QCall::ObjectHandleOnStack retRawBytes)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    PEFile *pPEFile = pAssembly->GetFile();
    if (pPEFile != NULL)
    {
        PEImage *pPEImage = pPEFile->GetILimage();

        if (pPEImage != NULL)
        {
            SBuffer dataBuffer;
            pPEImage->GetImageBits(PEImageLayout::LAYOUT_FLAT, dataBuffer);

            if (dataBuffer.GetSize() > 0)
            {
                retRawBytes.SetByteArray(dataBuffer, dataBuffer.GetSize());
            }
        }
    }
    
    END_QCALL;
}

//---------------------------------------------------------------------------------------
//
// Release QCALL for System.SafePEFileHandle
//
//

// static
void QCALLTYPE AssemblyNative::ReleaseSafePEFileHandle(PEFile *pPEFile)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(pPEFile));
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    pPEFile->Release();

    END_QCALL;
}

// save the manifest to disk!
extern void ManagedBitnessFlagsToUnmanagedBitnessFlags(
    INT32 portableExecutableKind, INT32 imageFileMachine,
    DWORD* pPeFlags, DWORD* pCorhFlags);

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


void QCALLTYPE AssemblyNative::GetGrantSet(QCall::AssemblyHandle pAssembly, QCall::ObjectHandleOnStack retGranted, QCall::ObjectHandleOnStack retDenied)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    IAssemblySecurityDescriptor *pSecDesc = pAssembly->GetSecurityDescriptor();

    {
        GCX_COOP();

        pSecDesc->Resolve();

        OBJECTREF granted, denied;

        granted = pSecDesc->GetGrantedPermissionSet(&denied);

        retGranted.Set(granted);
        retDenied.Set(denied);
    }

    END_QCALL;
}

//
// QCalls to determine if everything introduced by the assembly is either security critical or safe critical
//

// static
BOOL QCALLTYPE AssemblyNative::IsAllSecurityCritical(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    BOOL fIsCritical = FALSE;

    BEGIN_QCALL;

    fIsCritical = pAssembly->GetSecurityDescriptor()->IsAllCritical();

    END_QCALL;

    return fIsCritical;
}

// static
BOOL QCALLTYPE AssemblyNative::IsAllSecuritySafeCritical(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    BOOL fIsSafeCritical = FALSE;

    BEGIN_QCALL;

    fIsSafeCritical = pAssembly->GetSecurityDescriptor()->IsAllSafeCritical();

    END_QCALL;

    return fIsSafeCritical;
}

// static
BOOL QCALLTYPE AssemblyNative::IsAllPublicAreaSecuritySafeCritical(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    BOOL fIsAllPublicAreaSafeCritical = FALSE;

    BEGIN_QCALL;

    fIsAllPublicAreaSafeCritical = pAssembly->GetSecurityDescriptor()->IsAllPublicAreaSafeCritical();

    END_QCALL;

    return fIsAllPublicAreaSafeCritical;
}

// static
BOOL QCALLTYPE AssemblyNative::IsAllSecurityTransparent(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    BOOL fIsTransparent = FALSE;

    BEGIN_QCALL;

    fIsTransparent = pAssembly->GetSecurityDescriptor()->IsAllTransparent();

    END_QCALL;

    return fIsTransparent;
}

// return the on disk assembly module for reflection emit. This only works for dynamic assembly.
FCIMPL1(ReflectModuleBaseObject *, AssemblyNative::GetOnDiskAssemblyModule, AssemblyBaseObject* pAssemblyUNSAFE)
{
    FCALL_CONTRACT;

    ASSEMBLYREF refAssembly = (ASSEMBLYREF)ObjectToOBJECTREF(pAssemblyUNSAFE);
    
    if (refAssembly == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    DomainAssembly *pAssembly = refAssembly->GetDomainAssembly();

    FC_RETURN_MODULE_OBJECT(pAssembly->GetCurrentAssembly()->GetOnDiskManifestModule(), refAssembly);
}
FCIMPLEND

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



#ifdef FEATURE_APPX
/*static*/
BOOL QCALLTYPE AssemblyNative::IsDesignerBindingContext(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    BOOL fRet = FALSE;

    BEGIN_QCALL;

    PEFile *pPEFile = pAssembly->GetFile();
    fRet = pPEFile->IsDesignerBindingContext();

    END_QCALL;

    return fRet;
}
#endif // FEATURE_APPX

/*static*/
INT_PTR QCALLTYPE AssemblyNative::InitializeAssemblyLoadContext(INT_PTR ptrManagedAssemblyLoadContext, BOOL fRepresentsTPALoadContext)
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
        IfFailThrow(CLRPrivBinderAssemblyLoadContext::SetupContext(pCurDomain->GetId().m_dwId, pTPABinderContext, ptrManagedAssemblyLoadContext, &pBindContext));
        ptrNativeAssemblyLoadContext = reinterpret_cast<INT_PTR>(pBindContext);
    }
    else
    {
        // We are initializing the managed instance of Assembly Load Context that would represent the TPA binder.
        // First, confirm we do not have an existing managed ALC attached to the TPA binder.
        INT_PTR ptrTPAAssemblyLoadContext = pTPABinderContext->GetManagedAssemblyLoadContext();
        if ((ptrTPAAssemblyLoadContext != NULL) && (ptrTPAAssemblyLoadContext != ptrManagedAssemblyLoadContext))
        {
            COMPlusThrow(kInvalidOperationException, IDS_HOST_ASSEMBLY_RESOLVER_INCOMPATIBLE_TPA_BINDING_CONTEXT);
        }

        // Attach the managed TPA binding context with the native one.
        pTPABinderContext->SetManagedAssemblyLoadContext(ptrManagedAssemblyLoadContext);
        ptrNativeAssemblyLoadContext = reinterpret_cast<INT_PTR>(pTPABinderContext);
    }
   
    END_QCALL;
    
    return ptrNativeAssemblyLoadContext;
}

/*static*/
BOOL QCALLTYPE AssemblyNative::OverrideDefaultAssemblyLoadContextForCurrentDomain(INT_PTR ptrNativeAssemblyLoadContext)
{
    QCALL_CONTRACT;
    
    BOOL fOverrodeDefaultLoadContext = FALSE;
    
    BEGIN_QCALL;
    
    AppDomain *pCurDomain = AppDomain::GetCurrentDomain();
    
    if (pCurDomain->LockBindingModel())
    {
        // Only one thread will ever enter here - it will be the ones that actually locked the binding model
        //
        // AssemblyLoadContext should have a binder associated with it
        IUnknown *pOverrideBinder = reinterpret_cast<IUnknown *>(ptrNativeAssemblyLoadContext);
        _ASSERTE(pOverrideBinder != NULL);
        
        // Get reference to the current default context binder
        
        IUnknown * pCurrentDefaultContextBinder = pCurDomain->GetFusionContext();
        
        // The default context binder can never be null since the runtime always sets one up
        _ASSERTE(pCurrentDefaultContextBinder != NULL);
        
        // The default context should also be the same as TPABinder context
        _ASSERTE(pCurrentDefaultContextBinder == pCurDomain->GetTPABinderContext());
        
        // Override the default context binder in the VM
        pCurDomain->OverrideDefaultContextBinder(pOverrideBinder);
        
        fOverrodeDefaultLoadContext = TRUE;
    }
    
    END_QCALL;
    
    return fOverrodeDefaultLoadContext;
}

BOOL QCALLTYPE AssemblyNative::CanUseAppPathAssemblyLoadContextInCurrentDomain()
{
    QCALL_CONTRACT;
    
    BOOL fCanUseAppPathAssemblyLoadContext = FALSE;
    
    BEGIN_QCALL;

    AppDomain *pCurDomain = AppDomain::GetCurrentDomain();

    pCurDomain->LockBindingModel();
        
    fCanUseAppPathAssemblyLoadContext = !pCurDomain->IsHostAssemblyResolverInUse();

    END_QCALL;
    
    return fCanUseAppPathAssemblyLoadContext;
}

/*static*/
INT_PTR QCALLTYPE AssemblyNative::GetLoadContextForAssembly(QCall::AssemblyHandle pAssembly)
{
    QCALL_CONTRACT;

    INT_PTR ptrManagedAssemblyLoadContext = NULL;
    
    BEGIN_QCALL;
    
    // Get the PEAssembly for the RuntimeAssembly
    PEFile *pPEFile = pAssembly->GetFile();
    PTR_PEAssembly pPEAssembly = pPEFile->AsAssembly();
    _ASSERTE(pAssembly != NULL);
   
    // Platform assemblies are semantically bound against the "Default" binder which could be the TPA Binder or
    // the overridden binder. In either case, the reference to the same will be returned when this QCall returns.
    if (!pPEAssembly->IsProfileAssembly())
    {
        // Get the binding context for the assembly.
        //
        ICLRPrivBinder *pOpaqueBinder = nullptr;
        AppDomain *pCurDomain = AppDomain::GetCurrentDomain();
        CLRPrivBinderCoreCLR *pTPABinder = pCurDomain->GetTPABinderContext();

        
        // GetBindingContext returns a ICLRPrivAssembly which can be used to get access to the
        // actual ICLRPrivBinder instance in which the assembly was loaded.
        PTR_ICLRPrivBinder pBindingContext = pPEAssembly->GetBindingContext();
        UINT_PTR assemblyBinderID = 0;
        IfFailThrow(pBindingContext->GetBinderID(&assemblyBinderID));

        // If the assembly was bound using the TPA binder,
        // then we will return the reference to "Default" binder from the managed implementation when this QCall returns.
        //
        // See earlier comment about "Default" binder for additional context.
        pOpaqueBinder = reinterpret_cast<ICLRPrivBinder *>(assemblyBinderID);
        
        // We should have a load context binder at this point.
        _ASSERTE(pOpaqueBinder != nullptr);

        if (!AreSameBinderInstance(pTPABinder, pOpaqueBinder))
        {
            // Only CLRPrivBinderAssemblyLoadContext instance contains the reference to its
            // corresponding managed instance.
            CLRPrivBinderAssemblyLoadContext *pBinder = (CLRPrivBinderAssemblyLoadContext *)(pOpaqueBinder);
            
            // Fetch the managed binder reference from the native binder instance
            ptrManagedAssemblyLoadContext = pBinder->GetManagedAssemblyLoadContext();
            _ASSERTE(ptrManagedAssemblyLoadContext != NULL);
        }
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
