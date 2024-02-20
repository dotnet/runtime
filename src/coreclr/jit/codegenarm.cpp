// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        ARM Code Generator                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_ARM
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "emit.h"

//------------------------------------------------------------------------
// genInstrWithConstant: We will typically generate one instruction
//
//    ins  reg1, reg2, imm
//
// However the imm might not fit as a directly encodable immediate.
// When it doesn't fit we generate extra instruction(s) that sets up
// the 'regTmp' with the proper immediate value.
//
//     mov  regTmp, imm
//     ins  reg1, reg2, regTmp
//
// Generally, codegen constants are marked non-containable if they don't fit. This function
// is used for cases that aren't mirrored in the IR, such as in the prolog.
//
// Arguments:
//    ins                 - instruction
//    attr                - operation size and GC attribute
//    reg1, reg2          - first and second register operands
//    imm                 - immediate value (third operand when it fits)
//    flags               - whether flags are set
//    tmpReg              - temp register to use when the 'imm' doesn't fit. Can be REG_NA
//                          if caller knows for certain the constant will fit.
//
// Return Value:
//    returns true if the immediate was small enough to be encoded inside instruction. If not,
//    returns false meaning the immediate was too large and tmpReg was used and modified.
//
bool CodeGen::genInstrWithConstant(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm, insFlags flags, regNumber tmpReg)
{
    bool immFitsInIns = false;

    // reg1 is usually a dest register
    // reg2 is always source register
    assert(tmpReg != reg2); // regTmp cannot match any source register

    switch (ins)
    {
        case INS_add:
        case INS_sub:
            if (imm < 0)
            {
                imm = -imm;
                ins = (ins == INS_add) ? INS_sub : INS_add;
            }
            immFitsInIns = validImmForInstr(ins, (target_ssize_t)imm, flags);
            break;

        default:
            assert(!"Unexpected instruction in genInstrWithConstant");
            break;
    }

    if (immFitsInIns)
    {
        // generate a single instruction that encodes the immediate directly
        GetEmitter()->emitIns_R_R_I(ins, attr, reg1, reg2, (target_ssize_t)imm);
    }
    else
    {
        // caller can specify REG_NA  for tmpReg, when it "knows" that the immediate will always fit
        assert(tmpReg != REG_NA);

        // generate two or more instructions

        // first we load the immediate into tmpReg
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, imm);

        // generate the instruction using a three register encoding with the immediate in tmpReg
        GetEmitter()->emitIns_R_R_R(ins, attr, reg1, reg2, tmpReg);
    }
    return immFitsInIns;
}

//------------------------------------------------------------------------
// genStackPointerAdjustment: add a specified constant value to the stack pointer.
// An available temporary register is required to be specified, in case the constant
// is too large to encode in an "add" instruction (or "sub" instruction if we choose
// to use one), such that we need to load the constant into a register first, before using it.
//
// Arguments:
//    spDelta                 - the value to add to SP (can be negative)
//    tmpReg                  - an available temporary register
//
// Return Value:
//    returns true if the immediate was small enough to be encoded inside instruction. If not,
//    returns false meaning the immediate was too large and tmpReg was used and modified.
//
bool CodeGen::genStackPointerAdjustment(ssize_t spDelta, regNumber tmpReg)
{
    // Even though INS_add is specified here, the encoder will choose either
    // an INS_add or an INS_sub and encode the immediate as a positive value
    //
    return genInstrWithConstant(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta, INS_FLAGS_DONT_CARE, tmpReg);
}

//------------------------------------------------------------------------
// genCallFinally: Generate a call to the finally block.
//
BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    assert(block->KindIs(BBJ_CALLFINALLY));

    GetEmitter()->emitIns_J(INS_bl, block->GetTarget());

    BasicBlock* nextBlock = block->Next();

    if (block->HasFlag(BBF_RETLESS_CALL))
    {
        if ((nextBlock == nullptr) || !BasicBlock::sameEHRegion(block, nextBlock))
        {
            instGen(INS_BREAKPOINT);
        }

        return block;
    }
    else
    {
        assert((nextBlock != nullptr) && nextBlock->isBBCallFinallyPairTail());

        // Because of the way the flowgraph is connected, the liveness info for this one instruction
        // after the call is not (can not be) correct in cases where a variable has a last use in the
        // handler.  So turn off GC reporting for this single instruction.
        GetEmitter()->emitDisableGC();

        BasicBlock* const finallyContinuation = nextBlock->GetFinallyContinuation();

        // Now go to where the finally funclet needs to return to.
        if (nextBlock->NextIs(finallyContinuation) && !compiler->fgInDifferentRegions(nextBlock, finallyContinuation))
        {
            // Fall-through.
            // TODO-ARM-CQ: Can we get rid of this instruction, and just have the call return directly
            // to the next instruction? This would depend on stack walking from within the finally
            // handler working without this instruction being in this special EH region.
            instGen(INS_nop);
        }
        else
        {
            GetEmitter()->emitIns_J(INS_b, finallyContinuation);
        }

        GetEmitter()->emitEnableGC();

        return nextBlock;
    }
}

//------------------------------------------------------------------------
// genEHCatchRet:
void CodeGen::genEHCatchRet(BasicBlock* block)
{
    genMov32RelocatableDisplacement(block->GetTarget(), REG_INTRET);
}

//------------------------------------------------------------------------
// instGen_Set_Reg_To_Imm: Move an immediate value into an integer register.
//
void CodeGen::instGen_Set_Reg_To_Imm(emitAttr  size,
                                     regNumber reg,
                                     ssize_t   imm,
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
        // TODO-CrossBitness: we wouldn't need the cast below if we had CodeGen::instGen_Set_Reg_To_Reloc_Imm.
        genMov32RelocatableImmediate(size, (BYTE*)imm, reg);
    }
    else if (imm == 0)
    {
        instGen_Set_Reg_To_Zero(size, reg, flags);
    }
    else
    {
        // TODO-CrossBitness: we wouldn't need the cast below if we had CodeGen::instGen_Set_Reg_To_Reloc_Imm.
        const int val32 = (int)imm;
        if (validImmForMov(val32))
        {
            GetEmitter()->emitIns_R_I(INS_mov, size, reg, val32, flags);
        }
        else // We have to use a movw/movt pair of instructions
        {
            const int imm_lo16 = val32 & 0xffff;
            const int imm_hi16 = (val32 >> 16) & 0xffff;

            assert(validImmForMov(imm_lo16));
            assert(imm_hi16 != 0);

            GetEmitter()->emitIns_R_I(INS_movw, size, reg, imm_lo16);

            // If we've got a low register, the high word is all bits set,
            // and the high bit of the low word is set, we can sign extend
            // halfword and save two bytes of encoding. This can happen for
            // small magnitude negative numbers 'n' for -32768 <= n <= -1.

            if (GetEmitter()->isLowRegister(reg) && (imm_hi16 == 0xffff) && ((imm_lo16 & 0x8000) == 0x8000))
            {
                GetEmitter()->emitIns_Mov(INS_sxth, EA_4BYTE, reg, reg, /* canSkip */ false);
            }
            else
            {
                GetEmitter()->emitIns_R_I(INS_movt, size, reg, imm_hi16);
            }

            if (flags == INS_FLAGS_SET)
                GetEmitter()->emitIns_Mov(INS_mov, size, reg, reg, /* canSkip */ false, INS_FLAGS_SET);
        }
    }

    regSet.verifyRegUsed(reg);
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
            GenTreeDblCon* dblConst   = tree->AsDblCon();
            double         constValue = dblConst->AsDblCon()->DconValue();
            // TODO-ARM-CQ: Do we have a faster/smaller way to generate 0.0 in thumb2 ISA ?
            if (targetType == TYP_FLOAT)
            {
                // Get a temp integer register
                regNumber tmpReg = tree->GetSingleTempReg();

                float f = forceCastToFloat(constValue);
                instGen_Set_Reg_To_Imm(EA_4BYTE, tmpReg, *((int*)(&f)));
                GetEmitter()->emitIns_Mov(INS_vmov_i2f, EA_4BYTE, targetReg, tmpReg, /* canSkip */ false);
            }
            else
            {
                assert(targetType == TYP_DOUBLE);

                unsigned* cv = (unsigned*)&constValue;

                // Get two temp integer registers
                regNumber tmpReg1 = tree->ExtractTempReg();
                regNumber tmpReg2 = tree->GetSingleTempReg();

                instGen_Set_Reg_To_Imm(EA_4BYTE, tmpReg1, cv[0]);
                instGen_Set_Reg_To_Imm(EA_4BYTE, tmpReg2, cv[1]);

                GetEmitter()->emitIns_R_R_R(INS_vmov_i2d, EA_8BYTE, targetReg, tmpReg1, tmpReg2);
            }
        }
        break;

        case GT_CNS_VEC:
        {
            unreached();
        }

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// genCodeForBinary: Generate code for many binary arithmetic operators
// This method is expected to have called genConsumeOperands() before calling it.
//
// Arguments:
//    treeNode - The binary operation for which we are generating code.
//
// Return Value:
//    None.
//
// Notes:
//    Mul and div are not handled here.
//    See the assert below for the operators that are handled.

void CodeGen::genCodeForBinary(GenTreeOp* treeNode)
{
    const genTreeOps oper       = treeNode->OperGet();
    regNumber        targetReg  = treeNode->GetRegNum();
    var_types        targetType = treeNode->TypeGet();
    emitter*         emit       = GetEmitter();

    assert(treeNode->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_ADD_LO, GT_ADD_HI, GT_SUB_LO, GT_SUB_HI, GT_OR, GT_XOR, GT_AND,
                            GT_AND_NOT));

    GenTree* op1 = treeNode->gtGetOp1();
    GenTree* op2 = treeNode->gtGetOp2();

    instruction ins = genGetInsForOper(oper, targetType);

    // The arithmetic node must be sitting in a register (since it's not contained)
    noway_assert(targetReg != REG_NA);

    if ((oper == GT_ADD_LO || oper == GT_SUB_LO))
    {
        // During decomposition, all operands become reg
        assert(!op1->isContained() && !op2->isContained());
        emit->emitIns_R_R_R(ins, emitTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum(), op2->GetRegNum(),
                            INS_FLAGS_SET);
    }
    else
    {
        regNumber r = emit->emitInsTernary(ins, emitTypeSize(treeNode), treeNode, op1, op2);
        assert(r == targetReg);
    }

    genProduceReg(treeNode);
}

