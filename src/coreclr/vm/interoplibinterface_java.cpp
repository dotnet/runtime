// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_JAVAMARSHAL

// Runtime headers
#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

using CrossreferenceHandleCallback = void(STDMETHODCALLTYPE *)(MarkCrossReferences*);

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

extern "C" void QCALLTYPE JavaMarshal_FinishCrossReferenceProcessing(
    _In_ MarkCrossReferences *crossReferences,
    _In_ int length,
    _In_ void* unreachableObjectHandles)
{
    QCALL_CONTRACT;
    _ASSERTE(crossReferences->ComponentsLen >= 0);

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

    MarkCrossReferences arg;
    arg.ComponentsLen = sccsLen;
    arg.Components = sccs;
    arg.CrossReferencesLen = ccrsLen;
    arg.CrossReferences = ccrs;

    g_MarkCrossReferences(&arg);
    return true;
}

#endif // FEATURE_JAVAMARSHAL
