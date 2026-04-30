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

static void SetMultiplyUsed(GenTree* node DEBUGARG(const char* reason))
{
    JITDUMP("Setting [%06u] as multiply-used: %s\n", Compiler::dspTreeID(node), reason);
    assert(varTypeIsEnregisterable(node));
    assert(!node->isContained());
    node->gtLIRFlags |= LIR::Flags::MultiplyUsed;
}

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

//---------------------------------------------------------------------------------------------
// LowerPEPCall: Lower a call node dispatched through a PortableEntryPoint (PEP)
//
// Given a call node with gtControlExpr representing a call target which is the address of a portable entrypoint,
// this function lowers the call to appropriately dispatch through the portable entrypoint using the Portable
// entrypoint calling convention.
// To do this, it:
//      1. Introduces a new local variable to hold the PEP address
//      2. Adds a new well-known argument to the call passing this local
//      3. Rewrites the control expression to indirect through the new local, since for PEP's, the actual call target
//         must be loaded from the portable entry point address.
//
// Arguments:
//    call         -  The call node to lower. It is expected that the call node has gtControlExpr set to the original
//                      call target and that the call does not have a PEP arg already.
//
// Return Value:
//    None. The call node is modified in place.
//
void Lowering::LowerPEPCall(GenTreeCall* call)
{
    JITDUMP("Begin lowering PEP call\n");
    DISPTREERANGE(BlockRange(), call);

    // PEP call must always have a control expression
    assert(call->gtControlExpr != nullptr);
    LIR::Use callTargetUse(BlockRange(), &call->gtControlExpr, call);

    JITDUMP("Creating new local variable for PEP");
    unsigned int   callTargetLclNum    = callTargetUse.ReplaceWithLclVar(m_compiler);
    GenTreeLclVar* callTargetLclForArg = m_compiler->gtNewLclvNode(callTargetLclNum, TYP_I_IMPL);
    DISPTREE(call);

    JITDUMP("Add new arg to call arg list corresponding to PEP target");
    NewCallArg pepTargetArg =
        NewCallArg::Primitive(callTargetLclForArg).WellKnown(WellKnownArg::WasmPortableEntryPoint);
    CallArg* pepArg = call->gtArgs.PushBack(m_compiler, pepTargetArg);

    pepArg->SetEarlyNode(nullptr);
    pepArg->SetLateNode(callTargetLclForArg);
    call->gtArgs.PushLateBack(pepArg);

    // Set up ABI information for this arg; PEP's should be passed as the last param to a wasm function
    unsigned  pepIndex = call->gtArgs.CountArgs() - 1;
    regNumber pepReg   = MakeWasmReg(pepIndex, WasmValueType::I);
    pepArg->AbiInfo =
        ABIPassingInformation::FromSegmentByValue(m_compiler,
                                                  ABIPassingSegment::InRegister(pepReg, 0, TARGET_POINTER_SIZE));
    BlockRange().InsertBefore(call, callTargetLclForArg);

    // Lower the new PEP arg now that the call abi info is updated and lcl var is inserted
    LowerArg(call, pepArg);
    DISPTREE(call);

    JITDUMP("Rewrite PEP call's control expression to indirect through the new local variable\n");
    // Rewrite the call's control expression to have an additional load from the PEP local
    GenTree* controlExpr = call->gtControlExpr;
    GenTree* target      = Ind(controlExpr);
    BlockRange().InsertAfter(controlExpr, target);
    call->gtControlExpr = target;

    JITDUMP("Finished lowering PEP call\n");
    DISPTREERANGE(BlockRange(), call);
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
    if ((node->gtFlags & GTF_IND_NONFAULTING) == 0)
    {
        // We need to be able to null check the address, and that requires multiple uses of the address operand.
        SetMultiplyUsed(node->Addr() DEBUGARG("LowerStoreIndir faulting Addr"));
    }

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
    LowerBinaryArithmetic(mul);
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

    if (binOp->gtOverflowEx())
    {
        SetMultiplyUsed(binOp->gtGetOp1() DEBUGARG("LowerBinaryArithmetic op1 (overflow exception)"));
        SetMultiplyUsed(binOp->gtGetOp2() DEBUGARG("LowerBinaryArithmetic op2 (overflow exception)"));
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
        SetMultiplyUsed(divMod->gtGetOp1() DEBUGARG("LowerDivOrMod op1 (arithmetic exception)"));
        SetMultiplyUsed(divMod->gtGetOp2() DEBUGARG("LowerDivOrMod op2 (arithmetic exception)"));
    }
    else if ((exSetFlags & ExceptionSetFlags::DivideByZeroException) != ExceptionSetFlags::None)
    {
        SetMultiplyUsed(divMod->gtGetOp2() DEBUGARG("LowerDivOrMod op2 (divide by zero exception)"));
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
        {
            doCpObj = false;
        }

        // CopyObj or CopyBlk
        if (doCpObj)
        {
            // Try to use bulk copy helper
            if (TryLowerBlockStoreAsGcBulkCopyCall(blkNode))
            {
                return;
            }

            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindCpObjUnroll;
        }
        else
        {
            assert(blkNode->OperIs(GT_STORE_BLK));
            // memory.copy
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindNativeOpcode;
        }

        if (src->OperIs(GT_IND))
        {
            GenTree* srcAddr = src->gtGetOp1();
            if ((blkNode->gtBlkOpKind != GenTreeBlk::BlkOpKindNativeOpcode) ||
                ((src->gtFlags & GTF_IND_NONFAULTING) == 0))
            {
                SetMultiplyUsed(srcAddr DEBUGARG("LowerBlockStore source address (indirection)"));
            }
        }
    }

    if (((blkNode->gtBlkOpKind != GenTreeBlk::BlkOpKindNativeOpcode) ||
         ((blkNode->gtFlags & GTF_IND_NONFAULTING) == 0)))
    {
        SetMultiplyUsed(dstAddr DEBUGARG("LowerBlockStore destination address"));
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
        SetMultiplyUsed(tree->gtGetOp1() DEBUGARG("LowerCast op1 (overflow exception)"));
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
// LowerIndexAddr: Lowers a GT_INDEX_ADDR node
//
// Mark operands that need multiple uses for exception-inducing checks.
//
// Arguments:
//    indexAddr - the node to be lowered
//
void Lowering::LowerIndexAddr(GenTreeIndexAddr* indexAddr)
{
    if (indexAddr->IsBoundsChecked())
    {
        SetMultiplyUsed(indexAddr->Arr() DEBUGARG("LowerIndexAddr Arr"));
        SetMultiplyUsed(indexAddr->Index() DEBUGARG("LowerIndexAddr Index"));
    }
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
// ContainCheckNonLocalJmp:
//   No-op for wasm.
//
// Arguments:
//    node - The GT_NONLOCAL_JMP node.
//
void Lowering::ContainCheckNonLocalJmp(GenTreeUnOp* node)
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
// AfterLowerBlocks: stackify the nodes in all blocks.
//
// Stackification involves moving nodes around and inserting temporaries
// as necessary. We expect the vast majority of IR to already be in correct
// shape as our frontend is itself tree-based.
//
// It is done right after lowering, thus before RA and liveness, so that
// the introduced temporaries can get enregistered and the last-use info
// on LCL_VAR nodes in RA is readily correct.
//
void Lowering::AfterLowerBlocks()
{
    struct Temporary
    {
        unsigned   LclNum;
        Temporary* Prev = nullptr;
    };

    class Stackifier
    {
        Lowering*             m_lower;
        Compiler*             m_compiler;
        ArrayStack<GenTree**> m_stack;
        unsigned              m_minimumTempLclNum;
        Temporary*            m_availableTemps[TYP_COUNT] = {};
        Temporary*            m_inUseTemps[TYP_COUNT]     = {};
        bool                  m_anyChanges                = false;

    public:
        Stackifier(Lowering* lower)
            : m_lower(lower)
            , m_compiler(lower->m_compiler)
            , m_stack(m_compiler->getAllocator(CMK_Lower))
            , m_minimumTempLclNum(m_compiler->lvaCount)
        {
        }

        void StackifyBlock(BasicBlock* block)
        {
            m_anyChanges     = false;
            m_lower->m_block = block;
            GenTree* node    = block->lastNode();
            while (node != nullptr)
            {
                assert(IsDataFlowRoot(node));
                node = StackifyTree(node);
                // We don't track liveness of temporaries more precisely since introducing earlier uses
                // may interfere with later (by that point already inserted and stackified) stores.
                ReleaseTemporaries();
            }
            m_lower->m_block = nullptr;

            JITDUMP(FMT_BB ": %s\n", block->bbNum,
                    m_anyChanges ? "stackified with some changes" : "already in WASM value stack order");
        }

        GenTree* StackifyTree(GenTree* root)
        {
            int initialDepth = m_stack.Height();

            // Simple greedy algorithm working backwards. The invariant is that the stack top must be placed right next
            // to (in normal linear order - before) the node we last stackified.
            m_stack.Push(&root);

            GenTree* lastStackified = root->gtNext;
            while (m_stack.Height() != initialDepth)
            {
                GenTree** use  = m_stack.Pop();
                GenTree*  node = *use;
                GenTree*  prev = (lastStackified != nullptr) ? lastStackified->gtPrev : root;
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
                    INDEBUG(const char* reason);
                    if (CanMoveForward(node DEBUGARG(&reason)))
                    {
                        MoveForward(node, prev DEBUGARG(reason));
                    }
                    else
                    {
                        node = ReplaceWithTemporary(use, prev);
                    }
                    m_anyChanges = true;
                    break;
                }

                // In stack order, the last operand is closest to its parent, thus put on top here.
                node->VisitOperandUses([this](GenTree** use) {
                    m_stack.Push(use);
                    return GenTree::VisitResult::Continue;
                });
                lastStackified = node;
            }

            return lastStackified->gtPrev;
        }

        bool IsDataFlowRoot(GenTree* node)
        {
            return !node->IsValue() || node->IsUnusedValue();
        }

        bool CanMoveForward(GenTree* node DEBUGARG(const char** pReason))
        {
            if (node->IsInvariant())
            {
                // Leaf node without control or dataflow dependencies.
                INDEBUG(*pReason = "invariant");
                return true;
            }

            if (node->isContained())
            {
                // Contained nodes are part of their parent so their position in the LIR stream in not significant.
                // As a fiction that simplifies this algorithm, we move them to the place where they would be were
                // they not contained.
                INDEBUG(*pReason = "contained");
                return true;
            }

            if (node->OperIs(GT_LCL_VAR) && !m_compiler->lvaGetDesc(node->AsLclVarCommon())->IsAddressExposed())
            {
                // By IR invariants, there can be no intervening stores between a local's position in the LIR stream
                // and its parent. So we can always move a local forward, closer to its parent.
                INDEBUG(*pReason = "local");
                return true;
            }

            // TODO-WASM: devise a less-than-quadratic (ideally linear) algorithm that would allow us to handle more
            // complex cases here.
            return false;
        }

        void MoveForward(GenTree* node, GenTree* prev DEBUGARG(const char* reason))
        {
            JITDUMP("Stackifier moving [%06u] after [%06u]: %s\n", Compiler::dspTreeID(node), Compiler::dspTreeID(prev),
                    reason);
            assert(m_lower->IsInvariantInRange(node, prev->gtNext));
            m_lower->BlockRange().Remove(node);
            m_lower->BlockRange().InsertAfter(prev, node);
        }

        GenTree* ReplaceWithTemporary(GenTree** use, GenTree* prev)
        {
            GenTree* node     = *use;
            unsigned lclNum   = RequestTemporary(node->TypeGet());
            GenTree* lclStore = m_compiler->gtNewStoreLclVarNode(lclNum, node);
            GenTree* lclNode  = m_compiler->gtNewLclVarNode(lclNum);

            m_lower->BlockRange().InsertAfter(node, lclStore);
            m_lower->BlockRange().InsertAfter(prev, lclNode);
            *use = lclNode;

            JITDUMP("Replaced [%06u] with a temporary:\n", Compiler::dspTreeID(node));
            DISPNODE(node);
            DISPNODE(lclNode);

            if ((node->gtLIRFlags & LIR::Flags::MultiplyUsed) == LIR::Flags::MultiplyUsed)
            {
                JITDUMP("Transferring multiply-used flag from old node to new temporary.\n");
                node->gtLIRFlags &= ~LIR::Flags::MultiplyUsed;
                SetMultiplyUsed(lclNode DEBUGARG("Transferred flag during stackification"));
            }

            return lclNode;
        }

        unsigned RequestTemporary(var_types type)
        {
            assert(varTypeIsEnregisterable(type));

            unsigned   lclNum;
            Temporary* local = Remove(&m_availableTemps[genActualType(type)]);
            if (local != nullptr)
            {
                lclNum = local->LclNum;
                assert(m_compiler->lvaGetDesc(lclNum)->TypeGet() == genActualType(type));
            }
            else
            {
                lclNum = m_compiler->lvaGrabTemp(true DEBUGARG("Stackifier temporary"));
                assert(lclNum >= m_minimumTempLclNum);
                LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
                varDsc->lvType    = genActualType(type);

                // Allocate a new temporary to describe this local
                local         = new (m_compiler, CMK_Lower) Temporary();
                local->LclNum = lclNum;
            }
            Append(&m_inUseTemps[genActualType(type)], local);

            JITDUMP("Temporary V%02u is now in use\n", lclNum);
            return lclNum;
        }

        void ReleaseTemporaries()
        {
            if (m_minimumTempLclNum == m_compiler->lvaCount)
            {
                // No temporaries were created
                return;
            }
            assert(m_minimumTempLclNum < m_compiler->lvaCount);

            JITDUMP("Releasing stackifier temporaries:\n");
            // Reclaim all in-use temporaries
            for (int i = 0; i < TYP_COUNT; i++)
            {
                while (m_inUseTemps[i] != nullptr)
                {
                    Temporary* temp = Remove(&m_inUseTemps[i]);
                    assert(temp->LclNum >= m_minimumTempLclNum);
                    Append(&m_availableTemps[i], temp);
                    JITDUMP("Temporary V%02u is now available\n", temp->LclNum);
                }
            }
        }

        Temporary* Remove(Temporary** pTemps)
        {
            Temporary* local = *pTemps;
            if (local != nullptr)
            {
                *pTemps = local->Prev;
            }
            return local;
        }

        void Append(Temporary** pTemps, Temporary* local)
        {
            local->Prev = *pTemps;
            *pTemps     = local;
        }
    };

    Stackifier stackifier(this);
    for (BasicBlock* block : m_compiler->Blocks())
    {
        stackifier.StackifyBlock(block);
    }
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
        SetMultiplyUsed(thisArg->GetNode() DEBUGARG("AfterLowerArgsForCall thisArg (null check)"));
    }
}
