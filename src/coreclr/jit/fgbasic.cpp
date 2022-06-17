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
    fgComputePredsDone = false;

    /* We haven't yet computed the bbCheapPreds lists */
    fgCheapPredsValid = false;

    /* We haven't yet computed the edge weight */
    fgEdgeWeightsComputed    = false;
    fgHaveValidEdgeWeights   = false;
    fgSlopUsedInEdgeWeights  = false;
    fgRangeUsedInEdgeWeights = true;
    fgCalledCount            = BB_ZERO_WEIGHT;

    /* We haven't yet computed the dominator sets */
    fgDomsComputed         = false;
    fgReturnBlocksComputed = false;

#ifdef DEBUG
    fgReachabilitySetsValid = false;
#endif // DEBUG

    /* We don't know yet which loops will always execute calls */
    fgLoopCallMarked = false;

    /* Initialize the basic block list */

    fgFirstBB        = nullptr;
    fgLastBB         = nullptr;
    fgFirstColdBlock = nullptr;
    fgEntryBB        = nullptr;
    fgOSREntryBB     = nullptr;

#if defined(FEATURE_EH_FUNCLETS)
    fgFirstFuncletBB  = nullptr;
    fgFuncletsCreated = false;
#endif // FEATURE_EH_FUNCLETS

    fgBBcount = 0;

#ifdef DEBUG
    fgBBcountAtCodegen = 0;
    fgBBOrder          = nullptr;
#endif // DEBUG

    fgBBNumMax        = 0;
    fgEdgeCount       = 0;
    fgDomBBcount      = 0;
    fgBBVarSetsInited = false;
    fgReturnCount     = 0;

    // Initialize BlockSet data.
    fgCurBBEpoch             = 0;
    fgCurBBEpochSize         = 0;
    fgBBSetCountInSizeTUnits = 0;

    genReturnBB    = nullptr;
    genReturnLocal = BAD_VAR_NUM;

    /* We haven't reached the global morphing phase */
    fgGlobalMorph = false;
    fgModified    = false;

#ifdef DEBUG
    fgSafeBasicBlockCreation = true;
#endif // DEBUG

    fgLocalVarLivenessDone = false;

    /* Statement list is not threaded yet */

    fgStmtListThreaded = false;

    // Initialize the logic for adding code. This is used to insert code such
    // as the code that raises an exception when an array range check fails.

    fgAddCodeList = nullptr;
    fgAddCodeModf = false;

    for (int i = 0; i < SCK_COUNT; i++)
    {
        fgExcptnTargetCache[i] = nullptr;
    }

    /* Keep track of the max count of pointer arguments */
    fgPtrArgCntMax = 0;

    /* This global flag is set whenever we remove a statement */
    fgStmtRemoved = false;

    /* This global flag is set whenever we add a throw block for a RngChk */
    fgRngChkThrowAdded = false; /* reset flag for fgIsCodeAdded() */

    /* Keep track of whether or not EH statements have been optimized */
    fgOptimizedFinally = false;

    /* We will record a list of all BBJ_RETURN blocks here */
    fgReturnBlocks = nullptr;

    /* This is set by fgComputeReachability */
    fgEnterBlks = BlockSetOps::UninitVal();

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    fgAlwaysBlks = BlockSetOps::UninitVal();
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

#ifdef DEBUG
    fgEnterBlksSetValid = false;
#endif // DEBUG

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

    if (!compIsForInlining())
    {
        m_promotedStructDeathVars = nullptr;
    }
#ifdef FEATURE_SIMD
    fgPreviousCandidateSIMDFieldAsgStmt = nullptr;
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
    fgClassInstrumentor          = nullptr;
    fgPredListSortVector         = nullptr;
}

/*****************************************************************************
 *
 *  Create a basic block and append it to the current BB list.
 */

BasicBlock* Compiler::fgNewBasicBlock(BBjumpKinds jumpKind)
{
    // This method must not be called after the exception table has been
    // constructed, because it doesn't not provide support for patching
    // the exception table.

    noway_assert(compHndBBtabCount == 0);

    BasicBlock* block;

    /* Allocate the block descriptor */

    block = bbNewBasicBlock(jumpKind);
    noway_assert(block->bbJumpKind == jumpKind);

    /* Append the block to the end of the global basic block list */

    if (fgFirstBB)
    {
        fgLastBB->setNext(block);
    }
    else
    {
        fgFirstBB     = block;
        block->bbPrev = nullptr;
    }

    fgLastBB = block;

    return block;
}

