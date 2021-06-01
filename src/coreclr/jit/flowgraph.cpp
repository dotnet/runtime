// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph Miscellany

//------------------------------------------------------------------------
// blockNeedsGCPoll: Determine whether the block needs GC poll inserted
//
// Arguments:
//   block         - the block to check
//
// Notes:
//    The GC poll may not be required because of optimizations applied earlier
//    or because of GC poll done implicitly by regular unmanaged calls.
//
// Returns:
//    Whether the GC poll needs to be inserted after the block
//
static bool blockNeedsGCPoll(BasicBlock* block)
{
    bool blockMayNeedGCPoll = false;
    for (Statement* stmt = block->FirstNonPhiDef(); stmt != nullptr; stmt = stmt->GetNextStmt())
    {
        if ((stmt->GetRootNode()->gtFlags & GTF_CALL) != 0)
        {
            for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
            {
                if (tree->OperGet() == GT_CALL)
                {
                    GenTreeCall* call = tree->AsCall();
                    if (call->IsUnmanaged())
                    {
                        if (!call->IsSuppressGCTransition())
                        {
                            // If the block contains regular unmanaged call, we can depend on it
                            // to poll for GC. No need to scan further.
                            return false;
                        }

                        blockMayNeedGCPoll = true;
                    }
                }
            }
        }
    }
    return blockMayNeedGCPoll;
}

//------------------------------------------------------------------------------
// fgInsertGCPolls : Insert GC polls for basic blocks containing calls to methods
//                   with SuppressGCTransitionAttribute.
//
// Notes:
//    When not optimizing, the method relies on BBF_HAS_SUPPRESSGC_CALL flag to
//    find the basic blocks that require GC polls; when optimizing the tree nodes
//    are scanned to find calls to methods with SuppressGCTransitionAttribute.
//
//    This must be done after any transformations that would add control flow between
//    calls.
//
// Returns:
//    PhaseStatus indicating what, if anything, was changed.
//

PhaseStatus Compiler::fgInsertGCPolls()
{
    PhaseStatus result = PhaseStatus::MODIFIED_NOTHING;

    if ((optMethodFlags & OMF_NEEDS_GCPOLLS) == 0)
    {
        return result;
    }

    bool createdPollBlocks = false;

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgInsertGCPolls() for %s\n", info.compFullName);
        fgDispBasicBlocks(false);
        printf("\n");
    }
#endif // DEBUG

    BasicBlock* block;

    // Walk through the blocks and hunt for a block that needs a GC Poll
    for (block = fgFirstBB; block; block = block->bbNext)
    {
        // When optimizations are enabled, we can't rely on BBF_HAS_SUPPRESSGC_CALL flag:
        // the call could've been moved, e.g., hoisted from a loop, CSE'd, etc.
        if (opts.OptimizationDisabled() ? ((block->bbFlags & BBF_HAS_SUPPRESSGC_CALL) == 0) : !blockNeedsGCPoll(block))
        {
            continue;
        }

        result = PhaseStatus::MODIFIED_EVERYTHING;

        // This block needs a GC poll. We either just insert a callout or we split the block and inline part of
        // the test.

        // If we're doing GCPOLL_CALL, just insert a GT_CALL node before the last node in the block.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        switch (block->bbJumpKind)
        {
            case BBJ_RETURN:
            case BBJ_ALWAYS:
            case BBJ_COND:
            case BBJ_SWITCH:
            case BBJ_NONE:
            case BBJ_THROW:
            case BBJ_CALLFINALLY:
                break;
            default:
                assert(!"Unexpected block kind");
        }
#endif // DEBUG

        GCPollType pollType = GCPOLL_INLINE;

        // We'd like to inset an inline poll. Below is the list of places where we
        // can't or don't want to emit an inline poll. Check all of those. If after all of that we still
        // have INLINE, then emit an inline check.

        if (opts.OptimizationDisabled())
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Selecting CALL poll in block " FMT_BB " because of debug/minopts\n", block->bbNum);
            }
#endif // DEBUG

            // Don't split blocks and create inlined polls unless we're optimizing.
            pollType = GCPOLL_CALL;
        }
        else if (genReturnBB == block)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Selecting CALL poll in block " FMT_BB " because it is the single return block\n", block->bbNum);
            }
#endif // DEBUG

            // we don't want to split the single return block
            pollType = GCPOLL_CALL;
        }
        else if (BBJ_SWITCH == block->bbJumpKind)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Selecting CALL poll in block " FMT_BB " because it is a SWITCH block\n", block->bbNum);
            }
#endif // DEBUG

            // We don't want to deal with all the outgoing edges of a switch block.
            pollType = GCPOLL_CALL;
        }
        else if ((block->bbFlags & BBF_COLD) != 0)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Selecting CALL poll in block " FMT_BB " because it is a cold block\n", block->bbNum);
            }
#endif // DEBUG

            // We don't want to split a cold block.
            pollType = GCPOLL_CALL;
        }

        BasicBlock* curBasicBlock = fgCreateGCPoll(pollType, block);
        createdPollBlocks |= (block != curBasicBlock);
        block = curBasicBlock;
    }

    // If we split a block to create a GC Poll, then rerun fgReorderBlocks to push the rarely run blocks out
    // past the epilog.  We should never split blocks unless we're optimizing.
    if (createdPollBlocks)
    {
        noway_assert(opts.OptimizationEnabled());
        fgReorderBlocks();
        constexpr bool computePreds = true;
        constexpr bool computeDoms  = false;
        fgUpdateChangedFlowGraph(computePreds, computeDoms);
    }
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** After fgInsertGCPolls()\n");
        fgDispBasicBlocks(true);
    }
#endif // DEBUG

    return result;
}

//------------------------------------------------------------------------------
// fgCreateGCPoll : Insert a GC poll of the specified type for the given basic block.
//
// Arguments:
//    pollType  - The type of GC poll to insert
//    block     - Basic block to insert the poll for
//
// Return Value:
//    If new basic blocks are inserted, the last inserted block; otherwise, the input block.
//

BasicBlock* Compiler::fgCreateGCPoll(GCPollType pollType, BasicBlock* block)
{
    bool createdPollBlocks;

    void* addrTrap;
    void* pAddrOfCaptureThreadGlobal;

    addrTrap = info.compCompHnd->getAddrOfCaptureThreadGlobal(&pAddrOfCaptureThreadGlobal);

    // If the trap and address of thread global are null, make the call.
    if (addrTrap == nullptr && pAddrOfCaptureThreadGlobal == nullptr)
    {
        pollType = GCPOLL_CALL;
    }

    // Create the GC_CALL node
    GenTree* call = gtNewHelperCallNode(CORINFO_HELP_POLL_GC, TYP_VOID);
    call          = fgMorphCall(call->AsCall());
    gtSetEvalOrder(call);

    BasicBlock* bottom = nullptr;

    if (pollType == GCPOLL_CALL)
    {
        createdPollBlocks = false;

        Statement* newStmt = nullptr;
        if ((block->bbJumpKind == BBJ_ALWAYS) || (block->bbJumpKind == BBJ_CALLFINALLY) ||
            (block->bbJumpKind == BBJ_NONE))
        {
            // For BBJ_ALWAYS, BBJ_CALLFINALLY, and BBJ_NONE and  we don't need to insert it before the condition.
            // Just append it.
            newStmt = fgNewStmtAtEnd(block, call);
        }
        else
        {
            newStmt = fgNewStmtNearEnd(block, call);
            // For DDB156656, we need to associate the GC Poll with the IL offset (and therefore sequence
            // point) of the tree before which we inserted the poll.  One example of when this is a
            // problem:
            //  if (...) {  //1
            //      ...
            //  } //2
            //  else { //3
            //      ...
            //  }
            //  (gcpoll) //4
            //  return. //5
            //
            //  If we take the if statement at 1, we encounter a jump at 2.  This jumps over the else
            //  and lands at 4.  4 is where we inserted the gcpoll.  However, that is associated with
            //  the sequence point a 3.  Therefore, the debugger displays the wrong source line at the
            //  gc poll location.
            //
            //  More formally, if control flow targets an instruction, that instruction must be the
            //  start of a new sequence point.
            Statement* nextStmt = newStmt->GetNextStmt();
            if (nextStmt != nullptr)
            {
                // Is it possible for gtNextStmt to be NULL?
                newStmt->SetILOffsetX(nextStmt->GetILOffsetX());
            }
        }

        if (fgStmtListThreaded)
        {
            gtSetStmtInfo(newStmt);
            fgSetStmtSeq(newStmt);
        }

        block->bbFlags |= BBF_GC_SAFE_POINT;
#ifdef DEBUG
        if (verbose)
        {
            printf("*** creating GC Poll in block " FMT_BB "\n", block->bbNum);
            gtDispBlockStmts(block);
        }
#endif // DEBUG
    }
    else // GCPOLL_INLINE
    {
        assert(pollType == GCPOLL_INLINE);
        createdPollBlocks = true;
        // if we're doing GCPOLL_INLINE, then:
        //  1) Create two new blocks: Poll and Bottom.  The original block is called Top.

        // I want to create:
        // top -> poll -> bottom (lexically)
        // so that we jump over poll to get to bottom.
        BasicBlock*   top                = block;
        BasicBlock*   topFallThrough     = nullptr;
        unsigned char lpIndexFallThrough = BasicBlock::NOT_IN_LOOP;

        if (top->bbJumpKind == BBJ_COND)
        {
            topFallThrough     = top->bbNext;
            lpIndexFallThrough = topFallThrough->bbNatLoopNum;
        }

        BasicBlock* poll          = fgNewBBafter(BBJ_NONE, top, true);
        bottom                    = fgNewBBafter(top->bbJumpKind, poll, true);
        BBjumpKinds   oldJumpKind = top->bbJumpKind;
        unsigned char lpIndex     = top->bbNatLoopNum;

        // Update block flags
        const unsigned __int64 originalFlags = top->bbFlags | BBF_GC_SAFE_POINT;

        // We are allowed to split loops and we need to keep a few other flags...
        //
        noway_assert((originalFlags & (BBF_SPLIT_NONEXIST &
                                       ~(BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1 | BBF_LOOP_PREHEADER |
                                         BBF_RETLESS_CALL))) == 0);
        top->bbFlags = originalFlags & (~(BBF_SPLIT_LOST | BBF_LOOP_PREHEADER | BBF_RETLESS_CALL) | BBF_GC_SAFE_POINT);
        bottom->bbFlags |= originalFlags & (BBF_SPLIT_GAINED | BBF_IMPORTED | BBF_GC_SAFE_POINT | BBF_LOOP_PREHEADER |
                                            BBF_RETLESS_CALL);
        bottom->inheritWeight(top);
        poll->bbFlags |= originalFlags & (BBF_SPLIT_GAINED | BBF_IMPORTED | BBF_GC_SAFE_POINT);

        // Mark Poll as rarely run.
        poll->bbSetRunRarely();
        poll->bbNatLoopNum = lpIndex; // Set the bbNatLoopNum in case we are in a loop

        // Bottom gets all the outgoing edges and inherited flags of Original.
        bottom->bbJumpDest   = top->bbJumpDest;
        bottom->bbNatLoopNum = lpIndex; // Set the bbNatLoopNum in case we are in a loop
        if (lpIndex != BasicBlock::NOT_IN_LOOP)
        {
            // Set the new lpBottom in the natural loop table
            optLoopTable[lpIndex].lpBottom = bottom;
        }

        if (lpIndexFallThrough != BasicBlock::NOT_IN_LOOP)
        {
            // Set the new lpHead in the natural loop table
            optLoopTable[lpIndexFallThrough].lpHead = bottom;
        }

        // Add the GC_CALL node to Poll.
        Statement* pollStmt = fgNewStmtAtEnd(poll, call);
        if (fgStmtListThreaded)
        {
            gtSetStmtInfo(pollStmt);
            fgSetStmtSeq(pollStmt);
        }

        // Remove the last statement from Top and add it to Bottom if necessary.
        if ((oldJumpKind == BBJ_COND) || (oldJumpKind == BBJ_RETURN) || (oldJumpKind == BBJ_THROW))
        {
            Statement* stmt = top->firstStmt();
            while (stmt->GetNextStmt() != nullptr)
            {
                stmt = stmt->GetNextStmt();
            }
            fgRemoveStmt(top, stmt);
            fgInsertStmtAtEnd(bottom, stmt);
        }

        // for BBJ_ALWAYS blocks, bottom is an empty block.

        // Create a GT_EQ node that checks against g_TrapReturningThreads.  True jumps to Bottom,
        // false falls through to poll.  Add this to the end of Top.  Top is now BBJ_COND.  Bottom is
        // now a jump target
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef ENABLE_FAST_GCPOLL_HELPER
        // Prefer the fast gc poll helepr over the double indirection
        noway_assert(pAddrOfCaptureThreadGlobal == nullptr);
#endif

        GenTree* value; // The value of g_TrapReturningThreads
        if (pAddrOfCaptureThreadGlobal != nullptr)
        {
            // Use a double indirection
            GenTree* addr =
                gtNewIndOfIconHandleNode(TYP_I_IMPL, (size_t)pAddrOfCaptureThreadGlobal, GTF_ICON_CONST_PTR, true);

            value = gtNewOperNode(GT_IND, TYP_INT, addr);
            // This indirection won't cause an exception.
            value->gtFlags |= GTF_IND_NONFAULTING;
        }
        else
        {
            // Use a single indirection
            value = gtNewIndOfIconHandleNode(TYP_INT, (size_t)addrTrap, GTF_ICON_GLOBAL_PTR, false);
        }

        // NOTE: in c++ an equivalent load is done via LoadWithoutBarrier() to ensure that the
        // program order is preserved. (not hoisted out of a loop or cached in a local, for example)
        //
        // Here we introduce the read really late after all major optimizations are done, and the location
        // is formally unknown, so noone could optimize the load, thus no special flags are needed.

        // Compare for equal to zero
        GenTree* trapRelop = gtNewOperNode(GT_EQ, TYP_INT, value, gtNewIconNode(0, TYP_INT));

        trapRelop->gtFlags |= GTF_RELOP_JMP_USED | GTF_DONT_CSE;
        GenTree* trapCheck = gtNewOperNode(GT_JTRUE, TYP_VOID, trapRelop);
        gtSetEvalOrder(trapCheck);
        Statement* trapCheckStmt = fgNewStmtAtEnd(top, trapCheck);
        if (fgStmtListThreaded)
        {
            gtSetStmtInfo(trapCheckStmt);
            fgSetStmtSeq(trapCheckStmt);
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Adding trapCheck in " FMT_BB "\n", top->bbNum);
            gtDispTree(trapCheck);
        }
#endif

        top->bbJumpDest = bottom;
        top->bbJumpKind = BBJ_COND;

        // Bottom has Top and Poll as its predecessors.  Poll has just Top as a predecessor.
        fgAddRefPred(bottom, poll);
        fgAddRefPred(bottom, top);
        fgAddRefPred(poll, top);

        // Replace Top with Bottom in the predecessor list of all outgoing edges from Bottom
        // (1 for unconditional branches, 2 for conditional branches, N for switches).
        switch (oldJumpKind)
        {
            case BBJ_NONE:
                fgReplacePred(bottom->bbNext, top, bottom);
                break;
            case BBJ_RETURN:
            case BBJ_THROW:
                // no successors
                break;
            case BBJ_COND:
                // replace predecessor in the fall through block.
                noway_assert(bottom->bbNext);
                fgReplacePred(bottom->bbNext, top, bottom);

                // fall through for the jump target
                FALLTHROUGH;

            case BBJ_ALWAYS:
            case BBJ_CALLFINALLY:
                fgReplacePred(bottom->bbJumpDest, top, bottom);
                break;
            case BBJ_SWITCH:
                NO_WAY("SWITCH should be a call rather than an inlined poll.");
                break;
            default:
                NO_WAY("Unknown block type for updating predecessor lists.");
        }

        if (compCurBB == top)
        {
            compCurBB = bottom;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("*** creating inlined GC Poll in top block " FMT_BB "\n", top->bbNum);
            gtDispBlockStmts(top);
            printf(" poll block is " FMT_BB "\n", poll->bbNum);
            gtDispBlockStmts(poll);
            printf(" bottom block is " FMT_BB "\n", bottom->bbNum);
            gtDispBlockStmts(bottom);

            printf("\nAfter this change in fgCreateGCPoll the BB graph is:");
            fgDispBasicBlocks(false);
        }
#endif // DEBUG
    }

    return createdPollBlocks ? bottom : block;
}

//------------------------------------------------------------------------
// fgCanSwitchToOptimized: Determines if conditions are met to allow switching the opt level to optimized
//
// Return Value:
//    True if the opt level may be switched from tier 0 to optimized, false otherwise
//
// Assumptions:
//    - compInitOptions() has been called
//    - compSetOptimizationLevel() has not been called
//
// Notes:
//    This method is to be called at some point before compSetOptimizationLevel() to determine if the opt level may be
//    changed based on information gathered in early phases.

bool Compiler::fgCanSwitchToOptimized()
{
    bool result = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0) && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT) &&
                  !opts.compDbgCode && !compIsForInlining();
    if (result)
    {
        // Ensure that it would be safe to change the opt level
        assert(opts.compFlags == CLFLG_MINOPT);
        assert(!opts.IsMinOptsSet());
    }

    return result;
}

//------------------------------------------------------------------------
// fgSwitchToOptimized: Switch the opt level from tier 0 to optimized
//
// Assumptions:
//    - fgCanSwitchToOptimized() is true
//    - compSetOptimizationLevel() has not been called
//
// Notes:
//    This method is to be called at some point before compSetOptimizationLevel() to switch the opt level to optimized
//    based on information gathered in early phases.

void Compiler::fgSwitchToOptimized()
{
    assert(fgCanSwitchToOptimized());

    // Switch to optimized and re-init options
    JITDUMP("****\n**** JIT Tier0 jit request switching to Tier1 because of loop\n****\n");
    assert(opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0));
    opts.jitFlags->Clear(JitFlags::JIT_FLAG_TIER0);
    opts.jitFlags->Clear(JitFlags::JIT_FLAG_BBINSTR);

    // Leave a note for jit diagnostics
    compSwitchedToOptimized = true;

    compInitOptions(opts.jitFlags);

    // Notify the VM of the change
    info.compCompHnd->setMethodAttribs(info.compMethodHnd, CORINFO_FLG_SWITCHED_TO_OPTIMIZED);
}

//------------------------------------------------------------------------
// fgMayExplicitTailCall: Estimates conservatively for an explicit tail call, if the importer may actually use a tail
// call.
//
// Return Value:
//    - False if a tail call will not be generated
//    - True if a tail call *may* be generated
//
// Assumptions:
//    - compInitOptions() has been called
//    - info.compIsVarArgs has been initialized
//    - An explicit tail call has been seen
//    - compSetOptimizationLevel() has not been called

