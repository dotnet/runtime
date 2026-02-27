// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering WASM                                   XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the WebAssembly   XX
XX  architecture.  For a more detailed view of what is lowering, please      XX
XX  take a look at Lower.cpp                                                 XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "lower.h"

//------------------------------------------------------------------------
// IsCallTargetInRange: Can a call target address be encoded in-place?
//
// Return Value:
//    Always true since there are no encoding range considerations on WASM.
//
bool Lowering::IsCallTargetInRange(void* addr)
{
    return true;
}

//------------------------------------------------------------------------
// IsContainableImmed: Is an immediate encodable in-place?
//
// Return Value:
//    True if the immediate can be folded into an instruction,
//    for example small enough and non-relocatable.
//
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode) const
{
    return false;
}

//------------------------------------------------------------------------
// LowerStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Widening small stores (on ARM).
//
// Returns:
//   Next node to lower.
//
GenTree* Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    if (storeLoc->OperIs(GT_STORE_LCL_FLD))
    {
        // We should only encounter this for lclVars that are lvDoNotEnregister.
        verifyLclFldDoNotEnregister(storeLoc->GetLclNum());
    }

    ContainCheckStoreLoc(storeLoc);
    return storeLoc->gtNext;
}

//------------------------------------------------------------------------
// LowerStoreIndir: Determine addressing mode for an indirection, and whether operands are contained.
//
// Arguments:
//    node       - The indirect store node (GT_STORE_IND) of interest
//
// Return Value:
//    Next node to lower.
//
GenTree* Lowering::LowerStoreIndir(GenTreeStoreInd* node)
{
    ContainCheckStoreIndir(node);
    return node->gtNext;
}

//------------------------------------------------------------------------
// LowerMul: Lower a GT_MUL node.
//
// Arguments:
//    mul - The node to lower
//
// Return Value:
//    The next node to lower.
//
GenTree* Lowering::LowerMul(GenTreeOp* mul)
{
    assert(mul->OperIs(GT_MUL));
    ContainCheckMul(mul);
    return mul->gtNext;
}

