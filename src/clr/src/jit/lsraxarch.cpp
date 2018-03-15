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
// BuildNode: Set register requirements for a node
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
void LinearScan::BuildNode(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    assert(!tree->isContained());

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

    // floating type generates AVX instruction (vmovss etc.), set the flag
    SetContainsAVXFlags(varTypeIsFloating(tree->TypeGet()));
    switch (tree->OperGet())
    {
        default:
            BuildSimple(tree);
            break;

        case GT_LCL_VAR:
            // Because we do containment analysis before we redo dataflow and identify register
            // candidates, the containment analysis only !lvDoNotEnregister to estimate register
            // candidates.
            // If there is a lclVar that is estimated to be register candidate but
            // is not, if they were marked regOptional they should now be marked contained instead.
            // TODO-XArch-CQ: When this is being called while RefPositions are being created,
            // use lvLRACandidate here instead.
            if (tree->IsRegOptional())
            {
                if (!compiler->lvaTable[tree->AsLclVarCommon()->gtLclNum].lvTracked ||
                    compiler->lvaTable[tree->AsLclVarCommon()->gtLclNum].lvDoNotEnregister)
                {
                    tree->ClearRegOptional();
                    tree->SetContained();
                    info->dstCount = 0;
                    return;
                }
            }
            __fallthrough;

        case GT_LCL_FLD:
            info->srcCount = 0;

#ifdef FEATURE_SIMD
            // Need an additional register to read upper 4 bytes of Vector3.
            if (tree->TypeGet() == TYP_SIMD12)
            {
                // We need an internal register different from targetReg in which 'tree' produces its result
                // because both targetReg and internal reg will be in use at the same time.
                info->internalFloatCount     = 1;
                info->isInternalRegDelayFree = true;
                info->setInternalCandidates(this, allSIMDRegs());
            }
#endif
            break;

        case GT_STORE_LCL_FLD:
        case GT_STORE_LCL_VAR:
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
            break;

#if !defined(_TARGET_64BIT_)

        case GT_LONG:
            assert(tree->IsUnusedValue()); // Contained nodes are already processed, only unused GT_LONG can reach here.
            // An unused GT_LONG node needs to consume its sources, but need not produce a register.
            tree->gtType = TYP_VOID;
            tree->ClearUnusedValue();
            info->isLocalDefUse = false;
            info->srcCount      = 2;
            info->dstCount      = 0;
            appendLocationInfoToList(tree->gtGetOp1());
            appendLocationInfoToList(tree->gtGetOp2());
            break;

#endif // !defined(_TARGET_64BIT_)

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
            assert(info->dstCount == 0);
            if (tree->TypeGet() == TYP_VOID)
            {
                info->srcCount = 0;
            }
            else
            {
                assert(tree->TypeGet() == TYP_INT);

                info->srcCount = 1;

                info->setSrcCandidates(this, RBM_INTRET);
                LocationInfoListNode* locationInfo = getLocationInfo(tree->gtOp.gtOp1);
                locationInfo->info.setSrcCandidates(this, RBM_INTRET);
                useList.Append(locationInfo);
            }
            break;

        // A GT_NOP is either a passthrough (if it is void, or if it has
        // a child), but must be considered to produce a dummy value if it
        // has a type but no child
        case GT_NOP:
            info->srcCount = 0;
            assert((tree->gtOp.gtOp1 == nullptr) || tree->isContained());
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
        {
            info->srcCount = 0;
            assert(info->dstCount == 0);
            GenTree* cmp = tree->gtGetOp1();
            assert(!cmp->IsValue());
        }
        break;

        case GT_JCC:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;

        case GT_SETCC:
            info->srcCount = 0;
            assert(info->dstCount == 1);
#ifdef _TARGET_X86_
            info->setDstCandidates(this, RBM_BYTE_REGS);
#endif // _TARGET_X86_
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
            info->internalIntCount = 1;
            assert(info->dstCount == 0);
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->srcCount == 2);
            break;

        case GT_ASG:
            noway_assert(!"We should never hit any assignment operator in lowering");
            info->srcCount = 0;
            break;

#if !defined(_TARGET_64BIT_)
        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
#endif
        case GT_ADD:
        case GT_SUB:
        case GT_AND:
        case GT_OR:
        case GT_XOR:
        case GT_BT:
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            break;

        case GT_RETURNTRAP:
            // This just turns into a compare of its child with an int + a conditional call.
            info->srcCount = GetOperandInfo(tree->gtOp.gtOp1);
            assert(info->dstCount == 0);
            info->internalIntCount = 1;
            info->setInternalCandidates(this, allRegs(TYP_INT));
            break;

        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:
            BuildModDiv(tree->AsOp());
            break;

        case GT_MUL:
        case GT_MULHI:
#if defined(_TARGET_X86_) && !defined(LEGACY_BACKEND)
        case GT_MUL_LONG:
#endif
            BuildMul(tree->AsOp());
            break;

        case GT_INTRINSIC:
            BuildIntrinsic(tree->AsOp());
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
            BuildCast(tree);
            break;

        case GT_BITCAST:
        {
            LocationInfoListNode* locationInfo = getLocationInfo(tree->gtOp.gtOp1);
            locationInfo->info.isTgtPref       = true;
            useList.Append(locationInfo);
            info->srcCount = 1;
            info->dstCount = 1;
        }
        break;

        case GT_NEG:
            info->srcCount = GetOperandInfo(tree->gtOp.gtOp1);

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
                info->setInternalCandidates(this, internalFloatRegCandidates());
            }
            break;

        case GT_NOT:
            info->srcCount = GetOperandInfo(tree->gtOp.gtOp1);
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
            (void)BuildShiftRotate(tree);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_TEST_EQ:
        case GT_TEST_NE:
        case GT_CMP:
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
            info->srcCount = 3;
            assert(info->dstCount == 1);

            // comparand is preferenced to RAX.
            // Remaining two operands can be in any reg other than RAX.
            LocationInfoListNode* locationInfo = getLocationInfo(tree->gtCmpXchg.gtOpLocation);
            locationInfo->info.setSrcCandidates(this, allRegs(TYP_INT) & ~RBM_RAX);
            useList.Append(locationInfo);
            LocationInfoListNode* valueInfo = getLocationInfo(tree->gtCmpXchg.gtOpValue);
            valueInfo->info.setSrcCandidates(this, allRegs(TYP_INT) & ~RBM_RAX);
            useList.Append(valueInfo);
            info->setDstCandidates(this, RBM_RAX);
            LocationInfoListNode* comparandInfo = getLocationInfo(tree->gtCmpXchg.gtOpComparand);
            comparandInfo->info.setSrcCandidates(this, RBM_RAX);
            useList.Append(comparandInfo);
        }
        break;

        case GT_LOCKADD:
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
            assert(info->dstCount == (tree->TypeGet() == TYP_VOID) ? 0 : 1);
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

#if !defined(FEATURE_PUT_STRUCT_ARG_STK)
        case GT_OBJ:
#endif
        case GT_BLK:
        case GT_DYN_BLK:
            // These should all be eliminated prior to Lowering.
            assert(!"Non-store block node in Lowering");
            info->srcCount = 0;
            break;

#ifdef FEATURE_PUT_STRUCT_ARG_STK
        case GT_PUTARG_STK:
            BuildPutArgStk(tree->AsPutArgStk());
            break;
