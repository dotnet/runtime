// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "stringliteralmap.h"
#include "virtualcallstub.h"
#include "threadsuspend.h"
#include "castcache.h"
#include "mlinfo.h"
#ifndef DACCESS_COMPILE
#include "comdelegate.h"
#endif
#include "comcallablewrapper.h"

#define STUBMANAGER_RANGELIST(stubManager) (stubManager::g_pManager->GetRangeList())

UINT64 LoaderAllocator::cLoaderAllocatorsCreated = 1;

LoaderAllocator::LoaderAllocator(bool collectible) : 
    m_stubPrecodeRangeList(STUB_CODE_BLOCK_STUBPRECODE, collectible),
    m_fixupPrecodeRangeList(STUB_CODE_BLOCK_FIXUPPRECODE, collectible)
{
    LIMITED_METHOD_CONTRACT;

    // initialize all members up front to NULL so that short-circuit failure won't cause invalid values
    m_InitialReservedMemForLoaderHeaps = NULL;
    m_pLowFrequencyHeap = NULL;
    m_pHighFrequencyHeap = NULL;
    m_pStubHeap = NULL;
    m_pPrecodeHeap = NULL;
    m_pExecutableHeap = NULL;
#ifdef FEATURE_READYTORUN
    m_pDynamicHelpersHeap = NULL;
#endif
    m_pFuncPtrStubs = NULL;
    m_hLoaderAllocatorObjectHandle = NULL;
    m_pStringLiteralMap = NULL;

    m_cReferences = (UINT32)-1;

    m_pFirstDomainAssemblyFromSameALCToDelete = NULL;

#ifdef FAT_DISPATCH_TOKENS
    // DispatchTokenFat pointer table for token overflow scenarios. Lazily allocated.
    m_pFatTokenSetLock = NULL;
    m_pFatTokenSet = NULL;
#endif

    m_pVirtualCallStubManager = NULL;

#ifdef FEATURE_TIERED_COMPILATION
    m_callCountingManager = NULL;
#endif

#ifdef FEATURE_ON_STACK_REPLACEMENT
    m_onStackReplacementManager = NULL;
#endif

    m_fGCPressure = false;
    m_fTerminated = false;
    m_fUnloaded = false;
    m_fMarked = false;
    m_pLoaderAllocatorDestroyNext = NULL;
    m_pDomain = NULL;
    m_pCodeHeapInitialAlloc = NULL;
    m_pVSDHeapInitialAlloc = NULL;
    m_pLastUsedCodeHeap = NULL;
    m_pLastUsedDynamicCodeHeap = NULL;
    m_pJumpStubCache = NULL;
    m_IsCollectible = collectible;

    m_pMarshalingData = NULL;

#ifdef FEATURE_COMINTEROP
    m_pComCallWrapperCache = NULL;
#endif

    m_pUMEntryThunkCache = NULL;

    m_nLoaderAllocator = InterlockedIncrement64((LONGLONG *)&LoaderAllocator::cLoaderAllocatorsCreated);

#ifdef FEATURE_PGO
    m_pgoManager = NULL;
#endif
}

LoaderAllocator::~LoaderAllocator()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
    }
    CONTRACTL_END;
#if !defined(DACCESS_COMPILE)
    Terminate();

    // Assert that VSD is not still active when the destructor is called.
    _ASSERTE(m_pVirtualCallStubManager == NULL);

     // Code manager is responsible for cleaning up.
    _ASSERTE(m_pJumpStubCache == NULL);
#endif
}

#ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
//
void LoaderAllocator::AddReference()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE((m_cReferences > (UINT32)0) && (m_cReferences != (UINT32)-1));
    InterlockedIncrement((LONG *)&m_cReferences);
}
#endif //!DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Adds reference if the native object is alive  - code:LoaderAllocator#AssemblyPhases.
// Returns TRUE if the reference was added.
//
BOOL LoaderAllocator::AddReferenceIfAlive()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    for (;;)
    {
        // Local snaphost of ref-count
        UINT32 cReferencesLocalSnapshot = m_cReferences;
        _ASSERTE(cReferencesLocalSnapshot != (UINT32)-1);

        if (cReferencesLocalSnapshot == 0)
        {   // Ref-count was 0, do not AddRef
            return FALSE;
        }

        UINT32 cOriginalReferences = InterlockedCompareExchange(
            (LONG *)&m_cReferences,
            cReferencesLocalSnapshot + 1,
            cReferencesLocalSnapshot);

        if (cOriginalReferences == cReferencesLocalSnapshot)
        {   // The exchange happened
            return TRUE;
        }
        // Let's spin till we are the only thread to modify this value
    }
#else //DACCESS_COMPILE
    // DAC won't AddRef
    return IsAlive();
#endif //DACCESS_COMPILE
} // LoaderAllocator::AddReferenceIfAlive

//---------------------------------------------------------------------------------------
//
BOOL LoaderAllocator::Release()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Only actually destroy the domain assembly when all references to it are gone.
    // This should preserve behavior in the debugger such that an UnloadModule event
    // will occur before the underlying data structure cease functioning.
#ifndef DACCESS_COMPILE

    _ASSERTE((m_cReferences > (UINT32)0) && (m_cReferences != (UINT32)-1));
    LONG cNewReferences = InterlockedDecrement((LONG *)&m_cReferences);
    return (cNewReferences == 0);
#else //DACCESS_COMPILE

    return (m_cReferences == (UINT32)0);
#endif //DACCESS_COMPILE
} // LoaderAllocator::Release

#ifndef DACCESS_COMPILE
//---------------------------------------------------------------------------------------
//
BOOL LoaderAllocator::CheckAddReference_Unlocked(LoaderAllocator *pOtherLA)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // This must be checked before calling this function
    _ASSERTE(pOtherLA != this);

    // This function requires the that loader allocator lock have been taken.
    _ASSERTE(GetDomain()->GetLoaderAllocatorReferencesLock()->OwnedByCurrentThread());

    if (m_LoaderAllocatorReferences.Lookup(pOtherLA) == NULL)
    {
        GCX_COOP();
        // Build a managed reference to keep the target object live
        AllocateHandle(pOtherLA->GetExposedObject());

        // Keep track of the references that have already been made
        m_LoaderAllocatorReferences.Add(pOtherLA);

        // Notify the other LoaderAllocator that a reference exists
        pOtherLA->AddReference();
        return TRUE;
    }

    return FALSE;
}

//---------------------------------------------------------------------------------------
//
BOOL LoaderAllocator::EnsureReference(LoaderAllocator *pOtherLA)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Check if this lock can be taken in all places that the function is called
    _ASSERTE(GetDomain()->GetLoaderAllocatorReferencesLock()->Debug_CanTake());

    if (!IsCollectible())
        return FALSE;

    if (this == pOtherLA)
        return FALSE;

    if (!pOtherLA->IsCollectible())
        return FALSE;

    CrstHolder ch(GetDomain()->GetLoaderAllocatorReferencesLock());
    return CheckAddReference_Unlocked(pOtherLA);
}

BOOL LoaderAllocator::EnsureInstantiation(Module *pDefiningModule, Instantiation inst)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL fNewReferenceNeeded = FALSE;

    // Check if this lock can be taken in all places that the function is called
    _ASSERTE(GetDomain()->GetLoaderAllocatorReferencesLock()->Debug_CanTake());

    if (!IsCollectible())
        return FALSE;

    CrstHolder ch(GetDomain()->GetLoaderAllocatorReferencesLock());

    if (pDefiningModule != NULL)
    {
        LoaderAllocator *pDefiningLoaderAllocator = pDefiningModule->GetLoaderAllocator();
        if (pDefiningLoaderAllocator->IsCollectible())
        {
            if (pDefiningLoaderAllocator != this)
            {
                fNewReferenceNeeded = CheckAddReference_Unlocked(pDefiningLoaderAllocator) || fNewReferenceNeeded;
            }
        }
    }

    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        TypeHandle arg = inst[i];
        LoaderAllocator *pOtherLA = arg.GetLoaderModule()->GetLoaderAllocator();

        if (pOtherLA == this)
            continue;

        if (!pOtherLA->IsCollectible())
            continue;

        fNewReferenceNeeded = CheckAddReference_Unlocked(pOtherLA) || fNewReferenceNeeded;
    }

    return fNewReferenceNeeded;
}

bool LoaderAllocator::Marked()
{
    LIMITED_METHOD_CONTRACT;
    return m_fMarked;
}

void LoaderAllocator::ClearMark()
{
    LIMITED_METHOD_CONTRACT;
    m_fMarked = false;
}

void LoaderAllocator::Mark()
{
    WRAPPER_NO_CONTRACT;

    if (!m_fMarked)
    {
        m_fMarked = true;

        LoaderAllocatorSet::Iterator iter = m_LoaderAllocatorReferences.Begin();
        while (iter != m_LoaderAllocatorReferences.End())
        {
            LoaderAllocator *pAllocator = *iter;
            pAllocator->Mark();
            iter++;
        }
    }
}

