// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "debugmacrosext.h"

// MethodDescBackpatchInfoTracker:
//   - Root container for all other types in this file
//   - There is one instance per LoaderAllocator
//   - Contains a collection of MethodDescBackpatchInfo objects
//   - Contains a collection of MethodDescEntryPointSlots objects
//
// MethodDescBackpatchInfo:
//   - Container for backpatch information for a MethodDesc allocated in the same LoaderAllocator
//   - Contains an EntryPointSlots collection that contains slots allocated in the same LoaderAllocator. These are slots
//     recorded for backpatching when the MethodDesc's code entry point changes.
//   - Contains a LoaderAllocatorSet collection that contains dependent LoaderAllocators that in turn have slots recorded for
//     backpatching when the MethodDesc's entry point changes. These are slots associated with the MethodDesc but allocated and
//     recorded in a LoaderAllocator on the MethodDesc's LoaderAllocator.
//
// EntryPointSlots and MethodDescEntryPointSlots
//   - Collection of slots recorded for backpatching
//   - There is one instance per MethodDescBackpatchInfo for slots allocated in the MethodDesc's LoaderAllocator
//   - There is one instance per MethodDesc in MethodDescBackpatchInfoTracker, for slots allocated in LoaderAllocators that are 
//     dependent on the MethodDesc's LoaderAllocator. The dependent LoaderAllocators are also recorded in the
//     MethodDescBackPatchInfo associated with the MethodDesc's LoaderAllocator.

typedef SHash<PtrSetSHashTraits<LoaderAllocator *>> LoaderAllocatorSet;

#ifndef CROSSGEN_COMPILE

#define DISABLE_COPY(T) \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// EntryPointSlots

// See comment at the top of methoddescbackpatchinfo.h for a description of this and related data structures
class EntryPointSlots
{
public:
    enum SlotType : UINT8
    {
        SlotType_Normal, // pointer-sized value not in executable code
        SlotType_Vtable, // pointer-sized value not in executable code, may be relative based on MethodTable::VTableIndir2_t
        SlotType_Executable, // pointer-sized value in executable code
        SlotType_ExecutableRel32, // 32-bit value relative to the end of the slot, in executable code

        SlotType_Count,
        SlotType_Mask = SlotType_Vtable | SlotType_Executable | SlotType_ExecutableRel32
    };

private:
    typedef SArray<TADDR> SlotArray;

private:
    SlotArray m_slots;

public:
    EntryPointSlots()
    {
        LIMITED_METHOD_CONTRACT;
    }

#ifndef DACCESS_COMPILE
private:
    static SIZE_T GetRequiredSlotAlignment(SlotType slotType)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(slotType >= SlotType_Normal);
        _ASSERTE(slotType < SlotType_Count);

        return slotType == SlotType_ExecutableRel32 ? sizeof(INT32) : sizeof(void *);
    }

public:
    void AddSlot_Locked(TADDR slot, SlotType slotType);
    void Backpatch_Locked(PCODE entryPoint);
    static void Backpatch_Locked(TADDR slot, SlotType slotType, PCODE entryPoint);
#endif

    DISABLE_COPY(EntryPointSlots);
};

// See comment at the top of methoddescbackpatchinfo.h for a description of this and related data structures
class MethodDescEntryPointSlots
{
private:
    MethodDesc *m_methodDesc;

    // This field and its data is protected by MethodDescBackpatchInfoTracker's lock
    EntryPointSlots m_slots;

public:
    MethodDescEntryPointSlots(MethodDesc *methodDesc) : m_methodDesc(methodDesc)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(methodDesc != nullptr);
    }

public:
    MethodDesc *GetMethodDesc() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_methodDesc;
    }

#ifndef DACCESS_COMPILE
    EntryPointSlots *GetSlots()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_methodDesc != nullptr);

        return &m_slots;
    }
#endif

    DISABLE_COPY(MethodDescEntryPointSlots);
};

class MethodDescEntryPointSlotsHashTraits
    : public DeleteElementsOnDestructSHashTraits<NoRemoveSHashTraits<DefaultSHashTraits<MethodDescEntryPointSlots *>>>
{
public:
    typedef DeleteElementsOnDestructSHashTraits<NoRemoveSHashTraits<DefaultSHashTraits<MethodDescEntryPointSlots *>>> Base;
    typedef Base::element_t element_t;
    typedef Base::count_t count_t;

    typedef MethodDesc *key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e->GetMethodDesc();
    }

    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }

    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)((size_t)dac_cast<TADDR>(k) >> 2);
    }

    static const element_t Null() { LIMITED_METHOD_CONTRACT; return nullptr; }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == nullptr; }
};

