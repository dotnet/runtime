// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        Arm64 Code Generator                               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_ARM64
#include "emit.h"
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"
#include "patchpointinfo.h"

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Prolog / Epilog                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

void CodeGen::genPopCalleeSavedRegistersAndFreeLclFrame(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    regMaskTP rsRestoreRegs = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

    if (isFramePointerUsed())
    {
        rsRestoreRegs |= RBM_FPBASE;
    }

    rsRestoreRegs |= RBM_LR; // We must save/restore the return address (in the LR register)

    regMaskTP regsToRestoreMask = rsRestoreRegs;

    const int totalFrameSize = genTotalFrameSize();

    // Fetch info about the frame we saved when creating the prolog.
    //
    const int frameType          = compiler->compFrameInfo.frameType;
    const int calleeSaveSpOffset = compiler->compFrameInfo.calleeSaveSpOffset;
    const int calleeSaveSpDelta  = compiler->compFrameInfo.calleeSaveSpDelta;
    const int offsetSpToSavedFp  = compiler->compFrameInfo.offsetSpToSavedFp;

    switch (frameType)
    {
        case 1:
        {
            JITDUMP("Frame type 1. #outsz=0; #framesz=%d; localloc? %s\n", totalFrameSize,
                    dspBool(compiler->compLocallocUsed));

            if (compiler->compLocallocUsed)
            {
                // Restore sp from fp
                //      mov sp, fp
                inst_Mov(TYP_I_IMPL, REG_SPBASE, REG_FPBASE, /* canSkip */ false);
                compiler->unwindSetFrameReg(REG_FPBASE, 0);
            }

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and post-index SP.
            break;
        }

        case 2:
        {
            JITDUMP("Frame type 2 (save FP/LR at bottom). #outsz=%d; #framesz=%d; localloc? %s\n",
                    unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, dspBool(compiler->compLocallocUsed));

            assert(!genSaveFpLrWithAllCalleeSavedRegisters);

            if (compiler->compLocallocUsed)
            {
                // Restore sp from fp
                //      sub sp, fp, #outsz // Uses #outsz if FP/LR stored at bottom
                int SPtoFPdelta = genSPtoFPdelta();
                GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, SPtoFPdelta);
                compiler->unwindSetFrameReg(REG_FPBASE, SPtoFPdelta);
            }

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and post-index SP.
            break;
        }

        case 3:
        {
            JITDUMP("Frame type 3 (save FP/LR at bottom). #outsz=%d; #framesz=%d; localloc? %s\n",
                    unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, dspBool(compiler->compLocallocUsed));

            assert(!genSaveFpLrWithAllCalleeSavedRegisters);

            JITDUMP("    calleeSaveSpDelta=%d\n", calleeSaveSpDelta);

            regsToRestoreMask &= ~(RBM_FP | RBM_LR); // We'll restore FP/LR at the end, and (hopefully) post-index SP.

            int remainingFrameSz = totalFrameSize - calleeSaveSpDelta;
            assert(remainingFrameSz > 0);

            if (compiler->lvaOutgoingArgSpaceSize > 504)
            {
                // We can't do "ldp fp,lr,[sp,#outsz]" because #outsz is too big.
                // If compiler->lvaOutgoingArgSpaceSize is not aligned, we need to align the SP adjustment.
                assert(remainingFrameSz > (int)compiler->lvaOutgoingArgSpaceSize);
                int spAdjustment2Unaligned = remainingFrameSz - compiler->lvaOutgoingArgSpaceSize;
                int spAdjustment2          = (int)roundUp((unsigned)spAdjustment2Unaligned, STACK_ALIGN);
                int alignmentAdjustment2   = spAdjustment2 - spAdjustment2Unaligned;
                assert((alignmentAdjustment2 == 0) || (alignmentAdjustment2 == REGSIZE_BYTES));

                // Restore sp from fp. No need to update sp after this since we've set up fp before adjusting sp
                // in prolog.
                //      sub sp, fp, #alignmentAdjustment2
                GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, alignmentAdjustment2);
                compiler->unwindSetFrameReg(REG_FPBASE, alignmentAdjustment2);

                // Generate:
                //      ldp fp,lr,[sp]
                //      add sp,sp,#remainingFrameSz

                JITDUMP("    alignmentAdjustment2=%d\n", alignmentAdjustment2);
                genEpilogRestoreRegPair(REG_FP, REG_LR, alignmentAdjustment2, spAdjustment2, false, REG_IP1, nullptr);
            }
            else
            {
                if (compiler->compLocallocUsed)
                {
                    // Restore sp from fp; here that's #outsz from SP
                    //      sub sp, fp, #outsz
                    int SPtoFPdelta = genSPtoFPdelta();
                    assert(SPtoFPdelta == (int)compiler->lvaOutgoingArgSpaceSize);
                    GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, SPtoFPdelta);
                    compiler->unwindSetFrameReg(REG_FPBASE, SPtoFPdelta);
                }

                // Generate:
                //      ldp fp,lr,[sp,#outsz]
                //      add sp,sp,#remainingFrameSz     ; might need to load this constant in a scratch register if
                //                                      ; it's large

                JITDUMP("    remainingFrameSz=%d\n", remainingFrameSz);

                genEpilogRestoreRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize, remainingFrameSz, false,
                                        REG_IP1, nullptr);
            }

            // Unlike frameType=1 or frameType=2 that restore SP at the end,
            // frameType=3 already adjusted SP above to delete local frame.
            // There is at most one alignment slot between SP and where we store the callee-saved registers.
            assert((calleeSaveSpOffset == 0) || (calleeSaveSpOffset == REGSIZE_BYTES));

            break;
        }

        case 4:
        {
            JITDUMP("Frame type 4 (save FP/LR at top). #outsz=%d; #framesz=%d; localloc? %s\n",
                    unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, dspBool(compiler->compLocallocUsed));

            assert(genSaveFpLrWithAllCalleeSavedRegisters);

            if (compiler->compLocallocUsed)
            {
                // Restore sp from fp
                //      sub sp, fp, #outsz // Uses #outsz if FP/LR stored at bottom
                int SPtoFPdelta = genSPtoFPdelta();
                GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, SPtoFPdelta);
                compiler->unwindSetFrameReg(REG_FPBASE, SPtoFPdelta);
            }
            break;
        }

        case 5:
        {
            JITDUMP("Frame type 5 (save FP/LR at top). #outsz=%d; #framesz=%d; localloc? %s\n",
                    unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, dspBool(compiler->compLocallocUsed));

            assert(genSaveFpLrWithAllCalleeSavedRegisters);
            assert((calleeSaveSpOffset == 0) || (calleeSaveSpOffset == REGSIZE_BYTES));

            // Restore sp from fp:
            //      sub sp, fp, #sp-to-fp-delta
            // This is the same whether there is localloc or not. Note that we don't need to do anything to remove the
            // "remainingFrameSz" to reverse the SUB of that amount in the prolog.
            GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, offsetSpToSavedFp);
            compiler->unwindSetFrameReg(REG_FPBASE, offsetSpToSavedFp);
            break;
        }

        default:
            unreached();
    }

    JITDUMP("    calleeSaveSpOffset=%d, calleeSaveSpDelta=%d\n", calleeSaveSpOffset, calleeSaveSpDelta);
    genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, calleeSaveSpOffset, calleeSaveSpDelta);

    switch (frameType)
    {
        case 1:
        {
            // Generate:
            //      ldp fp,lr,[sp],#framesz

            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, totalFrameSize,
                                          INS_OPTS_POST_INDEX);
            compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, -totalFrameSize);
            break;
        }

        case 2:
        {
            // Generate:
            //      ldp fp,lr,[sp,#outsz]
            //      add sp,sp,#framesz

            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                          compiler->lvaOutgoingArgSpaceSize);
            compiler->unwindSaveRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize);

            GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, totalFrameSize);
            compiler->unwindAllocStack(totalFrameSize);
            break;
        }
        case 3:
        case 4:
        case 5:
        {
            // Nothing to do after restoring callee-saved registers.
            break;
        }

        default:
        {
            unreached();
        }
    }

    // For OSR, we must also adjust the SP to remove the Tier0 frame.
    //
    if (compiler->opts.IsOSR())
    {
        PatchpointInfo* const patchpointInfo = compiler->info.compPatchpointInfo;
        const int             tier0FrameSize = patchpointInfo->TotalFrameSize();
        JITDUMP("Extra SP adjust for OSR to pop off Tier0 frame: %d bytes\n", tier0FrameSize);

        // Tier0 size may exceed simple immediate. We're in the epilog so not clear if we can
        // use a scratch reg. So just do two subtracts if necessary.
        //
        int spAdjust = tier0FrameSize;
        if (!GetEmitter()->emitIns_valid_imm_for_add(tier0FrameSize, EA_PTRSIZE))
        {
            const int lowPart  = spAdjust & 0xFFF;
            const int highPart = spAdjust - lowPart;
            assert(GetEmitter()->emitIns_valid_imm_for_add(highPart, EA_PTRSIZE));
            GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, highPart);
            compiler->unwindAllocStack(highPart);
            spAdjust = lowPart;
        }
        assert(GetEmitter()->emitIns_valid_imm_for_add(spAdjust, EA_PTRSIZE));
        GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spAdjust);
        compiler->unwindAllocStack(spAdjust);
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
    bool     immFitsInIns = false;
    emitAttr size         = EA_SIZE(attr);

    // reg1 is usually a dest register
    // reg2 is always source register
    assert(tmpReg != reg2); // regTmp can not match any source register

    switch (ins)
    {
        case INS_add:
        case INS_sub:
            if (imm < 0)
            {
                imm = -imm;
                ins = (ins == INS_add) ? INS_sub : INS_add;
            }
            immFitsInIns = emitter::emitIns_valid_imm_for_add(imm, size);
            break;

        case INS_strb:
            assert(size == EA_1BYTE);
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, EA_1BYTE);
            break;

        case INS_strh:
            assert(size == EA_2BYTE);
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, EA_2BYTE);
            break;

        case INS_str:
            // reg1 is a source register for store instructions
            assert(tmpReg != reg1); // regTmp can not match any source register
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, size);
            break;

        case INS_ldrb:
        case INS_ldrsb:
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, EA_1BYTE);
            break;

        case INS_ldrh:
        case INS_ldrsh:
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, EA_2BYTE);
            break;

        case INS_ldrsw:
            assert(size == EA_4BYTE);
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, EA_4BYTE);
            break;

        case INS_ldr:
            assert((size == EA_4BYTE) || (size == EA_8BYTE) || (size == EA_16BYTE));
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, size);
            break;

        default:
            assert(!"Unexpected instruction in genInstrWithConstant");
            break;
    }

    if (immFitsInIns)
    {
        // generate a single instruction that encodes the immediate directly
        GetEmitter()->emitIns_R_R_I(ins, attr, reg1, reg2, imm);
    }
    else
    {
        // caller can specify REG_NA  for tmpReg, when it "knows" that the immediate will always fit
        assert(tmpReg != REG_NA);

        // generate two or more instructions

        // first we load the immediate into tmpReg
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, imm);
        regSet.verifyRegUsed(tmpReg);

        // when we are in an unwind code region
        // we record the extra instructions using unwindPadding()
        if (inUnwindRegion)
        {
            compiler->unwindPadding();
        }

        // generate the instruction using a three register encoding with the immediate in tmpReg
        GetEmitter()->emitIns_R_R_R(ins, attr, reg1, reg2, tmpReg);
    }
    return immFitsInIns;
}

//------------------------------------------------------------------------
// genStackPointerAdjustment: add a specified constant value to the stack pointer in either the prolog
// or the epilog. The unwind codes for the generated instructions are produced. An available temporary
// register is required to be specified, in case the constant is too large to encode in an "add"
// instruction (or "sub" instruction if we choose to use one), such that we need to load the constant
// into a register first, before using it.
//
// Arguments:
//    spDelta                 - the value to add to SP (can be negative)
//    tmpReg                  - an available temporary register
//    pTmpRegIsZero           - If we use tmpReg, and pTmpRegIsZero is non-null, we set *pTmpRegIsZero to 'false'.
//                              Otherwise, we don't touch it.
//    reportUnwindData        - If true, report the change in unwind data. Otherwise, do not report it.
//
// Return Value:
//    None.

void CodeGen::genStackPointerAdjustment(ssize_t spDelta, regNumber tmpReg, bool* pTmpRegIsZero, bool reportUnwindData)
{
    // Even though INS_add is specified here, the encoder will choose either
    // an INS_add or an INS_sub and encode the immediate as a positive value
    //
    bool wasTempRegisterUsedForImm =
        !genInstrWithConstant(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta, tmpReg, true);
    if (wasTempRegisterUsedForImm)
    {
        if (pTmpRegIsZero != nullptr)
        {
            *pTmpRegIsZero = false;
        }
    }

    if (reportUnwindData)
    {
        // spDelta is negative in the prolog, positive in the epilog, but we always tell the unwind codes the positive
        // value.
        ssize_t  spDeltaAbs    = abs(spDelta);
        unsigned unwindSpDelta = (unsigned)spDeltaAbs;
        assert((ssize_t)unwindSpDelta == spDeltaAbs); // make sure that it fits in a unsigned

        compiler->unwindAllocStack(unwindSpDelta);
    }
}

//------------------------------------------------------------------------
// genPrologSaveRegPair: Save a pair of general-purpose or floating-point/SIMD registers in a function or funclet
// prolog. If possible, we use pre-indexed addressing to adjust SP and store the registers with a single instruction.
// The caller must ensure that we can use the STP instruction, and that spOffset will be in the legal range for that
// instruction.
//
// Arguments:
//    reg1                     - First register of pair to save.
//    reg2                     - Second register of pair to save.
//    spOffset                 - The offset from SP to store reg1 (must be positive or zero).
//    spDelta                  - If non-zero, the amount to add to SP before the register saves (must be negative or
//                               zero).
//    useSaveNextPair          - True if the last prolog instruction was to save the previous register pair. This
//                               allows us to emit the "save_next" unwind code.
//    tmpReg                   - An available temporary register. Needed for the case of large frames.
//    pTmpRegIsZero            - If we use tmpReg, and pTmpRegIsZero is non-null, we set *pTmpRegIsZero to 'false'.
//                               Otherwise, we don't touch it.
//
// Return Value:
//    None.

void CodeGen::genPrologSaveRegPair(regNumber reg1,
                                   regNumber reg2,
                                   int       spOffset,
                                   int       spDelta,
                                   bool      useSaveNextPair,
                                   regNumber tmpReg,
                                   bool*     pTmpRegIsZero)
{
    assert(spOffset >= 0);
    assert(spDelta <= 0);
    assert((spDelta % 16) == 0);                                  // SP changes must be 16-byte aligned
    assert(genIsValidFloatReg(reg1) == genIsValidFloatReg(reg2)); // registers must be both general-purpose, or both
                                                                  // FP/SIMD

    bool needToSaveRegs = true;
    if (spDelta != 0)
    {
        assert(!useSaveNextPair);
        if ((spOffset == 0) && (spDelta >= -512))
        {
            // We can use pre-indexed addressing.
            // stp REG, REG + 1, [SP, #spDelta]!
            // 64-bit STP offset range: -512 to 504, multiple of 8.
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spDelta, INS_OPTS_PRE_INDEX);
            compiler->unwindSaveRegPairPreindexed(reg1, reg2, spDelta);

            needToSaveRegs = false;
        }
        else // (spOffset != 0) || (spDelta < -512)
        {
            // We need to do SP adjustment separately from the store; we can't fold in a pre-indexed addressing and the
            // non-zero offset.

            // generate sub SP,SP,imm
            genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
        }
    }

    if (needToSaveRegs)
    {
        // stp REG, REG + 1, [SP, #offset]
        // 64-bit STP offset range: -512 to 504, multiple of 8.
        assert(spOffset <= 504);
        assert((spOffset % 8) == 0);
        GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spOffset);

        if (TargetOS::IsUnix && compiler->generateCFIUnwindCodes())
        {
            useSaveNextPair = false;
        }

        if (useSaveNextPair)
        {
            // This works as long as we've only been saving pairs, in order, and we've saved the previous one just
            // before this one.
            compiler->unwindSaveNext();
        }
        else
        {
            compiler->unwindSaveRegPair(reg1, reg2, spOffset);
        }
    }
}

//------------------------------------------------------------------------
// genPrologSaveReg: Like genPrologSaveRegPair, but for a single register. Save a single general-purpose or
// floating-point/SIMD register in a function or funclet prolog. Note that if we wish to change SP (i.e., spDelta != 0),
// then spOffset must be 8. This is because otherwise we would create an alignment hole above the saved register, not
// below it, which we currently don't support. This restriction could be loosened if the callers change to handle it
// (and this function changes to support using pre-indexed STR addressing). The caller must ensure that we can use the
// STR instruction, and that spOffset will be in the legal range for that instruction.
//
// Arguments:
//    reg1                     - Register to save.
//    spOffset                 - The offset from SP to store reg1 (must be positive or zero).
//    spDelta                  - If non-zero, the amount to add to SP before the register saves (must be negative or
//                               zero).
//    tmpReg                   - An available temporary register. Needed for the case of large frames.
//    pTmpRegIsZero            - If we use tmpReg, and pTmpRegIsZero is non-null, we set *pTmpRegIsZero to 'false'.
//                               Otherwise, we don't touch it.
//
// Return Value:
//    None.

void CodeGen::genPrologSaveReg(regNumber reg1, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero)
{
    assert(spOffset >= 0);
    assert(spDelta <= 0);
    assert((spDelta % 16) == 0); // SP changes must be 16-byte aligned

    bool needToSaveRegs = true;
    if (spDelta != 0)
    {
        if ((spOffset == 0) && (spDelta >= -256))
        {
            // We can use pre-index addressing.
            // str REG, [SP, #spDelta]!
            GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, reg1, REG_SPBASE, spDelta, INS_OPTS_PRE_INDEX);
            compiler->unwindSaveRegPreindexed(reg1, spDelta);

            needToSaveRegs = false;
        }
        else // (spOffset != 0) || (spDelta < -256)
        {
            // generate sub SP,SP,imm
            genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
        }
    }

    if (needToSaveRegs)
    {
        // str REG, [SP, #offset]
        // 64-bit STR offset range: 0 to 32760, multiple of 8.
        GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);
    }
}

//------------------------------------------------------------------------
// genEpilogRestoreRegPair: This is the opposite of genPrologSaveRegPair(), run in the epilog instead of the prolog.
// The stack pointer adjustment, if requested, is done after the register restore, using post-index addressing.
// The caller must ensure that we can use the LDP instruction, and that spOffset will be in the legal range for that
// instruction.
//
// Arguments:
//    reg1                     - First register of pair to restore.
//    reg2                     - Second register of pair to restore.
//    spOffset                 - The offset from SP to load reg1 (must be positive or zero).
//    spDelta                  - If non-zero, the amount to add to SP after the register restores (must be positive or
//                               zero).
//    useSaveNextPair          - True if the last prolog instruction was to save the previous register pair. This
//                               allows us to emit the "save_next" unwind code.
//    tmpReg                   - An available temporary register. Needed for the case of large frames.
//    pTmpRegIsZero            - If we use tmpReg, and pTmpRegIsZero is non-null, we set *pTmpRegIsZero to 'false'.
//                               Otherwise, we don't touch it.
//
// Return Value:
//    None.

void CodeGen::genEpilogRestoreRegPair(regNumber reg1,
                                      regNumber reg2,
                                      int       spOffset,
                                      int       spDelta,
                                      bool      useSaveNextPair,
                                      regNumber tmpReg,
                                      bool*     pTmpRegIsZero)
{
    assert(spOffset >= 0);
    assert(spDelta >= 0);
    assert((spDelta % 16) == 0);                                  // SP changes must be 16-byte aligned
    assert(genIsValidFloatReg(reg1) == genIsValidFloatReg(reg2)); // registers must be both general-purpose, or both
                                                                  // FP/SIMD

    if (spDelta != 0)
    {
        assert(!useSaveNextPair);
        if ((spOffset == 0) && (spDelta <= 504))
        {
            // Fold the SP change into this instruction.
            // ldp reg1, reg2, [SP], #spDelta
            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spDelta, INS_OPTS_POST_INDEX);
            compiler->unwindSaveRegPairPreindexed(reg1, reg2, -spDelta);
        }
        else // (spOffset != 0) || (spDelta > 504)
        {
            // Can't fold in the SP change; need to use a separate ADD instruction.

            // ldp reg1, reg2, [SP, #offset]
            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spOffset);
            compiler->unwindSaveRegPair(reg1, reg2, spOffset);

            // generate add SP,SP,imm
            genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
        }
    }
    else
    {
        GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spOffset);

        if (TargetOS::IsUnix && compiler->generateCFIUnwindCodes())
        {
            useSaveNextPair = false;
        }

        if (useSaveNextPair)
        {
            compiler->unwindSaveNext();
        }
        else
        {
            compiler->unwindSaveRegPair(reg1, reg2, spOffset);
        }
    }
}

//------------------------------------------------------------------------
// genEpilogRestoreReg: The opposite of genPrologSaveReg(), run in the epilog instead of the prolog.
//
// Arguments:
//    reg1                     - Register to restore.
//    spOffset                 - The offset from SP to restore reg1 (must be positive or zero).
//    spDelta                  - If non-zero, the amount to add to SP after the register restores (must be positive or
//                               zero).
//    tmpReg                   - An available temporary register. Needed for the case of large frames.
//    pTmpRegIsZero            - If we use tmpReg, and pTmpRegIsZero is non-null, we set *pTmpRegIsZero to 'false'.
//                               Otherwise, we don't touch it.
//
// Return Value:
//    None.

