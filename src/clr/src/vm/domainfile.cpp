// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// DomainFile.cpp
//

// --------------------------------------------------------------------------------


#include "common.h"

// --------------------------------------------------------------------------------
// Headers
// --------------------------------------------------------------------------------

#include <shlwapi.h>

#include "security.h"
#include "securitymeta.h"
#include "invokeutil.h"
#include "eeconfig.h"
#include "dynamicmethod.h"
#include "field.h"
#include "dbginterface.h"
#include "eventtrace.h"

#ifdef FEATURE_PREJIT
#include <corcompile.h>
#include "compile.h"
#endif  // FEATURE_PREJIT

#include "umthunkhash.h"
#include "peimagelayout.inl"

#if !defined(FEATURE_CORECLR) && !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
#include "policy.h" // for fusion::util::isanyframeworkassembly
#endif
#include "winrthelpers.h"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif // FEATURE_PERFMAP

BOOL DomainAssembly::IsUnloading()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    BOOL fIsUnloading = FALSE;

    fIsUnloading = this->GetAppDomain()->IsUnloading();

    if (!fIsUnloading)
    {
        fIsUnloading = m_fDebuggerUnloadStarted;
    }

    return fIsUnloading;
}


#ifndef DACCESS_COMPILE
DomainFile::DomainFile(AppDomain *pDomain, PEFile *pFile)
  : m_pDomain(pDomain),
    m_pFile(pFile),
    m_pOriginalFile(NULL),
    m_pModule(NULL),
    m_level(FILE_LOAD_CREATE),
    m_pError(NULL),
    m_notifyflags(NOT_NOTIFIED),
    m_loading(TRUE),
    m_pDynamicMethodTable(NULL),
    m_pUMThunkHash(NULL),
    m_bDisableActivationCheck(FALSE),
    m_dwReasonForRejectingNativeImage(0)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;  // From CreateHandle
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_hExposedModuleObject = NULL;
    pFile->AddRef();
}

DomainFile::~DomainFile()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pFile->Release();
    if(m_pOriginalFile)
        m_pOriginalFile->Release();
    if (m_pDynamicMethodTable)
        m_pDynamicMethodTable->Destroy();
#if defined(FEATURE_MIXEDMODE) && !defined(CROSSGEN_COMPILE)
    if (m_pUMThunkHash)
        delete m_pUMThunkHash;
#endif
    delete m_pError;
}

#endif //!DACCESS_COMPILE

LoaderAllocator * DomainFile::GetLoaderAllocator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    Assembly *pAssembly = GetDomainAssembly()->GetAssembly();
    if ((pAssembly != NULL) && (pAssembly->IsCollectible()))
    {
        return pAssembly->GetLoaderAllocator();
    }
    else
    {
        return this->GetAppDomain()->GetLoaderAllocator();
    }
}

#ifndef DACCESS_COMPILE

void DomainFile::ReleaseFiles()
{
    WRAPPER_NO_CONTRACT;
    Module* pModule=GetCurrentModule();
    if(pModule)
        pModule->StartUnload();

    if (m_pFile)
        m_pFile->ReleaseIL();
    if(m_pOriginalFile)
        m_pOriginalFile->ReleaseIL();

    if(pModule)
        pModule->ReleaseILData();
}

BOOL DomainFile::TryEnsureActive()
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    BOOL success = TRUE;

    EX_TRY
      {
          EnsureActive();
      }
    EX_CATCH
      {
          success = FALSE;
      }
    EX_END_CATCH(RethrowTransientExceptions);

    RETURN success;
}

// Optimization intended for EnsureLoadLevel only
#include <optsmallperfcritical.h>
void DomainFile::EnsureLoadLevel(FileLoadLevel targetLevel)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    TRIGGERSGC ();
    if (IsLoading())
    {
        this->GetAppDomain()->LoadDomainFile(this, targetLevel);

        // Enforce the loading requirement.  Note that we may have a deadlock in which case we
        // may be off by one which is OK.  (At this point if we are short of targetLevel we know
        // we have done so because of reentrancy contraints.)

        RequireLoadLevel((FileLoadLevel)(targetLevel-1));
    }
    else
        ThrowIfError(targetLevel);

    RETURN;
}
#include <optdefault.h>

void DomainFile::AttemptLoadLevel(FileLoadLevel targetLevel)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    if (IsLoading())
        this->GetAppDomain()->LoadDomainFile(this, targetLevel);
    else
        ThrowIfError(targetLevel);

    RETURN;
}


CHECK DomainFile::CheckLoadLevel(FileLoadLevel requiredLevel, BOOL deadlockOK)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (deadlockOK)
    {
#ifndef CROSSGEN_COMPILE
        // CheckLoading requires waiting on a host-breakable lock.
        // Since this is only a checked-build assert and we've been
        // living with it for a while, I'll leave it as is.
        //@TODO: CHECK statements are *NOT* debug-only!!!
        CONTRACT_VIOLATION(ThrowsViolation|GCViolation|TakesLockViolation);
        CHECK(this->GetAppDomain()->CheckLoading(this, requiredLevel));
#endif
    }
    else
    {
        CHECK_MSG(m_level >= requiredLevel,
                  "File not sufficiently loaded");
    }

    CHECK_OK;
}



void DomainFile::RequireLoadLevel(FileLoadLevel targetLevel)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    if (GetLoadLevel() < targetLevel)
    {
        ThrowIfError(targetLevel);
        ThrowHR(MSEE_E_ASSEMBLYLOADINPROGRESS); // @todo: better exception
    }

    RETURN;
}


void DomainFile::SetError(Exception *ex)
{
    CONTRACT_VOID
    {
        PRECONDITION(!IsError());
        PRECONDITION(ex != NULL);
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        POSTCONDITION(IsError());
    }
    CONTRACT_END;

    m_pError = new ExInfo(ex->DomainBoundClone());

    GetCurrentModule()->NotifyEtwLoadFinished(ex->GetHR());

    if (!IsProfilerNotified())
    {
        SetProfilerNotified();

#ifdef PROFILING_SUPPORTED
        if (GetCurrentModule() != NULL
            && !GetCurrentModule()->GetAssembly()->IsDomainNeutral())
        {
            // Only send errors for non-shared assemblies; other assemblies might be successfully completed
            // in another app domain later.
            GetCurrentModule()->NotifyProfilerLoadFinished(ex->GetHR());
        }
#endif
    }

    RETURN;
}

void DomainFile::ThrowIfError(FileLoadLevel targetLevel)
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACT_END;

    if (m_level < targetLevel)
    {
        if (m_pError)
            m_pError->Throw();
    }

    RETURN;
}

CHECK DomainFile::CheckNoError(FileLoadLevel targetLevel)
{
    LIMITED_METHOD_CONTRACT;
    CHECK(m_level >= targetLevel
          || !IsError());

    CHECK_OK;
}

CHECK DomainFile::CheckLoaded()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CHECK_MSG(CheckNoError(FILE_LOADED), "DomainFile load resulted in an error");

    if (IsLoaded())
        CHECK_OK;

    // Mscorlib is allowed to run managed code much earlier than other
    // assemblies for bootstrapping purposes.  This is because it has no
    // dependencies, security checks, and doesn't rely on loader notifications.

    if (GetFile()->IsSystem())
        CHECK_OK;

    CHECK_MSG(GetFile()->CheckLoaded(), "PEFile has not been loaded");

    CHECK_OK;
}

CHECK DomainFile::CheckActivated()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CHECK_MSG(CheckNoError(FILE_ACTIVE), "DomainFile load resulted in an error");

    if (IsActive())
        CHECK_OK;

    // Mscorlib is allowed to run managed code much earlier than other
    // assemblies for bootstrapping purposes.  This is because it has no
    // dependencies, security checks, and doesn't rely on loader notifications.

    if (GetFile()->IsSystem())
        CHECK_OK;

    CHECK_MSG(GetFile()->CheckLoaded(), "PEFile has not been loaded");
    CHECK_MSG(IsLoaded(), "DomainFile has not been fully loaded");
    CHECK_MSG(m_bDisableActivationCheck || CheckLoadLevel(FILE_ACTIVE), "File has not had execution verified");

    CHECK_OK;
}

#endif //!DACCESS_COMPILE

DomainAssembly *DomainFile::GetDomainAssembly()
{
    CONTRACTL
    {
        SUPPORTS_DAC;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    if (IsAssembly())
    {
        return dac_cast<PTR_DomainAssembly>(this);
    }
    else
    {
        return dac_cast<PTR_DomainModule>(this)->GetDomainAssembly();
    }
#else
    _ASSERTE(IsAssembly());
    return (DomainAssembly *) this;
#endif // FEATURE_MULTIMODULE_ASSEMBLIES
}

BOOL DomainFile::IsIntrospectionOnly()
{
    WRAPPER_NO_CONTRACT;
    return GetFile()->IsIntrospectionOnly();
}

// Return true iff the debugger should get notifications about this assembly.
//
// Notes:
//   The debuggee may be stopped while a DomainAssmebly is being initialized.  In this time window,
//   GetAssembly() may be NULL.  If that's the case, this function has to return FALSE.  Later on, when
//   the DomainAssembly is fully initialized, this function will return TRUE.  This is the only scenario
//   where this function is mutable.  In other words, a DomainAssembly can only change from being invisible
//   to visible, but NOT vice versa.  Once a DomainAssmebly is fully initialized, this function should be
//   immutable for an instance of a module. That ensures that the debugger gets consistent
//   notifications about it. It this value mutates, than the debugger may miss relevant notifications.
BOOL DomainAssembly::IsVisibleToDebugger()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // If you can't run an assembly, then don't send notifications to the debugger.
    // This check includeds IsIntrospectionOnly().
    return ((GetAssembly() != NULL) ? GetAssembly()->HasRunAccess() : FALSE);
}

#ifndef DACCESS_COMPILE
#ifdef FEATURE_PREJIT
void DomainFile::ExternalLog(DWORD level, const WCHAR *fmt, ...)
{
    WRAPPER_NO_CONTRACT;

    va_list args;
    va_start(args, fmt);

    GetOriginalFile()->ExternalVLog(LF_ZAP, level, fmt, args);

    va_end(args);
}

void DomainFile::ExternalLog(DWORD level, const char *msg)
{
    WRAPPER_NO_CONTRACT;

    GetOriginalFile()->ExternalLog(level, msg);
}
#endif

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------------------------------------
//
// Returns managed representation of the module (Module or ModuleBuilder).
// Returns NULL if the managed scout was already collected (see code:LoaderAllocator#AssemblyPhases).
//
OBJECTREF DomainFile::GetExposedModuleObject()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    LoaderAllocator * pLoaderAllocator = GetLoaderAllocator();

    if (m_hExposedModuleObject == NULL)
    {
        // Atomically create a handle
        LOADERHANDLE handle = pLoaderAllocator->AllocateHandle(NULL);

        FastInterlockCompareExchangePointer(&m_hExposedModuleObject, handle, static_cast<LOADERHANDLE>(NULL));
    }

    if (pLoaderAllocator->GetHandleValue(m_hExposedModuleObject) == NULL)
    {
        REFLECTMODULEBASEREF refClass = NULL;

        // Will be TRUE only if LoaderAllocator managed object was already collected and therefore we should
        // return NULL
        BOOL fIsLoaderAllocatorCollected = FALSE;

        GCPROTECT_BEGIN(refClass);

        if (GetFile()->IsDynamic())
        {
            refClass = (REFLECTMODULEBASEREF) AllocateObject(MscorlibBinder::GetClass(CLASS__MODULE_BUILDER));
        }
        else
        {
            refClass = (REFLECTMODULEBASEREF) AllocateObject(MscorlibBinder::GetClass(CLASS__MODULE));
        }
        refClass->SetModule(m_pModule);

        // Attach the reference to the assembly to keep the LoaderAllocator for this collectible type
        // alive as long as a reference to the module is kept alive.
        if (GetModule()->GetAssembly() != NULL)
        {
            OBJECTREF refAssembly = GetModule()->GetAssembly()->GetExposedObject();
            if ((refAssembly == NULL) && GetModule()->GetAssembly()->IsCollectible())
            {
                fIsLoaderAllocatorCollected = TRUE;
            }
            refClass->SetAssembly(refAssembly);
        }

        pLoaderAllocator->CompareExchangeValueInHandle(m_hExposedModuleObject, (OBJECTREF)refClass, NULL);
        GCPROTECT_END();

        if (fIsLoaderAllocatorCollected)
        {   // The LoaderAllocator managed object was already collected, we cannot re-create it
            // Note: We did not publish the allocated Module/ModuleBuilder object, it will get collected
            // by GC
            return NULL;
        }
    }

    return pLoaderAllocator->GetHandleValue(m_hExposedModuleObject);
} // DomainFile::GetExposedModuleObject
#endif // CROSSGEN_COMPILE

BOOL DomainFile::DoIncrementalLoad(FileLoadLevel level)
{
    STANDARD_VM_CONTRACT;

    if (IsError())
        return FALSE;

    Thread *pThread;
    pThread = GetThread();
    _ASSERTE(pThread);
    INTERIOR_STACK_PROBE_FOR(pThread, 8);

    switch (level)
    {
    case FILE_LOAD_BEGIN:
        Begin();
        break;

    case FILE_LOAD_FIND_NATIVE_IMAGE:
#ifdef FEATURE_PREJIT
        FindNativeImage();
#endif
        break;

    case FILE_LOAD_VERIFY_NATIVE_IMAGE_DEPENDENCIES:
#ifdef FEATURE_PREJIT
        VerifyNativeImageDependencies();
#endif
        break;

    case FILE_LOAD_ALLOCATE:
        Allocate();
        break;

    case FILE_LOAD_ADD_DEPENDENCIES:
        AddDependencies();
        break;

    case FILE_LOAD_PRE_LOADLIBRARY:
        PreLoadLibrary();
        break;

    case FILE_LOAD_LOADLIBRARY:
        LoadLibrary();
        break;

    case FILE_LOAD_POST_LOADLIBRARY:
        PostLoadLibrary();
        break;

    case FILE_LOAD_EAGER_FIXUPS:
        EagerFixups();
        break;

    case FILE_LOAD_VTABLE_FIXUPS:
        VtableFixups();
        break;

    case FILE_LOAD_DELIVER_EVENTS:
        DeliverSyncEvents();
        break;

    case FILE_LOADED:
        FinishLoad();
        break;

    case FILE_LOAD_VERIFY_EXECUTION:
        VerifyExecution();
        break;

    case FILE_ACTIVE:
        Activate();
        break;

    default:
        UNREACHABLE();
    }

    END_INTERIOR_STACK_PROBE;

#ifdef FEATURE_MULTICOREJIT
    {
        Module * pModule = GetModule();

        if (pModule != NULL) // Should not triggle assert when module is NULL
        {
            this->GetAppDomain()->GetMulticoreJitManager().RecordModuleLoad(pModule, level);
        }
    }
#endif

    return TRUE;
}

#ifdef FEATURE_PREJIT

