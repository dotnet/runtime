// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: ProfilingEnumerators.cpp
//
// All enumerators returned by the profiling API to enumerate objects or to catch up on
// the current CLR state (usually for attaching profilers) are defined in
// ProfilingEnumerators.h,cpp.
//
// This cpp file contains implementations specific to the derived enumerator classes, as
// well as helpers for iterating over AppDomains, assemblies, modules, etc., that have
// been loaded enough that they may be made visible to profilers.
//

//

#include "common.h"
#include "frozenobjectheap.h"

#ifdef PROFILING_SUPPORTED

#include "proftoeeinterfaceimpl.h"
#include "profilingenumerators.h"

// ---------------------------------------------------------------------------------------
//  ProfilerFunctionEnum/ICorProfilerFunctionEnum implementation
// ---------------------------------------------------------------------------------------

BOOL ProfilerFunctionEnum::Init(BOOL fWithReJITIDs)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        // If we needs to get rejit ID, which requires a lock (which, in turn may switch us to
        // preemptive mode).
        if (fWithReJITIDs) GC_TRIGGERS; else GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Depending on our GC mode, the jit manager may have to take a
        // reader lock to prevent things from changing while reading...
        CAN_TAKE_LOCK;

    } CONTRACTL_END;

    EEJitManager::CodeHeapIterator heapIterator;
    while(heapIterator.Next())
    {
        MethodDesc *pMD = heapIterator.GetMethod();

        // On AMD64 JumpStub is used to call functions that is 2GB away.  JumpStubs have a CodeHeader
        // with NULL MethodDesc, are stored in code heap and are reported by EEJitManager::EnumCode.
        if (pMD == NULL)
            continue;

        // There are two possible reasons to skip this MD.
        //
        // 1) If it has no metadata (i.e., LCG / IL stubs), then skip it
        //
        // 2) If it has no code compiled yet for it, then skip it.
        //
        if (pMD->IsNoMetadata() || !pMD->HasNativeCode())
        {
            continue;
        }

        COR_PRF_FUNCTION * element = m_elements.Append();
        if (element == NULL)
        {
            return FALSE;
        }
        element->functionId = (FunctionID) pMD;

        if (fWithReJITIDs)
        {
            // This causes triggering and locking, while the non-rejitid case does not.
            element->reJitId = ReJitManager::GetReJitId(pMD, heapIterator.GetMethodCode());
        }
        else
        {
            element->reJitId = 0;
        }
    }

    return TRUE;
}

// ---------------------------------------------------------------------------------------
//  ProfilerObjectEnum/ICorProfilerObjectEnum implementation
// ---------------------------------------------------------------------------------------

BOOL ProfilerObjectEnum::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    FrozenObjectHeapManager* foh = SystemDomain::GetFrozenObjectHeapManager();
    CrstHolder ch(&foh->m_Crst);

    const unsigned segmentsCount = foh->m_FrozenSegments.GetCount();
    FrozenObjectSegment** segments = foh->m_FrozenSegments.GetElements();
    if (segments != nullptr)
    {
        for (unsigned segmentIdx = 0; segmentIdx < segmentsCount; segmentIdx++)
        {
            const FrozenObjectSegment* segment = segments[segmentIdx];

            Object* currentObj = segment->GetFirstObject();
            while (currentObj != nullptr)
            {
                *m_elements.Append() = reinterpret_cast<size_t>(currentObj);
                currentObj = segment->GetNextObject(currentObj);
            }
        }
    }
    return TRUE;
}

