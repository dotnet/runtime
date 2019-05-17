// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: compile.cpp
//

//
// Support for zap compiler and zap files 
// ===========================================================================



#include "common.h"

#ifdef FEATURE_PREJIT 

#include <corcompile.h>

#include "assemblyspec.hpp"

#include "compile.h"
#include "excep.h"
#include "field.h"
#include "eeconfig.h"
#include "zapsig.h"
#include "gcrefmap.h"


#include "virtualcallstub.h"
#include "typeparse.h"
#include "typestring.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "stringarraylist.h"

#ifdef FEATURE_COMINTEROP
#include "clrtocomcall.h"
#include "comtoclrcall.h"
#include "winrttypenameconverter.h"
#endif // FEATURE_COMINTEROP

#include "dllimportcallback.h"
#include "caparser.h"
#include "sigbuilder.h"
#include "cgensys.h"
#include "peimagelayout.inl"


#ifdef FEATURE_COMINTEROP
#include "clrprivbinderwinrt.h"
#include "winrthelpers.h"
#endif

#ifdef CROSSGEN_COMPILE
#include "crossgenroresolvenamespace.h"
#endif

#ifndef NO_NGENPDB
#include <cvinfo.h>
#endif

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

#include "argdestination.h"

#include "versionresilienthashcode.h"
#include "inlinetracking.h"
#include "jithost.h"

#ifdef CROSSGEN_COMPILE
CompilationDomain * theDomain;
#endif

VerboseLevel g_CorCompileVerboseLevel = CORCOMPILE_NO_LOG;

//
// CEECompileInfo implements most of ICorCompileInfo
//

HRESULT CEECompileInfo::Startup(  BOOL fForceDebug,
                                  BOOL fForceProfiling,
                                  BOOL fForceInstrument)
{
    SystemDomain::SetCompilationOverrides(fForceDebug,
                                          fForceProfiling,
                                          fForceInstrument);

    HRESULT hr = S_OK;

    m_fCachingOfInliningHintsEnabled = TRUE;

    _ASSERTE(!g_fEEStarted && !g_fEEInit && "You cannot run the EE inside an NGEN compilation process");

    if (!g_fEEStarted && !g_fEEInit)
    {
#ifdef CROSSGEN_COMPILE
        GetSystemInfo(&g_SystemInfo);

        theDomain = new CompilationDomain(fForceDebug,
                                          fForceProfiling,
                                          fForceInstrument);
#endif

        // When NGEN'ing this call may execute EE code, e.g. the managed code to set up
        // the SharedDomain.
        hr = InitializeEE(COINITEE_DEFAULT);
    }

    //
    // JIT interface expects to be called with
    // preemptive GC enabled
    //
    if (SUCCEEDED(hr)) {
#ifdef _DEBUG
        Thread *pThread = GetThread();
        _ASSERTE(pThread);
#endif

        GCX_PREEMP_NO_DTOR();
    }

    return hr;
}

HRESULT CEECompileInfo::CreateDomain(ICorCompilationDomain **ppDomain,
                                     IMetaDataAssemblyEmit *pEmitter,
                                     BOOL fForceDebug,
                                     BOOL fForceProfiling,
                                     BOOL fForceInstrument)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    CompilationDomain * pCompilationDomain = theDomain;

    {
        SystemDomain::LockHolder lh;
        pCompilationDomain->Init();
    }

    if (pEmitter)
        pCompilationDomain->SetDependencyEmitter(pEmitter);
    

#ifdef DEBUGGING_SUPPORTED 
    // Notify the debugger here, before the thread transitions into the
    // AD to finish the setup, and before any assemblies are loaded into it.
    SystemDomain::PublishAppDomainAndInformDebugger(pCompilationDomain);
#endif // DEBUGGING_SUPPORTED
    
    pCompilationDomain->LoadSystemAssemblies();
    
    pCompilationDomain->SetupSharedStatics();
    
    *ppDomain = static_cast<ICorCompilationDomain*>(pCompilationDomain);
    
    {
        GCX_COOP();

        pCompilationDomain->CreateFusionContext();

        pCompilationDomain->SetFriendlyName(W("Compilation Domain"));
        SystemDomain::System()->LoadDomain(pCompilationDomain);
    }

    COOPERATIVE_TRANSITION_END();

    return S_OK;
}


HRESULT CEECompileInfo::DestroyDomain(ICorCompilationDomain *pDomain)
{
    STANDARD_VM_CONTRACT;

#ifndef CROSSGEN_COMPILE
    COOPERATIVE_TRANSITION_BEGIN();

    GCX_COOP();

    CompilationDomain *pCompilationDomain = (CompilationDomain *) pDomain;

    // DDB 175659: Make sure that canCallNeedsRestore() returns FALSE during compilation 
    // domain shutdown.
    pCompilationDomain->setCannotCallNeedsRestore();

    pCompilationDomain->Unload(TRUE);

    COOPERATIVE_TRANSITION_END();
#endif

    return S_OK;
}

#ifdef TRITON_STRESS_NEED_IMPL
int LogToSvcLogger(LPCWSTR format, ...)
{
    STANDARD_VM_CONTRACT;

    StackSString s;

    va_list args;
    va_start(args, format);
    s.VPrintf(format, args);
    va_end(args);

    GetSvcLogger()->Printf(W("%s"), s.GetUnicode());

    return 0;
}
#endif

HRESULT CEECompileInfo::LoadAssemblyByPath(
    LPCWSTR                  wzPath,
    
    // Normally this is FALSE, but crossgen /CreatePDB sets this to TRUE, so it can
    // explicitly load an NI by path
    BOOL                     fExplicitBindToNativeImage,

    CORINFO_ASSEMBLY_HANDLE *pHandle)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    COOPERATIVE_TRANSITION_BEGIN();

    Assembly * pAssembly;
    HRESULT    hrProcessLibraryBitnessMismatch = S_OK;

    // We don't want to do a LoadFrom, since they do not work with ngen. Instead,
    // read the metadata from the file and do a bind based on that.

    EX_TRY
    {
        // Pre-open the image so we can grab some metadata to help initialize the
        // binder's AssemblySpec, which we'll use later to load the assembly for real.

        PEImageHolder pImage;


        if (pImage == NULL)
        {
            pImage = PEImage::OpenImage(
                wzPath,

                // If we're explicitly binding to an NGEN image, we do not want the cache
                // this PEImage for use later, as pointers that need fixup
                // Normal caching is done when we open it "for real" further down when we
                // call LoadDomainAssembly().
                fExplicitBindToNativeImage ? MDInternalImport_NoCache : MDInternalImport_Default);
        }

        if (fExplicitBindToNativeImage && !pImage->HasReadyToRunHeader())
        {
            pImage->VerifyIsNIAssembly();
        }
        else
        {
            pImage->VerifyIsAssembly();
        }

        // Check to make sure the bitness of the assembly matches the bitness of the process
        // we will be loading it into and store the result.  If a COR_IMAGE_ERROR gets thrown
        // by LoadAssembly then we can blame it on bitness mismatch.  We do the check here
        // and not in the CATCH to distinguish between the COR_IMAGE_ERROR that can be thrown by
        // VerifyIsAssembly (not necessarily a bitness mismatch) and that from LoadAssembly
#ifdef _TARGET_64BIT_
        if (pImage->Has32BitNTHeaders())
        {
            hrProcessLibraryBitnessMismatch = PEFMT_E_32BIT;
        }
#else // !_TARGET_64BIT_
        if (!pImage->Has32BitNTHeaders())
        {
            hrProcessLibraryBitnessMismatch = PEFMT_E_64BIT;
        }
#endif // !_TARGET_64BIT_
        
        AssemblySpec spec;
        spec.InitializeSpec(TokenFromRid(1, mdtAssembly), pImage->GetMDImport(), NULL);

        if (spec.IsMscorlib())
        {
            pAssembly = SystemDomain::System()->SystemAssembly();
        }
        else
        {
            AppDomain * pDomain = AppDomain::GetCurrentDomain();

            PEAssemblyHolder pAssemblyHolder;
            BOOL isWinRT = FALSE;

#ifdef FEATURE_COMINTEROP
            isWinRT = spec.IsContentType_WindowsRuntime();
            if (isWinRT)
            {
                LPCSTR  szNameSpace;
                LPCSTR  szTypeName;
                // It does not make sense to pass the file name to recieve fake type name for empty WinMDs, because we would use the name 
                // for binding in next call to BindAssemblySpec which would fail for fake WinRT type name
                // We will throw/return the error instead and the caller will recognize it and react to it by not creating the ngen image - 
                // see code:Zapper::ComputeDependenciesInCurrentDomain
                IfFailThrow(::GetFirstWinRTTypeDef(pImage->GetMDImport(), &szNameSpace, &szTypeName, NULL, NULL));
                spec.SetWindowsRuntimeType(szNameSpace, szTypeName);
            }
#endif //FEATURE_COMINTEROP

            // If there is a host binder then use it to bind the assembly.
            if (isWinRT)
            {
                pAssemblyHolder = pDomain->BindAssemblySpec(&spec, TRUE, FALSE);
            }
            else
            {
                //ExplicitBind
                CoreBindResult bindResult;
                spec.SetCodeBase(pImage->GetPath());
                spec.Bind(
                    pDomain,
                    TRUE,                   // fThrowOnFileNotFound
                    &bindResult, 

                    // fNgenExplicitBind: Generally during NGEN compilation, this is
                    // TRUE, meaning "I am NGEN, and I am doing an explicit bind to the IL
                    // image, so don't infer the NI and try to open it, because I already
                    // have it open". But if we're executing crossgen /CreatePDB, this should
                    // be FALSE so that downstream code doesn't assume we're explicitly
                    // trying to bind to an IL image (we're actually explicitly trying to
                    // open an NI).
                    !fExplicitBindToNativeImage,

                    // fExplicitBindToNativeImage: Most callers want this FALSE; but crossgen
                    // /CreatePDB explicitly specifies NI names to open, and cannot assume
                    // that IL assemblies will be available.
                    fExplicitBindToNativeImage
                    );
                pAssemblyHolder = PEAssembly::Open(&bindResult,FALSE);
            }

            // Now load assembly into domain.
            DomainAssembly * pDomainAssembly = pDomain->LoadDomainAssembly(&spec, pAssemblyHolder, FILE_LOAD_BEGIN);

            if (spec.CanUseWithBindingCache() && pDomainAssembly->CanUseWithBindingCache())
                pDomain->AddAssemblyToCache(&spec, pDomainAssembly);

            pAssembly = pDomain->LoadAssembly(&spec, pAssemblyHolder, FILE_LOADED);

            // Add a dependency to the current assembly.  This is done to match the behavior
            // of LoadAssemblyFusion, so that the same native image is generated whether we
            // ngen install by file name or by assembly name.
            pDomain->ToCompilationDomain()->AddDependency(&spec, pAssemblyHolder);
        }

        // Kind of a workaround - if we could have loaded this assembly via normal load,

        *pHandle = CORINFO_ASSEMBLY_HANDLE(pAssembly);
    }
    EX_CATCH_HRESULT(hr);
    
    if ( hrProcessLibraryBitnessMismatch != S_OK && ( hr == COR_E_BADIMAGEFORMAT || hr == HRESULT_FROM_WIN32(ERROR_BAD_EXE_FORMAT) ) )
    {
        hr = hrProcessLibraryBitnessMismatch;
    }
    
    COOPERATIVE_TRANSITION_END();

    return hr;
}


#ifdef FEATURE_COMINTEROP
HRESULT CEECompileInfo::LoadTypeRefWinRT(
    IMDInternalImport       *pAssemblyImport,
    mdTypeRef               ref,
    CORINFO_ASSEMBLY_HANDLE *pHandle)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    ReleaseHolder<IAssemblyName> pAssemblyName;

    COOPERATIVE_TRANSITION_BEGIN();
    
    EX_TRY
    {
        Assembly *pAssembly;

        mdToken tkResolutionScope;
        if(FAILED(pAssemblyImport->GetResolutionScopeOfTypeRef(ref, &tkResolutionScope)))
            hr = S_FALSE;
        else if(TypeFromToken(tkResolutionScope) == mdtAssemblyRef)
        {
            DWORD dwAssemblyRefFlags;
            IfFailThrow(pAssemblyImport->GetAssemblyRefProps(tkResolutionScope, NULL, NULL,
                                                     NULL, NULL,
                                                     NULL, NULL, &dwAssemblyRefFlags));
            if (IsAfContentType_WindowsRuntime(dwAssemblyRefFlags))
            {
                LPCSTR psznamespace;
                LPCSTR pszname;
                pAssemblyImport->GetNameOfTypeRef(ref, &psznamespace, &pszname);
                AssemblySpec spec;
                spec.InitializeSpec(tkResolutionScope, pAssemblyImport, NULL);
                spec.SetWindowsRuntimeType(psznamespace, pszname);
                
                _ASSERTE(spec.HasBindableIdentity());
                
                pAssembly = spec.LoadAssembly(FILE_LOADED);

                //
                // Return the module handle
                //

                *pHandle = CORINFO_ASSEMBLY_HANDLE(pAssembly);
            }
            else
            {
                hr = S_FALSE;
            }
        }
        else
        {
            hr = S_FALSE;
        }
    }
    EX_CATCH_HRESULT(hr);

    COOPERATIVE_TRANSITION_END();

    return hr;
}
#endif

BOOL CEECompileInfo::IsInCurrentVersionBubble(CORINFO_MODULE_HANDLE hModule)
{
    WRAPPER_NO_CONTRACT;

    return ((Module*)hModule)->IsInCurrentVersionBubble();
}

HRESULT CEECompileInfo::LoadAssemblyModule(
    CORINFO_ASSEMBLY_HANDLE assembly,
    mdFile                  file,
    CORINFO_MODULE_HANDLE   *pHandle)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    Assembly *pAssembly = (Assembly*) assembly;

    Module *pModule = pAssembly->GetManifestModule()->LoadModule(GetAppDomain(), file, TRUE)->GetModule();

    //
    // Return the module handle
    //

    *pHandle = CORINFO_MODULE_HANDLE(pModule);

    COOPERATIVE_TRANSITION_END();

    return S_OK;
}


BOOL CEECompileInfo::CheckAssemblyZap(
    CORINFO_ASSEMBLY_HANDLE assembly, 
  __out_ecount_opt(*cAssemblyManifestModulePath) 
    LPWSTR                  assemblyManifestModulePath, 
    LPDWORD                 cAssemblyManifestModulePath)
{
    STANDARD_VM_CONTRACT;

    BOOL result = FALSE;

    COOPERATIVE_TRANSITION_BEGIN();

    Assembly *pAssembly = (Assembly*) assembly;

    if (pAssembly->GetManifestFile()->HasNativeImage())
    {
        PEImage *pImage = pAssembly->GetManifestFile()->GetPersistentNativeImage();

        if (assemblyManifestModulePath != NULL)
        {
            DWORD length = pImage->GetPath().GetCount();
            if (length > *cAssemblyManifestModulePath)
            {
                length = *cAssemblyManifestModulePath - 1;
                wcsncpy_s(assemblyManifestModulePath, *cAssemblyManifestModulePath, pImage->GetPath(), length);
                assemblyManifestModulePath[length] = 0;
            }
            else
                wcscpy_s(assemblyManifestModulePath, *cAssemblyManifestModulePath, pImage->GetPath());
        }

        result = TRUE;
    }

    COOPERATIVE_TRANSITION_END();

    return result;
}

HRESULT CEECompileInfo::SetCompilationTarget(CORINFO_ASSEMBLY_HANDLE     assembly,
                                             CORINFO_MODULE_HANDLE       module)
{
    STANDARD_VM_CONTRACT;

    Assembly *pAssembly = (Assembly *) assembly;
    Module *pModule = (Module *) module;

    CompilationDomain *pDomain = (CompilationDomain *) GetAppDomain();
    pDomain->SetTarget(pAssembly, pModule);

    if (!pAssembly->IsSystem())
    {
        // It is possible to get through a compile without calling BindAssemblySpec on mscorlib.  This
        // is because refs to mscorlib are short circuited in a number of places.  So, we will explicitly
        // add it to our dependencies.

        AssemblySpec mscorlib;
        mscorlib.InitializeSpec(SystemDomain::SystemFile());
        GetAppDomain()->BindAssemblySpec(&mscorlib,TRUE,FALSE);

        if (!IsReadyToRunCompilation() && !SystemDomain::SystemFile()->HasNativeImage())
        {
            return NGEN_E_SYS_ASM_NI_MISSING;
        }
    }

    if (IsReadyToRunCompilation() && !pModule->GetFile()->IsILOnly())
    {
        GetSvcLogger()->Printf(LogLevel_Error, W("Error: ReadyToRun is not supported for mixed mode assemblies\n"));
        return E_FAIL;
    }

    return S_OK;
}

IMDInternalImport *
    CEECompileInfo::GetAssemblyMetaDataImport(CORINFO_ASSEMBLY_HANDLE assembly)
{
    STANDARD_VM_CONTRACT;

    IMDInternalImport * import;

    COOPERATIVE_TRANSITION_BEGIN();

    import = ((Assembly*)assembly)->GetManifestImport();
    import->AddRef();

    COOPERATIVE_TRANSITION_END();

    return import;
}

IMDInternalImport *
    CEECompileInfo::GetModuleMetaDataImport(CORINFO_MODULE_HANDLE scope)
{
    STANDARD_VM_CONTRACT;

    IMDInternalImport * import;

    COOPERATIVE_TRANSITION_BEGIN();

    import = ((Module*)scope)->GetMDImport();
    import->AddRef();

    COOPERATIVE_TRANSITION_END();

    return import;
}

CORINFO_MODULE_HANDLE
    CEECompileInfo::GetAssemblyModule(CORINFO_ASSEMBLY_HANDLE assembly)
{
    STANDARD_VM_CONTRACT;

    CANNOTTHROWCOMPLUSEXCEPTION();

    return (CORINFO_MODULE_HANDLE) ((Assembly*)assembly)->GetManifestModule();
}

PEDecoder * CEECompileInfo::GetModuleDecoder(CORINFO_MODULE_HANDLE scope)
{
    STANDARD_VM_CONTRACT;

    PEDecoder *result;

    COOPERATIVE_TRANSITION_BEGIN();

    //
    // Note that we go ahead and return the native image if we are using that.
    // It contains everything we need to ngen.  However, the caller must be
    // aware and check for the native image case, since some fields will need to come
    // from the CORCOMPILE_ZAP_HEADER rather than the PE headers.
    //

    PEFile *pFile = ((Module *) scope)->GetFile();

    if (pFile->HasNativeImage())
        result = pFile->GetLoadedNative();
    else
        result = pFile->GetLoadedIL();

    COOPERATIVE_TRANSITION_END();

    return result;

}

void CEECompileInfo::GetModuleFileName(CORINFO_MODULE_HANDLE scope,
                                       SString               &result)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    result.Set(((Module*)scope)->GetPath());

    COOPERATIVE_TRANSITION_END();
}

CORINFO_ASSEMBLY_HANDLE
    CEECompileInfo::GetModuleAssembly(CORINFO_MODULE_HANDLE module)
{
    STANDARD_VM_CONTRACT;

    CANNOTTHROWCOMPLUSEXCEPTION();

    return (CORINFO_ASSEMBLY_HANDLE) GetModule(module)->GetAssembly();
}


#ifdef CROSSGEN_COMPILE
//
// Small wrapper to avoid having too many crossgen ifdefs
//
class AssemblyForLoadHint
{
    IMDInternalImport * m_pMDImport;
public:
    AssemblyForLoadHint(IMDInternalImport * pMDImport)
        : m_pMDImport(pMDImport)
    {
    }

    IMDInternalImport * GetManifestImport()
    {
        return m_pMDImport;
    }

    LPCSTR GetSimpleName()
    {
        LPCSTR name = "";
        IfFailThrow(m_pMDImport->GetAssemblyProps(TokenFromRid(1, mdtAssembly), NULL, NULL, NULL, &name, NULL, NULL));
        return name;
    }

    void GetDisplayName(SString &result, DWORD flags = 0)
    {
        PEAssembly::GetFullyQualifiedAssemblyName(m_pMDImport, TokenFromRid(1, mdtAssembly), result, flags);
    }

    BOOL IsSystem()
    {
        return FALSE;
    }
};
#endif

//-----------------------------------------------------------------------------
// For an assembly with a full name of "Foo, Version=2.0.0.0, Culture=neutral",
// we want any of these attributes specifications to match:
//    DependencyAttribute("Foo", LoadHint.Always)
//    DependencyAttribute("Foo,", LoadHint.Always)
//    DependencyAttribute("Foo, Version=2.0.0.0, Culture=neutral", LoadHint.Always)
// The second case of "Foo," is needed only for intra-V2 compat as
// it was supported at one point during V2. We may be able to get rid of it.
template <typename ASSEMBLY>
BOOL IsAssemblySpecifiedInCA(ASSEMBLY * pAssembly, SString dependencyNameFromCA)
{
    STANDARD_VM_CONTRACT;

    // First, check for this:
    //    DependencyAttribute("Foo", LoadHint.Always)
    StackSString simpleName(SString::Utf8, pAssembly->GetSimpleName());
    if (simpleName.EqualsCaseInsensitive(dependencyNameFromCA))
        return TRUE;

    // Now, check for this:
    //    DependencyAttribute("Foo,", LoadHint.Always)
    SString comma(W(","));
    StackSString simpleNameWithComma(simpleName, comma);
    if (simpleNameWithComma.EqualsCaseInsensitive(dependencyNameFromCA))
        return TRUE;

    // Finally:
    //    DependencyAttribute("Foo, Version=2.0.0.0, Culture=neutral", LoadHint.Always)
    StackSString fullName;
    pAssembly->GetDisplayName(fullName);
    if (fullName.EqualsCaseInsensitive(dependencyNameFromCA))
        return TRUE;

    return FALSE;
}

template <typename ASSEMBLY>
void GetLoadHint(ASSEMBLY * pAssembly, ASSEMBLY *pAssemblyDependency,
                 LoadHintEnum *loadHint, LoadHintEnum *defaultLoadHint = NULL)
{
    STANDARD_VM_CONTRACT;

    *loadHint = LoadDefault;

    const BYTE  *pbAttr;                // Custom attribute data as a BYTE*.
    ULONG       cbAttr;                 // Size of custom attribute data.
    mdToken     mdAssembly;

    // Look for the binding custom attribute
    {
        IMDInternalImport *pImport = pAssembly->GetManifestImport();

        IfFailThrow(pImport->GetAssemblyFromScope(&mdAssembly));

        MDEnumHolder hEnum(pImport);        // Enumerator for custom attributes
        IfFailThrow(pImport->EnumCustomAttributeByNameInit(mdAssembly, DEPENDENCY_TYPE, &hEnum));

        mdCustomAttribute tkAttribute;      // A custom attribute on this assembly.
        while (pImport->EnumNext(&hEnum, &tkAttribute))
        {
            // Get raw custom attribute.
            IfFailThrow(pImport->GetCustomAttributeAsBlob(tkAttribute, (const void**)&pbAttr, &cbAttr));

            CustomAttributeParser cap(pbAttr, cbAttr);

            IfFailThrow(cap.ValidateProlog());

            // Extract string from custom attribute
            LPCUTF8 szString;
            ULONG   cbString;
            IfFailThrow(cap.GetNonNullString(&szString, &cbString));

            // Convert the string to Unicode.
            StackSString dependencyNameFromCA(SString::Utf8, szString, cbString);

            if (IsAssemblySpecifiedInCA(pAssemblyDependency, dependencyNameFromCA))
            {
                // Get dependency setting
                UINT32 u4;
                IfFailThrow(cap.GetU4(&u4));
                *loadHint = (LoadHintEnum)u4;
                break;
            }
        }
    }

    // If not preference is specified, look for the built-in assembly preference
    if (*loadHint == LoadDefault || defaultLoadHint != NULL)
    {
        IMDInternalImport *pImportDependency = pAssemblyDependency->GetManifestImport();

        IfFailThrow(pImportDependency->GetAssemblyFromScope(&mdAssembly));

        HRESULT hr = pImportDependency->GetCustomAttributeByName(mdAssembly,
            DEFAULTDEPENDENCY_TYPE,
            (const void**)&pbAttr, &cbAttr);
        IfFailThrow(hr);

        // Parse the attribute
        if (hr == S_OK)
        {
            CustomAttributeParser cap(pbAttr, cbAttr);
            IfFailThrow(cap.ValidateProlog());

            // Get default bind setting
            UINT32 u4 = 0;
            IfFailThrow(cap.GetU4(&u4));

            if (defaultLoadHint)
                *defaultLoadHint = (LoadHintEnum) u4;
            else
                *loadHint = (LoadHintEnum) u4;
        }
    }
}

HRESULT CEECompileInfo::GetLoadHint(CORINFO_ASSEMBLY_HANDLE hAssembly,
                                    CORINFO_ASSEMBLY_HANDLE hAssemblyDependency,
                                    LoadHintEnum *loadHint,
                                    LoadHintEnum *defaultLoadHint)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    EX_TRY
    {
        Assembly *pAssembly           = (Assembly *) hAssembly;
        Assembly *pAssemblyDependency = (Assembly *) hAssemblyDependency;

        ::GetLoadHint(pAssembly, pAssemblyDependency, loadHint, defaultLoadHint);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CEECompileInfo::GetAssemblyVersionInfo(CORINFO_ASSEMBLY_HANDLE hAssembly,
                                               CORCOMPILE_VERSION_INFO *pInfo)
{
    STANDARD_VM_CONTRACT;

    Assembly *pAssembly = (Assembly *) hAssembly;

    pAssembly->GetDomainAssembly()->GetCurrentVersionInfo(pInfo);

    return S_OK;
}

void CEECompileInfo::GetAssemblyCodeBase(CORINFO_ASSEMBLY_HANDLE hAssembly, SString &result)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    Assembly *pAssembly = (Assembly *)hAssembly;
    _ASSERTE(pAssembly != NULL);

    pAssembly->GetCodeBase(result);

    COOPERATIVE_TRANSITION_END();
}

//=================================================================================

void FakePromote(PTR_PTR_Object ppObj, ScanContext *pSC, uint32_t dwFlags)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    CORCOMPILE_GCREFMAP_TOKENS newToken = (dwFlags & GC_CALL_INTERIOR) ? GCREFMAP_INTERIOR : GCREFMAP_REF;

    _ASSERTE((*(CORCOMPILE_GCREFMAP_TOKENS *)ppObj == NULL) || (*(CORCOMPILE_GCREFMAP_TOKENS *)ppObj == newToken));

    *(CORCOMPILE_GCREFMAP_TOKENS *)ppObj = newToken;
}

//=================================================================================

void FakePromoteCarefully(promote_func *fn, Object **ppObj, ScanContext *pSC, uint32_t dwFlags)
{
    (*fn)(ppObj, pSC, dwFlags);
}

//=================================================================================

void FakeGcScanRoots(MetaSig& msig, ArgIterator& argit, MethodDesc * pMD, BYTE * pFrame)
{
    STANDARD_VM_CONTRACT;

    ScanContext sc;

    // Encode generic instantiation arg
    if (argit.HasParamType())
    {
        // Note that intrinsic array methods have hidden instantiation arg too, but it is not reported to GC
        if (pMD->RequiresInstMethodDescArg())
            *(CORCOMPILE_GCREFMAP_TOKENS *)(pFrame + argit.GetParamTypeArgOffset()) = GCREFMAP_METHOD_PARAM;
        else 
        if (pMD->RequiresInstMethodTableArg())
            *(CORCOMPILE_GCREFMAP_TOKENS *)(pFrame + argit.GetParamTypeArgOffset()) = GCREFMAP_TYPE_PARAM;
    }

    // If the function has a this pointer, add it to the mask
    if (argit.HasThis())
    {
        BOOL interior = pMD->GetMethodTable()->IsValueType() && !pMD->IsUnboxingStub();

        FakePromote((Object **)(pFrame + argit.GetThisOffset()), &sc, interior ? GC_CALL_INTERIOR : 0);
    }

    if (argit.IsVarArg())
    {
        *(CORCOMPILE_GCREFMAP_TOKENS *)(pFrame + argit.GetVASigCookieOffset()) = GCREFMAP_VASIG_COOKIE;

        // We are done for varargs - the remaining arguments are reported via vasig cookie
        return;
    }

    // Also if the method has a return buffer, then it is the first argument, and could be an interior ref,
    // so always promote it.
    if (argit.HasRetBuffArg())
    {
        FakePromote((Object **)(pFrame + argit.GetRetBuffArgOffset()), &sc, GC_CALL_INTERIOR);
    }

    //
    // Now iterate the arguments
    //

    // Cycle through the arguments, and call msig.GcScanRoots for each
    int argOffset;
    while ((argOffset = argit.GetNextOffset()) != TransitionBlock::InvalidOffset)
    {
        ArgDestination argDest(pFrame, argOffset, argit.GetArgLocDescForStructInRegs());
        msig.GcScanRoots(&argDest, &FakePromote, &sc, &FakePromoteCarefully);
    }
}

void CEECompileInfo::GetCallRefMap(CORINFO_METHOD_HANDLE hMethod, GCRefMapBuilder * pBuilder, bool isDispatchCell)
{
#ifdef _DEBUG
    DWORD dwInitialLength = pBuilder->GetBlobLength();
    UINT nTokensWritten = 0;
#endif

    MethodDesc *pMD = (MethodDesc *)hMethod;

    SigTypeContext typeContext(pMD);
    PCCOR_SIGNATURE pSig;
    DWORD cbSigSize;
    pMD->GetSig(&pSig, &cbSigSize);
    MetaSig msig(pSig, cbSigSize, pMD->GetModule(), &typeContext);

    //
    // Shared default interface methods (i.e. virtual interface methods with an implementation) require
    // an instantiation argument. But if we're in a situation where we haven't resolved the method yet
    // we need to pretent that unresolved default interface methods are like any other interface
    // methods and don't have an instantiation argument.
    // See code:CEEInfo::getMethodSigInternal
    //
    assert(!isDispatchCell || !pMD->RequiresInstArg() || pMD->GetMethodTable()->IsInterface());
    if (pMD->RequiresInstArg() && !isDispatchCell)
    {
        msig.SetHasParamTypeArg();
    }

    ArgIterator argit(&msig);

    UINT nStackBytes = argit.SizeOfFrameArgumentArray();

    // Allocate a fake stack
    CQuickBytes qbFakeStack;
    qbFakeStack.AllocThrows(sizeof(TransitionBlock) + nStackBytes);
    memset(qbFakeStack.Ptr(), 0, qbFakeStack.Size());

    BYTE * pFrame = (BYTE *)qbFakeStack.Ptr();

    // Fill it in
    FakeGcScanRoots(msig, argit, pMD, pFrame);

    //
    // Encode the ref map
    //

    UINT nStackSlots;

#ifdef _TARGET_X86_
    UINT cbStackPop = argit.CbStackPop();
    pBuilder->WriteStackPop(cbStackPop / sizeof(TADDR));

    nStackSlots = nStackBytes / sizeof(TADDR) + NUM_ARGUMENT_REGISTERS;
#else
    nStackSlots = (sizeof(TransitionBlock) + nStackBytes - TransitionBlock::GetOffsetOfFirstGCRefMapSlot()) / TARGET_POINTER_SIZE;
#endif

    for (UINT pos = 0; pos < nStackSlots; pos++)
    {
        int ofs;

#ifdef _TARGET_X86_
        ofs = (pos < NUM_ARGUMENT_REGISTERS) ?
            (TransitionBlock::GetOffsetOfArgumentRegisters() + ARGUMENTREGISTERS_SIZE - (pos + 1) * sizeof(TADDR)) :
            (TransitionBlock::GetOffsetOfArgs() + (pos - NUM_ARGUMENT_REGISTERS) * sizeof(TADDR));
#else
        ofs = TransitionBlock::GetOffsetOfFirstGCRefMapSlot() + pos * TARGET_POINTER_SIZE;
#endif

        CORCOMPILE_GCREFMAP_TOKENS token = *(CORCOMPILE_GCREFMAP_TOKENS *)(pFrame + ofs);

        if (token != 0)
        {
            INDEBUG(nTokensWritten++;)
            pBuilder->WriteToken(pos, token);
        }
    }

    // We are done
    pBuilder->Flush();

#ifdef _DEBUG
    //
    // Verify that decoder produces what got encoded
    //

    DWORD dwFinalLength;
    PVOID pBlob = pBuilder->GetBlob(&dwFinalLength);

    UINT nTokensDecoded = 0;

    GCRefMapDecoder decoder((BYTE *)pBlob + dwInitialLength);

#ifdef _TARGET_X86_
    _ASSERTE(decoder.ReadStackPop() * sizeof(TADDR) == cbStackPop);
#endif

    while (!decoder.AtEnd())
    {
        int pos = decoder.CurrentPos();
        int token = decoder.ReadToken();

        int ofs;

#ifdef _TARGET_X86_
        ofs = (pos < NUM_ARGUMENT_REGISTERS) ?
            (TransitionBlock::GetOffsetOfArgumentRegisters() + ARGUMENTREGISTERS_SIZE - (pos + 1) * sizeof(TADDR)) :
            (TransitionBlock::GetOffsetOfArgs() + (pos - NUM_ARGUMENT_REGISTERS) * sizeof(TADDR));
#else
        ofs = TransitionBlock::GetOffsetOfFirstGCRefMapSlot() + pos * TARGET_POINTER_SIZE;
#endif

        if (token != 0)
        {
            _ASSERTE(*(CORCOMPILE_GCREFMAP_TOKENS *)(pFrame + ofs) == token);
            nTokensDecoded++;
        }
    }

    // Verify that all tokens got decoded.
    _ASSERTE(nTokensWritten == nTokensDecoded);
#endif // _DEBUG
}

