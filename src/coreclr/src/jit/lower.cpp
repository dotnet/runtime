// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               Lower                                       XX
XX                                                                           XX
XX  Preconditions:                                                           XX
XX                                                                           XX
XX  Postconditions (for the nodes currently handled):                        XX
XX    - All operands requiring a register are explicit in the graph          XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#include "lower.h"

#if !defined(_TARGET_64BIT_)
#include "decomposelongs.h"
#endif // !defined(_TARGET_64BIT_)

//------------------------------------------------------------------------
// MakeSrcContained: Make "childNode" a contained node
//
// Arguments:
//    parentNode - is a non-leaf node that can contain its 'childNode'
//    childNode  - is an op that will now be contained by its parent.
//
// Notes:
//    If 'childNode' it has any existing sources, they will now be sources for the parent.
//
void Lowering::MakeSrcContained(GenTreePtr parentNode, GenTreePtr childNode)
{
    assert(!parentNode->OperIsLeaf());
    int srcCount = childNode->gtLsraInfo.srcCount;
    assert(srcCount >= 0);
    m_lsra->clearOperandCounts(childNode);
    assert(parentNode->gtLsraInfo.srcCount > 0);
    parentNode->gtLsraInfo.srcCount += srcCount - 1;
}

//------------------------------------------------------------------------
// CheckImmedAndMakeContained: Checks if the 'childNode' is a containable immediate
//    and, if so, makes it contained.
//
// Arguments:
//    parentNode - is any non-leaf node
//    childNode  - is an child op of 'parentNode'
//
// Return value:
//     true if we are able to make childNode a contained immediate
//
bool Lowering::CheckImmedAndMakeContained(GenTree* parentNode, GenTree* childNode)
{
    assert(!parentNode->OperIsLeaf());
    // If childNode is a containable immediate
    if (IsContainableImmed(parentNode, childNode))
    {
        // then make it contained within the parentNode
        MakeSrcContained(parentNode, childNode);
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// IsSafeToContainMem: Checks for conflicts between childNode and parentNode,
// and returns 'true' iff memory operand childNode can be contained in parentNode.
//
// Arguments:
//    parentNode  - a non-leaf binary node
//    childNode   - a memory op that is a child op of 'parentNode'
//
// Return value:
//    true if it is safe to make childNode a contained memory operand.
//
bool Lowering::IsSafeToContainMem(GenTree* parentNode, GenTree* childNode)
{
    assert(parentNode->OperIsBinary());
    assert(childNode->isMemoryOp());

    unsigned int childFlags = (childNode->gtFlags & GTF_ALL_EFFECT);

    GenTree* node;
    for (node = childNode; node != parentNode; node = node->gtNext)
    {
        assert(node != nullptr);

        if ((childFlags != 0) && node->IsCall())
        {
            bool isPureHelper = (node->gtCall.gtCallType == CT_HELPER) &&
                                comp->s_helperCallProperties.IsPure(comp->eeGetHelperNum(node->gtCall.gtCallMethHnd));
            if (!isPureHelper && ((node->gtFlags & childFlags & GTF_ALL_EFFECT) != 0))
            {
                return false;
            }
        }
        else if (node->OperIsStore() && comp->fgNodesMayInterfere(node, childNode))
        {
            return false;
        }
    }

    return true;
}

//------------------------------------------------------------------------

// This is the main entry point for Lowering.
GenTree* Lowering::LowerNode(GenTree* node)
{
    assert(node != nullptr);
    switch (node->gtOper)
    {
        case GT_IND:
            TryCreateAddrMode(LIR::Use(BlockRange(), &node->gtOp.gtOp1, node), true);
            break;

        case GT_STOREIND:
            LowerStoreInd(node);
            break;

        case GT_ADD:
            return LowerAdd(node);

        case GT_UDIV:
        case GT_UMOD:
            LowerUnsignedDivOrMod(node);
            break;

        case GT_DIV:
        case GT_MOD:
            return LowerSignedDivOrMod(node);

        case GT_SWITCH:
            return LowerSwitch(node);

        case GT_CALL:
            LowerCall(node);
            break;

        case GT_JMP:
            LowerJmpMethod(node);
            break;

        case GT_RETURN:
            LowerRet(node);
            break;

        case GT_CAST:
            LowerCast(node);
            break;

        case GT_ARR_ELEM:
            return LowerArrElem(node);

        case GT_ROL:
        case GT_ROR:
            LowerRotate(node);
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            if (node->TypeGet() == TYP_SIMD12)
            {
                // GT_SIMD node requiring to produce TYP_SIMD12 in fact
                // produces a TYP_SIMD16 result
                node->gtType = TYP_SIMD16;
            }
            break;

        case GT_LCL_VAR:
        case GT_STORE_LCL_VAR:
            if (node->TypeGet() == TYP_SIMD12)
            {
#ifdef _TARGET_64BIT_
                // Assumption 1:
                // RyuJit backend depends on the assumption that on 64-Bit targets Vector3 size is rounded off
                // to TARGET_POINTER_SIZE and hence Vector3 locals on stack can be treated as TYP_SIMD16 for
                // reading and writing purposes.
                //
                // Assumption 2:
                // RyuJit backend is making another implicit assumption that Vector3 type args when passed in
                // registers or on stack, the upper most 4-bytes will be zero.
                //
                // For P/Invoke return and Reverse P/Invoke argument passing, native compiler doesn't guarantee
                // that upper 4-bytes of a Vector3 type struct is zero initialized and hence assumption 2 is
                // invalid.
                //
                // RyuJIT x64 Windows: arguments are treated as passed by ref and hence read/written just 12
                // bytes. In case of Vector3 returns, Caller allocates a zero initialized Vector3 local and
                // passes it retBuf arg and Callee method writes only 12 bytes to retBuf. For this reason,
                // there is no need to clear upper 4-bytes of Vector3 type args.
                //
                // RyuJIT x64 Unix: arguments are treated as passed by value and read/writen as if TYP_SIMD16.
                // Vector3 return values are returned two return registers and Caller assembles them into a
                // single xmm reg. Hence RyuJIT explicitly generates code to clears upper 4-bytes of Vector3
                // type args in prolog and Vector3 type return value of a call
                node->gtType = TYP_SIMD16;
#else
                NYI("Lowering of TYP_SIMD12 locals");
#endif // _TARGET_64BIT_
            }
#endif // FEATURE_SIMD

        default:
            break;
    }

    return node->gtNext;
}

/**  -- Switch Lowering --
 * The main idea of switch lowering is to keep transparency of the register requirements of this node
 * downstream in LSRA.  Given that the switch instruction is inherently a control statement which in the JIT
 * is represented as a simple tree node, at the time we actually generate code for it we end up
 * generating instructions that actually modify the flow of execution that imposes complicated
 * register requirement and lifetimes.
 *
 * So, for the purpose of LSRA, we want to have a more detailed specification of what a switch node actually
 * means and more importantly, which and when do we need a register for each instruction we want to issue
 * to correctly allocate them downstream.
 *
 * For this purpose, this procedure performs switch lowering in two different ways:
 *
 * a) Represent the switch statement as a zero-index jump table construct.  This means that for every destination
 *    of the switch, we will store this destination in an array of addresses and the code generator will issue
 *    a data section where this array will live and will emit code that based on the switch index, will indirect and
 *    jump to the destination specified in the jump table.
 *
 *    For this transformation we introduce a new GT node called GT_SWITCH_TABLE that is a specialization of the switch
 *    node for jump table based switches.
 *    The overall structure of a GT_SWITCH_TABLE is:
 *
 *    GT_SWITCH_TABLE
 *           |_________ localVar   (a temporary local that holds the switch index)
 *           |_________ jumpTable  (this is a special node that holds the address of the jump table array)
 *
 *     Now, the way we morph a GT_SWITCH node into this lowered switch table node form is the following:
 *
 *    Input:     GT_SWITCH (inside a basic block whose Branch Type is BBJ_SWITCH)
 *                    |_____ expr (an arbitrarily complex GT_NODE that represents the switch index)
 *
 *    This gets transformed into the following statements inside a BBJ_COND basic block (the target would be
 *    the default case of the switch in case the conditional is evaluated to true).
 *
 *     ----- original block, transformed
 *     GT_ASG
 *        |_____ tempLocal (a new temporary local variable used to store the switch index)
 *        |_____ expr      (the index expression)
 *
 *     GT_JTRUE
 *        |_____ GT_COND
 *                 |_____ GT_GE
 *                           |___ Int_Constant  (This constant is the index of the default case
 *                                               that happens to be the highest index in the jump table).
 *                           |___ tempLocal     (The local variable were we stored the index expression).
 *
 *     ----- new basic block
 *     GT_SWITCH_TABLE
 *        |_____ tempLocal
 *        |_____ jumpTable (a new jump table node that now LSRA can allocate registers for explicitly
 *                          and LinearCodeGen will be responsible to generate downstream).
 *
 *     This way there are no implicit temporaries.
 *
 * b) For small-sized switches, we will actually morph them into a series of conditionals of the form
 *     if (case falls into the default){ goto jumpTable[size]; // last entry in the jump table is the default case }
 *     (For the default case conditional, we'll be constructing the exact same code as the jump table case one).
 *     else if (case == firstCase){ goto jumpTable[1]; }
 *     else if (case == secondCase) { goto jumptable[2]; } and so on.
 *
 *     This transformation is of course made in JIT-IR, not downstream to CodeGen level, so this way we no longer
 *     require internal temporaries to maintain the index we're evaluating plus we're using existing code from
 *     LinearCodeGen to implement this instead of implement all the control flow constructs using InstrDscs and
 *     InstrGroups downstream.
 */

GenTree* Lowering::LowerSwitch(GenTree* node)
{
    unsigned     jumpCnt;
    unsigned     targetCnt;
    BasicBlock** jumpTab;

    assert(node->gtOper == GT_SWITCH);

    // The first step is to build the default case conditional construct that is
    // shared between both kinds of expansion of the switch node.

    // To avoid confusion, we'll alias m_block to originalSwitchBB
    // that represents the node we're morphing.
    BasicBlock* originalSwitchBB = m_block;
    LIR::Range& switchBBRange    = LIR::AsRange(originalSwitchBB);

    // jumpCnt is the number of elements in the jump table array.
    // jumpTab is the actual pointer to the jump table array.
    // targetCnt is the number of unique targets in the jump table array.
    jumpCnt   = originalSwitchBB->bbJumpSwt->bbsCount;
    jumpTab   = originalSwitchBB->bbJumpSwt->bbsDstTab;
    targetCnt = originalSwitchBB->NumSucc(comp);

// GT_SWITCH must be a top-level node with no use.
#ifdef DEBUG
    {
        LIR::Use use;
        assert(!switchBBRange.TryGetUse(node, &use));
    }
#endif

    JITDUMP("Lowering switch BB%02u, %d cases\n", originalSwitchBB->bbNum, jumpCnt);

    // Handle a degenerate case: if the switch has only a default case, just convert it
    // to an unconditional branch. This should only happen in minopts or with debuggable
    // code.
    if (targetCnt == 1)
    {
        JITDUMP("Lowering switch BB%02u: single target; converting to BBJ_ALWAYS\n", originalSwitchBB->bbNum);
        noway_assert(comp->opts.MinOpts() || comp->opts.compDbgCode);
        if (originalSwitchBB->bbNext == jumpTab[0])
        {
            originalSwitchBB->bbJumpKind = BBJ_NONE;
            originalSwitchBB->bbJumpDest = nullptr;
        }
        else
        {
            originalSwitchBB->bbJumpKind = BBJ_ALWAYS;
            originalSwitchBB->bbJumpDest = jumpTab[0];
        }
        // Remove extra predecessor links if there was more than one case.
        for (unsigned i = 1; i < jumpCnt; ++i)
        {
            (void)comp->fgRemoveRefPred(jumpTab[i], originalSwitchBB);
        }

        // We have to get rid of the GT_SWITCH node but a child might have side effects so just assign
        // the result of the child subtree to a temp.
        GenTree* rhs = node->gtOp.gtOp1;

        unsigned lclNum                 = comp->lvaGrabTemp(true DEBUGARG("Lowering is creating a new local variable"));
        comp->lvaSortAgain              = true;
        comp->lvaTable[lclNum].lvType   = rhs->TypeGet();
        comp->lvaTable[lclNum].lvRefCnt = 1;

        GenTreeLclVar* store =
            new (comp, GT_STORE_LCL_VAR) GenTreeLclVar(GT_STORE_LCL_VAR, rhs->TypeGet(), lclNum, BAD_IL_OFFSET);
        store->gtOp1   = rhs;
        store->gtFlags = (rhs->gtFlags & GTF_COMMON_MASK);
        store->gtFlags |= GTF_VAR_DEF;

        switchBBRange.InsertAfter(node, store);
        switchBBRange.Remove(node);

        return store;
    }

    noway_assert(jumpCnt >= 2);

    // Spill the argument to the switch node into a local so that it can be used later.
    unsigned blockWeight = originalSwitchBB->getBBWeight(comp);

    LIR::Use use(switchBBRange, &(node->gtOp.gtOp1), node);
    use.ReplaceWithLclVar(comp, blockWeight);

    // GT_SWITCH(indexExpression) is now two statements:
    //   1. a statement containing 'asg' (for temp = indexExpression)
    //   2. and a statement with GT_SWITCH(temp)

    assert(node->gtOper == GT_SWITCH);
    GenTreePtr temp = node->gtOp.gtOp1;
    assert(temp->gtOper == GT_LCL_VAR);
    unsigned   tempLclNum  = temp->gtLclVarCommon.gtLclNum;
    LclVarDsc* tempVarDsc  = comp->lvaTable + tempLclNum;
    var_types  tempLclType = tempVarDsc->TypeGet();

    BasicBlock* defaultBB   = jumpTab[jumpCnt - 1];
    BasicBlock* followingBB = originalSwitchBB->bbNext;

    /* Is the number of cases right for a test and jump switch? */
    const bool fFirstCaseFollows = (followingBB == jumpTab[0]);
    const bool fDefaultFollows   = (followingBB == defaultBB);

    unsigned minSwitchTabJumpCnt = 2; // table is better than just 2 cmp/jcc

    // This means really just a single cmp/jcc (aka a simple if/else)
    if (fFirstCaseFollows || fDefaultFollows)
    {
        minSwitchTabJumpCnt++;
    }

#if defined(_TARGET_ARM_)
    // On ARM for small switch tables we will
    // generate a sequence of compare and branch instructions
    // because the code to load the base of the switch
    // table is huge and hideous due to the relocation... :(
    minSwitchTabJumpCnt += 2;
#endif // _TARGET_ARM_

    // Once we have the temporary variable, we construct the conditional branch for
    // the default case.  As stated above, this conditional is being shared between
    // both GT_SWITCH lowering code paths.
    // This condition is of the form: if (temp > jumpTableLength - 2){ goto jumpTable[jumpTableLength - 1]; }
    GenTreePtr gtDefaultCaseCond = comp->gtNewOperNode(GT_GT, TYP_INT, comp->gtNewLclvNode(tempLclNum, tempLclType),
                                                       comp->gtNewIconNode(jumpCnt - 2, TYP_INT));

    // Make sure we perform an unsigned comparison, just in case the switch index in 'temp'
    // is now less than zero 0 (that would also hit the default case).
    gtDefaultCaseCond->gtFlags |= GTF_UNSIGNED;

    /* Increment the lvRefCnt and lvRefCntWtd for temp */
    tempVarDsc->incRefCnts(blockWeight, comp);

    GenTreePtr gtDefaultCaseJump = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, gtDefaultCaseCond);
    gtDefaultCaseJump->gtFlags   = node->gtFlags;

    LIR::Range condRange = LIR::SeqTree(comp, gtDefaultCaseJump);
    switchBBRange.InsertAtEnd(std::move(condRange));

    BasicBlock* afterDefaultCondBlock = comp->fgSplitBlockAfterNode(originalSwitchBB, condRange.LastNode());

    // afterDefaultCondBlock is now the switch, and all the switch targets have it as a predecessor.
    // originalSwitchBB is now a BBJ_NONE, and there is a predecessor edge in afterDefaultCondBlock
    // representing the fall-through flow from originalSwitchBB.
    assert(originalSwitchBB->bbJumpKind == BBJ_NONE);
    assert(originalSwitchBB->bbNext == afterDefaultCondBlock);
    assert(afterDefaultCondBlock->bbJumpKind == BBJ_SWITCH);
    assert(afterDefaultCondBlock->bbJumpSwt->bbsHasDefault);
    assert(afterDefaultCondBlock->isEmpty()); // Nothing here yet.

    // The GT_SWITCH code is still in originalSwitchBB (it will be removed later).

    // Turn originalSwitchBB into a BBJ_COND.
    originalSwitchBB->bbJumpKind = BBJ_COND;
    originalSwitchBB->bbJumpDest = jumpTab[jumpCnt - 1];

    // Fix the pred for the default case: the default block target still has originalSwitchBB
    // as a predecessor, but the fgSplitBlockAfterStatement() moved all predecessors to point
    // to afterDefaultCondBlock.
    flowList* oldEdge = comp->fgRemoveRefPred(jumpTab[jumpCnt - 1], afterDefaultCondBlock);
    comp->fgAddRefPred(jumpTab[jumpCnt - 1], originalSwitchBB, oldEdge);

    // If we originally had 2 unique successors, check to see whether there is a unique
    // non-default case, in which case we can eliminate the switch altogether.
    // Note that the single unique successor case is handled above.
    BasicBlock* uniqueSucc = nullptr;
    if (targetCnt == 2)
    {
        uniqueSucc = jumpTab[0];
        noway_assert(jumpCnt >= 2);
        for (unsigned i = 1; i < jumpCnt - 1; i++)
        {
            if (jumpTab[i] != uniqueSucc)
            {
                uniqueSucc = nullptr;
                break;
            }
        }
    }
    if (uniqueSucc != nullptr)
    {
        // If the unique successor immediately follows this block, we have nothing to do -
        // it will simply fall-through after we remove the switch, below.
        // Otherwise, make this a BBJ_ALWAYS.
        // Now, fixup the predecessor links to uniqueSucc.  In the original jumpTab:
        //   jumpTab[i-1] was the default target, which we handled above,
        //   jumpTab[0] is the first target, and we'll leave that predecessor link.
        // Remove any additional predecessor links to uniqueSucc.
        for (unsigned i = 1; i < jumpCnt - 1; ++i)
        {
            assert(jumpTab[i] == uniqueSucc);
            (void)comp->fgRemoveRefPred(uniqueSucc, afterDefaultCondBlock);
        }
        if (afterDefaultCondBlock->bbNext == uniqueSucc)
        {
            afterDefaultCondBlock->bbJumpKind = BBJ_NONE;
            afterDefaultCondBlock->bbJumpDest = nullptr;
        }
        else
        {
            afterDefaultCondBlock->bbJumpKind = BBJ_ALWAYS;
            afterDefaultCondBlock->bbJumpDest = uniqueSucc;
        }
    }
    // If the number of possible destinations is small enough, we proceed to expand the switch
    // into a series of conditional branches, otherwise we follow the jump table based switch
    // transformation.
    else if (jumpCnt < minSwitchTabJumpCnt)
    {
        // Lower the switch into a series of compare and branch IR trees.
        //
        // In this case we will morph the node in the following way:
        // 1. Generate a JTRUE statement to evaluate the default case. (This happens above.)
        // 2. Start splitting the switch basic block into subsequent basic blocks, each of which will contain
        //    a statement that is responsible for performing a comparison of the table index and conditional
        //    branch if equal.

        JITDUMP("Lowering switch BB%02u: using compare/branch expansion\n", originalSwitchBB->bbNum);

        // We'll use 'afterDefaultCondBlock' for the first conditional. After that, we'll add new
        // blocks. If we end up not needing it at all (say, if all the non-default cases just fall through),
        // we'll delete it.
        bool        fUsedAfterDefaultCondBlock = false;
        BasicBlock* currentBlock               = afterDefaultCondBlock;
        LIR::Range* currentBBRange             = &LIR::AsRange(currentBlock);

        // Walk to entries 0 to jumpCnt - 1. If a case target follows, ignore it and let it fall through.
        // If no case target follows, the last one doesn't need to be a compare/branch: it can be an
        // unconditional branch.
        bool fAnyTargetFollows = false;
        for (unsigned i = 0; i < jumpCnt - 1; ++i)
        {
            assert(currentBlock != nullptr);

            // Remove the switch from the predecessor list of this case target's block.
            // We'll add the proper new predecessor edge later.
            flowList* oldEdge = comp->fgRemoveRefPred(jumpTab[i], afterDefaultCondBlock);

            if (jumpTab[i] == followingBB)
            {
                // This case label follows the switch; let it fall through.
                fAnyTargetFollows = true;
                continue;
            }

            // We need a block to put in the new compare and/or branch.
            // If we haven't used the afterDefaultCondBlock yet, then use that.
            if (fUsedAfterDefaultCondBlock)
            {
                BasicBlock* newBlock = comp->fgNewBBafter(BBJ_NONE, currentBlock, true);
                comp->fgAddRefPred(newBlock, currentBlock); // The fall-through predecessor.
                currentBlock   = newBlock;
                currentBBRange = &LIR::AsRange(currentBlock);
            }
            else
            {
                assert(currentBlock == afterDefaultCondBlock);
                fUsedAfterDefaultCondBlock = true;
            }

            // We're going to have a branch, either a conditional or unconditional,
            // to the target. Set the target.
            currentBlock->bbJumpDest = jumpTab[i];

            // Wire up the predecessor list for the "branch" case.
            comp->fgAddRefPred(jumpTab[i], currentBlock, oldEdge);

            if (!fAnyTargetFollows && (i == jumpCnt - 2))
            {
                // We're processing the last one, and there is no fall through from any case
                // to the following block, so we can use an unconditional branch to the final
                // case: there is no need to compare against the case index, since it's
                // guaranteed to be taken (since the default case was handled first, above).

                currentBlock->bbJumpKind = BBJ_ALWAYS;
            }
            else
            {
                // Otherwise, it's a conditional branch. Set the branch kind, then add the
                // condition statement.
                currentBlock->bbJumpKind = BBJ_COND;

                // Now, build the conditional statement for the current case that is
                // being evaluated:
                // GT_JTRUE
                //   |__ GT_COND
                //          |____GT_EQ
                //                 |____ (switchIndex) (The temp variable)
                //                 |____ (ICon)        (The actual case constant)
                GenTreePtr gtCaseCond =
                    comp->gtNewOperNode(GT_EQ, TYP_INT, comp->gtNewLclvNode(tempLclNum, tempLclType),
                                        comp->gtNewIconNode(i, TYP_INT));
                /* Increment the lvRefCnt and lvRefCntWtd for temp */
                tempVarDsc->incRefCnts(blockWeight, comp);

                GenTreePtr gtCaseBranch = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, gtCaseCond);
                LIR::Range caseRange    = LIR::SeqTree(comp, gtCaseBranch);
                currentBBRange->InsertAtEnd(std::move(condRange));
            }
        }

        if (fAnyTargetFollows)
        {
            // There is a fall-through to the following block. In the loop
            // above, we deleted all the predecessor edges from the switch.
            // In this case, we need to add one back.
            comp->fgAddRefPred(currentBlock->bbNext, currentBlock);
        }

        if (!fUsedAfterDefaultCondBlock)
        {
            // All the cases were fall-through! We don't need this block.
            // Convert it from BBJ_SWITCH to BBJ_NONE and unset the BBF_DONT_REMOVE flag
            // so fgRemoveBlock() doesn't complain.
            JITDUMP("Lowering switch BB%02u: all switch cases were fall-through\n", originalSwitchBB->bbNum);
            assert(currentBlock == afterDefaultCondBlock);
            assert(currentBlock->bbJumpKind == BBJ_SWITCH);
            currentBlock->bbJumpKind = BBJ_NONE;
            currentBlock->bbFlags &= ~BBF_DONT_REMOVE;
            comp->fgRemoveBlock(currentBlock, /* unreachable */ false); // It's an empty block.
        }
    }
    else
    {
        // Lower the switch into an indirect branch using a jump table:
        //
        // 1. Create the constant for the default case
        // 2. Generate a GT_GE condition to compare to the default case
        // 3. Generate a GT_JTRUE to jump.
        // 4. Load the jump table address into a local (presumably the just
        //    created constant for GT_SWITCH).
        // 5. Create a new node for the lowered switch, this will both generate
        //    the branch table and also will be responsible for the indirect
        //    branch.

        JITDUMP("Lowering switch BB%02u: using jump table expansion\n", originalSwitchBB->bbNum);

        GenTreePtr gtTableSwitch =
            comp->gtNewOperNode(GT_SWITCH_TABLE, TYP_VOID, comp->gtNewLclvNode(tempLclNum, tempLclType),
                                comp->gtNewJmpTableNode());
        /* Increment the lvRefCnt and lvRefCntWtd for temp */
        tempVarDsc->incRefCnts(blockWeight, comp);

        // this block no longer branches to the default block
        afterDefaultCondBlock->bbJumpSwt->removeDefault();
        comp->fgInvalidateSwitchDescMapEntry(afterDefaultCondBlock);

        LIR::Range& afterDefaultCondBBRange = LIR::AsRange(afterDefaultCondBlock);
        afterDefaultCondBBRange.InsertAtEnd(LIR::SeqTree(comp, gtTableSwitch));
    }

    GenTree* next = node->gtNext;

    // Get rid of the GT_SWITCH(temp).
    switchBBRange.Remove(node->gtOp.gtOp1);
    switchBBRange.Remove(node);

    return next;
}

