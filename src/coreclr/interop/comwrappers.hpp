// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_COMWRAPPERS_HPP_
#define _INTEROP_COMWRAPPERS_HPP_

#include "platform.h"
#include <interoplib.h>
#include <interoplibabi.h>
#include "referencetrackertypes.hpp"

#ifndef DEFINE_ENUM_FLAG_OPERATORS
#define DEFINE_ENUM_FLAG_OPERATORS(ENUMTYPE) \
extern "C++" { \
    inline ENUMTYPE operator | (ENUMTYPE a, ENUMTYPE b) { return ENUMTYPE(((int)a)|((int)b)); } \
    inline ENUMTYPE operator |= (ENUMTYPE &a, ENUMTYPE b) { return (ENUMTYPE &)(((int &)a) |= ((int)b)); } \
    inline ENUMTYPE operator & (ENUMTYPE a, ENUMTYPE b) { return ENUMTYPE(((int)a)&((int)b)); } \
    inline ENUMTYPE operator &= (ENUMTYPE &a, ENUMTYPE b) { return (ENUMTYPE &)(((int &)a) &= ((int)b)); } \
    inline ENUMTYPE operator ~ (ENUMTYPE a) { return (ENUMTYPE)(~((int)a)); } \
    inline ENUMTYPE operator ^ (ENUMTYPE a, ENUMTYPE b) { return ENUMTYPE(((int)a)^((int)b)); } \
    inline ENUMTYPE operator ^= (ENUMTYPE &a, ENUMTYPE b) { return (ENUMTYPE &)(((int &)a) ^= ((int)b)); } \
}
#endif

enum class CreateComInterfaceFlagsEx : int32_t
{
    None = InteropLib::Com::CreateComInterfaceFlags_None,
    CallerDefinedIUnknown = InteropLib::Com::CreateComInterfaceFlags_CallerDefinedIUnknown,
    TrackerSupport = InteropLib::Com::CreateComInterfaceFlags_TrackerSupport,

    // Highest bits are reserved for internal usage
    LacksICustomQueryInterface = 1 << 29,
    IsComActivated = 1 << 30,
    IsPegged = 1 << 31,

    InternalMask = IsPegged | IsComActivated | LacksICustomQueryInterface,
};

DEFINE_ENUM_FLAG_OPERATORS(CreateComInterfaceFlagsEx);

// Forward declarations
namespace ABI
{
    struct ComInterfaceDispatch;
    struct ComInterfaceEntry;
}

static constexpr size_t ManagedObjectWrapperRefCountOffset();

// Class for wrapping a managed object and projecting it in a non-managed environment
class ManagedObjectWrapper
{
    friend constexpr size_t ManagedObjectWrapperRefCountOffset();
public:
    Volatile<InteropLib::OBJECTHANDLE> Target;

private:
    LONGLONG _refCount;

    const int32_t _runtimeDefinedCount;
    const int32_t _userDefinedCount;
    const ABI::ComInterfaceEntry* _runtimeDefined;
    const ABI::ComInterfaceEntry* _userDefined;
    ABI::ComInterfaceDispatch* _dispatches;

    Volatile<CreateComInterfaceFlagsEx> _flags;

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
        _In_ InteropLib::Com::CreateComInterfaceFlags flags,
        _In_ InteropLib::OBJECTHANDLE objectHandle,
        _In_ int32_t userDefinedCount,
        _In_ ABI::ComInterfaceEntry* userDefined,
        _Outptr_ ManagedObjectWrapper** mow);

    // Destroy the instance
    static void Destroy(_In_ ManagedObjectWrapper* wrapper);

private:
    ManagedObjectWrapper(
        _In_ CreateComInterfaceFlagsEx flags,
        _In_ InteropLib::OBJECTHANDLE objectHandle,
        _In_ int32_t runtimeDefinedCount,
        _In_ const ABI::ComInterfaceEntry* runtimeDefined,
        _In_ int32_t userDefinedCount,
        _In_ const ABI::ComInterfaceEntry* userDefined,
        _In_ ABI::ComInterfaceDispatch* dispatches);

    ~ManagedObjectWrapper();

    // Query the runtime defined tables.
    void* AsRuntimeDefined(_In_ REFIID riid);

    // Query the user defined tables.
    void* AsUserDefined(_In_ REFIID riid);