void CEECompileInfo::CompressDebugInfo(
    IN ICorDebugInfo::OffsetMapping * pOffsetMapping,
    IN ULONG            iOffsetMapping,
    IN ICorDebugInfo::NativeVarInfo * pNativeVarInfo,
    IN ULONG            iNativeVarInfo,
    IN OUT SBuffer    * pDebugInfoBuffer
    )
{
    STANDARD_VM_CONTRACT;

    CompressDebugInfo::CompressBoundariesAndVars(pOffsetMapping, iOffsetMapping, pNativeVarInfo, iNativeVarInfo, pDebugInfoBuffer, NULL);
}

ICorJitHost* CEECompileInfo::GetJitHost()
{
    return JitHost::getJitHost();
}

HRESULT CEECompileInfo::GetBaseJitFlags(
        IN  CORINFO_METHOD_HANDLE   hMethod,
        OUT CORJIT_FLAGS           *pFlags)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMD = (MethodDesc *)hMethod;
    *pFlags = CEEInfo::GetBaseCompileFlags(pMD);

    return S_OK;
}

//=================================================================================

#ifdef _DEBUG 

static struct
{
    size_t total;
    size_t noEmbed;
    size_t array;
    size_t primitives;
    size_t szarray;
} embedStats;

#endif // _DEBUG

BOOL CEEPreloader::CanEmbedClassID(CORINFO_CLASS_HANDLE    typeHandle)
{
    STANDARD_VM_CONTRACT;

    TypeHandle hnd = (TypeHandle) typeHandle;
    return m_image->CanEagerBindToTypeHandle(hnd) && 
        !hnd.AsMethodTable()->NeedsCrossModuleGenericsStaticsInfo();
}

BOOL CEEPreloader::CanEmbedModuleID(CORINFO_MODULE_HANDLE    moduleHandle)
{
    STANDARD_VM_CONTRACT;

    return m_image->CanEagerBindToModule((Module *)moduleHandle);
}

BOOL CEEPreloader::CanEmbedModuleHandle(CORINFO_MODULE_HANDLE    moduleHandle)
{
    STANDARD_VM_CONTRACT;

    return m_image->CanEagerBindToModule((Module *)moduleHandle);

}
BOOL CEEPreloader::CanEmbedClassHandle(CORINFO_CLASS_HANDLE    typeHandle)
{
    STANDARD_VM_CONTRACT;

    TypeHandle hnd = (TypeHandle) typeHandle;

    BOOL decision = m_image->CanEagerBindToTypeHandle(hnd);

#ifdef _DEBUG 
    embedStats.total++;

    if (!decision)
        embedStats.noEmbed++;

    if (hnd.IsArray())
    {
        embedStats.array++;

        CorElementType arrType = hnd.AsArray()->GetInternalCorElementType();
        if (arrType == ELEMENT_TYPE_SZARRAY)
            embedStats.szarray++;

        CorElementType elemType = hnd.AsArray()->GetArrayElementTypeHandle().GetInternalCorElementType();
        if (elemType <= ELEMENT_TYPE_R8)
            embedStats.primitives++;
    }
#endif // _DEBUG
    return decision;
}


/*static*/ BOOL CanEmbedMethodDescViaContext(MethodDesc * pMethod, MethodDesc * pContext)
{
    STANDARD_VM_CONTRACT;

    if (pContext != NULL)
    {
        _ASSERTE(pContext->GetLoaderModule() == GetAppDomain()->ToCompilationDomain()->GetTargetModule());

        // a method can always embed its own handle
        if (pContext == pMethod)
        {
            return TRUE;
        }

        // Methods that are tightly bound to the same method table can 
        // always refer each other directly. This check allows methods 
        // within one speculative generic instantiations to call each 
        // other directly.
        //
        if ((pContext->GetMethodTable() == pMethod->GetMethodTable()) &&
            pContext->IsTightlyBoundToMethodTable() &&
            pMethod->IsTightlyBoundToMethodTable())
        {
            return TRUE;
        }
    }
    return FALSE;
}

BOOL CEEPreloader::CanEmbedMethodHandle(CORINFO_METHOD_HANDLE methodHandle, 
                                        CORINFO_METHOD_HANDLE contextHandle)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * pContext = GetMethod(contextHandle);
    MethodDesc * pMethod  = GetMethod(methodHandle);

    if (CanEmbedMethodDescViaContext(pMethod, pContext))
        return TRUE;

    return m_image->CanEagerBindToMethodDesc(pMethod);
}

BOOL CEEPreloader::CanEmbedFieldHandle(CORINFO_FIELD_HANDLE    fieldHandle)
{
    STANDARD_VM_CONTRACT;

    return m_image->CanEagerBindToFieldDesc((FieldDesc *) fieldHandle);

}

void* CEECompileInfo::GetStubSize(void *pStubAddress, DWORD *pSizeToCopy)
{
    CONTRACT(void*)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(pStubAddress && pSizeToCopy);
    }
    CONTRACT_END;

    Stub *stub = Stub::RecoverStubAndSize((TADDR)pStubAddress, pSizeToCopy);
    _ASSERTE(*pSizeToCopy > sizeof(Stub));
    RETURN stub;
}

HRESULT CEECompileInfo::GetStubClone(void *pStub, BYTE *pBuffer, DWORD dwBufferSize)
{
    STANDARD_VM_CONTRACT;

    if (pStub == NULL)
    {
        return E_INVALIDARG;
    }

    return (reinterpret_cast<Stub *>(pStub)->CloneStub(pBuffer, dwBufferSize));
}

HRESULT CEECompileInfo::GetTypeDef(CORINFO_CLASS_HANDLE classHandle,
                                   mdTypeDef *token)
{
    STANDARD_VM_CONTRACT;

    CANNOTTHROWCOMPLUSEXCEPTION();

    TypeHandle hClass(classHandle);

    *token = hClass.GetCl();

    return S_OK;
}

HRESULT CEECompileInfo::GetMethodDef(CORINFO_METHOD_HANDLE methodHandle,
                                     mdMethodDef *token)
{
    STANDARD_VM_CONTRACT;

    CANNOTTHROWCOMPLUSEXCEPTION();

    *token = ((MethodDesc*)methodHandle)->GetMemberDef();

    return S_OK;
}

/*********************************************************************/
// Used to determine if a methodHandle can be embedded in an ngen image.
// Depends on what things are persisted by CEEPreloader

BOOL CEEPreloader::CanEmbedFunctionEntryPoint(
    CORINFO_METHOD_HANDLE   methodHandle,
    CORINFO_METHOD_HANDLE   contextHandle, /* = NULL */
    CORINFO_ACCESS_FLAGS    accessFlags /*=CORINFO_ACCESS_ANY*/)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * pMethod = GetMethod(methodHandle);

    // Methods with native callable attribute are special , since 
    // they are used as LDFTN targets.Native Callable methods
    // uses the same code path as reverse pinvoke and embedding them
    // in an ngen image require saving the reverse pinvoke stubs.
    if (pMethod->HasNativeCallableAttribute())
        return FALSE;

    return TRUE;
}

BOOL CEEPreloader::DoesMethodNeedRestoringBeforePrestubIsRun(
        CORINFO_METHOD_HANDLE   methodHandle)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * ftn = GetMethod(methodHandle);

    // The restore mechanism for InstantiatedMethodDescs (IMDs) is complicated, and causes
    // circular dependency complications with the GC if we hardbind to the prestub/precode
    // of an unrestored IMD. As such, we're eliminating hardbinding to unrestored MethodDescs
    // that belong to generic types.

    //@TODO: The reduction may be overkill, and we may consider refining the cases.

    // Specifically, InstantiatedMethodDescs can have preferred zap modules different than
    // the zap modules for their owning types. As such, in a soft-binding case a MethodDesc
    // may not be able to trace back to its owning Module without hitting an unrestored
    // fixup token. For example, the 64-bit JIT can not yet provide generic type arguments
    // and uses instantiating stubs to call static methods on generic types. If such an stub
    // belong to a module other than the module in which the generic type is declared, then
    // it is possible for the MethodTable::m_pEEClass or the EEClass::m_pModule pointers to 
    // be unrestored. The complication arises when a call to the prestub/precode of such 
    // an unrestored IMD causes us try to restore the IMD and this in turn causes us to 
    // transition to preemptive GC and as such GC needs the metadata signature from the IMD 
    // to iterate its arguments. But since we're currently restoring the IMD, we may not be 
    // able to get to the signature, and as such we're stuck.

    // The same problem exists for instantiation arguments. We may need the instantiation
    // arguments while walking the signature during GC, and if they are not restored we're stuck.

    if (ftn->HasClassOrMethodInstantiation())
    {
        if (ftn->NeedsRestore(m_image))
            return TRUE;
    }

    return FALSE;
}

BOOL CEECompileInfo::IsNativeCallableMethod(CORINFO_METHOD_HANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    MethodDesc * pMethod = GetMethod(handle);
    return pMethod->HasNativeCallableAttribute();
}

BOOL CEEPreloader::CanSkipDependencyActivation(CORINFO_METHOD_HANDLE   context,
                                               CORINFO_MODULE_HANDLE   moduleFrom,
                                               CORINFO_MODULE_HANDLE   moduleTo)
{
    STANDARD_VM_CONTRACT;

    // Can't skip any fixups for speculative generic instantiations
    if (Module::GetPreferredZapModuleForMethodDesc(GetMethod(context)) != m_image->GetModule())
        return FALSE;

    // We don't need a fixup for eager bound dependencies since we are going to have 
    // an uncontional one already.
    return m_image->CanEagerBindToModule((Module *)moduleTo);
}

CORINFO_MODULE_HANDLE CEEPreloader::GetPreferredZapModuleForClassHandle(
        CORINFO_CLASS_HANDLE classHnd)
{
    STANDARD_VM_CONTRACT;

    return CORINFO_MODULE_HANDLE(Module::GetPreferredZapModuleForTypeHandle(TypeHandle(classHnd)));
}

// This method is called directly from zapper
extern BOOL CanDeduplicateCode(CORINFO_METHOD_HANDLE method, CORINFO_METHOD_HANDLE duplicateMethod);

BOOL CanDeduplicateCode(CORINFO_METHOD_HANDLE method, CORINFO_METHOD_HANDLE duplicateMethod)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    // For now, the deduplication is supported for IL stubs only
    DynamicMethodDesc * pMethod = GetMethod(method)->AsDynamicMethodDesc();
    DynamicMethodDesc * pDuplicateMethod = GetMethod(duplicateMethod)->AsDynamicMethodDesc();

    //
    // Make sure that the return types match (for code:Thread::HijackThread)
    //

#ifdef _TARGET_X86_
    MetaSig msig1(pMethod);
    MetaSig msig2(pDuplicateMethod);
    if (!msig1.HasFPReturn() != !msig2.HasFPReturn())
        return FALSE;
#endif // _TARGET_X86_

    MetaSig::RETURNTYPE returnType = pMethod->ReturnsObject();
    MetaSig::RETURNTYPE returnTypeDuplicate = pDuplicateMethod->ReturnsObject();

    if (returnType != returnTypeDuplicate)
        return FALSE;

    //
    // Do not enable deduplication of structs returned in registers
    //

    if (returnType == MetaSig::RETVALUETYPE)
        return FALSE;

    //
    // Make sure that the IL stub flags match
    //

    if (pMethod->GetExtendedFlags() != pDuplicateMethod->GetExtendedFlags())
        return FALSE;

    return TRUE;
}

void CEEPreloader::NoteDeduplicatedCode(CORINFO_METHOD_HANDLE method, CORINFO_METHOD_HANDLE duplicateMethod)
{
    STANDARD_VM_CONTRACT;

#ifndef FEATURE_FULL_NGEN // Deduplication
    DuplicateMethodEntry e;
    e.pMD = GetMethod(method);
    e.pDuplicateMD = GetMethod(duplicateMethod);
    m_duplicateMethodsHash.Add(e);
#endif
}

HRESULT CEECompileInfo::GetFieldDef(CORINFO_FIELD_HANDLE fieldHandle,
                                    mdFieldDef *token)
{
    STANDARD_VM_CONTRACT;

    CANNOTTHROWCOMPLUSEXCEPTION();

    *token = ((FieldDesc*)fieldHandle)->GetMemberDef();

    return S_OK;
}

void CEECompileInfo::EncodeModuleAsIndex(CORINFO_MODULE_HANDLE  fromHandle,
                                         CORINFO_MODULE_HANDLE  handle,
                                         DWORD*                 pIndex,
                                         IMetaDataAssemblyEmit* pAssemblyEmit)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    Module *fromModule = GetModule(fromHandle);
    Assembly *fromAssembly = fromModule->GetAssembly();

    Module *module = GetModule(handle);
    Assembly *assembly = module->GetAssembly();

    if (assembly == fromAssembly)
        *pIndex = 0;
    else
    {
        UPTR    result;
        mdToken token;

        CompilationDomain *pDomain = GetAppDomain()->ToCompilationDomain();
    
        RefCache *pRefCache = pDomain->GetRefCache(fromModule);
        if (!pRefCache)
            ThrowOutOfMemory();

        
        if (!assembly->GetManifestFile()->HasBindableIdentity())
        {
            // If the module that we'd like to encode for a later fixup doesn't have
            // a bindable identity, then this will fail at runtime. So, we ask the
            // compilation domain for a matching assembly with a bindable identity.
            // This is possible because this module must have been bound in the past,
            // and the compilation domain will keep track of at least one corresponding
            // bindable identity.
            AssemblySpec defSpec;
            defSpec.InitializeSpec(assembly->GetManifestFile());

            AssemblySpec* pRefSpec = pDomain->FindAssemblyRefSpecForDefSpec(&defSpec);
            _ASSERTE(pRefSpec != nullptr);

            IfFailThrow(pRefSpec->EmitToken(pAssemblyEmit, &token, TRUE, TRUE));
            token += fromModule->GetAssemblyRefMax();
        }
        else
        {
            result = pRefCache->m_sAssemblyRefMap.LookupValue((UPTR)assembly, NULL);

            if (result == (UPTR)INVALIDENTRY)
                token = fromModule->FindAssemblyRef(assembly);
            else
                token = (mdAssemblyRef) result;

            if (IsNilToken(token))
            {
                token = fromAssembly->AddAssemblyRef(assembly, pAssemblyEmit);
                token += fromModule->GetAssemblyRefMax();
            }
        }

        *pIndex = RidFromToken(token);

        pRefCache->m_sAssemblyRefMap.InsertValue((UPTR) assembly, (UPTR)token);
    }

    COOPERATIVE_TRANSITION_END();
}

void CEECompileInfo::EncodeClass(
                         CORINFO_MODULE_HANDLE referencingModule,
                         CORINFO_CLASS_HANDLE  classHandle,
                         SigBuilder *          pSigBuilder,
                         LPVOID                pEncodeModuleContext,
                         ENCODEMODULE_CALLBACK pfnEncodeModule)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th(classHandle);

    ZapSig zapSig((Module *)referencingModule, pEncodeModuleContext, ZapSig::NormalTokens,
                  (EncodeModuleCallback) pfnEncodeModule, NULL);

    COOPERATIVE_TRANSITION_BEGIN();

    BOOL fSuccess;
    fSuccess = zapSig.GetSignatureForTypeHandle(th, pSigBuilder);
    _ASSERTE(fSuccess);

    COOPERATIVE_TRANSITION_END();
}

CORINFO_MODULE_HANDLE CEECompileInfo::GetLoaderModuleForMscorlib()
{
    STANDARD_VM_CONTRACT;

    return CORINFO_MODULE_HANDLE(SystemDomain::SystemModule());
}

CORINFO_MODULE_HANDLE CEECompileInfo::GetLoaderModuleForEmbeddableType(CORINFO_CLASS_HANDLE clsHnd)
{
    STANDARD_VM_CONTRACT;

    TypeHandle t = TypeHandle(clsHnd);
    return CORINFO_MODULE_HANDLE(t.GetLoaderModule());
}

CORINFO_MODULE_HANDLE CEECompileInfo::GetLoaderModuleForEmbeddableMethod(CORINFO_METHOD_HANDLE methHnd)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMD = GetMethod(methHnd);
    return CORINFO_MODULE_HANDLE(pMD->GetLoaderModule());
}

CORINFO_MODULE_HANDLE CEECompileInfo::GetLoaderModuleForEmbeddableField(CORINFO_FIELD_HANDLE fieldHnd)
{
    STANDARD_VM_CONTRACT;

    FieldDesc *pFD = (FieldDesc *) fieldHnd;
    return CORINFO_MODULE_HANDLE(pFD->GetLoaderModule());
}

void CEECompileInfo::EncodeMethod(
                          CORINFO_MODULE_HANDLE referencingModule,
                          CORINFO_METHOD_HANDLE handle,
                          SigBuilder *          pSigBuilder,
                          LPVOID                pEncodeModuleContext,
                          ENCODEMODULE_CALLBACK pfnEncodeModule,
                          CORINFO_RESOLVED_TOKEN * pResolvedToken,
                          CORINFO_RESOLVED_TOKEN * pConstrainedResolvedToken,
                          BOOL                  fEncodeUsingResolvedTokenSpecStreams)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();
    MethodDesc *pMethod = GetMethod(handle);

    BOOL fSuccess;
    fSuccess = ZapSig::EncodeMethod(pMethod, 
                              (Module *) referencingModule,
                              pSigBuilder,
                              pEncodeModuleContext, 
                              pfnEncodeModule, NULL,
                              pResolvedToken, pConstrainedResolvedToken,
                              fEncodeUsingResolvedTokenSpecStreams);
    _ASSERTE(fSuccess);

    COOPERATIVE_TRANSITION_END();
}

mdToken CEECompileInfo::TryEncodeMethodAsToken(
                CORINFO_METHOD_HANDLE handle,
                CORINFO_RESOLVED_TOKEN * pResolvedToken,
                CORINFO_MODULE_HANDLE * referencingModule)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * pMethod = GetMethod(handle);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        _ASSERTE(pResolvedToken != NULL);

        Module * pReferencingModule = (Module *)pResolvedToken->tokenScope;

        if (!pReferencingModule->IsInCurrentVersionBubble())
            return mdTokenNil;

        // If this is a MemberRef with TypeSpec, we might come to here because we resolved the method
        // into a non-generic base class in the same version bubble. However, since we don't have the
        // proper type context during ExternalMethodFixupWorker, we can't really encode using token
        if (pResolvedToken->pTypeSpec != NULL)
            return mdTokenNil;
            
        unsigned methodToken = pResolvedToken->token;

        switch (TypeFromToken(methodToken))
        {
        case mdtMethodDef:
            if (pReferencingModule->LookupMethodDef(methodToken) != pMethod)
                return mdTokenNil;
            break;

        case mdtMemberRef:
            if (pReferencingModule->LookupMemberRefAsMethod(methodToken) != pMethod)
                return mdTokenNil;
            break;

        default:
            return mdTokenNil;
        }

        *referencingModule = CORINFO_MODULE_HANDLE(pReferencingModule);
        return methodToken;
    }
#endif // FEATURE_READYTORUN_COMPILER

    Module *pModule = pMethod->GetModule();
    if (!pModule->IsInCurrentVersionBubble())
    {
        Module * pTargetModule = GetAppDomain()->ToCompilationDomain()->GetTargetModule();
        *referencingModule = CORINFO_MODULE_HANDLE(pTargetModule);
        return pTargetModule->LookupMemberRefByMethodDesc(pMethod);
    }
    else
    {
        mdToken defToken = pMethod->GetMemberDef();
        if (pModule->LookupMethodDef(defToken) == pMethod)
        {
            *referencingModule = CORINFO_MODULE_HANDLE(pModule);
            return defToken;
        }
    }

    return mdTokenNil;
}

DWORD CEECompileInfo::TryEncodeMethodSlot(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * pMethod = GetMethod(handle);

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        // We can only encode real interface methods as slots
        if (!pMethod->IsInterface() || pMethod->IsStatic())
            return (DWORD)-1;

        // And only if the interface lives in the current version bubble
        // If may be possible to relax this restriction if we can guarantee that the external interfaces are
        // really not changing. We will play it safe for now.
        if (!pMethod->GetModule()->IsInCurrentVersionBubble())
            return (DWORD)-1;
    }
#endif

    return pMethod->GetSlot();
}

void EncodeTypeInDictionarySignature(
            Module * pInfoModule,
            SigPointer ptr, 
            SigBuilder * pSigBuilder,
            LPVOID encodeContext,
            ENCODEMODULE_CALLBACK pfnEncodeModule)
{
    STANDARD_VM_CONTRACT;

    CorElementType typ = ELEMENT_TYPE_END;
    IfFailThrow(ptr.GetElemType(&typ));

    if (typ == ELEMENT_TYPE_INTERNAL)
    {
        TypeHandle th;

        IfFailThrow(ptr.GetPointer((void**)&th));

        ZapSig zapSig(pInfoModule, encodeContext, ZapSig::NormalTokens,
                      (EncodeModuleCallback) pfnEncodeModule, NULL);

        //
        // Write class
        //
        BOOL fSuccess;
        fSuccess = zapSig.GetSignatureForTypeHandle(th, pSigBuilder);
        _ASSERTE(fSuccess);

        return;
    }
    else
    if (typ == ELEMENT_TYPE_GENERICINST)
    {
        //
        // SigParser expects ELEMENT_TYPE_MODULE_ZAPSIG to be before ELEMENT_TYPE_GENERICINST
        //
        SigPointer peek(ptr);
        ULONG instType = 0;
        IfFailThrow(peek.GetData(&instType));
        _ASSERTE(instType == ELEMENT_TYPE_INTERNAL);

        TypeHandle th;
        IfFailThrow(peek.GetPointer((void **)&th));

        Module * pTypeHandleModule = th.GetModule();

        if (!pTypeHandleModule->IsInCurrentVersionBubble())
        {
            pTypeHandleModule = GetAppDomain()->ToCompilationDomain()->GetTargetModule();
        }

        if (pTypeHandleModule != pInfoModule)
        {
            DWORD index = pfnEncodeModule(encodeContext, (CORINFO_MODULE_HANDLE)pTypeHandleModule);
            _ASSERTE(index != ENCODE_MODULE_FAILED);

            pSigBuilder->AppendElementType((CorElementType) ELEMENT_TYPE_MODULE_ZAPSIG);
            pSigBuilder->AppendData(index);
        }

        pSigBuilder->AppendElementType(ELEMENT_TYPE_GENERICINST);

        EncodeTypeInDictionarySignature(pTypeHandleModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
        IfFailThrow(ptr.SkipExactlyOne());

        ULONG argCnt = 0; // Get number of parameters
        IfFailThrow(ptr.GetData(&argCnt));
        pSigBuilder->AppendData(argCnt);

        while (argCnt--)
        {
            EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
            IfFailThrow(ptr.SkipExactlyOne());
        }

        return;
    }
    else if((CorElementTypeZapSig)typ == ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG)
    {
        pSigBuilder->AppendElementType((CorElementType)ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG);

        IfFailThrow(ptr.GetElemType(&typ));

        _ASSERTE(typ == ELEMENT_TYPE_SZARRAY || typ == ELEMENT_TYPE_ARRAY);
    }

    pSigBuilder->AppendElementType(typ);

    if (!CorIsPrimitiveType(typ))
    {
        switch (typ)
        {
            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_MVAR:
                {
                    ULONG varNum;
                    // Skip variable number
                    IfFailThrow(ptr.GetData(&varNum));
                    pSigBuilder->AppendData(varNum);
                }
                break;
            case ELEMENT_TYPE_OBJECT:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_TYPEDBYREF:
                break;

            case ELEMENT_TYPE_BYREF: //fallthru
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_PINNED:
            case ELEMENT_TYPE_SZARRAY:
                EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
                IfFailThrow(ptr.SkipExactlyOne());
                break;

            case ELEMENT_TYPE_ARRAY:
                {
                    EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
                    IfFailThrow(ptr.SkipExactlyOne());

                    ULONG rank = 0; // Get rank
                    IfFailThrow(ptr.GetData(&rank));
                    pSigBuilder->AppendData(rank);

                    if (rank)
                    {
                        ULONG nsizes = 0;
                        IfFailThrow(ptr.GetData(&nsizes));
                        pSigBuilder->AppendData(nsizes);

                        while (nsizes--)
                        {
                            ULONG data = 0;
                            IfFailThrow(ptr.GetData(&data));
                            pSigBuilder->AppendData(data);
                        }

                        ULONG nlbounds = 0;
                        IfFailThrow(ptr.GetData(&nlbounds));
                        pSigBuilder->AppendData(nlbounds);

                        while (nlbounds--)
                        {
                            ULONG data = 0;
                            IfFailThrow(ptr.GetData(&data));
                            pSigBuilder->AppendData(data);
                        }
                    }
                }
                break;

            default:
                _ASSERTE(!"Unexpected element in signature");
        }
    }
}

void CEECompileInfo::EncodeGenericSignature(
            LPVOID signature,
            BOOL fMethod,
            SigBuilder * pSigBuilder,
            LPVOID encodeContext,
            ENCODEMODULE_CALLBACK pfnEncodeModule)
{
    STANDARD_VM_CONTRACT;

    Module * pInfoModule = MscorlibBinder::GetModule();

    SigPointer ptr((PCCOR_SIGNATURE)signature);

    ULONG entryKind; // DictionaryEntryKind
    IfFailThrow(ptr.GetData(&entryKind));
    pSigBuilder->AppendData(entryKind);

    if (!fMethod)
    {
        ULONG dictionaryIndex = 0;
        IfFailThrow(ptr.GetData(&dictionaryIndex));

        pSigBuilder->AppendData(dictionaryIndex);
    }

    switch (entryKind)
    {
    case DeclaringTypeHandleSlot:
        EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
        IfFailThrow(ptr.SkipExactlyOne());
        // fall through

    case TypeHandleSlot:
        EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
        IfFailThrow(ptr.SkipExactlyOne());
        break;

    case ConstrainedMethodEntrySlot:
        EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
        IfFailThrow(ptr.SkipExactlyOne());
        // fall through

    case MethodDescSlot:
    case MethodEntrySlot:
    case DispatchStubAddrSlot:
        {
            EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
            IfFailThrow(ptr.SkipExactlyOne());

            ULONG methodFlags;
            IfFailThrow(ptr.GetData(&methodFlags));
            pSigBuilder->AppendData(methodFlags);

            if ((methodFlags & ENCODE_METHOD_SIG_SlotInsteadOfToken) == 0)
            {
                EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
                IfFailThrow(ptr.SkipExactlyOne());
            }
            
            ULONG tokenOrSlot;
            IfFailThrow(ptr.GetData(&tokenOrSlot));
            pSigBuilder->AppendData(tokenOrSlot);

            if (methodFlags & ENCODE_METHOD_SIG_MethodInstantiation)
            {
                DWORD nGenericMethodArgs;
                IfFailThrow(ptr.GetData(&nGenericMethodArgs));
                pSigBuilder->AppendData(nGenericMethodArgs);

                for (DWORD i = 0; i < nGenericMethodArgs; i++)
                {
                    EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
                    IfFailThrow(ptr.SkipExactlyOne());
                }
            }
        }
        break;

    case FieldDescSlot:
        {
            EncodeTypeInDictionarySignature(pInfoModule, ptr, pSigBuilder, encodeContext, pfnEncodeModule);
            IfFailThrow(ptr.SkipExactlyOne());

            DWORD fieldIndex;
            IfFailThrow(ptr.GetData(&fieldIndex));
            pSigBuilder->AppendData(fieldIndex);
        }
        break;

    default:
        _ASSERTE(false);
    }

    ULONG dictionarySlot;
    IfFailThrow(ptr.GetData(&dictionarySlot));
    pSigBuilder->AppendData(dictionarySlot);
}

void CEECompileInfo::EncodeField(
                         CORINFO_MODULE_HANDLE referencingModule,
                         CORINFO_FIELD_HANDLE  handle,
                         SigBuilder *          pSigBuilder,
                         LPVOID                encodeContext,
                         ENCODEMODULE_CALLBACK pfnEncodeModule,
                         CORINFO_RESOLVED_TOKEN * pResolvedToken, 
                         BOOL fEncodeUsingResolvedTokenSpecStreams)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    ZapSig::EncodeField(GetField(handle), 
                        (Module *) referencingModule,
                        pSigBuilder,
                        encodeContext, 
                        pfnEncodeModule,
                        pResolvedToken,
                        fEncodeUsingResolvedTokenSpecStreams);

    COOPERATIVE_TRANSITION_END();
}

BOOL CEECompileInfo::IsEmptyString(mdString token,
                                   CORINFO_MODULE_HANDLE module)
{
    STANDARD_VM_CONTRACT;

    BOOL fRet = FALSE;

    COOPERATIVE_TRANSITION_BEGIN();

    EEStringData strData;
    ((Module *)module)->InitializeStringData(token, &strData, NULL);
    fRet = (strData.GetCharCount() == 0);

    COOPERATIVE_TRANSITION_END();

    return fRet;
}

#ifdef FEATURE_READYTORUN_COMPILER
CORCOMPILE_FIXUP_BLOB_KIND CEECompileInfo::GetFieldBaseOffset(
        CORINFO_CLASS_HANDLE classHnd,
        DWORD * pBaseOffset)
{
    STANDARD_VM_CONTRACT;

    MethodTable * pMT = (MethodTable *)classHnd;
    Module * pModule = pMT->GetModule();

    if (!pMT->IsLayoutFixedInCurrentVersionBubble())
    {
        return pMT->IsValueType() ? ENCODE_CHECK_FIELD_OFFSET : ENCODE_FIELD_OFFSET;
    }

    if (pMT->IsValueType())
    {
        return ENCODE_NONE;
    }

    if (pMT->GetParentMethodTable()->IsInheritanceChainLayoutFixedInCurrentVersionBubble())
    {
        return ENCODE_NONE;
    }

    if (pMT->HasLayout())
    {
        // We won't try to be smart for classes with layout.
        // They are complex to get right, and very rare anyway.
        return ENCODE_FIELD_OFFSET;
    }

    *pBaseOffset = ReadyToRunInfo::GetFieldBaseOffset(pMT);
    return ENCODE_FIELD_BASE_OFFSET;
}

