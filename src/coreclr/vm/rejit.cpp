// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ReJit.cpp
//

//
// This module implements the tracking and execution of rejit requests. In order to avoid
// any overhead on the non-profiled case we don't intrude on any 'normal' data structures
// except one member on the AppDomain to hold our main hashtable and crst (the
// ReJitManager). See comments in rejit.h to understand relationships between ReJitInfo,
// SharedReJitInfo, and ReJitManager, particularly SharedReJitInfo::InternalFlags which
// capture the state of a rejit request, and ReJitInfo::InternalFlags which captures the
// state of a particular MethodDesc from a rejit request.
//
// A ReJIT request (tracked via SharedReJitInfo) is made at the level of a (Module *,
// methodDef) pair, and thus affects all instantiations of a generic. Each MethodDesc
// affected by a ReJIT request has its state tracked via a ReJitInfo instance. A
// ReJitInfo can represent a rejit request against an already-jitted MethodDesc, or a
// rejit request against a not-yet-jitted MethodDesc (called a "pre-rejit" request). A
// Pre-ReJIT request happens when a profiler specifies a (Module *, methodDef) pair that
// has not yet been JITted, or that represents a generic function which always has the
// potential to JIT new instantiations in the future.
//
// Top-level functions in this file of most interest are:
//
// * (static) code:ReJitManager::RequestReJIT:
// Profiling API just delegates all rejit requests directly to this function. It is
// responsible for recording the request into the appropriate ReJITManagers and for
// jump-stamping any already-JITted functions affected by the request (so that future
// calls hit the prestub)
//
// * code:ReJitManager::DoReJitIfNecessary:
// MethodDesc::DoPrestub calls this to determine whether it's been invoked to do a rejit.
// If so, ReJitManager::DoReJitIfNecessary is responsible for (indirectly) gathering the
// appropriate IL and codegen flags, calling UnsafeJitFunction(), and redirecting the
// jump-stamp from the prestub to the newly-rejitted code.
//
// * code:ReJitManager::GetCurrentReJitFlags:
// CEEInfo::canInline() calls this as part of its calculation of whether it may inline a
// given method. (Profilers may specify on a per-rejit-request basis whether the rejit of
// a method may inline callees.)
//
//
// #Invariants:
//
// For a given Module/MethodDef there is at most 1 SharedReJitInfo that is not Reverted,
// though there may be many that are in the Reverted state. If a method is rejitted
// multiple times, with multiple versions actively in use on the stacks, then all but the
// most recent are put into the Reverted state even though they may not yet be physically
// reverted and pitched yet.
//
// For a given MethodDesc there is at most 1 ReJitInfo in the kJumpToPrestub or kJumpToRejittedCode
// state.
//
// The ReJitManager::m_crstTable lock is held whenever reading or writing to that
// ReJitManager instance's table (including state transitions applied to the ReJitInfo &
// SharedReJitInfo instances stored in that table).
//
// The ReJitManager::m_crstTable lock is never held during callbacks to the profiler
// such as GetReJITParameters, ReJITStarted, JITComplete, ReportReJITError
//
// Any thread holding the ReJitManager::m_crstTable lock can't block during runtime suspension
// therefore it can't call any GC_TRIGGERS functions
//
// Transitions between SharedRejitInfo states happen only in the following cicumstances:
//   1) New SharedRejitInfo added to table (Requested State)
//      Inside RequestRejit
//      Global Crst held, table Crst held
//
//   2) Requested -> GettingReJITParameters
//      Inside DoRejitIfNecessary
//      Global Crst NOT held, table Crst held
//
//   3) GettingReJITParameters -> Active
//      Inside DoRejitIfNecessary
//      Global Crst NOT held, table Crst held
//
//   4) * -> Reverted
//      Inside RequestRejit or RequestRevert
//      Global Crst held, table Crst held
//
//
// Transitions between RejitInfo states happen only in the following circumstances:
//   1) New RejitInfo added to table (kJumpNone state)
//      Inside RequestRejit
//      Global Crst MAY/MAY NOT be held, table Crst held
//      Allowed SharedReJit states: Requested, GettingReJITParameters, Active
//
//   2) kJumpNone -> kJumpToPrestub
//      Inside RequestRejit
//      Global Crst MAY/MAY NOT be held, table Crst held
//      Allowed SharedReJit states: Requested, GettingReJITParameters, Active
//
//   3) kJumpToPreStub -> kJumpToRejittedCode
//      Inside DoReJitIfNecessary
//      Global Crst NOT held, table Crst held
//      Allowed SharedReJit states: Active
//
//   4) * -> kJumpNone
//      Inside RequestRevert, RequestRejit
//      Global Crst held, table crst held
//      Allowed SharedReJit states: Reverted
//
//
// #Beware Invariant misconceptions - don't make bad assumptions!
//   Even if a SharedReJitInfo is in the Reverted state:
//     a) RejitInfos may still be in the kJumpToPreStub or kJumpToRejittedCode state
//        Reverted really just means the runtime has started reverting, but it may not
//        be complete yet on the thread executing Revert or RequestRejit.
//     b) The code for this version of the method may be executing on any number of
//        threads. Even after transitioning all rejit infos to kJumpNone state we
//        have no power to abort or hijack threads already running the rejitted code.
//
//   Even if a SharedReJitInfo is in the Active state:
//     a) The corresponding ReJitInfos may not be jump-stamped yet.
//        Some thread is still in the progress of getting this thread jump-stamped
//        OR it is a place-holder ReJitInfo.
//     b) An older ReJitInfo linked to a reverted SharedReJitInfo could still be
//        in kJumpToPreStub or kJumpToReJittedCode state. RequestRejit is still in
//        progress on some thread.
//
//
// #Known issues with REJIT at this time:
//   NGEN inlined methods will not be properly rejitted
//   Exception callstacks through rejitted code do not produce correct StackTraces
//   Live debugging is not supported when rejit is enabled
//   Rejit leaks rejitted methods, RejitInfos, and SharedRejitInfos until AppDomain unload
//   Dump debugging doesn't correctly locate RejitInfos that are keyed by MethodDesc
//   Metadata update creates large memory increase switching to RW (not specifically a rejit issue)
//
// ======================================================================================

