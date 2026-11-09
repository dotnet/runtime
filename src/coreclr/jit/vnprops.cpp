// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Contextual VN facts, their provenance, and the IR properties they establish.
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "valuenum.h"
#include "ssaconfig.h"

//------------------------------------------------------------------------
// GetWriteBarrierForm: Determinate the exact type of write barrier required for the
//    given address.
//
// Arguments:
//    vnStore - ValueNumStore object
//    vn      - VN of the address
//
// Return Value:
//    Exact type of write barrier required for the given address.
//
static GCInfo::WriteBarrierForm GetWriteBarrierForm(Compiler* comp, ValueNum vn)
{
    ValueNumStore*  vnStore = comp->vnStore;
    const var_types type    = vnStore->TypeOfVN(vn);
    if (type == TYP_REF)
    {
        return GCInfo::WriteBarrierForm::WBF_BarrierUnchecked;
    }
    if (type != TYP_BYREF)
    {
        return GCInfo::WriteBarrierForm::WBF_BarrierUnknown;
    }

    VNFuncApp funcApp;
    if (vnStore->GetVNFunc(vnStore->VNNormalValue(vn), &funcApp))
    {
        if (funcApp.FuncIs(VNF_PtrToArrElem))
        {
            // Check whether the array is on the heap
            ValueNum arrayVN = funcApp.GetArg(1);
            return GetWriteBarrierForm(comp, arrayVN);
        }
        if (funcApp.FuncIs(VNF_PtrToLoc))
        {
            // Pointer to a local
            return GCInfo::WriteBarrierForm::WBF_NoBarrier;
        }
        if ((funcApp.FuncIs(VNF_PtrToStatic)) && vnStore->IsVNHandle(funcApp.GetArg(0), GTF_ICON_STATIC_BOX_PTR))
        {
            // Boxed static - always on the heap
            return GCInfo::WriteBarrierForm::WBF_BarrierUnchecked;
        }
        if (funcApp.FuncIs(VNF_ADD))
        {
            // Check arguments of the GT_ADD
            // To make it conservative, we require one of the arguments to be a constant, e.g.:
            //
            //   addressOfLocal + cns    -> NoBarrier
            //   cns + addressWithinHeap -> BarrierUnchecked
            //
            // Because "addressOfLocal + nativeIntVariable" could be in fact a pointer to the heap.
            // if "nativeIntVariable == addressWithinHeap - addressOfLocal".
            //
            if (vnStore->IsVNConstantNonHandle(funcApp.GetArg(0)))
            {
                return GetWriteBarrierForm(comp, funcApp.GetArg(1));
            }
            if (vnStore->IsVNConstantNonHandle(funcApp.GetArg(1)))
            {
                return GetWriteBarrierForm(comp, funcApp.GetArg(0));
            }
        }
    }
    return GCInfo::WriteBarrierForm::WBF_BarrierUnknown;
}

//------------------------------------------------------------------------
// fgVNCollectStoreProperties: Record contextual store facts and defer barrier classification
// until all reaching values have been numbered. Memory-state updates remain with store numbering.
//
void Compiler::fgVNCollectStoreProperties(GenTree* store, ValueNum valueVN)
{
    assert(store->OperIs(GT_STOREIND, GT_STORE_BLK));
    GenTree* value = store->Data();
    // A null value never requires a write barrier. Keep the store and its destination
    // null check: this fact says nothing about whether the destination is valid.
    if ((vnKnownNullValues != nullptr) && store->OperIs(GT_STOREIND) && store->TypeIs(TYP_REF) &&
        value->TypeIs(TYP_REF) && ((store->gtFlags & GTF_IND_TGT_NOT_HEAP) == 0) &&
        fgVNGetFactAnchor(vnKnownNullValues, valueVN, compCurBB, false))
    {
        store->gtFlags |= GTF_IND_TGT_NOT_HEAP;
        JITDUMP("Null-edge fact removes write barrier on [%06u]\n", dspTreeID(store));
    }

    bool classifyBarrier = false;
    if (store->OperIs(GT_STOREIND))
    {
        classifyBarrier =
            store->TypeIs(TYP_REF) && value->TypeIs(TYP_REF) && ((store->gtFlags & GTF_IND_TGT_NOT_HEAP) == 0);
    }
    else if (store->OperIs(GT_STORE_BLK))
    {
        classifyBarrier = store->AsBlk()->GetLayout()->HasGCPtr() &&
                          ((store->gtFlags & (GTF_IND_TGT_NOT_HEAP | GTF_IND_TGT_HEAP)) == 0);
    }
    if (classifyBarrier)
    {
        if (vnWriteBarrierStores == nullptr)
        {
            vnWriteBarrierStores =
                new (getAllocator(CMK_ValueNumber)) ArrayStack<GenTree*>(getAllocator(CMK_ValueNumber));
        }
        vnWriteBarrierStores->Push(store);
    }
}

