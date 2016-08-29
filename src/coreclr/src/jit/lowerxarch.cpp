// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for AMD64                              XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the AMD64         XX
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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_XARCH_

#include "jit.h"
#include "lower.h"

// xarch supports both ROL and ROR instructions so no lowering is required.
void Lowering::LowerRotate(GenTreePtr tree)
{
}

// there is not much lowering to do with storing a local but
// we do some handling of contained immediates and widening operations of unsigneds
void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
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
    if (storeLoc->TypeGet() == TYP_SIMD12)
    {
        // Need an additional register to extract upper 4 bytes of Vector3.
        info->internalFloatCount = 1;
        info->setInternalCandidates(m_lsra, m_lsra->allSIMDRegs());

        // In this case don't mark the operand as contained as we want it to
        // be evaluated into an xmm register
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

    // Try to widen the ops if they are going into a local var.
    if ((storeLoc->gtOper == GT_STORE_LCL_VAR) && (storeLoc->gtOp1->gtOper == GT_CNS_INT))
    {
        GenTreeIntCon* con  = storeLoc->gtOp1->AsIntCon();
        ssize_t        ival = con->gtIconVal;

        unsigned   varNum = storeLoc->gtLclNum;
        LclVarDsc* varDsc = comp->lvaTable + varNum;

        if (varDsc->lvIsSIMDType())
        {
            noway_assert(storeLoc->gtType != TYP_STRUCT);
        }
        unsigned size = genTypeSize(storeLoc);
        // If we are storing a constant into a local variable
        // we extend the size of the store here
        if ((size < 4) && !varTypeIsStruct(varDsc))
        {
            if (!varTypeIsUnsigned(varDsc))
            {
                if (genTypeSize(storeLoc) == 1)
                {
                    if ((ival & 0x7f) != ival)
                    {
                        ival = ival | 0xffffff00;
                    }
                }
                else
                {
                    assert(genTypeSize(storeLoc) == 2);
                    if ((ival & 0x7fff) != ival)
                    {
                        ival = ival | 0xffff0000;
                    }
                }
            }

            // A local stack slot is at least 4 bytes in size, regardless of
            // what the local var is typed as, so auto-promote it here
            // unless it is a field of a promoted struct
            // TODO-XArch-CQ: if the field is promoted shouldn't we also be able to do this?
            if (!varDsc->lvIsStructField)
            {
                storeLoc->gtType = TYP_INT;
                con->SetIconValue(ival);
            }
        }
    }
}

/**
 * Takes care of annotating the register requirements
 * for every TreeNodeInfo struct that maps to each tree node.
 * Preconditions:
 *    LSRA Has been initialized and there is a TreeNodeInfo node
 *    already allocated and initialized for every tree in the IR.
 * Postconditions:
 *    Every TreeNodeInfo instance has the right annotations on register
 *    requirements needed by LSRA to build the Interval Table (source,
 *    destination and internal [temp] register counts).
 *    This code is refactored originally from LSRA.
 */
void Lowering::TreeNodeInfoInit(GenTree* tree)
{
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    TreeNodeInfo* info = &(tree->gtLsraInfo);

    switch (tree->OperGet())
    {
        GenTree* op1;
        GenTree* op2;

        default:
            TreeNodeInfoInitSimple(tree);
            break;

        case GT_LCL_FLD:
            info->srcCount = 0;
            info->dstCount = 1;

#ifdef FEATURE_SIMD
            // Need an additional register to read upper 4 bytes of Vector3.
            if (tree->TypeGet() == TYP_SIMD12)
            {
                // We need an internal register different from targetReg in which 'tree' produces its result
                // because both targetReg and internal reg will be in use at the same time. This is achieved
                // by asking for two internal registers.
                info->internalFloatCount = 2;
                info->setInternalCandidates(m_lsra, m_lsra->allSIMDRegs());
            }
#endif
            break;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
            info->srcCount = 1;
            info->dstCount = 0;
            LowerStoreLoc(tree->AsLclVarCommon());
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
            if (tree->gtNext == nullptr)
            {
                // An uncontained GT_LONG node needs to consume its source operands
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
            // this just turns into a compare of its child with an int
            // + a conditional call
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
            SetMulOpCounts(tree);
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
            TreeNodeInfoInitShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            LowerCmp(tree);
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

#ifdef _TARGET_X86_
        case GT_OBJ:
            NYI_X86("GT_OBJ");
#endif //_TARGET_X86_

        case GT_INITBLK:
        case GT_COPYBLK:
        case GT_COPYOBJ:
            TreeNodeInfoInitBlockStore(tree->AsBlkOp());
            break;

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        case GT_PUTARG_STK:
            TreeNodeInfoInitPutArgStk(tree);
            break;
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

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
                LowerGCWriteBarrier(tree);
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
                if (SetStoreIndOpCountsIfRMWMemOp(tree))
                {
                    break;
                }
            }

            SetIndirAddrOpCounts(tree);
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
            SetIndirAddrOpCounts(tree);
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

#ifdef _TARGET_X86_
    // Exclude RBM_NON_BYTE_REGS from dst candidates of tree node and src candidates of operands
    // if the tree node is a byte type.
    //
    // Example1: GT_STOREIND(byte, addr, op2) - storeind of byte sized value from op2 into mem 'addr'
    // Storeind itself will not produce any value and hence dstCount=0. But op2 could be TYP_INT
    // value. In this case we need to exclude esi/edi from the src candidates of op2.
    //
    // Example2: GT_CAST(int <- bool <- int) - here type of GT_CAST node is int and castToType is bool.
    //
    // Example3: GT_EQ(int, op1 of type ubyte, op2 of type ubyte) - in this case codegen uses
    // ubyte as the result of comparison and if the result needs to be materialized into a reg
    // simply zero extend it to TYP_INT size.  Here is an example of generated code:
    //         cmp dl, byte ptr[addr mode]
    //         movzx edx, dl
    //
    // Though this looks conservative in theory, in practice we could not think of a case where
    // the below logic leads to conservative register specification.  In future when or if we find
    // one such case, this logic needs to be fine tuned for that case(s).
    if (varTypeIsByte(tree) || ((tree->OperGet() == GT_CAST) && varTypeIsByte(tree->CastToType())) ||
        (tree->OperIsCompare() && varTypeIsByte(tree->gtGetOp1()) && varTypeIsByte(tree->gtGetOp2())))
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

    // We need to be sure that we've set info->srcCount and info->dstCount appropriately
    assert((info->dstCount < 2) || (tree->IsMultiRegCall() && info->dstCount == MAX_RET_REG_COUNT));
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
    info->dstCount     = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
    if (kind & (GTK_CONST | GTK_LEAF))
    {
        info->srcCount = 0;
    }
    else if (kind & (GTK_SMPOP))
    {
        if (tree->gtGetOp2() != nullptr)
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
    }
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

#if FEATURE_VARARG
    bool callHasFloatRegArgs = false;
#endif // !FEATURE_VARARG

    // First, count reg args
    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->IsList());

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

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
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
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
            continue;
        }

        regNumber argReg    = REG_NA;
        regMaskTP argMask   = RBM_NONE;
        short     regCount  = 0;
        bool      isOnStack = true;
        if (curArgTabEntry->regNum != REG_STK)
        {
            isOnStack         = false;
            var_types argType = argNode->TypeGet();

#if FEATURE_VARARG
            callHasFloatRegArgs |= varTypeIsFloating(argType);
#endif // !FEATURE_VARARG

            argReg   = curArgTabEntry->regNum;
            regCount = 1;

            // Default case is that we consume one source; modify this later (e.g. for
            // promoted structs)
            info->srcCount++;

            argMask = genRegMask(argReg);
            argNode = argNode->gtEffectiveVal();
        }

        // If the struct arg is wrapped in CPYBLK the type of the param will be TYP_VOID.
        // Use the curArgTabEntry's isStruct to get whether the param is a struct.
        if (varTypeIsStruct(argNode) FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY(|| curArgTabEntry->isStruct))
        {
            unsigned   originalSize = 0;
            LclVarDsc* varDsc       = nullptr;
            if (argNode->gtOper == GT_LCL_VAR)
            {
                varDsc       = compiler->lvaTable + argNode->gtLclVarCommon.gtLclNum;
                originalSize = varDsc->lvSize();
            }
            else if (argNode->gtOper == GT_MKREFANY)
            {
                originalSize = 2 * TARGET_POINTER_SIZE;
            }
            else if (argNode->gtOper == GT_OBJ)
            {
                noway_assert(!"GT_OBJ not supported for amd64");
            }
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
            else if (argNode->gtOper == GT_PUTARG_REG)
            {
                originalSize = genTypeSize(argNode->gtType);
            }
            else if (argNode->gtOper == GT_LIST)
            {
                originalSize = 0;

                // There could be up to 2 PUTARG_REGs in the list
                GenTreeArgList* argListPtr   = argNode->AsArgList();
                unsigned        iterationNum = 0;
                for (; argListPtr; argListPtr = argListPtr->Rest())
                {
                    GenTreePtr putArgRegNode = argListPtr->gtOp.gtOp1;
                    assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                    if (iterationNum == 0)
                    {
                        varDsc       = compiler->lvaTable + putArgRegNode->gtOp.gtOp1->gtLclVarCommon.gtLclNum;
                        originalSize = varDsc->lvSize();
                        assert(originalSize != 0);
                    }
                    else
                    {
                        // Need an extra source for every node, but the first in the list.
                        info->srcCount++;

                        // Get the mask for the second putarg_reg
                        argMask = genRegMask(curArgTabEntry->otherRegNum);
                    }

                    putArgRegNode->gtLsraInfo.setDstCandidates(l, argMask);
                    putArgRegNode->gtLsraInfo.setSrcCandidates(l, argMask);

                    // To avoid redundant moves, have the argument child tree computed in the
                    // register in which the argument is passed to the call.
                    putArgRegNode->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, l->getUseCandidates(putArgRegNode));
                    iterationNum++;
                }

                assert(iterationNum <= CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS);
            }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
            else
            {
                noway_assert(!"Can't predict unsupported TYP_STRUCT arg kind");
            }

            unsigned slots          = ((unsigned)(roundUp(originalSize, TARGET_POINTER_SIZE))) / REGSIZE_BYTES;
            unsigned remainingSlots = slots;

            if (!isOnStack)
            {
                remainingSlots = slots - 1;

                regNumber reg = (regNumber)(argReg + 1);
                while (remainingSlots > 0 && reg <= REG_ARG_LAST)
                {
                    argMask |= genRegMask(reg);
                    reg = (regNumber)(reg + 1);
                    remainingSlots--;
                    regCount++;
                }
            }

            short internalIntCount = 0;
            if (remainingSlots > 0)
            {
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
                // This TYP_STRUCT argument is also passed in the outgoing argument area
                // We need a register to address the TYP_STRUCT
                internalIntCount = 1;
#else  // FEATURE_UNIX_AMD64_STRUCT_PASSING
                // And we may need 2
                internalIntCount            = 2;
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
            }
            argNode->gtLsraInfo.internalIntCount = internalIntCount;

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
            if (argNode->gtOper == GT_PUTARG_REG)
            {
                argNode->gtLsraInfo.setDstCandidates(l, argMask);
                argNode->gtLsraInfo.setSrcCandidates(l, argMask);
            }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
        }
        else
        {
            argNode->gtLsraInfo.setDstCandidates(l, argMask);
            argNode->gtLsraInfo.setSrcCandidates(l, argMask);
        }

        // To avoid redundant moves, have the argument child tree computed in the
        // register in which the argument is passed to the call.
        if (argNode->gtOper == GT_PUTARG_REG)
        {
            argNode->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, l->getUseCandidates(argNode));
        }