BOOL CEECompileInfo::NeedsTypeLayoutCheck(CORINFO_CLASS_HANDLE classHnd)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th(classHnd);

    if (th.IsTypeDesc())
        return FALSE;

    MethodTable * pMT = th.AsMethodTable();

    if (!pMT->IsValueType())
        return FALSE;

    // Skip this check for equivalent types. Equivalent types are used for interop that ensures
    // matching layout.
    if (pMT->GetClass()->IsEquivalentType())
        return FALSE;

    return !pMT->IsLayoutFixedInCurrentVersionBubble();
}

extern void ComputeGCRefMap(MethodTable * pMT, BYTE * pGCRefMap, size_t cbGCRefMap);

void CEECompileInfo::EncodeTypeLayout(CORINFO_CLASS_HANDLE classHandle, SigBuilder * pSigBuilder)
{
    STANDARD_VM_CONTRACT;

    MethodTable * pMT = TypeHandle(classHandle).AsMethodTable();
    _ASSERTE(pMT->IsValueType());

    DWORD dwSize = pMT->GetNumInstanceFieldBytes();
    DWORD dwAlignment = CEEInfo::getClassAlignmentRequirementStatic(pMT);

    DWORD dwFlags = 0;

#ifdef FEATURE_HFA
    if (pMT->IsHFA())
        dwFlags |= READYTORUN_LAYOUT_HFA;
#endif

    // Check everything 
    dwFlags |= READYTORUN_LAYOUT_Alignment;
    if (dwAlignment == TARGET_POINTER_SIZE)
        dwFlags |= READYTORUN_LAYOUT_Alignment_Native;

    dwFlags |= READYTORUN_LAYOUT_GCLayout;
    if (!pMT->ContainsPointers())
        dwFlags |= READYTORUN_LAYOUT_GCLayout_Empty;

    pSigBuilder->AppendData(dwFlags);

    // Size is checked unconditionally
    pSigBuilder->AppendData(dwSize);

#ifdef FEATURE_HFA
    if (dwFlags & READYTORUN_LAYOUT_HFA)
    {
        pSigBuilder->AppendData(pMT->GetHFAType());
    }
#endif

    if ((dwFlags & READYTORUN_LAYOUT_Alignment) && !(dwFlags & READYTORUN_LAYOUT_Alignment_Native))
    {
        pSigBuilder->AppendData(dwAlignment);
    }

    if ((dwFlags & READYTORUN_LAYOUT_GCLayout) && !(dwFlags & READYTORUN_LAYOUT_GCLayout_Empty))
    {
        size_t cbGCRefMap = (dwSize / TARGET_POINTER_SIZE + 7) / 8;
        _ASSERTE(cbGCRefMap > 0);

        BYTE * pGCRefMap = (BYTE *)_alloca(cbGCRefMap);

        ComputeGCRefMap(pMT, pGCRefMap, cbGCRefMap);

        for (size_t i = 0; i < cbGCRefMap; i++)
            pSigBuilder->AppendByte(pGCRefMap[i]);
    }
}

BOOL CEECompileInfo::AreAllClassesFullyLoaded(CORINFO_MODULE_HANDLE moduleHandle)
{
    STANDARD_VM_CONTRACT;

    return ((Module *)moduleHandle)->AreAllClassesFullyLoaded();
}

int CEECompileInfo::GetVersionResilientTypeHashCode(CORINFO_MODULE_HANDLE moduleHandle, mdToken token)
{
    STANDARD_VM_CONTRACT;

    int dwHashCode;
    if (!::GetVersionResilientTypeHashCode(((Module *)moduleHandle)->GetMDImport(), token, &dwHashCode))
        ThrowHR(COR_E_BADIMAGEFORMAT);

    return dwHashCode;
}

int CEECompileInfo::GetVersionResilientMethodHashCode(CORINFO_METHOD_HANDLE methodHandle)
{
    STANDARD_VM_CONTRACT;

    return ::GetVersionResilientMethodHashCode(GetMethod(methodHandle));
}

#endif // FEATURE_READYTORUN_COMPILER

BOOL CEECompileInfo::HasCustomAttribute(CORINFO_METHOD_HANDLE method, LPCSTR customAttributeName)
{
    STANDARD_VM_CONTRACT;

    MethodDesc * pMD = GetMethod(method);
    return S_OK == pMD->GetMDImport()->GetCustomAttributeByName(pMD->GetMemberDef(), customAttributeName, NULL, NULL);
}

#define OMFConst_Read            0x0001
#define OMFConst_Write           0x0002
#define OMFConst_Exec            0x0004
#define OMFConst_F32Bit          0x0008
#define OMFConst_ReservedBits1   0x00f0
#define OMFConst_FSel            0x0100
#define OMFConst_FAbs            0x0200
#define OMFConst_ReservedBits2   0x0C00
#define OMFConst_FGroup          0x1000
#define OMFConst_ReservedBits3   0xE000

#define OMF_StandardText  (OMFConst_FSel|OMFConst_F32Bit|OMFConst_Exec|OMFConst_Read) // 0x10D
#define OMF_SentinelType  (OMFConst_FAbs|OMFConst_F32Bit) // 0x208


// ----------------------------------------------------------------------------
// NGEN PDB SUPPORT
// 
// The NGEN PDB format consists of structs stacked together into buffers, which are
// passed to the PDB API. For a description of the structures, see
// InternalApis\vctools\inc\cvinfo.h.
// 
// The interface to the PDB used below is NGEN-specific, and is exposed via
// diasymreader.dll. For a description of this interface, see ISymNGenWriter2 inside
// public\devdiv\inc\corsym.h and debugger\sh\symwrtr\ngenpdbwriter.h,cpp
// ----------------------------------------------------------------------------

#if defined(NO_NGENPDB) && !defined(FEATURE_PERFMAP)
BOOL CEECompileInfo::GetIsGeneratingNgenPDB() 
{
    return FALSE; 
}

void CEECompileInfo::SetIsGeneratingNgenPDB(BOOL fGeneratingNgenPDB) 
{
}

BOOL IsNgenPDBCompilationProcess()
{
    return FALSE;
}
#else
BOOL CEECompileInfo::GetIsGeneratingNgenPDB() 
{
    LIMITED_METHOD_DAC_CONTRACT;
    return m_fGeneratingNgenPDB; 
}

void CEECompileInfo::SetIsGeneratingNgenPDB(BOOL fGeneratingNgenPDB) 
{
    LIMITED_METHOD_DAC_CONTRACT;
    m_fGeneratingNgenPDB = fGeneratingNgenPDB; 
}

BOOL IsNgenPDBCompilationProcess()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return IsCompilationProcess() && g_pCEECompileInfo->GetIsGeneratingNgenPDB();
}

#endif // NO_NGENPDB && !FEATURE_PERFMAP

#ifndef NO_NGENPDB
// This is the prototype of "CreateNGenPdbWriter" exported by diasymreader.dll 
typedef HRESULT (__stdcall *CreateNGenPdbWriter_t)(const WCHAR *pwszNGenImagePath, const WCHAR *pwszPdbPath, void **ppvObj);

// Allocator to specify when requesting boundaries information for PDB
BYTE* SimpleNew(void *, size_t cBytes)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BYTE * p = new BYTE[cBytes];
    return p;
}

// PDB convention has any IPs that don't map to source code (e.g., prolog, epilog, etc.)
// to be mapped to line number "0xFeeFee".
const int kUnmappedIP = 0xFeeFee;


// ----------------------------------------------------------------------------
// Simple pair of offsets for each source file name.  Pair includes its offset into the
// PDB string table, and its offset in the files checksum table.
// 
struct DocNameOffsets
{
    ULONG32 m_dwStrTableOffset;
    ULONG32 m_dwChksumTableOffset;
    DocNameOffsets(ULONG32 dwStrTableOffset, ULONG32 dwChksumTableOffset)
        : m_dwStrTableOffset(dwStrTableOffset), m_dwChksumTableOffset(dwChksumTableOffset)
    {
        LIMITED_METHOD_CONTRACT;
    }

    DocNameOffsets()
        : m_dwStrTableOffset((ULONG32) -1), m_dwChksumTableOffset((ULONG32) -1)
    {
        LIMITED_METHOD_CONTRACT;
    }
};


// ----------------------------------------------------------------------------
// This is used when creating the hash table which maps source file names to
// DocNameOffsets instances.  The only interesting stuff here is that:
//     * Equality is determined by a case-insensitive comparison on the source file
//         names
//     * Hashing is done by hashing the source file names
//     
struct DocNameToOffsetMapTraits : public NoRemoveSHashTraits < MapSHashTraits<LPCSTR, DocNameOffsets> >
{
public:
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;

        if (k1 == NULL && k2 == NULL)
            return TRUE;
        if (k1 == NULL || k2 == NULL)
            return FALSE;
        return _stricmp(k1, k2) == 0;
    }

    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;

        if (k == NULL)
            return 0;
        else
            return HashiStringA(k);
    }

    typedef LPCSTR KEY;
    typedef DocNameOffsets VALUE;
    typedef NoRemoveSHashTraits < MapSHashTraits<LPCSTR, DocNameOffsets> > PARENT;
    typedef PARENT::element_t element_t;
    static const element_t Null() { LIMITED_METHOD_CONTRACT; return element_t((KEY)0,VALUE((ULONG32) -1, (ULONG32) -1)); }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.Key() == (KEY)0; }
};


// ----------------------------------------------------------------------------
// Hash table that maps the UTF-8 string of a source file name to its corresponding
// DocNameToOffsetMapTraits
// 
class DocNameToOffsetMap : public SHash<DocNameToOffsetMapTraits>
{
    typedef SHash<DocNameToOffsetMapTraits> PARENT;
    typedef LPCSTR KEY;
    typedef DocNameOffsets VALUE;
    
public:
    void Add(KEY key, VALUE value)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            PRECONDITION(key != (KEY)0);
        }
        CONTRACTL_END;

        PARENT::Add(KeyValuePair<KEY,VALUE>(key, value));
    }

    void AddOrReplace(KEY key, VALUE value)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            PRECONDITION(key != (KEY)0);
        }
        CONTRACTL_END;

        PARENT::AddOrReplace(KeyValuePair<KEY,VALUE>(key, value));
    }

    BOOL Lookup(KEY key, VALUE* pValue)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(key != (KEY)0);
        }
        CONTRACTL_END;

        const KeyValuePair<KEY,VALUE> *pRet = PARENT::LookupPtr(key);
        if (pRet == NULL)
            return FALSE;

        *pValue = pRet->Value();
        return TRUE;
    }
};

// ----------------------------------------------------------------------------
// Simple class to sort ICorDebugInfo::OffsetMapping arrays by IL offset
// 
class QuickSortILNativeMapByIL : public CQuickSort<ICorDebugInfo::OffsetMapping>
{
  public:
    QuickSortILNativeMapByIL(
        ICorDebugInfo::OffsetMapping * rgMap,
        int cEntries)
      : CQuickSort<ICorDebugInfo::OffsetMapping>(rgMap, cEntries)
    {
        LIMITED_METHOD_CONTRACT;
    }

    int Compare(ICorDebugInfo::OffsetMapping * pFirst,
                ICorDebugInfo::OffsetMapping * pSecond)
    {
        LIMITED_METHOD_CONTRACT;

        if (pFirst->ilOffset < pSecond->ilOffset)
            return -1;
        else if (pFirst->ilOffset == pSecond->ilOffset)
            return 0;
        else
            return 1;
    }
};

// ----------------------------------------------------------------------------
// Simple class to sort IL to Native mapping arrays by Native offset
// 
class QuickSortILNativeMapByNativeOffset : public CQuickSort<ICorDebugInfo::OffsetMapping>
{
public:
    QuickSortILNativeMapByNativeOffset(
        ICorDebugInfo::OffsetMapping * rgMap,
        int cEntries)
        : CQuickSort<ICorDebugInfo::OffsetMapping>(rgMap, cEntries)
    {
        LIMITED_METHOD_CONTRACT;
    }

    int Compare(ICorDebugInfo::OffsetMapping * pFirst,
        ICorDebugInfo::OffsetMapping * pSecond)
    {
        LIMITED_METHOD_CONTRACT;

        if (pFirst->nativeOffset < pSecond->nativeOffset)
            return -1;
        else if (pFirst->nativeOffset == pSecond->nativeOffset)
            return 0;
        else
            return 1;
    }
};

// ----------------------------------------------------------------------------
// Simple structure used when merging the JIT manager's IL-to-native maps
// (ICorDebugInfo::OffsetMapping) with the IL PDB's source-to-IL map.
// 
struct MapIndexPair
{
public:
    // Index into ICorDebugInfo::OffsetMapping
    ULONG32 m_iIlNativeMap;
    
    // Corresponding index into the IL PDB's sequence point arrays
    ULONG32 m_iSeqPoints;

    MapIndexPair() : 
        m_iIlNativeMap((ULONG32) -1), 
        m_iSeqPoints((ULONG32) -1)
    {
        LIMITED_METHOD_CONTRACT;
    }
};

// ----------------------------------------------------------------------------
// Simple class to sort MapIndexPairs by native IP offset. A MapIndexPair sorts "earlier"
// if its m_iIlNativeMap index gives you an IP offset (i.e.,
// m_rgIlNativeMap[m_iIlNativeMap].nativeOffset) that is smaller.
// 
class QuickSortMapIndexPairsByNativeOffset : public CQuickSort<MapIndexPair>
{
  public:
    QuickSortMapIndexPairsByNativeOffset(
        MapIndexPair * rgMap, 
        int cEntries, 
        ICorDebugInfo::OffsetMapping * rgIlNativeMap,
        ULONG32 cIlNativeMap)
        : CQuickSort<MapIndexPair>(rgMap, cEntries),
          m_rgIlNativeMap(rgIlNativeMap),
          m_cIlNativeMap(cIlNativeMap)
    {
        LIMITED_METHOD_CONTRACT;
    }

    int Compare(MapIndexPair * pFirst,
                MapIndexPair * pSecond)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(pFirst->m_iIlNativeMap < m_cIlNativeMap);
        _ASSERTE(pSecond->m_iIlNativeMap < m_cIlNativeMap);

        DWORD dwFirstNativeOffset = m_rgIlNativeMap[pFirst->m_iIlNativeMap].nativeOffset;
        DWORD dwSecondNativeOffset = m_rgIlNativeMap[pSecond->m_iIlNativeMap].nativeOffset;

        if (dwFirstNativeOffset < dwSecondNativeOffset)
            return -1;
        else if (dwFirstNativeOffset == dwSecondNativeOffset)
            return 0;
        else
            return 1;
    }

protected:
    ICorDebugInfo::OffsetMapping * m_rgIlNativeMap;
    ULONG32 m_cIlNativeMap;
};

// ----------------------------------------------------------------------------
// The following 3 classes contain the code to generate PDBs
// 

// NGEN always generates PDBs with public symbols lists (so tools can map IP ranges to
// methods).  This bitmask indicates what extra info should be added to the PDB
enum PDBExtraData
{
    // Add string table subsection, files checksum subsection, and lines subsection to
    // allow tools to map IP ranges to source lines.
    kPDBLines  = 0x00000001,
};


// ----------------------------------------------------------------------------
// Manages generating all PDB data for an NGENd image.  One of these is instantiated per
// run of "ngen createpdb"
// 
class NGenPdbWriter
{
private:
    CreateNGenPdbWriter_t m_Create;
    HMODULE m_hModule;
    ReleaseHolder<ISymUnmanagedBinder> m_pBinder;
    LPCWSTR m_wszPdbPath;
    DWORD m_dwExtraData;
    LPCWSTR m_wszManagedPDBSearchPath;

public:
    NGenPdbWriter (LPCWSTR wszNativeImagePath, LPCWSTR wszPdbPath, DWORD dwExtraData, LPCWSTR wszManagedPDBSearchPath)
        : m_Create(NULL),
          m_hModule(NULL),
          m_wszPdbPath(wszPdbPath),
          m_dwExtraData(dwExtraData),
          m_wszManagedPDBSearchPath(wszManagedPDBSearchPath)
    {
        LIMITED_METHOD_CONTRACT;
    }

#define WRITER_LOAD_ERROR_MESSAGE W("Unable to load ") NATIVE_SYMBOL_READER_DLL W(".  Please ensure that ") NATIVE_SYMBOL_READER_DLL W(" is on the path.  Error='%d'\n")

    HRESULT Load(LPCWSTR wszDiasymreaderPath = nullptr)
    {
        STANDARD_VM_CONTRACT;

        HRESULT hr = S_OK;

        m_hModule = WszLoadLibrary(wszDiasymreaderPath != nullptr ? wszDiasymreaderPath : (LPCWSTR)NATIVE_SYMBOL_READER_DLL);
        if (m_hModule == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            GetSvcLogger()->Printf(WRITER_LOAD_ERROR_MESSAGE, GetLastError());
            return hr;
        }

        m_Create = reinterpret_cast<CreateNGenPdbWriter_t>(GetProcAddress(m_hModule, "CreateNGenPdbWriter"));
        if (m_Create == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            GetSvcLogger()->Printf(WRITER_LOAD_ERROR_MESSAGE, GetLastError());
            return hr;
        }

        if ((m_dwExtraData & kPDBLines) != 0)
        {
            hr = FakeCoCreateInstanceEx(
                CLSID_CorSymBinder_SxS,
                wszDiasymreaderPath != nullptr ? wszDiasymreaderPath : (LPCWSTR)NATIVE_SYMBOL_READER_DLL,
                IID_ISymUnmanagedBinder,
                (void**)&m_pBinder,
                NULL);
        }

        return hr;
    }

    HRESULT WritePDBDataForModule(Module * pModule);

    ~NGenPdbWriter()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_hModule)
            FreeLibrary(m_hModule);

        m_Create = NULL;
    }
};

#define UNKNOWN_SOURCE_FILE_PATH W("unknown")

// ----------------------------------------------------------------------------
// Manages generating all PDB data for an EE Module. Directly responsible for writing the
// string table and file checksum subsections. One of these is instantiated per Module
// found when using the ModuleIterator over the CORINFO_ASSEMBLY_HANDLE corresponding to
// this invocation of NGEN createpdb.
// 
class NGenModulePdbWriter
{
private:
    // Simple holder to coordinate the PDB calls to OpenModW and CloseMod on a given PDB
    // Mod *.
    class PDBModHolder
    {
    private:
        ReleaseHolder<ISymNGenWriter2> m_pWriter;
        LPBYTE m_pMod;

    public:
        PDBModHolder()
            : m_pWriter(NULL),
              m_pMod(NULL)
        {
            LIMITED_METHOD_CONTRACT;
        }

        ~PDBModHolder()
        {
            LIMITED_METHOD_CONTRACT;

            if ((m_pWriter != NULL) && (m_pMod != NULL))
            {
                m_pWriter->CloseMod(m_pMod);
            }
        }

        HRESULT Open(ISymNGenWriter2 * pWriter, LPCWSTR wszModule, LPCWSTR wszObjFile)
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(m_pWriter == NULL);

            m_pWriter = pWriter;
            m_pWriter->AddRef();

            _ASSERTE(m_pMod == NULL);

            HRESULT hr = m_pWriter->OpenModW(wszModule, wszObjFile, &m_pMod);
            if (FAILED(hr))
            {
                m_pMod = NULL;
            }
            return hr;
        }

        LPBYTE GetModPtr()
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(m_pMod != NULL);
            return m_pMod;
        }
    };

private:
    // This holder ensures we delete a half-generated PDB file if we manage to create it
    // on disk, but fail at some point after it was created. When NGenModulePdbWriter is
    // destroyed, m_deletePDBFileHolder's destructor will delete the PDB file if there
    // was a prior error.
    // 
    //************* NOTE! *************
    // 
    // These members should appear FIRST so that they get destructed last. That way, if
    // we encounter an error generating the PDB file, we ensure that we release all PDB
    // interfaces and close the PDB file BEFORE this holder tries to *delete* the PDB
    // file. Also, keep these two in this relative order, so that m_deletePDBFileHolder
    // is destructed before m_wszPDBFilePath.
    WCHAR m_wszPDBFilePath[MAX_LONGPATH];
    DeleteFileHolder m_deletePDBFileHolder;
    // 
    // ************* NOTE! *************
    
    CreateNGenPdbWriter_t m_Create;
    LPCWSTR m_wszPdbPath;
    ReleaseHolder<ISymNGenWriter2> m_pWriter;
    Module * m_pModule;
    DWORD m_dwExtraData;
    LPCWSTR m_wszManagedPDBSearchPath;

    // Currently The DiasymWriter does not use the correct PDB signature for NGEN PDBS unless 
    // the NGEN DLL whose symbols are being generated end in .ni.dll.   Thus we copy
    // to this name if it does not follow this covention (as is true with readyToRun
    // dlls).   This variable remembers this temp file path so we can delete it after
    // Pdb generation.   If DiaSymWriter is fixed, we can remove this.  
    SString m_tempSourceDllName;

    // Interfaces for reading IL PDB info
    ReleaseHolder<ISymUnmanagedBinder> m_pBinder;
    ReleaseHolder<ISymUnmanagedReader> m_pReader;
    NewInterfaceArrayHolder<ISymUnmanagedDocument> m_rgpDocs;       // All docs in the PDB Mod
    // I know m_ilPdbCount and m_finalPdbDocCount are confusing.Here is the reason :
    // For NGenMethodLinesPdbWriter::WriteDebugSILLinesSubsection, we won't write the path info.  
    // In order to let WriteDebugSILLinesSubsection find "UNKNOWN_SOURCE_FILE_PATH" which does 
    // not exist in m_rgpDocs, no matter if we have IL PDB or not, we let m_finalPdbDocCount 
    // equal m_ilPdbDocCount + 1 and write the extra one path as "UNKNOWN_SOURCE_FILE_PATH"
    ULONG32 m_ilPdbDocCount;
    ULONG32 m_finalPdbDocCount;

    // Keeps track of source file names and how they map to offsets in the relevant PDB
    // subsections.
    DocNameToOffsetMap m_docNameToOffsetMap;

    // Holds a PDB Mod *
    PDBModHolder m_pdbMod;

    // Buffer in which to store the entire string table (i.e., list of all source file
    // names).  This buffer is held alive as long as m_docNameToOffsetMap is needed, as
    // the latter contains offsets into this buffer.
    NewArrayHolder<BYTE> m_rgbStringTableSubsection;

    HRESULT InitILPdbData();
    HRESULT WriteStringTable();
    HRESULT WriteFileChecksums();

public:
    NGenModulePdbWriter(CreateNGenPdbWriter_t Create, LPCWSTR wszPdbPath, DWORD dwExtraData, ISymUnmanagedBinder * pBinder, Module * pModule, LPCWSTR wszManagedPDBSearchPath)
        : m_Create(Create),
          m_wszPdbPath(wszPdbPath),
          m_pWriter(NULL),
          m_pModule(pModule),
          m_dwExtraData(dwExtraData),
          m_wszManagedPDBSearchPath(wszManagedPDBSearchPath),
          m_pBinder(pBinder),
          m_ilPdbDocCount(0),
          m_finalPdbDocCount(1)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_pBinder != NULL)
            m_pBinder->AddRef();

        ZeroMemory(m_wszPDBFilePath, sizeof(m_wszPDBFilePath));
    }

    ~NGenModulePdbWriter();
    
    HRESULT WritePDBData();

    HRESULT WriteMethodPDBData(PEImageLayout * pLoadedLayout, USHORT iCodeSection, BYTE *pCodeBase, MethodDesc * hotDesc, PCODE start, bool isILPDBProvided);
};

// ----------------------------------------------------------------------------
// Manages generating the lines subsection in the PDB data for a given managed method.
// One of these is instantiated per managed method we find when iterating through all
// methods in a Module.
// 
class NGenMethodLinesPdbWriter
{
private:
    ISymNGenWriter2 * m_pWriter;
    LPBYTE m_pMod;
    ISymUnmanagedReader * m_pReader;
    MethodDesc * m_hotDesc;
    PCODE m_start;
    USHORT m_iCodeSection;
    TADDR m_addrCodeSection;
    const IJitManager::MethodRegionInfo * m_pMethodRegionInfo;
    EECodeInfo * m_pCodeInfo;
    DocNameToOffsetMap * m_pDocNameToOffsetMap;
    bool m_isILPDBProvided;

    // IL-to-native map from JIT manager
    ULONG32 m_cIlNativeMap;
    NewArrayHolder<ICorDebugInfo::OffsetMapping> m_rgIlNativeMap;

    // IL PDB info for this one method
    NewInterfaceArrayHolder<ISymUnmanagedDocument> m_rgpDocs;  // Source files defining this method.
    NewArrayHolder<ULONG32> m_rgilOffsets;                     // Array of IL offsets for this method
    NewArrayHolder<ULONG32> m_rgnLineStarts;                   // Array of source lines for this method
    ULONG32 m_cSeqPoints;                                      // Count of above two parallel arrays

    HRESULT WriteNativeILMapPDBData();
    LPBYTE InitDebugLinesHeaderSection(
        DEBUG_S_SUBSECTION_TYPE type,
        ULONG32 ulCodeStartOffset,
        ULONG32 cbCode,
        ULONG32 lineSize,
        CV_DebugSSubsectionHeader_t **ppSubSectHeader /*out*/,
        CV_DebugSLinesHeader_t ** ppLinesHeader /*out*/,
        LPBYTE * ppbLinesSubsectionCur /*out*/);

    HRESULT WriteDebugSLinesSubsection(
        ULONG32 ulCodeStartOffset,
        ULONG32 cbCode,
        MapIndexPair * rgMapIndexPairs,
        ULONG32 cMapIndexPairs);

    HRESULT WriteDebugSILLinesSubsection(
        ULONG32 ulCodeStartOffset,
        ULONG32 cbCode,
        ICorDebugInfo::OffsetMapping * rgILNativeMap,
        ULONG32 rgILNativeMapAdjustSize);

    BOOL FinalizeLinesFileBlock(
        CV_DebugSLinesFileBlockHeader_t * pLinesFileBlockHeader,
        CV_Line_t * pLineBlockStart,
        CV_Line_t * pLineBlockAfterEnd
#ifdef _DEBUG
        , BOOL ignorekUnmappedIPCheck = false
#endif
        );

public:
    NGenMethodLinesPdbWriter(
        ISymNGenWriter2 * pWriter,
        LPBYTE pMod,
        ISymUnmanagedReader * pReader,
        MethodDesc * hotDesc,
        PCODE start, 
        USHORT iCodeSection, 
        TADDR addrCodeSection,
        const IJitManager::MethodRegionInfo * pMethodRegionInfo,
        EECodeInfo * pCodeInfo,
        DocNameToOffsetMap * pDocNameToOffsetMap,
        bool isILPDBProvided)
        : m_pWriter(pWriter),
          m_pMod(pMod),
          m_pReader(pReader),
          m_hotDesc(hotDesc),
          m_start(start),
          m_iCodeSection(iCodeSection),
          m_addrCodeSection(addrCodeSection),
          m_pMethodRegionInfo(pMethodRegionInfo),
          m_pCodeInfo(pCodeInfo),
          m_pDocNameToOffsetMap(pDocNameToOffsetMap),
          m_isILPDBProvided(isILPDBProvided),
          m_cIlNativeMap(0),
          m_cSeqPoints(0)
    {
        LIMITED_METHOD_CONTRACT;
    }

    HRESULT WritePDBData();
};

// ----------------------------------------------------------------------------
// NGenPdbWriter implementation



//---------------------------------------------------------------------------------------
//
// Coordinates calling all the other classes & methods to generate PDB info for the
// given Module
//
// Arguments:
//      pModule - EE Module to write PDB data for
//

HRESULT NGenPdbWriter::WritePDBDataForModule(Module * pModule)
{
    STANDARD_VM_CONTRACT;
    NGenModulePdbWriter ngenModulePdbWriter(m_Create, m_wszPdbPath, m_dwExtraData, m_pBinder, pModule, m_wszManagedPDBSearchPath);
    return ngenModulePdbWriter.WritePDBData();
}


// ----------------------------------------------------------------------------
// NGenModulePdbWriter implementation