bool Compiler::fgMayExplicitTailCall()
{
    assert(!compIsForInlining());

    if (info.compFlags & CORINFO_FLG_SYNCH)
    {
        // Caller is synchronized
        return false;
    }

    if (opts.IsReversePInvoke())
    {
        // Reverse P/Invoke
        return false;
    }

#if !FEATURE_FIXED_OUT_ARGS
    if (info.compIsVarArgs)
    {
        // Caller is varargs
        return false;
    }
#endif // FEATURE_FIXED_OUT_ARGS

    return true;
}

//------------------------------------------------------------------------
// fgFindJumpTargets: walk the IL stream, determining jump target offsets
//
// Arguments:
//    codeAddr   - base address of the IL code buffer
//    codeSize   - number of bytes in the IL code buffer
//    jumpTarget - [OUT] bit vector for flagging jump targets
//
// Notes:
//    If inlining or prejitting the root, this method also makes
//    various observations about the method that factor into inline
//    decisions.
//
//    May throw an exception if the IL is malformed.
//
//    jumpTarget[N] is set to 1 if IL offset N is a jump target in the method.
//
//    Also sets lvAddrExposed and lvHasILStoreOp, ilHasMultipleILStoreOp in lvaTable[].

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif

//------------------------------------------------------------------------
// fgImport: read the IL for the method and create jit IR
//
// Returns:
//    phase status
//
PhaseStatus Compiler::fgImport()
{
    impImport();

    // Estimate how much of method IL was actually imported.
    //
    // Note this includes (to some extent) the impact of importer folded
    // branches, provided the folded tree covered the entire block's IL.
    unsigned importedILSize = 0;
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if ((block->bbFlags & BBF_IMPORTED) != 0)
        {
            // Assume if we generate any IR for the block we generate IR for the entire block.
            if (block->firstStmt() != nullptr)
            {
                IL_OFFSET beginOffset = block->bbCodeOffs;
                IL_OFFSET endOffset   = block->bbCodeOffsEnd;

                if ((beginOffset != BAD_IL_OFFSET) && (endOffset != BAD_IL_OFFSET) && (endOffset > beginOffset))
                {
                    unsigned blockILSize = endOffset - beginOffset;
                    importedILSize += blockILSize;
                }
            }
        }
    }

    // Could be tripped up if we ever duplicate blocks
    assert(importedILSize <= info.compILCodeSize);

    // Leave a note if we only did a partial import.
    if (importedILSize != info.compILCodeSize)
    {
        JITDUMP("\n** Note: %s IL was partially imported -- imported %u of %u bytes of method IL\n",
                compIsForInlining() ? "inlinee" : "root method", importedILSize, info.compILCodeSize);
    }

    // Record this for diagnostics and for the inliner's budget computations
    info.compILImportSize = importedILSize;

    if (compIsForInlining())
    {
        compInlineResult->SetImportedILSize(info.compILImportSize);
    }

    // Full preds are only used later on
    assert(!fgComputePredsDone);
    if (fgCheapPredsValid)
    {
        // Cheap predecessors are only used during importation
        fgRemovePreds();
    }

    return PhaseStatus::MODIFIED_EVERYTHING;
}

/*****************************************************************************
 * This function returns true if tree is a node with a call
 * that unconditionally throws an exception
 */

bool Compiler::fgIsThrow(GenTree* tree)
{
    if (!tree->IsCall())
    {
        return false;
    }
    GenTreeCall* call = tree->AsCall();
    if ((call->gtCallType == CT_HELPER) && s_helperCallProperties.AlwaysThrow(eeGetHelperNum(call->gtCallMethHnd)))
    {
        noway_assert(call->gtFlags & GTF_EXCEPT);
        return true;
    }
    return false;
}

/*****************************************************************************
 * This function returns true for blocks that are in different hot-cold regions.
 * It returns false when the blocks are both in the same regions
 */

bool Compiler::fgInDifferentRegions(BasicBlock* blk1, BasicBlock* blk2)
{
    noway_assert(blk1 != nullptr);
    noway_assert(blk2 != nullptr);

    if (fgFirstColdBlock == nullptr)
    {
        return false;
    }

    // If one block is Hot and the other is Cold then we are in different regions
    return ((blk1->bbFlags & BBF_COLD) != (blk2->bbFlags & BBF_COLD));
}

bool Compiler::fgIsBlockCold(BasicBlock* blk)
{
    noway_assert(blk != nullptr);

    if (fgFirstColdBlock == nullptr)
    {
        return false;
    }

    return ((blk->bbFlags & BBF_COLD) != 0);
}

/*****************************************************************************
 * This function returns true if tree is a GT_COMMA node with a call
 * that unconditionally throws an exception
 */

bool Compiler::fgIsCommaThrow(GenTree* tree, bool forFolding /* = false */)
{
    // Instead of always folding comma throws,
    // with stress enabled we only fold half the time

    if (forFolding && compStressCompile(STRESS_FOLD, 50))
    {
        return false; /* Don't fold */
    }

    /* Check for cast of a GT_COMMA with a throw overflow */
    if ((tree->gtOper == GT_COMMA) && (tree->gtFlags & GTF_CALL) && (tree->gtFlags & GTF_EXCEPT))
    {
        return (fgIsThrow(tree->AsOp()->gtOp1));
    }
    return false;
}

//------------------------------------------------------------------------
// fgIsIndirOfAddrOfLocal: Determine whether "tree" is an indirection of a local.
//
// Arguments:
//    tree - The tree node under consideration
//
// Return Value:
//    If "tree" is a indirection (GT_IND, GT_BLK, or GT_OBJ) whose arg is:
//    - an ADDR, whose arg in turn is a LCL_VAR, return that LCL_VAR node;
//    - a LCL_VAR_ADDR, return that LCL_VAR_ADDR;
//    - else nullptr.
//
// static
GenTreeLclVar* Compiler::fgIsIndirOfAddrOfLocal(GenTree* tree)
{
    GenTreeLclVar* res = nullptr;
    if (tree->OperIsIndir())
    {
        GenTree* addr = tree->AsIndir()->Addr();

        // Post rationalization, we can have Indir(Lea(..) trees. Therefore to recognize
        // Indir of addr of a local, skip over Lea in Indir(Lea(base, index, scale, offset))
        // to get to base variable.
        if (addr->OperGet() == GT_LEA)
        {
            // We use this method in backward dataflow after liveness computation - fgInterBlockLocalVarLiveness().
            // Therefore it is critical that we don't miss 'uses' of any local.  It may seem this method overlooks
            // if the index part of the LEA has indir( someAddrOperator ( lclVar ) ) to search for a use but it's
            // covered by the fact we're traversing the expression in execution order and we also visit the index.
            GenTreeAddrMode* lea  = addr->AsAddrMode();
            GenTree*         base = lea->Base();

            if (base != nullptr)
            {
                if (base->OperGet() == GT_IND)
                {
                    return fgIsIndirOfAddrOfLocal(base);
                }
                // else use base as addr
                addr = base;
            }
        }

        if (addr->OperGet() == GT_ADDR)
        {
            GenTree* lclvar = addr->AsOp()->gtOp1;
            if (lclvar->OperGet() == GT_LCL_VAR)
            {
                res = lclvar->AsLclVar();
            }
        }
        else if (addr->OperGet() == GT_LCL_VAR_ADDR)
        {
            res = addr->AsLclVar();
        }
    }
    return res;
}

GenTreeCall* Compiler::fgGetStaticsCCtorHelper(CORINFO_CLASS_HANDLE cls, CorInfoHelpFunc helper)
{
    bool     bNeedClassID = true;
    unsigned callFlags    = 0;

    var_types type = TYP_BYREF;

    // This is sort of ugly, as we have knowledge of what the helper is returning.
    // We need the return type.
    switch (helper)
    {
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR:
            bNeedClassID = false;
            FALLTHROUGH;

        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR:
            callFlags |= GTF_CALL_HOISTABLE;
            FALLTHROUGH;

        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE:
        case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS:
        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS:
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE:
        case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS:
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS:
            // type = TYP_BYREF;
            break;

        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR:
            bNeedClassID = false;
            FALLTHROUGH;

        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:
            callFlags |= GTF_CALL_HOISTABLE;
            FALLTHROUGH;

        case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE:
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE:
        case CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS:
            type = TYP_I_IMPL;
            break;

        default:
            assert(!"unknown shared statics helper");
            break;
    }

    GenTreeCall::Use* argList = nullptr;

    GenTree* opModuleIDArg;
    GenTree* opClassIDArg;

    // Get the class ID
    unsigned clsID;
    size_t   moduleID;
    void*    pclsID;
    void*    pmoduleID;

    clsID = info.compCompHnd->getClassDomainID(cls, &pclsID);

    moduleID = info.compCompHnd->getClassModuleIdForStatics(cls, nullptr, &pmoduleID);

    if (!(callFlags & GTF_CALL_HOISTABLE))
    {
        if (info.compCompHnd->getClassAttribs(cls) & CORINFO_FLG_BEFOREFIELDINIT)
        {
            callFlags |= GTF_CALL_HOISTABLE;
        }
    }

    if (pmoduleID)
    {
        opModuleIDArg = gtNewIndOfIconHandleNode(TYP_I_IMPL, (size_t)pmoduleID, GTF_ICON_CIDMID_HDL, true);
    }
    else
    {
        opModuleIDArg = gtNewIconNode((size_t)moduleID, TYP_I_IMPL);
    }

    if (bNeedClassID)
    {
        if (pclsID)
        {
            opClassIDArg = gtNewIndOfIconHandleNode(TYP_INT, (size_t)pclsID, GTF_ICON_CIDMID_HDL, true);
        }
        else
        {
            opClassIDArg = gtNewIconNode(clsID, TYP_INT);
        }

        // call the helper to get the base
        argList = gtNewCallArgs(opModuleIDArg, opClassIDArg);
    }
    else
    {
        argList = gtNewCallArgs(opModuleIDArg);
    }

    GenTreeCall* result = gtNewHelperCallNode(helper, type, argList);
    result->gtFlags |= callFlags;

    // If we're importing the special EqualityComparer<T>.Default or Comparer<T>.Default
    // intrinsics, flag the helper call. Later during inlining, we can
    // remove the helper call if the associated field lookup is unused.
    if ((info.compFlags & CORINFO_FLG_JIT_INTRINSIC) != 0)
    {
        NamedIntrinsic ni = lookupNamedIntrinsic(info.compMethodHnd);
        if ((ni == NI_System_Collections_Generic_EqualityComparer_get_Default) ||
            (ni == NI_System_Collections_Generic_Comparer_get_Default))
        {
            JITDUMP("\nmarking helper call [%06u] as special dce...\n", result->gtTreeID);
            result->gtCallMoreFlags |= GTF_CALL_M_HELPER_SPECIAL_DCE;
        }
    }

    return result;
}

GenTreeCall* Compiler::fgGetSharedCCtor(CORINFO_CLASS_HANDLE cls)
{
#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        CORINFO_RESOLVED_TOKEN resolvedToken;
        memset(&resolvedToken, 0, sizeof(resolvedToken));
        resolvedToken.hClass = cls;

        return impReadyToRunHelperToTree(&resolvedToken, CORINFO_HELP_READYTORUN_STATIC_BASE, TYP_BYREF);
    }
#endif

    // Call the shared non gc static helper, as its the fastest
    return fgGetStaticsCCtorHelper(cls, info.compCompHnd->getSharedCCtorHelper(cls));
}

//------------------------------------------------------------------------------
// fgAddrCouldBeNull : Check whether the address tree can represent null.
//
//
// Arguments:
//    addr     -  Address to check
//
// Return Value:
//    True if address could be null; false otherwise

bool Compiler::fgAddrCouldBeNull(GenTree* addr)
{
    addr = addr->gtEffectiveVal();
    if ((addr->gtOper == GT_CNS_INT) && addr->IsIconHandle())
    {
        return false;
    }
    else if (addr->OperIs(GT_CNS_STR))
    {
        return false;
    }
    else if (addr->gtOper == GT_LCL_VAR)
    {
        unsigned varNum = addr->AsLclVarCommon()->GetLclNum();

        if (lvaIsImplicitByRefLocal(varNum))
        {
            return false;
        }

        LclVarDsc* varDsc = &lvaTable[varNum];

        if (varDsc->lvStackByref)
        {
            return false;
        }
    }
    else if (addr->gtOper == GT_ADDR)
    {
        if (addr->AsOp()->gtOp1->gtOper == GT_CNS_INT)
        {
            GenTree* cns1Tree = addr->AsOp()->gtOp1;
            if (!cns1Tree->IsIconHandle())
            {
                // Indirection of some random constant...
                // It is safest just to return true
                return true;
            }
        }

        return false; // we can't have a null address
    }
    else if (addr->gtOper == GT_ADD)
    {
        if (addr->AsOp()->gtOp1->gtOper == GT_CNS_INT)
        {
            GenTree* cns1Tree = addr->AsOp()->gtOp1;
            if (!cns1Tree->IsIconHandle())
            {
                if (!fgIsBigOffset(cns1Tree->AsIntCon()->gtIconVal))
                {
                    // Op1 was an ordinary small constant
                    return fgAddrCouldBeNull(addr->AsOp()->gtOp2);
                }
            }
            else // Op1 was a handle represented as a constant
            {
                // Is Op2 also a constant?
                if (addr->AsOp()->gtOp2->gtOper == GT_CNS_INT)
                {
                    GenTree* cns2Tree = addr->AsOp()->gtOp2;
                    // Is this an addition of a handle and constant
                    if (!cns2Tree->IsIconHandle())
                    {
                        if (!fgIsBigOffset(cns2Tree->AsIntCon()->gtIconVal))
                        {
                            // Op2 was an ordinary small constant
                            return false; // we can't have a null address
                        }
                    }
                }
            }
        }
        else
        {
            // Op1 is not a constant
            // What about Op2?
            if (addr->AsOp()->gtOp2->gtOper == GT_CNS_INT)
            {
                GenTree* cns2Tree = addr->AsOp()->gtOp2;
                // Is this an addition of a small constant
                if (!cns2Tree->IsIconHandle())
                {
                    if (!fgIsBigOffset(cns2Tree->AsIntCon()->gtIconVal))
                    {
                        // Op2 was an ordinary small constant
                        return fgAddrCouldBeNull(addr->AsOp()->gtOp1);
                    }
                }
            }
        }
    }
    return true; // default result: addr could be null
}

//------------------------------------------------------------------------------
// fgOptimizeDelegateConstructor: try and optimize construction of a delegate
//
// Arguments:
//    call -- call to original delegate constructor
//    exactContextHnd -- [out] context handle to update
//    ldftnToken -- [in]  resolved token for the method the delegate will invoke,
//      if known, or nullptr if not known
//
// Return Value:
//    Original call tree if no optimization applies.
//    Updated call tree if optimized.

GenTree* Compiler::fgOptimizeDelegateConstructor(GenTreeCall*            call,
                                                 CORINFO_CONTEXT_HANDLE* ExactContextHnd,
                                                 CORINFO_RESOLVED_TOKEN* ldftnToken)
{
    JITDUMP("\nfgOptimizeDelegateConstructor: ");
    noway_assert(call->gtCallType == CT_USER_FUNC);
    CORINFO_METHOD_HANDLE methHnd = call->gtCallMethHnd;
    CORINFO_CLASS_HANDLE  clsHnd  = info.compCompHnd->getMethodClass(methHnd);

    GenTree* targetMethod = call->gtCallArgs->GetNext()->GetNode();
    noway_assert(targetMethod->TypeGet() == TYP_I_IMPL);
    genTreeOps            oper            = targetMethod->OperGet();
    CORINFO_METHOD_HANDLE targetMethodHnd = nullptr;
    GenTree*              qmarkNode       = nullptr;
    if (oper == GT_FTN_ADDR)
    {
        targetMethodHnd = targetMethod->AsFptrVal()->gtFptrMethod;
    }
    else if (oper == GT_CALL && targetMethod->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_VIRTUAL_FUNC_PTR))
    {
        GenTree* handleNode = targetMethod->AsCall()->gtCallArgs->GetNext()->GetNext()->GetNode();

        if (handleNode->OperGet() == GT_CNS_INT)
        {
            // it's a ldvirtftn case, fetch the methodhandle off the helper for ldvirtftn. It's the 3rd arg
            targetMethodHnd = CORINFO_METHOD_HANDLE(handleNode->AsIntCon()->gtCompileTimeHandle);
        }
        // Sometimes the argument to this is the result of a generic dictionary lookup, which shows
        // up as a GT_QMARK.
        else if (handleNode->OperGet() == GT_QMARK)
        {
            qmarkNode = handleNode;
        }
    }
    // Sometimes we don't call CORINFO_HELP_VIRTUAL_FUNC_PTR but instead just call
    // CORINFO_HELP_RUNTIMEHANDLE_METHOD directly.
    else if (oper == GT_QMARK)
    {
        qmarkNode = targetMethod;
    }
    if (qmarkNode)
    {
        noway_assert(qmarkNode->OperGet() == GT_QMARK);
        // The argument is actually a generic dictionary lookup.  For delegate creation it looks
        // like:
        // GT_QMARK
        //  GT_COLON
        //      op1 -> call
        //              Arg 1 -> token (has compile time handle)
        //      op2 -> lclvar
        //
        //
        // In this case I can find the token (which is a method handle) and that is the compile time
        // handle.
        noway_assert(qmarkNode->AsOp()->gtOp2->OperGet() == GT_COLON);
        noway_assert(qmarkNode->AsOp()->gtOp2->AsOp()->gtOp1->OperGet() == GT_CALL);
        GenTreeCall* runtimeLookupCall = qmarkNode->AsOp()->gtOp2->AsOp()->gtOp1->AsCall();

        // This could be any of CORINFO_HELP_RUNTIMEHANDLE_(METHOD|CLASS)(_LOG?)
        GenTree* tokenNode = runtimeLookupCall->gtCallArgs->GetNext()->GetNode();
        noway_assert(tokenNode->OperGet() == GT_CNS_INT);
        targetMethodHnd = CORINFO_METHOD_HANDLE(tokenNode->AsIntCon()->gtCompileTimeHandle);
    }

    // Verify using the ldftnToken gives us all of what we used to get
    // via the above pattern match, and more...
    if (ldftnToken != nullptr)
    {
        assert(ldftnToken->hMethod != nullptr);

        if (targetMethodHnd != nullptr)
        {
            assert(targetMethodHnd == ldftnToken->hMethod);
        }

        targetMethodHnd = ldftnToken->hMethod;
    }
    else
    {
        assert(targetMethodHnd == nullptr);
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (opts.IsReadyToRun())
    {
        if (IsTargetAbi(CORINFO_CORERT_ABI))
        {
            if (ldftnToken != nullptr)
            {
                JITDUMP("optimized\n");

                GenTree*             thisPointer       = call->gtCallThisArg->GetNode();
                GenTree*             targetObjPointers = call->gtCallArgs->GetNode();
                GenTreeCall::Use*    helperArgs        = nullptr;
                CORINFO_LOOKUP       pLookup;
                CORINFO_CONST_LOOKUP entryPoint;
                info.compCompHnd->getReadyToRunDelegateCtorHelper(ldftnToken, clsHnd, &pLookup);
                if (!pLookup.lookupKind.needsRuntimeLookup)
                {
                    helperArgs = gtNewCallArgs(thisPointer, targetObjPointers);
                    entryPoint = pLookup.constLookup;
                }
                else
                {
                    assert(oper != GT_FTN_ADDR);
                    CORINFO_CONST_LOOKUP genericLookup;
                    info.compCompHnd->getReadyToRunHelper(ldftnToken, &pLookup.lookupKind,
                                                          CORINFO_HELP_READYTORUN_GENERIC_HANDLE, &genericLookup);
                    GenTree* ctxTree = getRuntimeContextTree(pLookup.lookupKind.runtimeLookupKind);
                    helperArgs       = gtNewCallArgs(thisPointer, targetObjPointers, ctxTree);
                    entryPoint       = genericLookup;
                }
                call = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_DELEGATE_CTOR, TYP_VOID, helperArgs);
                call->setEntryPoint(entryPoint);
            }
            else
            {
                JITDUMP("not optimized, CORERT no ldftnToken\n");
            }
        }
        // ReadyToRun has this optimization for a non-virtual function pointers only for now.
        else if (oper == GT_FTN_ADDR)
        {
            JITDUMP("optimized\n");

            GenTree*          thisPointer       = call->gtCallThisArg->GetNode();
            GenTree*          targetObjPointers = call->gtCallArgs->GetNode();
            GenTreeCall::Use* helperArgs        = gtNewCallArgs(thisPointer, targetObjPointers);

            call = gtNewHelperCallNode(CORINFO_HELP_READYTORUN_DELEGATE_CTOR, TYP_VOID, helperArgs);

            CORINFO_LOOKUP entryPoint;
            info.compCompHnd->getReadyToRunDelegateCtorHelper(ldftnToken, clsHnd, &entryPoint);
            assert(!entryPoint.lookupKind.needsRuntimeLookup);
            call->setEntryPoint(entryPoint.constLookup);
        }
        else
        {
            JITDUMP("not optimized, R2R virtual case\n");
        }
    }
    else
