// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_JAVAMARSHAL

// Runtime headers
#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

using CrossreferenceHandleCallback = void(STDMETHODCALLTYPE *)(MarkCrossReferencesArgs*);

namespace
{
    CrossreferenceHandleCallback g_MarkCrossReferences = NULL;

    Volatile<bool> g_GCBridgeActive = false;
    CLREvent* g_bridgeFinished = nullptr;

    void ClearPendingBridgeBits(
        _In_reads_(handleCount) uintptr_t* handles,
        size_t handleCount)
    {
        LIMITED_METHOD_CONTRACT;

        for (size_t i = 0; i < handleCount; i++)
        {
            OBJECTHANDLE handle = reinterpret_cast<OBJECTHANDLE>(handles[i]);
            Object* object = OBJECTREFToObject(ObjectFromHandle(handle));
            if (object != nullptr)
            {
                object->GetHeader()->ClrBit(BIT_SBLK_BRIDGE_PENDING);
            }
        }
    }

    void ReleaseGCBridgeArgumentsWorker(
        _In_ MarkCrossReferencesArgs* args)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

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

bool Interop::IsGCBridgeActive()
{
    LIMITED_METHOD_CONTRACT;

    return g_GCBridgeActive;
}

bool Interop::TryGetObjectFromHandleWithoutBridgeWait(
    _In_ OBJECTHANDLE handle,
    _Out_ Object** result)
{
    LIMITED_METHOD_CONTRACT;

    // g_GCBridgeActive is Volatile<bool> and pending bit clear uses InterlockedAnd. These should be set with release
    // stores guaranteeing the order: weak-reference nulling -> pending-bit clearing -> g_GCBridgeActive = false
    Object* object = OBJECTREFToObject(ObjectFromHandle(handle));
    if (g_GCBridgeActive && object != nullptr &&
        (object->GetHeader()->GetBitsAcquire() & BIT_SBLK_BRIDGE_PENDING) != 0)
    {
        return false;
    }

    // If object is nullptr, the handle value is stable, refetching is harmless
    // If bridge is not active, refetching the handle should guarantee we get the right value
    // since `g_GCBridgeActive` is Volatile<bool>
    // The only remaining interesting case is gc bridge being active, with non-null object which
    // had the bridge pending bit not set.
    //
    // If the bridge pending bit was never set because the object was not due for collection, handle value is stable
    // If the bridge pending bit was set but got cleared in the meantime due to this code racing with the bridge
    // finisher, refetching the handle will guarantee that we see the new value of the handle, because the
    // bit is cleared with InterlockedAnd which does a release barrier (guaranteeing that the potential handle nulling
    // was already published).
    Object* confirmedObject = OBJECTREFToObject(ObjectFromHandle(handle));
    if (confirmedObject != object)
    {
        return false;
    }

    *result = confirmedObject;
    return true;
}

void Interop::WaitForGCBridgeFinish()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    while (g_GCBridgeActive)
    {
        GCX_PREEMP();
        g_bridgeFinished->Wait(INFINITE, false);
        // In theory, even though we waited for bridge to finish, because we are in preemptive mode
        // the thread could have been suspended and another GC could have happened, triggering bridge
        // processing again. In this case we would wait again for bridge processing.
    }
}

void Interop::TriggerClientBridgeProcessing(
    _In_ MarkCrossReferencesArgs* args)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(GCHeapUtilities::IsGCInProgress());
    }
    CONTRACTL_END;

    size_t pendingBridgeHandleCount;
    uintptr_t* pendingBridgeHandles = GCHeapUtilities::GetGCHeap()->GetPendingBridgeHandles(&pendingBridgeHandleCount);

    _ASSERTE(!g_GCBridgeActive);

    bool gcBridgeTriggered = JavaNative::TriggerClientBridgeProcessing(args);

    if (!gcBridgeTriggered)
    {
        // Release the memory allocated since the GCBridge
        // wasn't trigger for some reason.
        ClearPendingBridgeBits(pendingBridgeHandles, pendingBridgeHandleCount);
        ReleaseGCBridgeArgumentsWorker(args);
        return;
    }

    // This runs during GC while the world is stopped, no synchronisation required
    if (g_bridgeFinished)
    {
        g_bridgeFinished->Reset();
    }

    // Mark the GCBridge as active.
    g_GCBridgeActive = true;
}

