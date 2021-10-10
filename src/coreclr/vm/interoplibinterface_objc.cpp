// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_OBJCMARSHAL

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
    ObjCMarshalNative::BeginEndCallback g_BeginEndCallback;
    ObjCMarshalNative::IsReferencedCallback g_IsReferencedCallback;
    ObjCMarshalNative::EnteredFinalizationCallback g_TrackedObjectEnteredFinalizationCallback;
}

BOOL QCALLTYPE ObjCMarshalNative::TryInitializeReferenceTracker(
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

void* QCALLTYPE ObjCMarshalNative::CreateReferenceTrackingHandle(
        _In_ QCall::ObjectHandleOnStack obj,
        _Out_ int* memInSizeT,
        _Outptr_ void** mem)
{
    QCALL_CONTRACT;
    _ASSERTE(memInSizeT != NULL);
    _ASSERTE(mem != NULL);

    OBJECTHANDLE instHandle;
    size_t memInSizeTLocal;
    void* taggedMemoryLocal;

    BEGIN_QCALL;

    // The reference tracking system must be initialized.
    if (!g_ReferenceTrackerInitialized)
        COMPlusThrow(kInvalidOperationException, W("InvalidOperation_ObjectiveCMarshalNotInitialized"));

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
            COMPlusThrow(kInvalidOperationException, W("InvalidOperation_ObjectiveCTypeNoFinalizer"));

        // Initialize the syncblock for this instance.
        SyncBlock* syncBlock = gc.objRef->GetSyncBlock();
        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();
        taggedMemoryLocal = interopInfo->AllocTaggedMemory(&memInSizeTLocal);
        _ASSERTE(taggedMemoryLocal != NULL);

        instHandle = GetAppDomain()->CreateTypedHandle(gc.objRef, HNDTYPE_REFCOUNTED);

        GCPROTECT_END();
    }

    END_QCALL;

    *memInSizeT = (int)memInSizeTLocal;
    *mem = taggedMemoryLocal;
    return (void*)instHandle;
}

namespace
{
    BOOL s_msgSendOverridden = FALSE;
    void* s_msgSendOverrides[ObjCMarshalNative::MessageSendFunction::Last + 1] = {};

    const char* ObjectiveCLibrary = "/usr/lib/libobjc.dylib";
    const char* MsgSendEntryPoints[ObjCMarshalNative::MessageSendFunction::Last + 1] =
    {
        OBJC_MSGSEND,
        OBJC_MSGSEND "_fpret",
        OBJC_MSGSEND "_stret",
        OBJC_MSGSEND "Super",
        OBJC_MSGSEND "Super_stret",
    };

    bool IsObjectiveCMessageSendFunction(_In_z_ const char* libraryName, _In_z_ const char* entrypointName)
    {
        // Is the function in libobjc and named appropriately.
        return ((strcmp(libraryName, ObjectiveCLibrary) == 0)
                && (strncmp(entrypointName, OBJC_MSGSEND, _countof(OBJC_MSGSEND) -1) == 0));
    }

    const void* STDMETHODCALLTYPE MessageSendPInvokeOverride(_In_z_ const char* libraryName, _In_z_ const char* entrypointName)
    {
        if (!IsObjectiveCMessageSendFunction(libraryName, entrypointName))
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

BOOL QCALLTYPE ObjCMarshalNative::TrySetGlobalMessageSendCallback(
    _In_ MessageSendFunction msgSendFunction,
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
    bool TryGetTaggedMemory(_In_ OBJECTREF object, _Out_ void** tagged)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(tagged));
        }
        CONTRACTL_END;

        SyncBlock* syncBlock = object->PassiveGetSyncBlock();
        if (syncBlock == NULL)
            return false;

        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfoNoCreate();
        if (interopInfo == NULL)
            return false;

        // If no tagged memory is allocated, then the instance is not
        // being tracked.
        void* taggedLocal = interopInfo->GetTaggedMemory();
        if (taggedLocal == NULL)
            return false;

        *tagged = taggedLocal;
        return true;
    }
}