// ---------------------------------------------------------------------------------------
// Catch-up helpers
//
// #ProfilerEnumGeneral
//
// The following functions factor out the iteration code to ensure we only consider
// AppDomains, assemblies, modules, etc., that the profiler can safely query about. The
// parameters to these functions are of types that may have confusing syntax, but all
// that's going on is that the caller may supply an object instance and a member function
// on that object (non-static) to be called for each iterated item. This is just a
// statically-typed way of doing the usual pattern of providing a function pointer for
// the callback plus a void * context object to pass to the function. If the
// caller-supplied callback returns anything other than S_OK, the iteration code will
// stop iterating, and immediately propagate the callback's return value to the original
// caller. Start looking at code:ProfilerModuleEnum::Init for an example of how these
// helpers get used.
//
// The reason we have helpers to begin with is so we can centralize the logic that
// enforces the following rather subtle invariants:
//
//     * Provide enough entities that the profiler gets a complete set of entities from
//         the union of catch-up enumeration and "callbacks" (e.g., ModuleLoadFinished).
//     * Exclude entities that have unloaded to the point where it's no longer safe to
//         query information about them.
//
// The catch-up spec summarizes this via the following timeline for any given entity:
//
// Entity available in catch-up enumeration
//     < Entity's LoadFinished (or equivalent) callback is issued
//     < Entity NOT available from catch-up enumeration
//     < Entity's UnloadStarted (or equivalent) callback is issued
//
// These helpers avoid duplicate code in the ProfilerModuleEnum implementation, and will
// also help avoid future duplicate code should we decide to provide more catch-up
// enumerations for attaching profilers to find currently loaded AppDomains, Classes,
// etc.
//
// Note: The debugging API has similar requirements around which entities at which stage
// of loading are permitted to be enumerated over. See code:IDacDbiInterface#Enumeration
// for debugger details. Note that profapi's needs are not exactly the same. For example,
// Assemblies appear in the debugging API enumerations as soon as they begin to load,
// whereas Assemblies (like all other entities) appear in the profiling API enumerations
// once their load is complete (i.e., just before AssemblyLoadFinished).  Also,
// debuggers enumerate DomainModules and DomainAssemblies, whereas profilers enumerate
// Modules and Assemblies.
//
// For information about other synchronization issues with profiler catch-up, see
// code:ProfilingAPIUtility::LoadProfiler#ProfCatchUpSynchronization
//
// ---------------------------------------------------------------------------------------


//---------------------------------------------------------------------------------------
//
// Iterates through exactly those AppDomains that should be visible to the profiler, and
// calls a caller-supplied function to operate on each iterated AppDomain
//
// Arguments:
//    * callbackObj - Caller-supplied object containing the callback method to call for
//        each AppDomain
//    * callbackMethod - Caller-supplied method to call for each AppDomain. If this
//        method returns anything other than S_OK, then the iteration is aborted, and
//        callbackMethod's return value is returned to our caller.
//

