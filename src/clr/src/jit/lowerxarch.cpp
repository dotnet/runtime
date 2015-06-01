//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

// there is not much lowering to do with storing a local but 
// we do some handling of contained immediates and widening operations of unsigneds
void Lowering::LowerStoreLoc(GenTreeLclVarCommon* storeLoc)
{
    TreeNodeInfo* info = &(storeLoc->gtLsraInfo);

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
    GenTree* op1 = storeLoc->gtOp1;
    if (IsContainableImmed(storeLoc, op1) && (!op1->IsZero() || varTypeIsSmall(storeLoc)))
    {
        MakeSrcContained(storeLoc, op1);
    }

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

#if !defined(_TARGET_64BIT_)
        case GT_LONG:
            // Passthrough
            info->srcCount = 0;
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
                info->srcCount = (tree->TypeGet() == TYP_VOID) ? 0 : 1;
                info->dstCount = 0;

                regMaskTP useCandidates;
                switch (tree->TypeGet())
                {
                case TYP_VOID:   useCandidates = RBM_NONE; break;
                case TYP_FLOAT:  useCandidates = RBM_FLOATRET; break;
                case TYP_DOUBLE: useCandidates = RBM_DOUBLERET; break;
#if defined(_TARGET_64BIT_)
                case TYP_LONG:   useCandidates = RBM_LNGRET; break;
#endif // defined(_TARGET_64BIT_)
                default:         useCandidates = RBM_INTRET; break;
                }
                if (useCandidates != RBM_NONE)
                {
                    tree->gtOp.gtOp1->gtLsraInfo.setSrcCandidates(l, useCandidates);
                }
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
            // SSE2 arithmetic instructions doesn't support the form "op mem, xmm".  
            // Rather they only support "op xmm, mem/xmm" form.
            if (varTypeIsFloating(tree->TypeGet()))
            {
                // overflow operations aren't supported on float/double types.
                assert(!tree->gtOverflow());

                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(tree->gtOp.gtOp1->TypeGet() == tree->gtOp.gtOp2->TypeGet());

                info->srcCount = 2;
                info->dstCount = 1;              

                op2 = tree->gtOp.gtOp2; 
                if (op2->isMemoryOp() || op2->IsCnsNonZeroFltOrDbl())
                {
                    MakeSrcContained(tree, op2);
                }
                break;
            }

            __fallthrough;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
        {            
            // We're not marking a constant hanging on the left of the add
            // as containable so we assign it to a register having CQ impact.
            // TODO-XArch-CQ: Detect this case and support both generating a single instruction 
            // for GT_ADD(Constant, SomeTree) and GT_ADD(SomeTree, Constant)
            info->srcCount = 2;
            info->dstCount = 1;
            op2 = tree->gtOp.gtOp2;

            // We can directly encode the second operand if it is either a containable constant or a local field.
            // In case of local field, we can encode it directly provided its type matches with 'tree' type.
            // This is because during codegen, type of 'tree' is used to determine emit Type size. If the types
            // do not match, they get normalized (i.e. sign/zero extended) on load into a register.
            bool directlyEncodable = false;
            if (IsContainableImmed(tree, op2))
            {
                directlyEncodable = true;
            }
            else if ((tree->gtOp.gtOp1->gtOper != GT_IND)  && op2->isLclField() && tree->TypeGet() == op2->TypeGet())
            {
                directlyEncodable = true;
            }

            if (directlyEncodable)
            {
                l->clearDstCount(op2);
                info->srcCount = 1;
            }
        }
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
            if (varTypeIsFloating(tree->TypeGet()))
            {   
                // No implicit conversions at this stage as the expectation is that
                // everything is made explicit by adding casts.
                assert(tree->gtOp.gtOp1->TypeGet() == tree->gtOp.gtOp2->TypeGet());

                info->srcCount = 2;
                info->dstCount = 1;

                op2 = tree->gtOp.gtOp2;
                if (op2->isMemoryOp() || op2->IsCnsNonZeroFltOrDbl())
                {
                    MakeSrcContained(tree, op2);
                }
                break;
            }
            __fallthrough;

        case GT_UMOD:
        case GT_UDIV:
        {
            info->srcCount = 2;
            info->dstCount = 1;

            op1 = tree->gtOp.gtOp1;
            op2 = tree->gtOp.gtOp2;

            // See if we have an optimizable power of 2 which will be expanded 
            // using instructions other than division.
            // (fgMorph has already done magic number transforms)

            if (op2->IsIntCnsFitsInI32())
            {
                bool isSigned = tree->OperGet() == GT_MOD || tree->OperGet() == GT_DIV;
                ssize_t amount = op2->gtIntConCommon.IconValue();

                if (isPow2(abs(amount)) && (isSigned || amount > 0)
                    && amount != -1)
                {
                    MakeSrcContained(tree, op2);
                    
                    if (isSigned)
                    {
                        // we are going to use CDQ instruction so want these RDX:RAX
                        info->setDstCandidates(l, RBM_RAX);
                        // If possible would like to have op1 in RAX to avoid a register move
                        op1->gtLsraInfo.setSrcCandidates(l, RBM_RAX);
                    }
                    break;
                }
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
            }
        }
        break;

        case GT_MUL:
        case GT_MULHI:
            SetMulOpCounts(tree);
            break;
        
        case GT_MATH:
            {
                // Both operand and its result must be of floating point type.
                op1 = tree->gtOp.gtOp1;
                assert(varTypeIsFloating(op1));
                assert(op1->TypeGet() == tree->TypeGet());

                info->srcCount = 1;
                info->dstCount = 1;

                switch(tree->gtMath.gtMathFN)
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
                         
                         // TODO-XArch-CQ: avoid using an internal register for the mask.
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
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            TreeNodeInfoInitSIMD(tree, l);
            break;
#endif // FEATURE_SIMD

        case GT_CAST:
            {
                // TODO-XArch-CQ: Int-To-Int conversions - castOp cannot be a memory op and must have an assigned register.
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
        {
            info->srcCount = 2;
            info->dstCount = 1;
            // For shift operations, we need that the number
            // of bits moved gets stored in CL in case 
            // the number of bits to shift is not a constant.
            GenTreePtr shiftBy = tree->gtOp.gtOp2;
            GenTreePtr source = tree->gtOp.gtOp1;

            // x64 can encode 8 bits of shift and it will use 5 or 6. (the others are masked off)
            // We will allow whatever can be encoded - hope you know what you are doing.
            if (!IsContainableImmed(tree, shiftBy)
                || shiftBy->gtIntConCommon.IconValue() > 255
                || shiftBy->gtIntConCommon.IconValue() < 0)
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

                // call can take a Rm op on x64
                info->srcCount++;               

                // In case of fast tail implemented as jmp, make sure that gtControlExpr is
                // computed into a register.
                if (!tree->gtCall.IsFastTailCall())
                {
                    if (ctrlExpr->isIndir())
                    {
                        MakeSrcContained(tree, ctrlExpr);
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
            if (tree->gtCall.IsVarargs())
            {
                tree->gtLsraInfo.setInternalCandidates(l, RBM_NONE);
            }

            // Set destination candidates for return value of the call.
            if (varTypeIsFloating(registerType))
            {
#ifdef _TARGET_X86_
                // The return value will be on the X87 stack, and we will need to move it.
                info->setDstCandidates(l, l->allRegs(registerType));
#else // !_TARGET_X86_
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
                        noway_assert(!"GT_LDOBJ not supported for amd64");
                    }
                    else
                    {
                        noway_assert(!"Can't predict unsupported TYP_STRUCT arg kind");
                    }

                    unsigned slots = ((unsigned)(roundUp(originalSize, TARGET_POINTER_SIZE))) / REGSIZE_BYTES;
                    regNumber reg = (regNumber)(argReg + 1);
                    unsigned remainingSlots = slots - 1;
                    while (remainingSlots > 0 && reg <= REG_ARG_LAST)
                    {
                        argMask |= genRegMask(reg);
                        reg = (regNumber)(reg + 1);
                        remainingSlots--;
                        regCount++;
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
                // In the case of a varargs call, the ABI dictates that if we have floating point args,
                // we must pass the enregistered arguments in both the integer and floating point registers.
                // Since the integer register is not associated with this arg node, we will reserve it as
                // an internal register so that it is not used during the evaluation of the call node
                // (e.g. for the target).
                if (tree->gtCall.IsVarargs() && varTypeIsFloating(argNode))
                {
                    regNumber targetReg = compiler->getCallArgIntRegister(argReg);
                    tree->gtLsraInfo.setInternalIntCount(tree->gtLsraInfo.internalIntCount + 1);
                    tree->gtLsraInfo.addInternalCandidates(l, genRegMask(targetReg));
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
                        // We don't want to make 0 contained, because we can generate smaller code
                        // by zeroing a register and then storing it.
                        argInfo->dstCount = 0;
                        if (arg->gtOper == GT_PUTARG_STK) 
                        {
                            op1 = arg->gtOp.gtOp1;
                            if (IsContainableImmed(arg, op1) && !op1->IsZero())
                            {
                                MakeSrcContained(arg, op1);
                            }
                        }
                    }
                }
                args = args->gtOp.gtOp2;
            }

            // If it is a fast tail call, it is already preferenced to use RAX.
            // Therefore, no need set src candidates on call tgt again.
            if (tree->gtCall.IsVarargs() && 
                callHasFloatRegArgs &&                 
                !tree->gtCall.IsFastTailCall() &&
                (ctrlExpr != nullptr))
            {
                // Don't assign the call target to any of the argument registers because
                // we will use them to also pass floating point arguments as required
                // by Amd64 ABI.
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

#ifdef _TARGET_X86_
        case GT_LDOBJ:
            NYI_X86("GT_LDOBJ");
#endif //_TARGET_X86_

        case GT_INITBLK:
        {
            // Sources are dest address, initVal and size
            info->srcCount = 3;
            info->dstCount = 0;

            GenTreeInitBlk* initBlkNode = tree->AsInitBlk();

            GenTreePtr blockSize = initBlkNode->Size();
            GenTreePtr   dstAddr = initBlkNode->Dest();
            GenTreePtr   initVal = initBlkNode->InitVal();

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

            if (blockSize->IsCnsIntOrI() && blockSize->gtIntCon.gtIconVal <= helperThreshold)
            {
                ssize_t size = blockSize->gtIntCon.gtIconVal;

                // Always favor unrolling vs rep stos.
                if (size <= INITBLK_UNROLL_LIMIT && initVal->IsCnsIntOrI())
                {
                    // Replace the integer constant in initVal 
                    // to fill an 8-byte word with the fill value of the InitBlk
                    assert(initVal->gtIntCon.gtIconVal == (initVal->gtIntCon.gtIconVal & 0xFF));
#ifdef _TARGET_AMD64_
                    if (size < REGSIZE_BYTES)
                    {
                        initVal->gtIntCon.gtIconVal = 0x01010101 * initVal->gtIntCon.gtIconVal;
                    }
                    else
                    {
                        initVal->gtIntCon.gtIconVal = 0x0101010101010101LL * initVal->gtIntCon.gtIconVal;
                        initVal->gtType = TYP_LONG;
                    }
#else // !_TARGET_AMD64_
                    initVal->gtIntCon.gtIconVal = 0x01010101 * initVal->gtIntCon.gtIconVal;
#endif // !_TARGET_AMD64_

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
#else // !_TARGET_AMD64_
                NYI("InitBlk helper call for RyuJIT/x86");
#endif // !_TARGET_AMD64_
            }
            break;
        }

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

            bool IsRepMovsProfitable = false;
            
            // If the destination is not on the stack, let's find out if we
            // can improve code size by using rep movsq instead of generating
            // sequences of movsq instructions.
            if (!dstAddr->OperIsLocalAddr())
            {
                // Let's inspect the struct/class layout and determine if it's profitable
                // to use rep movsq for copying non-gc memory instead of using single movsq 
                // instructions for each memory slot.
                unsigned i = 0;
                BYTE* gcPtrs = cpObjNode->gtGcPtrs;
                
                do {
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
                MakeSrcContained(tree, clsTok);
                info->internalIntCount = 1;
                info->setInternalCandidates(l, RBM_RCX);
            }
            else
            {
                // We don't need to materialize the struct size because we will unroll
                // the loop using movsq that automatically increments the pointers.
                MakeSrcContained(tree, clsTok);
            }

            dstAddr->gtLsraInfo.setSrcCandidates(l, RBM_RDI);
            srcAddr->gtLsraInfo.setSrcCandidates(l, RBM_RSI);
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

            // In case of a CpBlk with a constant size and less than CPBLK_MOVS_LIMIT size
            // we can use rep movs to generate code instead of the helper call.

            // This threshold will decide from using the helper or let the JIT decide to inline
            // a code sequence of its choice.
            ssize_t helperThreshold = max(CPBLK_MOVS_LIMIT, CPBLK_UNROLL_LIMIT);

            // TODO-X86-CQ: The helper call either is not supported on x86 or required more work
            // (I don't know which).
#ifdef _TARGET_AMD64_
            if (blockSize->IsCnsIntOrI() && blockSize->gtIntCon.gtIconVal <= helperThreshold)
#endif // _TARGET_AMD64_
            {
                assert(!blockSize->IsIconHandle());
                ssize_t size = blockSize->gtIntCon.gtIconVal;

                // If we have a buffer between XMM_REGSIZE_BYTES and CPBLK_UNROLL_LIMIT bytes, we'll use SSE2. 
                // Structs and buffer with sizes <= CPBLK_UNROLL_LIMIT bytes are occurring in more than 95% of
                // our framework assemblies, so this is the main code generation scheme we'll use.
                if (size <= CPBLK_UNROLL_LIMIT)
                {
                    MakeSrcContained(tree, blockSize);
                    
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
#endif // _TARGET_AMD64_
        }
        break;

        case GT_LCLHEAP:
        {
            info->srcCount = 1;
            info->dstCount = 1;

            // Need a variable number of temp regs (see genLclHeap() in codegenamd64.cpp):
            // Here '-' means don't care.
            //
            //     Size?                    Init Memory?         # temp regs
            //      0                            -                  0
            //      const and <=6 ptr words      -                  0
            //      const and <PageSize          No                 0
            //      >6 ptr words                 Yes                hasPspSym ? 1 : 0
            //      Non-const                    Yes                hasPspSym ? 1 : 0
            //      Non-const                    No                 2            
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

                    // For small allocations upto 6 pointer sized words (i.e. 48 bytes of localloc)
                    // we will generate 'push 0'.
                    if (cntStackAlignedWidthItems <= 6)
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
                        // >6 and need to zero initialize allocated stack space.
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

            if (other->isMemoryOp())
            {
                MakeSrcContained(tree, other);
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

            // If the source is a containable immediate, make it contained, unless it is
            // an int-size or larger store of zero to memory, because we can generate smaller code
            // by zeroing a register and then storing it.
            if (IsContainableImmed(tree, src) &&
                (!src->IsZero() || varTypeIsSmall(tree) || tree->gtGetOp1()->OperGet() == GT_CLS_VAR_ADDR))
            {
                MakeSrcContained(tree, src);
            }

            // Perform recognition of trees with the following structure:
            // StoreInd(IndA, BinOp(expr, IndA))
            // to be able to fold this into an instruction of the form
            // BINOP [addressing mode for IndA], register
            // where register is the actual place where 'expr'
            // is computed.
            //
            // SSE2 doesn't support RMW form of instructions.
            if (!varTypeIsFloating(tree) && LowerStoreInd(tree))
                break;

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

#if !FEATURE_EH_FUNCLETS
        case GT_END_LFIN:
            NYI_X86("Implement GT_END_LFIN for x86");
#endif

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

        if (tree->OperIsBinary() && info->srcCount >= 2)
        {
            if (isRMWRegOper(tree))
            {
                GenTree* op1 = tree->gtOp.gtOp1;
                GenTree* op2 = tree->gtOp.gtOp2;

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
                        GenTree* base = delayUseSrc->AsIndir()->Base();
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
        // Though this looks conservative in theory, in practice we could not think of a case where
        // the below logic leads to conservative register specification.  In future when or if we find
        // one such case, this logic needs to be fine tuned for that case(s).
        if (varTypeIsByte(tree) || ((tree->OperGet() == GT_CAST) && varTypeIsByte(tree->CastToType())))
        {
            regMaskTP regMask;
            if (info->dstCount > 0)
            {
                regMask = info->getDstCandidates(l);
                assert(regMask != RBM_NONE);
                info->setDstCandidates(l, regMask & ~RBM_NON_BYTE_REGS);
            }

            if (info->srcCount > 0)
            {
                // No need to set src candidates on a contained child operand.
                GenTree *op = tree->gtOp.gtOp1;
                assert(op != nullptr);
                bool containedNode = (op->gtLsraInfo.srcCount == 0) && (op->gtLsraInfo.dstCount == 0);
                if (!containedNode)
                {
                    regMask = op->gtLsraInfo.getSrcCandidates(l);
                    assert(regMask != RBM_NONE);
                    op->gtLsraInfo.setSrcCandidates(l, regMask & ~RBM_NON_BYTE_REGS);
                }

                op = tree->gtOp.gtOp2;
                if (op != nullptr)
                {
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
    GenTreeSIMD* simdTree = tree->AsSIMD();
    TreeNodeInfo* info = &(tree->gtLsraInfo);
    info->dstCount = 1;
    switch(simdTree->gtSIMDIntrinsicID)
    {
        GenTree* op2;

    case SIMDIntrinsicInit:
        {
            info->srcCount = 1;
            GenTree* op1 = tree->gtOp.gtOp1;

            // This sets all fields of a SIMD struct to the given value.
            // Mark op1 as contained if it is either zero or int constant of all 1's,
            // or a float constant with 16 or 32 byte simdType (AVX case)
            //
            // Should never see small int base type vectors except for zero initialization.
            assert(!varTypeIsSmallInt(simdTree->gtSIMDBaseType) || op1->IsZero());

            if (op1->IsZero() || 
                (simdTree->gtSIMDBaseType == TYP_INT && op1->IsCnsIntOrI() && op1->AsIntConCommon()->IconValue() == 0xffffffff) ||
                (simdTree->gtSIMDBaseType == TYP_LONG && op1->IsCnsIntOrI() && op1->AsIntConCommon()->IconValue() == 0xffffffffffffffffLL)
               )
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
        CheckImmedAndMakeContained(tree,  tree->gtGetOp2());
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
        if ((comp->getSIMDInstructionSet() == InstructionSet_SSE2) || (simdTree->gtOp.gtOp1->TypeGet() == TYP_SIMD32))
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
        op2 = tree->gtOp.gtOp2;

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
            (void) comp->getSIMDInitTempVarNum();
        }
        else if (!varTypeIsFloating(simdTree->gtSIMDBaseType))
        {
            bool needFloatTemp;
            if (varTypeIsSmallInt(simdTree->gtSIMDBaseType) && (comp->getSIMDInstructionSet() == InstructionSet_AVX))
            {
                int byteShiftCnt = (int) op2->AsIntCon()->gtIconVal * genTypeSize(simdTree->gtSIMDBaseType);
                needFloatTemp = (byteShiftCnt >= 16);
            }
            else
            {
                needFloatTemp = !op2->IsZero();
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
        info->srcCount = 2;
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

    // !!! This code was leveraged from codegen.cpp
#if NOGC_WRITE_BARRIERS
#ifdef _TARGET_AMD64_
#error "NOGC_WRITE_BARRIERS is not supported for _TARGET_AMD64"
#else // !_TARGET_AMD64_
    NYI("NYI: NOGC_WRITE_BARRIERS for RyuJIT/x86");
#endif // !_TARGET_AMD64_
#endif // NOGC_WRITE_BARRIERS
    // For the standard JIT Helper calls
    // op1 goes into REG_ARG_0 and
    // op2 goes into REG_ARG_1
    // Set this RefPosition, and the previous one, to the physical
    // register instead of a virtual one
    //
    addr->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_0);
    src->gtLsraInfo.setSrcCandidates(m_lsra, RBM_ARG_1);
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

    // If indirTree is of TYP_SIMD12, don't mark addr as contained
    // so that it always get computed to a register.  This would
    // mean codegen side logic doesn't need to handle all possible
    // addr expressions that could be contained.
    // 
    // TODO-XArch-CQ: handle other addr mode expressions that could be marked
    // as contained.
#ifdef FEATURE_SIMD
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

        return ;
    }
#endif //FEATURE_SIMD

    // These nodes go into an addr mode:
    // - GT_CLS_VAR_ADDR turns into a constant.
    // - GT_LCL_VAR_ADDR is a stack addr mode.
    if ((addr->OperGet() == GT_CLS_VAR_ADDR) || (addr->OperGet() == GT_LCL_VAR_ADDR))
    {
        // make this contained, it turns into a constant that goes into an addr mode
        MakeSrcContained(indirTree, addr);
    }

    // TODO-XArch-CQ: The below condition is incorrect and need to be revisited for the following reasons:  
    // a) FitsInAddrBase() already checks for opts.compReloc and
    // b) opts.compReloc is set only during Ngen.  
    // c) During lowering we should not be checking gtRegNum
    // For the above reasons this condition will never be true and indir of absolute addresses
    // that can be encoded as PC-relative 32-bit offset are never marked as contained.
    // 
    // The right condition to check probably here is
    // "addr->IsCnsIntOrI() && comp->codeGen->genAddrShouldUsePCRel(addr->AsIntConCommon()->IconValue())"
    //
    // Apart from making this change, codegen side changes are needed to handle contained addr
    // where GT_IND is possible as an operand.
    else if (addr->IsCnsIntOrI() &&
             addr->AsIntConCommon()->FitsInAddrBase(comp) &&
             comp->opts.compReloc &&
             (addr->gtRegNum != REG_NA))
    {
        MakeSrcContained(indirTree, addr);
    }
    else if (addr->OperGet() == GT_LEA)
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

#ifdef _TARGET_X86_
    info->setDstCandidates(m_lsra, RBM_BYTE_REGS);
#endif // _TARGET_X86_

    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtOp.gtOp2;
    var_types op1Type = op1->TypeGet();
    var_types op2Type = op2->TypeGet();

#if !defined(_TARGET_64BIT_)
    // Long compares will consume GT_LONG nodes, each of which produces two results.
    // Thus for each long operand there will be an additional source.
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
        else if (otherOp->isMemoryOp())
        {
            if ((otherOp == op2) || IsSafeToContainMem(tree, otherOp)) 
            {
                MakeSrcContained(tree, otherOp);
            }
        }

        return;
    }

    // TODO-XArch-CQ: factor out cmp optimization in 'genCondSetFlags' to be used here
    // or in other backend.
    
    bool hasShortCast = false;
    if (CheckImmedAndMakeContained(tree, op2))
    {
        bool op1CanBeContained  = (op1Type == op2Type);
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

            bool    isEqualityCompare = (tree->gtOper == GT_EQ || tree->gtOper == GT_NE);
            bool    useTest           = isEqualityCompare && (ival == 0);

            if (!useTest)
            {
                ssize_t lo = 0;  // minimum imm value allowed for cmp reg,imm
                ssize_t hi = 0;  // maximum imm value allowed for cmp reg,imm
                bool    isUnsigned = false;

                switch (op1Type) {
                case TYP_BOOL:
                    op1Type = TYP_UBYTE;
                    __fallthrough;
                case TYP_UBYTE:
                    lo = 0;
                    hi = 0x7f;
                    isUnsigned = true;
                    break;
                case TYP_BYTE:
                    lo = -0x80;
                    hi =  0x7f;
                    break;
                case TYP_CHAR:
                    lo = 0;
                    hi = 0x7fff;
                    isUnsigned = true;
                    break;
                case TYP_SHORT:
                    lo = -0x8000;
                    hi =  0x7fff;
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
                                // Mark the 'andOp1' memory operand as contained
                                // Note that for equality comparisons we don't need
                                // to deal with any signed or unsigned issues.
                                MakeSrcContained(op1, andOp1);
                            }
                            // Mark the 'op1' (the GT_AND) operand as contained
                            MakeSrcContained(tree, op1);

                            // During Codegen we will now generate "test andOp1, andOp2CnsVal"
                        }
                    }
                }
                else if (op1->OperGet() == GT_CAST)
                {
                    //If the op1 is a cast operation, and cast type is one byte sized unsigned type, 
                    //we can directly use the number in register, instead of doing an extra cast step.
                    var_types   dstType       = op1->CastToType();
                    bool        isUnsignedDst = varTypeIsUnsigned(dstType);
                    emitAttr    castSize      = EA_ATTR(genTypeSize(dstType));
                    GenTreePtr  castOp1       = op1->gtOp.gtOp1;
                    genTreeOps  castOp1Oper   = castOp1->OperGet();
                    bool        safeOper      = false;

                    // It is not always safe to change the gtType of 'castOp1' to TYP_UBYTE
                    // For example when 'castOp1Oper' is a GT_RSZ or GT_RSH then we are shifting
                    // bits from the left into the lower bits.  If we change the type to a TYP_UBYTE
                    // we will instead generate a byte sized shift operation:  shr  al, 24
                    // For the following ALU operations is it safe to change the gtType to the
                    // smaller type:   
                    //
                    if ((castOp1Oper == GT_CNS_INT) || 
                        (castOp1Oper == GT_CALL)    ||    // the return value from a Call
                        (castOp1Oper == GT_LCL_VAR) ||
                        castOp1->OperIsLogical()    ||    // GT_AND, GT_OR, GT_XOR
                        castOp1->isMemoryOp()         )   // isIndir() || isLclField();
                    {
                        safeOper = true;
                    }

                    if ((castSize == EA_1BYTE) && isUnsignedDst &&    // Unsigned cast to TYP_UBYTE
                        safeOper &&                                   // Must be a safe operation
                        !op1->gtOverflow()                         )  // Must not be an overflow checking cast
                    {
                        // Currently all of the Oper accepted as 'safeOper' are 
                        // non-overflow checking operations.  If we were to add 
                        // an overflow checking operation then this assert needs 
                        // to be moved above to guard entry to this block.
                        // 
                        assert(!castOp1->gtOverflowEx());             // Must not be an overflow checking operation
                        
                        GenTreePtr removeTreeNode = op1;
                        GenTreePtr removeTreeNodeChild = castOp1;
                        tree->gtOp.gtOp1 = castOp1;
                        castOp1->gtType = TYP_UBYTE;

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
                                op2->gtType    = TYP_UBYTE;
                                tree->gtFlags |= GTF_UNSIGNED;
                                
                                //right now the op1's type is the same as op2's type.
                                //if op1 is MemoryOp, we should make the op1 as contained node.
                                if (castOp1->isMemoryOp())
                                {
                                    MakeSrcContained(tree, op1);
                                }
                            }
                        }
                        comp->fgSnipNode(comp->compCurStmt->AsStmt(), removeTreeNode);
#ifdef DEBUG
                        if (comp->verbose)
                        {
                            printf("LowerCmp: Removing a GT_CAST to TYP_UBYTE and changing castOp1->gtType to TYP_UBYTE\n");
                            comp->gtDispTree(tree);
                        }
#endif
                    }
                }
            }
        }
    }
    else if (op2->isMemoryOp())
    {
        if (op1Type == op2Type)
        {
            MakeSrcContained(tree, op2);

            // Mark the tree as doing unsigned comparison if
            // both the operands are small and unsigned types.
            // Otherwise we will end up performing a signed comparison
            // of two small unsigned values without zero extending them to
            // TYP_INT size and which is incorrect.
            if (varTypeIsSmall(op1Type) && varTypeIsUnsigned(op1Type))
            {
                tree->gtFlags |= GTF_UNSIGNED;
            }
        }
    }
    else if (op1->isMemoryOp()) 
    {
        if ((op1Type == op2Type) && IsSafeToContainMem(tree, op1))
        {
            MakeSrcContained(tree, op1);

            // Mark the tree as doing unsigned comparison if
            // both the operands are small and unsigned types.
            // Otherwise we will end up performing a signed comparison
            // of two small unsigned values without zero extending them to
            // TYP_INT size and which is incorrect.
            if (varTypeIsSmall(op1Type) && varTypeIsUnsigned(op1Type))
            {
                tree->gtFlags |= GTF_UNSIGNED;
            }
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
void Lowering::LowerCast( GenTreePtr* ppTree) 
{
    GenTreePtr  tree    = *ppTree;
    assert(tree->OperGet() == GT_CAST);

    GenTreePtr  op1     = tree->gtOp.gtOp1;
    var_types   dstType = tree->CastToType();
    var_types   srcType = op1->TypeGet();
    var_types   tmpType = TYP_UNDEF;
    bool        srcUns  = false;

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
        tmp->gtFlags |= (tree->gtFlags & (GTF_UNSIGNED|GTF_OVERFLOW|GTF_EXCEPT));

        tree->gtFlags &= ~GTF_UNSIGNED;
        tree->gtOp.gtOp1 = tmp;
        op1->InsertAfterSelf(tmp);
    }
}

/** Lower StoreInd takes care of recognizing the cases where we have a treeNode with the following
 * structure:
 * storeInd(gtInd(subTreeA), binOp(gtInd(subTreeA), subtreeB) or
 * storeInd(gtInd(subTreeA), binOp(subtreeB, gtInd(subTreeA)) for the case of commutative 
 * operations.
 *
 * In x86/x64 this storeInd pattern can be effectively encoded in a single instruction of the 
 * form in case of integer operations:
 * binOp [addressing mode], regSubTreeB
 * where regSubTreeB is the register where subTreeB was computed.
 *
 * If the recognition is successful, we mark all the nodes under the storeInd node as contained so codeGen 
 * will generate the single instruction discussed above.
 *
 * Right now, we recognize few cases:
 *     a) The gtIndir child is a lclVar
 *     b) A constant 
 *     c) An lea.
 *     d) BinOp is either add, sub, xor, or, and, shl, rsh, rsz.
 *     
 *  TODO-CQ: Enable support for more complex indirections (if needed) or use the value numbering
 *  package to perform more complex tree recognition.
 *
 *  TODO-XArch-CQ: Add support for RMW of lcl fields (e.g. lclfield binop= source)
 *
 * Return value:  In case we recognize the tree pattern, we return true to specify lower we're
 *                finished and no further code needs to be run in order to lower this type of node.
 */
bool Lowering::LowerStoreInd(GenTreePtr tree)
{
    assert(tree->OperGet() == GT_STOREIND);

    // SSE2 doesn't support RMW operations on float/double types.
    assert(!varTypeIsFloating(tree));

    GenTreePtr indirDst = tree->gtGetOp1();
    GenTreePtr indirSrc = tree->gtGetOp2();

    const genTreeOps oper = indirSrc->OperGet();

    if (indirDst->OperGet() != GT_LEA &&
        indirDst->OperGet() != GT_LCL_VAR &&
        indirDst->OperGet() != GT_LCL_VAR_ADDR &&
        indirDst->OperGet() != GT_CLS_VAR_ADDR)
    {
        JITDUMP("Lower of StoreInd didn't mark the node as self contained\n");
        JITDUMP("because the type of indirection in the left hand side \n");
        JITDUMP("is not yet supported:\n");
        DISPTREE(indirDst);
        return false;
    }

    if (GenTree::OperIsBinary(oper))
    {
        if (indirSrc->gtOverflowEx())
        {
            // We can not use Read-Modify-Write instruction forms with overflow checking instructions
            // because we are not allowed to modify the target until after the overflow check.
            // 
            JITDUMP("Lower of StoreInd cannot lower overflow checking instructions into RMW forms\n");
            DISPTREE(indirDst);
            return false;
        }

        if (oper != GT_ADD &&
            oper != GT_SUB &&
            oper != GT_AND &&
            oper != GT_OR  &&
            oper != GT_XOR &&
            oper != GT_LSH &&
            oper != GT_RSH &&
            oper != GT_RSZ)
        {
            JITDUMP("Lower of StoreInd didn't mark the node as self contained\n");
            JITDUMP("because the node operator not yet supported:\n");
            DISPTREE(indirSrc);
            return false;
        }

        if ((oper == GT_LSH ||
             oper == GT_RSH ||
             oper == GT_RSZ) &&
            varTypeIsSmall(tree))
        {
            //In ldind, Integer values smaller than 4 bytes, a boolean, or a character converted to 4 bytes by sign or zero-extension as appropriate.
            //If directly shift the short type data using sar, we will lose the sign or zero-extension bits. This will generate the wrong code.
            return false;
        }

        GenTreePtr rhsLeft = indirSrc->gtGetOp1();
        GenTreePtr rhsRight = indirSrc->gtGetOp2();

        GenTreePtr indirCandidate = nullptr;
        GenTreePtr indirOpSource = nullptr;

        if (rhsLeft->OperGet() == GT_IND &&
            rhsLeft->gtGetOp1()->OperGet() == indirDst->OperGet() &&
            IsSafeToContainMem(indirSrc, rhsLeft))
        {
            indirCandidate = rhsLeft;
            indirOpSource = rhsRight;
        } 
        else if (GenTree::OperIsCommutative(oper) &&
                 rhsRight->OperGet() == GT_IND &&
                 rhsRight->gtGetOp1()->OperGet() == indirDst->OperGet())
        {
            indirCandidate = rhsRight;
            indirOpSource = rhsLeft;
        }

        if (indirCandidate == nullptr &&
            indirOpSource == nullptr)
        {
            JITDUMP("Lower of StoreInd didn't mark the node as self contained\n");
            JITDUMP("because the indirections don't match or the operator is not commutative\n");
            DISPTREE(tree);
            return false;
        }

        if (IndirsAreEquivalent(indirCandidate, tree))
        {
            JITDUMP("Lower succesfully detected an assignment of the form: *addrMode BinOp= source\n");
            tree->gtLsraInfo.srcCount = indirOpSource->gtLsraInfo.dstCount;
            SetStoreIndOpCounts(tree, indirCandidate);
            return true;
        }
        else 
        {
            JITDUMP("Lower of StoreInd didn't mark the node as self contained\n");
            JITDUMP("because the indirections are not equivalent.\n");
            DISPTREE(tree);
            return false;
        }
    } 
    else if (GenTree::OperIsUnary(oper))
    {
        // Nodes other than GT_NOT and GT_NEG are not yet supported
        // so we bail for now.
        if (oper != GT_NOT && oper != GT_NEG)
            return false;

        // If the operand of the GT_NOT | GT_NEG is not an indirection,
        // then this is not a RMW pattern.
        if (indirSrc->gtGetOp1()->OperGet() != GT_IND)
            return false;

        // We have a GT_IND below the NOT/NEG, so we attempt to recognize
        // the RMW pattern.
        GenTreePtr indirCandidate = indirSrc->gtGetOp1();
        if (IndirsAreEquivalent(indirCandidate, tree))
        {
            JITDUMP("Lower succesfully detected an assignment of the form: *addrMode = UnaryOp(*addrMode)\n");
            tree->gtLsraInfo.srcCount = 0;
            SetStoreIndOpCounts(tree, indirCandidate);
            return true;
        }
        else 
        {
            JITDUMP("Lower of StoreInd didn't mark the node as self contained\n");
            JITDUMP("because the indirections are not equivalent.\n");
            DISPTREE(tree);
            return false;
        }
    }
    else
    {
        JITDUMP("Lower of StoreInd didn't mark the node as self contained\n");
        JITDUMP("because the operator on the right hand side of the indirection is not\n");
        JITDUMP("a binary or unary operator.\n");
        DISPTREE(tree);
        return false;
    }
}

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
        assert(indirCandidateChild->OperGet() == GT_LCL_VAR || indirCandidateChild->OperGet() == GT_CLS_VAR_ADDR);
        info->srcCount += indirCandidateChild->gtLsraInfo.dstCount;
        // If it is a GT_LCL_VAR, it still needs the reg to hold the address. 
        // However for GT_CLS_VAR_ADDR, we don't need that reg to hold the address, because field address value is known at this time.
        if(indirCandidateChild->OperGet() == GT_CLS_VAR_ADDR)
        {
            m_lsra->clearOperandCounts(indirDst);
        }
    }
    m_lsra->clearOperandCounts(indirCandidateChild);
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
        if (op2->isMemoryOp() || op2->IsCnsNonZeroFltOrDbl())
        {
            MakeSrcContained(tree, op2);
        }
        return;
    }
    
    bool isUnsignedMultiply    = ((tree->gtFlags & GTF_UNSIGNED) != 0);
    bool requiresOverflowCheck = tree->gtOverflowEx();
    bool useLeaEncoding = false;
    GenTreePtr memOp = nullptr;

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
        info->setDstCandidates(m_lsra,RBM_RAX);
    }
    else if (tree->gtOper == GT_MULHI)
    {
        // have to use the encoding:RDX:RAX = RAX * rm
        info->setDstCandidates(m_lsra, RBM_RAX);
    }
    else if (IsContainableImmed(tree, op2) || IsContainableImmed(tree, op1))
    {
        GenTreeIntConCommon* imm;
        GenTreePtr other;

        if (IsContainableImmed(tree, op2))
        { 
            imm = op2->AsIntConCommon();
            other = op1; 
        }
        else
        { 
            imm = op1->AsIntConCommon();
            other = op2; 
        }

        // CQ: We want to rewrite this into a LEA
        ssize_t immVal = imm->AsIntConCommon()->IconValue();
        if (!requiresOverflowCheck && (immVal == 3 || immVal == 5 || immVal == 9))
        {
            useLeaEncoding = true;
        }

        MakeSrcContained(tree, imm);   // The imm is always contained
        if (other->isIndir())
        {
            memOp = other;             // memOp may be contained below
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
    if ((memOp != nullptr)                    &&
        !useLeaEncoding                       &&
        (memOp->TypeGet() == tree->TypeGet()) &&
        IsSafeToContainMem(tree, memOp))
    {
        MakeSrcContained(tree, memOp);
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
        return false;

    // x86/x64 does support a three op multiply when op2|op1 is a contained immediate
    if ((tree->OperGet() == GT_MUL) &&
        (Lowering::IsContainableImmed(tree, tree->gtOp.gtOp2) ||
        Lowering::IsContainableImmed(tree, tree->gtOp.gtOp1)))
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
bool Lowering:: IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (!childNode->IsIntCnsFitsInI32())
        return false;
    if (childNode->IsIconHandle() && comp->opts.compReloc)
        return false;

    return true;
}

#endif // _TARGET_AMD64_

#endif // !LEGACY_BACKEND
