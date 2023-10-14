// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "eventtrace.h"
#include "winbase.h"
#include "contract.h"
#include "ex.h"
#include "dbginterface.h"
#include "finalizerthread.h"
#include "clrversion.h"
#include "typestring.h"

#include "eventtracepriv.h"

//---------------------------------------------------------------------------------------
// Code for sending GC heap object events is generally the same for both FEATURE_NATIVEAOT
// and !FEATURE_NATIVEAOT builds
//---------------------------------------------------------------------------------------

bool s_forcedGCInProgress = false;
class ForcedGCHolder
{
public:
    ForcedGCHolder() { LIMITED_METHOD_CONTRACT; s_forcedGCInProgress = true; }
    ~ForcedGCHolder() { LIMITED_METHOD_CONTRACT; s_forcedGCInProgress = false; }
};


// Simple helpers called by the GC to decide whether it needs to do a walk of heap
// objects and / or roots.

BOOL ETW::GCLog::ShouldWalkHeapObjectsForEtw()
{
    LIMITED_METHOD_CONTRACT;
    return s_forcedGCInProgress &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     CLR_GCHEAPDUMP_KEYWORD);
}

BOOL ETW::GCLog::ShouldWalkHeapRootsForEtw()
{
    LIMITED_METHOD_CONTRACT;
    return s_forcedGCInProgress &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     CLR_GCHEAPDUMP_KEYWORD);
}

BOOL ETW::GCLog::ShouldTrackMovementForEtw()
{
    LIMITED_METHOD_CONTRACT;
    return ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_GCHEAPSURVIVALANDMOVEMENT_KEYWORD);
}

BOOL ETW::GCLog::ShouldWalkStaticsAndCOMForEtw()
{
    LIMITED_METHOD_CONTRACT;

    return s_forcedGCInProgress &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     CLR_GCHEAPDUMP_KEYWORD);
}

// Batches the list of moved/surviving references for the GCBulkMovedObjectRanges /
// GCBulkSurvivingObjectRanges events
struct EtwGcMovementContext
{
public:
    // An instance of EtwGcMovementContext is dynamically allocated and stored
    // inside of MovedReferenceContextForEtwAndProfapi, which in turn is dynamically
    // allocated and pointed to by a profiling_context pointer created by the GC on the stack.
    // This is used to batch and send GCBulkSurvivingObjectRanges events and
    // GCBulkMovedObjectRanges events. This method is passed a pointer to
    // MovedReferenceContextForEtwAndProfapi::pctxEtw; if non-NULL it gets returned;
    // else, a new EtwGcMovementContext is allocated, stored in that pointer, and
    // then returned. Callers should test for NULL, which can be returned if out of
    // memory
    static EtwGcMovementContext* GetOrCreateInGCContext(EtwGcMovementContext** ppContext)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ppContext != NULL);

        EtwGcMovementContext* pContext = *ppContext;
        if (pContext == NULL)
        {
            pContext = new (nothrow) EtwGcMovementContext;
            *ppContext = pContext;
        }
        return pContext;
    }

    EtwGcMovementContext() :
        iCurBulkSurvivingObjectRanges(0),
        iCurBulkMovedObjectRanges(0)
    {
        LIMITED_METHOD_CONTRACT;
        Clear();
    }

    // Resets structure for reuse on construction, and after each flush.
    // (Intentionally leave iCurBulk* as is, since they persist across flushes within a GC.)
    void Clear()
    {
        LIMITED_METHOD_CONTRACT;
        cBulkSurvivingObjectRanges = 0;
        cBulkMovedObjectRanges = 0;
        ZeroMemory(rgGCBulkSurvivingObjectRanges, sizeof(rgGCBulkSurvivingObjectRanges));
        ZeroMemory(rgGCBulkMovedObjectRanges, sizeof(rgGCBulkMovedObjectRanges));
    }

    //---------------------------------------------------------------------------------------
    // GCBulkSurvivingObjectRanges
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkSurvivingObjectRanges event
    UINT iCurBulkSurvivingObjectRanges;

    // Number of surviving object ranges currently filled out in rgGCBulkSurvivingObjectRanges array
    UINT cBulkSurvivingObjectRanges;

    // Struct array containing the primary data for each GCBulkSurvivingObjectRanges
    // event. Fix the size so the total event stays well below the 64K limit (leaving
    // lots of room for non-struct fields that come before the values data)
    EventStructGCBulkSurvivingObjectRangesValue rgGCBulkSurvivingObjectRanges[
        (cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkSurvivingObjectRangesValue)];

    //---------------------------------------------------------------------------------------
    // GCBulkMovedObjectRanges
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkMovedObjectRanges event
    UINT iCurBulkMovedObjectRanges;

    // Number of Moved object ranges currently filled out in rgGCBulkMovedObjectRanges array
    UINT cBulkMovedObjectRanges;

    // Struct array containing the primary data for each GCBulkMovedObjectRanges
    // event. Fix the size so the total event stays well below the 64K limit (leaving
    // lots of room for non-struct fields that come before the values data)
    EventStructGCBulkMovedObjectRangesValue rgGCBulkMovedObjectRanges[
        (cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkMovedObjectRangesValue)];
};

