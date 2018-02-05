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
void Lowering::MakeSrcContained(GenTree* parentNode, GenTree* childNode)
{
    assert(!parentNode->OperIsLeaf());
    assert(childNode->canBeContained());
    childNode->SetContained();
    assert(childNode->isContained());
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
//    parentNode - any non-leaf node
//    childNode  - some node that is an input to `parentNode`
//
// Return value:
//    true if it is safe to make childNode a contained memory operand.
//
bool Lowering::IsSafeToContainMem(GenTree* parentNode, GenTree* childNode)
{
    m_scratchSideEffects.Clear();
    m_scratchSideEffects.AddNode(comp, childNode);

    for (GenTree* node = childNode->gtNext; node != parentNode; node = node->gtNext)
    {
        if (m_scratchSideEffects.InterferesWith(comp, node, false))
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
            ContainCheckIndir(node->AsIndir());
            break;

        case GT_STOREIND:
            TryCreateAddrMode(LIR::Use(BlockRange(), &node->gtOp.gtOp1, node), true);
            if (!comp->codeGen->gcInfo.gcIsWriteBarrierAsgNode(node))
            {
                LowerStoreIndir(node->AsIndir());
            }
            break;

        case GT_ADD:
        {
            GenTree* afterTransform = LowerAdd(node);
            if (afterTransform != nullptr)
            {
                return afterTransform;
            }
            __fallthrough;
        }

#if !defined(_TARGET_64BIT_)
        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
#endif
        case GT_SUB:
        case GT_AND:
        case GT_OR:
        case GT_XOR:
            ContainCheckBinary(node->AsOp());
            break;

        case GT_MUL:
        case GT_MULHI:
#if defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)
        case GT_MUL_LONG:
#endif
            ContainCheckMul(node->AsOp());
            break;

        case GT_UDIV:
        case GT_UMOD:
            if (!LowerUnsignedDivOrMod(node->AsOp()))
            {
                ContainCheckDivOrMod(node->AsOp());
            }
            break;

        case GT_DIV:
        case GT_MOD:
            return LowerSignedDivOrMod(node);

        case GT_SWITCH:
            return LowerSwitch(node);

        case GT_CALL:
            LowerCall(node);
            break;

        case GT_LT:
        case GT_LE:
        case GT_GT:
        case GT_GE:
        case GT_EQ:
        case GT_NE:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_CMP:
            return LowerCompare(node);

        case GT_JTRUE:
            return LowerJTrue(node->AsOp());

        case GT_JMP:
            LowerJmpMethod(node);
            break;

        case GT_RETURN:
            LowerRet(node);
            break;

        case GT_RETURNTRAP:
            ContainCheckReturnTrap(node->AsOp());
            break;

        case GT_CAST:
            LowerCast(node);
            break;

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            ContainCheckBoundsChk(node->AsBoundsChk());
            break;
#endif // _TARGET_XARCH_
        case GT_ARR_ELEM:
            return LowerArrElem(node);

        case GT_ARR_OFFSET:
            ContainCheckArrOffset(node->AsArrOffs());
            break;

        case GT_ROL:
        case GT_ROR:
            LowerRotate(node);
            break;

#ifndef _TARGET_64BIT_
        case GT_LSH_HI:
        case GT_RSH_LO:
            ContainCheckShiftRotate(node->AsOp());
            break;
#endif // !_TARGET_64BIT_

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
            LowerShift(node->AsOp());
#else
            ContainCheckShiftRotate(node->AsOp());
#endif
            break;

        case GT_STORE_BLK:
        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
        {
            GenTreeBlk* blkNode = node->AsBlk();
            TryCreateAddrMode(LIR::Use(BlockRange(), &blkNode->Addr(), blkNode), false);
            LowerBlockStore(blkNode);
        }
        break;

        case GT_LCLHEAP:
            ContainCheckLclHeap(node->AsOp());
            break;

#ifdef _TARGET_XARCH_
        case GT_INTRINSIC:
            ContainCheckIntrinsic(node->AsOp());
            break;
#endif // _TARGET_XARCH_

#ifdef FEATURE_SIMD
        case GT_SIMD:
            LowerSIMD(node->AsSIMD());
            break;
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWIntrinsic:
            LowerHWIntrinsic(node->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_LCL_VAR:
            WidenSIMD12IfNecessary(node->AsLclVarCommon());
            break;

        case GT_STORE_LCL_VAR:
#if defined(_TARGET_AMD64_) && defined(FEATURE_SIMD)
        {
            GenTreeLclVarCommon* const store = node->AsLclVarCommon();
            if ((store->TypeGet() == TYP_SIMD8) != (store->gtOp1->TypeGet() == TYP_SIMD8))
            {
                GenTreeUnOp* bitcast =
                    new (comp, GT_BITCAST) GenTreeOp(GT_BITCAST, store->TypeGet(), store->gtOp1, nullptr);
                store->gtOp1 = bitcast;
                BlockRange().InsertBefore(store, bitcast);
            }
        }
#endif // _TARGET_AMD64_
            WidenSIMD12IfNecessary(node->AsLclVarCommon());
            __fallthrough;

        case GT_STORE_LCL_FLD:
            // TODO-1stClassStructs: Once we remove the requirement that all struct stores
            // are block stores (GT_STORE_BLK or GT_STORE_OBJ), here is where we would put the local
            // store under a block store if codegen will require it.
            if ((node->TypeGet() == TYP_STRUCT) && (node->gtGetOp1()->OperGet() != GT_PHI))
            {
#if FEATURE_MULTIREG_RET
                GenTree* src = node->gtGetOp1();
                assert((src->OperGet() == GT_CALL) && src->AsCall()->HasMultiRegRetVal());
#else  // !FEATURE_MULTIREG_RET
                assert(!"Unexpected struct local store in Lowering");
#endif // !FEATURE_MULTIREG_RET
            }
            LowerStoreLoc(node->AsLclVarCommon());
            break;

#ifdef _TARGET_ARM64_
        case GT_CMPXCHG:
            CheckImmedAndMakeContained(node, node->AsCmpXchg()->gtOpComparand);
            break;

        case GT_XADD:
#endif
        case GT_LOCKADD:
            CheckImmedAndMakeContained(node, node->gtOp.gtOp2);
            break;

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
    ReplaceWithLclVar(use);

    // GT_SWITCH(indexExpression) is now two statements:
    //   1. a statement containing 'asg' (for temp = indexExpression)
    //   2. and a statement with GT_SWITCH(temp)

    assert(node->gtOper == GT_SWITCH);
    GenTree* temp = node->gtOp.gtOp1;
    assert(temp->gtOper == GT_LCL_VAR);
    unsigned   tempLclNum  = temp->gtLclVarCommon.gtLclNum;
    LclVarDsc* tempVarDsc  = comp->lvaTable + tempLclNum;
    var_types  tempLclType = temp->TypeGet();

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
    GenTree* gtDefaultCaseCond = comp->gtNewOperNode(GT_GT, TYP_INT, comp->gtNewLclvNode(tempLclNum, tempLclType),
                                                     comp->gtNewIconNode(jumpCnt - 2, genActualType(tempLclType)));

    // Make sure we perform an unsigned comparison, just in case the switch index in 'temp'
    // is now less than zero 0 (that would also hit the default case).
    gtDefaultCaseCond->gtFlags |= GTF_UNSIGNED;

    /* Increment the lvRefCnt and lvRefCntWtd for temp */
    tempVarDsc->incRefCnts(blockWeight, comp);

    GenTree* gtDefaultCaseJump = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, gtDefaultCaseCond);
    gtDefaultCaseJump->gtFlags = node->gtFlags;

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

    bool useJumpSequence = jumpCnt < minSwitchTabJumpCnt;

#if defined(_TARGET_UNIX_) && defined(_TARGET_ARM_)
    // Force using an inlined jumping instead switch table generation.
    // Switch jump table is generated with incorrect values in CoreRT case,
    // so any large switch will crash after loading to PC any such value.
    // I think this is due to the fact that we use absolute addressing
    // instead of relative. But in CoreRT is used as a rule relative
    // addressing when we generate an executable.
    // This bug is also present in Legacy JIT.
    // See also https://github.com/dotnet/coreclr/issues/13194
    // Also https://github.com/dotnet/coreclr/pull/13197
    useJumpSequence = useJumpSequence || comp->IsTargetAbi(CORINFO_CORERT_ABI);
#endif // defined(_TARGET_UNIX_) && defined(_TARGET_ARM_)

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
    else if (useJumpSequence || comp->compStressCompile(Compiler::STRESS_SWITCH_CMP_BR_EXPANSION, 50))
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
                GenTree* gtCaseCond = comp->gtNewOperNode(GT_EQ, TYP_INT, comp->gtNewLclvNode(tempLclNum, tempLclType),
                                                          comp->gtNewIconNode(i, tempLclType));
                /* Increment the lvRefCnt and lvRefCntWtd for temp */
                tempVarDsc->incRefCnts(blockWeight, comp);

                GenTree*   gtCaseBranch = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, gtCaseCond);
                LIR::Range caseRange    = LIR::SeqTree(comp, gtCaseBranch);
                currentBBRange->InsertAtEnd(std::move(caseRange));
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
        // At this point the default case has already been handled and we need to generate a jump
        // table based switch or a bit test based switch at the end of afterDefaultCondBlock. Both
        // switch variants need the switch value so create the necessary LclVar node here.
        GenTree*    switchValue      = comp->gtNewLclvNode(tempLclNum, tempLclType);
        LIR::Range& switchBlockRange = LIR::AsRange(afterDefaultCondBlock);
        tempVarDsc->incRefCnts(blockWeight, comp);
        switchBlockRange.InsertAtEnd(switchValue);

        // Try generating a bit test based switch first,
        // if that's not possible a jump table based switch will be generated.
        if (!TryLowerSwitchToBitTest(jumpTab, jumpCnt, targetCnt, afterDefaultCondBlock, switchValue))
        {
            JITDUMP("Lowering switch BB%02u: using jump table expansion\n", originalSwitchBB->bbNum);

#ifdef _TARGET_64BIT_
            if (tempLclType != TYP_I_IMPL)
            {
                // SWITCH_TABLE expects the switch value (the index into the jump table) to be TYP_I_IMPL.
                // Note that the switch value is unsigned so the cast should be unsigned as well.
                switchValue = comp->gtNewCastNode(TYP_I_IMPL, switchValue, TYP_U_IMPL);
                switchValue->gtFlags |= GTF_UNSIGNED;
                switchBlockRange.InsertAtEnd(switchValue);
            }
#endif

            GenTree* switchTable = comp->gtNewJmpTableNode();
            GenTree* switchJump  = comp->gtNewOperNode(GT_SWITCH_TABLE, TYP_VOID, switchValue, switchTable);
            switchBlockRange.InsertAfter(switchValue, switchTable, switchJump);

            // this block no longer branches to the default block
            afterDefaultCondBlock->bbJumpSwt->removeDefault();
        }

        comp->fgInvalidateSwitchDescMapEntry(afterDefaultCondBlock);
    }

    GenTree* next = node->gtNext;

    // Get rid of the GT_SWITCH(temp).
    switchBBRange.Remove(node->gtOp.gtOp1);
    switchBBRange.Remove(node);

    return next;
}

//------------------------------------------------------------------------
// TryLowerSwitchToBitTest: Attempts to transform a jump table switch into a bit test.
//
// Arguments:
//    jumpTable - The jump table
//    jumpCount - The number of blocks in the jump table
//    targetCount - The number of distinct blocks in the jump table
//    bbSwitch - The switch block
//    switchValue - A LclVar node that provides the switch value
//
// Return value:
//    true if the switch has been lowered to a bit test
//
// Notes:
//    If the jump table contains less than 32 (64 on 64 bit targets) entries and there
//    are at most 2 distinct jump targets then the jump table can be converted to a word
//    of bits where a 0 bit corresponds to one jump target and a 1 bit corresponds to the
//    other jump target. Instead of the indirect jump a BT-JCC sequnce is used to jump
//    to the appropiate target:
//        mov eax, 245 ; jump table converted to a "bit table"
//        bt  eax, ebx ; ebx is supposed to contain the switch value
//        jc target1
//      target0:
//        ...
//      target1:
//    Such code is both shorter and faster (in part due to the removal of a memory load)
//    than the traditional jump table base code. And of course, it also avoids the need
//    to emit the jump table itself that can reach up to 256 bytes (for 64 entries).
//
bool Lowering::TryLowerSwitchToBitTest(
    BasicBlock* jumpTable[], unsigned jumpCount, unsigned targetCount, BasicBlock* bbSwitch, GenTree* switchValue)
{
#ifndef _TARGET_XARCH_
    // Other architectures may use this if they substitute GT_BT with equivalent code.
    return false;
#else
    assert(jumpCount >= 2);
    assert(targetCount >= 2);
    assert(bbSwitch->bbJumpKind == BBJ_SWITCH);
    assert(switchValue->OperIs(GT_LCL_VAR));

    //
    // Quick check to see if it's worth going through the jump table. The bit test switch supports
    // up to 2 targets but targetCount also includes the default block so we need to allow 3 targets.
    // We'll ensure that there are only 2 targets when building the bit table.
    //

    if (targetCount > 3)
    {
        return false;
    }

    //
    // The number of bits in the bit table is the same as the number of jump table entries. But the
    // jump table also includes the default target (at the end) so we need to ignore it. The default
    // has already been handled by a JTRUE(GT(switchValue, jumpCount - 2)) that LowerSwitch generates.
    //

    const unsigned bitCount = jumpCount - 1;

    if (bitCount > (genTypeSize(TYP_I_IMPL) * 8))
    {
        return false;
    }

    //
    // Build a bit table where a bit set to 0 corresponds to bbCase0 and a bit set to 1 corresponds to
    // bbCase1. Simply use the first block in the jump table as bbCase1, later we can invert the bit
    // table and/or swap the blocks if it's beneficial.
    //

    BasicBlock* bbCase0  = nullptr;
    BasicBlock* bbCase1  = jumpTable[0];
    size_t      bitTable = 1;

    for (unsigned bitIndex = 1; bitIndex < bitCount; bitIndex++)
    {
        if (jumpTable[bitIndex] == bbCase1)
        {
            bitTable |= (size_t(1) << bitIndex);
        }
        else if (bbCase0 == nullptr)
        {
            bbCase0 = jumpTable[bitIndex];
        }
        else if (jumpTable[bitIndex] != bbCase0)
        {
            // If it's neither bbCase0 nor bbCase1 then it means we have 3 targets. There can't be more
            // than 3 because of the check at the start of the function.
            assert(targetCount == 3);
            return false;
        }
    }

    //
    // One of the case blocks has to follow the switch block. This requirement could be avoided
    // by adding a BBJ_ALWAYS block after the switch block but doing that sometimes negatively
    // impacts register allocation.
    //

    if ((bbSwitch->bbNext != bbCase0) && (bbSwitch->bbNext != bbCase1))
    {
        return false;
    }

#ifdef _TARGET_64BIT_
    //
    // See if we can avoid a 8 byte immediate on 64 bit targets. If all upper 32 bits are 1
    // then inverting the bit table will make them 0 so that the table now fits in 32 bits.
    // Note that this does not change the number of bits in the bit table, it just takes
    // advantage of the fact that loading a 32 bit immediate into a 64 bit register zero
    // extends the immediate value to 64 bit.
    //

    if (~bitTable <= UINT32_MAX)
    {
        bitTable = ~bitTable;
        std::swap(bbCase0, bbCase1);
    }
#endif

    //
    // Rewire the blocks as needed and figure out the condition to use for JCC.
    //

    genTreeOps bbSwitchCondition = GT_NONE;
    bbSwitch->bbJumpKind         = BBJ_COND;

    comp->fgRemoveAllRefPreds(bbCase1, bbSwitch);
    comp->fgRemoveAllRefPreds(bbCase0, bbSwitch);

    if (bbSwitch->bbNext == bbCase0)
    {
        // GT_LT + GTF_UNSIGNED generates JC so we jump to bbCase1 when the bit is set
        bbSwitchCondition    = GT_LT;
        bbSwitch->bbJumpDest = bbCase1;

        comp->fgAddRefPred(bbCase0, bbSwitch);
        comp->fgAddRefPred(bbCase1, bbSwitch);
    }
    else
    {
        assert(bbSwitch->bbNext == bbCase1);

        // GT_GE + GTF_UNSIGNED generates JNC so we jump to bbCase0 when the bit is not set
        bbSwitchCondition    = GT_GE;
        bbSwitch->bbJumpDest = bbCase0;

        comp->fgAddRefPred(bbCase0, bbSwitch);
        comp->fgAddRefPred(bbCase1, bbSwitch);
    }

    //
    // Append BT(bitTable, switchValue) and JCC(condition) to the switch block.
    //

    var_types bitTableType = (bitCount <= (genTypeSize(TYP_INT) * 8)) ? TYP_INT : TYP_LONG;
    GenTree*  bitTableIcon = comp->gtNewIconNode(bitTable, bitTableType);
    GenTree*  bitTest      = comp->gtNewOperNode(GT_BT, TYP_VOID, bitTableIcon, switchValue);
    bitTest->gtFlags |= GTF_SET_FLAGS;
    GenTreeCC* jcc = new (comp, GT_JCC) GenTreeCC(GT_JCC, bbSwitchCondition);
    jcc->gtFlags |= GTF_UNSIGNED | GTF_USE_FLAGS;

    LIR::AsRange(bbSwitch).InsertAfter(switchValue, bitTableIcon, bitTest, jcc);

    return true;
#endif // _TARGET_XARCH_
}

