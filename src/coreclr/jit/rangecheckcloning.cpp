// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "jitpch.h"
#include "rangecheckcloning.h"

bool BoundsCheckInfo::Initialize(const Compiler*   comp,
                                 Statement*        statement,
                                 GenTreeBoundsChk* bndChkNode,
                                 GenTree*          bndChkParentNode)
{
    idxVN = comp->vnStore->VNConservativeNormalValue(bndChkNode->GetIndex()->gtVNPair);
    lenVN = comp->vnStore->VNConservativeNormalValue(bndChkNode->GetArrayLength()->gtVNPair);
    if ((idxVN == ValueNumStore::NoVN) || (lenVN == ValueNumStore::NoVN))
    {
        return false;
    }
    stmt         = statement;
    bndChk       = bndChkNode;
    bndChkParent = bndChkParentNode;

    if (bndChkParent != nullptr)
    {
        assert(bndChkParent->OperIs(GT_COMMA));
        if (bndChkParent->gtGetOp2() == bndChkNode)
        {
            // GT_BOUNDS_CHECK is mostly LHS of COMMAs
            // In rare cases, it can be either a root node or RHS of COMMAs
            // Unfortunately, optRemoveRangeCheck doesn't know how to handle it
            // being RHS of COMMAs. TODO-RangeCheckCloning: fix that
            return false;
        }
    }

    if (bndChkNode->GetIndex()->IsIntCnsFitsInI32())
    {
        // Index being a constant means with have 0 index and cns offset
        offset = static_cast<int>(bndChkNode->GetIndex()->AsIntCon()->IconValue());
        idxVN  = comp->vnStore->VNZeroForType(TYP_INT);
    }
    else
    {
        // Otherwise, peel the offset from the index using VN
        comp->vnStore->PeelOffsetsI32(&idxVN, &offset);
        assert(idxVN != ValueNumStore::NoVN);
    }

    if (offset < 0)
    {
        // Not supported yet.
        return false;
    }
    return true;
}

