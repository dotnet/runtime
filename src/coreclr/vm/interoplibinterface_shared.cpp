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

#ifdef FEATURE_OBJCMARSHAL
    PTR_CUTF8 libraryName = md->GetLibNameRaw();
    PTR_CUTF8 entrypointName = md->GetEntrypointName();
    if (libraryName == NULL || entrypointName == NULL)
        return false;

    if (ObjCMarshalNative::IsRuntimeMsgSendFunctionOverridden(libraryName, entrypointName))
        return true;
#endif // FEATURE_OBJCMARSHAL

    return false;
}

ManagedToNativeExceptionCallback Interop::GetPropagatingExceptionCallback(
    _In_ EECodeInfo* codeInfo,
    _In_ OBJECTHANDLE throwable,
    _Outptr_ void** context)
{
    CONTRACT(ManagedToNativeExceptionCallback)
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        PRECONDITION(codeInfo != NULL);
        PRECONDITION(throwable != NULL);
        PRECONDITION(context != NULL);
    }
    CONTRACT_END;

    ManagedToNativeExceptionCallback callback = NULL;
    *context = NULL;

#ifdef FEATURE_OBJCMARSHAL
    EX_TRY
    {
        callback = (ManagedToNativeExceptionCallback)ObjCMarshalNative::GetPropagatingExceptionCallback(
            codeInfo,
            throwable,
            context);
    }
    EX_CATCH
    {
        EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(
            COR_E_EXECUTIONENGINE,
            W("Unhandled managed exception handler threw an exception."));
    }
    EX_END_CATCH_UNREACHABLE;
#endif // FEATURE_OBJCMARSHAL

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
    }
}

void Interop::OnBeforeGCScanRoots()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_OBJCMARSHAL
    ObjCMarshalNative::BeforeRefCountedHandleCallbacks();
#endif // FEATURE_OBJCMARSHAL
}

void Interop::OnAfterGCScanRoots()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_OBJCMARSHAL
    ObjCMarshalNative::AfterRefCountedHandleCallbacks();
#endif // FEATURE_OBJCMARSHAL
}
