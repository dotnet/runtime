// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "comwrappers.hpp"
#include <interoplibimports.h>
#include <corerror.h>

#ifdef _WIN32
#include <new> // placement new
#endif // _WIN32

using OBJECTHANDLE = InteropLib::OBJECTHANDLE;
using AllocScenario = InteropLibImports::AllocScenario;
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

    struct ComInterfaceDispatch
    {
        const void* vtable;
    };
    ABI_ASSERT(sizeof(ComInterfaceDispatch) == sizeof(void*));

    using InteropLib::ABI::DispatchAlignmentThisPtr;
    using InteropLib::ABI::DispatchThisPtrMask;
    ABI_ASSERT(sizeof(void*) < DispatchAlignmentThisPtr);

    const intptr_t AlignmentThisPtrMaxPadding = DispatchAlignmentThisPtr - sizeof(void*);
    const size_t EntriesPerThisPtr = (DispatchAlignmentThisPtr / sizeof(void*)) - 1;

    // Check if the instance can dispatch according to the ABI.
    bool IsAbleToDispatch(_In_ ComInterfaceDispatch* disp)
    {
        return (reinterpret_cast<intptr_t>(disp) & DispatchThisPtrMask) != 0;
    }

    // Given the number of dispatch entries, compute the needed number of 'this' pointer entries.
    constexpr size_t ComputeThisPtrForDispatchSection(_In_ size_t dispatchCount)
    {
        return (dispatchCount / ABI::EntriesPerThisPtr) + ((dispatchCount % ABI::EntriesPerThisPtr) == 0 ? 0 : 1);
    }

    // Given a pointer and a padding allowance, attempt to find an offset into
    // the memory that is properly aligned for the dispatch section.
    char* AlignDispatchSection(_In_ char* section, _In_ intptr_t extraPadding)
    {
        _ASSERTE(section != nullptr);

        // If the dispatch section is not properly aligned by default, we
        // utilize the padding to make sure the dispatch section is aligned.
        while ((reinterpret_cast<intptr_t>(section) % ABI::DispatchAlignmentThisPtr) != 0)
        {
            // Check if there is padding to attempt an alignment.
            if (extraPadding <= 0)
                return nullptr;

            extraPadding -= sizeof(void*);

#ifdef _DEBUG
            // Poison unused portions of the section.
            ::memset(section, 0xff, sizeof(void*));
#endif

            section += sizeof(void*);
        }

        return section;
    }

    struct ComInterfaceEntry
    {
        GUID IID;
        const void* Vtable;
    };

    struct EntrySet
    {
        const ComInterfaceEntry* start;
        int32_t count;
    };

    // Populate the dispatch section with the entry sets
    ComInterfaceDispatch* PopulateDispatchSection(
        _In_ void* thisPtr,
        _In_ void* dispatchSection,
        _In_ size_t entrySetCount,
        _In_ const EntrySet* entrySets)
    {
        // Define dispatch section iterator.
        const void** currDisp = reinterpret_cast<const void**>(dispatchSection);

        // Keep rolling count of dispatch entries.
        int32_t dispCount = 0;

        // Iterate over all interface entry sets.
        const EntrySet* curr = entrySets;
        const EntrySet* end = entrySets + entrySetCount;
        for (; curr != end; ++curr)
        {
            const ComInterfaceEntry* currEntry = curr->start;
            int32_t entryCount = curr->count;

            // Update dispatch section with 'this' pointer and vtables.
            for (int32_t i = 0; i < entryCount; ++i, ++dispCount, ++currEntry)
            {
                // Insert the 'this' pointer at the appropriate locations
                // e.g.:
                //       32-bit         |      64-bit
                //   (0 * 4) % 16 =  0  |  (0 * 8) % 16 = 0
                //   (1 * 4) % 16 =  4  |  (1 * 8) % 16 = 8
                //   (2 * 4) % 16 =  8  |  (2 * 8) % 16 = 0
                //   (3 * 4) % 16 = 12  |  (3 * 8) % 16 = 8
                //   (4 * 4) % 16 =  0  |  (4 * 8) % 16 = 0
                //   (5 * 4) % 16 =  4  |  (5 * 8) % 16 = 8
                //
                if (((dispCount * sizeof(void*)) % ABI::DispatchAlignmentThisPtr) == 0)
                {
                    *currDisp++ = thisPtr;
                    ++dispCount;
                }

                // Fill in the dispatch entry
                *currDisp++ = currEntry->Vtable;
            }
        }

        return reinterpret_cast<ComInterfaceDispatch*>(dispatchSection);
    }

    // Given the entry index, compute the dispatch index.
    ComInterfaceDispatch* IndexIntoDispatchSection(_In_ int32_t i, _In_ ComInterfaceDispatch* dispatches)
    {
        // Convert the supplied zero based index into what it represents as a count.
        const size_t count = static_cast<size_t>(i) + 1;

        // Based on the supplied count, compute how many previous 'this' pointers would be
        // required in the dispatch section and add that to the supplied index to get the
        // index into the dispatch section.
        const size_t idx = ComputeThisPtrForDispatchSection(count) + i;

        ComInterfaceDispatch* disp = dispatches + idx;
        _ASSERTE(IsAbleToDispatch(disp));
        return disp;
    }

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
        || !InteropLibImports::HasValidTarget(wrapper->Target))
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

