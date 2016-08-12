// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
// * code:ReJitPublishMethodHolder::ReJitPublishMethodHolder
// MethodDesc::MakeJitWorker() calls this to determine if there's an outstanding
// "pre-rejit" request for a MethodDesc that has just been jitted for the first time. We
// also call this from MethodDesc::CheckRestore when restoring generic methods.
// The holder applies the jump-stamp to the
// top of the originally JITted code, with the jump target being the prestub.
// When ReJIT is enabled this holder enters the ReJIT
// lock to enforce atomicity of doing the pre-rejit-jmp-stamp & publishing/restoring
// the PCODE, which is required to avoid races with a profiler that calls RequestReJIT
// just as the method finishes compiling/restoring.
//
// * code:ReJitPublishMethodTableHolder::ReJitPublishMethodTableHolder
// Does the same thing as ReJitPublishMethodHolder except iterating over every
// method in the MethodTable. This is called from MethodTable::SetIsRestored.
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
//      Inside RequestRejit, DoJumpStampIfNecessary
//      Global Crst MAY/MAY NOT be held, table Crst held
//      Allowed SharedReJit states: Requested, GettingReJITParameters, Active
//
//   2) kJumpNone -> kJumpToPrestub
//      Inside RequestRejit, DoJumpStampIfNecessary
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

#include "../debug/ee/debugger.h"
#include "../debug/ee/walker.h"
#include "../debug/ee/controller.h"

// This HRESULT is only used as a private implementation detail. If it escapes functions
// defined in this file it is a bug. Corerror.xml has a comment in it reserving this
// value for our use but it doesn't appear in the public headers.
#define CORPROF_E_RUNTIME_SUSPEND_REQUIRED 0x80131381

// This is just used as a unique id. Overflow is OK. If we happen to have more than 4+Billion rejits
// and somehow manage to not run out of memory, we'll just have to redefine ReJITID as size_t.
/* static */
ReJITID SharedReJitInfo::s_GlobalReJitId = 1;

/* static */
CrstStatic ReJitManager::s_csGlobalRequest;


//---------------------------------------------------------------------------------------
// Helpers

inline DWORD JitFlagsFromProfCodegenFlags(DWORD dwCodegenFlags)
{
    LIMITED_METHOD_DAC_CONTRACT;

    DWORD jitFlags = 0;

    // Note: COR_PRF_CODEGEN_DISABLE_INLINING is checked in
    // code:CEEInfo::canInline#rejit (it has no equivalent CORJIT flag).

    if ((dwCodegenFlags & COR_PRF_CODEGEN_DISABLE_ALL_OPTIMIZATIONS) != 0)
    {
        jitFlags |= CORJIT_FLG_DEBUG_CODE;
    }

    // In the future more flags may be added that need to be converted here (e.g.,
    // COR_PRF_CODEGEN_ENTERLEAVE / CORJIT_FLG_PROF_ENTERLEAVE)

    return jitFlags;
}

//---------------------------------------------------------------------------------------
// Allocation helpers used by ReJitInfo / SharedReJitInfo to ensure they
// stick stuff on the appropriate loader heap.

void * LoaderHeapAllocatedRejitStructure::operator new (size_t size, LoaderHeap * pHeap, const NoThrow&)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(return NULL;);
        PRECONDITION(CheckPointer(pHeap));
    }
    CONTRACTL_END;
    
#ifdef DACCESS_COMPILE
    return ::operator new(size, nothrow);
#else
    return pHeap->AllocMem_NoThrow(S_SIZE_T(size));
#endif
}

void * LoaderHeapAllocatedRejitStructure::operator new (size_t size, LoaderHeap * pHeap)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pHeap));
    }
    CONTRACTL_END;
    
#ifdef DACCESS_COMPILE
    return ::operator new(size);
#else
    return pHeap->AllocMem(S_SIZE_T(size));
#endif
}


//---------------------------------------------------------------------------------------
//
// Simple, thin abstraction of debugger breakpoint patching. Given an address and a
// previously procured DebuggerControllerPatch governing the code address, this decides
// whether the code address is patched. If so, it returns a pointer to the debugger's
// buffer (of what's "underneath" the int 3 patch); otherwise, it returns the code
// address itself.
//
// Arguments:
//      * pbCode - Code address to return if unpatched
//      * dbgpatch - DebuggerControllerPatch to test
//
// Return Value:
//      Either pbCode or the debugger's patch buffer, as per description above.
//
// Assumptions:
//      Caller must manually grab (and hold) the ControllerLockHolder and get the
//      DebuggerControllerPatch before calling this helper.
//      
// Notes:
//     pbCode need not equal the code address governed by dbgpatch, but is always
//     "related" (and sometimes really is equal). For example, this helper may be used
//     when writing a code byte to an internal rejit buffer (e.g., in preparation for an
//     eventual 64-bit interlocked write into the code stream), and thus pbCode would
//     point into the internal rejit buffer whereas dbgpatch governs the corresponding
//     code byte in the live code stream. This function would then be used to determine
//     whether a byte should be written into the internal rejit buffer OR into the
//     debugger controller's breakpoint buffer.
//

LPBYTE FirstCodeByteAddr(LPBYTE pbCode, DebuggerControllerPatch * dbgpatch)
{
    LIMITED_METHOD_CONTRACT;

    if (dbgpatch != NULL && dbgpatch->IsActivated())
    {
        // Debugger has patched the code, so return the address of the buffer
        return LPBYTE(&(dbgpatch->opcode));
    }

    // no active patch, just return the direct code address
    return pbCode;
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

#ifdef FEATURE_CORECLR
    if (g_pDebugInterface == NULL)
    {
        return CORPROF_E_DEBUGGING_DISABLED;
    }
#else
    // g_pDebugInterface is initialized on startup on desktop CLR, regardless of whether a debugger
    // or profiler is loaded.  So it should always be available.
    _ASSERTE(g_pDebugInterface != NULL);
#endif // FEATURE_CORECLR


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

//---------------------------------------------------------------------------------------
// ReJitManager implementation

// All the state-changey stuff is kept up here in the !DACCESS_COMPILE block.
// The more read-only inspection-y stuff follows the block.

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
// Called by the prestub worker, this function is a simple wrapper which determines the
// appropriate ReJitManager, and then calls DoReJitIfNecessaryWorker() on it. See the
// comment at the top of code:ReJitManager::DoReJitIfNecessaryWorker for more info,
// including parameter & return value descriptions.

// static
PCODE ReJitManager::DoReJitIfNecessary(PTR_MethodDesc pMD)
{
    STANDARD_VM_CONTRACT;

    if (!pMD->HasNativeCode())
    {
        // If method hasn't been jitted yet, the prestub worker should just continue as
        // usual.
        return NULL;
    }

    // We've already published the JITted code for this MethodDesc, and yet we're
    // back in the prestub (who called us).  Ask the appropriate rejit manager if that's because of a rejit request.  If so, the
    // ReJitManager will take care of the rejit now
    return pMD->GetReJitManager()->DoReJitIfNecessaryWorker(pMD);
}

//---------------------------------------------------------------------------------------
//
// ICorProfilerInfo4::RequestReJIT calls into this guy to do most of the
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
    ULONG       cFunctions,
    ModuleID    rgModuleIDs[],
    mdMethodDef rgMethodDefs[])
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
    //BUGBUG: Its not clear to me why it is safe to hold ReJitInfo* lists
    // outside the table locks. If an AppDomain unload occurred I don't see anything
    // that prevents them from being deleted. If this is a bug it is a pre-existing
    // condition and nobody has reported it as an issue yet. AppDomainExit probably
    // needs to synchronize with something.
    // Jan also pointed out the ModuleIDs have the same issue, in order to use this
    // function safely the profiler needs prevent the AppDomain which contains the
    // modules from being unloaded. I doubt any profilers are doing this intentionally
    // but calling from within typical callbacks like ModuleLoadFinished or
    // JIT events would do it for the current domain I think. Of course RequestRejit
    // could always be called with ModuleIDs in some other AppDomain.
    //END BUGBUG
    SHash<ReJitManagerJumpStampBatchTraits> mgrToJumpStampBatch;
    CDynArray<ReJitReportErrorWorkItem> errorRecords;
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

        ReJitManager * pReJitMgr = pModule->GetReJitManager();
        _ASSERTE(pReJitMgr != NULL);
        ReJitManagerJumpStampBatch * pJumpStampBatch = mgrToJumpStampBatch.Lookup(pReJitMgr);
        if (pJumpStampBatch == NULL)
        {
            pJumpStampBatch = new (nothrow)ReJitManagerJumpStampBatch(pReJitMgr);
            if (pJumpStampBatch == NULL)
            {
                return E_OUTOFMEMORY;
            }

            hr = S_OK;
            EX_TRY
            {
                // This guy throws when out of memory, but remains internally
                // consistent (without adding the new element)
                mgrToJumpStampBatch.Add(pJumpStampBatch);
            }
            EX_CATCH_HRESULT(hr);

            _ASSERT(hr == S_OK || hr == E_OUTOFMEMORY);
            if (FAILED(hr))
            {
                return hr;
            }
        }


        // At this stage, pMD may be NULL or non-NULL, and the specified function may or
        // may not be a generic (or a function on a generic class).  The operations
        // below depend on these conditions as follows:
        // 
        // (1) If pMD == NULL || PMD has no code || pMD is generic
        // Do a "PRE-REJIT" (add a placeholder ReJitInfo that points to module/token;
        // there's nothing to jump-stamp)
        // 
        // (2) IF pMD != NULL, but not generic (or function on generic class)
        // Do a REAL REJIT (add a real ReJitInfo that points to pMD and jump-stamp)
        // 
        // (3) IF pMD != NULL, and is a generic (or function on generic class)
        // Do a real rejit (including jump-stamp) for all already-jitted instantiations.

        BaseDomain * pBaseDomainFromModule = pModule->GetDomain();
        SharedReJitInfo * pSharedInfo = NULL;
        {
            CrstHolder ch(&(pReJitMgr->m_crstTable));

            // Do a PRE-rejit
            if (pMD == NULL || !pMD->HasNativeCode() || pMD->HasClassOrMethodInstantiation())
            {
                hr = pReJitMgr->MarkForReJit(
                    pModule,
                    rgMethodDefs[i],
                    pJumpStampBatch,
                    &errorRecords,
                    &pSharedInfo);
                if (FAILED(hr))
                {
                    _ASSERTE(hr == E_OUTOFMEMORY);
                    return hr;
                }
            }

            if (pMD == NULL)
            {
                // nothing is loaded yet so only the pre-rejit placeholder is needed. We're done for this method.
                continue;
            }

            if (!pMD->HasClassOrMethodInstantiation() && pMD->HasNativeCode())
            {
                // We have a JITted non-generic. Easy case. Just mark the JITted method
                // desc as needing to be rejitted
                hr = pReJitMgr->MarkForReJit(
                    pMD,
                    pSharedInfo,
                    pJumpStampBatch,
                    &errorRecords,
                    NULL);      // Don't need the SharedReJitInfo to be returned

                if (FAILED(hr))
                {
                    _ASSERTE(hr == E_OUTOFMEMORY);
                    return hr;
                }
            }
            
            if (!pMD->HasClassOrMethodInstantiation())
            {
                // not generic, we're done for this method
                continue;
            }

            // Ok, now the case of a generic function (or function on generic class), which
            // is loaded, and may thus have compiled instantiations.
            // It's impossible to get to any other kind of domain from the profiling API
            _ASSERTE(pBaseDomainFromModule->IsAppDomain() ||
                pBaseDomainFromModule->IsSharedDomain());

            if (pBaseDomainFromModule->IsSharedDomain())
            {
                // Iterate through all modules loaded into the shared domain, to
                // find all instantiations living in the shared domain. This will
                // include orphaned code (i.e., shared code used by ADs that have
                // all unloaded), which is good, because orphaned code could get
                // re-adopted if a new AD is created that can use that shared code
                hr = pReJitMgr->MarkAllInstantiationsForReJit(
                    pSharedInfo,
                    NULL,  // NULL means to search SharedDomain instead of an AD
                    pModule,
                    rgMethodDefs[i],
                    pJumpStampBatch,
                    &errorRecords);
            }
            else
            {
                // Module is unshared, so just use the module's domain to find instantiations.
                hr = pReJitMgr->MarkAllInstantiationsForReJit(
                    pSharedInfo,
                    pBaseDomainFromModule->AsAppDomain(),
                    pModule,
                    rgMethodDefs[i],
                    pJumpStampBatch,
                    &errorRecords);
            }
            if (FAILED(hr))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
        }

        // We want to iterate through all compilations of existing instantiations to
        // ensure they get marked for rejit.  Note: There may be zero instantiations,
        // but we won't know until we try.
        if (pBaseDomainFromModule->IsSharedDomain())
        {
            // Iterate through all real domains, to find shared instantiations.
            AppDomainIterator appDomainIterator(TRUE);
            while (appDomainIterator.Next())
            {
                AppDomain * pAppDomain = appDomainIterator.GetDomain();
                if (pAppDomain->IsUnloading())
                {
                    continue;
                }
                CrstHolder ch(&(pReJitMgr->m_crstTable));
                hr = pReJitMgr->MarkAllInstantiationsForReJit(
                    pSharedInfo,
                    pAppDomain,
                    pModule,
                    rgMethodDefs[i],
                    pJumpStampBatch,
                    &errorRecords);
                if (FAILED(hr))
                {
                    _ASSERTE(hr == E_OUTOFMEMORY);
                    return hr;
                }
            }
        }
    }   // for (ULONG i = 0; i < cFunctions; i++)

    // For each rejit mgr, if there's work to do, suspend EE if needed,
    // enter the rejit mgr's crst, and do the batched work.
    BOOL fEESuspended = FALSE;
    SHash<ReJitManagerJumpStampBatchTraits>::Iterator beginIter = mgrToJumpStampBatch.Begin();
    SHash<ReJitManagerJumpStampBatchTraits>::Iterator endIter = mgrToJumpStampBatch.End();
    for (SHash<ReJitManagerJumpStampBatchTraits>::Iterator iter = beginIter; iter != endIter; iter++)
    {
        ReJitManagerJumpStampBatch * pJumpStampBatch = *iter;
        ReJitManager * pMgr = pJumpStampBatch->pReJitManager;

        int cBatchedPreStubMethods = pJumpStampBatch->preStubMethods.Count();
        if (cBatchedPreStubMethods == 0)
        {
            continue;
        }
        if(!fEESuspended)
        {
            // As a potential future optimization we could speculatively try to update the jump stamps without
            // suspending the runtime. That needs to be plumbed through BatchUpdateJumpStamps though.
            
            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_REJIT);
            fEESuspended = TRUE;
        }

        CrstHolder ch(&(pMgr->m_crstTable));
        _ASSERTE(ThreadStore::HoldingThreadStore());
        hr = pMgr->BatchUpdateJumpStamps(&(pJumpStampBatch->undoMethods), &(pJumpStampBatch->preStubMethods), &errorRecords);
        if (FAILED(hr))
            break;
    }
    if (fEESuspended)
    {
        ThreadSuspend::RestartEE(FALSE, TRUE);
    }

    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }

    // Report any errors that were batched up
    for (int i = 0; i < errorRecords.Count(); i++)
    {
        ReportReJITError(&(errorRecords[i]));
    }

    INDEBUG(SharedDomain::GetDomain()->GetReJitManager()->Dump(
        "Finished RequestReJIT().  Dumping Shared ReJitManager\n"));

    // We got through processing everything, but profiler will need to see the individual ReJITError
    // callbacks to know what, if anything, failed.
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Helper used by ReJitManager::RequestReJIT to jump stamp all the methods that were
// specified by the caller. Also used by RejitManager::DoJumpStampForAssemblyIfNecessary
// when rejitting a batch of generic method instantiations in a newly loaded NGEN assembly.
// 
// This method is responsible for calling ReJITError on the profiler if anything goes
// wrong.
//
// Arguments:
//    * pUndoMethods - array containing the methods that need the jump stamp removed
//    * pPreStubMethods - array containing the methods that need to be jump stamped to prestub
//    * pErrors - any errors will be appended to this array
//
// Returns:
//    S_OK - all methods are updated or added an error to the pErrors array
//    E_OUTOFMEMORY - some methods neither updated nor added an error to pErrors array
//                    ReJitInfo state remains consistent
//
// Assumptions:
//         1) Caller prevents contention by either:
//            a) Suspending the runtime
//            b) Ensuring all methods being updated haven't been published
//
HRESULT ReJitManager::BatchUpdateJumpStamps(CDynArray<ReJitInfo *> * pUndoMethods, CDynArray<ReJitInfo *> * pPreStubMethods, CDynArray<ReJitReportErrorWorkItem> * pErrors)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUndoMethods));
        PRECONDITION(CheckPointer(pPreStubMethods));
        PRECONDITION(CheckPointer(pErrors));
    }
    CONTRACTL_END;

    _ASSERTE(m_crstTable.OwnedByCurrentThread());
    HRESULT hr = S_OK;

    ReJitInfo ** ppInfoEnd = pUndoMethods->Ptr() + pUndoMethods->Count();
    for (ReJitInfo ** ppInfoCur = pUndoMethods->Ptr(); ppInfoCur < ppInfoEnd; ppInfoCur++)
    {
        // If we are undoing jumpstamps they have been published already
        // and our caller is holding the EE suspended
        _ASSERTE(ThreadStore::HoldingThreadStore());
        if (FAILED(hr = (*ppInfoCur)->UndoJumpStampNativeCode(TRUE)))
        {
            if (FAILED(hr = AddReJITError(*ppInfoCur, hr, pErrors)))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
        }
    }

    ppInfoEnd = pPreStubMethods->Ptr() + pPreStubMethods->Count();
    for (ReJitInfo ** ppInfoCur = pPreStubMethods->Ptr(); ppInfoCur < ppInfoEnd; ppInfoCur++)
    {
        if (FAILED(hr = (*ppInfoCur)->JumpStampNativeCode()))
        {
            if (FAILED(hr = AddReJITError(*ppInfoCur, hr, pErrors)))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
        }
    }
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Helper used by ReJitManager::RequestReJIT to iterate through any generic
// instantiations of a function in a given AppDomain, and to create the corresponding
// ReJitInfos for those MethodDescs. This also adds corresponding entries to a temporary
// dynamic array created by our caller for batching up the jump-stamping we'll need to do
// later.
// 
// This method is responsible for calling ReJITError on the profiler if anything goes
// wrong.
//
// Arguments:
//    * pSharedForAllGenericInstantiations - The SharedReJitInfo for this mdMethodDef's
//        rejit request. This is what we must associate any newly-created ReJitInfo with.
//    * pAppDomainToSearch - AppDomain in which to search for generic instantiations
//        matching the specified methodDef. If it is NULL, then we'll search for all
//        MethodDescs whose metadata definition appears in a Module loaded into the
//        SharedDomain (regardless of which ADs--if any--are using those MethodDescs).
//        This captures the case of domain-neutral code that was in use by an AD that
//        unloaded, and may come into use again once a new AD loads that can use the
//        shared code.
//    * pModuleContainingMethodDef - Module* containing the specified methodDef token.
//    * methodDef - Token for the method for which we're searching for MethodDescs.
//    * pJumpStampBatch - Batch we're responsible for placing ReJitInfo's into, on which
//        the caller will update the jump stamps.
//    * pRejitErrors - Dynamic array we're responsible for adding error records into.
//        The caller will report them to the profiler outside the table lock
//   
// Returns:
//    S_OK - all methods were either marked for rejit OR have appropriate error records
//           in pRejitErrors
//    E_OUTOFMEMORY - some methods weren't marked for rejit AND we didn't have enough
//           memory to create the error records
//
// Assumptions:
//     * This function should only be called on the ReJitManager that owns the (generic)
//         definition of methodDef
//     * If pModuleContainingMethodDef is loaded into the SharedDomain, then
//         pAppDomainToSearch may be NULL (to search all instantiations loaded shared),
//         or may be non-NULL (to search all instantiations loaded into
//         pAppDomainToSearch)
//     * If pModuleContainingMethodDef is not loaded domain-neutral, then
//         pAppDomainToSearch must be non-NULL (and, indeed, must be the very AD that
//         pModuleContainingMethodDef is loaded into).
//

