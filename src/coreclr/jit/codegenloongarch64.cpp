// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) Loongson Technology. All rights reserved.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        LOONGARCH64 Code Generator                         XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_LOONGARCH64
#include "emit.h"
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"

static short splitLow(int value) {
    return (value & 0xffff);
}

// Returns true if 'value' is a legal signed immediate 16 bit encoding.
static bool isValidSimm16(ssize_t value)
{
    return -( ((int)1) << 15 ) <= value && value < ( ((int)1) << 15 );
};

// Returns true if 'value' is a legal unsigned immediate 16 bit encoding.
static bool isValidUimm16(ssize_t value)
{
    return (0 == (value >> 16));
};

// Returns true if 'value' is a legal signed immediate 12 bit encoding.
static bool isValidSimm12(ssize_t value)
{
    return -( ((int)1) << 11 ) <= value && value < ( ((int)1) << 11 );
};

// Returns true if 'value' is a legal unsigned immediate 11 bit encoding.
static bool isValidUimm11(ssize_t value)
{
    return (0 == (value >> 11));
};

// Returns true if 'value' is a legal unsigned immediate 12 bit encoding.
static bool isValidUimm12(ssize_t value)
{
    return (0 == (value >> 12));
};

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
//     li64  regTmp, imm
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
    emitAttr size         = EA_SIZE(attr);

    // reg1 is usually a dest register
    // reg2 is always source register
    assert(tmpReg != reg2); // regTmp can not match any source register

#ifdef DEBUG
    switch (ins)
    {
        case INS_addi_d:

        case INS_st_b:
        case INS_st_h:
        case INS_st_w:
        case INS_fst_s:
        case INS_st_d:
        case INS_fst_d:

        case INS_ld_b:
        case INS_ld_h:
        case INS_ld_w:
        case INS_fld_s:
        case INS_ld_d:
        case INS_fld_d:
            break;

        default:
            assert(!"Unexpected instruction in genInstrWithConstant");
            break;
    }
#endif
    bool immFitsInIns = (-2048 <= imm) && (imm <= 2047);

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
        assert(!EA_IS_RELOC(size));
        GetEmitter()->emitIns_I_la(size, tmpReg, imm);
        regSet.verifyRegUsed(tmpReg);

        // when we are in an unwind code region
        // we record the extra instructions using unwindPadding()
        if (inUnwindRegion)
        {
            compiler->unwindPadding();
        }

        if (ins == INS_addi_d)
        {
            GetEmitter()->emitIns_R_R_R(INS_add_d, attr, reg1, reg2, tmpReg);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R(INS_add_d, attr, tmpReg, reg2, tmpReg);
            GetEmitter()->emitIns_R_R_I(ins, attr, reg1, tmpReg, 0);
        }
    }
    return immFitsInIns;
}

//------------------------------------------------------------------------
// genStackPointerAdjustment: add a specified constant value to the stack pointer in either the prolog
// or the epilog. The unwind codes for the generated instructions are produced. An available temporary
// register is required to be specified, in case the constant is too large to encode in an "daddu"
// instruction (or "dsubu" instruction if we choose to use one), such that we need to load the constant
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
    // Even though INS_addi_d is specified here, the encoder will choose either
    // an INS_add_d or an INS_addi_d and encode the immediate as a positive value
    //
    bool wasTempRegisterUsedForImm =
        !genInstrWithConstant(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta, tmpReg, true);
    if (wasTempRegisterUsedForImm)
    {
        if (pTmpRegIsZero != nullptr)
        {
            *pTmpRegIsZero = false;
        }
    }

    if (reportUnwindData)
    {
        // spDelta is negative in the prolog, positive in the epilog,
        // but we always tell the unwind codes the positive value.
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

    instruction ins = INS_st_d;
    if (genIsValidFloatReg(reg1))
        ins = INS_fst_d;

    if (spDelta != 0)
    {
        // generate addi.d SP,SP,-imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);

        assert((spDelta+spOffset+16)<=0);

        assert(spOffset <= 2031);//2047-16
    }

// #ifdef OPTIMIZE_LOONGSON_EXT
//     if (!(spOffset & 0xf) && (spOffset <= 0xff0) && (INS_st_d == ins))
//     {
//         GetEmitter()->emitIns_R_R_R_I(INS_gssq, EA_PTRSIZE, reg2, reg1, REG_SPBASE, spOffset >> 4);
//         compiler->unwindSaveRegPair(reg1, reg2, spOffset);
//     }
//     else
// #endif
    {
    GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
    compiler->unwindSaveReg(reg1, spOffset);

    GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg2, REG_SPBASE, spOffset+8);
    compiler->unwindSaveReg(reg2, spOffset+8);
    }
}

//------------------------------------------------------------------------
// genPrologSaveReg: Like genPrologSaveRegPair, but for a single register. Save a single general-purpose or
// floating-point/SIMD register in a function or funclet prolog. Note that if we wish to change SP (i.e., spDelta != 0),
// then spOffset must be 8. This is because otherwise we would create an alignment hole above the saved register, not
// below it, which we currently don't support. This restriction could be loosened if the callers change to handle it
// (and this function changes to support using pre-indexed SD addressing). The caller must ensure that we can use the
// SD instruction, and that spOffset will be in the legal range for that instruction.
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

    instruction ins = INS_st_d;
    if (genIsValidFloatReg(reg1))
        ins = INS_fst_d;

    if (spDelta != 0)
    {
        // generate daddiu SP,SP,-imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
    }

    GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
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

    instruction ins = INS_ld_d;
    if (genIsValidFloatReg(reg1))
        ins = INS_fld_d;

    if (spDelta != 0)
    {
        assert(!useSaveNextPair);
// #ifdef OPTIMIZE_LOONGSON_EXT
//         if (!(spOffset & 0xf) && (spOffset <= 0xff0) && (INS_ld_d == ins))
//         {
//             GetEmitter()->emitIns_R_R_R_I(INS_gslq, EA_PTRSIZE, reg2, reg1, REG_SPBASE, spOffset >> 4);
//             compiler->unwindSaveRegPair(reg1, reg2, spOffset);
//         }
//         else
// #endif
        {
        GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg2, REG_SPBASE, spOffset+8);
        compiler->unwindSaveReg(reg2, spOffset+8);

        GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);
        }

        // generate daddiu SP,SP,imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
    }
    else
    {
// #ifdef OPTIMIZE_LOONGSON_EXT
//         if (!(spOffset & 0xf) && (spOffset <= 0xff0) && (INS_ld_d == ins))
//         {
//             GetEmitter()->emitIns_R_R_R_I(INS_gslq, EA_PTRSIZE, reg2, reg1, REG_SPBASE, spOffset >> 4);
//             compiler->unwindSaveRegPair(reg1, reg2, spOffset);
//         }
//         else
// #endif
        {
        GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg2, REG_SPBASE, spOffset+8);
        compiler->unwindSaveReg(reg2, spOffset+8);

        GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);
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

    instruction ins = INS_ld_d;
    if (genIsValidFloatReg(reg1))
        ins = INS_fld_d;

    if (spDelta != 0)
    {
        // ld reg1, offset(SP)
        GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);

        // generate add SP,SP,imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
    }
    else
    {
        GetEmitter()->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
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
        regMaskTP reg1Mask = genFindLowestBit(regsMask);
        regNumber reg1     = genRegNumFromMask(reg1Mask);
        regsMask &= ~reg1Mask;
        regsCount -= 1;

        bool isPairSave = false;
        if (regsCount > 0)
        {
            regMaskTP reg2Mask = genFindLowestBit(regsMask);
            regNumber reg2     = genRegNumFromMask(reg2Mask);
            if (reg2 == REG_NEXT(reg1))
            {
                // The JIT doesn't allow saving pair (S7,FP), even though the
                // save_regp register pair unwind code specification allows it.
                // The JIT always saves (FP,RA) as a pair, and uses the save_fpra
                // unwind code. This only comes up in stress mode scenarios
                // where callee-saved registers are not allocated completely
                // from lowest-to-highest, without gaps.
                if (reg1 != REG_FP)
                {
                    // Both registers must have the same type to be saved as pair.
                    if (genIsValidFloatReg(reg1) == genIsValidFloatReg(reg2))
                    {
                        isPairSave = true;

                        regsMask &= ~reg2Mask;
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
    assert((regsMask & (RBM_CALLEE_SAVED | RBM_FP | RBM_RA)) == regsMask); // Do not expect anything else.

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
            // We can use two SD instructions.
            genPrologSaveRegPair(regPair.reg1, regPair.reg2, spOffset, spDelta, regPair.useSaveNextPair, REG_R21,
                                 nullptr);

            spOffset += 2 * slotSize;
        }
        else
        {
            // No register pair; we use a SD instruction.
            genPrologSaveReg(regPair.reg1, spOffset, spDelta, REG_R21, nullptr);
            spOffset += slotSize;
        }

        spDelta = 0; // We've now changed SP already, if necessary; don't do it again.
    }
}

//------------------------------------------------------------------------
// genSaveCalleeSavedRegistersHelp: Save the callee-saved registers in 'regsToSaveMask' to the stack frame
// in the function or funclet prolog. Registers are saved in register number order from low addresses
// to high addresses. This means that integer registers are saved at lower addresses than floatint-point/SIMD
// registers.
//
// If establishing frame pointer chaining, it must be done after saving the callee-saved registers.
//
// We can only use the instructions that are allowed by the unwind codes. The caller ensures that
// there is enough space on the frame to store these registers, and that the store instructions
// we need to use (SD) are encodable with the stack-pointer immediate offsets we need to use.
//
// The caller can tell us to fold in a stack pointer adjustment, which we will do with the first instruction.
// Note that the stack pointer adjustment must be by a multiple of 16 to preserve the invariant that the
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
// Notes:
//    The save set can not contain FP/RA in which case FP/RA is saved along with the other callee-saved registers.
//
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
            genStackPointerAdjustment(spDelta, REG_R21, nullptr, /* reportUnwindData */ true);
        }
        return;
    }

    assert((spDelta % 16) == 0);

    assert(regsToSaveCount <= genCountBits(RBM_CALLEE_SAVED));

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

            genEpilogRestoreRegPair(regPair.reg1, regPair.reg2, spOffset, stackDelta, regPair.useSaveNextPair, REG_R21,
                                    nullptr);
        }
        else
        {
            spOffset -= slotSize;
            genEpilogRestoreReg(regPair.reg1, spOffset, stackDelta, REG_R21, nullptr);
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
//      ld     s7, 88(sp)
//      ld     s6, 80(sp)
//      ld     s5, 72(sp)
//      ld     s4, 64(sp)
//      ld     s3, 56(sp)
//      ld     s2, 48(sp)
//      ld     s1, 40(sp)
//      ld     s0, 32(sp)
//
// For the case of non-zero spDelta, we assume the base of the callee-save registers to restore is at SP, and
// the last restore adjusts SP by the specified amount. For example:
//      ld     s7, 56(sp)
//      ld     s6, 48(sp)
//      ld     s5, 40(sp)
//      ld     s4, 32(sp)
//      ld     s3, 24(sp)
//      ld     s2, 16(sp)
//      ld     s1, 88(sp)
//      ld     s0, 80(sp)
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
            genStackPointerAdjustment(spDelta, REG_R21, nullptr, /* reportUnwindData */ true);
        }
        return;
    }

    assert((spDelta % 16) == 0);

    // We also can restore FP and RA, even though they are not in RBM_CALLEE_SAVED.
    assert(regsToRestoreCount <= genCountBits(RBM_CALLEE_SAVED | RBM_FP | RBM_RA));

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
 *      catch:          a0 = the exception object that was caught (see GT_CATCH_ARG)
 *      filter:         a0 = the exception object to filter (see GT_CATCH_ARG), a1 = CallerSP of the containing function
 *      finally/fault:  none
 *
 *  Funclets set the following registers on exit:
 *
 *      catch:          v0 = the address at which execution should resume (see BBJ_EHCATCHRET)
 *      filter:         v0 = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *      finally/fault:  none
 *
 *  The LOONGARCH64 funclet prolog sequence is one of the following (Note: #framesz is total funclet frame size,
 *  including everything; #outsz is outgoing argument space. #framesz must be a multiple of 16):
 *
 *  Frame type 1:
 *     For #framesz <= 32760 and FP/RA at bottom:
 *     daddiu sp,sp,-#framesz    ; establish the frame (predecrement by #framesz), save FP/RA
 *     sd fp,#outsz(sp)
 *     sd ra,#outsz+8(sp)
 *     sd s0,#xxx-8(sp)          ; save callee-saved registers, as necessary
 *     sd s1,#xxx(sp)
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned.
 *      |-----------------------|
 *      |      Saved FP, RA     | // 16 bytes
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *  Frame type 2:
 *     For #framesz <= 32760 and FP/RA at top:
 *     daddiu sp,sp,-#framesz          ; establish the frame
 *     sd s0,xxx(sp)                 ; save callee-saved registers, as necessary
 *     sd s1,xxx+8(sp)
 *     sd s?,xxx+?(sp)
 *     sd fp,xxx+?(sp)              ; save FP/RA.
 *     sd ra,xxx+?(sp)
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |      Saved FP, RA     | // 16 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned.
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *  Frame type 3:
 *     For #framesz > 32760 and FP/RA at bottom:
 *     ; for funclet, #framesz-#outsz will be less than 32760.
 *
 *     daddiu sp,sp,-(#framesz-#FPRA_delta)     ; note maybe 16byte-alignment.
 *     sd fp, pad(sp)                           ; pad is depended on stack-16byte-alignment..
 *     sd ra, pad+8(sp)
 *     sd s0,#xxx(sp)                         ; save callee-saved registers, as necessary,
 *     sd s1,#xxx+8(sp)
 *     daddiu sp,sp,-#outsz                     ; create space for outgoing argument space, mabye 16byte-alignment.
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~
 *      |-----------------------|
 *      |      Saved FP, RA     | // 16 bytes
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *  Frame type 4:
 *     For #framesz > 32760 and FP/RA at top:
 *     daddiu sp,sp,-#framesz+PSP_offset  ; establish the frame, maybe 16byte-alignment.
 *     sd s0,xxx(sp)                      ; save callee-saved registers, as necessary
 *     sd s1,xxx+8(sp)
 *     sd s?,xxx+?(sp)
 *     sd fp,xxx+?(sp)              ; save FP/RA.
 *     sd ra,xxx+?(sp)
 *
 *     daddiu sp,sp,-#PSP_offset    ; establish the frame, maybe 16byte-alignment.
 *
 *  The funclet frame is thus:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |  Varargs regs space   | // Only for varargs main functions; 64 bytes
 *      |-----------------------|
 *      |      Saved FP, RA     | // 16 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned.
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 *
 * Both #1 and #2 only change SP once. That means that there will be a maximum of one alignment slot needed. For the general case, #3,
 * it is possible that we will need to add alignment to both changes to SP, leading to 16 bytes of alignment. Remember that the stack
 * pointer needs to be 16 byte aligned at all times. The size of the PSP slot plus callee-saved registers space is a maximum of 224 bytes:
 *
 *     FP,RA registers
 *     8 int callee-saved register s0-s7
 *     8 float callee-saved registers f24-f31
 *     8 saved integer argument registers a0-a7, if varargs function
 *     1 PSP slot
 *     1 alignment slot, future maybe add gp
 *     == 28 slots * 8 bytes = 224 bytes.
 *
 * The outgoing argument size, however, can be very large, if we call a function that takes a large number of
 * arguments (note that we currently use the same outgoing argument space size in the funclet as for the main
 * function, even if the funclet doesn't have any calls, or has a much smaller, or larger, maximum number of
 * outgoing arguments for any call). In that case, we need to 16-byte align the initial change to SP, before
 * saving off the callee-saved registers and establishing the PSPsym, so we can use the limited immediate offset
 * encodings we have available, before doing another 16-byte aligned SP adjustment to create the outgoing argument
 * space. Both changes to SP might need to add alignment padding.
 *
 * In addition to the above "standard" frames, we also need to support a frame where the saved FP/RA are at the
 * highest addresses. This is to match the frame layout (specifically, callee-saved registers including FP/RA
 * and the PSPSym) that is used in the main function when a GS cookie is required due to the use of localloc.
 * (Note that localloc cannot be used in a funclet.) In these variants, not only has the position of FP/RA
 * changed, but where the alignment padding is placed has also changed.
 *
 *
 * Note that in all cases, the PSPSym is in exactly the same position with respect to Caller-SP, and that location is the same relative to Caller-SP
 * as in the main function.
 *
 * Funclets do not have varargs arguments. However, because the PSPSym must exist at the same offset from Caller-SP as in the main function, we
 * must add buffer space for the saved varargs/argument registers here, if the main function did the same.
 *
 *     ; After this header, fill the PSP slot, for use by the VM (it gets reported with the GC info), or by code generation of nested filters.
 *     ; This is not part of the "OS prolog"; it has no associated unwind data, and is not reversed in the funclet epilog.
 *
 *     if (this is a filter funclet)
 *     {
 *          // a1 on entry to a filter funclet is CallerSP of the containing function:
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
 *          ld a1, CallerSP_to_PSP_slot_delta(a1)  ; Load the CallerSP of the main function (stored in the PSP of the dynamically containing funclet or function)
 *          sd a1, SP_to_PSP_slot_delta(sp)        ; store the PSP
 *          daddiu fp, a1, Function_CallerSP_to_FP_delta ; re-establish the frame pointer
 *     }
 *     else
 *     {
 *          // This is NOT a filter funclet. The VM re-establishes the frame pointer on entry.
 *          // TODO-LOONGARCH64-CQ: if VM set x1 to CallerSP on entry, like for filters, we could save an instruction.
 *
 *          daddiu a3, fp, Function_FP_to_CallerSP_delta  ; compute the CallerSP, given the frame pointer. a3 is scratch?
 *          sd a3, SP_to_PSP_slot_delta(sp)         ; store the PSP
 *     }
 *
 *  An example epilog sequence is then:
 *
 *     daddiu sp,sp,#outsz             ; if any outgoing argument space
 *     ...                          ; restore callee-saved registers
 *     ld s0,#xxx-8(sp)
 *     ld s1,#xxx(sp)
 *     ld fp,#framesz-8(sp)
 *     ld ra,#framesz(sp)
 *     daddiu  sp,sp,#framesz
 *     jr  ra
 *
 */
// clang-format on

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
        printf("*************** In genFuncletProlog()\n");
#endif

    assert(block != NULL);
    assert(block->bbFlags & BBF_FUNCLET_BEG);

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    regMaskTP maskSaveRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = genFuncletInfo.fiSaveRegs & ~maskSaveRegsFloat;

    // Funclets must always save RA and FP, since when we have funclets we must have an FP frame.
    assert((maskSaveRegsInt & RBM_RA) != 0);
    assert((maskSaveRegsInt & RBM_FP) != 0);

    bool isFilter = (block->bbCatchTyp == BBCT_FILTER);
    int frameSize  = genFuncletInfo.fiSpDelta1;

    regMaskTP maskArgRegsLiveIn;
    if (isFilter)
    {
        maskArgRegsLiveIn = RBM_A0 | RBM_A1;
    }
    else if ((block->bbCatchTyp == BBCT_FINALLY) || (block->bbCatchTyp == BBCT_FAULT))
    {
        maskArgRegsLiveIn = RBM_NONE;
    }
    else
    {
        maskArgRegsLiveIn = RBM_A0;
    }

#ifdef DEBUG
    if (compiler->opts.disAsm)
        printf("DEBUG: CodeGen::genFuncletProlog, frameType:%d\n\n", genFuncletInfo.fiFrameType);
#endif

    int offset = 0;
    if (genFuncletInfo.fiFrameType == 1)
    {
        // fiFrameType constraints:
        assert(frameSize < 0);
        assert(frameSize >= -2048);

        assert(genFuncletInfo.fiSP_to_FPRA_save_delta < 2040);
        genStackPointerAdjustment(frameSize, REG_R21, nullptr, /* reportUnwindData */ true);

// #ifdef OPTIMIZE_LOONGSON_EXT
//         if (!(genFuncletInfo.fiSP_to_FPRA_save_delta & 0xf) && (genFuncletInfo.fiSP_to_FPRA_save_delta <= 0xff0))
//         {
//             GetEmitter()->emitIns_R_R_R_I(INS_gssq, EA_PTRSIZE, REG_RA, REG_FP, REG_SPBASE, genFuncletInfo.fiSP_to_FPRA_save_delta >> 4);
//             compiler->unwindSaveRegPair(REG_FP, REG_RA, genFuncletInfo.fiSP_to_FPRA_save_delta);
//         }
//         else
// #endif
        {
        GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_FP, REG_SPBASE, genFuncletInfo.fiSP_to_FPRA_save_delta);
        compiler->unwindSaveReg(REG_FP, genFuncletInfo.fiSP_to_FPRA_save_delta);

        GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_RA, REG_SPBASE, genFuncletInfo.fiSP_to_FPRA_save_delta + 8);
        compiler->unwindSaveReg(REG_RA, genFuncletInfo.fiSP_to_FPRA_save_delta + 8);
        }

        maskSaveRegsInt &= ~(RBM_RA | RBM_FP); // We've saved these now

        genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, genFuncletInfo.fiSP_to_PSP_slot_delta + 8, 0);
    }
    else if (genFuncletInfo.fiFrameType == 2)
    {
        // fiFrameType constraints:
        assert(frameSize < 0);
        assert(frameSize >= -2048);

        assert(genFuncletInfo.fiSP_to_FPRA_save_delta < 2040);
        genStackPointerAdjustment(frameSize, REG_R21, nullptr, /* reportUnwindData */ true);

        genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, genFuncletInfo.fiSP_to_PSP_slot_delta + 8, 0);
    }
    else if (genFuncletInfo.fiFrameType == 3)
    {
        // fiFrameType constraints:
        assert(frameSize < -2048);

        offset = -frameSize - genFuncletInfo.fiSP_to_FPRA_save_delta;
        int SP_delta = roundUp((UINT)offset, STACK_ALIGN);
        offset = SP_delta - offset;

        genStackPointerAdjustment(-SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);

// #ifdef OPTIMIZE_LOONGSON_EXT
//         if (!(offset & 0xf) && (offset <= 0xff0))
//         {
//             GetEmitter()->emitIns_R_R_R_I(INS_gssq, EA_PTRSIZE, REG_RA, REG_FP, REG_SPBASE, offset >> 4);
//             compiler->unwindSaveRegPair(REG_FP, REG_RA, offset);
//         }
//         else
// #endif
        {
        GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_FP, REG_SPBASE, offset);
        compiler->unwindSaveReg(REG_FP, offset);

        GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_RA, REG_SPBASE, offset + 8);
        compiler->unwindSaveReg(REG_RA, offset + 8);
        }

        maskSaveRegsInt &= ~(RBM_RA | RBM_FP); // We've saved these now

        offset = frameSize + SP_delta + genFuncletInfo.fiSP_to_PSP_slot_delta + 8;
        genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, offset, 0);

        genStackPointerAdjustment(frameSize + SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);
    }
    else if (genFuncletInfo.fiFrameType == 4)
    {
        // fiFrameType constraints:
        assert(frameSize < -2048);

        offset = -frameSize - (genFuncletInfo.fiSP_to_PSP_slot_delta + 8);
        int SP_delta = roundUp((UINT)offset, STACK_ALIGN);
        offset = SP_delta - offset;

        genStackPointerAdjustment(-SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);

        genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, offset, 0);

        genStackPointerAdjustment(frameSize + SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);
    }
    else
    {
        unreached();
    }

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // If there is no PSPSym (CoreRT ABI), we are done. Otherwise, we need to set up the PSPSym in the functlet frame.
    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        if (isFilter)
        {
            // This is the first block of a filter
            // Note that register a1 = CallerSP of the containing function
            // A1 is overwritten by the first Load (new callerSP)
            // A2 is scratch when we have a large constant offset

            // Load the CallerSP of the main function (stored in the PSP of the dynamically containing funclet or
            // function)
            genInstrWithConstant(INS_ld_d, EA_PTRSIZE, REG_A1, REG_A1, genFuncletInfo.fiCallerSP_to_PSP_slot_delta,
                                 REG_A2, false);
            regSet.verifyRegUsed(REG_A1);

            // Store the PSP value (aka CallerSP)
            genInstrWithConstant(INS_st_d, EA_PTRSIZE, REG_A1, REG_SPBASE, genFuncletInfo.fiSP_to_PSP_slot_delta, REG_A2,
                                 false);

            // re-establish the frame pointer
            genInstrWithConstant(INS_addi_d, EA_PTRSIZE, REG_FPBASE, REG_A1,
                                 genFuncletInfo.fiFunction_CallerSP_to_FP_delta, REG_A2, false);
        }
        else // This is a non-filter funclet
        {
            // A3 is scratch, A2 can also become scratch.

            // compute the CallerSP, given the frame pointer. a3 is scratch?
            genInstrWithConstant(INS_addi_d, EA_PTRSIZE, REG_A3, REG_FPBASE,
                                 -genFuncletInfo.fiFunction_CallerSP_to_FP_delta, REG_A2, false);
            regSet.verifyRegUsed(REG_A3);

            genInstrWithConstant(INS_st_d, EA_PTRSIZE, REG_A3, REG_SPBASE, genFuncletInfo.fiSP_to_PSP_slot_delta, REG_A2,
                                 false);
        }
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
    int frameSize  = genFuncletInfo.fiSpDelta1;

    if (!unwindStarted)
    {
        // We can delay this until we know we'll generate an unwindable instruction, if necessary.
        compiler->unwindBegEpilog();
        unwindStarted = true;
    }

    regMaskTP maskRestoreRegsFloat = genFuncletInfo.fiSaveRegs & RBM_ALLFLOAT;
    regMaskTP maskRestoreRegsInt   = genFuncletInfo.fiSaveRegs & ~maskRestoreRegsFloat;

    // Funclets must always save RA and FP, since when we have funclets we must have an FP frame.
    assert((maskRestoreRegsInt & RBM_RA) != 0);
    assert((maskRestoreRegsInt & RBM_FP) != 0);

#ifdef DEBUG
    if (compiler->opts.disAsm)
        printf("DEBUG: CodeGen::genFuncletEpilog, frameType:%d\n\n", genFuncletInfo.fiFrameType);
#endif

    regMaskTP regsToRestoreMask = maskRestoreRegsInt | maskRestoreRegsFloat;

    assert(frameSize < 0);
    if (genFuncletInfo.fiFrameType == 1)
    {
        // fiFrameType constraints:
        assert(frameSize >= -2048);
        assert(genFuncletInfo.fiSP_to_FPRA_save_delta < 2040);

        regsToRestoreMask &= ~(RBM_RA | RBM_FP); // We restore FP/RA at the end

        genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, genFuncletInfo.fiSP_to_PSP_slot_delta + 8, 0);

// #ifdef OPTIMIZE_LOONGSON_EXT
//         if (!(genFuncletInfo.fiSP_to_FPRA_save_delta & 0xf) && (genFuncletInfo.fiSP_to_FPRA_save_delta <= 0xff0))
//         {
//             GetEmitter()->emitIns_R_R_R_I(INS_gslq, EA_PTRSIZE, REG_RA, REG_FP, REG_SPBASE, genFuncletInfo.fiSP_to_FPRA_save_delta >> 4);
//             compiler->unwindSaveRegPair(REG_FP, REG_RA, genFuncletInfo.fiSP_to_FPRA_save_delta);
//         }
//         else
// #endif
        {
        GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, REG_RA, REG_SPBASE, genFuncletInfo.fiSP_to_FPRA_save_delta + 8);
        compiler->unwindSaveReg(REG_RA, genFuncletInfo.fiSP_to_FPRA_save_delta + 8);

        GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, REG_FP, REG_SPBASE, genFuncletInfo.fiSP_to_FPRA_save_delta);
        compiler->unwindSaveReg(REG_FP, genFuncletInfo.fiSP_to_FPRA_save_delta);
        }

        // generate daddiu SP,SP,imm
        genStackPointerAdjustment(-frameSize, REG_R21, nullptr, /* reportUnwindData */ true);
    }
    else if (genFuncletInfo.fiFrameType == 2)
    {
        // fiFrameType constraints:
        assert(frameSize >= -2048);
        assert(genFuncletInfo.fiSP_to_FPRA_save_delta < 2040);

        genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, genFuncletInfo.fiSP_to_PSP_slot_delta + 8, 0);

        // generate daddiu SP,SP,imm
        genStackPointerAdjustment(-frameSize, REG_R21, nullptr, /* reportUnwindData */ true);
    }
    else if (genFuncletInfo.fiFrameType == 3)
    {
        // fiFrameType constraints:
        assert(frameSize < -2048);


        int offset = -frameSize - genFuncletInfo.fiSP_to_FPRA_save_delta;
        int SP_delta = roundUp((UINT)offset, STACK_ALIGN);
        offset = SP_delta - offset;

        //first, generate daddiu SP,SP,imm
        genStackPointerAdjustment(-frameSize - SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);

        int offset2 = frameSize + SP_delta + genFuncletInfo.fiSP_to_PSP_slot_delta + 8;
        assert(offset2 < 2040);//can amend.

        regsToRestoreMask &= ~(RBM_RA | RBM_FP); // We restore FP/RA at the end
        genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, offset2, 0);

