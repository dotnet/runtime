// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// Flowgraph Construction and Maintenance

void Compiler::fgInit()
{
    impInit();

    /* Initialization for fgWalkTreePre() and fgWalkTreePost() */

    fgFirstBBScratch = nullptr;

#ifdef DEBUG
    fgPrintInlinedMethods = false;
#endif // DEBUG

    /* We haven't yet computed the bbPreds lists */
    fgPredsComputed = false;

    /* We haven't yet computed the edge weight */
    fgEdgeWeightsComputed    = false;
    fgHaveValidEdgeWeights   = false;
    fgSlopUsedInEdgeWeights  = false;
    fgRangeUsedInEdgeWeights = true;
    fgCalledCount            = BB_ZERO_WEIGHT;

    fgReturnBlocksComputed = false;

    /* Initialize the basic block list */

    fgFirstBB          = nullptr;
    fgLastBB           = nullptr;
    fgFirstColdBlock   = nullptr;
    fgEntryBB          = nullptr;
    fgOSREntryBB       = nullptr;
    fgEntryBBExtraRefs = 0;

#if defined(FEATURE_EH_FUNCLETS)
    fgFirstFuncletBB  = nullptr;
    fgFuncletsCreated = false;
#endif // FEATURE_EH_FUNCLETS

    fgBBcount = 0;

#ifdef DEBUG
    fgBBcountAtCodegen = 0;
    fgBBOrder          = nullptr;
#endif // DEBUG

    fgMightHaveNaturalLoops = false;
    fgBBNumMax              = 0;
    fgEdgeCount             = 0;
    fgDomBBcount            = 0;
    fgBBVarSetsInited       = false;
    fgReturnCount           = 0;

    m_dfsTree          = nullptr;
    m_loops            = nullptr;
    m_loopSideEffects  = nullptr;
    m_blockToLoop      = nullptr;
    m_domTree          = nullptr;
    m_reachabilitySets = nullptr;

    // Initialize BlockSet data.
    fgCurBBEpoch             = 0;
    fgCurBBEpochSize         = 0;
    fgBBSetCountInSizeTUnits = 0;

    genReturnBB    = nullptr;
    genReturnLocal = BAD_VAR_NUM;

    /* We haven't reached the global morphing phase */
    fgGlobalMorph     = false;
    fgGlobalMorphDone = false;

    fgModified = false;

#ifdef DEBUG
    fgSafeBasicBlockCreation = true;
    fgSafeFlowEdgeCreation   = true;
#endif // DEBUG

    fgLocalVarLivenessDone = false;
    fgIsDoingEarlyLiveness = false;
    fgDidEarlyLiveness     = false;

    /* Statement list is not threaded yet */

    fgNodeThreading = NodeThreading::None;

    // Initialize the logic for adding code. This is used to insert code such
    // as the code that raises an exception when an array range check fails.
    fgAddCodeList   = nullptr;
    fgAddCodeDscMap = nullptr;

    /* Keep track of the max count of pointer arguments */
    fgPtrArgCntMax = 0;

    /* This global flag is set whenever we remove a statement */
    fgStmtRemoved = false;

    // This global flag is set when we create throw helper blocks
    fgRngChkThrowAdded = false;

    /* Keep track of whether or not EH statements have been optimized */
    fgOptimizedFinally = false;

    /* We will record a list of all BBJ_RETURN blocks here */
    fgReturnBlocks = nullptr;

    fgUsedSharedTemps = nullptr;

#if !defined(FEATURE_EH_FUNCLETS)
    ehMaxHndNestingCount = 0;
#endif // !FEATURE_EH_FUNCLETS

    /* Init the fgBigOffsetMorphingTemps to be BAD_VAR_NUM. */
    for (int i = 0; i < TYP_COUNT; i++)
    {
        fgBigOffsetMorphingTemps[i] = BAD_VAR_NUM;
    }

    fgNoStructPromotion      = false;
    fgNoStructParamPromotion = false;

    optValnumCSE_phase = false; // referenced in fgMorphSmpOp()

#ifdef DEBUG
    fgNormalizeEHDone = false;
#endif // DEBUG

#ifdef DEBUG
    if (!compIsForInlining())
    {
        const int noStructPromotionValue = JitConfig.JitNoStructPromotion();
        assert(0 <= noStructPromotionValue && noStructPromotionValue <= 2);
        if (noStructPromotionValue == 1)
        {
            fgNoStructPromotion = true;
        }
        if (noStructPromotionValue == 2)
        {
            fgNoStructParamPromotion = true;
        }
    }
#endif // DEBUG

#ifdef FEATURE_SIMD
    fgPreviousCandidateSIMDFieldStoreStmt = nullptr;
#endif

    fgHasSwitch                  = false;
    fgPgoDisabled                = false;
    fgPgoSchema                  = nullptr;
    fgPgoData                    = nullptr;
    fgPgoSchemaCount             = 0;
    fgNumProfileRuns             = 0;
    fgPgoBlockCounts             = 0;
    fgPgoEdgeCounts              = 0;
    fgPgoClassProfiles           = 0;
    fgPgoMethodProfiles          = 0;
    fgPgoInlineePgo              = 0;
    fgPgoInlineeNoPgo            = 0;
    fgPgoInlineeNoPgoSingleBlock = 0;
    fgCountInstrumentor          = nullptr;
    fgHistogramInstrumentor      = nullptr;
    fgValueInstrumentor          = nullptr;
    fgPredListSortVector         = nullptr;
    fgCanonicalizedFirstBB       = false;
}

//------------------------------------------------------------------------
// fgEnsureFirstBBisScratch: Ensure that fgFirstBB is a scratch BasicBlock
//
// Returns:
//   True, if a new basic block was allocated.
//
// Notes:
//   This should be called before adding on-entry initialization code to
//   the method, to ensure that fgFirstBB is not part of a loop.
//
//   Does nothing, if fgFirstBB is already a scratch BB. After calling this,
//   fgFirstBB may already contain code. Callers have to be careful
//   that they do not mess up the order of things added to this block and
//   inadvertently change semantics.
//
//   We maintain the invariant that a scratch BB ends with BBJ_ALWAYS,
//   so that when adding independent bits of initialization,
//   callers can generally append to the fgFirstBB block without worrying
//   about what code is there already.
//
//   Can be called at any time, and can be called multiple times.
//
bool Compiler::fgEnsureFirstBBisScratch()
{
    // Have we already allocated a scratch block?
    if (fgFirstBBisScratch())
    {
        return false;
    }

    assert(fgFirstBBScratch == nullptr);

    BasicBlock* block;

    if (fgFirstBB != nullptr)
    {
        // If we have profile data the new block will inherit fgFirstBlock's weight
        if (fgFirstBB->hasProfileWeight())
        {
            block->inheritWeight(fgFirstBB);
        }

        // The first block has an implicit ref count which we must
        // remove. Note the ref count could be greater than one, if
        // the first block is not scratch and is targeted by a
        // branch.
        assert(fgFirstBB->bbRefs >= 1);
        fgFirstBB->bbRefs--;

        // The new scratch bb will fall through to the old first bb
        block = BasicBlock::New(this);
        FlowEdge* const edge = fgAddRefPred(fgFirstBB, block);
        edge->setLikelihood(1.0);
        block->SetKindAndTargetEdge(BBJ_ALWAYS, edge);
        fgInsertBBbefore(fgFirstBB, block);
    }
    else
    {
        noway_assert(fgLastBB == nullptr);
        block = BasicBlock::New(this, BBJ_ALWAYS);
        fgFirstBB = block;
        fgLastBB  = block;
    }

    noway_assert(fgLastBB != nullptr);

    // Set the expected flags
    block->SetFlags(BBF_INTERNAL | BBF_IMPORTED | BBF_NONE_QUIRK);

    // This new first BB has an implicit ref, and no others.
    //
    // But if we call this early, before fgLinkBasicBlocks,
    // defer and let it handle adding the implicit ref.
    //
    block->bbRefs = fgPredsComputed ? 1 : 0;

    fgFirstBBScratch = fgFirstBB;

#ifdef DEBUG
    if (verbose)
    {
        printf("New scratch " FMT_BB "\n", block->bbNum);
    }
#endif

    return true;
}

//------------------------------------------------------------------------
// fgFirstBBisScratch: Check if fgFirstBB is a scratch block
//
// Returns:
//   true if fgFirstBB is a scratch block.
//
bool Compiler::fgFirstBBisScratch()
{
    if (fgFirstBBScratch != nullptr)
    {
        assert(fgFirstBBScratch == fgFirstBB);
        assert(fgFirstBBScratch->HasFlag(BBF_INTERNAL));
        if (fgPredsComputed)
        {
            assert(fgFirstBBScratch->countOfInEdges() == 1);
        }

        // Normally, the first scratch block is a fall-through block. However, if the block after it was an empty
        // BBJ_ALWAYS block, it might get removed, and the code that removes it will make the first scratch block
        // a BBJ_ALWAYS block.
        assert(fgFirstBBScratch->KindIs(BBJ_ALWAYS));

        return true;
    }
    else
    {
        return false;
    }
}

//------------------------------------------------------------------------
// fgBBisScratch: Check if a given block is a scratch block.
//
// Arguments:
//   block - block in question
//
// Returns:
//   true if this block is the first block and is a scratch block.
//
bool Compiler::fgBBisScratch(BasicBlock* block)
{
    return fgFirstBBisScratch() && (block == fgFirstBB);
}

/*
    Removes a block from the return block list
*/
void Compiler::fgRemoveReturnBlock(BasicBlock* block)
{
    if (fgReturnBlocks == nullptr)
    {
        return;
    }

    if (fgReturnBlocks->block == block)
    {
        // It's the 1st entry, assign new head of list.
        fgReturnBlocks = fgReturnBlocks->next;
        return;
    }

    for (BasicBlockList* retBlocks = fgReturnBlocks; retBlocks->next != nullptr; retBlocks = retBlocks->next)
    {
        if (retBlocks->next->block == block)
        {
            // Found it; splice it out.
            retBlocks->next = retBlocks->next->next;
            return;
        }
    }
}

//------------------------------------------------------------------------
// fgConvertBBToThrowBB: Change a given block to a throw block.
//
// Arguments:
//   block - block in question
//
void Compiler::fgConvertBBToThrowBB(BasicBlock* block)
{
    JITDUMP("Converting " FMT_BB " to BBJ_THROW\n", block->bbNum);
    assert(fgPredsComputed);

    // Ordering of the following operations matters.
    // First, if we are looking at the first block of a callfinally pair, remove the pairing.
    // Don't actually remove the BBJ_CALLFINALLYRET as that might affect block iteration in
    // the callers.
    if (block->isBBCallFinallyPair())
    {
        BasicBlock* const leaveBlock = block->Next();
        fgPrepareCallFinallyRetForRemoval(leaveBlock);
    }

    // Scrub this block from the pred lists of any successors
    fgRemoveBlockAsPred(block);

    // Update jump kind after the scrub.
    block->SetKindAndTargetEdge(BBJ_THROW);
    block->RemoveFlags(BBF_RETLESS_CALL); // no longer a BBJ_CALLFINALLY

    // Any block with a throw is rare
    block->bbSetRunRarely();
}

/*****************************************************************************
 * fgChangeSwitchBlock:
 *
 * We have a BBJ_SWITCH jump at 'oldSwitchBlock' and we want to move this
 * switch jump over to 'newSwitchBlock'.  All of the blocks that are jumped
 * to from jumpTab[] need to have their predecessor lists updated by removing
 * the 'oldSwitchBlock' and adding 'newSwitchBlock'.
 */

void Compiler::fgChangeSwitchBlock(BasicBlock* oldSwitchBlock, BasicBlock* newSwitchBlock)
{
    noway_assert(oldSwitchBlock != nullptr);
    noway_assert(newSwitchBlock != nullptr);
    noway_assert(oldSwitchBlock->KindIs(BBJ_SWITCH));
    assert(fgPredsComputed);

    // Walk the switch's jump table, updating the predecessor for each branch.
    BBswtDesc* swtDesc = oldSwitchBlock->GetSwitchTargets();

    for (unsigned i = 0; i < swtDesc->bbsCount; i++)
    {
        FlowEdge* succEdge = swtDesc->bbsDstTab[i];
        assert(succEdge != nullptr);

        if (succEdge->getSourceBlock() != oldSwitchBlock)
        {
            // swtDesc can have duplicate targets, so we may have updated this edge already
            //
            assert(succEdge->getSourceBlock() == newSwitchBlock);
            assert(succEdge->getDupCount() > 1);
        }
        else
        {
            // Redirect edge's source block from oldSwitchBlock to newSwitchBlock,
            // and keep successor block's pred list in order
            //
            fgReplacePred(succEdge, newSwitchBlock);
        }
    }

    if (m_switchDescMap != nullptr)
    {
        SwitchUniqueSuccSet uniqueSuccSet;

        // If already computed and cached the unique descriptors for the old block, let's
        // update those for the new block.
        if (m_switchDescMap->Lookup(oldSwitchBlock, &uniqueSuccSet))
        {
            m_switchDescMap->Set(newSwitchBlock, uniqueSuccSet, BlockToSwitchDescMap::Overwrite);
        }
        else
        {
            fgInvalidateSwitchDescMapEntry(newSwitchBlock);
        }
        fgInvalidateSwitchDescMapEntry(oldSwitchBlock);
    }
}

//------------------------------------------------------------------------
// fgChangeEhfBlock: We have a BBJ_EHFINALLYRET block at 'oldBlock' and we want to move this
// to 'newBlock'. All of the 'oldBlock' successors need to have their predecessor lists updated
// by removing edges to 'oldBlock' and adding edges to 'newBlock'.
//
// Arguments:
//   oldBlock - previous BBJ_EHFINALLYRET block
//   newBlock - block that is replacing 'oldBlock'
//
void Compiler::fgChangeEhfBlock(BasicBlock* oldBlock, BasicBlock* newBlock)
{
    assert(oldBlock != nullptr);
    assert(newBlock != nullptr);
    assert(oldBlock->KindIs(BBJ_EHFINALLYRET));
    assert(fgPredsComputed);

    BBehfDesc* ehfDesc = oldBlock->GetEhfTargets();

    for (unsigned i = 0; i < ehfDesc->bbeCount; i++)
    {
        FlowEdge* succEdge = ehfDesc->bbeSuccs[i];
        fgReplacePred(succEdge, newBlock);
    }
}

//------------------------------------------------------------------------
// fgReplaceEhfSuccessor: update BBJ_EHFINALLYRET block so that all control
//   that previously flowed to oldSucc now flows to newSucc. It is assumed
//   that oldSucc is currently a successor of `block`. We only allow a successor
//   block to appear once in the successor list. Thus, if the new successor
//   already exists in the list, we simply remove the old successor.
//
// Arguments:
//   block   - BBJ_EHFINALLYRET block
//   newSucc - new successor
//   oldSucc - old successor
//
void Compiler::fgReplaceEhfSuccessor(BasicBlock* block, BasicBlock* oldSucc, BasicBlock* newSucc)
{
    assert(block != nullptr);
    assert(oldSucc != nullptr);
    assert(newSucc != nullptr);
    assert(block->KindIs(BBJ_EHFINALLYRET));
    assert(fgPredsComputed);

    BBehfDesc* const ehfDesc   = block->GetEhfTargets();
    const unsigned   succCount = ehfDesc->bbeCount;
    FlowEdge** const succTab   = ehfDesc->bbeSuccs;

    // Walk the successor table looking for the old successor, which we expect to find only once.
    unsigned oldSuccNum = UINT_MAX;
    unsigned newSuccNum = UINT_MAX;
    for (unsigned i = 0; i < succCount; i++)
    {
        assert(succTab[i]->getSourceBlock() == block);

        if (succTab[i]->getDestinationBlock() == newSucc)
        {
            assert(newSuccNum == UINT_MAX);
            newSuccNum = i;
        }

        if (succTab[i]->getDestinationBlock() == oldSucc)
        {
            assert(oldSuccNum == UINT_MAX);
            oldSuccNum = i;
        }
    }

    noway_assert((oldSuccNum != UINT_MAX) && "Did not find oldSucc in succTab[]");

    if (newSuccNum != UINT_MAX)
    {
        // The new successor is already in the table; simply remove the old one.
        fgRemoveEhfSuccessor(block, oldSuccNum);

        JITDUMP("Remove existing BBJ_EHFINALLYRET " FMT_BB " successor " FMT_BB "; replacement successor " FMT_BB
                " already exists in list\n",
                block->bbNum, oldSucc->bbNum, newSucc->bbNum);
    }
    else
    {
        // Remove the old edge [block => oldSucc]
        //
        fgRemoveAllRefPreds(oldSucc, block);

        // Create the new edge [block => newSucc]
        //
        FlowEdge* const newEdge = fgAddRefPred(newSucc, block);

        // Replace the old one with the new one.
        //
        succTab[oldSuccNum] = newEdge;

        JITDUMP("Replace BBJ_EHFINALLYRET " FMT_BB " successor " FMT_BB " with " FMT_BB "\n", block->bbNum,
                oldSucc->bbNum, newSucc->bbNum);
    }
}

//------------------------------------------------------------------------
// fgRemoveEhfSuccessor: update BBJ_EHFINALLYRET block to remove the successor at `succIndex`
// in the block's jump table.
// Updates the predecessor list of the successor, if necessary.
//
// Arguments:
//   block     - BBJ_EHFINALLYRET block
//   succIndex - index of the successor in block->GetEhfTargets()->bbeSuccs
//
void Compiler::fgRemoveEhfSuccessor(BasicBlock* block, const unsigned succIndex)
{
    assert(block != nullptr);
    assert(block->KindIs(BBJ_EHFINALLYRET));
    assert(fgPredsComputed);

    BBehfDesc* const ehfDesc   = block->GetEhfTargets();
    const unsigned   succCount = ehfDesc->bbeCount;
    FlowEdge**       succTab   = ehfDesc->bbeSuccs;
    assert(succIndex < succCount);
    FlowEdge* succEdge = succTab[succIndex];

    fgRemoveRefPred(succEdge);

    // If succEdge not the last entry, move everything after in the table down one slot.
    if ((succIndex + 1) < succCount)
    {
        memmove_s(&succTab[succIndex], (succCount - succIndex) * sizeof(FlowEdge*), &succTab[succIndex + 1],
                  (succCount - succIndex - 1) * sizeof(FlowEdge*));
    }

#ifdef DEBUG
    // We only expect to see a successor once in the table.
    for (unsigned i = succIndex; i < (succCount - 1); i++)
    {
        assert(succTab[i]->getDestinationBlock() != succEdge->getDestinationBlock());
    }
#endif // DEBUG

    ehfDesc->bbeCount--;
}

//------------------------------------------------------------------------
// fgRemoveEhfSuccessor: Removes `succEdge` from its BBJ_EHFINALLYRET source block's jump table.
// Updates the predecessor list of the successor block, if necessary.
//
// Arguments:
//   block     - BBJ_EHFINALLYRET block
//   succEdge - FlowEdge* to be removed from predecessor block's jump table
//
void Compiler::fgRemoveEhfSuccessor(FlowEdge* succEdge)
{
    assert(succEdge != nullptr);
    assert(fgPredsComputed);

    BasicBlock* block = succEdge->getSourceBlock();
    assert(block != nullptr);
    assert(block->KindIs(BBJ_EHFINALLYRET));

    fgRemoveRefPred(succEdge);

    BBehfDesc* const ehfDesc   = block->GetEhfTargets();
    const unsigned   succCount = ehfDesc->bbeCount;
    FlowEdge**       succTab   = ehfDesc->bbeSuccs;
    bool             found     = false;

    // Search succTab for succEdge so we can splice it out of the table.
    for (unsigned i = 0; i < succCount; i++)
    {
        if (succTab[i] == succEdge)
        {
            // If succEdge not the last entry, move everything after in the table down one slot.
            if ((i + 1) < succCount)
            {
                memmove_s(&succTab[i], (succCount - i) * sizeof(FlowEdge*), &succTab[i + 1],
                          (succCount - i - 1) * sizeof(FlowEdge*));
            }

            found = true;

#ifdef DEBUG
            // We only expect to see a successor once in the table.
            for (; i < (succCount - 1); i++)
            {
                assert(succTab[i]->getDestinationBlock() != succEdge->getDestinationBlock());
            }
#endif // DEBUG
        }
    }

    assert(found);
    ehfDesc->bbeCount--;
}

//------------------------------------------------------------------------
// Compiler::fgReplaceJumpTarget: For a given block, replace the target 'oldTarget' with 'newTarget'.
//
// Arguments:
//    block     - the block in which a jump target will be replaced.
//    newTarget - the new branch target of the block.
//    oldTarget - the old branch target of the block.
//
// Notes:
// 1. Only branches are changed: BBJ_ALWAYS, the non-fallthrough path of BBJ_COND, BBJ_SWITCH, etc.
//    We assert for other jump kinds.
// 2. All branch targets found are updated. If there are multiple ways for a block
//    to reach 'oldTarget' (e.g., multiple arms of a switch), all of them are changed.
// 3. The predecessor lists are updated.
// 4. If any switch table entry was updated, the switch table "unique successor" cache is invalidated.
//
void Compiler::fgReplaceJumpTarget(BasicBlock* block, BasicBlock* oldTarget, BasicBlock* newTarget)
{
    assert(block != nullptr);
    assert(fgPredsComputed);

    switch (block->GetKind())
    {
        case BBJ_CALLFINALLY:
        case BBJ_CALLFINALLYRET:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
        case BBJ_LEAVE: // This function can be called before import, so we still have BBJ_LEAVE
        {
            assert(block->TargetIs(oldTarget));
            fgRemoveRefPred(block->GetTargetEdge());
            FlowEdge* const newEdge = fgAddRefPred(newTarget, block, block->GetTargetEdge());
            block->SetTargetEdge(newEdge);
            break;
        }

        case BBJ_COND:

            if (block->TrueTargetIs(oldTarget))
            {
                FlowEdge* const oldEdge = block->GetTrueEdge();

                if (block->TrueEdgeIs(oldEdge))
                {
                    // fgRemoveRefPred returns nullptr for BBJ_COND blocks with two flow edges to target
                    fgRemoveConditionalJump(block);
                    assert(block->KindIs(BBJ_ALWAYS));
                    assert(block->TargetIs(oldTarget));
                }

                // fgRemoveRefPred should have removed the flow edge
                fgRemoveRefPred(oldEdge);
                assert(oldEdge->getDupCount() == 0);

                // TODO-NoFallThrough: Proliferate weight from oldEdge
                // (as a quirk, we avoid doing so for the true target to reduce diffs for now)
                FlowEdge* const newEdge = fgAddRefPred(newTarget, block);

                if (block->KindIs(BBJ_ALWAYS))
                {
                    newEdge->setLikelihood(1.0);
                    block->SetTargetEdge(newEdge);
                }
                else
                {
                    assert(block->KindIs(BBJ_COND));
                    block->SetTrueEdge(newEdge);

                    if (oldEdge->hasLikelihood())
                    {
                        newEdge->setLikelihood(oldEdge->getLikelihood());
                    }
                }
            }
            else
            {
                assert(block->FalseTargetIs(oldTarget));
                FlowEdge* const oldEdge = block->GetFalseEdge();

                // fgRemoveRefPred should have removed the flow edge
                fgRemoveRefPred(oldEdge);
                assert(oldEdge->getDupCount() == 0);
                FlowEdge* const newEdge = fgAddRefPred(newTarget, block, oldEdge);
                block->SetFalseEdge(newEdge);
            }
            break;

        case BBJ_SWITCH:
        {
            unsigned const   jumpCnt = block->GetSwitchTargets()->bbsCount;
            FlowEdge** const jumpTab = block->GetSwitchTargets()->bbsDstTab;
            bool             changed = false;

            for (unsigned i = 0; i < jumpCnt; i++)
            {
                if (jumpTab[i]->getDestinationBlock() == oldTarget)
                {
                    fgRemoveRefPred(jumpTab[i]);
                    jumpTab[i] = fgAddRefPred(newTarget, block, jumpTab[i]);
                    changed    = true;
                }
            }

            if (changed)
            {
                InvalidateUniqueSwitchSuccMap();
            }
            break;
        }

        case BBJ_EHFINALLYRET:
            fgReplaceEhfSuccessor(block, oldTarget, newTarget);
            break;

        default:
            assert(!"Block doesn't have a jump target!");
            unreached();
            break;
    }
}

//------------------------------------------------------------------------
// fgReplacePred: update the predecessor list, swapping one pred for another
//
// Arguments:
//   block - block with the pred list we want to update
//   oldPred - pred currently appearing in block's pred list
//   newPred - pred that will take oldPred's place.
//
// Notes:
//
// A block can only appear once in the preds list. If a predecessor has multiple
// ways to get to this block, then the pred edge DupCount will be >1.
//
// This function assumes that all branches from the predecessor (practically, that all
// switch cases that target this block) are changed to branch from the new predecessor,
// with the same dup count.
//
// Note that the block bbRefs is not changed, since 'block' has the same number of
// references as before, just from a different predecessor block.
//
// Also note this may cause sorting of the pred list.
//
void Compiler::fgReplacePred(BasicBlock* block, BasicBlock* oldPred, BasicBlock* newPred)
{
    noway_assert(block != nullptr);
    noway_assert(oldPred != nullptr);
    noway_assert(newPred != nullptr);

    bool modified = false;

    for (FlowEdge* const pred : block->PredEdges())
    {
        if (oldPred == pred->getSourceBlock())
        {
            pred->setSourceBlock(newPred);
            modified = true;
            break;
        }
    }

    // We may now need to reorder the pred list.
    //
    if (modified)
    {
        block->ensurePredListOrder(this);
    }
}

//------------------------------------------------------------------------
// fgReplacePred: redirects the given edge to a new predecessor block
//
// Arguments:
//   edge - the edge whose source block we want to update
//   newPred - the new predecessor block for edge
//
// Notes:
//
// This function assumes that all branches from the predecessor (practically, that all
// switch cases that target the successor block) are changed to branch from the new predecessor,
// with the same dup count.
//
// Note that the successor block's bbRefs is not changed, since it has the same number of
// references as before, just from a different predecessor block.
//
// Also note this may cause sorting of the pred list.
//
void Compiler::fgReplacePred(FlowEdge* edge, BasicBlock* const newPred)
{
    assert(edge != nullptr);
    assert(newPred != nullptr);
    assert(edge->getSourceBlock() != newPred);

    edge->setSourceBlock(newPred);

    // We may now need to reorder the pred list.
    //
    BasicBlock* succBlock = edge->getDestinationBlock();
    assert(succBlock != nullptr);
    succBlock->ensurePredListOrder(this);
}

/*****************************************************************************
 *  For a block that is in a handler region, find the first block of the most-nested
 *  handler containing the block.
 */
