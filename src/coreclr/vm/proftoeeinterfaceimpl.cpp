// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: ProfToEEInterfaceImpl.cpp
//
// This module implements the ICorProfilerInfo* interfaces, which allow the
// Profiler to communicate with the EE.  This allows the Profiler DLL to get
// access to private EE data structures and other things that should never be
// exported outside of the EE.
//

//
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE!
//
// PLEASE READ!
//
// There are strict rules for how to implement ICorProfilerInfo* methods.  Please read
// https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/profilability.md
// to understand the rules and why they exist.
//
// As a reminder, here is a short summary of your responsibilities.  Every PUBLIC
// ENTRYPOINT (from profiler to EE) must have:
//
// - An entrypoint macro at the top (see code:#P2CLRRestrictionsOverview).  Your choices are:
//       PROFILER_TO_CLR_ENTRYPOINT_SYNC (typical choice):
//          Indicates the method may only be called by the profiler from within
//          a callback (from EE to profiler).
//       PROFILER_TO_CLR_ENTRYPOINT_CALLABLE_ON_INIT_ONLY
//          Even more restrictive, this indicates the method may only be called
//          from within the Initialize() callback
//       PROFILER_TO_CLR_ENTRYPOINT_ASYNC
//          Indicates this method may be called anytime.
//          THIS IS DANGEROUS.  PLEASE READ ABOVE DOC FOR GUIDANCE ON HOW TO SAFELY
//          CODE AN ASYNCHRONOUS METHOD.
//   You may use variants of these macros ending in _EX that accept bit flags (see
//   code:ProfToClrEntrypointFlags) if you need to specify additional parameters to how
//   the entrypoint should behave, though typically you can omit the flags and the
//   default (kP2EENone) will be used.
//
// - A complete contract block with comments over every contract choice.  Wherever
//   possible, use the preferred contracts (if not possible, you must comment why):
//       NOTHROW
//       GC_NOTRIGGER
//       MODE_ANY
//       CANNOT_TAKE_LOCK
//       (EE_THREAD_(NOT)_REQUIRED are unenforced and are thus optional.  If you wish
//       to specify these, EE_THREAD_NOT_REQUIRED is preferred.)
//   Note that the preferred contracts in this file are DIFFERENT than the preferred
//   contracts for eetoprofinterfaceimpl.cpp.
//
// Private helper functions in this file do not have the same preferred contracts as
// public entrypoints, and they should be contracted following the same guidelines
// as per the rest of the EE.
//
// NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE! NOTE!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//
//
// #P2CLRRestrictionsOverview
//
// The public ICorProfilerInfo(N) functions below have different restrictions on when
// they're allowed to be called. Listed roughly in order from most to least restrictive:
//     * PROFILER_TO_CLR_ENTRYPOINT_CALLABLE_ON_INIT_ONLY: Functions that are only
//         allowed to be called while the profiler is initializing on startup, from
//         inside the profiler's ICorProfilerCallback::Initialize method
//     * PROFILER_TO_CLR_ENTRYPOINT_SYNC: Functions that may be called from within any of
//         the profiler's callbacks, or anytime from a thread created by the profiler.
//         These functions may only be called by profilers loaded on startup
//     * PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach): Same as above,
//         except these may be called by startup AND attaching profilers.
//     * PROFILER_TO_CLR_ENTRYPOINT_ASYNC: Functions that may be called at any time and
//         from any thread by a profiler loaded on startup
//     * PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach): Same as above,
//         except these may be called by startup AND attaching profilers.
//
//  The above restrictions are lifted for certain tests that run with these environment
//  variables set. (These are only available on DEBUG builds--including chk--not retail
//  builds.)
//    * COMPlus_TestOnlyEnableSlowELTHooks:
//         * If nonzero, then on startup the runtime will act as if a profiler was loaded
//             on startup and requested ELT slow-path (even if no profiler is loaded on
//             startup). This will also allow the SetEnterLeaveFunctionHooks(2) info
//             functions to be called outside of Initialize(). If a profiler later
//             attaches and calls these functions, then the slow-path wrapper will call
//             into the profiler's ELT hooks.
//    * COMPlus_TestOnlyEnableObjectAllocatedHook:
//         * If nonzero, then on startup the runtime will act as if a profiler was loaded
//             on startup and requested ObjectAllocated callback (even if no profiler is loaded
//             on startup). If a profiler later attaches and calls these functions, then the
//             ObjectAllocated notifications will call into the profiler's ObjectAllocated callback.
//    * COMPlus_TestOnlyEnableICorProfilerInfo:
//         * If nonzero, then attaching profilers allows to call ICorProfilerInfo inteface,
//             which would otherwise be disallowed for attaching profilers
//    * COMPlus_TestOnlyAllowedEventMask
//         * If a profiler needs to work around the restrictions of either
//             COR_PRF_ALLOWABLE_AFTER_ATTACH or COR_PRF_MONITOR_IMMUTABLE it may set
//             this environment variable. Its value should be a bitmask containing all
//             the flags that are:
//             * normally immutable or disallowed after attach, AND
//             * that the test plans to set after startup and / or by an attaching
//                 profiler.
//
//

//
// ======================================================================================

#include "common.h"
#include <posterror.h>
#include "proftoeeinterfaceimpl.h"
#include "proftoeeinterfaceimpl.inl"
#include "dllimport.h"
#include "threads.h"
#include "method.hpp"
#include "vars.hpp"
#include "dbginterface.h"
#include "corprof.h"
#include "class.h"
#include "object.h"
#include "ceegen.h"
#include "eeconfig.h"
#include "generics.h"
#include "gcinfo.h"
#include "safemath.h"
#include "threadsuspend.h"
#include "inlinetracking.h"

#ifdef PROFILING_SUPPORTED
#include "profilinghelper.h"
#include "profilinghelper.inl"
#include "eetoprofinterfaceimpl.inl"
#include "profilingenumerators.h"
#endif

#include "profdetach.h"

#include "metadataexports.h"

#ifdef FEATURE_PERFTRACING
#include "eventpipeadapter.h"
#endif // FEATURE_PERFTRACING

//---------------------------------------------------------------------------------------
// Helpers

// An OR'd combination of these flags may be specified in the _EX entrypoint macros to
// customize the behavior.
enum ProfToClrEntrypointFlags
{
    // Just use the default behavior (this one is used if the non-_EX entrypoint macro is
    // specified without any flags).
    kP2EENone                       = 0x00000000,

    // By default, Info functions are not allowed to be used by an attaching profiler.
    // Specify this flag to override the default.
    kP2EEAllowableAfterAttach       = 0x00000001,

    // This info method has a GC_TRIGGERS contract.  Whereas contracts are debug-only,
    // this flag is used in retail builds as well.
    kP2EETriggers                   = 0x00000002,
};

// Default versions of the entrypoint macros use kP2EENone if no
// ProfToClrEntrypointFlags are specified

#define PROFILER_TO_CLR_ENTRYPOINT_ASYNC(logParams)             \
    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EENone, logParams)

#define PROFILER_TO_CLR_ENTRYPOINT_SYNC(logParams)              \
    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EENone, logParams)

// ASYNC entrypoints log and ensure an attaching profiler isn't making a call that's
// only supported by startup profilers.

#define CHECK_IF_ATTACHING_PROFILER_IS_ALLOWED_HELPER(p2eeFlags)                           \
    do                                                                                     \
    {                                                                                      \
        if ((((p2eeFlags) & kP2EEAllowableAfterAttach) == 0) &&                            \
            (m_pProfilerInfo->pProfInterface->IsLoadedViaAttach()))                      \
        {                                                                                  \
            LOG((LF_CORPROF,                                                               \
                 LL_ERROR,                                                                 \
                 "**PROF: ERROR: Returning CORPROF_E_UNSUPPORTED_FOR_ATTACHING_PROFILER "  \
                 "due to a call illegally made by an attaching profiler \n"));             \
            return CORPROF_E_UNSUPPORTED_FOR_ATTACHING_PROFILER;                           \
        }                                                                                  \
    } while(0)

#ifdef _DEBUG

#define CHECK_IF_ATTACHING_PROFILER_IS_ALLOWED(p2eeFlags)                           \
    do                                                                              \
    {                                                                               \
        if (!((&g_profControlBlock)->fTestOnlyEnableICorProfilerInfo))              \
        {                                                                           \
            CHECK_IF_ATTACHING_PROFILER_IS_ALLOWED_HELPER(p2eeFlags);               \
        }                                                                           \
    } while(0)



#else  //_DEBUG

#define CHECK_IF_ATTACHING_PROFILER_IS_ALLOWED(p2eeFlags)                           \
    do                                                                              \
    {                                                                               \
        CHECK_IF_ATTACHING_PROFILER_IS_ALLOWED_HELPER(p2eeFlags);                   \
    } while(0)

#endif //_DEBUG

#define PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(p2eeFlags, logParams)                           \
    do                                                                                      \
    {                                                                                       \
        INCONTRACT(AssertTriggersContract(((p2eeFlags) & kP2EETriggers)));                  \
        _ASSERTE(m_pProfilerInfo->curProfStatus.Get() != kProfStatusNone);                \
        LOG(logParams);                                                                     \
        /* If profiler was neutered, disallow call */                                       \
        if (m_pProfilerInfo->curProfStatus.Get() == kProfStatusDetaching)                 \
        {                                                                                   \
            LOG((LF_CORPROF,                                                                \
                 LL_ERROR,                                                                  \
                 "**PROF: ERROR: Returning CORPROF_E_PROFILER_DETACHING "                   \
                 "due to a post-neutered profiler call\n"));                                \
            return CORPROF_E_PROFILER_DETACHING;                                            \
        }                                                                                   \
        CHECK_IF_ATTACHING_PROFILER_IS_ALLOWED(p2eeFlags);                                  \
    } while(0)

// SYNC entrypoints must ensure the current EE Thread shows evidence that we're
// inside a callback.  If there's no EE Thread, then we automatically "pass"
// the check, and the SYNC call is allowed.
#define PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(p2eeFlags, logParams)                    \
    do                                                                              \
    {                                                                               \
        PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(p2eeFlags, logParams);                  \
        DWORD __dwExpectedCallbackState = COR_PRF_CALLBACKSTATE_INCALLBACK;         \
        if (((p2eeFlags) & kP2EETriggers) != 0)                                     \
        {                                                                           \
            __dwExpectedCallbackState |= COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE;   \
        }                                                                           \
        if (!AreCallbackStateFlagsSet(__dwExpectedCallbackState))                   \
        {                                                                           \
            LOG((LF_CORPROF,                                                        \
                 LL_ERROR,                                                          \
                 "**PROF: ERROR: Returning CORPROF_E_UNSUPPORTED_CALL_SEQUENCE "    \
                 "due to illegal asynchronous profiler call\n"));                   \
            return CORPROF_E_UNSUPPORTED_CALL_SEQUENCE;                             \
        }                                                                           \
    } while(0)

// INIT_ONLY entrypoints must ensure we're executing inside the profiler's
// Initialize() implementation on startup (attach init doesn't count!).
#define PROFILER_TO_CLR_ENTRYPOINT_CALLABLE_ON_INIT_ONLY(logParams)                             \
    do                                                                                          \
    {                                                                                           \
        PROFILER_TO_CLR_ENTRYPOINT_ASYNC(logParams);                                            \
        if (m_pProfilerInfo->curProfStatus.Get() != kProfStatusInitializingForStartupLoad &&  \
            m_pProfilerInfo->curProfStatus.Get() != kProfStatusInitializingForAttachLoad)     \
        {                                                                                       \
            return CORPROF_E_CALL_ONLY_FROM_INIT;                                               \
        }                                                                                       \
    } while(0)

// This macro is used to ensure that the current thread is not in a forbid
// suspend region.   Some methods are allowed to be called asynchronously,
// but some of them call JIT functions that take a reader lock.  So we need to ensure
// the current thread hasn't been hijacked by a profiler while it was holding the writer lock.
// Checking the ForbidSuspendThread region is a sufficient test for this
#define FAIL_IF_IN_FORBID_SUSPEND_REGION()                                  \
    do                                                                      \
    {                                                                       \
        Thread * __pThread = GetThreadNULLOk();                             \
        if ((__pThread != NULL) && (__pThread->IsInForbidSuspendRegion()))  \
        {                                                                   \
        return CORPROF_E_ASYNCHRONOUS_UNSAFE;                               \
        }                                                                   \
    } while(0)

//
// This type is an overlay onto the exported type COR_PRF_FRAME_INFO.
// The first four fields *must* line up with the same fields in the
// exported type.  After that, we can add to the end as we wish.
//
typedef struct _COR_PRF_FRAME_INFO_INTERNAL {
    USHORT size;
    USHORT version;
    FunctionID funcID;
    UINT_PTR IP;
    void *extraArg;
    LPVOID thisArg;
} COR_PRF_FRAME_INFO_INTERNAL, *PCOR_PRF_FRAME_INFO_INTERNAL;

//
// After we ship a product with a certain struct type for COR_PRF_FRAME_INFO_INTERNAL
// we have that as a version.  If we change that in a later product, we can increment
// the counter below and then we can properly do versioning.
//
#define COR_PRF_FRAME_INFO_INTERNAL_CURRENT_VERSION 1


//---------------------------------------------------------------------------------------
//
// Converts TypeHandle to a ClassID
//
// Arguments:
//      th - TypeHandle to convert
//
// Return Value:
//      Requested ClassID.
//

ClassID TypeHandleToClassID(TypeHandle th)
{
    WRAPPER_NO_CONTRACT;
    return reinterpret_cast<ClassID> (th.AsPtr());
}

//---------------------------------------------------------------------------------------
//
// Converts TypeHandle for a non-generic type to a ClassID
//
// Arguments:
//      th - TypeHandle to convert
//
// Return Value:
//      Requested ClassID.  NULL if th represents a generic type
//
#ifdef PROFILING_SUPPORTED

static ClassID NonGenericTypeHandleToClassID(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    if ((!th.IsNull()) && (th.HasInstantiation()))
{
        return NULL;
}

    return TypeHandleToClassID(th);
}

//---------------------------------------------------------------------------------------
//
// Converts MethodDesc * to FunctionID
//
// Arguments:
//      pMD - MethodDesc * to convert
//
// Return Value:
//      Requested FunctionID
//

static FunctionID MethodDescToFunctionID(MethodDesc * pMD)
{
    LIMITED_METHOD_CONTRACT;
    return reinterpret_cast< FunctionID > (pMD);
}

#endif

//---------------------------------------------------------------------------------------
//
// Converts FunctionID to MethodDesc *
//
// Arguments:
//      functionID - FunctionID to convert
//
// Return Value:
//      MethodDesc * requested
//

MethodDesc *FunctionIdToMethodDesc(FunctionID functionID)
{
    LIMITED_METHOD_CONTRACT;

    MethodDesc *pMethodDesc;

    pMethodDesc = reinterpret_cast< MethodDesc* >(functionID);

    _ASSERTE(pMethodDesc != NULL);
    return pMethodDesc;
}

#ifdef PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
// ModuleILHeap IUnknown implementation
//
// Function headers unnecessary, as MSDN adequately documents IUnknown
//

ULONG ModuleILHeap::AddRef()
{
    // Lifetime of this object is controlled entirely by the CLR.  This
    // is created on first request, and is automatically destroyed when
    // the profiler is detached.
    return 1;
}


ULONG ModuleILHeap::Release()
{
    // Lifetime of this object is controlled entirely by the CLR.  This
    // is created on first request, and is automatically destroyed when
    // the profiler is detached.
    return 1;
}


HRESULT ModuleILHeap::QueryInterface(REFIID riid, void ** pp)
{
    HRESULT     hr = S_OK;

    if (pp == NULL)
    {
        return E_POINTER;
    }

    *pp = 0;
    if (riid == IID_IUnknown)
    {
        *pp = static_cast<IUnknown *>(this);
    }
    else if (riid == IID_IMethodMalloc)
    {
        *pp = static_cast<IMethodMalloc *>(this);
    }
    else
    {
        hr = E_NOINTERFACE;
    }

    if (hr == S_OK)
    {
        // CLR manages lifetime of this object, but in case that changes (or
        // this code gets copied/pasted elsewhere), we'll still AddRef here so
        // QI remains a good citizen either way.
        AddRef();
    }
    return hr;
}

//---------------------------------------------------------------------------------------
// Profiler entrypoint to allocate space from this module's heap.
//
// Arguments
//      cb - size in bytes of allocation request
//
// Return value
//      pointer to allocated memory, or NULL if there was an error

void * STDMETHODCALLTYPE ModuleILHeap::Alloc(ULONG cb)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // (see GC_TRIGGERS comment below)
        CAN_TAKE_LOCK;

        // Allocations using loader heaps below enter a critsec, which switches
        // to preemptive, which is effectively a GC trigger
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

    }
    CONTRACTL_END;

    LOG((LF_CORPROF, LL_INFO1000, "**PROF: ModuleILHeap::Alloc 0x%08xp.\n", cb));

    if (cb == 0)
    {
        return NULL;
    }

    return new (nothrow) BYTE[cb];
}

//---------------------------------------------------------------------------------------
// The one and only instance of the IL heap

ModuleILHeap ModuleILHeap::s_Heap;

//---------------------------------------------------------------------------------------
// Implementation of ProfToEEInterfaceImpl's IUnknown

//
// The VM controls the lifetime of ProfToEEInterfaceImpl, not the
// profiler.  We'll automatically take care of cleanup when profilers
// unload and detach.
//

ULONG STDMETHODCALLTYPE ProfToEEInterfaceImpl::AddRef()
    {
    LIMITED_METHOD_CONTRACT;
    return 1;
}

ULONG STDMETHODCALLTYPE ProfToEEInterfaceImpl::Release()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

COM_METHOD ProfToEEInterfaceImpl::QueryInterface(REFIID id, void ** pInterface)
{
    if (pInterface == NULL)
    {
        return E_POINTER;
    }

    if (id == IID_ICorProfilerInfo)
    {
        *pInterface = static_cast<ICorProfilerInfo *>(this);
    }
    else if (id == IID_ICorProfilerInfo2)
    {
        *pInterface = static_cast<ICorProfilerInfo2 *>(this);
    }
    else if (id == IID_ICorProfilerInfo3)
    {
        *pInterface = static_cast<ICorProfilerInfo3 *>(this);
    }
    else if (id == IID_ICorProfilerInfo4)
    {
        *pInterface = static_cast<ICorProfilerInfo4 *>(this);
    }
    else if (id == IID_ICorProfilerInfo5)
    {
        *pInterface = static_cast<ICorProfilerInfo5 *>(this);
    }
    else if (id == IID_ICorProfilerInfo6)
    {
        *pInterface = static_cast<ICorProfilerInfo6 *>(this);
    }
    else if (id == IID_ICorProfilerInfo7)
    {
        *pInterface = static_cast<ICorProfilerInfo7 *>(this);
    }
    else if (id == IID_ICorProfilerInfo8)
    {
        *pInterface = static_cast<ICorProfilerInfo8 *>(this);
    }
    else if (id == IID_ICorProfilerInfo9)
    {
        *pInterface = static_cast<ICorProfilerInfo9 *>(this);
    }
    else if (id == IID_ICorProfilerInfo10)
    {
        *pInterface = static_cast<ICorProfilerInfo10 *>(this);
    }
    else if (id == IID_ICorProfilerInfo11)
    {
        *pInterface = static_cast<ICorProfilerInfo11 *>(this);
    }
    else if (id == IID_ICorProfilerInfo12)
    {
        *pInterface = static_cast<ICorProfilerInfo12 *>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorProfilerInfo *>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    // CLR manages lifetime of this object, but in case that changes (or
    // this code gets copied/pasted elsewhere), we'll still AddRef here so
    // QI remains a good citizen either way.
    AddRef();

    return S_OK;
}
#endif // PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
//
// GC-related helpers.  These are called from elsewhere in the EE to determine profiler
// state, and to update the profiling API with info from the GC.
//

//---------------------------------------------------------------------------------------
//
// ProfilerObjectAllocatedCallback is called if a profiler is attached, requesting
// ObjectAllocated callbacks.
//
// Arguments:
//      objref - Reference to newly-allocated object
//      classId - ClassID of newly-allocated object
//

void __stdcall ProfilerObjectAllocatedCallback(OBJECTREF objref, ClassID classId)
{
    CONTRACTL
{
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    TypeHandle th = OBJECTREFToObject(objref)->GetTypeHandle();

    // WARNING: objref can move as a result of the ObjectAllocated() call below if
    // the profiler causes a GC, so any operations on the objref should occur above
    // this comment (unless you're prepared to add a GCPROTECT around the objref).

#ifdef PROFILING_SUPPORTED
    // Notify the profiler of the allocation

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAllocations() || CORProfilerTrackLargeAllocations());
        // Note that for generic code we always return uninstantiated ClassIDs and FunctionIDs.
        // Thus we strip any instantiations of the ClassID (which is really a type handle) here.
        g_profControlBlock.ObjectAllocated(
                (ObjectID) OBJECTREFToObject(objref),
                classId);
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED
}

//---------------------------------------------------------------------------------------
//
// Wrapper around the GC Started callback
//
// Arguments:
//      generation - Generation being collected
//      induced - Was this GC induced by GC.Collect?
//

void __stdcall GarbageCollectionStartedCallback(int generation, BOOL induced)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY; // can be called even on GC threads
    }
    CONTRACTL_END;

#ifdef PROFILING_SUPPORTED
    //
    // Mark that we are starting a GC.  This will allow profilers to do limited object inspection
    // during callbacks that occur while a GC is happening.
    //
    g_profControlBlock.fGCInProgress = TRUE;

    // Notify the profiler of start of the collection
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC() || CORProfilerTrackBasicGC());
        BOOL generationCollected[COR_PRF_GC_PINNED_OBJECT_HEAP+1];
        if (generation == COR_PRF_GC_GEN_2)
            generation = COR_PRF_GC_PINNED_OBJECT_HEAP;
        for (int gen = 0; gen <= COR_PRF_GC_PINNED_OBJECT_HEAP; gen++)
            generationCollected[gen] = gen <= generation;

        g_profControlBlock.GarbageCollectionStarted(
            COR_PRF_GC_PINNED_OBJECT_HEAP+1,
            generationCollected,
            induced ? COR_PRF_GC_INDUCED : COR_PRF_GC_OTHER);
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED
}

//---------------------------------------------------------------------------------------
//
// Wrapper around the GC Finished callback
//

void __stdcall GarbageCollectionFinishedCallback()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY; // can be called even on GC threads
    }
    CONTRACTL_END;

#ifdef PROFILING_SUPPORTED
    // Notify the profiler of end of the collection
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC() || CORProfilerTrackBasicGC());
        g_profControlBlock.GarbageCollectionFinished();
        END_PROFILER_CALLBACK();
    }

    // Mark that GC is finished.
    g_profControlBlock.fGCInProgress = FALSE;
#endif // PROFILING_SUPPORTED
}

#ifdef PROFILING_SUPPORTED
//---------------------------------------------------------------------------------------
//
// Describes a GC generation by number and address range
//

struct GenerationDesc
{
    int generation;
    BYTE *rangeStart;
    BYTE *rangeEnd;
    BYTE *rangeEndReserved;
};

struct GenerationTable
{
    ULONG count;
    ULONG capacity;
    static const ULONG defaultCapacity = 5; // that's the minimum for Gen0-2 + LOH + POH
    GenerationTable *prev;
    GenerationDesc *genDescTable;
#ifdef  _DEBUG
    ULONG magic;
#define GENERATION_TABLE_MAGIC 0x34781256
#define GENERATION_TABLE_BAD_MAGIC 0x55aa55aa
#endif
};


//---------------------------------------------------------------------------------------
//
// This is a callback used by the GC when we call GCHeapUtilities::DiagDescrGenerations
// (from UpdateGenerationBounds() below).  The GC gives us generation information through
// this callback, which we use to update the GenerationDesc in the corresponding
// GenerationTable
//
// Arguments:
//      context - The containing GenerationTable
//      generation - Generation number
//      rangeStart - Address where generation starts
//      rangeEnd - Address where generation ends
//      rangeEndReserved - Address where generation reserved space ends
//

// static
static void GenWalkFunc(void * context,
                        int generation,
                        BYTE * rangeStart,
                        BYTE * rangeEnd,
                        BYTE * rangeEndReserved)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY; // can be called even on GC threads
        PRECONDITION(CheckPointer(context));
        PRECONDITION(0 <= generation && generation <= 4);
        PRECONDITION(CheckPointer(rangeStart));
        PRECONDITION(CheckPointer(rangeEnd));
        PRECONDITION(CheckPointer(rangeEndReserved));
    } CONTRACT_END;

    GenerationTable *generationTable = (GenerationTable *)context;

    _ASSERTE(generationTable->magic == GENERATION_TABLE_MAGIC);

    ULONG count = generationTable->count;
    if (count >= generationTable->capacity)
    {
        ULONG newCapacity = generationTable->capacity == 0 ? GenerationTable::defaultCapacity : generationTable->capacity * 2;
        GenerationDesc *newGenDescTable = new (nothrow) GenerationDesc[newCapacity];
        if (newGenDescTable == NULL)
        {
            // if we can't allocate a bigger table, we'll have to ignore this call
            RETURN;
        }
        memcpy(newGenDescTable, generationTable->genDescTable, sizeof(generationTable->genDescTable[0]) * generationTable->count);
        delete[] generationTable->genDescTable;
        generationTable->genDescTable = newGenDescTable;
        generationTable->capacity = newCapacity;
    }
    _ASSERTE(count < generationTable->capacity);

    GenerationDesc *genDescTable = generationTable->genDescTable;

    genDescTable[count].generation = generation;
    genDescTable[count].rangeStart = rangeStart;
    genDescTable[count].rangeEnd = rangeEnd;
    genDescTable[count].rangeEndReserved = rangeEndReserved;

    generationTable->count = count + 1;
}

// This is the table of generation bounds updated by the gc
// and read by the profiler. So this is a single writer,
// multiple readers scenario.
static GenerationTable *s_currentGenerationTable;

// The generation table is updated atomically by replacing the
// pointer to it. The only tricky part is knowing when
// the old table can be deleted.
static Volatile<LONG> s_generationTableLock;

// This is just so we can assert there's a single writer
#ifdef  ENABLE_CONTRACTS
static Volatile<LONG> s_generationTableWriterCount;
#endif
#endif // PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
//
// This is called from the gc to push a new set of generation bounds
//

void __stdcall UpdateGenerationBounds()
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY; // can be called even on GC threads
#ifdef PROFILING_SUPPORTED
        PRECONDITION(FastInterlockIncrement(&s_generationTableWriterCount) == 1);
        POSTCONDITION(FastInterlockDecrement(&s_generationTableWriterCount) == 0);
#endif // PROFILING_SUPPORTED
    } CONTRACT_END;

#ifdef PROFILING_SUPPORTED
    // Notify the profiler of start of the collection
    if (CORProfilerTrackGC() || CORProfilerTrackBasicGC())
    {
        // generate a new generation table
        GenerationTable *newGenerationTable = new (nothrow) GenerationTable();
        if (newGenerationTable == NULL)
            RETURN;
        newGenerationTable->count = 0;
        newGenerationTable->capacity = GenerationTable::defaultCapacity;
        // if there is already a current table, use its capacity as a guess for the capacity
        if (s_currentGenerationTable != NULL)
            newGenerationTable->capacity = s_currentGenerationTable->capacity;
        newGenerationTable->prev = NULL;
        newGenerationTable->genDescTable = new (nothrow) GenerationDesc[newGenerationTable->capacity];
        if (newGenerationTable->genDescTable == NULL)
            newGenerationTable->capacity = 0;

#ifdef  _DEBUG
        newGenerationTable->magic = GENERATION_TABLE_MAGIC;
#endif
        // fill in the values by calling back into the gc, which will report
        // the ranges by calling GenWalkFunc for each one
        IGCHeap *hp = GCHeapUtilities::GetGCHeap();
        hp->DiagDescrGenerations(GenWalkFunc, newGenerationTable);

        // remember the old table and plug in the new one
        GenerationTable *oldGenerationTable = s_currentGenerationTable;
        s_currentGenerationTable = newGenerationTable;

        // WARNING: tricky code!
        //
        // We sample the generation table lock *after* plugging in the new table
        // We do so using an interlocked operation so the cpu can't reorder
        // the write to the s_currentGenerationTable with the increment.
        // If the interlocked increment returns 1, we know nobody can be using
        // the old table (readers increment the lock before using the table,
        // and decrement it afterwards). Any new readers coming in
        // will use the new table. So it's safe to delete the old
        // table.
        // On the other hand, if the interlocked increment returns
        // something other than one, we put the old table on a list
        // dangling off of the new one. Next time around, we'll try again
        // deleting any old tables.
        if (FastInterlockIncrement(&s_generationTableLock) == 1)
        {
            // We know nobody can be using any of the old tables
            while (oldGenerationTable != NULL)
            {
                _ASSERTE(oldGenerationTable->magic == GENERATION_TABLE_MAGIC);
#ifdef  _DEBUG
                oldGenerationTable->magic = GENERATION_TABLE_BAD_MAGIC;
#endif
                GenerationTable *temp = oldGenerationTable;
                oldGenerationTable = oldGenerationTable->prev;
                delete[] temp->genDescTable;
                delete temp;
            }
        }
        else
        {
            // put the old table on a list
            newGenerationTable->prev = oldGenerationTable;
        }
        FastInterlockDecrement(&s_generationTableLock);
    }
#endif // PROFILING_SUPPORTED
    RETURN;
}

#ifdef PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
//
// Determines whether we are in a window to allow object inspection.
//
// Return Value:
//      Returns S_OK if we can determine that we are in a window to allow object
//      inspection.  Otherwise a failure HRESULT is returned
//

HRESULT AllowObjectInspection()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY; // tests for preemptive mode dynamically as its main function so contract enforcement is not appropriate
    }
    CONTRACTL_END;

    //
    // Check first to see if we are in the process of doing a GC and presume that the profiler
    // is making this object inspection from the same thread that notified of a valid ObjectID.
    //
    if (g_profControlBlock.fGCInProgress)
    {
        return S_OK;
    }

    //
    // Thus we must have a managed thread, and it must be in coop mode.
    // (That will also guarantee we're in a callback).
    //
    Thread * pThread = GetThreadNULLOk();

    if (pThread == NULL)
    {
        return CORPROF_E_NOT_MANAGED_THREAD;
    }

    // Note this is why we don't enforce the contract of being in cooperative mode the whole point
    // is that clients of this fellow want to return a robust error if not cooperative
    // so technically they are mode_any although the only true preemptive support they offer
    // is graceful failure in that case
    if (!pThread->PreemptiveGCDisabled())
    {
        return CORPROF_E_UNSUPPORTED_CALL_SEQUENCE;
    }

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// helper functions for the GC events
//


#endif // PROFILING_SUPPORTED

#if defined(PROFILING_SUPPORTED) || defined(FEATURE_EVENT_TRACE)

//---------------------------------------------------------------------------------------
//
// It's generally unsafe for profiling API code to call Get(GCSafe)TypeHandle() on
// objects, since we can encounter objects on the heap whose types belong to unloading
// AppDomains. In such cases, getting the type handle of the object could AV.  Use this
// function instead, which will return NULL for potentially unloaded types.
//
// Arguments:
//      pObj - Object * whose ClassID is desired
//
// Return Value:
//      ClassID of the object, if it's safe to look it up. Else NULL.
//

ClassID SafeGetClassIDFromObject(Object * pObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    TypeHandle th = pObj->GetGCSafeTypeHandleIfPossible();
    if(th == NULL)
    {
        return NULL;
    }

    return TypeHandleToClassID(th);
}

//---------------------------------------------------------------------------------------
//
// Callback of type walk_fn used by IGCHeap::DiagWalkObject.  Keeps a count of each
// object reference found.
//
// Arguments:
//      pBO - Object reference encountered in walk
//      context - running count of object references encountered
//
// Return Value:
//      Always returns TRUE to object walker so it walks the entire object
//