//--------------------------------------------------------------------------------------
// genLclHeap: Generate code for localloc
//
// Description:
//      There are 2 ways depending from build version to generate code for localloc:
//          1) For debug build where memory should be initialized we generate loop
//             which invoke push {tmpReg} N times.
//          2) For non-debug build, we tickle the pages to ensure that SP is always
//             valid and is in sync with the "stack guard page". Amount of iteration
//             is N/eeGetPageSize().
//
// Comments:
//      There can be some optimization:
//          1) It's not needed to generate loop for zero size allocation
//          2) For small allocation (less than 4 store) we unroll loop
//          3) For allocation less than eeGetPageSize() and when it's not needed to initialize
//             memory to zero, we can just decrement SP.
//
// Notes: Size N should be aligned to STACK_ALIGN before any allocation
//
void CodeGen::genLclHeap(GenTree* tree)
{
    assert(tree->OperGet() == GT_LCLHEAP);
    assert(compiler->compLocallocUsed);

    GenTree* size = tree->AsOp()->gtOp1;
    noway_assert((genActualType(size->gtType) == TYP_INT) || (genActualType(size->gtType) == TYP_I_IMPL));

    // Result of localloc will be returned in regCnt.
    // Also it used as temporary register in code generation
    // for storing allocation size
    regNumber            regCnt                   = tree->GetRegNum();
    var_types            type                     = genActualType(size->gtType);
    emitAttr             easz                     = emitTypeSize(type);
    BasicBlock*          endLabel                 = nullptr;
    unsigned             stackAdjustment          = 0;
    regNumber            regTmp                   = REG_NA;
    const target_ssize_t ILLEGAL_LAST_TOUCH_DELTA = (target_ssize_t)-1;
    target_ssize_t       lastTouchDelta =
        ILLEGAL_LAST_TOUCH_DELTA; // The number of bytes from SP to the last stack address probed.

    noway_assert(isFramePointerUsed()); // localloc requires Frame Pointer to be established since SP changes
    noway_assert(genStackLevel == 0);   // Can't have anything on the stack

    // Check to 0 size allocations
    // size_t amount = 0;
    if (size->IsCnsIntOrI())
    {
        // If size is a constant, then it must be contained.
        assert(size->isContained());

        // If amount is zero then return null in regCnt
        size_t amount = size->AsIntCon()->gtIconVal;
        if (amount == 0)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);
            goto BAILOUT;
        }
    }
    else
    {
        // If 0 bail out by returning null in regCnt
        genConsumeRegAndCopy(size, regCnt);
        endLabel = genCreateTempLabel();
        GetEmitter()->emitIns_R_R(INS_TEST, easz, regCnt, regCnt);
        inst_JMP(EJ_eq, endLabel);
    }

    // Setup the regTmp, if there is one.
    if (tree->AvailableTempRegCount() > 0)
    {
        regTmp = tree->ExtractTempReg();
    }

    // If we have an outgoing arg area then we must adjust the SP by popping off the
    // outgoing arg area. We will restore it right before we return from this method.
    if (compiler->lvaOutgoingArgSpaceSize > 0)
    {
        // This must be true for the stack to remain aligned
        assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) == 0);

        // We're guaranteed (by LinearScan::BuildLclHeap()) to have a legal regTmp if we need one.
        genStackPointerAdjustment(compiler->lvaOutgoingArgSpaceSize, regTmp);

        stackAdjustment += compiler->lvaOutgoingArgSpaceSize;
    }

    // Put aligned allocation size to regCnt
    if (size->IsCnsIntOrI())
    {
        // 'amount' is the total number of bytes to localloc to properly STACK_ALIGN
        target_size_t amount = (target_size_t)size->AsIntCon()->gtIconVal;
        amount               = AlignUp(amount, STACK_ALIGN);

        // For small allocations we will generate up to four push instructions (either 2 or 4, exactly,
        // since STACK_ALIGN is 8, and REGSIZE_BYTES is 4).
        static_assert_no_msg(STACK_ALIGN == (REGSIZE_BYTES * 2));
        assert(amount % REGSIZE_BYTES == 0);
        target_size_t pushCount = amount / REGSIZE_BYTES;
        if (pushCount <= 4)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);

            while (pushCount != 0)
            {
                inst_IV(INS_push, (unsigned)genRegMask(regCnt));
                pushCount -= 1;
            }

            lastTouchDelta = 0;

            goto ALLOC_DONE;
        }
        else if (!compiler->info.compInitMem && (amount < compiler->eeGetPageSize())) // must be < not <=
        {
            // Since the size is less than a page, simply adjust the SP value.
            // The SP might already be in the guard page, must touch it BEFORE
            // the alloc, not after.
            GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regCnt, REG_SP, 0);
            inst_RV_IV(INS_sub, REG_SP, amount, EA_PTRSIZE);

            lastTouchDelta = amount;

            goto ALLOC_DONE;
        }

        // regCnt will be the total number of bytes to locAlloc
        instGen_Set_Reg_To_Imm(EA_4BYTE, regCnt, amount);
    }
    else
    {
        // Round up the number of bytes to allocate to a STACK_ALIGN boundary.
        inst_RV_IV(INS_add, regCnt, (STACK_ALIGN - 1), emitActualTypeSize(type));
        inst_RV_IV(INS_AND, regCnt, ~(STACK_ALIGN - 1), emitActualTypeSize(type));
    }

    // Allocation
    if (compiler->info.compInitMem)
    {
        // At this point 'regCnt' is set to the total number of bytes to localloc.
        // Since we have to zero out the allocated memory AND ensure that the stack pointer is always valid
        // by tickling the pages, we will just push 0's on the stack.

        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regTmp);

        // Loop:
        BasicBlock* loop = genCreateTempLabel();
        genDefineTempLabel(loop);

        noway_assert(STACK_ALIGN == 8);
        inst_IV(INS_push, (unsigned)genRegMask(regTmp));
        inst_IV(INS_push, (unsigned)genRegMask(regTmp));

        // If not done, loop
        // Note that regCnt is the number of bytes to stack allocate.
        assert(genIsValidIntReg(regCnt));
        GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, regCnt, STACK_ALIGN, INS_FLAGS_SET);
        inst_JMP(EJ_ne, loop);

        lastTouchDelta = 0;
    }
    else
    {
        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        //
        // We don't need to zero out the allocated memory. However, we do have
        // to tickle the pages to ensure that SP is always valid and is
        // in sync with the "stack guard page".  Note that in the worst
        // case SP is on the last byte of the guard page.  Thus you must
        // touch SP-0 first not SP-0x1000.
        //
        // Another subtlety is that you don't want SP to be exactly on the
        // boundary of the guard page because PUSH is predecrement, thus
        // call setup would not touch the guard page but just beyond it
        //
        // Note that we go through a few hoops so that SP never points to
        // illegal pages at any time during the tickling process
        //
        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        //       bvc   Loop                    // result is smaller than original SP (no wrap around)
        //       mov   regCnt, #0              // Overflow, pick lowest possible value
        //
        //  Loop:
        //       ldr   regTmp, [SP + 0]        // tickle the page - read from the page
        //       sub   regTmp, SP, PAGE_SIZE   // decrement SP by eeGetPageSize()
        //       cmp   regTmp, regCnt
        //       jb    Done
        //       mov   SP, regTmp
        //       j     Loop
        //
        //  Done:
        //       mov   SP, regCnt
        //

        BasicBlock* loop = genCreateTempLabel();
        BasicBlock* done = genCreateTempLabel();

        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        GetEmitter()->emitIns_R_R_R(INS_sub, EA_PTRSIZE, regCnt, REG_SPBASE, regCnt, INS_FLAGS_SET);

        inst_JMP(EJ_vc, loop); // branch if the V flag is not set

        // Overflow, set regCnt to lowest possible value
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);

        genDefineTempLabel(loop);

        // tickle the page - Read from the updated SP - this triggers a page fault when on the guard page
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regTmp, REG_SPBASE, 0);

        // decrement SP by eeGetPageSize()
        GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, regTmp, REG_SPBASE, compiler->eeGetPageSize());

        GetEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, regTmp, regCnt);
        inst_JMP(EJ_lo, done);

        // Update SP to be at the next page of stack that we will tickle
        GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_SPBASE, regTmp, /* canSkip */ false);

        // Jump to loop and tickle new stack address
        inst_JMP(EJ_jmp, loop);

        // Done with stack tickle loop
        genDefineTempLabel(done);

        // Now just move the final value to SP
        GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_SPBASE, regCnt, /* canSkip */ false);

        // lastTouchDelta is dynamic, and can be up to a page. So if we have outgoing arg space,
        // we're going to assume the worst and probe.
    }

ALLOC_DONE:
    // Re-adjust SP to allocate outgoing arg area. We must probe this adjustment.
    if (stackAdjustment != 0)
    {
        assert((stackAdjustment % STACK_ALIGN) == 0); // This must be true for the stack to remain aligned
        assert((lastTouchDelta == ILLEGAL_LAST_TOUCH_DELTA) || (lastTouchDelta >= 0));

        if ((lastTouchDelta == ILLEGAL_LAST_TOUCH_DELTA) ||
            (stackAdjustment + (unsigned)lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES >
             compiler->eeGetPageSize()))
        {
            genStackPointerConstantAdjustmentLoopWithProbe(-(ssize_t)stackAdjustment, regTmp);
        }
        else
        {
            genStackPointerConstantAdjustment(-(ssize_t)stackAdjustment, regTmp);
        }

        // Return the stackalloc'ed address in result register.
        // regCnt = SP + stackAdjustment.
        genInstrWithConstant(INS_add, EA_PTRSIZE, regCnt, REG_SPBASE, (ssize_t)stackAdjustment, INS_FLAGS_DONT_CARE,
                             regTmp);
    }
    else // stackAdjustment == 0
    {
        // Move the final value of SP to regCnt
        inst_Mov(TYP_I_IMPL, regCnt, REG_SPBASE, /* canSkip */ false);
    }

BAILOUT:
    if (endLabel != nullptr)
        genDefineTempLabel(endLabel);

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genTableBasedSwitch: generate code for a switch statement based on a table of ip-relative offsets
//
void CodeGen::genTableBasedSwitch(GenTree* treeNode)
{
    genConsumeOperands(treeNode->AsOp());
    regNumber idxReg  = treeNode->AsOp()->gtOp1->GetRegNum();
    regNumber baseReg = treeNode->AsOp()->gtOp2->GetRegNum();

    GetEmitter()->emitIns_R_ARX(INS_ldr, EA_4BYTE, REG_PC, baseReg, idxReg, TARGET_POINTER_SIZE, 0);
}

//------------------------------------------------------------------------
// genJumpTable: emits the table and an instruction to get the address of the first element
//
void CodeGen::genJumpTable(GenTree* treeNode)
{
    noway_assert(compiler->compCurBB->KindIs(BBJ_SWITCH));
    assert(treeNode->OperGet() == GT_JMPTABLE);

    unsigned     jumpCount = compiler->compCurBB->GetSwitchTargets()->bbsCount;
    BasicBlock** jumpTable = compiler->compCurBB->GetSwitchTargets()->bbsDstTab;
    unsigned     jmpTabBase;

    jmpTabBase = GetEmitter()->emitBBTableDataGenBeg(jumpCount, false);

    JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", compiler->compMethodID, jmpTabBase);

    for (unsigned i = 0; i < jumpCount; i++)
    {
        BasicBlock* target = *jumpTable++;
        noway_assert(target->HasFlag(BBF_HAS_LABEL));

        JITDUMP("            DD      L_M%03u_" FMT_BB "\n", compiler->compMethodID, target->bbNum);

        GetEmitter()->emitDataGenData(i, target);
    }

    GetEmitter()->emitDataGenEnd();

    genMov32RelocatableDataLabel(jmpTabBase, treeNode->GetRegNum());

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genLockedInstructions: Generate code for a GT_XADD or GT_XCHG node.
//
// Arguments:
//    treeNode - the GT_XADD/XCHG node
//
void CodeGen::genLockedInstructions(GenTreeOp* treeNode)
{
    GenTree*  data      = treeNode->AsOp()->gtOp2;
    GenTree*  addr      = treeNode->AsOp()->gtOp1;
    regNumber targetReg = treeNode->GetRegNum();
    regNumber dataReg   = data->GetRegNum();
    regNumber addrReg   = addr->GetRegNum();

    genConsumeAddress(addr);
    genConsumeRegs(data);

    assert(!treeNode->OperIs(GT_XORR, GT_XAND));
    assert(treeNode->OperIs(GT_XCHG) || !varTypeIsSmall(treeNode->TypeGet()));

    emitAttr dataSize = emitActualTypeSize(data);

    regNumber tempReg  = treeNode->ExtractTempReg(RBM_ALLINT);
    regNumber storeReg = (treeNode->OperGet() == GT_XCHG) ? dataReg : treeNode->ExtractTempReg(RBM_ALLINT);
    regNumber loadReg  = (targetReg != REG_NA) ? targetReg : storeReg;

    // Check allocator assumptions
    //
    // The register allocator should have extended the lifetimes of all input and internal registers so that
    // none interfere with the target.
    noway_assert(addrReg != targetReg);

    noway_assert(addrReg != loadReg);
    noway_assert(dataReg != loadReg);

    noway_assert((treeNode->OperGet() == GT_XCHG) || (addrReg != dataReg));

    assert(addr->isUsedFromReg());
    noway_assert(tempReg != REG_NA);
    noway_assert(tempReg != targetReg);
    noway_assert((targetReg != REG_NA) || (treeNode->OperGet() != GT_XCHG));

    // Store exclusive unpredictable cases must be avoided
    noway_assert(tempReg != addrReg);

    // NOTE: `genConsumeAddress` marks the consumed register as not a GC pointer, as it assumes that the input
    // registers
    // die at the first instruction generated by the node. This is not the case for these atomics as the  input
    // registers are multiply-used. As such, we need to mark the addr register as containing a GC pointer until
    // we are finished generating the code for this node.

    gcInfo.gcMarkRegPtrVal(addrReg, addr->TypeGet());

    // Emit code like this:
    //   retry:
    //     ldrex loadReg, [addrReg]
    //     add storeReg, loadReg, dataReg      # Only for GT_XADD
    //                                         # GT_XCHG storeReg === dataReg
    //     strex tempReg, storeReg, [addrReg]
    //     cmp tempReg, 0
    //     bne retry
    //     dmb ish

    instruction insLd = INS_ldrex;
    instruction insSt = INS_strex;
    if (varTypeIsByte(treeNode->TypeGet()))
    {
        insLd = INS_ldrexb;
        insSt = INS_strexb;
    }
    else if (varTypeIsShort(treeNode->TypeGet()))
    {
        insLd = INS_ldrexh;
        insSt = INS_strexh;
    }

    instGen_MemoryBarrier();

    BasicBlock* labelRetry = genCreateTempLabel();
    genDefineTempLabel(labelRetry);

    // The following instruction includes a acquire half barrier
    GetEmitter()->emitIns_R_R(insLd, dataSize, loadReg, addrReg);

    if (treeNode->OperGet() == GT_XADD)
    {
        if (data->isContainedIntOrIImmed())
        {
            genInstrWithConstant(INS_add, dataSize, storeReg, loadReg, data->AsIntConCommon()->IconValue(),
                                 INS_FLAGS_DONT_CARE, tempReg);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R(INS_add, dataSize, storeReg, loadReg, dataReg);
        }
    }

    // The following instruction includes a release half barrier
    GetEmitter()->emitIns_R_R_R(insSt, dataSize, tempReg, storeReg, addrReg);

    GetEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, tempReg, 0);
    GetEmitter()->emitIns_J(INS_bne, labelRetry);

    instGen_MemoryBarrier();

    gcInfo.gcMarkRegSetNpt(addr->gtGetRegMask());

    if (targetReg != REG_NA)
    {
        if (varTypeIsSmall(treeNode->TypeGet()) && varTypeIsSigned(treeNode->TypeGet()))
        {
            instruction mov = varTypeIsShort(treeNode->TypeGet()) ? INS_sxth : INS_sxtb;
            GetEmitter()->emitIns_Mov(mov, EA_4BYTE, targetReg, targetReg, /* canSkip */ false);
        }

        genProduceReg(treeNode);
    }
}