// Contains above struct for ETW, plus extra info (opaque to us) used by the profiling
// API to track its own information.
struct MovedReferenceContextForEtwAndProfapi
{
    // An instance of MovedReferenceContextForEtwAndProfapi is dynamically allocated and
    // pointed to by a profiling_context pointer created by the GC on the stack. This is used to
    // batch and send GCBulkSurvivingObjectRanges events and GCBulkMovedObjectRanges
    // events and the corresponding callbacks for profapi profilers. This method is
    // passed a pointer to a MovedReferenceContextForEtwAndProfapi; if non-NULL it gets
    // returned; else, a new MovedReferenceContextForEtwAndProfapi is allocated, stored
    // in that pointer, and then returned. Callers should test for NULL, which can be
    // returned if out of memory
    static MovedReferenceContextForEtwAndProfapi* CreateInGCContext(LPVOID pvContext)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(pvContext != NULL);

        MovedReferenceContextForEtwAndProfapi* pContext = *(MovedReferenceContextForEtwAndProfapi**)pvContext;

        // Shouldn't be called if the context was already created.  Perhaps someone made
        // one too many BeginMovedReferences calls, or didn't have an EndMovedReferences
        // in between?
        _ASSERTE(pContext == NULL);

        pContext = new (nothrow) MovedReferenceContextForEtwAndProfapi;
        *(MovedReferenceContextForEtwAndProfapi**)pvContext = pContext;

        return pContext;
    }


    MovedReferenceContextForEtwAndProfapi() :
        pctxProfAPI(NULL),
        pctxEtw(NULL)

    {
        LIMITED_METHOD_CONTRACT;
    }

    LPVOID pctxProfAPI;
    EtwGcMovementContext* pctxEtw;
};


//---------------------------------------------------------------------------------------
//
// Called by the GC for each moved or surviving reference that it encounters. This
// batches the info into our context's buffer, and flushes that buffer to ETW as it fills
// up.
//
// Arguments:
//      * pbMemBlockStart - Start of moved/surviving block
//      * pbMemBlockEnd - Next pointer after end of moved/surviving block
//      * cbRelocDistance - How far did the block move? (0 for non-compacted / surviving
//          references; negative if moved to earlier addresses)
//      * profilingContext - Where our context is stored
//      * fCompacting - Is this a compacting GC? Used to decide whether to send the moved
//          or surviving event
//

// static
void ETW::GCLog::MovedReference(
    BYTE* pbMemBlockStart,
    BYTE* pbMemBlockEnd,
    ptrdiff_t cbRelocDistance,
    size_t profilingContext,
    BOOL fCompacting,
    BOOL fAllowProfApiNotification /* = TRUE */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;  // EEToProfInterfaceImpl::AllocateMovedReferencesData takes lock
    }
    CONTRACTL_END;

    MovedReferenceContextForEtwAndProfapi* pCtxForEtwAndProfapi =
        (MovedReferenceContextForEtwAndProfapi*) profilingContext;
    if (pCtxForEtwAndProfapi == NULL)
    {
        _ASSERTE(!"MovedReference() encountered a NULL profilingContext");
        return;
    }