void DomainFile::VerifyNativeImageDependencies(bool verifyOnly)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        PRECONDITION(verifyOnly || (m_pDomain->GetDomainFileLoadLevel(this) ==
                                    FILE_LOAD_FIND_NATIVE_IMAGE));
    }
    CONTRACTL_END;

    // This function gets called multiple times. The first call is the real work.
    // Subsequent calls are only to verify that everything still looks OK.
    if (!verifyOnly)
        ClearNativeImageStress();

    if (!m_pFile->HasNativeImage())
    {
        CheckZapRequired();
        return;
    }

    {
    // Go through native dependencies & make sure they still have their prejit images after
    // the security check.
    // NOTE: we could theoretically do this without loading the dependencies, if we cache the
    // COR_TRUST structures from the dependencies in the version information.
    //
    // Verify that all of our hard dependencies are loaded at the right base address.
    // If not, abandon prejit image (or fix ours up)
    // Also, if there are any hard dependencies, then our native image also needs to be
    // loaded at the right base address

    // Note: we will go through all of our dependencies, call Load on them, and check the base
    // addresses & identity.
    // It is important to note that all of those dependencies are also going to do the
    // same thing, so we might conceivably check a base address as OK, and then have that image
    // abandoned by that assembly during its VerifyNativeImageDependencies phase.
    // However, we avoid this problem since the hard depedencies stored are a closure of the
    // hard dependencies of an image.  This effectively means that our check here is a superset
    // of the check that the dependencies will perform.  Even if we hit a dependency loop, we
    // will still guarantee that we've examined all of our dependencies.

    ReleaseHolder<PEImage> pNativeImage = m_pFile->GetNativeImageWithRef();
    if(pNativeImage==NULL)
    {
        CheckZapRequired();
        return;
    }

    PEImageLayout* pNativeLayout = pNativeImage->GetLoadedLayout();

    // reuse same codepath for both manifest and non-manifest modules
    ReleaseHolder<PEImage> pManifestNativeImage(NULL);

    PEFile* pManifestFile = m_pFile;
    PEImageLayout* pManifestNativeLayout = pNativeLayout;

    if (!IsAssembly())
    {
        pManifestFile = GetDomainAssembly()->GetCurrentAssembly()
            ->GetManifestModule()->GetFile();

        pManifestNativeImage = pManifestFile->GetNativeImageWithRef();

        if (pManifestNativeImage == NULL)
        {
            ExternalLog(LL_ERROR, "Rejecting native image because there is no "
                "ngen image for manifest module. Check why the manifest module "
                "does not have an ngen image");
            m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_NoNiForManifestModule;
            STRESS_LOG3(LF_ZAP,LL_INFO100,"Rejecting native file %p, because its manifest module %p has no NI - reason 0x%x\n",pNativeImage.GetValue(),pManifestFile,m_dwReasonForRejectingNativeImage);
            goto NativeImageRejected;
        }

        return;
    }

    COUNT_T cDependencies;
    CORCOMPILE_DEPENDENCY *pDependencies = pManifestNativeLayout->GetNativeDependencies(&cDependencies);

    LOG((LF_ZAP, LL_INFO100, "ZAP: Checking native image dependencies for %S.\n",
         pNativeImage->GetPath().GetUnicode()));

    for (COUNT_T iDependency = 0; iDependency < cDependencies; iDependency++)
    {
        CORCOMPILE_DEPENDENCY *pDependency = &(pDependencies[iDependency]);

        // Later, for domain neutral assemblies, we will also want to verify security policy
        // in such cases, the prejit image should store the publisher info for the dependencies
        // for us.

        // If this is not a hard-bound dependency, then skip to the next dependency
        if (pDependency->signNativeImage == INVALID_NGEN_SIGNATURE)
            continue;

#ifdef FEATURE_CORECLR // hardbinding

        //
        // CoreCLR hard binds to mscorlib.dll only. Avoid going through the full load.
        //

#ifdef _DEBUG
        AssemblySpec name;
        name.InitializeSpec(pDependency->dwAssemblyRef,
                            ((pManifestNativeImage != NULL) ? pManifestNativeImage : pNativeImage)->GetNativeMDImport(),
                            GetDomainAssembly());
        _ASSERTE(name.IsMscorlib());
#endif

        PEAssembly * pDependencyFile = SystemDomain::SystemFile();
 
#else // FEATURE_CORECLR

        //
        // Load the manifest file for the given name assembly spec.
        //

        AssemblySpec name;
        name.InitializeSpec(pDependency->dwAssemblyRef,
                            ((pManifestNativeImage != NULL) ? pManifestNativeImage : pNativeImage)->GetNativeMDImport(),
                            GetDomainAssembly());

        if (this->GetAppDomain()->IsCompilationDomain())
        {
            //
            // Allow transitive closure of hardbound dependecies to be loaded during ngen.
            //

            DomainAssembly * pDependencyAssembly = name.LoadDomainAssembly(FILE_LOAD_FIND_NATIVE_IMAGE);
            pDependencyAssembly->GetFile()->SetSafeToHardBindTo();
        }

        DomainAssembly * pDependencyAssembly = NULL;
        {
            // We are about to validate the hard-bound dependencies of the assembly being loaded. The invariant of being hard-bound states
            // that each hard-bound dependency must have its NI image to be valid and available for loading and this is done recursively for each
            // hard-bound dependency.
            //
            // The validity (and presence) of the NI image happens in FILE_LOAD_ALLOCATE stage of assembly load, which is the next stage in assembly loading,
            // and not the current stage (FILE_LOAD_VERIFY_NATIVE_DEPENDENCIES). In FILE_LOAD_ALLOCATE, we do sharing checks, closure validation, redirection policy application, etc
            // before computing if a NI is available and if it is, whether it is valid or not.
            //
            // However, we need to know about validity of NI in the current(and earlier) stage. As a result, we will temporarily set the assembly load limit (defined as the maximum
            // load level till which recursive assembly load can execute) to be FILE_LOAD_ALLOCATE if we have been invoked to validate the NI dependencies for the first time. 
            //
            // A valid concern at this point is that we would allow to load a dependency at a load stage higher than its dependent assembly as it could crete cycles. This concern is
            // alleviated since we are doing this override (of the load stage) only for hard-bound dependencies and NGEN is responsible for ensuring that there are no cycles.
            //
            // As a result, once the dependency load returns, we will know for sure if the dependency has a valid NI or not. 
            OVERRIDE_LOAD_LEVEL_LIMIT(verifyOnly ? FILE_LOADED : FILE_LOAD_ALLOCATE);
            pDependencyAssembly = name.LoadDomainAssembly(FILE_LOADED);
        }

        PEAssembly * pDependencyFile = pDependencyAssembly->GetFile();

#endif // FEATURE_CORECLR

        ReleaseHolder<PEImage> pDependencyNativeImage = pDependencyFile->GetNativeImageWithRef();
        if (pDependencyNativeImage == NULL)
        {
            ExternalLog(LL_ERROR, W("Rejecting native image because dependency %s is not native"),
                        pDependencyFile->GetPath().GetUnicode());
            m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_DependencyNotNative;
            STRESS_LOG3(LF_ZAP,LL_INFO100,"Rejecting native file %p, because dependency %p is not NI - reason 0x%x\n",pNativeImage.GetValue(),pDependencyFile,m_dwReasonForRejectingNativeImage);
            goto NativeImageRejected;
        }

#ifndef FEATURE_FUSION // Fusion does this verification at native binding time.
        PTR_PEImageLayout pDependencyNativeLayout = pDependencyNativeImage->GetLoadedLayout();
        // Assert that the native image signature is as expected
        // Fusion will ensure this
        CORCOMPILE_VERSION_INFO * pDependencyNativeVersion =
                pDependencyNativeLayout->GetNativeVersionInfo();

        LoggablePEAssembly logAsm(pDependencyFile);
        if (!RuntimeVerifyNativeImageDependency(pDependency, pDependencyNativeVersion, &logAsm))
            goto NativeImageRejected;
#endif
    }
    LOG((LF_ZAP, LL_INFO100, "ZAP: Native image dependencies for %S OK.\n",
            pNativeImage->GetPath().GetUnicode()));

    return;
}

NativeImageRejected:
    m_pFile->ClearNativeImage();
    m_pFile->SetCannotUseNativeImage();

    CheckZapRequired();

    return;
}

BOOL DomainFile::IsZapRequired()
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

    if (!m_pFile->HasMetadata() || !g_pConfig->RequireZap(GetSimpleName()))
        return FALSE;

#if defined(_DEBUG) && defined(FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS)
    // If we're intentionally treating NIs as if they were MSIL assemblies, and the test
    // is flexible enough to accept that (e.g., complus_zaprequired=2), then zaps are not
    // required (i.e., it's ok for m_pFile->m_nativeImage to be NULL), but only if we
    // loaded an actual NI to be treated as an IL assembly
    if (PEFile::ShouldTreatNIAsMSIL())
    {
        // Since the RequireZap() call above returned true, we know that some level of
        // zap requiredness was configured
        _ASSERTE(g_pConfig->RequireZaps() != EEConfig::REQUIRE_ZAPS_NONE);

        // If config uses this special value (2), zaps are not required, so long as
        // we're using an actual NI as IL
        if ((g_pConfig->RequireZaps() == EEConfig::REQUIRE_ZAPS_ALL_JIT_OK) &&
            m_pFile->HasOpenedILimage() &&
            m_pFile->GetOpenedILimage()->HasNativeHeader())
        {
            return FALSE;
        }
    }
#endif // defined(_DEBUG) && defined(FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS)

    // Does this look like a resource-only assembly?  We assume an assembly is resource-only
    // if it contains no TypeDef (other than the <Module> TypeDef) and no MethodDef.
    // Note that pMD->GetCountWithTokenKind(mdtTypeDef) doesn't count the <Module> type.
    IMDInternalImportHolder pMD = m_pFile->GetMDImport();
    if (pMD->GetCountWithTokenKind(mdtTypeDef) == 0 && pMD->GetCountWithTokenKind(mdtMethodDef) == 0)
        return FALSE;

    DomainAssembly * pDomainAssembly = GetDomainAssembly();

    // If the manifest module does not have an ngen image, the non-manifest
    // modules cannot either
    if (m_pFile->IsModule() && !pDomainAssembly->GetFile()->CanUseNativeImage())
        m_pFile->SetCannotUseNativeImage();

    // Some cases are not supported by design. They can never have a native image.
    // So ignore such cases

    if (!m_pFile->CanUseNativeImage() &&
        g_pConfig->RequireZaps() == EEConfig::REQUIRE_ZAPS_SUPPORTED)
        return FALSE;

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    if (IsCompilationProcess())
    {
        // Ignore the assembly being ngened.

        bool fileIsBeingNGened = false;

        if (this->GetAppDomain()->IsCompilationDomain())
        {
            Assembly * assemblyBeingNGened = this->GetAppDomain()->ToCompilationDomain()->GetTargetAssembly();
            if (assemblyBeingNGened == NULL || assemblyBeingNGened == pDomainAssembly->GetCurrentAssembly())
                fileIsBeingNGened = true;
        }
        else if (IsSystem())
        {
            // mscorlib gets loaded before the CompilationDomain gets created.
            // However, we may be ngening mscorlib itself
            fileIsBeingNGened = true;
        }

        if (fileIsBeingNGened)
            return FALSE;
    }
#endif

    return TRUE;
}

void DomainFile::CheckZapRequired()
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

    if (m_pFile->HasNativeImage() || !IsZapRequired())
        return;

#ifdef FEATURE_READYTORUN
    if(m_pFile->GetLoaded()->HasReadyToRunHeader())
        return;
#endif

    // Flush any log messages
    GetFile()->FlushExternalLog();

    StackSString ss;
    ss.Printf("ZapRequire: Could not get native image for %s.\n"
              "Use FusLogVw.exe to check the reason.",
              GetSimpleName());

#if defined(_DEBUG)
    // Assert as some test may not check their error codes well. So throwing an
    // exception may not cause a test failure (as it should).
    StackScratchBuffer scratch;
    DbgAssertDialog(__FILE__, __LINE__, (char*)ss.GetUTF8(scratch));
#endif // defined(_DEBUG)

    COMPlusThrowNonLocalized(kFileNotFoundException, ss.GetUnicode());
}

// Discarding an ngen image can cause problems. For more coverage,
// this stress-mode discards ngen images even if not needed.

void DomainFile::ClearNativeImageStress()
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    static ConfigDWORD clearNativeImageStress;
    DWORD stressPercentage = clearNativeImageStress.val(CLRConfig::INTERNAL_clearNativeImageStress);
    _ASSERTE(stressPercentage <= 100);
    if (stressPercentage == 0 || !GetFile()->HasNativeImage())
        return;

    // Note that discarding a native image can affect dependencies. So its not enough
    // to only check DomainFile::IsZapRequired() here.
    if (g_pConfig->RequireZaps() != EEConfig::REQUIRE_ZAPS_NONE)
        return;

    // Its OK to ClearNativeImage even for a shared assembly, as the current PEFile will
    // be discarded if we decide to share the assembly. However, we always use the same
    // PEFile for the system assembly. So discarding the native image in the current
    // AppDomain will actually affect the system assembly in the shared domain, and other
    // appdomains may have already committed to using its ngen image.
    if (GetFile()->IsSystem() && !this->GetAppDomain()->IsDefaultDomain())
        return;

    if (g_IBCLogger.InstrEnabled())
        return;

    ULONG hash = HashStringA(GetSimpleName());

    // Hash in the FileLoadLevel so that we make a different decision for every level.
    FileLoadLevel fileLoadLevel = m_pDomain->GetDomainFileLoadLevel(this);
    hash ^= ULONG(fileLoadLevel);
    // We do not discard native images after this level
    _ASSERTE(fileLoadLevel < FILE_LOAD_VERIFY_NATIVE_IMAGE_DEPENDENCIES);

    // Different app-domains should make different decisions
    hash ^= HashString(this->GetAppDomain()->GetFriendlyName());

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    // Since DbgRandomOnHashAndExe() is not so random under ngen.exe, also
    // factor in the module being compiled
    if (this->GetAppDomain()->IsCompilationDomain())
    {
        Module * module = this->GetAppDomain()->ToCompilationDomain()->GetTargetModule();
        // Has the target module been set yet?
        if (module)
            hash ^= HashStringA(module->GetSimpleName());
    }
#endif

    if (DbgRandomOnHashAndExe(hash, float(stressPercentage)/100))
    {
        GetFile()->SetCannotUseNativeImage();
        GetFile()->ClearNativeImage();
        ExternalLog(LL_ERROR, "Rejecting native image for **clearNativeImageStress**");
    }
#endif
}

#endif // FEATURE_PREJIT

void DomainFile::PreLoadLibrary()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    // Check skip verification for loading if required
    if (!GetFile()->CanLoadLibrary())
    {
        DomainAssembly* pDomainAssembly = GetDomainAssembly();
        if (pDomainAssembly->GetSecurityDescriptor()->IsResolved())
        {
            if (Security::CanSkipVerification(pDomainAssembly))
                GetFile()->SetSkipVerification();
        }
        else
        {
            AppDomain *pAppDomain = this->GetAppDomain();
            PEFile *pFile = GetFile();
            _ASSERTE(pFile != NULL);
            PEImage *pImage = pFile->GetILimage();
            _ASSERTE(pImage != NULL);
            _ASSERTE(!pImage->IsFile());
            if (pImage->HasV1Metadata())
            {
                // In V1 case, try to derive SkipVerification status from parents
                do
                {
                    PEAssembly * pAssembly = pFile->GetAssembly();
                    if (pAssembly == NULL)
                        break;
                    pFile = pAssembly->GetCreator();
                    if (pFile != NULL)
                    {
                        pAssembly = pFile->GetAssembly();
                        // Find matching DomainAssembly for the given PEAsssembly
                        // Perf: This does not scale
                        AssemblyIterationFlags flags =
                            (AssemblyIterationFlags) (kIncludeLoaded | kIncludeLoading | kIncludeExecution);
                        AppDomain::AssemblyIterator i = pAppDomain->IterateAssembliesEx(flags);
                        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

                        while (i.Next(pDomainAssembly.This()))
                        {
                            if ((pDomainAssembly != NULL) && (pDomainAssembly->GetFile() == pAssembly))
                            {
                                break;
                            }
                        }
                        if (pDomainAssembly != NULL)
                        {
                            if (pDomainAssembly->GetSecurityDescriptor()->IsResolved())
                            {
                                if (Security::CanSkipVerification(pDomainAssembly))
                                {
                                    GetFile()->SetSkipVerification();
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Potential Bug: Unable to find DomainAssembly for given PEAssembly
                            // In retail build gracefully exit loop
                            _ASSERTE(pDomainAssembly != NULL);
                            break;
                        }
                    }
                }
                while (pFile != NULL);
            }
        }
    }
} // DomainFile::PreLoadLibrary

// Note that this is the sole loading function which must be called OUTSIDE THE LOCK, since
// it will potentially involve the OS loader lock.
void DomainFile::LoadLibrary()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    Thread::LoadingFileHolder holder(GetThread());
    GetThread()->SetLoadingFile(this);
    GetFile()->LoadLibrary();
}

void DomainFile::PostLoadLibrary()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        // Note that GetFile()->LoadLibrary must be called before this OUTSIDE OF THE LOCKS
        PRECONDITION(GetFile()->CheckLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT
    if (GetFile()->HasNativeImage())
    {
        InsertIntoDomainFileWithNativeImageList();
    }
#endif
#ifdef PROFILING_SUPPORTED
    // After this point, it is possible to load types.
    // We need to notify the profiler now because the profiler may need to inject methods into
    // the module, and to do so reliably, it must have the chance to do so before
    // any types are loaded from the module.
    //
    // In the past we only allowed injecting types/methods on non-NGEN images so notifying here
    // worked ok, but for NGEN images this is pretty ugly. Rejitting often occurs in this callback,
    // but then during fixup the results of LoadedMethodDesc iterator would change and we would
    // need to re-iterate everything. Aside from Rejit other code often wasn't designed to handle
    // running before Fixup. A concrete example VS recently hit, calling GetClassLayout using
    // a MethodTable which doesn't need restore but its parent pointer isn't fixed up yet.
    // We've already set the rules so that profilers can't modify the member list of types in NGEN images
    // so it doesn't matter if types are pre-loaded. We only need the guarantee that code for the
    // loaded types won't execute yet. For NGEN images we deliver the load notification in
    // FILE_LOAD_DELIVER_EVENTS.
    if (!GetFile()->HasNativeImage())
    {
        if (!IsProfilerNotified())
        {
            SetProfilerNotified();
            GetCurrentModule()->NotifyProfilerLoadFinished(S_OK);
        }
    }

#endif
}

void DomainFile::AddDependencies()
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_PREJIT

#ifdef FEATURE_CORECLR // hardbinding
    //
    // CoreCLR hard binds to mscorlib.dll only. No need to track hardbound dependencies.
    //
#else
    // Add hard bindings as unconditional dependencies
    if (GetFile()->HasNativeImage() && GetCurrentModule()->HasNativeImage() && IsAssembly())
    {
        PEImage *pNativeImage = GetFile()->GetPersistentNativeImage();
        PEImageLayout *pNativeLayout = pNativeImage->GetLoadedLayout();

        COUNT_T cDependencies;
        CORCOMPILE_DEPENDENCY *pDependencies = pNativeLayout->GetNativeDependencies(&cDependencies);
        CORCOMPILE_DEPENDENCY *pDependenciesEnd = pDependencies + cDependencies;

        while (pDependencies < pDependenciesEnd)
        {
            if (pDependencies->signNativeImage != INVALID_NGEN_SIGNATURE)
            {

                //
                // Load the manifest file for the given name assembly spec.
                //

                AssemblySpec name;
                name.InitializeSpec(pDependencies->dwAssemblyRef,
                                    pNativeImage->GetNativeMDImport(),
                                    GetDomainAssembly());

                DomainAssembly *pDependency = name.LoadDomainAssembly(FILE_LOADED);

                // Right now we only support hard binding to other manifest modules so we don't
                // need to consider the other module cases
                Module *pModule = pDependency->GetModule();

                // Add hard binding as an unconditional active dependency
                STRESS_LOG4(LF_CODESHARING,LL_INFO100,"unconditional dependency %p %p %i %i\n",
                    GetFile(),GetCurrentModule(),GetFile()->HasNativeImage(),GetCurrentModule()->HasNativeImage());
                if(!pModule->IsSystem())
                    GetCurrentModule()->AddActiveDependency(pModule, TRUE);
            }

            pDependencies++;
        }
    }
#endif // FEATURE_CORECLR

#endif // FEATURE_PREJIT
}