#if FEATURE_VARARG
        // In the case of a varargs call, the ABI dictates that if we have floating point args,
        // we must pass the enregistered arguments in both the integer and floating point registers.
        // Since the integer register is not associated with this arg node, we will reserve it as
        // an internal register so that it is not used during the evaluation of the call node
        // (e.g. for the target).
        if (call->IsVarargs() && varTypeIsFloating(argNode))
        {
            regNumber targetReg = compiler->getCallArgIntRegister(argReg);
            info->setInternalIntCount(info->internalIntCount + 1);
            info->addInternalCandidates(l, genRegMask(targetReg));
        }
#endif // FEATURE_VARARG
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
#if !defined(_TARGET_64BIT_)
            if (arg->TypeGet() == TYP_LONG)
            {
                assert(arg->OperGet() == GT_LONG);
                GenTreePtr loArg = arg->gtGetOp1();
                GenTreePtr hiArg = arg->gtGetOp2();
                assert((loArg->OperGet() == GT_PUTARG_STK) && (hiArg->OperGet() == GT_PUTARG_STK));
                assert((loArg->gtLsraInfo.dstCount == 1) && (hiArg->gtLsraInfo.dstCount == 1));
                loArg->gtLsraInfo.isLocalDefUse = true;
                hiArg->gtLsraInfo.isLocalDefUse = true;
            }
            else