#endif
        if (targetMethodHnd != nullptr)
    {
        CORINFO_METHOD_HANDLE alternateCtor = nullptr;
        DelegateCtorArgs      ctorData;
        ctorData.pMethod = info.compMethodHnd;
        ctorData.pArg3   = nullptr;
        ctorData.pArg4   = nullptr;
        ctorData.pArg5   = nullptr;

        alternateCtor = info.compCompHnd->GetDelegateCtor(methHnd, clsHnd, targetMethodHnd, &ctorData);
        if (alternateCtor != methHnd)
        {
            JITDUMP("optimized\n");
            // we erase any inline info that may have been set for generics has it is not needed here,
            // and in fact it will pass the wrong info to the inliner code
            *ExactContextHnd = nullptr;

            call->gtCallMethHnd = alternateCtor;

            noway_assert(call->gtCallArgs->GetNext()->GetNext() == nullptr);
            GenTreeCall::Use* addArgs = nullptr;
            if (ctorData.pArg5)
            {
                GenTree* arg5 = gtNewIconHandleNode(size_t(ctorData.pArg5), GTF_ICON_FTN_ADDR);
                addArgs       = gtPrependNewCallArg(arg5, addArgs);
            }
            if (ctorData.pArg4)
            {
                GenTree* arg4 = gtNewIconHandleNode(size_t(ctorData.pArg4), GTF_ICON_FTN_ADDR);
                addArgs       = gtPrependNewCallArg(arg4, addArgs);
            }
            if (ctorData.pArg3)
            {
                GenTree* arg3 = gtNewIconHandleNode(size_t(ctorData.pArg3), GTF_ICON_FTN_ADDR);
                addArgs       = gtPrependNewCallArg(arg3, addArgs);
            }
            call->gtCallArgs->GetNext()->SetNext(addArgs);
        }
        else
        {
            JITDUMP("not optimized, no alternate ctor\n");
        }
    }
    else
    {
        JITDUMP("not optimized, no target method\n");
    }
    return call;
}

bool Compiler::fgCastNeeded(GenTree* tree, var_types toType)
{
    //
    // If tree is a relop and we need an 4-byte integer
    //  then we never need to insert a cast
    //
    if ((tree->OperKind() & GTK_RELOP) && (genActualType(toType) == TYP_INT))
    {
        return false;
    }

    var_types fromType;

    //
    // Is the tree as GT_CAST or a GT_CALL ?
    //
    if (tree->OperGet() == GT_CAST)
    {
        fromType = tree->CastToType();
    }
    else if (tree->OperGet() == GT_CALL)
    {
        fromType = (var_types)tree->AsCall()->gtReturnType;
    }
    else
    {
        fromType = tree->TypeGet();
    }

    //
    // If both types are the same then an additional cast is not necessary
    //
    if (toType == fromType)
    {
        return false;
    }
    //
    // If the sign-ness of the two types are different then a cast is necessary
    //
    if (varTypeIsUnsigned(toType) != varTypeIsUnsigned(fromType))
    {
        return true;
    }
    //
    // If the from type is the same size or smaller then an additional cast is not necessary
    //
    if (genTypeSize(toType) >= genTypeSize(fromType))
    {
        return false;
    }

    //
    // Looks like we will need the cast
    //
    return true;
}

// If assigning to a local var, add a cast if the target is
// marked as NormalizedOnStore. Returns true if any change was made
GenTree* Compiler::fgDoNormalizeOnStore(GenTree* tree)
{
    //
    // Only normalize the stores in the global morph phase
    //
    if (fgGlobalMorph)
    {
        noway_assert(tree->OperGet() == GT_ASG);

        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->AsOp()->gtOp2;

        if (op1->gtOper == GT_LCL_VAR && genActualType(op1->TypeGet()) == TYP_INT)
        {
            // Small-typed arguments and aliased locals are normalized on load.
            // Other small-typed locals are normalized on store.
            // If it is an assignment to one of the latter, insert the cast on RHS
            unsigned   varNum = op1->AsLclVarCommon()->GetLclNum();
            LclVarDsc* varDsc = &lvaTable[varNum];

            if (varDsc->lvNormalizeOnStore())
            {
                noway_assert(op1->gtType <= TYP_INT);
                op1->gtType = TYP_INT;

                if (fgCastNeeded(op2, varDsc->TypeGet()))
                {
                    op2                 = gtNewCastNode(TYP_INT, op2, false, varDsc->TypeGet());
                    tree->AsOp()->gtOp2 = op2;

                    // Propagate GTF_COLON_COND
                    op2->gtFlags |= (tree->gtFlags & GTF_COLON_COND);
                }
            }
        }
    }

    return tree;
}

/*****************************************************************************
 *
 *  Mark whether the edge "srcBB -> dstBB" forms a loop that will always
 *  execute a call or not.
 */

inline void Compiler::fgLoopCallTest(BasicBlock* srcBB, BasicBlock* dstBB)
{
    /* Bail if this is not a backward edge */

    if (srcBB->bbNum < dstBB->bbNum)
    {
        return;
    }

    /* Unless we already know that there is a loop without a call here ... */

    if (!(dstBB->bbFlags & BBF_LOOP_CALL0))
    {
        /* Check whether there is a loop path that doesn't call */

        if (optReachWithoutCall(dstBB, srcBB))
        {
            dstBB->bbFlags |= BBF_LOOP_CALL0;
            dstBB->bbFlags &= ~BBF_LOOP_CALL1;
        }
        else
        {
            dstBB->bbFlags |= BBF_LOOP_CALL1;
        }
    }
}

/*****************************************************************************
 *
 *  Mark which loops are guaranteed to execute a call.
 */

void Compiler::fgLoopCallMark()
{
    BasicBlock* block;

    /* If we've already marked all the block, bail */

    if (fgLoopCallMarked)
    {
        return;
    }

    fgLoopCallMarked = true;

    /* Walk the blocks, looking for backward edges */

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        switch (block->bbJumpKind)
        {
            case BBJ_COND:
            case BBJ_CALLFINALLY:
            case BBJ_ALWAYS:
            case BBJ_EHCATCHRET:
                fgLoopCallTest(block, block->bbJumpDest);
                break;

            case BBJ_SWITCH:

                unsigned jumpCnt;
                jumpCnt = block->bbJumpSwt->bbsCount;
                BasicBlock** jumpPtr;
                jumpPtr = block->bbJumpSwt->bbsDstTab;

                do
                {
                    fgLoopCallTest(block, *jumpPtr);
                } while (++jumpPtr, --jumpCnt);

                break;

            default:
                break;
        }
    }
}

/*****************************************************************************
 *
 *  Note the fact that the given block is a loop header.
 */

inline void Compiler::fgMarkLoopHead(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("fgMarkLoopHead: Checking loop head block " FMT_BB ": ", block->bbNum);
    }
#endif

    /* Have we decided to generate fully interruptible code already? */

    if (GetInterruptible())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("method is already fully interruptible\n");
        }
#endif
        return;
    }

    /* Is the loop head block known to execute a method call? */

    if (block->bbFlags & BBF_GC_SAFE_POINT)
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("this block will execute a call\n");
        }
#endif
        return;
    }

    /* Are dominator sets available? */

    if (fgDomsComputed)
    {
        /* Make sure that we know which loops will always execute calls */

        if (!fgLoopCallMarked)
        {
            fgLoopCallMark();
        }

        /* Will every trip through our loop execute a call? */

        if (block->bbFlags & BBF_LOOP_CALL1)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("this block dominates a block that will execute a call\n");
            }
#endif
            return;
        }
    }

    /*
     *  We have to make this method fully interruptible since we can not
     *  ensure that this loop will execute a call every time it loops.
     *
     *  We'll also need to generate a full register map for this method.
     */

    assert(!codeGen->isGCTypeFixed());

    if (!compCanEncodePtrArgCntMax())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("a callsite with more than 1023 pushed args exists\n");
        }
#endif
        return;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("no guaranteed callsite exits, marking method as fully interruptible\n");
    }
#endif
    SetInterruptible(true);
}

GenTree* Compiler::fgGetCritSectOfStaticMethod()
{
    noway_assert(!compIsForInlining());

    noway_assert(info.compIsStatic); // This method should only be called for static methods.

    GenTree* tree = nullptr;

    CORINFO_LOOKUP_KIND kind;
    info.compCompHnd->getLocationOfThisType(info.compMethodHnd, &kind);

    if (!kind.needsRuntimeLookup)
    {
        void *critSect = nullptr, **pCrit = nullptr;
        critSect = info.compCompHnd->getMethodSync(info.compMethodHnd, (void**)&pCrit);
        noway_assert((!critSect) != (!pCrit));

        tree = gtNewIconEmbHndNode(critSect, pCrit, GTF_ICON_METHOD_HDL, info.compMethodHnd);
    }
    else
    {
        // Collectible types requires that for shared generic code, if we use the generic context paramter
        // that we report it. (This is a conservative approach, we could detect some cases particularly when the
        // context parameter is this that we don't need the eager reporting logic.)
        lvaGenericsContextInUse = true;

        switch (kind.runtimeLookupKind)
        {
            case CORINFO_LOOKUP_THISOBJ:
            {
                noway_assert(!"Should never get this for static method.");
                break;
            }

            case CORINFO_LOOKUP_CLASSPARAM:
            {
                // In this case, the hidden param is the class handle.
                tree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                tree->gtFlags |= GTF_VAR_CONTEXT;
                break;
            }

            case CORINFO_LOOKUP_METHODPARAM:
            {
                // In this case, the hidden param is the method handle.
                tree = gtNewLclvNode(info.compTypeCtxtArg, TYP_I_IMPL);
                tree->gtFlags |= GTF_VAR_CONTEXT;
                // Call helper CORINFO_HELP_GETCLASSFROMMETHODPARAM to get the class handle
                // from the method handle.
                tree = gtNewHelperCallNode(CORINFO_HELP_GETCLASSFROMMETHODPARAM, TYP_I_IMPL, gtNewCallArgs(tree));
                break;
            }

            default:
            {
                noway_assert(!"Unknown LOOKUP_KIND");
                break;
            }
        }

        noway_assert(tree); // tree should now contain the CORINFO_CLASS_HANDLE for the exact class.

        // Given the class handle, get the pointer to the Monitor.
        tree = gtNewHelperCallNode(CORINFO_HELP_GETSYNCFROMCLASSHANDLE, TYP_I_IMPL, gtNewCallArgs(tree));
    }

    noway_assert(tree);
    return tree;
}

#if defined(FEATURE_EH_FUNCLETS)

/*****************************************************************************
 *
 *  Add monitor enter/exit calls for synchronized methods, and a try/fault
 *  to ensure the 'exit' is called if the 'enter' was successful. On x86, we
 *  generate monitor enter/exit calls and tell the VM the code location of
 *  these calls. When an exception occurs between those locations, the VM
 *  automatically releases the lock. For non-x86 platforms, the JIT is
 *  responsible for creating a try/finally to protect the monitor enter/exit,
 *  and the VM doesn't need to know anything special about the method during
 *  exception processing -- it's just a normal try/finally.
 *
 *  We generate the following code:
 *
 *      void Foo()
 *      {
 *          unsigned byte acquired = 0;
 *          try {
 *              JIT_MonEnterWorker(<lock object>, &acquired);
 *
 *              *** all the preexisting user code goes here ***
 *
 *              JIT_MonExitWorker(<lock object>, &acquired);
 *          } fault {
 *              JIT_MonExitWorker(<lock object>, &acquired);
 *         }
 *      L_return:
 *         ret
 *      }
 *
 *  If the lock is actually acquired, then the 'acquired' variable is set to 1
 *  by the helper call. During normal exit, the finally is called, 'acquired'
 *  is 1, and the lock is released. If an exception occurs before the lock is
 *  acquired, but within the 'try' (extremely unlikely, but possible), 'acquired'
 *  will be 0, and the monitor exit call will quickly return without attempting
 *  to release the lock. Otherwise, 'acquired' will be 1, and the lock will be
 *  released during exception processing.
 *
 *  For synchronized methods, we generate a single return block.
 *  We can do this without creating additional "step" blocks because "ret" blocks
 *  must occur at the top-level (of the original code), not nested within any EH
 *  constructs. From the CLI spec, 12.4.2.8.2.3 "ret": "Shall not be enclosed in any
 *  protected block, filter, or handler." Also, 3.57: "The ret instruction cannot be
 *  used to transfer control out of a try, filter, catch, or finally block. From within
 *  a try or catch, use the leave instruction with a destination of a ret instruction
 *  that is outside all enclosing exception blocks."
 *
 *  In addition, we can add a "fault" at the end of a method and be guaranteed that no
 *  control falls through. From the CLI spec, section 12.4 "Control flow": "Control is not
 *  permitted to simply fall through the end of a method. All paths shall terminate with one
 *  of these instructions: ret, throw, jmp, or (tail. followed by call, calli, or callvirt)."
 *
 *  We only need to worry about "ret" and "throw", as the CLI spec prevents any other
 *  alternatives. Section 15.4.3.3 "Implementation information" states about exiting
 *  synchronized methods: "Exiting a synchronized method using a tail. call shall be
 *  implemented as though the tail. had not been specified." Section 3.37 "jmp" states:
 *  "The jmp instruction cannot be used to transferred control out of a try, filter,
 *  catch, fault or finally block; or out of a synchronized region." And, "throw" will
 *  be handled naturally; no additional work is required.
 */