bool CountContainedObjectRef(Object * pBO, void * context)
{
    LIMITED_METHOD_CONTRACT;
    // Increase the count
    (*((size_t *)context))++;

    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// Callback of type walk_fn used by IGCHeap::DiagWalkObject.  Stores each object reference
// encountered into an array.
//
// Arguments:
//      pBO - Object reference encountered in walk
//      context - Array of locations within the walked object that point to other
//                objects.  On entry, (*context) points to the next unfilled array
//                entry.  On exit, that location is filled, and (*context) is incremented
//                to point to the next entry.
//
// Return Value:
//      Always returns TRUE to object walker so it walks the entire object
//

bool SaveContainedObjectRef(Object * pBO, void * context)
{
    LIMITED_METHOD_CONTRACT;
    // Assign the value
    **((Object ***)context) = pBO;

    // Now increment the array pointer
    //
    // Note that HeapWalkHelper has already walked the references once to count them up,
    // and then allocated an array big enough to hold those references.  First time this
    // callback is called for a given object, (*context) points to the first entry in the
    // array.  So "blindly" incrementing (*context) here and using it next time around
    // for the next reference, over and over again, should be safe.
    (*((Object ***)context))++;

    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// Callback of type walk_fn used by the GC when walking the heap, to help profapi and ETW
// track objects.  This orchestrates the use of the above callbacks which dig
// into object references contained each object encountered by this callback.
// This method is defined when either GC_PROFILING is defined or FEATURE_EVENT_TRACING
// is defined and can operate fully when only one of the two is defined.
//
// Arguments:
//      pBO - Object reference encountered on the heap
//      pvContext - Pointer to ProfilerWalkHeapContext, containing ETW context built up
//       during this GC, and which remembers if profapi-profiler is supposed to be called.
//
// Return Value:
//      BOOL indicating whether the heap walk should continue.
//      TRUE=continue
//      FALSE=stop
//
extern bool s_forcedGCInProgress;

bool HeapWalkHelper(Object * pBO, void * pvContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    OBJECTREF *   arrObjRef      = NULL;
    size_t        cNumRefs       = 0;
    bool          bOnStack       = false;
    MethodTable * pMT            = pBO->GetGCSafeMethodTable();

    ProfilerWalkHeapContext * pProfilerWalkHeapContext = (ProfilerWalkHeapContext *) pvContext;

    if (pMT->ContainsPointersOrCollectible())
    {
        // First round through calculates the number of object refs for this class
        GCHeapUtilities::GetGCHeap()->DiagWalkObject(pBO, &CountContainedObjectRef, (void *)&cNumRefs);

        if (cNumRefs > 0)
        {
            // Create an array to contain all of the refs for this object
            bOnStack = cNumRefs <= 32 ? true : false;

            if (bOnStack)
            {
                // It's small enough, so just allocate on the stack
                arrObjRef = (OBJECTREF *)_alloca(cNumRefs * sizeof(OBJECTREF));
            }
            else
            {
                // Otherwise, allocate from the heap
                arrObjRef = new (nothrow) OBJECTREF[cNumRefs];

                if (!arrObjRef)
                {
                    return FALSE;
                }
            }

            // Second round saves off all of the ref values
            OBJECTREF * pCurObjRef = arrObjRef;
            GCHeapUtilities::GetGCHeap()->DiagWalkObject(pBO, &SaveContainedObjectRef, (void *)&pCurObjRef);
        }
    }

    HRESULT hr = E_FAIL;

#if defined(GC_PROFILING)
    if (pProfilerWalkHeapContext->fProfilerPinned)
    {
        // It is not safe and could be overflowed to downcast size_t to ULONG on WIN64.
        // However, we have to do this dangerous downcast here to comply with the existing Profiling COM interface.
        // We are currently evaluating ways to fix this potential overflow issue.
        hr = g_profControlBlock.ObjectReference(
            (ObjectID) pBO,
            SafeGetClassIDFromObject(pBO),
            (ULONG) cNumRefs,
            (ObjectID *) arrObjRef);
    }
#endif

#ifdef FEATURE_EVENT_TRACE
    if (s_forcedGCInProgress &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     CLR_GCHEAPDUMP_KEYWORD))
    {
        ETW::GCLog::ObjectReference(
            pProfilerWalkHeapContext,
            pBO,
            (ULONGLONG) SafeGetClassIDFromObject(pBO),
            cNumRefs,
            (Object **) arrObjRef);

    }
#endif // FEATURE_EVENT_TRACE

    // If the data was not allocated on the stack, need to clean it up.
    if ((arrObjRef != NULL) && !bOnStack)
    {
        delete [] arrObjRef;
    }

    // Return TRUE iff we want to the heap walk to continue. The only way we'd abort the
    // heap walk is if we're issuing profapi callbacks, and the profapi profiler
    // intentionally returned a failed HR (as its request that we stop the walk). There's
    // a potential conflict here. If a profapi profiler and an ETW profiler are both
    // monitoring the heap dump, and the profapi profiler requests to abort the walk (but
    // the ETW profiler may not want to abort the walk), then what do we do? The profapi
    // profiler gets precedence. We don't want to accidentally send more callbacks to a
    // profapi profiler that explicitly requested an abort. The ETW profiler will just
    // have to deal. In theory, I could make the code more complex by remembering that a
    // profapi profiler requested to abort the dump but an ETW profiler is still
    // attached, and then intentionally inhibit the remainder of the profapi callbacks
    // for this GC. But that's unnecessary complexity. In practice, it should be
    // extremely rare that a profapi profiler is monitoring heap dumps AND an ETW
    // profiler is also monitoring heap dumps.
    return (pProfilerWalkHeapContext->fProfilerPinned) ? SUCCEEDED(hr) : TRUE;
}

#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACING)

#ifdef PROFILING_SUPPORTED
//---------------------------------------------------------------------------------------
//
// Callback of type walk_fn used by the GC when walking the heap, to help profapi
// track objects.  This is really just a wrapper around
// EEToProfInterfaceImpl::AllocByClass, which does the real work
//
// Arguments:
//      pBO - Object reference encountered on the heap
//      pv - Structure used by EEToProfInterfaceImpl::AllocByClass to do its work.
//
// Return Value:
//      BOOL indicating whether the heap walk should continue.
//      TRUE=continue
//      FALSE=stop
//      Currently always returns TRUE
//

bool AllocByClassHelper(Object * pBO, void * pv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    _ASSERTE(pv != NULL);

    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackAllocations());
        // Pass along the call
        g_profControlBlock.AllocByClass(
            (ObjectID) pBO,
            SafeGetClassIDFromObject(pBO),
            pv);
        END_PROFILER_CALLBACK();
    }

    return TRUE;
}

#endif // PROFILING_SUPPORTED
#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

//---------------------------------------------------------------------------------------
//
// Callback of type promote_func called by GC while scanning roots (in GCProfileWalkHeap,
// called after the collection).  Wrapper around EEToProfInterfaceImpl::RootReference2,
// which does the real work.
//
// Arguments:
//      pObj - Object reference encountered
///     ppRoot - Address that references ppObject (can be interior pointer)
//      pSC - ProfilingScanContext * containing the root kind and GCReferencesData used
//            by RootReference2
//      dwFlags - Properties of the root as GC_CALL* constants (this function converts
//                to COR_PRF_GC_ROOT_FLAGS.
//

void ScanRootsHelper(Object* pObj, Object ** ppRoot, ScanContext *pSC, uint32_t dwFlags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // RootReference2 can return E_OUTOFMEMORY, and we're swallowing that.
    // Furthermore, we can't really handle it because we're callable during GC promotion.
    // On the other hand, this only means profiling information will be incomplete,
    // so it's ok to swallow E_OUTOFMEMORY.
    //
    FAULT_NOT_FATAL();

    ProfilingScanContext *pPSC = (ProfilingScanContext *)pSC;

    DWORD dwEtwRootFlags = 0;
    if (dwFlags & GC_CALL_INTERIOR)
        dwEtwRootFlags |= kEtwGCRootFlagsInterior;
    if (dwFlags & GC_CALL_PINNED)
        dwEtwRootFlags |= kEtwGCRootFlagsPinning;

#if defined(GC_PROFILING)
    void *rootID = NULL;
    switch (pPSC->dwEtwRootKind)
    {
    case    kEtwGCRootKindStack:
        rootID = pPSC->pMD;
        break;

    case    kEtwGCRootKindHandle:
        _ASSERT(!"Shouldn't see handle here");
        break;

    case    kEtwGCRootKindFinalizer:
    default:
        break;
    }

    // Notify profiling API of the root
    if (pPSC->fProfilerPinned)
    {
        // Let the profiling code know about this root reference
        g_profControlBlock.RootReference2((BYTE *)pObj, pPSC->dwEtwRootKind, (EtwGCRootFlags)dwEtwRootFlags, (BYTE *)rootID, &((pPSC)->pHeapId));
    }
#endif

#ifdef FEATURE_EVENT_TRACE
    // Notify ETW of the root
    if (s_forcedGCInProgress &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     CLR_GCHEAPDUMP_KEYWORD))
    {
        ETW::GCLog::RootReference(
            NULL,           // handle is NULL, cuz this is a non-HANDLE root
            pObj,           // object being rooted
            NULL,           // pSecondaryNodeForDependentHandle is NULL, cuz this isn't a dependent handle
            FALSE,          // is dependent handle
            pPSC,
            dwFlags,        // dwGCFlags
            dwEtwRootFlags);
    }
#endif // FEATURE_EVENT_TRACE
}

#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
#ifdef PROFILING_SUPPORTED

//---------------------------------------------------------------------------------------
//
// Private ProfToEEInterfaceImpl maintenance functions
//


//---------------------------------------------------------------------------------------
//
// Initialize ProfToEEInterfaceImpl (including ModuleILHeap statics)
//
// Return Value:
//      HRESULT indicating success
//

HRESULT ProfToEEInterfaceImpl::Init()
{
    CONTRACTL
    {
        NOTHROW;
        CANNOT_TAKE_LOCK;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORPROF, LL_INFO1000, "**PROF: Init.\n"));

#ifdef _DEBUG
    if (ProfilingAPIUtility::ShouldInjectProfAPIFault(kProfAPIFault_StartupInternal))
    {
        return E_OUTOFMEMORY;
    }
#endif //_DEBUG

    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Destroy ProfToEEInterfaceImpl (including ModuleILHeap statics)
//

ProfToEEInterfaceImpl::~ProfToEEInterfaceImpl()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORPROF, LL_INFO1000, "**PROF: Terminate.\n"));
}

//---------------------------------------------------------------------------------------
//
// Obsolete info functions
//

HRESULT ProfToEEInterfaceImpl::GetInprocInspectionInterface(IUnknown **)
{
    LIMITED_METHOD_CONTRACT;
    return E_NOTIMPL;
}

HRESULT ProfToEEInterfaceImpl::GetInprocInspectionIThisThread(IUnknown **)
{
    LIMITED_METHOD_CONTRACT;
    return E_NOTIMPL;
}

HRESULT ProfToEEInterfaceImpl::BeginInprocDebugging(BOOL, DWORD *)
{
    LIMITED_METHOD_CONTRACT;
    return E_NOTIMPL;
}

HRESULT ProfToEEInterfaceImpl::EndInprocDebugging(DWORD)
{
    LIMITED_METHOD_CONTRACT;
    return E_NOTIMPL;
}

HRESULT ProfToEEInterfaceImpl::SetFunctionReJIT(FunctionID)
{
    LIMITED_METHOD_CONTRACT;
    return E_NOTIMPL;
}




//---------------------------------------------------------------------------------------
//
// *******************************
// Public Profiler->EE entrypoints
// *******************************
//
// ProfToEEInterfaceImpl implementation of public ICorProfilerInfo* methods
//
// NOTE: All ICorProfilerInfo* method implementations must follow the rules stated
// at the top of this file!
//

// See corprof.idl / MSDN for detailed comments about each of these public
// functions, their parameters, return values, etc.

HRESULT ProfToEEInterfaceImpl::SetEventMask(DWORD dwEventMask)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: SetEventMask 0x%08x.\n",
        dwEventMask));

    _ASSERTE(CORProfilerPresentOrInitializing());

    return m_pProfilerInfo->pProfInterface->SetEventMask(dwEventMask, 0 /* No high bits */);
}

HRESULT ProfToEEInterfaceImpl::SetEventMask2(DWORD dwEventsLow, DWORD dwEventsHigh)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: SetEventMask2 0x%08x, 0x%08x.\n",
        dwEventsLow, dwEventsHigh));

    _ASSERTE(CORProfilerPresentOrInitializing());

    return m_pProfilerInfo->pProfInterface->SetEventMask(dwEventsLow, dwEventsHigh);
}


HRESULT ProfToEEInterfaceImpl::GetHandleFromThread(ThreadID threadId, HANDLE *phThread)
{
    CONTRACTL
{
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetHandleFromThread 0x%p.\n",
        threadId));

    if (!IsManagedThread(threadId))
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    HANDLE hThread = ((Thread *)threadId)->GetThreadHandle();

    if (hThread == INVALID_HANDLE_VALUE)
        hr = E_INVALIDARG;

    else if (phThread)
        *phThread = hThread;

    return (hr);
}

HRESULT ProfToEEInterfaceImpl::GetObjectSize(ObjectID objectId, ULONG *pcSize)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!  Fail at runtime if in preemptive mode via AllowObjectInspection()
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetObjectSize 0x%p.\n",
         objectId));

    if (objectId == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = AllowObjectInspection();
    if (FAILED(hr))
    {
        return hr;
    }

    // Get the object pointer
    Object *pObj = reinterpret_cast<Object *>(objectId);

    // Get the size
    if (pcSize)
    {
        SIZE_T size = pObj->GetSize();

        if(size < MIN_OBJECT_SIZE)
        {
            size = PtrAlign(size);
        }

        if (size > UINT32_MAX)
        {
            *pcSize = UINT32_MAX;
            return COR_E_OVERFLOW;
        }
        *pcSize = (ULONG)size;
    }

    // Indicate success
    return (S_OK);
}

HRESULT ProfToEEInterfaceImpl::GetObjectSize2(ObjectID objectId, SIZE_T *pcSize)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!  Fail at runtime if in preemptive mode via AllowObjectInspection()
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetObjectSize2 0x%p.\n",
         objectId));

    if (objectId == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = AllowObjectInspection();
    if (FAILED(hr))
    {
        return hr;
    }

    // Get the object pointer
    Object *pObj = reinterpret_cast<Object *>(objectId);

    // Get the size
    if (pcSize)
    {
        SIZE_T size = pObj->GetSize();

        if(size < MIN_OBJECT_SIZE)
        {
            size = PtrAlign(size);
        }

        *pcSize = size;
    }

    // Indicate success
    return (S_OK);
}


HRESULT ProfToEEInterfaceImpl::IsArrayClass(
    /* [in] */  ClassID classId,
    /* [out] */ CorElementType *pBaseElemType,
    /* [out] */ ClassID *pBaseClassId,
    /* [out] */ ULONG   *pcRank)
{
    CONTRACTL
    {
        NOTHROW;

        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: IsArrayClass 0x%p.\n",
         classId));

    HRESULT hr;

    if (classId == NULL)
    {
        return E_INVALIDARG;
    }

    TypeHandle th = TypeHandle::FromPtr((void *)classId);

    if (th.IsArray())
    {
        // Fill in the type if they want it
        if (pBaseElemType != NULL)
        {
            *pBaseElemType = th.GetArrayElementTypeHandle().GetVerifierCorElementType();
        }

        // If this is an array of classes and they wish to have the base type
        // If there is no associated class with this type, then there's no problem
        // because GetClass returns NULL which is the default we want to return in
        // this case.
        // Note that for generic code we always return uninstantiated ClassIDs and FunctionIDs
        if (pBaseClassId != NULL)
        {
            *pBaseClassId = TypeHandleToClassID(th.GetArrayElementTypeHandle());
        }

        // If they want the number of dimensions of the array
        if (pcRank != NULL)
        {
            *pcRank = (ULONG) th.GetRank();
        }

        // S_OK indicates that this was indeed an array
        hr = S_OK;
    }
    else
    {
        if (pBaseClassId != NULL)
        {
            *pBaseClassId = NULL;
        }

        // This is not an array, S_FALSE indicates so.
        hr = S_FALSE;
    }

    return hr;
}

HRESULT ProfToEEInterfaceImpl::GetThreadInfo(ThreadID threadId, DWORD *pdwWin32ThreadId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetThreadInfo 0x%p.\n",
         threadId));

    if (!IsManagedThread(threadId))
    {
        return E_INVALIDARG;
    }

    if (pdwWin32ThreadId)
    {
        *pdwWin32ThreadId = ((Thread *)threadId)->GetOSThreadId();
    }

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::GetCurrentThreadID(ThreadID *pThreadId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetCurrentThreadID.\n"));

    HRESULT hr = S_OK;

    // No longer assert that GetThread doesn't return NULL, since callbacks
    // can now occur on non-managed threads (such as the GC helper threads)
    Thread * pThread = GetThreadNULLOk();

    // If pThread is null, then the thread has never run managed code and
    // so has no ThreadID
    if (!IsManagedThread(pThread))
        hr = CORPROF_E_NOT_MANAGED_THREAD;

    // Only provide value if they want it
    else if (pThreadId)
        *pThreadId = (ThreadID) pThread;

    return (hr);
}

//---------------------------------------------------------------------------------------
//
// Internal helper function to wrap a call into the JIT manager to get information about
// a managed function based on IP
//
// Arguments:
//      ip - IP address inside managed function of interest
//      ppCodeInfo - [out] information about the managed function based on IP
//
// Return Value:
//     HRESULT indicating success or failure.
//
//

HRESULT GetFunctionInfoInternal(LPCBYTE ip, EECodeInfo * pCodeInfo)
{
    CONTRACTL
    {
        NOTHROW;

        GC_NOTRIGGER;
        EE_THREAD_NOT_REQUIRED;
        CAN_TAKE_LOCK;
        CANNOT_RETAKE_LOCK;


        // If this is called asynchronously (from a hijacked thread, as with F1), it must not re-enter the
        // host (SQL).  Corners will be cut to ensure this is the case
        if (ShouldAvoidHostCalls()) { HOST_NOCALLS; } else { HOST_CALLS; }
    }
    CONTRACTL_END;

    // Before calling into the code manager, ensure the GC heap has been
    // initialized--else the code manager will assert trying to get info from the heap.
    if (!IsGarbageCollectorFullyInitialized())
    {
        return CORPROF_E_NOT_YET_AVAILABLE;
    }

    if (ShouldAvoidHostCalls())
    {
        ExecutionManager::ReaderLockHolder rlh(NoHostCalls);
        if (!rlh.Acquired())
        {
            // Couldn't get the info.  Try again later
            return CORPROF_E_ASYNCHRONOUS_UNSAFE;
        }

        pCodeInfo->Init((PCODE)ip, ExecutionManager::ScanNoReaderLock);
    }
    else
    {
        pCodeInfo->Init((PCODE)ip);
    }

    if (!pCodeInfo->IsValid())
    {
        return E_FAIL;
    }

    return S_OK;
}


HRESULT GetFunctionFromIPInternal(LPCBYTE ip, EECodeInfo * pCodeInfo, BOOL failOnNoMetadata)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    _ASSERTE (pCodeInfo != NULL);

    HRESULT hr = GetFunctionInfoInternal(ip, pCodeInfo);
    if (FAILED(hr))
    {
        return hr;
    }

    if (failOnNoMetadata)
    {
        // never return a method that the user of the profiler API cannot use
        if (pCodeInfo->GetMethodDesc()->IsNoMetadata())
        {
            return E_FAIL;
        }
    }

    return S_OK;
}


HRESULT ProfToEEInterfaceImpl::GetFunctionFromIP(LPCBYTE ip, FunctionID * pFunctionId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Querying the code manager requires a reader lock.  However, see
        // code:#DisableLockOnAsyncCalls
        DISABLED(CAN_TAKE_LOCK);

        // Asynchronous functions can be called at arbitrary times when runtime
        // is holding locks that cannot be reentered without causing deadlock.
        // This contract detects any attempts to reenter locks held at the time
        // this function was called.
        CANNOT_RETAKE_LOCK;


        // If this is called asynchronously (from a hijacked thread, as with F1), it must not re-enter the
        // host (SQL).  Corners will be cut to ensure this is the case
        if (ShouldAvoidHostCalls()) { HOST_NOCALLS; } else { HOST_CALLS; }
    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetFunctionFromIP 0x%p.\n",
        ip));

    // This call is allowed asynchronously, but the JIT functions take a reader lock.
    // So we need to ensure the current thread hasn't been hijacked by a profiler while
    // it was holding the writer lock.  Checking the ForbidSuspendThread region is a
    // sufficient test for this
    FAIL_IF_IN_FORBID_SUSPEND_REGION();

    HRESULT hr = S_OK;

    EECodeInfo codeInfo;

    hr = GetFunctionFromIPInternal(ip, &codeInfo, /* failOnNoMetadata */ TRUE);
    if (FAILED(hr))
    {
        return hr;
    }

    if (pFunctionId)
    {
        *pFunctionId = MethodDescToFunctionID(codeInfo.GetMethodDesc());
    }

    return S_OK;
}


HRESULT ProfToEEInterfaceImpl::GetFunctionFromIP2(LPCBYTE ip, FunctionID * pFunctionId, ReJITID * pReJitId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Grabbing the rejitid requires entering the rejit manager's hash table & lock,
        // which can switch us to preemptive mode and trigger GCs
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Grabbing the rejitid requires entering the rejit manager's hash table & lock,
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetFunctionFromIP2 0x%p.\n",
        ip));

    HRESULT hr = S_OK;

    EECodeInfo codeInfo;

    hr = GetFunctionFromIPInternal(ip, &codeInfo, /* failOnNoMetadata */ TRUE);
    if (FAILED(hr))
    {
        return hr;
    }

    if (pFunctionId)
    {
        *pFunctionId = MethodDescToFunctionID(codeInfo.GetMethodDesc());
    }

    if (pReJitId != NULL)
    {
        MethodDesc * pMD = codeInfo.GetMethodDesc();
        *pReJitId = ReJitManager::GetReJitId(pMD, codeInfo.GetStartAddress());
    }

    return S_OK;
}

//*****************************************************************************
// Given a function id, retrieve the metadata token and a reader api that
// can be used against the token.
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::GetTokenAndMetaDataFromFunction(
    FunctionID  functionId,
    REFIID      riid,
    IUnknown    **ppOut,
    mdToken     *pToken)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // PEFile::GetRWImporter and GetReadablePublicMetaDataInterface take locks
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetTokenAndMetaDataFromFunction 0x%p.\n",
         functionId));

    if (functionId == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT     hr = S_OK;

    MethodDesc *pMD = FunctionIdToMethodDesc(functionId);

    if (pToken)
    {
        *pToken = pMD->GetMemberDef();
    }

    // don't bother with any of this module fetching if the metadata access isn't requested
    if (ppOut)
    {
        Module * pMod = pMD->GetModule();
        hr = pMod->GetReadablePublicMetaDataInterface(ofRead, riid, (LPVOID *) ppOut);
    }

    return hr;
}

//---------------------------------------------------------------------------------------
// What follows are the GetCodeInfo* APIs and their helpers.  The two helpers factor out
// some of the common code to validate parameters and then determine the code info from
// the start of the code.  Each individual GetCodeInfo* API differs in how it uses these
// helpers, particuarly in how it determines the start of the code (GetCodeInfo3 needs
// to use the rejit manager to determine the code start, whereas the others do not).
// Factoring out like this allows us to have statically determined contracts that differ
// based on whether we need to use the rejit manager, which requires locking and
// may trigger GCs.
//---------------------------------------------------------------------------------------


HRESULT ValidateParametersForGetCodeInfo(
    MethodDesc * pMethodDesc,
    ULONG32  cCodeInfos,
    COR_PRF_CODE_INFO codeInfos[])
{
    LIMITED_METHOD_CONTRACT;

    if (pMethodDesc == NULL)
    {
        return E_INVALIDARG;
    }

    if ((cCodeInfos != 0) && (codeInfos == NULL))
    {
        return E_INVALIDARG;
    }

    if (pMethodDesc->HasClassOrMethodInstantiation() && pMethodDesc->IsTypicalMethodDefinition())
    {
        // In this case, we used to replace pMethodDesc with its canonical instantiation
        // (FindOrCreateTypicalSharedInstantiation).  However, a profiler should never be able
        // to get to this point anyway, since any MethodDesc a profiler gets from us
        // cannot be typical (i.e., cannot be a generic with types still left uninstantiated).
        // We assert here just in case a test proves me wrong, but generally we will
        // disallow this code path.
        _ASSERTE(!"Profiler passed a typical method desc (a generic with types still left uninstantiated) to GetCodeInfo2");
        return E_INVALIDARG;
    }

    return S_OK;
}

HRESULT GetCodeInfoFromCodeStart(
    PCODE start,
    ULONG32  cCodeInfos,
    ULONG32 * pcCodeInfos,
    COR_PRF_CODE_INFO codeInfos[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        // We need to take the ExecutionManager reader lock to find the
        // appropriate jit manager.
        CAN_TAKE_LOCK;


        // If this is called asynchronously (from a hijacked thread, as with F1), it must not re-enter the
        // host (SQL).  Corners will be cut to ensure this is the case
        if (ShouldAvoidHostCalls()) { HOST_NOCALLS; } else { HOST_CALLS; }
    }
    CONTRACTL_END;

    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    ///////////////////////////////////
    // Get the code region info for this function. This is a multi step process.
    //
    // MethodDesc ==> Code Address ==> JitMananger ==>
    // MethodToken ==> MethodRegionInfo
    //
    // (Our caller handled the first step: MethodDesc ==> Code Address.)
    //
    // <WIN64-ONLY>
    //
    // On WIN64 we have a choice of where to go to find out the function address range size:
    // GC info (which is what we're doing below on all architectures) or the OS unwind
    // info, stored in the RUNTIME_FUNCTION structure.  The latter produces
    // a SMALLER size than the former, because the latter excludes some data from
    // the set we report to the OS for unwind info.  For example, switch tables can be
    // separated out from the regular code and not be reported as OS unwind info, and thus
    // those addresses will not appear in the range reported by the RUNTIME_FUNCTION gotten via:
    //
    //      IJitManager* pJitMan = ExecutionManager::FindJitMan((PBYTE)codeInfos[0].startAddress);
    //      PRUNTIME_FUNCTION pfe = pJitMan->GetUnwindInfo((PBYTE)codeInfos[0].startAddress);
    //      *pcCodeInfos = (ULONG) (pfe->EndAddress - pfe->BeginAddress);
    //
    // (Note that GCInfo & OS unwind info report the same start address--it's the size that's
    // different.)
    //
    // The advantage of using the GC info is that it's available on all architectures,
    // and it gives you a more complete picture of the addresses belonging to the function.
    //
    // A disadvantage of using GC info is we'll report those extra addresses (like switch
    // tables) that a profiler might turn back around and use in a call to
    // GetFunctionFromIP.  A profiler may expect we'd be able to map back any address
    // in the function's GetCodeInfo ranges back to that function's FunctionID (methoddesc).  But
    // querying these extra addresses will cause GetFunctionFromIP to fail, as they're not
    // actually valid instruction addresses that the IP register can be set to.
    //
    // The advantage wins out, so we're going with GC info everywhere.
    //
    // </WIN64-ONLY>

    HRESULT hr;

    if (start == NULL)
    {
        return CORPROF_E_FUNCTION_NOT_COMPILED;
    }

    EECodeInfo codeInfo;
    hr = GetFunctionInfoInternal(
        (LPCBYTE) start,
        &codeInfo);
    if (hr == CORPROF_E_ASYNCHRONOUS_UNSAFE)
    {
        _ASSERTE(ShouldAvoidHostCalls());
        return hr;
    }
    if (FAILED(hr))
    {
        return CORPROF_E_FUNCTION_NOT_COMPILED;
    }

    IJitManager::MethodRegionInfo methodRegionInfo;
    codeInfo.GetMethodRegionInfo(&methodRegionInfo);

    //
    // Fill out the codeInfo structures with valuse from the
    // methodRegion
    //
    // Note that we're assuming that a method will never be split into
    // more than two regions ... this is unlikely to change any time in
    // the near future.
    //
    if (NULL != codeInfos)
    {
        if (cCodeInfos > 0)
        {
            //
            // We have to return the two regions in the order that they would appear
            // if straight-line compiled
            //
            if (PCODEToPINSTR(start) == methodRegionInfo.hotStartAddress)
            {
                codeInfos[0].startAddress =
                    (UINT_PTR)methodRegionInfo.hotStartAddress;
                codeInfos[0].size = methodRegionInfo.hotSize;
            }
            else
            {
                _ASSERTE(methodRegionInfo.coldStartAddress != NULL);
                codeInfos[0].startAddress =
                    (UINT_PTR)methodRegionInfo.coldStartAddress;
                codeInfos[0].size = methodRegionInfo.coldSize;
            }

            if (NULL != methodRegionInfo.coldStartAddress)
            {
                if (cCodeInfos > 1)
                {
                    if (PCODEToPINSTR(start) == methodRegionInfo.hotStartAddress)
                    {
                        codeInfos[1].startAddress =
                            (UINT_PTR)methodRegionInfo.coldStartAddress;
                        codeInfos[1].size = methodRegionInfo.coldSize;
                    }
                    else
                    {
                        codeInfos[1].startAddress =
                            (UINT_PTR)methodRegionInfo.hotStartAddress;
                        codeInfos[1].size = methodRegionInfo.hotSize;
                    }
                }
            }
        }
    }

    if (NULL != pcCodeInfos)
    {
        *pcCodeInfos = (NULL != methodRegionInfo.coldStartAddress) ? 2 : 1;
    }


    return S_OK;
}

//*****************************************************************************
// Gets the location and size of a jitted function
//*****************************************************************************

HRESULT ProfToEEInterfaceImpl::GetCodeInfo(FunctionID functionId, LPCBYTE * pStart, ULONG * pcSize)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // (See locking contract comment in GetCodeInfoHelper.)
        DISABLED(CAN_TAKE_LOCK);

        // (See locking contract comment in GetCodeInfoHelper.)
        CANNOT_RETAKE_LOCK;


        // If this is called asynchronously (from a hijacked thread, as with F1), it must not re-enter the
        // host (SQL).  Corners will be cut to ensure this is the case
        if (ShouldAvoidHostCalls()) { HOST_NOCALLS; } else { HOST_CALLS; }
    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    // This is called asynchronously, but GetCodeInfoHelper() will
    // ensure we're not called at a dangerous time
    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetCodeInfo 0x%p.\n",
        functionId));

    // GetCodeInfo may be called asynchronously, and the JIT functions take a reader
    // lock.  So we need to ensure the current thread hasn't been hijacked by a profiler while
    // it was holding the writer lock.  Checking the ForbidSuspendThread region is a sufficient test for this
    FAIL_IF_IN_FORBID_SUSPEND_REGION();

    if (functionId == 0)
    {
        return E_INVALIDARG;
    }

    MethodDesc * pMethodDesc = FunctionIdToMethodDesc(functionId);

    COR_PRF_CODE_INFO codeInfos[2];
    ULONG32 cCodeInfos;

    HRESULT hr = GetCodeInfoFromCodeStart(
        pMethodDesc->GetNativeCode(),
        _countof(codeInfos),
        &cCodeInfos,
        codeInfos);

    if ((FAILED(hr)) || (0 == cCodeInfos))
    {
        return hr;
    }

    if (NULL != pStart)
    {
        *pStart = reinterpret_cast< LPCBYTE >(codeInfos[0].startAddress);
    }

    if (NULL != pcSize)
    {
        if (!FitsIn<ULONG>(codeInfos[0].size))
        {
            return E_UNEXPECTED;
        }
        *pcSize = static_cast<ULONG>(codeInfos[0].size);
    }

    return hr;
}

HRESULT ProfToEEInterfaceImpl::GetCodeInfo2(FunctionID functionId,
                                            ULONG32  cCodeInfos,
                                            ULONG32 * pcCodeInfos,
                                            COR_PRF_CODE_INFO codeInfos[])
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // (See locking contract comment in GetCodeInfoHelper.)
        DISABLED(CAN_TAKE_LOCK);

        // (See locking contract comment in GetCodeInfoHelper.)
        CANNOT_RETAKE_LOCK;


        // If this is called asynchronously (from a hijacked thread, as with F1), it must not re-enter the
        // host (SQL).  Corners will be cut to ensure this is the case
        if (ShouldAvoidHostCalls()) { HOST_NOCALLS; } else { HOST_CALLS; }

        PRECONDITION(CheckPointer(pcCodeInfos, NULL_OK));
        PRECONDITION(CheckPointer(codeInfos, NULL_OK));
    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetCodeInfo2 0x%p.\n",
        functionId));

    HRESULT hr = S_OK;

    EX_TRY
    {
        MethodDesc * pMethodDesc = FunctionIdToMethodDesc(functionId);

        hr = ValidateParametersForGetCodeInfo(pMethodDesc, cCodeInfos, codeInfos);
        if (SUCCEEDED(hr))
        {
            hr = GetCodeInfoFromCodeStart(
                pMethodDesc->GetNativeCode(),
                cCodeInfos,
                pcCodeInfos,
                codeInfos);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


HRESULT ProfToEEInterfaceImpl::GetCodeInfo3(FunctionID functionId,
                                            ReJITID  reJitId,
                                            ULONG32  cCodeInfos,
                                            ULONG32* pcCodeInfos,
                                            COR_PRF_CODE_INFO codeInfos[])


{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // We need to access the rejitmanager, which means taking locks, which means we
        // may trigger a GC
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // We need to access the rejitmanager, which means taking locks
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(pcCodeInfos, NULL_OK));
        PRECONDITION(CheckPointer(codeInfos, NULL_OK));
    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetCodeInfo3 0x%p 0x%p.\n",
        functionId, reJitId));

    HRESULT hr = S_OK;

    EX_TRY
    {
        MethodDesc * pMethodDesc = FunctionIdToMethodDesc(functionId);

        hr = ValidateParametersForGetCodeInfo(pMethodDesc, cCodeInfos, codeInfos);
        if (SUCCEEDED(hr))
        {
            PCODE pCodeStart = NULL;
            CodeVersionManager* pCodeVersionManager = pMethodDesc->GetCodeVersionManager();
            {
                CodeVersionManager::LockHolder codeVersioningLockHolder;

                ILCodeVersion ilCodeVersion = pCodeVersionManager->GetILCodeVersion(pMethodDesc, reJitId);

                NativeCodeVersionCollection nativeCodeVersions = ilCodeVersion.GetNativeCodeVersions(pMethodDesc);
                for (NativeCodeVersionIterator iter = nativeCodeVersions.Begin(); iter != nativeCodeVersions.End(); iter++)
                {
                    // Now that tiered compilation can create more than one jitted code version for the same rejit id
                    // we are arbitrarily choosing the first one to return.  To address a specific version of native code
                    // use GetCodeInfo4.
                    pCodeStart = iter->GetNativeCode();
                    break;
                }
            }

            hr = GetCodeInfoFromCodeStart(pCodeStart,
                                          cCodeInfos,
                                          pcCodeInfos,
                                          codeInfos);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


HRESULT ProfToEEInterfaceImpl::GetEventMask(DWORD * pdwEvents)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;


    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO10,
        "**PROF: GetEventMask.\n"));

    if (pdwEvents == NULL)
    {
        return E_INVALIDARG;
    }

    *pdwEvents = m_pProfilerInfo->eventMask.GetEventMask();
    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::GetEventMask2(DWORD *pdwEventsLow, DWORD *pdwEventsHigh)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;


    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO10,
        "**PROF: GetEventMask2.\n"));

    if ((pdwEventsLow == NULL) || (pdwEventsHigh == NULL))
    {
        return E_INVALIDARG;
    }

    *pdwEventsLow = m_pProfilerInfo->eventMask.GetEventMask();
    *pdwEventsHigh = m_pProfilerInfo->eventMask.GetEventMaskHigh();
    return S_OK;
}

// static
void ProfToEEInterfaceImpl::MethodTableCallback(void* context, void* objectUNSAFE)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // each callback identifies the address of a method table within the frozen object segment
    // that pointer is an object ID by definition -- object references point to the method table
    CDynArray< ObjectID >* objects = reinterpret_cast< CDynArray< ObjectID >* >(context);

    *objects->Append() = reinterpret_cast< ObjectID >(objectUNSAFE);
}

// static
void ProfToEEInterfaceImpl::ObjectRefCallback(void* context, void* objectUNSAFE)
{
    // we don't care about embedded object references, ignore them
}