//------------------------------------------------------------------------
// fgValueNumberWriteBarrier: Publish the barrier classification before CSE can hide
// the store's value or address behind locals. Called after all reaching VNs are available.
//
void Compiler::fgValueNumberWriteBarrier(GenTree* store)
{
    assert(store->OperIs(GT_STOREIND, GT_STORE_BLK));
    if ((store->gtFlags & GTF_IND_TGT_NOT_HEAP) != 0)
    {
        return;
    }

    if (store->OperIs(GT_STOREIND))
    {
        if (!store->TypeIs(TYP_REF) || !store->Data()->TypeIs(TYP_REF))
        {
            return;
        }
        auto isNonGcValue = [this](ValueNum vn) {
            return ((vn == ValueNumStore::VNForNull()) || vnStore->IsVNObjHandle(vn)) ? ValueNumStore::VNVisit::Continue
                                                                                      : ValueNumStore::VNVisit::Abort;
        };
        if (vnStore->VNVisitReachingVNs(vnStore->VNConservativeNormalValue(store->Data()->gtVNPair), isNonGcValue) ==
            ValueNumStore::VNVisit::Continue)
        {
            store->gtFlags |= GTF_IND_TGT_NOT_HEAP;
            return;
        }
    }
    else if (!store->AsBlk()->GetLayout()->HasGCPtr())
    {
        return;
    }

    if ((store->gtFlags & GTF_IND_TGT_HEAP) == 0)
    {
        GCInfo::WriteBarrierForm form =
            GetWriteBarrierForm(this, vnStore->VNConservativeNormalValue(store->AsIndir()->Addr()->gtVNPair));
        if (form == GCInfo::WBF_NoBarrier)
        {
            store->gtFlags |= GTF_IND_TGT_NOT_HEAP;
        }
        else if (form == GCInfo::WBF_BarrierUnchecked)
        {
            store->gtFlags |= GTF_IND_TGT_HEAP;
        }
    }
}

