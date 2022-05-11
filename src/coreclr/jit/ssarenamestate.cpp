// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "ssaconfig.h"
#include "ssarenamestate.h"

//------------------------------------------------------------------------
// SsaRenameState: Initialize SsaRenameState
//
// Arguments:
//    alloc - A memory allocator
//    lvaCount - The number of local variables
//
SsaRenameState::SsaRenameState(CompAllocator alloc, unsigned lvaCount)
    : m_alloc(alloc), m_lvaCount(lvaCount), m_stacks(nullptr), m_stackListTail(nullptr)
{
}

//------------------------------------------------------------------------
// EnsureStacks: Allocate memory for the stacks array.
//
void SsaRenameState::EnsureStacks()
{
    if (m_stacks == nullptr)
    {
        m_stacks = new (m_alloc) Stack[m_lvaCount]();
    }
}

//------------------------------------------------------------------------
// Top: Get the SSA number at the top of the stack for the specified variable.
//
// Arguments:
//    lclNum - The local variable number
//
// Return Value:
//    The SSA number.
//
// Notes:
//    The stack must not be empty. Method parameters and local variables that are live in at
//    the start of the first block must have associated SSA definitions and their SSA numbers
//    must have been pushed first.
//
unsigned SsaRenameState::Top(unsigned lclNum)
{
    noway_assert(m_stacks != nullptr);
    StackNode* top = m_stacks[lclNum].Top();
    noway_assert(top != nullptr);

    DBG_SSA_JITDUMP("[SsaRenameState::Top] " FMT_BB ", V%02u, ssaNum = %d\n", top->m_block->bbNum, lclNum,
                    top->m_ssaNum);

    return top->m_ssaNum;
}

//------------------------------------------------------------------------
// Push: Push a SSA number onto the stack for the specified variable.
//
// Arguments:
//    block  - The block where the SSA definition occurs
//    lclNum - The local variable number
//    ssaNum - The SSA number
//
void SsaRenameState::Push(BasicBlock* block, unsigned lclNum, unsigned ssaNum)
{
    DBG_SSA_JITDUMP("[SsaRenameState::Push] " FMT_BB ", V%02u, ssaNum = %d\n", block->bbNum, lclNum, ssaNum);

    EnsureStacks();
    Push(&m_stacks[lclNum], block, ssaNum);
}

//------------------------------------------------------------------------
// Push: Push a SSA number onto a stack
//
// Arguments:
//    stack  - The stack to push to
//    block  - The block where the SSA definition occurs
//    ssaNum - The SSA number
//
void SsaRenameState::Push(Stack* stack, BasicBlock* block, unsigned ssaNum)
{
    StackNode* top = stack->Top();

    if ((top == nullptr) || (top->m_block != block))
    {
        stack->Push(AllocStackNode(m_stackListTail, block, ssaNum));
        // Append the stack to the stack list. The stack list allows PopBlockStacks
        // to easily find stacks that need popping.
        m_stackListTail = stack;
    }
    else
    {
        // If we already have a stack node for this block then simply update
        // update the SSA number, the previous one is no longer needed.
        top->m_ssaNum = ssaNum;
    }

    INDEBUG(DumpStack(stack));
}

void SsaRenameState::PopBlockStacks(BasicBlock* block)
{
    DBG_SSA_JITDUMP("[SsaRenameState::PopBlockStacks] " FMT_BB "\n", block->bbNum);

    while ((m_stackListTail != nullptr) && (m_stackListTail->Top()->m_block == block))
    {
        StackNode* top = m_stackListTail->Pop();
        INDEBUG(DumpStack(m_stackListTail));
        m_stackListTail = top->m_listPrev;
        m_freeStack.Push(top);
    }

#ifdef DEBUG
    if (m_stacks != nullptr)
    {
        // It should now be the case that no stack in stacks has an entry for "block" on top --
        // the loop above popped them all.
        for (unsigned i = 0; i < m_lvaCount; ++i)
        {
            if (m_stacks[i].Top() != nullptr)
            {
                assert(m_stacks[i].Top()->m_block != block);
            }
        }
    }
#endif // DEBUG
}

#ifdef DEBUG
//------------------------------------------------------------------------
// DumpStack: Print the specified stack.
//
// Arguments:
//    stack - The stack to print
//
void SsaRenameState::DumpStack(Stack* stack)
{
    if (JitTls::GetCompiler()->verboseSsa)
    {
        if (stack == &m_memoryStack[ByrefExposed])
        {
            printf("ByrefExposed: ");
        }
        else if (stack == &m_memoryStack[GcHeap])
        {
            printf("GcHeap: ");
        }
        else
        {
            printf("V%02u: ", stack - m_stacks);
        }

        for (StackNode* i = stack->Top(); i != nullptr; i = i->m_stackPrev)
        {
            printf("%s<" FMT_BB ", %u>", (i == stack->Top()) ? "" : ", ", i->m_block->bbNum, i->m_ssaNum);
        }

        printf("\n");
    }
}
#endif // DEBUG