void Compiler::fgAddSyncMethodEnterExit()
{
    assert((info.compFlags & CORINFO_FLG_SYNCH) != 0);

    // We need to do this transformation before funclets are created.
    assert(!fgFuncletsCreated);

    // Assume we don't need to update the bbPreds lists.
    assert(!fgComputePredsDone);

#if !FEATURE_EH
    // If we don't support EH, we can't add the EH needed by synchronized methods.
    // Of course, we could simply ignore adding the EH constructs, since we don't
    // support exceptions being thrown in this mode, but we would still need to add
    // the monitor enter/exit, and that doesn't seem worth it for this minor case.
    // By the time EH is working, we can just enable the whole thing.
    NYI("No support for synchronized methods");
#endif // !FEATURE_EH

    // Create a scratch first BB where we can put the new variable initialization.
    // Don't put the scratch BB in the protected region.

    fgEnsureFirstBBisScratch();

    // Create a block for the start of the try region, where the monitor enter call
    // will go.

    assert(fgFirstBB->bbFallsThrough());

    BasicBlock* tryBegBB  = fgNewBBafter(BBJ_NONE, fgFirstBB, false);
    BasicBlock* tryNextBB = tryBegBB->bbNext;
    BasicBlock* tryLastBB = fgLastBB;

    // If we have profile data the new block will inherit the next block's weight
    if (tryNextBB->hasProfileWeight())
    {
        tryBegBB->inheritWeight(tryNextBB);
    }

    // Create a block for the fault.

    assert(!tryLastBB->bbFallsThrough());
    BasicBlock* faultBB = fgNewBBafter(BBJ_EHFINALLYRET, tryLastBB, false);

    assert(tryLastBB->bbNext == faultBB);
    assert(faultBB->bbNext == nullptr);
    assert(faultBB == fgLastBB);

    { // Scope the EH region creation

        // Add the new EH region at the end, since it is the least nested,
        // and thus should be last.

        EHblkDsc* newEntry;
        unsigned  XTnew = compHndBBtabCount;

        newEntry = fgAddEHTableEntry(XTnew);

        // Initialize the new entry

        newEntry->ebdHandlerType = EH_HANDLER_FAULT;

        newEntry->ebdTryBeg  = tryBegBB;
        newEntry->ebdTryLast = tryLastBB;

        newEntry->ebdHndBeg  = faultBB;
        newEntry->ebdHndLast = faultBB;

        newEntry->ebdTyp = 0; // unused for fault

        newEntry->ebdEnclosingTryIndex = EHblkDsc::NO_ENCLOSING_INDEX;
        newEntry->ebdEnclosingHndIndex = EHblkDsc::NO_ENCLOSING_INDEX;

        newEntry->ebdTryBegOffset    = tryBegBB->bbCodeOffs;
        newEntry->ebdTryEndOffset    = tryLastBB->bbCodeOffsEnd;
        newEntry->ebdFilterBegOffset = 0;
        newEntry->ebdHndBegOffset    = 0; // handler doesn't correspond to any IL
        newEntry->ebdHndEndOffset    = 0; // handler doesn't correspond to any IL

        // Set some flags on the new region. This is the same as when we set up
        // EH regions in fgFindBasicBlocks(). Note that the try has no enclosing
        // handler, and the fault has no enclosing try.

        tryBegBB->bbFlags |= BBF_DONT_REMOVE | BBF_TRY_BEG | BBF_IMPORTED;

        faultBB->bbFlags |= BBF_DONT_REMOVE | BBF_IMPORTED;
        faultBB->bbCatchTyp = BBCT_FAULT;

        tryBegBB->setTryIndex(XTnew);
        tryBegBB->clearHndIndex();

        faultBB->clearTryIndex();
        faultBB->setHndIndex(XTnew);

        // Walk the user code blocks and set all blocks that don't already have a try handler
        // to point to the new try handler.

        BasicBlock* tmpBB;
        for (tmpBB = tryBegBB->bbNext; tmpBB != faultBB; tmpBB = tmpBB->bbNext)
        {
            if (!tmpBB->hasTryIndex())
            {
                tmpBB->setTryIndex(XTnew);
            }
        }

        // Walk the EH table. Make every EH entry that doesn't already have an enclosing
        // try index mark this new entry as their enclosing try index.

        unsigned  XTnum;
        EHblkDsc* HBtab;

        for (XTnum = 0, HBtab = compHndBBtab; XTnum < XTnew; XTnum++, HBtab++)
        {
            if (HBtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                HBtab->ebdEnclosingTryIndex =
                    (unsigned short)XTnew; // This EH region wasn't previously nested, but now it is.
            }
        }

#ifdef DEBUG
        if (verbose)
        {
            JITDUMP("Synchronized method - created additional EH descriptor EH#%u for try/fault wrapping monitor "
                    "enter/exit\n",
                    XTnew);
            fgDispBasicBlocks();
            fgDispHandlerTab();
        }

        fgVerifyHandlerTab();
#endif // DEBUG
    }

    // Create a 'monitor acquired' boolean (actually, an unsigned byte: 1 = acquired, 0 = not acquired).

    var_types typeMonAcquired = TYP_UBYTE;
    this->lvaMonAcquired      = lvaGrabTemp(true DEBUGARG("Synchronized method monitor acquired boolean"));

    lvaTable[lvaMonAcquired].lvType = typeMonAcquired;

    { // Scope the variables of the variable initialization

        // Initialize the 'acquired' boolean.

        GenTree* zero     = gtNewZeroConNode(genActualType(typeMonAcquired));
        GenTree* varNode  = gtNewLclvNode(lvaMonAcquired, typeMonAcquired);
        GenTree* initNode = gtNewAssignNode(varNode, zero);

        fgNewStmtAtEnd(fgFirstBB, initNode);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nSynchronized method - Add 'acquired' initialization in first block %s\n",
                   fgFirstBB->dspToString());
            gtDispTree(initNode);
            printf("\n");
        }
#endif
    }

    // Make a copy of the 'this' pointer to be used in the handler so it does not inhibit enregistration
    // of all uses of the variable.
    unsigned lvaCopyThis = 0;
    if (!info.compIsStatic)
    {
        lvaCopyThis                  = lvaGrabTemp(true DEBUGARG("Synchronized method monitor acquired boolean"));
        lvaTable[lvaCopyThis].lvType = TYP_REF;

        GenTree* thisNode = gtNewLclvNode(info.compThisArg, TYP_REF);
        GenTree* copyNode = gtNewLclvNode(lvaCopyThis, TYP_REF);
        GenTree* initNode = gtNewAssignNode(copyNode, thisNode);

        fgNewStmtAtEnd(tryBegBB, initNode);
    }

    fgCreateMonitorTree(lvaMonAcquired, info.compThisArg, tryBegBB, true /*enter*/);

    // exceptional case
    fgCreateMonitorTree(lvaMonAcquired, lvaCopyThis, faultBB, false /*exit*/);

    // non-exceptional cases
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbJumpKind == BBJ_RETURN)
        {
            fgCreateMonitorTree(lvaMonAcquired, info.compThisArg, block, false /*exit*/);
        }
    }
}

// fgCreateMonitorTree: Create tree to execute a monitor enter or exit operation for synchronized methods
//    lvaMonAcquired: lvaNum of boolean variable that tracks if monitor has been acquired.
//    lvaThisVar: lvaNum of variable being used as 'this' pointer, may not be the original one.  Is only used for
//    nonstatic methods
//    block: block to insert the tree in.  It is inserted at the end or in the case of a return, immediately before the
//    GT_RETURN
//    enter: whether to create a monitor enter or exit

GenTree* Compiler::fgCreateMonitorTree(unsigned lvaMonAcquired, unsigned lvaThisVar, BasicBlock* block, bool enter)
{
    // Insert the expression "enter/exitCrit(this, &acquired)" or "enter/exitCrit(handle, &acquired)"

    var_types typeMonAcquired = TYP_UBYTE;
    GenTree*  varNode         = gtNewLclvNode(lvaMonAcquired, typeMonAcquired);
    GenTree*  varAddrNode     = gtNewOperNode(GT_ADDR, TYP_BYREF, varNode);
    GenTree*  tree;

    if (info.compIsStatic)
    {
        tree = fgGetCritSectOfStaticMethod();
        tree = gtNewHelperCallNode(enter ? CORINFO_HELP_MON_ENTER_STATIC : CORINFO_HELP_MON_EXIT_STATIC, TYP_VOID,
                                   gtNewCallArgs(tree, varAddrNode));
    }
    else
    {
        tree = gtNewLclvNode(lvaThisVar, TYP_REF);
        tree = gtNewHelperCallNode(enter ? CORINFO_HELP_MON_ENTER : CORINFO_HELP_MON_EXIT, TYP_VOID,
                                   gtNewCallArgs(tree, varAddrNode));
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nSynchronized method - Add monitor %s call to block %s\n", enter ? "enter" : "exit",
               block->dspToString());
        gtDispTree(tree);
        printf("\n");
    }
#endif

    if (block->bbJumpKind == BBJ_RETURN && block->lastStmt()->GetRootNode()->gtOper == GT_RETURN)
    {
        GenTree* retNode = block->lastStmt()->GetRootNode();
        GenTree* retExpr = retNode->AsOp()->gtOp1;

        if (retExpr != nullptr)
        {
            // have to insert this immediately before the GT_RETURN so we transform:
            // ret(...) ->
            // ret(comma(comma(tmp=...,call mon_exit), tmp)
            //
            //
            // Before morph stage, it is possible to have a case of GT_RETURN(TYP_LONG, op1) where op1's type is
            // TYP_STRUCT (of 8-bytes) and op1 is call node. See the big comment block in impReturnInstruction()
            // for details for the case where info.compRetType is not the same as info.compRetNativeType.  For
            // this reason pass compMethodInfo->args.retTypeClass which is guaranteed to be a valid class handle
            // if the return type is a value class.  Note that fgInsertCommFormTemp() in turn uses this class handle
            // if the type of op1 is TYP_STRUCT to perform lvaSetStruct() on the new temp that is created, which
            // in turn passes it to VM to know the size of value type.
            GenTree* temp = fgInsertCommaFormTemp(&retNode->AsOp()->gtOp1, info.compMethodInfo->args.retTypeClass);

            GenTree* lclVar = retNode->AsOp()->gtOp1->AsOp()->gtOp2;

            // The return can't handle all of the trees that could be on the right-hand-side of an assignment,
            // especially in the case of a struct. Therefore, we need to propagate GTF_DONT_CSE.
            // If we don't, assertion propagation may, e.g., change a return of a local to a return of "CNS_INT   struct
            // 0",
            // which downstream phases can't handle.
            lclVar->gtFlags |= (retExpr->gtFlags & GTF_DONT_CSE);
            retNode->AsOp()->gtOp1->AsOp()->gtOp2 = gtNewOperNode(GT_COMMA, retExpr->TypeGet(), tree, lclVar);
        }
        else
        {
            // Insert this immediately before the GT_RETURN
            fgNewStmtNearEnd(block, tree);
        }
    }
    else
    {
        fgNewStmtAtEnd(block, tree);
    }

    return tree;
}

// Convert a BBJ_RETURN block in a synchronized method to a BBJ_ALWAYS.
// We've previously added a 'try' block around the original program code using fgAddSyncMethodEnterExit().
// Thus, we put BBJ_RETURN blocks inside a 'try'. In IL this is illegal. Instead, we would
// see a 'leave' inside a 'try' that would get transformed into BBJ_CALLFINALLY/BBJ_ALWAYS blocks
// during importing, and the BBJ_ALWAYS would point at an outer block with the BBJ_RETURN.
// Here, we mimic some of the logic of importing a LEAVE to get the same effect for synchronized methods.
void Compiler::fgConvertSyncReturnToLeave(BasicBlock* block)
{
    assert(!fgFuncletsCreated);
    assert(info.compFlags & CORINFO_FLG_SYNCH);
    assert(genReturnBB != nullptr);
    assert(genReturnBB != block);
    assert(fgReturnCount <= 1); // We have a single return for synchronized methods
    assert(block->bbJumpKind == BBJ_RETURN);
    assert((block->bbFlags & BBF_HAS_JMP) == 0);
    assert(block->hasTryIndex());
    assert(!block->hasHndIndex());
    assert(compHndBBtabCount >= 1);

    unsigned tryIndex = block->getTryIndex();
    assert(tryIndex == compHndBBtabCount - 1); // The BBJ_RETURN must be at the top-level before we inserted the
                                               // try/finally, which must be the last EH region.

    EHblkDsc* ehDsc = ehGetDsc(tryIndex);
    assert(ehDsc->ebdEnclosingTryIndex ==
           EHblkDsc::NO_ENCLOSING_INDEX); // There are no enclosing regions of the BBJ_RETURN block
    assert(ehDsc->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX);

    // Convert the BBJ_RETURN to BBJ_ALWAYS, jumping to genReturnBB.
    block->bbJumpKind = BBJ_ALWAYS;
    block->bbJumpDest = genReturnBB;
    fgAddRefPred(genReturnBB, block);

#ifdef DEBUG
    if (verbose)
    {
        printf("Synchronized method - convert block " FMT_BB " to BBJ_ALWAYS [targets " FMT_BB "]\n", block->bbNum,
               block->bbJumpDest->bbNum);
    }
#endif
}

#endif // FEATURE_EH_FUNCLETS

//------------------------------------------------------------------------
// fgAddReversePInvokeEnterExit: Add enter/exit calls for reverse PInvoke methods
//
// Arguments:
//      None.
//
// Return Value:
//      None.

void Compiler::fgAddReversePInvokeEnterExit()
{
    assert(opts.IsReversePInvoke());

    lvaReversePInvokeFrameVar = lvaGrabTempWithImplicitUse(false DEBUGARG("Reverse Pinvoke FrameVar"));

    LclVarDsc* varDsc   = &lvaTable[lvaReversePInvokeFrameVar];
    varDsc->lvType      = TYP_BLK;
    varDsc->lvExactSize = eeGetEEInfo()->sizeOfReversePInvokeFrame;

    // Add enter pinvoke exit callout at the start of prolog

    GenTree* pInvokeFrameVar = gtNewOperNode(GT_ADDR, TYP_I_IMPL, gtNewLclvNode(lvaReversePInvokeFrameVar, TYP_BLK));

    GenTree* tree;

    CorInfoHelpFunc reversePInvokeEnterHelper;

    GenTreeCall::Use* args;

    if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TRACK_TRANSITIONS))
    {
        reversePInvokeEnterHelper = CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER_TRACK_TRANSITIONS;

        GenTree* stubArgument;
        if (info.compPublishStubParam)
        {
            // If we have a secret param for a Reverse P/Invoke, that means that we are in an IL stub.
            // In this case, the method handle we pass down to the Reverse P/Invoke helper should be
            // the target method, which is passed in the secret parameter.
            stubArgument = gtNewLclvNode(lvaStubArgumentVar, TYP_I_IMPL);
        }
        else
        {
            stubArgument = gtNewIconNode(0, TYP_I_IMPL);
        }

        args = gtNewCallArgs(pInvokeFrameVar, gtNewIconEmbMethHndNode(info.compMethodHnd), stubArgument);
    }
    else
    {
        reversePInvokeEnterHelper = CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER;
        args                      = gtNewCallArgs(pInvokeFrameVar);
    }

    tree = gtNewHelperCallNode(reversePInvokeEnterHelper, TYP_VOID, args);

    fgEnsureFirstBBisScratch();

    fgNewStmtAtBeg(fgFirstBB, tree);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nReverse PInvoke method - Add reverse pinvoke enter in first basic block %s\n",
               fgFirstBB->dspToString());
        gtDispTree(tree);
        printf("\n");
    }
#endif

    // Add reverse pinvoke exit callout at the end of epilog

    tree = gtNewOperNode(GT_ADDR, TYP_I_IMPL, gtNewLclvNode(lvaReversePInvokeFrameVar, TYP_BLK));

    CorInfoHelpFunc reversePInvokeExitHelper = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TRACK_TRANSITIONS)
                                                   ? CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT_TRACK_TRANSITIONS
                                                   : CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT;

    tree = gtNewHelperCallNode(reversePInvokeExitHelper, TYP_VOID, gtNewCallArgs(tree));

    assert(genReturnBB != nullptr);

    fgNewStmtNearEnd(genReturnBB, tree);

#ifdef DEBUG
    if (verbose)
    {
        printf("\nReverse PInvoke method - Add reverse pinvoke exit in return basic block %s\n",
               genReturnBB->dspToString());
        gtDispTree(tree);
        printf("\n");
    }
#endif
}

/*****************************************************************************
 *
 *  Return 'true' if there is more than one BBJ_RETURN block.
 */

bool Compiler::fgMoreThanOneReturnBlock()
{
    unsigned retCnt = 0;

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        if (block->bbJumpKind == BBJ_RETURN)
        {
            retCnt++;
            if (retCnt > 1)
            {
                return true;
            }
        }
    }

    return false;
}

namespace
{
// Define a helper class for merging return blocks (which we do when the input has
// more than the limit for this configuration).
//
// Notes: sets fgReturnCount, genReturnBB, and genReturnLocal.
class MergedReturns
{
public:
#ifdef JIT32_GCENCODER

    // X86 GC encoding has a hard limit of SET_EPILOGCNT_MAX epilogs.
    const static unsigned ReturnCountHardLimit = SET_EPILOGCNT_MAX;
#else  // JIT32_GCENCODER

    // We currently apply a hard limit of '4' to all other targets (see
    // the other uses of SET_EPILOGCNT_MAX), though it would be good
    // to revisit that decision based on CQ analysis.
    const static unsigned ReturnCountHardLimit = 4;
#endif // JIT32_GCENCODER

private:
    Compiler* comp;

    // As we discover returns, we'll record them in `returnBlocks`, until
    // the limit is reached, at which point we'll keep track of the merged
    // return blocks in `returnBlocks`.
    BasicBlock* returnBlocks[ReturnCountHardLimit];

    // Each constant value returned gets its own merged return block that
    // returns that constant (up to the limit on number of returns); in
    // `returnConstants` we track the constant values returned by these
    // merged constant return blocks.
    INT64 returnConstants[ReturnCountHardLimit];

    // Indicators of where in the lexical block list we'd like to place
    // each constant return block.
    BasicBlock* insertionPoints[ReturnCountHardLimit];

    // Number of return blocks allowed
    PhasedVar<unsigned> maxReturns;

    // Flag to keep track of when we've hit the limit of returns and are
    // actively merging returns together.
    bool mergingReturns = false;

public:
    MergedReturns(Compiler* comp) : comp(comp)
    {
        comp->fgReturnCount = 0;
    }

    void SetMaxReturns(unsigned value)
    {
        maxReturns = value;
        maxReturns.MarkAsReadOnly();
    }

    //------------------------------------------------------------------------
    // Record: Make note of a return block in the input program.
    //
    // Arguments:
    //    returnBlock - Block in the input that has jump kind BBJ_RETURN
    //
    // Notes:
    //    Updates fgReturnCount appropriately, and generates a merged return
    //    block if necessary.  If a constant merged return block is used,
    //    `returnBlock` is rewritten to jump to it.  If a non-constant return
    //    block is used, `genReturnBB` is set to that block, and `genReturnLocal`
    //    is set to the lclvar that it returns; morph will need to rewrite
    //    `returnBlock` to set the local and jump to the return block in such
    //    cases, which it will do after some key transformations like rewriting
    //    tail calls and calls that return to hidden buffers.  In either of these
    //    cases, `fgReturnCount` and the merged return block's profile information
    //    will be updated to reflect or anticipate the rewrite of `returnBlock`.
    //
    void Record(BasicBlock* returnBlock)
    {
        // Add this return to our tally
        unsigned oldReturnCount = comp->fgReturnCount++;

        if (!mergingReturns)
        {
            if (oldReturnCount < maxReturns)
            {
                // No need to merge just yet; simply record this return.
                returnBlocks[oldReturnCount] = returnBlock;
                return;
            }

            // We'e reached our threshold
            mergingReturns = true;

            // Merge any returns we've already identified
            for (unsigned i = 0, searchLimit = 0; i < oldReturnCount; ++i)
            {
                BasicBlock* mergedReturnBlock = Merge(returnBlocks[i], searchLimit);
                if (returnBlocks[searchLimit] == mergedReturnBlock)
                {
                    // We've added a new block to the searchable set
                    ++searchLimit;
                }
            }
        }

        // We have too many returns, so merge this one in.
        // Search limit is new return count minus one (to exclude this block).
        unsigned searchLimit = comp->fgReturnCount - 1;
        Merge(returnBlock, searchLimit);
    }

    //------------------------------------------------------------------------
    // EagerCreate: Force creation of a non-constant merged return block `genReturnBB`.
    //
    // Return Value:
    //    The newly-created block which returns `genReturnLocal`.
    //
    BasicBlock* EagerCreate()
    {
        mergingReturns = true;
        return Merge(nullptr, 0);
    }

