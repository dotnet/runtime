// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <interoplibimports.h>
#include "comwrappers.h"

namespace ABI
{
    //---------------------------------------------------------------------------------
    // Dispatch section of the ManagedObjectWrapper (MOW)
    //
    // Within the dispatch section, the ManagedObjectWrapper itself is inserted at all 16 byte
    // aligned location. This allows the simple masking of the any ComInterfaceDispatch* to get
    // access to the ManagedObjectWrapper by masking the lower 4 bits. Below is a sketch of how
    // the dispatch section would appear in a 32-bit process.
    //
    //           16 byte aligned                            Vtable
    //           +-----------+
    //           | MOW this |
    //           +-----------+                              +-----+
    //  COM IP-->| VTable ptr|----------------------------->|slot1|
    //           +-----------+           +-----+            +-----+
    //           | VTable ptr|---------->|slot1|            |slot2|
    //           +-----------+           +-----+            +     +
    //           | VTable ptr|           | ....|            | ... |
    //           +-----------+           +     +            +     +
    //           | MOW  this |           |slotN|            |slotN|
    //           +           +           +-----+            +-----+
    //           |  ....     |
    //           +-----------+
    //
    // A 16 byte alignment permits a ratio of 3:1 COM vtables to ManagedObjectWrapper 'this'
    // pointers in 32-bit process, but in 64-bit process the mapping is unfortunately only 1:1.
    // See the dispatch section building API below for an example of how indexing works.
    //--------------------------------------------------------------------------------

    struct ComInterfaceDispatch
    {
        const void* vtable;
    };
    ABI_ASSERT(sizeof(ComInterfaceDispatch) == sizeof(void*));

    const size_t DispatchAlignmentThisPtr = 16; // Should be a power of 2.
    const intptr_t DispatchThisPtrMask = ~(DispatchAlignmentThisPtr - 1);
    ABI_ASSERT(sizeof(void*) < DispatchAlignmentThisPtr);

    const intptr_t AlignmentThisPtrMaxPadding = DispatchAlignmentThisPtr - sizeof(void*);
    const size_t EntriesPerThisPtr = (DispatchAlignmentThisPtr / sizeof(void*)) - 1;

    // Check if the instance can dispatch according to the ABI.
    bool IsAbleToDispatch(_In_ ComInterfaceDispatch* disp)
    {
        return (reinterpret_cast<intptr_t>(disp) & DispatchThisPtrMask) != 0;
    }

    // Given the number of dispatch entries compute the needed number of 'this' pointer entries.
    constexpr size_t ComputeThisPtrForDispatchSection(_In_ size_t dispatchCount)
    {
        return (dispatchCount / ABI::EntriesPerThisPtr) + ((dispatchCount % ABI::EntriesPerThisPtr) == 0 ? 0 : 1);
    }

    // Given a pointer and a padding allowance, attempt to find an offset into
    // the memory that is properly aligned for the dispatch section.
    char* AlignDispatchSection(_In_ char* section, _In_ intptr_t extraPadding)
    {
        _ASSERTE(section != nullptr);

        // If the dispatch section is not properly aligned by default, we use
        // utilize the padding to make sure the dispatch section can be aligned.
        while ((reinterpret_cast<intptr_t>(section) % ABI::DispatchAlignmentThisPtr) != 0)
        {
            // Check if there is padding to attempt an alignment
            if (extraPadding <= 0)
            {
                std::abort(); // [TODO] Replace
            }

            extraPadding -= sizeof(void*);

#ifdef _DEBUG
            // Poison unused sections of the section
            std::memset(section, 0xff, sizeof(void*));
#endif

            section += sizeof(void*);
        }

        return section;
    }

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
        // Define dispatch section iterator
        const void** currDisp = reinterpret_cast<const void**>(dispatchSection);

        // Keep rolling count of dispatch entries
        int32_t dispCount = 0;

        // Iterate over all interface entry sets
        const EntrySet* curr = entrySets;
        const EntrySet* end = entrySets + entrySetCount;
        for (; curr != end; ++curr)
        {
            const ComInterfaceEntry* currEntry = curr->start;
            int32_t entryCount = curr->count;

            // Update dispatch section with 'this' pointer and vtables
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

    // Given the entry index, compute the dispatch index
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

namespace
{
    HRESULT STDMETHODCALLTYPE ManagedObjectWrapper_QueryInterface(
        _In_ ABI::ComInterfaceDispatch* disp,
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
    {
        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        return wrapper->QueryInterface(riid, ppvObject);
    }

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

    // Hard-coded ManagedObjectWrapper IUnknown vtable
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

namespace InteropLib
{
    void GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease)
    {
        _ASSERTE(fpQueryInterface != nullptr
                && fpAddRef != nullptr
                && fpRelease != nullptr);

        *fpQueryInterface = ManagedObjectWrapper_IUnknownImpl.QueryInterface;
        *fpAddRef = ManagedObjectWrapper_IUnknownImpl.AddRef;
        *fpRelease = ManagedObjectWrapper_IUnknownImpl.Release;
    }
}

namespace
{
    const int32_t TrackerRefShift = 32;
    constexpr ULONGLONG TrackerRefCounter = ULONGLONG{ 1 } << TrackerRefShift;

    constexpr ULONG GetTrackerCount(_In_ ULONGLONG c)
    {
        return static_cast<ULONG>(c >> TrackerRefShift);
    }

    ULONG STDMETHODCALLTYPE TrackerTarget_AddRefFromReferenceTracker(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlags::TrackerSupport));

        return wrapper->AddRefFromReferenceTracker();
    }

    ULONG STDMETHODCALLTYPE TrackerTarget_ReleaseFromReferenceTracker(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlags::TrackerSupport));

