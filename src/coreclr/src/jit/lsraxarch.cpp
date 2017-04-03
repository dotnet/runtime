// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Register Requirements for AMD64                        XX
XX                                                                           XX
XX  This encapsulates all the logic for setting register requirements for    XX
XX  the AMD64 architecture.                                                  XX
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

#ifdef _TARGET_XARCH_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

//------------------------------------------------------------------------
// TreeNodeInfoInitStoreLoc: Set register requirements for a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Setting the appropriate candidates for a store of a multi-reg call return value.
//    - Requesting an internal register for SIMD12 stores.
//    - Handling of contained immediates.
//    - Widening operations of unsigneds. (TODO: Move to 1st phase of Lowering)

void Lowering::TreeNodeInfoInitStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    TreeNodeInfo* info = &(storeLoc->gtLsraInfo);

    // Is this the case of var = call where call is returning
    // a value in multiple return registers?
    GenTree* op1 = storeLoc->gtGetOp1();
    if (op1->IsMultiRegCall())
    {
        // backend expects to see this case only for store lclvar.
        assert(storeLoc->OperGet() == GT_STORE_LCL_VAR);

        // srcCount = number of registers in which the value is returned by call
        GenTreeCall*    call        = op1->AsCall();
        ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
        info->srcCount              = retTypeDesc->GetReturnRegCount();

        // Call node srcCandidates = Bitwise-OR(allregs(GetReturnRegType(i))) for all i=0..RetRegCount-1
        regMaskTP srcCandidates = m_lsra->allMultiRegCallNodeRegs(call);
        op1->gtLsraInfo.setSrcCandidates(m_lsra, srcCandidates);
        return;
    }

#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(storeLoc))
    {
        if (op1->IsCnsIntOrI())
        {
            // InitBlk
            MakeSrcContained(storeLoc, op1);
        }
        else if (storeLoc->TypeGet() == TYP_SIMD12)
        {
            // Need an additional register to extract upper 4 bytes of Vector3.
            info->internalFloatCount = 1;
            info->setInternalCandidates(m_lsra, m_lsra->allSIMDRegs());

            // In this case don't mark the operand as contained as we want it to
            // be evaluated into an xmm register
        }
        return;
    }
#endif // FEATURE_SIMD

    // If the source is a containable immediate, make it contained, unless it is
    // an int-size or larger store of zero to memory, because we can generate smaller code
    // by zeroing a register and then storing it.
    if (IsContainableImmed(storeLoc, op1) && (!op1->IsIntegralConst(0) || varTypeIsSmall(storeLoc)))
    {
        MakeSrcContained(storeLoc, op1);
    }

    // TODO: This should be moved to Lowering, but it widens the types, which changes the behavior
    // of the above condition.
    LowerStoreLoc(storeLoc);
}

//------------------------------------------------------------------------
// TreeNodeInfoInit: Set register requirements for a node
//
// Arguments:
//    treeNode - the node of interest
//
// Notes:
// Preconditions:
//    LSRA Has been initialized and there is a TreeNodeInfo node
//    already allocated and initialized for every tree in the IR.
// Postconditions:
//    Every TreeNodeInfo instance has the right annotations on register
//    requirements needed by LSRA to build the Interval Table (source,
//    destination and internal [temp] register counts).
//
void Lowering::TreeNodeInfoInit(GenTree* tree)
{
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    TreeNodeInfo* info = &(tree->gtLsraInfo);
#ifdef DEBUG
    if (comp->verbose)
    {
        printf("TreeNodeInfoInit:\n");
        comp->gtDispTreeRange(BlockRange(), tree);
    }
#endif
    // floating type generates AVX instruction (vmovss etc.), set the flag
    SetContainsAVXFlags(varTypeIsFloating(tree->TypeGet()));
    switch (tree->OperGet())
    {
        GenTree* op1;
        GenTree* op2;

        default:
            TreeNodeInfoInitSimple(tree);
            break;

        case GT_LCL_FLD:
        case GT_LCL_VAR:
            info->srcCount = 0;
            info->dstCount = 1;

#ifdef FEATURE_SIMD
            // Need an additional register to read upper 4 bytes of Vector3.
            if (tree->TypeGet() == TYP_SIMD12)
            {
                // We need an internal register different from targetReg in which 'tree' produces its result
                // because both targetReg and internal reg will be in use at the same time.
                info->internalFloatCount     = 1;
                info->isInternalRegDelayFree = true;
                info->setInternalCandidates(m_lsra, m_lsra->allSIMDRegs());
            }
#endif
            break;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
#ifdef _TARGET_X86_
            if (tree->gtGetOp1()->OperGet() == GT_LONG)
            {
                info->srcCount = 2;
            }
            else
#endif // _TARGET_X86_
            {
                info->srcCount = 1;
            }
            info->dstCount = 0;
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
            info->srcCount = 0;
            info->dstCount = 0;
            break;

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
            break;

#if !defined(_TARGET_64BIT_)

        case GT_LONG:
            if ((tree->gtLIRFlags & LIR::Flags::IsUnusedValue) != 0)
            {
                // An unused GT_LONG node needs to consume its sources.
                info->srcCount = 2;
            }
            else
            {
                // Passthrough
                info->srcCount = 0;
            }

            info->dstCount = 0;
            break;

#endif // !defined(_TARGET_64BIT_)

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

        // A GT_NOP is either a passthrough (if it is void, or if it has
        // a child), but must be considered to produce a dummy value if it
        // has a type but no child
        case GT_NOP:
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
        {
            info->srcCount = 0;
            info->dstCount = 0;

            GenTree* cmp = tree->gtGetOp1();
            l->clearDstCount(cmp);

#ifdef FEATURE_SIMD
            // Say we have the following IR
            //   simdCompareResult = GT_SIMD((In)Equality, v1, v2)
            //   integerCompareResult = GT_EQ/NE(simdCompareResult, true/false)
            //   GT_JTRUE(integerCompareResult)
            //
            // In this case we don't need to generate code for GT_EQ_/NE, since SIMD (In)Equality
            // intrinsic will set or clear the Zero flag.

            genTreeOps cmpOper = cmp->OperGet();
            if (cmpOper == GT_EQ || cmpOper == GT_NE)
            {
                GenTree* cmpOp1 = cmp->gtGetOp1();
                GenTree* cmpOp2 = cmp->gtGetOp2();

                if (cmpOp1->IsSIMDEqualityOrInequality() && (cmpOp2->IsIntegralConst(0) || cmpOp2->IsIntegralConst(1)))
                {
                    // We always generate code for a SIMD equality comparison, but the compare
                    // is contained (evaluated as part of the GT_JTRUE).
                    // Neither the SIMD node nor the immediate need to be evaluated into a register.
                    l->clearOperandCounts(cmp);
                    l->clearDstCount(cmpOp1);
                    l->clearOperandCounts(cmpOp2);

                    // Codegen of SIMD (in)Equality uses target integer reg only for setting flags.
                    // A target reg is not needed on AVX when comparing against Vector Zero.
                    // In all other cases we need to reserve an int type internal register, since we
                    // have cleared dstCount.
                    if (!compiler->canUseAVX() || !cmpOp1->gtGetOp2()->IsIntegralConstVector(0))
                    {
                        ++(cmpOp1->gtLsraInfo.internalIntCount);
                        regMaskTP internalCandidates = cmpOp1->gtLsraInfo.getInternalCandidates(l);
                        internalCandidates |= l->allRegs(TYP_INT);
                        cmpOp1->gtLsraInfo.setInternalCandidates(l, internalCandidates);
                    }

                    // We have to reverse compare oper in the following cases:
                    // 1) SIMD Equality: Sets Zero flag on equal otherwise clears it.
                    //    Therefore, if compare oper is == or != against false(0), we will
                    //    be checking opposite of what is required.
                    //
                    // 2) SIMD inEquality: Clears Zero flag on true otherwise sets it.
                    //    Therefore, if compare oper is == or != against true(1), we will
                    //    be checking opposite of what is required.
                    GenTreeSIMD* simdNode = cmpOp1->AsSIMD();
                    if (simdNode->gtSIMDIntrinsicID == SIMDIntrinsicOpEquality)
                    {
                        if (cmpOp2->IsIntegralConst(0))
                        {
                            cmp->SetOper(GenTree::ReverseRelop(cmpOper));
                        }
                    }
                    else
                    {
                        assert(simdNode->gtSIMDIntrinsicID == SIMDIntrinsicOpInEquality);
                        if (cmpOp2->IsIntegralConst(1))
                        {
                            cmp->SetOper(GenTree::ReverseRelop(cmpOper));
                        }
                    }
                }
            }
#endif // FEATURE_SIMD
        }
        break;

        case GT_JCC:
            info->srcCount = 0;
            info->dstCount = 0;
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

#if !defined(_TARGET_64BIT_)
        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
#endif
        case GT_ADD:
        case GT_SUB:
            // SSE2 arithmetic instructions doesn't support the form "op mem, xmm".
            // Rather they only support "op xmm, mem/xmm" form.
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // overflow operations aren't supported on float/double types.
                assert(!tree->gtOverflow());

                op1 = tree->gtGetOp1();
                op2 = tree->gtGetOp2();

                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(op1->TypeGet() == op2->TypeGet());

                info->srcCount = 2;
                info->dstCount = 1;

                if (op2->isMemoryOp() || op2->IsCnsNonZeroFltOrDbl())
                {
                    MakeSrcContained(tree, op2);
                }
                else if (tree->OperIsCommutative() &&
                         (op1->IsCnsNonZeroFltOrDbl() || (op1->isMemoryOp() && IsSafeToContainMem(tree, op1))))
                {
                    // Though we have GT_ADD(op1=memOp, op2=non-memOp, we try to reorder the operands
                    // as long as it is safe so that the following efficient code sequence is generated:
                    //      addss/sd targetReg, memOp    (if op1Reg == targetReg) OR
                    //      movaps targetReg, op2Reg; addss/sd targetReg, [memOp]
                    //
                    // Instead of
                    //      movss op1Reg, [memOp]; addss/sd targetReg, Op2Reg  (if op1Reg == targetReg) OR
                    //      movss op1Reg, [memOp]; movaps targetReg, op1Reg, addss/sd targetReg, Op2Reg
                    MakeSrcContained(tree, op1);
                }
                else
                {
                    // If there are no containable operands, we can make an operand reg optional.
                    SetRegOptionalForBinOp(tree);
                }
                break;
            }

            __fallthrough;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            TreeNodeInfoInitLogicalOp(tree);
            break;

        case GT_RETURNTRAP:
            // This just turns into a compare of its child with an int + a conditional call
            info->srcCount = 1;
            info->dstCount = 0;
            if (tree->gtOp.gtOp1->isIndir())
            {
                MakeSrcContained(tree, tree->gtOp.gtOp1);
            }
            info->internalIntCount = 1;
            info->setInternalCandidates(l, l->allRegs(TYP_INT));
            break;

        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:
            TreeNodeInfoInitModDiv(tree);
            break;

        case GT_MUL:
        case GT_MULHI:
#if defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)
        case GT_MUL_LONG:
#endif
            TreeNodeInfoInitMul(tree);
            break;

        case GT_INTRINSIC:
            TreeNodeInfoInitIntrinsic(tree);
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            TreeNodeInfoInitSIMD(tree);
            break;
#endif // FEATURE_SIMD

        case GT_CAST:
            TreeNodeInfoInitCast(tree);
            break;

        case GT_NEG:
            info->srcCount = 1;
            info->dstCount = 1;

            // TODO-XArch-CQ:
            // SSE instruction set doesn't have an instruction to negate a number.
            // The recommended way is to xor the float/double number with a bitmask.
            // The only way to xor is using xorps or xorpd both of which operate on
            // 128-bit operands.  To hold the bit-mask we would need another xmm
            // register or a 16-byte aligned 128-bit data constant. Right now emitter
            // lacks the support for emitting such constants or instruction with mem
            // addressing mode referring to a 128-bit operand. For now we use an
            // internal xmm register to load 32/64-bit bitmask from data section.
            // Note that by trading additional data section memory (128-bit) we can
            // save on the need for an internal register and also a memory-to-reg
            // move.
            //
            // Note: another option to avoid internal register requirement is by
            // lowering as GT_SUB(0, src).  This will generate code different from
            // Jit64 and could possibly result in compat issues (?).
            if (varTypeIsFloating(tree))
            {
                info->internalFloatCount = 1;
                info->setInternalCandidates(l, l->internalFloatRegCandidates());
            }
            else
            {
                // Codegen of this tree node sets ZF and SF flags.
                tree->gtFlags |= GTF_ZSF_SET;
            }
            break;

        case GT_NOT:
            info->srcCount = 1;
            info->dstCount = 1;
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
#ifdef _TARGET_X86_
        case GT_LSH_HI:
        case GT_RSH_LO:
#endif
            TreeNodeInfoInitShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
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

            // comparand is preferenced to RAX.
            // Remaining two operands can be in any reg other than RAX.
            tree->gtCmpXchg.gtOpComparand->gtLsraInfo.setSrcCandidates(l, RBM_RAX);
            tree->gtCmpXchg.gtOpLocation->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~RBM_RAX);
            tree->gtCmpXchg.gtOpValue->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~RBM_RAX);
            tree->gtLsraInfo.setDstCandidates(l, RBM_RAX);
            break;

        case GT_LOCKADD:
            info->srcCount = 2;
            info->dstCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;

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

#if !defined(FEATURE_PUT_STRUCT_ARG_STK)
        case GT_OBJ:
#endif
        case GT_BLK:
        case GT_DYN_BLK:
            // These should all be eliminated prior to Lowering.
            assert(!"Non-store block node in Lowering");
            info->srcCount = 0;
            info->dstCount = 0;
            break;