//---------------------------------------------------------------------------------------
//
// Writes out all source files into the string table subsection for the PDB Mod*
// controlled by this NGenModulePdbWriter.  Updates m_docNameToOffsetMap to add string
// table offset for each source file as it gets added.
// 
HRESULT NGenModulePdbWriter::WriteStringTable()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_pWriter != NULL);

    HRESULT hr;
    UINT64 cbStringTableEstimate =
        sizeof(DWORD) +
        sizeof(CV_DebugSSubsectionHeader_t) +
        m_finalPdbDocCount * (MAX_LONGPATH + 1);
    if (!FitsIn<ULONG32>(cbStringTableEstimate))
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }
    
    m_rgbStringTableSubsection = new BYTE[ULONG32(cbStringTableEstimate)];
    LPBYTE pbStringTableSubsectionCur = m_rgbStringTableSubsection;

    // Subsection signature
    *((DWORD *) pbStringTableSubsectionCur) = CV_SIGNATURE_C13;
    pbStringTableSubsectionCur += sizeof(DWORD);

    // Subsection header
    CV_DebugSSubsectionHeader_t * pSubSectHeader = (CV_DebugSSubsectionHeader_t *) pbStringTableSubsectionCur;
    memset(pSubSectHeader, 0, sizeof(*pSubSectHeader));
    pSubSectHeader->type = DEBUG_S_STRINGTABLE;
    pbStringTableSubsectionCur += sizeof(*pSubSectHeader);
    // pSubSectHeader->cbLen counts the number of bytes that appear AFTER the subsection
    // header above (i.e., the size of the string table itself). We'll fill out
    // pSubSectHeader->cbLen below, once it's calculated

    LPBYTE pbStringTableStart = pbStringTableSubsectionCur;

    // The actual strings
    for (ULONG32 i = 0; i < m_finalPdbDocCount; i++)
    {
        // For NGenMethodLinesPdbWriter::WriteDebugSILLinesSubsection, we won't write the path info.  
        // In order to let WriteDebugSILLinesSubsection can find "UNKNOWN_SOURCE_FILE_PATH" which is 
        // not existed in m_rgpDocs, no matter we have IL PDB or not, we let m_finalPdbDocCount equals to 
        // m_ilPdbDocCount + 1 and write the extra one path as "UNKNOWN_SOURCE_FILE_PATH". That also explains
        // why we have a inconsistence between m_finalPdbDocCount and m_ilPdbDocCount.
        WCHAR wszURL[MAX_LONGPATH] = UNKNOWN_SOURCE_FILE_PATH;
        ULONG32 cchURL;
        if (i < m_ilPdbDocCount)
        {
            hr = m_rgpDocs[i]->GetURL(_countof(wszURL), &cchURL, wszURL);
            if (FAILED(hr))
                return hr;
        }
        int cbWritten = WideCharToMultiByte(
            CP_UTF8,
            0,                                      // dwFlags
            wszURL,
            -1,                                     // i.e., input is NULL-terminated
            (LPSTR) pbStringTableSubsectionCur,     // output: UTF8 string starts here
            ULONG32(cbStringTableEstimate) - 
                int(pbStringTableSubsectionCur - m_rgbStringTableSubsection),    // Available space
            NULL,                                   // lpDefaultChar
            NULL                                    // lpUsedDefaultChar
            );
        if (cbWritten == 0)
            return HRESULT_FROM_WIN32(GetLastError());

        // Remember the string table offset for later
        m_docNameToOffsetMap.AddOrReplace(
            (LPCSTR) pbStringTableSubsectionCur, 
            DocNameOffsets(
                ULONG32(pbStringTableSubsectionCur - pbStringTableStart),
                (ULONG32) -1));
        
        pbStringTableSubsectionCur += cbWritten;
        if (pbStringTableSubsectionCur >= (m_rgbStringTableSubsection + cbStringTableEstimate))
            return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    // Now that we know pSubSectHeader->cbLen, fill it in
    pSubSectHeader->cbLen = CV_off32_t(pbStringTableSubsectionCur - pbStringTableStart);

    // Subsection is now filled out, so use the PDB API to add it
    hr = m_pWriter->ModAddSymbols(
        m_pdbMod.GetModPtr(),
        m_rgbStringTableSubsection, 
        int(pbStringTableSubsectionCur - m_rgbStringTableSubsection));
    if (FAILED(hr))
        return hr;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// This takes care of actually loading the IL PDB itself, and initializing the
// ISymUnmanaged* interfaces with module-level data from the IL PDB.
// 
HRESULT NGenModulePdbWriter::InitILPdbData()
{
    // Load the managed PDB
    
    ReleaseHolder<IUnknown> pUnk = NULL;
    HRESULT hr = m_pModule->GetReadablePublicMetaDataInterface(ofReadOnly, IID_IMetaDataImport, (LPVOID *) &pUnk);
    if (FAILED(hr))
    {
        GetSvcLogger()->Printf(
            W("Unable to obtain metadata for '%s'  Error: '0x%x'.\n"),
            LPCWSTR(m_pModule->GetFile()->GetILimage()->GetPath()),
            hr);
        return hr;
    }

    hr = m_pBinder->GetReaderForFile(
        pUnk,
        m_pModule->GetFile()->GetILimage()->GetPath(),
        m_wszManagedPDBSearchPath,
        &m_pReader);
    if (FAILED(hr))
    {
        GetSvcLogger()->Printf(
            W("Unable to find managed PDB matching '%s'.  Managed PDB search path: '%s'\n"),
            LPCWSTR(m_pModule->GetFile()->GetILimage()->GetPath()),
            (((m_wszManagedPDBSearchPath == NULL) || (*m_wszManagedPDBSearchPath == W('\0'))) ?
                W("(not specified)") :
                m_wszManagedPDBSearchPath));
        return hr;
    }

    GetSvcLogger()->Log(W("Loaded managed PDB"));

    // Grab the full path of the managed PDB so we can log it
    WCHAR wszIlPdbPath[MAX_LONGPATH];
    ULONG32 cchIlPdbPath;
    hr = m_pReader->GetSymbolStoreFileName(
        _countof(wszIlPdbPath),
        &cchIlPdbPath,
        wszIlPdbPath);
    if (FAILED(hr))
    {
        GetSvcLogger()->Log(W("\n"));
    }
    else
    {
        GetSvcLogger()->Printf(W(": '%s'\n"), wszIlPdbPath);
    }

    // Read all source files names from the IL PDB
    ULONG32 cDocs;
    hr = m_pReader->GetDocuments(
        0,              // cDocsRequested
        &cDocs,
        NULL            // Array
        );
    if (FAILED(hr))
        return hr;
    
    m_rgpDocs = new ISymUnmanagedDocument * [cDocs];
    hr = m_pReader->GetDocuments(
        cDocs,
        &m_ilPdbDocCount,
        m_rgpDocs);
    if (FAILED(hr))
        return hr;
    m_finalPdbDocCount = m_ilPdbDocCount + 1;
    // Commit m_rgpDocs to calling Release() on each ISymUnmanagedDocument* in the array
    m_rgpDocs.SetElementCount(m_ilPdbDocCount);
    
    return S_OK;
}

NGenModulePdbWriter::~NGenModulePdbWriter()
{
    // Delete any temporary files we created. 
    if (m_tempSourceDllName.GetCount() != 0)
        DeleteFileW(m_tempSourceDllName);
    m_tempSourceDllName.Clear();
}

//---------------------------------------------------------------------------------------
//
// This manages writing all Module-level data to the PDB, including public symbols,
// string table, files checksum, section contribution table, and, indirectly, the lines
// subsection
// 
HRESULT NGenModulePdbWriter::WritePDBData()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_pWriter == NULL);

    HRESULT hr;

    // This will try to open the managed PDB if lines info was requested.  This is a
    // likely failure point, so intentionally do this before creating the NGEN PDB file
    // on disk.
    bool isILPDBProvided = false;
    if ((m_dwExtraData & kPDBLines) != 0)
    {
        hr = InitILPdbData();
        if (FAILED(hr))
            return hr;
        isILPDBProvided = true;
    }

    // Create the PDB file we will write into.

    _ASSERTE(m_Create != NULL);
    _ASSERTE(m_pModule != NULL);

    PEImageLayout * pLoadedLayout = m_pModule->GetFile()->GetLoaded();

    // Currently DiaSymReader does not work properly generating NGEN PDBS unless 
    // the DLL whose PDB is being generated ends in .ni.*.   Unfortunately, readyToRun
    // images do not follow this convention and end up producing bad PDBS.  To fix
    // this (without changing diasymreader.dll which ships indepdendently of .NET Core)
    // we copy the file to somethign with this convention before generating the PDB
    // and delete it when we are done.  
    SString dllPath = pLoadedLayout->GetPath();
    if (!dllPath.EndsWithCaseInsensitive(W(".ni.dll")) && !dllPath.EndsWithCaseInsensitive(W(".ni.exe")))
    {
        SString::Iterator fileNameStart = dllPath.End();
        if (!dllPath.FindBack(fileNameStart, DIRECTORY_SEPARATOR_STR_W))
            fileNameStart = dllPath.Begin();

        SString::Iterator ext = dllPath.End();
        dllPath.FindBack(ext, '.');

        // m_tempSourceDllName = Convertion of  INPUT.dll  to INPUT.ni.dll where the PDB lives.  
        m_tempSourceDllName = m_wszPdbPath;
        m_tempSourceDllName += SString(dllPath, fileNameStart, ext - fileNameStart);
        m_tempSourceDllName += W(".ni");
        m_tempSourceDllName += SString(dllPath, ext, dllPath.End() - ext);
        CopyFileW(dllPath, m_tempSourceDllName, false);
        dllPath = m_tempSourceDllName;
    }

    ReleaseHolder<ISymNGenWriter> pWriter1;
    hr = m_Create(dllPath, m_wszPdbPath, &pWriter1);
    if (FAILED(hr))
        return hr;
    
    hr = pWriter1->QueryInterface(IID_ISymNGenWriter2, (LPVOID*) &m_pWriter);
    if (FAILED(hr))
    {
        GetSvcLogger()->Printf(
            W("An incorrect version of diasymreader.dll was found.  Please ensure that version 11 or greater of diasymreader.dll is on the path.  You can typically find this DLL in the desktop .NET install directory for 4.5 or greater.  Error='0x%x'\n"),
            hr);
        return hr;
    }

    // PDB file is now created.  Get its path and initialize the holder so the PDB file
    // can be deleted if we don't make it successfully to the end

    hr = m_pWriter->QueryPDBNameExW(m_wszPDBFilePath, _countof(m_wszPDBFilePath));
    if (SUCCEEDED(hr))
    {
        // A failure in QueryPDBNameExW above isn't fatal--it just means we can't
        // initialize m_deletePDBFileHolder, and thus may leave the PDB file on disk if
        // there's *another* error later on. And if we do hit another error, NGEN will
        // still return an error exit code, so the worst we'll have is a bogus PDB file
        // that no one should expect works anyway.
        m_deletePDBFileHolder.Assign(m_wszPDBFilePath);
    }


    hr = m_pdbMod.Open(m_pWriter, pLoadedLayout->GetPath(), m_pModule->GetPath());
    if (FAILED(hr))
        return hr;

    hr = WriteStringTable();
    if (FAILED(hr))
        return hr;

    hr = WriteFileChecksums();
    if (FAILED(hr))
        return hr;
    

    COUNT_T sectionCount = pLoadedLayout->GetNumberOfSections();
    IMAGE_SECTION_HEADER *section = pLoadedLayout->FindFirstSection();
    COUNT_T sectionIndex = 0;
    USHORT iCodeSection = 0;
    BYTE *pCodeBase = NULL;
    while (sectionIndex < sectionCount) 
    {
        hr = m_pWriter->AddSection((USHORT)(sectionIndex + 1),
                                 OMF_StandardText, 
                                 0,
                                 section[sectionIndex].SizeOfRawData);
        if (FAILED(hr))
            return hr;

        if (strcmp((const char *)&section[sectionIndex].Name[0], ".text") == 0) {
            _ASSERTE((iCodeSection == 0) && (pCodeBase == NULL));
            iCodeSection = (USHORT)(sectionIndex + 1);
            pCodeBase = (BYTE *)section[sectionIndex].VirtualAddress;
        }

        // In order to support the DIA RVA-to-lines API against the PDB we're
        // generating, we need to update the section contribution table with each
        // section we add.
        hr = m_pWriter->ModAddSecContribEx(
            m_pdbMod.GetModPtr(),
            (USHORT)(sectionIndex + 1),
            0,
            section[sectionIndex].SizeOfRawData,
            section[sectionIndex].Characteristics,
            0,          // dwDataCrc
            0           // dwRelocCrc
            );
        if (FAILED(hr))
            return hr;

        sectionIndex++;
    }

    _ASSERTE(iCodeSection != 0);
    _ASSERTE(pCodeBase != NULL);


    // To support lines info, we need a "dummy" section, indexed as 0, for use as a
    // sentinel when MSPDB sets up its section contribution table
    hr = m_pWriter->AddSection(0,           // Dummy section 0
        OMF_SentinelType, 
        0,
        0xFFFFffff);
    if (FAILED(hr))
        return hr;
    

#ifdef FEATURE_READYTORUN_COMPILER
    if (pLoadedLayout->HasReadyToRunHeader())
    {
        ReadyToRunInfo::MethodIterator mi(m_pModule->GetReadyToRunInfo());
        while (mi.Next())
        {
            MethodDesc *hotDesc = mi.GetMethodDesc();

            hr = WriteMethodPDBData(pLoadedLayout, iCodeSection, pCodeBase, hotDesc, mi.GetMethodStartAddress(), isILPDBProvided);
            if (FAILED(hr))
                return hr;
        }
    }
    else
#endif // FEATURE_READYTORUN_COMPILER
    {
        MethodIterator mi(m_pModule);
        while (mi.Next()) 
        {
            MethodDesc *hotDesc = mi.GetMethodDesc();
            hotDesc->CheckRestore();

            hr = WriteMethodPDBData(pLoadedLayout, iCodeSection, pCodeBase, hotDesc, mi.GetMethodStartAddress(), isILPDBProvided);
            if (FAILED(hr))
                return hr;
        }
    }

    // We made it successfully to the end, so don't delete the PDB file.
    m_deletePDBFileHolder.SuppressRelease();
    return S_OK;
}

HRESULT NGenModulePdbWriter::WriteMethodPDBData(PEImageLayout * pLoadedLayout, USHORT iCodeSection, BYTE *pCodeBase, MethodDesc * hotDesc, PCODE start, bool isILPDBProvided)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr;

    EECodeInfo codeInfo(start);
    _ASSERTE(codeInfo.IsValid());

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    PCODE pHotCodeStart = methodRegionInfo.hotStartAddress;
    _ASSERTE(pHotCodeStart);

    PCODE pColdCodeStart = methodRegionInfo.coldStartAddress;
    SString mAssemblyName;
    mAssemblyName.SetUTF8(m_pModule->GetAssembly()->GetSimpleName());
    SString assemblyName;
    assemblyName.SetUTF8(hotDesc->GetAssembly()->GetSimpleName());
    SString methodToken;
    methodToken.Printf("%X", hotDesc->GetMemberDef());

    // Hot name
    {
        SString fullName;
        TypeString::AppendMethodInternal(
            fullName, 
            hotDesc, 
            TypeString::FormatNamespace | TypeString::FormatSignature);
        fullName.Append(W("$#"));
        if (!mAssemblyName.Equals(assemblyName))
            fullName.Append(assemblyName);
        fullName.Append(W("#"));
        fullName.Append(methodToken);
        BSTRHolder hotNameHolder(SysAllocString(fullName.GetUnicode()));
        hr = m_pWriter->AddSymbol(hotNameHolder,
                                iCodeSection,
                                (pHotCodeStart - (TADDR)pLoadedLayout->GetBase() - (TADDR)pCodeBase));
        if (FAILED(hr))
            return hr;
    }

    // Cold name
    {
        if (pColdCodeStart) {

            SString fullNameCold;
            fullNameCold.Append(W("[COLD] "));
            TypeString::AppendMethodInternal(
                fullNameCold, 
                hotDesc, 
                TypeString::FormatNamespace | TypeString::FormatSignature);
            fullNameCold.Append(W("$#"));
            if (!mAssemblyName.Equals(assemblyName))
                fullNameCold.Append(assemblyName);
            fullNameCold.Append(W("#"));
            fullNameCold.Append(methodToken);

            BSTRHolder coldNameHolder(SysAllocString(fullNameCold.GetUnicode()));
            hr = m_pWriter->AddSymbol(coldNameHolder,
                                    iCodeSection,
                                    (pColdCodeStart - (TADDR)pLoadedLayout->GetBase() - (TADDR)pCodeBase));
                
            if (FAILED(hr))
                return hr;

        }
    }

    // Offset / lines mapping
    // Skip functions that are too big for PDB lines format
    if (FitsIn<DWORD>(methodRegionInfo.hotSize) &&
        FitsIn<DWORD>(methodRegionInfo.coldSize))
    {
        NGenMethodLinesPdbWriter methodLinesWriter(
            m_pWriter,
            m_pdbMod.GetModPtr(),
            m_pReader,
            hotDesc, 
            start, 
            iCodeSection, 
            (TADDR)pLoadedLayout->GetBase() + (TADDR)pCodeBase, 
            &methodRegionInfo, 
            &codeInfo, 
            &m_docNameToOffsetMap,
            isILPDBProvided);

        hr = methodLinesWriter.WritePDBData();
        if (FAILED(hr))
            return hr;
    }

    return S_OK;
}

// ----------------------------------------------------------------------------
// Handles writing the file checksums subsection to the PDB
// 
HRESULT NGenModulePdbWriter::WriteFileChecksums()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(m_pWriter != NULL);

    // The file checksums subsection of the PDB (i.e., "DEBUG_S_FILECHKSMS"), is a blob
    // consisting of a few structs stacked one after the other:
    // 
    // * (1) DWORD = CV_SIGNATURE_C13 -- the usual subsection signature DWORD
    // * (2) CV_DebugSSubsectionHeader_t -- the usual subsection header, with type =
    //     DEBUG_S_FILECHKSMS
    // * (3) Blob consisting of an array of checksum data -- the format of this piece is
    //     not defined via structs (not sure why), but is defined in
    //     vctools\PDB\doc\lines.docx
    //     
    HRESULT hr;

    // PDB format requires that the checksum size can always be expressed in a BYTE.
    const BYTE kcbEachChecksumEstimate = 0xFF;

    UINT64 cbChecksumSubsectionEstimate =
        sizeof(DWORD) +
        sizeof(CV_DebugSSubsectionHeader_t) +
        m_finalPdbDocCount * kcbEachChecksumEstimate;
    if (!FitsIn<ULONG32>(cbChecksumSubsectionEstimate))
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    NewArrayHolder<BYTE> rgbChksumSubsection(new BYTE[ULONG32(cbChecksumSubsectionEstimate)]);
    LPBYTE pbChksumSubsectionCur = rgbChksumSubsection;

    // (1) Subsection signature
    *((DWORD *) pbChksumSubsectionCur) = CV_SIGNATURE_C13;
    pbChksumSubsectionCur += sizeof(DWORD);

    // (2) Subsection header
    CV_DebugSSubsectionHeader_t * pSubSectHeader = (CV_DebugSSubsectionHeader_t *) pbChksumSubsectionCur;
    memset(pSubSectHeader, 0, sizeof(*pSubSectHeader));
    pSubSectHeader->type = DEBUG_S_FILECHKSMS;
    pbChksumSubsectionCur += sizeof(*pSubSectHeader);
    // pSubSectHeader->cblen to be filled in later once we know the size

    LPBYTE pbChksumDataStart = pbChksumSubsectionCur;

    // (3) Iterate through source files, steal their checksum info from the IL PDB, and
    // write it into the NGEN PDB.
    for (ULONG32 i = 0; i < m_finalPdbDocCount; i++)
    {
        WCHAR wszURL[MAX_LONGPATH] = UNKNOWN_SOURCE_FILE_PATH;
        char szURL[MAX_LONGPATH];
        ULONG32 cchURL;


        bool isKnownSourcePath = i < m_ilPdbDocCount;
        if (isKnownSourcePath)
        {
            // For NGenMethodLinesPdbWriter::WriteDebugSILLinesSubsection, we won't write the path info.  
            // In order to let WriteDebugSILLinesSubsection can find "UNKNOWN_SOURCE_FILE_PATH" which is 
            // not existed in m_rgpDocs, no matter we have IL PDB or not, we let m_finalPdbDocCount equals to 
            // m_ilPdbDocCount + 1 and write the extra one path as "UNKNOWN_SOURCE_FILE_PATH". That also explains
            // why we have a inconsistence between m_finalPdbDocCount and m_ilPdbDocCount.
            hr = m_rgpDocs[i]->GetURL(_countof(wszURL), &cchURL, wszURL);
            if (FAILED(hr))
                return hr;
        }

        int cbWritten = WideCharToMultiByte(
            CP_UTF8,
            0,                                      // dwFlags
            wszURL,
            -1,                                     // i.e., input is NULL-terminated
            szURL,                                  // output: UTF8 string starts here
            _countof(szURL),                        // Available space
            NULL,                                   // lpDefaultChar
            NULL                                    // lpUsedDefaultChar
            );
        if (cbWritten == 0)
            return HRESULT_FROM_WIN32(GetLastError());

        // find offset into string table and add to blob; meanwhile update hash to
        // remember the offset into the cksum table
        const KeyValuePair<LPCSTR,DocNameOffsets> * pMapEntry = 
            m_docNameToOffsetMap.LookupPtr(szURL);
        if (pMapEntry == NULL)
        {
            // Should never happen, as it implies we found a source file that was never
            // written to the string table
            return E_UNEXPECTED;
        }
        DocNameOffsets docNameOffsets(pMapEntry->Value());
        docNameOffsets.m_dwChksumTableOffset = ULONG32(pbChksumSubsectionCur - pbChksumDataStart);

        // Update the map with the new docNameOffsets that contains the cksum table
        // offset as well. Note that we must ensure the key (LPCSTR) remains the same
        // (thus we explicitly ask for the Key()). This class guarantees that string
        // pointer (which comes from the string table buffer field) will remain allocated
        // as long as the map is.
        m_docNameToOffsetMap.AddOrReplace(pMapEntry->Key(), docNameOffsets);
        * (ULONG32 *) pbChksumSubsectionCur = docNameOffsets.m_dwStrTableOffset;
        pbChksumSubsectionCur += sizeof(ULONG32);

        // Checksum algorithm and bytes

        BYTE rgbChecksum[kcbEachChecksumEstimate];
        ULONG32 cbChecksum = 0;
        BYTE bChecksumAlgorithmType = CHKSUM_TYPE_NONE;
        if (isKnownSourcePath)
        {
            GUID guidChecksumAlgorithm;
            hr = m_rgpDocs[i]->GetCheckSumAlgorithmId(&guidChecksumAlgorithm);
            if (SUCCEEDED(hr))
            {
                // If we got the checksum algorithm, we can write it all out to the buffer. 
                // Else, we'll just omit the checksum info
                if (memcmp(&guidChecksumAlgorithm, &CorSym_SourceHash_MD5, sizeof(GUID)) == 0)
                    bChecksumAlgorithmType = CHKSUM_TYPE_MD5;
                else if (memcmp(&guidChecksumAlgorithm, &CorSym_SourceHash_SHA1, sizeof(GUID)) == 0)
                    bChecksumAlgorithmType = CHKSUM_TYPE_SHA1;
            }
        }

        if (bChecksumAlgorithmType != CHKSUM_TYPE_NONE)
        {
            hr = m_rgpDocs[i]->GetCheckSum(sizeof(rgbChecksum), &cbChecksum, rgbChecksum);
            if (FAILED(hr) || !FitsIn<BYTE>(cbChecksum))
            {
                // Should never happen, but just in case checksum data is invalid, just put
                // no checksum into the NGEN PDB
                bChecksumAlgorithmType = CHKSUM_TYPE_NONE;
                cbChecksum = 0;
            }
        }

        // checksum length & algorithm
        *pbChksumSubsectionCur = (BYTE) cbChecksum;             
        pbChksumSubsectionCur++;
        *pbChksumSubsectionCur = bChecksumAlgorithmType;
        pbChksumSubsectionCur++;

        // checksum data bytes
        memcpy(pbChksumSubsectionCur, rgbChecksum, cbChecksum);
        pbChksumSubsectionCur += cbChecksum;

        // Must align to the next 4-byte boundary
        LPBYTE pbChksumSubsectionCurAligned = (LPBYTE) ALIGN_UP(pbChksumSubsectionCur, 4);
        memset(pbChksumSubsectionCur, 0, pbChksumSubsectionCurAligned-pbChksumSubsectionCur);
        pbChksumSubsectionCur = pbChksumSubsectionCurAligned;
    }

    // Now that we know pSubSectHeader->cbLen, fill it in
    pSubSectHeader->cbLen = CV_off32_t(pbChksumSubsectionCur - pbChksumDataStart);

    // Subsection is now filled out, so add it
    hr = m_pWriter->ModAddSymbols(
        m_pdbMod.GetModPtr(), 
        rgbChksumSubsection, 
        int(pbChksumSubsectionCur - rgbChksumSubsection));
    if (FAILED(hr))
        return hr;

    return S_OK;
}

// ----------------------------------------------------------------------------
// NGenMethodLinesPdbWriter implementation


//---------------------------------------------------------------------------------------
//
// Manages the writing of all lines-file subsections requred for a given method.  if a
// method is hot/cold split, this will write two line-file subsections to the PDB--one
// for the hot region, and one for the cold.
//