// #ifdef OPTIMIZE_LOONGSON_EXT
//         if (!(offset & 0xf) && (offset <= 0xff0))
//         {
//             GetEmitter()->emitIns_R_R_R_I(INS_gslq, EA_PTRSIZE, REG_RA, REG_FP, REG_SPBASE, offset >> 4);
//             compiler->unwindSaveRegPair(REG_FP, REG_RA, offset);
//         }
//         else
// #endif
        {
        GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, REG_RA, REG_SPBASE, offset + 8);
        compiler->unwindSaveReg(REG_RA, offset + 8);

        GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, REG_FP, REG_SPBASE, offset);
        compiler->unwindSaveReg(REG_FP, offset);
        }
        //second, generate daddiu SP,SP,imm for remaine space.
        genStackPointerAdjustment(SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);
    }
    else if (genFuncletInfo.fiFrameType == 4)
    {
        // fiFrameType constraints:
        assert(frameSize < -2048);

        int offset = -frameSize - (genFuncletInfo.fiSP_to_PSP_slot_delta + 8);
        int SP_delta = roundUp((UINT)offset, STACK_ALIGN);
        offset = SP_delta - offset;

        genStackPointerAdjustment(-frameSize - SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);

        genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, offset, 0);

        genStackPointerAdjustment(SP_delta, REG_R21, nullptr, /* reportUnwindData */ true);
    }
    else
    {
        unreached();
    }
    GetEmitter()->emitIns_R_R_I(INS_jirl, emitActualTypeSize(TYP_I_IMPL), REG_R0, REG_RA, 0);
    compiler->unwindReturn(REG_RA);

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

    genFuncletInfo.fiFunction_CallerSP_to_FP_delta = genCallerSPtoFPdelta();

    regMaskTP rsMaskSaveRegs = regSet.rsMaskCalleeSaved;
    assert((rsMaskSaveRegs & RBM_RA) != 0);
    assert((rsMaskSaveRegs & RBM_FP) != 0);

    unsigned PSPSize = (compiler->lvaPSPSym != BAD_VAR_NUM) ? 8 : 0;

    unsigned saveRegsCount = genCountBits(rsMaskSaveRegs);
    assert((saveRegsCount == compiler->compCalleeRegsPushed) || (saveRegsCount == compiler->compCalleeRegsPushed - 1));

    unsigned saveRegsPlusPSPSize;
    if (!IsSaveFpRaWithAllCalleeSavedRegisters())
        saveRegsPlusPSPSize = roundUp((UINT)genTotalFrameSize(), STACK_ALIGN) - compiler->compLclFrameSize +PSPSize/* -2*8*/;
    else
        saveRegsPlusPSPSize = roundUp((UINT)genTotalFrameSize(), STACK_ALIGN) - compiler->compLclFrameSize +PSPSize;

    if (compiler->info.compIsVarArgs)
    {
        // For varargs we always save all of the integer register arguments
        // so that they are contiguous with the incoming stack arguments.
        saveRegsPlusPSPSize += MAX_REG_ARG * REGSIZE_BYTES;
    }
    unsigned saveRegsPlusPSPSizeAligned = roundUp(saveRegsPlusPSPSize, STACK_ALIGN);

    assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
    unsigned outgoingArgSpaceAligned = roundUp(compiler->lvaOutgoingArgSpaceSize, STACK_ALIGN);

    unsigned maxFuncletFrameSizeAligned = saveRegsPlusPSPSizeAligned + outgoingArgSpaceAligned;
    assert((maxFuncletFrameSizeAligned % STACK_ALIGN) == 0);

    int SP_to_FPRA_save_delta = compiler->lvaOutgoingArgSpaceSize;

    unsigned funcletFrameSize        = saveRegsPlusPSPSize + compiler->lvaOutgoingArgSpaceSize;
    unsigned funcletFrameSizeAligned = roundUp(funcletFrameSize, STACK_ALIGN);
    assert(funcletFrameSizeAligned <= maxFuncletFrameSizeAligned);

    unsigned funcletFrameAlignmentPad = funcletFrameSizeAligned - funcletFrameSize;
    assert((funcletFrameAlignmentPad == 0) || (funcletFrameAlignmentPad == REGSIZE_BYTES));

    if (maxFuncletFrameSizeAligned <= (2048-8))
    {
        if (!IsSaveFpRaWithAllCalleeSavedRegisters())
        {
            genFuncletInfo.fiFrameType = 1;
            saveRegsPlusPSPSize -= 2*8;// FP/RA
        }
        else
        {
            genFuncletInfo.fiFrameType = 2;
            SP_to_FPRA_save_delta += REGSIZE_BYTES + PSPSize;
        }
    }
    else
    {
        unsigned saveRegsPlusPSPAlignmentPad = saveRegsPlusPSPSizeAligned - saveRegsPlusPSPSize;
        assert((saveRegsPlusPSPAlignmentPad == 0) || (saveRegsPlusPSPAlignmentPad == REGSIZE_BYTES));

        if (!IsSaveFpRaWithAllCalleeSavedRegisters())
        {
            genFuncletInfo.fiFrameType = 3;
            saveRegsPlusPSPSize -= 2*8;// FP/RA
        }
        else
        {
            genFuncletInfo.fiFrameType = 4;
            SP_to_FPRA_save_delta += REGSIZE_BYTES + PSPSize;
        }
    }


    int CallerSP_to_PSP_slot_delta = -(int)saveRegsPlusPSPSize;
    genFuncletInfo.fiSpDelta1 = -(int)funcletFrameSizeAligned;
    int SP_to_PSP_slot_delta = funcletFrameSizeAligned - saveRegsPlusPSPSize;

    /* Now save it for future use */
    genFuncletInfo.fiSaveRegs                   = rsMaskSaveRegs;
    genFuncletInfo.fiSP_to_FPRA_save_delta      = SP_to_FPRA_save_delta;

    genFuncletInfo.fiSP_to_PSP_slot_delta       = SP_to_PSP_slot_delta;
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
        printf("  SP to FP/RA save location delta: %d\n", genFuncletInfo.fiSP_to_FPRA_save_delta);
        printf("                       Frame type: %d\n", genFuncletInfo.fiFrameType);
        printf("                       SP delta 1: %d\n", genFuncletInfo.fiSpDelta1);

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

    assert(genFuncletInfo.fiSP_to_FPRA_save_delta >= 0);
#endif // DEBUG
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
    // Generate a call to the finally, like this:
    //      mov  a0,qword ptr [fp + 10H] / sp    // Load a0 with PSPSym, or sp if PSPSym is not used
    //      bl  finally-funclet
    //      b    finally-return                  // Only for non-retless finally calls
    // The 'b' can be a NOP if we're going to the next block.

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        GetEmitter()->emitIns_R_S(INS_ld_d, EA_PTRSIZE, REG_A0, compiler->lvaPSPSym, 0);
    }
    else
    {
        GetEmitter()->emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_A0, REG_SPBASE, 0);
    }
    GetEmitter()->emitIns_J(INS_bl, block->bbJumpDest);

    if (block->bbFlags & BBF_RETLESS_CALL)
    {
        // We have a retless call, and the last instruction generated was a call.
        // If the next block is in a different EH region (or is the end of the code
        // block), then we need to generate a breakpoint here (since it will never
        // get executed) to get proper unwind behavior.

        if ((block->bbNext == nullptr) || !BasicBlock::sameEHRegion(block, block->bbNext))
        {
            instGen(INS_break); // This should never get executed
        }
    }
    else
    {
        // Because of the way the flowgraph is connected, the liveness info for this one instruction
        // after the call is not (can not be) correct in cases where a variable has a last use in the
        // handler.  So turn off GC reporting for this single instruction.
        GetEmitter()->emitDisableGC();

        // Now go to where the finally funclet needs to return to.
        if (block->bbNext->bbJumpDest == block->bbNext->bbNext)
        {
            // Fall-through.
            // TODO-LOONGARCH64-CQ: Can we get rid of this instruction, and just have the call return directly
            // to the next instruction? This would depend on stack walking from within the finally
            // handler working without this instruction being in this special EH region.
            instGen(INS_nop);
        }
        else
        {
            inst_JMP(EJ_jmp, block->bbNext->bbJumpDest);
        }

        GetEmitter()->emitEnableGC();
    }

    // The BBJ_ALWAYS is used because the BBJ_CALLFINALLY can't point to the
    // jump target using bbJumpDest - that is already used to point
    // to the finally block. So just skip past the BBJ_ALWAYS unless the
    // block is RETLESS.
    if (!(block->bbFlags & BBF_RETLESS_CALL))
    {
        assert(block->isBBCallAlwaysPair());
        block = block->bbNext;
    }
    return block;
}

void CodeGen::genEHCatchRet(BasicBlock* block)
{
    GetEmitter()->emitIns_R_L(INS_lea, EA_PTRSIZE, block->bbJumpDest, REG_INTRET);
}

//  move an immediate value into an integer register
void CodeGen::instGen_Set_Reg_To_Imm(emitAttr  size,
                                     regNumber reg,
                                     ssize_t   imm,
                                     insFlags flags DEBUGARG(size_t targetHandle) DEBUGARG(GenTreeFlags gtFlags))
{//maybe optimize.
    emitter* emit = GetEmitter();

    if (!compiler->opts.compReloc)
    {
        size = EA_SIZE(size); // Strip any Reloc flags from size if we aren't doing relocs.
    }

    if (EA_IS_RELOC(size))
    {
        assert(genIsValidIntReg(reg));//TODO: maybe optimize!!!
        emit->emitIns_R_AI(INS_bl, size, reg, imm);//for example: EA_PTR_DSP_RELOC
    }
    else
    {
        emit->emitIns_I_la(size, reg, imm);//TODO: maybe optimize.
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
            // relocatable values tend to come down as a CNS_INT of native int type
            // so the line between these two opcodes is kind of blurry
            GenTreeIntConCommon* con    = tree->AsIntConCommon();
            ssize_t              cnsVal = con->IconValue();

            //if (con->ImmedValNeedsReloc(compiler))
            if (con->ImmedValNeedsReloc(compiler) && compiler->opts.compReloc)
            {
                //instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, targetReg, cnsVal);
                assert(compiler->opts.compReloc);
                GetEmitter()->emitIns_R_AI(INS_bl, EA_HANDLE_CNS_RELOC, targetReg, cnsVal);
                regSet.verifyRegUsed(targetReg);
            }
            else
            {
                genSetRegToIcon(targetReg, cnsVal, targetType);
            }
        }
        break;

        case GT_CNS_DBL:
        {
            emitter* emit       = GetEmitter();
            emitAttr size       = emitActualTypeSize(tree);
            double   constValue = tree->AsDblCon()->gtDconVal;

            // Make sure we use "daddiu reg, zero, 0x00"  only for positive zero (0.0)
            // and not for negative zero (-0.0)
            if (*(__int64*)&constValue == 0)
            {
                // A faster/smaller way to generate 0.0
                // We will just zero out the entire vector register for both float and double
                emit->emitIns_R_R(INS_movgr2fr_d, EA_8BYTE, targetReg, REG_R0);
            }
            /*else if (emitter::emitIns_valid_imm_for_fmov(constValue))
            {// LOONGARCH64 doesn't need this.
                assert(!"unimplemented on LOONGARCH yet");
            }*/
            else
            {
                // Get a temp integer register to compute long address.
                //regNumber addrReg = tree->GetSingleTempReg();

                // We must load the FP constant from the constant pool
                // Emit a data section constant for the float or double constant.
                CORINFO_FIELD_HANDLE hnd = emit->emitFltOrDblConst(constValue, size);

                // Load the FP constant.
                assert(targetReg >= REG_F0);

                instruction ins = size == EA_4BYTE ? INS_fld_s : INS_fld_d;

                // Compute the address of the FP constant and load the data.
                emit->emitIns_R_C(ins, size, targetReg, REG_NA, hnd, 0);
            }
        }
        break;

        default:
            unreached();
    }
}

// Produce code for a GT_INC_SATURATE node.
void CodeGen::genCodeForIncSaturate(GenTree* tree)
{
    assert(!"unimplemented on LOONGARCH yet");
#if 0
    regNumber targetReg = tree->GetRegNum();

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(!tree->isContained());
    // The dst can only be a register.
    assert(targetReg != REG_NA);

    GenTree* operand = tree->gtGetOp1();
    assert(!operand->isContained());
    // The src must be a register.
    regNumber operandReg = genConsumeReg(operand);

    GetEmitter()->emitIns_R_R_I(INS_addi_d, emitActualTypeSize(tree), targetReg, operandReg, 1);
    GetEmitter()->emitIns_R_R_I(INS_bne, emitActualTypeSize(tree), targetReg, REG_R0, 2);
    GetEmitter()->emitIns_R_R_R(INS_andn, emitActualTypeSize(tree), targetReg, targetReg, REG_R0);

    genProduceReg(tree);
#endif
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

    // op1 and op2 can only be a reg at present, will amend in the future.
    assert(!op1->isContained());
    assert(!op2->isContained());

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(targetReg != REG_NA);

    if (EA_SIZE(attr) == EA_8BYTE)
    {
        instruction ins = isUnsigned ? INS_mulh_du : INS_mulh_d;

        emit->emitIns_R_R_R(ins, attr, targetReg, op1->GetRegNum(), op2->GetRegNum());
    }
    else
    {
        assert(EA_SIZE(attr) == EA_4BYTE);
        instruction ins = isUnsigned ? INS_mulh_wu : INS_mulh_w;

        emit->emitIns_R_R_R(ins, attr, targetReg, op1->GetRegNum(), op2->GetRegNum());
    }

    genProduceReg(treeNode);
}

// Generate code for ADD, SUB, MUL, AND, OR and XOR
// This method is expected to have called genConsumeOperands() before calling it.
void CodeGen::genCodeForBinary(GenTreeOp* treeNode)
{
    const genTreeOps oper       = treeNode->OperGet();
    regNumber        targetReg  = treeNode->GetRegNum();
    emitter*         emit       = GetEmitter();

    assert(oper == GT_ADD || oper == GT_SUB || oper == GT_MUL || oper == GT_AND ||
           oper == GT_OR || oper == GT_XOR);

    GenTree*    op1 = treeNode->gtGetOp1();
    GenTree*    op2 = treeNode->gtGetOp2();
    instruction ins = genGetInsForOper(treeNode);

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(targetReg != REG_NA);

    regNumber r = emit->emitInsTernary(ins, emitActualTypeSize(treeNode), treeNode, op1, op2);
    assert(r == targetReg);

    genProduceReg(treeNode);
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
    LclVarDsc* varDsc         = &(compiler->lvaTable[varNum]);
    bool       isRegCandidate = varDsc->lvIsRegCandidate();

    // lcl_vars are not defs
    assert((tree->gtFlags & GTF_VAR_DEF) == 0);

    // If this is a register candidate that has been spilled, genConsumeReg() will
    // reload it at the point of use.  Otherwise, if it's not in a register, we load it here.

    if (!isRegCandidate && !(tree->gtFlags & GTF_SPILLED))
    {
        var_types  targetType = varDsc->GetRegisterType(tree);
        //if (tree->gtFlags & GTF_UNSIGNED)
        //    targetType = varTypeSignedToUnsigned(targetType);//uuuuu.
        emitter*  emit       = GetEmitter();

        // targetType must be a normal scalar type and not a TYP_STRUCT
        assert(targetType != TYP_STRUCT);
        instruction ins  = ins_Load(targetType);
        emitAttr    attr = emitTypeSize(targetType);

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
    regNumber targetReg  = tree->GetRegNum();
    emitter*  emit       = GetEmitter();
    noway_assert(targetType != TYP_STRUCT);

#ifdef FEATURE_SIMD
    // storing of TYP_SIMD12 (i.e. Vector3) field
    if (tree->TypeGet() == TYP_SIMD12)
    {
        genStoreLclTypeSIMD12(tree);
        return;
    }
#endif // FEATURE_SIMD

    // record the offset
    unsigned offset = tree->GetLclOffs();

    // We must have a stack store with GT_STORE_LCL_FLD
    noway_assert(targetReg == REG_NA);

    unsigned varNum = tree->GetLclNum();
    assert(varNum < compiler->lvaCount);
    LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

    // Ensure that lclVar nodes are typed correctly.
    assert(!varDsc->lvNormalizeOnStore() || targetType == genActualType(varDsc->TypeGet()));

    GenTree* data = tree->gtOp1;
    genConsumeRegs(data);

    regNumber dataReg = REG_NA;
    if (data->isContainedIntOrIImmed())
    {
        assert(data->IsIntegralConst(0));
        dataReg = REG_R0;
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

    instruction ins = ins_StoreFromSrc(dataReg, targetType);

    emitAttr attr = emitTypeSize(targetType);

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

    // var = call, where call returns a multi-reg return value
    // case is handled separately.
    if (data->gtSkipReloadOrCopy()->IsMultiRegNode())
    {
        genMultiRegCallStoreToLocal(lclNode);
        return;
    }

    regNumber targetReg  = lclNode->GetRegNum();
    emitter*  emit       = GetEmitter();

    unsigned varNum = lclNode->GetLclNum();
    assert(varNum < compiler->lvaCount);
    LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);
    var_types targetType = varDsc->GetRegisterType(lclNode);

    if (lclNode->IsMultiReg())
    {
        regNumber    operandReg = genConsumeReg(data);
        unsigned int regCount   = varDsc->lvFieldCnt;
        for (unsigned i = 0; i < regCount; ++i)
        {
            assert(!"unimplemented on LOONGARCH yet");
            regNumber varReg = lclNode->GetRegByIndex(i);
            assert(varReg != REG_NA);
            unsigned   fieldLclNum = varDsc->lvFieldLclStart + i;
            LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(fieldLclNum);
            assert(fieldVarDsc->TypeGet() == TYP_FLOAT);
            GetEmitter()->emitIns_R_R_I(INS_st_d, emitTypeSize(TYP_FLOAT), varReg, operandReg, i);
        }
        genProduceReg(lclNode);
    }
    else
    {
#ifdef FEATURE_SIMD
        // storing of TYP_SIMD12 (i.e. Vector3) field
        if (lclNode->TypeGet() == TYP_SIMD12)
        {
            genStoreLclTypeSIMD12(lclNode);
            return;
        }
#endif // FEATURE_SIMD

        genConsumeRegs(data);

        regNumber dataReg = REG_NA;
        if (data->isContained())
        {
            // This is only possible for a zero-init or bitcast.
            const bool zeroInit = data->IsIntegralConst(0);
#if 0
            //TODO: supporting the SIMD on LoongArch64.
            if (zeroInit && varTypeIsSIMD(targetType))
            {
                assert(!varTypeIsSIMD(targetType));
                //assert(targetType == TYP_SIMD8);//TODO:TYP_SIMD16
                assert(targetReg == REG_NA);
                GetEmitter()->emitIns_S_R(INS_st_d, EA_8BYTE, REG_R0, varNum, 0);
                genUpdateLife(lclNode);
                return;
            }
#else
            assert(!varTypeIsSIMD(targetType));
#endif

            if (zeroInit)
            {
                dataReg = REG_R0;
            }
            else if (data->IsIntegralConst())
            {
                ssize_t imm = data->AsIntConCommon()->IconValue();
                emit->emitIns_I_la(EA_PTRSIZE, REG_R21, imm);
                dataReg = REG_R21;
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

        if (targetReg == REG_NA) // store into stack based LclVar
        {
            inst_set_SV_var(lclNode);

            instruction ins  = ins_StoreFromSrc(dataReg, targetType);
            emitAttr    attr = emitActualTypeSize(targetType);

            emit->emitIns_S_R(ins, attr, dataReg, varNum, /* offset */ 0);

            genUpdateLife(lclNode);

            varDsc->SetRegNum(REG_STK);
        }
        else // store into register (i.e move into register)
        {
            if (dataReg != targetReg)
            {
                // Assign into targetReg when dataReg (from op1) is not the same register
                inst_Mov(targetType, targetReg, dataReg, true);
            }
            genProduceReg(lclNode);
        }
    }
}

//------------------------------------------------------------------------
// genSimpleReturn: Generates code for simple return statement for loongarch64.
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
            bool                 isRegCandidate = compiler->lvaTable[lcl->GetLclNum()].lvIsRegCandidate();
            if (isRegCandidate && ((op1->gtFlags & GTF_SPILLED) == 0))
            {
                // We may need to generate a zero-extending mov instruction to load the value from this GT_LCL_VAR

                unsigned   lclNum  = lcl->GetLclNum();
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
        emitAttr attr = emitActualTypeSize(targetType);
        if (varTypeUsesFloatArgReg(treeNode))
        {
            if (attr == EA_4BYTE)
                GetEmitter()->emitIns_R_R(INS_fmov_s, attr, retReg, op1->GetRegNum());
            else
                GetEmitter()->emitIns_R_R(INS_fmov_d, attr, retReg, op1->GetRegNum());
        }
        else
        {
            if (attr == EA_4BYTE) // && op1->OperIs(GT_LCL_VAR) && (emitActualTypeSize(compiler->lvaTable[op1->AsLclVar()->GetLclNum()].lvType) == EA_8BYTE))
            {
                if (treeNode->gtFlags & GTF_UNSIGNED)
                    GetEmitter()->emitIns_R_R_I_I(INS_bstrpick_d, EA_PTRSIZE, retReg, op1->GetRegNum(), 31, 0);
                else
                    GetEmitter()->emitIns_R_R_I(INS_slli_w, attr, retReg, op1->GetRegNum(), 0);
            }
            else
                GetEmitter()->emitIns_R_R_I(INS_ori, attr, retReg, op1->GetRegNum(), 0);
        }
    }
}

/***********************************************************************************************
 *  Generate code for localloc
 */
void CodeGen::genLclHeap(GenTree* tree)
{
    assert(tree->OperGet() == GT_LCLHEAP);
    assert(compiler->compLocallocUsed);

    emitter* emit = GetEmitter();
    GenTree* size = tree->AsOp()->gtOp1;
    noway_assert((genActualType(size->gtType) == TYP_INT) || (genActualType(size->gtType) == TYP_I_IMPL));

    regNumber            targetReg                = tree->GetRegNum();
    regNumber            regCnt                   = REG_NA;
    regNumber            pspSymReg                = REG_NA;
    var_types            type                     = genActualType(size->gtType);
    emitAttr             easz                     = emitTypeSize(type);
    BasicBlock*          endLabel                 = nullptr;//can optimize for loongarch.
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
        emit->emitIns_J_cond_la(INS_beq, endLabel, targetReg, REG_R0);

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
            if (regCnt != targetReg)
            {
                emit->emitIns_R_R_I(INS_ori, easz, regCnt, targetReg, 0);
            }
        }

        // Align to STACK_ALIGN
        // regCnt will be the total number of bytes to localloc
        inst_RV_IV(INS_addi_d, regCnt, (STACK_ALIGN - 1), emitActualTypeSize(type));

        assert(regCnt != REG_R21);
        ssize_t imm2 = ~(STACK_ALIGN - 1);
        emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_R21, REG_R0, imm2);
        emit->emitIns_R_R_R(INS_and, emitActualTypeSize(type), regCnt, regCnt, REG_R21);
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
        unsigned outgoingArgSpaceAligned = roundUp(compiler->lvaOutgoingArgSpaceSize, STACK_ALIGN);
        //assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) == 0); // This must be true for the stack to remain
        //                                                                // aligned
        genInstrWithConstant(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, outgoingArgSpaceAligned,
                             rsGetRsvdReg());
        stackAdjustment += outgoingArgSpaceAligned;
    }

    if (size->IsCnsIntOrI())
    {
        // We should reach here only for non-zero, constant size allocations.
        assert(amount > 0);
        ssize_t imm = -16;

        // For small allocations we will generate up to four stp instructions, to zero 16 to 64 bytes.
        static_assert_no_msg(STACK_ALIGN == (REGSIZE_BYTES * 2));
        assert(amount % (REGSIZE_BYTES * 2) == 0); // stp stores two registers at a time
        size_t stpCount = amount / (REGSIZE_BYTES * 2);
        if (stpCount <= 4)
        {
            imm = -16 * stpCount;
            emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, imm);

            imm = -imm;
            while (stpCount != 0)
            {
                imm -= 8;
                emit->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_R0, REG_SPBASE, imm);
                imm -= 8;
                emit->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_R0, REG_SPBASE, imm);
                stpCount -= 1;
            }

            lastTouchDelta = 0;

            goto ALLOC_DONE;
        }
        else if (!compiler->info.compInitMem && (amount < compiler->eeGetPageSize())) // must be < not <=
        {
            // Since the size is less than a page, simply adjust the SP value.
            // The SP might already be in the guard page, so we must touch it BEFORE
            // the alloc, not after.

            // ld_w r0, 0(SP)
            emit->emitIns_R_R_I(INS_ld_w, EA_4BYTE, REG_R0, REG_SP, 0);

            lastTouchDelta = amount;
            imm = -(ssize_t)amount;
            assert(-8192 <= imm && imm < 0);
            if (-2048 <= imm && imm < 0)
                emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, imm);
            else
            {
                emit->emitIns_R_R_I(INS_srai_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, 3);
                emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, imm >> 3);
                emit->emitIns_R_R_I(INS_slli_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, 3);
            }

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
        genSetRegToIcon(regCnt, amount, ((unsigned int)amount == amount) ? TYP_INT : TYP_LONG);
    }

    if (compiler->info.compInitMem)
    {
        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        // Since we have to zero out the allocated memory AND ensure that the stack pointer is always valid
        // by tickling the pages, we will just push 0's on the stack.
        //
        // Note: regCnt is guaranteed to be even on Amd64 since STACK_ALIGN/TARGET_POINTER_SIZE = 2
        // and localloc size is a multiple of STACK_ALIGN.

        // Loop:
        ssize_t imm = -16;
        emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, imm);

        emit->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_R0, REG_SPBASE, 8);
        emit->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_R0, REG_SPBASE, 0);

        // If not done, loop
        // Note that regCnt is the number of bytes to stack allocate.
        // Therefore we need to subtract 16 from regcnt here.
        assert(genIsValidIntReg(regCnt));

        emit->emitIns_R_R_I(INS_addi_d, emitActualTypeSize(type), regCnt, regCnt, -16);

        assert(imm == (-4 << 2));//goto loop.
        emit->emitIns_R_R_I(INS_bne, EA_PTRSIZE, regCnt, REG_R0, (-4 << 2));

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
        //       sltu     R21, SP, regCnt
        //       sub_d    regCnt, SP, regCnt      // regCnt now holds ultimate SP
        //       masknez  regCnt, regCnt, R21     // Overflow, pick lowest possible value
        //
        //       lu12i_w regTmp, eeGetPageSize()>>12
        //  Loop:
        //       ld_w   r0, 0(SP)               // tickle the page - read from the page
        //       sub_d  R21, SP, regTmp         // decrement SP by eeGetPageSize()
        //       bltu   R21, regCnt, Done
        //       sub_d  SP, SP,regTmp
        //       b     Loop
        //
        //  Done:
        //       mov   SP, regCnt
        //

        // Setup the regTmp
        regNumber regTmp = tree->GetSingleTempReg();

        assert(regCnt != REG_R21);
        emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, REG_R21, REG_SPBASE, regCnt);

        //// dsubu  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        emit->emitIns_R_R_R(INS_sub_d, EA_PTRSIZE, regCnt, REG_SPBASE, regCnt);

        // Overflow, set regCnt to lowest possible value
        emit->emitIns_R_R_R(INS_masknez, EA_PTRSIZE, regCnt, regCnt, REG_R21);

        assert(compiler->eeGetPageSize() == ((compiler->eeGetPageSize()>>12)<<12));
        emit->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, regTmp, compiler->eeGetPageSize()>>12);

        //genDefineTempLabel(loop);

        // tickle the page - Read from the updated SP - this triggers a page fault when on the guard page
        emit->emitIns_R_R_I(INS_ld_w, EA_4BYTE, REG_R0, REG_SPBASE, 0);

        // decrement SP by eeGetPageSize()
        emit->emitIns_R_R_R(INS_sub_d, EA_PTRSIZE, REG_R21, REG_SPBASE, regTmp);

        assert(regTmp != REG_R21);

        ssize_t imm = 3 << 2;//goto done.
        emit->emitIns_R_R_I(INS_bltu, EA_PTRSIZE, REG_R21, regCnt, imm);

        emit->emitIns_R_R_R(INS_sub_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, regTmp);

        imm = -4 << 2;
        // Jump to loop and tickle new stack address
        emit->emitIns_I(INS_b, EA_PTRSIZE, imm);

        // Done with stack tickle loop
        //genDefineTempLabel(done);

        // Now just move the final value to SP
        emit->emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_SPBASE, regCnt, 0);

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
        genInstrWithConstant(INS_addi_d, EA_PTRSIZE, targetReg, REG_SPBASE, (ssize_t)stackAdjustment, tmpReg);
    }
    else // stackAdjustment == 0
    {
        // Move the final value of SP to targetReg
        GetEmitter()->emitIns_R_R_I(INS_ori, EA_PTRSIZE, targetReg, REG_SPBASE, 0);
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
    instruction ins       = genGetInsForOper(tree);

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(!tree->isContained());
    // The dst can only be a register.
    assert(targetReg != REG_NA);

    GenTree* operand = tree->gtGetOp1();
    assert(!operand->isContained());
    // The src must be a register.
    regNumber operandReg = genConsumeReg(operand);

    emitAttr attr = emitActualTypeSize(tree);
    GetEmitter()->emitIns_R_R(ins, attr, targetReg, operandReg);

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
    assert(!"unimpleement on LOONGAARCH64 yet");
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
{//can amend further.
    assert(tree->OperIs(GT_MOD, GT_UMOD, GT_DIV, GT_UDIV));

    var_types targetType = tree->TypeGet();
    emitter*  emit       = GetEmitter();

    genConsumeOperands(tree);

    if (varTypeIsFloating(targetType))
    {
        // Floating point divide never raises an exception
        assert(varTypeIsFloating(tree->gtOp1));
        assert(varTypeIsFloating(tree->gtOp2));
        assert(tree->gtOper == GT_DIV);
        //genCodeForBinary(tree);
        instruction ins = genGetInsForOper(tree);
        emit->emitIns_R_R_R(ins, emitActualTypeSize(targetType), tree->GetRegNum(), tree->gtOp1->GetRegNum(), tree->gtOp2->GetRegNum());
    }
    else // an integer divide operation
    {
        GenTree* divisorOp = tree->gtGetOp2();
        // divisorOp can be immed or reg
        assert(!divisorOp->isContained() || divisorOp->isContainedIntOrIImmed());

        if (divisorOp->IsIntegralConst(0) || divisorOp->GetRegNum() == REG_R0)
        {
            // We unconditionally throw a divide by zero exception
            genJumpToThrowHlpBlk(EJ_jmp, SCK_DIV_BY_ZERO);
        }
        else // the divisor is not the constant zero
        {
            GenTree* src1 = tree->gtOp1;
            unsigned TypeSize = genTypeSize(genActualType(tree->TypeGet()));
            emitAttr size = EA_ATTR(TypeSize);

            assert(TypeSize >= genTypeSize(genActualType(src1->TypeGet()))
                && TypeSize >= genTypeSize(genActualType(divisorOp->TypeGet())));

            //ssize_t intConstValue = divisorOp->AsIntCon()->gtIconVal;
            regNumber Reg1 = src1->GetRegNum();
            regNumber divisorReg = divisorOp->GetRegNum();
            instruction ins;

            // Check divisorOp first as we can always allow it to be a contained immediate
            if (divisorOp->isContainedIntOrIImmed())
            {
                ssize_t intConst = (int)(divisorOp->AsIntCon()->gtIconVal);
                divisorReg = REG_R21;
                if ((-2048 <= intConst) && (intConst <= 0x7ff))
                    emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_R21, REG_R0, (short)intConst);
                else
                {
                    emit->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, REG_R21, intConst >> 12);
                    emit->emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_R21, REG_R21, intConst & 0xfff);
                }
            }
            // Only for commutative operations do we check src1 and allow it to be a contained immediate
            else if (tree->OperIsCommutative())
            {
                // src1 can be immed or reg
                assert(!src1->isContained() || src1->isContainedIntOrIImmed());

                // Check src1 and allow it to be a contained immediate
                if (src1->isContainedIntOrIImmed())
                {
                    assert(!divisorOp->isContainedIntOrIImmed());
                    ssize_t intConst = (int)(src1->AsIntCon()->gtIconVal);
                    Reg1 = REG_R21;
                    if ((-2048 <= intConst) && (intConst <= 0x7ff))
                        emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_R21, REG_R0, (short)intConst);
                    else
                    {
                        emit->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, REG_R21, intConst >> 12);
                        emit->emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_R21, REG_R21, intConst & 0xfff);
                    }
                }
            }
            else
            {
                // src1 can only be a reg
                assert(!src1->isContained());
            }

            // Generate the require runtime checks for GT_DIV or GT_UDIV
            if (tree->gtOper == GT_DIV || tree->gtOper == GT_MOD)
            {
                // Two possible exceptions:
                //     (AnyVal /  0) => DivideByZeroException
                //     (MinInt / -1) => ArithmeticException
                //
                bool checkDividend = true;

                // Do we have an immediate for the 'divisorOp'?
                //
                if (divisorOp->IsCnsIntOrI())
                {
                    ssize_t intConstValue = divisorOp->AsIntCon()->gtIconVal;
                    //assert(intConstValue != 0); // already checked above by IsIntegralConst(0)
                    if (intConstValue != -1)
                    {
                        checkDividend = false; // We statically know that the dividend is not -1
                    }
                }
                else // insert check for divison by zero
                {
                    // Check if the divisor is zero throw a DivideByZeroException
                    genJumpToThrowHlpBlk_la(SCK_DIV_BY_ZERO, INS_beq, divisorReg);
                }

                if (checkDividend)
                {
                    // Check if the divisor is not -1 branch to 'sdivLabel'
                    emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_R21, REG_R0, -1);
                    BasicBlock* sdivLabel = genCreateTempLabel();//can optimize for loongarch64.
                    emit->emitIns_J_cond_la(INS_bne, sdivLabel, REG_R21, divisorReg);

                    // If control flow continues past here the 'divisorReg' is known to be -1
                    regNumber dividendReg = tree->gtGetOp1()->GetRegNum();
                    // At this point the divisor is known to be -1
                    //
                    // Wether dividendReg is MinInt or not
                    //

                    emit->emitIns_J_cond_la(INS_beq, sdivLabel, dividendReg, REG_R0);

                    emit->emitIns_R_R_R(size == EA_4BYTE ? INS_add_w : INS_add_d, size, REG_R21, dividendReg, dividendReg);
                    genJumpToThrowHlpBlk_la(SCK_ARITH_EXCPN, INS_beq, REG_R21);
                    genDefineTempLabel(sdivLabel);
                }

                // Generate the sdiv instruction
                if (size == EA_4BYTE)
                {
                    if (tree->OperGet() == GT_DIV)
                    {
                        ins = INS_div_w;
                    }
                    else
                        ins = INS_mod_w;
                }
                else
                {
                    if (tree->OperGet() == GT_DIV)
                    {
                        ins = INS_div_d;
                    }
                    else
                        ins = INS_mod_d;
                }

                emit->emitIns_R_R_R(ins, size, tree->GetRegNum(), Reg1, divisorReg);
            }
            else //if (tree->gtOper == GT_UDIV) GT_UMOD
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
                    genJumpToThrowHlpBlk_la(SCK_DIV_BY_ZERO, INS_beq, divisorReg);
                }

                if (size == EA_4BYTE)
                {
                    if (tree->OperGet() == GT_UDIV)
                    {
                        ins = INS_div_wu;
                    }
                    else
                        ins = INS_mod_wu;

                    //TODO: temp workround, should amend for optimize.
                    emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, Reg1, Reg1, 0);
                    emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, divisorReg, divisorReg, 0);
                }
                else
                {
                    if (tree->OperGet() == GT_UDIV)
                    {
                        ins = INS_div_du;
                    }
                    else
                        ins = INS_mod_du;
                }

                emit->emitIns_R_R_R(ins, size, tree->GetRegNum(), Reg1, divisorReg);
            }
        }
    }
    genProduceReg(tree);
}