#ifdef FEATURE_PUT_STRUCT_ARG_STK
        case GT_PUTARG_STK:
            LowerPutArgStk(tree->AsPutArgStk());
            TreeNodeInfoInitPutArgStk(tree->AsPutArgStk());
            break;
#endif // FEATURE_PUT_STRUCT_ARG_STK

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
            TreeNodeInfoInitLclHeap(tree);
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

            GenTreePtr other;
            if (CheckImmedAndMakeContained(tree, node->gtIndex))
            {
                other = node->gtArrLen;
            }
            else if (CheckImmedAndMakeContained(tree, node->gtArrLen))
            {
                other = node->gtIndex;
            }
            else if (node->gtIndex->isMemoryOp())
            {
                other = node->gtIndex;
            }
            else
            {
                other = node->gtArrLen;
            }

            if (node->gtIndex->TypeGet() == node->gtArrLen->TypeGet())
            {
                if (other->isMemoryOp())
                {
                    MakeSrcContained(tree, other);
                }
                else
                {
                    // We can mark 'other' as reg optional, since it is not contained.
                    SetRegOptional(other);
                }
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
            info->srcCount = 2;
            info->dstCount = 1;
            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            tree->AsArrIndex()->ArrObj()->gtLsraInfo.isDelayFree = true;
            info->hasDelayFreeSrc                                = true;
            break;

        case GT_ARR_OFFSET:
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            info->srcCount = 3;
            info->dstCount = 1;

            // we don't want to generate code for this
            if (tree->gtArrOffs.gtOffset->IsIntegralConst(0))
            {
                MakeSrcContained(tree, tree->gtArrOffs.gtOffset);
            }
            else
            {
                // Here we simply need an internal register, which must be different
                // from any of the operand's registers, but may be the same as targetReg.
                info->internalIntCount = 1;
            }
            break;

        case GT_LEA:
            // The LEA usually passes its operands through to the GT_IND, in which case we'll
            // clear the info->srcCount and info->dstCount later, but we may be instantiating an address,
            // so we set them here.
            info->srcCount = 0;
            if (tree->AsAddrMode()->HasBase())
            {
                info->srcCount++;
            }
            if (tree->AsAddrMode()->HasIndex())
            {
                info->srcCount++;
            }
            info->dstCount = 1;
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

            // If the source is a containable immediate, make it contained, unless it is
            // an int-size or larger store of zero to memory, because we can generate smaller code
            // by zeroing a register and then storing it.
            if (IsContainableImmed(tree, src) &&
                (!src->IsIntegralConst(0) || varTypeIsSmall(tree) || tree->gtGetOp1()->OperGet() == GT_CLS_VAR_ADDR))
            {
                MakeSrcContained(tree, src);
            }
            else if (!varTypeIsFloating(tree))
            {
                // Perform recognition of trees with the following structure:
                //        StoreInd(addr, BinOp(expr, GT_IND(addr)))
                // to be able to fold this into an instruction of the form
                //        BINOP [addr], register
                // where register is the actual place where 'expr' is computed.
                //
                // SSE2 doesn't support RMW form of instructions.
                if (TreeNodeInfoInitIfRMWMemOp(tree))
                {
                    break;
                }
            }

            TreeNodeInfoInitIndir(tree);
        }
        break;

        case GT_NULLCHECK:
            info->dstCount      = 0;
            info->srcCount      = 1;
            info->isLocalDefUse = true;
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

#if !FEATURE_EH_FUNCLETS
        case GT_END_LFIN:
            info->srcCount = 0;
            info->dstCount = 0;
            break;
#endif

        case GT_CLS_VAR:
            // These nodes are eliminated by rationalizer.
            JITDUMP("Unexpected node %s in Lower.\n", GenTree::NodeName(tree->OperGet()));
            unreached();
            break;
    } // end switch (tree->OperGet())

    // If op2 of a binary-op gets marked as contained, then binary-op srcCount will be 1.
    // Even then we would like to set isTgtPref on Op1.
    if (tree->OperIsBinary() && info->srcCount >= 1)
    {
        if (isRMWRegOper(tree))
        {
            GenTree* op1 = tree->gtOp.gtOp1;
            GenTree* op2 = tree->gtOp.gtOp2;

            // Commutative opers like add/mul/and/or/xor could reverse the order of
            // operands if it is safe to do so.  In such a case we would like op2 to be
            // target preferenced instead of op1.
            if (tree->OperIsCommutative() && op1->gtLsraInfo.dstCount == 0 && op2 != nullptr)
            {
                op1 = op2;
                op2 = tree->gtOp.gtOp1;
            }

            // If we have a read-modify-write operation, we want to preference op1 to the target.
            // If op1 is contained, we don't want to preference it, but it won't
            // show up as a source in that case, so it will be ignored.
            op1->gtLsraInfo.isTgtPref = true;

            // Is this a non-commutative operator, or is op2 a contained memory op?
            // (Note that we can't call IsContained() at this point because it uses exactly the
            // same information we're currently computing.)
            // In either case, we need to make op2 remain live until the op is complete, by marking
            // the source(s) associated with op2 as "delayFree".
            // Note that if op2 of a binary RMW operator is a memory op, even if the operator
            // is commutative, codegen cannot reverse them.
            // TODO-XArch-CQ: This is not actually the case for all RMW binary operators, but there's
            // more work to be done to correctly reverse the operands if they involve memory
            // operands.  Also, we may need to handle more cases than GT_IND, especially once
            // we've modified the register allocator to not require all nodes to be assigned
            // a register (e.g. a spilled lclVar can often be referenced directly from memory).
            // Note that we may have a null op2, even with 2 sources, if op1 is a base/index memory op.

            GenTree* delayUseSrc = nullptr;
            // TODO-XArch-Cleanup: We should make the indirection explicit on these nodes so that we don't have
            // to special case them.
            if (tree->OperGet() == GT_XADD || tree->OperGet() == GT_XCHG || tree->OperGet() == GT_LOCKADD)
            {
                // These tree nodes will have their op1 marked as isDelayFree=true.
                // Hence these tree nodes should have a Def position so that op1's reg
                // gets freed at DefLoc+1.
                if (tree->TypeGet() == TYP_VOID)
                {
                    // Right now a GT_XADD node could be morphed into a
                    // GT_LOCKADD of TYP_VOID. See gtExtractSideEffList().
                    // Note that it is advantageous to use GT_LOCKADD
                    // instead of of GT_XADD as the former uses lock.add,
                    // which allows its second operand to be a contained
                    // immediate wheres xadd instruction requires its
                    // second operand to be in a register.
                    assert(tree->gtLsraInfo.dstCount == 0);

                    // Give it an artificial type and mark it isLocalDefUse = true.
                    // This would result in a Def position created but not considered
                    // consumed by its parent node.
                    tree->gtType                   = TYP_INT;
                    tree->gtLsraInfo.isLocalDefUse = true;
                }
                else
                {
                    assert(tree->gtLsraInfo.dstCount != 0);
                }

                delayUseSrc = op1;
            }
            else if ((op2 != nullptr) &&
                     (!tree->OperIsCommutative() || (op2->isMemoryOp() && (op2->gtLsraInfo.srcCount == 0))))
            {
                delayUseSrc = op2;
            }
            if (delayUseSrc != nullptr)
            {
                // If delayUseSrc is an indirection and it doesn't produce a result, then we need to set "delayFree'
                // on the base & index, if any.
                // Otherwise, we set it on delayUseSrc itself.
                if (delayUseSrc->isIndir() && (delayUseSrc->gtLsraInfo.dstCount == 0))
                {
                    GenTree* base  = delayUseSrc->AsIndir()->Base();
                    GenTree* index = delayUseSrc->AsIndir()->Index();
                    if (base != nullptr)
                    {
                        base->gtLsraInfo.isDelayFree = true;
                    }
                    if (index != nullptr)
                    {
                        index->gtLsraInfo.isDelayFree = true;
                    }
                }
                else
                {
                    delayUseSrc->gtLsraInfo.isDelayFree = true;
                }
                info->hasDelayFreeSrc = true;
            }
        }
    }

    TreeNodeInfoInitCheckByteable(tree);

    // We need to be sure that we've set info->srcCount and info->dstCount appropriately
    assert((info->dstCount < 2) || (tree->IsMultiRegCall() && info->dstCount == MAX_RET_REG_COUNT));
}

//------------------------------------------------------------------------
// TreeNodeInfoInitCheckByteable: Check the tree to see if "byte-able" registers are
// required, and set the tree node info accordingly.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitCheckByteable(GenTree* tree)
{
#ifdef _TARGET_X86_
    LinearScan*   l    = m_lsra;
    TreeNodeInfo* info = &(tree->gtLsraInfo);

    // Exclude RBM_NON_BYTE_REGS from dst candidates of tree node and src candidates of operands
    // if the tree node is a byte type.
    //
    // Though this looks conservative in theory, in practice we could not think of a case where
    // the below logic leads to conservative register specification.  In future when or if we find
    // one such case, this logic needs to be fine tuned for that case(s).

    if (ExcludeNonByteableRegisters(tree))
    {
        regMaskTP regMask;
        if (info->dstCount > 0)
        {
            regMask = info->getDstCandidates(l);
            assert(regMask != RBM_NONE);
            info->setDstCandidates(l, regMask & ~RBM_NON_BYTE_REGS);
        }

        if (tree->OperIsSimple() && (info->srcCount > 0))
        {
            // No need to set src candidates on a contained child operand.
            GenTree* op = tree->gtOp.gtOp1;
            assert(op != nullptr);
            bool containedNode = (op->gtLsraInfo.srcCount == 0) && (op->gtLsraInfo.dstCount == 0);
            if (!containedNode)
            {
                regMask = op->gtLsraInfo.getSrcCandidates(l);
                assert(regMask != RBM_NONE);
                op->gtLsraInfo.setSrcCandidates(l, regMask & ~RBM_NON_BYTE_REGS);
            }

            if (tree->OperIsBinary() && (tree->gtOp.gtOp2 != nullptr))
            {
                op            = tree->gtOp.gtOp2;
                containedNode = (op->gtLsraInfo.srcCount == 0) && (op->gtLsraInfo.dstCount == 0);
                if (!containedNode)
                {
                    regMask = op->gtLsraInfo.getSrcCandidates(l);
                    assert(regMask != RBM_NONE);
                    op->gtLsraInfo.setSrcCandidates(l, regMask & ~RBM_NON_BYTE_REGS);
                }
            }
        }
    }
#endif //_TARGET_X86_
}

//------------------------------------------------------------------------
// TreeNodeInfoInitSimple: Sets the srcCount and dstCount for all the trees
// without special handling based on the tree node type.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitSimple(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    unsigned      kind = tree->OperKind();
    info->dstCount     = tree->IsValue() ? 1 : 0;
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

#if !defined(_TARGET_64BIT_)
    if (tree->TypeGet() == TYP_LONG)
    {
        GenTree* op1 = tree->gtGetOp1();
        noway_assert(op1->OperGet() == GT_LONG);
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();
        info->srcCount = 2;
        loVal->gtLsraInfo.setSrcCandidates(l, RBM_LNGRET_LO);
        hiVal->gtLsraInfo.setSrcCandidates(l, RBM_LNGRET_HI);
        info->dstCount = 0;
    }
    else