HRESULT NGenMethodLinesPdbWriter::WritePDBData()
{
    STANDARD_VM_CONTRACT;

    if (m_hotDesc->IsNoMetadata())
    {
        // IL stubs will not have data in the IL PDB, so just skip them.
        return S_OK;
    }

    //
    // First, we'll need to merge the IL-to-native map from the JIT manager with the
    // IL-to-source map from the IL PDB. This merging is done into a single piece that
    // includes all regions of the code when it's split
    // 

    // Grab the IL-to-native map from the JIT manager
    DebugInfoRequest debugInfoRequest;
    debugInfoRequest.InitFromStartingAddr(m_hotDesc, m_start);
    BOOL fSuccess = m_pCodeInfo->GetJitManager()->GetBoundariesAndVars(
        debugInfoRequest,
        SimpleNew, NULL,            // Allocator
        &m_cIlNativeMap,
        &m_rgIlNativeMap,
        NULL, NULL);
    if (!fSuccess)
    {
        // Shouldn't happen, but just skip this method if it does
        return S_OK;
    }
    HRESULT hr;
    if (FAILED(hr = WriteNativeILMapPDBData()))
    {
        return hr;
    }

    if (!m_isILPDBProvided)
    {
        return S_OK;
    }

    // We will traverse this IL-to-native map (from the JIT) in parallel with the
    // source-to-IL map provided by the IL PDB (below).  Both need to be sorted by IL so
    // we can easily find matching entries in the two maps
    QuickSortILNativeMapByIL sorterByIl(m_rgIlNativeMap, m_cIlNativeMap);
    sorterByIl.Sort();

    // Now grab IL-to-source map from the IL PDBs (just known as "sequence points"
    // according to the IL PDB API)
    
    ReleaseHolder<ISymUnmanagedMethod> pMethod;
    hr = m_pReader->GetMethod(
        m_hotDesc->GetMemberDef(),
        &pMethod);
    if (FAILED(hr))
    {
        // Ignore any methods not included in the IL PDB.  Although we've already
        // excluded LCG & IL stubs from methods we're considering, there can still be
        // methods in the NGEN module that are not in the IL PDB (e.g., implicit ctors).
        return S_OK;
    }

    ULONG32 cSeqPointsExpected;
    hr = pMethod->GetSequencePointCount(&cSeqPointsExpected);
    if (FAILED(hr))
    {
        // Should never happen, but we can just skip this function if the IL PDB can't
        // find sequence point info
        return S_OK;
    }

    ULONG32 cSeqPointsReturned;
    m_rgilOffsets = new ULONG32[cSeqPointsExpected];
    m_rgpDocs = new ISymUnmanagedDocument * [cSeqPointsExpected];
    m_rgnLineStarts = new ULONG32[cSeqPointsExpected];
    
    //  This is guaranteed to return the sequence points sorted in order of the IL
    //  offsets (m_rgilOffsets)
    hr = pMethod->GetSequencePoints(
        cSeqPointsExpected,
        &cSeqPointsReturned,
        m_rgilOffsets,
        m_rgpDocs,
        m_rgnLineStarts,
        NULL,       // ColumnStarts not needed
        NULL,       // LineEnds not needed
        NULL);      // ColumnEnds not needed
    if (FAILED(hr))
    {
        // Shouldn't happen, but just skip this method if it does
        return S_OK;
    }
    // Commit m_rgpDocs to calling Release() on all ISymUnmanagedDocument* returned into
    // the array.
    m_rgpDocs.SetElementCount(cSeqPointsReturned);

    // Now merge the two maps together into an array of MapIndexPair structures. Traverse
    // both maps in parallel (both ordered by IL offset), looking for IL offset matches.
    // Range matching: If an entry in the IL-to-native map has no matching entry in the
    // IL PDB, then seek up in the IL PDB to the previous sequence point and merge to
    // that (assuming that previous sequence point from the IL PDB did not already have
    // an exact match to some other entry in the IL-to-native map).
    ULONG32 cMapIndexPairsMax = m_cIlNativeMap;
    NewArrayHolder<MapIndexPair> rgMapIndexPairs(new MapIndexPair [cMapIndexPairsMax]);
    ULONG32 iSeqPoints = 0;

    // Keep track (via iSeqPointLastUnmatched) of the most recent entry in the IL PDB
    // that we passed over because it had no matching entry in the IL-to-native map. We
    // may use this to do a range-match if necessary. We'll set iSeqPointLastUnmatched to
    // the currently interated IL PDB entry after our cursor in the il-to-native map
    // passed it by, but only if fCurSeqPointMatched is FALSE
    ULONG32 iSeqPointLastUnmatched = (ULONG32) -1;
    BOOL fCurSeqPointMatched = FALSE;
    
    ULONG32 iIlNativeMap = 0;
    ULONG32 iMapIndexPairs = 0;
    
    // Traverse IL PDB entries and IL-to-native map entries (both sorted by IL) in
    // parallel
    // 
    //     * Record matching indices in our output map, rgMapIndexPairs, indexed by
    //         iMapIndexPairs.
    // 
    //     * We will have at most m_cIlNativeMap entries in rgMapIndexPairs by the time
    //         we're done. (Each il-to-native map entry will be considered for inclusion
    //         in this output. Those il-to-native map entries with a match in the il PDB
    //         will be included, the rest skipped.)
    // 
    //     * iSeqPointLastUnmatched != -1 iff it equals a prior entry in the IL PDB that
    //         we skipped over because it could not be exactly matched to an entry in the
    //         il-to-native map.  In such a case, it will be considered for a
    //         range-match to the next il-to-native map entry
    while (iIlNativeMap < m_cIlNativeMap)
    {
        _ASSERTE (iMapIndexPairs < cMapIndexPairsMax);

        // IP addresses that map to "special" places (prolog, epilog, or
        // other hidden code), will just map to 0xFeeFee, as per convention
        if ((m_rgIlNativeMap[iIlNativeMap].ilOffset == NO_MAPPING) ||
            (m_rgIlNativeMap[iIlNativeMap].ilOffset == PROLOG) ||
            (m_rgIlNativeMap[iIlNativeMap].ilOffset == EPILOG))
        {
            rgMapIndexPairs[iMapIndexPairs].m_iIlNativeMap = iIlNativeMap;
            rgMapIndexPairs[iMapIndexPairs].m_iSeqPoints = kUnmappedIP;
            iMapIndexPairs++;

            // If we were remembering a prior unmatched entry in the IL PDB, reset it
            iSeqPointLastUnmatched = (ULONG32) -1;

            // Advance il-native map, NOT il-source map
            iIlNativeMap++;
            continue;
        }

        // Cases below actually look at the IL PDB sequence point, so ensure it's still
        // in range; otherwise, we're done.
        if (iSeqPoints >= cSeqPointsReturned)
            break;

        if (m_rgIlNativeMap[iIlNativeMap].ilOffset < m_rgilOffsets[iSeqPoints])
        {
            // Our cursor over the ilnative map is behind the sourceil
            // map
            
            if (iSeqPointLastUnmatched != (ULONG32) -1)
            {
                // Range matching: This ilnative entry is behind our cursor in the
                // sourceil map, but this ilnative entry is also ahead of the previous
                // (unmatched) entry in the sourceil map. So this is a case where the JIT
                // generated sequence points that surround, without matching, that
                // previous entry in the sourceil map. So match to that previous
                // (unmatched) entry in the sourceil map.
                _ASSERTE(m_rgilOffsets[iSeqPointLastUnmatched] < m_rgIlNativeMap[iIlNativeMap].ilOffset);
                rgMapIndexPairs[iMapIndexPairs].m_iIlNativeMap = iIlNativeMap;
                rgMapIndexPairs[iMapIndexPairs].m_iSeqPoints = iSeqPointLastUnmatched;
                iMapIndexPairs++;
                
                // Reset our memory of the last unmatched entry in the IL PDB
                iSeqPointLastUnmatched = (ULONG32) -1;
            }
            else if (iMapIndexPairs > 0)
            {
                DWORD lastMatchedilNativeIndex = rgMapIndexPairs[iMapIndexPairs - 1].m_iIlNativeMap;
                if (m_rgIlNativeMap[iIlNativeMap].ilOffset == m_rgIlNativeMap[lastMatchedilNativeIndex].ilOffset &&
                    m_rgIlNativeMap[iIlNativeMap].nativeOffset < m_rgIlNativeMap[lastMatchedilNativeIndex].nativeOffset)
                {
                    rgMapIndexPairs[iMapIndexPairs - 1].m_iIlNativeMap = iIlNativeMap;
                }

            }
            // Go to next ilnative map entry
            iIlNativeMap++;
            continue;
        }

        if (m_rgilOffsets[iSeqPoints] < m_rgIlNativeMap[iIlNativeMap].ilOffset)
        {
            // Our cursor over the ilnative map is ahead of the sourceil
            // map, so go to next sourceil map entry.  Remember that we're passing over
            // this entry in the sourceil map, in case we choose to match to it later.
            if (!fCurSeqPointMatched)
            {
                iSeqPointLastUnmatched = iSeqPoints;
            }
            iSeqPoints++;
            fCurSeqPointMatched = FALSE;
            continue;
        }

        // At a match
        _ASSERTE(m_rgilOffsets[iSeqPoints] == m_rgIlNativeMap[iIlNativeMap].ilOffset);
        rgMapIndexPairs[iMapIndexPairs].m_iIlNativeMap = iIlNativeMap;
        rgMapIndexPairs[iMapIndexPairs].m_iSeqPoints = iSeqPoints;
        
        // If we were remembering a prior unmatched entry in the IL PDB, reset it
        iSeqPointLastUnmatched = (ULONG32) -1;
        
        // Advance il-native map, do not advance il-source map in case the next il-native
        // entry matches this current il-source map entry, but remember that this current
        // il-source map entry has found an exact match
        iMapIndexPairs++;
        iIlNativeMap++;
        fCurSeqPointMatched = TRUE;
    }

    ULONG32 cMapIndexPairs = iMapIndexPairs;

    // PDB format requires the lines array to be sorted by IP offset
    QuickSortMapIndexPairsByNativeOffset sorterByIp(rgMapIndexPairs, cMapIndexPairs, m_rgIlNativeMap, m_cIlNativeMap);
    sorterByIp.Sort();

    //
    // Now that the maps are merged and sorted, determine whether there's a hot/cold
    // split, where that split is, and then call WriteLinesSubsection to write out each
    // region into its own lines-file subsection
    // 

    // Find the point where the code got split
    ULONG32 iMapIndexPairsFirstEntryInColdSection = cMapIndexPairs;
    for (iMapIndexPairs = 0; iMapIndexPairs < cMapIndexPairs; iMapIndexPairs++)
    {
        DWORD dwNativeOffset = m_rgIlNativeMap[rgMapIndexPairs[iMapIndexPairs].m_iIlNativeMap].nativeOffset;
        if (dwNativeOffset >= m_pMethodRegionInfo->hotSize)
        {
            iMapIndexPairsFirstEntryInColdSection = iMapIndexPairs;
            break;
        }
    }

    // Adjust the cold offsets (if any) to be relative to the cold start
    for (iMapIndexPairs = iMapIndexPairsFirstEntryInColdSection; iMapIndexPairs < cMapIndexPairs; iMapIndexPairs++)
    {
        DWORD dwNativeOffset = m_rgIlNativeMap[rgMapIndexPairs[iMapIndexPairs].m_iIlNativeMap].nativeOffset;
        _ASSERTE (dwNativeOffset >= m_pMethodRegionInfo->hotSize);

        // Adjust offset so it's relative to the cold region start
        dwNativeOffset -= DWORD(m_pMethodRegionInfo->hotSize);
        _ASSERTE(dwNativeOffset < m_pMethodRegionInfo->coldSize);
        m_rgIlNativeMap[rgMapIndexPairs[iMapIndexPairs].m_iIlNativeMap].nativeOffset = dwNativeOffset;
    }

    // Write out the hot region into its own lines-file subsection
    hr = WriteDebugSLinesSubsection(
        ULONG32(m_pMethodRegionInfo->hotStartAddress - m_addrCodeSection),
        ULONG32(m_pMethodRegionInfo->hotSize),
        rgMapIndexPairs,
        iMapIndexPairsFirstEntryInColdSection);
    if (FAILED(hr))
        return hr;
    
    // If there was a hot/cold split, write a separate lines-file subsection for the cold
    // region
    if (iMapIndexPairsFirstEntryInColdSection < cMapIndexPairs)
    {
        hr = WriteDebugSLinesSubsection(
            ULONG32(m_pMethodRegionInfo->coldStartAddress - m_addrCodeSection),
            ULONG32(m_pMethodRegionInfo->coldSize),
            &rgMapIndexPairs[iMapIndexPairsFirstEntryInColdSection],
            cMapIndexPairs - iMapIndexPairsFirstEntryInColdSection);
        if (FAILED(hr))
            return hr;
    }

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Manages the writing of all native-IL subsections requred for a given method. Almost do
// the same thing as NGenMethodLinesPdbWriter::WritePDBData. But we will write the native-IL 
// map this time.  
//

HRESULT NGenMethodLinesPdbWriter::WriteNativeILMapPDBData()
{
    STANDARD_VM_CONTRACT;

    HRESULT hr;

    QuickSortILNativeMapByNativeOffset sorterByNativeOffset(m_rgIlNativeMap, m_cIlNativeMap);
    sorterByNativeOffset.Sort();

    ULONG32 iIlNativeMap = 0;
    ULONG32 ilNativeMapFirstEntryInColdeSection = m_cIlNativeMap;
    for (iIlNativeMap = 0; iIlNativeMap < m_cIlNativeMap; iIlNativeMap++)
    {
        if (m_rgIlNativeMap[iIlNativeMap].nativeOffset >= m_pMethodRegionInfo->hotSize)
        {
            ilNativeMapFirstEntryInColdeSection = iIlNativeMap;
            break;
        }
    }

    NewArrayHolder<ICorDebugInfo::OffsetMapping> coldRgIlNativeMap(new ICorDebugInfo::OffsetMapping[m_cIlNativeMap - ilNativeMapFirstEntryInColdeSection]);
    // Adjust the cold offsets (if any) to be relative to the cold start
    for (iIlNativeMap = ilNativeMapFirstEntryInColdeSection; iIlNativeMap < m_cIlNativeMap; iIlNativeMap++)
    {
        DWORD dwNativeOffset = m_rgIlNativeMap[iIlNativeMap].nativeOffset;
        _ASSERTE(dwNativeOffset >= m_pMethodRegionInfo->hotSize);

        // Adjust offset so it's relative to the cold region start
        dwNativeOffset -= DWORD(m_pMethodRegionInfo->hotSize);
        _ASSERTE(dwNativeOffset < m_pMethodRegionInfo->coldSize);
        coldRgIlNativeMap[iIlNativeMap - ilNativeMapFirstEntryInColdeSection].ilOffset = m_rgIlNativeMap[iIlNativeMap].ilOffset;
        coldRgIlNativeMap[iIlNativeMap - ilNativeMapFirstEntryInColdeSection].nativeOffset = dwNativeOffset;
        coldRgIlNativeMap[iIlNativeMap - ilNativeMapFirstEntryInColdeSection].source = m_rgIlNativeMap[iIlNativeMap].source;
    }

    // Write out the hot region into its own lines-file subsection
    hr = WriteDebugSILLinesSubsection(
        ULONG32(m_pMethodRegionInfo->hotStartAddress - m_addrCodeSection),
        ULONG32(m_pMethodRegionInfo->hotSize),
        m_rgIlNativeMap,
        ilNativeMapFirstEntryInColdeSection);
    if (FAILED(hr))
        return hr;

    // If there was a hot/cold split, write a separate lines-file subsection for the cold
    // region
    if (ilNativeMapFirstEntryInColdeSection < m_cIlNativeMap)
    {
        hr = WriteDebugSILLinesSubsection(
            ULONG32(m_pMethodRegionInfo->coldStartAddress - m_addrCodeSection),
            ULONG32(m_pMethodRegionInfo->coldSize),
            coldRgIlNativeMap,
            m_cIlNativeMap - ilNativeMapFirstEntryInColdeSection);
        if (FAILED(hr))
            return hr;
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Helper called by NGenMethodLinesPdbWriter::WriteDebugSLinesSubsection and 
// NGenMethodLinesPdbWriter::WriteDebugSILLinesSubsection to initial the DEBUG_S*_LINE 
// subsection headers.
//
// Arguments:
//      * ulCodeStartOffset - Offset relative to the code section, or where this region
//          of code begins
//      * type - the subsection's type
//      * lineSize - how many lines mapping the subsection will have.
//      * cbCode - Size in bytes of this region of code
//      * ppSubSectHeader -  output value which returns the intialed CV_DebugSLinesHeader_t struct pointer.
//      * ppLinesHeader - output value which returns the initialed CV_DebugSLinesHeader_t struct pointer.
//      * ppbLinesSubsectionCur - output value which points to the address right after the DebugSLinesHeader
//
// Return Value:
//      * Pointer which points the staring address of the SubSection.
//

LPBYTE NGenMethodLinesPdbWriter::InitDebugLinesHeaderSection(
    DEBUG_S_SUBSECTION_TYPE type,
    ULONG32 ulCodeStartOffset,
    ULONG32 cbCode,
    ULONG32 lineSize,
    CV_DebugSSubsectionHeader_t **ppSubSectHeader /*out*/,
    CV_DebugSLinesHeader_t ** ppLinesHeader /*out*/,
    LPBYTE * ppbLinesSubsectionCur /*out*/)
{
    STANDARD_VM_CONTRACT;

    UINT64 cbLinesSubsectionEstimate =
        sizeof(DWORD) +
        sizeof(CV_DebugSSubsectionHeader_t) +
        sizeof(CV_DebugSLinesHeader_t) +
        // Worst case: assume each sequence point will require its own
        // CV_DebugSLinesFileBlockHeader_t
        (lineSize * (sizeof(CV_DebugSLinesFileBlockHeader_t) + sizeof(CV_Line_t)));
    if (!FitsIn<ULONG32>(cbLinesSubsectionEstimate))
    {
        return NULL;
    }

    LPBYTE rgbLinesSubsection = new BYTE[ULONG32(cbLinesSubsectionEstimate)];
    LPBYTE pbLinesSubsectionCur = rgbLinesSubsection;

    // * (1) DWORD = CV_SIGNATURE_C13 -- the usual subsection signature DWORD
    *((DWORD *)pbLinesSubsectionCur) = CV_SIGNATURE_C13;
    pbLinesSubsectionCur += sizeof(DWORD);

    // * (2) CV_DebugSSubsectionHeader_t
    CV_DebugSSubsectionHeader_t * pSubSectHeader = (CV_DebugSSubsectionHeader_t *)pbLinesSubsectionCur;
    memset(pSubSectHeader, 0, sizeof(*pSubSectHeader));
    pSubSectHeader->type = type;
    *ppSubSectHeader = pSubSectHeader;
    // pSubSectHeader->cblen to be filled in later once we know the size
    pbLinesSubsectionCur += sizeof(*pSubSectHeader);

    // * (3) CV_DebugSLinesHeader_t
    CV_DebugSLinesHeader_t * pLinesHeader = (CV_DebugSLinesHeader_t *)pbLinesSubsectionCur;
    memset(pLinesHeader, 0, sizeof(*pLinesHeader));
    pLinesHeader->offCon = ulCodeStartOffset;
    pLinesHeader->segCon = m_iCodeSection;
    pLinesHeader->flags = 0;   // 0 means line info, but not column info, is included
    pLinesHeader->cbCon = cbCode;
    *ppLinesHeader = pLinesHeader;
    pbLinesSubsectionCur += sizeof(*pLinesHeader);
    *ppbLinesSubsectionCur = pbLinesSubsectionCur;
    return rgbLinesSubsection;
}

//---------------------------------------------------------------------------------------
//
// Helper called by NGenMethodLinesPdbWriter::WritePDBData to do the actual PDB writing of a single
// lines-subsection.  This is called once for the hot region, and once for the cold
// region, of a given method that has been split.  That means you get two
// lines-subsections for split methods.
//
// Arguments:
//      * ulCodeStartOffset - Offset relative to the code section, or where this region
//          of code begins
//      * cbCode - Size in bytes of this region of code
//      * rgMapIndexPairs - Array of indices forming the merged data from the JIT
//          Manager's IL-to-native map and the IL PDB's IL-to-source map.  It is assumed
//          that this array has indices sorted such that the native offsets increase
//      * cMapIndexPairs - Size in entries of above array.
//
// Assumptions:
//      rgMapIndexPairs must be sorted in order of nativeOffset, i.e.,
//      m_rgIlNativeMap[rgMapIndexPairs[i].m_iIlNativeMap].nativeOffset increases with i.
//

HRESULT NGenMethodLinesPdbWriter::WriteDebugSLinesSubsection(
    ULONG32 ulCodeStartOffset,
    ULONG32 cbCode,
    MapIndexPair * rgMapIndexPairs,
    ULONG32 cMapIndexPairs)
{
    STANDARD_VM_CONTRACT;

    // The lines subsection of the PDB (i.e., "DEBUG_S_LINES"), is a blob consisting of a
    // few structs stacked one after the other:
    // 
    // * (1) DWORD = CV_SIGNATURE_C13 -- the usual subsection signature DWORD
    // * (2) CV_DebugSSubsectionHeader_t -- the usual subsection header, with type =
    //     DEBUG_S_LINES
    // * (3) CV_DebugSLinesHeader_t -- a single header for the entire subsection.  Its
    //     purpose is to specify the native function being described, and to specify the
    //     size of the variable-sized "blocks" that follow
    // * (4) CV_DebugSLinesFileBlockHeader_t -- For each block, you get one of these.  A
    //     block is defined by a set of sequence points that map to the same source
    //     file.  While iterating through the offsets, we need to define new blocks
    //     whenever the source file changes.  In C#, this typically only happens when
    //     you advance to (or away from) an unmapped IP (0xFeeFee).
    // * (5) CV_Line_t (Line array entries) -- For each block, you get several line
    //     array entries, one entry for the beginning of each sequence point.

    HRESULT hr;


    CV_DebugSSubsectionHeader_t * pSubSectHeader = NULL;
    CV_DebugSLinesHeader_t * pLinesHeader = NULL;
    CV_DebugSLinesFileBlockHeader_t * LinesFileBlockHeader = NULL;

    // the InitDebugLinesHeaderSection will help us taking care of 
    // * (1) DWORD = CV_SIGNATURE_C13
    // * (2) CV_DebugSSubsectionHeader_t 
    // * (3) CV_DebugSLinesHeader_t 
    LPBYTE pbLinesSubsectionCur;
    LPBYTE prgbLinesSubsection = InitDebugLinesHeaderSection(
        DEBUG_S_LINES,
        ulCodeStartOffset,
        cbCode,
        cMapIndexPairs,
        &pSubSectHeader,
        &pLinesHeader,
        &pbLinesSubsectionCur);

    if (pbLinesSubsectionCur == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    NewArrayHolder<BYTE> rgbLinesSubsection(prgbLinesSubsection);

    // The loop below takes care of
    //     * (4) CV_DebugSLinesFileBlockHeader_t
    //     * (5) CV_Line_t (Line array entries)
    //
    BOOL fAtLeastOneBlockWritten = FALSE;
    CV_DebugSLinesFileBlockHeader_t * pLinesFileBlockHeader = NULL;
    CV_Line_t * pLineCur = NULL;
    CV_Line_t * pLinePrev = NULL;
    CV_Line_t * pLineBlockStart = NULL;
    BOOL fBeginNewBlock = TRUE;
    ULONG32 iSeqPointsPrev = (ULONG32) -1;
    DWORD dwNativeOffsetPrev = (DWORD) -1;
    DWORD ilOffsetPrev = (DWORD) -1;
    WCHAR wszURLPrev[MAX_LONGPATH];
    memset(&wszURLPrev, 0, sizeof(wszURLPrev));
    LPBYTE pbEnd = NULL;

    for (ULONG32 iMapIndexPairs=0; iMapIndexPairs < cMapIndexPairs; iMapIndexPairs++)
    {
        ULONG32 iSeqPoints = rgMapIndexPairs[iMapIndexPairs].m_iSeqPoints;
        ULONG32 iIlNativeMap = rgMapIndexPairs[iMapIndexPairs].m_iIlNativeMap;

        // Sometimes the JIT manager will give us duplicate IPs in the IL-to-native
        // offset mapping. PDB format frowns on that. Since rgMapIndexPairs is being
        // iterated in native offset order, it's easy to find these dupes right now, and
        // skip all but the first map containing a given IP offset.
        if (pLinePrev != NULL && m_rgIlNativeMap[iIlNativeMap].nativeOffset == pLinePrev->offset)
        {
            if (ilOffsetPrev == kUnmappedIP)
            {
                // if the previous IL offset is kUnmappedIP, then we should rewrite it. 
                pLineCur = pLinePrev;
            }
            else if (iSeqPoints != kUnmappedIP && m_rgilOffsets[iSeqPoints] < ilOffsetPrev)
            {
                pLineCur = pLinePrev;
            }
            else
            {
                // Found a native offset dupe, ignore the current map entry
                continue;
            }
        }

        if ((iSeqPoints != kUnmappedIP) && (iSeqPoints != iSeqPointsPrev))
        {
            // This is the first iteration where we're looking at this iSeqPoints.  So
            // check whether the document name has changed on us.  If it has, that means
            // we need to start a new block.
            WCHAR wszURL[MAX_LONGPATH];
            ULONG32 cchURL;
            hr = m_rgpDocs[iSeqPoints]->GetURL(_countof(wszURL), &cchURL, wszURL);
            if (FAILED(hr))
            {
                // Skip function if IL PDB has data missing
                return S_OK;
            }

            // wszURL is the best we have for a unique identifier of documents.  See
            // whether the previous document's URL is different
            if (_wcsicmp(wszURL, wszURLPrev) != 0)
            {
                // New document.  Update wszURLPrev, and remember that we need to start a
                // new file block
                if (wcscpy_s(wszURLPrev, _countof(wszURLPrev), wszURL) != 0)
                {
                    continue;
                }
                fBeginNewBlock = TRUE;
            }

            iSeqPointsPrev = iSeqPoints;
        }
        if (fBeginNewBlock)
        {
            // We've determined that we need to start a new block. So perform fixups
            // against the previous block (if any) first
            if (FinalizeLinesFileBlock(pLinesFileBlockHeader, pLineBlockStart, pLineCur))
            {
                fAtLeastOneBlockWritten = TRUE;
            }
            else if (pLinesFileBlockHeader != NULL)
            {
                // Previous block had no usable data.  So rewind back to the previous
                // block header, and we'll start there with the next block
                pbLinesSubsectionCur = LPBYTE(pLinesFileBlockHeader);
                pLineCur = (CV_Line_t *) pbLinesSubsectionCur;
            }

            // Now get the info we'll need for the next block
            char szURL[MAX_LONGPATH];
            int cbWritten = WideCharToMultiByte(
                CP_UTF8,
                0,                                      // dwFlags
                wszURLPrev,
                -1,                                     // i.e., input is NULL-terminated
                szURL,                                  // output: UTF8 string starts here
                _countof(szURL),                        // Available space
                NULL,                                   // lpDefaultChar
                NULL                                    // lpUsedDefaultChar
                );
            if (cbWritten == 0)
                continue;

            DocNameOffsets docNameOffsets;
            BOOL fExists = m_pDocNameToOffsetMap->Lookup(szURL, &docNameOffsets);
            if (fExists)
            {
                _ASSERTE(docNameOffsets.m_dwChksumTableOffset != (ULONG32) -1);
            }
            else
            {
                // We may get back an invalid document in the 0xFeeFee case (i.e., a
                // sequence point that intentionally doesn't map back to a publicly
                // available source code line).  In that case, we'll use the bogus cksum
                // offset of -1 for now, and verify we're in the 0xFeeFee case later on
                // (see code:NGenMethodLinesPdbWriter::FinalizeLinesFileBlock).
                _ASSERTE(szURL[0] == '\0');
                _ASSERTE(docNameOffsets.m_dwChksumTableOffset == (ULONG32) -1);
            }


            // * (4) CV_DebugSLinesFileBlockHeader_t
            if (pLineCur == NULL)
            {
                // First lines file block, so begin the block header immediately after the
                // subsection headers
                pLinesFileBlockHeader = (CV_DebugSLinesFileBlockHeader_t *) pbLinesSubsectionCur;
            }
            else
            {
                // We've had blocks before this one, so add this block at our current
                // location in the blob
                pLinesFileBlockHeader = (CV_DebugSLinesFileBlockHeader_t *) pLineCur;
            }
            
            // PDB structure sizes guarantee this is the case, though their docs are
            // explicit that each lines-file block header must be 4-byte aligned.
            _ASSERTE(IS_ALIGNED(pLinesFileBlockHeader, 4));

            memset(pLinesFileBlockHeader, 0, sizeof(*pLinesFileBlockHeader));
            pLinesFileBlockHeader->offFile = docNameOffsets.m_dwChksumTableOffset;
            // pLinesFileBlockHeader->nLines to be filled in when block is complete
            // pLinesFileBlockHeader->cbBlock to be filled in when block is complete

            pLineCur = (CV_Line_t *) (pLinesFileBlockHeader + 1);
            pLineBlockStart = pLineCur;
            fBeginNewBlock = FALSE;
        }


        pLineCur->offset = m_rgIlNativeMap[iIlNativeMap].nativeOffset;
        pLineCur->linenumStart = 
            (iSeqPoints == kUnmappedIP) ? 
            kUnmappedIP : 
            m_rgnLineStarts[iSeqPoints];
        pLineCur->deltaLineEnd = 0;
        pLineCur->fStatement = 1;
        ilOffsetPrev = (iSeqPoints == kUnmappedIP) ? kUnmappedIP : m_rgilOffsets[iSeqPoints];
        pLinePrev = pLineCur;
        pLineCur++;
    }       // for (ULONG32 iMapIndexPairs=0; iMapIndexPairs < cMapIndexPairs; iMapIndexPairs++)

    if (pLineCur == NULL)
    {
        // There were no lines data for this function, so don't write anything
        return S_OK;
    }

    // Perform fixups against the last block we wrote
    if (FinalizeLinesFileBlock(pLinesFileBlockHeader, pLineBlockStart, pLineCur))
        fAtLeastOneBlockWritten = TRUE;

    if (!fAtLeastOneBlockWritten)
    {
        // There were no valid blocks to write for this function, so don't bother
        // calling PDB writing API.  No problem.
        return S_OK;
    }

    // Now that we know pSubSectHeader->cbLen, fill it in
    pSubSectHeader->cbLen = CV_off32_t(LPBYTE(pLineCur) - LPBYTE(pLinesHeader));

    // Subsection is now filled out, so add it.
    hr = m_pWriter->ModAddSymbols(
        m_pMod,
        rgbLinesSubsection, 

        // The size we pass here is the size of the entire byte array that we pass in.
        int(LPBYTE(pLineCur) - rgbLinesSubsection));

    if (FAILED(hr))
        return hr;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Helper called by NGenMethodLinesPdbWriter::WriteNativeILMapPDBData to do the actual PDB writing of a single
// lines-subsection.  This is called once for the hot region, and once for the cold
// region, of a given method that has been split.  That means you get two
// lines-subsections for split methods.
//
// Arguments:
//      * ulCodeStartOffset - Offset relative to the code section, or where this region
//          of code begins
//      * cbCode - Size in bytes of this region of code
//      * rgIlNativeMap - IL to Native map array.
//      * rgILNativeMapAdjustSize - the number of elements we need to read in rgILNativeMap.
//

HRESULT NGenMethodLinesPdbWriter::WriteDebugSILLinesSubsection(
    ULONG32 ulCodeStartOffset,
    ULONG32 cbCode,
    ICorDebugInfo::OffsetMapping * rgIlNativeMap,
    ULONG32 rgILNativeMapAdjustSize)
{
    STANDARD_VM_CONTRACT;

    // The lines subsection of the PDB (i.e., "DEBUG_S_IL_LINES"), is a blob consisting of a
    // few structs stacked one after the other:
    // 
    // * (1) DWORD = CV_SIGNATURE_C13 -- the usual subsection signature DWORD
    // * (2) CV_DebugSSubsectionHeader_t -- the usual subsection header, with type =
    //     DEBUG_S_LINES
    // * (3) CV_DebugSLinesHeader_t -- a single header for the entire subsection.  Its
    //     purpose is to specify the native function being described, and to specify the
    //     size of the variable-sized "blocks" that follow
    // * (4) CV_DebugSLinesFileBlockHeader_t -- For each block, you get one of these.  A
    //     block is defined by a set of sequence points that map to the same source
    //     file.  While iterating through the offsets, we need to define new blocks
    //     whenever the source file changes.  In C#, this typically only happens when
    //     you advance to (or away from) an unmapped IP (0xFeeFee).
    // * (5) CV_Line_t (Line array entries) -- For each block, you get several line
    //     array entries, one entry for the beginning of each sequence point.

    HRESULT hr;

    CV_DebugSSubsectionHeader_t * pSubSectHeader = NULL;
    CV_DebugSLinesHeader_t * pLinesHeader = NULL;
    CV_DebugSLinesFileBlockHeader_t * pLinesFileBlockHeader = NULL;

    // the InitDebugLinesHeaderSection will help us taking care of 
    // * (1) DWORD = CV_SIGNATURE_C13
    // * (2) CV_DebugSSubsectionHeader_t 
    // * (3) CV_DebugSLinesHeader_t 
    LPBYTE pbLinesSubsectionCur;
    LPBYTE prgbLinesSubsection = InitDebugLinesHeaderSection(
        DEBUG_S_IL_LINES,
        ulCodeStartOffset,
        cbCode,
        rgILNativeMapAdjustSize,
        &pSubSectHeader,
        &pLinesHeader,
        &pbLinesSubsectionCur);

    if (prgbLinesSubsection == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    NewArrayHolder<BYTE> rgbLinesSubsection(prgbLinesSubsection);

    // The loop below takes care of
    //     * (4) CV_DebugSLinesFileBlockHeader_t
    //     * (5) CV_Line_t (Line array entries)
    //
    CV_Line_t * pLineCur = NULL;
    CV_Line_t * pLineBlockStart = NULL;
    BOOL fBeginNewBlock = TRUE;
    LPBYTE pbEnd = NULL;

    pLinesFileBlockHeader = (CV_DebugSLinesFileBlockHeader_t *)pbLinesSubsectionCur;
    // PDB structure sizes guarantee this is the case, though their docs are
    // explicit that each lines-file block header must be 4-byte aligned.
    _ASSERTE(IS_ALIGNED(pLinesFileBlockHeader, 4));

    memset(pLinesFileBlockHeader, 0, sizeof(*pLinesFileBlockHeader));
    char szURL[MAX_PATH];
    int cbWritten = WideCharToMultiByte(
        CP_UTF8,
        0,                                      // dwFlags
        UNKNOWN_SOURCE_FILE_PATH,
        -1,                                     // i.e., input is NULL-terminated
        szURL,                                  // output: UTF8 string starts here
        _countof(szURL),                        // Available space
        NULL,                                   // lpDefaultChar
        NULL                                    // lpUsedDefaultChar
        );
    _ASSERTE(cbWritten > 0);
    DocNameOffsets docNameOffsets;
    m_pDocNameToOffsetMap->Lookup(szURL, &docNameOffsets);
    pLinesFileBlockHeader->offFile = docNameOffsets.m_dwChksumTableOffset;
    // pLinesFileBlockHeader->nLines to be filled in when block is complete
    // pLinesFileBlockHeader->cbBlock to be filled in when block is complete

    pLineCur = (CV_Line_t *)(pLinesFileBlockHeader + 1);
    pLineBlockStart = pLineCur;
    CV_Line_t * pLinePrev = NULL;

    for (ULONG32 iINativeMap = 0;iINativeMap < rgILNativeMapAdjustSize; iINativeMap++)
    {
        if ((rgIlNativeMap[iINativeMap].ilOffset == NO_MAPPING) ||
            (rgIlNativeMap[iINativeMap].ilOffset == PROLOG) ||
            (rgIlNativeMap[iINativeMap].ilOffset == EPILOG))
        {
            rgIlNativeMap[iINativeMap].ilOffset = kUnmappedIP;
        }

        // Sometimes the JIT manager will give us duplicate native offset in the IL-to-native
        // offset mapping. PDB format frowns on that. Since rgMapIndexPairs is being
        // iterated in native offset order, it's easy to find these dupes right now, and
        // skip all but the first map containing a given IP offset.
        if (pLinePrev != NULL &&
            rgIlNativeMap[iINativeMap].nativeOffset == pLinePrev->offset)
        {
            if (pLinePrev->linenumStart == kUnmappedIP)
            {
                // if the previous IL offset is kUnmappedIP, then we should rewrite it. 
                pLineCur = pLinePrev;
            }
            else if (rgIlNativeMap[iINativeMap].ilOffset != kUnmappedIP &&
                rgIlNativeMap[iINativeMap].ilOffset < pLinePrev->linenumStart)
            {
                pLineCur = pLinePrev;
            }
            else
            {
                // Found a native offset dupe, ignore the current map entry
                continue;
            }
        }

        pLineCur->linenumStart = rgIlNativeMap[iINativeMap].ilOffset;

        pLineCur->offset = rgIlNativeMap[iINativeMap].nativeOffset;
        pLineCur->fStatement = 1;
        pLineCur->deltaLineEnd = 0;
        pLinePrev = pLineCur;
        pLineCur++;
    }

    if (pLineCur == NULL)
    {
        // There were no lines data for this function, so don't write anything
        return S_OK;
    }

    if (!FinalizeLinesFileBlock(pLinesFileBlockHeader, pLineBlockStart, pLineCur
#ifdef _DEBUG
        , true
#endif
        ))
    {
        return S_OK;
    }

    // Now that we know pSubSectHeader->cbLen, fill it in
    pSubSectHeader->cbLen = CV_off32_t(LPBYTE(pLineCur) - LPBYTE(pLinesHeader));

    // Subsection is now filled out, so add it.
    hr = m_pWriter->ModAddSymbols(
        m_pMod,
        rgbLinesSubsection,

        // The size we pass here is the size of the entire byte array that we pass in.
        long(LPBYTE(pLineCur) - rgbLinesSubsection));

    if (FAILED(hr))
        return hr;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Performs final fixups on the last lines-file block we completed, specifically writing
// in the size of the block, now that it's known.  Also responsible for determining
// whether there is even any data to write in the first place.
//
// Arguments:
//      * pLinesFileBlockHeader - lines-file block header to write to
//      * pLineBlockStart - First CV_Line_t * of this block
//      * pLineBlockAfterEnd - Last CV_Line_t * of this block plus 1
//
// Return Value:
//      * TRUE: lines-file block was nonempty, and is now finalized
//      * FALSE: lines-file block was empty, and caller should toss it out.
//

BOOL NGenMethodLinesPdbWriter::FinalizeLinesFileBlock(
    CV_DebugSLinesFileBlockHeader_t * pLinesFileBlockHeader, 
    CV_Line_t * pLineBlockStart,
    CV_Line_t * pLineBlockAfterEnd
#ifdef _DEBUG
  , BOOL ignorekUnmappedIPCheck
#endif
    )
{
    LIMITED_METHOD_CONTRACT;

    if (pLinesFileBlockHeader == NULL)
    {
        // If a given function has no sequence points at all, pLinesFileBlockHeader can
        // be NULL.  No problem
        return FALSE;
    }

    if (pLineBlockStart == pLineBlockAfterEnd)
    {
        // If we start a lines file block and then realize that there are no entries
        // (i.e., no valid sequence points to map), then we end up with an empty block. 
        // No problem, just skip the block.
        return FALSE;
    }

    _ASSERTE(pLineBlockStart != NULL);
    _ASSERTE(pLineBlockAfterEnd != NULL);
    _ASSERTE(pLineBlockAfterEnd > pLineBlockStart);

    if (pLinesFileBlockHeader->offFile == (ULONG32) -1)
    {
        // The file offset we set for this block is invalid. This should be due to the
        // 0xFeeFee case (i.e., sequence points that intentionally don't map back to a
        // publicly available source code line). Fix up the offset to be valid (point it
        // at the first file), but the offset will generally be ignored by the PDB
        // reader.
#ifdef _DEBUG
    {
        if (!ignorekUnmappedIPCheck)
        {
            for (CV_Line_t * pLineCur = pLineBlockStart; pLineCur < pLineBlockAfterEnd; pLineCur++)
            {
                _ASSERTE(pLineCur->linenumStart == kUnmappedIP);
            }
        }
    }
#endif // _DEBUG
        pLinesFileBlockHeader->offFile = 0;
    }

    // Now that we know the size of the block, finish filling out the lines file block
    // header
    pLinesFileBlockHeader->nLines = CV_off32_t(pLineBlockAfterEnd - pLineBlockStart);
    pLinesFileBlockHeader->cbBlock = pLinesFileBlockHeader->nLines * sizeof(CV_Line_t);
    
    return TRUE;
}
#endif // NO_NGENPDB
#if defined(FEATURE_PERFMAP) || !defined(NO_NGENPDB)
HRESULT __stdcall CreatePdb(CORINFO_ASSEMBLY_HANDLE hAssembly, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath, LPCWSTR pDiasymreaderPath)
{
    STANDARD_VM_CONTRACT;

    Assembly *pAssembly = reinterpret_cast<Assembly *>(hAssembly);
    _ASSERTE(pAssembly);
    _ASSERTE(pNativeImagePath);
    _ASSERTE(pPdbPath);

#if !defined(NO_NGENPDB)
    NGenPdbWriter pdbWriter(
        pNativeImagePath, 
        pPdbPath, 
        pdbLines ? kPDBLines : 0,
        pManagedPdbSearchPath);
    IfFailThrow(pdbWriter.Load(pDiasymreaderPath));
#elif defined(FEATURE_PERFMAP)
    NativeImagePerfMap perfMap(pAssembly, pPdbPath);
#endif

    ModuleIterator moduleIterator = pAssembly->IterateModules();
    Module *pModule = NULL;
    BOOL fAtLeastOneNativeModuleFound = FALSE;
    
    while (moduleIterator.Next()) 
    {
        pModule = moduleIterator.GetModule();

        if (pModule->HasNativeOrReadyToRunImage())
        {
#if !defined(NO_NGENPDB)
            IfFailThrow(pdbWriter.WritePDBDataForModule(pModule));
#elif defined(FEATURE_PERFMAP)
            perfMap.LogDataForModule(pModule);
#endif
            fAtLeastOneNativeModuleFound = TRUE;
        }
    }

    if (!fAtLeastOneNativeModuleFound)
    {
        GetSvcLogger()->Printf(
            W("Loaded image '%s' (for input file '%s') is not a native image.\n"),
            pAssembly->GetManifestFile()->GetPath().GetUnicode(),
            pNativeImagePath);
        return CORDBG_E_NO_IMAGE_AVAILABLE;
    }

    GetSvcLogger()->Printf(
#if !defined(NO_NGENPDB)
        W("Successfully generated PDB for native assembly '%s'.\n"),
#elif defined(FEATURE_PERFMAP)
        W("Successfully generated perfmap for native assembly '%s'.\n"),
#endif
        pNativeImagePath);

    return S_OK;
}
#else
HRESULT __stdcall CreatePdb(CORINFO_ASSEMBLY_HANDLE hAssembly, BSTR pNativeImagePath, BSTR pPdbPath, BOOL pdbLines, BSTR pManagedPdbSearchPath, LPCWSTR pDiasymreaderPath)
{
    return E_NOTIMPL;
}
#endif // defined(FEATURE_PERFMAP) || !defined(NO_NGENPDB)

// End of PDB writing code
// ----------------------------------------------------------------------------


BOOL CEEPreloader::CanPrerestoreEmbedClassHandle(CORINFO_CLASS_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    if (IsReadyToRunCompilation())
        return FALSE;

    TypeHandle th(handle);

    return m_image->CanPrerestoreEagerBindToTypeHandle(th, NULL);
}

BOOL CEEPreloader::CanPrerestoreEmbedMethodHandle(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    if (IsReadyToRunCompilation())
        return FALSE;

    MethodDesc *pMD = (MethodDesc*) handle;

    return m_image->CanPrerestoreEagerBindToMethodDesc(pMD, NULL);
}

ICorCompilePreloader * CEECompileInfo::PreloadModule(CORINFO_MODULE_HANDLE module,
                                                ICorCompileDataStore *pData,
                                                CorProfileData       *profileData)
{
    STANDARD_VM_CONTRACT;

    NewHolder<CEEPreloader> pPreloader(new CEEPreloader((Module *) module, pData));
    
    COOPERATIVE_TRANSITION_BEGIN();

    if (PartialNGenStressPercentage() == 0)
    {
        pPreloader->Preload(profileData);
    }

    COOPERATIVE_TRANSITION_END();

    return pPreloader.Extract();
}

void CEECompileInfo::SetAssemblyHardBindList(
    __in_ecount( cHardBindList )
        LPWSTR *pHardBindList,
    DWORD  cHardBindList)
{
    STANDARD_VM_CONTRACT;

}

HRESULT CEECompileInfo::SetVerboseLevel(
         IN  VerboseLevel            level)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;
    g_CorCompileVerboseLevel = level;
    return hr;
}

//
// Preloader:
//
CEEPreloader::CEEPreloader(Module *pModule,
             ICorCompileDataStore *pData)
    : m_pData(pData)
{
    m_image = new DataImage(pModule, this);

    CONSISTENCY_CHECK(pModule == GetAppDomain()->ToCompilationDomain()->GetTargetModule());

    GetAppDomain()->ToCompilationDomain()->SetTargetImage(m_image, this);

    m_methodCompileLimit = pModule->GetMDImport()->GetCountWithTokenKind(mdtMethodDef) * 10;
}

CEEPreloader::~CEEPreloader()
{
    WRAPPER_NO_CONTRACT;
    delete m_image;
}

void CEEPreloader::Preload(CorProfileData * profileData)
{
    STANDARD_VM_CONTRACT;

    bool doNothingNgen = false;
#ifdef _DEBUG
    static ConfigDWORD fDoNothingNGen;
    doNothingNgen = !!fDoNothingNGen.val(CLRConfig::INTERNAL_ZapDoNothing);
#endif

    if (!doNothingNgen)
    {
        m_image->GetModule()->SetProfileData(profileData);
        m_image->GetModule()->ExpandAll(m_image);
    }

    // Triage all items created by initial expansion. 
    // We will try to accept all items created by initial expansion. 
    TriageForZap(TRUE);
}

//
// ICorCompilerPreloader
//

DWORD CEEPreloader::MapMethodEntryPoint(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMD = GetMethod(handle);
    Precode * pPrecode = pMD->GetSavedPrecode(m_image);

    return m_image->GetRVA(pPrecode);
}

DWORD CEEPreloader::MapClassHandle(CORINFO_CLASS_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th = TypeHandle::FromPtr(handle);
    if (th.IsTypeDesc())
        return m_image->GetRVA(th.AsTypeDesc()) | 2;
    else
        return m_image->GetRVA(th.AsMethodTable());
}

DWORD CEEPreloader::MapMethodHandle(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    return m_image->GetRVA(handle);
}

DWORD CEEPreloader::MapFieldHandle(CORINFO_FIELD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    return m_image->GetRVA(handle);
}

DWORD CEEPreloader::MapAddressOfPInvokeFixup(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMD = GetMethod(handle);

    _ASSERTE(pMD->IsNDirect());
    NDirectWriteableData * pMDWriteableData = ((NDirectMethodDesc *)pMD)->GetWriteableData();

    return m_image->GetRVA(pMDWriteableData) + offsetof(NDirectWriteableData, m_pNDirectTarget);
}

DWORD CEEPreloader::MapGenericHandle(CORINFO_GENERIC_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    return m_image->GetRVA(handle);
}

DWORD CEEPreloader::MapModuleIDHandle(CORINFO_MODULE_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    return m_image->GetRVA(handle) + (DWORD)Module::GetOffsetOfModuleID();
}

CORINFO_METHOD_HANDLE CEEPreloader::NextUncompiledMethod()
{
    STANDARD_VM_CONTRACT;

    // If we have run out of methods to compile, ensure that we have code for all methods
    // that we are about to save.
    if (m_uncompiledMethods.GetCount() == 0)
    {
        // The subsequent populations are done in non-expansive way (won't load new types)
        m_image->GetModule()->PrepopulateDictionaries(m_image, TRUE);

        // Make sure that we have generated code for all instantiations that we are going to save
        // The new items that we encounter here were most likely side effects of verification or failed inlining,
        // so do not try to save them eagerly.
        while (TriageForZap(FALSE)) {
            // Loop as long as new types are added
        }
    }

    // Take next uncompiled method
    COUNT_T count = m_uncompiledMethods.GetCount();
    if (count == 0)
        return NULL;

    MethodDesc * pMD = m_uncompiledMethods[count - 1];
    m_uncompiledMethods.SetCount(count - 1);

#ifdef _DEBUG 
    if (LoggingOn(LF_ZAP, LL_INFO10000))
    {
        StackSString methodString;
        TypeString::AppendMethodDebug(methodString, pMD);

        LOG((LF_ZAP, LL_INFO10000, "CEEPreloader::NextUncompiledMethod: %S\n", methodString.GetUnicode()));
    }
#endif // _DEBUG

    return (CORINFO_METHOD_HANDLE) pMD;
}

void CEEPreloader::AddMethodToTransitiveClosureOfInstantiations(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    TriageMethodForZap(GetMethod(handle), TRUE);
}

BOOL CEEPreloader::IsMethodInTransitiveClosureOfInstantiations(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMD = GetMethod(handle);

    return (m_acceptedMethods.Lookup(pMD) != NULL) && (m_rejectedMethods.Lookup(pMD) == NULL);
}

BOOL CEEPreloader::IsTypeInTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th = (TypeHandle) handle;

    return (m_acceptedTypes.Lookup(th) != NULL) && (m_rejectedTypes.Lookup(th) == NULL);
}

void CEEPreloader::MethodReferencedByCompiledCode(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

#ifndef FEATURE_FULL_NGEN // Unreferenced methods
    //
    // Keep track of methods that are actually referenced by the code. We use this information 
    // to avoid generating code for unreferenced methods not visible outside the assembly.
    // These methods are very unlikely to be ever used at runtime because of they only ever be 
    // called via private reflection.
    //
    MethodDesc *pMD = GetMethod(handle);

    const CompileMethodEntry * pEntry = m_compileMethodsHash.LookupPtr(pMD);
    if (pEntry != NULL)
    {
        if (pEntry->fReferenced)
            return;
        const_cast<CompileMethodEntry *>(pEntry)->fReferenced = true;

        if (pEntry->fScheduled)
            return;        
        AppendUncompiledMethod(pMD);
    }
    else
    {
        CompileMethodEntry entry;
        entry.pMD = pMD;
        entry.fReferenced = true;
        entry.fScheduled = false;
        m_compileMethodsHash.Add(entry);
    }

    if (pMD->IsWrapperStub())
        MethodReferencedByCompiledCode((CORINFO_METHOD_HANDLE)pMD->GetWrappedMethodDesc());
#endif // FEATURE_FULL_NGEN
}

BOOL CEEPreloader::IsUncompiledMethod(CORINFO_METHOD_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMD = GetMethod(handle);

#ifndef FEATURE_FULL_NGEN // Unreferenced methods
    const CompileMethodEntry * pEntry = m_compileMethodsHash.LookupPtr(pMD);
    return (pEntry != NULL) && (pEntry->fScheduled || !pEntry->fReferenced);
#else
    return m_compileMethodsHash.LookupPtr(pMD) != NULL;
#endif
}

static bool IsTypeAccessibleOutsideItsAssembly(TypeHandle th)
{
    STANDARD_VM_CONTRACT;

    if (th.IsTypeDesc())
    {
        if (th.AsTypeDesc()->HasTypeParam())
            return IsTypeAccessibleOutsideItsAssembly(th.AsTypeDesc()->GetTypeParam());

        return true;
    }

    MethodTable * pMT = th.AsMethodTable();

    if (pMT == g_pCanonMethodTableClass)
        return true;

    switch (pMT->GetClass()->GetProtection())
    {
    case tdPublic:
        break;
    case tdNestedPublic:
    case tdNestedFamily:
    case tdNestedFamORAssem:
        {
            MethodTable * pMTEnclosing = pMT->LoadEnclosingMethodTable();
            if (pMTEnclosing == NULL)
                return false;
            if (!IsTypeAccessibleOutsideItsAssembly(pMTEnclosing))
                return false;
        }
        break;

    default:
        return false;
    }

    if (pMT->HasInstantiation())
    {
        Instantiation instantiation = pMT->GetInstantiation();
        for (DWORD i = 0; i < instantiation.GetNumArgs(); i++)
        {
            if (!IsTypeAccessibleOutsideItsAssembly(instantiation[i]))
                return false;
        }
    }

    return true;
}

static bool IsMethodAccessibleOutsideItsAssembly(MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    // Note that this ignores friend access.

    switch (pMD->GetAttrs() & mdMemberAccessMask)
    {
    case mdFamily:
    case mdFamORAssem:
    case mdPublic:
        break;

    default:
        return false;
    }

    if (!IsTypeAccessibleOutsideItsAssembly(pMD->GetMethodTable()))
        return false;

    if (pMD->HasMethodInstantiation())
    {
        Instantiation instantiation = pMD->GetMethodInstantiation();
        for (DWORD i = 0; i < instantiation.GetNumArgs(); i++)
        {
            if (!IsTypeAccessibleOutsideItsAssembly(instantiation[i]))
                return false;
        }
    }

    return true;
}

static bool IsMethodCallableOutsideItsAssembly(MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    // Virtual methods can be called via interfaces, etc. We would need to do
    // more analysis to trim them. For now, assume that they can be referenced outside this assembly.
    if (pMD->IsVirtual())
        return true;

    // Class constructors are often used with reflection. Always generate code for them.
    if (pMD->IsClassConstructorOrCtor())
        return true;

    if (IsMethodAccessibleOutsideItsAssembly(pMD))
        return true;

    return false;
}

BOOL IsGenericTooDeeplyNested(TypeHandle t);
void CEEPreloader::AddToUncompiledMethods(MethodDesc *pMD, BOOL fForStubs)
{
    STANDARD_VM_CONTRACT;

    // TriageTypeForZap() and TriageMethodForZap() should ensure this.
    _ASSERTE(m_image->GetModule() == pMD->GetLoaderModule());

    if (!fForStubs)
    {
        if (!pMD->IsIL())
            return;

        if (!pMD->MayHaveNativeCode() && !pMD->IsWrapperStub())
            return;
    }

    // If it's already been compiled, don't add it to the set of uncompiled methods
    if (m_image->GetCodeAddress(pMD) != NULL)
        return;
    
    // If it's already in the queue to be compiled don't add it again
    const CompileMethodEntry * pEntry = m_compileMethodsHash.LookupPtr(pMD);

#ifndef FEATURE_FULL_NGEN // Unreferenced methods
   if (pEntry != NULL)
    {
        if (pEntry->fScheduled)
            return;

        if (!pEntry->fReferenced)
            return;

        const_cast<CompileMethodEntry *>(pEntry)->fScheduled = true;
    }
    else
    {
        // The unreferenced methods optimization works for generic methods and methods on generic types only. 
        // Non-generic methods take different path.
        //
        // It unclear whether it is worth it to enable it for non-generic methods too. The benefit 
        // for non-generic methods is small, and the non-generic methods are more likely to be called 
        // via private reflection.

        bool fSchedule = fForStubs || IsMethodCallableOutsideItsAssembly(pMD);

        CompileMethodEntry entry;
        entry.pMD = pMD;
        entry.fScheduled = fSchedule;
        entry.fReferenced = false;
        m_compileMethodsHash.Add(entry);

        if (!fSchedule)
            return;
    }
#else // // FEATURE_FULL_NGEN
    // Schedule the method for compilation
    if (pEntry != NULL)
        return;
    CompileMethodEntry entry;
    entry.pMD = pMD;
    m_compileMethodsHash.Add(entry);
#endif // FEATURE_FULL_NGEN

    if (pMD->HasMethodInstantiation())
    {
        Instantiation instantiation = pMD->GetMethodInstantiation();
        for (DWORD i = 0; i < instantiation.GetNumArgs(); i++)
        {
            if (IsGenericTooDeeplyNested(instantiation[i]))
                return;
        }
    }

    // Add it to the set of uncompiled methods
    AppendUncompiledMethod(pMD);
}

//
// Used to validate instantiations produced by the production rules before we actually try to instantiate them.
//
static BOOL CanSatisfyConstraints(Instantiation typicalInst, Instantiation candidateInst)
{
    STANDARD_VM_CONTRACT;

    // The dependency must be of the form C<T> --> D<T>
    _ASSERTE(typicalInst.GetNumArgs() == candidateInst.GetNumArgs());
    if (typicalInst.GetNumArgs() != candidateInst.GetNumArgs())
        return FALSE;

    SigTypeContext typeContext(candidateInst, Instantiation());

    for (DWORD i = 0; i < candidateInst.GetNumArgs(); i++)
    {
        TypeHandle thArg = candidateInst[i];

        // If this is "__Canon" and we are code sharing then we can't rule out that some
        // compatible instantiation may meet the constraints
        if (thArg == TypeHandle(g_pCanonMethodTableClass))
            continue;

        // Otherwise we approximate, and just assume that we have "parametric" constraints
        // of the form "T : IComparable<T>" rather than "odd" constraints such as "T : IComparable<string>".
        // That is, we assume checking the constraint at the canonical type is sufficient
        // to tell us if the constraint holds for all compatible types.
        //
        // For example of where this does not hold, consider if
        //     class C<T>
        //     class D<T> where T : IComparable<T>
        //     struct Struct<T> : IComparable<string>
        // Assume we generate C<Struct<object>>.  Now the constraint
        //     Struct<object> : IComparable<object>
        // does not hold, so we do not generate the instantiation, even though strictly speaking
        // the compatible instantiation C<Struct<string>> will satisfy the constraint
        //     Struct<string> : IComparable<string>

        TypeVarTypeDesc* tyvar = typicalInst[i].AsGenericVariable();

        tyvar->LoadConstraints();

        if (!tyvar->SatisfiesConstraints(&typeContext,thArg)) {
#ifdef _DEBUG
            /*
            // In case we want to know which illegal instantiations we ngen'ed
            StackSString candidateInstName;
            StackScratchBuffer buffer;
            thArg.GetName(candidateInstName);
            char output[1024];
            _snprintf_s(output, _countof(output), _TRUNCATE, "Generics TypeDependencyAttribute processing: Couldn't satisfy a constraint.  Class with Attribute: %s  Bad candidate instantiated type: %s\r\n", pMT->GetDebugClassName(), candidateInstName.GetANSI(buffer));
            OutputDebugStringA(output);
            */
#endif
            return FALSE;
        }
    }

    return TRUE;
}


//
// This method has duplicated logic from bcl\system\collections\generic\comparer.cs
//
static void SpecializeComparer(SString& ss, Instantiation& inst)
{
    STANDARD_VM_CONTRACT;

    if (inst.GetNumArgs() != 1) {
        _ASSERTE(!"Improper use of a TypeDependencyAttribute for Comparer");
        return;
    }

    TypeHandle elemTypeHnd = inst[0];

    //
    // Override the default ObjectComparer for special cases
    //
    if (elemTypeHnd.CanCastTo(
        TypeHandle(MscorlibBinder::GetClass(CLASS__ICOMPARABLEGENERIC)).Instantiate(Instantiation(&elemTypeHnd, 1))))
    {
        ss.Set(W("System.Collections.Generic.GenericComparer`1"));
        return;
    }

    if (Nullable::IsNullableType(elemTypeHnd))
    {
        Instantiation nullableInst = elemTypeHnd.AsMethodTable()->GetInstantiation();
        if (nullableInst[0].CanCastTo(
            TypeHandle(MscorlibBinder::GetClass(CLASS__ICOMPARABLEGENERIC)).Instantiate(nullableInst)))
        {
            ss.Set(W("System.Collections.Generic.NullableComparer`1"));
            inst = nullableInst;
            return;
        }
    }

    if (elemTypeHnd.IsEnum())
    {
        CorElementType et = elemTypeHnd.GetVerifierCorElementType();
        if (et == ELEMENT_TYPE_I1 ||
            et == ELEMENT_TYPE_I2 ||
            et == ELEMENT_TYPE_I4)
        {
            ss.Set(W("System.Collections.Generic.Int32EnumComparer`1"));
            return;
        }
        if (et == ELEMENT_TYPE_U1 ||
            et == ELEMENT_TYPE_U2 ||
            et == ELEMENT_TYPE_U4)
        {
            ss.Set(W("System.Collections.Generic.UInt32EnumComparer`1"));
            return;
        }
        if (et == ELEMENT_TYPE_I8)
        {
            ss.Set(W("System.Collections.Generic.Int64EnumComparer`1"));
            return;
        }
        if (et == ELEMENT_TYPE_U8)
        {
            ss.Set(W("System.Collections.Generic.UInt64EnumComparer`1"));
            return;
        }
    }
}

//
// This method has duplicated logic from bcl\system\collections\generic\equalitycomparer.cs
// and matching logic in jitinterface.cpp
//
static void SpecializeEqualityComparer(SString& ss, Instantiation& inst)
{
    STANDARD_VM_CONTRACT;

    if (inst.GetNumArgs() != 1) {
        _ASSERTE(!"Improper use of a TypeDependencyAttribute for EqualityComparer");
        return;
    }

    TypeHandle elemTypeHnd = inst[0];

    //
    // Override the default ObjectEqualityComparer for special cases
    //
    if (elemTypeHnd.CanCastTo(
        TypeHandle(MscorlibBinder::GetClass(CLASS__IEQUATABLEGENERIC)).Instantiate(Instantiation(&elemTypeHnd, 1))))
    {
        ss.Set(W("System.Collections.Generic.GenericEqualityComparer`1"));
        return;
    }

    if (Nullable::IsNullableType(elemTypeHnd))
    {
        Instantiation nullableInst = elemTypeHnd.AsMethodTable()->GetInstantiation();
        if (nullableInst[0].CanCastTo(
            TypeHandle(MscorlibBinder::GetClass(CLASS__IEQUATABLEGENERIC)).Instantiate(nullableInst)))
        {
            ss.Set(W("System.Collections.Generic.NullableEqualityComparer`1"));
            inst = nullableInst;
            return;
        }
    }

    if (elemTypeHnd.IsEnum())
    {
        // Note: We have different comparers for Short and SByte because for those types we need to make sure we call GetHashCode on the actual underlying type as the 
        // implementation of GetHashCode is more complex than for the other types.
        CorElementType et = elemTypeHnd.GetVerifierCorElementType();
        if (et == ELEMENT_TYPE_I4 ||
            et == ELEMENT_TYPE_U4 ||
            et == ELEMENT_TYPE_U2 ||
            et == ELEMENT_TYPE_I2 ||
            et == ELEMENT_TYPE_U1 ||
            et == ELEMENT_TYPE_I1)
        {
            ss.Set(W("System.Collections.Generic.EnumEqualityComparer`1"));
            return;
        }
        else if (et == ELEMENT_TYPE_I8 ||
                 et == ELEMENT_TYPE_U8)
        {
            ss.Set(W("System.Collections.Generic.LongEnumEqualityComparer`1"));
            return;
        }
    }
}

#ifdef FEATURE_COMINTEROP
// Instantiation of WinRT types defined in non-WinRT module. This check is required to generate marshaling stubs for
// instantiations of shadow WinRT types like EventHandler<ITracingStatusChangedEventArgs> in mscorlib.
static BOOL IsInstantationOfShadowWinRTType(MethodTable * pMT)
{
    STANDARD_VM_CONTRACT;

    Instantiation inst = pMT->GetInstantiation();
    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        TypeHandle th = inst[i];
        if (th.IsProjectedFromWinRT() && !th.GetModule()->IsWindowsRuntimeModule())
            return TRUE;
    }
    return FALSE;
}
#endif

void CEEPreloader::ApplyTypeDependencyProductionsForType(TypeHandle t)
{
    STANDARD_VM_CONTRACT;

    // Only actual types
    if (t.IsTypeDesc())
        return;

    MethodTable * pMT = t.AsMethodTable();

    if (!pMT->HasInstantiation() || pMT->ContainsGenericVariables())
        return;

#ifdef FEATURE_COMINTEROP
    // At run-time, generic redirected interfaces and delegates need matching instantiations
    // of other types/methods in order to be marshaled across the interop boundary.
    if (m_image->GetModule()->IsWindowsRuntimeModule() || IsInstantationOfShadowWinRTType(pMT))
    {
        // We only apply WinRT dependencies when compiling .winmd assemblies since redirected
        // types are heavily used in non-WinRT code as well and would bloat native images.
        if (pMT->IsLegalNonArrayWinRTType())
        {
            TypeHandle thWinRT;
            WinMDAdapter::RedirectedTypeIndex index;
            if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pMT, &index))
            {
                // redirected interface needs the mscorlib-local definition of the corresponding WinRT type
                MethodTable *pWinRTMT = WinRTInterfaceRedirector::GetWinRTTypeForRedirectedInterfaceIndex(index);
                thWinRT = TypeHandle(pWinRTMT);

                // and matching stub methods
                WORD wNumSlots = pWinRTMT->GetNumVirtuals();
                for (WORD i = 0; i < wNumSlots; i++)
                {
                    MethodDesc *pAdapterMD = WinRTInterfaceRedirector::GetStubMethodForRedirectedInterface(
                        index,
                        i,
                        TypeHandle::Interop_NativeToManaged,
                        FALSE,
                        pMT->GetInstantiation());

                    TriageMethodForZap(pAdapterMD, TRUE);
                }
            }
            if (WinRTDelegateRedirector::ResolveRedirectedDelegate(pMT, &index))
            {
                // redirected delegate needs the mscorlib-local definition of the corresponding WinRT type
                thWinRT = TypeHandle(WinRTDelegateRedirector::GetWinRTTypeForRedirectedDelegateIndex(index));
            }

            if (!thWinRT.IsNull())
            {
                thWinRT = thWinRT.Instantiate(pMT->GetInstantiation());
                TriageTypeForZap(thWinRT, TRUE);
            }
        }
    }
#endif // FEATURE_COMINTEROP

    pMT = pMT->GetCanonicalMethodTable();

    // The TypeDependencyAttribute attribute is currently only allowed on mscorlib types
    // Don't even look for the attribute on types in other assemblies.
    if(!pMT->GetModule()->IsSystem()) {
        return;
    }

    // Part 1. - check for an NGEN production rule specified by a use of CompilerServices.TypeDependencyAttribute
    //  e.g. C<T> --> D<T>
    //
    // For example, if C<int> is generated then we produce D<int>.
    //
    // Normally NGEN can detect such productions through the process of compilation, but there are some
    // legitimate uses of reflection to generate generic instantiations which NGEN cannot detect.
    // In particular typically D<T> will have more constraints than C<T>, e.g.
    //     class D<T> where T : IComparable<T>
    // Uses of dynamic constraints are an example - consider making a Comparer<T>, where we can have a
    // FastComparer<T> where T : IComparable<T>, and the "slow" version checks for the non-generic
    // IComparer interface.
    // Also, T[] : IList<T>, IReadOnlyList<T>, and both of those interfaces should have a type dependency on SZArrayHelper's generic methods.
    //
    IMDInternalImport *pImport = pMT->GetMDImport();
    HRESULT hr;

    _ASSERTE(pImport);
    //walk all of the TypeDependencyAttributes
    MDEnumHolder hEnum(pImport);
    hr = pImport->EnumCustomAttributeByNameInit(pMT->GetCl(),
                                                g_CompilerServicesTypeDependencyAttribute, &hEnum);
    if (SUCCEEDED(hr))
    {
        mdCustomAttribute tkAttribute;
        const BYTE *pbAttr;
        ULONG cbAttr;

        while (pImport->EnumNext(&hEnum, &tkAttribute))
        {
            //get attribute and validate format
            if (FAILED(pImport->GetCustomAttributeAsBlob(
                tkAttribute,
                reinterpret_cast<const void **>(&pbAttr),
                &cbAttr)))
            {
                continue;
            }

            CustomAttributeParser cap(pbAttr, cbAttr);
            if (FAILED(cap.SkipProlog()))
                continue;

            LPCUTF8 szString;
            ULONG   cbString;
            if (FAILED(cap.GetNonNullString(&szString, &cbString)))
                continue;

            StackSString ss(SString::Utf8, szString, cbString);
            Instantiation inst = pMT->GetInstantiation();

#ifndef FEATURE_FULL_NGEN
            // Do not expand non-canonical instantiations. They are not that expensive to create at runtime 
            // using code:ClassLoader::CreateTypeHandleForNonCanonicalGenericInstantiation if necessary.
            if (!ClassLoader::IsCanonicalGenericInstantiation(inst))
                continue;
#endif

            if (ss.Equals(W("System.Collections.Generic.ObjectComparer`1")))
            {
                SpecializeComparer(ss, inst);
            }
            else
            if (ss.Equals(W("System.Collections.Generic.ObjectEqualityComparer`1")))
            {
                SpecializeEqualityComparer(ss, inst);
            }

            // Try to load the class using its name as a fully qualified name. If that fails,
            // then we try to load it in the assembly of the current class.
            TypeHandle typicalDepTH = TypeName::GetTypeUsingCASearchRules(ss.GetUnicode(), pMT->GetAssembly());

            _ASSERTE(!typicalDepTH.IsNull());
            // This attribute is currently only allowed to refer to mscorlib types
            _ASSERTE(typicalDepTH.GetModule()->IsSystem());
            if (!typicalDepTH.GetModule()->IsSystem())
                continue;

            // For IList<T>, ICollection<T>, IEnumerable<T>, IReadOnlyCollection<T> & IReadOnlyList<T>, include SZArrayHelper's
            // generic methods (or at least the relevant ones) in the ngen image in 
            // case someone casts a T[] to an IList<T> (or ICollection<T> or IEnumerable<T>, etc).
            if (MscorlibBinder::IsClass(typicalDepTH.AsMethodTable(), CLASS__SZARRAYHELPER))
            {
#ifdef FEATURE_FULL_NGEN
                if (pMT->GetNumGenericArgs() != 1 || !pMT->IsInterface()) {
                    _ASSERTE(!"Improper use of a TypeDependencyAttribute for SZArrayHelper");
                    continue;
                }
                TypeHandle elemTypeHnd = pMT->GetInstantiation()[0];
                if (elemTypeHnd.IsValueType())
                    ApplyTypeDependencyForSZArrayHelper(pMT, elemTypeHnd);
#endif
                continue;
            }

            _ASSERTE(typicalDepTH.IsTypicalTypeDefinition());
            if (!typicalDepTH.IsTypicalTypeDefinition())
                continue;

            // It certainly can't be immediately recursive...
            _ASSERTE(!typicalDepTH.GetMethodTable()->HasSameTypeDefAs(pMT));

            // We want to rule out some cases where we know for sure that the generated type
            // won't satisfy its constraints.  However, some generated types may represent
            // canonicals in sets of shared instantaitions,

            if (CanSatisfyConstraints(typicalDepTH.GetInstantiation(), inst))
            {
                TypeHandle instDepTH =
                    ClassLoader::LoadGenericInstantiationThrowing(typicalDepTH.GetModule(), typicalDepTH.GetCl(), inst);

                _ASSERTE(!instDepTH.ContainsGenericVariables());
                _ASSERTE(instDepTH.GetNumGenericArgs() == typicalDepTH.GetNumGenericArgs());
                _ASSERTE(instDepTH.GetMethodTable()->HasSameTypeDefAs(typicalDepTH.GetMethodTable()));

                // OK, add the generated type to the dependency set
                TriageTypeForZap(instDepTH, TRUE);
            }
        }
    }
} // CEEPreloader::ApplyTypeDependencyProductionsForType


// Given IEnumerable<Foo>, we want to add System.SZArrayHelper.GetEnumerator<Foo> 
// to the ngen image.  This way we can cast a T[] to an IList<T> and
// use methods on it (from SZArrayHelper) without pulling in the JIT.
// Do the same for ICollection<T>/IReadOnlyCollection<T> and 
// IList<T>/IReadOnlyList<T>, but only add the relevant methods 
// from those interfaces.  
void CEEPreloader::ApplyTypeDependencyForSZArrayHelper(MethodTable * pInterfaceMT, TypeHandle elemTypeHnd)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(elemTypeHnd.AsMethodTable()->IsValueType());

    // We expect this to only be called for IList<T>/IReadOnlyList<T>, ICollection<T>/IReadOnlyCollection<T>, IEnumerable<T>.
    _ASSERTE(pInterfaceMT->IsInterface());
    _ASSERTE(pInterfaceMT->GetNumGenericArgs() == 1);

    // This is the list of methods that don't throw exceptions on SZArrayHelper.
    static const BinderMethodID SZArrayHelperMethodIDs[] = { 
        // Read-only methods that are present on both regular and read-only interfaces.
        METHOD__SZARRAYHELPER__GETENUMERATOR,
        METHOD__SZARRAYHELPER__GET_COUNT,
        METHOD__SZARRAYHELPER__GET_ITEM, 
        // The rest of the methods is present on regular interfaces only.
        METHOD__SZARRAYHELPER__SET_ITEM, 
        METHOD__SZARRAYHELPER__COPYTO, 
        METHOD__SZARRAYHELPER__INDEXOF,
        METHOD__SZARRAYHELPER__CONTAINS };

    static const int cReadOnlyMethods = 3;
    static const int cAllMethods = 7;

    static const BinderMethodID LastMethodOnGenericArrayInterfaces[] = {
        METHOD__SZARRAYHELPER__GETENUMERATOR, // Last method of IEnumerable<T>
        METHOD__SZARRAYHELPER__REMOVE, // Last method of ICollection<T>.
        METHOD__SZARRAYHELPER__REMOVEAT, // Last method of IList<T>
    };
    
    // Assuming the binder ID's are properly laid out in mscorlib.h
#if _DEBUG
    for(unsigned int i=0; i < NumItems(LastMethodOnGenericArrayInterfaces) - 1; i++) {
        _ASSERTE(LastMethodOnGenericArrayInterfaces[i] < LastMethodOnGenericArrayInterfaces[i+1]);
    }
#endif

    MethodTable* pExactMT = MscorlibBinder::GetClass(CLASS__SZARRAYHELPER);

    // Subtract one from the non-generic IEnumerable that the generic IEnumerable<T>
    // inherits from.  
    unsigned inheritanceDepth = pInterfaceMT->GetNumInterfaces() - 1;
    PREFIX_ASSUME(0 <= inheritanceDepth && inheritanceDepth < NumItems(LastMethodOnGenericArrayInterfaces));
    
    // Read-only interfaces happen to always have one method
    bool fIsReadOnly = pInterfaceMT->GetNumVirtuals() == 1;

    for(int i=0; i < (fIsReadOnly ? cReadOnlyMethods : cAllMethods); i++)
    {
        // Check whether the method applies for this type.
        if (SZArrayHelperMethodIDs[i] > LastMethodOnGenericArrayInterfaces[inheritanceDepth])
            continue;

        MethodDesc * pPrimaryMD = MscorlibBinder::GetMethod(SZArrayHelperMethodIDs[i]);

        MethodDesc * pInstantiatedMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pPrimaryMD, 
                                           pExactMT, false, Instantiation(&elemTypeHnd, 1), false);

        TriageMethodForZap(pInstantiatedMD, true);
    }
}