void DomainFile::EagerFixups()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
    if (IsIntrospectionOnly())
        return; 
    
    if (GetCurrentModule()->HasNativeImage())
    {
        GetCurrentModule()->RunEagerFixups();
    }
#ifdef FEATURE_READYTORUN
    else
    if (GetCurrentModule()->IsReadyToRun())
    {
#ifndef CROSSGEN_COMPILE
        GetCurrentModule()->RunEagerFixups();
#endif

        PEImageLayout * pLayout = GetCurrentModule()->GetReadyToRunInfo()->GetImage();

        TADDR base = dac_cast<TADDR>(pLayout->GetBase());

        ExecutionManager::AddCodeRange(base, base + (TADDR)pLayout->GetVirtualSize(),
                                        ExecutionManager::GetReadyToRunJitManager(),
                                         RangeSection::RANGE_SECTION_READYTORUN,
                                         GetCurrentModule() /* (void *)pLayout */);
    }
#endif // FEATURE_READYTORUN

#endif // FEATURE_PREJIT
}

void DomainFile::VtableFixups()
{
    WRAPPER_NO_CONTRACT;

#if defined(FEATURE_MIXEDMODE) && !defined(CROSSGEN_COMPILE)
    if (!GetCurrentModule()->IsResource())
        GetCurrentModule()->FixupVTables();
#endif
}

void DomainFile::FinishLoad()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

#ifdef FEATURE_PREJIT

    if (m_pFile->HasNativeImage())
    {
#ifdef FEATURE_FUSION
        // <REVISIT_TODO>Because of bug 112034, we may commit to a native image even though
        // we should not have.</REVISIT_TODO>

// #ifdef _DEBUG

        // Verify that the native image dependencies are still valid
        // Since we had already committed to using a native image, they cannot
        // be invalidated
        VerifyNativeImageDependencies(true);
        _ASSERTE(m_pFile->HasNativeImage());

        if (!m_pFile->HasNativeImage())
        {
            STRESS_LOG1(LF_CODESHARING, LL_FATALERROR, "Incorrectly committed to using native image for %S",
                                                       m_pFile->GetPath().GetUnicode());
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
// #endif

#endif // FEATURE_FUSION

        LOG((LF_ZAP, LL_INFO10, "Using native image %S.\n", m_pFile->GetPersistentNativeImage()->GetPath().GetUnicode()));
        ExternalLog(LL_INFO10, "Native image successfully used.");

        // Inform metadata that it has been loaded from a native image
        // (and so there was an opportunity to check for or fix inconsistencies in the original IL metadata)
        m_pFile->GetMDImport()->SetVerifiedByTrustedSource(TRUE);
    }

    // Are we absolutely required to use a native image?
    CheckZapRequired();

#if defined(FEATURE_CORECLR) && defined(FEATURE_COMINTEROP)
    // If this is a winmd file, ensure that the ngen reference namespace is loadable.
    // This is necessary as on the phone we don't check ngen image dependencies, and thus we can get in a situation 
    // where a winmd is loaded as a dependency of an ngen image, but the type used to build cross module references 
    // in winmd files isn't loaded.
    if (GetFile()->AsAssembly()->IsWindowsRuntime() && GetFile()->HasHostAssembly())
    {
        IMDInternalImport *pImport = GetFile()->GetPersistentMDImport();
        LPCSTR  szNameSpace;
        LPCSTR  szTypeName;
        // It does not make sense to pass the file name to recieve fake type name for empty WinMDs, because we would use the name 
        // for binding in next call to BindAssemblySpec which would fail for fake WinRT type name
        // We will throw/return the error instead and the caller will recognize it and react to it by not creating the ngen image - 
        // see code:Zapper::ComputeDependenciesInCurrentDomain
        if (SUCCEEDED(::GetFirstWinRTTypeDef(pImport, &szNameSpace, &szTypeName, NULL, NULL)))
        {
            // Build assembly spec to describe binding to that WinRT type.
            AssemblySpec spec;
            IfFailThrow(spec.Init("WindowsRuntimeAssemblyName, ContentType=WindowsRuntime"));
            spec.SetWindowsRuntimeType(szNameSpace, szTypeName);

            // Bind to assembly using the CLRPriv binder infrastructure. (All WinRT loads are done through CLRPriv binders
            ReleaseHolder<IAssemblyName> pAssemblyName;
            IfFailThrow(spec.CreateFusionName(&pAssemblyName, FALSE, TRUE));
            ReleaseHolder<ICLRPrivAssembly> pPrivAssembly;
            IfFailThrow(GetFile()->GetHostAssembly()->BindAssemblyByName(pAssemblyName, &pPrivAssembly));

            // Verify that we found this. If this invariant doesn't hold, then the ngen images that reference this winmd are be invalid.
            // ALSO, this winmd file is invalid as it doesn't follow spec about how it is distributed.
            if (GetAppDomain()->FindAssembly(pPrivAssembly) != this)
            {
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }
        }
    }
#endif // defined(FEATURE_CORECLR) && defined(FEATURE_COMINTEROP)
#endif // FEATURE_PREJIT

    // Flush any log messages
#ifdef FEATURE_PREJIT
    GetFile()->FlushExternalLog();
#endif
    // Must set this a bit prematurely for the DAC stuff to work
    m_level = FILE_LOADED;

    // Now the DAC can find this module by enumerating assemblies in a domain.
    DACNotify::DoModuleLoadNotification(m_pModule);

#if defined(DEBUGGING_SUPPORTED) && !defined(DACCESS_COMPILE)
    if (IsDebuggerNotified() && (g_pDebugInterface != NULL))
    {
        // We already notified dbgapi that this module was loading (via LoadModule()).
        // Now let the dbgapi know the module has reached FILE_LOADED, so it can do any
        // processing that needs to wait until this stage (e.g., binding breakpoints in
        // NGENd generics).
        g_pDebugInterface->LoadModuleFinished(m_pModule, m_pDomain);
    }
#endif // defined(DEBUGGING_SUPPORTED) && !defined(DACCESS_COMPILE)

    // Set a bit to indicate that the module has been loaded in some domain, and therefore
    // typeloads can involve types from this module. (Used for candidate instantiations.)
    GetModule()->SetIsReadyForTypeLoad();

#ifdef FEATURE_PERFMAP
    // Notify the perfmap of the IL image load.
    PerfMap::LogImageLoad(m_pFile);
#endif
}

void DomainFile::VerifyExecution()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(IsLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    if (GetModule()->IsIntrospectionOnly())
    {
        // Throw an exception
        COMPlusThrow(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY);
    }

    if (GetModule()->GetAssembly()->IsSIMDVectorAssembly() && 
        !GetModule()->GetAssembly()->GetSecurityDescriptor()->IsFullyTrusted())
    {
        COMPlusThrow(kFileLoadException, IDS_EE_SIMD_PARTIAL_TRUST_DISALLOWED);
    }

    if(GetFile()->PassiveDomainOnly())
    {
    // Remove path - location must be hidden for security purposes
        LPCWSTR path=GetFile()->GetPath();
        LPCWSTR pStart = wcsrchr(path, '\\');
        if (pStart != NULL)
            pStart++;
        else
            pStart = path;
        COMPlusThrow(kInvalidOperationException, IDS_EE_CODEEXECUTION_ASSEMBLY_FOR_PASSIVE_DOMAIN_ONLY,pStart);
    }

    RETURN;
}

void DomainFile::Activate()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(IsLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    // If we are a module, ensure we've activated the assembly first.

    if (!IsAssembly())
    {
        GetDomainAssembly()->EnsureActive();
    }
    else
    {
        // We cannot execute any code in this assembly until we know what exception plan it is on.
        // At the point of an exception's stack-crawl it is too late because we cannot tolerate a GC.
        // See PossiblyUnwrapThrowable and its callers.
        _ASSERTE(GetLoadedModule() == GetDomainAssembly()->GetLoadedAssembly()->GetManifestModule());
        GetLoadedModule()->IsRuntimeWrapExceptions();
    }

    // Now activate any dependencies.
    // This will typically cause reentrancy of course.

    if (!IsSingleAppDomain())
    {
        // increment the counter (see the comment in Module::AddActiveDependency)
        GetModule()->IncrementNumberOfActivations();

#ifdef FEATURE_LOADER_OPTIMIZATION
        AppDomain *pDomain = this->GetAppDomain();
        Module::DependencyIterator i = GetCurrentModule()->IterateActiveDependencies();
        STRESS_LOG2(LF_LOADER, LL_INFO100,"Activating module %p in AD %i",GetCurrentModule(),pDomain->GetId().m_dwId);

        while (i.Next())
        {
            Module *pModule = i.GetDependency();
            DomainFile *pDomainFile = pModule->FindDomainFile(pDomain);
            if (pDomainFile == NULL)
                pDomainFile = pDomain->LoadDomainNeutralModuleDependency(pModule, FILE_LOADED);

            STRESS_LOG3(LF_LOADER, LL_INFO100,"Activating dependency %p -> %p, unconditional=%i",GetCurrentModule(),pModule,i.IsUnconditional());

            if (i.IsUnconditional())
            {
                // Let any failures propagate
                pDomainFile->EnsureActive();
            }
            else
            {
                // Enable triggers if we fail here
                if (!pDomainFile->TryEnsureActive())
                    GetCurrentModule()->EnableModuleFailureTriggers(pModule, this->GetAppDomain());
            }
            STRESS_LOG3(LF_LOADER, LL_INFO100,"Activated dependency %p -> %p, unconditional=%i",GetCurrentModule(),pModule,i.IsUnconditional());
        }
#endif
    }

#ifndef CROSSGEN_COMPILE
    if (m_pModule->CanExecuteCode())
    {
        //
        // Now call the module constructor.  Note that this might cause reentrancy;
        // this is fine and will be handled by the class cctor mechanism.
        //

        MethodTable *pMT = m_pModule->GetGlobalMethodTable();
        if (pMT != NULL)
        {
            pMT->CheckRestore();
            m_bDisableActivationCheck=TRUE;
            pMT->CheckRunClassInitThrowing();
        }
#ifdef FEATURE_CORECLR
        if (g_pConfig->VerifyModulesOnLoad())
        {
            m_pModule->VerifyAllMethods();
        }
#endif //FEATURE_CORECLR
#ifdef _DEBUG
        if (g_pConfig->ExpandModulesOnLoad())
        {
            m_pModule->ExpandAll();
        }
#endif //_DEBUG
    }
    else
    {
        // This exception does not need to be localized as it can only happen in
        // NGen and PEVerify, and we are not localizing those tools.
        _ASSERTE(this->GetAppDomain()->IsPassiveDomain());
        // This assert will fire if we attempt to run non-mscorlib code from within ngen
        // Current audits of the system indicate that this will never occur, but if it does
        // the exception below will prevent actual non-mscorlib code execution.
        _ASSERTE(!this->GetAppDomain()->IsCompilationDomain());

        LPCWSTR message = W("You may be trying to evaluate a permission from an assembly ")
                          W("without FullTrust, or which cannot execute code for other reasons.");
        COMPlusThrowNonLocalized(kFileLoadException, message);
    }
#endif // CROSSGEN_COMPILE

    RETURN;
}

#ifdef FEATURE_LOADER_OPTIMIZATION
BOOL DomainFile::PropagateActivationInAppDomain(Module *pModuleFrom, Module *pModuleTo, AppDomain* pDomain)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pModuleFrom));
        PRECONDITION(CheckPointer(pModuleTo));
        THROWS; // should only throw transient failures
        DISABLED(GC_TRIGGERS);
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_MULTICOREJIT
    // Reset the flag to allow managed code to be called in multicore JIT background thread from this routine
    ThreadStateNCStackHolder holder(-1, Thread::TSNC_CallingManagedCodeDisabled);
#endif

    BOOL completed=true;
    EX_TRY
    {
        GCX_COOP();

        ENTER_DOMAIN_PTR(pDomain,ADV_ITERATOR); //iterator
        DomainFile *pDomainFileFrom = pModuleFrom->FindDomainFile(pDomain);
        if (pDomainFileFrom != NULL && pDomain->IsLoading(pDomainFileFrom, FILE_ACTIVE))
        {
            STRESS_LOG3(LF_LOADER, LL_INFO100,"Found DomainFile %p for module %p in AppDomain %i\n",pDomainFileFrom,pModuleFrom,pDomain->GetId().m_dwId);
            DomainFile *pDomainFileTo = pModuleTo->FindDomainFile(pDomain);
            if (pDomainFileTo == NULL)
                pDomainFileTo = pDomain->LoadDomainNeutralModuleDependency(pModuleTo, FILE_LOADED);

            if (!pDomainFileTo->TryEnsureActive())
                pModuleFrom->EnableModuleFailureTriggers(pModuleTo, pDomain);
            else if (!pDomainFileTo->IsActive())
            {
                // We are in a reentrant case
                completed = FALSE;
            }
        }
        END_DOMAIN_TRANSITION;
    }
    EX_CATCH
    {
          if (!IsExceptionOfType(kAppDomainUnloadedException, GET_EXCEPTION()))
            EX_RETHROW;
    }
    EX_END_CATCH(SwallowAllExceptions)
    return completed;
}
#endif

// Returns TRUE if activation is completed for all app domains
// static
BOOL DomainFile::PropagateNewActivation(Module *pModuleFrom, Module *pModuleTo)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pModuleFrom));
        PRECONDITION(CheckPointer(pModuleTo));
        THROWS; // should only throw transient failures
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL completed = TRUE;
#ifdef FEATURE_LOADER_OPTIMIZATION
    if (pModuleFrom->GetAssembly()->IsDomainNeutral())
    {
        AppDomainIterator ai(TRUE);
        Thread *pThread = GetThread();

        while (ai.Next())
        {
            STRESS_LOG3(LF_LOADER, LL_INFO100,"Attempting to propagate domain-neutral conditional module dependency %p -> %p to AppDomain %i\n",pModuleFrom,pModuleTo,ai.GetDomain()->GetId().m_dwId);
            // This is to minimize the chances of trying to run code in an appdomain that's shutting down.
            if (ai.GetDomain()->CanThreadEnter(pThread))
            {
                completed &= PropagateActivationInAppDomain(pModuleFrom,pModuleTo,ai.GetDomain());
            }
        }
    }
    else
#endif
    {
        AppDomain *pDomain = pModuleFrom->GetDomain()->AsAppDomain();
        DomainFile *pDomainFileFrom = pModuleFrom->GetDomainFile(pDomain);
        if (pDomain->IsLoading(pDomainFileFrom, FILE_ACTIVE))
        {
            // The dependency should already be loaded
            DomainFile *pDomainFileTo = pModuleTo->GetDomainFile(pDomain);
            if (!pDomainFileTo->TryEnsureActive())
                pModuleFrom->EnableModuleFailureTriggers(pModuleTo, pDomain);
            else if (!pDomainFileTo->IsActive())
            {
                // Reentrant case
                completed = FALSE;
            }
        }
    }

    return completed;
}

// Checks that module has not been activated in any domain
CHECK DomainFile::CheckUnactivatedInAllDomains(Module *pModule)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pModule));
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pModule->GetAssembly()->IsDomainNeutral())
    {
        AppDomainIterator ai(TRUE);

        while (ai.Next())
        {
            AppDomain *pDomain = ai.GetDomain();
            DomainFile *pDomainFile = pModule->FindDomainFile(pDomain);
            if (pDomainFile != NULL)
                CHECK(!pDomainFile->IsActive());
        }
    }
    else
    {
        DomainFile *pDomainFile = pModule->FindDomainFile(pModule->GetDomain()->AsAppDomain());
        if (pDomainFile != NULL)
            CHECK(!pDomainFile->IsActive());
    }

    CHECK_OK;
}

#ifdef FEATURE_PREJIT
DomainFile *DomainFile::FindNextDomainFileWithNativeImage()
{
    LIMITED_METHOD_CONTRACT;
    return m_pNextDomainFileWithNativeImage;
}

void DomainFile::InsertIntoDomainFileWithNativeImageList()
{
    LIMITED_METHOD_CONTRACT;

    while (true)
    {
        DomainFile *pLastDomainFileFoundWithNativeImage = m_pDomain->m_pDomainFileWithNativeImageList;
        m_pNextDomainFileWithNativeImage = pLastDomainFileFoundWithNativeImage;
        if (pLastDomainFileFoundWithNativeImage == InterlockedCompareExchangeT(&m_pDomain->m_pDomainFileWithNativeImageList, this, pLastDomainFileFoundWithNativeImage))
            break;
    }
}
#endif

//--------------------------------------------------------------------------------
// DomainAssembly
//--------------------------------------------------------------------------------

DomainAssembly::DomainAssembly(AppDomain *pDomain, PEFile *pFile, AssemblyLoadSecurity *pLoadSecurity, LoaderAllocator *pLoaderAllocator)
  : DomainFile(pDomain, pFile),
    m_pAssembly(NULL),
    m_debuggerFlags(DACF_NONE),
#ifdef FEATURE_FUSION
    m_pAssemblyBindingClosure(NULL),
#endif
    m_MissingDependenciesCheckStatus(CMD_Unknown),
    m_fSkipPolicyResolution(pLoadSecurity != NULL && !pLoadSecurity->ShouldResolvePolicy()),
    m_fDebuggerUnloadStarted(FALSE),
    m_fCollectible(pLoaderAllocator->IsCollectible()),
    m_fHostAssemblyPublished(false),
    m_fCalculatedShouldLoadDomainNeutral(false),
    m_fShouldLoadDomainNeutral(false)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    pFile->ValidateForExecution();

#ifndef CROSSGEN_COMPILE
    if (m_fCollectible)
    {
        ((AssemblyLoaderAllocator *)pLoaderAllocator)->SetDomainAssembly(this);
    }
