// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_JAVAMARSHAL

// Runtime headers
#include "common.h"
#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "event.h"
#include "thread.inl"

#include "interoplibinterface.h"

using CrossreferenceHandleCallback = void(__stdcall *)(MarkCrossReferencesArgs*);

namespace
{
    volatile CrossreferenceHandleCallback g_MarkCrossReferences = NULL;

    Volatile<bool> g_GCBridgeActive = false;
    CLREventStatic g_bridgeFinished;

    void ReleaseGCBridgeArgumentsWorker(
        MarkCrossReferencesArgs* args)
    {
        _ASSERTE(args != NULL);

        // Memory was allocated for the collections by the GC.
        // See callers of GCToEEInterface::TriggerGCBridge().

        // Free memory in each of the SCCs
        for (size_t i = 0; i < args->ComponentCount; i++)
        {
            delete[] args->Components[i].Contexts;
        }
        delete[] args->Components;
        delete[] args->CrossReferences;
        delete args;
    }
}

void JavaMarshalNative::TriggerClientBridgeProcessing(
    _In_ MarkCrossReferencesArgs* args)
{
    _ASSERTE(GCHeapUtilities::IsGCInProgress());

    if (g_GCBridgeActive)
    {
        // Release the memory allocated since the GCBridge
        // is already running and we're not passing them to it.
        ReleaseGCBridgeArgumentsWorker(args);
        return;
    }

    // Not initialized
    if (g_MarkCrossReferences == NULL)
    {
        // Release the memory allocated since we
        // don't have a GC bridge callback.
        ReleaseGCBridgeArgumentsWorker(args);
        return;
    }

    g_MarkCrossReferences(args);

    // This runs during GC while the world is stopped, no synchronisation required
    g_bridgeFinished.Reset();

    // Mark the GCBridge as active.
    g_GCBridgeActive = true;
}

extern "C" BOOL QCALLTYPE JavaMarshal_Initialize(
    void* markCrossReferences)
{
    _ASSERTE(markCrossReferences != NULL);

    BOOL success = FALSE;

    // Switch to Cooperative mode since we are setting callbacks that
    // will be used during a GC and we want to ensure a GC isn't occurring
    // while they are being set.
    Thread* pThisThread = ThreadStore::GetCurrentThreadIfAvailable();
    pThisThread->DeferTransitionFrame();
    pThisThread->DisablePreemptiveMode();

    if (PalInterlockedCompareExchangePointer((void* volatile*)&g_MarkCrossReferences, (void*)markCrossReferences, NULL) == NULL)
    {
        success = g_bridgeFinished.CreateManualEventNoThrow(false);
    }

    pThisThread->EnablePreemptiveMode();

    return success;
}

extern "C" void QCALLTYPE JavaMarshal_FinishCrossReferenceProcessing(
    MarkCrossReferencesArgs *crossReferences,
    size_t length,
    void* unreachableObjectHandles)
{
    _ASSERTE(crossReferences->ComponentCount >= 0);

    _ASSERTE(g_GCBridgeActive);

    // Mark the GCBridge as inactive.
    // This must be synchronized with the GC so switch to cooperative mode.
    {
        Thread* pThisThread = ThreadStore::GetCurrentThreadIfAvailable();
        pThisThread->DeferTransitionFrame();
        pThisThread->DisablePreemptiveMode();

        GCHeapUtilities::GetGCHeap()->NullBridgeObjectsWeakRefs(length, unreachableObjectHandles);

        IGCHandleManager* pHandleManager = GCHandleUtilities::GetGCHandleManager();
        OBJECTHANDLE* handles = (OBJECTHANDLE*)unreachableObjectHandles;
        for (size_t i = 0; i < length; i++)
            pHandleManager->DestroyHandleOfUnknownType(handles[i]);

        g_GCBridgeActive = false;
        g_bridgeFinished.Set();

        pThisThread->EnablePreemptiveMode();
    }

    ReleaseGCBridgeArgumentsWorker(crossReferences);
}

FCIMPL2(FC_BOOL_RET, GCHandle_InternalTryGetBridgeWait, OBJECTHANDLE handle, OBJECTREF* pObjResult)
{
    if (g_GCBridgeActive)
    {
        FC_RETURN_BOOL(false);
    }

    *pObjResult = ObjectFromHandle(handle);
    FC_RETURN_BOOL(true);
}
FCIMPLEND

extern "C" void QCALLTYPE GCHandle_InternalGetBridgeWait(OBJECTHANDLE handle, OBJECTREF* pObj)
{
    _ASSERTE(pObj != NULL);

    // Transition to cooperative mode to ensure that the GC is not in progress
    Thread* pThisThread = ThreadStore::GetCurrentThreadIfAvailable();
    pThisThread->DeferTransitionFrame();
    pThisThread->DisablePreemptiveMode();

    while (g_GCBridgeActive)
    {
        // This wait will transition to pre-emptive mode to wait for the bridge to finish.
        g_bridgeFinished.Wait(INFINITE, false, false);
    }

    // If we reach here, then the bridge has finished processing and we can be sure that
    // it isn't currently active.

    // No GC can happen between the wait and obtaining of the reference, so the
    // bridge processing status can't change, guaranteeing the nulling of weak refs
    // took place in the bridge processing finish stage.
    *pObj = ObjectFromHandle(handle);

    // Re-enable preemptive mode before we exit the QCall to ensure we're in the right GC state.
    pThisThread->EnablePreemptiveMode();
}

#endif // FEATURE_JAVAMARSHAL