void CodeGen::genEpilogRestoreReg(regNumber reg1, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero)
{
    assert(spOffset >= 0);
    assert(spDelta >= 0);
    assert((spDelta % 16) == 0); // SP changes must be 16-byte aligned

    if (spDelta != 0)
    {
        if ((spOffset == 0) && (spDelta <= 255))
        {
            // We can use post-index addressing.
            // ldr REG, [SP], #spDelta
            GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, reg1, REG_SPBASE, spDelta, INS_OPTS_POST_INDEX);
            compiler->unwindSaveRegPreindexed(reg1, -spDelta);
        }
        else // (spOffset != 0) || (spDelta > 255)
        {
            // ldr reg1, [SP, #offset]
            GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
            compiler->unwindSaveReg(reg1, spOffset);

            // generate add SP,SP,imm
            genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
        }
    }
    else
    {
        // ldr reg1, [SP, #offset]
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);
    }
}

//------------------------------------------------------------------------
// genBuildRegPairsStack: Build a stack of register pairs for prolog/epilog save/restore for the given mask.
// The first register pair will contain the lowest register. Register pairs will combine neighbor
// registers in pairs. If it can't be done (for example if we have a hole or this is the last reg in a mask with
// odd number of regs) then the second element of that RegPair will be REG_NA.
//
// Arguments:
//   regsMask - a mask of registers for prolog/epilog generation;
//   regStack - a regStack instance to build the stack in, used to save temp copyings.
//
// Return value:
//   no return value; the regStack argument is modified.
//
// static
void CodeGen::genBuildRegPairsStack(regMaskTP regsMask, ArrayStack<RegPair>* regStack)
{
    assert(regStack != nullptr);
    assert(regStack->Height() == 0);

    unsigned regsCount = genCountBits(regsMask);

    while (regsMask != RBM_NONE)
    {
        regNumber reg1 = genFirstRegNumFromMaskAndToggle(regsMask);
        regsCount -= 1;

        bool isPairSave = false;
        if (regsCount > 0)
        {
            regNumber reg2 = genFirstRegNumFromMask(regsMask);
            if (reg2 == REG_NEXT(reg1))
            {
                // The JIT doesn't allow saving pair (R28,FP), even though the
                // save_regp register pair unwind code specification allows it.
                // The JIT always saves (FP,LR) as a pair, and uses the save_fplr
                // unwind code. This only comes up in stress mode scenarios
                // where callee-saved registers are not allocated completely
                // from lowest-to-highest, without gaps.
                if (reg1 != REG_R28)
                {
                    // Both registers must have the same type to be saved as pair.
                    if (genIsValidFloatReg(reg1) == genIsValidFloatReg(reg2))
                    {
                        isPairSave = true;

                        regsMask ^= genRegMask(reg2);
                        regsCount -= 1;

                        regStack->Push(RegPair(reg1, reg2));
                    }
                }
            }
        }
        if (!isPairSave)
        {
            regStack->Push(RegPair(reg1));
        }
    }
    assert(regsCount == 0 && regsMask == RBM_NONE);

    genSetUseSaveNextPairs(regStack);
}

//------------------------------------------------------------------------
// genSetUseSaveNextPairs: Set useSaveNextPair for each RegPair on the stack which unwind info can be encoded as
// save_next code.
//
// Arguments:
//   regStack - a regStack instance to set useSaveNextPair.
//
// Notes:
// We can use save_next for RegPair(N, N+1) only when we have sequence like (N-2, N-1), (N, N+1).
// In this case in the prolog save_next for (N, N+1) refers to save_pair(N-2, N-1);
// in the epilog the unwinder will search for the first save_pair (N-2, N-1)
// and then go back to the first save_next (N, N+1) to restore it first.
//
// static
void CodeGen::genSetUseSaveNextPairs(ArrayStack<RegPair>* regStack)
{
    for (int i = 1; i < regStack->Height(); ++i)
    {
        RegPair& curr = regStack->BottomRef(i);
        RegPair  prev = regStack->Bottom(i - 1);

        if (prev.reg2 == REG_NA || curr.reg2 == REG_NA)
        {
            continue;
        }

        if (REG_NEXT(prev.reg2) != curr.reg1)
        {
            continue;
        }

        if (genIsValidFloatReg(prev.reg2) != genIsValidFloatReg(curr.reg1))
        {
            // It is possible to support changing of the last int pair with the first float pair,
            // but it is very rare case and it would require superfluous changes in the unwinder.
            continue;
        }
        curr.useSaveNextPair = true;
    }
}

//------------------------------------------------------------------------
// genGetSlotSizeForRegsInMask: Get the stack slot size appropriate for the register type from the mask.
//
// Arguments:
//   regsMask - a mask of registers for prolog/epilog generation.
//
// Return value:
//   stack slot size in bytes.
//
// Note: Because int and float register type sizes match we can call this function with a mask that includes both.
//
// static
int CodeGen::genGetSlotSizeForRegsInMask(regMaskTP regsMask)
{
    assert((regsMask & (RBM_CALLEE_SAVED | RBM_FP | RBM_LR)) == regsMask); // Do not expect anything else.

    static_assert_no_msg(REGSIZE_BYTES == FPSAVE_REGSIZE_BYTES);
    return REGSIZE_BYTES;
}

//------------------------------------------------------------------------
// genSaveCalleeSavedRegisterGroup: Saves the group of registers described by the mask.
//
// Arguments:
//   regsMask             - a mask of registers for prolog generation;
//   spDelta              - if non-zero, the amount to add to SP before the first register save (or together with it);
//   spOffset             - the offset from SP that is the beginning of the callee-saved register area;
//
void CodeGen::genSaveCalleeSavedRegisterGroup(regMaskTP regsMask, int spDelta, int spOffset)
{
    const int slotSize = genGetSlotSizeForRegsInMask(regsMask);

    ArrayStack<RegPair> regStack(compiler->getAllocator(CMK_Codegen));
    genBuildRegPairsStack(regsMask, &regStack);

    for (int i = 0; i < regStack.Height(); ++i)
    {
        RegPair regPair = regStack.Bottom(i);
        if (regPair.reg2 != REG_NA)
        {
            // We can use a STP instruction.
            genPrologSaveRegPair(regPair.reg1, regPair.reg2, spOffset, spDelta, regPair.useSaveNextPair, REG_IP0,
                                 nullptr);

            spOffset += 2 * slotSize;
        }
        else
        {
            // No register pair; we use a STR instruction.
            genPrologSaveReg(regPair.reg1, spOffset, spDelta, REG_IP0, nullptr);
            spOffset += slotSize;
        }

        spDelta = 0; // We've now changed SP already, if necessary; don't do it again.
    }
}

//------------------------------------------------------------------------
// genSaveCalleeSavedRegistersHelp: Save the callee-saved registers in 'regsToSaveMask' to the stack frame
// in the function or funclet prolog. Registers are saved in register number order from low addresses
// to high addresses. This means that integer registers are saved at lower addresses than floatint-point/SIMD
// registers. However, when genSaveFpLrWithAllCalleeSavedRegisters is true, the integer registers are stored
// at higher addresses than floating-point/SIMD registers, that is, the relative order of these two classes
// is reversed. This is done to put the saved frame pointer very high in the frame, for simplicity.
//
// TODO: We could always put integer registers at the higher addresses, if desired, to remove this special
// case. It would cause many asm diffs when first implemented.
//
// If establishing frame pointer chaining, it must be done after saving the callee-saved registers.
//
// We can only use the instructions that are allowed by the unwind codes. The caller ensures that
// there is enough space on the frame to store these registers, and that the store instructions
// we need to use (STR or STP) are encodable with the stack-pointer immediate offsets we need to use.
//
// The caller can tell us to fold in a stack pointer adjustment, which we will do with the first instruction.
// Note that the stack pointer adjustment must be by a multiple of 16 to preserve the invariant that the
// stack pointer is always 16 byte aligned. If we are saving an odd number of callee-saved
// registers, though, we will have an empty alignment slot somewhere. It turns out we will put
// it below (at a lower address) the callee-saved registers, as that is currently how we
// do frame layout. This means that the first stack offset will be 8 and the stack pointer
// adjustment must be done by a SUB, and not folded in to a pre-indexed store.
//
// Arguments:
//    regsToSaveMask          - The mask of callee-saved registers to save. If empty, this function does nothing.
//    lowestCalleeSavedOffset - The offset from SP that is the beginning of the callee-saved register area. Note that
//                              if non-zero spDelta, then this is the offset of the first save *after* that
//                              SP adjustment.
//    spDelta                 - If non-zero, the amount to add to SP before the register saves (must be negative or
//                              zero).
//
// Notes:
//    The save set can contain LR in which case LR is saved along with the other callee-saved registers.
//    But currently Jit doesn't use frames without frame pointer on arm64.
//
void CodeGen::genSaveCalleeSavedRegistersHelp(regMaskTP regsToSaveMask, int lowestCalleeSavedOffset, int spDelta)
{
    assert(spDelta <= 0);
    assert(-spDelta <= STACK_PROBE_BOUNDARY_THRESHOLD_BYTES);

    unsigned regsToSaveCount = genCountBits(regsToSaveMask);
    if (regsToSaveCount == 0)
    {
        if (spDelta != 0)
        {
            // Currently this is the case for varargs only
            // whose size is MAX_REG_ARG * REGSIZE_BYTES = 64 bytes.
            genStackPointerAdjustment(spDelta, REG_NA, nullptr, /* reportUnwindData */ true);
        }
        return;
    }

    assert((spDelta % 16) == 0);

    // We also can save FP and LR, even though they are not in RBM_CALLEE_SAVED.
    assert(regsToSaveCount <= genCountBits(RBM_CALLEE_SAVED | RBM_FP | RBM_LR));

    // Save integer registers at higher addresses than floating-point registers.

    regMaskTP maskSaveRegsFloat = regsToSaveMask & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = regsToSaveMask & ~maskSaveRegsFloat;

    if (maskSaveRegsFloat != RBM_NONE)
    {
        genSaveCalleeSavedRegisterGroup(maskSaveRegsFloat, spDelta, lowestCalleeSavedOffset);
        spDelta = 0;
        lowestCalleeSavedOffset += genCountBits(maskSaveRegsFloat) * FPSAVE_REGSIZE_BYTES;
    }

    if (maskSaveRegsInt != RBM_NONE)
    {
        genSaveCalleeSavedRegisterGroup(maskSaveRegsInt, spDelta, lowestCalleeSavedOffset);
        // No need to update spDelta, lowestCalleeSavedOffset since they're not used after this.
    }
}

//------------------------------------------------------------------------
// genRestoreCalleeSavedRegisterGroup: Restores the group of registers described by the mask.
//
// Arguments:
//   regsMask             - a mask of registers for epilog generation;
//   spDelta              - if non-zero, the amount to add to SP after the last register restore (or together with it);
//   spOffset             - the offset from SP that is the beginning of the callee-saved register area;
//
void CodeGen::genRestoreCalleeSavedRegisterGroup(regMaskTP regsMask, int spDelta, int spOffset)
{
    const int slotSize = genGetSlotSizeForRegsInMask(regsMask);

    ArrayStack<RegPair> regStack(compiler->getAllocator(CMK_Codegen));
    genBuildRegPairsStack(regsMask, &regStack);

    int stackDelta = 0;
    for (int i = 0; i < regStack.Height(); ++i)
    {
        bool lastRestoreInTheGroup = (i == regStack.Height() - 1);
        bool updateStackDelta      = lastRestoreInTheGroup && (spDelta != 0);
        if (updateStackDelta)
        {
            // Update stack delta only if it is the last restore (the first save).
            assert(stackDelta == 0);
            stackDelta = spDelta;
        }

        RegPair regPair = regStack.Top(i);
        if (regPair.reg2 != REG_NA)
        {
            spOffset -= 2 * slotSize;

            genEpilogRestoreRegPair(regPair.reg1, regPair.reg2, spOffset, stackDelta, regPair.useSaveNextPair, REG_IP1,
                                    nullptr);
        }
        else
        {
            spOffset -= slotSize;
            genEpilogRestoreReg(regPair.reg1, spOffset, stackDelta, REG_IP1, nullptr);
        }
    }
}

//------------------------------------------------------------------------
// genRestoreCalleeSavedRegistersHelp: Restore the callee-saved registers in 'regsToRestoreMask' from the stack frame
// in the function or funclet epilog. This exactly reverses the actions of genSaveCalleeSavedRegistersHelp().
//
// Arguments:
//    regsToRestoreMask       - The mask of callee-saved registers to restore. If empty, this function does nothing.
//    lowestCalleeSavedOffset - The offset from SP that is the beginning of the callee-saved register area.
//    spDelta                 - If non-zero, the amount to add to SP after the register restores (must be positive or
//                              zero).
//
// Here's an example restore sequence:
//      ldp     x27, x28, [sp,#96]
//      ldp     x25, x26, [sp,#80]
//      ldp     x23, x24, [sp,#64]
//      ldp     x21, x22, [sp,#48]
//      ldp     x19, x20, [sp,#32]
//
// For the case of non-zero spDelta, we assume the base of the callee-save registers to restore is at SP, and
// the last restore adjusts SP by the specified amount. For example:
//      ldp     x27, x28, [sp,#64]
//      ldp     x25, x26, [sp,#48]
//      ldp     x23, x24, [sp,#32]
//      ldp     x21, x22, [sp,#16]
//      ldp     x19, x20, [sp], #80
//
// Note you call the unwind functions specifying the prolog operation that is being un-done. So, for example, when
// generating a post-indexed load, you call the unwind function for specifying the corresponding preindexed store.
//
// Return Value:
//    None.

void CodeGen::genRestoreCalleeSavedRegistersHelp(regMaskTP regsToRestoreMask, int lowestCalleeSavedOffset, int spDelta)
{
    assert(spDelta >= 0);
    unsigned regsToRestoreCount = genCountBits(regsToRestoreMask);
    if (regsToRestoreCount == 0)
    {
        if (spDelta != 0)
        {
            // Currently this is the case for varargs only
            // whose size is MAX_REG_ARG * REGSIZE_BYTES = 64 bytes.
            genStackPointerAdjustment(spDelta, REG_NA, nullptr, /* reportUnwindData */ true);
        }
        return;
    }

    assert((spDelta % 16) == 0);

    // We also can restore FP and LR, even though they are not in RBM_CALLEE_SAVED.
    assert(regsToRestoreCount <= genCountBits(RBM_CALLEE_SAVED | RBM_FP | RBM_LR));

    // Point past the end, to start. We predecrement to find the offset to load from.
    static_assert_no_msg(REGSIZE_BYTES == FPSAVE_REGSIZE_BYTES);
    int spOffset = lowestCalleeSavedOffset + regsToRestoreCount * REGSIZE_BYTES;

    // Save integer registers at higher addresses than floating-point registers.

    regMaskTP maskRestoreRegsFloat = regsToRestoreMask & RBM_ALLFLOAT;
    regMaskTP maskRestoreRegsInt   = regsToRestoreMask & ~maskRestoreRegsFloat;

    // Restore in the opposite order of saving.

    if (maskRestoreRegsInt != RBM_NONE)
    {
        int spIntDelta = (maskRestoreRegsFloat != RBM_NONE) ? 0 : spDelta; // should we delay the SP adjustment?
        genRestoreCalleeSavedRegisterGroup(maskRestoreRegsInt, spIntDelta, spOffset);
        spOffset -= genCountBits(maskRestoreRegsInt) * REGSIZE_BYTES;
    }

    if (maskRestoreRegsFloat != RBM_NONE)
    {
        // If there is any spDelta, it must be used here.
        genRestoreCalleeSavedRegisterGroup(maskRestoreRegsFloat, spDelta, spOffset);
        // No need to update spOffset since it's not used after this.
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
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletProlog()\n");
#endif

    assert(block != NULL);
    assert(block->HasFlag(BBF_FUNCLET_BEG));

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    regMaskTP maskSaveRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = genFuncletInfo.fiSaveRegs & ~maskSaveRegsFloat;

    // Funclets must always save LR and FP, since when we have funclets we must have an FP frame.
    assert((maskSaveRegsInt & RBM_LR) != 0);
    assert((maskSaveRegsInt & RBM_FP) != 0);

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

    if (genFuncletInfo.fiFrameType == 1)
    {
        if (compiler->opts.IsOSR())
        {
            // With OSR we may see large values for fiSpDelta1.
            // We repurpose genAllocLclFram to do the necessary probing.
            bool scratchRegIsZero = false;
            genAllocLclFrame(-genFuncletInfo.fiSpDelta1, REG_SCRATCH, &scratchRegIsZero, maskArgRegsLiveIn);
            genStackPointerAdjustment(genFuncletInfo.fiSpDelta1, REG_SCRATCH, nullptr, /* reportUnwindData */ true);
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, 0);
            compiler->unwindSaveRegPair(REG_FP, REG_LR, 0);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, genFuncletInfo.fiSpDelta1,
                                          INS_OPTS_PRE_INDEX);
            compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);
        }

        maskSaveRegsInt &= ~(RBM_LR | RBM_FP); // We've saved these now

        assert(genFuncletInfo.fiSpDelta2 == 0);
        assert(genFuncletInfo.fiSP_to_FPLR_save_delta == 0);
    }
    else if (genFuncletInfo.fiFrameType == 2)
    {
        // fiFrameType==2 constraints:
        assert(genFuncletInfo.fiSpDelta1 < 0);
        assert(genFuncletInfo.fiSpDelta1 >= -512);

        // generate sub SP,SP,imm
        genStackPointerAdjustment(genFuncletInfo.fiSpDelta1, REG_NA, nullptr, /* reportUnwindData */ true);

        assert(genFuncletInfo.fiSpDelta2 == 0);

        GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                      genFuncletInfo.fiSP_to_FPLR_save_delta);
        compiler->unwindSaveRegPair(REG_FP, REG_LR, genFuncletInfo.fiSP_to_FPLR_save_delta);

        maskSaveRegsInt &= ~(RBM_LR | RBM_FP); // We've saved these now
    }
    else if (genFuncletInfo.fiFrameType == 3)
    {
        if (compiler->opts.IsOSR())
        {
            // With OSR we may see large values for fiSpDelta1
            // We repurpose genAllocLclFram to do the necessary probing.
            bool scratchRegIsZero = false;
            genAllocLclFrame(-genFuncletInfo.fiSpDelta1, REG_SCRATCH, &scratchRegIsZero, maskArgRegsLiveIn);
            genStackPointerAdjustment(genFuncletInfo.fiSpDelta1, REG_SCRATCH, nullptr, /* reportUnwindData */ true);
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, 0);
            compiler->unwindSaveRegPair(REG_FP, REG_LR, 0);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, genFuncletInfo.fiSpDelta1,
                                          INS_OPTS_PRE_INDEX);
            compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);
        }

        maskSaveRegsInt &= ~(RBM_LR | RBM_FP); // We've saved these now
    }
    else if (genFuncletInfo.fiFrameType == 4)
    {
        // fiFrameType==4 constraints:
        assert(genFuncletInfo.fiSpDelta1 < 0);
        assert(genFuncletInfo.fiSpDelta1 >= -512);

        // generate sub SP,SP,imm
        genStackPointerAdjustment(genFuncletInfo.fiSpDelta1, REG_NA, nullptr, /* reportUnwindData */ true);

        assert(genFuncletInfo.fiSpDelta2 == 0);
    }
    else
    {
        assert(genFuncletInfo.fiFrameType == 5);

        if (compiler->opts.IsOSR())
        {
            // With OSR we may see large values for fiSpDelta1.
            // We repurpose genAllocLclFram to do the necessary probing.
            bool scratchRegIsZero = false;
            genAllocLclFrame(-genFuncletInfo.fiSpDelta1, REG_SCRATCH, &scratchRegIsZero, maskArgRegsLiveIn);
            genStackPointerAdjustment(genFuncletInfo.fiSpDelta1, REG_SCRATCH, nullptr, /* reportUnwindData */ true);
        }
        else
        {
            assert(genFuncletInfo.fiSpDelta1 < 0);
            assert(genFuncletInfo.fiSpDelta1 >= -240);
            genStackPointerAdjustment(genFuncletInfo.fiSpDelta1, REG_NA, nullptr, /* reportUnwindData */ true);
        }
    }

    int lowestCalleeSavedOffset = genFuncletInfo.fiSP_to_CalleeSave_delta +
                                  genFuncletInfo.fiSpDelta2; // We haven't done the second adjustment of SP yet (if any)
    genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, lowestCalleeSavedOffset, 0);

    if ((genFuncletInfo.fiFrameType == 3) || (genFuncletInfo.fiFrameType == 5))
    {
        // Note that genFuncletInfo.fiSpDelta2 is always a non-positive value
        assert(genFuncletInfo.fiSpDelta2 <= 0);

        // generate sub SP,SP,imm
        if (genFuncletInfo.fiSpDelta2 < 0)
        {
            genStackPointerAdjustment(genFuncletInfo.fiSpDelta2, REG_R2, nullptr, /* reportUnwindData */ true);
        }
        else
        {
            // we will only see fiSpDelta2 == 0 for osr funclets
            assert(compiler->opts.IsOSR());
        }
    }

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // If there is no PSPSym (NativeAOT ABI), we are done. Otherwise, we need to set up the PSPSym in the funclet frame.
    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        if (isFilter)
        {
            // This is the first block of a filter
            // Note that register x1 = CallerSP of the containing function
            // X1 is overwritten by the first Load (new callerSP)
            // X2 is scratch when we have a large constant offset

            // Load the CallerSP of the main function (stored in the PSP of the dynamically containing funclet or
            // function)
            genInstrWithConstant(INS_ldr, EA_PTRSIZE, REG_R1, REG_R1, genFuncletInfo.fiCallerSP_to_PSP_slot_delta,
                                 REG_R2, false);
            regSet.verifyRegUsed(REG_R1);

            // Store the PSP value (aka CallerSP)
            genInstrWithConstant(INS_str, EA_PTRSIZE, REG_R1, REG_SPBASE, genFuncletInfo.fiSP_to_PSP_slot_delta, REG_R2,
                                 false);

            // re-establish the frame pointer
            genInstrWithConstant(INS_add, EA_PTRSIZE, REG_FPBASE, REG_R1,
                                 genFuncletInfo.fiFunction_CallerSP_to_FP_delta, REG_R2, false);
        }
        else // This is a non-filter funclet
        {
            // X3 is scratch, X2 can also become scratch

            // compute the CallerSP, given the frame pointer. x3 is scratch.
            genInstrWithConstant(INS_add, EA_PTRSIZE, REG_R3, REG_FPBASE,
                                 -genFuncletInfo.fiFunction_CallerSP_to_FP_delta, REG_R2, false);
            regSet.verifyRegUsed(REG_R3);

            genInstrWithConstant(INS_str, EA_PTRSIZE, REG_R3, REG_SPBASE, genFuncletInfo.fiSP_to_PSP_slot_delta, REG_R2,
                                 false);
        }
    }
}