//--------------------------------------------------------------------------------
// fgVNDropSurvivedCastException: Drop a cast helper call's InvalidCastExc when a dominating cast of
//    the same object to the same class has been survived, and record this one.
//
// Arguments:
//    call - the CastClass helper call, already value numbered
//
// Notes:
//    The cast's value number is memoized per object and class, so the exception is removed from the
//    call's set here rather than left out when the value number is first built.
//
void Compiler::fgVNDropSurvivedCastException(GenTreeCall* call)
{
    GenTree* const castTo = call->gtArgs.GetUserArgByIndex(0)->GetNode();
    GenTree* const obj    = call->gtArgs.GetUserArgByIndex(1)->GetNode();

    ValueNum const itemL =
        vnStore->VNForFuncNoFolding(TYP_REF, VNF_InvalidCastExc, vnStore->VNNormalValue(obj->gtVNPair.GetLiberal()),
                                    vnStore->VNNormalValue(castTo->gtVNPair.GetLiberal()));
    ValueNum const itemC     = vnStore->VNForFuncNoFolding(TYP_REF, VNF_InvalidCastExc,
                                                           vnStore->VNNormalValue(obj->gtVNPair.GetConservative()),
                                                           vnStore->VNNormalValue(castTo->gtVNPair.GetConservative()));
    bool const     survivedL = fgVNSurvivedException(itemL DEBUGARG(call));
    bool const     survivedC = (itemC == itemL) && survivedL;

    if (survivedL)
    {
        ValueNum norm;
        ValueNum exc;
        vnStore->VNUnpackExc(call->gtVNPair.GetLiberal(), &norm, &exc);
        call->gtVNPair.SetLiberal(vnStore->VNWithExc(norm, vnStore->VNExcSetRemove(exc, itemL)));
    }
    if (survivedC)
    {
        ValueNum norm;
        ValueNum exc;
        vnStore->VNUnpackExc(call->gtVNPair.GetConservative(), &norm, &exc);
        call->gtVNPair.SetConservative(vnStore->VNWithExc(norm, vnStore->VNExcSetRemove(exc, itemC)));
    }
}

//--------------------------------------------------------------------------------
// fgVNRecordNullBranchFacts: Record null and non-null facts established on branch edges.
//
// Arguments:
//    block - the block whose trees have just been value numbered
//
void Compiler::fgVNRecordNullBranchFacts(BasicBlock* block)
{
    if (!block->KindIs(BBJ_COND) || (block->GetTrueTarget() == block->GetFalseTarget()))
    {
        return;
    }

    GenTree* cond = block->lastStmt()->GetRootNode()->gtGetOp1();
    if (!cond->OperIs(GT_EQ, GT_NE))
    {
        return;
    }

    GenTree* value = cond->gtGetOp1();
    GenTree* zero  = cond->gtGetOp2();
    if (value->IsIntegralConst(0))
    {
        std::swap(value, zero);
    }
    if (!zero->IsIntegralConst(0) || !value->TypeIs(TYP_REF) || !value->gtVNPair.BothDefined())
    {
        return;
    }

    auto edgeEstablishesFact = [this, block](BasicBlock* successor) {
        if ((successor == block) || (successor == fgFirstBB) || !m_dfsTree->Contains(successor))
        {
            return false;
        }

        // A fact about one edge is a fact at the successor only if every entry takes
        // that edge. Include exceptional entries, just as the join proof does.
        FlowEdge* preds = BlockDominancePreds(successor);
        if (preds == nullptr)
        {
            return false;
        }
        for (FlowEdge* edge = preds; edge != nullptr; edge = edge->getNextPredEdge())
        {
            BasicBlock* pred = edge->getSourceBlock();
            if (m_dfsTree->Contains(pred) && (pred != block))
            {
                return false;
            }
        }

        return true;
    };

    BasicBlock* successor = cond->OperIs(GT_NE) ? block->GetTrueTarget() : block->GetFalseTarget();
    if (edgeEstablishesFact(successor))
    {
        ValueNum valueVN = vnStore->VNNormalValue(value->GetVN(VNK_Liberal));
        if (!vnStore->IsKnownNonNull(valueVN))
        {
            fgVNSurvivedException(vnStore->VNForFunc(TYP_REF, VNF_NullPtrExc, valueVN)
                                      DEBUGARG(block->lastStmt()->GetRootNode()),
                                  successor);
            JITDUMP("Non-null branch fact for " FMT_VN " at " FMT_BB "\n", valueVN, successor->bbNum);
        }
        ValueNum conservativeVN = vnStore->VNNormalValue(value->GetVN(VNK_Conservative));
        if ((conservativeVN != valueVN) && !vnStore->IsKnownNonNull(conservativeVN))
        {
            fgVNSurvivedException(vnStore->VNForFunc(TYP_REF, VNF_NullPtrExc, conservativeVN)
                                      DEBUGARG(block->lastStmt()->GetRootNode()),
                                  successor);
            vnHasConservativeBranchFacts = true;
            JITDUMP("Conservative non-null branch fact for " FMT_VN " at " FMT_BB "\n", conservativeVN,
                    successor->bbNum);
        }
    }

    successor = cond->OperIs(GT_EQ) ? block->GetTrueTarget() : block->GetFalseTarget();
    if (edgeEstablishesFact(successor))
    {
        ValueNum valueVN = vnStore->VNConservativeNormalValue(value->gtVNPair);
        if (valueVN != ValueNumStore::VNForNull())
        {
            if (vnKnownNullValues == nullptr)
            {
                vnKnownNullValues = new (getAllocator(CMK_ValueNumber)) VNFactMap(getAllocator(CMK_ValueNumber));
            }
            fgVNGetFactAnchor(vnKnownNullValues, valueVN, successor, true);
            JITDUMP("Null branch fact for " FMT_VN " at " FMT_BB "\n", valueVN, successor->bbNum);
        }
    }
}

