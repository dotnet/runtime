// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM64_
#include "emit.h"
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Prolog / Epilog                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

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
//    tmpReg              - temp register to use when the 'imm' doesn't fit
//    inUnwindRegion      - true if we are in a prolog/epilog region with unwind codes
//
// Return Value:
//    returns true if the immediate was too large and tmpReg was used and modified.
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
        case INS_strh:
        case INS_str:
            // reg1 is a source register for store instructions
            assert(tmpReg != reg1); // regTmp can not match any source register
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, size);
            break;

        case INS_ldrsb:
        case INS_ldrsh:
        case INS_ldrsw:
        case INS_ldrb:
        case INS_ldrh:
        case INS_ldr:
            immFitsInIns = emitter::emitIns_valid_imm_for_ldst_offset(imm, size);
            break;

        default:
            assert(!"Unexpected instruction in genInstrWithConstant");
            break;
    }

    if (immFitsInIns)
    {
        // generate a single instruction that encodes the immediate directly
        getEmitter()->emitIns_R_R_I(ins, attr, reg1, reg2, imm);
    }
    else
    {
        // caller can specify REG_NA  for tmpReg, when it "knows" that the immediate will always fit
        assert(tmpReg != REG_NA);

        // generate two or more instructions

        // first we load the immediate into tmpReg
        instGen_Set_Reg_To_Imm(size, tmpReg, imm);
        regTracker.rsTrackRegTrash(tmpReg);

        // when we are in an unwind code region
        // we record the extra instructions using unwindPadding()
        if (inUnwindRegion)
        {
            compiler->unwindPadding();
        }

        // generate the instruction using a three register encoding with the immediate in tmpReg
        getEmitter()->emitIns_R_R_R(ins, attr, reg1, reg2, tmpReg);
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
//
// Return Value:
//    None.

void CodeGen::genStackPointerAdjustment(ssize_t spDelta, regNumber tmpReg, bool* pTmpRegIsZero)
{
    // Even though INS_add is specified here, the encoder will choose either
    // an INS_add or an INS_sub and encode the immediate as a positive value
    //
    if (genInstrWithConstant(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta, tmpReg, true))
    {
        if (pTmpRegIsZero != nullptr)
        {
            *pTmpRegIsZero = false;
        }
    }

    // spDelta is negative in the prolog, positive in the epilog, but we always tell the unwind codes the positive
    // value.
    ssize_t  spDeltaAbs    = abs(spDelta);
    unsigned unwindSpDelta = (unsigned)spDeltaAbs;
    assert((ssize_t)unwindSpDelta == spDeltaAbs); // make sure that it fits in a unsigned

    compiler->unwindAllocStack(unwindSpDelta);
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
//    lastSavedWasPreviousPair - True if the last prolog instruction was to save the previous register pair. This
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
                                   bool      lastSavedWasPreviousPair,
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
        if ((spOffset == 0) && (spDelta >= -512))
        {
            // We can use pre-indexed addressing.
            // stp REG, REG + 1, [SP, #spDelta]!
            // 64-bit STP offset range: -512 to 504, multiple of 8.
            getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spDelta, INS_OPTS_PRE_INDEX);
            compiler->unwindSaveRegPairPreindexed(reg1, reg2, spDelta);

            needToSaveRegs = false;
        }
        else // (spDelta < -512))
        {
            // We need to do SP adjustment separately from the store; we can't fold in a pre-indexed addressing and the
            // non-zero offset.

            // generate sub SP,SP,imm
            genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero);
        }
    }

    if (needToSaveRegs)
    {
        // stp REG, REG + 1, [SP, #offset]
        // 64-bit STP offset range: -512 to 504, multiple of 8.
        assert(spOffset <= 504);
        getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spOffset);

        if (lastSavedWasPreviousPair)
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

    if (spDelta != 0)
    {
        // generate sub SP,SP,imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero);
    }

    // str REG, [SP, #offset]
    // 64-bit STR offset range: 0 to 32760, multiple of 8.
    getEmitter()->emitIns_R_R_I(INS_str, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
    compiler->unwindSaveReg(reg1, spOffset);
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
//    tmpReg                   - An available temporary register. Needed for the case of large frames.
//    pTmpRegIsZero            - If we use tmpReg, and pTmpRegIsZero is non-null, we set *pTmpRegIsZero to 'false'.
//                               Otherwise, we don't touch it.
//
// Return Value:
//    None.

void CodeGen::genEpilogRestoreRegPair(
    regNumber reg1, regNumber reg2, int spOffset, int spDelta, regNumber tmpReg, bool* pTmpRegIsZero)
{
    assert(spOffset >= 0);
    assert(spDelta >= 0);
    assert((spDelta % 16) == 0); // SP changes must be 16-byte aligned

    if (spDelta != 0)
    {
        if ((spOffset == 0) && (spDelta <= 504))
        {
            // Fold the SP change into this instruction.
            // ldp reg1, reg2, [SP], #spDelta
            getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spDelta, INS_OPTS_POST_INDEX);
            compiler->unwindSaveRegPairPreindexed(reg1, reg2, -spDelta);
        }
        else // (spDelta > 504))
        {
            // Can't fold in the SP change; need to use a separate ADD instruction.

            // ldp reg1, reg2, [SP, #offset]
            getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spOffset);
            compiler->unwindSaveRegPair(reg1, reg2, spOffset);

            // generate add SP,SP,imm
            genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero);
        }
    }
    else
    {
        // ldp reg1, reg2, [SP, #offset]
        getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, spOffset);
        compiler->unwindSaveRegPair(reg1, reg2, spOffset);
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

    // ldr reg1, [SP, #offset]
    getEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
    compiler->unwindSaveReg(reg1, spOffset);

    if (spDelta != 0)
    {
        // generate add SP,SP,imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero);
    }
}

//------------------------------------------------------------------------
// genSaveCalleeSavedRegistersHelp: Save the callee-saved registers in 'regsToSaveMask' to the stack frame
// in the function or funclet prolog. The save set does not contain FP, since that is
// guaranteed to be saved separately, so we can set up chaining. We can only use the instructions
// that are allowed by the unwind codes. Integer registers are stored at lower addresses,
// FP/SIMD registers are stored at higher addresses. There are no gaps. The caller ensures that
// there is enough space on the frame to store these registers, and that the store instructions
// we need to use (STR or STP) are encodable with the stack-pointer immediate offsets we need to
// use. Note that the save set can contain LR if this is a frame without a frame pointer, in
// which case LR is saved along with the other callee-saved registers. The caller can tell us
// to fold in a stack pointer adjustment, which we will do with the first instruction. Note that
// the stack pointer adjustment must be by a multiple of 16 to preserve the invariant that the
// stack pointer is always 16 byte aligned. If we are saving an odd number of callee-saved
// registers, though, we will have an empty aligment slot somewhere. It turns out we will put
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
// Return Value:
//    None.

void CodeGen::genSaveCalleeSavedRegistersHelp(regMaskTP regsToSaveMask, int lowestCalleeSavedOffset, int spDelta)
{
    assert(spDelta <= 0);
    unsigned regsToSaveCount = genCountBits(regsToSaveMask);
    if (regsToSaveCount == 0)
    {
        if (spDelta != 0)
        {
            // Currently this is the case for varargs only
            // whose size is MAX_REG_ARG * REGSIZE_BYTES = 64 bytes.
            genStackPointerAdjustment(spDelta, REG_NA, nullptr);
        }
        return;
    }

    assert((spDelta % 16) == 0);
    assert((regsToSaveMask & RBM_FP) == 0);                             // we never save FP here
    assert(regsToSaveCount <= genCountBits(RBM_CALLEE_SAVED | RBM_LR)); // We also save LR, even though it is not in
                                                                        // RBM_CALLEE_SAVED.

    regMaskTP maskSaveRegsFloat = regsToSaveMask & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = regsToSaveMask & ~maskSaveRegsFloat;

    int spOffset = lowestCalleeSavedOffset; // this is the offset *after* we change SP.

    unsigned intRegsToSaveCount   = genCountBits(maskSaveRegsInt);
    unsigned floatRegsToSaveCount = genCountBits(maskSaveRegsFloat);
    bool     isPairSave           = false;
#ifdef DEBUG
    bool isRegsToSaveCountOdd = ((intRegsToSaveCount + floatRegsToSaveCount) % 2 != 0);
#endif

    // Save the integer registers

    bool lastSavedWasPair = false;

    while (maskSaveRegsInt != RBM_NONE)
    {
        // If this is the first store that needs to change SP (spDelta != 0),
        // then the offset must be 8 to account for alignment for the odd count
        // or it must be 0 for the even count.
        assert((spDelta == 0) || (isRegsToSaveCountOdd && spOffset == REGSIZE_BYTES) ||
               (!isRegsToSaveCountOdd && spOffset == 0));

        isPairSave         = (intRegsToSaveCount >= 2);
        regMaskTP reg1Mask = genFindLowestBit(maskSaveRegsInt);
        regNumber reg1     = genRegNumFromMask(reg1Mask);
        maskSaveRegsInt &= ~reg1Mask;
        intRegsToSaveCount -= 1;

        if (isPairSave)
        {
            // We can use a STP instruction.

            regMaskTP reg2Mask = genFindLowestBit(maskSaveRegsInt);
            regNumber reg2     = genRegNumFromMask(reg2Mask);
            assert((reg2 == REG_NEXT(reg1)) || (reg2 == REG_LR));
            maskSaveRegsInt &= ~reg2Mask;
            intRegsToSaveCount -= 1;

            genPrologSaveRegPair(reg1, reg2, spOffset, spDelta, lastSavedWasPair, REG_IP0, nullptr);

            // TODO-ARM64-CQ: this code works in the prolog, but it's a bit weird to think about "next" when generating
            // this epilog, to get the codes to match. Turn this off until that is better understood.
            // lastSavedWasPair = true;

            spOffset += 2 * REGSIZE_BYTES;
        }
        else
        {
            // No register pair; we use a STR instruction.

            genPrologSaveReg(reg1, spOffset, spDelta, REG_IP0, nullptr);

            lastSavedWasPair = false;
            spOffset += REGSIZE_BYTES;
        }

        spDelta = 0; // We've now changed SP already, if necessary; don't do it again.
    }

    assert(intRegsToSaveCount == 0);

    // Save the floating-point/SIMD registers

    lastSavedWasPair = false;

    while (maskSaveRegsFloat != RBM_NONE)
    {
        // If this is the first store that needs to change SP (spDelta != 0),
        // then the offset must be 8 to account for alignment for the odd count
        // or it must be 0 for the even count.
        assert((spDelta == 0) || (isRegsToSaveCountOdd && spOffset == REGSIZE_BYTES) ||
               (!isRegsToSaveCountOdd && spOffset == 0));

        isPairSave         = (floatRegsToSaveCount >= 2);
        regMaskTP reg1Mask = genFindLowestBit(maskSaveRegsFloat);
        regNumber reg1     = genRegNumFromMask(reg1Mask);
        maskSaveRegsFloat &= ~reg1Mask;
        floatRegsToSaveCount -= 1;

        if (isPairSave)
        {
            // We can use a STP instruction.

            regMaskTP reg2Mask = genFindLowestBit(maskSaveRegsFloat);
            regNumber reg2     = genRegNumFromMask(reg2Mask);
            assert(reg2 == REG_NEXT(reg1));
            maskSaveRegsFloat &= ~reg2Mask;
            floatRegsToSaveCount -= 1;

            genPrologSaveRegPair(reg1, reg2, spOffset, spDelta, lastSavedWasPair, REG_IP0, nullptr);

            // TODO-ARM64-CQ: this code works in the prolog, but it's a bit weird to think about "next" when generating
            // this epilog, to get the codes to match. Turn this off until that is better understood.
            // lastSavedWasPair = true;

            spOffset += 2 * FPSAVE_REGSIZE_BYTES;
        }
        else
        {
            // No register pair; we use a STR instruction.

            genPrologSaveReg(reg1, spOffset, spDelta, REG_IP0, nullptr);

            lastSavedWasPair = false;
            spOffset += FPSAVE_REGSIZE_BYTES;
        }

        spDelta = 0; // We've now changed SP already, if necessary; don't do it again.
    }

    assert(floatRegsToSaveCount == 0);
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
            genStackPointerAdjustment(spDelta, REG_NA, nullptr);
        }
        return;
    }

    assert((spDelta % 16) == 0);
    assert((regsToRestoreMask & RBM_FP) == 0); // we never restore FP here
    assert(regsToRestoreCount <=
           genCountBits(RBM_CALLEE_SAVED | RBM_LR)); // We also save LR, even though it is not in RBM_CALLEE_SAVED.

    regMaskTP maskRestoreRegsFloat = regsToRestoreMask & RBM_ALLFLOAT;
    regMaskTP maskRestoreRegsInt   = regsToRestoreMask & ~maskRestoreRegsFloat;

    assert(REGSIZE_BYTES == FPSAVE_REGSIZE_BYTES);
    int spOffset = lowestCalleeSavedOffset + regsToRestoreCount * REGSIZE_BYTES; // Point past the end, to start. We
                                                                                 // predecrement to find the offset to
                                                                                 // load from.

    unsigned floatRegsToRestoreCount         = genCountBits(maskRestoreRegsFloat);
    unsigned intRegsToRestoreCount           = genCountBits(maskRestoreRegsInt);
    int      stackDelta                      = 0;
    bool     isPairRestore                   = false;
    bool     thisIsTheLastRestoreInstruction = false;
#ifdef DEBUG
    bool isRegsToRestoreCountOdd = ((floatRegsToRestoreCount + intRegsToRestoreCount) % 2 != 0);
#endif

    // We want to restore in the opposite order we saved, so the unwind codes match. Be careful to handle odd numbers of
    // callee-saved registers properly.

    // Restore the floating-point/SIMD registers

    while (maskRestoreRegsFloat != RBM_NONE)
    {
        thisIsTheLastRestoreInstruction = (floatRegsToRestoreCount <= 2) && (maskRestoreRegsInt == RBM_NONE);
        isPairRestore                   = (floatRegsToRestoreCount % 2) == 0;

        // Update stack delta only if it is the last restore (the first save).
        if (thisIsTheLastRestoreInstruction)
        {
            assert(stackDelta == 0);
            stackDelta = spDelta;
        }

        // Update stack offset.
        if (isPairRestore)
        {
            spOffset -= 2 * FPSAVE_REGSIZE_BYTES;
        }
        else
        {
            spOffset -= FPSAVE_REGSIZE_BYTES;
        }

        // If this is the last restore (the first save) that needs to change SP (stackDelta != 0),
        // then the offset must be 8 to account for alignment for the odd count
        // or it must be 0 for the even count.
        assert((stackDelta == 0) || (isRegsToRestoreCountOdd && spOffset == FPSAVE_REGSIZE_BYTES) ||
               (!isRegsToRestoreCountOdd && spOffset == 0));

        regMaskTP reg2Mask = genFindHighestBit(maskRestoreRegsFloat);
        regNumber reg2     = genRegNumFromMask(reg2Mask);
        maskRestoreRegsFloat &= ~reg2Mask;
        floatRegsToRestoreCount -= 1;

        if (isPairRestore)
        {
            regMaskTP reg1Mask = genFindHighestBit(maskRestoreRegsFloat);
            regNumber reg1     = genRegNumFromMask(reg1Mask);
            maskRestoreRegsFloat &= ~reg1Mask;
            floatRegsToRestoreCount -= 1;

            genEpilogRestoreRegPair(reg1, reg2, spOffset, stackDelta, REG_IP0, nullptr);
        }
        else
        {
            genEpilogRestoreReg(reg2, spOffset, stackDelta, REG_IP0, nullptr);
        }
    }

    assert(floatRegsToRestoreCount == 0);

    // Restore the integer registers

    while (maskRestoreRegsInt != RBM_NONE)
    {
        thisIsTheLastRestoreInstruction = (intRegsToRestoreCount <= 2);
        isPairRestore                   = (intRegsToRestoreCount % 2) == 0;

        // Update stack delta only if it is the last restore (the first save).
        if (thisIsTheLastRestoreInstruction)
        {
            assert(stackDelta == 0);
            stackDelta = spDelta;
        }

        // Update stack offset.
        spOffset -= REGSIZE_BYTES;
        if (isPairRestore)
        {
            spOffset -= REGSIZE_BYTES;
        }

        // If this is the last restore (the first save) that needs to change SP (stackDelta != 0),
        // then the offset must be 8 to account for alignment for the odd count
        // or it must be 0 for the even count.
        assert((stackDelta == 0) || (isRegsToRestoreCountOdd && spOffset == REGSIZE_BYTES) ||
               (!isRegsToRestoreCountOdd && spOffset == 0));

        regMaskTP reg2Mask = genFindHighestBit(maskRestoreRegsInt);
        regNumber reg2     = genRegNumFromMask(reg2Mask);
        maskRestoreRegsInt &= ~reg2Mask;
        intRegsToRestoreCount -= 1;

        if (isPairRestore)
        {
            regMaskTP reg1Mask = genFindHighestBit(maskRestoreRegsInt);
            regNumber reg1     = genRegNumFromMask(reg1Mask);
            maskRestoreRegsInt &= ~reg1Mask;
            intRegsToRestoreCount -= 1;

            genEpilogRestoreRegPair(reg1, reg2, spOffset, stackDelta, REG_IP0, nullptr);
        }
        else
        {
            genEpilogRestoreReg(reg2, spOffset, stackDelta, REG_IP0, nullptr);
        }
    }

    assert(intRegsToRestoreCount == 0);
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
 *     stp fp,lr,[sp,-#framesz]!    ; establish the frame, save FP/LR
 *     stp x19,x20,[sp,#xxx]        ; save callee-saved registers, as necessary
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes
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
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes
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
 *     stp fp,lr,[sp,- (#framesz - #outsz)]!    ; establish the frame, save FP/LR: note that it is guaranteed here that (#framesz - #outsz) <= 168
 *     stp x19,x20,[sp,#xxx]                    ; save callee-saved registers, as necessary
 *     sub sp,sp,#outsz                         ; create space for outgoing argument space
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the first SP subtraction 16 byte aligned
 *      |-----------------------|
 *      |      Saved FP, LR     | // 16 bytes
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned (specifically, to 16-byte align the outgoing argument space).
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes
 *      |-----------------------| <---- Ambient SP
 *      |       |               |         
 *      ~       | Stack grows   ~         
 *      |       | downward      |         
 *              V
 *
 * Both #1 and #2 only change SP once. That means that there will be a maximum of one alignment slot needed. For the general case, #3,
 * it is possible that we will need to add alignment to both changes to SP, leading to 16 bytes of alignment. Remember that the stack
 * pointer needs to be 16 byte aligned at all times. The size of the PSP slot plus callee-saved registers space is a maximum of 168 bytes:
 * (1 PSP slot + 12 integer registers + 8 FP/SIMD registers) * 8 bytes. The outgoing argument size, however, can be very large, if we call a
 * function that takes a large number of arguments (note that we currently use the same outgoing argument space size in the funclet as for the main
 * function, even if the funclet doesn't have any calls, or has a much smaller, or larger, maximum number of outgoing arguments for any call).
 * In that case, we need to 16-byte align the initial change to SP, before saving off the callee-saved registers and establishing the PSPsym,
 * so we can use the limited immediate offset encodings we have available, before doing another 16-byte aligned SP adjustment to create the
 * outgoing argument space. Both changes to SP might need to add alignment padding.
 *
 * Note that in all cases, the PSPSym is in exactly the same position with respect to Caller-SP, and that location is the same relative to Caller-SP
 * as in the main function.
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
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |       incoming        |
 *      |       arguments       |
 *      +=======================+ <---- Caller's SP
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes
 *      |-----------------------|
 *      |      Saved FP, LR     | // 16 bytes
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned.
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes
 *      |-----------------------| <---- Ambient SP
 *      |       |               |         
 *      ~       | Stack grows   ~         
 *      |       | downward      |         
 *              V
 */
// clang-format on

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletProlog()\n");
#endif

    assert(block != NULL);
    assert(block->bbFlags && BBF_FUNCLET_BEG);

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

    int lowestCalleeSavedOffset = genFuncletInfo.fiSP_to_CalleeSave_delta;

    if (genFuncletInfo.fiFrameType == 1)
    {
        getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, genFuncletInfo.fiSpDelta1,
                                      INS_OPTS_PRE_INDEX);
        compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);

        assert(genFuncletInfo.fiSpDelta2 == 0);
        assert(genFuncletInfo.fiSP_to_FPLR_save_delta == 0);
    }
    else if (genFuncletInfo.fiFrameType == 2)
    {
        // fiFrameType==2 constraints:
        assert(genFuncletInfo.fiSpDelta1 < 0);
        assert(genFuncletInfo.fiSpDelta1 >= -512);

        // generate sub SP,SP,imm
        genStackPointerAdjustment(genFuncletInfo.fiSpDelta1, REG_NA, nullptr);

        assert(genFuncletInfo.fiSpDelta2 == 0);

        getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                      genFuncletInfo.fiSP_to_FPLR_save_delta);
        compiler->unwindSaveRegPair(REG_FP, REG_LR, genFuncletInfo.fiSP_to_FPLR_save_delta);
    }
    else
    {
        assert(genFuncletInfo.fiFrameType == 3);
        getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, genFuncletInfo.fiSpDelta1,
                                      INS_OPTS_PRE_INDEX);
        compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);

        lowestCalleeSavedOffset += genFuncletInfo.fiSpDelta2; // We haven't done the second adjustment of SP yet.
    }
    maskSaveRegsInt &= ~(RBM_LR | RBM_FP); // We've saved these now

    genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, lowestCalleeSavedOffset, 0);

    if (genFuncletInfo.fiFrameType == 3)
    {
        // Note that genFuncletInfo.fiSpDelta2 is always a negative value
        assert(genFuncletInfo.fiSpDelta2 < 0);

        // generate sub SP,SP,imm
        genStackPointerAdjustment(genFuncletInfo.fiSpDelta2, REG_R2, nullptr);
    }

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    if (isFilter)
    {
        // This is the first block of a filter
        // Note that register x1 = CallerSP of the containing function
        // X1 is overwritten by the first Load (new callerSP)
        // X2 is scratch when we have a large constant offset

        // Load the CallerSP of the main function (stored in the PSP of the dynamically containing funclet or function)
        genInstrWithConstant(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_R1, REG_R1,
                             genFuncletInfo.fiCallerSP_to_PSP_slot_delta, REG_R2, false);
        regTracker.rsTrackRegTrash(REG_R1);

        // Store the PSP value (aka CallerSP)
        genInstrWithConstant(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_R1, REG_SPBASE,
                             genFuncletInfo.fiSP_to_PSP_slot_delta, REG_R2, false);

        // re-establish the frame pointer
        genInstrWithConstant(INS_add, EA_PTRSIZE, REG_FPBASE, REG_R1, genFuncletInfo.fiFunction_CallerSP_to_FP_delta,
                             REG_R2, false);
    }
    else // This is a non-filter funclet
    {
        // X3 is scratch, X2 can also become scratch

        // compute the CallerSP, given the frame pointer. x3 is scratch.
        genInstrWithConstant(INS_add, EA_PTRSIZE, REG_R3, REG_FPBASE, -genFuncletInfo.fiFunction_CallerSP_to_FP_delta,
                             REG_R2, false);
        regTracker.rsTrackRegTrash(REG_R3);

        genInstrWithConstant(ins_Store(TYP_I_IMPL), EA_PTRSIZE, REG_R3, REG_SPBASE,
                             genFuncletInfo.fiSP_to_PSP_slot_delta, REG_R2, false);
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

    maskRestoreRegsInt &= ~(RBM_LR | RBM_FP); // We restore FP/LR at the end

    int lowestCalleeSavedOffset = genFuncletInfo.fiSP_to_CalleeSave_delta;

    if (genFuncletInfo.fiFrameType == 3)
    {
        // Note that genFuncletInfo.fiSpDelta2 is always a negative value
        assert(genFuncletInfo.fiSpDelta2 < 0);

        // generate add SP,SP,imm
        genStackPointerAdjustment(-genFuncletInfo.fiSpDelta2, REG_R2, nullptr);

        lowestCalleeSavedOffset += genFuncletInfo.fiSpDelta2;
    }

    regMaskTP regsToRestoreMask = maskRestoreRegsInt | maskRestoreRegsFloat;
    genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, lowestCalleeSavedOffset, 0);

    if (genFuncletInfo.fiFrameType == 1)
    {
        getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, -genFuncletInfo.fiSpDelta1,
                                      INS_OPTS_POST_INDEX);
        compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);

        assert(genFuncletInfo.fiSpDelta2 == 0);
        assert(genFuncletInfo.fiSP_to_FPLR_save_delta == 0);
    }
    else if (genFuncletInfo.fiFrameType == 2)
    {
        getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                      genFuncletInfo.fiSP_to_FPLR_save_delta);
        compiler->unwindSaveRegPair(REG_FP, REG_LR, genFuncletInfo.fiSP_to_FPLR_save_delta);

        // fiFrameType==2 constraints:
        assert(genFuncletInfo.fiSpDelta1 < 0);
        assert(genFuncletInfo.fiSpDelta1 >= -512);

        // generate add SP,SP,imm
        genStackPointerAdjustment(-genFuncletInfo.fiSpDelta1, REG_NA, nullptr);

        assert(genFuncletInfo.fiSpDelta2 == 0);
    }
    else
    {
        assert(genFuncletInfo.fiFrameType == 3);

        getEmitter()->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, -genFuncletInfo.fiSpDelta1,
                                      INS_OPTS_POST_INDEX);
        compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, genFuncletInfo.fiSpDelta1);
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
    assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT); // The frame size and offsets must be
                                                                          // finalized

    genFuncletInfo.fiFunction_CallerSP_to_FP_delta = genCallerSPtoFPdelta();

    regMaskTP rsMaskSaveRegs = regSet.rsMaskCalleeSaved;
    assert((rsMaskSaveRegs & RBM_LR) != 0);
    assert((rsMaskSaveRegs & RBM_FP) != 0);

    unsigned saveRegsCount       = genCountBits(rsMaskSaveRegs);
    unsigned saveRegsPlusPSPSize = saveRegsCount * REGSIZE_BYTES + /* PSPSym */ REGSIZE_BYTES;
    if (compiler->info.compIsVarArgs)
    {
        // For varargs we always save all of the integer register arguments
        // so that they are contiguous with the incoming stack arguments.
        saveRegsPlusPSPSize += MAX_REG_ARG * REGSIZE_BYTES;
    }
    unsigned saveRegsPlusPSPSizeAligned = (unsigned)roundUp(saveRegsPlusPSPSize, STACK_ALIGN);

    assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
    unsigned outgoingArgSpaceAligned = (unsigned)roundUp(compiler->lvaOutgoingArgSpaceSize, STACK_ALIGN);

    unsigned maxFuncletFrameSizeAligned = saveRegsPlusPSPSizeAligned + outgoingArgSpaceAligned;
    assert((maxFuncletFrameSizeAligned % STACK_ALIGN) == 0);

    int SP_to_FPLR_save_delta;
    int SP_to_PSP_slot_delta;
    int CallerSP_to_PSP_slot_delta;

    if (maxFuncletFrameSizeAligned <= 512)
    {
        unsigned funcletFrameSize        = saveRegsPlusPSPSize + compiler->lvaOutgoingArgSpaceSize;
        unsigned funcletFrameSizeAligned = (unsigned)roundUp(funcletFrameSize, STACK_ALIGN);
        assert(funcletFrameSizeAligned <= maxFuncletFrameSizeAligned);

        unsigned funcletFrameAlignmentPad = funcletFrameSizeAligned - funcletFrameSize;
        assert((funcletFrameAlignmentPad == 0) || (funcletFrameAlignmentPad == REGSIZE_BYTES));

        SP_to_FPLR_save_delta      = compiler->lvaOutgoingArgSpaceSize;
        SP_to_PSP_slot_delta       = SP_to_FPLR_save_delta + 2 /* FP, LR */ * REGSIZE_BYTES + funcletFrameAlignmentPad;
        CallerSP_to_PSP_slot_delta = -(int)(saveRegsPlusPSPSize - 2 /* FP, LR */ * REGSIZE_BYTES);

        if (compiler->lvaOutgoingArgSpaceSize == 0)
        {
            genFuncletInfo.fiFrameType = 1;
        }
        else
        {
            genFuncletInfo.fiFrameType = 2;
        }
        genFuncletInfo.fiSpDelta1 = -(int)funcletFrameSizeAligned;
        genFuncletInfo.fiSpDelta2 = 0;

        assert(genFuncletInfo.fiSpDelta1 + genFuncletInfo.fiSpDelta2 == -(int)funcletFrameSizeAligned);
    }
    else
    {
        unsigned saveRegsPlusPSPAlignmentPad = saveRegsPlusPSPSizeAligned - saveRegsPlusPSPSize;
        assert((saveRegsPlusPSPAlignmentPad == 0) || (saveRegsPlusPSPAlignmentPad == REGSIZE_BYTES));

        SP_to_FPLR_save_delta = outgoingArgSpaceAligned;
        SP_to_PSP_slot_delta  = SP_to_FPLR_save_delta + 2 /* FP, LR */ * REGSIZE_BYTES + saveRegsPlusPSPAlignmentPad;
        CallerSP_to_PSP_slot_delta =
            -(int)(saveRegsPlusPSPSizeAligned - 2 /* FP, LR */ * REGSIZE_BYTES - saveRegsPlusPSPAlignmentPad);

        genFuncletInfo.fiFrameType = 3;
        genFuncletInfo.fiSpDelta1  = -(int)saveRegsPlusPSPSizeAligned;
        genFuncletInfo.fiSpDelta2  = -(int)outgoingArgSpaceAligned;

        assert(genFuncletInfo.fiSpDelta1 + genFuncletInfo.fiSpDelta2 == -(int)maxFuncletFrameSizeAligned);
    }

    /* Now save it for future use */

    genFuncletInfo.fiSaveRegs                   = rsMaskSaveRegs;
    genFuncletInfo.fiSP_to_FPLR_save_delta      = SP_to_FPLR_save_delta;
    genFuncletInfo.fiSP_to_PSP_slot_delta       = SP_to_PSP_slot_delta;
    genFuncletInfo.fiSP_to_CalleeSave_delta     = SP_to_PSP_slot_delta + REGSIZE_BYTES;
    genFuncletInfo.fiCallerSP_to_PSP_slot_delta = CallerSP_to_PSP_slot_delta;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n");
        printf("Funclet prolog / epilog info\n");
        printf("                        Save regs: ");
        dspRegMask(genFuncletInfo.fiSaveRegs);
        printf("\n");
        printf("    Function CallerSP-to-FP delta: %d\n", genFuncletInfo.fiFunction_CallerSP_to_FP_delta);
        printf("  SP to FP/LR save location delta: %d\n", genFuncletInfo.fiSP_to_FPLR_save_delta);
        printf("             SP to PSP slot delta: %d\n", genFuncletInfo.fiSP_to_PSP_slot_delta);
        printf("    SP to callee-saved area delta: %d\n", genFuncletInfo.fiSP_to_CalleeSave_delta);
        printf("      Caller SP to PSP slot delta: %d\n", genFuncletInfo.fiCallerSP_to_PSP_slot_delta);
        printf("                       Frame type: %d\n", genFuncletInfo.fiFrameType);
        printf("                       SP delta 1: %d\n", genFuncletInfo.fiSpDelta1);
        printf("                       SP delta 2: %d\n", genFuncletInfo.fiSpDelta2);

        if (CallerSP_to_PSP_slot_delta != compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)) // for debugging
        {
            printf("lvaGetCallerSPRelativeOffset(lvaPSPSym): %d\n",
                   compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym));
        }
    }
#endif // DEBUG

    assert(genFuncletInfo.fiSP_to_FPLR_save_delta >= 0);
    assert(genFuncletInfo.fiSP_to_PSP_slot_delta >= 0);
    assert(genFuncletInfo.fiSP_to_CalleeSave_delta >= 0);
    assert(genFuncletInfo.fiCallerSP_to_PSP_slot_delta <= 0);
    assert(compiler->lvaPSPSym != BAD_VAR_NUM);
    assert(genFuncletInfo.fiCallerSP_to_PSP_slot_delta ==
           compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main function and
                                                                         // funclet!
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

// Get the register assigned to the given node

regNumber CodeGenInterface::genGetAssignedReg(GenTreePtr tree)
{
    return tree->gtRegNum;
}

//------------------------------------------------------------------------
// genSpillVar: Spill a local variable
//
// Arguments:
//    tree      - the lclVar node for the variable being spilled
//
// Return Value:
//    None.
//
// Assumptions:
//    The lclVar must be a register candidate (lvRegCandidate)

void CodeGen::genSpillVar(GenTreePtr tree)
{
    unsigned   varNum = tree->gtLclVarCommon.gtLclNum;
    LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

    assert(varDsc->lvIsRegCandidate());

    // We don't actually need to spill if it is already living in memory
    bool needsSpill = ((tree->gtFlags & GTF_VAR_DEF) == 0 && varDsc->lvIsInReg());
    if (needsSpill)
    {
        var_types lclTyp = varDsc->TypeGet();
        if (varDsc->lvNormalizeOnStore())
            lclTyp    = genActualType(lclTyp);
        emitAttr size = emitTypeSize(lclTyp);

        bool restoreRegVar = false;
        if (tree->gtOper == GT_REG_VAR)
        {
            tree->SetOper(GT_LCL_VAR);
            restoreRegVar = true;
        }

        // mask off the flag to generate the right spill code, then bring it back
        tree->gtFlags &= ~GTF_REG_VAL;

        instruction storeIns = ins_Store(tree->TypeGet(), compiler->isSIMDTypeLocalAligned(varNum));

        assert(varDsc->lvRegNum == tree->gtRegNum);
        inst_TT_RV(storeIns, tree, tree->gtRegNum, 0, size);

        tree->gtFlags |= GTF_REG_VAL;

        if (restoreRegVar)
        {
            tree->SetOper(GT_REG_VAR);
        }

        genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(tree));
        gcInfo.gcMarkRegSetNpt(varDsc->lvRegMask());

        if (VarSetOps::IsMember(compiler, gcInfo.gcTrkStkPtrLcls, varDsc->lvVarIndex))
        {
#ifdef DEBUG
            if (!VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
            {
                JITDUMP("\t\t\t\t\t\t\tVar V%02u becoming live\n", varNum);
            }
            else
            {
                JITDUMP("\t\t\t\t\t\t\tVar V%02u continuing live\n", varNum);
            }
#endif
            VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
        }
    }

    tree->gtFlags &= ~GTF_SPILL;
    varDsc->lvRegNum = REG_STK;
    if (varTypeIsMultiReg(tree))
    {
        varDsc->lvOtherReg = REG_STK;
    }
}

// inline
void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTreePtr tree)
{
    assert(tree->OperIsScalarLocal() || (tree->gtOper == GT_COPY));
    varDsc->lvRegNum = tree->gtRegNum;
}

/*****************************************************************************/
/*****************************************************************************/

/*****************************************************************************
 *
 *  Generate code that will set the given register to the integer constant.
 */

void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type, insFlags flags)
{
    // Reg cannot be a FP reg
    assert(!genIsValidFloatReg(reg));

    // The only TYP_REF constant that can come this path is a managed 'null' since it is not
    // relocatable.  Other ref type constants (e.g. string objects) go through a different
    // code path.
    noway_assert(type != TYP_REF || val == 0);

    instGen_Set_Reg_To_Imm(emitActualTypeSize(type), reg, val, flags);
}

/*****************************************************************************
 *
 *   Generate code to check that the GS cookie wasn't thrashed by a buffer
 *   overrun.  On ARM64 we always use REG_TMP_0 and REG_TMP_1 as temp registers
 *   and this works fine in the case of tail calls
 *   Implementation Note: pushReg = true, in case of tail calls.
 */
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    noway_assert(compiler->gsGlobalSecurityCookieAddr || compiler->gsGlobalSecurityCookieVal);

    // Make sure that the return register is reported as live GC-ref so that any GC that kicks in while
    // executing GS cookie check will not collect the object pointed to by REG_INTRET (R0).
    if (!pushReg && (compiler->info.compRetType == TYP_REF))
        gcInfo.gcRegGCrefSetCur |= RBM_INTRET;

    regNumber regGSConst = REG_TMP_0;
    regNumber regGSValue = REG_TMP_1;

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
        // load the GS cookie constant into a reg
        //
        genSetRegToIcon(regGSConst, compiler->gsGlobalSecurityCookieVal, TYP_I_IMPL);
    }
    else
    {
        // Ngen case - GS cookie constant needs to be accessed through an indirection.
        instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, regGSConst, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        getEmitter()->emitIns_R_R_I(ins_Load(TYP_I_IMPL), EA_PTRSIZE, regGSConst, regGSConst, 0);
    }
    // Load this method's GS value from the stack frame
    getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, regGSValue, compiler->lvaGSSecurityCookie, 0);
    // Compare with the GC cookie constant
    getEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, regGSConst, regGSValue);

    BasicBlock*  gsCheckBlk = genCreateTempLabel();
    emitJumpKind jmpEqual   = genJumpKindForOper(GT_EQ, CK_SIGNED);
    inst_JMP(jmpEqual, gsCheckBlk);
    genEmitHelperCall(CORINFO_HELP_FAIL_FAST, 0, EA_UNKNOWN);
    genDefineTempLabel(gsCheckBlk);
}