/*****************************************************************************
 *
 *  Generates code for an EH funclet epilog.
 *
 *  See the description of frame shapes at genFuncletProlog().
 */

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletEpilog()\n");
#endif

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    bool unwindStarted = false;

    if (!unwindStarted)
    {
        // We can delay this until we know we'll generate an unwindable instruction, if necessary.
        compiler->unwindBegEpilog();
        unwindStarted = true;
    }

    regMaskTP maskRestoreRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskRestoreRegsInt   = genFuncletInfo.fiSaveRegs & ~maskRestoreRegsFloat;

    // Funclets must always save LR and FP, since when we have funclets we must have an FP frame.
    assert((maskRestoreRegsInt & RBM_LR) != 0);
    assert((maskRestoreRegsInt & RBM_FP) != 0);

    if ((genFuncletInfo.fiFrameType == 3) || (genFuncletInfo.fiFrameType == 5))
    {
        // Note that genFuncletInfo.fiSpDelta2 is always a non-positive value
        assert(genFuncletInfo.fiSpDelta2 <= 0);

        // generate add SP,SP,imm
        if (genFuncletInfo.fiSpDelta2 < 0)
        {
            genStackPointerAdjustment(-genFuncletInfo.fiSpDelta2, REG_R2, nullptr, /* reportUnwindData */ true);
        }
        else
        {
            // we should only zee zero SpDelta2 with osr.
            assert(compiler->opts.IsOSR());
        }
    }

    regMaskTP regsToRestoreMask = maskRestoreRegsInt | maskRestoreRegsFloat;
    if ((genFuncletInfo.fiFrameType == 1) || (genFuncletInfo.fiFrameType == 2) || (genFuncletInfo.fiFrameType == 3))
    {
        regsToRestoreMask &= ~(RBM_LR | RBM_FP); // We restore FP/LR at the end
    }
    int lowestCalleeSavedOffset = genFuncletInfo.fiSP_to_CalleeSave_delta + genFuncletInfo.fiSpDelta2;
    genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, lowestCalleeSavedOffset, 0);

    if (genFuncletInfo.fiFrameType == 1)
    {
        // With OSR we may see large values for fiSpDelta1
        //
        if (compiler->opts.IsOSR())
        {
            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, 0);
            compiler->unwindSaveRegPair(REG_FP, REG_LR, 0);

            genStackPointerAdjustment(-genFuncletInfo.fiSpDelta1, REG_SCRATCH, nullptr, /* reportUnwindData */ true);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, -genFuncletInfo.fiSpDelta1,
                                          INS_OPTS_POST_INDEX);
            compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);
        }

        assert(genFuncletInfo.fiSpDelta2 == 0);
        assert(genFuncletInfo.fiSP_to_FPLR_save_delta == 0);
    }
    else if (genFuncletInfo.fiFrameType == 2)
    {
        GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                      genFuncletInfo.fiSP_to_FPLR_save_delta);
        compiler->unwindSaveRegPair(REG_FP, REG_LR, genFuncletInfo.fiSP_to_FPLR_save_delta);

        // fiFrameType==2 constraints:
        assert(genFuncletInfo.fiSpDelta1 < 0);
        assert(genFuncletInfo.fiSpDelta1 >= -512);

        // generate add SP,SP,imm
        genStackPointerAdjustment(-genFuncletInfo.fiSpDelta1, REG_NA, nullptr, /* reportUnwindData */ true);

        assert(genFuncletInfo.fiSpDelta2 == 0);
    }
    else if (genFuncletInfo.fiFrameType == 3)
    {
        // With OSR we may see large values for fiSpDelta1
        //
        if (compiler->opts.IsOSR())
        {
            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, 0);
            compiler->unwindSaveRegPair(REG_FP, REG_LR, 0);

            genStackPointerAdjustment(-genFuncletInfo.fiSpDelta1, REG_SCRATCH, nullptr, /* reportUnwindData */ true);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, -genFuncletInfo.fiSpDelta1,
                                          INS_OPTS_POST_INDEX);
            compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);
        }
    }
    else if (genFuncletInfo.fiFrameType == 4)
    {
        // fiFrameType==4 constraints:
        assert(genFuncletInfo.fiSpDelta1 < 0);
        assert(genFuncletInfo.fiSpDelta1 >= -512);

        // generate add SP,SP,imm
        genStackPointerAdjustment(-genFuncletInfo.fiSpDelta1, REG_NA, nullptr, /* reportUnwindData */ true);

        assert(genFuncletInfo.fiSpDelta2 == 0);
    }
    else
    {
        assert(genFuncletInfo.fiFrameType == 5);
        // Same work as fiFrameType==4, but different asserts.

        assert(genFuncletInfo.fiSpDelta1 < 0);

        // With OSR we may see large values for fiSpDelta1 as the funclet
        // frame currently must pad with the Tier0 frame size.
        //
        if (compiler->opts.IsOSR())
        {
            genStackPointerAdjustment(-genFuncletInfo.fiSpDelta1, REG_SCRATCH, nullptr, /* reportUnwindData */ true);
        }
        else
        {
            // generate add SP,SP,imm
            assert(genFuncletInfo.fiSpDelta1 >= -240);
            genStackPointerAdjustment(-genFuncletInfo.fiSpDelta1, REG_NA, nullptr, /* reportUnwindData */ true);
        }
    }

    inst_RV(INS_ret, REG_LR, TYP_I_IMPL);
    compiler->unwindReturn(REG_LR);

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
    if (!compiler->ehAnyFunclets())
        return;

    assert(isFramePointerUsed());

    // The frame size and offsets must be finalized
    assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT);

    unsigned const PSPSize = (compiler->lvaPSPSym != BAD_VAR_NUM) ? REGSIZE_BYTES : 0;

    // Because a method and funclets must have the same caller-relative PSPSym offset,
    // if there is a PSPSym, we have to pad the funclet frame size for OSR.
    //
    unsigned osrPad = 0;
    if (compiler->opts.IsOSR() && (PSPSize > 0))
    {
        osrPad = compiler->info.compPatchpointInfo->TotalFrameSize();

        // OSR pad must be already aligned to stack size.
        assert((osrPad % STACK_ALIGN) == 0);
    }

    genFuncletInfo.fiFunction_CallerSP_to_FP_delta = genCallerSPtoFPdelta() - osrPad;

    regMaskTP rsMaskSaveRegs = regSet.rsMaskCalleeSaved;
    assert((rsMaskSaveRegs & RBM_LR) != 0);
    assert((rsMaskSaveRegs & RBM_FP) != 0);

    unsigned saveRegsCount       = genCountBits(rsMaskSaveRegs);
    unsigned saveRegsPlusPSPSize = saveRegsCount * REGSIZE_BYTES + PSPSize;
    if (compiler->info.compIsVarArgs)
    {
        // For varargs we always save all of the integer register arguments
        // so that they are contiguous with the incoming stack arguments.
        saveRegsPlusPSPSize += MAX_REG_ARG * REGSIZE_BYTES;
    }

    if (compiler->lvaMonAcquired != BAD_VAR_NUM && !compiler->opts.IsOSR())
    {
        // We furthermore allocate the "monitor acquired" bool between PSP and
        // the saved registers because this is part of the EnC header.
        // Note that OSR methods reuse the monitor bool created by tier 0.
        saveRegsPlusPSPSize += compiler->lvaLclSize(compiler->lvaMonAcquired);
    }

    unsigned const saveRegsPlusPSPSizeAligned = roundUp(saveRegsPlusPSPSize, STACK_ALIGN);

    assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
    unsigned const outgoingArgSpaceAligned = roundUp(compiler->lvaOutgoingArgSpaceSize, STACK_ALIGN);

    // If do two SP adjustments, each one must be aligned. This represents the largest possible stack size, if two
    // separate alignment slots are required.
    unsigned const twoSpAdjustmentFuncletFrameSizeAligned =
        osrPad + saveRegsPlusPSPSizeAligned + outgoingArgSpaceAligned;
    assert((twoSpAdjustmentFuncletFrameSizeAligned % STACK_ALIGN) == 0);

    int SP_to_FPLR_save_delta;
    int SP_to_PSP_slot_delta;
    int CallerSP_to_PSP_slot_delta;

    // Are we stressing frame type 5? Don't do it unless we have non-zero outgoing arg space.
    const bool useFrameType5 =
        genSaveFpLrWithAllCalleeSavedRegisters && genForceFuncletFrameType5 && (compiler->lvaOutgoingArgSpaceSize > 0);

    if ((twoSpAdjustmentFuncletFrameSizeAligned <= 512) && !useFrameType5)
    {
        unsigned const oneSpAdjustmentFuncletFrameSize =
            osrPad + saveRegsPlusPSPSize + compiler->lvaOutgoingArgSpaceSize;
        unsigned const oneSpAdjustmentFuncletFrameSizeAligned = roundUp(oneSpAdjustmentFuncletFrameSize, STACK_ALIGN);
        assert(oneSpAdjustmentFuncletFrameSizeAligned <= twoSpAdjustmentFuncletFrameSizeAligned);

        unsigned const oneSpAdjustmentFuncletFrameSizeAlignmentPad =
            oneSpAdjustmentFuncletFrameSizeAligned - oneSpAdjustmentFuncletFrameSize;
        assert((oneSpAdjustmentFuncletFrameSizeAlignmentPad == 0) ||
               (oneSpAdjustmentFuncletFrameSizeAlignmentPad == REGSIZE_BYTES));

        if (genSaveFpLrWithAllCalleeSavedRegisters)
        {
            SP_to_FPLR_save_delta = oneSpAdjustmentFuncletFrameSizeAligned - (2 /* FP, LR */ * REGSIZE_BYTES);
            if (compiler->info.compIsVarArgs)
            {
                SP_to_FPLR_save_delta -= MAX_REG_ARG * REGSIZE_BYTES;
            }

            SP_to_PSP_slot_delta = compiler->lvaOutgoingArgSpaceSize + oneSpAdjustmentFuncletFrameSizeAlignmentPad;
            CallerSP_to_PSP_slot_delta = -(int)(osrPad + saveRegsPlusPSPSize);

            genFuncletInfo.fiFrameType = 4;
        }
        else
        {
            SP_to_FPLR_save_delta = compiler->lvaOutgoingArgSpaceSize;
            SP_to_PSP_slot_delta =
                SP_to_FPLR_save_delta + 2 /* FP, LR */ * REGSIZE_BYTES + oneSpAdjustmentFuncletFrameSizeAlignmentPad;
            CallerSP_to_PSP_slot_delta = -(int)(osrPad + saveRegsPlusPSPSize - 2 /* FP, LR */ * REGSIZE_BYTES);

            if (compiler->lvaOutgoingArgSpaceSize == 0)
            {
                genFuncletInfo.fiFrameType = 1;
            }
            else
            {
                genFuncletInfo.fiFrameType = 2;
            }
        }

        genFuncletInfo.fiSpDelta1 = -(int)oneSpAdjustmentFuncletFrameSizeAligned;
        genFuncletInfo.fiSpDelta2 = 0;

        assert(genFuncletInfo.fiSpDelta1 + genFuncletInfo.fiSpDelta2 == -(int)oneSpAdjustmentFuncletFrameSizeAligned);
    }
    else
    {
        unsigned const saveRegsPlusPSPAlignmentPad = saveRegsPlusPSPSizeAligned - saveRegsPlusPSPSize;
        assert((saveRegsPlusPSPAlignmentPad == 0) || (saveRegsPlusPSPAlignmentPad == REGSIZE_BYTES));

        if (genSaveFpLrWithAllCalleeSavedRegisters)
        {
            SP_to_FPLR_save_delta = twoSpAdjustmentFuncletFrameSizeAligned - (2 /* FP, LR */ * REGSIZE_BYTES);
            if (compiler->info.compIsVarArgs)
            {
                SP_to_FPLR_save_delta -= MAX_REG_ARG * REGSIZE_BYTES;
            }

            SP_to_PSP_slot_delta       = outgoingArgSpaceAligned + saveRegsPlusPSPAlignmentPad;
            CallerSP_to_PSP_slot_delta = -(int)(osrPad + saveRegsPlusPSPSize);

            genFuncletInfo.fiFrameType = 5;
        }
        else
        {
            SP_to_FPLR_save_delta = outgoingArgSpaceAligned;
            SP_to_PSP_slot_delta = SP_to_FPLR_save_delta + 2 /* FP, LR */ * REGSIZE_BYTES + saveRegsPlusPSPAlignmentPad;
            CallerSP_to_PSP_slot_delta = -(int)(osrPad + saveRegsPlusPSPSizeAligned - 2 /* FP, LR */ * REGSIZE_BYTES -
                                                saveRegsPlusPSPAlignmentPad);

            genFuncletInfo.fiFrameType = 3;
        }

        genFuncletInfo.fiSpDelta1 = -(int)(osrPad + saveRegsPlusPSPSizeAligned);
        genFuncletInfo.fiSpDelta2 = -(int)outgoingArgSpaceAligned;

        assert(genFuncletInfo.fiSpDelta1 + genFuncletInfo.fiSpDelta2 == -(int)twoSpAdjustmentFuncletFrameSizeAligned);
    }

    /* Now save it for future use */

    genFuncletInfo.fiSaveRegs                   = rsMaskSaveRegs;
    genFuncletInfo.fiSP_to_FPLR_save_delta      = SP_to_FPLR_save_delta;
    genFuncletInfo.fiSP_to_PSP_slot_delta       = SP_to_PSP_slot_delta;
    genFuncletInfo.fiSP_to_CalleeSave_delta     = SP_to_PSP_slot_delta + PSPSize;
    genFuncletInfo.fiCallerSP_to_PSP_slot_delta = CallerSP_to_PSP_slot_delta;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n");
        printf("Funclet prolog / epilog info\n");
        printf("                        Save regs: ");
        dspRegMask(genFuncletInfo.fiSaveRegs);
        printf("\n");
        if (compiler->opts.IsOSR())
        {
            printf("                          OSR Pad: %d\n", osrPad);
        }
        printf("  SP to FP/LR save location delta: %d\n", genFuncletInfo.fiSP_to_FPLR_save_delta);
        printf("             SP to PSP slot delta: %d\n", genFuncletInfo.fiSP_to_PSP_slot_delta);
        printf("    SP to callee-saved area delta: %d\n", genFuncletInfo.fiSP_to_CalleeSave_delta);
        printf("      Caller SP to PSP slot delta: %d\n", genFuncletInfo.fiCallerSP_to_PSP_slot_delta);
        printf("                       Frame type: %d\n", genFuncletInfo.fiFrameType);
        printf("                       SP delta 1: %d\n", genFuncletInfo.fiSpDelta1);
        printf("                       SP delta 2: %d\n", genFuncletInfo.fiSpDelta2);

        if (compiler->lvaPSPSym != BAD_VAR_NUM)
        {
            if (CallerSP_to_PSP_slot_delta !=
                compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)) // for debugging
            {
                printf("lvaGetCallerSPRelativeOffset(lvaPSPSym): %d\n",
                       compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym));
            }
        }
    }

    assert(genFuncletInfo.fiSP_to_FPLR_save_delta >= 0);
    assert(genFuncletInfo.fiSP_to_PSP_slot_delta >= 0);
    assert(genFuncletInfo.fiSP_to_CalleeSave_delta >= 0);
    assert(genFuncletInfo.fiCallerSP_to_PSP_slot_delta <= 0);

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        assert(genFuncletInfo.fiCallerSP_to_PSP_slot_delta ==
               compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main function and
                                                                             // funclet!
    }
#endif // DEBUG
}

void CodeGen::genSetPSPSym(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (compiler->lvaPSPSym == BAD_VAR_NUM)
    {
        return;
    }

    noway_assert(isFramePointerUsed()); // We need an explicit frame pointer

    int SPtoCallerSPdelta = -genCallerSPtoInitialSPdelta();

    if (compiler->opts.IsOSR())
    {
        SPtoCallerSPdelta += compiler->info.compPatchpointInfo->TotalFrameSize();
    }

    // We will just use the initReg since it is an available register
    // and we are probably done using it anyway...
    regNumber regTmp = initReg;
    *pInitRegZeroed  = false;

    GetEmitter()->emitIns_R_R_Imm(INS_add, EA_PTRSIZE, regTmp, REG_SPBASE, SPtoCallerSPdelta);
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

    int bytesToWrite = untrLclHi - untrLclLo;

    const regNumber zeroSimdReg          = REG_ZERO_INIT_FRAME_SIMD;
    bool            simdRegZeroed        = false;
    const int       simdRegPairSizeBytes = 2 * FP_REGSIZE_BYTES;

    regNumber addrReg = REG_ZERO_INIT_FRAME_REG1;

    if (addrReg == initReg)
    {
        *pInitRegZeroed = false;
    }

    int addrOffset = 0;

    // The following invariants are held below:
    //
    //   1) [addrReg, #addrOffset] points at a location where next chunk of zero bytes will be written;
    //   2) bytesToWrite specifies the number of bytes on the frame to initialize;
    //   3) if simdRegZeroed is true then 128-bit wide zeroSimdReg contains zeroes.

    const int bytesUseZeroingLoop = 192;

    if (bytesToWrite >= bytesUseZeroingLoop)
    {
        // Generates the following code:
        //
        // When the size of the region is greater than or equal to 256 bytes
        // **and** DC ZVA instruction use is permitted
        // **and** the instruction block size is configured to 64 bytes:
        //
        //    movi    v16.16b, #0
        //    add     x9, fp, #(untrLclLo+64)
        //    add     x10, fp, #(untrLclHi-64)
        //    stp     q16, q16, [x9, #-64]
        //    stp     q16, q16, [x9, #-32]
        //    bfm     x9, xzr, #0, #5
        //
        // loop:
        //    dc      zva, x9
        //    add     x9, x9, #64
        //    cmp     x9, x10
        //    blo     loop
        //
        //    stp     q16, q16, [x10]
        //    stp     q16, q16, [x10, #32]
        //
        // Otherwise:
        //
        //     movi    v16.16b, #0
        //     add     x9, fp, #(untrLclLo-32)
        //     mov     x10, #(bytesToWrite-64)
        //
        // loop:
        //     stp     q16, q16, [x9, #32]
        //     stp     q16, q16, [x9, #64]!
        //     subs    x10, x10, #64
        //     bge     loop

        const int bytesUseDataCacheZeroInstruction = 256;

        GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, zeroSimdReg, 0, INS_OPTS_16B);
        simdRegZeroed = true;

        if ((bytesToWrite >= bytesUseDataCacheZeroInstruction) &&
            compiler->compOpportunisticallyDependsOn(InstructionSet_Dczva))
        {
            // The first and the last 64 bytes should be written with two stp q-reg instructions.
            // This is in order to avoid **unintended** zeroing of the data by dc zva
            // outside of [fp+untrLclLo, fp+untrLclHi) memory region.

            genInstrWithConstant(INS_add, EA_PTRSIZE, addrReg, genFramePointerReg(), untrLclLo + 64, addrReg);
            addrOffset = -64;

            const regNumber endAddrReg = REG_ZERO_INIT_FRAME_REG2;

            if (endAddrReg == initReg)
            {
                *pInitRegZeroed = false;
            }

            genInstrWithConstant(INS_add, EA_PTRSIZE, endAddrReg, genFramePointerReg(), untrLclHi - 64, endAddrReg);

            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, addrOffset);
            addrOffset += simdRegPairSizeBytes;

            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, addrOffset);
            addrOffset += simdRegPairSizeBytes;

            assert(addrOffset == 0);

            GetEmitter()->emitIns_R_R_I_I(INS_bfm, EA_PTRSIZE, addrReg, REG_ZR, 0, 5);
            // addrReg points at the beginning of a cache line.

            GetEmitter()->emitIns_R(INS_dczva, EA_PTRSIZE, addrReg);
            GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, addrReg, addrReg, 64);
            GetEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, addrReg, endAddrReg);
            GetEmitter()->emitIns_J(INS_blo, NULL, -4);

            addrReg      = endAddrReg;
            bytesToWrite = 64;
        }
        else
        {
            genInstrWithConstant(INS_add, EA_PTRSIZE, addrReg, genFramePointerReg(), untrLclLo - 32, addrReg);
            addrOffset = 32;

            const regNumber countReg = REG_ZERO_INIT_FRAME_REG2;

            if (countReg == initReg)
            {
                *pInitRegZeroed = false;
            }

            instGen_Set_Reg_To_Imm(EA_PTRSIZE, countReg, bytesToWrite - 64);

            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, 32);
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, 64,
                                          INS_OPTS_PRE_INDEX);

            GetEmitter()->emitIns_R_R_I(INS_subs, EA_PTRSIZE, countReg, countReg, 64);
            GetEmitter()->emitIns_J(INS_bge, NULL, -4);

            bytesToWrite %= 64;
        }
    }
    else
    {
        genInstrWithConstant(INS_add, EA_PTRSIZE, addrReg, genFramePointerReg(), untrLclLo, addrReg);
    }

    if (bytesToWrite >= simdRegPairSizeBytes)
    {
        // Generates the following code:
        //
        //     movi    v16.16b, #0
        //     stp     q16, q16, [x9, #addrOffset]
        //     stp     q16, q16, [x9, #(addrOffset+32)]
        // ...
        //     stp     q16, q16, [x9, #(addrOffset+roundDown(bytesToWrite, 32))]

        if (!simdRegZeroed)
        {
            GetEmitter()->emitIns_R_I(INS_movi, EA_16BYTE, zeroSimdReg, 0, INS_OPTS_16B);
            simdRegZeroed = true;
        }

        for (; bytesToWrite >= simdRegPairSizeBytes; bytesToWrite -= simdRegPairSizeBytes)
        {
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_16BYTE, zeroSimdReg, zeroSimdReg, addrReg, addrOffset);
            addrOffset += simdRegPairSizeBytes;
        }
    }

    const int regPairSizeBytes = 2 * REGSIZE_BYTES;

    if (bytesToWrite >= regPairSizeBytes)
    {
        GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_ZR, REG_ZR, addrReg, addrOffset);
        addrOffset += regPairSizeBytes;
        bytesToWrite -= regPairSizeBytes;
    }

    if (bytesToWrite >= REGSIZE_BYTES)
    {
        GetEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, REG_ZR, addrReg, addrOffset);
        addrOffset += REGSIZE_BYTES;
        bytesToWrite -= REGSIZE_BYTES;
    }

    if (bytesToWrite == sizeof(int))
    {
        GetEmitter()->emitIns_R_R_I(INS_str, EA_4BYTE, REG_ZR, addrReg, addrOffset);
        bytesToWrite = 0;
    }

    assert(bytesToWrite == 0);
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           End Prolog / Epilog                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    assert(block->KindIs(BBJ_CALLFINALLY));

    // Generate a call to the finally, like this:
    //      mov         x0,qword ptr [fp + 10H] / sp    // Load x0 with PSPSym, or sp if PSPSym is not used
    //      bl          finally-funclet
    //      b           finally-return                  // Only for non-retless finally calls
    // The 'b' can be a NOP if we're going to the next block.

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        GetEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, REG_R0, compiler->lvaPSPSym, 0);
    }
    else
    {
        GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_R0, REG_SPBASE, /* canSkip */ false);
    }
    GetEmitter()->emitIns_J(INS_bl_local, block->GetTarget());

    BasicBlock* const nextBlock = block->Next();

    if (block->HasFlag(BBF_RETLESS_CALL))
    {
        // We have a retless call, and the last instruction generated was a call.
        // If the next block is in a different EH region (or is the end of the code
        // block), then we need to generate a breakpoint here (since it will never
        // get executed) to get proper unwind behavior.

        if ((nextBlock == nullptr) || !BasicBlock::sameEHRegion(block, nextBlock))
        {
            instGen(INS_BREAKPOINT); // This should never get executed
        }

        return block;
    }
    else
    {
        // Because of the way the flowgraph is connected, the liveness info for this one instruction
        // after the call is not (can not be) correct in cases where a variable has a last use in the
        // handler.  So turn off GC reporting for this single instruction.
        GetEmitter()->emitDisableGC();

        BasicBlock* const finallyContinuation = nextBlock->GetFinallyContinuation();

        // Now go to where the finally funclet needs to return to.
        if (nextBlock->NextIs(finallyContinuation) && !compiler->fgInDifferentRegions(nextBlock, finallyContinuation))
        {
            // Fall-through.
            // TODO-ARM64-CQ: Can we get rid of this instruction, and just have the call return directly
            // to the next instruction? This would depend on stack walking from within the finally
            // handler working without this instruction being in this special EH region.
            instGen(INS_nop);
        }
        else
        {
            inst_JMP(EJ_jmp, finallyContinuation);
        }

        GetEmitter()->emitEnableGC();

        return nextBlock;
    }
}

