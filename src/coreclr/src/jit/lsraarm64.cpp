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
// TreeNodeInfoInitStoreLoc: Set register requirements for a store of a lclVar
//
// Arguments:
//    storeLoc - the local store (GT_STORE_LCL_FLD or GT_STORE_LCL_VAR)
//
// Notes:
//    This involves:
//    - Setting the appropriate candidates for a store of a multi-reg call return value.
//    - Handling of contained immediates.

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

    CheckImmedAndMakeContained(storeLoc, op1);
}

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
                info->internalIntCount = 2;
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
        {
            info->srcCount = 2;
            info->dstCount = 1;

            GenTreePtr shiftBy = tree->gtOp.gtOp2;
            GenTreePtr source  = tree->gtOp.gtOp1;
            if (shiftBy->IsCnsIntOrI())
            {
                l->clearDstCount(shiftBy);
                info->srcCount--;
            }
        }
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
            info->srcCount = 2;
            info->dstCount = 1;

            // We need one internal register when generating code for GT_ARR_INDEX, however the
            // register allocator always may just give us the same one as it gives us for the 'dst'
            // as a workaround we will just ask for two internal registers.
            //
            info->internalIntCount = 2;

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

        info->srcCount++;

        // In case of fast tail implemented as jmp, make sure that gtControlExpr is
        // computed into a register.
        if (call->IsFastTailCall())
        {
            // Fast tail call - make sure that call target is always computed in IP0
            // so that epilog sequence can generate "br xip0" to achieve fast tail call.
            ctrlExpr->gtLsraInfo.setSrcCandidates(l, genRegMask(REG_IP0));
        }
    }

    RegisterType registerType = call->TypeGet();

    // Set destination candidates for return value of the call.
    if (hasMultiRegRetVal)
    {
        assert(retTypeDesc != nullptr);
        info->setDstCandidates(l, retTypeDesc->GetABIReturnRegs());
    }
    else if (varTypeIsFloating(registerType))
    {
        info->setDstCandidates(l, RBM_FLOATRET);
    }
    else if (registerType == TYP_LONG)
    {
        info->setDstCandidates(l, RBM_LNGRET);
    }
    else
    {
        info->setDstCandidates(l, RBM_INTRET);
    }

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

    // First, count reg args
    bool callHasFloatRegArgs = false;

    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->OperIsList());

        GenTreePtr argNode = list->Current();

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        if (curArgTabEntry->regNum == REG_STK)
        {
            // late arg that is not passed in a register
            assert(argNode->gtOper == GT_PUTARG_STK);

            TreeNodeInfoInitPutArgStk(argNode->AsPutArgStk(), curArgTabEntry);
            continue;
        }

        var_types argType    = argNode->TypeGet();
        bool      argIsFloat = varTypeIsFloating(argType);
        callHasFloatRegArgs |= argIsFloat;

        regNumber argReg = curArgTabEntry->regNum;
        // We will setup argMask to the set of all registers that compose this argument
        regMaskTP argMask = 0;

        argNode = argNode->gtEffectiveVal();

        // A GT_FIELD_LIST has a TYP_VOID, but is used to represent a multireg struct
        if (varTypeIsStruct(argNode) || (argNode->gtOper == GT_FIELD_LIST))
        {
            GenTreePtr actualArgNode = argNode;
            unsigned   originalSize  = 0;

            if (argNode->gtOper == GT_FIELD_LIST)
            {
                // There could be up to 2-4 PUTARG_REGs in the list (3 or 4 can only occur for HFAs)
                GenTreeFieldList* fieldListPtr = argNode->AsFieldList();

                // Initailize the first register and the first regmask in our list
                regNumber targetReg    = argReg;
                regMaskTP targetMask   = genRegMask(targetReg);
                unsigned  iterationNum = 0;
                originalSize           = 0;

                for (; fieldListPtr; fieldListPtr = fieldListPtr->Rest())
                {
                    GenTreePtr putArgRegNode = fieldListPtr->Current();
                    assert(putArgRegNode->gtOper == GT_PUTARG_REG);
                    GenTreePtr putArgChild = putArgRegNode->gtOp.gtOp1;

                    originalSize += REGSIZE_BYTES; // 8 bytes

                    // Record the register requirements for the GT_PUTARG_REG node
                    putArgRegNode->gtLsraInfo.setDstCandidates(l, targetMask);
                    putArgRegNode->gtLsraInfo.setSrcCandidates(l, targetMask);

                    // To avoid redundant moves, request that the argument child tree be
                    // computed in the register in which the argument is passed to the call.
                    putArgChild->gtLsraInfo.setSrcCandidates(l, targetMask);

                    // We consume one source for each item in this list
                    info->srcCount++;
                    iterationNum++;

                    // Update targetReg and targetMask for the next putarg_reg (if any)
                    targetReg  = genRegArgNext(targetReg);
                    targetMask = genRegMask(targetReg);
                }
            }
            else
            {
#ifdef DEBUG
                compiler->gtDispTreeRange(BlockRange(), argNode);
#endif
                noway_assert(!"Unsupported TYP_STRUCT arg kind");
            }

            unsigned  slots          = ((unsigned)(roundUp(originalSize, REGSIZE_BYTES))) / REGSIZE_BYTES;
            regNumber curReg         = argReg;
            regNumber lastReg        = argIsFloat ? REG_ARG_FP_LAST : REG_ARG_LAST;
            unsigned  remainingSlots = slots;

            while (remainingSlots > 0)
            {
                argMask |= genRegMask(curReg);
                remainingSlots--;

                if (curReg == lastReg)
                    break;

                curReg = genRegArgNext(curReg);
            }

            // Struct typed arguments must be fully passed in registers (Reg/Stk split not allowed)
            noway_assert(remainingSlots == 0);
            argNode->gtLsraInfo.internalIntCount = 0;
        }
        else // A scalar argument (not a struct)
        {
            // We consume one source
            info->srcCount++;

            argMask |= genRegMask(argReg);
            argNode->gtLsraInfo.setDstCandidates(l, argMask);
            argNode->gtLsraInfo.setSrcCandidates(l, argMask);

            if (argNode->gtOper == GT_PUTARG_REG)
            {
                GenTreePtr putArgChild = argNode->gtOp.gtOp1;

                // To avoid redundant moves, request that the argument child tree be
                // computed in the register in which the argument is passed to the call.
                putArgChild->gtLsraInfo.setSrcCandidates(l, argMask);
            }
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

        // Skip arguments that have been moved to the Late Arg list
        if (!(args->gtFlags & GTF_LATE_ARG))
        {
            if (arg->gtOper == GT_PUTARG_STK)
            {
                fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, arg);
                assert(curArgTabEntry);

                assert(curArgTabEntry->regNum == REG_STK);

                TreeNodeInfoInitPutArgStk(arg->AsPutArgStk(), curArgTabEntry);
            }
            else
            {
                TreeNodeInfo* argInfo = &(arg->gtLsraInfo);
                if (argInfo->dstCount != 0)
                {
                    argInfo->isLocalDefUse = true;
                }

                argInfo->dstCount = 0;
            }
        }
        args = args->gtOp.gtOp2;
    }

    // If it is a fast tail call, it is already preferenced to use IP0.
    // Therefore, no need set src candidates on call tgt again.
    if (call->IsVarargs() && callHasFloatRegArgs && !call->IsFastTailCall() && (ctrlExpr != nullptr))
    {
        // Don't assign the call target to any of the argument registers because
        // we will use them to also pass floating point arguments as required
        // by Arm64 ABI.
        ctrlExpr->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~(RBM_ARG_REGS));
    }
}

