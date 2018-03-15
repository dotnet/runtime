// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Register Requirements for ARM64                        XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the ARM64 architecture.                                                  XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM64_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

//------------------------------------------------------------------------
// BuildNode: Set the register requirements for RA.
//
// Notes:
//    Takes care of annotating the register requirements
//    for every TreeNodeInfo struct that maps to each tree node.
//
// Preconditions:
//    LSRA has been initialized and there is a TreeNodeInfo node
//    already allocated and initialized for every tree in the IR.
//
// Postconditions:
//    Every TreeNodeInfo instance has the right annotations on register
//    requirements needed by LSRA to build the Interval Table (source,
//    destination and internal [temp] register counts).
//
void LinearScan::BuildNode(GenTree* tree)
{
    TreeNodeInfo* info         = currentNodeInfo;
    unsigned      kind         = tree->OperKind();
    RegisterType  registerType = TypeGet(tree);

    if (tree->isContained())
    {
        info->dstCount = 0;
        assert(info->srcCount == 0);
        return;
    }

    // Set the default dstCount. This may be modified below.
    if (tree->IsValue())
    {
        info->dstCount = 1;
        if (tree->IsUnusedValue())
        {
            info->isLocalDefUse = true;
        }
    }
    else
    {
        info->dstCount = 0;
    }

    switch (tree->OperGet())
    {
        GenTree* op1;
        GenTree* op2;

        default:
            BuildSimple(tree);
            break;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            info->srcCount = 1;
            assert(info->dstCount == 0);
            BuildStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_FIELD_LIST:
            // These should always be contained. We don't correctly allocate or
            // generate code for a non-contained GT_FIELD_LIST.
            noway_assert(!"Non-contained GT_FIELD_LIST");
            break;

        case GT_LIST:
        case GT_ARGPLACE:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_CNS_DBL:
            info->srcCount = 0;
            assert(info->dstCount == 1);
            {
                GenTreeDblCon* dblConst   = tree->AsDblCon();
                double         constValue = dblConst->gtDblCon.gtDconVal;

                if (emitter::emitIns_valid_imm_for_fmov(constValue))
                {
                    // Directly encode constant to instructions.
                }
                else
                {
                    // Reserve int to load constant from memory (IF_LARGELDC)
                    info->internalIntCount = 1;
                }
            }
            break;

        case GT_BOX:
        case GT_COMMA:
        case GT_QMARK:
        case GT_COLON:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            unreached();
            break;

        case GT_RETURN:
            BuildReturn(tree);
            break;

        case GT_RETFILT:
            if (tree->TypeGet() == TYP_VOID)
            {
                info->srcCount = 0;
                assert(info->dstCount == 0);
            }
            else
            {
                assert(tree->TypeGet() == TYP_INT);

                info->srcCount = 1;
                assert(info->dstCount == 0);

                info->setSrcCandidates(this, RBM_INTRET);
                LocationInfoListNode* locationInfo = getLocationInfo(tree->gtOp.gtOp1);
                locationInfo->info.setSrcCandidates(this, RBM_INTRET);
                useList.Append(locationInfo);
            }
            break;

        case GT_NOP:
            // A GT_NOP is either a passthrough (if it is void, or if it has
            // a child), but must be considered to produce a dummy value if it
            // has a type but no child
            info->srcCount = 0;
            if (tree->TypeGet() != TYP_VOID && tree->gtOp.gtOp1 == nullptr)
            {
                assert(info->dstCount == 1);
            }
            else
            {
                assert(info->dstCount == 0);
            }
            break;

        case GT_JTRUE:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_JMP:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_SWITCH:
            // This should never occur since switch nodes must not be visible at this
            // point in the JIT.
            info->srcCount = 0;
            noway_assert(!"Switch must be lowered at this point");
            break;

        case GT_JMPTABLE:
            info->srcCount = 0;
            assert(info->dstCount == 1);
            break;

        case GT_SWITCH_TABLE:
            info->srcCount         = appendBinaryLocationInfoToList(tree->AsOp());
            info->internalIntCount = 1;
            assert(info->dstCount == 0);
            break;

        case GT_ASG:
            noway_assert(!"We should never hit any assignment operator in lowering");
            info->srcCount = 0;
            break;

        case GT_ADD:
        case GT_SUB:
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // overflow operations aren't supported on float/double types.
                assert(!tree->gtOverflow());

                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(tree->gtOp.gtOp1->TypeGet() == tree->gtOp.gtOp2->TypeGet());
            }

            __fallthrough;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->dstCount == 1);
            break;

        case GT_RETURNTRAP:
            // this just turns into a compare of its child with an int
            // + a conditional call
            appendLocationInfoToList(tree->gtGetOp1());
            info->srcCount = 1;
            assert(info->dstCount == 0);
            break;

        case GT_MOD:
        case GT_UMOD:
            NYI_IF(varTypeIsFloating(tree->TypeGet()), "FP Remainder in ARM64");
            assert(!"Shouldn't see an integer typed GT_MOD node in ARM64");
            break;

        case GT_MUL:
            if (tree->gtOverflow())
            {
                // Need a register different from target reg to check for overflow.
                info->internalIntCount       = 1;
                info->isInternalRegDelayFree = true;
            }
            __fallthrough;

        case GT_DIV:
        case GT_MULHI:
        case GT_UDIV:
        {
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->dstCount == 1);
        }
        break;

        case GT_INTRINSIC:
        {
            noway_assert((tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Abs) ||
                         (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Ceiling) ||
                         (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Floor) ||
                         (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Round) ||
                         (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Sqrt));

            // Both operand and its result must be of the same floating point type.
            op1 = tree->gtOp.gtOp1;
            assert(varTypeIsFloating(op1));
            assert(op1->TypeGet() == tree->TypeGet());

            appendLocationInfoToList(op1);
            info->srcCount = 1;
            assert(info->dstCount == 1);
        }
        break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            BuildSIMD(tree->AsSIMD());
            break;
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWIntrinsic:
            BuildHWIntrinsic(tree->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_CAST:
        {
            // TODO-ARM64-CQ: Int-To-Int conversions - castOp cannot be a memory op and must have an assigned
            //                register.
            //         see CodeGen::genIntToIntCast()

            appendLocationInfoToList(tree->gtGetOp1());
            info->srcCount = 1;
            assert(info->dstCount == 1);

            // Non-overflow casts to/from float/double are done using SSE2 instructions
            // and that allow the source operand to be either a reg or memop. Given the
            // fact that casts from small int to float/double are done as two-level casts,
            // the source operand is always guaranteed to be of size 4 or 8 bytes.
            var_types castToType = tree->CastToType();
            GenTree*  castOp     = tree->gtCast.CastOp();
            var_types castOpType = castOp->TypeGet();
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                castOpType = genUnsignedType(castOpType);
            }

            // Some overflow checks need a temp reg

            Lowering::CastInfo castInfo;
            // Get information about the cast.
            Lowering::getCastDescription(tree, &castInfo);

            if (castInfo.requiresOverflowCheck)
            {
                var_types srcType = castOp->TypeGet();
                emitAttr  cmpSize = EA_ATTR(genTypeSize(srcType));

                // If we cannot store the comparisons in an immediate for either
                // comparing against the max or min value, then we will need to
                // reserve a temporary register.

                bool canStoreMaxValue = emitter::emitIns_valid_imm_for_cmp(castInfo.typeMax, cmpSize);
                bool canStoreMinValue = emitter::emitIns_valid_imm_for_cmp(castInfo.typeMin, cmpSize);

                if (!canStoreMaxValue || !canStoreMinValue)
                {
                    info->internalIntCount = 1;
                }
            }
        }
        break;

        case GT_NEG:
        case GT_NOT:
            appendLocationInfoToList(tree->gtGetOp1());
            info->srcCount = 1;
            assert(info->dstCount == 1);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            BuildShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_JCMP:
            BuildCmp(tree);
            break;

        case GT_CKFINITE:
            appendLocationInfoToList(tree->gtOp.gtOp1);
            info->srcCount = 1;
            assert(info->dstCount == 1);
            info->internalIntCount = 1;
            break;

        case GT_CMPXCHG:
        {
            GenTreeCmpXchg* cmpXchgNode = tree->AsCmpXchg();
            info->srcCount              = cmpXchgNode->gtOpComparand->isContained() ? 2 : 3;
            assert(info->dstCount == 1);

            info->internalIntCount = 1;

            // For ARMv8 exclusives the lifetime of the addr and data must be extended because
            // it may be used used multiple during retries
            LocationInfoListNode* locationInfo = getLocationInfo(tree->gtCmpXchg.gtOpLocation);
            locationInfo->info.isDelayFree     = true;
            useList.Append(locationInfo);
            LocationInfoListNode* valueInfo = getLocationInfo(tree->gtCmpXchg.gtOpValue);
            valueInfo->info.isDelayFree     = true;
            useList.Append(valueInfo);
            if (!cmpXchgNode->gtOpComparand->isContained())
            {
                LocationInfoListNode* comparandInfo = getLocationInfo(tree->gtCmpXchg.gtOpComparand);
                comparandInfo->info.isDelayFree     = true;
                useList.Append(comparandInfo);
            }
            info->hasDelayFreeSrc = true;

            // Internals may not collide with target
            info->isInternalRegDelayFree = true;
        }
        break;

        case GT_LOCKADD:
        case GT_XADD:
        case GT_XCHG:
        {
            assert(info->dstCount == (tree->TypeGet() == TYP_VOID) ? 0 : 1);
            info->srcCount         = tree->gtOp.gtOp2->isContained() ? 1 : 2;
            info->internalIntCount = (tree->OperGet() == GT_XCHG) ? 1 : 2;

            // For ARMv8 exclusives the lifetime of the addr and data must be extended because
            // it may be used used multiple during retries
            assert(!tree->gtOp.gtOp1->isContained());
            LocationInfoListNode* op1Info = getLocationInfo(tree->gtOp.gtOp1);
            useList.Append(op1Info);
            LocationInfoListNode* op2Info = nullptr;
            if (!tree->gtOp.gtOp2->isContained())
            {
                op2Info = getLocationInfo(tree->gtOp.gtOp2);
                useList.Append(op2Info);
            }
            if (info->dstCount != 0)
            {
                op1Info->info.isDelayFree = true;
                if (op2Info != nullptr)
                {
                    op2Info->info.isDelayFree = true;
                }
                // Internals may not collide with target
                info->isInternalRegDelayFree = true;
                info->hasDelayFreeSrc        = true;
            }
        }
        break;

        case GT_PUTARG_STK:
            BuildPutArgStk(tree->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
            BuildPutArgReg(tree->AsUnOp());
            break;

        case GT_CALL:
            BuildCall(tree->AsCall());
            break;

        case GT_ADDR:
        {
            // For a GT_ADDR, the child node should not be evaluated into a register
            GenTree* child = tree->gtOp.gtOp1;
            assert(!isCandidateLocalRef(child));
            assert(child->isContained());
            assert(info->dstCount == 1);
            info->srcCount = 0;
        }
        break;

        case GT_BLK:
        case GT_DYN_BLK:
            // These should all be eliminated prior to Lowering.
            assert(!"Non-store block node in Lowering");
            info->srcCount = 0;
            break;

        case GT_STORE_BLK:
        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
            BuildBlockStore(tree->AsBlk());
            break;

        case GT_INIT_VAL:
            // Always a passthrough of its child's value.
            assert(!"INIT_VAL should always be contained");
            break;

        case GT_LCLHEAP:
        {
            assert(info->dstCount == 1);

            // Need a variable number of temp regs (see genLclHeap() in codegenamd64.cpp):
            // Here '-' means don't care.
            //
            //  Size?                   Init Memory?    # temp regs
            //   0                          -               0
            //   const and <=6 ptr words    -               0
            //   const and <PageSize        No              0
            //   >6 ptr words               Yes           hasPspSym ? 1 : 0
            //   Non-const                  Yes           hasPspSym ? 1 : 0
            //   Non-const                  No              2
            //
            // PSPSym - If the method has PSPSym increment internalIntCount by 1.
            //
            bool hasPspSym;
#if FEATURE_EH_FUNCLETS
            hasPspSym = (compiler->lvaPSPSym != BAD_VAR_NUM);
#else
            hasPspSym = false;
#endif

            GenTree* size = tree->gtOp.gtOp1;
            if (size->IsCnsIntOrI())
            {
                assert(size->isContained());
                info->srcCount = 0;

                size_t sizeVal = size->gtIntCon.gtIconVal;

                if (sizeVal == 0)
                {
                    info->internalIntCount = 0;
                }
                else
                {
                    // Compute the amount of memory to properly STACK_ALIGN.
                    // Note: The Gentree node is not updated here as it is cheap to recompute stack aligned size.
                    // This should also help in debugging as we can examine the original size specified with
                    // localloc.
                    sizeVal                          = AlignUp(sizeVal, STACK_ALIGN);
                    size_t cntStackAlignedWidthItems = (sizeVal >> STACK_ALIGN_SHIFT);

                    // For small allocations upto 4 'stp' instructions (i.e. 64 bytes of localloc)
                    //
                    if (cntStackAlignedWidthItems <= 4)
                    {
                        info->internalIntCount = 0;
                    }
                    else if (!compiler->info.compInitMem)
                    {
                        // No need to initialize allocated stack space.
                        if (sizeVal < compiler->eeGetPageSize())
                        {
                            info->internalIntCount = 0;
                        }
                        else
                        {
                            // We need two registers: regCnt and RegTmp
                            info->internalIntCount = 2;
                        }
                    }
                    else
                    {
                        // greater than 4 and need to zero initialize allocated stack space.
                        // If the method has PSPSym, we need an internal register to hold regCnt
                        // since targetReg allocated to GT_LCLHEAP node could be the same as one of
                        // the the internal registers.
                        info->internalIntCount = hasPspSym ? 1 : 0;
                    }
                }
            }
            else
            {
                appendLocationInfoToList(size);
                info->srcCount = 1;
                if (!compiler->info.compInitMem)
                {
                    info->internalIntCount = 2;
                }
                else
                {
                    // If the method has PSPSym, we need an internal register to hold regCnt
                    // since targetReg allocated to GT_LCLHEAP node could be the same as one of
                    // the the internal registers.
                    info->internalIntCount = hasPspSym ? 1 : 0;
                }
            }

            // If the method has PSPSym, we would need an addtional register to relocate it on stack.
            if (hasPspSym)
            {
                // Exclude const size 0
                if (!size->IsCnsIntOrI() || (size->gtIntCon.gtIconVal > 0))
                    info->internalIntCount++;
            }
        }
        break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        {
            GenTreeBoundsChk* node = tree->AsBoundsChk();
            // Consumes arrLen & index - has no result
            assert(info->dstCount == 0);

            GenTree* intCns = nullptr;
            GenTree* other  = nullptr;
            info->srcCount  = GetOperandInfo(tree->AsBoundsChk()->gtIndex);
            info->srcCount += GetOperandInfo(tree->AsBoundsChk()->gtArrLen);
        }
        break;

        case GT_ARR_ELEM:
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM in lowering");
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_ARR_INDEX:
        {
            info->srcCount = 2;
            assert(info->dstCount == 1);
            info->internalIntCount       = 1;
            info->isInternalRegDelayFree = true;

            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            LocationInfoListNode* arrObjInfo = getLocationInfo(tree->AsArrIndex()->ArrObj());
            arrObjInfo->info.isDelayFree     = true;
            useList.Append(arrObjInfo);
            useList.Append(getLocationInfo(tree->AsArrIndex()->IndexExpr()));
            info->hasDelayFreeSrc = true;
        }
        break;

        case GT_ARR_OFFSET:
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            info->srcCount = 2;
            if (!tree->gtArrOffs.gtOffset->isContained())
            {
                appendLocationInfoToList(tree->AsArrOffs()->gtOffset);
                info->srcCount++;
            }
            appendLocationInfoToList(tree->AsArrOffs()->gtIndex);
            appendLocationInfoToList(tree->AsArrOffs()->gtArrObj);
            assert(info->dstCount == 1);
            info->internalIntCount = 1;
            break;

        case GT_LEA:
        {
            GenTreeAddrMode* lea = tree->AsAddrMode();

            GenTree* base  = lea->Base();
            GenTree* index = lea->Index();
            int      cns   = lea->Offset();

            // This LEA is instantiating an address, so we set up the srcCount here.
            info->srcCount = 0;
            if (base != nullptr)
            {
                info->srcCount++;
                appendLocationInfoToList(base);
            }
            if (index != nullptr)
            {
                info->srcCount++;
                appendLocationInfoToList(index);
            }
            assert(info->dstCount == 1);

            // On ARM64 we may need a single internal register
            // (when both conditions are true then we still only need a single internal register)
            if ((index != nullptr) && (cns != 0))
            {
                // ARM64 does not support both Index and offset so we need an internal register
                info->internalIntCount = 1;
            }
            else if (!emitter::emitIns_valid_imm_for_add(cns, EA_8BYTE))
            {
                // This offset can't be contained in the add instruction, so we need an internal register
                info->internalIntCount = 1;
            }
        }
        break;

        case GT_STOREIND:
        {
            assert(info->dstCount == 0);

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
            {
                info->srcCount = 2;
                BuildGCWriteBarrier(tree);
                break;
            }

            BuildIndir(tree->AsIndir());
            if (!tree->gtGetOp2()->isContained())
            {
                appendLocationInfoToList(tree->gtGetOp2());
                info->srcCount++;
            }
        }
        break;

        case GT_NULLCHECK:
            // Unlike ARM, ARM64 implements NULLCHECK as a load to REG_ZR, so no internal register
            // is required, and it is not a localDefUse.
            assert(info->dstCount == 0);
            assert(!tree->gtGetOp1()->isContained());
            appendLocationInfoToList(tree->gtOp.gtOp1);
            info->srcCount = 1;
            break;

        case GT_IND:
            assert(info->dstCount == 1);
            BuildIndir(tree->AsIndir());
            break;

        case GT_CATCH_ARG:
            info->srcCount = 0;
            assert(info->dstCount == 1);
            info->setDstCandidates(this, RBM_EXCEPTION_OBJECT);
            break;

        case GT_CLS_VAR:
            info->srcCount = 0;
            // GT_CLS_VAR, by the time we reach the backend, must always
            // be a pure use.
            // It will produce a result of the type of the
            // node, and use an internal register for the address.

            assert(info->dstCount == 1);
            assert((tree->gtFlags & (GTF_VAR_DEF | GTF_VAR_USEASG)) == 0);
            info->internalIntCount = 1;
            break;

        case GT_INDEX_ADDR:
            assert(info->dstCount == 1);
            info->srcCount         = appendBinaryLocationInfoToList(tree->AsOp());
            info->internalIntCount = 1;
            break;
    } // end switch (tree->OperGet())

    if (tree->IsUnusedValue() && (info->dstCount != 0))
    {
        info->isLocalDefUse = true;
    }
    // We need to be sure that we've set info->srcCount and info->dstCount appropriately
    assert((info->dstCount < 2) || tree->IsMultiRegCall());
    assert(info->isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsUnusedValue() || (info->dstCount != 0));
    assert(info->dstCount == tree->GetRegisterDstCount());
}