BasicBlock* Compiler::fgFirstBlockOfHandler(BasicBlock* block)
{
    assert(block->hasHndIndex());
    return ehGetDsc(block->getHndIndex())->ebdHndBeg;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// fgInvalidateBBLookup: In non-Release builds, set fgBBs to a dummy value.
// After calling this, fgInitBBLookup must be called before using fgBBs again.
//
void Compiler::fgInvalidateBBLookup()
{
    fgBBs = (BasicBlock**)0xCDCD;
}
#endif // DEBUG

/*****************************************************************************
 *
 *  The following helps find a basic block given its PC offset.
 */

void Compiler::fgInitBBLookup()
{
    BasicBlock** dscBBptr;

    /* Allocate the basic block table */

    dscBBptr = fgBBs = new (this, CMK_BasicBlock) BasicBlock*[fgBBcount];

    /* Walk all the basic blocks, filling in the table */

    for (BasicBlock* const block : Blocks())
    {
        *dscBBptr++ = block;
    }

    noway_assert(dscBBptr == fgBBs + fgBBcount);
}

BasicBlock* Compiler::fgLookupBB(unsigned addr)
{
    unsigned lo;
    unsigned hi;

    /* Do a binary search */

    for (lo = 0, hi = fgBBcount - 1;;)
    {

    AGAIN:;

        if (lo > hi)
        {
            break;
        }

        unsigned    mid = (lo + hi) / 2;
        BasicBlock* dsc = fgBBs[mid];

        // We introduce internal blocks for BBJ_CALLFINALLY. Skip over these.

        while (dsc->HasFlag(BBF_INTERNAL))
        {
            dsc = dsc->Next();
            mid++;

            // We skipped over too many, Set hi back to the original mid - 1

            if (mid > hi)
            {
                mid = (lo + hi) / 2;
                hi  = mid - 1;
                goto AGAIN;
            }
        }

        unsigned pos = dsc->bbCodeOffs;

        if (pos < addr)
        {
            if ((lo == hi) && (lo == (fgBBcount - 1)))
            {
                noway_assert(addr == dsc->bbCodeOffsEnd);
                return nullptr; // NULL means the end of method
            }
            lo = mid + 1;
            continue;
        }

        if (pos > addr)
        {
            hi = mid - 1;
            continue;
        }

        return dsc;
    }
#ifdef DEBUG
    printf("ERROR: Couldn't find basic block at offset %04X\n", addr);
#endif // DEBUG
    NO_WAY("fgLookupBB failed.");
}

//------------------------------------------------------------------------
// FgStack: simple stack model for the inlinee's evaluation stack.
//
// Model the inputs available to various operations in the inline body.
// Tracks constants, arguments, array lengths.

class FgStack
{
public:
    FgStack() : slot0(SLOT_INVALID), slot1(SLOT_INVALID), depth(0)
    {
        // Empty
    }

    enum FgSlot
    {
        SLOT_INVALID  = UINT_MAX,
        SLOT_UNKNOWN  = 0,
        SLOT_CONSTANT = 1,
        SLOT_ARRAYLEN = 2,
        SLOT_ARGUMENT = 3
    };

    void Clear()
    {
        depth = 0;
    }
    void PushUnknown()
    {
        Push(SLOT_UNKNOWN);
    }
    void PushConstant()
    {
        Push(SLOT_CONSTANT);
    }
    void PushArrayLen()
    {
        Push(SLOT_ARRAYLEN);
    }
    void PushArgument(unsigned arg)
    {
        Push((FgSlot)(SLOT_ARGUMENT + arg));
    }
    FgSlot GetSlot0() const
    {
        return depth >= 1 ? slot0 : FgSlot::SLOT_UNKNOWN;
    }
    FgSlot GetSlot1() const
    {
        return depth >= 2 ? slot1 : FgSlot::SLOT_UNKNOWN;
    }
    FgSlot Top(const int n = 0)
    {
        if (n == 0)
        {
            return depth >= 1 ? slot0 : SLOT_UNKNOWN;
        }
        if (n == 1)
        {
            return depth == 2 ? slot1 : SLOT_UNKNOWN;
        }
        unreached();
    }
    static bool IsConstant(FgSlot value)
    {
        return value == SLOT_CONSTANT;
    }
    static bool IsConstantOrConstArg(FgSlot value, InlineInfo* info)
    {
        return IsConstant(value) || IsConstArgument(value, info);
    }
    static bool IsArrayLen(FgSlot value)
    {
        return value == SLOT_ARRAYLEN;
    }
    static bool IsArgument(FgSlot value)
    {
        return value >= SLOT_ARGUMENT;
    }
    static bool IsConstArgument(FgSlot value, InlineInfo* info)
    {
        if ((info == nullptr) || !IsArgument(value))
        {
            return false;
        }
        const unsigned argNum = value - SLOT_ARGUMENT;
        if (argNum < info->argCnt)
        {
            return info->inlArgInfo[argNum].argIsInvariant;
        }
        return false;
    }
    static bool IsExactArgument(FgSlot value, InlineInfo* info)
    {
        if ((info == nullptr) || !IsArgument(value))
        {
            return false;
        }
        const unsigned argNum = value - SLOT_ARGUMENT;
        if (argNum < info->argCnt)
        {
            return info->inlArgInfo[argNum].argIsExact;
        }
        return false;
    }
    static unsigned SlotTypeToArgNum(FgSlot value)
    {
        assert(IsArgument(value));
        return value - SLOT_ARGUMENT;
    }
    bool IsStackTwoDeep() const
    {
        return depth == 2;
    }
    bool IsStackOneDeep() const
    {
        return depth == 1;
    }
    bool IsStackAtLeastOneDeep() const
    {
        return depth >= 1;
    }
    void Push(FgSlot slot)
    {
        assert(depth <= 2);
        slot1 = slot0;
        slot0 = slot;
        if (depth < 2)
        {
            depth++;
        }
    }

private:
    FgSlot   slot0;
    FgSlot   slot1;
    unsigned depth;
};

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
//    Also sets m_addrExposed and lvHasILStoreOp, ilHasMultipleILStoreOp in lvaTable[].
//
void Compiler::fgFindJumpTargets(const BYTE* codeAddr, IL_OFFSET codeSize, FixedBitVect* jumpTarget)
{
    const BYTE* codeBegp = codeAddr;
    const BYTE* codeEndp = codeAddr + codeSize;
    unsigned    varNum;
    var_types   varType      = DUMMY_INIT(TYP_UNDEF); // TYP_ type
    bool        typeIsNormed = false;
    FgStack     pushedStack;
    const bool  isForceInline          = (info.compFlags & CORINFO_FLG_FORCEINLINE) != 0;
    const bool  makeInlineObservations = (compInlineResult != nullptr);
    const bool  isInlining             = compIsForInlining();
    unsigned    retBlocks              = 0;
    int         prefixFlags            = 0;
    bool        preciseScan            = makeInlineObservations && compInlineResult->GetPolicy()->RequiresPreciseScan();
    const bool  resolveTokens          = preciseScan;

    // Track offsets where IL instructions begin in DEBUG builds. Used to
    // validate debug info generated by the JIT.
    assert(codeSize == compInlineContext->GetILSize());
    INDEBUG(FixedBitVect* ilInstsSet = FixedBitVect::bitVectInit(codeSize, this));

    if (makeInlineObservations)
    {
        // Set default values for profile (to avoid NoteFailed in CALLEE_IL_CODE_SIZE's handler)
        // these will be overridden later.
        compInlineResult->NoteBool(InlineObservation::CALLSITE_HAS_PROFILE_WEIGHTS, true);
        compInlineResult->NoteDouble(InlineObservation::CALLSITE_PROFILE_FREQUENCY, 1.0);
        // Observe force inline state and code size.
        compInlineResult->NoteBool(InlineObservation::CALLEE_IS_FORCE_INLINE, isForceInline);
        compInlineResult->NoteInt(InlineObservation::CALLEE_IL_CODE_SIZE, codeSize);

        // Determine if call site is within a try.
        if (isInlining && impInlineInfo->iciBlock->hasTryIndex())
        {
            compInlineResult->Note(InlineObservation::CALLSITE_IN_TRY_REGION);
        }

        // Determine if the call site is in a no-return block
        if (isInlining && impInlineInfo->iciBlock->KindIs(BBJ_THROW))
        {
            compInlineResult->Note(InlineObservation::CALLSITE_IN_NORETURN_REGION);
        }

        // Determine if the call site is in a loop.
        if (isInlining && impInlineInfo->iciBlock->HasFlag(BBF_BACKWARD_JUMP))
        {
            compInlineResult->Note(InlineObservation::CALLSITE_IN_LOOP);
        }

#ifdef DEBUG

        // If inlining, this method should still be a candidate.
        if (isInlining)
        {
            assert(compInlineResult->IsCandidate());
        }

#endif // DEBUG

        // note that we're starting to look at the opcodes.
        compInlineResult->Note(InlineObservation::CALLEE_BEGIN_OPCODE_SCAN);
    }

    CORINFO_RESOLVED_TOKEN resolvedToken;

    OPCODE opcode     = CEE_NOP;
    OPCODE prevOpcode = CEE_NOP;
    bool   handled    = false;
    while (codeAddr < codeEndp)
    {
        prevOpcode = opcode;
        opcode     = (OPCODE)getU1LittleEndian(codeAddr);

        INDEBUG(ilInstsSet->bitVectSet((UINT)(codeAddr - codeBegp)));

        codeAddr += sizeof(__int8);

        if (!handled && preciseScan)
        {
            // Push something unknown to the stack since we couldn't find anything useful for inlining
            pushedStack.PushUnknown();
        }
        handled = false;

    DECODE_OPCODE:

        if ((unsigned)opcode >= CEE_COUNT)
        {
            BADCODE3("Illegal opcode", ": %02X", (int)opcode);
        }

        if ((opcode >= CEE_LDARG_0 && opcode <= CEE_STLOC_S) || (opcode >= CEE_LDARG && opcode <= CEE_STLOC))
        {
            opts.lvRefCount++;
        }

        if (makeInlineObservations && (opcode >= CEE_LDNULL) && (opcode <= CEE_LDC_R8))
        {
            // LDTOKEN and LDSTR are handled below
            pushedStack.PushConstant();
            handled = true;
        }

        unsigned sz = opcodeSizes[opcode];

        switch (opcode)
        {
            case CEE_PREFIX1:
            {
                if (codeAddr >= codeEndp)
                {
                    goto TOO_FAR;
                }
                opcode = (OPCODE)(256 + getU1LittleEndian(codeAddr));
                codeAddr += sizeof(__int8);
                goto DECODE_OPCODE;
            }

            case CEE_PREFIX2:
            case CEE_PREFIX3:
            case CEE_PREFIX4:
            case CEE_PREFIX5:
            case CEE_PREFIX6:
            case CEE_PREFIX7:
            case CEE_PREFIXREF:
            {
                BADCODE3("Illegal opcode", ": %02X", (int)opcode);
            }

            case CEE_SIZEOF:
            case CEE_LDTOKEN:
            case CEE_LDSTR:
            {
                if (preciseScan)
                {
                    pushedStack.PushConstant();
                    handled = true;
                }
                break;
            }

            case CEE_DUP:
            {
                if (preciseScan)
                {
                    pushedStack.Push(pushedStack.Top());
                    handled = true;
                }
                break;
            }

            case CEE_THROW:
            {
                if (makeInlineObservations)
                {
                    compInlineResult->Note(InlineObservation::CALLEE_THROW_BLOCK);
                }
                break;
            }

            case CEE_BOX:
            {
                if (makeInlineObservations)
                {
                    int toSkip =
                        impBoxPatternMatch(nullptr, codeAddr + sz, codeEndp, BoxPatterns::MakeInlineObservation);
                    if (toSkip > 0)
                    {
                        // toSkip > 0 means we most likely will hit a pattern (e.g. box+isinst+brtrue) that
                        // will be folded into a const

                        if (preciseScan)
                        {
                            codeAddr += toSkip;
                        }
                    }
                }
                break;
            }

            case CEE_CASTCLASS:
            case CEE_ISINST:
            {
                if (makeInlineObservations)
                {
                    FgStack::FgSlot slot = pushedStack.Top();
                    if (FgStack::IsConstantOrConstArg(slot, impInlineInfo) ||
                        FgStack::IsExactArgument(slot, impInlineInfo))
                    {
                        compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_EXPR_UN);
                        handled = true; // and keep argument in the pushedStack
                    }
                    else if (FgStack::IsArgument(slot))
                    {
                        compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_CAST);
                        handled = true; // and keep argument in the pushedStack
                    }
                }
                break;
            }

            case CEE_CALL:
            case CEE_CALLVIRT:
            {
                // There has to be code after the call, otherwise the inlinee is unverifiable.
                if (isInlining)
                {
                    noway_assert(codeAddr < codeEndp - sz);
                }

                if (!makeInlineObservations)
                {
                    break;
                }

                CORINFO_METHOD_HANDLE methodHnd   = nullptr;
                bool                  isIntrinsic = false;
                NamedIntrinsic        ni          = NI_Illegal;

                if (resolveTokens)
                {
                    impResolveToken(codeAddr, &resolvedToken, CORINFO_TOKENKIND_Method);
                    methodHnd   = resolvedToken.hMethod;
                    isIntrinsic = eeIsIntrinsic(methodHnd);
                }

                if (isIntrinsic)
                {
                    ni = lookupNamedIntrinsic(methodHnd);

                    bool foldableIntrinsic = false;

                    if (IsMathIntrinsic(ni))
                    {
                        // Most Math(F) intrinsics have single arguments
                        foldableIntrinsic = FgStack::IsConstantOrConstArg(pushedStack.Top(), impInlineInfo);
                    }
                    else
                    {
                        switch (ni)
                        {
                            // These are most likely foldable without arguments
                            case NI_System_Collections_Generic_Comparer_get_Default:
                            case NI_System_Collections_Generic_EqualityComparer_get_Default:
                            case NI_System_Enum_HasFlag:
                            case NI_System_GC_KeepAlive:
                            {
                                pushedStack.PushUnknown();
                                foldableIntrinsic = true;
                                break;
                            }

                            case NI_System_SpanHelpers_SequenceEqual:
                            case NI_System_Buffer_Memmove:
                            {
                                if (FgStack::IsConstArgument(pushedStack.Top(), impInlineInfo))
                                {
                                    // Constant (at its call-site) argument feeds the Memmove/Memcmp length argument.
                                    // We most likely will be able to unroll it.
                                    // It is important to only raise this hint for constant arguments, if it's just a
                                    // constant in the inlinee itself then we don't need to inline it for unrolling.
                                    compInlineResult->Note(InlineObservation::CALLSITE_UNROLLABLE_MEMOP);
                                }
                                break;
                            }

                            case NI_System_Span_get_Item:
                            case NI_System_ReadOnlySpan_get_Item:
                            {
                                if (FgStack::IsArgument(pushedStack.Top(0)) || FgStack::IsArgument(pushedStack.Top(1)))
                                {
                                    compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK);
                                }
                                break;
                            }

                            case NI_System_Runtime_CompilerServices_RuntimeHelpers_IsKnownConstant:
                                if (FgStack::IsConstArgument(pushedStack.Top(), impInlineInfo))
                                {
                                    compInlineResult->Note(InlineObservation::CALLEE_CONST_ARG_FEEDS_ISCONST);
                                }
                                else
                                {
                                    compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_ISCONST);
                                }
                                // RuntimeHelpers.IsKnownConstant is always folded into a const
                                pushedStack.PushConstant();
                                foldableIntrinsic = true;
                                break;

                            // These are foldable if the first argument is a constant
                            case NI_PRIMITIVE_LeadingZeroCount:
                            case NI_PRIMITIVE_Log2:
                            case NI_PRIMITIVE_PopCount:
                            case NI_PRIMITIVE_TrailingZeroCount:
                            case NI_System_Type_get_IsEnum:
                            case NI_System_Type_GetEnumUnderlyingType:
                            case NI_System_Type_get_IsValueType:
                            case NI_System_Type_get_IsPrimitive:
                            case NI_System_Type_get_IsByRefLike:
                            case NI_System_Type_GetTypeFromHandle:
                            case NI_System_String_get_Length:
                            case NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness:
#if defined(FEATURE_HW_INTRINSICS)
#if defined(TARGET_ARM64)
                            case NI_ArmBase_Arm64_LeadingZeroCount:
                            case NI_ArmBase_Arm64_ReverseElementBits:
                            case NI_ArmBase_LeadingZeroCount:
                            case NI_ArmBase_ReverseElementBits:
                            case NI_Vector64_Create:
                            case NI_Vector64_CreateScalar:
                            case NI_Vector64_CreateScalarUnsafe:
#endif // TARGET_ARM64
                            case NI_Vector2_Create:
                            case NI_Vector2_CreateBroadcast:
                            case NI_Vector3_Create:
                            case NI_Vector3_CreateBroadcast:
                            case NI_Vector3_CreateFromVector2:
                            case NI_Vector4_Create:
                            case NI_Vector4_CreateBroadcast:
                            case NI_Vector4_CreateFromVector2:
                            case NI_Vector4_CreateFromVector3:
                            case NI_Vector128_Create:
                            case NI_Vector128_CreateScalar:
                            case NI_Vector128_CreateScalarUnsafe:
                            case NI_VectorT_CreateBroadcast:
#if defined(TARGET_XARCH)
                            case NI_BMI1_TrailingZeroCount:
                            case NI_BMI1_X64_TrailingZeroCount:
                            case NI_LZCNT_LeadingZeroCount:
                            case NI_LZCNT_X64_LeadingZeroCount:
                            case NI_POPCNT_PopCount:
                            case NI_POPCNT_X64_PopCount:
                            case NI_Vector256_Create:
                            case NI_Vector512_Create:
                            case NI_Vector256_CreateScalar:
                            case NI_Vector512_CreateScalar:
                            case NI_Vector256_CreateScalarUnsafe:
                            case NI_Vector512_CreateScalarUnsafe:
                            case NI_X86Base_BitScanForward:
                            case NI_X86Base_X64_BitScanForward:
                            case NI_X86Base_BitScanReverse:
                            case NI_X86Base_X64_BitScanReverse:
#endif // TARGET_XARCH
#endif // FEATURE_HW_INTRINSICS
                            {
                                // Top() in order to keep it as is in case of foldableIntrinsic
                                if (FgStack::IsConstantOrConstArg(pushedStack.Top(), impInlineInfo))
                                {
                                    foldableIntrinsic = true;
                                }
                                break;
                            }

                            // These are foldable if two arguments are constants
                            case NI_PRIMITIVE_RotateLeft:
                            case NI_PRIMITIVE_RotateRight:
                            case NI_System_Type_op_Equality:
                            case NI_System_Type_op_Inequality:
                            case NI_System_String_get_Chars:
                            case NI_System_Type_IsAssignableTo:
                            case NI_System_Type_IsAssignableFrom:
                            {
                                if (FgStack::IsConstantOrConstArg(pushedStack.Top(0), impInlineInfo) &&
                                    FgStack::IsConstantOrConstArg(pushedStack.Top(1), impInlineInfo))
                                {
                                    foldableIntrinsic = true;
                                    pushedStack.PushConstant();
                                }
                                break;
                            }

                            case NI_IsSupported_True:
                            case NI_IsSupported_False:
                            case NI_IsSupported_Type:
                            {
                                foldableIntrinsic = true;
                                pushedStack.PushConstant();
                                break;
                            }

                            case NI_Vector_GetCount:
                            {
                                foldableIntrinsic = true;
                                pushedStack.PushConstant();
                                // TODO: for FEATURE_SIMD check if it's a loop condition - we unroll such loops.
                                break;
                            }

                            case NI_SRCS_UNSAFE_Add:
                            case NI_SRCS_UNSAFE_AddByteOffset:
                            case NI_SRCS_UNSAFE_AreSame:
                            case NI_SRCS_UNSAFE_ByteOffset:
                            case NI_SRCS_UNSAFE_IsAddressGreaterThan:
                            case NI_SRCS_UNSAFE_IsAddressLessThan:
                            case NI_SRCS_UNSAFE_IsNullRef:
                            case NI_SRCS_UNSAFE_Subtract:
                            case NI_SRCS_UNSAFE_SubtractByteOffset:
                            {
                                // These are effectively primitive binary operations so the
                                // handling roughly mirrors the handling for CEE_ADD and
                                // friends that exists elsewhere in this method

                                if (!preciseScan)
                                {
                                    switch (ni)
                                    {
                                        case NI_SRCS_UNSAFE_AreSame:
                                        case NI_SRCS_UNSAFE_IsAddressGreaterThan:
                                        case NI_SRCS_UNSAFE_IsAddressLessThan:
                                        case NI_SRCS_UNSAFE_IsNullRef:
                                        {
                                            fgObserveInlineConstants(opcode, pushedStack, isInlining);
                                            break;
                                        }

                                        default:
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    // Unlike the normal binary operation handling, this is an intrinsic call that will
                                    // get replaced
                                    // with simple IR, so we care about `const op const` as well.

                                    FgStack::FgSlot arg0;

                                    bool isArg0Arg, isArg0Const, isArg1Const;
                                    bool isArg1Arg, isArg0ConstArg, isArg1ConstArg;

                                    if (ni == NI_SRCS_UNSAFE_IsNullRef)
                                    {
                                        // IsNullRef is unary, but it always compares against 0

                                        arg0 = pushedStack.Top(0);

                                        isArg0Arg      = FgStack::IsArgument(arg0);
                                        isArg0Const    = FgStack::IsConstant(arg0);
                                        isArg0ConstArg = FgStack::IsConstArgument(arg0, impInlineInfo);

                                        isArg1Arg      = false;
                                        isArg1Const    = true;
                                        isArg1ConstArg = false;
                                    }
                                    else
                                    {
                                        arg0 = pushedStack.Top(1);

                                        isArg0Arg      = FgStack::IsArgument(arg0);
                                        isArg0Const    = FgStack::IsConstant(arg0);
                                        isArg0ConstArg = FgStack::IsConstArgument(arg0, impInlineInfo);

                                        FgStack::FgSlot arg1 = pushedStack.Top(0);

                                        isArg1Arg      = FgStack::IsArgument(arg0);
                                        isArg1Const    = FgStack::IsConstant(arg1);
                                        isArg1ConstArg = FgStack::IsConstantOrConstArg(arg1, impInlineInfo);
                                    }

                                    // Const op ConstArg -> ConstArg
                                    if (isArg0Const && isArg1ConstArg)
                                    {
                                        // keep stack unchanged
                                        foldableIntrinsic = true;
                                    }
                                    // ConstArg op Const    -> ConstArg
                                    // ConstArg op ConstArg -> ConstArg
                                    else if (isArg0ConstArg && (isArg1Const || isArg1ConstArg))
                                    {
                                        if (isArg1Const)
                                        {
                                            pushedStack.Push(arg0);
                                        }
                                        foldableIntrinsic = true;
                                    }
                                    // Const op Const -> Const
                                    else if (isArg0Const && isArg1Const)
                                    {
                                        // both are constants so we still want to track this as foldable, unlike
                                        // what is done for the regulary binary operator handling, since we have
                                        // a CEE_CALL node and not something more primitive
                                        foldableIntrinsic = true;
                                    }
                                    // Arg op ConstArg
                                    // Arg op Const
                                    else if (isArg0Arg && (isArg1Const || isArg1ConstArg))
                                    {
                                        // "Arg op CNS" --> keep arg0 in the stack for the next ops
                                        pushedStack.Push(arg0);
                                        handled = true;

                                        // TODO-CQ: The normal binary operator handling pushes arg0
                                        // and tracks this as CALLEE_BINARY_EXRP_WITH_CNS. We can't trivially
                                        // do the same here without more work.
                                    }
                                    // ConstArg op Arg
                                    // Const    op Arg
                                    else if (isArg1Arg && (isArg0Const || isArg0ConstArg))
                                    {
                                        // "CNS op ARG" --> keep arg1 in the stack for the next ops
                                        handled = true;

                                        // TODO-CQ: The normal binary operator handling keeps arg1
                                        // and tracks this as CALLEE_BINARY_EXRP_WITH_CNS. We can't trivially
                                        // do the same here without more work.
                                    }

                                    // X op ConstArg
                                    if (isArg1ConstArg)
                                    {
                                        pushedStack.Push(arg0);
                                        handled = true;
                                    }
                                }

                                break;
                            }

                            case NI_SRCS_UNSAFE_AsPointer:
                            {
                                // These are effectively primitive unary operations so the
                                // handling roughly mirrors the handling for CEE_CONV_U and
                                // friends that exists elsewhere in this method

                                FgStack::FgSlot arg = pushedStack.Top();

                                if (FgStack::IsConstArgument(arg, impInlineInfo))
                                {
                                    foldableIntrinsic = true;
                                }
                                else if (FgStack::IsArgument(arg))
                                {
                                    handled = true;
                                }
                                else if (FgStack::IsConstant(arg))
                                {
                                    // input is a constant so we still want to track this as foldable, unlike
                                    // what is done for the regulary unary operator handling, since we have
                                    // a CEE_CALL node and not something more primitive
                                    foldableIntrinsic = true;
                                }

                                break;
                            }

#if defined(FEATURE_HW_INTRINSICS)
#if defined(TARGET_ARM64)
                            case NI_Vector64_As:
                            case NI_Vector64_AsByte:
                            case NI_Vector64_AsDouble:
                            case NI_Vector64_AsInt16:
                            case NI_Vector64_AsInt32:
                            case NI_Vector64_AsInt64:
                            case NI_Vector64_AsNInt:
                            case NI_Vector64_AsNUInt:
                            case NI_Vector64_AsSByte:
                            case NI_Vector64_AsSingle:
                            case NI_Vector64_AsUInt16:
                            case NI_Vector64_AsUInt32:
                            case NI_Vector64_AsUInt64:
                            case NI_Vector64_op_UnaryPlus:
#endif // TARGET_XARCH
                            case NI_Vector128_As:
                            case NI_Vector128_AsByte:
                            case NI_Vector128_AsDouble:
                            case NI_Vector128_AsInt16:
                            case NI_Vector128_AsInt32:
                            case NI_Vector128_AsInt64:
                            case NI_Vector128_AsNInt:
                            case NI_Vector128_AsNUInt:
                            case NI_Vector128_AsSByte:
                            case NI_Vector128_AsSingle:
                            case NI_Vector128_AsUInt16:
                            case NI_Vector128_AsUInt32:
                            case NI_Vector128_AsUInt64:
                            case NI_Vector128_AsVector4:
                            case NI_Vector128_op_UnaryPlus:
                            case NI_VectorT_As:
                            case NI_VectorT_AsVectorByte:
                            case NI_VectorT_AsVectorDouble:
                            case NI_VectorT_AsVectorInt16:
                            case NI_VectorT_AsVectorInt32:
                            case NI_VectorT_AsVectorInt64:
                            case NI_VectorT_AsVectorNInt:
                            case NI_VectorT_AsVectorNUInt:
                            case NI_VectorT_AsVectorSByte:
                            case NI_VectorT_AsVectorSingle:
                            case NI_VectorT_AsVectorUInt16:
                            case NI_VectorT_AsVectorUInt32:
                            case NI_VectorT_AsVectorUInt64:
                            case NI_VectorT_op_UnaryPlus:
#if defined(TARGET_XARCH)
                            case NI_Vector256_As:
                            case NI_Vector256_AsByte:
                            case NI_Vector256_AsDouble:
                            case NI_Vector256_AsInt16:
                            case NI_Vector256_AsInt32:
                            case NI_Vector256_AsInt64:
                            case NI_Vector256_AsNInt:
                            case NI_Vector256_AsNUInt:
                            case NI_Vector256_AsSByte:
                            case NI_Vector256_AsSingle:
                            case NI_Vector256_AsUInt16:
                            case NI_Vector256_AsUInt32:
                            case NI_Vector256_AsUInt64:
                            case NI_Vector256_op_UnaryPlus:
                            case NI_Vector512_As:
                            case NI_Vector512_AsByte:
                            case NI_Vector512_AsDouble:
                            case NI_Vector512_AsInt16:
                            case NI_Vector512_AsInt32:
                            case NI_Vector512_AsInt64:
                            case NI_Vector512_AsNInt:
                            case NI_Vector512_AsNUInt:
                            case NI_Vector512_AsSByte:
                            case NI_Vector512_AsSingle:
                            case NI_Vector512_AsUInt16:
                            case NI_Vector512_AsUInt32:
                            case NI_Vector512_AsUInt64:
#endif // TARGET_XARCH
#endif // FEATURE_HW_INTRINSICS
                            case NI_SRCS_UNSAFE_As:
                            case NI_SRCS_UNSAFE_AsRef:
                            case NI_SRCS_UNSAFE_BitCast:
                            case NI_SRCS_UNSAFE_SkipInit:
                            {
                                // TODO-CQ: These are no-ops in that they never produce any IR
                                // and simply return op1 untouched. We should really track them
                                // as such and adjust the multiplier even more, but we'll settle
                                // for marking it as foldable until additional work can happen.

                                foldableIntrinsic = true;
                                break;
                            }

#if defined(FEATURE_HW_INTRINSICS)
#if defined(TARGET_ARM64)
                            case NI_Vector64_get_AllBitsSet:
                            case NI_Vector64_get_One:
                            case NI_Vector64_get_Zero:
#endif // TARGET_ARM64
                            case NI_Vector2_get_One:
                            case NI_Vector2_get_Zero:
                            case NI_Vector3_get_One:
                            case NI_Vector3_get_Zero:
                            case NI_Vector4_get_One:
                            case NI_Vector4_get_Zero:
                            case NI_Vector128_get_AllBitsSet:
                            case NI_Vector128_get_One:
                            case NI_Vector128_get_Zero:
                            case NI_VectorT_get_AllBitsSet:
                            case NI_VectorT_get_One:
                            case NI_VectorT_get_Zero:
#if defined(TARGET_XARCH)
                            case NI_Vector256_get_AllBitsSet:
                            case NI_Vector256_get_One:
                            case NI_Vector256_get_Zero:
                            case NI_Vector512_get_AllBitsSet:
                            case NI_Vector512_get_One:
                            case NI_Vector512_get_Zero:
#endif // TARGET_XARCH
#endif // FEATURE_HW_INTRINSICS
                            {
                                // These always produce a vector constant

                                foldableIntrinsic = true;

                                // TODO-CQ: We should really push a constant onto the stack
                                // However, this isn't trivially possible without the inliner
                                // understanding a new type of "vector constant" so it doesn't
                                // negatively impact other possible checks/handling

                                break;
                            }

                            case NI_SRCS_UNSAFE_NullRef:
                            case NI_SRCS_UNSAFE_SizeOf:
                            {
                                // These always produce a constant

                                foldableIntrinsic = true;
                                pushedStack.PushConstant();

                                break;
                            }

                            default:
                            {
                                break;
                            }
                        }
                    }

                    if (foldableIntrinsic)
                    {
                        compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_INTRINSIC);
                        handled = true;
                    }
                    else if (ni != NI_Illegal)
                    {
                        // Otherwise note "intrinsic" (most likely will be lowered as single instructions)
                        // except Math where only a few intrinsics won't end up as normal calls
                        if (!IsMathIntrinsic(ni) || IsTargetIntrinsic(ni))
                        {
                            compInlineResult->Note(InlineObservation::CALLEE_INTRINSIC);
                        }
                    }
                }

                if ((codeAddr < codeEndp - sz) && (OPCODE)getU1LittleEndian(codeAddr + sz) == CEE_RET)
                {
                    // If the method has a call followed by a ret, assume that
                    // it is a wrapper method.
                    compInlineResult->Note(InlineObservation::CALLEE_LOOKS_LIKE_WRAPPER);
                }

                if (!isIntrinsic && !handled && FgStack::IsArgument(pushedStack.Top()))
                {
                    // Optimistically assume that "call(arg)" returns something arg-dependent.
                    // However, we don't know how many args it expects and its return type.
                    handled = true;
                }
            }
            break;

            case CEE_LDIND_I1:
            case CEE_LDIND_U1:
            case CEE_LDIND_I2:
            case CEE_LDIND_U2:
            case CEE_LDIND_I4:
            case CEE_LDIND_U4:
            case CEE_LDIND_I8:
            case CEE_LDIND_I:
            case CEE_LDIND_R4:
            case CEE_LDIND_R8:
            case CEE_LDIND_REF:
            {
                if (FgStack::IsArgument(pushedStack.Top()))
                {
                    handled = true;
                }
                break;
            }

            // Unary operators:
            case CEE_CONV_I:
            case CEE_CONV_U:
            case CEE_CONV_I1:
            case CEE_CONV_I2:
            case CEE_CONV_I4:
            case CEE_CONV_I8:
            case CEE_CONV_R4:
            case CEE_CONV_R8:
            case CEE_CONV_U4:
            case CEE_CONV_U8:
            case CEE_CONV_U2:
            case CEE_CONV_U1:
            case CEE_CONV_R_UN:
            case CEE_CONV_OVF_I:
            case CEE_CONV_OVF_U:
            case CEE_CONV_OVF_I1:
            case CEE_CONV_OVF_U1:
            case CEE_CONV_OVF_I2:
            case CEE_CONV_OVF_U2:
            case CEE_CONV_OVF_I4:
            case CEE_CONV_OVF_U4:
            case CEE_CONV_OVF_I8:
            case CEE_CONV_OVF_U8:
            case CEE_CONV_OVF_I_UN:
            case CEE_CONV_OVF_U_UN:
            case CEE_CONV_OVF_I1_UN:
            case CEE_CONV_OVF_I2_UN:
            case CEE_CONV_OVF_I4_UN:
            case CEE_CONV_OVF_I8_UN:
            case CEE_CONV_OVF_U1_UN:
            case CEE_CONV_OVF_U2_UN:
            case CEE_CONV_OVF_U4_UN:
            case CEE_CONV_OVF_U8_UN:
            case CEE_NOT:
            case CEE_NEG:
            {
                if (makeInlineObservations)
                {
                    FgStack::FgSlot arg = pushedStack.Top();
                    if (FgStack::IsConstArgument(arg, impInlineInfo))
                    {
                        compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_EXPR_UN);
                        handled = true;
                    }
                    else if (FgStack::IsArgument(arg) || FgStack::IsConstant(arg))
                    {
                        handled = true;
                    }
                }
                break;
            }

            // Binary operators:
            case CEE_ADD:
            case CEE_SUB:
            case CEE_MUL:
            case CEE_DIV:
            case CEE_DIV_UN:
            case CEE_REM:
            case CEE_REM_UN:
            case CEE_AND:
            case CEE_OR:
            case CEE_XOR:
            case CEE_SHL:
            case CEE_SHR:
            case CEE_SHR_UN:
            case CEE_ADD_OVF:
            case CEE_ADD_OVF_UN:
            case CEE_MUL_OVF:
            case CEE_MUL_OVF_UN:
            case CEE_SUB_OVF:
            case CEE_SUB_OVF_UN:
            case CEE_CEQ:
            case CEE_CGT:
            case CEE_CGT_UN:
            case CEE_CLT:
            case CEE_CLT_UN:
            {
                if (!makeInlineObservations)
                {
                    break;
                }

                if (!preciseScan)
                {
                    switch (opcode)
                    {
                        case CEE_CEQ:
                        case CEE_CGT:
                        case CEE_CGT_UN:
                        case CEE_CLT:
                        case CEE_CLT_UN:
                            fgObserveInlineConstants(opcode, pushedStack, isInlining);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    FgStack::FgSlot arg0 = pushedStack.Top(1);
                    FgStack::FgSlot arg1 = pushedStack.Top(0);

                    // Const op ConstArg -> ConstArg
                    if (FgStack::IsConstant(arg0) && FgStack::IsConstArgument(arg1, impInlineInfo))
                    {
                        // keep stack unchanged
                        handled = true;
                        compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_EXPR);
                    }
                    // ConstArg op Const    -> ConstArg
                    // ConstArg op ConstArg -> ConstArg
                    else if (FgStack::IsConstArgument(arg0, impInlineInfo) &&
                             FgStack::IsConstantOrConstArg(arg1, impInlineInfo))
                    {
                        if (FgStack::IsConstant(arg1))
                        {
                            pushedStack.Push(arg0);
                        }
                        handled = true;
                        compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_EXPR);
                    }
                    // Const op Const -> Const
                    else if (FgStack::IsConstant(arg0) && FgStack::IsConstant(arg1))
                    {
                        // both are constants, but we're mostly interested in cases where a const arg leads to
                        // a foldable expression.
                        handled = true;
                    }
                    // Arg op ConstArg
                    // Arg op Const
                    else if (FgStack::IsArgument(arg0) && FgStack::IsConstantOrConstArg(arg1, impInlineInfo))
                    {
                        // "Arg op CNS" --> keep arg0 in the stack for the next ops
                        pushedStack.Push(arg0);
                        handled = true;
                        compInlineResult->Note(InlineObservation::CALLEE_BINARY_EXRP_WITH_CNS);
                    }
                    // ConstArg op Arg
                    // Const    op Arg
                    else if (FgStack::IsArgument(arg1) && FgStack::IsConstantOrConstArg(arg0, impInlineInfo))
                    {
                        // "CNS op ARG" --> keep arg1 in the stack for the next ops
                        handled = true;
                        compInlineResult->Note(InlineObservation::CALLEE_BINARY_EXRP_WITH_CNS);
                    }
                    // X / ConstArg
                    // X % ConstArg
                    if (FgStack::IsConstArgument(arg1, impInlineInfo))
                    {
                        if ((opcode == CEE_DIV) || (opcode == CEE_DIV_UN) || (opcode == CEE_REM) ||
                            (opcode == CEE_REM_UN))
                        {
                            compInlineResult->Note(InlineObservation::CALLSITE_DIV_BY_CNS);
                        }
                        pushedStack.Push(arg0);
                        handled = true;
                    }
                }
                break;
            }

            // Jumps
            case CEE_LEAVE:
            case CEE_LEAVE_S:
            case CEE_BR:
            case CEE_BR_S:
            case CEE_BRFALSE:
            case CEE_BRFALSE_S:
            case CEE_BRTRUE:
            case CEE_BRTRUE_S:
            case CEE_BEQ:
            case CEE_BEQ_S:
            case CEE_BGE:
            case CEE_BGE_S:
            case CEE_BGE_UN:
            case CEE_BGE_UN_S:
            case CEE_BGT:
            case CEE_BGT_S:
            case CEE_BGT_UN:
            case CEE_BGT_UN_S:
            case CEE_BLE:
            case CEE_BLE_S:
            case CEE_BLE_UN:
            case CEE_BLE_UN_S:
            case CEE_BLT:
            case CEE_BLT_S:
            case CEE_BLT_UN:
            case CEE_BLT_UN_S:
            case CEE_BNE_UN:
            case CEE_BNE_UN_S:
            {
                if (codeAddr > codeEndp - sz)
                {
                    goto TOO_FAR;
                }

                // Compute jump target address
                signed jmpDist = (sz == 1) ? getI1LittleEndian(codeAddr) : getI4LittleEndian(codeAddr);

                if ((jmpDist == 0) &&
                    (opcode == CEE_LEAVE || opcode == CEE_LEAVE_S || opcode == CEE_BR || opcode == CEE_BR_S) &&
                    opts.DoEarlyBlockMerging())
                {
                    break; /* NOP */
                }

                unsigned jmpAddr = (IL_OFFSET)(codeAddr - codeBegp) + sz + jmpDist;

                // Make sure target is reasonable
                if (jmpAddr >= codeSize)
                {
                    BADCODE3("code jumps to outer space", " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
                }

                if (makeInlineObservations && (jmpDist < 0))
                {
                    compInlineResult->Note(InlineObservation::CALLEE_BACKWARD_JUMP);
                }

                // Mark the jump target
                jumpTarget->bitVectSet(jmpAddr);

                // See if jump might be sensitive to inlining
                if (!preciseScan && makeInlineObservations && (opcode != CEE_BR_S) && (opcode != CEE_BR))
                {
                    fgObserveInlineConstants(opcode, pushedStack, isInlining);
                }
                else if (preciseScan && makeInlineObservations)
                {
                    switch (opcode)
                    {
                        // Binary
                        case CEE_BEQ:
                        case CEE_BGE:
                        case CEE_BGT:
                        case CEE_BLE:
                        case CEE_BLT:
                        case CEE_BNE_UN:
                        case CEE_BGE_UN:
                        case CEE_BGT_UN:
                        case CEE_BLE_UN:
                        case CEE_BLT_UN:
                        case CEE_BEQ_S:
                        case CEE_BGE_S:
                        case CEE_BGT_S:
                        case CEE_BLE_S:
                        case CEE_BLT_S:
                        case CEE_BNE_UN_S:
                        case CEE_BGE_UN_S:
                        case CEE_BGT_UN_S:
                        case CEE_BLE_UN_S:
                        case CEE_BLT_UN_S:
                        {
                            FgStack::FgSlot op1 = pushedStack.Top(1);
                            FgStack::FgSlot op2 = pushedStack.Top(0);

                            if (FgStack::IsConstantOrConstArg(op1, impInlineInfo) &&
                                FgStack::IsConstantOrConstArg(op2, impInlineInfo))
                            {
                                compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_BRANCH);
                            }
                            if (FgStack::IsConstArgument(op1, impInlineInfo) ||
                                FgStack::IsConstArgument(op2, impInlineInfo))
                            {
                                compInlineResult->Note(InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST);
                            }

                            if ((FgStack::IsArgument(op1) && FgStack::IsArrayLen(op2)) ||
                                (FgStack::IsArgument(op2) && FgStack::IsArrayLen(op1)))
                            {
                                compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK);
                            }
                            else if ((FgStack::IsArgument(op1) && FgStack::IsConstantOrConstArg(op2, impInlineInfo)) ||
                                     (FgStack::IsArgument(op2) && FgStack::IsConstantOrConstArg(op1, impInlineInfo)))
                            {
                                compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST);
                            }
                            else if (FgStack::IsArgument(op1) || FgStack::IsArgument(op2))
                            {
                                compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_TEST);
                            }
                            else if (FgStack::IsConstant(op1) || FgStack::IsConstant(op2))
                            {
                                compInlineResult->Note(InlineObservation::CALLEE_BINARY_EXRP_WITH_CNS);
                            }
                            break;
                        }

                        // Unary
                        case CEE_BRFALSE_S:
                        case CEE_BRTRUE_S:
                        case CEE_BRFALSE:
                        case CEE_BRTRUE:
                        {
                            if (FgStack::IsConstantOrConstArg(pushedStack.Top(), impInlineInfo))
                            {
                                compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_BRANCH);
                            }
                            else if (FgStack::IsArgument(pushedStack.Top()))
                            {
                                // E.g. brtrue is basically "if (X == 0)"
                                compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST);
                            }
                            break;
                        }

                        default:
                            break;
                    }
                }
            }
            break;

            case CEE_LDFLDA:
            case CEE_LDFLD:
            case CEE_STFLD:
            {
                if (FgStack::IsArgument(pushedStack.Top()))
                {
                    compInlineResult->Note(InlineObservation::CALLEE_ARG_STRUCT_FIELD_ACCESS);
                    handled = true; // keep argument on top of the stack
                }
                break;
            }

            case CEE_LDELEM_I1:
            case CEE_LDELEM_U1:
            case CEE_LDELEM_I2:
            case CEE_LDELEM_U2:
            case CEE_LDELEM_I4:
            case CEE_LDELEM_U4:
            case CEE_LDELEM_I8:
            case CEE_LDELEM_I:
            case CEE_LDELEM_R4:
            case CEE_LDELEM_R8:
            case CEE_LDELEM_REF:
            case CEE_STELEM_I:
            case CEE_STELEM_I1:
            case CEE_STELEM_I2:
            case CEE_STELEM_I4:
            case CEE_STELEM_I8:
            case CEE_STELEM_R4:
            case CEE_STELEM_R8:
            case CEE_STELEM_REF:
            case CEE_LDELEM:
            case CEE_STELEM:
            {
                if (!preciseScan)
                {
                    break;
                }
                if (FgStack::IsArgument(pushedStack.Top()) || FgStack::IsArgument(pushedStack.Top(1)))
                {
                    compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK);
                }
                break;
            }

            case CEE_SWITCH:
            {
                if (makeInlineObservations)
                {
                    compInlineResult->Note(InlineObservation::CALLEE_HAS_SWITCH);
                    if (FgStack::IsConstantOrConstArg(pushedStack.Top(), impInlineInfo))
                    {
                        compInlineResult->Note(InlineObservation::CALLSITE_FOLDABLE_SWITCH);
                    }

                    // Fail fast, if we're inlining and can't handle this.
                    if (isInlining && compInlineResult->IsFailure())
                    {
                        return;
                    }
                }

                // Make sure we don't go past the end reading the number of cases
                if (codeAddr > codeEndp - sizeof(DWORD))
                {
                    goto TOO_FAR;
                }

                // Read the number of cases
                unsigned jmpCnt = getU4LittleEndian(codeAddr);
                codeAddr += sizeof(DWORD);

                if (jmpCnt > codeSize / sizeof(DWORD))
                {
                    goto TOO_FAR;
                }

                // Find the end of the switch table
                unsigned jmpBase = (unsigned)((codeAddr - codeBegp) + jmpCnt * sizeof(DWORD));

                // Make sure there is more code after the switch
                if (jmpBase >= codeSize)
                {
                    goto TOO_FAR;
                }

                // jmpBase is also the target of the default case, so mark it
                jumpTarget->bitVectSet(jmpBase);

                // Process table entries
                while (jmpCnt > 0)
                {
                    unsigned jmpAddr = jmpBase + getI4LittleEndian(codeAddr);
                    codeAddr += 4;

                    if (jmpAddr >= codeSize)
                    {
                        BADCODE3("jump target out of range", " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
                    }

                    jumpTarget->bitVectSet(jmpAddr);
                    jmpCnt--;
                }

                // We've advanced past all the bytes in this instruction
                sz = 0;
            }
            break;

            case CEE_UNALIGNED:
            {
                noway_assert(sz == sizeof(__int8));
                prefixFlags |= PREFIX_UNALIGNED;

                codeAddr += sizeof(__int8);

                impValidateMemoryAccessOpcode(codeAddr, codeEndp, false);
                handled = true;
                goto OBSERVE_OPCODE;
            }

            case CEE_CONSTRAINED:
            {
                noway_assert(sz == sizeof(unsigned));
                prefixFlags |= PREFIX_CONSTRAINED;

                codeAddr += sizeof(unsigned);

                {
                    OPCODE actualOpcode = impGetNonPrefixOpcode(codeAddr, codeEndp);

                    if (actualOpcode != CEE_CALLVIRT && actualOpcode != CEE_CALL && actualOpcode != CEE_LDFTN)
                    {
                        BADCODE("constrained. has to be followed by callvirt, call or ldftn");
                    }
                }
                handled = true;
                goto OBSERVE_OPCODE;
            }

            case CEE_READONLY:
            {
                noway_assert(sz == 0);
                prefixFlags |= PREFIX_READONLY;

                {
                    OPCODE actualOpcode = impGetNonPrefixOpcode(codeAddr, codeEndp);

                    if ((actualOpcode != CEE_LDELEMA) && !impOpcodeIsCallOpcode(actualOpcode))
                    {
                        BADCODE("readonly. has to be followed by ldelema or call");
                    }
                }
                handled = true;
                goto OBSERVE_OPCODE;
            }

            case CEE_VOLATILE:
            {
                noway_assert(sz == 0);
                prefixFlags |= PREFIX_VOLATILE;

                impValidateMemoryAccessOpcode(codeAddr, codeEndp, true);
                handled = true;
                goto OBSERVE_OPCODE;
            }

            case CEE_TAILCALL:
            {
                noway_assert(sz == 0);
                prefixFlags |= PREFIX_TAILCALL_EXPLICIT;

                {
                    OPCODE actualOpcode = impGetNonPrefixOpcode(codeAddr, codeEndp);

                    if (!impOpcodeIsCallOpcode(actualOpcode))
                    {
                        BADCODE("tailcall. has to be followed by call, callvirt or calli");
                    }
                }
                handled = true;
                goto OBSERVE_OPCODE;
            }

            case CEE_STARG:
            case CEE_STARG_S:
            {
                noway_assert(sz == sizeof(BYTE) || sz == sizeof(WORD));

                if (codeAddr > codeEndp - sz)
                {
                    goto TOO_FAR;
                }

                varNum = (sz == sizeof(BYTE)) ? getU1LittleEndian(codeAddr) : getU2LittleEndian(codeAddr);

                if (isInlining)
                {
                    if (varNum < impInlineInfo->argCnt)
                    {
                        impInlineInfo->inlArgInfo[varNum].argHasStargOp = true;
                    }
                }
                else
                {
                    // account for possible hidden param
                    varNum = compMapILargNum(varNum);

                    // This check is only intended to prevent an AV.  Bad varNum values will later
                    // be handled properly by the verifier.
                    if (varNum < lvaTableCnt)
                    {
                        // In non-inline cases, note written-to arguments.
                        lvaTable[varNum].lvHasILStoreOp = 1;
                    }
                }
            }
            break;

            case CEE_STLOC_0:
            case CEE_STLOC_1:
            case CEE_STLOC_2:
            case CEE_STLOC_3:
                varNum = (opcode - CEE_STLOC_0);
                goto STLOC;

            case CEE_STLOC:
            case CEE_STLOC_S:
            {
                noway_assert(sz == sizeof(BYTE) || sz == sizeof(WORD));

                if (codeAddr > codeEndp - sz)
                {
                    goto TOO_FAR;
                }

                varNum = (sz == sizeof(BYTE)) ? getU1LittleEndian(codeAddr) : getU2LittleEndian(codeAddr);

            STLOC:
                if (isInlining)
                {
                    InlLclVarInfo& lclInfo = impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt];

                    if (lclInfo.lclHasStlocOp)
                    {
                        lclInfo.lclHasMultipleStlocOp = 1;
                    }
                    else
                    {
                        lclInfo.lclHasStlocOp = 1;
                    }
                }
                else
                {
                    varNum += info.compArgsCount;

                    // This check is only intended to prevent an AV.  Bad varNum values will later
                    // be handled properly by the verifier.
                    if (varNum < lvaTableCnt)
                    {
                        // In non-inline cases, note written-to locals.
                        if (lvaTable[varNum].lvHasILStoreOp)
                        {
                            lvaTable[varNum].lvHasMultipleILStoreOp = 1;
                        }
                        else
                        {
                            lvaTable[varNum].lvHasILStoreOp = 1;
                        }
                    }
                }
            }
            break;

            case CEE_LDLOC_0:
            case CEE_LDLOC_1:
            case CEE_LDLOC_2:
            case CEE_LDLOC_3:
                //
                if (preciseScan && makeInlineObservations && (prevOpcode == (CEE_STLOC_3 - (CEE_LDLOC_3 - opcode))))
                {
                    // Fold stloc+ldloc
                    pushedStack.Push(pushedStack.Top(1)); // throw away SLOT_UNKNOWN inserted by STLOC
                    handled = true;
                }
                break;

            case CEE_LDARGA:
            case CEE_LDARGA_S:
            case CEE_LDLOCA:
            case CEE_LDLOCA_S:
            {
                // Handle address-taken args or locals
                noway_assert(sz == sizeof(BYTE) || sz == sizeof(WORD));

                if (codeAddr > codeEndp - sz)
                {
                    goto TOO_FAR;
                }

                varNum = (sz == sizeof(BYTE)) ? getU1LittleEndian(codeAddr) : getU2LittleEndian(codeAddr);

                if (isInlining)
                {
                    if (opcode == CEE_LDLOCA || opcode == CEE_LDLOCA_S)
                    {
                        varType = impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt].lclTypeInfo;

                        impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt].lclHasLdlocaOp = true;
                    }
                    else
                    {
                        noway_assert(opcode == CEE_LDARGA || opcode == CEE_LDARGA_S);

                        varType = impInlineInfo->lclVarInfo[varNum].lclTypeInfo;

                        impInlineInfo->inlArgInfo[varNum].argHasLdargaOp = true;

                        pushedStack.PushArgument(varNum);
                        handled = true;
                    }
                }
                else
                {
                    if (opcode == CEE_LDLOCA || opcode == CEE_LDLOCA_S)
                    {
                        if (varNum >= info.compMethodInfo->locals.numArgs)
                        {
                            BADCODE("bad local number");
                        }

                        varNum += info.compArgsCount;
                    }
                    else
                    {
                        noway_assert(opcode == CEE_LDARGA || opcode == CEE_LDARGA_S);

                        if (varNum >= info.compILargsCount)
                        {
                            BADCODE("bad argument number");
                        }

                        varNum = compMapILargNum(varNum); // account for possible hidden param
                    }

                    varType = (var_types)lvaTable[varNum].lvType;

                    // Determine if the next instruction will consume
                    // the address. If so we won't mark this var as
                    // address taken.
                    //
                    // We will put structs on the stack and changing
                    // the addrTaken of a local requires an extra pass
                    // in the morpher so we won't apply this
                    // optimization to structs.
                    //
                    // Debug code spills for every IL instruction, and
                    // therefore it will split statements, so we will
                    // need the address.  Note that this optimization
                    // is based in that we know what trees we will
                    // generate for this ldfld, and we require that we
                    // won't need the address of this local at all

                    const bool notStruct    = !varTypeIsStruct(lvaGetDesc(varNum));
                    const bool notLastInstr = (codeAddr < codeEndp - sz);
                    const bool notDebugCode = !opts.compDbgCode;

                    if (notStruct && notLastInstr && notDebugCode && impILConsumesAddr(codeAddr + sz))
                    {
                        // We can skip the addrtaken, as next IL instruction consumes
                        // the address.
                    }
                    else
                    {
                        lvaTable[varNum].lvHasLdAddrOp = 1;
                        if (!info.compIsStatic && (varNum == 0))
                        {
                            // Addr taken on "this" pointer is significant,
                            // go ahead to mark it as permanently addr-exposed here.
                            // This may be conservative, but probably not very.
                            lvaSetVarAddrExposed(0 DEBUGARG(AddressExposedReason::TOO_CONSERVATIVE));
                        }
                    }
                } // isInlining

                typeIsNormed = !varTypeIsGC(varType) && !varTypeIsStruct(varType);
            }
            break;

            case CEE_JMP:
                retBlocks++;