#endif

    // !!! backout

    m_hExposedAssemblyObject = NULL;

    NewHolder<IAssemblySecurityDescriptor> pSecurityDescriptorHolder(Security::CreateAssemblySecurityDescriptor(pDomain, this, pLoaderAllocator));

    if (pLoadSecurity != NULL)
    {
#ifdef FEATURE_CAS_POLICY
        // If this assembly had a file name specified, we aren't allowed to load from remote sources and we
        // aren't in CAS policy mode (which sandboxes remote assemblies automatically), then we need to do a
        // check on this assembly's zone of origin when creating it.
        if (pLoadSecurity->m_fCheckLoadFromRemoteSource &&
            !pLoadSecurity->m_fSuppressSecurityChecks &&
            !m_pDomain->GetSecurityDescriptor()->AllowsLoadsFromRemoteSources() &&
            !pFile->IsIntrospectionOnly())
        {
            SString strCodeBase;
            BYTE pbUniqueID[MAX_SIZE_SECURITY_ID];
            DWORD cbUniqueID = COUNTOF(pbUniqueID);
            SecZone dwZone = NoZone;

            GetSecurityIdentity(strCodeBase,
                                &dwZone,
                                0,
                                pbUniqueID,
                                &cbUniqueID);

            // Since loads from remote sources are not enabled for this assembly, we only want to allow the
            // load if any of the following conditions apply:
            //
            //  * The load is coming off the local machine
            //  * The load is coming from the intranet or a trusted site, and the code base is UNC.  (ie,
            //    don't allow HTTP loads off the local intranet

            bool safeLoad = false;
            if (dwZone == LocalMachine)
            {
                safeLoad = true;
            }
            else if (dwZone == Intranet || dwZone == Trusted)
            {
                if (UrlIsFileUrl(strCodeBase.GetUnicode()))
                {
                    safeLoad = true;
                }
                else if (PathIsUNC(strCodeBase.GetUnicode()))
                {
                    safeLoad = true;
                }
            }

            if (!safeLoad)
            {
                // We've tried to load an assembly from a location where it would have been sandboxed in legacy
                // CAS situations, but the application hasn't indicated that this is a safe thing to do. In
                // order to prevent accidental security holes by silently loading assemblies in full trust that
                // an application expected to be sandboxed, we'll throw an exception instead.
                //
                // Since this exception can commonly occur with if the file is physically located on the
                // hard drive, but has the mark of the web on it we'll also try to detect this mark and
                // provide a customized error message if we find it.   We do that by re-evaluating the
                // assembly's zone with the NOSAVEDFILECHECK flag, which ignores the mark of the web, and if
                // that comes back as MyComputer we flag the assembly as having the mark of the web on it.
                SecZone dwNoMotwZone = NoZone;
                GetSecurityIdentity(strCodeBase, &dwNoMotwZone, MUTZ_NOSAVEDFILECHECK, pbUniqueID, &cbUniqueID);

                if (dwNoMotwZone == LocalMachine)
                {
                    COMPlusThrow(kNotSupportedException, IDS_E_LOADFROM_REMOTE_SOURCE_MOTW);
                }
                else
                {
                    COMPlusThrow(kNotSupportedException, IDS_E_LOADFROM_REMOTE_SOURCE);
                }
            }
        }
#endif // FEATURE_CAS_POLICY

        if (GetFile()->IsSourceGAC())
        {
            // Assemblies in the GAC are not allowed to
            // specify additional evidence.  They must always follow default machine policy rules.

            // So, we just ignore the evidence. (Ideally we would throw an error, but it would introduce app
            // compat issues.)
        }
        else
        {
#ifdef FEATURE_FUSION
            // We do not support sharing behavior of ALWAYS when using evidence to load assemblies
            if (pDomain->GetSharePolicy() == AppDomain::SHARE_POLICY_ALWAYS
                && ShouldLoadDomainNeutral())
            {
                // Just because we have information about the loaded assembly's security doesn't mean that
                // we're trying to override evidence, make sure we're not just trying to push a grant set
                if (((pLoadSecurity->m_pEvidence != NULL) && (*pLoadSecurity->m_pEvidence != NULL)) ||
                    ((pLoadSecurity->m_pAdditionalEvidence != NULL) && (*pLoadSecurity->m_pAdditionalEvidence != NULL)))
                {
                    // We may not be able to reduce sharing policy at this point, if we have already loaded
                    // some non-GAC assemblies as domain neutral.  For this case we must regrettably fail
                    // the whole operation.
                    if (!pDomain->ReduceSharePolicyFromAlways())
                    {
                        ThrowHR(COR_E_CANNOT_SPECIFY_EVIDENCE);
                    }
                }
            }
#endif
            {
                GCX_COOP();

#ifdef FEATURE_CAS_POLICY
                if (pLoadSecurity->m_pAdditionalEvidence != NULL)
                {
                    if(*pLoadSecurity->m_pAdditionalEvidence != NULL)
                    {
                        pSecurityDescriptorHolder->SetAdditionalEvidence(*pLoadSecurity->m_pAdditionalEvidence);
                    }
                }
                else if (pLoadSecurity->m_pEvidence != NULL)
                {
                    if (*pLoadSecurity->m_pEvidence != NULL)
                    {
                        pSecurityDescriptorHolder->SetEvidence(*pLoadSecurity->m_pEvidence);
                    }
                }
#endif // FEATURE_CAS_POLICY

                // If the assembly being loaded already knows its grant set (for instnace, it's being pushed
                // from the loading assembly), then we can set that up now as well
                if (!pLoadSecurity->ShouldResolvePolicy())
                {
                    _ASSERTE(pLoadSecurity->m_pGrantSet != NULL);

#ifdef FEATURE_CAS_POLICY
                    // The permissions from an anonymously hosted dynamic method are fulltrust/transparent,
                    // so ensure we have full trust to pass that on to the new assembly
                    if(pLoadSecurity->m_fPropagatingAnonymouslyHostedDynamicMethodGrant &&
                       !CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_Security_DisableAnonymouslyHostedDynamicMethodCreatorSecurityCheck))
                    {
                        Security::SpecialDemand(SSWT_LATEBOUND_LINKDEMAND, SECURITY_FULL_TRUST);
                    }
#endif // FEATURE_CAS_POLICY

                    pSecurityDescriptorHolder->PropagatePermissionSet(
                                                      *pLoadSecurity->m_pGrantSet,
                                                      pLoadSecurity->m_pRefusedSet == NULL ? NULL : *pLoadSecurity->m_pRefusedSet,
                                                      pLoadSecurity->m_dwSpecialFlags);
                }
            }
        }
    }

    SetupDebuggingConfig();

    // Add a Module iterator entry for this assembly.
    IfFailThrow(m_Modules.Append(this));

    m_pSecurityDescriptor = pSecurityDescriptorHolder.Extract();
}

DomainAssembly::~DomainAssembly()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_fHostAssemblyPublished)
    {
        // Remove association first.
        GetAppDomain()->UnPublishHostedAssembly(this);
    }

    ModuleIterator i = IterateModules(kModIterIncludeLoading);
    while (i.Next())
    {
        if (i.GetDomainFile() != this)
            delete i.GetDomainFile();
    }

    if (m_pAssembly != NULL && !m_pAssembly->IsDomainNeutral())
    {
        delete m_pAssembly;
    }

    delete m_pSecurityDescriptor;
}

void DomainAssembly::ReleaseFiles()
{
    STANDARD_VM_CONTRACT;

    if(m_pAssembly)
        m_pAssembly->StartUnload();
#ifdef FEATURE_FUSION
    // release the old closure from the holder
    m_pAssemblyBindingClosure=NULL;
#endif
    ModuleIterator i = IterateModules(kModIterIncludeLoading);
    while (i.Next())
    {
        if (i.GetDomainFile() != this)
             i.GetDomainFile()->ReleaseFiles();
    }

    DomainFile::ReleaseFiles();
}

void DomainAssembly::SetAssembly(Assembly* pAssembly)
{
    STANDARD_VM_CONTRACT;

    UpdatePEFile(pAssembly->GetManifestFile());
    _ASSERTE(pAssembly->GetManifestModule()->GetFile()==m_pFile);
    m_pAssembly = pAssembly;
    m_pModule = pAssembly->GetManifestModule();

    pAssembly->SetDomainAssembly(this);
}

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
void DomainAssembly::AddModule(DomainModule *pModule)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    DWORD index = RidFromToken(pModule->GetToken());

    while (index >= m_Modules.GetCount())
        IfFailThrow(m_Modules.Append(NULL));

    m_Modules.Set(index, pModule);
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

#ifndef CROSSGEN_COMPILE
//---------------------------------------------------------------------------------------
//
// Returns managed representation of the assembly (Assembly or AssemblyBuilder).
// Returns NULL if the managed scout was already collected (see code:LoaderAllocator#AssemblyPhases).
//
OBJECTREF DomainAssembly::GetExposedAssemblyObject()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    LoaderAllocator * pLoaderAllocator = GetLoaderAllocator();

    if (!pLoaderAllocator->IsManagedScoutAlive())
    {   // We already collected the managed scout, so we cannot re-create any managed objects
        // Note: This is an optimization, as the managed scout can be collected right after this check
        return NULL;
    }

    if (m_hExposedAssemblyObject == NULL)
    {
        // Atomically create a handle

        LOADERHANDLE handle = pLoaderAllocator->AllocateHandle(NULL);

        FastInterlockCompareExchangePointer(&m_hExposedAssemblyObject, handle, static_cast<LOADERHANDLE>(NULL));
    }

    if (pLoaderAllocator->GetHandleValue(m_hExposedAssemblyObject) == NULL)
    {
        ASSEMBLYREF   assemblyObj = NULL;
        MethodTable * pMT;
        if (GetFile()->IsDynamic())
        {
            // This is unnecessary because the managed InternalAssemblyBuilder object
            // should have already been created at the time of DefineDynamicAssembly
            OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);
            pMT = MscorlibBinder::GetClass(CLASS__INTERNAL_ASSEMBLY_BUILDER);
        }
        else
        {
            OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);
            pMT = MscorlibBinder::GetClass(CLASS__ASSEMBLY);
        }

        // Will be TRUE only if LoaderAllocator managed object was already collected and therefore we should
        // return NULL
        BOOL fIsLoaderAllocatorCollected = FALSE;

        // Create the assembly object
        GCPROTECT_BEGIN(assemblyObj);
        assemblyObj = (ASSEMBLYREF)AllocateObject(pMT);

        assemblyObj->SetAssembly(this);

        // Attach the reference to the assembly to keep the LoaderAllocator for this collectible type
        // alive as long as a reference to the assembly is kept alive.
        // Currently we overload the sync root field of the assembly to do so, but the overload is not necessary.
        if (GetAssembly() != NULL)
        {
            OBJECTREF refLA = GetAssembly()->GetLoaderAllocator()->GetExposedObject();
            if ((refLA == NULL) && GetAssembly()->GetLoaderAllocator()->IsCollectible())
            {   // The managed LoaderAllocator object was collected
                fIsLoaderAllocatorCollected = TRUE;
            }
            assemblyObj->SetSyncRoot(refLA);
        }

        if (!fIsLoaderAllocatorCollected)
        {   // We should not expose this value in case the LoaderAllocator managed object was already
            // collected
            pLoaderAllocator->CompareExchangeValueInHandle(m_hExposedAssemblyObject, (OBJECTREF)assemblyObj, NULL);
        }
        GCPROTECT_END();

        if (fIsLoaderAllocatorCollected)
        {   // The LoaderAllocator managed object was already collected, we cannot re-create it
            // Note: We did not publish the allocated Assembly/AssmeblyBuilder object, it will get collected
            // by GC
            return NULL;
        }
    }

    return pLoaderAllocator->GetHandleValue(m_hExposedAssemblyObject);
} // DomainAssembly::GetExposedAssemblyObject
#endif // CROSSGEN_COMPILE

#ifdef FEATURE_LOADER_OPTIMIZATION

#ifdef FEATURE_FUSION
// This inner method exists to avoid EX_TRY calling _alloca repeatedly in the for loop below.
DomainAssembly::CMDI_Result DomainAssembly::CheckMissingDependencyInner(IAssemblyBindingClosure* pClosure, DWORD idx)
{
    CONTRACTL {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    SafeComHolder<IAssemblyName>  pAssemblyName;
    HRESULT hrBindFailure = S_OK;
    HRESULT hr = pClosure->GetNextFailureAssembly(idx, &pAssemblyName, &hrBindFailure);
    if (hr == HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS))
    {
        return CMDI_End;
    }

    IfFailThrow(hr);

    CMDI_Result ret = CMDI_AssemblyResolveFailed;
    AssemblySpec spec;
    PEAssemblyHolder result;

    EX_TRY
    {
        spec.InitializeSpec(pAssemblyName, this, FALSE);
        result = this->GetAppDomain()->TryResolveAssembly(&spec,FALSE);

        if (result && result->CanUseWithBindingCache())
        {
            this->GetAppDomain()->AddFileToCache(&spec, result);
            ret = CMDI_AssemblyResolveSucceeded;
        }
        else
        {
            _ASSERTE(FAILED(hrBindFailure));

            StackSString name;
            spec.GetFileOrDisplayName(0, name);
            NewHolder<EEFileLoadException> pEx(new EEFileLoadException(name, hrBindFailure));
            this->GetAppDomain()->AddExceptionToCache(&spec, pEx);
        }
    }
    EX_CATCH
    {
        // For compat reasons, we don't want to throw right now but make sure that we
        // cache the exception so that it can be thrown if/when we try to load the
        // further down the road. See VSW 528532 for more details.
    }
    EX_END_CATCH(RethrowTransientExceptions);

    return ret;
}