#endif // !defined(_TARGET_64BIT_)
            {
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
void Lowering::TreeNodeInfoInitBlockStore(GenTreeBlkOp* blkNode)
{
    GenTree*    dstAddr = blkNode->Dest();
    unsigned    size;
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    // Sources are dest address, initVal or source, and size
    blkNode->gtLsraInfo.srcCount = 3;
    blkNode->gtLsraInfo.dstCount = 0;

    if (blkNode->OperGet() == GT_INITBLK)
    {
        GenTreeInitBlk* initBlkNode = blkNode->AsInitBlk();

        GenTreePtr blockSize = initBlkNode->Size();
        GenTreePtr initVal   = initBlkNode->InitVal();

        // If we have an InitBlk with constant block size we can optimize several ways:
        // a) If the size is smaller than a small memory page but larger than INITBLK_UNROLL_LIMIT bytes
        //    we use rep stosb since this reduces the register pressure in LSRA and we have
        //    roughly the same performance as calling the helper.
        // b) If the size is <= INITBLK_UNROLL_LIMIT bytes and the fill byte is a constant,
        //    we can speed this up by unrolling the loop using SSE2 stores.  The reason for
        //    this threshold is because our last investigation (Fall 2013), more than 95% of initblks
        //    in our framework assemblies are actually <= INITBLK_UNROLL_LIMIT bytes size, so this is the
        //    preferred code sequence for the vast majority of cases.

        // This threshold will decide from using the helper or let the JIT decide to inline
        // a code sequence of its choice.
        ssize_t helperThreshold = max(INITBLK_STOS_LIMIT, INITBLK_UNROLL_LIMIT);

        // TODO-X86-CQ: Investigate whether a helper call would be beneficial on x86
        if (blockSize->IsCnsIntOrI() && blockSize->gtIntCon.gtIconVal <= helperThreshold)
        {
            ssize_t size = blockSize->gtIntCon.gtIconVal;

            // Always favor unrolling vs rep stos.
            if (size <= INITBLK_UNROLL_LIMIT && initVal->IsCnsIntOrI())
            {
                // The fill value of an initblk is interpreted to hold a
                // value of (unsigned int8) however a constant of any size
                // may practically reside on the evaluation stack. So extract
                // the lower byte out of the initVal constant and replicate
                // it to a larger constant whose size is sufficient to support
                // the largest width store of the desired inline expansion.

                ssize_t fill = initVal->gtIntCon.gtIconVal & 0xFF;
#ifdef _TARGET_AMD64_
                if (size < REGSIZE_BYTES)
                {
                    initVal->gtIntCon.gtIconVal = 0x01010101 * fill;
                }
                else
                {
                    initVal->gtIntCon.gtIconVal = 0x0101010101010101LL * fill;
                    initVal->gtType             = TYP_LONG;
                }
#else  // !_TARGET_AMD64_
                initVal->gtIntCon.gtIconVal = 0x01010101 * fill;
#endif // !_TARGET_AMD64_

                MakeSrcContained(blkNode, blockSize);

                // In case we have a buffer >= 16 bytes
                // we can use SSE2 to do a 128-bit store in a single
                // instruction.
                if (size >= XMM_REGSIZE_BYTES)
                {
                    // Reserve an XMM register to fill it with
                    // a pack of 16 init value constants.
                    blkNode->gtLsraInfo.internalFloatCount = 1;
                    blkNode->gtLsraInfo.setInternalCandidates(l, l->internalFloatRegCandidates());
                }
                initBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindUnroll;
            }
            else
            {
                // rep stos has the following register requirements:
                // a) The memory address to be in RDI.
                // b) The fill value has to be in RAX.
                // c) The buffer size must be in RCX.
                dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_RDI);
                initVal->gtLsraInfo.setSrcCandidates(l, RBM_RAX);
                blockSize->gtLsraInfo.setSrcCandidates(l, RBM_RCX);
                initBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindRepInstr;
            }
        }
        else
        {
#ifdef _TARGET_AMD64_
            // The helper follows the regular AMD64 ABI.
            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_0);
            initVal->gtLsraInfo.setSrcCandidates(l, RBM_ARG_1);
            blockSize->gtLsraInfo.setSrcCandidates(l, RBM_ARG_2);
            initBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindHelper;
#else  // !_TARGET_AMD64_
            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_RDI);
            initVal->gtLsraInfo.setSrcCandidates(l, RBM_RAX);
            blockSize->gtLsraInfo.setSrcCandidates(l, RBM_RCX);
            initBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindRepInstr;
#endif // !_TARGET_AMD64_
        }
    }
    else if (blkNode->OperGet() == GT_COPYOBJ)
    {
        GenTreeCpObj* cpObjNode = blkNode->AsCpObj();

        GenTreePtr clsTok  = cpObjNode->ClsTok();
        GenTreePtr srcAddr = cpObjNode->Source();

        unsigned slots = cpObjNode->gtSlots;

#ifdef DEBUG
        // CpObj must always have at least one GC-Pointer as a member.
        assert(cpObjNode->gtGcPtrCount > 0);

        assert(dstAddr->gtType == TYP_BYREF || dstAddr->gtType == TYP_I_IMPL);
        assert(clsTok->IsIconHandle());

        CORINFO_CLASS_HANDLE clsHnd    = (CORINFO_CLASS_HANDLE)clsTok->gtIntCon.gtIconVal;
        size_t               classSize = compiler->info.compCompHnd->getClassSize(clsHnd);
        size_t               blkSize   = roundUp(classSize, TARGET_POINTER_SIZE);

        // Currently, the EE always round up a class data structure so
        // we are not handling the case where we have a non multiple of pointer sized
        // struct. This behavior may change in the future so in order to keeps things correct
        // let's assert it just to be safe. Going forward we should simply
        // handle this case.
        assert(classSize == blkSize);
        assert((blkSize / TARGET_POINTER_SIZE) == slots);
        assert(cpObjNode->HasGCPtr());
#endif

        bool IsRepMovsProfitable = false;

        // If the destination is not on the stack, let's find out if we
        // can improve code size by using rep movsq instead of generating
        // sequences of movsq instructions.
        if (!dstAddr->OperIsLocalAddr())
        {
            // Let's inspect the struct/class layout and determine if it's profitable
            // to use rep movsq for copying non-gc memory instead of using single movsq
            // instructions for each memory slot.
            unsigned i      = 0;
            BYTE*    gcPtrs = cpObjNode->gtGcPtrs;

            do
            {
                unsigned nonGCSlots = 0;
                // Measure a contiguous non-gc area inside the struct and note the maximum.
                while (i < slots && gcPtrs[i] == TYPE_GC_NONE)
                {
                    nonGCSlots++;
                    i++;
                }

                while (i < slots && gcPtrs[i] != TYPE_GC_NONE)
                {
                    i++;
                }

                if (nonGCSlots >= CPOBJ_NONGC_SLOTS_LIMIT)
                {
                    IsRepMovsProfitable = true;
                    break;
                }
            } while (i < slots);
        }
        else if (slots >= CPOBJ_NONGC_SLOTS_LIMIT)
        {
            IsRepMovsProfitable = true;
        }

        // There are two cases in which we need to materialize the
        // struct size:
        // a) When the destination is on the stack we don't need to use the
        //    write barrier, we can just simply call rep movsq and get a win in codesize.
        // b) If we determine we have contiguous non-gc regions in the struct where it's profitable
        //    to use rep movsq instead of a sequence of single movsq instructions.  According to the
        //    Intel Manual, the sweet spot for small structs is between 4 to 12 slots of size where
        //    the entire operation takes 20 cycles and encodes in 5 bytes (moving RCX, and calling rep movsq).
        if (IsRepMovsProfitable)
        {
            // We need the size of the contiguous Non-GC-region to be in RCX to call rep movsq.
            MakeSrcContained(blkNode, clsTok);
            blkNode->gtLsraInfo.internalIntCount = 1;
            blkNode->gtLsraInfo.setInternalCandidates(l, RBM_RCX);
        }
        else
        {
            // We don't need to materialize the struct size because we will unroll
            // the loop using movsq that automatically increments the pointers.
            MakeSrcContained(blkNode, clsTok);
        }

        dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_RDI);
        srcAddr->gtLsraInfo.setSrcCandidates(l, RBM_RSI);
    }
    else
    {
        assert(blkNode->OperGet() == GT_COPYBLK);
        GenTreeCpBlk* cpBlkNode = blkNode->AsCpBlk();

        GenTreePtr blockSize = cpBlkNode->Size();
        GenTreePtr srcAddr   = cpBlkNode->Source();

        // In case of a CpBlk with a constant size and less than CPBLK_MOVS_LIMIT size
        // we can use rep movs to generate code instead of the helper call.

        // This threshold will decide from using the helper or let the JIT decide to inline
        // a code sequence of its choice.
        ssize_t helperThreshold = max(CPBLK_MOVS_LIMIT, CPBLK_UNROLL_LIMIT);

        // TODO-X86-CQ: Investigate whether a helper call would be beneficial on x86
        if (blockSize->IsCnsIntOrI() && blockSize->gtIntCon.gtIconVal <= helperThreshold)
        {
            assert(!blockSize->IsIconHandle());
            ssize_t size = blockSize->gtIntCon.gtIconVal;

            // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2.
            // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
            // our framework assemblies, so this is the main code generation scheme we'll use.
            if (size <= CPBLK_UNROLL_LIMIT)
            {
                MakeSrcContained(blkNode, blockSize);

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
                    if ((size % 2) != 0)
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
                }

                // If src or dst are on stack, we don't have to generate the address into a register
                // because it's just some constant+SP
                if (srcAddr->OperIsLocalAddr())
                {
                    MakeSrcContained(blkNode, srcAddr);
                }

                if (dstAddr->OperIsLocalAddr())
                {
                    MakeSrcContained(blkNode, dstAddr);
                }

                cpBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindUnroll;
            }
            else
            {
                dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_RDI);
                srcAddr->gtLsraInfo.setSrcCandidates(l, RBM_RSI);
                blockSize->gtLsraInfo.setSrcCandidates(l, RBM_RCX);
                cpBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindRepInstr;
            }
        }
#ifdef _TARGET_AMD64_
        else
        {
            // In case we have a constant integer this means we went beyond
            // CPBLK_MOVS_LIMIT bytes of size, still we should never have the case of
            // any GC-Pointers in the src struct.
            if (blockSize->IsCnsIntOrI())
            {
                assert(!blockSize->IsIconHandle());
            }

            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_0);
            srcAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_1);
            blockSize->gtLsraInfo.setSrcCandidates(l, RBM_ARG_2);
            cpBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindHelper;
        }
#elif defined(_TARGET_X86_)
        else
        {
            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_RDI);
            srcAddr->gtLsraInfo.setSrcCandidates(l, RBM_RSI);
            blockSize->gtLsraInfo.setSrcCandidates(l, RBM_RCX);
            cpBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindRepInstr;
        }