HRESULT ReJitManager::MarkAllInstantiationsForReJit(
    SharedReJitInfo * pSharedForAllGenericInstantiations,
    AppDomain * pAppDomainToSearch,
    PTR_Module pModuleContainingMethodDef,
    mdMethodDef methodDef,
    ReJitManagerJumpStampBatch* pJumpStampBatch,
    CDynArray<ReJitReportErrorWorkItem> * pRejitErrors)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pSharedForAllGenericInstantiations));
        PRECONDITION(CheckPointer(pAppDomainToSearch, NULL_OK));
        PRECONDITION(CheckPointer(pModuleContainingMethodDef));
        PRECONDITION(CheckPointer(pJumpStampBatch));
    }
    CONTRACTL_END;

    _ASSERTE(m_crstTable.OwnedByCurrentThread());
    _ASSERTE(methodDef != mdTokenNil);
    _ASSERTE(pJumpStampBatch->pReJitManager == this);

    HRESULT hr;

    BaseDomain * pDomainContainingGenericDefinition = pModuleContainingMethodDef->GetDomain();

#ifdef _DEBUG
    // This function should only be called on the ReJitManager that owns the (generic)
    // definition of methodDef 
    _ASSERTE(this == pDomainContainingGenericDefinition->GetReJitManager());

    // If the generic definition is not loaded domain-neutral, then all its
    // instantiations will also be non-domain-neutral and loaded into the same
    // domain as the generic definition.  So the caller may only pass the
    // domain containing the generic definition as pAppDomainToSearch
    if (!pDomainContainingGenericDefinition->IsSharedDomain())
    {
        _ASSERTE(pDomainContainingGenericDefinition == pAppDomainToSearch);
    }
#endif //_DEBUG

    // If pAppDomainToSearch is NULL, iterate through all existing 
    // instantiations loaded into the SharedDomain. If pAppDomainToSearch is non-NULL, 
    // iterate through all existing instantiations in pAppDomainToSearch, and only consider
    // instantiations in non-domain-neutral assemblies (as we already covered domain 
    // neutral assemblies when we searched the SharedDomain).
    LoadedMethodDescIterator::AssemblyIterationMode mode = LoadedMethodDescIterator::kModeSharedDomainAssemblies;
    // these are the default flags which won't actually be used in shared mode other than
    // asserting they were specified with their default values
    AssemblyIterationFlags assemFlags = (AssemblyIterationFlags) (kIncludeLoaded | kIncludeExecution);
    ModuleIterationOption moduleFlags = (ModuleIterationOption) kModIterIncludeLoaded;
    if (pAppDomainToSearch != NULL)
    {
        mode = LoadedMethodDescIterator::kModeUnsharedADAssemblies;
        assemFlags = (AssemblyIterationFlags)(kIncludeAvailableToProfilers | kIncludeExecution);
        moduleFlags = (ModuleIterationOption)kModIterIncludeAvailableToProfilers;
    }
    LoadedMethodDescIterator it(
        pAppDomainToSearch, 
        pModuleContainingMethodDef, 
        methodDef,
        mode,
        assemFlags,
        moduleFlags);
    CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
    while (it.Next(pDomainAssembly.This()))
    {
        MethodDesc * pLoadedMD = it.Current();

        if (!pLoadedMD->HasNativeCode())
        {
            // Skip uninstantiated MethodDescs. The placeholder added by our caller
            // is sufficient to ensure they'll eventually be rejitted when they get
            // compiled.
            continue;
        }

        if (FAILED(hr = IsMethodSafeForReJit(pLoadedMD)))
        {
            if (FAILED(hr = AddReJITError(pModuleContainingMethodDef, methodDef, pLoadedMD, hr, pRejitErrors)))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
            continue;
        }

#ifdef _DEBUG
        if (!pDomainContainingGenericDefinition->IsSharedDomain())
        {
            // Method is defined outside of the shared domain, so its instantiation must
            // be defined in the AD we're iterating over (pAppDomainToSearch, which, as
            // asserted above, must be the same domain as the generic's definition)
            _ASSERTE(pLoadedMD->GetDomain() == pAppDomainToSearch);
        }
#endif // _DEBUG

        // This will queue up the MethodDesc for rejitting and create all the
        // look-aside tables needed.
        SharedReJitInfo * pSharedUsed = NULL;
        hr = MarkForReJit(
            pLoadedMD, 
            pSharedForAllGenericInstantiations, 
            pJumpStampBatch,
            pRejitErrors,
            &pSharedUsed);
        if (FAILED(hr))
        {
            _ASSERTE(hr == E_OUTOFMEMORY);
            return hr;
        }
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Helper used by ReJitManager::MarkAllInstantiationsForReJit and
// ReJitManager::RequestReJIT to do the actual ReJitInfo allocation and
// placement inside m_table. Note that callers don't use MarkForReJitHelper
// directly. Instead, callers actually use the inlined overloaded wrappers
// ReJitManager::MarkForReJit (one for placeholder (i.e., methodDef pre-rejit)
// ReJitInfos and one for regular (i.e., MethodDesc) ReJitInfos). When the
// overloaded MarkForReJit wrappers call this, they ensure that either pMD is
// valid XOR (pModule, methodDef) is valid.
//
// Arguments:
//    * pMD - MethodDesc for which to find / create ReJitInfo. Only used if
//        we're creating a regular ReJitInfo
//    * pModule - Module for which to find / create ReJitInfo. Only used if
//        we're creating a placeholder ReJitInfo
//    * methodDef - methodDef for which to find / create ReJitInfo. Only used
//        if we're creating a placeholder ReJitInfo
//    * pSharedToReuse - SharedReJitInfo to associate any newly created
//        ReJitInfo with. If NULL, we'll create a new one.
//    * pJumpStampBatch - a batch of methods that need to have jump stamps added
//        or removed. This method will add new ReJitInfos to the batch as needed.
//    * pRejitErrors - An array of rejit errors that this call will append to
//        if there is an error marking
//    * ppSharedUsed - [out]: SharedReJitInfo used for this request. If
//        pSharedToReuse is non-NULL, *ppSharedUsed == pSharedToReuse. Else,
//        *ppSharedUsed is the SharedReJitInfo newly-created to associate with
//            the ReJitInfo used for this request.
//
// Return Value:
//    * S_OK: Successfully created a new ReJitInfo to manage this request
//    * S_FALSE: An existing ReJitInfo was already available to manage this
//        request, so we didn't need to create a new one.
//    * E_OUTOFMEMORY
//    * Else, a failure HRESULT indicating what went wrong.
//

HRESULT ReJitManager::MarkForReJitHelper(
    PTR_MethodDesc pMD, 
    PTR_Module pModule, 
    mdMethodDef methodDef,
    SharedReJitInfo * pSharedToReuse, 
    ReJitManagerJumpStampBatch* pJumpStampBatch,
    CDynArray<ReJitReportErrorWorkItem> * pRejitErrors,
    /* out */ SharedReJitInfo ** ppSharedUsed)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pMD, NULL_OK));
        PRECONDITION(CheckPointer(pModule, NULL_OK));
        PRECONDITION(CheckPointer(pJumpStampBatch));
        PRECONDITION(CheckPointer(pRejitErrors));
        PRECONDITION(CheckPointer(ppSharedUsed, NULL_OK));
    }
    CONTRACTL_END;

    CrstHolder ch(&m_crstTable);

    // Either pMD is valid, xor (pModule,methodDef) is valid
    _ASSERTE(
        ((pMD != NULL) && (pModule == NULL) && (methodDef == mdTokenNil)) ||
        ((pMD == NULL) && (pModule != NULL) && (methodDef != mdTokenNil)));
    _ASSERTE(pJumpStampBatch->pReJitManager == this);

    if (ppSharedUsed != NULL)
        *ppSharedUsed = NULL;
    HRESULT hr = S_OK;

    // Check if there was there a previous rejit request for pMD
    
    ReJitInfoHash::KeyIterator beginIter(&m_table, TRUE /* begin */); 
    ReJitInfoHash::KeyIterator endIter(&m_table, FALSE /* begin */);

    if (pMD != NULL)
    {
        beginIter = GetBeginIterator(pMD);
        endIter = GetEndIterator(pMD);
    }
    else
    {
        beginIter = GetBeginIterator(pModule, methodDef);
        endIter = GetEndIterator(pModule, methodDef);
    }

    for (ReJitInfoHash::KeyIterator iter = beginIter;
        iter != endIter; 
        iter++)
    {
        ReJitInfo * pInfo = *iter;
        _ASSERTE(pInfo->m_pShared != NULL);

#ifdef _DEBUG
        if (pMD != NULL)
        {
            _ASSERTE(pInfo->GetMethodDesc() == pMD);
        }
        else
        {
            Module * pModuleTest = NULL;
            mdMethodDef methodDefTest = mdTokenNil;
            pInfo->GetModuleAndToken(&pModuleTest, &methodDefTest);
            _ASSERTE((pModule == pModuleTest) && (methodDef == methodDefTest));
        }
#endif //_DEBUG

        SharedReJitInfo * pShared = pInfo->m_pShared;

        switch (pShared->GetState())
        {
        case SharedReJitInfo::kStateRequested:
            // We can 'reuse' this instance because the profiler doesn't know about
            // it yet. (This likely happened because a profiler called RequestReJIT
            // twice in a row, without us having a chance to jmp-stamp the code yet OR
            // while iterating through instantiations of a generic, the iterator found
            // duplicate entries for the same instantiation.)
            _ASSERTE(pShared->m_pbIL == NULL);
            _ASSERTE(pInfo->m_pCode == NULL);

            if (ppSharedUsed != NULL)
                *ppSharedUsed = pShared;
            
            INDEBUG(AssertRestOfEntriesAreReverted(iter, endIter));
            return S_FALSE;

        case SharedReJitInfo::kStateGettingReJITParameters:
        case SharedReJitInfo::kStateActive:
        {
            // Profiler has already requested to rejit this guy, AND we've already
            // at least started getting the rejit parameters from the profiler. We need to revert this
            // instance (this will put back the original code)

            INDEBUG(AssertRestOfEntriesAreReverted(iter, endIter));
            hr = Revert(pShared, pJumpStampBatch);
            if (FAILED(hr))
            {
                _ASSERTE(hr == E_OUTOFMEMORY);
                return hr;
            }
            _ASSERTE(pShared->GetState() == SharedReJitInfo::kStateReverted);

            // No need to continue looping.  Break out of loop to create a new
            // ReJitInfo to service the request.
            goto EXIT_LOOP;
        }
        case SharedReJitInfo::kStateReverted:
            // just ignore this guy
            continue;

        default:
            UNREACHABLE();
        }
    }