// Generate code for InitBlk by performing a loop unroll
// Preconditions:
//   a) Both the size and fill byte value are integer constants.
//   b) The size of the struct to initialize is smaller than INITBLK_UNROLL_LIMIT bytes.
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
        assert(dstAddr->OperIsLocalAddr());
        dstLclNum = dstAddr->AsLclVarCommon()->GetLclNum();
        dstOffset = dstAddr->AsLclVarCommon()->GetLclOffs();
    }

    regNumber srcReg;
    GenTree*  src = node->Data();

    if (src->OperIs(GT_INIT_VAL))
    {
        assert(src->isContained());
        src = src->gtGetOp1();
    }

    if (!src->isContained())
    {
        srcReg = genConsumeReg(src);
    }
    else
    {
        assert(src->IsIntegralConst(0));
        srcReg = REG_R0;
    }

    if (node->IsVolatile())
    {
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();
    unsigned size = node->GetLayout()->GetSize();

    assert(size <= INT32_MAX);
    assert(dstOffset < INT32_MAX - static_cast<int>(size));

    for (unsigned regSize = 2 * REGSIZE_BYTES; size >= regSize; size -= regSize, dstOffset += regSize)
    {
        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(INS_st_d, EA_8BYTE, srcReg, dstLclNum, dstOffset);
            emit->emitIns_S_R(INS_st_d, EA_8BYTE, srcReg, dstLclNum, dstOffset + 8);
        }
        else
        {
            emit->emitIns_R_R_I(INS_st_d, EA_8BYTE, srcReg, dstAddrBaseReg, dstOffset);
            emit->emitIns_R_R_I(INS_st_d, EA_8BYTE, srcReg, dstAddrBaseReg, dstOffset + 8);
        }
    }

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
                storeIns = INS_st_b;
                attr     = EA_4BYTE;
                break;
            case 2:
                storeIns = INS_st_h;
                attr     = EA_4BYTE;
                break;
            case 4:
                storeIns = INS_st_w;
                attr     = EA_ATTR(regSize);
                break;
            case 8:
                storeIns = INS_st_d;
                attr     = EA_ATTR(regSize);
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

// Generate code for CpObj nodes wich copy structs that have interleaved
// GC pointers.
// For this case we'll generate a sequence of loads/stores in the case of struct
// slots that don't contain GC pointers.  The generated code will look like:
// ld tempReg, 8(A5)
// sd tempReg, 8(A6)
//
// In the case of a GC-Pointer we'll call the ByRef write barrier helper
// who happens to use the same registers as the previous call to maintain
// the same register requirements and register killsets:
// bl CORINFO_HELP_ASSIGN_BYREF
//
// So finally an example would look like this:
// ld tempReg, 8(A5)
// sd tempReg, 8(A6)
// bl CORINFO_HELP_ASSIGN_BYREF
// ld tempReg, 8(A5)
// sd tempReg, 8(A6)
// bl CORINFO_HELP_ASSIGN_BYREF
// ld tempReg, 8(A5)
// sd tempReg, 8(A6)
void CodeGen::genCodeForCpObj(GenTreeObj* cpObjNode)
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

    bool dstOnStack = dstAddr->gtSkipReloadOrCopy()->OperIsLocalAddr();

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

    if (cpObjNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile CpObj operation
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();

    emitAttr attrSrcAddr = emitActualTypeSize(srcAddrType);
    emitAttr attrDstAddr = emitActualTypeSize(dstAddr->TypeGet());

    // If we can prove it's on the stack we don't need to use the write barrier.
    if (dstOnStack)
    {
        unsigned i = 0;
        // Check if two or more remaining slots and use two ld/sd sequence
        while (i < slots - 1)
        {
            emitAttr attr0 = emitTypeSize(layout->GetGCPtrType(i + 0));
            emitAttr attr1 = emitTypeSize(layout->GetGCPtrType(i + 1));

            emit->emitIns_R_R_I(INS_ld_d, attr0, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
            emit->emitIns_R_R_I(INS_ld_d, attr1, tmpReg2, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_addi_d, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF, 2 * TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_st_d, attr0, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
            emit->emitIns_R_R_I(INS_st_d, attr1, tmpReg2, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_addi_d, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF, 2 * TARGET_POINTER_SIZE);
            i += 2;
        }

        // Use a ld/sd sequence for the last remainder
        if (i < slots)
        {
            emitAttr attr0 = emitTypeSize(layout->GetGCPtrType(i + 0));

            emit->emitIns_R_R_I(INS_ld_d, attr0, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
            emit->emitIns_R_R_I(INS_addi_d, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_st_d, attr0, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
            emit->emitIns_R_R_I(INS_addi_d, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE);
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
                // Check if the next slot's type is also TYP_GC_NONE and use two ld/sd
                if ((i + 1 < slots) && !layout->IsGCPtr(i + 1))
                {
                    emit->emitIns_R_R_I(INS_ld_d, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
                    emit->emitIns_R_R_I(INS_ld_d, EA_8BYTE, tmpReg2, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_addi_d, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF, 2 * TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_st_d, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
                    emit->emitIns_R_R_I(INS_st_d, EA_8BYTE, tmpReg2, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_addi_d, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF, 2 * TARGET_POINTER_SIZE);
                    ++i; // extra increment of i, since we are copying two items
                }
                else
                {
                    emit->emitIns_R_R_I(INS_ld_d, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
                    emit->emitIns_R_R_I(INS_addi_d, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_st_d, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
                    emit->emitIns_R_R_I(INS_addi_d, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE);
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

    if (cpObjNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a INS_BARRIER_RMB after a volatile CpObj operation
        ////TODO: there is only BARRIER_FULL for LOONGARCH64.
        instGen_MemoryBarrier(BARRIER_FULL);
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
    GetEmitter()->emitIns_R_R_I(INS_slli_d, EA_8BYTE, REG_R21, idxReg, 2);
    GetEmitter()->emitIns_R_R_R(INS_add_d, EA_8BYTE, baseReg, baseReg, REG_R21);
    GetEmitter()->emitIns_R_R_I(INS_ld_w, EA_4BYTE, baseReg, baseReg, 0);

    // add it to the absolute address of fgFirstBB
    //compiler->fgFirstBB->bbFlags |= BBF_JMP_TARGET;//TODO for LOONGARCH64.
    GetEmitter()->emitIns_R_L(INS_lea, EA_PTRSIZE, compiler->fgFirstBB, tmpReg);
    GetEmitter()->emitIns_R_R_R(INS_add_d, EA_PTRSIZE, baseReg, baseReg, tmpReg);

    // jr baseReg
    GetEmitter()->emitIns_R_R_I(INS_jirl, emitActualTypeSize(TYP_I_IMPL), REG_R0, baseReg, 0);
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

    jmpTabBase = GetEmitter()->emitBBTableDataGenBeg(jumpCount, true);

    jmpTabOffs = 0;

    JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", compiler->compMethodID, jmpTabBase);

    for (unsigned i = 0; i < jumpCount; i++)
    {
        BasicBlock* target = *jumpTable++;
        noway_assert(target->bbFlags & BBF_HAS_LABEL);

        JITDUMP("            DD      L_M%03u_" FMT_BB "\n", compiler->compMethodID, target->bbNum);

        GetEmitter()->emitDataGenData(i, target);
    };

    GetEmitter()->emitDataGenEnd();

    // Access to inline data is 'abstracted' by a special type of static member
    // (produced by eeFindJitDataOffs) which the emitter recognizes as being a reference
    // to constant data, not a real static field.
    GetEmitter()->emitIns_R_C(INS_bl, emitActualTypeSize(TYP_I_IMPL), treeNode->GetRegNum(), REG_NA,
                              compiler->eeFindJitDataOffs(jmpTabBase), 0);
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
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genCodeForCmpXchg: Produce code for a GT_CMPXCHG node.
//
// Arguments:
//    tree - the GT_CMPXCHG node
//
void CodeGen::genCodeForCmpXchg(GenTreeCmpXchg* treeNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

static inline bool isImmed(GenTree* treeNode)
{
        if (treeNode->gtGetOp1()->isContainedIntOrIImmed())
        {
            return true;
        }
        else if (treeNode->OperIsBinary())
        {
            if (treeNode->gtGetOp2()->isContainedIntOrIImmed())
                return true;
        }

        return false;
}

instruction CodeGen::genGetInsForOper(GenTree* treeNode)
{
    var_types  type = treeNode->TypeGet();
    genTreeOps oper = treeNode->OperGet();
    GenTree*   op1  = treeNode->gtGetOp1();
    GenTree*   op2;
    emitAttr   attr = emitActualTypeSize(treeNode);
    bool isImm = false;

    instruction ins = INS_break;

    if (varTypeIsFloating(type))
    {
        switch (oper)
        {
            case GT_ADD:
                if (attr == EA_4BYTE)
                    ins = INS_fadd_s;
                else
                    ins = INS_fadd_d;
                break;
            case GT_SUB:
                if (attr == EA_4BYTE)
                    ins = INS_fsub_s;
                else
                    ins = INS_fsub_d;
                break;
            case GT_MUL:
                if (attr == EA_4BYTE)
                    ins = INS_fmul_s;
                else
                    ins = INS_fmul_d;
                break;
            case GT_DIV:
                if (attr == EA_4BYTE)
                    ins = INS_fdiv_s;
                else
                    ins = INS_fdiv_d;
                break;
            case GT_NEG:
                if (attr == EA_4BYTE)
                    ins = INS_fneg_s;
                else
                    ins = INS_fneg_d;
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
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                    {
                        ins = INS_addi_d;
                    }
                    else
                    {
                        assert(attr == EA_4BYTE);
                        ins = INS_addi_w;
                    }
                }
                else
                {
                    if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                    {
                        ins = INS_add_d;
                    }
                    else
                    {
                        assert(attr == EA_4BYTE);
                        ins = INS_add_w;
                    }
                }
                break;

            case GT_SUB:
                isImm = isImmed(treeNode);
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_sub_d;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_sub_w;
                }
                break;

            case GT_MOD:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_mod_d;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_mod_w;
                }
                break;

            case GT_DIV:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_div_d;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_div_w;
                }
                break;

            case GT_UMOD:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_mod_du;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_mod_wu;
                }
                break;

            case GT_UDIV:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_div_du;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_div_wu;
                }
                break;

            case GT_MUL:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    //if ((treeNode->gtFlags & GTF_UNSIGNED) != 0)
                    //    ins = INS_mul_d;
                    //else
                        ins = INS_mul_d;
                }
                else
                {
                    if ((treeNode->gtFlags & GTF_UNSIGNED) != 0)
                        ins = INS_mulw_d_wu;
                    else
                        ins = INS_mul_w;
                }
                break;

            case GT_NEG:
                if (attr == EA_8BYTE)
                {
                    ins = INS_dneg;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_neg;
                }
                break;

            case GT_NOT:
                ins = INS_not;
                break;

            case GT_AND:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    ins = INS_andi;
                }
                else
                {
                    ins = INS_and;
                }
                break;

            case GT_OR:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    ins = INS_ori;
                }
                else
                {
                    ins = INS_or;
                }
                break;

            case GT_LSH:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    //it's better to check sa.
                    if (attr == EA_4BYTE)
                        ins = INS_slli_w;
                    else
                        ins = INS_slli_d;
                }
                else
                {
                    if (attr == EA_4BYTE)
                        ins = INS_sll_w;
                    else
                        ins = INS_sll_d;
                }
                break;

            case GT_RSZ:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    //it's better to check sa.
                    if (attr == EA_4BYTE)
                        ins = INS_srli_w;
                    else
                        ins = INS_srli_d;
                }
                else
                {
                    if (attr == EA_4BYTE)
                        ins = INS_srl_w;
                    else
                        ins = INS_srl_d;
                }
                break;

            case GT_RSH:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    //it's better to check sa.
                    if (attr == EA_4BYTE)
                        ins = INS_srai_w;
                    else
                        ins = INS_srai_d;
                }
                else
                {
                    if (attr == EA_4BYTE)
                        ins = INS_sra_w;
                    else
                        ins = INS_sra_d;
                }
                break;

            case GT_ROR:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    //it's better to check sa.
                    if (attr == EA_4BYTE)
                        ins = INS_rotri_w;
                    else
                        ins = INS_rotri_d;
                }
                else
                {
                    if (attr == EA_4BYTE)
                        ins = INS_rotr_w;
                    else
                        ins = INS_rotr_d;
                }
                break;

            case GT_XOR:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    ins = INS_xori;
                }
                else
                {
                    ins = INS_xor;
                }
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

    BasicBlock* skipLabel = genCreateTempLabel();
    GetEmitter()->emitIns_J_cond_la(INS_beq, skipLabel, data->GetRegNum(), REG_R0);

    void* pAddr = nullptr;
    void* addr = compiler->compGetHelperFtn(CORINFO_HELP_STOP_FOR_GC, &pAddr);
    emitter::EmitCallType callType;
    regNumber callTarget;

    if (addr == nullptr)
    {
        callType = emitter::EC_INDIR_R;
        callTarget = REG_DEFAULT_HELPER_CALL_TARGET;

        //instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
        if (compiler->opts.compReloc)
        {
            GetEmitter()->emitIns_R_AI(INS_bl, EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
        }
        else
        {
            //GetEmitter()->emitIns_R_I(INS_pcaddu12i, EA_PTRSIZE, callTarget, (ssize_t)pAddr);
            //GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, callTarget, callTarget, );
            GetEmitter()->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, callTarget, ((ssize_t)pAddr & 0xfffff000)>>12);
            GetEmitter()->emitIns_R_I(INS_lu32i_d, EA_PTRSIZE, callTarget, (ssize_t)pAddr >> 32);
            GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, callTarget, callTarget, ((ssize_t)pAddr & 0xfff)>>2);
        }
        regSet.verifyRegUsed(callTarget);
        //GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, callTarget, callTarget, 0);
    }
    else
    {
        callType = emitter::EC_FUNC_TOKEN;
        callTarget = REG_NA;
    }

    ////TODO: can optimize further !!!
    GetEmitter()->emitIns_Call(callType, compiler->eeFindHelper(CORINFO_HELP_STOP_FOR_GC), INDEBUG_LDISASM_COMMA(nullptr) addr, 0,
                               EA_UNKNOWN, EA_UNKNOWN, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, DebugInfo(), /* IL offset */
                               callTarget,                             /* ireg */
                               REG_NA, 0, 0,                           /* xreg, xmul, disp */
                               false                                   /* isJump */
                               );

    genDefineTempLabel(skipLabel);

    regMaskTP killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
    regSet.verifyRegistersUsed(killMask);
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
        genStoreIndTypeSIMD12(tree);
        return;
    }
#endif // FEATURE_SIMD

    GenTree* data = tree->Data();
    GenTree* addr = tree->Addr();

    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(tree, data);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        // data and addr must be in registers.
        // Consume both registers so that any copies of interfering
        // registers are taken care of.
        genConsumeOperands(tree);

        // At this point, we should not have any interference.
        // That is, 'data' must not be in REG_WRITE_BARRIER_DST_BYREF,
        //  as that is where 'addr' must go.
        noway_assert(data->GetRegNum() != REG_WRITE_BARRIER_DST_BYREF);

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
            dataReg = REG_R0;
        }
        else // data is not contained, so evaluate it into a register
        {
            assert(!data->isContained());
            dataReg = data->GetRegNum();
        }

        var_types   type = tree->TypeGet();
        instruction ins  = ins_Store(type);

        if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
        {
            // issue a full memory barrier before a volatile StInd
            instGen_MemoryBarrier();
        }

        GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), dataReg, tree);
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
    assert(!"unimplemented on LOONGARCH yet");
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
    assert(!op1->isContained());             // Cannot be contained
    assert(genIsValidIntReg(op1->GetRegNum())); // Must be a valid int reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = genActualType(op1->TypeGet());
    assert(!varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    emitter *emit = GetEmitter();
    emitAttr attr = emitActualTypeSize(dstType);

    // We should never see a srcType whose size is neither EA_4BYTE or EA_8BYTE
    emitAttr srcSize = EA_ATTR(genTypeSize(srcType));
    noway_assert((srcSize == EA_4BYTE) || (srcSize == EA_8BYTE));

    bool IsUnsigned = treeNode->gtFlags & GTF_UNSIGNED;
    instruction ins = INS_invalid;

    genConsumeOperands(treeNode->AsOp());

    if (IsUnsigned)
    {//should amend.
        emit->emitIns_R_R(INS_movgr2fr_d, EA_8BYTE, REG_SCRATCH_FLT, op1->GetRegNum()); // save op1

        if (srcSize == EA_8BYTE)
        {
            ssize_t imm = 4 << 2;
            emit->emitIns_R_R_I(INS_bge, EA_8BYTE, op1->GetRegNum(), REG_R0, imm);

            emit->emitIns_R_R_I(INS_andi, EA_8BYTE, REG_R21, op1->GetRegNum(), 1);
            emit->emitIns_R_R_I(INS_srli_d, EA_8BYTE, op1->GetRegNum(), op1->GetRegNum(), 1);
            emit->emitIns_R_R_R(INS_or, EA_8BYTE, op1->GetRegNum(), op1->GetRegNum(), REG_R21);
        }
        else
        {
            srcSize = EA_8BYTE;
            emit->emitIns_R_R_I_I(INS_bstrins_d, EA_8BYTE, op1->GetRegNum(), REG_R0, 63, 32);
        }
    }

    ins = srcSize == EA_8BYTE ? INS_movgr2fr_d : INS_movgr2fr_w;
    emit->emitIns_R_R(ins, attr, treeNode->GetRegNum(), op1->GetRegNum());

    if (dstType == TYP_DOUBLE)
    {
        if (srcSize == EA_4BYTE)
        {
            ins = INS_ffint_d_w;
        }
        else
        {
            assert(srcSize == EA_8BYTE);
            ins = INS_ffint_d_l;
        }
    }
    else
    {
        assert(dstType == TYP_FLOAT);
        if (srcSize == EA_4BYTE)
        {
            ins = INS_ffint_s_w;
        }
        else
        {
            assert(srcSize == EA_8BYTE);
            ins = INS_ffint_s_l;
        }
    }

    emit->emitIns_R_R(ins, attr, treeNode->GetRegNum(), treeNode->GetRegNum());

    if (IsUnsigned)
    {
        srcSize = EA_ATTR(genTypeSize(srcType));
        emit->emitIns_R_R(INS_movfr2gr_d, attr, op1->GetRegNum(), REG_SCRATCH_FLT); // recover op1

        if (srcSize == EA_8BYTE)
        {
            ssize_t imm = 3 << 2;
            emit->emitIns_R_R_I(INS_bge, EA_8BYTE, op1->GetRegNum(), REG_R0, imm);

            emit->emitIns_R_R(dstType == TYP_DOUBLE ? INS_fmov_d : INS_fmov_s, attr, REG_SCRATCH_FLT, treeNode->GetRegNum());
            emit->emitIns_R_R_R(dstType == TYP_DOUBLE ? INS_fadd_d : INS_fadd_s, attr, treeNode->GetRegNum(), REG_SCRATCH_FLT, treeNode->GetRegNum());
        }
    }

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
    assert(!op1->isContained());               // Cannot be contained
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

    instruction ins1 = INS_invalid;
    instruction ins2 = INS_invalid;
    bool IsUnsigned = varTypeIsUnsigned(dstType);

    regNumber tmpReg = REG_SCRATCH_FLT;
    assert(tmpReg != op1->GetRegNum());

    if (srcType == TYP_DOUBLE)
    {
        if (dstSize == EA_4BYTE)
        {
            ins1 = INS_ftintrz_w_d;
            ins2 = INS_movfr2gr_s;
        }
        else
        {
            assert(dstSize == EA_8BYTE);
            ins1 = INS_ftintrz_l_d;
            ins2 = INS_movfr2gr_d;
        }
    }
    else
    {
        assert(srcType == TYP_FLOAT);
        if (dstSize == EA_4BYTE)
        {
            ins1 = INS_ftintrz_w_s;
            ins2 = INS_movfr2gr_s;
        }
        else
        {
            assert(dstSize == EA_8BYTE);
            ins1 = INS_ftintrz_l_s;
            ins2 = INS_movfr2gr_d;
        }
    }

    genConsumeOperands(treeNode->AsOp());

    if (IsUnsigned)
    {
        ssize_t imm = 0;

        if (srcType == TYP_DOUBLE)
        {
            if (dstSize == EA_4BYTE)
            {
                imm = 0x41e00;
            }
            else
            {
                imm = 0x43e00;
            }
        }
        else
        {
            assert(srcType == TYP_FLOAT);
            if (dstSize == EA_4BYTE)
            {
                imm = 0x4f000;
            }
            else
            {
                imm = 0x5f000;
            }
        }

        //{
        //    GetEmitter()->emitIns_R_R(INS_movgr2fr_d, EA_8BYTE, tmpReg, REG_R0);

        //    GetEmitter()->emitIns_R_R_I(srcType == TYP_DOUBLE ? INS_c_olt_d : INS_c_olt_s, EA_8BYTE, op1->GetRegNum(), tmpReg, 2);
        //    GetEmitter()->emitIns_I_I(INS_bc1f, EA_PTRSIZE, 2, 4 << 2);

        //    GetEmitter()->emitIns_R_R_I(INS_ori*/, EA_PTRSIZE, treeNode->GetRegNum(), REG_R0, 0);
        //    GetEmitter()->emitIns_I(INS_b, EA_PTRSIZE, srcType == TYP_DOUBLE ? 14 << 2 : 13 << 2);
        //}

        if (srcType == TYP_DOUBLE)
            GetEmitter()->emitIns_R_R_I(INS_lu52i_d, EA_8BYTE, REG_R21, REG_R0, imm >> 8);
        else
            GetEmitter()->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, REG_R21, imm);

        GetEmitter()->emitIns_R_R(srcType == TYP_DOUBLE ? INS_movgr2fr_d : INS_movgr2fr_w, EA_8BYTE, tmpReg, REG_R21);

        GetEmitter()->emitIns_R_R_I(srcType == TYP_DOUBLE ? INS_fcmp_clt_d : INS_fcmp_clt_s, EA_8BYTE, op1->GetRegNum(), tmpReg, 2);

        GetEmitter()->emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_R21, REG_R0, 0);
        GetEmitter()->emitIns_I_I(INS_bcnez, EA_PTRSIZE, 2, 4 << 2);

        GetEmitter()->emitIns_R_R_R(srcType == TYP_DOUBLE ? INS_fsub_d : INS_fsub_s, EA_8BYTE, tmpReg, op1->GetRegNum(), tmpReg);

        GetEmitter()->emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_R21, REG_R0, 1);
        GetEmitter()->emitIns_R_R_I(dstSize == EA_8BYTE ? INS_slli_d : INS_slli_w, EA_PTRSIZE, REG_R21, REG_R21, dstSize == EA_8BYTE ? 63 : 31);

        GetEmitter()->emitIns_R_R_R_I(INS_fsel, EA_PTRSIZE, tmpReg, tmpReg, op1->GetRegNum(), 2);

        GetEmitter()->emitIns_R_R(ins1, dstSize, tmpReg, tmpReg);
        GetEmitter()->emitIns_R_R(ins2, dstSize, treeNode->GetRegNum(), tmpReg);

        GetEmitter()->emitIns_R_R_R(INS_or, dstSize, treeNode->GetRegNum(), REG_R21, treeNode->GetRegNum());
    }
    else
    {
        GetEmitter()->emitIns_R_R(ins1, dstSize, tmpReg, op1->GetRegNum());
        GetEmitter()->emitIns_R_R(ins2, dstSize, treeNode->GetRegNum(), tmpReg);
    }

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
    ssize_t   expMask     = (targetType == TYP_FLOAT) ? 0xFF : 0x7FF; // Bit mask to extract exponent.
    ssize_t   size        = (targetType == TYP_FLOAT) ? 8 : 11;  // Bit size to extract exponent.
    ssize_t   pos         = (targetType == TYP_FLOAT) ? 23 : 52; // Bit pos of exponent.

    emitter* emit = GetEmitter();
    emitAttr attr = emitActualTypeSize(treeNode);

    // Extract exponent into a register.
    regNumber intReg = treeNode->GetSingleTempReg();
    regNumber fpReg  = genConsumeReg(op1);

    emit->emitIns_R_R(attr == EA_8BYTE ? INS_movfr2gr_d : INS_movfr2gr_s, attr, intReg, fpReg);

    // Mask of exponent with all 1's and check if the exponent is all 1's
    instruction ins = (targetType == TYP_FLOAT) ? INS_bstrpick_w : INS_bstrpick_d;
    emit->emitIns_R_R_I_I(ins, EA_PTRSIZE, intReg, intReg, pos+size-1, pos);
    emit->emitIns_R_R_I(INS_xori, attr, intReg, intReg, expMask);

    genJumpToThrowHlpBlk_la(SCK_ARITH_EXCPN, INS_beq, intReg);

    // if it is a finite value copy it to targetReg
    if (treeNode->GetRegNum() != fpReg)
    {
        emit->emitIns_R_R(ins_Copy(targetType), attr, treeNode->GetRegNum(), fpReg);
    }
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_EQ/GT_NE/GT_LT/GT_LE/GT_GE/GT_GT node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForCompare(GenTreeOp* jtree)
{
    emitter* emit = GetEmitter();

    GenTreeOp* tree = nullptr;
    regNumber targetReg;
    if (jtree->OperIs(GT_JTRUE))
    {
        tree = jtree->gtGetOp1()->AsOp();
        targetReg = REG_RA;
        assert(tree->GetRegNum() == REG_NA);

        jtree->gtOp2 = (GenTree*)REG_RA;//targetReg
        jtree->SetRegNum((regNumber)INS_bnez);
    }
    else
    {
        tree = jtree;
        targetReg = tree->GetRegNum();
    }
    assert(targetReg != REG_NA);

    GenTree*  op1     = tree->gtOp1;
    GenTree*  op2     = tree->gtOp2;
    var_types op1Type = genActualType(op1->TypeGet());
    var_types op2Type = genActualType(op2->TypeGet());

    assert(!op1->isUsedFromMemory());
    assert(!op2->isUsedFromMemory());

    genConsumeOperands(tree);

    emitAttr cmpSize = EA_ATTR(genTypeSize(op1Type));

    assert(genTypeSize(op1Type) == genTypeSize(op2Type));

    if (varTypeIsFloating(op1Type))
    {
        assert(tree->OperIs(GT_LT, GT_LE, GT_EQ, GT_NE, GT_GT, GT_GE));
        bool IsUnordered = (tree->gtFlags & GTF_RELOP_NAN_UN) != 0;

        if(IsUnordered)
        {
            if(tree->OperIs(GT_LT))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cult_s : INS_fcmp_cult_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_LE))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cule_s : INS_fcmp_cule_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_EQ))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cueq_s : INS_fcmp_cueq_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_NE))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cune_s : INS_fcmp_cune_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_GT))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cult_s : INS_fcmp_cult_d, cmpSize, op2->GetRegNum(), op1->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_GE))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cule_s : INS_fcmp_cule_d, cmpSize, op2->GetRegNum(), op1->GetRegNum(), 1/*cc*/);
        }
        else
        {
            if(tree->OperIs(GT_LT))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_clt_s : INS_fcmp_clt_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_LE))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cle_s : INS_fcmp_cle_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_EQ))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_ceq_s : INS_fcmp_ceq_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_NE))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cne_s : INS_fcmp_cne_d, cmpSize, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_GT))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_clt_s : INS_fcmp_clt_d, cmpSize, op2->GetRegNum(), op1->GetRegNum(), 1/*cc*/);
            else if(tree->OperIs(GT_GE))
                emit->emitIns_R_R_I(cmpSize==EA_4BYTE ? INS_fcmp_cle_s : INS_fcmp_cle_d, cmpSize, op2->GetRegNum(), op1->GetRegNum(), 1/*cc*/);
        }

        emit->emitIns_R_R(INS_mov, EA_PTRSIZE, targetReg, REG_R0);
        emit->emitIns_R_I(INS_movcf2gr, EA_PTRSIZE, targetReg, 1/*cc*/);
    }
    else if (op1->isContainedIntOrIImmed() && op2->isContainedIntOrIImmed())
    {
        ssize_t imm1 = op1->AsIntCon()->gtIconVal;
        ssize_t imm2 = op2->AsIntCon()->gtIconVal;

        assert(tree->OperIs(GT_LT, GT_LE, GT_EQ, GT_NE, GT_GT, GT_GE));

        bool IsUnsigned = (tree->gtFlags & GTF_UNSIGNED) != 0;
        instruction ins = INS_beqz;

        switch (cmpSize)
        {
        case EA_4BYTE:
            {
                imm1 = static_cast<int32_t>(imm1);
                imm2 = static_cast<int32_t>(imm2);
            }
            break;
        case EA_8BYTE:
            break;
        case EA_1BYTE:
            {
                imm1 = static_cast<int8_t>(imm1);
                imm2 = static_cast<int8_t>(imm2);
            }
            break;
        //case EA_2BYTE:
        //    imm = static_cast<uint16_t>(imm);
        //    break;
        default:
            assert(!"Unexpected type in jumpCompare.");
        }

        switch (tree->OperGet())
        {
        case GT_LT:
            if (((!IsUnsigned) && (imm1 < imm2)) || ((IsUnsigned) && ((unsigned)imm1 < (unsigned)imm2)))
            {
                ins = INS_b;
            }
            break;
        case GT_LE:
            if (((!IsUnsigned) && (imm1 <= imm2)) || ((IsUnsigned) && ((unsigned)imm1 <= (unsigned)imm2)))
            {
                ins = INS_b;
            }
            break;
        case GT_EQ:
            if (imm1 == imm2)
            {
                ins = INS_b;
            }
            break;
        case GT_NE:
            if (imm1 != imm2)
            {
                ins = INS_b;
            }
            break;
        case GT_GT:
            if (((!IsUnsigned) && (imm1 > imm2)) || ((IsUnsigned) && ((unsigned)imm1 > (unsigned)imm2)))
            {
                ins = INS_b;
            }
            break;
        case GT_GE:
            if (((!IsUnsigned) && (imm1 >= imm2)) || ((IsUnsigned) && ((unsigned)imm1 >= (unsigned)imm2)))
            {
                ins = INS_b;
            }
            break;
        default:
            break;
        }

        assert(ins != INS_invalid);
        jtree->gtOp2 = (GenTree*)REG_SP;
        jtree->SetRegNum((regNumber)ins);
    }
    else
    {
        //TODO:can optimize further.
        if (op1->isContainedIntOrIImmed())
        {
            op1 = tree->gtOp2;
            op2 = tree->gtOp1;
            switch (tree->OperGet())
            {
            case GT_LT:
                tree->SetOper(GT_GT);
                break;
            case GT_LE:
                tree->SetOper(GT_GE);
                break;
            case GT_GT:
                tree->SetOper(GT_LT);
                break;
            case GT_GE:
                tree->SetOper(GT_LE);
                break;
            default:
                break;
            }
        }
        assert(!op1->isContainedIntOrIImmed());
        assert(tree->OperIs(GT_LT, GT_LE, GT_EQ, GT_NE, GT_GT, GT_GE));

        bool IsUnsigned = (tree->gtFlags & GTF_UNSIGNED) != 0;
        regNumber regOp1 = op1->GetRegNum();

        if (op2->isContainedIntOrIImmed())
        {
            ssize_t imm = op2->AsIntCon()->gtIconVal;

            {
                switch (cmpSize)
                {
                case EA_4BYTE:
                    imm = static_cast<int32_t>(imm);
                    break;
                case EA_8BYTE:
                    break;
                case EA_1BYTE:
                    imm = static_cast<int8_t>(imm);
                    break;
                //case EA_2BYTE:
                //    imm = static_cast<uint16_t>(imm);
                //    break;
                default:
                    assert(!"Unexpected type in jumpTrue(imm).");
                }
            }

            if (tree->OperIs(GT_LT))
            {
                if (!IsUnsigned && isValidSimm12(imm)) {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else if (IsUnsigned && isValidUimm11(imm)) {
                    emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else {
                    emit->emitIns_I_la(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                }
            }
            else if (tree->OperIs(GT_LE))
            {
                if (!IsUnsigned && isValidSimm12(imm + 1)) {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, targetReg, regOp1, imm + 1);
                }
                else if (IsUnsigned && isValidUimm11(imm + 1)) {
                    emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, targetReg, regOp1, imm + 1);
                }
                else {
                    emit->emitIns_I_la(EA_PTRSIZE, REG_RA, imm + 1);
                    emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                }
            }
            else if (tree->OperIs(GT_GT))
            {
                if (!IsUnsigned && isValidSimm12(imm + 1)) {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, REG_RA, regOp1, imm + 1);
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, REG_RA, 1);
                }
                else if (IsUnsigned && isValidUimm11(imm + 1)) {
                    emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, REG_RA, regOp1, imm + 1);
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, REG_RA, 1);
                }
                else {
                    emit->emitIns_I_la(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, REG_RA, regOp1);
                }
            }
            else if (tree->OperIs(GT_GE))
            {   if (!IsUnsigned && isValidSimm12(imm)) {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else if (IsUnsigned && isValidUimm11(imm)) {
                    emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else {
                    emit->emitIns_I_la(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                }
                emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, targetReg, 1);
            }
            else if (tree->OperIs(GT_NE))
            {
                if (!imm) {
                    emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, targetReg, REG_R0, regOp1);
                }
                else if (isValidUimm12(imm)) {
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, regOp1, imm);
                    emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, targetReg, REG_R0, targetReg);
                }
                else {
                    emit->emitIns_I_la(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(INS_xor, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                    emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, targetReg, REG_R0, targetReg);
                }
            }
            else if (tree->OperIs(GT_EQ))
            {
                if (!imm) {
                    emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, targetReg, regOp1, 1);
                }
                else if (isValidUimm12(imm)) {
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, regOp1, imm);
                    emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, targetReg, targetReg, 1);
                }
                else {
                    emit->emitIns_I_la(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(INS_xor, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                    emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, targetReg, targetReg, 1);
                }
            }

            genProduceReg(tree);
        }
        else
        {
            regNumber tmpRegOp1 = tree->ExtractTempReg();
            regNumber tmpRegOp2 = tree->ExtractTempReg();
            regNumber regOp2 = op2->GetRegNum();
            if (cmpSize == EA_4BYTE)
            {
                regOp1 = tmpRegOp1;
                regOp2 = tmpRegOp2;
                if (IsUnsigned)
                {
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_PTRSIZE, tmpRegOp1, op1->GetRegNum(), 31, 0);
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_PTRSIZE, tmpRegOp2, op2->GetRegNum(), 31, 0);
                }
                else
                {
                    emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, tmpRegOp1, op1->GetRegNum(), 0);
                    emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, tmpRegOp2, op2->GetRegNum(), 0);
                }
            }

            if (tree->OperIs(GT_LT))
            {
                emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp1, regOp2);
            }
            else if (tree->OperIs(GT_LE))
            {
                emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp2, regOp1);
                emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, targetReg, 1);
            }
            else if (tree->OperIs(GT_GT))
            {
                emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp2, regOp1);
            }
            else if (tree->OperIs(GT_GE))
            {
                emit->emitIns_R_R_R(IsUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp1, regOp2);
                emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, targetReg, 1);
            }
            else if (tree->OperIs(GT_NE))
            {
                emit->emitIns_R_R_R(INS_xor, EA_PTRSIZE, targetReg, regOp1, regOp2);
                emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, targetReg, REG_R0, targetReg);
            }
            else if (tree->OperIs(GT_EQ))
            {
                emit->emitIns_R_R_R(INS_xor, EA_PTRSIZE, targetReg, regOp1, regOp2);
                emit->emitIns_R_R_I(INS_sltui, EA_PTRSIZE, targetReg, targetReg, 1);
            }

            genProduceReg(tree);
        }
    }
}