void CEEPreloader::AddTypeToTransitiveClosureOfInstantiations(CORINFO_CLASS_HANDLE handle)
{
    STANDARD_VM_CONTRACT;

    TriageTypeForZap((TypeHandle) handle, TRUE);
}

const unsigned MAX_ZAP_INSTANTIATION_NESTING = 10;

BOOL IsGenericTooDeeplyNested(TypeHandle t)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;
    //if this type is more than N levels nested deep, do not add it to the
    //closure.  Build a queue for a DFS of the depth of instantiation.

    //the current index in the queue we're visiting
    int currentQueueIdx; //use -1 to indicate that we're done.
    //the current generic arg type.
    TypeHandle currentVisitingType[MAX_ZAP_INSTANTIATION_NESTING];

    //the ordinal in the GetInstantiation for the current type (over [0,
    //GetNumGenericArg())
    unsigned currentGenericArgEdge[MAX_ZAP_INSTANTIATION_NESTING];

    //initialize the DFS.
    memset(currentGenericArgEdge, 0, sizeof(currentGenericArgEdge));
    currentVisitingType[0] = t;
    currentQueueIdx = 0;

    while( currentQueueIdx >= 0 )
    {
        //see if we're done with this node
        if( currentVisitingType[currentQueueIdx].GetNumGenericArgs()
            <= currentGenericArgEdge[currentQueueIdx] )
        {
            --currentQueueIdx;
        }
        else
        {
            //more edges to visit.  So visit one edge
            _ASSERTE(currentGenericArgEdge[currentQueueIdx] < currentVisitingType[currentQueueIdx].GetNumGenericArgs());
            TypeHandle current = currentVisitingType[currentQueueIdx].GetInstantiation()[currentGenericArgEdge[currentQueueIdx]];
            ++currentGenericArgEdge[currentQueueIdx];
            //only value types cause a problem because of "approximate" type
            //loading, so only worry about scanning value type arguments.
            if( current.HasInstantiation() && current.IsValueType()  )
            {
                //new edge.  Make sure there is space in the queue.
                if( (currentQueueIdx + 1) >= (int)NumItems(currentGenericArgEdge) )
                {
                    //exceeded the allowable depth.  Stop processing.
                    return TRUE;
                }
                else
                {
                    ++currentQueueIdx;
                    currentGenericArgEdge[currentQueueIdx] = 0;
                    currentVisitingType[currentQueueIdx] = current;
                }
            }
        }
    }

    return FALSE;
}

