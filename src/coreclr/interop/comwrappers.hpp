// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_COMWRAPPERS_HPP_
#define _INTEROP_COMWRAPPERS_HPP_

#include "platform.h"
#include <interoplib.h>
#include <interoplibabi.h>
#include "referencetrackertypes.hpp"

using InteropLib::Com::CreateComInterfaceFlagsEx;

static constexpr size_t ManagedObjectWrapperRefCountOffset();
static constexpr size_t ManagedObjectWrapperFlagsOffset();

// Class for wrapping a managed object and projecting it in a non-managed environment
class ManagedObjectWrapper final : public InteropLib::ABI::ManagedObjectWrapperLayout
{
public: // static
    // Get the implementation for IUnknown.
    static void GetIUnknownImpl(
            _Out_ void** fpQueryInterface,
            _Out_ void** fpAddRef,
            _Out_ void** fpRelease);

    static void const* GetIReferenceTrackerTargetImpl() noexcept;

    static void const* GetTaggedCurrentVersionImpl() noexcept;

    // Convert the IUnknown if the instance is a ManagedObjectWrapper
    // into a ManagedObjectWrapper, otherwise null.
    static ManagedObjectWrapper* MapFromIUnknown(_In_ IUnknown* pUnk);

    // Convert the IUnknown if the instance is a ManagedObjectWrapper
    // into a ManagedObjectWrapper, otherwise null. This API provides
    // a stronger guarantee than MapFromIUnknown(), but does so by
    // performing a QueryInterface() which may not always be possible.
    // See implementation for more details.
    static ManagedObjectWrapper* MapFromIUnknownWithQueryInterface(_In_ IUnknown* pUnk);
private:
    // Query the runtime defined tables.
    void* AsRuntimeDefined(_In_ REFIID riid);

    // Query the user defined tables.
    void* AsUserDefined(_In_ REFIID riid);
public:
    // N.B. Does not impact the reference count of the object.
    void* As(_In_ REFIID riid);

    // Attempt to set the target object handle based on an assumed current value.
    bool IsSet(_In_ CreateComInterfaceFlagsEx flag) const;
    void SetFlag(_In_ CreateComInterfaceFlagsEx flag);
    void ResetFlag(_In_ CreateComInterfaceFlagsEx flag);

    // Indicate if the wrapper should be considered a GC root.
    bool IsRooted() const;

    // Check if the wrapper has been marked to be destroyed.
    bool IsMarkedToDestroy() const;

    InteropLib::OBJECTHANDLE GetTarget() const;

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

// Manage native object wrappers that support IReferenceTracker.
class TrackerObjectManager
{
public:
    static bool HasReferenceTrackerManager();

    static bool TryRegisterReferenceTrackerManager(_In_ IReferenceTrackerManager* manager);

    // Called before wrapper is about to be finalized (the same lifetime as short weak handle).
    static HRESULT BeforeWrapperFinalized(_In_ IReferenceTracker* obj);

public:
    // Begin the reference tracking process for external objects.
    static HRESULT BeginReferenceTracking(InteropLibImports::RuntimeCallContext* cxt);

    // End the reference tracking process for external object.
    static HRESULT EndReferenceTracking();

    static HRESULT DetachNonPromotedObjects(_In_ InteropLibImports::RuntimeCallContext* cxt);
};

// Class used to hold COM objects (i.e. IUnknown base class)
// This class mimics the semantics of ATL::CComPtr<T> (https://learn.microsoft.com/cpp/atl/reference/ccomptr-class).
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

// Keep these forward declarations in sync with the method definitions in interop/comwrappers.cpp
namespace InteropLib
{
    namespace ABI
    {
        struct ComInterfaceDispatch;
    }
}
HRESULT STDMETHODCALLTYPE ManagedObjectWrapper_QueryInterface(
    _In_ InteropLib::ABI::ComInterfaceDispatch* disp,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject);
HRESULT STDMETHODCALLTYPE TrackerTarget_QueryInterface(
    _In_ InteropLib::ABI::ComInterfaceDispatch* disp,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject);

#endif // _INTEROP_COMWRAPPERS_HPP_