//------------------------------------------------------------------------
// genCodeForCmpXchg: Produce code for a GT_CMPXCHG node.
//
// Arguments:
//    tree - the GT_CMPXCHG node
//
void CodeGen::genCodeForCmpXchg(GenTreeCmpXchg* treeNode)
{
    assert(treeNode->OperIs(GT_CMPXCHG));

    GenTree* addr      = treeNode->Addr();      // arg1
    GenTree* data      = treeNode->Data();      // arg2
    GenTree* comparand = treeNode->Comparand(); // arg3

    regNumber targetReg    = treeNode->GetRegNum();
    regNumber dataReg      = data->GetRegNum();
    regNumber addrReg      = addr->GetRegNum();
    regNumber comparandReg = comparand->GetRegNum();

    genConsumeAddress(addr);
    genConsumeRegs(data);
    genConsumeRegs(comparand);

    emitAttr dataSize = emitActualTypeSize(data);

    regNumber exResultReg = treeNode->ExtractTempReg(RBM_ALLINT);

    // Check allocator assumptions
    //
    // The register allocator should have extended the lifetimes of all input and internal registers so that
    // none interfere with the target.
    noway_assert(addrReg != targetReg);
    noway_assert(dataReg != targetReg);
    noway_assert(comparandReg != targetReg);
    noway_assert(addrReg != dataReg);
    noway_assert(targetReg != REG_NA);
    noway_assert(exResultReg != REG_NA);
    noway_assert(exResultReg != targetReg);

    assert(addr->isUsedFromReg());
    assert(data->isUsedFromReg());
    assert(!comparand->isUsedFromMemory());

    // Store exclusive unpredictable cases must be avoided
    noway_assert(exResultReg != dataReg);
    noway_assert(exResultReg != addrReg);

    // NOTE: `genConsumeAddress` marks the consumed register as not a GC pointer, as it assumes that the input
    // registers
    // die at the first instruction generated by the node. This is not the case for these atomics as the  input
    // registers are multiply-used. As such, we need to mark the addr register as containing a GC pointer until
    // we are finished generating the code for this node.

    gcInfo.gcMarkRegPtrVal(addrReg, addr->TypeGet());

    // Emit code like this:
    //   retry:
    //     ldrex targetReg, [addrReg]
    //     cmp targetReg, comparandReg
    //     bne compareFail
    //     strex exResult, dataReg, [addrReg]
    //     cmp exResult, 0
    //     bne retry
    //   compareFail:
    //     dmb ish

    instruction insLd = INS_ldrex;
    instruction insSt = INS_strex;
    if (varTypeIsByte(treeNode->TypeGet()))
    {
        insLd = INS_ldrexb;
        insSt = INS_strexb;
    }
    else if (varTypeIsShort(treeNode->TypeGet()))
    {
        insLd = INS_ldrexh;
        insSt = INS_strexh;
    }

    instGen_MemoryBarrier();

    BasicBlock* labelRetry       = genCreateTempLabel();
    BasicBlock* labelCompareFail = genCreateTempLabel();
    genDefineTempLabel(labelRetry);

    // The following instruction includes a acquire half barrier
    GetEmitter()->emitIns_R_R(insLd, dataSize, targetReg, addrReg);

    if (comparand->isContainedIntOrIImmed())
    {
        if (comparand->IsIntegralConst(0) && emitter::isLowRegister(targetReg))
        {
            GetEmitter()->emitIns_J_R(INS_cbnz, EA_4BYTE, labelCompareFail, targetReg);
        }
        else
        {
            assert(comparand->AsIntConCommon()->IconValue() <= INT32_MAX);
            GetEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, targetReg,
                                      (target_ssize_t)comparand->AsIntConCommon()->IconValue());
            GetEmitter()->emitIns_J(INS_bne, labelCompareFail);
        }
    }
    else
    {
        GetEmitter()->emitIns_R_R(INS_cmp, EA_4BYTE, targetReg, comparandReg);
        GetEmitter()->emitIns_J(INS_bne, labelCompareFail);
    }

    // The following instruction includes a release half barrier
    GetEmitter()->emitIns_R_R_R(insSt, dataSize, exResultReg, dataReg, addrReg);

    GetEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, exResultReg, 0);
    GetEmitter()->emitIns_J(INS_bne, labelRetry);

    genDefineTempLabel(labelCompareFail);

    instGen_MemoryBarrier();

    gcInfo.gcMarkRegSetNpt(addr->gtGetRegMask());

    if (varTypeIsSmall(treeNode->TypeGet()) && varTypeIsSigned(treeNode->TypeGet()))
    {
        instruction mov = varTypeIsShort(treeNode->TypeGet()) ? INS_sxth : INS_sxtb;
        GetEmitter()->emitIns_Mov(mov, EA_4BYTE, targetReg, targetReg, /* canSkip */ false);
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genGetInsForOper: Return instruction encoding of the operation tree.
//
instruction CodeGen::genGetInsForOper(genTreeOps oper, var_types type)
{
    instruction ins;

    if (varTypeIsFloating(type))
        return CodeGen::ins_MathOp(oper, type);

    switch (oper)
    {
        case GT_ADD:
            ins = INS_add;
            break;
        case GT_AND:
            ins = INS_AND;
            break;
        case GT_AND_NOT:
            ins = INS_bic;
            break;
        case GT_MUL:
            ins = INS_MUL;
            break;
#if !defined(USE_HELPERS_FOR_INT_DIV)
        case GT_DIV:
            ins = INS_sdiv;
            break;
#endif // !USE_HELPERS_FOR_INT_DIV
        case GT_LSH:
            ins = INS_SHIFT_LEFT_LOGICAL;
            break;
        case GT_NEG:
            ins = INS_rsb;
            break;
        case GT_NOT:
            ins = INS_NOT;
            break;
        case GT_OR:
            ins = INS_OR;
            break;
        case GT_RSH:
            ins = INS_SHIFT_RIGHT_ARITHM;
            break;
        case GT_RSZ:
            ins = INS_SHIFT_RIGHT_LOGICAL;
            break;
        case GT_SUB:
            ins = INS_sub;
            break;
        case GT_XOR:
            ins = INS_XOR;
            break;
        case GT_ROR:
            ins = INS_ror;
            break;
        case GT_ADD_LO:
            ins = INS_add;
            break;
        case GT_ADD_HI:
            ins = INS_adc;
            break;
        case GT_SUB_LO:
            ins = INS_sub;
            break;
        case GT_SUB_HI:
            ins = INS_sbc;
            break;
        case GT_LSH_HI:
            ins = INS_SHIFT_LEFT_LOGICAL;
            break;
        case GT_RSH_LO:
            ins = INS_SHIFT_RIGHT_LOGICAL;
            break;
        default:
            unreached();
            break;
    }
    return ins;
}

//------------------------------------------------------------------------
// genCodeForNegNot: Produce code for a GT_NEG/GT_NOT node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForNegNot(GenTree* tree)
{
    assert(tree->OperIs(GT_NEG, GT_NOT));

    var_types targetType = tree->TypeGet();

    assert(!tree->OperIs(GT_NOT) || !varTypeIsFloating(targetType));

    regNumber   targetReg = tree->GetRegNum();
    instruction ins       = genGetInsForOper(tree->OperGet(), targetType);

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(!tree->isContained());
    // The dst can only be a register.
    assert(targetReg != REG_NA);

    GenTree* operand = tree->gtGetOp1();
    assert(!operand->isContained());
    // The src must be a register.
    regNumber operandReg = genConsumeReg(operand);

    if (ins == INS_vneg)
    {
        GetEmitter()->emitIns_R_R(ins, emitTypeSize(tree), targetReg, operandReg);
    }
    else
    {
        GetEmitter()->emitIns_R_R_I(ins, emitTypeSize(tree), targetReg, operandReg, 0, INS_FLAGS_SET);
    }

    genProduceReg(tree);
}

// Generate code for CpObj nodes which copy structs that have interleaved
// GC pointers.
// For this case we'll generate a sequence of loads/stores in the case of struct
// slots that don't contain GC pointers.  The generated code will look like:
// ldr tempReg, [R13, #8]
// str tempReg, [R14, #8]
//
// In the case of a GC-Pointer we'll call the ByRef write barrier helper
// who happens to use the same registers as the previous call to maintain
// the same register requirements and register killsets:
// bl CORINFO_HELP_ASSIGN_BYREF
//
// So finally an example would look like this:
// ldr tempReg, [R13, #8]
// str tempReg, [R14, #8]
// bl CORINFO_HELP_ASSIGN_BYREF
// ldr tempReg, [R13, #8]
// str tempReg, [R14, #8]
// bl CORINFO_HELP_ASSIGN_BYREF
// ldr tempReg, [R13, #8]
// str tempReg, [R14, #8]
void CodeGen::genCodeForCpObj(GenTreeBlk* cpObjNode)
{
    GenTree*  dstAddr       = cpObjNode->Addr();
    GenTree*  source        = cpObjNode->Data();
    var_types srcAddrType   = TYP_BYREF;
    bool      sourceIsLocal = false;
    regNumber dstReg        = REG_NA;
    regNumber srcReg        = REG_NA;

    assert(source->isContained());
    if (source->gtOper == GT_IND)
    {
        GenTree* srcAddr = source->gtGetOp1();
        assert(!srcAddr->isContained());
        srcAddrType = srcAddr->TypeGet();
    }
    else
    {
        noway_assert(source->IsLocal());
        sourceIsLocal = true;
    }

    bool dstOnStack = dstAddr->gtSkipReloadOrCopy()->OperIs(GT_LCL_ADDR);

#ifdef DEBUG
    assert(!dstAddr->isContained());

    // This GenTree node has data about GC pointers, this means we're dealing
    // with CpObj.
    assert(cpObjNode->GetLayout()->HasGCPtr());
#endif // DEBUG

    // Consume the operands and get them into the right registers.
    // They may now contain gc pointers (depending on their type; gcMarkRegPtrVal will "do the right thing").
    genConsumeBlockOp(cpObjNode, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_SRC_BYREF, REG_NA);
    gcInfo.gcMarkRegPtrVal(REG_WRITE_BARRIER_SRC_BYREF, srcAddrType);
    gcInfo.gcMarkRegPtrVal(REG_WRITE_BARRIER_DST_BYREF, dstAddr->TypeGet());

    // Temp register used to perform the sequence of loads and stores.
    regNumber tmpReg = cpObjNode->ExtractTempReg();
    assert(genIsValidIntReg(tmpReg));

    if (cpObjNode->IsVolatile())
    {
        // issue a full memory barrier before & after a volatile CpObj operation
        instGen_MemoryBarrier();
    }

    emitter*     emit   = GetEmitter();
    ClassLayout* layout = cpObjNode->GetLayout();
    unsigned     slots  = layout->GetSlotCount();

    // If we can prove it's on the stack we don't need to use the write barrier.
    if (dstOnStack)
    {
        for (unsigned i = 0; i < slots; ++i)
        {
            emitAttr attr = emitTypeSize(layout->GetGCPtrType(i));

            emit->emitIns_R_R_I(INS_ldr, attr, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE,
                                INS_FLAGS_DONT_CARE, INS_OPTS_LDST_POST_INC);
            emit->emitIns_R_R_I(INS_str, attr, tmpReg, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE,
                                INS_FLAGS_DONT_CARE, INS_OPTS_LDST_POST_INC);
        }
    }
    else
    {
        unsigned gcPtrCount = layout->GetGCPtrCount();

        unsigned i = 0;
        while (i < slots)
        {
            if (!layout->IsGCPtr(i))
            {
                emit->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE,
                                    INS_FLAGS_DONT_CARE, INS_OPTS_LDST_POST_INC);
                emit->emitIns_R_R_I(INS_str, EA_PTRSIZE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE,
                                    INS_FLAGS_DONT_CARE, INS_OPTS_LDST_POST_INC);
            }
            else
            {
                genEmitHelperCall(CORINFO_HELP_ASSIGN_BYREF, 0, EA_PTRSIZE);
                gcPtrCount--;
            }
            ++i;
        }
        assert(gcPtrCount == 0);
    }

    if (cpObjNode->IsVolatile())
    {
        // issue a full memory barrier before & after a volatile CpObj operation
        instGen_MemoryBarrier();
    }

    // Clear the gcInfo for registers of source and dest.
    // While we normally update GC info prior to the last instruction that uses them,
    // these actually live into the helper call.
    gcInfo.gcMarkRegSetNpt(RBM_WRITE_BARRIER_SRC_BYREF | RBM_WRITE_BARRIER_DST_BYREF);
}

