// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_OBJCBRIDGE

// Runtime headers
#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

#include <pinvokeoverride.h>

#define OBJC_MSGSEND "objc_msgSend"

namespace
{
    BOOL g_ReferenceTrackerInitialized;
    ObjCBridgeNative::BeginEndCallback g_BeginEndCallback;
    ObjCBridgeNative::IsReferencedCallback g_IsReferencedCallback;
    ObjCBridgeNative::EnteredFinalizationCallback g_TrackedObjectEnteredFinalizationCallback;

    // Right now the begin/end values are defined to be
    // positive for begin and negative for end. The actual
    // value isn't important. Since CoreCLR is calling this
    // during a gen2 that will be used but it isn't defined
    // at this point. See documentation for further details.
    const int BeginValue = 2;
    const int EndValue = -BeginValue;

    // Defined handle types for the specific object uses.
    const HandleType InstanceHandleType{ HNDTYPE_REFCOUNTED };
}

BOOL QCALLTYPE ObjCBridgeNative::TryInitializeReferenceTracker(
    _In_ BeginEndCallback beginEndCallback,
    _In_ IsReferencedCallback isReferencedCallback,
    _In_ EnteredFinalizationCallback trackedObjectEnteredFinalization)
{
    QCALL_CONTRACT;
    _ASSERTE(beginEndCallback != NULL
            && isReferencedCallback != NULL
            && trackedObjectEnteredFinalization != NULL);

    BOOL success = FALSE;

    BEGIN_QCALL;

    // Switch to Cooperative mode since we are setting callbacks that
    // will be used during a GC and we want to ensure a GC isn't occuring
    // while they are being set.
    {
        GCX_COOP();
        if (FastInterlockCompareExchange((LONG*)&g_ReferenceTrackerInitialized, TRUE, FALSE) == FALSE)
        {
            g_BeginEndCallback = beginEndCallback;
            g_IsReferencedCallback = isReferencedCallback;
            g_TrackedObjectEnteredFinalizationCallback = trackedObjectEnteredFinalization;

            success = TRUE;
        }
    }

    END_QCALL;

    return success;
}

void* QCALLTYPE ObjCBridgeNative::CreateReferenceTrackingHandle(
    _In_ QCall::ObjectHandleOnStack obj,
    _Outptr_ void** scratchMemory)
{
    QCALL_CONTRACT;
    _ASSERTE(scratchMemory != NULL);

    OBJECTHANDLE instHandle;
    void* scratchMemoryLocal;

    BEGIN_QCALL;

    // The reference tracking system must be initialized.
    if (!g_ReferenceTrackerInitialized)
        COMPlusThrow(kInvalidOperationException, W("InvalidOperation_ReferenceTrackerNotInitialized"));

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();

        struct
        {
            OBJECTREF objRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.objRef = obj.Get();

        // The object's type must be marked appropriately and with a finalizer.
        if (!gc.objRef->GetMethodTable()->IsTrackedReferenceWithFinalizer())
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_TrackedNativeReferenceNoFinalizer"));

        // Initialize the syncblock for this instance.
        SyncBlock* syncBlock = gc.objRef->GetSyncBlock();
        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();
        scratchMemoryLocal = interopInfo->AllocReferenceTrackingScratchMemory();
        _ASSERTE(scratchMemoryLocal != NULL);

        instHandle = GetAppDomain()->CreateTypedHandle(gc.objRef, InstanceHandleType);
        if (instHandle == NULL)
            ThrowOutOfMemory();

        GCPROTECT_END();
    }

    END_QCALL;

    *scratchMemory = scratchMemoryLocal;
    return (void*)instHandle;
}

namespace
{
    BOOL s_msgSendOverridden = FALSE;
    void* s_msgSendOverrides[ObjCBridgeNative::MsgSendFunction::Last + 1] = {};