HRESULT ProfToEEInterfaceImpl::EnumModuleFrozenObjects(ModuleID moduleID,
                                                       ICorProfilerObjectEnum** ppEnum)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: EnumModuleFrozenObjects 0x%p.\n",
         moduleID));

    if (NULL == ppEnum)
    {
        return E_INVALIDARG;
    }

    Module* pModule = reinterpret_cast< Module* >(moduleID);
    if (pModule == NULL || pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    HRESULT hr = S_OK;

    EX_TRY
    {
        // If we don't support frozen objects at all, then just return empty
        // enumerator.
        *ppEnum = new ProfilerObjectEnum();
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}



/*
 * GetArrayObjectInfo
 *
 * This function returns informatin about array objects.  In particular, the dimensions
 * and where the data buffer is stored.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetArrayObjectInfo(ObjectID objectId,
                    ULONG32 cDimensionSizes,
                    ULONG32 pDimensionSizes[],
                    int pDimensionLowerBounds[],
                    BYTE    **ppData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!  Fail at runtime if in preemptive mode via AllowObjectInspection()
        MODE_ANY;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetArrayObjectInfo 0x%p.\n",
         objectId));

    if (objectId == NULL)
    {
        return E_INVALIDARG;
    }

    if ((pDimensionSizes == NULL) ||
        (pDimensionLowerBounds == NULL) ||
        (ppData == NULL))
    {
        return E_INVALIDARG;
    }

    HRESULT hr = AllowObjectInspection();
    if (FAILED(hr))
    {
        return hr;
    }

    Object * pObj = reinterpret_cast<Object *>(objectId);

    // GC callbacks may come from a non-EE thread, which is considered permanently preemptive.
    // We are about calling some object inspection functions, which require to be in co-op mode.
    // Given that none managed objects can be changed by managed code until GC resumes the
    // runtime, it is safe to violate the mode contract and to inspect managed objects from a
    // non-EE thread when GetArrayObjectInfo is called within GC callbacks.
    if (NativeThreadInGC())
    {
        CONTRACT_VIOLATION(ModeViolation);
        return GetArrayObjectInfoHelper(pObj, cDimensionSizes, pDimensionSizes, pDimensionLowerBounds, ppData);
    }

    return GetArrayObjectInfoHelper(pObj, cDimensionSizes, pDimensionSizes, pDimensionLowerBounds, ppData);
}

HRESULT ProfToEEInterfaceImpl::GetArrayObjectInfoHelper(Object * pObj,
                    ULONG32 cDimensionSizes,
                    __out_ecount(cDimensionSizes) ULONG32 pDimensionSizes[],
                    __out_ecount(cDimensionSizes) int pDimensionLowerBounds[],
                    BYTE    **ppData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Because of the object pointer parameter, we must be either in CO-OP mode,
        // or on a non-EE thread in the process of doing a GC .
        if (!NativeThreadInGC()) { MODE_COOPERATIVE; }

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    // Must have an array.
    MethodTable * pMT = pObj->GetMethodTable();
    if (!pMT->IsArray())
    {
        return E_INVALIDARG;
    }

    ArrayBase * pArray = static_cast<ArrayBase*> (pObj);

    unsigned rank = pArray->GetRank();

    if (cDimensionSizes < rank)
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    // Copy range for each dimension (rank)
    int * pBounds      = pArray->GetBoundsPtr();
    int * pLowerBounds = pArray->GetLowerBoundsPtr();

    unsigned i;
    for(i = 0; i < rank; i++)
    {
        pDimensionSizes[i]       = pBounds[i];
        pDimensionLowerBounds[i] = pLowerBounds[i];
    }

    // Pointer to data.
    *ppData = pArray->GetDataPtr();

    return S_OK;
}

/*
 * GetBoxClassLayout
 *
 * Returns information about how a particular value type is laid out.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetBoxClassLayout(ClassID classId,
                                                ULONG32 *pBufferOffset)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetBoxClassLayout 0x%p.\n",
         classId));

    if (pBufferOffset == NULL)
    {
        return E_INVALIDARG;
    }

    if (classId == NULL)
    {
        return E_INVALIDARG;
    }

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);

    //
    // This is the incorrect API for arrays.  Use GetArrayInfo and GetArrayLayout.
    //
    if (!typeHandle.IsValueType())
    {
        return E_INVALIDARG;
    }

    *pBufferOffset = Object::GetOffsetOfFirstField();

    return S_OK;
}

/*
 * GetThreadAppDomain
 *
 * Returns the app domain currently associated with the given thread.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetThreadAppDomain(ThreadID threadId,
                                                  AppDomainID *pAppDomainId)

{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetThreadAppDomain 0x%p.\n",
         threadId));

    if (pAppDomainId == NULL)
    {
        return E_INVALIDARG;
    }

    Thread *pThread;

    if (threadId == NULL)
    {
        pThread = GetThreadNULLOk();
    }
    else
    {
        pThread = (Thread *)threadId;
    }

    //
    // If pThread is null, then the thread has never run managed code and
    // so has no ThreadID.
    //
    if (!IsManagedThread(pThread))
    {
        return CORPROF_E_NOT_MANAGED_THREAD;
    }

    *pAppDomainId = (AppDomainID)pThread->GetDomain();

    return S_OK;
}


/*
 * GetRVAStaticAddress
 *
 * This function returns the absolute address of the given field in the given
 * class.  The field must be an RVA Static token.
 *
 * Parameters:
 *    classId - the containing class.
 *    fieldToken - the field we are querying.
 *    pAddress - location for storing the resulting address location.
 *
 * Returns:
 *    S_OK on success,
 *    E_INVALIDARG if not an RVA static,
 *    CORPROF_E_DATAINCOMPLETE if not yet initialized.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetRVAStaticAddress(ClassID classId,
                                                   mdFieldDef fieldToken,
                                                   void **ppAddress)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // FieldDesc::GetStaticAddress takes a lock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetRVAStaticAddress 0x%p, 0x%08x.\n",
         classId,
         fieldToken));

    //
    // Check for NULL parameters
    //
    if ((classId == NULL) || (ppAddress == NULL))
    {
        return E_INVALIDARG;
    }

    if (GetThreadNULLOk() == NULL)
    {
        return CORPROF_E_NOT_MANAGED_THREAD;
    }

    if (GetAppDomain() == NULL)
    {
        return E_FAIL;
    }

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);

    //
    // If this class is not fully restored, that is all the information we can get at this time.
    //
    if (!typeHandle.IsRestored())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Get the field descriptor object
    //
    FieldDesc *pFieldDesc = typeHandle.GetModule()->LookupFieldDef(fieldToken);

    if (pFieldDesc == NULL)
    {
        return E_INVALIDARG;
    }

    //
    // Verify this field is of the right type
    //
    if(!pFieldDesc->IsStatic() ||
       !pFieldDesc->IsRVA() ||
       pFieldDesc->IsThreadStatic())
    {
        return E_INVALIDARG;
    }

    // It may seem redundant to try to retrieve the same method table from GetEnclosingMethodTable, but classId
    // leads to the instantiated method table while GetEnclosingMethodTable returns the uninstantiated one.
    MethodTable *pMethodTable = pFieldDesc->GetEnclosingMethodTable();

    //
    // Check that the data is available
    //
    if (!IsClassOfMethodTableInited(pMethodTable))
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Store the result and return
    //
    PTR_VOID pAddress = pFieldDesc->GetStaticAddress(NULL);
    if (pAddress == NULL)
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    *ppAddress = pAddress;

    return S_OK;
}


/*
 * GetAppDomainStaticAddress
 *
 * This function returns the absolute address of the given field in the given
 * class in the given app domain.  The field must be an App Domain Static token.
 *
 * Parameters:
 *    classId - the containing class.
 *    fieldToken - the field we are querying.
 *    appDomainId - the app domain container.
 *    pAddress - location for storing the resulting address location.
 *
 * Returns:
 *    S_OK on success,
 *    E_INVALIDARG if not an app domain static,
 *    CORPROF_E_DATAINCOMPLETE if not yet initialized or the module is being unloaded.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetAppDomainStaticAddress(ClassID classId,
                                                         mdFieldDef fieldToken,
                                                         AppDomainID appDomainId,
                                                         void **ppAddress)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // FieldDesc::GetStaticAddress & FieldDesc::GetBase take locks
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetAppDomainStaticAddress 0x%p, 0x%08x, 0x%p.\n",
         classId,
         fieldToken,
         appDomainId));

    //
    // Check for NULL parameters
    //
    if ((classId == NULL) || (appDomainId == NULL) || (ppAddress == NULL))
    {
        return E_INVALIDARG;
    }

    // Some domains, like the system domain, aren't APP domains, and thus don't contain any
    // statics.  See if the profiler is trying to be naughty.
    if (!((BaseDomain*) appDomainId)->IsAppDomain())
    {
        return E_INVALIDARG;
    }

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);

    //
    // If this class is not fully restored, that is all the information we can get at this time.
    //
    if (!typeHandle.IsRestored())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // We might have caught a collectible assembly in the middle of being collected
    Module *pModule = typeHandle.GetModule();
    if (pModule->IsCollectible() &&
        (pModule->GetLoaderAllocator() == NULL || pModule->GetLoaderAllocator()->GetExposedObject() == NULL))
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Get the field descriptor object
    //
    FieldDesc *pFieldDesc = typeHandle.GetModule()->LookupFieldDef(fieldToken);

    if (pFieldDesc == NULL)
    {
        //
        // Give specific error code for literals.
        //
        DWORD dwFieldAttrs;
        if (FAILED(typeHandle.GetModule()->GetMDImport()->GetFieldDefProps(fieldToken, &dwFieldAttrs)))
        {
            return E_INVALIDARG;
        }

        if (IsFdLiteral(dwFieldAttrs))
        {
            return CORPROF_E_LITERALS_HAVE_NO_ADDRESS;
        }

        return E_INVALIDARG;
    }

    //
    // Verify this field is of the right type
    //
    if(!pFieldDesc->IsStatic() ||
       pFieldDesc->IsRVA() ||
       pFieldDesc->IsThreadStatic())
    {
        return E_INVALIDARG;
    }

    // It may seem redundant to try to retrieve the same method table from GetEnclosingMethodTable, but classId
    // leads to the instantiated method table while GetEnclosingMethodTable returns the uninstantiated one.
    MethodTable *pMethodTable = pFieldDesc->GetEnclosingMethodTable();
    AppDomain * pAppDomain = (AppDomain *)appDomainId;

    //
    // Check that the data is available
    //
    if (!IsClassOfMethodTableInited(pMethodTable))
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Get the address
    //
    void *base = (void*)pFieldDesc->GetBase();

    if (base == NULL)
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Store the result and return
    //
    PTR_VOID pAddress = pFieldDesc->GetStaticAddress(base);
    if (pAddress == NULL)
    {
        return E_INVALIDARG;
    }

    *ppAddress = pAddress;

    return S_OK;
}

/*
 * GetThreadStaticAddress
 *
 * This function returns the absolute address of the given field in the given
 * class on the given thread.  The field must be an Thread Static token. threadId
 * must be the current thread ID or NULL, which means using curernt thread ID.
 *
 * Parameters:
 *    classId - the containing class.
 *    fieldToken - the field we are querying.
 *    threadId - the thread container, which has to be the current managed thread or
 *               NULL, which means to use the current managed thread.
 *    pAddress - location for storing the resulting address location.
 *
 * Returns:
 *    S_OK on success,
 *    E_INVALIDARG if not a thread static,
 *    CORPROF_E_DATAINCOMPLETE if not yet initialized.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetThreadStaticAddress(ClassID classId,
                                                      mdFieldDef fieldToken,
                                                      ThreadID threadId,
                                                      void **ppAddress)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetThreadStaticAddress 0x%p, 0x%08x, 0x%p.\n",
         classId,
         fieldToken,
         threadId));

    //
    // Verify the value of threadId, which must be the current thread ID or NULL, which means using curernt thread ID.
    //
    if ((threadId != NULL) && (threadId != ((ThreadID)GetThreadNULLOk())))
    {
        return E_INVALIDARG;
    }

    threadId = reinterpret_cast<ThreadID>(GetThreadNULLOk());
    AppDomainID appDomainId = reinterpret_cast<AppDomainID>(GetAppDomain());

    //
    // Check for NULL parameters
    //
    if ((classId == NULL) || (ppAddress == NULL) || !IsManagedThread(threadId) || (appDomainId == NULL))
    {
        return E_INVALIDARG;
    }

    return GetThreadStaticAddress2(classId,
                                   fieldToken,
                                   appDomainId,
                                   threadId,
                                   ppAddress);
}

/*
 * GetThreadStaticAddress2
 *
 * This function returns the absolute address of the given field in the given
 * class on the given thread.  The field must be an Thread Static token.
 *
 * Parameters:
 *    classId - the containing class.
 *    fieldToken - the field we are querying.
 *    appDomainId - the AppDomain container.
 *    threadId - the thread container.
 *    pAddress - location for storing the resulting address location.
 *
 * Returns:
 *    S_OK on success,
 *    E_INVALIDARG if not a thread static,
 *    CORPROF_E_DATAINCOMPLETE if not yet initialized.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetThreadStaticAddress2(ClassID classId,
                                                       mdFieldDef fieldToken,
                                                       AppDomainID appDomainId,
                                                       ThreadID threadId,
                                                       void **ppAddress)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetThreadStaticAddress2 0x%p, 0x%08x, 0x%p, 0x%p.\n",
         classId,
         fieldToken,
         appDomainId,
         threadId));


    if (threadId == NULL)
    {
        if (GetThreadNULLOk() == NULL)
        {
            return CORPROF_E_NOT_MANAGED_THREAD;
        }

        threadId = reinterpret_cast<ThreadID>(GetThreadNULLOk());
    }

    //
    // Check for NULL parameters
    //
    if ((classId == NULL) || (ppAddress == NULL) || !IsManagedThread(threadId) || (appDomainId == NULL))
    {
        return E_INVALIDARG;
    }

    // Some domains, like the system domain, aren't APP domains, and thus don't contain any
    // statics.  See if the profiler is trying to be naughty.
    if (!((BaseDomain*) appDomainId)->IsAppDomain())
    {
        return E_INVALIDARG;
    }

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);

    //
    // If this class is not fully restored, that is all the information we can get at this time.
    //
    if (!typeHandle.IsRestored())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Get the field descriptor object
    //
    FieldDesc *pFieldDesc = typeHandle.GetModule()->LookupFieldDef(fieldToken);

    if (pFieldDesc == NULL)
    {
        return E_INVALIDARG;
    }

    //
    // Verify this field is of the right type
    //
    if(!pFieldDesc->IsStatic() ||
       !pFieldDesc->IsThreadStatic() ||
       pFieldDesc->IsRVA())
    {
        return E_INVALIDARG;
    }

    // It may seem redundant to try to retrieve the same method table from GetEnclosingMethodTable, but classId
    // leads to the instantiated method table while GetEnclosingMethodTable returns the uninstantiated one.
    MethodTable *pMethodTable = pFieldDesc->GetEnclosingMethodTable();

    //
    // Check that the data is available
    //
    if (!IsClassOfMethodTableInited(pMethodTable))
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Store the result and return
    //
    PTR_VOID pAddress = (void *)(((Thread *)threadId)->GetStaticFieldAddrNoCreate(pFieldDesc));
    if (pAddress == NULL)
    {
        return E_INVALIDARG;
    }

    *ppAddress = pAddress;

    return S_OK;
}

/*
 * GetContextStaticAddress
 *
 * This function returns the absolute address of the given field in the given
 * class in the given context.  The field must be an Context Static token.
 *
 * Parameters:
 *    classId - the containing class.
 *    fieldToken - the field we are querying.
 *    contextId - the context container.
 *    pAddress - location for storing the resulting address location.
 *
 * Returns:
 *    E_NOTIMPL
 *
 */
HRESULT ProfToEEInterfaceImpl::GetContextStaticAddress(ClassID classId,
                                                       mdFieldDef fieldToken,
                                                       ContextID contextId,
                                                       void **ppAddress)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetContextStaticAddress 0x%p, 0x%08x, 0x%p.\n",
         classId,
         fieldToken,
         contextId));

    return E_NOTIMPL;
}

/*
 * GetAppDomainsContainingModule
 *
 * This function returns the AppDomains in which the given module has been loaded
 *
 * Parameters:
 *    moduleId - the module with static variables.
 *    cAppDomainIds - the input size of appDomainIds array.
 *    pcAppDomainIds - the output size of appDomainIds array.
 *    appDomainIds - the array to be filled up with AppDomainIDs containing initialized
 *                   static variables from the moduleId's moudle.
 *
 * Returns:
 *    S_OK on success,
 *    E_INVALIDARG for invalid parameters,
 *    CORPROF_E_DATAINCOMPLETE if moduleId's module is not yet initialized.
 *
 */
HRESULT ProfToEEInterfaceImpl::GetAppDomainsContainingModule(ModuleID moduleId,
                                                             ULONG32 cAppDomainIds,
                                                             ULONG32 * pcAppDomainIds,
                                                             AppDomainID appDomainIds[])
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This method iterates over AppDomains, which adds, then releases, a reference on
        // each AppDomain iterated.  This causes locking, and can cause triggering if the
        // AppDomain gets destroyed as a result of the release. (See code:AppDomainIterator::Next
        // and its call to code:AppDomain::Release.)
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // (See comment above GC_TRIGGERS.)
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetAppDomainsContainingModule 0x%p, 0x%08x, 0x%p, 0x%p.\n",
         moduleId,
         cAppDomainIds,
         pcAppDomainIds,
         appDomainIds));


    //
    // Check for NULL parameters
    //
    if ((moduleId == NULL) || ((appDomainIds == NULL) && (cAppDomainIds != 0)) || (pcAppDomainIds == NULL))
    {
        return E_INVALIDARG;
    }

    Module* pModule = reinterpret_cast< Module* >(moduleId);
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // IterateAppDomainContainingModule uses AppDomainIterator, which cannot be called while the current thread
    // is holding the ThreadStore lock.
    if (ThreadStore::HoldingThreadStore())
    {
        return CORPROF_E_UNSUPPORTED_CALL_SEQUENCE;
    }

    IterateAppDomainContainingModule iterateAppDomainContainingModule(pModule, cAppDomainIds, pcAppDomainIds, appDomainIds);

    return iterateAppDomainContainingModule.PopulateArray();
}



/*
 * GetStaticFieldInfo
 *
 * This function returns a bit mask of the type of statics the
 * given field is.
 *
 * Parameters:
 *    classId - the containing class.
 *    fieldToken - the field we are querying.
 *    pFieldInfo - location for storing the resulting bit mask.
 *
 * Returns:
 *    S_OK on success,
 *    E_INVALIDARG if pFieldInfo is NULL
 *
 */
HRESULT ProfToEEInterfaceImpl::GetStaticFieldInfo(ClassID classId,
                                                  mdFieldDef fieldToken,
                                                  COR_PRF_STATIC_TYPE *pFieldInfo)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetStaticFieldInfo 0x%p, 0x%08x.\n",
         classId,
         fieldToken));

    //
    // Check for NULL parameters
    //
    if ((classId == NULL) || (pFieldInfo == NULL))
    {
        return E_INVALIDARG;
    }

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);

    //
    // If this class is not fully restored, that is all the information we can get at this time.
    //
    if (!typeHandle.IsRestored())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Get the field descriptor object
    //
    FieldDesc *pFieldDesc = typeHandle.GetModule()->LookupFieldDef(fieldToken);

    if (pFieldDesc == NULL)
    {
        return E_INVALIDARG;
    }

    *pFieldInfo = COR_PRF_FIELD_NOT_A_STATIC;

    if (pFieldDesc->IsRVA())
    {
        *pFieldInfo = (COR_PRF_STATIC_TYPE)(*pFieldInfo | COR_PRF_FIELD_RVA_STATIC);
    }

    if (pFieldDesc->IsThreadStatic())
    {
        *pFieldInfo = (COR_PRF_STATIC_TYPE)(*pFieldInfo | COR_PRF_FIELD_THREAD_STATIC);
    }

    if ((*pFieldInfo == COR_PRF_FIELD_NOT_A_STATIC) && pFieldDesc->IsStatic())
    {
        *pFieldInfo = (COR_PRF_STATIC_TYPE)(*pFieldInfo | COR_PRF_FIELD_APP_DOMAIN_STATIC);
    }

    return S_OK;
}



/*
 * GetClassIDInfo2
 *
 * This function generalizes GetClassIDInfo for all types, both generic and non-generic.  It returns
 * the module, type token, and an array of instantiation classIDs that were used to instantiate the
 * given classId.
 *
 * Parameters:
 *   classId - The classId (TypeHandle) to query information about.
 *   pParentClassId - The ClassID (TypeHandle) of the parent class.
 *   pModuleId - An optional parameter for returning the module of the class.
 *   pTypeDefToken - An optional parameter for returning the metadata token of the class.
 *   cNumTypeArgs - The count of the size of the array typeArgs
 *   pcNumTypeArgs - Returns the number of elements of typeArgs filled in, or if typeArgs is NULL
 *         the number that would be needed.
 *   typeArgs - An array to store generic type parameters for the class.
 *
 * Returns:
 *   S_OK if successful.
 */
HRESULT ProfToEEInterfaceImpl::GetClassIDInfo2(ClassID classId,
                                            ModuleID *pModuleId,
                                            mdTypeDef *pTypeDefToken,
                                            ClassID *pParentClassId,
                                            ULONG32 cNumTypeArgs,
                                            ULONG32 *pcNumTypeArgs,
                                            ClassID typeArgs[])
{

    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(CheckPointer(pParentClassId, NULL_OK));
        PRECONDITION(CheckPointer(pModuleId, NULL_OK));
        PRECONDITION(CheckPointer(pTypeDefToken,  NULL_OK));
        PRECONDITION(CheckPointer(pcNumTypeArgs, NULL_OK));
        PRECONDITION(CheckPointer(typeArgs, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetClassIDInfo2 0x%p.\n",
        classId));

    //
    // Verify parameters.
    //
    if (classId == NULL)
    {
        return E_INVALIDARG;
    }

    if ((cNumTypeArgs != 0) && (typeArgs == NULL))
    {
        return E_INVALIDARG;
    }

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);

    //
    // If this class is not fully restored, that is all the information we can get at this time.
    //
    if (!typeHandle.IsRestored())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Handle globals which don't have the instances.
    //
    if (classId == PROFILER_GLOBAL_CLASS)
    {
        if (pParentClassId != NULL)
        {
            *pParentClassId = NULL;
        }

        if (pModuleId != NULL)
        {
            *pModuleId = PROFILER_GLOBAL_MODULE;
        }

        if (pTypeDefToken != NULL)
        {
            *pTypeDefToken = mdTokenNil;
        }

        return S_OK;
    }

    //
    // Do not do arrays via this API
    //
    if (typeHandle.IsArray())
    {
        return CORPROF_E_CLASSID_IS_ARRAY;
    }

    if (typeHandle.IsTypeDesc())
    {
        // a typedesc?  We don't know how to
        // deal with those.
        return CORPROF_E_CLASSID_IS_COMPOSITE;
    }

    //
    // Fill in the basic information
    //
    if (pParentClassId != NULL)
    {
        TypeHandle parentTypeHandle = typeHandle.GetParent();
        if (!parentTypeHandle.IsNull())
        {
            *pParentClassId = TypeHandleToClassID(parentTypeHandle);
        }
        else
        {
            *pParentClassId = NULL;
        }
    }

    if (pModuleId != NULL)
    {
        *pModuleId = (ModuleID) typeHandle.GetModule();
        _ASSERTE(*pModuleId != NULL);
    }

    if (pTypeDefToken != NULL)
    {
        *pTypeDefToken = typeHandle.GetCl();
        _ASSERTE(*pTypeDefToken != NULL);
    }

    //
    // See if they are just looking to get the buffer size.
    //
    if (cNumTypeArgs == 0)
    {
        if (pcNumTypeArgs != NULL)
        {
            *pcNumTypeArgs = typeHandle.GetMethodTable()->GetNumGenericArgs();
        }
        return S_OK;
    }

    //
    // Adjust the count for the size of the given array.
    //
    if (cNumTypeArgs > typeHandle.GetMethodTable()->GetNumGenericArgs())
    {
        cNumTypeArgs = typeHandle.GetMethodTable()->GetNumGenericArgs();
    }

    if (pcNumTypeArgs != NULL)
    {
        *pcNumTypeArgs = cNumTypeArgs;
    }

    //
    // Copy over the instantiating types.
    //
    ULONG32 count;
    Instantiation inst = typeHandle.GetMethodTable()->GetInstantiation();

    for (count = 0; count < cNumTypeArgs; count ++)
    {
        typeArgs[count] = TypeHandleToClassID(inst[count]);
    }

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::GetModuleInfo(ModuleID     moduleId,
    LPCBYTE *    ppBaseLoadAddress,
    ULONG        cchName,
    ULONG *      pcchName,
    __out_ecount_part_opt(cchName, *pcchName) WCHAR wszName[],
    AssemblyID * pAssemblyId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // See comment in code:ProfToEEInterfaceImpl::GetModuleInfo2
        CAN_TAKE_LOCK;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;


        PRECONDITION(CheckPointer((Module *)moduleId, NULL_OK));
        PRECONDITION(CheckPointer(ppBaseLoadAddress,  NULL_OK));
        PRECONDITION(CheckPointer(pcchName, NULL_OK));
        PRECONDITION(CheckPointer(wszName, NULL_OK));
        PRECONDITION(CheckPointer(pAssemblyId, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetModuleInfo 0x%p.\n",
        moduleId));

    // Paramter validation is taken care of in GetModuleInfo2.

    return GetModuleInfo2(
        moduleId,
        ppBaseLoadAddress,
        cchName,
        pcchName,
        wszName,
        pAssemblyId,
        NULL);          // Don't need module type
}

//---------------------------------------------------------------------------------------
//
// Helper used by GetModuleInfo2 to determine the bitmask of COR_PRF_MODULE_FLAGS for
// the specified module.
//
// Arguments:
//      pModule - Module to get the flags for
//
// Return Value:
//      Bitmask of COR_PRF_MODULE_FLAGS corresponding to pModule
//

DWORD ProfToEEInterfaceImpl::GetModuleFlags(Module * pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        MODE_ANY;
    }
    CONTRACTL_END;

    PEFile * pPEFile = pModule->GetFile();
    if (pPEFile == NULL)
    {
        // Hopefully this should never happen; but just in case, don't try to determine the
        // flags without a PEFile.
        return 0;
    }

    DWORD dwRet = 0;

    // First, set the flags that are dependent on which PEImage / layout we look at
    // inside the Module (disk/flat)
#ifdef FEATURE_READYTORUN
    if (pModule->IsReadyToRun())
    {
        // Ready To Run
        dwRet |= (COR_PRF_MODULE_DISK | COR_PRF_MODULE_NGEN);
    }
#endif
    // Not NGEN or ReadyToRun.
    if (pPEFile->HasOpenedILimage())
    {
        PEImage * pILImage = pPEFile->GetOpenedILimage();
        if (pILImage->IsFile())
        {
            dwRet |= COR_PRF_MODULE_DISK;
        }
        if (pPEFile->GetLoadedIL()->IsFlat())
        {
            dwRet |= COR_PRF_MODULE_FLAT_LAYOUT;
        }
    }

    if (pModule->IsReflection())
    {
        dwRet |= COR_PRF_MODULE_DYNAMIC;
    }

    if (pModule->IsCollectible())
    {
        dwRet |= COR_PRF_MODULE_COLLECTIBLE;
    }

    if (pModule->IsResource())
    {
        dwRet |= COR_PRF_MODULE_RESOURCE;
    }

    return dwRet;
}

HRESULT ProfToEEInterfaceImpl::GetModuleInfo2(ModuleID     moduleId,
    LPCBYTE *    ppBaseLoadAddress,
    ULONG        cchName,
    ULONG *      pcchName,
    __out_ecount_part_opt(cchName, *pcchName) WCHAR wszName[],
    AssemblyID * pAssemblyId,
    DWORD *      pdwModuleFlags)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // The pModule->GetScopeName() call below can result in locks getting taken to
        // access the metadata implementation.  However, these locks do not do a mode
        // change.
        CAN_TAKE_LOCK;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;


        PRECONDITION(CheckPointer((Module *)moduleId, NULL_OK));
        PRECONDITION(CheckPointer(ppBaseLoadAddress,  NULL_OK));
        PRECONDITION(CheckPointer(pcchName, NULL_OK));
        PRECONDITION(CheckPointer(wszName, NULL_OK));
        PRECONDITION(CheckPointer(pAssemblyId, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetModuleInfo2 0x%p.\n",
        moduleId));

    if (moduleId == NULL)
    {
        return E_INVALIDARG;
    }

    Module * pModule = (Module *) moduleId;
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    HRESULT     hr = S_OK;

    EX_TRY
    {

        PEFile * pFile = pModule->GetFile();

        // Pick some safe defaults to begin with.
        if (ppBaseLoadAddress != NULL)
            *ppBaseLoadAddress = 0;
        if (wszName != NULL)
            *wszName = 0;
        if (pcchName != NULL)
            *pcchName = 0;
        if (pAssemblyId != NULL)
            *pAssemblyId = PROFILER_PARENT_UNKNOWN;

        // Module flags can be determined first without fear of error
        if (pdwModuleFlags != NULL)
            *pdwModuleFlags = GetModuleFlags(pModule);

        // Get the module file name
        LPCWSTR wszFileName = pFile->GetPath();
        _ASSERTE(wszFileName != NULL);
        PREFIX_ASSUME(wszFileName != NULL);

        // If there is no filename, which is the case for RefEmit modules and for SQL
        // modules, then rather than returning an empty string for the name, just use the
        // module name from metadata (a.k.a. Module.ScopeName). This is required to
        // support SQL F1 sampling profiling.
        StackSString strScopeName;
        LPCUTF8 szScopeName = NULL;
        if ((*wszFileName == W('\0')) && SUCCEEDED(pModule->GetScopeName(&szScopeName)))
        {
            strScopeName.SetUTF8(szScopeName);
            strScopeName.Normalize();
            wszFileName = strScopeName.GetUnicode();
        }

        ULONG trueLen = (ULONG)(wcslen(wszFileName) + 1);

        // Return name of module as required.
        if (wszName && cchName > 0)
        {
            if (cchName < trueLen)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            }
            else
            {
                wcsncpy_s(wszName, cchName, wszFileName, trueLen);
            }
        }

        // If they request the actual length of the name
        if (pcchName)
            *pcchName = trueLen;

        if (ppBaseLoadAddress != NULL && !pFile->IsDynamic())
        {
            if (pModule->IsProfilerNotified())
            {
                // Set the base load address -- this could be null in certain error conditions
                *ppBaseLoadAddress = pModule->GetProfilerBase();
            }
            else
            {
                *ppBaseLoadAddress = NULL;
            }

            if (*ppBaseLoadAddress == NULL)
            {
                hr = CORPROF_E_DATAINCOMPLETE;
            }
        }

        // Return the parent assembly for this module if desired.
        if (pAssemblyId != NULL)
        {
            // Lie and say the assembly isn't available until we are loaded (even though it is.)
            // This is for backward compatibilty - we may want to change it
            if (pModule->IsProfilerNotified())
            {
                Assembly *pAssembly = pModule->GetAssembly();
                _ASSERTE(pAssembly);

                *pAssemblyId = (AssemblyID) pAssembly;
            }
            else
            {
                hr = CORPROF_E_DATAINCOMPLETE;
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return (hr);
}


/*
 * Get a metadata interface instance which maps to the given module.
 * One may ask for the metadata to be opened in read+write mode, but
 * this will result in slower metadata execution of the program, because
 * changes made to the metadata cannot be optimized as they were from
 * the compiler.
 */
HRESULT ProfToEEInterfaceImpl::GetModuleMetaData(ModuleID    moduleId,
    DWORD       dwOpenFlags,
    REFIID      riid,
    IUnknown    **ppOut)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Currently, this function is technically EE_THREAD_REQUIRED because
        // some functions in synch.cpp assert that there is a Thread object,
        // but we might be able to lift that restriction and make this be
        // EE_THREAD_NOT_REQUIRED.

        // PEFile::GetRWImporter & PEFile::GetEmitter &
        // GetReadablePublicMetaDataInterface take locks
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetModuleMetaData 0x%p, 0x%08x.\n",
        moduleId,
        dwOpenFlags));

    if (moduleId == NULL)
    {
        return E_INVALIDARG;
    }

    // Check for unsupported bits, and return E_INVALIDARG if present
    if ((dwOpenFlags & ~(ofNoTransform | ofRead | ofWrite)) != 0)
    {
        return E_INVALIDARG;
    }

    Module * pModule;
    HRESULT hr = S_OK;

    pModule = (Module *) moduleId;
    _ASSERTE(pModule != NULL);
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // Make sure we can get the importer first
    if (pModule->IsResource())
    {
        if (ppOut)
            *ppOut = NULL;
        return S_FALSE;
    }

    // Decide which type of open mode we are in to see which you require.
    if ((dwOpenFlags & ofWrite) == 0)
    {
        // Readable interface
        return pModule->GetReadablePublicMetaDataInterface(dwOpenFlags, riid, (LPVOID *) ppOut);
    }

    // Writeable interface
    IUnknown *pObj = NULL;
    EX_TRY
    {
        pObj = pModule->GetEmitter();
    }
    EX_CATCH_HRESULT_NO_ERRORINFO(hr);

    // Ask for the interface the caller wanted, only if they provide a out param
    if (SUCCEEDED(hr) && ppOut)
        hr = pObj->QueryInterface(riid, (void **) ppOut);

    return (hr);
}


/*
 * Retrieve a pointer to the body of a method starting at it's header.
 * A method is scoped by the module it lives in.  Because this function
 * is designed to give a tool access to IL before it has been loaded
 * by the Runtime, it uses the metadata token of the method to find
 * the instance desired.  Note that this function has no effect on
 * already compiled code.
 */
HRESULT ProfToEEInterfaceImpl::GetILFunctionBody(ModuleID    moduleId,
    mdMethodDef methodId,
    LPCBYTE     *ppMethodHeader,
    ULONG       *pcbMethodSize)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // PEFile::CheckLoaded & Module::GetDynamicIL both take a lock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;


    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(
        kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetILFunctionBody 0x%p, 0x%08x.\n",
         moduleId,
         methodId));

    Module *    pModule;                // Working pointer for real class.
    ULONG       RVA;                    // Return RVA of the method body.
    DWORD       dwImplFlags;            // Flags for the item.

    if ((moduleId == NULL) ||
        (methodId == mdMethodDefNil) ||
        (methodId == 0) ||
        (TypeFromToken(methodId) != mdtMethodDef))
    {
        return E_INVALIDARG;
    }

    pModule = (Module *) moduleId;
    _ASSERTE(pModule != NULL && methodId != mdMethodDefNil);
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // Find the method body based on metadata.
    IMDInternalImport *pImport = pModule->GetMDImport();
    _ASSERTE(pImport);

    PEFile *pFile = pModule->GetFile();

    if (!pFile->CheckLoaded())
        return (CORPROF_E_DATAINCOMPLETE);

    LPCBYTE pbMethod = NULL;

    // Don't return rewritten IL, use the new API to get that.
    pbMethod = (LPCBYTE) pModule->GetDynamicIL(methodId, FALSE);

    // Method not overriden - get the original copy of the IL by going to metadata
    if (pbMethod == NULL)
    {
        HRESULT hr = S_OK;
        IfFailRet(pImport->GetMethodImplProps(methodId, &RVA, &dwImplFlags));

        // Check to see if the method has associated IL
        if ((RVA == 0 && !pFile->IsDynamic()) || !(IsMiIL(dwImplFlags) || IsMiOPTIL(dwImplFlags) || IsMiInternalCall(dwImplFlags)))
        {
            return (CORPROF_E_FUNCTION_NOT_IL);
        }

        EX_TRY
        {
            // Get the location of the IL
            pbMethod = (LPCBYTE) (pModule->GetIL(RVA));
        }
        EX_CATCH_HRESULT(hr);

        if (FAILED(hr))
        {
            return hr;
        }
    }

    // Fill out param if provided
    if (ppMethodHeader)
        *ppMethodHeader = pbMethod;

    // Calculate the size of the method itself.
    if (pcbMethodSize)
    {
        if (!FitsIn<ULONG>(PEDecoder::ComputeILMethodSize((TADDR)pbMethod)))
        {
            return E_UNEXPECTED;
        }
        *pcbMethodSize = static_cast<ULONG>(PEDecoder::ComputeILMethodSize((TADDR)pbMethod));
    }
    return (S_OK);
}

