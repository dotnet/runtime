// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        PPC64LE Code Generator Common Code                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_POWERPC64 // This file is ONLY used for POWERPC64 architecture

#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "emit.h"
#include "patchpointinfo.h"

// TODO POWERPC64


//------------------------------------------------------------------------
// genStackPointerConstantAdjustment: add a specified constant value to the stack pointer.
// No probe is done.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative or zero.
//    regTmp                  - an available temporary register that is used if 'spDelta' cannot be encoded by
//                              'sub sp, sp, #spDelta' instruction.
//                              Can be REG_NA if the caller knows for certain that 'spDelta' fits into the immediate
//                              value range.
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerConstantAdjustment(ssize_t spDelta, regNumber regTmp)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genStackPointerConstantAdjustmentWithProbe: add a specified constant value to the stack pointer,
// and probe the stack as appropriate. Should only be called as a helper for
// genStackPointerConstantAdjustmentLoopWithProbe.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative or zero. If zero, the probe happens,
//                              but the stack pointer doesn't move.
//    regTmp                  - temporary register to use as target for probe load instruction
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerConstantAdjustmentWithProbe(ssize_t spDelta, regNumber regTmp)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genStackPointerConstantAdjustmentLoopWithProbe: Add a specified constant value to the stack pointer,
// and probe the stack as appropriate. Generates one probe per page, up to the total amount required.
// This will generate a sequence of probes in-line.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative.
//    regTmp                  - temporary register to use as target for probe load instruction
//
// Return Value:
//    Offset in bytes from SP to last probed address.
//
target_ssize_t CodeGen::genStackPointerConstantAdjustmentLoopWithProbe(ssize_t spDelta, regNumber regTmp)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genSetRegToConst: Generate code to set a register 'targetReg' of type 'targetType'
//    to the constant specified by the constant (GT_CNS_INT or GT_CNS_DBL) in 'tree'.
//
// Notes:
//    This does not call genProduceReg() on the target register.
//
void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, GenTree* tree)
{
    switch (tree->gtOper)
    {
        case GT_CNS_INT:
	{
            // relocatable values tend to come down as a CNS_INT of native int type
            // so the line between these two opcodes is kind of blurry
            GenTreeIntConCommon* con    = tree->AsIntConCommon();
            ssize_t              cnsVal = con->IconValue();

            emitAttr attr = emitActualTypeSize(targetType);

            // TODO-CQ: Currently we cannot do this for all handles because of
            // https://github.com/dotnet/runtime/issues/60712
            if (con->ImmedValNeedsReloc(compiler))
            {
                attr = EA_SET_FLG(attr, EA_CNS_RELOC_FLG);
            }

            if (targetType == TYP_BYREF)
            {
                attr = EA_SET_FLG(attr, EA_BYREF_FLG);
            }

            instGen_Set_Reg_To_Imm(attr, targetReg, cnsVal);
            regSet.verifyRegUsed(targetReg);
        }
        break;

	default:
	    abort();
    }
}

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_EQ/GT_NE/GT_LT/GT_LE/GT_GE/GT_GT/GT_CMP node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForCompare(GenTreeOp* tree)
{
    regNumber targetReg = tree->GetRegNum();
    emitter*  emit      = GetEmitter();

    GenTree*  op1     = tree->gtOp1;
    GenTree*  op2     = tree->gtOp2;
    var_types op1Type = genActualType(op1->TypeGet());
    var_types op2Type = genActualType(op2->TypeGet());
    instruction ins;

    assert(!op1->isUsedFromMemory());

    emitAttr cmpSize = EA_ATTR(genTypeSize(op1Type));

    assert(genTypeSize(op1Type) == genTypeSize(op2Type));

    if (varTypeIsFloating(op1Type))
    {
	abort();
    }
    else
    {
	assert(!varTypeIsFloating(op2Type));
	assert(!op1->isContainedIntOrIImmed());
	
	if (op2->IsCnsIntOrI())
	{
	    GenTreeIntConCommon* intConst = op2->AsIntConCommon();
	    ins  = (cmpSize == EA_8BYTE) ? INS_cmpdi : INS_cmpwi;
	    emit->emitIns_R_I(ins, cmpSize, op1->GetRegNum(), intConst->IconValue());
	}
	else
	{
	    ins = (cmpSize == EA_8BYTE) ? INS_cmpd : INS_cmpw;
	    emit->emitIns_R_R(ins, cmpSize, op1->GetRegNum(), op2->GetRegNum());
	}
    }

    if (targetReg != REG_NA)
    {
	// Need to materialize the comparison result into a register
        // Use inst_SETCC to convert CR0 flags to 0 or 1
        GenCondition condition = GenCondition::FromIntegralRelop(tree);
        inst_SETCC(condition, tree->TypeGet(), targetReg);
        genProduceReg(tree);
    }

}

//------------------------------------------------------------------------
// genCodeForTreeNode Generate code for a single node in the tree.
//
// Preconditions:
//    All operands have been evaluated.
//
void CodeGen::genCodeForTreeNode(GenTree* treeNode)
{
    regNumber targetReg  = treeNode->GetRegNum();
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = GetEmitter();

#ifdef DEBUG
    // Validate that all the operands for the current node are consumed in order.
    // This is important because LSRA ensures that any necessary copies will be
    // handled correctly.
    lastConsumedNode = nullptr;
    if (compiler->verbose)
    {
        unsigned seqNum = treeNode->gtSeqNum; // Useful for setting a conditional break in Visual Studio
        compiler->gtDispLIRNode(treeNode, "Generating: ");
    }
#endif // DEBUG

    // Is this a node whose value is already in a register?  LSRA denotes this by
    // setting the GTF_REUSE_REG_VAL flag.
    if (treeNode->IsReuseRegVal())
    {
        genCodeForReuseVal(treeNode);
        return;
    }

    // contained nodes are part of their parents for codegen purposes
    // ex : immediates, most LEAs
    if (treeNode->isContained())
    {
        return;
    }

    switch (treeNode->gtOper)
    {
	case GT_NOP:
	    break;

	case GT_CNS_INT:
	    genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
	    break;

	case GT_IND:
	    genCodeForIndir(treeNode->AsIndir());
	    break;

	case GT_CMP:
	case GT_EQ:
	case GT_NE:
	case GT_LT:
	case GT_LE:
	case GT_GE:
	case GT_GT:
	    genConsumeOperands(treeNode->AsOp());
	    genCodeForCompare(treeNode->AsOp());
	    break;

	case GT_JCC:
	    genCodeForJcc(treeNode->AsCC());
            break;

	case GT_CALL:
	    genCall(treeNode->AsCall());
            break;

	case GT_IL_OFFSET:
            // Do nothing; these nodes are simply markers for debug info.
            break;

	case GT_NO_OP:
            instGen(INS_nop);
            break;

	case GT_STORE_LCL_VAR:
            genCodeForStoreLclVar(treeNode->AsLclVar());
            break;

	case GT_LCL_VAR:
	    genCodeForLclVar(treeNode->AsLclVar());
	    break;

	case GT_RETFILT:
	case GT_RETURN:
	    genReturn(treeNode);
	    break;

        default:
	    printf("ERROR: Unhandled tree node operation: %s (oper=%d)\n",
                   GenTree::OpName(treeNode->gtOper), treeNode->gtOper);
            printf("Tree node details: type=%s, flags=0x%x\n",
                   varTypeName(treeNode->TypeGet()), treeNode->gtFlags);
	    abort();
    }
}

//---------------------------------------------------------------------
// genSetGSSecurityCookie: Set the "GS" security cookie in the prolog.
//
// Arguments:
//     initReg        - register to use as a scratch register
//     pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'false' if and only if
//                      this call sets 'initReg' to a non-zero value.
//
// Return Value:
//     None
//
void CodeGen::genSetGSSecurityCookie(regNumber initReg, bool* pInitRegZeroed)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genEmitGSCookieCheck: Generate code to check that the GS cookie
// wasn't thrashed by a buffer overrun.
//
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genIntrinsic - generate code for a given intrinsic
//
// Arguments
//    treeNode - the GT_INTRINSIC node
//
// Return value:
//    None
//
void CodeGen::genIntrinsic(GenTreeIntrinsic* treeNode)
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genPutArgStk - generate code for a GT_PUTARG_STK node
//
// Arguments
//    treeNode - the GT_PUTARG_STK node
//
// Return value:
//    None
//
void CodeGen::genPutArgStk(GenTreePutArgStk* treeNode)
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genPutArgReg - generate code for a GT_PUTARG_REG node
//
// Arguments
//    tree - the GT_PUTARG_REG node
//
// Return value:
//    None
//
void CodeGen::genPutArgReg(GenTreeOp* tree)
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genPutArgSplit - generate code for a GT_PUTARG_SPLIT node
//
// Arguments
//    tree - the GT_PUTARG_SPLIT node
//
// Return value:
//    None
//
void CodeGen::genPutArgSplit(GenTreePutArgSplit* treeNode)
{
    //_ASSERTE("!NYI");
    abort();
}