public:
    // N.B. Does not impact the reference count of the object.
    void* As(_In_ REFIID riid);

    // Attempt to set the target object handle based on an assumed current value.
    bool TrySetObjectHandle(_In_ InteropLib::OBJECTHANDLE objectHandle, _In_ InteropLib::OBJECTHANDLE current = nullptr);
    bool IsSet(_In_ CreateComInterfaceFlagsEx flag) const;
    void SetFlag(_In_ CreateComInterfaceFlagsEx flag);
    void ResetFlag(_In_ CreateComInterfaceFlagsEx flag);

    // Indicate if the wrapper should be considered a GC root.
    bool IsRooted() const;

    // Check if the wrapper has been marked to be destroyed.
    bool IsMarkedToDestroy() const;

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

// ABI contract. This below offset is assumed in managed code and the DAC.
ABI_ASSERT(offsetof(ManagedObjectWrapper, Target) == 0);

static constexpr size_t ManagedObjectWrapperRefCountOffset()
{
    // _refCount is a private field and offsetof won't let you look at private fields.
    // To overcome, this function is a friend function of ManagedObjectWrapper.
    return offsetof(ManagedObjectWrapper, _refCount);
}

// ABI contract used by the DAC.
ABI_ASSERT(offsetof(ManagedObjectWrapper, Target) == offsetof(InteropLib::ABI::ManagedObjectWrapperLayout, ManagedObject));
ABI_ASSERT(ManagedObjectWrapperRefCountOffset() == offsetof(InteropLib::ABI::ManagedObjectWrapperLayout, RefCount));

// State ownership mechanism.
enum class TrackerObjectState
{
    NotSet,
    SetNoRelease,
    SetForRelease,
};

// Class for connecting a native COM object to a managed object instance
class NativeObjectWrapperContext
{
    IReferenceTracker* _trackerObject;
    void* _runtimeContext;
    Volatile<BOOL> _trackerObjectDisconnected;
    TrackerObjectState _trackerObjectState;
    IUnknown* _nativeObjectAsInner;

#ifdef _DEBUG
    size_t _sentinel;
#endif
public: // static
    // Convert a context pointer into a NativeObjectWrapperContext.
    static NativeObjectWrapperContext* MapFromRuntimeContext(_In_ void* cxt);

    // Create a NativeObjectWrapperContext instance
    static HRESULT Create(
        _In_ IUnknown* external,
        _In_opt_ IUnknown* nativeObjectAsInner,
        _In_ InteropLib::Com::CreateObjectFlags flags,
        _In_ size_t runtimeContextSize,
        _Outptr_ NativeObjectWrapperContext** context);

    // Destroy the instance
    static void Destroy(_In_ NativeObjectWrapperContext* wrapper);

private:
    NativeObjectWrapperContext(_In_ void* runtimeContext, _In_opt_ IReferenceTracker* trackerObject, _In_opt_ IUnknown* nativeObjectAsInner);
    ~NativeObjectWrapperContext();

public:
    // Get the associated runtime context for this context.
    void* GetRuntimeContext() const noexcept;

    // Get the IReferenceTracker instance.
    IReferenceTracker* GetReferenceTracker() const noexcept;

    // Disconnect reference tracker instance.
    void DisconnectTracker() noexcept;

private:
    void HandleReferenceTrackerAggregation() noexcept;
};

// Manage native object wrappers that support IReferenceTracker.
class TrackerObjectManager
{
public:
    // Called when an IReferenceTracker instance is found.
    static HRESULT OnIReferenceTrackerFound(_In_ IReferenceTracker* obj);

    // Called after wrapper has been created.
    static HRESULT AfterWrapperCreated(_In_ IReferenceTracker* obj);

    // Called before wrapper is about to be finalized (the same lifetime as short weak handle).
    static HRESULT BeforeWrapperFinalized(_In_ IReferenceTracker* obj);

public:
    // Begin the reference tracking process for external objects.
    static HRESULT BeginReferenceTracking(InteropLibImports::RuntimeCallContext* cxt);

    // End the reference tracking process for external object.
    static HRESULT EndReferenceTracking();
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
        Attach(other.Detach());
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
#endif // _INTEROP_COMWRAPPERS_HPP_