#include "common.h"
#include "rejit.h"
#include "method.hpp"
#include "eeconfig.h"
#include "methoditer.h"
#include "dbginterface.h"
#include "threadsuspend.h"

#ifdef FEATURE_REJIT
#ifdef FEATURE_CODE_VERSIONING

#include "../debug/ee/debugger.h"
#include "../debug/ee/walker.h"
#include "../debug/ee/controller.h"
#include "codeversion.h"

/* static */
CrstStatic ReJitManager::s_csGlobalRequest;


//---------------------------------------------------------------------------------------
// Helpers

//static
CORJIT_FLAGS ReJitManager::JitFlagsFromProfCodegenFlags(DWORD dwCodegenFlags)
{
    LIMITED_METHOD_DAC_CONTRACT;

    CORJIT_FLAGS jitFlags;
    if ((dwCodegenFlags & COR_PRF_CODEGEN_DISABLE_ALL_OPTIMIZATIONS) != 0)
    {
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE);
    }
    if ((dwCodegenFlags & COR_PRF_CODEGEN_DEBUG_INFO) != 0)
    {
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);
    }
    if ((dwCodegenFlags & COR_PRF_CODEGEN_DISABLE_INLINING) != 0)
    {
        jitFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_NO_INLINING);
    }

    // In the future more flags may be added that need to be converted here (e.g.,
    // COR_PRF_CODEGEN_ENTERLEAVE / CORJIT_FLAG_PROF_ENTERLEAVE)

    return jitFlags;
}

//---------------------------------------------------------------------------------------
// ProfilerFunctionControl implementation

ProfilerFunctionControl::ProfilerFunctionControl(LoaderHeap * pHeap) :
    m_refCount(1),
    m_pHeap(pHeap),
    m_dwCodegenFlags(0),
    m_cbIL(0),
    m_pbIL(NULL),
    m_cInstrumentedMapEntries(0),
    m_rgInstrumentedMapEntries(NULL)
{
    LIMITED_METHOD_CONTRACT;
}

ProfilerFunctionControl::~ProfilerFunctionControl()
{
    LIMITED_METHOD_CONTRACT;

    // Intentionally not deleting m_pbIL or m_rgInstrumentedMapEntries, as its ownership gets transferred to the
    // SharedReJitInfo that manages that rejit request.
}


HRESULT ProfilerFunctionControl::QueryInterface(REFIID id, void** pInterface)
{
    LIMITED_METHOD_CONTRACT;

    if ((id != IID_IUnknown) &&
        (id != IID_ICorProfilerFunctionControl))
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    *pInterface = this;
    this->AddRef();
    return S_OK;
}

ULONG ProfilerFunctionControl::AddRef()
{
    LIMITED_METHOD_CONTRACT;

    return InterlockedIncrement(&m_refCount);
}

ULONG ProfilerFunctionControl::Release()
{
    LIMITED_METHOD_CONTRACT;

    ULONG refCount = InterlockedDecrement(&m_refCount);

    if (0 == refCount)
    {
        delete this;
    }

    return refCount;
}

//---------------------------------------------------------------------------------------
//
// Profiler calls this to specify a set of flags from COR_PRF_CODEGEN_FLAGS
// to control rejitting a particular methodDef.
//
// Arguments:
//    * flags - set of flags from COR_PRF_CODEGEN_FLAGS
//
// Return Value:
//    Always S_OK;
//

HRESULT ProfilerFunctionControl::SetCodegenFlags(DWORD flags)
{
    LIMITED_METHOD_CONTRACT;

    m_dwCodegenFlags = flags;
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Profiler calls this to specify the IL to use when rejitting a particular methodDef.
//
// Arguments:
//    * cbNewILMethodHeader - Size in bytes of pbNewILMethodHeader
//    * pbNewILMethodHeader - Pointer to beginning of IL header + IL bytes.
//
// Return Value:
//    HRESULT indicating success or failure.
//
// Notes:
//    Caller owns allocating and freeing pbNewILMethodHeader as expected.
//    SetILFunctionBody copies pbNewILMethodHeader into a separate buffer.
//

HRESULT ProfilerFunctionControl::SetILFunctionBody(ULONG cbNewILMethodHeader, LPCBYTE pbNewILMethodHeader)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (cbNewILMethodHeader == 0)
    {
        return E_INVALIDARG;
    }

    if (pbNewILMethodHeader == NULL)
    {
        return E_INVALIDARG;
    }

    _ASSERTE(m_cbIL == 0);
    _ASSERTE(m_pbIL == NULL);

#ifdef DACCESS_COMPILE
    m_pbIL = new (nothrow) BYTE[cbNewILMethodHeader];
#else
    // IL is stored on the appropriate loader heap, and its memory will be owned by the
    // SharedReJitInfo we copy the pointer to.
    m_pbIL = (LPBYTE) (void *) m_pHeap->AllocMem_NoThrow(S_SIZE_T(cbNewILMethodHeader));
#endif
    if (m_pbIL == NULL)
    {
        return E_OUTOFMEMORY;
    }

    m_cbIL = cbNewILMethodHeader;
    memcpy(m_pbIL, pbNewILMethodHeader, cbNewILMethodHeader);

    return S_OK;
}

