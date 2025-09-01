// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "comwrappers.hpp"
#include <interoplibimports.h>
#include <corerror.h>
#include <minipal/utils.h>

#ifdef _WIN32
#include <new> // placement new
#endif // _WIN32

using OBJECTHANDLE = InteropLib::OBJECTHANDLE;
using TryInvokeICustomQueryInterfaceResult = InteropLibImports::TryInvokeICustomQueryInterfaceResult;

namespace ABI
{
    //---------------------------------------------------------------------------------
    // Dispatch section of the ManagedObjectWrapper (MOW)
    //
    // Within the dispatch section, the ManagedObjectWrapper itself is inserted at a defined
    // aligned location. This allows the simple masking of the any ComInterfaceDispatch* to get
    // access to the ManagedObjectWrapper by masking the lower N bits. Below is a sketch of how
    // the dispatch section would appear in a 32-bit process for a 16 bit alignment.
    //
    //           16 byte aligned                            Vtable
    //           +-----------+
    //           | MOW this  |
    //           +-----------+                              +-----+
    //  COM IP-->| VTable ptr|----------------------------->|slot1|
    //           +-----------+           +-----+            +-----+
    //           | VTable ptr|---------->|slot1|            |slot2|
    //           +-----------+           +-----+            +     +
    //           | VTable ptr|           | ....|            | ... |
    //           +-----------+           +     +            +     +
    //           | MOW this  |           |slotN|            |slotN|
    //           +           +           +-----+            +-----+
    //           |  ....     |
    //           +-----------+
    //
    // A 16 byte alignment permits a ratio of 3:1 COM vtables to ManagedObjectWrapper 'this'
    // pointers in a 32-bit process, but in a 64-bit process the mapping is only 1:1.
    // See the dispatch section building API below for an example of how indexing works.
    //--------------------------------------------------------------------------------

    using InteropLib::ABI::ComInterfaceDispatch;
    using InteropLib::ABI::ComInterfaceEntry;
    using InteropLib::ABI::DispatchAlignmentThisPtr;
    using InteropLib::ABI::DispatchThisPtrMask;
    using InteropLib::ABI::IndexIntoDispatchSection;

    // Given a dispatcher instance, return the associated ManagedObjectWrapper.
    ManagedObjectWrapper* ToManagedObjectWrapper(_In_ ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        intptr_t wrapperMaybe = reinterpret_cast<intptr_t>(disp) & DispatchThisPtrMask;
        return *reinterpret_cast<ManagedObjectWrapper**>(wrapperMaybe);
    }
}