void CodeGen::genEHCatchRet(BasicBlock* block)
{
    // For long address (default): `adrp + add` will be emitted.
    // For short address (proven later): `adr` will be emitted.
    GetEmitter()->emitIns_R_L(INS_adr, EA_PTRSIZE, block->GetTarget(), REG_INTRET);
}

//  move an immediate value into an integer register

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
        // This emits a pair of adrp/add (two instructions) with fix-ups.
        GetEmitter()->emitIns_R_AI(INS_adrp, size, reg, imm DEBUGARG(targetHandle) DEBUGARG(gtFlags));
    }
    else if (imm == 0)
    {
        instGen_Set_Reg_To_Zero(size, reg, flags);
    }
    else
    {
        if (emitter::emitIns_valid_imm_for_mov(imm, size))
        {
            GetEmitter()->emitIns_R_I(INS_mov, size, reg, imm, INS_OPTS_NONE,
                                      INS_SCALABLE_OPTS_NONE DEBUGARG(targetHandle) DEBUGARG(gtFlags));
        }
        else
        {
            // Arm64 allows any arbitrary 16-bit constant to be loaded into a register halfword
            // There are three forms
            //    movk which loads into any halfword preserving the remaining halfwords
            //    movz which loads into any halfword zeroing the remaining halfwords
            //    movn which loads into any halfword zeroing the remaining halfwords then bitwise inverting the register
            // In some cases it is preferable to use movn, because it has the side effect of filling the other halfwords
            // with ones

            // Determine whether movn or movz will require the fewest instructions to populate the immediate
            int preferMovn = 0;

            for (int i = (size == EA_8BYTE) ? 48 : 16; i >= 0; i -= 16)
            {
                if (uint16_t(imm >> i) == 0xffff)
                    ++preferMovn; // a single movk 0xffff could be skipped if movn was used
                else if (uint16_t(imm >> i) == 0x0000)
                    --preferMovn; // a single movk 0 could be skipped if movz was used
            }

            // Select the first instruction.  Any additional instruction will use movk
            instruction ins = (preferMovn > 0) ? INS_movn : INS_movz;

            // Initial movz or movn will fill the remaining bytes with the skipVal
            // This can allow skipping filling a halfword
            uint16_t skipVal = (preferMovn > 0) ? 0xffff : 0;

            unsigned bits = (size == EA_8BYTE) ? 64 : 32;

            // Iterate over imm examining 16 bits at a time
            for (unsigned i = 0; i < bits; i += 16)
            {
                uint16_t imm16 = uint16_t(imm >> i);

                if (imm16 != skipVal)
                {
                    if (ins == INS_movn)
                    {
                        // For the movn case, we need to bitwise invert the immediate.  This is because
                        //   (movn x0, ~imm16) === (movz x0, imm16; or x0, x0, #0xffff`ffff`ffff`0000)
                        imm16 = ~imm16;
                    }

                    GetEmitter()->emitIns_R_I_I(ins, size, reg, imm16, i,
                                                INS_OPTS_LSL DEBUGARG(i == 0 ? targetHandle : 0)
                                                    DEBUGARG(i == 0 ? gtFlags : GTF_EMPTY));

                    // Once the initial movz/movn is emitted the remaining instructions will all use movk
                    ins = INS_movk;
                }
            }

            // We must emit a movn or movz or we have not done anything
            // The cases which hit this assert should be (emitIns_valid_imm_for_mov() == true) and
            // should not be in this else condition
            assert(ins == INS_movk);
        }
        // The caller may have requested that the flags be set on this mov (rarely/never)
        if (flags == INS_FLAGS_SET)
        {
            GetEmitter()->emitIns_R_I(INS_tst, size, reg, 0);
        }
    }

    regSet.verifyRegUsed(reg);
}

/***********************************************************************************
 *
 * Generate code to set a register 'targetReg' of type 'targetType' to the constant
 * specified by the constant (GT_CNS_INT or GT_CNS_DBL) in 'tree'. This does not call
 * genProduceReg() on the target register.
 */
void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, GenTree* tree)
{
    switch (tree->gtOper)
    {
        case GT_CNS_INT:
        {
            GenTreeIntCon* con    = tree->AsIntCon();
            ssize_t        cnsVal = con->IconValue();

            emitAttr attr = emitActualTypeSize(targetType);
            // TODO-CQ: Currently we cannot do this for all handles because of
            // https://github.com/dotnet/runtime/issues/60712
            if (con->ImmedValNeedsReloc(compiler))
            {
                attr = EA_SET_FLG(attr, EA_CNS_RELOC_FLG);
                GenTreeFlags tlsFlags = (GTF_ICON_TLSGD_OFFSET | GTF_ICON_TLS_HDL);

                if (tree->IsIconHandle(GTF_ICON_TLS_HDL))
                {
                    //attr = EA_SET_FLG(attr, EA_CNS_TLSGD_RELOC);
                    //if (tree->AsIntCon()->gtIconVal == 0)
                    //{
                    //    GetEmitter()->emitIns_R(INS_mrs_tpid0, attr, targetReg);
                    //}
                    //else
                    //{
                    //    attr = EA_SET_FLG(attr, EA_CNS_TLSGD_RELOC);
                    //    GetEmitter()->emitIns_R_R_I(INS_ldr, attr, tree->GetRegNum(), REG_R0, tree->AsIntCon()->gtIconVal);
                    //    //GetEmitter()->emitInsLoadStoreOp(INS_lea, attr, targetReg, tree);
                    //}
                    //break;
                }
                //else if (tree->IsIconHandle(GTF_ICON_FTN_ADDR) && ((tree->gtFlags & GTF_TLS_GET_ADDR) == GTF_TLS_GET_ADDR))
                //{
                //    GetEmitter()->emitIns_R_R(INS_lea, attr, tree->GetReg(), REG_R0);
                //    break;
                //}
                else if ((tree->gtFlags & tlsFlags) == tlsFlags)
                {
                    /*attr = EA_SET_FLG(attr, EA_CNS_TLSGD_RELOC);*/
                    break;
                }
            }

            if (targetType == TYP_BYREF)
            {
                attr = EA_SET_FLG(attr, EA_BYREF_FLG);
            }

            instGen_Set_Reg_To_Imm(attr, targetReg, cnsVal,
                                   INS_FLAGS_DONT_CARE DEBUGARG(con->gtTargetHandle) DEBUGARG(con->gtFlags));
            regSet.verifyRegUsed(targetReg);
        }
        break;

        case GT_CNS_DBL:
        {
            emitter* emit       = GetEmitter();
            emitAttr size       = emitActualTypeSize(tree);
            double   constValue = tree->AsDblCon()->DconValue();

            // Make sure we use "movi reg, 0x00"  only for positive zero (0.0) and not for negative zero (-0.0)
            if (*(__int64*)&constValue == 0)
            {
                // A faster/smaller way to generate 0.0
                // We will just zero out the entire vector register for both float and double
                emit->emitIns_R_I(INS_movi, EA_16BYTE, targetReg, 0x00, INS_OPTS_16B);
            }
            else if (emitter::emitIns_valid_imm_for_fmov(constValue))
            {
                // We can load the FP constant using the fmov FP-immediate for this constValue
                emit->emitIns_R_F(INS_fmov, size, targetReg, constValue);
            }
            else
            {
                // Get a temp integer register to compute long address.
                regNumber addrReg = tree->GetSingleTempReg();

                // We must load the FP constant from the constant pool
                // Emit a data section constant for the float or double constant.
                CORINFO_FIELD_HANDLE hnd = emit->emitFltOrDblConst(constValue, size);
                // For long address (default): `adrp + ldr + fmov` will be emitted.
                // For short address (proven later), `ldr` will be emitted.
                emit->emitIns_R_C(INS_ldr, size, targetReg, addrReg, hnd, 0);
            }
        }
        break;

        case GT_CNS_VEC:
        {
            GenTreeVecCon* vecCon = tree->AsVecCon();

            emitter* emit = GetEmitter();
            emitAttr attr = emitTypeSize(targetType);

            switch (tree->TypeGet())
            {
#if defined(FEATURE_SIMD)
                case TYP_SIMD8:
                {
                    if (vecCon->IsAllBitsSet())
                    {
                        emit->emitIns_R_I(INS_mvni, attr, targetReg, 0, INS_OPTS_2S);
                    }
                    else if (vecCon->IsZero())
                    {
                        emit->emitIns_R_I(INS_movi, attr, targetReg, 0, INS_OPTS_2S);
                    }
                    else
                    {
                        // Get a temp integer register to compute long address.
                        regNumber addrReg = tree->GetSingleTempReg();

                        simd8_t constValue;
                        memcpy(&constValue, &vecCon->gtSimdVal, sizeof(simd8_t));

                        CORINFO_FIELD_HANDLE hnd = emit->emitSimd8Const(constValue);
                        emit->emitIns_R_C(INS_ldr, attr, targetReg, addrReg, hnd, 0);
                    }
                    break;
                }

                case TYP_SIMD12:
                {
                    if (vecCon->IsAllBitsSet())
                    {
                        emit->emitIns_R_I(INS_mvni, attr, targetReg, 0, INS_OPTS_4S);
                    }
                    else if (vecCon->IsZero())
                    {
                        emit->emitIns_R_I(INS_movi, attr, targetReg, 0, INS_OPTS_4S);
                    }
                    else
                    {
                        // Get a temp integer register to compute long address.
                        regNumber addrReg = tree->GetSingleTempReg();

                        simd16_t constValue = {};
                        memcpy(&constValue, &vecCon->gtSimdVal, sizeof(simd12_t));

                        CORINFO_FIELD_HANDLE hnd = emit->emitSimd16Const(constValue);
                        emit->emitIns_R_C(INS_ldr, attr, targetReg, addrReg, hnd, 0);
                    }
                    break;
                }

                case TYP_SIMD16:
                {
                    if (vecCon->IsAllBitsSet())
                    {
                        emit->emitIns_R_I(INS_mvni, attr, targetReg, 0, INS_OPTS_4S);
                    }
                    else if (vecCon->IsZero())
                    {
                        emit->emitIns_R_I(INS_movi, attr, targetReg, 0, INS_OPTS_4S);
                    }
                    else
                    {
                        // Get a temp integer register to compute long address.
                        regNumber addrReg = tree->GetSingleTempReg();

                        simd16_t constValue;
                        memcpy(&constValue, &vecCon->gtSimdVal, sizeof(simd16_t));

                        CORINFO_FIELD_HANDLE hnd = emit->emitSimd16Const(constValue);
                        emit->emitIns_R_C(INS_ldr, attr, targetReg, addrReg, hnd, 0);
                    }
                    break;
                }
#endif // FEATURE_SIMD

                default:
                {
                    unreached();
                }
            }

            break;
        }

        default:
            unreached();
    }
}

// Produce code for a GT_INC_SATURATE node.
void CodeGen::genCodeForIncSaturate(GenTree* tree)
{
    regNumber targetReg = tree->GetRegNum();

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(!tree->isContained());
    // The dst can only be a register.
    assert(targetReg != REG_NA);

    GenTree* operand = tree->gtGetOp1();
    assert(!operand->isContained());
    // The src must be a register.
    regNumber operandReg = genConsumeReg(operand);

    GetEmitter()->emitIns_R_R_I(INS_adds, emitActualTypeSize(tree), targetReg, operandReg, 1);
    GetEmitter()->emitIns_R_R_COND(INS_cinv, emitActualTypeSize(tree), targetReg, targetReg, INS_COND_HS);

    genProduceReg(tree);
}

// Generate code to get the high N bits of a N*N=2N bit multiplication result
void CodeGen::genCodeForMulHi(GenTreeOp* treeNode)
{
    assert(!treeNode->gtOverflowEx());

    genConsumeOperands(treeNode);

    regNumber targetReg  = treeNode->GetRegNum();
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = GetEmitter();
    emitAttr  attr       = emitActualTypeSize(treeNode);
    unsigned  isUnsigned = (treeNode->gtFlags & GTF_UNSIGNED);

    GenTree* op1 = treeNode->gtGetOp1();
    GenTree* op2 = treeNode->gtGetOp2();

    assert(!varTypeIsFloating(targetType));

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(targetReg != REG_NA);

    if (EA_SIZE(attr) == EA_8BYTE)
    {
        instruction ins = isUnsigned ? INS_umulh : INS_smulh;

        regNumber r = emit->emitInsTernary(ins, attr, treeNode, op1, op2);

        assert(r == targetReg);
    }
    else
    {
        assert(EA_SIZE(attr) == EA_4BYTE);

        instruction ins = isUnsigned ? INS_umull : INS_smull;

        regNumber r = emit->emitInsTernary(ins, EA_4BYTE, treeNode, op1, op2);

        emit->emitIns_R_R_I(isUnsigned ? INS_lsr : INS_asr, EA_8BYTE, targetReg, targetReg, 32);
    }

    genProduceReg(treeNode);
}

// Generate code for ADD, SUB, MUL, DIV, UDIV, AND, AND_NOT, OR and XOR
// This method is expected to have called genConsumeOperands() before calling it.
void CodeGen::genCodeForBinary(GenTreeOp* tree)
{
    const genTreeOps oper       = tree->OperGet();
    regNumber        targetReg  = tree->GetRegNum();
    var_types        targetType = tree->TypeGet();
    emitter*         emit       = GetEmitter();

    assert(tree->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_DIV, GT_UDIV, GT_AND, GT_AND_NOT, GT_OR, GT_XOR));

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(targetReg != REG_NA);

    // Handles combined operations: 'madd', 'msub'
    if (op2->OperIs(GT_MUL) && op2->isContained())
    {
        // In the future, we might consider enabling this for floating-point "unsafe" math.
        assert(varTypeIsIntegral(tree));

        // These operations cannot set flags
        assert((tree->gtFlags & GTF_SET_FLAGS) == 0);

        GenTree* a = op1;
        GenTree* b = op2->gtGetOp1();
        GenTree* c = op2->gtGetOp2();

        instruction ins;
        switch (oper)
        {
            case GT_ADD:
            {
                // d = a + b * c
                // madd: d, b, c, a
                ins = INS_madd;
                break;
            }

            case GT_SUB:
            {
                // d = a - b * c
                // msub: d, b, c, a
                ins = INS_msub;
                break;
            }

            default:
                unreached();
        }

        emit->emitIns_R_R_R_R(ins, emitActualTypeSize(tree), targetReg, b->GetRegNum(), c->GetRegNum(), a->GetRegNum());
        genProduceReg(tree);
        return;
    }
    else if (op2->OperIs(GT_LSH, GT_RSH, GT_RSZ) && op2->isContained())
    {
        assert(varTypeIsIntegral(tree));

        GenTree* a = op1;
        GenTree* b = op2->gtGetOp1();
        GenTree* c = op2->gtGetOp2();

        // The shift amount needs to be contained as well
        assert(c->isContained() && c->IsCnsIntOrI());

        instruction ins = genGetInsForOper(tree->OperGet(), targetType);
        insOpts     opt = INS_OPTS_NONE;

        if ((tree->gtFlags & GTF_SET_FLAGS) != 0)
        {
            // A subset of operations can still set flags

            switch (oper)
            {
                case GT_ADD:
                {
                    ins = INS_adds;
                    break;
                }

                case GT_SUB:
                {
                    ins = INS_subs;
                    break;
                }

                case GT_AND:
                {
                    ins = INS_ands;
                    break;
                }

                default:
                {
                    noway_assert(!"Unexpected BinaryOp with GTF_SET_FLAGS set");
                }
            }
        }

        opt = ShiftOpToInsOpts(op2->gtOper);

        emit->emitIns_R_R_R_I(ins, emitActualTypeSize(tree), targetReg, a->GetRegNum(), b->GetRegNum(),
                              c->AsIntConCommon()->IconValue(), opt);

        genProduceReg(tree);
        return;
    }
    else if (op2->OperIs(GT_CAST) && op2->isContained())
    {
        assert(varTypeIsIntegral(tree));

        GenTree* a = op1;
        GenTree* b = op2->AsCast()->CastOp();

        instruction ins = genGetInsForOper(tree->OperGet(), targetType);
        insOpts     opt = INS_OPTS_NONE;

        if ((tree->gtFlags & GTF_SET_FLAGS) != 0)
        {
            // A subset of operations can still set flags

            switch (oper)
            {
                case GT_ADD:
                {
                    ins = INS_adds;
                    break;
                }

                case GT_SUB:
                {
                    ins = INS_subs;
                    break;
                }

                default:
                {
                    noway_assert(!"Unexpected BinaryOp with GTF_SET_FLAGS set");
                }
            }
        }

        bool isZeroExtending = op2->AsCast()->IsZeroExtending();

        if (varTypeIsByte(op2->CastToType()))
        {
            opt = isZeroExtending ? INS_OPTS_UXTB : INS_OPTS_SXTB;
        }
        else if (varTypeIsShort(op2->CastToType()))
        {
            opt = isZeroExtending ? INS_OPTS_UXTH : INS_OPTS_SXTH;
        }
        else
        {
            assert(op2->TypeIs(TYP_LONG) && genActualTypeIsInt(b));
            opt = isZeroExtending ? INS_OPTS_UXTW : INS_OPTS_SXTW;
        }

        emit->emitIns_R_R_R(ins, emitActualTypeSize(tree), targetReg, a->GetRegNum(), b->GetRegNum(), opt);

        genProduceReg(tree);
        return;
    }

    instruction ins = genGetInsForOper(tree->OperGet(), targetType);

    if ((tree->gtFlags & GTF_SET_FLAGS) != 0)
    {
        switch (oper)
        {
            case GT_ADD:
                ins = INS_adds;
                break;
            case GT_SUB:
                ins = INS_subs;
                break;
            case GT_AND:
                ins = INS_ands;
                break;
            case GT_AND_NOT:
                ins = INS_bics;
                break;
            default:
                noway_assert(!"Unexpected BinaryOp with GTF_SET_FLAGS set");
        }
    }

    regNumber r = emit->emitInsTernary(ins, emitActualTypeSize(tree), tree, op1, op2);
    assert(r == targetReg);

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

    unsigned   varNum     = tree->GetLclNum();
    LclVarDsc* varDsc     = compiler->lvaGetDesc(varNum);
    var_types  targetType = varDsc->GetRegisterType(tree);

    bool isRegCandidate = varDsc->lvIsRegCandidate();

    // lcl_vars are not defs
    assert((tree->gtFlags & GTF_VAR_DEF) == 0);

    // If this is a register candidate that has been spilled, genConsumeReg() will
    // reload it at the point of use.  Otherwise, if it's not in a register, we load it here.

    if (!isRegCandidate && !tree->IsMultiReg() && !(tree->gtFlags & GTF_SPILLED))
    {
        // targetType must be a normal scalar type and not a TYP_STRUCT
        assert(targetType != TYP_STRUCT);

        instruction ins  = ins_Load(targetType);
        emitAttr    attr = emitActualTypeSize(targetType);

        emitter* emit = GetEmitter();
        emit->emitIns_R_S(ins, attr, tree->GetRegNum(), varNum, 0);
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

#ifdef FEATURE_SIMD
    if (targetType == TYP_SIMD12)
    {
        genStoreLclTypeSimd12(tree);
        return;
    }
#endif // FEATURE_SIMD

    regNumber targetReg = tree->GetRegNum();
    emitter*  emit      = GetEmitter();
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
        dataReg = REG_ZR;
    }
    else if (data->isContained())
    {
        if (data->IsCnsVec())
        {
            assert(data->AsVecCon()->IsZero());
            dataReg = REG_ZR;
        }
        else
        {
            assert(data->OperIs(GT_BITCAST));
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

    instruction ins = ins_StoreFromSrc(dataReg, targetType);

    emitAttr attr = emitActualTypeSize(targetType);

    emit->emitIns_S_R(ins, attr, dataReg, varNum, offset);

    genUpdateLife(tree);

    varDsc->SetRegNum(REG_STK);
}

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
        assert(varTypeIsSIMD(data) && varDsc->lvIsHfa() && (varDsc->GetHfaType() == TYP_FLOAT));
        regNumber    operandReg = genConsumeReg(data);
        unsigned int regCount   = varDsc->lvFieldCnt;
        for (unsigned i = 0; i < regCount; ++i)
        {
            regNumber varReg = lclNode->GetRegByIndex(i);
            assert(varReg != REG_NA);
            unsigned   fieldLclNum = varDsc->lvFieldLclStart + i;
            LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(fieldLclNum);
            assert(fieldVarDsc->TypeGet() == TYP_FLOAT);
            GetEmitter()->emitIns_R_R_I(INS_dup, emitTypeSize(TYP_FLOAT), varReg, operandReg, i);
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
                    emit->emitIns_R_I(INS_movi, emitActualTypeSize(targetType), targetReg, 0x00, INS_OPTS_16B);
                }
                else
                {
                    if (targetType == TYP_SIMD16)
                    {
                        GetEmitter()->emitIns_S_S_R_R(INS_stp, EA_8BYTE, EA_8BYTE, REG_ZR, REG_ZR, varNum, 0);
                    }
                    else
                    {
                        assert(targetType == TYP_SIMD8);
                        GetEmitter()->emitIns_S_R(INS_str, EA_8BYTE, REG_ZR, varNum, 0);
                    }
                }
                genUpdateLifeStore(lclNode, targetReg, varDsc);
                return;
            }
            if (zeroInit)
            {
                dataReg = REG_ZR;
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

            instruction ins  = ins_StoreFromSrc(dataReg, targetType);
            emitAttr    attr = emitActualTypeSize(targetType);

            emit->emitIns_S_R(ins, attr, dataReg, varNum, /* offset */ 0);
        }
        else // store into register (i.e move into register)
        {
            // Assign into targetReg when dataReg (from op1) is not the same register
            // Only zero/sign extend if we are using general registers.
            if (varTypeIsIntegral(targetType) && emit->isGeneralRegister(targetReg) && emit->isGeneralRegister(dataReg))
            {
                // We use 'emitActualTypeSize' as the instructions require 8BYTE or 4BYTE.
                inst_Mov_Extend(targetType, /* srcInReg */ true, targetReg, dataReg, /* canSkip */ true,
                                emitActualTypeSize(targetType));
            }
            else if (TargetOS::IsUnix && data->IsIconHandle(GTF_ICON_TLS_HDL))
            {
                assert(data->AsIntCon()->IconValue() == 0);
                emitAttr attr = emitActualTypeSize(targetType);
                // On non-windows, need to load the address from system register.
                emit->emitIns_R(INS_mrs_tpid0, attr, targetReg);
            }
            else
            {
                inst_Mov(targetType, targetReg, dataReg, /* canSkip */ true);
            }
        }
        genUpdateLifeStore(lclNode, targetReg, varDsc);
    }
}