//------------------------------------------------------------------------
// LowerNeg: Lower a GT_NEG node.
//
// Arguments:
//    node - The node to lower
//
// Return Value:
//    The next node to lower.
//
GenTree* Lowering::LowerNeg(GenTreeOp* node)
{
    if (!node->TypeIs(TYP_INT, TYP_LONG))
    {
        return node->gtNext;
    }

    // For integer types (TYP_INT and TYP_LONG), NEG(x) ==> SUB(0, x)
    //
    GenTree* x    = node->gtGetOp1();
    GenTree* zero = m_compiler->gtNewZeroConNode(node->TypeGet());

    // To preserve stack order we must insert the zero before the entire
    // tree rooted at x.
    //
    GenTree* insertBefore = x->gtFirstNodeInOperandOrder();
    BlockRange().InsertBefore(insertBefore, zero);
    LowerNode(zero);
    node->ChangeOper(GT_SUB);
    node->gtOp1 = zero;
    node->gtOp2 = x;

    return LowerNode(node);
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
//    For wasm we handle all this in codegen
//
GenTree* Lowering::LowerJTrue(GenTreeOp* jtrue)
{
    // TODO-WASM: recognize eqz cases
    return nullptr;
}

//------------------------------------------------------------------------
// LowerBinaryArithmetic: lowers the given binary arithmetic node.
//
// Arguments:
//    node - the arithmetic node to lower
//
// Returns:
//    The next node to lower.
//
GenTree* Lowering::LowerBinaryArithmetic(GenTreeOp* binOp)
{
    ContainCheckBinary(binOp);

    if (binOp->gtOverflow())
    {
        binOp->gtGetOp1()->SetMultiplyUsed();
        binOp->gtGetOp2()->SetMultiplyUsed();
    }

    return binOp->gtNext;
}

//------------------------------------------------------------------------
// LowerDivOrMod: Lowers a GT_[U]DIV/GT_[U]MOD node.
//
// Mark operands that need multiple uses for exception-inducing checks.
//
// Arguments:
//    divMod - the node to be lowered
//
void Lowering::LowerDivOrMod(GenTreeOp* divMod)
{
    ExceptionSetFlags exSetFlags = divMod->OperExceptions(m_compiler);
    if ((exSetFlags & ExceptionSetFlags::ArithmeticException) != ExceptionSetFlags::None)
    {
        divMod->gtGetOp1()->SetMultiplyUsed();
        divMod->gtGetOp2()->SetMultiplyUsed();
    }
    else if ((exSetFlags & ExceptionSetFlags::DivideByZeroException) != ExceptionSetFlags::None)
    {
        divMod->gtGetOp2()->SetMultiplyUsed();
    }

    ContainCheckDivOrMod(divMod);
}

//------------------------------------------------------------------------
// LowerBlockStore: Lower a block store node
//
// Arguments:
//    blkNode - The block store node to lower
//
void Lowering::LowerBlockStore(GenTreeBlk* blkNode)
{
    GenTree* dstAddr = blkNode->Addr();
    GenTree* src     = blkNode->Data();

    if (blkNode->OperIsInitBlkOp())
    {
        if (src->OperIs(GT_INIT_VAL))
        {
            src->SetContained();
            src = src->AsUnOp()->gtGetOp1();
        }

        if (blkNode->IsZeroingGcPointersOnHeap())
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindLoop;
            src->SetContained();
        }
        else
        {
            // memory.fill
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindNativeOpcode;
        }
    }
    else
    {
        assert(src->OperIs(GT_IND, GT_LCL_VAR, GT_LCL_FLD));
        src->SetContained();

        if (src->OperIs(GT_LCL_VAR))
        {
            // TODO-1stClassStructs: for now we can't work with STORE_BLOCK source in register.
            const unsigned srcLclNum = src->AsLclVar()->GetLclNum();
            m_compiler->lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(DoNotEnregisterReason::StoreBlkSrc));
        }

        ClassLayout* layout  = blkNode->GetLayout();
        bool         doCpObj = layout->HasGCPtr();

        // If copying to the stack instead of the heap, we should treat it as a raw memcpy for
        //  smaller generated code and potentially better performance.
        if (blkNode->IsAddressNotOnHeap(m_compiler))
            doCpObj = false;

        // CopyObj or CopyBlk
        if (doCpObj)
        {
            // Try to use bulk copy helper
            if (TryLowerBlockStoreAsGcBulkCopyCall(blkNode))
            {
                return;
            }

            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindCpObjUnroll;
            dstAddr->SetMultiplyUsed();
            if (src->OperIs(GT_IND))
                src->gtGetOp1()->SetMultiplyUsed();
        }
        else
        {
            assert(blkNode->OperIs(GT_STORE_BLK));
            // memory.copy
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindNativeOpcode;
        }
    }
}

//------------------------------------------------------------------------
// LowerPutArgStk: Lower a GT_PUTARG_STK.
//
// Arguments:
//    putArgStk - The node to lower
//
void Lowering::LowerPutArgStk(GenTreePutArgStk* putArgNode)
{
    unreached(); // Currently no stack args on WASM.
}

//------------------------------------------------------------------------
// LowerCast: Lower GT_CAST(srcType, DstType) nodes.
//
// Arguments:
//    tree - GT_CAST node to be lowered
//
// Return Value:
//    None.
//
void Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperIs(GT_CAST));

    if (tree->gtOverflow())
    {
        tree->gtGetOp1()->SetMultiplyUsed();
    }
    ContainCheckCast(tree->AsCast());
}

//------------------------------------------------------------------------
// LowerRotate: Lower GT_ROL and GT_ROR nodes.
//
// Arguments:
//    tree - the node to lower
//
// Return Value:
//    None.
//
void Lowering::LowerRotate(GenTree* tree)
{
    ContainCheckShiftRotate(tree->AsOp());
}

//------------------------------------------------------------------------
// ContainCheckCallOperands: Determine whether operands of a call should be contained.
//
// Arguments:
//    call       - The call node of interest
//
// Return Value:
//    None.
//
void Lowering::ContainCheckCallOperands(GenTreeCall* call)
{
}

//------------------------------------------------------------------------
// ContainCheckStoreIndir: determine whether the sources of a STOREIND node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
    ContainCheckIndir(node);
}