/*****************************************************************************
 *
 *  Generate code for all the basic blocks in the function.
 */

void CodeGen::genCodeForBBlist()
{
    unsigned   varNum;
    LclVarDsc* varDsc;

    unsigned savedStkLvl;

#ifdef DEBUG
    genInterruptibleUsed = true;

    // You have to be careful if you create basic blocks from now on
    compiler->fgSafeBasicBlockCreation = false;

    // This stress mode is not comptible with fully interruptible GC
    if (genInterruptible && compiler->opts.compStackCheckOnCall)
    {
        compiler->opts.compStackCheckOnCall = false;
    }

    // This stress mode is not comptible with fully interruptible GC
    if (genInterruptible && compiler->opts.compStackCheckOnRet)
    {
        compiler->opts.compStackCheckOnRet = false;
    }
#endif // DEBUG

    // Prepare the blocks for exception handling codegen: mark the blocks that needs labels.
    genPrepForEHCodegen();

    assert(!compiler->fgFirstBBScratch ||
           compiler->fgFirstBB == compiler->fgFirstBBScratch); // compiler->fgFirstBBScratch has to be first.

    /* Initialize the spill tracking logic */

    regSet.rsSpillBeg();

#ifdef DEBUGGING_SUPPORT
    /* Initialize the line# tracking logic */

    if (compiler->opts.compScopeInfo)
    {
        siInit();
    }
#endif

    // The current implementation of switch tables requires the first block to have a label so it
    // can generate offsets to the switch label targets.
    // TODO-ARM64-CQ: remove this when switches have been re-implemented to not use this.
    if (compiler->fgHasSwitch)
    {
        compiler->fgFirstBB->bbFlags |= BBF_JMP_TARGET;
    }

    genPendingCallLabel = nullptr;

    /* Initialize the pointer tracking code */

    gcInfo.gcRegPtrSetInit();
    gcInfo.gcVarPtrSetInit();

    /* If any arguments live in registers, mark those regs as such */

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        /* Is this variable a parameter assigned to a register? */

        if (!varDsc->lvIsParam || !varDsc->lvRegister)
            continue;

        /* Is the argument live on entry to the method? */

        if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
            continue;

        /* Is this a floating-point argument? */

        if (varDsc->IsFloatRegType())
            continue;

        noway_assert(!varTypeIsFloating(varDsc->TypeGet()));

        /* Mark the register as holding the variable */

        regTracker.rsTrackRegLclVar(varDsc->lvRegNum, varNum);
    }

    unsigned finallyNesting = 0;

    // Make sure a set is allocated for compiler->compCurLife (in the long case), so we can set it to empty without
    // allocation at the start of each basic block.
    VarSetOps::AssignNoCopy(compiler, compiler->compCurLife, VarSetOps::MakeEmpty(compiler));

    /*-------------------------------------------------------------------------
     *
     *  Walk the basic blocks and generate code for each one
     *
     */

    BasicBlock* block;
    BasicBlock* lblk; /* previous block */

    for (lblk = NULL, block = compiler->fgFirstBB; block != NULL; lblk = block, block = block->bbNext)
    {
#ifdef DEBUG
        if (compiler->verbose)
        {
            printf("\n=============== Generating ");
            block->dspBlockHeader(compiler, true, true);
            compiler->fgDispBBLiveness(block);
        }
#endif // DEBUG

        /* Figure out which registers hold variables on entry to this block */

        regSet.ClearMaskVars();
        gcInfo.gcRegGCrefSetCur = RBM_NONE;
        gcInfo.gcRegByrefSetCur = RBM_NONE;

        compiler->m_pLinearScan->recordVarLocationsAtStartOfBB(block);

        genUpdateLife(block->bbLiveIn);

        // Even if liveness didn't change, we need to update the registers containing GC references.
        // genUpdateLife will update the registers live due to liveness changes. But what about registers that didn't
        // change? We cleared them out above. Maybe we should just not clear them out, but update the ones that change
        // here. That would require handling the changes in recordVarLocationsAtStartOfBB().

        regMaskTP newLiveRegSet  = RBM_NONE;
        regMaskTP newRegGCrefSet = RBM_NONE;
        regMaskTP newRegByrefSet = RBM_NONE;
#ifdef DEBUG
        VARSET_TP VARSET_INIT_NOCOPY(removedGCVars, VarSetOps::MakeEmpty(compiler));
        VARSET_TP VARSET_INIT_NOCOPY(addedGCVars, VarSetOps::MakeEmpty(compiler));
#endif
        VARSET_ITER_INIT(compiler, iter, block->bbLiveIn, varIndex);
        while (iter.NextElem(compiler, &varIndex))
        {
            unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

            if (varDsc->lvIsInReg())
            {
                newLiveRegSet |= varDsc->lvRegMask();
                if (varDsc->lvType == TYP_REF)
                {
                    newRegGCrefSet |= varDsc->lvRegMask();
                }
                else if (varDsc->lvType == TYP_BYREF)
                {
                    newRegByrefSet |= varDsc->lvRegMask();
                }
#ifdef DEBUG
                if (verbose && VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varIndex))
                {
                    VarSetOps::AddElemD(compiler, removedGCVars, varIndex);
                }
#endif // DEBUG
                VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varIndex);
            }
            else if (compiler->lvaIsGCTracked(varDsc))
            {
#ifdef DEBUG
                if (verbose && !VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varIndex))
                {
                    VarSetOps::AddElemD(compiler, addedGCVars, varIndex);
                }
#endif // DEBUG
                VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varIndex);
            }
        }

        regSet.rsMaskVars = newLiveRegSet;

#ifdef DEBUG
        if (compiler->verbose)
        {
            if (!VarSetOps::IsEmpty(compiler, addedGCVars))
            {
                printf("\t\t\t\t\t\t\tAdded GCVars: ");
                dumpConvertedVarSet(compiler, addedGCVars);
                printf("\n");
            }
            if (!VarSetOps::IsEmpty(compiler, removedGCVars))
            {
                printf("\t\t\t\t\t\t\tRemoved GCVars: ");
                dumpConvertedVarSet(compiler, removedGCVars);
                printf("\n");
            }
        }
#endif // DEBUG

        gcInfo.gcMarkRegSetGCref(newRegGCrefSet DEBUGARG(true));
        gcInfo.gcMarkRegSetByref(newRegByrefSet DEBUGARG(true));

        /* Blocks with handlerGetsXcptnObj()==true use GT_CATCH_ARG to
           represent the exception object (TYP_REF).
           We mark REG_EXCEPTION_OBJECT as holding a GC object on entry
           to the block,  it will be the first thing evaluated
           (thanks to GTF_ORDER_SIDEEFF).
         */

        if (handlerGetsXcptnObj(block->bbCatchTyp))
        {
            for (GenTree* node : LIR::AsRange(block))
            {
                if (node->OperGet() == GT_CATCH_ARG)
                {
                    gcInfo.gcMarkRegSetGCref(RBM_EXCEPTION_OBJECT);
                    break;
                }
            }
        }

        /* Start a new code output block */

        genUpdateCurrentFunclet(block);

#ifdef _TARGET_XARCH_
        if (genAlignLoops && block->bbFlags & BBF_LOOP_HEAD)
        {
            getEmitter()->emitLoopAlign();
        }
#endif

#ifdef DEBUG
        if (compiler->opts.dspCode)
            printf("\n      L_M%03u_BB%02u:\n", Compiler::s_compMethodsCount, block->bbNum);
#endif

        block->bbEmitCookie = NULL;

        if (block->bbFlags & (BBF_JMP_TARGET | BBF_HAS_LABEL))
        {
            /* Mark a label and update the current set of live GC refs */

            block->bbEmitCookie = getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                             gcInfo.gcRegByrefSetCur, FALSE);
        }

        if (block == compiler->fgFirstColdBlock)
        {
#ifdef DEBUG
            if (compiler->verbose)
            {
                printf("\nThis is the start of the cold region of the method\n");
            }
#endif
            // We should never have a block that falls through into the Cold section
            noway_assert(!lblk->bbFallsThrough());

            // We require the block that starts the Cold section to have a label
            noway_assert(block->bbEmitCookie);
            getEmitter()->emitSetFirstColdIGCookie(block->bbEmitCookie);
        }

        /* Both stacks are always empty on entry to a basic block */

        genStackLevel = 0;

        savedStkLvl = genStackLevel;

        /* Tell everyone which basic block we're working on */

        compiler->compCurBB = block;

#ifdef DEBUGGING_SUPPORT
        siBeginBlock(block);

        // BBF_INTERNAL blocks don't correspond to any single IL instruction.
        if (compiler->opts.compDbgInfo && (block->bbFlags & BBF_INTERNAL) &&
            !compiler->fgBBisScratch(block)) // If the block is the distinguished first scratch block, then no need to
                                             // emit a NO_MAPPING entry, immediately after the prolog.
        {
            genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::NO_MAPPING, true);
        }

        bool firstMapping = true;
#endif // DEBUGGING_SUPPORT

        /*---------------------------------------------------------------------
         *
         *  Generate code for each statement-tree in the block
         *
         */

        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            genReserveFuncletProlog(block);
        }

        // Clear compCurStmt and compCurLifeTree.
        compiler->compCurStmt     = nullptr;
        compiler->compCurLifeTree = nullptr;

        // Traverse the block in linear order, generating code for each node as we
        // as we encounter it.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUGGING_SUPPORT
        IL_OFFSETX currentILOffset = BAD_IL_OFFSET;
#endif
        for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
        {
#ifdef DEBUGGING_SUPPORT
            // Do we have a new IL offset?
            if (node->OperGet() == GT_IL_OFFSET)
            {
                genEnsureCodeEmitted(currentILOffset);
                currentILOffset = node->gtStmt.gtStmtILoffsx;
                genIPmappingAdd(currentILOffset, firstMapping);
                firstMapping = false;
            }
#endif // DEBUGGING_SUPPORT

#ifdef DEBUG
            if (node->OperGet() == GT_IL_OFFSET)
            {
                noway_assert(node->gtStmt.gtStmtLastILoffs <= compiler->info.compILCodeSize ||
                             node->gtStmt.gtStmtLastILoffs == BAD_IL_OFFSET);

                if (compiler->opts.dspCode && compiler->opts.dspInstrs &&
                    node->gtStmt.gtStmtLastILoffs != BAD_IL_OFFSET)
                {
                    while (genCurDispOffset <= node->gtStmt.gtStmtLastILoffs)
                    {
                        genCurDispOffset += dumpSingleInstr(compiler->info.compCode, genCurDispOffset, ">    ");
                    }
                }
            }
#endif // DEBUG

            genCodeForTreeNode(node);
            if (node->gtHasReg() && node->gtLsraInfo.isLocalDefUse)
            {
                genConsumeReg(node);
            }
        } // end for each node in block

#ifdef DEBUG
        // The following set of register spill checks and GC pointer tracking checks used to be
        // performed at statement boundaries. Now, with LIR, there are no statements, so they are
        // performed at the end of each block.
        // TODO: could these checks be performed more frequently? E.g., at each location where
        // the register allocator says there are no live non-variable registers. Perhaps this could
        // be done by (a) keeping a running count of live non-variable registers by using
        // gtLsraInfo.srcCount and gtLsraInfo.dstCount to decrement and increment the count, respectively,
        // and running the checks when the count is zero. Or, (b) use the map maintained by LSRA
        // (operandToLocationInfoMap) to mark a node somehow when, after the execution of that node,
        // there will be no live non-variable registers.

        regSet.rsSpillChk();

        /* Make sure we didn't bungle pointer register tracking */

        regMaskTP ptrRegs       = gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur;
        regMaskTP nonVarPtrRegs = ptrRegs & ~regSet.rsMaskVars;

        // If return is a GC-type, clear it.  Note that if a common
        // epilog is generated (genReturnBB) it has a void return
        // even though we might return a ref.  We can't use the compRetType
        // as the determiner because something we are tracking as a byref
        // might be used as a return value of a int function (which is legal)
        GenTree* blockLastNode = block->lastNode();
        if ((blockLastNode != nullptr) &&
            (blockLastNode->gtOper == GT_RETURN) &&
            (varTypeIsGC(compiler->info.compRetType) ||
             (blockLastNode->gtOp.gtOp1 != nullptr && varTypeIsGC(blockLastNode->gtOp.gtOp1->TypeGet()))))
        {
            nonVarPtrRegs &= ~RBM_INTRET;
        }

        if  (nonVarPtrRegs)
        {
            printf("Regset after BB%02u gcr=", block->bbNum);
            printRegMaskInt(gcInfo.gcRegGCrefSetCur & ~regSet.rsMaskVars);
            compiler->getEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur & ~regSet.rsMaskVars);
            printf(", byr=");
            printRegMaskInt(gcInfo.gcRegByrefSetCur & ~regSet.rsMaskVars);
            compiler->getEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur & ~regSet.rsMaskVars);
            printf(", regVars=");
            printRegMaskInt(regSet.rsMaskVars);
            compiler->getEmitter()->emitDispRegSet(regSet.rsMaskVars);
            printf("\n");
        }

        noway_assert(nonVarPtrRegs == RBM_NONE);
#endif // DEBUG

#if defined(DEBUG) && defined(_TARGET_ARM64_)
        if (block->bbNext == nullptr)
        {
            // Unit testing of the ARM64 emitter: generate a bunch of instructions into the last block
            // (it's as good as any, but better than the prolog, which can only be a single instruction
            // group) then use COMPlus_JitLateDisasm=* to see if the late disassembler
            // thinks the instructions are the same as we do.
            genArm64EmitterUnitTests();
        }
#endif // defined(DEBUG) && defined(_TARGET_ARM64_)

#ifdef DEBUGGING_SUPPORT
        // It is possible to reach the end of the block without generating code for the current IL offset.
        // For example, if the following IR ends the current block, no code will have been generated for
        // offset 21:
        //
        //          (  0,  0) [000040] ------------                il_offset void   IL offset: 21
        //
        //     N001 (  0,  0) [000039] ------------                nop       void
        //
        // This can lead to problems when debugging the generated code. To prevent these issues, make sure
        // we've generated code for the last IL offset we saw in the block.
        genEnsureCodeEmitted(currentILOffset);

        if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
        {
            siEndBlock(block);

            /* Is this the last block, and are there any open scopes left ? */

            bool isLastBlockProcessed = (block->bbNext == NULL);
            if (block->isBBCallAlwaysPair())
            {
                isLastBlockProcessed = (block->bbNext->bbNext == NULL);
            }

            if (isLastBlockProcessed && siOpenScopeList.scNext)
            {
                /* This assert no longer holds, because we may insert a throw
                   block to demarcate the end of a try or finally region when they
                   are at the end of the method.  It would be nice if we could fix
                   our code so that this throw block will no longer be necessary. */

                // noway_assert(block->bbCodeOffsEnd != compiler->info.compILCodeSize);

                siCloseAllOpenScopes();
            }
        }

#endif // DEBUGGING_SUPPORT

        genStackLevel -= savedStkLvl;

#ifdef DEBUG
        // compCurLife should be equal to the liveOut set, except that we don't keep
        // it up to date for vars that are not register candidates
        // (it would be nice to have a xor set function)

        VARSET_TP VARSET_INIT_NOCOPY(extraLiveVars, VarSetOps::Diff(compiler, block->bbLiveOut, compiler->compCurLife));
        VarSetOps::UnionD(compiler, extraLiveVars, VarSetOps::Diff(compiler, compiler->compCurLife, block->bbLiveOut));
        VARSET_ITER_INIT(compiler, extraLiveVarIter, extraLiveVars, extraLiveVarIndex);
        while (extraLiveVarIter.NextElem(compiler, &extraLiveVarIndex))
        {
            unsigned   varNum = compiler->lvaTrackedToVarNum[extraLiveVarIndex];
            LclVarDsc* varDsc = compiler->lvaTable + varNum;
            assert(!varDsc->lvIsRegCandidate());
        }
#endif

        /* Both stacks should always be empty on exit from a basic block */

        noway_assert(genStackLevel == 0);

#if 0
        // On AMD64, we need to generate a NOP after a call that is the last instruction of the block, in several
        // situations, to support proper exception handling semantics. This is mostly to ensure that when the stack
        // walker computes an instruction pointer for a frame, that instruction pointer is in the correct EH region.
        // The document "X64 and ARM ABIs.docx" has more details. The situations:
        // 1. If the call instruction is in a different EH region as the instruction that follows it.
        // 2. If the call immediately precedes an OS epilog. (Note that what the JIT or VM consider an epilog might
        //    be slightly different from what the OS considers an epilog, and it is the OS-reported epilog that matters here.)
        // We handle case #1 here, and case #2 in the emitter.
        if (getEmitter()->emitIsLastInsCall())
        {
            // Ok, the last instruction generated is a call instruction. Do any of the other conditions hold?
            // Note: we may be generating a few too many NOPs for the case of call preceding an epilog. Technically,
            // if the next block is a BBJ_RETURN, an epilog will be generated, but there may be some instructions
            // generated before the OS epilog starts, such as a GS cookie check.
            if ((block->bbNext == nullptr) ||
                !BasicBlock::sameEHRegion(block, block->bbNext))
            {
                // We only need the NOP if we're not going to generate any more code as part of the block end.

                switch (block->bbJumpKind)
                {
                case BBJ_ALWAYS:
                case BBJ_THROW:
                case BBJ_CALLFINALLY:
                case BBJ_EHCATCHRET:
                    // We're going to generate more code below anyway, so no need for the NOP.

                case BBJ_RETURN:
                case BBJ_EHFINALLYRET:
                case BBJ_EHFILTERRET:
                    // These are the "epilog follows" case, handled in the emitter.

                    break;

                case BBJ_NONE:
                    if (block->bbNext == nullptr)
                    {
                        // Call immediately before the end of the code; we should never get here    .
                        instGen(INS_BREAKPOINT); // This should never get executed
                    }
                    else
                    {
                        // We need the NOP
                        instGen(INS_nop);
                    }
                    break;

                case BBJ_COND:
                case BBJ_SWITCH:
                    // These can't have a call as the last instruction!

                default:
                    noway_assert(!"Unexpected bbJumpKind");
                    break;
                }
            }
        }
#endif // 0

        /* Do we need to generate a jump or return? */

        switch (block->bbJumpKind)
        {
            case BBJ_ALWAYS:
                inst_JMP(EJ_jmp, block->bbJumpDest);
                break;

            case BBJ_RETURN:
                genExitCode(block);
                break;

            case BBJ_THROW:
                // If we have a throw at the end of a function or funclet, we need to emit another instruction
                // afterwards to help the OS unwinder determine the correct context during unwind.
                // We insert an unexecuted breakpoint instruction in several situations
                // following a throw instruction:
                // 1. If the throw is the last instruction of the function or funclet. This helps
                //    the OS unwinder determine the correct context during an unwind from the
                //    thrown exception.
                // 2. If this is this is the last block of the hot section.
                // 3. If the subsequent block is a special throw block.
                // 4. On AMD64, if the next block is in a different EH region.
                if ((block->bbNext == NULL) || (block->bbNext->bbFlags & BBF_FUNCLET_BEG) ||
                    !BasicBlock::sameEHRegion(block, block->bbNext) ||
                    (!isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block->bbNext)) ||
                    block->bbNext == compiler->fgFirstColdBlock)
                {
                    instGen(INS_BREAKPOINT); // This should never get executed
                }

                break;

            case BBJ_CALLFINALLY:

                // Generate a call to the finally, like this:
                //      mov         x0,qword ptr [fp + 10H]         // Load x0 with PSPSym
                //      bl          finally-funclet
                //      b           finally-return                  // Only for non-retless finally calls
                // The 'b' can be a NOP if we're going to the next block.

                getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, REG_R0, compiler->lvaPSPSym, 0);
                getEmitter()->emitIns_J(INS_bl_local, block->bbJumpDest);

                if (block->bbFlags & BBF_RETLESS_CALL)
                {
                    // We have a retless call, and the last instruction generated was a call.
                    // If the next block is in a different EH region (or is the end of the code
                    // block), then we need to generate a breakpoint here (since it will never
                    // get executed) to get proper unwind behavior.

                    if ((block->bbNext == nullptr) || !BasicBlock::sameEHRegion(block, block->bbNext))
                    {
                        instGen(INS_BREAKPOINT); // This should never get executed
                    }
                }
                else
                {
                    // Because of the way the flowgraph is connected, the liveness info for this one instruction
                    // after the call is not (can not be) correct in cases where a variable has a last use in the
                    // handler.  So turn off GC reporting for this single instruction.
                    getEmitter()->emitDisableGC();

                    // Now go to where the finally funclet needs to return to.
                    if (block->bbNext->bbJumpDest == block->bbNext->bbNext)
                    {
                        // Fall-through.
                        // TODO-ARM64-CQ: Can we get rid of this instruction, and just have the call return directly
                        // to the next instruction? This would depend on stack walking from within the finally
                        // handler working without this instruction being in this special EH region.
                        instGen(INS_nop);
                    }
                    else
                    {
                        inst_JMP(EJ_jmp, block->bbNext->bbJumpDest);
                    }

                    getEmitter()->emitEnableGC();
                }

                // The BBJ_ALWAYS is used because the BBJ_CALLFINALLY can't point to the
                // jump target using bbJumpDest - that is already used to point
                // to the finally block. So just skip past the BBJ_ALWAYS unless the
                // block is RETLESS.
                if (!(block->bbFlags & BBF_RETLESS_CALL))
                {
                    assert(block->isBBCallAlwaysPair());

                    lblk  = block;
                    block = block->bbNext;
                }
                break;

            case BBJ_EHCATCHRET:
                // For long address (default): `adrp + add` will be emitted.
                // For short address (proven later): `adr` will be emitted.
                getEmitter()->emitIns_R_L(INS_adr, EA_PTRSIZE, block->bbJumpDest, REG_INTRET);

                __fallthrough;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genReserveFuncletEpilog(block);
                break;

            case BBJ_NONE:
            case BBJ_COND:
            case BBJ_SWITCH:
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

#ifdef DEBUG
        compiler->compCurBB = 0;
#endif

    } //------------------ END-FOR each block of the method -------------------

    /* Nothing is live at this point */
    genUpdateLife(VarSetOps::MakeEmpty(compiler));

    /* Finalize the spill  tracking logic */

    regSet.rsSpillEnd();

    /* Finalize the temp   tracking logic */

    compiler->tmpEnd();

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\n# ");
        printf("compCycleEstimate = %6d, compSizeEstimate = %5d ", compiler->compCycleEstimate, compiler->compSizeEstimate);
        printf("%s\n", compiler->info.compFullName);
    }
#endif
}

// return the child that has the same reg as the dst (if any)
// other child returned (out param) in 'other'
// TODO-Cleanup: move to CodeGenCommon.cpp
GenTree* sameRegAsDst(GenTree* tree, GenTree*& other /*out*/)
{
    if (tree->gtRegNum == REG_NA)
    {
        other = nullptr;
        return NULL;
    }

    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtOp.gtOp2;
    if (op1->gtRegNum == tree->gtRegNum)
    {
        other = op2;
        return op1;
    }
    if (op2->gtRegNum == tree->gtRegNum)
    {
        other = op1;
        return op2;
    }
    else
    {
        other = nullptr;
        return NULL;
    }
}

//  move an immediate value into an integer register

void CodeGen::instGen_Set_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm, insFlags flags)
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
        getEmitter()->emitIns_R_AI(INS_adrp, size, reg, imm);
    }
    else if (imm == 0)
    {
        instGen_Set_Reg_To_Zero(size, reg, flags);
    }
    else
    {
        if (emitter::emitIns_valid_imm_for_mov(imm, size))
        {
            getEmitter()->emitIns_R_I(INS_mov, size, reg, imm);
        }
        else
        {
            getEmitter()->emitIns_R_I(INS_mov, size, reg, (imm & 0xffff));
            getEmitter()->emitIns_R_I_I(INS_movk, size, reg, ((imm >> 16) & 0xffff), 16, INS_OPTS_LSL);

            if ((size == EA_8BYTE) &&
                ((imm >> 32) != 0)) // Sometimes the upper 32 bits are zero and the first mov has zero-ed them
            {
                getEmitter()->emitIns_R_I_I(INS_movk, EA_8BYTE, reg, ((imm >> 32) & 0xffff), 32, INS_OPTS_LSL);
                if ((imm >> 48) != 0) // Frequently the upper 16 bits are zero and the first mov has zero-ed them
                {
                    getEmitter()->emitIns_R_I_I(INS_movk, EA_8BYTE, reg, ((imm >> 48) & 0xffff), 48, INS_OPTS_LSL);
                }
            }
        }
        // The caller may have requested that the flags be set on this mov (rarely/never)
        if (flags == INS_FLAGS_SET)
        {
            getEmitter()->emitIns_R_I(INS_tst, size, reg, 0);
        }
    }

    regTracker.rsTrackRegIntCns(reg, imm);
}

/***********************************************************************************
 *
 * Generate code to set a register 'targetReg' of type 'targetType' to the constant
 * specified by the constant (GT_CNS_INT or GT_CNS_DBL) in 'tree'. This does not call
 * genProduceReg() on the target register.
 */
void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, GenTreePtr tree)
{
    switch (tree->gtOper)
    {
        case GT_CNS_INT:
        {
            // relocatable values tend to come down as a CNS_INT of native int type
            // so the line between these two opcodes is kind of blurry
            GenTreeIntConCommon* con    = tree->AsIntConCommon();
            ssize_t              cnsVal = con->IconValue();

            bool needReloc = compiler->opts.compReloc && tree->IsIconHandle();
            if (needReloc)
            {
                instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, targetReg, cnsVal);
                regTracker.rsTrackRegTrash(targetReg);
            }
            else
            {
                genSetRegToIcon(targetReg, cnsVal, targetType);
            }
        }
        break;

        case GT_CNS_DBL:
        {
            emitter*       emit       = getEmitter();
            emitAttr       size       = emitTypeSize(tree);
            GenTreeDblCon* dblConst   = tree->AsDblCon();
            double         constValue = dblConst->gtDblCon.gtDconVal;

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
                regMaskTP addrRegMask = tree->gtRsvdRegs;
                regNumber addrReg     = genRegNumFromMask(addrRegMask);
                noway_assert(addrReg != REG_NA);

                // We must load the FP constant from the constant pool
                // Emit a data section constant for the float or double constant.
                CORINFO_FIELD_HANDLE hnd = emit->emitFltOrDblConst(dblConst);
                // For long address (default): `adrp + ldr + fmov` will be emitted.
                // For short address (proven later), `ldr` will be emitted.
                emit->emitIns_R_C(INS_ldr, size, targetReg, addrReg, hnd, 0);
            }
        }
        break;

        default:
            unreached();
    }
}

// Generate code to get the high N bits of a N*N=2N bit multiplication result
void CodeGen::genCodeForMulHi(GenTreeOp* treeNode)
{
    assert(!(treeNode->gtFlags & GTF_UNSIGNED));
    assert(!treeNode->gtOverflowEx());

#if 0
    regNumber targetReg  = treeNode->gtRegNum;
    var_types targetType = treeNode->TypeGet();
    emitter *emit        = getEmitter();
    emitAttr size        = emitTypeSize(treeNode);
    GenTree *op1         = treeNode->gtOp.gtOp1;
    GenTree *op2         = treeNode->gtOp.gtOp2;

    // to get the high bits of the multiply, we are constrained to using the
    // 1-op form:  RDX:RAX = RAX * rm
    // The 3-op form (Rx=Ry*Rz) does not support it.

    genConsumeOperands(treeNode->AsOp());

    GenTree* regOp = op1;
    GenTree* rmOp  = op2; 
            
    // Set rmOp to the contained memory operand (if any)
    //
    if (op1->isContained() || (!op2->isContained() && (op2->gtRegNum == targetReg)))
    {
        regOp = op2;
        rmOp  = op1;
    }
    assert(!regOp->isContained());
            
    // Setup targetReg when neither of the source operands was a matching register
    if (regOp->gtRegNum != targetReg)
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, regOp->gtRegNum, targetType);
    }
            
    emit->emitInsBinary(INS_imulEAX, size, treeNode, rmOp);
            
    // Move the result to the desired register, if necessary
    if (targetReg != REG_RDX)
    {
        inst_RV_RV(INS_mov, targetReg, REG_RDX, targetType);
    }
#else  // !0
    NYI("genCodeForMulHi");
#endif // !0
}

// generate code for a DIV or MOD operation
//
void CodeGen::genCodeForDivMod(GenTreeOp* treeNode)
{
    // unused on ARM64
}

// Generate code for ADD, SUB, MUL, DIV, UDIV, AND, OR and XOR
// This method is expected to have called genConsumeOperands() before calling it.
void CodeGen::genCodeForBinary(GenTree* treeNode)
{
    const genTreeOps oper       = treeNode->OperGet();
    regNumber        targetReg  = treeNode->gtRegNum;
    var_types        targetType = treeNode->TypeGet();
    emitter*         emit       = getEmitter();

    assert(oper == GT_ADD || oper == GT_SUB || oper == GT_MUL || oper == GT_DIV || oper == GT_UDIV || oper == GT_AND ||
           oper == GT_OR || oper == GT_XOR);

    GenTreePtr  op1 = treeNode->gtGetOp1();
    GenTreePtr  op2 = treeNode->gtGetOp2();
    instruction ins = genGetInsForOper(treeNode->OperGet(), targetType);

    // The arithmetic node must be sitting in a register (since it's not contained)
    noway_assert(targetReg != REG_NA);

    regNumber r = emit->emitInsTernary(ins, emitTypeSize(treeNode), treeNode, op1, op2);
    noway_assert(r == targetReg);

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// isStructReturn: Returns whether the 'treeNode' is returning a struct.
//
// Arguments:
//    treeNode - The tree node to evaluate whether is a struct return.
//
// Return Value:
//    Returns true if the 'treeNode" is a GT_RETURN node of type struct.
//    Otherwise returns false.
//
bool CodeGen::isStructReturn(GenTreePtr treeNode)
{
    // This method could be called for 'treeNode' of GT_RET_FILT or GT_RETURN.
    // For the GT_RET_FILT, the return is always
    // a bool or a void, for the end of a finally block.
    noway_assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);

    return varTypeIsStruct(treeNode);
}

//------------------------------------------------------------------------
// genStructReturn: Generates code for returning a struct.
//
// Arguments:
//    treeNode - The GT_RETURN tree node.
//
// Return Value:
//    None
//
// Assumption:
//    op1 of GT_RETURN node is either GT_LCL_VAR or multi-reg GT_CALL
void CodeGen::genStructReturn(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN);
    assert(isStructReturn(treeNode));
    GenTreePtr op1 = treeNode->gtGetOp1();

    if (op1->OperGet() == GT_LCL_VAR)
    {
        GenTreeLclVarCommon* lclVar  = op1->AsLclVarCommon();
        LclVarDsc*           varDsc  = &(compiler->lvaTable[lclVar->gtLclNum]);
        var_types            lclType = genActualType(varDsc->TypeGet());

        // Currently only multireg TYP_STRUCT types such as HFA's and 16-byte structs are supported
        // In the future we could have FEATURE_SIMD types like TYP_SIMD16
        assert(lclType == TYP_STRUCT);
        assert(varDsc->lvIsMultiRegRet);

        ReturnTypeDesc retTypeDesc;
        unsigned       regCount;

        retTypeDesc.InitializeStructReturnType(compiler, varDsc->lvVerTypeInfo.GetClassHandle());
        regCount = retTypeDesc.GetReturnRegCount();

        assert(regCount >= 2);
        assert(op1->isContained());

        // Copy var on stack into ABI return registers
        int offset = 0;
        for (unsigned i = 0; i < regCount; ++i)
        {
            var_types type = retTypeDesc.GetReturnRegType(i);
            regNumber reg  = retTypeDesc.GetABIReturnReg(i);
            getEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), reg, lclVar->gtLclNum, offset);
            offset += genTypeSize(type);
        }
    }
    else // op1 must be multi-reg GT_CALL
    {
        assert(op1->IsMultiRegCall() || op1->IsCopyOrReloadOfMultiRegCall());

        genConsumeRegs(op1);

        GenTree*     actualOp1 = op1->gtSkipReloadOrCopy();
        GenTreeCall* call      = actualOp1->AsCall();

        ReturnTypeDesc* pRetTypeDesc;
        unsigned        regCount;
        unsigned        matchingCount = 0;

        pRetTypeDesc = call->GetReturnTypeDesc();
        regCount     = pRetTypeDesc->GetReturnRegCount();

        var_types regType[MAX_RET_REG_COUNT];
        regNumber returnReg[MAX_RET_REG_COUNT];
        regNumber allocatedReg[MAX_RET_REG_COUNT];
        regMaskTP srcRegsMask       = 0;
        regMaskTP dstRegsMask       = 0;
        bool      needToShuffleRegs = false; // Set to true if we have to move any registers

        for (unsigned i = 0; i < regCount; ++i)
        {
            regType[i]   = pRetTypeDesc->GetReturnRegType(i);
            returnReg[i] = pRetTypeDesc->GetABIReturnReg(i);

            regNumber reloadReg = REG_NA;
            if (op1->IsCopyOrReload())
            {
                // GT_COPY/GT_RELOAD will have valid reg for those positions
                // that need to be copied or reloaded.
                reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(i);
            }

            if (reloadReg != REG_NA)
            {
                allocatedReg[i] = reloadReg;
            }
            else
            {
                allocatedReg[i] = call->GetRegNumByIdx(i);
            }

            if (returnReg[i] == allocatedReg[i])
            {
                matchingCount++;
            }
            else // We need to move this value
            {
                // We want to move the value from allocatedReg[i] into returnReg[i]
                // so record these two registers in the src and dst masks
                //
                srcRegsMask |= genRegMask(allocatedReg[i]);
                dstRegsMask |= genRegMask(returnReg[i]);

                needToShuffleRegs = true;
            }
        }

        if (needToShuffleRegs)
        {
            assert(matchingCount < regCount);

            unsigned  remainingRegCount = regCount - matchingCount;
            regMaskTP extraRegMask      = treeNode->gtRsvdRegs;

            while (remainingRegCount > 0)
            {
                // set 'available' to the 'dst' registers that are not currently holding 'src' registers
                //
                regMaskTP availableMask = dstRegsMask & ~srcRegsMask;

                regMaskTP dstMask;
                regNumber srcReg;
                regNumber dstReg;
                var_types curType   = TYP_UNKNOWN;
                regNumber freeUpReg = REG_NA;

                if (availableMask == 0)
                {
                    // Circular register dependencies
                    // So just free up the lowest register in dstRegsMask by moving it to the 'extra' register

                    assert(dstRegsMask == srcRegsMask);         // this has to be true for us to reach here
                    assert(extraRegMask != 0);                  // we require an 'extra' register
                    assert((extraRegMask & ~dstRegsMask) != 0); // it can't be part of dstRegsMask

                    availableMask = extraRegMask & ~dstRegsMask;

                    regMaskTP srcMask = genFindLowestBit(srcRegsMask);
                    freeUpReg         = genRegNumFromMask(srcMask);
                }

                dstMask = genFindLowestBit(availableMask);
                dstReg  = genRegNumFromMask(dstMask);
                srcReg  = REG_NA;

                if (freeUpReg != REG_NA)
                {
                    // We will free up the srcReg by moving it to dstReg which is an extra register
                    //
                    srcReg = freeUpReg;

                    // Find the 'srcReg' and set 'curType', change allocatedReg[] to dstReg
                    // and add the new register mask bit to srcRegsMask
                    //
                    for (unsigned i = 0; i < regCount; ++i)
                    {
                        if (allocatedReg[i] == srcReg)
                        {
                            curType         = regType[i];
                            allocatedReg[i] = dstReg;
                            srcRegsMask |= genRegMask(dstReg);
                        }
                    }
                }
                else // The normal case
                {
                    // Find the 'srcReg' and set 'curType'
                    //
                    for (unsigned i = 0; i < regCount; ++i)
                    {
                        if (returnReg[i] == dstReg)
                        {
                            srcReg  = allocatedReg[i];
                            curType = regType[i];
                        }
                    }
                    // After we perform this move we will have one less registers to setup
                    remainingRegCount--;
                }
                assert(curType != TYP_UNKNOWN);

                inst_RV_RV(ins_Copy(curType), dstReg, srcReg, curType);

                // Clear the appropriate bits in srcRegsMask and dstRegsMask
                srcRegsMask &= ~genRegMask(srcReg);
                dstRegsMask &= ~genRegMask(dstReg);

            } // while (remainingRegCount > 0)

        } // (needToShuffleRegs)

    } // op1 must be multi-reg GT_CALL
}

