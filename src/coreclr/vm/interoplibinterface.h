// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Interface between the VM and Interop library.
//

#ifndef _INTEROPLIBINTERFACE_H_
#define _INTEROPLIBINTERFACE_H_

#ifdef FEATURE_COMWRAPPERS

namespace InteropLibInterface
{
    // Base definition of the external object context.
    struct ExternalObjectContextBase
    {
        PTR_VOID Identity;
        DWORD SyncBlockIndex;
    };
}

// Native calls for the managed ComWrappers API
class ComWrappersNative
{
public:
    static const INT64 InvalidWrapperId = 0;

public: // Native QCalls for the abstract ComWrappers managed type.
    static void QCALLTYPE GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease);

    static BOOL QCALLTYPE TryGetOrCreateComInterfaceForObject(
        _In_ QCall::ObjectHandleOnStack comWrappersImpl,
        _In_ INT64 wrapperId,
        _In_ QCall::ObjectHandleOnStack instance,
        _In_ INT32 flags,
        _Outptr_ void** wrapperRaw);

    static BOOL QCALLTYPE TryGetOrCreateObjectForComInstance(
        _In_ QCall::ObjectHandleOnStack comWrappersImpl,
        _In_ INT64 wrapperId,
        _In_ void* externalComObject,
        _In_opt_ void* innerMaybe,
        _In_ INT32 flags,
        _In_ QCall::ObjectHandleOnStack wrapperMaybe,
        _Inout_ QCall::ObjectHandleOnStack retValue);

public: // Lifetime management for COM Wrappers
    static void DestroyManagedObjectComWrapper(_In_ void* wrapper);
    static void DestroyExternalComObjectContext(_In_ void* context);
    static void MarkExternalComObjectContextCollected(_In_ void* context);

public: // COM activation
    static void MarkWrapperAsComActivated(_In_ IUnknown* wrapperMaybe);

public: // Unwrapping support
    static IUnknown* GetIdentityForObject(_In_ OBJECTREF* objectPROTECTED, _In_ REFIID riid, _Out_ INT64* wrapperId);
    static bool HasManagedObjectComWrapper(_In_ OBJECTREF object, _Out_ bool* isActive);

public: // GC interaction
    static void OnFullGCStarted();
    static void OnFullGCFinished();
};

class GlobalComWrappersForMarshalling
{
public:
    // Native QCall for the ComWrappers managed type to indicate a global instance
    // is registered for marshalling. This should be set if the private static member
    // representing the global instance for marshalling on ComWrappers is non-null.
    static void QCALLTYPE SetGlobalInstanceRegisteredForMarshalling(_In_ INT64 id);

public: // Functions operating on a registered global instance for marshalling
    static bool IsRegisteredInstance(_In_ INT64 id);

    static bool TryGetOrCreateComInterfaceForObject(
        _In_ OBJECTREF instance,
        _Outptr_ void** wrapperRaw);

    static bool TryGetOrCreateObjectForComInstance(
        _In_ IUnknown* externalComObject,
        _In_ INT32 objFromComIPFlags,
        _Out_ OBJECTREF* objRef);
};

class GlobalComWrappersForTrackerSupport
{
public:
    // Native QCall for the ComWrappers managed type to indicate a global instance
    // is registered for tracker support. This should be set if the private static member
    // representing the global instance for tracker support on ComWrappers is non-null.
    static void QCALLTYPE SetGlobalInstanceRegisteredForTrackerSupport(_In_ INT64 id);

public: // Functions operating on a registered global instance for tracker support
    static bool IsRegisteredInstance(_In_ INT64 id);

    static bool TryGetOrCreateComInterfaceForObject(
        _In_ OBJECTREF instance,
        _Outptr_ void** wrapperRaw);

    static bool TryGetOrCreateObjectForComInstance(
        _In_ IUnknown* externalComObject,
        _Out_ OBJECTREF* objRef);
};

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

public: // static
    static BOOL QCALLTYPE TryInitializeReferenceTracker(
        _In_ BeginEndCallback beginEndCallback,
        _In_ IsReferencedCallback isReferencedCallback,
        _In_ EnteredFinalizationCallback trackedObjectEnteredFinalization);

    static void* QCALLTYPE CreateReferenceTrackingHandle(
        _In_ QCall::ObjectHandleOnStack obj,
        _Out_ int* memInSizeT,
        _Outptr_ void** mem);

    static BOOL QCALLTYPE TrySetGlobalMessageSendCallback(
        _In_ MessageSendFunction msgSendFunction,
        _In_ void* fptr);

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

#endif // FEATURE_OBJCMARSHAL

class Interop
{
public:
    // Check if pending exceptions are possible for the following native export.
    static bool ShouldCheckForPendingException(_In_ NDirectMethodDesc* md);

    // A no return callback that is designed to help propagate a managed
    // exception going from Managed to Native.
    using ManagedToNativeExceptionCallback = /* no return */ void(*)(_In_ void* context);

    static ManagedToNativeExceptionCallback GetPropagatingExceptionCallback(
        _In_ EECodeInfo* codeInfo,
        _In_ OBJECTHANDLE throwable,
        _Outptr_ void** context);

    // Notify started/finished when GC is running.
    static void OnGCStarted(_In_ int nCondemnedGeneration);
    static void OnGCFinished(_In_ int nCondemnedGeneration);

    // Notify before/after when GC is scanning roots.
    // Present assumption is that calls will never be nested.
    static void OnBeforeGCScanRoots();
    static void OnAfterGCScanRoots();
};

#endif // _INTEROPLIBINTERFACE_H_
