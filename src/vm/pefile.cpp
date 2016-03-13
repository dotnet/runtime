// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEFile.cpp
// 

// --------------------------------------------------------------------------------


#include "common.h"
#include "pefile.h"
#include "strongname.h"
#include "corperm.h"
#include "eecontract.h"
#include "apithreadstress.h"
#include "eeconfig.h"
#ifdef FEATURE_FUSION
#include "fusionpriv.h"
#include "shlwapi.h"
#endif
#include "product_version.h"
#include "eventtrace.h"
#include "security.h"
#include "corperm.h"
#include "dbginterface.h"
#include "peimagelayout.inl"
#include "dlwrap.h"
#include "invokeutil.h"
#ifdef FEATURE_PREJIT
#include "compile.h"
#endif
#include "strongnameinternal.h"

#ifdef FEATURE_VERSIONING
#include "../binder/inc/applicationcontext.hpp"
#endif

#ifndef FEATURE_FUSION
#include "clrprivbinderutil.h"
#include "../binder/inc/coreclrbindercommon.h"
#endif

#ifdef FEATURE_CAS_POLICY
#include <wintrust.h>
#endif

#ifdef FEATURE_PREJIT
#include "compile.h"

#ifdef DEBUGGING_SUPPORTED
SVAL_IMPL_INIT(DWORD, PEFile, s_NGENDebugFlags, 0);
#endif
#endif

#include "sha1.h"

#if defined(FEATURE_HOSTED_BINDER) && defined(FEATURE_FUSION)
#include "clrprivbinderfusion.h"
#include "clrprivbinderappx.h"
#include "clrprivbinderloadfile.h" 
#endif

#ifndef DACCESS_COMPILE

// ================================================================================
// PEFile class - this is an abstract base class for PEModule and PEAssembly
// <TODO>@todo: rename TargetFile</TODO>
// ================================================================================

PEFile::PEFile(PEImage *identity, BOOL fCheckAuthenticodeSignature/*=TRUE*/) :
#if _DEBUG
    m_pDebugName(NULL),
#endif
    m_identity(NULL),
    m_openedILimage(NULL),
#ifdef FEATURE_PREJIT    
    m_nativeImage(NULL),
    m_fCanUseNativeImage(TRUE),
#endif
    m_MDImportIsRW_Debugger_Use_Only(FALSE),
    m_bHasPersistentMDImport(FALSE),
    m_pMDImport(NULL),
    m_pImporter(NULL),
    m_pEmitter(NULL),
#ifndef FEATURE_CORECLR
    m_pAssemblyImporter(NULL),
    m_pAssemblyEmitter(NULL),
#endif
    m_pMetadataLock(::new SimpleRWLock(PREEMPTIVE, LOCK_TYPE_DEFAULT)),
    m_refCount(1),
    m_hash(NULL),
    m_flags(0),
    m_fStrongNameVerified(FALSE)
#ifdef FEATURE_CAS_POLICY
    ,m_certificate(NULL),
    m_fCheckedCertificate(FALSE)
    ,m_pSecurityManager(NULL)
    ,m_securityManagerLock(CrstPEFileSecurityManager)
#endif // FEATURE_CAS_POLICY
#ifdef FEATURE_HOSTED_BINDER
    ,m_pHostAssembly(nullptr)
#endif // FEATURE_HOSTED_BINDER
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (identity)
    {
        identity->AddRef();
        m_identity = identity;

        if(identity->IsOpened())
        {
            //already opened, prepopulate
            identity->AddRef();
            m_openedILimage = identity;
        }
    }


#ifdef FEATURE_CAS_POLICY
    if (fCheckAuthenticodeSignature)
    {
        CheckAuthenticodeSignature();
    }
#endif // FEATURE_CAS_POLICY
}



PEFile::~PEFile()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    ReleaseMetadataInterfaces(TRUE);
    
    if (m_hash != NULL)
        delete m_hash;

#ifdef FEATURE_PREJIT
    if (m_nativeImage != NULL)
    {
        MarkNativeImageInvalidIfOwned();

        m_nativeImage->Release();
    }
#endif //FEATURE_PREJIT


    if (m_openedILimage != NULL)
        m_openedILimage->Release();
    if (m_identity != NULL)
        m_identity->Release();
    if (m_pMetadataLock)
        delete m_pMetadataLock;
#ifdef FEATURE_CAS_POLICY
    if (m_pSecurityManager) {
        m_pSecurityManager->Release();
        m_pSecurityManager = NULL;
    }
    if (m_certificate && !g_pCertificateCache->Contains(m_certificate))
        CoTaskMemFree(m_certificate);
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_HOSTED_BINDER
    if (m_pHostAssembly != NULL)
    {
        m_pHostAssembly->Release();
    }
#endif
}

#ifndef  DACCESS_COMPILE
void PEFile::ReleaseIL()
{
    WRAPPER_NO_CONTRACT;
    if (m_openedILimage!=NULL )
    {
        ReleaseMetadataInterfaces(TRUE, TRUE);
        if (m_identity != NULL)
        {
            m_identity->Release();
            m_identity=NULL;
        }
        m_openedILimage->Release();
        m_openedILimage = NULL;
    }
}
#endif

/* static */
PEFile *PEFile::Open(PEImage *image)
{
    CONTRACT(PEFile *)
    {
        PRECONDITION(image != NULL);
        PRECONDITION(image->CheckFormat());
        POSTCONDITION(RETVAL != NULL);
        POSTCONDITION(!RETVAL->IsModule());
        POSTCONDITION(!RETVAL->IsAssembly());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    PEFile *pFile = new PEFile(image, FALSE);

    if (image->HasNTHeaders() && image->HasCorHeader())
        pFile->OpenMDImport_Unsafe(); //no one else can see the object yet

#if _DEBUG
    pFile->m_debugName = image->GetPath();
    pFile->m_debugName.Normalize();
    pFile->m_pDebugName = pFile->m_debugName;
#endif

    RETURN pFile;
}

// ------------------------------------------------------------
// Loader support routines
// ------------------------------------------------------------

template<class T> void CoTaskFree(T *p)
{
    if (p != NULL)
    {
        p->T::~T();

        CoTaskMemFree(p);
    }
}


NEW_WRAPPER_TEMPLATE1(CoTaskNewHolder, CoTaskFree<_TYPE>);

BOOL PEFile::CanLoadLibrary()
{
    WRAPPER_NO_CONTRACT;

    // Dynamic and resource modules don't need LoadLibrary.
    if (IsDynamic() || IsResource()||IsLoaded())
        return TRUE;

    // If we're been granted skip verification, OK
    if (HasSkipVerification())
        return TRUE;

    // Otherwise, we can only load if IL only.
    return IsILOnly();
}


#ifdef FEATURE_CORECLR
void PEFile::ValidateImagePlatformNeutrality()
{
    STANDARD_VM_CONTRACT;

    //--------------------------------------------------------------------------------
    // There are no useful applications of the "/platform" switch for CoreCLR.
    // CoreCLR will do the conservative thing and by default only accept appbase assemblies
    // compiled with "/platform:anycpu" (or no "/platform" switch at all.)
    // However, with hosting flags it is possible to suppress this check and allow
    // platform specific assemblies. This was primarily added to support C++/CLI
    // generated assemblies build with /CLR:PURE flags. This was a need for the CoreSystem
    // server work.
    //
    // We do allow Platform assemblies to have platform specific code (because they
    // in fact do have such code.   
    //--------------------------------------------------------------------------------
    if (!(GetAssembly()->IsProfileAssembly()) && !GetAppDomain()->AllowPlatformSpecificAppAssemblies())
    {
        
        DWORD machine, kind;
        BOOL fMachineOk,fPlatformFlagsOk;

#ifdef FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
        if (ShouldTreatNIAsMSIL() && GetILimage()->HasNativeHeader())
        {
            GetILimage()->GetNativeILPEKindAndMachine(&kind, &machine);                 
        }
        else       
#endif // FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
        {
            //The following function gets the kind and machine given by the IL image. 
            //In the case of NGened images- It gets the original kind and machine of the IL image
            //from the copy maintained by  NI
            GetPEKindAndMachine(&kind, &machine);       
        } 
        
        fMachineOk = (machine == IMAGE_FILE_MACHINE_I386);
        fPlatformFlagsOk = ((kind & (peILonly | pe32Plus | pe32BitRequired)) == peILonly);
        
#ifdef FEATURE_LEGACYNETCF
        if (GetAppDomain()->GetAppDomainCompatMode() == BaseDomain::APPDOMAINCOMPAT_APP_EARLIER_THAN_WP8)
            fPlatformFlagsOk = ((kind & (peILonly | pe32Plus)) == peILonly);
#endif

        if (!(fMachineOk &&
              fPlatformFlagsOk))
        {
            // This exception matches what the desktop OS hook throws - unfortunate that this is so undescriptive.
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
        }
    }
}
#endif

#ifdef FEATURE_MIXEDMODE

#ifndef CROSSGEN_COMPILE

// Returns TRUE if this file references managed CRT (msvcmNN*).
BOOL PEFile::ReferencesManagedCRT()
{
    STANDARD_VM_CONTRACT;

    IMDInternalImportHolder pImport = GetMDImport();
    MDEnumHolder hEnum(pImport);

    IfFailThrow(pImport->EnumInit(mdtModuleRef, mdTokenNil, &hEnum));

    mdModuleRef tk;
    while (pImport->EnumNext(&hEnum, &tk))
    {
        // we are looking for "msvcmNN*"
        LPCSTR szName;
        IfFailThrow(pImport->GetModuleRefProps(tk, &szName));
        
        if (_strnicmp(szName, "msvcm", 5) == 0 && isdigit(szName[5]) && isdigit(szName[6]))
        {
            return TRUE;
        }
    }

    return FALSE;
}

void PEFile::CheckForDisallowedInProcSxSLoadWorker()
{
    STANDARD_VM_CONTRACT;

    // provide an opt-out switch for now
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_DisableIJWVersionCheck) != 0)
        return;

    // ************************************************************************************************
    // 1. See if this file should be checked
    // The following checks filter out non-mixed mode assemblies that don't reference msvcmNN*. We only
    // care about non-ILONLY images (IJW) or 2.0 C++/CLI pure images.
    if (IsResource() || IsDynamic())
        return;

    // check the metadata version string
    COUNT_T size;
    PVOID pMetaData = (PVOID)GetMetadata(&size);
    if (!pMetaData)
    {
        // No metadata section? Well somebody should have caught this earlier so report as
        // ExecutionEngine rather than BIF.
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }

    LPCSTR pVersion = NULL;
    IfFailThrow(GetImageRuntimeVersionString(pMetaData, &pVersion));

    char chV;
    unsigned uiMajor, uiMinor;
    BOOL fLegacyImage = (sscanf_s(pVersion, "%c%u.%u", &chV, 1, &uiMajor, &uiMinor) == 3 && (chV == W('v') || chV == W('V')) && uiMajor <= 2);

    // Note that having VTFixups properly working really is limited to non-ILONLY images. In particular,
    // the shim does not even attempt to patch ILONLY images in any way with bootstrap thunks.
    if (IsILOnly())
    {
        // all >2.0 ILONLY images are fine because >2.0 managed CRTs can be loaded in multiple runtimes
        if (!fLegacyImage)
            return;

        // legacy ILONLY images that don't reference the managed CRT are fine
        if (!ReferencesManagedCRT())
            return;
    }

    // get the version of this runtime
    WCHAR wzThisRuntimeVersion[_MAX_PATH];
    DWORD cchVersion = COUNTOF(wzThisRuntimeVersion);
    IfFailThrow(g_pCLRRuntime->GetVersionString(wzThisRuntimeVersion, &cchVersion));
    
    // ************************************************************************************************
    // 2. For legacy assemblies, verify that legacy APIs are/would be bound to this runtime
    if (fLegacyImage)
    {
        WCHAR wzAPIVersion[_MAX_PATH];
        bool fLegacyAPIsAreBound = false;
     
        {   // Check if the legacy APIs have already been bound to us using the new hosting APIs.
            ReleaseHolder<ICLRMetaHost> pMetaHost;
            IfFailThrow(CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)&pMetaHost));

            ReleaseHolder<ICLRRuntimeInfo> pInfo;
            // Returns S_FALSE when no runtime is currently bound, S_OK when one is.
            HRESULT hr = pMetaHost->QueryLegacyV2RuntimeBinding(IID_ICLRRuntimeInfo, (LPVOID*)&pInfo);
            IfFailThrow(hr);

            if (hr == S_OK)
            {   // Legacy APIs are bound, now check if they are bound to us.
                fLegacyAPIsAreBound = true;

                cchVersion = COUNTOF(wzAPIVersion);
                IfFailThrow(pInfo->GetVersionString(wzAPIVersion, &cchVersion));

                if (SString::_wcsicmp(wzThisRuntimeVersion, wzAPIVersion) == 0)
                {   // This runtime is the one bound to the legacy APIs, ok to load legacy assembly.
                    return;
                }
            }
        }

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4996) // we are going to call deprecated APIs
#endif
        // We need the above QueryLegacyV2RuntimeBinding check because GetRequestedRuntimeInfo will not take into
        // account the current binding, which could have been set by the host rather than through an EXE config.
        // If, however, the legacy APIs are not bound (indicated in fLegacyAPIsAreBound) then we can assume that
        // the legacy APIs would bind using the equivalent of CorBindToRuntime(NULL) as a result of loading this
        // legacy IJW assembly, and so we use GetRequestedRuntimeInfo to check without actually causing the bind.
        // By avoiding causing the bind, we avoid a binding side effect in the failure case.
        if (!fLegacyAPIsAreBound &&
            SUCCEEDED(GetRequestedRuntimeInfo(NULL, NULL, NULL, 0,       // pExe, pwszVersion, pConfigurationFile, startupFlags
                      RUNTIME_INFO_UPGRADE_VERSION | RUNTIME_INFO_DONT_RETURN_DIRECTORY | RUNTIME_INFO_DONT_SHOW_ERROR_DIALOG,
                      NULL, 0, NULL,                                     // pDirectory, dwDirectory, pdwDirectoryLength
                      wzAPIVersion, COUNTOF(wzAPIVersion), &cchVersion)))  // pVersion, cchBuffer, pdwLength
        {
            if (SString::_wcsicmp(wzThisRuntimeVersion, wzAPIVersion) == 0)
            {
                // it came back as this version - call CorBindToRuntime to actually bind it
                ReleaseHolder<ICLRRuntimeHost> pHost;
                IfFailThrow(CorBindToRuntime(wzAPIVersion, NULL, CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (LPVOID *)&pHost));

                // and verify that nobody beat us to it
                IfFailThrow(GetCORVersion(wzAPIVersion, COUNTOF(wzAPIVersion), &cchVersion));

                if (SString::_wcsicmp(wzThisRuntimeVersion, wzAPIVersion) == 0)
                {
                    // we have verified that when the assembly calls CorBindToRuntime(NULL),
                    // it will get this runtime, so we allow it to be loaded
                    return;
                }
            }
        }
#ifdef _MSC_VER
#pragma warning(pop)
#endif

        MAKE_WIDEPTR_FROMUTF8(pwzVersion, pVersion);

        ExternalLog(LF_LOADER, LL_ERROR, W("ERR: Rejecting IJW module built against %s because it could be loaded into another runtime in this process."), pwzVersion);
        COMPlusThrow(kFileLoadException, IDS_EE_IJWLOAD_CROSSVERSION_DISALLOWED, pwzVersion, QUOTE_MACRO_L(VER_MAJORVERSION.VER_MINORVERSION));
    }

    // ************************************************************************************************
    // 3. For 4.0+ assemblies, verify that it hasn't been loaded into another runtime
    ReleaseHolder<ICLRRuntimeHostInternal> pRuntimeHostInternal;
    IfFailThrow(g_pCLRRuntime->GetInterface(CLSID_CLRRuntimeHostInternal,
                                            IID_ICLRRuntimeHostInternal,
                                            &pRuntimeHostInternal));

    PTR_VOID pModuleBase = GetLoadedIL()->GetBase();

    ReleaseHolder<ICLRRuntimeInfo> pRuntimeInfo;
    HRESULT hr = pRuntimeHostInternal->LockModuleForRuntime((BYTE *)pModuleBase, IID_ICLRRuntimeInfo, &pRuntimeInfo);
    
    IfFailThrow(hr);

    if (hr == S_OK)
    {
        // this runtime was the first one to lock the module
        return;
    }

    // another runtime has loaded this module so we have to block the load
    WCHAR wzLoadedRuntimeVersion[_MAX_PATH];
    cchVersion = COUNTOF(wzLoadedRuntimeVersion);
    IfFailThrow(pRuntimeInfo->GetVersionString(wzLoadedRuntimeVersion, &cchVersion));

    ExternalLog(LF_LOADER, LL_ERROR, W("ERR: Rejecting IJW module because it is already loaded into runtime version %s in this process."), wzLoadedRuntimeVersion);
    COMPlusThrow(kFileLoadException, IDS_EE_IJWLOAD_MULTIRUNTIME_DISALLOWED, wzThisRuntimeVersion, wzLoadedRuntimeVersion);
}

// We don't allow loading IJW and C++/CLI pure images built against <=2.0 if legacy APIs are not bound to this
// runtime. For IJW images built against >2.0, we don't allow the load if the image has already been loaded by
// another runtime in this process.
void PEFile::CheckForDisallowedInProcSxSLoad()
{
    STANDARD_VM_CONTRACT;

    // have we checked this one before?
    if (!IsInProcSxSLoadVerified())
    {
        CheckForDisallowedInProcSxSLoadWorker();

        // if no exception was thrown, remember the fact that we don't have to do the check again
        SetInProcSxSLoadVerified();
    }
}

#else // CROSSGEN_COMPILE

void PEFile::CheckForDisallowedInProcSxSLoad()
{
    // Noop for crossgen
}

#endif // CROSSGEN_COMPILE

#endif // FEATURE_MIXEDMODE


//-----------------------------------------------------------------------------------------------------
// Catch attempts to load x64 assemblies on x86, etc.
//-----------------------------------------------------------------------------------------------------
static void ValidatePEFileMachineType(PEFile *peFile)
{
    STANDARD_VM_CONTRACT;

    if (peFile->IsIntrospectionOnly())
        return;    // ReflectionOnly assemblies permitted to violate CPU restrictions

    if (peFile->IsDynamic())
        return;    // PEFiles for ReflectionEmit assemblies don't cache the machine type.

    if (peFile->IsResource())
        return;    // PEFiles for resource assemblies don't cache the machine type.

    if (peFile->HasNativeImage())
        return;    // If it passed the native binder, no need to do the check again esp. at the risk of inviting an IL page-in.

    DWORD peKind;
    DWORD actualMachineType;
    peFile->GetPEKindAndMachine(&peKind, &actualMachineType);

    if (actualMachineType == IMAGE_FILE_MACHINE_I386 && ((peKind & (peILonly | pe32BitRequired)) == peILonly))
        return;    // Image is marked CPU-agnostic.

    if (actualMachineType != IMAGE_FILE_MACHINE_NATIVE)
    {
#ifdef FEATURE_LEGACYNETCF
        if (GetAppDomain()->GetAppDomainCompatMode() == BaseDomain::APPDOMAINCOMPAT_APP_EARLIER_THAN_WP8)
        {
            if (actualMachineType == IMAGE_FILE_MACHINE_I386 && ((peKind & peILonly)) == peILonly)
                return;
        }
#endif

#ifdef _TARGET_AMD64_
        // v4.0 64-bit compatibility workaround. The 64-bit v4.0 CLR's Reflection.Load(byte[]) api does not detect cpu-matches. We should consider fixing that in
        // the next SxS release. In the meantime, this bypass will retain compat for 64-bit v4.0 CLR for target platforms that existed at the time.
        //
        // Though this bypass kicks in for all Load() flavors, the other Load() flavors did detect cpu-matches through various other code paths that still exist.
        // Or to put it another way, this #ifdef makes the (4.5 only) ValidatePEFileMachineType() a NOP for x64, hence preserving 4.0 compatibility.
        if (actualMachineType == IMAGE_FILE_MACHINE_I386 || actualMachineType == IMAGE_FILE_MACHINE_IA64)
            return;
#endif // _WIN64_

        // Image has required machine that doesn't match the CLR.
        StackSString name;
        if (peFile->IsAssembly())
            ((PEAssembly*)peFile)->GetDisplayName(name);
        else
            name = StackSString(SString::Utf8, peFile->GetSimpleName());

        COMPlusThrow(kBadImageFormatException, IDS_CLASSLOAD_WRONGCPU, name.GetUnicode());
    }

    return;   // If we got here, all is good.
}