EXIT_LOOP:

    // Either there was no ReJitInfo yet for this MethodDesc OR whatever we've found
    // couldn't be reused (and needed to be reverted).  Create a new ReJitInfo to return
    // to the caller.
    // 
    // If the caller gave us a pMD that is a new generic instantiation, then the caller
    // may also have provided a pSharedToReuse for the generic.  Use that instead of
    // creating a new one.

    SharedReJitInfo * pShared = NULL;

    if (pSharedToReuse != NULL)
    {
        pShared = pSharedToReuse;
    }
    else
    {
        PTR_LoaderHeap pHeap = NULL;
        if (pModule != NULL)
        {
            pHeap = pModule->GetLoaderAllocator()->GetLowFrequencyHeap();
        }
        else
        {
            pHeap = pMD->GetLoaderAllocator()->GetLowFrequencyHeap();
        }
        pShared = new (pHeap, nothrow) SharedReJitInfo;
        if (pShared == NULL)
        {
            return E_OUTOFMEMORY;
        }
    }

    _ASSERTE(pShared != NULL);

    // ReJitInfos with MethodDesc's need to be jump-stamped,
    // ReJitInfos with Module/MethodDef are placeholders that don't need a stamp
    ReJitInfo * pInfo = NULL;
    ReJitInfo ** ppInfo = &pInfo;
    if (pMD != NULL)
    {
        ppInfo = pJumpStampBatch->preStubMethods.Append();
        if (ppInfo == NULL)
        {
            return E_OUTOFMEMORY;
        }
    }
    hr = AddNewReJitInfo(pMD, pModule, methodDef, pShared, ppInfo);
    if (FAILED(hr))
    {
        // NOTE: We could consider using an AllocMemTracker or AllocMemHolder
        // here to back out the allocation of pShared, but it probably
        // wouldn't make much of a difference. We'll only get here if we ran
        // out of memory allocating the pInfo, so our memory has already been
        // blown. We can't cause much leaking due to this error path.
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }

    _ASSERTE(*ppInfo != NULL);

    if (ppSharedUsed != NULL)
        *ppSharedUsed = pShared;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Helper used by the above helpers (and also during jump-stamping) to
// allocate and store a new ReJitInfo.
//
// Arguments:
//    * pMD - MethodDesc for which to create ReJitInfo. Only used if we're
//        creating a regular ReJitInfo
//    * pModule - Module for which create ReJitInfo. Only used if we're
//        creating a placeholder ReJitInfo
//    * methodDef - methodDef for which to create ReJitInfo. Only used if
//        we're creating a placeholder ReJitInfo
//    * pShared - SharedReJitInfo to associate the newly created ReJitInfo
//        with.
//    * ppInfo - [out]: ReJitInfo created
//
// Return Value:
//    * S_OK: ReJitInfo successfully created & stored.
//    * Else, failure indicating the problem. Currently only E_OUTOFMEMORY.
//    
// Assumptions:
//   * Caller should be holding this ReJitManager's table crst.
//

HRESULT ReJitManager::AddNewReJitInfo(
    PTR_MethodDesc pMD, 
    PTR_Module pModule,
    mdMethodDef methodDef,
    SharedReJitInfo * pShared,
    ReJitInfo ** ppInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pMD, NULL_OK));
        PRECONDITION(CheckPointer(pModule, NULL_OK));
        PRECONDITION(CheckPointer(pShared));
        PRECONDITION(CheckPointer(ppInfo));
    }
    CONTRACTL_END;

    _ASSERTE(m_crstTable.OwnedByCurrentThread());
    _ASSERTE(pShared->GetState() != SharedReJitInfo::kStateReverted);

    // Either pMD is valid, xor (pModule,methodDef) is valid
    _ASSERTE(
        ((pMD != NULL) && (pModule == NULL) && (methodDef == mdTokenNil)) ||
        ((pMD == NULL) && (pModule != NULL) && (methodDef != mdTokenNil)));

    HRESULT hr;
    ReJitInfo * pInfo = NULL;

    if (pMD != NULL)
    {
        PTR_LoaderHeap pHeap = pMD->GetLoaderAllocator()->GetLowFrequencyHeap();
        pInfo = new (pHeap, nothrow) ReJitInfo(pMD, pShared);
    }
    else
    {
        PTR_LoaderHeap pHeap = pModule->GetLoaderAllocator()->GetLowFrequencyHeap();
        pInfo = new (pHeap, nothrow) ReJitInfo(pModule, methodDef, pShared);
    }
    if (pInfo == NULL)
    {
        return E_OUTOFMEMORY;
    }

    hr = S_OK;
    EX_TRY
    {
        // This guy throws when out of memory, but remains internally
        // consistent (without adding the new element)
        m_table.Add(pInfo);
    }
    EX_CATCH_HRESULT(hr);

    _ASSERT(hr == S_OK || hr == E_OUTOFMEMORY);
    if (FAILED(hr))
    {
        pInfo = NULL;
        return hr;
    }

    *ppInfo = pInfo;
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Given a MethodDesc, call ReJitInfo::JumpStampNativeCode to stamp the top of its
// originally-jitted-code with a jmp that goes to the prestub. This is called by the
// prestub worker after jitting the original code of a function (i.e., the "pre-rejit"
// scenario). In this case, the EE is not suspended. But that's ok, because the PCODE has
// not yet been published to the MethodDesc, and no thread can be executing inside the
// originally JITted function yet.
//
// Arguments:
//    * pMD - MethodDesc to jmp-stamp
//    * pCode - Top of the code that was just jitted (using original IL).
//
//
// Return value:
//    * S_OK: Either we successfully did the jmp-stamp, or we didn't have to (e.g., there
//        was no outstanding pre-rejit request for this MethodDesc, or a racing thread
//        took care of it for us).
//    * Else, HRESULT indicating failure.

// Assumptions:
//     The caller has not yet published pCode to the MethodDesc, so no threads can be
//     executing inside pMD's code yet. Thus, we don't need to suspend the runtime while
//     applying the jump-stamp like we usually do for rejit requests that are made after
//     a function has been JITted.
//

HRESULT ReJitManager::DoJumpStampIfNecessary(MethodDesc* pMD, PCODE pCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pCode != NULL);
    }
    CONTRACTL_END;

    HRESULT hr;

    _ASSERTE(IsTableCrstOwnedByCurrentThread());

    ReJitInfo * pInfoToJumpStamp = NULL;

    // First, try looking up ReJitInfo by MethodDesc. A "regular" MethodDesc-based
    // ReJitInfo already exists for "case 1" (see comment above
    // code:ReJitInfo::JumpStampNativeCode), and could even exist for "case 2"
    // (pre-rejit), if either:
    //     * The pre-rejit was requested after the MD had already been loaded (though
    //         before it had been jitted) OR
    //     * there was a race to JIT the original code for the MD, and another thread got
    //         here before us and already added the ReJitInfo for that MD.

    ReJitInfoHash::KeyIterator beginIter = GetBeginIterator(pMD);
    ReJitInfoHash::KeyIterator endIter = GetEndIterator(pMD);

    pInfoToJumpStamp = FindPreReJittedReJitInfo(beginIter, endIter);
    if (pInfoToJumpStamp != NULL)
    {
        _ASSERTE(pInfoToJumpStamp->GetMethodDesc() == pMD);
        // does it need to be jump-stamped?
        if (pInfoToJumpStamp->GetState() != ReJitInfo::kJumpNone)
        {
            return S_OK;
        }
        else
        {
            return pInfoToJumpStamp->JumpStampNativeCode(pCode);
        }
    }

    // In this case, try looking up by module / metadata token.  This is the case where
    // the pre-rejit request occurred before the MD was loaded.

    Module * pModule = pMD->GetModule();
    _ASSERTE(pModule != NULL);
    mdMethodDef methodDef = pMD->GetMemberDef();

    beginIter = GetBeginIterator(pModule, methodDef);
    endIter = GetEndIterator(pModule, methodDef);
    ReJitInfo * pInfoPlaceholder = NULL;

    pInfoPlaceholder = FindPreReJittedReJitInfo(beginIter, endIter);
    if (pInfoPlaceholder == NULL)
    {
        // No jump stamping to do.
        return S_OK;
    }

    // The placeholder may already have a rejit info for this MD, in which
    // case we don't need to do any additional work
    for (ReJitInfo * pInfo = pInfoPlaceholder->m_pShared->GetMethods(); pInfo != NULL; pInfo = pInfo->m_pNext)
    {
        if ((pInfo->GetKey().m_keyType == ReJitInfo::Key::kMethodDesc) &&
            (pInfo->GetMethodDesc() == pMD))
        {
            // Any rejit info we find should already be jumpstamped
            _ASSERTE(pInfo->GetState() != ReJitInfo::kJumpNone);
            return S_OK;
        }
    }

#ifdef _DEBUG
    {
        Module * pModuleTest = NULL;
        mdMethodDef methodDefTest = mdTokenNil;
        INDEBUG(pInfoPlaceholder->GetModuleAndToken(&pModuleTest, &methodDefTest));
        _ASSERTE((pModule == pModuleTest) && (methodDef == methodDefTest));
    }
#endif //_DEBUG

    // We have finished JITting the original code for a function that had been
    // "pre-rejitted" (i.e., requested to be rejitted before it was first compiled). So
    // now is the first time where we know the MethodDesc of the request.
    if (FAILED(hr = IsMethodSafeForReJit(pMD)))
    {
        // No jump stamping to do.
        return hr;
    }

    // Create the ReJitInfo associated with the MethodDesc now (pInfoToJumpStamp), and
    // jump-stamp the original code.
    pInfoToJumpStamp = NULL;
    hr = AddNewReJitInfo(pMD, NULL /*pModule*/, NULL /*methodDef*/, pInfoPlaceholder->m_pShared, &pInfoToJumpStamp);
    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(pInfoToJumpStamp != NULL);
    return pInfoToJumpStamp->JumpStampNativeCode(pCode);
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

    // Serialize all RequestReJIT() and Revert() calls against each other (even across AppDomains)
    CrstHolder ch(&(s_csGlobalRequest));

    // Request at least 1 method to revert!
    _ASSERTE ((cFunctions != 0) && (rgModuleIDs != NULL) && (rgMethodDefs != NULL));

    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_REJIT);
    for (ULONG i = 0; i < cFunctions; i++)
    {
        HRESULT hr = E_UNEXPECTED;
        Module * pModule = reinterpret_cast< Module * >(rgModuleIDs[i]);
        if (pModule == NULL || TypeFromToken(rgMethodDefs[i]) != mdtMethodDef)
        {
            hr = E_INVALIDARG;
        }
        else if (pModule->IsBeingUnloaded())
        {
            hr = CORPROF_E_DATAINCOMPLETE;
        }
        else if (pModule->IsReflection())
        {
            hr = CORPROF_E_MODULE_IS_DYNAMIC;
        }
        else
        {
            hr = pModule->GetReJitManager()->RequestRevertByToken(pModule, rgMethodDefs[i]);
        }
        
        if (rgHrStatuses != NULL)
        {
            rgHrStatuses[i] = hr;
        }
    }

    ThreadSuspend::RestartEE(FALSE /* bFinishedGC */, TRUE /* SuspendSucceded */);

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Called by AppDomain::Exit() to notify the SharedDomain's ReJitManager that this
// AppDomain is exiting.  The SharedDomain's ReJitManager will then remove any
// ReJitInfos relating to MDs owned by AppDomain.  This is how we remove
// non-domain-neutral instantiations of domain-neutral generics from the SharedDomain's
// ReJitManager.
//
// Arguments:
//      pAppDomain - AppDomain that is exiting.
//

// static
void ReJitManager::OnAppDomainExit(AppDomain * pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    // All ReJitInfos and SharedReJitInfos for this AD's ReJitManager automatically get
    // cleaned up as they're allocated on the AD's loader heap.
    
    // We explicitly clean up the SHash here, as its entries get allocated using regular
    // "new"
    pAppDomain->GetReJitManager()->m_table.RemoveAll();

    // We need to ensure that any MethodDescs from pAppDomain that are stored on the
    // SharedDomain's ReJitManager get removed from the SharedDomain's ReJitManager's
    // hash table, and from the linked lists tied to their owning SharedReJitInfo. (This
    // covers the case of non-domain-neutral instantiations of domain-neutral generics.)
    SharedDomain::GetDomain()->GetReJitManager()->RemoveReJitInfosFromDomain(pAppDomain);
}


//---------------------------------------------------------------------------------------
//
// Small helper to determine whether a given (possibly instantiated generic) MethodDesc
// is safe to rejit.  If not, this function is responsible for calling into the
// profiler's ReJITError()
//
// Arguments:
//      pMD - MethodDesc to test
// Return Value:
//      S_OK iff pMD is safe to rejit
//      CORPROF_E_FUNCTION_IS_COLLECTIBLE - function can't be rejitted because it is collectible
//      

// static
HRESULT ReJitManager::IsMethodSafeForReJit(PTR_MethodDesc pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pMD != NULL);

    // Weird, non-user functions were already weeded out in RequestReJIT(), and will
    // also never be passed to us by the prestub worker (for the pre-rejit case).
    _ASSERTE(pMD->IsIL());

    // Any MethodDescs that could be collected are not currently supported.  Although we
    // rule out all Ref.Emit modules in RequestReJIT(), there can still exist types defined
    // in a non-reflection module and instantiated into a collectible assembly
    // (e.g., List<MyCollectibleStruct>).  In the future we may lift this
    // restriction by updating the ReJitManager when the collectible assemblies
    // owning the instantiations get collected.
    if (pMD->GetLoaderAllocator()->IsCollectible())
    {
        return CORPROF_E_FUNCTION_IS_COLLECTIBLE;
    }

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Simple wrapper around GetCurrentReJitWorker. See
// code:ReJitManager::GetCurrentReJitWorker for information about parameters, return
// values, etc.

// static
DWORD ReJitManager::GetCurrentReJitFlags(PTR_MethodDesc pMD)
{
    CONTRACTL 
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMD));
    } 
    CONTRACTL_END;

    return pMD->GetReJitManager()->GetCurrentReJitFlagsWorker(pMD);
}