#if !defined(TARGET_X86) && !defined(TARGET_ARM)
                if (!isInlining)
                {
                    // We transform this into a set of ldarg's + tail call and
                    // thus may push more onto the stack than originally thought.
                    // This doesn't interfere with verification because CEE_JMP
                    // is never verifiable, and there's nothing unsafe you can
                    // do with a an IL stack overflow if the JIT is expecting it.
                    info.compMaxStack = max(info.compMaxStack, info.compILargsCount);
                    break;
                }
#endif // !TARGET_X86 && !TARGET_ARM

                // If we are inlining, we need to fail for a CEE_JMP opcode, just like
                // the list of other opcodes (for all platforms).

                FALLTHROUGH;

            case CEE_MKREFANY:
            case CEE_RETHROW:
                if (makeInlineObservations)
                {
                    // Arguably this should be NoteFatal, but the legacy behavior is
                    // to ignore this for the prejit root.
                    compInlineResult->Note(InlineObservation::CALLEE_UNSUPPORTED_OPCODE);

                    // Fail fast if we're inlining...
                    if (isInlining)
                    {
                        assert(compInlineResult->IsFailure());
                        return;
                    }
                }
                break;

            case CEE_LOCALLOC:

                compLocallocSeen = true;

                // We now allow localloc callees to become candidates in some cases.
                if (makeInlineObservations)
                {
                    compInlineResult->Note(InlineObservation::CALLEE_HAS_LOCALLOC);
                    if (isInlining && compInlineResult->IsFailure())
                    {
                        return;
                    }
                }
                break;

            case CEE_LDARG_0:
            case CEE_LDARG_1:
            case CEE_LDARG_2:
            case CEE_LDARG_3:
                if (makeInlineObservations)
                {
                    pushedStack.PushArgument(opcode - CEE_LDARG_0);
                    handled = true;
                }
                break;

            case CEE_LDARG_S:
            case CEE_LDARG:
            {
                if (codeAddr > codeEndp - sz)
                {
                    goto TOO_FAR;
                }

                varNum = (sz == sizeof(BYTE)) ? getU1LittleEndian(codeAddr) : getU2LittleEndian(codeAddr);

                if (makeInlineObservations)
                {
                    pushedStack.PushArgument(varNum);
                    handled = true;
                }
            }
            break;

            case CEE_LDLEN:
                if (makeInlineObservations)
                {
                    pushedStack.PushArrayLen();
                    handled = true;
                }
                break;

            case CEE_RET:
                retBlocks++;
                break;

            default:
                break;
        }

        // Skip any remaining operands this opcode may have
        codeAddr += sz;

        // Clear any prefix flags that may have been set
        prefixFlags = 0;

        // Increment the number of observed instructions
        opts.instrCount++;

    OBSERVE_OPCODE:

        // Note the opcode we just saw
        if (makeInlineObservations)
        {
            InlineObservation obs =
                typeIsNormed ? InlineObservation::CALLEE_OPCODE_NORMED : InlineObservation::CALLEE_OPCODE;
            compInlineResult->NoteInt(obs, opcode);
        }

        typeIsNormed = false;
    }

    if (codeAddr != codeEndp)
    {
    TOO_FAR:
        BADCODE3("Code ends in the middle of an opcode, or there is a branch past the end of the method",
                 " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
    }

    INDEBUG(compInlineContext->SetILInstsSet(ilInstsSet));

    if (makeInlineObservations)
    {
        compInlineResult->Note(InlineObservation::CALLEE_END_OPCODE_SCAN);

        // If there are no return blocks we know it does not return, however if there
        // return blocks we don't know it returns as it may be counting unreachable code.
        // However we will still make the CALLEE_DOES_NOT_RETURN observation.

        compInlineResult->NoteBool(InlineObservation::CALLEE_DOES_NOT_RETURN, retBlocks == 0);

        if ((retBlocks == 0) && isInlining &&
            info.compCompHnd->notifyMethodInfoUsage(impInlineInfo->iciCall->gtCallMethHnd))
        {
            // Mark the call node as "no return" as it can impact caller's code quality.
            impInlineInfo->iciCall->gtCallMoreFlags |= GTF_CALL_M_DOES_NOT_RETURN;
            // Mark root method as containing a noreturn call.
            impInlineRoot()->setMethodHasNoReturnCalls();

            // NOTE: we also ask VM whether we're allowed to do so - we don't want to mark a call
            // as "no-return" if its IL may change.
        }

        // If the inline is viable and discretionary, do the
        // profitability screening.
        if (compInlineResult->IsDiscretionaryCandidate())
        {
            // Make some callsite specific observations that will feed
            // into the profitability model.
            impMakeDiscretionaryInlineObservations(impInlineInfo, compInlineResult);

            // None of those observations should have changed the
            // inline's viability.
            assert(compInlineResult->IsCandidate());

            if (isInlining)
            {
                // Assess profitability...
                CORINFO_METHOD_INFO* methodInfo = &impInlineInfo->inlineCandidateInfo->methInfo;
                compInlineResult->DetermineProfitability(methodInfo);

                if (compInlineResult->IsFailure())
                {
                    impInlineRoot()->m_inlineStrategy->NoteUnprofitable();
                    JITDUMP("\n\nInline expansion aborted, inline not profitable\n");
                    return;
                }
                else
                {
                    // The inline is still viable.
                    assert(compInlineResult->IsCandidate());
                }
            }
            else
            {
                // Prejit root case. Profitability assessment for this
                // is done over in compCompileHelper.
            }
        }
    }

    // None of the local vars in the inlinee should have address taken or been written to.
    // Therefore we should NOT need to enter this "if" statement.
    if (!isInlining && !info.compIsStatic)
    {
        fgAdjustForAddressExposedOrWrittenThis();
    }

    // Now that we've seen the IL, set lvSingleDef for root method
    // locals.
    //
    // We could also do this for root method arguments but single-def
    // arguments are set by the caller and so we don't know anything
    // about the possible values or types.
    //
    // For inlinees we do this over in impInlineFetchLocal and
    // impInlineFetchArg (here args are included as we sometimes get
    // new information about the types of inlinee args).
    if (!isInlining)
    {
        const unsigned firstLcl = info.compArgsCount;
        const unsigned lastLcl  = firstLcl + info.compMethodInfo->locals.numArgs;
        for (unsigned lclNum = firstLcl; lclNum < lastLcl; lclNum++)
        {
            LclVarDsc* lclDsc = lvaGetDesc(lclNum);
            assert(lclDsc->lvSingleDef == 0);
            lclDsc->lvSingleDef = !lclDsc->lvHasMultipleILStoreOp && !lclDsc->lvHasLdAddrOp;

            if (lclDsc->lvSingleDef)
            {
                JITDUMP("Marked V%02u as a single def local\n", lclNum);
            }
        }
    }
}

