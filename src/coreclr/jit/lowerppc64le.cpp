// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX             Lowering for PPC64LE common code                              XX
XX                                                                           XX
XX  This encapsulates common logic for lowering trees for the POWERPC64      XX
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

#ifdef TARGET_POWERPC64 // This file is ONLY used for POWERPC64 architectures

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

#ifdef FEATURE_HW_INTRINSICS
#include "hwintrinsic.h"
#endif
#include "ex.h"

//------------------------------------------------------------------------
// IsCallTargetInRange: Can a call target address be encoded in-place?
//
// Return Value:
//    True if the addr fits into the range.
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
// Notes:
//    PowerPC64LE instruction formats support various immediate sizes:
//    - D-form: 16-bit signed immediate (addi, ori, andi, etc.)
//    - DS-form: 14-bit signed immediate, 4-byte aligned (ld, std)
//    - Some instructions support 12-bit unsigned immediates
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode) const
{
    //_ASSERTE(!"NYI");
    // Floating-point operations don't support immediate operands in PowerPC64LE
    if (varTypeIsFloating(parentNode->TypeGet()))
    {
        return false;
    }

    // Make sure we have an actual immediate constant
    if (!childNode->IsCnsIntOrI())
    {
        return false;
    }

    // Check if the immediate needs relocation - if so, it cannot be contained
    if (childNode->AsIntCon()->ImmedValNeedsReloc(comp))
    {
        return false;
    }

    // Get the immediate value
    target_ssize_t immVal = (target_ssize_t)childNode->AsIntCon()->gtIconVal;

    // Helper lambda to check if value fits in signed 16-bit immediate (D-form)
    auto isValidSimm16 = [](target_ssize_t val) -> bool {
        return (val >= -32768) && (val <= 32767);
    };

    // Helper lambda to check if value fits in unsigned 16-bit immediate
    auto isValidUimm16 = [](target_ssize_t val) -> bool {
        return (val >= 0) && (val <= 65535);
    };

    // Helper lambda to check if value fits in signed 12-bit immediate
    auto isValidSimm12 = [](target_ssize_t val) -> bool {
        return (val >= -2048) && (val <= 2047);
    };

    // Check based on parent operation
    switch (parentNode->OperGet())
    {
        case GT_ADD:
        case GT_SUB:
            // addi/addis support 16-bit signed immediate
            // subi is encoded as addi with negated immediate
            return isValidSimm16(immVal);

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_CMP:
        case GT_BOUNDS_CHECK:
            // cmpi/cmpli support 16-bit signed immediate
            return isValidSimm16(immVal);

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            // andi., andis., ori, oris, xori, xoris support 16-bit unsigned immediate
            return isValidUimm16(immVal);

        case GT_JCMP:
            // Jump compare typically compares with zero
            return (immVal == 0);

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            // Allow zero immediate for stores (can use zero register)
            if (immVal == 0)
            {
                return true;
            }
            break;

        case GT_CMPXCHG:
        case GT_LOCKADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
            // Atomic operations typically don't support immediate operands
            // They require the value to be in a register
            return false;

        default:
            break;
    }

    return false;
}

#if 0
//------------------------------------------------------------------------
// IsContainableUnaryOrBinaryOp: Is the child node a unary/binary op that is containable from the parent node?
//
// Return Value:
//    True if the child node can be contained.
//
// Notes:
//    This can handle the decision to emit 'madd' or 'msub'.
//
bool Lowering::IsContainableUnaryOrBinaryOp(GenTree* parentNode, GenTree* childNode) const
{
    _ASSERTE(!"NYI");
}
#endif

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
    GenTree* next = node->gtNext;
    ContainCheckStoreIndir(node);  // Perform containment checks
    return next;  // Return next node to lower
}

