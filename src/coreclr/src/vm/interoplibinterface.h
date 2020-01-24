// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Interface between the VM and Interop library.
//

#ifdef FEATURE_COMINTEROP

// Native calls for the managed ComWrappers API
class ComWrappersNative
{
public:
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
        _Inout_ QCall::ObjectHandleOnStack retValue);

    static void QCALLTYPE RegisterForReferenceTrackerHost(
        _In_ QCall::ObjectHandleOnStack comWrappersImpl);
};

#endif // FEATURE_COMINTEROP