void Interop::FinishCrossReferenceProcessing(
    _In_ MarkCrossReferencesArgs *args,
    _In_ size_t length,
    _In_ void* unreachableObjectHandles)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(g_GCBridgeActive);

    // Mark the GCBridge as inactive.
    // This must be synchronized with the GC so switch to cooperative mode.
    {
        GCX_COOP();

        GCHeapUtilities::GetGCHeap()->NullBridgeObjectsWeakRefs(length, unreachableObjectHandles);
        size_t pendingBridgeHandleCount;
        uintptr_t* pendingBridgeHandles = GCHeapUtilities::GetGCHeap()->GetPendingBridgeHandles(&pendingBridgeHandleCount);
        ClearPendingBridgeBits(pendingBridgeHandles, pendingBridgeHandleCount);

        IGCHandleManager* pHandleManager = GCHandleUtilities::GetGCHandleManager();
        for (size_t i = 0; i < length; i++)
            pHandleManager->DestroyHandleOfUnknownType(((OBJECTHANDLE*)unreachableObjectHandles)[i]);
        g_GCBridgeActive = false;
        g_bridgeFinished->Set();
    }

    ReleaseGCBridgeArgumentsWorker(args);
}

extern "C" BOOL QCALLTYPE JavaMarshal_Initialize(
    _In_ void* markCrossReferences,
    QCallExceptionStatus* qcallError)
{
    QCALL_CONTRACT;
    _ASSERTE(markCrossReferences != NULL);

    BOOL success = FALSE;

    BEGIN_QCALL;

    // Switch to Cooperative mode since we are setting callbacks that
    // will be used during a GC and we want to ensure a GC isn't occurring
    // while they are being set.
    {
        GCX_COOP();
        if (InterlockedCompareExchangeT((void**)&g_MarkCrossReferences, markCrossReferences, NULL) == NULL)
        {
            success = TRUE;
            g_bridgeFinished = new CLREvent();
            g_bridgeFinished->CreateManualEvent(false);
        }
    }

    END_QCALL;

    return success;
}

extern "C" void* QCALLTYPE JavaMarshal_CreateReferenceTrackingHandle(
    _In_ QCall::ObjectHandleOnStack obj,
    _In_ void* context,
    QCallExceptionStatus* qcallError)
{
    QCALL_CONTRACT;

    OBJECTHANDLE instHandle = NULL;

    BEGIN_QCALL;

    GCX_COOP();
    instHandle = GetAppDomain()->CreateCrossReferenceHandle(obj.Get(), context);

    END_QCALL;

    return (void*)instHandle;
}

extern "C" void QCALLTYPE JavaMarshal_FinishCrossReferenceProcessing(
    _In_ MarkCrossReferencesArgs *crossReferences,
    _In_ size_t length,
    _In_ void* unreachableObjectHandles,
    QCallExceptionStatus* qcallError)
{
    QCALL_CONTRACT;
    _ASSERTE(crossReferences->ComponentCount >= 0);

    BEGIN_QCALL;

    Interop::FinishCrossReferenceProcessing(crossReferences, length, unreachableObjectHandles);

    END_QCALL;
}

extern "C" BOOL QCALLTYPE JavaMarshal_GetContext(
    _In_ OBJECTHANDLE handle,
    _Out_ void** context)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;
    _ASSERTE(handle != NULL);
    _ASSERTE(context != NULL);

    IGCHandleManager* mgr = GCHandleUtilities::GetGCHandleManager();
    HandleType type = mgr->HandleFetchType(handle);
    if (type != HNDTYPE_CROSSREFERENCE)
        return FALSE;

    *context = mgr->GetExtraInfoFromHandle(handle);
    return TRUE;
}

bool JavaNative::TriggerClientBridgeProcessing(
    _In_ MarkCrossReferencesArgs *args)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(GCHeapUtilities::IsGCInProgress());
    }
    CONTRACTL_END;

    // Not initialized
    if (g_MarkCrossReferences == NULL)
        return false;

    g_MarkCrossReferences(args);
    return true;
}

#endif // FEATURE_JAVAMARSHAL