// ManagedObjectWrapper_QueryInterface needs to be visible outside of this compilation unit
// to support the DAC (look for the GetEEFuncEntryPoint call).
HRESULT STDMETHODCALLTYPE ManagedObjectWrapper_QueryInterface(
    _In_ ABI::ComInterfaceDispatch* disp,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
{
    ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
    return wrapper->QueryInterface(riid, ppvObject);
}

namespace
{
    ULONG STDMETHODCALLTYPE ManagedObjectWrapper_AddRef(_In_ ABI::ComInterfaceDispatch* disp)
    {
        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        return wrapper->AddRef();
    }

    ULONG STDMETHODCALLTYPE ManagedObjectWrapper_Release(_In_ ABI::ComInterfaceDispatch* disp)
    {
        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        return wrapper->Release();
    }

    // Hard-coded ManagedObjectWrapper IUnknown vtable.
    const struct
    {
        decltype(&ManagedObjectWrapper_QueryInterface) QueryInterface;
        decltype(&ManagedObjectWrapper_AddRef) AddRef;
        decltype(&ManagedObjectWrapper_Release) Release;
    } ManagedObjectWrapper_IUnknownImpl {
        &ManagedObjectWrapper_QueryInterface,
        &ManagedObjectWrapper_AddRef,
        &ManagedObjectWrapper_Release
    };

    static_assert(sizeof(ManagedObjectWrapper_IUnknownImpl) == (3 * sizeof(void*)), "Unexpected vtable size");
}

// TrackerTarget_QueryInterface needs to be visible outside of this compilation unit
// to support the DAC (look for the GetEEFuncEntryPoint call).
HRESULT STDMETHODCALLTYPE TrackerTarget_QueryInterface(
    _In_ ABI::ComInterfaceDispatch* disp,
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
{
    ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);

    // AddRef is "safe" at this point because since it is a MOW with an outstanding
    // Reference Tracker reference, we know for sure the MOW is not claimed yet
    // but the managed object could be. If the managed object is alive at this
    // moment the AddRef will ensure it remains alive for the duration of the
    // QueryInterface.
    ComHolder<ManagedObjectWrapper> ensureStableLifetime{ wrapper };

    // For MOWs that have outstanding Reference Tracker reference, they could be either:
    //  1. Marked to Destroy - in this case it is unsafe to touch wrapper.
    //  2. Object Handle target has been NULLed out by GC.
    if (wrapper->IsMarkedToDestroy()
        || !InteropLibImports::HasValidTarget(wrapper->GetTarget()))
    {
        // It is unsafe to proceed with a QueryInterface call. The MOW has been
        // marked destroyed or the associated managed object has been collected.
        return COR_E_ACCESSING_CCW;
    }

    return wrapper->QueryInterface(riid, ppvObject);
}

namespace
{
    const int32_t TrackerRefShift = 32;
    const ULONGLONG TrackerRefCounter   = ULONGLONG{ 1 } << TrackerRefShift;
    const ULONGLONG DestroySentinel     = 0x0000000080000000;
    const ULONGLONG TrackerRefCountMask = 0xffffffff00000000;
    const ULONGLONG ComRefCountMask     = 0x000000007fffffff;

    constexpr ULONG GetTrackerCount(_In_ ULONGLONG c)
    {
        return static_cast<ULONG>((c & TrackerRefCountMask) >> TrackerRefShift);
    }

    constexpr ULONG GetComCount(_In_ ULONGLONG c)
    {
        return static_cast<ULONG>(c & ComRefCountMask);
    }

    constexpr bool IsMarkedToDestroy(_In_ ULONGLONG c)
    {
        return (c & DestroySentinel) != 0;
    }

    ULONG STDMETHODCALLTYPE TrackerTarget_AddRefFromReferenceTracker(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlagsEx::TrackerSupport));

        return wrapper->AddRefFromReferenceTracker();
    }

    ULONG STDMETHODCALLTYPE TrackerTarget_ReleaseFromReferenceTracker(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlagsEx::TrackerSupport));

        return wrapper->ReleaseFromReferenceTracker();
    }

    HRESULT STDMETHODCALLTYPE TrackerTarget_Peg(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlagsEx::TrackerSupport));

        return wrapper->Peg();
    }

    HRESULT STDMETHODCALLTYPE TrackerTarget_Unpeg(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlagsEx::TrackerSupport));

        return wrapper->Unpeg();
    }

    // Hard-coded IReferenceTrackerTarget vtable
    const struct
    {
        decltype(&TrackerTarget_QueryInterface) QueryInterface;
        decltype(&ManagedObjectWrapper_AddRef) AddRef;
        decltype(&ManagedObjectWrapper_Release) Release;
        decltype(&TrackerTarget_AddRefFromReferenceTracker) AddRefFromReferenceTracker;
        decltype(&TrackerTarget_ReleaseFromReferenceTracker) ReleaseFromReferenceTracker;
        decltype(&TrackerTarget_Peg) Peg;
        decltype(&TrackerTarget_Unpeg) Unpeg;
    } ManagedObjectWrapper_IReferenceTrackerTargetImpl {
        &TrackerTarget_QueryInterface,
        &ManagedObjectWrapper_AddRef,
        &ManagedObjectWrapper_Release,
        &TrackerTarget_AddRefFromReferenceTracker,
        &TrackerTarget_ReleaseFromReferenceTracker,
        &TrackerTarget_Peg,
        &TrackerTarget_Unpeg
    };

    static_assert(sizeof(ManagedObjectWrapper_IReferenceTrackerTargetImpl) == (7 * sizeof(void*)), "Unexpected vtable size");
}

