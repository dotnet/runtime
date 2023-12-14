// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: pendingload.cpp
//

//

#include "common.h"
#include "excep.h"
#include "pendingload.h"

#ifndef DACCESS_COMPILE

#ifdef PENDING_TYPE_LOAD_TABLE_STATS

LONG pendingTypeLoadEntryDynamicAllocations = 0;
void PendingTypeLoadEntryDynamicAlloc()
{
    InterlockedIncrement(&pendingTypeLoadEntryDynamicAllocations);
}
#endif // PENDING_TYPE_LOAD_TABLE_STATS

static BYTE s_PendingTypeLoadTable[sizeof(PendingTypeLoadTable)];

/*static*/
PendingTypeLoadTable* PendingTypeLoadTable::GetTable()
{
    LIMITED_METHOD_CONTRACT;
    return reinterpret_cast<PendingTypeLoadTable*>(&s_PendingTypeLoadTable);
}

#ifdef _DEBUG
void PendingTypeLoadTable::Shard::Dump()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: shard contains:\n"));
    for (Entry *pSearch = this->m_pLinkedListOfActiveEntries; pSearch; pSearch = pSearch->m_pNext)
    {
        SString name;
        TypeKey entryTypeKey = pSearch->GetTypeKey();
        TypeString::AppendTypeKeyDebug(name, &entryTypeKey);
        LOG((LF_CLASSLOADER, LL_INFO10000, "  Entry %s with handle %p at level %s\n", name.GetUTF8(), pSearch->m_typeHandle.AsPtr(),
                pSearch->m_typeHandle.IsNull() ? "not-applicable" : classLoadLevelName[pSearch->m_typeHandle.GetLoadLevel()]));
    }
}
#endif

#endif // #ifndef DACCESS_COMPILE