//---------------------------------------------------------------------------------------
//
// Given a methodDef token, finds the corresponding ReJitInfo, and asks the
// ReJitInfo to perform a revert.
//
// Arguments:
//    * pModule - Module to revert
//    * methodDef - methodDef token to revert
//
// Return Value:
//      HRESULT indicating success or failure.  If the method was never
//      rejitted in the first place, this method returns a special error code
//      (CORPROF_E_ACTIVE_REJIT_REQUEST_NOT_FOUND).
//      E_OUTOFMEMORY
//

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4702) // Disable bogus unreachable code warning
#endif // _MSC_VER
HRESULT ReJitManager::RequestRevertByToken(PTR_Module pModule, mdMethodDef methodDef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(ThreadStore::HoldingThreadStore());
    CrstHolder ch(&m_crstTable);

    _ASSERTE(pModule != NULL);
    _ASSERTE(methodDef != mdTokenNil);

    ReJitInfo * pInfo = NULL;
    MethodDesc * pMD = NULL;

    pInfo = FindNonRevertedReJitInfo(pModule, methodDef);
    if (pInfo == NULL)
    {
        pMD = pModule->LookupMethodDef(methodDef);
        pInfo = FindNonRevertedReJitInfo(pMD);
        if (pInfo == NULL)
            return CORPROF_E_ACTIVE_REJIT_REQUEST_NOT_FOUND;
    }

    _ASSERTE (pInfo != NULL);
    _ASSERTE (pInfo->m_pShared != NULL);
    _ASSERTE (pInfo->m_pShared->GetState() != SharedReJitInfo::kStateReverted);
    ReJitManagerJumpStampBatch batch(this);
    HRESULT hr = Revert(pInfo->m_pShared, &batch);
    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }
    CDynArray<ReJitReportErrorWorkItem> errorRecords;
    hr = BatchUpdateJumpStamps(&(batch.undoMethods), &(batch.preStubMethods), &errorRecords);
    if (FAILED(hr))
    {
        _ASSERTE(hr == E_OUTOFMEMORY);
        return hr;
    }

    // If there were any errors, return the first one. This matches previous error handling
    // behavior that only returned the first error encountered within Revert().
    for (int i = 0; i < errorRecords.Count(); i++)
    {
        _ASSERTE(FAILED(errorRecords[i].hrStatus));
        return errorRecords[i].hrStatus;
    }
    return S_OK;
}
#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER



//---------------------------------------------------------------------------------------
//
// Called by the prestub worker, this function decides if the MethodDesc needs to be
// rejitted, and if so, this will call the profiler to get the rejit parameters (if they
// are not yet stored), and then perform the actual re-JIT (by calling, indirectly,
// UnsafeJitFunction).
// 
// In order to allow the re-JIT to occur outside of any locks, the following sequence is
// performed:
// 
//     * Enter this ReJitManager's table crst
//       * Find the single ReJitInfo (if any) in the table matching the input pMD. This
//           represents the outstanding rejit request against thie pMD
//     * If necessary, ask profiler for IL & codegen flags (by calling
//         GetReJITParameters()), thus transitioning the corresponding SharedReJitInfo
//         state kStateRequested-->kStateActive
//     * Exit this ReJitManager's table crst
// * (following steps occur when DoReJitIfNecessary() calls DoReJit())
//   * Call profiler's ReJitCompilationStarted()
//   * Call UnsafeJitFunction with the IL / codegen flags provided by profiler, as stored
//       on the SharedReJitInfo. Note that if another Rejit request came in, then we would
//       create new SharedReJitInfo & ReJitInfo structures to track it, rather than
//       modifying the ReJitInfo / SharedReJitInfo we found above. So the ReJitInfo we're
//       using here (outside the lock), is "fixed" in the sense that its IL / codegen flags
//       will not change.
//   * (below is where we handle any races that might have occurred between threads
//     simultaneously rejitting this function)
//   * Enter this ReJitManager's table crst
//     * Check to see if another thread has already published the rejitted PCODE to
//         ReJitInfo::m_pCode. If so, bail.
//     * If we're the winner, publish our rejitted PCODE to ReJitInfo::m_pCode...
//     * ...and update the jump-stamp at the top of the originally JITted code so that it
//         now points to our rejitted code (instead of the prestub)
//   * Exit this ReJitManager's table crst
//   * Call profiler's ReJitCompilationFinished()
//   * Fire relevant ETW events
//
// Arguments:
//      pMD - MethodDesc to decide whether to rejit
//
// Return Value:
//      * If a rejit was performed, the PCODE of the generated code.
//      * If the ReJitManager changed its mind and chose not to do a rejit (e.g., a
//          revert request raced with this rejit request, and the revert won), just
//          return the PCODE of the originally JITted code (pMD->GetNativeCode())
//      * Else, NULL (which means the ReJitManager doesn't know or care about this 
//          MethodDesc)
//

PCODE ReJitManager::DoReJitIfNecessaryWorker(PTR_MethodDesc pMD)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(!IsTableCrstOwnedByCurrentThread());

    // Fast-path: If the rejit map is empty, no need to look up anything. Do this outside
    // of a lock to impact our caller (the prestub worker) as little as possible. If the
    // map is nonempty, we'll acquire the lock at that point and do the lookup for real.
    if (m_table.GetCount() == 0)
    {
        return NULL;
    }

    HRESULT hr = S_OK;
    ReJitInfo * pInfoToRejit = NULL;
    Module* pModule = NULL;
    mdMethodDef methodDef = mdTokenNil;
    BOOL fNeedsParameters = FALSE;
    BOOL fWaitForParameters = FALSE;

    {
        // Serialize access to the rejit table.  Though once we find the ReJitInfo we want,
        // exit the Crst so we can ReJIT the method without holding a lock.
        CrstHolder ch(&m_crstTable);

        ReJitInfoHash::KeyIterator iter = GetBeginIterator(pMD);
        ReJitInfoHash::KeyIterator end = GetEndIterator(pMD);

        if (iter == end)
        {
            // No rejit actions necessary
            return NULL;
        }


        for (; iter != end; iter++)
        {
            ReJitInfo * pInfo = *iter;
            _ASSERTE(pInfo->GetMethodDesc() == pMD);
            _ASSERTE(pInfo->m_pShared != NULL);
            SharedReJitInfo * pShared = pInfo->m_pShared;

            switch (pShared->GetState())
            {
            case SharedReJitInfo::kStateRequested:
                if (pInfo->GetState() == ReJitInfo::kJumpNone)
                {
                    // We haven't actually suspended threads and jump-stamped the
                    // method's prolog so just ignore this guy
                    INDEBUG(AssertRestOfEntriesAreReverted(iter, end));
                    return NULL;
                }
                // When the SharedReJitInfo is still in the requested state, we haven't
                // gathered IL & codegen flags from the profiler yet.  So, we can't be
                // pointing to rejitted code already.  So we must be pointing to the prestub
                _ASSERTE(pInfo->GetState() == ReJitInfo::kJumpToPrestub);
                
                pInfo->GetModuleAndTokenRegardlessOfKeyType(&pModule, &methodDef);
                pShared->m_dwInternalFlags &= ~SharedReJitInfo::kStateMask;
                pShared->m_dwInternalFlags |= SharedReJitInfo::kStateGettingReJITParameters;
                pInfoToRejit = pInfo;
                fNeedsParameters = TRUE;
                break;

            case SharedReJitInfo::kStateGettingReJITParameters:
                if (pInfo->GetState() == ReJitInfo::kJumpNone)
                {
                    // We haven't actually suspended threads and jump-stamped the
                    // method's prolog so just ignore this guy
                    INDEBUG(AssertRestOfEntriesAreReverted(iter, end));
                    return NULL;
                }
                pInfoToRejit = pInfo;
                fWaitForParameters = TRUE;
                break;

            case SharedReJitInfo::kStateActive:
                INDEBUG(AssertRestOfEntriesAreReverted(iter, end));
                if (pInfo->GetState() == ReJitInfo::kJumpNone)
                {
                    // We haven't actually suspended threads and jump-stamped the
                    // method's prolog so just ignore this guy
                    return NULL;
                }
                if (pInfo->GetState() == ReJitInfo::kJumpToRejittedCode)
                {
                    // Looks like another thread has beat us in a race to rejit, so ignore.
                    return NULL;
                }

                // Found a ReJitInfo to actually rejit.
                _ASSERTE(pInfo->GetState() == ReJitInfo::kJumpToPrestub);
                pInfoToRejit = pInfo;
                goto ExitLoop;

            case SharedReJitInfo::kStateReverted:
                // just ignore this guy
                continue;

            default:
                UNREACHABLE();
            }
        }
    ExitLoop:
        ;
    }

    if (pInfoToRejit == NULL)
    {
        // Didn't find the requested MD to rejit.
        return NULL;
    }

    if (fNeedsParameters)
    {
        // Here's where we give a chance for the rejit requestor to
        // examine and modify the IL & codegen flags before it gets to
        // the JIT. This allows one to add probe calls for things like
        // code coverage, performance, or whatever. These will be
        // stored in pShared.
        _ASSERTE(pModule != NULL);
        _ASSERTE(methodDef != mdTokenNil);
        ReleaseHolder<ProfilerFunctionControl> pFuncControl =
            new (nothrow)ProfilerFunctionControl(pModule->GetLoaderAllocator()->GetLowFrequencyHeap());
        HRESULT hr = S_OK;
        if (pFuncControl == NULL)
        {
            hr = E_OUTOFMEMORY;
        }
        else
        {
            BEGIN_PIN_PROFILER(CORProfilerPresent());
            hr = g_profControlBlock.pProfInterface->GetReJITParameters(
                (ModuleID)pModule,
                methodDef,
                pFuncControl);
            END_PIN_PROFILER();
        }

        if (FAILED(hr))
        {
            {
                CrstHolder ch(&m_crstTable);
                if (pInfoToRejit->m_pShared->m_dwInternalFlags == SharedReJitInfo::kStateGettingReJITParameters)
                {
                    pInfoToRejit->m_pShared->m_dwInternalFlags &= ~SharedReJitInfo::kStateMask;
                    pInfoToRejit->m_pShared->m_dwInternalFlags |= SharedReJitInfo::kStateRequested;
                }
            }
            ReportReJITError(pModule, methodDef, pMD, hr);
            return NULL;
        }

        {
            CrstHolder ch(&m_crstTable);
            if (pInfoToRejit->m_pShared->m_dwInternalFlags == SharedReJitInfo::kStateGettingReJITParameters)
            {
                // Inside the above call to ICorProfilerCallback4::GetReJITParameters, the profiler
                // will have used the specified pFuncControl to provide its IL and codegen flags. 
                // So now we transfer it out to the SharedReJitInfo.
                pInfoToRejit->m_pShared->m_dwCodegenFlags = pFuncControl->GetCodegenFlags();
                pInfoToRejit->m_pShared->m_pbIL = pFuncControl->GetIL();
                // pShared is now the owner of the memory for the IL buffer
                pInfoToRejit->m_pShared->m_instrumentedILMap.SetMappingInfo(pFuncControl->GetInstrumentedMapEntryCount(),
                    pFuncControl->GetInstrumentedMapEntries());
                pInfoToRejit->m_pShared->m_dwInternalFlags &= ~SharedReJitInfo::kStateMask;
                pInfoToRejit->m_pShared->m_dwInternalFlags |= SharedReJitInfo::kStateActive;
                _ASSERTE(pInfoToRejit->m_pCode == NULL);
                _ASSERTE(pInfoToRejit->GetState() == ReJitInfo::kJumpToPrestub);
            }
        }
    }
    else if (fWaitForParameters)
    {
        // This feels lame, but it doesn't appear like we have the good threading primitves
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
                CrstHolder ch(&m_crstTable);
                if (pInfoToRejit->m_pShared->GetState() == SharedReJitInfo::kStateActive)
                {
                    break; // the other thread got the parameters succesfully, go race to rejit
                }
                else if (pInfoToRejit->m_pShared->GetState() == SharedReJitInfo::kStateRequested)
                {
                    return NULL; // the other thread had an error getting parameters and went
                                 // back to requested
                }
                else if (pInfoToRejit->m_pShared->GetState() == SharedReJitInfo::kStateReverted)
                {
                    break; // we got reverted, enter DoReJit anyways and it will detect this and
                           // bail out.
                }
            }
            ClrSleepEx(1, FALSE);
        }
    }

    // We've got the info from the profiler, so JIT the method.  This is also
    // responsible for updating the jump target from the prestub to the newly
    // rejitted code AND for publishing the top of the newly rejitted code to
    // pInfoToRejit->m_pCode.  If two threads race to rejit, DoReJit handles the
    // race, and ensures the winner publishes his result to pInfoToRejit->m_pCode.
    return DoReJit(pInfoToRejit);

}


//---------------------------------------------------------------------------------------
//
// Called by DoReJitIfNecessaryWorker(), this function assumes the IL & codegen flags have
// already been gathered from the profiler, and then calls UnsafeJitFunction to perform
// the re-JIT (bracketing that with profiler callbacks to announce the start/finish of
// the rejit).
// 
// This is also responsible for handling any races between multiple threads
// simultaneously rejitting a function.  See the comment at the top of
// code:ReJitManager::DoReJitIfNecessaryWorker for details.
//
// Arguments:
//      pInfo - ReJitInfo tracking this MethodDesc's rejit request
//
// Return Value:
//      * Generally, return the PCODE of the start of the rejitted code.  However,
//          depending on the result of races determined by DoReJit(), the return value
//          can be different:
//      * If the current thread races with another thread to do the rejit, return the
//          PCODE generated by the winner.
//      * If the current thread races with another thread doing a revert, and the revert
//          wins, then return the PCODE of the start of the originally JITted code
//          (i.e., pInfo->GetMethodDesc()->GetNativeCode())
//