void const* ManagedObjectWrapper::GetIReferenceTrackerTargetImpl() noexcept
{
    return &ManagedObjectWrapper_IReferenceTrackerTargetImpl;
}

namespace
{
    // This IID represents an internal interface we define to tag any ManagedObjectWrappers we create.
    // This interface type and GUID do not correspond to any public interface; it is an internal implementation detail.
    // 5c13e51c-4f32-4726-a3fd-f3edd63da3a0
    const GUID IID_TaggedImpl = { 0x5c13e51c, 0x4f32, 0x4726, { 0xa3, 0xfd, 0xf3, 0xed, 0xd6, 0x3d, 0xa3, 0xa0 } };

    class DECLSPEC_UUID("5c13e51c-4f32-4726-a3fd-f3edd63da3a0") ITaggedImpl : public IUnknown
    {
    public:
        STDMETHOD(IsCurrentVersion)(_In_ void* version) = 0;
    };

    HRESULT STDMETHODCALLTYPE ITaggedImpl_IsCurrentVersion(_In_ void*, _In_ void* version)
    {
        return (version == (void*)&ITaggedImpl_IsCurrentVersion) ? S_OK : E_FAIL;
    }
}

void ManagedObjectWrapper::GetIUnknownImpl(
    _Out_ void** fpQueryInterface,
    _Out_ void** fpAddRef,
    _Out_ void** fpRelease)
{
    _ASSERTE(fpQueryInterface != nullptr
            && fpAddRef != nullptr
            && fpRelease != nullptr);

    *fpQueryInterface = (void*)ManagedObjectWrapper_IUnknownImpl.QueryInterface;
    *fpAddRef = (void*)ManagedObjectWrapper_IUnknownImpl.AddRef;
    *fpRelease = (void*)ManagedObjectWrapper_IUnknownImpl.Release;
}

void const* ManagedObjectWrapper::GetTaggedCurrentVersionImpl() noexcept
{
    return reinterpret_cast<void const*>(&ITaggedImpl_IsCurrentVersion);
}

// The logic here should match code:ClrDataAccess::DACTryGetComWrappersObjectFromCCW in daccess/request.cpp
ManagedObjectWrapper* ManagedObjectWrapper::MapFromIUnknown(_In_ IUnknown* pUnk)
{
    _ASSERTE(pUnk != nullptr);

    // If the first Vtable entry is part of a ManagedObjectWrapper impl,
    // we know how to interpret the IUnknown.
    void** vtable = *reinterpret_cast<void***>(pUnk);
    if (*vtable != ManagedObjectWrapper_IUnknownImpl.QueryInterface
        && *vtable != ManagedObjectWrapper_IReferenceTrackerTargetImpl.QueryInterface)
    {
        return nullptr;
    }

    ABI::ComInterfaceDispatch* disp = reinterpret_cast<ABI::ComInterfaceDispatch*>(pUnk);
    return ABI::ToManagedObjectWrapper(disp);
}