//------------------------------------------------------------------------
// ContainCheckIndir: Determine whether operands of an indir should be contained.
//
// Arguments:
//    indirNode - The indirection node of interest
//
// Notes:
//    This is called for both store and load indirections.
//
// Return Value:
//    None.
//
void Lowering::ContainCheckIndir(GenTreeIndir* indirNode)
{
    // If this is the rhs of a block copy it will be handled when we handle the store.
    if (indirNode->TypeIs(TYP_STRUCT))
    {
        return;
    }

    // TODO-WASM-CQ: contain suitable LEAs here. Take note of the fact that for this to be correct we must prove the
    // LEA doesn't overflow. It will involve creating a new frontend node to represent "nuw" (offset) addition.
}

//------------------------------------------------------------------------
// ContainCheckBinary: Determine whether a binary op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckBinary(GenTreeOp* node)
{
}

//------------------------------------------------------------------------
// ContainCheckMul: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckMul(GenTreeOp* node)
{
}

//------------------------------------------------------------------------
// ContainCheckDivOrMod: determine which operands of a div/mod should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckDivOrMod(GenTreeOp* node)
{
}

//------------------------------------------------------------------------
// ContainCheckShiftRotate: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckShiftRotate(GenTreeOp* node)
{
}

//------------------------------------------------------------------------
// ContainCheckStoreLoc: determine whether the source of a STORE_LCL* should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreLoc(GenTreeLclVarCommon* storeLoc) const
{
}

//------------------------------------------------------------------------
// ContainCheckCast: determine whether the source of a CAST node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCast(GenTreeCast* node)
{
    // TODO-WASM-CQ: do containment for casts which can be expressed in terms of memory loads.
}

//------------------------------------------------------------------------
// ContainCheckCompare: determine whether the sources of a compare node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCompare(GenTreeOp* cmp)
{
    // TODO-WASM-CQ: do containment for [i32|i64].eqz.
}

//------------------------------------------------------------------------
// ContainCheckSelect: determine whether the source of a select should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckSelect(GenTreeOp* node)
{
}