#ifdef PROFILING_SUPPORTED
    // ProfAPI
    if (fAllowProfApiNotification)
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC() || CORProfilerTrackGCMovedObjects());
        (&g_profControlBlock)->MovedReference(pbMemBlockStart,
                                                          pbMemBlockEnd,
                                                          cbRelocDistance,
                                                          &(pCtxForEtwAndProfapi->pctxProfAPI),
                                                          fCompacting);
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    // ETW

    if (!ShouldTrackMovementForEtw())
        return;

    EtwGcMovementContext* pContext =
        EtwGcMovementContext::GetOrCreateInGCContext(&pCtxForEtwAndProfapi->pctxEtw);
    if (pContext == NULL)
        return;

    if (fCompacting)
    {
        // Moved references

        _ASSERTE(pContext->cBulkMovedObjectRanges < ARRAY_SIZE(pContext->rgGCBulkMovedObjectRanges));
        EventStructGCBulkMovedObjectRangesValue* pValue =
            &pContext->rgGCBulkMovedObjectRanges[pContext->cBulkMovedObjectRanges];
        pValue->OldRangeBase = pbMemBlockStart;
        pValue->NewRangeBase = pbMemBlockStart + cbRelocDistance;
        pValue->RangeLength = pbMemBlockEnd - pbMemBlockStart;
        pContext->cBulkMovedObjectRanges++;

        // If buffer is now full, empty it into ETW
        if (pContext->cBulkMovedObjectRanges == ARRAY_SIZE(pContext->rgGCBulkMovedObjectRanges))
        {
            FireEtwGCBulkMovedObjectRanges(
                pContext->iCurBulkMovedObjectRanges,
                pContext->cBulkMovedObjectRanges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkMovedObjectRanges[0]),
                &pContext->rgGCBulkMovedObjectRanges[0]);

            pContext->iCurBulkMovedObjectRanges++;
            pContext->Clear();
        }
    }
    else
    {
        // Surviving references

        _ASSERTE(pContext->cBulkSurvivingObjectRanges < ARRAY_SIZE(pContext->rgGCBulkSurvivingObjectRanges));
        EventStructGCBulkSurvivingObjectRangesValue* pValue =
            &pContext->rgGCBulkSurvivingObjectRanges[pContext->cBulkSurvivingObjectRanges];
        pValue->RangeBase = pbMemBlockStart;
        pValue->RangeLength = pbMemBlockEnd - pbMemBlockStart;
        pContext->cBulkSurvivingObjectRanges++;

        // If buffer is now full, empty it into ETW
        if (pContext->cBulkSurvivingObjectRanges == ARRAY_SIZE(pContext->rgGCBulkSurvivingObjectRanges))
        {
            FireEtwGCBulkSurvivingObjectRanges(
                pContext->iCurBulkSurvivingObjectRanges,
                pContext->cBulkSurvivingObjectRanges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkSurvivingObjectRanges[0]),
                &pContext->rgGCBulkSurvivingObjectRanges[0]);

            pContext->iCurBulkSurvivingObjectRanges++;
            pContext->Clear();
        }
    }
}


//---------------------------------------------------------------------------------------
//
// Called by the GC just before it begins enumerating plugs.  Gives us a chance to
// allocate our context structure, to allow us to batch plugs before firing events
// for them
//
// Arguments:
//      * pProfilingContext - Points to location on stack (in GC function) where we can
//         store a pointer to the context we allocate
//

// static
VOID ETW::GCLog::BeginMovedReferences(size_t* pProfilingContext)
{
    LIMITED_METHOD_CONTRACT;

    MovedReferenceContextForEtwAndProfapi::CreateInGCContext(LPVOID(pProfilingContext));
}


//---------------------------------------------------------------------------------------
//
// Called by the GC at the end of a heap walk to give us a place to flush any remaining
// buffers of data to ETW or the profapi profiler
//
// Arguments:
//      profilingContext - Our context we built up during the heap walk
//

// static
VOID ETW::GCLog::EndMovedReferences(size_t profilingContext, BOOL fAllowProfApiNotification /* = TRUE */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    MovedReferenceContextForEtwAndProfapi* pCtxForEtwAndProfapi = (MovedReferenceContextForEtwAndProfapi*) profilingContext;
    if (pCtxForEtwAndProfapi == NULL)
    {
        _ASSERTE(!"EndMovedReferences() encountered a NULL profilingContext");
        return;
    }

#ifdef PROFILING_SUPPORTED
    // ProfAPI
    if (fAllowProfApiNotification)
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC() || CORProfilerTrackGCMovedObjects());
        (&g_profControlBlock)->EndMovedReferences(&(pCtxForEtwAndProfapi->pctxProfAPI));
        END_PROFILER_CALLBACK();
    }
#endif //PROFILING_SUPPORTED

    // ETW

    if (!ShouldTrackMovementForEtw())
        return;

    // If context isn't already set up for us, then we haven't been collecting any data
    // for ETW events.
    EtwGcMovementContext* pContext = pCtxForEtwAndProfapi->pctxEtw;
    if (pContext == NULL)
        return;

    // Flush any remaining moved or surviving range data

    if (pContext->cBulkMovedObjectRanges > 0)
    {
        FireEtwGCBulkMovedObjectRanges(
            pContext->iCurBulkMovedObjectRanges,
            pContext->cBulkMovedObjectRanges,
            GetClrInstanceId(),
            sizeof(pContext->rgGCBulkMovedObjectRanges[0]),
            &pContext->rgGCBulkMovedObjectRanges[0]);
    }

    if (pContext->cBulkSurvivingObjectRanges > 0)
    {
        FireEtwGCBulkSurvivingObjectRanges(
            pContext->iCurBulkSurvivingObjectRanges,
            pContext->cBulkSurvivingObjectRanges,
            GetClrInstanceId(),
            sizeof(pContext->rgGCBulkSurvivingObjectRanges[0]),
            &pContext->rgGCBulkSurvivingObjectRanges[0]);
    }

    pCtxForEtwAndProfapi->pctxEtw = NULL;
    delete pContext;
}