// NOTE: this method deliberately does not update the call arg table. It must only
// be used by NewPutArg and LowerArg; these functions are responsible for updating
// the call arg table as necessary.
void Lowering::ReplaceArgWithPutArgOrBitcast(GenTree** argSlot, GenTree* putArgOrBitcast)
{
    assert(argSlot != nullptr);
    assert(*argSlot != nullptr);
    assert(putArgOrBitcast->OperIsPutArg() || putArgOrBitcast->OperIs(GT_BITCAST));

    GenTree* arg = *argSlot;

    // Replace the argument with the putarg/copy
    *argSlot                    = putArgOrBitcast;
    putArgOrBitcast->gtOp.gtOp1 = arg;

    // Insert the putarg/copy into the block
    BlockRange().InsertAfter(arg, putArgOrBitcast);
}

//------------------------------------------------------------------------
// NewPutArg: rewrites the tree to put an arg in a register or on the stack.
//
// Arguments:
//    call - the call whose arg is being rewritten.
//    arg  - the arg being rewritten.
//    info - the fgArgTabEntry information for the argument.
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
//    this method allocates a single GT_PUTARG_REG for 1 eightbyte structs and a GT_FIELD_LIST of two GT_PUTARG_REGs
//    for two eightbyte structs.
//
//    For STK passed structs the method generates GT_PUTARG_STK tree. For System V systems with native struct passing
//    (i.e. FEATURE_UNIX_AMD64_STRUCT_PASSING defined) this method also sets the GC pointers count and the pointers
//    layout object, so the codegen of the GT_PUTARG_STK could use this for optimizing copying to the stack by value.
//    (using block copy primitives for non GC pointers and a single TARGET_POINTER_SIZE copy with recording GC info.)
//
GenTree* Lowering::NewPutArg(GenTreeCall* call, GenTree* arg, fgArgTabEntry* info, var_types type)
{
    assert(call != nullptr);
    assert(arg != nullptr);
    assert(info != nullptr);

    GenTree* putArg         = nullptr;
    bool     updateArgTable = true;

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

#ifdef _TARGET_ARMARCH_
    // Mark contained when we pass struct
    // GT_FIELD_LIST is always marked conatained when it is generated
    if (type == TYP_STRUCT)
    {
        arg->SetContained();
        if ((arg->OperGet() == GT_OBJ) && (arg->AsObj()->Addr()->OperGet() == GT_LCL_VAR_ADDR))
        {
            MakeSrcContained(arg, arg->AsObj()->Addr());
        }
    }
#endif

#ifdef _TARGET_ARM_
    // Struct can be split into register(s) and stack on ARM
    if (info->isSplit)
    {
        assert(arg->OperGet() == GT_OBJ || arg->OperGet() == GT_FIELD_LIST);
        // TODO: Need to check correctness for FastTailCall
        if (call->IsFastTailCall())
        {
            NYI_ARM("lower: struct argument by fast tail call");
        }

        putArg = new (comp, GT_PUTARG_SPLIT)
            GenTreePutArgSplit(arg, info->slotNum PUT_STRUCT_ARG_STK_ONLY_ARG(info->numSlots), info->numRegs,
                               call->IsFastTailCall(), call);
        putArg->gtRegNum = info->regNum;

        // If struct argument is morphed to GT_FIELD_LIST node(s),
        // we can know GC info by type of each GT_FIELD_LIST node.
        // So we skip setting GC Pointer info.
        //
        GenTreePutArgSplit* argSplit = putArg->AsPutArgSplit();
        if (arg->OperGet() == GT_OBJ)
        {
            BYTE*       gcLayout = nullptr;
            unsigned    numRefs  = 0;
            GenTreeObj* argObj   = arg->AsObj();

            if (argObj->IsGCInfoInitialized())
            {
                gcLayout = argObj->gtGcPtrs;
                numRefs  = argObj->GetGcPtrCount();
            }
            else
            {
                // Set GC Pointer info
                gcLayout = new (comp, CMK_Codegen) BYTE[info->numSlots + info->numRegs];
                numRefs  = comp->info.compCompHnd->getClassGClayout(arg->gtObj.gtClass, gcLayout);
                argSplit->setGcPointers(numRefs, gcLayout);
            }

            // Set type of registers
            for (unsigned index = 0; index < info->numRegs; index++)
            {
                var_types regType          = comp->getJitGCType(gcLayout[index]);
                argSplit->m_regType[index] = regType;
            }
        }
        else
        {
            GenTreeFieldList* fieldListPtr = arg->AsFieldList();
            for (unsigned index = 0; index < info->numRegs; fieldListPtr = fieldListPtr->Rest(), index++)
            {
                var_types regType          = fieldListPtr->gtGetOp1()->TypeGet();
                argSplit->m_regType[index] = regType;

                // Clear the register assignments on the fieldList nodes, as these are contained.
                fieldListPtr->gtRegNum = REG_NA;
            }
        }
    }
    else
#endif // _TARGET_ARM_
    {
        if (!isOnStack)
        {
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
                //    in this case the arg comes as a GT_FIELD_LIST of two GT_LCL_FLDs
                //     - the two eightbytes of the struct.
                //    The code creates a GT_PUTARG_REG node for each GT_LCL_FLD in the GT_FIELD_LIST
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

                    putArg = comp->gtNewPutArgReg(type, arg, info->regNum);
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

                    assert(arg->OperGet() == GT_FIELD_LIST);

                    GenTreeFieldList* fieldListPtr = arg->AsFieldList();
                    assert(fieldListPtr->IsFieldListHead());

                    for (unsigned ctr = 0; fieldListPtr != nullptr; fieldListPtr = fieldListPtr->Rest(), ctr++)
                    {
                        // Create a new GT_PUTARG_REG node with op1 the original GT_LCL_FLD.
                        GenTree* newOper = comp->gtNewPutArgReg(
                            comp->GetTypeFromClassificationAndSizes(info->structDesc.eightByteClassifications[ctr],
                                                                    info->structDesc.eightByteSizes[ctr]),
                            fieldListPtr->gtOp.gtOp1, (ctr == 0) ? info->regNum : info->otherRegNum);

                        // Splice in the new GT_PUTARG_REG node in the GT_FIELD_LIST
                        ReplaceArgWithPutArgOrBitcast(&fieldListPtr->gtOp.gtOp1, newOper);

                        // Initialize all the gtRegNum's since the list won't be traversed in an LIR traversal.
                        fieldListPtr->gtRegNum = REG_NA;
                    }

                    // Just return arg. The GT_FIELD_LIST is not replaced.
                    // Nothing more to do.
                    return arg;
                }
                else
                {
                    assert(false && "Illegal count of eightbytes for the CLR type system"); // No more than 2 eightbytes
                                                                                            // for the CLR.
                }
            }
            else
#else // not defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#if FEATURE_MULTIREG_ARGS
            if ((info->numRegs > 1) && (arg->OperGet() == GT_FIELD_LIST))
            {
                assert(arg->OperGet() == GT_FIELD_LIST);

                GenTreeFieldList* fieldListPtr = arg->AsFieldList();
                assert(fieldListPtr->IsFieldListHead());

                // There could be up to 2-4 PUTARG_REGs in the list (3 or 4 can only occur for HFAs)
                regNumber argReg = info->regNum;
                for (unsigned ctr = 0; fieldListPtr != nullptr; fieldListPtr = fieldListPtr->Rest(), ctr++)
                {
                    GenTree*  curOp  = fieldListPtr->gtOp.gtOp1;
                    var_types curTyp = curOp->TypeGet();

                    // Create a new GT_PUTARG_REG node with op1
                    GenTree* newOper = comp->gtNewPutArgReg(curTyp, curOp, argReg);

                    // Splice in the new GT_PUTARG_REG node in the GT_FIELD_LIST
                    ReplaceArgWithPutArgOrBitcast(&fieldListPtr->gtOp.gtOp1, newOper);

                    // Update argReg for the next putarg_reg (if any)
                    argReg = genRegArgNext(argReg);

#if defined(_TARGET_ARM_)
                    // A double register is modelled as an even-numbered single one
                    if (fieldListPtr->Current()->TypeGet() == TYP_DOUBLE)
                    {
                        argReg = genRegArgNext(argReg);
                    }
#endif // _TARGET_ARM_
                    // Initialize all the gtRegNum's since the list won't be traversed in an LIR traversal.
                    fieldListPtr->gtRegNum = REG_NA;
                }

                // Just return arg. The GT_FIELD_LIST is not replaced.
                // Nothing more to do.
                return arg;
            }
            else
#endif // FEATURE_MULTIREG_ARGS
#endif // not defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
            {
                putArg = comp->gtNewPutArgReg(type, arg, info->regNum);
            }
        }
        else
        {
            // Mark this one as tail call arg if it is a fast tail call.
            // This provides the info to put this argument in in-coming arg area slot
            // instead of in out-going arg area slot.

            // Make sure state is correct. The PUTARG_STK has TYP_VOID, as it doesn't produce
            // a result. So the type of its operand must be the correct type to push on the stack.
            // For a FIELD_LIST, this will be the type of the field (not the type of the arg),
            // but otherwise it is generally the type of the operand.
            PUT_STRUCT_ARG_STK_ONLY(assert(info->isStruct == varTypeIsStruct(type)));
            if ((arg->OperGet() != GT_FIELD_LIST))
            {
#if defined(FEATURE_SIMD) && defined(FEATURE_PUT_STRUCT_ARG_STK)
                if (type == TYP_SIMD12)
                {
                    assert(info->numSlots == 3);
                }
                else
#endif // defined(FEATURE_SIMD) && defined(FEATURE_PUT_STRUCT_ARG_STK)
                {
                    assert(genActualType(arg->TypeGet()) == type);
                }
            }

            putArg =
                new (comp, GT_PUTARG_STK) GenTreePutArgStk(GT_PUTARG_STK, TYP_VOID, arg,
                                                           info->slotNum PUT_STRUCT_ARG_STK_ONLY_ARG(info->numSlots),
                                                           call->IsFastTailCall(), call);

#ifdef FEATURE_PUT_STRUCT_ARG_STK
            // If the ArgTabEntry indicates that this arg is a struct
            // get and store the number of slots that are references.
            // This is later used in the codegen for PUT_ARG_STK implementation
            // for struct to decide whether and how many single eight-byte copies
            // to be done (only for reference slots), so gcinfo is emitted.
            // For non-reference slots faster/smaller size instructions are used -
            // pair copying using XMM registers or rep mov instructions.
            if (info->isStruct)
            {
                // We use GT_OBJ for non-SIMD struct arguments. However, for
                // SIMD arguments the GT_OBJ has already been transformed.
                if (arg->gtOper != GT_OBJ)
                {
                    assert(varTypeIsSIMD(arg));
                }
                else
                {
                    unsigned numRefs  = 0;
                    BYTE*    gcLayout = new (comp, CMK_Codegen) BYTE[info->numSlots];
                    assert(!varTypeIsSIMD(arg));
                    numRefs = comp->info.compCompHnd->getClassGClayout(arg->gtObj.gtClass, gcLayout);
                    putArg->AsPutArgStk()->setGcPointers(numRefs, gcLayout);

#ifdef _TARGET_X86_
                    // On x86 VM lies about the type of a struct containing a pointer sized
                    // integer field by returning the type of its field as the type of struct.
                    // Such struct can be passed in a register depending its position in
                    // parameter list.  VM does this unwrapping only one level and therefore
                    // a type like Struct Foo { Struct Bar { int f}} awlays needs to be
                    // passed on stack.  Also, VM doesn't lie about type of such a struct
                    // when it is a field of another struct.  That is VM doesn't lie about
                    // the type of Foo.Bar
                    //
                    // We now support the promotion of fields that are of type struct.
                    // However we only support a limited case where the struct field has a
                    // single field and that single field must be a scalar type. Say Foo.Bar
                    // field is getting passed as a parameter to a call, Since it is a TYP_STRUCT,
                    // as per x86 ABI it should always be passed on stack.  Therefore GenTree
                    // node under a PUTARG_STK could be GT_OBJ(GT_LCL_VAR_ADDR(v1)), where
                    // local v1 could be a promoted field standing for Foo.Bar.  Note that
                    // the type of v1 will be the type of field of Foo.Bar.f when Foo is
                    // promoted.  That is v1 will be a scalar type.  In this case we need to
                    // pass v1 on stack instead of in a register.
                    //
                    // TODO-PERF: replace GT_OBJ(GT_LCL_VAR_ADDR(v1)) with v1 if v1 is
                    // a scalar type and the width of GT_OBJ matches the type size of v1.
                    // Note that this cannot be done till call node arguments are morphed
                    // because we should not lose the fact that the type of argument is
                    // a struct so that the arg gets correctly marked to be passed on stack.
                    GenTree* objOp1 = arg->gtGetOp1();
                    if (objOp1->OperGet() == GT_LCL_VAR_ADDR)
                    {
                        unsigned lclNum = objOp1->AsLclVarCommon()->GetLclNum();
                        if (comp->lvaTable[lclNum].lvType != TYP_STRUCT)
                        {
                            comp->lvaSetVarDoNotEnregister(lclNum DEBUGARG(Compiler::DNER_VMNeedsStackAddr));
                        }
                    }
#endif // _TARGET_X86_
                }
            }
#endif // FEATURE_PUT_STRUCT_ARG_STK
        }
    }

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
void Lowering::LowerArg(GenTreeCall* call, GenTree** ppArg)
{
    GenTree* arg = *ppArg;

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

    fgArgTabEntry* info = comp->gtArgEntryByNode(call, arg);
    assert(info->node == arg);
    var_types type = arg->TypeGet();

    if (varTypeIsSmall(type))
    {
        // Normalize 'type', it represents the item that we will be storing in the Outgoing Args
        type = TYP_INT;
    }

#if defined(FEATURE_SIMD)
#if defined(_TARGET_X86_)
    // Non-param TYP_SIMD12 local var nodes are massaged in Lower to TYP_SIMD16 to match their
    // allocated size (see lvSize()). However, when passing the variables as arguments, and
    // storing the variables to the outgoing argument area on the stack, we must use their
    // actual TYP_SIMD12 type, so exactly 12 bytes is allocated and written.
    if (type == TYP_SIMD16)
    {
        if ((arg->OperGet() == GT_LCL_VAR) || (arg->OperGet() == GT_STORE_LCL_VAR))
        {
            unsigned   varNum = arg->AsLclVarCommon()->GetLclNum();
            LclVarDsc* varDsc = &comp->lvaTable[varNum];
            type              = varDsc->lvType;
        }
        else if (arg->OperGet() == GT_SIMD)
        {
            assert((arg->AsSIMD()->gtSIMDSize == 16) || (arg->AsSIMD()->gtSIMDSize == 12));

            if (arg->AsSIMD()->gtSIMDSize == 12)
            {
                type = TYP_SIMD12;
            }
        }
    }
#elif defined(_TARGET_AMD64_)
    // TYP_SIMD8 parameters that are passed as longs
    if (type == TYP_SIMD8 && genIsValidIntReg(info->regNum))
    {
        GenTreeUnOp* bitcast = new (comp, GT_BITCAST) GenTreeOp(GT_BITCAST, TYP_LONG, arg, nullptr);
        BlockRange().InsertAfter(arg, bitcast);

        info->node = *ppArg = arg = bitcast;
        type                      = TYP_LONG;
    }
#endif // defined(_TARGET_X86_)
#endif // defined(FEATURE_SIMD)

    // If we hit this we are probably double-lowering.
    assert(!arg->OperIsPutArg());

#if !defined(_TARGET_64BIT_)
    if (varTypeIsLong(type))
    {
        bool isReg = (info->regNum != REG_STK);
        if (isReg)
        {
            noway_assert(arg->OperGet() == GT_LONG);
            assert(info->numRegs == 2);

            GenTree* argLo = arg->gtGetOp1();
            GenTree* argHi = arg->gtGetOp2();

            GenTreeFieldList* fieldList = new (comp, GT_FIELD_LIST) GenTreeFieldList(argLo, 0, TYP_INT, nullptr);
            // Only the first fieldList node (GTF_FIELD_LIST_HEAD) is in the instruction sequence.
            (void)new (comp, GT_FIELD_LIST) GenTreeFieldList(argHi, 4, TYP_INT, fieldList);
            GenTree* putArg = NewPutArg(call, fieldList, info, type);

            BlockRange().InsertBefore(arg, putArg);
            BlockRange().Remove(arg);
            *ppArg     = fieldList;
            info->node = fieldList;
        }
        else
        {
            assert(arg->OperGet() == GT_LONG);
            // For longs, we will replace the GT_LONG with a GT_FIELD_LIST, and put that under a PUTARG_STK.
            // Although the hi argument needs to be pushed first, that will be handled by the general case,
            // in which the fields will be reversed.
            assert(info->numSlots == 2);
            GenTree*          argLo     = arg->gtGetOp1();
            GenTree*          argHi     = arg->gtGetOp2();
            GenTreeFieldList* fieldList = new (comp, GT_FIELD_LIST) GenTreeFieldList(argLo, 0, TYP_INT, nullptr);
            // Only the first fieldList node (GTF_FIELD_LIST_HEAD) is in the instruction sequence.
            (void)new (comp, GT_FIELD_LIST) GenTreeFieldList(argHi, 4, TYP_INT, fieldList);
            GenTree* putArg  = NewPutArg(call, fieldList, info, type);
            putArg->gtRegNum = info->regNum;

            // We can't call ReplaceArgWithPutArgOrBitcast here because it presumes that we are keeping the original
            // arg.
            BlockRange().InsertBefore(arg, fieldList, putArg);
            BlockRange().Remove(arg);
            *ppArg = putArg;
        }
    }
    else
#endif // !defined(_TARGET_64BIT_)
    {

#ifdef _TARGET_ARMARCH_
        if (call->IsVarargs() || comp->opts.compUseSoftFP)
        {
            // For vararg call or on armel, reg args should be all integer.
            // Insert copies as needed to move float value to integer register.
            GenTree* newNode = LowerFloatArg(ppArg, info);
            if (newNode != nullptr)
            {
                type = newNode->TypeGet();
            }
        }
#endif // _TARGET_ARMARCH_

        GenTree* putArg = NewPutArg(call, arg, info, type);

        // In the case of register passable struct (in one or two registers)
        // the NewPutArg returns a new node (GT_PUTARG_REG or a GT_FIELD_LIST with two GT_PUTARG_REGs.)
        // If an extra node is returned, splice it in the right place in the tree.
        if (arg != putArg)
        {
            ReplaceArgWithPutArgOrBitcast(ppArg, putArg);
        }
    }
}