//---------------------------------------------------------------------------------------
//
// Collect unreferenced assemblies, remove them from the assembly list and return their loader allocator
// list.
//
//static
LoaderAllocator * LoaderAllocator::GCLoaderAllocators_RemoveAssemblies(AppDomain * pAppDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;
    // List of LoaderAllocators being deleted
    LoaderAllocator * pFirstDestroyedLoaderAllocator = NULL;

#if 0
    // Debug logic for debugging the loader allocator gc.
    {
        /* Iterate through every loader allocator, and print its current state */
        AppDomain::AssemblyIterator iData;
        iData = pAppDomain->IterateAssembliesEx((AssemblyIterationFlags)(
            kIncludeExecution | kIncludeLoaded | kIncludeCollected));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

        while (iData.Next_Unlocked(pDomainAssembly.This()))
        {
            // The assembly could be collected (ref-count = 0), do not use holder which calls add-ref
            Assembly * pAssembly = pDomainAssembly->GetAssembly();

            if (pAssembly != NULL)
            {
                LoaderAllocator * pLoaderAllocator = pAssembly->GetLoaderAllocator();
                if (pLoaderAllocator->IsCollectible())
                {
                    printf("LA %p ReferencesTo %d\n", pLoaderAllocator, pLoaderAllocator->m_cReferences);
                    LoaderAllocatorSet::Iterator iter = pLoaderAllocator->m_LoaderAllocatorReferences.Begin();
                    while (iter != pLoaderAllocator->m_LoaderAllocatorReferences.End())
                    {
                        LoaderAllocator * pAllocator = *iter;
                        printf("LARefTo: %p\n", pAllocator);
                        iter++;
                    }
                }
            }
        }
    }
#endif //0

    AppDomain::AssemblyIterator i;
    {
        // Iterate through every loader allocator, marking as we go
        CrstHolder chLoaderAllocatorReferencesLock(pAppDomain->GetLoaderAllocatorReferencesLock());
        CrstHolder chAssemblyListLock(pAppDomain->GetAssemblyListLock());

        i = pAppDomain->IterateAssembliesEx((AssemblyIterationFlags)(
            kIncludeExecution | kIncludeLoaded | kIncludeCollected));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;

        while (i.Next_Unlocked(pDomainAssembly.This()))
        {
            // The assembly could be collected (ref-count = 0), do not use holder which calls add-ref
            Assembly * pAssembly = pDomainAssembly->GetAssembly();

            if (pAssembly != NULL)
            {
                LoaderAllocator * pLoaderAllocator = pAssembly->GetLoaderAllocator();
                if (pLoaderAllocator->IsCollectible())
                {
                    if (pLoaderAllocator->IsAlive())
                        pLoaderAllocator->Mark();
                }
            }
        }

        // Iterate through every loader allocator, unmarking marked loaderallocators, and
        // build a free list of unmarked ones
        i = pAppDomain->IterateAssembliesEx((AssemblyIterationFlags)(
            kIncludeExecution | kIncludeLoaded | kIncludeCollected));

        while (i.Next_Unlocked(pDomainAssembly.This()))
        {
            // The assembly could be collected (ref-count = 0), do not use holder which calls add-ref
            Assembly * pAssembly = pDomainAssembly->GetAssembly();

            if (pAssembly != NULL)
            {
                LoaderAllocator * pLoaderAllocator = pAssembly->GetLoaderAllocator();
                if (pLoaderAllocator->IsCollectible())
                {
                    if (pLoaderAllocator->Marked())
                    {
                        pLoaderAllocator->ClearMark();
                    }
                    else if (!pLoaderAllocator->IsAlive())
                    {
                        // Check that we don't have already this LoaderAllocator in the list to destroy
                        // (in case multiple assemblies are loaded in the same LoaderAllocator)
                        bool addAllocator = true;
                        LoaderAllocator * pCheckAllocatorToDestroy = pFirstDestroyedLoaderAllocator;
                        while (pCheckAllocatorToDestroy != NULL)
                        {
                            if (pCheckAllocatorToDestroy == pLoaderAllocator)
                            {
                                addAllocator = false;
                                break;
                            }

                            pCheckAllocatorToDestroy = pCheckAllocatorToDestroy->m_pLoaderAllocatorDestroyNext;
                        }

                        // Otherwise, we have a LoaderAllocator that we add to the list
                        if (addAllocator)
                        {
                            pLoaderAllocator->m_pLoaderAllocatorDestroyNext = pFirstDestroyedLoaderAllocator;
                            // We will store a reference to this assembly, and use it later in this function
                            pFirstDestroyedLoaderAllocator = pLoaderAllocator;
                            _ASSERTE(pLoaderAllocator->m_pFirstDomainAssemblyFromSameALCToDelete != NULL);
                        }
                    }
                }
            }
        }
    }

    // Iterate through free list, removing from Assembly list
    LoaderAllocator * pDomainLoaderAllocatorDestroyIterator = pFirstDestroyedLoaderAllocator;

    while (pDomainLoaderAllocatorDestroyIterator != NULL)
    {
        _ASSERTE(!pDomainLoaderAllocatorDestroyIterator->IsAlive());

        GetAppDomain()->RemoveTypesFromTypeIDMap(pDomainLoaderAllocatorDestroyIterator);

        DomainAssemblyIterator domainAssemblyIt(pDomainLoaderAllocatorDestroyIterator->m_pFirstDomainAssemblyFromSameALCToDelete);

        // Release all assemblies from the same ALC
        while (!domainAssemblyIt.end())
        {
            DomainAssembly* domainAssemblyToRemove = domainAssemblyIt;
            pAppDomain->RemoveAssembly(domainAssemblyToRemove);

            if (!domainAssemblyToRemove->GetAssembly()->IsDynamic())
            {
                pAppDomain->RemoveFileFromCache(domainAssemblyToRemove->GetPEAssembly());
                AssemblySpec spec;
                spec.InitializeSpec(domainAssemblyToRemove->GetPEAssembly());
                VERIFY(pAppDomain->RemoveAssemblyFromCache(domainAssemblyToRemove));
            }

            domainAssemblyIt++;
        }

        pDomainLoaderAllocatorDestroyIterator = pDomainLoaderAllocatorDestroyIterator->m_pLoaderAllocatorDestroyNext;
    }

    return pFirstDestroyedLoaderAllocator;
} // LoaderAllocator::GCLoaderAllocators_RemoveAssemblies