HRESULT ProfilerFunctionControl::SetILInstrumentedCodeMap(ULONG cILMapEntries, COR_IL_MAP * rgILMapEntries)
{
#ifdef DACCESS_COMPILE
    // I'm not sure why any of these methods would need to be compiled in DAC? Could we remove the
    // entire class from the DAC'ized code build?
    _ASSERTE(!"This shouldn't be called in DAC");
    return E_NOTIMPL;
#else

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (cILMapEntries >= (MAXULONG / sizeof(COR_IL_MAP)))
    {
        // Too big!  The allocation below would overflow when calculating the size.
        return E_INVALIDARG;
    }

    if (g_pDebugInterface == NULL)
    {
        return CORPROF_E_DEBUGGING_DISABLED;
    }


    // copy the il map and il map entries into the corresponding fields.
    m_cInstrumentedMapEntries = cILMapEntries;

    // IL is stored on the appropriate loader heap, and its memory will be owned by the
    // SharedReJitInfo we copy the pointer to.
    m_rgInstrumentedMapEntries = (COR_IL_MAP*) (void *) m_pHeap->AllocMem_NoThrow(S_SIZE_T(cILMapEntries * sizeof(COR_IL_MAP)));

    if (m_rgInstrumentedMapEntries == NULL)
        return E_OUTOFMEMORY;


    memcpy_s(m_rgInstrumentedMapEntries, sizeof(COR_IL_MAP) * cILMapEntries, rgILMapEntries, sizeof(COR_IL_MAP) * cILMapEntries);

    return S_OK;
#endif // DACCESS_COMPILE
}

//---------------------------------------------------------------------------------------
//
// ReJitManager may use this to access the codegen flags the profiler had set on this
// ICorProfilerFunctionControl.
//
// Return Value:
//     * codegen flags previously set via SetCodegenFlags; 0 if none were set.
//
DWORD ProfilerFunctionControl::GetCodegenFlags()
{
    return m_dwCodegenFlags;
}

//---------------------------------------------------------------------------------------
//
// ReJitManager may use this to access the IL header + instructions the
// profiler had set on this ICorProfilerFunctionControl via SetIL
//
// Return Value:
//     * Pointer to ProfilerFunctionControl-allocated buffer containing the
//         IL header and instructions the profiler had provided.
//
LPBYTE ProfilerFunctionControl::GetIL()
{
    return m_pbIL;
}

//---------------------------------------------------------------------------------------
//
// ReJitManager may use this to access the count of instrumented map entry flags the
// profiler had set on this ICorProfilerFunctionControl.
//
// Return Value:
//    * size of the instrumented map entry array
//
ULONG ProfilerFunctionControl::GetInstrumentedMapEntryCount()
{
    return m_cInstrumentedMapEntries;
}

//---------------------------------------------------------------------------------------
//
// ReJitManager may use this to access the instrumented map entries the
// profiler had set on this ICorProfilerFunctionControl.
//
// Return Value:
//    * the array of instrumented map entries
//
COR_IL_MAP* ProfilerFunctionControl::GetInstrumentedMapEntries()
{
    return m_rgInstrumentedMapEntries;
}

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
// ReJitManager implementation

// All the state-changey stuff is kept up here in the !DACCESS_COMPILE block.
// The more read-only inspection-y stuff follows the block.

//---------------------------------------------------------------------------------------
//
// ICorProfilerInfo4::RequestReJIT calls into this method to do most of the
// work. Takes care of finding the appropriate ReJitManager instances to
// record the rejit requests and perform jmp-stamping.
//
// Arguments:
//    * cFunctions - Element count of rgModuleIDs & rgMethodDefs
//    * rgModuleIDs - Parallel array of ModuleIDs to rejit
//    * rgMethodDefs - Parallel array of methodDefs to rejit
//
// Return Value:
//      HRESULT indicating success or failure of the overall operation.  Each
//      individual methodDef (or MethodDesc associated with the methodDef)
//      may encounter its own failure, which is reported by the ReJITError()
//      callback, which is called into the profiler directly.
//

// static
HRESULT ReJitManager::RequestReJIT(
    ULONG               cFunctions,
    ModuleID            rgModuleIDs[],
    mdMethodDef         rgMethodDefs[],
    COR_PRF_REJIT_FLAGS flags)
{
    return ReJitManager::UpdateActiveILVersions(cFunctions, rgModuleIDs, rgMethodDefs, NULL, FALSE, flags);
}

