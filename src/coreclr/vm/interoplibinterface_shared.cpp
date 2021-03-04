// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runtime headers
#include "common.h"

#include "interoplibinterface.h"

using ManagedToNativeExceptionCallback = Interop::ManagedToNativeExceptionCallback;

bool Interop::ShouldCheckForPendingException(_In_ NDirectMethodDesc* md)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(md != NULL);
    }
    CONTRACTL_END;

#ifdef FEATURE_OBJCBRIDGE
    PTR_CUTF8 libraryName = md->GetLibNameRaw();
    PTR_CUTF8 entrypointName = md->GetEntrypointName();
    if (libraryName == NULL || entrypointName == NULL)
        return false;

    if (ObjCBridgeNative::IsRuntimeMsgSendFunctionOverridden(libraryName, entrypointName))
        return true;
#endif // FEATURE_OBJCBRIDGE

    return false;
}

namespace
{
    void* CallInvokeUnhandledExceptionPropagation(
        _In_ OBJECTREF* exceptionPROTECTED,
        _Outptr_ void** callbackContext)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(exceptionPROTECTED != NULL);
            PRECONDITION(callbackContext != NULL);
        }
        CONTRACTL_END;

        void* callback = NULL;
        *callbackContext = NULL;

#ifdef FEATURE_OBJCBRIDGE
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__OBJCBRIDGE__INVOKEUNHANDLEDEXCEPTIONPROPAGATION);
        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*exceptionPROTECTED);
        args[ARGNUM_1] = PTR_TO_ARGHOLDER(callbackContext);
        CALL_MANAGED_METHOD(callback, void*, args);
#endif // FEATURE_OBJCBRIDGE

        return callback;
    }
}

ManagedToNativeExceptionCallback Interop::GetPropagatingExceptionCallback(
    _In_ EECodeInfo* codeInfo,
    _In_ OBJECTHANDLE throwable,
    _Outptr_ void** context)
{
    CONTRACT(ManagedToNativeExceptionCallback)
    {
        THROWS;
        MODE_PREEMPTIVE;
        PRECONDITION(codeInfo != NULL);
        PRECONDITION(context != NULL);
    }
    CONTRACT_END;

    ManagedToNativeExceptionCallback callback;
    void* callbackContext;

    {
        GCX_COOP();
        struct
        {
            OBJECTREF throwableRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.throwableRef = ObjectFromHandle(throwable);

        callback = (ManagedToNativeExceptionCallback)CallInvokeUnhandledExceptionPropagation(
            &gc.throwableRef,
            &callbackContext);

        GCPROTECT_END();
    }

    *context = callbackContext;
    RETURN callback;
}

void Interop::OnGCStarted(_In_ int nCondemnedGeneration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //
    // Note that we could get nested GCStart/GCEnd calls, such as:
    // GCStart for Gen 2 background GC
    //    GCStart for Gen 0/1 foreground GC
    //    GCEnd   for Gen 0/1 foreground GC
    //    ...
    // GCEnd for Gen 2 background GC
    //
    // The nCondemnedGeneration >= 2 check takes care of this nesting problem
    //
    // See Interop::OnGCFinished()
    if (nCondemnedGeneration >= 2)
    {
#ifdef FEATURE_COMWRAPPERS
        ComWrappersNative::OnFullGCStarted();
#endif // FEATURE_COMWRAPPERS
#ifdef FEATURE_OBJCBRIDGE
        ObjCBridgeNative::OnFullGCStarted();
#endif // FEATURE_OBJCBRIDGE
    }
}

void Interop::OnGCFinished(_In_ int nCondemnedGeneration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // See Interop::OnGCStarted()
    if (nCondemnedGeneration >= 2)
    {
#ifdef FEATURE_COMWRAPPERS
        ComWrappersNative::OnFullGCFinished();
#endif // FEATURE_COMWRAPPERS
#ifdef FEATURE_OBJCBRIDGE
        ObjCBridgeNative::OnFullGCFinished();
#endif // FEATURE_OBJCBRIDGE
    }
}