//---------------------------------------------------------------------------------------
// Retrieves an IMethodMalloc pointer around a ModuleILHeap instance that will own
// allocating heap space for this module (for IL rewriting).
//
// Arguments:
//      moduleId - ModuleID this allocator shall allocate for
//      ppMalloc - [out] IMethodMalloc pointer the profiler will use for allocation requests
//                       against this module
//
// Return value
//      HRESULT indicating success / failure
//
// Notes
//        IL method bodies used to have the requirement that they must be referenced as
//        RVA's to the loaded module, which means they come after the module within
//        METHOD_MAX_RVA.  In order to make it easier for a tool to swap out the body of
//        a method, this allocator will ensure memory allocated after that point.
//
//        Now that requirement is completely gone, so there's nothing terribly special
//        about this allocator, we just keep it around for legacy purposes.

HRESULT ProfToEEInterfaceImpl::GetILFunctionBodyAllocator(ModuleID         moduleId,
                                                          IMethodMalloc ** ppMalloc)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // ModuleILHeap::FindOrCreateHeap may take a Crst if it
        // needs to create a new allocator and add it to the list.  Taking a crst
        // switches to preemptive, which is effectively a GC trigger
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // (see GC_TRIGGERS comment)
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetILFunctionBodyAllocator 0x%p.\n",
        moduleId));

    if ((moduleId == NULL) || (ppMalloc == NULL))
    {
        return E_INVALIDARG;
    }

    Module * pModule = (Module *) moduleId;

    if (pModule->IsBeingUnloaded() ||
        !pModule->GetFile()->CheckLoaded())
    {
        return (CORPROF_E_DATAINCOMPLETE);
    }

    *ppMalloc = &ModuleILHeap::s_Heap;
    return S_OK;
}

/*
 * Replaces the method body for a function in a module.  This will replace
 * the RVA of the method in the metadata to point to this new method body,
 * and adjust any internal data structures as required.  This function can
 * only be called on those methods which have never been compiled by a JITTER.
 * Please use the GetILFunctionBodyAllocator to allocate space for the new method to
 * ensure the buffer is compatible.
 */
HRESULT ProfToEEInterfaceImpl::SetILFunctionBody(ModuleID    moduleId,
    mdMethodDef methodId,
    LPCBYTE     pbNewILMethodHeader)
{
    CONTRACTL
    {
        // PEFile::GetEmitter, Module::SetDynamicIL all throw
        THROWS;

        // Locks are taken (see CAN_TAKE_LOCK below), which may cause mode switch to
        // preemptive, which is triggers.
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // Module::SetDynamicIL & PEFile::CheckLoaded & PEFile::GetEmitter take locks
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: SetILFunctionBody 0x%p, 0x%08x.\n",
         moduleId,
         methodId));

    if ((moduleId == NULL) ||
        (methodId == mdMethodDefNil) ||
        (TypeFromToken(methodId) != mdtMethodDef) ||
        (pbNewILMethodHeader == NULL))
    {
        return E_INVALIDARG;
    }

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    Module      *pModule;               // Working pointer for real class.
    HRESULT     hr = S_OK;

    // Cannot set the body for anything other than a method def
    if (TypeFromToken(methodId) != mdtMethodDef)
        return (E_INVALIDARG);

    // Cast module to appropriate type
    pModule = (Module *) moduleId;
    _ASSERTE (pModule != NULL); // Enforced in CorProfInfo::SetILFunctionBody
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // Remember the profiler is doing this, as that means we must never detach it!
    g_profControlBlock.mainProfilerInfo.pProfInterface->SetUnrevertiblyModifiedILFlag();

    // This action is not temporary!
    // If the profiler want to be able to revert, they need to use
    // the new ReJIT APIs.
    pModule->SetDynamicIL(methodId, (TADDR)pbNewILMethodHeader, FALSE);

    return (hr);
}

/*
 * Sets the codemap for the replaced IL function body
 */
HRESULT ProfToEEInterfaceImpl::SetILInstrumentedCodeMap(FunctionID functionId,
        BOOL fStartJit,
        ULONG cILMapEntries,
        COR_IL_MAP rgILMapEntries[])
{
    CONTRACTL
    {
        // Debugger::SetILInstrumentedCodeMap throws
        THROWS;

        // Debugger::SetILInstrumentedCodeMap triggers
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Debugger::SetILInstrumentedCodeMap takes a lock when it calls Debugger::GetOrCreateMethodInfo
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: SetILInstrumentedCodeMap 0x%p, %d.\n",
         functionId,
         fStartJit));

    if (functionId == NULL)
    {
        return E_INVALIDARG;
    }

    if (cILMapEntries >= (MAXULONG / sizeof(COR_IL_MAP)))
    {
        // Too big!  The allocation below would overflow when calculating the size.
        return E_INVALIDARG;
    }


#ifdef DEBUGGING_SUPPORTED

    if (g_pDebugInterface == NULL)
    {
        return CORPROF_E_DEBUGGING_DISABLED;
    }

    COR_IL_MAP * rgNewILMapEntries = new (nothrow) COR_IL_MAP[cILMapEntries];

    if (rgNewILMapEntries == NULL)
        return E_OUTOFMEMORY;

    memcpy_s(rgNewILMapEntries, sizeof(COR_IL_MAP) * cILMapEntries, rgILMapEntries, sizeof(COR_IL_MAP) * cILMapEntries);

    MethodDesc *pMethodDesc = FunctionIdToMethodDesc(functionId);
    return g_pDebugInterface->SetILInstrumentedCodeMap(pMethodDesc,
                                                       fStartJit,
                                                       cILMapEntries,
                                                       rgNewILMapEntries);

#else //DEBUGGING_SUPPORTED
    return E_NOTIMPL;
#endif //DEBUGGING_SUPPORTED
}

HRESULT ProfToEEInterfaceImpl::ForceGC()
{
    CONTRACTL
    {
        // GC calls "new" which throws
        THROWS;

        // Uh duh, look at the name of the function, dude
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Initiating a GC causes a runtime suspension which requires the
        // mother of all locks: the thread store lock.
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    ASSERT_NO_EE_LOCKS_HELD();

    // We need to use IsGarbageCollectorFullyInitialized() instead of IsGCHeapInitialized() because
    // there are other GC initialization being done after IsGCHeapInitialized() becomes TRUE,
    // and before IsGarbageCollectorFullyInitialized() becomes TRUE.
    if (!IsGarbageCollectorFullyInitialized())
    {
        return CORPROF_E_NOT_YET_AVAILABLE;
    }

    // Disallow the cases where a profiler calls this off a hijacked CLR thread
    // or inside a profiler callback.  (Allow cases where this is a native thread, or a
    // thread which previously successfully called ForceGC.)
    Thread * pThread = GetThreadNULLOk();
    if ((pThread != NULL) &&
            (!AreCallbackStateFlagsSet(COR_PRF_CALLBACKSTATE_FORCEGC_WAS_CALLED)) &&
            (pThread->GetFrame() != FRAME_TOP
            || AreCallbackStateFlagsSet(COR_PRF_CALLBACKSTATE_INCALLBACK)))
    {
        LOG((LF_CORPROF,
             LL_ERROR,
             "**PROF: ERROR: Returning CORPROF_E_UNSUPPORTED_CALL_SEQUENCE "
             "due to illegal hijacked profiler call or call from inside another callback\n"));
        return CORPROF_E_UNSUPPORTED_CALL_SEQUENCE;
    }

    // NOTE: We cannot use the standard macro PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX
    // here because the macro ensures that either the current thread is not an
    // EE thread, or, if it is, that the CALLBACK flag is set. In classic apps
    // a profiler-owned native thread will not get an EE thread associated with
    // it, however, in AppX apps, during the first call into the GC on a
    // profiler-owned thread, the EE will associate an EE-thread with the profiler
    // thread. As a consequence the second call to ForceGC on the same thread
    // would fail, since this is now an EE thread and this API is not called from
    // a callback.

    // First part of the PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX macro:
    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: ForceGC.\n"));

#ifdef FEATURE_EVENT_TRACE
    // This helper, used by ETW and profAPI ensures a managed thread gets created for
    // this thread before forcing the GC.
    HRESULT hr = ETW::GCLog::ForceGCForDiagnostics();
#else // !FEATURE_EVENT_TRACE
    HRESULT hr = E_FAIL;
#endif // FEATURE_EVENT_TRACE

    // If a Thread object was just created for this thread, remember the fact that it
    // was a ForceGC() thread, so we can be more lenient when doing
    // COR_PRF_CALLBACKSTATE_INCALLBACK later on from other APIs
    pThread = GetThreadNULLOk();
    if (pThread != NULL)
    {
        pThread->SetProfilerCallbackStateFlags(COR_PRF_CALLBACKSTATE_FORCEGC_WAS_CALLED);
    }

    return hr;
}


/*
 * Returns the ContextID for the current thread.
 */
HRESULT ProfToEEInterfaceImpl::GetThreadContext(ThreadID threadId,
                                                ContextID *pContextId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetThreadContext 0x%p.\n",
         threadId));

    if (!IsManagedThread(threadId))
    {
        return E_INVALIDARG;
    }

    // Cast to right type
    Thread *pThread = reinterpret_cast<Thread *>(threadId);

    // Get the context for the Thread* provided
    AppDomain *pContext = pThread->GetDomain(); // Context is same as AppDomain in CoreCLR
    _ASSERTE(pContext);

    // If there's no current context, return incomplete info
    if (!pContext)
        return (CORPROF_E_DATAINCOMPLETE);

    // Set the result and return
    if (pContextId)
        *pContextId = reinterpret_cast<ContextID>(pContext);

    return (S_OK);
}

HRESULT ProfToEEInterfaceImpl::GetClassIDInfo(ClassID classId,
    ModuleID *pModuleId,
    mdTypeDef *pTypeDefToken)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetClassIDInfo 0x%p.\n",
        classId));

    if (classId == NULL)
    {
        return E_INVALIDARG;
    }

    if (pModuleId != NULL)
    {
        *pModuleId = NULL;
    }

    if (pTypeDefToken != NULL)
    {
        *pTypeDefToken = NULL;
    }

    // Handle globals which don't have the instances.
    if (classId == PROFILER_GLOBAL_CLASS)
    {
        if (pModuleId != NULL)
        {
            *pModuleId = PROFILER_GLOBAL_MODULE;
        }

        if (pTypeDefToken != NULL)
        {
            *pTypeDefToken = mdTokenNil;
    }
    }
    else if (classId == NULL)
    {
        return E_INVALIDARG;
    }
    // Get specific data.
    else
    {
        TypeHandle th = TypeHandle::FromPtr((void *)classId);

        if (!th.IsTypeDesc())
        {
            if (!th.IsArray())
            {
                //
                // If this class is not fully restored, that is all the information we can get at this time.
                //
                if (!th.IsRestored())
                {
                    return CORPROF_E_DATAINCOMPLETE;
                }

                if (pModuleId != NULL)
                {
                    *pModuleId = (ModuleID) th.GetModule();
                    _ASSERTE(*pModuleId != NULL);
                }

                if (pTypeDefToken != NULL)
                {
                    *pTypeDefToken = th.GetCl();
                    _ASSERTE(*pTypeDefToken != NULL);
                }
            }
        }
    }

    return (S_OK);
}


HRESULT ProfToEEInterfaceImpl::GetFunctionInfo(FunctionID functionId,
    ClassID *pClassId,
    ModuleID *pModuleId,
    mdToken *pToken)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetFunctionInfo 0x%p.\n",
        functionId));

    if (functionId == NULL)
    {
        return E_INVALIDARG;
    }

    MethodDesc *pMDesc = (MethodDesc *) functionId;
    MethodTable *pMT = pMDesc->GetMethodTable();
    if (!pMT->IsRestored())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    ClassID classId = PROFILER_GLOBAL_CLASS;

    if (pMT != NULL)
    {
        classId = NonGenericTypeHandleToClassID(TypeHandle(pMT));
    }

    if (pClassId != NULL)
    {
        *pClassId = classId;
    }

    if (pModuleId != NULL)
    {
        *pModuleId = (ModuleID) pMDesc->GetModule();
    }

    if (pToken != NULL)
    {
        *pToken = pMDesc->GetMemberDef();
    }

    return (S_OK);
}

/*
 * GetILToNativeMapping returns a map from IL offsets to native
 * offsets for this code. An array of COR_DEBUG_IL_TO_NATIVE_MAP
 * structs will be returned, and some of the ilOffsets in this array
 * may be the values specified in CorDebugIlToNativeMappingTypes.
 */
HRESULT ProfToEEInterfaceImpl::GetILToNativeMapping(FunctionID functionId,
                                                    ULONG32 cMap,
                                                    ULONG32 * pcMap,    // [out]
                                                    COR_DEBUG_IL_TO_NATIVE_MAP map[]) // [out]
{
    CONTRACTL
    {
        // MethodDesc::FindOrCreateTypicalSharedInstantiation throws
        THROWS;

        // MethodDesc::FindOrCreateTypicalSharedInstantiation triggers, but shouldn't trigger when
        // called from here.  Since the profiler has a valid functionId, the methoddesc for
        // this code will already have been created.  We should be able to enforce this by
        // passing allowCreate=FALSE  to FindOrCreateTypicalSharedInstantiation.
        DISABLED(GC_NOTRIGGER);

        // Yay!
        MODE_ANY;

        // The call to g_pDebugInterface->GetILToNativeMapping() below may call
        // Debugger::AcquireDebuggerLock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetILToNativeMapping 0x%p.\n",
        functionId));

    return GetILToNativeMapping2(functionId, 0, cMap, pcMap, map);
}

HRESULT ProfToEEInterfaceImpl::GetILToNativeMapping2(FunctionID functionId,
                                                    ReJITID reJitId,
                                                    ULONG32 cMap,
                                                    ULONG32 * pcMap,    // [out]
                                                    COR_DEBUG_IL_TO_NATIVE_MAP map[]) // [out]
{
    CONTRACTL
    {
        // MethodDesc::FindOrCreateTypicalSharedInstantiation throws
        THROWS;

        // MethodDesc::FindOrCreateTypicalSharedInstantiation triggers, but shouldn't trigger when
        // called from here.  Since the profiler has a valid functionId, the methoddesc for
        // this code will already have been created.  We should be able to enforce this by
        // passing allowCreate=FALSE  to FindOrCreateTypicalSharedInstantiation.
        DISABLED(GC_NOTRIGGER);

        // Yay!
        MODE_ANY;

        // The call to g_pDebugInterface->GetILToNativeMapping() below may call
        // Debugger::AcquireDebuggerLock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetILToNativeMapping2 0x%p 0x%p.\n",
        functionId, reJitId));

    if (functionId == NULL)
    {
        return E_INVALIDARG;
    }

    if ((cMap > 0) &&
        ((pcMap == NULL) || (map == NULL)))
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    EX_TRY
    {
        // Cast to proper type
        MethodDesc * pMD = FunctionIdToMethodDesc(functionId);

        if (pMD->HasClassOrMethodInstantiation() && pMD->IsTypicalMethodDefinition())
        {
            // In this case, we used to replace pMD with its canonical instantiation
            // (FindOrCreateTypicalSharedInstantiation).  However, a profiler should never be able
            // to get to this point anyway, since any MethodDesc a profiler gets from us
            // cannot be typical (i.e., cannot be a generic with types still left uninstantiated).
            // We assert here just in case a test proves me wrong, but generally we will
            // disallow this code path.
            _ASSERTE(!"Profiler passed a typical method desc (a generic with types still left uninstantiated) to GetILToNativeMapping2");
            hr = E_INVALIDARG;
        }
        else
        {
            PCODE pCodeStart = NULL;
            CodeVersionManager *pCodeVersionManager = pMD->GetCodeVersionManager();
            ILCodeVersion ilCodeVersion = NULL;
            {
                CodeVersionManager::LockHolder codeVersioningLockHolder;

                pCodeVersionManager->GetILCodeVersion(pMD, reJitId);

                NativeCodeVersionCollection nativeCodeVersions = ilCodeVersion.GetNativeCodeVersions(pMD);
                for (NativeCodeVersionIterator iter = nativeCodeVersions.Begin(); iter != nativeCodeVersions.End(); iter++)
                {
                    // Now that tiered compilation can create more than one jitted code version for the same rejit id
                    // we are arbitrarily choosing the first one to return.  To address a specific version of native code
                    // use GetILToNativeMapping3.
                    pCodeStart = iter->GetNativeCode();
                    break;
                }
            }

            hr = GetILToNativeMapping3(pCodeStart, cMap, pcMap, map);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}



//*****************************************************************************
// Given an ObjectID, go get the EE ClassID for it.
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::GetClassFromObject(ObjectID objectId,
                                                  ClassID * pClassId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!  Fail at runtime if in preemptive mode via AllowObjectInspection()
        MODE_ANY;

        // Object::GetTypeHandle takes a lock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetClassFromObject 0x%p.\n",
         objectId));

    if (objectId == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = AllowObjectInspection();
    if (FAILED(hr))
    {
        return hr;
    }

    // Cast the ObjectID as a Object
    Object *pObj = reinterpret_cast<Object *>(objectId);

    // Set the out param and indicate success
    // Note that for generic code we always return uninstantiated ClassIDs and FunctionIDs
    if (pClassId)
    {
        *pClassId = SafeGetClassIDFromObject(pObj);
    }

    return S_OK;
}

//*****************************************************************************
// Given a module and a token for a class, go get the EE data structure for it.
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::GetClassFromToken(ModuleID    moduleId,
    mdTypeDef   typeDef,
    ClassID     *pClassId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // ClassLoader::LoadTypeDefOrRefThrowing triggers
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // ClassLoader::LoadTypeDefOrRefThrowing takes a lock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetClassFromToken 0x%p, 0x%08x.\n",
         moduleId,
         typeDef));

    if ((moduleId == NULL) || (typeDef == mdTypeDefNil) || (typeDef == NULL))
    {
        return E_INVALIDARG;
    }

    if (!g_profControlBlock.fBaseSystemClassesLoaded)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;
    }

    // Get the module
    Module *pModule = (Module *) moduleId;

    // No module, or it's disassociated from metadata
    if ((pModule == NULL) || (pModule->IsBeingUnloaded()))
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // First, check the RID map. This is important since it
    // works during teardown (and the below doesn't)
    TypeHandle th;
    th = pModule->LookupTypeDef(typeDef);
    if (th.IsNull())
    {
        HRESULT hr = S_OK;

        EX_TRY {
            th = ClassLoader::LoadTypeDefOrRefThrowing(pModule, typeDef,
                                                      ClassLoader::ThrowIfNotFound,
                                                      ClassLoader::PermitUninstDefOrRef);
        }
        EX_CATCH_HRESULT(hr);

        if (FAILED(hr))
        {
            return hr;
        }
    }

    if (!th.GetMethodTable())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //
    // Check if it is generic
    //
    ClassID classId = NonGenericTypeHandleToClassID(th);

    if (classId == NULL)
    {
        return CORPROF_E_TYPE_IS_PARAMETERIZED;
    }

    // Return value if necessary
    if (pClassId)
    {
        *pClassId = classId;
    }

    return S_OK;
}


HRESULT ProfToEEInterfaceImpl::GetClassFromTokenAndTypeArgs(ModuleID moduleID,
                                                            mdTypeDef typeDef,
                                                            ULONG32 cTypeArgs,
                                                            ClassID typeArgs[],
                                                            ClassID* pClassID)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // LoadGenericInstantiationThrowing may load
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // ClassLoader::LoadGenericInstantiationThrowing takes a lock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetClassFromTokenAndTypeArgs 0x%p, 0x%08x.\n",
         moduleID,
         typeDef));

    if (!g_profControlBlock.fBaseSystemClassesLoaded)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;
    }

    Module* pModule = reinterpret_cast< Module* >(moduleID);

    if (pModule == NULL || pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // This array needs to be accessible at least until the call to
    // ClassLoader::LoadGenericInstantiationThrowing
    TypeHandle* genericParameters = new (nothrow) TypeHandle[cTypeArgs];
    NewArrayHolder< TypeHandle > holder(genericParameters);

    if (NULL == genericParameters)
    {
        return E_OUTOFMEMORY;
    }

    for (ULONG32 i = 0; i < cTypeArgs; ++i)
    {
        genericParameters[i] = TypeHandle(reinterpret_cast< MethodTable* >(typeArgs[i]));
    }

    //
    // nickbe 11/24/2003 10:12:56
    //
    // In RTM/Everett we decided to load the class if it hadn't been loaded yet
    // (see ProfToEEInterfaceImpl::GetClassFromToken). For compatibility we're
    // going to make the same decision here. It's potentially confusing to tell
    // someone a type doesn't exist at one point in time, but does exist later,
    // and there is no good way for us to determing that a class may eventually
    // be loaded without going ahead and loading it
    //
    TypeHandle th;
    HRESULT hr = S_OK;

    EX_TRY
    {
        // Not sure if this is a valid override or not - making this a VIOLATION
        // until we're sure.
        CONTRACT_VIOLATION(LoadsTypeViolation);

        if (GetThreadNULLOk() == NULL)
        {
            // Type system will try to validate as part of its contract if the current
            // AppDomain returned by GetAppDomain can load types in specified module's
            // assembly.   On a non-EE thread it results in an AV in a check build
            // since the type system tries to dereference NULL returned by GetAppDomain.
            // More importantly, loading a type on a non-EE thread is not allowed.
            //
            // ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE() states that callers will not
            // try to load a type, so that type system will not try to test type
            // loadability in the current AppDomain.  However,
            // ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE does not prevent callers from
            // loading a type.   It is profiler's responsibility not to attempt to load
            // a type in unsupported ways (e.g. from a non-EE thread).  It doesn't
            // impact retail builds, in which contracts are not available.
            ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

            // ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE also defines FAULT_FORBID, which
            // causes Scanruntime to flag a fault violation in AssemblySpec::InitializeSpec,
            // which is defined as FAULTS.   It only happens in a type-loading path, which
            // is not supported on a non-EE thread.  Suppressing a contract violation in an
            // unsupported execution path is more preferable than causing AV when calling
            // GetClassFromTokenAndTypeArgs on a non-EE thread in a check build.  See Dev10
            // 682526 for more details.
            FAULT_NOT_FATAL();

            th = ClassLoader::LoadGenericInstantiationThrowing(pModule,
                                                               typeDef,
                                                               Instantiation(genericParameters, cTypeArgs),
                                                               ClassLoader::LoadTypes);
        }
        else
        {
            th = ClassLoader::LoadGenericInstantiationThrowing(pModule,
                                                               typeDef,
                                                               Instantiation(genericParameters, cTypeArgs),
                                                               ClassLoader::LoadTypes);
        }
    }
    EX_CATCH_HRESULT(hr);

    if (FAILED(hr))
    {
        return hr;
    }

    if (th.IsNull())
    {
        // Hmm, the type isn't loaded yet.
        return CORPROF_E_DATAINCOMPLETE;
    }

    *pClassID = TypeHandleToClassID(th);

    return S_OK;
}

//*****************************************************************************
// Given the token for a method, return the fucntion id.
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::GetFunctionFromToken(ModuleID moduleId,
    mdToken typeDef,
    FunctionID *pFunctionId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetFunctionFromToken 0x%p, 0x%08x.\n",
         moduleId,
         typeDef));

    if ((moduleId == NULL) || (typeDef == mdTokenNil))
    {
        return E_INVALIDARG;
    }

    if (!g_profControlBlock.fBaseSystemClassesLoaded)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;
    }

    // Default HRESULT
    HRESULT hr = S_OK;

    // Get the module
    Module *pModule = (Module *) moduleId;

    // No module, or disassociated from metadata
    if (pModule == NULL || pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // Default return value of NULL
    MethodDesc *pDesc = NULL;

    // Different lookup depending on whether it's a Def or Ref
    if (TypeFromToken(typeDef) == mdtMethodDef)
    {
        pDesc = pModule->LookupMethodDef(typeDef);
    }
    else if (TypeFromToken(typeDef) == mdtMemberRef)
    {
        pDesc = pModule->LookupMemberRefAsMethod(typeDef);
    }
    else
    {
        return E_INVALIDARG;
    }

    if (NULL == pDesc)
    {
        return E_INVALIDARG;
    }

    //
    // Check that this is a non-generic method
    //
    if (pDesc->HasClassOrMethodInstantiation())
    {
        return CORPROF_E_FUNCTION_IS_PARAMETERIZED;
    }

    if (pFunctionId && SUCCEEDED(hr))
    {
        *pFunctionId = MethodDescToFunctionID(pDesc);
    }

    return (hr);
}

HRESULT ProfToEEInterfaceImpl::GetFunctionFromTokenAndTypeArgs(ModuleID moduleID,
                                                               mdMethodDef funcDef,
                                                               ClassID classId,
                                                               ULONG32 cTypeArgs,
                                                               ClassID typeArgs[],
                                                               FunctionID* pFunctionID)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // It can trigger type loads
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // MethodDesc::FindOrCreateAssociatedMethodDesc enters a Crst
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetFunctionFromTokenAndTypeArgs 0x%p, 0x%08x, 0x%p.\n",
         moduleID,
         funcDef,
         classId));

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classId);
    Module* pModule = reinterpret_cast< Module* >(moduleID);

    if ((pModule == NULL) || typeHandle.IsNull())
    {
        return E_INVALIDARG;
    }

    if (!g_profControlBlock.fBaseSystemClassesLoaded)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;
    }

    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    MethodDesc* pMethodDesc = NULL;

    if (mdtMethodDef == TypeFromToken(funcDef))
    {
        pMethodDesc = pModule->LookupMethodDef(funcDef);
    }
    else if (mdtMemberRef == TypeFromToken(funcDef))
    {
        pMethodDesc = pModule->LookupMemberRefAsMethod(funcDef);
    }
    else
    {
        return E_INVALIDARG;
    }

    MethodTable* pMethodTable = typeHandle.GetMethodTable();

    if (pMethodTable == NULL || !pMethodTable->IsRestored() ||
        pMethodDesc == NULL)
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // This array needs to be accessible at least until the call to
    // MethodDesc::FindOrCreateAssociatedMethodDesc
    TypeHandle* genericParameters = new (nothrow) TypeHandle[cTypeArgs];
    NewArrayHolder< TypeHandle > holder(genericParameters);

    if (NULL == genericParameters)
    {
        return E_OUTOFMEMORY;
    }

    for (ULONG32 i = 0; i < cTypeArgs; ++i)
    {
        genericParameters[i] = TypeHandle(reinterpret_cast< MethodTable* >(typeArgs[i]));
    }

    MethodDesc* result = NULL;
    HRESULT hr = S_OK;

    EX_TRY
    {
        result = MethodDesc::FindOrCreateAssociatedMethodDesc(pMethodDesc,
                                                              pMethodTable,
                                                              FALSE,
                                                              Instantiation(genericParameters, cTypeArgs),
                                                              TRUE);
    }
    EX_CATCH_HRESULT(hr);

    if (NULL != result)
    {
        *pFunctionID = MethodDescToFunctionID(result);
    }

    return hr;
}

//*****************************************************************************
// Retrieve information about a given application domain, which is like a
// sub-process.
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::GetAppDomainInfo(AppDomainID appDomainId,
    ULONG       cchName,
    ULONG       *pcchName,
    __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[],
    ProcessID   *pProcessId)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // AppDomain::GetFriendlyNameForDebugger triggers
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // AppDomain::GetFriendlyNameForDebugger takes a lock
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetAppDomainInfo 0x%p.\n",
         appDomainId));

    if (appDomainId == NULL)
    {
        return E_INVALIDARG;
    }

    BaseDomain   *pDomain;            // Internal data structure.
    HRESULT     hr = S_OK;

    // <TODO>@todo:
    // Right now, this ID is not a true AppDomain, since we use the old
    // AppDomain/SystemDomain model in the profiling API.  This means that
    // the profiler exposes the SharedDomain and the SystemDomain to the
    // outside world. It's not clear whether this is actually the right thing
    // to do or not. - seantrow
    //
    // Postponed to V2.
    // </TODO>

    pDomain = (BaseDomain *) appDomainId;

    // Make sure they've passed in a valid appDomainId
    if (pDomain == NULL)
        return (E_INVALIDARG);

    // Pick sensible defaults.
    if (pcchName)
        *pcchName = 0;
    if (szName)
        *szName = 0;
    if (pProcessId)
        *pProcessId = 0;

    LPCWSTR szFriendlyName;
    if (pDomain == SystemDomain::System())
        szFriendlyName = g_pwBaseLibrary;
    else
        szFriendlyName = ((AppDomain*)pDomain)->GetFriendlyNameForDebugger();

    if (szFriendlyName != NULL)
    {
        // Get the module file name
        ULONG trueLen = (ULONG)(wcslen(szFriendlyName) + 1);

        // Return name of module as required.
        if (szName && cchName > 0)
        {
            ULONG copyLen = trueLen;

            if (copyLen >= cchName)
            {
                copyLen = cchName - 1;
            }

            wcsncpy_s(szName, cchName, szFriendlyName, copyLen);
        }

        // If they request the actual length of the name
        if (pcchName)
            *pcchName = trueLen;
    }

    // If we don't have a friendly name but the call was requesting it, then return incomplete data HR
    else
    {
        if ((szName != NULL && cchName > 0) || pcchName)
            hr = CORPROF_E_DATAINCOMPLETE;
    }

    if (pProcessId)
        *pProcessId = (ProcessID) GetCurrentProcessId();

    return (hr);
}


//*****************************************************************************
// Retrieve information about an assembly, which is a collection of dll's.
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::GetAssemblyInfo(AssemblyID    assemblyId,
    ULONG       cchName,
    ULONG       *pcchName,
    __out_ecount_part_opt(cchName, *pcchName) WCHAR szName[],
    AppDomainID *pAppDomainId,
    ModuleID    *pModuleId)
{
    CONTRACTL
    {
        // SString::SString throws
        THROWS;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // PEAssembly::GetSimpleName() enters a lock via use of the metadata interface
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: GetAssemblyInfo 0x%p.\n",
         assemblyId));

    if (assemblyId == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    Assembly    *pAssembly;             // Internal data structure for assembly.

    pAssembly = (Assembly *) assemblyId;
    _ASSERTE(pAssembly != NULL);

    if (pcchName || szName)
    {
        // Get the friendly name of the assembly
        SString name(SString::Utf8, pAssembly->GetSimpleName());

        const COUNT_T nameLength = name.GetCount() + 1;

        if ((NULL != szName) && (cchName > 0))
        {
            wcsncpy_s(szName, cchName, name.GetUnicode(), min(nameLength, cchName - 1));
        }

        if (NULL != pcchName)
        {
            *pcchName = nameLength;
        }
    }

    // Get the parent application domain.
    if (pAppDomainId)
    {
        *pAppDomainId = (AppDomainID) pAssembly->GetDomain();
        _ASSERTE(*pAppDomainId != NULL);
    }

    // Find the module the manifest lives in.
    if (pModuleId)
    {
        *pModuleId = (ModuleID) pAssembly->GetManifestModule();

        // This is the case where the profiler has called GetAssemblyInfo
        // on an assembly that has been completely created yet.
        if (!*pModuleId)
            hr = CORPROF_E_DATAINCOMPLETE;
    }

    return (hr);
}

// Setting ELT hooks is only allowed from within Initialize().  However, test-only
// profilers may need to set those hooks from an attaching profiling.  See
// code:ProfControlBlock#TestOnlyELT
#ifdef PROF_TEST_ONLY_FORCE_ELT
#define PROFILER_TO_CLR_ENTRYPOINT_SET_ELT(logParams)                                   \
    do                                                                                  \
    {                                                                                   \
        if (g_profControlBlock.fTestOnlyForceEnterLeave)                                \
        {                                                                               \
            PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach, logParams);  \
        }                                                                               \
        else                                                                            \
        {                                                                               \
            PROFILER_TO_CLR_ENTRYPOINT_CALLABLE_ON_INIT_ONLY(logParams);                \
        }                                                                               \
    } while(0)
#else //  PROF_TEST_ONLY_FORCE_ELT
#define PROFILER_TO_CLR_ENTRYPOINT_SET_ELT                                              \
    PROFILER_TO_CLR_ENTRYPOINT_CALLABLE_ON_INIT_ONLY
#endif //  PROF_TEST_ONLY_FORCE_ELT


HRESULT ProfToEEInterfaceImpl::SetEnterLeaveFunctionHooks(FunctionEnter * pFuncEnter,
                                                          FunctionLeave * pFuncLeave,
                                                          FunctionTailcall * pFuncTailcall)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    // The profiler must call SetEnterLeaveFunctionHooks during initialization, since
    // the enter/leave events are immutable and must also be set during initialization.
    PROFILER_TO_CLR_ENTRYPOINT_SET_ELT((LF_CORPROF,
                                        LL_INFO10,
                                        "**PROF: SetEnterLeaveFunctionHooks 0x%p, 0x%p, 0x%p.\n",
                                        pFuncEnter,
                                        pFuncLeave,
                                        pFuncTailcall));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    return g_profControlBlock.mainProfilerInfo.pProfInterface->SetEnterLeaveFunctionHooks(pFuncEnter, pFuncLeave, pFuncTailcall);
}


HRESULT ProfToEEInterfaceImpl::SetEnterLeaveFunctionHooks2(FunctionEnter2 * pFuncEnter,
                                                           FunctionLeave2 * pFuncLeave,
                                                           FunctionTailcall2 * pFuncTailcall)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    // The profiler must call SetEnterLeaveFunctionHooks2 during initialization, since
    // the enter/leave events are immutable and must also be set during initialization.
    PROFILER_TO_CLR_ENTRYPOINT_SET_ELT((LF_CORPROF,
                                        LL_INFO10,
                                        "**PROF: SetEnterLeaveFunctionHooks2 0x%p, 0x%p, 0x%p.\n",
                                        pFuncEnter,
                                        pFuncLeave,
                                        pFuncTailcall));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    return
        g_profControlBlock.mainProfilerInfo.pProfInterface->SetEnterLeaveFunctionHooks2(pFuncEnter, pFuncLeave, pFuncTailcall);
}


HRESULT ProfToEEInterfaceImpl::SetEnterLeaveFunctionHooks3(FunctionEnter3 * pFuncEnter3,
                                                           FunctionLeave3 * pFuncLeave3,
                                                           FunctionTailcall3 * pFuncTailcall3)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    // The profiler must call SetEnterLeaveFunctionHooks3 during initialization, since
    // the enter/leave events are immutable and must also be set during initialization.
    PROFILER_TO_CLR_ENTRYPOINT_SET_ELT((LF_CORPROF,
                                        LL_INFO10,
                                        "**PROF: SetEnterLeaveFunctionHooks3 0x%p, 0x%p, 0x%p.\n",
                                        pFuncEnter3,
                                        pFuncLeave3,
                                        pFuncTailcall3));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    return
        g_profControlBlock.mainProfilerInfo.pProfInterface->SetEnterLeaveFunctionHooks3(pFuncEnter3,
                                                                       pFuncLeave3,
                                                                       pFuncTailcall3);
}