//--------------------------------------------------------------------------------
// fgVNSurvivedException: Whether this exception item is ruled out at the given point, and record
//    that the current block survives it. A block one of the recorded blocks dominates adds nothing; a
//    block dominating a recorded one takes its place; any other is a further way of reaching the item
//    and is added. One hash lookup serves both.
//
// Arguments:
//    excItemVN - the exception item
//    source    - the operation, or branch terminator when recording an edge fact
//    block     - the edge destination; null for the current operation's block
//
// Return Value:
//    The establishing block, or nullptr when the item has not been survived.
//
BasicBlock* Compiler::fgVNSurvivedException(ValueNum excItemVN DEBUGARG(GenTree* source), BasicBlock* block)
{
#ifdef DEBUG
    if (block == nullptr)
    {
        VNFuncApp item;
        if (vnStore->GetVNFunc(excItemVN, &item))
        {
            ProofFacts established = gtNodeMayEstablishProof(source);
            if (item.FuncIs(VNF_NullPtrExc))
            {
                assert((established & PROOF_NULL) != 0);
            }
            else if (item.FuncIs(VNF_DivideByZeroExc, VNF_ArithmeticExc))
            {
                assert((established & PROOF_DIVISION) != 0);
            }
        }
    }
#endif
    return fgVNGetFactAnchor(vnSurvivedExceptions, excItemVN, block != nullptr ? block : compCurBB, true);
}

// Large witness sets use a sparse Fenwick tree over dominator preorder. Each bucket
// holds the witness with the greatest dominator postorder. A prefix query therefore
// finds a witness dominating the query exactly when one exists: dominance requires
// preorder(witness) <= preorder(query) and postorder(witness) >= postorder(query).
// Both lookup and insertion take O(log(block count)) hash operations. Small sets
// retain their inline/list representation, avoiding an index for ordinary methods.
BasicBlock* Compiler::VNFactBlocks::FindDominating(FlowGraphDominatorTree* domTree, BasicBlock* block)
{
    if ((more != nullptr) && (more->index != nullptr))
    {
        for (unsigned i = domTree->GetPreorderNum(block) + 1; i != 0; i &= i - 1)
        {
            BasicBlock* witness;
            if (more->index->Lookup(i, &witness) && domTree->Dominates(witness, block))
            {
                return witness;
            }
        }
        return nullptr;
    }

    if (domTree->Dominates(first, block))
    {
        return first;
    }
    if (more != nullptr)
    {
        for (int i = 0; i < more->blocks.Height(); i++)
        {
            BasicBlock* witness = more->blocks.BottomRef(i);
            if (domTree->Dominates(witness, block))
            {
                return witness;
            }
        }
    }
    return nullptr;
}