PCODE ReJitManager::DoReJit(ReJitInfo * pInfo)
{
    STANDARD_VM_CONTRACT;

#ifdef PROFILING_SUPPORTED

    INDEBUG(Dump("Inside DoRejit().  Dumping this ReJitManager\n"));

    _ASSERTE(!pInfo->GetMethodDesc()->IsNoMetadata());
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackJITInfo());
        g_profControlBlock.pProfInterface->ReJITCompilationStarted((FunctionID)pInfo->GetMethodDesc(),
            pInfo->m_pShared->GetId(),
            TRUE);
        END_PIN_PROFILER();
    }

    COR_ILMETHOD_DECODER ILHeader(pInfo->GetIL(), pInfo->GetMethodDesc()->GetMDImport(), NULL);
    PCODE pCodeOfRejittedCode = NULL;

    // Note that we're intentionally not enclosing UnsafeJitFunction in a try block
    // to swallow exceptions.  It's expected that any exception thrown is fatal and
    // should pass through.  This is in contrast to MethodDesc::MakeJitWorker, which
    // does enclose UnsafeJitFunction in a try block, and attempts to swallow an
    // exception that occurs on the current thread when another thread has
    // simultaneously attempted (and provably succeeded in) the JITting of the same
    // function.  This is a very unusual case (likely due to an out of memory error
    // encountered on the current thread and not on the competing thread), which is
    // not worth attempting to cover.
    pCodeOfRejittedCode = UnsafeJitFunction(
        pInfo->GetMethodDesc(),
        &ILHeader,
        JitFlagsFromProfCodegenFlags(pInfo->m_pShared->m_dwCodegenFlags),
        0);

    _ASSERTE(pCodeOfRejittedCode != NULL);

    // This atomically updates the jmp target (from prestub to top of rejitted code) and publishes
    // the top of rejitted code into pInfo, all inside the same acquisition of this
    // ReJitManager's table Crst.
    HRESULT hr = S_OK;
    BOOL fEESuspended = FALSE;
    BOOL fNotify = FALSE;
    PCODE ret = NULL;
    while (true)
    {
        if (fEESuspended)
        {
            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_REJIT);
        }
        CrstHolder ch(&m_crstTable);

        // Now that we're under the lock, recheck whether pInfo->m_pCode has been filled
        // in...
        if (pInfo->m_pCode != NULL)
        {
            // Yup, another thread rejitted this request at the same time as us, and beat
            // us to publishing the result. Intentionally skip the rest of this, and do
            // not issue a ReJITCompilationFinished from this thread.
            ret = pInfo->m_pCode;
            break;
        }
        
        // BUGBUG: This revert check below appears to introduce behavior we probably don't want.
        // This is a pre-existing issue and I don't have time to create a test for this right now,
        // but wanted to capture the issue in a comment for future work.
        // Imagine the profiler has one thread which is calling RequestReJIT periodically
        // updating the method's IL:
        //   1) RequestReJit (table lock keeps these atomic)
        //     1.1) Revert old shared rejit info
        //     1.2) Create new shared rejit info
        //   2) RequestReJit (table lock keeps these atomic)
        //     2.1) Revert old shared rejit info
        //     2.2) Create new shared rejit info
        //   ...
        // On a second thread we keep calling the method which needs to periodically rejit
        // to update to the newest version:
        //   a) [DoReJitIfNecessaryWorker] detects active rejit request
        //   b) [DoReJit] if shared rejit info is reverted, execute original method code.
        //
        // Because (a) and (b) are not under the same lock acquisition this ordering is possible:
        // (1), (a), (2), (b)
        // The result is that (b) sees the shared rejit is reverted and the method executes its
        // original code. As a profiler using rejit I would expect either the IL specified in
        // (1) or the IL specified in (2) would be used, but never the original IL.
        //
        // I think the correct behavior is to bind a method execution to the current rejit
        // version at some point, and from then on we guarantee to execute that version of the
        // code, regardless of reverts or re-rejit request.
        //
        // There is also a related issue with GetCurrentReJitFlagsWorker which assumes jitting
        // always corresponds to the most recent version of the method. If we start pinning
        // method invocations to particular versions then that method can't be allowed to
        // float forward to the newest version, nor can it abort if the most recent version
        // is reverted.
        // END BUGBUG
        // 
        // And recheck whether some other thread tried to revert this method in the
        // meantime (this check would also include an attempt to re-rejit the method
        // (i.e., calling RequestReJIT on the method multiple times), which would revert
        // this pInfo before creating a new one to track the latest rejit request).
        if (pInfo->m_pShared->GetState() == SharedReJitInfo::kStateReverted)
        {
            // Yes, we've been reverted, so the jmp-to-prestub has already been removed,
            // and we should certainly not attempt to redirect that nonexistent jmp to
            // the code we just rejitted
            _ASSERTE(pInfo->GetMethodDesc()->GetNativeCode() != NULL);
            ret = pInfo->GetMethodDesc()->GetNativeCode();
            break;
        }

#ifdef DEBUGGING_SUPPORTED
        // Notify the debugger of the rejitted function, so it can generate
        // DebuggerMethodInfo / DebugJitInfo for it. Normally this is done inside
        // UnsafeJitFunction (via CallCompileMethodWithSEHWrapper), but it skips this
        // when it detects the MethodDesc was already jitted. Since we know here that
        // we're rejitting it (and this is not just some sort of multi-thread JIT race),
        // now is a good place to notify the debugger.
        if (g_pDebugInterface != NULL)
        {
            g_pDebugInterface->JITComplete(pInfo->GetMethodDesc(), pCodeOfRejittedCode);
        }

#endif // DEBUGGING_SUPPORTED

        _ASSERTE(pInfo->m_pShared->GetState() == SharedReJitInfo::kStateActive);
        _ASSERTE(pInfo->GetState() == ReJitInfo::kJumpToPrestub);

        // Atomically publish the PCODE and update the jmp stamp (to go to the rejitted
        // code) under the lock
        hr = pInfo->UpdateJumpTarget(fEESuspended, pCodeOfRejittedCode);
        if (hr == CORPROF_E_RUNTIME_SUSPEND_REQUIRED)
        {
            _ASSERTE(!fEESuspended);
            fEESuspended = TRUE;
            continue;
        }
        if (FAILED(hr))
        {
            break;
        }
        pInfo->m_pCode = pCodeOfRejittedCode;
        fNotify = TRUE;
        ret = pCodeOfRejittedCode;

        _ASSERTE(pInfo->m_pShared->GetState() == SharedReJitInfo::kStateActive);
        _ASSERTE(pInfo->GetState() == ReJitInfo::kJumpToRejittedCode);
        break;
    }

    if (fEESuspended)
    {
        ThreadSuspend::RestartEE(FALSE /* bFinishedGC */, TRUE /* SuspendSucceded */);
        fEESuspended = FALSE;
    }

    if (FAILED(hr))
    {
        Module* pModule = NULL;
        mdMethodDef methodDef = mdTokenNil;
        pInfo->GetModuleAndTokenRegardlessOfKeyType(&pModule, &methodDef);
        ReportReJITError(pModule, methodDef, pInfo->GetMethodDesc(), hr);
    }

    // Notify the profiler that JIT completed.
    if (fNotify)
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackJITInfo());
        g_profControlBlock.pProfInterface->ReJITCompilationFinished((FunctionID)pInfo->GetMethodDesc(),
            pInfo->m_pShared->GetId(),
            S_OK,
            TRUE);
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED

    // Fire relevant ETW events
    if (fNotify)
    {
        ETW::MethodLog::MethodJitted(
            pInfo->GetMethodDesc(),
            NULL,               // namespaceOrClassName
            NULL,               // methodName
            NULL,               // methodSignature
            pCodeOfRejittedCode,
            pInfo->m_pShared->GetId());
    }
    return ret;
}


//---------------------------------------------------------------------------------------
//
// Transition SharedReJitInfo to Reverted state and add all associated ReJitInfos to the
// undo list in the method batch
//
// Arguments:
//      pShared - SharedReJitInfo to revert
//      pJumpStampBatch - a batch of methods that need their jump stamps reverted. This method
//                        is responsible for adding additional ReJitInfos to the list.
//
// Return Value:
//      S_OK if all MDs are batched and the SharedReJitInfo is marked reverted 
//      E_OUTOFMEMORY (MDs couldn't be added to batch, SharedReJitInfo is not reverted)
//
// Assumptions:
//      Caller must be holding this ReJitManager's table crst.
//

HRESULT ReJitManager::Revert(SharedReJitInfo * pShared, ReJitManagerJumpStampBatch* pJumpStampBatch)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_crstTable.OwnedByCurrentThread());
    _ASSERTE((pShared->GetState() == SharedReJitInfo::kStateRequested) ||
             (pShared->GetState() == SharedReJitInfo::kStateGettingReJITParameters) ||
             (pShared->GetState() == SharedReJitInfo::kStateActive));
    _ASSERTE(pShared->GetMethods() != NULL);
    _ASSERTE(pJumpStampBatch->pReJitManager == this);

    HRESULT hrReturn = S_OK;
    for (ReJitInfo * pInfo = pShared->GetMethods(); pInfo != NULL; pInfo = pInfo->m_pNext)
    {
        if (pInfo->GetState() == ReJitInfo::kJumpNone)
        {
            // Nothing to revert for this MethodDesc / instantiation.
            continue;
        }

        ReJitInfo** ppInfo = pJumpStampBatch->undoMethods.Append();
        if (ppInfo == NULL)
        {
            return E_OUTOFMEMORY;
        }
        *ppInfo = pInfo;
    }

    pShared->m_dwInternalFlags &= ~SharedReJitInfo::kStateMask;
    pShared->m_dwInternalFlags |= SharedReJitInfo::kStateReverted;
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Removes any ReJitInfos relating to MDs for the specified AppDomain from this
// ReJitManager. This is used to remove non-domain-neutral instantiations of
// domain-neutral generics from the SharedDomain's ReJitManager, when the AppDomain
// containing those non-domain-neutral instantiations is unloaded.
//
// Arguments:
//      * pAppDomain - AppDomain that is exiting, and is thus the one for which we should
//          find ReJitInfos to remove
//
//

void ReJitManager::RemoveReJitInfosFromDomain(AppDomain * pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder ch(&m_crstTable);

    INDEBUG(Dump("Dumping SharedDomain rejit manager BEFORE AD Unload"));

    for (ReJitInfoHash::Iterator iterCur = m_table.Begin(), iterEnd = m_table.End();
        iterCur != iterEnd; 
        iterCur++)
    {
        ReJitInfo * pInfo = *iterCur;

        if (pInfo->m_key.m_keyType != ReJitInfo::Key::kMethodDesc)
        {
            // Skip all "placeholder" ReJitInfos--they'll always be allocated on a
            // loader heap for the shared domain.
            _ASSERTE(pInfo->m_key.m_keyType == ReJitInfo::Key::kMetadataToken);
            _ASSERTE(PTR_Module(pInfo->m_key.m_pModule)->GetDomain()->IsSharedDomain());
            continue;
        }

        if (pInfo->GetMethodDesc()->GetDomain() != pAppDomain)
        {
            // We only care about non-domain-neutral instantiations that live in
            // pAppDomain.
            continue;
        }

        // Remove this ReJitInfo from the linked-list of ReJitInfos associated with its
        // SharedReJitInfo.
        pInfo->m_pShared->RemoveMethod(pInfo);

        // Remove this ReJitInfo from the ReJitManager's hash table.
        m_table.Remove(iterCur);

        // pInfo is not deallocated yet.  That will happen when pAppDomain finishes
        // unloading and its loader heaps get freed.
    }
    INDEBUG(Dump("Dumping SharedDomain rejit manager AFTER AD Unload"));
}

#endif // DACCESS_COMPILE
// The rest of the ReJitManager methods are safe to compile for DAC


//---------------------------------------------------------------------------------------
//
// Helper to iterate through m_table, finding the single matching non-reverted ReJitInfo.
// The caller may search either by MethodDesc * XOR by (Module *, methodDef) pair.
//
// Arguments:
//      * pMD - MethodDesc * to search for. (NULL if caller is searching by (Module *,
//          methodDef)
//      * pModule - Module * to search for. (NULL if caller is searching by MethodDesc *)
//      * methodDef - methodDef to search for. (NULL if caller is searching by MethodDesc
//          *)
//
// Return Value:
//      ReJitInfo * requested, or NULL if none is found
//
// Assumptions:
//      Caller should be holding this ReJitManager's table crst.
//

PTR_ReJitInfo ReJitManager::FindNonRevertedReJitInfoHelper(
    PTR_MethodDesc pMD, 
    PTR_Module pModule, 
    mdMethodDef methodDef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    // Either pMD is valid, xor (pModule,methodDef) is valid
    _ASSERTE(
        ((pMD != NULL) && (pModule == NULL) && (methodDef == mdTokenNil)) ||
        ((pMD == NULL) && (pModule != NULL) && (methodDef != mdTokenNil)));

    // Caller should hold the Crst around calling this function and using the ReJitInfo.
#ifndef DACCESS_COMPILE
    _ASSERTE(m_crstTable.OwnedByCurrentThread());
#endif

    ReJitInfoHash::KeyIterator beginIter(&m_table, TRUE /* begin */); 
    ReJitInfoHash::KeyIterator endIter(&m_table, FALSE /* begin */);

    if (pMD != NULL)
    {
        beginIter = GetBeginIterator(pMD);
        endIter = GetEndIterator(pMD);
    }
    else
    {
        beginIter = GetBeginIterator(pModule, methodDef);
        endIter = GetEndIterator(pModule, methodDef);
    }

    for (ReJitInfoHash::KeyIterator iter = beginIter;
        iter != endIter; 
        iter++)
    {
        PTR_ReJitInfo pInfo = *iter;
        _ASSERTE(pInfo->m_pShared != NULL);

        if (pInfo->m_pShared->GetState() == SharedReJitInfo::kStateReverted)
            continue;

        INDEBUG(AssertRestOfEntriesAreReverted(iter, endIter));
        return pInfo;
    }

    return NULL;
}


//---------------------------------------------------------------------------------------
//
// ReJitManager instance constructor--for now, does nothing
//

ReJitManager::ReJitManager()
{
    LIMITED_METHOD_DAC_CONTRACT;
}


//---------------------------------------------------------------------------------------
//
// Called from BaseDomain::BaseDomain to do any constructor-time initialization.
// Presently, this takes care of initializing the Crst, choosing the type based on
// whether this ReJitManager belongs to the SharedDomain.
//
// Arguments:
//    * fSharedDomain - nonzero iff this ReJitManager belongs to the SharedDomain.
//    

void ReJitManager::PreInit(BOOL fSharedDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        CAN_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    m_crstTable.Init(
        fSharedDomain ? CrstReJITSharedDomainTable : CrstReJITDomainTable,
        CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN));
#endif // DACCESS_COMPILE
}


//---------------------------------------------------------------------------------------
//
// Finds the ReJitInfo tracking a pre-rejit request.
//
// Arguments:
//    * beginIter - Iterator to start search
//    * endIter - Iterator to end search
//
// Return Value:
//      NULL if no such ReJitInfo exists.  This can occur if two thread race
//      to JIT the original code and we're the loser.  Else, the ReJitInfo * found.
//
// Assumptions:
//      Caller must be holding this ReJitManager's table lock.
//

ReJitInfo * ReJitManager::FindPreReJittedReJitInfo(
    ReJitInfoHash::KeyIterator beginIter,
    ReJitInfoHash::KeyIterator endIter)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Caller shouldn't be handing out iterators unless he's already locking the table.
#ifndef DACCESS_COMPILE
    _ASSERTE(m_crstTable.OwnedByCurrentThread());