void CEEPreloader::TriageTypeForZap(TypeHandle th, BOOL fAcceptIfNotSure, BOOL fExpandDependencies)
{
    STANDARD_VM_CONTRACT;

    // We care about param types only
    if (th.IsTypicalTypeDefinition() && !th.IsTypeDesc())
        return;

    // We care about types from our module only
    if (m_image->GetModule() != th.GetLoaderModule())
        return;

    // Check if we have decided to accept this type already.
    if (m_acceptedTypes.Lookup(th) != NULL)
        return;

    // Check if we have decided to reject this type already.
    if (m_rejectedTypes.Lookup(th) != NULL)
        return;

    enum { Investigate, Accepted, Rejected } triage = Investigate;

    const char * rejectReason = NULL;

    // TypeVarTypeDesc are saved via links from code:Module::m_GenericParamToDescMap
    if (th.IsGenericVariable())
    {
        triage = Rejected;
        rejectReason = "type is a Generic variable";
        goto Done;
    }

    /* Consider this example:

    class A<T> {}
    class B<U> : A<U> {}

    class C<V> : B<V> 
    {
        void foo<W>()
        {
            typeof(C<W>);
            typeof(B<A<W>>);

            typeof(List<V>);
        }
    }

    The open instantiations can be divided into the following 3 categories:

    1. A<T>,  B<U>, A<U>,  C<V>, B<V>, A<V> are open instantiations involving 
       ELEMENT_TYPE_VARs that need to be saved in the ngen image.
    2. List<V> is an instantiations that also involves ELEMENT_TYPE_VARs.
       However, it need not be saved since it will only be needed during the
       verification of foo<W>(). 
    3. C<W>, A<W>, B<A<W>> are open instantiations involving ELEMENT_TYPE_MVARs 
       that need not be saved since they will only be needed during the
       verification of foo<W>().

    Distinguishing between 1 and 2 requires walking C<V> and determining
    which ones are field/parent/interface types required by c<V>. However,
    category 3 is easy to detect, and can easily be pruned out. Hence,
    we pass in methodTypeVarsOnly=TRUE here.
    */
    if (th.ContainsGenericVariables(TRUE/*methodTypeVarsOnly*/))
    {
        triage = Rejected;
        rejectReason = "type contains method generic variables";
        goto Done;
    }

    // Filter out weird cases we do not care about.
    if (!m_image->GetModule()->GetAvailableParamTypes()->ContainsValue(th))
    {
        triage = Rejected;
        rejectReason = "type is not in the current module";
        return;
    }

    // Reject invalid generic instantiations. They will not be fully loaded
    // as they will throw a TypeLoadException before they reach CLASS_LOAD_LEVEL_FINAL.
    if (!th.IsFullyLoaded())
    {
        // This may load new types. May load new types.
        ClassLoader::TryEnsureLoaded(th);

        if (!th.IsFullyLoaded())
        {
            triage = Rejected;
            rejectReason = "type could not be fully loaded, possibly because it does not satisfy its constraints";
            goto Done;
        }
    }

    // Do not save any types containing generic class parameters from another module
    Module *pOpenModule;
    pOpenModule = th.GetDefiningModuleForOpenType();
    if (pOpenModule != NULL && pOpenModule != m_image->GetModule())
    {
        triage = Rejected;
        rejectReason = "type contains generic variables from another module";
        goto Done;
    }

    // Always store items in their preferred zap module even if we are not sure
    if (Module::GetPreferredZapModuleForTypeHandle(th) == m_image->GetModule())
    {
        triage = Accepted;
        goto Done;
    }

#ifdef FEATURE_FULL_NGEN
    // Only save arrays and other param types in their preferred zap modules, 
    // i.e. never duplicate them.
    if (th.IsTypeDesc() || th.IsArrayType())
    {
        triage = Rejected;
        rejectReason = "type is a TypeDesc";
        goto Done;
    }

    {
        // Do not save instantiations found in one of our hardbound dependencies
        PtrHashMap::PtrIterator iter = GetAppDomain()->ToCompilationDomain()->IterateHardBoundModules();
        for (/**/; !iter.end(); ++iter)
        {
            Module * hardBoundModule = (Module*)iter.GetValue();
            if (hardBoundModule->GetAvailableParamTypes()->ContainsValue(th))
            {
                triage = Rejected;
                rejectReason = "type was found in a hardbound dependency";
                goto Done;
            }
        }
    }

    // We are not really sure about this type. Accept it only if we have been asked to.
    if (fAcceptIfNotSure)
    {
        if (!m_fSpeculativeTriage)
        {
            // We will take a look later before we actually start compiling the instantiations
            m_speculativeTypes.Append(th);
            m_acceptedTypes.Add(th);
            return;
        }

        triage = Accepted;
        goto Done;
    }
#else
    rejectReason = "type is not in the preferred module";
    triage = Rejected;
#endif

Done:
    switch (triage)
    {
    case Accepted:
        m_acceptedTypes.Add(th);
        if (fExpandDependencies)
        {
            ExpandTypeDependencies(th);
        }
        break;

    case Rejected:

        m_rejectedTypes.Add(th);

#ifdef LOGGING
        // It is expensive to call th.GetName, only do it when we are actually logging
        if (LoggingEnabled())
        {
            SString typeName;
            th.GetName(typeName);
            LOG((LF_ZAP, LL_INFO10000, "TriageTypeForZap rejects %S (%08x) because %s\n", 
                 typeName.GetUnicode(), th.AsPtr(), rejectReason));
        }
#endif
        break;

    default:
        // We have not found a compeling reason to accept or reject the type yet. Maybe next time...
        break;
    }
}

void CEEPreloader::ExpandTypeDependencies(TypeHandle th)
{
    STANDARD_VM_CONTRACT;

    if (th.IsTypeDesc())
        return;
    
    MethodTable* pMT = th.AsMethodTable();

    if (pMT->IsCanonicalMethodTable())
    {
        // Cutoff infinite recursion.
        if (!IsGenericTooDeeplyNested(th))
        {
            // Make sure all methods are compiled
            // We only want to check the method bodies owned by this type,
            // and not any method bodies owned by a parent type, as the
            // parent type may not get saved in this ngen image.
            MethodTable::IntroducedMethodIterator itr(pMT);
            for (/**/; itr.IsValid(); itr.Next())
            {
                AddToUncompiledMethods(itr.GetMethodDesc(), FALSE);
            }
        }
    }
    else
    {
        // Make sure canonical method table is saved
        TriageTypeForZap(pMT->GetCanonicalMethodTable(), TRUE);
    }
    
    if (pMT->SupportsGenericInterop(TypeHandle::Interop_ManagedToNative))
    {
        MethodTable::IntroducedMethodIterator itr(pMT->GetCanonicalMethodTable());
        for (/**/; itr.IsValid(); itr.Next())
        {
            MethodDesc *pMD = itr.GetMethodDesc();

            if (!pMD->HasMethodInstantiation())
            {
                if (pMT->IsInterface() || !pMD->IsSharedByGenericInstantiations())
                {
                    pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
                        pMD, 
                        pMT, 
                        FALSE,              // forceBoxedEntryPoint
                        Instantiation(),    // methodInst
                        FALSE,              // allowInstParam
                        TRUE);              // forceRemotableMethod
                }
                else
                {
                    _ASSERTE(pMT->IsDelegate());
                    pMD = InstantiatedMethodDesc::FindOrCreateExactClassMethod(pMT, pMD);
                }

                AddToUncompiledMethods(pMD, TRUE);
            }
        }
    }

    // Make sure parent type is saved
    TriageTypeForZap(pMT->GetParentMethodTable(), TRUE);
    
    // Make sure all instantiation arguments are saved
    Instantiation inst = pMT->GetInstantiation();
    for (DWORD iArg = 0; iArg < inst.GetNumArgs(); iArg++)
    {
        TriageTypeForZap(inst[iArg], TRUE);
    }
    
    // Make sure all interfaces implemeted by the class are saved
    MethodTable::InterfaceMapIterator intIterator = pMT->IterateInterfaceMap();
    while (intIterator.Next())
    {
        TriageTypeForZap(intIterator.GetInterface(), TRUE);
    }
    
    // Make sure approx types for all fields are saved
    ApproxFieldDescIterator fdIterator(pMT, ApproxFieldDescIterator::ALL_FIELDS);
    FieldDesc* pFD;
    while ((pFD = fdIterator.Next()) != NULL)
    {
        if (pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
        {
            TriageTypeForZap(pFD->GetFieldTypeHandleThrowing(), TRUE);
        }
    }
    
    // Make sure types for all generic static fields are saved
    
    if (pMT->HasGenericsStaticsInfo())
    {
        FieldDesc *pGenStaticFields = pMT->GetGenericsStaticFieldDescs();
        DWORD nFields = pMT->GetNumStaticFields();
        for (DWORD iField = 0; iField < nFields; iField++)
        {
            FieldDesc* pField = &pGenStaticFields[iField];
            if (pField->GetFieldType() == ELEMENT_TYPE_VALUETYPE)
            {
                TriageTypeForZap(pField->GetFieldTypeHandleThrowing(), TRUE);
            }
        }
    }
    
    // Expand type using the custom rules. May load new types.
    ApplyTypeDependencyProductionsForType(th);
}

// Triage instantiations of generic methods

void CEEPreloader::TriageMethodForZap(MethodDesc* pMD, BOOL fAcceptIfNotSure, BOOL fExpandDependencies)
{
    STANDARD_VM_CONTRACT;

    // Submit the method type for triage
    TriageTypeForZap(TypeHandle(pMD->GetMethodTable()), fAcceptIfNotSure);

    // We care about instantiated methods only
    if (pMD->IsTypicalMethodDefinition())
        return;

    // We care about methods from our module only
    if (m_image->GetModule() != pMD->GetLoaderModule())
        return;

    // Check if we have decided to accept this method already.
    if (m_acceptedMethods.Lookup(pMD) != NULL)
        return;

    // Check if we have decided to reject this method already.
    if (m_rejectedMethods.Lookup(pMD) != NULL)
        return;

    enum { Investigate, Accepted, Rejected } triage = Investigate;

    const char * rejectReason = NULL;

    // Do not save open methods
    if (pMD->ContainsGenericVariables())
    {
        triage = Rejected;
        rejectReason = "method contains method generic variables";
        goto Done;
    }

    // Always store items in their preferred zap module even if we are not sure
    if (Module::GetPreferredZapModuleForMethodDesc(pMD) == m_image->GetModule())
    {
        triage = Accepted;
        goto Done;
    }

#ifdef FEATURE_FULL_NGEN
    {
        // Do not save instantiations found in one of our hardbound dependencies
        PtrHashMap::PtrIterator iter = GetAppDomain()->ToCompilationDomain()->IterateHardBoundModules();
        for (/**/; !iter.end(); ++iter)
        {
            Module * hardBoundModule = (Module*)iter.GetValue();
            if (hardBoundModule->GetInstMethodHashTable()->ContainsMethodDesc(pMD))
            {
                triage = Rejected;
                rejectReason = "method was found in a hardbound dependency";
                goto Done;
            }
        }
    }

    // We are not really sure about this method. Accept it only if we have been asked to.
    if (fAcceptIfNotSure)
    {
        // It does not seem worth it to go through extra hoops to eliminate redundant 
        // speculative method instatiations from softbound dependencies like we do for types
        // if (!m_fSpeculativeTriage)
        // {
        //    // We will take a look later before we actually start compiling the instantiations
        //    ...
        // }

        triage = Accepted;
        goto Done;
    }
#else
    triage = Rejected;
#endif

Done:
    switch (triage)
    {
    case Accepted:
        m_acceptedMethods.Add(pMD);
        if (fExpandDependencies)
        {
            ExpandMethodDependencies(pMD);
        }
        break;

    case Rejected:
        m_rejectedMethods.Add(pMD);
        LOG((LF_ZAP, LL_INFO10000, "TriageMethodForZap rejects %s (%08x) because %s\n", 
            pMD->m_pszDebugMethodName, pMD, rejectReason));
        break;

    default:
        // We have not found a compeling reason to accept or reject the method yet. Maybe next time...
        break;
    }
}

void CEEPreloader::ExpandMethodDependencies(MethodDesc * pMD)
{
    STANDARD_VM_CONTRACT;

    AddToUncompiledMethods(pMD, FALSE);

    {
        // Make sure all instantiation arguments are saved
        Instantiation inst = pMD->GetMethodInstantiation();
        for (DWORD iArg = 0; iArg < inst.GetNumArgs(); iArg++)
        {
            TriageTypeForZap(inst[iArg], TRUE);
        }
    }

    // Make sure to add wrapped method desc
    if (pMD->IsWrapperStub())
        TriageMethodForZap(pMD->GetWrappedMethodDesc(), TRUE);
}

void CEEPreloader::TriageTypeFromSoftBoundModule(TypeHandle th, Module * pSoftBoundModule)
{
    STANDARD_VM_CONTRACT;

    // We care about types from our module only
    if (m_image->GetModule() != th.GetLoaderModule())
        return;

    // Nothing to do if we have rejected the type already.
    if (m_rejectedTypes.Lookup(th) != NULL)
        return;

    // We make guarantees about types living in its own PZM only
    if (Module::GetPreferredZapModuleForTypeHandle(th) != pSoftBoundModule)
        return;

    // Reject the type - it is guaranteed to be saved in PZM
    m_rejectedTypes.Add(th);

    if (!th.IsTypeDesc())
    {
        // Reject the canonical method table if possible.
        MethodTable* pMT = th.AsMethodTable();
        if (!pMT->IsCanonicalMethodTable())
            TriageTypeFromSoftBoundModule(pMT->GetCanonicalMethodTable(), pSoftBoundModule);

        // Reject parent method table if possible.
        TriageTypeFromSoftBoundModule(pMT->GetParentMethodTable(), pSoftBoundModule);

        // Reject all interfaces implemented by the type if possible.
        MethodTable::InterfaceMapIterator intIterator = pMT->IterateInterfaceMap();
        while (intIterator.Next())
        {
            TriageTypeFromSoftBoundModule(intIterator.GetInterface(), pSoftBoundModule);
        }

        // It does not seem worth it to reject the remaining items 
        // expanded by CEEPreloader::ExpandTypeDependencies here.
    }
}

#ifdef FEATURE_FULL_NGEN
static TypeHandle TryToLoadTypeSpecHelper(Module * pModule, PCCOR_SIGNATURE pSig, ULONG cSig)
{
    STANDARD_VM_CONTRACT;

    TypeHandle th;

    EX_TRY
    {
        SigPointer p(pSig, cSig);
        SigTypeContext typeContext;    // empty context is OK: encoding should not contain type variables.

        th = p.GetTypeHandleThrowing(pModule, &typeContext, ClassLoader::DontLoadTypes);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

    return th;
}

void CEEPreloader::TriageTypeSpecsFromSoftBoundModule(Module * pSoftBoundModule)
{
    STANDARD_VM_CONTRACT;

    //
    // Reject all typespecs that are guranteed to be found in soft bound PZM
    //

    IMDInternalImport *pInternalImport = pSoftBoundModule->GetMDImport();

    HENUMInternalHolder hEnum(pInternalImport);
    hEnum.EnumAllInit(mdtTypeSpec);

    mdToken tk;
    while (pInternalImport->EnumNext(&hEnum, &tk))
    {
        ULONG cSig;
        PCCOR_SIGNATURE pSig;

        if (FAILED(pInternalImport->GetTypeSpecFromToken(tk, &pSig, &cSig)))
        {
            pSig = NULL;
            cSig = 0;
        }

        // Check all types specs that do not contain variables
        if (SigPointer(pSig, cSig).IsPolyType(NULL) == hasNoVars)
        {
            TypeHandle th = TryToLoadTypeSpecHelper(pSoftBoundModule, pSig, cSig);

            if (th.IsNull())
                continue;

            TriageTypeFromSoftBoundModule(th, pSoftBoundModule);
        }
    }
}

void CEEPreloader::TriageSpeculativeType(TypeHandle th)
{
    STANDARD_VM_CONTRACT;

    // Nothing to do if we have rejected the type already
    if (m_rejectedTypes.Lookup(th) != NULL)
        return;

    Module * pPreferredZapModule = Module::GetPreferredZapModuleForTypeHandle(th);
    BOOL fHardBoundPreferredZapModule = FALSE;

    //
    // Even though we have done this check already earlier, do it again here in case we have picked up
    // any eager-bound dependency in the meantime
    //
    // Do not save instantiations found in one of our eager-bound dependencies
    PtrHashMap::PtrIterator iter = GetAppDomain()->ToCompilationDomain()->IterateHardBoundModules();
    for (/**/; !iter.end(); ++iter)
    {
        Module * hardBoundModule = (Module*)iter.GetValue();
        if (hardBoundModule->GetAvailableParamTypes()->ContainsValue(th))
        {
            m_rejectedTypes.Add(th);
            return;
        }

        if (hardBoundModule == pPreferredZapModule)
        {
            fHardBoundPreferredZapModule = TRUE;
        }
    }

    if (!fHardBoundPreferredZapModule && !pPreferredZapModule->AreTypeSpecsTriaged())
    {
        // Reject all types that are guaranteed to be instantiated in soft bound PZM
        TriageTypeSpecsFromSoftBoundModule(pPreferredZapModule);
        pPreferredZapModule->SetTypeSpecsTriaged();

        if (m_rejectedTypes.Lookup(th) != NULL)
            return;
    }

    // We have to no other option but to accept and expand the type
    ExpandTypeDependencies(th);
}

void CEEPreloader::TriageSpeculativeInstantiations()
{
    STANDARD_VM_CONTRACT;

    // Get definitive triage answer for speculative types that we have run into earlier
    // Note that m_speculativeTypes may be growing as this loop runs
    for (COUNT_T i = 0; i < m_speculativeTypes.GetCount(); i++)
    {
        TriageSpeculativeType(m_speculativeTypes[i]);
    }

    // We are done - the array of speculative types is no longer necessary
    m_speculativeTypes.Clear();
}
#endif // FEATURE_FULL_NGEN

BOOL CEEPreloader::TriageForZap(BOOL fAcceptIfNotSure, BOOL fExpandDependencies)
{
    STANDARD_VM_CONTRACT;

    DWORD dwNumTypes = m_image->GetModule()->GetAvailableParamTypes()->GetCount();
    DWORD dwNumMethods = m_image->GetModule()->GetInstMethodHashTable()->GetCount();

    // Triage types
    {
        // Create a local copy in case the new elements are added to the hashtable during population
        InlineSArray<TypeHandle, 20> pTypes;

        // Make sure the iterator is destroyed before there is a chance of loading new types
        {
            EETypeHashTable* pTable = m_image->GetModule()->GetAvailableParamTypes();

            EETypeHashTable::Iterator it(pTable);
            EETypeHashEntry *pEntry;
            while (pTable->FindNext(&it, &pEntry))
            {
                TypeHandle th = pEntry->GetTypeHandle();
                if (m_acceptedTypes.Lookup(th) == NULL && m_rejectedTypes.Lookup(th) == NULL)
                    pTypes.Append(th);
            }
        }

        for(COUNT_T i = 0; i < pTypes.GetCount(); i ++)
        {
            TriageTypeForZap(pTypes[i], fAcceptIfNotSure, fExpandDependencies);
        }
    }

    // Triage methods
    {
        // Create a local copy in case the new elements are added to the hashtable during population
        InlineSArray<MethodDesc*, 20> pMethods;

        // Make sure the iterator is destroyed before there is a chance of loading new methods
        {
            InstMethodHashTable* pTable = m_image->GetModule()->GetInstMethodHashTable();

            InstMethodHashTable::Iterator it(pTable);
            InstMethodHashEntry *pEntry;
            while (pTable->FindNext(&it, &pEntry))
            {
                MethodDesc* pMD = pEntry->GetMethod();
                if (m_acceptedMethods.Lookup(pMD) == NULL && m_rejectedMethods.Lookup(pMD) == NULL)
                    pMethods.Append(pMD);
            }
        }

        for(COUNT_T i = 0; i < pMethods.GetCount(); i ++)
        {
            TriageMethodForZap(pMethods[i], fAcceptIfNotSure, fExpandDependencies);
        }
    }

    // Returns TRUE if new types or methods has been added by the triage
    return (dwNumTypes != m_image->GetModule()->GetAvailableParamTypes()->GetCount()) ||
           (dwNumMethods != m_image->GetModule()->GetInstMethodHashTable()->GetCount());
}

void CEEPreloader::PrePrepareMethodIfNecessary(CORINFO_METHOD_HANDLE hMethod)
{
    STANDARD_VM_CONTRACT;

}

static void SetStubMethodDescOnInteropMethodDesc(MethodDesc* pInteropMD, MethodDesc* pStubMD, bool fReverseStub)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        // We store NGENed stubs on these MethodDesc types
        PRECONDITION(pInteropMD->IsNDirect() || pInteropMD->IsComPlusCall() || pInteropMD->IsGenericComPlusCall() || pInteropMD->IsEEImpl());
    }
    CONTRACTL_END;

    if (pInteropMD->IsNDirect())
    {
        _ASSERTE(!fReverseStub);
        NDirectMethodDesc* pNMD = (NDirectMethodDesc*)pInteropMD;
        pNMD->ndirect.m_pStubMD.SetValue(pStubMD);
    }
#ifdef FEATURE_COMINTEROP
    else if (pInteropMD->IsComPlusCall() || pInteropMD->IsGenericComPlusCall())
    {
        _ASSERTE(!fReverseStub);
        ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pInteropMD);
       pComInfo->m_pStubMD.SetValue(pStubMD);
    }
#endif // FEATURE_COMINTEROP
    else if (pInteropMD->IsEEImpl())
    {
        DelegateEEClass* pDelegateClass = (DelegateEEClass*)pInteropMD->GetClass();
        if (fReverseStub)
        {
            pDelegateClass->m_pReverseStubMD = pStubMD;
        }
        else
        {
#ifdef FEATURE_COMINTEROP
            // We don't currently NGEN both the P/Invoke and WinRT stubs for WinRT delegates.
            // If that changes, this function will need an extra parameter to tell what kind
            // of stub is being passed.
            if (pInteropMD->GetMethodTable()->IsWinRTDelegate())
            {
                pDelegateClass->m_pComPlusCallInfo->m_pStubMD.SetValue(pStubMD);
            }
            else
#endif // FEATURE_COMINTEROP
            {
                pDelegateClass->m_pForwardStubMD = pStubMD;
            }
        }
    }
    else
    {
        UNREACHABLE_MSG("unexpected type of MethodDesc");
    }
}

MethodDesc * CEEPreloader::CompileMethodStubIfNeeded(
        MethodDesc *pMD,
        MethodDesc *pStubMD,
        ICorCompilePreloader::CORCOMPILE_CompileStubCallback pfnCallback,
        LPVOID pCallbackContext)
{
    STANDARD_VM_CONTRACT;

    LOG((LF_ZAP, LL_INFO10000, "NGEN_ILSTUB: %s::%s -> %s::%s\n",
         pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, pStubMD->m_pszDebugClassName, pStubMD->m_pszDebugMethodName));

    // It is possible that the StubMD is a normal method pointed by InteropStubMethodAttribute,
    // and in that case we don't need to compile it here
    if (pStubMD->IsDynamicMethod())
    {
        if (!pStubMD->AsDynamicMethodDesc()->GetILStubResolver()->IsCompiled())
        {
            CORJIT_FLAGS jitFlags = pStubMD->AsDynamicMethodDesc()->GetILStubResolver()->GetJitFlags();

            pfnCallback(pCallbackContext, (CORINFO_METHOD_HANDLE)pStubMD, jitFlags);
        }

#ifndef FEATURE_FULL_NGEN // Deduplication
        const DuplicateMethodEntry * pDuplicate = m_duplicateMethodsHash.LookupPtr(pStubMD);
        if (pDuplicate != NULL)
            return pDuplicate->pDuplicateMD;
#endif
    }

//We do not store ILStubs so if the compilation failed for them
//It does not make sense to keep the MD corresponding to the IL
    if (pStubMD->IsILStub() && m_image->GetCodeAddress(pStubMD) == NULL)
        pStubMD=NULL;

    return pStubMD;
}