// The logic here should match code:ClrDataAccess::DACTryGetComWrappersObjectFromCCW in daccess/request.cpp
ManagedObjectWrapper* ManagedObjectWrapper::MapFromIUnknown(_In_ IUnknown* pUnk)
{
    _ASSERTE(pUnk != nullptr);

    // If the first Vtable entry is part of the ManagedObjectWrapper IUnknown impl,
    // we know how to interpret the IUnknown.
    void** vtable = *reinterpret_cast<void***>(pUnk);
    if (*vtable != ManagedObjectWrapper_IUnknownImpl.QueryInterface
        && *vtable != ManagedObjectWrapper_IReferenceTrackerTargetImpl.QueryInterface)
        return nullptr;

    ABI::ComInterfaceDispatch* disp = reinterpret_cast<ABI::ComInterfaceDispatch*>(pUnk);
    return ABI::ToManagedObjectWrapper(disp);
}

HRESULT ManagedObjectWrapper::Create(
    _In_ InteropLib::Com::CreateComInterfaceFlags flagsRaw,
    _In_ OBJECTHANDLE objectHandle,
    _In_ int32_t userDefinedCount,
    _In_ ABI::ComInterfaceEntry* userDefined,
    _Outptr_ ManagedObjectWrapper** mow)
{
    _ASSERTE(objectHandle != nullptr && mow != nullptr);

    auto flags = static_cast<CreateComInterfaceFlagsEx>(flagsRaw);
    _ASSERTE((flags & CreateComInterfaceFlagsEx::InternalMask) == CreateComInterfaceFlagsEx::None);

    // Maximum number of runtime supplied vtables.
    ABI::ComInterfaceEntry runtimeDefinedLocal[4];
    int32_t runtimeDefinedCount = 0;

    // Check if the caller will provide the IUnknown table.
    if ((flags & CreateComInterfaceFlagsEx::CallerDefinedIUnknown) == CreateComInterfaceFlagsEx::None)
    {
        ABI::ComInterfaceEntry& curr = runtimeDefinedLocal[runtimeDefinedCount++];
        curr.IID = __uuidof(IUnknown);
        curr.Vtable = &ManagedObjectWrapper_IUnknownImpl;
    }

    // Check if the caller wants tracker support.
    if ((flags & CreateComInterfaceFlagsEx::TrackerSupport) == CreateComInterfaceFlagsEx::TrackerSupport)
    {
        ABI::ComInterfaceEntry& curr = runtimeDefinedLocal[runtimeDefinedCount++];
        curr.IID = IID_IReferenceTrackerTarget;
        curr.Vtable = &ManagedObjectWrapper_IReferenceTrackerTargetImpl;
    }
    
    _ASSERTE(runtimeDefinedCount <= (int) ARRAYSIZE(runtimeDefinedLocal));

    // Compute size for ManagedObjectWrapper instance.
    const size_t totalRuntimeDefinedSize = runtimeDefinedCount * sizeof(ABI::ComInterfaceEntry);
    const size_t totalDefinedCount = static_cast<size_t>(runtimeDefinedCount) + userDefinedCount;

    // Compute the total entry size of dispatch section.
    const size_t totalDispatchSectionCount = ABI::ComputeThisPtrForDispatchSection(totalDefinedCount) + totalDefinedCount;
    const size_t totalDispatchSectionSize = totalDispatchSectionCount * sizeof(void*);

    // Allocate memory for the ManagedObjectWrapper.
    char* wrapperMem = (char*)InteropLibImports::MemAlloc(sizeof(ManagedObjectWrapper) + totalRuntimeDefinedSize + totalDispatchSectionSize + ABI::AlignmentThisPtrMaxPadding, AllocScenario::ManagedObjectWrapper);
    if (wrapperMem == nullptr)
        return E_OUTOFMEMORY;

    // Compute Runtime defined offset.
    char* runtimeDefinedOffset = wrapperMem + sizeof(ManagedObjectWrapper);

    // Copy in runtime supplied COM interface entries.
    ABI::ComInterfaceEntry* runtimeDefined = nullptr;
    if (0 < runtimeDefinedCount)
    {
        ::memcpy(runtimeDefinedOffset, runtimeDefinedLocal, totalRuntimeDefinedSize);
        runtimeDefined = reinterpret_cast<ABI::ComInterfaceEntry*>(runtimeDefinedOffset);
    }

    // Compute the dispatch section offset and ensure it is aligned.
    char* dispatchSectionOffset = runtimeDefinedOffset + totalRuntimeDefinedSize;
    dispatchSectionOffset = ABI::AlignDispatchSection(dispatchSectionOffset, ABI::AlignmentThisPtrMaxPadding);
    if (dispatchSectionOffset == nullptr)
        return E_UNEXPECTED;

    // Define the sets for the tables to insert
    const ABI::EntrySet AllEntries[] =
    {
        { runtimeDefined, runtimeDefinedCount },
        { userDefined, userDefinedCount }
    };

    ABI::ComInterfaceDispatch* dispSection = ABI::PopulateDispatchSection(wrapperMem, dispatchSectionOffset, ARRAYSIZE(AllEntries), AllEntries);

    ManagedObjectWrapper* wrapper = new (wrapperMem) ManagedObjectWrapper
        {
            flags,
            objectHandle,
            runtimeDefinedCount,
            runtimeDefined,
            userDefinedCount,
            userDefined,
            dispSection
        };

    *mow = wrapper;
    return S_OK;
}