#ifdef _TARGET_ARMARCH_
//------------------------------------------------------------------------
// LowerFloatArg: Lower float call arguments on the arm platform.
//
// Arguments:
//    arg  - The arg node
//    info - call argument info
//
// Return Value:
//    Return nullptr, if no transformation was done;
//    return arg if there was in place transformation;
//    return a new tree if the root was changed.
//
// Notes:
//    This must handle scalar float arguments as well as GT_FIELD_LISTs
//    with floating point fields.
//
GenTree* Lowering::LowerFloatArg(GenTree** pArg, fgArgTabEntry* info)
{
    GenTree* arg = *pArg;
    if (info->regNum != REG_STK)
    {
        if (arg->OperIsFieldList())
        {
            GenTreeFieldList* currListNode  = arg->AsFieldList();
            regNumber         currRegNumber = info->regNum;

            // Transform fields that are passed as registers in place.
            unsigned fieldRegCount;
            for (unsigned i = 0; i < info->numRegs; i += fieldRegCount)
            {
                assert(currListNode != nullptr);
                GenTree* node = currListNode->Current();
                if (varTypeIsFloating(node))
                {
                    GenTree* intNode = LowerFloatArgReg(node, currRegNumber);
                    assert(intNode != nullptr);

                    ReplaceArgWithPutArgOrBitcast(currListNode->pCurrent(), intNode);
                    currListNode->ChangeType(intNode->TypeGet());
                }

                if (node->TypeGet() == TYP_DOUBLE)
                {
                    currRegNumber = REG_NEXT(REG_NEXT(currRegNumber));
                    fieldRegCount = 2;
                }
                else
                {
                    currRegNumber = REG_NEXT(currRegNumber);
                    fieldRegCount = 1;
                }
                currListNode = currListNode->Rest();
            }
            // List fields were replaced in place.
            return arg;
        }
        else if (varTypeIsFloating(arg))
        {
            GenTree* intNode = LowerFloatArgReg(arg, info->regNum);
            assert(intNode != nullptr);
            ReplaceArgWithPutArgOrBitcast(pArg, intNode);
            return *pArg;
        }
    }
    return nullptr;
}

//------------------------------------------------------------------------
// LowerFloatArgReg: Lower the float call argument node that is passed via register.
//
// Arguments:
//    arg    - The arg node
//    regNum - register number
//
// Return Value:
//    Return new bitcast node, that moves float to int register.
//
GenTree* Lowering::LowerFloatArgReg(GenTree* arg, regNumber regNum)
{
    var_types floatType = arg->TypeGet();
    assert(varTypeIsFloating(floatType));
    var_types intType = (floatType == TYP_DOUBLE) ? TYP_LONG : TYP_INT;
    GenTree*  intArg  = comp->gtNewBitCastNode(intType, arg);
    intArg->gtRegNum  = regNum;
#ifdef _TARGET_ARM_
    if (floatType == TYP_DOUBLE)
    {
        regNumber nextReg                  = REG_NEXT(regNum);
        intArg->AsMultiRegOp()->gtOtherReg = nextReg;
    }
#endif
    return intArg;
}
#endif

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
GenTree* Lowering::AddrGen(ssize_t addr)
{
    // this should end up in codegen as : instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, reg, addr)
    GenTree* result = comp->gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);
    return result;
}