// NOTE: this method deliberately does not update the call arg table. It must only
// be used by NewPutArg and LowerArg; these functions are responsible for updating
// the call arg table as necessary.
void Lowering::ReplaceArgWithPutArgOrCopy(GenTree** argSlot, GenTree* putArgOrCopy)
{
    assert(argSlot != nullptr);
    assert(*argSlot != nullptr);
    assert(putArgOrCopy->OperGet() == GT_PUTARG_REG || putArgOrCopy->OperGet() == GT_PUTARG_STK ||
           putArgOrCopy->OperGet() == GT_COPY);

    GenTree* arg = *argSlot;

    // Replace the argument with the putarg/copy
    *argSlot                 = putArgOrCopy;
    putArgOrCopy->gtOp.gtOp1 = arg;

    // Insert the putarg/copy into the block
    BlockRange().InsertAfter(arg, putArgOrCopy);
}

//------------------------------------------------------------------------
// NewPutArg: rewrites the tree to put an arg in a register or on the stack.
//
// Arguments:
//    call - the call whose arg is being rewritten.
//    arg  - the arg being rewritten.
//    info - the ArgTabEntry information for the argument.
//    type - the type of the argument.
//
// Return Value:
//    The new tree that was created to put the arg in the right place
//    or the incoming arg if the arg tree was not rewritten.
//
// Assumptions:
//    call, arg, and info must be non-null.
//
// Notes:
//    For System V systems with native struct passing (i.e. FEATURE_UNIX_AMD64_STRUCT_PASSING defined)
//    this method allocates a single GT_PUTARG_REG for 1 eightbyte structs and a GT_LIST of two GT_PUTARG_REGs
//    for two eightbyte structs.
//
//    For STK passed structs the method generates GT_PUTARG_STK tree. For System V systems with native struct passing
//    (i.e. FEATURE_UNIX_AMD64_STRUCT_PASSING defined) this method also sets the GP pointers count and the pointers
//    layout object, so the codegen of the GT_PUTARG_STK could use this for optimizing copying to the stack by value.
//    (using block copy primitives for non GC pointers and a single TARGET_POINTER_SIZE copy with recording GC info.)
//
GenTreePtr Lowering::NewPutArg(GenTreeCall* call, GenTreePtr arg, fgArgTabEntryPtr info, var_types type)
{
    assert(call != nullptr);
    assert(arg != nullptr);
    assert(info != nullptr);

    GenTreePtr putArg         = nullptr;
    bool       updateArgTable = true;

#if !defined(_TARGET_64BIT_)
    if (varTypeIsLong(type))
    {
        // For TYP_LONG, we leave the GT_LONG as the arg, and put the putArg below it.
        // Therefore, we don't update the arg table entry.
        updateArgTable = false;
        type           = TYP_INT;
    }
#endif // !defined(_TARGET_64BIT_)

    bool isOnStack = true;
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    if (varTypeIsStruct(type))
    {
        isOnStack = !info->structDesc.passedInRegisters;
    }
    else
    {
        isOnStack = info->regNum == REG_STK;
    }
#else  // !FEATURE_UNIX_AMD64_STRUCT_PASSING
    isOnStack = info->regNum == REG_STK;
#endif // !FEATURE_UNIX_AMD64_STRUCT_PASSING

    if (!isOnStack)
    {
#ifdef FEATURE_SIMD
        // TYP_SIMD8 is passed in an integer register.  We need the putArg node to be of the int type.
        if (type == TYP_SIMD8 && genIsValidIntReg(info->regNum))
        {
            type = TYP_LONG;
        }
#endif // FEATURE_SIMD

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        if (info->isStruct)
        {
            // The following code makes sure a register passed struct arg is moved to
            // the register before the call is made.
            // There are two cases (comments added in the code below.)
            // 1. The struct is of size one eightbyte:
            //    In this case a new tree is created that is GT_PUTARG_REG
            //    with a op1 the original argument.
            // 2. The struct is contained in 2 eightbytes:
            //    in this case the arg comes as a GT_LIST of two GT_LCL_FLDs - the two eightbytes of the struct.
            //    The code creates a GT_PUTARG_REG node for each GT_LCL_FLD in the GT_LIST
            //    and splices it in the list with the corresponding original GT_LCL_FLD tree as op1.

            assert(info->structDesc.eightByteCount != 0);

            if (info->structDesc.eightByteCount == 1)
            {
                // clang-format off
                // Case 1 above: Create a GT_PUTARG_REG node with op1 of the original tree.
                //
                // Here the IR for this operation:
                // lowering call :
                //     N001(3, 2)[000017] ------ - N---- / --*  &lclVar   byref  V00 loc0
                //     N003(6, 5)[000052] * --XG------ - / --*  indir     int
                //     N004(3, 2)[000046] ------ - N---- + --*  &lclVar   byref  V02 tmp0
                //     (13, 11)[000070] -- - XG-- - R-- - arg0 in out + 00 / --*  storeIndir int
                //     N009(3, 4)[000054] ------ - N----arg0 in rdi + --*  lclFld    int    V02 tmp0[+0](last use)
                //     N011(33, 21)[000018] --CXG------ - *call      void   Test.Foo.test1
                //
                // args :
                //     lowering arg : (13, 11)[000070] -- - XG-- - R-- - *storeIndir int
                //
                // late :
                //    lowering arg : N009(3, 4)[000054] ------ - N----             *  lclFld    int    V02 tmp0[+0](last use)
                //    new node is : (3, 4)[000071] ------------             *  putarg_reg int    RV
                //
                // after :
                //    N001(3, 2)[000017] ------ - N---- / --*  &lclVar   byref  V00 loc0
                //    N003(6, 5)[000052] * --XG------ - / --*  indir     int
                //    N004(3, 2)[000046] ------ - N---- + --*  &lclVar   byref  V02 tmp0
                //    (13, 11)[000070] -- - XG-- - R-- - arg0 in out + 00 / --*  storeIndir int
                //    N009(3, 4)[000054] ------ - N---- | / --*  lclFld    int    V02 tmp0[+0](last use)
                //    (3, 4)[000071] ------------arg0 in rdi + --*  putarg_reg int    RV
                //    N011(33, 21)[000018] --CXG------ - *call      void   Test.Foo.test1
                //
                // clang-format on

                putArg = comp->gtNewOperNode(GT_PUTARG_REG, type, arg);
            }
            else if (info->structDesc.eightByteCount == 2)
            {
                // clang-format off
                // Case 2 above: Convert the LCL_FLDs to PUTARG_REG
                //
                // lowering call :
                //     N001(3, 2)  [000025] ------ - N----Source / --*  &lclVar   byref  V01 loc1
                //     N003(3, 2)  [000056] ------ - N----Destination + --*  &lclVar   byref  V03 tmp1
                //     N006(1, 1)  [000058] ------------ + --*  const     int    16
                //     N007(12, 12)[000059] - A--G---- - L - arg0 SETUP / --*  copyBlk   void
                //     N009(3, 4)  [000061] ------ - N----arg0 in rdi + --*  lclFld    long   V03 tmp1[+0]
                //     N010(3, 4)  [000063] ------------arg0 in rsi + --*  lclFld    long   V03 tmp1[+8](last use)
                //     N014(40, 31)[000026] --CXG------ - *call      void   Test.Foo.test2
                //
                // args :
                //     lowering arg : N007(12, 12)[000059] - A--G---- - L - *copyBlk   void
                //
                // late :
                //     lowering arg : N012(11, 13)[000065] ------------             *  <list>    struct
                //
                // after :
                //     N001(3, 2)[000025] ------ - N----Source / --*  &lclVar   byref  V01 loc1
                //     N003(3, 2)[000056] ------ - N----Destination + --*  &lclVar   byref  V03 tmp1
                //     N006(1, 1)[000058] ------------ + --*  const     int    16
                //     N007(12, 12)[000059] - A--G---- - L - arg0 SETUP / --*  copyBlk   void
                //     N009(3, 4)[000061] ------ - N---- | / --*  lclFld    long   V03 tmp1[+0]
                //     (3, 4)[000072] ------------arg0 in rdi + --*  putarg_reg long
                //     N010(3, 4)[000063] ------------ | / --*  lclFld    long   V03 tmp1[+8](last use)
                //     (3, 4)[000073] ------------arg0 in rsi + --*  putarg_reg long
                //     N014(40, 31)[000026] --CXG------ - *call      void   Test.Foo.test2
                //
                // clang-format on

                assert(arg->OperGet() == GT_LIST);
                GenTreeArgList* argListPtr = arg->AsArgList();

                for (unsigned ctr = 0; argListPtr != nullptr; argListPtr = argListPtr->Rest(), ctr++)
                {
                    // Create a new GT_PUTARG_REG node with op1 the original GT_LCL_FLD.
                    GenTreePtr newOper = comp->gtNewOperNode(
                        GT_PUTARG_REG,
                        comp->GetTypeFromClassificationAndSizes(info->structDesc.eightByteClassifications[ctr],
                                                                info->structDesc.eightByteSizes[ctr]),
                        argListPtr->gtOp.gtOp1);

                    // Splice in the new GT_PUTARG_REG node in the GT_LIST
                    ReplaceArgWithPutArgOrCopy(&argListPtr->gtOp.gtOp1, newOper);
                }

                // Just return arg. The GT_LIST is not replaced.
                // Nothing more to do.
                return arg;
            }
            else
            {
                assert(false &&
                       "Illegal count of eightbytes for the CLR type system"); // No more than 2 eightbytes for the CLR.
            }
        }
        else
#else // not defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#if FEATURE_MULTIREG_ARGS
        if ((info->numRegs > 1) && (arg->OperGet() == GT_LIST))
        {
            assert(arg->OperGet() == GT_LIST);
            GenTreeArgList* argListPtr = arg->AsArgList();

            for (unsigned ctr = 0; argListPtr != nullptr; argListPtr = argListPtr->Rest(), ctr++)
            {
                GenTreePtr curOp  = argListPtr->gtOp.gtOp1;
                var_types  curTyp = curOp->TypeGet();

                // Create a new GT_PUTARG_REG node with op1
                GenTreePtr newOper = comp->gtNewOperNode(GT_PUTARG_REG, curTyp, curOp);

                // Splice in the new GT_PUTARG_REG node in the GT_LIST
                ReplaceArgWithPutArgOrCopy(&argListPtr->gtOp.gtOp1, newOper);
            }

            // Just return arg. The GT_LIST is not replaced.
            // Nothing more to do.
            return arg;
        }
        else
#endif // FEATURE_MULTIREG_ARGS
#endif // not defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
        {
            putArg = comp->gtNewOperNode(GT_PUTARG_REG, type, arg);
        }
    }
    else
    {
        // Mark this one as tail call arg if it is a fast tail call.
        // This provides the info to put this argument in in-coming arg area slot
        // instead of in out-going arg area slot.

        FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY(assert(info->isStruct == varTypeIsStruct(type))); // Make sure state is
                                                                                                 // correct

#if FEATURE_FASTTAILCALL
        putArg = new (comp, GT_PUTARG_STK)
            GenTreePutArgStk(GT_PUTARG_STK, type, arg,
                             info->slotNum FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(info->numSlots)
                                 FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(info->isStruct),
                             call->IsFastTailCall() DEBUGARG(call));
#else
        putArg = new (comp, GT_PUTARG_STK)
            GenTreePutArgStk(GT_PUTARG_STK, type, arg,
                             info->slotNum FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(info->numSlots)
                                 FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(info->isStruct) DEBUGARG(call));
#endif

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        // If the ArgTabEntry indicates that this arg is a struct
        // get and store the number of slots that are references.
        // This is later used in the codegen for PUT_ARG_STK implementation
        // for struct to decide whether and how many single eight-byte copies
        // to be done (only for reference slots), so gcinfo is emitted.
        // For non-reference slots faster/smaller size instructions are used -
        // pair copying using XMM registers or rep mov instructions.
        if (info->isStruct)
        {
            unsigned numRefs  = 0;
            BYTE*    gcLayout = new (comp, CMK_Codegen) BYTE[info->numSlots];
            // We use GT_OBJ for non-SIMD struct arguments. However, for
            // SIMD arguments the GT_OBJ has already been transformed.
            if (arg->gtOper != GT_OBJ)
            {
                assert(varTypeIsSIMD(arg));
            }
            else
            {
                assert(!varTypeIsSIMD(arg));
                numRefs = comp->info.compCompHnd->getClassGClayout(arg->gtObj.gtClass, gcLayout);
            }

            putArg->AsPutArgStk()->setGcPointers(numRefs, gcLayout);
        }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
    }

    if (arg->InReg())
    {
        putArg->SetInReg();
    }
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    else if (info->isStruct)
    {
        if (info->structDesc.passedInRegisters)
        {
            putArg->SetInReg();
        }
    }
#endif

    JITDUMP("new node is : ");
    DISPNODE(putArg);
    JITDUMP("\n");

    if (arg->gtFlags & GTF_LATE_ARG)
    {
        putArg->gtFlags |= GTF_LATE_ARG;
    }
    else if (updateArgTable)
    {
        info->node = putArg;
    }
    return putArg;
}