// CheckMissingDependencies returns FALSE if any missing dependency would
// successfully bind with an AssemblyResolve event. When this is the case, we
// want to avoid sharing this assembly, since AssemblyResolve events are not
// under our control, and therefore not predictable.
CMD_State DomainAssembly::CheckMissingDependencies()
{
    CONTRACTL {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (MissingDependenciesCheckDone())
        return m_MissingDependenciesCheckStatus;

    if (this->GetAppDomain()->IsCompilationDomain())
    {
        // Compilation domains will never have resolve events.  Plus, this path
        // will sidestep the compilation domain's bind override, which will make
        // us skip over some dependencies.
        m_MissingDependenciesCheckStatus = CMD_NotNeeded;
        return m_MissingDependenciesCheckStatus;
    }

    if (IsSystem())
    {
        m_MissingDependenciesCheckStatus = CMD_NotNeeded;
        return m_MissingDependenciesCheckStatus;
    }

    GCX_PREEMP();
    IAssemblyBindingClosure * pClosure = GetAssemblyBindingClosure(LEVEL_COMPLETE);

    if(pClosure == NULL)
    {
        // If the closure is empty, no need to iterate them.
        m_MissingDependenciesCheckStatus = CMD_NotNeeded;
        return m_MissingDependenciesCheckStatus;
    }

    for (DWORD idx = 0;;idx++)
    {
        switch (CheckMissingDependencyInner(pClosure, idx))
        {
          case CMDI_AssemblyResolveSucceeded:
          {
            STRESS_LOG1(LF_CODESHARING,LL_INFO100,"Missing dependencies check FAILED, DomainAssembly=%p",this);
            m_MissingDependenciesCheckStatus = CMD_Resolved;
            return m_MissingDependenciesCheckStatus;
            break;
          }

          case CMDI_End:
          {
            STRESS_LOG1(LF_CODESHARING,LL_INFO100,"Missing dependencies check SUCCESSFUL, DomainAssembly=%p",this);
            m_MissingDependenciesCheckStatus = CMD_IndeedMissing;
            return m_MissingDependenciesCheckStatus;
            break;
          }

          case CMDI_AssemblyResolveFailed:
          {
            // Don't take any action, just continue the loop.
            break;
          }
        }
    }
}
#endif // FEATURE_FUSION

BOOL DomainAssembly::MissingDependenciesCheckDone()
{
    return m_MissingDependenciesCheckStatus != CMD_Unknown;
}

#ifdef FEATURE_CORECLR
CMD_State DomainAssembly::CheckMissingDependencies()
{
    //CoreCLR simply doesn't share if dependencies are missing
    return CMD_NotNeeded;
}
#endif // FEATURE_CORECLR

#endif // FEATURE_LOADER_OPTIMIZATION

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
DomainFile* DomainAssembly::FindModule(PEFile *pFile, BOOL includeLoading)
{
    CONTRACT (DomainFile*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ModuleIterator i = IterateModules(includeLoading ? kModIterIncludeLoading : kModIterIncludeLoaded);
    while (i.Next())
    {
        if (i.GetDomainFile()->Equals(pFile))
            RETURN i.GetDomainFile();
    }
    RETURN NULL;
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

DomainFile* DomainAssembly::FindIJWModule(HMODULE hMod)
{
    CONTRACT (DomainFile*)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ModuleIterator i = IterateModules(kModIterIncludeLoaded);
    while (i.Next())
    {
        PEFile *pFile = i.GetDomainFile()->GetFile();

        if (   !pFile->IsResource()
            && !pFile->IsDynamic()
            && !pFile->IsILOnly()
            && pFile->GetIJWBase() == hMod)
        {
            RETURN i.GetDomainFile();
        }
    }
    RETURN NULL;
}


void DomainAssembly::Begin()
{
    STANDARD_VM_CONTRACT;

    {
        AppDomain::LoadLockHolder lock(m_pDomain);
        m_pDomain->AddAssembly(this);
    }
    // Make it possible to find this DomainAssembly object from associated ICLRPrivAssembly.
    GetAppDomain()->PublishHostedAssembly(this);
    m_fHostAssemblyPublished = true;
}

#ifdef FEATURE_PREJIT
void DomainAssembly::FindNativeImage()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    // For non-Apollo builds (i.e., when FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS is
    // NOT defined), this is how we avoid use of NGEN when diagnostics requests it: By
    // clearing it out and forcing a load of the MSIL assembly. For Apollo builds
    // (FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS), though, this doesn't work, as we
    // don't have MSIL assemblies handy (particularly for Fx Assemblies), so we need to
    // keep the NGENd image loaded, but to treat it as if it were an MSIL assembly. See
    // code:PEFile::SetNativeImage.
#ifndef FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS
    if (!NGENImagesAllowed())
    {
        GetFile()->SetCannotUseNativeImage();

        if (GetFile()->HasNativeImage())
            GetFile()->ClearNativeImage();

        return;
    }
#endif // FEATURE_TREAT_NI_AS_MSIL_DURING_DIAGNOSTICS

#ifndef FEATURE_CORECLR // hardbinding
    // The IsSafeToHardBindTo() check is only for use during the ngen compilation phase. It discards ngen images for
    // assemblies that aren't hard-bound to (as this would cause all such assemblies be loaded eagerly.)
    if (!IsSystem() && this->GetAppDomain()->IsCompilationDomain() && !GetFile()->IsSafeToHardBindTo())
    {
        if (!this->GetAppDomain()->ToCompilationDomain()->IsSafeToHardBindTo(GetFile()))
        {
            GetFile()->SetCannotUseNativeImage();

            if (GetFile()->HasNativeImage())
                GetFile()->ClearNativeImage();

            return;
        }

        GetFile()->SetSafeToHardBindTo();
    }
#endif

#ifdef FEATURE_FUSION
    DomainAssembly * pDomainAssembly = GetDomainAssembly();
    if (pDomainAssembly->GetSecurityDescriptor()->HasAdditionalEvidence() ||
        !(pDomainAssembly->GetFile()->IsContextLoad() ||
        pDomainAssembly->GetFile()->HasHostAssembly()))
    {
        m_pFile->SetCannotUseNativeImage();
    }
#endif //FEATURE_FUSION

    ClearNativeImageStress();

    // We already have an image - we just need to do a few more checks

    if (GetFile()->HasNativeImage())
    {
#if defined(_DEBUG) && defined(FEATURE_CORECLR)
        if (g_pConfig->ForbidZap(GetSimpleName()))
        {
            SString sbuf;
            StackScratchBuffer scratch;
            sbuf.Printf("COMPlus_NgenBind_ZapForbid violation: %s.", GetSimpleName());
            DbgAssertDialog(__FILE__, __LINE__, sbuf.GetUTF8(scratch));
        }
#endif

        ReleaseHolder<PEImage> pNativeImage = GetFile()->GetNativeImageWithRef();

        if(!IsSystem() && !SystemDomain::System()->SystemFile()->HasNativeImage() && !CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NgenAllowMscorlibSoftbind))
        {
            m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_MscorlibNotNative;
            STRESS_LOG2(LF_ZAP,LL_INFO100,"Rejecting native file %p, because mscolib has not NI - reason 0x%x\n",pNativeImage.GetValue(),m_dwReasonForRejectingNativeImage);
            ExternalLog(LL_ERROR, "Rejecting native image because mscorlib does not have native image");
#ifdef FEATURE_FUSION
            if(GetFile())
                GetFile()->ETWTraceLogMessage(ETW::BinderLog::BinderStructs::NGEN_BIND_SYSTEM_ASSEMBLY_NATIVEIMAGE_NOT_AVAILABLE, NULL);
#endif
            GetFile()->ClearNativeImage();

#ifdef FEATURE_WINDOWSPHONE
            // On Phone, always through exceptions when we throw the NI out
            ThrowHR(CLR_E_BIND_SYS_ASM_NI_MISSING);
#endif
        }
        else
        if (!CheckZapSecurity(pNativeImage))
        {
            m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_FailedSecurityCheck;
            STRESS_LOG2(LF_ZAP,LL_INFO100,"Rejecting native file %p, because security check failed - reason 0x%x\n",pNativeImage.GetValue(),m_dwReasonForRejectingNativeImage);
            ExternalLog(LL_ERROR, "Rejecting native image because it failed the security check. "
                "The assembly's permissions must have changed since the time it was ngenned, "
                "or it is running with a different security context.");

#ifdef FEATURE_FUSION
            if(GetFile())
                GetFile()->ETWTraceLogMessage(ETW::BinderLog::BinderStructs::NGEN_BIND_ASSEMBLY_HAS_DIFFERENT_GRANT, NULL);
#endif
            GetFile()->ClearNativeImage();

#ifdef FEATURE_WINDOWSPHONE
            // On Phone, always through exceptions when we throw the NI out
            ThrowHR(CLR_E_BIND_NI_SECURITY_FAILURE);
#endif

        }
        else if (!CheckZapDependencyIdentities(pNativeImage))
        {
            m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_DependencyIdentityMismatch;
            STRESS_LOG2(LF_ZAP,LL_INFO100,"Rejecting native file %p, because dependency identity mismatch - reason 0x%x\n",pNativeImage.GetValue(),m_dwReasonForRejectingNativeImage);
            ExternalLog(LL_ERROR, "Rejecting native image because of identity mismatch "
                "with one or more of its assembly dependencies. The assembly needs "
                "to be ngenned again");

#ifdef FEATURE_FUSION
            if(GetFile())
                GetFile()->ETWTraceLogMessage(ETW::BinderLog::BinderStructs::NGEN_BIND_DEPENDENCY_HAS_DIFFERENT_IDENTITY, NULL);
#endif
            GetFile()->ClearNativeImage();

#ifdef FEATURE_WINDOWSPHONE
            // On Phone, always through exceptions when we throw the NI out
            ThrowHR(CLR_E_BIND_NI_DEP_IDENTITY_MISMATCH);
#endif

        }
        else
        {
            // We can only use a native image for a single Module. If this is a domain-bound
            // load, we know that this means only a single load will use this image, so we can just
            // flag it as in use.

            // If on the other hand, we are going to be domain neutral, we may have many loads use
            // the same native image.  Still, we only want to allow the native image to be used
            // by loads which are going to end up with the same Module.  So, we have to effectively
            // eagerly compute whether that will be the case eagerly, now.  To enable this computation,
            // we store the binding closure in the image.

            Module *  pNativeModule = pNativeImage->GetLoadedLayout()->GetPersistedModuleImage();
            EnsureWritablePages(pNativeModule);
            PEFile ** ppNativeFile = (PEFile **) (PBYTE(pNativeModule) + Module::GetFileOffset());
            BOOL bExpectedToBeShared= ShouldLoadDomainNeutral();
            if (!bExpectedToBeShared)
            {
                GetFile()->SetNativeImageUsedExclusively();
            }
#ifdef FEATURE_FUSION
            else
            {
                if (!IsSystem())
                {
                    GetFile()->SetNativeImageClosure(GetAssemblyBindingClosure(LEVEL_STARTING));
                }
            }
#endif //FEATURE_FUSION

            PEAssembly * pFile = (PEAssembly *)FastInterlockCompareExchangePointer((void **)ppNativeFile, (void *)GetFile(), (void *)NULL);
            STRESS_LOG3(LF_ZAP,LL_INFO100,"Attempted to set  new native file %p, old file was %p, location in the image=%p\n",GetFile(),pFile,ppNativeFile);
            if (pFile!=NULL && !IsSystem() &&

                    ( !bExpectedToBeShared ||
                       pFile == PEFile::Dummy() ||
                       pFile->IsNativeImageUsedExclusively() ||
#ifdef FEATURE_FUSION
                       !pFile->HasEqualNativeClosure(this) ||
#endif //FEATURE_FUSION
                       !(GetFile()->GetPath().Equals(pFile->GetPath())))

                )
            {
                // The non-shareable native image has already been used in this process by another Module.
                // We have to abandon the native image.  (Note that it isn't enough to
                // just abandon the preload image, since the code in the file will
                // reference the image directly).
                m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_CannotShareNiAssemblyNotDomainNeutral;
                STRESS_LOG3(LF_ZAP,LL_INFO100,"Rejecting native file %p, because it is already used by file %p - reason 0x%x\n",GetFile(),pFile,m_dwReasonForRejectingNativeImage);
                
                ExternalLog(LL_WARNING, "ZAP: An ngen image of an assembly which "
                    "is not loaded as domain-neutral cannot be used in multiple appdomains "
                    "- abandoning ngen image. The assembly will be JIT-compiled in "
                    "the second appdomain. See System.LoaderOptimization.MultiDomain "
                    "for information about domain-neutral loading.");
#ifdef FEATURE_FUSION
                if(GetFile())
                    GetFile()->ETWTraceLogMessage(ETW::BinderLog::BinderStructs::NGEN_BIND_ASSEMBLY_NOT_DOMAIN_NEUTRAL, NULL);
#endif
                GetFile()->ClearNativeImage();

                // We only support a (non-shared) native image to be used from a single
                // AppDomain. Its not obvious if this is an implementation restriction,
                // or if this should fail DomainFile::CheckZapRequired().
                // We err on the side of conservativeness, so that multi-domain tests
                // do not blow up in CheckZapRequired()
                GetFile()->SetCannotUseNativeImage();
            }
            else
            {
                //If we are the first and others can reuse us, we cannot go away
                if ((pFile == NULL) && (!GetFile()->IsNativeImageUsedExclusively()))
                    GetFile()->AddRef();

                LOG((LF_ZAP, LL_INFO100, "ZAP: Found a candidate native image for %s\n", GetSimpleName()));
            }
        }
    }

#if defined(FEATURE_CORECLR)
    if (!GetFile()->HasNativeImage())
    {
        //
        // Verify that the IL image is consistent with the NGen images loaded into appdomain
        //

        AssemblySpec spec;
        spec.InitializeSpec(GetFile());

        GUID mvid;
        GetFile()->GetMVID(&mvid);

        GetAppDomain()->CheckForMismatchedNativeImages(&spec, &mvid);
    }
#endif

    CheckZapRequired();
}
#endif // FEATURE_PREJIT

BOOL DomainAssembly::ShouldLoadDomainNeutral()
{
    STANDARD_VM_CONTRACT;

    if (m_fCalculatedShouldLoadDomainNeutral)
        return m_fShouldLoadDomainNeutral;
    
    m_fShouldLoadDomainNeutral = !!ShouldLoadDomainNeutralHelper();
    m_fCalculatedShouldLoadDomainNeutral = true;

    return m_fShouldLoadDomainNeutral;
}

BOOL DomainAssembly::ShouldLoadDomainNeutralHelper()
{
    STANDARD_VM_CONTRACT;

#ifdef FEATURE_LOADER_OPTIMIZATION

#ifndef FEATURE_CORECLR

    BOOL fIsShareableHostAssembly = FALSE;
    if (GetFile()->HasHostAssembly())
    {
        IfFailThrow(GetFile()->GetHostAssembly()->IsShareable(&fIsShareableHostAssembly));
    }
    
#ifdef FEATURE_FUSION
    // Only use domain neutral code for normal assembly loads
    if ((GetFile()->GetFusionAssembly() == NULL) && !fIsShareableHostAssembly)
    {
        return FALSE;
    }
#endif

#ifdef FEATURE_REFLECTION_ONLY_LOAD
    // Introspection only does not use domain neutral code
    if (IsIntrospectionOnly())
        return FALSE;
#endif

#ifdef FEATURE_FUSION
    // use domain neutral code only for Load context, as the
    // required eager binding interferes with LoadFrom binding semantics
    if (!GetFile()->IsContextLoad() && !fIsShareableHostAssembly)
        return FALSE;
#endif

    // Check app domain policy...
    if (this->GetAppDomain()->ApplySharePolicy(this))
    {
        if (IsSystem())
            return TRUE;

        // if not the default AD, ensure that the closure is filled in
        if (this->GetAppDomain() != SystemDomain::System()->DefaultDomain())
            GetAssemblyBindingClosure(LEVEL_COMPLETE);


        // Can be domain neutral only if we aren't binding any missing dependencies with
        // the assembly resolve event
        if ((this->GetAppDomain() != SystemDomain::System()->DefaultDomain()) &&
            (CheckMissingDependencies() == CMD_Resolved))
        {
            return FALSE;
        }

        // Ensure that all security conditions are met for code sharing
        if (!Security::CanShareAssembly(this))
        {
            return FALSE;
        }

        return TRUE;
    }
    return FALSE;

#else // FEATURE_CORECLR

    if (IsSystem())
        return TRUE;

    if (IsSingleAppDomain())
        return FALSE;

    if (GetFile()->IsDynamic())
        return FALSE;

#ifdef FEATURE_COMINTEROP
    if (GetFile()->IsWindowsRuntime())
        return FALSE;
#endif

    switch(this->GetAppDomain()->GetSharePolicy()) {
    case AppDomain::SHARE_POLICY_ALWAYS:
        return TRUE;

    case AppDomain::SHARE_POLICY_GAC:
        return IsSystem();

    case AppDomain::SHARE_POLICY_NEVER:
        return FALSE;

    case AppDomain::SHARE_POLICY_UNSPECIFIED:
    case AppDomain::SHARE_POLICY_COUNT:
        break;
    }
    
    return FALSE; // No meaning in doing costly closure walk for CoreCLR.

#endif // FEATURE_CORECLR

#else // FEATURE_LOADER_OPTIMIZATION
    return IsSystem();
#endif // FEATURE_LOADER_OPTIMIZATION
}

BOOL DomainAssembly::ShouldSkipPolicyResolution()
{
    LIMITED_METHOD_CONTRACT;
    return m_fSkipPolicyResolution;
}


#if defined(FEATURE_LOADER_OPTIMIZATION) && defined(FEATURE_FUSION)
//
// Returns TRUE if the attempt to steal ownership of the native image succeeded, or if there are other
// reasons for retrying load of the native image in the current appdomain.
//
// Returns FALSE if the native image should be rejected in the current appdomain.
//
static BOOL TryToStealSharedNativeImageOwnership(PEFile ** ppNativeImage, PEFile * pNativeFile, PEFile * pFile)
{
    STANDARD_VM_CONTRACT;

    if (pNativeFile == PEFile::Dummy())
    {
        // Nothing to steal anymore. Loading of the native image failed elsewhere.
        return FALSE;
    }

    _ASSERTE(!pNativeFile->IsNativeImageUsedExclusively());
    _ASSERTE(!pFile->IsNativeImageUsedExclusively());

    SharedDomain * pSharedDomain = SharedDomain::GetDomain();

    // Take the lock so that nobody steals or creates Assembly object for this native image while we are stealing it
    SharedFileLockHolder pNativeFileLock(pSharedDomain, pNativeFile, TRUE);

    if (pNativeFile != VolatileLoad(ppNativeImage))
    {
        // The ownership changed before we got a chance. Retry.
        return TRUE;
    }

    SharedAssemblyLocator locator(pNativeFile->AsAssembly(), SharedAssemblyLocator::PEASSEMBLYEXACT);
    if (pSharedDomain->FindShareableAssembly(&locator))
    {
        // Another shared assembly (with different binding closure) uses this image, therefore we cannot use it
        return FALSE;
    }

    BOOL success = InterlockedCompareExchangeT(ppNativeImage, pFile, pNativeFile) == pNativeFile;

    // If others can reuse us, we cannot go away
    if (success)
        pFile->AddRef();

    STRESS_LOG3(LF_ZAP,LL_INFO100,"Attempt to steal ownership from native file %p by %p success %d\n", pNativeFile, pFile, success);

    return TRUE;
}
#endif // FEATURE_LOADER_OPTIMIZATION && FEATURE_FUSION

// This is where the decision whether an assembly is DomainNeutral (shared) nor not is made.
void DomainAssembly::Allocate()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    // Make sure the security system is happy with this assembly being loaded into the domain
    GetSecurityDescriptor()->CheckAllowAssemblyLoad();

    AllocMemTracker   amTracker;
    AllocMemTracker * pamTracker = &amTracker;
    
    Assembly * pAssembly = m_pAssembly;
    
    if (pAssembly==NULL)
    {
        //! If you decide to remove "if" do not remove this brace: order is important here - in the case of an exception,
        //! the Assembly holder must destruct before the AllocMemTracker declared above.

        NewHolder<Assembly> assemblyHolder(NULL);

        // Determine whether we are supposed to load the assembly as a shared
        // assembly or into the app domain.
        if (ShouldLoadDomainNeutral())
        {

#ifdef FEATURE_LOADER_OPTIMIZATION

#ifdef FEATURE_FUSION
Retry:
#endif

            // Try to find an existing shared version of the assembly which
            // is compatible with our domain.

            SharedDomain * pSharedDomain = SharedDomain::GetDomain();

            SIZE_T nInitialShareableAssemblyCount = pSharedDomain->GetShareableAssemblyCount();
            DWORD dwSwitchCount = 0;

            SharedFileLockHolder pFileLock(pSharedDomain, GetFile(), FALSE);

            if (IsSystem())
            {
                pAssembly=SystemDomain::SystemAssembly();
            }
            else
            {
                SharedAssemblyLocator locator(this);
                pAssembly = pSharedDomain->FindShareableAssembly(&locator);

                if (pAssembly == NULL)
                {
                    pFileLock.Acquire();
                    pAssembly = pSharedDomain->FindShareableAssembly(&locator);
                }
            }

            if (pAssembly == NULL)
            {
#ifdef FEATURE_FUSION
                // Final verification that we can use the ngen image.
                //
                // code:DomainAssembly::FindNativeImage checks the binding closures before declaring the native image as shareable candidate, 
                // but the ultimate decisions about sharing happens inside code:Assembly::CanBeShared called from FindShareableAssembly above. 
                // code:Assembly::CanBeShared checks more conditions than just binding closures. In particular, it also checks whether AssemblyResolve 
                // event resolves any missing dependencies found in the binding closure - the assembly cannot be shared if it is the case.
                // The end result is that same ngen image can get here in multiple domains in parallel, but it may not be shareable between all of them.
                //
                // We reconcile this conflict by checking whether there is somebody else conflicting with us. If it is, we will try to steal
                // the ownership of the native image from the other guy and retry. The retry logic is required to prevent a perfectly valid
                // native image being dropped on the floor just because of multiple appdomains raced to load it.
                {
                    ReleaseHolder<PEImage> pNativeImage = GetFile()->GetNativeImageWithRef();
                    if ((pNativeImage != NULL) && (pNativeImage->GetLoadedLayout() != NULL))
                    {
                        Module * pNativeModule = pNativeImage->GetLoadedLayout()->GetPersistedModuleImage();
                        if (pNativeModule != NULL)
                        {
                            // The owner of the native module was set thread-safe in code:DomainAssembly::FindNativeImage
                            // However the final decision if we can share the native image is done in this function (see usage of code:FindShareableAssembly above)
                            PEFile ** ppNativeFile = (PEFile **) (PBYTE(pNativeModule) + Module::GetFileOffset());
                            PEFile * pNativeFile = VolatileLoad(ppNativeFile);
                            if (pNativeFile != GetFile())
                            {
                                pFileLock.Release();

                                // Ensures that multiple threads won't fight with each other indefinitely
                                __SwitchToThread(0, ++dwSwitchCount);

                                if (!TryToStealSharedNativeImageOwnership(ppNativeFile, pNativeFile, GetFile()))
                                {
                                    // If a shared assembly got loaded in the mean time, retry all lookups again
                                    if (pSharedDomain->GetShareableAssemblyCount() != nInitialShareableAssemblyCount)
                                        goto Retry;

                                    m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_NiAlreadyUsedInAnotherSharedAssembly;
                                    STRESS_LOG3(LF_ZAP,LL_INFO100,"Rejecting native file %p, because it is already used by shared file %p - reason 0x%x\n",GetFile(),pNativeFile,m_dwReasonForRejectingNativeImage);
                                    GetFile()->ClearNativeImage();
                                    GetFile()->SetCannotUseNativeImage();
                                }

                                goto Retry;
                            }
                        }
                    }
                }
#endif // FEATURE_FUSION

                // We can now rely on the fact that our MDImport will not change so we can stop refcounting it.
                GetFile()->MakeMDImportPersistent();

                // Go ahead and create new shared version of the assembly if possible
                // <TODO> We will need to pass a valid OBJECREF* here in the future when we implement SCU </TODO>
                assemblyHolder = pAssembly = Assembly::Create(pSharedDomain, GetFile(), GetDebuggerInfoBits(), FALSE, pamTracker, NULL);

                if (MissingDependenciesCheckDone())
                    pAssembly->SetMissingDependenciesCheckDone();

                // Compute the closure assembly dependencies
                // of the code & layout of given assembly.
                //
                // An assembly has direct dependencies listed in its manifest.
                //
                // We do not in general also have all of those dependencies' dependencies in the manifest.
                // After all, we may be only using a small portion of the assembly.
                //
                // However, since all dependent assemblies must also be shared (so that
                // the shared data in this assembly can refer to it), we are in
                // effect forced to behave as though we do have all of their dependencies.
                // This is because the resulting shared assembly that we will depend on
                // DOES have those dependencies, but we won't be able to validly share that
                // assembly unless we match all of ITS dependencies, too.
#ifdef FEATURE_FUSION
                if ((this->GetAppDomain()->GetFusionContext() != NULL) && !IsSystem())
                {
                    IAssemblyBindingClosure* pClosure = GetAssemblyBindingClosure(LEVEL_STARTING);
                    pAssembly->SetBindingClosure(pClosure);
                }
#endif // FEATURE_FUSION
                // Sets the tenured bit atomically with the hash insert.
                pSharedDomain->AddShareableAssembly(pAssembly);
            }
#else // FEATURE_LOADER_OPTIMIZATION
            _ASSERTE(IsSystem());
            if (SystemDomain::SystemAssembly())
            {
                pAssembly = SystemDomain::SystemAssembly();
            }
            else
            {
                // We can now rely on the fact that our MDImport will not change so we can stop refcounting it.
                GetFile()->MakeMDImportPersistent();

                // <TODO> We will need to pass a valid OBJECTREF* here in the future when we implement SCU </TODO>
                SharedDomain * pSharedDomain = SharedDomain::GetDomain();
                assemblyHolder = pAssembly = Assembly::Create(pSharedDomain, GetFile(), GetDebuggerInfoBits(), FALSE, pamTracker, NULL);
                pAssembly->SetIsTenured();
            }
#endif  // FEATURE_LOADER_OPTIMIZATION
        }
        else
        {
            // We can now rely on the fact that our MDImport will not change so we can stop refcounting it.
            GetFile()->MakeMDImportPersistent();
            
            // <TODO> We will need to pass a valid OBJECTREF* here in the future when we implement SCU </TODO>
            assemblyHolder = pAssembly = Assembly::Create(m_pDomain, GetFile(), GetDebuggerInfoBits(), FALSE, pamTracker, NULL);
            assemblyHolder->SetIsTenured();
        }


        //@todo! This is too early to be calling SuppressRelease. The right place to call it is below after
        // the CANNOTTHROWCOMPLUSEXCEPTION. Right now, we have to do this to unblock OOM injection testing quickly
        // as doing the right thing is nontrivial.
        pamTracker->SuppressRelease();
        assemblyHolder.SuppressRelease();
    }

#ifdef FEATURE_COMINTEROP
    // If we are in an AppX process we should prevent loading of PIA in the AppDomain.
    // This will ensure that we do not run into any compatibility issues in case a type has both a co-Class and a Winrt Class
    if (AppX::IsAppXProcess() && pAssembly->IsPIA())
    {
        COMPlusThrow(kNotSupportedException, W("NotSupported_PIAInAppxProcess"));
    }
#endif

    SetAssembly(pAssembly);

#ifdef FEATURE_PREJIT
    BOOL fInsertIntoAssemblySpecBindingCache = TRUE;

    // Insert AssemblyDef details into AssemblySpecBindingCache if appropriate

#ifdef FEATURE_FUSION
    fInsertIntoAssemblySpecBindingCache = GetFile()->GetLoadContext() == LOADCTX_TYPE_DEFAULT;
#endif
    
#if defined(FEATURE_APPX_BINDER)
    fInsertIntoAssemblySpecBindingCache = fInsertIntoAssemblySpecBindingCache && !GetFile()->HasHostAssembly();
#else
    fInsertIntoAssemblySpecBindingCache = fInsertIntoAssemblySpecBindingCache && GetFile()->CanUseWithBindingCache();
#endif

    if (fInsertIntoAssemblySpecBindingCache)
    {
        AssemblySpec specAssemblyDef;
        specAssemblyDef.InitializeSpec(GetFile());
        if (specAssemblyDef.IsStrongNamed() && specAssemblyDef.HasPublicKey())
        {
            specAssemblyDef.ConvertPublicKeyToToken();
        }
        m_pDomain->AddAssemblyToCache(&specAssemblyDef, this);
    }
#endif
} // DomainAssembly::Allocate

