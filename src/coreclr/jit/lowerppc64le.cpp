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
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
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
GenTree* Lowering::LowerMul(GenTreeOp* mul)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// LowerBlockStore: Lower a block store node
//
// Arguments:
//    blkNode - The block store node to lower
//
void Lowering::LowerBlockStore(GenTreeBlk* blkNode)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// LowerPutArgStkOrSplit: Lower a GT_PUTARG_STK/GT_PUTARG_SPLIT.
//
// Arguments:
//    putArgStk - The node to lower
//

void Lowering::LowerPutArgStkOrSplit(GenTreePutArgStk* putArgNode)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
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
    // There are no contained operands for arm.
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// ContainCheckStoreIndir: determine whether the sources of a STOREIND node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckStoreIndir(GenTreeStoreInd* node)
{
    _ASSERTE(!"NYI");
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
    //_ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// ContainCheckMul: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckMul(GenTreeOp* node)
{
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// ContainCheckDivOrMod: determine which operands of a div/mod should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckDivOrMod(GenTreeOp* node)
{
    _ASSERTE(!"NYI");
    // ARM doesn't have a div instruction with an immediate operand
}

//------------------------------------------------------------------------
// ContainCheckShiftRotate: Determine whether a mul op's operands should be contained.
//
// Arguments:
//    node - the node we care about
//
void Lowering::ContainCheckShiftRotate(GenTreeOp* node)
{
    _ASSERTE(!"NYI");
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
    _ASSERTE(!"NYI");
}

//------------------------------------------------------------------------
// ContainCheckCompare: determine whether the sources of a compare node should be contained.
//
// Arguments:
//    node - pointer to the node
//
void Lowering::ContainCheckCompare(GenTreeOp* cmp)
{
    _ASSERTE(!"NYI");
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