#ifdef FEATURE_SIMD
//----------------------------------------------------------------------------------
// genMultiRegStoreToSIMDLocal: store multi-reg value to a single-reg SIMD local
//
// Arguments:
//    lclNode  -  GentreeLclVar of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
void CodeGen::genMultiRegStoreToSIMDLocal(GenTreeLclVar* lclNode)
{

    //_ASSERTE("!NYI");
    abort();
}

#endif // FEATURE_SIMD

//------------------------------------------------------------------------
// genCodeForStoreLclVar: Produce code for a GT_STORE_LCL_VAR node.
//
// Arguments:
//    lclNode - the GT_STORE_LCL_VAR node
//
void CodeGen::genCodeForStoreLclVar(GenTreeLclVar* lclNode)
{
    GenTree* data = lclNode->gtOp1;

    // Stores from a multi-reg source are handled separately.
    if (data->gtSkipReloadOrCopy()->IsMultiRegNode())
    {
        genMultiRegStoreToLocal(lclNode);
        return;
    }

    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNode);
    if (lclNode->IsMultiReg())
    {
        // This is the case of storing to a multi-reg HFA local from a fixed-size SIMD type.
        // Note: PPC64LE may not support HFA in the same way as ARM64, but keeping structure similar
        assert(varTypeIsSIMD(data) && varDsc->lvIsHfa());
        regNumber    operandReg = genConsumeReg(data);
        unsigned int regCount   = varDsc->lvFieldCnt;
        for (unsigned i = 0; i < regCount; ++i)
        {
            regNumber varReg = lclNode->GetRegByIndex(i);
            assert(varReg != REG_NA);
            unsigned   fieldLclNum = varDsc->lvFieldLclStart + i;
            LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(fieldLclNum);
            // TODO-PPC64LE: Implement appropriate vector element extraction for PPC64LE
            // This may require different instructions than ARM64's INS_dup
            //NYI_PPC64("genCodeForStoreLclVar - multi-reg HFA store");
            abort();
        }
        genProduceReg(lclNode);
    }
    else
    {
        regNumber targetReg = lclNode->GetRegNum();
        emitter*  emit      = GetEmitter();

        unsigned  varNum     = lclNode->GetLclNum();
        var_types targetType = varDsc->GetRegisterType(lclNode);

#ifdef FEATURE_SIMD
        // storing of TYP_SIMD12 (i.e. Vector3) field
        if (targetType == TYP_SIMD12)
        {
            genStoreLclTypeSimd12(lclNode);
            return;
        }
#endif // FEATURE_SIMD

        genConsumeRegs(data);

        regNumber dataReg = REG_NA;
        if (data->isContained())
        {
            // This is only possible for a zero-init or bitcast.
            const bool zeroInit = (data->IsIntegralConst(0) || data->IsVectorZero());
            assert(zeroInit || data->OperIs(GT_BITCAST));

            if (zeroInit && varTypeIsSIMD(targetType))
            {
                if (targetReg != REG_NA)
                {
                    // TODO-PPC64LE: Implement SIMD zero initialization for PPC64LE
                    // This may use vector instructions like xxlxor or similar
                    //NYI_PPC64("genCodeForStoreLclVar - SIMD zero init to register");
                    abort();
                }
                else
                {
                    // Store zero to stack-based SIMD local
                    // TODO-PPC64LE: Implement stack store of zero SIMD value
                    //NYI_PPC64("genCodeForStoreLclVar - SIMD zero init to stack");
                    abort();
                }
                genUpdateLifeStore(lclNode, targetReg, varDsc);
                return;
            }
            if (zeroInit)
            {
                // For PPC64LE, we can use R0 as zero register in some contexts
                dataReg = REG_R0;
            }
            else
            {
                const GenTree* bitcastSrc = data->AsUnOp()->gtGetOp1();
                assert(!bitcastSrc->isContained());
                dataReg = bitcastSrc->GetRegNum();
            }
        }
        else
        {
            assert(!data->isContained());
            dataReg = data->GetRegNum();
        }
        assert(dataReg != REG_NA);

        if (targetReg == REG_NA) // store into stack based LclVar
        {
            inst_set_SV_var(lclNode);

            instruction ins  = ins_Store(targetType);
            emitAttr    attr = emitActualTypeSize(targetType);

            emit->emitIns_S_R(ins, attr, dataReg, varNum, /* offset */ 0);
        }
        else // store into register (i.e move into register)
        {
            // Assign into targetReg when dataReg (from op1) is not the same register
            if (varTypeIsIntegral(targetType) && emit->isGeneralRegister(targetReg) && emit->isGeneralRegister(dataReg))
            {
                // For PPC64LE, we may need sign/zero extension
                // Use appropriate move instruction with extension if needed
                inst_Mov(targetType, targetReg, dataReg, /* canSkip */ true);
            }
            else
            {
                // For floating point or when no extension needed
                inst_Mov(targetType, targetReg, dataReg, /* canSkip */ true);
            }
        }
        genUpdateLifeStore(lclNode, targetReg, varDsc);
    }
}

//------------------------------------------------------------------------
// genSimpleReturn: Generate code for a simple return (non-struct, non-void).
//
// Arguments:
//    treeNode - The GT_RETURN/GT_RETFILT/GT_SWIFT_ERROR_RET tree node with non-struct and non-void type
//
// Return Value:
//    None
//
void CodeGen::genSimpleReturn(GenTree* treeNode)
{
    assert(treeNode->OperIs(GT_RETURN, GT_RETFILT, GT_SWIFT_ERROR_RET));
    GenTree*  op1        = treeNode->AsOp()->GetReturnValue();
    var_types targetType = treeNode->TypeGet();

    assert(targetType != TYP_STRUCT);
    assert(targetType != TYP_VOID);

    regNumber retReg = varTypeUsesFloatArgReg(treeNode) ? REG_FLOATRET : REG_INTRET;

    bool movRequired = (op1->GetRegNum() != retReg);

    if (!movRequired)
    {
        if (op1->OperGet() == GT_LCL_VAR)
        {
            GenTreeLclVarCommon* lcl            = op1->AsLclVarCommon();
            const LclVarDsc*     varDsc         = compiler->lvaGetDesc(lcl);
            bool                 isRegCandidate = varDsc->lvIsRegCandidate();
            if (isRegCandidate && ((op1->gtFlags & GTF_SPILLED) == 0))
            {
                // We may need to generate a zero-extending mov instruction to load the value from this GT_LCL_VAR

                var_types op1Type = genActualType(op1->TypeGet());
                var_types lclType = genActualType(varDsc->TypeGet());

                if (genTypeSize(op1Type) < genTypeSize(lclType))
                {
                    movRequired = true;
                }
            }
        }
    }

    // For PPC64LE, use inst_Mov to move the return value to the appropriate return register
    inst_Mov(targetType, retReg, op1->GetRegNum(), /* canSkip */ !movRequired);
}

//------------------------------------------------------------------------
// genCodeForLclVar: Produce code for a GT_LCL_VAR node.
//
// Arguments:
//    tree - the GT_LCL_VAR node
//
void CodeGen::genCodeForLclVar(GenTreeLclVar* tree)
{
    unsigned varNum = tree->GetLclNum();
    assert(varNum < compiler->lvaCount);

    LclVarDsc* varDsc         = compiler->lvaGetDesc(varNum);
    bool       isRegCandidate = varDsc->lvIsRegCandidate();

    // lcl_vars are not defs
    assert((tree->gtFlags & GTF_VAR_DEF) == 0);

    // If this is a register candidate that has been spilled, genConsumeReg() will
    // reload it at the point of use. Otherwise, if it's not in a register, we load it here.
    if (!isRegCandidate && !tree->IsMultiReg() && ((tree->gtFlags & GTF_SPILLED) == 0))
    {
        var_types targetType = varDsc->GetRegisterType(tree);

        // targetType must be a normal scalar type and not a TYP_STRUCT
        assert(targetType != TYP_STRUCT);

        instruction ins  = ins_Load(targetType);
        emitAttr    attr = emitActualTypeSize(targetType);

        GetEmitter()->emitIns_R_S(ins, attr, tree->GetRegNum(), varNum, 0);
        genProduceReg(tree);
    }
}

//------------------------------------------------------------------------
// genCreateAndStoreGCInfo: Create and record GC Info for the function.
//
void CodeGen::genCreateAndStoreGCInfo(unsigned            codeSize,
                                      unsigned            prologSize,
				      unsigned epilogSize DEBUGARG(void* codePtr))
{
    //_ASSERTE("!NYI");
    abort();
}