//------------------------------------------------------------------------
// AfterLowerBlock: stackify the nodes in this block.
//
// Stackification involves moving nodes around and inserting temporaries
// as necessary. We expect the vast majority of IR to already be in correct
// shape as our frontend is itself tree-based.
//
// It is done right after lowering, thus before RA and liveness, so that
// the introduced temporaries can get enregistered and the last-use info
// on LCL_VAR nodes in RA is readily correct.
//
void Lowering::AfterLowerBlock()
{
    class Stackifier
    {
        Lowering* m_lower;
        bool      m_anyChanges = false;

    public:
        Stackifier(Lowering* lower)
            : m_lower(lower)
        {
        }

        void StackifyCurrentBlock()
        {
            GenTree* node = m_lower->BlockRange().LastNode();
            while (node != nullptr)
            {
                assert(IsDataFlowRoot(node));
                node = StackifyTree(node);
            }

            if (!m_anyChanges)
            {
                JITDUMP(FMT_BB ": already in WASM value stack order\n", m_lower->m_block->bbNum);
            }
        }

        GenTree* StackifyTree(GenTree* root)
        {
            ArrayStack<GenTree*>* stack        = &m_lower->m_stackificationStack;
            int                   initialDepth = stack->Height();

            // Simple greedy algorithm working backwards. The invariant is that the stack top must be placed right next
            // to (in normal linear order - before) the node we last stackified.
            stack->Push(root);
            GenTree* current = root->gtNext;
            while (stack->Height() != initialDepth)
            {
                GenTree* node = stack->Pop();
                GenTree* prev = (current != nullptr) ? current->gtPrev : root;
                while (node != prev)
                {
                    // Maybe this is an intervening void-equivalent node that we can also just stackify.
                    if (IsDataFlowRoot(prev))
                    {
                        prev = StackifyTree(prev);
                        continue;
                    }

                    // At this point, we'll have to modify the IR in some way. In general, these cases should be quite
                    // rare, introduced in lowering only. All HIR-induced cases (such as from "gtSetEvalOrder") should
                    // instead be ifdef-ed out for WASM.
                    m_anyChanges = true;

                    // Invariant nodes can be safely moved by the stackifier with no side effects.
                    // For other nodes, the side effects would require us to turn them into a temporary local, but this
                    //  is not possible for contained nodes like an IND inside a STORE_BLK. However, the few types of
                    //  contained nodes we have in Wasm should be safe to move freely since the lack of 'dup' or
                    //  persistent registers in Wasm means that the actual codegen will trigger the side effect(s) and
                    //  store the result into a Wasm local for any later uses during the containing node's execution,
                    //  i.e. cpobj where the src and dest get stashed at the start and then used as add operands
                    //  repeatedly.
                    // Locals can also be safely moved as long as they aren't address-exposed due to local var nodes
                    // being
                    //  implicitly pseudo-contained.
                    // TODO-WASM: Verify that it is actually safe to do this for all contained nodes.
                    if (
                        m_lower->IsInvariantInRange(node, prev->gtNext) ||
                        node->isContained() ||
                        (node->OperIs(GT_LCL_VAR) &&
                         !m_lower->m_compiler->lvaGetDesc(node->AsLclVarCommon())->IsAddressExposed()))
                    {
                        JITDUMP("Stackifier moving node [%06u] after [%06u]\n", Compiler::dspTreeID(node),
                                Compiler::dspTreeID(prev));
                        m_lower->BlockRange().Remove(node);
                        m_lower->BlockRange().InsertAfter(prev, node);
                        break;
                    }

                    /*
                    else
                    {
                        // To resolve this scenario we have two options:
                        // 1. We try moving the whole tree rooted at `node`.
                        //    To avoid quadratic behavior, we first stackify it and collect all the side effects
                        //    from it. Then we check for interference of those side effects with nodes between
                        //    'node' and 'prev'.
                        // 2. Failing that, we insert a temporary ('ReplaceWithLclVar') for 'node'.
                        //    To avoid explosion of temporaries, we maintain a busy/free set of them.
                        // For now, for simplicity we are implementing #2 only.

                        LIR::Use nodeUse;
                        // FIXME-WASM: TryGetUse is inefficient here, replace it with something more optimal
                        if (!m_lower->BlockRange().TryGetUse(node, &nodeUse))
                        {
                            JITDUMP("node==[%06u] prev==[%06u]\n", Compiler::dspTreeID(node),
                    Compiler::dspTreeID(prev)); NYI_WASM("Could not get a LIR::Use for the node to be moved by the
                    stackifier");
                        }

                        unsigned lclNum = nodeUse.ReplaceWithLclVar(m_lower->m_compiler);
                        GenTree* newNode = nodeUse.Def();
                        JITDUMP("Stackifier replaced node [%06u] with lcl var %u\n", Compiler::dspTreeID(node), lclNum);
                        m_lower->BlockRange().Remove(newNode);
                        m_lower->BlockRange().InsertAfter(prev, newNode);
                        JITDUMP("Stackifier moved new node [%06u] after [%06u]\n", Compiler::dspTreeID(newNode),
                    Compiler::dspTreeID(prev)); break;
                    }
                    */

                    JITDUMP("node==[%06u] prev==[%06u]\n", Compiler::dspTreeID(node), Compiler::dspTreeID(prev));
                    NYI_WASM("IR not in a stackified form");
                }

                // In stack order, the last operand is closest to its parent, thus put on top here.
                node->VisitOperands([stack](GenTree* operand) {
                    stack->Push(operand);
                    return GenTree::VisitResult::Continue;
                });
                current = node;
            }

            return current->gtPrev;
        }

        bool IsDataFlowRoot(GenTree* node)
        {
            return !node->IsValue() || node->IsUnusedValue();
        }
    };

    Stackifier stackifier(this);
    stackifier.StackifyCurrentBlock();
}

//------------------------------------------------------------------------
// AfterLowerArgsForCall: post processing after call args are lowered
//
// Arguments:
//    call - Call node
//
void Lowering::AfterLowerArgsForCall(GenTreeCall* call)
{
    if (call->NeedsNullCheck())
    {
        // Prepare for explicit null check
        CallArg* thisArg = call->gtArgs.GetThisArg();
        thisArg->GetNode()->SetMultiplyUsed();
    }
}