// static
HRESULT ReJitManager::UpdateActiveILVersions(
    ULONG               cFunctions,
    ModuleID            rgModuleIDs[],
    mdMethodDef         rgMethodDefs[],
    HRESULT             rgHrStatuses[],
    BOOL                fIsRevert,
    COR_PRF_REJIT_FLAGS flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Serialize all RequestReJIT() and Revert() calls against each other (even across AppDomains)
    CrstHolder ch(&(s_csGlobalRequest));

    HRESULT hr = S_OK;

    // Request at least 1 method to reJIT!
    _ASSERTE ((cFunctions != 0) && (rgModuleIDs != NULL) && (rgMethodDefs != NULL));

    // Temporary storage to batch up all the ReJitInfos that will get jump stamped
    // later when the runtime is suspended.
    //
    //DESKTOP WARNING: On CoreCLR we are safe but if this code ever gets ported back
    //there aren't any protections against domain unload. Any of these moduleIDs
    //code version managers, or code versions would become invalid if the domain which
    //contains them was unloaded.
    SHash<CodeActivationBatchTraits> mgrToCodeActivationBatch;
    CDynArray<CodeVersionManager::CodePublishError> errorRecords;
    for (ULONG i = 0; i < cFunctions; i++)
    {
        Module * pModule = reinterpret_cast< Module * >(rgModuleIDs[i]);
        if (pModule == NULL || TypeFromToken(rgMethodDefs[i]) != mdtMethodDef)
        {
            ReportReJITError(pModule, rgMethodDefs[i], NULL, E_INVALIDARG);
            continue;
        }

        if (pModule->IsBeingUnloaded())
        {
            ReportReJITError(pModule, rgMethodDefs[i], NULL, CORPROF_E_DATAINCOMPLETE);
            continue;
        }

        if (pModule->IsReflection())
        {
            ReportReJITError(pModule, rgMethodDefs[i], NULL, CORPROF_E_MODULE_IS_DYNAMIC);
            continue;
        }

        if (pModule->IsEditAndContinueEnabled())
        {
            ReportReJITError(pModule, rgMethodDefs[i], NULL, CORPROF_E_MODULE_IS_ENC);
            continue;
        }

        if (!pModule->GetMDImport()->IsValidToken(rgMethodDefs[i]))
        {
            ReportReJITError(pModule, rgMethodDefs[i], NULL, E_INVALIDARG);
            continue;
        }

        MethodDesc * pMD = pModule->LookupMethodDef(rgMethodDefs[i]);

        if (pMD != NULL)
        {
            _ASSERTE(!pMD->IsNoMetadata());

            // Weird, non-user functions can't be rejitted
            if (!pMD->IsIL())
            {
                // Intentionally not reporting an error in this case, to be consistent
                // with the pre-rejit case, as we have no opportunity to report an error
                // in a pre-rejit request for a non-IL method, since the rejit manager
                // never gets a call from the prestub worker for non-IL methods.  Thus,
                // since pre-rejit requests silently ignore rejit requests for non-IL
                // methods, regular rejit requests will also silently ignore rejit requests for
                // non-IL methods to be consistent.
                continue;
            }
        }

        hr = UpdateActiveILVersion(&mgrToCodeActivationBatch, pModule, rgMethodDefs[i], fIsRevert, static_cast<COR_PRF_REJIT_FLAGS>(flags | COR_PRF_REJIT_INLINING_CALLBACKS));
        if (FAILED(hr))
        {
            return hr;
        }

        if ((flags & COR_PRF_REJIT_BLOCK_INLINING) == COR_PRF_REJIT_BLOCK_INLINING)
        {
            hr = UpdateNativeInlinerActiveILVersions(&mgrToCodeActivationBatch, pModule, rgMethodDefs[i], fIsRevert, flags);
            if (FAILED(hr))
            {
                return hr;
            }

            if (pMD != NULL)
            {
                // If pMD is not null, then the method may have already been inlined somewhere. Go check.
                hr = UpdateJitInlinerActiveILVersions(&mgrToCodeActivationBatch, pMD, fIsRevert, flags);
                if (FAILED(hr))
                {
                    return hr;
                }
            }
        }
    }   // for (ULONG i = 0; i < cFunctions; i++)

    // For each code versioning mgr, if there's work to do,
    // enter the code versioning mgr's crst, and do the batched work.
    SHash<CodeActivationBatchTraits>::Iterator beginIter = mgrToCodeActivationBatch.Begin();
    SHash<CodeActivationBatchTraits>::Iterator endIter = mgrToCodeActivationBatch.End();

    {
        for (SHash<CodeActivationBatchTraits>::Iterator iter = beginIter; iter != endIter; iter++)
        {
            CodeActivationBatch * pCodeActivationBatch = *iter;
            CodeVersionManager * pCodeVersionManager = pCodeActivationBatch->m_pCodeVersionManager;

            int cMethodsToActivate = pCodeActivationBatch->m_methodsToActivate.Count();
            if (cMethodsToActivate == 0)
            {
                continue;
            }

            {
                // SetActiveILCodeVersions takes the SystemDomain crst, which needs to be acquired before the
                // ThreadStore crsts
                SystemDomain::LockHolder lh;

                hr = pCodeVersionManager->SetActiveILCodeVersions(pCodeActivationBatch->m_methodsToActivate.Ptr(), pCodeActivationBatch->m_methodsToActivate.Count(), &errorRecords);
                if (FAILED(hr))
                    break;
            }
        }
    }

    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }

    // Report any errors that were batched up
    for (int i = 0; i < errorRecords.Count(); i++)
    {
        if (rgHrStatuses != NULL)
        {
            for (DWORD j = 0; j < cFunctions; j++)
            {
                if (rgMethodDefs[j] == errorRecords[i].methodDef &&
                    reinterpret_cast<Module*>(rgModuleIDs[j]) == errorRecords[i].pModule)
                {
                    rgHrStatuses[j] = errorRecords[i].hrStatus;
                }
            }
        }
        else
        {
            ReportReJITError(&(errorRecords[i]));
        }

    }

    // We got through processing everything, but profiler will need to see the individual ReJITError
    // callbacks to know what, if anything, failed.
    return S_OK;
}

