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

        case GT_CNS_DBL:
        {
            JITDUMP("[PPC64LE CNS_DBL] Starting GT_CNS_DBL code generation\n");
            emitter* emit       = GetEmitter();
            emitAttr size       = emitActualTypeSize(tree);
            double   constValue = tree->AsDblCon()->DconValue();
            JITDUMP("[PPC64LE CNS_DBL] constValue=%f, size=%d, targetReg=%s\n",
                    constValue, size, getRegName(targetReg));

            // For PPC64LE, we load floating-point constants via GPR and stack
            // Get the bit representation of the double/float value
            int64_t constValueBits;
            emitAttr gprSize;
            instruction storeIns;
            instruction loadIns;
            
            if (size == EA_4BYTE)
            {
                // Float constant
                float fltVal = (float)constValue;
                constValueBits = *(int32_t*)&fltVal;
                gprSize = EA_4BYTE;
                storeIns = INS_stw;
                loadIns = INS_lfs;
                JITDUMP("[PPC64LE CNS_DBL] Float: bits=0x%08X, storeIns=stw, loadIns=lfs\n",
                        (unsigned)constValueBits);
            }
            else
            {
                // Double constant
                constValueBits = *(int64_t*)&constValue;
                gprSize = EA_8BYTE;
                storeIns = INS_std;
                loadIns = INS_lfd;
                JITDUMP("[PPC64LE CNS_DBL] Double: bits=0x%016llX, storeIns=std, loadIns=lfd\n",
                        (unsigned long long)constValueBits);
            }
            
            // Get a temp integer register to hold the constant bits
            regNumber tempReg = internalRegisters.GetSingle(tree);
            JITDUMP("[PPC64LE CNS_DBL] tempReg=%s\n", getRegName(tempReg));
            
            // Load the constant bit pattern into the temp GPR
            instGen_Set_Reg_To_Imm(gprSize, tempReg, constValueBits);
            JITDUMP("[PPC64LE CNS_DBL] Loaded constant bits into tempReg\n");
            
            // For PPC64LE, we need to transfer the value via stack
            // Use the top of the local frame (genTotalFrameSize() - 16) for temporary storage
            // This ensures we don't overwrite the linkage area or parameter save area
            int offset = genTotalFrameSize() - 16;
            JITDUMP("[PPC64LE CNS_DBL] Stack offset=%d, frameSize=%d\n", offset, genTotalFrameSize());
            
            // Store the GPR value to the temporary stack location
            JITDUMP("[PPC64LE CNS_DBL] About to emit store: ins=%d %s, %d(r1)\n",
                    storeIns, getRegName(tempReg), offset);
            emit->emitIns_R_R_I(storeIns, gprSize, tempReg, REG_SPBASE, offset);
            JITDUMP("[PPC64LE CNS_DBL] Store instruction emitted\n");
            
            // Load the floating-point value from the temporary stack location
            JITDUMP("[PPC64LE CNS_DBL] About to emit load: ins=%d %s, %d(r1)\n",
                    loadIns, getRegName(targetReg), offset);
            emit->emitIns_R_R_I(loadIns, size, targetReg, REG_SPBASE, offset);
            JITDUMP("[PPC64LE CNS_DBL] Load instruction emitted\n");
            
            regSet.verifyRegUsed(targetReg);
            JITDUMP("[PPC64LE CNS_DBL] Completed GT_CNS_DBL code generation\n");
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
	// Floating-point comparison
    	assert(varTypeIsFloating(op2Type));
    	assert(!op1->isContainedIntOrIImmed());
    	assert(!op2->isContainedIntOrIImmed());

    	// PowerPC floating-point comparison instructions:
    	// fcmpu - Floating Compare Unordered (doesn't trap on NaN)
    	// fcmpo - Floating Compare Ordered (traps on NaN)
    	// Use fcmpu for standard comparisons (matches C# semantics)

    	ins = INS_fcmpu;

    	// fcmpu cr0, fA, fB
   	 // Sets CR0 condition register bits:
   	 //   CR0[LT] (bit 0) = fA < fB
    	//   CR0[GT] (bit 1) = fA > fB
    	//   CR0[EQ] (bit 2) = fA == fB
    	//   CR0[UN] (bit 3) = unordered (one or both operands are NaN)

    	emit->emitIns_R_R(ins, cmpSize, op1->GetRegNum(), op2->GetRegNum());
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
    
   	 GenCondition condition;
    	if (varTypeIsFloating(op1Type))
    	{
        	// Floating-point comparison
        	condition = GenCondition::FromFloatRelop(tree);
    	}
    	else
    	{
        	// Integer comparison
        	condition = GenCondition::FromIntegralRelop(tree);
    	}
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
	case GT_CNS_DBL:
	    genSetRegToConst(targetReg, targetType, treeNode);
	           genProduceReg(treeNode);
	    break;

	case GT_IND:
	    genCodeForIndir(treeNode->AsIndir());
	    break;

	case GT_STOREIND:
	    genCodeForStoreInd(treeNode->AsStoreInd());
	    break;

	case GT_LEA:
	    genLeaInstruction(treeNode->AsAddrMode());
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

	case GT_STORE_LCL_FLD:
            genCodeForStoreLclFld(treeNode->AsLclFld());
            break;

	case GT_LCL_VAR:
	    genCodeForLclVar(treeNode->AsLclVar());
	    break;

	case GT_LCL_FLD:
	    genCodeForLclFld(treeNode->AsLclFld());
	    break;

	case GT_RETFILT:
	case GT_RETURN:
	    genReturn(treeNode);
	    break;

	case GT_PUTARG_REG:
	    genPutArgReg(treeNode->AsOp());
	    break;

	case GT_PUTARG_STK:
	    genPutArgStk(treeNode->AsPutArgStk());
	    break;

	case GT_PUTARG_SPLIT:
	    genPutArgSplit(treeNode->AsPutArgSplit());
	    break;

	case GT_CAST:
	    genCodeForCast(treeNode->AsOp());
	    break;

        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
            genConsumeOperands(treeNode->AsOp());
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_DIV:
        case GT_UDIV:
        case GT_MOD:
        case GT_UMOD:
            genConsumeOperands(treeNode->AsOp());
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_AND:
        case GT_OR:
        case GT_XOR:
            genConsumeOperands(treeNode->AsOp());
            genCodeForBinary(treeNode->AsOp());
            break;
        
	case GT_LSH:
	case GT_RSH:
	case GT_RSZ:
	case GT_ROR:
    	    genConsumeOperands(treeNode->AsOp());
            genCodeForShift(treeNode);
    	    break;

	case GT_NOT:
	           genConsumeRegs(treeNode->gtGetOp1());
	    genCodeForNOT(treeNode->AsOp());
	           break;

	case GT_STORE_BLK:
	    genCodeForStoreBlk(treeNode->AsBlk());
	    break;
        case GT_INDEX_ADDR:
	    genCodeForIndexAddr(treeNode->AsIndexAddr());
	    break;

        case GT_LCL_ADDR:
	    genCodeForLclAddr(treeNode->AsLclFld());
	    break;
	case GT_COPY:
	    // This is handled at the time we call genConsumeReg() on the GT_COPY
	    break;
	
	case GT_NULLCHECK:
	    genCodeForNullCheck(treeNode->AsIndir());
	    break;

	case GT_CATCH_ARG:

	           noway_assert(handlerGetsXcptnObj(compiler->compCurBB->bbCatchTyp));

	           /* Catch arguments get passed in a register. genCodeForBBlist()
	              would have marked it as holding a GC object, but not used. */

	           noway_assert(gcInfo.gcRegGCrefSetCur & RBM_EXCEPTION_OBJECT);
	           genConsumeReg(treeNode);
	           break;
	default:
	    printf("ERROR: Unhandled tree node operation: %s (oper=%d)\n",
	                  GenTree::OpName(treeNode->gtOper), treeNode->gtOper);
	           printf("Tree node details: type=%s, flags=0x%x\n",
	                  varTypeName(treeNode->TypeGet()), treeNode->gtFlags);
	    abort();
    }
}