//------------------------------------------------------------------------
//  TreeNodeInfoInitPutArgStk: Set the NodeInfo for a GT_PUTARG_STK node
//
// Arguments:
//    argNode       - a GT_PUTARG_STK node
//
// Return Value:
//    None.
//
// Notes:
//    Set the child node(s) to be contained when we have a multireg arg
//
void Lowering::TreeNodeInfoInitPutArgStk(GenTreePutArgStk* argNode, fgArgTabEntryPtr info)
{
    assert(argNode->gtOper == GT_PUTARG_STK);

    GenTreePtr putArgChild = argNode->gtOp.gtOp1;

    // Initialize 'argNode' as not contained, as this is both the default case
    //  and how MakeSrcContained expects to find things setup.
    //
    argNode->gtLsraInfo.srcCount = 1;
    argNode->gtLsraInfo.dstCount = 0;

    // Do we have a TYP_STRUCT argument (or a GT_FIELD_LIST), if so it must be a multireg pass-by-value struct
    if ((putArgChild->TypeGet() == TYP_STRUCT) || (putArgChild->OperGet() == GT_FIELD_LIST))
    {
        // We will use store instructions that each write a register sized value

        if (putArgChild->OperGet() == GT_FIELD_LIST)
        {
            // We consume all of the items in the GT_FIELD_LIST
            argNode->gtLsraInfo.srcCount = info->numSlots;
        }
        else
        {
            // We could use a ldp/stp sequence so we need two internal registers
            argNode->gtLsraInfo.internalIntCount = 2;

            if (putArgChild->OperGet() == GT_OBJ)
            {
                GenTreePtr objChild = putArgChild->gtOp.gtOp1;
                if (objChild->OperGet() == GT_LCL_VAR_ADDR)
                {
                    // We will generate all of the code for the GT_PUTARG_STK, the GT_OBJ and the GT_LCL_VAR_ADDR
                    // as one contained operation
                    //
                    MakeSrcContained(putArgChild, objChild);
                }
            }

            // We will generate all of the code for the GT_PUTARG_STK and it's child node
            // as one contained operation
            //
            MakeSrcContained(argNode, putArgChild);
        }
    }
    else
    {
        // We must not have a multi-reg struct
        assert(info->numSlots == 1);
    }
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
// Notes:

void Lowering::TreeNodeInfoInitBlockStore(GenTreeBlk* blkNode)
{
    GenTree*    dstAddr  = blkNode->Addr();
    unsigned    size     = blkNode->gtBlkSize;
    GenTree*    source   = blkNode->Data();
    LinearScan* l        = m_lsra;
    Compiler*   compiler = comp;

    // Sources are dest address and initVal or source.
    // We may require an additional source or temp register for the size.
    blkNode->gtLsraInfo.srcCount = 2;
    blkNode->gtLsraInfo.dstCount = 0;
    GenTreePtr srcAddrOrFill     = nullptr;
    bool       isInitBlk         = blkNode->OperIsInitBlkOp();

    if (!isInitBlk)
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
    }

    if (isInitBlk)
    {
        GenTreePtr initVal = source;
        if (initVal->OperIsInitVal())
        {
            initVal = initVal->gtGetOp1();
        }
        srcAddrOrFill = initVal;

#if 0
        if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll)
        {
            // TODO-ARM64-CQ: Currently we generate a helper call for every
            // initblk we encounter.  Later on we should implement loop unrolling
            // code sequences to improve CQ.
            // For reference see the code in lsraxarch.cpp.
        }
        else
#endif // 0
        {
            assert(blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindHelper);
            // The helper follows the regular ABI.
            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_0);
            initVal->gtLsraInfo.setSrcCandidates(l, RBM_ARG_1);
            if (size != 0)
            {
                // Reserve a temp register for the block size argument.
                blkNode->gtLsraInfo.setInternalCandidates(l, RBM_ARG_2);
                blkNode->gtLsraInfo.internalIntCount = 1;
            }
            else
            {
                // The block size argument is a third argument to GT_STORE_DYN_BLK
                noway_assert(blkNode->gtOper == GT_STORE_DYN_BLK);
                blkNode->gtLsraInfo.setSrcCount(3);
                GenTree* sizeNode = blkNode->AsDynBlk()->gtDynamicSize;
                sizeNode->gtLsraInfo.setSrcCandidates(l, RBM_ARG_2);
            }
        }
    }
    else
    {
        // CopyObj or CopyBlk
        // Sources are src and dest and size if not constant.

        if (blkNode->OperGet() == GT_STORE_OBJ)
        {
            // CopyObj

            // We don't need to materialize the struct size but we still need
            // a temporary register to perform the sequence of loads and stores.
            blkNode->gtLsraInfo.internalIntCount = 1;

            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_WRITE_BARRIER_DST_BYREF);
            // If we have a source address we want it in REG_WRITE_BARRIER_SRC_BYREF.
            // Otherwise, if it is a local, codegen will put its address in REG_WRITE_BARRIER_SRC_BYREF,
            // which is killed by a StoreObj (and thus needn't be reserved).
            if (srcAddrOrFill != nullptr)
            {
                srcAddrOrFill->gtLsraInfo.setSrcCandidates(l, RBM_WRITE_BARRIER_SRC_BYREF);
            }
        }
        else
        {
            // CopyBlk
            short     internalIntCount      = 0;
            regMaskTP internalIntCandidates = RBM_NONE;

#if 0
            if (blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindUnroll)
            {
                // TODO-ARM64-CQ: cpblk loop unrolling is currently not implemented.
                // In case of a CpBlk with a constant size and less than CPBLK_UNROLL_LIMIT size
                // we should unroll the loop to improve CQ.
                // For reference see the code in lsraxarch.cpp.
            }
            else
#endif // 0
            {
                assert(blkNode->gtBlkOpKind == GenTreeBlk::BlkOpKindHelper);
                dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_0);
                // The srcAddr goes in arg1.
                if (srcAddrOrFill != nullptr)
                {
                    srcAddrOrFill->gtLsraInfo.setSrcCandidates(l, RBM_ARG_1);
                }
                if (size != 0)
                {
                    // Reserve a temp register for the block size argument.
                    internalIntCandidates |= RBM_ARG_2;
                    internalIntCount++;
                }
                else
                {
                    // The block size argument is a third argument to GT_STORE_DYN_BLK
                    noway_assert(blkNode->gtOper == GT_STORE_DYN_BLK);
                    blkNode->gtLsraInfo.setSrcCount(3);
                    GenTree* blockSize = blkNode->AsDynBlk()->gtDynamicSize;
                    blockSize->gtLsraInfo.setSrcCandidates(l, RBM_ARG_2);
                }
            }
            if (internalIntCount != 0)
            {
                blkNode->gtLsraInfo.internalIntCount = internalIntCount;
                blkNode->gtLsraInfo.setInternalCandidates(l, internalIntCandidates);
            }
        }
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

