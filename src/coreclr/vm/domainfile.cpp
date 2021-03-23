// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// DomainFile.cpp
//

// --------------------------------------------------------------------------------


#include "common.h"

// --------------------------------------------------------------------------------
// Headers
// --------------------------------------------------------------------------------

#include <shlwapi.h>

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

#include "dllimportcallback.h"
#include "peimagelayout.inl"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif // FEATURE_PERFMAP

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
    delete m_pError;
}

#endif //!DACCESS_COMPILE

LoaderAllocator * DomainFile::GetLoaderAllocator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
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
        if (GetCurrentModule() != NULL)
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

    // CoreLib is allowed to run managed code much earlier than other
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

    // CoreLib is allowed to run managed code much earlier than other
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
    }
    CONTRACTL_END;

    _ASSERTE(IsAssembly());
    return (DomainAssembly *) this;
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

    return (GetAssembly() != NULL);
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
            refClass = (REFLECTMODULEBASEREF) AllocateObject(CoreLibBinder::GetClass(CLASS__MODULE_BUILDER));
        }
        else
        {
            refClass = (REFLECTMODULEBASEREF) AllocateObject(CoreLibBinder::GetClass(CLASS__MODULE));
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

    case FILE_LOAD_DELIVER_EVENTS:
        DeliverSyncEvents();
        break;

    case FILE_LOAD_VTABLE_FIXUPS:
        VtableFixups();
        break;

    case FILE_LOADED:
        FinishLoad();
        break;

    case FILE_ACTIVE:
        Activate();
        break;

    default:
        UNREACHABLE();
    }

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


        //
        // CoreCLR hard binds to CoreLib only. Avoid going through the full load.
        //

#ifdef _DEBUG
        AssemblySpec name;
        name.InitializeSpec(pDependency->dwAssemblyRef,
                            ((pManifestNativeImage != NULL) ? pManifestNativeImage : pNativeImage)->GetNativeMDImport(),
                            GetDomainAssembly());
        _ASSERTE(name.IsCoreLib());
#endif

        PEAssembly * pDependencyFile = SystemDomain::SystemFile();


        ReleaseHolder<PEImage> pDependencyNativeImage = pDependencyFile->GetNativeImageWithRef();
        if (pDependencyNativeImage == NULL)
        {
            ExternalLog(LL_ERROR, W("Rejecting native image because dependency %s is not native"),
                        pDependencyFile->GetPath().GetUnicode());
            m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_DependencyNotNative;
            STRESS_LOG3(LF_ZAP,LL_INFO100,"Rejecting native file %p, because dependency %p is not NI - reason 0x%x\n",pNativeImage.GetValue(),pDependencyFile,m_dwReasonForRejectingNativeImage);
            goto NativeImageRejected;
        }

        PTR_PEImageLayout pDependencyNativeLayout = pDependencyNativeImage->GetLoadedLayout();
        // Assert that the native image signature is as expected
        // Fusion will ensure this
        CORCOMPILE_VERSION_INFO * pDependencyNativeVersion =
                pDependencyNativeLayout->GetNativeVersionInfo();

        if (!RuntimeVerifyNativeImageDependency(pDependency, pDependencyNativeVersion, pDependencyFile))
            goto NativeImageRejected;
    }
    LOG((LF_ZAP, LL_INFO100, "ZAP: Native image dependencies for %S OK.\n",
            pNativeImage->GetPath().GetUnicode()));

    return;
}

NativeImageRejected:
    m_pFile->ClearNativeImage();

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

#if defined(_DEBUG)
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
#endif // defined(_DEBUG)

    // Does this look like a resource-only assembly?  We assume an assembly is resource-only
    // if it contains no TypeDef (other than the <Module> TypeDef) and no MethodDef.
    // Note that pMD->GetCountWithTokenKind(mdtTypeDef) doesn't count the <Module> type.
    IMDInternalImportHolder pMD = m_pFile->GetMDImport();
    if (pMD->GetCountWithTokenKind(mdtTypeDef) == 0 && pMD->GetCountWithTokenKind(mdtMethodDef) == 0)
        return FALSE;

    DomainAssembly * pDomainAssembly = GetDomainAssembly();

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
            // CoreLib gets loaded before the CompilationDomain gets created.
            // However, we may be ngening CoreLib itself
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
    ss.Printf("ZapRequire: Could not get native image for %s.\n",
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
}

void DomainFile::EagerFixups()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_PREJIT
    if (GetCurrentModule()->HasNativeImage())
    {
        GetCurrentModule()->RunEagerFixups();
    }
    else
#endif // FEATURE_PREJIT
#ifdef FEATURE_READYTORUN
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
}