//------------------------------------------------------------------------
// genReturn: Generates code for return statement.
//            In case of struct return, delegates to the genStructReturn method.
//
// Arguments:
//    treeNode - The GT_RETURN or GT_RETFILT tree node.
//
// Return Value:
//    None
//
void CodeGen::genReturn(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    GenTreePtr op1        = treeNode->gtGetOp1();
    var_types  targetType = treeNode->TypeGet();

#ifdef DEBUG
    if (targetType == TYP_VOID)
    {
        assert(op1 == nullptr);
    }
#endif

    if (isStructReturn(treeNode))
    {
        genStructReturn(treeNode);
    }
    else if (targetType != TYP_VOID)
    {
        assert(op1 != nullptr);
        noway_assert(op1->gtRegNum != REG_NA);

        genConsumeReg(op1);

        regNumber retReg = varTypeIsFloating(treeNode) ? REG_FLOATRET : REG_INTRET;

        bool movRequired = (op1->gtRegNum != retReg);

        if (!movRequired)
        {
            if (op1->OperGet() == GT_LCL_VAR)
            {
                GenTreeLclVarCommon* lcl            = op1->AsLclVarCommon();
                bool                 isRegCandidate = compiler->lvaTable[lcl->gtLclNum].lvIsRegCandidate();
                if (isRegCandidate && ((op1->gtFlags & GTF_SPILLED) == 0))
                {
                    assert(op1->InReg());

                    // We may need to generate a zero-extending mov instruction to load the value from this GT_LCL_VAR

                    unsigned   lclNum  = lcl->gtLclNum;
                    LclVarDsc* varDsc  = &(compiler->lvaTable[lclNum]);
                    var_types  op1Type = genActualType(op1->TypeGet());
                    var_types  lclType = genActualType(varDsc->TypeGet());

                    if (genTypeSize(op1Type) < genTypeSize(lclType))
                    {
                        movRequired = true;
                    }
                }
            }
        }

        if (movRequired)
        {
            emitAttr movSize = EA_ATTR(genTypeSize(targetType));
            getEmitter()->emitIns_R_R(INS_mov, movSize, retReg, op1->gtRegNum);
        }
    }

#ifdef PROFILING_SUPPORTED
    // There will be a single return block while generating profiler ELT callbacks.
    //
    // Reason for not materializing Leave callback as a GT_PROF_HOOK node after GT_RETURN:
    // In flowgraph and other places assert that the last node of a block marked as
    // GT_RETURN is either a GT_RETURN or GT_JMP or a tail call.  It would be nice to
    // maintain such an invariant irrespective of whether profiler hook needed or not.
    // Also, there is not much to be gained by materializing it as an explicit node.
    if (compiler->compCurBB == compiler->genReturnBB)
    {
        genProfilingLeaveCallback();
    }
#endif
}

/*****************************************************************************
 *
 * Generate code for a single node in the tree.
 * Preconditions: All operands have been evaluated
 *
 */
void CodeGen::genCodeForTreeNode(GenTreePtr treeNode)
{
    regNumber targetReg  = treeNode->gtRegNum;
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = getEmitter();

#ifdef DEBUG
    if (compiler->verbose)
    {
        unsigned seqNum = treeNode->gtSeqNum; // Useful for setting a conditional break in Visual Studio
        printf("Generating: ");
        compiler->gtDispTree(treeNode, nullptr, nullptr, true);
    }
#endif // DEBUG

    // Is this a node whose value is already in a register?  LSRA denotes this by
    // setting the GTF_REUSE_REG_VAL flag.
    if (treeNode->IsReuseRegVal())
    {
        // For now, this is only used for constant nodes.
        assert((treeNode->OperGet() == GT_CNS_INT) || (treeNode->OperGet() == GT_CNS_DBL));
        JITDUMP("  TreeNode is marked ReuseReg\n");
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
        case GT_START_NONGC:
            getEmitter()->emitDisableGC();
            break;

        case GT_PROF_HOOK:
            // We should be seeing this only if profiler hook is needed
            noway_assert(compiler->compIsProfilerHookNeeded());

#ifdef PROFILING_SUPPORTED
            // Right now this node is used only for tail calls. In future if
            // we intend to use it for Enter or Leave hooks, add a data member
            // to this node indicating the kind of profiler hook. For example,
            // helper number can be used.
            genProfilingLeaveCallback(CORINFO_HELP_PROF_FCN_TAILCALL);
#endif // PROFILING_SUPPORTED
            break;

        case GT_LCLHEAP:
            genLclHeap(treeNode);
            break;

        case GT_CNS_INT:
        case GT_CNS_DBL:
            genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
            break;

        case GT_NOT:
            assert(!varTypeIsFloating(targetType));

            __fallthrough;

        case GT_NEG:
        {
            instruction ins = genGetInsForOper(treeNode->OperGet(), targetType);

            // The arithmetic node must be sitting in a register (since it's not contained)
            assert(!treeNode->isContained());
            // The dst can only be a register.
            assert(targetReg != REG_NA);

            GenTreePtr operand = treeNode->gtGetOp1();
            assert(!operand->isContained());
            // The src must be a register.
            regNumber operandReg = genConsumeReg(operand);

            getEmitter()->emitIns_R_R(ins, emitTypeSize(treeNode), targetReg, operandReg);
        }
            genProduceReg(treeNode);
            break;

        case GT_DIV:
        case GT_UDIV:
            genConsumeOperands(treeNode->AsOp());

            if (varTypeIsFloating(targetType))
            {
                // Floating point divide never raises an exception
                genCodeForBinary(treeNode);
            }
            else // an integer divide operation
            {
                GenTreePtr divisorOp = treeNode->gtGetOp2();
                emitAttr   size      = EA_ATTR(genTypeSize(genActualType(treeNode->TypeGet())));

                if (divisorOp->IsIntegralConst(0))
                {
                    // We unconditionally throw a divide by zero exception
                    genJumpToThrowHlpBlk(EJ_jmp, SCK_DIV_BY_ZERO);

                    // We still need to call genProduceReg
                    genProduceReg(treeNode);
                }
                else // the divisor is not the constant zero
                {
                    regNumber divisorReg = divisorOp->gtRegNum;

                    // Generate the require runtime checks for GT_DIV or GT_UDIV
                    if (treeNode->gtOper == GT_DIV)
                    {
                        BasicBlock* sdivLabel = genCreateTempLabel();

                        // Two possible exceptions:
                        //     (AnyVal /  0) => DivideByZeroException
                        //     (MinInt / -1) => ArithmeticException
                        //
                        bool checkDividend = true;

                        // Do we have an immediate for the 'divisorOp'?
                        //
                        if (divisorOp->IsCnsIntOrI())
                        {
                            GenTreeIntConCommon* intConstTree  = divisorOp->AsIntConCommon();
                            ssize_t              intConstValue = intConstTree->IconValue();
                            assert(intConstValue != 0); // already checked above by IsIntegralConst(0))
                            if (intConstValue != -1)
                            {
                                checkDividend = false; // We statically know that the dividend is not -1
                            }
                        }
                        else // insert check for divison by zero
                        {
                            // Check if the divisor is zero throw a DivideByZeroException
                            emit->emitIns_R_I(INS_cmp, size, divisorReg, 0);
                            emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
                            genJumpToThrowHlpBlk(jmpEqual, SCK_DIV_BY_ZERO);
                        }

                        if (checkDividend)
                        {
                            // Check if the divisor is not -1 branch to 'sdivLabel'
                            emit->emitIns_R_I(INS_cmp, size, divisorReg, -1);

                            emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
                            inst_JMP(jmpNotEqual, sdivLabel);
                            // If control flow continues past here the 'divisorReg' is known to be -1

                            regNumber dividendReg = treeNode->gtGetOp1()->gtRegNum;
                            // At this point the divisor is known to be -1
                            //
                            // Issue the 'adds  zr, dividendReg, dividendReg' instruction
                            // this will set both the Z and V flags only when dividendReg is MinInt
                            //
                            emit->emitIns_R_R_R(INS_adds, size, REG_ZR, dividendReg, dividendReg);
                            inst_JMP(jmpNotEqual, sdivLabel);             // goto sdiv if the Z flag is clear
                            genJumpToThrowHlpBlk(EJ_vs, SCK_ARITH_EXCPN); // if the V flags is set throw
                                                                          // ArithmeticException

                            genDefineTempLabel(sdivLabel);
                        }
                        genCodeForBinary(treeNode); // Generate the sdiv instruction
                    }
                    else // (treeNode->gtOper == GT_UDIV)
                    {
                        // Only one possible exception
                        //     (AnyVal /  0) => DivideByZeroException
                        //
                        // Note that division by the constant 0 was already checked for above by the
                        // op2->IsIntegralConst(0) check
                        //
                        if (!divisorOp->IsCnsIntOrI())
                        {
                            // divisorOp is not a constant, so it could be zero
                            //
                            emit->emitIns_R_I(INS_cmp, size, divisorReg, 0);
                            emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
                            genJumpToThrowHlpBlk(jmpEqual, SCK_DIV_BY_ZERO);
                        }
                        genCodeForBinary(treeNode);
                    }
                }
            }
            break;

        case GT_OR:
        case GT_XOR:
        case GT_AND:
            assert(varTypeIsIntegralOrI(treeNode));
            __fallthrough;
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
            genConsumeOperands(treeNode->AsOp());
            genCodeForBinary(treeNode);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            genCodeForShift(treeNode);
            // genCodeForShift() calls genProduceReg()
            break;

        case GT_CAST:
            if (varTypeIsFloating(targetType) && varTypeIsFloating(treeNode->gtOp.gtOp1))
            {
                // Casts float/double <--> double/float
                genFloatToFloatCast(treeNode);
            }
            else if (varTypeIsFloating(treeNode->gtOp.gtOp1))
            {
                // Casts float/double --> int32/int64
                genFloatToIntCast(treeNode);
            }
            else if (varTypeIsFloating(targetType))
            {
                // Casts int32/uint32/int64/uint64 --> float/double
                genIntToFloatCast(treeNode);
            }
            else
            {
                // Casts int <--> int
                genIntToIntCast(treeNode);
            }
            // The per-case functions call genProduceReg()
            break;

        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR_ADDR:
            // Address of a local var.  This by itself should never be allocated a register.
            // If it is worth storing the address in a register then it should be cse'ed into
            // a temp and that would be allocated a register.
            noway_assert(targetType == TYP_BYREF);
            noway_assert(!treeNode->InReg());

            inst_RV_TT(INS_lea, targetReg, treeNode, 0, EA_BYREF);
            genProduceReg(treeNode);
            break;

        case GT_LCL_FLD:
        {
            GenTreeLclVarCommon* varNode = treeNode->AsLclVarCommon();
            assert(varNode->gtLclNum < compiler->lvaCount);
            unsigned   varNum = varNode->gtLclNum;
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

            if (targetType == TYP_STRUCT)
            {
                NYI("GT_LCL_FLD with TYP_STRUCT");
            }
            emitAttr size = emitTypeSize(targetType);

            noway_assert(targetType != TYP_STRUCT);
            noway_assert(targetReg != REG_NA);

            unsigned offset = treeNode->gtLclFld.gtLclOffs;

            if (varTypeIsFloating(targetType))
            {
                if (treeNode->InReg())
                {
                    NYI("GT_LCL_FLD with register to register Floating point move");
                }
                else
                {
                    emit->emitIns_R_S(ins_Load(targetType), size, targetReg, varNum, offset);
                }
            }
            else
            {
                size = EA_SET_SIZE(size, EA_8BYTE);
                emit->emitIns_R_S(ins_Move_Extend(targetType, treeNode->InReg()), size, targetReg, varNum, offset);
            }
            genProduceReg(treeNode);
        }
        break;

        case GT_LCL_VAR:
        {
            GenTreeLclVarCommon* varNode = treeNode->AsLclVarCommon();

            unsigned varNum = varNode->gtLclNum;
            assert(varNum < compiler->lvaCount);
            LclVarDsc* varDsc         = &(compiler->lvaTable[varNum]);
            bool       isRegCandidate = varDsc->lvIsRegCandidate();

            // lcl_vars are not defs
            assert((treeNode->gtFlags & GTF_VAR_DEF) == 0);

            if (isRegCandidate && !(treeNode->gtFlags & GTF_VAR_DEATH))
            {
                assert((treeNode->InReg()) || (treeNode->gtFlags & GTF_SPILLED));
            }

            // If this is a register candidate that has been spilled, genConsumeReg() will
            // reload it at the point of use.  Otherwise, if it's not in a register, we load it here.

            if (!treeNode->InReg() && !(treeNode->gtFlags & GTF_SPILLED))
            {
                assert(!isRegCandidate);

                // targetType must be a normal scalar type and not a TYP_STRUCT
                assert(targetType != TYP_STRUCT);

                instruction ins  = ins_Load(targetType);
                emitAttr    attr = emitTypeSize(targetType);

                attr = emit->emitInsAdjustLoadStoreAttr(ins, attr);

                emit->emitIns_R_S(ins, attr, targetReg, varNum, 0);
                genProduceReg(treeNode);
            }
        }
        break;

        case GT_STORE_LCL_FLD:
        {
            noway_assert(targetType != TYP_STRUCT);

            // record the offset
            unsigned offset = treeNode->gtLclFld.gtLclOffs;

            // We must have a stack store with GT_STORE_LCL_FLD
            noway_assert(!treeNode->InReg());
            noway_assert(targetReg == REG_NA);

            GenTreeLclVarCommon* varNode = treeNode->AsLclVarCommon();
            unsigned             varNum  = varNode->gtLclNum;
            assert(varNum < compiler->lvaCount);
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

            // Ensure that lclVar nodes are typed correctly.
            assert(!varDsc->lvNormalizeOnStore() || targetType == genActualType(varDsc->TypeGet()));

            GenTreePtr data = treeNode->gtOp.gtOp1->gtEffectiveVal();
            genConsumeRegs(data);

            regNumber dataReg = REG_NA;
            if (data->isContainedIntOrIImmed())
            {
                assert(data->IsIntegralConst(0));
                dataReg = REG_ZR;
            }
            else
            {
                assert(!data->isContained());
                genConsumeReg(data);
                dataReg = data->gtRegNum;
            }
            assert(dataReg != REG_NA);

            instruction ins = ins_Store(targetType);

            emitAttr attr = emitTypeSize(targetType);

            attr = emit->emitInsAdjustLoadStoreAttr(ins, attr);

            emit->emitIns_S_R(ins, attr, dataReg, varNum, offset);

            genUpdateLife(varNode);

            varDsc->lvRegNum = REG_STK;
        }
        break;

        case GT_STORE_LCL_VAR:
        {
            GenTreeLclVarCommon* varNode = treeNode->AsLclVarCommon();

            unsigned varNum = varNode->gtLclNum;
            assert(varNum < compiler->lvaCount);
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);
            unsigned   offset = 0;

            // Ensure that lclVar nodes are typed correctly.
            assert(!varDsc->lvNormalizeOnStore() || targetType == genActualType(varDsc->TypeGet()));

            GenTreePtr data = treeNode->gtOp.gtOp1->gtEffectiveVal();

            // var = call, where call returns a multi-reg return value
            // case is handled separately.
            if (data->gtSkipReloadOrCopy()->IsMultiRegCall())
            {
                genMultiRegCallStoreToLocal(treeNode);
            }
            else
            {
                genConsumeRegs(data);

                regNumber dataReg = REG_NA;
                if (data->isContainedIntOrIImmed())
                {
                    assert(data->IsIntegralConst(0));
                    dataReg = REG_ZR;
                }
                else
                {
                    assert(!data->isContained());
                    genConsumeReg(data);
                    dataReg = data->gtRegNum;
                }
                assert(dataReg != REG_NA);

                if (targetReg == REG_NA) // store into stack based LclVar
                {
                    inst_set_SV_var(varNode);

                    instruction ins  = ins_Store(targetType);
                    emitAttr    attr = emitTypeSize(targetType);

                    attr = emit->emitInsAdjustLoadStoreAttr(ins, attr);

                    emit->emitIns_S_R(ins, attr, dataReg, varNum, offset);

                    genUpdateLife(varNode);

                    varDsc->lvRegNum = REG_STK;
                }
                else // store into register (i.e move into register)
                {
                    if (dataReg != targetReg)
                    {
                        // Assign into targetReg when dataReg (from op1) is not the same register
                        inst_RV_RV(ins_Copy(targetType), targetReg, dataReg, targetType);
                    }
                    genProduceReg(treeNode);
                }
            }
        }
        break;

        case GT_RETFILT:
            // A void GT_RETFILT is the end of a finally. For non-void filter returns we need to load the result in
            // the return register, if it's not already there. The processing is the same as GT_RETURN.
            if (targetType != TYP_VOID)
            {
                // For filters, the IL spec says the result is type int32. Further, the only specified legal values
                // are 0 or 1, with the use of other values "undefined".
                assert(targetType == TYP_INT);
            }

            __fallthrough;

        case GT_RETURN:
            genReturn(treeNode);
            break;

        case GT_LEA:
        {
            // if we are here, it is the case where there is an LEA that cannot
            // be folded into a parent instruction
            GenTreeAddrMode* lea = treeNode->AsAddrMode();
            genLeaInstruction(lea);
        }
        // genLeaInstruction calls genProduceReg()
        break;

        case GT_IND:
            genConsumeAddress(treeNode->AsIndir()->Addr());
            emit->emitInsLoadStoreOp(ins_Load(targetType), emitTypeSize(treeNode), targetReg, treeNode->AsIndir());
            genProduceReg(treeNode);
            break;

        case GT_MULHI:
            genCodeForMulHi(treeNode->AsOp());
            genProduceReg(treeNode);
            break;

        case GT_MOD:
        case GT_UMOD:
            // Integer MOD should have been morphed into a sequence of sub, mul, div in fgMorph.
            //
            // We shouldn't be seeing GT_MOD on float/double as it is morphed into a helper call by front-end.
            noway_assert(!"Codegen for GT_MOD/GT_UMOD");
            break;

        case GT_INTRINSIC:
            genIntrinsic(treeNode);
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            genSIMDIntrinsic(treeNode->AsSIMD());
            break;
#endif // FEATURE_SIMD

        case GT_CKFINITE:
            genCkfinite(treeNode);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        {
            // TODO-ARM64-CQ: Check if we can use the currently set flags.
            // TODO-ARM64-CQ: Check for the case where we can simply transfer the carry bit to a register
            //         (signed < or >= where targetReg != REG_NA)

            GenTreeOp* tree    = treeNode->AsOp();
            GenTreePtr op1     = tree->gtOp1;
            GenTreePtr op2     = tree->gtOp2;
            var_types  op1Type = op1->TypeGet();
            var_types  op2Type = op2->TypeGet();

            assert(!op1->isContainedMemoryOp());
            assert(!op2->isContainedMemoryOp());

            genConsumeOperands(tree);

            emitAttr cmpSize = EA_UNKNOWN;

            if (varTypeIsFloating(op1Type))
            {
                assert(varTypeIsFloating(op2Type));
                assert(!op1->isContained());
                assert(op1Type == op2Type);
                cmpSize = EA_ATTR(genTypeSize(op1Type));

                if (op2->IsIntegralConst(0))
                {
                    emit->emitIns_R_F(INS_fcmp, cmpSize, op1->gtRegNum, 0.0);
                }
                else
                {
                    assert(!op2->isContained());
                    emit->emitIns_R_R(INS_fcmp, cmpSize, op1->gtRegNum, op2->gtRegNum);
                }
            }
            else
            {
                assert(!varTypeIsFloating(op2Type));
                // We don't support swapping op1 and op2 to generate cmp reg, imm
                assert(!op1->isContainedIntOrIImmed());

                // TODO-ARM64-CQ: the second register argument of a CMP can be sign/zero
                // extended as part of the instruction (using "CMP (extended register)").
                // We should use that if possible, swapping operands
                // (and reversing the condition) if necessary.
                unsigned op1Size = genTypeSize(op1Type);
                unsigned op2Size = genTypeSize(op2Type);

                if ((op1Size < 4) || (op1Size < op2Size))
                {
                    // We need to sign/zero extend op1 up to 32 or 64 bits.
                    instruction ins = ins_Move_Extend(op1Type, true);
                    inst_RV_RV(ins, op1->gtRegNum, op1->gtRegNum);
                }

                if (!op2->isContainedIntOrIImmed())
                {
                    if ((op2Size < 4) || (op2Size < op1Size))
                    {
                        // We need to sign/zero extend op2 up to 32 or 64 bits.
                        instruction ins = ins_Move_Extend(op2Type, true);
                        inst_RV_RV(ins, op2->gtRegNum, op2->gtRegNum);
                    }
                }
                cmpSize = EA_4BYTE;
                if ((op1Size == EA_8BYTE) || (op2Size == EA_8BYTE))
                {
                    cmpSize = EA_8BYTE;
                }

                if (op2->isContainedIntOrIImmed())
                {
                    GenTreeIntConCommon* intConst = op2->AsIntConCommon();
                    emit->emitIns_R_I(INS_cmp, cmpSize, op1->gtRegNum, intConst->IconValue());
                }
                else
                {
                    emit->emitIns_R_R(INS_cmp, cmpSize, op1->gtRegNum, op2->gtRegNum);
                }
            }

            // Are we evaluating this into a register?
            if (targetReg != REG_NA)
            {
                genSetRegToCond(targetReg, tree);
                genProduceReg(tree);
            }
        }
        break;

        case GT_JTRUE:
        {
            GenTree* cmp = treeNode->gtOp.gtOp1->gtEffectiveVal();
            assert(cmp->OperIsCompare());
            assert(compiler->compCurBB->bbJumpKind == BBJ_COND);

            // Get the "kind" and type of the comparison.  Note that whether it is an unsigned cmp
            // is governed by a flag NOT by the inherent type of the node
            emitJumpKind jumpKind[2];
            bool         branchToTrueLabel[2];
            genJumpKindsForTree(cmp, jumpKind, branchToTrueLabel);
            assert(jumpKind[0] != EJ_NONE);

            // On Arm64 the branches will always branch to the true label
            assert(branchToTrueLabel[0]);
            inst_JMP(jumpKind[0], compiler->compCurBB->bbJumpDest);

            if (jumpKind[1] != EJ_NONE)
            {
                // the second conditional branch always has to be to the true label
                assert(branchToTrueLabel[1]);
                inst_JMP(jumpKind[1], compiler->compCurBB->bbJumpDest);
            }
        }
        break;

        case GT_RETURNTRAP:
        {
            // this is nothing but a conditional call to CORINFO_HELP_STOP_FOR_GC
            // based on the contents of 'data'

            GenTree* data = treeNode->gtOp.gtOp1;
            genConsumeRegs(data);
            emit->emitIns_R_I(INS_cmp, EA_4BYTE, data->gtRegNum, 0);

            BasicBlock* skipLabel = genCreateTempLabel();

            emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
            inst_JMP(jmpEqual, skipLabel);
            // emit the call to the EE-helper that stops for GC (or other reasons)

            genEmitHelperCall(CORINFO_HELP_STOP_FOR_GC, 0, EA_UNKNOWN);
            genDefineTempLabel(skipLabel);
        }
        break;

        case GT_STOREIND:
        {
            GenTree*                 data             = treeNode->gtOp.gtOp2;
            GenTree*                 addr             = treeNode->gtOp.gtOp1;
            GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(treeNode, data);
            if (writeBarrierForm != GCInfo::WBF_NoBarrier)
            {
                // data and addr must be in registers.
                // Consume both registers so that any copies of interfering
                // registers are taken care of.
                genConsumeOperands(treeNode->AsOp());

#if NOGC_WRITE_BARRIERS
                // At this point, we should not have any interference.
                // That is, 'data' must not be in REG_WRITE_BARRIER_DST_BYREF,
                //  as that is where 'addr' must go.
                noway_assert(data->gtRegNum != REG_WRITE_BARRIER_DST_BYREF);

                // 'addr' goes into x14 (REG_WRITE_BARRIER_DST_BYREF)
                if (addr->gtRegNum != REG_WRITE_BARRIER_DST_BYREF)
                {
                    inst_RV_RV(INS_mov, REG_WRITE_BARRIER_DST_BYREF, addr->gtRegNum, addr->TypeGet());
                }

                // 'data'  goes into x15 (REG_WRITE_BARRIER)
                if (data->gtRegNum != REG_WRITE_BARRIER)
                {
                    inst_RV_RV(INS_mov, REG_WRITE_BARRIER, data->gtRegNum, data->TypeGet());
                }
#else
                // At this point, we should not have any interference.
                // That is, 'data' must not be in REG_ARG_0,
                //  as that is where 'addr' must go.
                noway_assert(data->gtRegNum != REG_ARG_0);

                // addr goes in REG_ARG_0
                if (addr->gtRegNum != REG_ARG_0)
                {
                    inst_RV_RV(INS_mov, REG_ARG_0, addr->gtRegNum, addr->TypeGet());
                }

                // data goes in REG_ARG_1
                if (data->gtRegNum != REG_ARG_1)
                {
                    inst_RV_RV(INS_mov, REG_ARG_1, data->gtRegNum, data->TypeGet());
                }
#endif // NOGC_WRITE_BARRIERS

                genGCWriteBarrier(treeNode, writeBarrierForm);
            }
            else // A normal store, not a WriteBarrier store
            {
                bool     reverseOps  = ((treeNode->gtFlags & GTF_REVERSE_OPS) != 0);
                bool     dataIsUnary = false;
                GenTree* nonRMWsrc   = nullptr;
                // We must consume the operands in the proper execution order,
                // so that liveness is updated appropriately.
                if (!reverseOps)
                {
                    genConsumeAddress(addr);
                }

                if (!data->isContained())
                {
                    genConsumeRegs(data);
                }

                if (reverseOps)
                {
                    genConsumeAddress(addr);
                }

                regNumber dataReg = REG_NA;
                if (data->isContainedIntOrIImmed())
                {
                    assert(data->IsIntegralConst(0));
                    dataReg = REG_ZR;
                }
                else // data is not contained, so evaluate it into a register
                {
                    assert(!data->isContained());
                    dataReg = data->gtRegNum;
                }

                emit->emitInsLoadStoreOp(ins_Store(targetType), emitTypeSize(treeNode), dataReg, treeNode->AsIndir());
            }
        }
        break;

        case GT_COPY:
            // This is handled at the time we call genConsumeReg() on the GT_COPY
            break;

        case GT_SWAP:
        {
            // Swap is only supported for lclVar operands that are enregistered
            // We do not consume or produce any registers.  Both operands remain enregistered.
            // However, the gc-ness may change.
            assert(genIsRegCandidateLocal(treeNode->gtOp.gtOp1) && genIsRegCandidateLocal(treeNode->gtOp.gtOp2));

            GenTreeLclVarCommon* lcl1    = treeNode->gtOp.gtOp1->AsLclVarCommon();
            LclVarDsc*           varDsc1 = &(compiler->lvaTable[lcl1->gtLclNum]);
            var_types            type1   = varDsc1->TypeGet();
            GenTreeLclVarCommon* lcl2    = treeNode->gtOp.gtOp2->AsLclVarCommon();
            LclVarDsc*           varDsc2 = &(compiler->lvaTable[lcl2->gtLclNum]);
            var_types            type2   = varDsc2->TypeGet();

            // We must have both int or both fp regs
            assert(!varTypeIsFloating(type1) || varTypeIsFloating(type2));

            // FP swap is not yet implemented (and should have NYI'd in LSRA)
            assert(!varTypeIsFloating(type1));

            regNumber oldOp1Reg     = lcl1->gtRegNum;
            regMaskTP oldOp1RegMask = genRegMask(oldOp1Reg);
            regNumber oldOp2Reg     = lcl2->gtRegNum;
            regMaskTP oldOp2RegMask = genRegMask(oldOp2Reg);

            // We don't call genUpdateVarReg because we don't have a tree node with the new register.
            varDsc1->lvRegNum = oldOp2Reg;
            varDsc2->lvRegNum = oldOp1Reg;

            // Do the xchg
            emitAttr size = EA_PTRSIZE;
            if (varTypeGCtype(type1) != varTypeGCtype(type2))
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
        break;

        case GT_LIST:
        case GT_ARGPLACE:
            // Nothing to do
            break;

        case GT_PUTARG_STK:
            genPutArgStk(treeNode);
            break;

        case GT_PUTARG_REG:
            assert(targetType != TYP_STRUCT); // Any TYP_STRUCT register args should have been removed by
                                              // fgMorphMultiregStructArg
            // We have a normal non-Struct targetType
            {
                GenTree* op1 = treeNode->gtOp.gtOp1;
                // If child node is not already in the register we need, move it
                genConsumeReg(op1);
                if (targetReg != op1->gtRegNum)
                {
                    inst_RV_RV(ins_Copy(targetType), targetReg, op1->gtRegNum, targetType);
                }
            }
            genProduceReg(treeNode);
            break;

        case GT_CALL:
            genCallInstruction(treeNode);
            break;

        case GT_JMP:
            genJmpMethod(treeNode);
            break;

        case GT_LOCKADD:
        case GT_XCHG:
        case GT_XADD:
            genLockedInstructions(treeNode);
            break;

        case GT_MEMORYBARRIER:
            instGen_MemoryBarrier();
            break;

        case GT_CMPXCHG:
            NYI("GT_CMPXCHG");
            break;

        case GT_RELOAD:
            // do nothing - reload is just a marker.
            // The parent node will call genConsumeReg on this which will trigger the unspill of this node's child
            // into the register specified in this node.
            break;

        case GT_NOP:
            break;

        case GT_NO_OP:
            if (treeNode->gtFlags & GTF_NO_OP_NO)
            {
                noway_assert(!"GTF_NO_OP_NO should not be set");
            }
            else
            {
                instGen(INS_nop);
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            genRangeCheck(treeNode);
            break;

        case GT_PHYSREG:
            if (targetReg != treeNode->AsPhysReg()->gtSrcReg)
            {
                inst_RV_RV(ins_Copy(targetType), targetReg, treeNode->AsPhysReg()->gtSrcReg, targetType);

                genTransferRegGCState(targetReg, treeNode->AsPhysReg()->gtSrcReg);
            }
            genProduceReg(treeNode);
            break;

        case GT_PHYSREGDST:
            break;

        case GT_NULLCHECK:
        {
            assert(!treeNode->gtOp.gtOp1->isContained());
            regNumber reg = genConsumeReg(treeNode->gtOp.gtOp1);
            emit->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, reg, 0);
        }
        break;

        case GT_CATCH_ARG:

            noway_assert(handlerGetsXcptnObj(compiler->compCurBB->bbCatchTyp));

            /* Catch arguments get passed in a register. genCodeForBBlist()
               would have marked it as holding a GC object, but not used. */

            noway_assert(gcInfo.gcRegGCrefSetCur & RBM_EXCEPTION_OBJECT);
            genConsumeReg(treeNode);
            break;

        case GT_PINVOKE_PROLOG:
            noway_assert(((gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & ~fullIntArgRegMask()) == 0);

            // the runtime side requires the codegen here to be consistent
            emit->emitDisableRandomNops();
            break;

        case GT_LABEL:
            genPendingCallLabel       = genCreateTempLabel();
            treeNode->gtLabel.gtLabBB = genPendingCallLabel;

            // For long address (default): `adrp + add` will be emitted.
            // For short address (proven later): `adr` will be emitted.
            emit->emitIns_R_L(INS_adr, EA_PTRSIZE, genPendingCallLabel, targetReg);
            break;

        case GT_COPYOBJ:
            genCodeForCpObj(treeNode->AsCpObj());
            break;

        case GT_COPYBLK:
        {
            GenTreeCpBlk* cpBlkOp = treeNode->AsCpBlk();
            if (cpBlkOp->gtBlkOpGcUnsafe)
            {
                getEmitter()->emitDisableGC();
            }

            switch (cpBlkOp->gtBlkOpKind)
            {
                case GenTreeBlkOp::BlkOpKindHelper:
                    genCodeForCpBlk(cpBlkOp);
                    break;
                case GenTreeBlkOp::BlkOpKindUnroll:
                    genCodeForCpBlkUnroll(cpBlkOp);
                    break;
                default:
                    unreached();
            }
            if (cpBlkOp->gtBlkOpGcUnsafe)
            {
                getEmitter()->emitEnableGC();
            }
        }
        break;

        case GT_INITBLK:
        {
            GenTreeInitBlk* initBlkOp = treeNode->AsInitBlk();
            switch (initBlkOp->gtBlkOpKind)
            {
                case GenTreeBlkOp::BlkOpKindHelper:
                    genCodeForInitBlk(initBlkOp);
                    break;
                case GenTreeBlkOp::BlkOpKindUnroll:
                    genCodeForInitBlkUnroll(initBlkOp);
                    break;
                default:
                    unreached();
            }
        }
        break;

        case GT_JMPTABLE:
            genJumpTable(treeNode);
            break;

        case GT_SWITCH_TABLE:
            genTableBasedSwitch(treeNode);
            break;

        case GT_ARR_INDEX:
            genCodeForArrIndex(treeNode->AsArrIndex());
            break;

        case GT_ARR_OFFSET:
            genCodeForArrOffset(treeNode->AsArrOffs());
            break;

        case GT_CLS_VAR_ADDR:
            NYI("GT_CLS_VAR_ADDR");
            break;

        case GT_IL_OFFSET:
            // Do nothing; these nodes are simply markers for debug info.
            break;

        default:
        {
#ifdef DEBUG
            char message[256];
            sprintf(message, "Unimplemented node type %s\n", GenTree::NodeName(treeNode->OperGet()));
#endif
            assert(!"Unknown node in codegen");
        }
        break;
    }
}

//----------------------------------------------------------------------------------
// genMultiRegCallStoreToLocal: store multi-reg return value of a call node to a local
//
// Arguments:
//    treeNode  -  Gentree of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
// Assumption:
//    The child of store is a multi-reg call node.
//    genProduceReg() on treeNode is made by caller of this routine.
//
void CodeGen::genMultiRegCallStoreToLocal(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_STORE_LCL_VAR);

    // Structs of size >=9 and <=16 are returned in two return registers on ARM64 and HFAs.
    assert(varTypeIsStruct(treeNode));

    // Assumption: current ARM64 implementation requires that a multi-reg struct
    // var in 'var = call' is flagged as lvIsMultiRegRet to prevent it from
    // being struct promoted.
    unsigned   lclNum = treeNode->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = &(compiler->lvaTable[lclNum]);
    noway_assert(varDsc->lvIsMultiRegRet);

    GenTree*     op1       = treeNode->gtGetOp1();
    GenTree*     actualOp1 = op1->gtSkipReloadOrCopy();
    GenTreeCall* call      = actualOp1->AsCall();
    assert(call->HasMultiRegRetVal());

    genConsumeRegs(op1);

    ReturnTypeDesc* pRetTypeDesc = call->GetReturnTypeDesc();
    unsigned        regCount     = pRetTypeDesc->GetReturnRegCount();

    if (treeNode->gtRegNum != REG_NA)
    {
        // Right now the only enregistrable structs supported are SIMD types.
        assert(varTypeIsSIMD(treeNode));
        NYI("GT_STORE_LCL_VAR of a SIMD enregisterable struct");
    }
    else
    {
        // Stack store
        int offset = 0;
        for (unsigned i = 0; i < regCount; ++i)
        {
            var_types type = pRetTypeDesc->GetReturnRegType(i);
            regNumber reg  = call->GetRegNumByIdx(i);
            if (op1->IsCopyOrReload())
            {
                // GT_COPY/GT_RELOAD will have valid reg for those positions
                // that need to be copied or reloaded.
                regNumber reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(i);
                if (reloadReg != REG_NA)
                {
                    reg = reloadReg;
                }
            }

            assert(reg != REG_NA);
            getEmitter()->emitIns_S_R(ins_Store(type), emitTypeSize(type), reg, lclNum, offset);
            offset += genTypeSize(type);
        }

        varDsc->lvRegNum = REG_STK;
    }
}

/***********************************************************************************************
 *  Generate code for localloc
 */