void ManagedObjectWrapper::Destroy(_In_ ManagedObjectWrapper* wrapper)
{
    _ASSERTE(wrapper != nullptr);
    _ASSERTE(GetComCount(wrapper->_refCount) == 0);

    // Attempt to set the destroyed bit.
    LONGLONG refCount;
    LONGLONG prev;
    do
    {
        prev = wrapper->_refCount;
        refCount = prev | DestroySentinel;
    } while (InterlockedCompareExchange64(&wrapper->_refCount, refCount, prev) != prev);

    // The destroy sentinel represents the bit that indicates the wrapper
    // should be destroyed. Since the reference count field (64-bit) holds
    // two counters we rely on the singular sentinal value - no other bits
    // in the 64-bit counter are set. If there are outstanding bits set it
    // indicates there are still outstanding references.
    if (refCount == DestroySentinel)
    {
        // Manually trigger the destructor since placement
        // new was used to allocate the object.
        wrapper->~ManagedObjectWrapper();
        InteropLibImports::MemFree(wrapper, AllocScenario::ManagedObjectWrapper);
    }
}

ManagedObjectWrapper::ManagedObjectWrapper(
    _In_ CreateComInterfaceFlagsEx flags,
    _In_ OBJECTHANDLE objectHandle,
    _In_ int32_t runtimeDefinedCount,
    _In_ const ABI::ComInterfaceEntry* runtimeDefined,
    _In_ int32_t userDefinedCount,
    _In_ const ABI::ComInterfaceEntry* userDefined,
    _In_ ABI::ComInterfaceDispatch* dispatches)
    : Target{ nullptr }
    , _refCount{ 1 }
    , _runtimeDefinedCount{ runtimeDefinedCount }
    , _userDefinedCount{ userDefinedCount }
    , _runtimeDefined{ runtimeDefined }
    , _userDefined{ userDefined }
    , _dispatches{ dispatches }
    , _flags{ flags }
{
    bool wasSet = TrySetObjectHandle(objectHandle);
    _ASSERTE(wasSet);
}