void PEFile::LoadLibrary(BOOL allowNativeSkip/*=TRUE*/) // if allowNativeSkip==FALSE force IL image load
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    // Catch attempts to load x64 assemblies on x86, etc.
    ValidatePEFileMachineType(this);

    // See if we've already loaded it.
    if (CheckLoaded(allowNativeSkip))
    {
#ifdef FEATURE_CORECLR
        if (!IsResource() && !IsDynamic())
            ValidateImagePlatformNeutrality();
#endif //FEATURE_CORECLR

#ifdef FEATURE_MIXEDMODE
        // Prevent loading C++/CLI images into multiple runtimes in the same process. Note that if ILOnly images
        // stop being LoadLibrary'ed, the check for pure 2.0 C++/CLI images will need to be done somewhere else.
        if (!IsIntrospectionOnly())
            CheckForDisallowedInProcSxSLoad();
#endif // FEATURE_MIXEDMODE
        RETURN;
    }

    // Note that we may be racing other threads here, in the case of domain neutral files

    // Resource images are always flat.
    if (IsResource())
    {
        GetILimage()->LoadNoMetaData(IsIntrospectionOnly());
        RETURN;
    }

#ifdef FEATURE_CORECLR
    ValidateImagePlatformNeutrality();
#endif //FEATURE_CORECLR

#if !defined(_WIN64)
    if (!HasNativeImage() && (!GetILimage()->Has32BitNTHeaders()) && !IsIntrospectionOnly())
    {
        // Tried to load 64-bit assembly on 32-bit platform.
        EEFileLoadException::Throw(this, COR_E_BADIMAGEFORMAT, NULL);
    }
#endif

    // Don't do this if we are unverifiable
    if (!CanLoadLibrary())
        ThrowHR(SECURITY_E_UNVERIFIABLE);


    // We need contents now
    if (!HasNativeImage())
    {
        EnsureImageOpened();
    }

    if (IsIntrospectionOnly())
    {
        GetILimage()->LoadForIntrospection();
        RETURN;
    }


    //---- Below this point, only do the things necessary for execution ----
    _ASSERTE(!IsIntrospectionOnly());

#ifdef FEATURE_PREJIT
    // For on-disk Dlls, we can call LoadLibrary
    if (IsDll() && !((HasNativeImage()?m_nativeImage:GetILimage())->GetPath().IsEmpty()))
    {
        // Note that we may get a DllMain notification inside here.
        if (allowNativeSkip && HasNativeImage())
        {
            m_nativeImage->Load();
            if(!m_nativeImage->IsNativeILILOnly())
                GetILimage()->Load();             // For IJW we have to load IL also...
        }
        else
            GetILimage()->Load();
    }
    else
#endif // FEATURE_PREJIT
    {

        // Since we couldn't call LoadLibrary, we must be an IL only image
        // or the image may still contain unfixed up stuff
        // Note that we make an exception for CompilationDomains, since PEImage
        // will map non-ILOnly images in a compilation domain.
        if (!GetILimage()->IsILOnly() && !GetAppDomain()->IsCompilationDomain())
        {
            if (!GetILimage()->HasV1Metadata())
                ThrowHR(COR_E_FIXUPSINEXE); // <TODO>@todo: better error</TODO>            
        }



        // If we are already mapped, we can just use the current image.
#ifdef FEATURE_PREJIT
        if (allowNativeSkip && HasNativeImage())
        {
            m_nativeImage->LoadFromMapped();

            if( !m_nativeImage->IsNativeILILOnly())
                GetILimage()->LoadFromMapped();        // For IJW we have to load IL also...
        }
        else
#endif
        {
            if (GetILimage()->IsFile())
                GetILimage()->LoadFromMapped();
            else
                GetILimage()->LoadNoFile();
        }
    }

#ifdef FEATURE_MIXEDMODE
    // Prevent loading C++/CLI images into multiple runtimes in the same process. Note that if ILOnly images
    // stop being LoadLibrary'ed, the check for pure 2.0 C++/CLI images will need to be done somewhere else.
    CheckForDisallowedInProcSxSLoad();
#endif // FEATURE_MIXEDMODE

    RETURN;
}

void PEFile::SetLoadedHMODULE(HMODULE hMod)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(hMod));
        PRECONDITION(CanLoadLibrary());
        POSTCONDITION(CheckLoaded());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    // See if the image is an internal PEImage.
    GetILimage()->SetLoadedHMODULE(hMod);

    RETURN;
}

/* static */
void PEFile::DefineEmitScope(
    GUID   iid, 
    void **ppEmit)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(ppEmit));
        POSTCONDITION(CheckPointer(*ppEmit));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;
    
    SafeComHolder<IMetaDataDispenserEx> pDispenser;
    
    // Get the Dispenser interface.
    MetaDataGetDispenser(
        CLSID_CorMetaDataDispenser, 
        IID_IMetaDataDispenserEx, 
        (void **)&pDispenser);
    if (pDispenser == NULL)
    {
        ThrowOutOfMemory();
    }
    
    // Set the option on the dispenser turn on duplicate check for TypeDef and moduleRef
    VARIANT varOption;
    V_VT(&varOption) = VT_UI4;
    V_I4(&varOption) = MDDupDefault | MDDupTypeDef | MDDupModuleRef | MDDupExportedType | MDDupAssemblyRef | MDDupPermission | MDDupFile;
    IfFailThrow(pDispenser->SetOption(MetaDataCheckDuplicatesFor, &varOption));
    
    // Set minimal MetaData size
    V_VT(&varOption) = VT_UI4;
    V_I4(&varOption) = MDInitialSizeMinimal;
    IfFailThrow(pDispenser->SetOption(MetaDataInitialSize, &varOption));
    
    // turn on the thread safety!
    V_I4(&varOption) = MDThreadSafetyOn;
    IfFailThrow(pDispenser->SetOption(MetaDataThreadSafetyOptions, &varOption));
    
    IfFailThrow(pDispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, iid, (IUnknown **)ppEmit));
    
    RETURN;
} // PEFile::DefineEmitScope

// ------------------------------------------------------------
// Identity
// ------------------------------------------------------------

BOOL PEFile::Equals(PEFile *pFile)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pFile));
        GC_NOTRIGGER;
        NOTHROW;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Same object is equal
    if (pFile == this)
        return TRUE;


    // Execution and introspection files are NOT equal
    if ( (!IsIntrospectionOnly()) != !(pFile->IsIntrospectionOnly()) )
    {
        return FALSE;
    }

#ifdef FEATURE_HOSTED_BINDER
    // Different host assemblies cannot be equal unless they are associated with the same host binder
    // It's ok if only one has a host binder because multiple threads can race to load the same assembly
    // and that may cause temporary candidate PEAssembly objects that never get bound to a host assembly
    // because another thread beats it; the losing thread will pick up the PEAssembly in the cache.
    if (pFile->HasHostAssembly() && this->HasHostAssembly())
    {
        UINT_PTR fileBinderId = 0;
        if (FAILED(pFile->GetHostAssembly()->GetBinderID(&fileBinderId)))
            return FALSE;

        UINT_PTR thisBinderId = 0;
        if (FAILED(this->GetHostAssembly()->GetBinderID(&thisBinderId)))
            return FALSE;

        if (fileBinderId != thisBinderId)
            return FALSE;

    }
#endif // FEATURE_HOSTED_BINDER


    // Same identity is equal
    if (m_identity != NULL && pFile->m_identity != NULL
        && m_identity->Equals(pFile->m_identity))
        return TRUE;

    // Same image is equal
    if (m_openedILimage != NULL && pFile->m_openedILimage != NULL
        && m_openedILimage->Equals(pFile->m_openedILimage))
        return TRUE;

    return FALSE;
}

BOOL PEFile::Equals(PEImage *pImage)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pImage));
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Same object is equal
    if (pImage == m_identity || pImage == m_openedILimage)
        return TRUE;

#ifdef FEATURE_PREJIT
    if(pImage == m_nativeImage)
        return TRUE;
#endif    
    // Same identity is equal
    if (m_identity != NULL
        && m_identity->Equals(pImage))
        return TRUE;

    // Same image is equal
    if (m_openedILimage != NULL
        && m_openedILimage->Equals(pImage))
        return TRUE;


    return FALSE;
}

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

void PEFile::GetCodeBaseOrName(SString &result)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_identity != NULL && !m_identity->GetPath().IsEmpty())
    {
        result.Set(m_identity->GetPath());
    }
    else if (IsAssembly())
    {
        ((PEAssembly*)this)->GetCodeBase(result);
    }
    else
        result.SetUTF8(GetSimpleName());
}

#ifdef FEATURE_CAS_POLICY

// Returns security information for the assembly based on the codebase
void PEFile::GetSecurityIdentity(SString &codebase, SecZone *pdwZone, DWORD dwFlags, BYTE *pbUniqueID, DWORD *pcbUniqueID)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pdwZone));
        PRECONDITION(CheckPointer(pbUniqueID));
        PRECONDITION(CheckPointer(pcbUniqueID));
    }
    CONTRACTL_END;

    if (IsAssembly())
    {
        ((PEAssembly*)this)->GetCodeBase(codebase);
    }
    else if (m_identity != NULL && !m_identity->GetPath().IsEmpty())
    {
        codebase.Set(W("file:///"));
        codebase.Append(m_identity->GetPath());
    }
    else
    {
        _ASSERTE( !"Unable to determine security identity" );
    }

    GCX_PREEMP();

    if(!codebase.IsEmpty())
    {
        *pdwZone = NoZone;

        InitializeSecurityManager();

        // We have a class name, return a class factory for it
        _ASSERTE(sizeof(SecZone) == sizeof(DWORD));
        IfFailThrow(m_pSecurityManager->MapUrlToZone(codebase,
                                                     reinterpret_cast<DWORD *>(pdwZone),
                                                     dwFlags));

        if (*pdwZone>=NumZones)            
            IfFailThrow(SecurityPolicy::ApplyCustomZoneOverride(pdwZone));
        
        IfFailThrow(m_pSecurityManager->GetSecurityId(codebase,
                                                      pbUniqueID,
                                                      pcbUniqueID,
                                                      0));
    }
}

void PEFile::InitializeSecurityManager()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    if(m_pSecurityManager == NULL)
    {
        CrstHolder holder(&m_securityManagerLock);
        if (m_pSecurityManager == NULL)
        {
                IfFailThrow(CoInternetCreateSecurityManager(NULL,
                                                            &m_pSecurityManager,
                                                            0));
        }
    }
}

#endif // FEATURE_CAS_POLICY

// ------------------------------------------------------------
// Checks
// ------------------------------------------------------------



CHECK PEFile::CheckLoaded(BOOL bAllowNativeSkip/*=TRUE*/)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACT_CHECK_END;

    CHECK(IsLoaded(bAllowNativeSkip)
          // We are allowed to skip LoadLibrary in most cases for ngen'ed IL only images
          || (bAllowNativeSkip && HasNativeImage() && IsILOnly()));

    CHECK_OK;
}

#ifndef FEATURE_CORECLR
// ------------------------------------------------------------
// Hash support
// ------------------------------------------------------------

#ifndef SHA1_HASH_SIZE
#define SHA1_HASH_SIZE 20
#endif

void PEFile::GetSHA1Hash(SBuffer &result)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckValue(result));
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Cache the SHA1 hash in a buffer
    if (m_hash == NULL)
    {
        // We shouldn't have to compute a SHA1 hash in any scenarios
        // where the image opening should be suppressed.
        EnsureImageOpened();

        m_hash = new InlineSBuffer<SHA1_HASH_SIZE>();
        GetILimage()->ComputeHash(CALG_SHA1, *m_hash);
    }

    result.Set(*m_hash);
}
#endif // FEATURE_CORECLR

// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

PTR_CVOID PEFile::GetMetadata(COUNT_T *pSize)
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(pSize, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

#ifdef FEATURE_PREJIT
    if (HasNativeImageMetadata())
    {
        RETURN m_nativeImage->GetMetadata(pSize);
    }
#endif

    if (IsDynamic()
         || !GetILimage()->HasNTHeaders()
         || !GetILimage()->HasCorHeader())
    {
        if (pSize != NULL)
            *pSize = 0;
        RETURN NULL;
    }
    else
    {
        RETURN GetILimage()->GetMetadata(pSize);
    }
}
#endif // #ifndef DACCESS_COMPILE

PTR_CVOID PEFile::GetLoadedMetadata(COUNT_T *pSize)
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(pSize, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

#ifdef FEATURE_PREJIT
    if (HasNativeImageMetadata())
    {
        RETURN GetLoadedNative()->GetMetadata(pSize);
    }
#endif

    if (!HasLoadedIL() 
         || !GetLoadedIL()->HasNTHeaders()
         || !GetLoadedIL()->HasCorHeader())
    {
        if (pSize != NULL)
            *pSize = 0;
        RETURN NULL;
    }
    else
    {
        RETURN GetLoadedIL()->GetMetadata(pSize);
    }
}

TADDR PEFile::GetIL(RVA il)
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(il != 0);
        PRECONDITION(!IsDynamic());
        PRECONDITION(!IsResource());
#ifndef DACCESS_COMPILE
        PRECONDITION(CheckLoaded());
#endif
        POSTCONDITION(RETVAL != NULL);
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    PEImageLayout *image = NULL;

#ifdef FEATURE_PREJIT
    // Note it is important to get the IL from the native image if 
    // available, since we are using the metadata from the native image
    // which has different IL rva's.
    if (HasNativeImageMetadata())
    {
        image = GetLoadedNative();

#ifndef DACCESS_COMPILE
        // NGen images are trusted to be well-formed.
        _ASSERTE(image->CheckILMethod(il));
#endif
    }
    else
#endif // FEATURE_PREJIT
    {
        image = GetLoadedIL();

#ifndef DACCESS_COMPILE
        // Verify that the IL blob is valid before giving it out
        if (!image->CheckILMethod(il))
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL_RANGE);
#endif
    }

    RETURN image->GetRvaData(il);
}

#ifndef DACCESS_COMPILE

void PEFile::OpenImporter()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } 
    CONTRACTL_END;

    // Make sure internal MD is in RW format.
    ConvertMDInternalToReadWrite();
 
    IMetaDataImport2 *pIMDImport = NULL;
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetPersistentMDImport(), 
                                                       IID_IMetaDataImport2, 
                                                       (void **)&pIMDImport));

    // Atomically swap it into the field (release it if we lose the race)
    if (FastInterlockCompareExchangePointer(&m_pImporter, pIMDImport, NULL) != NULL)
        pIMDImport->Release();
}

void PEFile::ConvertMDInternalToReadWrite()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(EX_THROW(EEMessageException, (E_OUTOFMEMORY)););
    }
    CONTRACTL_END;

    IMDInternalImport *pOld;            // Old (current RO) value of internal import.
    IMDInternalImport *pNew = NULL;     // New (RW) value of internal import.

    // Take a local copy of *ppImport.  This may be a pointer to an RO
    //  or to an RW MDInternalXX.
    pOld = m_pMDImport;
    IMetaDataImport *pIMDImport = m_pImporter;
    if (pIMDImport != NULL)
    {
        HRESULT hr = GetMetaDataInternalInterfaceFromPublic(pIMDImport, IID_IMDInternalImport, (void **)&pNew);
        if (FAILED(hr))
        {
            EX_THROW(EEMessageException, (hr));
        }
        if (pNew == pOld)
        {
            pNew->Release();
            return;
        }
    }
    else
    {
        // If an RO, convert to an RW, return S_OK.  If already RW, no conversion
        //  needed, return S_FALSE.
        HRESULT hr = ConvertMDInternalImport(pOld, &pNew);

        if (FAILED(hr))
        {
            EX_THROW(EEMessageException, (hr));
        }

        // If no conversion took place, don't change pointers.
        if (hr == S_FALSE)
            return;
    }

    // Swap the pointers in a thread safe manner.  If the contents of *ppImport
    //  equals pOld then no other thread got here first, and the old contents are
    //  replaced with pNew.  The old contents are returned.
    _ASSERTE(m_bHasPersistentMDImport);
    if (FastInterlockCompareExchangePointer(&m_pMDImport, pNew, pOld) == pOld)
    {   
        //if the debugger queries, it will now see that we have RW metadata
        m_MDImportIsRW_Debugger_Use_Only = TRUE;

        // Swapped -- get the metadata to hang onto the old Internal import.
        HRESULT hr=m_pMDImport->SetUserContextData(pOld);
        _ASSERTE(SUCCEEDED(hr)||!"Leaking old MDImport");
        IfFailThrow(hr);
    }
    else
    {   // Some other thread finished first.  Just free the results of this conversion.
        pNew->Release();
    }
}

void PEFile::ConvertMetadataToRWForEnC()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This should only ever be called on EnC capable files.
    // One can check this using Module::IsEditAndContinueCapable().
    
    // This should only be called if we're debugging, stopped, and on the helper thread.
    _ASSERTE(CORDebuggerAttached());
    _ASSERTE((g_pDebugInterface != NULL) && g_pDebugInterface->ThisIsHelperThread());
    _ASSERTE((g_pDebugInterface != NULL) && g_pDebugInterface->IsStopped());

    // Convert the metadata to RW for Edit and Continue, properly replacing the metadata import interface pointer and 
    // properly preserving the old importer. This will be called before the EnC system tries to apply a delta to the module's 
    // metadata. ConvertMDInternalToReadWrite() does that quite nicely for us.
    ConvertMDInternalToReadWrite();
}

void PEFile::OpenMDImport_Unsafe()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_pMDImport != NULL)
        return;
#ifdef FEATURE_PREJIT    
    if (m_nativeImage != NULL
#ifdef FEATURE_CORECLR
        && m_nativeImage->GetMDImport() != NULL
#endif
        )
    {
        // Use native image for metadata
        m_flags |= PEFILE_HAS_NATIVE_IMAGE_METADATA;
        m_pMDImport=m_nativeImage->GetMDImport();
    }
    else
#endif
    {
#ifdef FEATURE_PREJIT        
        m_flags &= ~PEFILE_HAS_NATIVE_IMAGE_METADATA;
#endif
        if (!IsDynamic()
           && GetILimage()->HasNTHeaders()
             && GetILimage()->HasCorHeader())
        {
            m_pMDImport=GetILimage()->GetMDImport();
        }
        else
            ThrowHR(COR_E_BADIMAGEFORMAT);

        m_bHasPersistentMDImport=TRUE;
    }
    _ASSERTE(m_pMDImport);
    m_pMDImport->AddRef();
}