void CodeGen::genLclHeap(GenTreePtr tree)
{
    assert(tree->OperGet() == GT_LCLHEAP);

    GenTreePtr size = tree->gtOp.gtOp1;
    noway_assert((genActualType(size->gtType) == TYP_INT) || (genActualType(size->gtType) == TYP_I_IMPL));

    regNumber   targetReg       = tree->gtRegNum;
    regMaskTP   tmpRegsMask     = tree->gtRsvdRegs;
    regNumber   regCnt          = REG_NA;
    regNumber   pspSymReg       = REG_NA;
    var_types   type            = genActualType(size->gtType);
    emitAttr    easz            = emitTypeSize(type);
    BasicBlock* endLabel        = nullptr;
    BasicBlock* loop            = nullptr;
    unsigned    stackAdjustment = 0;

#ifdef DEBUG
    // Verify ESP
    if (compiler->opts.compStackCheckOnRet)
    {
        noway_assert(compiler->lvaReturnEspCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvOnFrame);
        getEmitter()->emitIns_S_R(INS_cmp, EA_PTRSIZE, REG_SPBASE, compiler->lvaReturnEspCheck, 0);

        BasicBlock*  esp_check = genCreateTempLabel();
        emitJumpKind jmpEqual  = genJumpKindForOper(GT_EQ, CK_SIGNED);
        inst_JMP(jmpEqual, esp_check);
        getEmitter()->emitIns(INS_BREAKPOINT);
        genDefineTempLabel(esp_check);
    }
#endif

    noway_assert(isFramePointerUsed()); // localloc requires Frame Pointer to be established since SP changes
    noway_assert(genStackLevel == 0);   // Can't have anything on the stack

    // Whether method has PSPSym.
    bool hasPspSym;
#if FEATURE_EH_FUNCLETS
    hasPspSym = (compiler->lvaPSPSym != BAD_VAR_NUM);
#else
    hasPspSym = false;
#endif

    // compute the amount of memory to allocate to properly STACK_ALIGN.
    size_t amount = 0;
    if (size->IsCnsIntOrI())
    {
        // If size is a constant, then it must be contained.
        assert(size->isContained());

        // If amount is zero then return null in targetReg
        amount = size->gtIntCon.gtIconVal;
        if (amount == 0)
        {
            instGen_Set_Reg_To_Zero(EA_PTRSIZE, targetReg);
            goto BAILOUT;
        }

        // 'amount' is the total numbe of bytes to localloc to properly STACK_ALIGN
        amount = AlignUp(amount, STACK_ALIGN);
    }
    else
    {
        // If 0 bail out by returning null in targetReg
        genConsumeRegAndCopy(size, targetReg);
        endLabel = genCreateTempLabel();
        getEmitter()->emitIns_R_R(INS_TEST, easz, targetReg, targetReg);
        emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
        inst_JMP(jmpEqual, endLabel);

        // Compute the size of the block to allocate and perform alignment.
        // If the method has no PSPSym and compInitMem=true, we can reuse targetReg as regcnt,
        // since we don't need any internal registers.
        if (!hasPspSym && compiler->info.compInitMem)
        {
            assert(genCountBits(tmpRegsMask) == 0);
            regCnt = targetReg;
        }
        else
        {
            assert(genCountBits(tmpRegsMask) >= 1);
            regMaskTP regCntMask = genFindLowestBit(tmpRegsMask);
            tmpRegsMask &= ~regCntMask;
            regCnt = genRegNumFromMask(regCntMask);
            if (regCnt != targetReg)
                inst_RV_RV(INS_mov, regCnt, targetReg, size->TypeGet());
        }

        // Align to STACK_ALIGN
        // regCnt will be the total number of bytes to localloc
        inst_RV_IV(INS_add, regCnt, (STACK_ALIGN - 1), emitActualTypeSize(type));
        inst_RV_IV(INS_AND, regCnt, ~(STACK_ALIGN - 1), emitActualTypeSize(type));
    }

    stackAdjustment = 0;
#if FEATURE_EH_FUNCLETS
    // If we have PSPsym, then need to re-locate it after localloc.
    if (hasPspSym)
    {
        stackAdjustment += STACK_ALIGN;

        // Save a copy of PSPSym
        assert(genCountBits(tmpRegsMask) >= 1);
        regMaskTP pspSymRegMask = genFindLowestBit(tmpRegsMask);
        tmpRegsMask &= ~pspSymRegMask;
        pspSymReg = genRegNumFromMask(pspSymRegMask);
        getEmitter()->emitIns_R_S(ins_Load(TYP_I_IMPL), EA_PTRSIZE, pspSymReg, compiler->lvaPSPSym, 0);
    }
#endif

#if FEATURE_FIXED_OUT_ARGS
    // If we have an outgoing arg area then we must adjust the SP by popping off the
    // outgoing arg area. We will restore it right before we return from this method.
    //
    // Localloc is supposed to return stack space that is STACK_ALIGN'ed.  The following
    // are the cases that needs to be handled:
    //   i) Method has PSPSym + out-going arg area.
    //      It is guaranteed that size of out-going arg area is STACK_ALIGNED (see fgMorphArgs).
    //      Therefore, we will pop-off RSP upto out-going arg area before locallocating.
    //      We need to add padding to ensure RSP is STACK_ALIGN'ed while re-locating PSPSym + arg area.
    //  ii) Method has no PSPSym but out-going arg area.
    //      Almost same case as above without the requirement to pad for the final RSP to be STACK_ALIGN'ed.
    // iii) Method has PSPSym but no out-going arg area.
    //      Nothing to pop-off from the stack but needs to relocate PSPSym with SP padded.
    //  iv) Method has neither PSPSym nor out-going arg area.
    //      Nothing needs to popped off from stack nor relocated.
    if (compiler->lvaOutgoingArgSpaceSize > 0)
    {
        assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) == 0); // This must be true for the stack to remain
                                                                        // aligned
        inst_RV_IV(INS_add, REG_SPBASE, compiler->lvaOutgoingArgSpaceSize, EA_PTRSIZE);
        stackAdjustment += compiler->lvaOutgoingArgSpaceSize;
    }
#endif

    if (size->IsCnsIntOrI())
    {
        // We should reach here only for non-zero, constant size allocations.
        assert(amount > 0);

        // For small allocations we will generate up to four stp instructions
        size_t cntStackAlignedWidthItems = (amount >> STACK_ALIGN_SHIFT);
        if (cntStackAlignedWidthItems <= 4)
        {
            while (cntStackAlignedWidthItems != 0)
            {
                // We can use pre-indexed addressing.
                // stp ZR, ZR, [SP, #-16]!
                getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_ZR, REG_ZR, REG_SPBASE, -16, INS_OPTS_PRE_INDEX);
                cntStackAlignedWidthItems -= 1;
            }

            goto ALLOC_DONE;
        }
        else if (!compiler->info.compInitMem && (amount < compiler->eeGetPageSize())) // must be < not <=
        {
            // Since the size is a page or less, simply adjust the SP value
            // The SP might already be in the guard page, must touch it BEFORE
            // the alloc, not after.
            // ldr wz, [SP, #0]
            getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, REG_SP, 0);

            inst_RV_IV(INS_sub, REG_SP, amount, EA_PTRSIZE);

            goto ALLOC_DONE;
        }

        // else, "mov regCnt, amount"
        // If the method has no PSPSym and compInitMem=true, we can reuse targetReg as regcnt.
        // Since size is a constant, regCnt is not yet initialized.
        assert(regCnt == REG_NA);
        if (!hasPspSym && compiler->info.compInitMem)
        {
            assert(genCountBits(tmpRegsMask) == 0);
            regCnt = targetReg;
        }
        else
        {
            assert(genCountBits(tmpRegsMask) >= 1);
            regMaskTP regCntMask = genFindLowestBit(tmpRegsMask);
            tmpRegsMask &= ~regCntMask;
            regCnt = genRegNumFromMask(regCntMask);
        }
        genSetRegToIcon(regCnt, amount, ((int)amount == amount) ? TYP_INT : TYP_LONG);
    }

    if (compiler->info.compInitMem)
    {
        BasicBlock* loop = genCreateTempLabel();

        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        // Since we have to zero out the allocated memory AND ensure that RSP is always valid
        // by tickling the pages, we will just push 0's on the stack.
        //
        // Note: regCnt is guaranteed to be even on Amd64 since STACK_ALIGN/TARGET_POINTER_SIZE = 2
        // and localloc size is a multiple of STACK_ALIGN.

        // Loop:
        genDefineTempLabel(loop);

        // We can use pre-indexed addressing.
        // stp ZR, ZR, [SP, #-16]!
        getEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_ZR, REG_ZR, REG_SPBASE, -16, INS_OPTS_PRE_INDEX);

        // If not done, loop
        // Note that regCnt is the number of bytes to stack allocate.
        // Therefore we need to subtract 16 from regcnt here.
        assert(genIsValidIntReg(regCnt));
        inst_RV_IV(INS_subs, regCnt, 16, emitActualTypeSize(type));
        emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
        inst_JMP(jmpNotEqual, loop);
    }
    else
    {
        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        //
        // We don't need to zero out the allocated memory. However, we do have
        // to tickle the pages to ensure that SP is always valid and is
        // in sync with the "stack guard page".  Note that in the worst
        // case SP is on the last byte of the guard page.  Thus you must
        // touch SP+0 first not SP+x01000.
        //
        // Another subtlety is that you don't want SP to be exactly on the
        // boundary of the guard page because PUSH is predecrement, thus
        // call setup would not touch the guard page but just beyond it
        //
        // Note that we go through a few hoops so that SP never points to
        // illegal pages at any time during the ticking process
        //
        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        //       jb    Loop                    // result is smaller than orignial SP (no wrap around)
        //       mov   regCnt, #0              // Overflow, pick lowest possible value
        //
        //  Loop:
        //       ldr   wzr, [SP + 0]           // tickle the page - read from the page
        //       sub   regTmp, SP, PAGE_SIZE   // decrement SP by PAGE_SIZE
        //       cmp   regTmp, regCnt
        //       jb    Done
        //       mov   SP, regTmp
        //       j     Loop
        //
        //  Done:
        //       mov   SP, regCnt
        //

        // Setup the regTmp
        assert(tmpRegsMask != RBM_NONE);
        assert(genCountBits(tmpRegsMask) == 1);
        regNumber regTmp = genRegNumFromMask(tmpRegsMask);

        BasicBlock* loop = genCreateTempLabel();
        BasicBlock* done = genCreateTempLabel();

        //       subs  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        getEmitter()->emitIns_R_R_R(INS_subs, EA_PTRSIZE, regCnt, REG_SPBASE, regCnt);

        inst_JMP(EJ_vc, loop); // branch if the V flag is not set

        // Overflow, set regCnt to lowest possible value
        instGen_Set_Reg_To_Zero(EA_PTRSIZE, regCnt);

        genDefineTempLabel(loop);

        // tickle the page - Read from the updated SP - this triggers a page fault when on the guard page
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, 0);

        // decrement SP by PAGE_SIZE
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, regTmp, REG_SPBASE, compiler->eeGetPageSize());

        getEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, regTmp, regCnt);
        emitJumpKind jmpLTU = genJumpKindForOper(GT_LT, CK_UNSIGNED);
        inst_JMP(jmpLTU, done);

        // Update SP to be at the next page of stack that we will tickle
        getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_SPBASE, regCnt);

        // Jump to loop and tickle new stack address
        inst_JMP(EJ_jmp, loop);

        // Done with stack tickle loop
        genDefineTempLabel(done);

        // Now just move the final value to SP
        getEmitter()->emitIns_R_R(INS_mov, EA_PTRSIZE, REG_SPBASE, regCnt);
    }

ALLOC_DONE:
    // Re-adjust SP to allocate PSPSym and out-going arg area
    if (stackAdjustment != 0)
    {
        assert((stackAdjustment % STACK_ALIGN) == 0); // This must be true for the stack to remain aligned
        assert(stackAdjustment > 0);
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, (int)stackAdjustment);

#if FEATURE_EH_FUNCLETS
        // Write PSPSym to its new location.
        if (hasPspSym)
        {
            assert(genIsValidIntReg(pspSymReg));
            getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, pspSymReg, compiler->lvaPSPSym, 0);
        }
#endif
        // Return the stackalloc'ed address in result register.
        // TargetReg = RSP + stackAdjustment.
        //
        getEmitter()->emitIns_R_R_I(INS_add, EA_PTRSIZE, targetReg, REG_SPBASE, (int)stackAdjustment);
    }
    else // stackAdjustment == 0
    {
        // Move the final value of SP to targetReg
        inst_RV_RV(INS_mov, targetReg, REG_SPBASE);
    }

BAILOUT:
    if (endLabel != nullptr)
        genDefineTempLabel(endLabel);

    // Write the lvaShadowSPfirst stack frame slot
    noway_assert(compiler->lvaLocAllocSPvar != BAD_VAR_NUM);
    getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, targetReg, compiler->lvaLocAllocSPvar, 0);

#if STACK_PROBES
    if (compiler->opts.compNeedStackProbes)
    {
        genGenerateStackProbe();
    }
#endif

#ifdef DEBUG
    // Update new ESP
    if (compiler->opts.compStackCheckOnRet)
    {
        noway_assert(compiler->lvaReturnEspCheck != 0xCCCCCCCC &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvDoNotEnregister &&
                     compiler->lvaTable[compiler->lvaReturnEspCheck].lvOnFrame);
        getEmitter()->emitIns_S_R(ins_Store(TYP_I_IMPL), EA_PTRSIZE, targetReg, compiler->lvaReturnEspCheck, 0);
    }
#endif

    genProduceReg(tree);
}

// Generate code for InitBlk by performing a loop unroll
// Preconditions:
//   a) Both the size and fill byte value are integer constants.
//   b) The size of the struct to initialize is smaller than INITBLK_UNROLL_LIMIT bytes.
void CodeGen::genCodeForInitBlkUnroll(GenTreeInitBlk* initBlkNode)
{
#if 0
    // Make sure we got the arguments of the initblk/initobj operation in the right registers
    GenTreePtr blockSize = initBlkNode->Size();
    GenTreePtr   dstAddr = initBlkNode->Dest();
    GenTreePtr   initVal = initBlkNode->InitVal();

#ifdef DEBUG
    assert(!dstAddr->isContained());
    assert(!initVal->isContained());
    assert(blockSize->isContained());

    assert(blockSize->IsCnsIntOrI());
#endif // DEBUG

    size_t size = blockSize->gtIntCon.gtIconVal;

    assert(size <= INITBLK_UNROLL_LIMIT);
    assert(initVal->gtSkipReloadOrCopy()->IsCnsIntOrI());

    emitter *emit = getEmitter();

    genConsumeReg(initVal);
    genConsumeReg(dstAddr);

    // If the initVal was moved, or spilled and reloaded to a different register,
    // get the original initVal from below the GT_RELOAD, but only after capturing the valReg,
    // which needs to be the new register.
    regNumber valReg = initVal->gtRegNum;
    initVal = initVal->gtSkipReloadOrCopy();
#else  // !0
    NYI("genCodeForInitBlkUnroll");
#endif // !0
}

// Generates code for InitBlk by calling the VM memset helper function.
// Preconditions:
// a) The size argument of the InitBlk is not an integer constant.
// b) The size argument of the InitBlk is >= INITBLK_STOS_LIMIT bytes.
void CodeGen::genCodeForInitBlk(GenTreeInitBlk* initBlkNode)
{
    // Make sure we got the arguments of the initblk operation in the right registers
    GenTreePtr blockSize = initBlkNode->Size();
    GenTreePtr dstAddr   = initBlkNode->Dest();
    GenTreePtr initVal   = initBlkNode->InitVal();

#ifdef DEBUG
    assert(!dstAddr->isContained());
    assert(!initVal->isContained());
    assert(!blockSize->isContained());

#if 0
    // TODO-ARM64-CQ: When initblk loop unrolling is implemented
    //                put this assert back on.
    if (blockSize->IsCnsIntOrI())
    {
        assert(blockSize->gtIntCon.gtIconVal >= INITBLK_UNROLL_LIMIT);
    }
#endif // 0
#endif // DEBUG

    genConsumeRegAndCopy(blockSize, REG_ARG_2);
    genConsumeRegAndCopy(initVal, REG_ARG_1);
    genConsumeRegAndCopy(dstAddr, REG_ARG_0);

    genEmitHelperCall(CORINFO_HELP_MEMSET, 0, EA_UNKNOWN);
}

// Generate code for a load from some address + offset
//   base: tree node which can be either a local address or arbitrary node
//   offset: distance from the base from which to load
void CodeGen::genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset)
{
    emitter* emit = getEmitter();

    if (base->OperIsLocalAddr())
    {
        if (base->gtOper == GT_LCL_FLD_ADDR)
            offset += base->gtLclFld.gtLclOffs;
        emit->emitIns_R_S(ins, size, dst, base->gtLclVarCommon.gtLclNum, offset);
    }
    else
    {
        emit->emitIns_R_R_I(ins, size, dst, base->gtRegNum, offset);
    }
}

// Generate code for a store to some address + offset
//   base: tree node which can be either a local address or arbitrary node
//   offset: distance from the base from which to load
void CodeGen::genCodeForStoreOffset(instruction ins, emitAttr size, regNumber src, GenTree* base, unsigned offset)
{
#if 0
    emitter *emit = getEmitter();

    if (base->OperIsLocalAddr())
    {
        if (base->gtOper == GT_LCL_FLD_ADDR)
            offset += base->gtLclFld.gtLclOffs;
        emit->emitIns_S_R(ins, size, src, base->gtLclVarCommon.gtLclNum, offset);
    }
    else
    {
        emit->emitIns_AR_R(ins, size, src, base->gtRegNum, offset);
    }
#else  // !0
    NYI("genCodeForStoreOffset");
#endif // !0
}

// Generates CpBlk code by performing a loop unroll
// Preconditions:
//  The size argument of the CpBlk node is a constant and <= 64 bytes.
//  This may seem small but covers >95% of the cases in several framework assemblies.
void CodeGen::genCodeForCpBlkUnroll(GenTreeCpBlk* cpBlkNode)
{
#if 0
    // Make sure we got the arguments of the cpblk operation in the right registers
    GenTreePtr blockSize = cpBlkNode->Size();
    GenTreePtr   dstAddr = cpBlkNode->Dest();
    GenTreePtr   srcAddr = cpBlkNode->Source();

    assert(blockSize->IsCnsIntOrI());
    size_t size = blockSize->gtIntCon.gtIconVal;
    assert(size <= CPBLK_UNROLL_LIMIT);

    emitter *emit = getEmitter();

    if (!srcAddr->isContained())
        genConsumeReg(srcAddr);

    if (!dstAddr->isContained())
        genConsumeReg(dstAddr);

    unsigned offset = 0;

    // If the size of this struct is larger than 16 bytes
    // let's use SSE2 to be able to do 16 byte at a time 
    // loads and stores.
    if (size >= XMM_REGSIZE_BYTES)
    {
        assert(cpBlkNode->gtRsvdRegs != RBM_NONE);
        assert(genCountBits(cpBlkNode->gtRsvdRegs) == 1);
        regNumber xmmReg = genRegNumFromMask(cpBlkNode->gtRsvdRegs);
        assert(genIsValidFloatReg(xmmReg));
        size_t slots = size / XMM_REGSIZE_BYTES;

        while (slots-- > 0)
        {
            // Load
            genCodeForLoadOffset(INS_movdqu, EA_8BYTE, xmmReg, srcAddr, offset);
            // Store
            genCodeForStoreOffset(INS_movdqu, EA_8BYTE, xmmReg, dstAddr, offset);
            offset += XMM_REGSIZE_BYTES;
        }
    }

    // Fill the remainder (15 bytes or less) if there's one.
    if ((size & 0xf) != 0)
    {
        // Grab the integer temp register to emit the remaining loads and stores.
        regNumber tmpReg = genRegNumFromMask(cpBlkNode->gtRsvdRegs & RBM_ALLINT);

        if ((size & 8) != 0)
        {
            genCodeForLoadOffset(INS_mov, EA_8BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_mov, EA_8BYTE, tmpReg, dstAddr, offset);
            offset += 8;
        }
        if ((size & 4) != 0)
        {
            genCodeForLoadOffset(INS_mov, EA_4BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_mov, EA_4BYTE, tmpReg, dstAddr, offset);
            offset += 4;
        }
        if ((size & 2) != 0)
        {
            genCodeForLoadOffset(INS_mov, EA_2BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_mov, EA_2BYTE, tmpReg, dstAddr, offset);
            offset += 2;
        }
        if ((size & 1) != 0)
        {
            genCodeForLoadOffset(INS_mov, EA_1BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_mov, EA_1BYTE, tmpReg, dstAddr, offset);
        }
    }
#else  // !0
    NYI("genCodeForCpBlkUnroll");
#endif // !0
}

// Generate code for CpObj nodes wich copy structs that have interleaved
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
void CodeGen::genCodeForCpObj(GenTreeCpObj* cpObjNode)
{
    // Make sure we got the arguments of the cpobj operation in the right registers
    GenTreePtr clsTok  = cpObjNode->ClsTok();
    GenTreePtr dstAddr = cpObjNode->Dest();
    GenTreePtr srcAddr = cpObjNode->Source();

    bool dstOnStack = dstAddr->OperIsLocalAddr();

#ifdef DEBUG
    assert(!dstAddr->isContained());
    assert(!srcAddr->isContained());

    // This GenTree node has data about GC pointers, this means we're dealing
    // with CpObj.
    assert(cpObjNode->gtGcPtrCount > 0);
#endif // DEBUG

    // Consume these registers.
    // They may now contain gc pointers (depending on their type; gcMarkRegPtrVal will "do the right thing").
    genConsumeRegAndCopy(srcAddr, REG_WRITE_BARRIER_SRC_BYREF);
    gcInfo.gcMarkRegPtrVal(REG_WRITE_BARRIER_SRC_BYREF, srcAddr->TypeGet());

    genConsumeRegAndCopy(dstAddr, REG_WRITE_BARRIER_DST_BYREF);
    gcInfo.gcMarkRegPtrVal(REG_WRITE_BARRIER_DST_BYREF, dstAddr->TypeGet());

    // Temp register used to perform the sequence of loads and stores.
    regNumber tmpReg = genRegNumFromMask(cpObjNode->gtRsvdRegs);

#ifdef DEBUG
    assert(cpObjNode->gtRsvdRegs != RBM_NONE);
    assert(genCountBits(cpObjNode->gtRsvdRegs) == 1);
    assert(genIsValidIntReg(tmpReg));
#endif // DEBUG

    unsigned slots = cpObjNode->gtSlots;
    emitter* emit  = getEmitter();

    // If we can prove it's on the stack we don't need to use the write barrier.
    if (dstOnStack)
    {
        // TODO-ARM64-CQ: Consider using LDP/STP to save codesize.
        while (slots > 0)
        {
            emit->emitIns_R_R_I(INS_ldr, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE,
                                INS_OPTS_POST_INDEX);
            emit->emitIns_R_R_I(INS_str, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE,
                                INS_OPTS_POST_INDEX);
            slots--;
        }
    }
    else
    {
        BYTE*    gcPtrs     = cpObjNode->gtGcPtrs;
        unsigned gcPtrCount = cpObjNode->gtGcPtrCount;

        unsigned i = 0;
        while (i < slots)
        {
            switch (gcPtrs[i])
            {
                case TYPE_GC_NONE:
                    // TODO-ARM64-CQ: Consider using LDP/STP to save codesize in case of contigous NON-GC slots.
                    emit->emitIns_R_R_I(INS_ldr, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE,
                                        INS_OPTS_POST_INDEX);
                    emit->emitIns_R_R_I(INS_str, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE,
                                        INS_OPTS_POST_INDEX);
                    break;

                default:
                    // We have a GC pointer, call the memory barrier.
                    genEmitHelperCall(CORINFO_HELP_ASSIGN_BYREF, 0, EA_PTRSIZE);
                    gcPtrCount--;
                    break;
            }
            ++i;
        }
        assert(gcPtrCount == 0);
    }

    // Clear the gcInfo for REG_WRITE_BARRIER_SRC_BYREF and REG_WRITE_BARRIER_DST_BYREF.
    // While we normally update GC info prior to the last instruction that uses them,
    // these actually live into the helper call.
    gcInfo.gcMarkRegSetNpt(RBM_WRITE_BARRIER_SRC_BYREF | RBM_WRITE_BARRIER_DST_BYREF);
}

// Generate code for a CpBlk node by the means of the VM memcpy helper call
// Preconditions:
// a) The size argument of the CpBlk is not an integer constant
// b) The size argument is a constant but is larger than CPBLK_MOVS_LIMIT bytes.
void CodeGen::genCodeForCpBlk(GenTreeCpBlk* cpBlkNode)
{
    // Make sure we got the arguments of the cpblk operation in the right registers
    GenTreePtr blockSize = cpBlkNode->Size();
    GenTreePtr dstAddr   = cpBlkNode->Dest();
    GenTreePtr srcAddr   = cpBlkNode->Source();

    assert(!dstAddr->isContained());
    assert(!srcAddr->isContained());
    assert(!blockSize->isContained());

#if 0
#ifdef DEBUG
    // Enable this when we support cpblk loop unrolling.

    if (blockSize->IsCnsIntOrI())
    {
        assert(blockSize->gtIntCon.gtIconVal >= CPBLK_UNROLL_LIMIT);
    }
#endif // DEBUG
#endif // 0

    genConsumeRegAndCopy(blockSize, REG_ARG_2);
    genConsumeRegAndCopy(srcAddr, REG_ARG_1);
    genConsumeRegAndCopy(dstAddr, REG_ARG_0);

    genEmitHelperCall(CORINFO_HELP_MEMCPY, 0, EA_UNKNOWN);
}

// generate code do a switch statement based on a table of ip-relative offsets
void CodeGen::genTableBasedSwitch(GenTree* treeNode)
{
    genConsumeOperands(treeNode->AsOp());
    regNumber idxReg  = treeNode->gtOp.gtOp1->gtRegNum;
    regNumber baseReg = treeNode->gtOp.gtOp2->gtRegNum;

    regNumber tmpReg = genRegNumFromMask(treeNode->gtRsvdRegs);

    // load the ip-relative offset (which is relative to start of fgFirstBB)
    getEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, baseReg, baseReg, idxReg, INS_OPTS_LSL);

    // add it to the absolute address of fgFirstBB
    compiler->fgFirstBB->bbFlags |= BBF_JMP_TARGET;
    getEmitter()->emitIns_R_L(INS_adr, EA_PTRSIZE, compiler->fgFirstBB, tmpReg);
    getEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, baseReg, baseReg, tmpReg);

    // br baseReg
    getEmitter()->emitIns_R(INS_br, emitTypeSize(TYP_I_IMPL), baseReg);
}

// emits the table and an instruction to get the address of the first element
void CodeGen::genJumpTable(GenTree* treeNode)
{
    noway_assert(compiler->compCurBB->bbJumpKind == BBJ_SWITCH);
    assert(treeNode->OperGet() == GT_JMPTABLE);

    unsigned     jumpCount = compiler->compCurBB->bbJumpSwt->bbsCount;
    BasicBlock** jumpTable = compiler->compCurBB->bbJumpSwt->bbsDstTab;
    unsigned     jmpTabOffs;
    unsigned     jmpTabBase;

    jmpTabBase = getEmitter()->emitBBTableDataGenBeg(jumpCount, true);

    jmpTabOffs = 0;

    JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", Compiler::s_compMethodsCount, jmpTabBase);

    for (unsigned i = 0; i < jumpCount; i++)
    {
        BasicBlock* target = *jumpTable++;
        noway_assert(target->bbFlags & BBF_JMP_TARGET);

        JITDUMP("            DD      L_M%03u_BB%02u\n", Compiler::s_compMethodsCount, target->bbNum);

        getEmitter()->emitDataGenData(i, target);
    };

    getEmitter()->emitDataGenEnd();

    // Access to inline data is 'abstracted' by a special type of static member
    // (produced by eeFindJitDataOffs) which the emitter recognizes as being a reference
    // to constant data, not a real static field.
    getEmitter()->emitIns_R_C(INS_adr, emitTypeSize(TYP_I_IMPL), treeNode->gtRegNum, REG_NA,
                              compiler->eeFindJitDataOffs(jmpTabBase), 0);
    genProduceReg(treeNode);
}

// generate code for the locked operations:
// GT_LOCKADD, GT_XCHG, GT_XADD
void CodeGen::genLockedInstructions(GenTree* treeNode)
{
#if 0
    GenTree* data       = treeNode->gtOp.gtOp2;
    GenTree* addr       = treeNode->gtOp.gtOp1;
    regNumber targetReg = treeNode->gtRegNum;
    regNumber dataReg   = data->gtRegNum;
    regNumber addrReg   = addr->gtRegNum;
    instruction ins;

    // all of these nodes implicitly do an indirection on op1
    // so create a temporary node to feed into the pattern matching
    GenTreeIndir i = indirForm(data->TypeGet(), addr);
    genConsumeReg(addr);

    // The register allocator should have extended the lifetime of the address
    // so that it is not used as the target.
    noway_assert(addrReg != targetReg);

    // If data is a lclVar that's not a last use, we'd better have allocated a register
    // for the result (except in the case of GT_LOCKADD which does not produce a register result).
    assert(targetReg != REG_NA || treeNode->OperGet() == GT_LOCKADD || !genIsRegCandidateLocal(data) || (data->gtFlags & GTF_VAR_DEATH) != 0);

    genConsumeIfReg(data);
    if (targetReg != REG_NA && dataReg != REG_NA && dataReg != targetReg)
    {
        inst_RV_RV(ins_Copy(data->TypeGet()), targetReg, dataReg);
        data->gtRegNum = targetReg;

        // TODO-ARM64-Cleanup: Consider whether it is worth it, for debugging purposes, to restore the
        // original gtRegNum on data, after calling emitInsBinary below.
    }
    switch (treeNode->OperGet())
    {
    case GT_LOCKADD:
        instGen(INS_lock);
        ins = INS_add;
        break;
    case GT_XCHG:
        // lock is implied by xchg
        ins = INS_xchg;
        break;
    case GT_XADD:
        instGen(INS_lock);
        ins = INS_xadd;
        break;
    default:
        unreached();
    }
    getEmitter()->emitInsBinary(ins, emitTypeSize(data), &i, data);

    if (treeNode->gtRegNum != REG_NA)
    {
        genProduceReg(treeNode);
    }
#else  // !0
    NYI("genLockedInstructions");
#endif // !0
}

// generate code for BoundsCheck nodes
void CodeGen::genRangeCheck(GenTreePtr oper)
{
#ifdef FEATURE_SIMD
    noway_assert(oper->OperGet() == GT_ARR_BOUNDS_CHECK || oper->OperGet() == GT_SIMD_CHK);
#else  // !FEATURE_SIMD
    noway_assert(oper->OperGet() == GT_ARR_BOUNDS_CHECK);
#endif // !FEATURE_SIMD

    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTreePtr arrLen    = bndsChk->gtArrLen;
    GenTreePtr arrIndex  = bndsChk->gtIndex;
    GenTreePtr arrRef    = NULL;
    int        lenOffset = 0;

    GenTree *    src1, *src2;
    emitJumpKind jmpKind;

    genConsumeRegs(arrLen);
    genConsumeRegs(arrIndex);

    if (arrIndex->isContainedIntOrIImmed())
    {
        // To encode using a cmp immediate, we place the
        //  constant operand in the second position
        src1    = arrLen;
        src2    = arrIndex;
        jmpKind = genJumpKindForOper(GT_LE, CK_UNSIGNED);
    }
    else
    {
        src1    = arrIndex;
        src2    = arrLen;
        jmpKind = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    }

    GenTreeIntConCommon* intConst = nullptr;
    if (src2->isContainedIntOrIImmed())
    {
        intConst = src2->AsIntConCommon();
    }

    if (intConst != nullptr)
    {
        getEmitter()->emitIns_R_I(INS_cmp, EA_4BYTE, src1->gtRegNum, intConst->IconValue());
    }
    else
    {
        getEmitter()->emitIns_R_R(INS_cmp, EA_4BYTE, src1->gtRegNum, src2->gtRegNum);
    }

    genJumpToThrowHlpBlk(jmpKind, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLowerBound: Returns the offset from the Array object to the
//   lower bound for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayLowerBound(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT, even on 64-bit targets.
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * (dimension + rank);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLength: Returns the offset from the Array object to the
//   size for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayDimensionSize(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT, even on 64-bit targets.
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * dimension;
}

//------------------------------------------------------------------------
// genCodeForArrIndex: Generates code to bounds check the index for one dimension of an array reference,
//                     producing the effective index by subtracting the lower bound.
//
// Arguments:
//    arrIndex - the node for which we're generating code
//
// Return Value:
//    None.
//

void CodeGen::genCodeForArrIndex(GenTreeArrIndex* arrIndex)
{
    emitter*   emit      = getEmitter();
    GenTreePtr arrObj    = arrIndex->ArrObj();
    GenTreePtr indexNode = arrIndex->IndexExpr();
    regNumber  arrReg    = genConsumeReg(arrObj);
    regNumber  indexReg  = genConsumeReg(indexNode);
    regNumber  tgtReg    = arrIndex->gtRegNum;
    noway_assert(tgtReg != REG_NA);

    // We will use a temp register to load the lower bound and dimension size values
    //
    regMaskTP tmpRegsMask = arrIndex->gtRsvdRegs; // there will be two bits set
    tmpRegsMask &= ~genRegMask(tgtReg);           // remove the bit for 'tgtReg' from 'tmpRegsMask'

    regMaskTP tmpRegMask = genFindLowestBit(tmpRegsMask); // set tmpRegMsk to a one-bit mask
    regNumber tmpReg     = genRegNumFromMask(tmpRegMask); // set tmpReg from that mask
    noway_assert(tmpReg != REG_NA);

    assert(tgtReg != tmpReg);

    unsigned  dim      = arrIndex->gtCurrDim;
    unsigned  rank     = arrIndex->gtArrRank;
    var_types elemType = arrIndex->gtArrElemType;
    unsigned  offset;

    offset = genOffsetOfMDArrayLowerBound(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_8BYTE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R_R(INS_sub, EA_4BYTE, tgtReg, indexReg, tmpReg);

    offset = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_8BYTE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R(INS_cmp, EA_4BYTE, tgtReg, tmpReg);

    emitJumpKind jmpGEU = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    genJumpToThrowHlpBlk(jmpGEU, SCK_RNGCHK_FAIL);

    genProduceReg(arrIndex);
}

//------------------------------------------------------------------------
// genCodeForArrOffset: Generates code to compute the flattened array offset for
//    one dimension of an array reference:
//        result = (prevDimOffset * dimSize) + effectiveIndex
//    where dimSize is obtained from the arrObj operand
//
// Arguments:
//    arrOffset - the node for which we're generating code
//
// Return Value:
//    None.
//
// Notes:
//    dimSize and effectiveIndex are always non-negative, the former by design,
//    and the latter because it has been normalized to be zero-based.

void CodeGen::genCodeForArrOffset(GenTreeArrOffs* arrOffset)
{
    GenTreePtr offsetNode = arrOffset->gtOffset;
    GenTreePtr indexNode  = arrOffset->gtIndex;
    regNumber  tgtReg     = arrOffset->gtRegNum;

    noway_assert(tgtReg != REG_NA);

    if (!offsetNode->IsIntegralConst(0))
    {
        emitter*   emit   = getEmitter();
        GenTreePtr arrObj = arrOffset->gtArrObj;
        regNumber  arrReg = genConsumeReg(arrObj);
        noway_assert(arrReg != REG_NA);
        regNumber offsetReg = genConsumeReg(offsetNode);
        noway_assert(offsetReg != REG_NA);
        regNumber indexReg = genConsumeReg(indexNode);
        noway_assert(indexReg != REG_NA);
        regMaskTP tmpRegMask = arrOffset->gtRsvdRegs;
        regNumber tmpReg     = genRegNumFromMask(tmpRegMask);
        noway_assert(tmpReg != REG_NA);
        unsigned  dim      = arrOffset->gtCurrDim;
        unsigned  rank     = arrOffset->gtArrRank;
        var_types elemType = arrOffset->gtArrElemType;
        unsigned  offset   = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);

        // Load tmpReg with the dimension size
        emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_8BYTE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load

        // Evaluate tgtReg = offsetReg*dim_size + indexReg.
        emit->emitIns_R_R_R_R(INS_madd, EA_4BYTE, tgtReg, tmpReg, offsetReg, indexReg);
    }
    else
    {
        regNumber indexReg = genConsumeReg(indexNode);
        if (indexReg != tgtReg)
        {
            inst_RV_RV(INS_mov, tgtReg, indexReg, TYP_INT);
        }
    }
    genProduceReg(arrOffset);
}

// make a temporary indir we can feed to pattern matching routines
// in cases where we don't want to instantiate all the indirs that happen
//
// TODO-Cleanup: move to CodeGenCommon.cpp
GenTreeIndir CodeGen::indirForm(var_types type, GenTree* base)
{
    GenTreeIndir i(GT_IND, type, base, nullptr);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

// make a temporary int we can feed to pattern matching routines
// in cases where we don't want to instantiate
//
// TODO-Cleanup: move to CodeGenCommon.cpp
GenTreeIntCon CodeGen::intForm(var_types type, ssize_t value)
{
    GenTreeIntCon i(type, value);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

instruction CodeGen::genGetInsForOper(genTreeOps oper, var_types type)
{
    instruction ins = INS_brk;

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
// genCodeForShift: Generates the code sequence for a GenTree node that
// represents a bit shift or rotate operation (<<, >>, >>>, rol, ror).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//
void CodeGen::genCodeForShift(GenTreePtr tree)
{
    var_types   targetType = tree->TypeGet();
    genTreeOps  oper       = tree->OperGet();
    instruction ins        = genGetInsForOper(oper, targetType);
    emitAttr    size       = emitTypeSize(tree);

    assert(tree->gtRegNum != REG_NA);

    GenTreePtr operand = tree->gtGetOp1();
    genConsumeReg(operand);

    GenTreePtr shiftBy = tree->gtGetOp2();
    if (!shiftBy->IsCnsIntOrI())
    {
        genConsumeReg(shiftBy);
        getEmitter()->emitIns_R_R_R(ins, size, tree->gtRegNum, operand->gtRegNum, shiftBy->gtRegNum);
    }
    else
    {
        unsigned immWidth   = emitter::getBitWidth(size); // immWidth will be set to 32 or 64
        ssize_t  shiftByImm = shiftBy->gtIntCon.gtIconVal & (immWidth - 1);

        getEmitter()->emitIns_R_R_I(ins, size, tree->gtRegNum, operand->gtRegNum, shiftByImm);
    }

    genProduceReg(tree);
}

// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genUnspillRegIfNeeded(GenTree* tree)
{
    regNumber dstReg = tree->gtRegNum;

    GenTree* unspillTree = tree;
    if (tree->gtOper == GT_RELOAD)
    {
        unspillTree = tree->gtOp.gtOp1;
    }

    if (unspillTree->gtFlags & GTF_SPILLED)
    {
        if (genIsRegCandidateLocal(unspillTree))
        {
            // Reset spilled flag, since we are going to load a local variable from its home location.
            unspillTree->gtFlags &= ~GTF_SPILLED;

            GenTreeLclVarCommon* lcl    = unspillTree->AsLclVarCommon();
            LclVarDsc*           varDsc = &compiler->lvaTable[lcl->gtLclNum];

            var_types   targetType = unspillTree->gtType;
            instruction ins        = ins_Load(targetType, compiler->isSIMDTypeLocalAligned(lcl->gtLclNum));
            emitAttr    attr       = emitTypeSize(targetType);
            emitter*    emit       = getEmitter();

            // Fixes Issue #3326
            attr = emit->emitInsAdjustLoadStoreAttr(ins, attr);

            // Load local variable from its home location.
            inst_RV_TT(ins, dstReg, unspillTree, 0, attr);

            unspillTree->SetInReg();

            // TODO-Review: We would like to call:
            //      genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(tree));
            // instead of the following code, but this ends up hitting this assert:
            //      assert((regSet.rsMaskVars & regMask) == 0);
            // due to issues with LSRA resolution moves.
            // So, just force it for now. This probably indicates a condition that creates a GC hole!
            //
            // Extra note: I think we really want to call something like gcInfo.gcUpdateForRegVarMove,
            // because the variable is not really going live or dead, but that method is somewhat poorly
            // factored because it, in turn, updates rsMaskVars which is part of RegSet not GCInfo.
            // This code exists in other CodeGen*.cpp files.

            // Don't update the variable's location if we are just re-spilling it again.

            if ((unspillTree->gtFlags & GTF_SPILL) == 0)
            {
                genUpdateVarReg(varDsc, tree);
#ifdef DEBUG
                if (VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
                {
                    JITDUMP("\t\t\t\t\t\t\tRemoving V%02u from gcVarPtrSetCur\n", lcl->gtLclNum);
                }
#endif // DEBUG
                VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);

#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("\t\t\t\t\t\t\tV%02u in reg ", lcl->gtLclNum);
                    varDsc->PrintVarReg();
                    printf(" is becoming live  ");
                    compiler->printTreeID(unspillTree);
                    printf("\n");
                }
#endif // DEBUG

                regSet.AddMaskVars(genGetRegMask(varDsc));
            }

            gcInfo.gcMarkRegPtrVal(dstReg, unspillTree->TypeGet());
        }
        else if (unspillTree->IsMultiRegCall())
        {
            GenTreeCall*         call         = unspillTree->AsCall();
            ReturnTypeDesc*      pRetTypeDesc = call->GetReturnTypeDesc();
            unsigned             regCount     = pRetTypeDesc->GetReturnRegCount();
            GenTreeCopyOrReload* reloadTree   = nullptr;
            if (tree->OperGet() == GT_RELOAD)
            {
                reloadTree = tree->AsCopyOrReload();
            }

            // In case of multi-reg call node, GTF_SPILLED flag on it indicates that
            // one or more of its result regs are spilled.  Call node needs to be
            // queried to know which specific result regs to be unspilled.
            for (unsigned i = 0; i < regCount; ++i)
            {
                unsigned flags = call->GetRegSpillFlagByIdx(i);
                if ((flags & GTF_SPILLED) != 0)
                {
                    var_types dstType        = pRetTypeDesc->GetReturnRegType(i);
                    regNumber unspillTreeReg = call->GetRegNumByIdx(i);

                    if (reloadTree != nullptr)
                    {
                        dstReg = reloadTree->GetRegNumByIdx(i);
                        if (dstReg == REG_NA)
                        {
                            dstReg = unspillTreeReg;
                        }
                    }
                    else
                    {
                        dstReg = unspillTreeReg;
                    }

                    TempDsc* t = regSet.rsUnspillInPlace(call, unspillTreeReg, i);
                    getEmitter()->emitIns_R_S(ins_Load(dstType), emitActualTypeSize(dstType), dstReg, t->tdTempNum(),
                                              0);
                    compiler->tmpRlsTemp(t);
                    gcInfo.gcMarkRegPtrVal(dstReg, dstType);
                }
            }

            unspillTree->gtFlags &= ~GTF_SPILLED;
            unspillTree->SetInReg();
        }
        else
        {
            TempDsc* t = regSet.rsUnspillInPlace(unspillTree, unspillTree->gtRegNum);
            getEmitter()->emitIns_R_S(ins_Load(unspillTree->gtType), emitActualTypeSize(unspillTree->TypeGet()), dstReg,
                                      t->tdTempNum(), 0);
            compiler->tmpRlsTemp(t);

            unspillTree->gtFlags &= ~GTF_SPILLED;
            unspillTree->SetInReg();
            gcInfo.gcMarkRegPtrVal(dstReg, unspillTree->TypeGet());
        }
    }
}

// Do Liveness update for a subnodes that is being consumed by codegen
// including the logic for reload in case is needed and also takes care
// of locating the value on the desired register.
void CodeGen::genConsumeRegAndCopy(GenTree* tree, regNumber needReg)
{
    regNumber treeReg = genConsumeReg(tree);
    if (treeReg != needReg)
    {
        var_types targetType = tree->TypeGet();
        inst_RV_RV(ins_Copy(targetType), needReg, treeReg, targetType);
    }
}

void CodeGen::genRegCopy(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_COPY);

    var_types targetType = treeNode->TypeGet();
    regNumber targetReg  = treeNode->gtRegNum;
    assert(targetReg != REG_NA);

    GenTree* op1 = treeNode->gtOp.gtOp1;

    // Check whether this node and the node from which we're copying the value have the same
    // register type.
    // This can happen if (currently iff) we have a SIMD vector type that fits in an integer
    // register, in which case it is passed as an argument, or returned from a call,
    // in an integer register and must be copied if it's in an xmm register.

    if (varTypeIsFloating(treeNode) != varTypeIsFloating(op1))
    {
        inst_RV_RV(INS_fmov, targetReg, genConsumeReg(op1), targetType);
    }
    else
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, genConsumeReg(op1), targetType);
    }

    if (op1->IsLocal())
    {
        // The lclVar will never be a def.
        // If it is a last use, the lclVar will be killed by genConsumeReg(), as usual, and genProduceReg will
        // appropriately set the gcInfo for the copied value.
        // If not, there are two cases we need to handle:
        // - If this is a TEMPORARY copy (indicated by the GTF_VAR_DEATH flag) the variable
        //   will remain live in its original register.
        //   genProduceReg() will appropriately set the gcInfo for the copied value,
        //   and genConsumeReg will reset it.
        // - Otherwise, we need to update register info for the lclVar.

        GenTreeLclVarCommon* lcl = op1->AsLclVarCommon();
        assert((lcl->gtFlags & GTF_VAR_DEF) == 0);

        if ((lcl->gtFlags & GTF_VAR_DEATH) == 0 && (treeNode->gtFlags & GTF_VAR_DEATH) == 0)
        {
            LclVarDsc* varDsc = &compiler->lvaTable[lcl->gtLclNum];

            // If we didn't just spill it (in genConsumeReg, above), then update the register info
            if (varDsc->lvRegNum != REG_STK)
            {
                // The old location is dying
                genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(op1));

                gcInfo.gcMarkRegSetNpt(genRegMask(op1->gtRegNum));

                genUpdateVarReg(varDsc, treeNode);

                // The new location is going live
                genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
            }
        }
    }
    genProduceReg(treeNode);
}