#endif // FEATURE_PUT_STRUCT_ARG_STK

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
            BuildLclHeap(tree);
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            // Consumes arrLen & index - has no result
            info->srcCount = 2;
            assert(info->dstCount == 0);
            info->srcCount = GetOperandInfo(tree->AsBoundsChk()->gtIndex);
            info->srcCount += GetOperandInfo(tree->AsBoundsChk()->gtArrLen);
            break;

        case GT_ARR_ELEM:
            // These must have been lowered to GT_ARR_INDEX
            noway_assert(!"We should never see a GT_ARR_ELEM after Lowering.");
            info->srcCount = 0;
            break;

        case GT_ARR_INDEX:
        {
            info->srcCount = 2;
            assert(info->dstCount == 1);
            assert(!tree->AsArrIndex()->ArrObj()->isContained());
            assert(!tree->AsArrIndex()->IndexExpr()->isContained());
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
            assert(info->dstCount == 1);
            if (tree->gtArrOffs.gtOffset->isContained())
            {
                info->srcCount = 2;
            }
            else
            {
                // Here we simply need an internal register, which must be different
                // from any of the operand's registers, but may be the same as targetReg.
                info->srcCount         = 3;
                info->internalIntCount = 1;
                appendLocationInfoToList(tree->AsArrOffs()->gtOffset);
            }
            appendLocationInfoToList(tree->AsArrOffs()->gtIndex);
            appendLocationInfoToList(tree->AsArrOffs()->gtArrObj);
            break;

        case GT_LEA:
            // The LEA usually passes its operands through to the GT_IND, in which case it will
            // be contained, but we may be instantiating an address, in which case we set them here.
            info->srcCount = 0;
            assert(info->dstCount == 1);
            if (tree->AsAddrMode()->HasBase())
            {
                info->srcCount++;
                appendLocationInfoToList(tree->AsAddrMode()->Base());
            }
            if (tree->AsAddrMode()->HasIndex())
            {
                info->srcCount++;
                appendLocationInfoToList(tree->AsAddrMode()->Index());
            }
            break;

        case GT_STOREIND:
            if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
            {
                BuildGCWriteBarrier(tree);
                break;
            }
            BuildIndir(tree->AsIndir());
            break;

        case GT_NULLCHECK:
            assert(info->dstCount == 0);
            appendLocationInfoToList(tree->gtOp.gtOp1);
            info->srcCount = 1;
            break;

        case GT_IND:
            BuildIndir(tree->AsIndir());
            assert(info->dstCount == 1);
            break;

        case GT_CATCH_ARG:
            info->srcCount = 0;
            assert(info->dstCount == 1);
            info->setDstCandidates(this, RBM_EXCEPTION_OBJECT);
            break;

#if !FEATURE_EH_FUNCLETS
        case GT_END_LFIN:
            info->srcCount = 0;
            assert(info->dstCount == 0);
            break;
#endif

        case GT_CLS_VAR:
            // These nodes are eliminated by rationalizer.
            JITDUMP("Unexpected node %s in Lower.\n", GenTree::OpName(tree->OperGet()));
            unreached();
            break;

        case GT_INDEX_ADDR:
            assert(info->dstCount == 1);
            info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());

            if (tree->AsIndexAddr()->Index()->TypeGet() == TYP_I_IMPL)
            {
                info->internalIntCount = 1;
            }
            else
            {
                switch (tree->AsIndexAddr()->gtElemSize)
                {
                    case 1:
                    case 2:
                    case 4:
                    case 8:
                        break;

                    default:
                        info->internalIntCount = 1;
                        break;
                }
            }
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
            if (tree->OperIsCommutative() && op1->isContained() && op2 != nullptr)
            {
                op1 = op2;
                op2 = tree->gtOp.gtOp1;
            }

            // If we have a read-modify-write operation, we want to preference op1 to the target,
            // if it is not contained.
            if (!op1->isContained() && !op1->OperIs(GT_LIST))
            {
                useList.GetTreeNodeInfo(op1).isTgtPref = true;
            }

            // Is this a non-commutative operator, or is op2 a contained memory op?
            // In either case, we need to make op2 remain live until the op is complete, by marking
            // the source(s) associated with op2 as "delayFree" if this node defines a register.
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
                    assert(info->dstCount == 0);

                    // Give it an artificial type and mark it as an unused value.
                    // This results in a Def position created but not considered consumed by its parent node.
                    tree->gtType        = TYP_INT;
                    info->dstCount      = 1;
                    info->isLocalDefUse = true;
                    tree->SetUnusedValue();
                }
                else
                {
                    assert(info->dstCount != 0);
                }

                delayUseSrc = op1;
            }
            else if ((info->dstCount != 0) && (op2 != nullptr) &&
                     (!tree->OperIsCommutative() || (op2->isContained() && !op2->IsCnsIntOrI())))
            {
                delayUseSrc = op2;
            }
            if ((delayUseSrc != nullptr) && CheckAndSetDelayFree(delayUseSrc))
            {
                info->hasDelayFreeSrc = true;
            }
        }
    }

    BuildCheckByteable(tree);

    // We need to be sure that we've set info->srcCount and info->dstCount appropriately
    assert((info->dstCount < 2) || (tree->IsMultiRegCall() && info->dstCount == MAX_RET_REG_COUNT));
    assert(info->isLocalDefUse == (tree->IsValue() && tree->IsUnusedValue()));
    assert(!tree->IsUnusedValue() || (info->dstCount != 0));
    assert(info->dstCount == tree->GetRegisterDstCount());
}

//---------------------------------------------------------------------
// CheckAndSetDelayFree - Set isDelayFree on the given operand or its child(ren), if appropriate
//
// Arguments
//    delayUseSrc - a node that may have a delayed use
//
// Return Value:
//    True iff the node or one of its children has been marked isDelayFree
//
// Notes:
//    Only register operands should be marked isDelayFree, not contained immediates or memory.
//
bool LinearScan::CheckAndSetDelayFree(GenTree* delayUseSrc)
{
    // If delayUseSrc is an indirection and it doesn't produce a result, then we need to set "delayFree'
    // on the base & index, if any.
    // Otherwise, we set it on delayUseSrc itself.
    bool returnValue = false;
    if (delayUseSrc->isContained())
    {
        // If delayUseSrc is a non-Indir contained node (e.g. a local) there's no register use to delay.
        if (delayUseSrc->isIndir())
        {
            GenTree* base  = delayUseSrc->AsIndir()->Base();
            GenTree* index = delayUseSrc->AsIndir()->Index();
            if ((base != nullptr) && !base->isContained())
            {
                useList.GetTreeNodeInfo(base).isDelayFree = true;
                returnValue                               = true;
            }
            if (index != nullptr)
            {
                assert(!index->isContained());
                useList.GetTreeNodeInfo(index).isDelayFree = true;
                returnValue                                = true;
            }
        }
    }
    else
    {
        useList.GetTreeNodeInfo(delayUseSrc).isDelayFree = true;
        returnValue                                      = true;
    }
    return returnValue;
}

//------------------------------------------------------------------------
// BuildCheckByteable: Check the tree to see if "byte-able" registers are
// required, and set the tree node info accordingly.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildCheckByteable(GenTree* tree)
{
#ifdef _TARGET_X86_
    TreeNodeInfo* info = currentNodeInfo;
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
            regMask = info->getDstCandidates(this);
            assert(regMask != RBM_NONE);
            info->setDstCandidates(this, regMask & ~RBM_NON_BYTE_REGS);
        }

        if (tree->OperIsSimple())
        {
            GenTree* op = tree->gtOp.gtOp1;
            // We need byte registers on the operands of most simple operators that produce a byte result.
            // However, indirections are simple operators but do not require their address in a byte register.
            if ((op != nullptr) && !tree->OperIsIndir())
            {
                // No need to set src candidates on a contained child operand.
                if (!op->isContained())
                {
                    TreeNodeInfo& op1Info = useList.GetTreeNodeInfo(op);
                    regMask               = op1Info.getSrcCandidates(this);
                    assert(regMask != RBM_NONE);
                    op1Info.setSrcCandidates(this, regMask & ~RBM_NON_BYTE_REGS);
                }
            }

            if (tree->OperIsBinary() && (tree->gtOp.gtOp2 != nullptr))
            {
                op = tree->gtOp.gtOp2;
                if (!op->isContained())
                {
                    TreeNodeInfo& op2Info = useList.GetTreeNodeInfo(op);
                    regMask               = op2Info.getSrcCandidates(this);
                    assert(regMask != RBM_NONE);
                    op2Info.setSrcCandidates(this, regMask & ~RBM_NON_BYTE_REGS);
                }
            }
        }
    }