//------------------------------------------------------------------------
// genSimpleReturn: Generates code for simple return statement for arm64.
//
// Note: treeNode's and op1's registers are already consumed.
//
// Arguments:
//    treeNode - The GT_RETURN or GT_RETFILT tree node with non-struct and non-void type
//
// Return Value:
//    None
//
void CodeGen::genSimpleReturn(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    GenTree*  op1        = treeNode->gtGetOp1();
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
    emitAttr attr = emitActualTypeSize(targetType);
    GetEmitter()->emitIns_Mov(INS_mov, attr, retReg, op1->GetRegNum(), /* canSkip */ !movRequired);
}

/***********************************************************************************************
 *  Generate code for localloc
 */
void CodeGen::genLclHeap(GenTree* tree)
{
    assert(tree->OperGet() == GT_LCLHEAP);
    assert(compiler->compLocallocUsed);

    GenTree* size = tree->AsOp()->gtOp1;
    noway_assert((genActualType(size->gtType) == TYP_INT) || (genActualType(size->gtType) == TYP_I_IMPL));

    regNumber            targetReg                = tree->GetRegNum();
    regNumber            regCnt                   = REG_NA;
    regNumber            pspSymReg                = REG_NA;
    var_types            type                     = genActualType(size->gtType);
    emitAttr             easz                     = emitTypeSize(type);
    BasicBlock*          endLabel                 = nullptr;
    BasicBlock*          loop                     = nullptr;
    unsigned             stackAdjustment          = 0;
    const target_ssize_t ILLEGAL_LAST_TOUCH_DELTA = (target_ssize_t)-1;
    target_ssize_t       lastTouchDelta =
        ILLEGAL_LAST_TOUCH_DELTA; // The number of bytes from SP to the last stack address probed.

    noway_assert(isFramePointerUsed()); // localloc requires Frame Pointer to be established since SP changes
    noway_assert(genStackLevel == 0);   // Can't have anything on the stack

    // compute the amount of memory to allocate to properly STACK_ALIGN.
    size_t amount = 0;
    if (size->IsCnsIntOrI())
    {
        // If size is a constant, then it must be contained.
        assert(size->isContained());

        // If amount is zero then return null in targetReg
        amount = size->AsIntCon()->gtIconVal;
        if (amount == 0)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, targetReg);
            goto BAILOUT;
        }

        // 'amount' is the total number of bytes to localloc to properly STACK_ALIGN
        amount = AlignUp(amount, STACK_ALIGN);
    }
    else
    {
        // If 0 bail out by returning null in targetReg
        genConsumeRegAndCopy(size, targetReg);
        endLabel = genCreateTempLabel();
        GetEmitter()->emitIns_R_R(INS_tst, easz, targetReg, targetReg);
        inst_JMP(EJ_eq, endLabel);

        // Compute the size of the block to allocate and perform alignment.
        // If compInitMem=true, we can reuse targetReg as regcnt,
        // since we don't need any internal registers.
        if (compiler->info.compInitMem)
        {
            assert(tree->AvailableTempRegCount() == 0);
            regCnt = targetReg;
        }
        else
        {
            regCnt = tree->ExtractTempReg();
            inst_Mov(size->TypeGet(), regCnt, targetReg, /* canSkip */ true);
        }

        // Align to STACK_ALIGN
        // regCnt will be the total number of bytes to localloc
        inst_RV_IV(INS_add, regCnt, (STACK_ALIGN - 1), emitActualTypeSize(type));
        inst_RV_IV(INS_and, regCnt, ~(STACK_ALIGN - 1), emitActualTypeSize(type));
    }

    // If we have an outgoing arg area then we must adjust the SP by popping off the
    // outgoing arg area. We will restore it right before we return from this method.
    //
    // Localloc returns stack space that aligned to STACK_ALIGN bytes. The following
    // are the cases that need to be handled:
    //   i) Method has out-going arg area.
    //      It is guaranteed that size of out-going arg area is STACK_ALIGN'ed (see fgMorphArgs).
    //      Therefore, we will pop off the out-going arg area from the stack pointer before allocating the localloc
    //      space.
    //  ii) Method has no out-going arg area.
    //      Nothing to pop off from the stack.
    if (compiler->lvaOutgoingArgSpaceSize > 0)
    {
        assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) == 0); // This must be true for the stack to remain
                                                                        // aligned
        genInstrWithConstant(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize,
                             rsGetRsvdReg());
        stackAdjustment += compiler->lvaOutgoingArgSpaceSize;
    }

    if (size->IsCnsIntOrI())
    {
        // We should reach here only for non-zero, constant size allocations.
        assert(amount > 0);

        const int storePairRegsWritesBytes = 2 * REGSIZE_BYTES;

        // For small allocations we will generate up to four stp instructions, to zero 16 to 64 bytes.
        static_assert_no_msg(STACK_ALIGN == storePairRegsWritesBytes);
        assert(amount % storePairRegsWritesBytes == 0); // stp stores two registers at a time

        if (compiler->info.compInitMem)
        {
            if (amount <= compiler->getUnrollThreshold(Compiler::UnrollKind::Memset))
            {
                // The following zeroes the last 16 bytes and probes the page containing [sp, #16] address.
                // stp xzr, xzr, [sp, #-16]!
                GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_ZR, REG_SPBASE, -storePairRegsWritesBytes,
                                              INS_OPTS_PRE_INDEX);

                if (amount > storePairRegsWritesBytes)
                {
                    // The following sets SP to its final value and zeroes the first 16 bytes of the allocated space.
                    // stp xzr, xzr, [sp, #-amount+16]!
                    const ssize_t finalSpDelta = (ssize_t)amount - storePairRegsWritesBytes;
                    GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_ZR, REG_SPBASE, -finalSpDelta,
                                                  INS_OPTS_PRE_INDEX);

                    // The following zeroes the remaining space in [finalSp+16, initialSp-16) interval
                    // using a sequence of stp instruction with unsigned offset.
                    for (ssize_t offset = storePairRegsWritesBytes; offset < finalSpDelta;
                         offset += storePairRegsWritesBytes)
                    {
                        // stp xzr, xzr, [sp, #offset]
                        GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_ZR, REG_SPBASE, offset);
                    }
                }

                lastTouchDelta = 0;

                goto ALLOC_DONE;
            }
        }
        else if (amount < compiler->eeGetPageSize()) // must be < not <=
        {
            // Since the size is less than a page, simply adjust the SP value.
            // The SP might already be in the guard page, so we must touch it BEFORE
            // the alloc, not after.

            // Note the we check against the lower boundary of the post-index immediate range [-256, 256)
            // since the offset is -amount.
            const bool canEncodeLoadRegPostIndexOffset = amount <= 256;

            if (canEncodeLoadRegPostIndexOffset)
            {
                GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, -(ssize_t)amount,
                                            INS_OPTS_POST_INDEX);
            }
            else if (emitter::canEncodeLoadOrStorePairOffset(-(ssize_t)amount, EA_8BYTE))
            {
                // The following probes the page and allocates the local heap.
                // ldp tmpReg, xzr, [sp], #-amount
                // Note that we cannot use ldp xzr, xzr since
                // the behaviour of ldp where two source registers are the same is unpredictable.
                const regNumber tmpReg = targetReg;
                GetEmitter()->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, tmpReg, REG_ZR, REG_SPBASE, -(ssize_t)amount,
                                              INS_OPTS_POST_INDEX);
            }
            else
            {
                // ldr wzr, [sp]
                // sub, sp, #amount
                GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, 0);
                genInstrWithConstant(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, amount, rsGetRsvdReg());
            }

            lastTouchDelta = amount;

            goto ALLOC_DONE;
        }

        // else, "mov regCnt, amount"
        // If compInitMem=true, we can reuse targetReg as regcnt.
        // Since size is a constant, regCnt is not yet initialized.
        assert(regCnt == REG_NA);
        if (compiler->info.compInitMem)
        {
            assert(tree->AvailableTempRegCount() == 0);
            regCnt = targetReg;
        }
        else
        {
            regCnt = tree->ExtractTempReg();
        }
        instGen_Set_Reg_To_Imm(((unsigned int)amount == amount) ? EA_4BYTE : EA_8BYTE, regCnt, amount);
    }

    if (compiler->info.compInitMem)
    {
        BasicBlock* loop = genCreateTempLabel();

        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        // Since we have to zero out the allocated memory AND ensure that the stack pointer is always valid
        // by tickling the pages, we will just push 0's on the stack.
        //
        // Note: regCnt is guaranteed to be even on Amd64 since STACK_ALIGN/TARGET_POINTER_SIZE = 2
        // and localloc size is a multiple of STACK_ALIGN.

        // Loop:
        genDefineTempLabel(loop);

        // We can use pre-indexed addressing.
        // stp ZR, ZR, [SP, #-16]!
        GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_ZR, REG_ZR, REG_SPBASE, -16, INS_OPTS_PRE_INDEX);

        // If not done, loop
        // Note that regCnt is the number of bytes to stack allocate.
        // Therefore we need to subtract 16 from regcnt here.
        assert(genIsValidIntReg(regCnt));
        inst_RV_IV(INS_subs, regCnt, 16, emitActualTypeSize(type));
        inst_JMP(EJ_ne, loop);

        lastTouchDelta = 0;
    }
    else
    {
        // At this point 'regCnt' is set to the total number of bytes to localloc.
        //
        // We don't need to zero out the allocated memory. However, we do have
        // to tickle the pages to ensure that SP is always valid and is
        // in sync with the "stack guard page".  Note that in the worst
        // case SP is on the last byte of the guard page.  Thus you must
        // touch SP-0 first not SP-0x1000.
        //
        // This is similar to the prolog code in CodeGen::genAllocLclFrame().
        //
        // Note that we go through a few hoops so that SP never points to
        // illegal pages at any time during the tickling process.
        //
        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        //       bvc   Loop                    // result is smaller than original SP (no wrap around)
        //       mov   regCnt, #0              // Overflow, pick lowest possible value
        //
        //  Loop:
        //       ldr   wzr, [SP + 0]           // tickle the page - read from the page
        //       sub   regTmp, SP, PAGE_SIZE   // decrement SP by eeGetPageSize()
        //       cmp   regTmp, regCnt
        //       jb    Done
        //       mov   SP, regTmp
        //       j     Loop
        //
        //  Done:
        //       mov   SP, regCnt
        //

        // Setup the regTmp
        regNumber regTmp = tree->GetSingleTempReg();

        BasicBlock* loop = genCreateTempLabel();
        BasicBlock* done = genCreateTempLabel();

        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        GetEmitter()->emitIns_R_R_R(INS_subs, EA_PTRSIZE, regCnt, REG_SPBASE, regCnt);

        inst_JMP(EJ_vc, loop); // branch if the V flag is not set

        // Overflow, set regCnt to lowest possible value
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);

        genDefineTempLabel(loop);

        // tickle the page - Read from the updated SP - this triggers a page fault when on the guard page
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, 0);

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

        const regNumber tmpReg = rsGetRsvdReg();

        if ((lastTouchDelta == ILLEGAL_LAST_TOUCH_DELTA) ||
            (stackAdjustment + (unsigned)lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES >
             compiler->eeGetPageSize()))
        {
            genStackPointerConstantAdjustmentLoopWithProbe(-(ssize_t)stackAdjustment, tmpReg);
        }
        else
        {
            genStackPointerConstantAdjustment(-(ssize_t)stackAdjustment, tmpReg);
        }

        // Return the stackalloc'ed address in result register.
        // TargetReg = SP + stackAdjustment.
        //
        genInstrWithConstant(INS_add, EA_PTRSIZE, targetReg, REG_SPBASE, (ssize_t)stackAdjustment, tmpReg);
    }
    else // stackAdjustment == 0
    {
        // Move the final value of SP to targetReg
        inst_Mov(TYP_I_IMPL, targetReg, REG_SPBASE, /* canSkip */ false);
    }

BAILOUT:
    if (endLabel != nullptr)
        genDefineTempLabel(endLabel);

    genProduceReg(tree);
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

    if ((tree->gtFlags & GTF_SET_FLAGS) != 0)
    {
        switch (tree->OperGet())
        {
            case GT_NEG:
                ins = INS_negs;
                break;
            default:
                noway_assert(!"Unexpected UnaryOp with GTF_SET_FLAGS set");
        }
    }

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(!tree->isContained());
    // The dst can only be a register.
    assert(targetReg != REG_NA);

    GenTree* operand = tree->gtGetOp1();
    // The src must be a register.
    if (tree->OperIs(GT_NEG) && operand->isContained())
    {
        genTreeOps oper = operand->OperGet();
        switch (oper)
        {
            case GT_MUL:
            {
                ins          = INS_mneg;
                GenTree* op1 = tree->gtGetOp1();
                GenTree* a   = op1->gtGetOp1();
                GenTree* b   = op1->gtGetOp2();
                genConsumeRegs(op1);
                GetEmitter()->emitIns_R_R_R(ins, emitActualTypeSize(tree), targetReg, a->GetRegNum(), b->GetRegNum());
            }
            break;

            case GT_LSH:
            case GT_RSH:
            case GT_RSZ:
            {
                assert(ins == INS_neg || ins == INS_negs);
                assert(operand->gtGetOp2()->IsCnsIntOrI());
                assert(operand->gtGetOp2()->isContained());

                GenTree* op1 = tree->gtGetOp1();
                GenTree* a   = op1->gtGetOp1();
                GenTree* b   = op1->gtGetOp2();
                genConsumeRegs(op1);
                GetEmitter()->emitIns_R_R_I(ins, emitActualTypeSize(tree), targetReg, a->GetRegNum(),
                                            b->AsIntConCommon()->IntegralValue(), ShiftOpToInsOpts(oper));
            }
            break;

            default:
                unreached();
        }
    }
    else
    {
        assert(!operand->isContained());
        regNumber operandReg = genConsumeReg(operand);
        GetEmitter()->emitIns_R_R(ins, emitActualTypeSize(tree), targetReg, operandReg);
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForBswap: Produce code for a GT_BSWAP / GT_BSWAP16 node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForBswap(GenTree* tree)
{
    assert(tree->OperIs(GT_BSWAP, GT_BSWAP16));

    regNumber targetReg  = tree->GetRegNum();
    var_types targetType = tree->TypeGet();

    GenTree* operand = tree->gtGetOp1();
    assert(operand->isUsedFromReg());
    regNumber operandReg = genConsumeReg(operand);

    if (tree->OperIs(GT_BSWAP))
    {
        inst_RV_RV(INS_rev, targetReg, operandReg, targetType);
    }
    else
    {
        inst_RV_RV(INS_rev16, targetReg, operandReg, targetType);

        if (!genCanOmitNormalizationForBswap16(tree))
        {
            GetEmitter()->emitIns_Mov(INS_uxth, EA_4BYTE, targetReg, targetReg, /* canSkip */ false);
        }
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForDivMod: Produce code for a GT_DIV/GT_UDIV node. We don't see MOD:
// (1) integer MOD is morphed into a sequence of sub, mul, div in fgMorph;
// (2) float/double MOD is morphed into a helper call by front-end.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForDivMod(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_DIV, GT_UDIV));

    var_types targetType = tree->TypeGet();
    emitter*  emit       = GetEmitter();

    genConsumeOperands(tree);

    if (varTypeIsFloating(targetType))
    {
        // Floating point divide never raises an exception
        genCodeForBinary(tree);
    }
    else // an integer divide operation
    {
        // Generate the require runtime checks for GT_DIV or GT_UDIV.

        GenTree* divisorOp = tree->gtGetOp2();
        emitAttr size      = EA_ATTR(genTypeSize(genActualType(tree->TypeGet())));

        regNumber divisorReg = divisorOp->GetRegNum();

        ExceptionSetFlags exSetFlags = tree->OperExceptions(compiler);

        // (AnyVal / 0) => DivideByZeroException
        if ((exSetFlags & ExceptionSetFlags::DivideByZeroException) != ExceptionSetFlags::None)
        {
            if (divisorOp->IsIntegralConst(0))
            {
                // We unconditionally throw a divide by zero exception
                genJumpToThrowHlpBlk(EJ_jmp, SCK_DIV_BY_ZERO);

                // We still need to call genProduceReg
                genProduceReg(tree);

                return;
            }
            else
            {
                // Check if the divisor is zero throw a DivideByZeroException
                emit->emitIns_R_I(INS_cmp, size, divisorReg, 0);
                genJumpToThrowHlpBlk(EJ_eq, SCK_DIV_BY_ZERO);
            }
        }

        // (MinInt / -1) => ArithmeticException
        if ((exSetFlags & ExceptionSetFlags::ArithmeticException) != ExceptionSetFlags::None)
        {
            // Signed-division might overflow.

            assert(tree->OperIs(GT_DIV));
            assert(!divisorOp->IsIntegralConst(0));

            BasicBlock* sdivLabel  = genCreateTempLabel();
            GenTree*    dividendOp = tree->gtGetOp1();

            // Check if the divisor is not -1 branch to 'sdivLabel'
            emit->emitIns_R_I(INS_cmp, size, divisorReg, -1);

            inst_JMP(EJ_ne, sdivLabel);
            // If control flow continues past here the 'divisorReg' is known to be -1

            regNumber dividendReg = dividendOp->GetRegNum();
            // At this point the divisor is known to be -1
            //
            // Issue the 'cmp dividendReg, 1' instruction.
            // This is an alias to 'subs zr, dividendReg, 1' on ARM64 itself.
            // This will set the V (overflow) flags only when dividendReg is MinInt
            //
            emit->emitIns_R_I(INS_cmp, size, dividendReg, 1);
            genJumpToThrowHlpBlk(EJ_vs, SCK_ARITH_EXCPN); // if the V flags is set throw
                                                          // ArithmeticException

            genDefineTempLabel(sdivLabel);
        }

        genCodeForBinary(tree); // Generate the sdiv instruction
    }
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

    ClassLayout* layout = cpObjNode->GetLayout();
    unsigned     slots  = layout->GetSlotCount();

    // Temp register(s) used to perform the sequence of loads and stores.
    regNumber tmpReg  = cpObjNode->ExtractTempReg();
    regNumber tmpReg2 = REG_NA;

    assert(genIsValidIntReg(tmpReg));
    assert(tmpReg != REG_WRITE_BARRIER_SRC_BYREF);
    assert(tmpReg != REG_WRITE_BARRIER_DST_BYREF);

    if (slots > 1)
    {
        tmpReg2 = cpObjNode->GetSingleTempReg();
        assert(tmpReg2 != tmpReg);
        assert(genIsValidIntReg(tmpReg2));
        assert(tmpReg2 != REG_WRITE_BARRIER_DST_BYREF);
        assert(tmpReg2 != REG_WRITE_BARRIER_SRC_BYREF);
    }

    if (cpObjNode->IsVolatile())
    {
        // issue a full memory barrier before a volatile CpObj operation
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();

    // If we can prove it's on the stack we don't need to use the write barrier.
    if (dstOnStack)
    {
        unsigned i = 0;
        // Check if two or more remaining slots and use a ldp/stp sequence
        while (i < slots - 1)
        {
            emitAttr attr0 = emitTypeSize(layout->GetGCPtrType(i + 0));
            emitAttr attr1 = emitTypeSize(layout->GetGCPtrType(i + 1));

            emit->emitIns_R_R_R_I(INS_ldp, attr0, tmpReg, tmpReg2, REG_WRITE_BARRIER_SRC_BYREF, 2 * TARGET_POINTER_SIZE,
                                  INS_OPTS_POST_INDEX, attr1);
            emit->emitIns_R_R_R_I(INS_stp, attr0, tmpReg, tmpReg2, REG_WRITE_BARRIER_DST_BYREF, 2 * TARGET_POINTER_SIZE,
                                  INS_OPTS_POST_INDEX, attr1);
            i += 2;
        }

        // Use a ldr/str sequence for the last remainder
        if (i < slots)
        {
            emitAttr attr0 = emitTypeSize(layout->GetGCPtrType(i + 0));

            emit->emitIns_R_R_I(INS_ldr, attr0, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE,
                                INS_OPTS_POST_INDEX);
            emit->emitIns_R_R_I(INS_str, attr0, tmpReg, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE,
                                INS_OPTS_POST_INDEX);
        }
    }
    else
    {
        unsigned gcPtrCount = cpObjNode->GetLayout()->GetGCPtrCount();

        unsigned i = 0;
        while (i < slots)
        {
            if (!layout->IsGCPtr(i))
            {
                // Check if the next slot's type is also TYP_GC_NONE and use ldp/stp
                if ((i + 1 < slots) && !layout->IsGCPtr(i + 1))
                {
                    emit->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, tmpReg, tmpReg2, REG_WRITE_BARRIER_SRC_BYREF,
                                          2 * TARGET_POINTER_SIZE, INS_OPTS_POST_INDEX);
                    emit->emitIns_R_R_R_I(INS_stp, EA_8BYTE, tmpReg, tmpReg2, REG_WRITE_BARRIER_DST_BYREF,
                                          2 * TARGET_POINTER_SIZE, INS_OPTS_POST_INDEX);
                    ++i; // extra increment of i, since we are copying two items
                }
                else
                {
                    emit->emitIns_R_R_I(INS_ldr, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE,
                                        INS_OPTS_POST_INDEX);
                    emit->emitIns_R_R_I(INS_str, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE,
                                        INS_OPTS_POST_INDEX);
                }
            }
            else
            {
                // In the case of a GC-Pointer we'll call the ByRef write barrier helper
                genEmitHelperCall(CORINFO_HELP_ASSIGN_BYREF, 0, EA_PTRSIZE);
                gcPtrCount--;
            }
            ++i;
        }
        assert(gcPtrCount == 0);
    }

    if (cpObjNode->IsVolatile())
    {
        // issue a load barrier after a volatile CpObj operation
        instGen_MemoryBarrier(BARRIER_LOAD_ONLY);
    }

    // Clear the gcInfo for REG_WRITE_BARRIER_SRC_BYREF and REG_WRITE_BARRIER_DST_BYREF.
    // While we normally update GC info prior to the last instruction that uses them,
    // these actually live into the helper call.
    gcInfo.gcMarkRegSetNpt(RBM_WRITE_BARRIER_SRC_BYREF | RBM_WRITE_BARRIER_DST_BYREF);
}

// generate code do a switch statement based on a table of ip-relative offsets
void CodeGen::genTableBasedSwitch(GenTree* treeNode)
{
    genConsumeOperands(treeNode->AsOp());
    regNumber idxReg  = treeNode->AsOp()->gtOp1->GetRegNum();
    regNumber baseReg = treeNode->AsOp()->gtOp2->GetRegNum();

    regNumber tmpReg = treeNode->GetSingleTempReg();

    // load the ip-relative offset (which is relative to start of fgFirstBB)
    GetEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, baseReg, baseReg, idxReg, INS_OPTS_LSL);

    // add it to the absolute address of fgFirstBB
    GetEmitter()->emitIns_R_L(INS_adr, EA_PTRSIZE, compiler->fgFirstBB, tmpReg);
    GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, baseReg, baseReg, tmpReg);

    // br baseReg
    GetEmitter()->emitIns_R(INS_br, emitActualTypeSize(TYP_I_IMPL), baseReg);
}