// Do liveness update for a subnode that is being consumed by codegen.
// TODO-Cleanup: move to CodeGenCommon.cpp
regNumber CodeGen::genConsumeReg(GenTree* tree)
{
    if (tree->OperGet() == GT_COPY)
    {
        genRegCopy(tree);
    }
    // Handle the case where we have a lclVar that needs to be copied before use (i.e. because it
    // interferes with one of the other sources (or the target, if it's a "delayed use" register)).
    // TODO-Cleanup: This is a special copyReg case in LSRA - consider eliminating these and
    // always using GT_COPY to make the lclVar location explicit.
    // Note that we have to do this before calling genUpdateLife because otherwise if we spill it
    // the lvRegNum will be set to REG_STK and we will lose track of what register currently holds
    // the lclVar (normally when a lclVar is spilled it is then used from its former register
    // location, which matches the gtRegNum on the node).
    // (Note that it doesn't matter if we call this before or after genUnspillRegIfNeeded
    // because if it's on the stack it will always get reloaded into tree->gtRegNum).
    if (genIsRegCandidateLocal(tree))
    {
        GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
        LclVarDsc*           varDsc = &compiler->lvaTable[lcl->GetLclNum()];
        if ((varDsc->lvRegNum != REG_STK) && (varDsc->lvRegNum != tree->gtRegNum))
        {
            inst_RV_RV(ins_Copy(tree->TypeGet()), tree->gtRegNum, varDsc->lvRegNum);
        }
    }

    genUnspillRegIfNeeded(tree);

    // genUpdateLife() will also spill local var if marked as GTF_SPILL by calling CodeGen::genSpillVar
    genUpdateLife(tree);
    assert(tree->gtRegNum != REG_NA);

    // there are three cases where consuming a reg means clearing the bit in the live mask
    // 1. it was not produced by a local
    // 2. it was produced by a local that is going dead
    // 3. it was produced by a local that does not live in that reg (like one allocated on the stack)

    if (genIsRegCandidateLocal(tree))
    {
        GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
        LclVarDsc*           varDsc = &compiler->lvaTable[lcl->GetLclNum()];
        assert(varDsc->lvLRACandidate);

        if ((tree->gtFlags & GTF_VAR_DEATH) != 0)
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(varDsc->lvRegNum));
        }
        else if (varDsc->lvRegNum == REG_STK)
        {
            // We have loaded this into a register only temporarily
            gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
        }
    }
    else
    {
        gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
    }

    return tree->gtRegNum;
}

// Do liveness update for an address tree: one of GT_LEA, GT_LCL_VAR, or GT_CNS_INT (for call indirect).
// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genConsumeAddress(GenTree* addr)
{
    if (addr->OperGet() == GT_LEA)
    {
        genConsumeAddrMode(addr->AsAddrMode());
    }
    else if (!addr->isContained())
    {
        genConsumeReg(addr);
    }
}

// do liveness update for a subnode that is being consumed by codegen
// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genConsumeAddrMode(GenTreeAddrMode* addr)
{
    if (addr->Base())
        genConsumeReg(addr->Base());
    if (addr->Index())
        genConsumeReg(addr->Index());
}

// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genConsumeRegs(GenTree* tree)
{
    if (tree->isContained())
    {
        if (tree->isIndir())
        {
            genConsumeAddress(tree->AsIndir()->Addr());
        }
        else if (tree->OperGet() == GT_AND)
        {
            // This is the special contained GT_AND that we created in Lowering::LowerCmp()
            // Now we need to consume the operands of the GT_AND node.
            genConsumeOperands(tree->AsOp());
        }
        else
        {
            assert(tree->OperIsLeaf());
        }
    }
    else
    {
        genConsumeReg(tree);
    }
}

//------------------------------------------------------------------------
// genConsumeOperands: Do liveness update for the operands of a unary or binary tree
//
// Arguments:
//    tree - the GenTreeOp whose operands will have their liveness updated.
//
// Return Value:
//    None.
//
// Notes:
//    Note that this logic is localized here because we must do the liveness update in
//    the correct execution order.  This is important because we may have two operands
//    that involve the same lclVar, and if one is marked "lastUse" we must handle it
//    after the first.
// TODO-Cleanup: move to CodeGenCommon.cpp

void CodeGen::genConsumeOperands(GenTreeOp* tree)
{
    GenTree* firstOp  = tree->gtOp1;
    GenTree* secondOp = tree->gtOp2;
    if ((tree->gtFlags & GTF_REVERSE_OPS) != 0)
    {
        assert(secondOp != nullptr);
        firstOp  = secondOp;
        secondOp = tree->gtOp1;
    }
    if (firstOp != nullptr)
    {
        genConsumeRegs(firstOp);
    }
    if (secondOp != nullptr)
    {
        genConsumeRegs(secondOp);
    }
}

// do liveness update for register produced by the current node in codegen
// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genProduceReg(GenTree* tree)
{
    if (tree->gtFlags & GTF_SPILL)
    {
        if (genIsRegCandidateLocal(tree))
        {
            // Store local variable to its home location.
            tree->gtFlags &= ~GTF_REG_VAL;
            inst_TT_RV(ins_Store(tree->gtType, compiler->isSIMDTypeLocalAligned(tree->gtLclVarCommon.gtLclNum)), tree,
                       tree->gtRegNum);
        }
        else
        {
            tree->SetInReg();
            regSet.rsSpillTree(tree->gtRegNum, tree);
            tree->gtFlags |= GTF_SPILLED;
            tree->gtFlags &= ~GTF_SPILL;
            gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
            return;
        }
    }

    genUpdateLife(tree);

    // If we've produced a register, mark it as a pointer, as needed.
    if (tree->gtHasReg())
    {
        // We only mark the register in the following cases:
        // 1. It is not a register candidate local. In this case, we're producing a
        //    register from a local, but the local is not a register candidate. Thus,
        //    we must be loading it as a temp register, and any "last use" flag on
        //    the register wouldn't be relevant.
        // 2. The register candidate local is going dead. There's no point to mark
        //    the register as live, with a GC pointer, if the variable is dead.
        if (!genIsRegCandidateLocal(tree) || ((tree->gtFlags & GTF_VAR_DEATH) == 0))
        {
            gcInfo.gcMarkRegPtrVal(tree->gtRegNum, tree->TypeGet());
        }
    }
    tree->SetInReg();
}

// transfer gc/byref status of src reg to dst reg
// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genTransferRegGCState(regNumber dst, regNumber src)
{
    regMaskTP srcMask = genRegMask(src);
    regMaskTP dstMask = genRegMask(dst);

    if (gcInfo.gcRegGCrefSetCur & srcMask)
    {
        gcInfo.gcMarkRegSetGCref(dstMask);
    }
    else if (gcInfo.gcRegByrefSetCur & srcMask)
    {
        gcInfo.gcMarkRegSetByref(dstMask);
    }
    else
    {
        gcInfo.gcMarkRegSetNpt(dstMask);
    }
}

// generates an ip-relative call or indirect call via reg ('call reg')
//     pass in 'addr' for a relative call or 'base' for a indirect register call
//     methHnd - optional, only used for pretty printing
//     retSize - emitter type of return for GC purposes, should be EA_BYREF, EA_GCREF, or EA_PTRSIZE(not GC)
// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genEmitCall(int                   callType,
                          CORINFO_METHOD_HANDLE methHnd,
                          INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) void* addr,
                          emitAttr                                               retSize,
                          emitAttr                                               secondRetSize,
                          IL_OFFSETX                                             ilOffset,
                          regNumber                                              base,
                          bool                                                   isJump,
                          bool                                                   isNoGC)
{

    getEmitter()->emitIns_Call(emitter::EmitCallType(callType), methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr, 0,
                               retSize, secondRetSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, ilOffset, base, REG_NA, 0, 0, isJump,
                               emitter::emitNoGChelper(compiler->eeGetHelperNum(methHnd)));
}

// generates an indirect call via addressing mode (call []) given an indir node
//     methHnd - optional, only used for pretty printing
//     retSize - emitter type of return for GC purposes, should be EA_BYREF, EA_GCREF, or EA_PTRSIZE(not GC)
// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genEmitCall(int                   callType,
                          CORINFO_METHOD_HANDLE methHnd,
                          INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) GenTreeIndir* indir,
                          emitAttr                                                       retSize,
                          emitAttr                                                       secondRetSize,
                          IL_OFFSETX                                                     ilOffset)
{
    genConsumeAddress(indir->Addr());

    getEmitter()->emitIns_Call(emitter::EmitCallType(callType), methHnd, INDEBUG_LDISASM_COMMA(sigInfo) nullptr, 0,
                               retSize, secondRetSize, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, ilOffset, indir->Base() ? indir->Base()->gtRegNum : REG_NA,
                               indir->Index() ? indir->Index()->gtRegNum : REG_NA, indir->Scale(), indir->Offset());
}

// Produce code for a GT_CALL node
void CodeGen::genCallInstruction(GenTreePtr node)
{
    GenTreeCall* call = node->AsCall();

    assert(call->gtOper == GT_CALL);

    gtCallTypes callType = (gtCallTypes)call->gtCallType;

    IL_OFFSETX ilOffset = BAD_IL_OFFSET;

    // all virtuals should have been expanded into a control expression
    assert(!call->IsVirtual() || call->gtControlExpr || call->gtCallAddr);

    // Consume all the arg regs
    for (GenTreePtr list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->IsList());

        GenTreePtr argNode = list->Current();

        fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(call, argNode->gtSkipReloadOrCopy());
        assert(curArgTabEntry);

        if (curArgTabEntry->regNum == REG_STK)
            continue;

        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_LIST)
        {
            GenTreeArgList* argListPtr   = argNode->AsArgList();
            unsigned        iterationNum = 0;
            regNumber       argReg       = curArgTabEntry->regNum;
            for (; argListPtr != nullptr; argListPtr = argListPtr->Rest(), iterationNum++)
            {
                GenTreePtr putArgRegNode = argListPtr->gtOp.gtOp1;
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                genConsumeReg(putArgRegNode);

                if (putArgRegNode->gtRegNum != argReg)
                {
                    inst_RV_RV(ins_Move_Extend(putArgRegNode->TypeGet(), putArgRegNode->InReg()), argReg,
                               putArgRegNode->gtRegNum);
                }

                argReg = genRegArgNext(argReg);
            }
        }
        else
        {
            regNumber argReg = curArgTabEntry->regNum;
            genConsumeReg(argNode);
            if (argNode->gtRegNum != argReg)
            {
                inst_RV_RV(ins_Move_Extend(argNode->TypeGet(), argNode->InReg()), argReg, argNode->gtRegNum);
            }
        }

        // In the case of a varargs call,
        // the ABI dictates that if we have floating point args,
        // we must pass the enregistered arguments in both the
        // integer and floating point registers so, let's do that.
        if (call->IsVarargs() && varTypeIsFloating(argNode))
        {
            NYI_ARM64("CodeGen - IsVarargs");
        }
    }

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, regThis, 0);
    }

    // Either gtControlExpr != null or gtCallAddr != null or it is a direct non-virtual call to a user or helper method.
    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = call->gtControlExpr;
    if (callType == CT_INDIRECT)
    {
        assert(target == nullptr);
        target  = call->gtCall.gtCallAddr;
        methHnd = nullptr;
    }
    else
    {
        methHnd = call->gtCallMethHnd;
    }

    CORINFO_SIG_INFO* sigInfo = nullptr;
#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (callType != CT_HELPER)
    {
        sigInfo = call->callSig;
    }
#endif // DEBUG

    // If fast tail call, then we are done.  In this case we setup the args (both reg args
    // and stack args in incoming arg area) and call target in IP0.  Epilog sequence would
    // generate "br IP0".
    if (call->IsFastTailCall())
    {
        // Don't support fast tail calling JIT helpers
        assert(callType != CT_HELPER);

        // Fast tail calls materialize call target either in gtControlExpr or in gtCallAddr.
        assert(target != nullptr);

        genConsumeReg(target);

        if (target->gtRegNum != REG_IP0)
        {
            inst_RV_RV(INS_mov, REG_IP0, target->gtRegNum);
        }
        return;
    }

    // For a pinvoke to unmanged code we emit a label to clear
    // the GC pointer state before the callsite.
    // We can't utilize the typical lazy killing of GC pointers
    // at (or inside) the callsite.
    if (call->IsUnmanaged())
    {
        genDefineTempLabel(genCreateTempLabel());
    }

    // Determine return value size(s).
    ReturnTypeDesc* pRetTypeDesc  = call->GetReturnTypeDesc();
    emitAttr        retSize       = EA_PTRSIZE;
    emitAttr        secondRetSize = EA_UNKNOWN;

    if (call->HasMultiRegRetVal())
    {
        retSize       = emitTypeSize(pRetTypeDesc->GetReturnRegType(0));
        secondRetSize = emitTypeSize(pRetTypeDesc->GetReturnRegType(1));
    }
    else
    {
        assert(!varTypeIsStruct(call));

        if (call->gtType == TYP_REF || call->gtType == TYP_ARRAY)
        {
            retSize = EA_GCREF;
        }
        else if (call->gtType == TYP_BYREF)
        {
            retSize = EA_BYREF;
        }
    }

#ifdef DEBUGGING_SUPPORT
    // We need to propagate the IL offset information to the call instruction, so we can emit
    // an IL to native mapping record for the call, to support managed return value debugging.
    // We don't want tail call helper calls that were converted from normal calls to get a record,
    // so we skip this hash table lookup logic in that case.
    if (compiler->opts.compDbgInfo && compiler->genCallSite2ILOffsetMap != nullptr && !call->IsTailCall())
    {
        (void)compiler->genCallSite2ILOffsetMap->Lookup(call, &ilOffset);
    }
#endif // DEBUGGING_SUPPORT

    if (target != nullptr)
    {
        // For Arm64 a call target can not be a contained indirection
        assert(!target->isContainedIndir());

        // We have already generated code for gtControlExpr evaluating it into a register.
        // We just need to emit "call reg" in this case.
        //
        assert(genIsValidIntReg(target->gtRegNum));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize, secondRetSize, ilOffset, genConsumeReg(target));
    }
    else
    {
        // Generate a direct call to a non-virtual user defined or helper method
        assert(callType == CT_HELPER || callType == CT_USER_FUNC);

        void* addr = nullptr;
        if (callType == CT_HELPER)
        {
            // Direct call to a helper method.
            CorInfoHelpFunc helperNum = compiler->eeGetHelperNum(methHnd);
            noway_assert(helperNum != CORINFO_HELP_UNDEF);

            void* pAddr = nullptr;
            addr        = compiler->compGetHelperFtn(helperNum, (void**)&pAddr);

            if (addr == nullptr)
            {
                addr = pAddr;
            }
        }
        else
        {
            // Direct call to a non-virtual user function.
            CORINFO_ACCESS_FLAGS aflags = CORINFO_ACCESS_ANY;
            if (call->IsSameThis())
            {
                aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_THIS);
            }

            if ((call->NeedsNullCheck()) == 0)
            {
                aflags = (CORINFO_ACCESS_FLAGS)(aflags | CORINFO_ACCESS_NONNULL);
            }

            CORINFO_CONST_LOOKUP addrInfo;
            compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo, aflags);

            addr = addrInfo.addr;
        }
#if 0
        // Use this path if you want to load an absolute call target using 
        //  a sequence of movs followed by an indirect call (blr instruction)

        // Load the call target address in x16
        instGen_Set_Reg_To_Imm(EA_8BYTE, REG_IP0, (ssize_t) addr);

        // indirect call to constant address in IP0
        genEmitCall(emitter::EC_INDIR_R,
                    methHnd, 
                    INDEBUG_LDISASM_COMMA(sigInfo)
                    nullptr, //addr
                    retSize,
                    secondRetSize,
                    ilOffset,
                    REG_IP0);
#else
        // Non-virtual direct call to known addresses
        genEmitCall(emitter::EC_FUNC_TOKEN, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr, retSize, secondRetSize,
                    ilOffset);
#endif
    }

    // if it was a pinvoke we may have needed to get the address of a label
    if (genPendingCallLabel)
    {
        assert(call->IsUnmanaged());
        genDefineTempLabel(genPendingCallLabel);
        genPendingCallLabel = nullptr;
    }

    // Update GC info:
    // All Callee arg registers are trashed and no longer contain any GC pointers.
    // TODO-ARM64-Bug?: As a matter of fact shouldn't we be killing all of callee trashed regs here?
    // For now we will assert that other than arg regs gc ref/byref set doesn't contain any other
    // registers from RBM_CALLEE_TRASH
    assert((gcInfo.gcRegGCrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    assert((gcInfo.gcRegByrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    gcInfo.gcRegGCrefSetCur &= ~RBM_ARG_REGS;
    gcInfo.gcRegByrefSetCur &= ~RBM_ARG_REGS;

    var_types returnType = call->TypeGet();
    if (returnType != TYP_VOID)
    {
        regNumber returnReg;

        if (call->HasMultiRegRetVal())
        {
            assert(pRetTypeDesc != nullptr);
            unsigned regCount = pRetTypeDesc->GetReturnRegCount();

            // If regs allocated to call node are different from ABI return
            // regs in which the call has returned its result, move the result
            // to regs allocated to call node.
            for (unsigned i = 0; i < regCount; ++i)
            {
                var_types regType      = pRetTypeDesc->GetReturnRegType(i);
                returnReg              = pRetTypeDesc->GetABIReturnReg(i);
                regNumber allocatedReg = call->GetRegNumByIdx(i);
                if (returnReg != allocatedReg)
                {
                    inst_RV_RV(ins_Copy(regType), allocatedReg, returnReg, regType);
                }
            }
        }
        else
        {
            if (varTypeIsFloating(returnType))
            {
                returnReg = REG_FLOATRET;
            }
            else
            {
                returnReg = REG_INTRET;
            }

            if (call->gtRegNum != returnReg)
            {
                inst_RV_RV(ins_Copy(returnType), call->gtRegNum, returnReg, returnType);
            }
        }

        genProduceReg(call);
    }

    // If there is nothing next, that means the result is thrown away, so this value is not live.
    // However, for minopts or debuggable code, we keep it live to support managed return value debugging.
    if ((call->gtNext == nullptr) && !compiler->opts.MinOpts() && !compiler->opts.compDbgCode)
    {
        gcInfo.gcMarkRegSetNpt(RBM_INTRET);
    }
}

// Produce code for a GT_JMP node.
// The arguments of the caller needs to be transferred to the callee before exiting caller.
// The actual jump to callee is generated as part of caller epilog sequence.
// Therefore the codegen of GT_JMP is to ensure that the callee arguments are correctly setup.
void CodeGen::genJmpMethod(GenTreePtr jmp)
{
    assert(jmp->OperGet() == GT_JMP);
    assert(compiler->compJmpOpUsed);

    // If no arguments, nothing to do
    if (compiler->info.compArgsCount == 0)
    {
        return;
    }

    // Make sure register arguments are in their initial registers
    // and stack arguments are put back as well.
    unsigned   varNum;
    LclVarDsc* varDsc;

    // First move any en-registered stack arguments back to the stack.
    // At the same time any reg arg not in correct reg is moved back to its stack location.
    //
    // We are not strictly required to spill reg args that are not in the desired reg for a jmp call
    // But that would require us to deal with circularity while moving values around.  Spilling
    // to stack makes the implementation simple, which is not a bad trade off given Jmp calls
    // are not frequent.
    for (varNum = 0; (varNum < compiler->info.compArgsCount); varNum++)
    {
        varDsc = compiler->lvaTable + varNum;

        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaTable + fieldVarNum;
        }
        noway_assert(varDsc->lvIsParam);

        if (varDsc->lvIsRegArg && (varDsc->lvRegNum != REG_STK))
        {
            // Skip reg args which are already in its right register for jmp call.
            // If not, we will spill such args to their stack locations.
            //
            // If we need to generate a tail call profiler hook, then spill all
            // arg regs to free them up for the callback.
            if (!compiler->compIsProfilerHookNeeded() && (varDsc->lvRegNum == varDsc->lvArgReg))
                continue;
        }
        else if (varDsc->lvRegNum == REG_STK)
        {
            // Skip args which are currently living in stack.
            continue;
        }

        // If we came here it means either a reg argument not in the right register or
        // a stack argument currently living in a register.  In either case the following
        // assert should hold.
        assert(varDsc->lvRegNum != REG_STK);
        assert(varDsc->TypeGet() != TYP_STRUCT);
        var_types storeType = genActualType(varDsc->TypeGet());
        emitAttr  storeSize = emitActualTypeSize(storeType);

        getEmitter()->emitIns_S_R(ins_Store(storeType), storeSize, varDsc->lvRegNum, varNum, 0);

        // Update lvRegNum life and GC info to indicate lvRegNum is dead and varDsc stack slot is going live.
        // Note that we cannot modify varDsc->lvRegNum here because another basic block may not be expecting it.
        // Therefore manually update life of varDsc->lvRegNum.
        regMaskTP tempMask = genRegMask(varDsc->lvRegNum);
        regSet.RemoveMaskVars(tempMask);
        gcInfo.gcMarkRegSetNpt(tempMask);
        if (compiler->lvaIsGCTracked(varDsc))
        {
            VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varNum);
        }
    }

#ifdef PROFILING_SUPPORTED
    // At this point all arg regs are free.
    // Emit tail call profiler callback.
    genProfilingLeaveCallback(CORINFO_HELP_PROF_FCN_TAILCALL);
#endif

    // Next move any un-enregistered register arguments back to their register.
    regMaskTP fixedIntArgMask = RBM_NONE;    // tracks the int arg regs occupying fixed args in case of a vararg method.
    unsigned  firstArgVarNum  = BAD_VAR_NUM; // varNum of the first argument in case of a vararg method.
    for (varNum = 0; (varNum < compiler->info.compArgsCount); varNum++)
    {
        varDsc = compiler->lvaTable + varNum;
        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaTable + fieldVarNum;
        }
        noway_assert(varDsc->lvIsParam);

        // Skip if arg not passed in a register.
        if (!varDsc->lvIsRegArg)
            continue;

        // Register argument
        noway_assert(isRegParamType(genActualType(varDsc->TypeGet())));

        // Is register argument already in the right register?
        // If not load it from its stack location.
        regNumber argReg     = varDsc->lvArgReg; // incoming arg register
        regNumber argRegNext = REG_NA;

        if (varDsc->lvRegNum != argReg)
        {
            var_types loadType = TYP_UNDEF;
            if (varTypeIsStruct(varDsc))
            {
                // Must be <= 16 bytes or else it wouldn't be passed in registers
                noway_assert(EA_SIZE_IN_BYTES(varDsc->lvSize()) <= MAX_PASS_MULTIREG_BYTES);
                loadType = compiler->getJitGCType(varDsc->lvGcLayout[0]);
            }
            else
            {
                loadType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));
            }
            emitAttr loadSize = emitActualTypeSize(loadType);
            getEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argReg, varNum, 0);

            // Update argReg life and GC Info to indicate varDsc stack slot is dead and argReg is going live.
            // Note that we cannot modify varDsc->lvRegNum here because another basic block may not be expecting it.
            // Therefore manually update life of argReg.  Note that GT_JMP marks the end of the basic block
            // and after which reg life and gc info will be recomputed for the new block in genCodeForBBList().
            regSet.AddMaskVars(genRegMask(argReg));
            gcInfo.gcMarkRegPtrVal(argReg, loadType);

            if (compiler->lvaIsMultiregStruct(varDsc))
            {
                if (varDsc->lvIsHfa())
                {
                    NYI_ARM64("CodeGen::genJmpMethod with multireg HFA arg");
                }

                // Restore the second register.
                argRegNext = genRegArgNext(argReg);

                loadType = compiler->getJitGCType(varDsc->lvGcLayout[1]);
                loadSize = emitActualTypeSize(loadType);
                getEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argRegNext, varNum, TARGET_POINTER_SIZE);

                regSet.AddMaskVars(genRegMask(argRegNext));
                gcInfo.gcMarkRegPtrVal(argRegNext, loadType);
            }

            if (compiler->lvaIsGCTracked(varDsc))
            {
                VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varNum);
            }
        }

        // In case of a jmp call to a vararg method ensure only integer registers are passed.
        if (compiler->info.compIsVarArgs)
        {
            assert((genRegMask(argReg) & RBM_ARG_REGS) != RBM_NONE);

            fixedIntArgMask |= genRegMask(argReg);

            if (compiler->lvaIsMultiregStruct(varDsc))
            {
                assert(argRegNext != REG_NA);
                fixedIntArgMask |= genRegMask(argRegNext);
            }

            if (argReg == REG_ARG_0)
            {
                assert(firstArgVarNum == BAD_VAR_NUM);
                firstArgVarNum = varNum;
            }
        }
    }

    // Jmp call to a vararg method - if the method has fewer than 8 fixed arguments,
    // load the remaining integer arg registers from the corresponding
    // shadow stack slots.  This is for the reason that we don't know the number and type
    // of non-fixed params passed by the caller, therefore we have to assume the worst case
    // of caller passing all 8 integer arg regs.
    //
    // The caller could have passed gc-ref/byref type var args.  Since these are var args
    // the callee no way of knowing their gc-ness.  Therefore, mark the region that loads
    // remaining arg registers from shadow stack slots as non-gc interruptible.
    if (fixedIntArgMask != RBM_NONE)
    {
        assert(compiler->info.compIsVarArgs);
        assert(firstArgVarNum != BAD_VAR_NUM);

        regMaskTP remainingIntArgMask = RBM_ARG_REGS & ~fixedIntArgMask;
        if (remainingIntArgMask != RBM_NONE)
        {
            getEmitter()->emitDisableGC();
            for (int argNum = 0, argOffset = 0; argNum < MAX_REG_ARG; ++argNum)
            {
                regNumber argReg     = intArgRegs[argNum];
                regMaskTP argRegMask = genRegMask(argReg);

                if ((remainingIntArgMask & argRegMask) != 0)
                {
                    remainingIntArgMask &= ~argRegMask;
                    getEmitter()->emitIns_R_S(INS_ldr, EA_8BYTE, argReg, firstArgVarNum, argOffset);
                }

                argOffset += REGSIZE_BYTES;
            }
            getEmitter()->emitEnableGC();
        }
    }
}

// produce code for a GT_LEA subnode
void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    genConsumeOperands(lea);
    emitter* emit   = getEmitter();
    emitAttr size   = emitTypeSize(lea);
    unsigned offset = lea->gtOffset;

    // In ARM64 we can only load addresses of the form:
    //
    // [Base + index*scale]
    // [Base + Offset]
    // [Literal] (PC-Relative)
    //
    // So for the case of a LEA node of the form [Base + Index*Scale + Offset] we will generate:
    // destReg = baseReg + indexReg * scale;
    // destReg = destReg + offset;
    //
    // TODO-ARM64-CQ: The purpose of the GT_LEA node is to directly reflect a single target architecture
    //             addressing mode instruction.  Currently we're 'cheating' by producing one or more
    //             instructions to generate the addressing mode so we need to modify lowering to
    //             produce LEAs that are a 1:1 relationship to the ARM64 architecture.
    if (lea->Base() && lea->Index())
    {
        GenTree* memBase = lea->Base();
        GenTree* index   = lea->Index();
        unsigned offset  = lea->gtOffset;

        DWORD lsl;

        assert(isPow2(lea->gtScale));
        BitScanForward(&lsl, lea->gtScale);

        assert(lsl <= 4);

        if (offset != 0)
        {
            regMaskTP tmpRegMask = lea->gtRsvdRegs;
            regNumber tmpReg     = genRegNumFromMask(tmpRegMask);
            noway_assert(tmpReg != REG_NA);

            if (emitter::emitIns_valid_imm_for_add(offset, EA_8BYTE))
            {
                if (lsl > 0)
                {
                    // Generate code to set tmpReg = base + index*scale
                    emit->emitIns_R_R_R_I(INS_add, EA_PTRSIZE, tmpReg, memBase->gtRegNum, index->gtRegNum, lsl,
                                          INS_OPTS_LSL);
                }
                else // no scale
                {
                    // Generate code to set tmpReg = base + index
                    emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, tmpReg, memBase->gtRegNum, index->gtRegNum);
                }

                // Then compute target reg from [tmpReg + offset]
                emit->emitIns_R_R_I(INS_add, size, lea->gtRegNum, tmpReg, offset);
                ;
            }
            else // large offset
            {
                // First load/store tmpReg with the large offset constant
                instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);
                // Then add the base register
                //      rd = rd + base
                emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, tmpReg, tmpReg, memBase->gtRegNum);

                noway_assert(tmpReg != index->gtRegNum);

                // Then compute target reg from [tmpReg + index*scale]
                emit->emitIns_R_R_R_I(INS_add, size, lea->gtRegNum, tmpReg, index->gtRegNum, lsl, INS_OPTS_LSL);
            }
        }
        else
        {
            if (lsl > 0)
            {
                // Then compute target reg from [base + index*scale]
                emit->emitIns_R_R_R_I(INS_add, size, lea->gtRegNum, memBase->gtRegNum, index->gtRegNum, lsl,
                                      INS_OPTS_LSL);
            }
            else
            {
                // Then compute target reg from [base + index]
                emit->emitIns_R_R_R(INS_add, size, lea->gtRegNum, memBase->gtRegNum, index->gtRegNum);
            }
        }
    }
    else if (lea->Base())
    {
        GenTree* memBase = lea->Base();

        if (emitter::emitIns_valid_imm_for_add(offset, EA_8BYTE))
        {
            if (offset != 0)
            {
                // Then compute target reg from [memBase + offset]
                emit->emitIns_R_R_I(INS_add, size, lea->gtRegNum, memBase->gtRegNum, offset);
            }
            else // offset is zero
            {
                emit->emitIns_R_R(INS_mov, size, lea->gtRegNum, memBase->gtRegNum);
            }
        }
        else
        {
            // We require a tmpReg to hold the offset
            regMaskTP tmpRegMask = lea->gtRsvdRegs;
            regNumber tmpReg     = genRegNumFromMask(tmpRegMask);
            noway_assert(tmpReg != REG_NA);

            // First load tmpReg with the large offset constant
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

            // Then compute target reg from [memBase + tmpReg]
            emit->emitIns_R_R_R(INS_add, size, lea->gtRegNum, memBase->gtRegNum, tmpReg);
        }
    }
    else if (lea->Index())
    {
        // If we encounter a GT_LEA node without a base it means it came out
        // when attempting to optimize an arbitrary arithmetic expression during lower.
        // This is currently disabled in ARM64 since we need to adjust lower to account
        // for the simpler instructions ARM64 supports.
        // TODO-ARM64-CQ:  Fix this and let LEA optimize arithmetic trees too.
        assert(!"We shouldn't see a baseless address computation during CodeGen for ARM64");
    }

    genProduceReg(lea);
}