        return wrapper->ReleaseFromReferenceTracker();
    }

    HRESULT STDMETHODCALLTYPE TrackerTarget_Peg(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlags::TrackerSupport));

        return wrapper->Peg();
    }

    HRESULT STDMETHODCALLTYPE TrackerTarget_Unpeg(_In_ ABI::ComInterfaceDispatch* disp)
    {
        _ASSERTE(disp != nullptr && disp->vtable != nullptr);

        ManagedObjectWrapper* wrapper = ABI::ToManagedObjectWrapper(disp);
        _ASSERTE(wrapper->IsSet(CreateComInterfaceFlags::TrackerSupport));

        return wrapper->Unpeg();
    }

    // Hard-coded IReferenceTrackerTarget vtable
    const struct
    {
        decltype(ManagedObjectWrapper_IUnknownImpl) IUnknownImpl;
        decltype(&TrackerTarget_AddRefFromReferenceTracker) AddRefFromReferenceTracker;
        decltype(&TrackerTarget_ReleaseFromReferenceTracker) ReleaseFromReferenceTracker;
        decltype(&TrackerTarget_Peg) Peg;
        decltype(&TrackerTarget_Unpeg) Unpeg;
    } ManagedObjectWrapper_IReferenceTrackerTargetImpl {
        ManagedObjectWrapper_IUnknownImpl,
        &TrackerTarget_AddRefFromReferenceTracker,
        &TrackerTarget_ReleaseFromReferenceTracker,
        &TrackerTarget_Peg,
        &TrackerTarget_Unpeg
    };

    static_assert(sizeof(ManagedObjectWrapper_IReferenceTrackerTargetImpl) == (7 * sizeof(void*)), "Unexpected vtable size");
}

ManagedObjectWrapper* ManagedObjectWrapper::MapIUnknownToWrapper(_In_ IUnknown* pUnk)
{
    _ASSERTE(pUnk != nullptr);

    // If the first Vtable entry is part of the ManagedObjectWrapper IUnknown impl,
    // we know how to interpret the IUnknown.
    void* firstEntryInVtable = *reinterpret_cast<void**>(pUnk);
    if (firstEntryInVtable == ManagedObjectWrapper_IUnknownImpl.QueryInterface)
        return nullptr;

    ABI::ComInterfaceDispatch* disp = reinterpret_cast<ABI::ComInterfaceDispatch*>(pUnk);
    return ABI::ToManagedObjectWrapper(disp);
}

ManagedObjectWrapper* ManagedObjectWrapper::Create(
    _In_ CreateComInterfaceFlags flags,
    _In_ void* gcHandleToObject,
    _In_ int32_t userDefinedCount,
    _In_ ComInterfaceEntry* userDefined)
{
    // Maximum number of runtime supplied vtables
    ComInterfaceEntry runtimeDefined[4];
    int32_t runtimeDefinedCount = 0;

    // Check if the caller will provide the IUnknown table
    if ((flags & CreateComInterfaceFlags::CallerDefinedIUnknown) == CreateComInterfaceFlags::None)
    {
        ComInterfaceEntry& curr = runtimeDefined[runtimeDefinedCount++];
        curr.IID = __uuidof(IUnknown);
        curr.Vtable = &ManagedObjectWrapper_IUnknownImpl;
    }

    // Check if the caller wants tracker support
    if ((flags & CreateComInterfaceFlags::TrackerSupport) == CreateComInterfaceFlags::TrackerSupport)
    {
        ComInterfaceEntry& curr = runtimeDefined[runtimeDefinedCount++];
        curr.IID = __uuidof(IReferenceTrackerTarget);
        curr.Vtable = &ManagedObjectWrapper_IReferenceTrackerTargetImpl;
    }

    _ASSERTE(runtimeDefinedCount <= ARRAYSIZE(runtimeDefined));

    // Compute size for ManagedObjectWrapper instance
    const size_t totalRuntimeDefinedSize = runtimeDefinedCount * sizeof(ComInterfaceEntry);
    const size_t totalDefinedCount = static_cast<size_t>(runtimeDefinedCount) + userDefinedCount;

    // Compute the total entry size of dispatch section
    const size_t totalDispatchSectionCount = ABI::ComputeThisPtrForDispatchSection(totalDefinedCount) + totalDefinedCount;
    const size_t totalDispatchSectionSize = totalDispatchSectionCount * sizeof(void*);

    // Allocate memory for the ManagedObjectWrapper
    char* wrapperMem = (char*)0;// rt::Alloc(sizeof(ManagedObjectWrapper) + totalRuntimeDefinedSize + totalDispatchSectionSize + ABI::AlignmentThisPtrMaxPadding); [TODO]

    // Compute Runtime defined offset
    char* runtimeDefinedOffset = wrapperMem + sizeof(ManagedObjectWrapper);

    // Copy in runtime supplied COM interface entries
    if (0 < runtimeDefinedCount)
        std::memcpy(runtimeDefinedOffset, runtimeDefined, totalRuntimeDefinedSize);

    // Compute the dispatch section offset and ensure it is aligned
    char* dispatchSectionOffset = runtimeDefinedOffset + totalRuntimeDefinedSize;
    dispatchSectionOffset = ABI::AlignDispatchSection(dispatchSectionOffset, ABI::AlignmentThisPtrMaxPadding);

    // Define the sets for the tables to insert
    const ABI::EntrySet AllEntries[] =
    {
        { runtimeDefined, runtimeDefinedCount },
        { userDefined, userDefinedCount }
    };

    ABI::ComInterfaceDispatch* dispSection = ABI::PopulateDispatchSection(wrapperMem, dispatchSectionOffset, ARRAYSIZE(AllEntries), AllEntries);

    ManagedObjectWrapper* wrappers = new (wrapperMem) ManagedObjectWrapper
        {
            flags,
            gcHandleToObject,
            runtimeDefinedCount,
            runtimeDefined,
            userDefinedCount,
            userDefined,
            dispSection
        };

    return wrappers;
}