typedef SHash<MethodDescEntryPointSlotsHashTraits> MethodDescEntryPointSlotsHash;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// MethodDescBackpatchInfo

// See comment at the top of methoddescbackpatchinfo.h for a description of this and related data structures
class MethodDescBackpatchInfo
{
private:
    MethodDesc *m_methodDesc;

    // Entry point slots that need to be backpatched when the method's entry point changes. This may include vtable slots, slots
    // from virtual stub dispatch for interface methods (slots from dispatch stubs and resolve cache entries), etc. This
    // collection only contains slots allocated in this MethodDesc's LoaderAllocator. This field and its data is protected by
    // MethodDescBackpatchInfoTracker's lock.
    EntryPointSlots m_slots;

    // A set of LoaderAllocators from which slots that were allocated, are associated with the dependency MethodDesc and have
    // been recorded for backpatching. For example, a derived type in a shorter-lifetime LoaderAllocator that inherits a
    // MethodDesc from a longer-lifetime base type, would have its slot recorded in the slot's LoaderAllocator, and that
    // LoaderAllocator would be recorded here in the MethodDesc's LoaderAllocator. This field and its data is protected by
    // MethodDescBackpatchInfoTracker's lock.
    LoaderAllocatorSet *m_dependentLoaderAllocators;

public:
    MethodDescBackpatchInfo(MethodDesc *methodDesc = nullptr);

#ifndef DACCESS_COMPILE
public:
    ~MethodDescBackpatchInfo()
    {
        LIMITED_METHOD_CONTRACT;

        LoaderAllocatorSet *set = m_dependentLoaderAllocators;
        if (set != nullptr)
        {
            delete set;
        }
    }
#endif

public:
    MethodDesc *GetMethodDesc() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_methodDesc;
    }

#ifndef DACCESS_COMPILE
public:
    EntryPointSlots *GetSlots()
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(m_methodDesc != nullptr);

        return &m_slots;
    }

public:
    template<class Visit> void ForEachDependentLoaderAllocator_Locked(Visit visit);
    void AddDependentLoaderAllocator_Locked(LoaderAllocator *dependentLoaderAllocator);
    void RemoveDependentLoaderAllocator_Locked(LoaderAllocator *dependentLoaderAllocator);
#endif

    DISABLE_COPY(MethodDescBackpatchInfo);
};

class MethodDescBackpatchInfoHashTraits
    : public DeleteElementsOnDestructSHashTraits<NoRemoveSHashTraits<DefaultSHashTraits<MethodDescBackpatchInfo *>>>
{
public:
    typedef DeleteElementsOnDestructSHashTraits<NoRemoveSHashTraits<DefaultSHashTraits<MethodDescBackpatchInfo *>>> Base;
    typedef Base::element_t element_t;
    typedef Base::count_t count_t;

    typedef MethodDesc *key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e->GetMethodDesc();
    }

    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }

    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)((size_t)dac_cast<TADDR>(k) >> 2);
    }

    static const element_t Null() { LIMITED_METHOD_CONTRACT; return nullptr; }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == nullptr; }
};

typedef SHash<MethodDescBackpatchInfoHashTraits> MethodDescBackpatchInfoHash;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// MethodDescBackpatchInfoTracker

// See comment at the top of methoddescbackpatchinfo.h for a description of this and related data structures
class MethodDescBackpatchInfoTracker
{
private:
    static CrstStatic s_lock;

    // Contains information about slots associated with the MethodDesc that were recorded for backpatching. This field and its
    // data is protected by s_lock.
    MethodDescBackpatchInfoHash m_backpatchInfoHash;