#endif //_TARGET_X86_
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
bool LinearScan::isRMWRegOper(GenTree* tree)
{
    // TODO-XArch-CQ: Make this more accurate.
    // For now, We assume that most binary operators are of the RMW form.
    assert(tree->OperIsBinary());

    if (tree->OperIsCompare() || tree->OperIs(GT_CMP))
    {
        return false;
    }

    switch (tree->OperGet())
    {
        // These Opers either support a three op form (i.e. GT_LEA), or do not read/write their first operand
        case GT_LEA:
        case GT_STOREIND:
        case GT_ARR_INDEX:
        case GT_STORE_BLK:
        case GT_STORE_OBJ:
            return false;

        // x86/x64 does support a three op multiply when op2|op1 is a contained immediate
        case GT_MUL:
            return (!tree->gtOp.gtOp2->isContainedIntOrIImmed() && !tree->gtOp.gtOp1->isContainedIntOrIImmed());

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWIntrinsic:
            return tree->isRMWHWIntrinsic(compiler);
#endif // FEATURE_HW_INTRINSICS

        default:
            return true;
    }
}

//------------------------------------------------------------------------
// BuildShiftRotate: Set the NodeInfo for a shift or rotate.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
int LinearScan::BuildShiftRotate(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    // For shift operations, we need that the number
    // of bits moved gets stored in CL in case
    // the number of bits to shift is not a constant.
    int                   srcCount    = 0;
    GenTree*              shiftBy     = tree->gtOp.gtOp2;
    GenTree*              source      = tree->gtOp.gtOp1;
    LocationInfoListNode* shiftByInfo = nullptr;
    // x64 can encode 8 bits of shift and it will use 5 or 6. (the others are masked off)
    // We will allow whatever can be encoded - hope you know what you are doing.
    if (shiftBy->isContained())
    {
        srcCount += GetOperandInfo(source);
    }
    else
    {
        srcCount++;
        shiftByInfo = getLocationInfo(shiftBy);
        shiftByInfo->info.setSrcCandidates(this, RBM_RCX);
        info->setDstCandidates(this, allRegs(TYP_INT) & ~RBM_RCX);
        LocationInfoListNode* sourceInfo;
        srcCount += GetOperandInfo(source, &sourceInfo);
        for (; sourceInfo != nullptr; sourceInfo = sourceInfo->Next())
        {
            sourceInfo->info.setSrcCandidates(this, allRegs(TYP_INT) & ~RBM_RCX);
        }
    }

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
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_X86_
    // The first operand of a GT_LSH_HI and GT_RSH_LO oper is a GT_LONG so that
    // we can have a three operand form. Increment the srcCount.
    if (tree->OperGet() == GT_LSH_HI || tree->OperGet() == GT_RSH_LO)
    {
        assert((source->OperGet() == GT_LONG) && source->isContained());

        GenTree*              sourceLo     = source->gtOp.gtOp1;
        LocationInfoListNode* sourceLoInfo = useList.Begin();
        LocationInfoListNode* sourceHiInfo = useList.GetSecond(INDEBUG(source->gtGetOp2()));

        info->hasDelayFreeSrc = true;
        if (tree->OperGet() == GT_LSH_HI)
        {
            sourceLoInfo->info.isDelayFree = true;
        }
        else
        {
            sourceHiInfo->info.isDelayFree = true;
        }
    }
#endif
    if (shiftByInfo != nullptr)
    {
        if (tree->IsReverseOp())
        {
            useList.Prepend(shiftByInfo);
        }
        else
        {
            useList.Append(shiftByInfo);
        }
    }
    if (!tree->isContained())
    {
        info->srcCount = srcCount;
    }
    return srcCount;
}

//------------------------------------------------------------------------
// BuildCall: Set the NodeInfo for a call.
//
// Arguments:
//    call      - The call node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildCall(GenTreeCall* call)
{
    TreeNodeInfo*   info              = currentNodeInfo;
    bool            hasMultiRegRetVal = false;
    ReturnTypeDesc* retTypeDesc       = nullptr;

    assert(!call->isContained());
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
            assert(info->dstCount == 1);
        }
    }
    else
    {
        assert(info->dstCount == 0);
    }

    GenTree*              ctrlExpr     = call->gtControlExpr;
    LocationInfoListNode* ctrlExprInfo = nullptr;
    if (call->gtCallType == CT_INDIRECT)
    {
        ctrlExpr = call->gtCallAddr;
    }

    // If this is a varargs call, we will clear the internal candidates in case we need
    // to reserve some integer registers for copying float args.
    // We have to do this because otherwise the default candidates are allRegs, and adding
    // the individual specific registers will have no effect.
    if (call->IsVarargs())
    {
        info->setInternalCandidates(this, RBM_NONE);
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
        info->setDstCandidates(this, RBM_PINVOKE_TCB);
    }
    else
#endif // _TARGET_X86_
        if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        info->setDstCandidates(this, retTypeDesc->GetABIReturnRegs());
    }
    else if (varTypeIsFloating(registerType))
    {
#ifdef _TARGET_X86_
        // The return value will be on the X87 stack, and we will need to move it.
        info->setDstCandidates(this, allRegs(registerType));
#else  // !_TARGET_X86_
        info->setDstCandidates(this, RBM_FLOATRET);
#endif // !_TARGET_X86_
    }
    else if (registerType == TYP_LONG)
    {
        info->setDstCandidates(this, RBM_LNGRET);
    }
    else
    {
        info->setDstCandidates(this, RBM_INTRET);
    }

    // number of args to a call =
    // callRegArgs + (callargs - placeholders, setup, etc)
    // there is an explicit thisPtr but it is redundant

    bool callHasFloatRegArgs = false;
    bool isVarArgs           = call->IsVarargs();

    // First, count reg args
    for (GenTree* list = call->gtCallLateArgs; list; list = list->MoveNext())
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
        // Note that this property is statically checked by LinearScan::CheckBlock.
        GenTree* argNode = list->Current();

        // Each register argument corresponds to one source.
        if (argNode->OperIsPutArgReg())
        {
            info->srcCount++;
            HandleFloatVarArgs(call, argNode, &callHasFloatRegArgs);
            appendLocationInfoToList(argNode);
        }
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        else if (argNode->OperGet() == GT_FIELD_LIST)
        {
            for (GenTreeFieldList* entry = argNode->AsFieldList(); entry != nullptr; entry = entry->Rest())
            {
                assert(entry->Current()->OperIsPutArgReg());
                info->srcCount++;
                HandleFloatVarArgs(call, argNode, &callHasFloatRegArgs);
                appendLocationInfoToList(entry->Current());
            }
        }
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

#ifdef DEBUG
        // In DEBUG only, check validity with respect to the arg table entry.

        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        if (curArgTabEntry->regNum == REG_STK)
        {
            // late arg that is not passed in a register
            assert(argNode->gtOper == GT_PUTARG_STK);

#ifdef FEATURE_PUT_STRUCT_ARG_STK
            // If the node is TYP_STRUCT and it is put on stack with
            // putarg_stk operation, we consume and produce no registers.
            // In this case the embedded Obj node should not produce
            // registers too since it is contained.
            // Note that if it is a SIMD type the argument will be in a register.
            if (argNode->TypeGet() == TYP_STRUCT)
            {
                assert(argNode->gtOp.gtOp1 != nullptr && argNode->gtOp.gtOp1->OperGet() == GT_OBJ);
                assert(argNode->gtOp.gtOp1->isContained());
            }
#endif // FEATURE_PUT_STRUCT_ARG_STK
            continue;
        }
#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            assert(argNode->isContained());
            assert(varTypeIsStruct(argNode) || curArgTabEntry->isStruct);

            int i = 0;
            for (GenTreeFieldList* entry = argNode->AsFieldList(); entry != nullptr; entry = entry->Rest())
            {
                const regNumber argReg = (i == 0) ? curArgTabEntry->regNum : curArgTabEntry->otherRegNum;
                assert(entry->Current()->gtRegNum == argReg);
                assert(i < 2);
                i++;
            }
        }
        else
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING
        {
            const regNumber argReg = curArgTabEntry->regNum;
            assert(argNode->gtRegNum == argReg);
        }
