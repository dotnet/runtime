// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged helpers called by the managed finalizer thread.
//
#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"

#include "slist.h"
#include "RuntimeInstance.h"
#include "shash.h"

#include "regdisplay.h"
#include "StackFrameIterator.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "yieldprocessornormalized.h"

GPTR_DECL(Thread, g_pFinalizerThread);

CLREventStatic g_FinalizerEvent;
CLREventStatic g_FinalizerDoneEvent;

extern "C" void __cdecl ProcessFinalizers();

// Unmanaged front-end to the finalizer thread. We require this because at the point the GC creates the
// finalizer thread we can't run managed code. Instead this method waits
// for the first finalization request (by which time everything must be up and running) and kicks off the
// managed portion of the thread at that point
uint32_t WINAPI FinalizerStart(void* pContext)
{
    HANDLE hFinalizerEvent = (HANDLE)pContext;

    ThreadStore::AttachCurrentThread();
    Thread * pThread = ThreadStore::GetCurrentThread();

    // Disallow gcstress on this thread to work around the current implementation's limitation that it will
    // get into an infinite loop if performed on the finalizer thread.
    pThread->SetSuppressGcStress();

    g_pFinalizerThread = PTR_Thread(pThread);

    // We have some time until the first finalization request - use the time to calibrate normalized waits.
    EnsureYieldProcessorNormalizedInitialized();

    // Wait for a finalization request.
    uint32_t uResult = PalWaitForSingleObjectEx(hFinalizerEvent, INFINITE, FALSE);
    ASSERT(uResult == WAIT_OBJECT_0);

    // Since we just consumed the request (and the event is auto-reset) we must set the event again so the
    // managed finalizer code will immediately start processing the queue when we run it.
    UInt32_BOOL fResult = PalSetEvent(hFinalizerEvent);
    ASSERT(fResult);

    // Run the managed portion of the finalizer. This call will never return.

    ProcessFinalizers();

    ASSERT(!"Finalizer thread should never return");
    return 0;
}

bool RhInitializeFinalization()
{
    // Allocate the events the GC expects the finalizer thread to have. The g_FinalizerEvent event is signalled
    // by the GC whenever it completes a collection where it found otherwise unreachable finalizable objects.
    // The g_FinalizerDoneEvent is set by the finalizer thread every time it wakes up and drains the
    // queue of finalizable objects. It's mainly used by GC.WaitForPendingFinalizers().
    if (!g_FinalizerEvent.CreateAutoEventNoThrow(false))
        return false;
    if (!g_FinalizerDoneEvent.CreateManualEventNoThrow(false))
        return false;

    // Create the finalizer thread itself.
    if (!PalStartFinalizerThread(FinalizerStart, (void*)g_FinalizerEvent.GetOSEvent()))
        return false;

    return true;
}

void RhEnableFinalization()
{
    g_FinalizerEvent.Set();
}

EXTERN_C NATIVEAOT_API void __cdecl RhInitializeFinalizerThread()
{
    g_FinalizerEvent.Set();
}

EXTERN_C NATIVEAOT_API void __cdecl RhWaitForPendingFinalizers(UInt32_BOOL allowReentrantWait)
{
    // This must be called via p/invoke rather than RuntimeImport since it blocks and could starve the GC if
    // called in cooperative mode.
    ASSERT(!ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    // Can't call this from the finalizer thread itself.
    if (ThreadStore::GetCurrentThread() != g_pFinalizerThread)
    {
        // Clear any current indication that a finalization pass is finished and wake the finalizer thread up
        // (if there's no work to do it'll set the done event immediately).
        g_FinalizerDoneEvent.Reset();
        g_FinalizerEvent.Set();

        // Wait for the finalizer thread to get back to us.
        g_FinalizerDoneEvent.Wait(INFINITE, false, allowReentrantWait);
    }
}

// Block the current thread until at least one object needs to be finalized (returns true) or memory is low
// (returns false and the finalizer thread should initiate a garbage collection).
EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhpWaitForFinalizerRequest()
{
    // We can wait for two events; finalization queue has been populated and low memory resource notification.
    // But if the latter is signalled we shouldn't wait on it again immediately -- if the garbage collection
    // the finalizer thread initiates as a result is not sufficient to remove the low memory condition the
    // event will still be signalled and we'll end up looping doing cpu intensive collections, which won't
    // help the situation at all and could make it worse. So we remember whether the last event we reported
    // was low memory and if so we'll wait at least two seconds (the CLR value) on just a finalization
    // request.
    static bool fLastEventWasLowMemory = false;

    IGCHeap * pHeap = GCHeapUtilities::GetGCHeap();

    // Wait in a loop because we may have to retry if we decide to only wait for finalization events but the
    // two second timeout expires.
    do
    {
        HANDLE  lowMemEvent = NULL;
#if 0 // TODO: hook up low memory notification
        lowMemEvent = pHeap->GetLowMemoryNotificationEvent();
        HANDLE  rgWaitHandles[] = { g_FinalizerEvent.GetOSEvent(), lowMemEvent };
        uint32_t  cWaitHandles = (fLastEventWasLowMemory || (lowMemEvent == NULL)) ? 1 : 2;
        uint32_t  uTimeout = fLastEventWasLowMemory ? 2000 : INFINITE;

        uint32_t uResult = PalWaitForMultipleObjectsEx(cWaitHandles, rgWaitHandles, FALSE, uTimeout, FALSE);
#else
        uint32_t uResult = PalWaitForSingleObjectEx(g_FinalizerEvent.GetOSEvent(), INFINITE, FALSE);
#endif

        switch (uResult)
        {
        case WAIT_OBJECT_0:
            // At least one object is ready for finalization.
            {
                // Process pending finalizer work items from the GC first.
                FinalizerWorkItem* pWork = pHeap->GetExtraWorkForFinalization();
                while (pWork != NULL)
                {
                    FinalizerWorkItem* pNext = pWork->next;
                    pWork->callback(pWork);
                    pWork = pNext;
                }
            }
            FireEtwGCFinalizersBegin_V1(GetClrInstanceId());
            return TRUE;

        case WAIT_OBJECT_0 + 1:
            // Memory is low, tell the finalizer thread to garbage collect.
            ASSERT(!fLastEventWasLowMemory);
            fLastEventWasLowMemory = true;
            return FALSE;

        case WAIT_TIMEOUT:
            // We were waiting only for finalization events but didn't get one within the timeout period. Go
            // back to waiting for any event.
            ASSERT(fLastEventWasLowMemory);
            fLastEventWasLowMemory = false;
            break;

        default:
            ASSERT(!"Unexpected PalWaitForMultipleObjectsEx() result");
            return FALSE;
        }
    } while (true);
}

// Indicate that the current round of finalizations is complete.
EXTERN_C NATIVEAOT_API void __cdecl RhpSignalFinalizationComplete(uint32_t fcount)
{
    FireEtwGCFinalizersEnd_V1(fcount, GetClrInstanceId());
    g_FinalizerDoneEvent.Set();
}

//
// The following helpers are special in that they interact with internal GC state or directly manipulate
// managed references so they're called with a special co-operative p/invoke.
//

// Fetch next object which needs finalization or return null if we've reached the end of the list.
COOP_PINVOKE_HELPER_NOARGS(OBJECTREF, RhpGetNextFinalizableObject)
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
