// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

#include "excep.h"
#include "log.h"
#include "methoddescbackpatchinfo.h"

#ifdef CROSSGEN_COMPILE
    #error This file is not expected to be included into CrossGen
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// EntryPointSlots

#ifndef DACCESS_COMPILE

void EntryPointSlots::Backpatch_Locked(PCODE entryPoint)
{
    WRAPPER_NO_CONTRACT;
    static_assert_no_msg(SlotType_Count <= sizeof(INT32));
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockedByCurrentThread());
    _ASSERTE(entryPoint != NULL);

    TADDR *slots = m_slots.GetElements();
    COUNT_T slotCount = m_slots.GetCount();
    for (COUNT_T i = 0; i < slotCount; ++i)
    {
        TADDR slot = slots[i];
        SlotType slotType = (SlotType)(slot & SlotType_Mask);
        slot ^= slotType;
        Backpatch_Locked(slot, slotType, entryPoint);
    }
}

void EntryPointSlots::Backpatch_Locked(TADDR slot, SlotType slotType, PCODE entryPoint)
{
    WRAPPER_NO_CONTRACT;
    static_assert_no_msg(SlotType_Count <= sizeof(INT32));
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockedByCurrentThread());
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
            ((MethodTable::VTableIndir2_t *)slot)->SetValue(entryPoint);
            break;

        case SlotType_Executable:
            *(PCODE *)slot = entryPoint;
            goto Flush;

        case SlotType_ExecutableRel32:
            // A rel32 may require a jump stub on some architectures, and is currently not supported
            _ASSERTE(sizeof(void *) <= 4);

            *(PCODE *)slot = entryPoint - ((PCODE)slot + sizeof(PCODE));
            // fall through

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
// MethodDescBackpatchInfo

#ifndef DACCESS_COMPILE

void MethodDescBackpatchInfo::AddDependentLoaderAllocator_Locked(LoaderAllocator *dependentLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockedByCurrentThread());
    _ASSERTE(m_methodDesc != nullptr);
    _ASSERTE(dependentLoaderAllocator != nullptr);
    _ASSERTE(dependentLoaderAllocator != m_methodDesc->GetLoaderAllocator());

    LoaderAllocatorSet *set = m_dependentLoaderAllocators;
    if (set != nullptr)
    {
        if (set->Lookup(dependentLoaderAllocator) != nullptr)
        {
            return;
        }
        set->Add(dependentLoaderAllocator);
        return;
    }

    NewHolder<LoaderAllocatorSet> setHolder = new LoaderAllocatorSet();
    setHolder->Add(dependentLoaderAllocator);
    m_dependentLoaderAllocators = setHolder.Extract();
}

void MethodDescBackpatchInfo::RemoveDependentLoaderAllocator_Locked(LoaderAllocator *dependentLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockedByCurrentThread());
    _ASSERTE(m_methodDesc != nullptr);
    _ASSERTE(dependentLoaderAllocator != nullptr);
    _ASSERTE(dependentLoaderAllocator != m_methodDesc->GetLoaderAllocator());
    _ASSERTE(m_dependentLoaderAllocators != nullptr);
    _ASSERTE(m_dependentLoaderAllocators->Lookup(dependentLoaderAllocator) == dependentLoaderAllocator);

    m_dependentLoaderAllocators->Remove(dependentLoaderAllocator);
}

#endif // !DACCESS_COMPILE

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// MethodDescBackpatchInfoTracker

CrstStatic MethodDescBackpatchInfoTracker::s_lock;

#ifndef DACCESS_COMPILE

void MethodDescBackpatchInfoTracker::StaticInitialize()
{
    WRAPPER_NO_CONTRACT;
    s_lock.Init(CrstMethodDescBackpatchInfoTracker);
}

#endif // DACCESS_COMPILE

#ifdef _DEBUG