//---------------------------------------------------------------------------------------
//
// Collect unreferenced assemblies, delete all their remaining resources.
//
//static
void LoaderAllocator::GCLoaderAllocators(LoaderAllocator* pOriginalLoaderAllocator)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // List of LoaderAllocators being deleted
    LoaderAllocator * pFirstDestroyedLoaderAllocator = NULL;

    AppDomain* pAppDomain = (AppDomain*)pOriginalLoaderAllocator->GetDomain();

    // Collect all LoaderAllocators that don't have anymore DomainAssemblies alive
    // Note: that it may not collect our pOriginalLoaderAllocator in case this
    // LoaderAllocator hasn't loaded any DomainAssembly. We handle this case in the next loop.
    // Note: The removed LoaderAllocators are not reachable outside of this function anymore, because we
    // removed them from the assembly list
    pFirstDestroyedLoaderAllocator = GCLoaderAllocators_RemoveAssemblies(pAppDomain);

    bool isOriginalLoaderAllocatorFound = false;

    // Iterate through free list, firing ETW events and notifying the debugger
    LoaderAllocator * pDomainLoaderAllocatorDestroyIterator = pFirstDestroyedLoaderAllocator;
    while (pDomainLoaderAllocatorDestroyIterator != NULL)
    {
        _ASSERTE(!pDomainLoaderAllocatorDestroyIterator->IsAlive());
        // Fire ETW event
        ETW::LoaderLog::CollectibleLoaderAllocatorUnload((AssemblyLoaderAllocator *)pDomainLoaderAllocatorDestroyIterator);

        // Set the unloaded flag before notifying the debugger
        pDomainLoaderAllocatorDestroyIterator->SetIsUnloaded();

        DomainAssemblyIterator domainAssemblyIt(pDomainLoaderAllocatorDestroyIterator->m_pFirstDomainAssemblyFromSameALCToDelete);
        while (!domainAssemblyIt.end())
        {
            // Call AssemblyUnloadStarted event
            domainAssemblyIt->GetAssembly()->StartUnload();
            // Notify the debugger
            domainAssemblyIt->NotifyDebuggerUnload();
            domainAssemblyIt++;
        }

        if (pDomainLoaderAllocatorDestroyIterator == pOriginalLoaderAllocator)
        {
            isOriginalLoaderAllocatorFound = true;
        }
        pDomainLoaderAllocatorDestroyIterator = pDomainLoaderAllocatorDestroyIterator->m_pLoaderAllocatorDestroyNext;
    }

    // If the original LoaderAllocator was not processed, it is most likely a LoaderAllocator without any loaded DomainAssembly
    // But we still want to collect it so we add it to the list of LoaderAllocator to destroy
    if (!isOriginalLoaderAllocatorFound && !pOriginalLoaderAllocator->IsAlive())
    {
        pOriginalLoaderAllocator->m_pLoaderAllocatorDestroyNext = pFirstDestroyedLoaderAllocator;
        pFirstDestroyedLoaderAllocator = pOriginalLoaderAllocator;
    }

    // Iterate through free list, deleting DomainAssemblies
    pDomainLoaderAllocatorDestroyIterator = pFirstDestroyedLoaderAllocator;
    while (pDomainLoaderAllocatorDestroyIterator != NULL)
    {
        _ASSERTE(!pDomainLoaderAllocatorDestroyIterator->IsAlive());

        DomainAssemblyIterator domainAssemblyIt(pDomainLoaderAllocatorDestroyIterator->m_pFirstDomainAssemblyFromSameALCToDelete);
        while (!domainAssemblyIt.end())
        {
            delete (DomainAssembly*)domainAssemblyIt;
            domainAssemblyIt++;
        }
        // We really don't have to set it to NULL as the assembly is not reachable anymore, but just in case ...
        // (Also debugging NULL AVs if someone uses it accidentally is so much easier)
        pDomainLoaderAllocatorDestroyIterator->m_pFirstDomainAssemblyFromSameALCToDelete = NULL;

        pDomainLoaderAllocatorDestroyIterator->ReleaseManagedAssemblyLoadContext();

        // The native objects in dependent handles may refer to the virtual call stub manager's heaps, so clear the dependent
        // handles first
        pDomainLoaderAllocatorDestroyIterator->CleanupDependentHandlesToNativeObjects();

        // The following code was previously happening on delete ~DomainAssembly->Terminate
        // We are moving this part here in order to make sure that we can unload a LoaderAllocator
        // that didn't have a DomainAssembly
        // (we have now a LoaderAllocator with 0-n DomainAssembly)

        // This cleanup code starts resembling parts of AppDomain::Terminate too much.
        // It would be useful to reduce duplication and also establish clear responsibilities
        // for LoaderAllocator::Destroy, Assembly::Terminate, LoaderAllocator::Terminate
        // and LoaderAllocator::~LoaderAllocator. We need to establish how these
        // cleanup paths interact with app-domain unload and process tear-down, too.

        if (!IsAtProcessExit())
        {
            // Suspend the EE to do some clean up that can only occur
            // while no threads are running.
            GCX_COOP(); // SuspendEE may require current thread to be in Coop mode
                        // SuspendEE cares about the reason flag only when invoked for a GC
                        // Other values are typically ignored. If using SUSPEND_FOR_APPDOMAIN_SHUTDOWN
                        // is inappropriate, we can introduce a new flag or hijack an unused one.
            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_APPDOMAIN_SHUTDOWN);

            // drop the cast cache while still in COOP mode.
            CastCache::FlushCurrentCache();
        }

        ExecutionManager::Unload(pDomainLoaderAllocatorDestroyIterator);
        pDomainLoaderAllocatorDestroyIterator->UninitVirtualCallStubManager();

        // TODO: Do we really want to perform this on each LoaderAllocator?
        MethodTable::ClearMethodDataCache();
        ClearJitGenericHandleCache();

        if (!IsAtProcessExit())
        {
            // Resume the EE.
            ThreadSuspend::RestartEE(FALSE, TRUE);
        }

        // Because RegisterLoaderAllocatorForDeletion is modifying m_pLoaderAllocatorDestroyNext, we are saving it here
        LoaderAllocator* pLoaderAllocatorDestroyNext = pDomainLoaderAllocatorDestroyIterator->m_pLoaderAllocatorDestroyNext;

        // Register this LoaderAllocator for cleanup
        pAppDomain->RegisterLoaderAllocatorForDeletion(pDomainLoaderAllocatorDestroyIterator);

        // Go to next
        pDomainLoaderAllocatorDestroyIterator = pLoaderAllocatorDestroyNext;
    }

    // Deleting the DomainAssemblies will have created a list of LoaderAllocator's on the AppDomain
    // Call this shutdown function to clean those up.
    pAppDomain->ShutdownFreeLoaderAllocators();
} // LoaderAllocator::GCLoaderAllocators

//---------------------------------------------------------------------------------------
//
//static
BOOL LoaderAllocator::Destroy(QCall::LoaderAllocatorHandle pLoaderAllocator)
{
    if (ObjectHandleIsNull(pLoaderAllocator->GetLoaderAllocatorObjectHandle()))
    {
        STRESS_LOG1(LF_CLASSLOADER, LL_INFO100, "Begin LoaderAllocator::Destroy for loader allocator %p\n", reinterpret_cast<void *>(static_cast<PTR_LoaderAllocator>(pLoaderAllocator)));
        LoaderAllocatorID *pID = pLoaderAllocator->Id();

        {
            GCX_COOP();
            LoaderAllocator::RemoveMemoryToLoaderAllocatorAssociation(pLoaderAllocator);
        }

        // This will probably change for shared code unloading
        _ASSERTE(pID->GetType() == LAT_Assembly);

#ifdef FEATURE_COMINTEROP
        if (pLoaderAllocator->m_pComCallWrapperCache)
        {
            pLoaderAllocator->m_pComCallWrapperCache->Release();

            // if the above released the wrapper cache, then it will call back and reset our
            // m_pComCallWrapperCache to null.
            if (!pLoaderAllocator->m_pComCallWrapperCache)
            {
                LOG((LF_CLASSLOADER, LL_INFO10, "LoaderAllocator::Destroy ComCallWrapperCache released\n"));
            }
    #ifdef _DEBUG
            else
            {
                pLoaderAllocator->m_pComCallWrapperCache = NULL;
                LOG((LF_CLASSLOADER, LL_INFO10, "LoaderAllocator::Destroy ComCallWrapperCache not released\n"));
            }
    #endif // _DEBUG
        }
#endif // FEATURE_COMINTEROP

        DomainAssembly* pDomainAssembly = (DomainAssembly*)(pID->GetDomainAssemblyIterator());
        if (pDomainAssembly != NULL)
        {
            Assembly *pAssembly = pDomainAssembly->GetAssembly();
            pLoaderAllocator->m_pFirstDomainAssemblyFromSameALCToDelete = pAssembly->GetDomainAssembly();
        }

        // Iterate through all references to other loader allocators and decrement their reference
        // count
        LoaderAllocatorSet::Iterator iter = pLoaderAllocator->m_LoaderAllocatorReferences.Begin();
        while (iter != pLoaderAllocator->m_LoaderAllocatorReferences.End())
        {
            LoaderAllocator *pAllocator = *iter;
            pAllocator->Release();
            iter++;
        }

        // Release this loader allocator
        BOOL fIsLastReferenceReleased = pLoaderAllocator->Release();

        // If the reference count on this assembly got to 0, then a LoaderAllocator may
        // be able to be collected, thus, perform a garbage collection.
        // The reference count is setup such that in the case of non-trivial graphs, the reference count
        // may hit zero early.
        if (fIsLastReferenceReleased)
        {
            LoaderAllocator::GCLoaderAllocators(pLoaderAllocator);
        }
        STRESS_LOG1(LF_CLASSLOADER, LL_INFO100, "End LoaderAllocator::Destroy for loader allocator %p\n", reinterpret_cast<void *>(static_cast<PTR_LoaderAllocator>(pLoaderAllocator)));

        return TRUE;
    }

    return FALSE;
} // LoaderAllocator::Destroy

extern "C" BOOL QCALLTYPE LoaderAllocator_Destroy(QCall::LoaderAllocatorHandle pLoaderAllocator)
{
    QCALL_CONTRACT;

    BOOL ret = FALSE;

    BEGIN_QCALL;

    ret = LoaderAllocator::Destroy(pLoaderAllocator);

    END_QCALL;

    return ret;
}

#define MAX_LOADERALLOCATOR_HANDLE 0x40000000