#endif // !defined(_TARGET_64BIT_)
    {
        GenTree*  op1           = tree->gtGetOp1();
        regMaskTP useCandidates = RBM_NONE;

        info->srcCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
        info->dstCount = 0;

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        if (varTypeIsStruct(tree))
        {
            // op1 has to be either an lclvar or a multi-reg returning call
            if (op1->OperGet() == GT_LCL_VAR)
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
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
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
#if defined(_TARGET_64BIT_)
                case TYP_LONG:
                    useCandidates = RBM_LNGRET;
                    break;
#endif // defined(_TARGET_64BIT_)
                default:
                    useCandidates = RBM_INTRET;
                    break;
            }
        }

        if (useCandidates != RBM_NONE)
        {
            op1->gtLsraInfo.setSrcCandidates(l, useCandidates);
        }
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitShiftRotate: Set the NodeInfo for a shift or rotate.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitShiftRotate(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    LinearScan*   l    = m_lsra;

    info->srcCount = 2;
    info->dstCount = 1;

    // For shift operations, we need that the number
    // of bits moved gets stored in CL in case
    // the number of bits to shift is not a constant.
    GenTreePtr shiftBy = tree->gtOp.gtOp2;
    GenTreePtr source  = tree->gtOp.gtOp1;

#ifdef _TARGET_X86_
    // The first operand of a GT_LSH_HI and GT_RSH_LO oper is a GT_LONG so that
    // we can have a three operand form. Increment the srcCount.
    if (tree->OperGet() == GT_LSH_HI || tree->OperGet() == GT_RSH_LO)
    {
        assert(source->OperGet() == GT_LONG);

        info->srcCount++;

        if (tree->OperGet() == GT_LSH_HI)
        {
            GenTreePtr sourceLo              = source->gtOp.gtOp1;
            sourceLo->gtLsraInfo.isDelayFree = true;
        }
        else
        {
            GenTreePtr sourceHi              = source->gtOp.gtOp2;
            sourceHi->gtLsraInfo.isDelayFree = true;
        }

        source->gtLsraInfo.hasDelayFreeSrc = true;
        info->hasDelayFreeSrc              = true;
    }
#endif

    // x64 can encode 8 bits of shift and it will use 5 or 6. (the others are masked off)
    // We will allow whatever can be encoded - hope you know what you are doing.
    if (!IsContainableImmed(tree, shiftBy) || (shiftBy->gtIntConCommon.IconValue() > 255) ||
        (shiftBy->gtIntConCommon.IconValue() < 0))
    {
        source->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~RBM_RCX);
        shiftBy->gtLsraInfo.setSrcCandidates(l, RBM_RCX);
        info->setDstCandidates(l, l->allRegs(TYP_INT) & ~RBM_RCX);
    }
    else
    {
        MakeSrcContained(tree, shiftBy);

        // Note that Rotate Left/Right instructions don't set ZF and SF flags.
        //
        // If the operand being shifted is 32-bits then upper three bits are masked
        // by hardware to get actual shift count.  Similarly for 64-bit operands
        // shift count is narrowed to [0..63].  If the resulting shift count is zero,
        // then shift operation won't modify flags.
        //
        // TODO-CQ-XARCH: We can optimize generating 'test' instruction for GT_EQ/NE(shift, 0)
        // if the shift count is known to be non-zero and in the range depending on the
        // operand size.
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitPutArgReg: Set the NodeInfo for a PUTARG_REG.
//
// Arguments:
//    node                - The PUTARG_REG node.
//    argReg              - The register in which to pass the argument.
//    info                - The info for the node's using call.
//    isVarArgs           - True if the call uses a varargs calling convention.
//    callHasFloatRegArgs - Set to true if this PUTARG_REG uses an FP register.
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitPutArgReg(
    GenTreeUnOp* node, regNumber argReg, TreeNodeInfo& info, bool isVarArgs, bool* callHasFloatRegArgs)
{
    assert(node != nullptr);
    assert(node->OperIsPutArgReg());
    assert(argReg != REG_NA);

    // Each register argument corresponds to one source.
    info.srcCount++;

    // Set the register requirements for the node.
    const regMaskTP argMask = genRegMask(argReg);
    node->gtLsraInfo.setDstCandidates(m_lsra, argMask);
    node->gtLsraInfo.setSrcCandidates(m_lsra, argMask);

    // To avoid redundant moves, have the argument operand computed in the
    // register in which the argument is passed to the call.
    node->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(m_lsra, m_lsra->getUseCandidates(node));

#if FEATURE_VARARG
    *callHasFloatRegArgs |= varTypeIsFloating(node->TypeGet());

    // In the case of a varargs call, the ABI dictates that if we have floating point args,
    // we must pass the enregistered arguments in both the integer and floating point registers.
    // Since the integer register is not associated with this arg node, we will reserve it as
    // an internal register so that it is not used during the evaluation of the call node
    // (e.g. for the target).
    if (isVarArgs && varTypeIsFloating(node))
    {
        regNumber targetReg = comp->getCallArgIntRegister(argReg);
        info.setInternalIntCount(info.internalIntCount + 1);
        info.addInternalCandidates(m_lsra, genRegMask(targetReg));
    }
#endif // FEATURE_VARARG
}

//------------------------------------------------------------------------
// TreeNodeInfoInitCall: Set the NodeInfo for a call.
//
// Arguments:
//    call      - The call node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitCall(GenTreeCall* call)
{
    TreeNodeInfo*   info              = &(call->gtLsraInfo);
    LinearScan*     l                 = m_lsra;
    Compiler*       compiler          = comp;
    bool            hasMultiRegRetVal = false;
    ReturnTypeDesc* retTypeDesc       = nullptr;

    info->srcCount = 0;
    if (call->TypeGet() != TYP_VOID)
    {
        hasMultiRegRetVal = call->HasMultiRegRetVal();
        if (hasMultiRegRetVal)
        {
            // dst count = number of registers in which the value is returned by call
            retTypeDesc    = call->GetReturnTypeDesc();
            info->dstCount = retTypeDesc->GetReturnRegCount();
        }
        else
        {
            info->dstCount = 1;
        }
    }
    else
    {
        info->dstCount = 0;
    }

    GenTree* ctrlExpr = call->gtControlExpr;
    if (call->gtCallType == CT_INDIRECT)
    {
        // either gtControlExpr != null or gtCallAddr != null.
        // Both cannot be non-null at the same time.
        assert(ctrlExpr == nullptr);
        assert(call->gtCallAddr != nullptr);
        ctrlExpr = call->gtCallAddr;

#ifdef _TARGET_X86_
        // Fast tail calls aren't currently supported on x86, but if they ever are, the code
        // below that handles indirect VSD calls will need to be fixed.
        assert(!call->IsFastTailCall() || !call->IsVirtualStub());
#endif // _TARGET_X86_
    }

    // set reg requirements on call target represented as control sequence.
    if (ctrlExpr != nullptr)
    {
        // we should never see a gtControlExpr whose type is void.
        assert(ctrlExpr->TypeGet() != TYP_VOID);

        // call can take a Rm op on x64
        info->srcCount++;

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (!call->IsFastTailCall())
        {
#ifdef _TARGET_X86_
            // On x86, we need to generate a very specific pattern for indirect VSD calls:
            //
            //    3-byte nop
            //    call dword ptr [eax]
            //
            // Where EAX is also used as an argument to the stub dispatch helper. Make
            // sure that the call target address is computed into EAX in this case.
            if (call->IsVirtualStub() && (call->gtCallType == CT_INDIRECT))
            {
                assert(ctrlExpr->isIndir());

                ctrlExpr->gtGetOp1()->gtLsraInfo.setSrcCandidates(l, RBM_VIRTUAL_STUB_TARGET);
                MakeSrcContained(call, ctrlExpr);
            }
            else
#endif // _TARGET_X86_
                if (ctrlExpr->isIndir())
            {
                MakeSrcContained(call, ctrlExpr);
            }
        }
        else
        {
            // Fast tail call - make sure that call target is always computed in RAX
            // so that epilog sequence can generate "jmp rax" to achieve fast tail call.
            ctrlExpr->gtLsraInfo.setSrcCandidates(l, RBM_RAX);
        }
    }

    // If this is a varargs call, we will clear the internal candidates in case we need
    // to reserve some integer registers for copying float args.
    // We have to do this because otherwise the default candidates are allRegs, and adding
    // the individual specific registers will have no effect.
    if (call->IsVarargs())
    {
        info->setInternalCandidates(l, RBM_NONE);
    }

    RegisterType registerType = call->TypeGet();

    // Set destination candidates for return value of the call.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_X86_
    if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        // The x86 CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
        // TCB in REG_PINVOKE_TCB. AMD64/ARM64 use the standard calling convention. fgMorphCall() sets the
        // correct argument registers.
        info->setDstCandidates(l, RBM_PINVOKE_TCB);
    }
    else
#endif // _TARGET_X86_
        if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        info->setDstCandidates(l, retTypeDesc->GetABIReturnRegs());
    }
    else if (varTypeIsFloating(registerType))
    {
#ifdef _TARGET_X86_
        // The return value will be on the X87 stack, and we will need to move it.
        info->setDstCandidates(l, l->allRegs(registerType));
#else  // !_TARGET_X86_
        info->setDstCandidates(l, RBM_FLOATRET);
#endif // !_TARGET_X86_
    }
    else if (registerType == TYP_LONG)
    {
        info->setDstCandidates(l, RBM_LNGRET);
    }
    else
    {
        info->setDstCandidates(l, RBM_INTRET);
    }

    // number of args to a call =
    // callRegArgs + (callargs - placeholders, setup, etc)
    // there is an explicit thisPtr but it is redundant

    // If there is an explicit this pointer, we don't want that node to produce anything
    // as it is redundant
    if (call->gtCallObjp != nullptr)
    {
        GenTreePtr thisPtrNode = call->gtCallObjp;

        if (thisPtrNode->gtOper == GT_PUTARG_REG)
        {
            l->clearOperandCounts(thisPtrNode);
            l->clearDstCount(thisPtrNode->gtOp.gtOp1);
        }
        else
        {
            l->clearDstCount(thisPtrNode);
        }
    }

    bool callHasFloatRegArgs = false;
    bool isVarArgs           = call->IsVarargs();

    // First, count reg args
    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->OperIsList());

        // By this point, lowering has ensured that all call arguments are one of the following:
        // - an arg setup store
        // - an arg placeholder
        // - a nop
        // - a copy blk
        // - a field list
        // - a put arg
        //
        // Note that this property is statically checked by Lowering::CheckBlock.
        GenTreePtr argNode = list->Current();

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        if (curArgTabEntry->regNum == REG_STK)
        {
            // late arg that is not passed in a register
            DISPNODE(argNode);
            assert(argNode->gtOper == GT_PUTARG_STK);
            argNode->gtLsraInfo.srcCount = 1;
            argNode->gtLsraInfo.dstCount = 0;

#ifdef FEATURE_PUT_STRUCT_ARG_STK
            // If the node is TYP_STRUCT and it is put on stack with
            // putarg_stk operation, we consume and produce no registers.
            // In this case the embedded Obj node should not produce
            // registers too since it is contained.
            // Note that if it is a SIMD type the argument will be in a register.
            if (argNode->TypeGet() == TYP_STRUCT)
            {
                assert(argNode->gtOp.gtOp1 != nullptr && argNode->gtOp.gtOp1->OperGet() == GT_OBJ);
                argNode->gtOp.gtOp1->gtLsraInfo.dstCount = 0;
                argNode->gtLsraInfo.srcCount             = 0;
            }
#endif // FEATURE_PUT_STRUCT_ARG_STK

            continue;
        }

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            assert(varTypeIsStruct(argNode) || curArgTabEntry->isStruct);

            unsigned eightbyte = 0;
            for (GenTreeFieldList* entry = argNode->AsFieldList(); entry != nullptr; entry = entry->Rest())
            {
                const regNumber argReg = eightbyte == 0 ? curArgTabEntry->regNum : curArgTabEntry->otherRegNum;
                TreeNodeInfoInitPutArgReg(entry->Current()->AsUnOp(), argReg, *info, isVarArgs, &callHasFloatRegArgs);

                eightbyte++;
            }
        }
        else
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
        {
            TreeNodeInfoInitPutArgReg(argNode->AsUnOp(), curArgTabEntry->regNum, *info, isVarArgs,
                                      &callHasFloatRegArgs);
        }
    }

    // Now, count stack args
    // Note that these need to be computed into a register, but then
    // they're just stored to the stack - so the reg doesn't
    // need to remain live until the call.  In fact, it must not
    // because the code generator doesn't actually consider it live,
    // so it can't be spilled.

    GenTreePtr args = call->gtCallArgs;
    while (args)
    {
        GenTreePtr arg = args->gtOp.gtOp1;
        if (!(args->gtFlags & GTF_LATE_ARG))
        {
            TreeNodeInfo* argInfo = &(arg->gtLsraInfo);
            if (argInfo->dstCount != 0)
            {
                argInfo->isLocalDefUse = true;
            }

            // If the child of GT_PUTARG_STK is a constant, we don't need a register to
            // move it to memory (stack location).
            //
            // On AMD64, we don't want to make 0 contained, because we can generate smaller code
            // by zeroing a register and then storing it. E.g.:
            //      xor rdx, rdx
            //      mov gword ptr [rsp+28H], rdx
            // is 2 bytes smaller than:
            //      mov gword ptr [rsp+28H], 0
            //
            // On x86, we push stack arguments; we don't use 'mov'. So:
            //      push 0
            // is 1 byte smaller than:
            //      xor rdx, rdx
            //      push rdx

            argInfo->dstCount = 0;
            if (arg->gtOper == GT_PUTARG_STK)
            {
                GenTree* op1 = arg->gtOp.gtOp1;
                if (IsContainableImmed(arg, op1)
#if defined(_TARGET_AMD64_)
                    && !op1->IsIntegralConst(0)
#endif // _TARGET_AMD64_
                        )
                {
                    MakeSrcContained(arg, op1);
                }
            }
        }
        args = args->gtOp.gtOp2;
    }

#if FEATURE_VARARG
    // If it is a fast tail call, it is already preferenced to use RAX.
    // Therefore, no need set src candidates on call tgt again.
    if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExpr != nullptr))
    {
        // Don't assign the call target to any of the argument registers because
        // we will use them to also pass floating point arguments as required
        // by Amd64 ABI.
        ctrlExpr->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~(RBM_ARG_REGS));
    }
#endif // !FEATURE_VARARG
}