#endif

    for (ReJitInfoHash::KeyIterator iter = beginIter;
        iter != endIter; 
        iter++)
    {
        ReJitInfo * pInfo = *iter;
        SharedReJitInfo * pShared = pInfo->m_pShared;
        _ASSERTE(pShared != NULL);

        switch (pShared->GetState())
        {
        case SharedReJitInfo::kStateRequested:
        case SharedReJitInfo::kStateGettingReJITParameters:
        case SharedReJitInfo::kStateActive:
            if (pInfo->GetState() == ReJitInfo::kJumpToRejittedCode)
            {
                // There was a race for the original JIT, and we're the loser.  (The winner
                // has already published the original JIT's pcode, jump-stamped, and begun
                // the rejit!)
                return NULL;
            }

            // Otherwise, either we have a rejit request that has not yet been
            // jump-stamped, or there was a race for the original JIT, and another
            // thread jump-stamped its copy of the originally JITted code already.  In
            // that case, we still don't know who the winner or loser will be (PCODE may
            // not yet be published), so we'll have to jump-stamp our copy just in case
            // we win.
            _ASSERTE((pInfo->GetState() == ReJitInfo::kJumpNone) ||
                     (pInfo->GetState() == ReJitInfo::kJumpToPrestub));
            INDEBUG(AssertRestOfEntriesAreReverted(iter, endIter));
            return pInfo;


        case SharedReJitInfo::kStateReverted:
            // just ignore this guy
            continue;

        default:
            UNREACHABLE();
        }
    }

    return NULL;
}

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

ReJITID ReJitManager::GetReJitId(PTR_MethodDesc pMD, PCODE pCodeStart)
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
        GC_TRIGGERS;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pCodeStart != NULL);
    }
    CONTRACTL_END;

    // Fast-path: If the rejit map is empty, no need to look up anything. Do this outside
    // of a lock to impact our caller (the prestub worker) as little as possible. If the
    // map is nonempty, we'll acquire the lock at that point and do the lookup for real.
    if (m_table.GetCount() == 0)
    {
        return 0;
    }

    CrstHolder ch(&m_crstTable);

    return GetReJitIdNoLock(pMD, pCodeStart);
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
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pCodeStart != NULL);
    }
    CONTRACTL_END;

    // Caller must ensure this lock is taken!
    _ASSERTE(m_crstTable.OwnedByCurrentThread());

    ReJitInfo * pInfo = FindReJitInfo(pMD, pCodeStart, 0);
    if (pInfo == NULL)
    {
        return 0;
    }

    _ASSERTE(pInfo->m_pShared->GetState() == SharedReJitInfo::kStateActive ||
        pInfo->m_pShared->GetState() == SharedReJitInfo::kStateReverted);
    return pInfo->m_pShared->GetId();
}


//---------------------------------------------------------------------------------------
//
// Used by profilers to map a (MethodDesc *, ReJITID) pair to the corresponding PCODE for
// that rejit attempt. This can also be used for reverted methods, as the PCODE may still
// be available and in use even after a rejitted function has been reverted.
//
// Arguments:
//      * pMD - MethodDesc * of interest
//      * reJitId - ReJITID of interest
//
// Return Value:
//      Corresponding PCODE of the rejit attempt, or NULL if no such rejit attempt can be
//      found.
//

PCODE ReJitManager::GetCodeStart(PTR_MethodDesc pMD, ReJITID reJitId)
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(reJitId != 0);
    }
    CONTRACTL_END;

    // Fast-path: If the rejit map is empty, no need to look up anything. Do this outside
    // of a lock to impact our caller (the prestub worker) as little as possible. If the
    // map is nonempty, we'll acquire the lock at that point and do the lookup for real.
    if (m_table.GetCount() == 0)
    {
        return NULL;
    }

    CrstHolder ch(&m_crstTable);

    ReJitInfo * pInfo = FindReJitInfo(pMD, NULL, reJitId);
    if (pInfo == NULL)
    {
        return NULL;
    }

    _ASSERTE(pInfo->m_pShared->GetState() == SharedReJitInfo::kStateActive ||
             pInfo->m_pShared->GetState() == SharedReJitInfo::kStateReverted);
    
    return pInfo->m_pCode;
}


//---------------------------------------------------------------------------------------
//
// If a function has been requested to be rejitted, finds the one current
// SharedReJitInfo (ignoring all that are in the reverted state) and returns the codegen
// flags recorded on it (which were thus used to rejit the MD). CEEInfo::canInline() calls
// this as part of its calculation of whether it may inline a given method. (Profilers
// may specify on a per-rejit-request basis whether the rejit of a method may inline
// callees.)
//
// Arguments:
//      * pMD - MethodDesc * of interest.
//
// Return Value:
//     Returns the requested codegen flags, or 0 (i.e., no flags set) if no rejit attempt
//     can be found for the MD.
//

DWORD ReJitManager::GetCurrentReJitFlagsWorker(PTR_MethodDesc pMD)
{
    CONTRACTL 
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMD));
    } 
    CONTRACTL_END;

    // Fast-path: If the rejit map is empty, no need to look up anything. Do this outside
    // of a lock to impact our caller (e.g., the JIT asking if it can inline) as little as possible. If the
    // map is nonempty, we'll acquire the lock at that point and do the lookup for real.
    if (m_table.GetCount() == 0)
    {
        return 0;
    }

    CrstHolder ch(&m_crstTable);

    for (ReJitInfoHash::KeyIterator iter = GetBeginIterator(pMD), end = GetEndIterator(pMD);
        iter != end; 
        iter++)
    {
        ReJitInfo * pInfo = *iter;
        _ASSERTE(pInfo->GetMethodDesc() == pMD);
        _ASSERTE(pInfo->m_pShared != NULL);

        DWORD dwState = pInfo->m_pShared->GetState();

        if (dwState != SharedReJitInfo::kStateActive)
        {
            // Not active means we never asked profiler for the codegen flags OR the
            // rejit request has been reverted. So this one is useless.
            continue;
        }

        // Found it!
#ifdef _DEBUG
        // This must be the only such ReJitInfo for this MethodDesc.  Check the rest and
        // assert otherwise.
        {
            ReJitInfoHash::KeyIterator iterTest = iter;
            iterTest++;

            while(iterTest != end)
            {
                ReJitInfo * pInfoTest = *iterTest;
                _ASSERTE(pInfoTest->GetMethodDesc() == pMD);
                _ASSERTE(pInfoTest->m_pShared != NULL);

                DWORD dwStateTest = pInfoTest->m_pShared->GetState();

                if (dwStateTest == SharedReJitInfo::kStateActive)
                {
                    _ASSERTE(!"Multiple active ReJitInfos for same MethodDesc");
                    break;
                }
                iterTest++;
            }
        }
#endif //_DEBUG
        return pInfo->m_pShared->m_dwCodegenFlags;
    }

    return 0;
}

//---------------------------------------------------------------------------------------
//
// Helper to find the matching ReJitInfo by methoddesc paired with either pCodeStart or
// reJitId (exactly one should be non-zero, and will be used as the key for the lookup)
//
// Arguments:
//      * pMD - MethodDesc * to look up
//      * pCodeStart - PCODE of the particular rejit attempt to look up. NULL if looking
//          up by ReJITID.
//      * reJitId - ReJITID of the particular rejit attempt to look up. NULL if looking
//          up by PCODE.
//
// Return Value:
//      ReJitInfo * matching input parameters, or NULL if no such ReJitInfo could be
//      found.
//
// Assumptions:
//      Caller must be holding this ReJitManager's table lock.
//

PTR_ReJitInfo ReJitManager::FindReJitInfo(PTR_MethodDesc pMD, PCODE pCodeStart, ReJITID reJitId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    // Caller should hold the Crst around calling this function and using the ReJitInfo.
#ifndef DACCESS_COMPILE
    _ASSERTE(m_crstTable.OwnedByCurrentThread());
#endif

    // One of these two keys should be used, but not both!
    _ASSERTE(
        ((pCodeStart != NULL) || (reJitId != 0)) &&
        !((pCodeStart != NULL) && (reJitId != 0)));

    for (ReJitInfoHash::KeyIterator iter = GetBeginIterator(pMD), end = GetEndIterator(pMD);
        iter != end; 
        iter++)
    {
        PTR_ReJitInfo pInfo = *iter;
        _ASSERTE(pInfo->GetMethodDesc() == pMD);
        _ASSERTE(pInfo->m_pShared != NULL);

        if ((pCodeStart != NULL && pInfo->m_pCode == pCodeStart) ||      // pCodeStart is key
            (reJitId != 0 && pInfo->m_pShared->GetId() == reJitId))      // reJitId is key
        {
            return pInfo;
        }
    }

    return NULL;
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

HRESULT ReJitManager::GetReJITIDs(PTR_MethodDesc pMD, ULONG cReJitIds, ULONG * pcReJitIds, ReJITID reJitIds[])
{
    CONTRACTL
    {
        NOTHROW;
        CAN_TAKE_LOCK;
        GC_NOTRIGGER;
        INSTANCE_CHECK;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(pcReJitIds != NULL);
        PRECONDITION(reJitIds != NULL);
    }
    CONTRACTL_END;

    CrstHolder ch(&m_crstTable);

    ULONG cnt = 0;

    for (ReJitInfoHash::KeyIterator iter = GetBeginIterator(pMD), end = GetEndIterator(pMD);
        iter != end; 
        iter++)
    {
        ReJitInfo * pInfo = *iter;
        _ASSERTE(pInfo->GetMethodDesc() == pMD);
        _ASSERTE(pInfo->m_pShared != NULL);

        if (pInfo->m_pShared->GetState() == SharedReJitInfo::kStateActive ||
            pInfo->m_pShared->GetState() == SharedReJitInfo::kStateReverted)
        {
            if (cnt < cReJitIds)
            {
                reJitIds[cnt] = pInfo->m_pShared->GetId();
            }
            ++cnt;

            // no overflow
            _ASSERTE(cnt != 0);
        }
    }
    *pcReJitIds = cnt;

    return (cnt > cReJitIds) ? S_FALSE : S_OK;
}

//---------------------------------------------------------------------------------------
//
// Helper that inits a new ReJitReportErrorWorkItem and adds it to the pErrors array
//
// Arguments:
//      * pModule - The module in the module/MethodDef identifier pair for the method which
//                  had an error during rejit
//      * methodDef - The MethodDef in the module/MethodDef identifier pair for the method which
//                  had an error during rejit
//      * pMD - If available, the specific method instance which had an error during rejit
//      * hrStatus - HRESULT for the rejit error that occurred
//      * pErrors - the list of error records that this method will append to
//
// Return Value:
//      * S_OK: error was appended
//      * E_OUTOFMEMORY: Not enough memory to create the new error item. The array is unchanged.
//

//static
HRESULT ReJitManager::AddReJITError(Module* pModule, mdMethodDef methodDef, MethodDesc* pMD, HRESULT hrStatus, CDynArray<ReJitReportErrorWorkItem> * pErrors)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ReJitReportErrorWorkItem* pError = pErrors->Append();
    if (pError == NULL)
    {
        return E_OUTOFMEMORY;
    }
    pError->pModule = pModule;
    pError->methodDef = methodDef;
    pError->pMethodDesc = pMD;
    pError->hrStatus = hrStatus;
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Helper that inits a new ReJitReportErrorWorkItem and adds it to the pErrors array
//
// Arguments:
//      * pReJitInfo - The method which had an error during rejit
//      * hrStatus - HRESULT for the rejit error that occurred
//      * pErrors - the list of error records that this method will append to
//
// Return Value:
//      * S_OK: error was appended
//      * E_OUTOFMEMORY: Not enough memory to create the new error item. The array is unchanged.
//

//static
HRESULT ReJitManager::AddReJITError(ReJitInfo* pReJitInfo, HRESULT hrStatus, CDynArray<ReJitReportErrorWorkItem> * pErrors)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    Module * pModule = NULL;
    mdMethodDef methodDef = mdTokenNil;
    pReJitInfo->GetModuleAndTokenRegardlessOfKeyType(&pModule, &methodDef);
    return AddReJITError(pModule, methodDef, pReJitInfo->GetMethodDesc(), hrStatus, pErrors);
}

#ifdef _DEBUG
//---------------------------------------------------------------------------------------
//
// Debug-only helper used while iterating through the hash table of
// ReJitInfos to verify that all entries between the specified iterators are
// reverted.  Asserts if it finds any non-reverted entries.
//
// Arguments:
//    * iter - Iterator to start verifying at
//    * end - Iterator to stop verifying at
//
//

void ReJitManager::AssertRestOfEntriesAreReverted(
    ReJitInfoHash::KeyIterator iter, 
    ReJitInfoHash::KeyIterator end)
{
    LIMITED_METHOD_CONTRACT;

    // All other rejits should be in the reverted state
    while (++iter != end)
    {
        _ASSERTE((*iter)->m_pShared->GetState() == SharedReJitInfo::kStateReverted);
    }
}


//---------------------------------------------------------------------------------------
//
// Debug-only helper to dump ReJitManager contents to stdout. Only used if
// COMPlus_ProfAPI_EnableRejitDiagnostics is set.
//
// Arguments:
//      * szIntroText - Intro text passed by caller to be output before this ReJitManager
//          is dumped.
//
//

void ReJitManager::Dump(LPCSTR szIntroText)
{
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ProfAPI_EnableRejitDiagnostics) == 0)
        return;

    printf(szIntroText);
    fflush(stdout);

    CrstHolder ch(&m_crstTable);

    printf("BEGIN ReJitManager::Dump: 0x%p\n", this);

    for (ReJitInfoHash::Iterator iterCur = m_table.Begin(), iterEnd = m_table.End();
        iterCur != iterEnd; 
        iterCur++)
    {
        ReJitInfo * pInfo = *iterCur;
        printf(
            "\tInfo 0x%p: State=0x%x, Next=0x%p, Shared=%p, SharedState=0x%x\n",
            pInfo,
            pInfo->GetState(),
            (void*)pInfo->m_pNext,
            (void*)pInfo->m_pShared,
            pInfo->m_pShared->GetState());

        switch(pInfo->m_key.m_keyType)
        {
        case ReJitInfo::Key::kMethodDesc:
            printf(
                "\t\tMD=0x%p, %s.%s (%s)\n",
                (void*)pInfo->GetMethodDesc(),
                pInfo->GetMethodDesc()->m_pszDebugClassName,
                pInfo->GetMethodDesc()->m_pszDebugMethodName,
                pInfo->GetMethodDesc()->m_pszDebugMethodSignature);
            break;

        case ReJitInfo::Key::kMetadataToken:
            Module * pModule;
            mdMethodDef methodDef;
            pInfo->GetModuleAndToken(&pModule, &methodDef);
            printf(
                "\t\tModule=0x%p, Token=0x%x\n",
                pModule,
                methodDef);
            break;

        case ReJitInfo::Key::kUninitialized:
            printf("\t\tUNINITIALIZED\n");
            break;

        default:
            _ASSERTE(!"Unrecognized pInfo key type");
        }
        fflush(stdout);
    }
    printf("END   ReJitManager::Dump: 0x%p\n", this);
    fflush(stdout);
}

#endif // _DEBUG

//---------------------------------------------------------------------------------------
// ReJitInfo implementation

// All the state-changey stuff is kept up here in the !DACCESS_COMPILE block.
// The more read-only inspection-y stuff follows the block.