#endif // DEBUG
    }

    // Now, count stack args
    // Note that these need to be computed into a register, but then
    // they're just stored to the stack - so the reg doesn't
    // need to remain live until the call.  In fact, it must not
    // because the code generator doesn't actually consider it live,
    // so it can't be spilled.

    GenTree* args = call->gtCallArgs;
    while (args)
    {
        GenTree* arg = args->gtOp.gtOp1;
        if (!(arg->gtFlags & GTF_LATE_ARG) && !arg)
        {
            if (arg->IsValue() && !arg->isContained())
            {
                // argInfo->isLocalDefUse = true;
                assert(arg->IsUnusedValue());
            }
            // assert(argInfo->dstCount == 0);
        }
        args = args->gtOp.gtOp2;
    }

    // set reg requirements on call target represented as control sequence.
    if (ctrlExpr != nullptr)
    {
        LocationInfoListNode* ctrlExprInfo  = nullptr;
        int                   ctrlExprCount = GetOperandInfo(ctrlExpr);
        if (ctrlExprCount != 0)
        {
            assert(ctrlExprCount == 1);
            ctrlExprInfo = useList.Last();
            info->srcCount++;
        }

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (call->IsFastTailCall())
        {
            assert(!ctrlExpr->isContained() && ctrlExprInfo != nullptr);
            // Fast tail call - make sure that call target is always computed in RAX
            // so that epilog sequence can generate "jmp rax" to achieve fast tail call.
            ctrlExprInfo->info.setSrcCandidates(this, RBM_RAX);
        }
#ifdef _TARGET_X86_
        else if (call->IsVirtualStub() && (call->gtCallType == CT_INDIRECT))
        {
            // On x86, we need to generate a very specific pattern for indirect VSD calls:
            //
            //    3-byte nop
            //    call dword ptr [eax]
            //
            // Where EAX is also used as an argument to the stub dispatch helper. Make
            // sure that the call target address is computed into EAX in this case.
            assert(ctrlExprInfo != nullptr);
            assert(ctrlExpr->isIndir() && ctrlExpr->isContained());
            ctrlExprInfo->info.setSrcCandidates(this, RBM_VIRTUAL_STUB_TARGET);
        }
#endif // _TARGET_X86_

#if FEATURE_VARARG
        // If it is a fast tail call, it is already preferenced to use RAX.
        // Therefore, no need set src candidates on call tgt again.
        if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExprInfo != nullptr))
        {
            // Don't assign the call target to any of the argument registers because
            // we will use them to also pass floating point arguments as required
            // by Amd64 ABI.
            ctrlExprInfo->info.setSrcCandidates(this, allRegs(TYP_INT) & ~(RBM_ARG_REGS));
        }
#endif // !FEATURE_VARARG
    }
}

//------------------------------------------------------------------------
// BuildBlockStore: Set the NodeInfo for a block store.
//
// Arguments:
//    blkNode       - The block store node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildBlockStore(GenTreeBlk* blkNode)
{
    TreeNodeInfo* info    = currentNodeInfo;
    GenTree*      dstAddr = blkNode->Addr();
    unsigned      size    = blkNode->gtBlkSize;
    GenTree*      source  = blkNode->Data();

    LocationInfoListNode* dstAddrInfo = nullptr;
    LocationInfoListNode* sourceInfo  = nullptr;
    LocationInfoListNode* sizeInfo    = nullptr;

    // Sources are dest address, initVal or source.
    // We may require an additional source or temp register for the size.
    if (!dstAddr->isContained())
    {
        info->srcCount++;
        dstAddrInfo = getLocationInfo(dstAddr);
    }
    assert(info->dstCount == 0);
    info->setInternalCandidates(this, RBM_NONE);
    GenTree* srcAddrOrFill = nullptr;
    bool     isInitBlk     = blkNode->OperIsInitBlkOp();

    regMaskTP dstAddrRegMask = RBM_NONE;
    regMaskTP sourceRegMask  = RBM_NONE;
    regMaskTP blkSizeRegMask = RBM_NONE;

    if (isInitBlk)
    {
        GenTree* initVal = source;
        if (initVal->OperIsInitVal())
        {
            assert(initVal->isContained());
            initVal = initVal->gtGetOp1();
        }
        srcAddrOrFill = initVal;
        if (!initVal->isContained())
        {
            info->srcCount++;
            sourceInfo = getLocationInfo(initVal);
        }

        switch (blkNode->gtBlkOpKind)
        {
            case GenTreeBlk::BlkOpKindUnroll:
                assert(initVal->IsCnsIntOrI());
                if (size >= XMM_REGSIZE_BYTES)
                {
                    // Reserve an XMM register to fill it with a pack of 16 init value constants.
                    info->internalFloatCount = 1;
                    info->setInternalCandidates(this, internalFloatRegCandidates());
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
            assert(source->isContained());
            srcAddrOrFill = source->gtGetOp1();
            if (!srcAddrOrFill->isContained())
            {
                sourceInfo = getLocationInfo(srcAddrOrFill);
                info->srcCount++;
            }
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
                        info->internalIntCount++;
                        regMaskTP regMask = allRegs(TYP_INT);

#ifdef _TARGET_X86_
                        if ((size & 1) != 0)
                        {
                            regMask &= ~RBM_NON_BYTE_REGS;
                        }
#endif
                        info->setInternalCandidates(this, regMask);
                    }

                    if (size >= XMM_REGSIZE_BYTES)
                    {
                        // If we have a buffer larger than XMM_REGSIZE_BYTES,
                        // reserve an XMM register to use it for a
                        // series of 16-byte loads and stores.
                        info->internalFloatCount = 1;
                        info->addInternalCandidates(this, internalFloatRegCandidates());
                        // Uses XMM reg for load and store and hence check to see whether AVX instructions
                        // are used for codegen, set ContainsAVX flag
                        SetContainsAVXFlags();
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

    if (dstAddrInfo != nullptr)
    {
        if (dstAddrRegMask != RBM_NONE)
        {
            dstAddrInfo->info.setSrcCandidates(this, dstAddrRegMask);
        }
        useList.Append(dstAddrInfo);
    }
    if (sourceRegMask != RBM_NONE)
    {
        if (sourceInfo != nullptr)
        {
            sourceInfo->info.setSrcCandidates(this, sourceRegMask);
        }
        else
        {
            // This is a local source; we'll use a temp register for its address.
            info->addInternalCandidates(this, sourceRegMask);
            info->internalIntCount++;
        }
    }
    if (sourceInfo != nullptr)
    {
        useList.Add(sourceInfo, blkNode->IsReverseOp());
    }

    if (blkNode->OperIs(GT_STORE_DYN_BLK))
    {
        // The block size argument is a third argument to GT_STORE_DYN_BLK
        info->srcCount++;

        GenTree* blockSize = blkNode->AsDynBlk()->gtDynamicSize;
        sizeInfo           = getLocationInfo(blockSize);
        useList.Add(sizeInfo, blkNode->AsDynBlk()->gtEvalSizeFirst);
    }

    if (blkSizeRegMask != RBM_NONE)
    {
        if (size != 0)
        {
            // Reserve a temp register for the block size argument.
            info->addInternalCandidates(this, blkSizeRegMask);
            info->internalIntCount++;
        }
        else
        {
            // The block size argument is a third argument to GT_STORE_DYN_BLK
            assert((blkNode->gtOper == GT_STORE_DYN_BLK) && (sizeInfo != nullptr));
            info->setSrcCount(3);
            sizeInfo->info.setSrcCandidates(this, blkSizeRegMask);
        }
    }
}

#ifdef FEATURE_PUT_STRUCT_ARG_STK
//------------------------------------------------------------------------
// BuildPutArgStk: Set the NodeInfo for a GT_PUTARG_STK.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildPutArgStk(GenTreePutArgStk* putArgStk)
{
    TreeNodeInfo* info = currentNodeInfo;
    info->srcCount     = 0;
    assert(info->dstCount == 0);

    if (putArgStk->gtOp1->gtOper == GT_FIELD_LIST)
    {
        putArgStk->gtOp1->SetContained();

#ifdef _TARGET_X86_
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

#if defined(FEATURE_SIMD)
            // Note that we need to check the GT_FIELD_LIST type, not 'fieldType'. This is because the
            // GT_FIELD_LIST will be TYP_SIMD12 whereas the fieldType might be TYP_SIMD16 for lclVar, where
            // we "round up" to 16.
            if (current->gtFieldType == TYP_SIMD12)
            {
                needsSimdTemp = true;
            }
#endif // defined(FEATURE_SIMD)

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
            if (!fieldNode->isContained())
            {
                appendLocationInfoToList(fieldNode);
                info->srcCount++;
            }
        }

        if (putArgStk->gtPutArgStkKind == GenTreePutArgStk::Kind::Push)
        {
            // If any of the fields cannot be stored with an actual push, we may need a temporary
            // register to load the value before storing it to the stack location.
            info->internalIntCount = 1;
            regMaskTP regMask      = allRegs(TYP_INT);
            if (needsByteTemp)
            {
                regMask &= ~RBM_NON_BYTE_REGS;
            }
            info->setInternalCandidates(this, regMask);
        }

#if defined(FEATURE_SIMD)
        // For PutArgStk of a TYP_SIMD12, we need a SIMD temp register.
        if (needsSimdTemp)
        {
            assert(info->dstCount == 0);
            info->internalFloatCount += 1;
            info->addInternalCandidates(this, allSIMDRegs());
        }
#endif // defined(FEATURE_SIMD)

        return;
#endif // _TARGET_X86_
    }

    GenTree*  src  = putArgStk->gtOp1;
    var_types type = src->TypeGet();

#if defined(FEATURE_SIMD) && defined(_TARGET_X86_)
    // For PutArgStk of a TYP_SIMD12, we need an extra register.
    if (putArgStk->isSIMD12())
    {
        appendLocationInfoToList(putArgStk->gtOp1);
        info->srcCount           = 1;
        info->internalFloatCount = 1;
        info->setInternalCandidates(this, allSIMDRegs());
        return;
    }
#endif // defined(FEATURE_SIMD) && defined(_TARGET_X86_)

    if (type != TYP_STRUCT)
    {
        BuildSimple(putArgStk);
        return;
    }

    GenTree* dst     = putArgStk;
    GenTree* srcAddr = nullptr;

    info->srcCount = GetOperandInfo(src);

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
                regMaskTP regMask = allRegs(TYP_INT);

#ifdef _TARGET_X86_
                if ((size % 2) != 0)
                {
                    regMask &= ~RBM_NON_BYTE_REGS;
                }
#endif
                info->setInternalCandidates(this, regMask);
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
                info->addInternalCandidates(this, internalFloatRegCandidates());
                SetContainsAVXFlags();
            }
            break;

        case GenTreePutArgStk::Kind::RepInstr:
            info->internalIntCount += 3;
            info->setInternalCandidates(this, (RBM_RDI | RBM_RCX | RBM_RSI));
            break;

        default:
            unreached();
    }
}
#endif // FEATURE_PUT_STRUCT_ARG_STK