//------------------------------------------------------------------------
// TreeNodeInfoInitBlockStore: Set the NodeInfo for a block store.
//
// Arguments:
//    blkNode       - The block store node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitBlockStore(GenTreeBlk* blkNode)
{
    GenTree*    dstAddr  = blkNode->Addr();
    unsigned    size     = blkNode->gtBlkSize;
    GenTree*    source   = blkNode->Data();
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    // Sources are dest address, initVal or source.
    // We may require an additional source or temp register for the size.
    blkNode->gtLsraInfo.srcCount = 2;
    blkNode->gtLsraInfo.dstCount = 0;
    blkNode->gtLsraInfo.setInternalCandidates(l, RBM_NONE);
    GenTreePtr srcAddrOrFill = nullptr;
    bool       isInitBlk     = blkNode->OperIsInitBlkOp();

    regMaskTP dstAddrRegMask = RBM_NONE;
    regMaskTP sourceRegMask  = RBM_NONE;
    regMaskTP blkSizeRegMask = RBM_NONE;

    if (isInitBlk)
    {
        GenTree* initVal = source;
        if (initVal->OperIsInitVal())
        {
            initVal = initVal->gtGetOp1();
        }
        srcAddrOrFill = initVal;

        switch (blkNode->gtBlkOpKind)
        {
            case GenTreeBlk::BlkOpKindUnroll:
                assert(initVal->IsCnsIntOrI());
                if (size >= XMM_REGSIZE_BYTES)
                {
                    // Reserve an XMM register to fill it with
                    // a pack of 16 init value constants.
                    ssize_t fill                           = initVal->gtIntCon.gtIconVal & 0xFF;
                    blkNode->gtLsraInfo.internalFloatCount = 1;
                    blkNode->gtLsraInfo.setInternalCandidates(l, l->internalFloatRegCandidates());
                    if ((fill == 0) && ((size & 0xf) == 0))
                    {
                        MakeSrcContained(blkNode, source);
                    }
                    // use XMM register to fill with constants, it's AVX instruction and set the flag
                    SetContainsAVXFlags();
                }
#ifdef _TARGET_X86_
                if ((size & 1) != 0)
                {
                    // On x86, you can't address the lower byte of ESI, EDI, ESP, or EBP when doing
                    // a "mov byte ptr [dest], val". If the fill size is odd, we will try to do this
                    // when unrolling, so only allow byteable registers as the source value. (We could
                    // consider just using BlkOpKindRepInstr instead.)
                    sourceRegMask = RBM_BYTE_REGS;
                }
#endif // _TARGET_X86_
                break;

            case GenTreeBlk::BlkOpKindRepInstr:
                // rep stos has the following register requirements:
                // a) The memory address to be in RDI.
                // b) The fill value has to be in RAX.
                // c) The buffer size will go in RCX.
                dstAddrRegMask = RBM_RDI;
                srcAddrOrFill  = initVal;
                sourceRegMask  = RBM_RAX;
                blkSizeRegMask = RBM_RCX;
                break;

            case GenTreeBlk::BlkOpKindHelper:
#ifdef _TARGET_AMD64_
                // The helper follows the regular AMD64 ABI.
                dstAddrRegMask = RBM_ARG_0;
                sourceRegMask  = RBM_ARG_1;
                blkSizeRegMask = RBM_ARG_2;
#else  // !_TARGET_AMD64_
                dstAddrRegMask             = RBM_RDI;
                sourceRegMask              = RBM_RAX;
                blkSizeRegMask             = RBM_RCX;
#endif // !_TARGET_AMD64_
                break;

            default:
                unreached();
        }
    }
    else
    {
        // CopyObj or CopyBlk
        if (source->gtOper == GT_IND)
        {
            srcAddrOrFill = blkNode->Data()->gtGetOp1();
            // We're effectively setting source as contained, but can't call MakeSrcContained, because the
            // "inheritance" of the srcCount is to a child not a parent - it would "just work" but could be misleading.
            // If srcAddr is already non-contained, we don't need to change it.
            if (srcAddrOrFill->gtLsraInfo.getDstCount() == 0)
            {
                srcAddrOrFill->gtLsraInfo.setDstCount(1);
                srcAddrOrFill->gtLsraInfo.setSrcCount(source->gtLsraInfo.srcCount);
            }
            m_lsra->clearOperandCounts(source);
        }
        else if (!source->IsMultiRegCall() && !source->OperIsSIMD())
        {
            assert(source->IsLocal());
            MakeSrcContained(blkNode, source);
        }
        if (blkNode->OperGet() == GT_STORE_OBJ)
        {
            if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindRepInstr)
            {
                // We need the size of the contiguous Non-GC-region to be in RCX to call rep movsq.
                blkSizeRegMask = RBM_RCX;
            }
            // The srcAddr must be in a register.  If it was under a GT_IND, we need to subsume all of its
            // sources.
            sourceRegMask  = RBM_RSI;
            dstAddrRegMask = RBM_RDI;
        }
        else
        {
            switch (blkNode->gtBlkOpKind)
            {
                case GenTreeBlk::BlkOpKindUnroll:
                    // If we have a remainder smaller than XMM_REGSIZE_BYTES, we need an integer temp reg.
                    //
                    // x86 specific note: if the size is odd, the last copy operation would be of size 1 byte.
                    // But on x86 only RBM_BYTE_REGS could be used as byte registers.  Therefore, exclude
                    // RBM_NON_BYTE_REGS from internal candidates.
                    if ((size & (XMM_REGSIZE_BYTES - 1)) != 0)
                    {
                        blkNode->gtLsraInfo.internalIntCount++;
                        regMaskTP regMask = l->allRegs(TYP_INT);

#ifdef _TARGET_X86_
                        if ((size & 1) != 0)
                        {
                            regMask &= ~RBM_NON_BYTE_REGS;
                        }
#endif
                        blkNode->gtLsraInfo.setInternalCandidates(l, regMask);
                    }

                    if (size >= XMM_REGSIZE_BYTES)
                    {
                        // If we have a buffer larger than XMM_REGSIZE_BYTES,
                        // reserve an XMM register to use it for a
                        // series of 16-byte loads and stores.
                        blkNode->gtLsraInfo.internalFloatCount = 1;
                        blkNode->gtLsraInfo.addInternalCandidates(l, l->internalFloatRegCandidates());
                        // Uses XMM reg for load and store and hence check to see whether AVX instructions
                        // are used for codegen, set ContainsAVX flag
                        SetContainsAVXFlags();
                    }
                    // If src or dst are on stack, we don't have to generate the address
                    // into a register because it's just some constant+SP.
                    if ((srcAddrOrFill != nullptr) && srcAddrOrFill->OperIsLocalAddr())
                    {
                        MakeSrcContained(blkNode, srcAddrOrFill);
                    }

                    if (dstAddr->OperIsLocalAddr())
                    {
                        MakeSrcContained(blkNode, dstAddr);
                    }

                    break;

                case GenTreeBlk::BlkOpKindRepInstr:
                    // rep stos has the following register requirements:
                    // a) The dest address has to be in RDI.
                    // b) The src address has to be in RSI.
                    // c) The buffer size will go in RCX.
                    dstAddrRegMask = RBM_RDI;
                    sourceRegMask  = RBM_RSI;
                    blkSizeRegMask = RBM_RCX;
                    break;

                case GenTreeBlk::BlkOpKindHelper:
#ifdef _TARGET_AMD64_
                    // The helper follows the regular AMD64 ABI.
                    dstAddrRegMask = RBM_ARG_0;
                    sourceRegMask  = RBM_ARG_1;
                    blkSizeRegMask = RBM_ARG_2;
#else  // !_TARGET_AMD64_
                    dstAddrRegMask         = RBM_RDI;
                    sourceRegMask          = RBM_RAX;
                    blkSizeRegMask         = RBM_RCX;
#endif // !_TARGET_AMD64_
                    break;

                default:
                    unreached();
            }
        }
    }

    if (dstAddrRegMask != RBM_NONE)
    {
        dstAddr->gtLsraInfo.setSrcCandidates(l, dstAddrRegMask);
    }
    if (sourceRegMask != RBM_NONE)
    {
        if (srcAddrOrFill != nullptr)
        {
            srcAddrOrFill->gtLsraInfo.setSrcCandidates(l, sourceRegMask);
        }
        else
        {
            // This is a local source; we'll use a temp register for its address.
            blkNode->gtLsraInfo.addInternalCandidates(l, sourceRegMask);
            blkNode->gtLsraInfo.internalIntCount++;
        }
    }
    if (blkSizeRegMask != RBM_NONE)
    {
        if (size != 0)
        {
            // Reserve a temp register for the block size argument.
            blkNode->gtLsraInfo.addInternalCandidates(l, blkSizeRegMask);
            blkNode->gtLsraInfo.internalIntCount++;
        }
        else
        {
            // The block size argument is a third argument to GT_STORE_DYN_BLK
            noway_assert(blkNode->gtOper == GT_STORE_DYN_BLK);
            blkNode->gtLsraInfo.setSrcCount(3);
            GenTree* blockSize = blkNode->AsDynBlk()->gtDynamicSize;
            blockSize->gtLsraInfo.setSrcCandidates(l, blkSizeRegMask);
        }
    }
}

#ifdef FEATURE_PUT_STRUCT_ARG_STK
//------------------------------------------------------------------------
// TreeNodeInfoInitPutArgStk: Set the NodeInfo for a GT_PUTARG_STK.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitPutArgStk(GenTreePutArgStk* putArgStk)
{
    TreeNodeInfo* info = &(putArgStk->gtLsraInfo);
    LinearScan*   l    = m_lsra;
    info->srcCount     = 0;

#ifdef _TARGET_X86_
    if (putArgStk->gtOp1->gtOper == GT_FIELD_LIST)
    {
        unsigned fieldCount    = 0;
        bool     needsByteTemp = false;
        bool     needsSimdTemp = false;
        unsigned prevOffset    = putArgStk->getArgSize();
        for (GenTreeFieldList* current = putArgStk->gtOp1->AsFieldList(); current != nullptr; current = current->Rest())
        {
            GenTree* const  fieldNode   = current->Current();
            const var_types fieldType   = fieldNode->TypeGet();
            const unsigned  fieldOffset = current->gtFieldOffset;
            assert(fieldType != TYP_LONG);
            info->srcCount++;

            // For x86 we must mark all integral fields as contained or reg-optional, and handle them
            // accordingly in code generation, since we may have up to 8 fields, which cannot all be in
            // registers to be consumed atomically by the call.
            if (varTypeIsIntegralOrI(fieldNode))
            {
                if (fieldNode->OperGet() == GT_LCL_VAR)
                {
                    LclVarDsc* varDsc = &(comp->lvaTable[fieldNode->AsLclVarCommon()->gtLclNum]);
                    if (varDsc->lvTracked && !varDsc->lvDoNotEnregister)
                    {
                        SetRegOptional(fieldNode);
                    }
                    else
                    {
                        MakeSrcContained(putArgStk, fieldNode);
                    }
                }
                else if (fieldNode->IsIntCnsFitsInI32())
                {
                    MakeSrcContained(putArgStk, fieldNode);
                }
                else
                {
                    // For the case where we cannot directly push the value, if we run out of registers,
                    // it would be better to defer computation until we are pushing the arguments rather
                    // than spilling, but this situation is not all that common, as most cases of promoted
                    // structs do not have a large number of fields, and of those most are lclVars or
                    // copy-propagated constants.
                    SetRegOptional(fieldNode);
                }
            }
#if defined(FEATURE_SIMD)
            // Note that we need to check the GT_FIELD_LIST type, not the fieldType. This is because the
            // GT_FIELD_LIST will be TYP_SIMD12 whereas the fieldType might be TYP_SIMD16 for lclVar, where
            // we "round up" to 16.
            else if (current->gtFieldType == TYP_SIMD12)
            {
                needsSimdTemp = true;
            }
#endif // defined(FEATURE_SIMD)
            else
            {
                assert(varTypeIsFloating(fieldNode) || varTypeIsSIMD(fieldNode));
            }

            // We can treat as a slot any field that is stored at a slot boundary, where the previous
            // field is not in the same slot. (Note that we store the fields in reverse order.)
            const bool fieldIsSlot = ((fieldOffset % 4) == 0) && ((prevOffset - fieldOffset) >= 4);
            if (!fieldIsSlot)
            {
                if (varTypeIsByte(fieldType))
                {
                    // If this field is a slot--i.e. it is an integer field that is 4-byte aligned and takes up 4 bytes
                    // (including padding)--we can store the whole value rather than just the byte. Otherwise, we will
                    // need a byte-addressable register for the store. We will enforce this requirement on an internal
                    // register, which we can use to copy multiple byte values.
                    needsByteTemp = true;
                }
            }

            if (varTypeIsGC(fieldType))
            {
                putArgStk->gtNumberReferenceSlots++;
            }
            prevOffset = fieldOffset;
            fieldCount++;
        }

        info->dstCount = 0;

        if (putArgStk->gtPutArgStkKind == GenTreePutArgStk::Kind::Push)
        {
            // If any of the fields cannot be stored with an actual push, we may need a temporary
            // register to load the value before storing it to the stack location.
            info->internalIntCount = 1;
            regMaskTP regMask      = l->allRegs(TYP_INT);
            if (needsByteTemp)
            {
                regMask &= ~RBM_NON_BYTE_REGS;
            }
            info->setInternalCandidates(l, regMask);
        }

#if defined(FEATURE_SIMD)
        // For PutArgStk of a TYP_SIMD12, we need a SIMD temp register.
        if (needsSimdTemp)
        {
            info->internalFloatCount += 1;
            info->addInternalCandidates(l, l->allSIMDRegs());
        }
#endif // defined(FEATURE_SIMD)

        return;
    }
#endif // _TARGET_X86_

#if defined(FEATURE_SIMD) && defined(_TARGET_X86_)
    // For PutArgStk of a TYP_SIMD12, we need an extra register.
    if (putArgStk->TypeGet() == TYP_SIMD12)
    {
        info->srcCount           = putArgStk->gtOp1->gtLsraInfo.dstCount;
        info->dstCount           = 0;
        info->internalFloatCount = 1;
        info->setInternalCandidates(l, l->allSIMDRegs());
        return;
    }
#endif // defined(FEATURE_SIMD) && defined(_TARGET_X86_)

    if (putArgStk->TypeGet() != TYP_STRUCT)
    {
        TreeNodeInfoInitSimple(putArgStk);
        return;
    }

    GenTreePtr dst     = putArgStk;
    GenTreePtr src     = putArgStk->gtOp1;
    GenTreePtr srcAddr = nullptr;

    bool haveLocalAddr = false;
    if ((src->OperGet() == GT_OBJ) || (src->OperGet() == GT_IND))
    {
        srcAddr = src->gtOp.gtOp1;
        assert(srcAddr != nullptr);
        haveLocalAddr = srcAddr->OperIsLocalAddr();
    }
    else
    {
        assert(varTypeIsSIMD(putArgStk));
    }

    info->srcCount = src->gtLsraInfo.dstCount;
    info->dstCount = 0;

    // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2.
    // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
    // our framework assemblies, so this is the main code generation scheme we'll use.
    ssize_t size = putArgStk->gtNumSlots * TARGET_POINTER_SIZE;
    switch (putArgStk->gtPutArgStkKind)
    {
        case GenTreePutArgStk::Kind::Push:
        case GenTreePutArgStk::Kind::PushAllSlots:
        case GenTreePutArgStk::Kind::Unroll:
            // If we have a remainder smaller than XMM_REGSIZE_BYTES, we need an integer temp reg.
            //
            // x86 specific note: if the size is odd, the last copy operation would be of size 1 byte.
            // But on x86 only RBM_BYTE_REGS could be used as byte registers.  Therefore, exclude
            // RBM_NON_BYTE_REGS from internal candidates.
            if ((putArgStk->gtNumberReferenceSlots == 0) && (size & (XMM_REGSIZE_BYTES - 1)) != 0)
            {
                info->internalIntCount++;
                regMaskTP regMask = l->allRegs(TYP_INT);

#ifdef _TARGET_X86_
                if ((size % 2) != 0)
                {
                    regMask &= ~RBM_NON_BYTE_REGS;
                }
#endif
                info->setInternalCandidates(l, regMask);
            }

#ifdef _TARGET_X86_
            if (size >= 8)
#else  // !_TARGET_X86_
            if (size >= XMM_REGSIZE_BYTES)
#endif // !_TARGET_X86_
            {
                // If we have a buffer larger than or equal to XMM_REGSIZE_BYTES on x64/ux,
                // or larger than or equal to 8 bytes on x86, reserve an XMM register to use it for a
                // series of 16-byte loads and stores.
                info->internalFloatCount = 1;
                info->addInternalCandidates(l, l->internalFloatRegCandidates());
                SetContainsAVXFlags();
            }
            break;

        case GenTreePutArgStk::Kind::RepInstr:
            info->internalIntCount += 3;
            info->setInternalCandidates(l, (RBM_RDI | RBM_RCX | RBM_RSI));
            break;

        default:
            unreached();
    }

    // Always mark the OBJ and ADDR as contained trees by the putarg_stk. The codegen will deal with this tree.
    MakeSrcContained(putArgStk, src);

    if (haveLocalAddr)
    {
        // If the source address is the address of a lclVar, make the source address contained to avoid unnecessary
        // copies.
        //
        // To avoid an assertion in MakeSrcContained, increment the parent's source count beforehand and decrement it
        // afterwards.
        info->srcCount++;
        MakeSrcContained(putArgStk, srcAddr);
        info->srcCount--;
    }
}
#endif // FEATURE_PUT_STRUCT_ARG_STK