//------------------------------------------------------------------------
// fgAdjustForAddressExposedOrWrittenThis: update var table for cases
//   where the this pointer value can change.
//
// Notes:
//    Modifies lvaArg0Var to refer to a temp if the value of 'this' can
//    change. The original this (info.compThisArg) then remains
//    unmodified in the method.  fgAddInternal is responsible for
//    adding the code to copy the initial this into the temp.

void Compiler::fgAdjustForAddressExposedOrWrittenThis()
{
    LclVarDsc* thisVarDsc = lvaGetDesc(info.compThisArg);

    // Optionally enable adjustment during stress.
    if (compStressCompile(STRESS_GENERIC_VARN, 15))
    {
        JITDUMP("JitStress: creating modifiable `this`\n");
        thisVarDsc->lvHasILStoreOp = true;
    }

    // If this is exposed or written to, create a temp for the modifiable this
    if (thisVarDsc->IsAddressExposed() || thisVarDsc->lvHasILStoreOp)
    {
        // If there is a "ldarga 0" or "starg 0", grab and use the temp.
        lvaArg0Var = lvaGrabTemp(false DEBUGARG("Address-exposed, or written this pointer"));
        noway_assert(lvaArg0Var > (unsigned)info.compThisArg);
        LclVarDsc* arg0varDsc = lvaGetDesc(lvaArg0Var);
        arg0varDsc->lvType    = thisVarDsc->TypeGet();
        arg0varDsc->SetAddressExposed(thisVarDsc->IsAddressExposed() DEBUGARG(thisVarDsc->GetAddrExposedReason()));
        arg0varDsc->lvDoNotEnregister = thisVarDsc->lvDoNotEnregister;
#ifdef DEBUG
        arg0varDsc->SetDoNotEnregReason(thisVarDsc->GetDoNotEnregReason());
#endif
        arg0varDsc->lvHasILStoreOp = thisVarDsc->lvHasILStoreOp;

        // Note that here we don't clear `m_doNotEnregReason` and it stays `doNotEnreg` with `AddrExposed` reason.
        thisVarDsc->CleanAddressExposed();
        thisVarDsc->lvHasILStoreOp = false;
    }
}

//------------------------------------------------------------------------
// fgObserveInlineConstants: look for operations that might get optimized
//   if this method were to be inlined, and report these to the inliner.
//
// Arguments:
//    opcode     -- MSIL opcode under consideration
//    stack      -- abstract stack model at this point in the IL
//    isInlining -- true if we're inlining (vs compiling a prejit root)
//
// Notes:
//    Currently only invoked on compare and branch opcodes.
//
//    If we're inlining we also look at the argument values supplied by
//    the caller at this call site.
//
//    The crude stack model may overestimate stack depth.

void Compiler::fgObserveInlineConstants(OPCODE opcode, const FgStack& stack, bool isInlining)
{
    // We should be able to record inline observations.
    assert(compInlineResult != nullptr);

    // The stack only has to be 1 deep for BRTRUE/FALSE
    bool lookForBranchCases = stack.IsStackAtLeastOneDeep();

    if (lookForBranchCases)
    {
        if (opcode == CEE_BRFALSE || opcode == CEE_BRFALSE_S || opcode == CEE_BRTRUE || opcode == CEE_BRTRUE_S)
        {
            FgStack::FgSlot slot0 = stack.GetSlot0();
            if (FgStack::IsArgument(slot0))
            {
                compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST);

                if (isInlining)
                {
                    // Check for the double whammy of an incoming constant argument
                    // feeding a constant test.
                    unsigned varNum = FgStack::SlotTypeToArgNum(slot0);
                    if (impInlineInfo->inlArgInfo[varNum].argIsInvariant)
                    {
                        compInlineResult->Note(InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST);
                    }
                }
            }

            return;
        }
    }

    // Remaining cases require at least two things on the stack.
    if (!stack.IsStackTwoDeep())
    {
        return;
    }

    FgStack::FgSlot slot0 = stack.GetSlot0();
    FgStack::FgSlot slot1 = stack.GetSlot1();

    // Arg feeds constant test
    if ((FgStack::IsConstant(slot0) && FgStack::IsArgument(slot1)) ||
        (FgStack::IsConstant(slot1) && FgStack::IsArgument(slot0)))
    {
        compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_CONSTANT_TEST);
    }

    // Arg feeds range check
    if ((FgStack::IsArrayLen(slot0) && FgStack::IsArgument(slot1)) ||
        (FgStack::IsArrayLen(slot1) && FgStack::IsArgument(slot0)))
    {
        compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_RANGE_CHECK);
    }

    // Check for an incoming arg that's a constant
    if (isInlining)
    {
        if (FgStack::IsArgument(slot0))
        {
            compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_TEST);

            unsigned varNum = FgStack::SlotTypeToArgNum(slot0);
            if (impInlineInfo->inlArgInfo[varNum].argIsInvariant)
            {
                compInlineResult->Note(InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST);
            }
        }

        if (FgStack::IsArgument(slot1))
        {
            compInlineResult->Note(InlineObservation::CALLEE_ARG_FEEDS_TEST);

            unsigned varNum = FgStack::SlotTypeToArgNum(slot1);
            if (impInlineInfo->inlArgInfo[varNum].argIsInvariant)
            {
                compInlineResult->Note(InlineObservation::CALLSITE_CONSTANT_ARG_FEEDS_TEST);
            }
        }
    }
}

//------------------------------------------------------------------------
// fgMarkBackwardJump: mark blocks indicating there is a jump backwards in
//   IL, from a higher to lower IL offset.
//
// Arguments:
//   targetBlock -- target of the jump
//   sourceBlock -- source of the jump
//
void Compiler::fgMarkBackwardJump(BasicBlock* targetBlock, BasicBlock* sourceBlock)
{
    noway_assert(targetBlock->bbNum <= sourceBlock->bbNum);

    for (BasicBlock* const block : Blocks(targetBlock, sourceBlock))
    {
        if (!block->HasFlag(BBF_BACKWARD_JUMP) && !block->KindIs(BBJ_RETURN))
        {
            block->SetFlags(BBF_BACKWARD_JUMP);
            compHasBackwardJump = true;
        }
    }

    sourceBlock->SetFlags(BBF_BACKWARD_JUMP_SOURCE);
    targetBlock->SetFlags(BBF_BACKWARD_JUMP_TARGET);
}

//------------------------------------------------------------------------
// fgLinkBasicBlocks: set block jump targets and add pred edges
//
// Notes:
//    Pred edges for BBJ_EHFILTERRET are set later by fgFindBasicBlocks.
//    Pred edges for BBJ_EHFINALLYRET are set later by impFixPredLists,
//     after setting up the callfinally blocks.
//
void Compiler::fgLinkBasicBlocks()
{
    // Create the basic block lookup tables
    //
    fgInitBBLookup();

#ifdef DEBUG
    // Verify blocks are in increasing bbNum order and
    // all pred list info is in initial state.
    //
    fgDebugCheckBBNumIncreasing();

    for (BasicBlock* const block : Blocks())
    {
        assert(block->bbPreds == nullptr);
        assert(block->bbLastPred == nullptr);
        assert(block->bbRefs == 0);
    }
#endif

    // First block is always reachable
    //
    fgFirstBB->bbRefs = 1;

    // Special arg to fgAddRefPred so it will use the initialization fast path.
    //
    const bool initializingPreds = true;

    for (BasicBlock* const curBBdesc : Blocks())
    {
        switch (curBBdesc->GetKind())
        {
            case BBJ_COND:
            {
                BasicBlock* const trueTarget  = fgLookupBB(curBBdesc->GetTargetOffs());
                BasicBlock* const falseTarget = curBBdesc->Next();
                FlowEdge* const trueEdge = fgAddRefPred<initializingPreds>(trueTarget, curBBdesc);
                FlowEdge* const falseEdge = fgAddRefPred<initializingPreds>(falseTarget, curBBdesc);
                curBBdesc->SetTrueEdge(trueEdge);
                curBBdesc->SetFalseEdge(falseEdge);

                if (trueTarget->bbNum <= curBBdesc->bbNum)
                {
                    fgMarkBackwardJump(trueTarget, curBBdesc);
                }

                if (curBBdesc->IsLast())
                {
                    BADCODE("Fall thru the end of a method");
                }

                break;
            }
            case BBJ_ALWAYS:
            case BBJ_LEAVE:
            {
                // Avoid fgLookupBB overhead for blocks that jump to next block
                // (curBBdesc cannot be the last block if it jumps to the next block)
                const bool jumpsToNext = (curBBdesc->GetTargetOffs() == curBBdesc->bbCodeOffsEnd);
                assert(!(curBBdesc->IsLast() && jumpsToNext));
                BasicBlock* const jumpDest = jumpsToNext ? curBBdesc->Next() : fgLookupBB(curBBdesc->GetTargetOffs());

                // Redundantly use SetKindAndTargetEdge() instead of SetTargetEdge() just this once,
                // so we don't break the HasInitializedTarget() invariant of SetTargetEdge().
                FlowEdge* const newEdge = fgAddRefPred<initializingPreds>(jumpDest, curBBdesc);
                curBBdesc->SetKindAndTargetEdge(curBBdesc->GetKind(), newEdge);

                if (curBBdesc->GetTarget()->bbNum <= curBBdesc->bbNum)
                {
                    fgMarkBackwardJump(curBBdesc->GetTarget(), curBBdesc);
                }
                break;
            }

            case BBJ_EHFILTERRET:
                // We can't set up the pred list for these just yet.
                // We do it in fgFindBasicBlocks.
                break;

            case BBJ_EHFINALLYRET:
                // We can't set up the pred list for these just yet.
                // We do it in impFixPredLists.
                break;

            case BBJ_EHFAULTRET:
            case BBJ_THROW:
            case BBJ_RETURN:
                break;

            case BBJ_SWITCH:
            {
                unsigned   jumpCnt = curBBdesc->GetSwitchTargets()->bbsCount;
                FlowEdge** jumpPtr = curBBdesc->GetSwitchTargets()->bbsDstTab;

                do
                {
                    BasicBlock*     jumpDest = fgLookupBB((unsigned)*(size_t*)jumpPtr);
                    FlowEdge* const newEdge  = fgAddRefPred<initializingPreds>(jumpDest, curBBdesc);
                    *jumpPtr                 = newEdge;
                    if (jumpDest->bbNum <= curBBdesc->bbNum)
                    {
                        fgMarkBackwardJump(jumpDest, curBBdesc);
                    }
                } while (++jumpPtr, --jumpCnt);

                /* Default case of CEE_SWITCH (next block), is at end of jumpTab[] */

                noway_assert(curBBdesc->NextIs((*(jumpPtr - 1))->getDestinationBlock()));
                break;
            }

            case BBJ_CALLFINALLY: // BBJ_CALLFINALLY and BBJ_EHCATCHRET don't appear until later
            case BBJ_EHCATCHRET:
            default:
                noway_assert(!"Unexpected bbKind");
                break;
        }
    }

    // If this is an OSR compile, note the original entry and
    // the OSR entry block.
    //
    // We don't yet alter flow; see fgFixEntryFlowForOSR.
    //
    if (opts.IsOSR())
    {
        assert(info.compILEntry >= 0);
        fgEntryBB    = fgLookupBB(0);
        fgOSREntryBB = fgLookupBB(info.compILEntry);
    }

    // Pred lists now established.
    //
    fgPredsComputed = true;
}

//------------------------------------------------------------------------
// fgMakeBasicBlocks: walk the IL creating basic blocks, and look for
//   operations that might get optimized if this method were to be inlined.
//
// Arguments:
//   codeAddr -- starting address of the method's IL stream
//   codeSize -- length of the IL stream
//   jumpTarget -- [in] bit vector of jump targets found by fgFindJumpTargets
//
// Returns:
//   number of return blocks (BBJ_RETURN) in the method (may be zero)
//
// Notes:
//   Invoked for prejited and jitted methods, and for all inlinees

unsigned Compiler::fgMakeBasicBlocks(const BYTE* codeAddr, IL_OFFSET codeSize, FixedBitVect* jumpTarget)
{
    unsigned    retBlocks = 0;
    const BYTE* codeBegp  = codeAddr;
    const BYTE* codeEndp  = codeAddr + codeSize;
    bool        tailCall  = false;
    unsigned    curBBoffs = 0;
    BasicBlock* curBBdesc;

    // Keep track of where we are in the scope lists, as we will also
    // create blocks at scope boundaries.
    if (opts.compDbgCode && (info.compVarScopesCount > 0))
    {
        compResetScopeLists();

        // Ignore scopes beginning at offset 0
        while (compGetNextEnterScope(0))
        { /* do nothing */
        }
        while (compGetNextExitScope(0))
        { /* do nothing */
        }
    }

    do
    {
        unsigned        jmpAddr = DUMMY_INIT(BAD_IL_OFFSET);
        BasicBlockFlags bbFlags = BBF_EMPTY;
        BBswtDesc*      swtDsc  = nullptr;
        unsigned        nxtBBoffs;
        OPCODE          opcode = (OPCODE)getU1LittleEndian(codeAddr);
        codeAddr += sizeof(__int8);
        BBKinds jmpKind = BBJ_COUNT;

    DECODE_OPCODE:

        /* Get the size of additional parameters */

        noway_assert((unsigned)opcode < CEE_COUNT);

        unsigned sz = opcodeSizes[opcode];

        switch (opcode)
        {
            signed jmpDist;

            case CEE_PREFIX1:
                if (jumpTarget->bitVectTest((UINT)(codeAddr - codeBegp)))
                {
                    BADCODE3("jump target between prefix 0xFE and opcode", " at offset %04X",
                             (IL_OFFSET)(codeAddr - codeBegp));
                }

                opcode = (OPCODE)(256 + getU1LittleEndian(codeAddr));
                codeAddr += sizeof(__int8);
                goto DECODE_OPCODE;

            /* Check to see if we have a jump/return opcode */

            case CEE_BRFALSE:
            case CEE_BRFALSE_S:
            case CEE_BRTRUE:
            case CEE_BRTRUE_S:

            case CEE_BEQ:
            case CEE_BEQ_S:
            case CEE_BGE:
            case CEE_BGE_S:
            case CEE_BGE_UN:
            case CEE_BGE_UN_S:
            case CEE_BGT:
            case CEE_BGT_S:
            case CEE_BGT_UN:
            case CEE_BGT_UN_S:
            case CEE_BLE:
            case CEE_BLE_S:
            case CEE_BLE_UN:
            case CEE_BLE_UN_S:
            case CEE_BLT:
            case CEE_BLT_S:
            case CEE_BLT_UN:
            case CEE_BLT_UN_S:
            case CEE_BNE_UN:
            case CEE_BNE_UN_S:

                jmpKind = BBJ_COND;
                goto JMP;

            case CEE_LEAVE:
            case CEE_LEAVE_S:

                // We need to check if we are jumping out of a finally-protected try.
                jmpKind = BBJ_LEAVE;
                goto JMP;

            case CEE_BR:
            case CEE_BR_S:
                jmpKind = BBJ_ALWAYS;
                goto JMP;

            JMP:

                /* Compute the target address of the jump */

                jmpDist = (sz == 1) ? getI1LittleEndian(codeAddr) : getI4LittleEndian(codeAddr);

                if ((jmpDist == 0) && (opcode == CEE_BR || opcode == CEE_BR_S) && opts.DoEarlyBlockMerging())
                {
                    continue; /* NOP */
                }

                jmpAddr = (IL_OFFSET)(codeAddr - codeBegp) + sz + jmpDist;
                break;

            case CEE_SWITCH:
            {
                unsigned jmpBase;
                unsigned jmpCnt; // # of switch cases (excluding default)

                FlowEdge** jmpTab;
                FlowEdge** jmpPtr;

                /* Allocate the switch descriptor */

                swtDsc = new (this, CMK_BasicBlock) BBswtDesc;

                /* Read the number of entries in the table */

                jmpCnt = getU4LittleEndian(codeAddr);
                codeAddr += 4;

                /* Compute  the base offset for the opcode */

                jmpBase = (IL_OFFSET)((codeAddr - codeBegp) + jmpCnt * sizeof(DWORD));

                /* Allocate the jump table */

                jmpPtr = jmpTab = new (this, CMK_FlowEdge) FlowEdge*[jmpCnt + 1];

                /* Fill in the jump table */

                for (unsigned count = jmpCnt; count; count--)
                {
                    jmpDist = getI4LittleEndian(codeAddr);
                    codeAddr += 4;

                    // store the offset in the pointer.  We change these in fgLinkBasicBlocks().
                    *jmpPtr++ = (FlowEdge*)(size_t)(jmpBase + jmpDist);
                }

                /* Append the default label to the target table */

                *jmpPtr++ = (FlowEdge*)(size_t)jmpBase;

                /* Make sure we found the right number of labels */

                noway_assert(jmpPtr == jmpTab + jmpCnt + 1);

                /* Compute the size of the switch opcode operands */

                sz = sizeof(DWORD) + jmpCnt * sizeof(DWORD);

                /* Fill in the remaining fields of the switch descriptor */

                swtDsc->bbsCount  = jmpCnt + 1;
                swtDsc->bbsDstTab = jmpTab;

                /* This is definitely a jump */

                jmpKind     = BBJ_SWITCH;
                fgHasSwitch = true;

                if (opts.compProcedureSplitting)
                {
                    // TODO-CQ: We might need to create a switch table; we won't know for sure until much later.
                    // However, switch tables don't work with hot/cold splitting, currently. The switch table data needs
                    // a relocation such that if the base (the first block after the prolog) and target of the switch
                    // branch are put in different sections, the difference stored in the table is updated. However, our
                    // relocation implementation doesn't support three different pointers (relocation address, base, and
                    // target). So, we need to change our switch table implementation to be more like
                    // JIT64: put the table in the code section, in the same hot/cold section as the switch jump itself
                    // (maybe immediately after the switch jump), and make the "base" address be also in that section,
                    // probably the address after the switch jump.
                    opts.compProcedureSplitting = false;
                    JITDUMP("Turning off procedure splitting for this method, as it might need switch tables; "
                            "implementation limitation.\n");
                }
            }
                goto GOT_ENDP;

            case CEE_ENDFILTER:
                bbFlags |= BBF_DONT_REMOVE;
                jmpKind = BBJ_EHFILTERRET;
                break;

            case CEE_ENDFINALLY:
                // Start with BBJ_EHFINALLYRET; change to BBJ_EHFAULTRET later if it's in a 'fault' clause.
                jmpKind = BBJ_EHFINALLYRET;
                break;

            case CEE_TAILCALL:
                if (compIsForInlining())
                {
                    // TODO-CQ: We can inline some callees with explicit tail calls if we can guarantee that the calls
                    // can be dispatched as tail calls from the caller.
                    compInlineResult->NoteFatal(InlineObservation::CALLEE_EXPLICIT_TAIL_PREFIX);
                    retBlocks++;
                    return retBlocks;
                }

                FALLTHROUGH;

            case CEE_READONLY:
            case CEE_CONSTRAINED:
            case CEE_VOLATILE:
            case CEE_UNALIGNED:
                // fgFindJumpTargets should have ruled out this possibility
                //   (i.e. a prefix opcodes as last instruction in a block)
                noway_assert(codeAddr < codeEndp);

                if (jumpTarget->bitVectTest((UINT)(codeAddr - codeBegp)))
                {
                    BADCODE3("jump target between prefix and an opcode", " at offset %04X",
                             (IL_OFFSET)(codeAddr - codeBegp));
                }
                break;

            case CEE_CALL:
            case CEE_CALLVIRT:
            case CEE_CALLI:
            {
                if (compIsForInlining() ||               // Ignore tail call in the inlinee. Period.
                    (!tailCall && !compTailCallStress()) // A new BB with BBJ_RETURN would have been created

                    // after a tailcall statement.
                    // We need to keep this invariant if we want to stress the tailcall.
                    // That way, the potential (tail)call statement is always the last
                    // statement in the block.
                    // Otherwise, we will assert at the following line in fgMorphCall()
                    //     noway_assert(fgMorphStmt->GetNextStmt() == NULL);
                    )
                {
                    // Neither .tailcall prefix, no tailcall stress. So move on.
                    break;
                }

                // Make sure the code sequence is legal for the tail call.
                // If so, mark this BB as having a BBJ_RETURN.

                if (codeAddr >= codeEndp - sz)
                {
                    BADCODE3("No code found after the call instruction", " at offset %04X",
                             (IL_OFFSET)(codeAddr - codeBegp));
                }

                if (tailCall)
                {
                    // impIsTailCallILPattern uses isRecursive flag to determine whether ret in a fallthrough block is
                    // allowed. We don't know at this point whether the call is recursive so we conservatively pass
                    // false. This will only affect explicit tail calls when IL verification is not needed for the
                    // method.
                    bool isRecursive = false;
                    if (!impIsTailCallILPattern(tailCall, opcode, codeAddr + sz, codeEndp, isRecursive))
                    {
                        BADCODE3("tail call not followed by ret", " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
                    }

                    if (fgMayExplicitTailCall())
                    {
                        compTailPrefixSeen = true;
                    }
                }
                else
                {
                    OPCODE nextOpcode = (OPCODE)getU1LittleEndian(codeAddr + sz);

                    if (nextOpcode != CEE_RET)
                    {
                        noway_assert(compTailCallStress());
                        // Next OPCODE is not a CEE_RET, bail the attempt to stress the tailcall.
                        // (I.e. We will not make a new BB after the "call" statement.)
                        break;
                    }
                }
            }

                /* For tail call, we just call CORINFO_HELP_TAILCALL, and it jumps to the
                   target. So we don't need an epilog - just like CORINFO_HELP_THROW.
                   Make the block BBJ_RETURN, but we will change it to BBJ_THROW
                   if the tailness of the call is satisfied.
                   NOTE : The next instruction is guaranteed to be a CEE_RET
                   and it will create another BasicBlock. But there may be an
                   jump directly to that CEE_RET. If we want to avoid creating
                   an unnecessary block, we need to check if the CEE_RETURN is
                   the target of a jump.
                 */

                FALLTHROUGH;

            case CEE_JMP:
            /* These are equivalent to a return from the current method
               But instead of directly returning to the caller we jump and
               execute something else in between */
            case CEE_RET:
                retBlocks++;
                jmpKind = BBJ_RETURN;
                break;

            case CEE_THROW:
            case CEE_RETHROW:
                jmpKind = BBJ_THROW;
                break;

#ifdef DEBUG
// make certain we did not forget any flow of control instructions
// by checking the 'ctrl' field in opcode.def. First filter out all
// non-ctrl instructions
#define BREAK(name)                                                                                                    \
    case name:                                                                                                         \
        break;
#define NEXT(name)                                                                                                     \
    case name:                                                                                                         \
        break;
#define CALL(name)
#define THROW(name)
#undef RETURN // undef contract RETURN macro
#define RETURN(name)
#define META(name)
#define BRANCH(name)
#define COND_BRANCH(name)
#define PHI(name)

#define OPDEF(name, string, pop, push, oprType, opcType, l, s1, s2, ctrl) ctrl(name)
#include "opcode.def"
#undef OPDEF

#undef PHI
#undef BREAK
#undef CALL
#undef NEXT
#undef THROW
#undef RETURN
#undef META
#undef BRANCH
#undef COND_BRANCH

            // These ctrl-flow opcodes don't need any special handling
            case CEE_NEWOBJ: // CTRL_CALL
                break;

            // what's left are forgotten instructions
            default:
                BADCODE("Unrecognized control Opcode");
                break;
#else  // !DEBUG
            default:
                break;
#endif // !DEBUG
        }

        /* Jump over the operand */

        codeAddr += sz;

    GOT_ENDP:

        tailCall = (opcode == CEE_TAILCALL);

        /* Make sure a jump target isn't in the middle of our opcode */

        if (sz)
        {
            IL_OFFSET offs = (IL_OFFSET)(codeAddr - codeBegp) - sz; // offset of the operand

            for (unsigned i = 0; i < sz; i++, offs++)
            {
                if (jumpTarget->bitVectTest(offs))
                {
                    BADCODE3("jump into the middle of an opcode", " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
                }
            }
        }

        /* Compute the offset of the next opcode */

        nxtBBoffs = (IL_OFFSET)(codeAddr - codeBegp);

        bool foundScope = false;

        if (opts.compDbgCode && (info.compVarScopesCount > 0))
        {
            while (compGetNextEnterScope(nxtBBoffs))
            {
                foundScope = true;
            }
            while (compGetNextExitScope(nxtBBoffs))
            {
                foundScope = true;
            }
        }

        /* Do we have a jump? */

        if (jmpKind == BBJ_COUNT)
        {
            /* No jump; make sure we don't fall off the end of the function */

            if (codeAddr == codeEndp)
            {
                BADCODE3("missing return opcode", " at offset %04X", (IL_OFFSET)(codeAddr - codeBegp));
            }

            /* If a label follows this opcode, we'll have to make a new BB */

            bool makeBlock = jumpTarget->bitVectTest(nxtBBoffs);

            if (!makeBlock && foundScope)
            {
                makeBlock = true;
#ifdef DEBUG
                if (verbose)
                {
                    printf("Splitting at BBoffs = %04u\n", nxtBBoffs);
                }
#endif // DEBUG
            }

            if (!makeBlock)
            {
                continue;
            }

            // Jump to the next block
            jmpKind = BBJ_ALWAYS;
            jmpAddr = nxtBBoffs;
            bbFlags |= BBF_NONE_QUIRK;
        }

        assert(jmpKind != BBJ_COUNT);

        /* We need to create a new basic block */

        switch (jmpKind)
        {
            case BBJ_SWITCH:
                curBBdesc = BasicBlock::New(this, swtDsc);
                break;

            case BBJ_COND:
            case BBJ_ALWAYS:
            case BBJ_LEAVE:
                noway_assert(jmpAddr != DUMMY_INIT(BAD_IL_OFFSET));
                curBBdesc = BasicBlock::New(this, jmpKind, jmpAddr);
                break;

            default:
                curBBdesc = BasicBlock::New(this, jmpKind);
                break;
        }

        curBBdesc->SetFlags(bbFlags);
        curBBdesc->bbRefs = 0;

        curBBdesc->bbCodeOffs    = curBBoffs;
        curBBdesc->bbCodeOffsEnd = nxtBBoffs;

        /* Append the block to the end of the global basic block list */

        if (fgFirstBB)
        {
            fgLastBB->SetNext(curBBdesc);
        }
        else
        {
            fgFirstBB = curBBdesc;
            assert(fgFirstBB->IsFirst());
        }

        fgLastBB = curBBdesc;

        DBEXEC(verbose, curBBdesc->dspBlockHeader(this, false, false, false));

        /* Remember where the next BB will start */

        curBBoffs = nxtBBoffs;
    } while (codeAddr < codeEndp);

    noway_assert(codeAddr == codeEndp);

    /* Finally link up the bbTarget of the blocks together */

    fgLinkBasicBlocks();

    return retBlocks;
}

/*****************************************************************************
 *
 *  Main entry point to discover the basic blocks for the current function.
 */

void Compiler::fgFindBasicBlocks()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgFindBasicBlocks() for %s\n", info.compFullName);
    }

    // Call this here so any dump printing it inspires doesn't appear in the bb table.
    //
    fgStressBBProf();
#endif

    // Allocate the 'jump target' bit vector
    FixedBitVect* jumpTarget = FixedBitVect::bitVectInit(info.compILCodeSize + 1, this);

    // Walk the instrs to find all jump targets
    fgFindJumpTargets(info.compCode, info.compILCodeSize, jumpTarget);
    if (compDonotInline())
    {
        return;
    }

    unsigned XTnum;

    /* Are there any exception handlers? */

    if (info.compXcptnsCount > 0)
    {
        noway_assert(!compIsForInlining());

        /* Check and mark all the exception handlers */

        for (XTnum = 0; XTnum < info.compXcptnsCount; XTnum++)
        {
            CORINFO_EH_CLAUSE clause;
            info.compCompHnd->getEHinfo(info.compMethodHnd, XTnum, &clause);
            noway_assert(clause.HandlerLength != (unsigned)-1);

            if (clause.TryLength <= 0)
            {
                BADCODE("try block length <=0");
            }

            /* Mark the 'try' block extent and the handler itself */

            if (clause.TryOffset > info.compILCodeSize)
            {
                BADCODE("try offset is > codesize");
            }
            jumpTarget->bitVectSet(clause.TryOffset);

            if (clause.TryOffset + clause.TryLength > info.compILCodeSize)
            {
                BADCODE("try end is > codesize");
            }
            jumpTarget->bitVectSet(clause.TryOffset + clause.TryLength);

            if (clause.HandlerOffset > info.compILCodeSize)
            {
                BADCODE("handler offset > codesize");
            }
            jumpTarget->bitVectSet(clause.HandlerOffset);

            if (clause.HandlerOffset + clause.HandlerLength > info.compILCodeSize)
            {
                BADCODE("handler end > codesize");
            }
            jumpTarget->bitVectSet(clause.HandlerOffset + clause.HandlerLength);

            if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
            {
                if (clause.FilterOffset > info.compILCodeSize)
                {
                    BADCODE("filter offset > codesize");
                }
                jumpTarget->bitVectSet(clause.FilterOffset);
            }
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        bool anyJumpTargets = false;
        printf("Jump targets:\n");
        for (unsigned i = 0; i < info.compILCodeSize + 1; i++)
        {
            if (jumpTarget->bitVectTest(i))
            {
                anyJumpTargets = true;
                printf("  IL_%04x\n", i);
            }
        }

        if (!anyJumpTargets)
        {
            printf("  none\n");
        }
    }
#endif // DEBUG

    /* Now create the basic blocks */

    fgReturnCount = fgMakeBasicBlocks(info.compCode, info.compILCodeSize, jumpTarget);

    if (compIsForInlining())
    {
        if (compInlineResult->IsFailure())
        {
            return;
        }

        noway_assert(info.compXcptnsCount == 0);
        compHndBBtab = impInlineInfo->InlinerCompiler->compHndBBtab;
        compHndBBtabAllocCount =
            impInlineInfo->InlinerCompiler->compHndBBtabAllocCount; // we probably only use the table, not add to it.
        compHndBBtabCount    = impInlineInfo->InlinerCompiler->compHndBBtabCount;
        info.compXcptnsCount = impInlineInfo->InlinerCompiler->info.compXcptnsCount;

        // Use a spill temp for the return value if there are multiple return blocks,
        // or if the inlinee has GC ref locals.
        if ((info.compRetNativeType != TYP_VOID) && ((fgReturnCount > 1) || impInlineInfo->HasGcRefLocals()))
        {
            // If we've spilled the ret expr to a temp we can reuse the temp
            // as the inlinee return spill temp.
            //
            // Todo: see if it is even better to always use this existing temp
            // for return values, even if we otherwise wouldn't need a return spill temp...
            lvaInlineeReturnSpillTemp = impInlineInfo->inlineCandidateInfo->preexistingSpillTemp;

            if (lvaInlineeReturnSpillTemp != BAD_VAR_NUM)
            {
                // This temp should already have the type of the return value.
                JITDUMP("\nInliner: re-using pre-existing spill temp V%02u\n", lvaInlineeReturnSpillTemp);

                // We may have co-opted an existing temp for the return spill.
                // We likely assumed it was single-def at the time, but now
                // we can see it has multiple definitions.
                if ((fgReturnCount > 1) && (lvaTable[lvaInlineeReturnSpillTemp].lvSingleDef == 1))
                {
                    // Make sure it is no longer marked single def. This is only safe
                    // to do if we haven't ever updated the type.
                    if (info.compRetType == TYP_REF)
                    {
                        assert(!lvaTable[lvaInlineeReturnSpillTemp].lvClassInfoUpdated);
                    }

                    JITDUMP("Marked return spill temp V%02u as NOT single def temp\n", lvaInlineeReturnSpillTemp);
                    lvaTable[lvaInlineeReturnSpillTemp].lvSingleDef = 0;
                }
            }
            else
            {
                // The lifetime of this var might expand multiple BBs. So it is a long lifetime compiler temp.
                lvaInlineeReturnSpillTemp = lvaGrabTemp(false DEBUGARG("Inline return value spill temp"));
                lvaTable[lvaInlineeReturnSpillTemp].lvType = info.compRetType;
                if (varTypeIsStruct(info.compRetType))
                {
                    lvaSetStruct(lvaInlineeReturnSpillTemp, info.compMethodInfo->args.retTypeClass, false);
                }

                // The return spill temp is single def only if the method has a single return block.
                if (fgReturnCount == 1)
                {
                    lvaTable[lvaInlineeReturnSpillTemp].lvSingleDef = 1;
                    JITDUMP("Marked return spill temp V%02u as a single def temp\n", lvaInlineeReturnSpillTemp);
                }

                // If the method returns a ref class, set the class of the spill temp
                // to the method's return value. We may update this later if it turns
                // out we can prove the method returns a more specific type.
                if (info.compRetType == TYP_REF)
                {
                    CORINFO_CLASS_HANDLE retClassHnd = impInlineInfo->inlineCandidateInfo->methInfo.args.retTypeClass;
                    if (retClassHnd != nullptr)
                    {
                        lvaSetClass(lvaInlineeReturnSpillTemp, retClassHnd);
                    }
                }
            }
        }

        return;
    }

    /* Mark all blocks within 'try' blocks as such */

    if (info.compXcptnsCount == 0)
    {
        return;
    }

    if (info.compXcptnsCount > MAX_XCPTN_INDEX)
    {
        IMPL_LIMITATION("too many exception clauses");
    }

    /* Allocate the exception handler table */

    fgAllocEHTable();

    /* Assume we don't need to sort the EH table (such that nested try/catch
     * appear before their try or handler parent). The EH verifier will notice
     * when we do need to sort it.
     */

    fgNeedToSortEHTable = false;

    verInitEHTree(info.compXcptnsCount);
    EHNodeDsc* initRoot = ehnNext; // remember the original root since
                                   // it may get modified during insertion

    // Annotate BBs with exception handling information required for generating correct eh code
    // as well as checking for correct IL

    EHblkDsc* HBtab;

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        CORINFO_EH_CLAUSE clause;
        info.compCompHnd->getEHinfo(info.compMethodHnd, XTnum, &clause);
        noway_assert(clause.HandlerLength != (unsigned)-1); // @DEPRECATED

#ifdef DEBUG
        if (verbose)
        {
            dispIncomingEHClause(XTnum, clause);
        }
#endif // DEBUG

        IL_OFFSET tryBegOff    = clause.TryOffset;
        IL_OFFSET tryEndOff    = tryBegOff + clause.TryLength;
        IL_OFFSET filterBegOff = 0;
        IL_OFFSET hndBegOff    = clause.HandlerOffset;
        IL_OFFSET hndEndOff    = hndBegOff + clause.HandlerLength;

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filterBegOff = clause.FilterOffset;
        }

        if (tryEndOff > info.compILCodeSize)
        {
            BADCODE3("end of try block beyond end of method for try", " at offset %04X", tryBegOff);
        }
        if (hndEndOff > info.compILCodeSize)
        {
            BADCODE3("end of hnd block beyond end of method for try", " at offset %04X", tryBegOff);
        }

        HBtab->ebdTryBegOffset    = tryBegOff;
        HBtab->ebdTryEndOffset    = tryEndOff;
        HBtab->ebdFilterBegOffset = filterBegOff;
        HBtab->ebdHndBegOffset    = hndBegOff;
        HBtab->ebdHndEndOffset    = hndEndOff;

        /* Convert the various addresses to basic blocks */

        BasicBlock* tryBegBB = fgLookupBB(tryBegOff);
        BasicBlock* tryEndBB =
            fgLookupBB(tryEndOff); // note: this can be NULL if the try region is at the end of the function
        BasicBlock* hndBegBB = fgLookupBB(hndBegOff);
        BasicBlock* hndEndBB = nullptr;
        BasicBlock* filtBB   = nullptr;
        BasicBlock* block;

        //
        // Assert that the try/hnd beginning blocks are set up correctly
        //
        if (tryBegBB == nullptr)
        {
            BADCODE("Try Clause is invalid");
        }

        if (hndBegBB == nullptr)
        {
            BADCODE("Handler Clause is invalid");
        }

        if (hndEndOff < info.compILCodeSize)
        {
            hndEndBB = fgLookupBB(hndEndOff);
        }

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filtBB = HBtab->ebdFilter = fgLookupBB(clause.FilterOffset);
            filtBB->bbCatchTyp        = BBCT_FILTER;
            hndBegBB->bbCatchTyp      = BBCT_FILTER_HANDLER;

            // Mark all BBs that belong to the filter with the XTnum of the corresponding handler
            for (block = filtBB; /**/; block = block->Next())
            {
                if (block == nullptr)
                {
                    BADCODE3("Missing endfilter for filter", " at offset %04X", filtBB->bbCodeOffs);
                    return;
                }

                // Still inside the filter
                block->setHndIndex(XTnum);

                if (block->KindIs(BBJ_EHFILTERRET))
                {
                    // Mark catch handler as successor.
                    FlowEdge* const newEdge = fgAddRefPred(hndBegBB, block);
                    newEdge->setLikelihood(1.0);
                    block->SetTargetEdge(newEdge);
                    assert(hndBegBB->bbCatchTyp == BBCT_FILTER_HANDLER);
                    break;
                }
            }

            if (block->IsLast() || !block->NextIs(hndBegBB))
            {
                BADCODE3("Filter does not immediately precede handler for filter", " at offset %04X",
                         filtBB->bbCodeOffs);
            }
        }
        else
        {
            HBtab->ebdTyp = clause.ClassToken;

            /* Set bbCatchTyp as appropriate */

            if (clause.Flags & CORINFO_EH_CLAUSE_FINALLY)
            {
                hndBegBB->bbCatchTyp = BBCT_FINALLY;
            }
            else
            {
                if (clause.Flags & CORINFO_EH_CLAUSE_FAULT)
                {
                    hndBegBB->bbCatchTyp = BBCT_FAULT;
                }
                else
                {
                    hndBegBB->bbCatchTyp = clause.ClassToken;

                    // These values should be non-zero value that will
                    // not collide with real tokens for bbCatchTyp
                    if (clause.ClassToken == 0)
                    {
                        BADCODE("Exception catch type is Null");
                    }

                    noway_assert(clause.ClassToken != BBCT_FAULT);
                    noway_assert(clause.ClassToken != BBCT_FINALLY);
                    noway_assert(clause.ClassToken != BBCT_FILTER);
                    noway_assert(clause.ClassToken != BBCT_FILTER_HANDLER);
                }
            }
        }

        /*  Prevent future optimizations of removing the first block   */
        /*  of a TRY block and the first block of an exception handler */

        tryBegBB->SetFlags(BBF_DONT_REMOVE);
        hndBegBB->SetFlags(BBF_DONT_REMOVE);
        hndBegBB->bbRefs++; // The first block of a handler gets an extra, "artificial" reference count.

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filtBB->SetFlags(BBF_DONT_REMOVE);
            filtBB->bbRefs++; // The first block of a filter gets an extra, "artificial" reference count.
        }

        tryBegBB->SetFlags(BBF_DONT_REMOVE);
        hndBegBB->SetFlags(BBF_DONT_REMOVE);

        //
        // Store the info to the table of EH block handlers
        //

        HBtab->ebdHandlerType = ToEHHandlerType(clause.Flags);

        HBtab->ebdTryBeg  = tryBegBB;
        HBtab->ebdTryLast = (tryEndBB == nullptr) ? fgLastBB : tryEndBB->Prev();

        HBtab->ebdHndBeg  = hndBegBB;
        HBtab->ebdHndLast = (hndEndBB == nullptr) ? fgLastBB : hndEndBB->Prev();

        //
        // Assert that all of our try/hnd blocks are setup correctly.
        //
        if (HBtab->ebdTryLast == nullptr)
        {
            BADCODE("Try Clause is invalid");
        }

        if (HBtab->ebdHndLast == nullptr)
        {
            BADCODE("Handler Clause is invalid");
        }

        //
        // Verify that it's legal
        //

        verInsertEhNode(&clause, HBtab);

    } // end foreach handler table entry

    fgSortEHTable();

    // Next, set things related to nesting that depend on the sorting being complete.

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        /* Mark all blocks in the finally/fault or catch clause */

        BasicBlock* tryBegBB = HBtab->ebdTryBeg;
        BasicBlock* hndBegBB = HBtab->ebdHndBeg;

        IL_OFFSET tryBegOff = HBtab->ebdTryBegOffset;
        IL_OFFSET tryEndOff = HBtab->ebdTryEndOffset;

        IL_OFFSET hndBegOff = HBtab->ebdHndBegOffset;
        IL_OFFSET hndEndOff = HBtab->ebdHndEndOffset;

        BasicBlock* block;

        for (block = hndBegBB; block && (block->bbCodeOffs < hndEndOff); block = block->Next())
        {
            if (!block->hasHndIndex())
            {
                block->setHndIndex(XTnum);

                // If the most nested EH handler region of this block is a 'fault' region, then change any
                // BBJ_EHFINALLYRET that were imported to BBJ_EHFAULTRET.
                if ((hndBegBB->bbCatchTyp == BBCT_FAULT) && block->KindIs(BBJ_EHFINALLYRET))
                {
                    block->SetKind(BBJ_EHFAULTRET);
                }
            }

            // All blocks in a catch handler or filter are rarely run, except the entry
            if ((block != hndBegBB) && (hndBegBB->bbCatchTyp != BBCT_FINALLY))
            {
                block->bbSetRunRarely();
            }
        }

        /* Mark all blocks within the covered range of the try */

        for (block = tryBegBB; block && (block->bbCodeOffs < tryEndOff); block = block->Next())
        {
            /* Mark this BB as belonging to a 'try' block */

            if (!block->hasTryIndex())
            {
                block->setTryIndex(XTnum);
            }

#ifdef DEBUG
            /* Note: the BB can't span the 'try' block */

            if (!block->HasFlag(BBF_INTERNAL))
            {
                noway_assert(tryBegOff <= block->bbCodeOffs);
                noway_assert(tryEndOff >= block->bbCodeOffsEnd || tryEndOff == tryBegOff);
            }
#endif
        }