void PEFile::OpenEmitter()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Make sure internal MD is in RW format.
    ConvertMDInternalToReadWrite();

    IMetaDataEmit *pIMDEmit = NULL;
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetPersistentMDImport(),
                                                       IID_IMetaDataEmit,
                                                       (void **)&pIMDEmit));

    // Atomically swap it into the field (release it if we lose the race)
    if (FastInterlockCompareExchangePointer(&m_pEmitter, pIMDEmit, NULL) != NULL)
        pIMDEmit->Release();
}

#ifndef FEATURE_CORECLR
void PEFile::OpenAssemblyImporter()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Make sure internal MD is in RW format.
    ConvertMDInternalToReadWrite();

    // Get the interface
    IMetaDataAssemblyImport *pIMDAImport = NULL;
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetPersistentMDImport(), 
                                                       IID_IMetaDataAssemblyImport, 
                                                       (void **)&pIMDAImport));

    // Atomically swap it into the field (release it if we lose the race)
    if (FastInterlockCompareExchangePointer(&m_pAssemblyImporter, pIMDAImport, NULL) != NULL)
        pIMDAImport->Release();
}

void PEFile::OpenAssemblyEmitter()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Make sure internal MD is in RW format.
    ConvertMDInternalToReadWrite();

    IMetaDataAssemblyEmit *pIMDAssemblyEmit = NULL;
    IfFailThrow(GetMetaDataPublicInterfaceFromInternal((void*)GetPersistentMDImport(),
                                                       IID_IMetaDataAssemblyEmit,
                                                       (void **)&pIMDAssemblyEmit));

    // Atomically swap it into the field (release it if we lose the race)
    if (FastInterlockCompareExchangePointer(&m_pAssemblyEmitter, pIMDAssemblyEmit, NULL) != NULL)
        pIMDAssemblyEmit->Release();
}
#endif // FEATURE_CORECLR

void PEFile::ReleaseMetadataInterfaces(BOOL bDestructor, BOOL bKeepNativeData/*=FALSE*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(bDestructor||m_pMetadataLock->IsWriterLock());
    }
    CONTRACTL_END;
    _ASSERTE(bDestructor || !m_bHasPersistentMDImport);
#ifndef FEATURE_CORECLR
    if (m_pAssemblyImporter != NULL)
    {
        m_pAssemblyImporter->Release();
        m_pAssemblyImporter = NULL;
    }
    if(m_pAssemblyEmitter)
    {
        m_pAssemblyEmitter->Release();
        m_pAssemblyEmitter=NULL;
    }
#endif

    if (m_pImporter != NULL)
    {
        m_pImporter->Release();
        m_pImporter = NULL;
    }
    if (m_pEmitter != NULL)
    {
        m_pEmitter->Release();
        m_pEmitter = NULL;
    }

    if (m_pMDImport != NULL && (!bKeepNativeData || !HasNativeImage()))
    {
        m_pMDImport->Release();
        m_pMDImport=NULL;
     }
}

#ifdef FEATURE_CAS_POLICY

void PEFile::CheckAuthenticodeSignature()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Check any security signature in the header.

    // This publisher data can potentially be cached and passed back in via
    // PEAssembly::CreateDelayed.
    //
    // HOWEVER - even if we cache it, the certificate still may need to be verified at
    // load time.  The only real caching can be done when the COR_TRUST certificate is
    // ABSENT.
    //
    // (In the case where it is present, we could still theoretically
    // cache the certificate and re-verify it and at least avoid touching the image
    // again, however this path is not implemented yet, so this is TBD if we decide
    // it is an important case to optimize for.)

    if (!HasSecurityDirectory())
    {
        LOG((LF_SECURITY, LL_INFO1000, "No certificates found in module\n"));
    }
    else if(g_pConfig->GeneratePublisherEvidence())
    {
        // <TODO>@todo: Just because we don't have a file path, doesn't mean we can't have a certicate (does it?)</TODO>
        if (!GetPath().IsEmpty())
        {
            GCX_PREEMP();

            // Ignore any errors here - if we fail to validate a certificate, we just don't
            // include it as evidence.

            DWORD size;
            CoTaskNewHolder<COR_TRUST> pCor = NULL;
            // Failing to find a signature is OK.
            LPWSTR pFileName = (LPWSTR) GetPath().GetUnicode();
            DWORD dwAuthFlags = COR_NOUI|COR_NOPOLICY;
#ifndef FEATURE_CORECLR
            // Authenticode Verification Start
            FireEtwAuthenticodeVerificationStart_V1(dwAuthFlags, 0, pFileName, GetClrInstanceId());            
#endif // !FEATURE_CORECLR

            HRESULT hr = ::GetPublisher(pFileName,
                                          NULL,
                                          dwAuthFlags,
                                          &pCor,
                                          &size);

#ifndef FEATURE_CORECLR
            // Authenticode Verification End
            FireEtwAuthenticodeVerificationStop_V1(dwAuthFlags, (ULONG)hr, pFileName, GetClrInstanceId());            
#endif // !FEATURE_CORECLR

            if( SUCCEEDED(hr) ) { 
                DWORD index = 0;
                EnumCertificateAdditionFlags dwFlags = g_pCertificateCache->AddEntry(pCor, &index);
                switch (dwFlags) {
                case CacheSaturated:
                    pCor.SuppressRelease();
                    m_certificate = pCor.GetValue();
                    break;

                case Success:
                    pCor.SuppressRelease();
                    // falling through
                case AlreadyExists:
                    m_certificate = g_pCertificateCache->GetEntry(index);
                    _ASSERTE(m_certificate);
                    break;
                }
            }
        }
    }
    else 
    {
        LOG((LF_SECURITY, LL_INFO1000, "Assembly has an Authenticode signature, but Publisher evidence has been disabled.\n"));
    }

    m_fCheckedCertificate = TRUE;
}

HRESULT STDMETHODCALLTYPE
GetPublisher(__in __in_z IN LPWSTR pwsFileName,      // File name, this is required even with the handle
             IN HANDLE hFile,            // Optional file name
             IN DWORD  dwFlags,          // COR_NOUI or COR_NOPOLICY
             OUT PCOR_TRUST *pInfo,      // Returns a PCOR_TRUST (Use FreeM)
             OUT DWORD      *dwInfo)     // Size of pInfo.                           
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    
    GUID gV2 = COREE_POLICY_PROVIDER;
    COR_POLICY_PROVIDER sCorPolicy;
    
    WINTRUST_DATA      sWTD;
    WINTRUST_FILE_INFO sWTFI;
    
    // Set up the COR trust provider
    memset(&sCorPolicy, 0, sizeof(COR_POLICY_PROVIDER));
    sCorPolicy.cbSize = sizeof(COR_POLICY_PROVIDER);
    
    // Set up the winverify provider structures
    memset(&sWTD, 0x00, sizeof(WINTRUST_DATA));
    memset(&sWTFI, 0x00, sizeof(WINTRUST_FILE_INFO));
    
    sWTFI.cbStruct      = sizeof(WINTRUST_FILE_INFO);
    sWTFI.hFile         = hFile;
    sWTFI.pcwszFilePath = pwsFileName;
    
    sWTD.cbStruct       = sizeof(WINTRUST_DATA);
    sWTD.pPolicyCallbackData = &sCorPolicy; // Add in the cor trust information!!
    if (dwFlags & COR_NOUI)
    {
        sWTD.dwUIChoice     = WTD_UI_NONE;        // No bad UI is overridden in COR TRUST provider
    }
    else
    {
        sWTD.dwUIChoice     = WTD_UI_ALL;        // No bad UI is overridden in COR TRUST provider
    }
    sWTD.dwUnionChoice  = WTD_CHOICE_FILE;
    sWTD.pFile          = &sWTFI;
    
    // Set the policies for the VM (we have stolen VMBased and use it like a flag)
    if (dwFlags != 0)
        sCorPolicy.VMBased = dwFlags;
    
    LeaveRuntimeHolder holder((size_t)WinVerifyTrust);
    
    // WinVerifyTrust calls mscorsecimpl.dll to do the policy check
    hr = WinVerifyTrust(GetFocus(), &gV2, &sWTD);
    
    *pInfo  = sCorPolicy.pbCorTrust;
    *dwInfo = sCorPolicy.cbCorTrust;
    
    return hr;
} // GetPublisher

#endif // FEATURE_CAS_POLICY

// ------------------------------------------------------------
// PE file access
// ------------------------------------------------------------

// Note that most of these APIs are currently passed through
// to the main image.  However, in the near future they will
// be rerouted to the native image in the prejitted case so
// we can avoid using the original IL image.

#endif //!DACCESS_COMPILE

#ifdef FEATURE_PREJIT
#ifndef DACCESS_COMPILE
// ------------------------------------------------------------
// Native image access
// ------------------------------------------------------------

void PEFile::SetNativeImage(PEImage *image)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(!HasNativeImage());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    _ASSERTE(image != NULL);
    PREFIX_ASSUME(image != NULL);

    if (image->GetLoadedLayout()->GetBase() != image->GetLoadedLayout()->GetPreferredBase())
    {
        ExternalLog(LL_WARNING,
                    W("Native image loaded at base address") LFMT_ADDR
                    W("rather than preferred address:") LFMT_ADDR ,
                    DBG_ADDR(image->GetLoadedLayout()->GetBase()),
                    DBG_ADDR(image->GetLoadedLayout()->GetPreferredBase()));
    }

#ifdef FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
    // In Apollo, first ask if we're supposed to be ignoring the prejitted code &
    // structures in NGENd images. If so, bail now and do not set m_nativeImage. We've
    // already set m_identity & m_openedILimage (possibly even pointing to the
    // NGEN/Triton image), and will use those PEImages to find and JIT IL (even if they
    // point to an NGENd/Tritonized image).
    if (ShouldTreatNIAsMSIL())
        RETURN;
#endif

    m_nativeImage = image;
    m_nativeImage->AddRef();
    m_nativeImage->Load();
    m_nativeImage->AllocateLazyCOWPages();

#if defined(_TARGET_AMD64_) && !defined(CROSSGEN_COMPILE)
    static ConfigDWORD configNGenReserveForJumpStubs;
    int percentReserveForJumpStubs = configNGenReserveForJumpStubs.val(CLRConfig::INTERNAL_NGenReserveForJumpStubs);
    if (percentReserveForJumpStubs != 0)
    {
        PEImageLayout * pLayout = image->GetLoadedLayout();
        ExecutionManager::GetEEJitManager()->EnsureJumpStubReserve((BYTE *)pLayout->GetBase(), pLayout->GetVirtualSize(),
            percentReserveForJumpStubs * (pLayout->GetVirtualSize() / 100));
    }
#endif

    ExternalLog(LL_INFO100, W("Attempting to use native image %s."), image->GetPath().GetUnicode());
    RETURN;
}

void PEFile::ClearNativeImage()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeImage());
        POSTCONDITION(!HasNativeImage());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    ExternalLog(LL_WARNING, "Discarding native image.");


    MarkNativeImageInvalidIfOwned();

    {
        GCX_PREEMP();
        SafeComHolderPreemp<IMDInternalImport> pOldImport=GetMDImportWithRef();
        SimpleWriteLockHolder lock(m_pMetadataLock);

        EX_TRY
        {
            ReleaseMetadataInterfaces(FALSE);
            m_flags &= ~PEFILE_HAS_NATIVE_IMAGE_METADATA;
            if (m_nativeImage)
                m_nativeImage->Release();
            m_nativeImage = NULL;
            // Make sure our normal image is open
            EnsureImageOpened();

            // Reopen metadata from normal image
            OpenMDImport();
        }
        EX_HOOK
        {
            RestoreMDImport(pOldImport);
        }
        EX_END_HOOK;
    }

    RETURN;
}


extern DWORD g_dwLogLevel;

//===========================================================================================================
// Encapsulates CLR and Fusion logging for runtime verification of native images.
//===========================================================================================================
static void RuntimeVerifyVLog(DWORD level, LoggableAssembly *pLogAsm, const WCHAR *fmt, va_list args)
{
    STANDARD_VM_CONTRACT;

    BOOL fOutputToDebugger = (level == LL_ERROR && IsDebuggerPresent());
    BOOL fOutputToLogging = LoggingOn(LF_ZAP, level);

    StackSString message;
    message.VPrintf(fmt, args);

    if (fOutputToLogging)
    {
        SString displayString = pLogAsm->DisplayString();
        LOG((LF_ZAP, level, "%s: \"%S\"\n", "ZAP", displayString.GetUnicode()));
        LOG((LF_ZAP, level, "%S", message.GetUnicode()));
        LOG((LF_ZAP, level, "\n"));
    }

    if (fOutputToDebugger)
    {
        SString displayString = pLogAsm->DisplayString();
        WszOutputDebugString(W("CLR:("));
        WszOutputDebugString(displayString.GetUnicode());
        WszOutputDebugString(W(") "));
        WszOutputDebugString(message);
        WszOutputDebugString(W("\n"));
    }

#ifdef FEATURE_FUSION
    IFusionBindLog *pFusionBindLog = pLogAsm->FusionBindLog();
    if (pFusionBindLog)
    {
        pFusionBindLog->LogMessage(0, FUSION_BIND_LOG_CATEGORY_NGEN, message);

        if (level == LL_ERROR) {
            pFusionBindLog->SetResultCode(FUSION_BIND_LOG_CATEGORY_NGEN, E_FAIL);
            pFusionBindLog->Flush(g_dwLogLevel, FUSION_BIND_LOG_CATEGORY_NGEN);
            pFusionBindLog->Flush(g_dwLogLevel, FUSION_BIND_LOG_CATEGORY_DEFAULT);
        }
    }
#endif //FEATURE_FUSION
}


//===========================================================================================================
// Encapsulates CLR and Fusion logging for runtime verification of native images.
//===========================================================================================================
static void RuntimeVerifyLog(DWORD level, LoggableAssembly *pLogAsm, const WCHAR *fmt, ...)
{
    STANDARD_VM_CONTRACT;

    // Avoid calling RuntimeVerifyVLog unless logging is on
    if (   ((level == LL_ERROR) && IsDebuggerPresent()) 
        || LoggingOn(LF_ZAP, level)
#ifdef FEATURE_FUSION
        || (pLogAsm->FusionBindLog() != NULL)
#endif
       ) 
    {
        va_list args;
        va_start(args, fmt);

        RuntimeVerifyVLog(level, pLogAsm, fmt, args);

        va_end(args);
    }
}

//==============================================================================

static const LPCWSTR CorCompileRuntimeDllNames[NUM_RUNTIME_DLLS] =
{
#ifdef FEATURE_CORECLR
    MAKEDLLNAME_W(W("CORECLR"))
#else
    MAKEDLLNAME_W(W("CLR")),
    MAKEDLLNAME_W(W("CLRJIT"))
#endif
};

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
static LPCWSTR s_ngenCompilerDllName = NULL;
#endif //!FEATURE_CORECLR && !CROSSGEN_COMPILE

LPCWSTR CorCompileGetRuntimeDllName(CorCompileRuntimeDlls id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
    if (id == NGEN_COMPILER_INFO)
    {
        // The NGen compiler needs to be handled differently as it can be customized,
        // unlike the other runtime DLLs.

        if (s_ngenCompilerDllName == NULL)
        {
            // Check if there is an override for the compiler DLL
            LPCWSTR ngenCompilerOverride = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_NGen_JitName);

            if (ngenCompilerOverride == NULL)
            {
                s_ngenCompilerDllName = DEFAULT_NGEN_COMPILER_DLL_NAME;
            }
            else
            {
                if (wcsstr(ngenCompilerOverride, W(".dll")) == NULL)
                {
                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE,
                        NGEN_COMPILER_OVERRIDE_KEY W(" should have a .DLL suffix"));
                }

                s_ngenCompilerDllName = ngenCompilerOverride;
            }
        }

        return s_ngenCompilerDllName;
    }
#endif //!FEATURE_CORECLR && !CROSSGEN_COMPILE

    return CorCompileRuntimeDllNames[id];
}

#ifndef CROSSGEN_COMPILE

//==============================================================================
// Will always return a valid HMODULE for CLR_INFO, but will return NULL for NGEN_COMPILER_INFO
// if the DLL has not yet been loaded (it does not try to cause a load).

// Gets set by IJitManager::LoadJit (yes, this breaks the abstraction boundary).
HMODULE s_ngenCompilerDll = NULL;

extern HMODULE CorCompileGetRuntimeDll(CorCompileRuntimeDlls id)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Currently special cased for every entry.
#ifdef FEATURE_CORECLR
    static_assert_no_msg(NUM_RUNTIME_DLLS == 1);
    static_assert_no_msg(CORECLR_INFO == 0);
#else // !FEATURE_CORECLR
    static_assert_no_msg(NUM_RUNTIME_DLLS == 2);
    static_assert_no_msg(CLR_INFO == 0);
    static_assert_no_msg(NGEN_COMPILER_INFO == 1);
#endif // else FEATURE_CORECLR

    HMODULE hMod = NULL;

    // Try to load the correct DLL
    switch (id)
    {
#ifdef FEATURE_CORECLR
    case CORECLR_INFO:
        hMod = GetCLRModule();
        break;
#else // !FEATURE_CORECLR
    case CLR_INFO:
        hMod = GetCLRModule();
        break;

    case NGEN_COMPILER_INFO:
        hMod = s_ngenCompilerDll;
        break;
#endif // else FEATURE_CORECLR

    default:
        COMPlusThrowNonLocalized(kExecutionEngineException,
            W("Invalid runtime DLL ID"));
        break;
    }

    return hMod;
}
#endif // CROSSGEN_COMPILE

//===========================================================================================================
// Helper for RuntimeVerifyNativeImageVersion(). Compares the loaded clr.dll and clrjit.dll's against
// the ones the native image was compiled against.
//===========================================================================================================
static BOOL RuntimeVerifyNativeImageTimestamps(const CORCOMPILE_VERSION_INFO *info, LoggableAssembly *pLogAsm)
{
    STANDARD_VM_CONTRACT;

#if !defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
    //
    // We will automatically fail any zap files which were compiled with different runtime dlls.
    // This is so that we don't load bad ngen images after recompiling or patching the runtime.
    //

    for (DWORD index = 0; index < NUM_RUNTIME_DLLS; index++)
    {
        HMODULE hMod = CorCompileGetRuntimeDll((CorCompileRuntimeDlls)index);

        if (hMod == NULL)
        {
            // Unless this is an NGen worker process, we don't want to load JIT compiler just to do a timestamp check.
            // In an ideal case, all assemblies have native images, and JIT compiler never needs to be loaded at runtime.
            // Loading JIT compiler just to check its timestamp would reduce the benefits of have native images.
            // Since CLR and JIT are intended to be serviced together, the possibility of accidentally using native
            // images created by an older JIT is very small, and is deemed an acceptable risk.
            // Note that when multiple JIT compilers are used (e.g., clrjit.dll and compatjit.dll on x64 in .NET 4.6),
            // they must all be in the same patch family.
            if (!IsCompilationProcess())
                continue;

            // If we are doing ngen, then eagerly make sure all the system
            // dependencies are loaded. Else ICorCompileInfo::CheckAssemblyZap()
            // will not work correctly.

            LPCWSTR wszDllName = CorCompileGetRuntimeDllName((CorCompileRuntimeDlls)index);
            if (FAILED(g_pCLRRuntime->LoadLibrary(wszDllName, &hMod)))
            {
                EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Unable to load CLR DLL during ngen"));
            }
        }

        _ASSERTE(hMod != NULL);

        PEDecoder pe(hMod);

        // Match NT header timestamp and checksum to test DLL identity

        if ((info->runtimeDllInfo[index].timeStamp == pe.GetTimeDateStamp()
             || info->runtimeDllInfo[index].timeStamp == 0)
            && (info->runtimeDllInfo[index].virtualSize == pe.GetVirtualSize()
                || info->runtimeDllInfo[index].virtualSize == 0))
        {
            continue;
        }

        {
            // set "ComPlus_CheckNGenImageTimeStamp" to 0 to ignore time-stamp-checking
            static ConfigDWORD checkNGenImageTimeStamp;
            BOOL enforceCheck = checkNGenImageTimeStamp.val(CLRConfig::EXTERNAL_CheckNGenImageTimeStamp);

            RuntimeVerifyLog(enforceCheck ? LL_ERROR : LL_WARNING,
                             pLogAsm,
                             W("Compiled with different CLR DLL (%s). Exact match expected."),
                             CorCompileGetRuntimeDllName((CorCompileRuntimeDlls)index));

            if (enforceCheck)
                return FALSE;
        }
    }
#endif // !CROSSGEN_COMPILE && !FEATURE_CORECLR

    return TRUE;
}

//===========================================================================================================
// Validates that an NI matches the running CLR, OS, CPU, etc. This is the entrypoint used by the CLR loader.
//
//===========================================================================================================
BOOL PEAssembly::CheckNativeImageVersion(PEImage *peimage)
{
    STANDARD_VM_CONTRACT;

    //
    // Get the zap version header. Note that modules will not have version
    // headers - they add no additional versioning constraints from their
    // assemblies.
    //
    PEImageLayoutHolder image=peimage->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED);

    if (!image->HasNativeHeader())
        return FALSE;

    if (!image->CheckNativeHeaderVersion())
    {
#ifdef FEATURE_CORECLR
        // Wrong native image version is fatal error on CoreCLR
        ThrowHR(COR_E_NI_AND_RUNTIME_VERSION_MISMATCH);
#else
        return FALSE;
#endif
    }

    CORCOMPILE_VERSION_INFO *info = image->GetNativeVersionInfo();
    if (info == NULL)
        return FALSE;

    LoggablePEAssembly logAsm(this);
    if (!RuntimeVerifyNativeImageVersion(info, &logAsm))
    {
#ifdef FEATURE_CORECLR
        // Wrong native image version is fatal error on CoreCLR
        ThrowHR(COR_E_NI_AND_RUNTIME_VERSION_MISMATCH);
#else
        return FALSE;
#endif
    }

#ifdef FEATURE_CORECLR
    CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();

    if (IsSystem())
    {
        // Require instrumented flags for mscorlib when collecting IBC data
        CorCompileConfigFlags instrumentationConfigFlags = (CorCompileConfigFlags) (configFlags & CORCOMPILE_CONFIG_INSTRUMENTATION);
        if ((info->wConfigFlags & instrumentationConfigFlags) != instrumentationConfigFlags)
        {
            ExternalLog(LL_ERROR, "Instrumented native image for Mscorlib.dll expected.");
            ThrowHR(COR_E_NI_AND_RUNTIME_VERSION_MISMATCH);
        }
    }

    // Otherwise, match regardless of the instrumentation flags
    configFlags = (CorCompileConfigFlags) (configFlags & ~(CORCOMPILE_CONFIG_INSTRUMENTATION_NONE | CORCOMPILE_CONFIG_INSTRUMENTATION));

    if ((info->wConfigFlags & configFlags) != configFlags)
    {
        return FALSE;
    }
#else
    //
    // Check image flavor. Skip this check in RuntimeVerifyNativeImageVersion called from fusion - fusion is responsible for choosing the right flavor.
    //
    if (!RuntimeVerifyNativeImageFlavor(info, &logAsm))
    {
        return FALSE;
    }
#endif

    return TRUE;
}

#ifndef FEATURE_CORECLR
//===========================================================================================================
// Validates that an NI matches the required flavor (debug, instrumented, etc.)
//
//===========================================================================================================
BOOL RuntimeVerifyNativeImageFlavor(const CORCOMPILE_VERSION_INFO *info, LoggableAssembly *pLogAsm)
{
    STANDARD_VM_CONTRACT;

    CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();

    if ((info->wConfigFlags & configFlags) != configFlags)
        return FALSE;

    return TRUE;
}
#endif

//===========================================================================================================
// Validates that an NI matches the running CLR, OS, CPU, etc.
//
// For historial reasons, some versions of the runtime perform this check at native bind time (preferrred),
// while others check at CLR load time.
//
// This is the common funnel for both versions and is agnostic to whether the "assembly" is represented
// by a CLR object or Fusion object.
//===========================================================================================================
BOOL RuntimeVerifyNativeImageVersion(const CORCOMPILE_VERSION_INFO *info, LoggableAssembly *pLogAsm)
{
    STANDARD_VM_CONTRACT;

    if (!RuntimeVerifyNativeImageTimestamps(info, pLogAsm))
        return FALSE;

    //
    // Check that the EE version numbers are the same.
    //
 
    if (info->wVersionMajor != VER_MAJORVERSION
        || info->wVersionMinor != VER_MINORVERSION
        || info->wVersionBuildNumber != VER_PRODUCTBUILD
        || info->wVersionPrivateBuildNumber != VER_PRODUCTBUILD_QFE)
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("CLR version recorded in native image doesn't match the current CLR."));
        return FALSE;
    }

    //
    // Check checked/free status
    //

    if (info->wBuild !=
#if _DEBUG
        CORCOMPILE_BUILD_CHECKED
#else
        CORCOMPILE_BUILD_FREE
#endif
        )
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("Checked/free mismatch with native image."));
        return FALSE;
    }

    //
    // Check processor
    //

    if (info->wMachine != IMAGE_FILE_MACHINE_NATIVE_NI)
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("Processor type recorded in native image doesn't match this machine's processor."));
        return FALSE;
    }

#ifndef CROSSGEN_COMPILE
    //
    // Check the processor specific ID
    //

    CORINFO_CPU cpuInfo;
    GetSpecificCpuInfo(&cpuInfo);

    if (!IsCompatibleCpuInfo(&cpuInfo, &info->cpuInfo))
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("Required CPU features recorded in native image don't match this machine's processor."));
        return FALSE;
    }
#endif // CROSSGEN_COMPILE

#if defined(_TARGET_AMD64_) && !defined(FEATURE_CORECLR)
    //
    // Check the right JIT compiler
    //

    bool nativeImageBuiltWithRyuJit = ((info->wCodegenFlags & CORCOMPILE_CODEGEN_USE_RYUJIT) != 0);
    if (UseRyuJit() != nativeImageBuiltWithRyuJit)
    {
        RuntimeVerifyLog(LL_ERROR, pLogAsm, W("JIT compiler used to generate native image doesn't match current JIT compiler."));
        return FALSE;
    }
#endif

    //
    // The zap is up to date.
    //

    RuntimeVerifyLog(LL_INFO100, pLogAsm, W("Native image has correct version information."));
    return TRUE;
}

#endif // !DACCESS_COMPILE

/* static */
CorCompileConfigFlags PEFile::GetNativeImageConfigFlags(BOOL fForceDebug/*=FALSE*/,
                                                        BOOL fForceProfiling/*=FALSE*/,
                                                        BOOL fForceInstrument/*=FALSE*/)
{
    LIMITED_METHOD_DAC_CONTRACT;

    CorCompileConfigFlags result = (CorCompileConfigFlags)0;

    // Debugging

#ifdef DEBUGGING_SUPPORTED
    // if these have been set, the take precedence over anything else
    if (s_NGENDebugFlags)
    {
        if ((s_NGENDebugFlags & CORCOMPILE_CONFIG_DEBUG_NONE) != 0)
        {
            result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG_NONE);
        }
        else
        {
            if ((s_NGENDebugFlags & CORCOMPILE_CONFIG_DEBUG) != 0)
            {
                result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG);
            }
        }
    }
    else