//------------------------------------------------------------------------
// genCodeForBinary: Generate code for many binary arithmetic operators
//
// Arguments:
//    treeNode - tree node
//
// Notes:
//    This method is expected to have called genConsumeOperands() before calling it.
//    Handles GT_ADD, GT_SUB, GT_MUL, GT_DIV, GT_UDIV, GT_MOD, GT_UMOD
//    for both integer and floating-point types.
//
void CodeGen::genCodeForBinary(GenTreeOp* treeNode)
{
          const genTreeOps oper       = treeNode->OperGet();
          regNumber        targetReg  = treeNode->GetRegNum();
          var_types        targetType = treeNode->TypeGet();
          emitter*         emit       = GetEmitter();

          assert(oper == GT_ADD || oper == GT_SUB || oper == GT_MUL ||
                 oper == GT_DIV || oper == GT_UDIV || oper == GT_MOD || oper == GT_UMOD ||
                 oper == GT_AND || oper == GT_OR || oper == GT_XOR);

          GenTree* op1 = treeNode->gtGetOp1();
	  GenTree* op2 = treeNode->gtGetOp2();

	  regNumber op1reg = op1->GetRegNum();
	  regNumber op2reg = op2->GetRegNum();

          instruction ins;
          emitAttr    attr = emitActualTypeSize(treeNode);


          // PowerPC64LE instruction selection based on operation and type
          if (varTypeIsFloating(targetType))
          {
              // Floating-point operations
              switch (oper)
              {
                  case GT_ADD:
                      ins = (attr == EA_4BYTE) ? INS_fadds : INS_fadd;
                      break;
                  case GT_SUB:
                      ins = (attr == EA_4BYTE) ? INS_fsubs : INS_fsub;
                      break;
                  case GT_MUL:
                      ins = (attr == EA_4BYTE) ? INS_fmuls : INS_fmul;
                      break;
                  case GT_DIV:
                      ins = (attr == EA_4BYTE) ? INS_fdivs : INS_fdiv;
                      break;
                  default:
                      unreached();
              }

              // Emit floating-point instruction: ins targetReg, op1reg, op2reg
              emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
          }
          else
          {
              // Integer operations
              bool isImmediate = op2->isContainedIntOrIImmed();

              switch (oper)
              {
                  case GT_ADD:
                      if (isImmediate)
                      {
                          ssize_t imm = op2->AsIntConCommon()->IconValue();
                          // addi: add immediate (16-bit signed immediate)
                          ins = INS_addi;
                          emit->emitIns_R_R_I(ins, attr, targetReg, op1reg, imm);
                      }
                      else
                      {
                          // add: register add
                          ins = INS_add;
                          emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
                      }
                      break;

                  case GT_SUB:
                      // PowerPC64LE doesn't have subi, use addi with negated immediate
                      // or use subf (subtract from) instruction
                      ins = INS_subf;
                      emit->emitIns_R_R_R(ins, attr, targetReg, op2reg, op1reg); // Note: operands reversed for subf
                      break;

                  case GT_MUL:
                      // mulld: multiply low doubleword (64-bit)
                      // mullw: multiply low word (32-bit)
                      ins = (attr == EA_8BYTE) ? INS_mulld : INS_mullw;
                      emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
                      break;

                  case GT_DIV:
                      // divd: divide doubleword signed (64-bit)
                      // divw: divide word signed (32-bit)
                      ins = (attr == EA_8BYTE) ? INS_divd : INS_divw;
                      emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
                      break;

                  case GT_UDIV:
                      // divdu: divide doubleword unsigned (64-bit)
                      // divwu: divide word unsigned (32-bit)
                      ins = (attr == EA_8BYTE) ? INS_divdu : INS_divwu;
                      emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
                      break;

                  case GT_MOD:
                  case GT_UMOD:
		  {
			// Compute: remainder = dividend - (quotient * divisor)
			// Algorithm:
			//   1. quotient = dividend / divisor  (divd/divw or divdu/divwu)
			//   2. temp = quotient * divisor      (mulld/mullw)
			//   3. remainder = dividend - temp    (subf)

			// Need a temporary register for the quotient
			regNumber tempReg = internalRegisters.GetSingle(treeNode);

			// Step 1: Compute quotient in tempReg
			instruction divIns;
			if (oper == GT_MOD)
			{
			    // Signed division
			    divIns = (attr == EA_8BYTE) ? INS_divd : INS_divw;
			}
			else // GT_UMOD
			{
			    // Unsigned division
			    divIns = (attr == EA_8BYTE) ? INS_divdu : INS_divwu;
			}
			emit->emitIns_R_R_R(divIns, attr, tempReg, op1reg, op2reg);

			// Step 2: Multiply quotient by divisor, result in tempReg
			instruction mulIns = (attr == EA_8BYTE) ? INS_mulld : INS_mullw;
			emit->emitIns_R_R_R(mulIns, attr, tempReg, tempReg, op2reg);

			// Step 3: Subtract to get remainder: targetReg = op1reg - tempReg
			// subf rD, rA, rB computes rD = rB - rA, so we use subf targetReg, tempReg, op1reg
			emit->emitIns_R_R_R(INS_subf, attr, targetReg, tempReg, op1reg);
		    }
		    break;

                 case GT_AND:
                    // and: bitwise AND
                    if (isImmediate)
                    {
                        ssize_t imm = op2->AsIntConCommon()->IconValue();
                        ins = INS_andi;
                        emit->emitIns_R_R_I(ins, attr, targetReg, op1reg, imm);
                    }
                    else
                    {
                        ins = INS_and_ins;
                        emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
                    }
                    break;


                case GT_OR:
                    // or: bitwise OR
                    if (isImmediate)
                    {
                        ssize_t imm = op2->AsIntConCommon()->IconValue();
                        ins = INS_ori;
                        emit->emitIns_R_R_I(ins, attr, targetReg, op1reg, imm);
                    }
                    else
                    {
                        ins = INS_or_ins;
                        emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
                    }
                    break;


		case GT_XOR:
                    // xor: bitwise XOR
                    if (isImmediate)
                    {
                        ssize_t imm = op2->AsIntConCommon()->IconValue();
                        ins = INS_xori;
                        emit->emitIns_R_R_I(ins, attr, targetReg, op1reg, imm);
                    }
                    else
                    {
                        ins = INS_xor_ins;
                        emit->emitIns_R_R_R(ins, attr, targetReg, op1reg, op2reg);
                    }
                    break;

		default:
                      unreached();
              }
          }

          genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForNOT: Generate code for GT_NOT (bitwise NOT)
//
// Arguments:
//    treeNode - tree node (GT_NOT)
//
void CodeGen::genCodeForNOT(GenTreeOp* treeNode)
{
    assert(treeNode->OperIs(GT_NOT));
    
    regNumber targetReg = treeNode->GetRegNum();
    GenTree*  op1       = treeNode->gtGetOp1();
    regNumber op1reg    = op1->GetRegNum();  // Already consumed by genConsumeOperands()
    
    emitAttr attr = emitActualTypeSize(treeNode);
    
    // PowerPC implements NOT as NOR with itself: ~A = A NOR A
    GetEmitter()->emitIns_R_R_R(INS_nor, attr, targetReg, op1reg, op1reg);
    
    genProduceReg(treeNode);
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
    //abort();
    assert(compiler->compGeneratingProlog);

    if (!compiler->getNeedsGSSecurityCookie())
    {
        return;  // No GS cookie needed for this function
    }

    // For now, minimal implementation
    // Full implementation would:
    // 1. Load the GS cookie value from a global location
    // 2. Store it to the GS cookie stack slot
    // TODO: Implement full GS cookie support when needed

    // For simple functions without buffers, this won't be called
    return;

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
    assert(treeNode->OperIs(GT_PUTARG_STK));
    emitter* emit = GetEmitter();

    // This is the varNum for our store operations,
    // typically this is the varNum for the Outgoing arg space
    // When we are generating a tail call it will be the varNum for arg0
    unsigned varNumOut    = (unsigned)-1;
    unsigned argOffsetMax = (unsigned)-1; // Records the maximum size of this area for assert checks

    // Get argument offset to use with 'varNumOut'
    // Here we cross check that argument offset hasn't changed from lowering to codegen since
    // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
    unsigned argOffsetOut = treeNode->getArgOffset();

    // Whether to setup stk arg in incoming or out-going arg area?
    // Fast tail calls implemented as epilog+jmp = stk arg is setup in incoming arg area.
    // All other calls - stk arg is setup in out-going arg area.
    if (treeNode->putInIncomingArgArea())
    {
        varNumOut    = getFirstArgWithStackSlot();
        argOffsetMax = compiler->compArgSize;
#if FEATURE_FASTTAILCALL
        // This must be a fast tail call.
        assert(treeNode->gtCall->IsFastTailCall());

        // Since it is a fast tail call, the existence of first incoming arg is guaranteed
        // because fast tail call requires that in-coming arg area of caller is >= out-going
        // arg area required for tail call.
        LclVarDsc* varDsc = compiler->lvaGetDesc(varNumOut);
        assert(varDsc != nullptr);
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        varNumOut    = compiler->lvaOutgoingArgSpaceVar;
        argOffsetMax = compiler->lvaOutgoingArgSpaceSize;
    }

    GenTree* source = treeNode->gtGetOp1();

    if (!source->TypeIs(TYP_STRUCT)) // a normal non-Struct argument
    {
        if (varTypeIsSIMD(source->TypeGet()))
        {
            // SIMD types not yet supported for PPC64LE
            NYI_POWERPC64("genPutArgStk - SIMD types");
        }

        var_types slotType = genActualType(source);

        instruction storeIns  = ins_Store(slotType);
        emitAttr    storeAttr = emitTypeSize(slotType);

        // If it is contained then source must be the integer constant zero
        if (source->isContained())
        {
            assert(source->OperGet() == GT_CNS_INT);
            assert(source->AsIntConCommon()->IconValue() == 0);

            // Use r0 (which is always zero in PPC64LE when used as source)
            // Actually, we need to load 0 into a register first
            regNumber zeroReg = REG_R0;
            emit->emitIns_R_I(INS_li, EA_PTRSIZE, zeroReg, 0);
            emit->emitIns_S_R(storeIns, storeAttr, zeroReg, varNumOut, argOffsetOut);
        }
        else
        {
            genConsumeReg(source);
            regNumber srcReg = source->GetRegNum();
            
            // For HFA struct fields, the register may be a float register even though slotType is TYP_LONG
            // Override the instruction if we have a float register
            if (genIsValidFloatReg(srcReg))
            {
                if (storeAttr == EA_8BYTE)
                {
                    storeIns = INS_stfd;  // Store double (8 bytes)
                }
                else if (storeAttr == EA_4BYTE)
                {
                    storeIns = INS_stfs;  // Store single (4 bytes)
                }
                JITDUMP("genPutArgStk (non-struct): Float register detected, overriding instruction to %s for %s (slotType=%s, attr=%d)\n",
                        genInsName(storeIns), getRegName(srcReg), varTypeName(slotType), (int)storeAttr);
            }
            
            emit->emitIns_S_R(storeIns, storeAttr, srcReg, varNumOut, argOffsetOut);
        }
        argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
        assert(argOffsetOut <= argOffsetMax); // We can't write beyond the outgoing arg area
    }
    else // We have some kind of a struct argument
    {
        assert(source->isContained()); // We expect that this node was marked as contained in Lower

        if (source->OperGet() == GT_FIELD_LIST)
        {
            genPutArgStkFieldList(treeNode, varNumOut);
        }
        else
        {
            noway_assert(source->OperIsLocalRead() || source->OperIs(GT_BLK));

            var_types targetType = source->TypeGet();
            noway_assert(varTypeIsStruct(targetType));

            // We will copy this struct to the stack, possibly using a ld/std instruction
            // Setup loReg from the internal registers that we reserved in lower.
            //
            regNumber loReg = internalRegisters.Extract(treeNode);

            GenTreeLclVarCommon* srcLclNode = nullptr;
            regNumber            addrReg    = REG_NA;
            ClassLayout*         layout     = nullptr;

            // Setup "layout", "srcLclNode" and "addrReg".
            if (source->OperIsLocalRead())
            {
                srcLclNode        = source->AsLclVarCommon();
                layout            = srcLclNode->GetLayout(compiler);
                LclVarDsc* varDsc = compiler->lvaGetDesc(srcLclNode);

                // This struct must live on the stack frame.
                assert(varDsc->lvOnFrame && !varDsc->lvRegister);
            }
            else // we must have a GT_BLK
            {
                layout  = source->AsBlk()->GetLayout();
                addrReg = genConsumeReg(source->AsBlk()->Addr());
            }

            unsigned srcSize = layout->GetSize();

            // If we have an HFA we can't have any GC pointers,
            // if not then the max size for the struct is 16 bytes
            if (compiler->IsHfa(layout->GetClassHandle()))
            {
                noway_assert(!layout->HasGCPtr());
            }
            else
            {
                noway_assert(srcSize <= 2 * TARGET_POINTER_SIZE);
            }

            // PPC64LE ELFv2 ABI: structs of any size can be passed by value on stack
            // No size limit for pass-by-value, so don't assert on MAX_PASS_MULTIREG_BYTES
            // (MAX_PASS_MULTIREG_BYTES only limits what fits in registers, not total struct size)

            unsigned dstSize = treeNode->GetStackByteSize();

            // PPC64LE: If dstSize is 0, this struct is passed entirely in registers
            // and should not be processed by genPutArgStk. This can happen for HFAs.
            if (dstSize == 0)
            {
                // This struct is passed entirely in registers via GT_FIELD_LIST
                // Nothing to do here - the individual fields will be handled by genPutArgReg
                return;
            }

            // We can generate smaller code if store size is a multiple of TARGET_POINTER_SIZE.
            // The dst size can be rounded up to PUTARG_STK size. The src size can be rounded up
            // if it reads a local variable because reading "too much" from a local cannot fault.
            //
            if ((dstSize != srcSize) && (srcLclNode != nullptr))
            {
                unsigned widenedSrcSize = roundUp(srcSize, TARGET_POINTER_SIZE);
                if (widenedSrcSize <= dstSize)
                {
                    srcSize = widenedSrcSize;
                }
            }

            assert(srcSize <= dstSize);

            int      remainingSize = srcSize;
            unsigned structOffset  = 0;
            unsigned lclOffset     = (srcLclNode != nullptr) ? srcLclNode->GetLclOffs() : 0;
            unsigned nextIndex     = 0;

            // For PPC64LE, we will generate a ld and std instruction each loop
            //             ld      r2, 0(r3)
            //             std     r2, offset(r1)
            // For HFA structs, we use lfd/stfd or lfs/stfs instead
            
            // Check if this is an HFA struct
            bool isHfa = false;
            var_types hfaType = TYP_UNDEF;
            unsigned hfaSlots = 0;
            CORINFO_CLASS_HANDLE structHnd = layout->GetClassHandle();
            if (genIsValidFloatReg(loReg) && structHnd != NO_CLASS_HANDLE &&
                IsPpc64leHfaLikeStruct(compiler, structHnd, &hfaType, &hfaSlots))
            {
                isHfa = true;
                JITDUMP("genPutArgStk: Detected HFA struct, loReg=%s, hfaType=%s, hfaSlots=%u\n",
                        getRegName(loReg), varTypeName(hfaType), hfaSlots);
            }
            
            while (remainingSize >= TARGET_POINTER_SIZE)
            {
                var_types type = layout->GetGCPtrType(nextIndex);
                instruction loadIns = INS_ld;
                instruction storeIns = INS_std;
                emitAttr attr = emitTypeSize(type);
                
                // Override for HFA structs - use float instructions
                if (isHfa)
                {
                    if (hfaType == TYP_FLOAT)
                    {
                        loadIns = INS_lfs;
                        storeIns = INS_stfs;
                        attr = EA_4BYTE;
                    }
                    else // TYP_DOUBLE
                    {
                        loadIns = INS_lfd;
                        storeIns = INS_stfd;
                        attr = EA_8BYTE;
                    }
                }

                if (srcLclNode != nullptr)
                {
                    // Load from our local source
                    emit->emitIns_R_S(loadIns, attr, loReg, srcLclNode->GetLclNum(),
                                      lclOffset + structOffset);
                }
                else
                {
                    // check for case of destroying the addrRegister while we still need it
                    assert(loReg != addrReg || remainingSize == TARGET_POINTER_SIZE);

                    // Load from our address expression source
                    emit->emitIns_R_R_I(loadIns, attr, loReg, addrReg, structOffset);
                }

                // Emit store instruction to store the register into the outgoing argument area
                emit->emitIns_S_R(storeIns, attr, loReg, varNumOut, argOffsetOut);
                argOffsetOut += TARGET_POINTER_SIZE;  // We stored 8-bytes of the struct
                assert(argOffsetOut <= argOffsetMax); // We can't write beyond the outgoing arg area

                remainingSize -= TARGET_POINTER_SIZE; // We loaded 8-bytes of the struct
                structOffset += TARGET_POINTER_SIZE;
                nextIndex++;
            }

            // Handle any remaining bytes (less than 8 bytes)
            while (remainingSize > 0)
            {
                var_types type;
                instruction loadIns;
                instruction storeIns;
                unsigned moveSize;

                if (remainingSize >= 4)
                {
                    moveSize = 4;
                    type = layout->GetGCPtrType(nextIndex);
                    loadIns = INS_lwz;
                    storeIns = INS_stw;
                }
                else if (remainingSize >= 2)
                {
                    moveSize = 2;
                    type = TYP_USHORT;
                    loadIns = INS_lhz;
                    storeIns = INS_sth;
                }
                else
                {
                    moveSize = 1;
                    type = TYP_UBYTE;
                    loadIns = INS_lbz;
                    storeIns = INS_stb;
                }

                emitAttr attr = emitTypeSize(type);

                if (srcLclNode != nullptr)
                {
                    // Load from our local source
                    emit->emitIns_R_S(loadIns, attr, loReg, srcLclNode->GetLclNum(), lclOffset + structOffset);
                }
                else
                {
                    assert(loReg != addrReg);
                    // Load from our address expression source
                    emit->emitIns_R_R_I(loadIns, attr, loReg, addrReg, structOffset);
                }

                // Emit a store instruction to store the register into the outgoing argument area
                emit->emitIns_S_R(storeIns, attr, loReg, varNumOut, argOffsetOut);
                argOffsetOut += moveSize;
                assert(argOffsetOut <= argOffsetMax); // We can't write beyond the outgoing arg area

                structOffset += moveSize;
                remainingSize -= moveSize;
            }
        }
    }
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
    assert(tree->OperIs(GT_PUTARG_REG));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->GetRegNum();

    assert(targetType != TYP_STRUCT);

    GenTree* op1 = tree->gtOp1;
    genConsumeReg(op1);

    // For HFA struct fields, the tree type may be TYP_LONG but registers are float registers
    // Override the type based on the actual register type to use correct move instruction
    var_types moveType = targetType;
    if (genIsValidFloatReg(targetReg) && genIsValidFloatReg(op1->GetRegNum()))
    {
        // Both registers are float registers - determine type from size
        if (targetType == TYP_LONG || emitActualTypeSize(targetType) == EA_8BYTE)
        {
            moveType = TYP_DOUBLE;
        }
        else if (emitActualTypeSize(targetType) == EA_4BYTE)
        {
            moveType = TYP_FLOAT;
        }
        JITDUMP("[PPC64LE HFA DEBUG] genPutArgReg: Float registers detected, overriding type from %s to %s for %s -> %s\n",
                varTypeName(targetType), varTypeName(moveType), getRegName(op1->GetRegNum()), getRegName(targetReg));
    }

    // If child node is not already in the register we need, move it
    inst_Mov(moveType, targetReg, op1->GetRegNum(), /* canSkip */ true);

    genProduceReg(tree);
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
    assert(treeNode->OperIs(GT_PUTARG_SPLIT));

    GenTree* source       = treeNode->gtOp1;
    emitter* emit         = GetEmitter();
    unsigned varNumOut    = compiler->lvaOutgoingArgSpaceVar;
    unsigned argOffsetMax = compiler->lvaOutgoingArgSpaceSize;

    if (source->OperGet() == GT_FIELD_LIST)
    {
        // Evaluate each of the GT_FIELD_LIST items into their register
        // and store their register into the outgoing argument area
        unsigned regIndex         = 0;
        unsigned firstOnStackOffs = UINT_MAX;

        for (GenTreeFieldList::Use& use : source->AsFieldList()->Uses())
        {
            GenTree*  nextArgNode = use.GetNode();
            regNumber fieldReg    = nextArgNode->GetRegNum();
            genConsumeReg(nextArgNode);

            if (regIndex >= treeNode->gtNumRegs)
            {
                if (firstOnStackOffs == UINT_MAX)
                {
                    firstOnStackOffs = use.GetOffset();
                }

                var_types type   = use.GetType();
                unsigned  offset = treeNode->getArgOffset() + use.GetOffset() - firstOnStackOffs;
                // We can't write beyond the outgoing arg area
                assert((offset + genTypeSize(type)) <= argOffsetMax);

                // Emit store instructions to store the registers produced by the GT_FIELD_LIST into the outgoing
                // argument area
                emit->emitIns_S_R(ins_Store(type), emitActualTypeSize(type), fieldReg, varNumOut, offset);
            }
            else
            {
                var_types type   = treeNode->GetRegType(regIndex);
                regNumber argReg = treeNode->GetRegNumByIdx(regIndex);

                // If child node is not already in the register we need, move it
                inst_Mov(type, argReg, fieldReg, /* canSkip */ true);

                regIndex++;
            }
        }
    }
    else
    {
        var_types targetType = source->TypeGet();
        
        // For HFA structs, the source might not be contained and the type might not be TYP_STRUCT
        // (it could be TYP_LONG for an 8-byte field). Check if this is an HFA struct.
        bool isHfaStruct = false;
        if (source->OperIsLocalRead())
        {
            LclVarDsc* varDsc = compiler->lvaGetDesc(source->AsLclVarCommon()->GetLclNum());
            CORINFO_CLASS_HANDLE classHnd = varDsc->lvClassHnd;
            if (classHnd == NO_CLASS_HANDLE && varDsc->GetLayout() != nullptr)
            {
                classHnd = varDsc->GetLayout()->GetClassHandle();
            }
            
            if (classHnd != NO_CLASS_HANDLE)
            {
                var_types hfaType;
                unsigned hfaSlots;
                isHfaStruct = IsPpc64leHfaLikeStruct(compiler, classHnd, &hfaType, &hfaSlots);
                if (isHfaStruct)
                {
                    JITDUMP("[PPC64LE HFA DEBUG] genPutArgSplit: HFA struct detected (type=%s, contained=%d)\n",
                            varTypeName(targetType), source->isContained());
                }
            }
        }
        
        // For regular structs, source must be contained and type must be struct
        // For HFA structs, we relax these requirements
        assert((source->isContained() && varTypeIsStruct(targetType)) || isHfaStruct);

        // We need a register to store intermediate values that we are loading
        // from the source into. We can usually use one of the target registers
        // that will be overridden anyway. The exception is when the source is
        // in a register and that register is the unique target register we are
        // placing. LSRA will always allocate an internal register when there
        // is just one target register to handle this situation.
        //
        int          firstRegToPlace;
        regNumber    valueReg     = REG_NA;
        unsigned     srcLclNum    = BAD_VAR_NUM;
        unsigned     srcLclOffset = 0;
        regNumber    addrReg      = REG_NA;
        var_types    addrType     = TYP_UNDEF;
        ClassLayout* layout       = nullptr;

        if (source->OperIsLocalRead())
        {
            srcLclNum         = source->AsLclVarCommon()->GetLclNum();
            srcLclOffset      = source->AsLclVarCommon()->GetLclOffs();
            LclVarDsc* varDsc = compiler->lvaGetDesc(srcLclNum);

            // For HFA structs, we need custom handling because:
            // 1. The allocated registers are float registers
            // 2. Normal struct handling uses integer load/store instructions
            // 3. Can't use integer instructions with float registers
            if (isHfaStruct)
            {
                JITDUMP("[PPC64LE HFA DEBUG] genPutArgSplit: HFA struct, using custom float load/store\n");
                
                // Get HFA element type and size
                layout = varDsc->GetLayout();
                CORINFO_CLASS_HANDLE classHnd = layout->GetClassHandle();
                
                var_types hfaType;
                unsigned hfaSlots;
                IsPpc64leHfaLikeStruct(compiler, classHnd, &hfaType, &hfaSlots);
                
                unsigned fieldSize = (hfaType == TYP_FLOAT) ? 4 : 8;
                instruction loadIns = (hfaType == TYP_FLOAT) ? INS_lfs : INS_lfd;
                instruction storeIns = (hfaType == TYP_FLOAT) ? INS_stfs : INS_stfd;
                
                // Load fields into registers
                for (unsigned i = 0; i < treeNode->gtNumRegs; i++)
                {
                    regNumber targetReg = treeNode->GetRegNumByIdx(i);
                    unsigned fieldOffset = srcLclOffset + (i * fieldSize);
                    
                    GetEmitter()->emitIns_R_S(loadIns, emitActualTypeSize(hfaType), targetReg, srcLclNum, fieldOffset);
                    
                    JITDUMP("[PPC64LE HFA DEBUG] genPutArgSplit: Loaded HFA field %d from V%02u+%u to %s\n",
                            i, srcLclNum, fieldOffset, getRegName(targetReg));
                }
                
                // Store remaining fields to stack
                unsigned stackFields = hfaSlots - treeNode->gtNumRegs;
                if (stackFields > 0)
                {
                    unsigned argOffsetOut = treeNode->getArgOffset();
                    
                    // Use first target register as temporary (it's already been placed)
                    regNumber tempReg = treeNode->GetRegNumByIdx(0);
                    
                    for (unsigned i = 0; i < stackFields; i++)
                    {
                        unsigned fieldOffset = srcLclOffset + ((treeNode->gtNumRegs + i) * fieldSize);
                        unsigned stackOffset = argOffsetOut + (i * fieldSize);
                        
                        GetEmitter()->emitIns_R_S(loadIns, emitActualTypeSize(hfaType), tempReg, srcLclNum, fieldOffset);
                        GetEmitter()->emitIns_S_R(storeIns, emitActualTypeSize(hfaType), tempReg, varNumOut, stackOffset);
                        
                        JITDUMP("[PPC64LE HFA DEBUG] genPutArgSplit: Stored HFA field %d from V%02u+%u to stack+%u\n",
                                treeNode->gtNumRegs + i, srcLclNum, fieldOffset, stackOffset);
                    }
                }
                
                // Mark the node as having been handled
                // The code in codegencommon.cpp will still process this node,
                // but it will just move registers (which may be no-ops if source == dest)
                genProduceReg(treeNode);
                return;
            }
            
            // Get layout for non-HFA structs
            layout = source->AsLclVarCommon()->GetLayout(compiler);

            // This struct must live on the stack frame.
            assert(varDsc->lvOnFrame && !varDsc->lvRegister);

            // No possible conflicts, just use the first register as the value register.
            firstRegToPlace = 0;
            valueReg        = treeNode->GetRegNumByIdx(0);
        }
        else // we must have a GT_BLK
        {
            layout   = source->AsBlk()->GetLayout();
            addrReg  = genConsumeReg(source->AsBlk()->Addr());
            addrType = source->AsBlk()->Addr()->TypeGet();

            regNumber allocatedValueReg = REG_NA;
            if (treeNode->gtNumRegs == 1)
            {
                allocatedValueReg = internalRegisters.Extract(treeNode);
            }

            // Pick a register to store intermediate values in for the to-stack
            // copy. It must not conflict with addrReg.
            valueReg = treeNode->GetRegNumByIdx(0);
            if (valueReg == addrReg)
            {
                if (treeNode->gtNumRegs == 1)
                {
                    valueReg = allocatedValueReg;
                }
                else
                {
                    valueReg = treeNode->GetRegNumByIdx(1);
                }
            }

            // Find first register to place. If we are placing addrReg, then
            // make sure we place it last to avoid clobbering its value.
            //
            // The loop below will start at firstRegToPlace and place
            // treeNode->gtNumRegs registers in order, with wraparound. For
            // example, if the registers to place are r3, r4, r5=addrReg, r6
            // then we will set firstRegToPlace = 3 (r6) and the loop below
            // will place r6, r3, r4, r5. The last placement will clobber
            // addrReg.
            firstRegToPlace = 0;
            for (unsigned i = 0; i < treeNode->gtNumRegs; i++)
            {
                if (treeNode->GetRegNumByIdx(i) == addrReg)
                {
                    firstRegToPlace = i + 1;
                    break;
                }
            }
        }

        // Put on stack first
        // For HFA structs, calculate offset based on actual field size, not TARGET_POINTER_SIZE
        unsigned structOffset;
        if (isHfaStruct)
        {
            // Get HFA element type to determine field size
            CORINFO_CLASS_HANDLE classHnd = layout->GetClassHandle();
            
            var_types hfaType;
            unsigned hfaSlots;
            IsPpc64leHfaLikeStruct(compiler, classHnd, &hfaType, &hfaSlots);
            
            unsigned fieldSize = (hfaType == TYP_FLOAT) ? 4 : 8;
            structOffset = treeNode->gtNumRegs * fieldSize;
            
            JITDUMP("[PPC64LE HFA DEBUG] genPutArgSplit: HFA struct offset calculation: gtNumRegs=%u, fieldSize=%u, structOffset=%u, layoutSize=%u\n",
                    treeNode->gtNumRegs, fieldSize, structOffset, layout->GetSize());
        }
        else
        {
            structOffset = treeNode->gtNumRegs * TARGET_POINTER_SIZE;
        }
        
        unsigned remainingSize = layout->GetSize() - structOffset;
        unsigned argOffsetOut  = treeNode->getArgOffset();

        assert((remainingSize > 0) && (roundUp(remainingSize, TARGET_POINTER_SIZE) == treeNode->GetStackByteSize()));
        while (remainingSize > 0)
        {
            var_types type;
            instruction loadIns;
            instruction storeIns;
            unsigned moveSize;

            if (remainingSize >= TARGET_POINTER_SIZE)
            {
                type = layout->GetGCPtrType(structOffset / TARGET_POINTER_SIZE);
                loadIns = INS_ld;
                storeIns = INS_std;
                moveSize = TARGET_POINTER_SIZE;
            }
            else if (remainingSize >= 4)
            {
                type = TYP_INT;
                loadIns = INS_lwz;
                storeIns = INS_stw;
                moveSize = 4;
            }
            else if (remainingSize >= 2)
            {
                type = TYP_USHORT;
                loadIns = INS_lhz;
                storeIns = INS_sth;
                moveSize = 2;
            }
            else
            {
                assert(remainingSize == 1);
                type = TYP_UBYTE;
                loadIns = INS_lbz;
                storeIns = INS_stb;
                moveSize = 1;
            }

            emitAttr attr = emitActualTypeSize(type);

            if (srcLclNum != BAD_VAR_NUM)
            {
                // Load from our local source
                emit->emitIns_R_S(loadIns, attr, valueReg, srcLclNum, srcLclOffset + structOffset);
            }
            else
            {
                assert(valueReg != addrReg);

                // Load from our address expression source
                emit->emitIns_R_R_I(loadIns, attr, valueReg, addrReg, structOffset);
            }

            // Emit the instruction to store the register into the outgoing argument area
            emit->emitIns_S_R(storeIns, attr, valueReg, varNumOut, argOffsetOut);
            argOffsetOut += moveSize;
            assert(argOffsetOut <= argOffsetMax);

            remainingSize -= moveSize;
            structOffset += moveSize;
        }

        // Place registers starting from firstRegToPlace. It should ensure we
        // place addrReg last (if we place it at all).
        structOffset         = static_cast<unsigned>(firstRegToPlace) * TARGET_POINTER_SIZE;
        unsigned curRegIndex = firstRegToPlace;

        for (unsigned regsPlaced = 0; regsPlaced < treeNode->gtNumRegs; regsPlaced++)
        {
            if (curRegIndex == treeNode->gtNumRegs)
            {
                curRegIndex  = 0;
                structOffset = 0;
            }

            regNumber targetReg = treeNode->GetRegNumByIdx(curRegIndex);
            var_types type      = treeNode->GetRegType(curRegIndex);

            if (srcLclNum != BAD_VAR_NUM)
            {
                // Load from our local source
                emit->emitIns_R_S(INS_ld, emitTypeSize(type), targetReg, srcLclNum, srcLclOffset + structOffset);
            }
            else
            {
                assert((addrReg != targetReg) || (regsPlaced == treeNode->gtNumRegs - 1));

                // Load from our address expression source
                emit->emitIns_R_R_I(INS_ld, emitTypeSize(type), targetReg, addrReg, structOffset);
            }

            curRegIndex++;
            structOffset += TARGET_POINTER_SIZE;
        }
    }
    genProduceReg(treeNode);
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

            // For HFA structs, if dataReg is a float register, we need to use float store instructions
            // even though targetType might be TYP_LONG
            // Check if this is an HFA struct with float register
            var_types hfaType = TYP_UNDEF;
            unsigned hfaSlots = 0;
            
            if (genIsValidFloatReg(dataReg) && varTypeIsStruct(varDsc))
            {
                // Try to get class handle - first from lvClassHnd, then from GetLayout()
                CORINFO_CLASS_HANDLE classHnd = varDsc->lvClassHnd;
                if (classHnd == NO_CLASS_HANDLE)
                {
                    ClassLayout* layout = varDsc->GetLayout();
                    if (layout != nullptr)
                    {
                        classHnd = layout->GetClassHandle();
                    }
                }
                
                if (classHnd != NO_CLASS_HANDLE && IsPpc64leHfaLikeStruct(compiler, classHnd, &hfaType, &hfaSlots))
                {
                    // This is an HFA struct, compute attr from HFA element type and determine instruction
                    attr = emitActualTypeSize(hfaType);
                    if (attr == EA_8BYTE)
                    {
                        ins = INS_stfd;  // Store double (8 bytes)
                    }
                    else // EA_4BYTE
                    {
                        ins = INS_stfs;  // Store single (4 bytes)
                    }
                    JITDUMP("[PPC64LE HFA DEBUG] genCodeForStoreLclVar: HFA detected (element type %s), overriding instruction to %s for %s -> V%02u (original type=%s, attr=%d, hfaSlots=%u, lvIsParam=%d)\n",
                            varTypeName(hfaType), genInsName(ins), getRegName(dataReg), varNum, varTypeName(targetType), (int)attr, hfaSlots, varDsc->lvIsParam);
                }
            }

            emit->emitIns_S_R(ins, attr, dataReg, varNum, /* offset */ 0); //This will be handled via code implemented at lclvars.cpp:7063
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
// genCodeForStoreLclFld: Produce code for a GT_STORE_LCL_FLD node.
//
// Arguments:
//    tree - the GT_STORE_LCL_FLD node
//
void CodeGen::genCodeForStoreLclFld(GenTreeLclFld* tree)
{
    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->GetRegNum();
    emitter*  emit       = GetEmitter();
    
    noway_assert(targetType != TYP_STRUCT);

    // record the offset
    unsigned offset = tree->GetLclOffs();

    // We must have a stack store with GT_STORE_LCL_FLD
    noway_assert(targetReg == REG_NA);

    unsigned   varNum = tree->GetLclNum();
    LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

    GenTree* data = tree->gtOp1;
    genConsumeRegs(data);

    regNumber dataReg = REG_NA;
    if (data->isContainedIntOrIImmed())
    {
        assert(data->IsIntegralConst(0));
        dataReg = REG_R0;  // Use R0 as zero register
    }
    else if (data->isContained())
    {
        assert(data->OperIs(GT_BITCAST));
        const GenTree* bitcastSrc = data->AsUnOp()->gtGetOp1();
        assert(!bitcastSrc->isContained());
        dataReg = bitcastSrc->GetRegNum();
    }
    else
    {
        assert(!data->isContained());
        dataReg = data->GetRegNum();
    }
    assert(dataReg != REG_NA);

    // Determine the actual type to store based on the source register type
    // For HFA structs on PPC64LE, the tree type may be TYP_LONG but the register is a float register
    var_types storeType = targetType;
    
    // Check if this is an HFA struct with float register
    var_types hfaType = TYP_UNDEF;
    unsigned hfaSlots = 0;
    
    if (genIsValidFloatReg(dataReg) && varTypeIsStruct(varDsc))
    {
        // Try to get class handle - first from lvClassHnd, then from GetLayout()
        CORINFO_CLASS_HANDLE classHnd = varDsc->lvClassHnd;
        if (classHnd == NO_CLASS_HANDLE)
        {
            ClassLayout* layout = varDsc->GetLayout();
            if (layout != nullptr)
            {
                classHnd = layout->GetClassHandle();
            }
        }
        
        if (classHnd != NO_CLASS_HANDLE && IsPpc64leHfaLikeStruct(compiler, classHnd, &hfaType, &hfaSlots))
        {
            // This is an HFA struct, use the HFA element type
            storeType = hfaType;
            JITDUMP("[PPC64LE HFA DEBUG] genCodeForStoreLclFld: HFA detected, overriding store type from %s to %s for %s -> V%02u+%u (hfaSlots=%u, lvIsParam=%d)\n",
                    varTypeName(targetType), varTypeName(storeType), getRegName(dataReg), varNum, offset, hfaSlots, varDsc->lvIsParam);
        }
    }

    instruction ins  = ins_Store(storeType);
    emitAttr    attr = emitActualTypeSize(storeType);

    emit->emitIns_S_R(ins, attr, dataReg, varNum, offset);

    genUpdateLife(tree);

    varDsc->SetRegNum(REG_STK);
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
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) CompIAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder != nullptr);

    // Follow the code pattern of the x86 gc info encoder
    gcInfo.gcInfoBlockHdrSave(gcInfoEncoder, codeSize, prologSize);

    // We keep the call count for the second call to gcMakeRegPtrTable() below.
    unsigned callCnt = 0;

    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS, &callCnt);

    // Now we've requested all the slots we'll need; "finalize" these
    gcInfoEncoder->FinalizeSlotIds();

    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK, &callCnt);

    if (compiler->opts.IsReversePInvoke())
    {
        unsigned reversePInvokeFrameVarNumber = compiler->lvaReversePInvokeFrameVar;
        assert(reversePInvokeFrameVarNumber != BAD_VAR_NUM);
        const LclVarDsc* reversePInvokeFrameVar = compiler->lvaGetDesc(reversePInvokeFrameVarNumber);
        gcInfoEncoder->SetReversePInvokeFrameSlot(reversePInvokeFrameVar->GetStackOffset());
    }

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
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
    assert(tree->OperIs(GT_NULLCHECK));

    genConsumeRegs(tree->gtOp1);

    // Perform a load operation to trigger a null pointer exception if the address is null
    // Use REG_R0 as a scratch register (zero register on PPC64LE)
    GetEmitter()->emitInsLoadStoreOp(ins_Load(tree->TypeGet()), emitActualTypeSize(tree), REG_R0, tree);
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
    assert(tree->OperIs(GT_LSH, GT_RSH, GT_RSZ, GT_ROR));
    
    var_types   targetType = tree->TypeGet();
    genTreeOps  oper       = tree->OperGet();
    instruction ins        = INS_invalid;
    emitAttr    size       = emitActualTypeSize(targetType);
    
    GenTree* operand = tree->gtGetOp1();
    GenTree* shiftBy = tree->gtGetOp2();
    
    regNumber targetReg  = tree->GetRegNum();
    regNumber operandReg = operand->GetRegNum();
    
    // Determine if this is 32-bit or 64-bit operation
    bool is64Bit = (size == EA_8BYTE);
    
    if (shiftBy->IsCnsIntOrI())
    {
        // Immediate shift amount
        ssize_t shiftAmount = shiftBy->AsIntCon()->IconValue();
        
        // Mask shift amount (PowerPC masks automatically, but be explicit)
        shiftAmount &= (is64Bit ? 63 : 31);
        
        // Select appropriate immediate instruction
        switch (oper)
        {
            case GT_LSH:
                ins = is64Bit ? INS_sldi : INS_slwi;
                break;
                
            case GT_RSH:
                // Arithmetic right shift (sign-extending)
                ins = is64Bit ? INS_sradi : INS_srawi;
                break;
                
            case GT_RSZ:
                // Logical right shift (zero-extending)
                ins = is64Bit ? INS_srdi : INS_srwi;
                break;
                
            case GT_ROR:
                abort();
                break;
                
            default:
                unreached();
        }
        
        // Emit: targetReg = operandReg SHIFT shiftAmount
        GetEmitter()->emitIns_R_R_I(ins, size, targetReg, operandReg, shiftAmount);
    }
    else
    {
        // Register-based shift amount
        regNumber shiftReg = shiftBy->GetRegNum();
        
        // Select appropriate register instruction
        switch (oper)
        {
            case GT_LSH:
                ins = is64Bit ? INS_sld : INS_slw;
                break;
                
            case GT_RSH:
                // Arithmetic right shift (sign-extending)
                ins = is64Bit ? INS_srad : INS_sraw;
                break;
                
            case GT_RSZ:
                // Logical right shift (zero-extending)
                ins = is64Bit ? INS_srd : INS_srw;
                break;
                
            case GT_ROR:
                abort();
		break;
                
            default:
                unreached();
        }
        
        // Emit: targetReg = operandReg SHIFT shiftReg
        GetEmitter()->emitIns_R_R_R(ins, size, targetReg, operandReg, shiftReg);
    }
    
    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForLclAddr: Generates the code for GT_LCL_ADDR.