//------------------------------------------------------------------------
// BuildLclHeap: Set the NodeInfo for a GT_LCLHEAP.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildLclHeap(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    info->srcCount     = 1;
    assert(info->dstCount == 1);

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
        appendLocationInfoToList(size);
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
// BuildModDiv: Set the NodeInfo for GT_MOD/GT_DIV/GT_UMOD/GT_UDIV.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildModDiv(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    GenTree*      op1  = tree->gtGetOp1();
    GenTree*      op2  = tree->gtGetOp2();

    assert(info->dstCount == 1);

    if (varTypeIsFloating(tree->TypeGet()))
    {
        info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
        return;
    }

    // Amd64 Div/Idiv instruction:
    //    Dividend in RAX:RDX  and computes
    //    Quotient in RAX, Remainder in RDX

    if (tree->OperGet() == GT_MOD || tree->OperGet() == GT_UMOD)
    {
        // We are interested in just the remainder.
        // RAX is used as a trashable register during computation of remainder.
        info->setDstCandidates(this, RBM_RDX);
    }
    else
    {
        // We are interested in just the quotient.
        // RDX gets used as trashable register during computation of quotient
        info->setDstCandidates(this, RBM_RAX);
    }

#ifdef _TARGET_X86_
    if (op1->OperGet() == GT_LONG)
    {
        assert(op1->isContained());

        // To avoid reg move would like to have op1's low part in RAX and high part in RDX.
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();

        assert(op2->IsCnsIntOrI());
        assert(tree->OperGet() == GT_UMOD);

        // This situation also requires an internal register.
        info->internalIntCount = 1;
        info->setInternalCandidates(this, allRegs(TYP_INT));

        LocationInfoListNode* loValInfo = getLocationInfo(loVal);
        LocationInfoListNode* hiValInfo = getLocationInfo(hiVal);
        loValInfo->info.setSrcCandidates(this, RBM_EAX);
        hiValInfo->info.setSrcCandidates(this, RBM_EDX);
        useList.Append(loValInfo);
        useList.Append(hiValInfo);
        info->srcCount = 2;
    }
    else
#endif
    {
        // If possible would like to have op1 in RAX to avoid a register move
        LocationInfoListNode* op1Info = getLocationInfo(op1);
        op1Info->info.setSrcCandidates(this, RBM_RAX);
        useList.Append(op1Info);
        info->srcCount = 1;
    }

    LocationInfoListNode* op2Info;
    info->srcCount += GetOperandInfo(op2, &op2Info);
    for (; op2Info != nullptr; op2Info = op2Info->Next())
    {
        op2Info->info.setSrcCandidates(this, allRegs(TYP_INT) & ~(RBM_RAX | RBM_RDX));
    }
}

//------------------------------------------------------------------------
// BuildIntrinsic: Set the NodeInfo for a GT_INTRINSIC.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildIntrinsic(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    // Both operand and its result must be of floating point type.
    GenTree* op1 = tree->gtGetOp1();
    assert(varTypeIsFloating(op1));
    assert(op1->TypeGet() == tree->TypeGet());

    info->srcCount = GetOperandInfo(op1);
    assert(info->dstCount == 1);

    switch (tree->gtIntrinsic.gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Sqrt:
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
                info->setInternalCandidates(this, internalFloatRegCandidates());
            }
            break;

#ifdef _TARGET_X86_
        case CORINFO_INTRINSIC_Cos:
        case CORINFO_INTRINSIC_Sin:
            NYI_X86("Math intrinsics Cos and Sin");
            break;
#endif // _TARGET_X86_

        case CORINFO_INTRINSIC_Round:
        case CORINFO_INTRINSIC_Ceiling:
        case CORINFO_INTRINSIC_Floor:
#if defined(LEGACY_BACKEND)
            NYI_X86("Math intrinsics Round, Ceiling, and Floor");