    //------------------------------------------------------------------------
    // PlaceReturns: Move any generated const return blocks to an appropriate
    //     spot in the lexical block list.
    //
    // Notes:
    //    The goal is to set things up favorably for a reasonable layout without
    //    putting too much burden on fgReorderBlocks; in particular, since that
    //    method doesn't (currently) shuffle non-profile, non-rare code to create
    //    fall-through and reduce gotos, this method places each const return
    //    block immediately after its last predecessor, so that the flow from
    //    there to it can become fallthrough without requiring any motion to be
    //    performed by fgReorderBlocks.
    //
    void PlaceReturns()
    {
        if (!mergingReturns)
        {
            // No returns generated => no returns to place.
            return;
        }

        for (unsigned index = 0; index < comp->fgReturnCount; ++index)
        {
            BasicBlock* returnBlock    = returnBlocks[index];
            BasicBlock* genReturnBlock = comp->genReturnBB;
            if (returnBlock == genReturnBlock)
            {
                continue;
            }

            BasicBlock* insertionPoint = insertionPoints[index];
            assert(insertionPoint != nullptr);

            comp->fgUnlinkBlock(returnBlock);
            comp->fgMoveBlocksAfter(returnBlock, returnBlock, insertionPoint);
            // Treat the merged return block as belonging to the same EH region
            // as the insertion point block, to make sure we don't break up
            // EH regions; since returning a constant won't throw, this won't
            // affect program behavior.
            comp->fgExtendEHRegionAfter(insertionPoint);
        }
    }

private:
    //------------------------------------------------------------------------
    // CreateReturnBB: Create a basic block to serve as a merged return point, stored to
    //    `returnBlocks` at the given index, and optionally returning the given constant.
    //
    // Arguments:
    //    index - Index into `returnBlocks` to store the new block into.
    //    returnConst - Constant that the new block should return; may be nullptr to
    //      indicate that the new merged return is for the non-constant case, in which
    //      case, if the method's return type is non-void, `comp->genReturnLocal` will
    //      be initialized to a new local of the appropriate type, and the new block will
    //      return it.
    //
    // Return Value:
    //    The new merged return block.
    //
    BasicBlock* CreateReturnBB(unsigned index, GenTreeIntConCommon* returnConst = nullptr)
    {
        BasicBlock* newReturnBB = comp->fgNewBBinRegion(BBJ_RETURN);
        newReturnBB->bbRefs     = 1; // bbRefs gets update later, for now it should be 1
        comp->fgReturnCount++;

        noway_assert(newReturnBB->bbNext == nullptr);

        JITDUMP("\n newReturnBB [" FMT_BB "] created\n", newReturnBB->bbNum);

        GenTree* returnExpr;

        if (returnConst != nullptr)
        {
            returnExpr             = comp->gtNewOperNode(GT_RETURN, returnConst->gtType, returnConst);
            returnConstants[index] = returnConst->IntegralValue();
        }
        else if (comp->compMethodHasRetVal())
        {
            // There is a return value, so create a temp for it.  Real returns will store the value in there and
            // it'll be reloaded by the single return.
            unsigned returnLocalNum   = comp->lvaGrabTemp(true DEBUGARG("Single return block return value"));
            comp->genReturnLocal      = returnLocalNum;
            LclVarDsc& returnLocalDsc = comp->lvaTable[returnLocalNum];

            if (comp->compMethodReturnsNativeScalarType())
            {
                returnLocalDsc.lvType = genActualType(comp->info.compRetType);
                if (varTypeIsStruct(returnLocalDsc.lvType))
                {
                    comp->lvaSetStruct(returnLocalNum, comp->info.compMethodInfo->args.retTypeClass, false);
                }
            }
            else if (comp->compMethodReturnsRetBufAddr())
            {
                returnLocalDsc.lvType = TYP_BYREF;
            }
            else if (comp->compMethodReturnsMultiRegRetType())
            {
                returnLocalDsc.lvType = TYP_STRUCT;
                comp->lvaSetStruct(returnLocalNum, comp->info.compMethodInfo->args.retTypeClass, true);
                returnLocalDsc.lvIsMultiRegRet = true;
            }
            else
            {
                assert(!"unreached");
            }

            if (varTypeIsFloating(returnLocalDsc.lvType))
            {
                comp->compFloatingPointUsed = true;
            }

#ifdef DEBUG
            // This temporary should not be converted to a double in stress mode,
            // because we introduce assigns to it after the stress conversion
            returnLocalDsc.lvKeepType = 1;
#endif

            GenTree* retTemp = comp->gtNewLclvNode(returnLocalNum, returnLocalDsc.TypeGet());

            // make sure copy prop ignores this node (make sure it always does a reload from the temp).
            retTemp->gtFlags |= GTF_DONT_CSE;
            returnExpr = comp->gtNewOperNode(GT_RETURN, retTemp->gtType, retTemp);
        }
        else
        {
            // return void
            noway_assert(comp->info.compRetType == TYP_VOID || varTypeIsStruct(comp->info.compRetType));
            comp->genReturnLocal = BAD_VAR_NUM;

            returnExpr = new (comp, GT_RETURN) GenTreeOp(GT_RETURN, TYP_VOID);
        }

        // Add 'return' expression to the return block
        comp->fgNewStmtAtEnd(newReturnBB, returnExpr);
        // Flag that this 'return' was generated by return merging so that subsequent
        // return block morhping will know to leave it alone.
        returnExpr->gtFlags |= GTF_RET_MERGED;

#ifdef DEBUG
        if (comp->verbose)
        {
            printf("\nmergeReturns statement tree ");
            Compiler::printTreeID(returnExpr);
            printf(" added to genReturnBB %s\n", newReturnBB->dspToString());
            comp->gtDispTree(returnExpr);
            printf("\n");
        }
#endif
        assert(index < maxReturns);
        returnBlocks[index] = newReturnBB;
        return newReturnBB;
    }

    //------------------------------------------------------------------------
    // Merge: Find or create an appropriate merged return block for the given input block.
    //
    // Arguments:
    //    returnBlock - Return block from the input program to find a merged return for.
    //                  May be nullptr to indicate that new block suitable for non-constant
    //                  returns should be generated but no existing block modified.
    //    searchLimit - Blocks in `returnBlocks` up to but not including index `searchLimit`
    //                  will be checked to see if we already have an appropriate merged return
    //                  block for this case.  If a new block must be created, it will be stored
    //                  to `returnBlocks` at index `searchLimit`.
    //
    // Return Value:
    //    Merged return block suitable for handling this return value.  May be newly-created
    //    or pre-existing.
    //
    // Notes:
    //    If a constant-valued merged return block is used, `returnBlock` will be rewritten to
    //    jump to the merged return block and its `GT_RETURN` statement will be removed.  If
    //    a non-constant-valued merged return block is used, `genReturnBB` and `genReturnLocal`
    //    will be set so that Morph can perform that rewrite, which it will do after some key
    //    transformations like rewriting tail calls and calls that return to hidden buffers.
    //    In either of these cases, `fgReturnCount` and the merged return block's profile
    //    information will be updated to reflect or anticipate the rewrite of `returnBlock`.
    //
    BasicBlock* Merge(BasicBlock* returnBlock, unsigned searchLimit)
    {
        assert(mergingReturns);

        BasicBlock* mergedReturnBlock = nullptr;

        // Do not look for mergable constant returns in debug codegen as
        // we may lose track of sequence points.
        if ((returnBlock != nullptr) && (maxReturns > 1) && !comp->opts.compDbgCode)
        {
            // Check to see if this is a constant return so that we can search
            // for and/or create a constant return block for it.

            GenTreeIntConCommon* retConst = GetReturnConst(returnBlock);
            if (retConst != nullptr)
            {
                // We have a constant.  Now find or create a corresponding return block.

                unsigned    index;
                BasicBlock* constReturnBlock = FindConstReturnBlock(retConst, searchLimit, &index);

                if (constReturnBlock == nullptr)
                {
                    // We didn't find a const return block.  See if we have space left
                    // to make one.

                    // We have already allocated `searchLimit` slots.
                    unsigned slotsReserved = searchLimit;
                    if (comp->genReturnBB == nullptr)
                    {
                        // We haven't made a non-const return yet, so we have to reserve
                        // a slot for one.
                        ++slotsReserved;
                    }

                    if (slotsReserved < maxReturns)
                    {
                        // We have enough space to allocate a slot for this constant.
                        constReturnBlock = CreateReturnBB(searchLimit, retConst);
                    }
                }

                if (constReturnBlock != nullptr)
                {
                    // Found a constant merged return block.
                    mergedReturnBlock = constReturnBlock;

                    // Change BBJ_RETURN to BBJ_ALWAYS targeting const return block.
                    assert((comp->info.compFlags & CORINFO_FLG_SYNCH) == 0);
                    returnBlock->bbJumpKind = BBJ_ALWAYS;
                    returnBlock->bbJumpDest = constReturnBlock;

                    // Remove GT_RETURN since constReturnBlock returns the constant.
                    assert(returnBlock->lastStmt()->GetRootNode()->OperIs(GT_RETURN));
                    assert(returnBlock->lastStmt()->GetRootNode()->gtGetOp1()->IsIntegralConst());
                    comp->fgRemoveStmt(returnBlock, returnBlock->lastStmt());

                    // Using 'returnBlock' as the insertion point for 'mergedReturnBlock'
                    // will give it a chance to use fallthrough rather than BBJ_ALWAYS.
                    // Resetting this after each merge ensures that any branches to the
                    // merged return block are lexically forward.

                    insertionPoints[index] = returnBlock;

                    // Update profile information in the mergedReturnBlock to
                    // reflect the additional flow.
                    //
                    if (returnBlock->hasProfileWeight())
                    {
                        BasicBlock::weight_t const oldWeight =
                            mergedReturnBlock->hasProfileWeight() ? mergedReturnBlock->bbWeight : BB_ZERO_WEIGHT;
                        BasicBlock::weight_t const newWeight = oldWeight + returnBlock->bbWeight;

                        JITDUMP("merging profile weight " FMT_WT " from " FMT_BB " to const return " FMT_BB "\n",
                                returnBlock->bbWeight, returnBlock->bbNum, mergedReturnBlock->bbNum);

                        mergedReturnBlock->setBBProfileWeight(newWeight);
                        DISPBLOCK(mergedReturnBlock);
                    }
                }
            }
        }

        if (mergedReturnBlock == nullptr)
        {
            // No constant return block for this return; use the general one.
            // We defer flow update and profile update to morph.
            //
            mergedReturnBlock = comp->genReturnBB;
            if (mergedReturnBlock == nullptr)
            {
                // No general merged return for this function yet; create one.
                // There had better still be room left in the array.
                assert(searchLimit < maxReturns);
                mergedReturnBlock = CreateReturnBB(searchLimit);
                comp->genReturnBB = mergedReturnBlock;
                // Downstream code expects the `genReturnBB` to always remain
                // once created, so that it can redirect flow edges to it.
                mergedReturnBlock->bbFlags |= BBF_DONT_REMOVE;
            }
        }

        if (returnBlock != nullptr)
        {
            // Update fgReturnCount to reflect or anticipate that `returnBlock` will no longer
            // be a return point.
            comp->fgReturnCount--;
        }

        return mergedReturnBlock;
    }

    //------------------------------------------------------------------------
    // GetReturnConst: If the given block returns an integral constant, return the
    //     GenTreeIntConCommon that represents the constant.
    //
    // Arguments:
    //    returnBlock - Block whose return value is to be inspected.
    //
    // Return Value:
    //    GenTreeIntCommon that is the argument of `returnBlock`'s `GT_RETURN` if
    //    such exists; nullptr otherwise.
    //
    static GenTreeIntConCommon* GetReturnConst(BasicBlock* returnBlock)
    {
        Statement* lastStmt = returnBlock->lastStmt();
        if (lastStmt == nullptr)
        {
            return nullptr;
        }

        GenTree* lastExpr = lastStmt->GetRootNode();
        if (!lastExpr->OperIs(GT_RETURN))
        {
            return nullptr;
        }

        GenTree* retExpr = lastExpr->gtGetOp1();
        if ((retExpr == nullptr) || !retExpr->IsIntegralConst())
        {
            return nullptr;
        }

        return retExpr->AsIntConCommon();
    }

    //------------------------------------------------------------------------
    // FindConstReturnBlock: Scan the already-created merged return blocks, up to `searchLimit`,
    //     and return the one corresponding to the given const expression if it exists.
    //
    // Arguments:
    //    constExpr - GenTreeIntCommon representing the constant return value we're
    //        searching for.
    //    searchLimit - Check `returnBlocks`/`returnConstants` up to but not including
    //        this index.
    //    index - [out] Index of return block in the `returnBlocks` array, if found;
    //        searchLimit otherwise.
    //
    // Return Value:
    //    A block that returns the same constant, if one is found; otherwise nullptr.
    //
    BasicBlock* FindConstReturnBlock(GenTreeIntConCommon* constExpr, unsigned searchLimit, unsigned* index)
    {
        INT64 constVal = constExpr->IntegralValue();

        for (unsigned i = 0; i < searchLimit; ++i)
        {
            // Need to check both for matching const val and for genReturnBB
            // because genReturnBB is used for non-constant returns and its
            // corresponding entry in the returnConstants array is garbage.
            // Check the returnBlocks[] first, so we don't access an uninitialized
            // returnConstants[] value (which some tools like valgrind will
            // complain about).

            BasicBlock* returnBlock = returnBlocks[i];

            if (returnBlock == comp->genReturnBB)
            {
                continue;
            }

            if (returnConstants[i] == constVal)
            {
                *index = i;
                return returnBlock;
            }
        }

        *index = searchLimit;
        return nullptr;
    }
};
}

/*****************************************************************************
*
*  Add any internal blocks/trees we may need
*/

void Compiler::fgAddInternal()
{
    noway_assert(!compIsForInlining());

    // The backend requires a scratch BB into which it can safely insert a P/Invoke method prolog if one is
    // required. Create it here.
    if (compMethodRequiresPInvokeFrame())
    {
        fgEnsureFirstBBisScratch();
        fgFirstBB->bbFlags |= BBF_DONT_REMOVE;
    }

    /*
    <BUGNUM> VSW441487 </BUGNUM>

    The "this" pointer is implicitly used in the following cases:
    1. Locking of synchronized methods
    2. Dictionary access of shared generics code
    3. If a method has "catch(FooException<T>)", the EH code accesses "this" to determine T.
    4. Initializing the type from generic methods which require precise cctor semantics
    5. Verifier does special handling of "this" in the .ctor

    However, we might overwrite it with a "starg 0".
    In this case, we will redirect all "ldarg(a)/starg(a) 0" to a temp lvaTable[lvaArg0Var]
    */

    if (!info.compIsStatic)
    {
        if (lvaArg0Var != info.compThisArg)
        {
            // When we're using the general encoder, we mark compThisArg address-taken to ensure that it is not
            // enregistered (since the decoder always reports a stack location for "this" for generics
            // context vars).
            bool lva0CopiedForGenericsCtxt;
#ifndef JIT32_GCENCODER
            lva0CopiedForGenericsCtxt = ((info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0);
#else  // JIT32_GCENCODER
            lva0CopiedForGenericsCtxt          = false;
#endif // JIT32_GCENCODER
            noway_assert(lva0CopiedForGenericsCtxt || !lvaTable[info.compThisArg].lvAddrExposed);
            noway_assert(!lvaTable[info.compThisArg].lvHasILStoreOp);
            noway_assert(lvaTable[lvaArg0Var].lvAddrExposed || lvaTable[lvaArg0Var].lvHasILStoreOp ||
                         lva0CopiedForGenericsCtxt);

            var_types thisType = lvaTable[info.compThisArg].TypeGet();

            // Now assign the original input "this" to the temp

            GenTree* tree;

            tree = gtNewLclvNode(lvaArg0Var, thisType);

            tree = gtNewAssignNode(tree,                                     // dst
                                   gtNewLclvNode(info.compThisArg, thisType) // src
                                   );

            /* Create a new basic block and stick the assignment in it */

            fgEnsureFirstBBisScratch();

            fgNewStmtAtEnd(fgFirstBB, tree);

#ifdef DEBUG
            if (verbose)
            {
                printf("\nCopy \"this\" to lvaArg0Var in first basic block %s\n", fgFirstBB->dspToString());
                gtDispTree(tree);
                printf("\n");
            }
#endif
        }
    }

    // Merge return points if required or beneficial
    MergedReturns merger(this);

#if defined(FEATURE_EH_FUNCLETS)
    // Add the synchronized method enter/exit calls and try/finally protection. Note
    // that this must happen before the one BBJ_RETURN block is created below, so the
    // BBJ_RETURN block gets placed at the top-level, not within an EH region. (Otherwise,
    // we'd have to be really careful when creating the synchronized method try/finally
    // not to include the BBJ_RETURN block.)
    if ((info.compFlags & CORINFO_FLG_SYNCH) != 0)
    {
        fgAddSyncMethodEnterExit();
    }
#endif // FEATURE_EH_FUNCLETS

    //
    //  We will generate just one epilog (return block)
    //   when we are asked to generate enter/leave callbacks
    //   or for methods with PInvoke
    //   or for methods calling into unmanaged code
    //   or for synchronized methods.
    //
    BasicBlock* lastBlockBeforeGenReturns = fgLastBB;
    if (compIsProfilerHookNeeded() || compMethodRequiresPInvokeFrame() || opts.IsReversePInvoke() ||
        ((info.compFlags & CORINFO_FLG_SYNCH) != 0))
    {
        // We will generate only one return block
        // We will transform the BBJ_RETURN blocks
        //  into jumps to the one return block
        //
        merger.SetMaxReturns(1);

        // Eagerly create the genReturnBB since the lowering of these constructs
        // will expect to find it.
        BasicBlock* mergedReturn = merger.EagerCreate();
        assert(mergedReturn == genReturnBB);
    }
    else
    {
        bool stressMerging = compStressCompile(STRESS_MERGED_RETURNS, 50);

        //
        // We are allowed to have multiple individual exits
        // However we can still decide to have a single return
        //
        if ((compCodeOpt() == SMALL_CODE) || stressMerging)
        {
            // Under stress or for Small_Code case we always
            // generate a single return block when we have multiple
            // return points
            //
            merger.SetMaxReturns(1);
        }
        else
        {
            merger.SetMaxReturns(MergedReturns::ReturnCountHardLimit);
        }
    }

    // Visit the BBJ_RETURN blocks and merge as necessary.

    for (BasicBlock* block = fgFirstBB; block != lastBlockBeforeGenReturns->bbNext; block = block->bbNext)
    {
        if ((block->bbJumpKind == BBJ_RETURN) && ((block->bbFlags & BBF_HAS_JMP) == 0))
        {
            merger.Record(block);
        }
    }

    merger.PlaceReturns();

    if (compMethodRequiresPInvokeFrame())
    {
        // The P/Invoke helpers only require a frame variable, so only allocate the
        // TCB variable if we're not using them.
        if (!opts.ShouldUsePInvokeHelpers())
        {
            info.compLvFrameListRoot           = lvaGrabTemp(false DEBUGARG("Pinvoke FrameListRoot"));
            LclVarDsc* rootVarDsc              = &lvaTable[info.compLvFrameListRoot];
            rootVarDsc->lvType                 = TYP_I_IMPL;
            rootVarDsc->lvImplicitlyReferenced = 1;
        }

        lvaInlinedPInvokeFrameVar = lvaGrabTempWithImplicitUse(false DEBUGARG("Pinvoke FrameVar"));

        LclVarDsc* varDsc = &lvaTable[lvaInlinedPInvokeFrameVar];
        varDsc->lvType    = TYP_BLK;
        // Make room for the inlined frame.
        varDsc->lvExactSize = eeGetEEInfo()->inlinedCallFrameInfo.size;
#if FEATURE_FIXED_OUT_ARGS
        // Grab and reserve space for TCB, Frame regs used in PInvoke epilog to pop the inlined frame.
        // See genPInvokeMethodEpilog() for use of the grabbed var. This is only necessary if we are
        // not using the P/Invoke helpers.
        if (!opts.ShouldUsePInvokeHelpers() && compJmpOpUsed)
        {
            lvaPInvokeFrameRegSaveVar = lvaGrabTempWithImplicitUse(false DEBUGARG("PInvokeFrameRegSave Var"));
            varDsc                    = &lvaTable[lvaPInvokeFrameRegSaveVar];
            varDsc->lvType            = TYP_BLK;
            varDsc->lvExactSize       = 2 * REGSIZE_BYTES;
        }
#endif
    }

    // Do we need to insert a "JustMyCode" callback?

    CORINFO_JUST_MY_CODE_HANDLE* pDbgHandle = nullptr;
    CORINFO_JUST_MY_CODE_HANDLE  dbgHandle  = nullptr;
    if (opts.compDbgCode && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB))
    {
        dbgHandle = info.compCompHnd->getJustMyCodeHandle(info.compMethodHnd, &pDbgHandle);
    }

    noway_assert(!dbgHandle || !pDbgHandle);

    if (dbgHandle || pDbgHandle)
    {
        // Test the JustMyCode VM global state variable
        GenTree* embNode        = gtNewIconEmbHndNode(dbgHandle, pDbgHandle, GTF_ICON_GLOBAL_PTR, info.compMethodHnd);
        GenTree* guardCheckVal  = gtNewOperNode(GT_IND, TYP_INT, embNode);
        GenTree* guardCheckCond = gtNewOperNode(GT_EQ, TYP_INT, guardCheckVal, gtNewZeroConNode(TYP_INT));

        // Create the callback which will yield the final answer

        GenTree* callback = gtNewHelperCallNode(CORINFO_HELP_DBG_IS_JUST_MY_CODE, TYP_VOID);
        callback          = new (this, GT_COLON) GenTreeColon(TYP_VOID, gtNewNothingNode(), callback);

        // Stick the conditional call at the start of the method

        fgEnsureFirstBBisScratch();
        fgNewStmtAtEnd(fgFirstBB, gtNewQmarkNode(TYP_VOID, guardCheckCond, callback));
    }

#if !defined(FEATURE_EH_FUNCLETS)

    /* Is this a 'synchronized' method? */

    if (info.compFlags & CORINFO_FLG_SYNCH)
    {
        GenTree* tree = NULL;

        /* Insert the expression "enterCrit(this)" or "enterCrit(handle)" */

        if (info.compIsStatic)
        {
            tree = fgGetCritSectOfStaticMethod();

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_ENTER_STATIC, TYP_VOID, gtNewCallArgs(tree));
        }
        else
        {
            noway_assert(lvaTable[info.compThisArg].lvType == TYP_REF);

            tree = gtNewLclvNode(info.compThisArg, TYP_REF);

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_ENTER, TYP_VOID, gtNewCallArgs(tree));
        }

        /* Create a new basic block and stick the call in it */

        fgEnsureFirstBBisScratch();

        fgNewStmtAtEnd(fgFirstBB, tree);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nSynchronized method - Add enterCrit statement in first basic block %s\n",
                   fgFirstBB->dspToString());
            gtDispTree(tree);
            printf("\n");
        }
