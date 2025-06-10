// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX             Lowering for RISCV64 common code                              XX
XX                                                                           XX
XX  This encapsulates common logic for lowering trees for the RISCV64        XX
XX  architectures.  For a more detailed view of what is lowering, please     XX
XX  take a look at Lower.cpp                                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_RISCV64 // This file is ONLY used for RISCV64 architectures

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

#ifdef FEATURE_HW_INTRINSICS
#include "hwintrinsic.h"
#endif

//------------------------------------------------------------------------
// IsCallTargetInRange: Can a call target address be encoded in-place?
//
// Return Value:
//    True if the addr fits into the range.
//
bool Lowering::IsCallTargetInRange(void* addr)
{
    // TODO-RISCV64: using B/BL for optimization.
    return false;
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
    if (!varTypeIsFloating(parentNode->TypeGet()))
    {
        // Make sure we have an actual immediate
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->AsIntCon()->ImmedValNeedsReloc(comp))
            return false;

        // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had target_ssize_t type.
        target_ssize_t immVal = (target_ssize_t)childNode->AsIntCon()->gtIconVal;

        switch (parentNode->OperGet())
        {
            case GT_EQ:
            case GT_NE:
                return emitter::isValidSimm12(-immVal) || (immVal == -2048);

            case GT_LE: // a <= N  ->  a < N+1
            case GT_GT: // a > N  ->  !(a <= N)  ->  !(a < N+1)
                immVal += 1;
                FALLTHROUGH;
            case GT_LT:
            case GT_GE:
            case GT_ADD:
            case GT_AND:
            case GT_OR:
            case GT_XOR:
                return emitter::isValidSimm12(immVal);
            case GT_JCMP:
                return true;

            case GT_CMPXCHG:
            case GT_XORR:
            case GT_XAND:
            case GT_XADD:
            case GT_XCHG:
            case GT_STORE_LCL_FLD:
            case GT_STORE_LCL_VAR:
                return (immVal == 0);

            default:
                break;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// LowerMul: Lower a GT_MUL/GT_MULHI/GT_MUL_LONG node.
//
// Performs contaiment checks.
//
// TODO-RISCV64-CQ: recognize GT_MULs that can be turned into MUL_LONGs,
// as those are cheaper.
//
// Arguments:
//    mul - The node to lower
//
// Return Value:
//    The next node to lower.
//
GenTree* Lowering::LowerMul(GenTreeOp* mul)
{
    assert(mul->OperIsMul());

    ContainCheckMul(mul);

    return mul->gtNext;
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
GenTree* Lowering::LowerJTrue(GenTreeOp* jtrue)
{
    GenTree*     op = jtrue->gtGetOp1();
    GenCondition cond;
    GenTree*     cmpOp1;
    GenTree*     cmpOp2;

    if (op->OperIsCompare() && !varTypeIsFloating(op->gtGetOp1()))
    {
        // We do not expect any other relops on RISCV64
        assert(op->OperIs(GT_EQ, GT_NE, GT_LT, GT_LE, GT_GE, GT_GT));

        cond = GenCondition::FromRelop(op);

        cmpOp1 = op->gtGetOp1();
        cmpOp2 = op->gtGetOp2();

        // We will fall through and turn this into a JCMP(op1, op2, kind), but need to remove the relop here.
        BlockRange().Remove(op);
    }
    else
    {
        cond = GenCondition(GenCondition::NE);

        cmpOp1 = op;
        cmpOp2 = comp->gtNewZeroConNode(cmpOp1->TypeGet());

        BlockRange().InsertBefore(jtrue, cmpOp2);

        // Fall through and turn this into a JCMP(op1, 0, NE).
    }

    // for RISCV64's compare and condition-branch instructions,
    // it's very similar to the IL instructions.
    jtrue->ChangeOper(GT_JCMP);
    jtrue->gtOp1                 = cmpOp1;
    jtrue->gtOp2                 = cmpOp2;
    jtrue->AsOpCC()->gtCondition = cond;

    if (cmpOp2->IsCnsIntOrI())
    {
        cmpOp2->SetContained();
    }

    return jtrue->gtNext;
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
    if (comp->opts.OptimizationEnabled())
    {
        GenTree*& op1 = binOp->gtOp1;
        GenTree*& op2 = binOp->gtOp2;

        bool isOp1Negated = op1->OperIs(GT_NOT);
        bool isOp2Negated = op2->OperIs(GT_NOT);
        if (binOp->OperIs(GT_AND, GT_OR, GT_XOR) && (isOp1Negated || isOp2Negated))
        {
            if ((isOp1Negated && isOp2Negated) || comp->compOpportunisticallyDependsOn(InstructionSet_Zbb))
            {
                if (isOp1Negated)
                {
                    BlockRange().Remove(op1);
                    op1 = op1->AsUnOp()->gtGetOp1();
                }
                if (isOp2Negated)
                {
                    BlockRange().Remove(op2);
                    op2 = op2->AsUnOp()->gtGetOp1();
                }

                if (isOp1Negated != isOp2Negated)
                {
                    assert(comp->compOpportunisticallyDependsOn(InstructionSet_Zbb));
                    if (isOp1Negated)
                        std::swap(op1, op2);

                    genTreeOps operNot = GT_NONE;
                    switch (binOp->OperGet())
                    {
                        case GT_AND:
                            operNot = GT_AND_NOT;
                            break;
                        case GT_OR:
                            operNot = GT_OR_NOT;
                            break;
                        default:
                            assert(binOp->OperIs(GT_XOR));
                            operNot = GT_XOR_NOT;
                            break;
                    }
                    binOp->ChangeOper(operNot);
                }
                else if (binOp->OperIs(GT_AND, GT_OR)) // XOR is good after negation removal, (~a ^ ~b) == (a ^ b)
                {
                    assert(isOp1Negated && isOp2Negated);
                    LIR::Use use;
                    if (BlockRange().TryGetUse(binOp, &use))
                    {
                        // (~a | ~b) == ~(a & b),  (~a & ~b) == ~(a | b)
                        genTreeOps reverseOper = binOp->OperIs(GT_AND) ? GT_OR : GT_AND;
                        binOp->ChangeOper(reverseOper);

                        GenTreeUnOp* negation = comp->gtNewOperNode(GT_NOT, binOp->gtType, binOp);
                        BlockRange().InsertAfter(binOp, negation);
                        use.ReplaceWith(negation);
                    }
                    else
                    {
                        binOp->SetUnusedValue();
                    }
                }
            }
        }
    }

    ContainCheckBinary(binOp);

    return binOp->gtNext;
}

//------------------------------------------------------------------------
// LowerStoreLoc: Lower a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Widening operations of unsigneds.
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
// LowerBlockStore: Set block store type
//
// Arguments:
//    blkNode       - The block store node of interest
//
// Return Value:
//    None.
//
void Lowering::LowerBlockStore(GenTreeBlk* blkNode)
{
    GenTree* dstAddr = blkNode->Addr();
    GenTree* src     = blkNode->Data();
    unsigned size    = blkNode->Size();

    if (blkNode->OperIsInitBlkOp())
    {
        if (src->OperIs(GT_INIT_VAL))
        {
            src->SetContained();
            src = src->AsUnOp()->gtGetOp1();
        }

        if ((size <= comp->getUnrollThreshold(Compiler::UnrollKind::Memset)) && src->OperIs(GT_CNS_INT))
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;

            // The fill value of an initblk is interpreted to hold a
            // value of (unsigned int8) however a constant of any size
            // may practically reside on the evaluation stack. So extract
            // the lower byte out of the initVal constant and replicate
            // it to a larger constant whose size is sufficient to support
            // the largest width store of the desired inline expansion.

            ssize_t fill = src->AsIntCon()->IconValue() & 0xFF;
            if (fill == 0)
            {
                src->SetContained();
            }
            else if (size >= REGSIZE_BYTES)
            {
                fill *= 0x0101010101010101LL;
                src->gtType = TYP_LONG;
            }
            else
            {
                fill *= 0x01010101;
            }
            src->AsIntCon()->SetIconValue(fill);

            ContainBlockStoreAddress(blkNode, size, dstAddr, nullptr);
        }
        else if (blkNode->IsZeroingGcPointersOnHeap())
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindLoop;
            // We're going to use REG_R0 for zero
            src->SetContained();
        }
        else
        {
            LowerBlockStoreAsHelperCall(blkNode);
            return;
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
            comp->lvaSetVarDoNotEnregister(srcLclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
        }

        ClassLayout* layout               = blkNode->GetLayout();
        bool         doCpObj              = layout->HasGCPtr();
        unsigned     copyBlockUnrollLimit = comp->getUnrollThreshold(Compiler::UnrollKind::Memcpy);

        if (doCpObj && (size <= copyBlockUnrollLimit))
        {
            // No write barriers are needed on the stack.
            // If the layout contains a byref, then we know it must live on the stack.
            if (blkNode->IsAddressNotOnHeap(comp))
            {
                // If the size is small enough to unroll then we need to mark the block as non-interruptible
                // to actually allow unrolling. The generated code does not report GC references loaded in the
                // temporary register(s) used for copying.
                doCpObj                  = false;
                blkNode->gtBlkOpGcUnsafe = true;
            }
        }

        // CopyObj or CopyBlk
        if (doCpObj)
        {
            // Try to use bulk copy helper
            if (TryLowerBlockStoreAsGcBulkCopyCall(blkNode))
            {
                return;
            }

            assert(dstAddr->TypeIs(TYP_BYREF, TYP_I_IMPL));
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindCpObjUnroll;
        }
        else if (blkNode->OperIs(GT_STORE_BLK) && (size <= copyBlockUnrollLimit))
        {
            blkNode->gtBlkOpKind = GenTreeBlk::BlkOpKindUnroll;

            if (src->OperIs(GT_IND))
            {
                ContainBlockStoreAddress(blkNode, size, src->AsIndir()->Addr(), src->AsIndir());
            }

            ContainBlockStoreAddress(blkNode, size, dstAddr, nullptr);
        }
        else
        {
            assert(blkNode->OperIs(GT_STORE_BLK));
            LowerBlockStoreAsHelperCall(blkNode);
        }
    }
}

//------------------------------------------------------------------------
// ContainBlockStoreAddress: Attempt to contain an address used by an unrolled block store.
//
// Arguments:
//    blkNode - the block store node
//    size - the block size
//    addr - the address node to try to contain
//    addrParent - the parent of addr, in case this is checking containment of the source address.
//
void Lowering::ContainBlockStoreAddress(GenTreeBlk* blkNode, unsigned size, GenTree* addr, GenTree* addrParent)
{
    assert(blkNode->OperIs(GT_STORE_BLK) && (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll));
    assert(size < INT32_MAX);

    if (addr->OperIs(GT_LCL_ADDR) && IsContainableLclAddr(addr->AsLclFld(), size))
    {
        addr->SetContained();
        return;
    }

    if (!addr->OperIs(GT_ADD) || addr->gtOverflow() || !addr->AsOp()->gtGetOp2()->OperIs(GT_CNS_INT))
    {
        return;
    }

    GenTreeIntCon* offsetNode = addr->AsOp()->gtGetOp2()->AsIntCon();
    ssize_t        offset     = offsetNode->IconValue();

    // TODO-RISCV64: not including the ldptr and SIMD offset which not used right now.
    if (!emitter::isValidSimm12(offset) || !emitter::isValidSimm12(offset + static_cast<int>(size)))
    {
        return;
    }

    if (!IsSafeToContainMem(blkNode, addrParent, addr))
    {
        return;
    }

    BlockRange().Remove(offsetNode);

    addr->ChangeOper(GT_LEA);
    addr->AsAddrMode()->SetIndex(nullptr);
    addr->AsAddrMode()->SetScale(0);
    addr->AsAddrMode()->SetOffset(static_cast<int>(offset));
    addr->SetContained();
}

//------------------------------------------------------------------------
// LowerPutArgStk: Lower a GT_PUTARG_STK.
//
// Arguments:
//    putArgNode - The node to lower
//
void Lowering::LowerPutArgStk(GenTreePutArgStk* putArgNode)
{
    GenTree* src = putArgNode->Data();

    if (src->TypeIs(TYP_STRUCT))
    {
        // STRUCT args (FIELD_LIST / BLK / LCL_VAR / LCL_FLD) will always be contained.
        MakeSrcContained(putArgNode, src);

        if (src->OperIs(GT_LCL_VAR))
        {
            // TODO-1stClassStructs: support struct enregistration here by retyping "src" to its register type for
            // the non-split case.
            comp->lvaSetVarDoNotEnregister(src->AsLclVar()->GetLclNum() DEBUGARG(DoNotEnregisterReason::IsStructArg));
        }
    }
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

    JITDUMP("LowerCast for: ");
    DISPNODE(tree);
    JITDUMP("\n");

    GenTree*  op1     = tree->AsOp()->gtOp1;
    var_types dstType = tree->CastToType();
    var_types srcType = genActualType(op1->TypeGet());

    if (varTypeIsFloating(srcType))
    {
        // Overflow casts should have been converted to helper call in morph.
        noway_assert(!tree->gtOverflow());
        // Small types should have had an intermediate int cast inserted in morph.
        assert(!varTypeIsSmall(dstType));
    }

    assert(!varTypeIsSmall(srcType));

    // Now determine if we have operands that should be contained.
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

// Determine if cast type is 32-bit zero extension
bool IsIntZeroExtCast(GenTreeCast* cast)
{
    GenTree* const  src          = cast->CastOp();
    const var_types srcType      = genActualType(src);
    const bool      srcUnsigned  = cast->IsUnsigned();
    const unsigned  srcSize      = genTypeSize(srcType);
    const var_types castType     = cast->gtCastType;
    const bool      castUnsigned = varTypeIsUnsigned(castType);
    const unsigned  castSize     = genTypeSize(castType);

    return varTypeIsIntegralOrI(srcType) && varTypeIsIntegralOrI(castType) && srcSize == 4 && castSize == 8 &&
           (castUnsigned || srcUnsigned);
}

// Determine SH(X)ADD(_UW) node for the given shift amount and signedness
genTreeOps GetShxaddOp(unsigned int shamt, bool isUnsigned)
{
    if (isUnsigned)
    {
        switch (shamt)
        {
            case 1:
                return GT_SH1ADD_UW;
            case 2:
                return GT_SH2ADD_UW;
            case 3:
                return GT_SH3ADD_UW;
            default:
                unreached();
        }
    }
    else
    {
        switch (shamt)
        {
            case 1:
                return GT_SH1ADD;
            case 2:
                return GT_SH2ADD;
            case 3:
                return GT_SH3ADD;
            default:
                unreached();
        }
    }
}

//------------------------------------------------------------------------
// TryLowerShiftAddToShxadd : Lower ADD(LSH) node to SH(X)ADD(.UW) node.
//
// Arguments:
//    tree - pointer to the node
//    next - [out] Next node to lower if this function returns true
//
// Return Value:
//    false if no changes were made
//
bool Lowering::TryLowerShiftAddToShxadd(GenTreeOp* tree, GenTree** next)
{
    if (comp->opts.OptimizationDisabled())
    {
        return false;
    }

    if (tree->isContained() || ((tree->gtFlags & GTF_ALL_EFFECT) != 0) || !tree->OperIs(GT_ADD) ||
        ((emitActualTypeSize(tree) != EA_8BYTE) && (emitActualTypeSize(tree) != EA_BYREF)))
    {
        return false;
    }

    GenTree* base  = nullptr;
    GenTree* shift = nullptr;

    if (tree->gtOp1->OperIs(GT_LSH, GT_MUL, GT_SLLI_UW))
    {
        shift = tree->gtOp1;
        base  = tree->gtOp2;
    }
    else if (tree->gtOp2->OperIs(GT_LSH, GT_MUL, GT_SLLI_UW))
    {
        shift = tree->gtOp2;
        base  = tree->gtOp1;
    }
    else
    {
        return false;
    }

    bool isSlliUw = false;
    if (shift->OperIs(GT_SLLI_UW))
    {
        isSlliUw = true;
    }

    GenTree*     index = shift->gtGetOp1();
    unsigned int scale = shift->GetScaledIndex();
    if (scale == 0)
    {
        return false;
    }

    assert(base->IsValue());
    assert(index->IsValue());

    if (base->isContained() || index->isContained() || !varTypeIsIntegralOrI(base) || !varTypeIsIntegralOrI(index) ||
        base->IsCnsIntOrI() || index->IsCnsIntOrI())
    {
        return false;
    }

    JITDUMP("Removing unused node:\n  ");
    DISPNODE(shift->gtGetOp2());
    BlockRange().Remove(shift->gtGetOp2());
    DEBUG_DESTROY_NODE(shift->gtGetOp2());

    JITDUMP("Removing unused node:\n  ");
    DISPNODE(shift);
    BlockRange().Remove(shift);
    DEBUG_DESTROY_NODE(shift);

    DWORD shamt;
    BitScanForward(&shamt, scale);

    tree->gtOp1 = index;
    tree->gtOp2 = base;
    tree->ChangeOper(GetShxaddOp(shamt, isSlliUw));

    JITDUMP("Base:\n  ");
    DISPNODE(tree->gtOp2);
    JITDUMP("Index:\n  ");
    DISPNODE(tree->gtOp1);

    JITDUMP("New SHXADD node:\n  ");
    DISPNODE(tree);
    JITDUMP("\n");

    if (index->OperIs(GT_CAST))
    {
        GenTreeCast* const cast = index->AsCast();
        GenTree* const     src  = cast->CastOp();

        if (IsIntZeroExtCast(cast))
        {
            JITDUMP("Removing unused node:\n  ");
            DISPNODE(cast);
            BlockRange().Remove(cast);
            DEBUG_DESTROY_NODE(cast);

            tree->gtOp1 = src;
            tree->ChangeOper(GetShxaddOp(shamt, true));

            JITDUMP("Index:\n  ");
            DISPNODE(tree->gtOp1);

            JITDUMP("Transformed SH(X)ADD node to SH(X)ADD_UW node:\n  ");
            DISPNODE(tree);
            JITDUMP("\n");
        }
    }

    *next = tree->gtNext;
    return true;
}

//------------------------------------------------------------------------
// TryLowerZextAddToAddUw : Lower ADD(CAST) node to ADD_UW node.
//
// Arguments:
//    tree - pointer to the node
//    next - [out] Next node to lower if this function returns true
//
// Return Value:
//    false if no changes were made
//
bool Lowering::TryLowerZextAddToAddUw(GenTreeOp* tree, GenTree** next)
{
    if (comp->opts.OptimizationDisabled())
    {
        return false;
    }

    if (tree->isContained() || ((tree->gtFlags & GTF_ALL_EFFECT) != 0) || !tree->OperIs(GT_ADD) ||
        ((emitActualTypeSize(tree) != EA_8BYTE) && (emitActualTypeSize(tree) != EA_BYREF)))
    {
        return false;
    }

    GenTree* base  = nullptr;
    GenTree* index = nullptr;

    if (tree->gtOp1->OperIs(GT_CAST))
    {
        index = tree->gtOp1;
        base  = tree->gtOp2;
    }
    else if (tree->gtOp2->OperIs(GT_CAST))
    {
        index = tree->gtOp2;
        base  = tree->gtOp1;
    }
    else
    {
        return false;
    }

    assert(base->IsValue());
    assert(index->IsValue());

    if (base->isContained() || index->isContained() || !varTypeIsIntegralOrI(base) || !varTypeIsIntegralOrI(index) ||
        base->IsCnsIntOrI() || index->IsCnsIntOrI())
    {
        return false;
    }

    GenTreeCast* const cast = index->AsCast();
    GenTree* const     src  = cast->CastOp();

    if (IsIntZeroExtCast(cast))
    {
        JITDUMP("Removing unused node:\n  ");
        DISPNODE(cast);
        BlockRange().Remove(cast);
        DEBUG_DESTROY_NODE(cast);

        tree->gtOp1 = src;
        tree->gtOp2 = base;
        tree->ChangeOper(GT_ADD_UW);

        JITDUMP("Base:\n  ");
        DISPNODE(tree->gtOp2);
        JITDUMP("Index:\n  ");
        DISPNODE(tree->gtOp1);

        JITDUMP("New ADD_UW node:\n  ");
        DISPNODE(tree);
        JITDUMP("\n");

        *next = tree->gtNext;

        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// TryLowerZextLeftShiftToSlliUw : Lower LSH(CAST) node to SLLI_UW node.
//
// Arguments:
//    tree - pointer to the node
//    next - [out] Next node to lower if this function returns true
//
// Return Value:
//    false if no changes were made
//
bool Lowering::TryLowerZextLeftShiftToSlliUw(GenTreeOp* tree, GenTree** next)
{
    if (comp->opts.OptimizationDisabled())
    {
        return false;
    }

    if (tree->isContained() || ((tree->gtFlags & GTF_ALL_EFFECT) != 0) || !tree->OperIs(GT_LSH) ||
        !tree->gtOp1->OperIs(GT_CAST) || !tree->gtOp2->IsCnsIntOrI() ||
        ((emitActualTypeSize(tree) != EA_8BYTE) && (emitActualTypeSize(tree) != EA_BYREF)))
    {
        return false;
    }

    GenTree* index = tree->gtOp1;

    assert(index->IsValue());

    if (index->isContained() || !varTypeIsIntegralOrI(index) || index->IsCnsIntOrI())
    {
        return false;
    }

    GenTreeCast* const cast = index->AsCast();
    GenTree* const     src  = cast->CastOp();

    if (IsIntZeroExtCast(cast))
    {
        JITDUMP("Removing unused node:\n  ");
        DISPNODE(cast);
        BlockRange().Remove(cast);
        DEBUG_DESTROY_NODE(cast);

        tree->gtOp1 = src;
        tree->ChangeOper(GT_SLLI_UW);

        JITDUMP("Index:\n  ");
        DISPNODE(tree->gtOp1);

        JITDUMP("New SLLI_UW node:\n  ");
        DISPNODE(tree);
        JITDUMP("\n");

        *next = tree->gtNext;

        return true;
    }

    return false;
}

#ifdef FEATURE_SIMD
//----------------------------------------------------------------------------------------------
// Lowering::LowerSIMD: Perform containment analysis for a SIMD intrinsic node.
//
//  Arguments:
//     simdNode - The SIMD intrinsic node.
//
void Lowering::LowerSIMD(GenTreeSIMD* simdNode)
{
    NYI_RISCV64("LowerSIMD");
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI_RISCV64("LowerHWIntrinsic");
}

//----------------------------------------------------------------------------------------------
// Lowering::IsValidConstForMovImm: Determines if the given node can be replaced by a mov/fmov immediate instruction
//
//  Arguments:
//     node - The hardware intrinsic node.
//
//  Returns:
//     true if the node can be replaced by a mov/fmov immediate instruction; otherwise, false
//
//  IMPORTANT:
//     This check may end up modifying node->gtOp1 if it is a cast node that can be removed
bool Lowering::IsValidConstForMovImm(GenTreeHWIntrinsic* node)
{
    NYI_RISCV64("IsValidConstForMovImm");
    return false;
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCmpOp: Lowers a Vector128 or Vector256 comparison intrinsic
//
//  Arguments:
//     node  - The hardware intrinsic node.
//     cmpOp - The comparison operation, currently must be GT_EQ or GT_NE
//
void Lowering::LowerHWIntrinsicCmpOp(GenTreeHWIntrinsic* node, genTreeOps cmpOp)
{
    NYI_RISCV64("LowerHWIntrinsicCmpOp");
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCreate: Lowers a Vector64 or Vector128 Create call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsicCreate(GenTreeHWIntrinsic* node)
{
    NYI_RISCV64("LowerHWIntrinsicCreate");
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicDot: Lowers a Vector64 or Vector128 Dot call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::LowerHWIntrinsicDot(GenTreeHWIntrinsic* node)
{
    NYI_RISCV64("LowerHWIntrinsicDot");
}

#endif // FEATURE_HW_INTRINSICS

//------------------------------------------------------------------------
// Containment analysis
//------------------------------------------------------------------------

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
    // There are no contained operands for RISCV64.
}

//------------------------------------------------------------------------
// ContainCheckStoreIndir: determine whether the sources of a STOREIND node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
    GenTree* src = node->Data();
    if (!varTypeIsFloating(src->TypeGet()) && src->IsIntegralConst(0))
    {
        // an integer zero for 'src' can be contained.
        MakeSrcContained(node, src);
    }

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

#ifdef FEATURE_SIMD
    NYI_RISCV64("ContainCheckIndir-SIMD");
#endif // FEATURE_SIMD

    GenTree* addr = indirNode->Addr();
    if (addr->OperIs(GT_LEA) && IsSafeToContainMem(indirNode, addr))
    {
        MakeSrcContained(indirNode, addr);
    }
    else if (addr->OperIs(GT_LCL_ADDR) && IsContainableLclAddr(addr->AsLclFld(), indirNode->Size()))
    {
        // These nodes go into an addr mode:
        // - GT_LCL_ADDR is a stack addr mode.
        MakeSrcContained(indirNode, addr);
    }
    else if (addr->IsCnsIntOrI() && !addr->AsIntCon()->ImmedValNeedsReloc(comp))
    {
        MakeSrcContained(indirNode, addr);
    }
}

//------------------------------------------------------------------------
// ContainCheckBinary: Determine whether a binary op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckBinary(GenTreeOp* node)
{
    // Check and make op2 contained (if it is a containable immediate)
    CheckImmedAndMakeContained(node, node->gtOp2);
}

//------------------------------------------------------------------------
// ContainCheckMul: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckMul(GenTreeOp* node)
{
    ContainCheckBinary(node);
}

//------------------------------------------------------------------------
// ContainCheckDivOrMod: determine which operands of a div/mod should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckDivOrMod(GenTreeOp* node)
{
    assert(node->OperIs(GT_MOD, GT_UMOD, GT_DIV, GT_UDIV));
}

//------------------------------------------------------------------------
// ContainCheckShiftRotate: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckShiftRotate(GenTreeOp* node)
{
    GenTree* shiftBy = node->gtOp2;
    assert(node->OperIsShiftOrRotate());

    if (shiftBy->IsCnsIntOrI())
    {
        MakeSrcContained(node, shiftBy);
    }
}

//------------------------------------------------------------------------
// ContainCheckStoreLoc: determine whether the source of a STORE_LCL* should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreLoc(GenTreeLclVarCommon* storeLoc) const
{
    assert(storeLoc->OperIsLocalStore());
    GenTree* op1 = storeLoc->gtGetOp1();

    if (op1->OperIs(GT_BITCAST))
    {
        // If we know that the source of the bitcast will be in a register, then we can make
        // the bitcast itself contained. This will allow us to store directly from the other
        // type if this node doesn't get a register.
        GenTree* bitCastSrc = op1->gtGetOp1();
        if (!bitCastSrc->isContained() && !bitCastSrc->IsRegOptional())
        {
            op1->SetContained();
            return;
        }
    }

    const LclVarDsc* varDsc = comp->lvaGetDesc(storeLoc);

#ifdef FEATURE_SIMD
    if (storeLoc->TypeIs(TYP_SIMD8, TYP_SIMD12))
    {
        // If this is a store to memory, we can initialize a zero vector in memory from REG_ZR.
        if ((op1->IsIntegralConst(0) || op1->IsVectorZero()) && varDsc->lvDoNotEnregister)
        {
            // For an InitBlk we want op1 to be contained
            MakeSrcContained(storeLoc, op1);
        }
        return;
    }
#endif // FEATURE_SIMD

    if (IsContainableImmed(storeLoc, op1))
    {
        MakeSrcContained(storeLoc, op1);
    }

    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    var_types type = varDsc->GetRegisterType(storeLoc);
    if (IsContainableImmed(storeLoc, op1) && (!op1->IsIntegralConst(0) || varTypeIsSmall(type)))
    {
        MakeSrcContained(storeLoc, op1);
    }
}

//------------------------------------------------------------------------
// ContainCheckCast: determine whether the source of a CAST node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCast(GenTreeCast* node)
{
    // There are no contained operands for RISCV64.
}

//------------------------------------------------------------------------
// ContainCheckCompare: determine whether the sources of a compare node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCompare(GenTreeOp* cmp)
{
    CheckImmedAndMakeContained(cmp, cmp->gtOp2);
}

//------------------------------------------------------------------------
// ContainCheckSelect : determine whether the source of a select should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckSelect(GenTreeOp* node)
{
    noway_assert(!"GT_SELECT nodes are not supported on riscv64");
}

//------------------------------------------------------------------------
// ContainCheckBoundsChk: determine whether any source of a bounds check node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckBoundsChk(GenTreeBoundsChk* node)
{
    assert(node->OperIs(GT_BOUNDS_CHECK));
    if (!CheckImmedAndMakeContained(node, node->GetIndex()))
    {
        CheckImmedAndMakeContained(node, node->GetArrayLength());
    }
}

#ifdef FEATURE_SIMD
//----------------------------------------------------------------------------------------------
// ContainCheckSIMD: Perform containment analysis for a SIMD intrinsic node.
//
//  Arguments:
//     simdNode - The SIMD intrinsic node.
//
void Lowering::ContainCheckSIMD(GenTreeSIMD* simdNode)
{
    NYI_RISCV64("ContainCheckSIMD");
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
//----------------------------------------------------------------------------------------------
// ContainCheckHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::ContainCheckHWIntrinsic(GenTreeHWIntrinsic* node)
{
    NYI_RISCV64("ContainCheckHWIntrinsic");
}
#endif // FEATURE_HW_INTRINSICS

#endif // TARGET_RISCV64