//------------------------------------------------------------------------
// genRangeCheck: generate code for GT_BOUNDS_CHECK node.
//
void CodeGen::genRangeCheck(GenTree* oper)
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genCodeForPhysReg - generate code for a GT_PHYSREG node
//
// Arguments
//    tree - the GT_PHYSREG node
//
// Return value:
//    None
//
void CodeGen::genCodeForPhysReg(GenTreePhysReg* tree)
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genCodeForNullCheck - generate code for a GT_NULLCHECK node
//
// Arguments
//    tree - the GT_NULLCHECK node
//
// Return value:
//    None
//
void CodeGen::genCodeForNullCheck(GenTreeIndir* tree)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genCodeForShift: Generates the code sequence for a GenTree node that
// represents a bit shift or rotate operation (<<, >>, >>>, rol, ror).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//
void CodeGen::genCodeForShift(GenTree* tree)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genCodeForLclAddr: Generates the code for GT_LCL_ADDR.
//
// Arguments:
//    lclAddrNode - the node.
//
void CodeGen::genCodeForLclAddr(GenTreeLclFld* lclAddrNode)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genCodeForInitBlkLoop - Generate code for an InitBlk using an inlined for-loop.
//    It's needed for cases when size is too big to unroll and we're not allowed
//    to use memset call due to atomicity requirements.
//
// Arguments:
//    initBlkNode - the GT_STORE_BLK node
//
void CodeGen::genCodeForInitBlkLoop(GenTreeBlk* initBlkNode)
{
    //_ASSERTE("!NYI");
    abort();
}

//----------------------------------------------------------------------------------
// genCodeForInitBlkUnroll: Generate unrolled block initialization code.
//
// Arguments:
//    node - the GT_STORE_BLK node to generate code for
//
void CodeGen::genCodeForInitBlkUnroll(GenTreeBlk* node)
{
    abort();
}
//------------------------------------------------------------------------
// inst_SETCC: Generate code to set a register to 0 or 1 based on a condition.
//
// Arguments:
//   condition - The condition
//   type      - The type of the value to be produced
//   dstReg    - The destination register to be set to 1 or 0
//
void CodeGen::inst_SETCC(GenCondition condition, var_types type, regNumber dstReg)
{
    //_ASSERTE("!NYI");
    assert(varTypeIsIntegral(type));
    assert(genIsValidIntReg(dstReg));

    // PowerPC uses branchy pattern like ARM32:
    // Emit code like:
    //   bCC True      ; branch if condition is true
    //   li rD, 0      ; set register to 0 (false case)
    //   b Next        ; skip the true case
    // True:
    //   li rD, 1      ; set register to 1 (true case)
    // Next:
    //   ...

    BasicBlock* labelTrue = genCreateTempLabel();
    inst_JCC(condition, labelTrue);

    // False case: set register to 0
    GetEmitter()->emitIns_R_I(INS_li, emitActualTypeSize(type), dstReg, 0);

    BasicBlock* labelNext = genCreateTempLabel();
    GetEmitter()->emitIns_J(INS_b, labelNext);

    // True case: set register to 1
    genDefineTempLabel(labelTrue);
    GetEmitter()->emitIns_R_I(INS_li, emitActualTypeSize(type), dstReg, 1);

    genDefineTempLabel(labelNext);
}


//------------------------------------------------------------------------
// inst_JMP: Generate a jump instruction.
//
void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
    assert(tgtBlock != nullptr);

    GetEmitter()->emitIns_J(emitter::emitJumpKindToIns(jmp), tgtBlock);
}


//------------------------------------------------------------------------
// genCodeForStoreBlk: Produce code for a GT_STORE_BLK node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForStoreBlk(GenTreeBlk* blkOp)
{
    //_ASSERTE("!NYI");
    abort();
}


//------------------------------------------------------------------------
// genCodeForLclFld: Produce code for a GT_LCL_FLD node.
//
// Arguments:
//    tree - the GT_LCL_FLD node
//
void CodeGen::genCodeForLclFld(GenTreeLclFld* tree)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genCodeForIndexAddr: Produce code for a GT_INDEX_ADDR node.
//
// Arguments:
//    tree - the GT_INDEX_ADDR node
//
void CodeGen::genCodeForIndexAddr(GenTreeIndexAddr* node)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genCall: Produce code for a GT_CALL node
//
void CodeGen::genCall(GenTreeCall* call)
{
    genCallPlaceRegArgs(call);

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);

        // Load word from "this" pointer to trigger null check
        // Using lwz (load word and zero) with R0 as destination (discarded)
        GetEmitter()->emitIns_R_R_I(INS_lwz, EA_4BYTE, REG_R0, regThis, 0);
    }

    // If fast tail call, then we are done here, we just have to load the call
    // target into the right registers. We ensure in RA that target is loaded
    // into a volatile register that won't be restored by epilog sequence.
    if (call->IsFastTailCall())
    {
        GenTree* target = getCallTarget(call, nullptr);

        if (target != nullptr)
        {
            // Indirect fast tail calls materialize call target either in gtControlExpr or in gtCallAddr.
            genConsumeReg(target);
        }
#ifdef FEATURE_READYTORUN
        else if (call->IsR2ROrVirtualStubRelativeIndir())
        {
            assert((call->IsR2RRelativeIndir() && (call->gtEntryPoint.accessType == IAT_PVALUE)) ||
                   (call->IsVirtualStubRelativeIndir() && (call->gtEntryPoint.accessType == IAT_VALUE)));
            assert(call->gtControlExpr == nullptr);

            regNumber tmpReg = internalRegisters.GetSingle(call);
            // Register where we save call address in should not be overridden by epilog.
            // Note: PPC64LE doesn't have a dedicated link register constant like ARM's RBM_LR,
            // but the link register is implicitly used by branch-and-link instructions.
            assert((genRegMask(tmpReg) & RBM_INT_CALLEE_TRASH) == genRegMask(tmpReg));

            regNumber callAddrReg =
                call->IsVirtualStubRelativeIndir() ? compiler->virtualStubParamInfo->GetReg() : REG_R2R_INDIRECT_PARAM;
            GetEmitter()->emitIns_R_R_I(ins_Load(TYP_I_IMPL), emitActualTypeSize(TYP_I_IMPL), tmpReg, callAddrReg, 0);
            // We will use this again when emitting the jump in genCallInstruction in the epilog
            internalRegisters.Add(call, genRegMask(tmpReg));
        }
#endif

        return;
    }

    // For a pinvoke to unmanaged code we emit a label to clear
    // the GC pointer state before the callsite.
    // We can't utilize the typical lazy killing of GC pointers
    // at (or inside) the callsite.
    if (compiler->killGCRefs(call))
    {
        genDefineTempLabel(genCreateTempLabel());
    }

    genCallInstruction(call);

    genDefinePendingCallLabel(call);

#ifdef DEBUG
    // We should not have GC pointers in killed registers live around the call.
    // GC info for arg registers were cleared when consuming arg nodes above
    // and LSRA should ensure it for other trashed registers.
    regMaskTP killMask = RBM_CALLEE_TRASH;
    if (call->IsHelperCall())
    {
        CorInfoHelpFunc helpFunc = compiler->eeGetHelperNum(call->gtCallMethHnd);
        killMask                 = compiler->compHelperCallKillSet(helpFunc);
    }

    assert((gcInfo.gcRegGCrefSetCur & killMask) == 0);
    assert((gcInfo.gcRegByrefSetCur & killMask) == 0);
#endif // DEBUG

    var_types returnType = call->TypeGet();
    if (returnType != TYP_VOID)
    {
        regNumber returnReg;

        if (call->HasMultiRegRetVal())
        {
            const ReturnTypeDesc* pRetTypeDesc = call->GetReturnTypeDesc();
            assert(pRetTypeDesc != nullptr);
            unsigned regCount = pRetTypeDesc->GetReturnRegCount();

            // If regs allocated to call node are different from ABI return
            // regs in which the call has returned its result, move the result
            // to regs allocated to call node.
            for (unsigned i = 0; i < regCount; ++i)
            {
                var_types regType      = pRetTypeDesc->GetReturnRegType(i);
                returnReg              = pRetTypeDesc->GetABIReturnReg(i, call->GetUnmanagedCallConv());
                regNumber allocatedReg = call->GetRegNumByIdx(i);
                inst_Mov(regType, allocatedReg, returnReg, /* canSkip */ true);
            }
        }
        else
        {
            if (varTypeUsesFloatReg(returnType))
            {
                returnReg = REG_FLOATRET;
            }
            else
            {
                returnReg = REG_INTRET;
            }

            if (call->GetRegNum() != returnReg)
            {
                inst_Mov(returnType, call->GetRegNum(), returnReg, /* canSkip */ false);
            }
        }

        genProduceReg(call);
    }
}

