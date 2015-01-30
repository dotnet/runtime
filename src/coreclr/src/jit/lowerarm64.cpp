//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for ARM64                              XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the ARM64         XX
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

#ifdef _TARGET_ARM64_

#include "jit.h"
#include "lower.h"

// there is not much lowering to do with storing a local but 
// we do some handling of contained immediates and widening operations of unsigneds
void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    TreeNodeInfo* info = &(storeLoc->gtLsraInfo);

    CheckImmedAndMakeContained(storeLoc, storeLoc->gtOp1);

    // Try to widen the ops if they are going into a local var.
    if ((storeLoc->gtOper == GT_STORE_LCL_VAR) &&
        (storeLoc->gtOp1->gtOper == GT_CNS_INT))
    {
        GenTreeIntCon* con = storeLoc->gtOp1->AsIntCon();
        ssize_t       ival = con->gtIconVal;

        unsigned        varNum = storeLoc->gtLclNum;
        LclVarDsc*      varDsc = comp->lvaTable + varNum;

        // If we are storing a constant into a local variable
        // we extend the size of the store here 
        if (genTypeSize(storeLoc) < 4)
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
            // TODO-ARM64-CQ: if the field is promoted shouldn't we also be able to do this?
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
 *    LSRA has been initialized and there is a TreeNodeInfo node
 *    already allocated and initialized for every tree in the IR.
 * Postconditions:
 *    Every TreeNodeInfo instance has the right annotations on register
 *    requirements needed by LSRA to build the Interval Table (source, 
 *    destination and internal [temp] register counts).
 *    This code is refactored originally from LSRA.
 */
void Lowering::TreeNodeInfoInit(GenTree* stmt)
{
    LinearScan* l = m_lsra;
    Compiler* compiler = comp;

    assert(stmt->gtStmt.gtStmtIsTopLevel());
    GenTree* tree = stmt->gtStmt.gtStmtList;
    
    while (tree)
    {
        unsigned kind = tree->OperKind();
        TreeNodeInfo* info = &(tree->gtLsraInfo);
        RegisterType registerType = TypeGet(tree);
        GenTree* next = tree->gtNext;

        switch (tree->OperGet())
        {
            GenTree* op1;
            GenTree* op2;

        default:
            info->dstCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
            if (kind & (GTK_CONST|GTK_LEAF))
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
            break;

        case GT_STORE_LCL_FLD:
            info->srcCount = 1;
            info->dstCount = 0;
            LowerStoreLoc(tree->AsLclVarCommon());
            break;

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
                    firstOperand->gtLsraInfo.dstCount = 0;
                }
                if (tree->TypeGet() == TYP_VOID && secondOperand->TypeGet() != TYP_VOID)
                {
                    secondOperand->gtLsraInfo.isLocalDefUse = true;
                    secondOperand->gtLsraInfo.dstCount = 0;
                }
            }

            __fallthrough;

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

        case GT_QMARK:
        case GT_COLON:
            info->srcCount = 0;
            info->dstCount = 0;
            unreached();
            break;

        case GT_RETURN:
            info->srcCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
            info->dstCount = 0;

            regMaskTP useCandidates;
            switch (tree->TypeGet())
            {
            case TYP_VOID:   useCandidates = RBM_NONE; break;
            case TYP_FLOAT:  useCandidates = RBM_FLOATRET; break;
            case TYP_DOUBLE: useCandidates = RBM_DOUBLERET; break;
            case TYP_LONG:   useCandidates = RBM_LNGRET; break;
            default:         useCandidates = RBM_INTRET; break;
            }
            if (useCandidates != RBM_NONE)
            {
                tree->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, useCandidates);
            }
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
                info->dstCount = 1;

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
            info->dstCount = 0;  // To avoid getting uninit errors.
            noway_assert(!"Switch must be lowered at this point");
            break;

        case GT_JMPTABLE:
            info->srcCount = 0;
            info->dstCount = 1;
            break;

        case GT_SWITCH_TABLE:
            info->srcCount = 2;
            info->internalIntCount = 1;
            info->dstCount = 0;
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
            info->dstCount = 1;
            break;

        case GT_MOD:
        case GT_UMOD:
            NYI_IF(varTypeIsFloating(tree->TypeGet()), "FP Remainder in ARM64");
            assert(!"Shouldn't see an integer typed GT_MOD node in ARM64");
            break;
   
        case GT_MUL:
            if (tree->gtOverflow())
            {
                info->internalIntCount = 1;
            }
            __fallthrough;

        case GT_DIV:
        case GT_MULHI:
        case GT_UDIV:
            {
                // TODO-ARM64-CQ: Optimize a divide by power of 2 as we do for AMD64
                info->srcCount = 2;
                info->dstCount = 1;
            }
            break;
        
        case GT_MATH:
            {
                NYI("Math intrinsics");
#if 0
                // TODO-ARM64-NYI
                // Right now only Sqrt/Abs are treated as math intrinsics
                noway_assert((tree->gtMath.gtMathFN == CORINFO_INTRINSIC_Sqrt) || 
                             (tree->gtMath.gtMathFN == CORINFO_INTRINSIC_Abs));

                // Both operand and its result must be of floating point type.
                op1 = tree->gtOp.gtOp1;
                assert(varTypeIsFloating(op1));
                assert(op1->TypeGet() == tree->TypeGet());

                info->srcCount = 1;
                info->dstCount = 1;

                switch (tree->gtMath.gtMathFN)
                {
                case CORINFO_INTRINSIC_Sqrt:
                    if (op1->isMemoryOp() || op1->IsCnsNonZeroFltOrDbl())
                    {
                        MakeSrcContained(tree, op1);
                    }
                    break;

                case CORINFO_INTRINSIC_Abs:
                    // Abs(float x) = x & 0x7fffffff
                    // Abs(double x) = x & 0x7ffffff ffffffff

                    // In case of Abs we need an internal register to hold mask.
                     
                    // TODO-ARM64-CQ: avoid using an internal register for the mask.
                    // Andps or andpd both will operate on 128-bit operands.  
                    // The data section constant to hold the mask is a 64-bit size.
                    // Therefore, we need both the operand and mask to be in 
                    // xmm register. When we add support in emitter to emit 128-bit
                    // data constants and instructions that operate on 128-bit
                    // memory operands we can avoid the need for an internal register.
                    if (tree->gtMath.gtMathFN == CORINFO_INTRINSIC_Abs)
                    {
                        info->internalFloatCount = 1;
                        info->setInternalCandidates(l, l->internalFloatRegCandidates());
                    }
                    break;

                default:
                     assert(!"Unsupported math intrinsic");
                     unreached();
                     break;
                }
#endif // 0
            }
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            TreeNodeInfoInitSIMD(tree, l);
            break;
#endif // FEATURE_SIMD

        case GT_CAST:
            {
                // TODO-ARM64-CQ: Int-To-Int conversions - castOp cannot be a memory op and must have an assigned register.
                //         see CodeGen::genIntToIntCast() 

                info->srcCount = 1;
                info->dstCount = 1;

                // Non-overflow casts to/from float/double are done using SSE2 instructions
                // and that allow the source operand to be either a reg or memop. Given the
                // fact that casts from small int to float/double are done as two-level casts, 
                // the source operand is always guaranteed to be of size 4 or 8 bytes.
                var_types castToType = tree->CastToType();
                GenTreePtr castOp    = tree->gtCast.CastOp();
                var_types castOpType = castOp->TypeGet();
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
#endif //DEBUG

                    // U8 -> R8 conversion requires that the operand be in a register.
                    if (castOpType != TYP_ULONG)
                    {
                        if (castOp->isMemoryOp() || castOp->IsCnsNonZeroFltOrDbl())
                        {
                            MakeSrcContained(tree, castOp);
                        }
                    }
                }

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
            {
                info->srcCount = 2;
                info->dstCount = 1;

                GenTreePtr shiftBy = tree->gtOp.gtOp2;
                GenTreePtr source = tree->gtOp.gtOp1;
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
            LowerCmp(tree);
            break;

        case GT_CKFINITE:
            info->srcCount = 1;
            info->dstCount = 1;
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
            {
                info->srcCount = 0;
                info->dstCount =  (tree->TypeGet() != TYP_VOID) ? 1 : 0;

                GenTree *ctrlExpr = tree->gtCall.gtControlExpr;
                if (tree->gtCall.gtCallType == CT_INDIRECT)
                {
                    // either gtControlExpr != null or gtCallAddr != null.
                    // Both cannot be non-null at the same time.
                    assert(ctrlExpr == nullptr);
                    assert(tree->gtCall.gtCallAddr != nullptr);
                    ctrlExpr = tree->gtCall.gtCallAddr;
                }

                // set reg requirements on call target represented as control sequence.
                if (ctrlExpr != nullptr)
                {
                    // we should never see a gtControlExpr whose type is void.
                    assert(ctrlExpr->TypeGet() != TYP_VOID);                
                    
                    info->srcCount++;               
                    
                    // In case of fast tail implemented as jmp, make sure that gtControlExpr is
                    // computed into a register.
                    if (tree->gtCall.IsFastTailCall())
                    {
                        // Fast tail call - make sure that call target is always computed in IP0
                        // so that epilog sequence can generate "br xip0" to achieve fast tail call.
                        
                        NYI_ARM64("Lower - Fast tail call");

                        ctrlExpr->gtLsraInfo.setSrcCandidates(l, genRegMask(REG_IP0));  // ip0?
                    }
                }

                // Set destination candidates for return value of the call.
                if (varTypeIsFloating(registerType))
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
                if (tree->gtCall.gtCallObjp != nullptr)
                {
                    GenTreePtr thisPtrNode = tree->gtCall.gtCallObjp;

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

                for (GenTreePtr list = tree->gtCall.gtCallLateArgs; list; list = list->MoveNext())
                {
                    assert(list->IsList());

                    GenTreePtr argNode = list->Current();

                    fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(tree, argNode);
                    assert(curArgTabEntry);

                    if (curArgTabEntry->regNum == REG_STK)
                    {
                        // late arg that is not passed in a register
                        DISPNODE(argNode);
                        assert(argNode->gtOper == GT_PUTARG_STK);
                        argNode->gtLsraInfo.srcCount = 1;
                        argNode->gtLsraInfo.dstCount = 0;
                        continue;
                    }

                    var_types argType = argNode->TypeGet();

                    callHasFloatRegArgs |= varTypeIsFloating(argType);

                    regNumber argReg = curArgTabEntry->regNum;
                    short regCount = 1;
                    // Default case is that we consume one source; modify this later (e.g. for
                    // promoted structs)
                    info->srcCount++;

                    regMaskTP argMask = genRegMask(argReg);
                    argNode = argNode->gtEffectiveVal();
                    
                    if (argNode->TypeGet() == TYP_STRUCT)
                    {
                        unsigned originalSize = 0;
                        bool isPromoted = false;
                        LclVarDsc* varDsc = nullptr;
                        if (argNode->gtOper == GT_LCL_VAR)
                        {
                            varDsc = compiler->lvaTable + argNode->gtLclVarCommon.gtLclNum;
                            originalSize = varDsc->lvSize();
                        }
                        else if (argNode->gtOper == GT_MKREFANY)
                        {
                            originalSize = 2 * TARGET_POINTER_SIZE;
                        }
                        else if (argNode->gtOper == GT_LDOBJ)
                        {
                            noway_assert(!"GT_LDOBJ not supported for arm64");
                        }
                        else
                        {
                            assert(!"Can't predict unsupported TYP_STRUCT arg kind");
                        }

                        unsigned slots = ((unsigned)(roundUp(originalSize, TARGET_POINTER_SIZE))) / REGSIZE_BYTES;
                        regNumber reg = (regNumber)(argReg + 1);
                        unsigned remainingSlots = slots - 1;

                        if (remainingSlots > 1)
                        {
                            NYI_ARM64("Lower - Struct typed arguments (size>16)");
                        }

                        while (remainingSlots > 0 && reg <= REG_ARG_LAST)
                        {
                            argMask |= genRegMask(reg);
                            reg = (regNumber)(reg + 1);
                            remainingSlots--;
                            regCount++;
                        }

                        if (remainingSlots > 1)
                        {
                            NYI_ARM64("Lower - Struct typed arguments (Reg/Stk split)");
                        }

                        short internalIntCount = 0;
                        if (remainingSlots > 0)
                        {
                            // This TYP_STRUCT argument is also passed in the outgoing argument area
                            // We need a register to address the TYP_STRUCT
                            // And we may need 2
                            internalIntCount = 2;
                        }
                        argNode->gtLsraInfo.internalIntCount = internalIntCount;
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
                }

                // Now, count stack args
                // Note that these need to be computed into a register, but then
                // they're just stored to the stack - so the reg doesn't
                // need to remain live until the call.  In fact, it must not
                // because the code generator doesn't actually consider it live,
                // so it can't be spilled.

                GenTreePtr args = tree->gtCall.gtCallArgs;
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
                        argInfo->dstCount = 0;
                    }
                    args = args->gtOp.gtOp2;
                }

                // If it is a fast tail call, it is already preferenced to use IP0.
                // Therefore, no need set src candidates on call tgt again.
                if (tree->gtCall.IsVarargs() && 
                    callHasFloatRegArgs &&                 
                    !tree->gtCall.IsFastTailCall() &&
                    (ctrlExpr != nullptr))
                {
                    // Don't assign the call target to any of the argument registers because
                    // we will use them to also pass floating point arguments as required
                    // by Arm64 ABI.
                    
                    NYI_ARM64("Lower - IsVarargs");

                    ctrlExpr->gtLsraInfo.setSrcCandidates(l, l->allRegs(TYP_INT) & ~(RBM_ARG_REGS));
                }
            }
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

        case GT_INITBLK:
            {
                // Sources are dest address, initVal and size
                info->srcCount = 3;
                info->dstCount = 0;

                GenTreeInitBlk* initBlkNode = tree->AsInitBlk();

                GenTreePtr blockSize = initBlkNode->Size();
                GenTreePtr   dstAddr = initBlkNode->Dest();
                GenTreePtr   initVal = initBlkNode->InitVal();

                // TODO-ARM64-CQ: Currently we generate a helper call for every
                // initblk we encounter.  Later on we should implement loop unrolling
                // code sequences to improve CQ.
                // For reference see the code in LowerXArch.cpp.

#if 0
                // If we have an InitBlk with constant block size we can speed this up by unrolling the loop.
                if (blockSize->IsCnsIntOrI() && 
                    blockSize->gtIntCon.gtIconVal <= INITBLK_UNROLL_LIMIT &&
                    && initVal->IsCnsIntOrI())
                {
                    ssize_t size = blockSize->gtIntCon.gtIconVal;
                    // Replace the integer constant in initVal 
                    // to fill an 8-byte word with the fill value of the InitBlk
                    assert(initVal->gtIntCon.gtIconVal == (initVal->gtIntCon.gtIconVal & 0xFF));
                    if (size < REGSIZE_BYTES)
                    {
                        initVal->gtIntCon.gtIconVal = 0x01010101 * initVal->gtIntCon.gtIconVal;
                    }
                    else
                    {
                        initVal->gtIntCon.gtIconVal = 0x0101010101010101LL * initVal->gtIntCon.gtIconVal;
                        initVal->gtType = TYP_LONG;
                    }

                    MakeSrcContained(tree, blockSize);

                    // In case we have a buffer >= 16 bytes
                    // we can use SSE2 to do a 128-bit store in a single
                    // instruction.
                    if (size >= XMM_REGSIZE_BYTES)
                    {
                        // Reserve an XMM register to fill it with 
                        // a pack of 16 init value constants.
                        info->internalFloatCount = 1;
                        info->setInternalCandidates(l, l->internalFloatRegCandidates());
                    }
                    initBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindUnroll;
                    }
                }
                else
#endif // 0
                {
                    // The helper follows the regular AMD64 ABI.
                    dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_ARG_0);
                    initVal->gtLsraInfo.setSrcCandidates(l, RBM_ARG_1);
                    blockSize->gtLsraInfo.setSrcCandidates(l, RBM_ARG_2);
                    initBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindHelper;
                }
            }
            break;

        case GT_COPYOBJ:
            {
                // Sources are src, dest and size (or class token for CpObj).
                info->srcCount = 3;
                info->dstCount = 0;

                GenTreeCpObj* cpObjNode = tree->AsCpObj();

                GenTreePtr  clsTok = cpObjNode->ClsTok();
                GenTreePtr dstAddr = cpObjNode->Dest();
                GenTreePtr srcAddr = cpObjNode->Source();

                unsigned slots = cpObjNode->gtSlots;

#ifdef DEBUG
                // CpObj must always have at least one GC-Pointer as a member.
                assert(cpObjNode->gtGcPtrCount > 0);

                assert(dstAddr->gtType == TYP_BYREF || dstAddr->gtType == TYP_I_IMPL);
                assert(clsTok->IsIconHandle());

                CORINFO_CLASS_HANDLE clsHnd = (CORINFO_CLASS_HANDLE)clsTok->gtIntCon.gtIconVal;
                size_t classSize = compiler->info.compCompHnd->getClassSize(clsHnd);
                size_t blkSize = roundUp(classSize, TARGET_POINTER_SIZE);

                // Currently, the EE always round up a class data structure so 
                // we are not handling the case where we have a non multiple of pointer sized 
                // struct. This behavior may change in the future so in order to keeps things correct
                // let's assert it just to be safe. Going forward we should simply
                // handle this case.
                assert(classSize == blkSize);
                assert((blkSize / TARGET_POINTER_SIZE) == slots);
                assert((cpObjNode->gtFlags & GTF_BLK_HASGCPTR) != 0);
#endif

                // We don't need to materialize the struct size but we still need
                // a temporary register to perform the sequence of loads and stores.
                MakeSrcContained(tree, clsTok);
                info->internalIntCount = 1;

                dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_WRITE_BARRIER_DST_BYREF);
                srcAddr->gtLsraInfo.setSrcCandidates(l, RBM_WRITE_BARRIER_SRC_BYREF);
            }
            break;

        case GT_COPYBLK:
            {
                // Sources are src, dest and size (or class token for CpObj).
                info->srcCount = 3;
                info->dstCount = 0;

                GenTreeCpBlk* cpBlkNode = tree->AsCpBlk();

                GenTreePtr blockSize = cpBlkNode->Size();
                GenTreePtr   dstAddr = cpBlkNode->Dest();
                GenTreePtr   srcAddr = cpBlkNode->Source();

                // In case of a CpBlk with a constant size and less than CPBLK_UNROLL_LIMIT size
                // we should unroll the loop to improve CQ.

                // TODO-ARM64-CQ: cpblk loop unrolling is currently not implemented.
#if 0
                if (blockSize->IsCnsIntOrI() && blockSize->gtIntCon.gtIconVal <= CPBLK_UNROLL_LIMIT)
                {
                    assert(!blockSize->IsIconHandle());
                    ssize_t size = blockSize->gtIntCon.gtIconVal;

                    // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2. 
                    // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
                    // our framework assemblies, so this is the main code generation scheme we'll use.
                    if ((size & (XMM_REGSIZE_BYTES - 1)) != 0)
                    {
                        info->internalIntCount++;
                        info->addInternalCandidates(l, l->allRegs(TYP_INT));
                    }

                    if (size >= XMM_REGSIZE_BYTES)
                    {
                        // If we have a buffer larger than XMM_REGSIZE_BYTES, 
                        // reserve an XMM register to use it for a 
                        // series of 16-byte loads and stores.
                        info->internalFloatCount = 1;
                        info->addInternalCandidates(l, l->internalFloatRegCandidates());
                    }

                    // If src or dst are on stack, we don't have to generate the address into a register
                    // because it's just some constant+SP
                    if (srcAddr->OperIsLocalAddr())
                    {
                        MakeSrcContained(tree, srcAddr);
                    }

                    if (dstAddr->OperIsLocalAddr())
                    {
                        MakeSrcContained(tree, dstAddr);
                    }

                    cpBlkNode->gtBlkOpKind = GenTreeBlkOp::BlkOpKindUnroll;
                }
                else
#endif // 0
                {
                    // In case we have a constant integer this means we went beyond
                    // CPBLK_UNROLL_LIMIT bytes of size, still we should never have the case of
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
            }
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
                        // This should also help in debugging as we can examine the original size specified with localloc.
                        sizeVal = AlignUp(sizeVal, STACK_ALIGN);
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
                            if (sizeVal < CORINFO_PAGE_SIZE)
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
                GenTree* other = nullptr;
                if (CheckImmedAndMakeContained(tree, node->gtIndex))
                {
                    intCns = node->gtIndex;
                    other = node->gtArrLen;
                }
                else if (CheckImmedAndMakeContained(tree, node->gtArrLen))
                {
                    intCns = node->gtArrLen;
                    other = node->gtIndex;
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
            // For GT_ARR_INDEX, the lifetime of the arrObj must be extended because it is actually used multiple
            // times while the result is being computed.
            tree->AsArrIndex()->ArrObj()->gtLsraInfo.isDelayFree = true;
            info->hasDelayFreeSrc = true;
            break;

        case GT_ARR_OFFSET:
            // This consumes the offset, if any, the arrObj and the effective index,
            // and produces the flattened offset for this dimension.
            info->srcCount = 3;
            info->dstCount = 1;
            info->internalIntCount = 1;
            // we don't want to generate code for this
            if (tree->gtArrOffs.gtOffset->IsZero())
            {
                MakeSrcContained(tree, tree->gtArrOffs.gtOffset);
            }
            break;

        case GT_LEA:
            // The LEA usually passes its operands through to the GT_IND, in which case we'll
            // clear the info->srcCount and info->dstCount later, but we may be instantiating an address,
            // so we set them here.
            info->srcCount = 0;
            if (tree->AsAddrMode()->Base() != nullptr)
            {
                info->srcCount++;
            }
            if (tree->AsAddrMode()->Index() != nullptr)
            {
                info->srcCount++;
            }
            info->dstCount = 1;
            break;

        case GT_STOREIND:
            {
                info->srcCount = 2;
                info->dstCount = 0;
                GenTree* src = tree->gtOp.gtOp2;

                if (compiler->codeGen->gcInfo.gcIsWriteBarrierAsgNode(tree))
                {
                    LowerGCWriteBarrier(tree);
                    break;
                }
                if (!varTypeIsFloating(src->TypeGet()) && src->IsZero())
                {
                    // an integer zero for 'src' can be contained.
                    MakeSrcContained(tree, src);
                }

                GenTreePtr addr = tree->gtOp.gtOp1;

                HandleIndirAddressExpression(tree, addr);
            }
            break;
        
        case GT_NULLCHECK:
            info->isLocalDefUse = true;

            __fallthrough;

        case GT_IND:
            {
                info->dstCount = tree->OperGet() == GT_NULLCHECK ? 0 : 1;
                info->srcCount = 1;
                
                GenTreePtr addr = tree->gtOp.gtOp1;
                
                HandleIndirAddressExpression(tree, addr);
            }
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
            assert((tree->gtFlags & (GTF_VAR_DEF|GTF_VAR_USEASG|GTF_VAR_USEDEF)) == 0);
            info->internalIntCount = 1;
            break;
        } // end switch (tree->OperGet())
    
        tree = next;

        // We need to be sure that we've set info->srcCount and info->dstCount appropriately
        assert(info->dstCount < 2);
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

void
Lowering::TreeNodeInfoInitSIMD(GenTree* tree, LinearScan* lsra)
{
    NYI("TreeNodeInfoInitSIMD");
    GenTreeSIMD* simdTree = tree->AsSIMD();
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    info->dstCount = 1;
    switch(simdTree->gtSIMDIntrinsicID)
    {
    case SIMDIntrinsicInit:
        {
            // This sets all fields of a SIMD struct to the given value.
            // Mark op1 as contained if it is either zero or int constant of all 1's.
            info->srcCount = 1;
            GenTree* op1 = tree->gtOp.gtOp1;
            if (op1->IsZero() || 
                (simdTree->gtSIMDBaseType == TYP_INT && op1->IsCnsIntOrI() && op1->AsIntConCommon()->IconValue() == 0xffffffff) ||
                (simdTree->gtSIMDBaseType == TYP_LONG && op1->IsCnsIntOrI() && op1->AsIntConCommon()->IconValue() == 0xffffffffffffffffLL)
                )               
            {
                MakeSrcContained(tree, tree->gtOp.gtOp1);
                info->srcCount = 0;
            }
        }
        break;

    case SIMDIntrinsicInitN:
        info->srcCount = (int)(simdTree->gtSIMDSize / genTypeSize(simdTree->gtSIMDBaseType));
        // Need an internal register to stitch together all the values into a single vector in an XMM reg
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
        if (simdTree->gtSIMDIntrinsicID == SIMDIntrinsicMul && 
            simdTree->gtSIMDBaseType == TYP_INT)
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
        info->srcCount = 2;
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
        info->srcCount = 2;
        info->internalFloatCount = 2;
        info->setInternalCandidates(lsra, lsra->allSIMDRegs());
        break;

    case SIMDIntrinsicGetItem:
        // This implements get_Item method. The sources are:
        //  - the source SIMD struct
        //  - index (which element to get)
        // The result is baseType of SIMD struct.
        info->srcCount = 2;

        op2 = tree->gtGetOp2()
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
            (void) comp->getSIMDInitTempVarNum();
        }
        else if (!varTypeIsFloating(simdTree->gtSIMDBaseType) && !op2->IsZero())
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

void Lowering::LowerGCWriteBarrier(GenTree* tree)
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
    src->gtLsraInfo.setSrcCandidates(m_lsra,  RBM_WRITE_BARRIER);
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


void Lowering::HandleIndirAddressExpression(GenTree* indirTree, GenTree* addr)
{
    GenTree* base = nullptr;
    GenTree* index = nullptr;
    unsigned mul, cns;
    bool rev;
    bool modifiedSources = false;
    TreeNodeInfo* info = &(indirTree->gtLsraInfo);

    if (addr->OperGet() == GT_LEA)
    {
        GenTreeAddrMode* lea = addr->AsAddrMode();
        base  = lea->Base();
        index = lea->Index();

        m_lsra->clearOperandCounts(addr);
        // The srcCount is decremented because addr is now "contained", 
        // then we account for the base and index below, if they are non-null.    
        info->srcCount--;
    }
    else if (comp->codeGen->genCreateAddrMode(addr, -1, true, 0, &rev, &base, &index, &mul, &cns, true /*nogen*/)
        && !(modifiedSources = AreSourcesPossiblyModified(indirTree, base, index)))
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

        bool foundBase = (base == nullptr);
        bool foundIndex = (index == nullptr);
        GenTreePtr nextChild = nullptr;
        for (GenTreePtr child = addr;
             child != nullptr && !child->OperIsLeaf();
             child = nextChild)
        {
            nextChild = nullptr;
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
        info->srcCount--; //base gets added in below
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
    CheckImmedAndMakeContained(tree, tree->gtOp.gtOp2);
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
 * Note that for the overflow conversions we still depend on helper calls and
 * don't expect to see them here. 
 * i) GT_CAST(float/double, int type with overflow detection) 
 *
 */
void Lowering::LowerCast( GenTreePtr* ppTree) 
{
    GenTreePtr  tree = *ppTree;
    assert(tree->OperGet() == GT_CAST);

    GenTreePtr  op1 = tree->gtOp.gtOp1;
    var_types   dstType = tree->CastToType();
    var_types   srcType = op1->TypeGet();
    var_types   tmpType = TYP_UNDEF;

    // We should never see the following casts as they are expected to be lowered 
    // apropriately or converted into helper calls by front-end.
    //   srcType = float/double   dstType = * and overflow detecting cast
    //       Reason: must be converted to a helper call
    //
    if (varTypeIsFloating(srcType))
    {
        noway_assert(!tree->gtOverflow());
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
        tmp->gtFlags |= (tree->gtFlags & (GTF_UNSIGNED|GTF_OVERFLOW|GTF_EXCEPT));

        tree->gtFlags &= ~GTF_UNSIGNED;
        tree->gtOp.gtOp1 = tmp;
        op1->InsertAfterSelf(tmp);
    }
}

// TODO-Cleanup: move to Lower.cpp?
void Lowering::SetStoreIndOpCounts(GenTreePtr storeInd, GenTreePtr indirCandidate)
{
    GenTreePtr indirDst = storeInd->gtGetOp1();
    GenTreePtr indirSrc = storeInd->gtGetOp2();
    TreeNodeInfo* info = &(storeInd->gtLsraInfo);

    info->dstCount = 0;

    m_lsra->clearOperandCounts(indirSrc);
    m_lsra->clearOperandCounts(indirCandidate);
    GenTreePtr indirCandidateChild = indirCandidate->gtGetOp1();
    if (indirCandidateChild->OperGet() == GT_LEA)
    {
        GenTreeAddrMode* addrMode = indirCandidateChild->AsAddrMode();
        assert(addrMode->Base()->OperIsLeaf());
        m_lsra->clearOperandCounts(addrMode->Base());
        info->srcCount++;

        if (addrMode->Index() != nullptr)
        {
            assert(addrMode->Index()->OperIsLeaf());
            m_lsra->clearOperandCounts(addrMode->Index());
            info->srcCount++;
        }

        m_lsra->clearOperandCounts(indirDst);
    }
    else 
    {
        assert(indirCandidateChild->OperGet() == GT_LCL_VAR);
        info->srcCount += indirCandidateChild->gtLsraInfo.dstCount;
    }
    m_lsra->clearOperandCounts(indirCandidateChild);
}

// returns true if the tree can use the read-modify-write memory instruction form
bool Lowering::isRMWRegOper(GenTreePtr tree)
{
    return false;
}

bool Lowering::IsCallTargetInRange(void* addr)
{
    // TODO-ARM64-CQ:  This is a workaround to unblock the JIT from getting calls working.
    // Currently, we'll be generating calls using blr and manually loading an absolute
    // call target in a register using a sequence of load immediate instructions.
    //
    // As you can expect, this is inefficient and it's not the recommended way as per the
    // ARM64 ABI Manual but will get us getting things done for now.
    // The work to get this right would be to implement PC-relative calls, the bl instruction
    // can only address things -128 + 128MB away, so this will require getting some additional
    // code to get jump thunks working.
    return true;
}

// return true if the immediate can be folded into an instruction, for example small enough and non-relocatable
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (varTypeIsFloating(parentNode->TypeGet()))
    {
        // We can contain a floating point 0.0 constant in a compare instruction
        switch  (parentNode->OperGet())
        {
        default:
            return false;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            if (childNode->IsZero())
                return true;
            break;
        }
    }
    else
    {
        // Make sure we have an actual immediate 
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->IsIconHandle() && comp->opts.compReloc)
            return false;

        ssize_t   immVal = childNode->gtIntCon.gtIconVal;
        emitAttr  attr   = emitActualTypeSize(childNode->TypeGet());
        emitAttr  size   = EA_SIZE(attr);

        switch (parentNode->OperGet())
        {
        default:
            return false;

        case GT_ADD:
        case GT_SUB:
            if (emitter::emitIns_valid_imm_for_add(immVal, size))
                return true;
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            if (emitter::emitIns_valid_imm_for_cmp(immVal, size))
                return true;
            break;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            if (emitter::emitIns_valid_imm_for_alu(immVal, size))
                return true;
            break;

        case GT_STORE_LCL_VAR:
            if (immVal == 0)
                return true;
            break;
        }
    }

    return false;
}

#endif // _TARGET_ARM64_

#endif // !LEGACY_BACKEND