HRESULT ProfToEEInterfaceImpl::SetEnterLeaveFunctionHooks3WithInfo(FunctionEnter3WithInfo * pFuncEnter3WithInfo,
                                                                   FunctionLeave3WithInfo * pFuncLeave3WithInfo,
                                                                   FunctionTailcall3WithInfo * pFuncTailcall3WithInfo)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    // The profiler must call SetEnterLeaveFunctionHooks3WithInfo during initialization, since
    // the enter/leave events are immutable and must also be set during initialization.
    PROFILER_TO_CLR_ENTRYPOINT_SET_ELT((LF_CORPROF,
                                        LL_INFO10,
                                        "**PROF: SetEnterLeaveFunctionHooks3WithInfo 0x%p, 0x%p, 0x%p.\n",
                                        pFuncEnter3WithInfo,
                                        pFuncLeave3WithInfo,
                                        pFuncTailcall3WithInfo));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    return
        g_profControlBlock.mainProfilerInfo.pProfInterface->SetEnterLeaveFunctionHooks3WithInfo(pFuncEnter3WithInfo,
                                                                               pFuncLeave3WithInfo,
                                                                               pFuncTailcall3WithInfo);
}


HRESULT ProfToEEInterfaceImpl::SetFunctionIDMapper(FunctionIDMapper *pFunc)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC((LF_CORPROF,
                                      LL_INFO10,
                                      "**PROF: SetFunctionIDMapper 0x%p.\n",
                                      pFunc));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    g_profControlBlock.mainProfilerInfo.pProfInterface->SetFunctionIDMapper(pFunc);

    return (S_OK);
}

HRESULT ProfToEEInterfaceImpl::SetFunctionIDMapper2(FunctionIDMapper2 *pFunc, void * clientData)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC((LF_CORPROF,
                                      LL_INFO10,
                                      "**PROF: SetFunctionIDMapper2. pFunc: 0x%p. clientData: 0x%p.\n",
                                      pFunc,
                                      clientData));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    g_profControlBlock.mainProfilerInfo.pProfInterface->SetFunctionIDMapper2(pFunc, clientData);

    return (S_OK);
}

/*
 * GetFunctionInfo2
 *
 * This function takes the frameInfo returned from a profiler callback and splays it
 * out into as much information as possible.
 *
 * Parameters:
 *   funcId - The function that is being requested.
 *   frameInfo - Frame specific information from a callback (for resolving generics).
 *   pClassId - An optional parameter for returning the class id of the function.
 *   pModuleId - An optional parameter for returning the module of the function.
 *   pToken - An optional parameter for returning the metadata token of the function.
 *   cTypeArgs - The count of the size of the array typeArgs
 *   pcTypeArgs - Returns the number of elements of typeArgs filled in, or if typeArgs is NULL
 *         the number that would be needed.
 *   typeArgs - An array to store generic type parameters for the function.
 *
 * Returns:
 *   S_OK if successful.
 */
HRESULT ProfToEEInterfaceImpl::GetFunctionInfo2(FunctionID funcId,
                                             COR_PRF_FRAME_INFO frameInfo,
                                             ClassID *pClassId,
                                             ModuleID *pModuleId,
                                             mdToken *pToken,
                                             ULONG32 cTypeArgs,
                                             ULONG32 *pcTypeArgs,
                                             ClassID typeArgs[])
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation eventually
        // reads metadata which causes us to take a reader lock.  However, see
        // code:#DisableLockOnAsyncCalls
        DISABLED(CAN_TAKE_LOCK);

        // Asynchronous functions can be called at arbitrary times when runtime
        // is holding locks that cannot be reentered without causing deadlock.
        // This contract detects any attempts to reenter locks held at the time
        // this function was called.
        CANNOT_RETAKE_LOCK;


        PRECONDITION(CheckPointer(pClassId, NULL_OK));
        PRECONDITION(CheckPointer(pModuleId, NULL_OK));
        PRECONDITION(CheckPointer(pToken,  NULL_OK));
        PRECONDITION(CheckPointer(pcTypeArgs, NULL_OK));
        PRECONDITION(CheckPointer(typeArgs, NULL_OK));
    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetFunctionInfo2 0x%p.\n",
        funcId));

    //
    // Verify parameters.
    //
    COR_PRF_FRAME_INFO_INTERNAL *pFrameInfo = (COR_PRF_FRAME_INFO_INTERNAL *)frameInfo;

    if ((funcId == NULL) ||
        ((pFrameInfo != NULL) && (pFrameInfo->funcID != funcId)))
    {
        return E_INVALIDARG;
    }

    MethodDesc *pMethDesc = FunctionIdToMethodDesc(funcId);

    if (pMethDesc == NULL)
    {
        return E_INVALIDARG;
    }

    if ((cTypeArgs != 0) && (typeArgs == NULL))
    {
        return E_INVALIDARG;
    }

    //
    // Find the exact instantiation of this function.
    //
    TypeHandle specificClass;
    MethodDesc* pActualMethod;

    ClassID classId = NULL;

    if (pMethDesc->IsSharedByGenericInstantiations())
    {
        BOOL exactMatch;
        OBJECTREF pThis = NULL;

        if (pFrameInfo != NULL)
        {
            // If FunctionID represents a generic methoddesc on a struct, then pFrameInfo->thisArg
            // isn't an Object*.  It's a pointer directly into the struct's members (i.e., it's not pointing at the
            // method table).  That means pFrameInfo->thisArg cannot be casted to an OBJECTREF for
            // use by Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation.  However,
            // Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation won't even need a this pointer
            // for the methoddesc it's processing if the methoddesc is on a value type.  So we
            // can safely pass NULL for the methoddesc's this in such a case.
            if (pMethDesc->GetMethodTable()->IsValueType())
            {
                _ASSERTE(!pMethDesc->AcquiresInstMethodTableFromThis());
                _ASSERTE(pThis == NULL);
            }
            else
            {
                pThis = ObjectToOBJECTREF((PTR_Object)(pFrameInfo->thisArg));
            }
        }

        exactMatch = Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation(
            pMethDesc,
            pThis,
            PTR_VOID((pFrameInfo != NULL) ? pFrameInfo->extraArg : NULL),
            &specificClass,
            &pActualMethod);

        if (exactMatch)
        {
            classId = TypeHandleToClassID(specificClass);
        }
        else if (!specificClass.HasInstantiation() || !specificClass.IsSharedByGenericInstantiations())
        {
            //
            // In this case we could not get the type args for the method, but if the class
            // is not a generic class or is instantiated with value types, this value is correct.
            //
            classId = TypeHandleToClassID(specificClass);
        }
        else
        {
            //
            // We could not get any class information.
            //
            classId = NULL;
        }
    }
    else
    {
        TypeHandle typeHandle(pMethDesc->GetMethodTable());
        classId = TypeHandleToClassID(typeHandle);
        pActualMethod = pMethDesc;
    }


    //
    // Fill in the ClassId, if desired
    //
    if (pClassId != NULL)
    {
        *pClassId = classId;
    }

    //
    // Fill in the ModuleId, if desired.
    //
    if (pModuleId != NULL)
    {
        *pModuleId = (ModuleID)pMethDesc->GetModule();
    }

    //
    // Fill in the token, if desired.
    //
    if (pToken != NULL)
    {
        *pToken = (mdToken)pMethDesc->GetMemberDef();
    }

    if ((cTypeArgs == 0) && (pcTypeArgs != NULL))
    {
        //
        // They are searching for the size of the array needed, we can return that now and
        // short-circuit all the work below.
        //
        if (pcTypeArgs != NULL)
        {
            *pcTypeArgs = pActualMethod->GetNumGenericMethodArgs();
        }
        return S_OK;
    }

    //
    // If no place to store resulting count, quit now.
    //
    if (pcTypeArgs == NULL)
    {
        return S_OK;
    }

    //
    // Fill in the type args
    //
    DWORD cArgsToFill = pActualMethod->GetNumGenericMethodArgs();

    if (cArgsToFill > cTypeArgs)
    {
        cArgsToFill = cTypeArgs;
    }

    *pcTypeArgs = cArgsToFill;

    if (cArgsToFill == 0)
    {
        return S_OK;
    }

    Instantiation inst = pActualMethod->GetMethodInstantiation();

    for (DWORD i = 0; i < cArgsToFill; i++)
    {
        typeArgs[i] = TypeHandleToClassID(inst[i]);
    }

    return S_OK;
}

/*
* IsFunctionDynamic
*
* This function takes a functionId that maybe of a metadata-less method like an IL Stub
* or LCG method and returns true in the pHasNoMetadata if it is indeed a metadata-less
* method.
*
* Parameters:
*   functionId - The function that is being requested.
*   isDynamic - An optional parameter for returning if the function has metadata or not.
*
* Returns:
*   S_OK if successful.
*/
HRESULT ProfToEEInterfaceImpl::IsFunctionDynamic(FunctionID functionId, BOOL *isDynamic)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;

        // Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation eventually
        // reads metadata which causes us to take a reader lock.  However, see
        // code:#DisableLockOnAsyncCalls
        DISABLED(CAN_TAKE_LOCK);

        // Asynchronous functions can be called at arbitrary times when runtime
        // is holding locks that cannot be reentered without causing deadlock.
        // This contract detects any attempts to reenter locks held at the time
        // this function was called.
        CANNOT_RETAKE_LOCK;


        PRECONDITION(CheckPointer(isDynamic, NULL_OK));
    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
            LL_INFO1000,
            "**PROF: IsFunctionDynamic 0x%p.\n",
            functionId));

    //
    // Verify parameters.
    //

    if (functionId == NULL)
    {
        return E_INVALIDARG;
    }

    MethodDesc *pMethDesc = FunctionIdToMethodDesc(functionId);

    if (pMethDesc == NULL)
    {
        return E_INVALIDARG;
    }

    //
    // Fill in the pHasNoMetadata, if desired.
    //
    if (isDynamic != NULL)
    {
        *isDynamic = pMethDesc->IsNoMetadata();
    }

    return S_OK;
}

/*
* GetFunctionFromIP3
*
* This function takes an IP and determines if it is a managed function returning its
* FunctionID. This method is different from GetFunctionFromIP in that will return
* FunctionIDs even if they have no associated metadata.
*
* Parameters:
*   ip - The instruction pointer.
*   pFunctionId - An optional parameter for returning the FunctionID.
*   pReJitId - The ReJIT id.
*
* Returns:
*   S_OK if successful.
*/
HRESULT ProfToEEInterfaceImpl::GetFunctionFromIP3(LPCBYTE ip, FunctionID * pFunctionId, ReJITID * pReJitId)
{
    CONTRACTL
    {
        NOTHROW;

        // Grabbing the rejitid requires entering the rejit manager's hash table & lock,
        // which can switch us to preemptive mode and trigger GCs
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;

        // Grabbing the rejitid requires entering the rejit manager's hash table & lock,
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
            LL_INFO1000,
            "**PROF: GetFunctionFromIP3 0x%p.\n",
            ip));

    HRESULT hr = S_OK;

    EECodeInfo codeInfo;

    hr = GetFunctionFromIPInternal(ip, &codeInfo, /* failOnNoMetadata */ FALSE);
    if (FAILED(hr))
    {
        return hr;
    }

    if (pFunctionId)
    {
        *pFunctionId = MethodDescToFunctionID(codeInfo.GetMethodDesc());
    }

    if (pReJitId != NULL)
    {
        MethodDesc * pMD = codeInfo.GetMethodDesc();
        *pReJitId = ReJitManager::GetReJitId(pMD, codeInfo.GetStartAddress());
    }

    return S_OK;
}

/*
* GetDynamicFunctionInfo
*
* This function takes a functionId that maybe of a metadata-less method like an IL Stub
* or LCG method and gives information about it without failing like GetFunctionInfo.
*
* Parameters:
*   functionId - The function that is being requested.
*   pModuleId - An optional parameter for returning the module of the function.
*   ppvSig -  An optional parameter for returning the signature of the function.
*   pbSig - An optional parameter for returning the size of the signature of the function.
*   cchName - A parameter for indicating the size of buffer for the wszName parameter.
*   pcchName - An optional parameter for returning the true size of the wszName parameter.
*   wszName - A parameter to the caller allocated buffer of size cchName
*
* Returns:
*   S_OK if successful.
*/
HRESULT ProfToEEInterfaceImpl::GetDynamicFunctionInfo(FunctionID functionId,
                                                      ModuleID *pModuleId,
                                                      PCCOR_SIGNATURE* ppvSig,
                                                      ULONG* pbSig,
                                                      ULONG cchName,
                                                      ULONG *pcchName,
            __out_ecount_part_opt(cchName, *pcchName) WCHAR wszName[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;

        // Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation eventually
        // reads metadata which causes us to take a reader lock.  However, see
        // code:#DisableLockOnAsyncCalls
        DISABLED(CAN_TAKE_LOCK);

        // Asynchronous functions can be called at arbitrary times when runtime
        // is holding locks that cannot be reentered without causing deadlock.
        // This contract detects any attempts to reenter locks held at the time
        // this function was called.
        CANNOT_RETAKE_LOCK;


        PRECONDITION(CheckPointer(pModuleId, NULL_OK));
        PRECONDITION(CheckPointer(ppvSig, NULL_OK));
        PRECONDITION(CheckPointer(pbSig,  NULL_OK));
        PRECONDITION(CheckPointer(pcchName, NULL_OK));
    }
    CONTRACTL_END;

    // See code:#DisableLockOnAsyncCalls
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
            LL_INFO1000,
            "**PROF: GetDynamicFunctionInfo 0x%p.\n",
            functionId));

    //
    // Verify parameters.
    //

    if (functionId == NULL)
    {
        return E_INVALIDARG;
    }

    MethodDesc *pMethDesc = FunctionIdToMethodDesc(functionId);

    if (pMethDesc == NULL)
    {
        return E_INVALIDARG;
    }

    if (!pMethDesc->IsNoMetadata())
        return E_INVALIDARG;

    //
    // Fill in the ModuleId, if desired.
    //
    if (pModuleId != NULL)
    {
        *pModuleId = (ModuleID)pMethDesc->GetModule();
    }

    //
    // Fill in the ppvSig and pbSig, if desired
    //
    if (ppvSig != NULL && pbSig != NULL)
    {
        pMethDesc->GetSig(ppvSig, pbSig);
    }

    HRESULT hr = S_OK;

    EX_TRY
    {
        if (wszName != NULL)
            *wszName = 0;
        if (pcchName != NULL)
            *pcchName = 0;

        StackSString ss;
        ss.SetUTF8(pMethDesc->GetName());
        ss.Normalize();
        LPCWSTR methodName = ss.GetUnicode();

        ULONG trueLen = (ULONG)(wcslen(methodName) + 1);

        // Return name of method as required.
        if (wszName && cchName > 0)
        {
            if (cchName < trueLen)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            }
            else
            {
                wcsncpy_s(wszName, cchName, methodName, trueLen);
            }
        }

        // If they request the actual length of the name
        if (pcchName)
            *pcchName = trueLen;
    }
    EX_CATCH_HRESULT(hr);

    return (hr);
}

/*
 * GetNativeCodeStartAddresses
 *
 * Gets all of the native code addresses associated with a particular function. iered compilation
 * potentially creates different native code versions for a method, and this function allows profilers
 * to view all native versions of a method.
 *
 * Parameters:
 *      functionID           - The function that is being requested.
 *      reJitId              - The ReJIT id.
 *      cCodeStartAddresses  - A parameter for indicating the size of buffer for the codeStartAddresses parameter.
 *      pcCodeStartAddresses - An optional parameter for returning the true size of the codeStartAddresses parameter.
 *      codeStartAddresses   - The array to be filled up with native code addresses.
 *
 * Returns:
 *   S_OK if successful
 *
 */
HRESULT ProfToEEInterfaceImpl::GetNativeCodeStartAddresses(FunctionID functionID,
                                                           ReJITID reJitId,
                                                           ULONG32 cCodeStartAddresses,
                                                           ULONG32 *pcCodeStartAddresses,
                                                           UINT_PTR codeStartAddresses[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(pcCodeStartAddresses, NULL_OK));
        PRECONDITION(CheckPointer(codeStartAddresses, NULL_OK));
    }
    CONTRACTL_END;

    if (functionID == NULL)
    {
        return E_INVALIDARG;
    }

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
    (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetNativeCodeStartAddresses 0x%p 0x%p.\n",
        functionID, reJitId));

    HRESULT hr = S_OK;

    EX_TRY
    {
        if (pcCodeStartAddresses != NULL)
        {
            *pcCodeStartAddresses = 0;
        }

        MethodDesc * methodDesc = FunctionIdToMethodDesc(functionID);
        PTR_MethodDesc pMD = PTR_MethodDesc(methodDesc);
        ULONG32 trueLen = 0;
        StackSArray<UINT_PTR> addresses;

        CodeVersionManager *pCodeVersionManager = pMD->GetCodeVersionManager();

        ILCodeVersion ilCodeVersion = NULL;
        {
            CodeVersionManager::LockHolder codeVersioningLockHolder;

            ilCodeVersion = pCodeVersionManager->GetILCodeVersion(pMD, reJitId);

            NativeCodeVersionCollection nativeCodeVersions = ilCodeVersion.GetNativeCodeVersions(pMD);
            for (NativeCodeVersionIterator iter = nativeCodeVersions.Begin(); iter != nativeCodeVersions.End(); iter++)
            {
                PCODE codeStart = (*iter).GetNativeCode();

                if (codeStart != NULL)
                {
                    addresses.Append(codeStart);
                    ++trueLen;
                }
            }
        }

        if (pcCodeStartAddresses != NULL)
        {
            *pcCodeStartAddresses = trueLen;
        }

        if (codeStartAddresses != NULL)
        {
            if (cCodeStartAddresses < trueLen)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            }
            else
            {
                for(ULONG32 i = 0; i < trueLen; ++i)
                {
                    codeStartAddresses[i] = addresses[i];
                }
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

/*
 * GetILToNativeMapping3
 *
 * This overload behaves the same as GetILToNativeMapping2, except it allows the profiler
 * to address specific native code versions instead of defaulting to the first one.
 *
 * Parameters:
 *      pNativeCodeStartAddress - start address of the native code version, returned by GetNativeCodeStartAddresses
 *      cMap                    - size of the map array
 *      pcMap                   - how many items are returned in the map array
 *      map                     - an array to store the il to native mappings in
 *
 * Returns:
 *   S_OK if successful
 *
 */
HRESULT ProfToEEInterfaceImpl::GetILToNativeMapping3(UINT_PTR pNativeCodeStartAddress,
                                                     ULONG32 cMap,
                                                     ULONG32 *pcMap,
                                                     COR_DEBUG_IL_TO_NATIVE_MAP map[])
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_NOTRIGGER);
        MODE_ANY;
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(pcMap, NULL_OK));
        PRECONDITION(CheckPointer(map, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetILToNativeMapping3 0x%p.\n",
        pNativeCodeStartAddress));

    if (pNativeCodeStartAddress == NULL)
    {
        return E_INVALIDARG;
    }

    if ((cMap > 0) &&
        ((pcMap == NULL) || (map == NULL)))
    {
        return E_INVALIDARG;
    }

#ifdef DEBUGGING_SUPPORTED
    if (g_pDebugInterface == NULL)
    {
        return CORPROF_E_DEBUGGING_DISABLED;
    }

    return (g_pDebugInterface->GetILToNativeMapping(pNativeCodeStartAddress, cMap, pcMap, map));
#else
    return E_NOTIMPL;
#endif
}

/*
 * GetCodeInfo4
 *
 * Gets the location and size of a jitted function. Tiered compilation potentially creates different native code
 * versions for a method, and this overload allows profilers to specify which native version it would like the
 * code info for.
 *
 * Parameters:
 *      pNativeCodeStartAddress - start address of the native code version, returned by GetNativeCodeStartAddresses
 *      cCodeInfos              - size of the codeInfos array
 *      pcCodeInfos             - how many items are returned in the codeInfos array
 *      codeInfos               - an array to store the code infos in
 *
 * Returns:
 *   S_OK if successful
 *
 */
HRESULT ProfToEEInterfaceImpl::GetCodeInfo4(UINT_PTR pNativeCodeStartAddress,
                                            ULONG32 cCodeInfos,
                                            ULONG32* pcCodeInfos,
                                            COR_PRF_CODE_INFO codeInfos[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(pcCodeInfos, NULL_OK));
        PRECONDITION(CheckPointer(codeInfos, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetCodeInfo4 0x%p.\n",
        pNativeCodeStartAddress));

    if ((cCodeInfos != 0) && (codeInfos == NULL))
    {
        return E_INVALIDARG;
    }

    return GetCodeInfoFromCodeStart(pNativeCodeStartAddress,
                                    cCodeInfos,
                                    pcCodeInfos,
                                    codeInfos);
}

HRESULT ProfToEEInterfaceImpl::RequestReJITWithInliners(
            DWORD       dwRejitFlags,
            ULONG       cFunctions,
            ModuleID    moduleIds[],
            mdMethodDef methodIds[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(moduleIds, NULL_OK));
        PRECONDITION(CheckPointer(methodIds, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EETriggers | kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: RequestReJITWithInliners.\n"));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    if (!m_pProfilerInfo->pProfInterface->IsCallback4Supported())
    {
        return CORPROF_E_CALLBACK4_REQUIRED;
    }

    if (!CORProfilerEnableRejit())
    {
        return CORPROF_E_REJIT_NOT_ENABLED;
    }

    if (!ReJitManager::IsReJITInlineTrackingEnabled())
    {
        return CORPROF_E_REJIT_INLINING_DISABLED;
    }

    // Request at least 1 method to reJIT!
    if ((cFunctions == 0) || (moduleIds == NULL) || (methodIds == NULL))
    {
        return E_INVALIDARG;
    }

    // We only support disabling inlining currently
    if ((dwRejitFlags & COR_PRF_REJIT_BLOCK_INLINING) != COR_PRF_REJIT_BLOCK_INLINING)
    {
        return E_INVALIDARG;
    }

    // Remember the profiler is doing this, as that means we must never detach it!
    g_profControlBlock.mainProfilerInfo.pProfInterface->SetUnrevertiblyModifiedILFlag();

    HRESULT hr = SetupThreadForReJIT();
    if (FAILED(hr))
    {
        return hr;
    }

    GCX_PREEMP();
    return ReJitManager::RequestReJIT(cFunctions, moduleIds, methodIds, static_cast<COR_PRF_REJIT_FLAGS>(dwRejitFlags));
}

/*
 * EnumerateObjectReferences
 *
 * Enumerates the object references (if any) from the ObjectID.
 *
 * Parameters:
 *      objectId        - object id of interest
 *      callback        - callback to call for each object reference
 *      clientData      - client data for the profiler to pass and receive for each reference
 *
 * Returns:
 *   S_OK if successful, S_FALSE if no references
 *
 */
HRESULT ProfToEEInterfaceImpl::EnumerateObjectReferences(ObjectID objectId, ObjectReferenceCallback callback, void* clientData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EnumerateObjectReferences 0x%p.\n",
        objectId));

    if (callback == nullptr)
    {
        return E_INVALIDARG;
    }

    Object* pBO = (Object*)objectId;
    MethodTable *pMT = pBO->GetMethodTable();

    if (pMT->ContainsPointersOrCollectible())
    {
        GCHeapUtilities::GetGCHeap()->DiagWalkObject2(pBO, (walk_fn2)callback, clientData);
        return S_OK;
    }
    else
    {
        return S_FALSE;
    }
}

/*
 * IsFrozenObject
 *
 * Determines whether the object is in a read-only segment
 *
 * Parameters:
 *      objectId        - object id of interest
 *
 * Returns:
 *   S_OK if successful
 *
 */
HRESULT ProfToEEInterfaceImpl::IsFrozenObject(ObjectID objectId, BOOL *pbFrozen)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: IsFrozenObject 0x%p.\n",
        objectId));

    *pbFrozen = GCHeapUtilities::GetGCHeap()->IsInFrozenSegment((Object*)objectId) ? TRUE : FALSE;

    return S_OK;
}

/*
 * GetLOHObjectSizeThreshold
 *
 * Gets the value of the configured LOH Threshold.
 *
 * Parameters:
 *      pThreshold        - value of the threshold in bytes
 *
 * Returns:
 *   S_OK if successful
 *
 */