ManagedObjectWrapper::~ManagedObjectWrapper()
{
    // If the target isn't null, then release it.
    if (Target != nullptr)
        InteropLibImports::DeleteObjectInstanceHandle(Target);
}

void* ManagedObjectWrapper::AsRuntimeDefined(_In_ REFIID riid)
{
    for (int32_t i = 0; i < _runtimeDefinedCount; ++i)
    {
        if (IsEqualGUID(_runtimeDefined[i].IID, riid))
        {
            return ABI::IndexIntoDispatchSection(i, _dispatches);
        }
    }

    return nullptr;
}

void* ManagedObjectWrapper::AsUserDefined(_In_ REFIID riid)
{
    for (int32_t i = 0; i < _userDefinedCount; ++i)
    {
        if (IsEqualGUID(_userDefined[i].IID, riid))
        {
            return ABI::IndexIntoDispatchSection(i + _runtimeDefinedCount, _dispatches);
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

bool ManagedObjectWrapper::TrySetObjectHandle(_In_ OBJECTHANDLE objectHandle, _In_ OBJECTHANDLE current)
{
    return (InterlockedCompareExchangePointer(&Target, objectHandle, current) == current);
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
        Destroy(this);

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
            TryInvokeICustomQueryInterfaceResult result = InteropLibImports::TryInvokeICustomQueryInterface(Target, riid, ppvObject);
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
                    _ASSERTE(false && "Unknown result value");
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

namespace
{
    const size_t LiveContextSentinel = 0x0a110ced;
    const size_t DeadContextSentinel = 0xdeaddead;
}

NativeObjectWrapperContext* NativeObjectWrapperContext::MapFromRuntimeContext(_In_ void* cxtMaybe)
{
    _ASSERTE(cxtMaybe != nullptr);

    // Convert the supplied context
    char* cxtRaw = reinterpret_cast<char*>(cxtMaybe);
    cxtRaw -= sizeof(NativeObjectWrapperContext);
    NativeObjectWrapperContext* cxt = reinterpret_cast<NativeObjectWrapperContext*>(cxtRaw);

#ifdef _DEBUG
    _ASSERTE(cxt->_sentinel == LiveContextSentinel);
#endif

    return cxt;
}

HRESULT NativeObjectWrapperContext::Create(
    _In_ IUnknown* external,
    _In_opt_ IUnknown* inner,
    _In_ InteropLib::Com::CreateObjectFlags flags,
    _In_ size_t runtimeContextSize,
    _Outptr_ NativeObjectWrapperContext** context)
{
    _ASSERTE(external != nullptr && context != nullptr);

    HRESULT hr;

    ComHolder<IReferenceTracker> trackerObject;
    if (flags & InteropLib::Com::CreateObjectFlags_TrackerObject)
    {
        hr = external->QueryInterface(IID_IReferenceTracker, (void**)&trackerObject);
        if (SUCCEEDED(hr))
            RETURN_IF_FAILED(TrackerObjectManager::OnIReferenceTrackerFound(trackerObject));
    }

    // Allocate memory for the RCW
    char* cxtMem = (char*)InteropLibImports::MemAlloc(sizeof(NativeObjectWrapperContext) + runtimeContextSize, AllocScenario::NativeObjectWrapper);
    if (cxtMem == nullptr)
        return E_OUTOFMEMORY;

    void* runtimeContext = cxtMem + sizeof(NativeObjectWrapperContext);

    // Contract specifically requires zeroing out runtime context.
    ::memset(runtimeContext, 0, runtimeContextSize);

    NativeObjectWrapperContext* contextLocal = new (cxtMem) NativeObjectWrapperContext{ runtimeContext, trackerObject, inner };

    if (trackerObject != nullptr)
    {
        // Inform the tracker object manager
        _ASSERTE(flags & InteropLib::Com::CreateObjectFlags_TrackerObject);
        hr = TrackerObjectManager::AfterWrapperCreated(trackerObject);
        if (FAILED(hr))
        {
            Destroy(contextLocal);
            return hr;
        }

        // Aggregation with a tracker object must be "cleaned up".
        if (flags & InteropLib::Com::CreateObjectFlags_Aggregated)
        {
            _ASSERTE(inner != nullptr);
            contextLocal->HandleReferenceTrackerAggregation();
        }
    }

    *context = contextLocal;
    return S_OK;
}

void NativeObjectWrapperContext::Destroy(_In_ NativeObjectWrapperContext* wrapper)
{
    _ASSERTE(wrapper != nullptr);

    // Manually trigger the destructor since placement
    // new was used to allocate the object.
    wrapper->~NativeObjectWrapperContext();
    InteropLibImports::MemFree(wrapper, AllocScenario::NativeObjectWrapper);
}

NativeObjectWrapperContext::NativeObjectWrapperContext(
    _In_ void* runtimeContext,
    _In_opt_ IReferenceTracker* trackerObject,
    _In_opt_ IUnknown* nativeObjectAsInner)
    : _trackerObject{ trackerObject }
    , _runtimeContext{ runtimeContext }
    , _trackerObjectDisconnected{ FALSE }
    , _trackerObjectState{ (trackerObject == nullptr ? TrackerObjectState::NotSet : TrackerObjectState::SetForRelease) }
    , _nativeObjectAsInner{ nativeObjectAsInner }
#ifdef _DEBUG
    , _sentinel{ LiveContextSentinel }
#endif
{
    if (_trackerObjectState == TrackerObjectState::SetForRelease)
        (void)_trackerObject->AddRef();
}

NativeObjectWrapperContext::~NativeObjectWrapperContext()
{
    DisconnectTracker();

    // If the inner was supplied, we need to release our reference.
    if (_nativeObjectAsInner != nullptr)
        (void)_nativeObjectAsInner->Release();

#ifdef _DEBUG
    _sentinel = DeadContextSentinel;
#endif
}

void* NativeObjectWrapperContext::GetRuntimeContext() const noexcept
{
    return _runtimeContext;
}

IReferenceTracker* NativeObjectWrapperContext::GetReferenceTracker() const noexcept
{
    return ((_trackerObjectState == TrackerObjectState::NotSet || _trackerObjectDisconnected) ? nullptr : _trackerObject);
}

// See TrackerObjectManager::AfterWrapperCreated() for AddRefFromTrackerSource() usage.
// See NativeObjectWrapperContext::HandleReferenceTrackerAggregation() for additional
// cleanup logistics.
void NativeObjectWrapperContext::DisconnectTracker() noexcept
{
    // Return if already disconnected or the tracker isn't set.
    if (FALSE != ::InterlockedCompareExchange((LONG*)&_trackerObjectDisconnected, TRUE, FALSE)
        || _trackerObjectState == TrackerObjectState::NotSet)
    {
        return;
    }

    _ASSERTE(_trackerObject != nullptr);

    // Always release the tracker source during a disconnect.
    // This to account for the implied IUnknown ownership by the runtime.
    (void)_trackerObject->ReleaseFromTrackerSource(); // IUnknown

    // Disconnect from the tracker.
    if (_trackerObjectState == TrackerObjectState::SetForRelease)
    {
        (void)_trackerObject->ReleaseFromTrackerSource(); // IReferenceTracker
        (void)_trackerObject->Release();
    }
}

void NativeObjectWrapperContext::HandleReferenceTrackerAggregation() noexcept
{
    _ASSERTE(_trackerObjectState == TrackerObjectState::SetForRelease && _trackerObject != nullptr);

    // Aggregation with an IReferenceTracker instance creates an extra AddRef()
    // on the outer (e.g. MOW) so we clean up that issue here.
    _trackerObjectState = TrackerObjectState::SetNoRelease;

    (void)_trackerObject->ReleaseFromTrackerSource(); // IReferenceTracker
    (void)_trackerObject->Release();
}