#endif

        /* We must be generating a single exit point for this to work */

        noway_assert(genReturnBB != nullptr);

        /* Create the expression "exitCrit(this)" or "exitCrit(handle)" */

        if (info.compIsStatic)
        {
            tree = fgGetCritSectOfStaticMethod();

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_EXIT_STATIC, TYP_VOID, gtNewCallArgs(tree));
        }
        else
        {
            tree = gtNewLclvNode(info.compThisArg, TYP_REF);

            tree = gtNewHelperCallNode(CORINFO_HELP_MON_EXIT, TYP_VOID, gtNewCallArgs(tree));
        }

        fgNewStmtNearEnd(genReturnBB, tree);

#ifdef DEBUG
        if (verbose)
        {
            printf("\nSynchronized method - Add exit expression ");
            printTreeID(tree);
            printf("\n");
        }
#endif

        // Reset cookies used to track start and end of the protected region in synchronized methods
        syncStartEmitCookie = NULL;
        syncEndEmitCookie   = NULL;
    }

#endif // !FEATURE_EH_FUNCLETS

    if (opts.IsReversePInvoke())
    {
        fgAddReversePInvokeEnterExit();
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** After fgAddInternal()\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif
}

/*****************************************************************************/
/*****************************************************************************/

void Compiler::fgFindOperOrder()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgFindOperOrder()\n");
    }
#endif

    /* Walk the basic blocks and for each statement determine
     * the evaluation order, cost, FP levels, etc... */

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        compCurBB = block;
        for (Statement* stmt : block->Statements())
        {
            /* Recursively process the statement */

            compCurStmt = stmt;
            gtSetStmtInfo(stmt);
        }
    }
}

//------------------------------------------------------------------------
// fgSimpleLowering: do full walk of all IR, lowering selected operations
// and computing lvaOutgoingArgumentAreaSize.
//
// Notes:
//    Lowers GT_ARR_LENGTH, GT_ARR_BOUNDS_CHECK, and GT_SIMD_CHK.
//
//    For target ABIs with fixed out args area, computes upper bound on
//    the size of this area from the calls in the IR.
//
//    Outgoing arg area size is computed here because we want to run it
//    after optimization (in case calls are removed) and need to look at
//    all possible calls in the method.

void Compiler::fgSimpleLowering()
{
#if FEATURE_FIXED_OUT_ARGS
    unsigned outgoingArgSpaceSize = 0;
#endif // FEATURE_FIXED_OUT_ARGS

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {
        // Walk the statement trees in this basic block.
        compCurBB = block; // Used in fgRngChkTarget.

        LIR::Range& range = LIR::AsRange(block);
        for (GenTree* tree : range)
        {
            switch (tree->OperGet())
            {
                case GT_ARR_LENGTH:
                {
                    GenTreeArrLen* arrLen = tree->AsArrLen();
                    GenTree*       arr    = arrLen->AsArrLen()->ArrRef();
                    GenTree*       add;
                    GenTree*       con;

                    /* Create the expression "*(array_addr + ArrLenOffs)" */

                    noway_assert(arr->gtNext == tree);

                    noway_assert(arrLen->ArrLenOffset() == OFFSETOF__CORINFO_Array__length ||
                                 arrLen->ArrLenOffset() == OFFSETOF__CORINFO_String__stringLen);

                    if ((arr->gtOper == GT_CNS_INT) && (arr->AsIntCon()->gtIconVal == 0))
                    {
                        // If the array is NULL, then we should get a NULL reference
                        // exception when computing its length.  We need to maintain
                        // an invariant where there is no sum of two constants node, so
                        // let's simply return an indirection of NULL.

                        add = arr;
                    }
                    else
                    {
                        con = gtNewIconNode(arrLen->ArrLenOffset(), TYP_I_IMPL);
                        add = gtNewOperNode(GT_ADD, TYP_REF, arr, con);

                        range.InsertAfter(arr, con, add);
                    }

                    // Change to a GT_IND.
                    tree->ChangeOperUnchecked(GT_IND);

                    tree->AsOp()->gtOp1 = add;
                    break;
                }

                case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
                case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
                case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
                {
                    // Add in a call to an error routine.
                    fgSetRngChkTarget(tree, false);
                    break;
                }

#if FEATURE_FIXED_OUT_ARGS
                case GT_CALL:
                {
                    GenTreeCall* call = tree->AsCall();
                    // Fast tail calls use the caller-supplied scratch
                    // space so have no impact on this method's outgoing arg size.
                    if (!call->IsFastTailCall())
                    {
                        // Update outgoing arg size to handle this call
                        const unsigned thisCallOutAreaSize = call->fgArgInfo->GetOutArgSize();
                        assert(thisCallOutAreaSize >= MIN_ARG_AREA_FOR_CALL);

                        if (thisCallOutAreaSize > outgoingArgSpaceSize)
                        {
                            outgoingArgSpaceSize = thisCallOutAreaSize;
                            JITDUMP("Bumping outgoingArgSpaceSize to %u for call [%06d]\n", outgoingArgSpaceSize,
                                    dspTreeID(tree));
                        }
                        else
                        {
                            JITDUMP("outgoingArgSpaceSize %u sufficient for call [%06d], which needs %u\n",
                                    outgoingArgSpaceSize, dspTreeID(tree), thisCallOutAreaSize);
                        }
                    }
                    else
                    {
                        JITDUMP("outgoingArgSpaceSize not impacted by fast tail call [%06d]\n", dspTreeID(tree));
                    }
                    break;
                }
#endif // FEATURE_FIXED_OUT_ARGS

                default:
                {
                    // No other operators need processing.
                    break;
                }
            } // switch on oper
        }     // foreach tree
    }         // foreach BB

#if FEATURE_FIXED_OUT_ARGS
    // Finish computing the outgoing args area size
    //
    // Need to make sure the MIN_ARG_AREA_FOR_CALL space is added to the frame if:
    // 1. there are calls to THROW_HEPLPER methods.
    // 2. we are generating profiling Enter/Leave/TailCall hooks. This will ensure
    //    that even methods without any calls will have outgoing arg area space allocated.
    //
    // An example for these two cases is Windows Amd64, where the ABI requires to have 4 slots for
    // the outgoing arg space if the method makes any calls.
    if (outgoingArgSpaceSize < MIN_ARG_AREA_FOR_CALL)
    {
        if (compUsesThrowHelper || compIsProfilerHookNeeded())
        {
            outgoingArgSpaceSize = MIN_ARG_AREA_FOR_CALL;
            JITDUMP("Bumping outgoingArgSpaceSize to %u for throw helper or profile hook", outgoingArgSpaceSize);
        }
    }

    // If a function has localloc, we will need to move the outgoing arg space when the
    // localloc happens. When we do this, we need to maintain stack alignment. To avoid
    // leaving alignment-related holes when doing this move, make sure the outgoing
    // argument space size is a multiple of the stack alignment by aligning up to the next
    // stack alignment boundary.
    if (compLocallocUsed)
    {
        outgoingArgSpaceSize = roundUp(outgoingArgSpaceSize, STACK_ALIGN);
        JITDUMP("Bumping outgoingArgSpaceSize to %u for localloc", outgoingArgSpaceSize);
    }

    assert((outgoingArgSpaceSize % TARGET_POINTER_SIZE) == 0);

    // Publish the final value and mark it as read only so any update
    // attempt later will cause an assert.
    lvaOutgoingArgSpaceSize = outgoingArgSpaceSize;
    lvaOutgoingArgSpaceSize.MarkAsReadOnly();

#endif // FEATURE_FIXED_OUT_ARGS

#ifdef DEBUG
    if (verbose && fgRngChkThrowAdded)
    {
        printf("\nAfter fgSimpleLowering() added some RngChk throw blocks");
        fgDispBasicBlocks();
        fgDispHandlerTab();
        printf("\n");
    }
#endif
}

/*****************************************************************************************************
 *
 *  Function to return the last basic block in the main part of the function. With funclets, it is
 *  the block immediately before the first funclet.
 *  An inclusive end of the main method.
 */

BasicBlock* Compiler::fgLastBBInMainFunction()
{
#if defined(FEATURE_EH_FUNCLETS)

    if (fgFirstFuncletBB != nullptr)
    {
        return fgFirstFuncletBB->bbPrev;
    }

#endif // FEATURE_EH_FUNCLETS

    assert(fgLastBB->bbNext == nullptr);

    return fgLastBB;
}

/*****************************************************************************************************
 *
 *  Function to return the first basic block after the main part of the function. With funclets, it is
 *  the block of the first funclet.  Otherwise it is NULL if there are no funclets (fgLastBB->bbNext).
 *  This is equivalent to fgLastBBInMainFunction()->bbNext
 *  An exclusive end of the main method.
 */

BasicBlock* Compiler::fgEndBBAfterMainFunction()
{
#if defined(FEATURE_EH_FUNCLETS)

    if (fgFirstFuncletBB != nullptr)
    {
        return fgFirstFuncletBB;
    }

#endif // FEATURE_EH_FUNCLETS

    assert(fgLastBB->bbNext == nullptr);

    return nullptr;
}

#if defined(FEATURE_EH_FUNCLETS)

/*****************************************************************************
 * Introduce a new head block of the handler for the prolog to be put in, ahead
 * of the current handler head 'block'.
 * Note that this code has some similarities to fgCreateLoopPreHeader().
 */

void Compiler::fgInsertFuncletPrologBlock(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\nCreating funclet prolog header for " FMT_BB "\n", block->bbNum);
    }
#endif

    assert(block->hasHndIndex());
    assert(fgFirstBlockOfHandler(block) == block); // this block is the first block of a handler

    /* Allocate a new basic block */

    BasicBlock* newHead = bbNewBasicBlock(BBJ_NONE);
    newHead->bbFlags |= BBF_INTERNAL;
    newHead->inheritWeight(block);
    newHead->bbRefs = 0;

    fgInsertBBbefore(block, newHead); // insert the new block in the block list
    fgExtendEHRegionBefore(block);    // Update the EH table to make the prolog block the first block in the block's EH
                                      // block.

    // Distribute the pred list between newHead and block. Incoming edges coming from outside
    // the handler go to the prolog. Edges coming from with the handler are back-edges, and
    // go to the existing 'block'.

    for (flowList* pred = block->bbPreds; pred; pred = pred->flNext)
    {
        BasicBlock* predBlock = pred->getBlock();
        if (!fgIsIntraHandlerPred(predBlock, block))
        {
            // It's a jump from outside the handler; add it to the newHead preds list and remove
            // it from the block preds list.

            switch (predBlock->bbJumpKind)
            {
                case BBJ_CALLFINALLY:
                    noway_assert(predBlock->bbJumpDest == block);
                    predBlock->bbJumpDest = newHead;
                    fgRemoveRefPred(block, predBlock);
                    fgAddRefPred(newHead, predBlock);
                    break;

                default:
                    // The only way into the handler is via a BBJ_CALLFINALLY (to a finally handler), or
                    // via exception handling.
                    noway_assert(false);
                    break;
            }
        }
    }

    assert(nullptr == fgGetPredForBlock(block, newHead));
    fgAddRefPred(block, newHead);

    assert((newHead->bbFlags & BBF_INTERNAL) == BBF_INTERNAL);
}

/*****************************************************************************
 *
 * Every funclet will have a prolog. That prolog will be inserted as the first instructions
 * in the first block of the funclet. If the prolog is also the head block of a loop, we
 * would end up with the prolog instructions being executed more than once.
 * Check for this by searching the predecessor list for loops, and create a new prolog header
 * block when needed. We detect a loop by looking for any predecessor that isn't in the
 * handler's try region, since the only way to get into a handler is via that try region.
 */

void Compiler::fgCreateFuncletPrologBlocks()
{
    noway_assert(fgComputePredsDone);
    noway_assert(!fgDomsComputed); // this function doesn't maintain the dom sets
    assert(!fgFuncletsCreated);

    bool      prologBlocksCreated = false;
    EHblkDsc* HBtabEnd;
    EHblkDsc* HBtab;

    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount; HBtab < HBtabEnd; HBtab++)
    {
        BasicBlock* head = HBtab->ebdHndBeg;

        if (fgAnyIntraHandlerPreds(head))
        {
            // We need to create a new block in which to place the prolog, and split the existing
            // head block predecessor edges into those that should point to the prolog, and those
            // that shouldn't.
            //
            // It's arguable that we should just always do this, and not only when we "need to",
            // so there aren't two different code paths. However, it's unlikely to be necessary
            // for catch handlers because they have an incoming argument (the exception object)
            // that needs to get stored or saved, so back-arcs won't normally go to the head. It's
            // possible when writing in IL to generate a legal loop (e.g., push an Exception object
            // on the stack before jumping back to the catch head), but C# probably won't. This will
            // most commonly only be needed for finallys with a do/while loop at the top of the
            // finally.
            //
            // Note that we don't check filters. This might be a bug, but filters always have a filter
            // object live on entry, so it's at least unlikely (illegal?) that a loop edge targets the
            // filter head.

            fgInsertFuncletPrologBlock(head);
            prologBlocksCreated = true;
        }
    }

    if (prologBlocksCreated)
    {
        // If we've modified the graph, reset the 'modified' flag, since the dominators haven't
        // been computed.
        fgModified = false;

#if DEBUG
        if (verbose)
        {
            JITDUMP("\nAfter fgCreateFuncletPrologBlocks()");
            fgDispBasicBlocks();
            fgDispHandlerTab();
        }

        fgVerifyHandlerTab();
        fgDebugCheckBBlist();
#endif // DEBUG
    }
}

/*****************************************************************************
 *
 *  Function to create funclets out of all EH catch/finally/fault blocks.
 *  We only move filter and handler blocks, not try blocks.
 */

void Compiler::fgCreateFunclets()
{
    assert(!fgFuncletsCreated);

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgCreateFunclets()\n");
    }