void Compiler::VNFactBlocks::IndexBlock(FlowGraphDominatorTree* domTree, BasicBlock* witness)
{
    BasicBlock* const block = witness;
    const unsigned    count = domTree->GetDfsTree()->GetPostOrderCount();
    for (unsigned i = domTree->GetPreorderNum(block) + 1; i <= count; i += i & (0u - i))
    {
        BasicBlock** slot = more->index->LookupPointerOrAdd(i, nullptr);
        if ((*slot != nullptr) && (domTree->GetPostorderNum(*slot) >= domTree->GetPostorderNum(block)))
        {
            // Every containing bucket already includes this witness as well.
            break;
        }
        *slot = witness;
    }
}

// Check a fact's dominance and join coverage. With record=true the caller establishes
// the fact on normal completion. Queries do not establish facts or mutate the map.
// Return the establishing block or covered join, or nullptr when the fact is unavailable.
BasicBlock* Compiler::fgVNGetFactAnchor(VNFactMap* facts, ValueNum factVN, BasicBlock* block, bool record)
{
    if ((facts == nullptr) || (block == nullptr) || (m_domTree == nullptr))
    {
        return nullptr;
    }

    VNFactBlocks* const blocks =
        record ? facts->LookupPointerOrAdd(factVN, VNFactBlocks()) : facts->LookupPointer(factVN);
    if (blocks == nullptr)
    {
        return nullptr;
    }
    if (blocks->first == nullptr)
    {
        if (record)
        {
            blocks->first = block;
        }
        return nullptr;
    }

    if (BasicBlock* anchor = blocks->FindDominating(m_domTree, block))
    {
        return anchor;
    }

    // Not dominated by a survivor. A join reached only through survivors counts as one and is recorded
    // as a survivor itself. Any other block is a further way of reaching the item. The join is judged on
    // its dominance predecessors, so a handler counts every way the try can be left before completing.
    FlowEdge* const domPreds    = BlockDominancePreds(block);
    bool            joinCovered = domPreds != nullptr;
    for (FlowEdge* edge = domPreds; edge != nullptr; edge = edge->getNextPredEdge())
    {
        BasicBlock* const pred = edge->getSourceBlock();
        if (!m_dfsTree->Contains(pred))
        {
            continue;
        }

        if (blocks->FindDominating(m_domTree, pred) == nullptr)
        {
            joinCovered = false;
            break;
        }
    }

    BasicBlock* proof = joinCovered ? block : nullptr;

    if (!record)
    {
        return proof;
    }

    BasicBlock* current = block;

    if ((blocks->more != nullptr) && (blocks->more->index != nullptr))
    {
        blocks->IndexBlock(m_domTree, current);
        return proof;
    }

    const int count = 1 + ((blocks->more == nullptr) ? 0 : blocks->more->blocks.Height());
    for (int i = 0; i < count; i++)
    {
        BasicBlock*& witness = (i == 0) ? blocks->first : blocks->more->blocks.BottomRef(i - 1);
        if (m_domTree->Dominates(block, witness))
        {
            witness = current;
            return proof;
        }
    }
    if (blocks->more == nullptr)
    {
        blocks->more = new (getAllocator(CMK_ValueNumber)) VNFactBlocks::Additional(getAllocator(CMK_ValueNumber));
    }

    // This is a representation threshold, not a limit on the facts we retain.
    if (count == 16)
    {
        blocks->more->index =
            new (getAllocator(CMK_ValueNumber)) VNFactBlocks::DominanceIndex(getAllocator(CMK_ValueNumber));
        blocks->IndexBlock(m_domTree, blocks->first);
        for (int i = 0; i < blocks->more->blocks.Height(); i++)
        {
            blocks->IndexBlock(m_domTree, blocks->more->blocks.BottomRef(i));
        }
        blocks->IndexBlock(m_domTree, current);
    }
    else
    {
        blocks->more->blocks.Push(current);
    }
    return proof;
}