//------------------------------------------------------------------------
// genCodeForJumpTrue: Generate code for a GT_JTRUE node.
//
// Arguments:
//    jtrue - The node
//
void CodeGen::genCodeForJumpTrue(GenTreeOp* jtrue)
{
    //assert(compiler->compCurBB->bbJumpKind == BBJ_COND);//should confirm.
    ////assert(jtrue->OperIs(GT_JTRUE));

    emitter* emit = GetEmitter();

    GenTreeOp* tree = jtrue->OperIs(GT_JTRUE) ? jtrue->gtGetOp1()->AsOp() : jtrue;
    regNumber targetReg = tree->GetRegNum();
    instruction ins = INS_invalid;

    if (jtrue->OperIs(GT_JTRUE) && jtrue->gtOp2)
    {
        emit->emitIns_J((instruction)jtrue->GetRegNum(), compiler->compCurBB->bbJumpDest, (int)(int64_t)jtrue->gtOp2);//5-bits;
        jtrue->SetRegNum(REG_NA);
        jtrue->gtOp2 = nullptr;
        return;
    }
    else
    {
        GenTree*  op1 = tree->gtOp1;
        GenTree*  op2 = tree->gtOp2;

        var_types op1Type = genActualType(op1->TypeGet());
        var_types op2Type = genActualType(op2->TypeGet());

        bool IsEq = tree == jtrue->gtPrev;

        assert(!op1->isUsedFromMemory());
        assert(!op2->isUsedFromMemory());

        genConsumeOperands(tree);

        emitAttr cmpSize = EA_ATTR(genTypeSize(op1Type));

        assert(targetReg == REG_NA);
        int SaveCcResultReg = (int)REG_RA << 5;

        if (varTypeIsFloating(op1Type))
        {
            assert(genTypeSize(op1Type) == genTypeSize(op2Type));
            //int cc = 1;

            assert(tree->OperIs(GT_LT, GT_LE, GT_EQ, GT_NE, GT_GT, GT_GE));
            bool IsUnordered = (tree->gtFlags & GTF_RELOP_NAN_UN) != 0;

            if (tree->OperIs(GT_EQ))
            {
                ins = INS_bcnez;
                if (cmpSize == EA_4BYTE)
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cueq_s : INS_fcmp_ceq_s, EA_4BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
                else
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cueq_d : INS_fcmp_ceq_d, EA_8BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            }
            else if (tree->OperIs(GT_NE))
            {
                ins = INS_bceqz;
                if (cmpSize == EA_4BYTE)
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_ceq_s : INS_fcmp_cueq_s, EA_4BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
                else
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_ceq_d : INS_fcmp_cueq_d, EA_8BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            }
            else if (tree->OperIs(GT_LT))
            {
                ins = INS_bcnez;
                if (cmpSize == EA_4BYTE)
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cult_s : INS_fcmp_clt_s, EA_4BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
                else
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cult_d : INS_fcmp_clt_d, EA_8BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            }
            else if (tree->OperIs(GT_LE))
            {
                ins = INS_bcnez;
                if (cmpSize == EA_4BYTE)
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cule_s : INS_fcmp_cle_s, EA_4BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
                else
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cule_d : INS_fcmp_cle_d, EA_8BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            }
            else if (tree->OperIs(GT_GE))
            {
                ins = INS_bceqz;
                if (cmpSize == EA_4BYTE)
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_clt_s : INS_fcmp_cult_s, EA_4BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
                else
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_clt_d : INS_fcmp_cult_d, EA_8BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            }
            else if (tree->OperIs(GT_GT))
            {
                ins = INS_bceqz;
                if (cmpSize == EA_4BYTE)
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cle_s : INS_fcmp_cule_s, EA_4BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
                else
                    emit->emitIns_R_R_I(IsUnordered ? INS_fcmp_cle_d : INS_fcmp_cule_d, EA_8BYTE, op1->GetRegNum(), op2->GetRegNum(), 1/*cc*/);
            }

            //assert(0 <= cc && cc < 8);
            if (IsEq)
                emit->emitIns_J(ins, compiler->compCurBB->bbJumpDest, (int)1/*cc*/);//5-bits;
            else
            {
                jtrue->gtOp2 = (GenTree*)(1/*cc*/);
                jtrue->SetRegNum((regNumber)ins);
            }
        }
        else if (op1->isContainedIntOrIImmed() && op2->isContainedIntOrIImmed())
        {
            ssize_t imm1 = op1->AsIntCon()->gtIconVal;
            ssize_t imm2 = op2->AsIntCon()->gtIconVal;

            assert(tree->OperIs(GT_LT, GT_LE, GT_EQ, GT_NE, GT_GT, GT_GE));

            bool IsUnsigned = (tree->gtFlags & GTF_UNSIGNED) != 0;

            switch (cmpSize)
            {
            case EA_4BYTE:
                {
                    imm1 = static_cast<int32_t>(imm1);
                    imm2 = static_cast<int32_t>(imm2);
                }
                break;
            case EA_8BYTE:
                break;
            case EA_1BYTE:
                {
                    imm1 = static_cast<int8_t>(imm1);
                    imm2 = static_cast<int8_t>(imm2);
                }
                break;
            //case EA_2BYTE:
            //    imm = static_cast<uint16_t>(imm);
            //    break;
            default:
                assert(!"Unexpected type in jumpTrue.");
            }
            switch (tree->OperGet())
            {
            case GT_LT:
                if (((!IsUnsigned) && (imm1 < imm2)) || ((IsUnsigned) && ((unsigned)imm1 < (unsigned)imm2)))
                {
                    ins = INS_b;
                }
                break;
            case GT_LE:
                if (((!IsUnsigned) && (imm1 <= imm2)) || ((IsUnsigned) && ((unsigned)imm1 <= (unsigned)imm2)))
                {
                    ins = INS_b;
                }
                break;
            case GT_EQ:
                if (imm1 == imm2)
                {
                    ins = INS_b;
                }
                break;
            case GT_NE:
                if (imm1 != imm2)
                {
                    ins = INS_b;
                }
                break;
            case GT_GT:
                if (((!IsUnsigned) && (imm1 > imm2)) || ((IsUnsigned) && ((unsigned)imm1 > (unsigned)imm2)))
                {
                    ins = INS_b;
                }
                break;
            case GT_GE:
                if (((!IsUnsigned) && (imm1 >= imm2)) || ((IsUnsigned) && ((unsigned)imm1 >= (unsigned)imm2)))
                {
                    ins = INS_b;
                }
                break;
            default:
                break;
            }

            if (IsEq && (ins != INS_invalid))
                emit->emitIns_J(ins, compiler->compCurBB->bbJumpDest, 0);//5-bits;
            else if (ins != INS_invalid)
            {
                jtrue->gtOp2 = (GenTree*)(uint64_t)SaveCcResultReg;
                jtrue->SetRegNum((regNumber)ins);
            }
        }
        else
        {
            //TODO:can optimize further.
            if (op1->isContainedIntOrIImmed())
            {
                op1 = tree->gtOp2;
                op2 = tree->gtOp1;
                switch (tree->OperGet())
                {
                case GT_LT:
                    tree->SetOper(GT_GT);
                    break;
                case GT_LE:
                    tree->SetOper(GT_GE);
                    break;
                case GT_GT:
                    tree->SetOper(GT_LT);
                    break;
                case GT_GE:
                    tree->SetOper(GT_LE);
                    break;
                default:
                    break;
                }
            }

            assert(tree->OperIs(GT_LT, GT_LE, GT_EQ, GT_NE, GT_GT, GT_GE));

            bool IsUnsigned = (tree->gtFlags & GTF_UNSIGNED) != 0;

            regNumber regOp1 = op1->GetRegNum();

            if (op2->isContainedIntOrIImmed())
            {
                ssize_t imm = op2->AsIntCon()->gtIconVal;

                if (imm)
                {
                    switch (cmpSize)
                    {
                    case EA_4BYTE:
                        if (op1->gtFlags & GTF_UNSIGNED)
                            imm = static_cast<uint32_t>(imm);
                        else
                            imm = static_cast<int32_t>(imm);
                        break;
                    case EA_8BYTE:
                        break;
                    case EA_1BYTE:
                        imm = static_cast<int8_t>(imm);
                        break;
                    //case EA_2BYTE:
                    //    imm = static_cast<uint16_t>(imm);
                    //    break;
                    default:
                        assert(!"Unexpected type in jumpTrue(imm).");
                    }

                    emit->emitIns_I_la(EA_PTRSIZE, REG_RA, imm);//TODO: maybe optimize.
                }
                else
                {
                    SaveCcResultReg = 0;
                }

                if (tree->OperIs(GT_LT)) {
                    SaveCcResultReg |= ((int)regOp1);
                    ins = IsUnsigned ? INS_bltu : INS_blt;
                }
                else if (tree->OperIs(GT_LE)) {
                    SaveCcResultReg = imm ? ((((int)regOp1) << 5) | (int)REG_RA) : (((int)regOp1) << 5);
                    ins = IsUnsigned ? INS_bgeu : INS_bge;
                }
                else if (tree->OperIs(GT_GT)) {
                    SaveCcResultReg = imm ? ((((int)regOp1) << 5) | (int)REG_RA) : (((int)regOp1) << 5);
                    ins = IsUnsigned ? INS_bltu : INS_blt;
                }
                else if (tree->OperIs(GT_GE)) {
                    SaveCcResultReg |= ((int)regOp1);
                    ins = IsUnsigned ? INS_bgeu : INS_bge;
                }
                else if (tree->OperIs(GT_NE)) {
                    SaveCcResultReg |= ((int)regOp1);
                    ins = INS_bne;
                }
                else if (tree->OperIs(GT_EQ)) {
                    SaveCcResultReg |= ((int)regOp1);
                    ins = INS_beq;
                }
            }
            else
            {
                regNumber regOp2 = op2->GetRegNum();
                if (IsUnsigned  && cmpSize == EA_4BYTE && op2->OperIs(GT_LCL_VAR) && compiler->lvaTable[op2->AsLclVar()->GetLclNum()].lvIsRegCandidate())
                {//TODO: should amend further!!!
                    regNumber tmpRegOp1 = tree->ExtractTempReg();
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, REG_RA, regOp2, 31, 0);
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, tmpRegOp1, regOp1, 31, 0);
                    regOp1 = tmpRegOp1;
                    regOp2 = REG_RA;
                }
                else if (IsUnsigned  && cmpSize == EA_4BYTE && op1->OperIs(GT_LCL_VAR) && compiler->lvaTable[op1->AsLclVar()->GetLclNum()].lvIsRegCandidate())
                {//TODO: should amend further!!!
                    regNumber tmpRegOp1 = tree->ExtractTempReg();
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, tmpRegOp1, regOp1, 31, 0);
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, REG_RA, regOp2, 31, 0);
                    regOp1 = tmpRegOp1;
                    regOp2 = REG_RA;
                }
                else if (cmpSize == EA_4BYTE && op1->OperIs(GT_CALL) && op2->OperIs(GT_LCL_VAR) && compiler->lvaTable[op2->AsLclVar()->GetLclNum()].lvIsRegCandidate())
                {//TODO: should amend further!!!
                    emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, REG_RA, regOp2, 0);
                    regOp2 = REG_RA;
                }
                else if (cmpSize == EA_4BYTE && ((op1->gtFlags | op2->gtFlags) & GTF_UNSIGNED))
                {//TODO: should amend further!!!
                    if (!(op1->gtFlags & GTF_UNSIGNED))
                    {
                        regNumber tmpRegOp1 = tree->ExtractTempReg();
                        emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, tmpRegOp1, regOp1, 31, 0);
                        regOp1 = tmpRegOp1;
                    }
                    if (!(op2->gtFlags & GTF_UNSIGNED))
                    {
                        emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_8BYTE, REG_RA, regOp2, 31, 0);
                        regOp2 = REG_RA;
                    }
                }

                if (tree->OperIs(GT_LT)) {
                    SaveCcResultReg = ((int)regOp1 | ((int)regOp2 << 5));
                    ins = IsUnsigned ? INS_bltu : INS_blt;
                }
                else if (tree->OperIs(GT_LE)) {
                    SaveCcResultReg = (((int)regOp1) << 5) | (int)regOp2;
                    ins = IsUnsigned ? INS_bgeu : INS_bge;
                }
                else if (tree->OperIs(GT_GT)) {
                    SaveCcResultReg = (((int)regOp1) << 5) | (int)regOp2;
                    ins = IsUnsigned ? INS_bltu : INS_blt;
                }
                else if (tree->OperIs(GT_GE)) {
                    SaveCcResultReg = ((int)regOp1 | ((int)regOp2 << 5));
                    ins = IsUnsigned ? INS_bgeu : INS_bge;
                }
                else if (tree->OperIs(GT_NE)) {
                    SaveCcResultReg = (((int)regOp1) << 5) | (int)regOp2;
                    ins = INS_bne;
                }
                else if (tree->OperIs(GT_EQ)) {
                    SaveCcResultReg = (((int)regOp1) << 5) | (int)regOp2;
                    ins = INS_beq;
                }
            }

            if (IsEq)
                emit->emitIns_J(ins, compiler->compCurBB->bbJumpDest, SaveCcResultReg);//5-bits;
            else
            {
                jtrue->gtOp2 = (GenTree*)(uint64_t)SaveCcResultReg;
                jtrue->SetRegNum((regNumber)ins);
            }
        }
    }
}

//------------------------------------------------------------------------
// genCodeForJumpCompare: Generates code for jmpCompare statement.
//
// A GT_JCMP node is created when a comparison and conditional branch
// can be executed in a single instruction.
//
// LOONGARCH64 has a few instructions with this behavior.
//   - beq/bne -- Compare and branch register equal/not equal
//
// The beq/bne supports the normal +/- 2^15 branch range for conditional branches
//
// A GT_JCMP beq/bne node is created when there is a GT_EQ or GT_NE
// integer/unsigned comparison against the value of Rt register which is used by
// a GT_JTRUE condition jump node.
//
// This node is repsonsible for consuming the register, and emitting the
// appropriate fused compare/test and branch instruction
//
// Two flags guide code generation
//    GTF_JCMP_EQ  -- Set if this is beq rather than bne
//
// Arguments:
//    tree - The GT_JCMP tree node.
//
// Return Value:
//    None
//
void CodeGen::genCodeForJumpCompare(GenTreeOp* tree)
{
    assert(compiler->compCurBB->bbJumpKind == BBJ_COND);

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();

    assert(tree->OperIs(GT_JCMP));
    assert(!varTypeIsFloating(tree));
    assert(!op1->isUsedFromMemory());
    assert(!op2->isUsedFromMemory());
    assert(op2->IsCnsIntOrI());
    assert(op2->isContained());

    genConsumeOperands(tree);

    regNumber reg  = op1->GetRegNum();
    emitAttr  attr = emitActualTypeSize(op1->TypeGet());

    //if (tree->gtFlags & GTF_JCMP_TST)
    //{
    //    assert(!"unimplemented on LOONGARCH yet");
    //    //ssize_t compareImm = op2->AsIntCon()->IconValue();

    //    //assert(isPow2(compareImm));

    //    //instruction ins = (tree->gtFlags & GTF_JCMP_EQ) ? INS_tbz : INS_tbnz;
    //    //int         imm = genLog2((size_t)compareImm);

    //    //GetEmitter()->emitIns_J_R_I(ins, attr, compiler->compCurBB->bbJumpDest, reg, imm);
    //}
    //else
    {
        instruction ins;
        int regs;
        if (op2->AsIntCon()->gtIconVal)
        {
            assert(reg != REG_R21);
            GetEmitter()->emitIns_I_la(EA_PTRSIZE, REG_R21, op2->AsIntCon()->gtIconVal);
            regs = (int)reg << 5;
            regs |= (int)REG_R21;//REG_R21
            ins = (tree->gtFlags & GTF_JCMP_EQ) ? INS_beq : INS_bne;
        }
        else
        {
            regs = (int)reg;
            ins = (tree->gtFlags & GTF_JCMP_EQ) ? INS_beqz : INS_bnez;
        }

        GetEmitter()->emitIns_J(ins, compiler->compCurBB->bbJumpDest, regs);//5-bits;
    }
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return offset from the stack pointer (Initial-SP) to the frame pointer. The frame pointer
// will point to the saved frame pointer slot (i.e., there will be frame pointer chaining).
//
int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());

    int delta;
    if (IsSaveFpRaWithAllCalleeSavedRegisters())
    {
        //delta = (compiler->compCalleeRegsPushed -2)* REGSIZE_BYTES + compiler->compLclFrameSize;
        //assert(delta == genTotalFrameSize() - compiler->lvaArgSize - 2*8);
        delta = genTotalFrameSize() - (compiler->info.compIsVarArgs ? MAX_REG_ARG * REGSIZE_BYTES : 0) -
                (compiler->compCalleeRegsPushed -1)* REGSIZE_BYTES;
    }
    else
    {
        delta = compiler->lvaOutgoingArgSpaceSize;
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

    assert(totalFrameSize > 0);
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
// SetSaveFpRaWithAllCalleeSavedRegisters - Set the variable that indicates if FP/RA registers
// are stored with the rest of the callee-saved registers.
void CodeGen::SetSaveFpRaWithAllCalleeSavedRegisters(bool value)
{
    JITDUMP("Setting genSaveFpRaWithAllCalleeSavedRegisters to %s\n", dspBool(value));
    genSaveFpRaWithAllCalleeSavedRegisters = value;
}

//---------------------------------------------------------------------
// IsSaveFpRaWithAllCalleeSavedRegisters - Return the value that indicates where FP/RA registers
// are stored in the prolog.
bool CodeGen::IsSaveFpRaWithAllCalleeSavedRegisters() const
{
    return genSaveFpRaWithAllCalleeSavedRegisters;
}

/*****************************************************************************
 *  Emit a call to a helper function.
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
        // li x, pAddr     #NOTE: this maybe muti-instructions.
        // ld x, [x]
        // jr x

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

        //instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
        //GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, callTarget, callTarget, 0);
        if (compiler->opts.compReloc)
        {
            GetEmitter()->emitIns_R_AI(INS_bl, EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
        }
        else
        {
            //GetEmitter()->emitIns_R_I(INS_pcaddu12i, EA_PTRSIZE, callTarget, (ssize_t)pAddr);
            //GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, callTarget, callTarget, );
            GetEmitter()->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, callTarget, ((ssize_t)pAddr & 0xfffff000)>>12);
            GetEmitter()->emitIns_R_I(INS_lu32i_d, EA_PTRSIZE, callTarget, (ssize_t)pAddr >> 32);
            GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, callTarget, callTarget, ((ssize_t)pAddr & 0xfff)>>2);
        }
        regSet.verifyRegUsed(callTarget);

        callType = emitter::EC_INDIR_R;
    }

    GetEmitter()->emitIns_Call(callType, compiler->eeFindHelper(helper), INDEBUG_LDISASM_COMMA(nullptr) addr, argSize,
                               retSize, EA_UNKNOWN, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, DebugInfo(), /* IL offset */
                               callTarget,                             /* ireg */
                               REG_NA, 0, 0,                           /* xreg, xmul, disp */
                               false                                   /* isJump */
                               );

    regMaskTP killMask = compiler->compHelperCallKillSet((CorInfoHelpFunc)helper);
    regSet.verifyRegistersUsed(killMask);
}