#endif

    fgCreateFuncletPrologBlocks();

    unsigned           XTnum;
    EHblkDsc*          HBtab;
    const unsigned int funcCnt = ehFuncletCount() + 1;

    if (!FitsIn<unsigned short>(funcCnt))
    {
        IMPL_LIMITATION("Too many funclets");
    }

    FuncInfoDsc* funcInfo = new (this, CMK_BasicBlock) FuncInfoDsc[funcCnt];

    unsigned short funcIdx;

    // Setup the root FuncInfoDsc and prepare to start associating
    // FuncInfoDsc's with their corresponding EH region
    memset((void*)funcInfo, 0, funcCnt * sizeof(FuncInfoDsc));
    assert(funcInfo[0].funKind == FUNC_ROOT);
    funcIdx = 1;

    // Because we iterate from the top to the bottom of the compHndBBtab array, we are iterating
    // from most nested (innermost) to least nested (outermost) EH region. It would be reasonable
    // to iterate in the opposite order, but the order of funclets shouldn't matter.
    //
    // We move every handler region to the end of the function: each handler will become a funclet.
    //
    // Note that fgRelocateEHRange() can add new entries to the EH table. However, they will always
    // be added *after* the current index, so our iteration here is not invalidated.
    // It *can* invalidate the compHndBBtab pointer itself, though, if it gets reallocated!

    for (XTnum = 0; XTnum < compHndBBtabCount; XTnum++)
    {
        HBtab = ehGetDsc(XTnum); // must re-compute this every loop, since fgRelocateEHRange changes the table
        if (HBtab->HasFilter())
        {
            assert(funcIdx < funcCnt);
            funcInfo[funcIdx].funKind    = FUNC_FILTER;
            funcInfo[funcIdx].funEHIndex = (unsigned short)XTnum;
            funcIdx++;
        }
        assert(funcIdx < funcCnt);
        funcInfo[funcIdx].funKind    = FUNC_HANDLER;
        funcInfo[funcIdx].funEHIndex = (unsigned short)XTnum;
        HBtab->ebdFuncIndex          = funcIdx;
        funcIdx++;
        fgRelocateEHRange(XTnum, FG_RELOCATE_HANDLER);
    }

    // We better have populated all of them by now
    assert(funcIdx == funcCnt);

    // Publish
    compCurrFuncIdx   = 0;
    compFuncInfos     = funcInfo;
    compFuncInfoCount = (unsigned short)funcCnt;

    fgFuncletsCreated = true;

#if DEBUG
    if (verbose)
    {
        JITDUMP("\nAfter fgCreateFunclets()");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }

    fgVerifyHandlerTab();
    fgDebugCheckBBlist();
#endif // DEBUG
}

#endif // defined(FEATURE_EH_FUNCLETS)

/*-------------------------------------------------------------------------
 *
 * Walk the basic blocks list to determine the first block to place in the
 * cold section.  This would be the first of a series of rarely executed blocks
 * such that no succeeding blocks are in a try region or an exception handler
 * or are rarely executed.
 */

void Compiler::fgDetermineFirstColdBlock()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** In fgDetermineFirstColdBlock()\n");
    }
#endif // DEBUG

    // Since we may need to create a new transistion block
    // we assert that it is OK to create new blocks.
    //
    assert(fgSafeBasicBlockCreation);

    fgFirstColdBlock = nullptr;

    if (!opts.compProcedureSplitting)
    {
        JITDUMP("No procedure splitting will be done for this method\n");
        return;
    }

#ifdef DEBUG
    if ((compHndBBtabCount > 0) && !opts.compProcedureSplittingEH)
    {
        JITDUMP("No procedure splitting will be done for this method with EH (by request)\n");
        return;
    }
#endif // DEBUG

#if defined(FEATURE_EH_FUNCLETS)
    // TODO-CQ: handle hot/cold splitting in functions with EH (including synchronized methods
    // that create EH in methods without explicit EH clauses).

    if (compHndBBtabCount > 0)
    {
        JITDUMP("No procedure splitting will be done for this method with EH (implementation limitation)\n");
        return;
    }
#endif // FEATURE_EH_FUNCLETS

    BasicBlock* firstColdBlock       = nullptr;
    BasicBlock* prevToFirstColdBlock = nullptr;
    BasicBlock* block;
    BasicBlock* lblk;

    for (lblk = nullptr, block = fgFirstBB; block != nullptr; lblk = block, block = block->bbNext)
    {
        bool blockMustBeInHotSection = false;

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
        if (bbIsHandlerBeg(block))
        {
            blockMustBeInHotSection = true;
        }
#endif // HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION

        // Do we have a candidate for the first cold block?
        if (firstColdBlock != nullptr)
        {
            // We have a candidate for first cold block

            // Is this a hot block?
            if (blockMustBeInHotSection || (block->isRunRarely() == false))
            {
                // We have to restart the search for the first cold block
                firstColdBlock       = nullptr;
                prevToFirstColdBlock = nullptr;
            }
        }
        else // (firstColdBlock == NULL)
        {
            // We don't have a candidate for first cold block

            // Is this a cold block?
            if (!blockMustBeInHotSection && (block->isRunRarely() == true))
            {
                //
                // If the last block that was hot was a BBJ_COND
                // then we will have to add an unconditional jump
                // so the code size for block needs be large
                // enough to make it worth our while
                //
                if ((lblk == nullptr) || (lblk->bbJumpKind != BBJ_COND) || (fgGetCodeEstimate(block) >= 8))
                {
                    // This block is now a candidate for first cold block
                    // Also remember the predecessor to this block
                    firstColdBlock       = block;
                    prevToFirstColdBlock = lblk;
                }
            }
        }
    }

    if (firstColdBlock == fgFirstBB)
    {
        // If the first block is Cold then we can't move any blocks
        // into the cold section

        firstColdBlock = nullptr;
    }

    if (firstColdBlock != nullptr)
    {
        noway_assert(prevToFirstColdBlock != nullptr);

        if (prevToFirstColdBlock == nullptr)
        {
            return; // To keep Prefast happy
        }

        // If we only have one cold block
        // then it may not be worth it to move it
        // into the Cold section as a jump to the
        // Cold section is 5 bytes in size.
        //
        if (firstColdBlock->bbNext == nullptr)
        {
            // If the size of the cold block is 7 or less
            // then we will keep it in the Hot section.
            //
            if (fgGetCodeEstimate(firstColdBlock) < 8)
            {
                firstColdBlock = nullptr;
                goto EXIT;
            }
        }

        // When the last Hot block fall through into the Cold section
        // we may need to add a jump
        //
        if (prevToFirstColdBlock->bbFallsThrough())
        {
            switch (prevToFirstColdBlock->bbJumpKind)
            {
                default:
                    noway_assert(!"Unhandled jumpkind in fgDetermineFirstColdBlock()");
                    break;

                case BBJ_CALLFINALLY:
                    // A BBJ_CALLFINALLY that falls through is always followed
                    // by an empty BBJ_ALWAYS.
                    //
                    assert(prevToFirstColdBlock->isBBCallAlwaysPair());
                    firstColdBlock =
                        firstColdBlock->bbNext; // Note that this assignment could make firstColdBlock == nullptr
                    break;

                case BBJ_COND:
                    //
                    // This is a slightly more complicated case, because we will
                    // probably need to insert a block to jump to the cold section.
                    //
                    if (firstColdBlock->isEmpty() && (firstColdBlock->bbJumpKind == BBJ_ALWAYS))
                    {
                        // We can just use this block as the transitionBlock
                        firstColdBlock = firstColdBlock->bbNext;
                        // Note that this assignment could make firstColdBlock == NULL
                    }
                    else
                    {
                        BasicBlock* transitionBlock = fgNewBBafter(BBJ_ALWAYS, prevToFirstColdBlock, true);
                        transitionBlock->bbJumpDest = firstColdBlock;
                        transitionBlock->inheritWeight(firstColdBlock);

                        noway_assert(fgComputePredsDone);

                        // Update the predecessor list for firstColdBlock
                        fgReplacePred(firstColdBlock, prevToFirstColdBlock, transitionBlock);

                        // Add prevToFirstColdBlock as a predecessor for transitionBlock
                        fgAddRefPred(transitionBlock, prevToFirstColdBlock);
                    }
                    break;

                case BBJ_NONE:
                    // If the block preceding the first cold block is BBJ_NONE,
                    // convert it to BBJ_ALWAYS to force an explicit jump.

                    prevToFirstColdBlock->bbJumpDest = firstColdBlock;
                    prevToFirstColdBlock->bbJumpKind = BBJ_ALWAYS;
                    break;
            }
        }
    }

    for (block = firstColdBlock; block != nullptr; block = block->bbNext)
    {
        block->bbFlags |= BBF_COLD;
    }

EXIT:;

#ifdef DEBUG
    if (verbose)
    {
        if (firstColdBlock)
        {
            printf("fgFirstColdBlock is " FMT_BB ".\n", firstColdBlock->bbNum);
        }
        else
        {
            printf("fgFirstColdBlock is NULL.\n");
        }

        fgDispBasicBlocks();
    }

    fgVerifyHandlerTab();
#endif // DEBUG

    fgFirstColdBlock = firstColdBlock;
}

/* static */
unsigned Compiler::acdHelper(SpecialCodeKind codeKind)
{
    switch (codeKind)
    {
        case SCK_RNGCHK_FAIL:
            return CORINFO_HELP_RNGCHKFAIL;
        case SCK_ARG_EXCPN:
            return CORINFO_HELP_THROW_ARGUMENTEXCEPTION;
        case SCK_ARG_RNG_EXCPN:
            return CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION;
        case SCK_DIV_BY_ZERO:
            return CORINFO_HELP_THROWDIVZERO;
        case SCK_ARITH_EXCPN:
            return CORINFO_HELP_OVERFLOW;
        default:
            assert(!"Bad codeKind");
            return 0;
    }
}

//------------------------------------------------------------------------
// fgAddCodeRef: Find/create an added code entry associated with the given block and with the given kind.
//
// Arguments:
//   srcBlk  - the block that needs an entry;
//   refData - the index to use as the cache key for sharing throw blocks;
//   kind    - the kind of exception;
//
// Return Value:
//   The target throw helper block or nullptr if throw helper blocks are disabled.
//
BasicBlock* Compiler::fgAddCodeRef(BasicBlock* srcBlk, unsigned refData, SpecialCodeKind kind)
{
    // Record that the code will call a THROW_HELPER
    // so on Windows Amd64 we can allocate the 4 outgoing
    // arg slots on the stack frame if there are no other calls.
    compUsesThrowHelper = true;

    if (!fgUseThrowHelperBlocks())
    {
        return nullptr;
    }

    const static BBjumpKinds jumpKinds[] = {
        BBJ_NONE,   // SCK_NONE
        BBJ_THROW,  // SCK_RNGCHK_FAIL
        BBJ_ALWAYS, // SCK_PAUSE_EXEC
        BBJ_THROW,  // SCK_DIV_BY_ZERO
        BBJ_THROW,  // SCK_ARITH_EXCP, SCK_OVERFLOW
        BBJ_THROW,  // SCK_ARG_EXCPN
        BBJ_THROW,  // SCK_ARG_RNG_EXCPN
    };

    noway_assert(sizeof(jumpKinds) == SCK_COUNT); // sanity check

    /* First look for an existing entry that matches what we're looking for */

    AddCodeDsc* add = fgFindExcptnTarget(kind, refData);

    if (add) // found it
    {
        return add->acdDstBlk;
    }

    /* We have to allocate a new entry and prepend it to the list */

    add          = new (this, CMK_Unknown) AddCodeDsc;
    add->acdData = refData;
    add->acdKind = kind;
    add->acdNext = fgAddCodeList;
#if !FEATURE_FIXED_OUT_ARGS
    add->acdStkLvl     = 0;
    add->acdStkLvlInit = false;
#endif // !FEATURE_FIXED_OUT_ARGS

    fgAddCodeList = add;

    /* Create the target basic block */

    BasicBlock* newBlk;

    newBlk = add->acdDstBlk = fgNewBBinRegion(jumpKinds[kind], srcBlk, /* runRarely */ true, /* insertAtEnd */ true);

#ifdef DEBUG
    if (verbose)
    {
        const char* msgWhere = "";
        if (!srcBlk->hasTryIndex() && !srcBlk->hasHndIndex())
        {
            msgWhere = "non-EH region";
        }
        else if (!srcBlk->hasTryIndex())
        {
            msgWhere = "handler";
        }
        else if (!srcBlk->hasHndIndex())
        {
            msgWhere = "try";
        }
        else if (srcBlk->getTryIndex() < srcBlk->getHndIndex())
        {
            msgWhere = "try";
        }
        else
        {
            msgWhere = "handler";
        }

        const char* msg;
        switch (kind)
        {
            case SCK_RNGCHK_FAIL:
                msg = " for RNGCHK_FAIL";
                break;
            case SCK_PAUSE_EXEC:
                msg = " for PAUSE_EXEC";
                break;
            case SCK_DIV_BY_ZERO:
                msg = " for DIV_BY_ZERO";
                break;
            case SCK_OVERFLOW:
                msg = " for OVERFLOW";
                break;
            case SCK_ARG_EXCPN:
                msg = " for ARG_EXCPN";
                break;
            case SCK_ARG_RNG_EXCPN:
                msg = " for ARG_RNG_EXCPN";
                break;
            default:
                msg = " for ??";
                break;
        }

        printf("\nfgAddCodeRef - Add BB in %s%s, new block %s\n", msgWhere, msg, add->acdDstBlk->dspToString());
    }
#endif // DEBUG

    /* Mark the block as added by the compiler and not removable by future flow
       graph optimizations. Note that no bbJumpDest points to these blocks. */

    newBlk->bbFlags |= BBF_IMPORTED;
    newBlk->bbFlags |= BBF_DONT_REMOVE;

    /* Remember that we're adding a new basic block */

    fgAddCodeModf      = true;
    fgRngChkThrowAdded = true;

    /* Now figure out what code to insert */

    GenTreeCall* tree;
    int          helper = CORINFO_HELP_UNDEF;

    switch (kind)
    {
        case SCK_RNGCHK_FAIL:
            helper = CORINFO_HELP_RNGCHKFAIL;
            break;

        case SCK_DIV_BY_ZERO:
            helper = CORINFO_HELP_THROWDIVZERO;
            break;

        case SCK_ARITH_EXCPN:
            helper = CORINFO_HELP_OVERFLOW;
            noway_assert(SCK_OVERFLOW == SCK_ARITH_EXCPN);
            break;

        case SCK_ARG_EXCPN:
            helper = CORINFO_HELP_THROW_ARGUMENTEXCEPTION;
            break;

        case SCK_ARG_RNG_EXCPN:
            helper = CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION;
            break;

        // case SCK_PAUSE_EXEC:
        //     noway_assert(!"add code to pause exec");

        default:
            noway_assert(!"unexpected code addition kind");
            return nullptr;
    }

    noway_assert(helper != CORINFO_HELP_UNDEF);

    // Add the appropriate helper call.
    tree = gtNewHelperCallNode(helper, TYP_VOID);

    // There are no args here but fgMorphArgs has side effects
    // such as setting the outgoing arg area (which is necessary
    // on AMD if there are any calls).
    tree = fgMorphArgs(tree);

    // Store the tree in the new basic block.
    assert(!srcBlk->isEmpty());
    if (!srcBlk->IsLIR())
    {
        fgInsertStmtAtEnd(newBlk, fgNewStmtFromTree(tree));
    }
    else
    {
        LIR::AsRange(newBlk).InsertAtEnd(LIR::SeqTree(this, tree));
    }

    return add->acdDstBlk;
}

/*****************************************************************************
 * Finds the block to jump to, to throw a given kind of exception
 * We maintain a cache of one AddCodeDsc for each kind, to make searching fast.
 * Note : Each block uses the same (maybe shared) block as the jump target for
 * a given type of exception
 */

Compiler::AddCodeDsc* Compiler::fgFindExcptnTarget(SpecialCodeKind kind, unsigned refData)
{
    assert(fgUseThrowHelperBlocks());
    if (!(fgExcptnTargetCache[kind] && // Try the cached value first
          fgExcptnTargetCache[kind]->acdData == refData))
    {
        // Too bad, have to search for the jump target for the exception

        AddCodeDsc* add = nullptr;

        for (add = fgAddCodeList; add != nullptr; add = add->acdNext)
        {
            if (add->acdData == refData && add->acdKind == kind)
            {
                break;
            }
        }

        fgExcptnTargetCache[kind] = add; // Cache it
    }

    return fgExcptnTargetCache[kind];
}

/*****************************************************************************
 *
 *  The given basic block contains an array range check; return the label this
 *  range check is to jump to upon failure.
 */

//------------------------------------------------------------------------
// fgRngChkTarget: Create/find the appropriate "range-fail" label for the block.
//
// Arguments:
//   srcBlk  - the block that needs an entry;
//   kind    - the kind of exception;
//
// Return Value:
//   The target throw helper block this check jumps to upon failure.
//
BasicBlock* Compiler::fgRngChkTarget(BasicBlock* block, SpecialCodeKind kind)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*** Computing fgRngChkTarget for block " FMT_BB "\n", block->bbNum);
        if (!block->IsLIR())
        {
            gtDispStmt(compCurStmt);
        }
    }
#endif // DEBUG

    /* We attach the target label to the containing try block (if any) */
    noway_assert(!compIsForInlining());
    return fgAddCodeRef(block, bbThrowIndex(block), kind);
}

// Sequences the tree.
// prevTree is what gtPrev of the first node in execution order gets set to.
// Returns the first node (execution order) in the sequenced tree.
GenTree* Compiler::fgSetTreeSeq(GenTree* tree, GenTree* prevTree, bool isLIR)
{
    GenTree list;

    if (prevTree == nullptr)
    {
        prevTree = &list;
    }
    fgTreeSeqLst = prevTree;
    fgTreeSeqNum = 0;
    fgTreeSeqBeg = nullptr;
    fgSetTreeSeqHelper(tree, isLIR);

    GenTree* result = prevTree->gtNext;
    if (prevTree == &list)
    {
        list.gtNext->gtPrev = nullptr;
    }

    return result;
}

/*****************************************************************************
 *
 *  Assigns sequence numbers to the given tree and its sub-operands, and
 *  threads all the nodes together via the 'gtNext' and 'gtPrev' fields.
 *  Uses 'global' - fgTreeSeqLst
 */