// Returns NULL if the managed LoaderAllocator object was already collected.
LOADERHANDLE LoaderAllocator::AllocateHandle(OBJECTREF value)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    LOADERHANDLE retVal;

    struct
    {
        OBJECTREF value;
        LOADERALLOCATORREF loaderAllocator;
        PTRARRAYREF handleTable;
        PTRARRAYREF handleTableOld;
    } gc;
    gc.value = NULL;
    gc.loaderAllocator = NULL;
    gc.handleTable = NULL;
    gc.handleTableOld = NULL;

    GCPROTECT_BEGIN(gc);

    gc.value = value;

    // The handle table is read locklessly, be careful
    if (IsCollectible())
    {
        gc.loaderAllocator = (LOADERALLOCATORREF)ObjectFromHandle(m_hLoaderAllocatorObjectHandle);
        if (gc.loaderAllocator == NULL)
        {   // The managed LoaderAllocator is already collected, we cannot allocate any exposed managed objects for it
            retVal = NULL;
        }
        else
        {
            DWORD slotsUsed;
            DWORD numComponents;

            do
            {
                {
                    CrstHolder ch(&m_crstLoaderAllocator);

                    gc.handleTable = gc.loaderAllocator->GetHandleTable();

                    if (!m_freeHandleIndexesStack.IsEmpty())
                    {
                        // Reuse a handle slot that was previously freed
                        DWORD freeHandleIndex = m_freeHandleIndexesStack.Pop();
                        gc.handleTable->SetAt(freeHandleIndex, gc.value);
                        retVal = (UINT_PTR)((freeHandleIndex + 1) << 1);
                        break;
                    }

                    slotsUsed = gc.loaderAllocator->GetSlotsUsed();

                    if (slotsUsed > MAX_LOADERALLOCATOR_HANDLE)
                    {
                        COMPlusThrowOM();
                    }

                    numComponents = gc.handleTable->GetNumComponents();

                    if (slotsUsed < numComponents)
                    {
                        // The handle table is large enough, allocate next slot from it
                        gc.handleTable->SetAt(slotsUsed, gc.value);
                        gc.loaderAllocator->SetSlotsUsed(slotsUsed + 1);
                        retVal = (UINT_PTR)((slotsUsed + 1) << 1);
                        break;
                    }
                }

                // We need to enlarge the handle table
                gc.handleTableOld = gc.handleTable;

                DWORD newSize = numComponents * 2;
                gc.handleTable = (PTRARRAYREF)AllocateObjectArray(newSize, g_pObjectClass);

                {
                    CrstHolder ch(&m_crstLoaderAllocator);

                    if (gc.loaderAllocator->GetHandleTable() == gc.handleTableOld)
                    {
                        /* Copy out of old array */
                        memmoveGCRefs(gc.handleTable->GetDataPtr(), gc.handleTableOld->GetDataPtr(), slotsUsed * sizeof(Object *));
                        gc.loaderAllocator->SetHandleTable(gc.handleTable);
                    }
                    else
                    {
                        // Another thread has beaten us on enlarging the handle array, use the handle table it has allocated
                        gc.handleTable = gc.loaderAllocator->GetHandleTable();
                    }

                    slotsUsed = gc.loaderAllocator->GetSlotsUsed();
                    numComponents = gc.handleTable->GetNumComponents();

                    if (slotsUsed < numComponents)
                    {
                        // The handle table is large enough, allocate next slot from it
                        gc.handleTable->SetAt(slotsUsed, gc.value);
                        gc.loaderAllocator->SetSlotsUsed(slotsUsed + 1);
                        retVal = (UINT_PTR)((slotsUsed + 1) << 1);
                        break;
                    }
                }

                // Loop in the unlikely case that another thread has beaten us on the handle array enlarging, but
                // all the slots were used up before the current thread was scheduled.
            }
            while (true);
        }
    }
    else
    {
        OBJECTREF* pRef = GetDomain()->AllocateObjRefPtrsInLargeTable(1);
        SetObjectReference(pRef, gc.value);
        retVal = (((UINT_PTR)pRef) + 1);
    }

    GCPROTECT_END();

    return retVal;
}

OBJECTREF LoaderAllocator::GetHandleValue(LOADERHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTREF objRet = NULL;
    GET_LOADERHANDLE_VALUE_FAST(this, handle, &objRet);
    return objRet;
}

void LoaderAllocator::FreeHandle(LOADERHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(handle != NULL);
    }
    CONTRACTL_END;

    SetHandleValue(handle, NULL);

    if ((((UINT_PTR)handle) & 1) == 0)
    {
        // The slot value doesn't have the low bit set, so it is an index to the handle table.
        // In this case, push the index of the handle to the stack of freed indexes for
        // reuse.
        CrstHolder ch(&m_crstLoaderAllocator);

        UINT_PTR index = (((UINT_PTR)handle) >> 1) - 1;
        // The Push can fail due to OOM. Ignore this failure, it is better than crashing. The
        // only effect is that the slot will not be reused in the future if the runtime survives
        // the low memory situation.
        m_freeHandleIndexesStack.Push((DWORD)index);
    }
}

OBJECTREF LoaderAllocator::CompareExchangeValueInHandle(LOADERHANDLE handle, OBJECTREF valueUNSAFE, OBJECTREF compareUNSAFE)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(handle != NULL);
    }
    CONTRACTL_END;

    OBJECTREF retVal;

    struct
    {
        OBJECTREF value;
        OBJECTREF compare;
        OBJECTREF previous;
    } gc;
    gc.value = NULL;
    gc.compare = NULL;
    gc.previous = NULL;

    GCPROTECT_BEGIN(gc);

    gc.value = valueUNSAFE;
    gc.compare = compareUNSAFE;

    if ((((UINT_PTR)handle) & 1) != 0)
    {
        OBJECTREF *ptr = (OBJECTREF *)(((UINT_PTR)handle) - 1);

        gc.previous = ObjectToOBJECTREF(InterlockedCompareExchangeT((Object **)ptr, OBJECTREFToObject(gc.value), OBJECTREFToObject(gc.compare)));
        if (gc.previous == gc.compare)
        {
            ErectWriteBarrier(ptr, gc.value);
        }
    }
    else
    {
        /* The handle table is read locklessly, be careful */
        CrstHolder ch(&m_crstLoaderAllocator);

        _ASSERTE(!ObjectHandleIsNull(m_hLoaderAllocatorObjectHandle));

        UINT_PTR index = (((UINT_PTR)handle) >> 1) - 1;
        LOADERALLOCATORREF loaderAllocator = (LOADERALLOCATORREF)ObjectFromHandle(m_hLoaderAllocatorObjectHandle);
        PTRARRAYREF handleTable = loaderAllocator->GetHandleTable();

        gc.previous = handleTable->GetAt(index);
        if (gc.previous == gc.compare)
        {
            handleTable->SetAt(index, gc.value);
        }
    }

    retVal = gc.previous;
    GCPROTECT_END();

    return retVal;
}

void LoaderAllocator::SetHandleValue(LOADERHANDLE handle, OBJECTREF value)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(handle != NULL);
    }
    CONTRACTL_END;

    GCX_COOP();

    GCPROTECT_BEGIN(value);

    // If the slot value does have the low bit set, then it is a simple pointer to the value
    // Otherwise, we will need a more complicated operation to clear the value.
    if ((((UINT_PTR)handle) & 1) != 0)
    {
        OBJECTREF *ptr = (OBJECTREF *)(((UINT_PTR)handle) - 1);
        SetObjectReference(ptr, value);
    }
    else
    {
        // The handle table is read locklessly, be careful
        CrstHolder ch(&m_crstLoaderAllocator);

        _ASSERTE(!ObjectHandleIsNull(m_hLoaderAllocatorObjectHandle));

        UINT_PTR index = (((UINT_PTR)handle) >> 1) - 1;
        LOADERALLOCATORREF loaderAllocator = (LOADERALLOCATORREF)ObjectFromHandle(m_hLoaderAllocatorObjectHandle);
        PTRARRAYREF handleTable = loaderAllocator->GetHandleTable();
        handleTable->SetAt(index, value);
    }

    GCPROTECT_END();

    return;
}

void LoaderAllocator::SetupManagedTracking(LOADERALLOCATORREF * pKeepLoaderAllocatorAlive)
{
    STANDARD_VM_CONTRACT;

    GCInterface::AddMemoryPressure(30000);
    m_fGCPressure = true;

    GCX_COOP();

    //
    // Initialize managed loader allocator reference holder
    //

    MethodTable *pMT = CoreLibBinder::GetClass(CLASS__LOADERALLOCATOR);

    *pKeepLoaderAllocatorAlive = (LOADERALLOCATORREF)AllocateObject(pMT);

    MethodDescCallSite initLoaderAllocator(METHOD__LOADERALLOCATOR__CTOR, (OBJECTREF *)pKeepLoaderAllocatorAlive);

    ARG_SLOT args[] = {
        ObjToArgSlot(*pKeepLoaderAllocatorAlive)
    };

    initLoaderAllocator.Call(args);

    m_hLoaderAllocatorObjectHandle = GetDomain()->CreateLongWeakHandle(*pKeepLoaderAllocatorAlive);

    RegisterHandleForCleanup(m_hLoaderAllocatorObjectHandle);
}

void LoaderAllocator::ActivateManagedTracking()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END

    GCX_COOP();

    // There is now one external reference to this LoaderAllocator (the managed scout)
    _ASSERTE(m_cReferences == (UINT32)-1);
    m_cReferences = (UINT32)1;

    LOADERALLOCATORREF loaderAllocator = (LOADERALLOCATORREF)ObjectFromHandle(m_hLoaderAllocatorObjectHandle);
    loaderAllocator->SetNativeLoaderAllocator(this);
}


