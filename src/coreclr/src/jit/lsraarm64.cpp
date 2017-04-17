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
// TreeNodeInfoInit: Set the register requirements for RA.
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
void Lowering::TreeNodeInfoInit(GenTree* tree)
{
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    unsigned      kind         = tree->OperKind();
    TreeNodeInfo* info         = &(tree->gtLsraInfo);
    RegisterType  registerType = TypeGet(tree);

    JITDUMP("TreeNodeInfoInit for: ");
    DISPNODE(tree);
    JITDUMP("\n");

    switch (tree->OperGet())
    {
        GenTree* op1;
        GenTree* op2;

        default:
            info->dstCount = tree->IsValue() ? 1 : 0;
            if (kind & (GTK_CONST | GTK_LEAF))
            {
                info->srcCount = 0;
            }
            else if (kind & (GTK_SMPOP))
            {
                if (tree->gtGetOp2IfPresent() != nullptr)
                {
                    info->srcCount = 2;
                }
                else
                {
                    info->srcCount = 1;
                }
            }
            else
            {
                unreached();
            }
            break;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            info->srcCount = 1;
            info->dstCount = 0;
            LowerStoreLoc(tree->AsLclVarCommon());
            TreeNodeInfoInitStoreLoc(tree->AsLclVarCommon());
            break;

        case GT_BOX:
            noway_assert(!"box should not exist here");
            // The result of 'op1' is also the final result
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_PHYSREGDST:
            info->srcCount = 1;
            info->dstCount = 0;
            break;

        case GT_COMMA:
        {
            GenTreePtr firstOperand;
            GenTreePtr secondOperand;
            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                firstOperand  = tree->gtOp.gtOp2;
                secondOperand = tree->gtOp.gtOp1;
            }
            else
            {
                firstOperand  = tree->gtOp.gtOp1;
                secondOperand = tree->gtOp.gtOp2;
            }
            if (firstOperand->TypeGet() != TYP_VOID)
            {
                firstOperand->gtLsraInfo.isLocalDefUse = true;
                firstOperand->gtLsraInfo.dstCount      = 0;
            }
            if (tree->TypeGet() == TYP_VOID && secondOperand->TypeGet() != TYP_VOID)
            {
                secondOperand->gtLsraInfo.isLocalDefUse = true;
                secondOperand->gtLsraInfo.dstCount      = 0;
            }
        }

            __fallthrough;

        case GT_LIST:
        case GT_FIELD_LIST:
        case GT_ARGPLACE:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_CNS_DBL:
            info->srcCount = 0;
            info->dstCount = 1;
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

        case GT_QMARK:
        case GT_COLON:
            info->srcCount = 0;
            info->dstCount = 0;
            unreached();
            break;

        case GT_RETURN:
            TreeNodeInfoInitReturn(tree);
            break;

        case GT_RETFILT:
            if (tree->TypeGet() == TYP_VOID)
            {
                info->srcCount = 0;
                info->dstCount = 0;
            }
            else
            {
                assert(tree->TypeGet() == TYP_INT);

                info->srcCount = 1;
                info->dstCount = 0;

                info->setSrcCandidates(l, RBM_INTRET);
                tree->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, RBM_INTRET);
            }
            break;

        case GT_NOP:
            // A GT_NOP is either a passthrough (if it is void, or if it has
            // a child), but must be considered to produce a dummy value if it
            // has a type but no child
            info->srcCount = 0;
            if (tree->TypeGet() != TYP_VOID && tree->gtOp.gtOp1 == nullptr)
            {
                info->dstCount = 1;
            }
            else
            {
                info->dstCount = 0;
            }
            break;

        case GT_JTRUE:
            info->srcCount = 0;
            info->dstCount = 0;
            l->clearDstCount(tree->gtOp.gtOp1);
            break;

        case GT_JMP:
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_SWITCH:
            // This should never occur since switch nodes must not be visible at this
            // point in the JIT.
            info->srcCount = 0;
            info->dstCount = 0; // To avoid getting uninit errors.
            noway_assert(!"Switch must be lowered at this point");
            break;

        case GT_JMPTABLE:
            info->srcCount = 0;
            info->dstCount = 1;
            break;

        case GT_SWITCH_TABLE:
            info->srcCount         = 2;
            info->internalIntCount = 1;
            info->dstCount         = 0;
            break;

        case GT_ASG:
        case GT_ASG_ADD:
        case GT_ASG_SUB:
            noway_assert(!"We should never hit any assignment operator in lowering");
            info->srcCount = 0;
            info->dstCount = 0;
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

                info->srcCount = 2;
                info->dstCount = 1;

                break;
            }

            __fallthrough;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            info->srcCount = 2;
            info->dstCount = 1;
            // Check and make op2 contained (if it is a containable immediate)
            CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
            break;

        case GT_RETURNTRAP:
            // this just turns into a compare of its child with an int
            // + a conditional call
            info->srcCount = 1;
            info->dstCount = 0;
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
            info->srcCount = 2;
            info->dstCount = 1;
        }
        break;

        case GT_INTRINSIC:
        {
            // TODO-ARM64-NYI
            // Right now only Abs/Round/Sqrt are treated as math intrinsics
            noway_assert((tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Abs) ||
                         (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Round) ||
                         (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Sqrt));

            // Both operand and its result must be of the same floating point type.
            op1 = tree->gtOp.gtOp1;
            assert(varTypeIsFloating(op1));
            assert(op1->TypeGet() == tree->TypeGet());

            info->srcCount = 1;
            info->dstCount = 1;
        }
        break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            TreeNodeInfoInitSIMD(tree);
            break;
