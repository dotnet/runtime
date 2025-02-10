// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "interpreter.h"

// Allocates the offset for var at the stack position identified by
// *pPos while bumping the pointer to point to the next stack location
int32_t InterpCompiler::AllocVarOffset(int var, int32_t *pPos)
{
    int32_t size, offset;

    offset = *pPos;
    size = m_pVars[var].size;

    m_pVars[var].offset = offset;

    *pPos = ALIGN_UP_TO(offset + size, INTERP_STACK_SLOT_SIZE);

    return m_pVars[var].offset;
}

void InterpCompiler::AllocVarOffsetCB(int *pVar, void *pData)
{
    AllocVarOffset(*pVar, &m_totalVarsStackSize);
}

void InterpCompiler::AllocOffsets()
{
    // FIXME add proper offset allocator
    InterpBasicBlock *pBB;

    for (pBB = m_pEntryBB; pBB != NULL; pBB = pBB->pNextBB)
    {
        InterpInst *pIns;

        for (pIns = pBB->pFirstIns; pIns != NULL; pIns = pIns->pNext)
            ForEachInsSVar(pIns, NULL, &InterpCompiler::AllocVarOffsetCB);
    }
    m_totalVarsStackSize = ALIGN_UP_TO(m_totalVarsStackSize, INTERP_STACK_ALIGNMENT);
    m_paramAreaOffset = m_totalVarsStackSize;
}