// We don't actually allocate a low frequency heap for collectible types.
// This is carefully tuned to sum up to 16 pages to reduce waste.
#define COLLECTIBLE_LOW_FREQUENCY_HEAP_SIZE        (0 * GetOsPageSize())
#define COLLECTIBLE_HIGH_FREQUENCY_HEAP_SIZE       (3 * GetOsPageSize())
#define COLLECTIBLE_STUB_HEAP_SIZE                 GetOsPageSize()
#define COLLECTIBLE_CODEHEAP_SIZE                  (10 * GetOsPageSize())
#define COLLECTIBLE_VIRTUALSTUBDISPATCH_HEAP_SPACE (2 * GetOsPageSize())

void LoaderAllocator::Init(BaseDomain *pDomain, BYTE *pExecutableHeapMemory)
{
    STANDARD_VM_CONTRACT;

    m_pDomain = pDomain;

    m_crstLoaderAllocator.Init(CrstLoaderAllocator, (CrstFlags)CRST_UNSAFE_COOPGC);
    m_InteropDataCrst.Init(CrstInteropData, CRST_REENTRANCY);
#ifdef FEATURE_COMINTEROP
    m_ComCallWrapperCrst.Init(CrstCOMCallWrapper);
#endif

    m_methodDescBackpatchInfoTracker.Initialize(this);

    //
    // Initialize the heaps
    //

    DWORD dwLowFrequencyHeapReserveSize;
    DWORD dwHighFrequencyHeapReserveSize;
    DWORD dwStubHeapReserveSize;
    DWORD dwExecutableHeapReserveSize;
    DWORD dwCodeHeapReserveSize;
    DWORD dwVSDHeapReserveSize;

    dwExecutableHeapReserveSize = 0;

    if (IsCollectible())
    {
        dwLowFrequencyHeapReserveSize  = COLLECTIBLE_LOW_FREQUENCY_HEAP_SIZE;
        dwHighFrequencyHeapReserveSize = COLLECTIBLE_HIGH_FREQUENCY_HEAP_SIZE;
        dwStubHeapReserveSize          = COLLECTIBLE_STUB_HEAP_SIZE;
        dwCodeHeapReserveSize          = COLLECTIBLE_CODEHEAP_SIZE;
        dwVSDHeapReserveSize           = COLLECTIBLE_VIRTUALSTUBDISPATCH_HEAP_SPACE;
    }
    else
    {
        dwLowFrequencyHeapReserveSize  = LOW_FREQUENCY_HEAP_RESERVE_SIZE;
        dwHighFrequencyHeapReserveSize = HIGH_FREQUENCY_HEAP_RESERVE_SIZE;
        dwStubHeapReserveSize          = STUB_HEAP_RESERVE_SIZE;

        // Non-collectible assemblies do not reserve space for these heaps.
        dwCodeHeapReserveSize = 0;
        dwVSDHeapReserveSize = 0;
    }

    // The global heap needs a bit of space for executable memory that is not associated with a rangelist.
    // Take a page from the high-frequency heap for this.
    if (pExecutableHeapMemory != NULL)
    {
        dwExecutableHeapReserveSize = GetOsPageSize();

        _ASSERTE(dwExecutableHeapReserveSize < dwHighFrequencyHeapReserveSize);
        dwHighFrequencyHeapReserveSize -= dwExecutableHeapReserveSize;
    }

    DWORD dwTotalReserveMemSize = dwLowFrequencyHeapReserveSize
                                + dwHighFrequencyHeapReserveSize
                                + dwStubHeapReserveSize
                                + dwCodeHeapReserveSize
                                + dwVSDHeapReserveSize
                                + dwExecutableHeapReserveSize;

    dwTotalReserveMemSize = (DWORD) ALIGN_UP(dwTotalReserveMemSize, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

    BYTE * initReservedMem = (BYTE*)ExecutableAllocator::Instance()->Reserve(dwTotalReserveMemSize);

    m_InitialReservedMemForLoaderHeaps = initReservedMem;

    if (initReservedMem == NULL)
        COMPlusThrowOM();

    if (IsCollectible())
    {
        m_pCodeHeapInitialAlloc = initReservedMem;
        initReservedMem += dwCodeHeapReserveSize;
        m_pVSDHeapInitialAlloc = initReservedMem;
        initReservedMem += dwVSDHeapReserveSize;
    }
    else
    {
        _ASSERTE((dwCodeHeapReserveSize == 0) && (m_pCodeHeapInitialAlloc == NULL));
        _ASSERTE((dwVSDHeapReserveSize == 0) && (m_pVSDHeapInitialAlloc == NULL));
    }

    if (dwLowFrequencyHeapReserveSize != 0)
    {
        _ASSERTE(!IsCollectible());

        m_pLowFrequencyHeap = new (&m_LowFreqHeapInstance) LoaderHeap(LOW_FREQUENCY_HEAP_RESERVE_SIZE,
                                                                      LOW_FREQUENCY_HEAP_COMMIT_SIZE,
                                                                      initReservedMem,
                                                                      dwLowFrequencyHeapReserveSize);
        initReservedMem += dwLowFrequencyHeapReserveSize;
    }

    if (dwExecutableHeapReserveSize != 0)
    {
        _ASSERTE(!IsCollectible());

        m_pExecutableHeap = new (pExecutableHeapMemory) LoaderHeap(STUB_HEAP_RESERVE_SIZE,
                                                                      STUB_HEAP_COMMIT_SIZE,
                                                                      initReservedMem,
                                                                      dwExecutableHeapReserveSize,
                                                                      NULL,
                                                                      UnlockedLoaderHeap::HeapKind::Executable
                                                                      );
        initReservedMem += dwExecutableHeapReserveSize;
    }

    m_pHighFrequencyHeap = new (&m_HighFreqHeapInstance) LoaderHeap(HIGH_FREQUENCY_HEAP_RESERVE_SIZE,
                                                                    HIGH_FREQUENCY_HEAP_COMMIT_SIZE,
                                                                    initReservedMem,
                                                                    dwHighFrequencyHeapReserveSize);
    initReservedMem += dwHighFrequencyHeapReserveSize;

    if (IsCollectible())
        m_pLowFrequencyHeap = m_pHighFrequencyHeap;

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
    m_pHighFrequencyHeap->m_fPermitStubsWithUnwindInfo = TRUE;
#endif

    m_pStubHeap = new (&m_StubHeapInstance) LoaderHeap(STUB_HEAP_RESERVE_SIZE,
                                                       STUB_HEAP_COMMIT_SIZE,
                                                       initReservedMem,
                                                       dwStubHeapReserveSize,
                                                       STUBMANAGER_RANGELIST(StubLinkStubManager),
                                                       UnlockedLoaderHeap::HeapKind::Executable);

    initReservedMem += dwStubHeapReserveSize;

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
    m_pStubHeap->m_fPermitStubsWithUnwindInfo = TRUE;
#endif

    m_pPrecodeHeap = new (&m_PrecodeHeapInstance) CodeFragmentHeap(this, STUB_CODE_BLOCK_PRECODE);

    m_pNewStubPrecodeHeap = new (&m_NewStubPrecodeHeapInstance) LoaderHeap(2 * GetStubCodePageSize(),
                                                                           2 * GetStubCodePageSize(),
                                                                           &m_stubPrecodeRangeList,
                                                                           UnlockedLoaderHeap::HeapKind::Interleaved,
                                                                           false /* fUnlocked */,
                                                                           StubPrecode::GenerateCodePage,
                                                                           StubPrecode::CodeSize);

    m_pFixupPrecodeHeap = new (&m_FixupPrecodeHeapInstance) LoaderHeap(2 * GetStubCodePageSize(),
                                                                       2 * GetStubCodePageSize(),
                                                                       &m_fixupPrecodeRangeList,
                                                                       UnlockedLoaderHeap::HeapKind::Interleaved,
                                                                       false /* fUnlocked */,
                                                                       FixupPrecode::GenerateCodePage,
                                                                       FixupPrecode::CodeSize);

    // Initialize the EE marshaling data to NULL.
    m_pMarshalingData = NULL;

    // Set up the IL stub cache
    m_ILStubCache.Init(this);

#ifdef FEATURE_COMINTEROP
    // Init the COM Interop data hash
    {
        LockOwner lock = { &m_InteropDataCrst, IsOwnerOfCrst };
        m_interopDataHash.Init(0, NULL, false, &lock);
    }
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_TIERED_COMPILATION
    if (g_pConfig->TieredCompilation())
    {
        m_callCountingManager = new CallCountingManager();
    }
#endif
}



#ifdef FEATURE_READYTORUN
PTR_CodeFragmentHeap LoaderAllocator::GetDynamicHelpersHeap()
{
    CONTRACTL {
        THROWS;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_pDynamicHelpersHeap == NULL)
    {
        CodeFragmentHeap * pDynamicHelpersHeap = new CodeFragmentHeap(this, STUB_CODE_BLOCK_DYNAMICHELPER);
        if (InterlockedCompareExchangeT(&m_pDynamicHelpersHeap, pDynamicHelpersHeap, NULL) != NULL)
            delete pDynamicHelpersHeap;
    }
    return m_pDynamicHelpersHeap;
}
#endif

FuncPtrStubs * LoaderAllocator::GetFuncPtrStubs()
{
    CONTRACTL {
        THROWS;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_pFuncPtrStubs == NULL)
    {
        FuncPtrStubs * pFuncPtrStubs = new FuncPtrStubs();
        if (InterlockedCompareExchangeT(&m_pFuncPtrStubs, pFuncPtrStubs, NULL) != NULL)
            delete pFuncPtrStubs;
    }
    return m_pFuncPtrStubs;
}

BYTE *LoaderAllocator::GetVSDHeapInitialBlock(DWORD *pSize)
{
    LIMITED_METHOD_CONTRACT;

    *pSize = 0;
    BYTE *buffer = InterlockedCompareExchangeT(&m_pVSDHeapInitialAlloc, NULL, m_pVSDHeapInitialAlloc);
    if (buffer != NULL)
    {
        *pSize = COLLECTIBLE_VIRTUALSTUBDISPATCH_HEAP_SPACE;
    }
    return buffer;
}

BYTE *LoaderAllocator::GetCodeHeapInitialBlock(const BYTE * loAddr, const BYTE * hiAddr, DWORD minimumSize, DWORD *pSize)
{
    LIMITED_METHOD_CONTRACT;

    *pSize = 0;
    // Check to see if the size is small enough that this might work
    if (minimumSize > COLLECTIBLE_CODEHEAP_SIZE)
        return NULL;

    // Check to see if initial alloc would be in the proper region
    if (loAddr != NULL || hiAddr != NULL)
    {
        if (m_pCodeHeapInitialAlloc < loAddr)
            return NULL;
        if ((m_pCodeHeapInitialAlloc + COLLECTIBLE_CODEHEAP_SIZE) > hiAddr)
            return NULL;
    }

    BYTE * buffer = InterlockedCompareExchangeT(&m_pCodeHeapInitialAlloc, NULL, m_pCodeHeapInitialAlloc);
    if (buffer != NULL)
    {
        *pSize = COLLECTIBLE_CODEHEAP_SIZE;
    }
    return buffer;
}

// in retail should be called from AppDomain::Terminate
void LoaderAllocator::Terminate()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    if (m_fTerminated)
        return;

    m_fTerminated = true;

    LOG((LF_CLASSLOADER, LL_INFO100, "Begin LoaderAllocator::Terminate for loader allocator %p\n", reinterpret_cast<void *>(static_cast<PTR_LoaderAllocator>(this))));

    DeleteMarshalingData();

    if (m_fGCPressure)
    {
        GCX_PREEMP();
        GCInterface::RemoveMemoryPressure(30000);
        m_fGCPressure = false;
    }

    delete m_pUMEntryThunkCache;
    m_pUMEntryThunkCache = NULL;

    m_crstLoaderAllocator.Destroy();
#ifdef FEATURE_COMINTEROP
    m_ComCallWrapperCrst.Destroy();
    m_InteropDataCrst.Destroy();
#endif
    m_LoaderAllocatorReferences.RemoveAll();

#ifdef FEATURE_TIERED_COMPILATION
    if (m_callCountingManager != NULL)
    {
        delete m_callCountingManager;
        m_callCountingManager = NULL;
    }
#endif

#ifdef FEATURE_ON_STACK_REPLACEMENT
    if (m_onStackReplacementManager != NULL)
    {
        delete m_onStackReplacementManager;
        m_onStackReplacementManager = NULL;
    }
#endif

    // In collectible types we merge the low frequency and high frequency heaps
    // So don't destroy them twice.
    if ((m_pLowFrequencyHeap != NULL) && (m_pLowFrequencyHeap != m_pHighFrequencyHeap))
    {
        m_pLowFrequencyHeap->~LoaderHeap();
        m_pLowFrequencyHeap = NULL;
    }

    if (m_pHighFrequencyHeap != NULL)
    {
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
        UnregisterUnwindInfoInLoaderHeap(m_pHighFrequencyHeap);
#endif

        m_pHighFrequencyHeap->~LoaderHeap();
        m_pHighFrequencyHeap = NULL;
    }

    if (m_pStubHeap != NULL)
    {
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
        UnregisterUnwindInfoInLoaderHeap(m_pStubHeap);
#endif

        m_pStubHeap->~LoaderHeap();
        m_pStubHeap = NULL;
    }

    if (m_pPrecodeHeap != NULL)
    {
        m_pPrecodeHeap->~CodeFragmentHeap();
        m_pPrecodeHeap = NULL;
    }

    if (m_pFixupPrecodeHeap != NULL)
    {
        m_pFixupPrecodeHeap->~LoaderHeap();
        m_pFixupPrecodeHeap = NULL;
    }

    if (m_pNewStubPrecodeHeap != NULL)
    {
        m_pNewStubPrecodeHeap->~LoaderHeap();
        m_pNewStubPrecodeHeap = NULL;
    }

#ifdef FEATURE_READYTORUN
    if (m_pDynamicHelpersHeap != NULL)
    {
        delete m_pDynamicHelpersHeap;
        m_pDynamicHelpersHeap = NULL;
    }
#endif

    if (m_pFuncPtrStubs != NULL)
    {
        delete m_pFuncPtrStubs;
        m_pFuncPtrStubs = NULL;
    }

    // This was the block reserved by BaseDomain::Init for the loaderheaps.
    if (m_InitialReservedMemForLoaderHeaps)
    {
        ExecutableAllocator::Instance()->Release(m_InitialReservedMemForLoaderHeaps);
        m_InitialReservedMemForLoaderHeaps=NULL;
    }

#ifdef FAT_DISPATCH_TOKENS
    if (m_pFatTokenSetLock != NULL)
    {
        delete m_pFatTokenSetLock;
        m_pFatTokenSetLock = NULL;
    }

    if (m_pFatTokenSet != NULL)
    {
        delete m_pFatTokenSet;
        m_pFatTokenSet = NULL;
    }
#endif // FAT_DISPATCH_TOKENS

    CleanupStringLiteralMap();

    LOG((LF_CLASSLOADER, LL_INFO100, "End LoaderAllocator::Terminate for loader allocator %p\n", reinterpret_cast<void *>(static_cast<PTR_LoaderAllocator>(this))));
}