#ifdef FEATURE_SIMD

//------------------------------------------------------------------------
// genSIMDIntrinsic: Generate code for a SIMD Intrinsic.  This is the main
// routine which in turn calls appropriate genSIMDIntrinsicXXX() routine.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    Currently, we only recognize SIMDVector<float> and SIMDVector<int>, and
//    a limited set of methods.
//
// TODO-CLEANUP Merge all versions of this function and move to new file simdcodegencommon.cpp.
void CodeGen::genSIMDIntrinsic(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

insOpts CodeGen::genGetSimdInsOpt(emitAttr size, var_types elementType)
{
    assert(!"unimplemented on LOONGARCH yet");
    return INS_OPTS_NONE;
}

// getOpForSIMDIntrinsic: return the opcode for the given SIMD Intrinsic
//
// Arguments:
//   intrinsicId    -   SIMD intrinsic Id
//   baseType       -   Base type of the SIMD vector
//   immed          -   Out param. Any immediate byte operand that needs to be passed to SSE2 opcode
//
//
// Return Value:
//   Instruction (op) to be used, and immed is set if instruction requires an immediate operand.
//
instruction CodeGen::getOpForSIMDIntrinsic(SIMDIntrinsicID intrinsicId, var_types baseType, unsigned* ival /*=nullptr*/)
{
    assert(!"unimplemented on LOONGARCH yet");
    return INS_invalid;
}

//------------------------------------------------------------------------
// genSIMDIntrinsicInit: Generate code for SIMD Intrinsic Initialize.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicInit(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//-------------------------------------------------------------------------------------------
// genSIMDIntrinsicInitN: Generate code for SIMD Intrinsic Initialize for the form that takes
//                        a number of arguments equal to the length of the Vector.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicInitN(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//----------------------------------------------------------------------------------
// genSIMDIntrinsicUnOp: Generate code for SIMD Intrinsic unary operations like sqrt.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicUnOp(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicWiden: Generate code for SIMD Intrinsic Widen operations
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Notes:
//    The Widen intrinsics are broken into separate intrinsics for the two results.
//
void CodeGen::genSIMDIntrinsicWiden(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicNarrow: Generate code for SIMD Intrinsic Narrow operations
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Notes:
//    This intrinsic takes two arguments. The first operand is narrowed to produce the
//    lower elements of the results, and the second operand produces the high elements.
//
void CodeGen::genSIMDIntrinsicNarrow(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicBinOp: Generate code for SIMD Intrinsic binary operations
// add, sub, mul, bit-wise And, AndNot and Or.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicBinOp(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicRelOp: Generate code for a SIMD Intrinsic relational operater
// == and !=
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicRelOp(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicDotProduct: Generate code for SIMD Intrinsic Dot Product.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicDotProduct(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------------------
// genSIMDIntrinsicGetItem: Generate code for SIMD Intrinsic get element at index i.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicGetItem(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------------------
// genSIMDIntrinsicSetItem: Generate code for SIMD Intrinsic set element at index i.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
void CodeGen::genSIMDIntrinsicSetItem(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//-----------------------------------------------------------------------------
// genSIMDIntrinsicUpperSave: save the upper half of a TYP_SIMD16 vector to
//                            the given register, if any, or to memory.
//
// Arguments:
//    simdNode - The GT_SIMD node
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
void CodeGen::genSIMDIntrinsicUpperSave(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//-----------------------------------------------------------------------------
// genSIMDIntrinsicUpperRestore: Restore the upper half of a TYP_SIMD16 vector to
//                               the given register, if any, or to memory.
//
// Arguments:
//    simdNode - The GT_SIMD node
//
// Return Value:
//    None.
//
// Notes:
//    For consistency with genSIMDIntrinsicUpperSave, and to ensure that lclVar nodes always
//    have their home register, this node has its targetReg on the lclVar child, and its source
//    on the simdNode.
//    Regarding spill, please see the note above on genSIMDIntrinsicUpperSave.  If we have spilled
//    an upper-half to the lclVar's home location, this node will be marked GTF_SPILLED.
//
void CodeGen::genSIMDIntrinsicUpperRestore(GenTreeSIMD* simdNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//-----------------------------------------------------------------------------
// genStoreIndTypeSIMD12: store indirect a TYP_SIMD12 (i.e. Vector3) to memory.
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
void CodeGen::genStoreIndTypeSIMD12(GenTree* treeNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//-----------------------------------------------------------------------------
// genLoadIndTypeSIMD12: load indirect a TYP_SIMD12 (i.e. Vector3) value.
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
void CodeGen::genLoadIndTypeSIMD12(GenTree* treeNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//-----------------------------------------------------------------------------
// genStoreLclTypeSIMD12: store a TYP_SIMD12 (i.e. Vector3) type field.
// Since Vector3 is not a hardware supported write size, it is performed
// as two stores: 8 byte followed by 4-byte.
//
// Arguments:
//    treeNode - tree node that is attempting to store TYP_SIMD12 field
//
// Return Value:
//    None.
//
void CodeGen::genStoreLclTypeSIMD12(GenTree* treeNode)
{
    assert(!"unimplemented on LOONGARCH yet");
}

#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
#include "hwintrinsic.h"

instruction CodeGen::getOpForHWIntrinsic(GenTreeHWIntrinsic* node, var_types instrType)
{
    assert(!"unimplemented on LOONGARCH yet");
    return INS_invalid;
}

//------------------------------------------------------------------------
// genHWIntrinsic: Produce code for a GT_HWINTRINSIC node.
//
// This is the main routine which in turn calls the genHWIntrinsicXXX() routines.
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsic(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicUnaryOp:
//
// Produce code for a GT_HWINTRINSIC node with form UnaryOp.
//
// Consumes one scalar operand produces a scalar
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicUnaryOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicCrcOp:
//
// Produce code for a GT_HWINTRINSIC node with form CrcOp.
//
// Consumes two scalar operands and produces a scalar result
//
// This form differs from BinaryOp because the attr depends on the size of op2
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicCrcOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdBinaryOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdBinaryOp.
//
// Consumes two SIMD operands and produces a SIMD result
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdBinaryOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSwitchTable: generate the jump-table for imm-intrinsics
//    with non-constant argument
//
// Arguments:
//    swReg      - register containing the switch case to execute
//    tmpReg     - temporary integer register for calculating the switch indirect branch target
//    swMax      - the number of switch cases.
//    emitSwCase - lambda to generate an individual switch case
//
// Notes:
//    Used for cases where an instruction only supports immediate operands,
//    but at jit time the operand is not a constant.
//
//    The importer is responsible for inserting an upstream range check
//    (GT_HW_INTRINSIC_CHK) for swReg, so no range check is needed here.
//
template <typename HWIntrinsicSwitchCaseBody>
void CodeGen::genHWIntrinsicSwitchTable(regNumber                 swReg,
                                        regNumber                 tmpReg,
                                        int                       swMax,
                                        HWIntrinsicSwitchCaseBody emitSwCase)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdExtractOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdExtractOp.
//
// Consumes one SIMD operand and one scalar
//
// The element index operand is typically a const immediate
// When it is not, a switch table is generated
//
// See genHWIntrinsicSwitchTable comments
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdExtractOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdInsertOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdInsertOp.
//
// Consumes one SIMD operand and two scalars
//
// The element index operand is typically a const immediate
// When it is not, a switch table is generated
//
// See genHWIntrinsicSwitchTable comments
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdInsertOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdSelectOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdSelectOp.
//
// Consumes three SIMD operands and produces a SIMD result
//
// This intrinsic form requires one of the source registers to be the
// destination register.  Inserts a INS_mov if this requirement is not met.
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdSelectOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdSetAllOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdSetAllOp.
//
// Consumes single scalar operand and produces a SIMD result
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdSetAllOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdUnaryOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdUnaryOp.
//
// Consumes single SIMD operand and produces a SIMD result
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdUnaryOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdBinaryRMWOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdBinaryRMWOp.
//
// Consumes two SIMD operands and produces a SIMD result.
// First operand is both source and destination.
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdBinaryRMWOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicSimdTernaryRMWOp:
//
// Produce code for a GT_HWINTRINSIC node with form SimdTernaryRMWOp
//
// Consumes three SIMD operands and produces a SIMD result.
// First operand is both source and destination.
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicSimdTernaryRMWOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicShaHashOp:
//
// Produce code for a GT_HWINTRINSIC node with form Sha1HashOp.
// Used in LOONGARCH64 SHA1 Hash operations.
//
// Consumes three operands and returns a Simd result.
// First Simd operand is both source and destination.
// Second Operand is an unsigned int.
// Third operand is a simd operand.

// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicShaHashOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genHWIntrinsicShaRotateOp:
//
// Produce code for a GT_HWINTRINSIC node with form Sha1RotateOp.
// Used in LOONGARCH64 SHA1 Rotate operations.
//
// Consumes one integer operand and returns unsigned int result.
//
// Arguments:
//    node - the GT_HWINTRINSIC node
//
// Return Value:
//    None.
//
void CodeGen::genHWIntrinsicShaRotateOp(GenTreeHWIntrinsic* node)
{
    assert(!"unimplemented on LOONGARCH yet");
}

#endif // FEATURE_HW_INTRINSICS

/*****************************************************************************
 * Unit testing of the LOONGARCH64 emitter: generate a bunch of instructions into the prolog
 * (it's as good a place as any), then use COMPlus_JitLateDisasm=* to see if the late
 * disassembler thinks the instructions as the same as we do.
 */

// Uncomment "#define ALL_LOONGARCH64_EMITTER_UNIT_TESTS" to run all the unit tests here.
// After adding a unit test, and verifying it works, put it under this #ifdef, so we don't see it run every time.
//#define ALL_LOONGARCH64_EMITTER_UNIT_TESTS

#if defined(DEBUG)
void CodeGen::genLOONGARCH64EmitterUnitTests()
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
    printf("*************** In genLOONGARCH64EmitterUnitTests()\n");

    printf("*************** End of genLOONGARCH64EmitterUnitTests()\n");
}
#endif // defined(DEBUG)

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
    assert(spDelta < 0);

    // We assert that the SP change is less than one page. If it's greater, you should have called a
    // function that does a probe, which will in turn call this function.
    assert((target_size_t)(-spDelta) <= compiler->eeGetPageSize());

    if (-2048 <= spDelta && spDelta < 0)
        GetEmitter()->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta);
    else
    {
        GetEmitter()->emitIns_R_R_I(INS_srai_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, 3);
        GetEmitter()->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta >> 3);
        GetEmitter()->emitIns_R_R_I(INS_slli_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, 3);
    }
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
    GetEmitter()->emitIns_R_R_I(INS_ld_w, EA_4BYTE, regTmp, REG_SP, 0);
    genStackPointerConstantAdjustment(spDelta, regTmp);
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
    assert(spDelta < 0);

    const target_size_t pageSize = compiler->eeGetPageSize();

    ssize_t spRemainingDelta = spDelta;
    do
    {
        ssize_t spOneDelta = -(ssize_t)min((target_size_t)-spRemainingDelta, pageSize);
        genStackPointerConstantAdjustmentWithProbe(spOneDelta, regTmp);
        spRemainingDelta -= spOneDelta;
    } while (spRemainingDelta < 0);

    // What offset from the final SP was the last probe? This depends on the fact that
    // genStackPointerConstantAdjustmentWithProbe() probes first, then does "SUB SP".
    target_size_t lastTouchDelta = (target_size_t)(-spDelta) % pageSize;
    if ((lastTouchDelta == 0) || (lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize))
    {
        // We haven't probed almost a complete page. If lastTouchDelta==0, then spDelta was an exact
        // multiple of pageSize, which means we last probed exactly one page back. Otherwise, we probed
        // the page, but very far from the end. If the next action on the stack might subtract from SP
        // first, before touching the current SP, then we do one more probe at the very bottom. This can
        // happen on x86, for example, when we copy an argument to the stack using a "SUB ESP; REP MOV"
        // strategy.

        GetEmitter()->emitIns_R_R_I(INS_ld_w, EA_4BYTE, regTmp, REG_SP, 0);
        lastTouchDelta = 0;
    }

    return lastTouchDelta;
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
            GetEmitter()->emitDisableGC();
            break;

        case GT_START_PREEMPTGC:
            // Kill callee saves GC registers, and create a label
            // so that information gets propagated to the emitter.
            gcInfo.gcMarkRegSetNpt(RBM_INT_CALLEE_SAVED);
            genDefineTempLabel(genCreateTempLabel());
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
            if ((targetType == TYP_DOUBLE) || (targetType == TYP_FLOAT))
                treeNode->gtOper = GT_CNS_DBL;
            FALLTHROUGH;
        case GT_CNS_DBL:
            genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
            break;

        case GT_NOT:
        case GT_NEG:
            genCodeForNegNot(treeNode);
            break;

        case GT_BSWAP:
        case GT_BSWAP16:
            genCodeForBswap(treeNode);
            break;

        case GT_MOD:
        case GT_UMOD:
        case GT_DIV:
        case GT_UDIV:
            genCodeForDivMod(treeNode->AsOp());
            break;

        case GT_OR:
        case GT_XOR:
        case GT_AND:
            assert(varTypeIsIntegralOrI(treeNode));

            FALLTHROUGH;

        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
            genConsumeOperands(treeNode->AsOp());
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROR:
            genCodeForShift(treeNode);
            break;

        case GT_CAST:
            genCodeForCast(treeNode->AsOp());
            break;

        case GT_BITCAST:
            genCodeForBitCast(treeNode->AsOp());
            break;

        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR_ADDR:
            genCodeForLclAddr(treeNode);
            break;

        case GT_LCL_FLD:
            genCodeForLclFld(treeNode->AsLclFld());
            break;

        case GT_LCL_VAR:
            genCodeForLclVar(treeNode->AsLclVar());
            break;

        case GT_STORE_LCL_FLD:
            genCodeForStoreLclFld(treeNode->AsLclFld());
            break;

        case GT_STORE_LCL_VAR:
            genCodeForStoreLclVar(treeNode->AsLclVar());
            break;

        case GT_RETFILT:
        case GT_RETURN:
            genReturn(treeNode);
            break;

        case GT_LEA:
            // If we are here, it is the case where there is an LEA that cannot be folded into a parent instruction.
            genLeaInstruction(treeNode->AsAddrMode());
            break;

        case GT_INDEX_ADDR:
            genCodeForIndexAddr(treeNode->AsIndexAddr());
            break;

        case GT_IND:
            genCodeForIndir(treeNode->AsIndir());
            break;

        case GT_INC_SATURATE:
            genCodeForIncSaturate(treeNode);
            break;

        case GT_MULHI:
            genCodeForMulHi(treeNode->AsOp());
            break;

        case GT_SWAP:
            genCodeForSwap(treeNode->AsOp());
            break;

        case GT_JMP:
            genJmpMethod(treeNode);
            break;

        case GT_CKFINITE:
            genCkfinite(treeNode);
            break;

        case GT_INTRINSIC:
            genIntrinsic(treeNode);
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            genSIMDIntrinsic(treeNode->AsSIMD());
            break;
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            genHWIntrinsic(treeNode->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_CMP:
            if (treeNode->GetRegNum() != REG_NA)
            {
                genCodeForCompare(treeNode->AsOp());
            }
            else if (!treeNode->gtNext)
                genCodeForJumpTrue(treeNode->AsOp());
            else if (!treeNode->gtNext->OperIs(GT_JTRUE))
            {
                GenTree* treeNode_next = treeNode->gtNext;
                while (treeNode_next)
                {
                    if (treeNode_next->OperIs(GT_JTRUE))
                        break;
                    treeNode_next = treeNode_next->gtNext;
                };
                assert(treeNode_next->OperIs(GT_JTRUE));
                //genCodeForJumpTrue(treeNode_next->AsOp());
                genCodeForCompare(treeNode_next->AsOp());
            }
            break;

        case GT_JTRUE:
            genCodeForJumpTrue(treeNode->AsOp());
            break;

        case GT_JCMP:
            genCodeForJumpCompare(treeNode->AsOp());
            break;

        case GT_JCC:
            genCodeForJcc(treeNode->AsCC());
            break;

        case GT_SETCC:
            genCodeForSetcc(treeNode->AsCC());
            break;

        case GT_RETURNTRAP:
            genCodeForReturnTrap(treeNode->AsOp());
            break;

        case GT_STOREIND:
            genCodeForStoreInd(treeNode->AsStoreInd());
            break;

        case GT_COPY:
            // This is handled at the time we call genConsumeReg() on the GT_COPY
            break;

        case GT_FIELD_LIST:
            // Should always be marked contained.
            assert(!"LIST, FIELD_LIST nodes should always be marked contained.");
            break;

        case GT_PUTARG_STK:
            genPutArgStk(treeNode->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
            genPutArgReg(treeNode->AsOp());
            break;

#if FEATURE_ARG_SPLIT
        case GT_PUTARG_SPLIT:
            genPutArgSplit(treeNode->AsPutArgSplit());
            break;
#endif // FEATURE_ARG_SPLIT

        case GT_CALL:
            genCallInstruction(treeNode->AsCall());
            break;

        case GT_MEMORYBARRIER:
        {
            CodeGen::BarrierKind barrierKind =
                treeNode->gtFlags & GTF_MEMORYBARRIER_LOAD ? BARRIER_LOAD_ONLY : BARRIER_FULL;

            instGen_MemoryBarrier(barrierKind);
            break;
        }

        case GT_XCHG:
        case GT_XADD:
            genLockedInstructions(treeNode->AsOp());
            break;

        case GT_CMPXCHG:
            genCodeForCmpXchg(treeNode->AsCmpXchg());
            break;

        case GT_RELOAD:
            // do nothing - reload is just a marker.
            // The parent node will call genConsumeReg on this which will trigger the unspill of this node's child
            // into the register specified in this node.
            break;

        case GT_NOP:
            break;

        case GT_KEEPALIVE:
            if (treeNode->AsOp()->gtOp1->isContained())
            {
                // For this case we simply need to update the lifetime of the local.
                genUpdateLife(treeNode->AsOp()->gtOp1);
            }
            else
            {
                genConsumeReg(treeNode->AsOp()->gtOp1);
            }
            break;

        case GT_NO_OP:
            instGen(INS_nop);
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            genRangeCheck(treeNode);
            break;

        case GT_PHYSREG:
            genCodeForPhysReg(treeNode->AsPhysReg());
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

        case GT_PINVOKE_PROLOG:
            noway_assert(((gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & ~fullIntArgRegMask()) == 0);

            // the runtime side requires the codegen here to be consistent
#ifdef PSEUDORANDOM_NOP_INSERTION
            emit->emitDisableRandomNops();
#endif // PSEUDORANDOM_NOP_INSERTION
            break;

        case GT_LABEL:
            genPendingCallLabel = genCreateTempLabel();
            emit->emitIns_R_L(INS_ld_d, EA_PTRSIZE, genPendingCallLabel, targetReg);
            break;

        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
        case GT_STORE_BLK:
            genCodeForStoreBlk(treeNode->AsBlk());
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

        case GT_IL_OFFSET:
            // Do nothing; these nodes are simply markers for debug info.
            break;

        default:
        {
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, ArrLen(message), _TRUNCATE, "NYI: Unimplemented node type %s",
                        GenTree::OpName(treeNode->OperGet()));
            NYIRAW(message);
#else
            NYI("unimplemented node");
#endif
        }
        break;
    }
}

//------------------------------------------------------------------------
// genSetRegToIcon: Generate code that will set the given register to the integer constant.
//
void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type)
{
    // Reg cannot be a FP reg
    assert(!genIsValidFloatReg(reg));

    // The only TYP_REF constant that can come this path is a managed 'null' since it is not
    // relocatable.  Other ref type constants (e.g. string objects) go through a different
    // code path.
    noway_assert(type != TYP_REF || val == 0);

    GetEmitter()->emitIns_I_la(emitActualTypeSize(type), reg, val);
    regSet.verifyRegUsed(reg);
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
    assert(compiler->compGeneratingProlog);

    if (!compiler->getNeedsGSSecurityCookie())
    {
        return;
    }

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
        noway_assert(compiler->gsGlobalSecurityCookieVal != 0);
        // initReg = #GlobalSecurityCookieVal; [frame.GSSecurityCookie] = initReg
        genSetRegToIcon(initReg, compiler->gsGlobalSecurityCookieVal, TYP_I_IMPL);
        GetEmitter()->emitIns_S_R(INS_st_d, EA_PTRSIZE, initReg, compiler->lvaGSSecurityCookie, 0);
    }
    else
    {
        //instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, initReg, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        //GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, initReg, initReg, 0);
        if (compiler->opts.compReloc)
        {
            GetEmitter()->emitIns_R_AI(INS_bl, EA_PTR_DSP_RELOC, initReg, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        }
        else
        {
            //GetEmitter()->emitIns_R_I(INS_pcaddu12i, EA_PTRSIZE, initReg, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
            //GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, initReg, initReg, );
            GetEmitter()->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, initReg, ((ssize_t)compiler->gsGlobalSecurityCookieAddr & 0xfffff000)>>12);
            GetEmitter()->emitIns_R_I(INS_lu32i_d, EA_PTRSIZE, initReg, (ssize_t)compiler->gsGlobalSecurityCookieAddr >> 32);
            GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, initReg, initReg, ((ssize_t)compiler->gsGlobalSecurityCookieAddr & 0xfff)>>2);
        }
        regSet.verifyRegUsed(initReg);
        GetEmitter()->emitIns_S_R(INS_st_d, EA_PTRSIZE, initReg, compiler->lvaGSSecurityCookie, 0);
    }

    *pInitRegZeroed = false;
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
void CodeGen::genIntrinsic(GenTree* treeNode)
{
    assert(!"unimplemented on LOONGARCH yet");
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
    GenTree*  source     = treeNode->gtOp1;
    var_types targetType = genActualType(source->TypeGet());
    emitter*  emit       = GetEmitter();

    // This is the varNum for our store operations,
    // typically this is the varNum for the Outgoing arg space
    // When we are generating a tail call it will be the varNum for arg0
    unsigned varNumOut    = (unsigned)-1;
    unsigned argOffsetMax = (unsigned)-1; // Records the maximum size of this area for assert checks

    // Get argument offset to use with 'varNumOut'
    // Here we cross check that argument offset hasn't changed from lowering to codegen since
    // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
    unsigned argOffsetOut = treeNode->getArgOffset();

#ifdef DEBUG
    fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(treeNode->gtCall, treeNode);
    assert(curArgTabEntry != nullptr);
    DEBUG_ARG_SLOTS_ASSERT(argOffsetOut == (curArgTabEntry->slotNum * TARGET_POINTER_SIZE));
#endif // DEBUG

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
        LclVarDsc* varDsc = &(compiler->lvaTable[varNumOut]);
        assert(varDsc != nullptr);
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        varNumOut    = compiler->lvaOutgoingArgSpaceVar;
        argOffsetMax = compiler->lvaOutgoingArgSpaceSize;
    }

    bool isStruct = (targetType == TYP_STRUCT) || (source->OperGet() == GT_FIELD_LIST);

    if (!isStruct) // a normal non-Struct argument
    {
        if (varTypeIsSIMD(targetType))
        {
            assert(!"unimplemented on LOONGARCH yet");
        }

        instruction storeIns  = ins_Store(targetType);
        emitAttr    storeAttr = emitTypeSize(targetType);

        // If it is contained then source must be the integer constant zero
        if (source->isContained())
        {
            assert(source->OperGet() == GT_CNS_INT);
            assert(source->AsIntConCommon()->IconValue() == 0);

            emit->emitIns_S_R(storeIns, storeAttr, REG_R0, varNumOut, argOffsetOut);
        }
        else
        {
            genConsumeReg(source);
            if (storeIns == INS_st_w)
            {
                emit->emitIns_R_R_R(INS_add_w, EA_4BYTE, source->GetRegNum(), source->GetRegNum(), REG_R0);
                storeIns = INS_st_d;
                storeAttr = EA_8BYTE;
            }
            emit->emitIns_S_R(storeIns, storeAttr, source->GetRegNum(), varNumOut, argOffsetOut);
        }
        argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
        assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
    }
    else // We have some kind of a struct argument
    {
        assert(source->isContained()); // We expect that this node was marked as contained in Lower

        if (source->OperGet() == GT_FIELD_LIST)
        {
            genPutArgStkFieldList(treeNode, varNumOut);
        }
        else // We must have a GT_OBJ or a GT_LCL_VAR
        {
            noway_assert((source->OperGet() == GT_LCL_VAR) || (source->OperGet() == GT_OBJ));

            var_types targetType = source->TypeGet();
            noway_assert(varTypeIsStruct(targetType));

            // Setup loReg from the internal registers that we reserved in lower.
            //
            regNumber loReg = treeNode->ExtractTempReg();
            regNumber addrReg = REG_NA;

            GenTreeLclVarCommon* varNode  = nullptr;
            GenTree*             addrNode = nullptr;

            if (source->OperGet() == GT_LCL_VAR)
            {
                varNode = source->AsLclVarCommon();
            }
            else // we must have a GT_OBJ
            {
                assert(source->OperGet() == GT_OBJ);

                addrNode = source->AsOp()->gtOp1;

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
                else // addrNode is used
                {
                    // Generate code to load the address that we need into a register
                    genConsumeAddress(addrNode);
                    addrReg = addrNode->GetRegNum();
                }
            }

            // Either varNode or addrNOde must have been setup above,
            // the xor ensures that only one of the two is setup, not both
            assert((varNode != nullptr) ^ (addrNode != nullptr));

            ClassLayout* layout;

            //unsigned gcPtrCount; // The count of GC pointers in the struct
            unsigned srcSize;
            bool     isHfa;

            //gcPtrCount = treeNode->gtNumSlots;
            // Setup the srcSize and layout
            if (source->OperGet() == GT_LCL_VAR)
            {
                assert(varNode != nullptr);
                LclVarDsc* varDsc = compiler->lvaGetDesc(varNode);

                // This struct also must live in the stack frame
                // And it can't live in a register (SIMD)
                assert(varDsc->lvType == TYP_STRUCT);
                assert(varDsc->lvOnFrame && !varDsc->lvRegister);

                srcSize = varDsc->lvSize(); // This yields the roundUp size, but that is fine
                                               // as that is how much stack is allocated for this LclVar
                layout  = varDsc->GetLayout();
            }
            else // we must have a GT_OBJ
            {
                assert(source->OperGet() == GT_OBJ);

                // If the source is an OBJ node then we need to use the type information
                // it provides (size and GC layout) even if the node wraps a lclvar. Due
                // to struct reinterpretation (e.g. Unsafe.As<X, Y>) it is possible that
                // the OBJ node has a different type than the lclvar.
                CORINFO_CLASS_HANDLE objClass = source->AsObj()->GetLayout()->GetClassHandle();

                srcSize = compiler->info.compCompHnd->getClassSize(objClass);
                layout  = source->AsObj()->GetLayout();
            }

            unsigned structSize;

            unsigned dstSize = treeNode->GetStackByteSize();
            if (dstSize != srcSize)
            {
                // We can generate a smaller code if store size is a multiple of TARGET_POINTER_SIZE.
                // The dst size can be rounded up to PUTARG_STK size.
                // The src size can be rounded up if it reads a local variable slot because the local
                // variable stack allocation size is rounded up to be a multiple of the TARGET_POINTER_SIZE.
                // The exception  is arm64 apple arguments because they can be passed without padding.
                if (varNode != nullptr)
                {
                    // If we have a varNode, even if it was casted using `OBJ`, we can read its original memory size.
                    const LclVarDsc* varDsc       = compiler->lvaGetDesc(varNode);
                    const unsigned   varStackSize = varDsc->lvSize();
                    if (varStackSize >= srcSize)
                    {
                        srcSize = varStackSize;
                    }
                }
            }
            if (dstSize == srcSize)
            {
                structSize = dstSize;
            }
            else
            {
                // With Unsafe object wwe can have different strange combinations:
                // PutArgStk<8>(Obj<16>(LclVar<8>)) -> copy 8 bytes;
                // PutArgStk<16>(Obj<16>(LclVar<8>)) -> copy 16 bytes, reading undefined memory after the local.
                structSize = min(dstSize, srcSize);
            }

            int      remainingSize = structSize;
            unsigned structOffset  = 0;
            unsigned nextIndex     = 0;

            while (remainingSize > 0)
            {
                var_types type;

                if (remainingSize >= TARGET_POINTER_SIZE)
                {
                    type = layout->GetGCPtrType(nextIndex);
                }
                else // (remainingSize < TARGET_POINTER_SIZE)
                {
                    // the left over size is smaller than a pointer and thus can never be a GC type
                    assert(!layout->IsGCPtr(nextIndex));

                    if (remainingSize == 1)
                    {
                        type = TYP_UBYTE;
                    }
                    else if (remainingSize == 2)
                    {
                        type = TYP_USHORT;
                    }
                    else
                    {
                        assert(remainingSize == 4);
                        type = TYP_UINT;
                    }
                }
                const emitAttr attr     = emitTypeSize(type);
                const unsigned moveSize = genTypeSize(type);
                assert(EA_SIZE_IN_BYTES(attr) == moveSize);

                remainingSize -= moveSize;

                instruction loadIns = ins_Load(type);
                if (varNode != nullptr)
                {
                    // Load from our varNumImp source
                    emit->emitIns_R_S(loadIns, attr, loReg, varNode->GetLclNum(), structOffset);
                }
                else
                {
                    assert(loReg != addrReg);
                    // Load from our address expression source
                    emit->emitIns_R_R_I(loadIns, attr, loReg, addrReg, structOffset);
                }

                // Emit a store instruction to store the register into the outgoing argument area
                instruction storeIns = ins_Store(type);
                emit->emitIns_S_R(storeIns, attr, loReg, varNumOut, argOffsetOut);
                argOffsetOut += moveSize;
                assert(argOffsetOut <= argOffsetMax); // We can't write beyond the outgoing arg area

                structOffset += moveSize;
                nextIndex++;
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

    // If child node is not already in the register we need, move it
    if (targetReg != op1->GetRegNum())
    {
        if (emitter::isFloatReg(targetReg) == emitter::isFloatReg(op1->GetRegNum()))
            inst_RV_RV(ins_Copy(targetType), targetReg, op1->GetRegNum(), targetType);
#if 1
        else if (emitter::isFloatReg(targetReg))
            GetEmitter()->emitIns_R_R(INS_movgr2fr_d, EA_8BYTE, targetReg, op1->GetRegNum());
        else //if (!emitter::isFloatReg(targetReg))
        {
            assert(!emitter::isFloatReg(targetReg));
            GetEmitter()->emitIns_R_R(INS_movfr2gr_d, EA_8BYTE, targetReg, op1->GetRegNum());
        }
#endif
    }
    genProduceReg(tree);
}

#if FEATURE_ARG_SPLIT
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
                var_types type = nextArgNode->TypeGet();
                emitAttr  attr = emitTypeSize(type);

                unsigned offset = treeNode->getArgOffset() + use.GetOffset() - firstOnStackOffs;
                // We can't write beyond the outgoing arg area
                assert(offset + EA_SIZE_IN_BYTES(attr) <= argOffsetMax);

                // Emit store instructions to store the registers produced by the GT_FIELD_LIST into the outgoing
                // argument area
                emit->emitIns_S_R(ins_Store(type), attr, fieldReg, varNumOut, offset);
            }
            else
            {
                var_types type   = treeNode->GetRegType(regIndex);
                regNumber argReg = treeNode->GetRegNumByIdx(regIndex);

                // If child node is not already in the register we need, move it
                if (argReg != fieldReg)
                {
                    inst_RV_RV(ins_Copy(type), argReg, fieldReg, type);
                }
                regIndex++;
            }
        }
    }
    else
    {
        var_types targetType = source->TypeGet();
        assert(source->OperGet() == GT_OBJ);
        assert(varTypeIsStruct(targetType));

        regNumber baseReg = treeNode->ExtractTempReg();
        regNumber addrReg = REG_NA;

        GenTreeLclVarCommon* varNode  = nullptr;
        GenTree*             addrNode = nullptr;

        addrNode = source->AsOp()->gtOp1;

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

        // Either varNode or addrNOde must have been setup above,
        // the xor ensures that only one of the two is setup, not both
        assert((varNode != nullptr) ^ (addrNode != nullptr));

        // This is the varNum for our load operations,
        // only used when we have a struct with a LclVar source
        unsigned srcVarNum = BAD_VAR_NUM;

        if (varNode != nullptr)
        {
            assert(varNode->isContained());
            srcVarNum = varNode->GetLclNum();
            assert(srcVarNum < compiler->lvaCount);

            // handle promote situation
            LclVarDsc* varDsc = compiler->lvaTable + srcVarNum;

            // This struct also must live in the stack frame
            // And it can't live in a register (SIMD)
            assert(varDsc->lvType == TYP_STRUCT);
            assert(varDsc->lvOnFrame && !varDsc->lvRegister);

            // We don't split HFA struct
            assert(!varDsc->lvIsHfa());
        }
        else // addrNode is used
        {
            assert(addrNode != nullptr);
            // TODO-Cleanup: `Lowering::NewPutArg` marks only `LCL_VAR_ADDR` as contained nowadays,
            // Generate code to load the address that we need into a register
            genConsumeAddress(addrNode);
            addrReg = addrNode->GetRegNum();

            // If addrReg equal to baseReg, we use the last target register as alternative baseReg.
            // Because the candidate mask for the internal baseReg does not include any of the target register,
            // we can ensure that baseReg, addrReg, and the last target register are not all same.
            assert(baseReg != addrReg);

            // We don't split HFA struct
            assert(!compiler->IsHfa(source->AsObj()->GetLayout()->GetClassHandle()));
       }

        ClassLayout* layout = source->AsObj()->GetLayout();

        // Put on stack first
        unsigned nextIndex     = treeNode->gtNumRegs;
        unsigned structOffset  = nextIndex * TARGET_POINTER_SIZE;
        int      remainingSize = treeNode->GetStackByteSize();
        unsigned argOffsetOut  = treeNode->getArgOffset();

        // remainingSize is always multiple of TARGET_POINTER_SIZE
        assert(remainingSize % TARGET_POINTER_SIZE == 0);
        while (remainingSize > 0)
        {
            var_types type = layout->GetGCPtrType(nextIndex);

            if (varNode != nullptr)
            {
                // Load from our varNumImp source
                emit->emitIns_R_S(INS_ld_d, emitTypeSize(type), baseReg, srcVarNum, structOffset);
            }
            else
            {
                // check for case of destroying the addrRegister while we still need it
                assert(baseReg != addrReg);

                // Load from our address expression source
                emit->emitIns_R_R_I(INS_ld_d, emitTypeSize(type), baseReg, addrReg, structOffset);
            }

            // Emit str instruction to store the register into the outgoing argument area
            emit->emitIns_S_R(INS_st_d, emitTypeSize(type), baseReg, varNumOut, argOffsetOut);

            argOffsetOut += TARGET_POINTER_SIZE;  // We stored 4-bytes of the struct
            assert(argOffsetOut <= argOffsetMax); // We can't write beyond the outgoing arg area
            remainingSize -= TARGET_POINTER_SIZE; // We loaded 4-bytes of the struct
            structOffset += TARGET_POINTER_SIZE;
            nextIndex += 1;
        }

        // We set up the registers in order, so that we assign the last target register `baseReg` is no longer in use,
        // in case we had to reuse the last target register for it.
        structOffset = 0;
        for (unsigned idx = 0; idx < treeNode->gtNumRegs; idx++)
        {
            regNumber targetReg = treeNode->GetRegNumByIdx(idx);
            var_types type      = treeNode->GetRegType(idx);

            if (varNode != nullptr)
            {
                // Load from our varNumImp source
                emit->emitIns_R_S(ins_Load(type), emitTypeSize(type), targetReg, srcVarNum, structOffset);
            }
            else
            {
                // check for case of destroying the addrRegister while we still need it
                if (targetReg == addrReg && idx != treeNode->gtNumRegs - 1)
                {
                    assert(targetReg != baseReg);
                    emit->emitIns_R_R_I(INS_ori, emitActualTypeSize(type), baseReg, addrReg, 0);
                    addrReg = baseReg;
                }

                // Load from our address expression source
                emit->emitIns_R_R_I(ins_Load(type), emitTypeSize(type), targetReg, addrReg, structOffset);
            }
            structOffset += TARGET_POINTER_SIZE;
        }
    }
    genProduceReg(treeNode);
}
#endif // FEATURE_ARG_SPLIT

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
void CodeGen::genMultiRegCallStoreToLocal(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_STORE_LCL_VAR);

    // Structs of size >=9 and <=16 are returned in two return registers on LOONGARCH64 and HFAs.
    assert(varTypeIsStruct(treeNode));

    // Assumption: current implementation requires that a multi-reg
    // var in 'var = call' is flagged as lvIsMultiRegRet to prevent it from
    // being promoted.
    unsigned   lclNum = treeNode->AsLclVarCommon()->GetLclNum();
    LclVarDsc* varDsc = &(compiler->lvaTable[lclNum]);
    noway_assert(varDsc->lvIsMultiRegRet);

    GenTree*     op1       = treeNode->gtGetOp1();
    GenTree*     actualOp1 = op1->gtSkipReloadOrCopy();
    GenTreeCall* call      = actualOp1->AsCall();
    assert(call->HasMultiRegRetVal());

    genConsumeRegs(op1);

    const ReturnTypeDesc* pRetTypeDesc = call->GetReturnTypeDesc();
    unsigned        regCount     = pRetTypeDesc->GetReturnRegCount();

    if (treeNode->GetRegNum() != REG_NA)
    {
        assert(!"unimplemented on LOONGARCH yet");
        // Right now the only enregistrable multi-reg return types supported are SIMD types.
        assert(varTypeIsSIMD(treeNode));
        assert(regCount != 0);

        regNumber dst = treeNode->GetRegNum();

        // Treat dst register as a homogenous vector with element size equal to the src size
        // Insert pieces in reverse order
        for (int i = regCount - 1; i >= 0; --i)
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
            if (varTypeIsFloating(type))
            {
                // If the register piece was passed in a floating point register
                // Use a vector mov element instruction
                // src is not a vector, so it is in the first element reg[0]
                // mov dst[i], reg[0]
                // This effectively moves from `reg[0]` to `dst[i]`, leaving other dst bits unchanged till further
                // iterations
                // For the case where reg == dst, if we iterate so that we write dst[0] last, we eliminate the need for
                // a temporary
                GetEmitter()->emitIns_R_R_I_I(INS_mov, emitTypeSize(type), dst, reg, i, 0);
            }
            else
            {
                // If the register piece was passed in an integer register
                // Use a vector mov from general purpose register instruction
                // mov dst[i], reg
                // This effectively moves from `reg` to `dst[i]`
                GetEmitter()->emitIns_R_R_I(INS_mov, emitTypeSize(type), dst, reg, i);
            }
        }

        genProduceReg(treeNode);
    }
    else
    {
        // Stack store
        int offset = 0;
        var_types type = pRetTypeDesc->GetReturnRegType(0);
        regNumber reg  = call->GetRegNumByIdx(0);
        if (op1->IsCopyOrReload())
        {
            // GT_COPY/GT_RELOAD will have valid reg for those positions
            // that need to be copied or reloaded.
            regNumber reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(0);
            if (reloadReg != REG_NA)
            {
                reg = reloadReg;
            }
        }

        assert(reg != REG_NA);
        GetEmitter()->emitIns_S_R(ins_Store(type), emitTypeSize(type), reg, lclNum, offset);

        if (1 < regCount)
        {
            offset = genTypeSize(type);
            type = pRetTypeDesc->GetReturnRegType(1);
            reg  = call->GetRegNumByIdx(1);
            offset = offset < genTypeSize(type) ? genTypeSize(type) : offset;
            GetEmitter()->emitIns_S_R(ins_Store(type), emitTypeSize(type), reg, lclNum, offset);
        }

        genUpdateLife(treeNode);
        varDsc->SetRegNum(REG_STK);
    }
}

//------------------------------------------------------------------------
// genRangeCheck: generate code for GT_ARR_BOUNDS_CHECK node.
//
void CodeGen::genRangeCheck(GenTree* oper)
{
    noway_assert(oper->OperIsBoundsCheck());
    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTree* arrLen    = bndsChk->GetArrayLength();
    GenTree* arrIndex  = bndsChk->GetIndex();
    GenTree* arrRef    = NULL;
    int      lenOffset = 0;

    GenTree*     src1;
    GenTree*     src2;
    regNumber    reg1;
    regNumber    reg2;
    emitJumpKind jmpKind = EJ_jmp;

    genConsumeRegs(arrIndex);
    genConsumeRegs(arrLen);

    emitter* emit = GetEmitter();
    GenTreeIntConCommon* intConst = nullptr;
    if (arrIndex->isContainedIntOrIImmed())
    {
        src1 = arrLen;
        src2 = arrIndex;
        reg1 = REG_R21;
        reg2 = src1->GetRegNum();

        intConst = src2->AsIntConCommon();
        ssize_t imm = intConst->IconValue();
        if (imm == INT64_MAX)
        {
            emit->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_R21, REG_R0, -1);
            emit->emitIns_R_R_I(INS_srli_d, EA_PTRSIZE, REG_R21, REG_R21, 1);
        }
        else
        {
            emit->emitIns_I_la(EA_PTRSIZE, REG_R21, imm);
        }
    }
    else
    {
        src1 = arrIndex;
        src2 = arrLen;
        reg1 = src1->GetRegNum();

        if (src2->isContainedIntOrIImmed())
        {
            reg2 = REG_R21;
            ssize_t imm = src2->AsIntConCommon()->IconValue();
            emit->emitIns_I_la(EA_PTRSIZE, REG_R21, imm);
        }
        else
        {
            reg2 = src2->GetRegNum();
        }
    }

#ifdef DEBUG
    var_types bndsChkType = genActualType(src2->TypeGet());
    var_types src1ChkType = genActualType(src1->TypeGet());
    // Bounds checks can only be 32 or 64 bit sized comparisons.
    assert(bndsChkType == TYP_INT || bndsChkType == TYP_LONG);
    assert(src1ChkType == TYP_INT || src1ChkType == TYP_LONG);
#endif // DEBUG

    genJumpToThrowHlpBlk_la(bndsChk->gtThrowKind, INS_bgeu, reg1, bndsChk->gtIndRngFailBB, reg2);
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
    assert(tree->OperIs(GT_PHYSREG));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->GetRegNum();

    if (targetReg != tree->gtSrcReg)
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, tree->gtSrcReg, targetType);
        genTransferRegGCState(targetReg, tree->gtSrcReg);
    }

    genProduceReg(tree);
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
    assert(!tree->gtOp1->isContained());
    regNumber addrReg = genConsumeReg(tree->gtOp1);

    regNumber targetReg = REG_R0;

    GetEmitter()->emitIns_R_R_I(INS_ld_w, EA_4BYTE, targetReg, addrReg, 0);
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
    emitter*  emit      = GetEmitter();
    GenTree*  arrObj    = arrIndex->ArrObj();
    GenTree*  indexNode = arrIndex->IndexExpr();
    regNumber arrReg    = genConsumeReg(arrObj);
    regNumber indexReg  = genConsumeReg(indexNode);
    regNumber tgtReg    = arrIndex->GetRegNum();
    noway_assert(tgtReg != REG_NA);

    // We will use a temp register to load the lower bound and dimension size values.

    //regNumber tmpReg = arrIndex->GetSingleTempReg();
    assert(tgtReg != REG_R21);

    unsigned  dim      = arrIndex->gtCurrDim;
    unsigned  rank     = arrIndex->gtArrRank;
    unsigned  offset;

    offset = compiler->eeGetMDArrayLowerBoundOffset(rank, dim);
    emit->emitIns_R_R_I(INS_ld_w, EA_4BYTE, REG_R21, arrReg, offset);
    emit->emitIns_R_R_R(INS_sub_w, EA_4BYTE, tgtReg, indexReg, REG_R21);

    offset = compiler->eeGetMDArrayLengthOffset(rank, dim);
    emit->emitIns_R_R_I(INS_ld_w, EA_4BYTE, REG_R21, arrReg, offset);
    genJumpToThrowHlpBlk_la(SCK_RNGCHK_FAIL, INS_bgeu, tgtReg, nullptr, REG_R21);

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
    GenTree*  offsetNode = arrOffset->gtOffset;
    GenTree*  indexNode  = arrOffset->gtIndex;
    regNumber tgtReg     = arrOffset->GetRegNum();

    noway_assert(tgtReg != REG_NA);

    if (!offsetNode->IsIntegralConst(0))
    {
        emitter*  emit      = GetEmitter();
        regNumber offsetReg = genConsumeReg(offsetNode);
        regNumber indexReg  = genConsumeReg(indexNode);
        regNumber arrReg    = genConsumeReg(arrOffset->gtArrObj);
        noway_assert(offsetReg != REG_NA);
        noway_assert(indexReg != REG_NA);
        noway_assert(arrReg != REG_NA);

        //regNumber tmpReg = arrOffset->GetSingleTempReg();

        unsigned  dim      = arrOffset->gtCurrDim;
        unsigned  rank     = arrOffset->gtArrRank;
        unsigned  offset   = compiler->eeGetMDArrayLengthOffset(rank, dim);

        // Load tmpReg with the dimension size and evaluate
        // tgtReg = offsetReg*tmpReg + indexReg.
        emit->emitIns_R_R_I(INS_ld_w, EA_4BYTE, REG_R21, arrReg, offset);
        emit->emitIns_R_R_R(INS_mul_d, EA_PTRSIZE, REG_R21, REG_R21, offsetReg);
        emit->emitIns_R_R_R(INS_add_d, EA_PTRSIZE, tgtReg, REG_R21, indexReg);
    }
    else
    {
        regNumber indexReg = genConsumeReg(indexNode);
        if (indexReg != tgtReg)
        {
            GetEmitter()->emitIns_R_R_I(INS_ori, emitActualTypeSize(TYP_INT), tgtReg, indexReg, 0);
        }
    }
    genProduceReg(arrOffset);
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
    //var_types   targetType = tree->TypeGet();
    //genTreeOps  oper       = tree->OperGet();
    instruction ins        = genGetInsForOper(tree);
    emitAttr    size       = emitActualTypeSize(tree);

    assert(tree->GetRegNum() != REG_NA);

    genConsumeOperands(tree->AsOp());

    GenTree* operand = tree->gtGetOp1();
    GenTree* shiftBy = tree->gtGetOp2();
    if (!shiftBy->IsCnsIntOrI())
    {
        GetEmitter()->emitIns_R_R_R(ins, size, tree->GetRegNum(), operand->GetRegNum(), shiftBy->GetRegNum());
    }
    else
    {
        unsigned shiftByImm = (unsigned)shiftBy->AsIntCon()->gtIconVal;

        //should check shiftByImm for loongarch32-ins.
        unsigned immWidth = emitter::getBitWidth(size); // For LOONGARCH64, immWidth will be set to 32 or 64
        shiftByImm &= (immWidth - 1);

        if (ins == INS_slli_w && shiftByImm >= 32)
        {
            ins = INS_slli_d;
        }
        else if (ins == INS_slli_d && shiftByImm >= 32 && shiftByImm < 64)
        {
            ins = INS_slli_d;
        }
        else if (ins == INS_srai_d && shiftByImm >= 32 && shiftByImm < 64)
        {
            ins = INS_srai_d;
        }
        else if (ins == INS_srli_d && shiftByImm >= 32 && shiftByImm < 64)
        {
            ins = INS_srli_d;
        }
        else if (ins == INS_rotri_d && shiftByImm >= 32 && shiftByImm < 64)
        {
            ins = INS_rotri_d;
        }

        GetEmitter()->emitIns_R_R_I(ins, size, tree->GetRegNum(), operand->GetRegNum(), shiftByImm);
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForLclAddr: Generates the code for GT_LCL_FLD_ADDR/GT_LCL_VAR_ADDR.
//
// Arguments:
//    tree - the node.
//
void CodeGen::genCodeForLclAddr(GenTree* tree)
{
    assert(tree->OperIs(GT_LCL_FLD_ADDR, GT_LCL_VAR_ADDR));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->GetRegNum();

    // Address of a local var.
    noway_assert((targetType == TYP_BYREF) || (targetType == TYP_I_IMPL));

    emitAttr size = emitTypeSize(targetType);

    inst_RV_TT(INS_lea, targetReg, tree, 0, size);
    genProduceReg(tree);
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

    emitAttr size   = emitTypeSize(targetType);
    unsigned offs   = tree->GetLclOffs();
    unsigned varNum = tree->GetLclNum();
    assert(varNum < compiler->lvaCount);

    emit->emitIns_R_S(ins_Load(targetType), size, targetReg, varNum, offs);

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

    //const regNumber tmpReg = node->GetSingleTempReg();

    // Generate the bounds check if necessary.
    if ((node->gtFlags & GTF_INX_RNGCHK) != 0)
    {
        GetEmitter()->emitIns_R_R_I(INS_ld_w, EA_4BYTE, REG_R21, base->GetRegNum(), node->gtLenOffset);
        //   if (index >= REG_R21)
        //   {
        //     JumpToThrowHlpBlk;
        //   }
        //
        //   sltu  AT, index, REG_R21
        //   bne  AT, zero, RngChkExit
        // IndRngFail:
        //   ...
        // RngChkExit:
        genJumpToThrowHlpBlk_la(SCK_RNGCHK_FAIL, INS_bgeu, index->GetRegNum(), node->gtIndRngFailBB, REG_R21);
    }

    emitAttr attr = emitActualTypeSize(node);
    // Can we use a ScaledAdd instruction?
    //
    if (isPow2(node->gtElemSize) && (node->gtElemSize <= 2048))
    {
        DWORD scale;
        BitScanForward(&scale, node->gtElemSize);

        // dest = base + index * scale
        genScaledAdd(emitActualTypeSize(node), node->GetRegNum(), base->GetRegNum(), index->GetRegNum(), scale);
    }
    else // we have to load the element size and use a MADD (multiply-add) instruction
    {
        // REG_R21 = element size
        CodeGen::genSetRegToIcon(REG_R21, (ssize_t)node->gtElemSize, TYP_INT);

        // dest = index * REG_R21 + base
        if (attr == EA_4BYTE)
        {
            GetEmitter()->emitIns_R_R_R(INS_mul_w, EA_4BYTE, REG_R21, index->GetRegNum(), REG_R21);
            GetEmitter()->emitIns_R_R_R(INS_add_w, attr, node->GetRegNum(), REG_R21, base->GetRegNum());
        }
        else
        {
            GetEmitter()->emitIns_R_R_R(INS_mul_d, EA_PTRSIZE, REG_R21, index->GetRegNum(), REG_R21);
            GetEmitter()->emitIns_R_R_R(INS_add_d, attr, node->GetRegNum(), REG_R21, base->GetRegNum());
        }
    }

    // dest = dest + elemOffs
    GetEmitter()->emitIns_R_R_I(INS_addi_d, attr, node->GetRegNum(), node->GetRegNum(), node->gtElemOffset);

    gcInfo.gcMarkRegSetNpt(base->gtGetRegMask());

    genProduceReg(node);
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
    // Handling of Vector3 type values loaded through indirection.
    if (tree->TypeGet() == TYP_SIMD12)
    {
        genLoadIndTypeSIMD12(tree);
        return;
    }
#endif // FEATURE_SIMD

    var_types   type      = tree->TypeGet();
    instruction ins       = ins_Load(type);
    instruction ins2      = INS_none;
    regNumber   targetReg = tree->GetRegNum();
    regNumber   tmpReg = targetReg;
    emitAttr    attr = emitActualTypeSize(type);
    int offset = 0;

    genConsumeAddress(tree->Addr());

    if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
    {
        instGen_MemoryBarrier(BARRIER_FULL);
    }

    GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), targetReg, tree);

    genProduceReg(tree);
}