//------------------------------------------------------------------------
// LowerArg: Lower one argument of a call. This entails splicing a "putarg" node between
// the argument evaluation and the call. This is the point at which the source is
// consumed and the value transitions from control of the register allocator to the calling
// convention.
//
// Arguments:
//    call  - The call node
//    ppArg - Pointer to the call argument pointer. We might replace the call argument by
//            changing *ppArg.
//
// Return Value:
//    None.
//
void Lowering::LowerArg(GenTreeCall* call, GenTreePtr* ppArg)
{
    GenTreePtr arg = *ppArg;

    JITDUMP("lowering arg : ");
    DISPNODE(arg);

    // No assignments should remain by Lowering.
    assert(!arg->OperIsAssignment());
    assert(!arg->OperIsPutArgStk());

    // Assignments/stores at this level are not really placing an argument.
    // They are setting up temporary locals that will later be placed into
    // outgoing regs or stack.
    if (arg->OperIsStore() || arg->IsArgPlaceHolderNode() || arg->IsNothingNode() || arg->OperIsCopyBlkOp())
    {
        return;
    }

    fgArgTabEntryPtr info = comp->gtArgEntryByNode(call, arg);
    assert(info->node == arg);
    bool      isReg = (info->regNum != REG_STK);
    var_types type  = arg->TypeGet();

    if (varTypeIsSmall(type))
    {
        // Normalize 'type', it represents the item that we will be storing in the Outgoing Args
        type = TYP_INT;
    }

    GenTreePtr putArg;

    // If we hit this we are probably double-lowering.
    assert(!arg->OperIsPutArg());

#if !defined(_TARGET_64BIT_)
    if (varTypeIsLong(type))
    {
        if (isReg)
        {
            NYI("Lowering of long register argument");
        }

        // For longs, we will create two PUTARG_STKs below the GT_LONG. The hi argument needs to
        // be pushed first, so the hi PUTARG_STK will precede the lo PUTARG_STK in execution order.
        noway_assert(arg->OperGet() == GT_LONG);
        GenTreePtr argLo = arg->gtGetOp1();
        GenTreePtr argHi = arg->gtGetOp2();

        GenTreePtr putArgLo = NewPutArg(call, argLo, info, type);
        GenTreePtr putArgHi = NewPutArg(call, argHi, info, type);

        arg->gtOp.gtOp1 = putArgLo;
        arg->gtOp.gtOp2 = putArgHi;

        BlockRange().InsertBefore(arg, putArgHi, putArgLo);

        // The execution order now looks like this:
        // argLoPrev <-> argLoFirst ... argLo <-> argHiFirst ... argHi <-> putArgHi <-> putArgLo <-> arg(GT_LONG)

        assert((arg->gtFlags & GTF_REVERSE_OPS) == 0);
        arg->gtFlags |= GTF_REVERSE_OPS; // We consume the high arg (op2) first.
    }
    else
#endif // !defined(_TARGET_64BIT_)
    {

#ifdef _TARGET_ARM64_
        // For vararg call, reg args should be all integer.
        // Insert a copy to move float value to integer register.
        if (call->IsVarargs() && varTypeIsFloating(type))
        {
            var_types  intType = (type == TYP_DOUBLE) ? TYP_LONG : TYP_INT;
            GenTreePtr intArg  = comp->gtNewOperNode(GT_COPY, intType, arg);

            info->node = intArg;
            ReplaceArgWithPutArgOrCopy(ppArg, intArg);

            // Update arg/type with new ones.
            arg  = intArg;
            type = intType;
        }
#endif

        putArg = NewPutArg(call, arg, info, type);

        // In the case of register passable struct (in one or two registers)
        // the NewPutArg returns a new node (GT_PUTARG_REG or a GT_LIST with two GT_PUTARG_REGs.)
        // If an extra node is returned, splice it in the right place in the tree.
        if (arg != putArg)
        {
            // putArg and arg are equals if arg is GT_LIST (a list of multiple LCL_FLDs to be passed in registers.)
            ReplaceArgWithPutArgOrCopy(ppArg, putArg);
        }
    }
}

// do lowering steps for each arg of a call
void Lowering::LowerArgsForCall(GenTreeCall* call)
{
    JITDUMP("objp:\n======\n");
    if (call->gtCallObjp)
    {
        LowerArg(call, &call->gtCallObjp);
    }

    GenTreeArgList* args = call->gtCallArgs;

    JITDUMP("\nargs:\n======\n");
    for (; args; args = args->Rest())
    {
        LowerArg(call, &args->Current());
    }

    JITDUMP("\nlate:\n======\n");
    for (args = call->gtCallLateArgs; args; args = args->Rest())
    {
        LowerArg(call, &args->Current());
    }
}

// helper that create a node representing a relocatable physical address computation
// (optionally specifying the register to place it in)
GenTree* Lowering::AddrGen(ssize_t addr, regNumber reg)
{
    // this should end up in codegen as : instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg, addr)
    GenTree* result = comp->gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);

    result->gtRegNum = reg;

    return result;
}

// variant that takes a void*
GenTree* Lowering::AddrGen(void* addr, regNumber reg)
{
    return AddrGen((ssize_t)addr, reg);
}

// do lowering steps for a call
// this includes:
//   - adding the placement nodes (either stack or register variety) for arguments
//   - lowering the expression that calculates the target address
//   - adding nodes for other operations that occur after the call sequence starts and before
//        control transfer occurs (profiling and tail call helpers, pinvoke incantations)
//
void Lowering::LowerCall(GenTree* node)
{
    GenTreeCall* call = node->AsCall();

    JITDUMP("lowering call (before):\n");
    DISPTREERANGE(BlockRange(), call);
    JITDUMP("\n");

    LowerArgsForCall(call);

// RyuJIT arm is not set up for lowered call control
#ifndef _TARGET_ARM_

    // note that everything generated from this point on runs AFTER the outgoing args are placed
    GenTree* result = nullptr;

    // for x86, this is where we record ESP for checking later to make sure stack is balanced

    // Check for Delegate.Invoke(). If so, we inline it. We get the
    // target-object and target-function from the delegate-object, and do
    // an indirect call.
    if (call->IsDelegateInvoke())
    {
        result = LowerDelegateInvoke(call);
    }
    else
    {
        //  Virtual and interface calls
        switch (call->gtFlags & GTF_CALL_VIRT_KIND_MASK)
        {
            case GTF_CALL_VIRT_STUB:
                result = LowerVirtualStubCall(call);
                break;

            case GTF_CALL_VIRT_VTABLE:
                // stub dispatching is off or this is not a virtual call (could be a tailcall)
                result = LowerVirtualVtableCall(call);
                break;

            case GTF_CALL_NONVIRT:
                if (call->IsUnmanaged())
                {
                    result = LowerNonvirtPinvokeCall(call);
                }
                else if (call->gtCallType == CT_INDIRECT)
                {
                    result = LowerIndirectNonvirtCall(call);
                }
                else
                {
                    result = LowerDirectCall(call);
                }
                break;

            default:
                noway_assert(!"strange call type");
                break;
        }
    }

    if (call->IsTailCallViaHelper())
    {
        // Either controlExpr or gtCallAddr must contain real call target.
        if (result == nullptr)
        {
            assert(call->gtCallType == CT_INDIRECT);
            assert(call->gtCallAddr != nullptr);
            result = call->gtCallAddr;
        }

        result = LowerTailCallViaHelper(call, result);
    }
    else if (call->IsFastTailCall())
    {
        LowerFastTailCall(call);
    }

    if (result != nullptr)
    {
        LIR::Range resultRange = LIR::SeqTree(comp, result);

        JITDUMP("results of lowering call:\n");
        DISPRANGE(resultRange);

        GenTree* insertionPoint = call;
        if (!call->IsTailCallViaHelper())
        {
            // The controlExpr should go before the gtCallCookie and the gtCallAddr, if they exist
            //
            // TODO-LIR: find out what's really required here, as this is currently a tree order
            // dependency.
            if (call->gtCallType == CT_INDIRECT)
            {
                bool isClosed = false;
                if (call->gtCallCookie != nullptr)
                {
#ifdef DEBUG
                    GenTree* firstCallAddrNode = BlockRange().GetTreeRange(call->gtCallAddr, &isClosed).FirstNode();
                    assert(isClosed);
                    assert(call->gtCallCookie->Precedes(firstCallAddrNode));
#endif // DEBUG

                    insertionPoint = BlockRange().GetTreeRange(call->gtCallCookie, &isClosed).FirstNode();
                    assert(isClosed);
                }
                else if (call->gtCallAddr != nullptr)
                {
                    insertionPoint = BlockRange().GetTreeRange(call->gtCallAddr, &isClosed).FirstNode();
                    assert(isClosed);
                }
            }
        }

        BlockRange().InsertBefore(insertionPoint, std::move(resultRange));

        call->gtControlExpr = result;
    }
#endif //!_TARGET_ARM_

    if (comp->opts.IsJit64Compat())
    {
        CheckVSQuirkStackPaddingNeeded(call);
    }

    JITDUMP("lowering call (after):\n");
    DISPTREERANGE(BlockRange(), call);
    JITDUMP("\n");
}

// Though the below described issue gets fixed in intellitrace dll of VS2015 (a.k.a Dev14),
// we still need this quirk for desktop so that older version of VS (e.g. VS2010/2012)
// continues to work.
// This quirk is excluded from other targets that have no back compat burden.
//
// Quirk for VS debug-launch scenario to work:
// See if this is a PInvoke call with exactly one param that is the address of a struct local.
// In such a case indicate to frame-layout logic to add 16-bytes of padding
// between save-reg area and locals.  This is to protect against the buffer
// overrun bug in microsoft.intellitrace.11.0.0.dll!ProfilerInterop.InitInterop().
//
// A work-around to this bug is to disable IntelliTrace debugging
// (VS->Tools->Options->IntelliTrace->Enable IntelliTrace - uncheck this option).
// The reason why this works on Jit64 is that at the point of AV the call stack is
//
// GetSystemInfo() Native call
// IL_Stub generated for PInvoke declaration.
// ProfilerInterface::InitInterop()
// ProfilerInterface.Cctor()
// VM asm worker
//
// The cctor body has just the call to InitInterop().  VM asm worker is holding
// something in rbx that is used immediately after the Cctor call.  Jit64 generated
// InitInterop() method is pushing the registers in the following order
//
//  rbx
//  rbp
//  rsi
//  rdi
//  r12
//  r13
//  Struct local
//
// Due to buffer overrun, rbx doesn't get impacted.  Whereas RyuJIT jitted code of
// the same method is pushing regs in the following order
//
//  rbp
//  rdi
//  rsi
//  rbx
//  struct local
//
// Therefore as a fix, we add padding between save-reg area and locals to
// make this scenario work against JB.
//
// Note: If this quirk gets broken due to other JIT optimizations, we should consider
// more tolerant fix.  One such fix is to padd the struct.
void Lowering::CheckVSQuirkStackPaddingNeeded(GenTreeCall* call)
{
    assert(comp->opts.IsJit64Compat());

#ifdef _TARGET_AMD64_
    // Confine this to IL stub calls which aren't marked as unmanaged.
    if (call->IsPInvoke() && !call->IsUnmanaged())
    {
        bool       paddingNeeded  = false;
        GenTreePtr firstPutArgReg = nullptr;
        for (GenTreeArgList* args = call->gtCallLateArgs; args; args = args->Rest())
        {
            GenTreePtr tmp = args->Current();
            if (tmp->OperGet() == GT_PUTARG_REG)
            {
                if (firstPutArgReg == nullptr)
                {
                    firstPutArgReg = tmp;
                    GenTreePtr op1 = firstPutArgReg->gtOp.gtOp1;

                    if (op1->OperGet() == GT_LCL_VAR_ADDR)
                    {
                        unsigned lclNum = op1->AsLclVarCommon()->GetLclNum();
                        // TODO-1stClassStructs: This is here to duplicate previous behavior,
                        // but is not needed because the scenario being quirked did not involve
                        // a SIMD or enregisterable struct.
                        // if(comp->lvaTable[lclNum].TypeGet() == TYP_STRUCT)
                        if (varTypeIsStruct(comp->lvaTable[lclNum].TypeGet()))
                        {
                            // First arg is addr of a struct local.
                            paddingNeeded = true;
                        }
                        else
                        {
                            // Not a struct local.
                            assert(paddingNeeded == false);
                            break;
                        }
                    }
                    else
                    {
                        // First arg is not a local var addr.
                        assert(paddingNeeded == false);
                        break;
                    }
                }
                else
                {
                    // Has more than one arg.
                    paddingNeeded = false;
                    break;
                }
            }
        }

        if (paddingNeeded)
        {
            comp->compVSQuirkStackPaddingNeeded = VSQUIRK_STACK_PAD;
        }
    }
#endif // _TARGET_AMD64_
}

// Inserts profiler hook, GT_PROF_HOOK for a tail call node.
//
// We need to insert this after all nested calls, but before all the arguments to this call have been set up.
// To do this, we look for the first GT_PUTARG_STK or GT_PUTARG_REG, and insert the hook immediately before
// that. If there are no args, then it should be inserted before the call node.
//
// For example:
//              *  stmtExpr  void  (top level) (IL 0x000...0x010)
// arg0 SETUP   |  /--*  argPlace  ref    REG NA $c5
// this in rcx  |  |     /--*  argPlace  ref    REG NA $c1
//              |  |     |  /--*  call      ref    System.Globalization.CultureInfo.get_InvariantCulture $c2
// arg1 SETUP   |  |     +--*  st.lclVar ref    V02 tmp1          REG NA $c2
//              |  |     |  /--*  lclVar    ref    V02 tmp1         u : 2 (last use) REG NA $c2
// arg1 in rdx  |  |     +--*  putarg_reg ref    REG NA
//              |  |     |  /--*  lclVar    ref    V00 arg0         u : 2 (last use) REG NA $80
// this in rcx  |  |     +--*  putarg_reg ref    REG NA
//              |  |  /--*  call nullcheck ref    System.String.ToLower $c5
//              |  |  {  *  stmtExpr  void  (embedded)(IL 0x000... ? ? ? )
//              |  |  {  \--*  prof_hook void   REG NA
// arg0 in rcx  |  +--*  putarg_reg ref    REG NA
// control expr |  +--*  const(h)  long   0x7ffe8e910e98 ftn REG NA
//              \--*  call      void   System.Runtime.Remoting.Identity.RemoveAppNameOrAppGuidIfNecessary $VN.Void
//
// In this case, the GT_PUTARG_REG src is a nested call. We need to put the instructions after that call
// (as shown). We assume that of all the GT_PUTARG_*, only the first one can have a nested call.
//
// Params:
//    callNode        - tail call node
//    insertionPoint  - if caller has an insertion point; If null
//                      profiler hook is inserted before args are setup
//                      but after all arg side effects are computed.
void Lowering::InsertProfTailCallHook(GenTreeCall* call, GenTree* insertionPoint)
{
    assert(call->IsTailCall());
    assert(comp->compIsProfilerHookNeeded());

    if (insertionPoint == nullptr)
    {
        GenTreePtr tmp = nullptr;
        for (GenTreeArgList* args = call->gtCallArgs; args; args = args->Rest())
        {
            tmp = args->Current();
            assert(tmp->OperGet() != GT_PUTARG_REG); // We don't expect to see these in gtCallArgs
            if (tmp->OperGet() == GT_PUTARG_STK)
            {
                // found it
                insertionPoint = tmp;
                break;
            }
        }

        if (insertionPoint == nullptr)
        {
            for (GenTreeArgList* args = call->gtCallLateArgs; args; args = args->Rest())
            {
                tmp = args->Current();
                if ((tmp->OperGet() == GT_PUTARG_REG) || (tmp->OperGet() == GT_PUTARG_STK))
                {
                    // found it
                    insertionPoint = tmp;
                    break;
                }
            }

            // If there are no args, insert before the call node
            if (insertionPoint == nullptr)
            {
                insertionPoint = call;
            }
        }
    }

    assert(insertionPoint != nullptr);
    GenTreePtr profHookNode = new (comp, GT_PROF_HOOK) GenTree(GT_PROF_HOOK, TYP_VOID);
    BlockRange().InsertBefore(insertionPoint, profHookNode);
}