/*  Init ebdHandlerNestingLevel of current clause, and bump up value for all
 *  enclosed clauses (which have to be before it in the table).
 *  Innermost try-finally blocks must precede outermost
 *  try-finally blocks.
 */

#if !defined(FEATURE_EH_FUNCLETS)
        HBtab->ebdHandlerNestingLevel = 0;
#endif // !FEATURE_EH_FUNCLETS

        HBtab->ebdEnclosingTryIndex = EHblkDsc::NO_ENCLOSING_INDEX;
        HBtab->ebdEnclosingHndIndex = EHblkDsc::NO_ENCLOSING_INDEX;

        noway_assert(XTnum < compHndBBtabCount);
        noway_assert(XTnum == ehGetIndex(HBtab));

        for (EHblkDsc* xtab = compHndBBtab; xtab < HBtab; xtab++)
        {
#if !defined(FEATURE_EH_FUNCLETS)
            if (jitIsBetween(xtab->ebdHndBegOffs(), hndBegOff, hndEndOff))
            {
                xtab->ebdHandlerNestingLevel++;
            }
#endif // !FEATURE_EH_FUNCLETS

            /* If we haven't recorded an enclosing try index for xtab then see
             *  if this EH region should be recorded.  We check if the
             *  first offset in the xtab lies within our region.  If so,
             *  the last offset also must lie within the region, due to
             *  nesting rules. verInsertEhNode(), below, will check for proper nesting.
             */
            if (xtab->ebdEnclosingTryIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                bool begBetween = jitIsBetween(xtab->ebdTryBegOffs(), tryBegOff, tryEndOff);
                if (begBetween)
                {
                    // Record the enclosing scope link
                    xtab->ebdEnclosingTryIndex = (unsigned short)XTnum;
                }
            }

            /* Do the same for the enclosing handler index.
             */
            if (xtab->ebdEnclosingHndIndex == EHblkDsc::NO_ENCLOSING_INDEX)
            {
                bool begBetween = jitIsBetween(xtab->ebdTryBegOffs(), hndBegOff, hndEndOff);
                if (begBetween)
                {
                    // Record the enclosing scope link
                    xtab->ebdEnclosingHndIndex = (unsigned short)XTnum;
                }
            }
        }

    } // end foreach handler table entry

#if !defined(FEATURE_EH_FUNCLETS)

    for (EHblkDsc* const HBtab : EHClauses(this))
    {
        if (ehMaxHndNestingCount <= HBtab->ebdHandlerNestingLevel)
            ehMaxHndNestingCount = HBtab->ebdHandlerNestingLevel + 1;
    }

#endif // !FEATURE_EH_FUNCLETS

    {
        // always run these checks for a debug build
        verCheckNestingLevel(initRoot);
    }

#ifndef DEBUG
    // fgNormalizeEH assumes that this test has been passed.  And Ssa assumes that fgNormalizeEHTable
    // has been run.  So do this unless we're in minOpts mode (and always in debug).
    if (!opts.MinOpts())
#endif
    {
        fgCheckBasicBlockControlFlow();
    }

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("*************** After fgFindBasicBlocks() has created the EH table\n");
        fgDispHandlerTab();
    }

    // We can't verify the handler table until all the IL legality checks have been done (above), since bad IL
    // (such as illegal nesting of regions) will trigger asserts here.
    fgVerifyHandlerTab();
#endif

    fgNormalizeEH();

    fgCheckForLoopsInHandlers();
}

//------------------------------------------------------------------------
// fgCheckForLoopsInHandlers: scan blocks seeing if any handler block
//   is a backedge target.
//
// Notes:
//    Sets compHasBackwardJumpInHandler if so. This will disable
//    setting patchpoints in this method and prompt the jit to
//    optimize the method instead.
//
//    We assume any late-added handler (say for synchronized methods) will
//    not introduce any loops.
//
void Compiler::fgCheckForLoopsInHandlers()
{
    // We only care about this if we are going to set OSR patchpoints
    // and the method has exception handling.
    //
    if (!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0))
    {
        return;
    }

    if (JitConfig.TC_OnStackReplacement() == 0)
    {
        return;
    }

    if (info.compXcptnsCount == 0)
    {
        return;
    }

    // Walk blocks in handlers and filters, looking for a backedge target.
    //
    assert(!compHasBackwardJumpInHandler);
    for (BasicBlock* const blk : Blocks())
    {
        if (blk->hasHndIndex())
        {
            if (blk->HasFlag(BBF_BACKWARD_JUMP_TARGET))
            {
                JITDUMP("\nHandler block " FMT_BB " is backward jump target; can't have patchpoints in this method\n",
                        blk->bbNum);
                compHasBackwardJumpInHandler = true;
                break;
            }
        }
    }
}

//------------------------------------------------------------------------
// fgFixEntryFlowForOSR: add control flow path from method start to
//   the appropriate IL offset for the OSR method
//
// Notes:
//    This is simply a branch from the method entry to the OSR entry --
//    the block where the OSR method should begin execution.
//
//    If the OSR entry is within a try we will eventually need add
//    suitable step blocks to reach the OSR entry without jumping into
//    the middle of the try. But we defer that until after importation.
//    See fgPostImportationCleanup.
//
//    Also protect the original method entry, if it was imported, since
//    we may decide to branch there during morph as part of the tail recursion
//    to loop optimization.
//
void Compiler::fgFixEntryFlowForOSR()
{
    // We should have looked for these blocks in fgLinkBasicBlocks.
    //
    assert(fgEntryBB != nullptr);
    assert(fgOSREntryBB != nullptr);

    // Now branch from method start to the OSR entry.
    //
    fgEnsureFirstBBisScratch();
    assert(fgFirstBB->KindIs(BBJ_ALWAYS) && fgFirstBB->JumpsToNext());
    fgRemoveRefPred(fgFirstBB->GetTarget(), fgFirstBB);
    FlowEdge* const newEdge = fgAddRefPred(fgOSREntryBB, fgFirstBB);
    newEdge->setLikelihood(1.0);
    fgFirstBB->SetKindAndTargetEdge(BBJ_ALWAYS, newEdge);

    // We don't know the right weight for this block, since
    // execution of the method was interrupted within the
    // loop containing fgOSREntryBB.
    //
    // A plausible guess might be to sum the non-backedge
    // weights of fgOSREntryBB and use those, but we don't
    // have edge weights available yet. Note that might be
    // an underestimate.
    //
    // For now we just guess that the loop will execute 100x.
    //
    fgFirstBB->inheritWeightPercentage(fgOSREntryBB, 1);

    JITDUMP("OSR: redirecting flow at method entry from " FMT_BB " to OSR entry " FMT_BB " for the importer\n",
            fgFirstBB->bbNum, fgOSREntryBB->bbNum);
}

/*****************************************************************************
 * Check control flow constraints for well formed IL. Bail if any of the constraints
 * are violated.
 */

void Compiler::fgCheckBasicBlockControlFlow()
{
    assert(!fgNormalizeEHDone); // These rules aren't quite correct after EH normalization has introduced new blocks

    EHblkDsc* HBtab;

    for (BasicBlock* const blk : Blocks())
    {
        if (blk->HasFlag(BBF_INTERNAL))
        {
            continue;
        }

        switch (blk->GetKind())
        {
            case BBJ_ALWAYS: // block does unconditional jump to target

                fgControlFlowPermitted(blk, blk->GetTarget());

                break;

            case BBJ_COND: // block conditionally jumps to the target

                fgControlFlowPermitted(blk, blk->GetFalseTarget());

                fgControlFlowPermitted(blk, blk->GetTrueTarget());

                break;

            case BBJ_RETURN: // block ends with 'ret'

                if (blk->hasTryIndex() || blk->hasHndIndex())
                {
                    BADCODE3("Return from a protected block", ". Before offset %04X", blk->bbCodeOffsEnd);
                }
                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFAULTRET:
            case BBJ_EHFILTERRET:

                if (!blk->hasHndIndex()) // must be part of a handler
                {
                    BADCODE3("Missing handler", ". Before offset %04X", blk->bbCodeOffsEnd);
                }

                HBtab = ehGetDsc(blk->getHndIndex());

                // Endfilter allowed only in a filter block
                if (blk->KindIs(BBJ_EHFILTERRET))
                {
                    if (!HBtab->HasFilter())
                    {
                        BADCODE("Unexpected endfilter");
                    }
                }
                else if (blk->KindIs(BBJ_EHFILTERRET))
                {
                    // endfinally allowed only in a finally block
                    if (!HBtab->HasFinallyHandler())
                    {
                        BADCODE("Unexpected endfinally");
                    }
                }
                else if (blk->KindIs(BBJ_EHFAULTRET))
                {
                    // 'endfault' (alias of IL 'endfinally') allowed only in a fault block
                    if (!HBtab->HasFaultHandler())
                    {
                        BADCODE("Unexpected endfault");
                    }
                }

                // The handler block should be the innermost block
                // Exception blocks are listed, innermost first.
                if (blk->hasTryIndex() && (blk->getTryIndex() < blk->getHndIndex()))
                {
                    BADCODE("endfinally / endfault / endfilter in nested try block");
                }

                break;

            case BBJ_THROW: // block ends with 'throw'
                /* throw is permitted from every BB, so nothing to check */
                /* importer makes sure that rethrow is done from a catch */
                break;

            case BBJ_LEAVE: // block always jumps to the target, maybe out of guarded
                            // region. Used temporarily until importing
                fgControlFlowPermitted(blk, blk->GetTarget(), true);

                break;

            case BBJ_SWITCH: // block ends with a switch statement
                for (BasicBlock* const bTarget : blk->SwitchTargets())
                {
                    fgControlFlowPermitted(blk, bTarget);
                }
                break;

            case BBJ_EHCATCHRET:  // block ends with a leave out of a catch (only #if defined(FEATURE_EH_FUNCLETS))
            case BBJ_CALLFINALLY: // block always calls the target finally
            default:
                noway_assert(!"Unexpected bbKind"); // these blocks don't get created until importing
                break;
        }
    }
}

/****************************************************************************
 * Check that the leave from the block is legal.
 * Consider removing this check here if we  can do it cheaply during importing
 */

