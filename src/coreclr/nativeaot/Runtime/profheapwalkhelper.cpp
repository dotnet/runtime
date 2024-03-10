// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// On desktop CLR, GC ETW event firing borrows heavily from code in the profiling API,
// as the GC already called hooks in the profapi to notify it of roots & references.
// This file shims up that profapi code the GC expects, though only for the purpose of
// firing ETW events (not for getting a full profapi up on redhawk).
//

#include "common.h"

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

#include "gcenv.h"
#include "gcheaputilities.h"
#include "eventtrace.h"
#include "profheapwalkhelper.h"

//---------------------------------------------------------------------------------------
//
// Callback of type ScanFunc called by GC while scanning roots (in GCProfileWalkHeap,
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

void ScanRootsHelper(Object* pObj, Object** ppRoot, ScanContext * pSC, DWORD dwFlags)
{
    ProfilingScanContext *pPSC = (ProfilingScanContext *)pSC;

    DWORD dwEtwRootFlags = 0;
    if (dwFlags & GC_CALL_INTERIOR)
        dwEtwRootFlags |= kEtwGCRootFlagsInterior;
    if (dwFlags & GC_CALL_PINNED)
        dwEtwRootFlags |= kEtwGCRootFlagsPinning;

    // Notify ETW of the root

    if (ETW::GCLog::ShouldWalkHeapRootsForEtw())
    {
        ETW::GCLog::RootReference(
            ppRoot,         // root address
            pObj,           // object being rooted
            NULL,           // pSecondaryNodeForDependentHandle is NULL, cuz this isn't a dependent handle
            FALSE,          // is dependent handle
            pPSC,
            dwFlags,        // dwGCFlags
            dwEtwRootFlags);
    }
}

//---------------------------------------------------------------------------------------
//
// Callback of type walk_fn used by GCHeap::WalkObject.  Keeps a count of each
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
    UNREFERENCED_PARAMETER(pBO);
    // Increase the count
    (*((size_t *)context))++;

    return TRUE;
}

//---------------------------------------------------------------------------------------
//
// Callback of type walk_fn used by GCHeap::WalkObject.  Stores each object reference
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
// Callback of type walk_fn used by the GC when walking the heap, to help profapi
// track objects.  This guy orchestrates the use of the above callbacks which dig
// into object references contained each object encountered by this callback.
//
// Arguments:
//      pBO - Object reference encountered on the heap
//
// Return Value:
//      BOOL indicating whether the heap walk should continue.
//      TRUE=continue
//      FALSE=stop
//

bool HeapWalkHelper(Object * pBO, void * pvContext)
{
    OBJECTREF *   arrObjRef      = NULL;
    size_t        cNumRefs       = 0;
    bool          bOnStack       = false;
    //MethodTable * pMT            = pBO->GetMethodTable();

    ProfilerWalkHeapContext * pProfilerWalkHeapContext = (ProfilerWalkHeapContext *) pvContext;

    //if (pMT->ContainsPointersOrCollectible())
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

#ifdef FEATURE_ETW
    if (ETW::GCLog::ShouldWalkHeapObjectsForEtw())
    {
        ETW::GCLog::ObjectReference(
            pProfilerWalkHeapContext,
            pBO,
            ULONGLONG(pBO->GetGCSafeMethodTable()),
            cNumRefs,
            (Object **) arrObjRef);
    }
#endif // FEATURE_ETW

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
    return TRUE;
}

#endif // defined(FEATURE_EVENT_TRACE) || defined(GC_PROFILING)