void Lowering::TreeNodeInfoInitGCWriteBarrier(GenTree* tree)
{
    GenTreePtr dst  = tree;
    GenTreePtr addr = tree->gtOp.gtOp1;
    GenTreePtr src  = tree->gtOp.gtOp2;

    if (addr->OperGet() == GT_LEA)
    {
        // In the case where we are doing a helper assignment, if the dst
        // is an indir through an lea, we need to actually instantiate the
        // lea in a register
        GenTreeAddrMode* lea = addr->AsAddrMode();

        short leaSrcCount = 0;
        if (lea->Base() != nullptr)
        {
            leaSrcCount++;
        }
        if (lea->Index() != nullptr)
        {
            leaSrcCount++;
        }
        lea->gtLsraInfo.srcCount = leaSrcCount;
        lea->gtLsraInfo.dstCount = 1;
    }

#if NOGC_WRITE_BARRIERS
    // For the NOGC JIT Helper calls
    //
    // the 'addr' goes into x14 (REG_WRITE_BARRIER_DST_BYREF)
    // the 'src'  goes into x15 (REG_WRITE_BARRIER)
    //
    addr->gtLsraInfo.setSrcCandidates(m_lsra, RBM_WRITE_BARRIER_DST_BYREF);
    src->gtLsraInfo.setSrcCandidates(m_lsra, RBM_WRITE_BARRIER);
#else
    // For the standard JIT Helper calls
    // op1 goes into REG_ARG_0 and
    // op2 goes into REG_ARG_1
    //
    addr->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_0);
    src->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_1);