// This implements the public runtime provider's GCHeapCollectKeyword.  It
// performs a full, gen-2, blocking GC.
VOID ETW::GCLog::ForceGC(LONGLONG l64ClientSequenceNumber)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef FEATURE_NATIVEAOT
    if (!IsGarbageCollectorFullyInitialized())
        return;
#endif // FEATURE_NATIVEAOT

    InterlockedExchange64(&s_l64LastClientSequenceNumber, l64ClientSequenceNumber);

    ForceGCForDiagnostics();
}

//---------------------------------------------------------------------------------------
//
// Contains code common to profapi and ETW scenarios where the profiler wants to force
// the CLR to perform a GC.  The important work here is to create a managed thread for
// the current thread BEFORE the GC begins.  On both ETW and profapi threads, there may
// not yet be a managed thread object.  But some scenarios require a managed thread
// object be present.
//
// Return Value:
//      HRESULT indicating success or failure
//
// Assumptions:
//      Caller should ensure that the EE has fully started up and that the GC heap is
//      initialized enough to actually perform a GC
//

// static
HRESULT ETW::GCLog::ForceGCForDiagnostics()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

#ifndef FEATURE_NATIVEAOT
    // Caller should ensure we're past startup.
    _ASSERTE(IsGarbageCollectorFullyInitialized());

    // In immersive apps the GarbageCollect() call below will call into the WinUI reference tracker,
    // which will call back into the runtime to track references. This call
    // chain would cause a Thread object to be created for this thread while code
    // higher on the stack owns the ThreadStoreLock. This will lead to asserts
    // since the ThreadStoreLock is non-reentrant. To avoid this we'll create
    // the Thread object here instead.
    if (GetThreadNULLOk() == NULL)
    {
        HRESULT hr = E_FAIL;
        SetupThreadNoThrow(&hr);
        if (FAILED(hr))
            return hr;
    }

    ASSERT_NO_EE_LOCKS_HELD();

    EX_TRY
    {
        // Need to switch to cooperative mode as the thread will access managed
        // references (through reference tracker callbacks).
        GCX_COOP();
#endif // FEATURE_NATIVEAOT

        ForcedGCHolder forcedGCHolder;

        hr = GCHeapUtilities::GetGCHeap()->GarbageCollect(
            -1,     // all generations should be collected
            false,  // low_memory_p
            collection_blocking);

#ifndef FEATURE_NATIVEAOT
    }
    EX_CATCH { }
    EX_END_CATCH(RethrowTerminalExceptions);
#endif // FEATURE_NATIVEAOT

    return hr;
}

//---------------------------------------------------------------------------------------
// WalkStaticsAndCOMForETW walks both CCW/RCW objects and static variables.
//---------------------------------------------------------------------------------------

VOID ETW::GCLog::WalkStaticsAndCOMForETW()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    EX_TRY
    {
        BulkTypeEventLogger typeLogger;

        // Walk RCWs/CCWs
        BulkComLogger comLogger(&typeLogger);
        comLogger.LogAllComObjects();

        // Walk static variables
        BulkStaticsLogger staticLogger(&typeLogger);
        staticLogger.LogAllStatics();

        // Ensure all loggers have written all events, fire type logger last to batch events
        // (FireBulkComEvent or FireBulkStaticsEvent may queue up additional types).
        comLogger.FireBulkComEvent();
        staticLogger.FireBulkStaticsEvent();
        typeLogger.FireBulkTypeEvent();
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
}