//----------------------------------------------------------------------------------
// genCodeForCpBlkHelper - Generate code for a CpBlk node by the means of the VM memcpy helper call
//
// Arguments:
//    cpBlkNode - the GT_STORE_[BLK|OBJ|DYN_BLK]
//
// Preconditions:
//   The register assignments have been set appropriately.
//   This is validated by genConsumeBlockOp().
//
void CodeGen::genCodeForCpBlkHelper(GenTreeBlk* cpBlkNode)
{
    // Destination address goes in arg0, source address goes in arg1, and size goes in arg2.
    // genConsumeBlockOp takes care of this for us.
    genConsumeBlockOp(cpBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile CpBlk operation
        instGen_MemoryBarrier();
    }

    genEmitHelperCall(CORINFO_HELP_MEMCPY, 0, EA_UNKNOWN);

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a INS_BARRIER_RMB after a volatile CpBlk operation
        instGen_MemoryBarrier(BARRIER_FULL);
    }
}

//----------------------------------------------------------------------------------
// genCodeForCpBlkUnroll: Generates CpBlk code by performing a loop unroll
//
// Arguments:
//    cpBlkNode  -  Copy block node
//
// Return Value:
//    None
//
// Assumption:
//  The size argument of the CpBlk node is a constant and <= CPBLK_UNROLL_LIMIT bytes.
//
void CodeGen::genCodeForCpBlkUnroll(GenTreeBlk* cpBlkNode)
{
    assert(cpBlkNode->OperIs(GT_STORE_BLK));

    unsigned  dstLclNum      = BAD_VAR_NUM;
    regNumber dstAddrBaseReg = REG_NA;
    int       dstOffset      = 0;
    GenTree*  dstAddr        = cpBlkNode->Addr();

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
        assert(dstAddr->OperIsLocalAddr());
        dstLclNum = dstAddr->AsLclVarCommon()->GetLclNum();
        dstOffset = dstAddr->AsLclVarCommon()->GetLclOffs();
    }

    unsigned  srcLclNum      = BAD_VAR_NUM;
    regNumber srcAddrBaseReg = REG_NA;
    int       srcOffset      = 0;
    GenTree*  src            = cpBlkNode->Data();

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
            assert(srcAddr->OperIsLocalAddr());
            srcLclNum = srcAddr->AsLclVarCommon()->GetLclNum();
            srcOffset = srcAddr->AsLclVarCommon()->GetLclOffs();
        }
    }

    if (cpBlkNode->IsVolatile())
    {
        // issue a full memory barrier before a volatile CpBlk operation
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();
    unsigned size = cpBlkNode->GetLayout()->GetSize();

    assert(size <= INT32_MAX);
    assert(srcOffset < INT32_MAX - static_cast<int>(size));
    assert(dstOffset < INT32_MAX - static_cast<int>(size));

    regNumber tempReg = cpBlkNode->ExtractTempReg(RBM_ALLINT);

    if (size >= 2 * REGSIZE_BYTES)
    {
        regNumber tempReg2 = REG_R21;//cpBlkNode->ExtractTempReg(RBM_ALLINT);//TODO:should amend.

        for (unsigned regSize = 2 * REGSIZE_BYTES; size >= regSize;
             size -= regSize, srcOffset += regSize, dstOffset += regSize)
        {
            if (srcLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_R_S(INS_ld_d, EA_8BYTE, tempReg, srcLclNum, srcOffset);
                emit->emitIns_R_S(INS_ld_d, EA_8BYTE, tempReg2, srcLclNum, srcOffset + 8);
            }
            else
            {
                emit->emitIns_R_R_I(INS_ld_d, EA_8BYTE, tempReg, srcAddrBaseReg, srcOffset);
                emit->emitIns_R_R_I(INS_ld_d, EA_8BYTE, tempReg2, srcAddrBaseReg, srcOffset + 8);
            }

            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_R(INS_st_d, EA_8BYTE, tempReg, dstLclNum, dstOffset);
                emit->emitIns_S_R(INS_st_d, EA_8BYTE, tempReg2, dstLclNum, dstOffset + 8);
            }
            else
            {
                emit->emitIns_R_R_I(INS_st_d, EA_8BYTE, tempReg, dstAddrBaseReg, dstOffset);
                emit->emitIns_R_R_I(INS_st_d, EA_8BYTE, tempReg2, dstAddrBaseReg, dstOffset + 8);
            }
        }
    }

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
                loadIns  = INS_ld_b;
                storeIns = INS_st_b;
                attr     = EA_4BYTE;
                break;
            case 2:
                loadIns  = INS_ld_h;
                storeIns = INS_st_h;
                attr     = EA_4BYTE;
                break;
            case 4:
                loadIns  = INS_ld_w;
                storeIns = INS_st_w;
                attr     = EA_ATTR(regSize);
                break;
            case 8:
                loadIns  = INS_ld_d;
                storeIns = INS_st_d;
                attr     = EA_ATTR(regSize);
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

    if (cpBlkNode->IsVolatile())
    {
        // issue a load barrier after a volatile CpBlk operation
        instGen_MemoryBarrier(BARRIER_LOAD_ONLY);
    }
}