HRESULT ProfToEEInterfaceImpl::GetLOHObjectSizeThreshold(DWORD *pThreshold)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetLOHObjectSizeThreshold\n"));

    if (pThreshold == nullptr)
    {
        return E_INVALIDARG;
    }

    *pThreshold = g_pConfig->GetGCLOHThreshold();

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::SuspendRuntime()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: SuspendRuntime\n"));

    if (!g_fEEStarted)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;
    }

    if (ThreadSuspend::SysIsSuspendInProgress() || (ThreadSuspend::GetSuspensionThread() != 0))
    {
        return CORPROF_E_SUSPENSION_IN_PROGRESS;
    }

    g_profControlBlock.fProfilerRequestedRuntimeSuspend = TRUE;
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_REASON::SUSPEND_FOR_PROFILER);
    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::ResumeRuntime()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: ResumeRuntime\n"));

    if (!g_fEEStarted)
    {
        return CORPROF_E_RUNTIME_UNINITIALIZED;
    }

    if (!g_profControlBlock.fProfilerRequestedRuntimeSuspend)
    {
        return CORPROF_E_UNSUPPORTED_CALL_SEQUENCE;
    }

    ThreadSuspend::RestartEE(FALSE /* bFinishedGC */, TRUE /* SuspendSucceeded */);
    g_profControlBlock.fProfilerRequestedRuntimeSuspend = FALSE;
    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::GetEnvironmentVariable(
    const WCHAR *szName,
    ULONG       cchValue,
    ULONG       *pcchValue,
    __out_ecount_part_opt(cchValue, *pcchValue) WCHAR szValue[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;

        PRECONDITION(CheckPointer(szName, NULL_NOT_OK));
        PRECONDITION(CheckPointer(pcchValue, NULL_OK));
        PRECONDITION(CheckPointer(szValue, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetEnvironmentVariable.\n"));

    if (szName == nullptr)
    {
        return E_INVALIDARG;
    }

    if ((cchValue != 0) && (szValue == nullptr))
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    if ((pcchValue != nullptr) || (szValue != nullptr))
    {
        DWORD trueLen = GetEnvironmentVariableW(szName, szValue, cchValue);
        if (trueLen == 0)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
        else if ((trueLen > cchValue) && (szValue != nullptr))
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }

        if (pcchValue != nullptr)
        {
            *pcchValue = trueLen;
        }
    }

    return hr;
}

HRESULT ProfToEEInterfaceImpl::SetEnvironmentVariable(const WCHAR *szName, const WCHAR *szValue)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
        CANNOT_TAKE_LOCK;

        PRECONDITION(CheckPointer(szName, NULL_NOT_OK));
        PRECONDITION(CheckPointer(szValue, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: SetEnvironmentVariable.\n"));

    if (szName == nullptr)
    {
        return E_INVALIDARG;
    }

    return SetEnvironmentVariableW(szName, szValue) ? S_OK : HRESULT_FROM_WIN32(GetLastError());
}

HRESULT ProfToEEInterfaceImpl::EventPipeStartSession(
    UINT32 cProviderConfigs,
    COR_PRF_EVENTPIPE_PROVIDER_CONFIG pProviderConfigs[],
    BOOL requestRundown,
    EVENTPIPE_SESSION* pSession)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeStartSession.\n"));

#ifdef FEATURE_PERFTRACING
    if (cProviderConfigs == 0
        || pProviderConfigs == NULL
        || pSession == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        EventPipeProviderConfigurationAdapter providerConfigsAdapter(pProviderConfigs, cProviderConfigs);
        UINT64 sessionID = EventPipeAdapter::Enable(NULL,
                                             0, // We don't use a circular buffer since it's synchronous
                                             providerConfigsAdapter,
                                             EP_SESSION_TYPE_SYNCHRONOUS,
                                             EP_SERIALIZATION_FORMAT_NETTRACE_V4,
                                             requestRundown,
                                             NULL,
                                             reinterpret_cast<EventPipeSessionSynchronousCallback>(&ProfToEEInterfaceImpl::EventPipeCallbackHelper),
                                             reinterpret_cast<void *>(m_pProfilerInfo));
        if (sessionID != 0)
        {
            EventPipeAdapter::StartStreaming(sessionID);

            *pSession = sessionID;
        }
        else
        {
            hr = E_FAIL;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT ProfToEEInterfaceImpl::EventPipeAddProviderToSession(
        EVENTPIPE_SESSION session,
        COR_PRF_EVENTPIPE_PROVIDER_CONFIG providerConfig)
{

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeAddProviderToSession.\n"));

#ifdef FEATURE_PERFTRACING
    if (providerConfig.providerName == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        EventPipeSession *pSession = EventPipeAdapter::GetSession(session);
        if (pSession == NULL)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            EventPipeProviderConfigurationAdapter adapter(&providerConfig, 1);
            EventPipeSessionProvider *pProvider = EventPipeAdapter::CreateSessionProvider(adapter);
            EventPipeAdapter::AddProviderToSession(pProvider, pSession);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT ProfToEEInterfaceImpl::EventPipeStopSession(
    EVENTPIPE_SESSION session)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeStopSession.\n"));

#ifdef FEATURE_PERFTRACING
    HRESULT hr = S_OK;
    EX_TRY
    {
        EventPipeAdapter::Disable(session);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT ProfToEEInterfaceImpl::EventPipeCreateProvider(
    const WCHAR *providerName,
    EVENTPIPE_PROVIDER *pProvider)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeCreateProvider.\n"));

#ifdef FEATURE_PERFTRACING
    if (providerName == NULL || pProvider == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        EventPipeProvider *pRealProvider = EventPipeAdapter::CreateProvider(providerName, nullptr);
        if (pRealProvider == NULL)
        {
            hr = E_FAIL;
        }
        else
        {
            *pProvider = reinterpret_cast<EVENTPIPE_PROVIDER>(pRealProvider);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT ProfToEEInterfaceImpl::EventPipeGetProviderInfo(
            EVENTPIPE_PROVIDER provider,
            ULONG      cchName,
            ULONG      *pcchName,
            WCHAR      szName[])
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeGetProviderInfo.\n"));

#ifdef FEATURE_PERFTRACING
        if (cchName > 0 && szName == NULL)
        {
            return E_INVALIDARG;
        }

        EventPipeProvider *pRealProvider = reinterpret_cast<EventPipeProvider *>(provider);
        if (pRealProvider == NULL)
        {
            // Bogus provider passed in
            return E_INVALIDARG;
        }

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = EventPipeAdapter::GetProviderName (pRealProvider, cchName, pcchName, szName);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT ProfToEEInterfaceImpl::EventPipeDefineEvent(
    EVENTPIPE_PROVIDER provider,
    const WCHAR *eventName,
    UINT32 eventID,
    UINT64 keywords,
    UINT32 eventVersion,
    UINT32 level,
    UINT8 opcode,
    BOOL needStack,
    UINT32 cParamDescs,
    COR_PRF_EVENTPIPE_PARAM_DESC pParamDescs[],
    EVENTPIPE_EVENT *pEvent)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeDefineEvent.\n"));

#ifdef FEATURE_PERFTRACING
    EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provider);
    if (pProvider == NULL || eventName == NULL || pEvent == NULL)
    {
        return E_INVALIDARG;
    }

    if (pParamDescs == NULL && cParamDescs > 0)
    {
        return E_INVALIDARG;
    }

    for (UINT32 i = 0; i < cParamDescs; ++i)
    {
        if ((EventPipeParameterType)(pParamDescs[i].type) == EP_PARAMETER_TYPE_OBJECT)
        {
            // The native EventPipeMetadataGenerator only knows how to encode
            // primitive types, it would not handle Object correctly
            return E_INVALIDARG;
        }
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        EventPipeParameterDescAdapter adapter(pParamDescs, cParamDescs);
        EventPipeEvent *pRealEvent = EventPipeAdapter::AddEvent(
            pProvider,
            eventID,
            eventName,
            keywords,
            eventVersion,
            (EventPipeEventLevel)level,
            opcode,
            adapter,
            needStack);
        *pEvent = reinterpret_cast<EVENTPIPE_EVENT>(pRealEvent);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

HRESULT ProfToEEInterfaceImpl::EventPipeWriteEvent(
    EVENTPIPE_EVENT event,
    UINT32 cData,
    COR_PRF_EVENT_DATA data[],
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: EventPipeWriteEvent.\n"));
#ifdef FEATURE_PERFTRACING
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(event);

    if (pEvent == NULL)
    {
        return E_INVALIDARG;
    }

    EventDataAdapter adapter(data, cData);
    EventPipeAdapter::WriteEvent(pEvent, adapter, pActivityId, pRelatedActivityId);

    return S_OK;
#else // FEATURE_PERFTRACING
    return E_NOTIMPL;
#endif // FEATURE_PERFTRACING
}

void ProfToEEInterfaceImpl::EventPipeCallbackHelper(EventPipeProvider *provider,
                                                    DWORD eventId,
                                                    DWORD eventVersion,
                                                    ULONG cbMetadataBlob,
                                                    LPCBYTE metadataBlob,
                                                    ULONG cbEventData,
                                                    LPCBYTE eventData,
                                                    LPCGUID pActivityId,
                                                    LPCGUID pRelatedActivityId,
                                                    Thread *pEventThread,
                                                    ULONG numStackFrames,
                                                    UINT_PTR stackFrames[],
                                                    void *additionalData)
{
    _ASSERTE(additionalData != NULL);
    ProfilerInfo *pProfilerInfo = reinterpret_cast<ProfilerInfo *>(additionalData);

    _ASSERTE(pProfilerInfo->pProfInterface.Load() != NULL);
    {
        EvacuationCounterHolder holder(pProfilerInfo);
        // But, a profiler could always register for a session and then detach without
        // closing the session. So check if we have an interface before proceeding.
        if (pProfilerInfo->pProfInterface.Load() != NULL)
        {
            pProfilerInfo->pProfInterface->EventPipeEventDelivered(provider,
                                                                       eventId,
                                                                       eventVersion,
                                                                       cbMetadataBlob,
                                                                       metadataBlob,
                                                                       cbEventData,
                                                                       eventData,
                                                                       pActivityId,
                                                                       pRelatedActivityId,
                                                                       pEventThread,
                                                                       numStackFrames,
                                                                       stackFrames);
        }
    }
};



/*
 * GetStringLayout
 *
 * This function describes to a profiler the internal layout of a string.
 *
 * Parameters:
 *   pBufferLengthOffset - Offset within an OBJECTREF of a string of the ArrayLength field.
 *   pStringLengthOffset - Offset within an OBJECTREF of a string of the StringLength field.
 *   pBufferOffset - Offset within an OBJECTREF of a string of the Buffer field.
 *
 * Returns:
 *   S_OK if successful.
 */
HRESULT ProfToEEInterfaceImpl::GetStringLayout(ULONG *pBufferLengthOffset,
                                             ULONG *pStringLengthOffset,
                                             ULONG *pBufferOffset)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(CheckPointer(pBufferLengthOffset, NULL_OK));
        PRECONDITION(CheckPointer(pStringLengthOffset, NULL_OK));
        PRECONDITION(CheckPointer(pBufferOffset,  NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetStringLayout.\n"));

    return this->GetStringLayoutHelper(pBufferLengthOffset, pStringLengthOffset, pBufferOffset);
}

/*
 * GetStringLayout2
 *
 * This function describes to a profiler the internal layout of a string.
 *
 * Parameters:
 *   pStringLengthOffset - Offset within an OBJECTREF of a string of the StringLength field.
 *   pBufferOffset - Offset within an OBJECTREF of a string of the Buffer field.
 *
 * Returns:
 *   S_OK if successful.
 */
HRESULT ProfToEEInterfaceImpl::GetStringLayout2(ULONG *pStringLengthOffset,
                                             ULONG *pBufferOffset)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(CheckPointer(pStringLengthOffset, NULL_OK));
        PRECONDITION(CheckPointer(pBufferOffset,  NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetStringLayout2.\n"));

    ULONG dummyBufferLengthOffset;
    return this->GetStringLayoutHelper(&dummyBufferLengthOffset, pStringLengthOffset, pBufferOffset);
}

/*
 * GetStringLayoutHelper
 *
 * This function describes to a profiler the internal layout of a string.
 *
 * Parameters:
 *   pBufferLengthOffset - Offset within an OBJECTREF of a string of the ArrayLength field.
 *   pStringLengthOffset - Offset within an OBJECTREF of a string of the StringLength field.
 *   pBufferOffset - Offset within an OBJECTREF of a string of the Buffer field.
 *
 * Returns:
 *   S_OK if successful.
 */
HRESULT ProfToEEInterfaceImpl::GetStringLayoutHelper(ULONG *pBufferLengthOffset,
                                             ULONG *pStringLengthOffset,
                                             ULONG *pBufferOffset)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(CheckPointer(pBufferLengthOffset, NULL_OK));
        PRECONDITION(CheckPointer(pStringLengthOffset, NULL_OK));
        PRECONDITION(CheckPointer(pBufferOffset,  NULL_OK));
    }
    CONTRACTL_END;

    // The String class no longer has a bufferLength field in it.
    // We are returning the offset of the stringLength because that is the closest we can get
    // This is most certainly a breaking change and a new method
    // ICorProfilerInfo3::GetStringLayout2 has been added on the interface ICorProfilerInfo3
    if (pBufferLengthOffset != NULL)
    {
        *pBufferLengthOffset = StringObject::GetStringLengthOffset();
    }

    if (pStringLengthOffset != NULL)
    {
        *pStringLengthOffset = StringObject::GetStringLengthOffset();
    }

    if (pBufferOffset != NULL)
    {
        *pBufferOffset = StringObject::GetBufferOffset();
    }

    return S_OK;
}

/*
 * GetClassLayout
 *
 * This function describes to a profiler the internal layout of a class.
 *
 * Parameters:
 *   classID - The class that is being queried.  It is really a TypeHandle.
 *   rFieldOffset - An array to store information about each field in the class.
 *   cFieldOffset - Count of the number of elements in rFieldOffset.
 *   pcFieldOffset - Upon return contains the number of elements filled in, or if
 *         cFieldOffset is zero, the number of elements needed.
 *   pulClassSize - Optional parameter for containing the size in bytes of the underlying
 *         internal class structure.
 *
 * Returns:
 *   S_OK if successful.
 */
HRESULT ProfToEEInterfaceImpl::GetClassLayout(ClassID classID,
                                             COR_FIELD_OFFSET rFieldOffset[],
                                             ULONG cFieldOffset,
                                             ULONG *pcFieldOffset,
                                             ULONG *pulClassSize)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(CheckPointer(rFieldOffset, NULL_OK));
        PRECONDITION(CheckPointer(pcFieldOffset));
        PRECONDITION(CheckPointer(pulClassSize,  NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetClassLayout 0x%p.\n",
        classID));

    //
    // Verify parameters
    //
    if ((pcFieldOffset == NULL) || (classID == NULL))
    {
         return E_INVALIDARG;
    }

    if ((cFieldOffset != 0) && (rFieldOffset == NULL))
    {
        return E_INVALIDARG;
    }

    TypeHandle typeHandle = TypeHandle::FromPtr((void *)classID);

    //
    // This is the incorrect API for arrays or strings.  Use GetArrayObjectInfo, and GetStringLayout
    //
    if (typeHandle.IsTypeDesc() || typeHandle.AsMethodTable()->IsArray())
    {
        return E_INVALIDARG;
    }

    //
    // We used to have a bug where this API incorrectly succeeded for strings during startup. Profilers
    // took dependency on this bug. Let the API to succeed for strings during startup for backward compatibility.
    //
    if (typeHandle.AsMethodTable()->IsString() && g_profControlBlock.fBaseSystemClassesLoaded)
    {
        return E_INVALIDARG;
    }

    //
    // If this class is not fully restored, that is all the information we can get at this time.
    //
    if (!typeHandle.IsRestored())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    // !IsValueType = IsArray || IsReferenceType   Since IsArry has been ruled out above, it must
    // be a reference type if !IsValueType.
    BOOL fReferenceType = !typeHandle.IsValueType();

    //
    // Fill in class size now
    //
    // Move after the check for typeHandle.GetMethodTable()->IsRestored()
    // because an unrestored MethodTable may have a bad EE class pointer
    // which will be used by MethodTable::GetNumInstanceFieldBytes
    //
    if (pulClassSize != NULL)
    {
        if (fReferenceType)
        {
            // aligned size including the object header for reference types
            *pulClassSize = typeHandle.GetMethodTable()->GetBaseSize();
        }
        else
        {
            // unboxed and unaligned size for value types
            *pulClassSize = typeHandle.GetMethodTable()->GetNumInstanceFieldBytes();
        }
    }

    ApproxFieldDescIterator fieldDescIterator(typeHandle.GetMethodTable(), ApproxFieldDescIterator::INSTANCE_FIELDS);

    ULONG cFields = fieldDescIterator.Count();

    //
    // If they are looking to just get the count, return that.
    //
    if ((cFieldOffset == 0)  || (rFieldOffset == NULL))
    {
        *pcFieldOffset = cFields;
        return S_OK;
    }

    //
    // Dont put too many in the array.
    //
    if (cFields > cFieldOffset)
    {
        cFields = cFieldOffset;
    }

    *pcFieldOffset = cFields;

    //
    // Now fill in the array
    //
    ULONG i;
    FieldDesc *pField;

    for (i = 0; i < cFields; i++)
    {
        pField = fieldDescIterator.Next();
        rFieldOffset[i].ridOfField = (ULONG)pField->GetMemberDef();
        rFieldOffset[i].ulOffset = (ULONG)pField->GetOffset() + (fReferenceType ? Object::GetOffsetOfFirstField() : 0);
    }

    return S_OK;
}


typedef struct _PROFILER_STACK_WALK_DATA
{
    StackSnapshotCallback *callback;
    ULONG32 infoFlags;
    ULONG32 contextFlags;
    void *clientData;

#ifdef FEATURE_EH_FUNCLETS
    StackFrame sfParent;
#endif
} PROFILER_STACK_WALK_DATA;


/*
 * ProfilerStackWalkCallback
 *
 * This routine is used as the callback from the general stack walker for
 * doing snapshot stack walks
 *
 */
StackWalkAction ProfilerStackWalkCallback(CrawlFrame *pCf, PROFILER_STACK_WALK_DATA *pData)
{
    CONTRACTL
    {
        NOTHROW;  // throw is RIGHT out... the throw at minimum allocates the thrown object which we *must* not do
        GC_NOTRIGGER; // the stack is not necessarily crawlable at this state !!!) we must not induce a GC
    }
    CONTRACTL_END;

    MethodDesc *pFunc = pCf->GetFunction();

    COR_PRF_FRAME_INFO_INTERNAL frameInfo;
    ULONG32 contextSize = 0;
    BYTE *context = NULL;

    UINT_PTR currentIP = 0;
    REGDISPLAY *pRegDisplay = pCf->GetRegisterSet();
#if defined(TARGET_X86)
    CONTEXT builtContext;
#endif

    //
    // For Unmanaged-to-managed transitions we get a NativeMarker back, which we want
    // to return to the profiler as the context seed if it wants to walk the unmanaged
    // stack frame, so we report the functionId as NULL to indicate this.
    //
    if (pCf->IsNativeMarker())
    {
        pFunc = NULL;
    }

    //
    // Skip all Lightweight reflection/emit functions
    //
    if ((pFunc != NULL) && pFunc->IsNoMetadata())
    {
        return SWA_CONTINUE;
    }

    //
    // If this is not a transition of any sort and not a managed
    // method, ignore it.
    //
    if (!pCf->IsNativeMarker() && !pCf->IsFrameless())
    {
        return SWA_CONTINUE;
    }

    currentIP = (UINT_PTR)pRegDisplay->ControlPC;

    frameInfo.size = sizeof(COR_PRF_FRAME_INFO_INTERNAL);
    frameInfo.version = COR_PRF_FRAME_INFO_INTERNAL_CURRENT_VERSION;

    if (pFunc != NULL)
    {
        frameInfo.funcID = MethodDescToFunctionID(pFunc);
        frameInfo.extraArg = NULL;
    }
    else
    {
        frameInfo.funcID = NULL;
        frameInfo.extraArg = NULL;
    }

    frameInfo.IP = currentIP;
    frameInfo.thisArg = NULL;

    if (pData->infoFlags & COR_PRF_SNAPSHOT_REGISTER_CONTEXT)
    {
#if defined(TARGET_X86)
        //
        // X86 stack walking does not keep the context up-to-date during the
        // walk.  Instead it keeps the REGDISPLAY up-to-date.  Thus, we need to
        // build a CONTEXT from the REGDISPLAY.
        //

        memset(&builtContext, 0, sizeof(builtContext));
        builtContext.ContextFlags = CONTEXT_INTEGER | CONTEXT_CONTROL;
        CopyRegDisplay(pRegDisplay, NULL, &builtContext);
        context = (BYTE *)(&builtContext);
#else
        context = (BYTE *)pRegDisplay->pCurrentContext;
#endif
        contextSize = sizeof(CONTEXT);
    }

    // NOTE:  We are intentionally not setting any callback state flags here (i.e., not using
    // SetCallbackStateFlagsHolder), as we want the DSS callback to "inherit" the
    // same callback state that DSS has:  if DSS was called asynchronously, then consider
    // the DSS callback to be called asynchronously.
    if (pData->callback(frameInfo.funcID,
                        frameInfo.IP,
                        (COR_PRF_FRAME_INFO)&frameInfo,
                        contextSize,
                        context,
                        pData->clientData) == S_OK)
    {
        return SWA_CONTINUE;
    }

    return SWA_ABORT;
}

#ifdef TARGET_X86

//---------------------------------------------------------------------------------------
// Normally, calling GetFunction() on the frame is sufficient to ensure
// HelperMethodFrames are intialized. However, sometimes we need to be able to specify
// that we should not enter the host while initializing, so we need to initialize such
// frames more directly. This small helper function directly forces the initialization,
// and ensures we don't enter the host as a result if we're executing in an asynchronous
// call (i.e., hijacked thread)
//
// Arguments:
//      pFrame - Frame to initialize.
//
// Return Value:
//     TRUE iff pFrame was successfully initialized (or was already initialized). If
//     pFrame is not a HelperMethodFrame (or derived type), this returns TRUE
//     immediately. FALSE indicates we tried to initialize w/out entering the host, and
//     had to abort as a result when a reader lock was needed but unavailable.
//

static BOOL EnsureFrameInitialized(Frame * pFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        // If this is called asynchronously (from a hijacked thread, as with F1), it must not re-enter the
        // host (SQL).  Corners will be cut to ensure this is the case
        if (ShouldAvoidHostCalls()) { HOST_NOCALLS; } else { HOST_CALLS; }

        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (pFrame->GetFrameType() != Frame::TYPE_HELPER_METHOD_FRAME)
    {
        // This frame is not a HelperMethodFrame or a frame derived from
        // HelperMethodFrame, so HMF-specific lazy initialization is not an issue.
        return TRUE;
    }

    HelperMethodFrame * pHMF = (HelperMethodFrame *) pFrame;

    if (pHMF->InsureInit(
        false,                      // initialInit
        NULL,                       // unwindState
        (ShouldAvoidHostCalls() ?
            NoHostCalls :
            AllowHostCalls)
        ) != NULL)
    {
        // InsureInit() succeeded and found the return address
        return TRUE;
    }

    // No return address was found. It must be because we asked InsureInit() to bail if
    // it would have entered the host
    _ASSERTE(ShouldAvoidHostCalls());
    return FALSE;
}

//---------------------------------------------------------------------------------------
//
// Implements the COR_PRF_SNAPSHOT_X86_OPTIMIZED algorithm called by DoStackSnapshot.
// Does a simple EBP walk, rather than invoking all of StackWalkFramesEx.
//
// Arguments:
//      pThreadToSnapshot - Thread whose stack should be walked
//      pctxSeed - Register context with which to seed the walk
//      callback - Function to call at each frame found during the walk
//      clientData - Parameter to pass through to callback
//
// Return Value:
//     HRESULT indicating success or failure.
//

HRESULT ProfToEEInterfaceImpl::ProfilerEbpWalker(
    Thread * pThreadToSnapshot,
    LPCONTEXT pctxSeed,
    StackSnapshotCallback * callback,
    void * clientData)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
        EE_THREAD_NOT_REQUIRED;

        // If this is called asynchronously (from a hijacked thread, as with F1), it must not re-enter the
        // host (SQL).  Corners will be cut to ensure this is the case
        if (ShouldAvoidHostCalls()) { HOST_NOCALLS; } else { HOST_CALLS; }
    }
    CONTRACTL_END;

    HRESULT hr;

    // We haven't set the stackwalker thread type flag yet (see next line), so it shouldn't be set. Only
    // exception to this is if the current call is made by a hijacking profiler which
    // redirected this thread while it was previously in the middle of another stack walk
    _ASSERTE(IsCalledAsynchronously() || !IsStackWalkerThread());

    // Remember that we're walking the stack.  This holder will reinstate the original
    // value of the stackwalker flag (from the thread type mask) in its destructor.
    StackWalkerWalkingThreadHolder threadStackWalking(pThreadToSnapshot);

    // This flag remembers if we reported a managed frame since the last unmanaged block
    // we reported. It's used to avoid reporting two unmanaged blocks in a row.
    BOOL fReportedAtLeastOneManagedFrameSinceLastUnmanagedBlock = FALSE;

    Frame * pFrameCur = pThreadToSnapshot->GetFrame();

    CONTEXT ctxCur;
    ZeroMemory(&ctxCur, sizeof(ctxCur));

    // Use seed if we got one.  Otherwise, EE explicit Frame chain will seed the walk.
    if (pctxSeed != NULL)
    {
        ctxCur.Ebp = pctxSeed->Ebp;
        ctxCur.Eip = pctxSeed->Eip;
        ctxCur.Esp = pctxSeed->Esp;
    }

    while (TRUE)
    {
        // At each iteration of the loop:
        //     * Analyze current frame (get managed data if it's a managed frame)
        //     * Report current frame via callback()
        //     * Walk down to next frame

        // **** Managed or unmanaged frame? ****

        EECodeInfo codeInfo;
        MethodDesc * pMethodDescCur = NULL;

        if (ctxCur.Eip != 0)
        {
            hr = GetFunctionInfoInternal(
                (LPCBYTE) ctxCur.Eip,
                &codeInfo);
            if (hr == CORPROF_E_ASYNCHRONOUS_UNSAFE)
            {
                _ASSERTE(ShouldAvoidHostCalls());
                return hr;
            }
            if (SUCCEEDED(hr))
            {
                pMethodDescCur = codeInfo.GetMethodDesc();
            }
        }

        // **** Report frame to profiler ****

        if (
            // Make sure the frame gave us an IP
            (ctxCur.Eip != 0) &&

            // Make sure any managed frame isn't for an IL stub or LCG
            ((pMethodDescCur == NULL) || !pMethodDescCur->IsNoMetadata()) &&

            // Only report unmanaged frames if the last frame we reported was managed
            // (avoid reporting two unmanaged blocks in a row)
            ((pMethodDescCur != NULL) || fReportedAtLeastOneManagedFrameSinceLastUnmanagedBlock))
        {
            // Around the call to the profiler, temporarily clear the
            // ThreadType_StackWalker type flag, as we have no control over what the
            // profiler may do inside its callback (it could theoretically attempt to
            // load other types, though I don't personally know of profilers that
            // currently do this).

            CLEAR_THREAD_TYPE_STACKWALKER();
            hr = callback(
                (FunctionID) pMethodDescCur,
                ctxCur.Eip,
                NULL,               // COR_PRF_FRAME_INFO
                sizeof(ctxCur),     // contextSize,
                (LPBYTE) &ctxCur,   // context,
                clientData);
            SET_THREAD_TYPE_STACKWALKER(pThreadToSnapshot);

            if (hr != S_OK)
            {
                return hr;
            }
            if (pMethodDescCur == NULL)
            {
                // Just reported an unmanaged block, so reset the flag
                fReportedAtLeastOneManagedFrameSinceLastUnmanagedBlock = FALSE;
            }
            else
            {
                // Just reported a managed block, so remember it
                fReportedAtLeastOneManagedFrameSinceLastUnmanagedBlock = TRUE;
            }
        }

        // **** Walk down to next frame ****

        // Is current frame managed or unmanaged?
        if (pMethodDescCur == NULL)
        {
            // Unmanaged frame.  Use explicit EE Frame chain to help

            REGDISPLAY frameRD;
            ZeroMemory(&frameRD, sizeof(frameRD));

            while (pFrameCur != FRAME_TOP)
            {
                // Frame is only useful if it will contain register context info
                if (!pFrameCur->NeedsUpdateRegDisplay())
                {
                    goto Loop;
                }


                // This should be the first call we make to the Frame, as it will
                // ensure we force lazy initialize of HelperMethodFrames
                if (!EnsureFrameInitialized(pFrameCur))
                {
                    return CORPROF_E_ASYNCHRONOUS_UNSAFE;
                }

                // This frame is only useful if it gives us an actual return address,
                // and is situated on the stack at or below our current ESP (stack
                // grows up)
                if ((pFrameCur->GetReturnAddress() != NULL) &&
                    (dac_cast<TADDR>(pFrameCur) >= dac_cast<TADDR>(ctxCur.Esp)))
                {
                    pFrameCur->UpdateRegDisplay(&frameRD);
                    break;
                }

Loop:
                pFrameCur = pFrameCur->PtrNextFrame();
            }

            if (pFrameCur == FRAME_TOP)
            {
                // No more frames.  Stackwalk is over
                return S_OK;
            }

            // Update ctxCur based on frame
            ctxCur.Eip = pFrameCur->GetReturnAddress();
            ctxCur.Ebp = GetRegdisplayFP(&frameRD);
            ctxCur.Esp = GetRegdisplaySP(&frameRD);
        }
        else
        {
            // Managed frame.

            // GC info will assist us in determining whether this is a non-EBP frame and
            // info about pushed arguments.
            GCInfoToken gcInfoToken = codeInfo.GetGCInfoToken();
            PTR_VOID gcInfo = gcInfoToken.Info;
            InfoHdr header;
            unsigned uiMethodSizeDummy;
            PTR_CBYTE table = PTR_CBYTE(gcInfo);
            table += decodeUnsigned(table, &uiMethodSizeDummy);
            table = decodeHeader(table, gcInfoToken.Version, &header);

            // Ok, GCInfo, can we do a simple EBP walk or what?

            if ((codeInfo.GetRelOffset() < header.prologSize) ||
                (!header.ebpFrame && !header.doubleAlign))
            {
                // We're either in the prolog or we're not in an EBP frame, in which case
                // we'll just defer to the code manager to unwind for us. This condition
                // is relatively rare, but can occur if:
                //
                //     * The profiler did a DSS from its Enter hook, in which case we're
                //         still inside the prolog, OR
                //     * The seed context or explicit EE Frame chain seeded us with a
                //         non-EBP frame function. In this case, using a naive EBP
                //         unwinding algorithm would actually skip over the next EBP
                //         frame, and would get SP all wrong as we try skipping over
                //         the pushed parameters. So let's just ask the code manager for
                //         help.
                //
                // Note that there are yet more conditions (much more rare) where the EBP
                // walk could get lost (e.g., we're inside an epilog). But we only care
                // about the most likely cases, and it's ok if the unlikely cases result
                // in truncated stacks, as unlikely cases will be statistically
                // irrelevant to CPU performance sampling profilers
                CodeManState codeManState;
                codeManState.dwIsSet = 0;
                REGDISPLAY rd;
                FillRegDisplay(&rd, &ctxCur);

                rd.SetEbpLocation(&ctxCur.Ebp);
                rd.SP = ctxCur.Esp;
                rd.ControlPC = ctxCur.Eip;

                codeInfo.GetCodeManager()->UnwindStackFrame(
                    &rd,
                    &codeInfo,
                    SpeculativeStackwalk,
                    &codeManState,
                    NULL);

                ctxCur.Ebp = *rd.GetEbpLocation();
                ctxCur.Esp = rd.SP;
                ctxCur.Eip = rd.ControlPC;
            }
            else
            {
                // We're in an actual EBP frame, so we can simplistically walk down to
                // the next frame using EBP.

                // Return address is stored just below saved EBP (stack grows up)
                ctxCur.Eip = *(DWORD *) (ctxCur.Ebp + sizeof(DWORD));

                ctxCur.Esp =
                    // Stack location where current function pushed its EBP
                    ctxCur.Ebp +

                    // Skip past that EBP
                    sizeof(DWORD) +

                    // Skip past return address pushed by caller
                    sizeof(DWORD) +

                    // Skip past arguments to current function that were pushed by caller.
                    // (Caller will pop varargs, so don't count those.)
                    (header.varargs ? 0 : (header.argCount * sizeof(DWORD)));

                // EBP for frame below us (stack grows up) has been saved onto our own
                // frame. Dereference it now.
                ctxCur.Ebp = *(DWORD *) ctxCur.Ebp;
            }
        }
    }
}
#endif // TARGET_X86

//*****************************************************************************
//  The profiler stackwalk Wrapper
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::ProfilerStackWalkFramesWrapper(Thread * pThreadToSnapshot, PROFILER_STACK_WALK_DATA * pData, unsigned flags)
{
    STATIC_CONTRACT_WRAPPER;

    StackWalkAction swaRet = pThreadToSnapshot->StackWalkFrames(
        (PSTACKWALKFRAMESCALLBACK)ProfilerStackWalkCallback,
         pData,
         flags,
         NULL);

    switch (swaRet)
    {
    default:
        _ASSERTE(!"Unexpected StackWalkAction returned from Thread::StackWalkFrames");
        return E_FAIL;

    case SWA_FAILED:
        return E_FAIL;

    case SWA_ABORT:
        return CORPROF_E_STACKSNAPSHOT_ABORTED;

    case SWA_DONE:
        return S_OK;
    }
}

//---------------------------------------------------------------------------------------
//
// DoStackSnapshot helper to call FindJitMan to determine if the specified
// context is in managed code.
//
// Arguments:
//      pCtx - Context to look at
//      hostCallPreference - Describes how to acquire the reader lock--either AllowHostCalls
//          or NoHostCalls (see code:HostCallPreference).
//
// Return Value:
//      S_OK: The context is in managed code
//      S_FALSE: The context is not in managed code.
//      Error: Unable to determine (typically because hostCallPreference was NoHostCalls
//         and the reader lock was unattainable without yielding)
//

HRESULT IsContextInManagedCode(const CONTEXT * pCtx, HostCallPreference hostCallPreference)
{
    WRAPPER_NO_CONTRACT;
    BOOL fFailedReaderLock = FALSE;

    // if there's no Jit Manager for the IP, it's not managed code.
    BOOL fIsManagedCode = ExecutionManager::IsManagedCode(GetIP(pCtx), hostCallPreference, &fFailedReaderLock);
    if (fFailedReaderLock)
    {
        return CORPROF_E_ASYNCHRONOUS_UNSAFE;
    }

    return fIsManagedCode ? S_OK : S_FALSE;
}

//*****************************************************************************
// Perform a stack walk, calling back to callback at each managed frame.
//*****************************************************************************
HRESULT ProfToEEInterfaceImpl::DoStackSnapshot(ThreadID thread,
                                              StackSnapshotCallback *callback,
                                              ULONG32 infoFlags,
                                              void *clientData,
                                               BYTE * pbContext,
                                              ULONG32 contextSize)
{
    CONTRACTL
    {
        // Yay!  (Note: NOTHROW is vital.  The throw at minimum allocates
        // the thrown object which we *must* not do.)
        NOTHROW;

        // Yay!  (Note: this is called asynchronously to view the stack at arbitrary times,
        // so the stack is not necessarily crawlable for GC at this state!)
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // #DisableLockOnAsyncCalls
        // This call is allowed asynchronously, however it does take locks.  Therefore,
        // we will hit contract asserts if we happen to be in a CANNOT_TAKE_LOCK zone when
        // a hijacking profiler hijacks this thread to run DoStackSnapshot.  CANNOT_RETAKE_LOCK
        // is a more granular locking contract that says "I promise that if I take locks, I
        // won't reenter any locks that were taken before this function was called".
        DISABLED(CAN_TAKE_LOCK);

        // Asynchronous functions can be called at arbitrary times when runtime
        // is holding locks that cannot be reentered without causing deadlock.
        // This contract detects any attempts to reenter locks held at the time
        // this function was called.
        CANNOT_RETAKE_LOCK;

    }
    CONTRACTL_END;

    // This CONTRACT_VIOLATION is still needed because DISABLED(CAN_TAKE_LOCK) does not
    // turn off contract violations.
    PERMANENT_CONTRACT_VIOLATION(TakesLockViolation, ReasonProfilerAsyncCannotRetakeLock);

    LPCONTEXT pctxSeed = reinterpret_cast<LPCONTEXT> (pbContext);

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: DoStackSnapshot 0x%p, 0x%p, 0x%08x, 0x%p, 0x%p, 0x%08x.\n",
        thread,
        callback,
        infoFlags,
        clientData,
        pctxSeed,
        contextSize));

    HRESULT hr = E_UNEXPECTED;
    // (hr assignment is to appease the compiler; we won't actually return without explicitly setting hr again)

    Thread *pThreadToSnapshot = NULL;
    Thread * pCurrentThread = GetThreadNULLOk();
    BOOL fResumeThread = FALSE;
    INDEBUG(ULONG ulForbidTypeLoad = 0;)
    BOOL fResetSnapshotThreadExternalCount = FALSE;
    int cRefsSnapshotThread = 0;

    // Remember whether we've already determined the current context of the target thread
    // is in managed (S_OK), not in managed (S_FALSE), or unknown (error).
    HRESULT hrCurrentContextIsManaged = E_FAIL;

    CONTEXT ctxCurrent;
    memset(&ctxCurrent, 0, sizeof(ctxCurrent));

    REGDISPLAY rd;

    PROFILER_STACK_WALK_DATA data;

    if (!g_fEEStarted )
    {
        // no managed code has run and things are likely in a very bad have loaded state
        // this is a bad time to try to walk the stack

        // Returning directly as there is nothing to cleanup yet
        return CORPROF_E_STACKSNAPSHOT_UNSAFE;
    }

    if (!CORProfilerStackSnapshotEnabled())
    {
        // Returning directly as there is nothing to cleanup yet, and can't skip gcholder ctor
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }

    if (thread == NULL)
    {
        pThreadToSnapshot = pCurrentThread;
    }
    else
    {
        pThreadToSnapshot = (Thread *)thread;
    }

#ifdef TARGET_X86
    if ((infoFlags & ~(COR_PRF_SNAPSHOT_REGISTER_CONTEXT | COR_PRF_SNAPSHOT_X86_OPTIMIZED)) != 0)
#else
    if ((infoFlags & ~(COR_PRF_SNAPSHOT_REGISTER_CONTEXT)) != 0)
#endif
    {
        // Returning directly as there is nothing to cleanup yet, and can't skip gcholder ctor
        return E_INVALIDARG;
    }

    if (!IsManagedThread(pThreadToSnapshot) || !IsGarbageCollectorFullyInitialized())
    {
        //
        // No managed frames, return now.
        //
        // Returning directly as there is nothing to cleanup yet, and can't skip gcholder ctor
        return S_OK;
    }

    // We must make sure no other thread tries to hijack the thread we're about to walk
    // Hijacking means Thread::HijackThread, i.e. bashing return addresses which would break the stack walk
    Thread::HijackLockHolder hijackLockHolder(pThreadToSnapshot);
    if (!hijackLockHolder.Acquired())
    {
        // Returning directly as there is nothing to cleanup yet, and can't skip gcholder ctor
        return CORPROF_E_STACKSNAPSHOT_UNSAFE;
    }

    if (pThreadToSnapshot != pCurrentThread         // Walking separate thread
        && pCurrentThread != NULL                         // Walker (current) thread is a managed / VM thread
        && ThreadSuspend::SysIsSuspendInProgress())          // EE is trying suspend itself
    {
        // Since we're walking a separate thread, we'd have to suspend it first (see below).
        // And since the current thread is a VM thread, that means the current thread's
        // m_dwForbidSuspendThread count will go up while it's trying to suspend the
        // target thread (see Thread::SuspendThread).  THAT means no one will be able
        // to suspend the current thread until its m_dwForbidSuspendThread is decremented
        // (which happens as soon as the target thread of DoStackSnapshot has been suspended).
        // Since we're in the process of suspending the entire runtime, now would be a bad time to
        // make the walker thread un-suspendable (see VsWhidbey bug 454936).  So let's just abort
        // now.  Note that there is no synchronization around calling Thread::SysIsSuspendInProgress().
        // So we will get occasional false positives or false negatives.  But that's benign, as the worst
        // that might happen is we might occasionally delay the EE suspension a little bit, or we might
        // too eagerly fail from ProfToEEInterfaceImpl::DoStackSnapshot sometimes.  But there won't
        // be any corruption or AV.
        //
        // Returning directly as there is nothing to cleanup yet, and can't skip gcholder ctor
        return CORPROF_E_STACKSNAPSHOT_UNSAFE;
    }

    // We only allow stackwalking if:
    // 1) Target thread to walk == current thread OR Target thread is suspended, AND
    // 2) Target thread to walk is currently executing JITted / NGENd code, AND
    // 3) Target thread to walk is seeded OR currently NOT unwinding the stack, AND
    // 4) Target thread to walk != current thread OR current thread is NOT in a can't stop or forbid suspend region

    // If the thread is in a forbid suspend region, it's dangerous to do anything:
    // - The code manager datastructures accessed during the stackwalk may be in inconsistent state.
    // - Thread::Suspend won't be able to suspend the thread.
    if (pThreadToSnapshot->IsInForbidSuspendRegion())
    {
        hr = CORPROF_E_STACKSNAPSHOT_UNSAFE;
        goto Cleanup;
    }

    HostCallPreference hostCallPreference;

    // First, check "1) Target thread to walk == current thread OR Target thread is suspended"
    if (pThreadToSnapshot != pCurrentThread && !g_profControlBlock.fProfilerRequestedRuntimeSuspend)
    {
#ifndef PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
        hr = E_NOTIMPL;
        goto Cleanup;
#else
        // Walking separate thread, so it must be suspended.  First, ensure that
        // target thread exists.
        //
        // NOTE: We're using the "dangerous" variant of this refcount function, because we
        // rely on the profiler to ensure it never tries to walk a thread being destroyed.
        // (Profiler must block in its ThreadDestroyed() callback until all uses of that thread,
        // such as walking its stack, are complete.)
        cRefsSnapshotThread = pThreadToSnapshot->IncExternalCountDANGEROUSProfilerOnly();
        fResetSnapshotThreadExternalCount = TRUE;

        if (cRefsSnapshotThread == 1 || !pThreadToSnapshot->HasValidThreadHandle())
        {
            // At this point, we've modified the VM state based on bad input
            // (pThreadToSnapshot) from the profiler.  This could cause
            // memory corruption and leave us vulnerable to security problems.
            // So destroy the process.
            _ASSERTE(!"Profiler trying to walk destroyed thread");
            EEPOLICY_HANDLE_FATAL_ERROR(CORPROF_E_STACKSNAPSHOT_INVALID_TGT_THREAD);
        }

        // Thread::SuspendThread() ensures that no one else should try to suspend us
        // while we're suspending pThreadToSnapshot.
        //
        // TRUE: OneTryOnly.  Don't loop waiting for others to get out of our way in
        // order to suspend the thread.  If it's not safe, just return an error immediately.
        Thread::SuspendThreadResult str = pThreadToSnapshot->SuspendThread(TRUE);
        if (str == Thread::STR_Success)
        {
            fResumeThread = TRUE;
        }
        else
        {
            hr = CORPROF_E_STACKSNAPSHOT_UNSAFE;
            goto Cleanup;
        }
#endif // !PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
    }

    hostCallPreference =
        ShouldAvoidHostCalls() ?
            NoHostCalls :       // Async call: Ensure this thread won't yield & re-enter host
            AllowHostCalls;     // Synchronous calls may re-enter host just fine

    // If target thread is in pre-emptive mode, the profiler's seed context is unnecessary
    // because our frame chain is good enough: it will give us at least as accurate a
    // starting point as the profiler could.  Also, since profiler contexts cannot be
    // trusted, we don't want to set the thread's profiler filter context to this, as a GC
    // that interrupts the profiler's stackwalk will end up using the profiler's (potentially
    // bogus) filter context.
    if (!pThreadToSnapshot->PreemptiveGCDisabledOther())
    {
        // Thread to be walked is in preemptive mode.  Throw out seed.
        pctxSeed = NULL;
    }
    else if (pThreadToSnapshot != pCurrentThread)
    {
    // With cross-thread stack-walks, the target thread's context could be unreliable.
    // That would shed doubt on either a profiler-provided context, or a default
    // context we chose.  So check if we're in a potentially unreliable case, and return
    // an error if so.
    //
    // These heurisitics are based on an actual bug where GetThreadContext returned a
    // self-consistent, but stale, context for a thread suspended after being redirected by
    // the GC (TFS Dev 10 bug # 733263).
        //
        // (Note that this whole block is skipped if pThreadToSnapshot is in preemptive mode (the IF
        // above), as the context is unused in such a case--the EE Frame chain is used
        // to seed the walk instead.)
#ifndef PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
        hr = E_NOTIMPL;
        goto Cleanup;
#else
        if (!pThreadToSnapshot->GetSafelyRedirectableThreadContext(Thread::kDefaultChecks, &ctxCurrent, &rd))
        {
            LOG((LF_CORPROF, LL_INFO100, "**PROF: GetSafelyRedirectableThreadContext failure leads to CORPROF_E_STACKSNAPSHOT_UNSAFE.\n"));
            hr = CORPROF_E_STACKSNAPSHOT_UNSAFE;
            goto Cleanup;
        }

        hrCurrentContextIsManaged = IsContextInManagedCode(&ctxCurrent, hostCallPreference);
        if (FAILED(hrCurrentContextIsManaged))
        {
            // Couldn't get the info.  Try again later
            _ASSERTE(ShouldAvoidHostCalls());
            hr = CORPROF_E_ASYNCHRONOUS_UNSAFE;
            goto Cleanup;
        }

        if ((hrCurrentContextIsManaged == S_OK) &&
            (!pThreadToSnapshot->PreemptiveGCDisabledOther()))
        {
            // Thread is in preemptive mode while executing managed code?!  This lie is
            // an early warning sign that the context is bogus.  Bail.
            LOG((LF_CORPROF, LL_INFO100, "**PROF: Target thread context is likely bogus.  Returning CORPROF_E_STACKSNAPSHOT_UNSAFE.\n"));
            hr = CORPROF_E_STACKSNAPSHOT_UNSAFE;
            goto Cleanup;
        }

        Frame * pFrame = pThreadToSnapshot->GetFrame();
        if (pFrame != FRAME_TOP)
        {
            TADDR spTargetThread = GetSP(&ctxCurrent);
            if (dac_cast<TADDR>(pFrame) < spTargetThread)
            {
                // An Explicit EE Frame is more recent on the stack than the current
                // stack pointer itself?  This lie is an early warning sign that the
                // context is bogus. Bail.
                LOG((LF_CORPROF, LL_INFO100, "**PROF: Target thread context is likely bogus.  Returning CORPROF_E_STACKSNAPSHOT_UNSAFE.\n"));
                hr = CORPROF_E_STACKSNAPSHOT_UNSAFE;
                goto Cleanup;
            }
        }

        // If the profiler did not specify a seed context of its own, use the current one we
        // just produced.
        //
        // Failing to seed the walk can cause us to to "miss" functions on the stack.  This is
        // because StackWalkFrames(), when doing an unseeded stackwalk, sets the
        // starting regdisplay's IP/SP to 0.  This, in turn causes StackWalkFramesEx
        // to set cf.isFrameless = (pEEJM != NULL); (which is FALSE, since we have no
        // jit manager, since we have no IP).  Once frameless is false, we look solely to
        // the Frame chain for our goodies, rather than looking at the code actually
        // being executed by the thread.  The problem with the frame chain is that some
        // frames (e.g., GCFrame) don't point to any functions being executed.  So
        // StackWalkFramesEx just skips such frames and moves to the next one.  That
        // can cause a chunk of calls to be skipped.  To prevent this from happening, we
        // "fake" a seed by just seeding the thread with its current context.  This forces
        // StackWalkFramesEx() to look at the IP rather than just the frame chain.
        if (pctxSeed == NULL)
        {
            pctxSeed = &ctxCurrent;
        }
#endif // !PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
    }

    // Second, check "2) Target thread to walk is currently executing JITted / NGENd code"
    // To do this, we need to find the proper context to investigate.  Start with
    // the seeded context, if available.  If not, use the target thread's current context.
    if (pctxSeed != NULL)
    {
        BOOL fSeedIsManaged;

        // Short cut: If we're just using the current context as the seed, we may
        // already have determined whether it's in managed code.  If so, just use that
        // result rather than calculating it again
        if ((pctxSeed == &ctxCurrent) && SUCCEEDED(hrCurrentContextIsManaged))
        {
            fSeedIsManaged = (hrCurrentContextIsManaged == S_OK);
        }
        else
        {
            hr = IsContextInManagedCode(pctxSeed, hostCallPreference);
            if (FAILED(hr))
            {
                hr = CORPROF_E_ASYNCHRONOUS_UNSAFE;
                goto Cleanup;
            }
            fSeedIsManaged = (hr == S_OK);
        }

        if (!fSeedIsManaged)
        {
            hr = CORPROF_E_STACKSNAPSHOT_UNMANAGED_CTX;
            goto Cleanup;
        }
    }

#ifdef _DEBUG
    //
    // Sanity check: If we are doing a cross-thread walk and there is no seed context, then
    // we better not be in managed code, otw we do not have a Frame on the stack from which to start
    // walking and we may miss the leaf-most chain of managed calls due to the way StackWalkFrames
    // is implemented.  However, there is an exception when the leaf-most EE frame of pThreadToSnapshot
    // is an InlinedCallFrame, which has an active call, implying pThreadToShanpshot is inside an
    // inlined P/Invoke.  In this case, the InlinedCallFrame will be used to help start off our
    // stackwalk at the top of the stack.
    //
    if (pThreadToSnapshot != pCurrentThread && !g_profControlBlock.fProfilerRequestedRuntimeSuspend)
    {
#ifndef PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
        hr = E_NOTIMPL;
        goto Cleanup;
#else
        if (pctxSeed == NULL)
        {
            if (pThreadToSnapshot->GetSafelyRedirectableThreadContext(Thread::kDefaultChecks, &ctxCurrent, &rd))
            {
                BOOL fFailedReaderLock = FALSE;
                BOOL fIsManagedCode = ExecutionManager::IsManagedCode(GetIP(&ctxCurrent), hostCallPreference, &fFailedReaderLock);

                if (!fFailedReaderLock)
                {
                    // not in jitted or ngend code or inside an inlined P/Invoke (the leaf-most EE Frame is
                    // an InlinedCallFrame with an active call)
                    _ASSERTE(!fIsManagedCode ||
                             (InlinedCallFrame::FrameHasActiveCall(pThreadToSnapshot->GetFrame())));
                }
            }
        }
#endif // !PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
    }
#endif //_DEBUG
    // Third, verify the target thread is seeded or not in the midst of an unwind.
    if (pctxSeed == NULL)
    {
        ThreadExceptionState* pExState = pThreadToSnapshot->GetExceptionState();

        // this tests to see if there is an exception in flight
        if (pExState->IsExceptionInProgress() && pExState->GetFlags()->UnwindHasStarted())
        {
            EHClauseInfo *pCurrentEHClauseInfo = pThreadToSnapshot->GetExceptionState()->GetCurrentEHClauseInfo();

            // if the exception code is telling us that we have entered a managed context then all is well
            if (!pCurrentEHClauseInfo->IsManagedCodeEntered())
            {
                hr = CORPROF_E_STACKSNAPSHOT_UNMANAGED_CTX;
                goto Cleanup;
            }
        }
    }

    // Check if the exception state is consistent.  See the comment for ThreadExceptionFlag for more information.
    if (pThreadToSnapshot->GetExceptionState()->HasThreadExceptionFlag(ThreadExceptionState::TEF_InconsistentExceptionState))
    {
        hr = CORPROF_E_STACKSNAPSHOT_UNSAFE;
        goto Cleanup;
    }

    data.callback = callback;
    data.infoFlags = infoFlags;
    data.contextFlags = 0;
    data.clientData = clientData;
#ifdef FEATURE_EH_FUNCLETS
    data.sfParent.Clear();
#endif

    // workaround: The ForbidTypeLoad book keeping in the stackwalker is not robust against exceptions.
    // Unfortunately, it is hard to get it right in the stackwalker since it has to be exception
    // handling free (frame unwinding may never return). We restore the ForbidTypeLoad counter here
    // in case it got messed up by exception thrown during the stackwalk.
    INDEBUG(if (pCurrentThread) ulForbidTypeLoad = pCurrentThread->m_ulForbidTypeLoad;)

    {
        // An AV during a profiler stackwalk is an isolated event and shouldn't bring
        // down the runtime.  Need to place the holder here, outside of ProfilerStackWalkFramesWrapper
        // since ProfilerStackWalkFramesWrapper uses __try, which doesn't like objects
        // with destructors.
        AVInRuntimeImplOkayHolder AVOkay;

        DWORD asyncFlags = 0;
        if (pThreadToSnapshot != pCurrentThread)
        {
            asyncFlags |= ALLOW_ASYNC_STACK_WALK;
            if (!g_profControlBlock.fProfilerRequestedRuntimeSuspend)
            {
                // THREAD_IS_SUSPENDED signals to the stack walker that the
                // thread is interrupted at an arbitrary point and to be careful
                // not to cause a deadlock. If the profiler suspended the runtime,
                // then we know the threads are at a safe place and we can walk all the threads.
                asyncFlags |= THREAD_IS_SUSPENDED;
            }
        }

        hr = DoStackSnapshotHelper(
                 pThreadToSnapshot,
                 &data,
                 HANDLESKIPPEDFRAMES |
                     FUNCTIONSONLY |
                     NOTIFY_ON_U2M_TRANSITIONS |
                     asyncFlags |
                     THREAD_EXECUTING_MANAGED_CODE |
                     PROFILER_DO_STACK_SNAPSHOT |
                     ALLOW_INVALID_OBJECTS, // stack walk logic should not look at objects - we could be in the middle of a gc.
                 pctxSeed);
    }

    INDEBUG(if (pCurrentThread) pCurrentThread->m_ulForbidTypeLoad = ulForbidTypeLoad;)

Cleanup:
#if defined(PLATFORM_SUPPORTS_SAFE_THREADSUSPEND)
    if (fResumeThread)
    {
        pThreadToSnapshot->ResumeThread();
    }
#endif // PLATFORM_SUPPORTS_SAFE_THREADSUSPEND
    if (fResetSnapshotThreadExternalCount)
    {
        pThreadToSnapshot->DecExternalCountDANGEROUSProfilerOnly();
    }

    return hr;
}


//---------------------------------------------------------------------------------------
//
// Exception swallowing wrapper around the profiler stackwalk
//
// Arguments:
//      pThreadToSnapshot - Thread whose stack should be walked
//      pData - data for stack walker
//      flags - flags parameter to pass to StackWalkFramesEx, and StackFrameIterator
//      pctxSeed - Register context with which to seed the walk
//
// Return Value:
//     HRESULT indicating success or failure.
//
HRESULT ProfToEEInterfaceImpl::DoStackSnapshotHelper(Thread * pThreadToSnapshot,
                                                     PROFILER_STACK_WALK_DATA * pData,
                                                     unsigned flags,
                                                     LPCONTEXT pctxSeed)
{
    STATIC_CONTRACT_NOTHROW;

    // We want to catch and swallow AVs here. For example, if the profiler gives
    // us a bogus seed context (this happens), we could AV when inspecting memory pointed to
    // by the (bogus) EBP register.
    //
    // EX_TRY/EX_CATCH does a lot of extras that we do not need and that can go wrong for us.
    // E.g. It asserts in debug build for AVs in mscorwks or it synthetizes an object for the exception.
    // We use a plain PAL_TRY/PAL_EXCEPT since it is all we need.
    struct Param {
        HRESULT                     hr;
        Thread *                    pThreadToSnapshot;
        PROFILER_STACK_WALK_DATA *  pData;
        unsigned                    flags;
        ProfToEEInterfaceImpl *     pProfToEE;
        LPCONTEXT                   pctxSeed;
        BOOL                        fResetProfilerFilterContext;
    };

    Param param;
    param.hr = E_UNEXPECTED;
    param.pThreadToSnapshot = pThreadToSnapshot;
    param.pData = pData;
    param.flags = flags;
    param.pProfToEE = this;
    param.pctxSeed = pctxSeed;
    param.fResetProfilerFilterContext = FALSE;

    PAL_TRY(Param *, pParam, &param)
    {
        if ((pParam->pData->infoFlags & COR_PRF_SNAPSHOT_X86_OPTIMIZED) != 0)
        {
#ifndef TARGET_X86
            // If check in the beginning of DoStackSnapshot (to return E_INVALIDARG) should
            // make this unreachable
            _ASSERTE(!"COR_PRF_SNAPSHOT_X86_OPTIMIZED on non-X86 should be unreachable!");
#else
            // New, simple EBP walker
            pParam->hr = pParam->pProfToEE->ProfilerEbpWalker(
                             pParam->pThreadToSnapshot,
                             pParam->pctxSeed,
                             pParam->pData->callback,
                             pParam->pData->clientData);
#endif  // TARGET_X86
        }
        else
        {
            // We're now fairly confident the stackwalk should be ok, so set
            // the context seed, if one was provided or cooked up.
            if (pParam->pctxSeed != NULL)
            {
                pParam->pThreadToSnapshot->SetProfilerFilterContext(pParam->pctxSeed);
                pParam->fResetProfilerFilterContext = TRUE;
            }

            // Whidbey-style walker, uses StackWalkFramesEx
            pParam->hr = pParam->pProfToEE->ProfilerStackWalkFramesWrapper(
                             pParam->pThreadToSnapshot,
                             pParam->pData,
                             pParam->flags);
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        param.hr = E_UNEXPECTED;
    }
    PAL_ENDTRY;

    // Undo the context seeding & thread suspend we did (if any)
    // to ensure that the thread we walked stayed suspended
    if (param.fResetProfilerFilterContext)
    {
        pThreadToSnapshot->SetProfilerFilterContext(NULL);
    }

    return param.hr;
}


HRESULT ProfToEEInterfaceImpl::GetGenerationBounds(ULONG cObjectRanges,
                                                   ULONG *pcObjectRanges,
                                                   COR_PRF_GC_GENERATION_RANGE ranges[])
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(CheckPointer(pcObjectRanges));
        PRECONDITION(cObjectRanges <= 0 || ranges != NULL);
        PRECONDITION(s_generationTableLock >= 0);
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetGenerationBounds.\n"));

    // Announce we are using the generation table now
    CounterHolder genTableLock(&s_generationTableLock);

    GenerationTable *generationTable = s_currentGenerationTable;

    if (generationTable == NULL)
    {
        return E_FAIL;
    }

    _ASSERTE(generationTable->magic == GENERATION_TABLE_MAGIC);

    GenerationDesc *genDescTable = generationTable->genDescTable;
    ULONG count = min(generationTable->count, cObjectRanges);
    for (ULONG i = 0; i < count; i++)
    {
        ranges[i].generation          = (COR_PRF_GC_GENERATION)genDescTable[i].generation;
        ranges[i].rangeStart          = (ObjectID)genDescTable[i].rangeStart;
        ranges[i].rangeLength         = genDescTable[i].rangeEnd         - genDescTable[i].rangeStart;
        ranges[i].rangeLengthReserved = genDescTable[i].rangeEndReserved - genDescTable[i].rangeStart;
    }

    *pcObjectRanges = generationTable->count;

    return S_OK;
}


HRESULT ProfToEEInterfaceImpl::GetNotifiedExceptionClauseInfo(COR_PRF_EX_CLAUSE_INFO * pinfo)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(CheckPointer(pinfo));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC((LF_CORPROF,
                                     LL_INFO1000,
                                     "**PROF: GetNotifiedExceptionClauseInfo.\n"));

    HRESULT hr = S_OK;

    ThreadExceptionState* pExState             = NULL;
    EHClauseInfo*         pCurrentEHClauseInfo = NULL;

    // notification requires that we are on a managed thread with an exception in flight
    Thread *pThread = GetThreadNULLOk();

    // If pThread is null, then the thread has never run managed code
    if (pThread == NULL)
    {
        hr = CORPROF_E_NOT_MANAGED_THREAD;
        goto NullReturn;
    }

    pExState = pThread->GetExceptionState();
    if (!pExState->IsExceptionInProgress())
    {
        // no exception is in flight -- successful failure
        hr = S_FALSE;
        goto NullReturn;
    }

    pCurrentEHClauseInfo = pExState->GetCurrentEHClauseInfo();
    if (pCurrentEHClauseInfo->GetClauseType() == COR_PRF_CLAUSE_NONE)
    {
        // no exception is in flight -- successful failure
        hr = S_FALSE;
        goto NullReturn;
    }

    pinfo->clauseType     = pCurrentEHClauseInfo->GetClauseType();
    pinfo->programCounter = pCurrentEHClauseInfo->GetIPForEHClause();
    pinfo->framePointer   = pCurrentEHClauseInfo->GetFramePointerForEHClause();
    pinfo->shadowStackPointer = 0;

    return S_OK;

NullReturn:
    memset(pinfo, 0, sizeof(*pinfo));
    return hr;
}


HRESULT ProfToEEInterfaceImpl::GetObjectGeneration(ObjectID objectId,
                                                   COR_PRF_GC_GENERATION_RANGE *range)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;


        PRECONDITION(objectId != NULL);
        PRECONDITION(CheckPointer(range));
        PRECONDITION(s_generationTableLock >= 0);
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
                                       (LF_CORPROF,
                                       LL_INFO1000,
                                       "**PROF: GetObjectGeneration 0x%p.\n",
                                       objectId));


    _ASSERTE((GetThreadNULLOk() == NULL) || (GetThreadNULLOk()->PreemptiveGCDisabled()));


    // Announce we are using the generation table now
    CounterHolder genTableLock(&s_generationTableLock);

    GenerationTable *generationTable = s_currentGenerationTable;

    if (generationTable == NULL)
    {
        return E_FAIL;
    }

    _ASSERTE(generationTable->magic == GENERATION_TABLE_MAGIC);

    GenerationDesc *genDescTable = generationTable->genDescTable;
    ULONG count = generationTable->count;
    for (ULONG i = 0; i < count; i++)
    {
        if (genDescTable[i].rangeStart <= (BYTE *)objectId && (BYTE *)objectId < genDescTable[i].rangeEndReserved)
        {
            range->generation          = (COR_PRF_GC_GENERATION)genDescTable[i].generation;
            range->rangeStart          = (ObjectID)genDescTable[i].rangeStart;
            range->rangeLength         = genDescTable[i].rangeEnd         - genDescTable[i].rangeStart;
            range->rangeLengthReserved = genDescTable[i].rangeEndReserved - genDescTable[i].rangeStart;

            return S_OK;
        }
    }

    return E_FAIL;
}

HRESULT ProfToEEInterfaceImpl::GetReJITIDs(
                           FunctionID          functionId,  // in
                           ULONG               cReJitIds,   // in
                           ULONG *             pcReJitIds,  // out
                           ReJITID             reJitIds[])  // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // taking a lock causes a GC
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // The rejit tables use a lock
        CAN_TAKE_LOCK;

        PRECONDITION(CheckPointer(pcReJitIds, NULL_OK));
        PRECONDITION(CheckPointer(reJitIds, NULL_OK));
        PRECONDITION((cReJitIds == 0) == (reJitIds == NULL));

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetReJITIDs 0x%p.\n",
         functionId));

    if (functionId == 0)
    {
        return E_INVALIDARG;
    }

    if ((pcReJitIds == NULL) || ((cReJitIds != 0) && (reJitIds == NULL)))
    {
        return E_INVALIDARG;
    }

    MethodDesc * pMD = FunctionIdToMethodDesc(functionId);

    return ReJitManager::GetReJITIDs(pMD, cReJitIds, pcReJitIds, reJitIds);
}