#endif // DEBUGGING_SUPPORTED
    {
        if (fForceDebug)
        {
            result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG);
        }
        else
        {
            result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_DEBUG_DEFAULT);
        }
    }

    // Profiling

#ifdef PROFILING_SUPPORTED
    if (fForceProfiling || CORProfilerUseProfileImages())
    {
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_PROFILING);

        result = (CorCompileConfigFlags) (result & ~(CORCOMPILE_CONFIG_DEBUG_NONE|
                                                     CORCOMPILE_CONFIG_DEBUG|
                                                     CORCOMPILE_CONFIG_DEBUG_DEFAULT));
    }
    else
#endif //PROFILING_SUPPORTED
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_PROFILING_NONE);

    // Instrumentation
#ifndef DACCESS_COMPILE
    BOOL instrumented = (!IsCompilationProcess() && g_pConfig->GetZapBBInstr());
#else
    BOOL instrumented = FALSE;
#endif
    if (instrumented || fForceInstrument)
    {
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_INSTRUMENTATION);
    }
    else
    {
        result = (CorCompileConfigFlags) (result|CORCOMPILE_CONFIG_INSTRUMENTATION_NONE);
    }

    // NOTE: Right now we are not taking instrumentation into account when binding.

    return result;
}

CorCompileConfigFlags PEFile::GetNativeImageConfigFlagsWithOverrides()
{
    LIMITED_METHOD_DAC_CONTRACT;

    BOOL fForceDebug, fForceProfiling, fForceInstrument;
    SystemDomain::GetCompilationOverrides(&fForceDebug,
                                          &fForceProfiling,
                                          &fForceInstrument);
    return PEFile::GetNativeImageConfigFlags(fForceDebug,
                                             fForceProfiling,
                                             fForceInstrument);
}

#ifndef DACCESS_COMPILE



//===========================================================================================================
// Validates that a hard-dep matches the a parent NI's compile-time hard-dep.
//
// For historial reasons, some versions of the runtime perform this check at native bind time (preferrred),
// while others check at CLR load time.
//
// This is the common funnel for both versions and is agnostic to whether the "assembly" is represented
// by a CLR object or Fusion object.
//
//===========================================================================================================
BOOL RuntimeVerifyNativeImageDependency(const CORCOMPILE_NGEN_SIGNATURE &ngenSigExpected,
                                        const CORCOMPILE_VERSION_INFO *pActual,
                                        LoggableAssembly              *pLogAsm)
{
    STANDARD_VM_CONTRACT;

    if (ngenSigExpected != pActual->signature)
    {
        // Signature did not match
        SString displayString = pLogAsm->DisplayString();
        RuntimeVerifyLog(LL_ERROR,
                         pLogAsm,
                         W("Rejecting native image because native image dependency %s ")
                         W("had a different identity than expected"),
                         displayString.GetUnicode());
#if (defined FEATURE_PREJIT) && (defined FEATURE_FUSION)
        if (pLogAsm->FusionBindLog())
        {
            if (ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, TRACE_LEVEL_INFORMATION, CLR_PRIVATEFUSION_KEYWORD))
            { 
                pLogAsm->FusionBindLog()->ETWTraceLogMessage(ETW::BinderLog::BinderStructs::NGEN_BIND_DEPENDENCY_HAS_DIFFERENT_IDENTITY, pLogAsm->FusionAssemblyName());
            }
        }
#endif

        return FALSE;
    }
    return TRUE;
}
// Wrapper function for use by parts of the runtime that actually have a CORCOMPILE_DEPENDENCY to work with.
BOOL RuntimeVerifyNativeImageDependency(const CORCOMPILE_DEPENDENCY   *pExpected,
                                        const CORCOMPILE_VERSION_INFO *pActual,
                                        LoggableAssembly              *pLogAsm)
{
    WRAPPER_NO_CONTRACT;

    return RuntimeVerifyNativeImageDependency(pExpected->signNativeImage,
                                              pActual,
                                              pLogAsm);
}

#endif // !DACCESS_COMPILE

#ifdef DEBUGGING_SUPPORTED
//
// Called through ICorDebugAppDomain2::SetDesiredNGENCompilerFlags to specify
// which kinds of ngen'd images fusion should load wrt debugging support
// Overrides any previous settings
//
void PEFile::SetNGENDebugFlags(BOOL fAllowOpt)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (fAllowOpt)
        s_NGENDebugFlags = CORCOMPILE_CONFIG_DEBUG_NONE;
    else
        s_NGENDebugFlags = CORCOMPILE_CONFIG_DEBUG;
    }

//
// Called through ICorDebugAppDomain2::GetDesiredNGENCompilerFlags to determine
// which kinds of ngen'd images fusion should load wrt debugging support
//
void PEFile::GetNGENDebugFlags(BOOL *fAllowOpt)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();

    *fAllowOpt = ((configFlags & CORCOMPILE_CONFIG_DEBUG) == 0);
}
#endif // DEBUGGING_SUPPORTED



#ifndef DACCESS_COMPILE
#ifdef FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS

//---------------------------------------------------------------------------------------
//
// Used in Apollo, this method determines whether profiling or debugging has requested
// the runtime to provide debuggable / profileable code. In other CLR builds, this would
// normally result in requiring the appropriate NGEN scenario be loaded (/Debug or
// /Profile) and to JIT if unavailable. In Apollo, however, these NGEN scenarios are
// never available, and even MSIL assemblies are often not available. So this function
// tells its caller to use the NGENd assembly as if it were an MSIL assembly--ignore the
// prejitted code and prebaked structures, and just JIT code and load classes from
// scratch.
//
// Return Value:
//      nonzero iff NGENd images should be treated as MSIL images.
//

// static
BOOL PEFile::ShouldTreatNIAsMSIL()
{
    LIMITED_METHOD_CONTRACT;

    // Ask profiling API & config vars whether NGENd images should be avoided
    // completely.
    if (!NGENImagesAllowed())
        return TRUE;

    // Ask profiling and debugging if they're requesting us to use ngen /Debug or
    // /Profile images (which aren't available under Apollo)

    CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();

    if ((configFlags & (CORCOMPILE_CONFIG_DEBUG | CORCOMPILE_CONFIG_PROFILING)) != 0)
        return TRUE;

    return FALSE;
}

#endif // FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS

#endif  //!DACCESS_COMPILE
#endif  // FEATURE_PREJIT

#ifndef DACCESS_COMPILE

// ------------------------------------------------------------
// Resource access
// ------------------------------------------------------------

void PEFile::GetEmbeddedResource(DWORD dwOffset, DWORD *cbResource, PBYTE *pbInMemoryResource)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory(););
    }
    CONTRACTL_END;

    // NOTE: it's not clear whether to load this from m_image or m_loadedImage.
    // m_loadedImage is probably preferable, but this may be called by security
    // before the image is loaded.

    PEImage *image;

#ifdef FEATURE_PREJIT
    if (m_nativeImage != NULL)
        image = m_nativeImage;
    else 
#endif    
    {
        EnsureImageOpened();
        image = GetILimage();
    }

    PEImageLayoutHolder theImage(image->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED));
    if (!theImage->CheckResource(dwOffset))
        ThrowHR(COR_E_BADIMAGEFORMAT);

    COUNT_T size;
    const void *resource = theImage->GetResource(dwOffset, &size);

    *cbResource = size;
    *pbInMemoryResource = (PBYTE) resource;
}

// ------------------------------------------------------------
// File loading
// ------------------------------------------------------------

PEAssembly * 
PEFile::LoadAssembly(
    mdAssemblyRef       kAssemblyRef,
    IMDInternalImport * pImport,                // = NULL
    LPCUTF8             szWinRtTypeNamespace,   // = NULL
    LPCUTF8             szWinRtTypeClassName)   // = NULL
{
    CONTRACT(PEAssembly *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (pImport == NULL)
        pImport = GetPersistentMDImport();

    if (((TypeFromToken(kAssemblyRef) != mdtAssembly) && 
         (TypeFromToken(kAssemblyRef) != mdtAssemblyRef)) || 
        (!pImport->IsValidToken(kAssemblyRef)))
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }
    
    AssemblySpec spec;
    
    spec.InitializeSpec(kAssemblyRef, pImport, GetAppDomain()->FindAssembly(GetAssembly()), IsIntrospectionOnly());
    if (szWinRtTypeClassName != NULL)
        spec.SetWindowsRuntimeType(szWinRtTypeNamespace, szWinRtTypeClassName);
    
    RETURN GetAppDomain()->BindAssemblySpec(&spec, TRUE, IsIntrospectionOnly());
}

// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------
#ifdef FEATURE_PREJIT
void PEFile::ExternalLog(DWORD facility, DWORD level, const WCHAR *fmt, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, fmt);

    ExternalVLog(facility, level, fmt, args);

    va_end(args);
}

void PEFile::ExternalLog(DWORD level, const WCHAR *fmt, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, fmt);

    ExternalVLog(LF_ZAP, level, fmt, args);

    va_end(args);
}

void PEFile::ExternalLog(DWORD level, const char *msg)
{
    WRAPPER_NO_CONTRACT;

    // It is OK to use %S here. We know that msg is ASCII-only.
    ExternalLog(level, W("%S"), msg);
}

void PEFile::ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    BOOL fOutputToDebugger = (level == LL_ERROR && IsDebuggerPresent());
    BOOL fOutputToLogging = LoggingOn(facility, level);

    if (!fOutputToDebugger && !fOutputToLogging)
        return;

    StackSString message;
    message.VPrintf(fmt, args);

    if (fOutputToLogging)
    {
        if (GetMDImport() != NULL)
            LOG((facility, level, "%s: \"%s\"\n", (facility == LF_ZAP ? "ZAP" : "LOADER"), GetSimpleName()));
        else
            LOG((facility, level, "%s: \"%S\"\n", (facility == LF_ZAP ? "ZAP" : "LOADER"), ((const WCHAR *)GetPath())));

        LOG((facility, level, "%S", message.GetUnicode()));
        LOG((facility, level, "\n"));
    }

    if (fOutputToDebugger)
    {
        WszOutputDebugString(W("CLR:("));

        StackSString codebase;
        GetCodeBaseOrName(codebase);
        WszOutputDebugString(codebase);

        WszOutputDebugString(W(") "));

        WszOutputDebugString(message);
        WszOutputDebugString(W("\n"));
    }

    RETURN;
}

void PEFile::FlushExternalLog()
{
    LIMITED_METHOD_CONTRACT;
}
#endif