#endif // _TARGET_X86_
    }
}

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
//------------------------------------------------------------------------
// TreeNodeInfoInitPutArgStk: Set the NodeInfo for a GT_PUTARG_STK.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void Lowering::TreeNodeInfoInitPutArgStk(GenTree* tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    LinearScan*   l    = m_lsra;

    if (tree->TypeGet() != TYP_STRUCT)
    {
        TreeNodeInfoInitSimple(tree);
        return;
    }

    GenTreePutArgStk* putArgStkTree = tree->AsPutArgStk();

    GenTreePtr dst     = tree;
    GenTreePtr src     = tree->gtOp.gtOp1;
    GenTreePtr srcAddr = nullptr;

    if ((src->OperGet() == GT_OBJ) || (src->OperGet() == GT_IND))
    {
        srcAddr = src->gtOp.gtOp1;
    }
    else
    {
        assert(varTypeIsSIMD(tree));
    }
    info->srcCount = src->gtLsraInfo.dstCount;

    // If this is a stack variable address,
    // make the op1 contained, so this way
    // there is no unnecessary copying between registers.
    // To avoid assertion, increment the parent's source.
    // It is recovered below.
    bool haveLocalAddr = ((srcAddr != nullptr) && (srcAddr->OperIsLocalAddr()));
    if (haveLocalAddr)
    {
        info->srcCount += 1;
    }

    info->dstCount = 0;

    // In case of a CpBlk we could use a helper call. In case of putarg_stk we
    // can't do that since the helper call could kill some already set up outgoing args.
    // TODO-Amd64-Unix: converge the code for putarg_stk with cpyblk/cpyobj.
    // The cpyXXXX code is rather complex and this could cause it to be more complex, but
    // it might be the right thing to do.

    // This threshold will decide from using the helper or let the JIT decide to inline
    // a code sequence of its choice.
    ssize_t helperThreshold = max(CPBLK_MOVS_LIMIT, CPBLK_UNROLL_LIMIT);
    ssize_t size            = putArgStkTree->gtNumSlots * TARGET_POINTER_SIZE;

    // TODO-X86-CQ: The helper call either is not supported on x86 or required more work
    // (I don't know which).

    // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2.
    // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
    // our framework assemblies, so this is the main code generation scheme we'll use.
    if (size <= CPBLK_UNROLL_LIMIT && putArgStkTree->gtNumberReferenceSlots == 0)
    {
        // If we have a remainder smaller than XMM_REGSIZE_BYTES, we need an integer temp reg.
        //
        // x86 specific note: if the size is odd, the last copy operation would be of size 1 byte.
        // But on x86 only RBM_BYTE_REGS could be used as byte registers.  Therefore, exclude
        // RBM_NON_BYTE_REGS from internal candidates.
        if ((size & (XMM_REGSIZE_BYTES - 1)) != 0)
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

        if (size >= XMM_REGSIZE_BYTES)
        {
            // If we have a buffer larger than XMM_REGSIZE_BYTES,
            // reserve an XMM register to use it for a
            // series of 16-byte loads and stores.
            info->internalFloatCount = 1;
            info->addInternalCandidates(l, l->internalFloatRegCandidates());
        }

        if (haveLocalAddr)
        {
            MakeSrcContained(putArgStkTree, srcAddr);
        }

        // If src or dst are on stack, we don't have to generate the address into a register
        // because it's just some constant+SP
        putArgStkTree->gtPutArgStkKind = GenTreePutArgStk::PutArgStkKindUnroll;
    }
    else
    {
        info->internalIntCount += 3;
        info->setInternalCandidates(l, (RBM_RDI | RBM_RCX | RBM_RSI));
        if (haveLocalAddr)
        {
            MakeSrcContained(putArgStkTree, srcAddr);
        }

        putArgStkTree->gtPutArgStkKind = GenTreePutArgStk::PutArgStkKindRepInstr;
    }

    // Always mark the OBJ and ADDR as contained trees by the putarg_stk. The codegen will deal with this tree.
    MakeSrcContained(putArgStkTree, src);

    // Balance up the inc above.
    if (haveLocalAddr)
    {
        info->srcCount -= 1;
    }
}
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

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
    //      0                            -                  0
    //      const and <=6 reg words      -                  0
    //      const and >6 reg words       Yes                0
    //      const and <PageSize          No                 0 (amd64) 1 (x86)
    //      const and >=PageSize         No                 2
    //      Non-const                    Yes                0
    //      Non-const                    No                 2

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

    // If possible would like to have op1 in RAX to avoid a register move
    op1->gtLsraInfo.setSrcCandidates(l, RBM_RAX);

    // divisor can be an r/m, but the memory indirection must be of the same size as the divide
    if (op2->isMemoryOp() && (op2->TypeGet() == tree->TypeGet()))
    {
        MakeSrcContained(tree, op2);
    }
    else
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
    switch (simdTree->gtSIMDIntrinsicID)
    {
        GenTree* op2;

        case SIMDIntrinsicInit:
        {
            info->srcCount = 1;
            GenTree* op1   = tree->gtOp.gtOp1;

            // This sets all fields of a SIMD struct to the given value.
            // Mark op1 as contained if it is either zero or int constant of all 1's,
            // or a float constant with 16 or 32 byte simdType (AVX case)
            //
            // Should never see small int base type vectors except for zero initialization.
            assert(!varTypeIsSmallInt(simdTree->gtSIMDBaseType) || op1->IsIntegralConst(0));

            if (op1->IsFPZero() || op1->IsIntegralConst(0) ||
                (varTypeIsIntegral(simdTree->gtSIMDBaseType) && op1->IsIntegralConst(-1)))
            {
                MakeSrcContained(tree, tree->gtOp.gtOp1);
                info->srcCount = 0;
            }
            else if ((comp->getSIMDInstructionSet() == InstructionSet_AVX) &&
                     ((simdTree->gtSIMDSize == 16) || (simdTree->gtSIMDSize == 32)))
            {
                // Either op1 is a float or dbl constant or an addr
                if (op1->IsCnsFltOrDbl() || op1->OperIsLocalAddr())
                {
                    MakeSrcContained(tree, tree->gtOp.gtOp1);
                    info->srcCount = 0;
                }
            }
        }
        break;

        case SIMDIntrinsicInitN:
        {
            info->srcCount = (short)(simdTree->gtSIMDSize / genTypeSize(simdTree->gtSIMDBaseType));

            // Need an internal register to stitch together all the values into a single vector in a SIMD reg
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
            if ((comp->getSIMDInstructionSet() == InstructionSet_SSE2) ||
                (simdTree->gtOp.gtOp1->TypeGet() == TYP_SIMD32))
            {
                // For SSE, or AVX with 32-byte vectors, we also need an internal register as scratch.
                // Further we need the targetReg and internal reg to be distinct registers.
                // This is achieved by requesting two internal registers; thus one of them
                // will be different from targetReg.
                // Note that if this is a TYP_SIMD16 or smaller on AVX, then we don't need a tmpReg.
                //
                // See genSIMDIntrinsicDotProduct() for details on code sequence generated and
                // the need for scratch registers.
                info->internalFloatCount = 2;
                info->setInternalCandidates(lsra, lsra->allSIMDRegs());
            }
            info->srcCount = 2;
            break;

        case SIMDIntrinsicGetItem:
            // This implements get_Item method. The sources are:
            //  - the source SIMD struct
            //  - index (which element to get)
            // The result is baseType of SIMD struct.
            info->srcCount = 2;
            op2            = tree->gtOp.gtOp2;

            // If the index is a constant, mark it as contained.
            if (CheckImmedAndMakeContained(tree, op2))
            {
                info->srcCount = 1;
            }

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
            break;

        case SIMDIntrinsicSetX:
        case SIMDIntrinsicSetY:
        case SIMDIntrinsicSetZ:
        case SIMDIntrinsicSetW:
            // We need an internal integer register
            info->srcCount         = 2;
            info->internalIntCount = 1;
            info->setInternalCandidates(lsra, lsra->allRegs(TYP_INT));
            break;

        case SIMDIntrinsicCast:
            info->srcCount = 1;
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
            info->internalIntCount = 1;
        }
    }
}

void Lowering::LowerGCWriteBarrier(GenTree* tree)
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
// Specify register requirements for address expression of an indirection operation.
//
// Arguments:
//    indirTree    -   GT_IND or GT_STOREIND gentree node
//
void Lowering::SetIndirAddrOpCounts(GenTreePtr indirTree)
{
    assert(indirTree->isIndir());

    GenTreePtr    addr = indirTree->gtGetOp1();
    TreeNodeInfo* info = &(indirTree->gtLsraInfo);

    GenTreePtr base  = nullptr;
    GenTreePtr index = nullptr;
    unsigned   mul, cns;
    bool       rev;
    bool       modifiedSources = false;

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
        // both of the registers are used at the same time. This achieved by reserving
        // two internal registers
        if (indirTree->OperGet() == GT_IND)
        {
            (info->internalFloatCount)++;
        }

        info->setInternalCandidates(m_lsra, m_lsra->allSIMDRegs());

        return;
    }
#endif // FEATURE_SIMD

    // These nodes go into an addr mode:
    // - GT_CLS_VAR_ADDR turns into a constant.
    // - GT_LCL_VAR_ADDR is a stack addr mode.
    if ((addr->OperGet() == GT_CLS_VAR_ADDR) || (addr->OperGet() == GT_LCL_VAR_ADDR))
    {
        // make this contained, it turns into a constant that goes into an addr mode
        MakeSrcContained(indirTree, addr);
    }
    else if (addr->IsCnsIntOrI() && addr->AsIntConCommon()->FitsInAddrBase(comp) &&
             addr->gtLsraInfo.getDstCandidates(m_lsra) != RBM_VIRTUAL_STUB_PARAM)
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
    else if (addr->OperGet() == GT_LEA)
    {
        GenTreeAddrMode* lea = addr->AsAddrMode();
        base                 = lea->Base();
        index                = lea->Index();

        m_lsra->clearOperandCounts(addr);
        // The srcCount is decremented because addr is now "contained",
        // then we account for the base and index below, if they are non-null.
        info->srcCount--;
    }
    else if (comp->codeGen->genCreateAddrMode(addr, -1, true, 0, &rev, &base, &index, &mul, &cns, true /*nogen*/) &&
             !(modifiedSources = AreSourcesPossiblyModified(indirTree, base, index)))
    {
        // An addressing mode will be constructed that may cause some
        // nodes to not need a register, and cause others' lifetimes to be extended
        // to the GT_IND or even its parent if it's an assignment

        assert(base != addr);
        m_lsra->clearOperandCounts(addr);

        GenTreePtr arrLength = nullptr;

        // Traverse the computation below GT_IND to find the operands
        // for the addressing mode, marking the various constants and
        // intermediate results as not consuming/producing.
        // If the traversal were more complex, we might consider using
        // a traversal function, but the addressing mode is only made
        // up of simple arithmetic operators, and the code generator
        // only traverses one leg of each node.

        bool       foundBase  = (base == nullptr);
        bool       foundIndex = (index == nullptr);
        GenTreePtr nextChild  = nullptr;
        for (GenTreePtr child = addr; child != nullptr && !child->OperIsLeaf(); child = nextChild)
        {
            nextChild      = nullptr;
            GenTreePtr op1 = child->gtOp.gtOp1;
            GenTreePtr op2 = (child->OperIsBinary()) ? child->gtOp.gtOp2 : nullptr;

            if (op1 == base)
            {
                foundBase = true;
            }
            else if (op1 == index)
            {
                foundIndex = true;
            }
            else
            {
                m_lsra->clearOperandCounts(op1);
                if (!op1->OperIsLeaf())
                {
                    nextChild = op1;
                }
            }

            if (op2 != nullptr)
            {
                if (op2 == base)
                {
                    foundBase = true;
                }
                else if (op2 == index)
                {
                    foundIndex = true;
                }
                else
                {
                    m_lsra->clearOperandCounts(op2);
                    if (!op2->OperIsLeaf())
                    {
                        assert(nextChild == nullptr);
                        nextChild = op2;
                    }
                }
            }
        }
        assert(foundBase && foundIndex);
        info->srcCount--; // it gets incremented below.
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
    else
    {
        // it is nothing but a plain indir
        info->srcCount--; // base gets added in below
        base = addr;
    }

    if (base != nullptr)
    {
        info->srcCount++;
    }

    if (index != nullptr && !modifiedSources)
    {
        info->srcCount++;
    }
}