// static
HRESULT ReJitManager::UpdateActiveILVersion(
    SHash<CodeActivationBatchTraits>   *pMgrToCodeActivationBatch,
    Module                             *pModule,
    mdMethodDef                         methodDef,
    BOOL                                fIsRevert,
    COR_PRF_REJIT_FLAGS                 flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(pMgrToCodeActivationBatch != NULL);
    _ASSERTE(pModule != NULL);
    _ASSERTE(methodDef != mdTokenNil);

    HRESULT hr = S_OK;

    CodeVersionManager * pCodeVersionManager = pModule->GetCodeVersionManager();
    _ASSERTE(pCodeVersionManager != NULL);
    CodeActivationBatch * pCodeActivationBatch = pMgrToCodeActivationBatch->Lookup(pCodeVersionManager);
    if (pCodeActivationBatch == NULL)
    {
        pCodeActivationBatch = new (nothrow)CodeActivationBatch(pCodeVersionManager);
        if (pCodeActivationBatch == NULL)
        {
            return E_OUTOFMEMORY;
        }

        hr = S_OK;
        EX_TRY
        {
            // This throws when out of memory, but remains internally
            // consistent (without adding the new element)
            pMgrToCodeActivationBatch->Add(pCodeActivationBatch);
        }
        EX_CATCH_HRESULT(hr);

        _ASSERT(hr == S_OK || hr == E_OUTOFMEMORY);
        if (FAILED(hr))
        {
            return hr;
        }
    }

    {
        CodeVersionManager::LockHolder codeVersioningLockHolder;

        // Bind the il code version
        ILCodeVersion* pILCodeVersion = pCodeActivationBatch->m_methodsToActivate.Append();
        if (pILCodeVersion == NULL)
        {
            return E_OUTOFMEMORY;
        }
        if (fIsRevert)
        {
            // activate the original version
            *pILCodeVersion = ILCodeVersion(pModule, methodDef);
        }
        else
        {
            // activate an unused or new IL version
            hr = ReJitManager::BindILVersion(pCodeVersionManager, pModule, methodDef, pILCodeVersion, flags);
            if (FAILED(hr))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
        }
    }

    return hr;
}

// static
HRESULT ReJitManager::UpdateNativeInlinerActiveILVersions(
    SHash<CodeActivationBatchTraits>   *pMgrToCodeActivationBatch,
    Module                             *pInlineeModule,
    mdMethodDef                         inlineeMethodDef,
    BOOL                                fIsRevert,
    COR_PRF_REJIT_FLAGS                 flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(pMgrToCodeActivationBatch != NULL);
    _ASSERTE(pInlineeModule != NULL);
    _ASSERTE(RidFromToken(inlineeMethodDef) != 0);

    HRESULT hr = S_OK;

    // Iterate through all modules, for any that are NGEN or R2R need to check if there are inliners there and call
    // RequestReJIT on them
    AppDomain::AssemblyIterator domainAssemblyIterator = SystemDomain::System()->DefaultDomain()->IterateAssembliesEx((AssemblyIterationFlags) (kIncludeLoaded | kIncludeExecution));
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    NativeImageInliningIterator inlinerIter;
    while (domainAssemblyIterator.Next(pDomainAssembly.This()))
    {
        _ASSERTE(pDomainAssembly != NULL);
        _ASSERTE(pDomainAssembly->GetAssembly() != NULL);

        Module * pModule = pDomainAssembly->GetModule();
        if (pModule->HasReadyToRunInlineTrackingMap())
        {
            inlinerIter.Reset(pModule, MethodInModule(pInlineeModule, inlineeMethodDef));

            while (inlinerIter.Next())
            {
                MethodInModule inliner = inlinerIter.GetMethod();
                {
                    CodeVersionManager *pCodeVersionManager = pModule->GetCodeVersionManager();
                    CodeVersionManager::LockHolder codeVersioningLockHolder;
                    ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(inliner.m_module, inliner.m_methodDef);
                    if (!ilVersion.HasDefaultIL())
                    {
                        // This method has already been ReJITted, no need to request another ReJIT at this point.
                        // The ReJITted method will be in the JIT inliner check below.
                        continue;
                    }
                }

                hr = UpdateActiveILVersion(pMgrToCodeActivationBatch, inliner.m_module, inliner.m_methodDef, fIsRevert, flags);
                if (FAILED(hr))
                {
                    ReportReJITError(inliner.m_module, inliner.m_methodDef, NULL, hr);
                }
            }
        }
    }

    return S_OK;
}

// static
HRESULT ReJitManager::UpdateJitInlinerActiveILVersions(
    SHash<CodeActivationBatchTraits>   *pMgrToCodeActivationBatch,
    MethodDesc                         *pInlinee,
    BOOL                                fIsRevert,
    COR_PRF_REJIT_FLAGS                 flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(pMgrToCodeActivationBatch != NULL);
    _ASSERTE(pInlinee != NULL);

    HRESULT hr = S_OK;

    Module *pModule = pInlinee->GetModule();
    if (pModule->HasJitInlineTrackingMap())
    {
        // JITInlineTrackingMap::VisitInliners wants to be in cooperative mode,
        // but UpdateActiveILVersion wants to be in preemptive mode. Rather than do
        // a bunch of mode switching just batch up the inliners.
        InlineSArray<MethodDesc *, 10> inliners;
        auto lambda = [&](MethodDesc *inliner, MethodDesc *inlinee)
        {
            _ASSERTE(!inliner->IsNoMetadata());

            if (inliner->IsIL())
            {
                EX_TRY
                {
                    // InlineSArray can throw if we run out of memory,
                    // need to guard against it.
                    inliners.Append(inliner);
                }
                EX_CATCH_HRESULT(hr);

                return SUCCEEDED(hr);
            }

            // Keep going
            return true;
        };

        JITInlineTrackingMap *pMap = pModule->GetJitInlineTrackingMap();
        pMap->VisitInliners(pInlinee, lambda);
        if (FAILED(hr))
        {
            return hr;
        }

        EX_TRY
        {
            // InlineSArray iterator can throw
            for (auto it = inliners.Begin(); it != inliners.End(); ++it)
            {
                Module *inlinerModule = (*it)->GetModule();
                mdMethodDef inlinerMethodDef = (*it)->GetMemberDef();
                hr = UpdateActiveILVersion(pMgrToCodeActivationBatch, inlinerModule, inlinerMethodDef, fIsRevert, flags);
                if (FAILED(hr))
                {
                    ReportReJITError(inlinerModule, inlinerMethodDef, NULL, hr);
                }
            }
        }
        EX_CATCH_HRESULT(hr);
    }

    return hr;
}