BOOL PEFile::GetResource(LPCSTR szName, DWORD *cbResource,
                                 PBYTE *pbInMemoryResource, DomainAssembly** pAssemblyRef,
                                 LPCSTR *szFileName, DWORD *dwLocation,
                                 StackCrawlMark *pStackMark, BOOL fSkipSecurityCheck,
                                 BOOL fSkipRaiseResolveEvent, DomainAssembly* pDomainAssembly, AppDomain* pAppDomain)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;


    mdToken            mdLinkRef;
    DWORD              dwResourceFlags;
    DWORD              dwOffset;
    mdManifestResource mdResource;
    Assembly*          pAssembly = NULL;
    PEFile*            pPEFile = NULL;
    ReleaseHolder<IMDInternalImport> pImport (GetMDImportWithRef());
    if (SUCCEEDED(pImport->FindManifestResourceByName(szName, &mdResource)))
    {
        pPEFile = this;
        IfFailThrow(pImport->GetManifestResourceProps(
            mdResource, 
            NULL,           //&szName,
            &mdLinkRef, 
            &dwOffset, 
            &dwResourceFlags));
    }
    else
    {
        if (fSkipRaiseResolveEvent || pAppDomain == NULL)
            return FALSE;

        DomainAssembly* pParentAssembly = GetAppDomain()->FindAssembly(GetAssembly());
        pAssembly = pAppDomain->RaiseResourceResolveEvent(pParentAssembly, szName);
        if (pAssembly == NULL)
            return FALSE;

        pDomainAssembly = pAssembly->GetDomainAssembly(pAppDomain);
        pPEFile = pDomainAssembly->GetFile();

        if (FAILED(pAssembly->GetManifestImport()->FindManifestResourceByName(
            szName,
            &mdResource)))
        {
            return FALSE;
        }
        
        if (dwLocation != 0)
        {
            if (pAssemblyRef != NULL)
                *pAssemblyRef = pDomainAssembly;
            
            *dwLocation = *dwLocation | 2; // ResourceLocation.containedInAnotherAssembly
        }
        IfFailThrow(pPEFile->GetPersistentMDImport()->GetManifestResourceProps(
            mdResource, 
            NULL,           //&szName,
            &mdLinkRef, 
            &dwOffset, 
            &dwResourceFlags));
    }
    
    
    switch(TypeFromToken(mdLinkRef)) {
    case mdtAssemblyRef:
        {
            if (pDomainAssembly == NULL)
                return FALSE;

            AssemblySpec spec;
            spec.InitializeSpec(mdLinkRef, GetPersistentMDImport(), pDomainAssembly, pDomainAssembly->GetFile()->IsIntrospectionOnly());
            pDomainAssembly = spec.LoadDomainAssembly(FILE_LOADED);

            if (dwLocation) {
                if (pAssemblyRef)
                    *pAssemblyRef = pDomainAssembly;

                *dwLocation = *dwLocation | 2; // ResourceLocation.containedInAnotherAssembly
            }

            return pDomainAssembly->GetResource(szName,
                                                cbResource,
                                                pbInMemoryResource,
                                                pAssemblyRef,
                                                szFileName,
                                                dwLocation,
                                                pStackMark,
                                                fSkipSecurityCheck,
                                                fSkipRaiseResolveEvent);
        }

    case mdtFile:
        if (mdLinkRef == mdFileNil)
        {
            // The resource is embedded in the manifest file

#ifndef CROSSGEN_COMPILE
            if (!IsMrPublic(dwResourceFlags) && pStackMark && !fSkipSecurityCheck)
            {
                Assembly *pCallersAssembly = SystemDomain::GetCallersAssembly(pStackMark);

                if (pCallersAssembly &&  // full trust for interop
                    (!pCallersAssembly->GetManifestFile()->Equals(this)))
                {
                    RefSecContext sCtx(AccessCheckOptions::kMemberAccess);

                    AccessCheckOptions accessCheckOptions(
                        AccessCheckOptions::kMemberAccess,  /*accessCheckType*/
                        NULL,                               /*pAccessContext*/
                        FALSE,                              /*throwIfTargetIsInaccessible*/
                        (MethodTable *) NULL                /*pTargetMT*/
                        );

                    // SL: return TRUE only if the caller is critical
                    // Desktop: return TRUE only if demanding MemberAccess succeeds
                    if (!accessCheckOptions.DemandMemberAccessOrFail(&sCtx, NULL, TRUE /*visibilityCheck*/))
                        return FALSE;
                }
            }
#endif // CROSSGEN_COMPILE

            if (dwLocation) {
                *dwLocation = *dwLocation | 5; // ResourceLocation.embedded |

                                               // ResourceLocation.containedInManifestFile
                return TRUE;
            }

            pPEFile->GetEmbeddedResource(dwOffset, cbResource, pbInMemoryResource);

            return TRUE;
        }
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
        // The resource is either linked or embedded in a non-manifest-containing file
        if (pDomainAssembly == NULL)
            return FALSE;

        return pDomainAssembly->GetModuleResource(mdLinkRef, szName, cbResource,
                                                  pbInMemoryResource, szFileName,
                                                  dwLocation, IsMrPublic(dwResourceFlags),
                                                  pStackMark, fSkipSecurityCheck);
#else
        return FALSE;
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_IN_MANIFESTRES);
    }
}

void PEFile::GetPEKindAndMachine(DWORD* pdwKind, DWORD* pdwMachine)
{
    WRAPPER_NO_CONTRACT;

    if (IsResource() || IsDynamic())
    {
        if (pdwKind)
            *pdwKind = 0;
        if (pdwMachine)
            *pdwMachine = 0;
        return;
    }

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        m_nativeImage->GetNativeILPEKindAndMachine(pdwKind, pdwMachine);
        return;
    }
#ifndef DACCESS_COMPILE
    if (!HasOpenedILimage())
    {
        //don't want to touch the IL image unless we already have
        ReleaseHolder<PEImage> pNativeImage = GetNativeImageWithRef();
        if (pNativeImage)
        {
            pNativeImage->GetNativeILPEKindAndMachine(pdwKind, pdwMachine);
            return;
        }
    }
#endif // DACCESS_COMPILE        
#endif // FEATURE_PREJIT

    GetILimage()->GetPEKindAndMachine(pdwKind, pdwMachine);
    return;
}

ULONG PEFile::GetILImageTimeDateStamp()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    if (IsNativeLoaded())
    {
        CONSISTENCY_CHECK(HasNativeImage());

        // The IL image's time stamp is copied to the native image.
        CORCOMPILE_VERSION_INFO* pVersionInfo = GetLoadedNative()->GetNativeVersionInfoMaybeNull();
        if (pVersionInfo == NULL)
        {
            return 0;
        }
        else
        {
            return pVersionInfo->sourceAssembly.timeStamp;
        }
    }
#endif // FEATURE_PREJIT

    return GetLoadedIL()->GetTimeDateStamp();
}

#ifdef FEATURE_CAS_POLICY

//---------------------------------------------------------------------------------------
//
// Get a SafePEFileHandle for this PEFile
//

SAFEHANDLE PEFile::GetSafeHandle()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    SAFEHANDLE objSafeHandle = NULL;

    GCPROTECT_BEGIN(objSafeHandle);

    objSafeHandle = (SAFEHANDLE)AllocateObject(MscorlibBinder::GetClass(CLASS__SAFE_PEFILE_HANDLE));
    CallDefaultConstructor(objSafeHandle);

    this->AddRef();
    objSafeHandle->SetHandle(this);

    GCPROTECT_END();

    return objSafeHandle;
}

#endif // FEATURE_CAS_POLICY

// ================================================================================
// PEAssembly class - a PEFile which represents an assembly
// ================================================================================

// Statics initialization.
/* static */
void PEAssembly::Attach()
{
    STANDARD_VM_CONTRACT;
}

#ifdef FEATURE_FUSION
PEAssembly::PEAssembly(PEImage *image,
                       IMetaDataEmit *pEmit,
                       IAssembly *pIAssembly,
                       IBindResult *pNativeFusionAssembly,
                       PEImage *pPEImageNI,
                       IFusionBindLog *pFusionLog,
                       IHostAssembly *pIHostAssembly,
                       PEFile *creator,
                       BOOL system,
                       BOOL introspectionOnly/*=FALSE*/,
                       ICLRPrivAssembly * pHostAssembly)
      : PEFile(image, FALSE),
        m_creator(NULL),
        m_pFusionAssemblyName(NULL),
        m_pFusionAssembly(NULL),
        m_pFusionLog(NULL),
        m_bFusionLogEnabled(TRUE),
        m_pIHostAssembly(NULL),
        m_pNativeAssemblyLocation(NULL),
        m_pNativeImageClosure(NULL),
        m_fStrongNameBypassed(FALSE)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(image, NULL_OK));
        PRECONDITION(CheckPointer(pEmit, NULL_OK));
        PRECONDITION(image != NULL || pEmit != NULL);
        PRECONDITION(CheckPointer(pIAssembly, NULL_OK));
        PRECONDITION(CheckPointer(pFusionLog, NULL_OK));
        PRECONDITION(CheckPointer(pIHostAssembly, NULL_OK));
        PRECONDITION(CheckPointer(creator, NULL_OK));
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;    

    if (introspectionOnly)
    {
        if (!system)  // Implementation restriction: mscorlib.dll cannot be loaded as introspection. The architecture depends on there being exactly one mscorlib.
        {
            m_flags |= PEFILE_INTROSPECTIONONLY;
#ifdef FEATURE_PREJIT
            SetCannotUseNativeImage();
#endif // FEATURE_PREJIT
        }
    }

    if (pIAssembly)
    {
        m_pFusionAssembly = pIAssembly;
        pIAssembly->AddRef();

        IfFailThrow(pIAssembly->GetAssemblyNameDef(&m_pFusionAssemblyName));
    }
    else if (pIHostAssembly)
    {
        m_flags |= PEFILE_ISTREAM;
#ifdef FEATURE_PREJIT
        m_fCanUseNativeImage = FALSE;
#endif // FEATURE_PREJIT

        m_pIHostAssembly = pIHostAssembly;
        pIHostAssembly->AddRef();

        IfFailThrow(pIHostAssembly->GetAssemblyNameDef(&m_pFusionAssemblyName));
    }

    if (pFusionLog)
    {
        m_pFusionLog = pFusionLog;
        pFusionLog->AddRef();
    }

    if (creator)
    {
        m_creator = creator;
        creator->AddRef();
    }

    m_flags |= PEFILE_ASSEMBLY;
    if (system)
        m_flags |= PEFILE_SYSTEM;

#ifdef FEATURE_PREJIT
    // Find the native image
    if (pIAssembly)
    {
        if (pNativeFusionAssembly != NULL)
            SetNativeImage(pNativeFusionAssembly);
    }
    // Only one of pNativeFusionAssembly and pPEImageNI may be set.
    _ASSERTE(!(pNativeFusionAssembly && pPEImageNI));

    if (pPEImageNI != NULL)
        this->PEFile::SetNativeImage(pPEImageNI);
#endif  // FEATURE_PREJIT

    // If we have no native image, we require a mapping for the file.
    if (!HasNativeImage() || !IsILOnly())
        EnsureImageOpened();

    // Open metadata eagerly to minimize failure windows
    if (pEmit == NULL)
        OpenMDImport_Unsafe(); //constructor, cannot race with anything
    else
    {
        _ASSERTE(!m_bHasPersistentMDImport);
        IfFailThrow(GetMetaDataInternalInterfaceFromPublic(pEmit, IID_IMDInternalImport,
                                                           (void **)&m_pMDImport));
        m_pEmitter = pEmit;
        pEmit->AddRef();
        m_bHasPersistentMDImport=TRUE;
        m_MDImportIsRW_Debugger_Use_Only = TRUE;
    }

    // m_pMDImport can be external
    // Make sure this is an assembly
    if (!m_pMDImport->IsValidToken(TokenFromRid(1, mdtAssembly)))
        ThrowHR(COR_E_ASSEMBLYEXPECTED);

    // Make sure we perform security checks after we've obtained IMDInternalImport interface
    DoLoadSignatureChecks();

    // Verify name eagerly
    LPCUTF8 szName = GetSimpleName();
    if (!*szName)
    {
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_EMPTY_ASSEMDEF_NAME);
    }

#ifdef FEATURE_PREJIT
    if (IsResource() || IsDynamic())
        m_fCanUseNativeImage = FALSE;
#endif // FEATURE_PREJIT

    if (m_pFusionAssembly)
    {
        m_loadContext = m_pFusionAssembly->GetFusionLoadContext();
        m_pFusionAssembly->GetAssemblyLocation(&m_dwLocationFlags);
    }
    else if (pHostAssembly != nullptr)
    {
        m_loadContext = LOADCTX_TYPE_HOSTED;
        m_dwLocationFlags = ASMLOC_UNKNOWN;
        m_pHostAssembly = clr::SafeAddRef(pHostAssembly); // Should use SetHostAssembly(pHostAssembly) here
    }
    else
    {
        m_loadContext = LOADCTX_TYPE_UNKNOWN;
        m_dwLocationFlags = ASMLOC_UNKNOWN;
    }

    TESTHOOKCALL(CompletedNativeImageBind(image,szName,HasNativeImage()));

#if _DEBUG
    GetCodeBaseOrName(m_debugName);
    m_debugName.Normalize();
    m_pDebugName = m_debugName;
#endif
}

#else // FEATURE_FUSION

PEAssembly::PEAssembly(
                CoreBindResult* pBindResultInfo, 
                IMetaDataEmit* pEmit, 
                PEFile *creator, 
                BOOL system,
                BOOL introspectionOnly/*=FALSE*/
#ifdef FEATURE_HOSTED_BINDER
                ,
                PEImage * pPEImageIL /*= NULL*/,
                PEImage * pPEImageNI /*= NULL*/,
                ICLRPrivAssembly * pHostAssembly /*= NULL*/
#endif
                )

  : PEFile(pBindResultInfo ? (pBindResultInfo->GetPEImage() ? pBindResultInfo->GetPEImage() : 
                                                              (pBindResultInfo->HasNativeImage() ? pBindResultInfo->GetNativeImage() : NULL)
#ifdef FEATURE_HOSTED_BINDER
                              ): pPEImageIL? pPEImageIL:(pPEImageNI? pPEImageNI:NULL), FALSE),
#else
                              ): NULL, FALSE),
#endif
    m_creator(clr::SafeAddRef(creator)),
    m_bIsFromGAC(FALSE),
    m_bIsOnTpaList(FALSE)
#ifdef FEATURE_CORECLR
    ,m_fProfileAssembly(0)
#else
    ,m_fStrongNameBypassed(FALSE)
#endif
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        PRECONDITION(CheckPointer(pEmit, NULL_OK));
        PRECONDITION(CheckPointer(creator, NULL_OK));
#ifdef FEATURE_HOSTED_BINDER
        PRECONDITION(pBindResultInfo == NULL || (pPEImageIL == NULL && pPEImageNI == NULL));
#endif
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    if (introspectionOnly)
    {
        if (!system)  // Implementation restriction: mscorlib.dll cannot be loaded as introspection. The architecture depends on there being exactly one mscorlib.
        {
            m_flags |= PEFILE_INTROSPECTIONONLY;
        }
    }

    m_flags |= PEFILE_ASSEMBLY;
    if (system)
        m_flags |= PEFILE_SYSTEM;

    // We check the precondition above that either pBindResultInfo is null or both pPEImageIL and pPEImageNI are,
    // so we'll only get a max of one native image passed in.
#ifdef FEATURE_HOSTED_BINDER
    if (pPEImageNI != NULL)
    {
        SetNativeImage(pPEImageNI);
    }
#endif

#ifdef FEATURE_PREJIT
    if (pBindResultInfo && pBindResultInfo->HasNativeImage())
        SetNativeImage(pBindResultInfo->GetNativeImage());
#endif

    // If we have no native image, we require a mapping for the file.
    if (!HasNativeImage() || !IsILOnly())
        EnsureImageOpened();

    // Initialize the status of the assembly being in the GAC, or being part of the TPA list, before
    // we start to do work (like strong name verification) that relies on those states to be valid.
    if(pBindResultInfo != nullptr)
    {
        m_bIsFromGAC = pBindResultInfo->IsFromGAC();
        m_bIsOnTpaList = pBindResultInfo->IsOnTpaList();
    }

    // Check security related stuff
    VerifyStrongName();

    // Open metadata eagerly to minimize failure windows
    if (pEmit == NULL)
        OpenMDImport_Unsafe(); //constructor, cannot race with anything
    else
    {
        _ASSERTE(!m_bHasPersistentMDImport);
        IfFailThrow(GetMetaDataInternalInterfaceFromPublic(pEmit, IID_IMDInternalImport,
                                                           (void **)&m_pMDImport));
        m_pEmitter = pEmit;
        pEmit->AddRef();
        m_bHasPersistentMDImport=TRUE;
        m_MDImportIsRW_Debugger_Use_Only = TRUE;
    }

    // m_pMDImport can be external
    // Make sure this is an assembly
    if (!m_pMDImport->IsValidToken(TokenFromRid(1, mdtAssembly)))
        ThrowHR(COR_E_ASSEMBLYEXPECTED);

    // Verify name eagerly
    LPCUTF8 szName = GetSimpleName();
    if (!*szName)
    {
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_EMPTY_ASSEMDEF_NAME);
    }

#ifdef FEATURE_HOSTED_BINDER
    // Set the host assembly and binding context as the AssemblySpec initialization
    // for CoreCLR will expect to have it set.
    if (pHostAssembly != nullptr)
    {
        m_pHostAssembly = clr::SafeAddRef(pHostAssembly);
    }

    if(pBindResultInfo != nullptr)
    {
        // Cannot have both pHostAssembly and a coreclr based bind
        _ASSERTE(pHostAssembly == nullptr);
        pBindResultInfo->GetBindAssembly(&m_pHostAssembly);
    }
#endif // FEATURE_HOSTED_BINDER        
    
#if _DEBUG
    GetCodeBaseOrName(m_debugName);
    m_debugName.Normalize();
    m_pDebugName = m_debugName;

    AssemblySpec spec;
    spec.InitializeSpec(this);

    spec.GetFileOrDisplayName(ASM_DISPLAYF_VERSION |
                              ASM_DISPLAYF_CULTURE |
                              ASM_DISPLAYF_PUBLIC_KEY_TOKEN,
                              m_sTextualIdentity);
#endif
}
#endif // FEATURE_FUSION


#if defined(FEATURE_HOSTED_BINDER)

#ifdef FEATURE_FUSION

PEAssembly *PEAssembly::Open(
    PEAssembly *pParentAssembly,
    PEImage *pPEImageIL,
    BOOL isIntrospectionOnly)
{
    STANDARD_VM_CONTRACT;
    PEAssembly * pPEAssembly = new PEAssembly(
        pPEImageIL, // PEImage
        nullptr,    // IMetaDataEmit
        nullptr,    // IAssembly
        nullptr,    // IBindResult pNativeFusionAssembly
        nullptr,    // PEImage *pNIImage
        nullptr,    // IFusionBindLog
        nullptr,    // IHostAssembly
        pParentAssembly,    // creator
        FALSE,      // isSystem
        isIntrospectionOnly,      // isIntrospectionOnly
        NULL);

    return pPEAssembly;
}

PEAssembly *PEAssembly::Open(
    PEAssembly *       pParent,
    PEImage *          pPEImageIL, 
    PEImage *          pPEImageNI, 
    ICLRPrivAssembly * pHostAssembly, 
    BOOL               fIsIntrospectionOnly)
{
    STANDARD_VM_CONTRACT;
    PEAssembly * pPEAssembly = new PEAssembly(
        pPEImageIL, // PEImage
        nullptr,    // IMetaDataEmit
        nullptr,    // IAssembly
        nullptr,    // IBindResult pNativeFusionAssembly
        pPEImageNI, // Native Image PEImage
        nullptr,    // IFusionBindLog
        nullptr,    // IHostAssembly
        pParent,    // creator
        FALSE,      // isSystem
        fIsIntrospectionOnly, 
        pHostAssembly);

    return pPEAssembly;
}

#else //FEATURE_FUSION