//------------------------------------------------------------------------
// TreeNodeInfoInitLclHeap: Set the NodeInfo for a GT_LCLHEAP.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitLclHeap(GenTree* tree)
{
    TreeNodeInfo* info     = &(tree->gtLsraInfo);
    LinearScan*   l        = m_lsra;
    Compiler*     compiler = comp;

    info->srcCount = 1;
    info->dstCount = 1;

    // Need a variable number of temp regs (see genLclHeap() in codegenamd64.cpp):
    // Here '-' means don't care.
    //
    //     Size?                    Init Memory?         # temp regs
    //      0                            -                  0 (returns 0)
    //      const and <=6 reg words      -                  0 (pushes '0')
    //      const and >6 reg words       Yes                0 (pushes '0')
    //      const and <PageSize          No                 0 (amd64) 1 (x86)
    //                                                        (x86:tmpReg for sutracting from esp)
    //      const and >=PageSize         No                 2 (regCnt and tmpReg for subtracing from sp)
    //      Non-const                    Yes                0 (regCnt=targetReg and pushes '0')
    //      Non-const                    No                 2 (regCnt and tmpReg for subtracting from sp)
    //
    // Note: Here we don't need internal register to be different from targetReg.
    // Rather, require it to be different from operand's reg.

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
            // This should also help in debugging as we can examine the original size specified with localloc.
            sizeVal = AlignUp(sizeVal, STACK_ALIGN);

            // For small allocations up to 6 pointer sized words (i.e. 48 bytes of localloc)
            // we will generate 'push 0'.
            assert((sizeVal % REGSIZE_BYTES) == 0);
            size_t cntRegSizedWords = sizeVal / REGSIZE_BYTES;
            if (cntRegSizedWords <= 6)
            {
                info->internalIntCount = 0;
            }
            else if (!compiler->info.compInitMem)
            {
                // No need to initialize allocated stack space.
                if (sizeVal < compiler->eeGetPageSize())
                {
#ifdef _TARGET_X86_
                    info->internalIntCount = 1; // x86 needs a register here to avoid generating "sub" on ESP.
#else                                           // !_TARGET_X86_
                    info->internalIntCount = 0;
#endif                                          // !_TARGET_X86_
                }
                else
                {
                    // We need two registers: regCnt and RegTmp
                    info->internalIntCount = 2;
                }
            }
            else
            {
                // >6 and need to zero initialize allocated stack space.
                info->internalIntCount = 0;
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
            info->internalIntCount = 0;
        }
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitLogicalOp: Set the NodeInfo for GT_AND/GT_OR/GT_XOR,
// as well as GT_ADD/GT_SUB.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitLogicalOp(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    LinearScan*   l    = m_lsra;

    // We're not marking a constant hanging on the left of the add
    // as containable so we assign it to a register having CQ impact.
    // TODO-XArch-CQ: Detect this case and support both generating a single instruction
    // for GT_ADD(Constant, SomeTree)
    info->srcCount = 2;
    info->dstCount = 1;

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    // We can directly encode the second operand if it is either a containable constant or a memory-op.
    // In case of memory-op, we can encode it directly provided its type matches with 'tree' type.
    // This is because during codegen, type of 'tree' is used to determine emit Type size. If the types
    // do not match, they get normalized (i.e. sign/zero extended) on load into a register.
    bool       directlyEncodable = false;
    bool       binOpInRMW        = false;
    GenTreePtr operand           = nullptr;

    if (IsContainableImmed(tree, op2))
    {
        directlyEncodable = true;
        operand           = op2;
    }
    else
    {
        binOpInRMW = IsBinOpInRMWStoreInd(tree);
        if (!binOpInRMW)
        {
            if (op2->isMemoryOp() && tree->TypeGet() == op2->TypeGet())
            {
                directlyEncodable = true;
                operand           = op2;
            }
            else if (tree->OperIsCommutative())
            {
                if (IsContainableImmed(tree, op1) ||
                    (op1->isMemoryOp() && tree->TypeGet() == op1->TypeGet() && IsSafeToContainMem(tree, op1)))
                {
                    // If it is safe, we can reverse the order of operands of commutative operations for efficient
                    // codegen
                    directlyEncodable = true;
                    operand           = op1;
                }
            }
        }
    }

    if (directlyEncodable)
    {
        assert(operand != nullptr);
        MakeSrcContained(tree, operand);
    }
    else if (!binOpInRMW)
    {
        // If this binary op neither has contained operands, nor is a
        // Read-Modify-Write (RMW) operation, we can mark its operands
        // as reg optional.
        SetRegOptionalForBinOp(tree);
    }

    // Codegen of this tree node sets ZF and SF flags.
    tree->gtFlags |= GTF_ZSF_SET;
}

//------------------------------------------------------------------------
// TreeNodeInfoInitModDiv: Set the NodeInfo for GT_MOD/GT_DIV/GT_UMOD/GT_UDIV.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitModDiv(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    LinearScan*   l    = m_lsra;

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    info->srcCount = 2;
    info->dstCount = 1;

    switch (tree->OperGet())
    {
        case GT_MOD:
        case GT_DIV:
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(op1->TypeGet() == op2->TypeGet());

                if (op2->isMemoryOp() || op2->IsCnsNonZeroFltOrDbl())
                {
                    MakeSrcContained(tree, op2);
                }
                else
                {
                    // If there are no containable operands, we can make an operand reg optional.
                    // SSE2 allows only op2 to be a memory-op.
                    SetRegOptional(op2);
                }

                return;
            }
            break;

        default:
            break;
    }

    // Amd64 Div/Idiv instruction:
    //    Dividend in RAX:RDX  and computes
    //    Quotient in RAX, Remainder in RDX

    if (tree->OperGet() == GT_MOD || tree->OperGet() == GT_UMOD)
    {
        // We are interested in just the remainder.
        // RAX is used as a trashable register during computation of remainder.
        info->setDstCandidates(l, RBM_RDX);
    }
    else
    {
        // We are interested in just the quotient.
        // RDX gets used as trashable register during computation of quotient
        info->setDstCandidates(l, RBM_RAX);
    }

    bool op2CanBeRegOptional = true;
#ifdef _TARGET_X86_
    if (op1->OperGet() == GT_LONG)
    {
        // To avoid reg move would like to have op1's low part in RAX and high part in RDX.
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();

        // Src count is actually 3, so increment.
        assert(op2->IsCnsIntOrI());
        assert(tree->OperGet() == GT_UMOD);
        info->srcCount++;
        op2CanBeRegOptional = false;

        // This situation also requires an internal register.
        info->internalIntCount = 1;
        info->setInternalCandidates(l, l->allRegs(TYP_INT));

        loVal->gtLsraInfo.setSrcCandidates(l, RBM_EAX);
        hiVal->gtLsraInfo.setSrcCandidates(l, RBM_EDX);
    }
    else