#endif // NOGC_WRITE_BARRIERS

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
    assert(indirTree->OperIsIndir());
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
    unsigned   cns   = 0;
    unsigned   mul;
    bool       rev;
    bool       modifiedSources = false;

    if ((addr->OperGet() == GT_LEA) && IsSafeToContainMem(indirTree, addr))
    {
        GenTreeAddrMode* lea = addr->AsAddrMode();
        base                 = lea->Base();
        index                = lea->Index();
        cns                  = lea->gtOffset;

        m_lsra->clearOperandCounts(addr);
        // The srcCount is decremented because addr is now "contained",
        // then we account for the base and index below, if they are non-null.
        info->srcCount--;
    }
    else if (comp->codeGen->genCreateAddrMode(addr, -1, true, 0, &rev, &base, &index, &mul, &cns, true /*nogen*/) &&
             !(modifiedSources = AreSourcesPossiblyModifiedLocals(indirTree, base, index)))
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

    // On ARM64 we may need a single internal register
    // (when both conditions are true then we still only need a single internal register)
    if ((index != nullptr) && (cns != 0))
    {
        // ARM64 does not support both Index and offset so we need an internal register
        info->internalIntCount = 1;
    }
    else if (!emitter::emitIns_valid_imm_for_ldst_offset(cns, emitTypeSize(indirTree)))
    {
        // This offset can't be contained in the ldr/str instruction, so we need an internal register
        info->internalIntCount = 1;
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
    TreeNodeInfo* info = &(tree->gtLsraInfo);

    info->srcCount = 2;
    info->dstCount = 1;
    CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
}

#endif // _TARGET_ARM64_

#endif // !LEGACY_BACKEND