void DomainFile::VtableFixups()
{
    WRAPPER_NO_CONTRACT;

#if !defined(CROSSGEN_COMPILE)
    if (!GetCurrentModule()->IsResource())
        GetCurrentModule()->FixupVTables();
#endif // !CROSSGEN_COMPILE
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

        LOG((LF_ZAP, LL_INFO10, "Using native image %S.\n", m_pFile->GetPersistentNativeImage()->GetPath().GetUnicode()));
        ExternalLog(LL_INFO10, "Native image successfully used.");

        // Inform metadata that it has been loaded from a native image
        // (and so there was an opportunity to check for or fix inconsistencies in the original IL metadata)
        m_pFile->GetMDImport()->SetVerifiedByTrustedSource(TRUE);
    }

    // Are we absolutely required to use a native image?
    CheckZapRequired();
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

#ifndef CROSSGEN_COMPILE

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
#ifdef _DEBUG
    if (g_pConfig->ExpandModulesOnLoad())
    {
        m_pModule->ExpandAll();
    }
#endif //_DEBUG

#endif // CROSSGEN_COMPILE

    RETURN;
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

DomainAssembly::DomainAssembly(AppDomain *pDomain, PEFile *pFile, LoaderAllocator *pLoaderAllocator)
  : DomainFile(pDomain, pFile),
    m_pAssembly(NULL),
    m_debuggerFlags(DACF_NONE),
    m_fDebuggerUnloadStarted(FALSE),
    m_fCollectible(pLoaderAllocator->IsCollectible()),
    m_fHostAssemblyPublished(false),
    m_pLoaderAllocator(pLoaderAllocator),
    m_NextDomainAssemblyInSameALC(NULL)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    pFile->ValidateForExecution();

    // !!! backout

    m_hExposedAssemblyObject = NULL;

    SetupDebuggingConfig();

    // Add a Module iterator entry for this assembly.
    IfFailThrow(m_Modules.Append(this));
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

    if (m_pAssembly != NULL)
    {
        delete m_pAssembly;
    }
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
            pMT = CoreLibBinder::GetClass(CLASS__INTERNAL_ASSEMBLY_BUILDER);
        }
        else
        {
            OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);
            pMT = CoreLibBinder::GetClass(CLASS__ASSEMBLY);
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

    ClearNativeImageStress();

    // We already have an image - we just need to do a few more checks

    if (GetFile()->HasNativeImage())
    {
#if defined(_DEBUG)
        if (g_pConfig->ForbidZap(GetSimpleName()))
        {
            SString sbuf;
            StackScratchBuffer scratch;
            sbuf.Printf("COMPlus_NgenBind_ZapForbid violation: %s.", GetSimpleName());
            DbgAssertDialog(__FILE__, __LINE__, sbuf.GetUTF8(scratch));
        }
#endif

        ReleaseHolder<PEImage> pNativeImage = GetFile()->GetNativeImageWithRef();

        if (!CheckZapDependencyIdentities(pNativeImage))
        {
            m_dwReasonForRejectingNativeImage = ReasonForRejectingNativeImage_DependencyIdentityMismatch;
            STRESS_LOG2(LF_ZAP,LL_INFO100,"Rejecting native file %p, because dependency identity mismatch - reason 0x%x\n",pNativeImage.GetValue(),m_dwReasonForRejectingNativeImage);
            ExternalLog(LL_ERROR, "Rejecting native image because of identity mismatch "
                "with one or more of its assembly dependencies. The assembly needs "
                "to be ngenned again");

            GetFile()->ClearNativeImage();

            // Always throw exceptions when we throw the NI out
            ThrowHR(CLR_E_BIND_NI_DEP_IDENTITY_MISMATCH);
        }
        else
        {
            Module *  pNativeModule = pNativeImage->GetLoadedLayout()->GetPersistedModuleImage();
            PEFile ** ppNativeFile = (PEFile **) (PBYTE(pNativeModule) + Module::GetFileOffset());

            PEAssembly * pFile = (PEAssembly *)FastInterlockCompareExchangePointer((void **)ppNativeFile, (void *)GetFile(), (void *)NULL);
            STRESS_LOG3(LF_ZAP,LL_INFO100,"Attempted to set  new native file %p, old file was %p, location in the image=%p\n",GetFile(),pFile,ppNativeFile);
            if (pFile!=NULL)
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
                GetFile()->ClearNativeImage();
            }
            else
            {
                GetFile()->AddRef();

                LOG((LF_ZAP, LL_INFO100, "ZAP: Found a candidate native image for %s\n", GetSimpleName()));
            }
        }
    }

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

    CheckZapRequired();
}
#endif // FEATURE_PREJIT