// Lower fast tail call implemented as epilog+jmp.
// Also inserts PInvoke method epilog if required.
void Lowering::LowerFastTailCall(GenTreeCall* call)
{
#if FEATURE_FASTTAILCALL
    // Tail call restrictions i.e. conditions under which tail prefix is ignored.
    // Most of these checks are already done by importer or fgMorphTailCall().
    // This serves as a double sanity check.
    assert((comp->info.compFlags & CORINFO_FLG_SYNCH) == 0); // tail calls from synchronized methods
    assert(!comp->opts.compNeedSecurityCheck);               // tail call from methods that need security check
    assert(!call->IsUnmanaged());                            // tail calls to unamanaged methods
    assert(!comp->compLocallocUsed);                         // tail call from methods that also do localloc
    assert(!comp->getNeedsGSSecurityCookie());               // jit64 compat: tail calls from methods that need GS check

    // We expect to see a call that meets the following conditions
    assert(call->IsFastTailCall());

    // VM cannot use return address hijacking when A() and B() tail call each
    // other in mutual recursion.  Therefore, this block is reachable through
    // a GC-safe point or the whole method is marked as fully interruptible.
    //
    // TODO-Cleanup:
    // optReachWithoutCall() depends on the fact that loop headers blocks
    // will have a block number > fgLastBB.  These loop headers gets added
    // after dominator computation and get skipped by OptReachWithoutCall().
    // The below condition cannot be asserted in lower because fgSimpleLowering()
    // can add a new basic block for range check failure which becomes
    // fgLastBB with block number > loop header block number.
    // assert((comp->compCurBB->bbFlags & BBF_GC_SAFE_POINT) ||
    //         !comp->optReachWithoutCall(comp->fgFirstBB, comp->compCurBB) || comp->genInterruptible);

    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.  This is a case of caller method has both PInvokes and tail calls.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(call));
    }

    // Args for tail call are setup in incoming arg area.  The gc-ness of args of
    // caller and callee (which being tail called) may not match.  Therefore, everything
    // from arg setup until the epilog need to be non-interuptible by GC.  This is
    // achieved by inserting GT_START_NONGC before the very first GT_PUTARG_STK node
    // of call is setup.  Note that once a stack arg is setup, it cannot have nested
    // calls subsequently in execution order to setup other args, because the nested
    // call could over-write the stack arg that is setup earlier.
    GenTreePtr           firstPutArgStk = nullptr;
    GenTreeArgList*      args;
    ArrayStack<GenTree*> putargs(comp);

    for (args = call->gtCallArgs; args; args = args->Rest())
    {
        GenTreePtr tmp = args->Current();
        if (tmp->OperGet() == GT_PUTARG_STK)
        {
            putargs.Push(tmp);
        }
    }

    for (args = call->gtCallLateArgs; args; args = args->Rest())
    {
        GenTreePtr tmp = args->Current();
        if (tmp->OperGet() == GT_PUTARG_STK)
        {
            putargs.Push(tmp);
        }
    }

    if (putargs.Height() > 0)
    {
        firstPutArgStk = putargs.Bottom();
    }

    // If we have a putarg_stk node, also count the number of non-standard args the
    // call node has.  Note that while determining whether a tail call can be fast
    // tail called, we don't count non-standard args (passed in R10 or R11) since they
    // don't contribute to outgoing arg space.  These non-standard args are not
    // accounted in caller's arg count but accounted in callee's arg count after
    // fgMorphArgs(). Therefore, exclude callee's non-standard args while mapping
    // callee's stack arg num to corresponding caller's stack arg num.
    unsigned calleeNonStandardArgCount = call->GetNonStandardAddedArgCount(comp);

    // Say Caller(a, b, c, d, e) fast tail calls Callee(e, d, c, b, a)
    // i.e. passes its arguments in reverse to Callee. During call site
    // setup, after computing argument side effects, stack args are setup
    // first and reg args next.  In the above example, both Callers and
    // Callee stack args (e and a respectively) share the same stack slot
    // and are alive at the same time.  The act of setting up Callee's
    // stack arg will over-write the stack arg of Caller and if there are
    // further uses of Caller stack arg we have to make sure that we move
    // it to a temp before over-writing its slot and use temp in place of
    // the corresponding Caller stack arg.
    //
    // For the above example, conceptually this is what is done
    //       tmp = e;
    //       Stack slot of e  = a
    //       R9 = b, R8 = c, RDx = d
    //       RCX = tmp
    //
    // The below logic is meant to detect cases like this and introduce
    // temps to set up args correctly for Callee.

    for (int i = 0; i < putargs.Height(); i++)
    {
        GenTreePtr putArgStkNode = putargs.Bottom(i);

        assert(putArgStkNode->OperGet() == GT_PUTARG_STK);

        // Get the caller arg num corresponding to this callee arg.
        // Note that these two args share the same stack slot. Therefore,
        // if there are further uses of corresponding caller arg, we need
        // to move it to a temp and use the temp in this call tree.
        //
        // Note that Caller is guaranteed to have a param corresponding to
        // this Callee's arg since fast tail call mechanism counts the
        // stack slots required for both Caller and Callee for passing params
        // and allow fast tail call only if stack slots required by Caller >=
        // Callee.
        fgArgTabEntryPtr argTabEntry = comp->gtArgEntryByNode(call, putArgStkNode);
        assert(argTabEntry);
        unsigned callerArgNum = argTabEntry->argNum - calleeNonStandardArgCount;
        noway_assert(callerArgNum < comp->info.compArgsCount);

        unsigned   callerArgLclNum = callerArgNum;
        LclVarDsc* callerArgDsc    = comp->lvaTable + callerArgLclNum;
        if (callerArgDsc->lvPromoted)
        {
            callerArgLclNum =
                callerArgDsc->lvFieldLclStart; // update the callerArgNum to the promoted struct field's lclNum
            callerArgDsc = comp->lvaTable + callerArgLclNum;
        }
        noway_assert(callerArgDsc->lvIsParam);

        // Start searching in execution order list till we encounter call node
        unsigned  tmpLclNum = BAD_VAR_NUM;
        var_types tmpType   = TYP_UNDEF;
        for (GenTreePtr treeNode = putArgStkNode->gtNext; treeNode != call; treeNode = treeNode->gtNext)
        {
            if (treeNode->OperIsLocal() || treeNode->OperIsLocalAddr())
            {
                // This should neither be a GT_REG_VAR nor GT_PHI_ARG.
                assert((treeNode->OperGet() != GT_REG_VAR) && (treeNode->OperGet() != GT_PHI_ARG));

                GenTreeLclVarCommon* lcl    = treeNode->AsLclVarCommon();
                LclVarDsc*           lclVar = &comp->lvaTable[lcl->gtLclNum];

                // Fast tail calling criteria permits passing of structs of size 1, 2, 4 and 8 as args.
                // It is possible that the callerArgLclNum corresponds to such a struct whose stack slot
                // is getting over-written by setting up of a stack arg and there are further uses of
                // any of its fields if such a struct is type-dependently promoted.  In this case too
                // we need to introduce a temp.
                if ((lcl->gtLclNum == callerArgNum) || (lcl->gtLclNum == callerArgLclNum))
                {
                    // Create tmp and use it in place of callerArgDsc
                    if (tmpLclNum == BAD_VAR_NUM)
                    {
                        tmpLclNum = comp->lvaGrabTemp(
                            true DEBUGARG("Fast tail call lowering is creating a new local variable"));
                        comp->lvaSortAgain                 = true;
                        tmpType                            = genActualType(callerArgDsc->lvaArgType());
                        comp->lvaTable[tmpLclNum].lvType   = tmpType;
                        comp->lvaTable[tmpLclNum].lvRefCnt = 1;
                    }

                    lcl->SetLclNum(tmpLclNum);
                    lcl->SetOper(GT_LCL_VAR);
                }
            }
        }

        // If we have created a temp, insert an embedded assignment stmnt before
        // the first putargStkNode i.e.
        //     tmpLcl = CallerArg
        if (tmpLclNum != BAD_VAR_NUM)
        {
            assert(tmpType != TYP_UNDEF);
            GenTreeLclVar* local =
                new (comp, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, tmpType, callerArgLclNum, BAD_IL_OFFSET);
            GenTree* assignExpr = comp->gtNewTempAssign(tmpLclNum, local);
            BlockRange().InsertBefore(firstPutArgStk, LIR::SeqTree(comp, assignExpr));
        }
    }

    // Insert GT_START_NONGC node before the first GT_PUTARG_STK node.
    // Note that if there are no args to be setup on stack, no need to
    // insert GT_START_NONGC node.
    GenTreePtr startNonGCNode = nullptr;
    if (firstPutArgStk != nullptr)
    {
        startNonGCNode = new (comp, GT_START_NONGC) GenTree(GT_START_NONGC, TYP_VOID);
        BlockRange().InsertBefore(firstPutArgStk, startNonGCNode);

        // Gc-interruptability in the following case:
        //     foo(a, b, c, d, e) { bar(a, b, c, d, e); }
        //     bar(a, b, c, d, e) { foo(a, b, d, d, e); }
        //
        // Since the instruction group starting from the instruction that sets up first
        // stack arg to the end of the tail call is marked as non-gc interruptible,
        // this will form a non-interruptible tight loop causing gc-starvation. To fix
        // this we insert GT_NO_OP as embedded stmt before GT_START_NONGC, if the method
        // has a single basic block and is not a GC-safe point.  The presence of a single
        // nop outside non-gc interruptible region will prevent gc starvation.
        if ((comp->fgBBcount == 1) && !(comp->compCurBB->bbFlags & BBF_GC_SAFE_POINT))
        {
            assert(comp->fgFirstBB == comp->compCurBB);
            GenTreePtr noOp = new (comp, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);
            BlockRange().InsertBefore(startNonGCNode, noOp);
        }
    }

    // Insert GT_PROF_HOOK node to emit profiler tail call hook. This should be
    // inserted before the args are setup but after the side effects of args are
    // computed. That is, GT_PROF_HOOK node needs to be inserted before GT_START_NONGC
    // node if one exists.
    if (comp->compIsProfilerHookNeeded())
    {
        InsertProfTailCallHook(call, startNonGCNode);
    }

#else // !FEATURE_FASTTAILCALL

    // Platform choose not to implement fast tail call mechanism.
    // In such a case we should never be reaching this method as
    // the expectation is that IsTailCallViaHelper() will always
    // be true on such a platform.
    unreached();
#endif
}

//------------------------------------------------------------------------
// LowerTailCallViaHelper: lower a call via the tailcall helper. Morph
// has already inserted tailcall helper special arguments. This function
// inserts actual data for some placeholders.
//
// For AMD64, lower
//      tail.call(void* copyRoutine, void* dummyArg, ...)
// as
//      Jit_TailCall(void* copyRoutine, void* callTarget, ...)
//
// For x86, lower
//      tail.call(<function args>, int numberOfOldStackArgs, int dummyNumberOfNewStackArgs, int flags, void* dummyArg)
// as
//      JIT_TailCall(<function args>, int numberOfOldStackArgsWords, int numberOfNewStackArgsWords, int flags, void*
//      callTarget)
// Note that the special arguments are on the stack, whereas the function arguments follow the normal convention.
//
// Also inserts PInvoke method epilog if required.
//
// Arguments:
//    call         -  The call node
//    callTarget   -  The real call target. This is used to replace the dummyArg during lowering.
//
// Return Value:
//    Returns control expression tree for making a call to helper Jit_TailCall.
//
GenTree* Lowering::LowerTailCallViaHelper(GenTreeCall* call, GenTree* callTarget)
{
    // Tail call restrictions i.e. conditions under which tail prefix is ignored.
    // Most of these checks are already done by importer or fgMorphTailCall().
    // This serves as a double sanity check.
    assert((comp->info.compFlags & CORINFO_FLG_SYNCH) == 0); // tail calls from synchronized methods
    assert(!comp->opts.compNeedSecurityCheck);               // tail call from methods that need security check
    assert(!call->IsUnmanaged());                            // tail calls to unamanaged methods
    assert(!comp->compLocallocUsed);                         // tail call from methods that also do localloc
    assert(!comp->getNeedsGSSecurityCookie());               // jit64 compat: tail calls from methods that need GS check

    // We expect to see a call that meets the following conditions
    assert(call->IsTailCallViaHelper());
    assert(callTarget != nullptr);

    // The TailCall helper call never returns to the caller and is not GC interruptible.
    // Therefore the block containing the tail call should be a GC safe point to avoid
    // GC starvation.
    assert(comp->compCurBB->bbFlags & BBF_GC_SAFE_POINT);

    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.  This is a case of caller method has both PInvokes and tail calls.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(call));
    }

    // Remove gtCallAddr from execution order if present.
    if (call->gtCallType == CT_INDIRECT)
    {
        assert(call->gtCallAddr != nullptr);

        bool               isClosed;
        LIR::ReadOnlyRange callAddrRange = BlockRange().GetTreeRange(call->gtCallAddr, &isClosed);
        assert(isClosed);

        BlockRange().Remove(std::move(callAddrRange));
    }

    // The callTarget tree needs to be sequenced.
    LIR::Range callTargetRange = LIR::SeqTree(comp, callTarget);

    fgArgTabEntry* argEntry;

#if defined(_TARGET_AMD64_)

// For AMD64, first argument is CopyRoutine and second argument is a place holder node.

#ifdef DEBUG
    argEntry = comp->gtArgEntryByArgNum(call, 0);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_REG);
    GenTree* firstArg = argEntry->node->gtOp.gtOp1;
    assert(firstArg->gtOper == GT_CNS_INT);
#endif

    // Replace second arg by callTarget.
    argEntry = comp->gtArgEntryByArgNum(call, 1);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_REG);
    GenTree* secondArg = argEntry->node->gtOp.gtOp1;

    BlockRange().InsertAfter(secondArg, std::move(callTargetRange));

    bool               isClosed;
    LIR::ReadOnlyRange secondArgRange = BlockRange().GetTreeRange(secondArg, &isClosed);
    assert(isClosed);

    BlockRange().Remove(std::move(secondArgRange));

    argEntry->node->gtOp.gtOp1 = callTarget;

#elif defined(_TARGET_X86_)

    // Verify the special args are what we expect, and replace the dummy args with real values.
    // We need to figure out the size of the outgoing stack arguments, not including the special args.
    // The number of 4-byte words is passed to the helper for the incoming and outgoing argument sizes.
    // This number is exactly the next slot number in the call's argument info struct.
    unsigned nNewStkArgsWords = call->fgArgInfo->GetNextSlotNum();
    assert(nNewStkArgsWords >= 4); // There must be at least the four special stack args.
    nNewStkArgsWords -= 4;

    unsigned numArgs = call->fgArgInfo->ArgCount();

    // arg 0 == callTarget.
    argEntry = comp->gtArgEntryByArgNum(call, numArgs - 1);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_STK);
    GenTree* arg0 = argEntry->node->gtOp.gtOp1;

    BlockRange().InsertAfter(arg0, std::move(callTargetRange));

    bool               isClosed;
    LIR::ReadOnlyRange secondArgRange = BlockRange().GetTreeRange(arg0, &isClosed);
    assert(isClosed);

    argEntry->node->gtOp.gtOp1 = callTarget;

    // arg 1 == flags
    argEntry = comp->gtArgEntryByArgNum(call, numArgs - 2);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_STK);
    GenTree* arg1 = argEntry->node->gtOp.gtOp1;
    assert(arg1->gtOper == GT_CNS_INT);

    ssize_t tailCallHelperFlags = 1 |                                  // always restore EDI,ESI,EBX
                                  (call->IsVirtualStub() ? 0x2 : 0x0); // Stub dispatch flag
    arg1->gtIntCon.gtIconVal = tailCallHelperFlags;

    // arg 2 == numberOfNewStackArgsWords
    argEntry = comp->gtArgEntryByArgNum(call, numArgs - 3);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_STK);
    GenTree* arg2 = argEntry->node->gtOp.gtOp1;
    assert(arg2->gtOper == GT_CNS_INT);

    arg2->gtIntCon.gtIconVal = nNewStkArgsWords;

#ifdef DEBUG
    // arg 3 == numberOfOldStackArgsWords
    argEntry = comp->gtArgEntryByArgNum(call, numArgs - 4);
    assert(argEntry != nullptr);
    assert(argEntry->node->gtOper == GT_PUTARG_STK);
    GenTree* arg3 = argEntry->node->gtOp.gtOp1;
    assert(arg3->gtOper == GT_CNS_INT);
#endif // DEBUG

#else
    NYI("LowerTailCallViaHelper");
#endif // _TARGET_*

    // Transform this call node into a call to Jit tail call helper.
    call->gtCallType    = CT_HELPER;
    call->gtCallMethHnd = comp->eeFindHelper(CORINFO_HELP_TAILCALL);
    call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;

    // Lower this as if it were a pure helper call.
    call->gtCallMoreFlags &= ~(GTF_CALL_M_TAILCALL | GTF_CALL_M_TAILCALL_VIA_HELPER);
    GenTree* result = LowerDirectCall(call);

    // Now add back tail call flags for identifying this node as tail call dispatched via helper.
    call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL | GTF_CALL_M_TAILCALL_VIA_HELPER;

    // Insert profiler tail call hook if needed.
    // Since we don't know the insertion point, pass null for second param.
    if (comp->compIsProfilerHookNeeded())
    {
        InsertProfTailCallHook(call, nullptr);
    }

    return result;
}

// Lower "jmp <method>" tail call to insert PInvoke method epilog if required.
void Lowering::LowerJmpMethod(GenTree* jmp)
{
    assert(jmp->OperGet() == GT_JMP);

    JITDUMP("lowering GT_JMP\n");
    DISPNODE(jmp);
    JITDUMP("============");

    // If PInvokes are in-lined, we have to remember to execute PInvoke method epilog anywhere that
    // a method returns.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(jmp));
    }
}

// Lower GT_RETURN node to insert PInvoke method epilog if required.
void Lowering::LowerRet(GenTree* ret)
{
    assert(ret->OperGet() == GT_RETURN);

    JITDUMP("lowering GT_RETURN\n");
    DISPNODE(ret);
    JITDUMP("============");

    // Method doing PInvokes has exactly one return block unless it has tail calls.
    if (comp->info.compCallUnmanaged && (comp->compCurBB == comp->genReturnBB))
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(ret));
    }
}

GenTree* Lowering::LowerDirectCall(GenTreeCall* call)
{
    noway_assert(call->gtCallType == CT_USER_FUNC || call->gtCallType == CT_HELPER);

    // Don't support tail calling helper methods.
    // But we might encounter tail calls dispatched via JIT helper appear as a tail call to helper.
    noway_assert(!call->IsTailCall() || call->IsTailCallViaHelper() || call->gtCallType == CT_USER_FUNC);

    // Non-virtual direct/indirect calls: Work out if the address of the
    // call is known at JIT time.  If not it is either an indirect call
    // or the address must be accessed via an single/double indirection.

    void*           addr;
    InfoAccessType  accessType;
    CorInfoHelpFunc helperNum = comp->eeGetHelperNum(call->gtCallMethHnd);

#ifdef FEATURE_READYTORUN_COMPILER
    if (call->gtEntryPoint.addr != nullptr)
    {
        accessType = call->gtEntryPoint.accessType;
        addr       = call->gtEntryPoint.addr;
    }
    else
#endif
        if (call->gtCallType == CT_HELPER)
    {
        noway_assert(helperNum != CORINFO_HELP_UNDEF);

        // the convention on getHelperFtn seems to be (it's not documented)
        // that it returns an address or if it returns null, pAddr is set to
        // another address, which requires an indirection
        void* pAddr;
        addr = comp->info.compCompHnd->getHelperFtn(helperNum, (void**)&pAddr);

        if (addr != nullptr)
        {
            accessType = IAT_VALUE;
        }
        else
        {
            accessType = IAT_PVALUE;
            addr       = pAddr;
        }
    }
    else
    {
        noway_assert(helperNum == CORINFO_HELP_UNDEF);

        CORINFO_ACCESS_FLAGS aflags = CORINFO_ACCESS_ANY;

        if (call->IsSameThis())
        {
            aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_THIS);
        }

        if (!call->NeedsNullCheck())
        {
            aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_NONNULL);
        }

        CORINFO_CONST_LOOKUP addrInfo;
        comp->info.compCompHnd->getFunctionEntryPoint(call->gtCallMethHnd, &addrInfo, aflags);

        accessType = addrInfo.accessType;
        addr       = addrInfo.addr;
    }

    GenTree* result = nullptr;
    switch (accessType)
    {
        case IAT_VALUE:
            // Non-virtual direct call to known address
            if (!IsCallTargetInRange(addr) || call->IsTailCall())
            {
                result = AddrGen(addr);
            }
            else
            {
                // a direct call within range of hardware relative call instruction
                // stash the address for codegen
                call->gtDirectCallAddress = addr;
            }
            break;

        case IAT_PVALUE:
        {
            // Non-virtual direct calls to addresses accessed by
            // a single indirection.
            GenTree* cellAddr = AddrGen(addr);
            GenTree* indir    = Ind(cellAddr);

#ifdef FEATURE_READYTORUN_COMPILER
#ifdef _TARGET_ARM64_
            // For arm64, we dispatch code same as VSD using X11 for indirection cell address,
            // which ZapIndirectHelperThunk expects.
            if (call->IsR2RRelativeIndir())
            {
                cellAddr->gtRegNum = REG_R2R_INDIRECT_PARAM;
                indir->gtRegNum    = REG_JUMP_THUNK_PARAM;
            }
#endif
#endif
            result = indir;
            break;
        }

        case IAT_PPVALUE:
            // Non-virtual direct calls to addresses accessed by
            // a double indirection.
            //
            // Double-indirection. Load the address into a register
            // and call indirectly through the register
            noway_assert(helperNum == CORINFO_HELP_UNDEF);
            result = AddrGen(addr);
            result = Ind(Ind(result));
            break;

        default:
            noway_assert(!"Bad accessType");
            break;
    }

    return result;
}

