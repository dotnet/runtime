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

// Global vars are variables that are referenced from multiple basic blocks. We reserve
// a dedicated slot for each such variable.
int32_t InterpCompiler::AllocGlobalVarOffset(int var)
{
    return AllocVarOffset(var, &m_totalVarsStackSize);
}

// For a var that is local to the current bblock that we process, as we iterate
// over instructions we mark the first and last intruction using it.
void InterpCompiler::SetVarLiveRange(int32_t var, int insIndex)
{
    // We don't track liveness yet for global vars
    if (m_pVars[var].global)
        return;
    if (m_pVars[var].liveStart == -1)
        m_pVars[var].liveStart = insIndex;
    m_pVars[var].liveEnd = insIndex;
}

void InterpCompiler::SetVarLiveRangeCB(int32_t *pVar, void *pData)
{
    SetVarLiveRange(*pVar, (int)(size_t)pData);
}

void InterpCompiler::InitializeGlobalVar(int32_t var, int bbIndex)
{
    // Check if already handled
    if (m_pVars[var].global)
        return;

    if (m_pVars[var].bbIndex == -1)
    {
        m_pVars[var].bbIndex = bbIndex;
    }
    else if (m_pVars[var].bbIndex != bbIndex)
    {
        AllocGlobalVarOffset(var);
        m_pVars[var].global = true;
        INTERP_DUMP("alloc global var %d to offset %d\n", var, m_pVars[var].offset);
    }
}

void InterpCompiler::InitializeGlobalVarCB(int32_t *pVar, void *pData)
{
    InitializeGlobalVar(*pVar, (int)(size_t)pData);
}

void InterpCompiler::InitializeGlobalVars()
{
    InterpBasicBlock *pBB;
    for (pBB = m_pEntryBB; pBB != NULL; pBB = pBB->pNextBB)
    {
        InterpInst *pIns;

        for (pIns = pBB->pFirstIns; pIns != NULL; pIns = pIns->pNext) {

            int32_t opcode = pIns->opcode;
            if (opcode == INTOP_NOP)
                continue;
            if (opcode == INTOP_LDLOCA)
            {
                int var = pIns->sVars[0];
                // If global flag is set, it means its offset was already allocated
                if (!m_pVars[var].global)
                {
                    AllocGlobalVarOffset(var);
                    m_pVars[var].global = true;
                    INTERP_DUMP("alloc global var %d to offset %d\n", var, m_pVars[var].offset);
                }
            }
            ForEachInsVar(pIns, (void*)(size_t)pBB->index, &InterpCompiler::InitializeGlobalVarCB);
        }
    }
    m_totalVarsStackSize = ALIGN_UP_TO(m_totalVarsStackSize, INTERP_STACK_ALIGNMENT);
}