//------------------------------------------------------------------------
// genCodeForShiftLong: Generates the code sequence for a GenTree node that
// represents a three operand bit shift or rotate operation (<<Hi, >>Lo).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//    b) The shift-by-amount in tree->AsOp()->gtOp2 is a contained constant
//
void CodeGen::genCodeForShiftLong(GenTree* tree)
{
    // Only the non-RMW case here.
    genTreeOps oper = tree->OperGet();
    assert(oper == GT_LSH_HI || oper == GT_RSH_LO);

    GenTree* operand = tree->AsOp()->gtOp1;
    assert(operand->OperGet() == GT_LONG);
    assert(operand->AsOp()->gtOp1->isUsedFromReg());
    assert(operand->AsOp()->gtOp2->isUsedFromReg());

    GenTree* operandLo = operand->gtGetOp1();
    GenTree* operandHi = operand->gtGetOp2();

    regNumber regLo = operandLo->GetRegNum();
    regNumber regHi = operandHi->GetRegNum();

    genConsumeOperands(tree->AsOp());

    var_types   targetType = tree->TypeGet();
    instruction ins        = genGetInsForOper(oper, targetType);

    GenTree* shiftBy = tree->gtGetOp2();

    assert(shiftBy->isContainedIntOrIImmed());

    unsigned count = (unsigned)shiftBy->AsIntConCommon()->IconValue();

    regNumber regResult = (oper == GT_LSH_HI) ? regHi : regLo;

    inst_Mov(targetType, tree->GetRegNum(), regResult, /* canSkip */ true);

    if (oper == GT_LSH_HI)
    {
        inst_RV_SH(ins, EA_4BYTE, tree->GetRegNum(), count);
        GetEmitter()->emitIns_R_R_R_I(INS_OR, EA_4BYTE, tree->GetRegNum(), tree->GetRegNum(), regLo, 32 - count,
                                      INS_FLAGS_DONT_CARE, INS_OPTS_LSR);
    }
    else
    {
        assert(oper == GT_RSH_LO);
        inst_RV_SH(INS_SHIFT_RIGHT_LOGICAL, EA_4BYTE, tree->GetRegNum(), count);
        GetEmitter()->emitIns_R_R_R_I(INS_OR, EA_4BYTE, tree->GetRegNum(), tree->GetRegNum(), regHi, 32 - count,
                                      INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForLclVar: Produce code for a GT_LCL_VAR node.
//
// Arguments:
//    tree - the GT_LCL_VAR node
//
void CodeGen::genCodeForLclVar(GenTreeLclVar* tree)
{
    // lcl_vars are not defs
    assert((tree->gtFlags & GTF_VAR_DEF) == 0);

    bool isRegCandidate = compiler->lvaGetDesc(tree)->lvIsRegCandidate();

    // If this is a register candidate that has been spilled, genConsumeReg() will
    // reload it at the point of use.  Otherwise, if it's not in a register, we load it here.

    if (!isRegCandidate && !tree->IsMultiReg() && !(tree->gtFlags & GTF_SPILLED))
    {
        const LclVarDsc* varDsc = compiler->lvaGetDesc(tree);
        var_types        type   = varDsc->GetRegisterType(tree);

        GetEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), tree->GetRegNum(), tree->GetLclNum(), 0);
        genProduceReg(tree);
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

    GenTree*  data    = tree->gtOp1;
    regNumber dataReg = REG_NA;
    genConsumeRegs(data);

    if (data->isContained())
    {
        assert(data->OperIs(GT_BITCAST));
        const GenTree* bitcastSrc = data->AsUnOp()->gtGetOp1();
        assert(!bitcastSrc->isContained());
        dataReg = bitcastSrc->GetRegNum();
    }
    else
    {

        dataReg = data->GetRegNum();
    }
    assert(dataReg != REG_NA);

    if (tree->IsOffsetMisaligned())
    {
        // Arm supports unaligned access only for integer types,
        // convert the storing floating data into 1 or 2 integer registers and write them as int.
        regNumber addr = tree->ExtractTempReg();
        emit->emitIns_R_S(INS_lea, EA_PTRSIZE, addr, varNum, offset);
        if (targetType == TYP_FLOAT)
        {
            regNumber floatAsInt = tree->GetSingleTempReg();
            emit->emitIns_Mov(INS_vmov_f2i, EA_4BYTE, floatAsInt, dataReg, /* canSkip */ false);
            emit->emitIns_R_R(INS_str, EA_4BYTE, floatAsInt, addr);
        }
        else
        {
            regNumber halfdoubleAsInt1 = tree->ExtractTempReg();
            regNumber halfdoubleAsInt2 = tree->GetSingleTempReg();
            emit->emitIns_R_R_R(INS_vmov_d2i, EA_8BYTE, halfdoubleAsInt1, halfdoubleAsInt2, dataReg);
            emit->emitIns_R_R_I(INS_str, EA_4BYTE, halfdoubleAsInt1, addr, 0);
            emit->emitIns_R_R_I(INS_str, EA_4BYTE, halfdoubleAsInt1, addr, 4);
        }
    }
    else
    {
        emitAttr    attr = emitTypeSize(targetType);
        instruction ins  = ins_StoreFromSrc(dataReg, targetType);
        emit->emitIns_S_R(ins, attr, dataReg, varNum, offset);
    }

    // Updating variable liveness after instruction was emitted
    genUpdateLife(tree);
    varDsc->SetRegNum(REG_STK);
}

//------------------------------------------------------------------------
// genCodeForStoreLclVar: Produce code for a GT_STORE_LCL_VAR node.
//
// Arguments:
//    tree - the GT_STORE_LCL_VAR node
//
void CodeGen::genCodeForStoreLclVar(GenTreeLclVar* tree)
{
    GenTree* data       = tree->gtOp1;
    GenTree* actualData = data->gtSkipReloadOrCopy();

    // Stores from a multi-reg source are handled separately.
    if (actualData->IsMultiRegNode())
    {
        genMultiRegStoreToLocal(tree);
    }
    else
    {
        unsigned   varNum     = tree->GetLclNum();
        LclVarDsc* varDsc     = compiler->lvaGetDesc(varNum);
        var_types  targetType = varDsc->GetRegisterType(tree);

        emitter* emit = GetEmitter();

        if (targetType == TYP_LONG)
        {
            genStoreLongLclVar(tree);
        }
        else
        {
            genConsumeRegs(data);

            regNumber dataReg = REG_NA;

            if (data->isContained())
            {
                assert(data->OperIs(GT_BITCAST));
                const GenTree* bitcastSrc = data->AsUnOp()->gtGetOp1();
                assert(!bitcastSrc->isContained());
                dataReg = bitcastSrc->GetRegNum();
            }
            else
            {
                dataReg = data->GetRegNum();
            }
            assert(dataReg != REG_NA);

            regNumber targetReg = tree->GetRegNum();

            if (targetReg == REG_NA) // store into stack based LclVar
            {
                inst_set_SV_var(tree);

                instruction ins  = ins_StoreFromSrc(dataReg, targetType);
                emitAttr    attr = emitTypeSize(targetType);

                emit->emitIns_S_R(ins, attr, dataReg, varNum, /* offset */ 0);
            }
            else // store into register (i.e move into register)
            {
                // Assign into targetReg when dataReg (from op1) is not the same register
                // Only zero/sign extend if we are using general registers.
                if (varTypeIsIntegral(targetType) && emit->isGeneralRegister(targetReg) &&
                    emit->isGeneralRegister(dataReg))
                {
                    // We use 'emitActualTypeSize' as the instructions require 4BYTE.
                    inst_Mov_Extend(targetType, /* srcInReg */ true, targetReg, dataReg, /* canSkip */ true,
                                    emitActualTypeSize(targetType));
                }
                else
                {
                    inst_Mov(targetType, targetReg, dataReg, /* canSkip */ true);
                }
            }

            genUpdateLifeStore(tree, targetReg, varDsc);
        }
    }
}

//------------------------------------------------------------------------
// genCodeForDivMod: Produce code for a GT_DIV/GT_UDIV/GT_MOD/GT_UMOD node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForDivMod(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_DIV, GT_UDIV, GT_MOD, GT_UMOD));

    // We shouldn't be seeing GT_MOD on float/double args as it should get morphed into a
    // helper call by front-end. Similarly we shouldn't be seeing GT_UDIV and GT_UMOD
    // on float/double args.
    noway_assert(tree->OperIs(GT_DIV) || !varTypeIsFloating(tree));

#if defined(USE_HELPERS_FOR_INT_DIV)
    noway_assert(!varTypeIsIntOrI(tree));
#endif // USE_HELPERS_FOR_INT_DIV

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->GetRegNum();
    emitter*  emit       = GetEmitter();

    genConsumeOperands(tree);

    noway_assert(targetReg != REG_NA);

    GenTree*    dst    = tree;
    GenTree*    src1   = tree->gtGetOp1();
    GenTree*    src2   = tree->gtGetOp2();
    instruction ins    = genGetInsForOper(tree->OperGet(), targetType);
    emitAttr    attr   = emitTypeSize(tree);
    regNumber   result = REG_NA;

    // dst can only be a reg
    assert(!dst->isContained());

    // src can be only reg
    assert(!src1->isContained() || !src2->isContained());

    if (varTypeIsFloating(targetType))
    {
        // Floating point divide never raises an exception

        emit->emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum());
    }
    else // an signed integer divide operation
    {
        // TODO-ARM-Bug: handle zero division exception.

        emit->emitIns_R_R_R(ins, attr, dst->GetRegNum(), src1->GetRegNum(), src2->GetRegNum());
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCkfinite: Generate code for ckfinite opcode.
//
// Arguments:
//    treeNode - The GT_CKFINITE node
//
// Return Value:
//    None.
//
// Assumptions:
//    GT_CKFINITE node has reserved an internal register.
//
void CodeGen::genCkfinite(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_CKFINITE);

    emitter*  emit       = GetEmitter();
    var_types targetType = treeNode->TypeGet();
    regNumber intReg     = treeNode->GetSingleTempReg();
    regNumber fpReg      = genConsumeReg(treeNode->AsOp()->gtOp1);
    regNumber targetReg  = treeNode->GetRegNum();

    // Extract and sign-extend the exponent into an integer register
    if (targetType == TYP_FLOAT)
    {
        emit->emitIns_Mov(INS_vmov_f2i, EA_4BYTE, intReg, fpReg, /* canSkip */ false);
        emit->emitIns_R_R_I_I(INS_sbfx, EA_4BYTE, intReg, intReg, 23, 8);
    }
    else
    {
        assert(targetType == TYP_DOUBLE);
        emit->emitIns_Mov(INS_vmov_f2i, EA_4BYTE, intReg, REG_NEXT(fpReg), /* canSkip */ false);
        emit->emitIns_R_R_I_I(INS_sbfx, EA_4BYTE, intReg, intReg, 20, 11);
    }

    // If exponent is all 1's, throw ArithmeticException
    emit->emitIns_R_I(INS_add, EA_4BYTE, intReg, 1, INS_FLAGS_SET);
    genJumpToThrowHlpBlk(EJ_eq, SCK_ARITH_EXCPN);

    // If it's a finite value, copy it to targetReg
    inst_Mov(targetType, targetReg, fpReg, /* canSkip */ true, emitTypeSize(treeNode));

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_EQ/GT_NE/GT_LT/GT_LE/GT_GE/GT_GT/GT_CMP node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForCompare(GenTreeOp* tree)
{
    // TODO-ARM-CQ: Check if we can use the currently set flags.
    // TODO-ARM-CQ: Check for the case where we can simply transfer the carry bit to a register
    //         (signed < or >= where targetReg != REG_NA)

    GenTree*  op1     = tree->gtOp1;
    GenTree*  op2     = tree->gtOp2;
    var_types op1Type = op1->TypeGet();
    var_types op2Type = op2->TypeGet();

    assert(!varTypeIsLong(op1Type));
    assert(!varTypeIsLong(op2Type));

    regNumber targetReg = tree->GetRegNum();
    emitter*  emit      = GetEmitter();

    if (varTypeIsFloating(op1Type))
    {
        assert(op1Type == op2Type);
        emit->emitInsBinary(INS_vcmp, emitTypeSize(op1Type), op1, op2);
        // vmrs with register 0xf has special meaning of transferring flags
        emit->emitIns_R(INS_vmrs, EA_4BYTE, REG_R15);
    }
    else
    {
        assert(!varTypeIsFloating(op2Type));
        var_types cmpType = (op1Type == op2Type) ? op1Type : TYP_INT;
        emit->emitInsBinary(INS_cmp, emitTypeSize(cmpType), op1, op2);
    }

    // Are we evaluating this into a register?
    if (targetReg != REG_NA)
    {
        inst_SETCC(GenCondition::FromRelop(tree), tree->TypeGet(), targetReg);
        genProduceReg(tree);
    }
}

//------------------------------------------------------------------------
// genCodeForJTrue: Produce code for a GT_JTRUE node.
//
// Arguments:
//    jtrue - the node
//
void CodeGen::genCodeForJTrue(GenTreeOp* jtrue)
{
    assert(compiler->compCurBB->KindIs(BBJ_COND));

    GenTree*  op  = jtrue->gtGetOp1();
    regNumber reg = genConsumeReg(op);
    inst_RV_RV(INS_tst, reg, reg, genActualType(op));
    inst_JMP(EJ_ne, compiler->compCurBB->GetTrueTarget());

    // If we cannot fall into the false target, emit a jump to it
    BasicBlock* falseTarget = compiler->compCurBB->GetFalseTarget();
    if (!compiler->compCurBB->CanRemoveJumpToTarget(falseTarget, compiler))
    {
        inst_JMP(EJ_jmp, falseTarget);
    }
}

//------------------------------------------------------------------------
// genCodeForReturnTrap: Produce code for a GT_RETURNTRAP node.
//
// Arguments:
//    tree - the GT_RETURNTRAP node
//
void CodeGen::genCodeForReturnTrap(GenTreeOp* tree)
{
    assert(tree->OperGet() == GT_RETURNTRAP);

    // this is nothing but a conditional call to CORINFO_HELP_STOP_FOR_GC
    // based on the contents of 'data'

    GenTree* data = tree->gtOp1;
    genConsumeIfReg(data);
    GenTreeIntCon cns = intForm(TYP_INT, 0);
    cns.SetContained();
    GetEmitter()->emitInsBinary(INS_cmp, emitTypeSize(TYP_INT), data, &cns);

    BasicBlock* skipLabel = genCreateTempLabel();

    inst_JMP(EJ_eq, skipLabel);

    // emit the call to the EE-helper that stops for GC (or other reasons)

    genEmitHelperCall(CORINFO_HELP_STOP_FOR_GC, 0, EA_UNKNOWN);
    genDefineTempLabel(skipLabel);
}

//------------------------------------------------------------------------
// genCodeForStoreInd: Produce code for a GT_STOREIND node.
//
// Arguments:
//    tree - the GT_STOREIND node
//
void CodeGen::genCodeForStoreInd(GenTreeStoreInd* tree)
{
    GenTree*  data = tree->Data();
    GenTree*  addr = tree->Addr();
    var_types type = tree->TypeGet();

    assert(!varTypeIsFloating(type) || (type == data->TypeGet()));

    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(tree);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        // data and addr must be in registers.
        // Consume both registers so that any copies of interfering
        // registers are taken care of.
        genConsumeOperands(tree);

        // At this point, we should not have any interference.
        // That is, 'data' must not be in REG_ARG_0,
        //  as that is where 'addr' must go.
        noway_assert(data->GetRegNum() != REG_ARG_0);

        // addr goes in REG_ARG_0
        inst_Mov(addr->TypeGet(), REG_ARG_0, addr->GetRegNum(), /* canSkip */ true);

        // data goes in REG_ARG_1
        inst_Mov(data->TypeGet(), REG_ARG_1, data->GetRegNum(), /* canSkip */ true);

        genGCWriteBarrier(tree, writeBarrierForm);
    }
    else // A normal store, not a WriteBarrier store
    {
        // We must consume the operands in the proper execution order,
        // so that liveness is updated appropriately.
        genConsumeAddress(addr);

        if (!data->isContained())
        {
            genConsumeRegs(data);
        }

        if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
        {
            // issue a full memory barrier a before volatile StInd
            instGen_MemoryBarrier();
        }

        regNumber dataReg = data->GetRegNum();
        GetEmitter()->emitInsLoadStoreOp(ins_StoreFromSrc(dataReg, type), emitActualTypeSize(type), dataReg, tree);

        // If store was to a variable, update variable liveness after instruction was emitted.
        genUpdateLife(tree);
    }
}

// genLongToIntCast: Generate code for long to int casts.
//
// Arguments:
//    cast - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    The cast node and its sources (via GT_LONG) must have been assigned registers.
//    The destination cannot be a floating point type or a small integer type.
//
void CodeGen::genLongToIntCast(GenTree* cast)
{
    assert(cast->OperGet() == GT_CAST);

    GenTree* src = cast->gtGetOp1();
    noway_assert(src->OperGet() == GT_LONG);

    genConsumeRegs(src);

    var_types srcType  = ((cast->gtFlags & GTF_UNSIGNED) != 0) ? TYP_ULONG : TYP_LONG;
    var_types dstType  = cast->CastToType();
    regNumber loSrcReg = src->gtGetOp1()->GetRegNum();
    regNumber hiSrcReg = src->gtGetOp2()->GetRegNum();
    regNumber dstReg   = cast->GetRegNum();

    assert((dstType == TYP_INT) || (dstType == TYP_UINT));
    assert(genIsValidIntReg(loSrcReg));
    assert(genIsValidIntReg(hiSrcReg));
    assert(genIsValidIntReg(dstReg));

    if (cast->gtOverflow())
    {
        //
        // Generate an overflow check for [u]long to [u]int casts:
        //
        // long  -> int  - check if the upper 33 bits are all 0 or all 1
        //
        // ulong -> int  - check if the upper 33 bits are all 0
        //
        // long  -> uint - check if the upper 32 bits are all 0
        // ulong -> uint - check if the upper 32 bits are all 0
        //

        if ((srcType == TYP_LONG) && (dstType == TYP_INT))
        {
            BasicBlock* allOne  = genCreateTempLabel();
            BasicBlock* success = genCreateTempLabel();

            inst_RV_RV(INS_tst, loSrcReg, loSrcReg, TYP_INT, EA_4BYTE);
            inst_JMP(EJ_mi, allOne);
            inst_RV_RV(INS_tst, hiSrcReg, hiSrcReg, TYP_INT, EA_4BYTE);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            inst_JMP(EJ_jmp, success);

            genDefineTempLabel(allOne);
            inst_RV_IV(INS_cmp, hiSrcReg, -1, EA_4BYTE);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);

            genDefineTempLabel(success);
        }
        else
        {
            if ((srcType == TYP_ULONG) && (dstType == TYP_INT))
            {
                inst_RV_RV(INS_tst, loSrcReg, loSrcReg, TYP_INT, EA_4BYTE);
                genJumpToThrowHlpBlk(EJ_mi, SCK_OVERFLOW);
            }

            inst_RV_RV(INS_tst, hiSrcReg, hiSrcReg, TYP_INT, EA_4BYTE);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
        }
    }

    inst_Mov(TYP_INT, dstReg, loSrcReg, /* canSkip */ true);

    genProduceReg(cast);
}