#endif // FEATURE_SIMD

        case GT_CAST:
        {
            // TODO-ARM64-CQ: Int-To-Int conversions - castOp cannot be a memory op and must have an assigned
            //                register.
            //         see CodeGen::genIntToIntCast()

            info->srcCount = 1;
            info->dstCount = 1;

            // Non-overflow casts to/from float/double are done using SSE2 instructions
            // and that allow the source operand to be either a reg or memop. Given the
            // fact that casts from small int to float/double are done as two-level casts,
            // the source operand is always guaranteed to be of size 4 or 8 bytes.
            var_types  castToType = tree->CastToType();
            GenTreePtr castOp     = tree->gtCast.CastOp();
            var_types  castOpType = castOp->TypeGet();
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                castOpType = genUnsignedType(castOpType);
            }
#ifdef DEBUG
            if (!tree->gtOverflow() && (varTypeIsFloating(castToType) || varTypeIsFloating(castOpType)))
            {
                // If converting to float/double, the operand must be 4 or 8 byte in size.
                if (varTypeIsFloating(castToType))
                {
                    unsigned opSize = genTypeSize(castOpType);
                    assert(opSize == 4 || opSize == 8);
                }
            }
#endif // DEBUG
            // Some overflow checks need a temp reg

            CastInfo castInfo;

            // Get information about the cast.
            getCastDescription(tree, &castInfo);

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
            info->srcCount = 1;
            info->dstCount = 1;
            break;

        case GT_NOT:
            info->srcCount = 1;
            info->dstCount = 1;
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            TreeNodeInfoInitShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            TreeNodeInfoInitCmp(tree);
            break;

        case GT_CKFINITE:
            info->srcCount         = 1;
            info->dstCount         = 1;
            info->internalIntCount = 1;
            break;

        case GT_CMPXCHG:
            info->srcCount = 3;
            info->dstCount = 1;

            // TODO-ARM64-NYI
            NYI("CMPXCHG");
            break;

        case GT_LOCKADD:
            info->srcCount = 2;
            info->dstCount = 0;
            CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
            break;

        case GT_CALL:
            TreeNodeInfoInitCall(tree->AsCall());
            break;

        case GT_ADDR:
        {
            // For a GT_ADDR, the child node should not be evaluated into a register
            GenTreePtr child = tree->gtOp.gtOp1;
            assert(!l->isCandidateLocalRef(child));
            l->clearDstCount(child);
            info->srcCount = 0;
            info->dstCount = 1;
        }
        break;

        case GT_BLK:
        case GT_DYN_BLK:
            // These should all be eliminated prior to Lowering.
            assert(!"Non-store block node in Lowering");
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_STORE_BLK:
        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
            LowerBlockStore(tree->AsBlk());
            TreeNodeInfoInitBlockStore(tree->AsBlk());
            break;

        case GT_INIT_VAL:
            // Always a passthrough of its child's value.
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_LCLHEAP:
        {
            info->srcCount = 1;
            info->dstCount = 1;

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

            GenTreePtr size = tree->gtOp.gtOp1;
            if (size->IsCnsIntOrI())
            {
                MakeSrcContained(tree, size);

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
            info->srcCount = 2;
            info->dstCount = 0;

            GenTree* intCns = nullptr;
            GenTree* other  = nullptr;
            if (CheckImmedAndMakeContained(tree, node->gtIndex))
            {
                intCns = node->gtIndex;
                other  = node->gtArrLen;
            }
            else if (CheckImmedAndMakeContained(tree, node->gtArrLen))
            {
                intCns = node->gtArrLen;
                other  = node->gtIndex;
            }
            else
            {
                other = node->gtIndex;
            }
        }
        break;

        case GT_ARR_ELEM:
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM in lowering");
            info->srcCount = 0;
            info->dstCount = 0;
            break;

        case GT_ARR_INDEX:
            info->srcCount               = 2;
            info->dstCount               = 1;
            info->internalIntCount       = 1;
            info->isInternalRegDelayFree = true;

            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            tree->AsArrIndex()->ArrObj()->gtLsraInfo.isDelayFree = true;
            info->hasDelayFreeSrc                                = true;
            break;

        case GT_ARR_OFFSET:
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            info->srcCount         = 3;
            info->dstCount         = 1;
            info->internalIntCount = 1;

            // we don't want to generate code for this
            if (tree->gtArrOffs.gtOffset->IsIntegralConst(0))
            {
                MakeSrcContained(tree, tree->gtArrOffs.gtOffset);
            }
            break;

        case GT_LEA:
        {
            GenTreeAddrMode* lea = tree->AsAddrMode();

            GenTree* base  = lea->Base();
            GenTree* index = lea->Index();
            unsigned cns   = lea->gtOffset;

            // This LEA is instantiating an address,
            // so we set up the srcCount and dstCount here.
            info->srcCount = 0;
            if (base != nullptr)
            {
                info->srcCount++;
            }
            if (index != nullptr)
            {
                info->srcCount++;
            }
            info->dstCount = 1;

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
            info->srcCount = 2;
            info->dstCount = 0;
            GenTree* src   = tree->gtOp.gtOp2;

            if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
            {
                TreeNodeInfoInitGCWriteBarrier(tree);
                break;
            }
            if (!varTypeIsFloating(src->TypeGet()) && src->IsIntegralConst(0))
            {
                // an integer zero for 'src' can be contained.
                MakeSrcContained(tree, src);
            }

            TreeNodeInfoInitIndir(tree);
        }
        break;

        case GT_NULLCHECK:
            info->dstCount      = 0;
            info->srcCount      = 1;
            info->isLocalDefUse = true;
            // null check is an indirection on an addr
            TreeNodeInfoInitIndir(tree);
            break;

        case GT_IND:
            info->dstCount = 1;
            info->srcCount = 1;
            TreeNodeInfoInitIndir(tree);
            break;

        case GT_CATCH_ARG:
            info->srcCount = 0;
            info->dstCount = 1;
            info->setDstCandidates(l, RBM_EXCEPTION_OBJECT);
            break;

        case GT_CLS_VAR:
            info->srcCount = 0;
            // GT_CLS_VAR, by the time we reach the backend, must always
            // be a pure use.
            // It will produce a result of the type of the
            // node, and use an internal register for the address.

            info->dstCount = 1;
            assert((tree->gtFlags & (GTF_VAR_DEF | GTF_VAR_USEASG | GTF_VAR_USEDEF)) == 0);
            info->internalIntCount = 1;
            break;
    } // end switch (tree->OperGet())

    // We need to be sure that we've set info->srcCount and info->dstCount appropriately
    assert((info->dstCount < 2) || tree->IsMultiRegCall());
}