// Holds state that batches of roots, nodes, edges, and types as the GC walks the heap
// at the end of a collection.
class EtwGcHeapDumpContext
{
public:
    // An instance of EtwGcHeapDumpContext is dynamically allocated and stored inside of
    // ProfilingScanContext and ProfilerWalkHeapContext, which are context structures
    // that the GC heap walker sends back to the callbacks. This method is passed a
    // pointer to ProfilingScanContext::pvEtwContext or
    // ProfilerWalkHeapContext::pvEtwContext; if non-NULL it gets returned; else, a new
    // EtwGcHeapDumpContext is allocated, stored in that pointer, and then returned.
    // Callers should test for NULL, which can be returned if out of memory
    static EtwGcHeapDumpContext* GetOrCreateInGCContext(LPVOID* ppvEtwContext)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ppvEtwContext != NULL);

        EtwGcHeapDumpContext* pContext = (EtwGcHeapDumpContext*)*ppvEtwContext;
        if (pContext == NULL)
        {
            pContext = new (nothrow) EtwGcHeapDumpContext;
            *ppvEtwContext = pContext;
        }
        return pContext;
    }

    EtwGcHeapDumpContext() :
        iCurBulkRootEdge(0),
        iCurBulkRootConditionalWeakTableElementEdge(0),
        iCurBulkNodeEvent(0),
        iCurBulkEdgeEvent(0),
        bulkTypeEventLogger()
    {
        LIMITED_METHOD_CONTRACT;
        ClearRootEdges();
        ClearRootConditionalWeakTableElementEdges();
        ClearNodes();
        ClearEdges();
    }

    // These helpers clear the individual buffers, for use after a flush and on
    // construction.  They intentionally leave the indices (iCur*) alone, since they
    // persist across flushes within a GC

    void ClearRootEdges()
    {
        LIMITED_METHOD_CONTRACT;
        cGcBulkRootEdges = 0;
        ZeroMemory(rgGcBulkRootEdges, sizeof(rgGcBulkRootEdges));
    }

    void ClearRootConditionalWeakTableElementEdges()
    {
        LIMITED_METHOD_CONTRACT;
        cGCBulkRootConditionalWeakTableElementEdges = 0;
        ZeroMemory(rgGCBulkRootConditionalWeakTableElementEdges, sizeof(rgGCBulkRootConditionalWeakTableElementEdges));
    }

    void ClearNodes()
    {
        LIMITED_METHOD_CONTRACT;
        cGcBulkNodeValues = 0;
        ZeroMemory(rgGcBulkNodeValues, sizeof(rgGcBulkNodeValues));
    }

    void ClearEdges()
    {
        LIMITED_METHOD_CONTRACT;
        cGcBulkEdgeValues = 0;
        ZeroMemory(rgGcBulkEdgeValues, sizeof(rgGcBulkEdgeValues));
    }

    //---------------------------------------------------------------------------------------
    // GCBulkRootEdge
    //
    // A "root edge" is the relationship between a source "GCRootID" (i.e., stack
    // variable, handle, static, etc.) and the target "RootedNodeAddress" (the managed
    // object that gets rooted).
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkRootEdge event
    UINT iCurBulkRootEdge;

    // Number of root edges currently filled out in rgGcBulkRootEdges array
    UINT cGcBulkRootEdges;

    // Struct array containing the primary data for each GCBulkRootEdge event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the root edge data)
    EventStructGCBulkRootEdgeValue rgGcBulkRootEdges[(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkRootEdgeValue)];


    //---------------------------------------------------------------------------------------
    // GCBulkRootConditionalWeakTableElementEdge
    //
    // These describe dependent handles, which simulate an edge connecting a key NodeID
    // to a value NodeID.
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkRootConditionalWeakTableElementEdge event
    UINT iCurBulkRootConditionalWeakTableElementEdge;

    // Number of root edges currently filled out in rgGCBulkRootConditionalWeakTableElementEdges array
    UINT cGCBulkRootConditionalWeakTableElementEdges;

    // Struct array containing the primary data for each GCBulkRootConditionalWeakTableElementEdge event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the root edge data)
    EventStructGCBulkRootConditionalWeakTableElementEdgeValue rgGCBulkRootConditionalWeakTableElementEdges
        [(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkRootConditionalWeakTableElementEdgeValue)];

    //---------------------------------------------------------------------------------------
    // GCBulkNode
    //
    // A "node" is ANY managed object sitting on the heap, including RootedNodeAddresses
    // as well as leaf nodes.
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkNode event
    UINT iCurBulkNodeEvent;

    // Number of nodes currently filled out in rgGcBulkNodeValues array
    UINT cGcBulkNodeValues;

    // Struct array containing the primary data for each GCBulkNode event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the node data)
    EventStructGCBulkNodeValue rgGcBulkNodeValues[(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkNodeValue)];

    //---------------------------------------------------------------------------------------
    // GCBulkEdge
    //
    // An "edge" is the relationship between a source node and its referenced target
    // node. Edges are reported in bulk, separately from Nodes, but it is expected that
    // the consumer read the Node and Edge streams together. One takes the first node
    // from the Node stream, and then reads EdgeCount entries in the Edge stream, telling
    // you all of that Node's targets. Then, one takes the next node in the Node stream,
    // and reads the next entries in the Edge stream (using this Node's EdgeCount to
    // determine how many) to find all of its targets. This continues on until the Node
    // and Edge streams have been fully read.
    //
    // GCBulkRootEdges are not duplicated in the GCBulkEdge events. GCBulkEdge events
    // begin at the GCBulkRootEdge.RootedNodeAddress and move forward.
    //
    //---------------------------------------------------------------------------------------

    // Sequence number for each GCBulkEdge event
    UINT iCurBulkEdgeEvent;

    // Number of nodes currently filled out in rgGcBulkEdgeValues array
    UINT cGcBulkEdgeValues;

    // Struct array containing the primary data for each GCBulkEdge event.  Fix the size so
    // the total event stays well below the 64K
    // limit (leaving lots of room for non-struct fields that come before the edge data)
    EventStructGCBulkEdgeValue rgGcBulkEdgeValues[(cbMaxEtwEvent - 0x100) / sizeof(EventStructGCBulkEdgeValue)];


    //---------------------------------------------------------------------------------------
    // BulkType
    //
    // Types are a bit more complicated to batch up, since their data is of varying
    // size.  BulkTypeEventLogger takes care of the pesky details for us
    //---------------------------------------------------------------------------------------

    BulkTypeEventLogger bulkTypeEventLogger;
};