PEAssembly *PEAssembly::Open(
    PEAssembly *       pParent,
    PEImage *          pPEImageIL, 
    PEImage *          pPEImageNI, 
    ICLRPrivAssembly * pHostAssembly, 
    BOOL               fIsIntrospectionOnly)
{
    STANDARD_VM_CONTRACT;

    PEAssembly * pPEAssembly = new PEAssembly(
        nullptr,        // BindResult
        nullptr,        // IMetaDataEmit
        pParent,        // PEFile creator
        FALSE,          // isSystem
        fIsIntrospectionOnly,
        pPEImageIL,
        pPEImageNI,
        pHostAssembly);

    return pPEAssembly;
}

#endif // FEATURE_FUSION

#endif // FEATURE_HOSTED_BINDER 


PEAssembly::~PEAssembly()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS; // Fusion uses crsts on AddRef/Release
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();
#ifdef FEATURE_FUSION    
    if (m_pFusionAssemblyName != NULL)
        m_pFusionAssemblyName->Release();
    if (m_pFusionAssembly != NULL)
        m_pFusionAssembly->Release();
    if (m_pIHostAssembly != NULL)
        m_pIHostAssembly->Release();
    if (m_pNativeAssemblyLocation != NULL)
    {
        m_pNativeAssemblyLocation->Release();
    }
    if (m_pNativeImageClosure!=NULL)
        m_pNativeImageClosure->Release();
    if (m_pFusionLog != NULL)
        m_pFusionLog->Release();
#endif // FEATURE_FUSION
    if (m_creator != NULL)
        m_creator->Release();

}

#ifndef  DACCESS_COMPILE
void PEAssembly::ReleaseIL()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS; 
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();
#ifdef FEATURE_FUSION
    if (m_pFusionAssemblyName != NULL)
    {
        m_pFusionAssemblyName->Release();
        m_pFusionAssemblyName=NULL;
    }
    if (m_pFusionAssembly != NULL)
    {
        m_pFusionAssembly->Release();
        m_pFusionAssembly=NULL;
    }
    if (m_pIHostAssembly != NULL)
    {
        m_pIHostAssembly->Release();
        m_pIHostAssembly=NULL;
    }
    if (m_pNativeAssemblyLocation != NULL)
    {
        m_pNativeAssemblyLocation->Release();
        m_pNativeAssemblyLocation=NULL;
    }
    _ASSERTE(m_pNativeImageClosure==NULL);
    
    if (m_pFusionLog != NULL)
    {
        m_pFusionLog->Release();
        m_pFusionLog=NULL;
    }
#endif // FEATURE_FUSION
    if (m_creator != NULL)
    {
        m_creator->Release();
        m_creator=NULL;
    }

    PEFile::ReleaseIL();
}
#endif 

/* static */

#ifdef FEATURE_FUSION
PEAssembly *PEAssembly::OpenSystem(IApplicationContext * pAppCtx)
#else
PEAssembly *PEAssembly::OpenSystem(IUnknown * pAppCtx)
#endif
{
    STANDARD_VM_CONTRACT;

    PEAssembly *result = NULL;

    EX_TRY
    {
        result = DoOpenSystem(pAppCtx);
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context

        if (!ex->IsTransient())
            EEFileLoadException::Throw(SystemDomain::System()->BaseLibrary(), ex->GetHR(), ex);
    }
    EX_END_HOOK;
    return result;
}

/* static */
#ifdef FEATURE_FUSION
PEAssembly *PEAssembly::DoOpenSystem(IApplicationContext * pAppCtx)
#else
PEAssembly *PEAssembly::DoOpenSystem(IUnknown * pAppCtx)
#endif
{
    CONTRACT(PEAssembly *)
    {
        POSTCONDITION(CheckPointer(RETVAL));
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

#ifdef FEATURE_FUSION
    SafeComHolder<IAssemblyName> pName;
    IfFailThrow(CreateAssemblyNameObject(&pName, W("mscorlib"), 0, NULL));

    UINT64 publicKeyValue = I64(CONCAT_MACRO(0x, VER_ECMA_PUBLICKEY));
    BYTE publicKeyToken[8] =
        {
            (BYTE) (publicKeyValue>>56),
            (BYTE) (publicKeyValue>>48),
            (BYTE) (publicKeyValue>>40),
            (BYTE) (publicKeyValue>>32),
            (BYTE) (publicKeyValue>>24),
            (BYTE) (publicKeyValue>>16),
            (BYTE) (publicKeyValue>>8),
            (BYTE) (publicKeyValue),
        };

    IfFailThrow(pName->SetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, publicKeyToken, sizeof(publicKeyToken)));

    USHORT version = VER_ASSEMBLYMAJORVERSION;
    IfFailThrow(pName->SetProperty(ASM_NAME_MAJOR_VERSION, &version, sizeof(version)));
    version = VER_ASSEMBLYMINORVERSION;
    IfFailThrow(pName->SetProperty(ASM_NAME_MINOR_VERSION, &version, sizeof(version)));
    version = VER_ASSEMBLYBUILD;
    IfFailThrow(pName->SetProperty(ASM_NAME_BUILD_NUMBER, &version, sizeof(version)));
    version = VER_ASSEMBLYBUILD_QFE;
    IfFailThrow(pName->SetProperty(ASM_NAME_REVISION_NUMBER, &version, sizeof(version)));

    IfFailThrow(pName->SetProperty(ASM_NAME_CULTURE, W(""), sizeof(WCHAR)));

#ifdef FEATURE_PREJIT
#ifdef PROFILING_SUPPORTED
    if (NGENImagesAllowed())
    {
        // Binding flags, zap string
        CorCompileConfigFlags configFlags = PEFile::GetNativeImageConfigFlagsWithOverrides();
        IfFailThrow(pName->SetProperty(ASM_NAME_CONFIG_MASK, &configFlags, sizeof(configFlags)));

        LPCWSTR configString = g_pConfig->ZapSet();
        IfFailThrow(pName->SetProperty(ASM_NAME_CUSTOM, (PVOID)configString,
                                        (DWORD) (wcslen(configString)+1)*sizeof(WCHAR)));

        // @TODO: Need some fuslogvw logging here
    }
#endif //PROFILING_SUPPORTED
#endif // FEATURE_PREJIT

    SafeComHolder<IAssembly> pIAssembly;
    SafeComHolder<IBindResult> pNativeFusionAssembly;
    SafeComHolder<IFusionBindLog> pFusionLog;

    {
        ETWOnStartup (FusionBinding_V1, FusionBindingEnd_V1);
        IfFailThrow(BindToSystem(pName, SystemDomain::System()->SystemDirectory(), NULL, pAppCtx, &pIAssembly, &pNativeFusionAssembly, &pFusionLog));
    }

    StackSString path;
    FusionBind::GetAssemblyManifestModulePath(pIAssembly, path);

    // Open the image with no required mapping.  This will be
    // promoted to a real open if we don't have a native image.
    PEImageHolder image (PEImage::OpenImage(path));

    PEAssembly* pPEAssembly = new PEAssembly(image, NULL, pIAssembly,pNativeFusionAssembly, NULL, pFusionLog, NULL, NULL, TRUE, FALSE);

#ifdef FEATURE_APPX_BINDER
    if (AppX::IsAppXProcess())
    {
        // Since mscorlib is loaded as a special case, create and assign an ICLRPrivAssembly for the new PEAssembly here.
        CLRPrivBinderAppX *   pBinder = CLRPrivBinderAppX::GetOrCreateBinder();
        CLRPrivBinderFusion * pFusionBinder = pBinder->GetFusionBinder();
        
        pFusionBinder->BindMscorlib(pPEAssembly);
    }
#endif
    
    RETURN pPEAssembly;
#else // FEATURE_FUSION
    ETWOnStartup (FusionBinding_V1, FusionBindingEnd_V1);
    CoreBindResult bindResult;
    ReleaseHolder<ICLRPrivAssembly> pPrivAsm;
    IfFailThrow(CCoreCLRBinderHelper::BindToSystem(&pPrivAsm, !IsCompilationProcess() || g_fAllowNativeImages));
    if(pPrivAsm != NULL)
    {
        bindResult.Init(pPrivAsm, TRUE, TRUE);
    }

    RETURN new PEAssembly(&bindResult, NULL, NULL, TRUE, FALSE);
#endif // FEATURE_FUSION
}

#ifdef FEATURE_FUSION
/* static */
PEAssembly *PEAssembly::Open(IAssembly *pIAssembly,
                             IBindResult *pNativeFusionAssembly,
                             IFusionBindLog *pFusionLog/*=NULL*/,
                             BOOL isSystemAssembly/*=FALSE*/,
                             BOOL isIntrospectionOnly/*=FALSE*/)
{
    STANDARD_VM_CONTRACT;

    PEAssembly *result = NULL;
    EX_TRY
    {
        result = DoOpen(pIAssembly, pNativeFusionAssembly, pFusionLog, isSystemAssembly, isIntrospectionOnly);
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context
        if (!ex->IsTransient())
            EEFileLoadException::Throw(pIAssembly, NULL, ex->GetHR(), ex);
    }
    EX_END_HOOK;

    return result;
}

// Thread stress
class DoOpenIAssemblyStress : APIThreadStress
{
public:
    IAssembly *pIAssembly;
    IBindResult *pNativeFusionAssembly;
    IFusionBindLog *pFusionLog;
    DoOpenIAssemblyStress(IAssembly *pIAssembly, IBindResult *pNativeFusionAssembly, IFusionBindLog *pFusionLog)
          : pIAssembly(pIAssembly), pNativeFusionAssembly(pNativeFusionAssembly),pFusionLog(pFusionLog) {LIMITED_METHOD_CONTRACT;}
    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        PEAssemblyHolder result (PEAssembly::Open(pIAssembly, pNativeFusionAssembly, pFusionLog, FALSE, FALSE));
    }
};

/* static */
PEAssembly *PEAssembly::DoOpen(IAssembly *pIAssembly,
                               IBindResult *pNativeFusionAssembly,
                               IFusionBindLog *pFusionLog,
                               BOOL isSystemAssembly,
                               BOOL isIntrospectionOnly/*=FALSE*/)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(pIAssembly));
        POSTCONDITION(CheckPointer(RETVAL));
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    DoOpenIAssemblyStress ts(pIAssembly,pNativeFusionAssembly,pFusionLog);

    PEImageHolder image;

    StackSString path;
    FusionBind::GetAssemblyManifestModulePath(pIAssembly, path);

    // Open the image with no required mapping.  This will be
    // promoted to a real open if we don't have a native image.
    image = PEImage::OpenImage(path, MDInternalImport_NoCache); // "identity" does not need to be cached 

    PEAssemblyHolder assembly (new PEAssembly(image, NULL, pIAssembly, pNativeFusionAssembly, NULL, pFusionLog,
                                               NULL, NULL, isSystemAssembly, isIntrospectionOnly));

    RETURN assembly.Extract();
}

/* static */
PEAssembly *PEAssembly::Open(IHostAssembly *pIHostAssembly, BOOL isSystemAssembly, BOOL isIntrospectionOnly)
{
    STANDARD_VM_CONTRACT;

    PEAssembly *result = NULL;

    EX_TRY
    {
        result = DoOpen(pIHostAssembly, isSystemAssembly, isIntrospectionOnly);
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context

        if (!ex->IsTransient())
            EEFileLoadException::Throw(NULL, pIHostAssembly, ex->GetHR(), ex);
    }
    EX_END_HOOK;
    return result;
}

// Thread stress
class DoOpenIHostAssemblyStress : APIThreadStress
{
public:
    IHostAssembly *pIHostAssembly;
    DoOpenIHostAssemblyStress(IHostAssembly *pIHostAssembly) :
        pIHostAssembly(pIHostAssembly) {LIMITED_METHOD_CONTRACT;}
    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        PEAssemblyHolder result (PEAssembly::Open(pIHostAssembly, FALSE, FALSE));
    }
};

/* static */
PEAssembly *PEAssembly::DoOpen(IHostAssembly *pIHostAssembly, BOOL isSystemAssembly,
                               BOOL isIntrospectionOnly)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(pIHostAssembly));
        POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    DoOpenIHostAssemblyStress ts(pIHostAssembly);

    UINT64 AssemblyId;
    IfFailThrow(pIHostAssembly->GetAssemblyId(&AssemblyId));

    PEImageHolder image(PEImage::FindById(AssemblyId, 0));

    PEAssemblyHolder assembly (new PEAssembly(image, NULL, NULL, NULL, NULL, NULL,
                                              pIHostAssembly, NULL, isSystemAssembly, isIntrospectionOnly));

    RETURN assembly.Extract();
}
#endif // FEATURE_FUSION

#ifndef CROSSGEN_COMPILE
/* static */
PEAssembly *PEAssembly::OpenMemory(PEAssembly *pParentAssembly,
                                   const void *flat, COUNT_T size,
                                   BOOL isIntrospectionOnly/*=FALSE*/,
                                   CLRPrivBinderLoadFile* pBinderToUse)
{
    STANDARD_VM_CONTRACT;

    PEAssembly *result = NULL;

    EX_TRY
    {
        result = DoOpenMemory(pParentAssembly, flat, size, isIntrospectionOnly, pBinderToUse);
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context

        if (!ex->IsTransient())
            EEFileLoadException::Throw(pParentAssembly, flat, size, ex->GetHR(), ex);
    }
    EX_END_HOOK;

    return result;
}


// Thread stress

class DoOpenFlatStress : APIThreadStress
{
public:
    PEAssembly *pParentAssembly;
    const void *flat;
    COUNT_T size;
    DoOpenFlatStress(PEAssembly *pParentAssembly, const void *flat, COUNT_T size)
        : pParentAssembly(pParentAssembly), flat(flat), size(size) {LIMITED_METHOD_CONTRACT;}
    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        PEAssemblyHolder result(PEAssembly::OpenMemory(pParentAssembly, flat, size, FALSE));
    }
};

/* static */
PEAssembly *PEAssembly::DoOpenMemory(
    PEAssembly *pParentAssembly,
    const void *flat,
    COUNT_T size,
    BOOL isIntrospectionOnly,
    CLRPrivBinderLoadFile* pBinderToUse)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(flat));
        PRECONDITION(CheckOverflow(flat, size));
        PRECONDITION(CheckPointer(pParentAssembly));
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Thread stress
    DoOpenFlatStress ts(pParentAssembly, flat, size);

    // Note that we must have a flat image stashed away for two reasons.
    // First, we need a private copy of the data which we can verify
    // before doing the mapping.  And secondly, we can only compute
    // the strong name hash on a flat image.

    PEImageHolder image(PEImage::LoadFlat(flat, size));

    // Need to verify that this is a CLR assembly
    if (!image->CheckILFormat())
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);

#if defined(FEATURE_HOSTED_BINDER) && !defined(FEATURE_CORECLR)
    if(pBinderToUse != NULL && !isIntrospectionOnly)
    {
        ReleaseHolder<ICLRPrivAssembly> pAsm;
        ReleaseHolder<IAssemblyName> pAssemblyName;
        IfFailThrow(pBinderToUse->BindAssemblyExplicit(image, &pAssemblyName, &pAsm));
        PEAssembly* pFile = nullptr;
        IfFailThrow(GetAppDomain()->BindHostedPrivAssembly(pParentAssembly, pAsm, pAssemblyName, &pFile));
        _ASSERTE(pFile);
        RETURN pFile;
    }
#endif //  FEATURE_HOSTED_BINDER && !FEATURE_CORECLR

#ifdef FEATURE_FUSION    
    RETURN new PEAssembly(image, NULL, NULL, NULL, NULL, NULL, NULL, pParentAssembly, FALSE, isIntrospectionOnly);
#else
    CoreBindResult bindResult;
    ReleaseHolder<ICLRPrivAssembly> assembly;
    IfFailThrow(CCoreCLRBinderHelper::GetAssemblyFromImage(image, NULL, &assembly));
    bindResult.Init(assembly,FALSE,FALSE);

    RETURN new PEAssembly(&bindResult, NULL, pParentAssembly, FALSE, isIntrospectionOnly);
#endif
}
#endif // !CROSSGEN_COMPILE

#if defined(FEATURE_MIXEDMODE) && !defined(CROSSGEN_COMPILE)
// Use for main exe loading
// This is also used for "spontaneous" (IJW) dll loading where
// we need to deliver DllMain callbacks, but we should eliminate this case

/* static */
PEAssembly *PEAssembly::OpenHMODULE(HMODULE hMod,
                                    IAssembly *pFusionAssembly,
                                    IBindResult *pNativeFusionAssembly,
                                    IFusionBindLog *pFusionLog/*=NULL*/,
                                    BOOL isIntrospectionOnly/*=FALSE*/)
{
    STANDARD_VM_CONTRACT;

    PEAssembly *result = NULL;

    ETWOnStartup (OpenHModule_V1, OpenHModuleEnd_V1);

    EX_TRY
    {
        result = DoOpenHMODULE(hMod, pFusionAssembly, pNativeFusionAssembly, pFusionLog, isIntrospectionOnly);
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context
        if (!ex->IsTransient())
            EEFileLoadException::Throw(pFusionAssembly, NULL, ex->GetHR(), ex);
    }
    EX_END_HOOK;
    return result;
}

// Thread stress
class DoOpenHMODULEStress : APIThreadStress
{
public:
    HMODULE hMod;
    IAssembly *pFusionAssembly;
    IBindResult *pNativeFusionAssembly;    
    IFusionBindLog *pFusionLog;
    DoOpenHMODULEStress(HMODULE hMod, IAssembly *pFusionAssembly, IBindResult *pNativeFusionAssembly, IFusionBindLog *pFusionLog)
      : hMod(hMod), pFusionAssembly(pFusionAssembly), pNativeFusionAssembly(pNativeFusionAssembly),pFusionLog(pFusionLog) {LIMITED_METHOD_CONTRACT;}
    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        PEAssemblyHolder result(PEAssembly::OpenHMODULE(hMod, pFusionAssembly,pNativeFusionAssembly, pFusionLog, FALSE));
    }
};

/* static */
PEAssembly *PEAssembly::DoOpenHMODULE(HMODULE hMod,
                                      IAssembly *pFusionAssembly,
                                      IBindResult *pNativeFusionAssembly,
                                      IFusionBindLog *pFusionLog,
                                      BOOL isIntrospectionOnly/*=FALSE*/)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(hMod));
        PRECONDITION(CheckPointer(pFusionAssembly));
        PRECONDITION(CheckPointer(pNativeFusionAssembly,NULL_OK));        
        POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    DoOpenHMODULEStress ts(hMod, pFusionAssembly, pNativeFusionAssembly, pFusionLog);

    PEImageHolder image(PEImage::LoadImage(hMod));

    RETURN new PEAssembly(image, NULL, pFusionAssembly, pNativeFusionAssembly, NULL, pFusionLog, NULL, NULL, FALSE, isIntrospectionOnly);
}
#endif // FEATURE_MIXEDMODE && !CROSSGEN_COMPILE


#ifndef FEATURE_FUSION
PEAssembly* PEAssembly::Open(CoreBindResult* pBindResult,
                                   BOOL isSystem, BOOL isIntrospectionOnly)
{

    return new PEAssembly(pBindResult,NULL,NULL,isSystem,isIntrospectionOnly);

};
#endif