ManagedObjectWrapper* ManagedObjectWrapper::MapFromIUnknownWithQueryInterface(_In_ IUnknown* pUnk)
{
    ManagedObjectWrapper* wrapper = MapFromIUnknown(pUnk);
    if (wrapper != nullptr)
        return wrapper;

    // It is possible the user has defined their own IUnknown impl so
    // we fallback to the tagged interface approach to be sure. This logic isn't
    // handled by the DAC logic and that is by-design. Care must be taken when
    // performing this QueryInterface() since users are free to implement a wrapper
    // using managed code and therefore performing this operation may not be
    // possible during a GC.
    ComHolder<ITaggedImpl> implMaybe;
    if (S_OK != pUnk->QueryInterface(IID_TaggedImpl, (void**)&implMaybe)
        || S_OK != implMaybe->IsCurrentVersion((void*)&ITaggedImpl_IsCurrentVersion))
    {
        return nullptr;
    }

    ABI::ComInterfaceDispatch* disp = reinterpret_cast<ABI::ComInterfaceDispatch*>(pUnk);
    return ABI::ToManagedObjectWrapper(disp);
}

void* ManagedObjectWrapper::AsRuntimeDefined(_In_ REFIID riid)
{
    // The order of interface lookup here is important.
    // See ComWrappers.CreateManagedObjectWrapper() for the expected order.
    int i = _userDefinedCount;

    if ((_flags & CreateComInterfaceFlagsEx::CallerDefinedIUnknown) == CreateComInterfaceFlagsEx::None)
    {
        if (riid == IID_IUnknown)
        {
            return ABI::IndexIntoDispatchSection(i, _dispatches);
        }

        ++i;
    }

    if ((_flags & CreateComInterfaceFlagsEx::TrackerSupport) == CreateComInterfaceFlagsEx::TrackerSupport)
    {
        if (riid == IID_IReferenceTrackerTarget)
        {
            return ABI::IndexIntoDispatchSection(i, _dispatches);
        }

        ++i;
    }

    if (riid == IID_TaggedImpl)
    {
        return ABI::IndexIntoDispatchSection(i, _dispatches);
    }

    return nullptr;
}

void* ManagedObjectWrapper::AsUserDefined(_In_ REFIID riid)
{
    for (int32_t i = 0; i < _userDefinedCount; ++i)
    {
        if (IsEqualGUID(_userDefined[i].IID, riid))
        {
            return ABI::IndexIntoDispatchSection(i, _dispatches);
        }
    }

    return nullptr;
}

void* ManagedObjectWrapper::As(_In_ REFIID riid)
{
    // Find target interface and return dispatcher or null if not found.
    void* typeMaybe = AsRuntimeDefined(riid);
    if (typeMaybe == nullptr)
        typeMaybe = AsUserDefined(riid);

    return typeMaybe;
}

bool ManagedObjectWrapper::IsSet(_In_ CreateComInterfaceFlagsEx flag) const
{
    return (_flags & flag) != CreateComInterfaceFlagsEx::None;
}

void ManagedObjectWrapper::SetFlag(_In_ CreateComInterfaceFlagsEx flag)
{
    LONG setMask = (LONG)flag;
    ::InterlockedOr((LONG*)&_flags, setMask);
}

void ManagedObjectWrapper::ResetFlag(_In_ CreateComInterfaceFlagsEx flag)
{
    LONG resetMask = (LONG)~flag;
    ::InterlockedAnd((LONG*)&_flags, resetMask);
}

bool ManagedObjectWrapper::IsRooted() const
{
    bool rooted = GetComCount(_refCount) > 0;
    if (!rooted)
    {
        // Only consider tracker ref count to be a "strong" ref count if it is pegged and alive.
        rooted = (GetTrackerCount(_refCount) > 0)
                 && (IsSet(CreateComInterfaceFlagsEx::IsPegged)
                     || InteropLibImports::GetGlobalPeggingState());
    }

    return rooted;
}

bool ManagedObjectWrapper::IsMarkedToDestroy() const
{
    return ::IsMarkedToDestroy(_refCount);
}

ULONG ManagedObjectWrapper::AddRefFromReferenceTracker()
{
    LONGLONG prev;
    LONGLONG curr;
    do
    {
        prev = _refCount;
        curr = prev + TrackerRefCounter;
    } while (::InterlockedCompareExchange64(&_refCount, curr, prev) != prev);

    return GetTrackerCount(curr);
}