#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Do the actual work of stamping the top of originally-jitted-code with a jmp that goes
// to the prestub. This can be called in one of three ways:
//     * Case 1: By RequestReJIT against an already-jitted function, in which case the
//         PCODE may be inferred by the MethodDesc, and our caller will have suspended
//         the EE for us, OR
//     * Case 2: By the prestub worker after jitting the original code of a function
//         (i.e., the "pre-rejit" scenario). In this case, the EE is not suspended. But
//         that's ok, because the PCODE has not yet been published to the MethodDesc, and
//         no thread can be executing inside the originally JITted function yet.
//     * Case 3: At type/method restore time for an NGEN'ed assembly. This is also the pre-rejit
//         scenario because we are guaranteed to do this before the code in the module
//         is executable. EE suspend is not required.
//
// Arguments:
//    * pCode - Case 1 (above): will be NULL, and we can infer the PCODE from the
//        MethodDesc; Case 2+3 (above, pre-rejit): will be non-NULL, and we'll need to use
//        this to find the code to stamp on top of.
//
// Return Value:
//    * S_OK: Either we successfully did the jmp-stamp, or a racing thread took care of
//        it for us.
//    * Else, HRESULT indicating failure.
//
// Assumptions:
//     The caller will have suspended the EE if necessary (case 1), before this is
//     called.
//
HRESULT ReJitInfo::JumpStampNativeCode(PCODE pCode /* = NULL */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        // It may seem dangerous to be stamping jumps over code while a GC is going on,
        // but we're actually safe. As we assert below, either we're holding the thread
        // store lock (and thus preventing a GC) OR we're stamping code that has not yet
        // been published (and will thus not be executed by managed therads or examined
        // by the GC).
        MODE_ANY;
    }
    CONTRACTL_END;

    PCODE pCodePublished = GetMethodDesc()->GetNativeCode();

    _ASSERTE((pCode != NULL) || (pCodePublished != NULL));
    _ASSERTE(GetMethodDesc()->GetReJitManager()->IsTableCrstOwnedByCurrentThread());

    HRESULT hr = S_OK;

    // We'll jump-stamp over pCode, or if pCode is NULL, jump-stamp over the published
    // code for this's MethodDesc.
    LPBYTE pbCode = (LPBYTE) pCode;
    if (pbCode == NULL)
    {
        // If caller didn't specify a pCode, just use the one that was published after
        // the original JIT.  (A specific pCode would be passed in the pre-rejit case,
        // to jump-stamp the original code BEFORE the PCODE gets published.)
        pbCode = (LPBYTE) pCodePublished;
    }
    _ASSERTE (pbCode != NULL);

    // The debugging API may also try to write to the very top of this function (though
    // with an int 3 for breakpoint purposes). Coordinate with the debugger so we know
    // whether we can safely patch the actual code, or instead write to the debugger's
    // buffer.
    DebuggerController::ControllerLockHolder lockController;

    // We could be in a race. Either two threads simultaneously JITting the same
    // method for the first time or two threads restoring NGEN'ed code.
    // Another thread may (or may not) have jump-stamped its copy of the code already
    _ASSERTE((GetState() == kJumpNone) || (GetState() == kJumpToPrestub));

    if (GetState() == kJumpToPrestub)
    {
        // The method has already been jump stamped so nothing left to do
        _ASSERTE(CodeIsSaved());
        return S_OK;
    }

    // Remember what we're stamping our jump on top of, so we can replace it during a
    // revert.
    for (int i = 0; i < sizeof(m_rgSavedCode); i++)
    {
        m_rgSavedCode[i] = *FirstCodeByteAddr(pbCode+i, DebuggerController::GetPatchTable()->GetPatch((CORDB_ADDRESS_TYPE *)(pbCode+i)));
    }

    EX_TRY
    {
        AllocMemTracker amt;

        // This guy might throw on out-of-memory, so rely on the tracker to clean-up
        Precode * pPrecode = Precode::Allocate(PRECODE_STUB, GetMethodDesc(), GetMethodDesc()->GetLoaderAllocator(), &amt);
        PCODE target = pPrecode->GetEntryPoint();

#if defined(_X86_) || defined(_AMD64_)

        // Normal unpatched code never starts with a jump
        // so make sure this code isn't already patched
        _ASSERTE(*FirstCodeByteAddr(pbCode, DebuggerController::GetPatchTable()->GetPatch((CORDB_ADDRESS_TYPE *)pbCode)) != X86_INSTR_JMP_REL32);

        INT64 i64OldCode = *(INT64*)pbCode;
        INT64 i64NewCode = i64OldCode;
        LPBYTE pbNewValue = (LPBYTE)&i64NewCode;
        *pbNewValue = X86_INSTR_JMP_REL32;
        INT32 UNALIGNED * pOffset = reinterpret_cast<INT32 UNALIGNED *>(&pbNewValue[1]);
        // This will throw for out-of-memory, so don't write anything until
        // after he succeeds
        // This guy will leak/cache/reuse the jumpstub
        *pOffset = rel32UsingJumpStub(reinterpret_cast<INT32 UNALIGNED *>(pbCode + 1), target, GetMethodDesc(), GetMethodDesc()->GetLoaderAllocator());

        // If we have the EE suspended or the code is unpublished there won't be contention on this code
        hr = UpdateJumpStampHelper(pbCode, i64OldCode, i64NewCode, FALSE);
        if (FAILED(hr))
        {
            ThrowHR(hr);
        }
            
        //
        // No failure point after this!
        //
        amt.SuppressRelease();

#else // _X86_ || _AMD64_
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"

#endif // _X86_ || _AMD64_

        m_dwInternalFlags &= ~kStateMask;
        m_dwInternalFlags |= kJumpToPrestub;
    }
    EX_CATCH_HRESULT(hr);
    _ASSERT(hr == S_OK || hr == E_OUTOFMEMORY);

    if (SUCCEEDED(hr))
    {
        _ASSERTE(GetState() == kJumpToPrestub);
        _ASSERTE(m_rgSavedCode[0] != 0); // saved code should not start with 0
    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Poke the JITted code to satsify a revert request (or to perform an implicit revert as
// part of a second, third, etc. rejit request). Reinstates the originally JITted code
// that had been jump-stamped over to perform a prior rejit.
//
// Arguments
//     fEESuspended - TRUE if the caller keeps the EE suspended during this call
//
//
// Return Value:
//     S_OK to indicate the revert succeeded,
//     CORPROF_E_RUNTIME_SUSPEND_REQUIRED to indicate the jumpstamp hasn't been reverted
//       and EE suspension will be needed for success
//     other failure HRESULT indicating what went wrong.
//
// Assumptions:
//     Caller must be holding the owning ReJitManager's table crst.
//

HRESULT ReJitInfo::UndoJumpStampNativeCode(BOOL fEESuspended)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(GetMethodDesc()->GetReJitManager()->IsTableCrstOwnedByCurrentThread());
    _ASSERTE((m_pShared->GetState() == SharedReJitInfo::kStateReverted));
    _ASSERTE((GetState() == kJumpToPrestub) || (GetState() == kJumpToRejittedCode));
    _ASSERTE(m_rgSavedCode[0] != 0); // saved code should not start with 0 (see above test)

    BYTE * pbCode = (BYTE*)GetMethodDesc()->GetNativeCode();
    DebuggerController::ControllerLockHolder lockController;

#if defined(_X86_) || defined(_AMD64_)
    _ASSERTE(m_rgSavedCode[0] != X86_INSTR_JMP_REL32);
    _ASSERTE(*FirstCodeByteAddr(pbCode, DebuggerController::GetPatchTable()->GetPatch((CORDB_ADDRESS_TYPE *)pbCode)) == X86_INSTR_JMP_REL32);
#else
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"
#endif // _X86_ || _AMD64_

    // For the interlocked compare, remember what pbCode is right now
    INT64 i64OldValue = *(INT64 *)pbCode;
    // Assemble the INT64 of the new code bytes to write.  Start with what's there now
    INT64 i64NewValue = i64OldValue;
    memcpy(LPBYTE(&i64NewValue), m_rgSavedCode, sizeof(m_rgSavedCode));
    HRESULT hr = UpdateJumpStampHelper(pbCode, i64OldValue, i64NewValue, !fEESuspended);
    _ASSERTE(hr == S_OK || (hr == CORPROF_E_RUNTIME_SUSPEND_REQUIRED && !fEESuspended));
    if (hr != S_OK)
        return hr;

    // Transition state of this ReJitInfo to indicate the MD no longer has any jump stamp
    m_dwInternalFlags &= ~kStateMask;
    m_dwInternalFlags |= kJumpNone;
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// After code has been rejitted, this is called to update the jump-stamp to go from
// pointing to the prestub, to pointing to the newly rejitted code.
//
// Arguments:
//     fEESuspended - TRUE if the caller keeps the EE suspended during this call
//     pRejittedCode - jitted code for the updated IL this method should execute
//
// Assumptions:
//      This rejit manager's table crst should be held by the caller
//
// Returns - S_OK if the jump target is updated
//           CORPROF_E_RUNTIME_SUSPEND_REQUIRED if the ee isn't suspended and it
//             will need to be in order to do the update safely
HRESULT ReJitInfo::UpdateJumpTarget(BOOL fEESuspended, PCODE pRejittedCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    MethodDesc * pMD = GetMethodDesc();
    _ASSERTE(pMD->GetReJitManager()->IsTableCrstOwnedByCurrentThread());
    _ASSERTE(m_pShared->GetState() == SharedReJitInfo::kStateActive);
    _ASSERTE(GetState() == kJumpToPrestub);
    _ASSERTE(m_pCode == NULL);

    // Beginning of originally JITted code containing the jmp that we will redirect.
    BYTE * pbCode = (BYTE*)pMD->GetNativeCode();

#if defined(_X86_) || defined(_AMD64_)

    HRESULT hr = S_OK;
    {
        DebuggerController::ControllerLockHolder lockController;

        // This will throw for out-of-memory, so don't write anything until
        // after he succeeds
        // This guy will leak/cache/reuse the jumpstub
        INT32 offset = 0;
        EX_TRY
        {
            offset = rel32UsingJumpStub(
                reinterpret_cast<INT32 UNALIGNED *>(&pbCode[1]),    // base of offset
                pRejittedCode,                                      // target of jump
                pMD,
                pMD->GetLoaderAllocator());
        }
        EX_CATCH_HRESULT(hr);
        _ASSERT(hr == S_OK || hr == E_OUTOFMEMORY);
        if (FAILED(hr))
        {
            return hr;
        }
        // For validation later, remember what pbCode is right now
        INT64 i64OldValue = *(INT64 *)pbCode;

        // Assemble the INT64 of the new code bytes to write.  Start with what's there now
        INT64 i64NewValue = i64OldValue;
        LPBYTE pbNewValue = (LPBYTE)&i64NewValue;

        // First byte becomes a rel32 jmp instruction (should be a no-op as asserted
        // above, but can't hurt)
        *pbNewValue = X86_INSTR_JMP_REL32;
        // Next 4 bytes are the jmp target (offset to jmp stub)
        INT32 UNALIGNED * pnOffset = reinterpret_cast<INT32 UNALIGNED *>(&pbNewValue[1]);
        *pnOffset = offset;

        hr = UpdateJumpStampHelper(pbCode, i64OldValue, i64NewValue, !fEESuspended);
        _ASSERTE(hr == S_OK || (hr == CORPROF_E_RUNTIME_SUSPEND_REQUIRED && !fEESuspended));
    }
    if (FAILED(hr))
    {
        return hr;
    }

#else // _X86_ || _AMD64_
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"
#endif // _X86_ || _AMD64_

    // State transition
    m_dwInternalFlags &= ~kStateMask;
    m_dwInternalFlags |= kJumpToRejittedCode;
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// This is called to modify the jump-stamp area, the first ReJitInfo::JumpStubSize bytes
// in the method's code. 
//
// Notes:
//      Callers use this method in a variety of circumstances:
//      a) when the code is unpublished (fContentionPossible == FALSE)
//      b) when the caller has taken the ThreadStoreLock and suspended the EE 
//         (fContentionPossible == FALSE)
//      c) when the code is published, the EE isn't suspended, and the jumpstamp
//         area consists of a single 5 byte long jump instruction
//         (fContentionPossible == TRUE)
//      This method will attempt to alter the jump-stamp even if the caller has not prevented
//      contention, but there is no guarantee it will be succesful. When the caller has prevented
//      contention, then success is assured. Callers may oportunistically try without
//      EE suspension, and then upgrade to EE suspension if the first attempt fails. 
//
// Assumptions:
//      This rejit manager's table crst should be held by the caller or fContentionPossible==FALSE
//      The debugger patch table lock should be held by the caller
//
// Arguments:
//      pbCode - pointer to the code where the jump stamp is placed
//      i64OldValue - the bytes which should currently be at the start of the method code
//      i64NewValue - the new bytes which should be written at the start of the method code
//      fContentionPossible - See the Notes section above.
//
// Returns:
//      S_OK => the jumpstamp has been succesfully updated.
//      CORPROF_E_RUNTIME_SUSPEND_REQUIRED => the jumpstamp remains unchanged (preventing contention will be necessary)
//      other failing HR => VirtualProtect failed, the jumpstamp remains unchanged
//
HRESULT ReJitInfo::UpdateJumpStampHelper(BYTE* pbCode, INT64 i64OldValue, INT64 i64NewValue, BOOL fContentionPossible)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc * pMD = GetMethodDesc();
    _ASSERTE(pMD->GetReJitManager()->IsTableCrstOwnedByCurrentThread() || !fContentionPossible);

    // When ReJIT is enabled, method entrypoints are always at least 8-byte aligned (see
    // code:EEJitManager::allocCode), so we can do a single 64-bit interlocked operation
    // to update the jump target.  However, some code may have gotten compiled before
    // the profiler had a chance to enable ReJIT (e.g., NGENd code, or code JITted
    // before a profiler attaches).  In such cases, we cannot rely on a simple
    // interlocked operation, and instead must suspend the runtime to ensure we can
    // safely update the jmp instruction.
    //
    // This method doesn't verify that the method is actually safe to rejit, we expect
    // callers to do that. At the moment NGEN'ed code is safe to rejit even if
    // it is unaligned, but code generated before the profiler attaches is not.
    if (fContentionPossible && !(IS_ALIGNED(pbCode, sizeof(INT64))))
    {
        return CORPROF_E_RUNTIME_SUSPEND_REQUIRED;
    }

    // The debugging API may also try to write to this function (though
    // with an int 3 for breakpoint purposes). Coordinate with the debugger so we know
    // whether we can safely patch the actual code, or instead write to the debugger's
    // buffer.
    if (fContentionPossible)
    {
        for (CORDB_ADDRESS_TYPE* pbProbeAddr = pbCode; pbProbeAddr < pbCode + ReJitInfo::JumpStubSize; pbProbeAddr++)
        {
            if (NULL != DebuggerController::GetPatchTable()->GetPatch(pbProbeAddr))
            {
                return CORPROF_E_RUNTIME_SUSPEND_REQUIRED;
            }
        }
    }

#if defined(_X86_) || defined(_AMD64_)

    DWORD oldProt;
    if (!ClrVirtualProtect((LPVOID)pbCode, 8, PAGE_EXECUTE_READWRITE, &oldProt))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (fContentionPossible)
    {
        INT64 i64InterlockReportedOldValue = FastInterlockCompareExchangeLong((INT64 *)pbCode, i64NewValue, i64OldValue);
        // Since changes to these bytes are protected by this rejitmgr's m_crstTable, we
        // shouldn't have two writers conflicting.
        _ASSERTE(i64InterlockReportedOldValue == i64OldValue);
    }
    else
    {
        // In this path the caller ensures:
        //   a) no thread will execute through the prologue area we are modifying
        //   b) no thread is stopped in a prologue such that it resumes in the middle of code we are modifying
        //   c) no thread is doing a debugger patch skip operation in which an unmodified copy of the method's 
        //      code could be executed from a patch skip buffer.

        // PERF: we might still want a faster path through here if we aren't debugging that doesn't do
        // all the patch checks
        for (int i = 0; i < ReJitInfo::JumpStubSize; i++)
        {
            *FirstCodeByteAddr(pbCode+i, DebuggerController::GetPatchTable()->GetPatch(pbCode+i)) = ((BYTE*)&i64NewValue)[i];
        }
    }
    
    if (oldProt != PAGE_EXECUTE_READWRITE)
    {
        // The CLR codebase in many locations simply ignores failures to restore the page protections
        // Its true that it isn't a problem functionally, but it seems a bit sketchy?
        // I am following the convention for now.
        ClrVirtualProtect((LPVOID)pbCode, 8, oldProt, &oldProt);
    }

    FlushInstructionCache(GetCurrentProcess(), pbCode, ReJitInfo::JumpStubSize);
    return S_OK;

#else // _X86_ || _AMD64_
#error "Need to define a way to jump-stamp the prolog in a safe way for this platform"
#endif // _X86_ || _AMD64_
}


#endif // DACCESS_COMPILE
// The rest of the ReJitInfo methods are safe to compile for DAC



//---------------------------------------------------------------------------------------
//
// ReJitInfos can be constructed in two ways:  As a "regular" ReJitInfo indexed by
// MethodDesc *, or as a "placeholder" ReJitInfo (to satisfy pre-rejit requests) indexed
// by (Module *, methodDef).  Both constructors call this helper to do all the common
// code for initializing the ReJitInfo.
//

void ReJitInfo::CommonInit()
{
    LIMITED_METHOD_CONTRACT;

    m_pCode = NULL;
    m_pNext = NULL;
    m_dwInternalFlags = kJumpNone;
    m_pShared->AddMethod(this);
    ZeroMemory(m_rgSavedCode, sizeof(m_rgSavedCode));
}


//---------------------------------------------------------------------------------------
//
// Regardless of which kind of ReJitInfo this is, this will always return its
// corresponding Module * & methodDef
//
// Arguments:
//      * ppModule - [out] Module * related to this ReJitInfo (which contains the
//          returned methodDef)
//      * pMethodDef - [out] methodDef related to this ReJitInfo
//

void ReJitInfo::GetModuleAndTokenRegardlessOfKeyType(Module ** ppModule, mdMethodDef * pMethodDef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_NOT_MAINLINE;
    }
    CONTRACTL_END;

    _ASSERTE(ppModule != NULL);
    _ASSERTE(pMethodDef != NULL);

    if (m_key.m_keyType == Key::kMetadataToken)
    {
        GetModuleAndToken(ppModule, pMethodDef);
    }
    else
    {
        MethodDesc * pMD = GetMethodDesc();
        _ASSERTE(pMD != NULL);
        _ASSERTE(pMD->IsRestored());

        *ppModule = pMD->GetModule();
        *pMethodDef = pMD->GetMemberDef();
    }

    _ASSERTE(*ppModule != NULL);
    _ASSERTE(*pMethodDef != mdTokenNil);
}


//---------------------------------------------------------------------------------------
//
// Used as part of the hash table implementation in the containing ReJitManager, this
// hashes a ReJitInfo by MethodDesc * when available, else by (Module *, methodDef)
// 
// Arguments:
//     key - Key representing the ReJitInfo to hash
//
// Return Value:
//     Hash value of the ReJitInfo represented by the specified key
//

// static
COUNT_T ReJitInfo::Hash(Key key)
{
    LIMITED_METHOD_CONTRACT;

    if (key.m_keyType == Key::kMethodDesc)
    {
        return HashPtr(0, PTR_MethodDesc(key.m_pMD));
    }

    _ASSERTE (key.m_keyType == Key::kMetadataToken);

    return HashPtr(key.m_methodDef, PTR_Module(key.m_pModule));
}


//---------------------------------------------------------------------------------------
//
// Return the IL to compile for a given ReJitInfo
//
// Return Value:
//      Pointer to IL buffer to compile.  If the profiler has specified IL to rejit,
//      this will be our copy of the IL buffer specified by the profiler.  Else, this
//      points to the original IL for the method from its module's metadata.
//
// Notes:
//     IL memory is managed by us, not the caller.  Caller must not free the buffer.
//

COR_ILMETHOD * ReJitInfo::GetIL()
{
    CONTRACTL
    {
        THROWS;             // Getting original IL via PEFile::GetIL can throw
        CAN_TAKE_LOCK;      // Looking up dynamically overridden IL takes a lock
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pShared->m_pbIL != NULL)
    {
        return reinterpret_cast<COR_ILMETHOD *>(m_pShared->m_pbIL);
    }

    // If the user hasn't overriden us, get whatever the original IL had
    return GetMethodDesc()->GetILHeader(TRUE);
}


//---------------------------------------------------------------------------------------
// SharedReJitInfo implementation


SharedReJitInfo::SharedReJitInfo()
    : m_dwInternalFlags(kStateRequested),
    m_pbIL(NULL),
    m_dwCodegenFlags(0),
    m_reJitId(InterlockedIncrement(reinterpret_cast<LONG*>(&s_GlobalReJitId))),
    m_pInfoList(NULL)
{
    LIMITED_METHOD_CONTRACT;
}


//---------------------------------------------------------------------------------------
//
// Link in the specified ReJitInfo to the list maintained by this SharedReJitInfo
//
// Arguments:
//      pInfo - ReJitInfo being added
//

void SharedReJitInfo::AddMethod(ReJitInfo * pInfo)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pInfo->m_pShared == this);

    // Push it on the head of our list
    _ASSERTE(pInfo->m_pNext == NULL);
    pInfo->m_pNext = PTR_ReJitInfo(m_pInfoList);
    m_pInfoList = pInfo;
}


