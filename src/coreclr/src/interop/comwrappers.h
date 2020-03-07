// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_COMWRAPPERS_H_
#define _INTEROP_COMWRAPPERS_H_

#include "platform.h"
#include <interoplib.h>
#include "referencetrackertypes.h"

enum class CreateComInterfaceFlags
{
    None = 0,
    CallerDefinedIUnknown = 1,
    TrackerSupport = 2,
};

DEFINE_ENUM_FLAG_OPERATORS(CreateComInterfaceFlags);

enum class CreateObjectFlags
{
    None = 0,
    TrackerObject = 1,
    IgnoreCache = 2,
};

DEFINE_ENUM_FLAG_OPERATORS(CreateObjectFlags);

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
    InteropLib::OBJECTHANDLE Target;

private:
    const int32_t _runtimeDefinedCount;
    const int32_t _userDefinedCount;
    const ComInterfaceEntry* _runtimeDefined;
    const ComInterfaceEntry* _userDefined;
    ABI::ComInterfaceDispatch* _dispatches;

    LONGLONG _refCount = 1;
    const CreateComInterfaceFlags _flags;

public: // static
    // Get the implementation for IUnknown.
    static void GetIUnknownImpl(
            _Out_ void** fpQueryInterface,
            _Out_ void** fpAddRef,
            _Out_ void** fpRelease);

    // Convert the IUnknown if the instance is a ManagedObjectWrapper
    // into a ManagedObjectWrapper, otherwise null.
    static ManagedObjectWrapper* MapFromIUnknown(_In_ IUnknown* pUnk);

    // Create a ManagedObjectWrapper instance
    static HRESULT Create(
        _In_ CreateComInterfaceFlags flags,
        _In_ InteropLib::OBJECTHANDLE objectHandle,
        _In_ int32_t userDefinedCount,
        _In_ ComInterfaceEntry* userDefined,
        _Outptr_ ManagedObjectWrapper** mow);

    // Destroy the instance
    static void Destroy(_In_ ManagedObjectWrapper* wrapper);

private:
    ManagedObjectWrapper(
        _In_ CreateComInterfaceFlags flags,
        _In_ InteropLib::OBJECTHANDLE objectHandle,
        _In_ int32_t runtimeDefinedCount,
        _In_ const ComInterfaceEntry* runtimeDefined,
        _In_ int32_t userDefinedCount,
        _In_ const ComInterfaceEntry* userDefined,
        _In_ ABI::ComInterfaceDispatch* dispatches);

    ~ManagedObjectWrapper();

public:

    void* As(_In_ REFIID riid);
    // Attempt to set the target object handle based on an assumed current value.
    bool TrySetObjectHandle(_In_ InteropLib::OBJECTHANDLE objectHandle, _In_ InteropLib::OBJECTHANDLE current = nullptr);
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
class NativeObjectWrapperContext
{
#ifdef _DEBUG
    const size_t _sentinal;
#endif

    IReferenceTracker* _trackerObject;
    void* _runtimeContext;

public: // static
    // Convert a context pointer into a NativeObjectWrapperContext.
    static NativeObjectWrapperContext* MapFromRuntimeContext(_In_ void* cxt);

    // Create a NativeObjectWrapperContext instance
    static HRESULT NativeObjectWrapperContext::Create(
        _In_ IUnknown* external,
        _In_ CreateObjectFlags flags,
        _In_ size_t runtimeContextSize,
        _Outptr_ NativeObjectWrapperContext** context);

    // Destroy the instance
    static void Destroy(_In_ NativeObjectWrapperContext* wrapper);

private:
    NativeObjectWrapperContext(_In_ IReferenceTracker* trackerObject, _In_ void* runtimeContext);
    ~NativeObjectWrapperContext();

public:
    // Get the associated runtime context for this context.
    void* GetRuntimeContext() const;

    // Get the IReferenceTracker instance.
    IReferenceTracker* GetReferenceTracker() const;
};

// API for creating an IAgileReference instance to a supplied IUnknown instance.
// See https://docs.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-rogetagilereference
//
// N.B. Using a template so that callers are required to provide an explicit IUnknown instance.
template<typename T>
HRESULT CreateAgileReference(
    _In_ T* object,
    _Outptr_ IAgileReference** agileReference);

// Manage native object wrappers that support IReferenceTracker.
class TrackerObjectManager
{
public:
    // Attempt to set a runtime implementation for use by the IReferenceTrackerHost implementation.
    static bool TrySetReferenceTrackerHostRuntimeImpl(
        _In_ InteropLib::OBJECTHANDLE objectHandle,
        _In_ InteropLib::OBJECTHANDLE current = nullptr);

    // Called when an IReferenceTracker instance is found.
    static HRESULT OnIReferenceTrackerFound(_In_ IReferenceTracker* obj);

    // Called after wrapper has been created.
    static HRESULT AfterWrapperCreated(_In_ NativeObjectWrapperContext* cxt);

    // Called before wrapper is about to be destroyed (the same lifetime as short weak handle).
    static HRESULT BeforeWrapperDestroyed(_In_ NativeObjectWrapperContext* cxt);

public:
    // Begin the reference tracking process for external objects.
    static HRESULT BeginReferenceTracking(InteropLibImports::RuntimeCallContext* cxt);

    // End the reference tracking process for external object.
    static HRESULT EndReferenceTracking();

    // Clean up internal resources used for reference tracking.
    static void OnShutdown();
};

// Class used to hold COM objects (i.e. IUnknown base class)
// This class mimics the semantics of ATL::CComPtr<T> (https://docs.microsoft.com/cpp/atl/reference/ccomptr-class).
template<typename T>
struct ComHolder
{
    T* p;

    ComHolder()
        : p{ nullptr }
    { }

    ComHolder(_In_ const ComHolder&) = delete;
    ComHolder& operator=(_In_ const ComHolder&) = delete;

    ComHolder(_Inout_ ComHolder&& other)
        : p{ other.Detach() }
    { }

    ComHolder& operator=(_Inout_ ComHolder&& other)
    {
        Release();
        p = other.Detach();
        return (*this);
    }

    ComHolder(_In_ T* i)
        : p{ i }
    {
        _ASSERTE(p != nullptr);
        (void)p->AddRef();
    }

    ~ComHolder()
    {
        Release();
    }

    T** operator&()
    {
        return &p;
    }

    T* operator->()
    {
        return p;
    }

    operator T*()
    {
        return p;
    }

    void Attach(_In_opt_ T* i) noexcept
    {
        Release();
        if (i != nullptr)
            (void)i->AddRef();
        p = i;
    }

    T* Detach() noexcept
    {
        T* tmp = p;
        p = nullptr;
        return tmp;
    }

    void Release() noexcept
    {
        if (p != nullptr)
        {
            (void)p->Release();
            p = nullptr;
        }
    }
};
#endif // _INTEROP_COMWRAPPERS_H_