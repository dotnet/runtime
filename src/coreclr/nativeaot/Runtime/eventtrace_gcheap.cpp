// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: eventtrace_gcheap.cpp
// Event Tracing support for GC heap dump and movement tracking
//

#include "common.h"

#include "gcenv.h"
#include "gcheaputilities.h"

#include "daccess.h"

#include "slist.h"
#include "varint.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "eventtrace_etw.h"
#include "eventtracebase.h"
#include "eventtracepriv.h"
#include "profheapwalkhelper.h"

/****************************************************************************/
/* Methods that are called from the runtime */
/****************************************************************************/


// Simple helpers called by the GC to decide whether it needs to do a walk of heap
// objects and / or roots.

BOOL ETW::GCLog::ShouldWalkHeapObjectsForEtw()
{
    return RUNTIME_PROVIDER_CATEGORY_ENABLED(
            TRACE_LEVEL_INFORMATION,
            CLR_GCHEAPDUMP_KEYWORD);    
}

BOOL ETW::GCLog::ShouldWalkHeapRootsForEtw()
{
    return RUNTIME_PROVIDER_CATEGORY_ENABLED(
            TRACE_LEVEL_INFORMATION,
            CLR_GCHEAPDUMP_KEYWORD);    
}

BOOL ETW::GCLog::ShouldTrackMovementForEtw()
{
    return RUNTIME_PROVIDER_CATEGORY_ENABLED(
        TRACE_LEVEL_INFORMATION,
        CLR_GCHEAPSURVIVALANDMOVEMENT_KEYWORD);
}

BOOL ETW::GCLog::ShouldWalkStaticsAndCOMForEtw()
{
    // @TODO
    return false;
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
    BOOL /*fAllowProfApiNotification*/) // @TODO: unused param from newer implementation
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
        (MovedReferenceContextForEtwAndProfapi*)profilingContext;
    if (pCtxForEtwAndProfapi == NULL)
    {
        _ASSERTE(!"MovedReference() encountered a NULL profilingContext");
        return;
    }

#ifdef PROFILING_SUPPORTED
    // ProfAPI
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        g_profControlBlock.pProfInterface->MovedReference(pbMemBlockStart,
            pbMemBlockEnd,
            cbRelocDistance,
            &(pCtxForEtwAndProfapi->pctxProfAPI),
            fCompacting);
        END_PIN_PROFILER();
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

        _ASSERTE(pContext->cBulkMovedObjectRanges < _countof(pContext->rgGCBulkMovedObjectRanges));
        EventStructGCBulkMovedObjectRangesValue* pValue =
            &pContext->rgGCBulkMovedObjectRanges[pContext->cBulkMovedObjectRanges];
        pValue->OldRangeBase = pbMemBlockStart;
        pValue->NewRangeBase = pbMemBlockStart + cbRelocDistance;
        pValue->RangeLength = pbMemBlockEnd - pbMemBlockStart;
        pContext->cBulkMovedObjectRanges++;

        // If buffer is now full, empty it into ETW
        if (pContext->cBulkMovedObjectRanges == _countof(pContext->rgGCBulkMovedObjectRanges))
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

        _ASSERTE(pContext->cBulkSurvivingObjectRanges < _countof(pContext->rgGCBulkSurvivingObjectRanges));
        EventStructGCBulkSurvivingObjectRangesValue* pValue =
            &pContext->rgGCBulkSurvivingObjectRanges[pContext->cBulkSurvivingObjectRanges];
        pValue->RangeBase = pbMemBlockStart;
        pValue->RangeLength = pbMemBlockEnd - pbMemBlockStart;
        pContext->cBulkSurvivingObjectRanges++;

        // If buffer is now full, empty it into ETW
        if (pContext->cBulkSurvivingObjectRanges == _countof(pContext->rgGCBulkSurvivingObjectRanges))
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
void ETW::GCLog::BeginMovedReferences(size_t* pProfilingContext)
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
void ETW::GCLog::EndMovedReferences(size_t profilingContext,
    BOOL /*fAllowProfApiNotification*/) // @TODO: unused param from newer implementation
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    MovedReferenceContextForEtwAndProfapi* pCtxForEtwAndProfapi = (MovedReferenceContextForEtwAndProfapi*)profilingContext;
    if (pCtxForEtwAndProfapi == NULL)
    {
        _ASSERTE(!"EndMovedReferences() encountered a NULL profilingContext");
        return;
    }