// static
HRESULT ReJitManager::BindILVersion(
    CodeVersionManager *pCodeVersionManager,
    PTR_Module          pModule,
    mdMethodDef         methodDef,
    ILCodeVersion      *pILCodeVersion,
    COR_PRF_REJIT_FLAGS flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pCodeVersionManager));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pILCodeVersion));
    }
    CONTRACTL_END;

    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
    _ASSERTE((pModule != NULL) && (methodDef != mdTokenNil));

    // Check if there was there a previous rejit request for this method that hasn't been exposed back
    // to the profiler yet
    ILCodeVersion ilCodeVersion = pCodeVersionManager->GetActiveILCodeVersion(pModule, methodDef);
    BOOL fDoCallback = (flags & COR_PRF_REJIT_INLINING_CALLBACKS) == COR_PRF_REJIT_INLINING_CALLBACKS;

    if (ilCodeVersion.GetRejitState() == ILCodeVersion::kStateRequested)
    {
        // We can 'reuse' this instance because the profiler doesn't know about
        // it yet. (This likely happened because a profiler called RequestReJIT
        // twice in a row, without us having a chance to jmp-stamp the code yet OR
        // while iterating through instantiations of a generic, the iterator found
        // duplicate entries for the same instantiation.)
        // TODO: this assert likely needs to be removed. This code path should be
        // hit for any duplicates, and that can happen regardless of whether this
        // is the first ReJIT or not.
        _ASSERTE(ilCodeVersion.HasDefaultIL());

        *pILCodeVersion = ilCodeVersion;

        if (fDoCallback)
        {
            // There could be a case where the method that a profiler requested ReJIT on also ends up in the
            // inlining graph from a different method. In that case we should override the previous setting,
            // but we should never override a request to get the callback with a request to suppress it.
            pILCodeVersion->SetEnableReJITCallback(true);
        }

        return S_FALSE;
    }

    // Either there was no ILCodeVersion yet for this MethodDesc OR whatever we've found
    // couldn't be reused (and needed to be reverted).  Create a new ILCodeVersion to return
    // to the caller.
    HRESULT hr = pCodeVersionManager->AddILCodeVersion(pModule, methodDef, pILCodeVersion, FALSE);
    pILCodeVersion->SetEnableReJITCallback(fDoCallback);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// ICorProfilerInfo4::RequestRevert calls into this guy to do most of the
// work. Takes care of finding the appropriate ReJitManager instances to
// perform the revert
//
// Arguments:
//    * cFunctions - Element count of rgModuleIDs & rgMethodDefs
//    * rgModuleIDs - Parallel array of ModuleIDs to revert
//    * rgMethodDefs - Parallel array of methodDefs to revert
//    * rgHrStatuses - [out] Parallel array of HRESULTs indicating success/failure
//        of reverting each (ModuleID, methodDef).
//
// Return Value:
//      HRESULT indicating success or failure of the overall operation.  Each
//      individual methodDef (or MethodDesc associated with the methodDef)
//      may encounter its own failure, which is reported by the rgHrStatuses
//      [out] parameter.
//

