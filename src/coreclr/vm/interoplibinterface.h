// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Interface between the VM and Interop library.
//

#ifndef _INTEROPLIBINTERFACE_H_
#define _INTEROPLIBINTERFACE_H_

#ifdef FEATURE_COMWRAPPERS
#include "interoplibinterface_comwrappers.h"
#endif // FEATURE_COMWRAPPERS

#ifdef FEATURE_OBJCMARSHAL

class ObjCMarshalNative
{
public:
    using BeginEndCallback = void(STDMETHODCALLTYPE *)(void);
    using IsReferencedCallback = int(STDMETHODCALLTYPE *)(_In_ void*);
    using EnteredFinalizationCallback = void(STDMETHODCALLTYPE *)(_In_ void*);

    // See MessageSendFunction in ObjectiveCMarshal class
    enum MessageSendFunction
    {
        MessageSendFunction_MsgSend = 0,
        MessageSendFunction_MsgSendFpret = 1,
        MessageSendFunction_MsgSendStret = 2,
        MessageSendFunction_MsgSendSuper = 3,
        MessageSendFunction_MsgSendSuperStret = 4,
        Last = MessageSendFunction_MsgSendSuperStret,
    };

public: // Instance inspection
    static bool IsTrackedReference(_In_ OBJECTREF object, _Out_ bool* isReferenced);

public: // Identification
    static bool IsRuntimeMessageSendFunction(
        _In_z_ const char* libraryName,
        _In_z_ const char* entrypointName);

public: // Exceptions
    static void* GetPropagatingExceptionCallback(
        _In_ EECodeInfo* codeInfo,
        _In_ OBJECTHANDLE throwable,
        _Outptr_ void** context);

public: // GC interaction
    static void BeforeRefCountedHandleCallbacks();
    static void AfterRefCountedHandleCallbacks();
    static void OnEnteredFinalizerQueue(_In_ OBJECTREF object);
};


extern "C" BOOL QCALLTYPE ObjCMarshal_TryInitializeReferenceTracker(
    _In_ ObjCMarshalNative::BeginEndCallback beginEndCallback,
    _In_ ObjCMarshalNative::IsReferencedCallback isReferencedCallback,
    _In_ ObjCMarshalNative::EnteredFinalizationCallback trackedObjectEnteredFinalization);

extern "C" void* QCALLTYPE ObjCMarshal_CreateReferenceTrackingHandle(
    _In_ QCall::ObjectHandleOnStack obj,
    _Out_ int* memInSizeT,
    _Outptr_ void** mem);

extern "C" BOOL QCALLTYPE ObjCMarshal_TrySetGlobalMessageSendCallback(
    _In_ ObjCMarshalNative::MessageSendFunction msgSendFunction,
    _In_ void* fptr);
#endif // FEATURE_OBJCMARSHAL

#ifdef FEATURE_JAVAMARSHAL
class JavaNative
{
public: // GC interaction
    static bool TriggerClientBridgeProcessing(
        _In_ MarkCrossReferencesArgs* args);
};

extern "C" BOOL QCALLTYPE JavaMarshal_Initialize(
    _In_ void* markCrossReferences);

extern "C" void* QCALLTYPE JavaMarshal_CreateReferenceTrackingHandle(
    _In_ QCall::ObjectHandleOnStack obj,
    _In_ void* context);

extern "C" void QCALLTYPE JavaMarshal_FinishCrossReferenceProcessing(
    _In_ MarkCrossReferencesArgs *crossReferences,
    _In_ size_t length,
    _In_ void* unreachableObjectHandles);

extern "C" BOOL QCALLTYPE JavaMarshal_GetContext(
    _In_ OBJECTHANDLE handle,
    _Out_ void** context);
#endif // FEATURE_JAVAMARSHAL

class Interop
{
public:
    // Check if pending exceptions are possible for the following native export.
    static bool ShouldCheckForPendingException(_In_ PInvokeMethodDesc* md);

    // A no return callback that is designed to help propagate a managed
    // exception going from Managed to Native.
    using ManagedToNativeExceptionCallback = /* no return */ void(*)(_In_ void* context);

    static ManagedToNativeExceptionCallback GetPropagatingExceptionCallback(
        _In_ EECodeInfo* codeInfo,
        _In_ OBJECTHANDLE throwable,
        _Outptr_ void** context);

    // Notify started/finished when GC is running.
    // These methods are called in a blocking fashion when a
    // GC of generation is started. These calls may overlap
    // so care must be taken when taking locks.
    static void OnGCStarted(_In_ int nCondemnedGeneration);
    static void OnGCFinished(_In_ int nCondemnedGeneration);

    // Notify before/after when GC is scanning roots.
    // Present assumption is that calls will never be nested.
    // The input indicates if the call is from a concurrent GC
    // thread or not. These will be nested within OnGCStarted
    // and OnGCFinished.
    static void OnBeforeGCScanRoots(_In_ bool isConcurrent);
    static void OnAfterGCScanRoots(_In_ bool isConcurrent);

#ifdef FEATURE_JAVAMARSHAL

    static bool IsGCBridgeActive();

    static void WaitForGCBridgeFinish();

    static void TriggerClientBridgeProcessing(
        _In_ MarkCrossReferencesArgs* args);

    static void FinishCrossReferenceProcessing(
        _In_ MarkCrossReferencesArgs *crossReferences,
        _In_ size_t length,
        _In_ void* unreachableObjectHandles);

#endif // FEATURE_JAVAMARSHAL
};

#endif // _INTEROPLIBINTERFACE_H_
