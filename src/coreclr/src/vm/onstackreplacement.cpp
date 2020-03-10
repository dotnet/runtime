// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: onstackreplacement.cpp
//
// ===========================================================================

#include "common.h"
#include "onstackreplacement.h"

#ifdef FEATURE_ON_STACK_REPLACEMENT

int OnStackReplacementManager::s_patchpointId = 0;
CrstStatic OnStackReplacementManager::s_lock;

#ifndef DACCESS_COMPILE

void OnStackReplacementManager::StaticInitialize()
{
    WRAPPER_NO_CONTRACT;
    s_lock.Init(CrstJitPatchpoint, CrstFlags(CRST_UNSAFE_COOPGC));
}

OnStackReplacementManager::OnStackReplacementManager() : m_jitPatchpointTable()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    LockOwner lock = {&s_lock, IsOwnerOfCrst};
    m_jitPatchpointTable.Init(INITIAL_TABLE_SIZE, &lock);
}

// Fetch or create patchpoint info for this patchpoint.
PerPatchpointInfo* OnStackReplacementManager::GetPerPatchpointInfo(PCODE ip)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    PTR_PCODE ppId = dac_cast<PTR_PCODE>(ip);
    PTR_PerPatchpointInfo ppInfo = NULL;

    BOOL hasData = m_jitPatchpointTable.GetValueSpeculative(ppId, (HashDatum*)&ppInfo);

    if (!hasData)
    {
        CrstHolder lock(&s_lock);
        hasData = m_jitPatchpointTable.GetValue(ppId, (HashDatum*)&ppInfo);

        if (!hasData)
        {
            ppInfo = dac_cast<PTR_PerPatchpointInfo>(new PerPatchpointInfo());
            m_jitPatchpointTable.InsertValue(ppId, (HashDatum)ppInfo);
            ppInfo->m_patchpointId = ++s_patchpointId;
        }
    }

    return ppInfo;
}

#endif // !DACCESS_COMPILE

#endif // FEATURE_ON_STACK_REPLACEMENT
 