void Compiler::fgControlFlowPermitted(BasicBlock* blkSrc, BasicBlock* blkDest, bool isLeave)
{
    assert(!fgNormalizeEHDone); // These rules aren't quite correct after EH normalization has introduced new blocks

    unsigned srcHndBeg, destHndBeg;
    unsigned srcHndEnd, destHndEnd;
    bool     srcInFilter, destInFilter;
    bool     srcInCatch = false;

    EHblkDsc* srcHndTab;

    srcHndTab = ehInitHndRange(blkSrc, &srcHndBeg, &srcHndEnd, &srcInFilter);
    ehInitHndRange(blkDest, &destHndBeg, &destHndEnd, &destInFilter);

    /* Impose the rules for leaving or jumping from handler blocks */

    if (blkSrc->hasHndIndex())
    {
        srcInCatch = srcHndTab->HasCatchHandler() && srcHndTab->InHndRegionILRange(blkSrc);

        /* Are we jumping within the same handler index? */
        if (BasicBlock::sameHndRegion(blkSrc, blkDest))
        {
            /* Do we have a filter clause? */
            if (srcHndTab->HasFilter())
            {
                /* filters and catch handlers share same eh index  */
                /* we need to check for control flow between them. */
                if (srcInFilter != destInFilter)
                {
                    if (!jitIsBetween(blkDest->bbCodeOffs, srcHndBeg, srcHndEnd))
                    {
                        BADCODE3("Illegal control flow between filter and handler", ". Before offset %04X",
                                 blkSrc->bbCodeOffsEnd);
                    }
                }
            }
        }
        else
        {
            /* The handler indexes of blkSrc and blkDest are different */
            if (isLeave)
            {
                /* Any leave instructions must not enter the dest handler from outside*/
                if (!jitIsBetween(srcHndBeg, destHndBeg, destHndEnd))
                {
                    BADCODE3("Illegal use of leave to enter handler", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
                }
            }
            else
            {
                /* We must use a leave to exit a handler */
                BADCODE3("Illegal control flow out of a handler", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }

            /* Do we have a filter clause? */
            if (srcHndTab->HasFilter())
            {
                /* It is ok to leave from the handler block of a filter, */
                /* but not from the filter block of a filter             */
                if (srcInFilter != destInFilter)
                {
                    BADCODE3("Illegal to leave a filter handler", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
                }
            }

            /* We should never leave a finally handler */
            if (srcHndTab->HasFinallyHandler())
            {
                BADCODE3("Illegal to leave a finally handler", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }

            /* We should never leave a fault handler */
            if (srcHndTab->HasFaultHandler())
            {
                BADCODE3("Illegal to leave a fault handler", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
    }
    else if (blkDest->hasHndIndex())
    {
        /* blkSrc was not inside a handler, but blkDst is inside a handler */
        BADCODE3("Illegal control flow into a handler", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
    }

    /* Are we jumping from a catch handler into the corresponding try? */
    /* VB uses this for "on error goto "                               */

    if (isLeave && srcInCatch)
    {
        // inspect all handlers containing the jump source

        bool      bValidJumpToTry   = false; // are we jumping in a valid way from a catch to the corresponding try?
        bool      bCatchHandlerOnly = true;  // false if we are jumping out of a non-catch handler
        EHblkDsc* ehTableEnd;
        EHblkDsc* ehDsc;

        for (ehDsc = compHndBBtab, ehTableEnd = compHndBBtab + compHndBBtabCount;
             bCatchHandlerOnly && ehDsc < ehTableEnd; ehDsc++)
        {
            if (ehDsc->InHndRegionILRange(blkSrc))
            {
                if (ehDsc->HasCatchHandler())
                {
                    if (ehDsc->InTryRegionILRange(blkDest))
                    {
                        // If we already considered the jump for a different try/catch,
                        // we would have two overlapping try regions with two overlapping catch
                        // regions, which is illegal.
                        noway_assert(!bValidJumpToTry);

                        // Allowed if it is the first instruction of an inner try
                        // (and all trys in between)
                        //
                        // try {
                        //  ..
                        // _tryAgain:
                        //  ..
                        //      try {
                        //      _tryNestedInner:
                        //        ..
                        //          try {
                        //          _tryNestedIllegal:
                        //            ..
                        //          } catch {
                        //            ..
                        //          }
                        //        ..
                        //      } catch {
                        //        ..
                        //      }
                        //  ..
                        // } catch {
                        //  ..
                        //  leave _tryAgain         // Allowed
                        //  ..
                        //  leave _tryNestedInner   // Allowed
                        //  ..
                        //  leave _tryNestedIllegal // Not Allowed
                        //  ..
                        // }
                        //
                        // Note: The leave is allowed also from catches nested inside the catch shown above.

                        /* The common case where leave is to the corresponding try */
                        if (ehDsc->ebdIsSameTry(this, blkDest->getTryIndex()) ||
                            /* Also allowed is a leave to the start of a try which starts in the handler's try */
                            fgFlowToFirstBlockOfInnerTry(ehDsc->ebdTryBeg, blkDest, false))
                        {
                            bValidJumpToTry = true;
                        }
                    }
                }
                else
                {
                    // We are jumping from a handler which is not a catch handler.

                    // If it's a handler, but not a catch handler, it must be either a finally or fault
                    if (!ehDsc->HasFinallyOrFaultHandler())
                    {
                        BADCODE3("Handlers must be catch, finally, or fault", ". Before offset %04X",
                                 blkSrc->bbCodeOffsEnd);
                    }

                    // Are we jumping out of this handler?
                    if (!ehDsc->InHndRegionILRange(blkDest))
                    {
                        bCatchHandlerOnly = false;
                    }
                }
            }
            else if (ehDsc->InFilterRegionILRange(blkSrc))
            {
                // Are we jumping out of a filter?
                if (!ehDsc->InFilterRegionILRange(blkDest))
                {
                    bCatchHandlerOnly = false;
                }
            }
        }

        if (bCatchHandlerOnly)
        {
            if (bValidJumpToTry)
            {
                return;
            }
            else
            {
                // FALL THROUGH
                // This is either the case of a leave to outside the try/catch,
                // or a leave to a try not nested in this try/catch.
                // The first case is allowed, the second one will be checked
                // later when we check the try block rules (it is illegal if we
                // jump to the middle of the destination try).
            }
        }
        else
        {
            BADCODE3("illegal leave to exit a finally, fault or filter", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
        }
    }

    /* Check all the try block rules */

    IL_OFFSET srcTryBeg;
    IL_OFFSET srcTryEnd;
    IL_OFFSET destTryBeg;
    IL_OFFSET destTryEnd;

    ehInitTryRange(blkSrc, &srcTryBeg, &srcTryEnd);
    ehInitTryRange(blkDest, &destTryBeg, &destTryEnd);

    /* Are we jumping between try indexes? */
    if (!BasicBlock::sameTryRegion(blkSrc, blkDest))
    {
        // Are we exiting from an inner to outer try?
        if (jitIsBetween(srcTryBeg, destTryBeg, destTryEnd) && jitIsBetween(srcTryEnd - 1, destTryBeg, destTryEnd))
        {
            if (!isLeave)
            {
                BADCODE3("exit from try block without a leave", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
        else if (jitIsBetween(destTryBeg, srcTryBeg, srcTryEnd))
        {
            // check that the dest Try is first instruction of an inner try
            if (!fgFlowToFirstBlockOfInnerTry(blkSrc, blkDest, false))
            {
                BADCODE3("control flow into middle of try", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
        else // there is no nesting relationship between src and dest
        {
            if (isLeave)
            {
                // check that the dest Try is first instruction of an inner try sibling
                if (!fgFlowToFirstBlockOfInnerTry(blkSrc, blkDest, true))
                {
                    BADCODE3("illegal leave into middle of try", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
                }
            }
            else
            {
                BADCODE3("illegal control flow in to/out of try block", ". Before offset %04X", blkSrc->bbCodeOffsEnd);
            }
        }
    }
}

/*****************************************************************************
 *  Check that blkDest is the first block of an inner try or a sibling
 *    with no intervening trys in between
 */

bool Compiler::fgFlowToFirstBlockOfInnerTry(BasicBlock* blkSrc, BasicBlock* blkDest, bool sibling)
{
    assert(!fgNormalizeEHDone); // These rules aren't quite correct after EH normalization has introduced new blocks

    noway_assert(blkDest->hasTryIndex());

    unsigned XTnum     = blkDest->getTryIndex();
    unsigned lastXTnum = blkSrc->hasTryIndex() ? blkSrc->getTryIndex() : compHndBBtabCount;
    noway_assert(XTnum < compHndBBtabCount);
    noway_assert(lastXTnum <= compHndBBtabCount);

    EHblkDsc* HBtab = ehGetDsc(XTnum);

    // check that we are not jumping into middle of try
    if (HBtab->ebdTryBeg != blkDest)
    {
        return false;
    }

    if (sibling)
    {
        noway_assert(!BasicBlock::sameTryRegion(blkSrc, blkDest));

        // find the l.u.b of the two try ranges
        // Set lastXTnum to the l.u.b.

        HBtab = ehGetDsc(lastXTnum);

        for (lastXTnum++, HBtab++; lastXTnum < compHndBBtabCount; lastXTnum++, HBtab++)
        {
            if (jitIsBetweenInclusive(blkDest->bbNum, HBtab->ebdTryBeg->bbNum, HBtab->ebdTryLast->bbNum))
            {
                break;
            }
        }
    }

    // now check there are no intervening trys between dest and l.u.b
    // (it is ok to have intervening trys as long as they all start at
    //  the same code offset)

    HBtab = ehGetDsc(XTnum);

    for (XTnum++, HBtab++; XTnum < lastXTnum; XTnum++, HBtab++)
    {
        if (HBtab->ebdTryBeg->bbNum < blkDest->bbNum && blkDest->bbNum <= HBtab->ebdTryLast->bbNum)
        {
            return false;
        }
    }

    return true;
}

/*****************************************************************************
 *  Returns the handler nesting level of the block.
 *  *pFinallyNesting is set to the nesting level of the inner-most
 *  finally-protected try the block is in.
 */

unsigned Compiler::fgGetNestingLevel(BasicBlock* block, unsigned* pFinallyNesting)
{
    unsigned  curNesting = 0;            // How many handlers is the block in
    unsigned  tryFin     = (unsigned)-1; // curNesting when we see innermost finally-protected try
    unsigned  XTnum;
    EHblkDsc* HBtab;

    /* We find the block's handler nesting level by walking over the
       complete exception table and find enclosing clauses. */

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        noway_assert(HBtab->ebdTryBeg && HBtab->ebdHndBeg);

        if (HBtab->HasFinallyHandler() && (tryFin == (unsigned)-1) && bbInTryRegions(XTnum, block))
        {
            tryFin = curNesting;
        }
        else if (bbInHandlerRegions(XTnum, block))
        {
            curNesting++;
        }
    }

    if (tryFin == (unsigned)-1)
    {
        tryFin = curNesting;
    }

    if (pFinallyNesting)
    {
        *pFinallyNesting = curNesting - tryFin;
    }

    return curNesting;
}

//------------------------------------------------------------------------
// fgFindBlockILOffset: Given a block, find the IL offset corresponding to the first statement
//      in the block with a legal IL offset. Skip any leading statements that have BAD_IL_OFFSET.
//      If no statement has an initialized statement offset (including the case where there are
//      no statements in the block), then return BAD_IL_OFFSET. This function is used when
//      blocks are split or modified, and we want to maintain the IL offset as much as possible
//      to preserve good debugging behavior.
//
// Arguments:
//      block - The block to check.
//
// Return Value:
//      The first good IL offset of a statement in the block, or BAD_IL_OFFSET if such an IL offset
//      cannot be found.
//
IL_OFFSET Compiler::fgFindBlockILOffset(BasicBlock* block)
{
    // This function searches for IL offsets in statement nodes, so it can't be used in LIR. We
    // could have a similar function for LIR that searches for GT_IL_OFFSET nodes.
    assert(!block->IsLIR());

    for (Statement* const stmt : block->Statements())
    {
        // Blocks always contain IL offsets in the root.
        DebugInfo di = stmt->GetDebugInfo().GetRoot();
        if (di.IsValid())
        {
            return di.GetLocation().GetOffset();
        }
    }

    return BAD_IL_OFFSET;
}

//------------------------------------------------------------------------------
// fgSplitBlockAtEnd - split the given block into two blocks.
//                   All code in the block stays in the original block.
//                   Control falls through from original to new block, and
//                   the new block is returned.
//------------------------------------------------------------------------------
BasicBlock* Compiler::fgSplitBlockAtEnd(BasicBlock* curr)
{
    // We'd like to use fgNewBBafter(), but we need to update the preds list before linking in the new block.
    // (We need the successors of 'curr' to be correct when we do this.)
    BasicBlock* newBlock = BasicBlock::New(this);

    // Start the new block with no refs. When we set the preds below, this will get updated correctly.
    newBlock->bbRefs = 0;

    if (curr->KindIs(BBJ_SWITCH))
    {
        // In the case of a switch statement there's more complicated logic in order to wire up the predecessor lists
        // but fortunately there's an existing method that implements this functionality.
        fgChangeSwitchBlock(curr, newBlock);
    }
    else
    {
        // For each successor of the original block, set the new block as their predecessor.

        for (BasicBlock* const succ : curr->Succs(this))
        {
            if (succ != newBlock)
            {
                JITDUMP(FMT_BB " previous predecessor was " FMT_BB ", now is " FMT_BB "\n", succ->bbNum, curr->bbNum,
                        newBlock->bbNum);
                fgReplacePred(succ, curr, newBlock);
            }
        }
    }

    newBlock->inheritWeight(curr);

    // Set the new block's flags. Note that the new block isn't BBF_INTERNAL unless the old block is.
    newBlock->CopyFlags(curr);

    // Remove flags that the new block can't have.
    newBlock->RemoveFlags(BBF_LOOP_HEAD | BBF_FUNCLET_BEG | BBF_KEEP_BBJ_ALWAYS | BBF_PATCHPOINT |
                          BBF_BACKWARD_JUMP_TARGET | BBF_LOOP_ALIGN);

    // Remove the GC safe bit on the new block. It seems clear that if we split 'curr' at the end,
    // such that all the code is left in 'curr', and 'newBlock' just gets the control flow, then
    // both 'curr' and 'newBlock' could accurately retain an existing GC safe bit. However, callers
    // use this function to split blocks in the middle, or at the beginning, and they don't seem to
    // be careful about updating this flag appropriately. So, removing the GC safe bit is simply
    // conservative: some functions might end up being fully interruptible that could be partially
    // interruptible if we exercised more care here.
    newBlock->RemoveFlags(BBF_GC_SAFE_POINT);

    // The new block has no code, so we leave bbCodeOffs/bbCodeOffsEnd set to BAD_IL_OFFSET. If a caller
    // puts code in the block, then it needs to update these.

    // Insert the new block in the block list after the 'curr' block.
    fgInsertBBafter(curr, newBlock);
    fgExtendEHRegionAfter(curr); // The new block is in the same EH region as the old block.

    // Remove flags from the old block that are no longer possible.
    curr->RemoveFlags(BBF_HAS_JMP | BBF_RETLESS_CALL);

    // Transfer the kind and target. Do this after the code above, to avoid null-ing out the old targets used by the
    // above code (and so newBlock->bbNext is valid, so SetCond() can initialize bbFalseTarget if newBlock is a
    // BBJ_COND).
    newBlock->TransferTarget(curr);

    // Default to fallthrough, and add the arc for that.
    FlowEdge* const newEdge = fgAddRefPred(newBlock, curr);
    newEdge->setLikelihood(1.0);
    
    curr->SetKindAndTargetEdge(BBJ_ALWAYS, newEdge);
    curr->SetFlags(BBF_NONE_QUIRK);
    assert(curr->JumpsToNext());

    return newBlock;
}

//------------------------------------------------------------------------------
// fgSplitBlockAfterStatement - Split the given block, with all code after
//                              the given statement going into the second block.
//------------------------------------------------------------------------------
BasicBlock* Compiler::fgSplitBlockAfterStatement(BasicBlock* curr, Statement* stmt)
{
    assert(!curr->IsLIR()); // No statements in LIR, so you can't use this function.

    BasicBlock* newBlock = fgSplitBlockAtEnd(curr);

    if (stmt != nullptr)
    {
        newBlock->bbStmtList = stmt->GetNextStmt();
        if (newBlock->bbStmtList != nullptr)
        {
            newBlock->bbStmtList->SetPrevStmt(curr->bbStmtList->GetPrevStmt());
        }
        curr->bbStmtList->SetPrevStmt(stmt);
        stmt->SetNextStmt(nullptr);

        // Update the IL offsets of the blocks to match the split.

        assert(newBlock->bbCodeOffs == BAD_IL_OFFSET);
        assert(newBlock->bbCodeOffsEnd == BAD_IL_OFFSET);

        // curr->bbCodeOffs remains the same
        newBlock->bbCodeOffsEnd = curr->bbCodeOffsEnd;

        IL_OFFSET splitPointILOffset = fgFindBlockILOffset(newBlock);

        curr->bbCodeOffsEnd  = max(curr->bbCodeOffs, splitPointILOffset);
        newBlock->bbCodeOffs = min(splitPointILOffset, newBlock->bbCodeOffsEnd);
    }
    else
    {
        assert(curr->bbStmtList == nullptr); // if no tree was given then it better be an empty block
    }

    return newBlock;
}

//------------------------------------------------------------------------------
// fgSplitBlockBeforeTree : Split the given block right before the given tree
//
// Arguments:
//    block        - The block containing the statement.
//    stmt         - The statement containing the tree.
//    splitPoint   - A tree inside the statement.
//    firstNewStmt - [out] The first new statement that was introduced.
//                   [firstNewStmt..stmt) are the statements added by this function.
//    splitNodeUse - The use of the tree to split at.
//
// Returns:
//    The last block after split
//
// Notes:
//    See comments in gtSplitTree
//
BasicBlock* Compiler::fgSplitBlockBeforeTree(
    BasicBlock* block, Statement* stmt, GenTree* splitPoint, Statement** firstNewStmt, GenTree*** splitNodeUse)
{
    gtSplitTree(block, stmt, splitPoint, firstNewStmt, splitNodeUse);

    BasicBlockFlags originalFlags = block->GetFlagsRaw();
    BasicBlock*     prevBb        = block;

    // We use fgSplitBlockAfterStatement() API here to split the block, however, we want to split
    // it *Before* rather than *After* so if the current statement is the first in the
    // current block - invoke fgSplitBlockAtBeginning
    if (stmt == block->firstStmt())
    {
        block = fgSplitBlockAtBeginning(prevBb);
    }
    else
    {
        assert(stmt->GetPrevStmt() != block->lastStmt());
        JITDUMP("Splitting " FMT_BB " after statement " FMT_STMT "\n", prevBb->bbNum, stmt->GetPrevStmt()->GetID());
        block = fgSplitBlockAfterStatement(prevBb, stmt->GetPrevStmt());
    }

    // We split a block, possibly, in the middle - we need to propagate some flags
    prevBb->SetFlagsRaw(originalFlags & (~(BBF_SPLIT_LOST | BBF_RETLESS_CALL) | BBF_GC_SAFE_POINT));
    block->SetFlags(originalFlags & (BBF_SPLIT_GAINED | BBF_IMPORTED | BBF_GC_SAFE_POINT | BBF_RETLESS_CALL));

    // prevBb should flow into block
    assert(prevBb->KindIs(BBJ_ALWAYS) && prevBb->JumpsToNext() && prevBb->NextIs(block));
    prevBb->SetFlags(BBF_NONE_QUIRK);

    return block;
}

//------------------------------------------------------------------------------
// fgSplitBlockAfterNode - Split the given block, with all code after
//                         the given node going into the second block.
//                         This function is only used in LIR.
//------------------------------------------------------------------------------
BasicBlock* Compiler::fgSplitBlockAfterNode(BasicBlock* curr, GenTree* node)
{
    assert(curr->IsLIR());

    BasicBlock* newBlock = fgSplitBlockAtEnd(curr);

    if (node != nullptr)
    {
        LIR::Range& currBBRange = LIR::AsRange(curr);

        if (node != currBBRange.LastNode())
        {
            LIR::Range nodesToMove = currBBRange.Remove(node->gtNext, currBBRange.LastNode());
            LIR::AsRange(newBlock).InsertAtBeginning(std::move(nodesToMove));
        }

        // Update the IL offsets of the blocks to match the split.

        assert(newBlock->bbCodeOffs == BAD_IL_OFFSET);
        assert(newBlock->bbCodeOffsEnd == BAD_IL_OFFSET);

        // curr->bbCodeOffs remains the same
        newBlock->bbCodeOffsEnd = curr->bbCodeOffsEnd;

        // Search backwards from the end of the current block looking for the IL offset to use
        // for the end IL offset for the original block.
        IL_OFFSET                   splitPointILOffset = BAD_IL_OFFSET;
        LIR::Range::ReverseIterator riter;
        LIR::Range::ReverseIterator riterEnd;
        for (riter = currBBRange.rbegin(), riterEnd = currBBRange.rend(); riter != riterEnd; ++riter)
        {
            if ((*riter)->gtOper == GT_IL_OFFSET)
            {
                GenTreeILOffset* ilOffset = (*riter)->AsILOffset();
                DebugInfo        rootDI   = ilOffset->gtStmtDI.GetRoot();
                if (rootDI.IsValid())
                {
                    splitPointILOffset = rootDI.GetLocation().GetOffset();
                    break;
                }
            }
        }

        curr->bbCodeOffsEnd = max(curr->bbCodeOffs, splitPointILOffset);

        // Also use this as the beginning offset of the next block. Presumably we could/should
        // look to see if the first node is a GT_IL_OFFSET node, and use that instead.
        newBlock->bbCodeOffs = min(splitPointILOffset, newBlock->bbCodeOffsEnd);
    }
    else
    {
        assert(curr->bbStmtList == nullptr); // if no node was given then it better be an empty block
    }

    return newBlock;
}

//------------------------------------------------------------------------------
// fgSplitBlockAtBeginning - Split the given block into two blocks.
//                         Control falls through from original to new block,
//                         and the new block is returned.
//                         All code in the original block goes into the new block
//------------------------------------------------------------------------------
BasicBlock* Compiler::fgSplitBlockAtBeginning(BasicBlock* curr)
{
    BasicBlock* newBlock = fgSplitBlockAtEnd(curr);

    if (curr->IsLIR())
    {
        newBlock->SetFirstLIRNode(curr->GetFirstLIRNode());
        curr->SetFirstLIRNode(nullptr);
    }
    else
    {
        newBlock->bbStmtList = curr->bbStmtList;
        curr->bbStmtList     = nullptr;
    }

    // The new block now has all the code, and the old block has none. Update the
    // IL offsets for the block to reflect this.

    newBlock->bbCodeOffs    = curr->bbCodeOffs;
    newBlock->bbCodeOffsEnd = curr->bbCodeOffsEnd;

    curr->bbCodeOffs    = BAD_IL_OFFSET;
    curr->bbCodeOffsEnd = BAD_IL_OFFSET;

    return newBlock;
}

//------------------------------------------------------------------------
// fgSplitEdge: Splits the edge between a block 'curr' and its successor 'succ' by creating a new block
//              that replaces 'succ' as a successor of 'curr', and which branches unconditionally
//              to (or falls through to) 'succ'. Note that for a BBJ_COND block 'curr',
//              'succ' might be the fall-through path or the branch path from 'curr'.
//
// Arguments:
//    curr - A block which branches to 'succ'
//    succ - The target block
//
// Return Value:
//    Returns a new block, that is a successor of 'curr' and which branches unconditionally to 'succ'
//
// Assumptions:
//    'curr' must have a bbKind of BBJ_COND, BBJ_ALWAYS, or BBJ_SWITCH
//
// Notes:
//    The returned block is empty.
//    Can be invoked before pred lists are built.

BasicBlock* Compiler::fgSplitEdge(BasicBlock* curr, BasicBlock* succ)
{
    assert(curr->KindIs(BBJ_COND, BBJ_SWITCH, BBJ_ALWAYS));
    assert(fgPredsComputed);
    assert(fgGetPredForBlock(succ, curr) != nullptr);

    BasicBlock* newBlock;
    if (curr->NextIs(succ))
    {
        // The successor is the fall-through path of a BBJ_COND, or
        // an immediately following block of a BBJ_SWITCH (which has
        // no fall-through path). For this case, simply insert a new
        // fall-through block after 'curr'.
        // TODO-NoFallThrough: Once bbFalseTarget can diverge from bbNext, this will be unnecessary for BBJ_COND
        newBlock = fgNewBBafter(BBJ_ALWAYS, curr, true /* extendRegion */);
        newBlock->SetFlags(BBF_NONE_QUIRK);
        assert(newBlock->JumpsToNext());
    }
    else
    {
        // The new block always jumps to 'succ'
        newBlock = fgNewBBinRegion(BBJ_ALWAYS, curr, /* isRunRarely */ curr->isRunRarely());
    }
    newBlock->CopyFlags(curr, succ->GetFlagsRaw() & BBF_BACKWARD_JUMP);

    JITDUMP("Splitting edge from " FMT_BB " to " FMT_BB "; adding " FMT_BB "\n", curr->bbNum, succ->bbNum,
            newBlock->bbNum);

    // newBlock replaces succ as curr's successor.
    fgReplaceJumpTarget(curr, succ, newBlock);

    // And 'succ' has 'newBlock' as a new predecessor.
    FlowEdge* const newEdge = fgAddRefPred(succ, newBlock);
    newEdge->setLikelihood(1.0);
    newBlock->SetTargetEdge(newEdge);

    // This isn't accurate, but it is complex to compute a reasonable number so just assume that we take the
    // branch 50% of the time.
    //
    // TODO: leverage edge likelihood.
    //
    if (!curr->KindIs(BBJ_ALWAYS))
    {
        newBlock->inheritWeightPercentage(curr, 50);
    }

    // The bbLiveIn and bbLiveOut are both equal to the bbLiveIn of 'succ'
    if (fgLocalVarLivenessDone)
    {
        VarSetOps::Assign(this, newBlock->bbLiveIn, succ->bbLiveIn);
        VarSetOps::Assign(this, newBlock->bbLiveOut, succ->bbLiveIn);
    }

    return newBlock;
}

// Removes the block from the bbPrev/bbNext chain
// Updates fgFirstBB and fgLastBB if necessary
// Does not update fgFirstFuncletBB or fgFirstColdBlock (fgUnlinkRange does)
void Compiler::fgUnlinkBlock(BasicBlock* block)
{
    if (block->IsFirst())
    {
        assert(block == fgFirstBB);
        assert(block != fgLastBB);
        assert((fgFirstBBScratch == nullptr) || (fgFirstBBScratch == fgFirstBB));

        fgFirstBB = block->Next();
        fgFirstBB->SetPrevToNull();

        if (fgFirstBBScratch != nullptr)
        {
#ifdef DEBUG
            // We had created an initial scratch BB, but now we're deleting it.
            if (verbose)
            {
                printf("Unlinking scratch " FMT_BB "\n", block->bbNum);
            }
#endif // DEBUG
            fgFirstBBScratch = nullptr;
        }
    }
    else if (block->IsLast())
    {
        assert(fgLastBB == block);
        fgLastBB = block->Prev();
        fgLastBB->SetNextToNull();
    }
    else
    {
        block->Prev()->SetNext(block->Next());
    }
}

//------------------------------------------------------------------------
// fgUnlinkBlockForRemoval: unlink a block from the linked list because it is
// being removed, and adjust fgBBcount.
//
// Arguments:
//   block - The block
//
void Compiler::fgUnlinkBlockForRemoval(BasicBlock* block)
{
    fgUnlinkBlock(block);
    fgBBcount--;
}

/*****************************************************************************************************
 *
 *  Function called to unlink basic block range [bBeg .. bEnd] from the basic block list.
 *
 *  'bBeg' can't be the first block.
 */

void Compiler::fgUnlinkRange(BasicBlock* bBeg, BasicBlock* bEnd)
{
    assert(bBeg != nullptr);
    assert(bEnd != nullptr);

    BasicBlock* bPrev = bBeg->Prev();
    assert(bPrev != nullptr); // Can't unlink a range starting with the first block

    /* If we removed the last block in the method then update fgLastBB */
    if (fgLastBB == bEnd)
    {
        fgLastBB = bPrev;
        fgLastBB->SetNextToNull();
    }
    else
    {
        bPrev->SetNext(bEnd->Next());
    }

    // If bEnd was the first Cold basic block update fgFirstColdBlock
    if (bEnd->IsFirstColdBlock(this))
    {
        fgFirstColdBlock = bPrev->Next();
    }

#if defined(FEATURE_EH_FUNCLETS)
#ifdef DEBUG
    // You can't unlink a range that includes the first funclet block. A range certainly
    // can't cross the non-funclet/funclet region. And you can't unlink the first block
    // of the first funclet with this, either. (If that's necessary, it could be allowed
    // by updating fgFirstFuncletBB to bEnd->bbNext.)
    for (BasicBlock* tempBB = bBeg; tempBB != bEnd->Next(); tempBB = tempBB->Next())
    {
        assert(tempBB != fgFirstFuncletBB);
    }
#endif // DEBUG
#endif // FEATURE_EH_FUNCLETS
}

//------------------------------------------------------------------------
// fgRemoveBlock: Remove a basic block. The block must be either unreachable or empty.
// If the block is a non-retless BBJ_CALLFINALLY then the paired BBJ_CALLFINALLYRET is also removed.
//
// Arguments:
//   block       - the block to remove
//   unreachable - indicates whether removal is because block is unreachable or empty
//
// Return Value:
//   The block after the block, or blocks, removed.
//
BasicBlock* Compiler::fgRemoveBlock(BasicBlock* block, bool unreachable)
{
    assert(block != nullptr);

    JITDUMP("fgRemoveBlock " FMT_BB ", unreachable=%s\n", block->bbNum, dspBool(unreachable));

    BasicBlock* bPrev = block->Prev();
    BasicBlock* bNext = block->Next();

    noway_assert((block == fgFirstBB) || (bPrev && bPrev->NextIs(block)));
    noway_assert(!block->HasFlag(BBF_DONT_REMOVE));

    // Should never remove a genReturnBB, as we might have special hookups there.
    noway_assert(block != genReturnBB);

    if (unreachable)
    {
        PREFIX_ASSUME(bPrev != nullptr);

        fgUnreachableBlock(block);

#if defined(FEATURE_EH_FUNCLETS)
        // If block was the fgFirstFuncletBB then set fgFirstFuncletBB to block->bbNext
        if (block == fgFirstFuncletBB)
        {
            fgFirstFuncletBB = block->Next();
        }
#endif // FEATURE_EH_FUNCLETS

        // If this is the first Cold basic block update fgFirstColdBlock
        if (block->IsFirstColdBlock(this))
        {
            fgFirstColdBlock = block->Next();
        }

        // A BBJ_CALLFINALLY is usually paired with a BBJ_CALLFINALLYRET.
        // If we delete such a BBJ_CALLFINALLY we also delete the BBJ_CALLFINALLYRET.
        if (block->isBBCallFinallyPair())
        {
            BasicBlock* const leaveBlock = block->Next();
            bNext                        = leaveBlock->Next();
            fgPrepareCallFinallyRetForRemoval(leaveBlock);
            fgRemoveBlock(leaveBlock, /* unreachable */ true);
        }
        else if (block->isBBCallFinallyPairTail())
        {
            // bPrev CALLFINALLY becomes RETLESS as the BBJ_CALLFINALLYRET block is unreachable
            bPrev->SetFlags(BBF_RETLESS_CALL);
        }
        else if (block->KindIs(BBJ_RETURN))
        {
            fgRemoveReturnBlock(block);
        }

        /* Unlink this block from the bbNext chain */
        fgUnlinkBlockForRemoval(block);

        /* At this point the bbPreds and bbRefs had better be zero */
        noway_assert((block->bbRefs == 0) && (block->bbPreds == nullptr));
    }
    else // block is empty
    {
        noway_assert(block->isEmpty());

        // The block cannot follow a non-retless BBJ_CALLFINALLY (because we don't know who may jump to it).
        noway_assert(!block->isBBCallFinallyPairTail());

#ifdef DEBUG
        if (verbose)
        {
            printf("Removing empty " FMT_BB "\n", block->bbNum);
        }

        /* Some extra checks for the empty case */
        assert(block->KindIs(BBJ_ALWAYS));

        /* Do not remove a block that jumps to itself - used for while (true){} */
        assert(!block->TargetIs(block));
#endif // DEBUG

        BasicBlock* succBlock = block->GetTarget();

        bool skipUnmarkLoop = false;

        if (succBlock->isLoopHead() && bPrev && (succBlock->bbNum <= bPrev->bbNum))
        {
            // It looks like `block` is the source of a back edge of a loop, and once we remove `block` the
            // loop will still exist because we'll move the edge to `bPrev`. So, don't unscale the loop blocks.
            skipUnmarkLoop = true;
        }

        // If this is the first Cold basic block update fgFirstColdBlock
        if (block->IsFirstColdBlock(this))
        {
            fgFirstColdBlock = block->Next();
        }

#if defined(FEATURE_EH_FUNCLETS)
        // Update fgFirstFuncletBB if necessary
        if (block == fgFirstFuncletBB)
        {
            fgFirstFuncletBB = block->Next();
        }
#endif // FEATURE_EH_FUNCLETS

        // Update successor block start IL offset, if empty predecessor
        // covers the immediately preceding range.
        if ((block->bbCodeOffsEnd == succBlock->bbCodeOffs) && (block->bbCodeOffs != BAD_IL_OFFSET))
        {
            assert(block->bbCodeOffs <= succBlock->bbCodeOffs);
            succBlock->bbCodeOffs = block->bbCodeOffs;
        }

        /* Remove the block */

        if (bPrev == nullptr)
        {
            /* special case if this is the first BB */

            noway_assert(block == fgFirstBB);

            /* old block no longer gets the extra ref count for being the first block */
            block->bbRefs--;
            succBlock->bbRefs++;
        }

        /* Update bbRefs and bbPreds.
         * All blocks jumping to 'block' will jump to 'succBlock'.
         * First, remove 'block' from the predecessor list of succBlock.
         */

        fgRemoveRefPred(succBlock, block);

        for (BasicBlock* const predBlock : block->PredBlocks())
        {
            /* change all jumps/refs to the removed block */
            switch (predBlock->GetKind())
            {
                default:
                    noway_assert(!"Unexpected bbKind in fgRemoveBlock()");
                    break;

                case BBJ_COND:
                case BBJ_CALLFINALLY:
                case BBJ_CALLFINALLYRET:
                case BBJ_ALWAYS:
                case BBJ_EHCATCHRET:
                case BBJ_SWITCH:
                case BBJ_EHFINALLYRET:
                    fgReplaceJumpTarget(predBlock, block, succBlock);
                    break;
            }
        }

        fgUnlinkBlockForRemoval(block);
        block->SetFlags(BBF_REMOVED);
    }

    if (bPrev != nullptr)
    {
        switch (bPrev->GetKind())
        {
            case BBJ_CALLFINALLY:
                // If prev is a BBJ_CALLFINALLY it better be marked as RETLESS
                noway_assert(bPrev->HasFlag(BBF_RETLESS_CALL));
                break;

            case BBJ_COND:
                // block should not be a target anymore
                assert(!bPrev->TrueTargetIs(block));
                assert(!bPrev->FalseTargetIs(block));

                /* Check if both sides of the BBJ_COND now jump to the same block */
                if (bPrev->TrueTargetIs(bPrev->GetFalseTarget()))
                {
                    fgRemoveConditionalJump(bPrev);
                }
                break;

            default:
                break;
        }

        ehUpdateForDeletedBlock(block);
    }

    return bNext;
}

//------------------------------------------------------------------------
// fgPrepareCallFinallyRetForRemoval: Prepare an unreachable BBJ_CALLFINALLYRET block for removal
// from the flow graph. Remove the block as a successor to predecessor BBJ_EHFINALLYRET blocks.
// Don't actually remove the block: change it to a BBJ_ALWAYS. The caller can either remove it
// directly, or wait for an unreachable code pass to remove it. This is done to avoid altering
// caller flow graph iteration. Note that this must be called before changing/removing the
// paired BBJ_CALLFINALLY.
//
// Arguments:
//   block - the block to process
//
void Compiler::fgPrepareCallFinallyRetForRemoval(BasicBlock* block)
{
    assert(block->KindIs(BBJ_CALLFINALLYRET));

    BasicBlock* const bCallFinally = block->Prev();
    assert(bCallFinally != nullptr);
    assert(bCallFinally->KindIs(BBJ_CALLFINALLY));

    block->RemoveFlags(BBF_DONT_REMOVE);

    // The BBJ_CALLFINALLYRET normally has a reference count of 1 and a single predecessor.
    // However, we might not have marked the BBJ_CALLFINALLY as BBF_RETLESS_CALL even though it is.
    // (Some early flow optimization should probably aggressively mark these as BBF_RETLESS_CALL
    // and not depend on fgRemoveBlock() to do that.)
    for (FlowEdge* leavePredEdge : block->PredEdges())
    {
        fgRemoveEhfSuccessor(leavePredEdge);
    }
    assert(block->bbRefs == 0);
    assert(block->bbPreds == nullptr);

    // If the BBJ_CALLFINALLYRET is unreachable, then the BBJ_CALLFINALLY must be retless.
    // Set to retless flag to avoid future asserts.
    bCallFinally->SetFlags(BBF_RETLESS_CALL);
    block->SetKind(BBJ_ALWAYS);
}

//------------------------------------------------------------------------
// fgConnectFallThrough: fix flow from a block that previously had a fall through
//
// Arguments:
//   bSrc - source of fall through
//   bDst - target of fall through
//
// Returns:
//   Newly inserted block after bSrc that jumps to bDst,
//   or nullptr if bSrc already falls through to bDst
//
BasicBlock* Compiler::fgConnectFallThrough(BasicBlock* bSrc, BasicBlock* bDst)
{
    assert(bSrc != nullptr);
    assert(fgPredsComputed);
    BasicBlock* jmpBlk = nullptr;

    /* If bSrc falls through to a block that is not bDst, we will insert a jump to bDst */

    if (bSrc->KindIs(BBJ_COND) && bSrc->FalseTargetIs(bDst) && !bSrc->NextIs(bDst))
    {
        // Add a new block after bSrc which jumps to 'bDst'
        jmpBlk = fgNewBBafter(BBJ_ALWAYS, bSrc, true);
        FlowEdge* oldEdge = bSrc->GetFalseEdge();
        fgReplacePred(oldEdge, jmpBlk);
        assert(jmpBlk->TargetIs(bDst));

        FlowEdge* newEdge = fgAddRefPred(jmpBlk, bSrc, oldEdge);
        bSrc->SetFalseEdge(newEdge);

        // When adding a new jmpBlk we will set the bbWeight and bbFlags
        //
        if (fgHaveValidEdgeWeights && fgHaveProfileWeights())
        {
            jmpBlk->bbWeight = (newEdge->edgeWeightMin() + newEdge->edgeWeightMax()) / 2;
            if (bSrc->bbWeight == BB_ZERO_WEIGHT)
            {
                jmpBlk->bbWeight = BB_ZERO_WEIGHT;
            }

            if (jmpBlk->bbWeight == BB_ZERO_WEIGHT)
            {
                jmpBlk->SetFlags(BBF_RUN_RARELY);
            }

            weight_t weightDiff = (newEdge->edgeWeightMax() - newEdge->edgeWeightMin());
            weight_t slop       = BasicBlock::GetSlopFraction(bSrc, bDst);
            //
            // If the [min/max] values for our edge weight is within the slop factor
            //  then we will set the BBF_PROF_WEIGHT flag for the block
            //
            if (weightDiff <= slop)
            {
                jmpBlk->SetFlags(BBF_PROF_WEIGHT);
            }
        }
        else
        {
            // We set the bbWeight to the smaller of bSrc->bbWeight or bDst->bbWeight
            if (bSrc->bbWeight < bDst->bbWeight)
            {
                jmpBlk->bbWeight = bSrc->bbWeight;
                jmpBlk->CopyFlags(bSrc, BBF_RUN_RARELY);
            }
            else
            {
                jmpBlk->bbWeight = bDst->bbWeight;
                jmpBlk->CopyFlags(bDst, BBF_RUN_RARELY);
            }
        }

        JITDUMP("Added an unconditional jump to " FMT_BB " after block " FMT_BB "\n", jmpBlk->GetTarget()->bbNum,
                bSrc->bbNum);
    }
    else if (bSrc->KindIs(BBJ_ALWAYS) && bSrc->HasInitializedTarget() && bSrc->JumpsToNext())
    {
        bSrc->SetFlags(BBF_NONE_QUIRK);
    }

    return jmpBlk;
}

//------------------------------------------------------------------------
// fgRenumberBlocks: update block bbNums to reflect bbNext order
//
// Returns:
//    true if blocks were renumbered or maxBBNum was updated.
//
// Notes:
//   Walk the flow graph, reassign block numbers to keep them in ascending order.
//   Return 'true' if any renumbering was actually done, OR if we change the
//   maximum number of assigned basic blocks (this can happen if we do inlining,
//   create a new, high-numbered block, then that block goes away. We go to
//   renumber the blocks, none of them actually change number, but we shrink the
//   maximum assigned block number. This affects the block set epoch).
//
//   As a consequence of renumbering, block pred lists may need to be reordered.
//
bool Compiler::fgRenumberBlocks()
{
    assert(fgPredsComputed);

    JITDUMP("\n*************** Before renumbering the basic blocks\n");
    JITDUMPEXEC(fgDispBasicBlocks());
    JITDUMPEXEC(fgDispHandlerTab());

    bool     renumbered  = false;
    bool     newMaxBBNum = false;
    unsigned num         = 1;

    for (BasicBlock* block : Blocks())
    {
        noway_assert(!block->HasFlag(BBF_REMOVED));

        if (block->bbNum != num)
        {
            JITDUMP("Renumber " FMT_BB " to " FMT_BB "\n", block->bbNum, num);
            renumbered   = true;
            block->bbNum = num;
        }

        if (block->IsLast())
        {
            fgLastBB = block;
            if (fgBBNumMax != num)
            {
                fgBBNumMax  = num;
                newMaxBBNum = true;
            }
        }

        num++;
    }

    // If we renumbered, then we may need to reorder some pred lists.
    //
    if (renumbered)
    {
        for (BasicBlock* const block : Blocks())
        {
            block->ensurePredListOrder(this);
        }
        JITDUMP("\n*************** After renumbering the basic blocks\n");
        JITDUMPEXEC(fgDispBasicBlocks());
        JITDUMPEXEC(fgDispHandlerTab());
    }
    else
    {
        JITDUMP("=============== No blocks renumbered!\n");
    }

    // Now update the BlockSet epoch, which depends on the block numbers.
    // If any blocks have been renumbered then create a new BlockSet epoch.
    // Even if we have not renumbered any blocks, we might still need to force
    // a new BlockSet epoch, for one of several reasons. If there are any new
    // blocks with higher numbers than the former maximum numbered block, then we
    // need a new epoch with a new size matching the new largest numbered block.
    // Also, if the number of blocks is different from the last time we set the
    // BlockSet epoch, then we need a new epoch. This wouldn't happen if we
    // renumbered blocks after every block addition/deletion, but it might be
    // the case that we can change the number of blocks, then set the BlockSet
    // epoch without renumbering, then change the number of blocks again, then
    // renumber.
    if (renumbered || newMaxBBNum)
    {
        NewBasicBlockEpoch();

        // The key in the unique switch successor map is dependent on the block number, so invalidate that cache.
        InvalidateUniqueSwitchSuccMap();
    }
    else
    {
        EnsureBasicBlockEpoch();
    }

    // Tell our caller if any blocks actually were renumbered.
    return renumbered || newMaxBBNum;
}

/*****************************************************************************
 *
 *  Is the BasicBlock bJump a forward branch?
 *   Optionally bSrc can be supplied to indicate that
 *   bJump must be forward with respect to bSrc
 */
bool Compiler::fgIsForwardBranch(BasicBlock* bJump, BasicBlock* bDest, BasicBlock* bSrc /* = NULL */)
{
    assert((bJump->KindIs(BBJ_ALWAYS, BBJ_CALLFINALLYRET) && bJump->TargetIs(bDest)) ||
           (bJump->KindIs(BBJ_COND) && bJump->TrueTargetIs(bDest)));

    bool        result = false;
    BasicBlock* bTemp  = (bSrc == nullptr) ? bJump : bSrc;

    while (true)
    {
        bTemp = bTemp->Next();

        if (bTemp == nullptr)
        {
            break;
        }

        if (bTemp == bDest)
        {
            result = true;
            break;
        }
    }

    return result;
}

/*****************************************************************************
 *
 *  Returns true if it is allowable (based upon the EH regions)
 *  to place block bAfter immediately after bBefore. It is allowable
 *  if the 'bBefore' and 'bAfter' blocks are in the exact same EH region.
 */

bool Compiler::fgEhAllowsMoveBlock(BasicBlock* bBefore, BasicBlock* bAfter)
{
    return BasicBlock::sameEHRegion(bBefore, bAfter);
}

/*****************************************************************************
 *
 *  Function called to move the range of blocks [bStart .. bEnd].
 *  The blocks are placed immediately after the insertAfterBlk.
 *  fgFirstFuncletBB is not updated; that is the responsibility of the caller, if necessary.
 */

void Compiler::fgMoveBlocksAfter(BasicBlock* bStart, BasicBlock* bEnd, BasicBlock* insertAfterBlk)
{
    /* We have decided to insert the block(s) after 'insertAfterBlk' */
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (verbose)
    {
        printf("Relocated block%s [" FMT_BB ".." FMT_BB "] inserted after " FMT_BB "%s\n", (bStart == bEnd) ? "" : "s",
               bStart->bbNum, bEnd->bbNum, insertAfterBlk->bbNum,
               insertAfterBlk->IsLast() ? " at the end of method" : "");
    }
#endif // DEBUG

    /* relink [bStart .. bEnd] into the flow graph */

    /* If insertAfterBlk was fgLastBB then update fgLastBB */
    if (insertAfterBlk == fgLastBB)
    {
        fgLastBB = bEnd;
        fgLastBB->SetNextToNull();
    }
    else
    {
        bEnd->SetNext(insertAfterBlk->Next());
    }

    insertAfterBlk->SetNext(bStart);
}

/*****************************************************************************
 *
 *  Function called to relocate a single range to the end of the method.
 *  Only an entire consecutive region can be moved and it will be kept together.
 *  Except for the first block, the range cannot have any blocks that jump into or out of the region.
 *  When successful we return the bLast block which is the last block that we relocated.
 *  When unsuccessful we return NULL.

    =============================================================
    NOTE: This function can invalidate all pointers into the EH table, as well as change the size of the EH table!
    =============================================================
 */

BasicBlock* Compiler::fgRelocateEHRange(unsigned regionIndex, FG_RELOCATE_TYPE relocateType)
{
    INDEBUG(const char* reason = "None";)

    // Figure out the range of blocks we're going to move

    unsigned    XTnum;
    EHblkDsc*   HBtab;
    BasicBlock* bStart  = nullptr;
    BasicBlock* bMiddle = nullptr;
    BasicBlock* bLast   = nullptr;
    BasicBlock* bPrev   = nullptr;

#if defined(FEATURE_EH_FUNCLETS)
    // We don't support moving try regions... yet?
    noway_assert(relocateType == FG_RELOCATE_HANDLER);
#endif // FEATURE_EH_FUNCLETS

    HBtab = ehGetDsc(regionIndex);

    if (relocateType == FG_RELOCATE_TRY)
    {
        bStart = HBtab->ebdTryBeg;
        bLast  = HBtab->ebdTryLast;
    }
    else if (relocateType == FG_RELOCATE_HANDLER)
    {
        if (HBtab->HasFilter())
        {
            // The filter and handler funclets must be moved together, and remain contiguous.
            bStart  = HBtab->ebdFilter;
            bMiddle = HBtab->ebdHndBeg;
            bLast   = HBtab->ebdHndLast;
        }
        else
        {
            bStart = HBtab->ebdHndBeg;
            bLast  = HBtab->ebdHndLast;
        }
    }

    // Our range must contain either all rarely run blocks or all non-rarely run blocks
    bool inTheRange = false;
    bool validRange = false;

    BasicBlock* block;

    noway_assert(bStart != nullptr && bLast != nullptr);
    if (bStart == fgFirstBB)
    {
        INDEBUG(reason = "can not relocate first block";)
        goto FAILURE;
    }

#if !defined(FEATURE_EH_FUNCLETS)
    // In the funclets case, we still need to set some information on the handler blocks
    if (bLast->IsLast())
    {
        INDEBUG(reason = "region is already at the end of the method";)
        goto FAILURE;
    }
#endif // !FEATURE_EH_FUNCLETS

    // Walk the block list for this purpose:
    // 1. Verify that all the blocks in the range are either all rarely run or not rarely run.
    // When creating funclets, we ignore the run rarely flag, as we need to be able to move any blocks
    // in the range.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if !defined(FEATURE_EH_FUNCLETS)
    bool isRare;
    isRare = bStart->isRunRarely();
#endif // !FEATURE_EH_FUNCLETS
    block = fgFirstBB;
    while (true)
    {
        if (block == bStart)
        {
            noway_assert(inTheRange == false);
            inTheRange = true;
        }
        else if (bLast->NextIs(block))
        {
            noway_assert(inTheRange == true);
            inTheRange = false;
            break; // we found the end, so we're done
        }

        if (inTheRange)
        {
#if !defined(FEATURE_EH_FUNCLETS)
            // Unless all blocks are (not) run rarely we must return false.
            if (isRare != block->isRunRarely())
            {
                INDEBUG(reason = "this region contains both rarely run and non-rarely run blocks";)
                goto FAILURE;
            }
#endif // !FEATURE_EH_FUNCLETS

            validRange = true;
        }

        if (block == nullptr)
        {
            break;
        }

        block = block->Next();
    }
    // Ensure that bStart .. bLast defined a valid range
    noway_assert((validRange == true) && (inTheRange == false));

    bPrev = bStart->Prev();
    noway_assert(bPrev != nullptr); // Can't move a range that includes the first block of the function.

    JITDUMP("Relocating %s range " FMT_BB ".." FMT_BB " (EH#%u) to end of BBlist\n",
            (relocateType == FG_RELOCATE_TRY) ? "try" : "handler", bStart->bbNum, bLast->bbNum, regionIndex);

#ifdef DEBUG
    if (verbose)
    {
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }

#if !defined(FEATURE_EH_FUNCLETS)

    // This is really expensive, and quickly becomes O(n^n) with funclets
    // so only do it once after we've created them (see fgCreateFunclets)
    if (expensiveDebugCheckLevel >= 2)
    {
        fgDebugCheckBBlist();
    }
#endif

#endif // DEBUG

#if defined(FEATURE_EH_FUNCLETS)

    bStart->SetFlags(BBF_FUNCLET_BEG); // Mark the start block of the funclet

    if (bMiddle != nullptr)
    {
        bMiddle->SetFlags(BBF_FUNCLET_BEG); // Also mark the start block of a filter handler as a funclet
    }

#endif // FEATURE_EH_FUNCLETS

    BasicBlock* bNext;
    bNext = bLast->Next();

    /* Temporarily unlink [bStart .. bLast] from the flow graph */
    fgUnlinkRange(bStart, bLast);

    BasicBlock* insertAfterBlk;
    insertAfterBlk = fgLastBB;

#if defined(FEATURE_EH_FUNCLETS)

    // There are several cases we need to consider when moving an EH range.
    // If moving a range X, we must consider its relationship to every other EH
    // range A in the table. Note that each entry in the table represents both
    // a protected region and a handler region (possibly including a filter region
    // that must live before and adjacent to the handler region), so we must
    // consider try and handler regions independently. These are the cases:
    // 1. A is completely contained within X (where "completely contained" means
    //    that the 'begin' and 'last' parts of A are strictly between the 'begin'
    //    and 'end' parts of X, and aren't equal to either, for example, they don't
    //    share 'last' blocks). In this case, when we move X, A moves with it, and
    //    the EH table doesn't need to change.
    // 2. X is completely contained within A. In this case, X gets extracted from A,
    //    and the range of A shrinks, but because A is strictly within X, the EH
    //    table doesn't need to change.
    // 3. A and X have exactly the same range. In this case, A is moving with X and
    //    the EH table doesn't need to change.
    // 4. A and X share the 'last' block. There are two sub-cases:
    //    (a) A is a larger range than X (such that the beginning of A precedes the
    //        beginning of X): in this case, we are moving the tail of A. We set the
    //        'last' block of A to the block preceding the beginning block of X.
    //    (b) A is a smaller range than X. Thus, we are moving the entirety of A along
    //        with X. In this case, nothing in the EH record for A needs to change.
    // 5. A and X share the 'beginning' block (but aren't the same range, as in #3).
    //    This can never happen here, because we are only moving handler ranges (we don't
    //    move try ranges), and handler regions cannot start at the beginning of a try
    //    range or handler range and be a subset.
    //
    // Note that A and X must properly nest for the table to be well-formed. For example,
    // the beginning of A can't be strictly within the range of X (that is, the beginning
    // of A isn't shared with the beginning of X) and the end of A outside the range.

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        if (XTnum != regionIndex) // we don't need to update our 'last' pointer
        {
            if (HBtab->ebdTryLast == bLast)
            {
                // If we moved a set of blocks that were at the end of
                // a different try region then we may need to update ebdTryLast
                for (block = HBtab->ebdTryBeg; block != nullptr; block = block->Next())
                {
                    if (block == bPrev)
                    {
                        // We were contained within it, so shrink its region by
                        // setting its 'last'
                        fgSetTryEnd(HBtab, bPrev);
                        break;
                    }
                    else if (HBtab->ebdTryLast->NextIs(block))
                    {
                        // bPrev does not come after the TryBeg, thus we are larger, and
                        // it is moving with us.
                        break;
                    }
                }
            }
            if (HBtab->ebdHndLast == bLast)
            {
                // If we moved a set of blocks that were at the end of
                // a different handler region then we must update ebdHndLast
                for (block = HBtab->ebdHndBeg; block != nullptr; block = block->Next())
                {
                    if (block == bPrev)
                    {
                        fgSetHndEnd(HBtab, bPrev);
                        break;
                    }
                    else if (HBtab->ebdHndLast->NextIs(block))
                    {
                        // bPrev does not come after the HndBeg
                        break;
                    }
                }
            }
        }
    } // end exception table iteration

    // Insert the block(s) we are moving after fgLastBlock
    fgMoveBlocksAfter(bStart, bLast, insertAfterBlk);

    if (fgFirstFuncletBB == nullptr) // The funclet region isn't set yet
    {
        fgFirstFuncletBB = bStart;
    }
    else
    {
        assert(fgFirstFuncletBB !=
               insertAfterBlk->Next()); // We insert at the end, not at the beginning, of the funclet region.
    }

    // These asserts assume we aren't moving try regions (which we might need to do). Only
    // try regions can have fall through into or out of the region.

    noway_assert(!bPrev->bbFallsThrough()); // There can be no fall through into a filter or handler region
    noway_assert(!bLast->bbFallsThrough()); // There can be no fall through out of a handler region

#ifdef DEBUG
    if (verbose)
    {
        printf("Create funclets: moved region\n");
        fgDispHandlerTab();
    }

// We have to wait to do this until we've created all the additional regions
// Because this relies on ebdEnclosingTryIndex and ebdEnclosingHndIndex
#endif // DEBUG

#else // !FEATURE_EH_FUNCLETS

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        if (XTnum == regionIndex)
        {
            // Don't update our handler's Last info
            continue;
        }

        if (HBtab->ebdTryLast == bLast)
        {
            // If we moved a set of blocks that were at the end of
            // a different try region then we may need to update ebdTryLast
            for (block = HBtab->ebdTryBeg; block != NULL; block = block->Next())
            {
                if (block == bPrev)
                {
                    fgSetTryEnd(HBtab, bPrev);
                    break;
                }
                else if (HBtab->ebdTryLast->NextIs(block))
                {
                    // bPrev does not come after the TryBeg
                    break;
                }
            }
        }
        if (HBtab->ebdHndLast == bLast)
        {
            // If we moved a set of blocks that were at the end of
            // a different handler region then we must update ebdHndLast
            for (block = HBtab->ebdHndBeg; block != NULL; block = block->Next())
            {
                if (block == bPrev)
                {
                    fgSetHndEnd(HBtab, bPrev);
                    break;
                }
                else if (HBtab->ebdHndLast->NextIs(block))
                {
                    // bPrev does not come after the HndBeg
                    break;
                }
            }
        }
    } // end exception table iteration

    // We have decided to insert the block(s) after fgLastBlock
    fgMoveBlocksAfter(bStart, bLast, insertAfterBlk);

    if (bPrev->KindIs(BBJ_ALWAYS) && bPrev->JumpsToNext())
    {
        bPrev->SetFlags(BBF_NONE_QUIRK);
    }

    if (bLast->KindIs(BBJ_ALWAYS) && bLast->JumpsToNext())
    {
        bLast->SetFlags(BBF_NONE_QUIRK);
    }

#endif // !FEATURE_EH_FUNCLETS

    goto DONE;

FAILURE:

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** Failed fgRelocateEHRange(" FMT_BB ".." FMT_BB ") because %s\n", bStart->bbNum,
               bLast->bbNum, reason);
    }
#endif // DEBUG

    bLast = nullptr;

DONE:

    return bLast;
}

/*****************************************************************************
 *
 * Insert a BasicBlock before the given block.
 */

BasicBlock* Compiler::fgNewBBbefore(BBKinds     jumpKind,
                                    BasicBlock* block,
                                    bool        extendRegion)
{
    // Create a new BasicBlock and chain it in

    BasicBlock* newBlk = BasicBlock::New(this, jumpKind);
    newBlk->SetFlags(BBF_INTERNAL);

    fgInsertBBbefore(block, newBlk);

    newBlk->bbRefs = 0;

    if (newBlk->bbFallsThrough() && block->isRunRarely())
    {
        newBlk->bbSetRunRarely();
    }

    if (extendRegion)
    {
        fgExtendEHRegionBefore(block);
    }
    else
    {
        // When extendRegion is false the caller is responsible for setting these two values
        newBlk->setTryIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
        newBlk->setHndIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
    }

    // We assume that if the block we are inserting before is in the cold region, then this new
    // block will also be in the cold region.
    newBlk->CopyFlags(block, BBF_COLD);

    return newBlk;
}

/*****************************************************************************
 *
 * Insert a BasicBlock after the given block.
 */

BasicBlock* Compiler::fgNewBBafter(BBKinds     jumpKind,
                                   BasicBlock* block,
                                   bool        extendRegion)
{
    // Create a new BasicBlock and chain it in

    BasicBlock* newBlk = BasicBlock::New(this, jumpKind);
    newBlk->SetFlags(BBF_INTERNAL);

    fgInsertBBafter(block, newBlk);

    newBlk->bbRefs = 0;

    if (block->bbFallsThrough() && block->isRunRarely())
    {
        newBlk->bbSetRunRarely();
    }

    if (extendRegion)
    {
        fgExtendEHRegionAfter(block);
    }
    else
    {
        // When extendRegion is false the caller is responsible for setting these two values
        newBlk->setTryIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
        newBlk->setHndIndex(MAX_XCPTN_INDEX); // Note: this is still a legal index, just unlikely
    }

    // If the new block is in the cold region (because the block we are inserting after
    // is in the cold region), mark it as such.
    newBlk->CopyFlags(block, BBF_COLD);

    return newBlk;
}

//------------------------------------------------------------------------
// fgNewBBFromTreeAfter: Create a basic block from the given tree and insert it
//    after the specified block.
//
// Arguments:
//    jumpKind          - jump kind for the new block.
//    block             - insertion point.
//    tree              - tree that will be wrapped into a statement and
//                        inserted in the new block.
//    debugInfo         - debug info to propagate into the new statement.
//    updateSideEffects - update side effects for the whole statement.
//
// Return Value:
//    The new block
//
// Notes:
//    The new block will have BBF_INTERNAL flag and EH region will be extended
//
BasicBlock* Compiler::fgNewBBFromTreeAfter(BBKinds     jumpKind,
                                           BasicBlock* block,
                                           GenTree*    tree,
                                           DebugInfo&  debugInfo,
                                           bool        updateSideEffects /* = false */)
{
    BasicBlock* newBlock = fgNewBBafter(jumpKind, block, true);
    newBlock->SetFlags(BBF_INTERNAL);
    Statement* stmt = fgNewStmtFromTree(tree, debugInfo);
    fgInsertStmtAtEnd(newBlock, stmt);
    newBlock->bbCodeOffs    = block->bbCodeOffsEnd;
    newBlock->bbCodeOffsEnd = block->bbCodeOffsEnd;
    if (updateSideEffects)
    {
        gtUpdateStmtSideEffects(stmt);
    }
    return newBlock;
}

/*****************************************************************************
 *  Inserts basic block before existing basic block.
 *
 *  If insertBeforeBlk is in the funclet region, then newBlk will be in the funclet region.
 *  (If insertBeforeBlk is the first block of the funclet region, then 'newBlk' will be the
 *  new first block of the funclet region.)
 */
void Compiler::fgInsertBBbefore(BasicBlock* insertBeforeBlk, BasicBlock* newBlk)
{
    if (insertBeforeBlk->IsFirst())
    {
        newBlk->SetNext(fgFirstBB);

        fgFirstBB = newBlk;
        assert(fgFirstBB->IsFirst());
    }
    else
    {
        fgInsertBBafter(insertBeforeBlk->Prev(), newBlk);
    }

#if defined(FEATURE_EH_FUNCLETS)

    /* Update fgFirstFuncletBB if insertBeforeBlk is the first block of the funclet region. */

    if (fgFirstFuncletBB == insertBeforeBlk)
    {
        fgFirstFuncletBB = newBlk;
    }

#endif // FEATURE_EH_FUNCLETS
}

/*****************************************************************************
 *  Inserts basic block after existing basic block.
 *
 *  If insertBeforeBlk is in the funclet region, then newBlk will be in the funclet region.
 *  (It can't be used to insert a block as the first block of the funclet region).
 */
void Compiler::fgInsertBBafter(BasicBlock* insertAfterBlk, BasicBlock* newBlk)
{
    if (fgLastBB == insertAfterBlk)
    {
        fgLastBB = newBlk;
        fgLastBB->SetNextToNull();
    }
    else
    {
        newBlk->SetNext(insertAfterBlk->Next());
    }

    insertAfterBlk->SetNext(newBlk);
}

//------------------------------------------------------------------------
// Finds the block closest to endBlk in the range [startBlk..endBlk) after which a block can be
// inserted easily. Note that endBlk cannot be returned; its predecessor is the last block that can
// be returned. The new block will be put in an EH region described by the arguments regionIndex,
// putInTryRegion, startBlk, and endBlk (explained below), so it must be legal to place to put the
// new block after the insertion location block, give it the specified EH region index, and not break
// EH nesting rules. This function is careful to choose a block in the correct EH region. However,
// it assumes that the new block can ALWAYS be placed at the end (just before endBlk). That means
// that the caller must ensure that is true.
//
// Below are the possible cases for the arguments to this method:
//      1. putInTryRegion == true and regionIndex > 0:
//         Search in the try region indicated by regionIndex.
//      2. putInTryRegion == false and regionIndex > 0:
//         a. If startBlk is the first block of a filter and endBlk is the block after the end of the
//            filter (that is, the startBlk and endBlk match a filter bounds exactly), then choose a
//            location within this filter region. (Note that, due to IL rules, filters do not have any
//            EH nested within them.) Otherwise, filters are skipped.
//         b. Else, search in the handler region indicated by regionIndex.
//      3. regionIndex = 0:
//         Search in the entire main method, excluding all EH regions. In this case, putInTryRegion must be true.
//
// This method makes sure to find an insertion point which would not cause the inserted block to
// be put inside any inner try/filter/handler regions.
//
// The actual insertion occurs after the returned block. Note that the returned insertion point might
// be the last block of a more nested EH region, because the new block will be inserted after the insertion
// point, and will not extend the more nested EH region. For example:
//
//      try3   try2   try1
//      |---   |      |      BB01
//      |      |---   |      BB02
//      |      |      |---   BB03
//      |      |      |      BB04
//      |      |---   |---   BB05
//      |                    BB06
//      |-----------------   BB07
//
// for regionIndex==try3, putInTryRegion==true, we might return BB05, even though BB05 will have a try index
// for try1 (the most nested 'try' region the block is in). That's because when we insert after BB05, the new
// block will be in the correct, desired EH region, since try1 and try2 regions will not be extended to include
// the inserted block. Furthermore, for regionIndex==try2, putInTryRegion==true, we can also return BB05. In this
// case, when the new block is inserted, the try1 region remains the same, but we need extend region 'try2' to
// include the inserted block. (We also need to check all parent regions as well, just in case any parent regions
// also end on the same block, in which case we would also need to extend the parent regions. This is standard
// procedure when inserting a block at the end of an EH region.)
//
// If nearBlk is non-nullptr then we return the closest block after nearBlk that will work best.
//
// We try to find a block in the appropriate region that is not a fallthrough block, so we can insert after it
// without the need to insert a jump around the inserted block.
//
// Note that regionIndex is numbered the same as BasicBlock::bbTryIndex and BasicBlock::bbHndIndex, that is, "0" is
// "main method" and otherwise is +1 from normal, so we can call, e.g., ehGetDsc(tryIndex - 1).
//
// Arguments:
//    regionIndex - the region index where the new block will be inserted. Zero means entire method;
//          non-zero means either a "try" or a "handler" region, depending on what putInTryRegion says.
//    putInTryRegion - 'true' to put the block in the 'try' region corresponding to 'regionIndex', 'false'
//          to put the block in the handler region. Should be 'true' if regionIndex==0.
//    startBlk - start block of range to search.
//    endBlk - end block of range to search (don't include this block in the range). Can be nullptr to indicate
//          the end of the function.
//    nearBlk - If non-nullptr, try to find an insertion location closely after this block. If nullptr, we insert
//          at the best location found towards the end of the acceptable block range.
//    jumpBlk - When nearBlk is set, this can be set to the block which jumps to bNext->bbNext (TODO: need to review
//    this?)
//    runRarely - true if the block being inserted is expected to be rarely run. This helps determine
//          the best place to put the new block, by putting in a place that has the same 'rarely run' characteristic.
//
// Return Value:
//    A block with the desired characteristics, so the new block will be inserted after this one.
//    If there is no suitable location, return nullptr. This should basically never happen.
//
BasicBlock* Compiler::fgFindInsertPoint(unsigned    regionIndex,
                                        bool        putInTryRegion,
                                        BasicBlock* startBlk,
                                        BasicBlock* endBlk,
                                        BasicBlock* nearBlk,
                                        BasicBlock* jumpBlk,
                                        bool        runRarely)
{
    noway_assert(startBlk != nullptr);
    noway_assert(startBlk != endBlk);
    noway_assert((regionIndex == 0 && putInTryRegion) || // Search in the main method
                 (putInTryRegion && regionIndex > 0 &&
                  startBlk->bbTryIndex == regionIndex) || // Search in the specified try     region
                 (!putInTryRegion && regionIndex > 0 &&
                  startBlk->bbHndIndex == regionIndex)); // Search in the specified handler region

#ifdef DEBUG
    // Assert that startBlk precedes endBlk in the block list.
    // We don't want to use bbNum to assert this condition, as we cannot depend on the block numbers being
    // sequential at all times.
    for (BasicBlock* b = startBlk; b != endBlk; b = b->Next())
    {
        assert(b != nullptr); // We reached the end of the block list, but never found endBlk.
    }
#endif // DEBUG

    JITDUMP("fgFindInsertPoint(regionIndex=%u, putInTryRegion=%s, startBlk=" FMT_BB ", endBlk=" FMT_BB
            ", nearBlk=" FMT_BB ", jumpBlk=" FMT_BB ", runRarely=%s)\n",
            regionIndex, dspBool(putInTryRegion), startBlk->bbNum, (endBlk == nullptr) ? 0 : endBlk->bbNum,
            (nearBlk == nullptr) ? 0 : nearBlk->bbNum, (jumpBlk == nullptr) ? 0 : jumpBlk->bbNum, dspBool(runRarely));

    bool insertingIntoFilter = false;
    if (!putInTryRegion)
    {
        EHblkDsc* const dsc = ehGetDsc(regionIndex - 1);
        insertingIntoFilter = dsc->HasFilter() && (startBlk == dsc->ebdFilter) && (endBlk == dsc->ebdHndBeg);
    }

    bool        reachedNear = false; // Have we reached 'nearBlk' in our search? If not, we'll keep searching.
    bool        inFilter    = false; // Are we in a filter region that we need to skip?
    BasicBlock* bestBlk =
        nullptr; // Set to the best insertion point we've found so far that meets all the EH requirements.
    BasicBlock* goodBlk =
        nullptr; // Set to an acceptable insertion point that we'll use if we don't find a 'best' option.
    BasicBlock* blk;

    if (nearBlk != nullptr)
    {
        // Does the nearBlk precede the startBlk?
        for (blk = nearBlk; blk != nullptr; blk = blk->Next())
        {
            if (blk == startBlk)
            {
                reachedNear = true;
                break;
            }
            else if (blk == endBlk)
            {
                break;
            }
        }
    }

    for (blk = startBlk; blk != endBlk; blk = blk->Next())
    {
        // The only way (blk == nullptr) could be true is if the caller passed an endBlk that preceded startBlk in the
        // block list, or if endBlk isn't in the block list at all. In DEBUG, we'll instead hit the similar
        // well-formedness assert earlier in this function.
        noway_assert(blk != nullptr);

        if (blk == nearBlk)
        {
            reachedNear = true;
        }

        if (blk->bbCatchTyp == BBCT_FILTER)
        {
            // Record the fact that we entered a filter region, so we don't insert into filters...
            // Unless the caller actually wanted the block inserted in this exact filter region.
            if (!insertingIntoFilter || (blk != startBlk))
            {
                inFilter = true;
            }
        }
        else if (blk->bbCatchTyp == BBCT_FILTER_HANDLER)
        {
            // Record the fact that we exited a filter region.
            inFilter = false;
        }

        // Don't insert a block inside this filter region.
        if (inFilter)
        {
            continue;
        }

        // Note that the new block will be inserted AFTER "blk". We check to make sure that doing so
        // would put the block in the correct EH region. We make an assumption here that you can
        // ALWAYS insert the new block before "endBlk" (that is, at the end of the search range)
        // and be in the correct EH region. This is must be guaranteed by the caller (as it is by
        // fgNewBBinRegion(), which passes the search range as an exact EH region block range).
        // Because of this assumption, we only check the EH information for blocks before the last block.
        if (!blk->NextIs(endBlk))
        {
            // We are in the middle of the search range. We can't insert the new block in
            // an inner try or handler region. We can, however, set the insertion
            // point to the last block of an EH try/handler region, if the enclosing
            // region is the region we wish to insert in. (Since multiple regions can
            // end at the same block, we need to search outwards, checking that the
            // block is the last block of every EH region out to the region we want
            // to insert in.) This is especially useful for putting a call-to-finally
            // block on AMD64 immediately after its corresponding 'try' block, so in the
            // common case, we'll just fall through to it. For example:
            //
            //      BB01
            //      BB02 -- first block of try
            //      BB03
            //      BB04 -- last block of try
            //      BB05 -- first block of finally
            //      BB06
            //      BB07 -- last block of handler
            //      BB08
            //
            // Assume there is only one try/finally, so BB01 and BB08 are in the "main function".
            // For AMD64 call-to-finally, we'll want to insert the BBJ_CALLFINALLY in
            // the main function, immediately after BB04. This allows us to do that.

            if (!fgCheckEHCanInsertAfterBlock(blk, regionIndex, putInTryRegion))
            {
                // Can't insert here.
                continue;
            }
        }

        // Look for an insert location. We want blocks that don't end with a fall through.
        // Quirk: Manually check for BBJ_COND fallthrough behavior
        const bool blkFallsThrough =
            blk->bbFallsThrough() && (!blk->KindIs(BBJ_COND) || blk->NextIs(blk->GetFalseTarget()));
        const bool blkJumpsToNext = blk->KindIs(BBJ_ALWAYS) && blk->HasFlag(BBF_NONE_QUIRK) && blk->JumpsToNext();
        if (!blkFallsThrough && !blkJumpsToNext)
        {
            bool updateBestBlk = true; // We will probably update the bestBlk

            // If we already have a best block, see if the 'runRarely' flags influences
            // our choice. If we want a runRarely insertion point, and the existing best
            // block is run rarely but the current block isn't run rarely, then don't
            // update the best block.
            // TODO-CQ: We should also handle the reverse case, where runRarely is false (we
            // want a non-rarely-run block), but bestBlock->isRunRarely() is true. In that
            // case, we should update the block, also. Probably what we want is:
            //    (bestBlk->isRunRarely() != runRarely) && (blk->isRunRarely() == runRarely)
            if ((bestBlk != nullptr) && runRarely && bestBlk->isRunRarely() && !blk->isRunRarely())
            {
                updateBestBlk = false;
            }

            if (updateBestBlk)
            {
                // We found a 'best' insertion location, so save it away.
                bestBlk = blk;

                // If we've reached nearBlk, we've satisfied all the criteria,
                // so we're done.
                if (reachedNear)
                {
                    goto DONE;
                }

                // If we haven't reached nearBlk, keep looking for a 'best' location, just
                // in case we'll find one at or after nearBlk. If no nearBlk was specified,
                // we prefer inserting towards the end of the given range, so keep looking
                // for more acceptable insertion locations.
            }
        }

        // No need to update goodBlk after we have set bestBlk, but we could still find a better
        // bestBlk, so keep looking.
        if (bestBlk != nullptr)
        {
            continue;
        }

        // Set the current block as a "good enough" insertion point, if it meets certain criteria.
        // We'll return this block if we don't find a "best" block in the search range. The block
        // can't be a BBJ_CALLFINALLY of a BBJ_CALLFINALLY/BBJ_CALLFINALLYRET pair (since we don't want
        // to insert anything between these two blocks). Otherwise, we can use it. However,
        // if we'd previously chosen a BBJ_COND block, then we'd prefer the "good" block to be
        // something else. We keep updating it until we've reached the 'nearBlk', to push it as
        // close to endBlk as possible.
        //
        if (!blk->isBBCallFinallyPair())
        {
            if (goodBlk == nullptr)
            {
                goodBlk = blk;
            }
            else if (goodBlk->KindIs(BBJ_COND) || !blk->KindIs(BBJ_COND))
            {
                if ((blk == nearBlk) || !reachedNear)
                {
                    goodBlk = blk;
                }
            }
        }
    }

    // If we didn't find a non-fall_through block, then insert at the last good block.

    if (bestBlk == nullptr)
    {
        bestBlk = goodBlk;
    }

DONE:

#if defined(JIT32_GCENCODER)
    // If we are inserting into a filter and the best block is the end of the filter region, we need to
    // insert after its predecessor instead: the JIT32 GC encoding used by the x86 CLR ABI  states that the
    // terminal block of a filter region is its exit block. If the filter region consists of a single block,
    // a new block cannot be inserted without either splitting the single block before inserting a new block
    // or inserting the new block before the single block and updating the filter description such that the
    // inserted block is marked as the entry block for the filter. Becuase this sort of split can be complex
    // (especially given that it must ensure that the liveness of the exception object is properly tracked),
    // we avoid this situation by never generating single-block filters on x86 (see impPushCatchArgOnStack).
    if (insertingIntoFilter && (bestBlk == endBlk->Prev()))
    {
        assert(bestBlk != startBlk);
        bestBlk = bestBlk->Prev();
    }
#endif // defined(JIT32_GCENCODER)

    return bestBlk;
}

//------------------------------------------------------------------------
// Creates a new BasicBlock and inserts it in a specific EH region, given by 'tryIndex', 'hndIndex', and 'putInFilter'.
//
// If 'putInFilter' it true, then the block is inserted in the filter region given by 'hndIndex'. In this case, tryIndex
// must be a less nested EH region (that is, tryIndex > hndIndex).
//
// Otherwise, the block is inserted in either the try region or the handler region, depending on which one is the inner
// region. In other words, if the try region indicated by tryIndex is nested in the handler region indicated by
// hndIndex,
// then the new BB will be created in the try region. Vice versa.
//
// Note that tryIndex and hndIndex are numbered the same as BasicBlock::bbTryIndex and BasicBlock::bbHndIndex, that is,
// "0" is "main method" and otherwise is +1 from normal, so we can call, e.g., ehGetDsc(tryIndex - 1).
//
// To be more specific, this function will create a new BB in one of the following 5 regions (if putInFilter is false):
// 1. When tryIndex = 0 and hndIndex = 0:
//    The new BB will be created in the method region.
// 2. When tryIndex != 0 and hndIndex = 0:
//    The new BB will be created in the try region indicated by tryIndex.
// 3. When tryIndex == 0 and hndIndex != 0:
//    The new BB will be created in the handler region indicated by hndIndex.
// 4. When tryIndex != 0 and hndIndex != 0 and tryIndex < hndIndex:
//    In this case, the try region is nested inside the handler region. Therefore, the new BB will be created
//    in the try region indicated by tryIndex.
// 5. When tryIndex != 0 and hndIndex != 0 and tryIndex > hndIndex:
//    In this case, the handler region is nested inside the try region. Therefore, the new BB will be created
//    in the handler region indicated by hndIndex.
//
// Note that if tryIndex != 0 and hndIndex != 0 then tryIndex must not be equal to hndIndex (this makes sense because
// if they are equal, you are asking to put the new block in both the try and handler, which is impossible).
//
// The BasicBlock will not be inserted inside an EH region that is more nested than the requested tryIndex/hndIndex
// region (so the function is careful to skip more nested EH regions when searching for a place to put the new block).
//
// This function cannot be used to insert a block as the first block of any region. It always inserts a block after
// an existing block in the given region.
//
// If nearBlk is nullptr, or the block is run rarely, then the new block is assumed to be run rarely.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//    tryIndex - the try region to insert the new block in, described above. This must be a number in the range
//               [0..compHndBBtabCount].
//    hndIndex - the handler region to insert the new block in, described above. This must be a number in the range
//               [0..compHndBBtabCount].
//    nearBlk  - insert the new block closely after this block, if possible. If nullptr, put the new block anywhere
//               in the requested region.
//    putInFilter - put the new block in the filter region given by hndIndex, as described above.
//    runRarely - 'true' if the new block is run rarely.
//    insertAtEnd - 'true' if the block should be inserted at the end of the region. Note: this is currently only
//                  implemented when inserting into the main function (not into any EH region).
//
// Return Value:
//    The new block.

BasicBlock* Compiler::fgNewBBinRegion(BBKinds     jumpKind,
                                      unsigned    tryIndex,
                                      unsigned    hndIndex,
                                      BasicBlock* nearBlk,
                                      bool        putInFilter /* = false */,
                                      bool        runRarely /* = false */,
                                      bool        insertAtEnd /* = false */)
{
    assert(tryIndex <= compHndBBtabCount);
    assert(hndIndex <= compHndBBtabCount);

    /* afterBlk is the block which will precede the newBB */
    BasicBlock* afterBlk;

    // start and end limit for inserting the block
    BasicBlock* startBlk = nullptr;
    BasicBlock* endBlk   = nullptr;

    bool     putInTryRegion = true;
    unsigned regionIndex    = 0;

    // First, figure out which region (the "try" region or the "handler" region) to put the newBB in.
    if ((tryIndex == 0) && (hndIndex == 0))
    {
        assert(!putInFilter);

        endBlk = fgEndBBAfterMainFunction(); // don't put new BB in funclet region

        if (insertAtEnd || (nearBlk == nullptr))
        {
            /* We'll just insert the block at the end of the method, before the funclets */

            afterBlk = fgLastBBInMainFunction();
            goto _FoundAfterBlk;
        }
        else
        {
            // We'll search through the entire method
            startBlk = fgFirstBB;
        }

        noway_assert(regionIndex == 0);
    }
    else
    {
        noway_assert(tryIndex > 0 || hndIndex > 0);
        PREFIX_ASSUME(tryIndex <= compHndBBtabCount);
        PREFIX_ASSUME(hndIndex <= compHndBBtabCount);

        // Decide which region to put in, the "try" region or the "handler" region.
        if (tryIndex == 0)
        {
            noway_assert(hndIndex > 0);
            putInTryRegion = false;
        }
        else if (hndIndex == 0)
        {
            noway_assert(tryIndex > 0);
            noway_assert(putInTryRegion);
            assert(!putInFilter);
        }
        else
        {
            noway_assert(tryIndex > 0 && hndIndex > 0 && tryIndex != hndIndex);
            putInTryRegion = (tryIndex < hndIndex);
        }

        if (putInTryRegion)
        {
            // Try region is the inner region.
            // In other words, try region must be nested inside the handler region.
            noway_assert(hndIndex == 0 || bbInHandlerRegions(hndIndex - 1, ehGetDsc(tryIndex - 1)->ebdTryBeg));
            assert(!putInFilter);
        }
        else
        {
            // Handler region is the inner region.
            // In other words, handler region must be nested inside the try region.
            noway_assert(tryIndex == 0 || bbInTryRegions(tryIndex - 1, ehGetDsc(hndIndex - 1)->ebdHndBeg));
        }

        // Figure out the start and end block range to search for an insertion location. Pick the beginning and
        // ending blocks of the target EH region (the 'endBlk' is one past the last block of the EH region, to make
        // loop iteration easier). Note that, after funclets have been created (for FEATURE_EH_FUNCLETS),
        // this linear block range will not include blocks of handlers for try/handler clauses nested within
        // this EH region, as those blocks have been extracted as funclets. That is ok, though, because we don't
        // want to insert a block in any nested EH region.

        if (putInTryRegion)
        {
            // We will put the newBB in the try region.
            EHblkDsc* ehDsc = ehGetDsc(tryIndex - 1);
            startBlk        = ehDsc->ebdTryBeg;
            endBlk          = ehDsc->ebdTryLast->Next();
            regionIndex     = tryIndex;
        }
        else if (putInFilter)
        {
            // We will put the newBB in the filter region.
            EHblkDsc* ehDsc = ehGetDsc(hndIndex - 1);
            startBlk        = ehDsc->ebdFilter;
            endBlk          = ehDsc->ebdHndBeg;
            regionIndex     = hndIndex;
        }
        else
        {
            // We will put the newBB in the handler region.
            EHblkDsc* ehDsc = ehGetDsc(hndIndex - 1);
            startBlk        = ehDsc->ebdHndBeg;
            endBlk          = ehDsc->ebdHndLast->Next();
            regionIndex     = hndIndex;
        }

        noway_assert(regionIndex > 0);
    }

    // Now find the insertion point.
    afterBlk = fgFindInsertPoint(regionIndex, putInTryRegion, startBlk, endBlk, nearBlk, nullptr, runRarely);

_FoundAfterBlk:;

    /* We have decided to insert the block after 'afterBlk'. */
    noway_assert(afterBlk != nullptr);

    JITDUMP("fgNewBBinRegion(jumpKind=%s, tryIndex=%u, hndIndex=%u, putInFilter=%s, runRarely=%s, insertAtEnd=%s): "
            "inserting after " FMT_BB "\n",
            bbKindNames[jumpKind], tryIndex, hndIndex, dspBool(putInFilter), dspBool(runRarely), dspBool(insertAtEnd),
            afterBlk->bbNum);

    return fgNewBBinRegionWorker(jumpKind, afterBlk, regionIndex, putInTryRegion);
}

//------------------------------------------------------------------------
// Creates a new BasicBlock and inserts it in the same EH region as 'srcBlk'.
//
// See the implementation of fgNewBBinRegion() used by this one for more notes.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//    srcBlk   - insert the new block in the same EH region as this block, and closely after it if possible.
//    runRarely - 'true' if the new block is run rarely.
//    insertAtEnd - 'true' if the block should be inserted at the end of the region. Note: this is currently only
//                  implemented when inserting into the main function (not into any EH region).
//
// Return Value:
//    The new block.

BasicBlock* Compiler::fgNewBBinRegion(BBKinds     jumpKind,
                                      BasicBlock* srcBlk,
                                      bool        runRarely /* = false */,
                                      bool        insertAtEnd /* = false */)
{
    assert(srcBlk != nullptr);

    const unsigned tryIndex    = srcBlk->bbTryIndex;
    const unsigned hndIndex    = srcBlk->bbHndIndex;
    bool           putInFilter = false;

    // Check to see if we need to put the new block in a filter. We do if srcBlk is in a filter.
    // This can only be true if there is a handler index, and the handler region is more nested than the
    // try region (if any). This is because no EH regions can be nested within a filter.
    if (BasicBlock::ehIndexMaybeMoreNested(hndIndex, tryIndex))
    {
        assert(hndIndex != 0); // If hndIndex is more nested, we must be in some handler!
        putInFilter = ehGetDsc(hndIndex - 1)->InFilterRegionBBRange(srcBlk);
    }

    return fgNewBBinRegion(jumpKind, tryIndex, hndIndex, srcBlk, putInFilter, runRarely, insertAtEnd);
}

//------------------------------------------------------------------------
// Creates a new BasicBlock and inserts it at the end of the function.
//
// See the implementation of fgNewBBinRegion() used by this one for more notes.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//
// Return Value:
//    The new block.

BasicBlock* Compiler::fgNewBBinRegion(BBKinds jumpKind)
{
    return fgNewBBinRegion(jumpKind, 0, 0, nullptr, /* putInFilter */ false, /* runRarely */ false,
                           /* insertAtEnd */ true);
}

//------------------------------------------------------------------------
// Creates a new BasicBlock, and inserts it after 'afterBlk'.
//
// The block cannot be inserted into a more nested try/handler region than that specified by 'regionIndex'.
// (It is given exactly 'regionIndex'.) Thus, the parameters must be passed to ensure proper EH nesting
// rules are followed.
//
// Arguments:
//    jumpKind - the jump kind of the new block to create.
//    afterBlk - insert the new block after this one.
//    regionIndex - the block will be put in this EH region.
//    putInTryRegion - If true, put the new block in the 'try' region corresponding to 'regionIndex', and
//          set its handler index to the most nested handler region enclosing that 'try' region.
//          Otherwise, put the block in the handler region specified by 'regionIndex', and set its 'try'
//          index to the most nested 'try' region enclosing that handler region.
//
// Return Value:
//    The new block.

BasicBlock* Compiler::fgNewBBinRegionWorker(BBKinds     jumpKind,
                                            BasicBlock* afterBlk,
                                            unsigned    regionIndex,
                                            bool        putInTryRegion)
{
    /* Insert the new block */
    BasicBlock* afterBlkNext = afterBlk->Next();
    (void)afterBlkNext; // prevent "unused variable" error from GCC
    BasicBlock* newBlk = fgNewBBafter(jumpKind, afterBlk, false);

    if (putInTryRegion)
    {
        noway_assert(regionIndex <= MAX_XCPTN_INDEX);
        newBlk->bbTryIndex = (unsigned short)regionIndex;
        newBlk->bbHndIndex = bbFindInnermostHandlerRegionContainingTryRegion(regionIndex);
    }
    else
    {
        newBlk->bbTryIndex = bbFindInnermostTryRegionContainingHandlerRegion(regionIndex);
        noway_assert(regionIndex <= MAX_XCPTN_INDEX);
        newBlk->bbHndIndex = (unsigned short)regionIndex;
    }

    // We're going to compare for equal try regions (to handle the case of 'mutually protect'
    // regions). We need to save off the current try region, otherwise we might change it
    // before it gets compared later, thereby making future comparisons fail.

    BasicBlock* newTryBeg;
    BasicBlock* newTryLast;
    (void)ehInitTryBlockRange(newBlk, &newTryBeg, &newTryLast);

    unsigned  XTnum;
    EHblkDsc* HBtab;

    for (XTnum = 0, HBtab = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, HBtab++)
    {
        // Is afterBlk at the end of a try region?
        if (HBtab->ebdTryLast == afterBlk)
        {
            noway_assert(newBlk->NextIs(afterBlkNext));

            bool extendTryRegion = false;
            if (newBlk->hasTryIndex())
            {
                // We're adding a block after the last block of some try region. Do
                // we extend the try region to include the block, or not?
                // If the try region is exactly the same as the try region
                // associated with the new block (based on the block's try index,
                // which represents the innermost try the block is a part of), then
                // we extend it.
                // If the try region is a "parent" try region -- an enclosing try region
                // that has the same last block as the new block's try region -- then
                // we also extend. For example:
                //      try { // 1
                //          ...
                //          try { // 2
                //          ...
                //      } /* 2 */ } /* 1 */
                // This example is meant to indicate that both try regions 1 and 2 end at
                // the same block, and we're extending 2. Thus, we must also extend 1. If we
                // only extended 2, we would break proper nesting. (Dev11 bug 137967)

                extendTryRegion = HBtab->ebdIsSameTry(newTryBeg, newTryLast) || bbInTryRegions(XTnum, newBlk);
            }

            // Does newBlk extend this try region?
            if (extendTryRegion)
            {
                // Yes, newBlk extends this try region

                // newBlk is the now the new try last block
                fgSetTryEnd(HBtab, newBlk);
            }
        }

        // Is afterBlk at the end of a handler region?
        if (HBtab->ebdHndLast == afterBlk)
        {
            noway_assert(newBlk->NextIs(afterBlkNext));

            // Does newBlk extend this handler region?
            bool extendHndRegion = false;
            if (newBlk->hasHndIndex())
            {
                // We're adding a block after the last block of some handler region. Do
                // we extend the handler region to include the block, or not?
                // If the handler region is exactly the same as the handler region
                // associated with the new block (based on the block's handler index,
                // which represents the innermost handler the block is a part of), then
                // we extend it.
                // If the handler region is a "parent" handler region -- an enclosing
                // handler region that has the same last block as the new block's handler
                // region -- then we also extend. For example:
                //      catch { // 1
                //          ...
                //          catch { // 2
                //          ...
                //      } /* 2 */ } /* 1 */
                // This example is meant to indicate that both handler regions 1 and 2 end at
                // the same block, and we're extending 2. Thus, we must also extend 1. If we
                // only extended 2, we would break proper nesting. (Dev11 bug 372051)

                extendHndRegion = bbInHandlerRegions(XTnum, newBlk);
            }

            if (extendHndRegion)
            {
                // Yes, newBlk extends this handler region

                // newBlk is now the last block of the handler.
                fgSetHndEnd(HBtab, newBlk);
            }
        }
    }

#ifdef DEBUG
    fgVerifyHandlerTab();
#endif

    return newBlk;
}

//------------------------------------------------------------------------
// fgUseThrowHelperBlocks: Determinate does compiler use throw helper blocks.
//
// Note:
//   For debuggable code, codegen will generate the 'throw' code inline.
// Return Value:
//    true if 'throw' helper block should be created.
bool Compiler::fgUseThrowHelperBlocks()
{
    return !opts.compDbgCode;
}