// variant that takes a void*
GenTree* Lowering::AddrGen(void* addr)
{
    return AddrGen((ssize_t)addr);
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

    call->ClearOtherRegs();
    LowerArgsForCall(call);

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

        ContainCheckRange(resultRange);
        BlockRange().InsertBefore(insertionPoint, std::move(resultRange));

        call->gtControlExpr = result;
    }

    if (comp->opts.IsJit64Compat())
    {
        CheckVSQuirkStackPaddingNeeded(call);
    }

    ContainCheckCallOperands(call);
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
        bool     paddingNeeded  = false;
        GenTree* firstPutArgReg = nullptr;
        for (GenTreeArgList* args = call->gtCallLateArgs; args; args = args->Rest())
        {
            GenTree* tmp = args->Current();
            if (tmp->OperGet() == GT_PUTARG_REG)
            {
                if (firstPutArgReg == nullptr)
                {
                    firstPutArgReg = tmp;
                    GenTree* op1   = firstPutArgReg->gtOp.gtOp1;

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
// AMD64:
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
// X86:
// Insert the profiler hook immediately before the call. The profiler hook will preserve
// all argument registers (ECX, EDX), but nothing else.
//
// Params:
//    callNode        - tail call node
//    insertionPoint  - if non-null, insert the profiler hook before this point.
//                      If null, insert the profiler hook before args are setup
//                      but after all arg side effects are computed.
//
void Lowering::InsertProfTailCallHook(GenTreeCall* call, GenTree* insertionPoint)
{
    assert(call->IsTailCall());
    assert(comp->compIsProfilerHookNeeded());

#if defined(_TARGET_X86_)

    if (insertionPoint == nullptr)
    {
        insertionPoint = call;
    }

#else // !defined(_TARGET_X86_)

    if (insertionPoint == nullptr)
    {
        GenTree* tmp = nullptr;
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

#endif // !defined(_TARGET_X86_)

    assert(insertionPoint != nullptr);
    GenTree* profHookNode = new (comp, GT_PROF_HOOK) GenTree(GT_PROF_HOOK, TYP_VOID);
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

#ifdef _TARGET_AMD64_
    assert(!comp->getNeedsGSSecurityCookie()); // jit64 compat: tail calls from methods that need GS check
#endif                                         // _TARGET_AMD64_

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
    GenTree*             firstPutArgStk = nullptr;
    GenTreeArgList*      args;
    ArrayStack<GenTree*> putargs(comp);

    for (args = call->gtCallArgs; args; args = args->Rest())
    {
        GenTree* tmp = args->Current();
        if (tmp->OperGet() == GT_PUTARG_STK)
        {
            putargs.Push(tmp);
        }
    }

    for (args = call->gtCallLateArgs; args; args = args->Rest())
    {
        GenTree* tmp = args->Current();
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
        GenTree* putArgStkNode = putargs.Bottom(i);

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
        fgArgTabEntry* argTabEntry = comp->gtArgEntryByNode(call, putArgStkNode);
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
        for (GenTree* treeNode = putArgStkNode->gtNext; treeNode != call; treeNode = treeNode->gtNext)
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
                        comp->lvaSortAgain                          = true;
                        tmpType                                     = genActualType(callerArgDsc->lvaArgType());
                        comp->lvaTable[tmpLclNum].lvType            = tmpType;
                        comp->lvaTable[tmpLclNum].lvRefCnt          = 1;
                        comp->lvaTable[tmpLclNum].lvDoNotEnregister = comp->lvaTable[lcl->gtLclNum].lvDoNotEnregister;
                    }

                    lcl->SetLclNum(tmpLclNum);
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
            ContainCheckRange(local, assignExpr);
            BlockRange().InsertBefore(firstPutArgStk, LIR::SeqTree(comp, assignExpr));
        }
    }

    // Insert GT_START_NONGC node before the first GT_PUTARG_STK node.
    // Note that if there are no args to be setup on stack, no need to
    // insert GT_START_NONGC node.
    GenTree* startNonGCNode = nullptr;
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
            GenTree* noOp = new (comp, GT_NO_OP) GenTree(GT_NO_OP, TYP_VOID);
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
// For ARM32, AMD64, lower
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

#ifdef _TARGET_AMD64_
    assert(!comp->getNeedsGSSecurityCookie()); // jit64 compat: tail calls from methods that need GS check
#endif                                         // _TARGET_AMD64_

    // We expect to see a call that meets the following conditions
    assert(call->IsTailCallViaHelper());
    assert(callTarget != nullptr);

    // The TailCall helper call never returns to the caller and is not GC interruptible.
    // Therefore the block containing the tail call should be a GC safe point to avoid
    // GC starvation. It is legal for the block to be unmarked iff the entry block is a
    // GC safe point, as the entry block trivially dominates every reachable block.
    assert((comp->compCurBB->bbFlags & BBF_GC_SAFE_POINT) || (comp->fgFirstBB->bbFlags & BBF_GC_SAFE_POINT));

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

#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM_)

// For ARM32 and AMD64, first argument is CopyRoutine and second argument is a place holder node.

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

    ContainCheckRange(callTargetRange);
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

    ContainCheckRange(callTargetRange);
    BlockRange().InsertAfter(arg0, std::move(callTargetRange));

    bool               isClosed;
    LIR::ReadOnlyRange secondArgRange = BlockRange().GetTreeRange(arg0, &isClosed);
    assert(isClosed);
    BlockRange().Remove(std::move(secondArgRange));

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

#ifdef PROFILING_SUPPORTED
    // Insert profiler tail call hook if needed.
    // Since we don't know the insertion point, pass null for second param.
    if (comp->compIsProfilerHookNeeded())
    {
        InsertProfTailCallHook(call, nullptr);
    }
#endif // PROFILING_SUPPORTED

    assert(call->IsTailCallViaHelper());

    return result;
}

#ifndef _TARGET_64BIT_
//------------------------------------------------------------------------
// Lowering::DecomposeLongCompare: Decomposes a TYP_LONG compare node.
//
// Arguments:
//    cmp - the compare node
//
// Return Value:
//    The next node to lower.
//
// Notes:
//    This is done during lowering because DecomposeLongs handles only nodes
//    that produce TYP_LONG values. Compare nodes may consume TYP_LONG values
//    but produce TYP_INT values.
//
GenTree* Lowering::DecomposeLongCompare(GenTree* cmp)
{
    assert(cmp->gtGetOp1()->TypeGet() == TYP_LONG);

    GenTree* src1 = cmp->gtGetOp1();
    GenTree* src2 = cmp->gtGetOp2();
    assert(src1->OperIs(GT_LONG));
    assert(src2->OperIs(GT_LONG));
    GenTree* loSrc1 = src1->gtGetOp1();
    GenTree* hiSrc1 = src1->gtGetOp2();
    GenTree* loSrc2 = src2->gtGetOp1();
    GenTree* hiSrc2 = src2->gtGetOp2();
    BlockRange().Remove(src1);
    BlockRange().Remove(src2);

    genTreeOps condition = cmp->OperGet();
    GenTree*   loCmp;
    GenTree*   hiCmp;

    if (cmp->OperIs(GT_EQ, GT_NE))
    {
        //
        // Transform (x EQ|NE y) into (((x.lo XOR y.lo) OR (x.hi XOR y.hi)) EQ|NE 0). If y is 0 then this can
        // be reduced to just ((x.lo OR x.hi) EQ|NE 0). The OR is expected to set the condition flags so we
        // don't need to generate a redundant compare against 0, we only generate a SETCC|JCC instruction.
        //
        // XOR is used rather than SUB because it is commutative and thus allows swapping the operands when
        // the first happens to be a constant. Usually only the second compare operand is a constant but it's
        // still possible to have a constant on the left side. For example, when src1 is a uint->ulong cast
        // then hiSrc1 would be 0.
        //

        if (loSrc1->OperIs(GT_CNS_INT))
        {
            std::swap(loSrc1, loSrc2);
        }

        if (loSrc2->IsIntegralConst(0))
        {
            BlockRange().Remove(loSrc2);
            loCmp = loSrc1;
        }
        else
        {
            loCmp = comp->gtNewOperNode(GT_XOR, TYP_INT, loSrc1, loSrc2);
            BlockRange().InsertBefore(cmp, loCmp);
            ContainCheckBinary(loCmp->AsOp());
        }

        if (hiSrc1->OperIs(GT_CNS_INT))
        {
            std::swap(hiSrc1, hiSrc2);
        }

        if (hiSrc2->IsIntegralConst(0))
        {
            BlockRange().Remove(hiSrc2);
            hiCmp = hiSrc1;
        }
        else
        {
            hiCmp = comp->gtNewOperNode(GT_XOR, TYP_INT, hiSrc1, hiSrc2);
            BlockRange().InsertBefore(cmp, hiCmp);
            ContainCheckBinary(hiCmp->AsOp());
        }

        hiCmp = comp->gtNewOperNode(GT_OR, TYP_INT, loCmp, hiCmp);
        BlockRange().InsertBefore(cmp, hiCmp);
        ContainCheckBinary(hiCmp->AsOp());
    }
    else
    {
        assert(cmp->OperIs(GT_LT, GT_LE, GT_GE, GT_GT));

        //
        // If the compare is signed then (x LT|GE y) can be transformed into ((x SUB y) LT|GE 0).
        // If the compare is unsigned we can still use SUB but we need to check the Carry flag,
        // not the actual result. In both cases we can simply check the appropiate condition flags
        // and ignore the actual result:
        //     SUB_LO loSrc1, loSrc2
        //     SUB_HI hiSrc1, hiSrc2
        //     SETCC|JCC (signed|unsigned LT|GE)
        // If loSrc2 happens to be 0 then the first SUB can be eliminated and the second one can
        // be turned into a CMP because the first SUB would have set carry to 0. This effectively
        // transforms a long compare against 0 into an int compare of the high part against 0.
        //
        // (x LE|GT y) can to be transformed into ((x SUB y) LE|GT 0) but checking that a long value
        // is greater than 0 is not so easy. We need to turn this into a positive/negative check
        // like the one we get for LT|GE compares, this can be achieved by swapping the compare:
        //     (x LE|GT y) becomes (y GE|LT x)
        //
        // Having to swap operands is problematic when the second operand is a constant. The constant
        // moves to the first operand where it cannot be contained and thus needs a register. This can
        // be avoided by changing the constant such that LE|GT becomes LT|GE:
        //     (x LE|GT 41) becomes (x LT|GE 42)
        //

        if (cmp->OperIs(GT_LE, GT_GT))
        {
            bool mustSwap = true;

            if (loSrc2->OperIs(GT_CNS_INT) && hiSrc2->OperIs(GT_CNS_INT))
            {
                uint32_t loValue  = static_cast<uint32_t>(loSrc2->AsIntCon()->IconValue());
                uint32_t hiValue  = static_cast<uint32_t>(hiSrc2->AsIntCon()->IconValue());
                uint64_t value    = static_cast<uint64_t>(loValue) | (static_cast<uint64_t>(hiValue) << 32);
                uint64_t maxValue = cmp->IsUnsigned() ? UINT64_MAX : INT64_MAX;

                if (value != maxValue)
                {
                    value++;
                    loValue = value & UINT32_MAX;
                    hiValue = (value >> 32) & UINT32_MAX;
                    loSrc2->AsIntCon()->SetIconValue(loValue);
                    hiSrc2->AsIntCon()->SetIconValue(hiValue);

                    condition = cmp->OperIs(GT_LE) ? GT_LT : GT_GE;
                    mustSwap  = false;
                }
            }

            if (mustSwap)
            {
                std::swap(loSrc1, loSrc2);
                std::swap(hiSrc1, hiSrc2);
                condition = GenTree::SwapRelop(condition);
            }
        }

        assert((condition == GT_LT) || (condition == GT_GE));

        if (loSrc2->IsIntegralConst(0))
        {
            BlockRange().Remove(loSrc2);

            // Very conservative dead code removal... but it helps.

            if (loSrc1->OperIs(GT_CNS_INT, GT_LCL_VAR, GT_LCL_FLD))
            {
                BlockRange().Remove(loSrc1);

                if (loSrc1->OperIs(GT_LCL_VAR, GT_LCL_FLD))
                {
                    comp->lvaDecRefCnts(m_block, loSrc1);
                }
            }
            else
            {
                loSrc1->SetUnusedValue();
            }

            hiCmp = comp->gtNewOperNode(GT_CMP, TYP_VOID, hiSrc1, hiSrc2);
            BlockRange().InsertBefore(cmp, hiCmp);
            ContainCheckCompare(hiCmp->AsOp());
        }
        else
        {
            loCmp = comp->gtNewOperNode(GT_CMP, TYP_VOID, loSrc1, loSrc2);
            hiCmp = comp->gtNewOperNode(GT_SUB_HI, TYP_INT, hiSrc1, hiSrc2);
            BlockRange().InsertBefore(cmp, loCmp, hiCmp);
            ContainCheckCompare(loCmp->AsOp());
            ContainCheckBinary(hiCmp->AsOp());

            //
            // Try to move the first SUB_HI operands right in front of it, this allows using
            // a single temporary register instead of 2 (one for CMP and one for SUB_HI). Do
            // this only for locals as they won't change condition flags. Note that we could
            // move constants (except 0 which generates XOR reg, reg) but it's extremly rare
            // to have a constant as the first operand.
            //

            if (hiSrc1->OperIs(GT_LCL_VAR, GT_LCL_FLD))
            {
                BlockRange().Remove(hiSrc1);
                BlockRange().InsertBefore(hiCmp, hiSrc1);
            }
        }
    }

    hiCmp->gtFlags |= GTF_SET_FLAGS;
    if (hiCmp->IsValue())
    {
        hiCmp->SetUnusedValue();
    }

    LIR::Use cmpUse;
    if (BlockRange().TryGetUse(cmp, &cmpUse) && cmpUse.User()->OperIs(GT_JTRUE))
    {
        BlockRange().Remove(cmp);

        GenTree* jcc    = cmpUse.User();
        jcc->gtOp.gtOp1 = nullptr;
        jcc->ChangeOper(GT_JCC);
        jcc->gtFlags |= (cmp->gtFlags & GTF_UNSIGNED) | GTF_USE_FLAGS;
        jcc->AsCC()->gtCondition = condition;
    }
    else
    {
        cmp->gtOp.gtOp1 = nullptr;
        cmp->gtOp.gtOp2 = nullptr;
        cmp->ChangeOper(GT_SETCC);
        cmp->gtFlags |= GTF_USE_FLAGS;
        cmp->AsCC()->gtCondition = condition;
    }

    return cmp->gtNext;
}
#endif // !_TARGET_64BIT_

//------------------------------------------------------------------------
// Lowering::OptimizeConstCompare: Performs various "compare with const" optimizations.
//
// Arguments:
//    cmp - the compare node
//
// Return Value:
//    The original compare node if lowering should proceed as usual or the next node
//    to lower if the compare node was changed in such a way that lowering is no
//    longer needed.
//
// Notes:
//    - Narrow operands to enable memory operand containment (XARCH specific).
//    - Transform cmp(and(x, y), 0) into test(x, y) (XARCH/Arm64 specific but could
//      be used for ARM as well if support for GT_TEST_EQ/GT_TEST_NE is added).
//    - Transform TEST(x, LSH(1, y)) into BT(x, y) (XARCH specific)
//    - Transform RELOP(OP, 0) into SETCC(OP) or JCC(OP) if OP can set the
//      condition flags appropriately (XARCH/ARM64 specific but could be extended
//      to ARM32 as well if ARM32 codegen supports GTF_SET_FLAGS).
//
GenTree* Lowering::OptimizeConstCompare(GenTree* cmp)
{
    assert(cmp->gtGetOp2()->IsIntegralConst());

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
    GenTree*       op1      = cmp->gtGetOp1();
    var_types      op1Type  = op1->TypeGet();
    GenTreeIntCon* op2      = cmp->gtGetOp2()->AsIntCon();
    ssize_t        op2Value = op2->IconValue();

#ifdef _TARGET_XARCH_
    if (IsContainableMemoryOp(op1) && varTypeIsSmall(op1Type) && genSmallTypeCanRepresentValue(op1Type, op2Value))
    {
        //
        // If op1's type is small then try to narrow op2 so it has the same type as op1.
        // Small types are usually used by memory loads and if both compare operands have
        // the same type then the memory load can be contained. In certain situations
        // (e.g "cmp ubyte, 200") we also get a smaller instruction encoding.
        //

        op2->gtType = op1Type;
    }
    else
#endif
        if (op1->OperIs(GT_CAST) && !op1->gtOverflow())
    {
        GenTreeCast* cast       = op1->AsCast();
        var_types    castToType = cast->CastToType();
        GenTree*     castOp     = cast->gtGetOp1();

        if (((castToType == TYP_BOOL) || (castToType == TYP_UBYTE)) && FitsIn<UINT8>(op2Value))
        {
            //
            // Since we're going to remove the cast we need to be able to narrow the cast operand
            // to the cast type. This can be done safely only for certain opers (e.g AND, OR, XOR).
            // Some opers just can't be narrowed (e.g DIV, MUL) while other could be narrowed but
            // doing so would produce incorrect results (e.g. RSZ, RSH).
            //
            // The below list of handled opers is conservative but enough to handle the most common
            // situations. In particular this include CALL, sometimes the JIT unnecessarilly widens
            // the result of bool returning calls.
            //
            bool removeCast =
#ifdef _TARGET_ARM64_
                (op2Value == 0) && cmp->OperIs(GT_EQ, GT_NE, GT_GT) &&
#endif
                (castOp->OperIs(GT_CALL, GT_LCL_VAR) || castOp->OperIsLogical()
#ifdef _TARGET_XARCH_
                 || IsContainableMemoryOp(castOp)
#endif
                     );

            if (removeCast)
            {
                assert(!castOp->gtOverflowEx()); // Must not be an overflow checking operation

#ifdef _TARGET_ARM64_
                bool cmpEq = cmp->OperIs(GT_EQ);

                cmp->SetOperRaw(cmpEq ? GT_TEST_EQ : GT_TEST_NE);
                op2->SetIconValue(0xff);
                op2->gtType = castOp->gtType;
#else
                castOp->gtType = castToType;
                op2->gtType    = castToType;
#endif
                // If we have any contained memory ops on castOp, they must now not be contained.
                if (castOp->OperIsLogical())
                {
                    GenTree* op1 = castOp->gtGetOp1();
                    if ((op1 != nullptr) && !op1->IsCnsIntOrI())
                    {
                        op1->ClearContained();
                    }
                    GenTree* op2 = castOp->gtGetOp2();
                    if ((op2 != nullptr) && !op2->IsCnsIntOrI())
                    {
                        op2->ClearContained();
                    }
                }
                cmp->gtOp.gtOp1 = castOp;

                BlockRange().Remove(cast);
            }
        }
    }
    else if (op1->OperIs(GT_AND) && cmp->OperIs(GT_EQ, GT_NE))
    {
        //
        // Transform ((x AND y) EQ|NE 0) into (x TEST_EQ|TEST_NE y) when possible.
        //

        GenTree* andOp1 = op1->gtGetOp1();
        GenTree* andOp2 = op1->gtGetOp2();

        if (op2Value != 0)
        {
            //
            // If we don't have a 0 compare we can get one by transforming ((x AND mask) EQ|NE mask)
            // into ((x AND mask) NE|EQ 0) when mask is a single bit.
            //

            if (isPow2(static_cast<size_t>(op2Value)) && andOp2->IsIntegralConst(op2Value))
            {
                op2Value = 0;
                op2->SetIconValue(0);
                cmp->SetOperRaw(GenTree::ReverseRelop(cmp->OperGet()));
            }
        }

        if (op2Value == 0)
        {
            BlockRange().Remove(op1);
            BlockRange().Remove(op2);

            cmp->SetOperRaw(cmp->OperIs(GT_EQ) ? GT_TEST_EQ : GT_TEST_NE);
            cmp->gtOp.gtOp1 = andOp1;
            cmp->gtOp.gtOp2 = andOp2;
            // We will re-evaluate containment below
            andOp1->ClearContained();
            andOp2->ClearContained();

#ifdef _TARGET_XARCH_
            if (IsContainableMemoryOp(andOp1) && andOp2->IsIntegralConst())
            {
                //
                // For "test" we only care about the bits that are set in the second operand (mask).
                // If the mask fits in a small type then we can narrow both operands to generate a "test"
                // instruction with a smaller encoding ("test" does not have a r/m32, imm8 form) and avoid
                // a widening load in some cases.
                //
                // For 16 bit operands we narrow only if the memory operand is already 16 bit. This matches
                // the behavior of a previous implementation and avoids adding more cases where we generate
                // 16 bit instructions that require a length changing prefix (0x66). These suffer from
                // significant decoder stalls on Intel CPUs.
                //
                // We could also do this for 64 bit masks that fit into 32 bit but it doesn't help.
                // In such cases morph narrows down the existing GT_AND by inserting a cast between it and
                // the memory operand so we'd need to add more code to recognize and eliminate that cast.
                //

                size_t mask = static_cast<size_t>(andOp2->AsIntCon()->IconValue());

                if (FitsIn<UINT8>(mask))
                {
                    andOp1->gtType = TYP_UBYTE;
                    andOp2->gtType = TYP_UBYTE;
                }
                else if (FitsIn<UINT16>(mask) && genTypeSize(andOp1) == 2)
                {
                    andOp1->gtType = TYP_USHORT;
                    andOp2->gtType = TYP_USHORT;
                }
            }
#endif
        }
    }

    if (cmp->OperIs(GT_TEST_EQ, GT_TEST_NE))
    {
#ifdef _TARGET_XARCH_
        //
        // Transform TEST_EQ|NE(x, LSH(1, y)) into BT(x, y) when possible. Using BT
        // results in smaller and faster code. It also doesn't have special register
        // requirements, unlike LSH that requires the shift count to be in ECX.
        // Note that BT has the same behavior as LSH when the bit index exceeds the
        // operand bit size - it uses (bit_index MOD bit_size).
        //

        GenTree* lsh = cmp->gtGetOp2();
        LIR::Use cmpUse;

        if (lsh->OperIs(GT_LSH) && varTypeIsIntOrI(lsh->TypeGet()) && lsh->gtGetOp1()->IsIntegralConst(1) &&
            BlockRange().TryGetUse(cmp, &cmpUse))
        {
            genTreeOps condition = cmp->OperIs(GT_TEST_NE) ? GT_LT : GT_GE;

            cmp->SetOper(GT_BT);
            cmp->gtType = TYP_VOID;
            cmp->gtFlags |= GTF_SET_FLAGS;
            cmp->gtOp.gtOp2 = lsh->gtGetOp2();
            cmp->gtGetOp2()->ClearContained();

            BlockRange().Remove(lsh->gtGetOp1());
            BlockRange().Remove(lsh);

            GenTreeCC* cc;

            if (cmpUse.User()->OperIs(GT_JTRUE))
            {
                cmpUse.User()->ChangeOper(GT_JCC);
                cc              = cmpUse.User()->AsCC();
                cc->gtCondition = condition;
            }
            else
            {
                cc = new (comp, GT_SETCC) GenTreeCC(GT_SETCC, condition, TYP_INT);
                BlockRange().InsertAfter(cmp, cc);
                cmpUse.ReplaceWith(comp, cc);
            }

            cc->gtFlags |= GTF_USE_FLAGS | GTF_UNSIGNED;

            return cmp->gtNext;
        }
#endif // _TARGET_XARCH_
    }
    else if (cmp->OperIs(GT_EQ, GT_NE))
    {
        GenTree* op1 = cmp->gtGetOp1();
        GenTree* op2 = cmp->gtGetOp2();

        // TODO-CQ: right now the below peep is inexpensive and gets the benefit in most
        // cases because in majority of cases op1, op2 and cmp would be in that order in
        // execution. In general we should be able to check that all the nodes that come
        // after op1 do not modify the flags so that it is safe to avoid generating a
        // test instruction.

        if (op2->IsIntegralConst(0) && (op1->gtNext == op2) && (op2->gtNext == cmp) &&
#ifdef _TARGET_XARCH_
            op1->OperIs(GT_AND, GT_OR, GT_XOR, GT_ADD, GT_SUB, GT_NEG))
#else // _TARGET_ARM64_
            op1->OperIs(GT_AND, GT_ADD, GT_SUB))
#endif
        {
            op1->gtFlags |= GTF_SET_FLAGS;
            op1->SetUnusedValue();

            BlockRange().Remove(op2);

            GenTree*   next = cmp->gtNext;
            GenTree*   cc;
            genTreeOps ccOp;
            LIR::Use   cmpUse;

            // Fast check for the common case - relop used by a JTRUE that immediately follows it.
            if ((next != nullptr) && next->OperIs(GT_JTRUE) && (next->gtGetOp1() == cmp))
            {
                cc   = next;
                ccOp = GT_JCC;
                next = nullptr;
                BlockRange().Remove(cmp);
            }
            else if (BlockRange().TryGetUse(cmp, &cmpUse) && cmpUse.User()->OperIs(GT_JTRUE))
            {
                cc   = cmpUse.User();
                ccOp = GT_JCC;
                next = nullptr;
                BlockRange().Remove(cmp);
            }
            else // The relop is not used by a JTRUE or it is not used at all.
            {
                // Transform the relop node it into a SETCC. If it's not used we could remove
                // it completely but that means doing more work to handle a rare case.
                cc   = cmp;
                ccOp = GT_SETCC;
            }

            genTreeOps condition = cmp->OperGet();
            cc->ChangeOper(ccOp);
            cc->AsCC()->gtCondition = condition;
            cc->gtFlags |= GTF_USE_FLAGS | (cmp->gtFlags & GTF_UNSIGNED);

            return next;
        }
    }
#endif // defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)

    return cmp;
}

//------------------------------------------------------------------------
// Lowering::LowerCompare: Lowers a compare node.
//
// Arguments:
//    cmp - the compare node
//
// Return Value:
//    The next node to lower.
//
GenTree* Lowering::LowerCompare(GenTree* cmp)
{
#ifndef _TARGET_64BIT_
    if (cmp->gtGetOp1()->TypeGet() == TYP_LONG)
    {
        return DecomposeLongCompare(cmp);
    }
#endif

    if (cmp->gtGetOp2()->IsIntegralConst() && !comp->opts.MinOpts())
    {
        GenTree* next = OptimizeConstCompare(cmp);

        // If OptimizeConstCompare return the compare node as "next" then we need to continue lowering.
        if (next != cmp)
        {
            return next;
        }
    }

#ifdef _TARGET_XARCH_
    if (cmp->gtGetOp1()->TypeGet() == cmp->gtGetOp2()->TypeGet())
    {
        if (varTypeIsSmall(cmp->gtGetOp1()->TypeGet()) && varTypeIsUnsigned(cmp->gtGetOp1()->TypeGet()))
        {
            //
            // If both operands have the same type then codegen will use the common operand type to
            // determine the instruction type. For small types this would result in performing a
            // signed comparison of two small unsigned values without zero extending them to TYP_INT
            // which is incorrect. Note that making the comparison unsigned doesn't imply that codegen
            // has to generate a small comparison, it can still correctly generate a TYP_INT comparison.
            //

            cmp->gtFlags |= GTF_UNSIGNED;
        }
    }
#endif // _TARGET_XARCH_
    ContainCheckCompare(cmp->AsOp());
    return cmp->gtNext;
}

//------------------------------------------------------------------------
// Lowering::LowerJTrue: Lowers a JTRUE node.
//
// Arguments:
//    jtrue - the JTRUE node
//
// Return Value:
//    The next node to lower (usually nullptr).
//
// Notes:
//    On ARM64 this may remove the JTRUE node and transform its associated
//    relop into a JCMP node.
//
GenTree* Lowering::LowerJTrue(GenTreeOp* jtrue)
{
#ifdef _TARGET_ARM64_
    GenTree* relop    = jtrue->gtGetOp1();
    GenTree* relopOp2 = relop->gtOp.gtGetOp2();

    if ((relop->gtNext == jtrue) && relopOp2->IsCnsIntOrI())
    {
        bool     useJCMP = false;
        unsigned flags   = 0;

        if (relop->OperIs(GT_EQ, GT_NE) && relopOp2->IsIntegralConst(0))
        {
            // Codegen will use cbz or cbnz in codegen which do not affect the flag register
            flags   = relop->OperIs(GT_EQ) ? GTF_JCMP_EQ : 0;
            useJCMP = true;
        }
        else if (relop->OperIs(GT_TEST_EQ, GT_TEST_NE) && isPow2(relopOp2->AsIntCon()->IconValue()))
        {
            // Codegen will use tbz or tbnz in codegen which do not affect the flag register
            flags   = GTF_JCMP_TST | (relop->OperIs(GT_TEST_EQ) ? GTF_JCMP_EQ : 0);
            useJCMP = true;
        }

        if (useJCMP)
        {
            relop->SetOper(GT_JCMP);
            relop->gtFlags &= ~(GTF_JCMP_TST | GTF_JCMP_EQ);
            relop->gtFlags |= flags;
            relop->gtType = TYP_VOID;

            relopOp2->SetContained();

            BlockRange().Remove(jtrue);

            assert(relop->gtNext == nullptr);
            return nullptr;
        }
    }
#endif // _TARGET_ARM64_

    ContainCheckJTrue(jtrue);

    assert(jtrue->gtNext == nullptr);
    return nullptr;
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

#if defined(_TARGET_AMD64_) && defined(FEATURE_SIMD)
    GenTreeUnOp* const unOp = ret->AsUnOp();
    if ((unOp->TypeGet() == TYP_LONG) && (unOp->gtOp1->TypeGet() == TYP_SIMD8))
    {
        GenTreeUnOp* bitcast = new (comp, GT_BITCAST) GenTreeOp(GT_BITCAST, TYP_LONG, unOp->gtOp1, nullptr);
        unOp->gtOp1          = bitcast;
        BlockRange().InsertBefore(unOp, bitcast);
    }
#endif // _TARGET_AMD64_

    // Method doing PInvokes has exactly one return block unless it has tail calls.
    if (comp->info.compCallUnmanaged && (comp->compCurBB == comp->genReturnBB))
    {
        InsertPInvokeMethodEpilog(comp->compCurBB DEBUGARG(ret));
    }
    ContainCheckRet(ret->AsOp());
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
            assert(pAddr == nullptr);
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
#if defined(_TARGET_ARMARCH_)
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

        fgArgTabEntry* thisArgTabEntry = comp->gtArgEntryByArgNum(call, argNum);
        thisArgNode                    = thisArgTabEntry->node;
    }
    else
    {
        thisArgNode = comp->gtGetThisArg(call);
    }

    assert(thisArgNode->gtOper == GT_PUTARG_REG);
    GenTree* originalThisExpr = thisArgNode->gtOp.gtOp1;
    GenTree* thisExpr         = originalThisExpr;

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
        ReplaceWithLclVar(thisExprUse, delegateInvokeTmp);

        thisExpr = thisExprUse.Def(); // it's changed; reload it.
        lclNum   = delegateInvokeTmp;
    }

    // replace original expression feeding into thisPtr with
    // [originalThis + offsetOfDelegateInstance]

    GenTree* newThisAddr = new (comp, GT_LEA)
        GenTreeAddrMode(TYP_BYREF, thisExpr, nullptr, 0, comp->eeGetEEInfo()->offsetOfDelegateInstance);

    GenTree* newThis = comp->gtNewOperNode(GT_IND, TYP_REF, newThisAddr);

    BlockRange().InsertAfter(thisExpr, newThisAddr, newThis);

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

    GenTree* stateNode    = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_BYTE, state);
    GenTree* addr         = new (comp, GT_LEA) GenTreeAddrMode(TYP_I_IMPL, base, nullptr, 1, pInfo->offsetOfGCState);
    GenTree* storeGcState = new (comp, GT_STOREIND) GenTreeStoreInd(TYP_BYTE, addr, stateNode);
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

    // The first BB must be a scratch BB in order for us to be able to safely insert the P/Invoke prolog.
    assert(comp->fgFirstBBisScratch());

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

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
    GenTreeArgList* argList = comp->gtNewArgList(frameAddr);