//---------------------------------------------------------------------------------------
//
// Called during a heap walk for each root reference encountered.  Batches up the root in
// the ETW context
//
// Arguments:
//      * pvHandle - If the root is a handle, this points to the handle
//      * pRootedNode - Points to object that is rooted
//      * pSecondaryNodeForDependentHandle - For dependent handles, this is the
//          secondary object
//      * fDependentHandle - nonzero iff this is for a dependent handle
//      * profilingScanContext - The shared profapi/etw context built up during the heap walk.
//      * dwGCFlags - Bitmask of "GC_"-style flags set by GC
//      * rootFlags - Bitmask of EtwGCRootFlags describing the root
//

// static
VOID ETW::GCLog::RootReference(
    LPVOID pvHandle,
    Object* pRootedNode,
    Object* pSecondaryNodeForDependentHandle,
    BOOL fDependentHandle,
    ProfilingScanContext* profilingScanContext,
    DWORD dwGCFlags,
    DWORD rootFlags)
{
    LIMITED_METHOD_CONTRACT;

    EtwGcHeapDumpContext* pContext =
        EtwGcHeapDumpContext::GetOrCreateInGCContext(&profilingScanContext->pvEtwContext);
    if (pContext == NULL)
        return;

    // Determine root kind, root ID, and handle-specific flags
    LPVOID pvRootID = NULL;
    BYTE nRootKind = (BYTE)profilingScanContext->dwEtwRootKind;
    switch (nRootKind)
    {
    case kEtwGCRootKindStack:
#if !defined (FEATURE_NATIVEAOT) && (defined(GC_PROFILING) || defined (DACCESS_COMPILE))
        pvRootID = profilingScanContext->pMD;
#endif // !defined (FEATURE_NATIVEAOT) && (defined(GC_PROFILING) || defined (DACCESS_COMPILE))
        break;

    case kEtwGCRootKindHandle:
        pvRootID = pvHandle;
        break;

    case kEtwGCRootKindFinalizer:
        _ASSERTE(pvRootID == NULL);
        break;

    case kEtwGCRootKindOther:
    default:
        _ASSERTE(nRootKind == kEtwGCRootKindOther);
        _ASSERTE(pvRootID == NULL);
        break;
    }

    // Convert GC root flags to ETW root flags
    if (dwGCFlags & GC_CALL_INTERIOR)
        rootFlags |= kEtwGCRootFlagsInterior;
    if (dwGCFlags & GC_CALL_PINNED)
        rootFlags |= kEtwGCRootFlagsPinning;

    // Add root edge to appropriate buffer
    if (fDependentHandle)
    {
        _ASSERTE(pContext->cGCBulkRootConditionalWeakTableElementEdges <
            ARRAY_SIZE(pContext->rgGCBulkRootConditionalWeakTableElementEdges));
        EventStructGCBulkRootConditionalWeakTableElementEdgeValue* pRCWTEEdgeValue =
            &pContext->rgGCBulkRootConditionalWeakTableElementEdges[pContext->cGCBulkRootConditionalWeakTableElementEdges];
        pRCWTEEdgeValue->GCKeyNodeID = pRootedNode;
        pRCWTEEdgeValue->GCValueNodeID = pSecondaryNodeForDependentHandle;
        pRCWTEEdgeValue->GCRootID = pvRootID;
        pContext->cGCBulkRootConditionalWeakTableElementEdges++;

        // If RCWTE edge buffer is now full, empty it into ETW
        if (pContext->cGCBulkRootConditionalWeakTableElementEdges ==
            ARRAY_SIZE(pContext->rgGCBulkRootConditionalWeakTableElementEdges))
        {
            FireEtwGCBulkRootConditionalWeakTableElementEdge(
                pContext->iCurBulkRootConditionalWeakTableElementEdge,
                pContext->cGCBulkRootConditionalWeakTableElementEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]),
                &pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]);

            pContext->iCurBulkRootConditionalWeakTableElementEdge++;
            pContext->ClearRootConditionalWeakTableElementEdges();
        }
    }
    else
    {
        _ASSERTE(pContext->cGcBulkRootEdges < ARRAY_SIZE(pContext->rgGcBulkRootEdges));
        EventStructGCBulkRootEdgeValue* pBulkRootEdgeValue = &pContext->rgGcBulkRootEdges[pContext->cGcBulkRootEdges];
        pBulkRootEdgeValue->RootedNodeAddress = pRootedNode;
        pBulkRootEdgeValue->GCRootKind = nRootKind;
        pBulkRootEdgeValue->GCRootFlag = rootFlags;
        pBulkRootEdgeValue->GCRootID = pvRootID;
        pContext->cGcBulkRootEdges++;

        // If root edge buffer is now full, empty it into ETW
        if (pContext->cGcBulkRootEdges == ARRAY_SIZE(pContext->rgGcBulkRootEdges))
        {
            FireEtwGCBulkRootEdge(
                pContext->iCurBulkRootEdge,
                pContext->cGcBulkRootEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkRootEdges[0]),
                &pContext->rgGcBulkRootEdges[0]);

            pContext->iCurBulkRootEdge++;
            pContext->ClearRootEdges();
        }
    }
}