// emits the table and an instruction to get the address of the first element
void CodeGen::genJumpTable(GenTree* treeNode)
{
    noway_assert(compiler->compCurBB->KindIs(BBJ_SWITCH));
    assert(treeNode->OperGet() == GT_JMPTABLE);

    unsigned     jumpCount = compiler->compCurBB->GetSwitchTargets()->bbsCount;
    BasicBlock** jumpTable = compiler->compCurBB->GetSwitchTargets()->bbsDstTab;
    unsigned     jmpTabOffs;
    unsigned     jmpTabBase;

    jmpTabBase = GetEmitter()->emitBBTableDataGenBeg(jumpCount, true);

    jmpTabOffs = 0;

    JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", compiler->compMethodID, jmpTabBase);

    for (unsigned i = 0; i < jumpCount; i++)
    {
        BasicBlock* target = *jumpTable++;
        noway_assert(target->HasFlag(BBF_HAS_LABEL));

        JITDUMP("            DD      L_M%03u_" FMT_BB "\n", compiler->compMethodID, target->bbNum);

        GetEmitter()->emitDataGenData(i, target);
    };

    GetEmitter()->emitDataGenEnd();

    // Access to inline data is 'abstracted' by a special type of static member
    // (produced by eeFindJitDataOffs) which the emitter recognizes as being a reference
    // to constant data, not a real static field.
    GetEmitter()->emitIns_R_C(INS_adr, emitActualTypeSize(TYP_I_IMPL), treeNode->GetRegNum(), REG_NA,
                              compiler->eeFindJitDataOffs(jmpTabBase), 0);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genLockedInstructions: Generate code for a GT_XADD, GT_XAND, GT_XORR or GT_XCHG node.
//
// Arguments:
//    treeNode - the GT_XADD/XAND/XORR/XCHG node
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

    assert(treeNode->OperIs(GT_XCHG) || !varTypeIsSmall(treeNode->TypeGet()));

    emitAttr dataSize = emitActualTypeSize(data);

    if (compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
    {
        assert(!data->isContainedIntOrIImmed());

        switch (treeNode->gtOper)
        {
            case GT_XORR:
                GetEmitter()->emitIns_R_R_R(INS_ldsetal, dataSize, dataReg, (targetReg == REG_NA) ? REG_ZR : targetReg,
                                            addrReg);
                break;
            case GT_XAND:
            {
                // Grab a temp reg to perform `MVN` for dataReg first.
                regNumber tempReg = treeNode->GetSingleTempReg();
                GetEmitter()->emitIns_R_R(INS_mvn, dataSize, tempReg, dataReg);
                GetEmitter()->emitIns_R_R_R(INS_ldclral, dataSize, tempReg, (targetReg == REG_NA) ? REG_ZR : targetReg,
                                            addrReg);
                break;
            }
            case GT_XCHG:
            {
                instruction ins = INS_swpal;
                if (varTypeIsByte(treeNode->TypeGet()))
                {
                    ins = INS_swpalb;
                }
                else if (varTypeIsShort(treeNode->TypeGet()))
                {
                    ins = INS_swpalh;
                }
                GetEmitter()->emitIns_R_R_R(ins, dataSize, dataReg, targetReg, addrReg);
                break;
            }
            case GT_XADD:
                GetEmitter()->emitIns_R_R_R(INS_ldaddal, dataSize, dataReg, (targetReg == REG_NA) ? REG_ZR : targetReg,
                                            addrReg);
                break;
            default:
                assert(!"Unexpected treeNode->gtOper");
        }
    }
    else
    {
        // These are imported normally if Atomics aren't supported.
        assert(!treeNode->OperIs(GT_XORR, GT_XAND));

        regNumber exResultReg  = treeNode->ExtractTempReg(RBM_ALLINT);
        regNumber storeDataReg = (treeNode->OperGet() == GT_XCHG) ? dataReg : treeNode->ExtractTempReg(RBM_ALLINT);
        regNumber loadReg      = (targetReg != REG_NA) ? targetReg : storeDataReg;

        // Check allocator assumptions
        //
        // The register allocator should have extended the lifetimes of all input and internal registers so that
        // none interfere with the target.
        noway_assert(addrReg != targetReg);

        noway_assert(addrReg != loadReg);
        noway_assert(dataReg != loadReg);

        noway_assert(addrReg != storeDataReg);
        noway_assert((treeNode->OperGet() == GT_XCHG) || (addrReg != dataReg));

        assert(addr->isUsedFromReg());
        noway_assert(exResultReg != REG_NA);
        noway_assert(exResultReg != targetReg);
        noway_assert((targetReg != REG_NA) || (treeNode->OperGet() != GT_XCHG));

        // Store exclusive unpredictable cases must be avoided
        noway_assert(exResultReg != storeDataReg);
        noway_assert(exResultReg != addrReg);

        // NOTE: `genConsumeAddress` marks the consumed register as not a GC pointer, as it assumes that the input
        // registers
        // die at the first instruction generated by the node. This is not the case for these atomics as the  input
        // registers are multiply-used. As such, we need to mark the addr register as containing a GC pointer until
        // we are finished generating the code for this node.

        gcInfo.gcMarkRegPtrVal(addrReg, addr->TypeGet());

        // Emit code like this:
        //   retry:
        //     ldxr loadReg, [addrReg]
        //     add storeDataReg, loadReg, dataReg         # Only for GT_XADD
        //                                                # GT_XCHG storeDataReg === dataReg
        //     stxr exResult, storeDataReg, [addrReg]
        //     cbnz exResult, retry
        //     dmb ish

        BasicBlock* labelRetry = genCreateTempLabel();
        genDefineTempLabel(labelRetry);

        instruction insLd = INS_ldaxr;
        instruction insSt = INS_stlxr;
        if (varTypeIsByte(treeNode->TypeGet()))
        {
            insLd = INS_ldaxrb;
            insSt = INS_stlxrb;
        }
        else if (varTypeIsShort(treeNode->TypeGet()))
        {
            insLd = INS_ldaxrh;
            insSt = INS_stlxrh;
        }

        // The following instruction includes a acquire half barrier
        GetEmitter()->emitIns_R_R(insLd, dataSize, loadReg, addrReg);

        switch (treeNode->OperGet())
        {
            case GT_XADD:
                if (data->isContainedIntOrIImmed())
                {
                    // Even though INS_add is specified here, the encoder will choose either
                    // an INS_add or an INS_sub and encode the immediate as a positive value
                    genInstrWithConstant(INS_add, dataSize, storeDataReg, loadReg, data->AsIntConCommon()->IconValue(),
                                         REG_NA);
                }
                else
                {
                    GetEmitter()->emitIns_R_R_R(INS_add, dataSize, storeDataReg, loadReg, dataReg);
                }
                break;
            case GT_XCHG:
                assert(!data->isContained());
                storeDataReg = dataReg;
                break;
            default:
                unreached();
        }

        // The following instruction includes a release half barrier
        GetEmitter()->emitIns_R_R_R(insSt, dataSize, exResultReg, storeDataReg, addrReg);

        GetEmitter()->emitIns_J_R(INS_cbnz, EA_4BYTE, labelRetry, exResultReg);

        instGen_MemoryBarrier();

        gcInfo.gcMarkRegSetNpt(addr->gtGetRegMask());
    }

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

    if (compiler->compOpportunisticallyDependsOn(InstructionSet_Atomics))
    {
        // casal use the comparand as the target reg
        GetEmitter()->emitIns_Mov(INS_mov, dataSize, targetReg, comparandReg, /* canSkip */ true);

        // Catch case we destroyed data or address before use
        noway_assert((addrReg != targetReg) || (targetReg == comparandReg));
        noway_assert((dataReg != targetReg) || (targetReg == comparandReg));

        instruction ins = INS_casal;
        if (varTypeIsByte(treeNode->TypeGet()))
        {
            ins = INS_casalb;
        }
        else if (varTypeIsShort(treeNode->TypeGet()))
        {
            ins = INS_casalh;
        }
        GetEmitter()->emitIns_R_R_R(ins, dataSize, targetReg, dataReg, addrReg);
    }
    else
    {
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
        //     ldxr targetReg, [addrReg]
        //     cmp targetReg, comparandReg
        //     bne compareFail
        //     stxr exResult, dataReg, [addrReg]
        //     cbnz exResult, retry
        //   compareFail:
        //     dmb ish

        BasicBlock* labelRetry       = genCreateTempLabel();
        BasicBlock* labelCompareFail = genCreateTempLabel();
        genDefineTempLabel(labelRetry);

        instruction insLd = INS_ldaxr;
        instruction insSt = INS_stlxr;
        if (varTypeIsByte(treeNode->TypeGet()))
        {
            insLd = INS_ldaxrb;
            insSt = INS_stlxrb;
        }
        else if (varTypeIsShort(treeNode->TypeGet()))
        {
            insLd = INS_ldaxrh;
            insSt = INS_stlxrh;
        }

        // The following instruction includes a acquire half barrier
        GetEmitter()->emitIns_R_R(insLd, dataSize, targetReg, addrReg);

        if (comparand->isContainedIntOrIImmed())
        {
            if (comparand->IsIntegralConst(0))
            {
                GetEmitter()->emitIns_J_R(INS_cbnz, emitActualTypeSize(treeNode), labelCompareFail, targetReg);
            }
            else
            {
                GetEmitter()->emitIns_R_I(INS_cmp, emitActualTypeSize(treeNode), targetReg,
                                          comparand->AsIntConCommon()->IconValue());
                GetEmitter()->emitIns_J(INS_bne, labelCompareFail);
            }
        }
        else
        {
            GetEmitter()->emitIns_R_R(INS_cmp, emitActualTypeSize(treeNode), targetReg, comparandReg);
            GetEmitter()->emitIns_J(INS_bne, labelCompareFail);
        }

        // The following instruction includes a release half barrier
        GetEmitter()->emitIns_R_R_R(insSt, dataSize, exResultReg, dataReg, addrReg);

        GetEmitter()->emitIns_J_R(INS_cbnz, EA_4BYTE, labelRetry, exResultReg);

        genDefineTempLabel(labelCompareFail);

        instGen_MemoryBarrier();

        gcInfo.gcMarkRegSetNpt(addr->gtGetRegMask());
    }

    if (varTypeIsSmall(treeNode->TypeGet()) && varTypeIsSigned(treeNode->TypeGet()))
    {
        instruction mov = varTypeIsShort(treeNode->TypeGet()) ? INS_sxth : INS_sxtb;
        GetEmitter()->emitIns_Mov(mov, EA_4BYTE, targetReg, targetReg, /* canSkip */ false);
    }

    genProduceReg(treeNode);
}

instruction CodeGen::genGetInsForOper(genTreeOps oper, var_types type)
{
    instruction ins = INS_BREAKPOINT;

    if (varTypeIsFloating(type))
    {
        switch (oper)
        {
            case GT_ADD:
                ins = INS_fadd;
                break;
            case GT_SUB:
                ins = INS_fsub;
                break;
            case GT_MUL:
                ins = INS_fmul;
                break;
            case GT_DIV:
                ins = INS_fdiv;
                break;
            case GT_NEG:
                ins = INS_fneg;
                break;

            default:
                NYI("Unhandled oper in genGetInsForOper() - float");
                unreached();
                break;
        }
    }
    else
    {
        switch (oper)
        {
            case GT_ADD:
                ins = INS_add;
                break;
            case GT_AND:
                ins = INS_and;
                break;
            case GT_AND_NOT:
                ins = INS_bic;
                break;
            case GT_DIV:
                ins = INS_sdiv;
                break;
            case GT_UDIV:
                ins = INS_udiv;
                break;
            case GT_MUL:
                ins = INS_mul;
                break;
            case GT_LSH:
                ins = INS_lsl;
                break;
            case GT_NEG:
                ins = INS_neg;
                break;
            case GT_NOT:
                ins = INS_mvn;
                break;
            case GT_OR:
                ins = INS_orr;
                break;
            case GT_ROR:
                ins = INS_ror;
                break;
            case GT_RSH:
                ins = INS_asr;
                break;
            case GT_RSZ:
                ins = INS_lsr;
                break;
            case GT_SUB:
                ins = INS_sub;
                break;
            case GT_XOR:
                ins = INS_eor;
                break;

            default:
                NYI("Unhandled oper in genGetInsForOper() - integer");
                unreached();
                break;
        }
    }
    return ins;
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
    genConsumeRegs(data);
    GetEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, data->GetRegNum(), 0);

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
#ifdef FEATURE_SIMD
    // Storing Vector3 of size 12 bytes through indirection
    if (tree->TypeGet() == TYP_SIMD12)
    {
        genStoreIndTypeSimd12(tree);
        return;
    }
#endif // FEATURE_SIMD

    GenTree* data = tree->Data();
    GenTree* addr = tree->Addr();

    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(tree);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        // data and addr must be in registers.
        // Consume both registers so that any copies of interfering
        // registers are taken care of.
        genConsumeOperands(tree);

        // At this point, we should not have any interference.
        // That is, 'data' must not be in REG_WRITE_BARRIER_DST,
        //  as that is where 'addr' must go.
        noway_assert(data->GetRegNum() != REG_WRITE_BARRIER_DST);

        // 'addr' goes into x14 (REG_WRITE_BARRIER_DST)
        genCopyRegIfNeeded(addr, REG_WRITE_BARRIER_DST);

        // 'data' goes into x15 (REG_WRITE_BARRIER_SRC)
        genCopyRegIfNeeded(data, REG_WRITE_BARRIER_SRC);

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

        regNumber dataReg;
        if (data->isContainedIntOrIImmed())
        {
            assert(data->IsIntegralConst(0));
            dataReg = REG_ZR;
        }
        else // data is not contained, so evaluate it into a register
        {
            assert(!data->isContained());
            dataReg = data->GetRegNum();
        }

        var_types   type = tree->TypeGet();
        instruction ins  = ins_StoreFromSrc(dataReg, type);

        if (tree->IsVolatile())
        {
            bool needsBarrier = true;
            ins               = genGetVolatileLdStIns(ins, dataReg, tree, &needsBarrier);

            if (needsBarrier)
            {
                // issue a full memory barrier before a volatile StInd
                // Note: We cannot issue store barrier ishst because it is a weaker barrier.
                // The loads can get rearranged around the barrier causing to read wrong
                // value.
                instGen_MemoryBarrier();
            }
        }

        GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), dataReg, tree);

        // If store was to a variable, update variable liveness after instruction was emitted.
        genUpdateLife(tree);
    }
}