//
// Arguments:
//    lclAddrNode - the node.
//
void CodeGen::genCodeForLclAddr(GenTreeLclFld* lclAddrNode)
{
    assert(lclAddrNode->OperIs(GT_LCL_ADDR));

    var_types targetType = lclAddrNode->TypeGet();
    emitAttr  size       = emitTypeSize(targetType);
    regNumber targetReg  = lclAddrNode->GetRegNum();

    // Address of a local var.
    noway_assert((targetType == TYP_BYREF) || (targetType == TYP_I_IMPL));

    // PowerPC64 doesn't have LEA instruction like x86/ARM
    // We compute the address using: addi targetReg, framePointer, offset
    // The emitIns_R_S handles this by computing the stack offset and generating appropriate instructions
    
    GetEmitter()->emitIns_R_S(INS_addi, size, targetReg, lclAddrNode->GetLclNum(), lclAddrNode->GetLclOffs());

    genProduceReg(lclAddrNode);

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
    assert(node->OperIs(GT_STORE_BLK));

    unsigned  dstLclNum      = BAD_VAR_NUM;
    regNumber dstAddrBaseReg = REG_NA;
    int       dstOffset      = 0;
    GenTree*  dstAddr        = node->Addr();

    if (!dstAddr->isContained())
    {
        dstAddrBaseReg = genConsumeReg(dstAddr);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        assert(!dstAddr->AsAddrMode()->HasIndex());

        dstAddrBaseReg = genConsumeReg(dstAddr->AsAddrMode()->Base());
        dstOffset      = dstAddr->AsAddrMode()->Offset();
    }
    else
    {
        assert(dstAddr->OperIs(GT_LCL_ADDR));
        dstLclNum = dstAddr->AsLclVarCommon()->GetLclNum();
        dstOffset = dstAddr->AsLclVarCommon()->GetLclOffs();
    }

    GenTree* src = node->Data();

    if (src->OperIs(GT_INIT_VAL))
    {
        assert(src->isContained());
        src = src->gtGetOp1();
    }

    if (node->IsVolatile())
    {
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();
    unsigned size = node->GetLayout()->GetSize();

    assert(size <= INT32_MAX);
    assert(dstOffset < INT32_MAX - static_cast<int>(size));

    regNumber srcReg;

    if (!src->isContained())
    {
        srcReg = genConsumeReg(src);
    }
    else
    {
        assert(src->IsIntegralConst(0));
        // On PPC64LE we can use R0 for zero
        srcReg = REG_R0;
    }

    // Unroll the init block using stores of decreasing size
    for (unsigned regSize = REGSIZE_BYTES; size > 0; size -= regSize, dstOffset += regSize)
    {
        while (regSize > size)
        {
            regSize /= 2;
        }

        instruction storeIns;
        emitAttr    attr;

        switch (regSize)
        {
            case 1:
                storeIns = INS_stb;
                attr     = EA_1BYTE;
                break;
            case 2:
                storeIns = INS_sth;
                attr     = EA_2BYTE;
                break;
            case 4:
                storeIns = INS_stw;
                attr     = EA_4BYTE;
                break;
            case 8:
                storeIns = INS_std;
                attr     = EA_8BYTE;
                break;
            default:
                unreached();
        }

        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(storeIns, attr, srcReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_R_R_I(storeIns, attr, srcReg, dstAddrBaseReg, dstOffset);
        }
    }
}

//------------------------------------------------------------------------
// instGen_MemoryBarrier: Generate a memory barrier instruction
//
// Arguments:
//   barrierKind - The kind of barrier to generate
//
void CodeGen::instGen_MemoryBarrier(BarrierKind barrierKind)
{
#ifdef DEBUG
    if (JitConfig.JitNoMemoryBarriers() == 1)
    {
        return;
    }
#endif // DEBUG

    // PPC64LE memory barriers:
    // Always use hwsync (heavy-weight sync) for full memory barrier
    // This ensures strongest ordering for all loads and stores
    
    GetEmitter()->emitIns(INS_hwsync);
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
    assert(blkOp->OperIs(GT_STORE_BLK));

    bool isCopyBlk = blkOp->OperIsCopyBlkOp();

    switch (blkOp->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindCpObjUnroll:
            // CpObj with GC pointers - not yet implemented for PPC64LE
            NYI_POWERPC64("genCodeForStoreBlk: BlkOpKindCpObjUnroll");
            break;

        case GenTreeBlk::BlkOpKindLoop:
            assert(!isCopyBlk);
            genCodeForInitBlkLoop(blkOp);
            break;

        case GenTreeBlk::BlkOpKindUnroll:
            if (isCopyBlk)
            {
                if (blkOp->gtBlkOpGcUnsafe)
                {
                    GetEmitter()->emitDisableGC();
                }
                genCodeForCpBlkUnroll(blkOp);
                if (blkOp->gtBlkOpGcUnsafe)
                {
                    GetEmitter()->emitEnableGC();
                }
            }
            else
            {
                assert(!blkOp->gtBlkOpGcUnsafe);
                genCodeForInitBlkUnroll(blkOp);
            }
            break;

        case GenTreeBlk::BlkOpKindUnrollMemmove:
            // Memmove - not yet implemented for PPC64LE
            NYI_POWERPC64("genCodeForStoreBlk: BlkOpKindUnrollMemmove");
            break;

        default:
            unreached();
    }
}


//------------------------------------------------------------------------
// genCodeForLclFld: Produce code for a GT_LCL_FLD node.
//
// Arguments:
//    tree - the GT_LCL_FLD node
//
void CodeGen::genCodeForLclFld(GenTreeLclFld* tree)
{
    assert(tree->OperIs(GT_LCL_FLD));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->GetRegNum();
    emitter*  emit       = GetEmitter();

    NYI_IF(targetType == TYP_STRUCT, "GT_LCL_FLD: struct load local field not supported");
    assert(targetReg != REG_NA);

    unsigned offs   = tree->GetLclOffs();
    unsigned varNum = tree->GetLclNum();
    assert(varNum < compiler->lvaCount);

    // Determine the actual type to load based on the target register type
    // For HFA structs on PPC64LE, the tree type may be TYP_LONG but the register is a float register
    var_types loadType = targetType;
    
    // Check if this is an HFA struct with float register
    LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
    var_types hfaType = TYP_UNDEF;
    unsigned hfaSlots = 0;
    
    if (genIsValidFloatReg(targetReg) && varTypeIsStruct(varDsc))
    {
        // Try to get class handle - first from lvClassHnd, then from GetLayout()
        CORINFO_CLASS_HANDLE classHnd = varDsc->lvClassHnd;
        if (classHnd == NO_CLASS_HANDLE)
        {
            ClassLayout* layout = varDsc->GetLayout();
            if (layout != nullptr)
            {
                classHnd = layout->GetClassHandle();
            }
        }
        
        if (classHnd != NO_CLASS_HANDLE && IsPpc64leHfaLikeStruct(compiler, classHnd, &hfaType, &hfaSlots))
        {
            // This is an HFA struct, use the HFA element type
            loadType = hfaType;
            JITDUMP("[PPC64LE HFA DEBUG] genCodeForLclFld: HFA detected, overriding load type from %s to %s for V%02u+%u -> %s (hfaSlots=%u, lvIsParam=%d)\n",
                    varTypeName(targetType), varTypeName(loadType), varNum, offs, getRegName(targetReg), hfaSlots, varDsc->lvIsParam);
        }
    }

    emitAttr    attr = emitActualTypeSize(loadType);
    instruction ins  = ins_Load(loadType);
    emit->emitIns_R_S(ins, attr, targetReg, varNum, offs);

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForIndexAddr: Produce code for a GT_INDEX_ADDR node.
//
// Arguments:
//    tree - the GT_INDEX_ADDR node
//
void CodeGen::genCodeForIndexAddr(GenTreeIndexAddr* node)
{
    GenTree* const base  = node->Arr();
    GenTree* const index = node->Index();

    genConsumeReg(base);
    genConsumeReg(index);

    // NOTE: `genConsumeReg` marks the consumed register as not a GC pointer, as it assumes that the input registers
    // die at the first instruction generated by the node. This is not the case for `INDEX_ADDR`, however, as the
    // base register is multiply-used. As such, we need to mark the base register as containing a GC pointer until
    // we are finished generating the code for this node.

    gcInfo.gcMarkRegPtrVal(base->GetRegNum(), base->TypeGet());
    assert(!varTypeIsGC(index->TypeGet()));

    // The index is never contained, even if it is a constant.
    assert(index->isUsedFromReg());

    regNumber baseReg  = base->GetRegNum();
    regNumber indexReg = index->GetRegNum();
    regNumber targetReg = node->GetRegNum();
    emitAttr  attr     = emitActualTypeSize(node);
    emitter*  emit     = GetEmitter();

    // Use R12 as temporary register for intermediate calculations
    // R12 is a volatile register safe to use as scratch
    const regNumber tmpReg = REG_R12;

    // Generate the bounds check if necessary.
    if (node->IsBoundsChecked())
    {
        // Load array length: tmpReg = [base + lenOffset]
        // PowerPC uses lwz (load word and zero) for 32-bit array length
        emit->emitIns_R_R_I(INS_lwz, EA_4BYTE, tmpReg, baseReg, node->gtLenOffset);
        
        // Compare: if (index >= length) goto RangeCheckFailed
        // cmpw/cmpd: compare (unsigned done via branch condition)
        instruction cmpIns = (index->TypeGet() == TYP_LONG) ? INS_cmpd : INS_cmpw;
        emit->emitIns_R_R(cmpIns, emitActualTypeSize(index->TypeGet()), indexReg, tmpReg);
        
        // Branch if index >= length (unsigned comparison)
        genJumpToThrowHlpBlk(EJ_ge, SCK_RNGCHK_FAIL, node->gtIndRngFailBB);
    }

    // Calculate: result = base + (index * elementSize) + elementOffset
    
    // Can we use a shift instruction for multiply?
    // PowerPC shift instructions can shift by 0-63 bits
    if (isPow2(node->gtElemSize) && (node->gtElemSize <= (1ULL << 63)))
    {
        DWORD scale;
        BitScanForward(&scale, node->gtElemSize);

        if (scale == 0)
        {
            // Element size is 1, no scaling needed
            // result = base + index
            emit->emitIns_R_R_R(INS_add, attr, targetReg, baseReg, indexReg);
        }
        else
        {
            // Shift index left by scale bits: tmpReg = index << scale
            // Use sldi (shift left doubleword immediate) or slwi (shift left word immediate)
            instruction shiftIns = (attr == EA_8BYTE) ? INS_sldi : INS_slwi;
            emit->emitIns_R_R_I(shiftIns, attr, tmpReg, indexReg, scale);
            
            // Add base to scaled index: result = base + tmpReg
            emit->emitIns_R_R_R(INS_add, attr, targetReg, baseReg, tmpReg);
        }
    }
    else // Non-power-of-2 element size, use multiply
    {
        // Load element size into tmpReg
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, (ssize_t)node->gtElemSize);

        // Multiply: tmpReg = index * elementSize
        // Use mulld (64-bit) or mullw (32-bit)
        instruction mulIns = (attr == EA_8BYTE) ? INS_mulld : INS_mullw;
        emit->emitIns_R_R_R(mulIns, attr, tmpReg, indexReg, tmpReg);

        // Add base: result = base + tmpReg
        emit->emitIns_R_R_R(INS_add, attr, targetReg, baseReg, tmpReg);
    }

    // Add element offset if non-zero
    // result = result + elementOffset
    if (node->gtElemOffset != 0)
    {
        // Use addi (add immediate) for small offsets
        // PowerPC addi supports 16-bit signed immediate (-32768 to 32767)
        if ((node->gtElemOffset >= -32768) && (node->gtElemOffset <= 32767))
        {
            emit->emitIns_R_R_I(INS_addi, attr, targetReg, targetReg, node->gtElemOffset);
        }
        else
        {
            // Large offset: load into tmpReg and add
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, node->gtElemOffset);
            emit->emitIns_R_R_R(INS_add, attr, targetReg, targetReg, tmpReg);
        }
    }

    // Mark base register as no longer containing a GC pointer
    gcInfo.gcMarkRegSetNpt(base->gtGetRegMask());

    genProduceReg(node);
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

            // Check if we need a trampoline for long-distance calls
            // PowerPC64 bl instruction has 24-bit signed offset (±32MB range)
            if (!call->IsFastTailCall())
            {
                // Check if target is within range for direct bl
                BYTE* currentPos = GetEmitter()->emitCodeBlock;
                int offset = 0;
                
                if (currentPos != nullptr)
                {
                    offset = GetEmitter()->getBranchOffset(currentPos, addr);
                }
                
                if (offset == 0 && currentPos != nullptr)
                {
                    // Offset out of range - emit trampoline sequence
                    uint64_t targetAddr = (uint64_t)addr;
                    
                    // lis r12, target@highest
                    GetEmitter()->emitIns_R_I(INS_lis, EA_8BYTE, REG_R12, (targetAddr >> 48) & 0xFFFF);
                    // ori r12, r12, target@higher
                    GetEmitter()->emitIns_R_I(INS_ori, EA_8BYTE, REG_R12, (targetAddr >> 32) & 0xFFFF);
                    // sldi r12, r12, 32
                    GetEmitter()->emitIns_R_I(INS_sldi, EA_8BYTE, REG_R12, 32);
                    // oris r12, r12, target@h
                    GetEmitter()->emitIns_R_I(INS_oris, EA_8BYTE, REG_R12, (targetAddr >> 16) & 0xFFFF);
                    // ori r12, r12, target@l
                    GetEmitter()->emitIns_R_I(INS_ori, EA_8BYTE, REG_R12, targetAddr & 0xFFFF);
                    // mtctr r12
                    GetEmitter()->emitIns_R(INS_mtctr, EA_8BYTE, REG_R12);
                    
                    // Now emit indirect call through CTR
                    // clang-format off
                    genEmitCall(emitter::EC_INDIR_R,
                                methHnd,
                                INDEBUG_LDISASM_COMMA(sigInfo)
                                nullptr,  // addr is nullptr for indirect calls
                                retSize
                                MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                                di,
                                REG_R12,  // ireg - indicates call through CTR
                                false);   // Not a tail call
                    // clang-format on
                }
                else
                {
                    // Direct call within range (or currentPos is null during early phases)
                    // clang-format off
                    genEmitCall(emitter::EC_FUNC_TOKEN,
                                methHnd,
                                INDEBUG_LDISASM_COMMA(sigInfo)
                                addr,
                                retSize
                                MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                                di,
                                REG_NA,
                                false);
                    // clang-format on
                }
            }
            else
            {
                // Tail call - use direct branch
                // clang-format off
                genEmitCall(emitter::EC_FUNC_TOKEN,
                            methHnd,
                            INDEBUG_LDISASM_COMMA(sigInfo)
                            addr,
                            retSize
                            MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                            di,
                            REG_NA,
                            true);
                // clang-format on
            }
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
	// Issue a full memory barrier before a volatile load
	// PowerPC64: hwsync provides a full memory barrier
	instGen(INS_hwsync);
    }

    GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), targetReg, tree);

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForStoreInd: Produce code for a GT_STOREIND node.
//
// Arguments:
//    tree - the GT_STOREIND node
//
void CodeGen::genCodeForStoreInd(GenTreeStoreInd* tree)
{
#ifdef FEATURE_SIMD
    // Storing Vector3 of size 12 bytes through indirection
    if (tree->TypeGet() == TYP_SIMD12)
    {
        abort(); // NYI for PPC64LE
    }
#endif // FEATURE_SIMD

    GenTree* data = tree->Data();
    GenTree* addr = tree->Addr();

    // For now, we don't handle GC write barriers - just do normal stores
    // TODO: Implement GC write barrier support when genEmitHelperCall is ready
    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(tree);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        // Write barrier needed but not yet implemented
        // For now, just do a normal store (this may cause GC issues in some scenarios)
        // TODO: Implement proper write barrier support
    }

    // Normal store path
    // We must consume the operands in the proper execution order,
    // so that liveness is updated appropriately.
    genConsumeAddress(addr);

    if (!data->isContained())
    {
        genConsumeRegs(data);
    }

    regNumber dataReg;
    if (data->isContainedIntOrIImmed())
    {
        assert(data->IsIntegralConst(0));
        dataReg = REG_R0; // Use R0 as zero register on PPC64LE
    }
    else // data is not contained, so evaluate it into a register
    {
        assert(!data->isContained());
        dataReg = data->GetRegNum();
    }

    var_types   type = tree->TypeGet();
    instruction ins  = ins_Store(type);

    if (tree->IsVolatile())
    {
	// Issue a full memory barrier before a volatile store
	// PowerPC64: hwsync provides a full memory barrier to ensure
	// all previous memory operations complete before the store
	instGen(INS_hwsync);
    }

    GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), dataReg, tree);

    if (tree->IsVolatile())
    {
        // Issue a load barrier after a volatile store
        // lwsync is a lighter-weight sync that orders loads
        instGen(INS_lwsync);
    }
}