//------------------------------------------------------------------------
// genCallInstruction - Generate instructions necessary to transfer control to the call.
//
// Arguments:
//    call - the GT_CALL node
//
// Remaks:
//   For tailcalls this function will generate a jump.
//
void CodeGen::genCallInstruction(GenTreeCall* call)
{
    // Determine return value size(s).
    const ReturnTypeDesc* pRetTypeDesc  = call->GetReturnTypeDesc();
    emitAttr              retSize       = EA_PTRSIZE;
    emitAttr              secondRetSize = EA_UNKNOWN;

    // unused values are of no interest to GC.
    if (!call->IsUnusedValue())
    {
        if (call->HasMultiRegRetVal())
        {
            retSize       = emitTypeSize(pRetTypeDesc->GetReturnRegType(0));
            secondRetSize = emitTypeSize(pRetTypeDesc->GetReturnRegType(1));
        }
        else
        {
            assert(call->gtType != TYP_STRUCT);

            if (call->gtType == TYP_REF)
            {
                retSize = EA_GCREF;
            }
            else if (call->gtType == TYP_BYREF)
            {
                retSize = EA_BYREF;
            }
        }
    }

    DebugInfo di;
    // We need to propagate the debug information to the call instruction, so we can emit
    // an IL to native mapping record for the call, to support managed return value debugging.
    // We don't want tail call helper calls that were converted from normal calls to get a record,
    // so we skip this hash table lookup logic in that case.
    if (compiler->opts.compDbgInfo && compiler->genCallSite2DebugInfoMap != nullptr && !call->IsTailCall())
    {
        (void)compiler->genCallSite2DebugInfoMap->Lookup(call, &di);
    }

    CORINFO_SIG_INFO* sigInfo = nullptr;
#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (!call->IsHelperCall())
    {
        sigInfo = call->callSig;
    }

    if (call->IsFastTailCall())
    {
        regMaskTP trashedByEpilog = RBM_CALLEE_SAVED;

        // The epilog may use and trash REG_GSCOOKIE_TMP_0/1. Make sure we have no
        // non-standard args that may be trash if this is a tailcall.
        if (compiler->getNeedsGSSecurityCookie())
        {
            trashedByEpilog |= genRegMask(REG_GSCOOKIE_TMP_0);
            trashedByEpilog |= genRegMask(REG_GSCOOKIE_TMP_1);
        }

        for (CallArg& arg : call->gtArgs.Args())
        {
            for (unsigned i = 0; i < arg.NewAbiInfo.NumSegments; i++)
            {
                const ABIPassingSegment& seg = arg.NewAbiInfo.Segment(i);
                if (seg.IsPassedInRegister() && ((trashedByEpilog & seg.GetRegisterMask()) != 0))
                {
                    JITDUMP("Tail call node:\n");
                    DISPTREE(call);
                    JITDUMP("Register used: %s\n", getRegName(seg.GetRegister()));
                    assert(!"Argument to tailcall may be trashed by epilog");
                }
            }
        }
    }
#endif // DEBUG
    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = getCallTarget(call, &methHnd);

    if (target != nullptr)
    {
        // A call target can not be a contained indirection
        assert(!target->isContainedIndir());

        // For fast tailcall we have already consumed the target. We ensure in
        // RA that the target was allocated into a volatile register that will
        // not be messed up by epilog sequence.
        if (!call->IsFastTailCall())
        {
            genConsumeReg(target);
        }

        // We have already generated code for gtControlExpr evaluating it into a register.
        // We just need to emit "call reg" in this case.
        //
        assert(genIsValidIntReg(target->GetRegNum()));

        // clang-format off
        genEmitCall(emitter::EC_INDIR_R,
                    methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo)
                    nullptr, // addr
                    retSize
                    MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                    di,
                    target->GetRegNum(),
                    call->IsFastTailCall());
        // clang-format on
    }
    else
    {
        // If we have no target and this is a call with indirection cell then
        // we do an optimization where we load the call address directly from
        // the indirection cell instead of duplicating the tree. In BuildCall
        // we ensure that get an extra register for the purpose. Note that for
        // CFG the call might have changed to
        // CORINFO_HELP_DISPATCH_INDIRECT_CALL in which case we still have the
        // indirection cell but we should not try to optimize.
        regNumber callThroughIndirReg = REG_NA;
        if (!call->IsHelperCall(compiler, CORINFO_HELP_DISPATCH_INDIRECT_CALL))
        {
            callThroughIndirReg = getCallIndirectionCellReg(call);
        }

        if (callThroughIndirReg != REG_NA)
        {
            assert(call->IsR2ROrVirtualStubRelativeIndir());
            regNumber targetAddrReg;
            // For fast tailcalls we have already loaded the call target when processing the call node.
            if (!call->IsFastTailCall())
            {
                // For PPC64LE, allocate an internal register to load the target into.
                // Similar to ARM32 approach - we use an internal register for the load.
                targetAddrReg = internalRegisters.GetSingle(call);

                GetEmitter()->emitIns_R_R_I(ins_Load(TYP_I_IMPL), emitActualTypeSize(TYP_I_IMPL), targetAddrReg,
                                            callThroughIndirReg, 0);
            }
            else
            {
                targetAddrReg = internalRegisters.GetSingle(call);
                // Register where we save call address in should not be overridden by epilog.
                // PPC64LE uses link register implicitly for branch-and-link instructions.
                // Ensure the target register is in the callee-trash set (volatile registers).
                assert((genRegMask(targetAddrReg) & RBM_INT_CALLEE_TRASH) == genRegMask(targetAddrReg));
            }

            // We have now generated code loading the target address from the indirection cell into `targetAddrReg`.
            // We just need to emit "bl targetAddrReg" (branch and link) in this case.
            //
            assert(genIsValidIntReg(targetAddrReg));

            // clang-format off
            genEmitCall(emitter::EC_INDIR_R,
                        methHnd,
                        INDEBUG_LDISASM_COMMA(sigInfo)
                        nullptr, // addr
                        retSize
                        MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                        di,
                        targetAddrReg,
                        call->IsFastTailCall());
            // clang-format on
        }
        else
        {
            // Generate a direct call to a non-virtual user defined or helper method
            assert(call->IsHelperCall() || (call->gtCallType == CT_USER_FUNC));

            void* addr = nullptr;
#ifdef FEATURE_READYTORUN
            if (call->gtEntryPoint.addr != NULL)
            {
                assert(call->gtEntryPoint.accessType == IAT_VALUE);
                addr = call->gtEntryPoint.addr;
            }
            else
#endif // FEATURE_READYTORUN
                if (call->IsHelperCall())
                {
                    CorInfoHelpFunc helperNum = compiler->eeGetHelperNum(methHnd);
                    noway_assert(helperNum != CORINFO_HELP_UNDEF);

                    void* pAddr = nullptr;
                    addr        = compiler->compGetHelperFtn(helperNum, (void**)&pAddr);
                    assert(pAddr == nullptr);
                }
                else
                {
                    // Direct call to a non-virtual user function.
                    addr = call->gtDirectCallAddress;
                }

            assert(addr != nullptr);

            // clang-format off
            genEmitCall(emitter::EC_FUNC_TOKEN,
                        methHnd,
                        INDEBUG_LDISASM_COMMA(sigInfo)
                        addr,
                        retSize
                        MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                        di,
                        REG_NA,
                        call->IsFastTailCall());
            // clang-format on
        }
    }
}

//------------------------------------------------------------------------
// genJmpPlaceVarArgs:
//   Generate code to place all varargs correctly for a JMP.
//
void CodeGen::genJmpPlaceVarArgs()
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genGetVolatileLdStIns: Determine the most efficient instruction to perform a
//    volatile load or store and whether an explicit barrier is required or not.
//
// Arguments:
//    currentIns   - the current instruction to perform load/store
//    targetReg    - the target register
//    indir        - the indirection node representing the volatile load/store
//    needsBarrier - OUT parameter. Set to true if an explicit memory barrier is required.
//
// Return Value:
//    instruction to perform the volatile load/store with.
//
instruction CodeGen::genGetVolatileLdStIns(instruction   currentIns,
					regNumber     targetReg,
					GenTreeIndir* indir,
					bool*         needsBarrier)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genCodeForIndir: Produce code for a GT_IND node.
//
// Arguments:
//    tree - the GT_IND node
//
void CodeGen::genCodeForIndir(GenTreeIndir* tree)
{
    assert(tree->OperIs(GT_IND));

#ifdef FEATURE_SIMD
    if (tree->TypeGet() == TYP_SIMD12)
    {
	abort();
    }
#endif

    var_types   type      = tree->TypeGet();
    instruction ins       = ins_Load(type);
    regNumber   targetReg = tree->GetRegNum();

    genConsumeAddress(tree->Addr());

    if (tree->IsVolatile())
    {
        abort();
    }

    GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), targetReg, tree);

    genProduceReg(tree);
}