GenTree* Lowering::LowerDelegateInvoke(GenTreeCall* call)
{
    noway_assert(call->gtCallType == CT_USER_FUNC);

    assert((comp->info.compCompHnd->getMethodAttribs(call->gtCallMethHnd) &
            (CORINFO_FLG_DELEGATE_INVOKE | CORINFO_FLG_FINAL)) == (CORINFO_FLG_DELEGATE_INVOKE | CORINFO_FLG_FINAL));

    GenTree* thisArgNode;
    if (call->IsTailCallViaHelper())
    {
#ifdef _TARGET_X86_ // x86 tailcall via helper follows normal calling convention, but with extra stack args.
        const unsigned argNum = 0;
#else  // !_TARGET_X86_
        // In case of helper dispatched tail calls, "thisptr" will be the third arg.
        // The first two args are: real call target and addr of args copy routine.
        const unsigned argNum  = 2;
#endif // !_TARGET_X86_

        fgArgTabEntryPtr thisArgTabEntry = comp->gtArgEntryByArgNum(call, argNum);
        thisArgNode                      = thisArgTabEntry->node;
    }
    else
    {
        thisArgNode = comp->gtGetThisArg(call);
    }

    assert(thisArgNode->gtOper == GT_PUTARG_REG);
    GenTree* originalThisExpr = thisArgNode->gtOp.gtOp1;

    // We're going to use the 'this' expression multiple times, so make a local to copy it.

    unsigned lclNum;

#ifdef _TARGET_X86_
    if (call->IsTailCallViaHelper() && originalThisExpr->IsLocal())
    {
        // For ordering purposes for the special tailcall arguments on x86, we forced the
        // 'this' pointer in this case to a local in Compiler::fgMorphTailCall().
        // We could possibly use this case to remove copies for all architectures and non-tailcall
        // calls by creating a new lcl var or lcl field reference, as is done in the
        // LowerVirtualVtableCall() code.
        assert(originalThisExpr->OperGet() == GT_LCL_VAR);
        lclNum = originalThisExpr->AsLclVarCommon()->GetLclNum();
    }
    else
#endif // _TARGET_X86_
    {
        unsigned delegateInvokeTmp = comp->lvaGrabTemp(true DEBUGARG("delegate invoke call"));

        LIR::Use thisExprUse(BlockRange(), &thisArgNode->gtOp.gtOp1, thisArgNode);
        thisExprUse.ReplaceWithLclVar(comp, m_block->getBBWeight(comp), delegateInvokeTmp);

        originalThisExpr = thisExprUse.Def(); // it's changed; reload it.
        lclNum           = delegateInvokeTmp;
    }

    // replace original expression feeding into thisPtr with
    // [originalThis + offsetOfDelegateInstance]

    GenTree* newThisAddr = new (comp, GT_LEA)
        GenTreeAddrMode(TYP_REF, originalThisExpr, nullptr, 0, comp->eeGetEEInfo()->offsetOfDelegateInstance);

    GenTree* newThis = comp->gtNewOperNode(GT_IND, TYP_REF, newThisAddr);

    BlockRange().InsertAfter(originalThisExpr, newThisAddr, newThis);

    thisArgNode->gtOp.gtOp1 = newThis;

    // the control target is
    // [originalThis + firstTgtOffs]

    GenTree* base = new (comp, GT_LCL_VAR) GenTreeLclVar(originalThisExpr->TypeGet(), lclNum, BAD_IL_OFFSET);

    unsigned targetOffs = comp->eeGetEEInfo()->offsetOfDelegateFirstTarget;
    GenTree* result     = new (comp, GT_LEA) GenTreeAddrMode(TYP_REF, base, nullptr, 0, targetOffs);
    GenTree* callTarget = Ind(result);

    // don't need to sequence and insert this tree, caller will do it

    return callTarget;
}

GenTree* Lowering::LowerIndirectNonvirtCall(GenTreeCall* call)
{
#ifdef _TARGET_X86_
    if (call->gtCallCookie != nullptr)
    {
        NYI_X86("Morphing indirect non-virtual call with non-standard args");
    }
#endif

    // Indirect cookie calls gets transformed by fgMorphArgs as indirect call with non-standard args.
    // Hence we should never see this type of call in lower.

    noway_assert(call->gtCallCookie == nullptr);

    return nullptr;
}

//------------------------------------------------------------------------
// CreateReturnTrapSeq: Create a tree to perform a "return trap", used in PInvoke
// epilogs to invoke a GC under a condition. The return trap checks some global
// location (the runtime tells us where that is and how many indirections to make),
// then, based on the result, conditionally calls a GC helper. We use a special node
// for this because at this time (late in the compilation phases), introducing flow
// is tedious/difficult.
//
// This is used for PInvoke inlining.
//
// Return Value:
//    Code tree to perform the action.
//
GenTree* Lowering::CreateReturnTrapSeq()
{
    // The GT_RETURNTRAP node expands to this:
    //    if (g_TrapReturningThreads)
    //    {
    //       RareDisablePreemptiveGC();
    //    }

    // The only thing to do here is build up the expression that evaluates 'g_TrapReturningThreads'.

    void* pAddrOfCaptureThreadGlobal = nullptr;
    LONG* addrOfCaptureThreadGlobal = comp->info.compCompHnd->getAddrOfCaptureThreadGlobal(&pAddrOfCaptureThreadGlobal);

    GenTree* testTree;
    if (addrOfCaptureThreadGlobal != nullptr)
    {
        testTree = Ind(AddrGen(addrOfCaptureThreadGlobal));
    }
    else
    {
        testTree = Ind(Ind(AddrGen(pAddrOfCaptureThreadGlobal)));
    }
    return comp->gtNewOperNode(GT_RETURNTRAP, TYP_INT, testTree);
}

//------------------------------------------------------------------------
// SetGCState: Create a tree that stores the given constant (0 or 1) into the
// thread's GC state field.
//
// This is used for PInvoke inlining.
//
// Arguments:
//    state - constant (0 or 1) to store into the thread's GC state field.
//
// Return Value:
//    Code tree to perform the action.
//
GenTree* Lowering::SetGCState(int state)
{
    // Thread.offsetOfGcState = 0/1

    assert(state == 0 || state == 1);

    const CORINFO_EE_INFO* pInfo = comp->eeGetEEInfo();

    GenTree* base = new (comp, GT_LCL_VAR) GenTreeLclVar(TYP_I_IMPL, comp->info.compLvFrameListRoot, -1);

    GenTree* storeGcState = new (comp, GT_STOREIND)
        GenTreeStoreInd(TYP_BYTE,
                        new (comp, GT_LEA) GenTreeAddrMode(TYP_I_IMPL, base, nullptr, 1, pInfo->offsetOfGCState),
                        new (comp, GT_CNS_INT) GenTreeIntCon(TYP_BYTE, state));

    return storeGcState;
}

//------------------------------------------------------------------------
// CreateFrameLinkUpdate: Create a tree that either links or unlinks the
// locally-allocated InlinedCallFrame from the Frame list.
//
// This is used for PInvoke inlining.
//
// Arguments:
//    action - whether to link (push) or unlink (pop) the Frame
//
// Return Value:
//    Code tree to perform the action.
//
GenTree* Lowering::CreateFrameLinkUpdate(FrameLinkAction action)
{
    const CORINFO_EE_INFO*                       pInfo         = comp->eeGetEEInfo();
    const CORINFO_EE_INFO::InlinedCallFrameInfo& callFrameInfo = pInfo->inlinedCallFrameInfo;

    GenTree* TCB = new (comp, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, TYP_I_IMPL, comp->info.compLvFrameListRoot,
                                                        (IL_OFFSET)-1); // cast to resolve ambiguity.

    // Thread->m_pFrame
    GenTree* addr = new (comp, GT_LEA) GenTreeAddrMode(TYP_I_IMPL, TCB, nullptr, 1, pInfo->offsetOfThreadFrame);

    GenTree* data = nullptr;

    if (action == PushFrame)
    {
        // Thread->m_pFrame = &inlinedCallFrame;
        data = new (comp, GT_LCL_FLD_ADDR)
            GenTreeLclFld(GT_LCL_FLD_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfFrameVptr);
    }
    else
    {
        assert(action == PopFrame);
        // Thread->m_pFrame = inlinedCallFrame.m_pNext;

        data = new (comp, GT_LCL_FLD) GenTreeLclFld(GT_LCL_FLD, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar,
                                                    pInfo->inlinedCallFrameInfo.offsetOfFrameLink);
    }
    GenTree* storeInd = new (comp, GT_STOREIND) GenTreeStoreInd(TYP_I_IMPL, addr, data);
    return storeInd;
}

//------------------------------------------------------------------------
// InsertPInvokeMethodProlog: Create the code that runs at the start of
// every method that has PInvoke calls.
//
// Initialize the TCB local and the InlinedCallFrame object. Then link ("push")
// the InlinedCallFrame object on the Frame chain. The layout of InlinedCallFrame
// is defined in vm/frames.h. See also vm/jitinterface.cpp for more information.
// The offsets of these fields is returned by the VM in a call to ICorStaticInfo::getEEInfo().
//
// The (current) layout is as follows:
//
//  64-bit  32-bit                                    CORINFO_EE_INFO
//  offset  offset  field name                        offset                  when set
//  -----------------------------------------------------------------------------------------
//  +00h    +00h    GS cookie                         offsetOfGSCookie
//  +08h    +04h    vptr for class InlinedCallFrame   offsetOfFrameVptr       method prolog
//  +10h    +08h    m_Next                            offsetOfFrameLink       method prolog
//  +18h    +0Ch    m_Datum                           offsetOfCallTarget      call site
//  +20h    n/a     m_StubSecretArg                                           not set by JIT
//  +28h    +10h    m_pCallSiteSP                     offsetOfCallSiteSP      x86: call site, and zeroed in method
//                                                                              prolog;
//                                                                            non-x86: method prolog (SP remains
//                                                                              constant in function, after prolog: no
//                                                                              localloc and PInvoke in same function)
//  +30h    +14h    m_pCallerReturnAddress            offsetOfReturnAddress   call site
//  +38h    +18h    m_pCalleeSavedFP                  offsetOfCalleeSavedFP   not set by JIT
//          +1Ch    JIT retval spill area (int)                               before call_gc    ???
//          +20h    JIT retval spill area (long)                              before call_gc    ???
//          +24h    Saved value of EBP                                        method prolog     ???
//
// Note that in the VM, InlinedCallFrame is a C++ class whose objects have a 'this' pointer that points
// to the InlinedCallFrame vptr (the 2nd field listed above), and the GS cookie is stored *before*
// the object. When we link the InlinedCallFrame onto the Frame chain, we must point at this location,
// and not at the beginning of the InlinedCallFrame local, which is actually the GS cookie.
//
// Return Value:
//    none
//
void Lowering::InsertPInvokeMethodProlog()
{
    noway_assert(comp->info.compCallUnmanaged);
    noway_assert(comp->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        return;
    }

    JITDUMP("======= Inserting PInvoke method prolog\n");

    LIR::Range& firstBlockRange = LIR::AsRange(comp->fgFirstBB);

    const CORINFO_EE_INFO*                       pInfo         = comp->eeGetEEInfo();
    const CORINFO_EE_INFO::InlinedCallFrameInfo& callFrameInfo = pInfo->inlinedCallFrameInfo;

    // First arg:  &compiler->lvaInlinedPInvokeFrameVar + callFrameInfo.offsetOfFrameVptr

    GenTree* frameAddr = new (comp, GT_LCL_FLD_ADDR)
        GenTreeLclFld(GT_LCL_FLD_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfFrameVptr);

    // Call runtime helper to fill in our InlinedCallFrame and push it on the Frame list:
    //     TCB = CORINFO_HELP_INIT_PINVOKE_FRAME(&symFrameStart, secretArg);
    // for x86, don't pass the secretArg.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_X86_
    GenTreeArgList* argList = comp->gtNewArgList(frameAddr);
#else  // !_TARGET_X86_
    GenTreeArgList*    argList = comp->gtNewArgList(frameAddr, PhysReg(REG_SECRET_STUB_PARAM));
#endif // !_TARGET_X86_

    GenTree* call = comp->gtNewHelperCallNode(CORINFO_HELP_INIT_PINVOKE_FRAME, TYP_I_IMPL, 0, argList);

    // some sanity checks on the frame list root vardsc
    LclVarDsc* varDsc = &comp->lvaTable[comp->info.compLvFrameListRoot];
    noway_assert(!varDsc->lvIsParam);
    noway_assert(varDsc->lvType == TYP_I_IMPL);

    GenTree* store =
        new (comp, GT_STORE_LCL_VAR) GenTreeLclVar(GT_STORE_LCL_VAR, TYP_I_IMPL, comp->info.compLvFrameListRoot,
                                                   (IL_OFFSET)-1); // cast to resolve ambiguity.
    store->gtOp.gtOp1 = call;
    store->gtFlags |= GTF_VAR_DEF;

    GenTree* insertionPoint = firstBlockRange.FirstNonPhiOrCatchArgNode();

    comp->fgMorphTree(store);
    firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, store));
    DISPTREERANGE(firstBlockRange, store);

#ifndef _TARGET_X86_ // For x86, this step is done at the call site (due to stack pointer not being static in the
                     // function).

    // --------------------------------------------------------
    // InlinedCallFrame.m_pCallSiteSP = @RSP;

    GenTreeLclFld* storeSP = new (comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfCallSiteSP);
    storeSP->gtOp1 = PhysReg(REG_SPBASE);

    firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, storeSP));
    DISPTREERANGE(firstBlockRange, storeSP);

#endif // !_TARGET_X86_

    // --------------------------------------------------------
    // InlinedCallFrame.m_pCalleeSavedEBP = @RBP;

    GenTreeLclFld* storeFP =
        new (comp, GT_STORE_LCL_FLD) GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar,
                                                   callFrameInfo.offsetOfCalleeSavedFP);
    storeFP->gtOp1 = PhysReg(REG_FPBASE);

    firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, storeFP));
    DISPTREERANGE(firstBlockRange, storeFP);

    // --------------------------------------------------------

    if (comp->opts.eeFlags & CORJIT_FLG_IL_STUB)
    {
        // Push a frame - if we are NOT in an IL stub, this is done right before the call
        // The init routine sets InlinedCallFrame's m_pNext, so we just set the thead's top-of-stack
        GenTree* frameUpd = CreateFrameLinkUpdate(PushFrame);
        firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, frameUpd));
        DISPTREERANGE(firstBlockRange, frameUpd);
    }
}

//------------------------------------------------------------------------
// InsertPInvokeMethodEpilog: Code that needs to be run when exiting any method
// that has PInvoke inlines. This needs to be inserted any place you can exit the
// function: returns, tailcalls and jmps.
//
// Arguments:
//    returnBB   -  basic block from which a method can return
//    lastExpr   -  GenTree of the last top level stmnt of returnBB (debug only arg)
//
// Return Value:
//    Code tree to perform the action.
//
void Lowering::InsertPInvokeMethodEpilog(BasicBlock* returnBB DEBUGARG(GenTreePtr lastExpr))
{
    assert(returnBB != nullptr);
    assert(comp->info.compCallUnmanaged);

    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        return;
    }

    JITDUMP("======= Inserting PInvoke method epilog\n");

    // Method doing PInvoke calls has exactly one return block unless it has "jmp" or tail calls.
    assert(((returnBB == comp->genReturnBB) && (returnBB->bbJumpKind == BBJ_RETURN)) ||
           returnBB->endsWithTailCallOrJmp(comp));

    LIR::Range& returnBlockRange = LIR::AsRange(returnBB);

    GenTree* insertionPoint = returnBlockRange.LastNode();
    assert(insertionPoint == lastExpr);

    // Note: PInvoke Method Epilog (PME) needs to be inserted just before GT_RETURN, GT_JMP or GT_CALL node in execution
    // order so that it is guaranteed that there will be no further PInvokes after that point in the method.
    //
    // Example1: GT_RETURN(op1) - say execution order is: Op1, GT_RETURN.  After inserting PME, execution order would be
    //           Op1, PME, GT_RETURN
    //
    // Example2: GT_CALL(arg side effect computing nodes, Stk Args Setup, Reg Args setup). The execution order would be
    //           arg side effect computing nodes, Stk Args setup, Reg Args setup, GT_CALL
    //           After inserting PME execution order would be:
    //           arg side effect computing nodes, Stk Args setup, Reg Args setup, PME, GT_CALL
    //
    // Example3: GT_JMP.  After inserting PME execution order would be: PME, GT_JMP
    //           That is after PME, args for GT_JMP call will be setup.

    // TODO-Cleanup: setting GCState to 1 seems to be redundant as InsertPInvokeCallProlog will set it to zero before a
    // PInvoke call and InsertPInvokeCallEpilog() will set it back to 1 after the PInvoke.  Though this is redundant,
    // it is harmeless.
    // Note that liveness is artificially extending the life of compLvFrameListRoot var if the method being compiled has
    // PInvokes.  Deleting the below stmnt would cause an an assert in lsra.cpp::SetLastUses() since compLvFrameListRoot
    // will be live-in to a BBJ_RETURN block without any uses.  Long term we need to fix liveness for x64 case to
    // properly extend the life of compLvFrameListRoot var.
    //
    // Thread.offsetOfGcState = 0/1
    // That is [tcb + offsetOfGcState] = 1
    GenTree* storeGCState = SetGCState(1);
    returnBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, storeGCState));

    if (comp->opts.eeFlags & CORJIT_FLG_IL_STUB)
    {
        // Pop the frame, in non-stubs we do this around each PInvoke call
        GenTree* frameUpd = CreateFrameLinkUpdate(PopFrame);
        returnBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, frameUpd));
    }
}