#ifdef PROFILING_SUPPORTED
    // ProfAPI
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        g_profControlBlock.pProfInterface->EndMovedReferences(&(pCtxForEtwAndProfapi->pctxProfAPI));
        END_PIN_PROFILER();
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
void ETW::GCLog::ForceGC(LONGLONG l64ClientSequenceNumber)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!GCHeapUtilities::IsGCHeapInitialized())
        return;

    // No InterlockedExchange64 on Redhawk, even though there is one for
    // InterlockedCompareExchange64. Technically, there's a race here by using
    // InterlockedCompareExchange64, but it's not worth addressing. The race would be
    // between two ETW controllers trying to trigger GCs simultaneously, in which case
    // one will win and get its sequence number to appear in the GCStart event, while the
    // other will lose. Rare, uninteresting, and low-impact.
    PalInterlockedCompareExchange64(&s_l64LastClientSequenceNumber, l64ClientSequenceNumber, s_l64LastClientSequenceNumber);

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

    _ASSERTE(GCHeapUtilities::IsGCHeapInitialized());

    ThreadStore::AttachCurrentThread();
    Thread* pThread = ThreadStore::GetCurrentThread();

    // While doing the GC, much code assumes & asserts the thread doing the GC is in
    // cooperative mode (but DisablePreemptiveMode cannot be used until a valid deferred
    // transition frame is put into place).
    pThread->SetDeferredTransitionFrameForNativeHelperThread();
    pThread->DisablePreemptiveMode();

    hr = GCHeapUtilities::GetGCHeap()->GarbageCollect(
        -1,     // all generations should be collected
        FALSE,  // low_memory_p
        collection_blocking);

    // In case this thread (generated by the ETW OS APIs) hangs around a while,
    // better stick it back into preemptive mode, so it doesn't block any other GCs
    pThread->EnablePreemptiveMode();

    return hr;
}

//---------------------------------------------------------------------------------------
// WalkStaticsAndCOMForETW walks both CCW/RCW objects and static variables.
//---------------------------------------------------------------------------------------