/* static */
PEAssembly *PEAssembly::Create(PEAssembly *pParentAssembly,
                               IMetaDataAssemblyEmit *pAssemblyEmit,
                               BOOL bIsIntrospectionOnly)
{
    CONTRACT(PEAssembly *)
    {
        PRECONDITION(CheckPointer(pParentAssembly));
        PRECONDITION(CheckPointer(pAssemblyEmit));
        STANDARD_VM_CHECK; 
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Set up the metadata pointers in the PEAssembly. (This is the only identity
    // we have.)
    SafeComHolder<IMetaDataEmit> pEmit;
    pAssemblyEmit->QueryInterface(IID_IMetaDataEmit, (void **)&pEmit);
#ifdef FEATURE_FUSION
    ReleaseHolder<ICLRPrivAssembly> pPrivAssembly;
    if (pParentAssembly->HasHostAssembly())
    {
        // Dynamic assemblies in AppX use their parent's ICLRPrivAssembly as the binding context.
        pPrivAssembly = clr::SafeAddRef(new CLRPrivBinderUtil::CLRPrivBinderAsAssemblyWrapper(
            pParentAssembly->GetHostAssembly()));
    }

    PEAssemblyHolder pFile(new PEAssembly(
        NULL, pEmit, NULL, NULL, NULL, NULL, NULL, pParentAssembly,
        FALSE, bIsIntrospectionOnly,
        pPrivAssembly));
#else
    PEAssemblyHolder pFile(new PEAssembly(NULL, pEmit, pParentAssembly, FALSE, bIsIntrospectionOnly));
#endif
    RETURN pFile.Extract();
}


#ifdef FEATURE_PREJIT

#ifdef FEATURE_FUSION
BOOL PEAssembly::HasEqualNativeClosure(DomainAssembly * pDomainAssembly)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDomainAssembly));
    }
    CONTRACTL_END;
    if (IsSystem())
        return TRUE;
    HRESULT hr = S_OK;


    if (m_pNativeImageClosure == NULL)
        return FALSE;

    // ensure theclosures are walked
    IAssemblyBindingClosure * pClosure = pDomainAssembly->GetAssemblyBindingClosure(LEVEL_COMPLETE);
    _ASSERTE(pClosure != NULL);

    if (m_pNativeImageClosure->HasBeenWalked(LEVEL_COMPLETE) != S_OK )
    {
        GCX_COOP();

        ENTER_DOMAIN_PTR(SystemDomain::System()->DefaultDomain(),ADV_DEFAULTAD);
        {
            GCX_PREEMP();
            IfFailThrow(m_pNativeImageClosure->EnsureWalked(GetFusionAssembly(),GetAppDomain()->GetFusionContext(),LEVEL_COMPLETE));
        }
        END_DOMAIN_TRANSITION;
    }


    hr = pClosure->IsEqual(m_pNativeImageClosure);
    IfFailThrow(hr);
    return (hr == S_OK);
}
#endif //FEATURE_FUSION

#ifdef FEATURE_FUSION
void PEAssembly::SetNativeImage(IBindResult *pNativeFusionAssembly)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    StackSString path;
    WCHAR pwzPath[MAX_LONGPATH];
    DWORD dwCCPath = MAX_LONGPATH;
    ReleaseHolder<IAssemblyLocation> pIAssemblyLocation;

    IfFailThrow(pNativeFusionAssembly->GetAssemblyLocation(&pIAssemblyLocation));
    IfFailThrow(pIAssemblyLocation->GetPath(pwzPath, &dwCCPath));
    path.Set(pwzPath);

    PEImageHolder image(PEImage::OpenImage(path));
    image->Load();

    // For desktop dev11, this verification is now done at native binding time.
    _ASSERTE(CheckNativeImageVersion(image));

    PEFile::SetNativeImage(image);
    IfFailThrow(pNativeFusionAssembly->GetAssemblyLocation(&m_pNativeAssemblyLocation));
}
#else //FEATURE_FUSION
void PEAssembly::SetNativeImage(PEImage * image)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    image->Load();

    if (CheckNativeImageVersion(image))
    {
        PEFile::SetNativeImage(image);
#if 0
        //Enable this code if you want to make sure we never touch the flat layout in the presence of the
        //ngen image.
//#if defined(_DEBUG)
        //find all the layouts in the il image and make sure we never touch them.
        unsigned ignored = 0;
        PTR_PEImageLayout layout = m_ILimage->GetLayout(PEImageLayout::LAYOUT_FLAT, 0);
        if (layout != NULL)
        {
            //cache a bunch of PE metadata in the PEDecoder
            m_ILimage->CheckILFormat();

            //we also need some of metadata (for the public key), so cache this too
            DWORD verifyOutputFlags;
            m_ILimage->VerifyStrongName(&verifyOutputFlags);
            //fudge this by a few pages to make sure we can still mess with the PE headers
            const size_t fudgeSize = 4096 * 4;
            ClrVirtualProtect((void*)(((char *)layout->GetBase()) + fudgeSize),
                              layout->GetSize() - fudgeSize, 0, &ignored);
            layout->Release();
        }
#endif
    }
    else
    {
        ExternalLog(LL_WARNING, "Native image is not correct version.");
    }
}
#endif //FEATURE_FUSION

#ifdef FEATURE_FUSION
void PEAssembly::ClearNativeImage()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeImage());
        POSTCONDITION(!HasNativeImage());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    PEFile::ClearNativeImage();

    if (m_pNativeAssemblyLocation != NULL)
        m_pNativeAssemblyLocation->Release();
    m_pNativeAssemblyLocation = NULL;
    if (m_pNativeImageClosure != NULL)
        m_pNativeImageClosure->Release();
    m_pNativeImageClosure = NULL;
    RETURN;
}
#endif //FEATURE_FUSION
#endif  // FEATURE_PREJIT


#ifdef FEATURE_FUSION
BOOL PEAssembly::IsBindingCodeBase()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_pIHostAssembly != NULL)
        return FALSE;

    if (m_pFusionAssembly == NULL)
        return (!GetPath().IsEmpty());

    if (m_dwLocationFlags == ASMLOC_UNKNOWN)
        return FALSE;

    return ((m_dwLocationFlags & ASMLOC_CODEBASE_HINT) != 0);
}

BOOL PEAssembly::IsSourceGAC()
{
    LIMITED_METHOD_CONTRACT;

    if ((m_pIHostAssembly != NULL) || (m_pFusionAssembly == NULL))
    {
        return FALSE;
    }

    return ((m_dwLocationFlags & ASMLOC_LOCATION_MASK) == ASMLOC_GAC);
}

BOOL PEAssembly::IsSourceDownloadCache()
{
    LIMITED_METHOD_CONTRACT;

    if ((m_pIHostAssembly != NULL) || (m_pFusionAssembly == NULL))
    {
        return FALSE;
    }
    
    return ((m_dwLocationFlags & ASMLOC_LOCATION_MASK) == ASMLOC_DOWNLOAD_CACHE);
}

#else // FEATURE_FUSION
BOOL PEAssembly::IsSourceGAC()
{
    WRAPPER_NO_CONTRACT;
    return m_bIsFromGAC;
};

#endif // FEATURE_FUSION

#endif // #ifndef DACCESS_COMPILE

#ifdef FEATURE_FUSION
BOOL PEAssembly::IsContextLoad()
{
    LIMITED_METHOD_CONTRACT;
    if ((m_pIHostAssembly != NULL) || (m_pFusionAssembly == NULL))
    {
        return FALSE;
    }
    return (IsSystem() || (m_loadContext == LOADCTX_TYPE_DEFAULT));
}

LOADCTX_TYPE PEAssembly::GetLoadContext()
{
    LIMITED_METHOD_CONTRACT;

    return m_loadContext;
}

DWORD PEAssembly::GetLocationFlags()
{
    LIMITED_METHOD_CONTRACT;

    return m_dwLocationFlags;
}

#endif


#ifndef DACCESS_COMPILE

#ifdef FEATURE_FUSION
PEKIND PEAssembly::GetFusionProcessorArchitecture()
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    PEImage * pImage = NULL;

#ifdef FEATURE_PREJIT 
    pImage = m_nativeImage;
#endif

    if (pImage == NULL)
        pImage = GetILimage();

    return pImage->GetFusionProcessorArchitecture();
}

IAssemblyName * PEAssembly::GetFusionAssemblyName()
{
    CONTRACT(IAssemblyName *)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (m_pFusionAssemblyName == NULL)
    {
        AssemblySpec spec;
        spec.InitializeSpec(this);
        PEImage * pImage = GetILimage();

#ifdef FEATURE_PREJIT 
        if ((pImage != NULL) && !pImage->MDImportLoaded())
            pImage = m_nativeImage;
#endif

        if (pImage != NULL)
        {
            spec.SetPEKIND(pImage->GetFusionProcessorArchitecture());
        }

        GCX_PREEMP();

        IfFailThrow(spec.CreateFusionName(&m_pFusionAssemblyName, FALSE));
    }

    RETURN m_pFusionAssemblyName;
}

// This version of GetFusionAssemlyName that can be used to return the reference in a
// NOTHROW/NOTRIGGER fashion. This is useful for scenarios where you dont want to invoke the THROWS/GCTRIGGERS
// version when you know the name would have been created and is available.
IAssemblyName * PEAssembly::GetFusionAssemblyNameNoCreate()
{
    LIMITED_METHOD_CONTRACT;

    return m_pFusionAssemblyName;
}

IAssembly *PEAssembly::GetFusionAssembly()
{
    CONTRACT(IAssembly *)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    RETURN m_pFusionAssembly;
}

IHostAssembly *PEAssembly::GetIHostAssembly()
{
    CONTRACT(IHostAssembly *)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    RETURN m_pIHostAssembly;
}

IAssemblyLocation *PEAssembly::GetNativeAssemblyLocation()
{
    CONTRACT(IAssemblyLocation *)
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNativeImage());
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    RETURN m_pNativeAssemblyLocation;
}
#endif // FEATURE_FUSION

// ------------------------------------------------------------
// Hash support
// ------------------------------------------------------------

void PEAssembly::VerifyStrongName()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // If we've already done the signature checks, we don't need to do them again.
    if (m_fStrongNameVerified)
    {
        return;
    }

#ifdef FEATURE_FUSION
    // System and dynamic assemblies don't need hash checks
    if (IsSystem() || IsDynamic())
#else
    // Without FUSION/GAC, we need to verify SN on all assemblies, except dynamic assemblies.
    if (IsDynamic())
#endif
    {

        m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
        m_fStrongNameVerified = TRUE;
        return;
    }

    // Next, verify the strong name, if necessary
#ifdef FEATURE_FUSION
    // See if the assembly comes from a secure location
    IAssembly *pFusionAssembly = GetAssembly()->GetFusionAssembly();
    if (pFusionAssembly)
    {
        DWORD dwLocation;
        IfFailThrow(pFusionAssembly->GetAssemblyLocation(&dwLocation));

        switch (dwLocation & ASMLOC_LOCATION_MASK)
        {
        case ASMLOC_GAC:
        case ASMLOC_DOWNLOAD_CACHE:
        case ASMLOC_DEV_OVERRIDE:
            // Assemblies from the GAC or download cache have
            // already been verified by Fusion.
            m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
            m_fStrongNameVerified = TRUE;
            return;

        case ASMLOC_RUN_FROM_SOURCE:
        case ASMLOC_UNKNOWN:
            // For now, just verify these every time, we need to
            // cache the fact that at least one verification has
            // been performed (if strong name policy permits
            // caching of verification results)
            break;

        default:
            UNREACHABLE();
        }
    }
#endif

    // Check format of image. Note we must delay this until after the GAC status has been
    // checked, to handle the case where we are not loading m_image.
    EnsureImageOpened();

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
    if (IsWindowsRuntime())
    {
        // Winmd files are always loaded in full trust.
        m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
        m_fStrongNameVerified = TRUE;        
        return;
    }
#endif

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
    if (m_nativeImage == NULL && !GetILimage()->IsTrustedNativeImage())
#else
    if (!GetILimage()->IsTrustedNativeImage())
#endif
    {
        if (!GetILimage()->CheckILFormat())
            ThrowHR(COR_E_BADIMAGEFORMAT);
    }

#if defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR)
    // Do not validate strong name signature during CrossGen. This is necessary
    // to make build-lab scenarios to work.
    if (IsCompilationProcess())
    {
        m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
    }
    else
#endif
    // Check the strong name if present.
    if (IsIntrospectionOnly())
    {
        // For introspection assemblies, we don't need to check strong names and we don't
        // need to do module hash checks.
        m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
    }
#if !defined(FEATURE_CORECLR)    
    //We do this to early out for WinMD files that are unsigned but have NI images as well.
    else if (!HasStrongNameSignature())
    {
#ifdef FEATURE_CAS_POLICY
        // We only check module hashes if there is a strong name or Authenticode signature
        if (m_certificate == NULL)
        {
            m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
        }
#endif
    }
#endif // !defined(FEATURE_CORECLR)    
    else
    {
#if defined(FEATURE_CORECLR)
        // Runtime policy on CoreCLR is to skip verification of ALL assemblies
        m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
        m_fStrongNameVerified = TRUE;
#else

#ifdef FEATURE_CORECLR
        BOOL skip = FALSE;

        // Skip verification for assemblies from the trusted path
        if (IsSystem() || m_bIsOnTpaList)
            skip = TRUE;

#ifdef FEATURE_LEGACYNETCF
        // crossgen should skip verification for Mango
        if (RuntimeIsLegacyNetCF(0))
            skip = TRUE;
#endif

        if (skip)
        {
            m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
            m_fStrongNameVerified = TRUE;
            return;
        }
#endif // FEATURE_CORECLR

        DWORD verifyOutputFlags = 0;
        HRESULT hr = GetILimage()->VerifyStrongName(&verifyOutputFlags);

        if (SUCCEEDED(hr))
        {
            // Strong name verified or delay sign OK'ed.
            // We will skip verification of modules in the delay signed case.

            if ((verifyOutputFlags & SN_OUTFLAG_WAS_VERIFIED) == 0)
                m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
        }
        else
        {
            // Strong name missing or error.  Throw in the latter case.
            if (hr != CORSEC_E_MISSING_STRONGNAME)
                ThrowHR(hr);

#ifdef FEATURE_CAS_POLICY
            // Since we are not strong named, don't check module hashes.
            // (Unless we have a security certificate, in which case check anyway.)

            if (m_certificate == NULL)
                m_flags |= PEFILE_SKIP_MODULE_HASH_CHECKS;
#endif
        }

#endif // FEATURE_CORECLR
    }

    m_fStrongNameVerified = TRUE;
}

#ifdef FEATURE_CORECLR
BOOL PEAssembly::IsProfileAssembly()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    //
    // For now, cache the result of the check below. This cache should be removed once/if the check below 
    // becomes cheap (e.g. does not access metadata anymore).
    //
    if (VolatileLoadWithoutBarrier(&m_fProfileAssembly) != 0)
    {
        return m_fProfileAssembly > 0;
    }

    //
    // In order to be a platform (profile) assembly, you must be from a trusted location (TPA list)
    // If we are binding by TPA list and this assembly is on it, IsSourceGAC is true => Assembly is Profile
    // If the assembly is a WinMD, it is automatically trusted since all WinMD scenarios are full trust scenarios.
    //
    // The check for Silverlight strongname platform assemblies is legacy backdoor. It was introduced by accidental abstraction leak
    // from the old Silverlight binder, people took advantage of it and we cannot easily get rid of it now. See DevDiv #710462.
    //
    BOOL bProfileAssembly = IsSourceGAC() && (IsSystem() || m_bIsOnTpaList);
    if(!AppX::IsAppXProcess())
    {
        bProfileAssembly |= IsSourceGAC() && IsSilverlightPlatformStrongNameSignature();
    }

    m_fProfileAssembly = bProfileAssembly ? 1 : -1;
    return bProfileAssembly;
}

BOOL PEAssembly::IsSilverlightPlatformStrongNameSignature()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsDynamic())
        return FALSE;

    DWORD cbPublicKey;
    const BYTE *pbPublicKey = static_cast<const BYTE *>(GetPublicKey(&cbPublicKey));
    if (pbPublicKey == nullptr)
    {
        return false;
    }

    if (StrongNameIsSilverlightPlatformKey(pbPublicKey, cbPublicKey))
        return true;

#ifdef FEATURE_STRONGNAME_TESTKEY_ALLOWED
    if (StrongNameIsTestKey(pbPublicKey, cbPublicKey))
        return true;
#endif

    return false;
}

#ifdef FEATURE_STRONGNAME_TESTKEY_ALLOWED
BOOL PEAssembly::IsProfileTestAssembly()
{
    WRAPPER_NO_CONTRACT;

    return IsSourceGAC() && IsTestKeySignature();
}

BOOL PEAssembly::IsTestKeySignature()
{
    WRAPPER_NO_CONTRACT;

    if (IsDynamic())
        return FALSE;

    DWORD cbPublicKey;
    const BYTE *pbPublicKey = static_cast<const BYTE *>(GetPublicKey(&cbPublicKey));
    if (pbPublicKey == nullptr)
    {
        return false;
    }

    return StrongNameIsTestKey(pbPublicKey, cbPublicKey);
}
#endif // FEATURE_STRONGNAME_TESTKEY_ALLOWED

#endif // FEATURE_CORECLR

// ------------------------------------------------------------
// Descriptive strings
// ------------------------------------------------------------

// Effective path is the path of nearest parent (creator) assembly which has a nonempty path.

const SString &PEAssembly::GetEffectivePath()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PEAssembly *pAssembly = this;

    while (pAssembly->m_identity == NULL
           || pAssembly->m_identity->GetPath().IsEmpty())
    {
        if (pAssembly->m_creator)
            pAssembly = pAssembly->m_creator->GetAssembly();
        else // Unmanaged exe which loads byte[]/IStream assemblies
            return SString::Empty();
    }

    return pAssembly->m_identity->GetPath();
}


// Codebase is the fusion codebase or path for the assembly.  It is in URL format.
// Note this may be obtained from the parent PEFile if we don't have a path or fusion
// assembly.
//
// fCopiedName means to get the "shadow copied" path rather than the original path, if applicable
void PEAssembly::GetCodeBase(SString &result, BOOL fCopiedName/*=FALSE*/)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
#ifdef FEATURE_FUSION
    // For a copied name, we always use the actual file path rather than the fusion info
    if (!fCopiedName && m_pFusionAssembly)
    {
        if ( ((m_dwLocationFlags & ASMLOC_LOCATION_MASK) == ASMLOC_RUN_FROM_SOURCE) ||
             ((m_dwLocationFlags & ASMLOC_LOCATION_MASK) == ASMLOC_DOWNLOAD_CACHE) )
        {
            // Assemblies in the download cache or run from source should have
            // a proper codebase set in them.
            FusionBind::GetAssemblyNameStringProperty(GetFusionAssemblyName(),
                                                      ASM_NAME_CODEBASE_URL,
                                                      result);
            return;
        }
    }
    else if (m_pIHostAssembly)
    {
        FusionBind::GetAssemblyNameStringProperty(GetFusionAssemblyName(),
                                                  ASM_NAME_CODEBASE_URL,
                                                  result);
        return;
    }
#endif    

    // All other cases use the file path.
    result.Set(GetEffectivePath());
    if (!result.IsEmpty())
        PathToUrl(result);
}