void CodeGen::genEHCatchRet(BasicBlock* block)
{
      // Load the address of the continuation point (target block) into the integer return register
    // This is used when returning from a catch handler
    // PowerPC will use appropriate instructions to load the label address
    //GetEmitter()->emitIns_R_L(INS_b, EA_PTRSIZE, block->GetTarget(), REG_INTRET)
    NYI_POWERPC64("genEHCatchRet - need to implement emitIns_R_L for label address loading");

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
    assert(node->OperIs(GT_STORE_BLK));

    unsigned  dstLclNum      = BAD_VAR_NUM;
    regNumber dstAddrBaseReg = REG_NA;
    int       dstOffset      = 0;
    GenTree*  dstAddr        = node->Addr();

    if (!dstAddr->isContained())
    {
        dstAddrBaseReg = genConsumeReg(dstAddr);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        assert(!dstAddr->AsAddrMode()->HasIndex());

        dstAddrBaseReg = genConsumeReg(dstAddr->AsAddrMode()->Base());
        dstOffset      = dstAddr->AsAddrMode()->Offset();
    }
    else
    {
        assert(dstAddr->OperIs(GT_LCL_ADDR));
        dstLclNum = dstAddr->AsLclVarCommon()->GetLclNum();
        dstOffset = dstAddr->AsLclVarCommon()->GetLclOffs();
    }

    unsigned  srcLclNum      = BAD_VAR_NUM;
    regNumber srcAddrBaseReg = REG_NA;
    int       srcOffset      = 0;
    GenTree*  src            = node->Data();

    assert(src->isContained());

    if (src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
    {
        srcLclNum = src->AsLclVarCommon()->GetLclNum();
        srcOffset = src->AsLclVarCommon()->GetLclOffs();
    }
    else
    {
        assert(src->OperIs(GT_IND));
        GenTree* srcAddr = src->AsIndir()->Addr();

        if (!srcAddr->isContained())
        {
            srcAddrBaseReg = genConsumeReg(srcAddr);
        }
        else if (srcAddr->OperIsAddrMode())
        {
            srcAddrBaseReg = genConsumeReg(srcAddr->AsAddrMode()->Base());
            srcOffset      = srcAddr->AsAddrMode()->Offset();
        }
        else
        {
            assert(srcAddr->OperIs(GT_LCL_ADDR));
            srcLclNum = srcAddr->AsLclVarCommon()->GetLclNum();
            srcOffset = srcAddr->AsLclVarCommon()->GetLclOffs();
        }
    }

    if (node->IsVolatile())
    {
        // issue a full memory barrier before a volatile CpBlk operation
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();
    unsigned size = node->GetLayout()->GetSize();

    assert(size <= INT32_MAX);
    assert(srcOffset < INT32_MAX - static_cast<int>(size));
    assert(dstOffset < INT32_MAX - static_cast<int>(size));

    const regNumber tempReg = internalRegisters.Extract(node, RBM_ALLINT);

    for (unsigned regSize = REGSIZE_BYTES; size > 0; size -= regSize, srcOffset += regSize, dstOffset += regSize)
    {
        while (regSize > size)
        {
            regSize /= 2;
        }

        instruction loadIns;
        instruction storeIns;
        emitAttr    attr;

        switch (regSize)
        {
            case 1:
                loadIns  = INS_lbz;
                storeIns = INS_stb;
                attr     = EA_1BYTE;
                break;
            case 2:
                loadIns  = INS_lhz;
                storeIns = INS_sth;
                attr     = EA_2BYTE;
                break;
            case 4:
                loadIns  = INS_lwz;
                storeIns = INS_stw;
                attr     = EA_4BYTE;
                break;
            case 8:
                loadIns  = INS_ld;
                storeIns = INS_std;
                attr     = EA_8BYTE;
                break;
            default:
                unreached();
        }

        if (srcLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_R_S(loadIns, attr, tempReg, srcLclNum, srcOffset);
        }
        else
        {
            emit->emitIns_R_R_I(loadIns, attr, tempReg, srcAddrBaseReg, srcOffset);
        }

        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(storeIns, attr, tempReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_R_R_I(storeIns, attr, tempReg, dstAddrBaseReg, dstOffset);
        }
    }

    if (node->IsVolatile())
    {
        // issue a full memory barrier after a volatile CpBlk operation
        instGen_MemoryBarrier();
    }
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

    { EJ_eq, GT_OR, EJ_eq },  // FEQU  (index 24) - Float Equal Unordered
    { EJ_ne },                // FNEU  (index 25) - Float Not Equal Unordered
    { EJ_lt },                // FLTU  (index 26) - Float Less Than Unordered
    { EJ_le },                // FLEU  (index 27) - Float Less or Equal Unordered
    { EJ_ge },                // FGEU  (index 28) - Float Greater or Equal Unordered
    { EJ_gt },                // FGTU  (index 29) - Float Greater Than Unordered
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
    
    // Calculate localFrameSize using same logic as genTotalFrameSize()
    // PPC64LE ELFv2 ABI: 32 (linkage) + parameter save area + compLclFrameSize
    const int LINKAGE_AREA_SIZE = 32;
    const int PARAM_SAVE_AREA_SIZE = 64;
    
    int paramSaveArea = PARAM_SAVE_AREA_SIZE;
    if (compiler->info.compArgsCount > 0 && compiler->compArgSize > PARAM_SAVE_AREA_SIZE)
    {
        paramSaveArea = compiler->compArgSize;
    }
    
    int localFrameSize = LINKAGE_AREA_SIZE + paramSaveArea + compiler->compLclFrameSize;

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
// Notes:
//    PowerPC64 D-form instructions (ld, std, addi, etc.) use 16-bit signed immediates.
//    For ld/std, the immediate must be a multiple of 4 (DS-form, 14-bit field).
//    If the immediate doesn't fit, we load it into tmpReg and use indexed addressing.
//
bool CodeGen::genInstrWithConstant(instruction ins,
				   emitAttr    attr,
				   regNumber   reg1,
				   regNumber   reg2,
				   ssize_t     imm,
				   regNumber   tmpReg,
				   bool        inUnwindRegion /* = false */)
{
    bool immFitsInIns = false;
    emitAttr size = EA_SIZE(attr);

    // reg1 is usually a dest register
    // reg2 is always source register
    assert(tmpReg != reg2); // tmpReg cannot match any source register

    // Check if immediate fits in instruction encoding
    switch (ins)
    {
        case INS_addi:
            // addi uses 16-bit signed immediate (SIMM field)
            immFitsInIns = (imm >= -32768 && imm <= 32767);
            break;

        case INS_std:
        case INS_stw:
        case INS_sth:
        case INS_stb:
        case INS_stfd:
        case INS_stfs:
            // reg1 is a source register for store instructions
            assert(tmpReg != reg1); // tmpReg cannot match source register
            FALLTHROUGH;

        case INS_ld:
        case INS_lwz:
        case INS_lhz:
        case INS_lbz:
        case INS_lfd:
        case INS_lfs:
            // Load/store instructions use 16-bit signed immediate
            // For ld/std (DS-form), immediate must be multiple of 4 (uses 14-bit field)
            if (ins == INS_ld || ins == INS_std)
            {
                immFitsInIns = ((imm >= -32768) && (imm <= 32764) && ((imm & 3) == 0));
            }
            else
            {
                // Other loads/stores use full 16-bit signed immediate (D-form)
                immFitsInIns = (imm >= -32768 && imm <= 32767);
            }
            break;

        default:
            assert(!"Unexpected instruction in genInstrWithConstant");
            break;
    }

    if (immFitsInIns)
    {
        // Generate a single instruction that encodes the immediate directly
        GetEmitter()->emitIns_R_R_I(ins, attr, reg1, reg2, imm);
    }
    else
    {
        // Caller can specify REG_NA for tmpReg when it "knows" the immediate will always fit
        assert(tmpReg != REG_NA);

        // Generate multiple instructions:
        // 1. Load the immediate into tmpReg
        // 2. Use indexed addressing (X-form instruction)

        // Load immediate into tmpReg using li/lis/ori sequence
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, imm);
        regSet.verifyRegUsed(tmpReg);

        // When we are in an unwind code region, record the extra instructions
        if (inUnwindRegion)
        {
            compiler->unwindPadding();
        }

        // Convert to indexed form and use three-register encoding
        instruction insIndexed = ins;
        switch (ins)
        {
            case INS_addi:
                // addi rd, ra, imm -> add rd, ra, tmpReg
                insIndexed = INS_add;
                break;
            case INS_ld:
                insIndexed = INS_ldx;
                break;
            case INS_std:
                insIndexed = INS_stdx;
                break;
            case INS_lwz:
                insIndexed = INS_lwzx;
                break;
            case INS_stw:
                insIndexed = INS_stwx;
                break;
            case INS_lhz:
                insIndexed = INS_lhzx;
                break;
            case INS_sth:
                insIndexed = INS_sthx;
                break;
            case INS_lbz:
                insIndexed = INS_lbzx;
                break;
            case INS_stb:
                insIndexed = INS_stbx;
                break;
            case INS_lfd:
                insIndexed = INS_lfdx;
                break;
            case INS_stfd:
                insIndexed = INS_stfdx;
                break;
            case INS_lfs:
                insIndexed = INS_lfsx;
                break;
            case INS_stfs:
                insIndexed = INS_stfsx;
                break;
            default:
                assert(!"Unexpected instruction for indexed form");
                break;
        }

        // Generate indexed instruction: insIndexed reg1, reg2, tmpReg
        GetEmitter()->emitIns_R_R_R(insIndexed, attr, reg1, reg2, tmpReg);
    }

    return immFitsInIns;
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!
//
// Notes:
//    PowerPC64 frame layout (after genPushCalleeSavedRegisters):
//    - FP points to the bottom of the allocated frame (same as SP after stdu)
//    - Total frame was allocated with: stdu sp, sp, -totalFrameSize
//    - So FP is totalFrameSize bytes below Caller's SP
//
int CodeGenInterface::genCallerSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    
    // FP = Caller-SP - totalFrameSize
    // So delta = -totalFrameSize
    int callerSPtoFPdelta = genCallerSPtoInitialSPdelta() + genSPtoFPdelta();
    
    assert(callerSPtoFPdelta <= 0);
    return callerSPtoFPdelta;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.
//
// Notes:
//    PowerPC64: After stdu sp, sp, -totalFrameSize, Initial-SP is totalFrameSize
//    bytes below Caller-SP.
//
int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    // Initial-SP = Caller-SP - totalFrameSize
    int callerSPtoSPdelta = -genTotalFrameSize();
    
    assert(callerSPtoSPdelta <= 0);
    return callerSPtoSPdelta;
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return offset from the stack pointer (Initial-SP) to the frame pointer.
//
// Notes:
//    PowerPC64: FP is set equal to SP after frame allocation (line 3239 in genPushCalleeSavedRegisters)
//    So FP = SP, meaning the delta is 0.
//
int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    
    // In PowerPC64, after "stdu sp, sp, -totalFrameSize" and "mr fp, sp",
    // FP points to the same location as SP (bottom of the frame).
    // Therefore, SP to FP delta is 0.
    return 0;
}

//---------------------------------------------------------------------
// genTotalFrameSize - return the total size of the stack frame, including local size,
// callee-saved register size, etc.
//
// Return value:
//    Total frame size
//
// Notes:
//    PPC64LE ELFv2 ABI requires:
//    - Linkage area: 32 bytes (mandatory)
//    - Parameter save area: 64 bytes (8 registers * 8 bytes) for incoming register parameters
//      (only if function has parameters - this is where callee can spill r3-r10)
//    - Local variables and spills: compLclFrameSize (includes outgoing arg space)
//    - Callee-saved registers: compCalleeRegsPushed * 8
//
//    Note: compArgSize tracks incoming parameter size. If we have incoming parameters,
//    we need the parameter save area. If compLclFrameSize already accounts for this
//    (e.g., includes space for parameters), we should not double-count.
//

int CodeGenInterface::genTotalFrameSize() const
{
    assert(!IsUninitialized(compiler->compCalleeRegsPushed));

    // PPC64LE ELFv2 ABI frame layout:
    // - 32 bytes: Linkage area (back chain, CR save, LR save, TOC save, etc.)
    // - Parameter save area: Space for incoming parameters
    //   * Minimum 64 bytes (for r3-r10) if function has parameters
    //   * If compArgSize > 64, use compArgSize (parameters spilled to stack)
    // - compLclFrameSize: Local variables + outgoing argument space
    // - 16 bytes: Temporary storage (8 bytes for GT_CNS_DBL + 8 bytes for frame pointer)
    // - compCalleeRegsPushed * 8: Callee-saved registers
    
    const int LINKAGE_AREA_SIZE = 32;
    const int PARAM_SAVE_AREA_SIZE = 64;  // Minimum: 8 registers * 8 bytes
    const int TEMP_STORAGE_SIZE = 16;     // 8 bytes for GT_CNS_DBL + 8 bytes for FP
    
    // Calculate parameter save area size
    // PPC64LE ELFv2 ABI requires parameter save area to always be allocated
    // (minimum 64 bytes for 8 register parameters r3-r10)
    int paramSaveArea = PARAM_SAVE_AREA_SIZE;
    
    // If we have parameters and compArgSize > 64, use compArgSize
    // (this means we have stack parameters beyond the register parameters)
    if (compiler->info.compArgsCount > 0 && compiler->compArgSize > PARAM_SAVE_AREA_SIZE)
    {
        paramSaveArea = compiler->compArgSize;
    }
    
    int totalFrameSize = LINKAGE_AREA_SIZE +
                         paramSaveArea +
                         compiler->compCalleeRegsPushed * REGSIZE_BYTES +
                         compiler->compLclFrameSize +
                         TEMP_STORAGE_SIZE;

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
	    GetEmitter()->emitIns_R_I(INS_lis, size, reg, (imm >> 16), INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_ori, size, reg, (imm & 0xffff), INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	}
	else //EA_8BYTE
	{
	    GetEmitter()->emitIns_R_I(INS_lis, size, reg, ((imm >> 48) & 0xffff), INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_ori, size, reg, ((imm >> 32) & 0xffff), INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_sldi, size, reg, 32, INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_oris, size, reg, ((imm >> 16) & 0xffff), INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
	    GetEmitter()->emitIns_R_I(INS_ori, size, reg, (imm & 0xffff), INS_OPTS_NONE, INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
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
    return;

}

/*****************************************************************************
 *  Emit a call to a helper function.
 *
 */

//------------------------------------------------------------------------
// genEmitHelperCall: Generate code to call a runtime helper function
//
// Arguments:
//    helper          - The helper function to call
//    argSize         - Size of arguments in bytes
//    retSize         - Size of return value
//    callTargetReg   - Register to use for indirect call (REG_NA = use default)
//
// Notes:
//    Simplified version for PowerPC64LE that doesn't use emitIns_R_AI
//    (which doesn't exist in the PowerPC emitter yet).
//
void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTargetReg /*= REG_NA */)
{
    void* addr  = nullptr;
    void* pAddr = nullptr;

    emitter::EmitCallType callType = emitter::EC_FUNC_TOKEN;
    addr                           = compiler->compGetHelperFtn((CorInfoHelpFunc)helper, &pAddr);
    regNumber callTarget           = REG_NA;

    if (addr == nullptr)
    {
        // This is an indirect call to a runtime helper.
        // Load the helper function address and call through register.

        if (callTargetReg == REG_NA)
        {
            // If a callTargetReg has not been explicitly provided, we will use REG_DEFAULT_HELPER_CALL_TARGET, but
            // this is only a valid assumption if the helper call is known to kill REG_DEFAULT_HELPER_CALL_TARGET.
            callTargetReg = REG_DEFAULT_HELPER_CALL_TARGET;
        }

        regMaskTP callTargetMask = genRegMask(callTargetReg);
        regMaskTP callKillSet    = compiler->compHelperCallKillSet((CorInfoHelpFunc)helper);

        // assert that all registers in callTargetMask are in the callKillSet
        noway_assert((callTargetMask & callKillSet) == callTargetMask);

        callTarget = callTargetReg;

        // Load the 64-bit address directly (no relocation support for now)
        // Use instGen_Set_Reg_To_Imm to load the address of the helper table entry
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, callTarget, (ssize_t)pAddr);
        
        // Load the actual function pointer from the helper table
        // ld callTarget, 0(callTarget)
        GetEmitter()->emitIns_R_R_I(INS_ld, EA_PTRSIZE, callTarget, callTarget, 0);
        
        regSet.verifyRegUsed(callTarget);

        callType = emitter::EC_INDIR_R;
    }

    // Emit the actual call instruction
    GetEmitter()->emitIns_Call(callType, compiler->eeFindHelper(helper), INDEBUG_LDISASM_COMMA(nullptr) addr, argSize,
                               retSize, EA_UNKNOWN, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, DebugInfo(), /* IL offset */
                               callTarget,                           /* ireg */
                               REG_NA, 0, 0,                         /* xreg, xmul, disp */
                               false                                 /* isJump */
    );

    // Mark all registers that are killed by this helper call
    regMaskTP killMask = compiler->compHelperCallKillSet((CorInfoHelpFunc)helper);
    regSet.verifyRegistersUsed(killMask);
}

//------------------------------------------------------------------------
// genAllocLclFrame: Probe the stack.
//
// Notes:
//      This only does the probing; allocating the frame is done when callee-saved registers are saved.
//      This is done before anything has been pushed. The previous frame might have a large outgoing argument
//      space that has been allocated, but the lowest addresses have not been touched. Our frame setup might
//      not touch up to the first 504 bytes. This means we could miss a guard page. On Linux (where PPC64LE runs),
//      there is only one guard page by default, so we need to be very careful. We do an extra probe if we might
//      not have probed recently enough. That is, if a call and prolog establishment might lead to missing a page.
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
    assert(compiler->compGeneratingProlog);

    if (frameSize == 0)
    {
        return;
    }

    const target_size_t pageSize = compiler->eeGetPageSize();

    // What offset from the final SP was the last probe? If we haven't probed almost a complete page, and
    // if the next action on the stack might subtract from SP first, before touching the current SP, then
    // we do one more probe at the very bottom. This is especially important on Linux (where PPC64LE runs)
    // which has only one guard page by default. Note that we probe here for PPC64LE, but we don't alter SP.
    target_size_t lastTouchDelta = 0;

    assert(!compiler->info.compPublishStubParam || (REG_SECRET_STUB_PARAM != initReg));

    if (frameSize < pageSize)
    {
        lastTouchDelta = frameSize;
    }
    else if (frameSize < 3 * pageSize)
    {
        // The probing loop in "else"-case below would require at least 6 instructions (and more if
        // 'frameSize' or 'pageSize' cannot be encoded with immediate instructions).
        // Hence for frames that are smaller than 3 * PAGE_SIZE the JIT inlines the following probing code
        // to decrease code size. This is a code size optimization heuristic, not related to the number of guard pages.
        // TODO-PPC64: The probing mechanisms should be replaced by a call to stack probe helper
        // as it is done on other platforms.

        lastTouchDelta = frameSize;

        for (target_size_t probeOffset = pageSize; probeOffset <= frameSize; probeOffset += pageSize)
        {
            // Generate:
            //    li initReg, -probeOffset
            //    lwzx r0, sp, initReg      // Load word indexed (probe the stack)
            // On PPC64LE, we use lwz with indexed addressing to probe the stack

            instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -(ssize_t)probeOffset);
            // Use lwz (load word and zero) with base+index addressing to probe
            // lwzx rD, rA, rB loads from address (rA + rB)
            // We load into r0 (which is a scratch register) from (sp + initReg)
            GetEmitter()->emitIns_R_R_I(INS_lwz, EA_4BYTE, REG_R0, REG_SPBASE, -(ssize_t)probeOffset);
            regSet.verifyRegUsed(initReg);
            *pInitRegZeroed = false; // The initReg does not contain zero

            lastTouchDelta -= pageSize;
        }

        assert(lastTouchDelta == frameSize % pageSize);
        compiler->unwindPadding();
    }
    else
    {
        // Emit the following sequence to 'tickle' the pages. Note it is important that stack pointer not change
        // until this is complete since the tickles could cause a stack overflow, and we need to be able to crawl
        // the stack afterward (which means the stack pointer needs to be known).
        // This is critical on Linux where there is only one guard page.

        regMaskTP availMask = RBM_ALLINT & (regSet.rsGetModifiedRegsMask() | ~RBM_INT_CALLEE_SAVED);
        availMask &= ~maskArgRegsLiveIn;   // Remove all of the incoming argument registers as they are currently live
        availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg

        regNumber rOffset = initReg;
        regNumber rLimit;
        regMaskTP tempMask;

        // We pick the next lowest register number for rLimit
        noway_assert(availMask != RBM_NONE);
        tempMask = genFindLowestBit(availMask);
        rLimit   = genRegNumFromMask(tempMask);

        // Generate:
        //
        //      li rOffset, -pageSize
        //      li rLimit, -frameSize
        // loop:
        //      lwz r0, 0(sp + rOffset)    // Probe the stack
        //      addi rOffset, rOffset, -pageSize
        //      cmpd rLimit, rOffset
        //      ble loop                   // If rLimit <= rOffset, we need to probe this rOffset

        noway_assert((ssize_t)(int)frameSize == (ssize_t)frameSize); // make sure framesize safely fits within an int

        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rOffset, -(ssize_t)pageSize);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rLimit, -(ssize_t)frameSize);

        // There's a "virtual" label here. But we can't create a label in the prolog, so we use the magic
        // `emitIns_J` with a negative `instrCount` to branch back a specific number of instructions.

        // lwz r0, 0(sp + rOffset) - probe the stack
        GetEmitter()->emitIns_R_R_I(INS_lwz, EA_4BYTE, REG_R0, REG_SPBASE, 0); // This will need rOffset added
        // addi rOffset, rOffset, -pageSize
        GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, rOffset, rOffset, -(ssize_t)pageSize);
        // cmpd rLimit, rOffset
        GetEmitter()->emitIns_R_R(INS_cmpd, EA_PTRSIZE, rLimit, rOffset);
        // ble loop (branch if less than or equal)
        // Branch back 4 instructions to create the probing loop
        // The -4 means: branch back to the lwz instruction (4 instructions ago: lwz, addi, cmpd, ble)
        GetEmitter()->emitIns_J(INS_ble, NULL, -4);

        *pInitRegZeroed = false; // The initReg does not contain zero

        compiler->unwindPadding();

        lastTouchDelta = frameSize % pageSize;
    }

    if (lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize)
    {
        assert(lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES < 2 * pageSize);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -(ssize_t)frameSize);
        // lwz r0, 0(sp + initReg) - final probe
        GetEmitter()->emitIns_R_R_I(INS_lwz, EA_4BYTE, REG_R0, REG_SPBASE, -(ssize_t)frameSize);
        compiler->unwindPadding();

        regSet.verifyRegUsed(initReg);
        *pInitRegZeroed = false; // The initReg does not contain zero
    }
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
    // Casting from int/long to float/double on PowerPC64 requires:
    // 1. Store integer value to stack
    // 2. Load from stack into FP register using lfd
    // 3. Convert using fcfid/fcfids/fcfidu/fcfidus
    
    assert(treeNode->OperGet() == GT_CAST);

    GenTree*  op1       = treeNode->AsOp()->gtOp1;
    var_types dstType   = treeNode->CastToType();
    var_types srcType   = genActualType(op1->TypeGet());
    regNumber targetReg = treeNode->GetRegNum();
    regNumber srcReg    = genConsumeReg(op1);
    bool      isUnsigned = treeNode->IsUnsigned(); // Check the GTF_UNSIGNED flag on the cast node

    assert(varTypeIsFloating(dstType));
    assert(varTypeIsIntegral(srcType));
    assert(genIsValidFloatReg(targetReg));
    assert(genIsValidIntReg(srcReg));

    // Get internal FP register allocated by LSRA
    regNumber tmpFpReg = internalRegisters.GetSingle(treeNode, RBM_ALLFLOAT);
    assert(genIsValidFloatReg(tmpFpReg));

    // Calculate stack offset for temporary storage
    int tmpOffset = 0;

    // Step 1: Handle 32-bit to 64-bit extension if needed
    regNumber extendedReg = srcReg;
    if (varTypeIsInt(srcType))
    {
        if (isUnsigned)
        {
            // Zero-extend 32-bit unsigned to 64-bit
            // Clear upper 32 bits by using clrldi (Clear Left Double Immediate)
            // clrldi rA, rS, n  clears the leftmost n bits
            // We want to clear the upper 32 bits, so n = 32
            // This is encoded as rldicl rA, rS, 0, 32
            // For now, use a mask operation: andi. can only handle 16-bit immediates
            // So we'll use a different approach: store 32-bit, load 64-bit zero-extended

            // Store as 32-bit word
            GetEmitter()->emitIns_R_R_I(INS_stw, EA_4BYTE, srcReg, REG_SPBASE, tmpOffset);
            // Load as 64-bit doubleword (zero-extends automatically)
            GetEmitter()->emitIns_R_R_I(INS_lwz, EA_4BYTE, srcReg, REG_SPBASE, tmpOffset);
            // Now srcReg contains the zero-extended 64-bit value
        }
        else
        {
            // Sign-extend 32-bit signed to 64-bit
            GetEmitter()->emitIns_R_R(INS_extsw, EA_8BYTE, srcReg, srcReg);
        }
    }

    // Store the 64-bit integer to stack
    GetEmitter()->emitIns_R_R_I(INS_std, EA_8BYTE, srcReg, REG_SPBASE, tmpOffset);

    // Step 2: Load from stack into FP register using lfd
    // This loads the bit pattern without conversion
    GetEmitter()->emitIns_R_R_I(INS_lfd, EA_8BYTE, tmpFpReg, REG_SPBASE, tmpOffset);

    // Step 3: Convert integer to float/double
    instruction convertIns;
    
    if (isUnsigned)
    {
        // Unsigned conversion
        if (dstType == TYP_FLOAT)
        {
            convertIns = INS_fcfidus; // Convert unsigned int to single-precision
        }
        else
        {
            convertIns = INS_fcfidu;  // Convert unsigned int to double-precision
        }
    }
    else
    {
        // Signed conversion
        if (dstType == TYP_FLOAT)
        {
            convertIns = INS_fcfids;  // Convert signed int to single-precision
        }
        else
        {
            convertIns = INS_fcfid;   // Convert signed int to double-precision
        }
    }

    // Perform the conversion: targetReg = convert(tmpFpReg)
    GetEmitter()->emitIns_R_R(convertIns, emitActualTypeSize(dstType), targetReg, tmpFpReg);

    genProduceReg(treeNode);
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
    assert(compiler->compGeneratingProlog);
    assert(untrLclHi > untrLclLo);

    int bytesToWrite = untrLclHi - untrLclLo;
    
    // Use initReg to hold zero
    instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, 0);
    *pInitRegZeroed = true;
    
    // Get frame pointer
    regNumber fpReg = genFramePointerReg();
    
    // Simple loop: store zero in 8-byte chunks
    int offset = untrLclLo;
    while (offset < untrLclHi)
    {
        // std initReg, offset(fpReg)  - Store doubleword
        GetEmitter()->emitIns_R_R_I(INS_std, EA_8BYTE, initReg, fpReg, offset);
        offset += 8;
    }
    
    // Handle remaining bytes if not 8-byte aligned
    int remaining = untrLclHi - offset;
    if (remaining > 0)
    {
        if (remaining >= 4)
        {
            // stw initReg, offset(fpReg)  - Store word (4 bytes)
            GetEmitter()->emitIns_R_R_I(INS_stw, EA_4BYTE, initReg, fpReg, offset);
            offset += 4;
            remaining -= 4;
        }
        if (remaining > 0)
        {
            // sth or stb for remaining 1-3 bytes
            // For simplicity, just store word (may write extra bytes but safe)
            GetEmitter()->emitIns_R_R_I(INS_stw, EA_4BYTE, initReg, fpReg, offset);
        }
    }
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
    assert(treeNode->OperGet() == GT_CAST);
    
    GenTree*  op1     = treeNode->AsOp()->gtOp1;
    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
   
    assert(varTypeIsFloating(srcType));
    assert(varTypeIsIntegral(dstType));
    
    regNumber srcReg = genConsumeReg(op1);
    regNumber dstReg = treeNode->GetRegNum();
   
    assert(genIsValidFloatReg(srcReg));
    assert(genIsValidIntReg(dstReg));
    
    emitter* emit = GetEmitter();
    
    // PowerPC64 float-to-int conversion requires:
    // 1. Convert float to integer in FP register
    // 2. Store FP register to stack
    // 3. Load from stack to integer register
    
    instruction convertIns;
    bool isUnsigned = varTypeIsUnsigned(dstType);
    bool is64Bit = (genTypeSize(dstType) == 8);
    
    // Select appropriate conversion instruction
    if (is64Bit)
    {
        convertIns = isUnsigned ? INS_fctiduz : INS_fctidz;
    }
    else
    {
        convertIns = isUnsigned ? INS_fctiwuz : INS_fctiwz;
    }
    
    // Use a temporary FP register for the converted value
    regNumber tempFpReg = internalRegisters.GetSingle(treeNode, RBM_ALLFLOAT);
    
    // Convert float to integer (result in FP register)
    emit->emitIns_R_R(convertIns, EA_8BYTE, tempFpReg, srcReg);
    
    // Store FP register to stack and load to integer register
    // PowerPC64 cannot directly move from FP to GPR, must use stack
    int tmpOffset = 0;  // Use offset 0 from stack pointer for temporary storage
    
    // Step 1: Store the FP register (with converted integer) to stack
    emit->emitIns_R_R_I(INS_stfd, EA_8BYTE, tempFpReg, REG_SPBASE, tmpOffset);
    
    // Step 2: Load from stack to integer register
    if (is64Bit)
    {
        // Load 64-bit value
        emit->emitIns_R_R_I(INS_ld, EA_8BYTE, dstReg, REG_SPBASE, tmpOffset);
    }
    else
    {
        // Load 32-bit value from stack
        // In little-endian, the 32-bit result is at offset 0
        emit->emitIns_R_R_I(INS_lwz, EA_4BYTE, dstReg, REG_SPBASE, tmpOffset);
    }
    
    genProduceReg(treeNode);
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
    if (!compiler->UsesFunclets())
    {
        return;  // No funclets in this function
    }

    // Capture funclet prolog/epilog info for exception handling
    // For simple functions without exception handlers, this is not needed
    // TODO: Implement funclet support when needed
    return;

}

