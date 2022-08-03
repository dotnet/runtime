// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// DomainAssembly.cpp
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

#include "dllimportcallback.h"
#include "peimagelayout.inl"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif // FEATURE_PERFMAP

#ifndef DACCESS_COMPILE
DomainAssembly::DomainAssembly(AppDomain* pDomain, PEAssembly* pPEAssembly, LoaderAllocator* pLoaderAllocator) :
    m_pAssembly(NULL),
    m_pDomain(pDomain),
    m_pPEAssembly(pPEAssembly),
    m_pModule(NULL),
    m_fCollectible(pLoaderAllocator->IsCollectible()),
    m_NextDomainAssemblyInSameALC(NULL),
    m_pLoaderAllocator(pLoaderAllocator),
    m_level(FILE_LOAD_CREATE),
    m_loading(TRUE),
    m_hExposedModuleObject(NULL),
    m_hExposedAssemblyObject(NULL),
    m_pError(NULL),
    m_bDisableActivationCheck(FALSE),
    m_fHostAssemblyPublished(FALSE),
    m_pDynamicMethodTable(NULL),
    m_debuggerFlags(DACF_NONE),
    m_notifyflags(NOT_NOTIFIED),
    m_fDebuggerUnloadStarted(FALSE)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;             // ValidateForExecution
        GC_TRIGGERS;        // ValidateForExecution
        MODE_ANY;
    }
    CONTRACTL_END;

    pPEAssembly->AddRef();
    pPEAssembly->ValidateForExecution();

    SetupDebuggingConfig();
}

DomainAssembly::~DomainAssembly()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pPEAssembly->Release();
    if (m_pDynamicMethodTable)
        m_pDynamicMethodTable->Destroy();

    delete m_pError;

    if (m_fHostAssemblyPublished)
    {
        // Remove association first.
        UnregisterFromHostAssembly();
    }

    if (m_pAssembly != NULL)
    {
        delete m_pAssembly;
    }
}

// Optimization intended for EnsureLoadLevel only
#include <optsmallperfcritical.h>
void DomainAssembly::EnsureLoadLevel(FileLoadLevel targetLevel)
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
        this->GetAppDomain()->LoadDomainAssembly(this, targetLevel);

        // Enforce the loading requirement.  Note that we may have a deadlock in which case we
        // may be off by one which is OK.  (At this point if we are short of targetLevel we know
        // we have done so because of reentrancy constraints.)

        RequireLoadLevel((FileLoadLevel)(targetLevel-1));
    }
    else
        ThrowIfError(targetLevel);

    RETURN;
}
#include <optdefault.h>

CHECK DomainAssembly::CheckLoadLevel(FileLoadLevel requiredLevel, BOOL deadlockOK)
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
        // CheckLoading requires waiting on a host-breakable lock.
        // Since this is only a checked-build assert and we've been
        // living with it for a while, I'll leave it as is.
        //@TODO: CHECK statements are *NOT* debug-only!!!
        CONTRACT_VIOLATION(ThrowsViolation|GCViolation|TakesLockViolation);
        CHECK(this->GetAppDomain()->CheckLoading(this, requiredLevel));
    }
    else
    {
        CHECK_MSG(m_level >= requiredLevel,
                  "File not sufficiently loaded");
    }

    CHECK_OK;
}



void DomainAssembly::RequireLoadLevel(FileLoadLevel targetLevel)
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


void DomainAssembly::SetError(Exception *ex)
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

    if (m_pModule)
    {
        m_pModule->NotifyEtwLoadFinished(ex->GetHR());

        if (!IsProfilerNotified())
        {
            SetProfilerNotified();

#ifdef PROFILING_SUPPORTED
            // Only send errors for non-shared assemblies; other assemblies might be successfully completed
            // in another app domain later.
            m_pModule->NotifyProfilerLoadFinished(ex->GetHR());
#endif
        }
    }

    RETURN;
}

void DomainAssembly::ThrowIfError(FileLoadLevel targetLevel)
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

CHECK DomainAssembly::CheckNoError(FileLoadLevel targetLevel)
{
    LIMITED_METHOD_CONTRACT;
    CHECK(m_level >= targetLevel
          || !IsError());

    CHECK_OK;
}

CHECK DomainAssembly::CheckLoaded()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CHECK_MSG(CheckNoError(FILE_LOADED), "DomainAssembly load resulted in an error");

    if (IsLoaded())
        CHECK_OK;

    // CoreLib is allowed to run managed code much earlier than other
    // assemblies for bootstrapping purposes.  This is because it has no
    // dependencies, security checks, and doesn't rely on loader notifications.

    if (GetPEAssembly()->IsSystem())
        CHECK_OK;

    CHECK_MSG(GetPEAssembly()->IsLoaded(), "PEAssembly has not been loaded");

    CHECK_OK;
}