void Lowering::LowerCmp(GenTreePtr tree)
{
    TreeNodeInfo* info = &(tree->gtLsraInfo);

    info->srcCount = 2;
    info->dstCount = 1;

#ifdef _TARGET_X86_
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
    // ucomiss or ucomisd to compare, both of which support the following form
    // ucomis[s|d] xmm, xmm/mem.  That is only the second operand can be a memory
    // op.
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
            reverseOps = (tree->gtOper == GT_GT || tree->gtOper == GT_GE);
        }
        else
        {
            reverseOps = (tree->gtOper == GT_LT || tree->gtOper == GT_LE);
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

    bool hasShortCast = false;
    if (CheckImmedAndMakeContained(tree, op2))
    {
        bool op1CanBeContained = (op1Type == op2Type);
        if (!op1CanBeContained)
        {
            if (genTypeSize(op1Type) == genTypeSize(op2Type))
            {
                // The constant is of the correct size, but we don't have an exact type match
                // We can treat the isMemoryOp as "contained"
                op1CanBeContained = true;
            }
        }

        // Do we have a short compare against a constant in op2
        //
        if (varTypeIsSmall(op1Type))
        {
            GenTreeIntCon* con  = op2->AsIntCon();
            ssize_t        ival = con->gtIconVal;

            bool isEqualityCompare = (tree->gtOper == GT_EQ || tree->gtOper == GT_NE);
            bool useTest           = isEqualityCompare && (ival == 0);

            if (!useTest)
            {
                ssize_t lo         = 0; // minimum imm value allowed for cmp reg,imm
                ssize_t hi         = 0; // maximum imm value allowed for cmp reg,imm
                bool    isUnsigned = false;

                switch (op1Type)
                {
                    case TYP_BOOL:
                        op1Type = TYP_UBYTE;
                        __fallthrough;
                    case TYP_UBYTE:
                        lo         = 0;
                        hi         = 0x7f;
                        isUnsigned = true;
                        break;
                    case TYP_BYTE:
                        lo = -0x80;
                        hi = 0x7f;
                        break;
                    case TYP_CHAR:
                        lo         = 0;
                        hi         = 0x7fff;
                        isUnsigned = true;
                        break;
                    case TYP_SHORT:
                        lo = -0x8000;
                        hi = 0x7fff;
                        break;
                    default:
                        unreached();
                }

                if ((ival >= lo) && (ival <= hi))
                {
                    // We can perform a small compare with the immediate 'ival'
                    tree->gtFlags |= GTF_RELOP_SMALL;
                    if (isUnsigned && !isEqualityCompare)
                    {
                        tree->gtFlags |= GTF_UNSIGNED;
                    }
                    // We can treat the isMemoryOp as "contained"
                    op1CanBeContained = true;
                }
            }
        }

        if (op1CanBeContained)
        {
            if (op1->isMemoryOp())
            {
                MakeSrcContained(tree, op1);
            }
            else
            {
                bool op1IsMadeContained = false;

                // When op1 is a GT_AND we can often generate a single "test" instruction
                // instead of two instructions (an "and" instruction followed by a "cmp"/"test")
                //
                // This instruction can only be used for equality or inequality comparions.
                // and we must have a compare against zero.
                //
                // If we have a postive test for a single bit we can reverse the condition and
                // make the compare be against zero
                //
                // Example:
                //                  GT_EQ                              GT_NE
                //                  /   \                              /   \
                //             GT_AND   GT_CNS (0x100)  ==>>      GT_AND   GT_CNS (0)
                //             /    \                             /    \
                //          andOp1  GT_CNS (0x100)             andOp1  GT_CNS (0x100)
                //
                // We will mark the GT_AND node as contained if the tree is a equality compare with zero
                // Additionally when we do this we also allow for a contained memory operand for "andOp1".
                //
                bool isEqualityCompare = (tree->gtOper == GT_EQ || tree->gtOper == GT_NE);

                if (isEqualityCompare && (op1->OperGet() == GT_AND))
                {
                    GenTreePtr andOp2 = op1->gtOp.gtOp2;
                    if (IsContainableImmed(op1, andOp2))
                    {
                        ssize_t andOp2CnsVal = andOp2->AsIntConCommon()->IconValue();
                        ssize_t relOp2CnsVal = op2->AsIntConCommon()->IconValue();

                        if ((relOp2CnsVal == andOp2CnsVal) && isPow2(andOp2CnsVal))
                        {
                            // We have a single bit test, so now we can change the
                            // tree into the alternative form,
                            // so that we can generate a test instruction.

                            // Reverse the equality comparison
                            tree->gtOper = (tree->gtOper == GT_EQ) ? GT_NE : GT_EQ;

                            // Change the relOp2CnsVal to zero
                            relOp2CnsVal = 0;
                            op2->AsIntConCommon()->SetIconValue(0);
                        }

                        // Now do we have a equality compare with zero?
                        //
                        if (relOp2CnsVal == 0)
                        {
                            // Note that child nodes must be made contained before parent nodes

                            // Check for a memory operand for op1 with the test instruction
                            //
                            GenTreePtr andOp1 = op1->gtOp.gtOp1;
                            if (andOp1->isMemoryOp())
                            {
                                // If the type of value memoryOp (andOp1) is not the same as the type of constant
                                // (andOp2) check to see whether it is safe to mark AndOp1 as contained.  For e.g. in
                                // the following case it is not safe to mark andOp1 as contained
                                //    AndOp1 = signed byte and andOp2 is an int constant of value 512.
                                //
                                // If it is safe, we update the type and value of andOp2 to match with andOp1.
                                bool containable = (andOp1->TypeGet() == op1->TypeGet());
                                if (!containable)
                                {
                                    ssize_t newIconVal = 0;

                                    switch (andOp1->TypeGet())
                                    {
                                        default:
                                            break;
                                        case TYP_BYTE:
                                            newIconVal  = (signed char)andOp2CnsVal;
                                            containable = FitsIn<signed char>(andOp2CnsVal);
                                            break;
                                        case TYP_BOOL:
                                        case TYP_UBYTE:
                                            newIconVal  = andOp2CnsVal & 0xFF;
                                            containable = true;
                                            break;
                                        case TYP_SHORT:
                                            newIconVal  = (signed short)andOp2CnsVal;
                                            containable = FitsIn<signed short>(andOp2CnsVal);
                                            break;
                                        case TYP_CHAR:
                                            newIconVal  = andOp2CnsVal & 0xFFFF;
                                            containable = true;
                                            break;
                                        case TYP_INT:
                                            newIconVal  = (INT32)andOp2CnsVal;
                                            containable = FitsIn<INT32>(andOp2CnsVal);
                                            break;
                                        case TYP_UINT:
                                            newIconVal  = andOp2CnsVal & 0xFFFFFFFF;
                                            containable = true;
                                            break;

#ifdef _TARGET_64BIT_
                                        case TYP_LONG:
                                            newIconVal  = (INT64)andOp2CnsVal;
                                            containable = true;
                                            break;
                                        case TYP_ULONG:
                                            newIconVal  = (UINT64)andOp2CnsVal;
                                            containable = true;
                                            break;
#endif //_TARGET_64BIT_
                                    }

                                    if (containable)
                                    {
                                        andOp2->gtType = andOp1->TypeGet();
                                        andOp2->AsIntConCommon()->SetIconValue(newIconVal);
                                    }
                                }

                                // Mark the 'andOp1' memory operand as contained
                                // Note that for equality comparisons we don't need
                                // to deal with any signed or unsigned issues.
                                if (containable)
                                {
                                    MakeSrcContained(op1, andOp1);
                                }
                            }
                            // Mark the 'op1' (the GT_AND) operand as contained
                            MakeSrcContained(tree, op1);
                            op1IsMadeContained = true;

                            // During Codegen we will now generate "test andOp1, andOp2CnsVal"
                        }
                    }
                }
                else if (op1->OperGet() == GT_CAST)
                {
                    // If the op1 is a cast operation, and cast type is one byte sized unsigned type,
                    // we can directly use the number in register, instead of doing an extra cast step.
                    var_types  dstType       = op1->CastToType();
                    bool       isUnsignedDst = varTypeIsUnsigned(dstType);
                    emitAttr   castSize      = EA_ATTR(genTypeSize(dstType));
                    GenTreePtr castOp1       = op1->gtOp.gtOp1;
                    genTreeOps castOp1Oper   = castOp1->OperGet();
                    bool       safeOper      = false;

                    // It is not always safe to change the gtType of 'castOp1' to TYP_UBYTE
                    // For example when 'castOp1Oper' is a GT_RSZ or GT_RSH then we are shifting
                    // bits from the left into the lower bits.  If we change the type to a TYP_UBYTE
                    // we will instead generate a byte sized shift operation:  shr  al, 24
                    // For the following ALU operations is it safe to change the gtType to the
                    // smaller type:
                    //
                    if ((castOp1Oper == GT_CNS_INT) || (castOp1Oper == GT_CALL) || // the return value from a Call
                        (castOp1Oper == GT_LCL_VAR) || castOp1->OperIsLogical() || // GT_AND, GT_OR, GT_XOR
                        castOp1->isMemoryOp())                                     // isIndir() || isLclField();
                    {
                        safeOper = true;
                    }

                    if ((castSize == EA_1BYTE) && isUnsignedDst && // Unsigned cast to TYP_UBYTE
                        safeOper &&                                // Must be a safe operation
                        !op1->gtOverflow())                        // Must not be an overflow checking cast
                    {
                        // Currently all of the Oper accepted as 'safeOper' are
                        // non-overflow checking operations.  If we were to add
                        // an overflow checking operation then this assert needs
                        // to be moved above to guard entry to this block.
                        //
                        assert(!castOp1->gtOverflowEx()); // Must not be an overflow checking operation

                        GenTreePtr removeTreeNode = op1;
                        tree->gtOp.gtOp1          = castOp1;
                        op1                       = castOp1;
                        castOp1->gtType           = TYP_UBYTE;

                        // trim down the value if castOp1 is an int constant since its type changed to UBYTE.
                        if (castOp1Oper == GT_CNS_INT)
                        {
                            castOp1->gtIntCon.gtIconVal = (UINT8)castOp1->gtIntCon.gtIconVal;
                        }

                        if (op2->isContainedIntOrIImmed())
                        {
                            ssize_t val = (ssize_t)op2->AsIntConCommon()->IconValue();
                            if (val >= 0 && val <= 255)
                            {
                                op2->gtType = TYP_UBYTE;
                                tree->gtFlags |= GTF_UNSIGNED;

                                // right now the op1's type is the same as op2's type.
                                // if op1 is MemoryOp, we should make the op1 as contained node.
                                if (castOp1->isMemoryOp())
                                {
                                    MakeSrcContained(tree, op1);
                                    op1IsMadeContained = true;
                                }
                            }
                        }

                        BlockRange().Remove(removeTreeNode);
#ifdef DEBUG
                        if (comp->verbose)
                        {
                            printf("LowerCmp: Removing a GT_CAST to TYP_UBYTE and changing castOp1->gtType to "
                                   "TYP_UBYTE\n");
                            comp->gtDispTreeRange(BlockRange(), tree);
                        }
#endif
                    }
                }

                // If not made contained, op1 can be marked as reg-optional.
                if (!op1IsMadeContained)
                {
                    SetRegOptional(op1);
                }
            }
        }
    }
    else if (op1Type == op2Type)
    {
        if (op2->isMemoryOp())
        {
            MakeSrcContained(tree, op2);
        }
        else if (op1->isMemoryOp() && IsSafeToContainMem(tree, op1))
        {
            MakeSrcContained(tree, op1);
        }
        else
        {
            // One of op1 or op2 could be marked as reg optional
            // to indicate that codgen can still generate code
            // if one of them is on stack.
            SetRegOptional(PreferredRegOptionalOperand(tree));
        }

        if (varTypeIsSmall(op1Type) && varTypeIsUnsigned(op1Type))
        {
            // Mark the tree as doing unsigned comparison if
            // both the operands are small and unsigned types.
            // Otherwise we will end up performing a signed comparison
            // of two small unsigned values without zero extending them to
            // TYP_INT size and which is incorrect.
            tree->gtFlags |= GTF_UNSIGNED;
        }
    }
}