#endif
    {
        // If possible would like to have op1 in RAX to avoid a register move
        op1->gtLsraInfo.setSrcCandidates(l, RBM_RAX);
    }

    // divisor can be an r/m, but the memory indirection must be of the same size as the divide
    if (op2->isMemoryOp() && (op2->TypeGet() == tree->TypeGet()))
    {
        MakeSrcContained(tree, op2);
    }
    else if (op2CanBeRegOptional)
    {
        op2->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~(RBM_RAX | RBM_RDX));

        // If there are no containable operands, we can make an operand reg optional.
        // Div instruction allows only op2 to be a memory op.
        SetRegOptional(op2);
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitIntrinsic: Set the NodeInfo for a GT_INTRINSIC.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitIntrinsic(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    LinearScan*   l    = m_lsra;

    // Both operand and its result must be of floating point type.
    GenTree* op1 = tree->gtGetOp1();
    assert(varTypeIsFloating(op1));
    assert(op1->TypeGet() == tree->TypeGet());

    info->srcCount = 1;
    info->dstCount = 1;

    switch (tree->gtIntrinsic.gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Sqrt:
            if (op1->isMemoryOp() || op1->IsCnsNonZeroFltOrDbl())
            {
                MakeSrcContained(tree, op1);
            }
            else
            {
                // Mark the operand as reg optional since codegen can still
                // generate code if op1 is on stack.
                SetRegOptional(op1);
            }
            break;

        case CORINFO_INTRINSIC_Abs:
            // Abs(float x) = x & 0x7fffffff
            // Abs(double x) = x & 0x7ffffff ffffffff

            // In case of Abs we need an internal register to hold mask.

            // TODO-XArch-CQ: avoid using an internal register for the mask.
            // Andps or andpd both will operate on 128-bit operands.
            // The data section constant to hold the mask is a 64-bit size.
            // Therefore, we need both the operand and mask to be in
            // xmm register. When we add support in emitter to emit 128-bit
            // data constants and instructions that operate on 128-bit
            // memory operands we can avoid the need for an internal register.
            if (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Abs)
            {
                info->internalFloatCount = 1;
                info->setInternalCandidates(l, l->internalFloatRegCandidates());
            }
            break;

#ifdef _TARGET_X86_
        case CORINFO_INTRINSIC_Cos:
        case CORINFO_INTRINSIC_Sin:
        case CORINFO_INTRINSIC_Round:
            NYI_X86("Math intrinsics Cos, Sin and Round");
            break;
#endif // _TARGET_X86_

        default:
            // Right now only Sqrt/Abs are treated as math intrinsics
            noway_assert(!"Unsupported math intrinsic");
            unreached();
            break;
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
    GenTreeSIMD*  simdTree = tree->AsSIMD();
    TreeNodeInfo* info     = &(tree->gtLsraInfo);
    LinearScan*   lsra     = m_lsra;
    info->dstCount         = 1;
    SetContainsAVXFlags(true, simdTree->gtSIMDSize);
    switch (simdTree->gtSIMDIntrinsicID)
    {
        GenTree* op1;
        GenTree* op2;

        case SIMDIntrinsicInit:
        {
            op1 = tree->gtOp.gtOp1;

#if !defined(_TARGET_64BIT_)
            if (op1->OperGet() == GT_LONG)
            {
                info->srcCount = 2;
            }
            else
#endif // !defined(_TARGET_64BIT_)
            {
                info->srcCount = 1;
            }

            // This sets all fields of a SIMD struct to the given value.
            // Mark op1 as contained if it is either zero or int constant of all 1's,
            // or a float constant with 16 or 32 byte simdType (AVX case)
            //
            // Should never see small int base type vectors except for zero initialization.
            assert(!varTypeIsSmallInt(simdTree->gtSIMDBaseType) || op1->IsIntegralConst(0));

#if !defined(_TARGET_64BIT_)
            if (op1->OperGet() == GT_LONG)
            {
                GenTree* op1lo = op1->gtGetOp1();
                GenTree* op1hi = op1->gtGetOp2();

                if ((op1lo->IsIntegralConst(0) && op1hi->IsIntegralConst(0)) ||
                    (op1lo->IsIntegralConst(-1) && op1hi->IsIntegralConst(-1)))
                {
                    assert(op1->gtLsraInfo.srcCount == 0);
                    assert(op1->gtLsraInfo.dstCount == 0);
                    assert(op1lo->gtLsraInfo.srcCount == 0);
                    assert(op1lo->gtLsraInfo.dstCount == 1);
                    assert(op1hi->gtLsraInfo.srcCount == 0);
                    assert(op1hi->gtLsraInfo.dstCount == 1);

                    op1lo->gtLsraInfo.dstCount = 0;
                    op1hi->gtLsraInfo.dstCount = 0;
                    info->srcCount             = 0;
                }
                else
                {
                    // need a temp
                    info->internalFloatCount = 1;
                    info->setInternalCandidates(lsra, lsra->allSIMDRegs());
                    info->isInternalRegDelayFree = true;
                }
            }
            else
#endif // !defined(_TARGET_64BIT_)
                if (op1->IsFPZero() || op1->IsIntegralConst(0) ||
                    (varTypeIsIntegral(simdTree->gtSIMDBaseType) && op1->IsIntegralConst(-1)))
            {
                MakeSrcContained(tree, op1);
                info->srcCount = 0;
            }
            else if ((comp->getSIMDInstructionSet() == InstructionSet_AVX) &&
                     ((simdTree->gtSIMDSize == 16) || (simdTree->gtSIMDSize == 32)))
            {
                // Either op1 is a float or dbl constant or an addr
                if (op1->IsCnsFltOrDbl() || op1->OperIsLocalAddr())
                {
                    MakeSrcContained(tree, op1);
                    info->srcCount = 0;
                }
            }
        }
        break;

        case SIMDIntrinsicInitN:
        {
            info->srcCount = (short)(simdTree->gtSIMDSize / genTypeSize(simdTree->gtSIMDBaseType));

            // Need an internal register to stitch together all the values into a single vector in a SIMD reg.
            info->internalFloatCount = 1;
            info->setInternalCandidates(lsra, lsra->allSIMDRegs());
        }
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
            // float/double vectors: This gets implemented as bitwise-And operation
            // with a mask and hence should never see  here.
            //
            // Must be a Vector<int> or Vector<short> Vector<sbyte>
            assert(simdTree->gtSIMDBaseType == TYP_INT || simdTree->gtSIMDBaseType == TYP_SHORT ||
                   simdTree->gtSIMDBaseType == TYP_BYTE);
            assert(comp->getSIMDInstructionSet() >= InstructionSet_SSE3_4);
            info->srcCount = 1;
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
            if (simdTree->gtSIMDIntrinsicID == SIMDIntrinsicMul && simdTree->gtSIMDBaseType == TYP_INT &&
                comp->getSIMDInstructionSet() == InstructionSet_SSE2)
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

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            info->srcCount = 2;

            // On SSE4/AVX, we can generate optimal code for (in)equality
            // against zero using ptest. We can safely do this optimization
            // for integral vectors but not for floating-point for the reason
            // that we have +0.0 and -0.0 and +0.0 == -0.0
            op2 = tree->gtGetOp2();
            if ((comp->getSIMDInstructionSet() >= InstructionSet_SSE3_4) && op2->IsIntegralConstVector(0))
            {
                MakeSrcContained(tree, op2);
            }
            else
            {
                // Need one SIMD register as scratch.
                // See genSIMDIntrinsicRelOp() for details on code sequence generated and
                // the need for one scratch register.
                //
                // Note these intrinsics produce a BOOL result, hence internal float
                // registers reserved are guaranteed to be different from target
                // integer register without explicitly specifying.
                info->internalFloatCount = 1;
                info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            }
            break;

        case SIMDIntrinsicDotProduct:
            // Float/Double vectors:
            // For SSE, or AVX with 32-byte vectors, we also need an internal register
            // as scratch. Further we need the targetReg and internal reg to be distinct
            // registers. Note that if this is a TYP_SIMD16 or smaller on AVX, then we
            // don't need a tmpReg.
            //
            // 32-byte integer vector on SSE4/AVX:
            // will take advantage of phaddd, which operates only on 128-bit xmm reg.
            // This will need 1 (in case of SSE4) or 2 (in case of AVX) internal
            // registers since targetReg is an int type register.
            //
            // See genSIMDIntrinsicDotProduct() for details on code sequence generated
            // and the need for scratch registers.
            if (varTypeIsFloating(simdTree->gtSIMDBaseType))
            {
                if ((comp->getSIMDInstructionSet() == InstructionSet_SSE2) ||
                    (simdTree->gtOp.gtOp1->TypeGet() == TYP_SIMD32))
                {
                    info->internalFloatCount     = 1;
                    info->isInternalRegDelayFree = true;
                    info->setInternalCandidates(lsra, lsra->allSIMDRegs());
                }
                // else don't need scratch reg(s).
            }
            else
            {
                assert(simdTree->gtSIMDBaseType == TYP_INT && comp->getSIMDInstructionSet() >= InstructionSet_SSE3_4);

                // No need to set isInternalRegDelayFree since targetReg is a
                // an int type reg and guaranteed to be different from xmm/ymm
                // regs.
                info->internalFloatCount = comp->canUseAVX() ? 2 : 1;
                info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            }
            info->srcCount = 2;
            break;

        case SIMDIntrinsicGetItem:
        {
            // This implements get_Item method. The sources are:
            //  - the source SIMD struct
            //  - index (which element to get)
            // The result is baseType of SIMD struct.
            info->srcCount = 2;
            op1            = tree->gtOp.gtOp1;
            op2            = tree->gtOp.gtOp2;

            // If the index is a constant, mark it as contained.
            if (CheckImmedAndMakeContained(tree, op2))
            {
                info->srcCount = 1;
            }

            if (op1->isMemoryOp())
            {
                MakeSrcContained(tree, op1);

                // Although GT_IND of TYP_SIMD12 reserves an internal float
                // register for reading 4 and 8 bytes from memory and
                // assembling them into target XMM reg, it is not required
                // in this case.
                op1->gtLsraInfo.internalIntCount   = 0;
                op1->gtLsraInfo.internalFloatCount = 0;
            }
            else
            {
                // If the index is not a constant, we will use the SIMD temp location to store the vector.
                // Otherwise, if the baseType is floating point, the targetReg will be a xmm reg and we
                // can use that in the process of extracting the element.
                //
                // If the index is a constant and base type is a small int we can use pextrw, but on AVX
                // we will need a temp if are indexing into the upper half of the AVX register.
                // In all other cases with constant index, we need a temp xmm register to extract the
                // element if index is other than zero.

                if (!op2->IsCnsIntOrI())
                {
                    (void)comp->getSIMDInitTempVarNum();
                }
                else if (!varTypeIsFloating(simdTree->gtSIMDBaseType))
                {
                    bool needFloatTemp;
                    if (varTypeIsSmallInt(simdTree->gtSIMDBaseType) &&
                        (comp->getSIMDInstructionSet() == InstructionSet_AVX))
                    {
                        int byteShiftCnt = (int)op2->AsIntCon()->gtIconVal * genTypeSize(simdTree->gtSIMDBaseType);
                        needFloatTemp    = (byteShiftCnt >= 16);
                    }
                    else
                    {
                        needFloatTemp = !op2->IsIntegralConst(0);
                    }

                    if (needFloatTemp)
                    {
                        info->internalFloatCount = 1;
                        info->setInternalCandidates(lsra, lsra->allSIMDRegs());
                    }
                }
            }
        }
        break;

        case SIMDIntrinsicSetX:
        case SIMDIntrinsicSetY:
        case SIMDIntrinsicSetZ:
        case SIMDIntrinsicSetW:
            info->srcCount = 2;

            // We need an internal integer register for SSE2 codegen
            if (comp->getSIMDInstructionSet() == InstructionSet_SSE2)
            {
                info->internalIntCount = 1;
                info->setInternalCandidates(lsra, lsra->allRegs(TYP_INT));
            }

            break;

        case SIMDIntrinsicCast:
            info->srcCount = 1;
            break;

        case SIMDIntrinsicConvertToSingle:
            info->srcCount = 1;
            if (simdTree->gtSIMDBaseType == TYP_UINT)
            {
                // We need an internal register different from targetReg.
                info->isInternalRegDelayFree = true;
                info->internalIntCount       = 1;
                info->internalFloatCount     = 2;
                info->setInternalCandidates(lsra, lsra->allSIMDRegs() | lsra->allRegs(TYP_INT));
            }
            break;

        case SIMDIntrinsicConvertToUInt32:
        case SIMDIntrinsicConvertToInt32:
            info->srcCount = 1;
            break;

        case SIMDIntrinsicWidenLo:
        case SIMDIntrinsicWidenHi:
            info->srcCount = 1;
            if (varTypeIsIntegral(simdTree->gtSIMDBaseType))
            {
                // We need an internal register different from targetReg.
                info->isInternalRegDelayFree = true;
                info->internalFloatCount     = 1;
                info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            }
            break;

        case SIMDIntrinsicConvertToInt64:
        case SIMDIntrinsicConvertToUInt64:
            // We need an internal register different from targetReg.
            info->isInternalRegDelayFree = true;
            info->srcCount               = 1;
            info->internalIntCount       = 1;
            if (comp->getSIMDInstructionSet() == InstructionSet_AVX)
            {
                info->internalFloatCount = 2;
            }
            else
            {
                info->internalFloatCount = 1;
            }
            info->setInternalCandidates(lsra, lsra->allSIMDRegs() | lsra->allRegs(TYP_INT));
            break;

        case SIMDIntrinsicConvertToDouble:
            // We need an internal register different from targetReg.
            info->isInternalRegDelayFree = true;
            info->srcCount               = 1;
            info->internalIntCount       = 1;
#ifdef _TARGET_X86_
            if (simdTree->gtSIMDBaseType == TYP_LONG)
            {
                info->internalFloatCount = 3;
            }
            else
#endif
                if ((comp->getSIMDInstructionSet() == InstructionSet_AVX) || (simdTree->gtSIMDBaseType == TYP_ULONG))
            {
                info->internalFloatCount = 2;
            }
            else
            {
                info->internalFloatCount = 1;
            }
            info->setInternalCandidates(lsra, lsra->allSIMDRegs() | lsra->allRegs(TYP_INT));
            break;

        case SIMDIntrinsicNarrow:
            // We need an internal register different from targetReg.
            info->isInternalRegDelayFree = true;
            info->srcCount               = 2;
            if ((comp->getSIMDInstructionSet() == InstructionSet_AVX) && (simdTree->gtSIMDBaseType != TYP_DOUBLE))
            {
                info->internalFloatCount = 2;
            }
            else
            {
                info->internalFloatCount = 1;
            }
            info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            break;

        case SIMDIntrinsicShuffleSSE2:
            info->srcCount = 2;
            // Second operand is an integer constant and marked as contained.
            op2 = tree->gtOp.gtOp2;
            noway_assert(op2->IsCnsIntOrI());
            MakeSrcContained(tree, op2);
            break;

        case SIMDIntrinsicGetX:
        case SIMDIntrinsicGetY:
        case SIMDIntrinsicGetZ:
        case SIMDIntrinsicGetW:
        case SIMDIntrinsicGetOne:
        case SIMDIntrinsicGetZero:
        case SIMDIntrinsicGetCount:
        case SIMDIntrinsicGetAllOnes:
            assert(!"Get intrinsics should not be seen during Lowering.");
            unreached();

        default:
            noway_assert(!"Unimplemented SIMD node type.");
            unreached();
    }
}
#endif // FEATURE_SIMD

//------------------------------------------------------------------------
// TreeNodeInfoInitCast: Set the NodeInfo for a GT_CAST.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitCast(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);

    // TODO-XArch-CQ: Int-To-Int conversions - castOp cannot be a memory op and must have an assigned register.
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

    if (!tree->gtOverflow() && (varTypeIsFloating(castToType) || varTypeIsFloating(castOpType)))
    {
#ifdef DEBUG
        // If converting to float/double, the operand must be 4 or 8 byte in size.
        if (varTypeIsFloating(castToType))
        {
            unsigned opSize = genTypeSize(castOpType);
            assert(opSize == 4 || opSize == 8);
        }
#endif // DEBUG

        // U8 -> R8 conversion requires that the operand be in a register.
        if (castOpType != TYP_ULONG)
        {
            if (castOp->isMemoryOp() || castOp->IsCnsNonZeroFltOrDbl())
            {
                MakeSrcContained(tree, castOp);
            }
            else
            {
                // Mark castOp as reg optional to indicate codegen
                // can still generate code if it is on stack.
                SetRegOptional(castOp);
            }
        }
    }

#if !defined(_TARGET_64BIT_)
    if (varTypeIsLong(castOpType))
    {
        noway_assert(castOp->OperGet() == GT_LONG);
        info->srcCount = 2;
    }
#endif // !defined(_TARGET_64BIT_)

    // some overflow checks need a temp reg:
    //  - GT_CAST from INT64/UINT64 to UINT32
    if (tree->gtOverflow() && (castToType == TYP_UINT))
    {
        if (genTypeSize(castOpType) == 8)
        {
            // Here we don't need internal register to be different from targetReg,
            // rather require it to be different from operand's reg.
            info->internalIntCount = 1;
        }
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitGCWriteBarrier: Set the NodeInfo for a GT_STOREIND requiring a write barrier.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitGCWriteBarrier(GenTree* tree)
{
    assert(tree->OperGet() == GT_STOREIND);

    GenTreeStoreInd* dst  = tree->AsStoreInd();
    GenTreePtr       addr = dst->Addr();
    GenTreePtr       src  = dst->Data();

    if (addr->OperGet() == GT_LEA)
    {
        // In the case where we are doing a helper assignment, if the dst
        // is an indir through an lea, we need to actually instantiate the
        // lea in a register
        GenTreeAddrMode* lea = addr->AsAddrMode();

        int leaSrcCount = 0;
        if (lea->HasBase())
        {
            leaSrcCount++;
        }
        if (lea->HasIndex())
        {
            leaSrcCount++;
        }
        lea->gtLsraInfo.srcCount = leaSrcCount;
        lea->gtLsraInfo.dstCount = 1;
    }

    bool useOptimizedWriteBarrierHelper = false; // By default, assume no optimized write barriers.

#if NOGC_WRITE_BARRIERS

#if defined(_TARGET_X86_)

    useOptimizedWriteBarrierHelper = true; // On x86, use the optimized write barriers by default.
#ifdef DEBUG
    GCInfo::WriteBarrierForm wbf = comp->codeGen->gcInfo.gcIsWriteBarrierCandidate(tree, src);
    if (wbf == GCInfo::WBF_NoBarrier_CheckNotHeapInDebug) // This one is always a call to a C++ method.
    {
        useOptimizedWriteBarrierHelper = false;
    }
#endif

    if (useOptimizedWriteBarrierHelper)
    {
        // Special write barrier:
        // op1 (addr) goes into REG_WRITE_BARRIER (rdx) and
        // op2 (src) goes into any int register.
        addr->gtLsraInfo.setSrcCandidates(m_lsra, RBM_WRITE_BARRIER);
        src->gtLsraInfo.setSrcCandidates(m_lsra, RBM_WRITE_BARRIER_SRC);
    }

#else // !defined(_TARGET_X86_)
#error "NOGC_WRITE_BARRIERS is not supported"
#endif // !defined(_TARGET_X86_)

#endif // NOGC_WRITE_BARRIERS

    if (!useOptimizedWriteBarrierHelper)
    {
        // For the standard JIT Helper calls:
        // op1 (addr) goes into REG_ARG_0 and
        // op2 (src) goes into REG_ARG_1
        addr->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_0);
        src->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_1);
    }

    // Both src and dst must reside in a register, which they should since we haven't set
    // either of them as contained.
    assert(addr->gtLsraInfo.dstCount == 1);
    assert(src->gtLsraInfo.dstCount == 1);
}