template<typename CallbackObject>
HRESULT IterateAppDomains(CallbackObject * callbackObj,
                          HRESULT (CallbackObject:: * callbackMethod)(AppDomain *))
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        // (See comments in code:ProfToEEInterfaceImpl::EnumModules for info about contracts.)
    }
    CONTRACTL_END;

    // #ProfilerEnumAppDomains (See also code:#ProfilerEnumGeneral)
    //
    // When enumerating AppDomains, ensure this timeline:
    // AD available in catch-up enumeration
    //     < AppDomainCreationFinished issued
    //     < AD NOT available from catch-up enumeration
    //
    //     * AppDomainCreationFinished (with S_OK hrStatus) is issued once the AppDomain
    //         reaches STAGE_ACTIVE.
    AppDomain * pAppDomain = ::GetAppDomain();
    if (pAppDomain->IsActive())
    {

        // Of course, the AD could start unloading here, but if it does we're guaranteed
        // the profiler has had a chance to see the Unload callback for the AD, and thus
        // the profiler can block in that callback until it's done with the enumerator
        // we provide.

        // Call user-supplied callback, and cancel iteration if requested
        HRESULT hr = (callbackObj->*callbackMethod)(pAppDomain);
        if (hr != S_OK)
        {
            return hr;
        }
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Iterates through exactly those Modules that should be visible to the profiler, and
// calls a caller-supplied function to operate on each iterated Module.  Any module that
// is loaded domain-neutral is skipped.
//
// Arguments:
//    * pAppDomain - Only unshared modules loaded into this AppDomain will be iterated
//    * callbackObj - Caller-supplied object containing the callback method to call for
//        each Module
//    * callbackMethod - Caller-supplied method to call for each Module. If this
//        method returns anything other than S_OK, then the iteration is aborted, and
//        callbackMethod's return value is returned to our caller.
//
// Notes:
//     * In theory, this could be broken down into an unshared assembly iterator that
//         takes a callback, and an unshared module iterator (based on an input
//         assembly) that takes a callback.  But that kind of granularity is unnecessary
//         now, and probably not useful in the future.  If that turns out to be wrong,
//         this can still be broken down that way later on.
//

template<typename CallbackObject>
HRESULT IterateUnsharedModules(AppDomain * pAppDomain,
                               CallbackObject * callbackObj,
                               HRESULT (CallbackObject:: * callbackMethod)(Module *))
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // #ProfilerEnumAssemblies (See also code:#ProfilerEnumGeneral)
    //
    // When enumerating assemblies, ensure this timeline:
    // Assembly available in catch-up enumeration
    //     < AssemblyLoadFinished issued
    //     < Assembly NOT available from catch-up enumeration
    //     < AssemblyUnloadStarted issued
    //
    // The IterateAssembliesEx parameter below ensures we will only include assemblies at
    // load level >= FILE_LOAD_LOADLIBRARY.
    //     * AssemblyLoadFinished is issued once the Assembly reaches
    //         code:FILE_LOAD_LOADLIBRARY
    //     * AssemblyUnloadStarted is issued as a result of either:
    //         * Collectible assemblies unloading. Such assemblies will no longer be
    //             enumerable.
    //
    // Note: To determine what happens in a given load stage of a module or assembly,
    // look at the switch statement in code:DomainAssembly::DoIncrementalLoad, and keep in
    // mind that it takes cases on the *next* load stage; in other words, the actions
    // that appear in a case for a given load stage are actually executed as we attempt
    // to transition TO that load stage, and thus they actually execute while the module
    // / assembly is still in the previous load stage.
    //
    // Note that the CLR may issue ModuleLoadFinished / AssemblyLoadFinished later, at
    // FILE_LOAD_EAGER_FIXUPS stage, if for some reason MLF/ALF hadn't been issued
    // earlier during FILE_LOAD_LOADLIBRARY. This does not affect the timeline, as either
    // way the profiler receives the notification AFTER the assembly would appear in the
    // enumeration.
    //
    // Although it's called an "AssemblyIterator", it actually iterates over
    // DomainAssembly instances.
    AppDomain::AssemblyIterator domainAssemblyIterator =
        pAppDomain->IterateAssembliesEx(
            (AssemblyIterationFlags) (kIncludeAvailableToProfilers | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

    while (domainAssemblyIterator.Next(pDomainAssembly.This()))
    {
        _ASSERTE(pDomainAssembly != NULL);
        _ASSERTE(pDomainAssembly->GetAssembly() != NULL);

        // #ProfilerEnumModules (See also code:#ProfilerEnumGeneral)
        //
        // When enumerating modules, ensure this timeline:
        // Module available in catch-up enumeration
        //     < ModuleLoadFinished issued
        //     < Module NOT available from catch-up enumeration
        //     < ModuleUnloadStarted issued
        //
        // Details for module callbacks are the same as those for assemblies, so see
        // code:#ProfilerEnumAssemblies for info on how the timing works.

        // Call user-supplied callback, and cancel iteration if requested
        HRESULT hr = (callbackObj->*callbackMethod)(pDomainAssembly->GetModule());
        if (hr != S_OK)
        {
            return hr;
        }
    }

    return S_OK;
}

//---------------------------------------------------------------------------------------
// ProfilerModuleEnum implementation
//---------------------------------------------------------------------------------------


//---------------------------------------------------------------------------------------
//
// Callback passed to IterateAppDomains, that takes the currently iterated AppDomain,
// and then iterates through the unshared modules loaded into that AD.  See
// code:ProfilerModuleEnum::Init for how this gets used.
//
// Arguments:
//      * pAppDomain - Current AppDomain being iterated.
//
// Return Value:
//      * S_OK = the iterator should continue after we return.
//      * S_FALSE = we verified m_pModule is loaded into this AppDomain, so no need
//          for the iterator to continue with the next AppDomain
//      * error indicating a failure
//

HRESULT ProfilerModuleEnum::AddUnsharedModulesFromAppDomain(AppDomain * pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    return IterateUnsharedModules<ProfilerModuleEnum>(
        pAppDomain,
        this,
        &ProfilerModuleEnum::AddUnsharedModule);
}


//---------------------------------------------------------------------------------------
//
// Callback passed to IterateUnsharedModules, that takes the currently iterated unshared
// Module, and adds it to the enumerator. See code:ProfilerModuleEnum::Init for how this
// gets used.
//
// Arguments:
//      * pModule - Current Module being iterated.
//
// Return Value:
//      * S_OK = the iterator should continue after we return.
//      * error indicating a failure
//
HRESULT ProfilerModuleEnum::AddUnsharedModule(Module * pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    ModuleID * pElement = m_elements.Append();
    if (pElement == NULL)
    {
        return E_OUTOFMEMORY;
    }
    *pElement = (ModuleID) pModule;
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Populate the module enumerator that's about to be given to the profiler. This is
// called from the ICorProfilerInfo3::EnumModules implementation.
//
// This code controls how the above iterator helpers and callbacks are used, so you might
// want to look here first to understand how how the helpers and callbacks are used.
//
// Return Value:
//     HRESULT indicating success or failure.
//
HRESULT ProfilerModuleEnum::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        // (See comments in code:ProfToEEInterfaceImpl::EnumModules for info about contracts.)

    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // When an assembly is loaded into an AppDomain, a DomainAssembly is
    // created (one per pairing of the AppDomain with the assembly). This means
    // that we'll create multiple DomainAssemblys for the same module if it is loaded
    // domain-neutral (i.e., "shared"). The profiling API callbacks shield the profiler
    // from this, and only report a given module the first time it's loaded. So a
    // profiler sees only one ModuleLoadFinished for a module loaded domain-neutral, even
    // though the module may be used by multiple AppDomains. The module enumerator must
    // mirror the behavior of the profiling API callbacks, by avoiding duplicate Modules
    // in the module list we return to the profiler. So first add unshared modules (non
    // domain-neutral) to the enumerator, and then separately add any shared modules that
    // were loaded into at least one AD.

    // First, iterate through all ADs. For each one, call
    // AddUnsharedModulesFromAppDomain, which iterates through all UNSHARED modules and
    // adds them to the enumerator.
    hr = IterateAppDomains<ProfilerModuleEnum>(
        this,
        &ProfilerModuleEnum::AddUnsharedModulesFromAppDomain);
    if (FAILED(hr))
    {
        return hr;
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Callback passed to IterateAppDomains, that takes the currently iterated AppDomain,
// and adds it to the enumerator if it has loaded the given module. See
// code:IterateAppDomainContainingModule::PopulateArray for how this gets used.
//
// Arguments:
//      * pAppDomain - Current AppDomain being iterated.
//
// Return Value:
//      * S_OK = the iterator should continue after we return.
//      * error indicating a failure
//
HRESULT IterateAppDomainContainingModule::AddAppDomainContainingModule(AppDomain * pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        // This method iterates over AppDomains, which adds, then releases, a reference on
        // each AppDomain iterated.  This causes locking, and can cause triggering if the
        // AppDomain gets destroyed as a result of the release. (See code:AppDomainIterator::Next
        // and its call to code:AppDomain::Release.)
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    DomainAssembly * pDomainAssembly = m_pModule->GetDomainAssembly();
    if ((pDomainAssembly != NULL) && (pDomainAssembly->IsAvailableToProfilers()))
    {
        if (m_index < m_cAppDomainIds)
        {
            m_rgAppDomainIds[m_index] = reinterpret_cast<AppDomainID>(pAppDomain);
        }

        m_index++;
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Populate the array with AppDomains in which the given module has been loaded
//
// Return Value:
//     HRESULT indicating success or failure.
//
HRESULT IterateAppDomainContainingModule::PopulateArray()
{
    CONTRACTL
    {
        NOTHROW;
        // This method iterates over AppDomains, which adds, then releases, a reference on
        // each AppDomain iterated.  This causes locking, and can cause triggering if the
        // AppDomain gets destroyed as a result of the release. (See code:AppDomainIterator::Next
        // and its call to code:AppDomain::Release.)
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    HRESULT hr = IterateAppDomains<IterateAppDomainContainingModule>(
        this,
        &IterateAppDomainContainingModule::AddAppDomainContainingModule);

    *m_pcAppDomainIds = m_index;

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Populate the thread enumerator that's about to be given to the profiler. This is
// called from the ICorProfilerInfo4::EnumThread implementation.
//
// Return Value:
//     HRESULT indicating success or failure.
//
HRESULT ProfilerThreadEnum::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    // If a profiler has requested that the runtime suspend to do stack snapshots, it
    // will be holding the ThreadStore lock already
    ThreadStoreLockHolder tsLock(!g_profControlBlock.fProfilerRequestedRuntimeSuspend);

    Thread * pThread = NULL;

    //
    // Walk through all the threads with the lock taken
    // Because the thread enumeration status need to change before the ThreadCreated/ThreadDestroyed
    // callback, we need to:
    // 1. Include Thread::TS_FullyInitialized threads for ThreadCreated
    // 2. Exclude Thread::TS_Dead | Thread::TS_ReportDead for ThreadDestroyed
    //
    while((pThread = ThreadStore::GetAllThreadList(
        pThread,
        Thread::TS_Dead | Thread::TS_ReportDead | Thread::TS_FullyInitialized,
        Thread::TS_FullyInitialized
        )))
    {
        if (pThread->IsGCSpecial())
            continue;

        *m_elements.Append() = (ThreadID) pThread;
    }

    _ASSERTE(ThreadStore::HoldingThreadStore() || g_profControlBlock.fProfilerRequestedRuntimeSuspend);
    return S_OK;
}


#endif // PROFILING_SUPPORTED
