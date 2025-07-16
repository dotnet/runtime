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

    bool gcBridgeTriggered = false;
    // Not initialized
    if (g_MarkCrossReferences != NULL)
    {
        g_MarkCrossReferences(args);
        gcBridgeTriggered = true;
    }

    if (!gcBridgeTriggered)
    {
        // Release the memory allocated since the GCBridge
        // wasn't trigger for some reason.
        ReleaseGCBridgeArgumentsWorker(args);
        return;
    }

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
    {
        bool wasCooperative = false;
        Thread* pThisThread = ThreadStore::GetCurrentThreadIfAvailable();
        pThisThread->DeferTransitionFrame();
        pThisThread->DisablePreemptiveMode();

        if (PalInterlockedCompareExchangePointer((void* volatile*)&g_MarkCrossReferences, (void*)markCrossReferences, NULL) == NULL)
        {
            success = g_bridgeFinished.CreateManualEventNoThrow(false);
        }

        pThisThread->EnablePreemptiveMode();
    }

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
        bool wasCooperative = false;
        Thread* pThisThread = ThreadStore::GetCurrentThreadIfAvailable();
        pThisThread->DeferTransitionFrame();
        pThisThread->DisablePreemptiveMode();

        GCHeapUtilities::GetGCHeap()->NullBridgeObjectsWeakRefs(length, unreachableObjectHandles);

        IGCHandleManager* pHandleManager = GCHandleUtilities::GetGCHandleManager();
        for (size_t i = 0; i < length; i++)
            pHandleManager->DestroyHandleOfUnknownType(((OBJECTHANDLE*)unreachableObjectHandles)[i]);

        g_GCBridgeActive = false;
        g_bridgeFinished.Set();

        pThisThread->EnablePreemptiveMode();
    }

    ReleaseGCBridgeArgumentsWorker(crossReferences);
}

FCIMPL0(FC_BOOL_RET, RhIsGCBridgeActive)
{
    FC_RETURN_BOOL(g_GCBridgeActive);
}
FCIMPLEND

extern "C" void QCALLTYPE JavaMarshal_WaitForGCBridgeFinish()
{
    // We will transition to pre-emptive mode to wait for the bridge to finish.
    Thread* pThisThread = ThreadStore::GetCurrentThreadIfAvailable();
    while (g_GCBridgeActive)
    {
        g_bridgeFinished.Wait(INFINITE, false, false);
    }
}

#endif // FEATURE_JAVAMARSHAL