#else
    GenTreeArgList*    argList = comp->gtNewArgList(frameAddr, PhysReg(REG_SECRET_STUB_PARAM));
#endif

    GenTree* call = comp->gtNewHelperCallNode(CORINFO_HELP_INIT_PINVOKE_FRAME, TYP_I_IMPL, argList);

    // some sanity checks on the frame list root vardsc
    LclVarDsc* varDsc = &comp->lvaTable[comp->info.compLvFrameListRoot];
    noway_assert(!varDsc->lvIsParam);
    noway_assert(varDsc->lvType == TYP_I_IMPL);

    GenTree* store =
        new (comp, GT_STORE_LCL_VAR) GenTreeLclVar(GT_STORE_LCL_VAR, TYP_I_IMPL, comp->info.compLvFrameListRoot,
                                                   (IL_OFFSET)-1); // cast to resolve ambiguity.
    store->gtOp.gtOp1 = call;
    store->gtFlags |= GTF_VAR_DEF;

    GenTree* const insertionPoint = firstBlockRange.FirstNonPhiOrCatchArgNode();

    comp->fgMorphTree(store);
    firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, store));
    DISPTREERANGE(firstBlockRange, store);

#if !defined(_TARGET_X86_) && !defined(_TARGET_ARM_)
    // For x86, this step is done at the call site (due to stack pointer not being static in the function).
    // For arm32, CallSiteSP is set up by the call to CORINFO_HELP_INIT_PINVOKE_FRAME.

    // --------------------------------------------------------
    // InlinedCallFrame.m_pCallSiteSP = @RSP;

    GenTreeLclFld* storeSP = new (comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfCallSiteSP);
    storeSP->gtOp1 = PhysReg(REG_SPBASE);
    storeSP->gtFlags |= GTF_VAR_DEF;

    firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, storeSP));
    DISPTREERANGE(firstBlockRange, storeSP);

#endif // !defined(_TARGET_X86_) && !defined(_TARGET_ARM_)

#if !defined(_TARGET_ARM_)
    // For arm32, CalleeSavedFP is set up by the call to CORINFO_HELP_INIT_PINVOKE_FRAME.

    // --------------------------------------------------------
    // InlinedCallFrame.m_pCalleeSavedEBP = @RBP;

    GenTreeLclFld* storeFP =
        new (comp, GT_STORE_LCL_FLD) GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar,
                                                   callFrameInfo.offsetOfCalleeSavedFP);
    storeFP->gtOp1 = PhysReg(REG_FPBASE);
    storeFP->gtFlags |= GTF_VAR_DEF;

    firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, storeFP));
    DISPTREERANGE(firstBlockRange, storeFP);
#endif // !defined(_TARGET_ARM_)

    // --------------------------------------------------------
    // On 32-bit targets, CORINFO_HELP_INIT_PINVOKE_FRAME initializes the PInvoke frame and then pushes it onto
    // the current thread's Frame stack. On 64-bit targets, it only initializes the PInvoke frame.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_
    if (comp->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB))
    {
        // Push a frame - if we are NOT in an IL stub, this is done right before the call
        // The init routine sets InlinedCallFrame's m_pNext, so we just set the thead's top-of-stack
        GenTree* frameUpd = CreateFrameLinkUpdate(PushFrame);
        firstBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, frameUpd));
        ContainCheckStoreIndir(frameUpd->AsIndir());
        DISPTREERANGE(firstBlockRange, frameUpd);
    }
#endif // _TARGET_64BIT_
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
void Lowering::InsertPInvokeMethodEpilog(BasicBlock* returnBB DEBUGARG(GenTree* lastExpr))
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
    ContainCheckStoreIndir(storeGCState->AsIndir());

    // Pop the frame if necessary. This always happens in the epilog on 32-bit targets. For 64-bit targets, we only do
    // this in the epilog for IL stubs; for non-IL stubs the frame is popped after every PInvoke call.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_
    if (comp->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB))
#endif // _TARGET_64BIT_
    {
        GenTree* frameUpd = CreateFrameLinkUpdate(PopFrame);
        returnBlockRange.InsertBefore(insertionPoint, LIR::SeqTree(comp, frameUpd));
        ContainCheckStoreIndir(frameUpd->AsIndir());
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

    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        // First argument is the address of the frame variable.
        GenTree* frameAddr = new (comp, GT_LCL_VAR_ADDR)
            GenTreeLclVar(GT_LCL_VAR_ADDR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, BAD_IL_OFFSET);

        // Insert call to CORINFO_HELP_JIT_PINVOKE_BEGIN
        GenTree* helperCall =
            comp->gtNewHelperCallNode(CORINFO_HELP_JIT_PINVOKE_BEGIN, TYP_VOID, comp->gtNewArgList(frameAddr));

        comp->fgMorphTree(helperCall);
        BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, helperCall));
        LowerNode(helperCall); // helper call is inserted before current node and should be lowered here.
        return;
    }

    // Emit the following sequence:
    //
    // InlinedCallFrame.callTarget = methodHandle   // stored in m_Datum
    // InlinedCallFrame.m_pCallSiteSP = SP          // x86 only
    // InlinedCallFrame.m_pCallerReturnAddress = return address
    // Thread.gcState = 0
    // (non-stub) - update top Frame on TCB         // 64-bit targets only

    // ----------------------------------------------------------------------------------
    // Setup InlinedCallFrame.callSiteTarget (which is how the JIT refers to it).
    // The actual field is InlinedCallFrame.m_Datum which has many different uses and meanings.

    GenTree* src = nullptr;

    if (callType == CT_INDIRECT)
    {
#if !defined(_TARGET_64BIT_)
        // On 32-bit targets, indirect calls need the size of the stack args in InlinedCallFrame.m_Datum.
        const unsigned numStkArgBytes = call->fgArgInfo->GetNextSlotNum() * TARGET_POINTER_SIZE;

        src = comp->gtNewIconNode(numStkArgBytes, TYP_INT);
#else
        // On 64-bit targets, indirect calls may need the stub parameter value in InlinedCallFrame.m_Datum.
        // If the stub parameter value is not needed, m_Datum will be initialized by the VM.
        if (comp->info.compPublishStubParam)
        {
            src = comp->gtNewLclvNode(comp->lvaStubArgumentVar, TYP_I_IMPL);
        }
#endif // !defined(_TARGET_64BIT_)
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
        store->gtFlags |= GTF_VAR_DEF;

        InsertTreeBeforeAndContainCheck(insertBefore, store);
    }

#ifdef _TARGET_X86_

    // ----------------------------------------------------------------------------------
    // InlinedCallFrame.m_pCallSiteSP = SP

    GenTreeLclFld* storeCallSiteSP = new (comp, GT_STORE_LCL_FLD)
        GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar, callFrameInfo.offsetOfCallSiteSP);

    storeCallSiteSP->gtOp1 = PhysReg(REG_SPBASE);
    storeCallSiteSP->gtFlags |= GTF_VAR_DEF;

    InsertTreeBeforeAndContainCheck(insertBefore, storeCallSiteSP);

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
    storeLab->gtFlags |= GTF_VAR_DEF;

    InsertTreeBeforeAndContainCheck(insertBefore, storeLab);

    // Push the PInvoke frame if necessary. On 32-bit targets this only happens in the method prolog if a method
    // contains PInvokes; on 64-bit targets this is necessary in non-stubs.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_
    if (!comp->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB))
    {
        // Set the TCB's frame to be the one we just created.
        // Note the init routine for the InlinedCallFrame (CORINFO_HELP_INIT_PINVOKE_FRAME)
        // has prepended it to the linked list to maintain the stack of Frames.
        //
        // Stubs do this once per stub, not once per call.
        GenTree* frameUpd = CreateFrameLinkUpdate(PushFrame);
        BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, frameUpd));
        ContainCheckStoreIndir(frameUpd->AsIndir());
    }