bool ObjCMarshalNative::IsTrackedReference(_In_ OBJECTREF object, _Out_ bool* isReferenced)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(isReferenced));
    }
    CONTRACTL_END;

    *isReferenced = false;

    void* taggedMemory;
    if (!TryGetTaggedMemory(object, &taggedMemory))
        return false;

    _ASSERTE(g_IsReferencedCallback != NULL);
    int result = g_IsReferencedCallback(taggedMemory);

    *isReferenced = (result != 0);
    return true;
}

bool ObjCMarshalNative::IsRuntimeMessageSendFunction(
    _In_z_ const char* libraryName,
    _In_z_ const char* entrypointName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(libraryName != NULL);
        PRECONDITION(entrypointName != NULL);
    }
    CONTRACTL_END;

    return IsObjectiveCMessageSendFunction(libraryName, entrypointName);
}

namespace
{
    bool CallAvailableUnhandledExceptionPropagation()
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
        }
        CONTRACTL_END;

        MethodDescCallSite dispatch(METHOD__OBJCMARSHAL__AVAILABLEUNHANDLEDEXCEPTIONPROPAGATION);
        return dispatch.Call_RetBool(NULL);
    }

    void* CallInvokeUnhandledExceptionPropagation(
        _In_ OBJECTREF* exceptionPROTECTED,
        _In_ REFLECTMETHODREF* methodRefPROTECTED,
        _Outptr_ void** callbackContext)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(exceptionPROTECTED != NULL);
            PRECONDITION(methodRefPROTECTED != NULL);
            PRECONDITION(callbackContext != NULL);
        }
        CONTRACTL_END;

        void* callback = NULL;
        *callbackContext = NULL;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__OBJCMARSHAL__INVOKEUNHANDLEDEXCEPTIONPROPAGATION);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*exceptionPROTECTED);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(*methodRefPROTECTED);
        args[ARGNUM_2] = PTR_TO_ARGHOLDER(callbackContext);
        CALL_MANAGED_METHOD(callback, void*, args);

        return callback;
    }
}

void* ObjCMarshalNative::GetPropagatingExceptionCallback(
    _In_ EECodeInfo* codeInfo,
    _In_ OBJECTHANDLE throwable,
    _Outptr_ void** context)
{
    CONTRACT(void*)
    {
        THROWS;
        MODE_PREEMPTIVE;
        PRECONDITION(codeInfo != NULL);
        PRECONDITION(throwable != NULL);
        PRECONDITION(context != NULL);
    }
    CONTRACT_END;

    void* callback = NULL;
    void* callbackContext = NULL;

    MethodDesc* method = codeInfo->GetMethodDesc();

    // If this is a dynamic method, let's see if we can
    // resolve it to its target.
    if (method->IsDynamicMethod())
    {
        DynamicMethodDesc* dynamicMethod = method->AsDynamicMethodDesc();
        ILStubResolver* resolver = dynamicMethod->GetILStubResolver();
        MethodDesc* methodMaybe = resolver->GetStubTargetMethodDesc();
        if (methodMaybe != NULL)
            method = methodMaybe;
    }

    {
        GCX_COOP();
        struct
        {
            OBJECTREF throwableRef;
            REFLECTMETHODREF methodRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        // Creating the StubMethodInfo isn't cheap, so check
        // if there are any handlers prior to dispatching.
        if (CallAvailableUnhandledExceptionPropagation())
        {
            gc.throwableRef = ObjectFromHandle(throwable);
            gc.methodRef = method->GetStubMethodInfo();

            callback = CallInvokeUnhandledExceptionPropagation(
                &gc.throwableRef,
                &gc.methodRef,
                &callbackContext);
        }

        GCPROTECT_END();
    }

    *context = callbackContext;
    RETURN callback;
}

void ObjCMarshalNative::BeforeRefCountedHandleCallbacks()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (g_BeginEndCallback != NULL)
        g_BeginEndCallback();
}

void ObjCMarshalNative::AfterRefCountedHandleCallbacks()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (g_BeginEndCallback != NULL)
        g_BeginEndCallback();
}

void ObjCMarshalNative::OnEnteredFinalizerQueue(_In_ OBJECTREF object)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    void* taggedMemory;
    if (!TryGetTaggedMemory(object, &taggedMemory))
        return;

    _ASSERTE(g_TrackedObjectEnteredFinalizationCallback != NULL);
    g_TrackedObjectEnteredFinalizationCallback(taggedMemory);
}

#endif // FEATURE_OBJCMARSHAL