#endif // LEGACY_BACKEND
            break;

        default:
            // Right now only Sqrt/Abs are treated as math intrinsics
            noway_assert(!"Unsupported math intrinsic");
            unreached();
            break;
    }
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
    // Only SIMDIntrinsicInit can be contained. Other than that,
    // only SIMDIntrinsicOpEquality and SIMDIntrinsicOpInEquality can have 0 dstCount.
    if (simdTree->isContained())
    {
        assert(simdTree->gtSIMDIntrinsicID == SIMDIntrinsicInit);
    }
    else if (info->dstCount != 1)
    {
        assert((simdTree->gtSIMDIntrinsicID == SIMDIntrinsicOpEquality) ||
               (simdTree->gtSIMDIntrinsicID == SIMDIntrinsicOpInEquality));
    }
    SetContainsAVXFlags(true, simdTree->gtSIMDSize);
    GenTree* op1   = simdTree->gtOp.gtOp1;
    GenTree* op2   = simdTree->gtOp.gtOp2;
    info->srcCount = 0;
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
        {
            // This sets all fields of a SIMD struct to the given value.
            // Mark op1 as contained if it is either zero or int constant of all 1's,
            // or a float constant with 16 or 32 byte simdType (AVX case)
            //
            // Should never see small int base type vectors except for zero initialization.
            assert(!varTypeIsSmallInt(simdTree->gtSIMDBaseType) || op1->IsIntegralConst(0));

#if !defined(_TARGET_64BIT_)
            if (op1->OperGet() == GT_LONG)
            {
                assert(op1->isContained());
                GenTree* op1lo = op1->gtGetOp1();
                GenTree* op1hi = op1->gtGetOp2();

                if (op1lo->isContained())
                {
                    assert(op1hi->isContained());
                    assert((op1lo->IsIntegralConst(0) && op1hi->IsIntegralConst(0)) ||
                           (op1lo->IsIntegralConst(-1) && op1hi->IsIntegralConst(-1)));
                    assert(info->srcCount == 0);
                }
                else
                {
                    assert(info->srcCount == 2);
                    info->internalFloatCount = 1;
                    info->setInternalCandidates(this, allSIMDRegs());
                    info->isInternalRegDelayFree = true;
                }
            }
#endif // !defined(_TARGET_64BIT_)
        }
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

            // Need an internal register to stitch together all the values into a single vector in a SIMD reg.
            info->internalFloatCount = 1;
            info->setInternalCandidates(this, allSIMDRegs());
        }
        break;

        case SIMDIntrinsicInitArray:
            // We have an array and an index, which may be contained.
            assert(info->srcCount == (simdTree->gtGetOp2()->isContained() ? 1 : 2));
            break;

        case SIMDIntrinsicDiv:
            // SSE2 has no instruction support for division on integer vectors
            noway_assert(varTypeIsFloating(simdTree->gtSIMDBaseType));
            assert(info->srcCount == 2);
            break;

        case SIMDIntrinsicAbs:
            // float/double vectors: This gets implemented as bitwise-And operation
            // with a mask and hence should never see  here.
            //
            // Must be a Vector<int> or Vector<short> Vector<sbyte>
            assert(simdTree->gtSIMDBaseType == TYP_INT || simdTree->gtSIMDBaseType == TYP_SHORT ||
                   simdTree->gtSIMDBaseType == TYP_BYTE);
            assert(compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported);
            assert(info->srcCount == 1);
            break;

        case SIMDIntrinsicSqrt:
            // SSE2 has no instruction support for sqrt on integer vectors.
            noway_assert(varTypeIsFloating(simdTree->gtSIMDBaseType));
            assert(info->srcCount == 1);
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
            assert(info->srcCount == 2);

            // SSE2 32-bit integer multiplication requires two temp regs
            if (simdTree->gtSIMDIntrinsicID == SIMDIntrinsicMul && simdTree->gtSIMDBaseType == TYP_INT &&
                compiler->getSIMDSupportLevel() == SIMD_SSE2_Supported)
            {
                info->internalFloatCount = 2;
                info->setInternalCandidates(this, allSIMDRegs());
            }
            break;

        case SIMDIntrinsicEqual:
            assert(info->srcCount == 2);
            break;

        // SSE2 doesn't support < and <= directly on int vectors.
        // Instead we need to use > and >= with swapped operands.
        case SIMDIntrinsicLessThan:
        case SIMDIntrinsicLessThanOrEqual:
            assert(info->srcCount == 2);
            noway_assert(!varTypeIsIntegral(simdTree->gtSIMDBaseType));
            break;

        // SIMDIntrinsicEqual is supported only on non-floating point base type vectors.
        // SSE2 cmpps/pd doesn't support > and >=  directly on float/double vectors.
        // Instead we need to use <  and <= with swapped operands.
        case SIMDIntrinsicGreaterThan:
            noway_assert(!varTypeIsFloating(simdTree->gtSIMDBaseType));
            assert(info->srcCount == 2);
            break;

        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            if (simdTree->gtGetOp2()->isContained())
            {
                // If the second operand is contained then ContainCheckSIMD has determined
                // that PTEST can be used. We only need a single source register and no
                // internal registers.
                assert(info->srcCount == 1);
            }
            else
            {
                // Can't use PTEST so we need 2 source registers, 1 internal SIMD register
                // (to hold the result of PCMPEQD or other similar SIMD compare instruction)
                // and one internal INT register (to hold the result of PMOVMSKB).
                assert(info->srcCount == 2);
                info->internalFloatCount = 1;
                info->setInternalCandidates(this, allSIMDRegs());
                info->internalIntCount = 1;
                info->addInternalCandidates(this, allRegs(TYP_INT));
            }
            // These SIMD nodes only set the condition flags.
            info->dstCount = 0;
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
                if ((compiler->getSIMDSupportLevel() == SIMD_SSE2_Supported) ||
                    (simdTree->gtOp.gtOp1->TypeGet() == TYP_SIMD32))
                {
                    info->internalFloatCount     = 1;
                    info->isInternalRegDelayFree = true;
                    info->setInternalCandidates(this, allSIMDRegs());
                }
                // else don't need scratch reg(s).
            }
            else
            {
                assert(simdTree->gtSIMDBaseType == TYP_INT && compiler->getSIMDSupportLevel() >= SIMD_SSE4_Supported);

                // No need to set isInternalRegDelayFree since targetReg is a
                // an int type reg and guaranteed to be different from xmm/ymm
                // regs.
                info->internalFloatCount = (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported) ? 2 : 1;
                info->setInternalCandidates(this, allSIMDRegs());
            }
            assert(info->srcCount == 2);
            break;

        case SIMDIntrinsicGetItem:
        {
            // This implements get_Item method. The sources are:
            //  - the source SIMD struct
            //  - index (which element to get)
            // The result is baseType of SIMD struct.
            // op1 may be a contained memory op, but if so we will consume its address.
            // op2 may be a contained constant.
            op1 = simdTree->gtOp.gtOp1;
            op2 = simdTree->gtOp.gtOp2;

            if (!op1->isContained())
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
                    (void)compiler->getSIMDInitTempVarNum();
                }
                else if (!varTypeIsFloating(simdTree->gtSIMDBaseType))
                {
                    bool needFloatTemp;
                    if (varTypeIsSmallInt(simdTree->gtSIMDBaseType) &&
                        (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported))
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
                        info->setInternalCandidates(this, allSIMDRegs());
                    }
                }
            }
        }
        break;

        case SIMDIntrinsicSetX:
        case SIMDIntrinsicSetY:
        case SIMDIntrinsicSetZ:
        case SIMDIntrinsicSetW:
            assert(info->srcCount == 2);

            // We need an internal integer register for SSE2 codegen
            if (compiler->getSIMDSupportLevel() == SIMD_SSE2_Supported)
            {
                info->internalIntCount = 1;
                info->setInternalCandidates(this, allRegs(TYP_INT));
            }

            break;

        case SIMDIntrinsicCast:
            assert(info->srcCount == 1);
            break;

        case SIMDIntrinsicConvertToSingle:
            assert(info->srcCount == 1);
            if (simdTree->gtSIMDBaseType == TYP_UINT)
            {
                // We need an internal register different from targetReg.
                info->isInternalRegDelayFree = true;
                info->internalIntCount       = 1;
                info->internalFloatCount     = 2;
                info->setInternalCandidates(this, allSIMDRegs() | allRegs(TYP_INT));
            }
            break;

        case SIMDIntrinsicConvertToInt32:
            assert(info->srcCount == 1);
            break;

        case SIMDIntrinsicWidenLo:
        case SIMDIntrinsicWidenHi:
            assert(info->srcCount == 1);
            if (varTypeIsIntegral(simdTree->gtSIMDBaseType))
            {
                // We need an internal register different from targetReg.
                info->isInternalRegDelayFree = true;
                info->internalFloatCount     = 1;
                info->setInternalCandidates(this, allSIMDRegs());
            }
            break;

        case SIMDIntrinsicConvertToInt64:
            assert(info->srcCount == 1);
            // We need an internal register different from targetReg.
            info->isInternalRegDelayFree = true;
            info->internalIntCount       = 1;
            if (compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported)
            {
                info->internalFloatCount = 2;
            }
            else
            {
                info->internalFloatCount = 1;
            }
            info->setInternalCandidates(this, allSIMDRegs() | allRegs(TYP_INT));
            break;

        case SIMDIntrinsicConvertToDouble:
            assert(info->srcCount == 1);
            // We need an internal register different from targetReg.
            info->isInternalRegDelayFree = true;
            info->internalIntCount       = 1;