#endif // _TARGET_64BIT_

    // IMPORTANT **** This instruction must come last!!! ****
    // It changes the thread's state to Preemptive mode
    // ----------------------------------------------------------------------------------
    //  [tcb + offsetOfGcState] = 0

    GenTree* storeGCState = SetGCState(0);
    BlockRange().InsertBefore(insertBefore, LIR::SeqTree(comp, storeGCState));
    ContainCheckStoreIndir(storeGCState->AsIndir());
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

    if (comp->opts.ShouldUsePInvokeHelpers())
    {
        noway_assert(comp->lvaInlinedPInvokeFrameVar != BAD_VAR_NUM);

        // First argument is the address of the frame variable.
        GenTree* frameAddr =
            new (comp, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, TYP_BYREF, comp->lvaInlinedPInvokeFrameVar, BAD_IL_OFFSET);
        frameAddr->SetOperRaw(GT_LCL_VAR_ADDR);

        // Insert call to CORINFO_HELP_JIT_PINVOKE_END
        GenTreeCall* helperCall =
            comp->gtNewHelperCallNode(CORINFO_HELP_JIT_PINVOKE_END, TYP_VOID, comp->gtNewArgList(frameAddr));

        comp->fgMorphTree(helperCall);
        BlockRange().InsertAfter(call, LIR::SeqTree(comp, helperCall));
        ContainCheckCallOperands(helperCall);
        return;
    }

    // gcstate = 1
    GenTree* insertionPoint = call->gtNext;

    GenTree* tree = SetGCState(1);
    BlockRange().InsertBefore(insertionPoint, LIR::SeqTree(comp, tree));
    ContainCheckStoreIndir(tree->AsIndir());

    tree = CreateReturnTrapSeq();
    BlockRange().InsertBefore(insertionPoint, LIR::SeqTree(comp, tree));
    ContainCheckReturnTrap(tree->AsOp());

    // Pop the frame if necessary. On 32-bit targets this only happens in the method epilog; on 64-bit targets thi
    // happens after every PInvoke call in non-stubs. 32-bit targets instead mark the frame as inactive.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_64BIT_
    if (!comp->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB))
    {
        tree = CreateFrameLinkUpdate(PopFrame);
        BlockRange().InsertBefore(insertionPoint, LIR::SeqTree(comp, tree));
        ContainCheckStoreIndir(tree->AsIndir());
    }
#else
    const CORINFO_EE_INFO::InlinedCallFrameInfo& callFrameInfo = comp->eeGetEEInfo()->inlinedCallFrameInfo;

    // ----------------------------------------------------------------------------------
    // InlinedCallFrame.m_pCallerReturnAddress = nullptr

    GenTreeLclFld* const storeCallSiteTracker =
        new (comp, GT_STORE_LCL_FLD) GenTreeLclFld(GT_STORE_LCL_FLD, TYP_I_IMPL, comp->lvaInlinedPInvokeFrameVar,
                                                   callFrameInfo.offsetOfReturnAddress);

    GenTreeIntCon* const constantZero = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);

    storeCallSiteTracker->gtOp1 = constantZero;
    storeCallSiteTracker->gtFlags |= GTF_VAR_DEF;

    BlockRange().InsertBefore(insertionPoint, constantZero, storeCallSiteTracker);
    ContainCheckStoreLoc(storeCallSiteTracker);
#endif // _TARGET_64BIT_
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
    // GC transitions are generated inline; if CORJIT_FLAG_USE_PINVOKE_HELPERS is specified,
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
        comp->info.compCompHnd->getAddressOfPInvokeTarget(methHnd, &lookup);

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
        ReplaceWithLclVar(thisPtrUse, vtableCallTemp);

        lclNum = vtableCallTemp;
    }

    // We'll introduce another use of this local so increase its ref count.
    comp->lvaTable[lclNum].incRefCnts(comp->compCurBB->getBBWeight(comp), comp);

    // Get hold of the vtable offset (note: this might be expensive)
    unsigned vtabOffsOfIndirection;
    unsigned vtabOffsAfterIndirection;
    bool     isRelative;
    comp->info.compCompHnd->getMethodVTableOffset(call->gtCallMethHnd, &vtabOffsOfIndirection,
                                                  &vtabOffsAfterIndirection, &isRelative);

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

    // Get the appropriate vtable chunk
    if (vtabOffsOfIndirection != CORINFO_VIRTUALCALL_NO_CHUNK)
    {
        if (isRelative)
        {
            // MethodTable offset is a relative pointer.
            //
            // Additional temporary variable is used to store virtual table pointer.
            // Address of method is obtained by the next computations:
            //
            // Save relative offset to tmp (vtab is virtual table pointer, vtabOffsOfIndirection is offset of
            // vtable-1st-level-indirection):
            // tmp = [vtab + vtabOffsOfIndirection]
            //
            // Save address of method to result (vtabOffsAfterIndirection is offset of vtable-2nd-level-indirection):
            // result = [vtab + vtabOffsOfIndirection + vtabOffsAfterIndirection + tmp]
            unsigned lclNumTmp = comp->lvaGrabTemp(true DEBUGARG("lclNumTmp"));

            comp->lvaTable[lclNumTmp].incRefCnts(comp->compCurBB->getBBWeight(comp), comp);
            GenTree* lclvNodeStore = comp->gtNewTempAssign(lclNumTmp, result);

            LIR::Range range = LIR::SeqTree(comp, lclvNodeStore);
            JITDUMP("result of obtaining pointer to virtual table:\n");
            DISPRANGE(range);
            BlockRange().InsertBefore(call, std::move(range));

            GenTree* tmpTree = comp->gtNewLclvNode(lclNumTmp, result->TypeGet());
            tmpTree          = Offset(tmpTree, vtabOffsOfIndirection);

            tmpTree       = comp->gtNewOperNode(GT_IND, TYP_I_IMPL, tmpTree, false);
            GenTree* offs = comp->gtNewIconNode(vtabOffsOfIndirection + vtabOffsAfterIndirection, TYP_INT);
            result = comp->gtNewOperNode(GT_ADD, TYP_I_IMPL, comp->gtNewLclvNode(lclNumTmp, result->TypeGet()), offs);

            result = Ind(OffsetByIndex(result, tmpTree));
        }
        else
        {
            // result = [REG_CALL_IND_SCRATCH + vtabOffsOfIndirection]
            result = Ind(Offset(result, vtabOffsOfIndirection));
        }
    }
    else
    {
        assert(!isRelative);
    }

    // Load the function address
    // result = [reg+vtabOffs]
    if (!isRelative)
    {
        result = Ind(Offset(result, vtabOffsAfterIndirection));
    }

    return result;
}