CHECK DomainAssembly::CheckActivated()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CHECK_MSG(CheckNoError(FILE_ACTIVE), "DomainAssembly load resulted in an error");

    if (IsActive())
        CHECK_OK;

    // CoreLib is allowed to run managed code much earlier than other
    // assemblies for bootstrapping purposes.  This is because it has no
    // dependencies, security checks, and doesn't rely on loader notifications.

    if (GetPEAssembly()->IsSystem())
        CHECK_OK;

    CHECK_MSG(GetPEAssembly()->IsLoaded(), "PEAssembly has not been loaded");
    CHECK_MSG(IsLoaded(), "DomainAssembly has not been fully loaded");
    CHECK_MSG(m_bDisableActivationCheck || CheckLoadLevel(FILE_ACTIVE), "File has not had execution verified");

    CHECK_OK;
}

#endif //!DACCESS_COMPILE

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

//---------------------------------------------------------------------------------------
//
// Returns managed representation of the module (Module or ModuleBuilder).
// Returns NULL if the managed scout was already collected (see code:LoaderAllocator#AssemblyPhases).
//
OBJECTREF DomainAssembly::GetExposedModuleObject()
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

        InterlockedCompareExchangeT(&m_hExposedModuleObject, handle, static_cast<LOADERHANDLE>(NULL));
    }

    if (pLoaderAllocator->GetHandleValue(m_hExposedModuleObject) == NULL)
    {
        REFLECTMODULEBASEREF refClass = NULL;

        // Will be TRUE only if LoaderAllocator managed object was already collected and therefore we should
        // return NULL
        BOOL fIsLoaderAllocatorCollected = FALSE;

        GCPROTECT_BEGIN(refClass);

        refClass = (REFLECTMODULEBASEREF) AllocateObject(CoreLibBinder::GetClass(CLASS__MODULE));
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
}

BOOL DomainAssembly::DoIncrementalLoad(FileLoadLevel level)
{
    STANDARD_VM_CONTRACT;

    if (IsError())
        return FALSE;

    Thread *pThread = GetThread();
    switch (level)
    {
    case FILE_LOAD_BEGIN:
        Begin();
        break;

    case FILE_LOAD_FIND_NATIVE_IMAGE:
        break;

    case FILE_LOAD_VERIFY_NATIVE_IMAGE_DEPENDENCIES:
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
        Module * pModule = m_pModule;

        if (pModule != NULL) // Should not triggle assert when module is NULL
        {
            this->GetAppDomain()->GetMulticoreJitManager().RecordModuleLoad(pModule, level);
        }
    }
#endif

    return TRUE;
}

void DomainAssembly::PreLoadLibrary()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

} // DomainAssembly::PreLoadLibrary

void DomainAssembly::LoadLibrary()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

}

void DomainAssembly::PostLoadLibrary()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        // Note that GetPEAssembly()->EnsureLoaded must be called before this OUTSIDE OF THE LOCKS
        PRECONDITION(GetPEAssembly()->IsLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

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
    if (!IsProfilerNotified())
    {
        SetProfilerNotified();
        GetModule()->NotifyProfilerLoadFinished(S_OK);
    }

#endif
}

void DomainAssembly::AddDependencies()
{
    STANDARD_VM_CONTRACT;
}

void DomainAssembly::EagerFixups()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_READYTORUN
    if (GetModule()->IsReadyToRun())
    {
        GetModule()->RunEagerFixups();

    }
#endif // FEATURE_READYTORUN
}

void DomainAssembly::VtableFixups()
{
    WRAPPER_NO_CONTRACT;

    GetModule()->FixupVTables();
}

void DomainAssembly::FinishLoad()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    // Must set this a bit prematurely for the DAC stuff to work
    m_level = FILE_LOADED;

    // Now the DAC can find this module by enumerating assemblies in a domain.
    DACNotify::DoModuleLoadNotification(m_pModule);

    // Set a bit to indicate that the module has been loaded in some domain, and therefore
    // typeloads can involve types from this module. (Used for candidate instantiations.)
    GetModule()->SetIsReadyForTypeLoad();

#ifdef FEATURE_PERFMAP
    // Notify the perfmap of the IL image load.
    PerfMap::LogImageLoad(m_pPEAssembly);
#endif
}

void DomainAssembly::Activate()
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(IsLoaded());
        STANDARD_VM_CHECK;
    }
    CONTRACT_END;

    // We cannot execute any code in this assembly until we know what exception plan it is on.
    // At the point of an exception's stack-crawl it is too late because we cannot tolerate a GC.
    // See PossiblyUnwrapThrowable and its callers.
    _ASSERTE(GetModule() == GetAssembly()->GetModule());
    GetModule()->IsRuntimeWrapExceptions();

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