// In the final codegen, each call instruction will receive a single offset as an argument. At this
// offset all the call arguments will be located. This offset will point into the param area. Vars
// allocated here have special constraints compared to normal local/global vars.
//
// For each call instruction, this method computes its args offset. The call offset is computed as
// the max offset of all call offsets on which the call depends. Stack ensures that all call offsets
// on which the call depends are calculated before the call in question, by deferring calls from the
// last to the first one.
// 
// This method allocates offsets of resolved calls following a constraint where the base offset
// of a call must be greater than the offset of any argument of other active call args. It first
// removes the call from an array of active calls. If a match is found, the call is removed from
// the array by moving the last entry into its place. Otherwise, it is a call without arguments.
// 
// If there are active calls, the call in question is pushed onto the stack as a deferred call.
// The call contains a list of other active calls on which it depends. Those calls need to be
// resolved first in order to determine optimal base offset for the call in question. Otherwise,
// if there are no active calls, we resolve the call in question and deferred calls from the stack.
// 
// For better understanding, consider a simple example:
//  a <- _ 
//  b <- _
//  call1 c <- b
//  d <- _
//  call2 _ <- a c d
//
//   When `a` is defined, call2 becomes an active call, since `a` is part of call2 arguments.
//   When `b` is defined, call1 also becomes an active call,
//   When reaching call1, we attempt to resolve it. The problem with this is that call2 is already
// active, and all arguments of call1 should be placed after any arguments of call2 (in this example
// it would be enough for them to be placed after `a`, but for simplicity we place them after all
// arguments, so after `d` offset). Given call1 offset depends on call2 offset, we initialize its
// callDeps (to call2) and add call1 to the set of currently deferred calls. Call1 is no longer an
// an active call at this point.
//   When reaching call2, we see we have no remaining active calls, so we will resolve its offset.
// Once the offset is resolved, we continue to resolve each remaining call from the deferred list.
// Processing call1, we iterate over each call dependency (in our case just call2) and allocate its
// offset accordingly so it doesn't overlap with any call2 args offsets.
void InterpCompiler::EndActiveCall(InterpInst *call)
{
    // Remove call from array
    m_pActiveCalls->Remove(call);

    // Push active call that should be resolved onto the stack
    if (m_pActiveCalls->GetSize())
    {
        TSList<InterpInst*> *callDeps = NULL;
        for (int i = 0; i < m_pActiveCalls->GetSize(); i++)
            callDeps = TSList<InterpInst*>::Push(callDeps, m_pActiveCalls->Get(i));
        call->info.pCallInfo->callDeps = callDeps;
 
        m_pDeferredCalls = TSList<InterpInst*>::Push(m_pDeferredCalls, call);
    }
    else
    {
        call->info.pCallInfo->callDeps = NULL;
        // If no other active calls, current active call and all deferred calls can be resolved from the stack
        InterpInst *deferredCall = call;
        while (deferredCall) {
            // `base_offset` is a relative offset (to the start of the call args stack) where the args for this
            // call reside. The deps for a call represent the list of active calls at the moment when the call ends.
            // This means that all deps for a call end after the call in question. Given we iterate over the list
            // of deferred calls from the last to the first one to end, all deps of a call are guaranteed to have
            // been processed at this point.
            int32_t baseOffset = 0;
            for (TSList<InterpInst*> *list = deferredCall->info.pCallInfo->callDeps; list; list = list->pNext)
            {
                int32_t endOffset = list->data->info.pCallInfo->callEndOffset;
                if (endOffset > baseOffset)
                    baseOffset = endOffset;
            }
            deferredCall->info.pCallInfo->callOffset = baseOffset;
            // Compute to offset of each call argument
            int32_t *callArgs = deferredCall->info.pCallInfo->pCallArgs;
            if (callArgs && (*callArgs != -1))
            {
                int32_t var = *callArgs;
                while (var != CALL_ARGS_TERMINATOR)
                {
                    AllocVarOffset(var, &baseOffset);
                    callArgs++;
                    var = *callArgs;
                }
            }
            deferredCall->info.pCallInfo->callEndOffset = ALIGN_UP_TO(baseOffset, INTERP_STACK_ALIGNMENT);

            if (m_pDeferredCalls)
            {
                deferredCall = m_pDeferredCalls->data;
                m_pDeferredCalls = TSList<InterpInst*>::Pop(m_pDeferredCalls);
            }
            else
            {
                deferredCall = NULL;
            }
        }
    }
}

// Remove dead vars from the end of the active vars array and update the current offset
// to point immediately after the first found alive var. The space that used to belong
// to the now dead vars will be reused for future defined local vars in the same bblock.
void InterpCompiler::CompactActiveVars(int32_t *pCurrentOffset)
{
    int32_t size = m_pActiveVars->GetSize();
    if (!size)
        return;
    int32_t i = size - 1;
    while (i >= 0)
    {
        int32_t var = m_pActiveVars->Get(i);
        // If var is alive we can't compact anymore
        if (m_pVars[var].alive)
            return;
        *pCurrentOffset = m_pVars[var].offset;
        m_pActiveVars->RemoveAt(i);
        i--;
    }
}