//-------------------------------------------------------------------------------------------
// genJumpKindsForTree:  Determine the number and kinds of conditional branches
//                       necessary to implement the given GT_CMP node
//
// Arguments:
//   cmpTree           - (input) The GenTree node that is used to set the Condition codes
//                     - The GenTree Relop node that was used to set the Condition codes
//   jmpKind[2]        - (output) One or two conditional branch instructions
//   jmpToTrueLabel[2] - (output) On Arm64 both branches will always branch to the true label
//
// Return Value:
//    Sets the proper values into the array elements of jmpKind[] and jmpToTrueLabel[]
//
// Assumptions:
//    At least one conditional branch instruction will be returned.
//    Typically only one conditional branch is needed
//     and the second jmpKind[] value is set to EJ_NONE
//-------------------------------------------------------------------------------------------

// static
void CodeGen::genJumpKindsForTree(GenTreePtr cmpTree, emitJumpKind jmpKind[2], bool jmpToTrueLabel[2])
{
    // On Arm64 both branches will always branch to the true label
    jmpToTrueLabel[0] = true;
    jmpToTrueLabel[1] = true;

    // For integer comparisons just use genJumpKindForOper
    if (!varTypeIsFloating(cmpTree->gtOp.gtOp1->gtEffectiveVal()))
    {
        CompareKind compareKind = ((cmpTree->gtFlags & GTF_UNSIGNED) != 0) ? CK_UNSIGNED : CK_SIGNED;
        jmpKind[0]              = genJumpKindForOper(cmpTree->gtOper, compareKind);
        jmpKind[1]              = EJ_NONE;
    }
    else // We have a Floating Point Compare operation
    {
        assert(cmpTree->OperIsCompare());

        // For details on this mapping, see the ARM64 Condition Code
        // table at section C1.2.3 in the ARMV8 architecture manual
        //

        // We must check the GTF_RELOP_NAN_UN to find out
        // if we need to branch when we have a NaN operand.
        //
        if ((cmpTree->gtFlags & GTF_RELOP_NAN_UN) != 0)
        {
            // Must branch if we have an NaN, unordered
            switch (cmpTree->gtOper)
            {
                case GT_EQ:
                    jmpKind[0] = EJ_eq; // branch or set when equal (and no NaN's)
                    jmpKind[1] = EJ_vs; // branch or set when we have a NaN
                    break;

                case GT_NE:
                    jmpKind[0] = EJ_ne; // branch or set when not equal (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_LT:
                    jmpKind[0] = EJ_lt; // branch or set when less than (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_LE:
                    jmpKind[0] = EJ_le; // branch or set when less than or equal (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GT:
                    jmpKind[0] = EJ_hi; // branch or set when greater than (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GE:
                    jmpKind[0] = EJ_hs; // branch or set when greater than or equal (or have NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                default:
                    unreached();
            }
        }
        else // ((cmpTree->gtFlags & GTF_RELOP_NAN_UN) == 0)
        {
            // Do not branch if we have an NaN, unordered
            switch (cmpTree->gtOper)
            {
                case GT_EQ:
                    jmpKind[0] = EJ_eq; // branch or set when equal (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_NE:
                    jmpKind[0] = EJ_gt; // branch or set when greater than (and no NaN's)
                    jmpKind[1] = EJ_lo; // branch or set when less than (and no NaN's)
                    break;

                case GT_LT:
                    jmpKind[0] = EJ_lo; // branch or set when less than (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_LE:
                    jmpKind[0] = EJ_ls; // branch or set when less than or equal (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GT:
                    jmpKind[0] = EJ_gt; // branch or set when greater than (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                case GT_GE:
                    jmpKind[0] = EJ_ge; // branch or set when greater than or equal (and no NaN's)
                    jmpKind[1] = EJ_NONE;
                    break;

                default:
                    unreached();
            }
        }
    }
}

//-------------------------------------------------------------------------------------------
// genSetRegToCond:  Set a register 'dstReg' to the appropriate one or zero value
//                   corresponding to a binary Relational operator result.
//
// Arguments:
//   dstReg          - The target register to set to 1 or 0
//   tree            - The GenTree Relop node that was used to set the Condition codes
//
// Return Value:     none
//
// Notes:
//    A full 64-bit value of either 1 or 0 is setup in the 'dstReg'
//-------------------------------------------------------------------------------------------

void CodeGen::genSetRegToCond(regNumber dstReg, GenTreePtr tree)
{
    emitJumpKind jumpKind[2];
    bool         branchToTrueLabel[2];
    genJumpKindsForTree(tree, jumpKind, branchToTrueLabel);
    assert(jumpKind[0] != EJ_NONE);

    // Set the reg according to the flags
    inst_SET(jumpKind[0], dstReg);

    // Do we need to use two operation to set the flags?
    //
    if (jumpKind[1] != EJ_NONE)
    {
        emitter* emit    = getEmitter();
        bool     ordered = ((tree->gtFlags & GTF_RELOP_NAN_UN) == 0);
        insCond  secondCond;

        // The only ones that require two operations are the
        // floating point compare operations of BEQ or BNE.UN
        //
        if (tree->gtOper == GT_EQ)
        {
            // This must be an ordered comparison.
            assert(ordered);
            assert(jumpKind[1] == EJ_vs); // We complement this value
            secondCond = INS_COND_VC;     // for the secondCond
        }
        else // gtOper == GT_NE
        {
            // This must be BNE.UN (unordered comparison)
            assert((tree->gtOper == GT_NE) && !ordered);
            assert(jumpKind[1] == EJ_lo); // We complement this value
            secondCond = INS_COND_HS;     // for the secondCond
        }

        // The second instruction is a 'csinc' instruction that either selects the previous dstReg
        // or increments the ZR register, which produces a 1 result.

        emit->emitIns_R_R_R_COND(INS_csinc, EA_8BYTE, dstReg, dstReg, REG_ZR, secondCond);
    }
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer cast
//    This method handles integer overflow checking casts
//    as well as ordinary integer casts.
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    The treeNode is not a contained node and must have an assigned register.
//    For a signed convert from byte, the source must be in a byte-addressable register.
//    Neither the source nor target type can be a floating point type.
//
// TODO-ARM64-CQ: Allow castOp to be a contained node without an assigned register.
//
void CodeGen::genIntToIntCast(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_CAST);

    GenTreePtr castOp = treeNode->gtCast.CastOp();
    emitter*   emit   = getEmitter();

    var_types dstType     = treeNode->CastToType();
    var_types srcType     = genActualType(castOp->TypeGet());
    emitAttr  movSize     = emitActualTypeSize(dstType);
    bool      movRequired = false;

    regNumber targetReg = treeNode->gtRegNum;
    regNumber sourceReg = castOp->gtRegNum;

    // For Long to Int conversion we will have a reserved integer register to hold the immediate mask
    regNumber tmpReg = (treeNode->gtRsvdRegs == RBM_NONE) ? REG_NA : genRegNumFromMask(treeNode->gtRsvdRegs);

    assert(genIsValidIntReg(targetReg));
    assert(genIsValidIntReg(sourceReg));

    instruction ins = INS_invalid;

    genConsumeReg(castOp);
    Lowering::CastInfo castInfo;

    // Get information about the cast.
    Lowering::getCastDescription(treeNode, &castInfo);

    if (castInfo.requiresOverflowCheck)
    {

        emitAttr cmpSize = EA_ATTR(genTypeSize(srcType));

        if (castInfo.signCheckOnly)
        {
            // We only need to check for a negative value in sourceReg
            emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, 0);
            emitJumpKind jmpLT = genJumpKindForOper(GT_LT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpLT, SCK_OVERFLOW);
            noway_assert(genTypeSize(srcType) == 4 || genTypeSize(srcType) == 8);
            // This is only interesting case to ensure zero-upper bits.
            if ((srcType == TYP_INT) && (dstType == TYP_ULONG))
            {
                // cast to TYP_ULONG:
                // We use a mov with size=EA_4BYTE
                // which will zero out the upper bits
                movSize     = EA_4BYTE;
                movRequired = true;
            }
        }
        else if (castInfo.unsignedSource || castInfo.unsignedDest)
        {
            // When we are converting from/to unsigned,
            // we only have to check for any bits set in 'typeMask'

            noway_assert(castInfo.typeMask != 0);
            emit->emitIns_R_I(INS_tst, cmpSize, sourceReg, castInfo.typeMask);
            emitJumpKind jmpNotEqual = genJumpKindForOper(GT_NE, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpNotEqual, SCK_OVERFLOW);
        }
        else
        {
            // For a narrowing signed cast
            //
            // We must check the value is in a signed range.

            // Compare with the MAX

            noway_assert((castInfo.typeMin != 0) && (castInfo.typeMax != 0));

            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMax, cmpSize))
            {
                emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, castInfo.typeMax);
            }
            else
            {
                noway_assert(tmpReg != REG_NA);
                instGen_Set_Reg_To_Imm(cmpSize, tmpReg, castInfo.typeMax);
                emit->emitIns_R_R(INS_cmp, cmpSize, sourceReg, tmpReg);
            }

            emitJumpKind jmpGT = genJumpKindForOper(GT_GT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpGT, SCK_OVERFLOW);

            // Compare with the MIN

            if (emitter::emitIns_valid_imm_for_cmp(castInfo.typeMin, cmpSize))
            {
                emit->emitIns_R_I(INS_cmp, cmpSize, sourceReg, castInfo.typeMin);
            }
            else
            {
                noway_assert(tmpReg != REG_NA);
                instGen_Set_Reg_To_Imm(cmpSize, tmpReg, castInfo.typeMin);
                emit->emitIns_R_R(INS_cmp, cmpSize, sourceReg, tmpReg);
            }

            emitJumpKind jmpLT = genJumpKindForOper(GT_LT, CK_SIGNED);
            genJumpToThrowHlpBlk(jmpLT, SCK_OVERFLOW);
        }
        ins = INS_mov;
    }
    else // Non-overflow checking cast.
    {
        if (genTypeSize(srcType) == genTypeSize(dstType))
        {
            ins = INS_mov;
        }
        else
        {
            var_types extendType = TYP_UNKNOWN;

            // If we need to treat a signed type as unsigned
            if ((treeNode->gtFlags & GTF_UNSIGNED) != 0)
            {
                extendType  = genUnsignedType(srcType);
                movSize     = emitTypeSize(extendType);
                movRequired = true;
            }
            else
            {
                if (genTypeSize(srcType) < genTypeSize(dstType))
                {
                    extendType = srcType;
                    if (srcType == TYP_UINT)
                    {
                        // If we are casting from a smaller type to
                        // a larger type, then we need to make sure the
                        // higher 4 bytes are zero to gaurentee the correct value.
                        // Therefore using a mov with EA_4BYTE in place of EA_8BYTE
                        // will zero the upper bits
                        movSize     = EA_4BYTE;
                        movRequired = true;
                    }
                }
                else // (genTypeSize(srcType) > genTypeSize(dstType))
                {
                    extendType = dstType;
                    if (dstType == TYP_INT)
                    {
                        movSize = EA_8BYTE; // a sxtw instruction requires EA_8BYTE
                    }
                }
            }

            ins = ins_Move_Extend(extendType, castOp->InReg());
        }
    }

    // We should never be generating a load from memory instruction here!
    assert(!emit->emitInsIsLoad(ins));

    if ((ins != INS_mov) || movRequired || (targetReg != sourceReg))
    {
        emit->emitIns_R_R(ins, movSize, targetReg, sourceReg);
    }

    genProduceReg(treeNode);
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
//    The cast is between float and double or vice versa.
//
void CodeGen::genFloatToFloatCast(GenTreePtr treeNode)
{
    // float <--> double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidFloatReg(targetReg));

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());               // Cannot be contained
    assert(genIsValidFloatReg(op1->gtRegNum)); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());

    // treeNode must be a reg
    assert(!treeNode->isContained());

    if (srcType != dstType)
    {
        insOpts cvtOption = (srcType == TYP_FLOAT) ? INS_OPTS_S_TO_D  // convert Single to Double
                                                   : INS_OPTS_D_TO_S; // convert Double to Single

        getEmitter()->emitIns_R_R(INS_fcvt, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum, cvtOption);
    }
    else if (treeNode->gtRegNum != op1->gtRegNum)
    {
        // If double to double cast or float to float cast. Emit a move instruction.
        getEmitter()->emitIns_R_R(INS_mov, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }

    genProduceReg(treeNode);
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
void CodeGen::genIntToFloatCast(GenTreePtr treeNode)
{
    // int type --> float/double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidFloatReg(targetReg));

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());             // Cannot be contained
    assert(genIsValidIntReg(op1->gtRegNum)); // Must be a valid int reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(!varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    // force the srcType to unsigned if GT_UNSIGNED flag is set
    if (treeNode->gtFlags & GTF_UNSIGNED)
    {
        srcType = genUnsignedType(srcType);
    }

    // We should never see a srcType whose size is neither EA_4BYTE or EA_8BYTE
    // For conversions from small types (byte/sbyte/int16/uint16) to float/double,
    // we expect the front-end or lowering phase to have generated two levels of cast.
    //
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

    getEmitter()->emitIns_R_R(ins, emitTypeSize(dstType), treeNode->gtRegNum, op1->gtRegNum, cvtOption);

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
void CodeGen::genFloatToIntCast(GenTreePtr treeNode)
{
    // we don't expect to see overflow detecting float/double --> int type conversions here
    // as they should have been converted into helper calls by front-end.
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidIntReg(targetReg)); // Must be a valid int reg.

    GenTreePtr op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());               // Cannot be contained
    assert(genIsValidFloatReg(op1->gtRegNum)); // Must be a valid float reg.

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

    getEmitter()->emitIns_R_R(ins, dstSize, treeNode->gtRegNum, op1->gtRegNum, cvtOption);

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
// TODO-ARM64-CQ - mark the operand as contained if known to be in
// memory (e.g. field or an array element).
//
void CodeGen::genCkfinite(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_CKFINITE);

    GenTreePtr op1         = treeNode->gtOp.gtOp1;
    var_types  targetType  = treeNode->TypeGet();
    int        expMask     = (targetType == TYP_FLOAT) ? 0x7F8 : 0x7FF; // Bit mask to extract exponent.
    int        shiftAmount = targetType == TYP_FLOAT ? 20 : 52;

    emitter* emit = getEmitter();

    // Extract exponent into a register.
    regNumber intReg = genRegNumFromMask(treeNode->gtRsvdRegs);
    regNumber fpReg  = genConsumeReg(op1);
    assert(intReg != REG_NA);

    emit->emitIns_R_R(ins_Copy(targetType), emitTypeSize(treeNode), intReg, fpReg);
    emit->emitIns_R_R_I(INS_lsr, emitTypeSize(targetType), intReg, intReg, shiftAmount);

    // Mask of exponent with all 1's and check if the exponent is all 1's
    emit->emitIns_R_R_I(INS_and, EA_4BYTE, intReg, intReg, expMask);
    emit->emitIns_R_I(INS_cmp, EA_4BYTE, intReg, expMask);

    // If exponent is all 1's, throw ArithmeticException
    emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
    genJumpToThrowHlpBlk(jmpEqual, SCK_ARITH_EXCPN);

    // if it is a finite value copy it to targetReg
    if (treeNode->gtRegNum != fpReg)
    {
        emit->emitIns_R_R(ins_Copy(targetType), emitTypeSize(treeNode), treeNode->gtRegNum, fpReg);
    }
    genProduceReg(treeNode);
}

int CodeGenInterface::genSPtoFPdelta()
{
    int delta;

    // We place the saved frame pointer immediately above the outgoing argument space.
    delta = (int)compiler->lvaOutgoingArgSpaceSize;

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

int CodeGenInterface::genTotalFrameSize()
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

int CodeGenInterface::genCallerSPtoFPdelta()
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

int CodeGenInterface::genCallerSPtoInitialSPdelta()
{
    int callerSPtoSPdelta = 0;

    callerSPtoSPdelta -= genTotalFrameSize();

    assert(callerSPtoSPdelta <= 0);
    return callerSPtoSPdelta;
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
void CodeGen::genIntrinsic(GenTreePtr treeNode)
{
    // Both operand and its result must be of the same floating point type.
    GenTreePtr srcNode = treeNode->gtOp.gtOp1;
    assert(varTypeIsFloating(srcNode));
    assert(srcNode->TypeGet() == treeNode->TypeGet());

    // Right now only Abs/Round/Sqrt are treated as math intrinsics.
    //
    switch (treeNode->gtIntrinsic.gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Abs:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_fabs, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        case CORINFO_INTRINSIC_Round:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_frintn, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        case CORINFO_INTRINSIC_Sqrt:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_fsqrt, emitTypeSize(treeNode), treeNode, srcNode);
            break;

        default:
            assert(!"genIntrinsic: Unsupported intrinsic");
            unreached();
    }

    genProduceReg(treeNode);
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
void CodeGen::genPutArgStk(GenTreePtr treeNode)
{
    assert(treeNode->OperGet() == GT_PUTARG_STK);
    var_types  targetType = treeNode->TypeGet();
    GenTreePtr source     = treeNode->gtOp.gtOp1;
    emitter*   emit       = getEmitter();

    // This is the varNum for our store operations,
    // typically this is the varNum for the Outgoing arg space
    // When we are generating a tail call it will be the varNum for arg0
    unsigned varNumOut;
    unsigned argOffsetMax; // Records the maximum size of this area for assert checks

    // This is the varNum for our load operations,
    // only used when we have a multireg struct with a LclVar source
    unsigned varNumInp = BAD_VAR_NUM;

    // Get argument offset to use with 'varNumOut'
    // Here we cross check that argument offset hasn't changed from lowering to codegen since
    // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
    unsigned argOffsetOut = treeNode->AsPutArgStk()->gtSlotNum * TARGET_POINTER_SIZE;

#ifdef DEBUG
    fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(treeNode->AsPutArgStk()->gtCall, treeNode);
    assert(curArgTabEntry);
    assert(argOffsetOut == (curArgTabEntry->slotNum * TARGET_POINTER_SIZE));
#endif // DEBUG

#if FEATURE_FASTTAILCALL
    bool putInIncomingArgArea = treeNode->AsPutArgStk()->putInIncomingArgArea;
#else
    const bool putInIncomingArgArea = false;
#endif
    // Whether to setup stk arg in incoming or out-going arg area?
    // Fast tail calls implemented as epilog+jmp = stk arg is setup in incoming arg area.
    // All other calls - stk arg is setup in out-going arg area.
    if (putInIncomingArgArea)
    {
        varNumOut    = getFirstArgWithStackSlot();
        argOffsetMax = compiler->compArgSize;
#if FEATURE_FASTTAILCALL
        // This must be a fast tail call.
        assert(treeNode->AsPutArgStk()->gtCall->AsCall()->IsFastTailCall());

        // Since it is a fast tail call, the existence of first incoming arg is guaranteed
        // because fast tail call requires that in-coming arg area of caller is >= out-going
        // arg area required for tail call.
        LclVarDsc* varDsc = &(compiler->lvaTable[varNumOut]);
        assert(varDsc != nullptr);
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        varNumOut    = compiler->lvaOutgoingArgSpaceVar;
        argOffsetMax = compiler->lvaOutgoingArgSpaceSize;
    }
    bool isStruct = (targetType == TYP_STRUCT) || (source->OperGet() == GT_LIST);

    if (!isStruct) // a normal non-Struct argument
    {
        instruction storeIns  = ins_Store(targetType);
        emitAttr    storeAttr = emitTypeSize(targetType);

        // If it is contained then source must be the integer constant zero
        if (source->isContained())
        {
            assert(source->OperGet() == GT_CNS_INT);
            assert(source->AsIntConCommon()->IconValue() == 0);
            emit->emitIns_S_R(storeIns, storeAttr, REG_ZR, varNumOut, argOffsetOut);
        }
        else
        {
            genConsumeReg(source);
            emit->emitIns_S_R(storeIns, storeAttr, source->gtRegNum, varNumOut, argOffsetOut);
        }
        argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
        assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
    }
    else // We have some kind of a struct argument
    {
        assert(source->isContained()); // We expect that this node was marked as contained in LowerArm64

        if (source->OperGet() == GT_LIST)
        {
            // Deal with the multi register passed struct args.
            GenTreeArgList* argListPtr = source->AsArgList();

            // Evaluate each of the GT_LIST items into their register
            // and store their register into the outgoing argument area
            for (; argListPtr != nullptr; argListPtr = argListPtr->Rest())
            {
                GenTreePtr nextArgNode = argListPtr->gtOp.gtOp1;
                genConsumeReg(nextArgNode);

                regNumber reg  = nextArgNode->gtRegNum;
                var_types type = nextArgNode->TypeGet();
                emitAttr  attr = emitTypeSize(type);

                // Emit store instructions to store the registers produced by the GT_LIST into the outgoing argument
                // area
                emit->emitIns_S_R(ins_Store(type), attr, reg, varNumOut, argOffsetOut);
                argOffsetOut += EA_SIZE_IN_BYTES(attr);
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            }
        }
        else // We must have a GT_OBJ or a GT_LCL_VAR
        {
            noway_assert((source->OperGet() == GT_LCL_VAR) || (source->OperGet() == GT_OBJ));

            var_types targetType = source->TypeGet();
            noway_assert(varTypeIsStruct(targetType));

            // We will copy this struct to the stack, possibly using a ldp instruction
            // Setup loReg and hiReg from the internal registers that we reserved in lower.
            //
            regNumber loReg   = REG_NA;
            regNumber hiReg   = REG_NA;
            regNumber addrReg = REG_NA;

            // In lowerArm64/TreeNodeInfoInitPutArgStk we have reserved two internal integer registers
            genGetRegPairFromMask(treeNode->gtRsvdRegs, &loReg, &hiReg);

            GenTreeLclVarCommon* varNode  = nullptr;
            GenTreePtr           addrNode = nullptr;

            if (source->OperGet() == GT_LCL_VAR)
            {
                varNode = source->AsLclVarCommon();
            }
            else // we must have a GT_OBJ
            {
                assert(source->OperGet() == GT_OBJ);

                addrNode = source->gtOp.gtOp1;

                // addrNode can either be a GT_LCL_VAR_ADDR or an address expression
                //
                if (addrNode->OperGet() == GT_LCL_VAR_ADDR)
                {
                    // We have a GT_OBJ(GT_LCL_VAR_ADDR)
                    //
                    // We will treat this case the same as above
                    // (i.e if we just had this GT_LCL_VAR directly as the source)
                    // so update 'source' to point this GT_LCL_VAR_ADDR node
                    // and continue to the codegen for the LCL_VAR node below
                    //
                    varNode  = addrNode->AsLclVarCommon();
                    addrNode = nullptr;
                }
            }

            // Either varNode or addrNOde must have been setup above,
            // the xor ensures that only one of the two is setup, not both
            assert((varNode != nullptr) ^ (addrNode != nullptr));

            BYTE     gcPtrs[MAX_ARG_REG_COUNT] = {};         // TYPE_GC_NONE = 0
            BYTE*    structGcLayout            = &gcPtrs[0]; // The GC layout for the struct
            unsigned gcPtrCount;                             // The count of GC pointers in the struct
            int      structSize;
            bool     isHfa;

            // Setup the structSize, isHFa, and gcPtrCount
            if (varNode != nullptr)
            {
                varNumInp = varNode->gtLclNum;
                assert(varNumInp < compiler->lvaCount);
                LclVarDsc* varDsc = &compiler->lvaTable[varNumInp];

                assert(varDsc->lvType == TYP_STRUCT);
                assert(varDsc->lvOnFrame);   // This struct also must live in the stack frame
                assert(!varDsc->lvRegister); // And it can't live in a register (SIMD)

                structSize = varDsc->lvSize(); // This yields the roundUp size, but that is fine
                                               // as that is how much stack is allocated for this LclVar
                isHfa          = varDsc->lvIsHfa();
                gcPtrCount     = varDsc->lvStructGcCount;
                structGcLayout = varDsc->lvGcLayout;
            }
            else // addrNode is used
            {
                assert(addrNode != nullptr);

                // Generate code to load the address that we need into a register
                genConsumeAddress(addrNode);
                addrReg = addrNode->gtRegNum;

                CORINFO_CLASS_HANDLE objClass = source->gtObj.gtClass;

                structSize = compiler->info.compCompHnd->getClassSize(objClass);
                isHfa      = compiler->IsHfa(objClass);
                gcPtrCount = compiler->info.compCompHnd->getClassGClayout(objClass, &gcPtrs[0]);
            }

            bool hasGCpointers = (gcPtrCount > 0); // true if there are any GC pointers in the struct

            // If we have an HFA we can't have any GC pointers,
            // if not then the max size for the the struct is 16 bytes
            if (isHfa)
            {
                noway_assert(gcPtrCount == 0);
            }
            else
            {
                noway_assert(structSize <= 2 * TARGET_POINTER_SIZE);
            }

            noway_assert(structSize <= MAX_PASS_MULTIREG_BYTES);

            // For a 16-byte structSize with GC pointers we will use two ldr and two str instructions
            //             ldr     x2, [x0]
            //             ldr     x3, [x0, #8]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //
            // For a 16-byte structSize with no GC pointers we will use a ldp and two str instructions
            //             ldp     x2, x3, [x0]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //
            // For a 32-byte structSize with no GC pointers we will use two ldp and four str instructions
            //             ldp     x2, x3, [x0]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //             ldp     x2, x3, [x0]
            //             str     x2, [sp, #32]
            //             str     x3, [sp, #40]
            //
            // Note that when loading from a varNode we currently can't use the ldp instruction
            // TODO-ARM64-CQ: Implement support for using a ldp instruction with a varNum (see emitIns_R_S)
            //

            int      remainingSize = structSize;
            unsigned structOffset  = 0;
            unsigned nextIndex     = 0;

            while (remainingSize >= 2 * TARGET_POINTER_SIZE)
            {
                var_types type0 = compiler->getJitGCType(gcPtrs[nextIndex + 0]);
                var_types type1 = compiler->getJitGCType(gcPtrs[nextIndex + 1]);

                if (hasGCpointers)
                {
                    // We have GC pointers, so use two ldr instructions
                    //
                    // We must do it this way because we can't currently pass or track
                    // two different emitAttr values for a ldp instruction.

                    // Make sure that the first load instruction does not overwrite the addrReg.
                    //
                    if (loReg != addrReg)
                    {
                        if (varNode != nullptr)
                        {
                            // Load from our varNumImp source
                            emit->emitIns_R_S(ins_Load(type0), emitTypeSize(type0), loReg, varNumInp, 0);
                            emit->emitIns_R_S(ins_Load(type1), emitTypeSize(type1), hiReg, varNumInp,
                                              TARGET_POINTER_SIZE);
                        }
                        else
                        {
                            // Load from our address expression source
                            emit->emitIns_R_R_I(ins_Load(type0), emitTypeSize(type0), loReg, addrReg, structOffset);
                            emit->emitIns_R_R_I(ins_Load(type1), emitTypeSize(type1), hiReg, addrReg,
                                                structOffset + TARGET_POINTER_SIZE);
                        }
                    }
                    else // loReg == addrReg
                    {
                        assert(varNode == nullptr); // because addrReg is REG_NA when varNode is non-null
                        assert(hiReg != addrReg);
                        // Load from our address expression source
                        emit->emitIns_R_R_I(ins_Load(type1), emitTypeSize(type1), hiReg, addrReg,
                                            structOffset + TARGET_POINTER_SIZE);
                        emit->emitIns_R_R_I(ins_Load(type0), emitTypeSize(type0), loReg, addrReg, structOffset);
                    }
                }
                else // our struct has no GC pointers
                {
                    if (varNode != nullptr)
                    {
                        // Load from our varNumImp source, currently we can't use a ldp instruction to do this
                        emit->emitIns_R_S(ins_Load(type0), emitTypeSize(type0), loReg, varNumInp, 0);
                        emit->emitIns_R_S(ins_Load(type1), emitTypeSize(type1), hiReg, varNumInp, TARGET_POINTER_SIZE);
                    }
                    else
                    {
                        // Use a ldp instruction

                        // Load from our address expression source
                        emit->emitIns_R_R_R_I(INS_ldp, EA_PTRSIZE, loReg, hiReg, addrReg, structOffset);
                    }
                }

                // Emit two store instructions to store the two registers into the outgoing argument area
                emit->emitIns_S_R(ins_Store(type0), emitTypeSize(type0), loReg, varNumOut, argOffsetOut);
                emit->emitIns_S_R(ins_Store(type1), emitTypeSize(type1), hiReg, varNumOut,
                                  argOffsetOut + TARGET_POINTER_SIZE);
                argOffsetOut += (2 * TARGET_POINTER_SIZE); // We stored 16-bytes of the struct
                assert(argOffsetOut <= argOffsetMax);      // We can't write beyound the outgoing area area

                remainingSize -= (2 * TARGET_POINTER_SIZE); // We loaded 16-bytes of the struct
                structOffset += (2 * TARGET_POINTER_SIZE);
                nextIndex += 2;
            }

            // For a 12-byte structSize we will we will generate two load instructions
            //             ldr     x2, [x0]
            //             ldr     w3, [x0, #8]
            //             str     x2, [sp, #16]
            //             str     w3, [sp, #24]
            //
            // When the first instruction has a loReg that is the same register as the addrReg,
            //  we set deferLoad to true and issue the intructions in the reverse order
            //             ldr     x3, [x2, #8]
            //             ldr     x2, [x2]
            //             str     x2, [sp, #16]
            //             str     x3, [sp, #24]
            //

            var_types nextType = compiler->getJitGCType(gcPtrs[nextIndex]);
            emitAttr  nextAttr = emitTypeSize(nextType);
            regNumber curReg   = loReg;

            bool      deferLoad   = false;
            var_types deferType   = TYP_UNKNOWN;
            emitAttr  deferAttr   = EA_PTRSIZE;
            int       deferOffset = 0;

            while (remainingSize > 0)
            {
                if (remainingSize >= TARGET_POINTER_SIZE)
                {
                    remainingSize -= TARGET_POINTER_SIZE;

                    if ((curReg == addrReg) && (remainingSize != 0))
                    {
                        deferLoad   = true;
                        deferType   = nextType;
                        deferAttr   = emitTypeSize(nextType);
                        deferOffset = structOffset;
                    }
                    else // the typical case
                    {
                        if (varNode != nullptr)
                        {
                            // Load from our varNumImp source
                            emit->emitIns_R_S(ins_Load(nextType), nextAttr, curReg, varNumInp, structOffset);
                        }
                        else
                        {
                            // Load from our address expression source
                            emit->emitIns_R_R_I(ins_Load(nextType), nextAttr, curReg, addrReg, structOffset);
                        }
                        // Emit a store instruction to store the register into the outgoing argument area
                        emit->emitIns_S_R(ins_Store(nextType), nextAttr, curReg, varNumOut, argOffsetOut);
                        argOffsetOut += EA_SIZE_IN_BYTES(nextAttr);
                        assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
                    }
                    curReg = hiReg;
                    structOffset += TARGET_POINTER_SIZE;
                    nextIndex++;
                    nextType = compiler->getJitGCType(gcPtrs[nextIndex]);
                    nextAttr = emitTypeSize(nextType);
                }
                else // (remainingSize < TARGET_POINTER_SIZE)
                {
                    int loadSize  = remainingSize;
                    remainingSize = 0;

                    // We should never have to do a non-pointer sized load when we have a LclVar source
                    assert(varNode == nullptr);

                    // the left over size is smaller than a pointer and thus can never be a GC type
                    assert(varTypeIsGC(nextType) == false);

                    var_types loadType = TYP_UINT;
                    if (loadSize == 1)
                    {
                        loadType = TYP_UBYTE;
                    }
                    else if (loadSize == 2)
                    {
                        loadType = TYP_USHORT;
                    }
                    else
                    {
                        // Need to handle additional loadSize cases here
                        noway_assert(loadSize == 4);
                    }

                    instruction loadIns  = ins_Load(loadType);
                    emitAttr    loadAttr = emitAttr(loadSize);

                    // When deferLoad is false, curReg can be the same as addrReg
                    // because the last instruction is allowed to overwrite addrReg.
                    //
                    noway_assert(!deferLoad || (curReg != addrReg));

                    emit->emitIns_R_R_I(loadIns, loadAttr, curReg, addrReg, structOffset);

                    // Emit a store instruction to store the register into the outgoing argument area
                    emit->emitIns_S_R(ins_Store(loadType), loadAttr, curReg, varNumOut, argOffsetOut);
                    argOffsetOut += EA_SIZE_IN_BYTES(loadAttr);
                    assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
                }
            }

            if (deferLoad)
            {
                // We should never have to do a deferred load when we have a LclVar source
                assert(varNode == nullptr);

                curReg = addrReg;

                // Load from our address expression source
                emit->emitIns_R_R_I(ins_Load(deferType), deferAttr, curReg, addrReg, deferOffset);

                // Emit a store instruction to store the register into the outgoing argument area
                emit->emitIns_S_R(ins_Store(nextType), nextAttr, curReg, varNumOut, argOffsetOut);
                argOffsetOut += EA_SIZE_IN_BYTES(nextAttr);
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            }
        }
    }
}

/*****************************************************************************
 *
 *  Create and record GC Info for the function.
 */
void CodeGen::genCreateAndStoreGCInfo(unsigned codeSize,
                                      unsigned prologSize,
                                      unsigned epilogSize DEBUGARG(void* codePtr))
{
    genCreateAndStoreGCInfoX64(codeSize, prologSize DEBUGARG(codePtr));
}

void CodeGen::genCreateAndStoreGCInfoX64(unsigned codeSize, unsigned prologSize DEBUGARG(void* codePtr))
{
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) AllowZeroAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder != nullptr);

    // Follow the code pattern of the x86 gc info encoder (genCreateAndStoreGCInfoJIT32).
    gcInfo.gcInfoBlockHdrSave(gcInfoEncoder, codeSize, prologSize);

    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS);

    // Now we've requested all the slots we'll need; "finalize" these (make more compact data structures for them).
    gcInfoEncoder->FinalizeSlotIds();

    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK);

#if defined(DEBUGGING_SUPPORT)
    if (compiler->opts.compDbgEnC)
    {
        // what we have to preserve is called the "frame header" (see comments in VM\eetwain.cpp)
        // which is:
        //  -return address
        //  -saved off RBP
        //  -saved 'this' pointer and bool for synchronized methods

        // 4 slots for RBP + return address + RSI + RDI
        int preservedAreaSize = 4 * REGSIZE_BYTES;

        if (compiler->info.compFlags & CORINFO_FLG_SYNCH)
        {
            if (!(compiler->info.compFlags & CORINFO_FLG_STATIC))
                preservedAreaSize += REGSIZE_BYTES;

            preservedAreaSize += 1; // bool for synchronized methods
        }

        // Used to signal both that the method is compiled for EnC, and also the size of the block at the top of the
        // frame
        gcInfoEncoder->SetSizeOfEditAndContinuePreservedArea(preservedAreaSize);
    }
#endif

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
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
        getEmitter()->emitIns_R_AI(INS_adrp, EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
        getEmitter()->emitIns_R_R(INS_ldr, EA_PTRSIZE, callTarget, callTarget);
        callType = emitter::EC_INDIR_R;
    }

    getEmitter()->emitIns_Call(callType, compiler->eeFindHelper(helper), INDEBUG_LDISASM_COMMA(nullptr) addr, argSize,
                               retSize, EA_UNKNOWN, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, BAD_IL_OFFSET, /* IL offset */
                               callTarget,                             /* ireg */
                               REG_NA, 0, 0,                           /* xreg, xmul, disp */
                               false,                                  /* isJump */
                               emitter::emitNoGChelper(helper));

    regMaskTP killMask = compiler->compHelperCallKillSet((CorInfoHelpFunc)helper);
    regTracker.rsTrashRegSet(killMask);
    regTracker.rsTrashRegsForGCInterruptability();
}

/*****************************************************************************/
#ifdef DEBUGGING_SUPPORT
/*****************************************************************************
 *                          genSetScopeInfo
 *
 * Called for every scope info piece to record by the main genSetScopeInfo()
 */

// TODO-Cleanup: move to CodeGenCommon.cpp
void CodeGen::genSetScopeInfo(unsigned            which,
                              UNATIVE_OFFSET      startOffs,
                              UNATIVE_OFFSET      length,
                              unsigned            varNum,
                              unsigned            LVnum,
                              bool                avail,
                              Compiler::siVarLoc& varLoc)
{
    /* We need to do some mapping while reporting back these variables */

    unsigned ilVarNum = compiler->compMap2ILvarNum(varNum);
    noway_assert((int)ilVarNum != ICorDebugInfo::UNKNOWN_ILNUM);

    VarName name = nullptr;

#ifdef DEBUG

    for (unsigned scopeNum = 0; scopeNum < compiler->info.compVarScopesCount; scopeNum++)
    {
        if (LVnum == compiler->info.compVarScopes[scopeNum].vsdLVnum)
        {
            name = compiler->info.compVarScopes[scopeNum].vsdName;
        }
    }

    // Hang on to this compiler->info.

    TrnslLocalVarInfo& tlvi = genTrnslLocalVarInfo[which];

    tlvi.tlviVarNum    = ilVarNum;
    tlvi.tlviLVnum     = LVnum;
    tlvi.tlviName      = name;
    tlvi.tlviStartPC   = startOffs;
    tlvi.tlviLength    = length;
    tlvi.tlviAvailable = avail;
    tlvi.tlviVarLoc    = varLoc;

#endif // DEBUG

    compiler->eeSetLVinfo(which, startOffs, length, ilVarNum, LVnum, name, avail, varLoc);
}
#endif // DEBUGGING_SUPPORT