//------------------------------------------------------------------------
// fgEnsureFirstBBisScratch: Ensure that fgFirstBB is a scratch BasicBlock
//
// Returns:
//   Nothing. May allocate a new block and alter the value of fgFirstBB.
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
//   We maintain the invariant that a scratch BB ends with BBJ_NONE or
//   BBJ_ALWAYS, so that when adding independent bits of initialization,
//   callers can generally append to the fgFirstBB block without worring
//   about what code is there already.
//
//   Can be called at any time, and can be called multiple times.
//
void Compiler::fgEnsureFirstBBisScratch()
{
    // Have we already allocated a scratch block?
    if (fgFirstBBisScratch())
    {
        return;
    }

    assert(fgFirstBBScratch == nullptr);

    BasicBlock* block = bbNewBasicBlock(BBJ_NONE);

    if (fgFirstBB != nullptr)
    {
        // If we have profile data the new block will inherit fgFirstBlock's weight
        if (fgFirstBB->hasProfileWeight())
        {
            block->inheritWeight(fgFirstBB);
        }

        // The first block has an implicit ref count which we must
        // remove. Note the ref count could be greater that one, if
        // the first block is not scratch and is targeted by a
        // branch.
        assert(fgFirstBB->bbRefs >= 1);
        fgFirstBB->bbRefs--;

        // The new scratch bb will fall through to the old first bb
        fgAddRefPred(fgFirstBB, block);
        fgInsertBBbefore(fgFirstBB, block);
    }
    else
    {
        noway_assert(fgLastBB == nullptr);
        fgFirstBB = block;
        fgLastBB  = block;
    }

    noway_assert(fgLastBB != nullptr);

    // Set the expected flags
    block->bbFlags |= (BBF_INTERNAL | BBF_IMPORTED);

    // This new first BB has an implicit ref, and no others.
    block->bbRefs = 1;

    fgFirstBBScratch = fgFirstBB;

#ifdef DEBUG
    if (verbose)
    {
        printf("New scratch " FMT_BB "\n", block->bbNum);
    }
#endif
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
        assert(fgFirstBBScratch->bbFlags & BBF_INTERNAL);
        assert(fgFirstBBScratch->countOfInEdges() == 1);

        // Normally, the first scratch block is a fall-through block. However, if the block after it was an empty
        // BBJ_ALWAYS block, it might get removed, and the code that removes it will make the first scratch block
        // a BBJ_ALWAYS block.
        assert(fgFirstBBScratch->KindIs(BBJ_NONE, BBJ_ALWAYS));

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
    noway_assert(oldSwitchBlock->bbJumpKind == BBJ_SWITCH);

    // Walk the switch's jump table, updating the predecessor for each branch.
    for (BasicBlock* const bJump : oldSwitchBlock->SwitchTargets())
    {
        noway_assert(bJump != nullptr);

        // Note that if there are duplicate branch targets in the switch jump table,
        // fgRemoveRefPred()/fgAddRefPred() will do the right thing: the second and
        // subsequent duplicates will simply subtract from and add to the duplicate
        // count (respectively).
        if (bJump->countOfInEdges() > 0)
        {
            //
            // Remove the old edge [oldSwitchBlock => bJump]
            //
            fgRemoveRefPred(bJump, oldSwitchBlock);
        }
        else
        {
            // bJump->countOfInEdges() must not be zero after preds are calculated.
            assert(!fgComputePredsDone);
        }

        //
        // Create the new edge [newSwitchBlock => bJump]
        //
        fgAddRefPred(bJump, newSwitchBlock);
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
// fgReplaceSwitchJumpTarget: update BBJ_SWITCH block  so that all control
//   that previously flowed to oldTarget now flows to newTarget.
//
// Arguments:
//   blockSwitch - block ending in a switch
//   newTarget   - new branch target
//   oldTarget   - old branch target
//
// Notes:
//   Updates the jump table and the cached unique target set (if any).
//   Can be called before or after pred lists are built.
//   If pred lists are built, updates pred lists.
//
void Compiler::fgReplaceSwitchJumpTarget(BasicBlock* blockSwitch, BasicBlock* newTarget, BasicBlock* oldTarget)
{
    noway_assert(blockSwitch != nullptr);
    noway_assert(newTarget != nullptr);
    noway_assert(oldTarget != nullptr);
    noway_assert(blockSwitch->bbJumpKind == BBJ_SWITCH);

    // For the jump targets values that match oldTarget of our BBJ_SWITCH
    // replace predecessor 'blockSwitch' with 'newTarget'
    //

    unsigned     jumpCnt = blockSwitch->bbJumpSwt->bbsCount;
    BasicBlock** jumpTab = blockSwitch->bbJumpSwt->bbsDstTab;

    unsigned i = 0;

    // Walk the switch's jump table looking for blocks to update the preds for
    while (i < jumpCnt)
    {
        if (jumpTab[i] == oldTarget) // We will update when jumpTab[i] matches
        {
            // Remove the old edge [oldTarget from blockSwitch]
            //
            if (fgComputePredsDone)
            {
                fgRemoveAllRefPreds(oldTarget, blockSwitch);
            }

            //
            // Change the jumpTab entry to branch to the new location
            //
            jumpTab[i] = newTarget;

            //
            // Create the new edge [newTarget from blockSwitch]
            //
            flowList* newEdge = nullptr;

            if (fgComputePredsDone)
            {
                newEdge = fgAddRefPred(newTarget, blockSwitch);
            }

            // Now set the correct value of newEdge->flDupCount
            // and replace any other jumps in jumpTab[] that go to oldTarget.
            //
            i++;
            while (i < jumpCnt)
            {
                if (jumpTab[i] == oldTarget)
                {
                    //
                    // We also must update this entry in the jumpTab
                    //
                    jumpTab[i] = newTarget;
                    newTarget->bbRefs++;

                    //
                    // Increment the flDupCount
                    //
                    if (fgComputePredsDone)
                    {
                        newEdge->flDupCount++;
                    }
                }
                i++; // Check the next entry in jumpTab[]
            }

            // Maintain, if necessary, the set of unique targets of "block."
            UpdateSwitchTableTarget(blockSwitch, oldTarget, newTarget);

            return; // We have replaced the jumps to oldTarget with newTarget
        }
        i++; // Check the next entry in jumpTab[] for a match
    }
    noway_assert(!"Did not find oldTarget in jumpTab[]");
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
//    We ignore other block types.
// 2. All branch targets found are updated. If there are multiple ways for a block
//    to reach 'oldTarget' (e.g., multiple arms of a switch), all of them are changed.
// 3. The predecessor lists are not changed.
// 4. If any switch table entry was updated, the switch table "unique successor" cache is invalidated.
//
// This function is most useful early, before the full predecessor lists have been computed.
//
void Compiler::fgReplaceJumpTarget(BasicBlock* block, BasicBlock* newTarget, BasicBlock* oldTarget)
{
    assert(block != nullptr);

    switch (block->bbJumpKind)
    {
        case BBJ_CALLFINALLY:
        case BBJ_COND:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
        case BBJ_LEAVE: // This function will be called before import, so we still have BBJ_LEAVE

            if (block->bbJumpDest == oldTarget)
            {
                block->bbJumpDest = newTarget;
            }
            break;

        case BBJ_NONE:
        case BBJ_EHFINALLYRET:
        case BBJ_THROW:
        case BBJ_RETURN:
            break;

        case BBJ_SWITCH:
        {
            unsigned const     jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock** const jumpTab = block->bbJumpSwt->bbsDstTab;
            bool               changed = false;

            for (unsigned i = 0; i < jumpCnt; i++)
            {
                if (jumpTab[i] == oldTarget)
                {
                    jumpTab[i] = newTarget;
                    changed    = true;
                }
            }

            if (changed)
            {
                InvalidateUniqueSwitchSuccMap();
            }
            break;
        }

        default:
            assert(!"Block doesn't have a valid bbJumpKind!!!!");
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
// A block can only appear once in the preds list (for normal preds, not
// cheap preds): if a predecessor has multiple ways to get to this block, then
// flDupCount will be >1, but the block will still appear exactly once. Thus, this
// function assumes that all branches from the predecessor (practically, that all
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
    assert(!fgCheapPredsValid);

    bool modified = false;

    for (flowList* const pred : block->PredEdges())
    {
        if (oldPred == pred->getBlock())
        {
            pred->setBlock(newPred);
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

/*****************************************************************************
 *  For a block that is in a handler region, find the first block of the most-nested
 *  handler containing the block.
 */
BasicBlock* Compiler::fgFirstBlockOfHandler(BasicBlock* block)
{
    assert(block->hasHndIndex());
    return ehGetDsc(block->getHndIndex())->ebdHndBeg;
}

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

        while (dsc->bbFlags & BBF_INTERNAL)
        {
            dsc = dsc->bbNext;
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

void Compiler::fgFindJumpTargets(const BYTE* codeAddr, IL_OFFSET codeSize, FixedBitVect* jumpTarget)
{
    const BYTE* codeBegp = codeAddr;
    const BYTE* codeEndp = codeAddr + codeSize;
    unsigned    varNum;
    var_types   varType = DUMMY_INIT(TYP_UNDEF); // TYP_ type
    typeInfo    ti;                              // Verifier type.
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
        compInlineResult->NoteBool(InlineObservation::CALLSITE_HAS_PROFILE, true);
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
        if (isInlining && (impInlineInfo->iciBlock->bbJumpKind == BBJ_THROW))
        {
            compInlineResult->Note(InlineObservation::CALLSITE_IN_NORETURN_REGION);
        }

        // Determine if the call site is in a loop.
        if (isInlining && ((impInlineInfo->iciBlock->bbFlags & BBF_BACKWARD_JUMP) != 0))
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
                            case NI_System_Type_get_IsValueType:
                            case NI_System_Type_get_IsByRefLike:
                            case NI_System_Type_GetTypeFromHandle:
                            case NI_System_String_get_Length:
                            case NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness:
                            case NI_System_Numerics_BitOperations_PopCount:
#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
                            case NI_Vector128_Create:
                            case NI_Vector256_Create:
#elif defined(TARGET_ARM64) && defined(FEATURE_HW_INTRINSICS)
                            case NI_Vector64_Create:
                            case NI_Vector128_Create:
#endif
                            {
                                // Top() in order to keep it as is in case of foldableIntrinsic
                                if (FgStack::IsConstantOrConstArg(pushedStack.Top(), impInlineInfo))
                                {
                                    foldableIntrinsic = true;
                                }
                                break;
                            }

                            // These are foldable if two arguments are constants
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
                            {
                                foldableIntrinsic = true;
                                pushedStack.PushConstant();
                                break;
                            }
#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
                            case NI_Vector128_get_Count:
                            case NI_Vector256_get_Count:
                                foldableIntrinsic = true;
                                pushedStack.PushConstant();
                                // TODO: check if it's a loop condition - we unroll such loops.
                                break;
#elif defined(TARGET_ARM64) && defined(FEATURE_HW_INTRINSICS)
                            case NI_Vector64_get_Count:
                            case NI_Vector128_get_Count:
                                foldableIntrinsic = true;
                                pushedStack.PushConstant();
                                break;
#endif

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

                if (compIsForInlining() && jmpDist == 0 &&
                    (opcode == CEE_LEAVE || opcode == CEE_LEAVE_S || opcode == CEE_BR || opcode == CEE_BR_S))
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
                        ti      = impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt].lclVerTypeInfo;

                        impInlineInfo->lclVarInfo[varNum + impInlineInfo->argCnt].lclHasLdlocaOp = true;
                    }
                    else
                    {
                        noway_assert(opcode == CEE_LDARGA || opcode == CEE_LDARGA_S);

                        varType = impInlineInfo->lclVarInfo[varNum].lclTypeInfo;
                        ti      = impInlineInfo->lclVarInfo[varNum].lclVerTypeInfo;

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
                    ti      = lvaTable[varNum].lvVerTypeInfo;

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

                typeIsNormed = ti.IsValueClass() && !varTypeIsStruct(varType);
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

        if (retBlocks == 0 && isInlining)
        {
            // Mark the call node as "no return" as it can impact caller's code quality.
            impInlineInfo->iciCall->gtCallMoreFlags |= GTF_CALL_M_DOES_NOT_RETURN;
            // Mark root method as containing a noreturn call.
            impInlineRoot()->setMethodHasNoReturnCalls();
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
    // impInlineFetchArg (here args are included as we somtimes get
    // new information about the types of inlinee args).
    if (!isInlining)
    {
        const unsigned firstLcl = info.compArgsCount;
        const unsigned lastLcl  = firstLcl + info.compMethodInfo->locals.numArgs;
        for (unsigned lclNum = firstLcl; lclNum < lastLcl; lclNum++)
        {
            LclVarDsc* lclDsc = lvaGetDesc(lclNum);
            assert(lclDsc->lvSingleDef == 0);
            // could restrict this to TYP_REF
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
//    unmodified in the method.  fgAddInternal is reponsible for
//    adding the code to copy the initial this into the temp.

void Compiler::fgAdjustForAddressExposedOrWrittenThis()
{
    LclVarDsc* thisVarDsc = lvaGetDesc(info.compThisArg);

    // Optionally enable adjustment during stress.
    if (compStressCompile(STRESS_GENERIC_VARN, 15))
    {
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
        arg0varDsc->lvVerTypeInfo  = thisVarDsc->lvVerTypeInfo;

        // Clear the TI_FLAG_THIS_PTR in the original 'this' pointer.
        noway_assert(arg0varDsc->lvVerTypeInfo.IsThisPtr());
        thisVarDsc->lvVerTypeInfo.ClearThisPtr();
        // Note that here we don't clear `m_doNotEnregReason` and it stays
        // `doNotEnreg` with `AddrExposed` reason.
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

#ifdef _PREFAST_
#pragma warning(pop)
#endif

//------------------------------------------------------------------------
// fgMarkBackwardJump: mark blocks indicating there is a jump backwards in
//   IL, from a higher to lower IL offset.
//
// Arguments:
//   targetBlock -- target of the jump
//   sourceBlock -- source of the jump

void Compiler::fgMarkBackwardJump(BasicBlock* targetBlock, BasicBlock* sourceBlock)
{
    noway_assert(targetBlock->bbNum <= sourceBlock->bbNum);

    for (BasicBlock* const block : Blocks(targetBlock, sourceBlock))
    {
        if (((block->bbFlags & BBF_BACKWARD_JUMP) == 0) && (block->bbJumpKind != BBJ_RETURN))
        {
            block->bbFlags |= BBF_BACKWARD_JUMP;
            compHasBackwardJump = true;
        }
    }

    sourceBlock->bbFlags |= BBF_BACKWARD_JUMP_SOURCE;
    targetBlock->bbFlags |= BBF_BACKWARD_JUMP_TARGET;
}

/*****************************************************************************
 *
 *  Finally link up the bbJumpDest of the blocks together
 */

void Compiler::fgLinkBasicBlocks()
{
    /* Create the basic block lookup tables */

    fgInitBBLookup();

    /* First block is always reachable */

    fgFirstBB->bbRefs = 1;

    /* Walk all the basic blocks, filling in the target addresses */

    for (BasicBlock* const curBBdesc : Blocks())
    {
        switch (curBBdesc->bbJumpKind)
        {
            case BBJ_COND:
            case BBJ_ALWAYS:
            case BBJ_LEAVE:
                curBBdesc->bbJumpDest = fgLookupBB(curBBdesc->bbJumpOffs);
                curBBdesc->bbJumpDest->bbRefs++;
                if (curBBdesc->bbJumpDest->bbNum <= curBBdesc->bbNum)
                {
                    fgMarkBackwardJump(curBBdesc->bbJumpDest, curBBdesc);
                }

                /* Is the next block reachable? */

                if (curBBdesc->KindIs(BBJ_ALWAYS, BBJ_LEAVE))
                {
                    break;
                }

                if (!curBBdesc->bbNext)
                {
                    BADCODE("Fall thru the end of a method");
                }

                // Fall through, the next block is also reachable
                FALLTHROUGH;

            case BBJ_NONE:
                curBBdesc->bbNext->bbRefs++;
                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
            case BBJ_THROW:
            case BBJ_RETURN:
                break;

            case BBJ_SWITCH:

                unsigned jumpCnt;
                jumpCnt = curBBdesc->bbJumpSwt->bbsCount;
                BasicBlock** jumpPtr;
                jumpPtr = curBBdesc->bbJumpSwt->bbsDstTab;

                do
                {
                    *jumpPtr = fgLookupBB((unsigned)*(size_t*)jumpPtr);
                    (*jumpPtr)->bbRefs++;
                    if ((*jumpPtr)->bbNum <= curBBdesc->bbNum)
                    {
                        fgMarkBackwardJump(*jumpPtr, curBBdesc);
                    }
                } while (++jumpPtr, --jumpCnt);

                /* Default case of CEE_SWITCH (next block), is at end of jumpTab[] */

                noway_assert(*(jumpPtr - 1) == curBBdesc->bbNext);
                break;

            case BBJ_CALLFINALLY: // BBJ_CALLFINALLY and BBJ_EHCATCHRET don't appear until later
            case BBJ_EHCATCHRET:
            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }
    }
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
        BBjumpKinds jmpKind = BBJ_NONE;

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

                if (compIsForInlining() && jmpDist == 0 && (opcode == CEE_BR || opcode == CEE_BR_S))
                {
                    continue; /* NOP */
                }

                jmpAddr = (IL_OFFSET)(codeAddr - codeBegp) + sz + jmpDist;
                break;

            case CEE_SWITCH:
            {
                unsigned jmpBase;
                unsigned jmpCnt; // # of switch cases (excluding default)

                BasicBlock** jmpTab;
                BasicBlock** jmpPtr;

                /* Allocate the switch descriptor */

                swtDsc = new (this, CMK_BasicBlock) BBswtDesc;

                /* Read the number of entries in the table */

                jmpCnt = getU4LittleEndian(codeAddr);
                codeAddr += 4;

                /* Compute  the base offset for the opcode */

                jmpBase = (IL_OFFSET)((codeAddr - codeBegp) + jmpCnt * sizeof(DWORD));

                /* Allocate the jump table */

                jmpPtr = jmpTab = new (this, CMK_BasicBlock) BasicBlock*[jmpCnt + 1];

                /* Fill in the jump table */

                for (unsigned count = jmpCnt; count; count--)
                {
                    jmpDist = getI4LittleEndian(codeAddr);
                    codeAddr += 4;

                    // store the offset in the pointer.  We change these in fgLinkBasicBlocks().
                    *jmpPtr++ = (BasicBlock*)(size_t)(jmpBase + jmpDist);
                }

                /* Append the default label to the target table */

                *jmpPtr++ = (BasicBlock*)(size_t)jmpBase;

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
                //   (i.e. a prefix opcodes as last intruction in a block)
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

        if (jmpKind == BBJ_NONE)
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
        }

        /* We need to create a new basic block */

        curBBdesc = fgNewBasicBlock(jmpKind);

        curBBdesc->bbFlags |= bbFlags;
        curBBdesc->bbRefs = 0;

        curBBdesc->bbCodeOffs    = curBBoffs;
        curBBdesc->bbCodeOffsEnd = nxtBBoffs;

        switch (jmpKind)
        {
            case BBJ_SWITCH:
                curBBdesc->bbJumpSwt = swtDsc;
                break;

            case BBJ_COND:
            case BBJ_ALWAYS:
            case BBJ_LEAVE:
                noway_assert(jmpAddr != DUMMY_INIT(BAD_IL_OFFSET));
                curBBdesc->bbJumpOffs = jmpAddr;
                break;

            default:
                break;
        }

        DBEXEC(verbose, curBBdesc->dspBlockHeader(this, false, false, false));

        /* Remember where the next BB will start */

        curBBoffs = nxtBBoffs;
    } while (codeAddr < codeEndp);

    noway_assert(codeAddr == codeEndp);

    /* Finally link up the bbJumpDest of the blocks together */

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

    unsigned retBlocks = fgMakeBasicBlocks(info.compCode, info.compILCodeSize, jumpTarget);

    if (compIsForInlining())
    {

#ifdef DEBUG
        // If fgFindJumpTargets marked the call as "no return" there
        // really should be no BBJ_RETURN blocks in the method.
        bool markedNoReturn = (impInlineInfo->iciCall->gtCallMoreFlags & GTF_CALL_M_DOES_NOT_RETURN) != 0;
        assert((markedNoReturn && (retBlocks == 0)) || (!markedNoReturn && (retBlocks >= 1)));
#endif // DEBUG

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
        if ((info.compRetNativeType != TYP_VOID) && ((retBlocks > 1) || impInlineInfo->HasGcRefLocals()))
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

                if (info.compRetType == TYP_REF)
                {
                    // We may have co-opted an existing temp for the return spill.
                    // We likely assumed it was single-def at the time, but now
                    // we can see it has multiple definitions.
                    if ((retBlocks > 1) && (lvaTable[lvaInlineeReturnSpillTemp].lvSingleDef == 1))
                    {
                        // Make sure it is no longer marked single def. This is only safe
                        // to do if we haven't ever updated the type.
                        assert(!lvaTable[lvaInlineeReturnSpillTemp].lvClassInfoUpdated);
                        JITDUMP("Marked return spill temp V%02u as NOT single def temp\n", lvaInlineeReturnSpillTemp);
                        lvaTable[lvaInlineeReturnSpillTemp].lvSingleDef = 0;
                    }
                }
            }
            else
            {
                // The lifetime of this var might expand multiple BBs. So it is a long lifetime compiler temp.
                lvaInlineeReturnSpillTemp = lvaGrabTemp(false DEBUGARG("Inline return value spill temp"));
                lvaTable[lvaInlineeReturnSpillTemp].lvType = info.compRetType;

                // If the method returns a ref class, set the class of the spill temp
                // to the method's return value. We may update this later if it turns
                // out we can prove the method returns a more specific type.
                if (info.compRetType == TYP_REF)
                {
                    // The return spill temp is single def only if the method has a single return block.
                    if (retBlocks == 1)
                    {
                        lvaTable[lvaInlineeReturnSpillTemp].lvSingleDef = 1;
                        JITDUMP("Marked return spill temp V%02u as a single def temp\n", lvaInlineeReturnSpillTemp);
                    }

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

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
        // This will change the block weight from 0 to 1
        // and clear the rarely run flag
        hndBegBB->makeBlockHot();
#else
        hndBegBB->bbSetRunRarely();   // handler entry points are rarely executed
#endif

        if (hndEndOff < info.compILCodeSize)
        {
            hndEndBB = fgLookupBB(hndEndOff);
        }

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filtBB = HBtab->ebdFilter = fgLookupBB(clause.FilterOffset);
            filtBB->bbCatchTyp        = BBCT_FILTER;
            hndBegBB->bbCatchTyp      = BBCT_FILTER_HANDLER;

#if HANDLER_ENTRY_MUST_BE_IN_HOT_SECTION
            // This will change the block weight from 0 to 1
            // and clear the rarely run flag
            filtBB->makeBlockHot();
#else
            filtBB->bbSetRunRarely(); // filter entry points are rarely executed
#endif

            // Mark all BBs that belong to the filter with the XTnum of the corresponding handler
            for (block = filtBB; /**/; block = block->bbNext)
            {
                if (block == nullptr)
                {
                    BADCODE3("Missing endfilter for filter", " at offset %04X", filtBB->bbCodeOffs);
                    return;
                }

                // Still inside the filter
                block->setHndIndex(XTnum);

                if (block->bbJumpKind == BBJ_EHFILTERRET)
                {
                    // Mark catch handler as successor.
                    block->bbJumpDest = hndBegBB;
                    assert(block->bbJumpDest->bbCatchTyp == BBCT_FILTER_HANDLER);
                    break;
                }
            }

            if (!block->bbNext || block->bbNext != hndBegBB)
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

        /* Mark the initial block and last blocks in the 'try' region */

        tryBegBB->bbFlags |= BBF_TRY_BEG;

        /*  Prevent future optimizations of removing the first block   */
        /*  of a TRY block and the first block of an exception handler */

        tryBegBB->bbFlags |= BBF_DONT_REMOVE;
        hndBegBB->bbFlags |= BBF_DONT_REMOVE;
        hndBegBB->bbRefs++; // The first block of a handler gets an extra, "artificial" reference count.

        if (clause.Flags & CORINFO_EH_CLAUSE_FILTER)
        {
            filtBB->bbFlags |= BBF_DONT_REMOVE;
            filtBB->bbRefs++; // The first block of a filter gets an extra, "artificial" reference count.
        }

        tryBegBB->bbFlags |= BBF_DONT_REMOVE;
        hndBegBB->bbFlags |= BBF_DONT_REMOVE;

        //
        // Store the info to the table of EH block handlers
        //

        HBtab->ebdHandlerType = ToEHHandlerType(clause.Flags);

        HBtab->ebdTryBeg  = tryBegBB;
        HBtab->ebdTryLast = (tryEndBB == nullptr) ? fgLastBB : tryEndBB->bbPrev;

        HBtab->ebdHndBeg  = hndBegBB;
        HBtab->ebdHndLast = (hndEndBB == nullptr) ? fgLastBB : hndEndBB->bbPrev;

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

        for (block = hndBegBB; block && (block->bbCodeOffs < hndEndOff); block = block->bbNext)
        {
            if (!block->hasHndIndex())
            {
                block->setHndIndex(XTnum);
            }

            // All blocks in a catch handler or filter are rarely run, except the entry
            if ((block != hndBegBB) && (hndBegBB->bbCatchTyp != BBCT_FINALLY))
            {
                block->bbSetRunRarely();
            }
        }

        /* Mark all blocks within the covered range of the try */

        for (block = tryBegBB; block && (block->bbCodeOffs < tryEndOff); block = block->bbNext)
        {
            /* Mark this BB as belonging to a 'try' block */

            if (!block->hasTryIndex())
            {
                block->setTryIndex(XTnum);
            }

#ifdef DEBUG
            /* Note: the BB can't span the 'try' block */

            if (!(block->bbFlags & BBF_INTERNAL))
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

    // Walk blocks in handlers and filters, looing for a backedge target.
    //
    assert(!compHasBackwardJumpInHandler);
    for (BasicBlock* const blk : Blocks())
    {
        if (blk->hasHndIndex())
        {
            if (blk->bbFlags & BBF_BACKWARD_JUMP_TARGET)
            {
                JITDUMP("\nHander block " FMT_BB "is backward jump target; can't have patchpoints in this method\n",
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
void Compiler::fgFixEntryFlowForOSR()
{
    // Ensure lookup IL->BB lookup table is valid
    //
    fgInitBBLookup();

    // Remember the original entry block in case this method is tail recursive.
    //
    fgEntryBB = fgLookupBB(0);

    // Find the OSR entry block.
    //
    assert(info.compILEntry >= 0);
    BasicBlock* const osrEntry = fgLookupBB(info.compILEntry);

    // Remember the OSR entry block so we can find it again later.
    //
    fgOSREntryBB = osrEntry;

    // Now branch from method start to the right spot.
    //
    fgEnsureFirstBBisScratch();
    fgFirstBB->bbJumpKind = BBJ_ALWAYS;
    fgFirstBB->bbJumpDest = osrEntry;
    fgAddRefPred(osrEntry, fgFirstBB);

    JITDUMP("OSR: redirecting flow at entry from entry " FMT_BB " to OSR entry " FMT_BB " for the importer\n",
            fgFirstBB->bbNum, osrEntry->bbNum);
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
        if (blk->bbFlags & BBF_INTERNAL)
        {
            continue;
        }

        switch (blk->bbJumpKind)
        {
            case BBJ_NONE: // block flows into the next one (no jump)

                fgControlFlowPermitted(blk, blk->bbNext);

                break;

            case BBJ_ALWAYS: // block does unconditional jump to target

                fgControlFlowPermitted(blk, blk->bbJumpDest);

                break;

            case BBJ_COND: // block conditionally jumps to the target

                fgControlFlowPermitted(blk, blk->bbNext);

                fgControlFlowPermitted(blk, blk->bbJumpDest);

                break;

            case BBJ_RETURN: // block ends with 'ret'

                if (blk->hasTryIndex() || blk->hasHndIndex())
                {
                    BADCODE3("Return from a protected block", ". Before offset %04X", blk->bbCodeOffsEnd);
                }
                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:

                if (!blk->hasHndIndex()) // must be part of a handler
                {
                    BADCODE3("Missing handler", ". Before offset %04X", blk->bbCodeOffsEnd);
                }

                HBtab = ehGetDsc(blk->getHndIndex());

                // Endfilter allowed only in a filter block
                if (blk->bbJumpKind == BBJ_EHFILTERRET)
                {
                    if (!HBtab->HasFilter())
                    {
                        BADCODE("Unexpected endfilter");
                    }
                }
                // endfinally allowed only in a finally/fault block
                else if (!HBtab->HasFinallyOrFaultHandler())
                {
                    BADCODE("Unexpected endfinally");
                }

                // The handler block should be the innermost block
                // Exception blocks are listed, innermost first.
                if (blk->hasTryIndex() && (blk->getTryIndex() < blk->getHndIndex()))
                {
                    BADCODE("endfinally / endfilter in nested try block");
                }

                break;

            case BBJ_THROW: // block ends with 'throw'
                /* throw is permitted from every BB, so nothing to check */
                /* importer makes sure that rethrow is done from a catch */
                break;

            case BBJ_LEAVE: // block always jumps to the target, maybe out of guarded
                            // region. Used temporarily until importing
                fgControlFlowPermitted(blk, blk->bbJumpDest, true);

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
                noway_assert(!"Unexpected bbJumpKind"); // these blocks don't get created until importing
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
    BasicBlock* newBlock = bbNewBasicBlock(curr->bbJumpKind);

    // Start the new block with no refs. When we set the preds below, this will get updated correctly.
    newBlock->bbRefs = 0;

    // For each successor of the original block, set the new block as their predecessor.
    // Note we are using the "rational" version of the successor iterator that does not hide the finallyret arcs.
    // Without these arcs, a block 'b' may not be a member of succs(preds(b))
    if (curr->bbJumpKind != BBJ_SWITCH)
    {
        for (BasicBlock* const succ : curr->Succs(this))
        {
            if (succ != newBlock)
            {
                JITDUMP(FMT_BB " previous predecessor was " FMT_BB ", now is " FMT_BB "\n", succ->bbNum, curr->bbNum,
                        newBlock->bbNum);
                fgReplacePred(succ, curr, newBlock);
            }
        }

        newBlock->bbJumpDest = curr->bbJumpDest;
        curr->bbJumpDest     = nullptr;
    }
    else
    {
        // In the case of a switch statement there's more complicated logic in order to wire up the predecessor lists
        // but fortunately there's an existing method that implements this functionality.
        newBlock->bbJumpSwt = curr->bbJumpSwt;

        fgChangeSwitchBlock(curr, newBlock);

        curr->bbJumpSwt = nullptr;
    }

    newBlock->inheritWeight(curr);

    // Set the new block's flags. Note that the new block isn't BBF_INTERNAL unless the old block is.
    newBlock->bbFlags = curr->bbFlags;

    // Remove flags that the new block can't have.
    newBlock->bbFlags &=
        ~(BBF_TRY_BEG | BBF_LOOP_HEAD | BBF_LOOP_CALL0 | BBF_LOOP_CALL1 | BBF_FUNCLET_BEG | BBF_LOOP_PREHEADER |
          BBF_KEEP_BBJ_ALWAYS | BBF_PATCHPOINT | BBF_BACKWARD_JUMP_TARGET | BBF_LOOP_ALIGN);

    // Remove the GC safe bit on the new block. It seems clear that if we split 'curr' at the end,
    // such that all the code is left in 'curr', and 'newBlock' just gets the control flow, then
    // both 'curr' and 'newBlock' could accurately retain an existing GC safe bit. However, callers
    // use this function to split blocks in the middle, or at the beginning, and they don't seem to
    // be careful about updating this flag appropriately. So, removing the GC safe bit is simply
    // conservative: some functions might end up being fully interruptible that could be partially
    // interruptible if we exercised more care here.
    newBlock->bbFlags &= ~BBF_GC_SAFE_POINT;

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    newBlock->bbFlags &= ~(BBF_FINALLY_TARGET);
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

    // The new block has no code, so we leave bbCodeOffs/bbCodeOffsEnd set to BAD_IL_OFFSET. If a caller
    // puts code in the block, then it needs to update these.

    // Insert the new block in the block list after the 'curr' block.
    fgInsertBBafter(curr, newBlock);
    fgExtendEHRegionAfter(curr); // The new block is in the same EH region as the old block.

    // Remove flags from the old block that are no longer possible.
    curr->bbFlags &= ~(BBF_HAS_JMP | BBF_RETLESS_CALL);

    // Default to fallthru, and add the arc for that.
    curr->bbJumpKind = BBJ_NONE;
    fgAddRefPred(newBlock, curr);

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

        curr->bbCodeOffsEnd  = splitPointILOffset;
        newBlock->bbCodeOffs = splitPointILOffset;
    }
    else
    {
        assert(curr->bbStmtList == nullptr); // if no tree was given then it better be an empty block
    }

    return newBlock;
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

        curr->bbCodeOffsEnd = splitPointILOffset;

        // Also use this as the beginning offset of the next block. Presumably we could/should
        // look to see if the first node is a GT_IL_OFFSET node, and use that instead.
        newBlock->bbCodeOffs = splitPointILOffset;
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
//    'curr' must have a bbJumpKind of BBJ_COND, BBJ_ALWAYS, or BBJ_SWITCH
//
// Notes:
//    The returned block is empty.
//    Can be invoked before pred lists are built.

BasicBlock* Compiler::fgSplitEdge(BasicBlock* curr, BasicBlock* succ)
{
    assert(curr->KindIs(BBJ_COND, BBJ_SWITCH, BBJ_ALWAYS));

    if (fgComputePredsDone)
    {
        assert(fgGetPredForBlock(succ, curr) != nullptr);
    }

    BasicBlock* newBlock;
    if (succ == curr->bbNext)
    {
        // The successor is the fall-through path of a BBJ_COND, or
        // an immediately following block of a BBJ_SWITCH (which has
        // no fall-through path). For this case, simply insert a new
        // fall-through block after 'curr'.
        newBlock = fgNewBBafter(BBJ_NONE, curr, true /*extendRegion*/);
    }
    else
    {
        newBlock = fgNewBBinRegion(BBJ_ALWAYS, curr, curr->isRunRarely());
        // The new block always jumps to 'succ'
        newBlock->bbJumpDest = succ;
    }
    newBlock->bbFlags |= (curr->bbFlags & succ->bbFlags & (BBF_BACKWARD_JUMP));

    JITDUMP("Splitting edge from " FMT_BB " to " FMT_BB "; adding " FMT_BB "\n", curr->bbNum, succ->bbNum,
            newBlock->bbNum);

    if (curr->bbJumpKind == BBJ_COND)
    {
        fgReplacePred(succ, curr, newBlock);
        if (curr->bbJumpDest == succ)
        {
            // Now 'curr' jumps to newBlock
            curr->bbJumpDest = newBlock;
        }
        fgAddRefPred(newBlock, curr);
    }
    else if (curr->bbJumpKind == BBJ_SWITCH)
    {
        // newBlock replaces 'succ' in the switch.
        fgReplaceSwitchJumpTarget(curr, newBlock, succ);

        // And 'succ' has 'newBlock' as a new predecessor.
        fgAddRefPred(succ, newBlock);
    }
    else
    {
        assert(curr->bbJumpKind == BBJ_ALWAYS);
        fgReplacePred(succ, curr, newBlock);
        curr->bbJumpDest = newBlock;
        fgAddRefPred(newBlock, curr);
    }

    // This isn't accurate, but it is complex to compute a reasonable number so just assume that we take the
    // branch 50% of the time.
    //
    if (curr->bbJumpKind != BBJ_ALWAYS)
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
    if (block->bbPrev)
    {
        block->bbPrev->bbNext = block->bbNext;
        if (block->bbNext)
        {
            block->bbNext->bbPrev = block->bbPrev;
        }
        else
        {
            fgLastBB = block->bbPrev;
        }
    }
    else
    {
        assert(block == fgFirstBB);
        assert(block != fgLastBB);
        assert((fgFirstBBScratch == nullptr) || (fgFirstBBScratch == fgFirstBB));

        fgFirstBB         = block->bbNext;
        fgFirstBB->bbPrev = nullptr;

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

    BasicBlock* bPrev = bBeg->bbPrev;
    assert(bPrev != nullptr); // Can't unlink a range starting with the first block

    bPrev->setNext(bEnd->bbNext);

    /* If we removed the last block in the method then update fgLastBB */
    if (fgLastBB == bEnd)
    {
        fgLastBB = bPrev;
        noway_assert(fgLastBB->bbNext == nullptr);
    }

    // If bEnd was the first Cold basic block update fgFirstColdBlock
    if (fgFirstColdBlock == bEnd)
    {
        fgFirstColdBlock = bPrev->bbNext;
    }

#if defined(FEATURE_EH_FUNCLETS)
#ifdef DEBUG
    // You can't unlink a range that includes the first funclet block. A range certainly
    // can't cross the non-funclet/funclet region. And you can't unlink the first block
    // of the first funclet with this, either. (If that's necessary, it could be allowed
    // by updating fgFirstFuncletBB to bEnd->bbNext.)
    for (BasicBlock* tempBB = bBeg; tempBB != bEnd->bbNext; tempBB = tempBB->bbNext)
    {
        assert(tempBB != fgFirstFuncletBB);
    }
#endif // DEBUG
#endif // FEATURE_EH_FUNCLETS
}

/*****************************************************************************************************
 *
 *  Function called to remove a basic block
 */

void Compiler::fgRemoveBlock(BasicBlock* block, bool unreachable)
{
    /* The block has to be either unreachable or empty */

    PREFIX_ASSUME(block != nullptr);

    BasicBlock* bPrev = block->bbPrev;

    JITDUMP("fgRemoveBlock " FMT_BB ", unreachable=%s\n", block->bbNum, dspBool(unreachable));

    // If we've cached any mappings from switch blocks to SwitchDesc's (which contain only the
    // *unique* successors of the switch block), invalidate that cache, since an entry in one of
    // the SwitchDescs might be removed.
    InvalidateUniqueSwitchSuccMap();

    noway_assert((block == fgFirstBB) || (bPrev && (bPrev->bbNext == block)));
    noway_assert(!(block->bbFlags & BBF_DONT_REMOVE));

    // Should never remove a genReturnBB, as we might have special hookups there.
    noway_assert(block != genReturnBB);

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    // Don't remove a finally target
    assert(!(block->bbFlags & BBF_FINALLY_TARGET));
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)

    if (unreachable)
    {
        PREFIX_ASSUME(bPrev != nullptr);

        fgUnreachableBlock(block);

#if defined(FEATURE_EH_FUNCLETS)
        // If block was the fgFirstFuncletBB then set fgFirstFuncletBB to block->bbNext
        if (block == fgFirstFuncletBB)
        {
            fgFirstFuncletBB = block->bbNext;
        }
#endif // FEATURE_EH_FUNCLETS

        if (bPrev->bbJumpKind == BBJ_CALLFINALLY)
        {
            // bPrev CALL becomes RETLESS as the BBJ_ALWAYS block is unreachable
            bPrev->bbFlags |= BBF_RETLESS_CALL;

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
            NO_WAY("No retless call finally blocks; need unwind target instead");
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
        }
        else if (bPrev->bbJumpKind == BBJ_ALWAYS && bPrev->bbJumpDest == block->bbNext &&
                 !(bPrev->bbFlags & BBF_KEEP_BBJ_ALWAYS) && (block != fgFirstColdBlock) &&
                 (block->bbNext != fgFirstColdBlock))
        {
            // previous block is a BBJ_ALWAYS to the next block: change to BBJ_NONE.
            // Note that we don't do it if bPrev follows a BBJ_CALLFINALLY block (BBF_KEEP_BBJ_ALWAYS),
            // because that would violate our invariant that BBJ_CALLFINALLY blocks are followed by
            // BBJ_ALWAYS blocks.
            bPrev->bbJumpKind = BBJ_NONE;
        }

        // If this is the first Cold basic block update fgFirstColdBlock
        if (block == fgFirstColdBlock)
        {
            fgFirstColdBlock = block->bbNext;
        }

        /* Unlink this block from the bbNext chain */
        fgUnlinkBlock(block);

        /* At this point the bbPreds and bbRefs had better be zero */
        noway_assert((block->bbRefs == 0) && (block->bbPreds == nullptr));

        /*  A BBJ_CALLFINALLY is usually paired with a BBJ_ALWAYS.
         *  If we delete such a BBJ_CALLFINALLY we also delete the BBJ_ALWAYS
         */
        if (block->isBBCallAlwaysPair())
        {
            BasicBlock* leaveBlk = block->bbNext;
            noway_assert(leaveBlk->bbJumpKind == BBJ_ALWAYS);

            leaveBlk->bbFlags &= ~BBF_DONT_REMOVE;
            leaveBlk->bbRefs  = 0;
            leaveBlk->bbPreds = nullptr;

            fgRemoveBlock(leaveBlk, /* unreachable */ true);

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
            fgClearFinallyTargetBit(leaveBlk->bbJumpDest);
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
        }
        else if (block->bbJumpKind == BBJ_RETURN)
        {
            fgRemoveReturnBlock(block);
        }
    }
    else // block is empty
    {
        noway_assert(block->isEmpty());

        // The block cannot follow a non-retless BBJ_CALLFINALLY (because we don't know who may jump to it).
        noway_assert(!block->isBBCallAlwaysPairTail());

        /* This cannot be the last basic block */
        noway_assert(block != fgLastBB);

#ifdef DEBUG
        if (verbose)
        {
            printf("Removing empty " FMT_BB "\n", block->bbNum);
        }
#endif // DEBUG

#ifdef DEBUG
        /* Some extra checks for the empty case */

        switch (block->bbJumpKind)
        {
            case BBJ_NONE:
                break;

            case BBJ_ALWAYS:
                /* Do not remove a block that jumps to itself - used for while (true){} */
                noway_assert(block->bbJumpDest != block);

                /* Empty GOTO can be removed iff bPrev is BBJ_NONE */
                noway_assert(bPrev && bPrev->bbJumpKind == BBJ_NONE);
                break;

            default:
                noway_assert(!"Empty block of this type cannot be removed!");
                break;
        }
#endif // DEBUG

        noway_assert(block->KindIs(BBJ_NONE, BBJ_ALWAYS));

        /* Who is the "real" successor of this block? */

        BasicBlock* succBlock;

        if (block->bbJumpKind == BBJ_ALWAYS)
        {
            succBlock = block->bbJumpDest;
        }
        else
        {
            succBlock = block->bbNext;
        }

        bool skipUnmarkLoop = false;

        // If block is the backedge for a loop and succBlock precedes block
        // then the succBlock becomes the new LOOP HEAD
        // NOTE: there's an assumption here that the blocks are numbered in increasing bbNext order.
        // NOTE 2: if fgDomsComputed is false, then we can't check reachability. However, if this is
        // the case, then the loop structures probably are also invalid, and shouldn't be used. This
        // can be the case late in compilation (such as Lower), where remnants of earlier created
        // structures exist, but haven't been maintained.
        if (block->isLoopHead() && (succBlock->bbNum <= block->bbNum))
        {
            succBlock->bbFlags |= BBF_LOOP_HEAD;

            if (block->isLoopAlign())
            {
                loopAlignCandidates++;
                succBlock->bbFlags |= BBF_LOOP_ALIGN;
                JITDUMP("Propagating LOOP_ALIGN flag from " FMT_BB " to " FMT_BB " for " FMT_LP "\n ", block->bbNum,
                        succBlock->bbNum, block->bbNatLoopNum);
            }

            if (fgDomsComputed && fgReachable(succBlock, block))
            {
                // Mark all the reachable blocks between 'succBlock' and 'bPrev'
                optScaleLoopBlocks(succBlock, bPrev);
            }
        }
        else if (succBlock->isLoopHead() && bPrev && (succBlock->bbNum <= bPrev->bbNum))
        {
            skipUnmarkLoop = true;
        }

        // If this is the first Cold basic block update fgFirstColdBlock
        if (block == fgFirstColdBlock)
        {
            fgFirstColdBlock = block->bbNext;
        }

#if defined(FEATURE_EH_FUNCLETS)
        // Update fgFirstFuncletBB if necessary
        if (block == fgFirstFuncletBB)
        {
            fgFirstFuncletBB = block->bbNext;
        }
#endif // FEATURE_EH_FUNCLETS

        /* First update the loop table and bbWeights */
        optUpdateLoopsBeforeRemoveBlock(block, skipUnmarkLoop);

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

            /* Must be a fall through to next block */

            noway_assert(block->bbJumpKind == BBJ_NONE);

            /* old block no longer gets the extra ref count for being the first block */
            block->bbRefs--;
            succBlock->bbRefs++;
        }

        /* Update bbRefs and bbPreds.
         * All blocks jumping to 'block' now jump to 'succBlock'.
         * First, remove 'block' from the predecessor list of succBlock.
         */

        fgRemoveRefPred(succBlock, block);

        for (flowList* const pred : block->PredEdges())
        {
            BasicBlock* predBlock = pred->getBlock();

            /* Are we changing a loop backedge into a forward jump? */

            if (block->isLoopHead() && (predBlock->bbNum >= block->bbNum) && (predBlock->bbNum <= succBlock->bbNum))
            {
                /* First update the loop table and bbWeights */
                optUpdateLoopsBeforeRemoveBlock(predBlock);
            }

            /* If predBlock is a new predecessor, then add it to succBlock's
               predecessor's list. */
            if (predBlock->bbJumpKind != BBJ_SWITCH)
            {
                // Even if the pred is not a switch, we could have a conditional branch
                // to the fallthrough, so duplicate there could be preds
                for (unsigned i = 0; i < pred->flDupCount; i++)
                {
                    fgAddRefPred(succBlock, predBlock);
                }
            }

            /* change all jumps to the removed block */
            switch (predBlock->bbJumpKind)
            {
                default:
                    noway_assert(!"Unexpected bbJumpKind in fgRemoveBlock()");
                    break;

                case BBJ_NONE:
                    noway_assert(predBlock == bPrev);
                    PREFIX_ASSUME(bPrev != nullptr);

                    /* In the case of BBJ_ALWAYS we have to change the type of its predecessor */
                    if (block->bbJumpKind == BBJ_ALWAYS)
                    {
                        /* bPrev now becomes a BBJ_ALWAYS */
                        bPrev->bbJumpKind = BBJ_ALWAYS;
                        bPrev->bbJumpDest = succBlock;
                    }
                    break;

                case BBJ_COND:
                    /* The links for the direct predecessor case have already been updated above */
                    if (predBlock->bbJumpDest != block)
                    {
                        break;
                    }

                    /* Check if both side of the BBJ_COND now jump to the same block */
                    if (predBlock->bbNext == succBlock)
                    {
                        // Make sure we are replacing "block" with "succBlock" in predBlock->bbJumpDest.
                        noway_assert(predBlock->bbJumpDest == block);
                        predBlock->bbJumpDest = succBlock;
                        fgRemoveConditionalJump(predBlock);
                        break;
                    }

                    /* Fall through for the jump case */
                    FALLTHROUGH;

                case BBJ_CALLFINALLY:
                case BBJ_ALWAYS:
                case BBJ_EHCATCHRET:
                    noway_assert(predBlock->bbJumpDest == block);
                    predBlock->bbJumpDest = succBlock;
                    break;

                case BBJ_SWITCH:
                    // Change any jumps from 'predBlock' (a BBJ_SWITCH) to 'block' to jump to 'succBlock'
                    //
                    // For the jump targets of 'predBlock' (a BBJ_SWITCH) that jump to 'block'
                    // remove the old predecessor at 'block' from 'predBlock'  and
                    // add the new predecessor at 'succBlock' from 'predBlock'
                    //
                    fgReplaceSwitchJumpTarget(predBlock, succBlock, block);
                    break;
            }
        }

        fgUnlinkBlock(block);
        block->bbFlags |= BBF_REMOVED;
    }

    // If this was marked for alignment, remove it
    block->unmarkLoopAlign(this DEBUG_ARG("Removed block"));

    if (bPrev != nullptr)
    {
        switch (bPrev->bbJumpKind)
        {
            case BBJ_CALLFINALLY:
                // If prev is a BBJ_CALLFINALLY it better be marked as RETLESS
                noway_assert(bPrev->bbFlags & BBF_RETLESS_CALL);
                break;

            case BBJ_ALWAYS:
                // Check for branch to next block. Just make sure the BBJ_ALWAYS block is not
                // part of a BBJ_CALLFINALLY/BBJ_ALWAYS pair. We do this here and don't rely on fgUpdateFlowGraph
                // because we can be called by ComputeDominators and it expects it to remove this jump to
                // the next block. This is the safest fix. We should remove all this BBJ_CALLFINALLY/BBJ_ALWAYS
                // pairing.

                if ((bPrev->bbJumpDest == bPrev->bbNext) &&
                    !fgInDifferentRegions(bPrev, bPrev->bbJumpDest)) // We don't remove a branch from Hot -> Cold
                {
                    if ((bPrev == fgFirstBB) || !bPrev->isBBCallAlwaysPairTail())
                    {
                        // It's safe to change the jump type
                        bPrev->bbJumpKind = BBJ_NONE;
                    }
                }
                break;

            case BBJ_COND:
                /* Check for branch to next block */
                if (bPrev->bbJumpDest == bPrev->bbNext)
                {
                    fgRemoveConditionalJump(bPrev);
                }
                break;

            default:
                break;
        }

        ehUpdateForDeletedBlock(block);
    }
}

/*****************************************************************************
 *
 *  Function called to connect to block that previously had a fall through
 */

BasicBlock* Compiler::fgConnectFallThrough(BasicBlock* bSrc, BasicBlock* bDst)
{
    BasicBlock* jmpBlk = nullptr;

    /* If bSrc is non-NULL */

    if (bSrc != nullptr)
    {
        /* If bSrc falls through to a block that is not bDst, we will insert a jump to bDst */

        if (bSrc->bbFallsThrough() && (bSrc->bbNext != bDst))
        {
            switch (bSrc->bbJumpKind)
            {

                case BBJ_NONE:
                    bSrc->bbJumpKind = BBJ_ALWAYS;
                    bSrc->bbJumpDest = bDst;
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Block " FMT_BB " ended with a BBJ_NONE, Changed to an unconditional jump to " FMT_BB
                               "\n",
                               bSrc->bbNum, bSrc->bbJumpDest->bbNum);
                    }
#endif
                    break;

                case BBJ_CALLFINALLY:
                case BBJ_COND:

                    // Add a new block after bSrc which jumps to 'bDst'
                    jmpBlk = fgNewBBafter(BBJ_ALWAYS, bSrc, true);

                    if (fgComputePredsDone)
                    {
                        fgAddRefPred(jmpBlk, bSrc, fgGetPredForBlock(bDst, bSrc));
                    }
                    // Record the loop number in the new block
                    jmpBlk->bbNatLoopNum = bSrc->bbNatLoopNum;

                    // When adding a new jmpBlk we will set the bbWeight and bbFlags
                    //
                    if (fgHaveValidEdgeWeights && fgHaveProfileData())
                    {
                        noway_assert(fgComputePredsDone);

                        flowList* newEdge = fgGetPredForBlock(jmpBlk, bSrc);

                        jmpBlk->bbWeight = (newEdge->edgeWeightMin() + newEdge->edgeWeightMax()) / 2;
                        if (bSrc->bbWeight == BB_ZERO_WEIGHT)
                        {
                            jmpBlk->bbWeight = BB_ZERO_WEIGHT;
                        }

                        if (jmpBlk->bbWeight == BB_ZERO_WEIGHT)
                        {
                            jmpBlk->bbFlags |= BBF_RUN_RARELY;
                        }

                        weight_t weightDiff = (newEdge->edgeWeightMax() - newEdge->edgeWeightMin());
                        weight_t slop       = BasicBlock::GetSlopFraction(bSrc, bDst);
                        //
                        // If the [min/max] values for our edge weight is within the slop factor
                        //  then we will set the BBF_PROF_WEIGHT flag for the block
                        //
                        if (weightDiff <= slop)
                        {
                            jmpBlk->bbFlags |= BBF_PROF_WEIGHT;
                        }
                    }
                    else
                    {
                        // We set the bbWeight to the smaller of bSrc->bbWeight or bDst->bbWeight
                        if (bSrc->bbWeight < bDst->bbWeight)
                        {
                            jmpBlk->bbWeight = bSrc->bbWeight;
                            jmpBlk->bbFlags |= (bSrc->bbFlags & BBF_RUN_RARELY);
                        }
                        else
                        {
                            jmpBlk->bbWeight = bDst->bbWeight;
                            jmpBlk->bbFlags |= (bDst->bbFlags & BBF_RUN_RARELY);
                        }
                    }

                    jmpBlk->bbJumpDest = bDst;

                    if (fgComputePredsDone)
                    {
                        fgReplacePred(bDst, bSrc, jmpBlk);
                    }
                    else
                    {
                        jmpBlk->bbFlags |= BBF_IMPORTED;
                    }

#ifdef DEBUG
                    if (verbose)
                    {
                        printf("Added an unconditional jump to " FMT_BB " after block " FMT_BB "\n",
                               jmpBlk->bbJumpDest->bbNum, bSrc->bbNum);
                    }
#endif // DEBUG
                    break;

                default:
                    noway_assert(!"Unexpected bbJumpKind");
                    break;
            }
        }
        else
        {
            // If bSrc is an unconditional branch to the next block
            // then change it to a BBJ_NONE block
            //
            if ((bSrc->bbJumpKind == BBJ_ALWAYS) && !(bSrc->bbFlags & BBF_KEEP_BBJ_ALWAYS) &&
                (bSrc->bbJumpDest == bSrc->bbNext))
            {
                bSrc->bbJumpKind = BBJ_NONE;
#ifdef DEBUG
                if (verbose)
                {
                    printf("Changed an unconditional jump from " FMT_BB " to the next block " FMT_BB
                           " into a BBJ_NONE block\n",
                           bSrc->bbNum, bSrc->bbNext->bbNum);
                }
#endif // DEBUG
            }
        }
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
    // If we renumber the blocks the dominator information will be out-of-date
    if (fgDomsComputed)
    {
        noway_assert(!"Can't call Compiler::fgRenumberBlocks() when fgDomsComputed==true");
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** Before renumbering the basic blocks\n");
        fgDispBasicBlocks();
        fgDispHandlerTab();
    }
#endif // DEBUG

    bool        renumbered  = false;
    bool        newMaxBBNum = false;
    BasicBlock* block;

    unsigned numStart = 1 + (compIsForInlining() ? impInlineInfo->InlinerCompiler->fgBBNumMax : 0);
    unsigned num;

    for (block = fgFirstBB, num = numStart; block != nullptr; block = block->bbNext, num++)
    {
        noway_assert((block->bbFlags & BBF_REMOVED) == 0);

        if (block->bbNum != num)
        {
            renumbered = true;
#ifdef DEBUG
            if (verbose)
            {
                printf("Renumber " FMT_BB " to " FMT_BB "\n", block->bbNum, num);
            }
#endif // DEBUG
            block->bbNum = num;
        }

        if (block->bbNext == nullptr)
        {
            fgLastBB  = block;
            fgBBcount = num - numStart + 1;
            if (compIsForInlining())
            {
                if (impInlineInfo->InlinerCompiler->fgBBNumMax != num)
                {
                    impInlineInfo->InlinerCompiler->fgBBNumMax = num;
                    newMaxBBNum                                = true;
                }
            }
            else
            {
                if (fgBBNumMax != num)
                {
                    fgBBNumMax  = num;
                    newMaxBBNum = true;
                }
            }
        }
    }

    // If we renumbered, then we may need to reorder some pred lists.
    //
    if (renumbered && fgComputePredsDone)
    {
        for (BasicBlock* const block : Blocks())
        {
            block->ensurePredListOrder(this);
        }
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\n*************** After renumbering the basic blocks\n");
        if (renumbered)
        {
            fgDispBasicBlocks();
            fgDispHandlerTab();
        }
        else
        {
            printf("=============== No blocks renumbered!\n");
        }
    }
#endif // DEBUG

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
bool Compiler::fgIsForwardBranch(BasicBlock* bJump, BasicBlock* bSrc /* = NULL */)
{
    bool result = false;

    if (bJump->KindIs(BBJ_COND, BBJ_ALWAYS))
    {
        BasicBlock* bDest = bJump->bbJumpDest;
        BasicBlock* bTemp = (bSrc == nullptr) ? bJump : bSrc;

        while (true)
        {
            bTemp = bTemp->bbNext;

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
               (insertAfterBlk->bbNext == nullptr) ? " at the end of method" : "");
    }
#endif // DEBUG

    /* relink [bStart .. bEnd] into the flow graph */

    bEnd->bbNext = insertAfterBlk->bbNext;
    if (insertAfterBlk->bbNext)
    {
        insertAfterBlk->bbNext->bbPrev = bEnd;
    }
    insertAfterBlk->setNext(bStart);

    /* If insertAfterBlk was fgLastBB then update fgLastBB */
    if (insertAfterBlk == fgLastBB)
    {
        fgLastBB = bEnd;
        noway_assert(fgLastBB->bbNext == nullptr);
    }
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
    if (bLast->bbNext == NULL)
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
        else if (block == bLast->bbNext)
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

        block = block->bbNext;
    }
    // Ensure that bStart .. bLast defined a valid range
    noway_assert((validRange == true) && (inTheRange == false));

    bPrev = bStart->bbPrev;
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

    bStart->bbFlags |= BBF_FUNCLET_BEG; // Mark the start block of the funclet

    if (bMiddle != nullptr)
    {
        bMiddle->bbFlags |= BBF_FUNCLET_BEG; // Also mark the start block of a filter handler as a funclet
    }

#endif // FEATURE_EH_FUNCLETS

    BasicBlock* bNext;
    bNext = bLast->bbNext;

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
                for (block = HBtab->ebdTryBeg; block != nullptr; block = block->bbNext)
                {
                    if (block == bPrev)
                    {
                        // We were contained within it, so shrink its region by
                        // setting its 'last'
                        fgSetTryEnd(HBtab, bPrev);
                        break;
                    }
                    else if (block == HBtab->ebdTryLast->bbNext)
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
                for (block = HBtab->ebdHndBeg; block != nullptr; block = block->bbNext)
                {
                    if (block == bPrev)
                    {
                        fgSetHndEnd(HBtab, bPrev);
                        break;
                    }
                    else if (block == HBtab->ebdHndLast->bbNext)
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
               insertAfterBlk->bbNext); // We insert at the end, not at the beginning, of the funclet region.
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
            for (block = HBtab->ebdTryBeg; block != NULL; block = block->bbNext)
            {
                if (block == bPrev)
                {
                    fgSetTryEnd(HBtab, bPrev);
                    break;
                }
                else if (block == HBtab->ebdTryLast->bbNext)
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
            for (block = HBtab->ebdHndBeg; block != NULL; block = block->bbNext)
            {
                if (block == bPrev)
                {
                    fgSetHndEnd(HBtab, bPrev);
                    break;
                }
                else if (block == HBtab->ebdHndLast->bbNext)
                {
                    // bPrev does not come after the HndBeg
                    break;
                }
            }
        }
    } // end exception table iteration

    // We have decided to insert the block(s) after fgLastBlock
    fgMoveBlocksAfter(bStart, bLast, insertAfterBlk);

    // If bPrev falls through, we will insert a jump to block
    fgConnectFallThrough(bPrev, bStart);

    // If bLast falls through, we will insert a jump to bNext
    fgConnectFallThrough(bLast, bNext);

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

//------------------------------------------------------------------------
// fgMightHaveLoop: return true if there is a possibility that the method has a loop (a back edge is present).
// This function doesn't depend on any previous loop computations, including predecessors. It looks for any
// lexical back edge to a block previously seen in a forward walk of the block list.
//
// As it walks all blocks and all successors of each block (including EH successors), it is not cheap.
// It returns as soon as any possible loop is discovered.
//
// Return Value:
//    true if there might be a loop
//
bool Compiler::fgMightHaveLoop()
{
    // Don't use a BlockSet for this temporary bitset of blocks: we don't want to have to call EnsureBasicBlockEpoch()
    // and potentially change the block epoch.

    BitVecTraits blockVecTraits(fgBBNumMax + 1, this);
    BitVec       blocksSeen(BitVecOps::MakeEmpty(&blockVecTraits));

    for (BasicBlock* const block : Blocks())
    {
        BitVecOps::AddElemD(&blockVecTraits, blocksSeen, block->bbNum);

        for (BasicBlock* const succ : block->GetAllSuccs(this))
        {
            if (BitVecOps::IsMember(&blockVecTraits, blocksSeen, succ->bbNum))
            {
                return true;
            }
        }
    }
    return false;
}

/*****************************************************************************
 *
 * Insert a BasicBlock before the given block.
 */

BasicBlock* Compiler::fgNewBBbefore(BBjumpKinds jumpKind, BasicBlock* block, bool extendRegion)
{
    // Create a new BasicBlock and chain it in

    BasicBlock* newBlk = bbNewBasicBlock(jumpKind);
    newBlk->bbFlags |= BBF_INTERNAL;

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
    newBlk->bbFlags |= (block->bbFlags & BBF_COLD);

    return newBlk;
}

/*****************************************************************************
 *
 * Insert a BasicBlock after the given block.
 */

BasicBlock* Compiler::fgNewBBafter(BBjumpKinds jumpKind, BasicBlock* block, bool extendRegion)
{
    // Create a new BasicBlock and chain it in

    BasicBlock* newBlk = bbNewBasicBlock(jumpKind);
    newBlk->bbFlags |= BBF_INTERNAL;

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
    newBlk->bbFlags |= (block->bbFlags & BBF_COLD);

    return newBlk;
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
    if (insertBeforeBlk->bbPrev)
    {
        fgInsertBBafter(insertBeforeBlk->bbPrev, newBlk);
    }
    else
    {
        newBlk->setNext(fgFirstBB);

        fgFirstBB      = newBlk;
        newBlk->bbPrev = nullptr;
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
    newBlk->bbNext = insertAfterBlk->bbNext;

    if (insertAfterBlk->bbNext)
    {
        insertAfterBlk->bbNext->bbPrev = newBlk;
    }

    insertAfterBlk->bbNext = newBlk;
    newBlk->bbPrev         = insertAfterBlk;

    if (fgLastBB == insertAfterBlk)
    {
        fgLastBB = newBlk;
        assert(fgLastBB->bbNext == nullptr);
    }
}

// We have two edges (bAlt => bCur) and (bCur => bNext).
//
// Returns true if the weight of (bAlt => bCur)
//  is greater than the weight of (bCur => bNext).
// We compare the edge weights if we have valid edge weights
//  otherwise we compare blocks weights.
//
bool Compiler::fgIsBetterFallThrough(BasicBlock* bCur, BasicBlock* bAlt)
{
    // bCur can't be NULL and must be a fall through bbJumpKind
    noway_assert(bCur != nullptr);
    noway_assert(bCur->bbFallsThrough());
    noway_assert(bAlt != nullptr);

    // We only handle the cases when bAlt is a BBJ_ALWAYS or a BBJ_COND
    if (!bAlt->KindIs(BBJ_ALWAYS, BBJ_COND))
    {
        return false;
    }

    // if bAlt doesn't jump to bCur it can't be a better fall through than bCur
    if (bAlt->bbJumpDest != bCur)
    {
        return false;
    }

    // Currently bNext is the fall through for bCur
    BasicBlock* bNext = bCur->bbNext;
    noway_assert(bNext != nullptr);

    // We will set result to true if bAlt is a better fall through than bCur
    bool result;
    if (fgHaveValidEdgeWeights)
    {
        // We will compare the edge weight for our two choices
        flowList* edgeFromAlt = fgGetPredForBlock(bCur, bAlt);
        flowList* edgeFromCur = fgGetPredForBlock(bNext, bCur);
        noway_assert(edgeFromCur != nullptr);
        noway_assert(edgeFromAlt != nullptr);

        result = (edgeFromAlt->edgeWeightMin() > edgeFromCur->edgeWeightMax());
    }
    else
    {
        if (bAlt->bbJumpKind == BBJ_ALWAYS)
        {
            // Our result is true if bAlt's weight is more than bCur's weight
            result = (bAlt->bbWeight > bCur->bbWeight);
        }
        else
        {
            noway_assert(bAlt->bbJumpKind == BBJ_COND);
            // Our result is true if bAlt's weight is more than twice bCur's weight
            result = (bAlt->bbWeight > (2 * bCur->bbWeight));
        }
    }
    return result;
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
    for (BasicBlock* b = startBlk; b != endBlk; b = b->bbNext)
    {
        assert(b != nullptr); // We reached the end of the block list, but never found endBlk.
    }
#endif // DEBUG

    JITDUMP("fgFindInsertPoint(regionIndex=%u, putInTryRegion=%s, startBlk=" FMT_BB ", endBlk=" FMT_BB
            ", nearBlk=" FMT_BB ", "
            "jumpBlk=" FMT_BB ", runRarely=%s)\n",
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
        for (blk = nearBlk; blk != nullptr; blk = blk->bbNext)
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

    for (blk = startBlk; blk != endBlk; blk = blk->bbNext)
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
        if (blk->bbNext != endBlk)
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

        // Look for an insert location:
        // 1. We want blocks that don't end with a fall through,
        // 2. Also, when blk equals nearBlk we may want to insert here.
        if (!blk->bbFallsThrough() || (blk == nearBlk))
        {
            bool updateBestBlk = true; // We will probably update the bestBlk

            // If blk falls through then we must decide whether to use the nearBlk
            // hint
            if (blk->bbFallsThrough())
            {
                noway_assert(blk == nearBlk);
                if (jumpBlk != nullptr)
                {
                    updateBestBlk = fgIsBetterFallThrough(blk, jumpBlk);
                }
                else
                {
                    updateBestBlk = false;
                }
            }

            // If we already have a best block, see if the 'runRarely' flags influences
            // our choice. If we want a runRarely insertion point, and the existing best
            // block is run rarely but the current block isn't run rarely, then don't
            // update the best block.
            // TODO-CQ: We should also handle the reverse case, where runRarely is false (we
            // want a non-rarely-run block), but bestBlock->isRunRarely() is true. In that
            // case, we should update the block, also. Probably what we want is:
            //    (bestBlk->isRunRarely() != runRarely) && (blk->isRunRarely() == runRarely)
            if (updateBestBlk && (bestBlk != nullptr) && runRarely && bestBlk->isRunRarely() && !blk->isRunRarely())
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
        // can't be a BBJ_CALLFINALLY of a BBJ_CALLFINALLY/BBJ_ALWAYS pair (since we don't want
        // to insert anything between these two blocks). Otherwise, we can use it. However,
        // if we'd previously chosen a BBJ_COND block, then we'd prefer the "good" block to be
        // something else. We keep updating it until we've reached the 'nearBlk', to push it as
        // close to endBlk as possible.
        if (!blk->isBBCallAlwaysPair())
        {
            if (goodBlk == nullptr)
            {
                goodBlk = blk;
            }
            else if ((goodBlk->bbJumpKind == BBJ_COND) || (blk->bbJumpKind != BBJ_COND))
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
    if (insertingIntoFilter && (bestBlk == endBlk->bbPrev))
    {
        assert(bestBlk != startBlk);
        bestBlk = bestBlk->bbPrev;
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

BasicBlock* Compiler::fgNewBBinRegion(BBjumpKinds jumpKind,
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
            endBlk          = ehDsc->ebdTryLast->bbNext;
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
            endBlk          = ehDsc->ebdHndLast->bbNext;
            regionIndex     = hndIndex;
        }

        noway_assert(regionIndex > 0);
    }

    // Now find the insertion point.
    afterBlk = fgFindInsertPoint(regionIndex, putInTryRegion, startBlk, endBlk, nearBlk, nullptr, runRarely);

_FoundAfterBlk:;

    /* We have decided to insert the block after 'afterBlk'. */
    noway_assert(afterBlk != nullptr);

    JITDUMP("fgNewBBinRegion(jumpKind=%u, tryIndex=%u, hndIndex=%u, putInFilter=%s, runRarely=%s, insertAtEnd=%s): "
            "inserting after " FMT_BB "\n",
            jumpKind, tryIndex, hndIndex, dspBool(putInFilter), dspBool(runRarely), dspBool(insertAtEnd),
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
//
// Return Value:
//    The new block.

BasicBlock* Compiler::fgNewBBinRegion(BBjumpKinds jumpKind,
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

BasicBlock* Compiler::fgNewBBinRegion(BBjumpKinds jumpKind)
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

BasicBlock* Compiler::fgNewBBinRegionWorker(BBjumpKinds jumpKind,
                                            BasicBlock* afterBlk,
                                            unsigned    regionIndex,
                                            bool        putInTryRegion)
{
    /* Insert the new block */
    BasicBlock* afterBlkNext = afterBlk->bbNext;
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
            noway_assert(afterBlkNext == newBlk->bbNext);

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
            noway_assert(afterBlkNext == newBlk->bbNext);

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

    /* If afterBlk falls through, we insert a jump around newBlk */
    fgConnectFallThrough(afterBlk, newBlk->bbNext);

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