void DomainAssembly::DeliverAsyncEvents()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    OVERRIDE_LOAD_LEVEL_LIMIT(FILE_ACTIVE);
    m_pDomain->RaiseLoadingAssemblyEvent(this);

}


void DomainAssembly::DeliverSyncEvents()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    GetCurrentModule()->NotifyEtwLoadFinished(S_OK);

    // We may be notified from inside the loader lock if we are delivering IJW events, so keep track.
#ifdef PROFILING_SUPPORTED
    if (!IsProfilerNotified())
    {
        SetProfilerNotified();
        GetCurrentModule()->NotifyProfilerLoadFinished(S_OK);
    }

#endif
#ifdef DEBUGGING_SUPPORTED
    GCX_COOP();
    if (!IsDebuggerNotified())
    {
        SetShouldNotifyDebugger();

        if (m_pDomain->IsDebuggerAttached())
        {
            // If this is the first assembly in the AppDomain, it may be possible to get a better name than the
            // default.
            CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
            m_pDomain->m_Assemblies.Get(m_pDomain, 0, pDomainAssembly.This());
            if ((pDomainAssembly == this) && !m_pDomain->IsUserCreatedDomain())
                m_pDomain->ResetFriendlyName();
        }

        // Still work to do even if no debugger is attached.
        NotifyDebuggerLoad(ATTACH_ASSEMBLY_LOAD, FALSE);

    }
#endif // DEBUGGING_SUPPORTED
} // DomainAssembly::DeliverSyncEvents

/*
  // The enum for dwLocation from managed code:
    public enum ResourceLocation
    {
        Embedded = 1,
        ContainedInAnotherAssembly = 2,
        ContainedInManifestFile = 4
    }
*/

BOOL DomainAssembly::GetResource(LPCSTR szName, DWORD *cbResource,
                                 PBYTE *pbInMemoryResource, DomainAssembly** pAssemblyRef,
                                 LPCSTR *szFileName, DWORD *dwLocation,
                                 StackCrawlMark *pStackMark, BOOL fSkipSecurityCheck,
                                 BOOL fSkipRaiseResolveEvent)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    return GetFile()->GetResource( szName,
                                   cbResource,
                                   pbInMemoryResource,
                                   pAssemblyRef,
                                   szFileName,
                                   dwLocation,
                                   pStackMark,
                                   fSkipSecurityCheck,
                                   fSkipRaiseResolveEvent,
                                   this,
                                   this->m_pDomain );
}

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
BOOL DomainAssembly::GetModuleResource(mdFile mdResFile, LPCSTR szResName,
                                       DWORD *cbResource, PBYTE *pbInMemoryResource,
                                       LPCSTR *szFileName, DWORD *dwLocation,
                                       BOOL fIsPublic, StackCrawlMark *pStackMark,
                                       BOOL fSkipSecurityCheck)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    const char     *szName;
    DWORD           dwFlags;
    DomainFile     *pModule = NULL;
    DWORD           dwOffset = 0;

    if (! ((TypeFromToken(mdResFile) == mdtFile) &&
           GetMDImport()->IsValidToken(mdResFile)) )
    {
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_FILE_TOKEN);
    }

    IfFailThrow(GetMDImport()->GetFileProps(
        mdResFile,
        &szName,
        NULL,
        NULL,
        &dwFlags));

    if (IsFfContainsMetaData(dwFlags))
    {
        // The resource is embedded in a manifest-containing file.
        mdManifestResource mdResource;
        mdToken mdLinkRef;
        DWORD dwResourceFlags;

        Module *pContainerModule = GetCurrentModule();
        // Use the real assembly with a rid map if possible
        if (pContainerModule != NULL)
            pModule = pContainerModule->LoadModule(m_pDomain, mdResFile, FALSE);
        else
        {
            PEModuleHolder pFile(GetAssembly()->LoadModule_AddRef(mdResFile, FALSE));
            pModule = m_pDomain->LoadDomainModule(this, pFile, FILE_LOADED);
        }

        if (FAILED(pModule->GetMDImport()->FindManifestResourceByName(szResName,
                                                                      &mdResource)))
            return FALSE;

        IfFailThrow(pModule->GetMDImport()->GetManifestResourceProps(
            mdResource,
            NULL, //&szName,
            &mdLinkRef,
            &dwOffset,
            &dwResourceFlags));

        if (mdLinkRef != mdFileNil)
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_CANT_GET_LINKREF);
        }
        fIsPublic = IsMrPublic(dwResourceFlags);
    }

#ifndef CROSSGEN_COMPILE
    if (!fIsPublic && pStackMark && !fSkipSecurityCheck)
    {
        Assembly *pCallersAssembly = SystemDomain::GetCallersAssembly(pStackMark);
        if (pCallersAssembly && // full trust for interop
            (!pCallersAssembly->GetManifestFile()->Equals(GetFile())))
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

    if (IsFfContainsMetaData(dwFlags)) {
        if (dwLocation) {
            *dwLocation = *dwLocation | 1; // ResourceLocation.embedded
            *szFileName = szName;
            return TRUE;
        }

        pModule->GetFile()->GetEmbeddedResource(dwOffset, cbResource,
                                                pbInMemoryResource);

        return TRUE;
    }

    // The resource is linked (it's in its own file)
    if (szFileName) {
        *szFileName = szName;
        return TRUE;
    }

    Module *pContainerModule = GetCurrentModule();

    // Use the real assembly with a rid map if possible
    if (pContainerModule != NULL)
        pModule = pContainerModule->LoadModule(m_pDomain, mdResFile);
    else
    {
        PEModuleHolder pFile(GetAssembly()->LoadModule_AddRef(mdResFile, TRUE));
        pModule = m_pDomain->LoadDomainModule(this, pFile, FILE_LOADED);
    }

    COUNT_T size;
    const void *contents = pModule->GetFile()->GetManagedFileContents(&size);

    *pbInMemoryResource = (BYTE *) contents;
    *cbResource = size;

    return TRUE;
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

#ifdef FEATURE_PREJIT

// --------------------------------------------------------------------------------
// Remember the timestamp of the CLR DLLs used to compile the ngen image.
// These will be checked at runtime by PEFile::CheckNativeImageTimeStamp().
//

void GetTimeStampsForNativeImage(CORCOMPILE_VERSION_INFO * pNativeVersionInfo)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(::GetAppDomain()->IsCompilationDomain());
    }
    CONTRACTL_END;

#ifdef FEATURE_CORECLR
    // Do not store runtime timestamps into NGen image for cross-platform NGen determinism
#else
    // fill in pRuntimeDllInfo
    CORCOMPILE_RUNTIME_DLL_INFO *pRuntimeDllInfo = pNativeVersionInfo->runtimeDllInfo;

    for (DWORD index = 0; index < NUM_RUNTIME_DLLS; index++)
    {
#ifdef CROSSGEN_COMPILE
        SString sFileName(SString::Utf8, CorCompileGetRuntimeDllName((CorCompileRuntimeDlls)index));

        PEImageHolder pImage;
        if (!GetAppDomain()->ToCompilationDomain()->FindImage(sFileName, MDInternalImport_NoCache, &pImage))
        {
            EEFileLoadException::Throw(sFileName, COR_E_FILENOTFOUND);
        }

        PEImageLayoutHolder pLayout(pImage->GetLayout(PEImageLayout::LAYOUT_FLAT,PEImage::LAYOUT_CREATEIFNEEDED));
        pRuntimeDllInfo[index].timeStamp = pLayout->GetTimeDateStamp();
        pRuntimeDllInfo[index].virtualSize = pLayout->GetVirtualSize();

#else // CROSSGEN_COMPILE

        HMODULE hMod = CorCompileGetRuntimeDll((CorCompileRuntimeDlls)index);

        if (hMod == NULL)
        {
            _ASSERTE((CorCompileRuntimeDlls)index == NGEN_COMPILER_INFO);

            LPCWSTR wszDllName = CorCompileGetRuntimeDllName((CorCompileRuntimeDlls)index);
            if (FAILED(g_pCLRRuntime->LoadLibrary(wszDllName, &hMod)))
            {
                EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Unable to load CLR DLL during ngen"));
            }
        }

        _ASSERTE(hMod != NULL);

        PEDecoder pe(hMod);

        pRuntimeDllInfo[index].timeStamp = pe.GetTimeDateStamp();
        pRuntimeDllInfo[index].virtualSize = pe.GetVirtualSize();
#endif // CROSSGEN_COMPILE

    }
#endif // FEATURE_CORECLR
}

//
// Which processor should ngen target?
// This is needed when ngen wants to target for "reach" if the ngen images will be
// used on other machines (the Operating System or the OEM build lab can do this).
// It can also be used to reduce the testing matrix
//
void GetNGenCpuInfo(CORINFO_CPU * cpuInfo)
{
    LIMITED_METHOD_CONTRACT;

#ifdef _TARGET_X86_

#ifdef FEATURE_CORECLR
    static CORINFO_CPU ngenCpuInfo =
        {
            (CPU_X86_PENTIUM_PRO << 8), // dwCPUType
            0x00000000,                 // dwFeatures
            0                           // dwExtendedFeatures
        };

    // We always generate P3-compatible code on CoreCLR
    *cpuInfo = ngenCpuInfo;
#else // FEATURE_CORECLR
    static CORINFO_CPU ngenCpuInfo =
        {
            (CPU_X86_PENTIUM_4 << 8),   // dwCPUType
            0x00008001,                 // dwFeatures
            0                           // dwExtendedFeatures
        };

#ifndef CROSSGEN_COMPILE
    GetSpecificCpuInfo(cpuInfo);
    if (!IsCompatibleCpuInfo(cpuInfo, &ngenCpuInfo))
    {
        // Use the actual cpuInfo if the platform is not compatible 
        // with the "recommended" processor. We expect most platforms to be compatible
        return;
    }
#endif

    *cpuInfo = ngenCpuInfo;
#endif // FEATURE_CORECLR

#else // _TARGET_X86_
    cpuInfo->dwCPUType = 0;
    cpuInfo->dwFeatures = 0;
    cpuInfo->dwExtendedFeatures = 0;
#endif // _TARGET_X86_
}

// --------------------------------------------------------------------------------

void DomainAssembly::GetCurrentVersionInfo(CORCOMPILE_VERSION_INFO *pNativeVersionInfo)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    // Clear memory so that we won't write random data into the zapped file
    ZeroMemory(pNativeVersionInfo, sizeof(CORCOMPILE_VERSION_INFO));

    // Pick up any compilation directives for code flavor

    BOOL fForceDebug, fForceProfiling, fForceInstrument;
    SystemDomain::GetCompilationOverrides(&fForceDebug,
                                          &fForceProfiling,
                                          &fForceInstrument);

    OSVERSIONINFOW osInfo;
    osInfo.dwOSVersionInfoSize = sizeof(osInfo);
    if (!GetOSVersion(&osInfo))
        _ASSERTE(!"GetOSVersion failed");

    _ASSERTE(osInfo.dwMajorVersion < 999);
    _ASSERTE(osInfo.dwMinorVersion < 999);
    pNativeVersionInfo->wOSPlatformID = (WORD) osInfo.dwPlatformId;

    // The native images should be OS-version agnostic. Do not store the actual OS version for determinism.
    // pNativeVersionInfo->wOSMajorVersion = (WORD) osInfo.dwMajorVersion;
    pNativeVersionInfo->wOSMajorVersion = 4;

    pNativeVersionInfo->wMachine = IMAGE_FILE_MACHINE_NATIVE_NI;

    pNativeVersionInfo->wVersionMajor = VER_MAJORVERSION;
    pNativeVersionInfo->wVersionMinor = VER_MINORVERSION;
    pNativeVersionInfo->wVersionBuildNumber = VER_PRODUCTBUILD;
    pNativeVersionInfo->wVersionPrivateBuildNumber = VER_PRODUCTBUILD_QFE;

    GetNGenCpuInfo(&pNativeVersionInfo->cpuInfo);

