// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_JAVAMARSHAL

// Runtime headers
#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

using CrossreferenceHandleCallback = void(STDMETHODCALLTYPE *)(size_t, StronglyConnectedComponent*, size_t, ComponentCrossReference*);

namespace
{
    BOOL g_Initialized;
    CrossreferenceHandleCallback g_MarkCrossReferences;
}

extern "C" BOOL QCALLTYPE JavaMarshal_Initialize(
    _In_ void* markCrossReferences)
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
        if (InterlockedCompareExchange((LONG*)&g_Initialized, TRUE, FALSE) == FALSE)
        {
            g_MarkCrossReferences = (CrossreferenceHandleCallback)markCrossReferences;
            success = TRUE;
        }
    }

    END_QCALL;

    return success;
}

extern "C" void* QCALLTYPE JavaMarshal_CreateReferenceTrackingHandle(
    _In_ QCall::ObjectHandleOnStack obj,
    _In_ void* context)
{
    QCALL_CONTRACT;

    OBJECTHANDLE instHandle = NULL;

    BEGIN_QCALL;

    GCX_COOP();
    instHandle = GetAppDomain()->CreateCrossReferenceHandle(obj.Get(), context);

    END_QCALL;

    return (void*)instHandle;
}

extern "C" void QCALLTYPE JavaMarshal_ReleaseMarkCrossReferenceResources(
    _In_ int32_t sccsLen,
    _In_ StronglyConnectedComponent* sccs,
    _In_ ComponentCrossReference* ccrs)
{
    QCALL_CONTRACT;
    _ASSERTE(sccsLen >= 0);

    BEGIN_QCALL;

    Interop::ReleaseGCBridgeArguments(sccsLen, sccs, ccrs);

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

bool JavaNative::TriggerGCBridge(
    _In_ size_t sccsLen,
    _In_ StronglyConnectedComponent* sccs,
    _In_ size_t ccrsLen,
    _In_ ComponentCrossReference* ccrs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Not initialized
    if (g_MarkCrossReferences == NULL)
        return false;

    g_MarkCrossReferences(sccsLen, sccs, ccrsLen, ccrs);
    return true;
}

#endif // FEATURE_JAVAMARSHAL