static BasicBlock* optCloneBlocks_DoClone(Compiler* comp, BasicBlock* block, BoundsCheckInfoStack* guard)
{
    BasicBlock* prevBb     = block;
    BasicBlock* fallbackBb = nullptr;

    BoundsCheckInfo firstCheck = guard->Bottom();
    BoundsCheckInfo lastCheck  = guard->Top();

    GenTree**   bndChkUse;
    Statement*  newFirstStmt;
    BasicBlock* fastpathBb =
        comp->fgSplitBlockBeforeTree(block, firstCheck.stmt, firstCheck.bndChk, &newFirstStmt, &bndChkUse);
    while ((newFirstStmt != nullptr) && (newFirstStmt != firstCheck.stmt))
    {
        comp->fgMorphStmtBlockOps(fastpathBb, newFirstStmt);
        newFirstStmt = newFirstStmt->GetNextStmt();
    }
    comp->fgMorphStmtBlockOps(fastpathBb, firstCheck.stmt);
    comp->gtUpdateStmtSideEffects(firstCheck.stmt);

    BasicBlock* lastBb = comp->fgSplitBlockAfterStatement(fastpathBb, lastCheck.stmt);

    // TODO-CloneBlocks: call gtSplitTree for lastBndChkStmt as well, to cut off
    // the stuff we don't have to clone.

    DebugInfo debugInfo = fastpathBb->firstStmt()->GetDebugInfo();

    int      offset = 0;
    GenTree* idx    = firstCheck.bndChk->GetIndex();
    GenTree* arrLen = firstCheck.bndChk->GetArrayLength();
    assert((idx->gtFlags & GTF_ALL_EFFECT) == 0);
    assert((arrLen->gtFlags & GTF_ALL_EFFECT) == 0);

    // Find the maximum offset
    for (int i = 0; i < guard->Height(); i++)
    {
        offset = max(offset, guard->Top(i).offset);
    }
    assert(offset >= 0);

    if (idx == nullptr)
    {
        // it can be null when we deal with constant indices, so we treat them as offsets
        // In this case, we don't need the LowerBound check, but let's still create it for
        // simplicity, the downstream code will eliminate it.
        idx = comp->gtNewIconNode(0);
    }
    else
    {
        idx = comp->gtCloneExpr(idx);
    }

    // Since we're re-using the index node from the first bounds check and its value was spilled
    // by the tree split, we need to restore the base index by subtracting the offset.

    GenTree* idxClone;
    if (firstCheck.offset > 0)
    {
        GenTree* offsetNode = comp->gtNewIconNode(-firstCheck.offset);
        idx                 = comp->gtNewOperNode(GT_ADD, TYP_INT, idx, offsetNode);
        idxClone            = comp->fgInsertCommaFormTemp(&idx);
    }
    else
    {
        idxClone = comp->gtCloneExpr(idx);
    }

    // 1) lowerBndBb:
    //
    // if (i >= 0)
    //     goto UpperBoundCheck
    // else
    //     goto Fallback
    //
    GenTreeOp* idxLowerBoundTree = comp->gtNewOperNode(GT_GE, TYP_INT, comp->gtCloneExpr(idx), comp->gtNewIconNode(0));
    idxLowerBoundTree->gtFlags |= GTF_RELOP_JMP_USED;

    GenTree*    jtrue      = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, idxLowerBoundTree);
    BasicBlock* lowerBndBb = comp->fgNewBBFromTreeAfter(BBJ_COND, prevBb, jtrue, debugInfo);

    // 2) upperBndBb:
    //
    // if (i < arrLen - indexOffset)
    //     goto Fastpath
    // else
    //     goto Fallback
    //

    GenTree*   arrLenClone = comp->gtCloneExpr(arrLen);
    GenTreeOp* idxUpperBoundTree;
    if (idx->IsIntegralConst(0))
    {
        // if the index is just 0, then we can simplify the condition to "arrLen > indexOffset"
        idxUpperBoundTree = comp->gtNewOperNode(GT_GT, TYP_INT, arrLenClone, comp->gtNewIconNode(offset));
    }
    else
    {
        // "i < arrLen + (-indexOffset)"
        GenTree*   negOffset = comp->gtNewIconNode(-offset);
        GenTreeOp* subNode   = comp->gtNewOperNode(GT_ADD, TYP_INT, arrLenClone, negOffset);
        idxUpperBoundTree    = comp->gtNewOperNode(GT_LT, TYP_INT, idxClone, subNode);
    }
    idxUpperBoundTree->gtFlags |= GTF_RELOP_JMP_USED;

    jtrue                  = comp->gtNewOperNode(GT_JTRUE, TYP_VOID, idxUpperBoundTree);
    BasicBlock* upperBndBb = comp->fgNewBBFromTreeAfter(BBJ_COND, lowerBndBb, jtrue, debugInfo);

    // 3) fallbackBb:
    //
    // For the fallback (slow path), we just entirely clone the fast path.
    // We do it only once in case we have multiple root bounds checks.
    //
    if (fallbackBb == nullptr)
    {
        fallbackBb = comp->fgNewBBafter(BBJ_ALWAYS, lowerBndBb, false);
        BasicBlock::CloneBlockState(comp, fallbackBb, fastpathBb);
    }

    // 4) fastBlockBb:
    //
    // No actions needed - it's our current block as is.

    // Wire up the edges
    //
    comp->fgRedirectTargetEdge(prevBb, lowerBndBb);
    // We need to link the fallbackBb to the lastBb only once.
    FlowEdge* fallbackToNextBb = comp->fgAddRefPred(lastBb, fallbackBb);
    fallbackBb->SetTargetEdge(fallbackToNextBb);
    fallbackToNextBb->setLikelihood(1.0f);

    FlowEdge* lowerBndToUpperBndEdge = comp->fgAddRefPred(upperBndBb, lowerBndBb);
    FlowEdge* lowerBndToFallbackEdge = comp->fgAddRefPred(fallbackBb, lowerBndBb);
    FlowEdge* upperBndToFastPathEdge = comp->fgAddRefPred(fastpathBb, upperBndBb);
    FlowEdge* upperBndToFallbackEdge = comp->fgAddRefPred(fallbackBb, upperBndBb);
    lowerBndBb->SetTrueEdge(lowerBndToUpperBndEdge);
    lowerBndBb->SetFalseEdge(lowerBndToFallbackEdge);
    upperBndBb->SetTrueEdge(upperBndToFastPathEdge);
    upperBndBb->SetFalseEdge(upperBndToFallbackEdge);

    // Set the weights. We assume that the fallback is rarely taken.
    //
    lowerBndBb->inheritWeightPercentage(prevBb, 100);
    upperBndBb->inheritWeightPercentage(prevBb, 100);
    fastpathBb->inheritWeightPercentage(prevBb, 100);
    fallbackBb->bbSetRunRarely();
    lowerBndToUpperBndEdge->setLikelihood(1.0f);
    lowerBndToFallbackEdge->setLikelihood(0.0f);
    upperBndToFastPathEdge->setLikelihood(1.0f);
    upperBndToFallbackEdge->setLikelihood(0.0f);

    lowerBndBb->SetFlags(BBF_INTERNAL);
    upperBndBb->SetFlags(BBF_INTERNAL | BBF_HAS_IDX_LEN);

    comp->fgMorphBlockStmt(lowerBndBb, lowerBndBb->lastStmt() DEBUGARG("Morph lowerBnd"));
    comp->fgMorphBlockStmt(upperBndBb, upperBndBb->lastStmt() DEBUGARG("Morph upperBnd"));

    if (lowerBndBb->lastStmt() != nullptr)
    {
        // lowerBndBb might be converted into no-op by fgMorphBlockStmt(lowerBndBb)
        // it happens when we emit BBJ_COND(0 >= 0) fake block (for simplicity)
        comp->gtUpdateStmtSideEffects(lowerBndBb->lastStmt());
    }
    comp->gtUpdateStmtSideEffects(upperBndBb->lastStmt());

    // Now drop the bounds check from the fast path
    while (!guard->Empty())
    {
        BoundsCheckInfo info = guard->Pop();
        comp->optRemoveRangeCheck(info.bndChk, info.bndChkParent, info.stmt);
        comp->gtSetStmtInfo(info.stmt);
        comp->fgSetStmtSeq(info.stmt);
    }

    // we need to inspect the fastpath block again
    return fastpathBb->Prev();
}