/*****************************************************************************
 * Unit testing of the ARM64 emitter: generate a bunch of instructions into the prolog
 * (it's as good a place as any), then use COMPlus_JitLateDisasm=* to see if the late
 * disassembler thinks the instructions as the same as we do.
 */

// Uncomment "#define ALL_ARM64_EMITTER_UNIT_TESTS" to run all the unit tests here.
// After adding a unit test, and verifying it works, put it under this #ifdef, so we don't see it run every time.
//#define ALL_ARM64_EMITTER_UNIT_TESTS

#if defined(DEBUG)
void CodeGen::genArm64EmitterUnitTests()
{
    if (!verbose)
    {
        return;
    }

    if (!compiler->opts.altJit)
    {
        // No point doing this in a "real" JIT.
        return;
    }

    // Mark the "fake" instructions in the output.
    printf("*************** In genArm64EmitterUnitTests()\n");

    emitter* theEmitter = getEmitter();

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    // We use this:
    //      genDefineTempLabel(genCreateTempLabel());
    // to create artificial labels to help separate groups of tests.

    //
    // Loads/Stores basic general register
    //

    genDefineTempLabel(genCreateTempLabel());

    // ldr/str Xt, [reg]
    theEmitter->emitIns_R_R(INS_ldr, EA_8BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrh, EA_2BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_str, EA_8BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strh, EA_2BYTE, REG_R8, REG_R9);

    // ldr/str Wt, [reg]
    theEmitter->emitIns_R_R(INS_ldr, EA_4BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_ldrh, EA_2BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_str, EA_4BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strb, EA_1BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_strh, EA_2BYTE, REG_R8, REG_R9);

    theEmitter->emitIns_R_R(INS_ldrsb, EA_4BYTE, REG_R8, REG_R9); // target Wt
    theEmitter->emitIns_R_R(INS_ldrsh, EA_4BYTE, REG_R8, REG_R9); // target Wt
    theEmitter->emitIns_R_R(INS_ldrsb, EA_8BYTE, REG_R8, REG_R9); // target Xt
    theEmitter->emitIns_R_R(INS_ldrsh, EA_8BYTE, REG_R8, REG_R9); // target Xt
    theEmitter->emitIns_R_R(INS_ldrsw, EA_8BYTE, REG_R8, REG_R9); // target Xt

    theEmitter->emitIns_R_R_I(INS_ldurb, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldurh, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sturb, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sturh, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursb, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursb, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursh, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursh, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_stur, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_stur, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldursw, EA_8BYTE, REG_R8, REG_R9, 1);

    // SP and ZR tests
    theEmitter->emitIns_R_R_I(INS_ldur, EA_8BYTE, REG_R8, REG_SP, 1);
    theEmitter->emitIns_R_R_I(INS_ldurb, EA_8BYTE, REG_ZR, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldurh, EA_8BYTE, REG_ZR, REG_SP, 1);

    // scaled
    theEmitter->emitIns_R_R_I(INS_ldrb, EA_1BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldrh, EA_2BYTE, REG_R8, REG_R9, 2);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_R8, REG_R9, 4);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_R8, REG_R9, 8);

    // pre-/post-indexed (unscaled)
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_R8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_R8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_PRE_INDEX);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // Compares
    //

    genDefineTempLabel(genCreateTempLabel());

    // cmp reg, reg
    theEmitter->emitIns_R_R(INS_cmp, EA_8BYTE, REG_R8, REG_R9);
    theEmitter->emitIns_R_R(INS_cmn, EA_8BYTE, REG_R8, REG_R9);

    // cmp reg, imm
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 0);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 4095);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 1 << 12);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 4095 << 12);

    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 0);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 4095);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 1 << 12);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 4095 << 12);

    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, -1);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, -0xfff);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_I(INS_cmp, EA_8BYTE, REG_R8, 0xffffffffff800000LL);

    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, -1);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, -0xfff);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_I(INS_cmn, EA_8BYTE, REG_R8, 0xffffffffff800000LL);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    // R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_R1, REG_R12);
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_R2, REG_R13);
    theEmitter->emitIns_R_R(INS_rbit, EA_8BYTE, REG_R3, REG_R14);
    theEmitter->emitIns_R_R(INS_rev, EA_8BYTE, REG_R4, REG_R15);
    theEmitter->emitIns_R_R(INS_rev16, EA_8BYTE, REG_R5, REG_R0);
    theEmitter->emitIns_R_R(INS_rev32, EA_8BYTE, REG_R6, REG_R1);

    theEmitter->emitIns_R_R(INS_cls, EA_4BYTE, REG_R7, REG_R2);
    theEmitter->emitIns_R_R(INS_clz, EA_4BYTE, REG_R8, REG_R3);
    theEmitter->emitIns_R_R(INS_rbit, EA_4BYTE, REG_R9, REG_R4);
    theEmitter->emitIns_R_R(INS_rev, EA_4BYTE, REG_R10, REG_R5);
    theEmitter->emitIns_R_R(INS_rev16, EA_4BYTE, REG_R11, REG_R6);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_I
    //

    genDefineTempLabel(genCreateTempLabel());

    // mov reg, imm(i16,hw)
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0000000000001234);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0000000043210000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0000567800000000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x8765000000000000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xFFFFFFFFFFFF1234);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xFFFFFFFF4321FFFF);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xFFFF5678FFFFFFFF);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x8765FFFFFFFFFFFF);

    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x00001234);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x87650000);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0xFFFF1234);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x4567FFFF);

    // mov reg, imm(N,r,s)
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x00FFFFF000000000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x6666666666666666);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_SP, 0x7FFF00007FFF0000);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x5555555555555555);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0xE003E003E003E003);
    theEmitter->emitIns_R_I(INS_mov, EA_8BYTE, REG_R8, 0x0707070707070707);

    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x00FFFFF0);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x66666666);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x03FFC000);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x55555555);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0xE003E003);
    theEmitter->emitIns_R_I(INS_mov, EA_4BYTE, REG_R8, 0x07070707);

    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0xE003E003E003E003);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x00FFFFF000000000);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x6666666666666666);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x0707070707070707);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x7FFF00007FFF0000);
    theEmitter->emitIns_R_I(INS_tst, EA_8BYTE, REG_R8, 0x5555555555555555);

    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0xE003E003);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x00FFFFF0);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x66666666);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x07070707);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0xFFF00000);
    theEmitter->emitIns_R_I(INS_tst, EA_4BYTE, REG_R8, 0x55555555);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    // tst reg, reg
    theEmitter->emitIns_R_R(INS_tst, EA_8BYTE, REG_R7, REG_R10);

    // mov reg, reg
    theEmitter->emitIns_R_R(INS_mov, EA_8BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_mov, EA_8BYTE, REG_R8, REG_SP);
    theEmitter->emitIns_R_R(INS_mov, EA_8BYTE, REG_SP, REG_R9);

    theEmitter->emitIns_R_R(INS_mvn, EA_8BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_R4, REG_R12);
    theEmitter->emitIns_R_R(INS_negs, EA_8BYTE, REG_R3, REG_R13);

    theEmitter->emitIns_R_R(INS_mov, EA_4BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_mvn, EA_4BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_neg, EA_4BYTE, REG_R4, REG_R12);
    theEmitter->emitIns_R_R(INS_negs, EA_4BYTE, REG_R3, REG_R13);

    theEmitter->emitIns_R_R(INS_sxtb, EA_8BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_sxth, EA_8BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_sxtw, EA_8BYTE, REG_R4, REG_R12);
    theEmitter->emitIns_R_R(INS_uxtb, EA_8BYTE, REG_R3, REG_R13); // map to Wt
    theEmitter->emitIns_R_R(INS_uxth, EA_8BYTE, REG_R2, REG_R14); // map to Wt

    theEmitter->emitIns_R_R(INS_sxtb, EA_4BYTE, REG_R7, REG_R10);
    theEmitter->emitIns_R_R(INS_sxth, EA_4BYTE, REG_R5, REG_R11);
    theEmitter->emitIns_R_R(INS_uxtb, EA_4BYTE, REG_R3, REG_R13);
    theEmitter->emitIns_R_R(INS_uxth, EA_4BYTE, REG_R2, REG_R14);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_I_I
    //

    genDefineTempLabel(genCreateTempLabel());

    // mov reg, imm(i16,hw)
    theEmitter->emitIns_R_I_I(INS_mov, EA_8BYTE, REG_R8, 0x1234, 0, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_mov, EA_8BYTE, REG_R8, 0x4321, 16, INS_OPTS_LSL);

    theEmitter->emitIns_R_I_I(INS_movk, EA_8BYTE, REG_R8, 0x4321, 16, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movn, EA_8BYTE, REG_R8, 0x5678, 32, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movz, EA_8BYTE, REG_R8, 0x8765, 48, INS_OPTS_LSL);

    theEmitter->emitIns_R_I_I(INS_movk, EA_4BYTE, REG_R8, 0x4321, 16, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movn, EA_4BYTE, REG_R8, 0x5678, 16, INS_OPTS_LSL);
    theEmitter->emitIns_R_I_I(INS_movz, EA_4BYTE, REG_R8, 0x8765, 16, INS_OPTS_LSL);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_I
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_I(INS_lsl, EA_8BYTE, REG_R0, REG_R0, 1);
    theEmitter->emitIns_R_R_I(INS_lsl, EA_4BYTE, REG_R9, REG_R3, 18);
    theEmitter->emitIns_R_R_I(INS_lsr, EA_8BYTE, REG_R7, REG_R0, 37);
    theEmitter->emitIns_R_R_I(INS_lsr, EA_4BYTE, REG_R0, REG_R1, 2);
    theEmitter->emitIns_R_R_I(INS_asr, EA_8BYTE, REG_R2, REG_R3, 53);
    theEmitter->emitIns_R_R_I(INS_asr, EA_4BYTE, REG_R9, REG_R3, 18);

    theEmitter->emitIns_R_R_I(INS_and, EA_8BYTE, REG_R2, REG_R3, 0x5555555555555555);
    theEmitter->emitIns_R_R_I(INS_ands, EA_8BYTE, REG_R1, REG_R5, 0x6666666666666666);
    theEmitter->emitIns_R_R_I(INS_eor, EA_8BYTE, REG_R8, REG_R9, 0x0707070707070707);
    theEmitter->emitIns_R_R_I(INS_orr, EA_8BYTE, REG_SP, REG_R3, 0xFFFC000000000000);
    theEmitter->emitIns_R_R_I(INS_ands, EA_4BYTE, REG_R8, REG_R9, 0xE003E003);

    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 31);
    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 32);
    theEmitter->emitIns_R_R_I(INS_ror, EA_8BYTE, REG_R8, REG_R9, 63);

    theEmitter->emitIns_R_R_I(INS_ror, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ror, EA_4BYTE, REG_R8, REG_R9, 31);

    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_add, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_sub, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_adds, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_adds, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_subs, EA_8BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0); // == mov
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, -1);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, -0xfff);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0x1000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xfff000);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xfffffffffffff000LL);
    theEmitter->emitIns_R_R_I(INS_subs, EA_4BYTE, REG_R8, REG_R9, 0xffffffffff800000LL);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_I cmp/txt
    //

    // cmp
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 0);

    // CMP (shifted register)
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 31, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 32, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 33, INS_OPTS_ASR);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 21, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 22, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 23, INS_OPTS_ASR);

    // TST (shifted register)
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 31, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 32, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 33, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_8BYTE, REG_R8, REG_R9, 34, INS_OPTS_ROR);

    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 21, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 22, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 23, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_I(INS_tst, EA_4BYTE, REG_R8, REG_R9, 24, INS_OPTS_ROR);

    // CMP (extended register)
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTW); // "cmp x8, x9, UXTW"; msdis
                                                                                    // disassembles this "cmp x8,x9",
                                                                                    // which looks like an msdis issue.
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTX);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTX);

    // CMP 64-bit (extended register) and left shift
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 2, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 3, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 4, INS_OPTS_UXTX);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 1, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 2, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 3, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_8BYTE, REG_R8, REG_R9, 4, INS_OPTS_SXTX);

    // CMP 32-bit (extended register) and left shift
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 2, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 4, INS_OPTS_UXTW);

    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 2, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_I(INS_cmp, EA_4BYTE, REG_R8, REG_R9, 4, INS_OPTS_SXTW);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_lsl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsr, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asr, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ror, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adc, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adcs, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbc, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbcs, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_udiv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sdiv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mneg, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smull, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smnegl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smulh, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umull, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umnegl, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umulh, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lslv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsrv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asrv, EA_8BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_rorv, EA_8BYTE, REG_R8, REG_R9, REG_R10);

    theEmitter->emitIns_R_R_R(INS_lsl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsr, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asr, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_ror, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adc, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_adcs, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbc, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sbcs, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_udiv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_sdiv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mul, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_mneg, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smull, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smnegl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_smulh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umull, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umnegl, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_umulh, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lslv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_lsrv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_asrv, EA_4BYTE, REG_R8, REG_R9, REG_R10);
    theEmitter->emitIns_R_R_R(INS_rorv, EA_4BYTE, REG_R8, REG_R9, REG_R10);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_I_I
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_I_I(INS_sbfm, EA_8BYTE, REG_R2, REG_R3, 4, 39);
    theEmitter->emitIns_R_R_I_I(INS_bfm, EA_8BYTE, REG_R1, REG_R5, 20, 23);
    theEmitter->emitIns_R_R_I_I(INS_ubfm, EA_8BYTE, REG_R8, REG_R9, 36, 7);

    theEmitter->emitIns_R_R_I_I(INS_sbfiz, EA_8BYTE, REG_R2, REG_R3, 7, 37);
    theEmitter->emitIns_R_R_I_I(INS_bfi, EA_8BYTE, REG_R1, REG_R5, 23, 21);
    theEmitter->emitIns_R_R_I_I(INS_ubfiz, EA_8BYTE, REG_R8, REG_R9, 39, 5);

    theEmitter->emitIns_R_R_I_I(INS_sbfx, EA_8BYTE, REG_R2, REG_R3, 10, 24);
    theEmitter->emitIns_R_R_I_I(INS_bfxil, EA_8BYTE, REG_R1, REG_R5, 26, 16);
    theEmitter->emitIns_R_R_I_I(INS_ubfx, EA_8BYTE, REG_R8, REG_R9, 42, 8);

    theEmitter->emitIns_R_R_I_I(INS_sbfm, EA_4BYTE, REG_R2, REG_R3, 4, 19);
    theEmitter->emitIns_R_R_I_I(INS_bfm, EA_4BYTE, REG_R1, REG_R5, 10, 13);
    theEmitter->emitIns_R_R_I_I(INS_ubfm, EA_4BYTE, REG_R8, REG_R9, 16, 7);

    theEmitter->emitIns_R_R_I_I(INS_sbfiz, EA_4BYTE, REG_R2, REG_R3, 5, 17);
    theEmitter->emitIns_R_R_I_I(INS_bfi, EA_4BYTE, REG_R1, REG_R5, 13, 11);
    theEmitter->emitIns_R_R_I_I(INS_ubfiz, EA_4BYTE, REG_R8, REG_R9, 19, 5);

    theEmitter->emitIns_R_R_I_I(INS_sbfx, EA_4BYTE, REG_R2, REG_R3, 3, 14);
    theEmitter->emitIns_R_R_I_I(INS_bfxil, EA_4BYTE, REG_R1, REG_R5, 11, 9);
    theEmitter->emitIns_R_R_I_I(INS_ubfx, EA_4BYTE, REG_R8, REG_R9, 22, 8);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R_I
    //

    genDefineTempLabel(genCreateTempLabel());

    // ADD (extended register)
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTX);

    // ADD (extended register) and left shift
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTX);

    // ADD (shifted register)
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 31, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 32, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_add, EA_8BYTE, REG_R8, REG_R9, REG_R10, 33, INS_OPTS_ASR);

    // EXTR (extract field from register pair)
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 1);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 31);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 32);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 63);

    theEmitter->emitIns_R_R_R_I(INS_extr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 1);
    theEmitter->emitIns_R_R_R_I(INS_extr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 31);

    // SUB (extended register)
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0, INS_OPTS_SXTX);

    // SUB (extended register) and left shift
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTB);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTH);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_SXTX);

    // SUB (shifted register)
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 27, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 28, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_sub, EA_4BYTE, REG_R8, REG_R9, REG_R10, 29, INS_OPTS_ASR);

    // bit operations
    theEmitter->emitIns_R_R_R_I(INS_and, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);

    theEmitter->emitIns_R_R_R_I(INS_and, EA_8BYTE, REG_R8, REG_R9, REG_R10, 1, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_8BYTE, REG_R8, REG_R9, REG_R10, 2, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_8BYTE, REG_R8, REG_R9, REG_R10, 3, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_8BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_ROR);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_8BYTE, REG_R8, REG_R9, REG_R10, 5, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_8BYTE, REG_R8, REG_R9, REG_R10, 6, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_8BYTE, REG_R8, REG_R9, REG_R10, 7, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_8BYTE, REG_R8, REG_R9, REG_R10, 8, INS_OPTS_ROR);

    theEmitter->emitIns_R_R_R_I(INS_and, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);

    theEmitter->emitIns_R_R_R_I(INS_and, EA_4BYTE, REG_R8, REG_R9, REG_R10, 1, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_ands, EA_4BYTE, REG_R8, REG_R9, REG_R10, 2, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eor, EA_4BYTE, REG_R8, REG_R9, REG_R10, 3, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orr, EA_4BYTE, REG_R8, REG_R9, REG_R10, 4, INS_OPTS_ROR);
    theEmitter->emitIns_R_R_R_I(INS_bic, EA_4BYTE, REG_R8, REG_R9, REG_R10, 5, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_I(INS_bics, EA_4BYTE, REG_R8, REG_R9, REG_R10, 6, INS_OPTS_LSR);
    theEmitter->emitIns_R_R_R_I(INS_eon, EA_4BYTE, REG_R8, REG_R9, REG_R10, 7, INS_OPTS_ASR);
    theEmitter->emitIns_R_R_R_I(INS_orn, EA_4BYTE, REG_R8, REG_R9, REG_R10, 8, INS_OPTS_ROR);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R_I  -- load/store pair
    //

    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 8);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 8);

    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 8);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 8);

    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldpsw, EA_4BYTE, REG_R8, REG_R9, REG_R10, 16, INS_OPTS_PRE_INDEX);

    // SP and ZR tests
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_ZR, REG_R1, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_R0, REG_ZR, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_R1, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_R0, REG_ZR, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_ZR, REG_SP, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_ZR, REG_ZR, REG_R8, 16, INS_OPTS_PRE_INDEX);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R_Ext    -- load/store shifted/extend
    //

    genDefineTempLabel(genCreateTempLabel());

    // LDR (register)
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 3);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsw, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsh, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldrsb, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);

    // STR (register)
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_8BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 3);

    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_str, EA_4BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_strh, EA_2BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_strb, EA_1BYTE, REG_R8, REG_SP, REG_R9, INS_OPTS_UXTX);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R_R
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R_R(INS_madd, EA_4BYTE, REG_R0, REG_R12, REG_R27, REG_R10);
    theEmitter->emitIns_R_R_R_R(INS_msub, EA_4BYTE, REG_R1, REG_R13, REG_R28, REG_R11);
    theEmitter->emitIns_R_R_R_R(INS_smaddl, EA_4BYTE, REG_R2, REG_R14, REG_R0, REG_R12);
    theEmitter->emitIns_R_R_R_R(INS_smsubl, EA_4BYTE, REG_R3, REG_R15, REG_R1, REG_R13);
    theEmitter->emitIns_R_R_R_R(INS_umaddl, EA_4BYTE, REG_R4, REG_R19, REG_R2, REG_R14);
    theEmitter->emitIns_R_R_R_R(INS_umsubl, EA_4BYTE, REG_R5, REG_R20, REG_R3, REG_R15);

    theEmitter->emitIns_R_R_R_R(INS_madd, EA_8BYTE, REG_R6, REG_R21, REG_R4, REG_R19);
    theEmitter->emitIns_R_R_R_R(INS_msub, EA_8BYTE, REG_R7, REG_R22, REG_R5, REG_R20);
    theEmitter->emitIns_R_R_R_R(INS_smaddl, EA_8BYTE, REG_R8, REG_R23, REG_R6, REG_R21);
    theEmitter->emitIns_R_R_R_R(INS_smsubl, EA_8BYTE, REG_R9, REG_R24, REG_R7, REG_R22);
    theEmitter->emitIns_R_R_R_R(INS_umaddl, EA_8BYTE, REG_R10, REG_R25, REG_R8, REG_R23);
    theEmitter->emitIns_R_R_R_R(INS_umsubl, EA_8BYTE, REG_R11, REG_R26, REG_R9, REG_R24);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    // R_COND
    //

    // cset reg, cond
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R9, INS_COND_EQ); // eq
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R8, INS_COND_NE); // ne
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R7, INS_COND_HS); // hs
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R6, INS_COND_LO); // lo
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R5, INS_COND_MI); // mi
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R4, INS_COND_PL); // pl
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R3, INS_COND_VS); // vs
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R2, INS_COND_VC); // vc
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R1, INS_COND_HI); // hi
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R0, INS_COND_LS); // ls
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R9, INS_COND_GE); // ge
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R8, INS_COND_LT); // lt
    theEmitter->emitIns_R_COND(INS_cset, EA_8BYTE, REG_R7, INS_COND_GT); // gt
    theEmitter->emitIns_R_COND(INS_cset, EA_4BYTE, REG_R6, INS_COND_LE); // le

    // csetm reg, cond
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R9, INS_COND_EQ); // eq
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R8, INS_COND_NE); // ne
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R7, INS_COND_HS); // hs
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R6, INS_COND_LO); // lo
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R5, INS_COND_MI); // mi
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R4, INS_COND_PL); // pl
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R3, INS_COND_VS); // vs
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R2, INS_COND_VC); // vc
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R1, INS_COND_HI); // hi
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R0, INS_COND_LS); // ls
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R9, INS_COND_GE); // ge
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R8, INS_COND_LT); // lt
    theEmitter->emitIns_R_COND(INS_csetm, EA_4BYTE, REG_R7, INS_COND_GT); // gt
    theEmitter->emitIns_R_COND(INS_csetm, EA_8BYTE, REG_R6, INS_COND_LE); // le

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    // R_R_COND
    //

    // cinc reg, reg, cond
    // cinv reg, reg, cond
    // cneg reg, reg, cond
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_8BYTE, REG_R0, REG_R4, INS_COND_EQ); // eq
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R1, REG_R5, INS_COND_NE); // ne
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_4BYTE, REG_R2, REG_R6, INS_COND_HS); // hs
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_8BYTE, REG_R3, REG_R7, INS_COND_LO); // lo
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R4, REG_R8, INS_COND_MI); // mi
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_8BYTE, REG_R5, REG_R9, INS_COND_PL); // pl
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_8BYTE, REG_R6, REG_R0, INS_COND_VS); // vs
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R7, REG_R1, INS_COND_VC); // vc
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_8BYTE, REG_R8, REG_R2, INS_COND_HI); // hi
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_4BYTE, REG_R9, REG_R3, INS_COND_LS); // ls
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_4BYTE, REG_R0, REG_R4, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_COND(INS_cneg, EA_8BYTE, REG_R2, REG_R5, INS_COND_LT); // lt
    theEmitter->emitIns_R_R_COND(INS_cinc, EA_4BYTE, REG_R2, REG_R6, INS_COND_GT); // gt
    theEmitter->emitIns_R_R_COND(INS_cinv, EA_8BYTE, REG_R3, REG_R7, INS_COND_LE); // le

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    // R_R_R_COND
    //

    // csel  reg, reg, reg, cond
    // csinc reg, reg, reg, cond
    // csinv reg, reg, reg, cond
    // csneg reg, reg, reg, cond
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_8BYTE, REG_R0, REG_R4, REG_R8, INS_COND_EQ);  // eq
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_4BYTE, REG_R1, REG_R5, REG_R9, INS_COND_NE); // ne
    theEmitter->emitIns_R_R_R_COND(INS_csinv, EA_4BYTE, REG_R2, REG_R6, REG_R0, INS_COND_HS); // hs
    theEmitter->emitIns_R_R_R_COND(INS_csneg, EA_8BYTE, REG_R3, REG_R7, REG_R1, INS_COND_LO); // lo
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_4BYTE, REG_R4, REG_R8, REG_R2, INS_COND_MI);  // mi
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_8BYTE, REG_R5, REG_R9, REG_R3, INS_COND_PL); // pl
    theEmitter->emitIns_R_R_R_COND(INS_csinv, EA_8BYTE, REG_R6, REG_R0, REG_R4, INS_COND_VS); // vs
    theEmitter->emitIns_R_R_R_COND(INS_csneg, EA_4BYTE, REG_R7, REG_R1, REG_R5, INS_COND_VC); // vc
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_8BYTE, REG_R8, REG_R2, REG_R6, INS_COND_HI);  // hi
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_4BYTE, REG_R9, REG_R3, REG_R7, INS_COND_LS); // ls
    theEmitter->emitIns_R_R_R_COND(INS_csinv, EA_4BYTE, REG_R0, REG_R4, REG_R8, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_R_COND(INS_csneg, EA_8BYTE, REG_R2, REG_R5, REG_R9, INS_COND_LT); // lt
    theEmitter->emitIns_R_R_R_COND(INS_csel, EA_4BYTE, REG_R2, REG_R6, REG_R0, INS_COND_GT);  // gt
    theEmitter->emitIns_R_R_R_COND(INS_csinc, EA_8BYTE, REG_R3, REG_R7, REG_R1, INS_COND_LE); // le

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    // R_R_FLAGS_COND
    //

    // ccmp reg1, reg2, nzcv, cond
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R9, REG_R3, INS_FLAGS_V, INS_COND_EQ);    // eq
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R8, REG_R2, INS_FLAGS_C, INS_COND_NE);    // ne
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R7, REG_R1, INS_FLAGS_Z, INS_COND_HS);    // hs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R6, REG_R0, INS_FLAGS_N, INS_COND_LO);    // lo
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R5, REG_R3, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R4, REG_R2, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R3, REG_R1, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R2, REG_R0, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R1, REG_R3, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R0, REG_R2, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R9, REG_R1, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R8, REG_R0, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R7, REG_R3, INS_FLAGS_NZC, INS_COND_GT);  // gt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R6, REG_R2, INS_FLAGS_NZCV, INS_COND_LE); // le

    // ccmp reg1, imm, nzcv, cond
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R9, 3, INS_FLAGS_V, INS_COND_EQ);     // eq
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R8, 2, INS_FLAGS_C, INS_COND_NE);     // ne
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R7, 1, INS_FLAGS_Z, INS_COND_HS);     // hs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R6, 0, INS_FLAGS_N, INS_COND_LO);     // lo
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R5, 31, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R4, 28, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R3, 25, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R2, 22, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R1, 19, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R0, 16, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R9, 13, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R8, 10, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R7, 7, INS_FLAGS_NZC, INS_COND_GT);   // gt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R6, 4, INS_FLAGS_NZCV, INS_COND_LE);  // le

    // ccmp reg1, imm, nzcv, cond  -- encoded as ccmn
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R9, -3, INS_FLAGS_V, INS_COND_EQ);     // eq
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R8, -2, INS_FLAGS_C, INS_COND_NE);     // ne
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R7, -1, INS_FLAGS_Z, INS_COND_HS);     // hs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R6, -5, INS_FLAGS_N, INS_COND_LO);     // lo
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R5, -31, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R4, -28, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R3, -25, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R2, -22, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R1, -19, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R0, -16, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R9, -13, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R8, -10, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_8BYTE, REG_R7, -7, INS_FLAGS_NZC, INS_COND_GT);   // gt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmp, EA_4BYTE, REG_R6, -4, INS_FLAGS_NZCV, INS_COND_LE);  // le

    // ccmn reg1, reg2, nzcv, cond
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R9, REG_R3, INS_FLAGS_V, INS_COND_EQ);    // eq
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R8, REG_R2, INS_FLAGS_C, INS_COND_NE);    // ne
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R7, REG_R1, INS_FLAGS_Z, INS_COND_HS);    // hs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R6, REG_R0, INS_FLAGS_N, INS_COND_LO);    // lo
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R5, REG_R3, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R4, REG_R2, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R3, REG_R1, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R2, REG_R0, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R1, REG_R3, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R0, REG_R2, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R9, REG_R1, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R8, REG_R0, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R7, REG_R3, INS_FLAGS_NZC, INS_COND_GT);  // gt
    theEmitter->emitIns_R_R_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R6, REG_R2, INS_FLAGS_NZCV, INS_COND_LE); // le

    // ccmn reg1, imm, nzcv, cond
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R9, 3, INS_FLAGS_V, INS_COND_EQ);     // eq
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R8, 2, INS_FLAGS_C, INS_COND_NE);     // ne
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R7, 1, INS_FLAGS_Z, INS_COND_HS);     // hs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R6, 0, INS_FLAGS_N, INS_COND_LO);     // lo
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R5, 31, INS_FLAGS_CV, INS_COND_MI);   // mi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R4, 28, INS_FLAGS_ZV, INS_COND_PL);   // pl
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R3, 25, INS_FLAGS_ZC, INS_COND_VS);   // vs
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R2, 22, INS_FLAGS_NV, INS_COND_VC);   // vc
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R1, 19, INS_FLAGS_NC, INS_COND_HI);   // hi
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R0, 16, INS_FLAGS_NZ, INS_COND_LS);   // ls
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R9, 13, INS_FLAGS_NONE, INS_COND_GE); // ge
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R8, 10, INS_FLAGS_NZV, INS_COND_LT);  // lt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_8BYTE, REG_R7, 7, INS_FLAGS_NZC, INS_COND_GT);   // gt
    theEmitter->emitIns_R_I_FLAGS_COND(INS_ccmn, EA_4BYTE, REG_R6, 4, INS_FLAGS_NZCV, INS_COND_LE);  // le

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // Branch to register
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R(INS_br, EA_PTRSIZE, REG_R8);
    theEmitter->emitIns_R(INS_blr, EA_PTRSIZE, REG_R9);
    theEmitter->emitIns_R(INS_ret, EA_PTRSIZE, REG_R8);
    theEmitter->emitIns_R(INS_ret, EA_PTRSIZE, REG_LR);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // Misc
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_I(INS_brk, EA_PTRSIZE, 0);
    theEmitter->emitIns_I(INS_brk, EA_PTRSIZE, 65535);

    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_OSHLD);
    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_OSHST);
    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_OSH);

    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_NSHLD);
    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_NSHST);
    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_NSH);

    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_ISHLD);
    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_ISHST);
    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_ISH);

    theEmitter->emitIns_BARR(INS_dsb, INS_BARRIER_LD);
    theEmitter->emitIns_BARR(INS_dmb, INS_BARRIER_ST);
    theEmitter->emitIns_BARR(INS_isb, INS_BARRIER_SY);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    ////////////////////////////////////////////////////////////////////////////////
    //
    // SIMD and Floating point
    //
    ////////////////////////////////////////////////////////////////////////////////

    //
    // Load/Stores vector register
    //

    genDefineTempLabel(genCreateTempLabel());

    // ldr/str Vt, [reg]
    theEmitter->emitIns_R_R(INS_ldr, EA_8BYTE, REG_V1, REG_R9);
    theEmitter->emitIns_R_R(INS_str, EA_8BYTE, REG_V2, REG_R8);
    theEmitter->emitIns_R_R(INS_ldr, EA_4BYTE, REG_V3, REG_R7);
    theEmitter->emitIns_R_R(INS_str, EA_4BYTE, REG_V4, REG_R6);
    theEmitter->emitIns_R_R(INS_ldr, EA_2BYTE, REG_V5, REG_R5);
    theEmitter->emitIns_R_R(INS_str, EA_2BYTE, REG_V6, REG_R4);
    theEmitter->emitIns_R_R(INS_ldr, EA_1BYTE, REG_V7, REG_R3);
    theEmitter->emitIns_R_R(INS_str, EA_1BYTE, REG_V8, REG_R2);
    theEmitter->emitIns_R_R(INS_ldr, EA_16BYTE, REG_V9, REG_R1);
    theEmitter->emitIns_R_R(INS_str, EA_16BYTE, REG_V10, REG_R0);

    // ldr/str Vt, [reg+cns]        -- scaled
    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V8, REG_R9, 1);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V8, REG_R9, 2);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V8, REG_R9, 4);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V8, REG_R9, 8);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V8, REG_R9, 16);

    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V7, REG_R10, 1);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V7, REG_R10, 2);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V7, REG_R10, 4);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V7, REG_R10, 8);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V7, REG_R10, 16);

    // ldr/str Vt, [reg],cns        -- post-indexed (unscaled)
    // ldr/str Vt, [reg+cns]!       -- post-indexed (unscaled)
    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);

    theEmitter->emitIns_R_R_I(INS_ldr, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_ldr, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_I(INS_str, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_POST_INDEX);

    theEmitter->emitIns_R_R_I(INS_str, EA_1BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_2BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_4BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_8BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_I(INS_str, EA_16BYTE, REG_V8, REG_R9, 1, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_I(INS_ldur, EA_1BYTE, REG_V8, REG_R9, 2);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_2BYTE, REG_V8, REG_R9, 3);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_4BYTE, REG_V8, REG_R9, 5);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_8BYTE, REG_V8, REG_R9, 9);
    theEmitter->emitIns_R_R_I(INS_ldur, EA_16BYTE, REG_V8, REG_R9, 17);

    theEmitter->emitIns_R_R_I(INS_stur, EA_1BYTE, REG_V7, REG_R10, 2);
    theEmitter->emitIns_R_R_I(INS_stur, EA_2BYTE, REG_V7, REG_R10, 3);
    theEmitter->emitIns_R_R_I(INS_stur, EA_4BYTE, REG_V7, REG_R10, 5);
    theEmitter->emitIns_R_R_I(INS_stur, EA_8BYTE, REG_V7, REG_R10, 9);
    theEmitter->emitIns_R_R_I(INS_stur, EA_16BYTE, REG_V7, REG_R10, 17);

    // load/store pair
    theEmitter->emitIns_R_R_R(INS_ldnp, EA_8BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_V1, REG_V2, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_8BYTE, REG_V2, REG_V3, REG_R10, 8);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_8BYTE, REG_V3, REG_V4, REG_R10, 24);

    theEmitter->emitIns_R_R_R(INS_ldnp, EA_4BYTE, REG_V4, REG_V5, REG_SP);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_V5, REG_V6, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_4BYTE, REG_V6, REG_V7, REG_SP, 4);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_4BYTE, REG_V7, REG_V8, REG_SP, 12);

    theEmitter->emitIns_R_R_R(INS_ldnp, EA_16BYTE, REG_V8, REG_V9, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_16BYTE, REG_V9, REG_V10, REG_R10, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldnp, EA_16BYTE, REG_V10, REG_V11, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_stnp, EA_16BYTE, REG_V11, REG_V12, REG_R10, 48);

    theEmitter->emitIns_R_R_R(INS_ldp, EA_8BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V1, REG_V2, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_V2, REG_V3, REG_SP, 8);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V3, REG_V4, REG_R10, 16);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_V4, REG_V5, REG_R10, 24, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V5, REG_V6, REG_SP, 32, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, REG_V6, REG_V7, REG_SP, 40, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_8BYTE, REG_V7, REG_V8, REG_R10, 48, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R(INS_ldp, EA_4BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V1, REG_V2, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_V2, REG_V3, REG_SP, 4);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V3, REG_V4, REG_R10, 8);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_V4, REG_V5, REG_R10, 12, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V5, REG_V6, REG_SP, 16, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_4BYTE, REG_V6, REG_V7, REG_SP, 20, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_4BYTE, REG_V7, REG_V8, REG_R10, 24, INS_OPTS_PRE_INDEX);

    theEmitter->emitIns_R_R_R(INS_ldp, EA_16BYTE, REG_V0, REG_V1, REG_R10);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V1, REG_V2, REG_SP, 0);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_16BYTE, REG_V2, REG_V3, REG_SP, 16);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V3, REG_V4, REG_R10, 32);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_16BYTE, REG_V4, REG_V5, REG_R10, 48, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V5, REG_V6, REG_SP, 64, INS_OPTS_POST_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_ldp, EA_16BYTE, REG_V6, REG_V7, REG_SP, 80, INS_OPTS_PRE_INDEX);
    theEmitter->emitIns_R_R_R_I(INS_stp, EA_16BYTE, REG_V7, REG_V8, REG_R10, 96, INS_OPTS_PRE_INDEX);

    // LDR (register)
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 3);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_8BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 3);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 2);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_4BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 2);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 4);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_16BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 4);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V1, REG_SP, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V2, REG_R7, REG_R9, INS_OPTS_LSL);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_LSL, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V4, REG_R7, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_SXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V6, REG_SP, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V7, REG_R7, REG_R9, INS_OPTS_UXTW, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V8, REG_R7, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V9, REG_R7, REG_R9, INS_OPTS_SXTX, 1);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V10, REG_R7, REG_R9, INS_OPTS_UXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_2BYTE, REG_V11, REG_SP, REG_R9, INS_OPTS_UXTX, 1);

    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V1, REG_R7, REG_R9);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V2, REG_SP, REG_R9, INS_OPTS_SXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V3, REG_R7, REG_R9, INS_OPTS_UXTW);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V4, REG_SP, REG_R9, INS_OPTS_SXTX);
    theEmitter->emitIns_R_R_R_Ext(INS_ldr, EA_1BYTE, REG_V5, REG_R7, REG_R9, INS_OPTS_UXTX);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R   mov and aliases for mov
    //

    // mov vector to vector
    theEmitter->emitIns_R_R(INS_mov, EA_8BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_mov, EA_16BYTE, REG_V2, REG_V3);

    theEmitter->emitIns_R_R(INS_mov, EA_4BYTE, REG_V12, REG_V13);
    theEmitter->emitIns_R_R(INS_mov, EA_2BYTE, REG_V14, REG_V15);
    theEmitter->emitIns_R_R(INS_mov, EA_1BYTE, REG_V16, REG_V17);

    // mov vector to general
    theEmitter->emitIns_R_R(INS_mov, EA_8BYTE, REG_R0, REG_V4);
    theEmitter->emitIns_R_R(INS_mov, EA_4BYTE, REG_R1, REG_V5);
    theEmitter->emitIns_R_R(INS_mov, EA_2BYTE, REG_R2, REG_V6);
    theEmitter->emitIns_R_R(INS_mov, EA_1BYTE, REG_R3, REG_V7);

    // mov general to vector
    theEmitter->emitIns_R_R(INS_mov, EA_8BYTE, REG_V8, REG_R4);
    theEmitter->emitIns_R_R(INS_mov, EA_4BYTE, REG_V9, REG_R5);
    theEmitter->emitIns_R_R(INS_mov, EA_2BYTE, REG_V10, REG_R6);
    theEmitter->emitIns_R_R(INS_mov, EA_1BYTE, REG_V11, REG_R7);

    // mov vector[index] to vector
    theEmitter->emitIns_R_R_I(INS_mov, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_mov, EA_4BYTE, REG_V2, REG_V3, 3);
    theEmitter->emitIns_R_R_I(INS_mov, EA_2BYTE, REG_V4, REG_V5, 7);
    theEmitter->emitIns_R_R_I(INS_mov, EA_1BYTE, REG_V6, REG_V7, 15);

    // mov to general from vector[index]
    theEmitter->emitIns_R_R_I(INS_mov, EA_8BYTE, REG_R8, REG_V16, 1);
    theEmitter->emitIns_R_R_I(INS_mov, EA_4BYTE, REG_R9, REG_V17, 2);
    theEmitter->emitIns_R_R_I(INS_mov, EA_2BYTE, REG_R10, REG_V18, 3);
    theEmitter->emitIns_R_R_I(INS_mov, EA_1BYTE, REG_R11, REG_V19, 4);

    // mov to vector[index] from general
    theEmitter->emitIns_R_R_I(INS_mov, EA_8BYTE, REG_V20, REG_R12, 1);
    theEmitter->emitIns_R_R_I(INS_mov, EA_4BYTE, REG_V21, REG_R13, 2);
    theEmitter->emitIns_R_R_I(INS_mov, EA_2BYTE, REG_V22, REG_R14, 6);
    theEmitter->emitIns_R_R_I(INS_mov, EA_1BYTE, REG_V23, REG_R15, 8);

    // mov vector[index] to vector[index2]
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_8BYTE, REG_V8, REG_V9, 1, 0);
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_4BYTE, REG_V10, REG_V11, 2, 1);
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_2BYTE, REG_V12, REG_V13, 5, 2);
    theEmitter->emitIns_R_R_I_I(INS_mov, EA_1BYTE, REG_V14, REG_V15, 12, 3);

    //////////////////////////////////////////////////////////////////////////////////

    // mov/dup scalar
    theEmitter->emitIns_R_R_I(INS_dup, EA_8BYTE, REG_V24, REG_V25, 1);
    theEmitter->emitIns_R_R_I(INS_dup, EA_4BYTE, REG_V26, REG_V27, 3);
    theEmitter->emitIns_R_R_I(INS_dup, EA_2BYTE, REG_V28, REG_V29, 7);
    theEmitter->emitIns_R_R_I(INS_dup, EA_1BYTE, REG_V30, REG_V31, 15);

    // mov/ins vector element
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_8BYTE, REG_V0, REG_V1, 0, 1);
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_4BYTE, REG_V2, REG_V3, 2, 2);
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_2BYTE, REG_V4, REG_V5, 4, 3);
    theEmitter->emitIns_R_R_I_I(INS_ins, EA_1BYTE, REG_V6, REG_V7, 8, 4);

    // umov to general from vector element
    theEmitter->emitIns_R_R_I(INS_umov, EA_8BYTE, REG_R0, REG_V8, 1);
    theEmitter->emitIns_R_R_I(INS_umov, EA_4BYTE, REG_R1, REG_V9, 2);
    theEmitter->emitIns_R_R_I(INS_umov, EA_2BYTE, REG_R2, REG_V10, 4);
    theEmitter->emitIns_R_R_I(INS_umov, EA_1BYTE, REG_R3, REG_V11, 8);

    // ins to vector element from general
    theEmitter->emitIns_R_R_I(INS_ins, EA_8BYTE, REG_V12, REG_R4, 1);
    theEmitter->emitIns_R_R_I(INS_ins, EA_4BYTE, REG_V13, REG_R5, 3);
    theEmitter->emitIns_R_R_I(INS_ins, EA_2BYTE, REG_V14, REG_R6, 7);
    theEmitter->emitIns_R_R_I(INS_ins, EA_1BYTE, REG_V15, REG_R7, 15);

    // smov to general from vector element
    theEmitter->emitIns_R_R_I(INS_smov, EA_4BYTE, REG_R5, REG_V17, 2);
    theEmitter->emitIns_R_R_I(INS_smov, EA_2BYTE, REG_R6, REG_V18, 4);
    theEmitter->emitIns_R_R_I(INS_smov, EA_1BYTE, REG_R7, REG_V19, 8);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_I   movi and mvni
    //

    // movi  imm8  (vector)
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V0, 0x00, INS_OPTS_8B);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V1, 0xFF, INS_OPTS_8B);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V2, 0x00, INS_OPTS_16B);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V3, 0xFF, INS_OPTS_16B);

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V4, 0x007F, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V5, 0x7F00, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V6, 0x003F, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V7, 0x3F00, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V8, 0x1F, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V9, 0x1F00, INS_OPTS_2S);      // LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V10, 0x1F0000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V11, 0x1F000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V12, 0x1FFF, INS_OPTS_2S);   // MSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V13, 0x1FFFFF, INS_OPTS_2S); // MSL 16

    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V14, 0x37, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V15, 0x3700, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V16, 0x370000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V17, 0x37000000, INS_OPTS_4S); // LSL 24

    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V18, 0x37FF, INS_OPTS_4S);   // MSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V19, 0x37FFFF, INS_OPTS_4S); // MSL 16

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V20, 0xFF80, INS_OPTS_4H);  // mvni
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V21, 0xFFC0, INS_OPTS_8H); // mvni

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V22, 0xFFFFFFE0, INS_OPTS_2S);  // mvni
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V23, 0xFFFFF0FF, INS_OPTS_4S); // mvni LSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V24, 0xFFF8FFFF, INS_OPTS_2S);  // mvni LSL 16
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V25, 0xFCFFFFFF, INS_OPTS_4S); // mvni LSL 24

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V26, 0xFFFFFE00, INS_OPTS_2S);  // mvni MSL  8
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V27, 0xFFFC0000, INS_OPTS_4S); // mvni MSL 16

    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V28, 0x00FF00FF00FF00FF, INS_OPTS_1D);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V29, 0x00FFFF0000FFFF00, INS_OPTS_2D);
    theEmitter->emitIns_R_I(INS_movi, EA_8BYTE, REG_V30, 0xFF000000FF000000);
    theEmitter->emitIns_R_I(INS_movi, EA_16BYTE, REG_V31, 0x0, INS_OPTS_2D);

    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V0, 0x0022, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V1, 0x2200, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V2, 0x0033, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V3, 0x3300, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V4, 0x42, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V5, 0x4200, INS_OPTS_2S);     // LSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V6, 0x420000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V7, 0x42000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V8, 0x42FF, INS_OPTS_2S);   // MSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_8BYTE, REG_V9, 0x42FFFF, INS_OPTS_2S); // MSL 16

    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V10, 0x5D, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V11, 0x5D00, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V12, 0x5D0000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V13, 0x5D000000, INS_OPTS_4S); // LSL 24

    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V14, 0x5DFF, INS_OPTS_4S);   // MSL  8
    theEmitter->emitIns_R_I(INS_mvni, EA_16BYTE, REG_V15, 0x5DFFFF, INS_OPTS_4S); // MSL 16

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_I   orr/bic vector immediate
    //

    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V0, 0x0022, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V1, 0x2200, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V2, 0x0033, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V3, 0x3300, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V4, 0x42, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V5, 0x4200, INS_OPTS_2S);     // LSL  8
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V6, 0x420000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_orr, EA_8BYTE, REG_V7, 0x42000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V10, 0x5D, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V11, 0x5D00, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V12, 0x5D0000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_orr, EA_16BYTE, REG_V13, 0x5D000000, INS_OPTS_4S); // LSL 24

    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V0, 0x0022, INS_OPTS_4H);
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V1, 0x2200, INS_OPTS_4H); // LSL  8
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V2, 0x0033, INS_OPTS_8H);
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V3, 0x3300, INS_OPTS_8H); // LSL  8

    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V4, 0x42, INS_OPTS_2S);
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V5, 0x4200, INS_OPTS_2S);     // LSL  8
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V6, 0x420000, INS_OPTS_2S);   // LSL 16
    theEmitter->emitIns_R_I(INS_bic, EA_8BYTE, REG_V7, 0x42000000, INS_OPTS_2S); // LSL 24

    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V10, 0x5D, INS_OPTS_4S);
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V11, 0x5D00, INS_OPTS_4S);     // LSL  8
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V12, 0x5D0000, INS_OPTS_4S);   // LSL 16
    theEmitter->emitIns_R_I(INS_bic, EA_16BYTE, REG_V13, 0x5D000000, INS_OPTS_4S); // LSL 24

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_F   cmp/fmov immediate
    //

    // fmov  imm8  (scalar)
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V14, 1.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V15, -1.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V0, 2.0); // encodes imm8 == 0
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V16, 10.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V17, -10.0);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V18, 31); // Largest encodable value
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V19, -31);
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V20, 1.25);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V21, -1.25);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V22, 0.125); // Smallest encodable value
    theEmitter->emitIns_R_F(INS_fmov, EA_4BYTE, REG_V23, -0.125);

    // fmov  imm8  (vector)
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V0, 2.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V24, 1.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V25, 1.0, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V26, 1.0, INS_OPTS_2D);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V27, -10.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V28, -10.0, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V29, -10.0, INS_OPTS_2D);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V30, 31.0, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V31, 31.0, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V0, 31.0, INS_OPTS_2D);
    theEmitter->emitIns_R_F(INS_fmov, EA_8BYTE, REG_V1, -0.125, INS_OPTS_2S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V2, -0.125, INS_OPTS_4S);
    theEmitter->emitIns_R_F(INS_fmov, EA_16BYTE, REG_V3, -0.125, INS_OPTS_2D);

    // fcmp with 0.0
    theEmitter->emitIns_R_F(INS_fcmp, EA_8BYTE, REG_V12, 0.0);
    theEmitter->emitIns_R_F(INS_fcmp, EA_4BYTE, REG_V13, 0.0);
    theEmitter->emitIns_R_F(INS_fcmpe, EA_8BYTE, REG_V14, 0.0);
    theEmitter->emitIns_R_F(INS_fcmpe, EA_4BYTE, REG_V15, 0.0);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R   fmov/fcmp/fcvt
    //

    // fmov to vector to vector
    theEmitter->emitIns_R_R(INS_fmov, EA_8BYTE, REG_V0, REG_V2);
    theEmitter->emitIns_R_R(INS_fmov, EA_4BYTE, REG_V1, REG_V3);

    // fmov to vector to general
    theEmitter->emitIns_R_R(INS_fmov, EA_8BYTE, REG_R0, REG_V4);
    theEmitter->emitIns_R_R(INS_fmov, EA_4BYTE, REG_R1, REG_V5);
    //    using the optional conversion specifier
    theEmitter->emitIns_R_R(INS_fmov, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_D_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fmov, EA_4BYTE, REG_R3, REG_V7, INS_OPTS_S_TO_4BYTE);

    // fmov to general to vector
    theEmitter->emitIns_R_R(INS_fmov, EA_8BYTE, REG_V8, REG_R4);
    theEmitter->emitIns_R_R(INS_fmov, EA_4BYTE, REG_V9, REG_R5);
    //   using the optional conversion specifier
    theEmitter->emitIns_R_R(INS_fmov, EA_8BYTE, REG_V10, REG_R6, INS_OPTS_8BYTE_TO_D);
    theEmitter->emitIns_R_R(INS_fmov, EA_4BYTE, REG_V11, REG_R7, INS_OPTS_4BYTE_TO_S);

    // fcmp/fcmpe
    theEmitter->emitIns_R_R(INS_fcmp, EA_8BYTE, REG_V8, REG_V16);
    theEmitter->emitIns_R_R(INS_fcmp, EA_4BYTE, REG_V9, REG_V17);
    theEmitter->emitIns_R_R(INS_fcmpe, EA_8BYTE, REG_V10, REG_V18);
    theEmitter->emitIns_R_R(INS_fcmpe, EA_4BYTE, REG_V11, REG_V19);

    // fcvt
    theEmitter->emitIns_R_R(INS_fcvt, EA_8BYTE, REG_V24, REG_V25, INS_OPTS_S_TO_D); // Single to Double
    theEmitter->emitIns_R_R(INS_fcvt, EA_4BYTE, REG_V26, REG_V27, INS_OPTS_D_TO_S); // Double to Single

    theEmitter->emitIns_R_R(INS_fcvt, EA_4BYTE, REG_V1, REG_V2, INS_OPTS_H_TO_S);
    theEmitter->emitIns_R_R(INS_fcvt, EA_8BYTE, REG_V3, REG_V4, INS_OPTS_H_TO_D);

    theEmitter->emitIns_R_R(INS_fcvt, EA_2BYTE, REG_V5, REG_V6, INS_OPTS_S_TO_H);
    theEmitter->emitIns_R_R(INS_fcvt, EA_2BYTE, REG_V7, REG_V8, INS_OPTS_D_TO_H);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R   floating point conversions
    //

    // fcvtas scalar
    theEmitter->emitIns_R_R(INS_fcvtas, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_V2, REG_V3);

    // fcvtas scalar to general
    theEmitter->emitIns_R_R(INS_fcvtas, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtas vector
    theEmitter->emitIns_R_R(INS_fcvtas, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtas, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtau scalar
    theEmitter->emitIns_R_R(INS_fcvtau, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_V2, REG_V3);

    // fcvtau scalar to general
    theEmitter->emitIns_R_R(INS_fcvtau, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtau vector
    theEmitter->emitIns_R_R(INS_fcvtau, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtau, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtms scalar
    theEmitter->emitIns_R_R(INS_fcvtms, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_V2, REG_V3);

    // fcvtms scalar to general
    theEmitter->emitIns_R_R(INS_fcvtms, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtms vector
    theEmitter->emitIns_R_R(INS_fcvtms, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtms, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtmu scalar
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtmu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtmu vector
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtmu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtns scalar
    theEmitter->emitIns_R_R(INS_fcvtns, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_V2, REG_V3);

    // fcvtns scalar to general
    theEmitter->emitIns_R_R(INS_fcvtns, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtns vector
    theEmitter->emitIns_R_R(INS_fcvtns, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtns, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtnu scalar
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtnu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtnu vector
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtnu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtps scalar
    theEmitter->emitIns_R_R(INS_fcvtps, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_V2, REG_V3);

    // fcvtps scalar to general
    theEmitter->emitIns_R_R(INS_fcvtps, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtps vector
    theEmitter->emitIns_R_R(INS_fcvtps, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtps, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtpu scalar
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtpu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtpu vector
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtpu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // fcvtzs scalar
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_V2, REG_V3);

    // fcvtzs scalar to general
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtzs vector
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtzs, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // fcvtzu scalar
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_V2, REG_V3);

    // fcvtzu scalar to general
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_4BYTE, REG_R0, REG_V4, INS_OPTS_S_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_4BYTE, REG_R1, REG_V5, INS_OPTS_D_TO_4BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_R2, REG_V6, INS_OPTS_S_TO_8BYTE);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_R3, REG_V7, INS_OPTS_D_TO_8BYTE);

    // fcvtzu vector
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fcvtzu, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    ////////////////////////////////////////////////////////////////////////////////

    // scvtf scalar
    theEmitter->emitIns_R_R(INS_scvtf, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V2, REG_V3);

    // scvtf scalar from general
    theEmitter->emitIns_R_R(INS_scvtf, EA_4BYTE, REG_V4, REG_R0, INS_OPTS_4BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_4BYTE, REG_V5, REG_R1, INS_OPTS_8BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V6, REG_R2, INS_OPTS_4BYTE_TO_D);
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V7, REG_R3, INS_OPTS_8BYTE_TO_D);

    // scvtf vector
    theEmitter->emitIns_R_R(INS_scvtf, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_scvtf, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

    // ucvtf scalar
    theEmitter->emitIns_R_R(INS_ucvtf, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V2, REG_V3);

    // ucvtf scalar from general
    theEmitter->emitIns_R_R(INS_ucvtf, EA_4BYTE, REG_V4, REG_R0, INS_OPTS_4BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_4BYTE, REG_V5, REG_R1, INS_OPTS_8BYTE_TO_S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V6, REG_R2, INS_OPTS_4BYTE_TO_D);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V7, REG_R3, INS_OPTS_8BYTE_TO_D);

    // ucvtf vector
    theEmitter->emitIns_R_R(INS_ucvtf, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_ucvtf, EA_16BYTE, REG_V12, REG_V13, INS_OPTS_2D);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R   floating point operations, one dest, one source
    //

    // fabs scalar
    theEmitter->emitIns_R_R(INS_fabs, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fabs, EA_8BYTE, REG_V2, REG_V3);

    // fabs vector
    theEmitter->emitIns_R_R(INS_fabs, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fabs, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fabs, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // fneg scalar
    theEmitter->emitIns_R_R(INS_fneg, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fneg, EA_8BYTE, REG_V2, REG_V3);

    // fneg vector
    theEmitter->emitIns_R_R(INS_fneg, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fneg, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fneg, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // fsqrt scalar
    theEmitter->emitIns_R_R(INS_fsqrt, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_fsqrt, EA_8BYTE, REG_V2, REG_V3);

    // fsqrt vector
    theEmitter->emitIns_R_R(INS_fsqrt, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_fsqrt, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_fsqrt, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    genDefineTempLabel(genCreateTempLabel());

    // abs scalar
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V2, REG_V3);

    // abs vector
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_abs, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_abs, EA_16BYTE, REG_V16, REG_V17, INS_OPTS_2D);

    // neg scalar
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V2, REG_V3);

    // neg vector
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_neg, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_neg, EA_16BYTE, REG_V16, REG_V17, INS_OPTS_2D);

    // mvn vector
    theEmitter->emitIns_R_R(INS_mvn, EA_8BYTE, REG_V4, REG_V5);
    theEmitter->emitIns_R_R(INS_mvn, EA_8BYTE, REG_V6, REG_V7, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_mvn, EA_16BYTE, REG_V8, REG_V9);
    theEmitter->emitIns_R_R(INS_mvn, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_16B);

    // cnt vector
    theEmitter->emitIns_R_R(INS_cnt, EA_8BYTE, REG_V22, REG_V23, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_cnt, EA_16BYTE, REG_V24, REG_V25, INS_OPTS_16B);

    // not vector (the same encoding as mvn)
    theEmitter->emitIns_R_R(INS_not, EA_8BYTE, REG_V12, REG_V13);
    theEmitter->emitIns_R_R(INS_not, EA_8BYTE, REG_V14, REG_V15, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_not, EA_16BYTE, REG_V16, REG_V17);
    theEmitter->emitIns_R_R(INS_not, EA_16BYTE, REG_V18, REG_V19, INS_OPTS_16B);

    // cls vector
    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_cls, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_cls, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_cls, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_cls, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // clz vector
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_clz, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_clz, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_clz, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_clz, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

    // rbit vector
    theEmitter->emitIns_R_R(INS_rbit, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rbit, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);

    // rev16 vector
    theEmitter->emitIns_R_R(INS_rev16, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rev16, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);

    // rev32 vector
    theEmitter->emitIns_R_R(INS_rev32, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rev32, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_rev32, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_rev32, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);

    // rev64 vector
    theEmitter->emitIns_R_R(INS_rev64, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_rev64, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_rev64, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_rev64, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_rev64, EA_8BYTE, REG_V12, REG_V13, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_rev64, EA_16BYTE, REG_V14, REG_V15, INS_OPTS_4S);

#endif

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R   floating point round to int, one dest, one source
    //

    // frinta scalar
    theEmitter->emitIns_R_R(INS_frinta, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frinta, EA_8BYTE, REG_V2, REG_V3);

    // frinta vector
    theEmitter->emitIns_R_R(INS_frinta, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frinta, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frinta, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frinti scalar
    theEmitter->emitIns_R_R(INS_frinti, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frinti, EA_8BYTE, REG_V2, REG_V3);

    // frinti vector
    theEmitter->emitIns_R_R(INS_frinti, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frinti, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frinti, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintm scalar
    theEmitter->emitIns_R_R(INS_frintm, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintm, EA_8BYTE, REG_V2, REG_V3);

    // frintm vector
    theEmitter->emitIns_R_R(INS_frintm, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintm, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintm, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintn scalar
    theEmitter->emitIns_R_R(INS_frintn, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintn, EA_8BYTE, REG_V2, REG_V3);

    // frintn vector
    theEmitter->emitIns_R_R(INS_frintn, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintn, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintn, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintp scalar
    theEmitter->emitIns_R_R(INS_frintp, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintp, EA_8BYTE, REG_V2, REG_V3);

    // frintp vector
    theEmitter->emitIns_R_R(INS_frintp, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintp, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintp, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintx scalar
    theEmitter->emitIns_R_R(INS_frintx, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintx, EA_8BYTE, REG_V2, REG_V3);

    // frintx vector
    theEmitter->emitIns_R_R(INS_frintx, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintx, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintx, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

    // frintz scalar
    theEmitter->emitIns_R_R(INS_frintz, EA_4BYTE, REG_V0, REG_V1);
    theEmitter->emitIns_R_R(INS_frintz, EA_8BYTE, REG_V2, REG_V3);

    // frintz vector
    theEmitter->emitIns_R_R(INS_frintz, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_frintz, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_4S);
    theEmitter->emitIns_R_R(INS_frintz, EA_16BYTE, REG_V8, REG_V9, INS_OPTS_2D);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R   floating point operations, one dest, two source
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_fadd, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fadd, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fadd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fadd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fadd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fsub, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fsub, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fsub, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fsub, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fsub, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fdiv, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fdiv, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fmax, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmax, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmax, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmax, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmax, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fmin, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmin, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmin, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmin, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmin, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    // fabd
    theEmitter->emitIns_R_R_R(INS_fabd, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fabd, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fabd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fabd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fabd, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_fmul, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmul, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmul, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmul, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmul, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by elem 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by elem 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmul, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fmulx, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmulx, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by elem 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by elem 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmulx, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fnmul, EA_4BYTE, REG_V0, REG_V1, REG_V2); // scalar 4BYTE
    theEmitter->emitIns_R_R_R(INS_fnmul, EA_8BYTE, REG_V3, REG_V4, REG_V5); // scalar 8BYTE

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_I  vector operations, one dest, one source reg, one immed
    //

    genDefineTempLabel(genCreateTempLabel());

    // 'sshr' scalar
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'sshr' vector
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_sshr, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'ssra' scalar
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'ssra' vector
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_ssra, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'srshr' scalar
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'srshr' vector
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_srshr, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'srsra' scalar
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'srsra' vector
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_srsra, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'shl' scalar
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'shl' vector
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_shl, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_shl, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'ushr' scalar
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'ushr' vector
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_ushr, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'usra' scalar
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'usra' vector
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_usra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_usra, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'urshr' scalar
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'urshr' vector
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_urshr, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'ursra' scalar
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'srsra' vector
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_ursra, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'sri' scalar
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'sri' vector
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sri, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_sri, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'sli' scalar
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V0, REG_V1, 1);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V2, REG_V3, 14);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V4, REG_V5, 27);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V6, REG_V7, 40);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V8, REG_V9, 63);

    // 'sli' vector
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sli, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V12, REG_V13, 33, INS_OPTS_2D);
    theEmitter->emitIns_R_R_I(INS_sli, EA_16BYTE, REG_V14, REG_V15, 63, INS_OPTS_2D);

    // 'sshll' vector
    theEmitter->emitIns_R_R_I(INS_sshll, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_sshll2, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_sshll, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_sshll2, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_sshll, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_sshll2, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);

    // 'ushll' vector
    theEmitter->emitIns_R_R_I(INS_ushll, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_ushll2, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_ushll, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_ushll2, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_ushll, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_ushll2, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);

    // 'shrn' vector
    theEmitter->emitIns_R_R_I(INS_shrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_shrn2, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_shrn, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_shrn2, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_shrn, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_shrn2, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);

    // 'rshrn' vector
    theEmitter->emitIns_R_R_I(INS_rshrn, EA_8BYTE, REG_V0, REG_V1, 1, INS_OPTS_8B);
    theEmitter->emitIns_R_R_I(INS_rshrn2, EA_16BYTE, REG_V2, REG_V3, 7, INS_OPTS_16B);
    theEmitter->emitIns_R_R_I(INS_rshrn, EA_8BYTE, REG_V4, REG_V5, 9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_I(INS_rshrn2, EA_16BYTE, REG_V6, REG_V7, 15, INS_OPTS_8H);
    theEmitter->emitIns_R_R_I(INS_rshrn, EA_8BYTE, REG_V8, REG_V9, 17, INS_OPTS_2S);
    theEmitter->emitIns_R_R_I(INS_rshrn2, EA_16BYTE, REG_V10, REG_V11, 31, INS_OPTS_4S);

    // 'sxtl' vector
    theEmitter->emitIns_R_R(INS_sxtl, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_sxtl2, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_sxtl, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_sxtl2, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_sxtl, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_sxtl2, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

    // 'uxtl' vector
    theEmitter->emitIns_R_R(INS_uxtl, EA_8BYTE, REG_V0, REG_V1, INS_OPTS_8B);
    theEmitter->emitIns_R_R(INS_uxtl2, EA_16BYTE, REG_V2, REG_V3, INS_OPTS_16B);
    theEmitter->emitIns_R_R(INS_uxtl, EA_8BYTE, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R(INS_uxtl2, EA_16BYTE, REG_V6, REG_V7, INS_OPTS_8H);
    theEmitter->emitIns_R_R(INS_uxtl, EA_8BYTE, REG_V8, REG_V9, INS_OPTS_2S);
    theEmitter->emitIns_R_R(INS_uxtl2, EA_16BYTE, REG_V10, REG_V11, INS_OPTS_4S);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R   vector operations, one dest, two source
    //

    genDefineTempLabel(genCreateTempLabel());

    // Specifying an Arrangement is optional
    //
    theEmitter->emitIns_R_R_R(INS_and, EA_8BYTE, REG_V6, REG_V7, REG_V8);
    theEmitter->emitIns_R_R_R(INS_bic, EA_8BYTE, REG_V9, REG_V10, REG_V11);
    theEmitter->emitIns_R_R_R(INS_eor, EA_8BYTE, REG_V12, REG_V13, REG_V14);
    theEmitter->emitIns_R_R_R(INS_orr, EA_8BYTE, REG_V15, REG_V16, REG_V17);
    theEmitter->emitIns_R_R_R(INS_orn, EA_8BYTE, REG_V18, REG_V19, REG_V20);
    theEmitter->emitIns_R_R_R(INS_and, EA_16BYTE, REG_V21, REG_V22, REG_V23);
    theEmitter->emitIns_R_R_R(INS_bic, EA_16BYTE, REG_V24, REG_V25, REG_V26);
    theEmitter->emitIns_R_R_R(INS_eor, EA_16BYTE, REG_V27, REG_V28, REG_V29);
    theEmitter->emitIns_R_R_R(INS_orr, EA_16BYTE, REG_V30, REG_V31, REG_V0);
    theEmitter->emitIns_R_R_R(INS_orn, EA_16BYTE, REG_V1, REG_V2, REG_V3);

    theEmitter->emitIns_R_R_R(INS_bsl, EA_8BYTE, REG_V4, REG_V5, REG_V6);
    theEmitter->emitIns_R_R_R(INS_bit, EA_8BYTE, REG_V7, REG_V8, REG_V9);
    theEmitter->emitIns_R_R_R(INS_bif, EA_8BYTE, REG_V10, REG_V11, REG_V12);
    theEmitter->emitIns_R_R_R(INS_bsl, EA_16BYTE, REG_V13, REG_V14, REG_V15);
    theEmitter->emitIns_R_R_R(INS_bit, EA_16BYTE, REG_V16, REG_V17, REG_V18);
    theEmitter->emitIns_R_R_R(INS_bif, EA_16BYTE, REG_V19, REG_V20, REG_V21);

    // Default Arrangement as per the ARM64 manual
    //
    theEmitter->emitIns_R_R_R(INS_and, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bic, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_eor, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_orr, EA_8BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_orn, EA_8BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_and, EA_16BYTE, REG_V21, REG_V22, REG_V23, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_bic, EA_16BYTE, REG_V24, REG_V25, REG_V26, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_eor, EA_16BYTE, REG_V27, REG_V28, REG_V29, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_orr, EA_16BYTE, REG_V30, REG_V31, REG_V0, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_orn, EA_16BYTE, REG_V1, REG_V2, REG_V3, INS_OPTS_16B);

    theEmitter->emitIns_R_R_R(INS_bsl, EA_8BYTE, REG_V4, REG_V5, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bit, EA_8BYTE, REG_V7, REG_V8, REG_V9, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bif, EA_8BYTE, REG_V10, REG_V11, REG_V12, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_bsl, EA_16BYTE, REG_V13, REG_V14, REG_V15, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_bit, EA_16BYTE, REG_V16, REG_V17, REG_V18, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_bif, EA_16BYTE, REG_V19, REG_V20, REG_V21, INS_OPTS_16B);

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V0, REG_V1, REG_V2); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_add, EA_8BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_add, EA_16BYTE, REG_V21, REG_V22, REG_V23, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V1, REG_V2, REG_V3); // scalar 8BYTE
    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V4, REG_V5, REG_V6, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V7, REG_V8, REG_V9, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sub, EA_8BYTE, REG_V10, REG_V11, REG_V12, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V13, REG_V14, REG_V15, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V16, REG_V17, REG_V18, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V19, REG_V20, REG_V21, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_sub, EA_16BYTE, REG_V22, REG_V23, REG_V24, INS_OPTS_2D);

    genDefineTempLabel(genCreateTempLabel());

    // saba vector
    theEmitter->emitIns_R_R_R(INS_saba, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_saba, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_saba, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_saba, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_saba, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_saba, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // sabd vector
    theEmitter->emitIns_R_R_R(INS_sabd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_sabd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uaba vector
    theEmitter->emitIns_R_R_R(INS_uaba, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uaba, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    // uabd vector
    theEmitter->emitIns_R_R_R(INS_uabd, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_16BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_8BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_uabd, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R  vector multiply
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_V0, REG_V1, REG_V2, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_V3, REG_V4, REG_V5, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R(INS_mul, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_mul, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_16B);
    theEmitter->emitIns_R_R_R(INS_mul, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R(INS_mul, EA_16BYTE, REG_V15, REG_V16, REG_V17, INS_OPTS_4S);

    theEmitter->emitIns_R_R_R(INS_pmul, EA_8BYTE, REG_V18, REG_V19, REG_V20, INS_OPTS_8B);
    theEmitter->emitIns_R_R_R(INS_pmul, EA_16BYTE, REG_V21, REG_V22, REG_V23, INS_OPTS_16B);

    // 'mul' vector by elem
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V0, REG_V1, REG_V16, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V2, REG_V3, REG_V15, 1, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V4, REG_V5, REG_V17, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V6, REG_V7, REG_V0, 0, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V8, REG_V9, REG_V1, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_8BYTE, REG_V10, REG_V11, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V12, REG_V13, REG_V14, 0, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V14, REG_V15, REG_V18, 1, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V16, REG_V17, REG_V13, 3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V18, REG_V19, REG_V3, 0, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V20, REG_V21, REG_V4, 3, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mul, EA_16BYTE, REG_V22, REG_V23, REG_V5, 7, INS_OPTS_8H);

    // 'mla' vector by elem
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V0, REG_V1, REG_V16, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V2, REG_V3, REG_V15, 1, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V4, REG_V5, REG_V17, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V6, REG_V7, REG_V0, 0, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V8, REG_V9, REG_V1, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_8BYTE, REG_V10, REG_V11, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V12, REG_V13, REG_V14, 0, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V14, REG_V15, REG_V18, 1, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V16, REG_V17, REG_V13, 3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V18, REG_V19, REG_V3, 0, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V20, REG_V21, REG_V4, 3, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mla, EA_16BYTE, REG_V22, REG_V23, REG_V5, 7, INS_OPTS_8H);

    // 'mls' vector by elem
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V0, REG_V1, REG_V16, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V2, REG_V3, REG_V15, 1, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V4, REG_V5, REG_V17, 3, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V6, REG_V7, REG_V0, 0, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V8, REG_V9, REG_V1, 3, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_8BYTE, REG_V10, REG_V11, REG_V2, 7, INS_OPTS_4H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V12, REG_V13, REG_V14, 0, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V14, REG_V15, REG_V18, 1, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V16, REG_V17, REG_V13, 3, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V18, REG_V19, REG_V3, 0, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V20, REG_V21, REG_V4, 3, INS_OPTS_8H);
    theEmitter->emitIns_R_R_R_I(INS_mls, EA_16BYTE, REG_V22, REG_V23, REG_V5, 7, INS_OPTS_8H);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R   floating point operations, one source/dest, and two source
    //

    genDefineTempLabel(genCreateTempLabel());

    theEmitter->emitIns_R_R_R(INS_fmla, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmla, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmla, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by elem 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by elem 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmla, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R(INS_fmls, EA_8BYTE, REG_V6, REG_V7, REG_V8, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R(INS_fmls, EA_16BYTE, REG_V9, REG_V10, REG_V11, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R(INS_fmls, EA_16BYTE, REG_V12, REG_V13, REG_V14, INS_OPTS_2D);

    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_4BYTE, REG_V15, REG_V16, REG_V17, 3); // scalar by elem 4BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_8BYTE, REG_V18, REG_V19, REG_V20, 1); // scalar by elem 8BYTE
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_8BYTE, REG_V21, REG_V22, REG_V23, 0, INS_OPTS_2S);
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_16BYTE, REG_V24, REG_V25, REG_V26, 2, INS_OPTS_4S);
    theEmitter->emitIns_R_R_R_I(INS_fmls, EA_16BYTE, REG_V27, REG_V28, REG_V29, 0, INS_OPTS_2D);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS
    //
    // R_R_R_R   floating point operations, one dest, and three source
    //

    theEmitter->emitIns_R_R_R_R(INS_fmadd, EA_4BYTE, REG_V0, REG_V8, REG_V16, REG_V24);
    theEmitter->emitIns_R_R_R_R(INS_fmsub, EA_4BYTE, REG_V1, REG_V9, REG_V17, REG_V25);
    theEmitter->emitIns_R_R_R_R(INS_fnmadd, EA_4BYTE, REG_V2, REG_V10, REG_V18, REG_V26);
    theEmitter->emitIns_R_R_R_R(INS_fnmsub, EA_4BYTE, REG_V3, REG_V11, REG_V19, REG_V27);

    theEmitter->emitIns_R_R_R_R(INS_fmadd, EA_8BYTE, REG_V4, REG_V12, REG_V20, REG_V28);
    theEmitter->emitIns_R_R_R_R(INS_fmsub, EA_8BYTE, REG_V5, REG_V13, REG_V21, REG_V29);
    theEmitter->emitIns_R_R_R_R(INS_fnmadd, EA_8BYTE, REG_V6, REG_V14, REG_V22, REG_V30);
    theEmitter->emitIns_R_R_R_R(INS_fnmsub, EA_8BYTE, REG_V7, REG_V15, REG_V23, REG_V31);

#endif

#ifdef ALL_ARM64_EMITTER_UNIT_TESTS

    BasicBlock* label = genCreateTempLabel();
    genDefineTempLabel(label);
    instGen(INS_nop);
    instGen(INS_nop);
    instGen(INS_nop);
    instGen(INS_nop);
    theEmitter->emitIns_R_L(INS_adr, EA_4BYTE_DSP_RELOC, label, REG_R0);

#endif // ALL_ARM64_EMITTER_UNIT_TESTS

    printf("*************** End of genArm64EmitterUnitTests()\n");
}
#endif // defined(DEBUG)

#endif // _TARGET_ARM64_

#endif // !LEGACY_BACKEND