void CodeGen::genEHCatchRet(BasicBlock* block)
{
    //_ASSERTE("!NYI");
    abort();
}

// The following classes
//   - InitBlockUnrollHelper
//   - CopyBlockUnrollHelper
// encapsulate algorithms that produce instruction sequences for inlined equivalents of memset() and memcpy() functions.
//
// Each class has a private template function that accepts an "InstructionStream" as a template class argument:
//   - InitBlockUnrollHelper::UnrollInitBlock<InstructionStream>(startDstOffset, byteCount, initValue)
//   - CopyBlockUnrollHelper::UnrollCopyBlock<InstructionStream>(startSrcOffset, startDstOffset, byteCount)
//
// The design goal is to separate optimization approaches implemented by the algorithms
// from the target platform specific details.
//
// InstructionStream is a "stream" of load/store instructions (i.e. ldr/ldp/str/stp) that represents an instruction
// sequence that will initialize a memory region with some value or copy values from one memory region to another.
//
// As far as UnrollInitBlock and UnrollCopyBlock concerned, InstructionStream implements the following class member
// functions:
//   - LoadPairRegs(offset, regSizeBytes)
//   - StorePairRegs(offset, regSizeBytes)
//   - LoadReg(offset, regSizeBytes)
//   - StoreReg(offset, regSizeBytes)
//
// There are three implementations of InstructionStream:
//   - CountingStream that counts how many instructions were pushed out of the stream
//   - VerifyingStream that validates that all the instructions in the stream are encodable on Arm64
//   - ProducingStream that maps the function to corresponding emitter functions
//
// The idea behind the design is that decision regarding what instruction sequence to emit
// (scalar instructions vs. SIMD instructions) is made by execution an algorithm producing an instruction sequence
// while counting the number of produced instructions and verifying that all the instructions are encodable.
//
// For example, using SIMD instructions might produce a shorter sequence but require "spilling" a value of a starting
// address
// to an integer register (due to stricter offset alignment rules for 16-byte wide SIMD instructions).
// This the CodeGen can take this fact into account before emitting an instruction sequence.
//
// Alternative design might have had VerifyingStream and ProducingStream fused into one class
// that would allow to undo an instruction if the sequence is not fully encodable.

#if 0
class CountingStream
{
public:
    CountingStream()
    {
	instrCount = 0;
    }

    void LoadPairRegs(int offset, unsigned regSizeBytes)
    {
	instrCount++;
    }

    void StorePairRegs(int offset, unsigned regSizeBytes)
    {
	instrCount++;
    }

    void LoadReg(int offset, unsigned regSizeBytes)
    {
	instrCount++;
    }

    void StoreReg(int offset, unsigned regSizeBytes)
    {
	instrCount++;
    }

    unsigned InstructionCount() const
    {
	return instrCount;
    }

private:
    unsigned instrCount;
};

class VerifyingStream
{
public:
    VerifyingStream()
    {
	canEncodeAllLoads  = true;
	canEncodeAllStores = true;
    }

    void LoadPairRegs(int offset, unsigned regSizeBytes)
    {
	canEncodeAllLoads = canEncodeAllLoads && emitter::canEncodeLoadOrStorePairOffset(offset, EA_SIZE(regSizeBytes));
    }

    void StorePairRegs(int offset, unsigned regSizeBytes)
    {
	canEncodeAllStores =
	canEncodeAllStores && emitter::canEncodeLoadOrStorePairOffset(offset, EA_SIZE(regSizeBytes));
    }

    void LoadReg(int offset, unsigned regSizeBytes)
    {
	canEncodeAllLoads =
	canEncodeAllLoads && emitter::emitIns_valid_imm_for_ldst_offset(offset, EA_SIZE(regSizeBytes));
    }

    void StoreReg(int offset, unsigned regSizeBytes)
    {
	canEncodeAllStores =
	canEncodeAllStores && emitter::emitIns_valid_imm_for_ldst_offset(offset, EA_SIZE(regSizeBytes));
    }

    bool CanEncodeAllLoads() const
    {
	return canEncodeAllLoads;
    }

    bool CanEncodeAllStores() const
    {
	return canEncodeAllStores;
    }

private:
    bool canEncodeAllLoads;
    bool canEncodeAllStores;
};

#endif

//----------------------------------------------------------------------------------
// genCodeForCpBlkUnroll: Generate unrolled block copy code.
//
// Arguments:
//    node - the GT_STORE_BLK node to generate code for
//
void CodeGen::genCodeForCpBlkUnroll(GenTreeBlk* node)
{
    //_ASSERTE("!NYI");
    abort();
}


//------------------------------------------------------------------------
// genCodeForMemmove: Perform an unrolled memmove. The idea that we can
//    ignore the fact that src and dst might overlap if we save the whole
//    src to temp regs in advance, e.g. for memmove(dst: x1, src: x0, len: 30):
//
//       ldr   q16, [x0]
//       ldr   q17, [x0, #0x0E]
//       str   q16, [x1]
//       str   q17, [x1, #0x0E]
//
// Arguments:
//    tree - GenTreeBlk node
//
void CodeGen::genCodeForMemmove(GenTreeBlk* tree)
{
    //_ASSERTE("!NYI");
    abort();
}
    

// clang-format off
const CodeGen::GenConditionDesc CodeGen::GenConditionDesc::map[32]
{
    { },       // NONE  (index 0)
    { },       // 1     (index 1)
    { EJ_lt }, // SLT   (index 2) - Signed Less Than
    { EJ_le }, // SLE   (index 3) - Signed Less or Equal
    { EJ_ge }, // SGE   (index 4) - Signed Greater or Equal
    { EJ_gt }, // SGT   (index 5) - Signed Greater Than
    { },       // S     (index 6) - Sign bit set (not used on PPC)
    { },       // NS    (index 7) - Sign bit not set (not used on PPC)

    { EJ_eq }, // EQ    (index 8) - Equal
    { EJ_ne }, // NE    (index 9) - Not Equal ← YOUR TEST USES THIS!
    { EJ_lt }, // ULT   (index 10) - Unsigned Less Than
    { EJ_le }, // ULE   (index 11) - Unsigned Less or Equal
    { EJ_ge }, // UGE   (index 12) - Unsigned Greater or Equal
    { EJ_gt }, // UGT   (index 13) - Unsigned Greater Than
    { },       // C     (index 14) - Carry (not used on PPC)
    { },       // NC    (index 15) - No Carry (not used on PPC)

    { EJ_eq }, // FEQ   (index 16) - Float Equal
    { EJ_ne }, // FNE   (index 17) - Float Not Equal
    { EJ_lt }, // FLT   (index 18) - Float Less Than
    { EJ_le }, // FLE   (index 19) - Float Less or Equal
    { EJ_ge }, // FGE   (index 20) - Float Greater or Equal
    { EJ_gt }, // FGT   (index 21) - Float Greater Than
    { },       // O     (index 22) - Overflow (not used on PPC)
    { },       // NO    (index 23) - No Overflow (not used on PPC)

    { },       // FEQU  (index 24) - Float Equal Unordered
    { },       // FNEU  (index 25) - Float Not Equal Unordered
    { },       // FLTU  (index 26) - Float Less Than Unordered
    { },       // FLEU  (index 27) - Float Less or Equal Unordered
    { },       // FGEU  (index 28) - Float Greater or Equal Unordered
    { },       // FGTU  (index 29) - Float Greater Than Unordered
    { },       // P     (index 30) - Parity (not used on PPC)
    { },       // NP    (index 31) - No Parity (not used on PPC)
};
// clang-format on

/*****************************************************************************
 *
 *  Generates code for a function epilog.
 *
 *  Please consult the "debugger team notification" comment in genFnProlog().
 */