PhaseStatus Compiler::optRangeCheckCloning()
{
    if (!doesMethodHaveBoundsChecks())
    {
        JITDUMP("Current method has no bounds checks\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (lvaHaveManyLocals(0.75))
    {
        // If we're close to running out of locals, we should be conservative
        JITDUMP("Too many locals - bail out.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    const bool preferSize = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT);
    if (preferSize)
    {
        // The optimization comes with a codegen size increase
        JITDUMP("Optimized for size - bail out.\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool changed = false;

    BoundsCheckInfoMap allBndChks(getAllocator(CMK_Generic));
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->Next())
    {
        if (block->isRunRarely())
        {
            continue;
        }

        if (!block->KindIs(BBJ_ALWAYS, BBJ_RETURN, BBJ_COND))
        {
            // For now, we support only simple blocks
            continue;
        }

        allBndChks.RemoveAll();

        for (Statement* const stmt : block->Statements())
        {
            if (block->KindIs(BBJ_COND, BBJ_RETURN) && (stmt == block->lastStmt()))
            {
                // TODO: Splitting these blocks at the last statements
                // require using gtSplitTree for the last bounds check.
                break;
            }

            struct TreeWalkData
            {
                Statement*          stmt;
                BoundsCheckInfoMap* boundsChks;
            } walkData = {stmt, &allBndChks};

            auto visitor = [](GenTree** slot, fgWalkData* data) -> fgWalkResult {
                GenTree*      node     = *slot;
                TreeWalkData* walkData = static_cast<TreeWalkData*>(data->pCallbackData);
                if (node->OperIs(GT_BOUNDS_CHECK))
                {
                    BoundsCheckInfo info{};
                    if (info.Initialize(data->compiler, walkData->stmt, node->AsBoundsChk(), data->parent))
                    {
                        IndexLengthPair       key(info.idxVN, info.lenVN);
                        BoundsCheckInfoStack* value;
                        if (!walkData->boundsChks->Lookup(key, &value))
                        {
                            CompAllocator allocator = data->compiler->getAllocator(CMK_Generic);
                            value                   = new (allocator) BoundsCheckInfoStack(allocator);
                            walkData->boundsChks->Set(key, value);
                        }
                        value->Push(info);
                    }
                }
                return WALK_CONTINUE;
            };
            fgWalkTreePre(stmt->GetRootNodePointer(), visitor, &walkData);
        }

        if (allBndChks.GetCount() == 0)
        {
            JITDUMP("No bounds checks in the block - bail out.\n");
            continue;
        }

        // Now choose the largest group of bounds checks
        BoundsCheckInfoStack* largestGroup = nullptr;
        for (BoundsCheckInfoMap::Node* keyValuePair : BoundsCheckInfoMap::KeyValueIteration(&allBndChks))
        {
            ArrayStack<BoundsCheckInfo>* value = keyValuePair->GetValue();
            if ((largestGroup == nullptr) || (value->Height() > largestGroup->Height()))
            {
                largestGroup = value;
            }
        }

        assert(largestGroup != nullptr);
        if (largestGroup->Height() < MIN_CHECKS_PER_GROUP)
        {
            JITDUMP("Not enough bounds checks in the largest group - bail out.\n");
            continue;
        }

        block   = optCloneBlocks_DoClone(this, block, largestGroup);
        changed = true;
    }

    if (changed)
    {
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