    // Contains slots associated with a MethodDesc from a dependency LoaderAllocator, which are recorded for backpatching when
    // the MethodDesc's entry point changes. This field and its data is protected by s_lock.
    MethodDescEntryPointSlotsHash m_dependencyMethodDescEntryPointSlotsHash;

#ifndef DACCESS_COMPILE
public:
    static void StaticInitialize();
#endif

#ifdef _DEBUG
public:
    static bool IsLockedByCurrentThread();
#endif

public:
    class ConditionalLockHolder : CrstHolderWithState
    {
    public:
        ConditionalLockHolder(bool acquireLock = true)
            : CrstHolderWithState(
#ifndef DACCESS_COMPILE
                acquireLock ? &MethodDescBackpatchInfoTracker::s_lock : nullptr
#else
                nullptr
#endif
                )
        {
            LIMITED_METHOD_CONTRACT;
        }
    };

public:
    MethodDescBackpatchInfoTracker()
    {
        LIMITED_METHOD_CONTRACT;
    }

#ifdef _DEBUG
public:
    static bool MayHaveEntryPointSlotsToBackpatch(PTR_MethodDesc methodDesc);
#endif

#ifndef DACCESS_COMPILE
public:
    MethodDescBackpatchInfo *GetBackpatchInfo_Locked(MethodDesc *methodDesc) const
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(IsLockedByCurrentThread());
        _ASSERTE(methodDesc != nullptr);
        _ASSERTE(MayHaveEntryPointSlotsToBackpatch(methodDesc));

        return m_backpatchInfoHash.Lookup(methodDesc);
    }

    MethodDescBackpatchInfo *GetOrAddBackpatchInfo_Locked(MethodDesc *methodDesc)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(IsLockedByCurrentThread());
        _ASSERTE(methodDesc != nullptr);
        _ASSERTE(MayHaveEntryPointSlotsToBackpatch(methodDesc));

        MethodDescBackpatchInfo *backpatchInfo = m_backpatchInfoHash.Lookup(methodDesc);
        if (backpatchInfo != nullptr)
        {
            return backpatchInfo;
        }
        return AddBackpatchInfo_Locked(methodDesc);
    }

private:
    MethodDescBackpatchInfo *AddBackpatchInfo_Locked(MethodDesc *methodDesc);

public:
    bool HasDependencyMethodDescEntryPointSlots() const
    {
        WRAPPER_NO_CONTRACT;
        return m_dependencyMethodDescEntryPointSlotsHash.GetCount() != 0;
    }

    EntryPointSlots *GetDependencyMethodDescEntryPointSlots_Locked(MethodDesc *methodDesc);
    EntryPointSlots *GetOrAddDependencyMethodDescEntryPointSlots_Locked(MethodDesc *methodDesc);
    void ClearDependencyMethodDescEntryPointSlots(LoaderAllocator *loaderAllocator);
#endif

    friend class ConditionalLockHolder;

    DISABLE_COPY(MethodDescBackpatchInfoTracker);
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Inline and template definitions

#ifndef DACCESS_COMPILE

inline void EntryPointSlots::AddSlot_Locked(TADDR slot, SlotType slotType)
{
    WRAPPER_NO_CONTRACT;
    static_assert_no_msg(SlotType_Count <= sizeof(INT32));
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockedByCurrentThread());
    _ASSERTE(slot != NULL);
    _ASSERTE(!(slot & SlotType_Mask));
    _ASSERTE(slotType >= SlotType_Normal);
    _ASSERTE(slotType < SlotType_Count);
    _ASSERTE(IS_ALIGNED((SIZE_T)slot, GetRequiredSlotAlignment(slotType)));

    m_slots.Append(slot | slotType);
}

#endif // DACCESS_COMPILE

inline MethodDescBackpatchInfo::MethodDescBackpatchInfo(MethodDesc *methodDesc)
    : m_methodDesc(methodDesc), m_dependentLoaderAllocators(nullptr)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(
        methodDesc == nullptr ||
        MethodDescBackpatchInfoTracker::MayHaveEntryPointSlotsToBackpatch(PTR_MethodDesc(methodDesc)));
}

#ifndef DACCESS_COMPILE

template<class Visit>
inline void MethodDescBackpatchInfo::ForEachDependentLoaderAllocator_Locked(Visit visit)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockedByCurrentThread());
    _ASSERTE(m_methodDesc != nullptr);

    LoaderAllocatorSet *set = m_dependentLoaderAllocators;
    if (set == nullptr)
    {
        return;
    }

    for (LoaderAllocatorSet::Iterator it = set->Begin(), itEnd = set->End(); it != itEnd; ++it)
    {
        visit(*it);
    }
}

#endif // DACCESS_COMPILE

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#undef DISABLE_COPY

#endif // !CROSSGEN_COMPILE
