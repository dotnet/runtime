// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Interface between the VM and Interop library.
//

#ifdef FEATURE_COMWRAPPERS

// Native calls for the managed ComWrappers API
class ComWrappersNative
{
public: // Native QCalls for the abstract ComWrappers managed type.
    static void QCALLTYPE GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease);

    static BOOL QCALLTYPE TryGetOrCreateComInterfaceForObject(
        _In_ QCall::ObjectHandleOnStack comWrappersImpl,
        _In_ QCall::ObjectHandleOnStack instance,
        _In_ INT32 flags,
        _Outptr_ void** wrapperRaw);

    static BOOL QCALLTYPE TryGetOrCreateObjectForComInstance(
        _In_ QCall::ObjectHandleOnStack comWrappersImpl,
        _In_ void* externalComObject,
        _In_ INT32 flags,
        _In_ QCall::ObjectHandleOnStack wrapperMaybe,
        _Inout_ QCall::ObjectHandleOnStack retValue);

public: // Lifetime management for COM Wrappers
    static void DestroyManagedObjectComWrapper(_In_ void* wrapper);
    static void DestroyExternalComObjectContext(_In_ void* context);
    static void MarkExternalComObjectContextCollected(_In_ void* context);

public: // COM activation
    static void MarkWrapperAsComActivated(_In_ IUnknown* wrapperMaybe);
};

class GlobalComWrappers
{
public:
    // Native QCall for the ComWrappers managed type to indicate a global instance is registered
    // This should be set if the private static member representing the global instance on ComWrappers is non-null.
    static void QCALLTYPE SetGlobalInstanceRegistered();

public: // Functions operating on a registered global instance
    static bool TryGetOrCreateComInterfaceForObject(
        _In_ OBJECTREF instance,
        _Outptr_ void** wrapperRaw);

    static bool TryGetOrCreateObjectForComInstance(
        _In_ IUnknown* externalComObject,
        _In_ INT32 objFromComIPFlags,
        _Out_ OBJECTREF* objRef);
};

#endif // FEATURE_COMWRAPPERS

class Interop
{
public:
    // Notify when GC started
    static void OnGCStarted(_In_ int nCondemnedGeneration);

    // Notify when GC finished
    static void OnGCFinished(_In_ int nCondemnedGeneration);
};