#ifdef FEATURE_READYTORUN
    if (m_pModule->IsReadyToRun())
    {
        m_pModule->GetReadyToRunInfo()->RegisterUnrelatedR2RModule();
    }
#endif

    RETURN;
}

void DomainAssembly::SetAssembly(Assembly* pAssembly)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pAssembly->GetModule()->GetPEAssembly()==m_pPEAssembly);
    _ASSERTE(m_pAssembly == NULL);

    m_pAssembly = pAssembly;
    m_pModule = pAssembly->GetModule();

    pAssembly->SetDomainAssembly(this);
}


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

        InterlockedCompareExchangeT(&m_hExposedAssemblyObject, handle, static_cast<LOADERHANDLE>(NULL));
    }

    if (pLoaderAllocator->GetHandleValue(m_hExposedAssemblyObject) == NULL)
    {
        ASSEMBLYREF   assemblyObj = NULL;
        MethodTable * pMT;

        pMT = CoreLibBinder::GetClass(CLASS__ASSEMBLY);

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
}

void DomainAssembly::Begin()
{
    STANDARD_VM_CONTRACT;

    {
        AppDomain::LoadLockHolder lock(m_pDomain);
        m_pDomain->AddAssembly(this);
    }
    // Make it possible to find this DomainAssembly object from associated BINDER_SPACE::Assembly.
    RegisterWithHostAssembly();
    m_fHostAssemblyPublished = true;
}

void DomainAssembly::RegisterWithHostAssembly()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    if (GetPEAssembly()->HasHostAssembly())
    {
        GetPEAssembly()->GetHostAssembly()->SetDomainAssembly(this);
    }
}

void DomainAssembly::UnregisterFromHostAssembly()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END

    if (GetPEAssembly()->HasHostAssembly())
    {
        GetPEAssembly()->GetHostAssembly()->SetDomainAssembly(nullptr);
    }
}

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
        NewHolder<Assembly> assemblyHolder(NULL);

        assemblyHolder = pAssembly = Assembly::Create(m_pDomain, GetPEAssembly(), GetDebuggerInfoBits(), this->IsCollectible(), pamTracker, this->IsCollectible() ? this->GetLoaderAllocator() : NULL);
        assemblyHolder->SetIsTenured();

        pamTracker->SuppressRelease();
        assemblyHolder.SuppressRelease();
    }

    SetAssembly(pAssembly);
}

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

    GetModule()->NotifyEtwLoadFinished(S_OK);

#ifdef DEBUGGING_SUPPORTED
    GCX_COOP();
    if (!IsDebuggerNotified())
    {
        SetShouldNotifyDebugger();

        // Still work to do even if no debugger is attached.
        NotifyDebuggerLoad(ATTACH_ASSEMBLY_LOAD, FALSE);

    }
#endif // DEBUGGING_SUPPORTED
}

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

    return GetPEAssembly()->GetResource( szName,
                                   cbResource,
                                   pbInMemoryResource,
                                   pAssemblyRef,
                                   szFileName,
                                   dwLocation,
                                   fSkipRaiseResolveEvent,
                                   this,
                                   this->m_pDomain );
}


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

    {
        ULONG size;
        BYTE *blob;
        mdModule mdMod;
        IMDInternalImport* mdImport = GetPEAssembly()->GetMDImport();
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

    if(this->ShouldNotifyDebugger())
    {
        result = result ||
            this->GetModule()->NotifyDebuggerLoad(this->GetAppDomain(), this, flags, attaching);
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

    // Dispatch module unload for the module. Debugger is resilient in case we haven't dispatched
    // a previous load event (such as if debugger attached after the modules was loaded).
    this->GetModule()->NotifyDebuggerUnload(this->GetAppDomain());

    g_pDebugInterface->UnloadAssembly(this);
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void DomainAssembly::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;

    DAC_ENUM_DTHIS();

    // Modules are needed for all minidumps, but they are enumerated elsewhere
    // so we don't need to duplicate effort; thus we do noting with m_pModule.

    // For MiniDumpNormal, we only want the file name.
    if (m_pPEAssembly.IsValid())
    {
        m_pPEAssembly->EnumMemoryRegions(flags);
    }

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE
    && m_pDomain.IsValid())
    {
        m_pDomain->EnumMemoryRegions(flags, true);
    }

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        if (m_pAssembly.IsValid())
        {
            m_pAssembly->EnumMemoryRegions(flags);
        }
    }
}

#endif // #ifdef DACCESS_COMPILE