//------------------------------------------------------------------------
// genIntToFloatCast: Generate code to cast an int to float/double
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
    // int --> float/double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->GetRegNum();
    assert(genIsValidFloatReg(targetReg));

    GenTree* op1 = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());                // Cannot be contained
    assert(genIsValidIntReg(op1->GetRegNum())); // Must be a valid int reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = genActualType(op1->TypeGet());
    assert(!varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (treeNode->gtFlags & GTF_UNSIGNED)
    {
        srcType = varTypeToUnsigned(srcType);
    }

    // We only expect a srcType whose size is EA_4BYTE.
    emitAttr srcSize = EA_ATTR(genTypeSize(srcType));
    noway_assert(srcSize == EA_4BYTE);

    instruction insVcvt = INS_invalid;

    if (dstType == TYP_DOUBLE)
    {
        insVcvt = (varTypeIsUnsigned(srcType)) ? INS_vcvt_u2d : INS_vcvt_i2d;
    }
    else
    {
        assert(dstType == TYP_FLOAT);
        insVcvt = (varTypeIsUnsigned(srcType)) ? INS_vcvt_u2f : INS_vcvt_i2f;
    }
    // All other cast are implemented by different CORINFO_HELP_XX2XX
    // Look to Compiler::fgMorphCast()

    genConsumeOperands(treeNode->AsOp());

    assert(insVcvt != INS_invalid);
    GetEmitter()->emitIns_Mov(INS_vmov_i2f, srcSize, treeNode->GetRegNum(), op1->GetRegNum(), /* canSkip */ false);
    GetEmitter()->emitIns_R_R(insVcvt, srcSize, treeNode->GetRegNum(), treeNode->GetRegNum());

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genFloatToIntCast: Generate code to cast float/double to int
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
    // we don't expect to see overflow detecting float/double --> int type conversions here
    // as they should have been converted into helper calls by front-end.
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->GetRegNum();
    assert(genIsValidIntReg(targetReg)); // Must be a valid int reg.

    GenTree* op1 = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());                  // Cannot be contained
    assert(genIsValidFloatReg(op1->GetRegNum())); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && !varTypeIsFloating(dstType));

    // We only expect a dstType whose size is EA_4BYTE.
    // For conversions to small types (byte/sbyte/int16/uint16) from float/double,
    // we expect the front-end or lowering phase to have generated two levels of cast.
    //
    emitAttr dstSize = EA_ATTR(genTypeSize(dstType));
    noway_assert(dstSize == EA_4BYTE);

    instruction insVcvt = INS_invalid;

    if (srcType == TYP_DOUBLE)
    {
        insVcvt = (varTypeIsUnsigned(dstType)) ? INS_vcvt_d2u : INS_vcvt_d2i;
    }
    else
    {
        assert(srcType == TYP_FLOAT);
        insVcvt = (varTypeIsUnsigned(dstType)) ? INS_vcvt_f2u : INS_vcvt_f2i;
    }
    // All other cast are implemented by different CORINFO_HELP_XX2XX
    // Look to Compiler::fgMorphCast()

    genConsumeOperands(treeNode->AsOp());

    regNumber tmpReg = treeNode->GetSingleTempReg();

    assert(insVcvt != INS_invalid);
    GetEmitter()->emitIns_R_R(insVcvt, dstSize, tmpReg, op1->GetRegNum());
    GetEmitter()->emitIns_Mov(INS_vmov_f2i, dstSize, treeNode->GetRegNum(), tmpReg, /* canSkip */ false);

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genEmitHelperCall: Emit a call to a helper function.
//
void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTargetReg /*= REG_NA */)
{
    // Can we call the helper function directly

    void *addr = NULL, **pAddr = NULL;

#if defined(DEBUG) && defined(PROFILING_SUPPORTED)
    // Don't ask VM if it hasn't requested ELT hooks
    if (!compiler->compProfilerHookNeeded && compiler->opts.compJitELTHookEnabled &&
        (helper == CORINFO_HELP_PROF_FCN_ENTER || helper == CORINFO_HELP_PROF_FCN_LEAVE ||
         helper == CORINFO_HELP_PROF_FCN_TAILCALL))
    {
        addr = compiler->compProfilerMethHnd;
    }
    else
#endif
    {
        addr = compiler->compGetHelperFtn((CorInfoHelpFunc)helper, (void**)&pAddr);
    }

    if (!addr || !validImmForBL((ssize_t)addr))
    {
        if (callTargetReg == REG_NA)
        {
            // If a callTargetReg has not been explicitly provided, we will use REG_DEFAULT_HELPER_CALL_TARGET, but
            // this is only a valid assumption if the helper call is known to kill REG_DEFAULT_HELPER_CALL_TARGET.
            callTargetReg = REG_DEFAULT_HELPER_CALL_TARGET;
        }

        // Load the address into a register and call through a register
        if (addr)
        {
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, callTargetReg, (ssize_t)addr);
        }
        else
        {
            GetEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, callTargetReg, (ssize_t)pAddr);
            regSet.verifyRegUsed(callTargetReg);
        }

        GetEmitter()->emitIns_Call(emitter::EC_INDIR_R, compiler->eeFindHelper(helper),
                                   INDEBUG_LDISASM_COMMA(nullptr) NULL, // addr
                                   argSize, retSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                   gcInfo.gcRegByrefSetCur, DebugInfo(),
                                   callTargetReg, // ireg
                                   REG_NA, 0, 0,  // xreg, xmul, disp
                                   false          // isJump
                                   );
    }
    else
    {
        GetEmitter()->emitIns_Call(emitter::EC_FUNC_TOKEN, compiler->eeFindHelper(helper),
                                   INDEBUG_LDISASM_COMMA(nullptr) addr, argSize, retSize, gcInfo.gcVarPtrSetCur,
                                   gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur, DebugInfo(), REG_NA, REG_NA, 0,
                                   0,    /* ilOffset, ireg, xreg, xmul, disp */
                                   false /* isJump */
                                   );
    }

    regSet.verifyRegistersUsed(RBM_CALLEE_TRASH);
}

