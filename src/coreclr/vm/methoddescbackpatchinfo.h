// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "debugmacrosext.h"
#include "crossloaderallocatorhash.h"

#ifndef CROSSGEN_COMPILE

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

public:
    // To be used when the thread will remain in preemptive GC mode while holding the lock
    class ConditionalLockHolderForGCPreemp : private CrstHolderWithState
    {
    public:
        ConditionalLockHolderForGCPreemp(bool acquireLock = true)
            : CrstHolderWithState(
#ifndef DACCESS_COMPILE
                acquireLock ? &s_lock : nullptr
#else
                nullptr
#endif
                )
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;
        }

        DISABLE_COPY(ConditionalLockHolderForGCPreemp);
    };

#ifndef DACCESS_COMPILE
public:
    // To be used when the thread may enter cooperative GC mode while holding the lock. The thread enters a
    // forbid-suspend-for-debugger region along with acquiring the lock, such that it would not suspend for the debugger while
    // holding the lock, as that may otherwise cause a FuncEval to deadlock when trying to acquire the lock.
    class ConditionalLockHolderForGCCoop : private CrstAndForbidSuspendForDebuggerHolder
    {
    public:
        ConditionalLockHolderForGCCoop(bool acquireLock = true)
            : CrstAndForbidSuspendForDebuggerHolder(acquireLock ? &s_lock : nullptr)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_PREEMPTIVE;
            }
            CONTRACTL_END;
        }

        DISABLE_COPY(ConditionalLockHolderForGCCoop);
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

#endif // !CROSSGEN_COMPILE