// static
HRESULT ReJitManager::RequestRevert(
    ULONG       cFunctions,
    ModuleID    rgModuleIDs[],
    mdMethodDef rgMethodDefs[],
    HRESULT     rgHrStatuses[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    return UpdateActiveILVersions(cFunctions, rgModuleIDs, rgMethodDefs, rgHrStatuses, TRUE, static_cast<COR_PRF_REJIT_FLAGS>(0));
}

// static
HRESULT ReJitManager::ConfigureILCodeVersion(ILCodeVersion ilCodeVersion)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(!CodeVersionManager::IsLockOwnedByCurrentThread());


    HRESULT hr = S_OK;
    Module* pModule = ilCodeVersion.GetModule();
    mdMethodDef methodDef = ilCodeVersion.GetMethodDef();
    BOOL fNeedsParameters = FALSE;
    BOOL fWaitForParameters = FALSE;

    {
        // Serialize access to the rejit state
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        switch (ilCodeVersion.GetRejitState())
        {
        case ILCodeVersion::kStateRequested:
            ilCodeVersion.SetRejitState(ILCodeVersion::kStateGettingReJITParameters);
            fNeedsParameters = TRUE;
            break;

        case ILCodeVersion::kStateGettingReJITParameters:
            fWaitForParameters = TRUE;
            break;

        default:
            return S_OK;
        }
    }

    if (fNeedsParameters)
    {
        HRESULT hr = S_OK;
        ReleaseHolder<ProfilerFunctionControl> pFuncControl = NULL;

        if (ilCodeVersion.GetEnableReJITCallback())
        {
            // Here's where we give a chance for the rejit requestor to
            // examine and modify the IL & codegen flags before it gets to
            // the JIT. This allows one to add probe calls for things like
            // code coverage, performance, or whatever. These will be
            // stored in pShared.
            _ASSERTE(pModule != NULL);
            _ASSERTE(methodDef != mdTokenNil);
            pFuncControl =
                new (nothrow)ProfilerFunctionControl(pModule->GetLoaderAllocator()->GetLowFrequencyHeap());
            if (pFuncControl == NULL)
            {
                hr = E_OUTOFMEMORY;
            }
            else
            {
                BEGIN_PROFILER_CALLBACK(CORProfilerPresent());
                hr = (&g_profControlBlock)->GetReJITParameters(
                    (ModuleID)pModule,
                    methodDef,
                    pFuncControl);
                END_PROFILER_CALLBACK();
            }
        }

        if (!ilCodeVersion.GetEnableReJITCallback() || FAILED(hr))
        {
            {
                // Historically on failure we would revert to the kRequested state and fall-back
                // to the initial code gen. The next time the method ran it would try again.
                //
                // Preserving that behavior is possible, but a bit awkward now that we have
                // Precode swapping as well. Instead of doing that I am acting as if GetReJITParameters
                // had succeeded, using the original IL, no jit flags, and no modified IL mapping.
                // This is similar to a fallback except the profiler won't get any further attempts
                // to provide the parameters correctly. If the profiler wants another attempt it would
                // need to call RequestRejit again.
                //
                // This code path also happens if the GetReJITParameters callback was suppressed due to
                // the method being ReJITted as an inliner by the runtime (instead of by the user).
                CodeVersionManager::LockHolder codeVersioningLockHolder;
                if (ilCodeVersion.GetRejitState() == ILCodeVersion::kStateGettingReJITParameters)
                {
                    ilCodeVersion.SetRejitState(ILCodeVersion::kStateActive);
                    ilCodeVersion.SetIL(ILCodeVersion(pModule, methodDef).GetIL());
                }
            }

            if (FAILED(hr))
            {
                // Only call if the GetReJITParameters call failed
                ReportReJITError(pModule, methodDef, pModule->LookupMethodDef(methodDef), hr);
            }
            return S_OK;
        }
        else
        {
            _ASSERTE(pFuncControl != NULL);

            CodeVersionManager::LockHolder codeVersioningLockHolder;
            if (ilCodeVersion.GetRejitState() == ILCodeVersion::kStateGettingReJITParameters)
            {
                // Inside the above call to ICorProfilerCallback4::GetReJITParameters, the profiler
                // will have used the specified pFuncControl to provide its IL and codegen flags.
                // So now we transfer it out to the SharedReJitInfo.
                ilCodeVersion.SetJitFlags(pFuncControl->GetCodegenFlags());
                ilCodeVersion.SetIL((COR_ILMETHOD*)pFuncControl->GetIL());
                // ilCodeVersion is now the owner of the memory for the IL buffer
                ilCodeVersion.SetInstrumentedILMap(pFuncControl->GetInstrumentedMapEntryCount(),
                    pFuncControl->GetInstrumentedMapEntries());
                ilCodeVersion.SetRejitState(ILCodeVersion::kStateActive);
            }
        }
    }
    else if (fWaitForParameters)
    {
        // This feels annoying, but it doesn't appear like we have the good threading primitives
        // for this. What I would like is an AutoResetEvent that atomically exits the table
        // Crst when I wait on it. From what I can tell our AutoResetEvent doesn't have
        // that atomic transition which means this ordering could occur:
        // [Thread 1] detect kStateGettingParameters and exit table lock
        // [Thread 2] enter table lock, transition kStateGettingParameters -> kStateActive
        // [Thread 2] signal AutoResetEvent
        // [Thread 2] exit table lock
        // [Thread 1] wait on AutoResetEvent (which may never be signaled again)
        //
        // Another option would be ManualResetEvents, one for each SharedReJitInfo, but
        // that feels like a lot of memory overhead to handle a case which occurs rarely.
        // A third option would be dynamically creating ManualResetEvents in a side
        // dictionary on demand, but that feels like a lot of complexity for an event
        // that occurs rarely.
        //
        // I just ended up with this simple polling loop. Assuming profiler
        // writers implement GetReJITParameters performantly we will only iterate
        // this loop once, and even then only in the rare case of threads racing
        // to JIT the same IL. If this really winds up causing performance issues
        // We can build something more sophisticated.
        while (true)
        {
            {
                CodeVersionManager::LockHolder codeVersioningLockHolder;
                if (ilCodeVersion.GetRejitState() == ILCodeVersion::kStateActive)
                {
                    break; // the other thread got the parameters successfully, go race to rejit
                }
            }
            ClrSleepEx(1, FALSE);
        }
    }

    return S_OK;
}

#endif // DACCESS_COMPILE
// The rest of the ReJitManager methods are safe to compile for DAC