//-----------------------------------------------------------------------------------------
// TreeNodeInfoInitIndir: Specify register requirements for address expression of an indirection operation.
//
// Arguments:
//    indirTree    -   GT_IND or GT_STOREIND gentree node
//
void Lowering::TreeNodeInfoInitIndir(GenTreePtr indirTree)
{
    assert(indirTree->isIndir());
    // If this is the rhs of a block copy (i.e. non-enregisterable struct),
    // it has no register requirements.
    if (indirTree->TypeGet() == TYP_STRUCT)
    {
        return;
    }

    GenTreePtr    addr = indirTree->gtGetOp1();
    TreeNodeInfo* info = &(indirTree->gtLsraInfo);

    GenTreePtr base  = nullptr;
    GenTreePtr index = nullptr;
    unsigned   mul, cns;
    bool       rev;

#ifdef FEATURE_SIMD
    // If indirTree is of TYP_SIMD12, don't mark addr as contained
    // so that it always get computed to a register.  This would
    // mean codegen side logic doesn't need to handle all possible
    // addr expressions that could be contained.
    //
    // TODO-XArch-CQ: handle other addr mode expressions that could be marked
    // as contained.
    if (indirTree->TypeGet() == TYP_SIMD12)
    {
        // Vector3 is read/written as two reads/writes: 8 byte and 4 byte.
        // To assemble the vector properly we would need an additional
        // XMM register.
        info->internalFloatCount = 1;

        // In case of GT_IND we need an internal register different from targetReg and
        // both of the registers are used at the same time.
        if (indirTree->OperGet() == GT_IND)
        {
            info->isInternalRegDelayFree = true;
        }

        info->setInternalCandidates(m_lsra, m_lsra->allSIMDRegs());

        return;
    }
#endif // FEATURE_SIMD

    if ((indirTree->gtFlags & GTF_IND_REQ_ADDR_IN_REG) != 0)
    {
        // The address of an indirection that requires its address in a reg.
        // Skip any further processing that might otherwise make it contained.
    }
    else if ((addr->OperGet() == GT_CLS_VAR_ADDR) || (addr->OperGet() == GT_LCL_VAR_ADDR))
    {
        // These nodes go into an addr mode:
        // - GT_CLS_VAR_ADDR turns into a constant.
        // - GT_LCL_VAR_ADDR is a stack addr mode.

        // make this contained, it turns into a constant that goes into an addr mode
        MakeSrcContained(indirTree, addr);
    }
    else if (addr->IsCnsIntOrI() && addr->AsIntConCommon()->FitsInAddrBase(comp))
    {
        // Amd64:
        // We can mark any pc-relative 32-bit addr as containable, except for a direct VSD call address.
        // (i.e. those VSD calls for which stub addr is known during JIT compilation time).  In this case,
        // VM requires us to pass stub addr in REG_VIRTUAL_STUB_PARAM - see LowerVirtualStubCall().  For
        // that reason we cannot mark such an addr as contained.  Note that this is not an issue for
        // indirect VSD calls since morphArgs() is explicitly materializing hidden param as a non-standard
        // argument.
        //
        // Workaround:
        // Note that LowerVirtualStubCall() sets addr->gtRegNum to REG_VIRTUAL_STUB_PARAM and Lowering::doPhase()
        // sets destination candidates on such nodes and resets addr->gtRegNum to REG_NA before calling
        // TreeNodeInfoInit(). Ideally we should set a flag on addr nodes that shouldn't be marked as contained
        // (in LowerVirtualStubCall()), but we don't have any GTF_* flags left for that purpose.  As a workaround
        // an explicit check is made here.
        //
        // On x86, direct VSD is done via a relative branch, and in fact it MUST be contained.
        MakeSrcContained(indirTree, addr);
    }
    else if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirTree, addr))
    {
        MakeSrcContained(indirTree, addr);
    }
    else if (addr->gtOper == GT_ARR_ELEM)
    {
        // The GT_ARR_ELEM consumes all the indices and produces the offset.
        // The array object lives until the mem access.
        // We also consume the target register to which the address is
        // computed

        info->srcCount++;
        assert(addr->gtLsraInfo.srcCount >= 2);
        addr->gtLsraInfo.srcCount -= 1;
    }
}

//------------------------------------------------------------------------
// TreeNodeInfoInitCmp: Set the register requirements for a compare.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitCmp(GenTreePtr tree)
{
    assert(tree->OperIsCompare());

    TreeNodeInfo* info = &(tree->gtLsraInfo);

    info->srcCount = 2;
    info->dstCount = 1;

#ifdef _TARGET_X86_
    // If the compare is used by a jump, we just need to set the condition codes. If not, then we need
    // to store the result into the low byte of a register, which requires the dst be a byteable register.
    // We always set the dst candidates, though, because if this is compare is consumed by a jump, they
    // won't be used. We might be able to use GTF_RELOP_JMP_USED to determine this case, but it's not clear
    // that flag is maintained until this location (especially for decomposed long compares).
    info->setDstCandidates(m_lsra, RBM_BYTE_REGS);
#endif // _TARGET_X86_

    GenTreePtr op1     = tree->gtOp.gtOp1;
    GenTreePtr op2     = tree->gtOp.gtOp2;
    var_types  op1Type = op1->TypeGet();
    var_types  op2Type = op2->TypeGet();

#if !defined(_TARGET_64BIT_)
    // Long compares will consume GT_LONG nodes, each of which produces two results.
    // Thus for each long operand there will be an additional source.
    // TODO-X86-CQ: Mark hiOp2 and loOp2 as contained if it is a constant or a memory op.
    if (varTypeIsLong(op1Type))
    {
        info->srcCount++;
    }
    if (varTypeIsLong(op2Type))
    {
        info->srcCount++;
    }
#endif // !defined(_TARGET_64BIT_)

    // If either of op1 or op2 is floating point values, then we need to use
    // ucomiss or ucomisd to compare, both of which support the following form:
    //     ucomis[s|d] xmm, xmm/mem
    // That is only the second operand can be a memory op.
    //
    // Second operand is a memory Op:  Note that depending on comparison operator,
    // the operands of ucomis[s|d] need to be reversed.  Therefore, either op1 or
    // op2 can be a memory op depending on the comparison operator.
    if (varTypeIsFloating(op1Type))
    {
        // The type of the operands has to be the same and no implicit conversions at this stage.
        assert(op1Type == op2Type);

        bool reverseOps;
        if ((tree->gtFlags & GTF_RELOP_NAN_UN) != 0)
        {
            // Unordered comparison case
            reverseOps = tree->OperIs(GT_GT, GT_GE);
        }
        else
        {
            reverseOps = tree->OperIs(GT_LT, GT_LE);
        }

        GenTreePtr otherOp;
        if (reverseOps)
        {
            otherOp = op1;
        }
        else
        {
            otherOp = op2;
        }

        assert(otherOp != nullptr);
        if (otherOp->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(tree, otherOp);
        }
        else if (otherOp->isMemoryOp() && ((otherOp == op2) || IsSafeToContainMem(tree, otherOp)))
        {
            MakeSrcContained(tree, otherOp);
        }
        else
        {
            // SSE2 allows only otherOp to be a memory-op. Since otherOp is not
            // contained, we can mark it reg-optional.
            SetRegOptional(otherOp);
        }

        return;
    }

    // TODO-XArch-CQ: factor out cmp optimization in 'genCondSetFlags' to be used here
    // or in other backend.

    if (CheckImmedAndMakeContained(tree, op2))
    {
        // If the types are the same, or if the constant is of the correct size,
        // we can treat the isMemoryOp as contained.
        if (op1Type == op2Type)
        {
            if (op1->isMemoryOp())
            {
                MakeSrcContained(tree, op1);
            }
            // If op1 codegen sets ZF and SF flags and ==/!= against
            // zero, we don't need to generate test instruction,
            // provided we don't have another GenTree node between op1
            // and tree that could potentially modify flags.
            //
            // TODO-CQ: right now the below peep is inexpensive and
            // gets the benefit in most of cases because in majority
            // of cases op1, op2 and tree would be in that order in
            // execution.  In general we should be able to check that all
            // the nodes that come after op1 in execution order do not
            // modify the flags so that it is safe to avoid generating a
            // test instruction.  Such a check requires that on each
            // GenTree node we need to set the info whether its codegen
            // will modify flags.
            //
            // TODO-CQ: We can optimize compare against zero in the
            // following cases by generating the branch as indicated
            // against each case.
            //  1) unsigned compare
            //        < 0  - always FALSE
            //       <= 0  - ZF=1 and jne
            //        > 0  - ZF=0 and je
            //       >= 0  - always TRUE
            //
            // 2) signed compare
            //        < 0  - SF=1 and js
            //       >= 0  - SF=0 and jns
            else if (tree->OperIs(GT_EQ, GT_NE) && op1->gtSetZSFlags() && op2->IsIntegralConst(0) &&
                     (op1->gtNext == op2) && (op2->gtNext == tree))
            {
                // Require codegen of op1 to set the flags.
                assert(!op1->gtSetFlags());
                op1->gtFlags |= GTF_SET_FLAGS;
            }
            else
            {
                SetRegOptional(op1);
            }
        }
    }
    else if (op1Type == op2Type)
    {
        // Note that TEST does not have a r,rm encoding like CMP has but we can still
        // contain the second operand because the emitter maps both r,rm and rm,r to
        // the same instruction code. This avoids the need to special case TEST here.
        if (op2->isMemoryOp())
        {
            MakeSrcContained(tree, op2);
        }
        else if (op1->isMemoryOp() && IsSafeToContainMem(tree, op1))
        {
            MakeSrcContained(tree, op1);
        }
        else if (op1->IsCnsIntOrI())
        {
            // TODO-CQ: We should be able to support swapping op1 and op2 to generate cmp reg, imm,
            // but there is currently an assert in CodeGen::genCompareInt().
            // https://github.com/dotnet/coreclr/issues/7270
            SetRegOptional(op2);
        }
        else
        {
            // One of op1 or op2 could be marked as reg optional
            // to indicate that codegen can still generate code
            // if one of them is on stack.
            SetRegOptional(PreferredRegOptionalOperand(tree));
        }
    }
}