#ifdef _TARGET_X86_
            if (simdTree->gtSIMDBaseType == TYP_LONG)
            {
                info->internalFloatCount = 3;
            }
            else
#endif
                if ((compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported) || (simdTree->gtSIMDBaseType == TYP_ULONG))
            {
                info->internalFloatCount = 2;
            }
            else
            {
                info->internalFloatCount = 1;
            }
            info->setInternalCandidates(this, allSIMDRegs() | allRegs(TYP_INT));
            break;

        case SIMDIntrinsicNarrow:
            assert(info->srcCount == 2);
            // We need an internal register different from targetReg.
            info->isInternalRegDelayFree = true;
            if ((compiler->getSIMDSupportLevel() == SIMD_AVX2_Supported) && (simdTree->gtSIMDBaseType != TYP_DOUBLE))
            {
                info->internalFloatCount = 2;
            }
            else
            {
                info->internalFloatCount = 1;
            }
            info->setInternalCandidates(this, allSIMDRegs());
            break;

        case SIMDIntrinsicShuffleSSE2:
            assert(info->srcCount == 1);
            // Second operand is an integer constant and marked as contained.
            assert(simdTree->gtOp.gtOp2->isContainedIntOrIImmed());
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

#ifdef FEATURE_HW_INTRINSICS
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
    TreeNodeInfo*       info        = currentNodeInfo;
    NamedIntrinsic      intrinsicID = intrinsicTree->gtHWIntrinsicId;
    var_types           baseType    = intrinsicTree->gtSIMDBaseType;
    InstructionSet      isa         = Compiler::isaOfHWIntrinsic(intrinsicID);
    HWIntrinsicCategory category    = Compiler::categoryOfHWIntrinsic(intrinsicID);
    HWIntrinsicFlag     flags       = Compiler::flagsOfHWIntrinsic(intrinsicID);
    int                 numArgs     = Compiler::numArgsOfHWIntrinsic(intrinsicTree);

    if (isa == InstructionSet_AVX || isa == InstructionSet_AVX2)
    {
        SetContainsAVXFlags(true, 32);
    }

    GenTree* op1   = intrinsicTree->gtOp.gtOp1;
    GenTree* op2   = intrinsicTree->gtOp.gtOp2;
    info->srcCount = 0;

    if (op1 != nullptr)
    {
        if (op1->OperIsList())
        {
            for (GenTreeArgList* list = op1->AsArgList(); list != nullptr; list = list->Rest())
            {
                info->srcCount += GetOperandInfo(list->Current());
            }
        }
        else
        {
            info->srcCount += GetOperandInfo(op1);
        }
    }

    if (op2 != nullptr)
    {
        info->srcCount += GetOperandInfo(op2);
    }

    if ((category == HW_Category_IMM) && ((flags & HW_Flag_NoJmpTableIMM) == 0))
    {
        GenTree* lastOp = Compiler::lastOpOfHWIntrinsic(intrinsicTree, numArgs);
        assert(lastOp != nullptr);
        if (Compiler::isImmHWIntrinsic(intrinsicID, lastOp) && !lastOp->isContainedIntOrIImmed())
        {
            assert(!lastOp->IsCnsIntOrI());

            // We need two extra reg when lastOp isn't a constant so
            // the offset into the jump table for the fallback path
            // can be computed.

            info->internalIntCount = 2;
            info->setInternalCandidates(this, allRegs(TYP_INT));
        }
    }

    // Check for "srcCount >= 2" to match against 3+ operand nodes where one is constant
    if ((op2 == nullptr) && (info->srcCount >= 2) && intrinsicTree->isRMWHWIntrinsic(compiler))
    {
        // TODO-XArch-CQ: This is currently done in order to handle intrinsics which have more than
        // two arguments but which still have RMW semantics (such as NI_SSE41_Insert). We should make
        // this handling more general and move it back out to LinearScan::BuildNode.

        assert(numArgs > 2);
        LocationInfoListNode* op2Info = useList.Begin()->Next();
        op2Info->info.isDelayFree     = true;
        info->hasDelayFreeSrc         = true;
    }

    switch (intrinsicID)
    {
        case NI_SSE_CompareEqualOrderedScalar:
        case NI_SSE_CompareEqualUnorderedScalar:
        case NI_SSE_CompareNotEqualOrderedScalar:
        case NI_SSE_CompareNotEqualUnorderedScalar:
        case NI_SSE2_CompareEqualOrderedScalar:
        case NI_SSE2_CompareEqualUnorderedScalar:
        case NI_SSE2_CompareNotEqualOrderedScalar:
        case NI_SSE2_CompareNotEqualUnorderedScalar:
            info->internalIntCount = 1;
            info->setInternalCandidates(this, RBM_BYTE_REGS);
            info->isInternalRegDelayFree = true;
            break;

        case NI_SSE_SetScalarVector128:
        case NI_SSE2_SetScalarVector128:
            // Need an internal register to stitch together all the values into a single vector in a SIMD reg.
            info->internalFloatCount = 1;
            info->setInternalCandidates(this, allSIMDRegs());
            info->isInternalRegDelayFree = true;
            break;

        case NI_SSE_ConvertToSingle:
        case NI_SSE_StaticCast:
        case NI_SSE2_ConvertToDouble:
        case NI_AVX_ExtendToVector256:
        case NI_AVX_GetLowerHalf:
        case NI_AVX_StaticCast:
        {
            assert(info->srcCount == 1);
            assert(info->dstCount == 1);
            useList.Last()->info.isTgtPref = true;
            break;
        }

        case NI_SSE2_MaskMove:
        {
            // SSE2 MaskMove hardcodes the destination (op3) in DI/EDI/RDI
            LocationInfoListNode* op3Info = useList.Begin()->Next()->Next();
            op3Info->info.setSrcCandidates(this, RBM_EDI);
            break;
        }

        case NI_SSE41_BlendVariable:
            if (!compiler->canUseVexEncoding())
            {
                // SSE4.1 blendv* hardcode the mask vector (op3) in XMM0
                LocationInfoListNode* op2Info = useList.Begin()->Next();
                LocationInfoListNode* op3Info = op2Info->Next();
                op2Info->info.isDelayFree     = true;
                op3Info->info.isDelayFree     = true;
                op3Info->info.setSrcCandidates(this, RBM_XMM0);
                info->hasDelayFreeSrc = true;
            }
            break;

        case NI_SSE41_TestAllOnes:
        {
            info->internalFloatCount = 1;
            info->setInternalCandidates(this, allSIMDRegs());
            break;
        }

        case NI_SSE41_Extract:
            if (baseType == TYP_FLOAT)
            {
                info->internalIntCount += 1;
            }
#ifdef _TARGET_X86_
            else if (varTypeIsByte(baseType))
            {
                info->setDstCandidates(this, RBM_BYTE_REGS);
            }
#endif
            break;

#ifdef _TARGET_X86_
        case NI_SSE42_Crc32:
        {
            // CRC32 may operate over "byte" but on x86 only RBM_BYTE_REGS can be used as byte registers.
            //
            // TODO - currently we use the BaseType to bring the type of the second argument
            // to the code generator. May encode the overload info in other way.
            var_types srcType = intrinsicTree->gtSIMDBaseType;
            if (varTypeIsByte(srcType))
            {
                LocationInfoListNode* op2Info = useList.GetSecond(INDEBUG(intrinsicTree->gtGetOp2()));
                op2Info->info.setSrcCandidates(this, RBM_BYTE_REGS);
            }
            break;
        }
#endif // _TARGET_X86_

        default:
            assert((intrinsicID > NI_HW_INTRINSIC_START) && (intrinsicID < NI_HW_INTRINSIC_END));
            break;
    }
}
#endif