#ifdef PROFILING_SUPPORTED

//-----------------------------------------------------------------------------------
// genProfilingEnterCallback: Generate the profiling function enter callback.
//
// Arguments:
//     initReg        - register to use as scratch register
//     pInitRegZeroed - OUT parameter. *pInitRegZeroed set to 'false' if 'initReg' is
//                      not zero after this call.
//
// Return Value:
//     None
//
void CodeGen::genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    // Give profiler a chance to back out of hooking this method
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    // On Arm arguments are prespilled on stack, which frees r0-r3.
    // For generating Enter callout we would need two registers and one of them has to be r0 to pass profiler handle.
    // The call target register could be any free register.
    regNumber argReg     = REG_PROFILER_ENTER_ARG;
    regMaskTP argRegMask = genRegMask(argReg);
    assert((regSet.rsMaskPreSpillRegArg & argRegMask) != 0);

    if (compiler->compProfilerMethHndIndirected)
    {
        GetEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, argReg, (ssize_t)compiler->compProfilerMethHnd);
        regSet.verifyRegUsed(argReg);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_4BYTE, argReg, (ssize_t)compiler->compProfilerMethHnd);
    }

    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER,
                      0,           // argSize. Again, we have to lie about it
                      EA_UNKNOWN); // retSize

    if (initReg == argReg)
    {
        *pInitRegZeroed = false;
    }
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
    assert((helper == CORINFO_HELP_PROF_FCN_LEAVE) || (helper == CORINFO_HELP_PROF_FCN_TAILCALL));

    // Only hook if profiler says it's okay.
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    compiler->info.compProfilerCallback = true;

    //
    // Push the profilerHandle
    //

    // Contract between JIT and Profiler Leave callout on arm:
    // Return size <= 4 bytes: REG_PROFILER_RET_SCRATCH will contain return value
    // Return size > 4 and <= 8: <REG_PROFILER_RET_SCRATCH,r1> will contain return value.
    // Floating point or double or HFA return values will be in s0-s15 in case of non-vararg methods.
    // It is assumed that profiler Leave callback doesn't trash registers r1,REG_PROFILER_RET_SCRATCH and s0-s15.
    //
    // In the following cases r0 doesn't contain a return value and hence need not be preserved before emitting Leave
    // callback.
    bool     r0InUse;
    emitAttr attr = EA_UNKNOWN;

    if (helper == CORINFO_HELP_PROF_FCN_TAILCALL)
    {
        // For the tail call case, the helper call is introduced during lower,
        // so the allocator will arrange things so R0 is not in use here.
        //
        // For the tail jump case, all reg args have been spilled via genJmpMethod,
        // so R0 is likewise not in use.
        r0InUse = false;
    }
    else if (compiler->info.compRetType == TYP_VOID)
    {
        r0InUse = false;
    }
    else if (varTypeIsFloating(compiler->info.compRetType) ||
             compiler->IsHfa(compiler->info.compMethodInfo->args.retTypeClass))
    {
        r0InUse = compiler->info.compIsVarArgs || compiler->opts.compUseSoftFP;
    }
    else
    {
        r0InUse = true;
    }

    if (r0InUse)
    {
        if (varTypeIsGC(compiler->info.compRetNativeType))
        {
            attr = emitActualTypeSize(compiler->info.compRetNativeType);
        }
        else if (compiler->compMethodReturnsRetBufAddr())
        {
            attr = EA_BYREF;
        }
        else
        {
            attr = EA_PTRSIZE;
        }
    }

    if (r0InUse)
    {
        // Has a return value and r0 is in use. For emitting Leave profiler callout we would need r0 for passing
        // profiler handle. Therefore, r0 is moved to REG_PROFILER_RETURN_SCRATCH as per contract.
        GetEmitter()->emitIns_Mov(INS_mov, attr, REG_PROFILER_RET_SCRATCH, REG_R0, /* canSkip */ false);
        genTransferRegGCState(REG_PROFILER_RET_SCRATCH, REG_R0);
        regSet.verifyRegUsed(REG_PROFILER_RET_SCRATCH);
    }

    if (compiler->compProfilerMethHndIndirected)
    {
        GetEmitter()->emitIns_R_AI(INS_ldr, EA_PTR_DSP_RELOC, REG_R0, (ssize_t)compiler->compProfilerMethHnd);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_R0, (ssize_t)compiler->compProfilerMethHnd);
    }

    gcInfo.gcMarkRegSetNpt(RBM_R0);
    regSet.verifyRegUsed(REG_R0);

    genEmitHelperCall(helper,
                      0,           // argSize
                      EA_UNKNOWN); // retSize

    // Restore state that existed before profiler callback
    if (r0InUse)
    {
        GetEmitter()->emitIns_Mov(INS_mov, attr, REG_R0, REG_PROFILER_RET_SCRATCH, /* canSkip */ false);
        genTransferRegGCState(REG_R0, REG_PROFILER_RET_SCRATCH);
        gcInfo.gcMarkRegSetNpt(RBM_PROFILER_RET_SCRATCH);
    }
}

#endif // PROFILING_SUPPORTED

//------------------------------------------------------------------------
// genEstablishFramePointer: Set up the frame pointer by adding an offset to the stack pointer.
//
// Arguments:
//    delta - the offset to add to the current stack pointer to establish the frame pointer
//    reportUnwindData - true if establishing the frame pointer should be reported in the OS unwind data.
//
void CodeGen::genEstablishFramePointer(int delta, bool reportUnwindData)
{
    assert(compiler->compGeneratingProlog);

    assert(arm_Valid_Imm_For_Add_SP(delta));
    GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);

    if (reportUnwindData)
    {
        compiler->unwindPadding();
    }
}

//------------------------------------------------------------------------
// genAllocLclFrame: Probe the stack and allocate the local stack frame - subtract from SP.
//
// Notes:
//      The first instruction of the prolog is always a push (which touches the lowest address
//      of the stack), either of the LR register or of some argument registers, e.g., in the case of
//      pre-spilling. The LR register is always pushed because we require it to allow for GC return
//      address hijacking (see the comment in CodeGen::genPushCalleeSavedRegisters()). These pushes
//      happen immediately before calling this function, so the SP at the current location has already
//      been touched.
//
// Arguments:
//      frameSize         - the size of the stack frame being allocated.
//      initReg           - register to use as a scratch register.
//      pInitRegZeroed    - OUT parameter. *pInitRegZeroed is set to 'false' if and only if
//                          this call sets 'initReg' to a non-zero value.
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

    assert(!compiler->info.compPublishStubParam || (REG_SECRET_STUB_PARAM != initReg));

    if (frameSize < pageSize)
    {
        GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, frameSize);
    }
    else
    {
        // Generate the following code:
        //
        //    movw  r4, #frameSize
        //    sub   r4, sp, r4
        //    bl    CORINFO_HELP_STACK_PROBE
        //    mov   sp, r4
        //
        // If frameSize can not be encoded by movw immediate this becomes:
        //
        //    movw  r4, #frameSizeLo16
        //    movt  r4, #frameSizeHi16
        //    sub   r4, sp, r4
        //    bl    CORINFO_HELP_STACK_PROBE
        //    mov   sp, r4

        genInstrWithConstant(INS_sub, EA_PTRSIZE, REG_STACK_PROBE_HELPER_ARG, REG_SPBASE, frameSize,
                             INS_FLAGS_DONT_CARE, REG_STACK_PROBE_HELPER_ARG);
        regSet.verifyRegUsed(REG_STACK_PROBE_HELPER_ARG);
        genEmitHelperCall(CORINFO_HELP_STACK_PROBE, 0, EA_UNKNOWN, REG_STACK_PROBE_HELPER_CALL_TARGET);
        regSet.verifyRegUsed(REG_STACK_PROBE_HELPER_CALL_TARGET);
        compiler->unwindPadding();
        GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_SPBASE, REG_STACK_PROBE_HELPER_ARG, /* canSkip */ false);

        if ((genRegMask(initReg) & (RBM_STACK_PROBE_HELPER_ARG | RBM_STACK_PROBE_HELPER_CALL_TARGET |
                                    RBM_STACK_PROBE_HELPER_TRASH)) != RBM_NONE)
        {
            *pInitRegZeroed = false;
        }
    }

    compiler->unwindAllocStack(frameSize);
}

void CodeGen::genPushFltRegs(regMaskTP regMask)
{
    assert(regMask != 0);                        // Don't call uness we have some registers to push
    assert((regMask & RBM_ALLFLOAT) == regMask); // Only floasting point registers should be in regMask

    regNumber lowReg = genRegNumFromMask(genFindLowestBit(regMask));
    int       slots  = genCountBits(regMask);
    // regMask should be contiguously set
    regMaskTP tmpMask = ((regMask >> lowReg) + 1); // tmpMask should have a single bit set
    assert((tmpMask & (tmpMask - 1)) == 0);
    assert(lowReg == REG_F16); // Currently we expect to start at F16 in the unwind codes

    // Our calling convention requires that we only use vpush for TYP_DOUBLE registers
    noway_assert(floatRegCanHoldType(lowReg, TYP_DOUBLE));
    noway_assert((slots % 2) == 0);

    GetEmitter()->emitIns_R_I(INS_vpush, EA_8BYTE, lowReg, slots / 2);
}

void CodeGen::genPopFltRegs(regMaskTP regMask)
{
    assert(regMask != 0);                        // Don't call uness we have some registers to pop
    assert((regMask & RBM_ALLFLOAT) == regMask); // Only floasting point registers should be in regMask

    regNumber lowReg = genRegNumFromMask(genFindLowestBit(regMask));
    int       slots  = genCountBits(regMask);
    // regMask should be contiguously set
    regMaskTP tmpMask = ((regMask >> lowReg) + 1); // tmpMask should have a single bit set
    assert((tmpMask & (tmpMask - 1)) == 0);

    // Our calling convention requires that we only use vpop for TYP_DOUBLE registers
    noway_assert(floatRegCanHoldType(lowReg, TYP_DOUBLE));
    noway_assert((slots % 2) == 0);

    GetEmitter()->emitIns_R_I(INS_vpop, EA_8BYTE, lowReg, slots / 2);
}

//------------------------------------------------------------------------
// genFreeLclFrame: free the local stack frame by adding `frameSize` to SP.
//
// Arguments:
//   frameSize - the frame size to free;
//   pUnwindStarted - was epilog unwind started or not.
//
// Notes:
//   If epilog unwind hasn't been started, and we generate code, we start unwind
//    and set* pUnwindStarted = true.
//
void CodeGen::genFreeLclFrame(unsigned frameSize, /* IN OUT */ bool* pUnwindStarted)
{
    assert(compiler->compGeneratingEpilog);

    if (frameSize == 0)
        return;

    // Add 'frameSize' to SP.
    //
    // Unfortunately, we can't just use:
    //
    //      inst_RV_IV(INS_add, REG_SPBASE, frameSize, EA_PTRSIZE);
    //
    // because we need to generate proper unwind codes for each instruction generated,
    // and large frame sizes might generate a temp register load which might
    // need an unwind code. We don't want to generate a "NOP" code for this
    // temp register load; we want the unwind codes to start after that.

    if (arm_Valid_Imm_For_Instr(INS_add, frameSize, INS_FLAGS_DONT_CARE))
    {
        if (!*pUnwindStarted)
        {
            compiler->unwindBegEpilog();
            *pUnwindStarted = true;
        }

        GetEmitter()->emitIns_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, frameSize, INS_FLAGS_DONT_CARE);
    }
    else
    {
        // We always save LR for return address hijacking and it will be
        // restored after this point, so it is available for use here. The
        // other possibility is r12 but it is not available as it can be used
        // for the target address for fast tailcalls.
        regNumber tmpReg = REG_LR;
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, frameSize);
        if (*pUnwindStarted)
        {
            compiler->unwindPadding();
        }

        // We're going to generate an unwindable instruction, so check again if
        // we need to start the unwind codes.

        if (!*pUnwindStarted)
        {
            compiler->unwindBegEpilog();
            *pUnwindStarted = true;
        }

        GetEmitter()->emitIns_R_R(INS_add, EA_PTRSIZE, REG_SPBASE, tmpReg, INS_FLAGS_DONT_CARE);
    }

    compiler->unwindAllocStack(frameSize);
}