void CodeGen::genFnEpilog(BasicBlock* block)
{
    assert(block != nullptr);

    regMaskTP regsToRestoreMask = regSet.rsGetModifiedCalleeSavedRegsMask();

    int totalFrameSize = genTotalFrameSize();
    int localFrameSize = compiler->compLclFrameSize + 96;

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        localFrameSize -= TARGET_POINTER_SIZE;
    }

    if ((compiler->lvaMonAcquired != BAD_VAR_NUM) && !compiler->opts.IsOSR())
    {
        localFrameSize -= TARGET_POINTER_SIZE;
    }

    constexpr int LR_save_offset = 16;
    constexpr int R2_save_offset = 24;

    emitter* emit = GetEmitter();
    int      offset;

    regMaskTP maskRestoreRegsFloat = regsToRestoreMask & RBM_ALLFLOAT;
    regMaskTP maskRestoreRegsInt   = regsToRestoreMask & RBM_INT_CALLEE_SAVED;

    offset = localFrameSize;
    for (int regNum = REG_R14; regNum <= REG_R31; regNum++)
    {
        regNumber reg     = (regNumber)regNum;
        regMaskTP regMask = genRegMask(reg);

        if ((maskRestoreRegsInt & regMask) != RBM_NONE)
        {
            offset += REGSIZE_BYTES;
        }
    }

    for (int regNum = REG_F31; regNum >= REG_F14; regNum--)
    {
        regNumber reg     = (regNumber)regNum;
        regMaskTP regMask = genRegMask(reg);

        if ((maskRestoreRegsFloat & regMask) != RBM_NONE)
        {
            offset -= REGSIZE_BYTES;
            emit->emitIns_R_R_I(INS_lfd, EA_8BYTE, reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
        }
    }

    for (int regNum = REG_R31; regNum >= REG_R14; regNum--)
    {
        regNumber reg     = (regNumber)regNum;
        regMaskTP regMask = genRegMask(reg);

        if ((maskRestoreRegsInt & regMask) != RBM_NONE)
        {
            offset -= REGSIZE_BYTES;
            emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
        }
    }

    emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, totalFrameSize);
    compiler->unwindAllocStack(totalFrameSize);

    emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_R0, REG_SPBASE, LR_save_offset);
    compiler->unwindSaveReg(REG_R0, LR_save_offset);

    emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_R2, REG_SPBASE, R2_save_offset);
    compiler->unwindSaveReg(REG_R2, R2_save_offset);

    emit->emitIns_R(INS_mtlr, EA_PTRSIZE, REG_R0);
    emit->emitIns(INS_blr);
}

//------------------------------------------------------------------------
// genPushCalleeSavedRegisters: Push any callee-saved registers we have used.
//
// Arguments (arm64):
//    initReg        - A scratch register (that gets set to zero on some platforms).
//    pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'true' if this method sets initReg register to zero,
//                     'false' if initReg was set to a non-zero value, and left unchanged if initReg was not touched.
//
void CodeGen::genPushCalleeSavedRegisters()
{
    assert(compiler->compGeneratingProlog);

    regMaskTP rsPushRegs = regSet.rsGetModifiedCalleeSavedRegsMask();

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

    // PPC64LE currently always uses the frame pointer in the same style as the
    // simpler fixed-frame LoongArch64/RISC-V64 implementations.
    assert(isFramePointerUsed());

    regSet.rsMaskCalleeSaved = rsPushRegs;

#ifdef DEBUG
    JITDUMP("Frame info. #outsz=%d; #framesz=%d; LclFrameSize=%d;\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
            genTotalFrameSize(), compiler->compLclFrameSize);

    if (compiler->compCalleeRegsPushed != genCountBits(regSet.rsMaskCalleeSaved))
    {
        printf("Error: unexpected number of callee-saved registers to push. Expected: %d. Got: %d ",
               compiler->compCalleeRegsPushed, genCountBits(rsPushRegs));
        dspRegMask(rsPushRegs);
        printf("\n");
        assert(compiler->compCalleeRegsPushed == genCountBits(rsPushRegs | RBM_FPBASE));
    }

    if (verbose)
    {
        regMaskTP maskSaveRegsFloat = rsPushRegs & RBM_ALLFLOAT;
        regMaskTP maskSaveRegsInt   = rsPushRegs & ~maskSaveRegsFloat;
        printf("Save float regs: ");
        dspRegMask(maskSaveRegsFloat);
        printf("\n");
        printf("Save int   regs: ");
        dspRegMask(maskSaveRegsInt);
        printf("\n");
    }
#endif // DEBUG

    int totalFrameSize = genTotalFrameSize();
    int localFrameSize = compiler->compLclFrameSize + 96;

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        localFrameSize -= TARGET_POINTER_SIZE;
    }

    if ((compiler->lvaMonAcquired != BAD_VAR_NUM) && !compiler->opts.IsOSR())
    {
        localFrameSize -= TARGET_POINTER_SIZE;
    }

#ifdef DEBUG
    if (compiler->opts.disAsm)
    {
        printf("Frame info. #outsz=%d; #framesz=%d; lcl=%d\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
               genTotalFrameSize(), localFrameSize);
    }
#endif

    constexpr int FP_backchain_save_offset = -8;
    constexpr int LR_save_offset           = 16;
    constexpr int R2_save_offset           = 24;

    GetEmitter()->emitIns_R_R_I(INS_std, EA_PTRSIZE, REG_R2, REG_SPBASE, R2_save_offset);
    GetEmitter()->emitIns_R(INS_mflr, EA_PTRSIZE, REG_R0);
    GetEmitter()->emitIns_R_R_I(INS_std, EA_PTRSIZE, REG_R0, REG_SPBASE, LR_save_offset);
    GetEmitter()->emitIns_R_R_I(INS_std, EA_PTRSIZE, REG_FP, REG_SPBASE, FP_backchain_save_offset);

    // Keep the implementation simple and ABI-conformant: save the ABI linkage
    // area entries first, then allocate the full frame with an updating store,
    // establish the frame pointer from SP, save FP at the top of the callee-save
    // area, then save the rest of the modified callee-saved registers in
    // ascending register order.
    GetEmitter()->emitIns_R_R_I(INS_stdu, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, -totalFrameSize);
    compiler->unwindAllocStack(totalFrameSize);

    GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_FP, REG_SPBASE, /* canSkip */ false);

    int offset = localFrameSize;

    regMaskTP maskSaveRegsFloat = rsPushRegs & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = rsPushRegs & RBM_INT_CALLEE_SAVED;

    for (int regNum = REG_R14; regNum <= REG_R31; regNum++)
    {
        regNumber reg     = (regNumber)regNum;
        regMaskTP regMask = genRegMask(reg);

        if ((maskSaveRegsInt & regMask) != RBM_NONE)
        {
            GetEmitter()->emitIns_R_R_I(INS_std, EA_PTRSIZE, reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
            offset += REGSIZE_BYTES;
        }
    }

    for (int regNum = REG_F14; regNum <= REG_F31; regNum++)
    {
        regNumber reg     = (regNumber)regNum;
        regMaskTP regMask = genRegMask(reg);

        if ((maskSaveRegsFloat & regMask) != RBM_NONE)
        {
            GetEmitter()->emitIns_R_R_I(INS_stfd, EA_8BYTE, reg, REG_SPBASE, offset);
            compiler->unwindSaveReg(reg, offset);
            offset += REGSIZE_BYTES;
        }
    }

    JITDUMP("    offsetSpToSavedFp=%d\n", FP_backchain_save_offset);
    compiler->unwindSetFrameReg(REG_FPBASE, FP_backchain_save_offset);

    if (compiler->info.compIsVarArgs)
    {
        JITDUMP("    compIsVarArgs=true\n");
        NYI_POWERPC64("genPushCalleeSavedRegisters does not yet support compIsVarArgs");
    }
}

//------------------------------------------------------------------------
// genInstrWithConstant:   we will typically generate one instruction
//
//    ins  reg1, reg2, imm
//
// However the imm might not fit as a directly encodable immediate,
// when it doesn't fit we generate extra instruction(s) that sets up
// the 'regTmp' with the proper immediate value.
//
//     mov  regTmp, imm
//     ins  reg1, reg2, regTmp
//
// Arguments:
//    ins                 - instruction
//    attr                - operation size and GC attribute
//    reg1, reg2          - first and second register operands
//    imm                 - immediate value (third operand when it fits)
//    tmpReg              - temp register to use when the 'imm' doesn't fit. Can be REG_NA
//                          if caller knows for certain the constant will fit.
//    inUnwindRegion      - true if we are in a prolog/epilog region with unwind codes.
//                          Default: false.
//
// Return Value:
//    returns true if the immediate was small enough to be encoded inside instruction. If not,
//    returns false meaning the immediate was too large and tmpReg was used and modified.
//
bool CodeGen::genInstrWithConstant(instruction ins,
				   emitAttr    attr,
				   regNumber   reg1,
				   regNumber   reg2,
				   ssize_t     imm,
				   regNumber   tmpReg,
				   bool        inUnwindRegion /* = false */)
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!
//
int CodeGenInterface::genCallerSPtoFPdelta() const
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.

int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return offset from the stack pointer (Initial-SP) to the frame pointer. The frame pointer
// will point to the saved frame pointer slot (i.e., there will be frame pointer chaining).
//
int CodeGenInterface::genSPtoFPdelta() const
{
    //_ASSERTE("!NYI");
    abort();
}

//---------------------------------------------------------------------
// genTotalFrameSize - return the total size of the stack frame, including local size,
// callee-saved register size, etc.
///
// Return value:
//    Total frame size
//

int CodeGenInterface::genTotalFrameSize() const
{
    assert(!IsUninitialized(compiler->compCalleeRegsPushed));

    // PPC64LE ABI-specific fixed frame addition currently required by this
    // backend:
    //   - 112 bytes fixed frame reservation
    //
    // The fixed pre-allocation FP backchain save at -8(sp) is not part of the
    // post-allocation frame size.
    int totalFrameSize = compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize + 112;

    assert(totalFrameSize >= 0);
    return totalFrameSize;
}