//------------------------------------------------------------------------
// genCodeForSwap: Produce code for a GT_SWAP node.
//
// Arguments:
//    tree - the GT_SWAP node
//
void CodeGen::genCodeForSwap(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_SWAP));

    // Swap is only supported for lclVar operands that are enregistered
    // We do not consume or produce any registers.  Both operands remain enregistered.
    // However, the gc-ness may change.
    assert(genIsRegCandidateLocal(tree->gtOp1) && genIsRegCandidateLocal(tree->gtOp2));

    GenTreeLclVarCommon* lcl1    = tree->gtOp1->AsLclVarCommon();
    LclVarDsc*           varDsc1 = compiler->lvaGetDesc(lcl1);
    var_types            type1   = varDsc1->TypeGet();
    GenTreeLclVarCommon* lcl2    = tree->gtOp2->AsLclVarCommon();
    LclVarDsc*           varDsc2 = compiler->lvaGetDesc(lcl2);
    var_types            type2   = varDsc2->TypeGet();

    // We must have both int or both fp regs
    assert(!varTypeIsFloating(type1) || varTypeIsFloating(type2));

    // FP swap is not yet implemented (and should have NYI'd in LSRA)
    assert(!varTypeIsFloating(type1));

    regNumber oldOp1Reg     = lcl1->GetRegNum();
    regMaskTP oldOp1RegMask = genRegMask(oldOp1Reg);
    regNumber oldOp2Reg     = lcl2->GetRegNum();
    regMaskTP oldOp2RegMask = genRegMask(oldOp2Reg);

    // We don't call genUpdateVarReg because we don't have a tree node with the new register.
    varDsc1->SetRegNum(oldOp2Reg);
    varDsc2->SetRegNum(oldOp1Reg);

    // Do the xchg
    emitAttr size = EA_PTRSIZE;
    if (varTypeIsGC(type1) != varTypeIsGC(type2))
    {
        // If the type specified to the emitter is a GC type, it will swap the GC-ness of the registers.
        // Otherwise it will leave them alone, which is correct if they have the same GC-ness.
        size = EA_GCREF;
    }

    NYI("register swap");
    // inst_RV_RV(INS_xchg, oldOp1Reg, oldOp2Reg, TYP_I_IMPL, size);

    // Update the gcInfo.
    // Manually remove these regs for the gc sets (mostly to avoid confusing duplicative dump output)
    gcInfo.gcRegByrefSetCur &= ~(oldOp1RegMask | oldOp2RegMask);
    gcInfo.gcRegGCrefSetCur &= ~(oldOp1RegMask | oldOp2RegMask);

    // gcMarkRegPtrVal will do the appropriate thing for non-gc types.
    // It will also dump the updates.
    gcInfo.gcMarkRegPtrVal(oldOp2Reg, type1);
    gcInfo.gcMarkRegPtrVal(oldOp1Reg, type2);
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
    // int type --> float/double conversions are always non-overflow ones
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

    // We should never see a srcType whose size is neither EA_4BYTE or EA_8BYTE
    emitAttr srcSize = EA_ATTR(genTypeSize(srcType));
    noway_assert((srcSize == EA_4BYTE) || (srcSize == EA_8BYTE));

    instruction ins       = varTypeIsUnsigned(srcType) ? INS_ucvtf : INS_scvtf;
    insOpts     cvtOption = INS_OPTS_NONE; // invalid value

    if (dstType == TYP_DOUBLE)
    {
        if (srcSize == EA_4BYTE)
        {
            cvtOption = INS_OPTS_4BYTE_TO_D;
        }
        else
        {
            assert(srcSize == EA_8BYTE);
            cvtOption = INS_OPTS_8BYTE_TO_D;
        }
    }
    else
    {
        assert(dstType == TYP_FLOAT);
        if (srcSize == EA_4BYTE)
        {
            cvtOption = INS_OPTS_4BYTE_TO_S;
        }
        else
        {
            assert(srcSize == EA_8BYTE);
            cvtOption = INS_OPTS_8BYTE_TO_S;
        }
    }

    genConsumeOperands(treeNode->AsOp());

    GetEmitter()->emitIns_R_R(ins, emitActualTypeSize(dstType), treeNode->GetRegNum(), op1->GetRegNum(), cvtOption);

    genProduceReg(treeNode);
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

    // We should never see a dstType whose size is neither EA_4BYTE or EA_8BYTE
    // For conversions to small types (byte/sbyte/int16/uint16) from float/double,
    // we expect the front-end or lowering phase to have generated two levels of cast.
    //
    emitAttr dstSize = EA_ATTR(genTypeSize(dstType));
    noway_assert((dstSize == EA_4BYTE) || (dstSize == EA_8BYTE));

    instruction ins       = INS_fcvtzs;    // default to sign converts
    insOpts     cvtOption = INS_OPTS_NONE; // invalid value

    if (varTypeIsUnsigned(dstType))
    {
        ins = INS_fcvtzu; // use unsigned converts
    }

    if (srcType == TYP_DOUBLE)
    {
        if (dstSize == EA_4BYTE)
        {
            cvtOption = INS_OPTS_D_TO_4BYTE;
        }
        else
        {
            assert(dstSize == EA_8BYTE);
            cvtOption = INS_OPTS_D_TO_8BYTE;
        }
    }
    else
    {
        assert(srcType == TYP_FLOAT);
        if (dstSize == EA_4BYTE)
        {
            cvtOption = INS_OPTS_S_TO_4BYTE;
        }
        else
        {
            assert(dstSize == EA_8BYTE);
            cvtOption = INS_OPTS_S_TO_8BYTE;
        }
    }

    genConsumeOperands(treeNode->AsOp());

    GetEmitter()->emitIns_R_R(ins, dstSize, treeNode->GetRegNum(), op1->GetRegNum(), cvtOption);

    genProduceReg(treeNode);
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

    GenTree*  op1         = treeNode->AsOp()->gtOp1;
    var_types targetType  = treeNode->TypeGet();
    int       expMask     = (targetType == TYP_FLOAT) ? 0x7F8 : 0x7FF; // Bit mask to extract exponent.
    int       shiftAmount = targetType == TYP_FLOAT ? 20 : 52;

    emitter* emit = GetEmitter();

    // Extract exponent into a register.
    regNumber intReg = treeNode->GetSingleTempReg();
    regNumber fpReg  = genConsumeReg(op1);

    inst_Mov(targetType, intReg, fpReg, /* canSkip */ false, emitActualTypeSize(treeNode));
    emit->emitIns_R_R_I(INS_lsr, emitActualTypeSize(targetType), intReg, intReg, shiftAmount);

    // Mask of exponent with all 1's and check if the exponent is all 1's
    emit->emitIns_R_R_I(INS_and, EA_4BYTE, intReg, intReg, expMask);
    emit->emitIns_R_I(INS_cmp, EA_4BYTE, intReg, expMask);

    // If exponent is all 1's, throw ArithmeticException
    genJumpToThrowHlpBlk(EJ_eq, SCK_ARITH_EXCPN);

    // if it is a finite value copy it to targetReg
    inst_Mov(targetType, treeNode->GetRegNum(), fpReg, /* canSkip */ true);

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_EQ/GT_NE/GT_LT/GT_LE/GT_GE/GT_GT/GT_TEST_EQ/GT_TEST_NE node.
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

    assert(!op1->isUsedFromMemory());

    emitAttr cmpSize = EA_ATTR(genTypeSize(op1Type));

    assert(genTypeSize(op1Type) == genTypeSize(op2Type));

    if (varTypeIsFloating(op1Type))
    {
        assert(varTypeIsFloating(op2Type));
        assert(!op1->isContained());
        assert(op1Type == op2Type);

        if (op2->IsFloatPositiveZero())
        {
            assert(op2->isContained());
            emit->emitIns_R_F(INS_fcmp, cmpSize, op1->GetRegNum(), 0.0);
        }
        else
        {
            assert(!op2->isContained());
            emit->emitIns_R_R(INS_fcmp, cmpSize, op1->GetRegNum(), op2->GetRegNum());
        }
    }
    else
    {
        assert(!varTypeIsFloating(op2Type));
        // We don't support swapping op1 and op2 to generate cmp reg, imm
        assert(!op1->isContainedIntOrIImmed());

        instruction ins = tree->OperIs(GT_TEST_EQ, GT_TEST_NE, GT_TEST) ? INS_tst : INS_cmp;

        if (op2->isContainedIntOrIImmed())
        {
            GenTreeIntConCommon* intConst = op2->AsIntConCommon();

            regNumber op1Reg = op1->GetRegNum();

            if (compiler->opts.OptimizationEnabled() && (ins == INS_cmp) && (targetReg != REG_NA) &&
                tree->OperIs(GT_LT) && !tree->IsUnsigned() && intConst->IsIntegralConst(0) &&
                ((cmpSize == EA_4BYTE) || (cmpSize == EA_8BYTE)))
            {
                emit->emitIns_R_R_I(INS_lsr, cmpSize, targetReg, op1Reg, (int)cmpSize * 8 - 1);
                genProduceReg(tree);
                return;
            }

            emit->emitIns_R_I(ins, cmpSize, op1Reg, intConst->IconValue());
        }
        else if (op2->isContained())
        {
            genTreeOps oper = op2->OperGet();
            switch (oper)
            {
                case GT_NEG:
                    assert(ins == INS_cmp);

                    ins  = INS_cmn;
                    oper = op2->gtGetOp1()->OperGet();
                    if (op2->gtGetOp1()->isContained())
                    {
                        switch (oper)
                        {
                            case GT_LSH:
                            case GT_RSH:
                            case GT_RSZ:
                            {
                                GenTree* shiftOp1 = op2->gtGetOp1()->gtGetOp1();
                                GenTree* shiftOp2 = op2->gtGetOp1()->gtGetOp2();

                                assert(shiftOp2->IsCnsIntOrI());
                                assert(shiftOp2->isContained());

                                emit->emitIns_R_R_I(ins, cmpSize, op1->GetRegNum(), shiftOp1->GetRegNum(),
                                                    shiftOp2->AsIntConCommon()->IntegralValue(),
                                                    ShiftOpToInsOpts(oper));
                            }
                            break;

                            default:
                                unreached();
                        }
                    }
                    else
                    {
                        emit->emitIns_R_R(ins, cmpSize, op1->GetRegNum(), op2->gtGetOp1()->GetRegNum());
                    }
                    break;

                case GT_LSH:
                case GT_RSH:
                case GT_RSZ:
                    assert(op2->gtGetOp2()->IsCnsIntOrI());
                    assert(op2->gtGetOp2()->isContained());

                    emit->emitIns_R_R_I(ins, cmpSize, op1->GetRegNum(), op2->gtGetOp1()->GetRegNum(),
                                        op2->gtGetOp2()->AsIntConCommon()->IntegralValue(), ShiftOpToInsOpts(oper));
                    break;

                default:
                    unreached();
            }
        }
        else
        {
            emit->emitIns_R_R(ins, cmpSize, op1->GetRegNum(), op2->GetRegNum());
        }
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
    GetEmitter()->emitIns_J_R(INS_cbnz, emitActualTypeSize(op), compiler->compCurBB->GetTrueTarget(), reg);
}

//------------------------------------------------------------------------
// genCodeForConditionalCompare: Produce code for a compare that's dependent on a previous compare.
//
// Arguments:
//    tree - a compare node (GT_EQ etc)
//    cond - the condition of the previous generated compare.
//
void CodeGen::genCodeForCCMP(GenTreeCCMP* ccmp)
{
    emitter* emit = GetEmitter();

    genConsumeOperands(ccmp);
    GenTree*  op1     = ccmp->gtGetOp1();
    GenTree*  op2     = ccmp->gtGetOp2();
    var_types op1Type = genActualType(op1->TypeGet());
    var_types op2Type = genActualType(op2->TypeGet());
    emitAttr  cmpSize = emitActualTypeSize(op1Type);
    regNumber srcReg1 = op1->GetRegNum();

    // No float support or swapping op1 and op2 to generate cmp reg, imm.
    assert(!varTypeIsFloating(op2Type));
    assert(!op1->isContainedIntOrIImmed());

    // For the ccmp flags, invert the condition of the compare.
    // For the condition, use the previous compare.
    const GenConditionDesc& condDesc = GenConditionDesc::Get(ccmp->gtCondition);
    insCond                 insCond  = JumpKindToInsCond(condDesc.jumpKind1);

    if (op2->isContainedIntOrIImmed())
    {
        GenTreeIntConCommon* intConst = op2->AsIntConCommon();
        emit->emitIns_R_I_FLAGS_COND(INS_ccmp, cmpSize, srcReg1, (int)intConst->IconValue(), ccmp->gtFlagsVal, insCond);
    }
    else
    {
        regNumber srcReg2 = op2->GetRegNum();
        emit->emitIns_R_R_FLAGS_COND(INS_ccmp, cmpSize, srcReg1, srcReg2, ccmp->gtFlagsVal, insCond);
    }
}

//------------------------------------------------------------------------
// genCodeForSelect: Produce code for a GT_SELECT/GT_SELECT_INV/GT_SELECT_NEG node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForSelect(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_SELECT, GT_SELECTCC, GT_SELECT_INC, GT_SELECT_INCCC, GT_SELECT_INV, GT_SELECT_INVCC,
                        GT_SELECT_NEG, GT_SELECT_NEGCC));
    GenTree*    opcond = nullptr;
    instruction ins    = INS_csel;
    GenTree*    op1    = tree->gtOp1;
    GenTree*    op2    = tree->gtOp2;

    if (tree->OperIs(GT_SELECT_INV, GT_SELECT_INVCC))
    {
        ins = (op2 == nullptr) ? INS_cinv : INS_csinv;
    }
    else if (tree->OperIs(GT_SELECT_NEG, GT_SELECT_NEGCC))
    {
        ins = (op2 == nullptr) ? INS_cneg : INS_csneg;
    }
    else if (tree->OperIs(GT_SELECT_INC, GT_SELECT_INCCC))
    {
        ins = (op2 == nullptr) ? INS_cinc : INS_csinc;
    }

    if (tree->OperIs(GT_SELECT, GT_SELECT_INV, GT_SELECT_NEG))
    {
        opcond = tree->AsConditional()->gtCond;
        genConsumeRegs(opcond);
    }

    if (op2 != nullptr)
    {
        var_types op1Type = genActualType(op1);
        var_types op2Type = genActualType(op2);
        assert(genTypeSize(op1Type) == genTypeSize(op2Type));
    }

    assert(!op1->isUsedFromMemory());

    emitter*     emit = GetEmitter();
    GenCondition cond;

    if (opcond != nullptr)
    {
        // Condition has been generated into a register - move it into flags.
        emit->emitIns_R_I(INS_cmp, emitActualTypeSize(opcond), opcond->GetRegNum(), 0);
        cond = GenCondition::NE;
    }
    else
    {
        assert(tree->OperIs(GT_SELECTCC, GT_SELECT_INCCC, GT_SELECT_INVCC, GT_SELECT_NEGCC));
        cond = tree->AsOpCC()->gtCondition;
    }

    assert(!op1->isContained() || op1->IsIntegralConst(0));
    assert(op2 == nullptr || !op2->isContained() || op2->IsIntegralConst(0));

    regNumber               targetReg = tree->GetRegNum();
    regNumber               srcReg1   = op1->IsIntegralConst(0) ? REG_ZR : genConsumeReg(op1);
    const GenConditionDesc& prevDesc  = GenConditionDesc::Get(cond);
    emitAttr                attr      = emitActualTypeSize(tree);
    regNumber               srcReg2;

    if (op2 == nullptr)
    {
        srcReg2 = srcReg1;
        emit->emitIns_R_R_COND(ins, attr, targetReg, srcReg1, JumpKindToInsCond(prevDesc.jumpKind1));
    }
    else
    {
        srcReg2 = (op2->IsIntegralConst(0) ? REG_ZR : genConsumeReg(op2));
        emit->emitIns_R_R_R_COND(ins, attr, targetReg, srcReg1, srcReg2, JumpKindToInsCond(prevDesc.jumpKind1));
    }

    // Some floating point comparision conditions require an additional condition check.
    // These checks are emitted as a subsequent check using GT_AND or GT_OR nodes.
    // e.g., using  GT_OR   => `dest = (cond1 || cond2) ? src1 : src2`
    //              GT_AND  => `dest = (cond1 && cond2) ? src1 : src2`
    // The GT_OR case results in emitting the following sequence of two csel instructions.
    // csel  dest, src1, src2, cond1    # emitted previously
    // csel  dest, src1, dest, cond2
    //
    if (prevDesc.oper == GT_AND)
    {
        // To ensure correctness with invert and negate variants of conditional select, the second instruction needs to
        // be csinv or csneg respectively.
        // dest = (cond1 && cond2) ? src1 : ~src2
        // csinv  dest, src1, src2, cond1
        // csinv  dest, dest, src2, cond2
        //
        // However, the other variants - increment and select, the second instruction needs to be csel.
        // dest = (cond1 && cond2) ? src1 : src2++
        // csinc  dest, src1, src2, cond1
        // csel  dest, dest, src1 cond2
        ins = ((ins == INS_csinv) || (ins == INS_csneg)) ? ins : INS_csel;
        emit->emitIns_R_R_R_COND(ins, attr, targetReg, targetReg, srcReg2, JumpKindToInsCond(prevDesc.jumpKind2));
    }
    else if (prevDesc.oper == GT_OR)
    {
        // Similarly, the second instruction needs to be csinc while emitting conditional increment.
        ins = (ins == INS_csinc) ? ins : INS_csel;
        emit->emitIns_R_R_R_COND(ins, attr, targetReg, srcReg1, targetReg, JumpKindToInsCond(prevDesc.jumpKind2));
    }

    regSet.verifyRegUsed(targetReg);
    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForJumpCompare: Generates code for a GT_JCMP or GT_JTEST statement.
//
// A GT_JCMP/GT_JTEST node is created when a comparison and conditional branch
// can be executed in a single instruction.
//
// Arm64 has a few instructions with this behavior.
//   - cbz/cbnz -- Compare and branch register zero/not zero
//   - tbz/tbnz -- Test and branch register bit zero/not zero
//
// The cbz/cbnz supports the normal +/- 1MB branch range for conditional branches
// The tbz/tbnz supports a  smaller +/- 32KB branch range
//
// A GT_JCMP cbz/cbnz node is created when there is a GT_EQ or GT_NE
// integer/unsigned comparison against #0 which is used by a GT_JTRUE
// condition jump node.
//
// A GT_JCMP tbz/tbnz node is created when there is a GT_TEST_EQ or GT_TEST_NE
// integer/unsigned comparison against against a mask with a single bit set
// which is used by a GT_JTRUE condition jump node.
//
// This node is responsible for consuming the register, and emitting the
// appropriate fused compare/test and branch instruction
//
// Arguments:
//    tree - The GT_JCMP/GT_JTEST tree node.
//
// Return Value:
//    None
//
void CodeGen::genCodeForJumpCompare(GenTreeOpCC* tree)
{
    assert(compiler->compCurBB->KindIs(BBJ_COND));

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    assert(tree->OperIs(GT_JCMP, GT_JTEST));
    assert(!varTypeIsFloating(tree));
    assert(!op1->isUsedFromMemory());
    assert(!op2->isUsedFromMemory());
    assert(op2->IsCnsIntOrI());
    assert(op2->isContained());

    GenCondition cc = tree->gtCondition;

    // For ARM64 we only expect equality comparisons.
    assert((cc.GetCode() == GenCondition::EQ) || (cc.GetCode() == GenCondition::NE));

    genConsumeOperands(tree);

    regNumber reg  = op1->GetRegNum();
    emitAttr  attr = emitActualTypeSize(op1->TypeGet());

    if (tree->OperIs(GT_JTEST))
    {
        ssize_t compareImm = op2->AsIntCon()->IconValue();

        assert(isPow2(((size_t)compareImm)));

        instruction ins = (cc.GetCode() == GenCondition::EQ) ? INS_tbz : INS_tbnz;
        int         imm = genLog2((size_t)compareImm);

        GetEmitter()->emitIns_J_R_I(ins, attr, compiler->compCurBB->GetTrueTarget(), reg, imm);
    }
    else
    {
        assert(op2->IsIntegralConst(0));

        instruction ins = (cc.GetCode() == GenCondition::EQ) ? INS_cbz : INS_cbnz;

        GetEmitter()->emitIns_J_R(ins, attr, compiler->compCurBB->GetTrueTarget(), reg);
    }
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return offset from the stack pointer (Initial-SP) to the frame pointer. The frame pointer
// will point to the saved frame pointer slot (i.e., there will be frame pointer chaining).
//
int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    int delta = -1; // initialization to illegal value

    if (IsSaveFpLrWithAllCalleeSavedRegisters())
    {
        // The saved frame pointer is at the top of the frame, just beneath the saved varargs register space and the
        // saved LR.
        delta = genTotalFrameSize() - (compiler->info.compIsVarArgs ? MAX_REG_ARG * REGSIZE_BYTES : 0) -
                2 /* FP, LR */ * REGSIZE_BYTES;
    }
    else
    {
        // We place the saved frame pointer immediately above the outgoing argument space.
        delta = (int)compiler->lvaOutgoingArgSpaceSize;
    }

    assert(delta >= 0);
    return delta;
}

//---------------------------------------------------------------------
// genTotalFrameSize - return the total size of the stack frame, including local size,
// callee-saved register size, etc.
//
// Return value:
//    Total frame size
//

int CodeGenInterface::genTotalFrameSize() const
{
    // For varargs functions, we home all the incoming register arguments. They are not
    // included in the compCalleeRegsPushed count. This is like prespill on ARM32, but
    // since we don't use "push" instructions to save them, we don't have to do the
    // save of these varargs register arguments as the first thing in the prolog.

    assert(!IsUninitialized(compiler->compCalleeRegsPushed));

    int totalFrameSize = (compiler->info.compIsVarArgs ? MAX_REG_ARG * REGSIZE_BYTES : 0) +
                         compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize;

    assert(totalFrameSize >= 0);
    return totalFrameSize;
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!

int CodeGenInterface::genCallerSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    int callerSPtoFPdelta;

    callerSPtoFPdelta = genCallerSPtoInitialSPdelta() + genSPtoFPdelta();

    assert(callerSPtoFPdelta <= 0);
    return callerSPtoFPdelta;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.

int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    int callerSPtoSPdelta = 0;

    callerSPtoSPdelta -= genTotalFrameSize();

    assert(callerSPtoSPdelta <= 0);
    return callerSPtoSPdelta;
}

//---------------------------------------------------------------------
// SetSaveFpLrWithAllCalleeSavedRegisters - Set the variable that indicates if FP/LR registers
// are stored with the rest of the callee-saved registers.
//
void CodeGen::SetSaveFpLrWithAllCalleeSavedRegisters(bool value)
{
    JITDUMP("Setting genSaveFpLrWithAllCalleeSavedRegisters to %s\n", dspBool(value));
    genSaveFpLrWithAllCalleeSavedRegisters = value;

    if (genSaveFpLrWithAllCalleeSavedRegisters)
    {
        // We'll use frame type 4 or 5. Frame type 5 only occurs if there is a very large outgoing argument
        // space. This is extremely rare, so under stress force using this frame type. However, frame type 5
        // isn't used if there is no outgoing argument space; this is checked elsewhere.

        if ((compiler->opts.compJitSaveFpLrWithCalleeSavedRegisters == 3) ||
            compiler->compStressCompile(Compiler::STRESS_GENERIC_VARN, 50))
        {
            genForceFuncletFrameType5 = true;
        }
    }
}

//---------------------------------------------------------------------
// IsSaveFpLrWithAllCalleeSavedRegisters - Return the value that indicates where FP/LR registers
// are stored in the prolog.
//
bool CodeGen::IsSaveFpLrWithAllCalleeSavedRegisters() const
{
    return genSaveFpLrWithAllCalleeSavedRegisters;
}