/*-----------------------------------------------------------------------------
 *
 *  Move of relocatable displacement value to register
 */
void CodeGen::genMov32RelocatableDisplacement(BasicBlock* block, regNumber reg)
{
    GetEmitter()->emitIns_R_L(INS_movw, EA_4BYTE_DSP_RELOC, block, reg);
    GetEmitter()->emitIns_R_L(INS_movt, EA_4BYTE_DSP_RELOC, block, reg);

    if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELATIVE_CODE_RELOCS))
    {
        GetEmitter()->emitIns_R_R_R(INS_add, EA_4BYTE_DSP_RELOC, reg, reg, REG_PC);
    }
}

/*-----------------------------------------------------------------------------
 *
 *  Move of relocatable data-label to register
 */
void CodeGen::genMov32RelocatableDataLabel(unsigned value, regNumber reg)
{
    GetEmitter()->emitIns_R_D(INS_movw, EA_HANDLE_CNS_RELOC, value, reg);
    GetEmitter()->emitIns_R_D(INS_movt, EA_HANDLE_CNS_RELOC, value, reg);

    if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELATIVE_CODE_RELOCS))
    {
        GetEmitter()->emitIns_R_R_R(INS_add, EA_HANDLE_CNS_RELOC, reg, reg, REG_PC);
    }
}

/*-----------------------------------------------------------------------------
 *
 * Move of relocatable immediate to register
 */
void CodeGen::genMov32RelocatableImmediate(emitAttr size, BYTE* addr, regNumber reg)
{
    _ASSERTE(EA_IS_RELOC(size));

    GetEmitter()->emitIns_MovRelocatableImmediate(INS_movw, size, reg, addr);
    GetEmitter()->emitIns_MovRelocatableImmediate(INS_movt, size, reg, addr);

    if (compiler->opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELATIVE_CODE_RELOCS))
    {
        GetEmitter()->emitIns_R_R_R(INS_add, size, reg, reg, REG_PC);
    }
}

/*-----------------------------------------------------------------------------
 *
 *  Returns register mask to push/pop to allocate a small stack frame,
 *  instead of using "sub sp" / "add sp". Returns RBM_NONE if either frame size
 *  is zero, or if we should use "sub sp" / "add sp" instead of push/pop.
 */
regMaskTP CodeGen::genStackAllocRegisterMask(unsigned frameSize, regMaskTP maskCalleeSavedFloat)
{
    assert(compiler->compGeneratingProlog || compiler->compGeneratingEpilog);

    // We can't do this optimization with callee saved floating point registers because
    // the stack would be allocated in a wrong spot.
    if (maskCalleeSavedFloat != RBM_NONE)
        return RBM_NONE;

    // Allocate space for small frames by pushing extra registers. It generates smaller and faster code
    // that extra sub sp,XXX/add sp,XXX.
    // R0 and R1 may be used by return value. Keep things simple and just skip the optimization
    // for the 3*REGSIZE_BYTES and 4*REGSIZE_BYTES cases. They are less common and they have more
    // significant negative side-effects (more memory bus traffic).
    switch (frameSize)
    {
        case REGSIZE_BYTES:
            return RBM_R3;
        case 2 * REGSIZE_BYTES:
            return RBM_R2 | RBM_R3;
        default:
            return RBM_NONE;
    }
}

//-----------------------------------------------------------------------------------
// instGen_MemoryBarrier: Emit a MemoryBarrier instruction
//
// Arguments:
//     barrierKind - kind of barrier to emit (ignored on arm32)
//
// Notes:
//     All MemoryBarriers instructions can be removed by DOTNET_JitNoMemoryBarriers=1
//     barrierKind argument is ignored on arm32 and a full memory barrier is emitted
//
void CodeGen::instGen_MemoryBarrier(BarrierKind barrierKind)
{
#ifdef DEBUG
    if (JitConfig.JitNoMemoryBarriers() == 1)
    {
        return;
    }
#endif // DEBUG

    // Avoid emitting redundant memory barriers on arm32 if they belong to the same IG
    // and there were no memory accesses in-between them
    if ((GetEmitter()->emitLastMemBarrier != nullptr) && compiler->opts.OptimizationEnabled())
    {
        assert(GetEmitter()->emitLastMemBarrier->idSmallCns() == INS_BARRIER_SY);
    }
    else
    {
        // ARM has only full barriers, so all barriers need to be emitted as full.
        GetEmitter()->emitIns_I(INS_dmb, EA_4BYTE, INS_BARRIER_SY);
    }
}

bool CodeGen::genCanUsePopToReturn(regMaskTP maskPopRegsInt, bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    if (!jmpEpilog && regSet.rsMaskPreSpillRegs(true) == RBM_NONE)
        return true;
    else
        return false;
}

void CodeGen::genPopCalleeSavedRegisters(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    regMaskTP maskPopRegs      = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;
    regMaskTP maskPopRegsFloat = maskPopRegs & RBM_ALLFLOAT;
    regMaskTP maskPopRegsInt   = maskPopRegs & ~maskPopRegsFloat;

    // First, pop float registers

    if (maskPopRegsFloat != RBM_NONE)
    {
        genPopFltRegs(maskPopRegsFloat);
        compiler->unwindPopMaskFloat(maskPopRegsFloat);
    }

    // Next, pop integer registers

    if (!jmpEpilog)
    {
        regMaskTP maskStackAlloc = genStackAllocRegisterMask(compiler->compLclFrameSize, maskPopRegsFloat);
        maskPopRegsInt |= maskStackAlloc;
    }

    if (isFramePointerUsed())
    {
        assert(!regSet.rsRegsModified(RBM_FPBASE));
        maskPopRegsInt |= RBM_FPBASE;
    }

    if (genCanUsePopToReturn(maskPopRegsInt, jmpEpilog))
    {
        maskPopRegsInt |= RBM_PC;
        // Record the fact that we use a pop to the PC to perform the return
        genUsedPopToReturn = true;
    }
    else
    {
        maskPopRegsInt |= RBM_LR;
        // Record the fact that we did not use a pop to the PC to perform the return
        genUsedPopToReturn = false;
    }

    assert(FitsIn<int>(maskPopRegsInt));
    inst_IV(INS_pop, (int)maskPopRegsInt);
    compiler->unwindPopMaskInt(maskPopRegsInt);
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet prolog.
 *
 *  Funclets have the following incoming arguments:
 *
 *      catch:          r0 = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:         r0 = the exception object to filter (see GT_CATCH_ARG), r1 = CallerSP of the containing function
 *      finally/fault:  none
 *
 *  Funclets set the following registers on exit:
 *
 *      catch:          r0 = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:         r0 = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:  none
 *
 *  The ARM funclet prolog sequence is:
 *
 *     push {regs,lr}   ; We push the callee-saved regs and 'lr'.
 *                      ;   TODO-ARM-CQ: We probably only need to save lr, plus any callee-save registers that we
 *                      ;         actually use in the funclet. Currently, we save the same set of callee-saved regs
 *                      ;         calculated for the entire function.
 *     sub sp, XXX      ; Establish the rest of the frame.
 *                      ;   XXX is determined by lvaOutgoingArgSpaceSize plus space for the PSP slot, aligned
 *                      ;   up to preserve stack alignment. If we push an odd number of registers, we also
 *                      ;   generate this, to keep the stack aligned.
 *
 *     ; Fill the PSP slot, for use by the VM (it gets reported with the GC info), or by code generation of nested
 *     ;     filters.
 *     ; This is not part of the "OS prolog"; it has no associated unwind data, and is not reversed in the funclet
 *     ;     epilog.
 *
 *     if (this is a filter funclet)
 *     {
 *          // r1 on entry to a filter funclet is CallerSP of the containing function:
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
 *          //    } filter {                         // ... will be processed here, while the "catch" funclet frame is
 *          //                                       // still on the stack
 *          //    } filter-handler {
 *          //    }
 *          //
 *          // Because of this, we need a PSP in the main function anytime a filter funclet doesn't know whether the
 *          // enclosing frame will be a funclet or main function. We won't know any time there is a filter protecting
 *          // nested EH. To simplify, we just always create a main function PSP for any function with a filter.
 *
 *          ldr r1, [r1 - PSP_slot_CallerSP_offset]     ; Load the CallerSP of the main function (stored in the PSP of
 *                                                      ; the dynamically containing funclet or function)
 *          str r1, [sp + PSP_slot_SP_offset]           ; store the PSP
 *          sub r11, r1, Function_CallerSP_to_FP_delta  ; re-establish the frame pointer
 *     }
 *     else
 *     {
 *          // This is NOT a filter funclet. The VM re-establishes the frame pointer on entry.
 *          // TODO-ARM-CQ: if VM set r1 to CallerSP on entry, like for filters, we could save an instruction.
 *
 *          add r3, r11, Function_CallerSP_to_FP_delta  ; compute the CallerSP, given the frame pointer. r3 is scratch.
 *          str r3, [sp + PSP_slot_SP_offset]           ; store the PSP
 *     }
 *
 *  The epilog sequence is then:
 *
 *     add sp, XXX      ; if necessary
 *     pop {regs,pc}
 *
 *  If it is worth it, we could push r0, r1, r2, r3 instead of using an additional add/sub instruction.
 *  Code size would be smaller, but we would be writing to / reading from the stack, which might be slow.
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |Callee saved registers |
 *      |-----------------------|
 *      |Pre-spill regs space   |   // This is only necessary to keep the PSP slot at the same offset
 *      |                       |   // in function and funclet
 *      |-----------------------|
 *      |        PSP slot       |   // Omitted in NativeAOT ABI
 *      |-----------------------|
 *      ~  possible 4 byte pad  ~
 *      ~     for alignment     ~
 *      |-----------------------|
 *      |   Outgoing arg space  |
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 */

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletProlog()\n");
#endif

    assert(block != NULL);
    assert(block->HasFlag(BBF_FUNCLET_BEG));

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    regMaskTP maskPushRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskPushRegsInt   = genFuncletInfo.fiSaveRegs & ~maskPushRegsFloat;

    regMaskTP maskStackAlloc = genStackAllocRegisterMask(genFuncletInfo.fiSpDelta, maskPushRegsFloat);
    maskPushRegsInt |= maskStackAlloc;

    assert(FitsIn<int>(maskPushRegsInt));
    inst_IV(INS_push, (int)maskPushRegsInt);
    compiler->unwindPushMaskInt(maskPushRegsInt);

    if (maskPushRegsFloat != RBM_NONE)
    {
        genPushFltRegs(maskPushRegsFloat);
        compiler->unwindPushMaskFloat(maskPushRegsFloat);
    }

    bool isFilter = (block->bbCatchTyp == BBCT_FILTER);

    regMaskTP maskArgRegsLiveIn;
    if (isFilter)
    {
        maskArgRegsLiveIn = RBM_R0 | RBM_R1;
    }
    else if ((block->bbCatchTyp == BBCT_FINALLY) || (block->bbCatchTyp == BBCT_FAULT))
    {
        maskArgRegsLiveIn = RBM_NONE;
    }
    else
    {
        maskArgRegsLiveIn = RBM_R0;
    }

    regNumber initReg       = REG_R3; // R3 is never live on entry to a funclet, so it can be trashed
    bool      initRegZeroed = false;

    if (maskStackAlloc == RBM_NONE)
    {
        genAllocLclFrame(genFuncletInfo.fiSpDelta, initReg, &initRegZeroed, maskArgRegsLiveIn);
    }

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // If there is no PSPSym (NativeAOT ABI), we are done.
    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    if (isFilter)
    {
        // This is the first block of a filter

        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, REG_R1, REG_R1, genFuncletInfo.fiPSP_slot_CallerSP_offset);
        regSet.verifyRegUsed(REG_R1);
        GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_R1, REG_SPBASE, genFuncletInfo.fiPSP_slot_SP_offset);
        GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_FPBASE, REG_R1,
                                    genFuncletInfo.fiFunctionCallerSPtoFPdelta);
    }
    else
    {
        // This is a non-filter funclet
        GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_R3, REG_FPBASE,
                                    genFuncletInfo.fiFunctionCallerSPtoFPdelta);
        regSet.verifyRegUsed(REG_R3);
        GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_R3, REG_SPBASE, genFuncletInfo.fiPSP_slot_SP_offset);
    }
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 */

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletEpilog()\n");
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    // Just as for the main function, we delay starting the unwind codes until we have
    // an instruction which we know needs an unwind code. This is to support code like
    // this:
    //      movw    r3, 0x38e0
    //      add     sp, r3
    //      pop     {r4,r5,r6,r10,r11,pc}
    // where the "movw" shouldn't be part of the unwind codes. See genFnEpilog() for more details.

    bool unwindStarted = false;

    /* The saved regs info saves the LR register. We need to pop the PC register to return */
    assert(genFuncletInfo.fiSaveRegs & RBM_LR);

    regMaskTP maskPopRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskPopRegsInt   = genFuncletInfo.fiSaveRegs & ~maskPopRegsFloat;

    regMaskTP maskStackAlloc = genStackAllocRegisterMask(genFuncletInfo.fiSpDelta, maskPopRegsFloat);
    maskPopRegsInt |= maskStackAlloc;

    if (maskStackAlloc == RBM_NONE)
    {
        genFreeLclFrame(genFuncletInfo.fiSpDelta, &unwindStarted);
    }

    if (!unwindStarted)
    {
        // We'll definitely generate an unwindable instruction next
        compiler->unwindBegEpilog();
        unwindStarted = true;
    }

    maskPopRegsInt &= ~RBM_LR;
    maskPopRegsInt |= RBM_PC;

    if (maskPopRegsFloat != RBM_NONE)
    {
        genPopFltRegs(maskPopRegsFloat);
        compiler->unwindPopMaskFloat(maskPopRegsFloat);
    }

    assert(FitsIn<int>(maskPopRegsInt));
    inst_IV(INS_pop, (int)maskPopRegsInt);
    compiler->unwindPopMaskInt(maskPopRegsInt);

    compiler->unwindEndEpilog();
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
    if (compiler->ehAnyFunclets())
    {
        assert(isFramePointerUsed());
        assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT); // The frame size and offsets must be
                                                                              // finalized

        // Frame pointer doesn't point at the end, it points at the pushed r11. So, instead
        // of adding the number of callee-saved regs to CallerSP, we add 1 for lr and 1 for r11
        // (plus the "pre spill regs"). Note that we assume r12 and r13 aren't saved
        // (also assumed in genFnProlog()).
        assert((regSet.rsMaskCalleeSaved & (RBM_R12 | RBM_R13)) == 0);
        unsigned preSpillRegArgSize                = genCountBits(regSet.rsMaskPreSpillRegs(true)) * REGSIZE_BYTES;
        genFuncletInfo.fiFunctionCallerSPtoFPdelta = preSpillRegArgSize + 2 * REGSIZE_BYTES;

        regMaskTP rsMaskSaveRegs  = regSet.rsMaskCalleeSaved;
        unsigned  saveRegsCount   = genCountBits(rsMaskSaveRegs);
        unsigned  saveRegsSize    = saveRegsCount * REGSIZE_BYTES; // bytes of regs we're saving
        unsigned  saveSizeWithPSP = saveRegsSize + REGSIZE_BYTES /* PSP sym */;
        if (compiler->lvaMonAcquired != BAD_VAR_NUM)
        {
            saveSizeWithPSP += TARGET_POINTER_SIZE;
        }
        assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
        unsigned funcletFrameSize = preSpillRegArgSize + saveSizeWithPSP + compiler->lvaOutgoingArgSpaceSize;

        unsigned funcletFrameSizeAligned  = roundUp(funcletFrameSize, STACK_ALIGN);
        unsigned funcletFrameAlignmentPad = funcletFrameSizeAligned - funcletFrameSize;
        unsigned spDelta                  = funcletFrameSizeAligned - saveRegsSize;

        unsigned PSP_slot_SP_offset = compiler->lvaOutgoingArgSpaceSize + funcletFrameAlignmentPad;
        int      PSP_slot_CallerSP_offset =
            -(int)(funcletFrameSize - compiler->lvaOutgoingArgSpaceSize); // NOTE: it's negative!

        /* Now save it for future use */

        genFuncletInfo.fiSaveRegs                 = rsMaskSaveRegs;
        genFuncletInfo.fiSpDelta                  = spDelta;
        genFuncletInfo.fiPSP_slot_SP_offset       = PSP_slot_SP_offset;
        genFuncletInfo.fiPSP_slot_CallerSP_offset = PSP_slot_CallerSP_offset;