HRESULT ProfToEEInterfaceImpl::SetupThreadForReJIT()
{
    LIMITED_METHOD_CONTRACT;

    Thread* pThread = GetThreadNULLOk();
    if (pThread == NULL)
    {
        HRESULT hr = S_OK;
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
            return hr;
    }

    pThread->SetProfilerCallbackStateFlags(COR_PRF_CALLBACKSTATE_REJIT_WAS_CALLED);
    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::RequestReJIT(ULONG       cFunctions,   // in
                                            ModuleID    moduleIds[],  // in
                                            mdMethodDef methodIds[])  // in
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // When we suspend the runtime we drop into preemptive mode
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // We need to suspend the runtime, this takes a lot of locks!
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(moduleIds, NULL_OK));
        PRECONDITION(CheckPointer(methodIds, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EETriggers | kP2EEAllowableAfterAttach,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: RequestReJIT.\n"));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    if (!m_pProfilerInfo->pProfInterface->IsCallback4Supported())
    {
        return CORPROF_E_CALLBACK4_REQUIRED;
    }

    if (!CORProfilerEnableRejit())
    {
        return CORPROF_E_REJIT_NOT_ENABLED;
    }

    // Request at least 1 method to reJIT!
    if ((cFunctions == 0) || (moduleIds == NULL) || (methodIds == NULL))
    {
        return E_INVALIDARG;
    }

    // Remember the profiler is doing this, as that means we must never detach it!
    g_profControlBlock.mainProfilerInfo.pProfInterface->SetUnrevertiblyModifiedILFlag();

    HRESULT hr = SetupThreadForReJIT();
    if (FAILED(hr))
    {
        return hr;
    }

    GCX_PREEMP();
    return ReJitManager::RequestReJIT(cFunctions, moduleIds, methodIds, static_cast<COR_PRF_REJIT_FLAGS>(0));
}

HRESULT ProfToEEInterfaceImpl::RequestRevert(ULONG       cFunctions,  // in
                                             ModuleID    moduleIds[], // in
                                             mdMethodDef methodIds[], // in
                                             HRESULT     rgHrStatuses[])    // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // The rejit manager requires a lock to iterate through methods to revert, and
        // taking the lock can drop us into preemptive mode.
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // The rejit manager requires a lock to iterate through methods to revert
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(moduleIds, NULL_OK));
        PRECONDITION(CheckPointer(methodIds, NULL_OK));
        PRECONDITION(CheckPointer(rgHrStatuses, NULL_OK));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
         LL_INFO1000,
         "**PROF: RequestRevert.\n"));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    if (!CORProfilerEnableRejit())
    {
        return CORPROF_E_REJIT_NOT_ENABLED;
    }

    // Request at least 1 method to revert!
    if ((cFunctions == 0) || (moduleIds == NULL) || (methodIds == NULL))
    {
        return E_INVALIDARG;
    }

    // Remember the profiler is doing this, as that means we must never detach it!
    g_profControlBlock.mainProfilerInfo.pProfInterface->SetUnrevertiblyModifiedILFlag();

    // Initialize the status array
    if (rgHrStatuses != NULL)
    {
        memset(rgHrStatuses, 0, sizeof(HRESULT) * cFunctions);
        _ASSERTE(S_OK == rgHrStatuses[0]);
    }

    HRESULT hr = SetupThreadForReJIT();
    if (FAILED(hr))
    {
        return hr;
    }

    GCX_PREEMP();
    return ReJitManager::RequestRevert(cFunctions, moduleIds, methodIds, rgHrStatuses);
}


HRESULT ProfToEEInterfaceImpl::EnumJITedFunctions(ICorProfilerFunctionEnum ** ppEnum)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // If we're in preemptive mode we need to take a read lock to safely walk
        // the JIT data structures.
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(ppEnum, NULL_OK));

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO10,
        "**PROF: EnumJITedFunctions.\n"));

    if (ppEnum == NULL)
    {
        return E_INVALIDARG;
    }

    *ppEnum = NULL;

    NewHolder<ProfilerFunctionEnum> pJitEnum(new (nothrow) ProfilerFunctionEnum());
    if (pJitEnum == NULL)
    {
        return E_OUTOFMEMORY;
    }

    if (!pJitEnum->Init())
    {
        return E_OUTOFMEMORY;
    }

    // Ownership transferred to [out] param.  Caller must Release() when done with this.
    *ppEnum = (ICorProfilerFunctionEnum *)pJitEnum.Extract();

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::EnumJITedFunctions2(ICorProfilerFunctionEnum ** ppEnum)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Gathering rejitids requires taking a lock and that lock might switch to
        // preemptimve mode...
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // If we're in preemptive mode we need to take a read lock to safely walk
        // the JIT data structures.
        // Gathering RejitIDs also takes a lock.
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(ppEnum, NULL_OK));

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO10,
        "**PROF: EnumJITedFunctions.\n"));

    if (ppEnum == NULL)
    {
        return E_INVALIDARG;
    }

    *ppEnum = NULL;

    NewHolder<ProfilerFunctionEnum> pJitEnum(new (nothrow) ProfilerFunctionEnum());
    if (pJitEnum == NULL)
    {
        return E_OUTOFMEMORY;
    }

    if (!pJitEnum->Init(TRUE /* fWithReJITIDs */))
    {
        // If it fails, it's because of OOM.
        return E_OUTOFMEMORY;
    }

    // Ownership transferred to [out] param.  Caller must Release() when done with this.
    *ppEnum = (ICorProfilerFunctionEnum *)pJitEnum.Extract();

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::EnumModules(ICorProfilerModuleEnum ** ppEnum)
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // This method populates the enumerator, which requires iterating over
        // AppDomains, which adds, then releases, a reference on each AppDomain iterated.
        // This causes locking, and can cause triggering if the AppDomain gets destroyed
        // as a result of the release. (See code:AppDomainIterator::Next and its call to
        // code:AppDomain::Release.)
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // (See comment above GC_TRIGGERS.)
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(ppEnum, NULL_OK));

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO10,
        "**PROF: EnumModules.\n"));

    HRESULT hr;

    if (ppEnum == NULL)
    {
        return E_INVALIDARG;
    }

    *ppEnum = NULL;

    // ProfilerModuleEnum uese AppDomainIterator, which cannot be called while the current thead
    // is holding the ThreadStore lock.
    if (ThreadStore::HoldingThreadStore())
    {
        return CORPROF_E_UNSUPPORTED_CALL_SEQUENCE;
    }

    NewHolder<ProfilerModuleEnum> pModuleEnum(new (nothrow) ProfilerModuleEnum);
    if (pModuleEnum == NULL)
    {
        return E_OUTOFMEMORY;
    }

    hr = pModuleEnum->Init();
    if (FAILED(hr))
    {
        return hr;
    }

    // Ownership transferred to [out] param.  Caller must Release() when done with this.
    *ppEnum = (ICorProfilerModuleEnum *) pModuleEnum.Extract();

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::GetRuntimeInformation(USHORT * pClrInstanceId,
                                                     COR_PRF_RUNTIME_TYPE * pRuntimeType,
                                                     USHORT * pMajorVersion,
                                                     USHORT * pMinorVersion,
                                                     USHORT * pBuildNumber,
                                                     USHORT * pQFEVersion,
                                                     ULONG  cchVersionString,
                                                     ULONG  * pcchVersionString,
                                                     __out_ecount_part_opt(cchVersionString, *pcchVersionString) WCHAR  szVersionString[])
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Yay!
        CANNOT_TAKE_LOCK;

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: GetRuntimeInformation.\n"));

    if ((szVersionString != NULL) && (pcchVersionString == NULL))
    {
        return E_INVALIDARG;
    }

    if (pcchVersionString != NULL)
    {
        PCWSTR pczVersionString = CLR_PRODUCT_VERSION_L;

        // Get the module file name
        ULONG trueLen = (ULONG)(wcslen(pczVersionString) + 1);

        // Return name of module as required.
        if (szVersionString && cchVersionString > 0)
        {
            ULONG copyLen = trueLen;

            if (copyLen >= cchVersionString)
            {
                copyLen = cchVersionString - 1;
            }

            wcsncpy_s(szVersionString, cchVersionString, pczVersionString, copyLen);
        }

        *pcchVersionString = trueLen;
    }

    if (pClrInstanceId != NULL)
        *pClrInstanceId = static_cast<USHORT>(GetClrInstanceId());

    if (pRuntimeType != NULL)
        *pRuntimeType = COR_PRF_CORE_CLR;

    if (pMajorVersion != NULL)
        *pMajorVersion = RuntimeProductMajorVersion;

    if (pMinorVersion != NULL)
        *pMinorVersion = RuntimeProductMinorVersion;

    if (pBuildNumber != NULL)
        *pBuildNumber = RuntimeProductPatchVersion;

    if (pQFEVersion != NULL)
        *pQFEVersion = 0;

    return S_OK;
}


HRESULT ProfToEEInterfaceImpl::RequestProfilerDetach(DWORD dwExpectedCompletionMilliseconds)
{
    CONTRACTL
    {
       // Yay!
        NOTHROW;

        // Crst is used in ProfilingAPIDetach::RequestProfilerDetach so GC may be triggered
        GC_TRIGGERS;

        // Yay!
        MODE_ANY;

        // Yay!
        EE_THREAD_NOT_REQUIRED;

        // Crst is used in ProfilingAPIDetach::RequestProfilerDetach
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_ASYNC_EX(
        kP2EEAllowableAfterAttach | kP2EETriggers,
        (LF_CORPROF,
        LL_INFO1000,
        "**PROF: RequestProfilerDetach.\n"));

#ifdef FEATURE_PROFAPI_ATTACH_DETACH
    ProfilerInfo *pProfilerInfo = g_profControlBlock.GetProfilerInfo(this);
    _ASSERTE(pProfilerInfo != NULL);
    return ProfilingAPIDetach::RequestProfilerDetach(pProfilerInfo, dwExpectedCompletionMilliseconds);
#else // FEATURE_PROFAPI_ATTACH_DETACH
    return E_NOTIMPL;
#endif // FEATURE_PROFAPI_ATTACH_DETACH
}

typedef struct _COR_PRF_ELT_INFO_INTERNAL
{
    // Point to a platform dependent structure ASM helper push on the stack
    void * platformSpecificHandle;

    // startAddress of COR_PRF_FUNCTION_ARGUMENT_RANGE structure needs to point
    // TO the argument value, not BE the argument value.  So, when the argument
    // is this, we need to point TO this.  Because of the calling sequence change
    // in ELT3, we need to reserve the pointer here instead of using one of our
    // stack variables.
    void * pThis;

    // Reserve space for output parameter COR_PRF_FRAME_INFO of
    // GetFunctionXXXX3Info functions
    COR_PRF_FRAME_INFO_INTERNAL frameInfo;

} COR_PRF_ELT_INFO_INTERNAL;

//---------------------------------------------------------------------------------------
//
// ProfilingGetFunctionEnter3Info provides frame information and argument infomation of
// the function ELT callback is inspecting.  It is called either by the profiler or the
// C helper function.
//
// Arguments:
//      * functionId - [in] FunctionId of the function being inspected by ELT3
//      * eltInfo - [in] The opaque pointer FunctionEnter3WithInfo callback passed to the profiler
//      * pFrameInfo - [out] Pointer to COR_PRF_FRAME_INFO the profiler later can use to inspect
//                     generic types
//      * pcbArgumentInfo - [in, out] Pointer to ULONG that specifies the size of structure
//                          pointed by pArgumentInfo
//      * pArgumentInfo - [out] Pointer to COR_PRF_FUNCTION_ARGUMENT_INFO structure the profiler
//                        must preserve enough space for the function it is inspecting
//
// Return Value:
//    HRESULT indicating success or failure.
//

HRESULT ProfilingGetFunctionEnter3Info(FunctionID functionId,                              // in
                                       COR_PRF_ELT_INFO eltInfo,                           // in
                                       COR_PRF_FRAME_INFO * pFrameInfo,                    // out
                                       ULONG * pcbArgumentInfo,                            // in, out
                                       COR_PRF_FUNCTION_ARGUMENT_INFO * pArgumentInfo)     // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // ProfileArgIterator::ProfileArgIterator may take locks
        CAN_TAKE_LOCK;


    }
    CONTRACTL_END;

    if ((functionId == NULL) || (eltInfo == NULL))
    {
        return E_INVALIDARG;
    }

    COR_PRF_ELT_INFO_INTERNAL * pELTInfo = (COR_PRF_ELT_INFO_INTERNAL *)eltInfo;
    ProfileSetFunctionIDInPlatformSpecificHandle(pELTInfo->platformSpecificHandle, functionId);

    // The loader won't trigger a GC or throw for already loaded argument types.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    //
    // Find the method this is referring to, so we can get the signature
    //
    MethodDesc * pMethodDesc = FunctionIdToMethodDesc(functionId);
    MetaSig metaSig(pMethodDesc);

    NewHolder<ProfileArgIterator> pProfileArgIterator;

    {
        // Can handle E_OUTOFMEMORY from ProfileArgIterator.
        FAULT_NOT_FATAL();

        pProfileArgIterator = new (nothrow) ProfileArgIterator(&metaSig, pELTInfo->platformSpecificHandle);

        if (pProfileArgIterator == NULL)
        {
            return E_UNEXPECTED;
        }
    }

    if (CORProfilerFrameInfoEnabled())
    {
        if (pFrameInfo == NULL)
        {
            return E_INVALIDARG;
        }

        //
        // Setup the COR_PRF_FRAME_INFO structure first.
        //
        COR_PRF_FRAME_INFO_INTERNAL * pCorPrfFrameInfo = &(pELTInfo->frameInfo);

        pCorPrfFrameInfo->size = sizeof(COR_PRF_FRAME_INFO_INTERNAL);
        pCorPrfFrameInfo->version = COR_PRF_FRAME_INFO_INTERNAL_CURRENT_VERSION;
        pCorPrfFrameInfo->funcID = functionId;
        pCorPrfFrameInfo->IP = ProfileGetIPFromPlatformSpecificHandle(pELTInfo->platformSpecificHandle);
        pCorPrfFrameInfo->extraArg = pProfileArgIterator->GetHiddenArgValue();
        pCorPrfFrameInfo->thisArg = pProfileArgIterator->GetThis();

        *pFrameInfo = (COR_PRF_FRAME_INFO)pCorPrfFrameInfo;
    }

    //
    // Do argument processing if desired.
    //
    if (CORProfilerFunctionArgsEnabled())
    {
        if (pcbArgumentInfo == NULL)
        {
            return E_INVALIDARG;
        }

        if ((*pcbArgumentInfo != 0) && (pArgumentInfo == NULL))
        {
            return E_INVALIDARG;
        }

        ULONG32 count = pProfileArgIterator->GetNumArgs();

        if (metaSig.HasThis())
        {
            count++;
        }

        ULONG ulArgInfoSize = sizeof(COR_PRF_FUNCTION_ARGUMENT_INFO) + (count * sizeof(COR_PRF_FUNCTION_ARGUMENT_RANGE));

        if (*pcbArgumentInfo < ulArgInfoSize)
        {
            *pcbArgumentInfo = ulArgInfoSize;
            return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        }

        _ASSERTE(pArgumentInfo != NULL);

        pArgumentInfo->numRanges         = count;
        pArgumentInfo->totalArgumentSize = 0;

        count = 0;

        if (metaSig.HasThis())
        {
            pELTInfo->pThis = pProfileArgIterator->GetThis();
            pArgumentInfo->ranges[count].startAddress = (UINT_PTR) (&(pELTInfo->pThis));

            UINT length = sizeof(pELTInfo->pThis);
            pArgumentInfo->ranges[count].length = length;
            pArgumentInfo->totalArgumentSize += length;
            count++;
        }

        while (count < pArgumentInfo->numRanges)
        {
            pArgumentInfo->ranges[count].startAddress = (UINT_PTR)(pProfileArgIterator->GetNextArgAddr());

            UINT length = pProfileArgIterator->GetArgSize();
            pArgumentInfo->ranges[count].length = length;
            pArgumentInfo->totalArgumentSize += length;
            count++;
        }
    }

    return S_OK;
}



HRESULT ProfToEEInterfaceImpl::GetFunctionEnter3Info(FunctionID functionId,                              // in
                                                     COR_PRF_ELT_INFO eltInfo,                           // in
                                                     COR_PRF_FRAME_INFO * pFrameInfo,                    // out
                                                     ULONG * pcbArgumentInfo,                            // in, out
                                                     COR_PRF_FUNCTION_ARGUMENT_INFO * pArgumentInfo)     // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // ProfilingGetFunctionEnter3Info may take locks
        CAN_TAKE_LOCK;


    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC((LF_CORPROF,
                                    LL_INFO1000,
                                    "**PROF: GetFunctionEnter3Info.\n"));

    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter3WithInfoHook() != NULL);

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    if (!CORProfilerELT3SlowPathEnterEnabled())
    {
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }

    return ProfilingGetFunctionEnter3Info(functionId, eltInfo, pFrameInfo, pcbArgumentInfo, pArgumentInfo);
}

