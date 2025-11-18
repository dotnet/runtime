// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "algorithm.h"

//------------------------------------------------------------------------
// WasmInterval
//
// Represents a Wasm BLOCK/END or LOOP/END
//
class WasmInterval
{
private:

    // m_chain refers to the conflict set member with the lowest m_start.
    // (for "trivial" singleton conflict sets m_chain will be `this`)
    WasmInterval* m_chain;

    // True index of start
    unsigned m_start;

    // True index of end; interval ends just before this block
    unsigned m_end;

    // Largest end index of any chained interval
    unsigned m_chainEnd;

    // true if this is a loop interval (extents cannot change)
    bool m_isLoop;

public:

    WasmInterval(unsigned start, unsigned end, bool isLoop)
        : m_chain(nullptr)
        , m_start(start)
        , m_end(end)
        , m_chainEnd(end)
        , m_isLoop(isLoop)
    {
        m_chain = this;
    }

    unsigned Start() const
    {
        return m_start;
    }

    unsigned End() const
    {
        return m_end;
    }

    unsigned ChainEnd() const
    {
        return m_chainEnd;
    }

    // Call while resolving intervals when building chains.
    WasmInterval* FetchAndUpdateChain()
    {
        if (m_chain == this)
        {
            return this;
        }

        WasmInterval* chain = m_chain->FetchAndUpdateChain();
        m_chain             = chain;
        return chain;
    }

    // Call after intervals are resolved and chains are fixed.
    WasmInterval* Chain() const
    {
        assert((m_chain == this) || (m_chain == m_chain->Chain()));
        return m_chain;
    }

    bool IsLoop() const
    {
        return m_isLoop;
    }

    void SetChain(WasmInterval* c)
    {
        m_chain       = c;
        c->m_chainEnd = max(c->m_chainEnd, m_chainEnd);
    }

    static WasmInterval* NewBlock(Compiler* comp, BasicBlock* start, BasicBlock* end)
    {
        WasmInterval* result =
            new (comp, CMK_WasmCfgLowering) WasmInterval(start->bbPreorderNum, end->bbPreorderNum, /* isLoop */ false);
        return result;
    }

    static WasmInterval* NewLoop(Compiler* comp, BasicBlock* start, BasicBlock* end)
    {
        WasmInterval* result =
            new (comp, CMK_WasmCfgLowering) WasmInterval(start->bbPreorderNum, end->bbPreorderNum, /* isLoop */ true);
        return result;
    }

#ifdef DEBUG
    void Dump(bool chainExtent = false)
    {
        printf("[%03u,%03u]%s", m_start, chainExtent ? m_chainEnd : m_end, m_isLoop && !chainExtent ? " L" : "");

        if (m_chain != this)
        {
            printf(" --> ");
            m_chain->Dump(true);
        }
        else
        {
            printf("\n");
        }
    }
#endif
};