#ifdef DEBUG
        if (verbose)
        {
            printf("\n");
            printf("Funclet prolog / epilog info\n");
            printf("    Function CallerSP-to-FP delta: %d\n", genFuncletInfo.fiFunctionCallerSPtoFPdelta);
            printf("                        Save regs: ");
            dspRegMask(rsMaskSaveRegs);
            printf("\n");
            printf("                         SP delta: %d\n", genFuncletInfo.fiSpDelta);
            printf("               PSP slot SP offset: %d\n", genFuncletInfo.fiPSP_slot_SP_offset);
            printf("        PSP slot Caller SP offset: %d\n", genFuncletInfo.fiPSP_slot_CallerSP_offset);

            if (PSP_slot_CallerSP_offset != compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym))
            {
                printf("lvaGetCallerSPRelativeOffset(lvaPSPSym): %d\n",
                       compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym));
            }
        }
#endif // DEBUG

        assert(PSP_slot_CallerSP_offset < 0);
        if (compiler->lvaPSPSym != BAD_VAR_NUM)
        {
            assert(PSP_slot_CallerSP_offset ==
                   compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main
                                                                                 // function and funclet!
        }
    }
}

void CodeGen::genSetPSPSym(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    noway_assert(isFramePointerUsed()); // We need an explicit frame pointer

    // We either generate:
    //     add     r1, r11, 8
    //     str     r1, [reg + PSPSymOffset]
    // or:
    //     add     r1, sp, 76
    //     str     r1, [reg + PSPSymOffset]
    // depending on the smallest encoding

    int SPtoCallerSPdelta = -genCallerSPtoInitialSPdelta();

    int       callerSPOffs;
    regNumber regBase;

    if (arm_Valid_Imm_For_Add_SP(SPtoCallerSPdelta))
    {
        // use the "add <reg>, sp, imm" form

        callerSPOffs = SPtoCallerSPdelta;
        regBase      = REG_SPBASE;
    }
    else
    {
        // use the "add <reg>, r11, imm" form

        int FPtoCallerSPdelta = -genCallerSPtoFPdelta();
        noway_assert(arm_Valid_Imm_For_Add(FPtoCallerSPdelta, INS_FLAGS_DONT_CARE));

        callerSPOffs = FPtoCallerSPdelta;
        regBase      = REG_FPBASE;
    }

    // We will just use the initReg since it is an available register
    // and we are probably done using it anyway...
    regNumber regTmp = initReg;
    *pInitRegZeroed  = false;

    GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, regTmp, regBase, callerSPOffs);
    GetEmitter()->emitIns_S_R(INS_str, EA_PTRSIZE, regTmp, compiler->lvaPSPSym, 0);
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
    assert(genUseBlockInit);
    assert(untrLclHi > untrLclLo);

    // Generate the following code:
    //
    // For cnt less than 10
    //
    //            mov     rZero1, 0
    //            mov     rZero2, 0
    //            mov     rCnt,  <cnt>
    //            stm     <rZero1,rZero2>,[rAddr!]
    // <optional> stm     <rZero1,rZero2>,[rAddr!]
    // <optional> stm     <rZero1,rZero2>,[rAddr!]
    // <optional> stm     <rZero1,rZero2>,[rAddr!]
    // <optional> str     rZero1,[rAddr]
    //
    // For rCnt greater than or equal to 10
    //
    //            mov     rZero1, 0
    //            mov     rZero2, 0
    //            mov     rCnt,  <cnt/2>
    //            sub     rAddr, sp, OFFS
    //
    //        loop:
    //            stm     <rZero1,rZero2>,[rAddr!]
    //            sub     rCnt,rCnt,1
    //            jnz     loop
    //
    // <optional> str     rZero1,[rAddr]   // When cnt is odd

    regNumber rAddr;
    regNumber rCnt = REG_NA; // Invalid
    regMaskTP regMask;

    regMaskTP availMask = regSet.rsGetModifiedRegsMask() | RBM_INT_CALLEE_TRASH; // Set of available registers
    availMask &= ~intRegState.rsCalleeRegArgMaskLiveIn; // Remove all of the incoming argument registers as they are
                                                        // currently live
    availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg as we will zero it and maybe use it for
                                       // a large constant.

    if (compiler->compLocallocUsed)
    {
        availMask &= ~RBM_SAVED_LOCALLOC_SP; // Remove the register reserved when we have a localloc frame
    }

    regNumber rZero1; // We're going to use initReg for rZero1
    regNumber rZero2;

    // We pick the next lowest register number for rZero2
    noway_assert(availMask != RBM_NONE);
    regMask = genFindLowestBit(availMask);
    rZero2  = genRegNumFromMask(regMask);
    availMask &= ~regMask;
    assert((genRegMask(rZero2) & intRegState.rsCalleeRegArgMaskLiveIn) == 0); // rZero2 is not a live incoming
                                                                              // argument reg

    // We pick the next lowest register number for rAddr
    noway_assert(availMask != RBM_NONE);
    regMask = genFindLowestBit(availMask);
    rAddr   = genRegNumFromMask(regMask);
    availMask &= ~regMask;

    bool     useLoop   = false;
    unsigned uCntBytes = untrLclHi - untrLclLo;
    assert((uCntBytes % sizeof(int)) == 0);         // The smallest stack slot is always 4 bytes.
    unsigned uCntSlots = uCntBytes / REGSIZE_BYTES; // How many register sized stack slots we're going to use.

    // When uCntSlots is 9 or less, we will emit a sequence of stm/stp instructions inline.
    // When it is 10 or greater, we will emit a loop containing a stm/stp instruction.
    // In both of these cases the stm/stp instruction will write two zeros to memory
    // and we will use a single str instruction at the end whenever we have an odd count.
    if (uCntSlots >= 10)
        useLoop = true;

    if (useLoop)
    {
        // We pick the next lowest register number for rCnt
        noway_assert(availMask != RBM_NONE);
        regMask = genFindLowestBit(availMask);
        rCnt    = genRegNumFromMask(regMask);
        availMask &= ~regMask;
    }

    // rAddr is not a live incoming argument reg
    assert((genRegMask(rAddr) & intRegState.rsCalleeRegArgMaskLiveIn) == 0);

    if (arm_Valid_Imm_For_Add(untrLclLo, INS_FLAGS_DONT_CARE))
    {
        GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, rAddr, genFramePointerReg(), untrLclLo);
    }
    else
    {
        // Load immediate into the InitReg register
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, (ssize_t)untrLclLo);
        GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, rAddr, genFramePointerReg(), initReg);
        *pInitRegZeroed = false;
    }

    if (useLoop)
    {
        noway_assert(uCntSlots >= 2);
        assert((genRegMask(rCnt) & intRegState.rsCalleeRegArgMaskLiveIn) == 0); // rCnt is not a live incoming
                                                                                // argument reg
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rCnt, (ssize_t)uCntSlots / 2);
    }

    rZero1 = genGetZeroReg(initReg, pInitRegZeroed);
    instGen_Set_Reg_To_Zero(EA_PTRSIZE, rZero2);
    target_ssize_t stmImm = (target_ssize_t)(genRegMask(rZero1) | genRegMask(rZero2));

    if (!useLoop)
    {
        while (uCntBytes >= REGSIZE_BYTES * 2)
        {
            GetEmitter()->emitIns_R_I(INS_stm, EA_PTRSIZE, rAddr, stmImm);
            uCntBytes -= REGSIZE_BYTES * 2;
        }
    }
    else
    {
        GetEmitter()->emitIns_R_I(INS_stm, EA_PTRSIZE, rAddr, stmImm); // zero stack slots
        GetEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, rCnt, 1, INS_FLAGS_SET);
        GetEmitter()->emitIns_J(INS_bhi, NULL, -3);
        uCntBytes %= REGSIZE_BYTES * 2;
    }

    if (uCntBytes >= REGSIZE_BYTES) // check and zero the last register-sized stack slot (odd number)
    {
        GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, rZero1, rAddr, 0);
        uCntBytes -= REGSIZE_BYTES;
    }

    noway_assert(uCntBytes == 0);
}

#endif // TARGET_ARM