void CEEPreloader::GenerateMethodStubs(
        CORINFO_METHOD_HANDLE hMethod,
        bool                  fNgenProfilerImage,
        CORCOMPILE_CompileStubCallback pfnCallback,
        LPVOID                pCallbackContext)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(hMethod != NULL && pfnCallback != NULL);
    }
    CONTRACTL_END;

    MethodDesc* pMD = GetMethod(hMethod);
    MethodDesc* pStubMD = NULL;

    // Do not generate IL stubs when generating ReadyToRun images
    // This prevents versionability concerns around IL stubs exposing internal
    // implementation details of the CLR.
    if (IsReadyToRunCompilation())
        return;

    DWORD dwNGenStubFlags = NDIRECTSTUB_FL_NGENEDSTUB;

    if (fNgenProfilerImage)
        dwNGenStubFlags |= NDIRECTSTUB_FL_NGENEDSTUBFORPROFILING;

    //
    // Generate IL stubs. If failed, we go through normal NGEN path
    // Catch any exceptions that occur when we try to create the IL_STUB
    //    
    EX_TRY
    {
        //
        // Take care of forward stubs
        //            
        if (pMD->IsNDirect())
        {
            NDirectMethodDesc* pNMD = (NDirectMethodDesc*)pMD;
            PInvokeStaticSigInfo sigInfo;
            NDirect::PopulateNDirectMethodDesc(pNMD, &sigInfo);
            pStubMD = NDirect::GetILStubMethodDesc((NDirectMethodDesc*)pMD, &sigInfo, dwNGenStubFlags);
        }
#ifdef FEATURE_COMINTEROP
        else if (pMD->IsComPlusCall() || pMD->IsGenericComPlusCall())
        {
            if (MethodNeedsForwardComStub(pMD, m_image))
            {
                // Look for predefined IL stubs in forward com interop scenario. 
                // If we've found a stub, that's what we'll use
                DWORD dwStubFlags;
                ComPlusCall::PopulateComPlusCallMethodDesc(pMD, &dwStubFlags);
                if (FAILED(FindPredefinedILStubMethod(pMD, dwStubFlags, &pStubMD)))
                {                    
                    pStubMD = ComPlusCall::GetILStubMethodDesc(pMD, dwStubFlags | dwNGenStubFlags);
                }
            }
        }
#endif // FEATURE_COMINTEROP
        else if (pMD->IsEEImpl())
        {
            MethodTable* pMT = pMD->GetMethodTable();
            CONSISTENCY_CHECK(pMT->IsDelegate());

            // we can filter out non-WinRT generic delegates right off the top
            if (!pMD->HasClassOrMethodInstantiation() || pMT->IsProjectedFromWinRT()
#ifdef FEATURE_COMINTEROP
                || WinRTTypeNameConverter::IsRedirectedType(pMT)
#endif // FEATURE_COMINTEROP
                )
            {
                if (COMDelegate::IsDelegateInvokeMethod(pMD)) // build forward stub
                {
#ifdef FEATURE_COMINTEROP
                    if ((pMT->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(pMT)) &&
                        (!pMT->HasInstantiation() || pMT->SupportsGenericInterop(TypeHandle::Interop_ManagedToNative))) // filter out shared generics
                    {
                        // Build the stub for all WinRT delegates, these will definitely be used for interop.
                        if (pMT->IsLegalNonArrayWinRTType())
                        {
                            COMDelegate::PopulateComPlusCallInfo(pMT);
                            pStubMD = COMDelegate::GetILStubMethodDesc((EEImplMethodDesc *)pMD, dwNGenStubFlags);
                        }
                    }
                    else
#endif // FEATURE_COMINTEROP
                    {
                        // Build the stub only if the delegate is decorated with UnmanagedFunctionPointerAttribute.
                        // Forward delegate stubs are rare so we require this opt-in to avoid bloating NGEN images.

                        if (S_OK == pMT->GetMDImport()->GetCustomAttributeByName(
                            pMT->GetCl(), g_UnmanagedFunctionPointerAttribute, NULL, NULL))
                        {
                            pStubMD = COMDelegate::GetILStubMethodDesc((EEImplMethodDesc *)pMD, dwNGenStubFlags);
                        }
                    }
                }
            }
        }

        // compile the forward stub
        if (pStubMD != NULL)
        {
            pStubMD = CompileMethodStubIfNeeded(pMD, pStubMD, pfnCallback, pCallbackContext);

            // We store the MethodDesc of the Stub on the NDirectMethodDesc/ComPlusCallMethodDesc/DelegateEEClass
            // that we can recover the stub MethodDesc at prestub time, do the fixups, and wire up the native code
            if (pStubMD != NULL)
            {
                 SetStubMethodDescOnInteropMethodDesc(pMD, pStubMD, false /* fReverseStub */);
                 pStubMD = NULL;
            }

        }
    }
    EX_CATCH
    {
        LOG((LF_ZAP, LL_WARNING, "NGEN_ILSTUB: Generating forward interop stub FAILED: %s::%s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));
    }
    EX_END_CATCH(RethrowTransientExceptions);

    //
    // Now take care of reverse P/Invoke stubs for delegates
    //
    if (pMD->IsEEImpl() && COMDelegate::IsDelegateInvokeMethod(pMD))
    {
        // Reverse P/Invoke is not supported for generic methods and WinRT delegates
        if (!pMD->HasClassOrMethodInstantiation() && !pMD->GetMethodTable()->IsProjectedFromWinRT())
        {
            EX_TRY
            {
#ifdef _TARGET_X86_
                // on x86, we call the target directly if Invoke has a no-marshal signature
                if (NDirect::MarshalingRequired(pMD))
#endif // _TARGET_X86_
                {
                    PInvokeStaticSigInfo sigInfo(pMD);
                    pStubMD = UMThunkMarshInfo::GetILStubMethodDesc(pMD, &sigInfo, NDIRECTSTUB_FL_DELEGATE | dwNGenStubFlags);

                    if (pStubMD != NULL)
                    {
                        // compile the reverse stub
                        pStubMD = CompileMethodStubIfNeeded(pMD, pStubMD, pfnCallback, pCallbackContext);

                        // We store the MethodDesc of the Stub on the DelegateEEClass
                        if (pStubMD != NULL)
                        {
                            SetStubMethodDescOnInteropMethodDesc(pMD, pStubMD, true /* fReverseStub */);
                        }
                    }
                }
            }
            EX_CATCH
            {
                LOG((LF_ZAP, LL_WARNING, "NGEN_ILSTUB: Generating reverse interop stub for delegate FAILED: %s::%s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));
            }
            EX_END_CATCH(RethrowTransientExceptions);
        }
    }

#ifdef FEATURE_COMINTEROP
    //
    // And finally generate reverse COM stubs
    //
    EX_TRY
    {
        // The method doesn't have to have a special type to be exposed to COM, in particular it doesn't
        // have to be ComPlusCallMethodDesc. However, it must have certain properties (custom attributes,
        // public visibility, etc.)
        if (MethodNeedsReverseComStub(pMD))
        {
            // initialize ComCallMethodDesc
            ComCallMethodDesc ccmd;
            ComCallMethodDescHolder ccmdHolder(&ccmd);
            ccmd.InitMethod(pMD, NULL);

            // generate the IL stub
            DWORD dwStubFlags;
            ComCall::PopulateComCallMethodDesc(&ccmd, &dwStubFlags);
            pStubMD = ComCall::GetILStubMethodDesc(pMD, dwStubFlags | dwNGenStubFlags);

            if (pStubMD != NULL)
            {
                // compile the reverse stub
                pStubMD = CompileMethodStubIfNeeded(pMD, pStubMD, pfnCallback, pCallbackContext);

                if (pStubMD != NULL)
                {
                    // store the stub in a hash table on the module
                    m_image->GetModule()->GetStubMethodHashTable()->InsertMethodDesc(pMD, pStubMD);
                }
            }
        }
    }
    EX_CATCH
    {
        LOG((LF_ZAP, LL_WARNING, "NGEN_ILSTUB: Generating reverse interop stub FAILED: %s::%s\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));
    }
    EX_END_CATCH(RethrowTransientExceptions);
#endif // FEATURE_COMINTEROP
}

bool CEEPreloader::IsDynamicMethod(CORINFO_METHOD_HANDLE hMethod)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pMD = GetMethod(hMethod);

    if (pMD)
    {
        return pMD->IsDynamicMethod();
    }

    return false;
}

// Set method profiling flags for layout of EE datastructures
void CEEPreloader::SetMethodProfilingFlags(CORINFO_METHOD_HANDLE hMethod, DWORD flags)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(hMethod != NULL);
    _ASSERTE(flags != 0);

    return m_image->SetMethodProfilingFlags(GetMethod(hMethod), flags);
}

/*********************************************************************/
// canSkipMethodPreparation: Is there a need for all calls from
// NGEN'd code to a particular MethodDesc to go through DoPrestub,
// depending on the method sematics?  If so return FALSE.
//
// This is used to rule out both ngen-hardbinds and intra-ngen-module
// direct calls.
//
// The cases where direct calls are not allowed are typically where
// a stub must be inserted by DoPrestub (we do not save stubs) or where
// we haven't saved the code for some reason or another, or where fixups
// are required in the MethodDesc.
//
// callerHnd=NULL implies any/unspecified caller.
//
// Note that there may be other requirements for going through the prestub
// which vary based on the scenario. These need to be handled separately

bool CEEPreloader::CanSkipMethodPreparation (
        CORINFO_METHOD_HANDLE   callerHnd,
        CORINFO_METHOD_HANDLE   calleeHnd,
        CorInfoIndirectCallReason *pReason,
        CORINFO_ACCESS_FLAGS    accessFlags/*=CORINFO_ACCESS_ANY*/)
{
    STANDARD_VM_CONTRACT;

    bool result = false;

    COOPERATIVE_TRANSITION_BEGIN();

    MethodDesc *  calleeMD    = (MethodDesc *)calleeHnd;
    MethodDesc *  callerMD    = (MethodDesc *)callerHnd;

    {
        result = calleeMD->CanSkipDoPrestub(callerMD, pReason, accessFlags);
    }

    COOPERATIVE_TRANSITION_END();

    return result;
}

CORINFO_METHOD_HANDLE CEEPreloader::LookupMethodDef(mdMethodDef token)
{
    STANDARD_VM_CONTRACT;
    MethodDesc *resultMD = nullptr;

    EX_TRY
    {
        MethodDesc *pMD = MemberLoader::GetMethodDescFromMethodDef(m_image->GetModule(), token, FALSE);

        if (IsReadyToRunCompilation() && pMD->HasClassOrMethodInstantiation())
        {
            _ASSERTE(IsCompilationProcess() && pMD->GetModule_NoLogging() == GetAppDomain()->ToCompilationDomain()->GetTargetModule());
        }

        resultMD = pMD->FindOrCreateTypicalSharedInstantiation();
    }
    EX_CATCH
    {
        this->Error(token, GET_EXCEPTION());
    }
    EX_END_CATCH(SwallowAllExceptions)

    return CORINFO_METHOD_HANDLE(resultMD);
}

bool CEEPreloader::GetMethodInfo(mdMethodDef token, CORINFO_METHOD_HANDLE ftnHnd, CORINFO_METHOD_INFO * methInfo)
{
    STANDARD_VM_CONTRACT;
    bool result = false;

    EX_TRY
    {
        result = GetZapJitInfo()->getMethodInfo(ftnHnd, methInfo);
    }
    EX_CATCH
    {
        result = false;
        this->Error(token, GET_EXCEPTION());
    }
    EX_END_CATCH(SwallowAllExceptions)

    return result;
}

static BOOL MethodIsVisibleOutsideItsAssembly(DWORD dwMethodAttr)
{
    LIMITED_METHOD_CONTRACT;
    return (IsMdPublic(dwMethodAttr) ||
        IsMdFamORAssem(dwMethodAttr) ||
        IsMdFamily(dwMethodAttr));
}

static BOOL ClassIsVisibleOutsideItsAssembly(DWORD dwClassAttr, BOOL fIsGlobalClass)
{
    LIMITED_METHOD_CONTRACT;

    if (fIsGlobalClass)
    {
        return TRUE;
    }

    return (IsTdPublic(dwClassAttr) ||
        IsTdNestedPublic(dwClassAttr) ||
        IsTdNestedFamily(dwClassAttr) ||
        IsTdNestedFamORAssem(dwClassAttr));
}

static BOOL MethodIsVisibleOutsideItsAssembly(MethodDesc * pMD)
{
    LIMITED_METHOD_CONTRACT;

    MethodTable * pMT = pMD->GetMethodTable();

    if (!ClassIsVisibleOutsideItsAssembly(pMT->GetAttrClass(), pMT->IsGlobalClass()))
        return FALSE;

    return MethodIsVisibleOutsideItsAssembly(pMD->GetAttrs());
}

CorCompileILRegion CEEPreloader::GetILRegion(mdMethodDef token)
{
    STANDARD_VM_CONTRACT;

    // Since we are running managed code during NGen the inlining hint may be 
    // changing underneeth us as the code is JITed. We need to prevent the inlining
    // hints from changing once we start to use them to place IL in the image.
    g_pCEECompileInfo->DisableCachingOfInliningHints();

    // Default if there is something completely wrong, e.g. the type failed to load.
    // We may need the IL at runtime.
    CorCompileILRegion region = CORCOMPILE_ILREGION_WARM;

    EX_TRY
    {
        MethodDesc *pMD = m_image->GetModule()->LookupMethodDef(token);

        if (pMD == NULL || !pMD->GetMethodTable()->IsFullyLoaded())
        {
            // Something is completely wrong - use the default
        }
        else
        if (m_image->IsStored(pMD))
        {
            if (pMD->IsNotInline())
            {
                if (pMD->HasClassOrMethodInstantiation())
                {
                    region = CORCOMPILE_ILREGION_GENERICS;
                }
                else
                {
                    region = CORCOMPILE_ILREGION_COLD;
                }
            }
            else
            if (MethodIsVisibleOutsideItsAssembly(pMD))
            {
                // We are inlining only leaf methods, except for mscorlib. Thus we can assume that only methods
                // visible outside its assembly are likely to be inlined.
                region = CORCOMPILE_ILREGION_INLINEABLE;
            }
            else
            {
                // We may still need the IL of the non-nonvisible methods for inlining in certain scenarios:
                // dynamically emitted IL, friend assemblies or JITing of generic instantiations
                region = CORCOMPILE_ILREGION_WARM;
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

    return region;
}


CORINFO_METHOD_HANDLE CEEPreloader::FindMethodForProfileEntry(CORBBTPROF_BLOB_PARAM_SIG_ENTRY * profileBlobEntry)
{
    STANDARD_VM_CONTRACT;
    MethodDesc *  pMethod = nullptr;

    _ASSERTE(profileBlobEntry->blob.type == ParamMethodSpec);

    if (PartialNGenStressPercentage() != 0)
        return CORINFO_METHOD_HANDLE( NULL );

    Module * pModule = GetAppDomain()->ToCompilationDomain()->GetTargetModule();
    pMethod = pModule->LoadIBCMethodHelper(m_image, profileBlobEntry);
   
    return CORINFO_METHOD_HANDLE( pMethod );
}

void CEEPreloader::ReportInlining(CORINFO_METHOD_HANDLE inliner, CORINFO_METHOD_HANDLE inlinee)
{
    STANDARD_VM_CONTRACT;
    m_image->ReportInlining(inliner, inlinee);
}

void CEEPreloader::Link()
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    m_image->PreSave();

    m_image->GetModule()->Save(m_image);
    m_image->GetModule()->Arrange(m_image);
    m_image->GetModule()->Fixup(m_image);

    m_image->PostSave();

    COOPERATIVE_TRANSITION_END();
}

void CEEPreloader::FixupRVAs()
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    m_image->FixupRVAs();

    COOPERATIVE_TRANSITION_END();
}

void CEEPreloader::SetRVAsForFields(IMetaDataEmit * pEmit)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    m_image->SetRVAsForFields(pEmit);

    COOPERATIVE_TRANSITION_END();
}

void CEEPreloader::GetRVAFieldData(mdFieldDef fd, PVOID * ppData, DWORD * pcbSize, DWORD * pcbAlignment)
{
    STANDARD_VM_CONTRACT;

    COOPERATIVE_TRANSITION_BEGIN();

    FieldDesc * pFD = m_image->GetModule()->LookupFieldDef(fd);
    if (pFD == NULL)
        ThrowHR(COR_E_TYPELOAD);

    _ASSERTE(pFD->IsRVA());

    UINT size = pFD->LoadSize();

    // 
    // Compute an alignment for the data based on the alignment
    // of the RVA.  We'll align up to 8 bytes.
    //

    UINT align = 1;
    DWORD rva = pFD->GetOffset();
    DWORD rvaTemp = rva;   

    while ((rvaTemp&1) == 0 && align < 8 && align < size)
    {
        align <<= 1;
        rvaTemp >>= 1;
    }


    *ppData = pFD->GetStaticAddressHandle(NULL);
    *pcbSize = size;
    *pcbAlignment = align;

    COOPERATIVE_TRANSITION_END();
}

ULONG CEEPreloader::Release()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    delete this;
    return 0;
}

#ifdef FEATURE_READYTORUN_COMPILER
void CEEPreloader::GetSerializedInlineTrackingMap(SBuffer* pBuffer)
{
    InlineTrackingMap * pInlineTrackingMap = m_image->GetInlineTrackingMap();
    PersistentInlineTrackingMapR2R::Save(m_image->GetHeap(), pBuffer, pInlineTrackingMap);
}
#endif

void CEEPreloader::Error(mdToken token, Exception * pException)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = pException->GetHR();
    UINT    resID = 0;

    StackSString msg;

#ifdef CROSSGEN_COMPILE
    pException->GetMessage(msg);

    // Do we have an EEException with a resID?
    if (EEMessageException::IsEEMessageException(pException))
    {
        EEMessageException * pEEMessageException = (EEMessageException *) pException;
        resID = pEEMessageException->GetResID();
    }
#else
    {
        GCX_COOP();

        // Going though throwable gives more verbose error messages in certain cases that our tests depend on.
        OBJECTREF throwable = NingenEnabled() ? NULL : CLRException::GetThrowableFromException(pException);

        if (throwable != NULL)
        {
            GetExceptionMessage(throwable, msg);
        }
        else
        {
            pException->GetMessage(msg);
        }
    }
#endif
    
    m_pData->Error(token, hr, resID, msg.GetUnicode());
}

CEEInfo *g_pCEEInfo = NULL;

ICorDynamicInfo * __stdcall GetZapJitInfo()
{
    STANDARD_VM_CONTRACT;

    if (g_pCEEInfo == NULL)
    {
        CEEInfo * p = new CEEInfo();
        if (InterlockedCompareExchangeT(&g_pCEEInfo, p, NULL) != NULL)
            delete p;
    }

    return g_pCEEInfo;
}

CEECompileInfo *g_pCEECompileInfo = NULL;

ICorCompileInfo * __stdcall GetCompileInfo()
{
    STANDARD_VM_CONTRACT;

    if (g_pCEECompileInfo == NULL)
    {
        CEECompileInfo * p = new CEECompileInfo();
        if (InterlockedCompareExchangeT(&g_pCEECompileInfo, p, NULL) != NULL)
            delete p;
    }

    return g_pCEECompileInfo;
}

//
// CompilationDomain
//

CompilationDomain::CompilationDomain(BOOL fForceDebug,
                                     BOOL fForceProfiling,
                                     BOOL fForceInstrument)
  : m_fForceDebug(fForceDebug),
    m_fForceProfiling(fForceProfiling),
    m_fForceInstrument(fForceInstrument),
    m_pTargetAssembly(NULL),
    m_pTargetModule(NULL),
    m_pTargetImage(NULL),
    m_pEmit(NULL),
    m_pDependencyRefSpecs(NULL),
    m_pDependencies(NULL),
    m_cDependenciesCount(0),
    m_cDependenciesAlloc(0)
{
    STANDARD_VM_CONTRACT;

}

void CompilationDomain::ReleaseDependencyEmitter()
{
    m_pDependencyRefSpecs.Release();

    m_pEmit.Release();
}

CompilationDomain::~CompilationDomain()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pDependencies != NULL)
        delete [] m_pDependencies;

    ReleaseDependencyEmitter();

    for (unsigned i = 0; i < m_rRefCaches.Size(); i++)
    {
        delete m_rRefCaches[i];
        m_rRefCaches[i]=NULL;
    }

}

void CompilationDomain::Init()
{
    STANDARD_VM_CONTRACT;

#ifndef CROSSGEN_COMPILE
    AppDomain::Init();
#endif

#ifndef CROSSGEN_COMPILE
    // allocate a Virtual Call Stub Manager for the compilation domain
    InitVSD();
#endif

    SetCompilationDomain();
}

HRESULT CompilationDomain::AddDependencyEntry(PEAssembly *pFile,
                                           mdAssemblyRef ref,
                                           mdAssemblyRef def)
{
#ifdef _DEBUG
    // This method is not multi-thread safe.  This is OK because it is only called by NGen compiling, which is
    // effectively single-threaded.  The following code verifies that we're not called on multiple threads.
    static volatile LONG threadId = 0;
    if (threadId == 0)
    {
        InterlockedCompareExchange(&threadId, GetCurrentThreadId(), 0);
    }
    _ASSERTE((LONG)GetCurrentThreadId() == threadId);
#endif // _DEBUG

    _ASSERTE((pFile == NULL) == (def == mdAssemblyRefNil));

    if (m_cDependenciesCount == m_cDependenciesAlloc)
    {
        // Save the new count in a local variable.  Can't update m_cDependenciesAlloc until the new
        // CORCOMPILE_DEPENDENCY array is allocated, otherwise an out-of-memory exception from new[]
        // operator would put the data in an inconsistent state, causing heap corruption later.
        USHORT cNewDependenciesAlloc = m_cDependenciesAlloc == 0 ? 20 : m_cDependenciesAlloc * 2;

        // Grow m_pDependencies

        NewArrayHolder<CORCOMPILE_DEPENDENCY> pNewDependencies(new CORCOMPILE_DEPENDENCY[cNewDependenciesAlloc]);
        {
            // This block must execute transactionally. No throwing allowed. No bailing allowed.
            FAULT_FORBID();

            memset(pNewDependencies,  0, cNewDependenciesAlloc*sizeof(CORCOMPILE_DEPENDENCY));

            if (m_pDependencies)
            {
                memcpy(pNewDependencies, m_pDependencies,
                       m_cDependenciesCount*sizeof(CORCOMPILE_DEPENDENCY));
    
                delete [] m_pDependencies;
            }
    
            m_pDependencies = pNewDependencies.Extract();
            m_cDependenciesAlloc = cNewDependenciesAlloc;
        }
    }

    CORCOMPILE_DEPENDENCY *pDependency = &m_pDependencies[m_cDependenciesCount++];

    // Clear memory so that we won't write random data into the zapped file
    ZeroMemory(pDependency, sizeof(CORCOMPILE_DEPENDENCY));

    pDependency->dwAssemblyRef = ref;

    pDependency->dwAssemblyDef = def;

    pDependency->signNativeImage = INVALID_NGEN_SIGNATURE;

    if (pFile)
    {
        DomainAssembly *pAssembly = GetAppDomain()->LoadDomainAssembly(NULL, pFile, FILE_LOAD_CREATE);
        // Note that this can trigger an assembly load (of mscorlib)
        pAssembly->GetOptimizedIdentitySignature(&pDependency->signAssemblyDef);



        //
        // This is done in CompilationDomain::CanEagerBindToZapFile with full support for hardbinding
        //
        if (pFile->IsSystem() && pFile->HasNativeImage())
        {
            CORCOMPILE_VERSION_INFO * pNativeVersion = pFile->GetLoadedNative()->GetNativeVersionInfo();
            pDependency->signNativeImage = pNativeVersion->signature;
        }

    }

    return S_OK;
}

HRESULT CompilationDomain::AddDependency(AssemblySpec *pRefSpec,
                                         PEAssembly * pFile)
{
    HRESULT hr;

    //
    // Record the dependency
    //

    // This assert prevents dependencies from silently being loaded without being recorded.
    _ASSERTE(m_pEmit);

    // Normalize any reference to mscorlib; we don't want to record other non-canonical
    // mscorlib references in the ngen image since fusion doesn't understand how to bind them.
    // (Not to mention the fact that they are redundant.)
    AssemblySpec spec;
    if (pRefSpec->IsMscorlib())
    {
        _ASSERTE(pFile); // mscorlib had better not be missing
        if (!pFile)
            return E_UNEXPECTED;

        // Don't store a binding from mscorlib to itself.
        if (m_pTargetAssembly == SystemDomain::SystemAssembly())
            return S_OK;

        spec.InitializeSpec(pFile);
        pRefSpec = &spec;
    }
    else if (m_pTargetAssembly == NULL && pFile)
    {
        // If target assembly is still NULL, we must be loading either the target assembly or mscorlib.
        // Mscorlib is already handled above, so we must be loading the target assembly if we get here.
        // Use the assembly name given in the target assembly so that the native image is deterministic
        // regardless of how the target assembly is specified on the command line.
        spec.InitializeSpec(pFile);
        if (spec.IsStrongNamed() && spec.HasPublicKey())
        {
            spec.ConvertPublicKeyToToken();
        }
        pRefSpec = &spec;
    }
    else if (pRefSpec->IsStrongNamed() && pRefSpec->HasPublicKey())
    {
        // Normalize to always use public key token.  Otherwise we may insert one reference
        // using public key, and another reference using public key token.
        spec.CopyFrom(pRefSpec);
        spec.ConvertPublicKeyToToken();
        pRefSpec = &spec;
    }

#ifdef FEATURE_COMINTEROP
    // Only cache ref specs that have a unique identity. This is needed to avoid caching
    // things like WinRT type specs, which would benefit very little from being cached.
    if (!pRefSpec->HasUniqueIdentity())
    {
        // Successful bind of a reference with a non-unique assembly identity.
        _ASSERTE(pRefSpec->IsContentType_WindowsRuntime());

        AssemblySpec defSpec;
        if (pFile != NULL)
        {
            defSpec.InitializeSpec(pFile);

            // Windows Runtime Native Image binding depends on details exclusively described by the definition winmd file.
            // Therefore we can actually drop the existing ref spec here entirely.
            // Also, Windows Runtime Native Image binding uses the simple name of the ref spec as the
            // resolution rule for PreBind when finding definition assemblies.
            // See comment on CLRPrivBinderWinRT::PreBind for further details.
            pRefSpec = &defSpec;
        }

        // Unfortunately, we don't have any choice regarding failures (pFile == NULL) because
        // there is no value to canonicalize on (i.e., a def spec created from a non-NULL
        // pFile) and so we must cache all non-unique-assembly-id failures.
        const AssemblySpecDefRefMapEntry * pEntry = m_dependencyDefRefMap.LookupPtr(&defSpec);
        if (pFile == NULL || pEntry == NULL)
        {
            mdAssemblyRef refToken = mdAssemblyRefNil;
            IfFailRet(pRefSpec->EmitToken(m_pEmit, &refToken, TRUE, TRUE));

            mdAssemblyRef defToken = mdAssemblyRefNil;
            if (pFile != NULL)
            {
                IfFailRet(defSpec.EmitToken(m_pEmit, &defToken, TRUE, TRUE));

                NewHolder<AssemblySpec> pNewDefSpec = new AssemblySpec();
                pNewDefSpec->CopyFrom(&defSpec);
                pNewDefSpec->CloneFields();

                NewHolder<AssemblySpec> pNewRefSpec = new AssemblySpec();
                pNewRefSpec->CopyFrom(pRefSpec);
                pNewRefSpec->CloneFields();

                _ASSERTE(m_dependencyDefRefMap.LookupPtr(pNewDefSpec) == NULL);

                AssemblySpecDefRefMapEntry e;
                e.m_pDef = pNewDefSpec;
                e.m_pRef = pNewRefSpec;
                m_dependencyDefRefMap.Add(e);

                pNewDefSpec.SuppressRelease();
                pNewRefSpec.SuppressRelease();
            }

            IfFailRet(AddDependencyEntry(pFile, refToken, defToken));
        }
    }
    else
#endif // FEATURE_COMINTEROP
    {
        //
        // See if we've already added the contents of the ref
        // Else, emit token for the ref
        //

        if (m_pDependencyRefSpecs->Store(pRefSpec))
            return S_OK;

        mdAssemblyRef refToken;
        IfFailRet(pRefSpec->EmitToken(m_pEmit, &refToken));

        //
        // Make a spec for the bound assembly
        //

        mdAssemblyRef defToken = mdAssemblyRefNil;

        // All dependencies of a shared assembly need to be shared. So for a shared
        // assembly, we want to remember the missing assembly ref during ngen, so that
        // we can probe eagerly for the dependency at load time, and make sure that
        // it is loaded as shared.
        // In such a case, pFile will be NULL
        if (pFile)
        {
            AssemblySpec assemblySpec;
            assemblySpec.InitializeSpec(pFile);

            IfFailRet(assemblySpec.EmitToken(m_pEmit, &defToken));
        }

        //
        // Add the entry.  Include the PEFile if we are not doing explicit bindings.
        //

        IfFailRet(AddDependencyEntry(pFile, refToken, defToken));
    }

    return S_OK;
}

//----------------------------------------------------------------------------
AssemblySpec* CompilationDomain::FindAssemblyRefSpecForDefSpec(
    AssemblySpec* pDefSpec)
{
    WRAPPER_NO_CONTRACT;

    if (pDefSpec == nullptr)
        return nullptr;

    const AssemblySpecDefRefMapEntry * pEntry = m_dependencyDefRefMap.LookupPtr(pDefSpec);
    _ASSERTE(pEntry != NULL);

    return (pEntry != NULL) ? pEntry->m_pRef : NULL;
}


//----------------------------------------------------------------------------
// Is it OK to embed direct pointers to an ngen dependency?
// true if hardbinding is OK, false otherwise
//
// targetModule - The pointer points into the native image of this Module.
//                If this native image gets relocated, the native image of
//                the source Module is invalidated unless the embedded
//                pointer can be fixed up appropriately.
// limitToHardBindList - Is it OK to hard-bind to a dependency even if it is
//                not asked for explicitly?

BOOL CompilationDomain::CanEagerBindToZapFile(Module *targetModule, BOOL limitToHardBindList)
{
    // We do this check before checking the hashtables because m_cantHardBindModules
    // will contain non-manifest modules. However, we do want them to be able
    // to hard-bind to themselves
    if (targetModule == m_pTargetModule)
    {
        return TRUE;
    }

    //
    // CoreCLR does not have attributes for fine grained eager binding control.
    // We hard bind to mscorlib.dll only.
    //
    return targetModule->IsSystem();
}


void CompilationDomain::SetTarget(Assembly *pAssembly, Module *pModule)
{
    STANDARD_VM_CONTRACT;

    m_pTargetAssembly = pAssembly;
    m_pTargetModule = pModule;
}

void CompilationDomain::SetTargetImage(DataImage *pImage, CEEPreloader * pPreloader)
{
    STANDARD_VM_CONTRACT;

    m_pTargetImage = pImage;
    m_pTargetPreloader = pPreloader;

    _ASSERTE(pImage->GetModule() == GetTargetModule());
}

void ReportMissingDependency(Exception * e)
{
    // Avoid duplicate error messages
    if (FAILED(g_hrFatalError))
        return;

    SString s;

    e->GetMessage(s);
    GetSvcLogger()->Printf(LogLevel_Error, W("Error: %s\n"), s.GetUnicode());

    g_hrFatalError = COR_E_FILELOAD;
}

PEAssembly *CompilationDomain::BindAssemblySpec(
    AssemblySpec *pSpec,
    BOOL fThrowOnFileNotFound,
    BOOL fUseHostBinderIfAvailable)
{
    PEAssembly *pFile = NULL;
    //
    // Do the binding
    //

    EX_TRY
    {
        //
        // Use normal binding rules
        // (possibly with our custom IApplicationContext)
        //
        pFile = AppDomain::BindAssemblySpec(
            pSpec,
            fThrowOnFileNotFound,
            fUseHostBinderIfAvailable);
    }
    EX_HOOK
    {
        if (!g_fNGenMissingDependenciesOk)
        {
            ReportMissingDependency(GET_EXCEPTION());
            EX_RETHROW;
        }

        //
        // Record missing dependencies
        //
#ifdef FEATURE_COMINTEROP                
        if (!g_fNGenWinMDResilient || pSpec->HasUniqueIdentity())
#endif
        {
            IfFailThrow(AddDependency(pSpec, NULL));
        }
    }
    EX_END_HOOK

#ifdef FEATURE_COMINTEROP                
    if (!g_fNGenWinMDResilient || pSpec->HasUniqueIdentity())
#endif
    {
        IfFailThrow(AddDependency(pSpec, pFile));
    }

    return pFile;
}

HRESULT
    CompilationDomain::SetContextInfo(LPCWSTR path, BOOL isExe)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    COOPERATIVE_TRANSITION_BEGIN();


    COOPERATIVE_TRANSITION_END();

    return hr;
}

void CompilationDomain::SetDependencyEmitter(IMetaDataAssemblyEmit *pEmit)
{
    STANDARD_VM_CONTRACT;

    pEmit->AddRef();
    m_pEmit = pEmit;

    m_pDependencyRefSpecs = new AssemblySpecHash();
}


HRESULT
    CompilationDomain::GetDependencies(CORCOMPILE_DEPENDENCY **ppDependencies,
                                       DWORD *pcDependencies)
{
    STANDARD_VM_CONTRACT;


    //
    // Return the bindings.
    //

    *ppDependencies = m_pDependencies;
    *pcDependencies = m_cDependenciesCount;

    // Cannot add any more dependencies
    ReleaseDependencyEmitter();

    return S_OK;
}


#ifdef CROSSGEN_COMPILE
HRESULT CompilationDomain::SetPlatformWinmdPaths(LPCWSTR pwzPlatformWinmdPaths)
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_COMINTEROP
    // Create the array list on the heap since it will be passed off for the Crossgen RoResolveNamespace mockup to keep for the life of the process
    StringArrayList *saPaths = new StringArrayList();

    SString strPaths(pwzPlatformWinmdPaths);
    if (!strPaths.IsEmpty())
    {
        for (SString::Iterator i = strPaths.Begin(); i != strPaths.End(); )
        {
            // Skip any leading spaces or semicolons
            if (strPaths.Skip(i, W(';')))
            {
                continue;
            }
        
            SString::Iterator iEnd = i;     // Where current assembly name ends
            SString::Iterator iNext;        // Where next assembly name starts
            if (strPaths.Find(iEnd, W(';')))
            {
                iNext = iEnd + 1;
            }
            else
            {
                iNext = iEnd = strPaths.End();
            }
        
            _ASSERTE(i < iEnd);
            if(i != iEnd)
            {
                saPaths->Append(SString(strPaths, i, iEnd));
            }
            i = iNext;
        }
    }
    Crossgen::SetFirstPartyWinMDPaths(saPaths);
#endif // FEATURE_COMINTEROP

    return S_OK;
}
#endif // CROSSGEN_COMPILE


#endif // FEATURE_PREJIT