//------------------------------------------------------------------------
// InsertPInvokeCallProlog: Emit the call-site prolog for direct calls to unmanaged code.
// It does all the necessary call-site setup of the InlinedCallFrame.
//
// Arguments:
//    call - the call for which we are inserting the PInvoke prolog.
//
// Return Value:
//    None.
//
void Lowering::InsertPInvokeCallProlog(GenTreeCall* call)
{
    JITDUMP("======= Inserting PInvoke call prolog\n");

    GenTree* insertBefore = call;
    if (call->gtCallType == CT_INDIRECT)
    {
        bool isClosed;
        insertBefore = BlockRange().GetTreeRange(call->gtCallAddr, &isClosed).FirstNode();
        assert(isClosed);
    }

    const CORINFO_EE_INFO::InlinedCallFrameInfo& callFrameInfo = comp->eeGetEEInfo()->inlinedCallFrameInfo;

    gtCallTypes callType = (gtCallTypes)call->gtCallType;

    noway_assert(comp->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

#if COR_JIT_EE_VERSION > 460
    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        // First argument is the address of the frame variable.
        GenTree* frameAddr = new (comp, GT_LCL_VAR_ADDR)
            GenTreeLclVar(GT_LCL_VAR_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, BAD_IL_OFFSET);

        // Insert call to CORINFO_HELP_JIT_PINVOKE_BEGIN
        GenTree* helperCall =
            comp->gtNewHelperCallNode(CORINFO_HELP_JIT_PINVOKE_BEGIN, TYP_VOID, 0, comp->gtNewArgList(frameAddr));

        comp->fgMorphTree(helperCall);
        BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, helperCall));
        return;
    }
#endif

    // Emit the following sequence:
    //
    // InlinedCallFrame.callTarget = methodHandle   // stored in m_Datum
    // InlinedCallFrame.m_pCallSiteSP = SP          // x86 only
    // InlinedCallFrame.m_pCallerReturnAddress = return address
    // Thread.gcState = 0
    // (non-stub) - update top Frame on TCB

    // ----------------------------------------------------------------------------------
    // Setup InlinedCallFrame.callSiteTarget (which is how the JIT refers to it).
    // The actual field is InlinedCallFrame.m_Datum which has many different uses and meanings.

    GenTree* src = nullptr;

    if (callType == CT_INDIRECT)
    {
        if (comp->info.compPublishStubParam)
        {
            src = new (comp, GT_LCL_VAR) GenTreeLclVar(TYP_I_IMPL, comp->lvaStubArgumentVar, BAD_IL_OFFSET);
        }
        // else { If we don't have secret parameter, m_Datum will be initialized by VM code }
    }
    else
    {
        assert(callType == CT_USER_FUNC);

        void*                 pEmbedMethodHandle = nullptr;
        CORINFO_METHOD_HANDLE embedMethodHandle =
            comp->info.compCompHnd->embedMethodHandle(call->gtCallMethHnd, &pEmbedMethodHandle);

        noway_assert((!embedMethodHandle) != (!pEmbedMethodHandle));

        if (embedMethodHandle != nullptr)
        {
            // InlinedCallFrame.callSiteTarget = methodHandle
            src = AddrGen(embedMethodHandle);
        }
        else
        {
            // InlinedCallFrame.callSiteTarget = *pEmbedMethodHandle
            src = Ind(AddrGen(pEmbedMethodHandle));
        }
    }

    if (src != nullptr)
    {
        // Store into InlinedCallFrame.m_Datum, the offset of which is given by offsetOfCallTarget.
        GenTreeLclFld* store =
            new (comp, GT_STORE_LCL_FLD) GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar,
                                                       callFrameInfo.offsetOfCallTarget);
        store->gtOp1 = src;

        BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, store));
    }

#ifdef _TARGET_X86_

    // ----------------------------------------------------------------------------------
    // InlinedCallFrame.m_pCallSiteSP = SP

    GenTreeLclFld* storeCallSiteSP = new (comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfCallSiteSP);

    storeCallSiteSP->gtOp1 = PhysReg(REG_SPBASE);

    BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, storeCallSiteSP));

#endif

    // ----------------------------------------------------------------------------------
    // InlinedCallFrame.m_pCallerReturnAddress = &label (the address of the instruction immediately following the call)

    GenTreeLclFld* storeLab =
        new (comp, GT_STORE_LCL_FLD) GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar,
                                                   callFrameInfo.offsetOfReturnAddress);

    // We don't have a real label, and inserting one is hard (even if we made a special node),
    // so for now we will just 'know' what this means in codegen.
    GenTreeLabel* labelRef = new (comp, GT_LABEL) GenTreeLabel(nullptr);
    labelRef->gtType       = TYP_I_IMPL;
    storeLab->gtOp1        = labelRef;

    BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, storeLab));

    if (!(comp->opts.eeFlags & CORJIT_FLG_IL_STUB))
    {
        // Set the TCB's frame to be the one we just created.
        // Note the init routine for the InlinedCallFrame (CORINFO_HELP_INIT_PINVOKE_FRAME)
        // has prepended it to the linked list to maintain the stack of Frames.
        //
        // Stubs do this once per stub, not once per call.
        GenTree* frameUpd = CreateFrameLinkUpdate(PushFrame);
        BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, frameUpd));
    }

    // IMPORTANT **** This instruction must come last!!! ****
    // It changes the thread's state to Preemptive mode
    // ----------------------------------------------------------------------------------
    //  [tcb + offsetOfGcState] = 0

    GenTree* storeGCState = SetGCState(0);
    BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, storeGCState));
}

//------------------------------------------------------------------------
// InsertPInvokeCallEpilog: Insert the code that goes after every inlined pinvoke call.
//
// Arguments:
//    call - the call for which we are inserting the PInvoke epilog.
//
// Return Value:
//    None.
//
void Lowering::InsertPInvokeCallEpilog(GenTreeCall* call)
{
    JITDUMP("======= Inserting PInvoke call epilog\n");

#if COR_JIT_EE_VERSION > 460
    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        noway_assert(comp->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

        // First argument is the address of the frame variable.
        GenTree* frameAddr =
            new (comp, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, BAD_IL_OFFSET);
        frameAddr->gtOper = GT_LCL_VAR_ADDR;

        // Insert call to CORINFO_HELP_JIT_PINVOKE_END
        GenTree* helperCall =
            comp->gtNewHelperCallNode(CORINFO_HELP_JIT_PINVOKE_END, TYP_VOID, 0, comp->gtNewArgList(frameAddr));

        comp->fgMorphTree(helperCall);
        BlockRange().InsertAfter(call, LIR::SeqTree(comp, helperCall));
        return;
    }
#endif

    // gcstate = 1
    GenTree* insertionPoint = call->gtNext;

    GenTree* tree = SetGCState(1);
    BlockRange().InsertBefore(insertionPoint, LIR::SeqTree(comp, tree));

    tree = CreateReturnTrapSeq();
    BlockRange().InsertBefore(insertionPoint, LIR::SeqTree(comp, tree));

    // Pop the frame if necessasry
    if (!(comp->opts.eeFlags & CORJIT_FLG_IL_STUB))
    {
        tree = CreateFrameLinkUpdate(PopFrame);
        BlockRange().InsertBefore(insertionPoint, LIR::SeqTree(comp, tree));
    }
}

//------------------------------------------------------------------------
// LowerNonvirtPinvokeCall: Lower a non-virtual / indirect PInvoke call
//
// Arguments:
//    call - The call to lower.
//
// Return Value:
//    The lowered call tree.
//
GenTree* Lowering::LowerNonvirtPinvokeCall(GenTreeCall* call)
{
    // PInvoke lowering varies depending on the flags passed in by the EE. By default,
    // GC transitions are generated inline; if CORJIT_FLG2_USE_PINVOKE_HELPERS is specified,
    // GC transitions are instead performed using helper calls. Examples of each case are given
    // below. Note that the data structure that is used to store information about a call frame
    // containing any P/Invoke calls is initialized in the method prolog (see
    // InsertPInvokeMethod{Prolog,Epilog} for details).
    //
    // Inline transitions:
    //     InlinedCallFrame inlinedCallFrame;
    //
    //     ...
    //
    //     // Set up frame information
    //     inlinedCallFrame.callTarget = methodHandle;      // stored in m_Datum
    //     inlinedCallFrame.m_pCallSiteSP = SP;             // x86 only
    //     inlinedCallFrame.m_pCallerReturnAddress = &label; (the address of the instruction immediately following the
    //     call)
    //     Thread.m_pFrame = &inlinedCallFrame; (non-IL-stub only)
    //
    //     // Switch the thread's GC mode to preemptive mode
    //     thread->m_fPreemptiveGCDisabled = 0;
    //
    //     // Call the unmanaged method
    //     target();
    //
    //     // Switch the thread's GC mode back to cooperative mode
    //     thread->m_fPreemptiveGCDisabled = 1;
    //
    //     // Rendezvous with a running collection if necessary
    //     if (g_TrapReturningThreads)
    //         RareDisablePreemptiveGC();
    //
    // Transistions using helpers:
    //
    //     OpaqueFrame opaqueFrame;
    //
    //     ...
    //
    //     // Call the JIT_PINVOKE_BEGIN helper
    //     JIT_PINVOKE_BEGIN(&opaqueFrame);
    //
    //     // Call the unmanaged method
    //     target();
    //
    //     // Call the JIT_PINVOKE_END helper
    //     JIT_PINVOKE_END(&opaqueFrame);
    //
    // Note that the JIT_PINVOKE_{BEGIN.END} helpers currently use the default calling convention for the target
    // platform. They may be changed in the future such that they preserve all register values.

    GenTree* result = nullptr;
    void*    addr   = nullptr;

    // assert we have seen one of these
    noway_assert(comp->info.compCallUnmanaged != 0);

    // All code generated by this function must not contain the randomly-inserted NOPs
    // that we insert to inhibit JIT spraying in partial trust scenarios.
    // The PINVOKE_PROLOG op signals this to the code generator/emitter.

    GenTree* prolog = new (comp, GT_NOP) GenTree(GT_PINVOKE_PROLOG, TYP_VOID);
    BlockRange().InsertBefore(call, prolog);

    InsertPInvokeCallProlog(call);

    if (call->gtCallType != CT_INDIRECT)
    {
        noway_assert(call->gtCallType == CT_USER_FUNC);
        CORINFO_METHOD_HANDLE methHnd = call->gtCallMethHnd;

        CORINFO_CONST_LOOKUP lookup;
#if COR_JIT_EE_VERSION > 460
        comp->info.compCompHnd->getAddressOfPInvokeTarget(methHnd, &lookup);
#else
        void*          pIndirection;
        lookup.accessType = IAT_PVALUE;
        lookup.addr       = comp->info.compCompHnd->getAddressOfPInvokeFixup(methHnd, &pIndirection);
        if (lookup.addr == nullptr)
        {
            lookup.accessType = IAT_PPVALUE;
            lookup.addr       = pIndirection;
        }
#endif

        void* addr = lookup.addr;
        switch (lookup.accessType)
        {
            case IAT_VALUE:
                if (!IsCallTargetInRange(addr))
                {
                    result = AddrGen(addr);
                }
                else
                {
                    // a direct call within range of hardware relative call instruction
                    // stash the address for codegen
                    call->gtDirectCallAddress = addr;
#ifdef FEATURE_READYTORUN_COMPILER
                    call->gtEntryPoint.addr = nullptr;
#endif
                }
                break;

            case IAT_PVALUE:
                result = Ind(AddrGen(addr));
                break;

            case IAT_PPVALUE:
                result = Ind(Ind(AddrGen(addr)));
                break;
        }
    }

    InsertPInvokeCallEpilog(call);

    return result;
}

// Expand the code necessary to calculate the control target.
// Returns: the expression needed to calculate the control target
// May insert embedded statements
GenTree* Lowering::LowerVirtualVtableCall(GenTreeCall* call)
{
    noway_assert(call->gtCallType == CT_USER_FUNC);

    // If this is a tail call via helper, thisPtr will be the third argument.
    int       thisPtrArgNum;
    regNumber thisPtrArgReg;

#ifndef _TARGET_X86_ // x86 tailcall via helper follows normal calling convention, but with extra stack args.
    if (call->IsTailCallViaHelper())
    {
        thisPtrArgNum = 2;
        thisPtrArgReg = REG_ARG_2;
    }
    else
#endif // !_TARGET_X86_
    {
        thisPtrArgNum = 0;
        thisPtrArgReg = comp->codeGen->genGetThisArgReg(call);
    }

    // get a reference to the thisPtr being passed
    fgArgTabEntry* argEntry = comp->gtArgEntryByArgNum(call, thisPtrArgNum);
    assert(argEntry->regNum == thisPtrArgReg);
    assert(argEntry->node->gtOper == GT_PUTARG_REG);
    GenTree* thisPtr = argEntry->node->gtOp.gtOp1;

    // If what we are passing as the thisptr is not already a local, make a new local to place it in
    // because we will be creating expressions based on it.
    unsigned lclNum;
    if (thisPtr->IsLocal())
    {
        lclNum = thisPtr->gtLclVarCommon.gtLclNum;
    }
    else
    {
        // Split off the thisPtr and store to a temporary variable.
        if (vtableCallTemp == BAD_VAR_NUM)
        {
            vtableCallTemp = comp->lvaGrabTemp(true DEBUGARG("virtual vtable call"));
        }

        LIR::Use thisPtrUse(BlockRange(), &(argEntry->node->gtOp.gtOp1), argEntry->node);
        thisPtrUse.ReplaceWithLclVar(comp, m_block->getBBWeight(comp), vtableCallTemp);

        lclNum = vtableCallTemp;
    }

    // We'll introduce another use of this local so increase its ref count.
    comp->lvaTable[lclNum].incRefCnts(comp->compCurBB->getBBWeight(comp), comp);

    // If the thisPtr is a local field, then construct a local field type node
    GenTree* local;
    if (thisPtr->isLclField())
    {
        local = new (comp, GT_LCL_FLD)
            GenTreeLclFld(GT_LCL_FLD, thisPtr->TypeGet(), lclNum, thisPtr->AsLclFld()->gtLclOffs);
    }
    else
    {
        local = new (comp, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, thisPtr->TypeGet(), lclNum, BAD_IL_OFFSET);
    }

    // pointer to virtual table = [REG_CALL_THIS + offs]
    GenTree* result = Ind(Offset(local, VPTR_OFFS));

    // Get hold of the vtable offset (note: this might be expensive)
    unsigned vtabOffsOfIndirection;
    unsigned vtabOffsAfterIndirection;
    comp->info.compCompHnd->getMethodVTableOffset(call->gtCallMethHnd, &vtabOffsOfIndirection,
                                                  &vtabOffsAfterIndirection);

    // Get the appropriate vtable chunk
    // result = [REG_CALL_IND_SCRATCH + vtabOffsOfIndirection]
    result = Ind(Offset(result, vtabOffsOfIndirection));

    // Load the function address
    // result = [reg+vtabOffs]
    result = Ind(Offset(result, vtabOffsAfterIndirection));

    return result;
}

// Lower stub dispatched virtual calls.
GenTree* Lowering::LowerVirtualStubCall(GenTreeCall* call)
{
    assert((call->gtFlags & GTF_CALL_VIRT_KIND_MASK) == GTF_CALL_VIRT_STUB);

    // An x86 JIT which uses full stub dispatch must generate only
    // the following stub dispatch calls:
    //
    // (1) isCallRelativeIndirect:
    //        call dword ptr [rel32]  ;  FF 15 ---rel32----
    // (2) isCallRelative:
    //        call abc                ;     E8 ---rel32----
    // (3) isCallRegisterIndirect:
    //     3-byte nop                 ;
    //     call dword ptr [eax]       ;     FF 10
    //
    // THIS IS VERY TIGHTLY TIED TO THE PREDICATES IN
    // vm\i386\cGenCpu.h, esp. isCallRegisterIndirect.

    GenTree* result = nullptr;

#ifdef _TARGET_64BIT_
    // Non-tail calls: Jump Stubs are not taken into account by VM for mapping an AV into a NullRef
    // exception. Therefore, JIT needs to emit an explicit null check.  Note that Jit64 too generates
    // an explicit null check.
    //
    // Tail calls: fgMorphTailCall() materializes null check explicitly and hence no need to emit
    // null check.

    // Non-64-bit: No need to null check the this pointer - the dispatch code will deal with this.
    // The VM considers exceptions that occur in stubs on 64-bit to be not managed exceptions and
    // it would be difficult to change this in a way so that it affects only the right stubs.

    if (!call->IsTailCallViaHelper())
    {
        call->gtFlags |= GTF_CALL_NULLCHECK;
    }
#endif

    // TODO-Cleanup: Disable emitting random NOPs

    // This is code to set up an indirect call to a stub address computed
    // via dictionary lookup.
    if (call->gtCallType == CT_INDIRECT)
    {
        NYI_X86("Virtual Stub dispatched call lowering via dictionary lookup");

        // The importer decided we needed a stub call via a computed
        // stub dispatch address, i.e. an address which came from a dictionary lookup.
        //   - The dictionary lookup produces an indirected address, suitable for call
        //     via "call [REG_VIRTUAL_STUB_PARAM]"
        //
        // This combination will only be generated for shared generic code and when
        // stub dispatch is active.

        // fgMorphArgs will have created trees to pass the address in REG_VIRTUAL_STUB_PARAM.
        // All we have to do here is add an indirection to generate the actual call target.

        GenTree* ind = Ind(call->gtCallAddr);
        BlockRange().InsertAfter(call->gtCallAddr, ind);
        call->gtCallAddr = ind;
    }
    else
    {
        // Direct stub call.
        // Get stub addr. This will return NULL if virtual call stubs are not active
        void* stubAddr = call->gtStubCallStubAddr;
        noway_assert(stubAddr != nullptr);

        // If not CT_INDIRECT,  then it should always be relative indir call.
        // This is ensured by VM.
        noway_assert(call->IsVirtualStubRelativeIndir());

        // Direct stub calls, though the stubAddr itself may still need to be
        // accesed via an indirection.
        GenTree* addr = AddrGen(stubAddr);

#ifdef _TARGET_X86_
        // On x86, for tailcall via helper, the JIT_TailCall helper takes the stubAddr as
        // the target address, and we set a flag that it's a VSD call. The helper then
        // handles any necessary indirection.
        if (call->IsTailCallViaHelper())
        {
            result = addr;
        }
#endif // _TARGET_X86_

        if (result == nullptr)
        {
            GenTree* indir = Ind(addr);

// On x86 we generate this:
//        call dword ptr [rel32]  ;  FF 15 ---rel32----
// So we don't use a register.
#ifndef _TARGET_X86_
            // on x64 we must materialize the target using specific registers.
            addr->gtRegNum  = REG_VIRTUAL_STUB_PARAM;
            indir->gtRegNum = REG_JUMP_THUNK_PARAM;
#endif
            result = indir;
        }
    }

    // TODO-Cleanup: start emitting random NOPS
    return result;
}