//-----------------------------------------------------------------------------------
// genProfilingLeaveCallback: Generate the profiling function leave or tailcall callback.
// Technically, this is not part of the epilog; it is called when we are generating code for a GT_RETURN node.
//
// Arguments:
//     helper - which helper to call. Either CORINFO_HELP_PROF_FCN_LEAVE or CORINFO_HELP_PROF_FCN_TAILCALL
//
// Return Value:
//     None
//
void CodeGen::genProfilingLeaveCallback(unsigned helper)
{
    //_ASSERTE("!NYI");
    abort();
}

//  move an immediate value into an integer register
void CodeGen::instGen_Set_Reg_To_Imm(emitAttr       size,
                                     regNumber      reg,                                     ssize_t        imm,
                                     insFlags flags DEBUGARG(size_t targetHandle) DEBUGARG(GenTreeFlags gtFlags))
{
    // reg cannot be a FP register
    assert(!genIsValidFloatReg(reg));

    if (!compiler->opts.compReloc)
    {
        size = EA_SIZE(size); // Strip any Reloc flags from size if we aren't doing relocs
    }

    if (EA_IS_RELOC(size))
    {
        abort();
    }
    else if (imm == 0)
    {
        // Zero: li reg, 0
	GetEmitter()->emitIns_R_I(INS_li, size, reg, 0, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
    }
    else if (GetEmitter()->emitIns_valid_imm_for_li(imm))
    {
	// 16-bit signed immediate: li reg, imm
	GetEmitter()->emitIns_R_I(INS_li, size, reg, imm, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
    }
    else
    {
	// For larger immediates, use multiple instructions
	// This is a simplified version - full implementation will be in emitOutputInstr
	if (size == EA_4BYTE)
	{
	    // 32-bit: lis + ori
	    GetEmitter()->emitIns_R_I(INS_li, size, reg, imm, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_ori, size, reg, imm, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	}
	else //EA_8BYTE
	{
	    GetEmitter()->emitIns_R_I(INS_li, size, reg, imm, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_ori, size, reg, imm, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_sldi, size, reg, 32, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_oris, size, reg, imm, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_ori, size, reg, imm, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	}
    }
}

//-----------------------------------------------------------------------------------
// genProfilingEnterCallback: Generate the profiling function enter callback.
//
// Arguments:
//     initReg        - register to use as scratch register
//     pInitRegZeroed - OUT parameter. *pInitRegZeroed set to 'false' if 'initReg' is
//                      set to non-zero value after this call.
//
// Return Value:
//     None
//
void CodeGen::genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed)
{
    //_ASSERTE("!NYI");
    abort();
}

/*****************************************************************************
 *  Emit a call to a helper function.
 *
 */

void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTargetReg /*= REG_NA */)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genAllocLclFrame: Probe the stack.
//
// Notes:
//      This only does the probing; allocating the frame is done when callee-saved registers are saved.
//      This is done before anything has been pushed. The previous frame might have a large outgoing argument
//      space that has been allocated, but the lowest addresses have not been touched. Our frame setup might
//      not touch up to the first 504 bytes. This means we could miss a guard page. On Windows, however,
//      there are always three guard pages, so we will not miss them all. On Linux, there is only one guard
//      page by default, so we need to be more careful. We do an extra probe if we might not have probed
//      recently enough. That is, if a call and prolog establishment might lead to missing a page. We do this
//      on Windows as well just to be consistent, even though it should not be necessary.
//  
// Arguments:
//      frameSize         - the size of the stack frame being allocated.
//      initReg           - register to use as a scratch register.
//      pInitRegZeroed    - OUT parameter. *pInitRegZeroed is set to 'false' if and only if
//                          this call sets 'initReg' to a non-zero value. Otherwise, it is unchanged.
//      maskArgRegsLiveIn - incoming argument registers that are currently live.
//
// Return value:
//      None
//
void CodeGen::genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn)
{
    //_ASSERTE("!NYI");
    abort();
}


//------------------------------------------------------------------------
// genIntToFloatCast: Generate code to cast an int/long to float/double
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    SrcType= int32/uint32/int64/uint64 and DstType=float/double.
//
void CodeGen::genIntToFloatCast(GenTree* treeNode)
{
    //_ASSERTE("!NYI");
    abort();
}

//-----------------------------------------------------------------------------
// genZeroInitFrameUsingBlockInit: architecture-specific helper for genZeroInitFrame in the case
// `genUseBlockInit` is set.
//
// Arguments:
//    untrLclHi      - (Untracked locals High-Offset)  The upper bound offset at which the zero init
//                                                     code will end initializing memory (not inclusive).
//    untrLclLo      - (Untracked locals Low-Offset)   The lower bound at which the zero init code will
//                                                     start zero initializing memory.
//    initReg        - A scratch register (that gets set to zero on some platforms).
//    pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'true' if this method sets initReg register to zero,
//                     'false' if initReg was set to a non-zero value, and left unchanged if initReg was not touched.
//
void CodeGen::genZeroInitFrameUsingBlockInit(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed)
{
    //_ASSERTE("!NYI");
    abort();
}


// clang-format off
/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 *
 *  Funclets have the following incoming arguments:
 *
 *      catch:          x0 = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:         x0 = the exception object to filter (see GT_CATCH_ARG), x1 = CallerSP of the containing function
 *      finally/fault:  none
 *
 *  Funclets set the following registers on exit:
 *
 *      catch:          x0 = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:         x0 = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:  none
 *
 *  The ARM64 funclet prolog sequence is one of the following (Note: #framesz is total funclet frame size,
 *  including everything; #outsz is outgoing argument space. #framesz must be a multiple of 16):
 *
 *  Frame type 1:
 *     For #outsz == 0 and #framesz <= 512:
 *     stp fp,lr,[sp,-#framesz]!    ; establish the frame (predecrement by #framesz), save FP/LR
 *     stp x19,x20,[sp,#xxx]        ; save callee-saved registers, as necessary
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |      OSR padding      | // If required
 *      |-----------------------|
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |    MonitorAcquired    | // 8 bytes; for synchronized methods
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in NativeAOT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned.
 *      |-----------------------|
 *      |      Saved FP, LR     | // 16 bytes
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *  Frame type 2:
 *     For #outsz != 0 and #framesz <= 512:
 *     sub sp,sp,#framesz           ; establish the frame
 *     stp fp,lr,[sp,#outsz]        ; save FP/LR.
 *     stp x19,x20,[sp,#xxx]        ; save callee-saved registers, as necessary
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |      OSR padding      | // If required
 *      |-----------------------|
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |    MonitorAcquired    | // 8 bytes; for synchronized methods
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in NativeAOT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned.
 *      |-----------------------|
 *      |      Saved FP, LR     | // 16 bytes
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *  Frame type 3:
 *     For #framesz > 512:
 *     stp fp,lr,[sp,- (#framesz - #outsz)]!    ; establish the frame, save FP/LR
 *                                              ; note that it is guaranteed here that (#framesz - #outsz) <= 240
 *     stp x19,x20,[sp,#xxx]                    ; save callee-saved registers, as necessary
 *     sub sp,sp,#outsz                         ; create space for outgoing argument space
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |      OSR padding      | // If required
 *      |-----------------------|
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |    MonitorAcquired    | // 8 bytes; for synchronized methods
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in NativeAOT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the first SP subtraction 16 byte aligned
 *      |-----------------------|
 *      |      Saved FP, LR     | // 16 bytes <-- SP after first adjustment (points at saved FP)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned (specifically, to 16-byte align the outgoing argument space).
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes
 *      |-----------------------| <---- Ambient SP (SP after second adjustment)
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 * Both #1 and #2 only change SP once. That means that there will be a maximum of one alignment slot needed. For the general case, #3,
 * it is possible that we will need to add alignment to both changes to SP, leading to 16 bytes of alignment. Remember that the stack
 * pointer needs to be 16 byte aligned at all times. The size of the PSP slot plus callee-saved registers space is a maximum of 240 bytes:
 *
 *     FP,LR registers
 *     10 int callee-saved register x19-x28
 *     8 float callee-saved registers v8-v15
 *     8 saved integer argument registers x0-x7, if varargs function
 *     1 PSP slot
 *     1 alignment slot or monitor acquired slot
 *     == 30 slots * 8 bytes = 240 bytes.
 *
 * The outgoing argument size, however, can be very large, if we call a function that takes a large number of
 * arguments (note that we currently use the same outgoing argument space size in the funclet as for the main
 * function, even if the funclet doesn't have any calls, or has a much smaller, or larger, maximum number of
 * outgoing arguments for any call). In that case, we need to 16-byte align the initial change to SP, before
 * saving off the callee-saved registers and establishing the PSPsym, so we can use the limited immediate offset
 * encodings we have available, before doing another 16-byte aligned SP adjustment to create the outgoing argument
 * space. Both changes to SP might need to add alignment padding.
 *
 * In addition to the above "standard" frames, we also need to support a frame where the saved FP/LR are at the
 * highest addresses. This is to match the frame layout (specifically, callee-saved registers including FP/LR
 * and the PSPSym) that is used in the main function when a GS cookie is required due to the use of localloc.
 * (Note that localloc cannot be used in a funclet.) In these variants, not only has the position of FP/LR
 * changed, but where the alignment padding is placed has also changed.
 *
 *  Frame type 4 (variant of frame types 1 and 2):
 *     For #framesz <= 512:
 *     sub sp,sp,#framesz           ; establish the frame
 *     stp x19,x20,[sp,#xxx]        ; save callee-saved registers, as necessary
 *     stp fp,lr,[sp,#yyy]          ; save FP/LR.
 *     ; write PSPSym
 *
 *  The "#framesz <= 512" condition ensures that after we've established the frame, we can use "stp" with its
 *  maximum allowed offset (504) to save the callee-saved register at the highest address.
 *
 *  We use "sub" instead of folding it into the next instruction as a predecrement, as we need to write PSPSym
 *  at the bottom of the stack, and there might also be an alignment padding slot.
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |      OSR padding      | // If required
 *      |-----------------------|
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |      Saved LR         | // 8 bytes
 *      |-----------------------|
 *      |      Saved FP         | // 8 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |    MonitorAcquired    | // 8 bytes; for synchronized methods
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in NativeAOT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned.
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes (optional; if #outsz > 0)
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *  Frame type 5 (variant of frame type 3):
 *     For #framesz > 512:
 *     sub sp,sp,(#framesz - #outsz) ; establish part of the frame. Note that it is guaranteed here that (#framesz - #outsz) <= 240
 *     stp x19,x20,[sp,#xxx]        ; save callee-saved registers, as necessary
 *     stp fp,lr,[sp,#yyy]          ; save FP/LR.
 *     sub sp,sp,#outsz             ; create space for outgoing argument space
 *     ; write PSPSym
 *
 *  For large frames with "#framesz > 512", we must do one SP adjustment first, after which we can save callee-saved
 *  registers with up to the maximum "stp" offset of 504. Then, we can establish the rest of the frame (namely, the
 *  space for the outgoing argument space).
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |      OSR padding      | // If required
 *      |-----------------------|
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |      Saved LR         | // 8 bytes
 *      |-----------------------|
 *      |      Saved FP         | // 8 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |    MonitorAcquired    | // 8 bytes; for synchronized methods
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in NativeAOT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the first SP subtraction 16 byte aligned <-- SP after first adjustment (points at alignment padding or PSP slot)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned (specifically, to 16-byte align the outgoing argument space).
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes
 *      |-----------------------| <---- Ambient SP (SP after second adjustment)
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 * Note that in this case we might have 16 bytes of alignment that is adjacent. This is because we are doing 2 SP
 * subtractions, and each one must be aligned up to 16 bytes.
 *
 * Note that in all cases, the PSPSym is in exactly the same position with respect to Caller-SP, and that location is the same relative to Caller-SP
 * as in the main function.
 *
 * Funclets do not have varargs arguments. However, because the PSPSym must exist at the same offset from Caller-SP as in the main function, we
 * must add buffer space for the saved varargs argument registers here, if the main function did the same.
 *
 *     ; After this header, fill the PSP slot, for use by the VM (it gets reported with the GC info), or by code generation of nested filters.
 *     ; This is not part of the "OS prolog"; it has no associated unwind data, and is not reversed in the funclet epilog.
 *
 *     if (this is a filter funclet)
 *     {
 *          // x1 on entry to a filter funclet is CallerSP of the containing function:
 *          // either the main function, or the funclet for a handler that this filter is dynamically nested within.
 *          // Note that a filter can be dynamically nested within a funclet even if it is not statically within
 *          // a funclet. Consider:
 *          //
 *          //    try {
 *          //        try {
 *          //            throw new Exception();
 *          //        } catch(Exception) {
 *          //            throw new Exception();     // The exception thrown here ...
 *          //        }
 *          //    } filter {                         // ... will be processed here, while the "catch" funclet frame is still on the stack
 *          //    } filter-handler {
 *          //    }
 *          //
 *          // Because of this, we need a PSP in the main function anytime a filter funclet doesn't know whether the enclosing frame will
 *          // be a funclet or main function. We won't know any time there is a filter protecting nested EH. To simplify, we just always
 *          // create a main function PSP for any function with a filter.
 *
 *          ldr x1, [x1, #CallerSP_to_PSP_slot_delta]  ; Load the CallerSP of the main function (stored in the PSP of the dynamically containing funclet or function)
 *          str x1, [sp, #SP_to_PSP_slot_delta]        ; store the PSP
 *          add fp, x1, #Function_CallerSP_to_FP_delta ; re-establish the frame pointer
 *     }
 *     else
 *     {
 *          // This is NOT a filter funclet. The VM re-establishes the frame pointer on entry.
 *          // TODO-ARM64-CQ: if VM set x1 to CallerSP on entry, like for filters, we could save an instruction.
 *
 *          add x3, fp, #Function_FP_to_CallerSP_delta  ; compute the CallerSP, given the frame pointer. x3 is scratch.
 *          str x3, [sp, #SP_to_PSP_slot_delta]         ; store the PSP
 *     }
 *
 *  An example epilog sequence is then:
 *
 *     add sp,sp,#outsz             ; if any outgoing argument space
 *     ...                          ; restore callee-saved registers
 *     ldp x19,x20,[sp,#xxx]
 *     ldp fp,lr,[sp],#framesz
 *     ret lr
 *
 * See CodeGen::genPushCalleeSavedRegisters() for a description of the main function frame layout.
 * See Compiler::lvaAssignVirtualFrameOffsetsToLocals() for calculation of main frame local variable offsets.
 */
// clang-format on

void CodeGen::genFuncletProlog(BasicBlock* block)
{
    //_ASSERTE("!NYI");
    abort();
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 *
 *  See the description of frame shapes at genFuncletProlog().
 */

void CodeGen::genFuncletEpilog()
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genFloatToIntCast: Generate code to cast float/double to int/long
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    SrcType=float/double and DstType= int32/uint32/int64/uint64
//
void CodeGen::genFloatToIntCast(GenTree* treeNode)
{
    //_ASSERTE("!NYI");
    abort();
}

/*****************************************************************************
 *
 *  Capture the information used to generate the funclet prologs and epilogs.
 *  Note that all funclet prologs are identical, and all funclet epilogs are
 *  identical (per type: filters are identical, and non-filters are identical).
 *  Thus, we compute the data used for these just once.
 *
 *  See genFuncletProlog() for more information about the prolog/epilog sequences.
 */

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
    //_ASSERTE("!NYI");
    abort();
}

void CodeGen::genSetPSPSym(regNumber initReg, bool* pInitRegZeroed)
{    
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genLeaInstruction: Produce code for a GT_LEA node.
//
// Arguments:
//    lea - the node
//
void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    //_ASSERTE("!NYI");
    abort();
}

#ifdef FEATURE_SIMD
//------------------------------------------------------------------------
// genSIMDSplitReturn: Generates code for returning a fixed-size SIMD type that lives
//                     in a single register, but is returned in multiple registers.
//
// Arguments:
//    src         - The source of the return
//    retTypeDesc - The return type descriptor.
//
void CodeGen::genSIMDSplitReturn(GenTree* src, ReturnTypeDesc* retTypeDesc)
{
    //_ASSERTE("!NYI");
    abort();
}
#endif // FEATURE_SIMD
    

//------------------------------------------------------------------------
// genIntCastOverflowCheck: Generate overflow checking code for an integer cast.
//
// Arguments:
//    cast - The GT_CAST node
//    desc - The cast description
//    reg  - The register containing the value to check
//
void CodeGen::genIntCastOverflowCheck(GenTreeCast* cast, const GenIntCastDesc& desc, regNumber reg)
{
    //_ASSERTE("!NYI");
    abort();
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer cast, with or without overflow check.
//
// Arguments:
//    cast - The GT_CAST node
//
// Assumptions:
//    Neither the source nor target type can be a floating point type.
//
void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    //_ASSERTE("!NYI");
    abort();
}
 
//------------------------------------------------------------------------
// genFloatToFloatCast: Generate code for a cast between float and double
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    The cast is between float and double.
//
void CodeGen::genFloatToFloatCast(GenTree* treeNode)
{
    //_ASSERTE("!NYI");
    abort();
}


/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           End Prolog / Epilog                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    //_ASSERTE("!NYI");
    abort();
}




#endif // TARGET_POWERPC64