//------------------------------------------------------------------------
// fgVNCopyExceptionDependencies: Copy proof provenance to a structurally identical expression clone.
//
// Arguments:
//    source - the original expression
//    clone  - its copy, with the same shape and values
//    dependencies - the records supplied to hoisting
//
void Compiler::fgVNCopyExceptionDependencies(GenTree*                       source,
                                             GenTree*                       clone,
                                             const VNExceptionDependencies& dependencies)
{
    assert(mostRecentlyActivePhase == PHASE_HOIST_LOOP_CODE);
    if (dependencies.map == nullptr)
    {
        return;
    }
    assert(source->OperGet() == clone->OperGet());
    BasicBlock* anchor;
    if (dependencies.map->Lookup(source, &anchor))
    {
        dependencies.map->Set(clone, anchor);
    }

    // Follow the same paired operand traversal used for loop-memory dependencies.
    auto cloneOperand = clone->OperandsBegin();
    for (GenTree* operand : source->Operands())
    {
        assert(cloneOperand != clone->OperandsEnd());
        fgVNCopyExceptionDependencies(operand, *cloneOperand, dependencies);
        ++cloneOperand;
    }
    assert(cloneOperand == clone->OperandsEnd());
}

//--------------------------------------------------------------------------------
// fgVNRecordExceptionDependency: Record where a contextual non-throwing proof published on a node holds.
//
// Arguments:
//    node   - the node given GTF_ORDER_DOMINATED for the proof
//    incoming - the establishing block that bounds motion
//
// Notes:
//    The flag keeps the node below the check it relies on, but carries no location. When
//    several proofs pin the same node, the binding one is in the block the others dominate.
//
void Compiler::fgVNRecordExceptionDependency(GenTree* node, BasicBlock* incoming)
{
    assert(mostRecentlyActivePhase == PHASE_VALUE_NUMBER);
    assert(incoming != nullptr);
    assert(node->OperSupportsDominatedOrdering());
    if (!vnRecordExceptionDependencies)
    {
        return;
    }
    if (vnExceptionDependencies == nullptr)
    {
        vnExceptionDependencies =
            new (getAllocator(CMK_ValueNumber)) VNExceptionDependencyMap(getAllocator(CMK_ValueNumber));
    }

    // A new entry starts empty, which any proof replaces.
    BasicBlock** const anchor = vnExceptionDependencies->LookupPointerOrAdd(node, nullptr);
    if ((*anchor == nullptr) || m_domTree->Dominates(*anchor, incoming))
    {
        *anchor = incoming;
    }
    JITDUMP("VN dependency for [%06u] anchored at " FMT_BB "\n", dspTreeID(node), (*anchor)->bbNum);
}

//------------------------------------------------------------------------
// fgVNGetExceptionDependency: Get the recorded dependency of a node's GTF_ORDER_DOMINATED.
//
// Arguments:
//    node       - the node
//    anchor     - [out] the block establishing the dependency
//    dependencies - the records supplied to hoisting
//
// Return Value:
//    False when the dependency is unknown.
//
// Notes:
//    The driver passes VN's records only to hoisting, which preserves the inspected nodes
//    and copies records to its expression clones. Other motion queries have no records.
//    Block records are additionally refused if the dominator tree has changed.
//    Matching value numbers would not validate a stale record, since a rewrite can keep a node's
//    value number while changing which check, if any, precedes it.
//
bool Compiler::fgVNGetExceptionDependency(GenTree*                       node,
                                          BasicBlock**                   anchor,
                                          const VNExceptionDependencies& dependencies) const
{
    assert(mostRecentlyActivePhase == PHASE_HOIST_LOOP_CODE);
    if ((dependencies.map == nullptr) || !dependencies.map->Lookup(node, anchor))
    {
        return false;
    }

    assert(*anchor != nullptr);

    return (m_domTree != nullptr) && (m_domTree == dependencies.domTree) && !(*anchor)->HasFlag(BBF_REMOVED);
}