    const char* ObjectiveCLibrary = "/usr/lib/libobjc.dylib";
    const char* MsgSendEntryPoints[ObjCBridgeNative::MsgSendFunction::Last + 1] =
    {
        OBJC_MSGSEND,
        OBJC_MSGSEND "_fpret",
        OBJC_MSGSEND "_stret",
        OBJC_MSGSEND "Super",
        OBJC_MSGSEND "Super_stret",
    };

    const void* STDMETHODCALLTYPE MessageSendPInvokeOverride(_In_z_ const char* libraryName, _In_z_ const char* entrypointName)
    {
        // All overrides are in libobjc
        if (strcmp(libraryName, ObjectiveCLibrary) != 0)
            return nullptr;

        // All overrides start with objc_msgSend
        if (strncmp(entrypointName, OBJC_MSGSEND, _countof(OBJC_MSGSEND) -1) != 0)
            return nullptr;

        for (int i = 0; i < _countof(MsgSendEntryPoints); ++i)
        {
            void* funcMaybe = s_msgSendOverrides[i];
            if (funcMaybe != nullptr
                && strcmp(entrypointName, MsgSendEntryPoints[i]) == 0)
            {
                return funcMaybe;
            }
        }

        return nullptr;
    }
}

BOOL QCALLTYPE ObjCBridgeNative::TrySetGlobalMessageSendCallback(
    _In_ MsgSendFunction msgSendFunction,
    _In_ void* fptr)
{
    QCALL_CONTRACT;

    bool success;

    BEGIN_QCALL;

    _ASSERTE(msgSendFunction >= 0 && msgSendFunction < _countof(s_msgSendOverrides));
    success = FastInterlockCompareExchangePointer(&s_msgSendOverrides[msgSendFunction], fptr, NULL) == NULL;

    // Set P/Invoke override callback if we haven't already
    if (success && FALSE == FastInterlockCompareExchange((LONG*)&s_msgSendOverridden, TRUE, FALSE))
        PInvokeOverride::SetPInvokeOverride(&MessageSendPInvokeOverride, PInvokeOverride::Source::ObjectiveCInterop);

    END_QCALL;

    return success ? TRUE : FALSE;
}

namespace
{
    bool TryGetReferenceTrackingScratchMemory(_In_ OBJECTREF object, _Out_ void** scratch)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(scratch));
        }
        CONTRACTL_END;

        SyncBlock* syncBlock = object->PassiveGetSyncBlock();
        if (syncBlock == NULL)
            return false;

        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfoNoCreate();
        if (interopInfo == NULL)
            return false;

        // If no scratch memory is allocated, then the instance is not
        // being tracked.
        void* scratchLocal = interopInfo->GetReferenceTrackingScratchMemory();
        if (scratchLocal == NULL)
            return false;

        *scratch = scratchLocal;
        return true;
    }
}

bool ObjCBridgeNative::IsTrackedReference(_In_ OBJECTREF object, _Out_ bool* isReferenced)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(isReferenced));
    }
    CONTRACTL_END;

    *isReferenced = false;

    void* scratchMemory;
    if (!TryGetReferenceTrackingScratchMemory(object, &scratchMemory))
        return false;

    _ASSERTE(g_IsReferencedCallback != NULL);
    int result = g_IsReferencedCallback(scratchMemory);

    *isReferenced = (result != 0);
    return true;
}

void ObjCBridgeNative::OnBackgroundGCStarted()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (g_BeginEndCallback != NULL)
        g_BeginEndCallback(BeginValue);
}

void ObjCBridgeNative::OnBackgroundGCFinished()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (g_BeginEndCallback != NULL)
        g_BeginEndCallback(EndValue);
}

void ObjCBridgeNative::OnEnteredFinalizerQueue(_In_ OBJECTREF object)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    void* scratchMemory;
    if (!TryGetReferenceTrackingScratchMemory(object, &scratchMemory))
        return;

    _ASSERTE(g_TrackedObjectEnteredFinalizationCallback != NULL);
    g_TrackedObjectEnteredFinalizationCallback(scratchMemory);
}

#endif // FEATURE_OBJCBRIDGE