// Lower stub dispatched virtual calls.
GenTree* Lowering::LowerVirtualStubCall(GenTreeCall* call)
{
    assert(call->IsVirtualStub());

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

    // This is code to set up an indirect call to a stub address computed
    // via dictionary lookup.
    if (call->gtCallType == CT_INDIRECT)
    {
        // The importer decided we needed a stub call via a computed
        // stub dispatch address, i.e. an address which came from a dictionary lookup.
        //   - The dictionary lookup produces an indirected address, suitable for call
        //     via "call [VirtualStubParam.reg]"
        //
        // This combination will only be generated for shared generic code and when
        // stub dispatch is active.

        // fgMorphArgs will have created trees to pass the address in VirtualStubParam.reg.
        // All we have to do here is add an indirection to generate the actual call target.

        GenTree* ind;

#ifdef _TARGET_ARM_
        // For ARM, fgMorphTailCall has already made gtCallAddr a GT_IND for virtual stub tail calls.
        // (When we eliminate LEGACY_BACKEND maybe we can eliminate this asymmetry?)
        if (call->IsTailCallViaHelper())
        {
            ind = call->gtCallAddr;
            assert(ind->gtOper == GT_IND);
        }
        else
#endif // _TARGET_ARM_
        {
            ind = Ind(call->gtCallAddr);
            BlockRange().InsertAfter(call->gtCallAddr, ind);
            call->gtCallAddr = ind;
        }

        ind->gtFlags |= GTF_IND_REQ_ADDR_IN_REG;

        ContainCheckIndir(ind->AsIndir());
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
            addr->gtRegNum = comp->virtualStubParamInfo->GetReg();

// On ARM we must use a proper address in R12(thunk register) without dereferencing.
// So for the jump we use the default register.
// TODO: specifying register probably unnecessary for other platforms, too.
#if !defined(_TARGET_UNIX_) && !defined(_TARGET_ARM_) && !defined(_TARGET_ARM64_)
            indir->gtRegNum = REG_JUMP_THUNK_PARAM;
#elif defined(_TARGET_ARM64_)
            // Prevent indir->gtRegNum from colliding with addr->gtRegNum
            indir->gtRegNum = REG_JUMP_THUNK_PARAM;

            // Sanity checks
            assert(addr->gtRegNum != indir->gtRegNum); // indir and addr registers must be different
            static_assert_no_msg((RBM_JUMP_THUNK_PARAM & RBM_ARG_REGS) == 0);
            static_assert_no_msg((RBM_JUMP_THUNK_PARAM & RBM_INT_CALLEE_TRASH) != 0);

#elif defined(_TARGET_ARM_)
            // TODO-ARM-Cleanup: This is a temporarey hotfix to fix a regression observed in Linux/ARM.
            if (!comp->IsTargetAbi(CORINFO_CORERT_ABI))
                indir->gtRegNum = REG_JUMP_THUNK_PARAM;
#endif
            indir->gtFlags |= GTF_IND_REQ_ADDR_IN_REG;
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
    node->VisitOperands([this, addrMode](GenTree* operand) -> GenTree::VisitResult {
        AddrModeCleanupHelper(addrMode, operand);
        return GenTree::VisitResult::Continue;
    });

    BlockRange().Remove(node);
}

//------------------------------------------------------------------------
// Lowering::AreSourcesPossibleModifiedLocals:
//    Given two nodes which will be used in an addressing mode (base,
//    index), check to see if they are lclVar reads, and if so, walk
//    backwards from the use until both reads have been visited to
//    determine if they are potentially modified in that range.
//
// Arguments:
//    addr - the node that uses the base and index nodes
//    base - the base node
//    index - the index node
//
// Returns: true if either the base or index may be modified between the
//          node and addr.
//
bool Lowering::AreSourcesPossiblyModifiedLocals(GenTree* addr, GenTree* base, GenTree* index)
{
    assert(addr != nullptr);

    unsigned markCount = 0;

    SideEffectSet baseSideEffects;
    if (base != nullptr)
    {
        if (base->OperIsLocalRead())
        {
            baseSideEffects.AddNode(comp, base);
        }
        else
        {
            base = nullptr;
        }
    }

    SideEffectSet indexSideEffects;
    if (index != nullptr)
    {
        if (index->OperIsLocalRead())
        {
            indexSideEffects.AddNode(comp, index);
        }
        else
        {
            index = nullptr;
        }
    }

    for (GenTree* cursor = addr;; cursor = cursor->gtPrev)
    {
        assert(cursor != nullptr);

        if (cursor == base)
        {
            base = nullptr;
        }

        if (cursor == index)
        {
            index = nullptr;
        }

        if ((base == nullptr) && (index == nullptr))
        {
            return false;
        }

        m_scratchSideEffects.Clear();
        m_scratchSideEffects.AddNode(comp, cursor);
        if ((base != nullptr) && m_scratchSideEffects.InterferesWith(baseSideEffects, false))
        {
            return true;
        }

        if ((index != nullptr) && m_scratchSideEffects.InterferesWith(indexSideEffects, false))
        {
            return true;
        }
    }
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
    GenTree* addr   = use.Def();
    GenTree* base   = nullptr;
    GenTree* index  = nullptr;
    unsigned scale  = 0;
    unsigned offset = 0;
    bool     rev    = false;

    // TODO-1stClassStructs: This logic is here to preserve prior behavior. Note that previously
    // block ops were not considered for addressing modes, but an add under it may have been.
    // This should be replaced with logic that more carefully determines when an addressing mode
    // would be beneficial for a block op.
    if (isIndir)
    {
        GenTree* indir = use.User();
        if (indir->TypeGet() == TYP_STRUCT)
        {
            isIndir = false;
        }
        else if (varTypeIsStruct(indir))
        {
            // We can have an indirection on the rhs of a block copy (it is the source
            // object). This is not a "regular" indirection.
            // (Note that the user check could be costly.)
            LIR::Use indirUse;
            if (BlockRange().TryGetUse(indir, &indirUse) && indirUse.User()->OperIsIndir())
            {
                isIndir = false;
            }
            else
            {
                isIndir = !indir->OperIsBlk();
            }
        }
    }

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
    if (!doAddrMode || AreSourcesPossiblyModifiedLocals(addr, base, index))
    {
        JITDUMP("No addressing mode:\n  ");
        DISPNODE(addr);
        return addr;
    }

    GenTree* arrLength = nullptr;

    JITDUMP("Addressing mode:\n");
    JITDUMP("  Base\n    ");
    DISPNODE(base);
    if (index != nullptr)
    {
        JITDUMP("  + Index * %u + %u\n    ", scale, offset);
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

    // Neither the base nor the index should now be contained.
    if (base != nullptr)
    {
        base->ClearContained();
    }
    if (index != nullptr)
    {
        index->ClearContained();
    }
    addrMode->gtRsvdRegs = addr->gtRsvdRegs;
    addrMode->gtFlags |= (addr->gtFlags & GTF_IND_FLAGS);
    addrMode->gtFlags &= ~GTF_ALL_EFFECT; // LEAs are side-effect-free.

    JITDUMP("New addressing mode node:\n");
    DISPNODE(addrMode);
    JITDUMP("\n");

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
//    The next node to lower if we have transformed the ADD; nullptr otherwise.
//
GenTree* Lowering::LowerAdd(GenTree* node)
{
    GenTree* next = node->gtNext;

#ifndef _TARGET_ARMARCH_
    if (varTypeIsIntegralOrI(node))
    {
        LIR::Use use;
        if (BlockRange().TryGetUse(node, &use))
        {
            // If this is a child of an indir, let the parent handle it.
            // If there is a chain of adds, only look at the topmost one.
            GenTree* parent = use.User();
            if (!parent->OperIsIndir() && (parent->gtOper != GT_ADD))
            {
                GenTree* addr = TryCreateAddrMode(std::move(use), false);
                if (addr != node)
                {
                    return addr->gtNext;
                }
            }
        }
    }
#endif // !_TARGET_ARMARCH_

    return nullptr;
}

//------------------------------------------------------------------------
// LowerUnsignedDivOrMod: Lowers a GT_UDIV/GT_UMOD node.
//
// Arguments:
//    divMod - pointer to the GT_UDIV/GT_UMOD node to be lowered
//
// Return Value:
//    Returns a boolean indicating whether the node was transformed.
//
// Notes:
//    - Transform UDIV/UMOD by power of 2 into RSZ/AND
//    - Transform UDIV by constant >= 2^(N-1) into GE
//    - Transform UDIV/UMOD by constant >= 3 into "magic division"
//

bool Lowering::LowerUnsignedDivOrMod(GenTreeOp* divMod)
{
    assert(divMod->OperIs(GT_UDIV, GT_UMOD));

    GenTree* next     = divMod->gtNext;
    GenTree* dividend = divMod->gtGetOp1();
    GenTree* divisor  = divMod->gtGetOp2();

#if !defined(_TARGET_64BIT_)
    if (dividend->OperIs(GT_LONG))
    {
        return false;
    }
#endif

    if (!divisor->IsCnsIntOrI())
    {
        return false;
    }

    if (dividend->IsCnsIntOrI())
    {
        // We shouldn't see a divmod with constant operands here but if we do then it's likely
        // because optimizations are disabled or it's a case that's supposed to throw an exception.
        // Don't optimize this.
        return false;
    }

    const var_types type = divMod->TypeGet();
    assert((type == TYP_INT) || (type == TYP_I_IMPL));

    size_t divisorValue = static_cast<size_t>(divisor->AsIntCon()->IconValue());

    if (type == TYP_INT)
    {
        // Clear up the upper 32 bits of the value, they may be set to 1 because constants
        // are treated as signed and stored in ssize_t which is 64 bit in size on 64 bit targets.
        divisorValue &= UINT32_MAX;
    }

    if (divisorValue == 0)
    {
        return false;
    }

    const bool isDiv = divMod->OperIs(GT_UDIV);

    if (isPow2(divisorValue))
    {
        genTreeOps newOper;

        if (isDiv)
        {
            newOper      = GT_RSZ;
            divisorValue = genLog2(divisorValue);
        }
        else
        {
            newOper = GT_AND;
            divisorValue -= 1;
        }

        divMod->SetOper(newOper);
        divisor->gtIntCon.SetIconValue(divisorValue);
        ContainCheckNode(divMod);
        return true;
    }
    if (isDiv)
    {
        // If the divisor is greater or equal than 2^(N - 1) then the result is 1
        // iff the dividend is greater or equal than the divisor.
        if (((type == TYP_INT) && (divisorValue > (UINT32_MAX / 2))) ||
            ((type == TYP_LONG) && (divisorValue > (UINT64_MAX / 2))))
        {
            divMod->SetOper(GT_GE);
            divMod->gtFlags |= GTF_UNSIGNED;
            ContainCheckNode(divMod);
            return true;
        }
    }

// TODO-ARM-CQ: Currently there's no GT_MULHI for ARM32
#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
    if (!comp->opts.MinOpts() && (divisorValue >= 3))
    {
        size_t magic;
        bool   add;
        int    shift;

        if (type == TYP_INT)
        {
            magic = MagicDivide::GetUnsigned32Magic(static_cast<uint32_t>(divisorValue), &add, &shift);
        }
        else
        {
#ifdef _TARGET_64BIT_
            magic = MagicDivide::GetUnsigned64Magic(static_cast<uint64_t>(divisorValue), &add, &shift);
#else
            unreached();
#endif
        }

        // Depending on the "add" flag returned by GetUnsignedMagicNumberForDivide we need to generate:
        // add == false (when divisor == 3 for example):
        //     div = (dividend MULHI magic) RSZ shift
        // add == true (when divisor == 7 for example):
        //     mulhi = dividend MULHI magic
        //     div   = (((dividend SUB mulhi) RSZ 1) ADD mulhi)) RSZ (shift - 1)
        const bool     requiresAdjustment       = add;
        const bool     requiresDividendMultiuse = requiresAdjustment || !isDiv;
        const unsigned curBBWeight              = m_block->getBBWeight(comp);
        unsigned       dividendLclNum           = BAD_VAR_NUM;

        if (requiresDividendMultiuse)
        {
            LIR::Use dividendUse(BlockRange(), &divMod->gtOp1, divMod);
            dividendLclNum = ReplaceWithLclVar(dividendUse);
            dividend       = divMod->gtGetOp1();
        }

        // Insert a new GT_MULHI node before the existing GT_UDIV/GT_UMOD node.
        // The existing node will later be transformed into a GT_RSZ/GT_SUB that
        // computes the final result. This way don't need to find and change the use
        // of the existing node.
        GenTree* mulhi = comp->gtNewOperNode(GT_MULHI, type, dividend, divisor);
        mulhi->gtFlags |= GTF_UNSIGNED;
        divisor->AsIntCon()->SetIconValue(magic);
        BlockRange().InsertBefore(divMod, mulhi);
        GenTree* firstNode = mulhi;

        if (requiresAdjustment)
        {
            GenTree* dividend = comp->gtNewLclvNode(dividendLclNum, type);
            GenTree* sub      = comp->gtNewOperNode(GT_SUB, type, dividend, mulhi);
            BlockRange().InsertBefore(divMod, dividend, sub);
            comp->lvaTable[dividendLclNum].incRefCnts(curBBWeight, comp);

            GenTree* one = comp->gtNewIconNode(1, TYP_INT);
            GenTree* rsz = comp->gtNewOperNode(GT_RSZ, type, sub, one);
            BlockRange().InsertBefore(divMod, one, rsz);

            LIR::Use mulhiUse(BlockRange(), &sub->gtOp.gtOp2, sub);
            unsigned mulhiLclNum = ReplaceWithLclVar(mulhiUse);

            GenTree* mulhiCopy = comp->gtNewLclvNode(mulhiLclNum, type);
            GenTree* add       = comp->gtNewOperNode(GT_ADD, type, rsz, mulhiCopy);
            BlockRange().InsertBefore(divMod, mulhiCopy, add);
            comp->lvaTable[mulhiLclNum].incRefCnts(curBBWeight, comp);

            mulhi = add;
            shift -= 1;
        }

        GenTree* shiftBy = comp->gtNewIconNode(shift, TYP_INT);
        BlockRange().InsertBefore(divMod, shiftBy);

        if (isDiv)
        {
            divMod->SetOper(GT_RSZ);
            divMod->gtOp1 = mulhi;
            divMod->gtOp2 = shiftBy;
        }
        else
        {
            GenTree* div = comp->gtNewOperNode(GT_RSZ, type, mulhi, shiftBy);

            // divisor UMOD dividend = dividend SUB (div MUL divisor)
            GenTree* divisor  = comp->gtNewIconNode(divisorValue, type);
            GenTree* mul      = comp->gtNewOperNode(GT_MUL, type, div, divisor);
            GenTree* dividend = comp->gtNewLclvNode(dividendLclNum, type);

            divMod->SetOper(GT_SUB);
            divMod->gtOp1 = dividend;
            divMod->gtOp2 = mul;

            BlockRange().InsertBefore(divMod, div, divisor, mul, dividend);
            comp->lvaTable[dividendLclNum].incRefCnts(curBBWeight, comp);
        }
        ContainCheckRange(firstNode, divMod);

        return true;
    }
#endif
    return false;
}

// LowerConstIntDivOrMod: Transform integer GT_DIV/GT_MOD nodes with a power of 2
//     const divisor into equivalent but faster sequences.
//
// Arguments:
//    node - pointer to the DIV or MOD node
//
// Returns:
//    nullptr if no transformation is done, or the next node in the transformed node sequence that
//    needs to be lowered.
//
GenTree* Lowering::LowerConstIntDivOrMod(GenTree* node)
{
    assert((node->OperGet() == GT_DIV) || (node->OperGet() == GT_MOD));
    GenTree* divMod   = node;
    GenTree* dividend = divMod->gtGetOp1();
    GenTree* divisor  = divMod->gtGetOp2();

    if (!divisor->IsCnsIntOrI())
    {
        return nullptr; // no transformations to make
    }

    const var_types type = divMod->TypeGet();
    assert((type == TYP_INT) || (type == TYP_LONG));

    if (dividend->IsCnsIntOrI())
    {
        // We shouldn't see a divmod with constant operands here but if we do then it's likely
        // because optimizations are disabled or it's a case that's supposed to throw an exception.
        // Don't optimize this.
        return nullptr;
    }

    ssize_t divisorValue = divisor->gtIntCon.IconValue();

    if (divisorValue == -1 || divisorValue == 0)
    {
        // x / 0 and x % 0 can't be optimized because they are required to throw an exception.

        // x / -1 can't be optimized because INT_MIN / -1 is required to throw an exception.

        // x % -1 is always 0 and the IL spec says that the rem instruction "can" throw an exception if x is
        // the minimum representable integer. However, the C# spec says that an exception "is" thrown in this
        // case so optimizing this case would break C# code.

        // A runtime check could be used to handle this case but it's probably too rare to matter.
        return nullptr;
    }

    bool isDiv = divMod->OperGet() == GT_DIV;

    if (isDiv)
    {
        if ((type == TYP_INT && divisorValue == INT_MIN) || (type == TYP_LONG && divisorValue == INT64_MIN))
        {
            // If the divisor is the minimum representable integer value then we can use a compare,
            // the result is 1 iff the dividend equals divisor.
            divMod->SetOper(GT_EQ);
            return node;
        }
    }

    size_t absDivisorValue =
        (divisorValue == SSIZE_T_MIN) ? static_cast<size_t>(divisorValue) : static_cast<size_t>(abs(divisorValue));

    if (!isPow2(absDivisorValue))
    {
        if (comp->opts.MinOpts())
        {
            return nullptr;
        }

#if defined(_TARGET_XARCH_) || defined(_TARGET_ARM64_)
        ssize_t magic;
        int     shift;

        if (type == TYP_INT)
        {
            magic = MagicDivide::GetSigned32Magic(static_cast<int32_t>(divisorValue), &shift);
        }
        else
        {
#ifdef _TARGET_64BIT_
            magic = MagicDivide::GetSigned64Magic(static_cast<int64_t>(divisorValue), &shift);
#else
            unreached();
#endif
        }

        divisor->gtIntConCommon.SetIconValue(magic);

        // Insert a new GT_MULHI node in front of the existing GT_DIV/GT_MOD node.
        // The existing node will later be transformed into a GT_ADD/GT_SUB that
        // computes the final result. This way don't need to find and change the
        // use of the existing node.
        GenTree* mulhi = comp->gtNewOperNode(GT_MULHI, type, divisor, dividend);
        BlockRange().InsertBefore(divMod, mulhi);

        // mulhi was the easy part. Now we need to generate different code depending
        // on the divisor value:
        // For 3 we need:
        //     div = signbit(mulhi) + mulhi
        // For 5 we need:
        //     div = signbit(mulhi) + sar(mulhi, 1) ; requires shift adjust
        // For 7 we need:
        //     mulhi += dividend                    ; requires add adjust
        //     div = signbit(mulhi) + sar(mulhi, 2) ; requires shift adjust
        // For -3 we need:
        //     mulhi -= dividend                    ; requires sub adjust
        //     div = signbit(mulhi) + sar(mulhi, 1) ; requires shift adjust
        bool     requiresAddSubAdjust     = signum(divisorValue) != signum(magic);
        bool     requiresShiftAdjust      = shift != 0;
        bool     requiresDividendMultiuse = requiresAddSubAdjust || !isDiv;
        unsigned curBBWeight              = comp->compCurBB->getBBWeight(comp);
        unsigned dividendLclNum           = BAD_VAR_NUM;

        if (requiresDividendMultiuse)
        {
            LIR::Use dividendUse(BlockRange(), &mulhi->gtOp.gtOp2, mulhi);
            dividendLclNum = ReplaceWithLclVar(dividendUse);
        }

        GenTree* adjusted;

        if (requiresAddSubAdjust)
        {
            dividend = comp->gtNewLclvNode(dividendLclNum, type);
            comp->lvaTable[dividendLclNum].incRefCnts(curBBWeight, comp);

            adjusted = comp->gtNewOperNode(divisorValue > 0 ? GT_ADD : GT_SUB, type, mulhi, dividend);
            BlockRange().InsertBefore(divMod, dividend, adjusted);
        }
        else
        {
            adjusted = mulhi;
        }

        GenTree* shiftBy = comp->gtNewIconNode(genTypeSize(type) * 8 - 1, type);
        GenTree* signBit = comp->gtNewOperNode(GT_RSZ, type, adjusted, shiftBy);
        BlockRange().InsertBefore(divMod, shiftBy, signBit);

        LIR::Use adjustedUse(BlockRange(), &signBit->gtOp.gtOp1, signBit);
        unsigned adjustedLclNum = ReplaceWithLclVar(adjustedUse);
        adjusted                = comp->gtNewLclvNode(adjustedLclNum, type);
        comp->lvaTable[adjustedLclNum].incRefCnts(curBBWeight, comp);
        BlockRange().InsertBefore(divMod, adjusted);

        if (requiresShiftAdjust)
        {
            shiftBy  = comp->gtNewIconNode(shift, TYP_INT);
            adjusted = comp->gtNewOperNode(GT_RSH, type, adjusted, shiftBy);
            BlockRange().InsertBefore(divMod, shiftBy, adjusted);
        }

        if (isDiv)
        {
            divMod->SetOperRaw(GT_ADD);
            divMod->gtOp.gtOp1 = adjusted;
            divMod->gtOp.gtOp2 = signBit;
        }
        else
        {
            GenTree* div = comp->gtNewOperNode(GT_ADD, type, adjusted, signBit);

            dividend = comp->gtNewLclvNode(dividendLclNum, type);
            comp->lvaTable[dividendLclNum].incRefCnts(curBBWeight, comp);

            // divisor % dividend = dividend - divisor x div
            GenTree* divisor = comp->gtNewIconNode(divisorValue, type);
            GenTree* mul     = comp->gtNewOperNode(GT_MUL, type, div, divisor);
            BlockRange().InsertBefore(divMod, dividend, div, divisor, mul);

            divMod->SetOperRaw(GT_SUB);
            divMod->gtOp.gtOp1 = dividend;
            divMod->gtOp.gtOp2 = mul;
        }

        return mulhi;
#else
        // Currently there's no GT_MULHI for ARM32
        return nullptr;
#endif
    }

    // We're committed to the conversion now. Go find the use if any.
    LIR::Use use;
    if (!BlockRange().TryGetUse(node, &use))
    {
        return nullptr;
    }

    // We need to use the dividend node multiple times so its value needs to be
    // computed once and stored in a temp variable.

    unsigned curBBWeight = comp->compCurBB->getBBWeight(comp);

    LIR::Use opDividend(BlockRange(), &divMod->gtOp.gtOp1, divMod);
    ReplaceWithLclVar(opDividend);

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
        ContainCheckShiftRotate(newDivMod->AsOp());

        if (divisorValue < 0)
        {
            // negate the result if the divisor is negative
            newDivMod = comp->gtNewOperNode(GT_NEG, type, newDivMod);
            ContainCheckNode(newDivMod);
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
        ContainCheckBinary(newDivMod->AsOp());

        comp->lvaTable[dividendLclNum].incRefCnts(curBBWeight, comp);
    }

    // Remove the divisor and dividend nodes from the linear order,
    // since we have reused them and will resequence the tree
    BlockRange().Remove(divisor);
    BlockRange().Remove(dividend);

    // linearize and insert the new tree before the original divMod node
    InsertTreeBeforeAndContainCheck(divMod, newDivMod);
    BlockRange().Remove(divMod);

    // replace the original divmod node with the new divmod tree
    use.ReplaceWith(comp, newDivMod);

    return newDivMod->gtNext;
}
//------------------------------------------------------------------------
// LowerSignedDivOrMod: transform integer GT_DIV/GT_MOD nodes with a power of 2
// const divisor into equivalent but faster sequences.
//
// Arguments:
//    node - the DIV or MOD node
//
// Returns:
//    The next node to lower.
//
GenTree* Lowering::LowerSignedDivOrMod(GenTree* node)
{
    assert((node->OperGet() == GT_DIV) || (node->OperGet() == GT_MOD));
    GenTree* next     = node->gtNext;
    GenTree* divMod   = node;
    GenTree* dividend = divMod->gtGetOp1();
    GenTree* divisor  = divMod->gtGetOp2();

#ifdef _TARGET_XARCH_
    if (!varTypeIsFloating(node->TypeGet()))
#endif // _TARGET_XARCH_
    {
        // LowerConstIntDivOrMod will return nullptr if it doesn't transform the node.
        GenTree* newNode = LowerConstIntDivOrMod(node);
        if (newNode != nullptr)
        {
            return newNode;
        }
    }
    ContainCheckDivOrMod(node->AsOp());

    return next;
}

//------------------------------------------------------------------------
// LowerShift: Lower shift nodes
//
// Arguments:
//    shift - the shift node (GT_LSH, GT_RSH or GT_RSZ)
//
// Notes:
//    Remove unnecessary shift count masking, xarch shift instructions
//    mask the shift count to 5 bits (or 6 bits for 64 bit operations).

void Lowering::LowerShift(GenTreeOp* shift)
{
    assert(shift->OperIs(GT_LSH, GT_RSH, GT_RSZ));

    size_t mask = 0x1f;
#ifdef _TARGET_64BIT_
    if (varTypeIsLong(shift->TypeGet()))
    {
        mask = 0x3f;
    }
#else
    assert(!varTypeIsLong(shift->TypeGet()));
#endif

    for (GenTree* andOp = shift->gtGetOp2(); andOp->OperIs(GT_AND); andOp = andOp->gtGetOp1())
    {
        GenTree* maskOp = andOp->gtGetOp2();

        if (!maskOp->IsCnsIntOrI())
        {
            break;
        }

        if ((static_cast<size_t>(maskOp->AsIntCon()->IconValue()) & mask) != mask)
        {
            break;
        }

        shift->gtOp2 = andOp->gtGetOp1();
        BlockRange().Remove(andOp);
        BlockRange().Remove(maskOp);
        // The parent was replaced, clear contain and regOpt flag.
        shift->gtOp2->ClearContained();
    }
    ContainCheckShiftRotate(shift);
}

void Lowering::WidenSIMD12IfNecessary(GenTreeLclVarCommon* node)
{
#ifdef FEATURE_SIMD
    if (node->TypeGet() == TYP_SIMD12)
    {
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
        //
        // RyuJIT x86 Windows: all non-param Vector3 local vars are allocated as 16 bytes. Vector3 arguments
        // are pushed as 12 bytes. For return values, a 16-byte local is allocated and the address passed
        // as a return buffer pointer. The callee doesn't write the high 4 bytes, and we don't need to clear
        // it either.

        unsigned   varNum = node->AsLclVarCommon()->GetLclNum();
        LclVarDsc* varDsc = &comp->lvaTable[varNum];

        if (comp->lvaMapSimd12ToSimd16(varDsc))
        {
            JITDUMP("Mapping TYP_SIMD12 lclvar node to TYP_SIMD16:\n");
            DISPNODE(node);
            JITDUMP("============");

            node->gtType = TYP_SIMD16;
        }
    }
#endif // FEATURE_SIMD
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
        ReplaceWithLclVar(arrObjUse);
    }

    GenTree* arrObjNode = arrElem->gtArrObj;
    assert(arrObjNode->IsLocal());

    LclVarDsc* const varDsc = &comp->lvaTable[arrElem->gtArrObj->AsLclVarCommon()->gtLclNum];

    GenTree* insertionPoint = arrElem;

    // The first ArrOffs node will have 0 for the offset of the previous dimension.
    GenTree* prevArrOffs = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);
    BlockRange().InsertBefore(insertionPoint, prevArrOffs);
    GenTree* nextToLower = prevArrOffs;

    for (unsigned char dim = 0; dim < rank; dim++)
    {
        GenTree* indexNode = arrElem->gtArrElem.gtArrInds[dim];

        // Use the original arrObjNode on the 0th ArrIndex node, and clone it for subsequent ones.
        GenTree* idxArrObjNode;
        if (dim == 0)
        {
            idxArrObjNode = arrObjNode;
        }
        else
        {
            idxArrObjNode = comp->gtClone(arrObjNode);
            varDsc->incRefCnts(blockWeight, comp);
            BlockRange().InsertBefore(insertionPoint, idxArrObjNode);
        }

        // Next comes the GT_ARR_INDEX node.
        GenTreeArrIndex* arrMDIdx = new (comp, GT_ARR_INDEX)
            GenTreeArrIndex(TYP_INT, idxArrObjNode, indexNode, dim, rank, arrElem->gtArrElem.gtArrElemType);
        arrMDIdx->gtFlags |= ((idxArrObjNode->gtFlags | indexNode->gtFlags) & GTF_ALL_EFFECT);
        BlockRange().InsertBefore(insertionPoint, arrMDIdx);

        GenTree* offsArrObjNode = comp->gtClone(arrObjNode);
        varDsc->incRefCnts(blockWeight, comp);
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

    GenTree* leaIndexNode = prevArrOffs;
    if (!jitIsScaleIndexMul(scale))
    {
        // We do the address arithmetic in TYP_I_IMPL, though note that the lower bounds and lengths in memory are
        // TYP_INT
        GenTree* scaleNode = new (comp, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, scale);
        GenTree* mulNode   = new (comp, GT_MUL) GenTreeOp(GT_MUL, TYP_I_IMPL, leaIndexNode, scaleNode);
        BlockRange().InsertBefore(insertionPoint, scaleNode, mulNode);
        leaIndexNode = mulNode;
        scale        = 1;
    }

    GenTree* leaBase = comp->gtClone(arrObjNode);
    varDsc->incRefCnts(blockWeight, comp);
    BlockRange().InsertBefore(insertionPoint, leaBase);

    GenTree* leaNode = new (comp, GT_LEA) GenTreeAddrMode(arrElem->TypeGet(), leaBase, leaIndexNode, scale, offset);

    BlockRange().InsertBefore(insertionPoint, leaNode);

    LIR::Use arrElemUse;
    if (BlockRange().TryGetUse(arrElem, &arrElemUse))
    {
        arrElemUse.ReplaceWith(comp, leaNode);
    }
    else
    {
        leaNode->SetUnusedValue();
    }

    BlockRange().Remove(arrElem);

    JITDUMP("Results of lowering ArrElem:\n");
    DISPTREERANGE(BlockRange(), leaNode);
    JITDUMP("\n\n");

    return nextToLower;
}

void Lowering::DoPhase()
{
    // If we have any PInvoke calls, insert the one-time prolog code. We'll inserted the epilog code in the
    // appropriate spots later. NOTE: there is a minor optimization opportunity here, as we still create p/invoke
    // data structures and setup/teardown even if we've eliminated all p/invoke calls due to dead code elimination.
    if (comp->info.compCallUnmanaged)
    {
        InsertPInvokeMethodProlog();
    }

#if !defined(_TARGET_64BIT_)
    DecomposeLongs decomp(comp); // Initialize the long decomposition class.
    if (comp->compLongUsed)
    {
        decomp.PrepareForDecomposition();
    }
#endif // !defined(_TARGET_64BIT_)

    for (BasicBlock* block = comp->fgFirstBB; block; block = block->bbNext)
    {
        /* Make the block publicly available */
        comp->compCurBB = block;

#if !defined(_TARGET_64BIT_)
        if (comp->compLongUsed)
        {
            decomp.DecomposeBlock(block);
        }
#endif //!_TARGET_64BIT_

        LowerBlock(block);
    }

#ifdef DEBUG
    JITDUMP("Lower has completed modifying nodes.\n");
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
    if ((comp->lvaCount != 0) && comp->backendRequiresLocalVarLifetimes())
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

    for (BasicBlock* block = comp->fgFirstBB; block; block = block->bbNext)
    {
        assert(LIR::AsRange(block).CheckLIR(comp, true));
    }
#endif
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
        case GT_FIELD_LIST:
        {
            GenTreeFieldList* list = arg->AsFieldList();
            assert(list->isContained());
            assert(list->IsFieldListHead());

            for (; list != nullptr; list = list->Rest())
            {
                assert(list->Current()->OperIsPutArg());
            }
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
//   compiler - the compiler context.
//   node - the node to check.
//
void Lowering::CheckNode(Compiler* compiler, GenTree* node)
{
    switch (node->OperGet())
    {
        case GT_CALL:
            CheckCall(node->AsCall());
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            assert(node->TypeGet() != TYP_SIMD12);
            break;
#ifdef _TARGET_64BIT_
        case GT_LCL_VAR:
        case GT_STORE_LCL_VAR:
        {
            unsigned   lclNum = node->AsLclVarCommon()->GetLclNum();
            LclVarDsc* lclVar = &compiler->lvaTable[lclNum];
            assert(node->TypeGet() != TYP_SIMD12 || compiler->lvaIsFieldOfDependentlyPromotedStruct(lclVar));
        }
        break;
#endif // _TARGET_64BIT_
#endif // SIMD

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
        CheckNode(compiler, node);
    }

    assert(blockRange.CheckLIR(compiler, true));
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
bool Lowering::IndirsAreEquivalent(GenTree* candidate, GenTree* storeInd)
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

    GenTree* pTreeA = candidate->gtGetOp1();
    GenTree* pTreeB = storeInd->gtGetOp1();

    // This method will be called by codegen (as well as during lowering).
    // After register allocation, the sources may have been spilled and reloaded
    // to a different register, indicated by an inserted GT_RELOAD node.
    pTreeA = pTreeA->gtSkipReloadOrCopy();
    pTreeB = pTreeB->gtSkipReloadOrCopy();

    genTreeOps oper;

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
                   (gtAddr1->gtScale == gtAddr2->gtScale) && (gtAddr1->Offset() == gtAddr2->Offset());
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
bool Lowering::NodesAreEquivalentLeaves(GenTree* tree1, GenTree* tree2)
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

/**
 * Get common information required to handle a cast instruction
 */
void Lowering::getCastDescription(GenTree* treeNode, CastInfo* castInfo)
{
    // Intialize castInfo
    memset(castInfo, 0, sizeof(*castInfo));

    GenTree* castOp = treeNode->gtCast.CastOp();

    var_types dstType = treeNode->CastToType();
    var_types srcType = genActualType(castOp->TypeGet());

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

            case TYP_USHORT:
                typeMask = ssize_t((int)0xFFFF0000L);
                break;

            case TYP_INT:
                if (srcType == TYP_UINT)
                {
                    signCheckOnly = true;
                }
                else
                {
#ifdef _TARGET_64BIT_
                    typeMask = 0xFFFFFFFF80000000LL;
#else
                    typeMask = 0x80000000;
#endif
                    typeMin = INT_MIN;
                    typeMax = INT_MAX;
                }
                break;

            case TYP_UINT:
                if (srcType == TYP_INT)
                {
                    signCheckOnly = true;
                }
                else
                {
#ifdef _TARGET_64BIT_
                    typeMask = 0xFFFFFFFF00000000LL;
#else
                    typeMask = 0x00000000;
#endif
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

//------------------------------------------------------------------------
// Containment Analysis
//------------------------------------------------------------------------
void Lowering::ContainCheckNode(GenTree* node)
{
    switch (node->gtOper)
    {
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
            ContainCheckStoreLoc(node->AsLclVarCommon());
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_CMP:
        case GT_JCMP:
            ContainCheckCompare(node->AsOp());
            break;

        case GT_JTRUE:
            ContainCheckJTrue(node->AsOp());
            break;

        case GT_ADD:
        case GT_SUB:
#if !defined(_TARGET_64BIT_)
        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
#endif
        case GT_AND:
        case GT_OR:
        case GT_XOR:
            ContainCheckBinary(node->AsOp());
            break;

#if defined(_TARGET_X86_)
        case GT_MUL_LONG:
#endif
        case GT_MUL:
        case GT_MULHI:
            ContainCheckMul(node->AsOp());
            break;
        case GT_DIV:
        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
            ContainCheckDivOrMod(node->AsOp());
            break;
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
#ifndef _TARGET_64BIT_
        case GT_LSH_HI:
        case GT_RSH_LO:
#endif
            ContainCheckShiftRotate(node->AsOp());
            break;
        case GT_ARR_OFFSET:
            ContainCheckArrOffset(node->AsArrOffs());
            break;
        case GT_LCLHEAP:
            ContainCheckLclHeap(node->AsOp());
            break;
        case GT_RETURN:
            ContainCheckRet(node->AsOp());
            break;
        case GT_RETURNTRAP:
            ContainCheckReturnTrap(node->AsOp());
            break;
        case GT_STOREIND:
            ContainCheckStoreIndir(node->AsIndir());
        case GT_IND:
            ContainCheckIndir(node->AsIndir());
            break;
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
#ifdef _TARGET_ARM_
        case GT_PUTARG_SPLIT:
#endif
            // The regNum must have been set by the lowering of the call.
            assert(node->gtRegNum != REG_NA);
            break;
#ifdef _TARGET_XARCH_
        case GT_INTRINSIC:
            ContainCheckIntrinsic(node->AsOp());
            break;
#endif // _TARGET_XARCH_
#ifdef FEATURE_SIMD
        case GT_SIMD:
            ContainCheckSIMD(node->AsSIMD());
            break;
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HWIntrinsic:
            ContainCheckHWIntrinsic(node->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS
        default:
            break;
    }
}

//------------------------------------------------------------------------
// ContainCheckDivOrMod: determine which operands of a div/mod should be contained.
//
// Arguments:
//    node - pointer to the GT_UDIV/GT_UMOD node
//
void Lowering::ContainCheckDivOrMod(GenTreeOp* node)
{
    assert(node->OperIs(GT_DIV, GT_MOD, GT_UDIV, GT_UMOD));

#ifdef _TARGET_XARCH_
    GenTree* dividend = node->gtGetOp1();
    GenTree* divisor  = node->gtGetOp2();

    if (varTypeIsFloating(node->TypeGet()))
    {
        // No implicit conversions at this stage as the expectation is that
        // everything is made explicit by adding casts.
        assert(dividend->TypeGet() == divisor->TypeGet());

        if (IsContainableMemoryOp(divisor) || divisor->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(node, divisor);
        }
        else
        {
            // If there are no containable operands, we can make an operand reg optional.
            // SSE2 allows only divisor to be a memory-op.
            divisor->SetRegOptional();
        }
        return;
    }
    bool divisorCanBeRegOptional = true;
#ifdef _TARGET_X86_
    if (dividend->OperGet() == GT_LONG)
    {
        divisorCanBeRegOptional = false;
        MakeSrcContained(node, dividend);
    }
#endif

    // divisor can be an r/m, but the memory indirection must be of the same size as the divide
    if (IsContainableMemoryOp(divisor) && (divisor->TypeGet() == node->TypeGet()))
    {
        MakeSrcContained(node, divisor);
    }
    else if (divisorCanBeRegOptional)
    {
        // If there are no containable operands, we can make an operand reg optional.
        // Div instruction allows only divisor to be a memory op.
        divisor->SetRegOptional();
    }
#endif // _TARGET_XARCH_
}

//------------------------------------------------------------------------
// ContainCheckReturnTrap: determine whether the source of a RETURNTRAP should be contained.
//
// Arguments:
//    node - pointer to the GT_RETURNTRAP node
//
void Lowering::ContainCheckReturnTrap(GenTreeOp* node)
{
#ifdef _TARGET_XARCH_
    assert(node->OperIs(GT_RETURNTRAP));
    // This just turns into a compare of its child with an int + a conditional call
    if (node->gtOp1->isIndir())
    {
        MakeSrcContained(node, node->gtOp1);
    }
#endif // _TARGET_XARCH_
}

//------------------------------------------------------------------------
// ContainCheckArrOffset: determine whether the source of an ARR_OFFSET should be contained.
//
// Arguments:
//    node - pointer to the GT_ARR_OFFSET node
//
void Lowering::ContainCheckArrOffset(GenTreeArrOffs* node)
{
    assert(node->OperIs(GT_ARR_OFFSET));
    // we don't want to generate code for this
    if (node->gtOffset->IsIntegralConst(0))
    {
        MakeSrcContained(node, node->gtArrOffs.gtOffset);
    }
}

//------------------------------------------------------------------------
// ContainCheckLclHeap: determine whether the source of a GT_LCLHEAP node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckLclHeap(GenTreeOp* node)
{
    assert(node->OperIs(GT_LCLHEAP));
    GenTree* size = node->gtOp.gtOp1;
    if (size->IsCnsIntOrI())
    {
        MakeSrcContained(node, size);
    }
}

//------------------------------------------------------------------------
// ContainCheckRet: determine whether the source of a node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckRet(GenTreeOp* ret)
{
    assert(ret->OperIs(GT_RETURN));

#if !defined(_TARGET_64BIT_)
    if (ret->TypeGet() == TYP_LONG)
    {
        GenTree* op1 = ret->gtGetOp1();
        noway_assert(op1->OperGet() == GT_LONG);
        MakeSrcContained(ret, op1);
    }
#endif // !defined(_TARGET_64BIT_)
#if FEATURE_MULTIREG_RET
    if (varTypeIsStruct(ret))
    {
        GenTree* op1 = ret->gtGetOp1();
        // op1 must be either a lclvar or a multi-reg returning call
        if (op1->OperGet() == GT_LCL_VAR)
        {
            GenTreeLclVarCommon* lclVarCommon = op1->AsLclVarCommon();
            LclVarDsc*           varDsc       = &(comp->lvaTable[lclVarCommon->gtLclNum]);
            assert(varDsc->lvIsMultiRegRet);

            // Mark var as contained if not enregistrable.
            if (!varTypeIsEnregisterableStruct(op1))
            {
                MakeSrcContained(ret, op1);
            }
        }
    }
#endif // FEATURE_MULTIREG_RET
}

//------------------------------------------------------------------------
// ContainCheckJTrue: determine whether the source of a JTRUE should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckJTrue(GenTreeOp* node)
{
    // The compare does not need to be generated into a register.
    GenTree* cmp = node->gtGetOp1();
    cmp->gtType  = TYP_VOID;
    cmp->gtFlags |= GTF_SET_FLAGS;
}

#endif // !LEGACY_BACKEND