//------------------------------------------------------------------------
// genCodeForInitBlkHelper - Generate code for an InitBlk node by the means of the VM memcpy helper call
//
// Arguments:
//    initBlkNode - the GT_STORE_[BLK|OBJ|DYN_BLK]
//
// Preconditions:
//   The register assignments have been set appropriately.
//   This is validated by genConsumeBlockOp().
//
void CodeGen::genCodeForInitBlkHelper(GenTreeBlk* initBlkNode)
{
    // Size goes in arg2, source address goes in arg1, and size goes in arg2.
    // genConsumeBlockOp takes care of this for us.
    genConsumeBlockOp(initBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);

    if (initBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile initBlock Operation
        instGen_MemoryBarrier();
    }

    genEmitHelperCall(CORINFO_HELP_MEMSET, 0, EA_UNKNOWN);
}

// Generate code for a load from some address + offset
//   base: tree node which can be either a local address or arbitrary node
//   offset: distance from the base from which to load
void CodeGen::genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset)
{
    emitter* emit = GetEmitter();

    if (base->OperIsLocalAddr())
    {
        if (base->gtOper == GT_LCL_FLD_ADDR)
            offset += base->AsLclFld()->GetLclOffs();
        emit->emitIns_R_S(ins, size, dst, base->AsLclVarCommon()->GetLclNum(), offset);
    }
    else
    {
        emit->emitIns_R_R_I(ins, size, dst, base->GetRegNum(), offset);
    }
}

// Generate code for a store to some address + offset
//   base: tree node which can be either a local address or arbitrary node
//   offset: distance from the base from which to load
void CodeGen::genCodeForStoreOffset(instruction ins, emitAttr size, regNumber src, GenTree* base, unsigned offset)
{
    emitter* emit = GetEmitter();

    if (base->OperIsLocalAddr())
    {
        if (base->gtOper == GT_LCL_FLD_ADDR)
            offset += base->AsLclFld()->GetLclOffs();
        emit->emitIns_S_R(ins, size, src, base->AsLclVarCommon()->GetLclNum(), offset);
    }
    else
    {
        emit->emitIns_R_R_I(ins, size, src, base->GetRegNum(), offset);
    }
}

//------------------------------------------------------------------------
// genCallInstruction: Produce code for a GT_CALL node
//
void CodeGen::genCallInstruction(GenTreeCall* call)
{
    gtCallTypes callType = (gtCallTypes)call->gtCallType;

    DebugInfo di;

    // all virtuals should have been expanded into a control expression
    assert(!call->IsVirtual() || call->gtControlExpr || call->gtCallAddr);

    // Consume all the arg regs
    for (GenTreeCall::Use& use : call->LateArgs())
    {
        GenTree* argNode = use.GetNode();

        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        // GT_RELOAD/GT_COPY use the child node
        argNode = argNode->gtSkipReloadOrCopy();

        if (curArgTabEntry->GetRegNum() == REG_STK)
            continue;

        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            regNumber argReg = curArgTabEntry->GetRegNum();
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
                GenTree* putArgRegNode = use.GetNode();
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                genConsumeReg(putArgRegNode);
#if 0
                inst_Mov_Extend(putArgRegNode->TypeGet(), /* srcInReg */ true, argReg, putArgRegNode->GetRegNum(),
                                /* canSkip */ true, emitActualTypeSize(TYP_I_IMPL));

                argReg = genRegArgNext(argReg);
#endif
            }
        }
#if FEATURE_ARG_SPLIT
        else if (curArgTabEntry->IsSplit())
        {
            assert(curArgTabEntry->numRegs >= 1);
            genConsumeArgSplitStruct(argNode->AsPutArgSplit());
        }
#endif // FEATURE_ARG_SPLIT
        else
        {
            regNumber argReg = curArgTabEntry->GetRegNum();
            genConsumeReg(argNode);
            if (argNode->GetRegNum() != argReg)
            {
                inst_RV_RV(ins_Move_Extend(argNode->TypeGet(), true), argReg, argNode->GetRegNum());
            }
        }
    }

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);

        // Ditto as genCodeForNullCheck
        GetEmitter()->emitIns_R_R_I(INS_ld_w, EA_4BYTE, REG_R0, regThis, 0);
    }

    // Either gtControlExpr != null or gtCallAddr != null or it is a direct non-virtual call to a user or helper
    // method.
    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = call->gtControlExpr;
    if (callType == CT_INDIRECT)
    {
        assert(target == nullptr);
        target  = call->gtCallAddr;
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
    // and stack args in incoming arg area) and call target.  Epilog sequence would
    // generate "br <reg>".
    if (call->IsFastTailCall())
    {
        // Don't support fast tail calling JIT helpers
        assert(callType != CT_HELPER);

        if (target != nullptr)
        {
            // Indirect fast tail calls materialize call target either in gtControlExpr or in gtCallAddr.
            genConsumeReg(target);

            // Use REG_FASTTAILCALL_TARGET on LOONGARCH64 as the call target register.
            if (target->GetRegNum() != REG_FASTTAILCALL_TARGET)
            {
                GetEmitter()->emitIns_R_R_I(INS_ori, EA_4BYTE, REG_FASTTAILCALL_TARGET, target->GetRegNum(), 0);
            }
        }

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

    // Determine return value size(s).
    const ReturnTypeDesc* pRetTypeDesc  = call->GetReturnTypeDesc();
    emitAttr              retSize       = EA_PTRSIZE;
    emitAttr              secondRetSize = EA_UNKNOWN;

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

    // We need to propagate the IL offset information to the call instruction, so we can emit
    // an IL to native mapping record for the call, to support managed return value debugging.
    // We don't want tail call helper calls that were converted from normal calls to get a record,
    // so we skip this hash table lookup logic in that case.
    if (compiler->opts.compDbgInfo && compiler->genCallSite2DebugInfoMap != nullptr && !call->IsTailCall())
    {
        (void)compiler->genCallSite2DebugInfoMap->Lookup(call, &di);
    }

    if (target != nullptr)
    {
        // A call target can not be a contained indirection
        assert(!target->isContainedIndir());

        genConsumeReg(target);

        // We have already generated code for gtControlExpr evaluating it into a register.
        // We just need to emit "call reg" in this case.
        //
        assert(genIsValidIntReg(target->GetRegNum()));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), di,
                    target->GetRegNum(), call->IsFastTailCall());
    }
    else if (call->IsR2ROrVirtualStubRelativeIndir())
    {
        // Generate a direct call to a non-virtual user defined or helper method
        assert(callType == CT_HELPER || callType == CT_USER_FUNC);
#ifdef FEATURE_READYTORUN_COMPILER
        assert(((call->IsR2RRelativeIndir()) && (call->gtEntryPoint.accessType == IAT_PVALUE)) ||
               ((call->IsVirtualStubRelativeIndir()) && (call->gtEntryPoint.accessType == IAT_VALUE)));
#endif // FEATURE_READYTORUN_COMPILER
        assert(call->gtControlExpr == nullptr);
        assert(!call->IsTailCall());

        regNumber tmpReg = call->GetSingleTempReg();
        GetEmitter()->emitIns_R_R(ins_Load(TYP_I_IMPL), emitActualTypeSize(TYP_I_IMPL), tmpReg, REG_R2R_INDIRECT_PARAM);

        // We have now generated code for gtControlExpr evaluating it into `tmpReg`.
        // We just need to emit "call tmpReg" in this case.
        //
        assert(genIsValidIntReg(tmpReg));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), di, tmpReg, call->IsFastTailCall());
    }
    else
    {
        // Generate a direct call to a non-virtual user defined or helper method
        assert(callType == CT_HELPER || callType == CT_USER_FUNC);

        void* addr = nullptr;
#ifdef FEATURE_READYTORUN_COMPILER
        if (call->gtEntryPoint.addr != NULL)
        {
            assert(call->gtEntryPoint.accessType == IAT_VALUE);
            addr = call->gtEntryPoint.addr;
        }
        else
#endif // FEATURE_READYTORUN_COMPILER
            if (callType == CT_HELPER)
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

// Non-virtual direct call to known addresses
        {
            genEmitCall(emitter::EC_FUNC_TOKEN, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr,
                        retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), di, REG_R21, call->IsFastTailCall());
        }
    }

    // if it was a pinvoke we may have needed to get the address of a label
    if (genPendingCallLabel)
    {
        genDefineInlineTempLabel(genPendingCallLabel);
        genPendingCallLabel = nullptr;
    }

    // Update GC info:
    // All Callee arg registers are trashed and no longer contain any GC pointers.
    // TODO-Bug?: As a matter of fact shouldn't we be killing all of callee trashed regs here?
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
            if (varTypeUsesFloatArgReg(returnType))
            {
                returnReg = REG_FLOATRET;
            }
            else
            {
                returnReg = REG_INTRET;
            }

            if (call->GetRegNum() != returnReg)
            {
                {
                    inst_RV_RV(ins_Copy(returnType), call->GetRegNum(), returnReg, returnType);
                }
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
void CodeGen::genJmpMethod(GenTree* jmp)
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

        if (varDsc->lvIsRegArg && (varDsc->GetRegNum() != REG_STK))
        {
            // Skip reg args which are already in its right register for jmp call.
            // If not, we will spill such args to their stack locations.
            //
            // If we need to generate a tail call profiler hook, then spill all
            // arg regs to free them up for the callback.
            if (!compiler->compIsProfilerHookNeeded() && (varDsc->GetRegNum() == varDsc->GetArgReg()))
                continue;
        }
        else if (varDsc->GetRegNum() == REG_STK)
        {
            // Skip args which are currently living in stack.
            continue;
        }

        // If we came here it means either a reg argument not in the right register or
        // a stack argument currently living in a register.  In either case the following
        // assert should hold.
        assert(varDsc->GetRegNum() != REG_STK);
        assert(varDsc->TypeGet() != TYP_STRUCT);
        var_types storeType = genActualType(varDsc->TypeGet());
        emitAttr  storeSize = emitActualTypeSize(storeType);

        GetEmitter()->emitIns_S_R(ins_Store(storeType), storeSize, varDsc->GetRegNum(), varNum, 0);
        // Update GetRegNum() life and GC info to indicate GetRegNum() is dead and varDsc stack slot is going live.
        // Note that we cannot modify varDsc->GetRegNum() here because another basic block may not be expecting it.
        // Therefore manually update life of varDsc->GetRegNum().
        regMaskTP tempMask = genRegMask(varDsc->GetRegNum());
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
        regNumber argReg     = varDsc->GetArgReg(); // incoming arg register
        regNumber argRegNext = REG_NA;

        if (varDsc->GetRegNum() != argReg)
        {
            var_types loadType = TYP_UNDEF;

            //NOTE for LOONGARCH: not supports the HFA.
            assert(!varDsc->lvIsHfaRegArg());
            {
                if (varTypeIsStruct(varDsc))
                {
                    // Must be <= 16 bytes or else it wouldn't be passed in registers,
                    // which can be bigger (and is handled above).
                    noway_assert(EA_SIZE_IN_BYTES(varDsc->lvSize()) <= 16);
                    if (emitter::isFloatReg(argReg))
                    {
                        loadType = varDsc->lvIs4Field1 ? TYP_FLOAT : TYP_DOUBLE;
                    }
                    else
                        loadType = varDsc->GetLayout()->GetGCPtrType(0);
                }
                else
                {
                    loadType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));
                }

                emitAttr loadSize = emitActualTypeSize(loadType);
                GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argReg, varNum, 0);

                // Update argReg life and GC Info to indicate varDsc stack slot is dead and argReg is going live.
                // Note that we cannot modify varDsc->GetRegNum() here because another basic block may not be expecting it.
                // Therefore manually update life of argReg.  Note that GT_JMP marks the end of the basic block
                // and after which reg life and gc info will be recomputed for the new block in genCodeForBBList().
                regSet.AddMaskVars(genRegMask(argReg));
                gcInfo.gcMarkRegPtrVal(argReg, loadType);

                //if (compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
                if (varDsc->GetOtherArgReg() < REG_STK)
                {
                    // Restore the second register.
                    argRegNext = varDsc->GetOtherArgReg();

                    if (emitter::isFloatReg(argRegNext))
                    {
                        loadType = varDsc->lvIs4Field2 ? TYP_FLOAT : TYP_DOUBLE;
                    }
                    else
                        loadType = varDsc->GetLayout()->GetGCPtrType(1);

                    loadSize = emitActualTypeSize(loadType);
                    int offs = loadSize == EA_4BYTE ? 4 : 8;
                    GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argRegNext, varNum, offs);

                    regSet.AddMaskVars(genRegMask(argRegNext));
                    gcInfo.gcMarkRegPtrVal(argRegNext, loadType);
                }

                if (compiler->lvaIsGCTracked(varDsc))
                {
                    VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
                }
            }
        }

        if (compiler->info.compIsVarArgs)
        {
            assert(!"unimplemented on LOONGARCH yet!");
            // In case of a jmp call to a vararg method ensure only integer registers are passed.
            assert((genRegMask(argReg) & (RBM_ARG_REGS)) != RBM_NONE);
            assert(!varDsc->lvIsHfaRegArg());

            fixedIntArgMask |= genRegMask(argReg);

            if (compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
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

    // Jmp call to a vararg method - if the method has fewer than fixed arguments that can be max size of reg,
    // load the remaining integer arg registers from the corresponding
    // shadow stack slots.  This is for the reason that we don't know the number and type
    // of non-fixed params passed by the caller, therefore we have to assume the worst case
    // of caller passing all integer arg regs that can be max size of reg.
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
            GetEmitter()->emitDisableGC();
            for (int argNum = 0, argOffset = 0; argNum < MAX_REG_ARG; ++argNum)
            {
                regNumber argReg     = intArgRegs[argNum];
                regMaskTP argRegMask = genRegMask(argReg);

                if ((remainingIntArgMask & argRegMask) != 0)
                {
                    remainingIntArgMask &= ~argRegMask;
                    GetEmitter()->emitIns_R_S(INS_ld_d, EA_PTRSIZE, argReg, firstArgVarNum, argOffset);
                }

                argOffset += REGSIZE_BYTES;
            }
            GetEmitter()->emitEnableGC();
        }
    }
}

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
    switch (desc.CheckKind())
    {
        case GenIntCastDesc::CHECK_POSITIVE:
        {
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_blt, reg, nullptr, REG_R0);
        }
        break;

        case GenIntCastDesc::CHECK_UINT_RANGE:
        {
            // We need to check if the value is not greater than 0xFFFFFFFF
            // if the upper 32 bits are zero.
            ssize_t imm = -1;
            GetEmitter()->emitIns_R_R_I(INS_addi_d, EA_8BYTE, REG_R21, REG_R0, imm);

            GetEmitter()->emitIns_R_R_I(INS_slli_d, EA_8BYTE, REG_R21, REG_R21, 32);
            GetEmitter()->emitIns_R_R_R(INS_and, EA_8BYTE, REG_R21, reg, REG_R21);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, REG_R21);
        }
        break;

        case GenIntCastDesc::CHECK_POSITIVE_INT_RANGE:
        {
            // We need to check if the value is not greater than 0x7FFFFFFF
            // if the upper 33 bits are zero.
            //instGen_Set_Reg_To_Imm(EA_8BYTE, REG_R21, 0xFFFFFFFF80000000LL);
            ssize_t imm = -1;
            GetEmitter()->emitIns_R_R_I(INS_addi_d, EA_8BYTE, REG_R21, REG_R0, imm);

            GetEmitter()->emitIns_R_R_I(INS_slli_d, EA_8BYTE, REG_R21, REG_R21, 31);

            GetEmitter()->emitIns_R_R_R(INS_and, EA_8BYTE, REG_R21, reg, REG_R21);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, REG_R21);
        }
        break;

        case GenIntCastDesc::CHECK_INT_RANGE:
        {
            const regNumber tempReg = cast->GetSingleTempReg();
            assert(tempReg != reg);
            GetEmitter()->emitIns_I_la(EA_8BYTE, tempReg, INT32_MAX);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_blt, tempReg, nullptr, reg);

            GetEmitter()->emitIns_I_la(EA_8BYTE, tempReg, INT32_MIN);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_blt, reg, nullptr, tempReg);
        }
        break;

        default:
        {
            assert(desc.CheckKind() == GenIntCastDesc::CHECK_SMALL_INT_RANGE);
            const int castMaxValue = desc.CheckSmallIntMax();
            const int castMinValue = desc.CheckSmallIntMin();
            instruction ins;

            if (castMaxValue > 2047)
            {//should amend.   should confirm !?!?
                assert((castMaxValue == 32767) || (castMaxValue == 65535));
                GetEmitter()->emitIns_I_la(EA_ATTR(desc.CheckSrcSize()), REG_R21, castMaxValue + 1);
                ins = castMinValue == 0 ? INS_bgeu : INS_bge;
                genJumpToThrowHlpBlk_la(SCK_OVERFLOW, ins, reg, nullptr, REG_R21);
            }
            else
            {//should amend.
                GetEmitter()->emitIns_R_R_I(INS_addi_w, EA_ATTR(desc.CheckSrcSize()), REG_R21, REG_R0, castMaxValue);
                ins = castMinValue == 0 ? INS_bltu : INS_blt;
                genJumpToThrowHlpBlk_la(SCK_OVERFLOW, ins, REG_R21, nullptr, reg);
            }

            if (castMinValue != 0)
            {
                if ((-2048 <= castMinValue) && (castMinValue < 2048))
                {
                    GetEmitter()->emitIns_R_R_I(INS_slti, EA_ATTR(desc.CheckSrcSize()), REG_R21, reg, castMinValue);
                }
                else
                {
                    GetEmitter()->emitIns_I_la(EA_8BYTE, REG_R21, castMinValue);
                    GetEmitter()->emitIns_R_R_R(INS_slt, EA_ATTR(desc.CheckSrcSize()), REG_R21, reg, REG_R21);
                }
                genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, REG_R21);
            }
        }
        break;
    }
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer cast, with or without overflow check.
//
// Arguments:
//    cast - The GT_CAST node
//
// Assumptions:
//    The cast node is not a contained node and must have an assigned register.
//    Neither the source nor target type can be a floating point type.
//
// TODO-LOONGARCH64-CQ: Allow castOp to be a contained node without an assigned register.
//
void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    genConsumeRegs(cast->gtGetOp1());

    emitter* emit = GetEmitter();
    var_types dstType = cast->CastToType();
    var_types srcType = genActualType(cast->gtGetOp1()->TypeGet());
    const regNumber srcReg = cast->gtGetOp1()->GetRegNum();
    const regNumber dstReg = cast->GetRegNum();
    const unsigned char pos = 0;
    const unsigned char size = 32;

    assert(genIsValidIntReg(srcReg));
    assert(genIsValidIntReg(dstReg));

    GenIntCastDesc desc(cast);

    if (desc.CheckKind() != GenIntCastDesc::CHECK_NONE)
    {
        genIntCastOverflowCheck(cast, desc, srcReg);
    }

    //if ((EA_ATTR(genTypeSize(srcType)) == EA_8BYTE) && (EA_ATTR(genTypeSize(dstType)) == EA_4BYTE))
    //{
    //    if (dstType == TYP_INT)
    //    {
    //        // convert t0 int32
    //        emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, dstReg, srcReg, 0);
    //    }
    //    else
    //    {
    //        // convert t0 uint32
    //        emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_PTRSIZE, dstReg, srcReg, pos+31, pos);
    //    }
    //}
    //else if ((desc.ExtendKind() != GenIntCastDesc::COPY) || (srcReg != dstReg))
    if ((desc.ExtendKind() != GenIntCastDesc::COPY) || (srcReg != dstReg))
    {
        instruction ins;

        switch (desc.ExtendKind())
        {
            case GenIntCastDesc::ZERO_EXTEND_SMALL_INT:
                if (desc.ExtendSrcSize() == 1)
                {
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_PTRSIZE, dstReg, srcReg, pos+7, pos);
                }
                else
                {
                    emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_PTRSIZE, dstReg, srcReg, pos+15, pos);
                }
                break;
            case GenIntCastDesc::SIGN_EXTEND_SMALL_INT:
                ins = (desc.ExtendSrcSize() == 1) ? INS_ext_w_b : INS_ext_w_h;
                emit->emitIns_R_R(ins, EA_PTRSIZE, dstReg, srcReg);
                break;
#ifdef TARGET_64BIT
            case GenIntCastDesc::ZERO_EXTEND_INT:
                emit->emitIns_R_R_I_I(INS_bstrpick_d, EA_PTRSIZE, dstReg, srcReg, pos+31, pos);
                break;
            case GenIntCastDesc::SIGN_EXTEND_INT:
                emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, dstReg, srcReg, 0);
                break;
#endif
            default:
                assert(desc.ExtendKind() == GenIntCastDesc::COPY);
#if 1
                if (srcType == TYP_INT)
                    emit->emitIns_R_R_I(INS_slli_w, EA_4BYTE, dstReg, srcReg, 0);//should amend.
                else
                    emit->emitIns_R_R_I(INS_ori, EA_PTRSIZE, dstReg, srcReg, 0);
#else
                emit->emitIns_R_R(INS_mov, EA_PTRSIZE, dstReg, srcReg);
#endif
                break;
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
    assert(!op1->isContained());               // Cannot be contained
    assert(genIsValidFloatReg(op1->GetRegNum())); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());

    // treeNode must be a reg
    assert(!treeNode->isContained());

    if (srcType != dstType)
    {
        instruction ins = (srcType == TYP_FLOAT) ? INS_fcvt_d_s  // convert Single to Double
                                                 : INS_fcvt_s_d; // convert Double to Single

        GetEmitter()->emitIns_R_R(ins, emitActualTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum());
    }
    else if (treeNode->GetRegNum() != op1->GetRegNum())
    {
        // If double to double cast or float to float cast. Emit a move instruction.
        instruction ins = (srcType == TYP_FLOAT) ? INS_fmov_s : INS_fmov_d;
        GetEmitter()->emitIns_R_R(ins, emitActualTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum());
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCreateAndStoreGCInfo: Create and record GC Info for the function.
//
void CodeGen::genCreateAndStoreGCInfo(unsigned codeSize,
                                      unsigned prologSize,
                                      unsigned epilogSize DEBUGARG(void* codePtr))
{
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) CompIAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder != nullptr);

    // Follow the code pattern of the x86 gc info encoder (genCreateAndStoreGCInfoJIT32).
    gcInfo.gcInfoBlockHdrSave(gcInfoEncoder, codeSize, prologSize);

    // We keep the call count for the second call to gcMakeRegPtrTable() below.
    unsigned callCnt = 0;

    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS, &callCnt);

    // Now we've requested all the slots we'll need; "finalize" these (make more compact data structures for them).
    gcInfoEncoder->FinalizeSlotIds();

    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK, &callCnt);

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

    if (compiler->opts.IsReversePInvoke())
    {
        unsigned reversePInvokeFrameVarNumber = compiler->lvaReversePInvokeFrameVar;
        assert(reversePInvokeFrameVarNumber != BAD_VAR_NUM);
        const LclVarDsc* reversePInvokeFrameVar = compiler->lvaGetDesc(reversePInvokeFrameVarNumber);//TODO: unused.
        gcInfoEncoder->SetReversePInvokeFrameSlot(reversePInvokeFrameVar->GetStackOffset());
    }

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
}

/* TODO for LOONGARCH64: not used for loongarch */
// clang-format off
const CodeGen::GenConditionDesc CodeGen::GenConditionDesc::map[32]
{
    //{ },       // NONE
    //{ },       // 1
    //{ EJ_lt }, // SLT
    //{ EJ_le }, // SLE
    //{ EJ_ge }, // SGE
    //{ EJ_gt }, // SGT
    //{ EJ_mi }, // S
    //{ EJ_pl }, // NS

    //{ EJ_eq }, // EQ
    //{ EJ_ne }, // NE
    //{ EJ_lo }, // ULT
    //{ EJ_ls }, // ULE
    //{ EJ_hs }, // UGE
    //{ EJ_hi }, // UGT
    //{ EJ_hs }, // C
    //{ EJ_lo }, // NC

    //{ EJ_eq },                // FEQ
    //{ EJ_gt, GT_AND, EJ_lo }, // FNE
    //{ EJ_lo },                // FLT
    //{ EJ_ls },                // FLE
    //{ EJ_ge },                // FGE
    //{ EJ_gt },                // FGT
    //{ EJ_vs },                // O
    //{ EJ_vc },                // NO

    //{ EJ_eq, GT_OR, EJ_vs },  // FEQU
    //{ EJ_ne },                // FNEU
    //{ EJ_lt },                // FLTU
    //{ EJ_le },                // FLEU
    //{ EJ_hs },                // FGEU
    //{ EJ_hi },                // FGTU
    //{ },                      // P
    //{ },                      // NP
};
// clang-format on

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
    /* TODO for LOONGARCH64: should redesign and delete. */
	assert(!"unimplemented on LOONGARCH yet");
}

//------------------------------------------------------------------------
// genCodeForStoreBlk: Produce code for a GT_STORE_OBJ/GT_STORE_DYN_BLK/GT_STORE_BLK node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForStoreBlk(GenTreeBlk* blkOp)
{
    assert(blkOp->OperIs(GT_STORE_OBJ, GT_STORE_DYN_BLK, GT_STORE_BLK));

    if (blkOp->OperIs(GT_STORE_OBJ))
    {
        assert(!blkOp->gtBlkOpGcUnsafe);
        assert(blkOp->OperIsCopyBlkOp());
        assert(blkOp->AsObj()->GetLayout()->HasGCPtr());
        genCodeForCpObj(blkOp->AsObj());
        return;
    }
    if (blkOp->gtBlkOpGcUnsafe)
    {
        GetEmitter()->emitDisableGC();
    }
    bool isCopyBlk = blkOp->OperIsCopyBlkOp();

    switch (blkOp->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindHelper:
            if (isCopyBlk)
            {
                genCodeForCpBlkHelper(blkOp);
            }
            else
            {
                genCodeForInitBlkHelper(blkOp);
            }
            break;

        case GenTreeBlk::BlkOpKindUnroll:
            if (isCopyBlk)
            {
                genCodeForCpBlkUnroll(blkOp);
            }
            else
            {
                genCodeForInitBlkUnroll(blkOp);
            }
            break;

        default:
            unreached();
    }

    if (blkOp->gtBlkOpGcUnsafe)
    {
        GetEmitter()->emitEnableGC();
    }
}
#if 1
//------------------------------------------------------------------------
// genScaledAdd: A helper for genLeaInstruction.
//TODO: can amend further.
void CodeGen::genScaledAdd(emitAttr attr, regNumber targetReg, regNumber baseReg, regNumber indexReg, int scale)
{
    emitter* emit = GetEmitter();
    if (scale == 0)
    {
        // target = base + index
        emit->emitIns_R_R_R(INS_add_d, attr, targetReg, baseReg, indexReg);
    }
    else
    {
        // target = base + index<<scale
        emit->emitIns_R_R_I(INS_slli_d, attr, REG_R21, indexReg, scale);
        emit->emitIns_R_R_R(INS_add_d, attr, targetReg, baseReg, REG_R21);
    }
}
#endif
//------------------------------------------------------------------------
// genLeaInstruction: Produce code for a GT_LEA node.
//
// Arguments:
//    lea - the node
//
void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    genConsumeOperands(lea);
    emitter* emit   = GetEmitter();
    emitAttr size   = emitTypeSize(lea);
    int      offset = lea->Offset();

    // In LOONGARCH we can only load addresses of the form:
    //
    // [Base + index*scale]
    // [Base + Offset]
    // [Literal] (PC-Relative)
    //
    // So for the case of a LEA node of the form [Base + Index*Scale + Offset] we will generate:
    // destReg = baseReg + indexReg * scale;
    // destReg = destReg + offset;
    //
    // TODO-LOONGARCH64-CQ: The purpose of the GT_LEA node is to directly reflect a single target architecture
    //             addressing mode instruction.  Currently we're 'cheating' by producing one or more
    //             instructions to generate the addressing mode so we need to modify lowering to
    //             produce LEAs that are a 1:1 relationship to the LOONGARCH64 architecture.
    if (lea->Base() && lea->Index())
    {
        GenTree* memBase = lea->Base();
        GenTree* index   = lea->Index();

        DWORD scale;

        assert(isPow2(lea->gtScale));
        BitScanForward(&scale, lea->gtScale);

        assert(scale <= 4);

        if (offset != 0)
        {
            regNumber tmpReg = lea->GetSingleTempReg();

            // When generating fully interruptible code we have to use the "large offset" sequence
            // when calculating a EA_BYREF as we can't report a byref that points outside of the object
            //
            bool useLargeOffsetSeq = compiler->GetInterruptible() && (size == EA_BYREF);

            if (!useLargeOffsetSeq && ((-2048 <= offset) && (offset <= 2047)))
            {
                // Generate code to set tmpReg = base + index*scale
                genScaledAdd(size, tmpReg, memBase->GetRegNum(), index->GetRegNum(), scale);

                // Then compute target reg from [tmpReg + offset]
                emit->emitIns_R_R_I(INS_addi_d, size, lea->GetRegNum(), tmpReg, offset);
            }
            else // large offset sequence
            {
                noway_assert(tmpReg != index->GetRegNum());
                noway_assert(tmpReg != memBase->GetRegNum());

                // First load/store tmpReg with the offset constant
                //      rTmp = imm
                GetEmitter()->emitIns_I_la(EA_PTRSIZE, tmpReg, offset);

                // Then add the scaled index register
                //      rTmp = rTmp + index*scale
                genScaledAdd(EA_PTRSIZE, tmpReg, tmpReg, index->GetRegNum(), scale);

                // Then compute target reg from [base + tmpReg ]
                //      rDst = base + rTmp
                emit->emitIns_R_R_R(INS_add_d, size, lea->GetRegNum(), memBase->GetRegNum(), tmpReg);
            }
        }
        else
        {
            // Then compute target reg from [base + index*scale]
            genScaledAdd(size, lea->GetRegNum(), memBase->GetRegNum(), index->GetRegNum(), scale);
        }
    }
    else if (lea->Base())
    {
        GenTree* memBase = lea->Base();

        if ((-2048 <= offset) && (offset <= 2047))
        {
            if (offset != 0)
            {
                // Then compute target reg from [memBase + offset]
                emit->emitIns_R_R_I(INS_addi_d, size, lea->GetRegNum(), memBase->GetRegNum(), offset);
            }
            else // offset is zero
            {
                if (lea->GetRegNum() != memBase->GetRegNum())
                {
                    emit->emitIns_R_R_I(INS_ori, size, lea->GetRegNum(), memBase->GetRegNum(), 0);
                }
            }
        }
        else
        {
            // We require a tmpReg to hold the offset
            regNumber tmpReg = lea->GetSingleTempReg();

            // First load tmpReg with the large offset constant
            GetEmitter()->emitIns_I_la(EA_PTRSIZE, tmpReg, offset);

            // Then compute target reg from [memBase + tmpReg]
            emit->emitIns_R_R_R(INS_add_d, size, lea->GetRegNum(), memBase->GetRegNum(), tmpReg);
        }
    }
    else if (lea->Index())
    {
        // If we encounter a GT_LEA node without a base it means it came out
        // when attempting to optimize an arbitrary arithmetic expression during lower.
        // This is currently disabled in LOONGARCH64 since we need to adjust lower to account
        // for the simpler instructions LOONGARCH64 supports.
        // TODO-LOONGARCH64-CQ:  Fix this and let LEA optimize arithmetic trees too.
        assert(!"We shouldn't see a baseless address computation during CodeGen for LOONGARCH64");
    }

    genProduceReg(lea);
}