#else //DACCESS_COMPILE
void LoaderAllocator::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();
    EMEM_OUT(("MEM: %p LoaderAllocator\n", dac_cast<TADDR>(this)));
    if (m_pLowFrequencyHeap.IsValid())
    {
        m_pLowFrequencyHeap->EnumMemoryRegions(flags);
    }
    if (m_pHighFrequencyHeap.IsValid())
    {
        m_pHighFrequencyHeap->EnumMemoryRegions(flags);
    }
    if (m_pStubHeap.IsValid())
    {
        m_pStubHeap->EnumMemoryRegions(flags);
    }
    if (m_pPrecodeHeap.IsValid())
    {
        m_pPrecodeHeap->EnumMemoryRegions(flags);
    }
    if (m_pExecutableHeap.IsValid())
    {
        m_pExecutableHeap->EnumMemoryRegions(flags);
    }
#ifdef FEATURE_READYTORUN
    if (m_pDynamicHelpersHeap.IsValid())
    {
        m_pDynamicHelpersHeap->EnumMemoryRegions(flags);
    }
#endif
    if (m_pFixupPrecodeHeap.IsValid())
    {
        m_pFixupPrecodeHeap->EnumMemoryRegions(flags);
    }
    if (m_pNewStubPrecodeHeap.IsValid())
    {
        m_pNewStubPrecodeHeap->EnumMemoryRegions(flags);
    }
    if (m_pVirtualCallStubManager.IsValid())
    {
        m_pVirtualCallStubManager->EnumMemoryRegions(flags);
    }
}
#endif //DACCESS_COMPILE

SIZE_T LoaderAllocator::EstimateSize()
{
    WRAPPER_NO_CONTRACT;
    SIZE_T retval=0;
    if(m_pHighFrequencyHeap)
        retval+=m_pHighFrequencyHeap->GetSize();
    if(m_pLowFrequencyHeap)
        retval+=m_pLowFrequencyHeap->GetSize();
    if(m_pStubHeap)
        retval+=m_pStubHeap->GetSize();
    if(m_pStringLiteralMap)
        retval+=m_pStringLiteralMap->GetSize();
    if(m_pVirtualCallStubManager)
        retval+=m_pVirtualCallStubManager->GetSize();

    return retval;
}

#ifndef DACCESS_COMPILE


DispatchToken LoaderAllocator::GetDispatchToken(
    UINT32 typeId, UINT32 slotNumber)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

