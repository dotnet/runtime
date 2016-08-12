// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//

//
// ==--==

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  SSA                                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once

#include "jitstd.h"

struct SsaRenameStateForBlock
{
    BasicBlock* m_bb;
    unsigned    m_count;

    SsaRenameStateForBlock(BasicBlock* bb, unsigned count) : m_bb(bb), m_count(count) {}
    SsaRenameStateForBlock() : m_bb(NULL), m_count(0) {}
};

// A record indicating that local "m_loc" was defined in block "m_bb".
struct SsaRenameStateLocDef
{
    BasicBlock* m_bb;
    unsigned    m_lclNum;

    SsaRenameStateLocDef(BasicBlock* bb, unsigned lclNum) : m_bb(bb), m_lclNum(lclNum) {}
};

struct SsaRenameState
{
    typedef jitstd::list<SsaRenameStateForBlock> Stack;
    typedef Stack** Stacks;
    typedef unsigned* Counts;
    typedef jitstd::list<SsaRenameStateLocDef> DefStack;

    SsaRenameState(const jitstd::allocator<int>& allocator, unsigned lvaCount);

    void EnsureCounts();
    void EnsureStacks();

    // Requires "lclNum" to be a variable number for which a new count corresponding to a
    // definition is desired. The method post increments the counter for the "lclNum."
    unsigned CountForDef(unsigned lclNum);

    // Requires "lclNum" to be a variable number for which an ssa number at the top of the
    // stack is required i.e., for variable "uses."
    unsigned CountForUse(unsigned lclNum);

    // Requires "lclNum" to be a variable number, and requires "count" to represent
    // an ssa number, that needs to be pushed on to the stack corresponding to the lclNum.
    void Push(BasicBlock* bb, unsigned lclNum, unsigned count);
    
    // Pop all stacks that have an entry for "bb" on top.
    void PopBlockStacks(BasicBlock* bb);

    // Similar functions for the special implicit "Heap" variable.
    unsigned CountForHeapDef()
    {
        if (heapCount == 0)
            heapCount = SsaConfig::FIRST_SSA_NUM;
        unsigned res = heapCount;
        heapCount++;
        return res;
    }
    unsigned CountForHeapUse()
    {
        return heapStack.back().m_count;
    }

    void PushHeap(BasicBlock* bb, unsigned count)
    {
        heapStack.push_back(SsaRenameStateForBlock(bb, count));
    }

    void PopBlockHeapStack(BasicBlock* bb);

    unsigned HeapCount() { return heapCount; }

#ifdef DEBUG
    // Debug interface
    void DumpStacks();
#endif

private:

    // Map of lclNum -> count.
    Counts counts;

    // Map of lclNum -> SsaRenameStateForBlock.
    Stacks stacks;

    // This list represents the set of locals defined in the current block.
    DefStack definedLocs;

    // Same state for the special implicit Heap variable.
    Stack     heapStack;
    unsigned  heapCount;

    // Number of stacks/counts to allocate.
    unsigned  lvaCount;

    // Allocator to allocate stacks.
    jitstd::allocator<void> m_alloc;
};

