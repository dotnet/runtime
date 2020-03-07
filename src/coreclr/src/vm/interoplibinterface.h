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

    static void* QCALLTYPE GetOrCreateComInterfaceForObject(
        _In_ QCall::ObjectHandleOnStack comWrappersImpl,
        _In_ QCall::ObjectHandleOnStack instance,
        _In_ INT32 flags);

    static void QCALLTYPE GetOrCreateObjectForComInstance(
        _In_ QCall::ObjectHandleOnStack comWrappersImpl,
        _In_ void* externalComObject,
        _In_ INT32 flags,
        _In_ QCall::ObjectHandleOnStack wrapperMaybe,
        _Inout_ QCall::ObjectHandleOnStack retValue);

public: // Lifetime management for COM Wrappers
    static void DestroyManagedObjectComWrapper(_In_ void* wrapper);
    static void DestroyExternalComObjectContext(_In_ void* context);
    static void MarkExternalComObjectContextCollected(_In_ void* context);
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
