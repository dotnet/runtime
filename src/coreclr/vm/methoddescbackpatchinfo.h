// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "debugmacrosext.h"
#include "crossloaderallocatorhash.h"


#define DISABLE_COPY(T) \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// EntryPointSlots

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
    static UINT_PTR ConvertSlotAndTypePairToUINT_PTR(TADDR slot, SlotType slotType)
    {
        slot |= (TADDR)slotType;
        return (UINT_PTR)slot;
    }

    static void ConvertUINT_PTRToSlotAndTypePair(UINT_PTR storedData, TADDR *pSlot, SlotType *pSlotType)
    {
        *pSlot = storedData;
        *pSlotType = (SlotType)(*pSlot & SlotType_Mask);
        *pSlot ^= *pSlotType;
    }

    static void Backpatch_Locked(TADDR slot, SlotType slotType, PCODE entryPoint);
#endif
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// MethodDescBackpatchInfoTracker

class MethodDescBackpatchInfoTracker
{
private:
    static CrstStatic s_lock;

    class BackpatchInfoTrackerHashTraits : public NoRemoveDefaultCrossLoaderAllocatorHashTraits<MethodDesc *, UINT_PTR>
    {
    };

    typedef CrossLoaderAllocatorHash<BackpatchInfoTrackerHashTraits> BackpatchInfoTrackerHash;

    // Contains information about slots associated with the MethodDesc that were recorded for backpatching. This field and its
    // data is protected by s_lock.
    BackpatchInfoTrackerHash m_backpatchInfoHash;

#ifndef DACCESS_COMPILE
public:
    static void StaticInitialize()
    {
        WRAPPER_NO_CONTRACT;
        s_lock.Init(CrstMethodDescBackpatchInfoTracker);
    }
#endif

    void Initialize(LoaderAllocator *pLoaderAllocator)
    {
        WRAPPER_NO_CONTRACT;
        m_backpatchInfoHash.Init(pLoaderAllocator);
    }

#ifdef _DEBUG
public:
    static bool IsLockOwnedByCurrentThread();
#endif

#ifndef DACCESS_COMPILE
public:
    class ConditionalLockHolder : private CrstHolderWithState
    {
    #ifdef ENABLE_CONTRACTS_IMPL
    private:
        GCNoTrigger m_gcNoTriggerHolder;
    #endif

    public:
        // Acquire the lock and put the thread in a GC_NOTRIGGER scope to ensure that this lock cannot be held while the thread
        // suspends for the debugger, otherwise FuncEvals that run may deadlock on acquiring this lock
        ConditionalLockHolder(bool acquireLock = true)
            : CrstHolderWithState(acquireLock ? &s_lock : nullptr)
        #ifdef ENABLE_CONTRACTS_IMPL
            , m_gcNoTriggerHolder(acquireLock, __FUNCTION__, __FILE__, __LINE__)
        #endif
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;
        }

        DISABLE_COPY(ConditionalLockHolder);
    };
#endif

public:
    MethodDescBackpatchInfoTracker()
    {
        LIMITED_METHOD_CONTRACT;
    }

#ifndef DACCESS_COMPILE
public:
    void Backpatch_Locked(MethodDesc *pMethodDesc, PCODE entryPoint);
    void AddSlotAndPatch_Locked(MethodDesc *pMethodDesc, LoaderAllocator *pLoaderAllocatorOfSlot, TADDR slot, EntryPointSlots::SlotType slotType, PCODE currentEntryPoint);
#endif

    DISABLE_COPY(MethodDescBackpatchInfoTracker);
};

#undef DISABLE_COPY