//------------------------------------------------------------------------
// genAllocLclFrame: Probe the stack and allocate the local stack frame: subtract from SP.
//
// Notes:
//      On LOONGARCH64, this only does the probing; allocating the frame is done when callee-saved registers are saved.
//      This is done before anything has been pushed. The previous frame might have a large outgoing argument
//      space that has been allocated, but the lowest addresses have not been touched. Our frame setup might
//      not touch up to the first 504 bytes. This means we could miss a guard page. On Windows, however,
//      there are always three guard pages, so we will not miss them all. On Linux, there is only one guard
//      page by default, so we need to be more careful. We do an extra probe if we might not have probed
//      recently enough. That is, if a call and prolog establishment might lead to missing a page. We do this
//      on Windows as well just to be consistent, even though it should not be necessary.
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
        // We don't need a register for the target of the dummy load
        // ld_w $0,offset(base) will ignor the addr-exception.
        regNumber rTemp = REG_R0;
        lastTouchDelta  = frameSize;

        for (target_size_t probeOffset = pageSize; probeOffset <= frameSize; probeOffset += pageSize)
        {
            // Generate:
            //    lw rTemp, -probeOffset(SP)  // load into initReg
            GetEmitter()->emitIns_I_la(EA_PTRSIZE, initReg, -(ssize_t)probeOffset);
            GetEmitter()->emitIns_R_R_R(INS_ldx_w, EA_4BYTE, rTemp, REG_SPBASE, initReg);
            regSet.verifyRegUsed(initReg);
            *pInitRegZeroed = false; // The initReg does not contain zero

            lastTouchDelta -= pageSize;
        }

        assert(lastTouchDelta == frameSize % pageSize);
        compiler->unwindPadding();
    }
    else
    {
        assert(frameSize >= 3 * pageSize);

        // Emit the following sequence to 'tickle' the pages. Note it is important that stack pointer not change
        // until this is complete since the tickles could cause a stack overflow, and we need to be able to crawl
        // the stack afterward (which means the stack pointer needs to be known).
        //
        // LOONGARCH64 needs 2 registers. See VERY_LARGE_FRAME_SIZE_REG_MASK for how these
        // are reserved.

        regMaskTP availMask = RBM_ALLINT & (regSet.rsGetModifiedRegsMask() | ~RBM_INT_CALLEE_SAVED);
        availMask &= ~maskArgRegsLiveIn;   // Remove all of the incoming argument registers as they are currently live
        availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg

        regNumber rOffset = initReg;
        regNumber rLimit;
        regMaskTP tempMask;

        // We don't need a register for the target of the dummy load
        // ld_w $0,offset(base) will ignor the addr-exception.
        regNumber rTemp = REG_R0;

        // We pick the next lowest register number for rLimit
        noway_assert(availMask != RBM_NONE);
        tempMask = genFindLowestBit(availMask);
        rLimit   = genRegNumFromMask(tempMask);
        availMask &= ~tempMask;

        // Generate:
        //
        //      instGen_Set_Reg_To_Imm(EA_PTRSIZE, rOffset, -(ssize_t)pageSize);
        //      instGen_Set_Reg_To_Imm(EA_PTRSIZE, rLimit, -(ssize_t)frameSize);
        //      INS_lu12i_w, REG_R21, pageSize >> 12
        //
        // loop:
        //      ldx_w  rTemp, sp, rOffset,
        //      sub_d  rOffset, rOffset, REG_R21
        //      bge rOffset, rLimit, loop                 // If rLimit is less or equal rOffset, we need to probe this rOffset.

        noway_assert((ssize_t)(int)frameSize == (ssize_t)frameSize); // make sure framesize safely fits within an int

        GetEmitter()->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, rOffset, -(ssize_t)pageSize >> 12);
        regSet.verifyRegUsed(rOffset);
        GetEmitter()->emitIns_I_la(EA_PTRSIZE, rLimit, -(ssize_t)frameSize);
        regSet.verifyRegUsed(rLimit);

        assert(!(pageSize & 0xfff));
        GetEmitter()->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, REG_R21, pageSize >> 12);

        // There's a "virtual" label here. But we can't create a label in the prolog, so we use the magic
        // `emitIns_J` with a negative `instrCount` to branch back a specific number of instructions.

        GetEmitter()->emitIns_R_R_R(INS_ldx_w, EA_4BYTE, rTemp, REG_SPBASE, rOffset);
        GetEmitter()->emitIns_R_R_R(INS_sub_d, EA_PTRSIZE, rOffset, rOffset, REG_R21);

        assert(REG_R21 != rLimit);
        assert(REG_R21 != rOffset);
        ssize_t imm = -2 << 2;
        GetEmitter()->emitIns_R_R_I(INS_bge, EA_PTRSIZE, rOffset, rLimit, imm);

        *pInitRegZeroed = false; // The initReg does not contain zero

        compiler->unwindPadding();

        lastTouchDelta = frameSize % pageSize;
    }

    if (lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize)
    {

        assert(lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES < 2 * pageSize);
        GetEmitter()->emitIns_I_la(EA_PTRSIZE, initReg, -(ssize_t)frameSize);
        GetEmitter()->emitIns_R_R_R(INS_ldx_w, EA_4BYTE, REG_R0, REG_SPBASE, initReg);
        compiler->unwindPadding();

        regSet.verifyRegUsed(initReg);
        *pInitRegZeroed = false; // The initReg does not contain zero
    }
}

inline void CodeGen::genJumpToThrowHlpBlk_la(SpecialCodeKind codeKind, instruction ins, regNumber reg1, BasicBlock* failBlk, regNumber reg2)
{
    assert(INS_beq <= ins && ins <= INS_bgeu);

    bool useThrowHlpBlk = compiler->fgUseThrowHelperBlocks();

    emitter* emit = GetEmitter();
    if (useThrowHlpBlk)
    {
        // For code with throw helper blocks, find and use the helper block for
        // raising the exception. The block may be shared by other trees too.

        BasicBlock* excpRaisingBlock;

        if (failBlk != nullptr)
        {
            // We already know which block to jump to. Use that.
            excpRaisingBlock = failBlk;

#ifdef DEBUG
            Compiler::AddCodeDsc* add =
                compiler->fgFindExcptnTarget(codeKind, compiler->bbThrowIndex(compiler->compCurBB));
            assert(excpRaisingBlock == add->acdDstBlk);
#if !FEATURE_FIXED_OUT_ARGS
            assert(add->acdStkLvlInit || isFramePointerUsed());
#endif // !FEATURE_FIXED_OUT_ARGS
#endif // DEBUG
        }
        else
        {
            // Find the helper-block which raises the exception.
            Compiler::AddCodeDsc* add =
                compiler->fgFindExcptnTarget(codeKind, compiler->bbThrowIndex(compiler->compCurBB));
            PREFIX_ASSUME_MSG((add != nullptr), ("ERROR: failed to find exception throw block"));
            excpRaisingBlock = add->acdDstBlk;
#if !FEATURE_FIXED_OUT_ARGS
            assert(add->acdStkLvlInit || isFramePointerUsed());
#endif // !FEATURE_FIXED_OUT_ARGS
        }

        noway_assert(excpRaisingBlock != nullptr);

        // Jump to the exception-throwing block on error.
        emit->emitIns_J(ins, excpRaisingBlock, (int)reg1 | ((int)reg2 << 5));//5-bits;
    }
    else
    {
        // The code to throw the exception will be generated inline, and
        //  we will jump around it in the normal non-exception case.

        void* pAddr = nullptr;
        void* addr = compiler->compGetHelperFtn((CorInfoHelpFunc)(compiler->acdHelper(codeKind)), &pAddr);
        emitter::EmitCallType callType;
        regNumber callTarget;

        // maybe optimize
        // ins = (instruction)(ins^((ins != INS_beq)+(ins != INS_bne)));
        if(ins == INS_blt)
            ins = INS_bge;
        else if(ins == INS_bltu)
            ins = INS_bgeu;
        else if(ins == INS_bge)
            ins = INS_blt;
        else if(ins == INS_bgeu)
            ins = INS_bltu;
        else
            ins = ins == INS_beq ? INS_bne : INS_beq;
        if (addr == nullptr)
        {
            callType = emitter::EC_INDIR_R;
            callTarget = REG_DEFAULT_HELPER_CALL_TARGET;

            //ssize_t imm = (4 + 1 + 1) << 2;// 4=li, 1=ld, 1=jirl.//TODO: maybe optimize.

            //instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
            //emit->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, callTarget, callTarget, 0);
            if (compiler->opts.compReloc)
            {
                ssize_t imm = (2 + 1) << 2;// , 1=jirl.
                emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, reg2, imm);
                GetEmitter()->emitIns_R_AI(INS_bl, EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
            }
            else
            {
                ssize_t imm = (3 + 1) << 2;// , 1=jirl.
                emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, reg2, imm);

                //GetEmitter()->emitIns_R_I(INS_pcaddu12i, EA_PTRSIZE, callTarget, (ssize_t)pAddr);
                //GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, callTarget, callTarget, );
                GetEmitter()->emitIns_R_I(INS_lu12i_w, EA_PTRSIZE, callTarget, ((ssize_t)pAddr & 0xfffff000)>>12);
                GetEmitter()->emitIns_R_I(INS_lu32i_d, EA_PTRSIZE, callTarget, (ssize_t)pAddr >> 32);
                GetEmitter()->emitIns_R_R_I(INS_ldptr_d, EA_PTRSIZE, callTarget, callTarget, ((ssize_t)pAddr & 0xfff)>>2);
            }
        }
        else
        {//INS_OPTS_C
            callType = emitter::EC_FUNC_TOKEN;
            callTarget = REG_NA;

            ssize_t imm = 5 << 2;
            if (compiler->opts.compReloc)
                imm = 3 << 2;

            emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, reg2, imm);
        }

        emit->emitIns_Call(callType, compiler->eeFindHelper(compiler->acdHelper(codeKind)), INDEBUG_LDISASM_COMMA(nullptr) addr, 0,
                                   EA_UNKNOWN, EA_UNKNOWN, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                   gcInfo.gcRegByrefSetCur, DebugInfo(), /* IL offset */
                                   callTarget,                             /* ireg */
                                   REG_NA, 0, 0,                           /* xreg, xmul, disp */
                                   false                                   /* isJump */
                                   );

        regMaskTP killMask = compiler->compHelperCallKillSet((CorInfoHelpFunc)(compiler->acdHelper(codeKind)));
        regSet.verifyRegistersUsed(killMask);
    }
}

//-----------------------------------------------------------------------------------
// instGen_MemoryBarrier: Emit a MemoryBarrier instruction
//
// Arguments:
//     barrierKind - kind of barrier to emit (Only supports the Full now!! This depends on the CPU).
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

    // TODO: Use the exact barrier type depending on the CPU.
    GetEmitter()->emitIns_I(INS_dbar, EA_4BYTE, INS_BARRIER_FULL);
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
void CodeGen::genProfilingLeaveCallback(unsigned helper /*= CORINFO_HELP_PROF_FCN_LEAVE*/)
{
    assert((helper == CORINFO_HELP_PROF_FCN_LEAVE) || (helper == CORINFO_HELP_PROF_FCN_TAILCALL));

    // Only hook if profiler says it's okay.
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    compiler->info.compProfilerCallback = true;

    // Need to save on to the stack level, since the helper call will pop the argument
    unsigned saveStackLvl2 = genStackLevel;

    /* Restore the stack level */
    SetStackLevel(saveStackLvl2);
}

/*-----------------------------------------------------------------------------
 *
 *  Push any callee-saved registers we have used
 */

void CodeGen::genPushCalleeSavedRegisters(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    regMaskTP rsPushRegs = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

    // On LA we push the FP (frame-pointer) here along with all other callee saved registers
    if (isFramePointerUsed())
        rsPushRegs |= RBM_FPBASE;

    //
    // It may be possible to skip pushing/popping ra for leaf methods. However, such optimization would require
    // changes in GC suspension architecture.
    //
    // We would need to guarantee that a tight loop calling a virtual leaf method can be suspended for GC. Today, we
    // generate partially interruptible code for both the method that contains the tight loop with the call and the leaf
    // method. GC suspension depends on return address hijacking in this case. Return address hijacking depends
    // on the return address to be saved on the stack. If we skipped pushing/popping ra, the return address would never
    // be saved on the stack and the GC suspension would time out.
    //
    // So if we wanted to skip pushing/popping ra for leaf frames, we would also need to do one of
    // the following to make GC suspension work in the above scenario:
    // - Make return address hijacking work even when ra is not saved on the stack.
    // - Generate fully interruptible code for loops that contains calls
    // - Generate fully interruptible code for leaf methods
    //
    // Given the limited benefit from this optimization (<10k for mscorlib NGen image), the extra complexity
    // is not worth it.
    //

    rsPushRegs |= RBM_RA; // We must save the return address (in the RA register).
    regSet.rsMaskCalleeSaved = rsPushRegs;
    regMaskTP maskSaveRegsFloat = rsPushRegs & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = rsPushRegs & ~maskSaveRegsFloat;

#ifdef DEBUG
    if (compiler->compCalleeRegsPushed != genCountBits(rsPushRegs))
    {
        printf("Error: unexpected number of callee-saved registers to push. Expected: %d. Got: %d ",
               compiler->compCalleeRegsPushed, genCountBits(rsPushRegs));
        dspRegMask(rsPushRegs);
        printf("\n");
        assert(compiler->compCalleeRegsPushed == genCountBits(rsPushRegs));
    }
#endif // DEBUG

    // See the document "LOONGARCH64 JIT Frame Layout" and/or "LOONGARCH64 Exception Data" for more details or requirements and
    // options. Case numbers in comments here refer to this document. See also Compiler::lvaAssignFrameOffsets()
    // for pictures of the general frame layouts, and CodeGen::genFuncletProlog() implementations (per architecture)
    // for pictures of the funclet frame layouts.
    //
    // For most frames, generate, e.g.:
    //      sdc1 f31, off+7*8(sp)
    //      ...
    //      sdc1 f24, off(sp)
    //
    //      sd s7, off2+7*8(sp)
    //      ...
    //      sd s1, off2+8(sp)
    //      sd s0, off2(sp)
    //
    //      sd fp,  0(sp)
    //      sd ra,  8(sp)
    //
    // Notes:
    // 1. FP is always saved, and the first store is FP, RA.
    // 2. General-purpose registers are 8 bytes, floating-point registers are 8 bytes, but SIMD/FP registers 16 bytes.
    //    TODO: supporting SIMD feature !
    // 3. For frames with varargs, not implemented completely and not tested !
    // 4. We allocate the frame here; no further changes to SP are allowed (except in the body, for localloc).
    //
    // For functions with GS and localloc, we change the frame so the frame pointer and RA are saved at the top
    // of the frame, just under the varargs registers (if any). Note that the funclet frames must follow the same
    // rule, and both main frame and funclet frames (if any) must put PSPSym in the same offset from Caller-SP.
    // Since this frame type is relatively rare, we force using it via stress modes, for additional coverage.
    //
    // The frames look like the following (simplified to only include components that matter for establishing the
    // frames). See also Compiler::lvaAssignFrameOffsets().
    //
    //
    // Frames with FP, RA saved at bottom of frame (above outgoing argument space):
    //
    //      |                       |
    //      |-----------------------|
    //      |  incoming arguments   |
    //      +=======================+ <---- Caller's SP
    //      |     Arguments  Or     | // if needed.
    //      |  Varargs regs space   | // Only for varargs functions; 64 bytes (TODO: not implement completely)
    //      |-----------------------|
    //      |Callee saved registers | // not including FP/RA; multiple of 8 bytes
    //      |-----------------------|
    //      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
    //      |-----------------------|
    //      | locals, temps, etc.   |
    //      |-----------------------|
    //      |  possible GS cookie   |
    //      |-----------------------|
    //      |      Saved RA         | // 8 bytes
    //      |-----------------------|
    //      |      Saved FP         | // 8 bytes
    //      |-----------------------|
    //      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
    //      |-----------------------| <---- Ambient SP
    //      |       |               |
    //      ~       | Stack grows   ~
    //      |       | downward      |
    //              V
    //
    //
    // Frames with FP, RA saved at top of frame (note: above all callee-saved regs):
    //
    //      |                       |
    //      |-----------------------|
    //      |  incoming arguments   |
    //      +=======================+ <---- Caller's SP
    //      |     Arguments  Or     | // if needed.
    //      |  Varargs regs space   | // Only for varargs functions; 64 bytes (TODO: not implement completely)
    //      |-----------------------|
    //      |      Saved RA         | // 8 bytes
    //      |-----------------------|
    //      |      Saved FP         | // 8 bytes
    //      |-----------------------|
    //      |Callee saved registers | // not including FP/RA; multiple of 8 bytes
    //      |-----------------------|
    //      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
    //      |-----------------------|
    //      | locals, temps, etc.   |
    //      |-----------------------|
    //      |  possible GS cookie   |
    //      |-----------------------|
    //      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
    //      |-----------------------| <---- Ambient SP
    //      |       |               |
    //      ~       | Stack grows   ~
    //      |       | downward      |
    //              V
    //

    int totalFrameSize = genTotalFrameSize();

    int offset; // This will be the starting place for saving the callee-saved registers, in increasing order.

#ifdef DEBUG
    if (verbose)
    {
        printf("Save float regs: ");
        dspRegMask(maskSaveRegsFloat);
        printf("\n");
        printf("Save int   regs: ");
        dspRegMask(maskSaveRegsInt);
        printf("\n");
    }
#endif // DEBUG

    // The frameType number is arbitrary, is defined below, and corresponds to one of the frame styles we
    // generate based on various sizes.
    int frameType = 0;

    // The amount to subtract from SP before starting to store the callee-saved registers. It might be folded into the
    // first save instruction as a "predecrement" amount, if possible.
    int calleeSaveSPDelta = 0;

    // By default, we'll establish the frame pointer chain. (Note that currently frames without FP are NYI.)
    bool establishFramePointer = true;

    // If we do establish the frame pointer, what is the amount we add to SP to do so?
    unsigned offsetSpToSavedFp = 0;

    if (isFramePointerUsed())
    {
        // We need to save both FP and RA.

        assert((maskSaveRegsInt & RBM_FP) != 0);
        assert((maskSaveRegsInt & RBM_RA) != 0);

        // If we need to generate a GS cookie, we need to make sure the saved frame pointer and return address
        // (FP and RA) are protected from buffer overrun by the GS cookie. If FP/RA are at the lowest addresses,
        // then they are safe, since they are lower than any unsafe buffers. And the GS cookie we add will
        // protect our caller's frame. If we have a localloc, however, that is dynamically placed lower than our
        // saved FP/RA. In that case, we save FP/RA along with the rest of the callee-saved registers, above
        // the GS cookie.
        //
        // After the frame is allocated, the frame pointer is established, pointing at the saved frame pointer to
        // create a frame pointer chain.
        //

        if (totalFrameSize < 2048)
        {
            GetEmitter()->emitIns_R_R_I(INS_addi_d, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, -totalFrameSize);
            compiler->unwindAllocStack(totalFrameSize);

            if (!IsSaveFpRaWithAllCalleeSavedRegisters())
            {
                // Case #1.
                //
                // Generate:
                //      daddiu sp, sp, -framesz
                //      sd fp, outsz(sp)
                //      sd ra, outsz+8(sp)
                //
                // The (totalFrameSize <= 2047) condition ensures the offsets of sd/ld.
                //
                // After saving callee-saved registers, we establish the frame pointer with:
                //      daddiu fp, sp, offset-fp
                // We do this *after* saving callee-saved registers, so the prolog/epilog unwind codes mostly match.

                JITDUMP("Frame type 1. #outsz=%d; #framesz=%d; LclFrameSize=%d\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
                        totalFrameSize, compiler->compLclFrameSize);

                frameType = 1;

                offsetSpToSavedFp = compiler->lvaOutgoingArgSpaceSize;

//#ifdef OPTIMIZE_LOONGSON_EXT
//                if (!(offsetSpToSavedFp & 0xf) && (offsetSpToSavedFp <= 0xff0))
//                {
//                    GetEmitter()->emitIns_R_R_R_I(INS_gssq, EA_PTRSIZE, REG_RA, REG_FP, REG_SPBASE, offsetSpToSavedFp >> 4);
//                    compiler->unwindSaveRegPair(REG_FP, REG_RA, offsetSpToSavedFp);
//                }
//                else
//#endif
//                {
                GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_FP, REG_SPBASE, offsetSpToSavedFp);
                compiler->unwindSaveReg(REG_FP, offsetSpToSavedFp);

                GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_RA, REG_SPBASE, offsetSpToSavedFp + 8);
                compiler->unwindSaveReg(REG_RA, offsetSpToSavedFp + 8);
//                }

                maskSaveRegsInt &= ~(RBM_FP | RBM_RA); // We've already saved FP/RA

                offset = compiler->compLclFrameSize + 2*REGSIZE_BYTES;//FP/RA
            }
            else
            {
                frameType = 2;

                offsetSpToSavedFp = genSPtoFPdelta();

                JITDUMP("Frame type 2(Top). #outsz=%d; #framesz=%d; LclFrameSize=%d, fpDelta:%d\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
                        totalFrameSize, compiler->compLclFrameSize, offsetSpToSavedFp);

                offset = compiler->compLclFrameSize;
            }
        }
        else
        {
            if (!IsSaveFpRaWithAllCalleeSavedRegisters())
            {
                JITDUMP("Frame type 3. #outsz=%d; #framesz=%d; LclFrameSize=%d\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
                        totalFrameSize, compiler->compLclFrameSize);

                frameType = 3;

                maskSaveRegsInt &= ~(RBM_FP | RBM_RA); // We've already saved FP/RA

                offset = totalFrameSize - compiler->compLclFrameSize - 2 * REGSIZE_BYTES;
                calleeSaveSPDelta = AlignUp((UINT)offset, STACK_ALIGN);
                offset = calleeSaveSPDelta - offset;
            }
            else
            {
                frameType = 4;

                JITDUMP("Frame type 4(Top). #outsz=%d; #framesz=%d; LclFrameSize=%d, SPDelta-1:%d\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
                        totalFrameSize, compiler->compLclFrameSize, calleeSaveSPDelta);

                offset = totalFrameSize - compiler->compLclFrameSize;
                calleeSaveSPDelta = AlignUp((UINT)offset, STACK_ALIGN);
                offset = calleeSaveSPDelta - offset;
                offsetSpToSavedFp = offset + REGSIZE_BYTES;
            }
        }
    }
    else
    {
        // No frame pointer (no chaining).
        assert((maskSaveRegsInt & RBM_FP) == 0);
        assert((maskSaveRegsInt & RBM_RA) != 0);

        // Note that there is no pre-indexed save_lrpair unwind code variant, so we can't allocate the frame using
        // 'sd' if we only have one callee-saved register plus RA to save.

        NYI("Frame without frame pointer");
        offset = 0;
    }

    assert(frameType != 0);

    JITDUMP("    offset=%d, calleeSaveSPDelta=%d\n", offset, calleeSaveSPDelta);
    genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, offset, -calleeSaveSPDelta);

    // For varargs, home the incoming arg registers last. Note that there is nothing to unwind here,
    // so we just report "NOP" unwind codes. If there's no more frame setup after this, we don't
    // need to add codes at all.

    //if (compiler->info.compIsVarArgs)
    //{
    //    JITDUMP("    compIsVarArgs=true\n");

    //    // There are 8 general-purpose registers to home, thus 'offset' must be 16-byte aligned here.
    //    assert((offset % 16) == 0);
    //    for (regNumber reg1 = REG_ARG_FIRST; reg1 < REG_ARG_LAST; reg1 = REG_NEXT(REG_NEXT(reg1)))
    //    {
    //        regNumber reg2 = REG_NEXT(reg1);
    //        // sd REG, offset(SP)
    //        // sd REG + 1, (offset+8)(SP)
    //        GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, reg1, REG_SPBASE, offset);
    //        compiler->unwindNop();
    //        GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, reg2, REG_SPBASE, offset + 8);
    //        compiler->unwindNop();
    //        offset += 2 * REGSIZE_BYTES;
    //    }
    //}

#ifdef DEBUG
    if (compiler->opts.disAsm)
        printf("DEBUG: LOONGARCH64, frameType:%d\n\n", frameType);
#endif
    if (frameType == 1)
    {
        //offsetSpToSavedFp = genSPtoFPdelta();
    }
    else if (frameType == 2)
    {
        //offsetSpToSavedFp = genSPtoFPdelta();
    }
    else if (frameType == 3)
    {
        if (compiler->lvaOutgoingArgSpaceSize >= 2040)
        {
            offset = totalFrameSize - calleeSaveSPDelta - compiler->lvaOutgoingArgSpaceSize;
            calleeSaveSPDelta = AlignUp((UINT)offset, STACK_ALIGN);
            offset = calleeSaveSPDelta - offset;

            genStackPointerAdjustment(-calleeSaveSPDelta, initReg, pInitRegZeroed, /* reportUnwindData */ true);

            offsetSpToSavedFp = offset;

//#ifdef OPTIMIZE_LOONGSON_EXT
//            if (!(offset & 0xf) && (offset <= 0xff0))
//            {
//                GetEmitter()->emitIns_R_R_R_I(INS_gssq, EA_PTRSIZE, REG_RA, REG_FP, REG_SPBASE, offset >> 4);
//                compiler->unwindSaveRegPair(REG_FP, REG_RA, offset);
//            }
//            else
//#endif
//            {
            GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_FP, REG_SPBASE, offset);
            compiler->unwindSaveReg(REG_FP, offset);

            GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_RA, REG_SPBASE, offset + 8);
            compiler->unwindSaveReg(REG_RA, offset + 8);
//            }

            genEstablishFramePointer(offset, /* reportUnwindData */ true);

            calleeSaveSPDelta = compiler->lvaOutgoingArgSpaceSize & ~0xf;
            genStackPointerAdjustment(-calleeSaveSPDelta, initReg, pInitRegZeroed, /* reportUnwindData */ true);
        }
        else
        {
            calleeSaveSPDelta = totalFrameSize - calleeSaveSPDelta;
            genStackPointerAdjustment(-calleeSaveSPDelta, initReg, pInitRegZeroed, /* reportUnwindData */ true);

            offset = compiler->lvaOutgoingArgSpaceSize;

//#ifdef OPTIMIZE_LOONGSON_EXT
//            if (!(offset & 0xf) && (offset <= 0xff0))
//            {
//                GetEmitter()->emitIns_R_R_R_I(INS_gssq, EA_PTRSIZE, REG_RA, REG_FP, REG_SPBASE, offset >> 4);
//                compiler->unwindSaveRegPair(REG_FP, REG_RA, offset);
//            }
//            else
//#endif
            GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_FP, REG_SPBASE, offset);
            compiler->unwindSaveReg(REG_FP, offset);

            GetEmitter()->emitIns_R_R_I(INS_st_d, EA_PTRSIZE, REG_RA, REG_SPBASE, offset + 8);
            compiler->unwindSaveReg(REG_RA, offset + 8);

            genEstablishFramePointer(offset, /* reportUnwindData */ true);
        }

        establishFramePointer = false;
    }
    else if (frameType == 4)
    {
        genEstablishFramePointer(offsetSpToSavedFp, /* reportUnwindData */ true);

        establishFramePointer = false;

        int remainingFrameSz = totalFrameSize - calleeSaveSPDelta;

        if (remainingFrameSz > 0)
        {
            genStackPointerAdjustment(-remainingFrameSz, initReg, pInitRegZeroed, /* reportUnwindData */ true);
        }
    }
    else
    {
        unreached();
    }

    if (establishFramePointer)
    {
        JITDUMP("    offsetSpToSavedFp=%d\n", offsetSpToSavedFp);
        genEstablishFramePointer(offsetSpToSavedFp, /* reportUnwindData */ true);
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
    assert(compiler->compGeneratingProlog);

    // Give profiler a chance to back out of hooking this method
    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }
}
#endif // TARGET_LOONGARCH64