//--------------------------------------------------------------------------------------------
// TreeNodeInfoInitIfRMWMemOp: Checks to see if there is a RMW memory operation rooted at
// GT_STOREIND node and if so will mark register requirements for nodes under storeInd so
// that CodeGen will generate a single instruction of the form:
//
//         binOp [addressing mode], reg
//
// Parameters
//         storeInd   - GT_STOREIND node
//
// Return value
//         True, if RMW memory op tree pattern is recognized and op counts are set.
//         False otherwise.
//
bool Lowering::TreeNodeInfoInitIfRMWMemOp(GenTreePtr storeInd)
{
    assert(storeInd->OperGet() == GT_STOREIND);

    // SSE2 doesn't support RMW on float values
    assert(!varTypeIsFloating(storeInd));

    // Terminology:
    // indirDst = memory write of an addr mode  (i.e. storeind destination)
    // indirSrc = value being written to memory (i.e. storeind source which could a binary/unary op)
    // indirCandidate = memory read i.e. a gtInd of an addr mode
    // indirOpSource = source operand used in binary/unary op (i.e. source operand of indirSrc node)

    GenTreePtr indirCandidate = nullptr;
    GenTreePtr indirOpSource  = nullptr;

    if (!IsRMWMemOpRootedAtStoreInd(storeInd, &indirCandidate, &indirOpSource))
    {
        JITDUMP("Lower of StoreInd didn't mark the node as self contained for reason: %d\n",
                storeInd->AsStoreInd()->GetRMWStatus());
        DISPTREERANGE(BlockRange(), storeInd);
        return false;
    }

    GenTreePtr indirDst = storeInd->gtGetOp1();
    GenTreePtr indirSrc = storeInd->gtGetOp2();
    genTreeOps oper     = indirSrc->OperGet();

    // At this point we have successfully detected a RMW memory op of one of the following forms
    //         storeInd(indirDst, indirSrc(indirCandidate, indirOpSource)) OR
    //         storeInd(indirDst, indirSrc(indirOpSource, indirCandidate) in case of commutative operations OR
    //         storeInd(indirDst, indirSrc(indirCandidate) in case of unary operations
    //
    // Here indirSrc = one of the supported binary or unary operation for RMW of memory
    //      indirCandidate = a GT_IND node
    //      indirCandidateChild = operand of GT_IND indirCandidate
    //
    // The logic below essentially does the following
    //      Make indirOpSource contained.
    //      Make indirSrc contained.
    //      Make indirCandidate contained.
    //      Make indirCandidateChild contained.
    //      Make indirDst contained except when it is a GT_LCL_VAR or GT_CNS_INT that doesn't fit within addr
    //      base.
    // Note that due to the way containment is supported, we accomplish some of the above by clearing operand counts
    // and directly propagating them upward.
    //

    TreeNodeInfo* info = &(storeInd->gtLsraInfo);
    info->dstCount     = 0;

    if (GenTree::OperIsBinary(oper))
    {
        // On Xarch RMW operations require that the source memory-op be in a register.
        assert(!indirOpSource->isMemoryOp() || indirOpSource->gtLsraInfo.dstCount == 1);
        JITDUMP("Lower succesfully detected an assignment of the form: *addrMode BinOp= source\n");
        info->srcCount = indirOpSource->gtLsraInfo.dstCount;
    }
    else
    {
        assert(GenTree::OperIsUnary(oper));
        JITDUMP("Lower succesfully detected an assignment of the form: *addrMode = UnaryOp(*addrMode)\n");
        info->srcCount = 0;
    }
    DISPTREERANGE(BlockRange(), storeInd);

    m_lsra->clearOperandCounts(indirSrc);
    m_lsra->clearOperandCounts(indirCandidate);

    GenTreePtr indirCandidateChild = indirCandidate->gtGetOp1();
    if (indirCandidateChild->OperGet() == GT_LEA)
    {
        GenTreeAddrMode* addrMode = indirCandidateChild->AsAddrMode();

        if (addrMode->HasBase())
        {
            assert(addrMode->Base()->OperIsLeaf());
            m_lsra->clearOperandCounts(addrMode->Base());
            info->srcCount++;
        }

        if (addrMode->HasIndex())
        {
            assert(addrMode->Index()->OperIsLeaf());
            m_lsra->clearOperandCounts(addrMode->Index());
            info->srcCount++;
        }

        m_lsra->clearOperandCounts(indirDst);
    }
    else
    {
        assert(indirCandidateChild->OperGet() == GT_LCL_VAR || indirCandidateChild->OperGet() == GT_LCL_VAR_ADDR ||
               indirCandidateChild->OperGet() == GT_CLS_VAR_ADDR || indirCandidateChild->OperGet() == GT_CNS_INT);

        // If it is a GT_LCL_VAR, it still needs the reg to hold the address.
        // We would still need a reg for GT_CNS_INT if it doesn't fit within addressing mode base.
        // For GT_CLS_VAR_ADDR, we don't need a reg to hold the address, because field address value is known at jit
        // time. Also, we don't need a reg for GT_CLS_VAR_ADDR.
        if (indirCandidateChild->OperGet() == GT_LCL_VAR_ADDR || indirCandidateChild->OperGet() == GT_CLS_VAR_ADDR)
        {
            m_lsra->clearOperandCounts(indirDst);
        }
        else if (indirCandidateChild->IsCnsIntOrI() && indirCandidateChild->AsIntConCommon()->FitsInAddrBase(comp))
        {
            m_lsra->clearOperandCounts(indirDst);
        }
        else
        {
            // Need a reg and hence increment src count of storeind
            info->srcCount += indirCandidateChild->gtLsraInfo.dstCount;
        }
    }
    m_lsra->clearOperandCounts(indirCandidateChild);

#ifdef _TARGET_X86_
    if (varTypeIsByte(storeInd))
    {
        // If storeInd is of TYP_BYTE, set indirOpSources to byteable registers.
        bool containedNode = indirOpSource->gtLsraInfo.dstCount == 0;
        if (!containedNode)
        {
            regMaskTP regMask = indirOpSource->gtLsraInfo.getSrcCandidates(m_lsra);
            assert(regMask != RBM_NONE);
            indirOpSource->gtLsraInfo.setSrcCandidates(m_lsra, regMask & ~RBM_NON_BYTE_REGS);
        }
    }
#endif

    return true;
}

//------------------------------------------------------------------------
// TreeNodeInfoInitMul: Set the NodeInfo for a multiply.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitMul(GenTreePtr tree)
{
#if defined(_TARGET_X86_)
    assert(tree->OperGet() == GT_MUL || tree->OperGet() == GT_MULHI || tree->OperGet() == GT_MUL_LONG);
#else
    assert(tree->OperGet() == GT_MUL || tree->OperGet() == GT_MULHI);
#endif
    TreeNodeInfo* info = &(tree->gtLsraInfo);

    info->srcCount = 2;
    info->dstCount = 1;

    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtOp.gtOp2;

    // Case of float/double mul.
    if (varTypeIsFloating(tree->TypeGet()))
    {
        assert(tree->OperGet() == GT_MUL);

        if (op2->isMemoryOp() || op2->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(tree, op2);
        }
        else if (op1->IsCnsNonZeroFltOrDbl() || (op1->isMemoryOp() && IsSafeToContainMem(tree, op1)))
        {
            // Since  GT_MUL is commutative, we will try to re-order operands if it is safe to
            // generate more efficient code sequence for the case of GT_MUL(op1=memOp, op2=non-memOp)
            MakeSrcContained(tree, op1);
        }
        else
        {
            // If there are no containable operands, we can make an operand reg optional.
            SetRegOptionalForBinOp(tree);
        }
        return;
    }

    bool       isUnsignedMultiply    = ((tree->gtFlags & GTF_UNSIGNED) != 0);
    bool       requiresOverflowCheck = tree->gtOverflowEx();
    bool       useLeaEncoding        = false;
    GenTreePtr memOp                 = nullptr;

    bool                 hasImpliedFirstOperand = false;
    GenTreeIntConCommon* imm                    = nullptr;
    GenTreePtr           other                  = nullptr;

    // There are three forms of x86 multiply:
    // one-op form:     RDX:RAX = RAX * r/m
    // two-op form:     reg *= r/m
    // three-op form:   reg = r/m * imm

    // This special widening 32x32->64 MUL is not used on x64
    CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(_TARGET_X86_)
    if (tree->OperGet() != GT_MUL_LONG)
#endif
    {
        assert((tree->gtFlags & GTF_MUL_64RSLT) == 0);
    }

    // Multiply should never be using small types
    assert(!varTypeIsSmall(tree->TypeGet()));

    // We do use the widening multiply to implement
    // the overflow checking for unsigned multiply
    //
    if (isUnsignedMultiply && requiresOverflowCheck)
    {
        // The only encoding provided is RDX:RAX = RAX * rm
        //
        // Here we set RAX as the only destination candidate
        // In LSRA we set the kill set for this operation to RBM_RAX|RBM_RDX
        //
        info->setDstCandidates(m_lsra, RBM_RAX);
        hasImpliedFirstOperand = true;
    }
    else if (tree->OperGet() == GT_MULHI)
    {
        // Have to use the encoding:RDX:RAX = RAX * rm. Since we only care about the
        // upper 32 bits of the result set the destination candidate to REG_RDX.
        info->setDstCandidates(m_lsra, RBM_RDX);
        hasImpliedFirstOperand = true;
    }
#if defined(_TARGET_X86_)
    else if (tree->OperGet() == GT_MUL_LONG)
    {
        // have to use the encoding:RDX:RAX = RAX * rm
        info->setDstCandidates(m_lsra, RBM_RAX);
        hasImpliedFirstOperand = true;
    }
#endif
    else if (IsContainableImmed(tree, op2) || IsContainableImmed(tree, op1))
    {
        if (IsContainableImmed(tree, op2))
        {
            imm   = op2->AsIntConCommon();
            other = op1;
        }
        else
        {
            imm   = op1->AsIntConCommon();
            other = op2;
        }

        // CQ: We want to rewrite this into a LEA
        ssize_t immVal = imm->AsIntConCommon()->IconValue();
        if (!requiresOverflowCheck && (immVal == 3 || immVal == 5 || immVal == 9))
        {
            useLeaEncoding = true;
        }

        MakeSrcContained(tree, imm); // The imm is always contained
        if (other->isMemoryOp())
        {
            memOp = other; // memOp may be contained below
        }
    }

    // We allow one operand to be a contained memory operand.
    // The memory op type must match with the 'tree' type.
    // This is because during codegen we use 'tree' type to derive EmitTypeSize.
    // E.g op1 type = byte, op2 type = byte but GT_MUL tree type is int.
    //
    if (memOp == nullptr && op2->isMemoryOp())
    {
        memOp = op2;
    }

    // To generate an LEA we need to force memOp into a register
    // so don't allow memOp to be 'contained'
    //
    if (!useLeaEncoding)
    {
        if ((memOp != nullptr) && (memOp->TypeGet() == tree->TypeGet()) && IsSafeToContainMem(tree, memOp))
        {
            MakeSrcContained(tree, memOp);
        }
        else if (imm != nullptr)
        {
            // Has a contained immediate operand.
            // Only 'other' operand can be marked as reg optional.
            assert(other != nullptr);
            SetRegOptional(other);
        }
        else if (hasImpliedFirstOperand)
        {
            // Only op2 can be marke as reg optional.
            SetRegOptional(op2);
        }
        else
        {
            // If there are no containable operands, we can make either of op1 or op2
            // as reg optional.
            SetRegOptionalForBinOp(tree);
        }
    }
}

//------------------------------------------------------------------------------
// SetContainsAVXFlags: Set ContainsAVX flag when it is floating type, set
// Contains256bitAVX flag when SIMD vector size is 32 bytes
//
// Arguments:
//    isFloatingPointType   - true if it is floating point type
//    sizeOfSIMDVector      - SIMD Vector size
//
void Lowering::SetContainsAVXFlags(bool isFloatingPointType /* = true */, unsigned sizeOfSIMDVector /* = 0*/)
{
#ifdef FEATURE_AVX_SUPPORT
    if (isFloatingPointType)
    {
        if (comp->getFloatingPointInstructionSet() == InstructionSet_AVX)
        {
            comp->getEmitter()->SetContainsAVX(true);
        }
        if (sizeOfSIMDVector == 32 && comp->getSIMDInstructionSet() == InstructionSet_AVX)
        {
            comp->getEmitter()->SetContains256bitAVX(true);
        }
    }
#endif
}

#ifdef _TARGET_X86_
//------------------------------------------------------------------------
// ExcludeNonByteableRegisters: Determines if we need to exclude non-byteable registers for
// various reasons
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    If we need to exclude non-byteable registers
//
bool Lowering::ExcludeNonByteableRegisters(GenTree* tree)
{
    // Example1: GT_STOREIND(byte, addr, op2) - storeind of byte sized value from op2 into mem 'addr'
    // Storeind itself will not produce any value and hence dstCount=0. But op2 could be TYP_INT
    // value. In this case we need to exclude esi/edi from the src candidates of op2.
    if (varTypeIsByte(tree))
    {
        return true;
    }
    // Example2: GT_CAST(int <- bool <- int) - here type of GT_CAST node is int and castToType is bool.
    else if ((tree->OperGet() == GT_CAST) && varTypeIsByte(tree->CastToType()))
    {
        return true;
    }
    else if (tree->OperIsCompare())
    {
        GenTree* op1 = tree->gtGetOp1();
        GenTree* op2 = tree->gtGetOp2();

        // Example3: GT_EQ(int, op1 of type ubyte, op2 of type ubyte) - in this case codegen uses
        // ubyte as the result of comparison and if the result needs to be materialized into a reg
        // simply zero extend it to TYP_INT size.  Here is an example of generated code:
        //         cmp dl, byte ptr[addr mode]
        //         movzx edx, dl
        if (varTypeIsByte(op1) && varTypeIsByte(op2))
        {
            return true;
        }
        // Example4: GT_EQ(int, op1 of type ubyte, op2 is GT_CNS_INT) - in this case codegen uses
        // ubyte as the result of the comparison and if the result needs to be materialized into a reg
        // simply zero extend it to TYP_INT size.
        else if (varTypeIsByte(op1) && op2->IsCnsIntOrI())
        {
            return true;
        }
        // Example4: GT_EQ(int, op1 is GT_CNS_INT, op2 of type ubyte) - in this case codegen uses
        // ubyte as the result of the comparison and if the result needs to be materialized into a reg
        // simply zero extend it to TYP_INT size.
        else if (op1->IsCnsIntOrI() && varTypeIsByte(op2))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
#ifdef FEATURE_SIMD
    else if (tree->OperGet() == GT_SIMD)
    {
        GenTreeSIMD* simdNode = tree->AsSIMD();
        switch (simdNode->gtSIMDIntrinsicID)
        {
            case SIMDIntrinsicOpEquality:
            case SIMDIntrinsicOpInEquality:
                // We manifest it into a byte register, so the target must be byteable.
                return true;

            case SIMDIntrinsicGetItem:
            {
                // This logic is duplicated from genSIMDIntrinsicGetItem().
                // When we generate code for a SIMDIntrinsicGetItem, under certain circumstances we need to
                // generate a movzx/movsx. On x86, these require byteable registers. So figure out which
                // cases will require this, so the non-byteable registers can be excluded.

                GenTree*  op1      = simdNode->gtGetOp1();
                GenTree*  op2      = simdNode->gtGetOp2();
                var_types baseType = simdNode->gtSIMDBaseType;
                if (!op1->isMemoryOp() && op2->IsCnsIntOrI() && varTypeIsSmallInt(baseType))
                {
                    bool     ZeroOrSignExtnReqd = true;
                    unsigned baseSize           = genTypeSize(baseType);
                    if (baseSize == 1)
                    {
                        if ((op2->gtIntCon.gtIconVal % 2) == 1)
                        {
                            ZeroOrSignExtnReqd = (baseType == TYP_BYTE);
                        }
                    }
                    else
                    {
                        assert(baseSize == 2);
                        ZeroOrSignExtnReqd = (baseType == TYP_SHORT);
                    }
                    return ZeroOrSignExtnReqd;
                }
                break;
            }

            default:
                break;
        }
        return false;
    }
#endif // FEATURE_SIMD
    else
    {
        return false;
    }
}
#endif // _TARGET_X86_

#endif // _TARGET_XARCH_

#endif // !LEGACY_BACKEND