//------------------------------------------------------------------------
// AddrModeCleanupHelper: Remove the nodes that are no longer used after an
// addressing mode is constructed
//
// Arguments:
//    addrMode - A pointer to a new GenTreeAddrMode
//    node     - The node currently being considered for removal
//
// Return Value:
//    None.
//
// Assumptions:
//    'addrMode' and 'node' must be contained in the current block
//
void Lowering::AddrModeCleanupHelper(GenTreeAddrMode* addrMode, GenTree* node)
{
    if (node == addrMode->Base() || node == addrMode->Index())
    {
        return;
    }

    // TODO-LIR: change this to use the LIR mark bit and iterate instead of recursing
    for (GenTree* operand : node->Operands())
    {
        AddrModeCleanupHelper(addrMode, operand);
    }

    BlockRange().Remove(node);
}

// given two nodes which will be used in an addressing mode (base, index)
// walk backwards from the use to those nodes to determine if they are
// potentially modified in that range
//
// returns: true if the sources given may be modified before they are used
bool Lowering::AreSourcesPossiblyModified(GenTree* addr, GenTree* base, GenTree* index)
{
    assert(addr != nullptr);

    for (GenTree* cursor = addr; cursor != nullptr; cursor = cursor->gtPrev)
    {
        if (cursor == base)
        {
            base = nullptr;
        }

        if (cursor == index)
        {
            index = nullptr;
        }

        if (base == nullptr && index == nullptr)
        {
            return false;
        }

        if (base != nullptr && comp->fgNodesMayInterfere(base, cursor))
        {
            return true;
        }

        if (index != nullptr && comp->fgNodesMayInterfere(index, cursor))
        {
            return true;
        }
    }

    unreached();
}

//------------------------------------------------------------------------
// TryCreateAddrMode: recognize trees which can be implemented using an
//    addressing mode and transform them to a GT_LEA
//
// Arguments:
//    use:     the use of the address we want to transform
//    isIndir: true if this addressing mode is the child of an indir
//
// Returns:
//    The created LEA node or the original address node if an LEA could
//    not be formed.
//
GenTree* Lowering::TryCreateAddrMode(LIR::Use&& use, bool isIndir)
{
    GenTree*   addr   = use.Def();
    GenTreePtr base   = nullptr;
    GenTreePtr index  = nullptr;
    unsigned   scale  = 0;
    unsigned   offset = 0;
    bool       rev    = false;

    // Find out if an addressing mode can be constructed
    bool doAddrMode =
        comp->codeGen->genCreateAddrMode(addr, -1, true, 0, &rev, &base, &index, &scale, &offset, true /*nogen*/);

    if (scale == 0)
    {
        scale = 1;
    }

    if (!isIndir)
    {
        // this is just a reg-const add
        if (index == nullptr)
        {
            return addr;
        }

        // this is just a reg-reg add
        if (scale == 1 && offset == 0)
        {
            return addr;
        }
    }

    // make sure there are not any side effects between def of leaves and use
    if (!doAddrMode || AreSourcesPossiblyModified(addr, base, index))
    {
        JITDUMP("  No addressing mode\n");
        return addr;
    }

    GenTreePtr arrLength = nullptr;

    JITDUMP("Addressing mode:\n");
    JITDUMP("  Base\n");
    DISPNODE(base);
    if (index != nullptr)
    {
        JITDUMP("  + Index * %u + %u\n", scale, offset);
        DISPNODE(index);
    }
    else
    {
        JITDUMP("  + %u\n", offset);
    }

    var_types addrModeType = addr->TypeGet();
    if (addrModeType == TYP_REF)
    {
        addrModeType = TYP_BYREF;
    }

    GenTreeAddrMode* addrMode = new (comp, GT_LEA) GenTreeAddrMode(addrModeType, base, index, scale, offset);

    addrMode->gtRsvdRegs = addr->gtRsvdRegs;
    addrMode->gtFlags |= (addr->gtFlags & (GTF_ALL_EFFECT | GTF_IND_FLAGS));

    JITDUMP("New addressing mode node:\n");
    DISPNODE(addrMode);
    JITDUMP("\n");

    // Required to prevent assert failure:
    //    Assertion failed 'op1 && op2' in flowgraph.cpp, Line: 34431
    // when iterating the operands of a GT_LEA
    // Test Case: self_host_tests_amd64\jit\jit64\opt\cse\VolatileTest_op_mul.exe
    //    Method: TestCSE:.cctor
    // The method genCreateAddrMode() above probably should be fixed
    //    to not return rev=true, when index is returned as NULL
    //
    if (rev && index == nullptr)
    {
        rev = false;
    }

    if (rev)
    {
        addrMode->gtFlags |= GTF_REVERSE_OPS;
    }
    else
    {
        addrMode->gtFlags &= ~(GTF_REVERSE_OPS);
    }

    BlockRange().InsertAfter(addr, addrMode);

    // Now we need to remove all the nodes subsumed by the addrMode
    AddrModeCleanupHelper(addrMode, addr);

    // Replace the original address node with the addrMode.
    use.ReplaceWith(comp, addrMode);

    return addrMode;
}

//------------------------------------------------------------------------
// LowerAdd: turn this add into a GT_LEA if that would be profitable
//
// Arguments:
//    node - the node we care about
//
// Returns:
//    The next node to lower.
//
GenTree* Lowering::LowerAdd(GenTree* node)
{
    GenTree* next = node->gtNext;

#ifdef _TARGET_ARMARCH_
    // For ARM architectures we don't have the LEA instruction
    // therefore we won't get much benefit from doing this.
    return next;
#else  // _TARGET_ARMARCH_
    if (!varTypeIsIntegralOrI(node))
    {
        return next;
    }

    LIR::Use use;
    if (!BlockRange().TryGetUse(node, &use))
    {
        return next;
    }

    // if this is a child of an indir, let the parent handle it
    if (use.User()->OperIsIndir())
    {
        return next;
    }

    // if there is a chain of adds, only look at the topmost one
    if (use.User()->gtOper == GT_ADD)
    {
        return next;
    }

    return TryCreateAddrMode(std::move(use), false)->gtNext;
#endif // !_TARGET_ARMARCH_
}

//------------------------------------------------------------------------
// LowerUnsignedDivOrMod: transform GT_UDIV/GT_UMOD nodes with a const power of 2
// divisor into GT_RSZ/GT_AND nodes.
//
// Arguments:
//    node - pointer to the GT_UDIV/GT_UMOD node to be lowered
//
void Lowering::LowerUnsignedDivOrMod(GenTree* node)
{
    assert((node->OperGet() == GT_UDIV) || (node->OperGet() == GT_UMOD));

    GenTree* divisor = node->gtGetOp2();

    if (divisor->IsCnsIntOrI())
    {
        size_t divisorValue = static_cast<size_t>(divisor->gtIntCon.IconValue());

        if (isPow2(divisorValue))
        {
            genTreeOps newOper;

            if (node->OperGet() == GT_UDIV)
            {
                newOper      = GT_RSZ;
                divisorValue = genLog2(divisorValue);
            }
            else
            {
                newOper = GT_AND;
                divisorValue -= 1;
            }

            node->SetOper(newOper);
            divisor->gtIntCon.SetIconValue(divisorValue);
        }
    }
}

//------------------------------------------------------------------------
// LowerSignedDivOrMod: transform integer GT_DIV/GT_MOD nodes with a power of 2
// const divisor into equivalent but faster sequences.
//
// Arguments:
//    node - pointer to node we care about
//
// Returns:
//    The next node to lower.
//
GenTree* Lowering::LowerSignedDivOrMod(GenTreePtr node)
{
    assert((node->OperGet() == GT_DIV) || (node->OperGet() == GT_MOD));

    GenTree* next    = node->gtNext;
    GenTree* divMod  = node;
    GenTree* divisor = divMod->gtGetOp2();

    if (!divisor->IsCnsIntOrI())
    {
        return next; // no transformations to make
    }

    const var_types type = divMod->TypeGet();
    assert((type == TYP_INT) || (type == TYP_LONG));

    GenTree* dividend = divMod->gtGetOp1();

    if (dividend->IsCnsIntOrI())
    {
        // We shouldn't see a divmod with constant operands here but if we do then it's likely
        // because optimizations are disabled or it's a case that's supposed to throw an exception.
        // Don't optimize this.
        return next;
    }

    ssize_t divisorValue = divisor->gtIntCon.IconValue();

    if (divisorValue == -1)
    {
        // x / -1 can't be optimized because INT_MIN / -1 is required to throw an exception.

        // x % -1 is always 0 and the IL spec says that the rem instruction "can" throw an exception if x is
        // the minimum representable integer. However, the C# spec says that an exception "is" thrown in this
        // case so optimizing this case would break C# code.

        // A runtime check could be used to handle this case but it's probably too rare to matter.
        return next;
    }

    bool isDiv = divMod->OperGet() == GT_DIV;

    if (isDiv)
    {
        if ((type == TYP_INT && divisorValue == INT_MIN) || (type == TYP_LONG && divisorValue == INT64_MIN))
        {
            // If the divisor is the minimum representable integer value then we can use a compare,
            // the result is 1 iff the dividend equals divisor.
            divMod->SetOper(GT_EQ);
            return next;
        }
    }

    size_t absDivisorValue =
        (divisorValue == SSIZE_T_MIN) ? static_cast<size_t>(divisorValue) : static_cast<size_t>(abs(divisorValue));

    if (!isPow2(absDivisorValue))
    {
        return next;
    }

    // We're committed to the conversion now. Go find the use.
    LIR::Use use;
    if (!BlockRange().TryGetUse(node, &use))
    {
        assert(!"signed DIV/MOD node is unused");
        return next;
    }

    // We need to use the dividend node multiple times so its value needs to be
    // computed once and stored in a temp variable.

    unsigned curBBWeight = comp->compCurBB->getBBWeight(comp);

    LIR::Use opDividend(BlockRange(), &divMod->gtOp.gtOp1, divMod);
    opDividend.ReplaceWithLclVar(comp, curBBWeight);

    dividend = divMod->gtGetOp1();
    assert(dividend->OperGet() == GT_LCL_VAR);

    unsigned dividendLclNum = dividend->gtLclVar.gtLclNum;

    GenTree* adjustment = comp->gtNewOperNode(GT_RSH, type, dividend, comp->gtNewIconNode(type == TYP_INT ? 31 : 63));

    if (absDivisorValue == 2)
    {
        // If the divisor is +/-2 then we'd end up with a bitwise and between 0/-1 and 1.
        // We can get the same result by using GT_RSZ instead of GT_RSH.
        adjustment->SetOper(GT_RSZ);
    }
    else
    {
        adjustment = comp->gtNewOperNode(GT_AND, type, adjustment, comp->gtNewIconNode(absDivisorValue - 1, type));
    }

    GenTree* adjustedDividend =
        comp->gtNewOperNode(GT_ADD, type, adjustment, comp->gtNewLclvNode(dividendLclNum, type));

    comp->lvaTable[dividendLclNum].incRefCnts(curBBWeight, comp);

    GenTree* newDivMod;

    if (isDiv)
    {
        // perform the division by right shifting the adjusted dividend
        divisor->gtIntCon.SetIconValue(genLog2(absDivisorValue));

        newDivMod = comp->gtNewOperNode(GT_RSH, type, adjustedDividend, divisor);

        if (divisorValue < 0)
        {
            // negate the result if the divisor is negative
            newDivMod = comp->gtNewOperNode(GT_NEG, type, newDivMod);
        }
    }
    else
    {
        // divisor % dividend = dividend - divisor x (dividend / divisor)
        // divisor x (dividend / divisor) translates to (dividend >> log2(divisor)) << log2(divisor)
        // which simply discards the low log2(divisor) bits, that's just dividend & ~(divisor - 1)
        divisor->gtIntCon.SetIconValue(~(absDivisorValue - 1));

        newDivMod = comp->gtNewOperNode(GT_SUB, type, comp->gtNewLclvNode(dividendLclNum, type),
                                        comp->gtNewOperNode(GT_AND, type, adjustedDividend, divisor));

        comp->lvaTable[dividendLclNum].incRefCnts(curBBWeight, comp);
    }

    // Remove the divisor and dividend nodes from the linear order,
    // since we have reused them and will resequence the tree
    BlockRange().Remove(divisor);
    BlockRange().Remove(dividend);

    // linearize and insert the new tree before the original divMod node
    BlockRange().InsertBefore(divMod, LIR::SeqTree(comp, newDivMod));
    BlockRange().Remove(divMod);

    // replace the original divmod node with the new divmod tree
    use.ReplaceWith(comp, newDivMod);

    return newDivMod->gtNext;
}

//------------------------------------------------------------------------
// LowerStoreInd: attempt to transform an indirect store to use an
//    addressing mode
//
// Arguments:
//    node - the node we care about
//
void Lowering::LowerStoreInd(GenTree* node)
{
    assert(node != nullptr);
    assert(node->OperGet() == GT_STOREIND);

    TryCreateAddrMode(LIR::Use(BlockRange(), &node->gtOp.gtOp1, node), true);

    // Mark all GT_STOREIND nodes to indicate that it is not known
    // whether it represents a RMW memory op.
    node->AsStoreInd()->SetRMWStatusDefault();
}

//------------------------------------------------------------------------
// LowerArrElem: Lower a GT_ARR_ELEM node
//
// Arguments:
//    node - the GT_ARR_ELEM node to lower.
//
// Return Value:
//    The next node to lower.
//
// Assumptions:
//    pTree points to a pointer to a GT_ARR_ELEM node.
//
// Notes:
//    This performs the following lowering.  We start with a node of the form:
//          /--*  <arrObj>
//          +--*  <index0>
//          +--*  <index1>
//       /--*  arrMD&[,]
//
//    First, we create temps for arrObj if it is not already a lclVar, and for any of the index
//    expressions that have side-effects.
//    We then transform the tree into:
//                      <offset is null - no accumulated offset for the first index>
//                   /--*  <arrObj>
//                   +--*  <index0>
//                /--*  ArrIndex[i, ]
//                +--*  <arrObj>
//             /--|  arrOffs[i, ]
//             |  +--*  <arrObj>
//             |  +--*  <index1>
//             +--*  ArrIndex[*,j]
//             +--*  <arrObj>
//          /--|  arrOffs[*,j]
//          +--*  lclVar NewTemp
//       /--*  lea (scale = element size, offset = offset of first element)
//
//    The new stmtExpr may be omitted if the <arrObj> is a lclVar.
//    The new stmtExpr may be embedded if the <arrObj> is not the first tree in linear order for
//    the statement containing the original arrMD.
//    Note that the arrMDOffs is the INDEX of the lea, but is evaluated before the BASE (which is the second
//    reference to NewTemp), because that provides more accurate lifetimes.
//    There may be 1, 2 or 3 dimensions, with 1, 2 or 3 arrMDIdx nodes, respectively.
//
GenTree* Lowering::LowerArrElem(GenTree* node)
{
    // This will assert if we don't have an ArrElem node
    GenTreeArrElem*     arrElem     = node->AsArrElem();
    const unsigned char rank        = arrElem->gtArrElem.gtArrRank;
    const unsigned      blockWeight = m_block->getBBWeight(comp);

    JITDUMP("Lowering ArrElem\n");
    JITDUMP("============\n");
    DISPTREERANGE(BlockRange(), arrElem);
    JITDUMP("\n");

    assert(arrElem->gtArrObj->TypeGet() == TYP_REF);

    // We need to have the array object in a lclVar.
    if (!arrElem->gtArrObj->IsLocal())
    {
        LIR::Use arrObjUse(BlockRange(), &arrElem->gtArrObj, arrElem);
        arrObjUse.ReplaceWithLclVar(comp, blockWeight);
    }

    GenTree* arrObjNode = arrElem->gtArrObj;
    assert(arrObjNode->IsLocal());

    GenTree* insertionPoint = arrElem;

    // The first ArrOffs node will have 0 for the offset of the previous dimension.
    GenTree* prevArrOffs = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);
    BlockRange().InsertBefore(insertionPoint, prevArrOffs);

    for (unsigned char dim = 0; dim < rank; dim++)
    {
        GenTree* indexNode = arrElem->gtArrElem.gtArrInds[dim];

        // Use the original arrObjNode on the 0th ArrIndex node, and clone it for subsequent ones.
        GenTreePtr idxArrObjNode;
        if (dim == 0)
        {
            idxArrObjNode = arrObjNode;
        }
        else
        {
            idxArrObjNode = comp->gtClone(arrObjNode);
            BlockRange().InsertBefore(insertionPoint, idxArrObjNode);
        }

        // Next comes the GT_ARR_INDEX node.
        GenTreeArrIndex* arrMDIdx = new (comp, GT_ARR_INDEX)
            GenTreeArrIndex(TYP_INT, idxArrObjNode, indexNode, dim, rank, arrElem->gtArrElem.gtArrElemType);
        arrMDIdx->gtFlags |= ((idxArrObjNode->gtFlags | indexNode->gtFlags) & GTF_ALL_EFFECT);
        BlockRange().InsertBefore(insertionPoint, arrMDIdx);

        GenTree* offsArrObjNode = comp->gtClone(arrObjNode);
        BlockRange().InsertBefore(insertionPoint, offsArrObjNode);

        GenTreeArrOffs* arrOffs =
            new (comp, GT_ARR_OFFSET) GenTreeArrOffs(TYP_I_IMPL, prevArrOffs, arrMDIdx, offsArrObjNode, dim, rank,
                                                     arrElem->gtArrElem.gtArrElemType);
        arrOffs->gtFlags |= ((prevArrOffs->gtFlags | arrMDIdx->gtFlags | offsArrObjNode->gtFlags) & GTF_ALL_EFFECT);
        BlockRange().InsertBefore(insertionPoint, arrOffs);

        prevArrOffs = arrOffs;
    }

    // Generate the LEA and make it reverse evaluation, because we want to evaluate the index expression before the
    // base.
    unsigned scale  = arrElem->gtArrElem.gtArrElemSize;
    unsigned offset = comp->eeGetMDArrayDataOffset(arrElem->gtArrElem.gtArrElemType, arrElem->gtArrElem.gtArrRank);

    GenTreePtr leaIndexNode = prevArrOffs;
    if (!jitIsScaleIndexMul(scale))
    {
        // We do the address arithmetic in TYP_I_IMPL, though note that the lower bounds and lengths in memory are
        // TYP_INT
        GenTreePtr scaleNode = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, scale);
        GenTreePtr mulNode   = new (comp, GT_MUL) GenTreeOp(GT_MUL, TYP_I_IMPL, leaIndexNode, scaleNode);
        BlockRange().InsertBefore(insertionPoint, scaleNode, mulNode);
        leaIndexNode = mulNode;
        scale        = 1;
    }

    GenTreePtr leaBase = comp->gtClone(arrObjNode);
    BlockRange().InsertBefore(insertionPoint, leaBase);

    GenTreePtr leaNode = new (comp, GT_LEA) GenTreeAddrMode(arrElem->TypeGet(), leaBase, leaIndexNode, scale, offset);
    leaNode->gtFlags |= GTF_REVERSE_OPS;

    BlockRange().InsertBefore(insertionPoint, leaNode);

    LIR::Use arrElemUse;
    if (BlockRange().TryGetUse(arrElem, &arrElemUse))
    {
        arrElemUse.ReplaceWith(comp, leaNode);
    }

    BlockRange().Remove(arrElem);

    JITDUMP("Results of lowering ArrElem:\n");
    DISPTREERANGE(BlockRange(), leaNode);
    JITDUMP("\n\n");

    return leaNode;
}