void Compiler::fgSetTreeSeqHelper(GenTree* tree, bool isLIR)
{
    genTreeOps oper;
    unsigned   kind;

    noway_assert(tree);
    assert(!IsUninitialized(tree));

    /* Figure out what kind of a node we have */

    oper = tree->OperGet();
    kind = tree->OperKind();

    /* Is this a leaf/constant node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        fgSetTreeSeqFinish(tree, isLIR);
        return;
    }

    // Special handling for dynamic block ops.
    if (tree->OperIs(GT_DYN_BLK, GT_STORE_DYN_BLK))
    {
        GenTreeDynBlk* dynBlk    = tree->AsDynBlk();
        GenTree*       sizeNode  = dynBlk->gtDynamicSize;
        GenTree*       dstAddr   = dynBlk->Addr();
        GenTree*       src       = dynBlk->Data();
        bool           isReverse = ((dynBlk->gtFlags & GTF_REVERSE_OPS) != 0);
        if (dynBlk->gtEvalSizeFirst)
        {
            fgSetTreeSeqHelper(sizeNode, isLIR);
        }

        // We either have a DYN_BLK or a STORE_DYN_BLK. If the latter, we have a
        // src (the Data to be stored), and isReverse tells us whether to evaluate
        // that before dstAddr.
        if (isReverse && (src != nullptr))
        {
            fgSetTreeSeqHelper(src, isLIR);
        }
        fgSetTreeSeqHelper(dstAddr, isLIR);
        if (!isReverse && (src != nullptr))
        {
            fgSetTreeSeqHelper(src, isLIR);
        }
        if (!dynBlk->gtEvalSizeFirst)
        {
            fgSetTreeSeqHelper(sizeNode, isLIR);
        }
        fgSetTreeSeqFinish(dynBlk, isLIR);
        return;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->gtGetOp2IfPresent();

        // Special handling for GT_LIST
        if (tree->OperGet() == GT_LIST)
        {
            // First, handle the list items, which will be linked in forward order.
            // As we go, we will link the GT_LIST nodes in reverse order - we will number
            // them and update fgTreeSeqList in a subsequent traversal.
            GenTree* nextList = tree;
            GenTree* list     = nullptr;
            while (nextList != nullptr && nextList->OperGet() == GT_LIST)
            {
                list              = nextList;
                GenTree* listItem = list->AsOp()->gtOp1;
                fgSetTreeSeqHelper(listItem, isLIR);
                nextList = list->AsOp()->gtOp2;
                if (nextList != nullptr)
                {
                    nextList->gtNext = list;
                }
                list->gtPrev = nextList;
            }
            // Next, handle the GT_LIST nodes.
            // Note that fgSetTreeSeqFinish() sets the gtNext to null, so we need to capture the nextList
            // before we call that method.
            nextList = list;
            do
            {
                assert(list != nullptr);
                list     = nextList;
                nextList = list->gtNext;
                fgSetTreeSeqFinish(list, isLIR);
            } while (list != tree);
            return;
        }

        /* Special handling for AddrMode */
        if (tree->OperIsAddrMode())
        {
            bool reverse = ((tree->gtFlags & GTF_REVERSE_OPS) != 0);
            if (reverse)
            {
                assert(op1 != nullptr && op2 != nullptr);
                fgSetTreeSeqHelper(op2, isLIR);
            }
            if (op1 != nullptr)
            {
                fgSetTreeSeqHelper(op1, isLIR);
            }
            if (!reverse && op2 != nullptr)
            {
                fgSetTreeSeqHelper(op2, isLIR);
            }

            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* Check for a nilary operator */

        if (op1 == nullptr)
        {
            noway_assert(op2 == nullptr);
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* Is this a unary operator?
         * Although UNARY GT_IND has a special structure */

        if (oper == GT_IND)
        {
            /* Visit the indirection first - op2 may point to the
             * jump Label for array-index-out-of-range */

            fgSetTreeSeqHelper(op1, isLIR);
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* Now this is REALLY a unary operator */

        if (!op2)
        {
            /* Visit the (only) operand and we're done */

            fgSetTreeSeqHelper(op1, isLIR);
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /*
           For "real" ?: operators, we make sure the order is
           as follows:

               condition
               1st operand
               GT_COLON
               2nd operand
               GT_QMARK
        */

        if (oper == GT_QMARK)
        {
            noway_assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);

            fgSetTreeSeqHelper(op1, isLIR);
            // Here, for the colon, the sequence does not actually represent "order of evaluation":
            // one or the other of the branches is executed, not both.  Still, to make debugging checks
            // work, we want the sequence to match the order in which we'll generate code, which means
            // "else" clause then "then" clause.
            fgSetTreeSeqHelper(op2->AsColon()->ElseNode(), isLIR);
            fgSetTreeSeqHelper(op2, isLIR);
            fgSetTreeSeqHelper(op2->AsColon()->ThenNode(), isLIR);

            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        if (oper == GT_COLON)
        {
            fgSetTreeSeqFinish(tree, isLIR);
            return;
        }

        /* This is a binary operator */

        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            fgSetTreeSeqHelper(op2, isLIR);
            fgSetTreeSeqHelper(op1, isLIR);
        }
        else
        {
            fgSetTreeSeqHelper(op1, isLIR);
            fgSetTreeSeqHelper(op2, isLIR);
        }

        fgSetTreeSeqFinish(tree, isLIR);
        return;
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        case GT_FIELD:
            noway_assert(tree->AsField()->gtFldObj == nullptr);
            break;

        case GT_CALL:

            /* We'll evaluate the 'this' argument value first */
            if (tree->AsCall()->gtCallThisArg != nullptr)
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtCallThisArg->GetNode(), isLIR);
            }

            for (GenTreeCall::Use& use : tree->AsCall()->Args())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }

            for (GenTreeCall::Use& use : tree->AsCall()->LateArgs())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }

            if ((tree->AsCall()->gtCallType == CT_INDIRECT) && (tree->AsCall()->gtCallCookie != nullptr))
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtCallCookie, isLIR);
            }

            if (tree->AsCall()->gtCallType == CT_INDIRECT)
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtCallAddr, isLIR);
            }

            if (tree->AsCall()->gtControlExpr)
            {
                fgSetTreeSeqHelper(tree->AsCall()->gtControlExpr, isLIR);
            }

            break;

        case GT_ARR_ELEM:

            fgSetTreeSeqHelper(tree->AsArrElem()->gtArrObj, isLIR);

            unsigned dim;
            for (dim = 0; dim < tree->AsArrElem()->gtArrRank; dim++)
            {
                fgSetTreeSeqHelper(tree->AsArrElem()->gtArrInds[dim], isLIR);
            }

            break;

        case GT_ARR_OFFSET:
            fgSetTreeSeqHelper(tree->AsArrOffs()->gtOffset, isLIR);
            fgSetTreeSeqHelper(tree->AsArrOffs()->gtIndex, isLIR);
            fgSetTreeSeqHelper(tree->AsArrOffs()->gtArrObj, isLIR);
            break;

        case GT_PHI:
            for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }
            break;

        case GT_FIELD_LIST:
            for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
            {
                fgSetTreeSeqHelper(use.GetNode(), isLIR);
            }
            break;

        case GT_CMPXCHG:
            // Evaluate the trees left to right
            fgSetTreeSeqHelper(tree->AsCmpXchg()->gtOpLocation, isLIR);
            fgSetTreeSeqHelper(tree->AsCmpXchg()->gtOpValue, isLIR);
            fgSetTreeSeqHelper(tree->AsCmpXchg()->gtOpComparand, isLIR);
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            // Evaluate the trees left to right
            fgSetTreeSeqHelper(tree->AsBoundsChk()->gtIndex, isLIR);
            fgSetTreeSeqHelper(tree->AsBoundsChk()->gtArrLen, isLIR);
            break;

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
            noway_assert(!"DYN_BLK nodes should be sequenced as a special case");
            break;

        case GT_INDEX_ADDR:
            // Evaluate the array first, then the index....
            assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);
            fgSetTreeSeqHelper(tree->AsIndexAddr()->Arr(), isLIR);
            fgSetTreeSeqHelper(tree->AsIndexAddr()->Index(), isLIR);
            break;

        default:
#ifdef DEBUG
            gtDispTree(tree);
            noway_assert(!"unexpected operator");
#endif // DEBUG
            break;
    }

    fgSetTreeSeqFinish(tree, isLIR);
}

void Compiler::fgSetTreeSeqFinish(GenTree* tree, bool isLIR)
{
    // If we are sequencing for LIR:
    // - Clear the reverse ops flag
    // - If we are processing a node that does not appear in LIR, do not add it to the list.
    if (isLIR)
    {
        tree->gtFlags &= ~GTF_REVERSE_OPS;

        if (tree->OperIs(GT_LIST, GT_ARGPLACE))
        {
            return;
        }
    }

    /* Append to the node list */
    ++fgTreeSeqNum;

#ifdef DEBUG
    tree->gtSeqNum = fgTreeSeqNum;

    if (verbose & 0)
    {
        printf("SetTreeOrder: ");
        printTreeID(fgTreeSeqLst);
        printf(" followed by ");
        printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    fgTreeSeqLst->gtNext = tree;
    tree->gtNext         = nullptr;
    tree->gtPrev         = fgTreeSeqLst;
    fgTreeSeqLst         = tree;

    /* Remember the very first node */

    if (!fgTreeSeqBeg)
    {
        fgTreeSeqBeg = tree;
        assert(tree->gtSeqNum == 1);
    }
}

/*****************************************************************************
 *
 *  Figure out the order in which operators should be evaluated, along with
 *  other information (such as the register sets trashed by each subtree).
 *  Also finds blocks that need GC polls and inserts them as needed.
 */

void Compiler::fgSetBlockOrder()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgSetBlockOrder()\n");
    }
#endif // DEBUG

#ifdef DEBUG
    BasicBlock::s_nMaxTrees = 0;
#endif

    /* Walk the basic blocks to assign sequence numbers */

    /* If we don't compute the doms, then we never mark blocks as loops. */
    if (fgDomsComputed)
    {
        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
            /* If this block is a loop header, mark it appropriately */

            if (block->isLoopHead())
            {
                fgMarkLoopHead(block);
            }
        }
    }
    else
    {
        /* If we don't have the dominators, use an abbreviated test for fully interruptible.  If there are
         * any back edges, check the source and destination blocks to see if they're GC Safe.  If not, then
         * go fully interruptible. */

        /* XXX Mon 1/21/2008
         * Wouldn't it be nice to have a block iterator that can do this loop?
         */
        for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
        {
// true if the edge is forward, or if it is a back edge and either the source and dest are GC safe.
#define EDGE_IS_GC_SAFE(src, dst)                                                                                      \
    (((src)->bbNum < (dst)->bbNum) || (((src)->bbFlags | (dst)->bbFlags) & BBF_GC_SAFE_POINT))

            bool partiallyInterruptible = true;
            switch (block->bbJumpKind)
            {
                case BBJ_COND:
                case BBJ_ALWAYS:
                    partiallyInterruptible = EDGE_IS_GC_SAFE(block, block->bbJumpDest);
                    break;

                case BBJ_SWITCH:

                    unsigned jumpCnt;
                    jumpCnt = block->bbJumpSwt->bbsCount;
                    BasicBlock** jumpPtr;
                    jumpPtr = block->bbJumpSwt->bbsDstTab;

                    do
                    {
                        partiallyInterruptible &= EDGE_IS_GC_SAFE(block, *jumpPtr);
                    } while (++jumpPtr, --jumpCnt);

                    break;

                default:
                    break;
            }

            if (!partiallyInterruptible)
            {
                // DDB 204533:
                // The GC encoding for fully interruptible methods does not
                // support more than 1023 pushed arguments, so we can't set
                // SetInterruptible() here when we have 1024 or more pushed args
                //
                if (compCanEncodePtrArgCntMax())
                {
                    SetInterruptible(true);
                }
                break;
            }
#undef EDGE_IS_GC_SAFE
        }
    }

    for (BasicBlock* block = fgFirstBB; block; block = block->bbNext)
    {

#if FEATURE_FASTTAILCALL
#ifndef JIT32_GCENCODER
        if (block->endsWithTailCallOrJmp(this, true) && optReachWithoutCall(fgFirstBB, block))
        {
            // This tail call might combine with other tail calls to form a
            // loop.  Thus we need to either add a poll, or make the method
            // fully interruptible.  I chose the later because that's what
            // JIT64 does.
            SetInterruptible(true);
        }
#endif // !JIT32_GCENCODER
#endif // FEATURE_FASTTAILCALL

        fgSetBlockOrder(block);
    }

    /* Remember that now the tree list is threaded */

    fgStmtListThreaded = true;

#ifdef DEBUG
    if (verbose)
    {
        printf("The biggest BB has %4u tree nodes\n", BasicBlock::s_nMaxTrees);
    }
    fgDebugCheckLinks();
#endif // DEBUG
}

/*****************************************************************************/

void Compiler::fgSetStmtSeq(Statement* stmt)
{
    GenTree list; // helper node that we use to start the StmtList
                  // It's located in front of the first node in the list

    /* Assign numbers and next/prev links for this tree */

    fgTreeSeqNum = 0;
    fgTreeSeqLst = &list;
    fgTreeSeqBeg = nullptr;

    fgSetTreeSeqHelper(stmt->GetRootNode(), false);

    /* Record the address of the first node */

    stmt->SetTreeList(fgTreeSeqBeg);

#ifdef DEBUG

    if (list.gtNext->gtPrev != &list)
    {
        printf("&list ");
        printTreeID(&list);
        printf(" != list.next->prev ");
        printTreeID(list.gtNext->gtPrev);
        printf("\n");
        goto BAD_LIST;
    }

    GenTree* temp;
    GenTree* last;
    for (temp = list.gtNext, last = &list; temp != nullptr; last = temp, temp = temp->gtNext)
    {
        if (temp->gtPrev != last)
        {
            printTreeID(temp);
            printf("->gtPrev = ");
            printTreeID(temp->gtPrev);
            printf(", but last = ");
            printTreeID(last);
            printf("\n");

        BAD_LIST:;

            printf("\n");
            gtDispTree(stmt->GetRootNode());
            printf("\n");

            for (GenTree* bad = &list; bad != nullptr; bad = bad->gtNext)
            {
                printf("  entry at ");
                printTreeID(bad);
                printf(" (prev=");
                printTreeID(bad->gtPrev);
                printf(",next=)");
                printTreeID(bad->gtNext);
                printf("\n");
            }

            printf("\n");
            noway_assert(!"Badly linked tree");
            break;
        }
    }
#endif // DEBUG

    /* Fix the first node's 'prev' link */

    noway_assert(list.gtNext->gtPrev == &list);
    list.gtNext->gtPrev = nullptr;

#ifdef DEBUG
    /* Keep track of the highest # of tree nodes */

    if (BasicBlock::s_nMaxTrees < fgTreeSeqNum)
    {
        BasicBlock::s_nMaxTrees = fgTreeSeqNum;
    }
#endif // DEBUG
}

/*****************************************************************************/

void Compiler::fgSetBlockOrder(BasicBlock* block)
{
    for (Statement* stmt : block->Statements())
    {
        fgSetStmtSeq(stmt);

        /* Are there any more trees in this basic block? */

        if (stmt->GetNextStmt() == nullptr)
        {
            /* last statement in the tree list */
            noway_assert(block->lastStmt() == stmt);
            break;
        }

#ifdef DEBUG
        if (block->bbStmtList == stmt)
        {
            /* first statement in the list */
            assert(stmt->GetPrevStmt()->GetNextStmt() == nullptr);
        }
        else
        {
            assert(stmt->GetPrevStmt()->GetNextStmt() == stmt);
        }

        assert(stmt->GetNextStmt()->GetPrevStmt() == stmt);
#endif // DEBUG
    }
}

//------------------------------------------------------------------------
// fgGetFirstNode: Get the first node in the tree, in execution order
//
// Arguments:
//    tree - The top node of the tree of interest
//
// Return Value:
//    The first node in execution order, that belongs to tree.
//
// Assumptions:
//     'tree' must either be a leaf, or all of its constituent nodes must be contiguous
//     in execution order.
//     TODO-Cleanup: Add a debug-only method that verifies this.

/* static */
GenTree* Compiler::fgGetFirstNode(GenTree* tree)
{
    GenTree* child = tree;
    while (child->NumChildren() > 0)
    {
        if (child->OperIsBinary() && child->IsReverseOp())
        {
            child = child->GetChild(1);
        }
        else
        {
            child = child->GetChild(0);
        }
    }
    return child;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult Compiler::fgChkThrowCB(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    // If this tree doesn't have the EXCEPT flag set, then there is no
    // way any of the child nodes could throw, so we can stop recursing.
    if (!(tree->gtFlags & GTF_EXCEPT))
    {
        return Compiler::WALK_SKIP_SUBTREES;
    }

    switch (tree->gtOper)
    {
        case GT_MUL:
        case GT_ADD:
        case GT_SUB:
        case GT_CAST:
            if (tree->gtOverflow())
            {
                return Compiler::WALK_ABORT;
            }
            break;

        case GT_INDEX:
        case GT_INDEX_ADDR:
            // These two call CORINFO_HELP_RNGCHKFAIL for Debug code
            if (tree->gtFlags & GTF_INX_RNGCHK)
            {
                return Compiler::WALK_ABORT;
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
            return Compiler::WALK_ABORT;

        default:
            break;
    }

    return Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult Compiler::fgChkLocAllocCB(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    if (tree->gtOper == GT_LCLHEAP)
    {
        return Compiler::WALK_ABORT;
    }

    return Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
/*static*/
Compiler::fgWalkResult Compiler::fgChkQmarkCB(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    if (tree->gtOper == GT_QMARK)
    {
        return Compiler::WALK_ABORT;
    }

    return Compiler::WALK_CONTINUE;
}

void Compiler::fgLclFldAssign(unsigned lclNum)
{
    assert(varTypeIsStruct(lvaTable[lclNum].lvType));
    if (lvaTable[lclNum].lvPromoted && lvaTable[lclNum].lvFieldCnt > 1)
    {
        lvaSetVarDoNotEnregister(lclNum DEBUGARG(DNER_LocalField));
    }
}

//------------------------------------------------------------------------
// fgCheckCallArgUpdate: check if we are replacing a call argument and add GT_PUTARG_TYPE if necessary.
//
// Arguments:
//    parent   - the parent that could be a call;
//    child    - the new child node;
//    origType - the original child type;
//
// Returns:
//   PUT_ARG_TYPE node if it is needed, nullptr otherwise.
//
GenTree* Compiler::fgCheckCallArgUpdate(GenTree* parent, GenTree* child, var_types origType)
{
    if ((parent == nullptr) || !parent->IsCall())
    {
        return nullptr;
    }
    const var_types newType = child->TypeGet();
    if (newType == origType)
    {
        return nullptr;
    }
    if (varTypeIsStruct(origType) || (genTypeSize(origType) == genTypeSize(newType)))
    {
        assert(!varTypeIsStruct(newType));
        return nullptr;
    }
    GenTree* putArgType = gtNewOperNode(GT_PUTARG_TYPE, origType, child);
#if defined(DEBUG)
    if (verbose)
    {
        printf("For call [%06d] the new argument's type [%06d]", dspTreeID(parent), dspTreeID(child));
        printf(" does not match the original type size, add a GT_PUTARG_TYPE [%06d]\n", dspTreeID(parent));
    }
#endif
    return putArgType;
}
