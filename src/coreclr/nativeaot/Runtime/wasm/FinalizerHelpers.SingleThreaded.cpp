// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged portion of finalization implementation for a single-threaded environment.
// Currently, only supports explicit finalization via WaitForPendingFinalizers.
//
#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

// Set when we have finalizable objects in the queue. Used for quick early outs.
bool g_FinalizationRequestPending = false;
static bool g_FinalizationInProgress = false;

// Finalizer method implemented by the managed runtime.
extern "C" void RhpProcessFinalizersAndReturn();

void FinalizeFinalizableObjects()
{
    // Must be called in preemptive mode as "FinalizeFinalizableObjects" RPIs back into managed.
    ASSERT(!ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    static bool s_finalizing = false;

    // Recursive wait on finalization is a no-op.
    if (g_FinalizationRequestPending && !g_FinalizationInProgress)
    {
        g_FinalizationInProgress = true;
        RhpProcessFinalizersAndReturn();

        // Note that managed code from above may have added new objects into the queue (via, e. g.,
        // "GC.ReRegisterForFinalize"). Let them wait for the next 'top-level' GC cycle. Restarting
        // now could lead to an infinite loop with "self-rearming" finalizers.
        g_FinalizationRequestPending = false;
        g_FinalizationInProgress = false;
    }
}

bool RhInitializeFinalization()
{
    return true;
}

// This method is called at the end of GC in case finalizable objects were present.
void RhEnableFinalization()
{
    g_FinalizationRequestPending = true;
}

EXTERN_C void QCALLTYPE RhWaitForPendingFinalizers(UInt32_BOOL allowReentrantWait)
{
    FinalizeFinalizableObjects();
}

// Fetch next object which needs finalization or return null if we've reached the end of the list.
FCIMPL0 (OBJECTREF, RhpGetNextFinalizableObject)
{
    while (true)
    {
        // Get the next finalizable object. If we get back NULL we've reached the end of the list.
        OBJECTREF refNext = GCHeapUtilities::GetGCHeap()->GetNextFinalizable();
        if (refNext == NULL)
            return NULL;

        // The queue may contain objects which have been marked as finalized already (via GC.SuppressFinalize()
        // for instance). Skip finalization for these but reset the flag so that the object can be put back on
        // the list with RegisterForFinalization().
        if (refNext->GetHeader()->GetBits() & BIT_SBLK_FINALIZER_RUN)
        {
            refNext->GetHeader()->ClrBit(BIT_SBLK_FINALIZER_RUN);
            continue;
        }

        // We've found the first finalizable object, return it to the caller.
        return refNext;
    }
}
FCIMPLEND