//---------------------------------------------------------------------------------------
//
// ProfilingGetFunctionLeave3Info provides frame information and return value infomation
// of the function ELT callback is inspecting.  It is called either by the profiler or the
// C helper function.
//
// Arguments:
//      * functionId - [in] FunctionId of the function being inspected by ELT3
//      * eltInfo - [in] The opaque pointer FunctionLeave3WithInfo callback passed to the profiler
//      * pFrameInfo - [out] Pointer to COR_PRF_FRAME_INFO the profiler later can use to inspect
//                     generic types
//      * pRetvalRange - [out] Pointer to COR_PRF_FUNCTION_ARGUMENT_RANGE to store return value
//
// Return Value:
//    HRESULT indicating success or failure.
//

HRESULT ProfilingGetFunctionLeave3Info(FunctionID functionId,                              // in
                                       COR_PRF_ELT_INFO eltInfo,                           // in
                                       COR_PRF_FRAME_INFO * pFrameInfo,                    // out
                                       COR_PRF_FUNCTION_ARGUMENT_RANGE * pRetvalRange)     // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // ProfileArgIterator::ProfileArgIterator may take locks
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;

    if ((pFrameInfo == NULL) || (eltInfo == NULL))
    {
        return E_INVALIDARG;
    }

    COR_PRF_ELT_INFO_INTERNAL * pELTInfo = (COR_PRF_ELT_INFO_INTERNAL *)eltInfo;
    ProfileSetFunctionIDInPlatformSpecificHandle(pELTInfo->platformSpecificHandle, functionId);

    // The loader won't trigger a GC or throw for already loaded argument types.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    //
    // Find the method this is referring to, so we can get the signature
    //
    MethodDesc * pMethodDesc = FunctionIdToMethodDesc(functionId);
    MetaSig metaSig(pMethodDesc);

    NewHolder<ProfileArgIterator> pProfileArgIterator;

    {
        // Can handle E_OUTOFMEMORY from ProfileArgIterator.
        FAULT_NOT_FATAL();

        pProfileArgIterator = new (nothrow) ProfileArgIterator(&metaSig, pELTInfo->platformSpecificHandle);

        if (pProfileArgIterator == NULL)
        {
            return E_UNEXPECTED;
        }
    }

    if (CORProfilerFrameInfoEnabled())
    {
        if (pFrameInfo == NULL)
        {
            return E_INVALIDARG;
        }

        COR_PRF_FRAME_INFO_INTERNAL * pCorPrfFrameInfo = &(pELTInfo->frameInfo);

        //
        // Setup the COR_PRF_FRAME_INFO structure first.
        //
        pCorPrfFrameInfo->size = sizeof(COR_PRF_FRAME_INFO_INTERNAL);
        pCorPrfFrameInfo->version = COR_PRF_FRAME_INFO_INTERNAL_CURRENT_VERSION;
        pCorPrfFrameInfo->funcID = functionId;
        pCorPrfFrameInfo->IP = ProfileGetIPFromPlatformSpecificHandle(pELTInfo->platformSpecificHandle);

        // Upon entering Leave hook, the register assigned to store this pointer on function calls may
        // already be reused and is likely not to contain this pointer.
        pCorPrfFrameInfo->extraArg = NULL;
        pCorPrfFrameInfo->thisArg = NULL;

        *pFrameInfo = (COR_PRF_FRAME_INFO)pCorPrfFrameInfo;
    }

    //
    // Do argument processing if desired.
    //
    if (CORProfilerFunctionReturnValueEnabled())
    {
        if (pRetvalRange == NULL)
        {
            return E_INVALIDARG;
        }

        if (!metaSig.IsReturnTypeVoid())
        {
            pRetvalRange->length = metaSig.GetReturnTypeSize();
            pRetvalRange->startAddress = (UINT_PTR)pProfileArgIterator->GetReturnBufferAddr();
        }
        else
        {
            pRetvalRange->length = 0;
            pRetvalRange->startAddress = 0;
        }
    }

    return S_OK;
}


HRESULT ProfToEEInterfaceImpl::GetFunctionLeave3Info(FunctionID functionId,                              // in
                                                     COR_PRF_ELT_INFO eltInfo,                           // in
                                                     COR_PRF_FRAME_INFO * pFrameInfo,                    // out
                                                     COR_PRF_FUNCTION_ARGUMENT_RANGE * pRetvalRange)     // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // ProfilingGetFunctionLeave3Info may take locks
        CAN_TAKE_LOCK;


    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC((LF_CORPROF,
                                    LL_INFO1000,
                                    "**PROF: GetFunctionLeave3Info.\n"));

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave3WithInfoHook() != NULL);

    if (!CORProfilerELT3SlowPathLeaveEnabled())
    {
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }

    return ProfilingGetFunctionLeave3Info(functionId, eltInfo, pFrameInfo, pRetvalRange);
}

//---------------------------------------------------------------------------------------
//
// ProfilingGetFunctionTailcall3Info provides frame information of the function ELT callback
// is inspecting.  It is called either by the profiler or the C helper function.
//
// Arguments:
//      * functionId - [in] FunctionId of the function being inspected by ELT3
//      * eltInfo - [in] The opaque pointer FunctionTailcall3WithInfo callback passed to the
//                  profiler
//      * pFrameInfo - [out] Pointer to COR_PRF_FRAME_INFO the profiler later can use to inspect
//                     generic types
//
// Return Value:
//    HRESULT indicating success or failure.
//

HRESULT ProfilingGetFunctionTailcall3Info(FunctionID functionId,                              // in
                                          COR_PRF_ELT_INFO eltInfo,                           // in
                                          COR_PRF_FRAME_INFO * pFrameInfo)                    // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // ProfileArgIterator::ProfileArgIterator may take locks
        CAN_TAKE_LOCK;


    }
    CONTRACTL_END;

    if ((functionId == NULL) || (eltInfo == NULL) || (pFrameInfo == NULL))
    {
        return E_INVALIDARG;
    }

    COR_PRF_ELT_INFO_INTERNAL * pELTInfo = (COR_PRF_ELT_INFO_INTERNAL *)eltInfo;
    ProfileSetFunctionIDInPlatformSpecificHandle(pELTInfo->platformSpecificHandle, functionId);

    // The loader won't trigger a GC or throw for already loaded argument types.
    ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

    //
    // Find the method this is referring to, so we can get the signature
    //
    MethodDesc * pMethodDesc = FunctionIdToMethodDesc(functionId);
    MetaSig metaSig(pMethodDesc);

    NewHolder<ProfileArgIterator> pProfileArgIterator;

    {
        // Can handle E_OUTOFMEMORY from ProfileArgIterator.
        FAULT_NOT_FATAL();

        pProfileArgIterator = new (nothrow) ProfileArgIterator(&metaSig, pELTInfo->platformSpecificHandle);

        if (pProfileArgIterator == NULL)
        {
            return E_UNEXPECTED;
        }
    }

    COR_PRF_FRAME_INFO_INTERNAL * pCorPrfFrameInfo = &(pELTInfo->frameInfo);

    //
    // Setup the COR_PRF_FRAME_INFO structure first.
    //
    pCorPrfFrameInfo->size = sizeof(COR_PRF_FRAME_INFO_INTERNAL);
    pCorPrfFrameInfo->version = COR_PRF_FRAME_INFO_INTERNAL_CURRENT_VERSION;
    pCorPrfFrameInfo->funcID = functionId;
    pCorPrfFrameInfo->IP = ProfileGetIPFromPlatformSpecificHandle(pELTInfo->platformSpecificHandle);

    // Tailcall is designed to report the caller, not the callee.  But the taillcall hook is invoked
    // with registers containing parameters passed to the callee before calling into the callee.
    // This pointer we get here is for the callee.  Because of the constraints imposed on tailcall
    // optimization, this pointer passed to the callee accidentally happens to be the same this pointer
    // passed to the caller.
    //
    // It is a fragile coincidence we should not depend on because JIT is free to change the
    // implementation details in the future.
    pCorPrfFrameInfo->extraArg = NULL;
    pCorPrfFrameInfo->thisArg = NULL;

    *pFrameInfo = (COR_PRF_FRAME_INFO)pCorPrfFrameInfo;

    return S_OK;
}


HRESULT ProfToEEInterfaceImpl::GetFunctionTailcall3Info(FunctionID functionId,                              // in
                                                        COR_PRF_ELT_INFO eltInfo,                           // in
                                                        COR_PRF_FRAME_INFO * pFrameInfo)                    // out
{
    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // ProfilingGetFunctionTailcall3Info may take locks
        CAN_TAKE_LOCK;


    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC((LF_CORPROF,
                                    LL_INFO1000,
                                    "**PROF: GetFunctionTailcall3Info.\n"));

    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall3WithInfoHook() != NULL);

    if (!g_profControlBlock.IsMainProfiler(this))
    {
        return E_INVALIDARG;
    }

    if (!CORProfilerELT3SlowPathTailcallEnabled())
    {
        return CORPROF_E_INCONSISTENT_WITH_FLAGS;
    }

    return ProfilingGetFunctionTailcall3Info(functionId, eltInfo, pFrameInfo);
}

HRESULT ProfToEEInterfaceImpl::EnumThreads(
    /* out */ ICorProfilerThreadEnum ** ppEnum)
{

    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // Need to acquire the thread store lock
        CAN_TAKE_LOCK;


        PRECONDITION(CheckPointer(ppEnum, NULL_OK));

    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach,
        (LF_CORPROF,
        LL_INFO10,
        "**PROF: EnumThreads.\n"));

    HRESULT hr;

    if (ppEnum == NULL)
    {
        return E_INVALIDARG;
    }

    *ppEnum = NULL;

    NewHolder<ProfilerThreadEnum> pThreadEnum(new (nothrow) ProfilerThreadEnum);
    if (pThreadEnum == NULL)
    {
        return E_OUTOFMEMORY;
    }

    hr = pThreadEnum->Init();
    if (FAILED(hr))
    {
        return hr;
    }

    // Ownership transferred to [out] param.  Caller must Release() when done with this.
    *ppEnum = (ICorProfilerThreadEnum *) pThreadEnum.Extract();

    return S_OK;
}

// This function needs to be called on any thread before making any ICorProfilerInfo* calls and must be
// made before any thread is suspended by this profiler.
// As you might have already figured out, this is done to avoid deadlocks situation when
// the suspended thread holds on the loader lock / heap lock while the current thread is trying to obtain
// the same lock.
HRESULT ProfToEEInterfaceImpl::InitializeCurrentThread()
{

    CONTRACTL
    {
        // Yay!
        NOTHROW;

        // Yay!
        GC_NOTRIGGER;

        // Yay!
        MODE_ANY;

        // May take thread store lock and OS APIs may also take locks
        CAN_TAKE_LOCK;

    }
    CONTRACTL_END;


    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
            kP2EEAllowableAfterAttach,
            (LF_CORPROF,
            LL_INFO10,
            "**PROF: InitializeCurrentThread.\n"));

    SetupTLSForThread();

    return S_OK;
}

struct InternalProfilerModuleEnum : public ProfilerModuleEnum
{
    CDynArray<ModuleID> *GetRawElementsArray()
    {
        return &m_elements;
    }
};

HRESULT ProfToEEInterfaceImpl::EnumNgenModuleMethodsInliningThisMethod(
    ModuleID    inlinersModuleId,
    ModuleID    inlineeModuleId,
    mdMethodDef inlineeMethodId,
    BOOL       *incompleteData,
    ICorProfilerMethodEnum** ppEnum)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(ppEnum));
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EETriggers, (LF_CORPROF, LL_INFO1000,  "**PROF: EnumNgenModuleMethodsInliningThisMethod.\n"));

    if (ppEnum == NULL)
    {
        return E_INVALIDARG;
    }
    *ppEnum = NULL;
    HRESULT hr = S_OK;

    Module *inlineeOwnerModule = reinterpret_cast<Module *>(inlineeModuleId);
    if (inlineeOwnerModule == NULL)
    {
        return E_INVALIDARG;
    }
    if (inlineeOwnerModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    Module  *inlinersModule = reinterpret_cast<Module *>(inlinersModuleId);
    if (inlinersModule == NULL)
    {
        return E_INVALIDARG;
    }
    if(inlinersModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    if (!inlinersModule->HasReadyToRunInlineTrackingMap())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    CDynArray<COR_PRF_METHOD> results;
    const COUNT_T staticBufferSize = 10;
    MethodInModule staticBuffer[staticBufferSize];
    NewArrayHolder<MethodInModule> dynamicBuffer;
    MethodInModule *methodsBuffer = staticBuffer;
    EX_TRY
    {
        // Trying to use static buffer
        COUNT_T methodsAvailable = inlinersModule->GetReadyToRunInliners(inlineeOwnerModule, inlineeMethodId, staticBufferSize, staticBuffer, incompleteData);

        // If static buffer is not enough, allocate an array.
        if (methodsAvailable > staticBufferSize)
        {
            DWORD dynamicBufferSize = methodsAvailable;
            dynamicBuffer = methodsBuffer = new MethodInModule[dynamicBufferSize];
            methodsAvailable = inlinersModule->GetReadyToRunInliners(inlineeOwnerModule, inlineeMethodId, dynamicBufferSize, dynamicBuffer, incompleteData);
            if (methodsAvailable > dynamicBufferSize)
            {
                _ASSERTE(!"Ngen image inlining info changed, this shouldn't be possible.");
                methodsAvailable = dynamicBufferSize;
            }
        }

        //Go through all inliners found in the inlinersModule and prepare them to export via results.
        results.AllocateBlockThrowing(methodsAvailable);
        for (COUNT_T j = 0; j < methodsAvailable; j++)
        {
            COR_PRF_METHOD *newPrfMethod = &results[j];
            newPrfMethod->moduleId = reinterpret_cast<ModuleID>(methodsBuffer[j].m_module);
            newPrfMethod->methodId = methodsBuffer[j].m_methodDef;
        }
        *ppEnum = new ProfilerMethodEnum(&results);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT ProfToEEInterfaceImpl::GetInMemorySymbolsLength(
    ModuleID moduleId,
    DWORD* pCountSymbolBytes)
{

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;


    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach,
        (LF_CORPROF,
            LL_INFO10,
            "**PROF: GetInMemorySymbolsLength.\n"));

    HRESULT hr = S_OK;
    if (pCountSymbolBytes == NULL)
    {
        return E_INVALIDARG;
    }
    *pCountSymbolBytes = 0;

    Module* pModule = reinterpret_cast< Module* >(moduleId);
    if (pModule == NULL)
    {
        return E_INVALIDARG;
    }
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //This method would work fine on reflection.emit, but there would be no way to know
    //if some other thread was changing the size of the symbols before this method returned.
    //Adding events or locks to detect/prevent changes would make the scenario workable
    if (pModule->IsReflection())
    {
        return COR_PRF_MODULE_DYNAMIC;
    }

    CGrowableStream* pStream = pModule->GetInMemorySymbolStream();
    if (pStream == NULL)
    {
        return S_OK;
    }

    STATSTG SizeData = { 0 };
    hr = pStream->Stat(&SizeData, STATFLAG_NONAME);
    if (FAILED(hr))
    {
        return hr;
    }
    if (SizeData.cbSize.u.HighPart > 0)
    {
        return COR_E_OVERFLOW;
    }
    *pCountSymbolBytes = SizeData.cbSize.u.LowPart;

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::ReadInMemorySymbols(
    ModuleID moduleId,
    DWORD symbolsReadOffset,
    BYTE* pSymbolBytes,
    DWORD countSymbolBytes,
    DWORD* pCountSymbolBytesRead)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(
        kP2EEAllowableAfterAttach,
        (LF_CORPROF,
            LL_INFO10,
            "**PROF: ReadInMemorySymbols.\n"));

    HRESULT hr = S_OK;
    if (pSymbolBytes == NULL)
    {
        return E_INVALIDARG;
    }
    if (pCountSymbolBytesRead == NULL)
    {
        return E_INVALIDARG;
    }
    *pCountSymbolBytesRead = 0;

    Module* pModule = reinterpret_cast< Module* >(moduleId);
    if (pModule == NULL)
    {
        return E_INVALIDARG;
    }
    if (pModule->IsBeingUnloaded())
    {
        return CORPROF_E_DATAINCOMPLETE;
    }

    //This method would work fine on reflection.emit, but there would be no way to know
    //if some other thread was changing the size of the symbols before this method returned.
    //Adding events or locks to detect/prevent changes would make the scenario workable
    if (pModule->IsReflection())
    {
        return COR_PRF_MODULE_DYNAMIC;
    }

    CGrowableStream* pStream = pModule->GetInMemorySymbolStream();
    if (pStream == NULL)
    {
        return E_INVALIDARG;
    }

    STATSTG SizeData = { 0 };
    hr = pStream->Stat(&SizeData, STATFLAG_NONAME);
    if (FAILED(hr))
    {
        return hr;
    }
    if (SizeData.cbSize.u.HighPart > 0)
    {
        return COR_E_OVERFLOW;
    }
    DWORD streamSize = SizeData.cbSize.u.LowPart;
    if (symbolsReadOffset >= streamSize)
    {
        return E_INVALIDARG;
    }

    *pCountSymbolBytesRead = min(streamSize - symbolsReadOffset, countSymbolBytes);
    memcpy_s(pSymbolBytes, countSymbolBytes, ((BYTE*)pStream->GetRawBuffer().StartAddress()) + symbolsReadOffset, *pCountSymbolBytesRead);

    return S_OK;
}

HRESULT ProfToEEInterfaceImpl::ApplyMetaData(
    ModuleID    moduleId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PROFILER_TO_CLR_ENTRYPOINT_SYNC_EX(kP2EEAllowableAfterAttach | kP2EETriggers, (LF_CORPROF, LL_INFO1000, "**PROF: ApplyMetaData.\n"));

    if (moduleId == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        Module *pModule = (Module *)moduleId;
        _ASSERTE(pModule != NULL);
        if (pModule->IsBeingUnloaded())
        {
            hr = CORPROF_E_DATAINCOMPLETE;
        }
       else
       {
            pModule->ApplyMetaData();
       }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Simple wrapper around EEToProfInterfaceImpl::ManagedToUnmanagedTransition.  This
// can be called by C++ code and directly by generated stubs.
//
// Arguments:
//      pMD - MethodDesc for the managed function involved in the transition
//      reason - Passed on to profiler to indicate why the transition is occurring
//

void __stdcall ProfilerManagedToUnmanagedTransitionMD(MethodDesc *pMD,
                                                      COR_PRF_TRANSITION_REASON reason)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // This function is called within the runtime, not directly from managed code.
    // Also, the only case MD is NULL is the calli pinvoke case, and we still
    // want to notify the profiler in that case.

    // Do not notify the profiler about QCalls
    if (pMD == NULL || !pMD->IsQCall())
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackTransitions());
        g_profControlBlock.ManagedToUnmanagedTransition(MethodDescToFunctionID(pMD), reason);
        END_PROFILER_CALLBACK();
    }
}

//---------------------------------------------------------------------------------------
//
// Simple wrapper around EEToProfInterfaceImpl::UnmanagedToManagedTransition.  This
// can be called by C++ code and directly by generated stubs.
//
// Arguments:
//      pMD - MethodDesc for the managed function involved in the transition
//      reason - Passed on to profiler to indicate why the transition is occurring
//

void __stdcall ProfilerUnmanagedToManagedTransitionMD(MethodDesc *pMD,
                                                      COR_PRF_TRANSITION_REASON reason)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // This function is called within the runtime, not directly from managed code.
    // Also, the only case MD is NULL is the calli pinvoke case, and we still
    // want to notify the profiler in that case.

    // Do not notify the profiler about QCalls
    if (pMD == NULL || !pMD->IsQCall())
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackTransitions());
        g_profControlBlock.UnmanagedToManagedTransition(MethodDescToFunctionID(pMD), reason);
        END_PROFILER_CALLBACK();
    }
}



#endif // PROFILING_SUPPORTED


//*******************************************************************************************
// These do a lot of work for us, setting up Frames, gathering arg info and resolving generics.
  //*******************************************************************************************

HCIMPL2(EXTERN_C void, ProfileEnter, UINT_PTR clientData, void * platformSpecificHandle)
{
    FCALL_CONTRACT;

#ifdef PROFILING_SUPPORTED

#ifdef PROF_TEST_ONLY_FORCE_ELT
    // If this test-only flag is set, it's possible we might not have a profiler
    // attached, or might not have any of the hooks set. See
    // code:ProfControlBlock#TestOnlyELT
    if (g_profControlBlock.fTestOnlyForceEnterLeave)
    {
        if ((g_profControlBlock.mainProfilerInfo.pProfInterface.Load() == NULL) ||
            (
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnterHook()          == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter2Hook()         == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter3Hook()         == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter3WithInfoHook() == NULL)
            )
           )
        {
            return;
        }
    }
#endif // PROF_TEST_ONLY_FORCE_ELT

    // ELT3 Fast-Path hooks should be NULL when ELT intermediary is used.
    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter3Hook() == NULL);
    _ASSERTE(GetThread()->PreemptiveGCDisabled());
    _ASSERTE(platformSpecificHandle != NULL);

    // Set up a frame
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    // Our contract is FCALL_CONTRACT, which is considered triggers if you set up a
    // frame, like we're about to do.
    SetCallbackStateFlagsHolder csf(
        COR_PRF_CALLBACKSTATE_INCALLBACK | COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE);

    COR_PRF_ELT_INFO_INTERNAL eltInfo;
    eltInfo.platformSpecificHandle = platformSpecificHandle;

    //
    // CLR v4 Slow-Path ELT
    //
    if (g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter3WithInfoHook() != NULL)
    {
        FunctionIDOrClientID functionIDOrClientID;
        functionIDOrClientID.clientID = clientData;
        g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter3WithInfoHook()(
            functionIDOrClientID,
            (COR_PRF_ELT_INFO)&eltInfo);
        goto LExit;
    }

    if (g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter2Hook() != NULL)
    {
        // We have run out of heap memory, so the content of the mapping table becomes stale.
        // All Whidbey ETL hooks must be turned off.
        if (!g_profControlBlock.mainProfilerInfo.pProfInterface->IsClientIDToFunctionIDMappingEnabled())
        {
            goto LExit;
        }

        // If ELT2 is in use, FunctionID will be returned to the JIT to be embedded into the ELT3 probes
        // instead of using clientID because the profiler may map several functionIDs to a clientID to
        // do things like code coverage analysis.  FunctionID to clientID has the one-on-one relationship,
        // while the reverse may not have this one-on-one mapping.  Therefore, FunctionID is used as the
        // key to retrieve the corresponding clientID from the internal FunctionID hash table.
        FunctionID functionId = clientData;
        _ASSERTE(functionId != NULL);
        clientData = g_profControlBlock.mainProfilerInfo.pProfInterface->LookupClientIDFromCache(functionId);

        //
        // Whidbey Fast-Path ELT
        //
        if (CORProfilerELT2FastPathEnterEnabled())
        {
            g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter2Hook()(
                functionId,
                clientData,
                NULL,
                NULL);
            goto LExit;
        }

        //
        // Whidbey Slow-Path ELT
        //
        ProfileSetFunctionIDInPlatformSpecificHandle(platformSpecificHandle, functionId);

        COR_PRF_FRAME_INFO frameInfo = NULL;
        COR_PRF_FUNCTION_ARGUMENT_INFO * pArgumentInfo = NULL;
        ULONG ulArgInfoSize = 0;

        if (CORProfilerFunctionArgsEnabled())
        {
            // The loader won't trigger a GC or throw for already loaded argument types.
            ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

            //
            // Find the method this is referring to, so we can get the signature
            //
            MethodDesc * pMethodDesc = FunctionIdToMethodDesc(functionId);
            MetaSig metaSig(pMethodDesc);

            NewHolder<ProfileArgIterator> pProfileArgIterator;

            {
                // Can handle E_OUTOFMEMORY from ProfileArgIterator.
                FAULT_NOT_FATAL();

                pProfileArgIterator = new (nothrow) ProfileArgIterator(&metaSig, platformSpecificHandle);

                if (pProfileArgIterator == NULL)
                {
                    goto LExit;
                }
            }

            ULONG32 count = pProfileArgIterator->GetNumArgs();

            if (metaSig.HasThis())
            {
                count++;
            }

            ulArgInfoSize = sizeof(COR_PRF_FUNCTION_ARGUMENT_INFO) + count * sizeof(COR_PRF_FUNCTION_ARGUMENT_RANGE);
            pArgumentInfo = (COR_PRF_FUNCTION_ARGUMENT_INFO *)_alloca(ulArgInfoSize);
        }

        HRESULT hr = ProfilingGetFunctionEnter3Info(functionId, (COR_PRF_ELT_INFO)&eltInfo, &frameInfo, &ulArgInfoSize, pArgumentInfo);

        _ASSERTE(hr == S_OK);
        g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnter2Hook()(functionId, clientData, frameInfo, pArgumentInfo);

        goto LExit;
    }


    // We will not be here unless the jit'd or ngen'd function we're about to enter
    // was backpatched with this wrapper around the profiler's hook, and that
    // wouldn't have happened unless the profiler supplied us with a hook
    // in the first place.  (Note that SetEnterLeaveFunctionHooks* will return
    // an error unless it's called in the profiler's Initialize(), so a profiler can't change
    // its mind about where the hooks are.)
    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnterHook() != NULL);

    // Note that we cannot assert CORProfilerTrackEnterLeave() (i.e., profiler flag
    // COR_PRF_MONITOR_ENTERLEAVE), because the profiler may decide whether
    // to enable the jitter to add enter/leave callouts independently of whether
    // the profiler actually has enter/leave hooks.  (If the profiler has no such hooks,
    // the callouts quickly return and do nothing.)

    //
    // Everett ELT
    //
    {
        g_profControlBlock.mainProfilerInfo.pProfInterface->GetEnterHook()((FunctionID)clientData);
    }

LExit:
    ;

    HELPER_METHOD_FRAME_END();      // Un-link the frame

#endif // PROFILING_SUPPORTED
}
HCIMPLEND

HCIMPL2(EXTERN_C void, ProfileLeave, UINT_PTR clientData, void * platformSpecificHandle)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();            // we pulse GC mode, so we are doing a poll

#ifdef PROFILING_SUPPORTED

#ifdef PROF_TEST_ONLY_FORCE_ELT
    // If this test-only flag is set, it's possible we might not have a profiler
    // attached, or might not have any of the hooks set. See
    // code:ProfControlBlock#TestOnlyELT
    if (g_profControlBlock.fTestOnlyForceEnterLeave)
    {
        if ((g_profControlBlock.mainProfilerInfo.pProfInterface.Load() == NULL) ||
            (
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeaveHook()          == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave2Hook()         == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave3Hook()         == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave3WithInfoHook() == NULL)
            )
           )
        {
            return;
        }
    }
#endif // PROF_TEST_ONLY_FORCE_ELT

    // ELT3 Fast-Path hooks should be NULL when ELT intermediary is used.
    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave3Hook() == NULL);
    _ASSERTE(GetThread()->PreemptiveGCDisabled());
    _ASSERTE(platformSpecificHandle != NULL);

    // Set up a frame
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    // Our contract is FCALL_CONTRACT, which is considered triggers if you set up a
    // frame, like we're about to do.
    SetCallbackStateFlagsHolder csf(
        COR_PRF_CALLBACKSTATE_INCALLBACK | COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE);

    COR_PRF_ELT_INFO_INTERNAL eltInfo;
    eltInfo.platformSpecificHandle = platformSpecificHandle;

    //
    // CLR v4 Slow-Path ELT
    //
    if (g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave3WithInfoHook() != NULL)
    {
        FunctionIDOrClientID functionIDOrClientID;
        functionIDOrClientID.clientID = clientData;
        g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave3WithInfoHook()(
            functionIDOrClientID,
            (COR_PRF_ELT_INFO)&eltInfo);
        goto LExit;
    }

    if (g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave2Hook() != NULL)
    {
        // We have run out of heap memory, so the content of the mapping table becomes stale.
        // All Whidbey ETL hooks must be turned off.
        if (!g_profControlBlock.mainProfilerInfo.pProfInterface->IsClientIDToFunctionIDMappingEnabled())
        {
            goto LExit;
        }

        // If ELT2 is in use, FunctionID will be returned to the JIT to be embedded into the ELT3 probes
        // instead of using clientID because the profiler may map several functionIDs to a clientID to
        // do things like code coverage analysis.  FunctionID to clientID has the one-on-one relationship,
        // while the reverse may not have this one-on-one mapping.  Therefore, FunctionID is used as the
        // key to retrieve the corresponding clientID from the internal FunctionID hash table.
        FunctionID functionId = clientData;
        _ASSERTE(functionId != NULL);
        clientData = g_profControlBlock.mainProfilerInfo.pProfInterface->LookupClientIDFromCache(functionId);

        //
        // Whidbey Fast-Path ELT
        //
        if (CORProfilerELT2FastPathLeaveEnabled())
        {
            g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave2Hook()(
                functionId,
                clientData,
                NULL,
                NULL);
            goto LExit;
        }

        //
        // Whidbey Slow-Path ELT
        //
        COR_PRF_FRAME_INFO frameInfo = NULL;
        COR_PRF_FUNCTION_ARGUMENT_RANGE argumentRange;

        HRESULT hr = ProfilingGetFunctionLeave3Info(functionId, (COR_PRF_ELT_INFO)&eltInfo, &frameInfo, &argumentRange);
        _ASSERTE(hr == S_OK);

        g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeave2Hook()(functionId, clientData, frameInfo, &argumentRange);
        goto LExit;
    }

    // We will not be here unless the jit'd or ngen'd function we're about to leave
    // was backpatched with this wrapper around the profiler's hook, and that
    // wouldn't have happened unless the profiler supplied us with a hook
    // in the first place.  (Note that SetEnterLeaveFunctionHooks* will return
    // an error unless it's called in the profiler's Initialize(), so a profiler can't change
    // its mind about where the hooks are.)
    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeaveHook() != NULL);

    // Note that we cannot assert CORProfilerTrackEnterLeave() (i.e., profiler flag
    // COR_PRF_MONITOR_ENTERLEAVE), because the profiler may decide whether
    // to enable the jitter to add enter/leave callouts independently of whether
    // the profiler actually has enter/leave hooks.  (If the profiler has no such hooks,
    // the callouts quickly return and do nothing.)

    //
    // Everett ELT
    //
    {
        g_profControlBlock.mainProfilerInfo.pProfInterface->GetLeaveHook()((FunctionID)clientData);
    }

LExit:

    ;

    HELPER_METHOD_FRAME_END();      // Un-link the frame

#endif // PROFILING_SUPPORTED
}
HCIMPLEND

HCIMPL2(EXTERN_C void, ProfileTailcall, UINT_PTR clientData, void * platformSpecificHandle)
{
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();            // we pulse GC mode, so we are doing a poll

#ifdef PROFILING_SUPPORTED

#ifdef PROF_TEST_ONLY_FORCE_ELT
    // If this test-only flag is set, it's possible we might not have a profiler
    // attached, or might not have any of the hooks set. See
    // code:ProfControlBlock#TestOnlyELT
    if (g_profControlBlock.fTestOnlyForceEnterLeave)
    {
        if ((g_profControlBlock.mainProfilerInfo.pProfInterface.Load() == NULL) ||
            (
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcallHook()          == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall2Hook()         == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall3Hook()         == NULL) &&
                (g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall3WithInfoHook() == NULL)
            )
           )
        {
            return;
        }
    }
#endif // PROF_TEST_ONLY_FORCE_ELT

    // ELT3 fast-path hooks should be NULL when ELT intermediary is used.
    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall3Hook() == NULL);
    _ASSERTE(GetThread()->PreemptiveGCDisabled());
    _ASSERTE(platformSpecificHandle != NULL);

    // Set up a frame
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_NOPOLL(Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    // Our contract is FCALL_CONTRACT, which is considered triggers if you set up a
    // frame, like we're about to do.
    SetCallbackStateFlagsHolder csf(
        COR_PRF_CALLBACKSTATE_INCALLBACK | COR_PRF_CALLBACKSTATE_IN_TRIGGERS_SCOPE);

    COR_PRF_ELT_INFO_INTERNAL eltInfo;
    eltInfo.platformSpecificHandle = platformSpecificHandle;

    //
    // CLR v4 Slow-Path ELT
    //
    if (g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall3WithInfoHook() != NULL)
    {
        FunctionIDOrClientID functionIDOrClientID;
        functionIDOrClientID.clientID = clientData;
        g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall3WithInfoHook()(
            functionIDOrClientID,
            (COR_PRF_ELT_INFO)&eltInfo);
        goto LExit;
    }

    if (g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall2Hook() != NULL)
    {
        // We have run out of heap memory, so the content of the mapping table becomes stale.
        // All Whidbey ETL hooks must be turned off.
        if (!g_profControlBlock.mainProfilerInfo.pProfInterface->IsClientIDToFunctionIDMappingEnabled())
        {
            goto LExit;
        }

        // If ELT2 is in use, FunctionID will be returned to the JIT to be embedded into the ELT3 probes
        // instead of using clientID because the profiler may map several functionIDs to a clientID to
        // do things like code coverage analysis.  FunctionID to clientID has the one-on-one relationship,
        // while the reverse may not have this one-on-one mapping.  Therefore, FunctionID is used as the
        // key to retrieve the corresponding clientID from the internal FunctionID hash table.
        FunctionID functionId = clientData;
        _ASSERTE(functionId != NULL);
        clientData = g_profControlBlock.mainProfilerInfo.pProfInterface->LookupClientIDFromCache(functionId);

        //
        // Whidbey Fast-Path ELT
        //
        if (CORProfilerELT2FastPathTailcallEnabled())
        {
            g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall2Hook()(
                functionId,
                clientData,
                NULL);
            goto LExit;
        }

        //
        // Whidbey Slow-Path ELT
        //
        COR_PRF_FRAME_INFO frameInfo = NULL;

        HRESULT hr = ProfilingGetFunctionTailcall3Info(functionId, (COR_PRF_ELT_INFO)&eltInfo, &frameInfo);
        _ASSERTE(hr == S_OK);

        g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcall2Hook()(functionId, clientData, frameInfo);
        goto LExit;
    }

    // We will not be here unless the jit'd or ngen'd function we're about to tailcall
    // was backpatched with this wrapper around the profiler's hook, and that
    // wouldn't have happened unless the profiler supplied us with a hook
    // in the first place.  (Note that SetEnterLeaveFunctionHooks* will return
    // an error unless it's called in the profiler's Initialize(), so a profiler can't change
    // its mind about where the hooks are.)
    _ASSERTE(g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcallHook() != NULL);

    // Note that we cannot assert CORProfilerTrackEnterLeave() (i.e., profiler flag
    // COR_PRF_MONITOR_ENTERLEAVE), because the profiler may decide whether
    // to enable the jitter to add enter/leave callouts independently of whether
    // the profiler actually has enter/leave hooks.  (If the profiler has no such hooks,
    // the callouts quickly return and do nothing.)

    //
    // Everett ELT
    //
    g_profControlBlock.mainProfilerInfo.pProfInterface->GetTailcallHook()((FunctionID)clientData);

LExit:

    ;

    HELPER_METHOD_FRAME_END();      // Un-link the frame

#endif // PROFILING_SUPPORTED
}
HCIMPLEND