/* static */
void PEAssembly::PathToUrl(SString &string)
{
    CONTRACTL
    {
        PRECONDITION(PEImage::CheckCanonicalFullPath(string));
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    SString::Iterator i = string.Begin();

#if !defined(PLATFORM_UNIX)
    if (i[0] == W('\\'))
    {
        // Network path
        string.Insert(i, SL("file://"));
        string.Skip(i, SL("file://"));
    }
    else
    {
        // Disk path
        string.Insert(i, SL("file:///"));
        string.Skip(i, SL("file:///"));
    }
#else
    // Unix doesn't have a distinction between a network or a local path
    _ASSERTE( i[0] == W('\\') || i[0] == W('/'));
    SString sss(SString::Literal, W("file://"));
    string.Insert(i, sss);
    string.Skip(i, sss);
#endif

    while (string.Find(i, W('\\')))
    {
        string.Replace(i, W('/'));
    }
}

void PEAssembly::UrlToPath(SString &string)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    SString::Iterator i = string.Begin();

    SString sss2(SString::Literal, W("file://"));
#if !defined(PLATFORM_UNIX)
    SString sss3(SString::Literal, W("file:///"));
    if (string.MatchCaseInsensitive(i, sss3))
        string.Delete(i, 8);
    else
#endif
    if (string.MatchCaseInsensitive(i, sss2))
        string.Delete(i, 7);

    while (string.Find(i, W('/')))
    {
        string.Replace(i, W('\\'));
    }

    RETURN;
}

BOOL PEAssembly::FindLastPathSeparator(const SString &path, SString::Iterator &i)
{
#ifdef PLATFORM_UNIX
    SString::Iterator slash = i;
    SString::Iterator backSlash = i;
    BOOL foundSlash = path.FindBack(slash, '/');
    BOOL foundBackSlash = path.FindBack(backSlash, '\\');
    if (!foundSlash && !foundBackSlash)
        return FALSE;
    else if (foundSlash && !foundBackSlash)
        i = slash;
    else if (!foundSlash && foundBackSlash)
        i = backSlash;
    else
        i = (backSlash > slash) ? backSlash : slash;
    return TRUE;
#else
    return path.FindBack(i, '\\');
#endif //PLATFORM_UNIX
}


// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------
#ifdef FEATURE_PREJIT
void PEAssembly::ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    PEFile::ExternalVLog(facility, level, fmt, args);

#ifdef FEATURE_FUSION
    if (FusionLoggingEnabled())
    {
        DWORD dwLogCategory = (facility == LF_ZAP ? FUSION_BIND_LOG_CATEGORY_NGEN : FUSION_BIND_LOG_CATEGORY_DEFAULT);

        StackSString message;
        message.VPrintf(fmt, args);
        m_pFusionLog->LogMessage(0, dwLogCategory, message);

        if (level == LL_ERROR) {
            m_pFusionLog->SetResultCode(dwLogCategory, E_FAIL);
            FlushExternalLog();
        }
    }
#endif //FEATURE_FUSION

    RETURN;
}

void PEAssembly::FlushExternalLog()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

#ifdef FEATURE_FUSION
    if (FusionLoggingEnabled()) {
        m_pFusionLog->Flush(g_dwLogLevel,  FUSION_BIND_LOG_CATEGORY_NGEN);
        m_pFusionLog->Flush(g_dwLogLevel,  FUSION_BIND_LOG_CATEGORY_DEFAULT);
    }
#endif //FEATURE_FUSION

    RETURN;
}
#endif //FEATURE_PREJIT
// ------------------------------------------------------------
// Metadata access
// ------------------------------------------------------------

HRESULT PEFile::GetVersion(USHORT *pMajor, USHORT *pMinor, USHORT *pBuild, USHORT *pRevision)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMajor, NULL_OK));
        PRECONDITION(CheckPointer(pMinor, NULL_OK));
        PRECONDITION(CheckPointer(pBuild, NULL_OK));
        PRECONDITION(CheckPointer(pRevision, NULL_OK));
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACTL_END;

    AssemblyMetaDataInternal md;
    HRESULT hr = S_OK;;
    if (m_bHasPersistentMDImport)
    {
        _ASSERTE(GetPersistentMDImport()->IsValidToken(TokenFromRid(1, mdtAssembly)));
        IfFailRet(GetPersistentMDImport()->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, &md, NULL));
    }
    else
    {
        ReleaseHolder<IMDInternalImport> pImport(GetMDImportWithRef());
        _ASSERTE(pImport->IsValidToken(TokenFromRid(1, mdtAssembly)));
        IfFailRet(pImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, NULL, &md, NULL));
    }
    
    if (pMajor != NULL)
        *pMajor = md.usMajorVersion;
    if (pMinor != NULL)
        *pMinor = md.usMinorVersion;
    if (pBuild != NULL)
        *pBuild = md.usBuildNumber;
    if (pRevision != NULL)
        *pRevision = md.usRevisionNumber;

    return hr;
}

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
// ================================================================================
// PEModule class - a PEFile which represents a satellite module
// ================================================================================

PEModule::PEModule(PEImage *image, PEAssembly *assembly, mdFile token, IMetaDataEmit *pEmit)
  : PEFile(image),
    m_assembly(NULL),
    m_token(token),
    m_bIsResource(-1)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(image, NULL_OK));
        PRECONDITION(CheckPointer(assembly));
        PRECONDITION(!IsNilToken(token));
        PRECONDITION(CheckPointer(pEmit, NULL_OK));
        PRECONDITION(image != NULL || pEmit != NULL);
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;
    
    DWORD flags;
    
    // get only the data which is required, here - flags
    // this helps avoid unnecessary memory touches
    IfFailThrow(assembly->GetPersistentMDImport()->GetFileProps(token, NULL, NULL, NULL, &flags));
    
    if (image != NULL)
    {
        if (IsFfContainsMetaData(flags) && !image->CheckILFormat())
            ThrowHR(COR_E_BADIMAGEFORMAT);
        
        if (assembly->IsIStream())
        {
            m_flags |= PEFILE_ISTREAM;
#ifdef FEATURE_PREJIT            
            m_fCanUseNativeImage = FALSE;
#endif
        }
    }
    
    assembly->AddRef();
    
    m_assembly = assembly;
    
    m_flags |= PEFILE_MODULE;
    if (assembly->IsSystem())
    {
        m_flags |= PEFILE_SYSTEM;
    }
    else
    {
        if (assembly->IsIntrospectionOnly())
        {
            m_flags |= PEFILE_INTROSPECTIONONLY;
#ifdef FEATURE_PREJIT            
            SetCannotUseNativeImage();        
#endif
        }
    }
    
    
    // Verify module format.  Note that some things have already happened:
    // - Fusion has verified the name matches the metadata
    // - PEimage has performed PE file format validation

    if (assembly->NeedsModuleHashChecks())
    {
        ULONG size;
        const void *hash;
        IfFailThrow(assembly->GetPersistentMDImport()->GetFileProps(token, NULL, &hash, &size, NULL));
        
        if (!CheckHash(assembly->GetHashAlgId(), hash, size))
            ThrowHR(COR_E_MODULE_HASH_CHECK_FAILED);
    }
    
#if defined(FEATURE_PREJIT) && !defined(CROSSGEN_COMPILE)
    // Find the native image
    if (IsFfContainsMetaData(flags)
        && m_fCanUseNativeImage
        && assembly->HasNativeImage()
        && assembly->GetFusionAssembly() != NULL)
    {
        IAssemblyLocation *pIAssemblyLocation = assembly->GetNativeAssemblyLocation();

        WCHAR wzPath[MAX_LONGPATH];
        WCHAR *pwzTemp = NULL;
        DWORD dwCCPath = MAX_LONGPATH;
        SString path;
        SString moduleName(SString::Utf8, GetSimpleName());

        // Compute the module path from the manifest module path
        IfFailThrow(pIAssemblyLocation->GetPath(wzPath, &dwCCPath));
        pwzTemp = PathFindFileName(wzPath);
        *pwzTemp = (WCHAR) 0x00;

        // <TODO>@todo: GetAppDomain????</TODO>
        path.Set(wzPath);
        path.Append((LPCWSTR) moduleName);

        SetNativeImage(path);
    }
#endif  // FEATURE_PREJIT && !CROSSGEN_COMPILE

#if _DEBUG
    GetCodeBaseOrName(m_debugName);
    m_pDebugName = m_debugName;
#endif
    
    if (IsFfContainsMetaData(flags))
    {
        if (image != NULL)
        {
            OpenMDImport_Unsafe(); //constructor. cannot race with anything
        }
        else
        {
            _ASSERTE(!m_bHasPersistentMDImport);
            IfFailThrow(GetMetaDataInternalInterfaceFromPublic(pEmit, IID_IMDInternalImport,
                                                               (void **)&m_pMDImport));
            m_pEmitter = pEmit;
            pEmit->AddRef();
            m_bHasPersistentMDImport=TRUE;
            m_MDImportIsRW_Debugger_Use_Only = TRUE;
        }
        
        // Fusion probably checks this, but we need to check this ourselves if
        // this file didn't come from Fusion
        if (!m_pMDImport->IsValidToken(m_pMDImport->GetModuleFromScope()))
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT);
    }
    else
    {
        // Go ahead and "load" image since it is essentially a noop, but will enable
        // more operations on the module earlier in the loading process.
        LoadLibrary();
    }
#ifdef FEATURE_PREJIT
    if (IsResource() || IsDynamic())
        m_fCanUseNativeImage = FALSE;
#endif    
}

PEModule::~PEModule()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_assembly->Release();
}

/* static */
PEModule *PEModule::Open(PEAssembly *assembly, mdFile token,
                         const SString &fileName)
{
    STANDARD_VM_CONTRACT;

    PEModule *result = NULL;

    EX_TRY
    {
        result = DoOpen(assembly, token, fileName);
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context

        if (!ex->IsTransient())
            EEFileLoadException::Throw(fileName, ex->GetHR(), ex);
    }
    EX_END_HOOK;

    return result;
}
// Thread stress
class DoOpenPathStress : APIThreadStress
{
public:
    PEAssembly *assembly;
    mdFile token;
    const SString &fileName;
    DoOpenPathStress(PEAssembly *assembly, mdFile token,
           const SString &fileName)
        : assembly(assembly), token(token), fileName(fileName)
    {
        WRAPPER_NO_CONTRACT;
        fileName.Normalize();
    }
    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        PEModuleHolder result(PEModule::Open(assembly, token, fileName));
    }
};

/* static */
PEModule *PEModule::DoOpen(PEAssembly *assembly, mdFile token,
                           const SString &fileName)
{
    CONTRACT(PEModule *)
    {
        PRECONDITION(CheckPointer(assembly));
        PRECONDITION(CheckValue(fileName));
        PRECONDITION(!IsNilToken(token));
        PRECONDITION(!fileName.IsEmpty());
        POSTCONDITION(CheckPointer(RETVAL));
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;
    
    DoOpenPathStress ts(assembly, token, fileName);
    
    // If this is a resource module, we must explicitly request a flat mapping
    DWORD flags;
    IfFailThrow(assembly->GetPersistentMDImport()->GetFileProps(token, NULL, NULL, NULL, &flags));
    
    PEImageHolder image;
#ifdef FEATURE_FUSION
    if (assembly->IsIStream())
    {
        SafeComHolder<IHostAssemblyModuleImport> pModuleImport;
        IfFailThrow(assembly->GetIHostAssembly()->GetModuleByName(fileName, &pModuleImport));
        
        SafeComHolder<IStream> pIStream;
        IfFailThrow(pModuleImport->GetModuleStream(&pIStream));
        
        DWORD dwModuleId;
        IfFailThrow(pModuleImport->GetModuleId(&dwModuleId));
        image = PEImage::OpenImage(pIStream, assembly->m_identity->m_StreamAsmId,
                                   dwModuleId, (flags & ffContainsNoMetaData));
    }
    else
#endif
    {
        image = PEImage::OpenImage(fileName);
    }
    
    if (flags & ffContainsNoMetaData)
        image->LoadNoMetaData(assembly->IsIntrospectionOnly());
    
    PEModuleHolder module(new PEModule(image, assembly, token, NULL));

    RETURN module.Extract();
}

/* static */
PEModule *PEModule::OpenMemory(PEAssembly *assembly, mdFile token,
                               const void *flat, COUNT_T size)
{
    STANDARD_VM_CONTRACT;

    PEModule *result = NULL;

    EX_TRY
    {
        result = DoOpenMemory(assembly, token, flat, size);
    }
    EX_HOOK
    {
        Exception *ex = GET_EXCEPTION();

        // Rethrow non-transient exceptions as file load exceptions with proper
        // context
        if (!ex->IsTransient())
            EEFileLoadException::Throw(assembly, flat, size, ex->GetHR(), ex);
    }
    EX_END_HOOK;
    return result;
}

// Thread stress
class DoOpenTokenStress : APIThreadStress
{
public:
    PEAssembly *assembly;
    mdFile token;
    const void *flat;
    COUNT_T size;
    DoOpenTokenStress(PEAssembly *assembly, mdFile token,
           const void *flat, COUNT_T size)
        : assembly(assembly), token(token), flat(flat), size(size) {LIMITED_METHOD_CONTRACT;}
    void Invoke()
    {
        WRAPPER_NO_CONTRACT;
        PEModuleHolder result(PEModule::OpenMemory(assembly, token, flat, size));
    }
};

// REVIEW: do we need to know the creator module which emitted the module (separately
// from the assembly parent) for security reasons?
/* static */
PEModule *PEModule::DoOpenMemory(PEAssembly *assembly, mdFile token,
                                 const void *flat, COUNT_T size)
{
    CONTRACT(PEModule *)
    {
        PRECONDITION(CheckPointer(assembly));
        PRECONDITION(!IsNilToken(token));
        PRECONDITION(CheckPointer(flat));
        POSTCONDITION(CheckPointer(RETVAL));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    DoOpenTokenStress ts(assembly, token, flat, size);

    PEImageHolder image(PEImage::LoadFlat(flat, size));

    RETURN new PEModule(image, assembly, token, NULL);
}

/* static */
PEModule *PEModule::Create(PEAssembly *assembly, mdFile token, IMetaDataEmit *pEmit)
{
    CONTRACT(PEModule *)
    {
        PRECONDITION(CheckPointer(assembly));
        PRECONDITION(!IsNilToken(token));
        STANDARD_VM_CHECK; 
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    RETURN new PEModule(NULL, assembly, token, pEmit);
}

// ------------------------------------------------------------
// Logging
// ------------------------------------------------------------
#ifdef FEATURE_PREJIT
void PEModule::ExternalVLog(DWORD facility, DWORD level, const WCHAR *fmt, va_list args)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    m_assembly->ExternalVLog(facility, level, fmt, args);

    RETURN;
}

void PEModule::FlushExternalLog()
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    m_assembly->FlushExternalLog();

    RETURN;
}

// ------------------------------------------------------------
// Loader support routines
// ------------------------------------------------------------
void PEModule::SetNativeImage(const SString &fullPath)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckValue(fullPath));
        PRECONDITION(!fullPath.IsEmpty());
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    PEImageHolder image(PEImage::OpenImage(fullPath));
    image->Load();

    PEFile::SetNativeImage(image);
}
#endif  // FEATURE_PREJIT

#endif // FEATURE_MULTIMODULE_ASSEMBLIES


void PEFile::EnsureImageOpened()
{
    WRAPPER_NO_CONTRACT;
    if (IsDynamic())
        return;
#ifdef FEATURE_PREJIT    
    if(HasNativeImage())
        m_nativeImage->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED)->Release();
    else
#endif        
        GetILimage()->GetLayout(PEImageLayout::LAYOUT_ANY,PEImage::LAYOUT_CREATEIFNEEDED)->Release();
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
PEFile::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // sizeof(PEFile) == 0xb8
    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p PEFile\n", dac_cast<TADDR>(this)));

#ifdef _DEBUG
    // Not a big deal if it's NULL or fails.
    m_debugName.EnumMemoryRegions(flags);
#endif

    if (m_identity.IsValid())
    {
        m_identity->EnumMemoryRegions(flags);
    }
    if (GetILimage().IsValid())
    {
        GetILimage()->EnumMemoryRegions(flags);
    }
#ifdef FEATURE_PREJIT
    if (m_nativeImage.IsValid())
    {
        m_nativeImage->EnumMemoryRegions(flags);
        DacEnumHostDPtrMem(m_nativeImage->GetLoadedLayout()->GetNativeVersionInfo());
    }
#endif
}

void
PEAssembly::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    PEFile::EnumMemoryRegions(flags);

    if (m_creator.IsValid())
    {
        m_creator->EnumMemoryRegions(flags);
    }
}

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
void
PEModule::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;

    PEFile::EnumMemoryRegions(flags);

    if (m_assembly.IsValid())
    {
        m_assembly->EnumMemoryRegions(flags);
    }
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES
#endif // #ifdef DACCESS_COMPILE


//-------------------------------------------------------------------------------
// Make best-case effort to obtain an image name for use in an error message.
//
// This routine must expect to be called before the this object is fully loaded.
// It can return an empty if the name isn't available or the object isn't initialized
// enough to get a name, but it mustn't crash.
//-------------------------------------------------------------------------------
LPCWSTR PEFile::GetPathForErrorMessages()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACTL_END

    if (!IsDynamic())
    {
        return m_identity->GetPathForErrorMessages();
    }
    else
    {
        return W("");
    }
}

#ifndef FEATURE_CORECLR
BOOL PEAssembly::IsReportedToUsageLog()
{
    LIMITED_METHOD_CONTRACT;
    BOOL fReported = TRUE;

    if (!IsDynamic())
        fReported = m_identity->IsReportedToUsageLog();

    return fReported;
}

void PEAssembly::SetReportedToUsageLog()
{
    LIMITED_METHOD_CONTRACT;

    if (!IsDynamic())
        m_identity->SetReportedToUsageLog();
}
#endif // !FEATURE_CORECLR

#ifdef DACCESS_COMPILE
TADDR PEFile::GetMDInternalRWAddress()
{
    if (!m_MDImportIsRW_Debugger_Use_Only)
        return 0;
    else
    {
        // This line of code is a bit scary, but it is correct for now at least...
        // 1) We are using 'm_pMDImport_Use_Accessor' directly, and not the accessor. The field is
        //    named this way to prevent debugger code that wants a host implementation of IMDInternalImport
        //    from accidentally trying to use this pointer. This pointer is a target pointer, not
        //    a host pointer. However in this function we do want the target pointer, so the usage is
        //    accurate.
        // 2) ASSUMPTION: We are assuming that the only valid implementation of RW metadata is 
        //    MDInternalRW. If that ever changes we would need some way to disambiguate, and
        //    probably this entire code path would need to be redesigned. 
        // 3) ASSUMPTION: We are assuming that no pointer adjustment is required to convert between
        //    IMDInternalImport*, IMDInternalImportENC* and MDInternalRW*. Ideally I was hoping to do this with a
        //    static_cast<> but the compiler complains that the ENC<->RW is an unrelated conversion.
        return (TADDR) m_pMDImport_UseAccessor;
    }
}
#endif

#if defined(FEATURE_HOSTED_BINDER)
// Returns the ICLRPrivBinder* instance associated with the PEFile
PTR_ICLRPrivBinder PEFile::GetBindingContext()
{
    LIMITED_METHOD_CONTRACT;
    
    PTR_ICLRPrivBinder pBindingContext = NULL;
    
#if defined(FEATURE_CORECLR)    
    // Mscorlib is always bound in context of the TPA Binder. However, since it gets loaded and published
    // during EEStartup *before* TPAbinder is initialized, we dont have a binding context to publish against.
    // Thus, we will always return NULL for its binding context.
    if (!IsSystem())
#endif // defined(FEATURE_CORECLR)    
    {
        pBindingContext = dac_cast<PTR_ICLRPrivBinder>(GetHostAssembly());
    }
    
    return pBindingContext;
}
#endif // FEATURE_HOSTED_BINDER