//------------------------------------------------------------------------
// BuildCast: Set the NodeInfo for a GT_CAST.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildCast(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
    // TODO-XArch-CQ: Int-To-Int conversions - castOp cannot be a memory op and must have an assigned register.
    //         see CodeGen::genIntToIntCast()

    // Non-overflow casts to/from float/double are done using SSE2 instructions
    // and that allow the source operand to be either a reg or memop. Given the
    // fact that casts from small int to float/double are done as two-level casts,
    // the source operand is always guaranteed to be of size 4 or 8 bytes.
    var_types castToType = tree->CastToType();
    GenTree*  castOp     = tree->gtCast.CastOp();
    var_types castOpType = castOp->TypeGet();

    info->srcCount = GetOperandInfo(castOp);
    assert(info->dstCount == 1);
    if (tree->gtFlags & GTF_UNSIGNED)
    {
        castOpType = genUnsignedType(castOpType);
    }

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

//-----------------------------------------------------------------------------------------
// BuildIndir: Specify register requirements for address expression of an indirection operation.
//
// Arguments:
//    indirTree    -   GT_IND or GT_STOREIND gentree node
//
void LinearScan::BuildIndir(GenTreeIndir* indirTree)
{
    TreeNodeInfo* info = currentNodeInfo;
    // If this is the rhs of a block copy (i.e. non-enregisterable struct),
    // it has no register requirements.
    if (indirTree->TypeGet() == TYP_STRUCT)
    {
        return;
    }

    int indirSrcCount = GetIndirInfo(indirTree);
    if (indirTree->gtOper == GT_STOREIND)
    {
        GenTree* source = indirTree->gtOp.gtOp2;
        if (indirTree->AsStoreInd()->IsRMWMemoryOp())
        {
            // Because 'source' is contained, we haven't yet determined its special register requirements, if any.
            // As it happens, the Shift or Rotate cases are the only ones with special requirements.
            assert(source->isContained() && source->OperIsRMWMemOp());
            GenTree* nonMemSource = nullptr;

            if (source->OperIsShiftOrRotate())
            {
                info->srcCount += BuildShiftRotate(source);
            }
            else
            {
                info->srcCount += appendBinaryLocationInfoToList(source->AsOp());
            }
            if (indirTree->AsStoreInd()->IsRMWDstOp1())
            {
                if (source->OperIsBinary())
                {
                    nonMemSource = source->gtOp.gtOp2;
                }
            }
            else if (indirTree->AsStoreInd()->IsRMWDstOp2())
            {
                nonMemSource = source->gtOp.gtOp1;
            }
            if (nonMemSource != nullptr)
            {
                assert(!nonMemSource->isContained() || (!nonMemSource->isMemoryOp() && !nonMemSource->IsLocal()));
#ifdef _TARGET_X86_
                if (varTypeIsByte(indirTree) && !nonMemSource->isContained())
                {
                    // If storeInd is of TYP_BYTE, set source to byteable registers.
                    TreeNodeInfo& nonMemSourceInfo = useList.GetTreeNodeInfo(nonMemSource);
                    regMaskTP     regMask          = nonMemSourceInfo.getSrcCandidates(this);
                    regMask &= ~RBM_NON_BYTE_REGS;
                    assert(regMask != RBM_NONE);
                    nonMemSourceInfo.setSrcCandidates(this, regMask);
                }
#endif
            }
        }
        else
        {
#ifdef _TARGET_X86_
            if (varTypeIsByte(indirTree) && !source->isContained())
            {
                // If storeInd is of TYP_BYTE, set source to byteable registers.
                LocationInfoListNode* sourceInfo = getLocationInfo(source);
                regMaskTP             regMask    = sourceInfo->info.getSrcCandidates(this);
                regMask &= ~RBM_NON_BYTE_REGS;
                assert(regMask != RBM_NONE);
                sourceInfo->info.setSrcCandidates(this, regMask);
                useList.Append(sourceInfo);
                info->srcCount++;
            }
            else
#endif
            {
                info->srcCount += GetOperandInfo(source);
            }
        }
    }
    info->srcCount += indirSrcCount;

#ifdef FEATURE_SIMD
    if (indirTree->TypeGet() == TYP_SIMD12)
    {
        // If indirTree is of TYP_SIMD12, addr is not contained. See comment in LowerIndir().
        assert(!indirTree->Addr()->isContained());

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

        info->setInternalCandidates(this, allSIMDRegs());

        return;
    }
#endif // FEATURE_SIMD

    assert(indirTree->Addr()->gtOper != GT_ARR_ELEM);
}

//------------------------------------------------------------------------
// BuildMul: Set the NodeInfo for a multiply.
//
// Arguments:
//    tree      - The node of interest
//
// Return Value:
//    None.
//
void LinearScan::BuildMul(GenTree* tree)
{
    TreeNodeInfo* info = currentNodeInfo;
#if defined(_TARGET_X86_)
    assert(tree->OperIs(GT_MUL, GT_MULHI, GT_MUL_LONG));
#else
    assert(tree->OperIs(GT_MUL, GT_MULHI));
#endif
    GenTree* op1   = tree->gtOp.gtOp1;
    GenTree* op2   = tree->gtOp.gtOp2;
    info->srcCount = appendBinaryLocationInfoToList(tree->AsOp());
    assert(info->dstCount == 1);

    // Case of float/double mul.
    if (varTypeIsFloating(tree->TypeGet()))
    {
        return;
    }

    bool isUnsignedMultiply    = ((tree->gtFlags & GTF_UNSIGNED) != 0);
    bool requiresOverflowCheck = tree->gtOverflowEx();

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
        info->setDstCandidates(this, RBM_RAX);
    }
    else if (tree->OperGet() == GT_MULHI)
    {
        // Have to use the encoding:RDX:RAX = RAX * rm. Since we only care about the
        // upper 32 bits of the result set the destination candidate to REG_RDX.
        info->setDstCandidates(this, RBM_RDX);
    }
#if defined(_TARGET_X86_)
    else if (tree->OperGet() == GT_MUL_LONG)
    {
        // have to use the encoding:RDX:RAX = RAX * rm
        info->setDstCandidates(this, RBM_RAX);
    }
#endif
    GenTree* containedMemOp = nullptr;
    if (op1->isContained() && !op1->IsCnsIntOrI())
    {
        assert(!op2->isContained() || op2->IsCnsIntOrI());
        containedMemOp = op1;
    }
    else if (op2->isContained() && !op2->IsCnsIntOrI())
    {
        containedMemOp = op2;
    }
    if ((containedMemOp != nullptr) && CheckAndSetDelayFree(containedMemOp))
    {
        info->hasDelayFreeSrc = true;
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
void LinearScan::SetContainsAVXFlags(bool isFloatingPointType /* = true */, unsigned sizeOfSIMDVector /* = 0*/)
{
    if (isFloatingPointType && compiler->canUseVexEncoding())
    {
        compiler->getEmitter()->SetContainsAVX(true);
        if (sizeOfSIMDVector == 32)
        {
            compiler->getEmitter()->SetContains256bitAVX(true);
        }
    }
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
bool LinearScan::ExcludeNonByteableRegisters(GenTree* tree)
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
    else if (tree->OperIsCompare() || tree->OperIs(GT_CMP))
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
                if (!isContainableMemoryOp(op1) && op2->IsCnsIntOrI() && varTypeIsSmallInt(baseType))
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