//------------------------------------------------------------------------
// LowerMul: Lower a GT_MUL/GT_MULHI/GT_MUL_LONG node.
//
// For ARM64 recognized GT_MULs that can be turned into GT_MUL_LONGs, as
// those are cheaper. Performs contaiment checks.
//
// Arguments:
//    mul - The node to lower
//
// Return Value:
//    The next node to lower.
//
// Notes:
//    PowerPC64 multiply instructions:
//    - mulld: Multiply Low Doubleword (64-bit result from 64-bit operands)
//    - mullw: Multiply Low Word (32-bit result from 32-bit operands)
//    - mulhd: Multiply High Doubleword (high 64 bits of 128-bit result)
//    - mulhw: Multiply High Word (high 32 bits of 64-bit result)
//    - mulhdu: Multiply High Doubleword Unsigned
//    - mulhwu: Multiply High Word Unsigned
GenTree* Lowering::LowerMul(GenTreeOp* mul)
{
    assert(mul->OperIsMul());
    
    // PowerPC64 supports direct register-to-register multiply operations
    // No special transformations needed, just perform containment checks
    ContainCheckMul(mul);
    
    return mul->gtNext;
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
    assert(binOp->OperIsBinary());
    
    // PowerPC64 arithmetic instructions support:
    // - Register-to-register operations (R = R op R)
    // - Some immediate forms for certain operations
    
    // Perform containment analysis for the binary operation
    ContainCheckBinary(binOp);
    
    return binOp->gtNext;
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
                // On PPC64LE we can use R0 for zero
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
            // On PPC64LE we can use R0 for zero
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
            if (dstAddr->OperIs(GT_LCL_ADDR) || layout->IsStackOnly(comp))
            {
                // If the size is small enough to unroll then we need to mark the block as non-interruptible
                // to actually allow unrolling. The generated code does not report GC references loaded in the
                // temporary register(s) used for copying.
                doCpObj                  = false;
                blkNode->gtBlkOpGcUnsafe = true;
            }
        }

        if (doCpObj)
        {
            // Try to use bulk copy helper
            if (TryLowerBlockStoreAsGcBulkCopyCall(blkNode))
            {
                return;
            }

            assert((dstAddr->TypeGet() == TYP_BYREF) || (dstAddr->TypeGet() == TYP_I_IMPL));
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

    // PPC64LE load/store instructions support signed 16-bit offsets (-32768 to 32767)
    if ((offset < -32768) || (offset > 32767) || ((ClrSafeInt<ssize_t>(offset) + ClrSafeInt<ssize_t>(size)).IsOverflow()))
    {
        return;
    }

    if ((offset + static_cast<ssize_t>(size)) > 32767)
    {
        return;
    }

    if (!IsInvariantInRange(addr, blkNode, addrParent))
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
// LowerPutArgStkOrSplit: Lower a GT_PUTARG_STK/GT_PUTARG_SPLIT.
//
// Arguments:
//    putArgNode - The node to lower
//

void Lowering::LowerPutArgStkOrSplit(GenTreePutArgStk* putArgNode)
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
        // Note: For PUTARG_SPLIT with FIELD_LIST, the FIELD_LIST itself is contained,
        // but the individual field nodes are NOT contained - they need to be allocated
        // registers by LSRA so genPutArgSplit can consume them via genConsumeReg.
    }
#if FEATURE_ARG_SPLIT
    // For PUTARG_SPLIT with direct local reads (not FIELD_LIST), mark them as contained
    else if (putArgNode->OperIs(GT_PUTARG_SPLIT) && src->OperIsLocalRead())
    {
        MakeSrcContained(putArgNode, src);
    }
#endif // FEATURE_ARG_SPLIT
}

//------------------------------------------------------------------------
// LowerCast: Lower GT_CAST(srcType, DstType) nodes.
//
// Arguments:
//    tree - GT_CAST node to be lowered
//
// Return Value:
//    nextNode to be lowered if tree is modified else returns nullptr
//
// Notes:
//    Casts from float/double to a smaller int type are transformed as follows:
//    GT_CAST(float/double, byte)     =   GT_CAST(GT_CAST(float/double, int32), byte)
//    GT_CAST(float/double, sbyte)    =   GT_CAST(GT_CAST(float/double, int32), sbyte)
//    GT_CAST(float/double, int16)    =   GT_CAST(GT_CAST(double/double, int32), int16)
//    GT_CAST(float/double, uint16)   =   GT_CAST(GT_CAST(double/double, int32), uint16)
//
//    Note that for the overflow conversions we still depend on helper calls and
//    don't expect to see them here.
//    i) GT_CAST(float/double, int type with overflow detection)
//
GenTree* Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    JITDUMP("LowerCast for: ");
    DISPNODE(tree);
    JITDUMP("\n");

    GenTree*  op1     = tree->AsOp()->gtOp1;
    var_types dstType = tree->CastToType();
    var_types srcType = genActualType(op1->TypeGet());

    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
        assert(!varTypeIsSmall(dstType)); // fgMorphCast creates intermediate casts when converting from float to small int.
    }

    assert(!varTypeIsSmall(srcType));

    // Now determine if we have operands that should be contained.
    ContainCheckCast(tree->AsCast());

    return nullptr;
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
    _ASSERTE(!"NYI");
}


const int POST_INDEXED_ADDRESSING_MAX_DISTANCE = 16;