/*****************************************************************************
 *  Emit a call to a helper function.
 *
 */

void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTargetReg /*= REG_NA */)
{
    void* addr  = nullptr;
    void* pAddr = nullptr;

    emitter::EmitCallType callType = emitter::EC_FUNC_TOKEN;
    addr                           = compiler->compGetHelperFtn((CorInfoHelpFunc)helper, &pAddr);
    regNumber callTarget           = REG_NA;

    if (addr == nullptr)
    {
        // This is call to a runtime helper.
        // adrp x, [reloc:rel page addr]
        // add x, x, [reloc:page offset]
        // ldr x, [x]
        // br x

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

        // adrp + add with relocations will be emitted
        GetEmitter()->emitIns_R_AI(INS_adrp, EA_PTR_DSP_RELOC, callTarget,
                                   (ssize_t)pAddr DEBUGARG((size_t)compiler->eeFindHelper(helper))
                                       DEBUGARG(GTF_ICON_METHOD_HDL));
        GetEmitter()->emitIns_R_R(INS_ldr, EA_PTRSIZE, callTarget, callTarget);
        callType = emitter::EC_INDIR_R;
    }

    GetEmitter()->emitIns_Call(callType, compiler->eeFindHelper(helper), INDEBUG_LDISASM_COMMA(nullptr) addr, argSize,
                               retSize, EA_UNKNOWN, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, DebugInfo(), callTarget, /* ireg */
                               REG_NA, 0, 0,                                     /* xreg, xmul, disp */
                               false                                             /* isJump */
                               );

    regMaskTP killMask = compiler->compHelperCallKillSet((CorInfoHelpFunc)helper);
    regSet.verifyRegistersUsed(killMask);
}

#ifdef FEATURE_SIMD
insOpts CodeGen::genGetSimdInsOpt(emitAttr size, var_types elementType)
{
    assert((size == EA_16BYTE) || (size == EA_8BYTE));
    insOpts result = INS_OPTS_NONE;

    switch (elementType)
    {
        case TYP_DOUBLE:
        case TYP_ULONG:
        case TYP_LONG:
            result = (size == EA_16BYTE) ? INS_OPTS_2D : INS_OPTS_1D;
            break;
        case TYP_FLOAT:
        case TYP_UINT:
        case TYP_INT:
            result = (size == EA_16BYTE) ? INS_OPTS_4S : INS_OPTS_2S;
            break;
        case TYP_USHORT:
        case TYP_SHORT:
            result = (size == EA_16BYTE) ? INS_OPTS_8H : INS_OPTS_4H;
            break;
        case TYP_UBYTE:
        case TYP_BYTE:
            result = (size == EA_16BYTE) ? INS_OPTS_16B : INS_OPTS_8B;
            break;
        default:
            assert(!"Unsupported element type");
            unreached();
    }

    return result;
}

//-----------------------------------------------------------------------------
// genSimdUpperSave: save the upper half of a TYP_SIMD16 vector to
//                   the given register, if any, or to memory.
//
// Arguments:
//    node - The GT_INTRINSIC node
//
// Return Value:
//    None.
//
// Notes:
//    The upper half of all SIMD registers are volatile, even the callee-save registers.
//    When a 16-byte SIMD value is live across a call, the register allocator will use this intrinsic
//    to cause the upper half to be saved.  It will first attempt to find another, unused, callee-save
//    register.  If such a register cannot be found, it will save it to an available caller-save register.
//    In that case, this node will be marked GTF_SPILL, which will cause this method to save
//    the upper half to the lclVar's home location.
//
void CodeGen::genSimdUpperSave(GenTreeIntrinsic* node)
{
    assert(node->gtIntrinsicName == NI_SIMD_UpperSave);

    GenTree* op1 = node->gtGetOp1();
    assert(op1->IsLocal());

    GenTreeLclVar* lclNode = op1->AsLclVar();
    LclVarDsc*     varDsc  = compiler->lvaGetDesc(lclNode);
    assert(emitTypeSize(varDsc->GetRegisterType(lclNode)) == 16);

    regNumber tgtReg = node->GetRegNum();
    assert(tgtReg != REG_NA);

    regNumber op1Reg = genConsumeReg(op1);
    assert(op1Reg != REG_NA);

    GetEmitter()->emitIns_R_R_I_I(INS_mov, EA_8BYTE, tgtReg, op1Reg, 0, 1);

    if ((node->gtFlags & GTF_SPILL) != 0)
    {
        // This is not a normal spill; we'll spill it to the lclVar location.
        // The localVar must have a stack home.

        unsigned varNum = lclNode->GetLclNum();
        assert(varDsc->lvOnFrame);

        // We want to store this to the upper 8 bytes of this localVar's home.
        int offset = 8;

        emitAttr attr = emitTypeSize(TYP_SIMD8);
        GetEmitter()->emitIns_S_R(INS_str, attr, tgtReg, varNum, offset);
    }
    else
    {
        genProduceReg(node);
    }
}

//-----------------------------------------------------------------------------
// genSimdUpperRestore: Restore the upper half of a TYP_SIMD16 vector to
//                      the given register, if any, or to memory.
//
// Arguments:
//    node - The GT_INTRINSIC node
//
// Return Value:
//    None.
//
// Notes:
//    For consistency with genSimdUpperSave, and to ensure that lclVar nodes always
//    have their home register, this node has its tgtReg on the lclVar child, and its source
//    on the node.
//    Regarding spill, please see the note above on genSimdUpperSave.  If we have spilled
//    an upper-half to the lclVar's home location, this node will be marked GTF_SPILLED.
//
void CodeGen::genSimdUpperRestore(GenTreeIntrinsic* node)
{
    assert(node->gtIntrinsicName == NI_SIMD_UpperRestore);

    GenTree* op1 = node->gtGetOp1();
    assert(op1->IsLocal());

    GenTreeLclVar* lclNode = op1->AsLclVar();
    LclVarDsc*     varDsc  = compiler->lvaGetDesc(lclNode);
    assert(emitTypeSize(varDsc->GetRegisterType(lclNode)) == 16);

    regNumber srcReg = node->GetRegNum();
    assert(srcReg != REG_NA);

    regNumber lclVarReg = genConsumeReg(lclNode);
    assert(lclVarReg != REG_NA);

    unsigned varNum = lclNode->GetLclNum();

    if (node->gtFlags & GTF_SPILLED)
    {
        // The localVar must have a stack home.
        assert(varDsc->lvOnFrame);

        // We will load this from the upper 8 bytes of this localVar's home.
        int offset = 8;

        emitAttr attr = emitTypeSize(TYP_SIMD8);
        GetEmitter()->emitIns_R_S(INS_ldr, attr, srcReg, varNum, offset);
    }

    GetEmitter()->emitIns_R_R_I_I(INS_mov, EA_8BYTE, lclVarReg, srcReg, 1, 0);
}

//-----------------------------------------------------------------------------
// genStoreIndTypeSimd12: store indirect a TYP_SIMD12 (i.e. Vector3) to memory.
// Since Vector3 is not a hardware supported write size, it is performed
// as two writes: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store indirect
//
//
// Return Value:
//    None.
//
void CodeGen::genStoreIndTypeSimd12(GenTreeStoreInd* treeNode)
{
    assert(treeNode->OperIs(GT_STOREIND));

    // Should not require a write barrier
    assert(gcInfo.gcIsWriteBarrierCandidate(treeNode) == GCInfo::WBF_NoBarrier);

    // addr and data should not be contained.

    GenTree* addr = treeNode->Addr();
    assert(!addr->isContained());

    GenTree* data = treeNode->Data();
    assert(!data->isContained());

    regNumber addrReg = genConsumeReg(addr);
    regNumber dataReg = genConsumeReg(data);

    // Need an additional integer register to extract upper 4 bytes from data.
    regNumber tmpReg = treeNode->GetSingleTempReg();

    // 8-byte write
    GetEmitter()->emitIns_R_R(INS_str, EA_8BYTE, dataReg, addrReg);

    // Extract upper 4-bytes from data
    GetEmitter()->emitIns_R_R_I(INS_mov, EA_4BYTE, tmpReg, dataReg, 2);

    // 4-byte write
    GetEmitter()->emitIns_R_R_I(INS_str, EA_4BYTE, tmpReg, addrReg, 8);
}

//-----------------------------------------------------------------------------
// genLoadIndTypeSimd12: load indirect a TYP_SIMD12 (i.e. Vector3) value.
// Since Vector3 is not a hardware supported write size, it is performed
// as two loads: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node of GT_IND
//
//
// Return Value:
//    None.
//
void CodeGen::genLoadIndTypeSimd12(GenTreeIndir* treeNode)
{
    assert(treeNode->OperIs(GT_IND));

    GenTree* addr = treeNode->Addr();
    assert(!addr->isContained());

    regNumber tgtReg  = treeNode->GetRegNum();
    regNumber addrReg = genConsumeReg(addr);

    // Need an additional int register to read upper 4 bytes, which is different from targetReg
    regNumber tmpReg = treeNode->GetSingleTempReg();

    // 8-byte read
    GetEmitter()->emitIns_R_R(INS_ldr, EA_8BYTE, tgtReg, addrReg);

    // 4-byte read
    GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, addrReg, 8);

    // Insert upper 4-bytes into data
    GetEmitter()->emitIns_R_R_I(INS_mov, EA_4BYTE, tgtReg, tmpReg, 2);

    genProduceReg(treeNode);
}

//-----------------------------------------------------------------------------
// genStoreLclTypeSimd12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genStoreLclTypeSimd12(GenTreeLclVarCommon* treeNode)
{
    assert(treeNode->OperIs(GT_STORE_LCL_FLD, GT_STORE_LCL_VAR));

    unsigned   offs   = treeNode->GetLclOffs();
    unsigned   varNum = treeNode->GetLclNum();
    LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
    assert(varNum < compiler->lvaCount);

    GenTree* data = treeNode->gtGetOp1();

    if (data->isContained())
    {
        // This is only possible for a zero-init.
        assert(data->IsIntegralConst(0) || data->IsVectorZero());

        // store lower 8 bytes
        GetEmitter()->emitIns_S_R(ins_Store(TYP_DOUBLE), EA_8BYTE, REG_ZR, varNum, offs);

        // Store upper 4 bytes
        GetEmitter()->emitIns_S_R(ins_Store(TYP_FLOAT), EA_4BYTE, REG_ZR, varNum, offs + 8);

        // Update life after instruction emitted
        genUpdateLife(treeNode);
        varDsc->SetRegNum(REG_STK);

        return;
    }

    regNumber tgtReg  = treeNode->GetRegNum();
    regNumber dataReg = genConsumeReg(data);

    if (tgtReg != REG_NA)
    {
        // Simply use mov if we move a SIMD12 reg to another SIMD12 reg
        assert(GetEmitter()->isVectorRegister(tgtReg));
        inst_Mov(treeNode->TypeGet(), tgtReg, dataReg, /* canSkip */ true);
    }
    else
    {
        GetEmitter()->emitStoreSimd12ToLclOffset(varNum, offs, dataReg, treeNode);
    }
    genUpdateLifeStore(treeNode, tgtReg, varDsc);
}

#endif // FEATURE_SIMD

#ifdef PROFILING_SUPPORTED

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
    assert(compiler->compGeneratingProlog);

    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    if (compiler->compProfilerMethHndIndirected)
    {
        instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, REG_PROFILER_ENTER_ARG_FUNC_ID,
                               (ssize_t)compiler->compProfilerMethHnd);
        GetEmitter()->emitIns_R_R(INS_ldr, EA_PTRSIZE, REG_PROFILER_ENTER_ARG_FUNC_ID, REG_PROFILER_ENTER_ARG_FUNC_ID);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_PROFILER_ENTER_ARG_FUNC_ID, (ssize_t)compiler->compProfilerMethHnd);
    }

    int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
    genInstrWithConstant(INS_add, EA_PTRSIZE, REG_PROFILER_ENTER_ARG_CALLER_SP, genFramePointerReg(),
                         (ssize_t)(-callerSPOffset), REG_PROFILER_ENTER_ARG_CALLER_SP);

    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER, 0, EA_UNKNOWN);

    if ((genRegMask(initReg) & RBM_PROFILER_ENTER_TRASH) != RBM_NONE)
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

    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    compiler->info.compProfilerCallback = true;

    if (compiler->compProfilerMethHndIndirected)
    {
        instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, REG_PROFILER_LEAVE_ARG_FUNC_ID,
                               (ssize_t)compiler->compProfilerMethHnd);
        GetEmitter()->emitIns_R_R(INS_ldr, EA_PTRSIZE, REG_PROFILER_LEAVE_ARG_FUNC_ID, REG_PROFILER_LEAVE_ARG_FUNC_ID);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_PROFILER_LEAVE_ARG_FUNC_ID, (ssize_t)compiler->compProfilerMethHnd);
    }

    gcInfo.gcMarkRegSetNpt(RBM_PROFILER_LEAVE_ARG_FUNC_ID);

    int callerSPOffset = compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
    genInstrWithConstant(INS_add, EA_PTRSIZE, REG_PROFILER_LEAVE_ARG_CALLER_SP, genFramePointerReg(),
                         (ssize_t)(-callerSPOffset), REG_PROFILER_LEAVE_ARG_CALLER_SP);

    gcInfo.gcMarkRegSetNpt(RBM_PROFILER_LEAVE_ARG_CALLER_SP);

    genEmitHelperCall(helper, 0, EA_UNKNOWN);
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

    if (delta == 0)
    {
        GetEmitter()->emitIns_Mov(INS_mov, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, /* canSkip */ false);
    }
    else
    {
        GetEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);
    }

    if (reportUnwindData)
    {
        compiler->unwindSetFrameReg(REG_FPBASE, delta);
    }
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
    assert(compiler->compGeneratingProlog);

    if (frameSize == 0)
    {
        return;
    }

    const target_size_t pageSize = compiler->eeGetPageSize();

    // What offset from the final SP was the last probe? If we haven't probed almost a complete page, and
    // if the next action on the stack might subtract from SP first, before touching the current SP, then
    // we do one more probe at the very bottom. This can happen if we call a function on arm64 that does
    // a "STP fp, lr, [sp-504]!", that is, pre-decrement SP then store. Note that we probe here for arm64,
    // but we don't alter SP.
    target_size_t lastTouchDelta = 0;

    assert(!compiler->info.compPublishStubParam || (REG_SECRET_STUB_PARAM != initReg));

    if (frameSize < pageSize)
    {
        lastTouchDelta = frameSize;
    }
    else if (frameSize < 3 * pageSize)
    {
        // The probing loop in "else"-case below would require at least 6 instructions (and more if
        // 'frameSize' or 'pageSize' can not be encoded with mov-instruction immediate).
        // Hence for frames that are smaller than 3 * PAGE_SIZE the JIT inlines the following probing code
        // to decrease code size.
        // TODO-ARM64: The probing mechanisms should be replaced by a call to stack probe helper
        // as it is done on other platforms.

        lastTouchDelta = frameSize;

        for (target_size_t probeOffset = pageSize; probeOffset <= frameSize; probeOffset += pageSize)
        {
            // Generate:
            //    movw initReg, -probeOffset
            //    ldr wzr, [sp + initReg]

            instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -(ssize_t)probeOffset);
            GetEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, initReg);
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
        //      mov rOffset, -pageSize    // On arm, this turns out to be "movw r1, 0xf000; sxth r1, r1".
        //                                // We could save 4 bytes in the prolog by using "movs r1, 0" at the
        //                                // runtime expense of running a useless first loop iteration.
        //      mov rLimit, -frameSize
        // loop:
        //      ldr wzr, [sp + rOffset]
        //      sub rOffset, pageSize
        //      cmp rLimit, rOffset
        //      b.ls loop                 // If rLimit is lower or same, we need to probe this rOffset. Note
        //                                // especially that if it is the same, we haven't probed this page.

        noway_assert((ssize_t)(int)frameSize == (ssize_t)frameSize); // make sure framesize safely fits within an int

        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rOffset, -(ssize_t)pageSize);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rLimit, -(ssize_t)frameSize);

        // There's a "virtual" label here. But we can't create a label in the prolog, so we use the magic
        // `emitIns_J` with a negative `instrCount` to branch back a specific number of instructions.

        GetEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, rOffset);
        GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, rOffset, rOffset, pageSize);
        GetEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, rLimit, rOffset); // If equal, we need to probe again
        GetEmitter()->emitIns_J(INS_bls, NULL, -4);

        *pInitRegZeroed = false; // The initReg does not contain zero

        compiler->unwindPadding();

        lastTouchDelta = frameSize % pageSize;
    }

    if (lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize)
    {
        assert(lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES < 2 * pageSize);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -(ssize_t)frameSize);
        GetEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, initReg);
        compiler->unwindPadding();

        regSet.verifyRegUsed(initReg);
        *pInitRegZeroed = false; // The initReg does not contain zero
    }
}

//-----------------------------------------------------------------------------------
// instGen_MemoryBarrier: Emit a MemoryBarrier instruction
//
// Arguments:
//     barrierKind - kind of barrier to emit (Full or Load-Only).
//
// Notes:
//     All MemoryBarriers instructions can be removed by DOTNET_JitNoMemoryBarriers=1
//
void CodeGen::instGen_MemoryBarrier(BarrierKind barrierKind)
{
#ifdef DEBUG
    if (JitConfig.JitNoMemoryBarriers() == 1)
    {
        return;
    }
#endif // DEBUG

    // Avoid emitting redundant memory barriers on arm64 if they belong to the same IG
    // and there were no memory accesses in-between them
    emitter::instrDesc* lastMemBarrier = GetEmitter()->emitLastMemBarrier;
    if ((lastMemBarrier != nullptr) && compiler->opts.OptimizationEnabled())
    {
        BarrierKind prevBarrierKind = BARRIER_FULL;
        if (lastMemBarrier->idSmallCns() == INS_BARRIER_ISHLD)
        {
            prevBarrierKind = BARRIER_LOAD_ONLY;
        }
        else
        {
            // Currently we only emit two kinds of barriers on arm64:
            //  ISH   - Full (inner shareable domain)
            //  ISHLD - LoadOnly (inner shareable domain)
            assert(lastMemBarrier->idSmallCns() == INS_BARRIER_ISH);
        }

        if ((prevBarrierKind == BARRIER_LOAD_ONLY) && (barrierKind == BARRIER_FULL))
        {
            // Previous memory barrier: load-only, current: full
            // Upgrade the previous one to full
            assert((prevBarrierKind == BARRIER_LOAD_ONLY) && (barrierKind == BARRIER_FULL));
            lastMemBarrier->idSmallCns(INS_BARRIER_ISH);
        }
    }
    else
    {
        GetEmitter()->emitIns_BARR(INS_dmb, barrierKind == BARRIER_LOAD_ONLY ? INS_BARRIER_ISHLD : INS_BARRIER_ISH);
    }
}

//------------------------------------------------------------------------
// genCodeForBfiz: Generates the code sequence for a GenTree node that
// represents a bitfield insert in zero with sign/zero extension.
//
// Arguments:
//    tree - the bitfield insert in zero node.
//
void CodeGen::genCodeForBfiz(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_BFIZ));

    emitAttr     size       = emitActualTypeSize(tree);
    unsigned     shiftBy    = (unsigned)tree->gtGetOp2()->AsIntCon()->IconValue();
    unsigned     shiftByImm = shiftBy & (emitter::getBitWidth(size) - 1);
    GenTreeCast* cast       = tree->gtGetOp1()->AsCast();
    GenTree*     castOp     = cast->CastOp();

    genConsumeRegs(castOp);
    unsigned srcBits = varTypeIsSmall(cast->CastToType()) ? genTypeSize(cast->CastToType()) * BITS_PER_BYTE
                                                          : genTypeSize(castOp) * BITS_PER_BYTE;
    const bool isUnsigned = cast->IsUnsigned() || varTypeIsUnsigned(cast->CastToType());
    GetEmitter()->emitIns_R_R_I_I(isUnsigned ? INS_ubfiz : INS_sbfiz, size, tree->GetRegNum(), castOp->GetRegNum(),
                                  (int)shiftByImm, (int)srcBits);

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// JumpKindToInsCond: Convert a Jump Kind to a condition.
//
// Arguments:
//    condition - the emitJumpKind.
//
insCond CodeGen::JumpKindToInsCond(emitJumpKind condition)
{
    /* Convert the condition to an insCond value */
    switch (condition)
    {
        case EJ_eq:
            return INS_COND_EQ;
        case EJ_ne:
            return INS_COND_NE;
        case EJ_hs:
            return INS_COND_HS;
        case EJ_lo:
            return INS_COND_LO;
        case EJ_mi:
            return INS_COND_MI;
        case EJ_pl:
            return INS_COND_PL;
        case EJ_vs:
            return INS_COND_VS;
        case EJ_vc:
            return INS_COND_VC;
        case EJ_hi:
            return INS_COND_HI;
        case EJ_ls:
            return INS_COND_LS;
        case EJ_ge:
            return INS_COND_GE;
        case EJ_lt:
            return INS_COND_LT;
        case EJ_gt:
            return INS_COND_GT;
        case EJ_le:
            return INS_COND_LE;
        default:
            NO_WAY("unexpected condition type");
            return INS_COND_EQ;
    }
}

//------------------------------------------------------------------------
// ShiftOpToInsOpts: Convert a shift-op to a insOpts.
//
// Arguments:
//    shiftOp - the shift-op
//
insOpts CodeGen::ShiftOpToInsOpts(genTreeOps shiftOp)
{
    switch (shiftOp)
    {
        case GT_LSH:
            return INS_OPTS_LSL;

        case GT_RSH:
            return INS_OPTS_ASR;

        case GT_RSZ:
            return INS_OPTS_LSR;

        default:
            NO_WAY("expected a shift-op");
            return INS_OPTS_NONE;
    }
}

#endif // TARGET_ARM64