//------------------------------------------------------------------------
// TreeNodeInfoInitReturn: Set the NodeInfo for a GT_RETURN.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitReturn(GenTree* tree)
{
    TreeNodeInfo* info     = &(tree->gtLsraInfo);
    LinearScan*   l        = m_lsra;
    Compiler*     compiler = comp;

    GenTree*  op1           = tree->gtGetOp1();
    regMaskTP useCandidates = RBM_NONE;

    info->srcCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
    info->dstCount = 0;

    if (varTypeIsStruct(tree))
    {
        // op1 has to be either an lclvar or a multi-reg returning call
        if ((op1->OperGet() == GT_LCL_VAR) || (op1->OperGet() == GT_LCL_FLD))
        {
            GenTreeLclVarCommon* lclVarCommon = op1->AsLclVarCommon();
            LclVarDsc*           varDsc       = &(compiler->lvaTable[lclVarCommon->gtLclNum]);
            assert(varDsc->lvIsMultiRegRet);

            // Mark var as contained if not enregistrable.
            if (!varTypeIsEnregisterableStruct(op1))
            {
                MakeSrcContained(tree, op1);
            }
        }
        else
        {
            noway_assert(op1->IsMultiRegCall());

            ReturnTypeDesc* retTypeDesc = op1->AsCall()->GetReturnTypeDesc();
            info->srcCount              = retTypeDesc->GetReturnRegCount();
            useCandidates               = retTypeDesc->GetABIReturnRegs();
        }
    }
    else
    {
        // Non-struct type return - determine useCandidates
        switch (tree->TypeGet())
        {
            case TYP_VOID:
                useCandidates = RBM_NONE;
                break;
            case TYP_FLOAT:
                useCandidates = RBM_FLOATRET;
                break;
            case TYP_DOUBLE:
                useCandidates = RBM_DOUBLERET;
                break;
            case TYP_LONG:
                useCandidates = RBM_LNGRET;
                break;
            default:
                useCandidates = RBM_INTRET;
                break;
        }
    }

    if (useCandidates != RBM_NONE)
    {
        tree->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, useCandidates);
    }
}