void ETW::GCLog::WalkStaticsAndCOMForETW()
{
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
void ETW::GCLog::RootReference(
    LPVOID pvHandle,
    Object* pRootedNode,
    Object* pSecondaryNodeForDependentHandle,
    BOOL fDependentHandle,
    ProfilingScanContext* profilingScanContext,
    DWORD dwGCFlags,
    DWORD rootFlags)
{
    LIMITED_METHOD_CONTRACT;

    if (pRootedNode == NULL)
        return;

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
            _countof(pContext->rgGCBulkRootConditionalWeakTableElementEdges));
        EventStructGCBulkRootConditionalWeakTableElementEdgeValue* pRCWTEEdgeValue =
            &pContext->rgGCBulkRootConditionalWeakTableElementEdges[pContext->cGCBulkRootConditionalWeakTableElementEdges];
        pRCWTEEdgeValue->GCKeyNodeID = pRootedNode;
        pRCWTEEdgeValue->GCValueNodeID = pSecondaryNodeForDependentHandle;
        pRCWTEEdgeValue->GCRootID = pvRootID;
        pContext->cGCBulkRootConditionalWeakTableElementEdges++;

        // If RCWTE edge buffer is now full, empty it into ETW
        if (pContext->cGCBulkRootConditionalWeakTableElementEdges ==
            _countof(pContext->rgGCBulkRootConditionalWeakTableElementEdges))
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
        _ASSERTE(pContext->cGcBulkRootEdges < _countof(pContext->rgGcBulkRootEdges));
        EventStructGCBulkRootEdgeValue* pBulkRootEdgeValue = &pContext->rgGcBulkRootEdges[pContext->cGcBulkRootEdges];
        pBulkRootEdgeValue->RootedNodeAddress = pRootedNode;
        pBulkRootEdgeValue->GCRootKind = nRootKind;
        pBulkRootEdgeValue->GCRootFlag = rootFlags;
        pBulkRootEdgeValue->GCRootID = pvRootID;
        pContext->cGcBulkRootEdges++;

        // If root edge buffer is now full, empty it into ETW
        if (pContext->cGcBulkRootEdges == _countof(pContext->rgGcBulkRootEdges))
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
void ETW::GCLog::ObjectReference(
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
    _ASSERTE(pContext->cGcBulkNodeValues < _countof(pContext->rgGcBulkNodeValues));
    EventStructGCBulkNodeValue* pBulkNodeValue = &pContext->rgGcBulkNodeValues[pContext->cGcBulkNodeValues];
    pBulkNodeValue->Address = pObjReferenceSource;
    pBulkNodeValue->Size = pObjReferenceSource->GetSize();
    pBulkNodeValue->TypeID = typeID;
    pBulkNodeValue->EdgeCount = cRefs;
    pContext->cGcBulkNodeValues++;

    // If Node buffer is now full, empty it into ETW
    if (pContext->cGcBulkNodeValues == _countof(pContext->rgGcBulkNodeValues))
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
        // Batch up this type with others to minimize events
        pContext->bulkTypeEventLogger.LogTypeAndParameters(typeID);
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
        _ASSERTE(pContext->cGcBulkEdgeValues < _countof(pContext->rgGcBulkEdgeValues));
        EventStructGCBulkEdgeValue* pBulkEdgeValue = &pContext->rgGcBulkEdgeValues[pContext->cGcBulkEdgeValues];
        pBulkEdgeValue->Value = rgObjReferenceTargets[i];
        // FUTURE: ReferencingFieldID
        pBulkEdgeValue->ReferencingFieldID = 0;
        pContext->cGcBulkEdgeValues++;

        // If Edge buffer is now full, empty it into ETW
        if (pContext->cGcBulkEdgeValues == _countof(pContext->rgGcBulkEdgeValues))
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
void ETW::GCLog::EndHeapDump(ProfilerWalkHeapContext* profilerWalkHeapContext)
{
    LIMITED_METHOD_CONTRACT;

    // If context isn't already set up for us, then we haven't been collecting any data
    // for ETW events.
    EtwGcHeapDumpContext* pContext = (EtwGcHeapDumpContext*)profilerWalkHeapContext->pvEtwContext;
    if (pContext == NULL)
        return;

    // If the GC events are enabled, flush any remaining root, node, and / or edge data
    if (RUNTIME_PROVIDER_CATEGORY_ENABLED(
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
    if (RUNTIME_PROVIDER_CATEGORY_ENABLED(
        TRACE_LEVEL_INFORMATION,
        CLR_TYPE_KEYWORD))
    {
        pContext->bulkTypeEventLogger.FireBulkTypeEvent();
        pContext->bulkTypeEventLogger.Cleanup();
    }

    // Delete any GC state built up in the context
    profilerWalkHeapContext->pvEtwContext = NULL;
    delete pContext;
}

namespace
{
    void ProfScanRootsHelper(Object** ppObject, ScanContext* pSC, uint32_t dwFlags)
    {
        Object* pObj = *ppObject;
        if (dwFlags& GC_CALL_INTERIOR)
        {
            pObj = GCHeapUtilities::GetGCHeap()->GetContainingObject(pObj, true);
            if (pObj == nullptr)
                return;
        }
        ScanRootsHelper(pObj, ppObject, pSC, dwFlags);
    }

    void GcScanRootsForETW(ScanFunc* fn, int condemned, int max_gen, ScanContext* sc)
    {
        UNREFERENCED_PARAMETER(condemned);
        UNREFERENCED_PARAMETER(max_gen);

        FOREACH_THREAD(pThread)
        {
            if (pThread->IsGCSpecial())
                continue;

            if (GCHeapUtilities::GetGCHeap()->IsThreadUsingAllocationContextHeap(pThread->GetAllocContext(), sc->thread_number))
                continue;

            sc->thread_under_crawl = pThread;
            sc->dwEtwRootKind = kEtwGCRootKindStack;
            pThread->GcScanRoots(fn, sc);
            sc->dwEtwRootKind = kEtwGCRootKindOther;
        }
        END_FOREACH_THREAD
    }

    void ScanHandleForETW(Object** pRef, Object* pSec, uint32_t flags, ScanContext* context, bool isDependent)
    {
        ProfilingScanContext* pSC = (ProfilingScanContext*)context;

        // Notify ETW of the handle
        if (ETW::GCLog::ShouldWalkHeapRootsForEtw())
        {
            ETW::GCLog::RootReference(
                pRef,
                *pRef,          // object being rooted
                pSec,           // pSecondaryNodeForDependentHandle
                isDependent,
                pSC,
                0,              // dwGCFlags,
                flags);     // ETW handle flags
        }
    }

    // This is called only if we've determined that either:
    //     a) The Profiling API wants to do a walk of the heap, and it has pinned the
    //     profiler in place (so it cannot be detached), and it's thus safe to call into the
    //     profiler, OR
    //     b) ETW infrastructure wants to do a walk of the heap either to log roots,
    //     objects, or both.
    // This can also be called to do a single walk for BOTH a) and b) simultaneously.  Since
    // ETW can ask for roots, but not objects
    void GCProfileWalkHeapWorker(BOOL fShouldWalkHeapRootsForEtw, BOOL fShouldWalkHeapObjectsForEtw)
    {
        ProfilingScanContext SC(FALSE);
        unsigned max_generation = GCHeapUtilities::GetGCHeap()->GetMaxGeneration();

        // **** Scan roots:  Only scan roots if profiling API wants them or ETW wants them.
        if (fShouldWalkHeapRootsForEtw)
        {
            GcScanRootsForETW(&ProfScanRootsHelper, max_generation, max_generation, &SC);
            SC.dwEtwRootKind = kEtwGCRootKindFinalizer;
            GCHeapUtilities::GetGCHeap()->DiagScanFinalizeQueue(&ProfScanRootsHelper, &SC);

            // Handles are kept independent of wks/svr/concurrent builds
            SC.dwEtwRootKind = kEtwGCRootKindHandle;
            GCHeapUtilities::GetGCHeap()->DiagScanHandles(&ScanHandleForETW, max_generation, &SC);
        }

        // **** Scan dependent handles: only if ETW wants roots
        if (fShouldWalkHeapRootsForEtw)
        {
            // GcScanDependentHandlesForProfiler double-checks
            // CORProfilerTrackConditionalWeakTableElements() before calling into the profiler

            ProfilingScanContext* pSC = &SC;

            // we'll re-use pHeapId (which was either unused (0) or freed by EndRootReferences2
            // (-1)), so reset it to NULL
            _ASSERTE((*((size_t *)(&pSC->pHeapId)) == (size_t)(-1)) ||
                    (*((size_t *)(&pSC->pHeapId)) == (size_t)(0)));
            pSC->pHeapId = NULL;

            GCHeapUtilities::GetGCHeap()->DiagScanDependentHandles(&ScanHandleForETW, max_generation, &SC);
        }

        ProfilerWalkHeapContext profilerWalkHeapContext(FALSE, SC.pvEtwContext);

        // **** Walk objects on heap: only if ETW wants them.
        if (fShouldWalkHeapObjectsForEtw)
        {
            GCHeapUtilities::GetGCHeap()->DiagWalkHeap(&HeapWalkHelper, &profilerWalkHeapContext, max_generation, true /* walk the large object heap */);
        }

        // **** Done! Indicate to ETW helpers that the heap walk is done, so any buffers
        // should be flushed into the ETW stream
        if (fShouldWalkHeapObjectsForEtw || fShouldWalkHeapRootsForEtw)
        {
            ETW::GCLog::EndHeapDump(&profilerWalkHeapContext);
        }
    }
}

void ETW::GCLog::WalkHeap()
{
    if (ETW::GCLog::ShouldWalkStaticsAndCOMForEtw())
        ETW::GCLog::WalkStaticsAndCOMForETW();

    BOOL fShouldWalkHeapRootsForEtw = ETW::GCLog::ShouldWalkHeapRootsForEtw();
    BOOL fShouldWalkHeapObjectsForEtw = ETW::GCLog::ShouldWalkHeapObjectsForEtw();

    // we need to walk the heap if one of GC_PROFILING or FEATURE_EVENT_TRACE
    // is defined, since both of them make use of the walk heap worker.
    if (fShouldWalkHeapRootsForEtw || fShouldWalkHeapObjectsForEtw)
    {
        GCProfileWalkHeapWorker(fShouldWalkHeapRootsForEtw, fShouldWalkHeapObjectsForEtw);
    }
}