ManagedObjectWrapper::ManagedObjectWrapper(
    _In_ CreateComInterfaceFlags flags,
    _In_ void* gcHandleToObject,
    _In_ int32_t runtimeDefinedCount,
    _In_ const ComInterfaceEntry* runtimeDefined,
    _In_ int32_t userDefinedCount,
    _In_ const ComInterfaceEntry* userDefined,
    _In_ ABI::ComInterfaceDispatch* dispatches)
    : Target{ gcHandleToObject }
    , _runtimeDefinedCount{ runtimeDefinedCount }
    , _userDefinedCount{ userDefinedCount }
    , _runtimeDefined{ runtimeDefined }
    , _userDefined{ userDefined }
    , _flags{ flags }
    , _dispatches{ dispatches }
{ }

ManagedObjectWrapper::~ManagedObjectWrapper()
{
    // rt::ReleaseGCHandle(Target); [TODO]
}

void* ManagedObjectWrapper::As(_In_ REFIID riid)
{
    // Find target interface and return dispatcher or null if not found.
    for (int32_t i = 0; i < _runtimeDefinedCount; ++i)
    {
        if (IsEqualGUID(_runtimeDefined[i].IID, riid))
        {
            return ABI::IndexIntoDispatchSection(i, _dispatches);
        }
    }

    for (int32_t i = 0; i < _userDefinedCount; ++i)
    {
        if (IsEqualGUID(_userDefined[i].IID, riid))
        {
            return ABI::IndexIntoDispatchSection(i + _runtimeDefinedCount, _dispatches);
        }
    }

    return nullptr;
}

void* ManagedObjectWrapper::GetObjectGCHandle() const
{
    return Target;
}

bool ManagedObjectWrapper::IsAlive() const
{
    return true; // rt::IsGCHandleLive(GetObjectGCHandle()); [TODO]
}

bool ManagedObjectWrapper::IsSet(_In_ CreateComInterfaceFlags flag) const
{
    return (_flags & flag) != CreateComInterfaceFlags::None;
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
    LONGLONG prev;
    LONGLONG curr;
    do
    {
        prev = _refCount;
        curr = prev - TrackerRefCounter;
    } while (::InterlockedCompareExchange64(&_refCount, curr, prev) != prev);

    return GetTrackerCount(curr);
}

HRESULT ManagedObjectWrapper::Peg()
{
    return E_NOTIMPL;
}

HRESULT ManagedObjectWrapper::Unpeg()
{
    return E_NOTIMPL;
}

HRESULT ManagedObjectWrapper::QueryInterface(
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
{
    if (ppvObject == nullptr)
        return E_POINTER;

    // Find target interface
    *ppvObject = As(riid);
    if (*ppvObject == nullptr)
        return E_NOINTERFACE;

    (void)AddRef();
    return S_OK;
}

ULONG ManagedObjectWrapper::AddRef(void)
{
    return (ULONG)::InterlockedIncrement64(&_refCount);
}

ULONG ManagedObjectWrapper::Release(void)
{
    ULONG refCount = (ULONG)::InterlockedDecrement64(&_refCount);
    if (refCount == 0)
    {
        // Manually trigger the destructor since placement
        // new was used to allocate object.
        this->~ManagedObjectWrapper();
        // rt::Free(this); [TODO]
    }

    return refCount;
}