//---------------------------------------------------------------------------------------
//
// Called during a heap walk for each object reference encountered.  Batches up the
// corresponding node, edges, and type data for the ETW events.
//
// Arguments:
//      * profilerWalkHeapContext - The shared profapi/etw context built up during the heap walk.
//      * pObjReferenceSource - Object doing the pointing
//      * typeID - Type of pObjReferenceSource
//      * fDependentHandle - nonzero iff this is for a dependent handle
//      * cRefs - Count of objects being pointed to
//      * rgObjReferenceTargets - Array of objects being pointed to
//

// static
VOID ETW::GCLog::ObjectReference(
    ProfilerWalkHeapContext* profilerWalkHeapContext,
    Object* pObjReferenceSource,
    ULONGLONG typeID,
    ULONGLONG cRefs,
    Object** rgObjReferenceTargets)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        // LogTypeAndParametersIfNecessary can take a lock
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    EtwGcHeapDumpContext* pContext =
        EtwGcHeapDumpContext::GetOrCreateInGCContext(&profilerWalkHeapContext->pvEtwContext);
    if (pContext == NULL)
        return;

    //---------------------------------------------------------------------------------------
    //    GCBulkNode events
    //---------------------------------------------------------------------------------------

    // Add Node (pObjReferenceSource) to buffer
    _ASSERTE(pContext->cGcBulkNodeValues < ARRAY_SIZE(pContext->rgGcBulkNodeValues));
    EventStructGCBulkNodeValue* pBulkNodeValue = &pContext->rgGcBulkNodeValues[pContext->cGcBulkNodeValues];
    pBulkNodeValue->Address = pObjReferenceSource;
    pBulkNodeValue->Size = pObjReferenceSource->GetSize();
    pBulkNodeValue->TypeID = typeID;
    pBulkNodeValue->EdgeCount = cRefs;
    pContext->cGcBulkNodeValues++;

    // If Node buffer is now full, empty it into ETW
    if (pContext->cGcBulkNodeValues == ARRAY_SIZE(pContext->rgGcBulkNodeValues))
    {
        FireEtwGCBulkNode(
            pContext->iCurBulkNodeEvent,
            pContext->cGcBulkNodeValues,
            GetClrInstanceId(),
            sizeof(pContext->rgGcBulkNodeValues[0]),
            &pContext->rgGcBulkNodeValues[0]);

        pContext->iCurBulkNodeEvent++;
        pContext->ClearNodes();
    }

    //---------------------------------------------------------------------------------------
    //    BulkType events
    //---------------------------------------------------------------------------------------

    // We send type information as necessary--only for nodes, and only for nodes that we
    // haven't already sent type info for
    if (typeID != 0)
    {
        ETW::TypeSystemLog::LogTypeAndParametersIfNecessary(
            &pContext->bulkTypeEventLogger,     // Batch up this type with others to minimize events
            typeID,

            // During heap walk, GC holds the lock for us, so we can directly enter the
            // hash to see if the type has already been logged
            ETW::TypeSystemLog::kTypeLogBehaviorTakeLockAndLogIfFirstTime
            );
    }

    //---------------------------------------------------------------------------------------
    //    GCBulkEdge events
    //---------------------------------------------------------------------------------------

    // Add Edges (rgObjReferenceTargets) to buffer. Buffer could fill up before all edges
    // are added (it could even fill up multiple times during this one call if there are
    // a lot of edges), so empty Edge buffer into ETW as we go along, as many times as we
    // need.

    for (ULONGLONG i = 0; i < cRefs; i++)
    {
        _ASSERTE(pContext->cGcBulkEdgeValues < ARRAY_SIZE(pContext->rgGcBulkEdgeValues));
        EventStructGCBulkEdgeValue* pBulkEdgeValue = &pContext->rgGcBulkEdgeValues[pContext->cGcBulkEdgeValues];
        pBulkEdgeValue->Value = rgObjReferenceTargets[i];
        // FUTURE: ReferencingFieldID
        pBulkEdgeValue->ReferencingFieldID = 0;
        pContext->cGcBulkEdgeValues++;

        // If Edge buffer is now full, empty it into ETW
        if (pContext->cGcBulkEdgeValues == ARRAY_SIZE(pContext->rgGcBulkEdgeValues))
        {
            FireEtwGCBulkEdge(
                pContext->iCurBulkEdgeEvent,
                pContext->cGcBulkEdgeValues,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkEdgeValues[0]),
                &pContext->rgGcBulkEdgeValues[0]);

            pContext->iCurBulkEdgeEvent++;
            pContext->ClearEdges();
        }
    }
}

