// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "excep.h"
#include "log.h"
#include "methoddescbackpatchinfo.h"


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// EntryPointSlots

#ifndef DACCESS_COMPILE

void EntryPointSlots::Backpatch_Locked(TADDR slot, SlotType slotType, PCODE entryPoint)
{
    WRAPPER_NO_CONTRACT;
    static_assert_no_msg(SlotType_Count <= sizeof(INT32));
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());
    _ASSERTE(slot != NULL);
    _ASSERTE(!(slot & SlotType_Mask));
    _ASSERTE(slotType >= SlotType_Normal);
    _ASSERTE(slotType < SlotType_Count);
    _ASSERTE(entryPoint != NULL);
    _ASSERTE(IS_ALIGNED((SIZE_T)slot, GetRequiredSlotAlignment(slotType)));

    switch (slotType)
    {
        case SlotType_Normal:
            *(PCODE *)slot = entryPoint;
            break;

        case SlotType_Vtable:
            *((MethodTable::VTableIndir2_t *)slot) = entryPoint;
            break;

        case SlotType_Executable:
        {
            ExecutableWriterHolder<void> slotWriterHolder((void*)slot, sizeof(PCODE*));
            *(PCODE *)slotWriterHolder.GetRW() = entryPoint;
            goto Flush;
        }

        case SlotType_ExecutableRel32:
        {
            // A rel32 may require a jump stub on some architectures, and is currently not supported
            _ASSERTE(sizeof(void *) <= 4);

            ExecutableWriterHolder<void> slotWriterHolder((void*)slot, sizeof(PCODE*));
            *(PCODE *)slotWriterHolder.GetRW() = entryPoint - ((PCODE)slot + sizeof(PCODE));
            // fall through
        }

        Flush:
            ClrFlushInstructionCache((LPCVOID)slot, sizeof(PCODE));
            break;

        default:
            UNREACHABLE();
            break;
    }
}

#endif // !DACCESS_COMPILE

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// MethodDescBackpatchInfoTracker

CrstStatic MethodDescBackpatchInfoTracker::s_lock;

#ifndef DACCESS_COMPILE

void MethodDescBackpatchInfoTracker::Backpatch_Locked(MethodDesc *pMethodDesc, PCODE entryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsLockOwnedByCurrentThread());
    _ASSERTE(pMethodDesc != nullptr);

    auto lambda = [&entryPoint](LoaderAllocator *pLoaderAllocatorOfSlot, MethodDesc *pMethodDesc, UINT_PTR slotData)
    {

        TADDR slot;
        EntryPointSlots::SlotType slotType;

        EntryPointSlots::ConvertUINT_PTRToSlotAndTypePair(slotData, &slot, &slotType);
        EntryPointSlots::Backpatch_Locked(slot, slotType, entryPoint);

        return true; // Keep walking
    };

    m_backpatchInfoHash.VisitValuesOfKey(pMethodDesc, lambda);
}

void MethodDescBackpatchInfoTracker::AddSlotAndPatch_Locked(MethodDesc *pMethodDesc, LoaderAllocator *pLoaderAllocatorOfSlot, TADDR slot, EntryPointSlots::SlotType slotType, PCODE currentEntryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsLockOwnedByCurrentThread());
    _ASSERTE(pMethodDesc != nullptr);
    _ASSERTE(pMethodDesc->MayHaveEntryPointSlotsToBackpatch());

    UINT_PTR slotData;
    slotData = EntryPointSlots::ConvertSlotAndTypePairToUINT_PTR(slot, slotType);

    m_backpatchInfoHash.Add(pMethodDesc, slotData, pLoaderAllocatorOfSlot);
    EntryPointSlots::Backpatch_Locked(slot, slotType, currentEntryPoint);
}

#endif // DACCESS_COMPILE

#ifdef _DEBUG
bool MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread()
{
    WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
    return !!s_lock.OwnedByCurrentThread();
#else
    return true;
#endif
}
#endif // _DEBUG

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