void InterpCompiler::AllocOffsets()
{
    InterpBasicBlock *pBB;
    m_pActiveVars = new TArray<int32_t>();
    m_pActiveCalls = new TArray<InterpInst*>();
    m_pDeferredCalls = NULL;

    InitializeGlobalVars();

    INTERP_DUMP("\nAllocating var offsets\n");

    int finalVarsStackSize = m_totalVarsStackSize;

    // We now have the top of stack offset. All local regs are allocated after this offset, with each basic block
    for (pBB = m_pEntryBB; pBB != NULL; pBB = pBB->pNextBB)
    {
        InterpInst *pIns;
        int insIndex = 0;

        INTERP_DUMP("BB%d\n", pBB->index);

        // All data structs should be left empty after a bblock iteration
        assert(m_pActiveVars->GetSize() == 0);
        assert(m_pActiveCalls->GetSize() == 0);
        assert(m_pDeferredCalls == NULL);

        for (pIns = pBB->pFirstIns; pIns != NULL; pIns = pIns->pNext)
        {
            if (pIns->opcode == INTOP_NOP)
                continue;

            // TODO NewObj will be marked as noCallArgs
            if (pIns->flags & INTERP_INST_FLAG_CALL)
            {
                if (pIns->info.pCallInfo && pIns->info.pCallInfo->pCallArgs)
                {
                    int32_t *callArgs = pIns->info.pCallInfo->pCallArgs;
                    int32_t var = *callArgs;

                    while (var != -1)
                    {
                        if (m_pVars[var].global || m_pVars[var].noCallArgs)
                        {
                            // Some vars can't be allocated on the call args stack, since the constraint is that
                            // call args vars die after the call. This isn't necessarily true for global vars or
                            // vars that are used by other instructions aside from the call.
                            // We need to copy the var into a new tmp var
                            int newVar = CreateVarExplicit(m_pVars[var].interpType, m_pVars[var].clsHnd, m_pVars[var].size);
                            m_pVars[newVar].call = pIns;
                            m_pVars[newVar].callArgs = true;

                            int32_t opcode = InterpGetMovForType(m_pVars[newVar].interpType, false);
                            InterpInst *newInst = InsertInsBB(pBB, pIns->pPrev, opcode);
                            newInst->SetDVar(newVar);
                            newInst->SetSVar(newVar);
                            if (opcode == INTOP_MOV_VT)
                                newInst->data[0] = m_pVars[var].size;
                            // The arg of the call is no longer global
                            *callArgs = newVar;
                            // Also update liveness for this instruction
                            ForEachInsVar(newInst, (void*)(size_t)insIndex, &InterpCompiler::SetVarLiveRangeCB);
                            insIndex++;
                        }
                        else
                        {
                            // Flag this var as it has special storage on the call args stack
                            m_pVars[var].call = pIns;
                            m_pVars[var].callArgs = true;
                        }
                        callArgs++;
                        var = *callArgs;
                    }
                }
            }
            // Set liveStart and liveEnd for every referenced local that is not global
            ForEachInsVar(pIns, (void*)(size_t)insIndex, &InterpCompiler::SetVarLiveRangeCB);
            insIndex++;
        }
        int32_t currentOffset = m_totalVarsStackSize;

        insIndex = 0;
        for (pIns = pBB->pFirstIns; pIns != NULL; pIns = pIns->pNext) {
            int32_t opcode = pIns->opcode;
            bool isCall = pIns->flags & INTERP_INST_FLAG_CALL;

            if (opcode == INTOP_NOP)
                continue;

#ifdef DEBUG
            if (m_verbose)
            {
                printf("\tins_index %d\t", insIndex);
                PrintIns(pIns);
            }
#endif

            // Expire source vars. We first mark them as not alive and then compact the array
            for (int i = 0; i < g_interpOpSVars[opcode]; i++)
            {
                int32_t var = pIns->sVars[i];
                if (var == CALL_ARGS_SVAR)
                    continue;
                if (!m_pVars[var].global && m_pVars[var].liveEnd == insIndex)
                {
                    // Mark the var as no longer being alive
                    assert(!m_pVars[var].callArgs);
                    m_pVars[var].alive = false;
                }
            }

            if (isCall)
                EndActiveCall(pIns);

            CompactActiveVars(&currentOffset);

            // Alloc dreg local starting at the stack_offset
            if (g_interpOpDVars[opcode])
            {
                int32_t var = pIns->dVar;

                if (m_pVars[var].callArgs)
                {
                    InterpInst *call = m_pVars[var].call;
                    // Check if already added
                    if (!(call->flags & INTERP_INST_FLAG_ACTIVE_CALL))
                    {
                        m_pActiveCalls->Add(call);
                        // Mark a flag on it so we don't have to lookup the array with every argument store.
                        call->flags |= INTERP_INST_FLAG_ACTIVE_CALL;
                    }
                }
                else if (!m_pVars[var].global && m_pVars[var].offset == -1)
                {
                    AllocVarOffset(var, &currentOffset);
                    INTERP_DUMP("alloc var %d to offset %d\n", var, m_pVars[var].offset);

                    if (currentOffset > finalVarsStackSize)
                        finalVarsStackSize = currentOffset;

                    if (m_pVars[var].liveEnd > insIndex)
                    {
                        // If dVar is still used in the basic block, add it to the active list
                        m_pActiveVars->Add(var);
                        m_pVars[var].alive = true;
                    }
                    else
                    {
                        // Otherwise dealloc it
                        currentOffset = m_pVars[var].offset;
                    }
                }
            }

#ifdef DEBUG
            if (m_verbose)
            {
                printf("active vars:");
                for (int i = 0; i < m_pActiveVars->GetSize(); i++)
                {
                    int32_t var = m_pActiveVars->Get(i);
                    if (m_pVars[var].alive)
                        printf(" %d (end %d),", var, m_pVars[var].liveEnd);
                }
                printf("\n");
            }
#endif
            insIndex++;
        }
    }
    finalVarsStackSize = ALIGN_UP_TO(finalVarsStackSize, INTERP_STACK_ALIGNMENT);

    // Iterate over all call args locals, update their final offset (aka add td->total_locals_size to them)
    // then also update td->total_locals_size to account for this space.
    m_paramAreaOffset = finalVarsStackSize;
    for (int32_t i = 0; i < m_varsSize; i++)
    {
        // These are allocated separately at the end of the stack
        if (m_pVars[i].callArgs)
        {
            m_pVars[i].offset += m_paramAreaOffset;
            int32_t topOffset = m_pVars[i].offset + m_pVars[i].size;
            if (finalVarsStackSize < topOffset)
                finalVarsStackSize = topOffset;
        }
    }
    m_totalVarsStackSize = ALIGN_UP_TO(finalVarsStackSize, INTERP_STACK_ALIGNMENT);
}