void Lowering::DoPhase()
{
#if 0
    // The code in this #if can be used to debug lowering issues according to
    // method hash.  To use, simply set environment variables lowerhashlo and lowerhashhi
#ifdef DEBUG
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("lowerhashlo");
    unsigned methHashLo = 0;
    if (lostr != NULL)
    {
        sscanf_s(lostr, "%x", &methHashLo);
    }
    char* histr = getenv("lowerhashhi");
    unsigned methHashHi = UINT32_MAX;
    if (histr != NULL)
    {
        sscanf_s(histr, "%x", &methHashHi);
    }
    if (methHash < methHashLo || methHash > methHashHi)
        return;
    else
    {
        printf("Lowering for method %s, hash = 0x%x.\n",
               info.compFullName, info.compMethodHash());
        printf("");         // in our logic this causes a flush
    }
#endif
#endif

#if !defined(_TARGET_64BIT_)
    DecomposeLongs decomp(comp); // Initialize the long decomposition class.
    decomp.PrepareForDecomposition();
#endif // !defined(_TARGET_64BIT_)

    for (BasicBlock* block = comp->fgFirstBB; block; block = block->bbNext)
    {
        /* Make the block publicly available */
        comp->compCurBB = block;

#if !defined(_TARGET_64BIT_)
        decomp.DecomposeBlock(block);
#endif //!_TARGET_64BIT_

        LowerBlock(block);
    }

    // If we have any PInvoke calls, insert the one-time prolog code. We've already inserted the epilog code in the
    // appropriate spots. NOTE: there is a minor optimization opportunity here, as we still create p/invoke data
    // structures and setup/teardown even if we've eliminated all p/invoke calls due to dead code elimination.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodProlog();
    }

#ifdef DEBUG
    JITDUMP("Lower has completed modifying nodes, proceeding to initialize LSRA TreeNodeInfo structs...\n");
    if (VERBOSE)
    {
        comp->fgDispBasicBlocks(true);
    }
#endif

    // TODO-Throughput: We re-sort local variables to get the goodness of enregistering recently
    // introduced local variables both by Rationalize and Lower; downside is we need to
    // recompute standard local variable liveness in order to get Linear CodeGen working.
    // For now we'll take the throughput hit of recomputing local liveness but in the long term
    // we're striving to use the unified liveness computation (fgLocalVarLiveness) and stop
    // computing it separately in LSRA.
    if (comp->lvaCount != 0)
    {
        comp->lvaSortAgain = true;
    }
    comp->EndPhase(PHASE_LOWERING_DECOMP);

    comp->fgLocalVarLiveness();
    // local var liveness can delete code, which may create empty blocks
    if (!comp->opts.MinOpts() && !comp->opts.compDbgCode)
    {
        comp->optLoopsMarked = false;
        bool modified        = comp->fgUpdateFlowGraph();
        if (modified || comp->lvaSortAgain)
        {
            JITDUMP("had to run another liveness pass:\n");
            comp->fgLocalVarLiveness();
        }
    }
#ifdef DEBUG
    JITDUMP("Liveness pass finished after lowering, IR:\n");
    JITDUMP("lvasortagain = %d\n", comp->lvaSortAgain);
    if (VERBOSE)
    {
        comp->fgDispBasicBlocks(true);
    }
#endif

    // The initialization code for the TreeNodeInfo map was initially part of a single full IR
    // traversal and it has been split because the order of traversal performed by fgWalkTreePost
    // does not necessarily lower nodes in execution order and also, it could potentially
    // add new BasicBlocks on the fly as part of the Lowering pass so the traversal won't be complete.
    //
    // Doing a new traversal guarantees we 'see' all new introduced trees and basic blocks allowing us
    // to correctly initialize all the data structures LSRA requires later on.
    // This code still has issues when it has to do with initialization of recently introduced locals by
    // lowering.  The effect of this is that any temporary local variable introduced by lowering won't be
    // enregistered yielding suboptimal CQ.
    // The reason for this is because we cannot re-sort the local variables per ref-count and bump of the number of
    // tracked variables just here because then LSRA will work with mismatching BitSets (i.e. BitSets with different
    // 'epochs' that were created before and after variable resorting, that will result in different number of tracked
    // local variables).
    //
    // The fix for this is to refactor this code to be run JUST BEFORE LSRA and not as part of lowering.
    // It's also desirable to avoid initializing this code using a non-execution order traversal.
    //
    LsraLocation currentLoc = 1;
    for (BasicBlock* block = m_lsra->startBlockSequence(); block != nullptr; block = m_lsra->moveToNextBlock())
    {
        GenTreePtr stmt;

        // Increment the LsraLocation (currentLoc) at each BasicBlock.
        // This ensures that the block boundary (RefTypeBB, RefTypeExpUse and RefTypeDummyDef) RefPositions
        // are in increasing location order.
        currentLoc += 2;

        m_block = block;
        for (GenTree* node : BlockRange().NonPhiNodes())
        {
/* We increment the number position of each tree node by 2 to
* simplify the logic when there's the case of a tree that implicitly
* does a dual-definition of temps (the long case).  In this case
* is easier to already have an idle spot to handle a dual-def instead
* of making some messy adjustments if we only increment the
* number position by one.
*/
#ifdef DEBUG
            node->gtSeqNum = currentLoc;
#endif

            node->gtLsraInfo.Initialize(m_lsra, node, currentLoc);
            node->gtClearReg(comp);

            // Mark the node's operands as used
            for (GenTree* operand : node->Operands())
            {
                operand->gtLIRFlags &= ~LIR::Flags::IsUnusedValue;
            }

            // If the node produces a value, mark it as unused.
            if (node->IsValue())
            {
                node->gtLIRFlags |= LIR::Flags::IsUnusedValue;
            }

            currentLoc += 2;
        }

        for (GenTree* node : BlockRange().NonPhiNodes())
        {
            TreeNodeInfoInit(node);

            // Only nodes that produce values should have a non-zero dstCount.
            assert((node->gtLsraInfo.dstCount == 0) || node->IsValue());

            // If the node produces an unused value, mark it as a local def-use
            if ((node->gtLIRFlags & LIR::Flags::IsUnusedValue) != 0)
            {
                node->gtLsraInfo.isLocalDefUse = true;
                node->gtLsraInfo.dstCount      = 0;
            }

#if 0
            // TODO-CQ: Enable this code after fixing the isContained() logic to not abort for these
            // top-level nodes that throw away their result.
            // If this is an interlocked operation that has a non-last-use lclVar as its op2,
            // make sure we allocate a target register for the interlocked operation.; otherwise we need
            // not allocate a register
            else if ((tree->OperGet() == GT_LOCKADD || tree->OperGet() == GT_XCHG || tree->OperGet() == GT_XADD))
            {
                tree->gtLsraInfo.dstCount = 0;
                if (tree->gtGetOp2()->IsLocal() && (tree->gtFlags & GTF_VAR_DEATH) == 0)
                    tree->gtLsraInfo.isLocalDefUse = true;
            }
#endif
        }

        assert(BlockRange().CheckLIR(comp, true));
    }
    DBEXEC(VERBOSE, DumpNodeInfoMap());
}

#ifdef DEBUG

//------------------------------------------------------------------------
// Lowering::CheckCallArg: check that a call argument is in an expected
//                         form after lowering.
//
// Arguments:
//   arg - the argument to check.
//
void Lowering::CheckCallArg(GenTree* arg)
{
    if (arg->OperIsStore() || arg->IsArgPlaceHolderNode() || arg->IsNothingNode() || arg->OperIsCopyBlkOp())
    {
        return;
    }

    switch (arg->OperGet())
    {
#if !defined(_TARGET_64BIT_)
        case GT_LONG:
            assert(arg->gtGetOp1()->OperIsPutArg());
            assert(arg->gtGetOp2()->OperIsPutArg());
            break;
#endif

        case GT_LIST:
            for (GenTreeArgList* list = arg->AsArgList(); list != nullptr; list = list->Rest())
            {
                assert(list->Current()->OperIsPutArg());
            }
            break;

        default:
            assert(arg->OperIsPutArg());
            break;
    }
}

//------------------------------------------------------------------------
// Lowering::CheckCall: check that a call is in an expected form after
//                      lowering. Currently this amounts to checking its
//                      arguments, but could be expanded to verify more
//                      properties in the future.
//
// Arguments:
//   call - the call to check.
//
void Lowering::CheckCall(GenTreeCall* call)
{
    if (call->gtCallObjp != nullptr)
    {
        CheckCallArg(call->gtCallObjp);
    }

    for (GenTreeArgList* args = call->gtCallArgs; args != nullptr; args = args->Rest())
    {
        CheckCallArg(args->Current());
    }

    for (GenTreeArgList* args = call->gtCallLateArgs; args != nullptr; args = args->Rest())
    {
        CheckCallArg(args->Current());
    }
}

//------------------------------------------------------------------------
// Lowering::CheckNode: check that an LIR node is in an expected form
//                      after lowering.
//
// Arguments:
//   node - the node to check.
//
void Lowering::CheckNode(GenTree* node)
{
    switch (node->OperGet())
    {
        case GT_CALL:
            CheckCall(node->AsCall());
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
#ifdef _TARGET_64BIT_
        case GT_LCL_VAR:
        case GT_STORE_LCL_VAR:
#endif // _TARGET_64BIT_
            assert(node->TypeGet() != TYP_SIMD12);
            break;
#endif

        default:
            break;
    }
}

//------------------------------------------------------------------------
// Lowering::CheckBlock: check that the contents of an LIR block are in an
//                       expected form after lowering.
//
// Arguments:
//   compiler - the compiler context.
//   block    - the block to check.
//
bool Lowering::CheckBlock(Compiler* compiler, BasicBlock* block)
{
    assert(block->isEmpty() || block->IsLIR());

    LIR::Range& blockRange = LIR::AsRange(block);
    for (GenTree* node : blockRange)
    {
        CheckNode(node);
    }

    assert(blockRange.CheckLIR(compiler));
    return true;
}
#endif

void Lowering::LowerBlock(BasicBlock* block)
{
    assert(block == comp->compCurBB); // compCurBB must already be set.
    assert(block->isEmpty() || block->IsLIR());

    m_block = block;

    // NOTE: some of the lowering methods insert calls before the node being
    // lowered (See e.g. InsertPInvoke{Method,Call}{Prolog,Epilog}). In
    // general, any code that is inserted before the current node should be
    // "pre-lowered" as they won't be subject to further processing.
    // Lowering::CheckBlock() runs some extra checks on call arguments in
    // order to help catch unlowered nodes.

    GenTree* node = BlockRange().FirstNode();
    while (node != nullptr)
    {
        node = LowerNode(node);
    }

    assert(CheckBlock(comp, block));
}

/** Verifies if both of these trees represent the same indirection.
 * Used by Lower to annotate if CodeGen generate an instruction of the
 * form *addrMode BinOp= expr
 *
 * Preconditions: both trees are children of GT_INDs and their underlying children
 * have the same gtOper.
 *
 * This is a first iteration to actually recognize trees that can be code-generated
 * as a single read-modify-write instruction on AMD64/x86.  For now
 * this method only supports the recognition of simple addressing modes (through GT_LEA)
 * or local var indirections.  Local fields, array access and other more complex nodes are
 * not yet supported.
 *
 * TODO-CQ:  Perform tree recognition by using the Value Numbering Package, that way we can recognize
 * arbitrary complex trees and support much more addressing patterns.
 */
bool Lowering::IndirsAreEquivalent(GenTreePtr candidate, GenTreePtr storeInd)
{
    assert(candidate->OperGet() == GT_IND);
    assert(storeInd->OperGet() == GT_STOREIND);

    // We should check the size of the indirections.  If they are
    // different, say because of a cast, then we can't call them equivalent.  Doing so could cause us
    // to drop a cast.
    // Signed-ness difference is okay and expected since a store indirection must always
    // be signed based on the CIL spec, but a load could be unsigned.
    if (genTypeSize(candidate->gtType) != genTypeSize(storeInd->gtType))
    {
        return false;
    }

    GenTreePtr pTreeA = candidate->gtGetOp1();
    GenTreePtr pTreeB = storeInd->gtGetOp1();

    // This method will be called by codegen (as well as during lowering).
    // After register allocation, the sources may have been spilled and reloaded
    // to a different register, indicated by an inserted GT_RELOAD node.
    pTreeA = pTreeA->gtSkipReloadOrCopy();
    pTreeB = pTreeB->gtSkipReloadOrCopy();

    genTreeOps oper;
    unsigned   kind;

    if (pTreeA->OperGet() != pTreeB->OperGet())
    {
        return false;
    }

    oper = pTreeA->OperGet();
    switch (oper)
    {
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
        case GT_CLS_VAR_ADDR:
        case GT_CNS_INT:
            return NodesAreEquivalentLeaves(pTreeA, pTreeB);

        case GT_LEA:
        {
            GenTreeAddrMode* gtAddr1 = pTreeA->AsAddrMode();
            GenTreeAddrMode* gtAddr2 = pTreeB->AsAddrMode();
            return NodesAreEquivalentLeaves(gtAddr1->Base(), gtAddr2->Base()) &&
                   NodesAreEquivalentLeaves(gtAddr1->Index(), gtAddr2->Index()) &&
                   gtAddr1->gtScale == gtAddr2->gtScale && gtAddr1->gtOffset == gtAddr2->gtOffset;
        }
        default:
            // We don't handle anything that is not either a constant,
            // a local var or LEA.
            return false;
    }
}

/** Test whether the two given nodes are the same leaves.
 *  Right now, only constant integers and local variables are supported
 */
bool Lowering::NodesAreEquivalentLeaves(GenTreePtr tree1, GenTreePtr tree2)
{
    if (tree1 == nullptr && tree2 == nullptr)
    {
        return true;
    }

    // both null, they are equivalent, otherwise if either is null not equivalent
    if (tree1 == nullptr || tree2 == nullptr)
    {
        return false;
    }

    tree1 = tree1->gtSkipReloadOrCopy();
    tree2 = tree2->gtSkipReloadOrCopy();

    if (tree1->TypeGet() != tree2->TypeGet())
    {
        return false;
    }

    if (tree1->OperGet() != tree2->OperGet())
    {
        return false;
    }

    if (!tree1->OperIsLeaf() || !tree2->OperIsLeaf())
    {
        return false;
    }

    switch (tree1->OperGet())
    {
        case GT_CNS_INT:
            return tree1->gtIntCon.gtIconVal == tree2->gtIntCon.gtIconVal &&
                   tree1->IsIconHandle() == tree2->IsIconHandle();
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
            return tree1->gtLclVarCommon.gtLclNum == tree2->gtLclVarCommon.gtLclNum;
        case GT_CLS_VAR_ADDR:
            return tree1->gtClsVar.gtClsVarHnd == tree2->gtClsVar.gtClsVarHnd;
        default:
            return false;
    }
}

#ifdef _TARGET_64BIT_
/**
 * Get common information required to handle a cast instruction
 *
 * Right now only supports 64 bit targets. In order to support 32 bit targets the
 * switch statement needs work.
 *
 */
void Lowering::getCastDescription(GenTreePtr treeNode, CastInfo* castInfo)
{
    // Intialize castInfo
    memset(castInfo, 0, sizeof(*castInfo));

    GenTreePtr castOp = treeNode->gtCast.CastOp();

    var_types dstType = treeNode->CastToType();
    var_types srcType = castOp->TypeGet();

    castInfo->unsignedDest   = varTypeIsUnsigned(dstType);
    castInfo->unsignedSource = varTypeIsUnsigned(srcType);

    // If necessary, force the srcType to unsigned when the GT_UNSIGNED flag is set.
    if (!castInfo->unsignedSource && (treeNode->gtFlags & GTF_UNSIGNED) != 0)
    {
        srcType                  = genUnsignedType(srcType);
        castInfo->unsignedSource = true;
    }

    if (treeNode->gtOverflow() &&
        (genTypeSize(srcType) >= genTypeSize(dstType) || (srcType == TYP_INT && dstType == TYP_ULONG)))
    {
        castInfo->requiresOverflowCheck = true;
    }

    if (castInfo->requiresOverflowCheck)
    {
        ssize_t typeMin       = 0;
        ssize_t typeMax       = 0;
        ssize_t typeMask      = 0;
        bool    signCheckOnly = false;

        // Do we need to compare the value, or just check masks

        switch (dstType)
        {
            default:
                assert(!"unreachable: getCastDescription");
                break;

            case TYP_BYTE:
                typeMask = ssize_t((int)0xFFFFFF80);
                typeMin  = SCHAR_MIN;
                typeMax  = SCHAR_MAX;
                break;

            case TYP_UBYTE:
                typeMask = ssize_t((int)0xFFFFFF00L);
                break;

            case TYP_SHORT:
                typeMask = ssize_t((int)0xFFFF8000);
                typeMin  = SHRT_MIN;
                typeMax  = SHRT_MAX;
                break;

            case TYP_CHAR:
                typeMask = ssize_t((int)0xFFFF0000L);
                break;

            case TYP_INT:
                if (srcType == TYP_UINT)
                {
                    signCheckOnly = true;
                }
                else
                {
                    typeMask = 0xFFFFFFFF80000000LL;
                    typeMin  = INT_MIN;
                    typeMax  = INT_MAX;
                }
                break;

            case TYP_UINT:
                if (srcType == TYP_INT)
                {
                    signCheckOnly = true;
                }
                else
                {
                    typeMask = 0xFFFFFFFF00000000LL;
                }
                break;

            case TYP_LONG:
                signCheckOnly = true;
                break;

            case TYP_ULONG:
                signCheckOnly = true;
                break;
        }

        if (signCheckOnly)
        {
            castInfo->signCheckOnly = true;
        }

        castInfo->typeMax  = typeMax;
        castInfo->typeMin  = typeMin;
        castInfo->typeMask = typeMask;
    }
}

#endif // _TARGET_64BIT_

#ifdef DEBUG
void Lowering::DumpNodeInfoMap()
{
    printf("-----------------------------\n");
    printf("TREE NODE INFO DUMP\n");
    printf("-----------------------------\n");

    for (BasicBlock* block = comp->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
        {
            comp->gtDispTree(node, nullptr, nullptr, true);
            printf("    +");
            node->gtLsraInfo.dump(m_lsra);
        }
    }
}
#endif // DEBUG

#endif // !LEGACY_BACKEND