/* Lower GT_CAST(srcType, DstType) nodes.
 *
 * Casts from small int type to float/double are transformed as follows:
 * GT_CAST(byte, float/double)     =   GT_CAST(GT_CAST(byte, int32), float/double)
 * GT_CAST(sbyte, float/double)    =   GT_CAST(GT_CAST(sbyte, int32), float/double)
 * GT_CAST(int16, float/double)    =   GT_CAST(GT_CAST(int16, int32), float/double)
 * GT_CAST(uint16, float/double)   =   GT_CAST(GT_CAST(uint16, int32), float/double)
 *
 * SSE2 conversion instructions operate on signed integers. casts from Uint32/Uint64
 * are morphed as follows by front-end and hence should not be seen here.
 * GT_CAST(uint32, float/double)   =   GT_CAST(GT_CAST(uint32, long), float/double)
 * GT_CAST(uint64, float)          =   GT_CAST(GT_CAST(uint64, double), float)
 *
 *
 * Similarly casts from float/double to a smaller int type are transformed as follows:
 * GT_CAST(float/double, byte)     =   GT_CAST(GT_CAST(float/double, int32), byte)
 * GT_CAST(float/double, sbyte)    =   GT_CAST(GT_CAST(float/double, int32), sbyte)
 * GT_CAST(float/double, int16)    =   GT_CAST(GT_CAST(double/double, int32), int16)
 * GT_CAST(float/double, uint16)   =   GT_CAST(GT_CAST(double/double, int32), uint16)
 *
 * SSE2 has instructions to convert a float/double vlaue into a signed 32/64-bit
 * integer.  The above transformations help us to leverage those instructions.
 *
 * Note that for the following conversions we still depend on helper calls and
 * don't expect to see them here.
 *  i) GT_CAST(float/double, uint64)
 * ii) GT_CAST(float/double, int type with overflow detection)
 *
 * TODO-XArch-CQ: (Low-pri): Jit64 generates in-line code of 8 instructions for (i) above.
 * There are hardly any occurrences of this conversion operation in platform
 * assemblies or in CQ perf benchmarks (1 occurrence in mscorlib, microsoft.jscript,
 * 1 occurence in Roslyn and no occurrences in system, system.core, system.numerics
 * system.windows.forms, scimark, fractals, bio mums). If we ever find evidence that
 * doing this optimization is a win, should consider generating in-lined code.
 */
void Lowering::LowerCast(GenTree* tree)
{
    assert(tree->OperGet() == GT_CAST);

    GenTreePtr op1     = tree->gtOp.gtOp1;
    var_types  dstType = tree->CastToType();
    var_types  srcType = op1->TypeGet();
    var_types  tmpType = TYP_UNDEF;
    bool       srcUns  = false;

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (tree->gtFlags & GTF_UNSIGNED)
    {
        srcType = genUnsignedType(srcType);
    }

    // We should never see the following casts as they are expected to be lowered
    // apropriately or converted into helper calls by front-end.
    //   srcType = float/double                    dstType = * and overflow detecting cast
    //       Reason: must be converted to a helper call
    //   srcType = float/double,                   dstType = ulong
    //       Reason: must be converted to a helper call
    //   srcType = uint                            dstType = float/double
    //       Reason: uint -> float/double = uint -> long -> float/double
    //   srcType = ulong                           dstType = float
    //       Reason: ulong -> float = ulong -> double -> float
    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
        noway_assert(dstType != TYP_ULONG);
    }
    else if (srcType == TYP_UINT)
    {
        noway_assert(!varTypeIsFloating(dstType));
    }
    else if (srcType == TYP_ULONG)
    {
        noway_assert(dstType != TYP_FLOAT);
    }

    // Case of src is a small type and dst is a floating point type.
    if (varTypeIsSmall(srcType) && varTypeIsFloating(dstType))
    {
        // These conversions can never be overflow detecting ones.
        noway_assert(!tree->gtOverflow());
        tmpType = TYP_INT;
    }
    // case of src is a floating point type and dst is a small type.
    else if (varTypeIsFloating(srcType) && varTypeIsSmall(dstType))
    {
        tmpType = TYP_INT;
    }

    if (tmpType != TYP_UNDEF)
    {
        GenTreePtr tmp = comp->gtNewCastNode(tmpType, op1, tmpType);
        tmp->gtFlags |= (tree->gtFlags & (GTF_UNSIGNED | GTF_OVERFLOW | GTF_EXCEPT));

        tree->gtFlags &= ~GTF_UNSIGNED;
        tree->gtOp.gtOp1 = tmp;
        BlockRange().InsertAfter(op1, tmp);
    }
}