#if _DEBUG
    pNativeVersionInfo->wBuild = CORCOMPILE_BUILD_CHECKED;
#else
    pNativeVersionInfo->wBuild = CORCOMPILE_BUILD_FREE;
#endif

#ifdef DEBUGGING_SUPPORTED
    if (fForceDebug || !CORDebuggerAllowJITOpts(GetDebuggerInfoBits()))
    {
        pNativeVersionInfo->wCodegenFlags |= CORCOMPILE_CODEGEN_DEBUGGING;
        pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_DEBUG;
    }
    else
#endif // DEBUGGING_SUPPORTED
    {
        pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_DEBUG_NONE;
    }

#if defined (PROFILING_SUPPORTED_DATA) || defined(PROFILING_SUPPORTED)
    if (fForceProfiling || CORProfilerUseProfileImages())
    {
        pNativeVersionInfo->wCodegenFlags |= CORCOMPILE_CODEGEN_PROFILING;
        pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_PROFILING;
#ifdef DEBUGGING_SUPPORTED
        // Note that we have hardwired profiling to also imply optimized debugging
        // info.  This cuts down on one permutation of prejit files.
        pNativeVersionInfo->wCodegenFlags &= ~CORCOMPILE_CODEGEN_DEBUGGING;
        pNativeVersionInfo->wConfigFlags &= ~(CORCOMPILE_CONFIG_DEBUG|
                                              CORCOMPILE_CONFIG_DEBUG_DEFAULT);
        pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_DEBUG_NONE;
#endif // DEBUGGING_SUPPORTED
    }
    else
#endif // PROFILING_SUPPORTED_DATA || PROFILING_SUPPORTED
    {
        pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_PROFILING_NONE;
    }

#ifdef DEBUGGING_SUPPORTED

    // Note the default assembly flags (from the custom attributes & INI file) , so we can
    // set determine whether or not the current settings
    // match the "default" setting or not.

    // Note that the INI file settings are considered a part of the
    // assembly, even though they could theoretically change between
    // ngen time and runtime.  It is just too expensive and awkward to
    // look up the INI file before binding to the native image at
    // runtime, so we effectively snapshot it at ngen time.

    DWORD defaultFlags = ComputeDebuggingConfig();

    if (CORDebuggerAllowJITOpts(defaultFlags))
    {
        // Default is optimized code
        if ((pNativeVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_DEBUGGING) == 0)
            pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_DEBUG_DEFAULT;
    }
    else
    {
        // Default is non-optimized debuggable code
        if ((pNativeVersionInfo->wCodegenFlags & CORCOMPILE_CODEGEN_DEBUGGING) != 0)
            pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_DEBUG_DEFAULT;
    }

#endif // DEBUGGING_SUPPORTED

    if (fForceInstrument || GetAssembly()->IsInstrumented())
    {
        pNativeVersionInfo->wCodegenFlags |= CORCOMPILE_CODEGEN_PROF_INSTRUMENTING;
        pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_INSTRUMENTATION;
    }
    else
    {
        pNativeVersionInfo->wConfigFlags |= CORCOMPILE_CONFIG_INSTRUMENTATION_NONE;
    }

#if defined(_TARGET_AMD64_) && !defined(FEATURE_CORECLR)
    if (UseRyuJit())
    {
        pNativeVersionInfo->wCodegenFlags |= CORCOMPILE_CODEGEN_USE_RYUJIT;
    }
#endif

    GetTimeStampsForNativeImage(pNativeVersionInfo);

    // Store signature of source assembly.
    GetOptimizedIdentitySignature(&pNativeVersionInfo->sourceAssembly);

    // signature will is hash of the whole file. It is written by zapper.
    // IfFailThrow(CoCreateGuid(&pNativeVersionInfo->signature));
}

void DomainAssembly::GetOptimizedIdentitySignature(CORCOMPILE_ASSEMBLY_SIGNATURE *pSignature)
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

    //
    // Write the MVID into the version header.
    //

    //
    // If this assembly has skip verification permission, then we store its
    // mvid.  If at load time the assembly still has skip verification
    // permission, then we can base the matches purely on mvid values and
    // skip the perf-heavy hashing of the file.
    //

    //
    // The reason that we tell IsFullyTrusted to do a quick check
    // only is because that allows us make a determination for the most
    // common full trust scenarios (local machine) without actually
    // resolving policy and bringing in a whole list of assembly
    // dependencies.
    //
    ReleaseHolder<IMDInternalImport> scope (GetFile()->GetMDImportWithRef());
    IfFailThrow(scope->GetScopeProps(NULL, &pSignature->mvid));

    // Use the NGen image if posssible. IL image does not even have to be present on CoreCLR.
    if (GetFile()->HasNativeImage())
    {
        PEImageHolder pNativeImage(GetFile()->GetNativeImageWithRef());

        CORCOMPILE_VERSION_INFO* pVersionInfo = pNativeImage->GetLoadedLayout()->GetNativeVersionInfo();
        pSignature->timeStamp = pVersionInfo->sourceAssembly.timeStamp;
        pSignature->ilImageSize = pVersionInfo->sourceAssembly.ilImageSize;
 
        return;
    }

    // Write the time stamp
    PEImageLayoutHolder ilLayout(GetFile()->GetAnyILWithRef());
    pSignature->timeStamp = ilLayout->GetTimeDateStamp();
    pSignature->ilImageSize = ilLayout->GetVirtualSize();
}

BOOL DomainAssembly::CheckZapDependencyIdentities(PEImage *pNativeImage)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

#ifdef FEATURE_CORECLR
    AssemblySpec spec;
    spec.InitializeSpec(this->GetFile());

    // The assembly spec should have the binding context associated with it
    _ASSERTE(spec.GetBindingContext()  || spec.IsAssemblySpecForMscorlib());
    
    CORCOMPILE_VERSION_INFO *pVersionInfo = pNativeImage->GetLoadedLayout()->GetNativeVersionInfo();

    // Check our own assembly first
    GetAppDomain()->CheckForMismatchedNativeImages(&spec, &pVersionInfo->sourceAssembly.mvid);

    // Check MVID in metadata against MVID in CORCOMPILE_VERSION_INFO - important when metadata is loaded from IL instead of NI
    ReleaseHolder<IMDInternalImport> pImport(this->GetFile()->GetMDImportWithRef());
    GUID mvid;
    IfFailThrow(pImport->GetScopeProps(NULL, &mvid));
    GetAppDomain()->CheckForMismatchedNativeImages(&spec, &mvid);

    // Now Check dependencies
    COUNT_T cDependencies;
    CORCOMPILE_DEPENDENCY *pDependencies = pNativeImage->GetLoadedLayout()->GetNativeDependencies(&cDependencies);
    CORCOMPILE_DEPENDENCY *pDependenciesEnd = pDependencies + cDependencies;

    while (pDependencies < pDependenciesEnd)
    {
        if (pDependencies->dwAssemblyDef != mdAssemblyRefNil)
        {
            AssemblySpec name;
            name.InitializeSpec(pDependencies->dwAssemblyDef, pNativeImage->GetNativeMDImport(), this);
            
#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)            
            if (!name.IsAssemblySpecForMscorlib())
            {
                // We just initialized the assembly spec for the NI dependency. This will not have binding context
                // associated with it, so set it from that of the parent.
                _ASSERTE(!name.GetBindingContext());
                ICLRPrivBinder *pParentAssemblyBindingContext = name.GetBindingContextFromParentAssembly(name.GetAppDomain());
                _ASSERTE(pParentAssemblyBindingContext);
                name.SetBindingContext(pParentAssemblyBindingContext);
            }
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
            
            GetAppDomain()->CheckForMismatchedNativeImages(&name, &pDependencies->signAssemblyDef.mvid);
        }

        pDependencies++;
    }
#endif

    return TRUE;
}

BOOL DomainAssembly::CheckZapSecurity(PEImage *pNativeImage)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

#ifdef FEATURE_CORECLR
    return TRUE;
#else

    //
    // System libraries are a special case, the security info's always OK.
    //

    if (IsSystem())
        return TRUE;

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    //
    // If we're just loading files as part of PDB generation, we're not executing code,
    // so no need to do security checks
    //
    
    if (IsNgenPDBCompilationProcess())
        return TRUE;
#endif

    ETWOnStartup (SecurityCatchCall_V1, SecurityCatchCallEnd_V1);

#ifdef CROSSGEN_COMPILE
    return TRUE;
#else

#ifdef FEATURE_APTCA
    if (!Security::NativeImageHasValidAptcaDependencies(pNativeImage, this))
    {
        return FALSE;
    }
#endif // !FEATURE_APTCA

    GCX_COOP();

    BOOL fHostProtectionOK = FALSE;
    BOOL fImageAndDependenciesAreFullTrust = FALSE;

    EX_TRY
    {
        // Check the HostProtection settings.
        EApiCategories eRequestedProtectedCategories = GetHostProtectionManager()->GetProtectedCategories();
        if (eRequestedProtectedCategories == eNoChecks)
            fHostProtectionOK = TRUE;

        // Due to native code generated for one IL image being more agressively put into another
        // assembly's native image, we're disabling partial trust NGEN images.  If the current
        // domain can only have fully trusted assemblies, then we can load this image, or if the current
        // assembly and its closure are all in the GAC we can also use it.  Otherwise, we'll conservatively
        // disable the use of this image.
        IApplicationSecurityDescriptor *pAppDomainSecurity = this->GetAppDomain()->GetSecurityDescriptor();
        if (pAppDomainSecurity->IsFullyTrusted() && pAppDomainSecurity->IsHomogeneous())
        {
            // A fully trusted homogenous domain can only have full trust assemblies, therefore this assembly
            // and all its dependencies must be full trust
            fImageAndDependenciesAreFullTrust = TRUE;
        }
        else if (IsClosedInGAC())
        {
            // The domain allows partial trust assemblies to be loaded into it.  However, this assembly and
            // all of its dependencies came from the GAC, so we know that they must all be trusted even if
            // other code in this domain is not.
            fImageAndDependenciesAreFullTrust = TRUE;
        }
        else
        {
            // The domain allows partial trust assemblies and we cannot prove that the closure of
            // dependencies of this assembly will all be fully trusted.  Conservatively throw away this NGEN
            // image.
            fImageAndDependenciesAreFullTrust = FALSE;
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    return fHostProtectionOK && fImageAndDependenciesAreFullTrust;
#endif // CROSSGEN_COMPILE

#endif // FEATURE_CORECLR
}
#endif // FEATURE_PREJIT

#ifdef FEATURE_CAS_POLICY
void DomainAssembly::InitializeSecurityManager()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    GetFile()->InitializeSecurityManager();
}
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_CAS_POLICY
// Returns security information for the assembly based on the codebase
void DomainAssembly::GetSecurityIdentity(SString &codebase,
                                         SecZone *pdwZone,
                                         DWORD dwFlags,
                                         BYTE *pbUniqueID,
                                         DWORD *pcbUniqueID)
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

    GetFile()->GetSecurityIdentity(codebase, pdwZone, dwFlags, pbUniqueID, pcbUniqueID);
}
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_FUSION
IAssemblyBindingClosure* DomainAssembly::GetAssemblyBindingClosure(WALK_LEVEL level)
{
    CONTRACT(IAssemblyBindingClosure *)
    {
        INSTANCE_CHECK;
        POSTCONDITION(CheckPointer(RETVAL,NULL_OK));
        //we could  return NULL instead of asserting but hitting code paths that call this for mscorlib is just wasting of cycles anyhow
        PRECONDITION(!IsSystem());
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACT_END;

    if (m_pAssemblyBindingClosure == NULL || m_pAssemblyBindingClosure->HasBeenWalked(level) == S_FALSE)
    {
        SafeComHolder<IAssemblyBindingClosure> pClosure;
        if (this->GetAppDomain()->GetFusionContext() == NULL)
        {
            _ASSERTE(IsSystem());
            RETURN NULL;
        }
        
        GCX_PREEMP();

        ReleaseHolder<IBindResult> pWinRTBindResult;
        IUnknown * pUnk;
        
        if (GetFile()->IsIStream())
        {
            pUnk = GetFile()->GetIHostAssembly();
        }
        else if (GetFile()->IsWindowsRuntime())
        {   // It is .winmd file (WinRT assembly)
            IfFailThrow(CLRPrivAssemblyWinRT::GetIBindResult(GetFile()->GetHostAssembly(), &pWinRTBindResult));
            pUnk = pWinRTBindResult;
        }
        else
        {
            pUnk = GetFile()->GetFusionAssembly();
        }

        if (m_pAssemblyBindingClosure == NULL)
        {
            IfFailThrow(this->GetAppDomain()->GetFusionContext()->GetAssemblyBindingClosure(pUnk, NULL, &pClosure));
            if (FastInterlockCompareExchangePointer<IAssemblyBindingClosure*>(&m_pAssemblyBindingClosure, pClosure.GetValue(), NULL) == NULL)
            {
                pClosure.SuppressRelease();
            }
        }
        IfFailThrow(m_pAssemblyBindingClosure->EnsureWalked(pUnk, this->GetAppDomain()->GetFusionContext(), level));
    }
    RETURN m_pAssemblyBindingClosure;
}

// This is used to determine if the binding closure of the assembly in question is in the GAC. Amongst other uses,
// this is the MULTI_DOMAIN_HOST scenario.
BOOL DomainAssembly::IsClosedInGAC()
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

    if (IsSystem())
        return TRUE;

    BOOL fIsWindowsRuntime = GetFile()->IsWindowsRuntime();

    if (!GetFile()->IsSourceGAC() && !fIsWindowsRuntime)
        return FALSE;

    // Do a binding closure that will help us determine if all the dependencies are in the GAC or not.
    IAssemblyBindingClosure * pClosure = GetAssemblyBindingClosure(LEVEL_GACCHECK);
    if (pClosure == NULL)
        return FALSE;
    
    // Once the closure is complete, determine if the dependencies are closed in the GAC (or not).
    HRESULT hr = pClosure->IsAllAssembliesInGAC();
    IfFailThrow(hr);
    
    return (hr == S_OK);
}

BOOL DomainAssembly::MayHaveUnknownDependencies()
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

    if (IsSystem())
        return FALSE;
    
    // Perform the binding closure walk to initialize state that will help us
    // determine if we have dependencies that could prevent code-sharing.
    IAssemblyBindingClosure * pClosure = GetAssemblyBindingClosure(LEVEL_WINRTCHECK);
    if (pClosure == NULL)
        return FALSE;
    
    HRESULT hr = pClosure->MayHaveUnknownDependencies();
    IfFailThrow(hr);

    return (hr == S_OK);
}

#endif // FEATURE_FUSION


// <TODO>@todo Find a better place for these</TODO>
#define DE_CUSTOM_VALUE_NAMESPACE        "System.Diagnostics"
#define DE_DEBUGGABLE_ATTRIBUTE_NAME     "DebuggableAttribute"

// <TODO>@todo .INI file is a temporary workaround for Beta 1</TODO>
#define DE_INI_FILE_SECTION_NAME          W(".NET Framework Debugging Control")
#define DE_INI_FILE_KEY_TRACK_INFO        W("GenerateTrackingInfo")
#define DE_INI_FILE_KEY_ALLOW_JIT_OPTS    W("AllowOptimize")

DWORD DomainAssembly::ComputeDebuggingConfig()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#ifdef DEBUGGING_SUPPORTED
    DWORD dacfFlags = DACF_ALLOW_JIT_OPTS;

    if (GetDebuggingOverrides(&dacfFlags))
    {
        dacfFlags |= DACF_USER_OVERRIDE;
    }
    else
    {
        IfFailThrow(GetDebuggingCustomAttributes(&dacfFlags));
    }

    return dacfFlags;
#else // !DEBUGGING_SUPPORTED
    return 0;
#endif // DEBUGGING_SUPPORTED
}

void DomainAssembly::SetupDebuggingConfig(void)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

#ifdef DEBUGGING_SUPPORTED
    DWORD dacfFlags = ComputeDebuggingConfig();

    SetDebuggerInfoBits((DebuggerAssemblyControlFlags)dacfFlags);

    LOG((LF_CORDB, LL_INFO10, "Assembly %S: bits=0x%x\n", GetDebugName(), GetDebuggerInfoBits()));
#endif // DEBUGGING_SUPPORTED
}

// The format for the (temporary) .INI file is:

// [.NET Framework Debugging Control]
// GenerateTrackingInfo=<n> where n is 0 or 1
// AllowOptimize=<n> where n is 0 or 1

// Where neither x nor y equal INVALID_INI_INT:
#define INVALID_INI_INT (0xFFFF)

bool DomainAssembly::GetDebuggingOverrides(DWORD *pdwFlags)
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