#ifdef FEATURE_SIMD
//------------------------------------------------------------------------
// BuildSIMD: Set the NodeInfo for a GT_SIMD tree.
//
// Arguments:
//    tree       - The GT_SIMD node of interest
//
// Return Value:
//    None.

void LinearScan::BuildSIMD(GenTreeSIMD* simdTree)
{
    TreeNodeInfo* info = currentNodeInfo;
    // Only SIMDIntrinsicInit can be contained
    if (simdTree->isContained())
    {
        assert(simdTree->gtSIMDIntrinsicID == SIMDIntrinsicInit);
    }
    assert(info->dstCount == 1);

    GenTree* op1 = simdTree->gtOp.gtOp1;
    GenTree* op2 = simdTree->gtOp.gtOp2;
    if (!op1->OperIs(GT_LIST))
    {
        info->srcCount += GetOperandInfo(op1);
    }
    if ((op2 != nullptr) && !op2->isContained())
    {
        info->srcCount += GetOperandInfo(op2);
    }

    switch (simdTree->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicInit:
            assert(info->srcCount == (simdTree->gtGetOp1()->isContained() ? 0 : 1));
            break;

        case SIMDIntrinsicCast:
        case SIMDIntrinsicSqrt:
        case SIMDIntrinsicAbs:
        case SIMDIntrinsicConvertToSingle:
        case SIMDIntrinsicConvertToInt32:
        case SIMDIntrinsicConvertToDouble:
        case SIMDIntrinsicConvertToInt64:
        case SIMDIntrinsicWidenLo:
        case SIMDIntrinsicWidenHi:
            assert(info->srcCount == 1);
            break;

        case SIMDIntrinsicGetItem:
        {
            op1 = simdTree->gtGetOp1();
            op2 = simdTree->gtGetOp2();

            // We have an object and an index, either of which may be contained.
            if (!op2->IsCnsIntOrI() && (!op1->isContained() || op1->OperIsLocal()))
            {
                // If the index is not a constant and not contained or is a local
                // we will need a general purpose register to calculate the address
                info->internalIntCount = 1;

                // internal register must not clobber input index
                LocationInfoListNode* op2Info =
                    (op1->isContained()) ? useList.Begin() : useList.GetSecond(INDEBUG(op2));
                op2Info->info.isDelayFree = true;
                info->hasDelayFreeSrc     = true;
            }

            if (!op2->IsCnsIntOrI() && (!op1->isContained()))
            {
                // If vector is not already in memory (contained) and the index is not a constant,
                // we will use the SIMD temp location to store the vector.
                compiler->getSIMDInitTempVarNum();
            }
        }
        break;

        case SIMDIntrinsicAdd:
        case SIMDIntrinsicSub:
        case SIMDIntrinsicMul:
        case SIMDIntrinsicDiv:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseAndNot:
        case SIMDIntrinsicBitwiseOr:
        case SIMDIntrinsicBitwiseXor:
        case SIMDIntrinsicMin:
        case SIMDIntrinsicMax:
        case SIMDIntrinsicEqual:
        case SIMDIntrinsicLessThan:
        case SIMDIntrinsicGreaterThan:
        case SIMDIntrinsicLessThanOrEqual:
        case SIMDIntrinsicGreaterThanOrEqual:
            assert(info->srcCount == 2);
            break;

        case SIMDIntrinsicSetX:
        case SIMDIntrinsicSetY:
        case SIMDIntrinsicSetZ:
        case SIMDIntrinsicSetW:
        case SIMDIntrinsicNarrow:
            assert(info->srcCount == 2);

            // Op1 will write to dst before Op2 is free
            useList.GetSecond(INDEBUG(simdTree->gtGetOp2()))->info.isDelayFree = true;
            info->hasDelayFreeSrc                                              = true;
            break;

        case SIMDIntrinsicInitN:
        {
            var_types baseType = simdTree->gtSIMDBaseType;
            info->srcCount     = (short)(simdTree->gtSIMDSize / genTypeSize(baseType));
            int initCount      = 0;
            for (GenTree* list = op1; list != nullptr; list = list->gtGetOp2())
            {
                assert(list->OperGet() == GT_LIST);
                GenTree* listItem = list->gtGetOp1();
                assert(listItem->TypeGet() == baseType);
                assert(!listItem->isContained());
                appendLocationInfoToList(listItem);
                initCount++;
            }
            assert(initCount == info->srcCount);

            if (varTypeIsFloating(simdTree->gtSIMDBaseType))
            {
                // Need an internal register to stitch together all the values into a single vector in a SIMD reg.
                info->setInternalCandidates(this, RBM_ALLFLOAT);
                info->internalFloatCount = 1;
            }
            break;
        }

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            assert(info->srcCount == (simdTree->gtGetOp2()->isContained() ? 1 : 2));
            break;

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            assert(info->srcCount == (simdTree->gtGetOp2()->isContained() ? 1 : 2));
            info->setInternalCandidates(this, RBM_ALLFLOAT);
            info->internalFloatCount = 1;
            break;

        case SIMDIntrinsicDotProduct:
            assert(info->srcCount == 2);
            info->setInternalCandidates(this, RBM_ALLFLOAT);
            info->internalFloatCount = 1;
            break;

        case SIMDIntrinsicSelect:
            // TODO-ARM64-CQ Allow lowering to see SIMDIntrinsicSelect so we can generate BSL VC, VA, VB
            // bsl target register must be VC.  Reserve a temp in case we need to shuffle things.
            // This will require a different approach, as GenTreeSIMD has only two operands.
            assert(!"SIMDIntrinsicSelect not yet supported");
            assert(info->srcCount == 3);
            info->setInternalCandidates(this, RBM_ALLFLOAT);
            info->internalFloatCount = 1;
            break;

        case SIMDIntrinsicInitArrayX:
        case SIMDIntrinsicInitFixed:
        case SIMDIntrinsicCopyToArray:
        case SIMDIntrinsicCopyToArrayX:
        case SIMDIntrinsicNone:
        case SIMDIntrinsicGetCount:
        case SIMDIntrinsicGetOne:
        case SIMDIntrinsicGetZero:
        case SIMDIntrinsicGetAllOnes:
        case SIMDIntrinsicGetX:
        case SIMDIntrinsicGetY:
        case SIMDIntrinsicGetZ:
        case SIMDIntrinsicGetW:
        case SIMDIntrinsicInstEquals:
        case SIMDIntrinsicHWAccel:
        case SIMDIntrinsicWiden:
        case SIMDIntrinsicInvalid:
            assert(!"These intrinsics should not be seen during register allocation");
            __fallthrough;

        default:
            noway_assert(!"Unimplemented SIMD node type.");
            unreached();
    }
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
#include "hwintrinsicArm64.h"
//------------------------------------------------------------------------
// BuildHWIntrinsic: Set the NodeInfo for a GT_HWIntrinsic tree.
//
// Arguments:
//    tree       - The GT_HWIntrinsic node of interest
//
// Return Value:
//    None.

void LinearScan::BuildHWIntrinsic(GenTreeHWIntrinsic* intrinsicTree)
{
    TreeNodeInfo*  info        = currentNodeInfo;
    NamedIntrinsic intrinsicID = intrinsicTree->gtHWIntrinsicId;

    GenTreeArgList* argList = nullptr;
    GenTree*        op1     = intrinsicTree->gtOp.gtOp1;
    GenTree*        op2     = intrinsicTree->gtOp.gtOp2;

    if (op1->OperIs(GT_LIST))
    {
        argList = op1->AsArgList();
        op1     = argList->Current();
        op2     = argList->Rest()->Current();

        for (GenTreeArgList* list = argList; list != nullptr; list = list->Rest())
        {
            info->srcCount += GetOperandInfo(list->Current());
        }
    }
    else
    {
        info->srcCount += GetOperandInfo(op1);
        if (op2 != nullptr)
        {
            info->srcCount += GetOperandInfo(op2);
        }
    }

    switch (compiler->getHWIntrinsicInfo(intrinsicID).form)
    {
        case HWIntrinsicInfo::Sha1HashOp:
            info->setInternalCandidates(this, RBM_ALLFLOAT);
            info->internalFloatCount = 1;
            if (!op2->isContained())
            {
                LocationInfoListNode* op2Info = useList.Begin()->Next();
                op2Info->info.isDelayFree     = true;
                GenTree* op3                  = intrinsicTree->gtOp.gtOp1->AsArgList()->Rest()->Rest()->Current();
                assert(!op3->isContained());
                LocationInfoListNode* op3Info = op2Info->Next();
                op3Info->info.isDelayFree     = true;
                info->hasDelayFreeSrc         = true;
                info->isInternalRegDelayFree  = true;
            }
            break;
        case HWIntrinsicInfo::SimdTernaryRMWOp:
            if (!op2->isContained())
            {
                LocationInfoListNode* op2Info = useList.Begin()->Next();
                op2Info->info.isDelayFree     = true;
                GenTree* op3                  = intrinsicTree->gtOp.gtOp1->AsArgList()->Rest()->Rest()->Current();
                assert(!op3->isContained());
                LocationInfoListNode* op3Info = op2Info->Next();
                op3Info->info.isDelayFree     = true;
                info->hasDelayFreeSrc         = true;
            }
            break;
        case HWIntrinsicInfo::Sha1RotateOp:
            info->setInternalCandidates(this, RBM_ALLFLOAT);
            info->internalFloatCount = 1;
            break;

        case HWIntrinsicInfo::SimdExtractOp:
        case HWIntrinsicInfo::SimdInsertOp:
            if (!op2->isContained())
            {
                // We need a temp to create a switch table
                info->internalIntCount = 1;
                info->setInternalCandidates(this, allRegs(TYP_INT));
            }
            break;

        default:
            break;
    }
}
#endif

#endif // _TARGET_ARM64_

#endif // !LEGACY_BACKEND