//------------------------------------------------------------------------
// TryMoveAddSubRMWAfterIndir: Try to move an RMW update of a local with an
// ADD/SUB operand earlier to happen right after an indirection on the same
// local, attempting to make these combinable intro post-indexed addressing.
//
// Arguments:
//    store - The store to a local
//
// Return Value:
//    True if the store was moved; otherwise false.
//
bool Lowering::TryMoveAddSubRMWAfterIndir(GenTreeLclVarCommon* store)
{
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// TryMakeIndirAndStoreAdjacent: Try to move a store earlier, right after the
// specified indirection.
//
// Arguments:
//   prevIndir - Indirection that comes before "store"
//   store     - Store that we want to happen next to the indirection
//
// Return Value:
//    True if the store was moved; otherwise false.
//
bool Lowering::TryMakeIndirAndStoreAdjacent(GenTreeIndir* prevIndir, GenTreeLclVarCommon* store)
{
    _ASSERTE(!"NYI");
}

#ifdef FEATURE_HW_INTRINSICS

//----------------------------------------------------------------------------------------------
// LowerHWIntrinsicFusedMultiplyAddScalar: Lowers AdvSimd_FusedMultiplyAddScalar intrinsics
//   when some of the operands are negated by "containing" such negation.
//
//  Arguments:
//     node - The original hardware intrinsic node
//
// |  op1 | op2 | op3 |
// |  +   |  +  |  +  | AdvSimd_FusedMultiplyAddScalar
// |  +   |  +  |  -  | AdvSimd_FusedMultiplySubtractScalar
// |  +   |  -  |  +  | AdvSimd_FusedMultiplySubtractScalar
// |  +   |  -  |  -  | AdvSimd_FusedMultiplyAddScalar
// |  -   |  +  |  +  | AdvSimd_FusedMultiplySubtractNegatedScalar
// |  -   |  +  |  -  | AdvSimd_FusedMultiplyAddNegatedScalar
// |  -   |  -  |  +  | AdvSimd_FusedMultiplyAddNegatedScalar
// |  -   |  -  |  -  | AdvSimd_FusedMultiplySubtractNegatedScalar
//
void Lowering::LowerHWIntrinsicFusedMultiplyAddScalar(GenTreeHWIntrinsic* node)
{
    _ASSERTE(!"NYI");
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsic(GenTreeHWIntrinsic* node)
{
    _ASSERTE(!"NYI");
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
bool Lowering::IsValidConstForMovImm(GenTreeHWIntrinsic* node)
{
    _ASSERTE(!"NYI");
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCmpOp: Lowers a Vector128 or Vector256 comparison intrinsic
//
//  Arguments:
//     node  - The hardware intrinsic node.
//     cmpOp - The comparison operation, currently must be GT_EQ or GT_NE
//
GenTree* Lowering::LowerHWIntrinsicCmpOp(GenTreeHWIntrinsic* node, genTreeOps cmpOp)
{
    _ASSERTE(!"NYI");
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCreate: Lowers a Vector64 or Vector128 Create call
//
// Performs the following transformations:
//  1. If all the arguments are constant (including the broadcast case), the vector
//     will be loaded from the data section, or turned into Zero/AllBitsSet, if possible.
//  2. Non-constant broadcasts (argCnt == 1) are turned into DuplicateToVector intrinsics.
//  3. Remaining cases get a chain of "Insert"s, from the second element to the last, where
//     the vector to be inserted into is created with CreateUnsafeScalar from the first element.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicCreate(GenTreeHWIntrinsic* node)
{
    _ASSERTE(!"NYI");
}

//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicDot: Lowers a Vector64 or Vector128 Dot call
//
//  Arguments:
//     node - The hardware intrinsic node.
//
GenTree* Lowering::LowerHWIntrinsicDot(GenTreeHWIntrinsic* node)
{
    _ASSERTE(!"NYI");
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
    return;
}

//------------------------------------------------------------------------
// ContainCheckStoreIndir: determine whether the sources of a STOREIND node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
	// For PowerPC64LE, we can contain integer zero as source
    GenTree* src = node->Data();
    if (src->IsIntegralConst(0))
    {
        // an integer zero for 'src' can be contained.
        MakeSrcContained(node, src);
    }
    ContainCheckIndir(node);  // Check address containment

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
    return;
}

//------------------------------------------------------------------------
// ContainCheckBinary: Determine whether a binary op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckBinary(GenTreeOp* node)
{
    //_ASSERTE(!"NYI");
    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    // PowerPC64LE binary instructions typically support immediate operands
    // in the second operand position (e.g., addi, ori, andi, etc.)
    // Check if op2 can be contained as an immediate

    if (CheckImmedAndMakeContained(node, op2))
    {
        return;
    }

    // For commutative operations, we can also try to contain op1
    // and swap the operands if successful

    if (node->OperIsCommutative() && CheckImmedAndMakeContained(node, op1))
    {
        MakeSrcContained(node, op1);
        std::swap(node->gtOp1, node->gtOp2);
        return;
    }

    // PowerPC64LE doesn't have complex addressing modes like x86,
    // so we don't need to check for containable memory operands here.
    // Memory operands are handled separately in load/store lowering.
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
    // _ASSERTE(!"NYI");
    // Need to check if ppc64le support div with immediate
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

    // If shift amount is a constant, we can contain it (use immediate form)
    if (shiftBy->IsCnsIntOrI())
    {
        MakeSrcContained(node, shiftBy);
    }
    // Otherwise, shift amount must be in a register (not contained)
}

//------------------------------------------------------------------------
// ContainCheckStoreLoc: determine whether the source of a STORE_LCL* should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreLoc(GenTreeLclVarCommon* storeLoc) const
{
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// ContainCheckCast: determine whether the source of a CAST node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCast(GenTreeCast* node)
{
    GenTree*  castOp     = node->CastOp();
    var_types castToType = node->CastToType();

    // PowerPC64LE can contain memory operands for integral casts in certain cases
    if (comp->opts.OptimizationEnabled() && !node->gtOverflow() && varTypeIsIntegral(castOp) &&
        varTypeIsIntegral(castToType))
    {
        // Most integral casts can be re-expressed as loads, except those that would be changing the sign.
        if (!varTypeIsSmall(castOp) || (varTypeIsUnsigned(castOp) == node->IsZeroExtending()))
        {
            bool srcIsContainable = false;

            // Make sure to only contain indirections codegen can handle.
            if (castOp->OperIs(GT_IND))
            {
                GenTreeIndir* indir = castOp->AsIndir();

                if (!indir->IsVolatile() && !indir->IsUnaligned())
                {
                    GenTree* addr = indir->Addr();

                    if (!addr->isContained())
                    {
                        srcIsContainable = true;
                    }
                }
            }
            else
            {
                assert(castOp->OperIsLocalRead() || !IsContainableMemoryOp(castOp));
                srcIsContainable = true;
            }

            if (srcIsContainable)
            {
                if (IsContainableMemoryOp(castOp) && IsSafeToContainMem(node, castOp))
                {
                    MakeSrcContained(node, castOp);
                }
                else if (IsSafeToMarkRegOptional(node, castOp))
                {
                    castOp->SetRegOptional();
                }
            }
        }
    }
}


//------------------------------------------------------------------------
// ContainCheckCompare: determine whether the sources of a compare node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCompare(GenTreeOp* cmp)
{
    return;
}

//------------------------------------------------------------------------
// ContainCheckSelect : determine whether the source of a select should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckSelect(GenTreeOp* node)
{
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// ContainCheckBoundsChk: determine whether any source of a bounds check node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckBoundsChk(GenTreeBoundsChk* node)
{
    _ASSERTE(!"NYI");
}

#ifdef FEATURE_HW_INTRINSICS

//----------------------------------------------------------------------------------------------
// ContainCheckHWIntrinsic: Perform containment analysis for a hardware intrinsic node.
//
//  Arguments:
//     node - The hardware intrinsic node.
//
void Lowering::ContainCheckHWIntrinsic(GenTreeHWIntrinsic* node)
{
    _ASSERTE(!"NYI");
}
//----------------------------------------------------------------------------------------------
// Lowering::LowerHWIntrinsicCndSel: Lowers a Sve ConditionalSelect call
//
//  Arguments:
//     node - The hardware intrinsic node of the form
//            ConditionalSelect(mask, trueValue, falseValue)
//
//  Returns:
//    Next node to lower.
//
GenTree* Lowering::LowerHWIntrinsicCndSel(GenTreeHWIntrinsic* cndSelNode)
{
    _ASSERTE(!"NYI");
}

//----------------------------------------------------------------------------------------------
// StoreFFRValue: For hwintrinsic that produce a first faulting register (FFR) value, create
// nodes to save its value to a local variable.
//
// Arguments:
//     node - The node before which the pseudo definition is needed
//
void Lowering::StoreFFRValue(GenTreeHWIntrinsic* node)
{
    _ASSERTE(!"NYI");
}

#endif // FEATURE_HW_INTRINSICS

#endif //TARGET_POWERPC64