void DomainAssembly::Allocate()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    AllocMemTracker   amTracker;
    AllocMemTracker * pamTracker = &amTracker;

    Assembly * pAssembly = m_pAssembly;

    if (pAssembly==NULL)
    {
        //! If you decide to remove "if" do not remove this brace: order is important here - in the case of an exception,
        //! the Assembly holder must destruct before the AllocMemTracker declared above.

        // We can now rely on the fact that our MDImport will not change so we can stop refcounting it.
        GetFile()->MakeMDImportPersistent();

        NewHolder<Assembly> assemblyHolder(NULL);

        assemblyHolder = pAssembly = Assembly::Create(m_pDomain, GetFile(), GetDebuggerInfoBits(), this->IsCollectible(), pamTracker, this->IsCollectible() ? this->GetLoaderAllocator() : NULL);
        assemblyHolder->SetIsTenured();

        //@todo! This is too early to be calling SuppressRelease. The right place to call it is below after
        // the CANNOTTHROWCOMPLUSEXCEPTION. Right now, we have to do this to unblock OOM injection testing quickly
        // as doing the right thing is nontrivial.
        pamTracker->SuppressRelease();
        assemblyHolder.SuppressRelease();
    }

    SetAssembly(pAssembly);

#ifdef FEATURE_PREJIT
    BOOL fInsertIntoAssemblySpecBindingCache = TRUE;

    // Insert AssemblyDef details into AssemblySpecBindingCache if appropriate


    fInsertIntoAssemblySpecBindingCache = fInsertIntoAssemblySpecBindingCache && GetFile()->CanUseWithBindingCache();

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
                                   fSkipRaiseResolveEvent,
                                   this,
                                   this->m_pDomain );
}


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

    // Do not store runtime timestamps into NGen image for cross-platform NGen determinism
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

#ifdef TARGET_X86

    static CORINFO_CPU ngenCpuInfo =
        {
            (CPU_X86_PENTIUM_PRO << 8), // dwCPUType
            0x00000000,                 // dwFeatures
            0                           // dwExtendedFeatures
        };

    // We always generate P3-compatible code on CoreCLR
    *cpuInfo = ngenCpuInfo;

#else // TARGET_X86
    cpuInfo->dwCPUType = 0;
    cpuInfo->dwFeatures = 0;
    cpuInfo->dwExtendedFeatures = 0;
#endif // TARGET_X86
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

#ifndef TARGET_UNIX
    pNativeVersionInfo->wOSPlatformID = VER_PLATFORM_WIN32_NT;
#else
    pNativeVersionInfo->wOSPlatformID = VER_PLATFORM_UNIX;
#endif

    // The native images should be OS-version agnostic. Do not store the actual OS version for determinism.
    // pNativeVersionInfo->wOSMajorVersion = (WORD) osInfo.dwMajorVersion;
    pNativeVersionInfo->wOSMajorVersion = 4;

    pNativeVersionInfo->wMachine = IMAGE_FILE_MACHINE_NATIVE_NI;

    pNativeVersionInfo->wVersionMajor = RuntimeFileMajorVersion;
    pNativeVersionInfo->wVersionMinor = RuntimeFileMinorVersion;
    pNativeVersionInfo->wVersionBuildNumber = RuntimeFileBuildVersion;
    pNativeVersionInfo->wVersionPrivateBuildNumber = RuntimeFileRevisionVersion;

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

    // Use the NGen image if possible. IL image does not even have to be present on CoreCLR.
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

    AssemblySpec spec;
    spec.InitializeSpec(this->GetFile());

    // The assembly spec should have the binding context associated with it
    _ASSERTE(spec.GetBindingContext()  || spec.IsAssemblySpecForCoreLib());

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

            if (!name.IsAssemblySpecForCoreLib())
            {
                // We just initialized the assembly spec for the NI dependency. This will not have binding context
                // associated with it, so set it from that of the parent.
                _ASSERTE(!name.GetBindingContext());
                ICLRPrivBinder *pParentAssemblyBindingContext = name.GetBindingContextFromParentAssembly(name.GetAppDomain());
                _ASSERTE(pParentAssemblyBindingContext);
                name.SetBindingContext(pParentAssemblyBindingContext);
            }

            GetAppDomain()->CheckForMismatchedNativeImages(&name, &pDependencies->signAssemblyDef.mvid);
        }

        pDependencies++;
    }

    return TRUE;
}
#endif // FEATURE_PREJIT

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
    IfFailThrow(GetDebuggingCustomAttributes(&dacfFlags));
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
                                                DEBUGGABLE_ATTRIBUTE_TYPE,
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
                     DEBUGGABLE_ATTRIBUTE_TYPE_NAME,
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

#endif // #ifdef DACCESS_COMPILE