ULONG ManagedObjectWrapper::ReleaseFromReferenceTracker()
{
    if (GetTrackerCount(_refCount) == 0)
    {
        _ASSERTE(!"Over release of MOW - ReferenceTracker");
        return (ULONG)-1;
    }

    LONGLONG refCount;
    LONGLONG prev;
    do
    {
        prev = _refCount;
        refCount = prev - TrackerRefCounter;
    } while (::InterlockedCompareExchange64(&_refCount, refCount, prev) != prev);

    // If we observe the destroy sentinel, then this release
    // must destroy the wrapper.
    if (refCount == DestroySentinel)
    {
        InteropLib::OBJECTHANDLE handle = InterlockedExchangePointer(&_target, nullptr);
        if (handle != nullptr)
        {
            InteropLibImports::DestroyHandle(handle);
        }
    }

    return GetTrackerCount(refCount);
}

HRESULT ManagedObjectWrapper::Peg()
{
    SetFlag(CreateComInterfaceFlagsEx::IsPegged);
    return S_OK;
}

HRESULT ManagedObjectWrapper::Unpeg()
{
    ResetFlag(CreateComInterfaceFlagsEx::IsPegged);
    return S_OK;
}

HRESULT ManagedObjectWrapper::QueryInterface(
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
{
    if (ppvObject == nullptr)
        return E_POINTER;

    // Find target interface
    *ppvObject = AsRuntimeDefined(riid);
    if (*ppvObject == nullptr)
    {
        // Check if the managed object has implemented ICustomQueryInterface
        if (!IsSet(CreateComInterfaceFlagsEx::LacksICustomQueryInterface))
        {
            TryInvokeICustomQueryInterfaceResult result = InteropLibImports::TryInvokeICustomQueryInterface(GetTarget(), riid, ppvObject);
            switch (result)
            {
                case TryInvokeICustomQueryInterfaceResult::Handled:
                    _ASSERTE(*ppvObject != nullptr);
                    return S_OK;

                case TryInvokeICustomQueryInterfaceResult::NotHandled:
                    // Continue querying the static tables.
                    break;

                case TryInvokeICustomQueryInterfaceResult::Failed:
                    _ASSERTE(*ppvObject == nullptr);
                    return E_NOINTERFACE;

                default:
#if !defined(__clang__) || (__clang_major__ > 13) // Workaround bug in old clang
                    _ASSERTE(false && "Unknown result value");
#endif
                    FALLTHROUGH;
                case TryInvokeICustomQueryInterfaceResult::FailedToInvoke:
                    // Set the 'lacks' flag since our attempt to use ICustomQueryInterface
                    // indicated the object lacks an implementation.
                    SetFlag(CreateComInterfaceFlagsEx::LacksICustomQueryInterface);
                    break;

                case TryInvokeICustomQueryInterfaceResult::OnGCThread:
                    // We are going to assume the caller is attempting to
                    // check if this wrapper has an interface that is supported
                    // during a GC and not trying to do something bad.
                    // Instead of returning immediately, we handle the case
                    // the same way that would occur if the managed object lacked
                    // an ICustomQueryInterface implementation.
                    break;
            }
        }

        *ppvObject = AsUserDefined(riid);
        if (*ppvObject == nullptr)
            return E_NOINTERFACE;
    }

    (void)AddRef();
    return S_OK;
}

ULONG ManagedObjectWrapper::AddRef(void)
{
    return GetComCount(::InterlockedIncrement64(&_refCount));
}

ULONG ManagedObjectWrapper::Release(void)
{
    if (GetComCount(_refCount) == 0)
    {
        _ASSERTE(!"Over release of MOW - COM");
        return (ULONG)-1;
    }

    return GetComCount(::InterlockedDecrement64(&_refCount));
}

InteropLib::OBJECTHANDLE ManagedObjectWrapper::GetTarget() const
{
    return _target;
}