#ifdef FAT_DISPATCH_TOKENS

    if (DispatchToken::RequiresDispatchTokenFat(typeId, slotNumber))
    {
        //
        // Lock and set are lazily created.
        //
        if (m_pFatTokenSetLock == NULL)
        {
            NewHolder<SimpleRWLock> pFatTokenSetLock = new SimpleRWLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT);
            SimpleWriteLockHolder lock(pFatTokenSetLock);
            NewHolder<FatTokenSet> pFatTokenSet = new FatTokenSet;

            if (InterlockedCompareExchangeT(
                    &m_pFatTokenSetLock, pFatTokenSetLock.GetValue(), NULL) != NULL)
            {   // Someone beat us to it
                lock.Release();
                // NewHolder will delete lock.
            }
            else
            {   // Make sure second allocation succeeds before suppressing holder of first.
                pFatTokenSetLock.SuppressRelease();
                m_pFatTokenSet = pFatTokenSet;
                pFatTokenSet.SuppressRelease();
            }
        }

        //
        // Take read lock, see if the requisite token has already been created and if so use it.
        // Otherwise, take write lock and create new token and add to the set.
        //

        // Lookup
        SimpleReadLockHolder rlock(m_pFatTokenSetLock);
        DispatchTokenFat key(typeId, slotNumber);
        DispatchTokenFat *pFat = m_pFatTokenSet->Lookup(&key);
        if (pFat != NULL)
        {   // <typeId,slotNumber> is already in the set.
            return DispatchToken(pFat);
        }
        else
        {   // Create
            rlock.Release();
            SimpleWriteLockHolder wlock(m_pFatTokenSetLock);

            // Check to see if someone beat us to the punch between
            // releasing the read lock and taking the write lock.
            pFat = m_pFatTokenSet->Lookup(&key);

            if (pFat == NULL)
            {   // No one beat us; allocate and insert a new DispatchTokenFat instance.
                pFat = new ((LPVOID)GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(DispatchTokenFat))))
                    DispatchTokenFat(typeId, slotNumber);

                m_pFatTokenSet->Add(pFat);
            }

            return DispatchToken(pFat);
        }
    }
#endif // FAT_DISPATCH_TOKENS

    return DispatchToken::CreateDispatchToken(typeId, slotNumber);
}

void LoaderAllocator::InitVirtualCallStubManager(BaseDomain * pDomain)
{
    STANDARD_VM_CONTRACT;

    NewHolder<VirtualCallStubManager> pMgr(new VirtualCallStubManager());

    // Init the manager, including all heaps and such.
    pMgr->Init(pDomain, this);

    m_pVirtualCallStubManager = pMgr;

    // Successfully created the manager.
    pMgr.SuppressRelease();
}

void LoaderAllocator::UninitVirtualCallStubManager()
{
    WRAPPER_NO_CONTRACT;

    if (m_pVirtualCallStubManager != NULL)
    {
        m_pVirtualCallStubManager->Uninit();
        delete m_pVirtualCallStubManager;
        m_pVirtualCallStubManager = NULL;
    }
}


EEMarshalingData *LoaderAllocator::GetMarshalingData()
{
    CONTRACT (EEMarshalingData*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(m_pMarshalingData));
    }
    CONTRACT_END;

    if (!m_pMarshalingData)
    {
        // Take the lock
        CrstHolder holder(&m_InteropDataCrst);

        if (!m_pMarshalingData)
        {
            m_pMarshalingData = new (GetLowFrequencyHeap()) EEMarshalingData(this, &m_InteropDataCrst);
        }
    }

    RETURN m_pMarshalingData;
}

void LoaderAllocator::DeleteMarshalingData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We are in shutdown - no need to take any lock
    if (m_pMarshalingData)
    {
        delete m_pMarshalingData;
        m_pMarshalingData = NULL;
    }
}

#endif // !DACCESS_COMPILE

BOOL GlobalLoaderAllocator::CanUnload()
{
    LIMITED_METHOD_CONTRACT;

    return FALSE;
}

BOOL AssemblyLoaderAllocator::CanUnload()
{
    LIMITED_METHOD_CONTRACT;

    return TRUE;
}

DomainAssemblyIterator::DomainAssemblyIterator(DomainAssembly* pFirstAssembly)
{
    pCurrentAssembly = pFirstAssembly;
    pNextAssembly = pCurrentAssembly ? pCurrentAssembly->GetNextDomainAssemblyInSameALC() : NULL;
}

void DomainAssemblyIterator::operator++()
{
    pCurrentAssembly = pNextAssembly;
    pNextAssembly = pCurrentAssembly ? pCurrentAssembly->GetNextDomainAssemblyInSameALC() : NULL;
}

#ifndef DACCESS_COMPILE

void AssemblyLoaderAllocator::Init(AppDomain* pAppDomain)
{
    m_Id.Init();

    // This is CRST_UNSAFE_ANYMODE to enable registering/unregistering dependent handles to native objects without changing the
    // GC mode, in case the caller requires that
    m_dependentHandleToNativeObjectSetCrst.Init(CrstLeafLock, CRST_UNSAFE_ANYMODE);

    LoaderAllocator::Init((BaseDomain *)pAppDomain);
    if (IsCollectible())
    {
        // TODO: the ShuffleThunkCache should really be using the m_pStubHeap, however the unloadability support
        // doesn't track the stubs or the related delegate classes and so we get crashes when a stub is used after
        // the AssemblyLoaderAllocator is gone (the stub memory is unmapped).
        // https://github.com/dotnet/runtime/issues/55697 tracks this issue.
        m_pShuffleThunkCache = new ShuffleThunkCache(SystemDomain::GetGlobalLoaderAllocator()->GetExecutableHeap());
    }
}


AssemblyLoaderAllocator::~AssemblyLoaderAllocator()
{
    if (m_binderToRelease != NULL)
    {
        delete m_binderToRelease;
        m_binderToRelease = NULL;
    }

    delete m_pShuffleThunkCache;
    m_pShuffleThunkCache = NULL;
}

void AssemblyLoaderAllocator::RegisterBinder(CustomAssemblyBinder* binderToRelease)
{
    // When the binder is registered it will be released by the destructor
    // of this instance
    _ASSERTE(m_binderToRelease == NULL);
    m_binderToRelease = binderToRelease;
}

STRINGREF *LoaderAllocator::GetStringObjRefPtrFromUnicodeString(EEStringData *pStringData, void** ppPinnedString)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pStringData));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (m_pStringLiteralMap == NULL)
    {
        LazyInitStringLiteralMap();
    }
    _ASSERTE(m_pStringLiteralMap);
    return m_pStringLiteralMap->GetStringLiteral(pStringData, TRUE, CanUnload(), ppPinnedString);
}

//*****************************************************************************
void LoaderAllocator::LazyInitStringLiteralMap()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    NewHolder<StringLiteralMap> pStringLiteralMap(new StringLiteralMap());

    pStringLiteralMap->Init();

    if (InterlockedCompareExchangeT<StringLiteralMap *>(&m_pStringLiteralMap, pStringLiteralMap, NULL) == NULL)
    {
        pStringLiteralMap.SuppressRelease();
    }
}

void LoaderAllocator::CleanupStringLiteralMap()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pStringLiteralMap)
    {
        delete m_pStringLiteralMap;
        m_pStringLiteralMap = NULL;
    }
}

STRINGREF *LoaderAllocator::IsStringInterned(STRINGREF *pString)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pString));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (m_pStringLiteralMap == NULL)
    {
        LazyInitStringLiteralMap();
    }
    _ASSERTE(m_pStringLiteralMap);
    return m_pStringLiteralMap->GetInternedString(pString, FALSE, CanUnload());
}

STRINGREF *LoaderAllocator::GetOrInternString(STRINGREF *pString)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pString));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    if (m_pStringLiteralMap == NULL)
    {
        LazyInitStringLiteralMap();
    }
    _ASSERTE(m_pStringLiteralMap);
    return m_pStringLiteralMap->GetInternedString(pString, TRUE, CanUnload());
}

void AssemblyLoaderAllocator::RegisterHandleForCleanup(OBJECTHANDLE objHandle)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(objHandle));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    void * pItem = GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(HandleCleanupListItem)));

    // InsertTail must be protected by a lock. Just use the loader allocator lock
    CrstHolder ch(&m_crstLoaderAllocator);
    m_handleCleanupList.InsertTail(new (pItem) HandleCleanupListItem(objHandle));
}

void AssemblyLoaderAllocator::UnregisterHandleFromCleanup(OBJECTHANDLE objHandle)
{
    CONTRACTL
    {
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(objHandle));
    }
    CONTRACTL_END;

    // FindAndRemove must be protected by a lock. Just use the loader allocator lock
    CrstHolder ch(&m_crstLoaderAllocator);

    for (HandleCleanupListItem* item = m_handleCleanupList.GetHead(); item != NULL; item = SList<HandleCleanupListItem>::GetNext(item))
    {
        if (item->m_handle == objHandle)
        {
            m_handleCleanupList.FindAndRemove(item);
            return;
        }
    }

    _ASSERTE(!"Trying to unregister a handle that was never registered");
}