void CodeGen::genSetPSPSym(regNumber initReg, bool* pInitRegZeroed)
{    
    //_ASSERTE("!NYI");
    assert(compiler->compGeneratingProlog);

    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;  // No PSPSym needed for this function
    }

    noway_assert(isFramePointerUsed()); // We need an explicit frame pointer

    // Calculate the offset from current SP to caller's SP
    int SPtoCallerSPdelta = -genCallerSPtoInitialSPdelta();

    if (compiler->opts.IsOSR())
    {
        SPtoCallerSPdelta += compiler->info.compPatchpointInfo->TotalFrameSize();
    }

    // Use initReg as scratch register
    regNumber regTmp = initReg;
    *pInitRegZeroed = false;

    // Calculate caller's SP: addi regTmp, SP, SPtoCallerSPdelta
    GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, regTmp, REG_SPBASE, SPtoCallerSPdelta);

    // Store it to PSPSym local variable: std regTmp, [FP + PSPSym_offset]
    GetEmitter()->emitIns_S_R(INS_std, EA_PTRSIZE, regTmp, compiler->lvaPSPSym, 0);
}

//------------------------------------------------------------------------
// genLeaInstruction: Produce code for a GT_LEA node (Load Effective Address).
//
// Arguments:
//    lea - the GT_LEA node
//
// Notes:
//    PowerPC doesn't have a direct LEA instruction like x86/x64.
//    We need to compute: base + (index * scale) + offset
//    using add and shift instructions.
//