bool MethodDescBackpatchInfoTracker::IsLockedByCurrentThread()
{
    WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
    return !!s_lock.OwnedByCurrentThread();
#else
    return true;
#endif
}

bool MethodDescBackpatchInfoTracker::MayHaveEntryPointSlotsToBackpatch(PTR_MethodDesc methodDesc)
{
    // The only purpose of this method is to allow asserts in inline functions defined in the .h file, by which time MethodDesc
    // is not fully defined

    WRAPPER_NO_CONTRACT;
    return methodDesc->MayHaveEntryPointSlotsToBackpatch();
}

#endif // _DEBUG

#ifndef DACCESS_COMPILE

MethodDescBackpatchInfo *MethodDescBackpatchInfoTracker::AddBackpatchInfo_Locked(MethodDesc *methodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsLockedByCurrentThread());
    _ASSERTE(methodDesc != nullptr);
    _ASSERTE(methodDesc->MayHaveEntryPointSlotsToBackpatch());
    _ASSERTE(m_backpatchInfoHash.Lookup(methodDesc) == nullptr);

    NewHolder<MethodDescBackpatchInfo> backpatchInfoHolder = new MethodDescBackpatchInfo(methodDesc);
    m_backpatchInfoHash.Add(backpatchInfoHolder);
    return backpatchInfoHolder.Extract();
}

EntryPointSlots *MethodDescBackpatchInfoTracker::GetDependencyMethodDescEntryPointSlots_Locked(MethodDesc *methodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsLockedByCurrentThread());
    _ASSERTE(methodDesc != nullptr);
    _ASSERTE(methodDesc->MayHaveEntryPointSlotsToBackpatch());

    MethodDescEntryPointSlots *methodDescSlots =
        m_dependencyMethodDescEntryPointSlotsHash.Lookup(methodDesc);
    return methodDescSlots == nullptr ? nullptr : methodDescSlots->GetSlots();
}

EntryPointSlots *MethodDescBackpatchInfoTracker::GetOrAddDependencyMethodDescEntryPointSlots_Locked(MethodDesc *methodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(IsLockedByCurrentThread());
    _ASSERTE(methodDesc != nullptr);
    _ASSERTE(methodDesc->MayHaveEntryPointSlotsToBackpatch());

    MethodDescEntryPointSlots *methodDescSlots = m_dependencyMethodDescEntryPointSlotsHash.Lookup(methodDesc);
    if (methodDescSlots != nullptr)
    {
        return methodDescSlots->GetSlots();
    }

    NewHolder<MethodDescEntryPointSlots> methodDescSlotsHolder = new MethodDescEntryPointSlots(methodDesc);
    m_dependencyMethodDescEntryPointSlotsHash.Add(methodDescSlotsHolder);
    return methodDescSlotsHolder.Extract()->GetSlots();
}

void MethodDescBackpatchInfoTracker::ClearDependencyMethodDescEntryPointSlots(LoaderAllocator *loaderAllocator)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(loaderAllocator != nullptr);
    _ASSERTE(loaderAllocator->GetMethodDescBackpatchInfoTracker() == this);

    ConditionalLockHolder lockHolder;

    for (MethodDescEntryPointSlotsHash::Iterator
            it = m_dependencyMethodDescEntryPointSlotsHash.Begin(),
            itEnd = m_dependencyMethodDescEntryPointSlotsHash.End();
        it != itEnd;
        ++it)
    {
        MethodDesc *methodDesc = (*it)->GetMethodDesc();
        MethodDescBackpatchInfo *backpatchInfo = methodDesc->GetBackpatchInfoTracker()->GetBackpatchInfo_Locked(methodDesc);
        if (backpatchInfo != nullptr)
        {
            backpatchInfo->RemoveDependentLoaderAllocator_Locked(loaderAllocator);
        }
    }

    m_dependencyMethodDescEntryPointSlotsHash.RemoveAll();
}

#endif // DACCESS_COMPILE

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
