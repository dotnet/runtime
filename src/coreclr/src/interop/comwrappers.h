// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_COMWRAPPERS_H_
#define _INTEROP_COMWRAPPERS_H_

#include "platform.h"
#include <objidl.h> // COM interfaces
#include <interoplib.h>
#include "referencetrackertypes.h"

using OBJECTHANDLE = InteropLib::OBJECTHANDLE;

enum class CreateComInterfaceFlags
{
    None = 0,
    CallerDefinedIUnknown = 1,
    IgnoreCache = 2,
    TrackerSupport = 4,
};

DEFINE_ENUM_FLAG_OPERATORS(CreateComInterfaceFlags);

enum class CreateRCWFlags
{
    None = 0,
    TrackerObject = 1,
    IgnoreCache = 2,
};

DEFINE_ENUM_FLAG_OPERATORS(CreateRCWFlags);

struct ComInterfaceEntry
{
    GUID IID;
    const void* Vtable;
};

// Forward declaration
namespace ABI
{
    struct ComInterfaceDispatch;
}

// Class for wrapping a managed object and projecting it in a non-managed environment
class ManagedObjectWrapper
{
public:
    OBJECTHANDLE Target;

private:
    const int32_t _runtimeDefinedCount;
    const int32_t _userDefinedCount;
    const ComInterfaceEntry* _runtimeDefined;
    const ComInterfaceEntry* _userDefined;
    ABI::ComInterfaceDispatch* _dispatches;

    LONGLONG _refCount = 1;
    const CreateComInterfaceFlags _flags;

public: // static
    // Convert the IUnknown if the instance is a ManagedObjectWrapper into a ManagedObjectWrapper, otherwise null.
    static ManagedObjectWrapper* MapIUnknownToWrapper(_In_ IUnknown* pUnk);

    // Create a ManagedObjectWrapper instance
    static ManagedObjectWrapper* Create(
        _In_ CreateComInterfaceFlags flags,
        _In_ OBJECTHANDLE objectHandle,
        _In_ int32_t userDefinedCount,
        _In_ ComInterfaceEntry* userDefined);

private:
    ManagedObjectWrapper(
        _In_ CreateComInterfaceFlags flags,
        _In_ OBJECTHANDLE objectHandle,
        _In_ int32_t runtimeDefinedCount,
        _In_ const ComInterfaceEntry* runtimeDefined,
        _In_ int32_t userDefinedCount,
        _In_ const ComInterfaceEntry* userDefined,
        _In_ ABI::ComInterfaceDispatch* dispatches);

public:
    ~ManagedObjectWrapper();

    void* As(_In_ REFIID riid);
    OBJECTHANDLE GetObjectHandle() const;
    bool IsAlive() const;
    bool IsSet(_In_ CreateComInterfaceFlags flag) const;

public: // IReferenceTrackerTarget
    ULONG AddRefFromReferenceTracker();
    ULONG ReleaseFromReferenceTracker();
    HRESULT Peg();
    HRESULT Unpeg();

public: // Lifetime
    HRESULT QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR * __RPC_FAR * ppvObject);
    ULONG AddRef(void);
    ULONG Release(void);
};

// ABI contract. This below offset is assumed in managed code.
ABI_ASSERT(offsetof(ManagedObjectWrapper, Target) == 0);

// Class for connecting a native COM object to a managed object instance
class NativeCOMWrapperInstance
{
    void* _gcHandle;
    IReferenceTracker* _trackerObject;
    IAgileReference* _objectReference;

public:
    NativeCOMWrapperInstance(_In_ void* gcHandle, _In_ IReferenceTracker* trackerObject, _In_ IAgileReference* reference);
    ~NativeCOMWrapperInstance();

    void* GetObjectGCHandle() const;
    bool IsAlive() const;

    // Get the IReferenceTracker instance without going through the reference proxy.
    IReferenceTracker* GetReferenceTrackerFast();

    // Get a type instance of the desired type through the reference proxy.
    template<typename T>
    HRESULT GetInstanceProxy(_Outptr_ T** t)
    {
        return _objectReference->Resolve(t);
    }
};

#endif // _INTEROP_COMWRAPPERS_H_