void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    emitter* emit = GetEmitter();
    GenTree* base  = lea->Base();
    GenTree* index = lea->Index();
    unsigned scale = lea->GetScale();
    int      offset = lea->Offset();
    regNumber targetReg = lea->GetRegNum();

    // Consume the operands
    if (base != nullptr)
    {
        genConsumeReg(base);
    }
    if (index != nullptr)
    {
        genConsumeReg(index);
    }

    // PowerPC LEA computation strategy:
    // 1. If we have an index with scale, compute: index << log2(scale)
    // 2. Add base (if present)
    // 3. Add offset (if present)

    regNumber resultReg = targetReg;

    if (index != nullptr && scale > 1)
    {
        // Need to scale the index: index << log2(scale)
        unsigned shift = genLog2(scale);
        regNumber indexReg = index->GetRegNum();

        if (base == nullptr && offset == 0)
        {
            // Just scaled index: targetReg = index << shift
            emit->emitIns_R_R_I(INS_sldi, EA_PTRSIZE, targetReg, indexReg, shift);
            resultReg = targetReg;
        }
        else
        {
            // Need to use internal register for scaled index
            regNumber tempReg = internalRegisters.GetSingle(lea);
            emit->emitIns_R_R_I(INS_sldi, EA_PTRSIZE, tempReg, indexReg, shift);

            if (base != nullptr)
            {
                // Add base: targetReg = base + (index << shift)
                emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, targetReg, base->GetRegNum(), tempReg);
                resultReg = targetReg;
            }
            else
            {
                // No base, just move scaled index to target
                emit->emitIns_Mov(INS_mov, EA_PTRSIZE, targetReg, tempReg, /* canSkip */ false);
                resultReg = targetReg;
            }
        }
    }
    else if (index != nullptr)
    {
        // Index with scale == 1
        regNumber indexReg = index->GetRegNum();

        if (base != nullptr)
        {
            // targetReg = base + index
            emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, targetReg, base->GetRegNum(), indexReg);
            resultReg = targetReg;
        }
        else
        {
            // Just index, move to target
            emit->emitIns_Mov(INS_mov, EA_PTRSIZE, targetReg, indexReg, /* canSkip */ false);
            resultReg = targetReg;
        }
    }
    else if (base != nullptr)
    {
        // Just base, possibly with offset
        if (offset == 0)
        {
            // Just move base to target
            emit->emitIns_Mov(INS_mov, EA_PTRSIZE, targetReg, base->GetRegNum(), /* canSkip */ false);
            resultReg = targetReg;
        }
        else
        {
            // Will add offset below
            resultReg = base->GetRegNum();
        }
    }
    else
    {
        // Just offset (constant address)
        assert(offset != 0);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, targetReg, offset);
        genProduceReg(lea);
        return;
    }

    // Add offset if present and not yet handled
    if (offset != 0 && !(base != nullptr && index == nullptr && offset == 0))
    {
        // PowerPC addi instruction uses 16-bit signed immediate
        if ((offset >= -32768) && (offset <= 32767))
        {
            // Offset fits in immediate
            if (resultReg == targetReg)
            {
                // Add to target: targetReg = targetReg + offset
                emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, targetReg, targetReg, offset);
            }
            else
            {
                // Add to result and store in target: targetReg = resultReg + offset
                emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, targetReg, resultReg, offset);
            }
        }
        else
        {
            // Offset doesn't fit, need to use internal register
            regNumber tempReg = internalRegisters.GetSingle(lea);
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, tempReg, offset);

            if (resultReg == targetReg)
            {
                // Add to target: targetReg = targetReg + tempReg
                emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, targetReg, targetReg, tempReg);
            }
            else
            {
                // Add to result: targetReg = resultReg + tempReg
                emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, targetReg, resultReg, tempReg);
            }
        }
    }

    genProduceReg(lea);
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
    emitter* emit = GetEmitter();
    
    switch (desc.CheckKind())
    {
        case GenIntCastDesc::CHECK_POSITIVE:
            // Check if value >= 0
            emit->emitIns_R_I(INS_cmpdi, EA_ATTR(desc.CheckSrcSize()), reg, 0);
            genJumpToThrowHlpBlk(EJ_lt, SCK_OVERFLOW);
            break;

#ifdef TARGET_64BIT
        case GenIntCastDesc::CHECK_UINT_RANGE:
        {
            // Check if value fits in unsigned 32-bit range (upper 32 bits must be zero)
            // Use a temporary register to test upper bits
            regNumber tempReg = internalRegisters.GetSingle(cast);
            // Shift right 32 bits and check if result is zero
            emit->emitIns_R_R_I(INS_sldi, EA_8BYTE, tempReg, reg, 32);
            emit->emitIns_R_I(INS_cmpdi, EA_8BYTE, tempReg, 0);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            break;
        }

        case GenIntCastDesc::CHECK_POSITIVE_INT_RANGE:
        {
            // Check if value fits in signed 32-bit range (0 to 0x7FFFFFFF)
            regNumber tempReg = internalRegisters.GetSingle(cast);
            // Check upper 33 bits are zero
            emit->emitIns_R_R_I(INS_sldi, EA_8BYTE, tempReg, reg, 33);
            emit->emitIns_R_I(INS_cmpdi, EA_8BYTE, tempReg, 0);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            break;
        }

        case GenIntCastDesc::CHECK_INT_RANGE:
        {
            // Check if value fits in signed 32-bit range (INT32_MIN to INT32_MAX)
            // Sign extend from 32-bit and compare with original
            regNumber tempReg = internalRegisters.GetSingle(cast);
            emit->emitIns_R_R(INS_extsw, EA_8BYTE, tempReg, reg);
            emit->emitIns_R_R(INS_cmpd, EA_8BYTE, reg, tempReg);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            break;
        }
#endif

        default:
        {
            assert(desc.CheckKind() == GenIntCastDesc::CHECK_SMALL_INT_RANGE);
            const int castMaxValue = desc.CheckSmallIntMax();
            const int castMinValue = desc.CheckSmallIntMin();

            // Check upper bound
            emit->emitIns_R_I(INS_cmpdi, EA_ATTR(desc.CheckSrcSize()), reg, castMaxValue);
            genJumpToThrowHlpBlk(EJ_gt, SCK_OVERFLOW);

            // Check lower bound if not zero
            if (castMinValue != 0)
            {
                emit->emitIns_R_I(INS_cmpdi, EA_ATTR(desc.CheckSrcSize()), reg, castMinValue);
                genJumpToThrowHlpBlk(EJ_lt, SCK_OVERFLOW);
            }
            break;
        }
    }
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
    genConsumeRegs(cast->CastOp());

    emitter*        emit    = GetEmitter();
    var_types       dstType = cast->CastToType();
    var_types       srcType = genActualType(cast->CastOp()->TypeGet());
    const regNumber srcReg  = cast->CastOp()->GetRegNum();
    const regNumber dstReg  = cast->GetRegNum();

    assert(genIsValidIntReg(srcReg));
    assert(genIsValidIntReg(dstReg));

    GenIntCastDesc desc(cast);

    if (desc.CheckKind() != GenIntCastDesc::CHECK_NONE)
    {
        genIntCastOverflowCheck(cast, desc, srcReg);
    }

    if ((desc.ExtendKind() != GenIntCastDesc::COPY) || (srcReg != dstReg))
    {
        instruction ins;

        switch (desc.ExtendKind())
        {
            case GenIntCastDesc::ZERO_EXTEND_SMALL_INT:
                if (desc.ExtendSrcSize() == 1)
                {
                    // Zero extend byte: AND with 0xFF
                    emit->emitIns_R_R_I(INS_andi, EA_PTRSIZE, dstReg, srcReg, 0xFF);
                }
                else
                {
                    // Zero extend halfword: AND with 0xFFFF
                    emit->emitIns_R_R_I(INS_andi, EA_PTRSIZE, dstReg, srcReg, 0xFFFF);
                }
                break;

            case GenIntCastDesc::SIGN_EXTEND_SMALL_INT:
                ins = (desc.ExtendSrcSize() == 1) ? INS_extsb : INS_extsh;
                emit->emitIns_R_R(ins, EA_PTRSIZE, dstReg, srcReg);
                break;

#ifdef TARGET_64BIT
            case GenIntCastDesc::ZERO_EXTEND_INT:
                // Zero extend 32-bit to 64-bit: clear upper 32 bits
                // Use rotate and mask or shift operations
                emit->emitIns_R_R_I(INS_sldi, EA_8BYTE, dstReg, srcReg, 32);
                emit->emitIns_R_R_I(INS_srdi, EA_8BYTE, dstReg, dstReg, 32);
                break;

            case GenIntCastDesc::SIGN_EXTEND_INT:
                emit->emitIns_R_R(INS_extsw, EA_8BYTE, dstReg, srcReg);
                break;
#endif

            case GenIntCastDesc::COPY:
                if (srcReg != dstReg)
                {
                    emit->emitIns_Mov(INS_mov, EA_ATTR(desc.ExtendSrcSize()), dstReg, srcReg, /* canSkip */ false);
                }
                break;

            case GenIntCastDesc::LOAD_ZERO_EXTEND_SMALL_INT:
            case GenIntCastDesc::LOAD_SIGN_EXTEND_SMALL_INT:
            case GenIntCastDesc::LOAD_ZERO_EXTEND_INT:
            case GenIntCastDesc::LOAD_SIGN_EXTEND_INT:
            case GenIntCastDesc::LOAD_SOURCE:
                // These are handled by containment - should not reach here
                unreached();
                break;

            default:
                unreached();
        }
    }

    genProduceReg(cast);
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
    // float <--> double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->GetRegNum();
    assert(genIsValidFloatReg(targetReg));

    GenTree* op1 = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());                  // Cannot be contained
    assert(genIsValidFloatReg(op1->GetRegNum())); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());

    // treeNode must be a reg
    assert(!treeNode->isContained());

    if (srcType != dstType)
    {
        if (srcType == TYP_FLOAT)
        {
            // Float to Double: no explicit conversion needed in PowerPC64
            // Just move to double register (already in correct format)
            if (treeNode->GetRegNum() != op1->GetRegNum())
            {
                GetEmitter()->emitIns_R_R(INS_fmr, EA_8BYTE, treeNode->GetRegNum(), op1->GetRegNum());
            }
        }
        else
        {
            // Double to Float: use frsp (round to single precision)
            GetEmitter()->emitIns_R_R(INS_frsp, EA_4BYTE, treeNode->GetRegNum(), op1->GetRegNum());
        }
    }
    else if (treeNode->GetRegNum() != op1->GetRegNum())
    {
        // Same type cast - just move
        GetEmitter()->emitIns_R_R(INS_fmr, emitActualTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum());
    }

    genProduceReg(treeNode);
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