#if defined(DEBUGGING_SUPPORTED) && !defined(FEATURE_CORESYSTEM)
    // TODO FIX in V5.0
    // Any touch of the file system is relatively expensive even in the warm case. 
    // 
    // Ideally we remove the .INI feature completely (if we need something put it in the .exe.config file)
    // 
    // However because of compatibility concerns, we won't do this until the next side-by-side release
    // In the mean time don't check in the case where we have already loaded the NGEN image, as the
    // JIT overrides don't mean anything in that case as we won't be jitting anyway.  
    // This avoids doing these probes for framework DLLs right away.
    if (GetFile()->HasNativeImage())
        return false;

    _ASSERTE(pdwFlags);

    bool fHasBits = false;
    WCHAR *pFileName = NULL;
    HRESULT hr = S_OK;
    UINT cbExtOrValue = 4;
    WCHAR *pTail = NULL;
    size_t len = 0;
    WCHAR *lpFileName = NULL;

    const WCHAR *wszFileName = GetFile()->GetPath();

    if (wszFileName == NULL)
    {
        return false;
    }

    // lpFileName is a copy of the original, and will be edited.
    CQuickBytes qb;
    len = wcslen(wszFileName);
    size_t cchlpFileName = (len + 1);
    lpFileName = (WCHAR*)qb.AllocThrows(cchlpFileName * sizeof(WCHAR));
    wcscpy_s(lpFileName, cchlpFileName, wszFileName);

    pFileName = wcsrchr(lpFileName, W('\\'));

    if (pFileName == NULL)
    {
        pFileName = lpFileName;
    }

    if (*pFileName == W('\\'))
    {
        pFileName++; //move the pointer past the last '\'
    }

    _ASSERTE(wcslen(W(".INI")) == cbExtOrValue);

    if (pFileName == NULL || (pTail=wcsrchr(pFileName, W('.'))) == NULL || (wcslen(pTail)<cbExtOrValue))
    {
        return false;
    }

    wcscpy_s(pTail, cchlpFileName - (pTail - lpFileName), W(".INI"));

    // Win2K has a problem if multiple processes call GetPrivateProfile* on the same
    // non-existent .INI file simultaneously.  The OS livelocks in the kernel (i.e.
    // outside of user space) and remains there at full CPU for several minutes.  Then
    // it breaks out.  Here is our work-around, while we pursue a fix in a future
    // version of the OS.
    if (WszGetFileAttributes(lpFileName) == INVALID_FILE_ATTRIBUTES)
        return false;

    // Having modified the filename, we use the full path
    // to actually get the file.
    if ((cbExtOrValue=WszGetPrivateProfileInt(DE_INI_FILE_SECTION_NAME,
                                              DE_INI_FILE_KEY_TRACK_INFO,
                                              INVALID_INI_INT,
                                              lpFileName)) != INVALID_INI_INT)
    {
        if (cbExtOrValue != 0)
        {
            *pdwFlags |= DACF_OBSOLETE_TRACK_JIT_INFO;
        }
        else
        {
            *pdwFlags &= (~DACF_OBSOLETE_TRACK_JIT_INFO);
        }

        fHasBits = true;
    }

    if ((cbExtOrValue=WszGetPrivateProfileInt(DE_INI_FILE_SECTION_NAME,
                                              DE_INI_FILE_KEY_ALLOW_JIT_OPTS,
                                              INVALID_INI_INT,
                                              lpFileName)) != INVALID_INI_INT)
    {
        if (cbExtOrValue != 0)
        {
            *pdwFlags |= DACF_ALLOW_JIT_OPTS;
        }
        else
        {
            *pdwFlags &= (~DACF_ALLOW_JIT_OPTS);
        }

        fHasBits = true;
    }

    return fHasBits;

#else  // DEBUGGING_SUPPORTED && !FEATURE_CORESYSTEM
    return false;
#endif // DEBUGGING_SUPPORTED && !FEATURE_CORESYSTEM
}


// For right now, we only check to see if the DebuggableAttribute is present - later may add fields/properties to the
// attributes.
HRESULT DomainAssembly::GetDebuggingCustomAttributes(DWORD *pdwFlags)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pdwFlags));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

#ifdef FEATURE_PREJIT
    ReleaseHolder<PEImage> pNativeImage=GetFile()->GetNativeImageWithRef();
    if (pNativeImage)
    {
        CORCOMPILE_VERSION_INFO * pVersion = pNativeImage->GetLoadedLayout()->GetNativeVersionInfo();
        PREFIX_ASSUME(pVersion != NULL);

        WORD codegen = pVersion->wCodegenFlags;

        if (codegen & CORCOMPILE_CODEGEN_DEBUGGING)
        {
            *pdwFlags &= (~DACF_ALLOW_JIT_OPTS);
        }
        else
        {
            *pdwFlags |= DACF_ALLOW_JIT_OPTS;
        }

    }
    else
#endif // FEATURE_PREJIT
    {
        ULONG size;
        BYTE *blob;
        mdModule mdMod;
        ReleaseHolder<IMDInternalImport> mdImport(GetFile()->GetMDImportWithRef());
        mdMod = mdImport->GetModuleFromScope();
        mdAssembly asTK = TokenFromRid(mdtAssembly, 1);

        hr = mdImport->GetCustomAttributeByName(asTK,
                                                DE_CUSTOM_VALUE_NAMESPACE
                                                NAMESPACE_SEPARATOR_STR
                                                DE_DEBUGGABLE_ATTRIBUTE_NAME,
                                                (const void**)&blob,
                                                &size);

        // If there is no custom value, then there is no entrypoint defined.
        if (!(FAILED(hr) || hr == S_FALSE))
        {
            // We're expecting a 6 or 8 byte blob:
            //
            // 1, 0, enable tracking, disable opts, 0, 0
            if ((size == 6) || (size == 8))
            {
                if (!((blob[0] == 1) && (blob[1] == 0)))
                {
                    BAD_FORMAT_NOTHROW_ASSERT(!"Invalid blob format for custom attribute");
                    return COR_E_BADIMAGEFORMAT;
                }

                if (blob[2] & 0x1)
                {
                    *pdwFlags |= DACF_OBSOLETE_TRACK_JIT_INFO;
                }
                else
                {
                    *pdwFlags &= (~DACF_OBSOLETE_TRACK_JIT_INFO);
                }

                if (blob[2] & 0x2)
                {
                    *pdwFlags |= DACF_IGNORE_PDBS;
                }
                else
                {
                    *pdwFlags &= (~DACF_IGNORE_PDBS);
                }


                // For compatibility, we enable optimizations if the tracking byte is zero,
                // even if disable opts is nonzero
                if (((blob[2] & 0x1) == 0) || (blob[3] == 0))
                {
                    *pdwFlags |= DACF_ALLOW_JIT_OPTS;
                }
                else
                {
                    *pdwFlags &= (~DACF_ALLOW_JIT_OPTS);
                }

                LOG((LF_CORDB, LL_INFO10, "Assembly %S: has %s=%d,%d bits = 0x%x\n", GetDebugName(),
                     DE_DEBUGGABLE_ATTRIBUTE_NAME,
                     blob[2], blob[3], *pdwFlags));
            }
        }
    }

    return hr;
}

BOOL DomainAssembly::NotifyDebuggerLoad(int flags, BOOL attaching)
{
    WRAPPER_NO_CONTRACT;

    BOOL result = FALSE;

    if (!IsVisibleToDebugger())
        return FALSE;

    // Debugger Attach is done totally out-of-process. Does not call code in-proc.
    _ASSERTE(!attaching);

    // Make sure the debugger has been initialized.  See code:Debugger::Startup.
    if (g_pDebugInterface == NULL)
    {
        _ASSERTE(!CORDebuggerAttached());
        return FALSE;
    }

    // There is still work we need to do even when no debugger is attached.

    if (flags & ATTACH_ASSEMBLY_LOAD)
    {
        if (ShouldNotifyDebugger())
        {
            g_pDebugInterface->LoadAssembly(this);
        }
        result = TRUE;
    }

    DomainModuleIterator i = IterateModules(kModIterIncludeLoading);
    while (i.Next())
    {
        DomainFile * pDomainFile = i.GetDomainFile();
        if(pDomainFile->ShouldNotifyDebugger())
        {
            result = result ||
                pDomainFile->GetModule()->NotifyDebuggerLoad(this->GetAppDomain(), pDomainFile, flags, attaching);
        }
    }
    if( ShouldNotifyDebugger())
    {
           result|=m_pModule->NotifyDebuggerLoad(m_pDomain, this, ATTACH_MODULE_LOAD, attaching);
           SetDebuggerNotified();
    }



    return result;
}

void DomainAssembly::NotifyDebuggerUnload()
{
    LIMITED_METHOD_CONTRACT;

    if (!IsVisibleToDebugger())
        return;

    if (!this->GetAppDomain()->IsDebuggerAttached())
        return;

    m_fDebuggerUnloadStarted = TRUE;

    // Dispatch module unloads for all modules. Debugger is resilient in case we haven't dispatched
    // a previous load event (such as if debugger attached after the modules was loaded).
    DomainModuleIterator i = IterateModules(kModIterIncludeLoading);
    while (i.Next())
    {
            i.GetDomainFile()->GetModule()->NotifyDebuggerUnload(this->GetAppDomain());
    }

    g_pDebugInterface->UnloadAssembly(this);

}

// This will enumerate for static GC refs (but not thread static GC refs)

void DomainAssembly::EnumStaticGCRefs(promote_func* fn, ScanContext* sc)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    _ASSERTE(GCHeap::IsGCInProgress() &&
         GCHeap::IsServerHeap()   &&
         IsGCSpecialThread());

    DomainModuleIterator i = IterateModules(kModIterIncludeLoaded);
    while (i.Next())
    {
        DomainFile* pDomainFile = i.GetDomainFile();

        if (pDomainFile->IsActive())
        {
            // We guarantee that at this point the module has it's DomainLocalModule set up
            // , as we create it while we load the module
            _ASSERTE(pDomainFile->GetLoadedModule()->GetDomainLocalModule(this->GetAppDomain()));
            pDomainFile->GetLoadedModule()->EnumRegularStaticGCRefs(this->GetAppDomain(), fn, sc);

            // We current to do not iterate over the ThreadLocalModules that correspond
            // to this Module. The GC discovers thread statics through the handle table.
        }
    }

    RETURN;
}



#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
//--------------------------------------------------------------------------------
// DomainModule
//--------------------------------------------------------------------------------

DomainModule::DomainModule(AppDomain *pDomain, DomainAssembly *pAssembly, PEFile *pFile)
  : DomainFile(pDomain, pFile),
    m_pDomainAssembly(pAssembly)
{
    STANDARD_VM_CONTRACT;
}

DomainModule::~DomainModule()
{
    WRAPPER_NO_CONTRACT;
}

void DomainModule::SetModule(Module* pModule)
{
    STANDARD_VM_CONTRACT;

    UpdatePEFile(pModule->GetFile());
    pModule->SetDomainFile(this);
    // SetDomainFile can throw and will unwind to DomainModule::Allocate at which
    // point pModule->Destruct will be called in the catch handler.  if we set
    // m_pModule = pModule before the call to SetDomainFile then we can end up with
    // a bad m_pModule pointer when SetDomainFile throws.  so we set m_pModule IIF
    // the call to SetDomainFile succeeds.
    m_pModule = pModule;
}

void DomainModule::Begin()
{
    STANDARD_VM_CONTRACT;
    m_pDomainAssembly->AddModule(this);
}

#ifdef FEATURE_PREJIT

void DomainModule::FindNativeImage()
{
    LIMITED_METHOD_CONTRACT;

    // Resource files are never prejitted.
}

#endif // FEATURE_PREJIT


void DomainModule::Allocate()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    // We can now rely on the fact that our MDImport will not change so we can stop refcounting it.
    GetFile()->MakeMDImportPersistent();

    AllocMemTracker amTracker;
    AllocMemTracker *pamTracker = &amTracker;

    Assembly *pAssembly = m_pDomainAssembly->GetCurrentAssembly();
    Module *pModule = NULL;

    if (pAssembly->IsDomainNeutral())
    {
        // For shared assemblies, the module may be already in the assembly list, even
        // though we haven't loaded it here yet.

        pModule = pAssembly->GetManifestModule()->GetModuleIfLoaded(GetToken(),FALSE, TRUE);
        if (pModule != NULL)
        {
            SetModule(pModule);
            return;
        }
        else
        {
#ifdef FEATURE_LOADER_OPTIMIZATION
            SharedDomain *pSharedDomain = SharedDomain::GetDomain();
            SharedFileLockHolder pFileLock(pSharedDomain, GetFile());
#else // FEATURE_LOADER_OPTIMIZATION
            _ASSERTE(IsSystem());
#endif // FEATURE_LOADER_OPTIMIZATION

            pModule = pAssembly->GetManifestModule()->GetModuleIfLoaded(GetToken(), FALSE, TRUE);
            if (pModule != NULL)
            {
                SetModule(pModule);
                return;
            }
            else
            {
                pModule = Module::Create(pAssembly, GetToken(), m_pFile, pamTracker);

                EX_TRY
                {
                    pAssembly->PrepareModuleForAssembly(pModule, pamTracker);
                    SetModule(pModule); //@todo: This innocent-looking call looks like a mixture of allocations and publishing code - it probably needs to be split.
                }
                EX_HOOK
                {
                    //! It's critical we destruct the manifest Module prior to the AllocMemTracker used to initialize it.
                    //! Otherwise, we will leave dangling pointers inside the Module that Module::Destruct will attempt
                    //! to dereference.
                    pModule->Destruct();
                }
                EX_END_HOOK

                {
                    CANNOTTHROWCOMPLUSEXCEPTION();
                    FAULT_FORBID();

                    //Cannot fail after this point.
                    pamTracker->SuppressRelease();
                    pModule->SetIsTenured();

                    pAssembly->PublishModuleIntoAssembly(pModule);



                    return;  // Explicit return to let you know you are NOT welcome to add code after the CANNOTTHROW/FAULT_FORBID expires
                }



            }
        }

    }
    else
    {
        pModule = Module::Create(pAssembly, GetToken(), m_pFile, pamTracker);
        EX_TRY
        {
            pAssembly->PrepareModuleForAssembly(pModule, pamTracker);
            SetModule(pModule); //@todo: This innocent-looking call looks like a mixture of allocations and publishing code - it probably needs to be split.
        }
        EX_HOOK
        {
            //! It's critical we destruct the manifest Module prior to the AllocMemTracker used to initialize it.
            //! Otherwise, we will leave dangling pointers inside the Module that Module::Destruct will attempt
            //! to dereference.
            pModule->Destruct();
        }
        EX_END_HOOK


        {
            CANNOTTHROWCOMPLUSEXCEPTION();
            FAULT_FORBID();

            //Cannot fail after this point.
            pamTracker->SuppressRelease();
            pModule->SetIsTenured();
            pAssembly->PublishModuleIntoAssembly(pModule);


            return;  // Explicit return to let you know you are NOT welcome to add code after the CANNOTTHROW/FAULT_FORBID expires
        }

    }


}



void DomainModule::DeliverAsyncEvents()
{
    LIMITED_METHOD_CONTRACT;
    return;
}

void DomainModule::DeliverSyncEvents()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        SO_INTOLERANT;
    }
    CONTRACTL_END;

    GetCurrentModule()->NotifyEtwLoadFinished(S_OK);

#ifdef PROFILING_SUPPORTED
    if (!IsProfilerNotified())
    {
        SetProfilerNotified();
        GetCurrentModule()->NotifyProfilerLoadFinished(S_OK);
    }
#endif

#ifdef DEBUGGING_SUPPORTED
    GCX_COOP();
    if(!IsDebuggerNotified())
    {
        SetShouldNotifyDebugger();
        {
            // Always give the module a chance to notify the debugger. If no debugger is attached, the
            // module can skip out on the notification.
            m_pModule->NotifyDebuggerLoad(m_pDomain, this, ATTACH_MODULE_LOAD, FALSE);
            SetDebuggerNotified();
        }
    }
#endif
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
DomainFile::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    //sizeof(DomainFile) == 0x60
    DAC_ENUM_VTHIS();

    // Modules are needed for all minidumps, but they are enumerated elsewhere
    // so we don't need to duplicate effort; thus we do noting with m_pModule.

    // For MiniDumpNormal, we only want the file name.
    if (m_pFile.IsValid())
    {
        m_pFile->EnumMemoryRegions(flags);
    }

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE
    && m_pDomain.IsValid())
    {
        m_pDomain->EnumMemoryRegions(flags, true);
    }
}

void
DomainAssembly::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    //sizeof(DomainAssembly) == 0xe0
    DAC_ENUM_VTHIS();
    DomainFile::EnumMemoryRegions(flags);

    // For minidumps without full memory, we need to always be able to iterate over m_Modules.
    m_Modules.EnumMemoryRegions(flags);

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        if (m_pAssembly.IsValid())
        {
            m_pAssembly->EnumMemoryRegions(flags);
        }
    }
}

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
void
DomainModule::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DomainFile::EnumMemoryRegions(flags);
    if (m_pDomainAssembly.IsValid())
    {
        m_pDomainAssembly->EnumMemoryRegions(flags);
    }
}
#endif // FEATURE_MULTIMODULE_ASSEMBLIES

#endif // #ifdef DACCESS_COMPILE

#if defined(FEATURE_MIXEDMODE) && !defined(CROSSGEN_COMPILE)
LPVOID DomainFile::GetUMThunk(LPVOID pManagedIp, PCCOR_SIGNATURE pSig, ULONG cSig)
{
    CONTRACT (LPVOID)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END


    if (m_pUMThunkHash == NULL)
    {
        UMThunkHash *pUMThunkHash = new UMThunkHash(GetModule(), this->GetAppDomain());
        if (FastInterlockCompareExchangePointer(&m_pUMThunkHash, pUMThunkHash, NULL) != NULL)
        {
            delete pUMThunkHash;
        }
    }
    RETURN m_pUMThunkHash->GetUMThunk(pManagedIp, pSig, cSig);
}
#endif // FEATURE_MIXEDMODE && !CROSSGEN_COMPILE
