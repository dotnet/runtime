// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CachedInterfaceDispatchPal.h"
#include "CachedInterfaceDispatch.h"

// The base memory allocator.
static AllocHeap * g_pAllocHeap = NULL;

bool InterfaceDispatch_InitializePal()
{
    g_pAllocHeap = new (nothrow) AllocHeap();
    if (g_pAllocHeap == NULL)
        return false;

    if (!g_pAllocHeap->Init())
        return false;

    return true;
}

// Allocate memory aligned at sizeof(void*)*2 boundaries
void *InterfaceDispatch_AllocDoublePointerAligned(size_t size)
{
    return g_pAllocHeap->AllocAligned(size, sizeof(void*) * 2);
}

// Allocate memory aligned at sizeof(void*) boundaries

void *InterfaceDispatch_AllocPointerAligned(size_t size)
{
    return g_pAllocHeap->AllocAligned(size, sizeof(void*));
}

FCIMPL4(PCODE, RhpUpdateDispatchCellCache, InterfaceDispatchCell * pCell, PCODE pTargetCode, MethodTable* pInstanceType, DispatchCellInfo *pNewCellInfo)
{
    return InterfaceDispatch_UpdateDispatchCellCache(pCell, pTargetCode, pInstanceType, pNewCellInfo);
}
FCIMPLEND

FCIMPL2(PCODE, RhpSearchDispatchCellCache, InterfaceDispatchCell * pCell, MethodTable* pInstanceType)
{
    return InterfaceDispatch_SearchDispatchCellCache(pCell, pInstanceType);
}
FCIMPLEND

// Given a dispatch cell, get the type and slot associated with it. This function MUST be implemented
// in cooperative native code, as the m_pCache field on the cell is unsafe to access from managed
// code due to its use of the GC state as a lock, and as lifetime control
FCIMPL2(void, RhpGetDispatchCellInfo, InterfaceDispatchCell * pCell, DispatchCellInfo* pDispatchCellInfo)
{
    *pDispatchCellInfo = pCell->GetDispatchCellInfo();
}
FCIMPLEND