//---------------------------------------------------------------------------------------
//
// Called by GC at end of heap dump to give us a convenient time to flush any remaining
// buffers of data to ETW
//
// Arguments:
//      profilerWalkHeapContext - Context containing data we've batched up
//

// static
VOID ETW::GCLog::EndHeapDump(ProfilerWalkHeapContext* profilerWalkHeapContext)
{
    LIMITED_METHOD_CONTRACT;

    // If context isn't already set up for us, then we haven't been collecting any data
    // for ETW events.
    EtwGcHeapDumpContext* pContext = (EtwGcHeapDumpContext*)profilerWalkHeapContext->pvEtwContext;
    if (pContext == NULL)
        return;

    // If the GC events are enabled, flush any remaining root, node, and / or edge data
    if (s_forcedGCInProgress &&
        ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
                                     TRACE_LEVEL_INFORMATION,
                                     CLR_GCHEAPDUMP_KEYWORD))
    {
        if (pContext->cGcBulkRootEdges > 0)
        {
            FireEtwGCBulkRootEdge(
                pContext->iCurBulkRootEdge,
                pContext->cGcBulkRootEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkRootEdges[0]),
                &pContext->rgGcBulkRootEdges[0]);
        }

        if (pContext->cGCBulkRootConditionalWeakTableElementEdges > 0)
        {
            FireEtwGCBulkRootConditionalWeakTableElementEdge(
                pContext->iCurBulkRootConditionalWeakTableElementEdge,
                pContext->cGCBulkRootConditionalWeakTableElementEdges,
                GetClrInstanceId(),
                sizeof(pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]),
                &pContext->rgGCBulkRootConditionalWeakTableElementEdges[0]);
        }

        if (pContext->cGcBulkNodeValues > 0)
        {
            FireEtwGCBulkNode(
                pContext->iCurBulkNodeEvent,
                pContext->cGcBulkNodeValues,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkNodeValues[0]),
                &pContext->rgGcBulkNodeValues[0]);
        }

        if (pContext->cGcBulkEdgeValues > 0)
        {
            FireEtwGCBulkEdge(
                pContext->iCurBulkEdgeEvent,
                pContext->cGcBulkEdgeValues,
                GetClrInstanceId(),
                sizeof(pContext->rgGcBulkEdgeValues[0]),
                &pContext->rgGcBulkEdgeValues[0]);
        }
    }

    // Ditto for type events
    if (ETW_TRACING_CATEGORY_ENABLED(
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        pContext->bulkTypeEventLogger.FireBulkTypeEvent();
    }

    // Delete any GC state built up in the context
    profilerWalkHeapContext->pvEtwContext = NULL;
    delete pContext;
}