//---------------------------------------------------------------------------------------
//
// Used by profiler to get the ReJITID corrseponding to a (MethodDesc *, PCODE) pair.
// Can also be used to determine whether (MethodDesc *, PCODE) corresponds to a rejit
// (vs. a regular JIT) for the purposes of deciding whether to notify the debugger about
// the rejit (and building the debugger JIT info structure).
//
// Arguments:
//      * pMD - MethodDesc * of interestg
//      * pCodeStart - PCODE of the particular interesting JITting of that MethodDesc *
//
// Return Value:
//      0 if no such ReJITID found (e.g., PCODE is from a JIT and not a rejit), else the
//      ReJITID requested.
//
// static
ReJITID ReJitManager::GetReJitId(PTR_MethodDesc pMD, PCODE pCodeStart)
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pCodeStart != NULL);
    }
    CONTRACTL_END;

    // Fast-path: If the rejit map is empty, no need to look up anything. Do this outside
    // of a lock to impact our caller (the prestub worker) as little as possible. If the
    // map is nonempty, we'll acquire the lock at that point and do the lookup for real.
    CodeVersionManager* pCodeVersionManager = pMD->GetCodeVersionManager();
    if (pCodeVersionManager->GetNonDefaultILVersionCount() == 0)
    {
        return 0;
    }

    CodeVersionManager::LockHolder codeVersioningLockHolder;
    return ReJitManager::GetReJitIdNoLock(pMD, pCodeStart);
}

//---------------------------------------------------------------------------------------
//
// See comment above code:ReJitManager::GetReJitId for main details of what this does.
//
// This function is basically the same as GetReJitId, except caller is expected to take
// the ReJitManager lock directly (via ReJitManager::TableLockHolder). This exists so
// that ETW can explicitly take the triggering ReJitManager lock up front, and in the
// proper order, to avoid lock leveling issues, and triggering issues with other locks it
// takes that are CRST_UNSAFE_ANYMODE
//

ReJITID ReJitManager::GetReJitIdNoLock(PTR_MethodDesc pMD, PCODE pCodeStart)
{
    CONTRACTL
    {
        NOTHROW;
        CANNOT_TAKE_LOCK;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pCodeStart != NULL);
    }
    CONTRACTL_END;

    // Caller must ensure this lock is taken!
    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());

    NativeCodeVersion nativeCodeVersion = pMD->GetCodeVersionManager()->GetNativeCodeVersion(pMD, pCodeStart);
    if (nativeCodeVersion.IsNull())
    {
        return 0;
    }
    return nativeCodeVersion.GetILCodeVersion().GetVersionId();
}

//---------------------------------------------------------------------------------------
//
// Called by profiler to retrieve an array of ReJITIDs corresponding to a MethodDesc *
//
// Arguments:
//      * pMD - MethodDesc * to look up
//      * cReJitIds - Element count capacity of reJitIds
//      * pcReJitIds - [out] Place total count of ReJITIDs found here; may be more than
//          cReJitIds if profiler passed an array that's too small to hold them all
//      * reJitIds - [out] Place ReJITIDs found here. Count of ReJITIDs returned here is
//          min(cReJitIds, *pcReJitIds)
//
// Return Value:
//      * S_OK: ReJITIDs successfully returned, array is big enough
//      * S_FALSE: ReJITIDs successfully found, but array was not big enough. Only
//          cReJitIds were returned and cReJitIds < *pcReJitId (latter being the total
//          number of ReJITIDs available).
//
// static
HRESULT ReJitManager::GetReJITIDs(PTR_MethodDesc pMD, ULONG cReJitIds, ULONG * pcReJitIds, ReJITID reJitIds[])
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pcReJitIds != NULL);
        PRECONDITION((cReJitIds == 0) == (reJitIds == NULL));
    }
    CONTRACTL_END;

    CodeVersionManager* pCodeVersionManager = pMD->GetCodeVersionManager();
    CodeVersionManager::LockHolder codeVersioningLockHolder;

    ULONG cnt = 0;

    ILCodeVersionCollection ilCodeVersions = pCodeVersionManager->GetILCodeVersions(pMD);
    for (ILCodeVersionIterator iter = ilCodeVersions.Begin(), end = ilCodeVersions.End();
        iter != end;
        iter++)
    {
        ILCodeVersion curILVersion = *iter;

        if (curILVersion.GetRejitState() == ILCodeVersion::kStateActive)
        {
            if (cnt < cReJitIds)
            {
                reJitIds[cnt] = curILVersion.GetVersionId();
            }
            ++cnt;

            // no overflow
            _ASSERTE(cnt != 0);
        }
    }
    *pcReJitIds = cnt;

    return (cnt > cReJitIds) ? S_FALSE : S_OK;
}

#endif // FEATURE_CODE_VERSIONING
#else // FEATURE_REJIT

// On architectures that don't support rejit, just keep around some do-nothing
// stubs so the rest of the VM doesn't have to be littered with #ifdef FEATURE_REJIT

// static
HRESULT ReJitManager::RequestReJIT(
    ULONG       cFunctions,
    ModuleID    rgModuleIDs[],
    mdMethodDef rgMethodDefs[],
    COR_PRF_REJIT_FLAGS flags)
{
    return E_NOTIMPL;
}

// static
HRESULT ReJitManager::RequestRevert(
    ULONG       cFunctions,
    ModuleID    rgModuleIDs[],
    mdMethodDef rgMethodDefs[],
    HRESULT     rgHrStatuses[])
{
    return E_NOTIMPL;
}

ReJITID ReJitManager::GetReJitId(PTR_MethodDesc pMD, PCODE pCodeStart)
{
    return 0;
}

ReJITID ReJitManager::GetReJitIdNoLock(PTR_MethodDesc pMD, PCODE pCodeStart)
{
    return 0;
}

HRESULT ReJitManager::GetReJITIDs(PTR_MethodDesc pMD, ULONG cReJitIds, ULONG * pcReJitIds, ReJITID reJitIds[])
{
    return E_NOTIMPL;
}

#endif // FEATURE_REJIT