//---------------------------------------------------------------------------------------
//
// Unlink the specified ReJitInfo from the list maintained by this SharedReJitInfo. 
// Currently this is only used on AD unload to remove ReJitInfos of non-domain-neutral instantiations
// of domain-neutral generics (which are tracked in the SharedDomain's ReJitManager). 
// This may be used in the future once we implement memory reclamation on revert().
//
// Arguments:
//      pInfo - ReJitInfo being removed
//

void SharedReJitInfo::RemoveMethod(ReJitInfo * pInfo)
{
    LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE

    // Find it
    ReJitInfo ** ppEntry = &m_pInfoList;
    while (*ppEntry != pInfo)
    {
        ppEntry = &(*ppEntry)->m_pNext;
        _ASSERTE(*ppEntry != NULL);
    }

    // Remove it
    _ASSERTE((*ppEntry)->m_pShared == this);
    *ppEntry = (*ppEntry)->m_pNext;

#endif // DACCESS_COMPILE
}

//---------------------------------------------------------------------------------------
//
// MethodDesc::MakeJitWorker() calls this to determine if there's an outstanding
// "pre-rejit" request for a MethodDesc that has just been jitted for the first time.
// This is also called when methods are being restored in NGEN images. The sequence looks like:
// *Enter holder
//   Enter Rejit table lock
//   DoJumpStampIfNecessary
// *Runtime code publishes/restores method
// *Exit holder
//   Leave rejit table lock
//   Send rejit error callbacks if needed
// 
// This also has a non-locking early-out if ReJIT is not enabled.
//
// #PublishCode:
// Note that the runtime needs to publish/restore the PCODE while this holder is
// on the stack, so it can happen under the ReJitManager's lock.
// This prevents a "lost pre-rejit" race with a profiler that calls
// RequestReJIT just as the method finishes compiling. In particular, the locking ensures
// atomicity between this set of steps (performed in DoJumpStampIfNecessary):
//     * (1) Checking whether there is a pre-rejit request for this MD
//     * (2) If not, skip doing the pre-rejit-jmp-stamp
//     * (3) Publishing the PCODE
//     
// with respect to these steps performed in RequestReJIT:
//     * (a) Is PCODE published yet?
//     * (b) If not, create pre-rejit (placeholder) ReJitInfo which the prestub will
//         consult when it JITs the original IL
//         
// Without this atomicity, we could get the ordering (1), (2), (a), (b), (3), resulting
// in the rejit request getting completely ignored (i.e., we file away the pre-rejit
// placeholder AFTER the prestub checks for it).
//
// A similar race is possible for code being restored. In that case the restoring thread
// does:
//      * (1) Check if there is a pre-rejit request for this MD
//      * (2) If not, no need to jmp-stamp
//      * (3) Restore the MD

// And RequestRejit does:
//      * (a) [In LoadedMethodDescIterator] Is a potential MD restored yet?
//      * (b) [In MarkInstantiationsForReJit] If not, don't queue it for jump-stamping
//
// Same ordering (1), (2), (a), (b), (3) results in missing both opportunities to jump
// stamp.

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
ReJitPublishMethodHolder::ReJitPublishMethodHolder(MethodDesc* pMethodDesc, PCODE pCode) :
m_pMD(NULL), m_hr(S_OK)
{
    // This method can't have a contract because entering the table lock
    // below increments GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the incremented count to flow out of the
    // method. The balancing decrement occurs in the destructor.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    // We come here from the PreStub and from MethodDesc::CheckRestore
    // The method should be effectively restored, but we haven't yet
    // cleared the unrestored bit so we can't assert pMethodDesc->IsRestored()
    // We can assert:
    _ASSERTE(pMethodDesc->GetMethodTable()->IsRestored());

    if (ReJitManager::IsReJITEnabled() && (pCode != NULL))
    {
        m_pMD = pMethodDesc;
        ReJitManager* pReJitManager = pMethodDesc->GetReJitManager();
        pReJitManager->m_crstTable.Enter();
        m_hr = pReJitManager->DoJumpStampIfNecessary(pMethodDesc, pCode);
    }
}


ReJitPublishMethodHolder::~ReJitPublishMethodHolder()
{
    // This method can't have a contract because leaving the table lock
    // below decrements GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the decremented count to flow out of the
    // method. The balancing increment occurred in the constructor.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS; // NOTRIGGER until we leave the lock
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    if (m_pMD)
    {
        ReJitManager* pReJitManager = m_pMD->GetReJitManager();
        pReJitManager->m_crstTable.Leave();
        if (FAILED(m_hr))
        {
            ReJitManager::ReportReJITError(m_pMD->GetModule(), m_pMD->GetMemberDef(), m_pMD, m_hr);
        }
    }
}

ReJitPublishMethodTableHolder::ReJitPublishMethodTableHolder(MethodTable* pMethodTable) :
m_pMethodTable(NULL)
{
    // This method can't have a contract because entering the table lock
    // below increments GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the incremented count to flow out of the
    // method. The balancing decrement occurs in the destructor.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    // We come here from MethodTable::SetIsRestored
    // The method table should be effectively restored, but we haven't yet
    // cleared the unrestored bit so we can't assert pMethodTable->IsRestored()

    if (ReJitManager::IsReJITEnabled())
    {
        m_pMethodTable = pMethodTable;
        ReJitManager* pReJitManager = pMethodTable->GetModule()->GetReJitManager();
        pReJitManager->m_crstTable.Enter();
        MethodTable::IntroducedMethodIterator itMethods(pMethodTable, FALSE);
        for (; itMethods.IsValid(); itMethods.Next())
        {
            // Although the MethodTable is restored, the methods might not be.
            // We need to be careful to only query portions of the MethodDesc
            // that work in a partially restored state. The only methods that need
            // further restoration are IL stubs (which aren't rejittable) and
            // generic methods. The only generic methods directly accesible from
            // the MethodTable are definitions. GetNativeCode() on generic defs
            // will run succesfully and return NULL which short circuits the
            // rest of the logic.
            MethodDesc * pMD = itMethods.GetMethodDesc();
            PCODE pCode = pMD->GetNativeCode();
            if (pCode != NULL)
            {
                HRESULT hr = pReJitManager->DoJumpStampIfNecessary(pMD, pCode);
                if (FAILED(hr))
                {
                    ReJitManager::AddReJITError(pMD->GetModule(), pMD->GetMemberDef(), pMD, hr, &m_errors);
                }
            }
        }
    }
}


ReJitPublishMethodTableHolder::~ReJitPublishMethodTableHolder()
{
    // This method can't have a contract because leaving the table lock
    // below decrements GCNoTrigger count. Contracts always revert these changes
    // at the end of the method but we need the decremented count to flow out of the
    // method. The balancing increment occurred in the constructor.
    STATIC_CONTRACT_NOTHROW; 
    STATIC_CONTRACT_GC_TRIGGERS; // NOTRIGGER until we leave the lock
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    STATIC_CONTRACT_MODE_ANY;

    if (m_pMethodTable)
    {
        ReJitManager* pReJitManager = m_pMethodTable->GetModule()->GetReJitManager();
        pReJitManager->m_crstTable.Leave();
        for (int i = 0; i < m_errors.Count(); i++)
        {
            ReJitManager::ReportReJITError(&(m_errors[i]));
        }
    }
}
#endif // !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

#else // FEATURE_REJIT

// On architectures that don't support rejit, just keep around some do-nothing
// stubs so the rest of the VM doesn't have to be littered with #ifdef FEATURE_REJIT

// static
HRESULT ReJitManager::RequestReJIT(
    ULONG       cFunctions,
    ModuleID    rgModuleIDs[],
    mdMethodDef rgMethodDefs[])
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

// static
void ReJitManager::OnAppDomainExit(AppDomain * pAppDomain)
{
}

ReJitManager::ReJitManager()
{
}

void ReJitManager::PreInit(BOOL fSharedDomain)
{
}

ReJITID ReJitManager::GetReJitId(PTR_MethodDesc pMD, PCODE pCodeStart)
{
    return 0;
}

ReJITID ReJitManager::GetReJitIdNoLock(PTR_MethodDesc pMD, PCODE pCodeStart)
{
    return 0;
}

PCODE ReJitManager::GetCodeStart(PTR_MethodDesc pMD, ReJITID reJitId)
{
    return NULL;
}

HRESULT ReJitManager::GetReJITIDs(PTR_MethodDesc pMD, ULONG cReJitIds, ULONG * pcReJitIds, ReJITID reJitIds[])
{
    return E_NOTIMPL;
}

#endif // FEATURE_REJIT