//------------------------------------------------------------------------
// gtNodeMayEstablishProof: Which facts relied on by GTF_ORDER_DOMINATED an operation may establish.
//
// Notes:
//    This must cover every operation at which value numbering records a survived exception
//    (fgVNGetFactAnchor asserts that it does). It is decided by the operation alone, not by its
//    flags or OperExceptions: an operation that can no longer throw still establishes the fact
//    for those after it. LIR motion uses this conservative classification to keep an establishing
//    operation from moving past a dependent operation.
//
Compiler::ProofFacts Compiler::gtNodeMayEstablishProof(GenTree* node)
{
    if (node->IsCall())
    {
        // Receiver null checks, and division helpers.
        return PROOF_ANY;
    }

#ifdef FEATURE_HW_INTRINSICS
    if (node->OperIs(GT_HWINTRINSIC))
    {
        // Memory operations, and vector division.
        return PROOF_ANY;
    }
#endif

    if (node->OperIs(GT_DIV, GT_UDIV, GT_MOD, GT_UMOD))
    {
        return PROOF_DIVISION;
    }

    if (node->OperIsIndirOrArrMetaData() || node->OperIs(GT_INTRINSIC) || node->OperIsAtomicOp())
    {
        return PROOF_NULL;
    }

    return PROOF_NONE;
}

//------------------------------------------------------------------------
// gtNodeOrderingAllowsHoist: Check a node's own ordering constraints when appending
//   it to the preheader. The caller checks its operands and other effects.
//
// The scoped records are supplied only to hoisting, which preserves their validity.
// A fact established anywhere in the preheader holds at its end.
//
bool Compiler::gtNodeOrderingAllowsHoist(GenTree*                       node,
                                         BasicBlock*                    preheader,
                                         const VNExceptionDependencies& dependencies)
{
    if (((node->gtFlags & GTF_ORDER_SIDEEFF) != 0) && node->OperSupportsOrderingSideEffect())
    {
        return false;
    }
    if (!node->HasOwnDominatedOrdering())
    {
        return true;
    }
    BasicBlock* anchor;
    return fgVNGetExceptionDependency(node, &anchor, dependencies) && m_domTree->Dominates(anchor, preheader);
}

//------------------------------------------------------------------------
// OrderingAllowsEarlierEvaluation: Check a tree's ordering constraints when it
//   moves earlier without anchor records. Only inherited constraints can pass;
//   each operand must allow the motion. Other interference is checked by callers.
//
static NOINLINE bool OrderingAllowsEarlierEvaluation(GenTree* tree)
{
    if ((tree->gtFlags & GTF_ORDER_EFFECT) == 0)
    {
        return true;
    }
    if ((((tree->gtFlags & GTF_ORDER_SIDEEFF) != 0) && tree->OperSupportsOrderingSideEffect()) ||
        tree->HasOwnDominatedOrdering())
    {
        return false;
    }
    for (GenTree* operand : tree->Operands())
    {
        if (((operand->gtFlags & GTF_ORDER_EFFECT) != 0) && !OrderingAllowsEarlierEvaluation(operand))
        {
            return false;
        }
    }
    return true;
}

//------------------------------------------------------------------------
// gtOrderingAllowsSwap: Whether dominated orderings allow evaluating 'second' ahead of 'first',
//   two trees evaluated in the same block, the block unknown.
//
// Notes:
//    Only GTF_ORDER_DOMINATED is considered; callers keep their own rules for other effects.
//    'first' moves later, which its dominated orderings allow. 'second' moves earlier, which
//    needs its recorded dependencies; without the block, only inherited ones qualify.
//
bool Compiler::gtOrderingAllowsSwap(GenTree* first, GenTree* second)
{
    // Leaves and effect-free expressions cannot contain a check that second relies on.
    // Do not test GTF_EXCEPT alone: contextual non-faulting operations retain ordering effects.
    if (((second->gtFlags & GTF_ORDER_DOMINATED) == 0) || first->OperIsLeaf() ||
        ((first->gtFlags & GTF_ALL_EFFECT) == 0))
    {
        return true;
    }

    return OrderingAllowsEarlierEvaluation(second);
}