//------------------------------------------------------------------------
// fgWasmControlFlow: determine how to emit control flow instructions for wasm
//
// Notes:
//
// Wasm Control Flow: naive algorithm (no if/else)
//
// * We consider only normal flow here, so eg callfinally just proceeds to the callfinally ret
// * Funclets have been identified and separated (though this is not strictly required). With
//   suitable RPO we can model funclet flow disjointly from main method flow
// * A prior pass has removed all irreducible flow.
//
// First we build a (normal flow) loop aware RPO.
//
// Each loop creates a Wasm LOOP/END. Since all loops are reducible and the body is compact the entry
// is the first lexical block and the extent is the lexically last block. The only back-edges are loop back edges.
//
// Each non-contiguous forward branch potentially creates a block. The only trick is figuring out how to
// arrange the block begins so we have proper nesting of Wasm blocks and Wasm loops.
//
// Since we have linear order of basic blocks, each non-contiguous forward branch can be characterized
// by the source and destination basic block indices in the order. Eg [0, 4]. So an interval begins at
// the start of the first block and ends at the start of the second.
//
// Each basic block start may be the end of some loops and /or a block. Or both. Note multiple
// blocks that end at the same point are not necessary.
//
// We walk the LaRPO from front to back.
// * If we see a loop head, we record a loop interval [x,y]. This extent cannot be altered.
// * If we see a noncontiguous branch (or switch), we record a block interval [a,b]. Here
//   b must remain fixed but we can increase a as needed to accomplish nesting.
//   For switches we will create multiple [a,b0], [a, b1]...
//
// If a forward branch targets a block that already has an interval ending at that block, we do
// not need a new interval for the branch. Because we're walking front to back, we will have already
// recorded an interval that starts earlier.
//
// We then scan the intervals in non-decreasing start order, lookin for earlier intervals that contain
// the start of the current interval but not the end. When we find one, the start of the current interval
// will need to decrease so the earlier interval can nest inside. That is, if we have a:[0, 4] and b:[2,6] we
// will need to decrease the start of b to match a and then reorder, and emit them as b:[0,6], a[0,4].
//
// To save some time we also create a union-find like setup to identify the first interval in a set of
// conflicting intervals. Say we have a:[0,4] b:[2,6] c:[5,7]. When we see that b conflicts with a,
// we note 'a' as the conflict "chain" for b, and also track the conflict extent in a. Then when
// we scan intervals for c, we see it conflicts with the chain starting at a, and we add it to the chain.
// The net result is a:[0,4(7)], b:[2,6]-->a, c:[5,7]-->a.
//
// Then we order on their conflict chain start and end extent, and so would emit c:[0,7], b:[0,6], a:[0,4]
//
// We then can use the properly ordered and nested intervals to track the control stack depth as we
// traverse the blocks in loop-aware RPO order, and emit the proper Wasm control flow.
//
// Still TODO
//
// * proper DFS/Loop finding
// * handling irreducible loops
// * handling funclets
// * proper handling of BR_TABLE defaults
// * branch inversion
// * actual block reordering
// * instruction emission
// * tail calls (RETURN_CALL)
// * UNREACHED in more places (eg noreturn calls)
// * Rethink need for BB0 (have m_end refer to end of last block in range, not start of first block after)
// * We do not branch with operands on the wasm stack, so we need to add suitable (void?) types to branches
// * During LaRPO formation, remember the position of the last block in the loop
//
PhaseStatus Compiler::fgWasmControlFlow()
{
    // -----------------------------------------------
    // (1) Build loop-aware RPO layout
    //
    if (m_dfsTree == nullptr)
    {
        m_dfsTree = fgComputeDfs</* useProfile */ true>();
        m_loops   = FlowGraphNaturalLoops::Find(m_dfsTree);
    }
    else
    {
        assert(m_loops != nullptr);
    }

    // Bail out for now if there is any irreducible flow.
    // TODO: run the irreducible flow fixing before this.
    //
    if (m_loops->ImproperLoopHeaders() > 0)
    {
        JITDUMP("\nThere are irreducible loops here, bailing\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // Our interval ends are at the starts of blocks, so we need a block that
    // comes after all existing blocks. So allocate one extra slot.
    //
    JITDUMP("\nCreating loop-aware RPO\n");
    BasicBlock** const initialLayout = new (this, CMK_WasmCfgLowering) BasicBlock*[m_dfsTree->GetPostOrderCount() + 1];

    // TODO: extend this to cover all the funclets as well.
    //
    // The "DFS" we run above should skip from CALLFINALLY to CALLFINALLYRET, treat all funclet returns
    // as having no successor, and add each funclet entry as a DFS seed. This will give us a disjoint DFS
    // tree for the main method and each funclet, and the layout below will properly transform them all into
    // Wasm control flow.
    //
    unsigned numHotBlocks  = 0;
    auto     addToSequence = [initialLayout, &numHotBlocks](BasicBlock* block) {
        // Skip funclets for now
        //
        if (block->hasHndIndex())
        {
            return;
        }

        JITDUMP("%03u " FMT_BB "\n", numHotBlocks, block->bbNum);

        // Set the block's ordinal.
        block->bbPreorderNum          = numHotBlocks;
        initialLayout[numHotBlocks++] = block;
    };

    fgVisitBlocksInLoopAwareRPO(m_dfsTree, m_loops, addToSequence);

    // Splice in a fake BB0
    //
    BasicBlock bb0;
    INDEBUG(bb0.bbNum = 0;);
    bb0.bbPreorderNum           = numHotBlocks;
    bb0.bbPostorderNum          = m_dfsTree->GetPostOrderCount();
    initialLayout[numHotBlocks] = &bb0;

    // -----------------------------------------------
    // (2) Build the intervals
    //
    // Allocate interval and scratch vectors. We'll use the scratch vector to keep track of
    // block intervals that end at a certain point.
    //
    jitstd::vector<WasmInterval*> intervals(getAllocator(CMK_WasmCfgLowering));
    jitstd::vector<WasmInterval*> scratch(numHotBlocks, nullptr, getAllocator(CMK_WasmCfgLowering));

    for (unsigned int cursor = 0; cursor < numHotBlocks; cursor++)
    {
        BasicBlock* const block = initialLayout[cursor];

        // See if we entered any loops
        //
        FlowGraphNaturalLoop* const loop = m_loops->GetLoopByHeader(block);

        if (loop != nullptr)
        {
            // Find the loop's lexical extent given our ordering
            // (maybe memoize this during loop finding...)
            //
            // Note that cursor may end up pointing at BB0
            //
            unsigned endCursor = cursor;
            while ((endCursor < numHotBlocks) && loop->ContainsBlock(initialLayout[endCursor]))
            {
                endCursor++;
            }

            WasmInterval* const loopInterval = WasmInterval::NewLoop(this, block, initialLayout[endCursor]);

            // We assume here that a block is only the header of one loop.
            //
            intervals.push_back(loopInterval);
        }

        // Now see where block branches to...
        //
        if (block->KindIs(BBJ_CALLFINALLY))
        {
            // We ignore these and treat them as if they fall through to the tail (if there is a tail).
            // Since the tail cannot be a join we don't need a block.
            //
            continue;
        }

        for (BasicBlock* const succ : block->Succs())
        {
            unsigned const succNum = succ->bbPreorderNum;

            // We ignore back edges; they don't inspire blocks.
            //
            if (succNum <= cursor)
            {
                JITDUMP("Backedge " FMT_BB "[%u] -> " FMT_BB "[%u]\n", block->bbNum, cursor, succ->bbNum, succNum);

                // The backedge target should be a loop header.
                // (TODO: scan loop stack to verify the loop is on the stack?)
                //
                // Note we currently bail out way above if there are any irreducible loops.
                //
                assert(m_loops->GetLoopByHeader(succ) != nullptr);
                continue;
            }

            // Branch to next needs no block, unless this is a switch
            // (eventually when we leave the default on the switch we can remove this).
            //
            if ((succNum == (cursor + 1)) && !block->KindIs(BBJ_SWITCH))
            {
                continue;
            }

            // Branch to cold block needs no block (presumably something EH related).
            // Eventually we need to case these out and handle them better.
            //
            if (succNum >= numHotBlocks)
            {
                continue;
            }

            // See if we already have a block that ends at this point and starts before.
            //
            WasmInterval* const existingBlock = scratch[succNum];

            if (existingBlock != nullptr)
            {
                // If so we don't need to track this branch.
                //
                JITDUMP("Subsumed " FMT_BB "[%u] -> " FMT_BB "[%u]\n", block->bbNum, cursor, succ->bbNum, succNum);
                assert(existingBlock->Start() <= cursor);
                continue;
            }

            // Non-contiguous, non-subsumed forward branch
            //
            WasmInterval* const branch = WasmInterval::NewBlock(this, block, initialLayout[succNum]);
            intervals.push_back(branch);

            // Remember an interval end here
            //
            scratch[succNum] = branch;
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        // Display the raw intervals...
        //
        JITDUMP("\n-------------- Initial set of wasm intervals\n");
        for (WasmInterval* interval : intervals)
        {
            JITDUMPEXEC(interval->Dump());
        }
        JITDUMP("--------------\n\n");
    }
#endif

    // -----------------------------------------------
    // (3) Find intervals that overlap
    //
    // See if this interval conflicts with any other. If so,
    // add the interval to that intervals conflict set, and return
    // the conflict set for further resolution.
    //
    // Since this is only looking at prior intervals it could be
    // merged with (2) above.
    //
    auto resolve = [&intervals](WasmInterval* const current) {
        for (WasmInterval* prior : intervals)
        {
            // We only need to consider intervals that start at the same point or earlier.
            //
            if (prior == current)
            {
                break;
            }

            // We should be walking in non-decreasing start order
            //
            assert(prior->Start() <= current->Start());

            // We may have chained this previous interval to another even earlier.
            // Find the head of that chain.
            //
            WasmInterval* const priorChain = prior->FetchAndUpdateChain();
            assert(priorChain->Start() <= current->Start());

            // See if the current interval starts at or inside
            // the chain interval and ends outside.
            //
            if ((current->Start() < priorChain->ChainEnd()) && (current->End() > priorChain->ChainEnd()))
            {
                current->SetChain(priorChain);
                break;
            }

            // See if the current interval starts at or inside
            // the prior interval and ends outside.
            //
            if ((current->Start() < prior->End()) && (current->End() > prior->End()))
            {
                // Note we chain to the chain interval, not the prior interval
                //
                // Say we have [0,3] [1,4] [2,6] [3,5].
                //
                // Examining [1,4], we see a conflict with [0,3], and so we chain [1,4] to [0,3].
                //  (and the "chain end of [0,3] is now [0,4])
                // Examining [2,6], we see a conflict with [0,3], and so we chain [2,6] to [0,3].
                //  (and the "chain end of [0,3] is now [0,6])
                //
                // When examining [3,5] we don't see a conflict with [0,6] or [0,3].
                // But there is a conflict with [1,4], which is chained to [0,3]
                // so we chain [3,5] to [0,3] instead of to [1,4].
                //
                // And after sorting we then emit [0,6] [0,5] [0,4] [0,3]
                //
                current->SetChain(priorChain);
                break;
            }
        }
    };

    for (WasmInterval* interval : intervals)
    {
        resolve(interval);
    }

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("\n-------------- After finding conflicts\n");
        for (WasmInterval* iv : intervals)
        {
            JITDUMPEXEC(iv->Dump());
        }
        JITDUMP("--------------\n\n");
    }
#endif

    // (4) Sort to put intervals in proper nesting order
    //
    // Sort by chain start index (ascending) then actual end index (descending) then isLoop
    //
    auto comesBefore = [](WasmInterval* i1, WasmInterval* i2) {
        WasmInterval* const chain1 = i1->Chain();
        WasmInterval* const chain2 = i2->Chain();

        // Lowest chain start
        //
        if (chain1->Start() < chain2->Start())
        {
            return true;
        }

        if (chain2->Start() < chain1->Start())
        {
            return false;
        }

        // Highest end
        //
        if (i1->End() > i2->End())
        {
            return true;
        }

        if (i2->End() > i1->End())
        {
            return false;
        }

        // Tiebreaker
        //
        if (i1->IsLoop())
        {
            return true;
        }

        return false;
    };

    jitstd::sort(intervals.begin(), intervals.end(), comesBefore);

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("\n-------------- After sorting\n");
        for (WasmInterval* interval : intervals)
        {
            JITDUMPEXEC(interval->Dump());
        }
        JITDUMP("--------------\n\n");
    }
#endif

    // (5) Create the wasm control flow operations
    //
    // Show (roughly) what the WASM control flow looks like
    //
    ArrayStack<WasmInterval*> activeIntervals(getAllocator(CMK_WasmCfgLowering));
    unsigned                  wasmCursor = 0;

    for (unsigned int cursor = 0; cursor < numHotBlocks; cursor++)
    {
        BasicBlock* const block = initialLayout[cursor];

        JITDUMP("Before " FMT_BB " at %u stack is:", block->bbNum, cursor);
        if (activeIntervals.Empty())
        {
            JITDUMP("empty");
        }
        else
        {
            for (int i = 0; i < activeIntervals.Height(); i++)
            {
                JITDUMP(" [%u,%u]", activeIntervals.Top(i)->Start(), activeIntervals.Top(i)->End());
            }
        }
        JITDUMP("\n");

        // Close intervals that end here (at most two, block and/or loop)
        //
        while (!activeIntervals.Empty() && (activeIntervals.Top()->End() == cursor))
        {
            JITDUMP("END    (%u)%s\n", activeIntervals.Top()->End(), activeIntervals.Top()->IsLoop() ? " LOOP" : "");
            activeIntervals.Pop();
        }

        // Open intervals that start here or earlier
        //
        if (wasmCursor < intervals.size())
        {
            WasmInterval* interval = intervals[wasmCursor];
            WasmInterval* chain    = interval->Chain();

            while (chain->Start() <= cursor)
            {
                JITDUMP("%s (%u)\n", interval->IsLoop() ? "LOOP " : "BLOCK", interval->End());

                wasmCursor++;
                activeIntervals.Push(interval);

                if (wasmCursor >= intervals.size())
                {
                    break;
                }

                interval = intervals[wasmCursor];
                chain    = interval->Chain();
            }
        }

        JITDUMP("  " FMT_BB "\n", block->bbNum);

        // Compute the depth of the block ending at targetNum
        // or (if isBackedge) the loop starting at targetNum
        //
        auto findDepth = [&activeIntervals](unsigned targetNum, bool isBackedge, unsigned& match) {
            int const h = activeIntervals.Height();

            for (int i = 0; i < h; i++)
            {
                WasmInterval* const ii = activeIntervals.Top(i);
                match                  = 0;

                if (isBackedge)
                {
                    // loops bind to start
                    match = ii->Start();
                }
                else
                {
                    // blocks bind to end
                    match = ii->End();
                }

                if ((match == targetNum) && (isBackedge == ii->IsLoop()))
                {
                    return i;
                }
            }

            JITDUMP("Could not find %u%s in active control stack\n", targetNum, isBackedge ? " (backedge)" : "");
            assert(!"Can't find target in control stack");

            return ~0;
        };

        switch (block->GetKind())
        {
            case BBJ_RETURN:
            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
            case BBJ_EHFILTERRET:
            case BBJ_EHCATCHRET:
            {
                JITDUMP("RETURN\n");
                break;
            }

            case BBJ_THROW:
            {
                JITDUMP("THROW\n");
                break;
            }

            case BBJ_CALLFINALLY:
            {
                // no-op (implied fall through to tail, if it exists)
                //
                if (!block->isBBCallFinallyPair())
                {
                    JITDUMP("UNREACHED\n");
                }
                break;
            }

            case BBJ_ALWAYS:
            case BBJ_CALLFINALLYRET:
            {
                unsigned const succNum = block->GetTarget()->bbPreorderNum;

                if (succNum == (cursor + 1))
                {
                    JITDUMP("FALLTHROUGH\n");
                }
                else if (succNum < numHotBlocks)
                {
                    bool const isBackedge = succNum <= cursor;
                    unsigned   blockNum   = 0;
                    unsigned   depth      = findDepth(succNum, isBackedge, blockNum);
                    JITDUMP("BR %d (%u)%s\n", depth, blockNum, isBackedge ? "be" : "");
                }

                break;
            }

            case BBJ_COND:
            {
                const unsigned trueNum  = block->GetTrueTarget()->bbPreorderNum;
                const unsigned falseNum = block->GetFalseTarget()->bbPreorderNum;

                if (trueNum == falseNum)
                {
                    JITDUMP("FALLTHROUGH\n");
                    break;
                }

                // If the true target is the next block, we are in a bind, since
                // we need to branch to it, but may not have induced a block.
                //
                // We could anticipate this above and induce a block like we do for switches.
                //
                // Or we can just invert the branch condition here; I think this should be viable.
                // (eg invoke the core part of optOptimizePostLayout).
                //
                const bool invertCondition = trueNum == (cursor + 1);

                if (invertCondition)
                {
                    // TODO: induce a block and avoid this case, or actually modify the IR
                    //
                    JITDUMP("FALLTHROUGH-inv\n");
                }
                else if (trueNum < numHotBlocks)
                {
                    bool const isBackedge = trueNum <= cursor;
                    unsigned   blockNum   = 0;
                    unsigned   depth      = findDepth(trueNum, isBackedge, blockNum);
                    JITDUMP("BR_IF %d (%u)%s\n", depth, blockNum, isBackedge ? "be" : "");
                }

                if (falseNum == (cursor + 1))
                {
                    JITDUMP("FALLTHROUGH\n");
                }
                else if (falseNum < numHotBlocks)
                {
                    bool const isBackedge = falseNum <= cursor;
                    unsigned   blockNum   = 0;
                    unsigned   depth      = findDepth(falseNum, isBackedge, blockNum);
                    JITDUMP("BR%s %d (%u)%s\n", invertCondition ? "_IF-inv" : "", depth, blockNum,
                            isBackedge ? "be" : "");
                }

                break;
            }

            case BBJ_SWITCH:
            {
                BBswtDesc* const desc      = block->GetSwitchTargets();
                unsigned const   caseCount = desc->GetCaseCount();

                // BR_TABLE supports a default case, so we need to ensure
                // that wasm lower does not remove it.
                //
                // For now, we expect non-wasm lower has made the default case check explicit
                // and so our BR_TABLE emission is deficient.
                //
                assert(!desc->HasDefaultCase());

                if (caseCount == 0)
                {
                    JITDUMP("FALLTHROUGH\n");
                    break;
                }

                JITDUMP("BR_TABLE");

                for (unsigned caseNum = 0; caseNum < caseCount; caseNum++)
                {
                    BasicBlock* const caseTarget    = desc->GetCase(caseNum)->getDestinationBlock();
                    unsigned const    caseTargetNum = caseTarget->bbPreorderNum;

                    if (caseTargetNum < numHotBlocks)
                    {
                        bool const isBackedge = caseTargetNum <= cursor;
                        unsigned   blockNum   = 0;
                        unsigned   depth      = findDepth(caseTargetNum, isBackedge, blockNum);
                        JITDUMP("%s %d (%u)%s", caseNum > 0 ? "," : "", depth, blockNum, isBackedge ? "be" : "");
                    }
                }

                JITDUMP("\n");
                break;
            }

            default:
            {
                assert(!"Unexpected block kind");
            }
        }

        JITDUMP("\n");
    }

    // We should have closed out all intervals unless there are loops
    // that end at the end of the method.
    //
    while (!activeIntervals.Empty())
    {
        WasmInterval* const i = activeIntervals.Pop();
        JITDUMP("END    (%u)%s\n", i->End(), i->IsLoop() ? " LOOP" : "");
    }

#ifdef DEBUG

    if (verbose)
    {
        // Ditto but in dot markup
        //
        activeIntervals.Reset();
        wasmCursor = 0;
        JITDUMP("\ndigraph WASM {\n");

        for (unsigned int cursor = 0; cursor < numHotBlocks; cursor++)
        {
            BasicBlock* const block = initialLayout[cursor];

            // Close intervals that end here (at most two, block and/or loop)
            //
            while (!activeIntervals.Empty() && (activeIntervals.Top()->End() == cursor))
            {
                JITDUMP("  }\n");
                activeIntervals.Pop();
            }

            // Open intervals that start here
            //
            if (wasmCursor < intervals.size())
            {
                WasmInterval* interval = intervals[wasmCursor];
                WasmInterval* chain    = interval->Chain();

                while (chain->Start() <= cursor)
                {
                    JITDUMP("  subgraph cluster_%u_%u%s {\n", chain->Start(), interval->End(),
                            interval->IsLoop() ? "_loop" : "");

                    if (interval->IsLoop())
                    {
                        JITDUMP("    color=red;\n");
                    }
                    else
                    {
                        JITDUMP("    color=black;\n");
                    }

                    wasmCursor++;
                    activeIntervals.Push(interval);

                    if (wasmCursor >= intervals.size())
                    {
                        break;
                    }

                    interval = intervals[wasmCursor];
                    chain    = interval->Chain();
                }
            }

            JITDUMP("    " FMT_BB ";\n", block->bbNum);
        }

        // Close remaining intervals
        //
        while (!activeIntervals.Empty())
        {
            activeIntervals.Pop();
            JITDUMP("  }\n");
        }

        // Now list all the branches

        for (unsigned int cursor = 0; cursor < numHotBlocks; cursor++)
        {
            BasicBlock* const block = initialLayout[cursor];

            if (block->KindIs(BBJ_CALLFINALLY))
            {
                if (block->isBBCallFinallyPair())
                {
                    JITDUMP("   " FMT_BB " -> " FMT_BB " [style=dotted];\n", block->bbNum, block->Next()->bbNum);
                }
            }
            else
            {
                for (BasicBlock* const succ : block->Succs())
                {
                    JITDUMP("   " FMT_BB " -> " FMT_BB ";\n", block->bbNum, succ->bbNum);
                }
            }
        }

        JITDUMP("}\n");
    }

#endif // DEBUG

    return PhaseStatus::MODIFIED_NOTHING;
}