#ifdef FEATURE_SIMD
//------------------------------------------------------------------------
// TreeNodeInfoInitSIMD: Set the NodeInfo for a GT_SIMD tree.
//
// Arguments:
//    tree       - The GT_SIMD node of interest
//
// Return Value:
//    None.

void Lowering::TreeNodeInfoInitSIMD(GenTree* tree)
{
    NYI("TreeNodeInfoInitSIMD");
    GenTreeSIMD*  simdTree = tree->AsSIMD();
    TreeNodeInfo* info     = &(tree->gtLsraInfo);
    LinearScan*   lsra     = m_lsra;
    info->dstCount         = 1;
    switch (simdTree->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicInit:
        {
            // This sets all fields of a SIMD struct to the given value.
            // Mark op1 as contained if it is either zero or int constant of all 1's.
            info->srcCount = 1;
            GenTree* op1   = tree->gtOp.gtOp1;
            if (op1->IsIntegralConst(0) || (simdTree->gtSIMDBaseType == TYP_INT && op1->IsCnsIntOrI() &&
                                            op1->AsIntConCommon()->IconValue() == 0xffffffff) ||
                (simdTree->gtSIMDBaseType == TYP_LONG && op1->IsCnsIntOrI() &&
                 op1->AsIntConCommon()->IconValue() == 0xffffffffffffffffLL))
            {
                MakeSrcContained(tree, tree->gtOp.gtOp1);
                info->srcCount = 0;
            }
        }
        break;

        case SIMDIntrinsicInitN:
            info->srcCount = (int)(simdTree->gtSIMDSize / genTypeSize(simdTree->gtSIMDBaseType));
            // Need an internal register to stitch together all the values into a single vector in an XMM reg.
            info->internalFloatCount = 1;
            info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            break;

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            info->srcCount = 2;
            CheckImmedAndMakeContained(tree, tree->gtGetOp2());
            break;

        case SIMDIntrinsicDiv:
            // SSE2 has no instruction support for division on integer vectors
            noway_assert(varTypeIsFloating(simdTree->gtSIMDBaseType));
            info->srcCount = 2;
            break;

        case SIMDIntrinsicAbs:
            // This gets implemented as bitwise-And operation with a mask
            // and hence should never see it here.
            unreached();
            break;

        case SIMDIntrinsicSqrt:
            // SSE2 has no instruction support for sqrt on integer vectors.
            noway_assert(varTypeIsFloating(simdTree->gtSIMDBaseType));
            info->srcCount = 1;
            break;

        case SIMDIntrinsicAdd:
        case SIMDIntrinsicSub:
        case SIMDIntrinsicMul:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseAndNot:
        case SIMDIntrinsicBitwiseOr:
        case SIMDIntrinsicBitwiseXor:
        case SIMDIntrinsicMin:
        case SIMDIntrinsicMax:
            info->srcCount = 2;

            // SSE2 32-bit integer multiplication requires two temp regs
            if (simdTree->gtSIMDIntrinsicID == SIMDIntrinsicMul && simdTree->gtSIMDBaseType == TYP_INT)
            {
                info->internalFloatCount = 2;
                info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            }
            break;

        case SIMDIntrinsicEqual:
            info->srcCount = 2;
            break;

        // SSE2 doesn't support < and <= directly on int vectors.
        // Instead we need to use > and >= with swapped operands.
        case SIMDIntrinsicLessThan:
        case SIMDIntrinsicLessThanOrEqual:
            info->srcCount = 2;
            noway_assert(!varTypeIsIntegral(simdTree->gtSIMDBaseType));
            break;

        // SIMDIntrinsicEqual is supported only on non-floating point base type vectors.
        // SSE2 cmpps/pd doesn't support > and >=  directly on float/double vectors.
        // Instead we need to use <  and <= with swapped operands.
        case SIMDIntrinsicGreaterThan:
            noway_assert(!varTypeIsFloating(simdTree->gtSIMDBaseType));
            info->srcCount = 2;
            break;

        case SIMDIntrinsicGreaterThanOrEqual:
            noway_assert(!varTypeIsFloating(simdTree->gtSIMDBaseType));
            info->srcCount = 2;

            // a >= b = (a==b) | (a>b)
            // To hold intermediate result of a==b and a>b we need two distinct
            // registers.  We can use targetReg and one internal reg provided
            // they are distinct which is not guaranteed. Therefore, we request
            // two internal registers so that one of the internal registers has
            // to be different from targetReg.
            info->internalFloatCount = 2;
            info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            break;

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            // Need two SIMD registers as scratch.
            // See genSIMDIntrinsicRelOp() for details on code sequence generate and
            // the need for two scratch registers.
            info->srcCount           = 2;
            info->internalFloatCount = 2;
            info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            break;

        case SIMDIntrinsicDotProduct:
            // Also need an internal register as scratch. Further we need that targetReg and internal reg
            // are two distinct regs.  It is achieved by requesting two internal registers and one of them
            // has to be different from targetReg.
            //
            // See genSIMDIntrinsicDotProduct() for details on code sequence generated and
            // the need for scratch registers.
            info->srcCount           = 2;
            info->internalFloatCount = 2;
            info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            break;

        case SIMDIntrinsicGetItem:
            // This implements get_Item method. The sources are:
            //  - the source SIMD struct
            //  - index (which element to get)
            // The result is baseType of SIMD struct.
            info->srcCount = 2;

            op2 = tree->gtGetOp2();

            // If the index is a constant, mark it as contained.
            if (CheckImmedAndMakeContained(tree, op2))
            {
                info->srcCount = 1;
            }

            // If the index is not a constant, we will use the SIMD temp location to store the vector.
            // Otherwise, if the baseType is floating point, the targetReg will be a xmm reg and we
            // can use that in the process of extracting the element.
            // In all other cases with constant index, we need a temp xmm register to extract the
            // element if index is other than zero.
            if (!op2->IsCnsIntOrI())
            {
                (void)comp->getSIMDInitTempVarNum();
            }
            else if (!varTypeIsFloating(simdTree->gtSIMDBaseType) && !op2->IsIntegralConst(0))
            {
                info->internalFloatCount = 1;
                info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            }
            break;

        case SIMDIntrinsicCast:
            info->srcCount = 1;
            break;

        // These should have been transformed in terms of other intrinsics
        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            assert("OpEquality/OpInEquality intrinsics should not be seen during Lowering.");
            unreached();

        case SIMDIntrinsicGetX:
        case SIMDIntrinsicGetY:
        case SIMDIntrinsicGetZ:
        case SIMDIntrinsicGetW:
        case SIMDIntrinsicGetOne:
        case SIMDIntrinsicGetZero:
        case SIMDIntrinsicGetLength:
        case SIMDIntrinsicGetAllOnes:
            assert(!"Get intrinsics should not be seen during Lowering.");
            unreached();

        default:
            noway_assert(!"Unimplemented SIMD node type.");
            unreached();
    }
}
#endif // FEATURE_SIMD

#endif // _TARGET_ARM64_

#endif // !LEGACY_BACKEND