//----------------------------------------------------------------------------------------------
// Returns true if this tree is bin-op of a GT_STOREIND of the following form
//      storeInd(subTreeA, binOp(gtInd(subTreeA), subtreeB)) or
//      storeInd(subTreeA, binOp(subtreeB, gtInd(subTreeA)) in case of commutative bin-ops
//
// The above form for storeInd represents a read-modify-write memory binary operation.
//
// Parameters
//     tree   -   GentreePtr of binOp
//
// Return Value
//     True if 'tree' is part of a RMW memory operation pattern
//
bool Lowering::IsBinOpInRMWStoreInd(GenTreePtr tree)
{
    // Must be a non floating-point type binary operator since SSE2 doesn't support RMW memory ops
    assert(!varTypeIsFloating(tree));
    assert(GenTree::OperIsBinary(tree->OperGet()));

    // Cheap bail out check before more expensive checks are performed.
    // RMW memory op pattern requires that one of the operands of binOp to be GT_IND.
    if (tree->gtGetOp1()->OperGet() != GT_IND && tree->gtGetOp2()->OperGet() != GT_IND)
    {
        return false;
    }

    LIR::Use use;
    if (!BlockRange().TryGetUse(tree, &use) || use.User()->OperGet() != GT_STOREIND || use.User()->gtGetOp2() != tree)
    {
        return false;
    }

    // Since it is not relatively cheap to recognize RMW memory op pattern, we
    // cache the result in GT_STOREIND node so that while lowering GT_STOREIND
    // we can use the result.
    GenTreePtr indirCandidate = nullptr;
    GenTreePtr indirOpSource  = nullptr;
    return IsRMWMemOpRootedAtStoreInd(use.User(), &indirCandidate, &indirOpSource);
}

//----------------------------------------------------------------------------------------------
// This method recognizes the case where we have a treeNode with the following structure:
//         storeInd(IndirDst, binOp(gtInd(IndirDst), indirOpSource)) OR
//         storeInd(IndirDst, binOp(indirOpSource, gtInd(IndirDst)) in case of commutative operations OR
//         storeInd(IndirDst, unaryOp(gtInd(IndirDst)) in case of unary operations
//
// Terminology:
//         indirDst = memory write of an addr mode  (i.e. storeind destination)
//         indirSrc = value being written to memory (i.e. storeind source which could either be a binary or unary op)
//         indirCandidate = memory read i.e. a gtInd of an addr mode
//         indirOpSource = source operand used in binary/unary op (i.e. source operand of indirSrc node)
//
// In x86/x64 this storeInd pattern can be effectively encoded in a single instruction of the
// following form in case of integer operations:
//         binOp [addressing mode], RegIndirOpSource
//         binOp [addressing mode], immediateVal
// where RegIndirOpSource is the register where indirOpSource was computed.
//
// Right now, we recognize few cases:
//     a) The gtInd child is a lea/lclVar/lclVarAddr/clsVarAddr/constant
//     b) BinOp is either add, sub, xor, or, and, shl, rsh, rsz.
//     c) unaryOp is either not/neg
//
// Implementation Note: The following routines need to be in sync for RMW memory op optimization
// to be correct and functional.
//     IndirsAreEquivalent()
//     NodesAreEquivalentLeaves()
//     Codegen of GT_STOREIND and genCodeForShiftRMW()
//     emitInsRMW()
//
//  TODO-CQ: Enable support for more complex indirections (if needed) or use the value numbering
//  package to perform more complex tree recognition.
//
//  TODO-XArch-CQ: Add support for RMW of lcl fields (e.g. lclfield binop= source)
//
//  Parameters:
//     tree               -  GT_STOREIND node
//     outIndirCandidate  -  out param set to indirCandidate as described above
//     ouutIndirOpSource  -  out param set to indirOpSource as described above
//
//  Return value
//     True if there is a RMW memory operation rooted at a GT_STOREIND tree
//     and out params indirCandidate and indirOpSource are set to non-null values.
//     Otherwise, returns false with indirCandidate and indirOpSource set to null.
//     Also updates flags of GT_STOREIND tree with its RMW status.
//
bool Lowering::IsRMWMemOpRootedAtStoreInd(GenTreePtr tree, GenTreePtr* outIndirCandidate, GenTreePtr* outIndirOpSource)
{
    assert(!varTypeIsFloating(tree));
    assert(outIndirCandidate != nullptr);
    assert(outIndirOpSource != nullptr);

    *outIndirCandidate = nullptr;
    *outIndirOpSource  = nullptr;

    // Early out if storeInd is already known to be a non-RMW memory op
    GenTreeStoreInd* storeInd = tree->AsStoreInd();
    if (storeInd->IsNonRMWMemoryOp())
    {
        return false;
    }

    GenTreePtr indirDst = storeInd->gtGetOp1();
    GenTreePtr indirSrc = storeInd->gtGetOp2();
    genTreeOps oper     = indirSrc->OperGet();

    // Early out if it is already known to be a RMW memory op
    if (storeInd->IsRMWMemoryOp())
    {
        if (GenTree::OperIsBinary(oper))
        {
            if (storeInd->IsRMWDstOp1())
            {
                *outIndirCandidate = indirSrc->gtGetOp1();
                *outIndirOpSource  = indirSrc->gtGetOp2();
            }
            else
            {
                assert(storeInd->IsRMWDstOp2());
                *outIndirCandidate = indirSrc->gtGetOp2();
                *outIndirOpSource  = indirSrc->gtGetOp1();
            }
            assert(IndirsAreEquivalent(*outIndirCandidate, storeInd));
        }
        else
        {
            assert(GenTree::OperIsUnary(oper));
            assert(IndirsAreEquivalent(indirSrc->gtGetOp1(), storeInd));
            *outIndirCandidate = indirSrc->gtGetOp1();
            *outIndirOpSource  = indirSrc->gtGetOp1();
        }

        return true;
    }

    // If reached here means that we do not know RMW status of tree rooted at storeInd
    assert(storeInd->IsRMWStatusUnknown());

    // Early out if indirDst is not one of the supported memory operands.
    if (indirDst->OperGet() != GT_LEA && indirDst->OperGet() != GT_LCL_VAR && indirDst->OperGet() != GT_LCL_VAR_ADDR &&
        indirDst->OperGet() != GT_CLS_VAR_ADDR && indirDst->OperGet() != GT_CNS_INT)
    {
        storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
        return false;
    }

    // We can not use Read-Modify-Write instruction forms with overflow checking instructions
    // because we are not allowed to modify the target until after the overflow check.
    if (indirSrc->gtOverflowEx())
    {
        storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
        return false;
    }

    if (GenTree::OperIsBinary(oper))
    {
        // Return if binary op is not one of the supported operations for RMW of memory.
        if (oper != GT_ADD && oper != GT_SUB && oper != GT_AND && oper != GT_OR && oper != GT_XOR &&
            !GenTree::OperIsShiftOrRotate(oper))
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
            return false;
        }

        if (GenTree::OperIsShiftOrRotate(oper) && varTypeIsSmall(storeInd))
        {
            // In ldind, Integer values smaller than 4 bytes, a boolean, or a character converted to 4 bytes
            // by sign or zero-extension as appropriate. If we directly shift the short type data using sar, we
            // will lose the sign or zero-extension bits.
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_TYPE);
            return false;
        }

        GenTreePtr rhsLeft  = indirSrc->gtGetOp1();
        GenTreePtr rhsRight = indirSrc->gtGetOp2();

        // The most common case is rhsRight is GT_IND
        if (GenTree::OperIsCommutative(oper) && rhsRight->OperGet() == GT_IND &&
            rhsRight->gtGetOp1()->OperGet() == indirDst->OperGet() && IndirsAreEquivalent(rhsRight, storeInd))
        {
            *outIndirCandidate = rhsRight;
            *outIndirOpSource  = rhsLeft;
            storeInd->SetRMWStatus(STOREIND_RMW_DST_IS_OP2);
            return true;
        }
        else if (rhsLeft->OperGet() == GT_IND && rhsLeft->gtGetOp1()->OperGet() == indirDst->OperGet() &&
                 IsSafeToContainMem(indirSrc, rhsLeft) && IndirsAreEquivalent(rhsLeft, storeInd))
        {
            *outIndirCandidate = rhsLeft;
            *outIndirOpSource  = rhsRight;
            storeInd->SetRMWStatus(STOREIND_RMW_DST_IS_OP1);
            return true;
        }

        storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
        return false;
    }
    else if (GenTree::OperIsUnary(oper))
    {
        // Nodes other than GT_NOT and GT_NEG are not yet supported.
        if (oper != GT_NOT && oper != GT_NEG)
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
            return false;
        }

        if (indirSrc->gtGetOp1()->OperGet() != GT_IND)
        {
            storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_ADDR);
            return false;
        }

        GenTreePtr indirCandidate = indirSrc->gtGetOp1();
        if (indirCandidate->gtGetOp1()->OperGet() == indirDst->OperGet() &&
            IndirsAreEquivalent(indirCandidate, storeInd))
        {
            // src and dest are the same in case of unary ops
            *outIndirCandidate = indirCandidate;
            *outIndirOpSource  = indirCandidate;
            storeInd->SetRMWStatus(STOREIND_RMW_DST_IS_OP1);
            return true;
        }
    }

    assert(*outIndirCandidate == nullptr);
    assert(*outIndirOpSource == nullptr);
    storeInd->SetRMWStatus(STOREIND_RMW_UNSUPPORTED_OPER);
    return false;
}