void AssemblyLoaderAllocator::CleanupHandles()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE(GetDomain()->IsAppDomain());

    // This method doesn't take a lock around RemoveHead because it's supposed to
    // be called only from Terminate
    while (!m_handleCleanupList.IsEmpty())
    {
        HandleCleanupListItem * pItem = m_handleCleanupList.RemoveHead();
        DestroyTypedHandle(pItem->m_handle);
    }
}

void AssemblyLoaderAllocator::RegisterDependentHandleToNativeObjectForCleanup(LADependentHandleToNativeObject *dependentHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(dependentHandle != nullptr);

    CrstHolder setLockHolder(&m_dependentHandleToNativeObjectSetCrst);

    _ASSERTE(m_dependentHandleToNativeObjectSet.Lookup(dependentHandle) == NULL);
    m_dependentHandleToNativeObjectSet.Add(dependentHandle);
}

void AssemblyLoaderAllocator::UnregisterDependentHandleToNativeObjectFromCleanup(LADependentHandleToNativeObject *dependentHandle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(dependentHandle != nullptr);

    CrstHolder setLockHolder(&m_dependentHandleToNativeObjectSetCrst);

    _ASSERTE(m_dependentHandleToNativeObjectSet.Lookup(dependentHandle) != NULL);
    m_dependentHandleToNativeObjectSet.Remove(dependentHandle);
}

void AssemblyLoaderAllocator::CleanupDependentHandlesToNativeObjects()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Locks under which dependent handles may be used must all be taken here to ensure that a thread using a dependent handle
    // would either observe it cleared, or that the dependent object remains valid under those locks. In particular, any locks
    // used to synchronize uses of CrossLoaderAllocatorHash instances must also be taken here.
    CrstHolder jitInlineTrackingMapLockHolder(JITInlineTrackingMap::GetMapCrst());
    MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder;

    CrstHolder setLockHolder(&m_dependentHandleToNativeObjectSetCrst);

    for (DependentHandleToNativeObjectSet::Iterator it = m_dependentHandleToNativeObjectSet.Begin(),
            itEnd = m_dependentHandleToNativeObjectSet.End();
        it != itEnd;
        ++it)
    {
        LADependentHandleToNativeObject *dependentHandle = *it;
        dependentHandle->Clear();
    }
}

void LoaderAllocator::RegisterFailedTypeInitForCleanup(ListLockEntry *pListLockEntry)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pListLockEntry));
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (!IsCollectible())
    {
        return;
    }

    void * pItem = GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(FailedTypeInitCleanupListItem)));

    // InsertTail must be protected by a lock. Just use the loader allocator lock
    CrstHolder ch(&m_crstLoaderAllocator);
    m_failedTypeInitCleanupList.InsertTail(new (pItem) FailedTypeInitCleanupListItem(pListLockEntry));
}

void LoaderAllocator::CleanupFailedTypeInit()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    if (!IsCollectible())
    {
        return;
    }

    _ASSERTE(GetDomain()->IsAppDomain());

    // This method doesn't take a lock around loader allocator state access, because
    // it's supposed to be called only during cleanup. However, the domain-level state
    // might be accessed by multiple threads.
    ListLock *pLock = GetDomain()->GetClassInitLock();

    while (!m_failedTypeInitCleanupList.IsEmpty())
    {
        FailedTypeInitCleanupListItem * pItem = m_failedTypeInitCleanupList.RemoveHead();

        ListLockHolder pInitLock(pLock);
        pLock->Unlink(pItem->m_pListLockEntry);
    }
}

void AssemblyLoaderAllocator::ReleaseManagedAssemblyLoadContext()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_binderToRelease != NULL)
    {
        // Release the managed ALC
        m_binderToRelease->ReleaseLoadContext();
    }
}

#ifdef FEATURE_COMINTEROP
ComCallWrapperCache * LoaderAllocator::GetComCallWrapperCache()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (!m_pComCallWrapperCache)
    {
        CrstHolder lh(&m_ComCallWrapperCrst);

        if (!m_pComCallWrapperCache)
            m_pComCallWrapperCache = ComCallWrapperCache::Create(this);
    }
    _ASSERTE(m_pComCallWrapperCache);
    return m_pComCallWrapperCache;
}
#endif // FEATURE_COMINTEROP

// U->M thunks created in this LoaderAllocator and not associated with a delegate.
UMEntryThunkCache *LoaderAllocator::GetUMEntryThunkCache()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (!m_pUMEntryThunkCache)
    {
        UMEntryThunkCache *pUMEntryThunkCache = new UMEntryThunkCache(GetAppDomain());

        if (InterlockedCompareExchangeT(&m_pUMEntryThunkCache, pUMEntryThunkCache, NULL) != NULL)
        {
            // some thread swooped in and set the field
            delete pUMEntryThunkCache;
        }
    }
    _ASSERTE(m_pUMEntryThunkCache);
    return m_pUMEntryThunkCache;
}

/* static */
void LoaderAllocator::RemoveMemoryToLoaderAllocatorAssociation(LoaderAllocator* pLoaderAllocator)
{
    CONTRACTL {
        THROWS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    GlobalLoaderAllocator* pGlobalAllocator = (GlobalLoaderAllocator*)SystemDomain::GetGlobalLoaderAllocator();
    pGlobalAllocator->m_memoryAssociations.RemoveRanges(pLoaderAllocator);
}

/* static */
void LoaderAllocator::AssociateMemoryWithLoaderAllocator(BYTE *start, const BYTE *end, LoaderAllocator* pLoaderAllocator)
{
    CONTRACTL {
        THROWS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    GlobalLoaderAllocator* pGlobalAllocator = (GlobalLoaderAllocator*)SystemDomain::GetGlobalLoaderAllocator();
    pGlobalAllocator->m_memoryAssociations.AddRange(start, end, pLoaderAllocator);
}

/* static */
PTR_LoaderAllocator LoaderAllocator::GetAssociatedLoaderAllocator_Unsafe(TADDR ptr)
{
    LIMITED_METHOD_CONTRACT;

    GlobalLoaderAllocator* pGlobalAllocator = (GlobalLoaderAllocator*)SystemDomain::GetGlobalLoaderAllocator();
    LoaderAllocator* pLoaderAllocator;
    if (pGlobalAllocator->m_memoryAssociations.IsInRangeWorker_Unlocked(ptr, reinterpret_cast<TADDR *>(&pLoaderAllocator)))
    {
        return pLoaderAllocator;
    }
    return NULL;
}


#ifdef FEATURE_COMINTEROP

// Look up interop data for a method table
// Returns the data pointer if present, NULL otherwise
InteropMethodTableData *LoaderAllocator::LookupComInteropData(MethodTable *pMT)
{
    // Take the lock
    CrstHolder holder(&m_InteropDataCrst);

    // Lookup
    InteropMethodTableData *pData = (InteropMethodTableData*)m_interopDataHash.LookupValue((UPTR)pMT, (LPVOID)NULL);

    // Not there...
    if (pData == (InteropMethodTableData*)INVALIDENTRY)
        return NULL;

    // Found it
    return pData;
}

// Returns TRUE if successfully inserted, FALSE if this would be a duplicate entry
BOOL LoaderAllocator::InsertComInteropData(MethodTable* pMT, InteropMethodTableData *pData)
{
    // We don't keep track of this kind of information for interfaces
    _ASSERTE(!pMT->IsInterface());

    // Take the lock
    CrstHolder holder(&m_InteropDataCrst);

    // Check to see that it's not already in there
    InteropMethodTableData *pDupData = (InteropMethodTableData*)m_interopDataHash.LookupValue((UPTR)pMT, (LPVOID)NULL);
    if (pDupData != (InteropMethodTableData*)INVALIDENTRY)
        return FALSE;

    // Not in there, so insert
    m_interopDataHash.InsertValue((UPTR)pMT, (LPVOID)pData);

    // Success
    return TRUE;
}

#endif // FEATURE_COMINTEROP

#endif // !DACCESS_COMPILE


#ifdef FEATURE_ON_STACK_REPLACEMENT
#ifndef DACCESS_COMPILE
PTR_OnStackReplacementManager LoaderAllocator::GetOnStackReplacementManager()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    if (m_onStackReplacementManager == NULL)
    {
        OnStackReplacementManager * newManager = new OnStackReplacementManager(this);

        if (InterlockedCompareExchangeT(&m_onStackReplacementManager, newManager, NULL) != NULL)
        {
            // some thread swooped in and set the field
            delete newManager;
        }
    }
    _ASSERTE(m_onStackReplacementManager != NULL);
    return m_onStackReplacementManager;
}
#endif //
#endif // FEATURE_ON_STACK_REPLACEMENT

