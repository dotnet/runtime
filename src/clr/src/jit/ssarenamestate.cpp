// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#include "ssaconfig.h"
#include "ssarenamestate.h"

/**
 * Constructor - initialize the stacks and counters maps (lclVar -> stack/counter) map.
 *
 * @params alloc The allocator class used to allocate jitstd data.
 */
SsaRenameState::SsaRenameState(CompAllocator alloc, unsigned lvaCount, bool byrefStatesMatchGcHeapStates)
    : stacks(nullptr)
    , definedLocs(alloc)
    , memoryStack(alloc)
    , lvaCount(lvaCount)
    , m_alloc(alloc)
    , byrefStatesMatchGcHeapStates(byrefStatesMatchGcHeapStates)
{
}

/**
 * Allocates memory for holding pointers to lcl's stacks,
 * if not allocated already.
 *
 */
void SsaRenameState::EnsureStacks()
{
    if (stacks == nullptr)
    {
        stacks = m_alloc.allocate<Stack*>(lvaCount);
        for (unsigned i = 0; i < lvaCount; ++i)
        {
            stacks[i] = nullptr;
        }
    }
}

/**
 * Returns a SSA count number for a local variable from top of the stack.
 *
 * @params lclNum The local variable def for which a count has to be returned.
 * @return the current variable name for the "use".
 *
 * @remarks If the stack is empty, then we have an use before a def. To handle this
 *          special case, we need to initialize the count with 'default+1', so the
 *          next definition will always use 'default+1' but return 'default' for
 *          all uses until a definition.
 *
 */
unsigned SsaRenameState::CountForUse(unsigned lclNum)
{
    EnsureStacks();
    DBG_SSA_JITDUMP("[SsaRenameState::CountForUse] V%02u\n", lclNum);

    Stack* stack = stacks[lclNum];
    noway_assert((stack != nullptr) && !stack->empty());
    return stack->back().m_count;
}

/**
 * Pushes a count value on the variable stack.
 *
 * @params lclNum The local variable def whose stack the count needs to be pushed onto.
 * @params count The current count value that needs to be pushed on to the stack.
 *
 * @remarks Usually called when renaming a "def."
 *          Create stack lazily when needed for the first time.
 */
void SsaRenameState::Push(BasicBlock* bb, unsigned lclNum, unsigned count)
{
    EnsureStacks();

    // We'll use BB00 here to indicate the "block before any real blocks..."
    DBG_SSA_JITDUMP("[SsaRenameState::Push] " FMT_BB ", V%02u, count = %d\n", bb != nullptr ? bb->bbNum : 0, lclNum,
                    count);

    Stack* stack = stacks[lclNum];

    if (stack == nullptr)
    {
        DBG_SSA_JITDUMP("\tCreating a new stack\n");
        stack = stacks[lclNum] = new (m_alloc) Stack(m_alloc);
    }

    if (stack->empty() || stack->back().m_bb != bb)
    {
        stack->push_back(SsaRenameStateForBlock(bb, count));
        // Remember that we've pushed a def for this loc (so we don't have
        // to traverse *all* the locs to do the necessary pops later).
        definedLocs.push_back(SsaRenameStateLocDef(bb, lclNum));
    }
    else
    {
        stack->back().m_count = count;
    }

#ifdef DEBUG
    if (JitTls::GetCompiler()->verboseSsa)
    {
        printf("\tContents of the stack: [");
        for (Stack::iterator iter2 = stack->begin(); iter2 != stack->end(); iter2++)
        {
            printf("<" FMT_BB ", %d>", ((*iter2).m_bb != nullptr ? (*iter2).m_bb->bbNum : 0), (*iter2).m_count);
        }
        printf("]\n");

        DumpStacks();
    }
#endif
}

void SsaRenameState::PopBlockStacks(BasicBlock* block)
{
    DBG_SSA_JITDUMP("[SsaRenameState::PopBlockStacks] " FMT_BB "\n", block->bbNum);
    // Iterate over the stacks for all the variables, popping those that have an entry
    // for "block" on top.
    while (!definedLocs.empty() && definedLocs.back().m_bb == block)
    {
        unsigned lclNum = definedLocs.back().m_lclNum;
        assert(stacks != nullptr); // Cannot be empty because definedLocs is not empty.
        Stack* stack = stacks[lclNum];
        assert(stack != nullptr);
        assert(stack->back().m_bb == block);
        stack->pop_back();
        definedLocs.pop_back();
    }
#ifdef DEBUG
    // It should now be the case that no stack in stacks has an entry for "block" on top --
    // the loop above popped them all.
    for (unsigned i = 0; i < lvaCount; ++i)
    {
        if (stacks != nullptr && stacks[i] != nullptr && !stacks[i]->empty())
        {
            assert(stacks[i]->back().m_bb != block);
        }
    }
    if (JitTls::GetCompiler()->verboseSsa)
    {
        DumpStacks();
    }
#endif // DEBUG
}

void SsaRenameState::PopBlockMemoryStack(MemoryKind memoryKind, BasicBlock* block)
{
    auto& stack = memoryStack[memoryKind];
    while (stack.size() > 0 && stack.back().m_bb == block)
    {
        stack.pop_back();
    }
}

#ifdef DEBUG
/**
 * Print the stack data for each variable in a loop.
 */
void SsaRenameState::DumpStacks()
{
    printf("Dumping stacks:\n-------------------------------\n");
    if (lvaCount == 0)
    {
        printf("None\n");
    }
    else
    {
        EnsureStacks();
        for (unsigned i = 0; i < lvaCount; ++i)
        {
            Stack* stack = stacks[i];
            printf("V%02u:\t", i);
            if (stack != nullptr)
            {
                for (Stack::iterator iter2 = stack->begin(); iter2 != stack->end(); ++iter2)
                {
                    if (iter2 != stack->begin())
                    {
                        printf(", ");
                    }
                    printf("<" FMT_BB ", %2d>", ((*iter2).m_bb != nullptr ? (*iter2).m_bb->bbNum : 0),
                           (*iter2).m_count);
                }
            }
            printf("\n");
        }
    }
}
#endif // DEBUG