//--------------------------------------------------------------------------------------------
// SetStoreIndOpCountsIfRMWMemOp checks to see if there is a RMW memory operation rooted at
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
bool Lowering::SetStoreIndOpCountsIfRMWMemOp(GenTreePtr storeInd)
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
    //      set storeInd src count to that of the dst count of indirOpSource
    //      clear operand counts on indirSrc  (i.e. marked as contained and storeInd will generate code for it)
    //      clear operand counts on indirCandidate
    //      clear operand counts on indirDst except when it is a GT_LCL_VAR or GT_CNS_INT that doesn't fit within addr
    //      base
    //      Increment src count of storeInd to account for the registers required to form indirDst addr mode
    //      clear operand counts on indirCandidateChild

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

    return true;
}

/**
 * Takes care of annotating the src and dst register
 * requirements for a GT_MUL treenode.
 */
void Lowering::SetMulOpCounts(GenTreePtr tree)
{
    assert(tree->OperGet() == GT_MUL || tree->OperGet() == GT_MULHI);

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
    assert((tree->gtFlags & GTF_MUL_64RSLT) == 0);

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
    else if (tree->gtOper == GT_MULHI)
    {
        // have to use the encoding:RDX:RAX = RAX * rm
        info->setDstCandidates(m_lsra, RBM_RAX);
        hasImpliedFirstOperand = true;
    }
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
// isRMWRegOper: Can this binary tree node be used in a Read-Modify-Write format
//
// Arguments:
//    tree      - a binary tree node
//
// Return Value:
//    Returns true if we can use the read-modify-write instruction form
//
// Notes:
//    This is used to determine whether to preference the source to the destination register.
//
bool Lowering::isRMWRegOper(GenTreePtr tree)
{
    // TODO-XArch-CQ: Make this more accurate.
    // For now, We assume that most binary operators are of the RMW form.
    assert(tree->OperIsBinary());

    if (tree->OperIsCompare())
    {
        return false;
    }

    // These Opers either support a three op form (i.e. GT_LEA), or do not read/write their first operand
    if ((tree->OperGet() == GT_LEA) || (tree->OperGet() == GT_STOREIND) || (tree->OperGet() == GT_ARR_INDEX))
    {
        return false;
    }

    // x86/x64 does support a three op multiply when op2|op1 is a contained immediate
    if ((tree->OperGet() == GT_MUL) &&
        (Lowering::IsContainableImmed(tree, tree->gtOp.gtOp2) || Lowering::IsContainableImmed(tree, tree->gtOp.gtOp1)))
    {
        return false;
    }

    // otherwise we return true.
    return true;
}

// anything is in range for AMD64
bool Lowering::IsCallTargetInRange(void* addr)
{
    return true;
}

// return true if the immediate can be folded into an instruction, for example small enough and non-relocatable
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (!childNode->IsIntCnsFitsInI32())
    {
        return false;
    }

    // At this point we know that it is an int const fits within 4-bytes and hence can safely cast to IntConCommon.
    // Icons that need relocation should never be marked as contained immed
    if (childNode->AsIntConCommon()->ImmedValNeedsReloc(comp))
    {
        return false;
    }

    return true;
}

//-----------------------------------------------------------------------
// PreferredRegOptionalOperand: returns one of the operands of given
// binary oper that is to be preferred for marking as reg optional.
//
// Since only one of op1 or op2 can be a memory operand on xarch, only
// one of  them have to be marked as reg optional.  Since Lower doesn't
// know apriori which of op1 or op2 is not likely to get a register, it
// has to make a guess. This routine encapsulates heuristics that
// guess whether it is likely to be beneficial to mark op1 or op2 as
// reg optional.
//
//
// Arguments:
//     tree  -  a binary-op tree node that is either commutative
//              or a compare oper.
//
// Returns:
//     Returns op1 or op2 of tree node that is preferred for
//     marking as reg optional.
//
// Note: if the tree oper is neither commutative nor a compare oper
// then only op2 can be reg optional on xarch and hence no need to
// call this routine.
GenTree* Lowering::PreferredRegOptionalOperand(GenTree* tree)
{
    assert(GenTree::OperIsBinary(tree->OperGet()));
    assert(tree->OperIsCommutative() || tree->OperIsCompare());

    GenTree* op1         = tree->gtGetOp1();
    GenTree* op2         = tree->gtGetOp2();
    GenTree* preferredOp = nullptr;

    // This routine uses the following heuristics:
    //
    // a) If both are tracked locals, marking the one with lower weighted
    // ref count as reg-optional would likely be beneficial as it has
    // higher probability of not getting a register.
    //
    // b) op1 = tracked local and op2 = untracked local: LSRA creates two
    // ref positions for op2: a def and use position. op2's def position
    // requires a reg and it is allocated a reg by spilling another
    // interval (if required) and that could be even op1.  For this reason
    // it is beneficial to mark op1 as reg optional.
    //
    // TODO: It is not always mandatory for a def position of an untracked
    // local to be allocated a register if it is on rhs of an assignment
    // and its use position is reg-optional and has not been assigned a
    // register.  Reg optional def positions is currently not yet supported.
    //
    // c) op1 = untracked local and op2 = tracked local: marking op1 as
    // reg optional is beneficial, since its use position is less likely
    // to get a register.
    //
    // d) If both are untracked locals (i.e. treated like tree temps by
    // LSRA): though either of them could be marked as reg optional,
    // marking op1 as reg optional is likely to be beneficial because
    // while allocating op2's def position, there is a possibility of
    // spilling op1's def and in which case op1 is treated as contained
    // memory operand rather than requiring to reload.
    //
    // e) If only one of them is a local var, prefer to mark it as
    // reg-optional.  This is heuristic is based on the results
    // obtained against CQ perf benchmarks.
    //
    // f) If neither of them are local vars (i.e. tree temps), prefer to
    // mark op1 as reg optional for the same reason as mentioned in (d) above.
    if (op1->OperGet() == GT_LCL_VAR && op2->OperGet() == GT_LCL_VAR)
    {
        LclVarDsc* v1 = comp->lvaTable + op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* v2 = comp->lvaTable + op2->AsLclVarCommon()->GetLclNum();

        if (v1->lvTracked && v2->lvTracked)
        {
            // Both are tracked locals.  The one with lower weight is less likely
            // to get a register and hence beneficial to mark the one with lower
            // weight as reg optional.
            if (v1->lvRefCntWtd < v2->lvRefCntWtd)
            {
                preferredOp = op1;
            }
            else
            {
                preferredOp = op2;
            }
        }
        else if (v2->lvTracked)
        {
            // v1 is an untracked lcl and it is use position is less likely to
            // get a register.
            preferredOp = op1;
        }
        else if (v1->lvTracked)
        {
            // v2 is an untracked lcl and its def position always
            // needs a reg.  Hence it is better to mark v1 as
            // reg optional.
            preferredOp = op1;
        }
        else
        {
            preferredOp = op1;
            ;
        }
    }
    else if (op1->OperGet() == GT_LCL_VAR)
    {
        preferredOp = op1;
    }
    else if (op2->OperGet() == GT_LCL_VAR)
    {
        preferredOp = op2;
    }
    else
    {
        // Neither of the operands is a local, prefer marking
        // operand that is evaluated first as reg optional
        // since its use position is less likely to get a register.
        bool reverseOps = ((tree->gtFlags & GTF_REVERSE_OPS) != 0);
        preferredOp     = reverseOps ? op2 : op1;
    }

    return preferredOp;
}

#endif // _TARGET_XARCH_

#endif // !LEGACY_BACKEND
