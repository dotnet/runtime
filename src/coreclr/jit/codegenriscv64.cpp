// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        RISCV64 Code Generator                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef TARGET_RISCV64
#include "emit.h"
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "gcinfoencoder.h"
#include "patchpointinfo.h"

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
    emitAttr size = EA_SIZE(attr);

    // reg1 is usually a dest register
    // reg2 is always source register
    assert(tmpReg != reg2); // tmpReg can not match any source register

#ifdef DEBUG
    switch (ins)
    {
        case INS_addi:

        case INS_sb:
        case INS_sh:
        case INS_sw:
        case INS_fsw:
        case INS_sd:
        case INS_fsd:

        case INS_lb:
        case INS_lh:
        case INS_lw:
        case INS_flw:
        case INS_ld:
        case INS_fld:
            break;

        default:
            assert(!"Unexpected instruction in genInstrWithConstant");
            break;
    }
#endif
    bool immFitsInIns = emitter::isValidSimm12(imm);

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
        GetEmitter()->emitLoadImmediate(size, tmpReg, imm);
        regSet.verifyRegUsed(tmpReg);

        // when we are in an unwind code region
        // we record the extra instructions using unwindPadding()
        if (inUnwindRegion)
        {
            compiler->unwindPadding();
        }

        if (ins == INS_addi)
        {
            GetEmitter()->emitIns_R_R_R(INS_add, attr, reg1, reg2, tmpReg);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R(INS_add, attr, tmpReg, reg2, tmpReg);
            GetEmitter()->emitIns_R_R_I(ins, attr, reg1, tmpReg, 0);
        }
    }
    return immFitsInIns;
}

//------------------------------------------------------------------------
// genStackPointerAdjustment: add a specified constant value to the stack pointer in either the prolog
// or the epilog. The unwind codes for the generated instructions are produced. An available temporary
// register is required to be specified, in case the constant is too large to encode in an "add"
// instruction, such that we need to load the constant
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
    // Even though INS_addi is specified here, the encoder will replace it with INS_add
    //
    bool wasTempRegisterUsedForImm =
        !genInstrWithConstant(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta, tmpReg, true);
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

    instruction ins = INS_sd;
    if (genIsValidFloatReg(reg1))
    {
        ins = INS_fsd;
    }

    if (spDelta != 0)
    {
        // generate addi SP,SP,-imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);

        assert((spDelta + spOffset + 16) <= 0);

        assert(spOffset <= 2031); // 2047-16
    }

    emitter* emit = GetEmitter();

    // sd reg1, #spOffset(sp)
    emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
    compiler->unwindSaveReg(reg1, spOffset);

    // sd reg2, #(spOffset + 8)(sp)
    emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg2, REG_SPBASE, spOffset + 8);
    compiler->unwindSaveReg(reg2, spOffset + 8);
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

    instruction ins = INS_sd;
    if (genIsValidFloatReg(reg1))
    {
        ins = INS_fsd;
    }

    if (spDelta != 0)
    {
        // generate addi SP,SP,-imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
    }

    emitter* emit = GetEmitter();

    // sd reg1, #spOffset(sp)
    emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
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

    instruction ins = INS_ld;
    if (genIsValidFloatReg(reg1))
    {
        ins = INS_fld;
    }

    emitter* emit = GetEmitter();

    if (spDelta != 0)
    {
        assert(!useSaveNextPair);

        // ld reg2, #(spOffset + 8)(SP)
        emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg2, REG_SPBASE, spOffset + 8);
        compiler->unwindSaveReg(reg2, spOffset + 8);

        // ld reg1, #spOffset(SP)
        emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);

        // generate addi SP,SP,imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
    }
    else
    {
        // ld reg2, #(spOffset + 8)(SP)
        emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg2, REG_SPBASE, spOffset + 8);
        compiler->unwindSaveReg(reg2, spOffset + 8);

        // ld reg1, #spOffset(SP)
        emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);
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

    instruction ins = INS_ld;
    if (genIsValidFloatReg(reg1))
    {
        ins = INS_fld;
    }

    emitter* emit = GetEmitter();

    if (spDelta != 0)
    {
        // ld reg1, #spOffset(SP)
        emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
        compiler->unwindSaveReg(reg1, spOffset);

        // generate addi SP,SP,imm
        genStackPointerAdjustment(spDelta, tmpReg, pTmpRegIsZero, /* reportUnwindData */ true);
    }
    else
    {
        // ld reg1 #spOffset(SP)
        emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, REG_SPBASE, spOffset);
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

    regNumber tempReg = rsGetRsvdReg();

    for (int i = 0; i < regStack.Height(); ++i)
    {
        RegPair regPair = regStack.Bottom(i);
        if (regPair.reg2 != REG_NA)
        {
            // We can use two SD instructions.
            genPrologSaveRegPair(regPair.reg1, regPair.reg2, spOffset, spDelta, regPair.useSaveNextPair, tempReg,
                                 nullptr);

            spOffset += slotSize << 1;
        }
        else
        {
            // No register pair; we use a SD instruction.
            genPrologSaveReg(regPair.reg1, spOffset, spDelta, tempReg, nullptr);
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
// registers, though, we will have an empty alignment slot somewhere. It turns out we will put
// it below (at a lower address) the callee-saved registers, as that is currently how we
// do frame layout. This means that the first stack offset will be 8 and the stack pointer
// adjustment must be done by an ADDI (or ADD), and not folded in to a pre-indexed store.
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
            // addi sp, sp, #spDelta
            genStackPointerAdjustment(spDelta, rsGetRsvdReg(), nullptr, /* reportUnwindData */ true);
        }
        return;
    }

    assert((spDelta % STACK_ALIGN) == 0);

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

    regNumber tempReg = rsGetRsvdReg();

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
            spOffset -= slotSize << 1;

            genEpilogRestoreRegPair(regPair.reg1, regPair.reg2, spOffset, stackDelta, regPair.useSaveNextPair, tempReg,
                                    nullptr);
        }
        else
        {
            spOffset -= slotSize;
            genEpilogRestoreReg(regPair.reg1, spOffset, stackDelta, tempReg, nullptr);
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
//      ld    s11, #xxx(sp)
//      ld    s10, #xxx(sp)
//      ld    s9, #xxx(sp)
//      ld    s8, #xxx(sp)
//      ld    s7, #xxx(sp)
//      ld    s6, #xxx(sp)
//      ld    s5, #xxx(sp)
//      ld    s4, #xxx(sp)
//      ld    s3, #xxx(sp)
//      ld    s2, #xxx(sp)
//      ld    s1, #xxx(sp)
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
            genStackPointerAdjustment(spDelta, rsGetRsvdReg(), nullptr, /* reportUnwindData */ true);
        }
        return;
    }

    assert((spDelta % STACK_ALIGN) == 0);

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
 *     catch:          a0 = the address at which execution should resume (see BBJ_EHCATCHRET)
 *     filter:         a0 = non-zero if the handler should handle the exception, zero otherwise (see GT_RETFILT)
 *     finally/fault:  none
 *
 *  The RISC-V64 funclet prolog is the following (Note: #framesz is total funclet frame size,
 *  including everything; #outsz is outgoing argument space. #framesz must be a multiple of 16):
 *
 *  Frame type liking:
 *     addi sp, sp, -#framesz    ; establish the frame
 *     sd s1, #outsz(sp)         ; save callee-saved registers, as necessary
 *     sd s2, #(outsz+8)(sp)
 *     sd ra, #(outsz+?)(sp)     ; save RA (8 bytes)
 *     sd fp, #(outsz+?+8)(sp)   ; save FP (8 bytes)
 *
 *  The funclet frame layout:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |     Arguments  Or     | // if needed
 *      |  Varargs regs space   | // Only for varargs functions; NYI on RV64
 *      |-----------------------|
 *      |    MonitorAcquired    | // 8 bytes; for synchronized methods
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in NativeAOT ABI)
 *      |-----------------------|
 *      ~  alignment padding    ~ // To make the whole frame 16 byte aligned
 *      |-----------------------|
 *      |      Saved FP         | // 8 bytes
 *      |-----------------------|
 *      |      Saved RA         | // 8 bytes
 *      |-----------------------|
 *      |Callee saved registers | // multiple of 8 bytes, not includting RA/FP
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 * Note, that SP only change once. That means, there will be a maximum of one alignment slot needed.
 * Also remember, the stack oiubter needs to be 16 byte aligned at all times.
 * The size of the PSP slot plus callee-saved registers space is a maximum of 280 bytes:
 *
 *     RA,FP registers
 *     11 int callee-saved register s1-s11
 *     12 float callee-saved registers f8-f9, f18-f27
 *     8 saved integer argument registers a0-a7, if varargs function support.
 *     1 PSP slot
 *     1 alignment slot or monitor acquired slot
 *     == 35 slots * 8 bytes = 280 bytes.
 *
 * The outgoing argument size, however, can be very large, if we call a function that takes a large number of
 * arguments (note that we currently use the same outgoing argument space size in the funclet as for the main
 * function, even if the funclet doesn't have any calls, or has a much smaller, or larger, maximum number of
 * outgoing arguments for any call). In that case, we need to 16-byte align the initial change to SP, before
 * saving off the callee-saved registers and establishing the PSPsym, so we can use the limited immediate offset
 * encodings we have available, before doing another 16-byte aligned SP adjustment to create the outgoing argument
 * space. Both changes to SP might need to add alignment padding.
 *
 *  An example epilog sequence:
 *     addi sp, sp, #outsz       ; if any outgoing argument space
 *     ld s1, #(xxx-8)(sp)       ; restore callee-saved registers
 *     ld s2, #xxx(sp)
 *     ld ra, #(xxx+?-8)(sp)     ; restore RA
 *     ld fp, #(xxx+?)(sp)       ; restore FP
 *     addi sp, sp, #framesz
 *     jarl zero, ra
 */
// clang-format on

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif
    // TODO-RISCV64: Implement varargs (NYI_RISCV64)
    // TODO-RISCV64-CQ: We can use C extension for optimization

    assert(block != NULL);
    assert(block->HasFlag(BBF_FUNCLET_BEG));

    ScopedSetVariable<bool> _setGeneratingProlog(&compiler->compGeneratingProlog, true);

    gcInfo.gcResetForBB();

    compiler->unwindBegProlog();

    const bool isFilter  = (block->bbCatchTyp == BBCT_FILTER);
    const int  frameSize = genFuncletInfo.fiSpDelta;

    assert(frameSize < 0);

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

    regMaskTP maskSaveRegs  = genFuncletInfo.fiSaveRegs & RBM_CALLEE_SAVED;
    int       regsSavedSize = (compiler->compCalleeRegsPushed - 2) << 3;

    int calleeSavedDelta = genFuncletInfo.fiSP_to_CalleeSaved_delta;

    emitter* emit = GetEmitter();

    if (calleeSavedDelta + regsSavedSize + genFuncletInfo.fiCalleeSavedPadding <= 2040)
    {
        calleeSavedDelta += genFuncletInfo.fiCalleeSavedPadding;

        // addi sp, sp, #frameSize
        genStackPointerAdjustment(frameSize, REG_SCRATCH, nullptr, /* reportUnwindData */ true);

        genSaveCalleeSavedRegistersHelp(maskSaveRegs, calleeSavedDelta, 0);
        calleeSavedDelta += regsSavedSize;

        // sd ra, #calleeSavedDelta(sp)
        emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_RA, REG_SPBASE, calleeSavedDelta);
        compiler->unwindSaveReg(REG_RA, calleeSavedDelta);

        // sd fp, #(calleeSavedDelta+8)(sp)
        emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_FP, REG_SPBASE, calleeSavedDelta + 8);
        compiler->unwindSaveReg(REG_FP, calleeSavedDelta + 8);
    }
    else
    {
        assert(frameSize < -2040);

        int spDelta = frameSize + calleeSavedDelta;

        // addi sp, sp, #spDelta
        genStackPointerAdjustment(spDelta, REG_SCRATCH, nullptr, /* reportUnwindData */ true);

        genSaveCalleeSavedRegistersHelp(maskSaveRegs, genFuncletInfo.fiCalleeSavedPadding, 0);
        regsSavedSize += genFuncletInfo.fiCalleeSavedPadding;

        // sd ra, #regsSavedSize(sp)
        emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_RA, REG_SPBASE, regsSavedSize);
        compiler->unwindSaveReg(REG_RA, regsSavedSize);

        // sd fp, #(regsSavedSize+8)(sp)
        emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_FP, REG_SPBASE, regsSavedSize + 8);
        compiler->unwindSaveReg(REG_FP, regsSavedSize + 8);

        // addi sp, sp -#calleeSavedDelta
        genStackPointerAdjustment(-calleeSavedDelta, REG_SCRATCH, nullptr, /* reportUnwindData */ true);
    }

    // This is the end of the OS-reported prolog for purposes of unwinding
    compiler->unwindEndProlog();

    // If there is no PSPSym (NativeAOT ABI), we are done. Otherwise, we need to set up the PSPSym in the functlet
    // frame.
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
            genInstrWithConstant(INS_ld, EA_PTRSIZE, REG_A1, REG_A1, genFuncletInfo.fiCallerSP_to_PSP_slot_delta,
                                 REG_A2, false);
            regSet.verifyRegUsed(REG_A1);

            // Store the PSP value (aka CallerSP)
            genInstrWithConstant(INS_sd, EA_PTRSIZE, REG_A1, REG_SPBASE, genFuncletInfo.fiSP_to_PSP_slot_delta, REG_A2,
                                 false);

            // re-establish the frame pointer
            genInstrWithConstant(INS_addi, EA_PTRSIZE, REG_FPBASE, REG_A1,
                                 genFuncletInfo.fiFunction_CallerSP_to_FP_delta, REG_A2, false);
        }
        else // This is a non-filter funclet
        {
            // A3 is scratch, A2 can also become scratch.

            // compute the CallerSP, given the frame pointer. a3 is scratch?
            genInstrWithConstant(INS_addi, EA_PTRSIZE, REG_A3, REG_FPBASE,
                                 -genFuncletInfo.fiFunction_CallerSP_to_FP_delta, REG_A2, false);
            regSet.verifyRegUsed(REG_A3);

            genInstrWithConstant(INS_sd, EA_PTRSIZE, REG_A3, REG_SPBASE, genFuncletInfo.fiSP_to_PSP_slot_delta, REG_A2,
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
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif
    // TODO-RISCV64: Implement varargs (NYI_RISCV64)
    // TODO-RISCV64-CQ: We can use C extension for optimization

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    compiler->unwindBegEpilog();

    const int frameSize = genFuncletInfo.fiSpDelta;

    assert(frameSize < 0);

    regMaskTP maskRestoreRegs = genFuncletInfo.fiSaveRegs & RBM_CALLEE_SAVED;
    int       regsRestoreSize = (compiler->compCalleeRegsPushed - 2) << 3;

    int calleeSavedDelta = genFuncletInfo.fiSP_to_CalleeSaved_delta;

    emitter*  emit    = GetEmitter();
    regNumber tempReg = rsGetRsvdReg();

    if (calleeSavedDelta + regsRestoreSize + genFuncletInfo.fiCalleeSavedPadding <= 2040)
    {
        calleeSavedDelta += genFuncletInfo.fiCalleeSavedPadding;
        genRestoreCalleeSavedRegistersHelp(maskRestoreRegs, calleeSavedDelta, 0);
        calleeSavedDelta += regsRestoreSize;

        // ld ra, #calleeSavedDelta(sp)
        emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_RA, REG_SPBASE, calleeSavedDelta);
        compiler->unwindSaveReg(REG_RA, calleeSavedDelta);

        // ld fp, #(calleeSavedDelta+8)(sp)
        emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_FP, REG_SPBASE, calleeSavedDelta + 8);
        compiler->unwindSaveReg(REG_FP, calleeSavedDelta + 8);

        // addi sp, sp, -#frameSize
        genStackPointerAdjustment(-frameSize, tempReg, nullptr, /* reportUnwindData */ true);
    }
    else
    {
        assert(frameSize < -2040);

        // addi sp, sp, #calleeSavedDelta
        genStackPointerAdjustment(calleeSavedDelta, tempReg, nullptr, /* reportUnwindData */ true);

        genRestoreCalleeSavedRegistersHelp(maskRestoreRegs, genFuncletInfo.fiCalleeSavedPadding, 0);
        regsRestoreSize += genFuncletInfo.fiCalleeSavedPadding;

        // ld ra, #regsRestoreSize(sp)
        emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_RA, REG_SPBASE, regsRestoreSize);
        compiler->unwindSaveReg(REG_RA, regsRestoreSize);

        // ld fp, #(regsRestoreSize+8)(sp)
        emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_FP, REG_SPBASE, regsRestoreSize + 8);
        compiler->unwindSaveReg(REG_FP, regsRestoreSize + 8);

        // addi sp, sp, -#(frameSize + calleeSavedDelta)
        genStackPointerAdjustment(-(frameSize + calleeSavedDelta), tempReg, nullptr, /* reportUnwindData */ true);
    }

    // jarl zero, ra
    emit->emitIns_R_R_I(INS_jalr, emitActualTypeSize(TYP_I_IMPL), REG_R0, REG_RA, 0);
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
    {
        return;
    }

    assert(isFramePointerUsed());

    // The frame size and offsets must be finalized
    assert(compiler->lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT);

    regMaskTP rsMaskSaveRegs = regSet.rsMaskCalleeSaved;
    assert((rsMaskSaveRegs & RBM_RA) != 0);
    assert((rsMaskSaveRegs & RBM_FP) != 0);

    unsigned pspSize = (compiler->lvaPSPSym != BAD_VAR_NUM) ? 8 : 0;

    // If there is a PSP slot, we have to pad the funclet frame size for OSR.
    // For more details see CodeGen::genFuncletProlog
    //
    unsigned osrPad = 0;
    if (compiler->opts.IsOSR() && (pspSize != 0))
    {
        osrPad = compiler->info.compPatchpointInfo->TotalFrameSize();

        // osrPad must be aligned to stackSize
        assert(osrPad % STACK_ALIGN == 0);
    }

    genFuncletInfo.fiCalleeSavedPadding            = 0;
    genFuncletInfo.fiFunction_CallerSP_to_FP_delta = genCallerSPtoFPdelta() - osrPad;

    unsigned savedRegsSize = genCountBits(rsMaskSaveRegs);
    assert(savedRegsSize == compiler->compCalleeRegsPushed);
    savedRegsSize <<= 3;

    unsigned saveRegsPlusPSPSize = savedRegsSize + pspSize;

    assert(compiler->lvaOutgoingArgSpaceSize % REGSIZE_BYTES == 0);
    unsigned outgoingArgSpaceAligned = roundUp(compiler->lvaOutgoingArgSpaceSize, STACK_ALIGN);

    unsigned funcletFrameSize        = osrPad + saveRegsPlusPSPSize + compiler->lvaOutgoingArgSpaceSize;
    unsigned funcletFrameSizeAligned = roundUp(funcletFrameSize, STACK_ALIGN);

    int SP_to_CalleeSaved_delta = compiler->lvaOutgoingArgSpaceSize;
    if ((SP_to_CalleeSaved_delta + savedRegsSize) >= 2040)
    {
        int offset              = funcletFrameSizeAligned - SP_to_CalleeSaved_delta;
        SP_to_CalleeSaved_delta = AlignUp((UINT)offset, STACK_ALIGN);

        genFuncletInfo.fiCalleeSavedPadding = SP_to_CalleeSaved_delta - offset;
    }

    if (compiler->lvaMonAcquired != BAD_VAR_NUM && !compiler->opts.IsOSR())
    {
        // We furthermore allocate the "monitor acquired" bool between PSP and
        // the saved registers because this is part of the EnC header.
        // Note that OSR methods reuse the monitor bool created by tier 0.
        osrPad += compiler->lvaLclSize(compiler->lvaMonAcquired);
    }

    /* Now save it for future use */
    genFuncletInfo.fiSpDelta                    = -(int)funcletFrameSizeAligned;
    genFuncletInfo.fiSaveRegs                   = rsMaskSaveRegs;
    genFuncletInfo.fiSP_to_CalleeSaved_delta    = SP_to_CalleeSaved_delta;
    genFuncletInfo.fiSP_to_PSP_slot_delta       = funcletFrameSizeAligned - osrPad - 8;
    genFuncletInfo.fiCallerSP_to_PSP_slot_delta = -(int)osrPad - 8;

#ifdef DEBUG
    if (verbose)
    {
        printf("\n");
        printf("Funclet prolog / epilog info\n");
        printf("                 Save regs: ");
        dspRegMask(genFuncletInfo.fiSaveRegs);
        printf("\n");
        if (compiler->opts.IsOSR())
        {
            printf("                           OSR Pad: %d\n", osrPad);
        }
        printf("     Function CallerSP-to-FP delta: %d\n", genFuncletInfo.fiFunction_CallerSP_to_FP_delta);
        printf("  SP to CalleeSaved location delta: %d\n", genFuncletInfo.fiSP_to_CalleeSaved_delta);
        printf("                          SP delta: %d\n", genFuncletInfo.fiSpDelta);
    }
    assert(genFuncletInfo.fiSP_to_CalleeSaved_delta >= 0);

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        assert(genFuncletInfo.fiCallerSP_to_PSP_slot_delta ==
               compiler->lvaGetCallerSPRelativeOffset(compiler->lvaPSPSym)); // same offset used in main function and
                                                                             // funclet!
    }
#endif // DEBUG
}

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif // DEBUG

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

    VarSetOps::Assign(compiler, gcInfo.gcVarPtrSetCur, GetEmitter()->emitInitGCrefVars);
    gcInfo.gcRegGCrefSetCur = GetEmitter()->emitInitGCrefRegs;
    gcInfo.gcRegByrefSetCur = GetEmitter()->emitInitByrefRegs;

#ifdef DEBUG
    if (compiler->opts.dspCode)
    {
        printf("\n__epilog:\n");
    }

    if (verbose)
    {
        printf("gcVarPtrSetCur=%s ", VarSetOps::ToString(compiler, gcInfo.gcVarPtrSetCur));
        dumpConvertedVarSet(compiler, gcInfo.gcVarPtrSetCur);
        printf(", gcRegGCrefSetCur=");
        printRegMaskInt(gcInfo.gcRegGCrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur);
        printf(", gcRegByrefSetCur=");
        printRegMaskInt(gcInfo.gcRegByrefSetCur);
        GetEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur);
        printf("\n");
    }
#endif // DEBUG

    bool jmpEpilog = block->HasFlag(BBF_HAS_JMP);

    GenTree* lastNode = block->lastNode();

    // Method handle and address info used in case of jump epilog
    CORINFO_METHOD_HANDLE methHnd = nullptr;
    CORINFO_CONST_LOOKUP  addrInfo;
    addrInfo.addr       = nullptr;
    addrInfo.accessType = IAT_VALUE;

    if (jmpEpilog && (lastNode->gtOper == GT_JMP))
    {
        methHnd = (CORINFO_METHOD_HANDLE)lastNode->AsVal()->gtVal1;
        compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &addrInfo);
    }

    compiler->unwindBegEpilog();

    if (jmpEpilog)
    {
        SetHasTailCalls(true);

        noway_assert(block->KindIs(BBJ_RETURN));
        noway_assert(block->GetFirstLIRNode() != nullptr);

        /* figure out what jump we have */
        GenTree* jmpNode = lastNode;
#if !FEATURE_FASTTAILCALL
        noway_assert(jmpNode->gtOper == GT_JMP);
#else  // FEATURE_FASTTAILCALL
        // armarch
        // If jmpNode is GT_JMP then gtNext must be null.
        // If jmpNode is a fast tail call, gtNext need not be null since it could have embedded stmts.
        noway_assert((jmpNode->gtOper != GT_JMP) || (jmpNode->gtNext == nullptr));

        // Could either be a "jmp method" or "fast tail call" implemented as epilog+jmp
        noway_assert((jmpNode->gtOper == GT_JMP) ||
                     ((jmpNode->gtOper == GT_CALL) && jmpNode->AsCall()->IsFastTailCall()));

        // The next block is associated with this "if" stmt
        if (jmpNode->gtOper == GT_JMP)
#endif // FEATURE_FASTTAILCALL
        {
            // Simply emit a jump to the methodHnd. This is similar to a call so we can use
            // the same descriptor with some minor adjustments.
            assert(methHnd != nullptr);
            assert(addrInfo.addr != nullptr);

            emitter::EmitCallType callType;
            void*                 addr;
            regNumber             indCallReg;
            switch (addrInfo.accessType)
            {
                case IAT_VALUE:
                // TODO-RISCV64-CQ: using B/BL for optimization.
                case IAT_PVALUE:
                    // Load the address into a register, load indirect and call  through a register
                    // We have to use REG_INDIRECT_CALL_TARGET_REG since we assume the argument registers are in use
                    callType   = emitter::EC_INDIR_R;
                    indCallReg = REG_INDIRECT_CALL_TARGET_REG;
                    addr       = NULL;
                    instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, indCallReg, (ssize_t)addrInfo.addr);
                    if (addrInfo.accessType == IAT_PVALUE)
                    {
                        GetEmitter()->emitIns_R_R_I(INS_ld, EA_PTRSIZE, indCallReg, indCallReg, 0);
                        regSet.verifyRegUsed(indCallReg);
                    }
                    break;

                case IAT_RELPVALUE:
                {
                    // Load the address into a register, load relative indirect and call through a register
                    // We have to use R12 since we assume the argument registers are in use
                    // LR is used as helper register right before it is restored from stack, thus,
                    // all relative address calculations are performed before LR is restored.
                    callType   = emitter::EC_INDIR_R;
                    indCallReg = REG_T2;
                    addr       = NULL;

                    regSet.verifyRegUsed(indCallReg);
                    break;
                }

                case IAT_PPVALUE:
                default:
                    NO_WAY("Unsupported JMP indirection");
            }

            /* Simply emit a jump to the methodHnd. This is similar to a call so we can use
             * the same descriptor with some minor adjustments.
             */

            genPopCalleeSavedRegisters(true);

            // clang-format off
            GetEmitter()->emitIns_Call(callType,
                                       methHnd,
                                       INDEBUG_LDISASM_COMMA(nullptr)
                                       addr,
                                       0,          // argSize
                                       EA_UNKNOWN // retSize
                                       MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(EA_UNKNOWN), // secondRetSize
                                       gcInfo.gcVarPtrSetCur,
                                       gcInfo.gcRegGCrefSetCur,
                                       gcInfo.gcRegByrefSetCur,
                                       DebugInfo(),
                                       indCallReg,    // ireg
                                       REG_NA,        // xreg
                                       0,             // xmul
                                       0,             // disp
                                       true);         // isJump
            // clang-format on
            CLANG_FORMAT_COMMENT_ANCHOR;
        }
#if FEATURE_FASTTAILCALL
        else
        {
            genPopCalleeSavedRegisters(true);
            genCallInstruction(jmpNode->AsCall());
        }
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        genPopCalleeSavedRegisters(false);

        GetEmitter()->emitIns_R_R_I(INS_jalr, EA_PTRSIZE, REG_R0, REG_RA, 0);
        compiler->unwindReturn(REG_RA);
    }

    compiler->unwindEndEpilog();
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

    genInstrWithConstant(INS_addi, EA_PTRSIZE, regTmp, REG_SPBASE, SPtoCallerSPdelta, regTmp, false);
    GetEmitter()->emitIns_S_R(INS_sd, EA_PTRSIZE, regTmp, compiler->lvaPSPSym, 0);
}

void CodeGen::genZeroInitFrameUsingBlockInit(int untrLclHi, int untrLclLo, regNumber initReg, bool* pInitRegZeroed)
{
    regNumber rAddr;
    regNumber rCnt = REG_NA; // Invalid
    regMaskTP regMask;

    regMaskTP availMask = regSet.rsGetModifiedRegsMask() | RBM_INT_CALLEE_TRASH; // Set of available registers
    // see: src/jit/registerriscv64.h
    availMask &= ~intRegState.rsCalleeRegArgMaskLiveIn; // Remove all of the incoming argument registers as they are
                                                        // currently live
    availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg as we will zero it and maybe use it for
                                       // a large constant.

    rAddr           = initReg;
    *pInitRegZeroed = false;

    // rAddr is not a live incoming argument reg
    assert((genRegMask(rAddr) & intRegState.rsCalleeRegArgMaskLiveIn) == 0);
    assert(untrLclLo % 4 == 0);

    if (emitter::isValidSimm12(untrLclLo))
    {
        GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, rAddr, genFramePointerReg(), untrLclLo);
    }
    else
    {
        // Load immediate into the InitReg register
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, (ssize_t)untrLclLo);
        GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, rAddr, genFramePointerReg(), initReg);
        *pInitRegZeroed = false;
    }

    bool     useLoop   = false;
    unsigned uCntBytes = untrLclHi - untrLclLo;
    assert((uCntBytes % sizeof(int)) == 0); // The smallest stack slot is always 4 bytes.
    unsigned int padding = untrLclLo & 0x7;

    if (padding)
    {
        assert(padding == 4);
        GetEmitter()->emitIns_R_R_I(INS_sw, EA_4BYTE, REG_R0, rAddr, 0);
        uCntBytes -= 4;
    }

    unsigned uCntSlots = uCntBytes / REGSIZE_BYTES; // How many register sized stack slots we're going to use.

    // When uCntSlots is 9 or less, we will emit a sequence of sd instructions inline.
    // When it is 10 or greater, we will emit a loop containing a sd instruction.
    // In both of these cases the sd instruction will write two zeros to memory
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

        noway_assert(uCntSlots >= 2);
        assert((genRegMask(rCnt) & intRegState.rsCalleeRegArgMaskLiveIn) == 0); // rCnt is not a live incoming
                                                                                // argument reg
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rCnt, (ssize_t)uCntSlots / 2);

        // TODO-RISCV64: maybe optimize further
        GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, rAddr, 8 + padding);
        GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, rAddr, 0 + padding);
        GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, rCnt, rCnt, -1);

        // bne rCnt, zero, -4 * 4
        ssize_t imm = -16;
        GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, rAddr, rAddr, 2 * REGSIZE_BYTES);
        GetEmitter()->emitIns_R_R_I(INS_bne, EA_PTRSIZE, rCnt, REG_R0, imm);

        uCntBytes %= REGSIZE_BYTES * 2;
    }
    else
    {
        while (uCntBytes >= REGSIZE_BYTES * 2)
        {
            GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, rAddr, 8 + padding);
            GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, rAddr, 0 + padding);
            GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, rAddr, rAddr, 2 * REGSIZE_BYTES + padding);
            uCntBytes -= REGSIZE_BYTES * 2;
            padding = 0;
        }
    }

    if (uCntBytes >= REGSIZE_BYTES) // check and zero the last register-sized stack slot (odd number)
    {
        if ((uCntBytes - REGSIZE_BYTES) == 0)
        {
            GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, rAddr, padding);
        }
        else
        {
            GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, rAddr, padding);
            GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, rAddr, rAddr, REGSIZE_BYTES);
        }
        uCntBytes -= REGSIZE_BYTES;
    }
    if (uCntBytes > 0)
    {
        assert(uCntBytes == sizeof(int));
        GetEmitter()->emitIns_R_R_I(INS_sw, EA_4BYTE, REG_R0, rAddr, padding);
        uCntBytes -= sizeof(int);
    }
    noway_assert(uCntBytes == 0);
}

void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
#if !FEATURE_FIXED_OUT_ARGS
    assert((tgtBlock->bbTgtStkDepth * sizeof(int) == genStackLevel) || isFramePointerUsed());
#endif // !FEATURE_FIXED_OUT_ARGS

    GetEmitter()->emitIns_J(emitter::emitJumpKindToIns(jmp), tgtBlock);
}

BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    assert(block->KindIs(BBJ_CALLFINALLY));

    // Generate a call to the finally, like this:
    //      mov  a0,qword ptr [fp + 10H] / sp    // Load a0 with PSPSym, or sp if PSPSym is not used
    //      jal  finally-funclet
    //      j    finally-return                  // Only for non-retless finally calls
    // The 'b' can be a NOP if we're going to the next block.

    if (compiler->lvaPSPSym != BAD_VAR_NUM)
    {
        GetEmitter()->emitIns_R_S(INS_ld, EA_PTRSIZE, REG_A0, compiler->lvaPSPSym, 0);
    }
    else
    {
        GetEmitter()->emitIns_R_R_I(INS_ori, EA_PTRSIZE, REG_A0, REG_SPBASE, 0);
    }
    GetEmitter()->emitIns_J(INS_jal, block->GetTarget());

    BasicBlock* const nextBlock = block->Next();

    if (block->HasFlag(BBF_RETLESS_CALL))
    {
        // We have a retless call, and the last instruction generated was a call.
        // If the next block is in a different EH region (or is the end of the code
        // block), then we need to generate a breakpoint here (since it will never
        // get executed) to get proper unwind behavior.

        if ((nextBlock == nullptr) || !BasicBlock::sameEHRegion(block, nextBlock))
        {
            instGen(INS_ebreak); // This should never get executed
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
            // TODO-RISCV64-CQ: Can we get rid of this instruction, and just have the call return directly
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
    GetEmitter()->emitIns_R_L(INS_lea, EA_PTRSIZE, block->GetTarget(), REG_INTRET);
}

//  move an immediate value into an integer register
void CodeGen::instGen_Set_Reg_To_Imm(emitAttr  size,
                                     regNumber reg,
                                     ssize_t   imm,
                                     insFlags flags DEBUGARG(size_t targetHandle) DEBUGARG(GenTreeFlags gtFlags))
{
    emitter* emit = GetEmitter();

    if (!compiler->opts.compReloc)
    {
        size = EA_SIZE(size); // Strip any Reloc flags from size if we aren't doing relocs.
    }

    if (EA_IS_RELOC(size))
    {
        assert(genIsValidIntReg(reg));
        GetEmitter()->emitIns_R_AI(INS_jal, size, reg, imm);
    }
    else
    {
        emit->emitLoadImmediate(size, reg, imm);
    }

    regSet.verifyRegUsed(reg);
}

void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, GenTree* tree)
{
    switch (tree->gtOper)
    {
        case GT_CNS_INT:
        {
            // relocatable values tend to come down as a CNS_INT of native int type
            // so the line between these two opcodes is kind of blurry
            GenTreeIntCon* con    = tree->AsIntCon();
            ssize_t        cnsVal = con->IconValue();

            emitAttr attr = emitActualTypeSize(targetType);
            // TODO-RISCV64-CQ: Currently we cannot do this for all handles because of
            // https://github.com/dotnet/runtime/issues/60712
            if (con->ImmedValNeedsReloc(compiler))
            {
                attr = EA_SET_FLG(attr, EA_CNS_RELOC_FLG);
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

            // Make sure we use "fmv.w.x reg, zero" only for positive zero (0.0)
            // and not for negative zero (-0.0)
            if (FloatingPointUtils::isPositiveZero(constValue))
            {
                // A faster/smaller way to generate 0.0
                // We will just zero out the entire vector register for both float and double
                emit->emitIns_R_R(size == EA_4BYTE ? INS_fmv_w_x : INS_fmv_d_x, size, targetReg, REG_R0);
            }
            else
            {
                // Get a temp integer register to compute long address.
                // regNumber addrReg = tree->GetSingleTempReg();

                // We must load the FP constant from the constant pool
                // Emit a data section constant for the float or double constant.
                CORINFO_FIELD_HANDLE hnd = emit->emitFltOrDblConst(constValue, size);

                // Load the FP constant.
                assert(emit->isFloatReg(targetReg));

                // Compute the address of the FP constant and load the data.
                emit->emitIns_R_C(size == EA_4BYTE ? INS_flw : INS_fld, size, targetReg, REG_NA, hnd, 0);
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
    regNumber targetReg = tree->GetRegNum();

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(!tree->isContained());
    // The dst can only be a register.
    assert(targetReg != REG_NA);

    GenTree* operand = tree->gtGetOp1();
    assert(!operand->isContained());
    // The src must be a register.
    regNumber operandReg = genConsumeReg(operand);
    emitAttr  attr       = emitActualTypeSize(tree);

    GetEmitter()->emitIns_R_R_I(INS_addi, attr, targetReg, operandReg, 1);
    // bne targetReg, zero, 2 * 4
    GetEmitter()->emitIns_R_R_I(INS_bne, attr, targetReg, REG_R0, 8);
    GetEmitter()->emitIns_R_R_I(INS_xori, attr, targetReg, targetReg, -1);

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

    // op1 and op2 can only be a reg at present, will amend in the future.
    assert(!op1->isContained());
    assert(!op2->isContained());

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(targetReg != REG_NA);

    if (EA_SIZE(attr) == EA_8BYTE)
    {
        instruction ins = isUnsigned ? INS_mulhu : INS_mulh;

        emit->emitIns_R_R_R(ins, attr, targetReg, op1->GetRegNum(), op2->GetRegNum());
    }
    else
    {
        assert(EA_SIZE(attr) == EA_4BYTE);
        if (isUnsigned)
        {
            regNumber tempReg = treeNode->GetSingleTempReg();
            emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tempReg, op1->GetRegNum(), 32);
            emit->emitIns_R_R_I(INS_slli, EA_8BYTE, targetReg, op2->GetRegNum(), 32);
            emit->emitIns_R_R_R(INS_mulhu, EA_8BYTE, targetReg, tempReg, targetReg);
            emit->emitIns_R_R_I(INS_srai, attr, targetReg, targetReg, 32);
        }
        else
        {
            emit->emitIns_R_R_R(INS_mul, EA_8BYTE, targetReg, op1->GetRegNum(), op2->GetRegNum());
            emit->emitIns_R_R_I(INS_srai, attr, targetReg, targetReg, 32);
        }
    }

    genProduceReg(treeNode);
}

// Generate code for ADD, SUB, MUL, AND, AND_NOT, OR and XOR
// This method is expected to have called genConsumeOperands() before calling it.
void CodeGen::genCodeForBinary(GenTreeOp* treeNode)
{
    const genTreeOps oper      = treeNode->OperGet();
    regNumber        targetReg = treeNode->GetRegNum();
    emitter*         emit      = GetEmitter();

    assert(treeNode->OperIs(GT_ADD, GT_SUB, GT_MUL, GT_AND, GT_AND_NOT, GT_OR, GT_XOR));

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

    if (!isRegCandidate && !tree->IsMultiReg() && !(tree->gtFlags & GTF_SPILLED))
    {
        var_types targetType = varDsc->GetRegisterType(tree);
        // targetType must be a normal scalar type and not a TYP_STRUCT
        assert(targetType != TYP_STRUCT);

        instruction ins  = ins_Load(targetType);
        emitAttr    attr = emitTypeSize(targetType);

        GetEmitter()->emitIns_R_S(ins, attr, tree->GetRegNum(), varNum, 0);
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
        genMultiRegStoreToLocal(lclNode);
        return;
    }

    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNode);
    if (lclNode->IsMultiReg())
    {
        // This is the case of storing to a multi-reg local, currently supported
        // only in ARM64 CodeGen. It may require HFA and SIMD features enabled.
        NYI_RISCV64("genCodeForStoreLclVar-----unimplemented on RISCV64 yet----");
    }
    else
    {
        regNumber targetReg  = lclNode->GetRegNum();
        emitter*  emit       = GetEmitter();
        unsigned  varNum     = lclNode->GetLclNum();
        var_types targetType = varDsc->GetRegisterType(lclNode);

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

            // TODO-RISCV64-CQ: supporting the SIMD.
            assert(!varTypeIsSIMD(targetType));

            if (zeroInit)
            {
                dataReg = REG_R0;
            }
            else if (data->IsIntegralConst())
            {
                ssize_t imm = data->AsIntConCommon()->IconValue();
                emit->emitLoadImmediate(EA_PTRSIZE, rsGetRsvdReg(), imm);
                dataReg = rsGetRsvdReg();
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
            if (data->IsIconHandle(GTF_ICON_TLS_HDL))
            {
                assert(data->AsIntCon()->IconValue() == 0);
                emitAttr attr = emitActualTypeSize(targetType);
                // need to load the address from thread pointer reg
                emit->emitIns_R_R(INS_mov, attr, targetReg, REG_TP);
            }
            else
            {
                inst_Mov(targetType, targetReg, dataReg, true);
            }
            genProduceReg(lclNode);
        }
    }
}

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
            GetEmitter()->emitIns_R_R_R(attr == EA_4BYTE ? INS_fsgnj_s : INS_fsgnj_d, attr, retReg, op1->GetRegNum(),
                                        op1->GetRegNum());
        }
        else
        {
            GetEmitter()->emitIns_R_R_I(attr == EA_4BYTE ? INS_addiw : INS_addi, attr, retReg, op1->GetRegNum(), 0);
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
    regNumber            tempReg                  = REG_NA;
    regNumber            pspSymReg                = REG_NA;
    var_types            type                     = genActualType(size->gtType);
    emitAttr             easz                     = emitTypeSize(type);
    BasicBlock*          endLabel                 = nullptr; // can optimize for riscv64.
    unsigned             stackAdjustment          = 0;
    const target_ssize_t ILLEGAL_LAST_TOUCH_DELTA = (target_ssize_t)-1;

    // The number of bytes from SP to the last stack address probed.
    target_ssize_t lastTouchDelta = ILLEGAL_LAST_TOUCH_DELTA;

    noway_assert(isFramePointerUsed()); // localloc requires Frame Pointer to be established since SP changes
    noway_assert(genStackLevel == 0);   // Can't have anything on the stack

    const target_ssize_t pageSize = compiler->eeGetPageSize();

    // According to RISC-V Privileged ISA page size is 4KiB
    noway_assert(pageSize == 0x1000);

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
        inst_RV_IV(INS_addi, regCnt, (STACK_ALIGN - 1), emitActualTypeSize(type));

        emit->emitIns_R_R_I(INS_andi, emitActualTypeSize(type), regCnt, regCnt, ~(STACK_ALIGN - 1));
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
        // assert((compiler->lvaOutgoingArgSpaceSize % STACK_ALIGN) == 0); // This must be true for the stack to remain
        //                                                                // aligned
        tempReg = tree->ExtractTempReg();
        genInstrWithConstant(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, outgoingArgSpaceAligned, tempReg);
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
        if (compiler->info.compInitMem)
        {
            if (stpCount <= 4)
            {
                imm = -16 * stpCount;
                emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, imm);

                imm = -imm;
                while (stpCount != 0)
                {
                    imm -= 8;
                    emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, REG_SPBASE, imm);
                    imm -= 8;
                    emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, REG_SPBASE, imm);
                    stpCount -= 1;
                }

                lastTouchDelta = 0;

                goto ALLOC_DONE;
            }
        }
        else if (amount < pageSize) // must be < not <=
        {
            // Since the size is less than a page, simply adjust the SP value.
            // The SP might already be in the guard page, so we must touch it BEFORE
            // the alloc, not after.

            // ld_w r0, 0(SP)
            emit->emitIns_R_R_I(INS_lw, EA_4BYTE, REG_R0, REG_SP, 0);

            lastTouchDelta = amount;
            imm            = -(ssize_t)amount;
            if (emitter::isValidSimm12(imm))
            {
                emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, imm);
            }
            else
            {
                if (tempReg == REG_NA)
                    tempReg = tree->ExtractTempReg();
                emit->emitLoadImmediate(EA_PTRSIZE, tempReg, amount);
                emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, tempReg);
            }

            goto ALLOC_DONE;
        }

        // else, "mov regCnt, amount"
        // If compInitMem=true, we can reuse targetReg as regcnt.
        // Since size is a constant, regCnt is not yet initialized.
        assert(regCnt == REG_NA);
        if (compiler->info.compInitMem)
        {
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
        // At this point 'regCnt' is set to the total number of bytes to locAlloc.
        // Since we have to zero out the allocated memory AND ensure that the stack pointer is always valid
        // by tickling the pages, we will just push 0's on the stack.
        //
        // Note: regCnt is guaranteed to be even on Amd64 since STACK_ALIGN/TARGET_POINTER_SIZE = 2
        // and localloc size is a multiple of STACK_ALIGN.

        // Loop:
        ssize_t imm = -16;
        emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, imm);

        emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, REG_SPBASE, 8);
        emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, REG_SPBASE, 0);

        // If not done, loop
        // Note that regCnt is the number of bytes to stack allocate.
        // Therefore we need to subtract 16 from regcnt here.
        assert(genIsValidIntReg(regCnt));

        emit->emitIns_R_R_I(INS_addi, emitActualTypeSize(type), regCnt, regCnt, -16);

        assert(imm == (-4 << 2)); // goto loop.
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
        //       sltu     RA, SP, regCnt
        //       sub      regCnt, SP, regCnt      // regCnt now holds ultimate SP
        //       beq      RA, REG_R0, Skip
        //       addi     regCnt, REG_R0, 0
        //
        //  Skip:
        //       lui      regTmp, eeGetPageSize()>>12
        //  Loop:
        //       lw       r0, 0(SP)               // tickle the page - read from the page
        //       sub      RA, SP, regTmp          // decrement SP by eeGetPageSize()
        //       bltu     RA, regCnt, Done
        //       sub      SP, SP,regTmp
        //       j        Loop
        //
        //  Done:
        //       mov      SP, regCnt
        //

        if (tempReg == REG_NA)
            tempReg = tree->ExtractTempReg();

        regNumber rPageSize = tree->GetSingleTempReg();

        assert(regCnt != tempReg);
        emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, tempReg, REG_SPBASE, regCnt);

        // sub  regCnt, SP, regCnt      // regCnt now holds ultimate SP
        emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, regCnt, REG_SPBASE, regCnt);

        // Overflow, set regCnt to lowest possible value
        emit->emitIns_R_R_I(INS_beq, EA_PTRSIZE, tempReg, REG_R0, 2 << 2);
        emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, regCnt, REG_R0, 0);

        emit->emitIns_R_I(INS_lui, EA_PTRSIZE, rPageSize, pageSize >> 12);

        // genDefineTempLabel(loop);

        // tickle the page - Read from the updated SP - this triggers a page fault when on the guard page
        emit->emitIns_R_R_I(INS_lw, EA_4BYTE, REG_R0, REG_SPBASE, 0);

        // decrement SP by eeGetPageSize()
        emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, tempReg, REG_SPBASE, rPageSize);

        assert(rPageSize != tempReg);

        ssize_t imm = 3 << 2; // goto done.
        emit->emitIns_R_R_I(INS_bltu, EA_PTRSIZE, tempReg, regCnt, imm);

        emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, rPageSize);

        imm = -4 << 2;
        // Jump to loop and tickle new stack address
        emit->emitIns_I(INS_j, EA_PTRSIZE, imm);

        // Done with stack tickle loop
        // genDefineTempLabel(done);

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

        if ((lastTouchDelta == ILLEGAL_LAST_TOUCH_DELTA) ||
            (stackAdjustment + (unsigned)lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize))
        {
            genStackPointerConstantAdjustmentLoopWithProbe(-(ssize_t)stackAdjustment, tempReg);
        }
        else
        {
            genStackPointerConstantAdjustment(-(ssize_t)stackAdjustment, tempReg);
        }

        // Return the stackalloc'ed address in result register.
        // TargetReg = SP + stackAdjustment.
        //
        genInstrWithConstant(INS_addi, EA_PTRSIZE, targetReg, REG_SPBASE, (ssize_t)stackAdjustment, tempReg);
    }
    else // stackAdjustment == 0
    {
        // Move the final value of SP to targetReg
        emit->emitIns_R_R_I(INS_ori, EA_PTRSIZE, targetReg, REG_SPBASE, 0);
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

    regNumber targetReg = tree->GetRegNum();

    // The arithmetic node must be sitting in a register (since it's not contained)
    assert(!tree->isContained());
    // The dst can only be a register.
    assert(targetReg != REG_NA);

    GenTree* operand = tree->gtGetOp1();
    assert(!operand->isContained());
    // The src must be a register.
    regNumber operandReg = genConsumeReg(operand);

    emitAttr attr = emitActualTypeSize(tree);
    if (tree->OperIs(GT_NEG))
    {
        if (varTypeIsFloating(targetType))
        {
            GetEmitter()->emitIns_R_R_R(targetType == TYP_DOUBLE ? INS_fsgnjn_d : INS_fsgnjn_s, attr, targetReg,
                                        operandReg, operandReg);
        }
        else
        {
            GetEmitter()->emitIns_R_R_R(attr == EA_4BYTE ? INS_subw : INS_sub, attr, targetReg, REG_R0, operandReg);
        }
    }
    else if (tree->OperIs(GT_NOT))
    {
        assert(!varTypeIsFloating(targetType));
        GetEmitter()->emitIns_R_R_I(INS_xori, attr, targetReg, operandReg, -1);
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
    NYI_RISCV64("genCodeForBswap-----unimplemented on RISCV64 yet----");
}

//------------------------------------------------------------------------
// genCodeForDivMod: Produce code for a GT_DIV/GT_UDIV node.
// (1) float/double MOD is morphed into a helper call by front-end.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForDivMod(GenTreeOp* tree)
{
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

        instruction ins = genGetInsForOper(tree);
        emit->emitIns_R_R_R(ins, emitActualTypeSize(targetType), tree->GetRegNum(), tree->gtOp1->GetRegNum(),
                            tree->gtOp2->GetRegNum());
    }
    else // an integer divide operation
    {
        GenTree*  dividendOp  = tree->gtGetOp1();
        GenTree*  divisorOp   = tree->gtGetOp2();
        regNumber dividendReg = dividendOp->GetRegNum();
        regNumber divisorReg  = divisorOp->GetRegNum();

        // divisorOp can be immed or reg
        assert(!dividendOp->isContained() && !dividendOp->isContainedIntOrIImmed());
        assert(!divisorOp->isContained() || divisorOp->isContainedIntOrIImmed());

        ExceptionSetFlags exceptions = tree->OperExceptions(compiler);
        if ((exceptions & ExceptionSetFlags::DivideByZeroException) != ExceptionSetFlags::None)
        {
            if (divisorOp->IsIntegralConst(0) || divisorOp->GetRegNum() == REG_ZERO)
            {
                // We unconditionally throw a divide by zero exception
                genJumpToThrowHlpBlk(EJ_jmp, SCK_DIV_BY_ZERO);
                genProduceReg(tree);
                return;
            }
            else // the divisor is not the constant zero
            {
                assert(emitter::isGeneralRegister(divisorReg));

                // Check if the divisor is zero throw a DivideByZeroException
                genJumpToThrowHlpBlk_la(SCK_DIV_BY_ZERO, INS_beq, divisorReg);
            }
        }

        assert(!divisorOp->IsIntegralConst(0));

        regNumber   tempReg = REG_NA;
        instruction ins;

        // Check divisorOp first as we can always allow it to be a contained immediate
        if (divisorOp->isContainedIntOrIImmed())
        {
            ssize_t intConst = (int)(divisorOp->AsIntCon()->gtIconVal);
            if (!emitter::isGeneralRegister(divisorReg))
            {
                tempReg    = tree->GetSingleTempReg();
                divisorReg = tempReg;
            }
            emit->emitLoadImmediate(EA_PTRSIZE, divisorReg, intConst);
        }
        else
        {
            // dividend can only be a reg
            assert(!dividendOp->isContained());
            assert(emitter::isGeneralRegister(dividendReg));
            assert(emitter::isGeneralRegister(divisorReg));
        }

        emitAttr size = EA_ATTR(genTypeSize(genActualType(tree)));
        bool     is4  = (size == EA_4BYTE);
        assert(is4 || (size == EA_8BYTE));

        // check (MinInt / -1) => ArithmeticException
        if (tree->OperIs(GT_DIV, GT_MOD))
        {
            if ((exceptions & ExceptionSetFlags::ArithmeticException) != ExceptionSetFlags::None)
            {
                if (tempReg == REG_NA)
                    tempReg = tree->GetSingleTempReg();

                // Check if the divisor is not -1 branch to 'sdivLabel'
                emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, tempReg, REG_ZERO, -1);
                BasicBlock* sdivLabel = genCreateTempLabel(); // can optimize for riscv64.
                emit->emitIns_J_cond_la(INS_bne, sdivLabel, tempReg, divisorReg);

                // If control flow continues past here the 'divisorReg' is known to be -1
                regNumber dividendReg = tree->gtGetOp1()->GetRegNum();

                // Build MinInt=0x80000000(00000000) in tempReg from -1
                instruction shiftIns = is4 ? INS_slliw : INS_slli;
                int         shiftBy  = is4 ? 31 : 63;
                emit->emitIns_R_R_I(shiftIns, size, tempReg, tempReg, shiftBy);

                // Check whether dividendReg is MinInt or not
                genJumpToThrowHlpBlk_la(SCK_ARITH_EXCPN, INS_beq, tempReg, nullptr, dividendReg);
                genDefineTempLabel(sdivLabel);
            }

            // Generate the sdiv instruction
            if (tree->OperIs(GT_DIV))
            {
                ins = is4 ? INS_divw : INS_div;
            }
            else
            {
                ins = is4 ? INS_remw : INS_rem;
            }
            emit->emitIns_R_R_R(ins, size, tree->GetRegNum(), dividendReg, divisorReg);
        }
        else // if (tree->OperIs(GT_UDIV, GT_UMOD))
        {
            if (tree->OperIs(GT_UDIV))
            {
                ins = is4 ? INS_divuw : INS_divu;
            }
            else
            {
                ins = is4 ? INS_remuw : INS_remu;
            }
            emit->emitIns_R_R_R(ins, size, tree->GetRegNum(), dividendReg, divisorReg);
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
        assert(dstAddr->OperIs(GT_LCL_ADDR));
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
            emit->emitIns_S_R(INS_sd, EA_8BYTE, srcReg, dstLclNum, dstOffset);
            emit->emitIns_S_R(INS_sd, EA_8BYTE, srcReg, dstLclNum, dstOffset + 8);
        }
        else
        {
            emit->emitIns_R_R_I(INS_sd, EA_8BYTE, srcReg, dstAddrBaseReg, dstOffset);
            emit->emitIns_R_R_I(INS_sd, EA_8BYTE, srcReg, dstAddrBaseReg, dstOffset + 8);
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
                storeIns = INS_sb;
                attr     = EA_4BYTE;
                break;
            case 2:
                storeIns = INS_sh;
                attr     = EA_4BYTE;
                break;
            case 4:
                storeIns = INS_sw;
                attr     = EA_ATTR(regSize);
                break;
            case 8:
                storeIns = INS_sd;
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

// Generate code for CpObj nodes which copy structs that have interleaved
// GC pointers.
// For this case we'll generate a sequence of loads/stores in the case of struct
// slots that don't contain GC pointers.  The generated code will look like:
// ld tempReg, 8(a5)
// sd tempReg, 8(a6)
//
// In the case of a GC-Pointer we'll call the ByRef write barrier helper
// who happens to use the same registers as the previous call to maintain
// the same register requirements and register killsets:
// call CORINFO_HELP_ASSIGN_BYREF
//
// So finally an example would look like this:
// ld tempReg, 8(a5)
// sd tempReg 8(a6)
// call CORINFO_HELP_ASSIGN_BYREF
// ld tempReg, 8(a5)
// sd tempReg, 8(a6)
// call CORINFO_HELP_ASSIGN_BYREF
// ld tempReg, 8(a5)
// sd tempReg, 8(a6)
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
            if ((i + 2) == slots)
            {
                attrSrcAddr = EA_8BYTE;
                attrDstAddr = EA_8BYTE;
            }

            emit->emitIns_R_R_I(INS_ld, attr0, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
            emit->emitIns_R_R_I(INS_ld, attr1, tmpReg2, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_addi, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF,
                                2 * TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_sd, attr0, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
            emit->emitIns_R_R_I(INS_sd, attr1, tmpReg2, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_addi, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF,
                                2 * TARGET_POINTER_SIZE);
            i += 2;
        }

        // Use a ld/sd sequence for the last remainder
        if (i < slots)
        {
            emitAttr attr0 = emitTypeSize(layout->GetGCPtrType(i + 0));
            if (i + 1 >= slots)
            {
                attrSrcAddr = EA_8BYTE;
                attrDstAddr = EA_8BYTE;
            }

            emit->emitIns_R_R_I(INS_ld, attr0, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
            emit->emitIns_R_R_I(INS_addi, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF,
                                TARGET_POINTER_SIZE);
            emit->emitIns_R_R_I(INS_sd, attr0, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
            emit->emitIns_R_R_I(INS_addi, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF,
                                TARGET_POINTER_SIZE);
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
                    if ((i + 2) == slots)
                    {
                        attrSrcAddr = EA_8BYTE;
                        attrDstAddr = EA_8BYTE;
                    }
                    emit->emitIns_R_R_I(INS_ld, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
                    emit->emitIns_R_R_I(INS_ld, EA_8BYTE, tmpReg2, REG_WRITE_BARRIER_SRC_BYREF, TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_addi, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF,
                                        2 * TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_sd, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
                    emit->emitIns_R_R_I(INS_sd, EA_8BYTE, tmpReg2, REG_WRITE_BARRIER_DST_BYREF, TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_addi, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF,
                                        2 * TARGET_POINTER_SIZE);
                    ++i; // extra increment of i, since we are copying two items
                }
                else
                {
                    if (i + 1 >= slots)
                    {
                        attrSrcAddr = EA_8BYTE;
                        attrDstAddr = EA_8BYTE;
                    }
                    emit->emitIns_R_R_I(INS_ld, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_SRC_BYREF, 0);
                    emit->emitIns_R_R_I(INS_addi, attrSrcAddr, REG_WRITE_BARRIER_SRC_BYREF, REG_WRITE_BARRIER_SRC_BYREF,
                                        TARGET_POINTER_SIZE);
                    emit->emitIns_R_R_I(INS_sd, EA_8BYTE, tmpReg, REG_WRITE_BARRIER_DST_BYREF, 0);
                    emit->emitIns_R_R_I(INS_addi, attrDstAddr, REG_WRITE_BARRIER_DST_BYREF, REG_WRITE_BARRIER_DST_BYREF,
                                        TARGET_POINTER_SIZE);
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
        // issue a INS_BARRIER_RMB after a volatile CpObj operation
        // TODO-RISCV64: there is only BARRIER_FULL for RISCV64.
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
    GetEmitter()->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpReg, idxReg, 2);
    GetEmitter()->emitIns_R_R_R(INS_add, EA_8BYTE, baseReg, baseReg, tmpReg);
    GetEmitter()->emitIns_R_R_I(INS_lw, EA_4BYTE, baseReg, baseReg, 0);

    // add it to the absolute address of fgFirstBB
    GetEmitter()->emitIns_R_L(INS_lea, EA_PTRSIZE, compiler->fgFirstBB, tmpReg);
    GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, baseReg, baseReg, tmpReg);

    // jr baseReg
    GetEmitter()->emitIns_R_R_I(INS_jalr, emitActualTypeSize(TYP_I_IMPL), REG_R0, baseReg, 0);
}

// emits the table and an instruction to get the address of the first element
void CodeGen::genJumpTable(GenTree* treeNode)
{
    noway_assert(compiler->compCurBB->KindIs(BBJ_SWITCH));
    assert(treeNode->OperGet() == GT_JMPTABLE);

    unsigned   jumpCount = compiler->compCurBB->GetSwitchTargets()->bbsCount;
    FlowEdge** jumpTable = compiler->compCurBB->GetSwitchTargets()->bbsDstTab;
    unsigned   jmpTabOffs;
    unsigned   jmpTabBase;

    jmpTabBase = GetEmitter()->emitBBTableDataGenBeg(jumpCount, true);

    jmpTabOffs = 0;

    JITDUMP("\n      J_M%03u_DS%02u LABEL   DWORD\n", compiler->compMethodID, jmpTabBase);

    for (unsigned i = 0; i < jumpCount; i++)
    {
        BasicBlock* target = (*jumpTable)->getDestinationBlock();
        noway_assert(target->HasFlag(BBF_HAS_LABEL));

        JITDUMP("            DD      L_M%03u_" FMT_BB "\n", compiler->compMethodID, target->bbNum);

        GetEmitter()->emitDataGenData(i, target);
    };

    GetEmitter()->emitDataGenEnd();

    // Access to inline data is 'abstracted' by a special type of static member
    // (produced by eeFindJitDataOffs) which the emitter recognizes as being a reference
    // to constant data, not a real static field.
    GetEmitter()->emitIns_R_C(INS_jal, emitActualTypeSize(TYP_I_IMPL), treeNode->GetRegNum(), REG_NA,
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
    assert(!varTypeIsSmall(treeNode->TypeGet()));

    GenTree*  data      = treeNode->AsOp()->gtOp2;
    GenTree*  addr      = treeNode->AsOp()->gtOp1;
    regNumber dataReg   = data->GetRegNum();
    regNumber addrReg   = addr->GetRegNum();
    regNumber targetReg = treeNode->GetRegNum();
    if (targetReg == REG_NA)
    {
        targetReg = REG_R0;
    }

    genConsumeAddress(addr);
    genConsumeRegs(data);

    emitAttr dataSize = emitActualTypeSize(data);
    bool     is4      = (dataSize == EA_4BYTE);

    assert(!data->isContainedIntOrIImmed());

    instruction ins = INS_none;
    switch (treeNode->gtOper)
    {
        case GT_XORR:
            ins = is4 ? INS_amoor_w : INS_amoor_d;
            break;
        case GT_XAND:
            ins = is4 ? INS_amoand_w : INS_amoand_d;
            break;
        case GT_XCHG:
            ins = is4 ? INS_amoswap_w : INS_amoswap_d;
            break;
        case GT_XADD:
            ins = is4 ? INS_amoadd_w : INS_amoadd_d;
            break;
        default:
            noway_assert(!"Unexpected treeNode->gtOper");
    }
    GetEmitter()->emitIns_R_R_R(ins, dataSize, targetReg, addrReg, dataReg);

    if (targetReg != REG_R0)
    {
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
    assert(!varTypeIsSmall(treeNode->TypeGet()));

    GenTree* locOp       = treeNode->Addr();
    GenTree* valOp       = treeNode->Data();
    GenTree* comparandOp = treeNode->Comparand();

    regNumber target    = treeNode->GetRegNum();
    regNumber loc       = locOp->GetRegNum();
    regNumber val       = valOp->GetRegNum();
    regNumber comparand = comparandOp->GetRegNum();
    regNumber storeErr  = treeNode->ExtractTempReg(RBM_ALLINT);

    // Register allocator should have extended the lifetimes of all input and internal registers
    // They should all be different
    noway_assert(target != loc);
    noway_assert(target != val);
    noway_assert(target != comparand);
    noway_assert(target != storeErr);
    noway_assert(loc != val);
    noway_assert(loc != comparand);
    noway_assert(loc != storeErr);
    noway_assert(val != comparand);
    noway_assert(val != storeErr);
    noway_assert(comparand != storeErr);
    noway_assert(target != REG_NA);
    noway_assert(storeErr != REG_NA);

    assert(locOp->isUsedFromReg());
    assert(valOp->isUsedFromReg());
    assert(!comparandOp->isUsedFromMemory());

    genConsumeAddress(locOp);
    genConsumeRegs(valOp);
    genConsumeRegs(comparandOp);

    // NOTE: `genConsumeAddress` marks consumed register as not a GC pointer, assuming the input
    // registers die at the first generated instruction. However, here the input registers are reused,
    // so mark the location register as a GC pointer until code generation for this node is finished.
    gcInfo.gcMarkRegPtrVal(loc, locOp->TypeGet());

    BasicBlock* retry = genCreateTempLabel();
    BasicBlock* fail  = genCreateTempLabel();

    emitter* e    = GetEmitter();
    emitAttr size = emitActualTypeSize(valOp);
    bool     is4  = (size == EA_4BYTE);

    genDefineTempLabel(retry);
    e->emitIns_R_R_R(is4 ? INS_lr_w : INS_lr_d, size, target, loc, REG_R0); // load original value
    e->emitIns_J_cond_la(INS_bne, fail, target, comparand);                 // fail if doesn’t match
    e->emitIns_R_R_R(is4 ? INS_sc_w : INS_sc_d, size, storeErr, loc, val);  // try to update
    e->emitIns_J(INS_bnez, retry, storeErr);                                // retry if update failed
    genDefineTempLabel(fail);

    gcInfo.gcMarkRegSetNpt(locOp->gtGetRegMask());
    genProduceReg(treeNode);
}

static inline bool isImmed(GenTree* treeNode)
{
    assert(treeNode->OperIsBinary());

    if (treeNode->gtGetOp2()->isContainedIntOrIImmed())
    {
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
    emitAttr   attr  = emitActualTypeSize(treeNode);
    bool       isImm = false;

    instruction ins = INS_ebreak;

    if (varTypeIsFloating(type))
    {
        switch (oper)
        {
            case GT_ADD:
                if (attr == EA_4BYTE)
                {
                    ins = INS_fadd_s;
                }
                else
                {
                    ins = INS_fadd_d;
                }
                break;
            case GT_SUB:
                if (attr == EA_4BYTE)
                {
                    ins = INS_fsub_s;
                }
                else
                {
                    ins = INS_fsub_d;
                }
                break;
            case GT_MUL:
                if (attr == EA_4BYTE)
                {
                    ins = INS_fmul_s;
                }
                else
                {
                    ins = INS_fmul_d;
                }
                break;
            case GT_DIV:
                if (attr == EA_4BYTE)
                {
                    ins = INS_fdiv_s;
                }
                else
                {
                    ins = INS_fdiv_d;
                }
                break;

            default:
                NO_WAY("Unhandled oper in genGetInsForOper() - float");
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
                        ins = INS_addi;
                    }
                    else
                    {
                        assert(attr == EA_4BYTE);
                        ins = INS_addiw;
                    }
                }
                else
                {
                    if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                    {
                        ins = INS_add;
                    }
                    else
                    {
                        assert(attr == EA_4BYTE);
                        ins = INS_addw;
                    }
                }
                break;

            case GT_SUB:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_sub;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_subw;
                }
                break;

            case GT_MOD:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_rem;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_remw;
                }
                break;

            case GT_DIV:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_div;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_divw;
                }
                break;

            case GT_UMOD:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_remu;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_remuw;
                }
                break;

            case GT_UDIV:
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    ins = INS_divu;
                }
                else
                {
                    assert(attr == EA_4BYTE);
                    ins = INS_divuw;
                }
                break;

            case GT_MUL:
                // TODO-RISCV64-CQ: Need to implement for complex cases
                if ((attr == EA_8BYTE) || (attr == EA_BYREF))
                {
                    op2 = treeNode->gtGetOp2();
                    if (genActualTypeIsInt(op1) && genActualTypeIsInt(op2))
                        ins = INS_mulw;
                    else
                        ins = INS_mul;
                }
                else
                {
                    ins = INS_mulw;
                }
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

            case GT_AND_NOT:
                NYI_RISCV64("GT_AND_NOT-----unimplemented/unused on RISCV64 yet----");
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
                    // it's better to check sa.
                    if (attr == EA_4BYTE)
                    {
                        ins = INS_slliw;
                    }
                    else
                    {
                        ins = INS_slli;
                    }
                }
                else
                {
                    if (attr == EA_4BYTE)
                    {
                        ins = INS_sllw;
                    }
                    else
                    {
                        ins = INS_sll;
                    }
                }
                break;

            case GT_RSZ:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    // it's better to check sa.
                    if (attr == EA_4BYTE)
                    {
                        ins = INS_srliw;
                    }
                    else
                    {
                        ins = INS_srli;
                    }
                }
                else
                {
                    if (attr == EA_4BYTE)
                    {
                        ins = INS_srlw;
                    }
                    else
                    {
                        ins = INS_srl;
                    }
                }
                break;

            case GT_RSH:
                isImm = isImmed(treeNode);
                if (isImm)
                {
                    // it's better to check sa.
                    if (attr == EA_4BYTE)
                    {
                        ins = INS_sraiw;
                    }
                    else
                    {
                        ins = INS_srai;
                    }
                }
                else
                {
                    if (attr == EA_4BYTE)
                    {
                        ins = INS_sraw;
                    }
                    else
                    {
                        ins = INS_sra;
                    }
                }
                break;

            case GT_ROR:
                NYI_RISCV64("GT_ROR-----unimplemented/unused on RISCV64 yet----");
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
                NO_WAY("Unhandled oper in genGetInsForOper() - integer");
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

    void*                 pAddr = nullptr;
    void*                 addr  = compiler->compGetHelperFtn(CORINFO_HELP_STOP_FOR_GC, &pAddr);
    emitter::EmitCallType callType;
    regNumber             callTarget;

    if (addr == nullptr)
    {
        callType   = emitter::EC_INDIR_R;
        callTarget = REG_DEFAULT_HELPER_CALL_TARGET;

        if (compiler->opts.compReloc)
        {
            GetEmitter()->emitIns_R_AI(INS_jal, EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
        }
        else
        {
            // TODO-RISCV64: maybe optimize further.
            GetEmitter()->emitLoadImmediate(EA_PTRSIZE, callTarget, (ssize_t)pAddr);
            GetEmitter()->emitIns_R_R_I(INS_ld, EA_PTRSIZE, callTarget, callTarget, 0);
        }
        regSet.verifyRegUsed(callTarget);
    }
    else
    {
        callType   = emitter::EC_FUNC_TOKEN;
        callTarget = REG_NA;
    }

    // TODO-RISCV64: can optimize further !!!
    GetEmitter()->emitIns_Call(callType, compiler->eeFindHelper(CORINFO_HELP_STOP_FOR_GC),
                               INDEBUG_LDISASM_COMMA(nullptr) addr, 0, EA_UNKNOWN, EA_UNKNOWN, gcInfo.gcVarPtrSetCur,
                               gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur, DebugInfo(), /* IL offset */
                               callTarget,                                                    /* ireg */
                               REG_NA, 0, 0,                                                  /* xreg, xmul, disp */
                               false                                                          /* isJump */
                               );

    regMaskTP killMask = compiler->compHelperCallKillSet(CORINFO_HELP_STOP_FOR_GC);
    regSet.verifyRegistersUsed(killMask);

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
        genStoreIndTypeSIMD12(tree);
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

        // 'addr' goes into REG_T3 (REG_WRITE_BARRIER_DST)
        genCopyRegIfNeeded(addr, REG_WRITE_BARRIER_DST);

        // 'data' goes into REG_T4 (REG_WRITE_BARRIER_SRC)
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
void CodeGen::genCodeForSwap(GenTreeOp*)
{
    // For now GT_SWAP handling is only (partially) supported in ARM64 and XARCH CodeGens.
    NYI_RISCV64("genCodeForSwap-----unimplemented/unused on RISCV64 yet----");
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

    // We should never see a srcType whose size is neither EA_4BYTE or EA_8BYTE
    emitAttr srcSize = EA_ATTR(genTypeSize(srcType));
    noway_assert((srcSize == EA_4BYTE) || (srcSize == EA_8BYTE));

    bool        isUnsigned = treeNode->gtFlags & GTF_UNSIGNED;
    instruction ins        = INS_invalid;

    if (isUnsigned)
    {
        if (dstType == TYP_DOUBLE)
        {
            if (srcSize == EA_4BYTE)
            {
                ins = INS_fcvt_d_wu;
            }
            else
            {
                assert(srcSize == EA_8BYTE);
                ins = INS_fcvt_d_lu;
            }
        }
        else
        {
            assert(dstType == TYP_FLOAT);
            if (srcSize == EA_4BYTE)
            {
                ins = INS_fcvt_s_wu;
            }
            else
            {
                assert(srcSize == EA_8BYTE);
                ins = INS_fcvt_s_lu;
            }
        }
    }
    else
    {
        if (dstType == TYP_DOUBLE)
        {
            if (srcSize == EA_4BYTE)
            {
                ins = INS_fcvt_d_w;
            }
            else
            {
                assert(srcSize == EA_8BYTE);
                ins = INS_fcvt_d_l;
            }
        }
        else
        {
            assert(dstType == TYP_FLOAT);
            if (srcSize == EA_4BYTE)
            {
                ins = INS_fcvt_s_w;
            }
            else
            {
                assert(srcSize == EA_8BYTE);
                ins = INS_fcvt_s_l;
            }
        }
    }

    genConsumeOperands(treeNode->AsOp());

    GetEmitter()->emitIns_R_R(ins, emitActualTypeSize(dstType), treeNode->GetRegNum(), op1->GetRegNum());

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
    // int type --> float/double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    assert(genIsValidIntReg(treeNode->GetRegNum())); // Must be a valid int reg.

    GenTree* op1 = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());                  // Cannot be contained
    assert(genIsValidFloatReg(op1->GetRegNum())); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = genActualType(op1->TypeGet());
    assert(varTypeIsFloating(srcType) && !varTypeIsFloating(dstType));

    // We should never see a dstType whose size is neither EA_4BYTE or EA_8BYTE
    emitAttr dstSize = EA_ATTR(genTypeSize(dstType));
    noway_assert((dstSize == EA_4BYTE) || (dstSize == EA_8BYTE));

    bool        isUnsigned = varTypeIsUnsigned(dstType);
    instruction ins        = INS_invalid;

    if (isUnsigned)
    {
        if (srcType == TYP_DOUBLE)
        {
            if (dstSize == EA_4BYTE)
            {
                ins = INS_fcvt_wu_d;
            }
            else
            {
                ins = INS_fcvt_lu_d;
            }
        }
        else
        {
            assert(srcType == TYP_FLOAT);
            if (dstSize == EA_4BYTE)
            {
                ins = INS_fcvt_wu_s;
            }
            else
            {
                ins = INS_fcvt_lu_s;
            }
        }
    }
    else
    {
        if (srcType == TYP_DOUBLE)
        {
            if (dstSize == EA_4BYTE)
            {
                ins = INS_fcvt_w_d;
            }
            else
            {
                ins = INS_fcvt_l_d;
            }
        }
        else
        {
            assert(srcType == TYP_FLOAT);
            if (dstSize == EA_4BYTE)
            {
                ins = INS_fcvt_w_s;
            }
            else
            {
                ins = INS_fcvt_l_s;
            }
        }
    }

    genConsumeOperands(treeNode->AsOp());

    regNumber tmpReg = treeNode->GetSingleTempReg();
    assert(tmpReg != treeNode->GetRegNum());
    assert(tmpReg != op1->GetRegNum());

    GetEmitter()->emitIns_R_R(ins, dstSize, treeNode->GetRegNum(), op1->GetRegNum());

    // This part emulates the "flush to zero" option because the RISC-V specification does not provide it.
    instruction feq_ins = INS_feq_s;
    if (srcType == TYP_DOUBLE)
    {
        feq_ins = INS_feq_d;
    }
    // Compare op1 with itself to get 0 if op1 is NaN and 1 for any other value
    GetEmitter()->emitIns_R_R_R(feq_ins, dstSize, tmpReg, op1->GetRegNum(), op1->GetRegNum());
    // Get subtraction result of REG_ZERO (always 0) and feq result
    // As a result we get 0 for NaN and -1 (all bits set) for any other value
    GetEmitter()->emitIns_R_R_R(INS_sub, dstSize, tmpReg, REG_ZERO, tmpReg);
    // and instruction with received mask produces 0 for NaN and preserves any other value
    GetEmitter()->emitIns_R_R_R(INS_and, dstSize, treeNode->GetRegNum(), treeNode->GetRegNum(), tmpReg);

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

    GenTree*  op1        = treeNode->AsOp()->gtOp1;
    var_types targetType = treeNode->TypeGet();
    int       expMask    = 0x381; // 0b1110000001;

    emitter* emit = GetEmitter();
    emitAttr attr = emitActualTypeSize(treeNode);

    // Extract exponent into a register.
    regNumber intReg = treeNode->GetSingleTempReg();
    regNumber fpReg  = genConsumeReg(op1);

    emit->emitIns_R_R(attr == EA_4BYTE ? INS_fclass_s : INS_fclass_d, attr, intReg, fpReg);
    // Mask of exponent with all 1's and check if the exponent is all 1's
    emit->emitIns_R_R_I(INS_andi, EA_PTRSIZE, intReg, intReg, expMask);
    // If exponent is all 1's, throw ArithmeticException
    genJumpToThrowHlpBlk_la(SCK_ARITH_EXCPN, INS_bne, intReg);

    // if it is a finite value copy it to targetReg
    if (treeNode->GetRegNum() != fpReg)
    {
        inst_Mov(targetType, treeNode->GetRegNum(), fpReg, /* canSkip */ true);
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_EQ/GT_NE/GT_LT/GT_LE/GT_GE/GT_GT node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForCompare(GenTreeOp* tree)
{
    GenTree*  op1     = tree->gtOp1;
    GenTree*  op2     = tree->gtOp2;
    var_types op1Type = genActualType(op1->TypeGet());
    var_types op2Type = genActualType(op2->TypeGet());

    assert(!op1->isUsedFromMemory());
    assert(!op2->isUsedFromMemory());

    emitAttr cmpSize = EA_ATTR(genTypeSize(op1Type));

    assert(genTypeSize(op1Type) == genTypeSize(op2Type));

    emitter*  emit      = GetEmitter();
    regNumber targetReg = tree->GetRegNum();

    assert(targetReg != REG_NA);
    assert(tree->TypeGet() != TYP_VOID);
    assert(!op1->isContainedIntOrIImmed());
    assert(tree->OperIs(GT_LT, GT_LE, GT_EQ, GT_NE, GT_GT, GT_GE));

    if (varTypeIsFloating(op1Type))
    {
        bool      isUnordered = (tree->gtFlags & GTF_RELOP_NAN_UN) != 0;
        regNumber regOp1      = op1->GetRegNum();
        regNumber regOp2      = op2->GetRegNum();

        if (isUnordered)
        {
            BasicBlock* skipLabel = nullptr;
            if (tree->OperIs(GT_LT))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_fle_s : INS_fle_d, cmpSize, targetReg, regOp2, regOp1);
            }
            else if (tree->OperIs(GT_LE))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_flt_s : INS_flt_d, cmpSize, targetReg, regOp2, regOp1);
            }
            else if (tree->OperIs(GT_EQ))
            {
                regNumber tempReg = tree->GetSingleTempReg();
                skipLabel         = genCreateTempLabel();
                emit->emitIns_R_R(cmpSize == EA_4BYTE ? INS_fclass_s : INS_fclass_d, cmpSize, targetReg, regOp1);
                emit->emitIns_R_R(cmpSize == EA_4BYTE ? INS_fclass_s : INS_fclass_d, cmpSize, tempReg, regOp2);
                emit->emitIns_R_R_R(INS_or, EA_8BYTE, tempReg, targetReg, tempReg);
                emit->emitIns_R_R_I(INS_andi, EA_8BYTE, tempReg, tempReg, 0x300);
                emit->emitIns_R_R_I(INS_addi, EA_8BYTE, targetReg, REG_R0, 1);
                emit->emitIns_J(INS_bnez, skipLabel, tempReg);
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_feq_s : INS_feq_d, cmpSize, targetReg, regOp1, regOp2);
                genDefineTempLabel(skipLabel);
            }
            else if (tree->OperIs(GT_NE))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_feq_s : INS_feq_d, cmpSize, targetReg, regOp1, regOp2);
            }
            else if (tree->OperIs(GT_GT))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_fle_s : INS_fle_d, cmpSize, targetReg, regOp1, regOp2);
            }
            else if (tree->OperIs(GT_GE))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_flt_s : INS_flt_d, cmpSize, targetReg, regOp1, regOp2);
            }
            if (skipLabel == nullptr)
            {
                emit->emitIns_R_R_R(INS_sub, EA_8BYTE, targetReg, REG_R0, targetReg);
                emit->emitIns_R_R_I(INS_addi, EA_8BYTE, targetReg, targetReg, 1);
            }
        }
        else
        {
            if (tree->OperIs(GT_LT))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_flt_s : INS_flt_d, cmpSize, targetReg, regOp1, regOp2);
            }
            else if (tree->OperIs(GT_LE))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_fle_s : INS_fle_d, cmpSize, targetReg, regOp1, regOp2);
            }
            else if (tree->OperIs(GT_EQ))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_feq_s : INS_feq_d, cmpSize, targetReg, regOp1, regOp2);
            }
            else if (tree->OperIs(GT_NE))
            {
                regNumber tempReg = tree->GetSingleTempReg();
                emit->emitIns_R_R(cmpSize == EA_4BYTE ? INS_fclass_s : INS_fclass_d, cmpSize, targetReg, regOp1);
                emit->emitIns_R_R(cmpSize == EA_4BYTE ? INS_fclass_s : INS_fclass_d, cmpSize, tempReg, regOp2);
                emit->emitIns_R_R_R(INS_or, EA_8BYTE, tempReg, targetReg, tempReg);
                emit->emitIns_R_R_I(INS_andi, EA_8BYTE, tempReg, tempReg, 0x300);
                emit->emitIns_R_R_I(INS_addi, EA_8BYTE, targetReg, REG_R0, 0);
                BasicBlock* skipLabel = genCreateTempLabel();
                emit->emitIns_J(INS_bnez, skipLabel, tempReg);
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_feq_s : INS_feq_d, cmpSize, targetReg, regOp1, regOp2);
                emit->emitIns_R_R_R(INS_sub, EA_8BYTE, targetReg, REG_R0, targetReg);
                emit->emitIns_R_R_I(INS_addi, EA_8BYTE, targetReg, targetReg, 1);
                genDefineTempLabel(skipLabel);
            }
            else if (tree->OperIs(GT_GT))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_flt_s : INS_flt_d, cmpSize, targetReg, regOp2, regOp1);
            }
            else if (tree->OperIs(GT_GE))
            {
                emit->emitIns_R_R_R(cmpSize == EA_4BYTE ? INS_fle_s : INS_fle_d, cmpSize, targetReg, regOp2, regOp1);
            }
        }
    }
    else
    {
        bool      isUnsigned = (tree->gtFlags & GTF_UNSIGNED) != 0;
        regNumber regOp1     = op1->GetRegNum();

        if (op2->isContainedIntOrIImmed())
        {
            ssize_t imm = op2->AsIntCon()->gtIconVal;

            switch (cmpSize)
            {
                case EA_4BYTE:
                    if (isUnsigned)
                    {
                        imm = static_cast<uint32_t>(imm);

                        regNumber tmpRegOp1 = tree->GetSingleTempReg();
                        assert(regOp1 != tmpRegOp1);

                        emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpRegOp1, regOp1, 32);
                        emit->emitIns_R_R_I(INS_srli, EA_8BYTE, tmpRegOp1, tmpRegOp1, 32);
                        regOp1 = tmpRegOp1;
                    }
                    else
                    {
                        imm = static_cast<int32_t>(imm);
                    }
                    break;
                case EA_8BYTE:
                    break;
                default:
                    unreached();
            }

            if (tree->OperIs(GT_LT))
            {
                if (!isUnsigned && emitter::isValidSimm12(imm))
                {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else if (isUnsigned && emitter::isValidUimm11(imm))
                {
                    emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else
                {
                    emit->emitLoadImmediate(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                }
            }
            else if (tree->OperIs(GT_LE))
            {
                if (!isUnsigned && emitter::isValidSimm12(imm + 1))
                {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, targetReg, regOp1, imm + 1);
                }
                else if (isUnsigned && emitter::isValidUimm11(imm + 1))
                {
                    emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, regOp1, imm + 1);
                }
                else
                {
                    emit->emitLoadImmediate(EA_PTRSIZE, REG_RA, imm + 1);
                    emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                }
            }
            else if (tree->OperIs(GT_GT))
            {
                if (!isUnsigned && emitter::isValidSimm12(imm + 1))
                {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, targetReg, regOp1, imm + 1);
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, targetReg, 1);
                }
                else if (isUnsigned && emitter::isValidUimm11(imm + 1))
                {
                    emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, regOp1, imm + 1);
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, targetReg, 1);
                }
                else
                {
                    emit->emitLoadImmediate(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, REG_RA, regOp1);
                }
            }
            else if (tree->OperIs(GT_GE))
            {
                if (!isUnsigned && emitter::isValidSimm12(imm))
                {
                    emit->emitIns_R_R_I(INS_slti, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else if (isUnsigned && emitter::isValidUimm11(imm))
                {
                    emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, regOp1, imm);
                }
                else
                {
                    emit->emitLoadImmediate(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                }
                emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, targetReg, 1);
            }
            else if (tree->OperIs(GT_NE))
            {
                if (!imm)
                {
                    emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, targetReg, REG_R0, regOp1);
                }
                else if (emitter::isValidUimm12(imm))
                {
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, regOp1, imm);
                    emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, targetReg, REG_R0, targetReg);
                }
                else
                {
                    emit->emitLoadImmediate(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(INS_xor, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                    emit->emitIns_R_R_R(INS_sltu, EA_PTRSIZE, targetReg, REG_R0, targetReg);
                }
            }
            else if (tree->OperIs(GT_EQ))
            {
                if (!imm)
                {
                    emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, regOp1, 1);
                }
                else if (emitter::isValidUimm12(imm))
                {
                    emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, regOp1, imm);
                    emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, targetReg, 1);
                }
                else
                {
                    emit->emitLoadImmediate(EA_PTRSIZE, REG_RA, imm);
                    emit->emitIns_R_R_R(INS_xor, EA_PTRSIZE, targetReg, regOp1, REG_RA);
                    emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, targetReg, 1);
                }
            }
        }
        else
        {
            regNumber regOp2 = op2->GetRegNum();

            if (cmpSize == EA_4BYTE)
            {
                regNumber tmpRegOp1 = REG_RA;
                regNumber tmpRegOp2 = tree->GetSingleTempReg();
                assert(regOp1 != tmpRegOp2);
                assert(regOp2 != tmpRegOp2);

                if (isUnsigned)
                {
                    emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpRegOp1, regOp1, 32);
                    emit->emitIns_R_R_I(INS_srli, EA_8BYTE, tmpRegOp1, tmpRegOp1, 32);

                    emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpRegOp2, regOp2, 32);
                    emit->emitIns_R_R_I(INS_srli, EA_8BYTE, tmpRegOp2, tmpRegOp2, 32);
                }
                else
                {
                    emit->emitIns_R_R_I(INS_slliw, EA_8BYTE, tmpRegOp1, regOp1, 0);
                    emit->emitIns_R_R_I(INS_slliw, EA_8BYTE, tmpRegOp2, regOp2, 0);
                }

                regOp1 = tmpRegOp1;
                regOp2 = tmpRegOp2;
            }

            if (tree->OperIs(GT_LT))
            {
                emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp1, regOp2);
            }
            else if (tree->OperIs(GT_LE))
            {
                emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp2, regOp1);
                emit->emitIns_R_R_I(INS_xori, EA_PTRSIZE, targetReg, targetReg, 1);
            }
            else if (tree->OperIs(GT_GT))
            {
                emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp2, regOp1);
            }
            else if (tree->OperIs(GT_GE))
            {
                emit->emitIns_R_R_R(isUnsigned ? INS_sltu : INS_slt, EA_8BYTE, targetReg, regOp1, regOp2);
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
                emit->emitIns_R_R_I(INS_sltiu, EA_PTRSIZE, targetReg, targetReg, 1);
            }
        }
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForJumpCompare: Generates code for jmpCompare statement.
//
// A GT_JCMP node is created when a comparison and conditional branch
// can be executed in a single instruction.
//
// Arguments:
//    tree - The GT_JCMP tree node.
//
// Return Value:
//    None
//
void CodeGen::genCodeForJumpCompare(GenTreeOpCC* tree)
{
    assert(compiler->compCurBB->KindIs(BBJ_COND));

    assert(tree->OperIs(GT_JCMP));
    assert(!varTypeIsFloating(tree));
    assert(tree->TypeGet() == TYP_VOID);
    assert(tree->GetRegNum() == REG_NA);

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2();
    assert(!op1->isUsedFromMemory());
    assert(!op2->isUsedFromMemory());
    assert(!op1->isContainedIntOrIImmed());

    var_types op1Type = genActualType(op1->TypeGet());
    var_types op2Type = genActualType(op2->TypeGet());
    assert(genTypeSize(op1Type) == genTypeSize(op2Type));

    genConsumeOperands(tree);

    emitter*    emit = GetEmitter();
    instruction ins  = INS_invalid;
    int         regs = 0;

    GenCondition cond = tree->gtCondition;

    emitAttr  cmpSize = EA_ATTR(genTypeSize(op1Type));
    regNumber regOp1  = op1->GetRegNum();

    if (op2->isContainedIntOrIImmed())
    {
        ssize_t imm = op2->AsIntCon()->gtIconVal;
        if (imm)
        {
            assert(regOp1 != REG_R0);
            switch (cmpSize)
            {
                case EA_4BYTE:
                {
                    regNumber tmpRegOp1 = rsGetRsvdReg();
                    assert(regOp1 != tmpRegOp1);
                    if (cond.IsUnsigned())
                    {
                        imm = static_cast<uint32_t>(imm);

                        assert(regOp1 != tmpRegOp1);
                        emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpRegOp1, regOp1, 32);
                        emit->emitIns_R_R_I(INS_srli, EA_8BYTE, tmpRegOp1, tmpRegOp1, 32);
                    }
                    else
                    {
                        imm = static_cast<int32_t>(imm);
                        emit->emitIns_R_R_I(INS_addiw, EA_8BYTE, tmpRegOp1, regOp1, 0);
                    }
                    regOp1 = tmpRegOp1;
                    break;
                }
                case EA_8BYTE:
                    break;
                default:
                    unreached();
            }

            emit->emitLoadImmediate(EA_PTRSIZE, REG_RA, imm);
            regs = (int)REG_RA << 5;
        }
        else
        {
            if (cmpSize == EA_4BYTE)
            {
                regNumber tmpRegOp1 = rsGetRsvdReg();
                assert(regOp1 != tmpRegOp1);
                if (cond.IsUnsigned())
                {
                    emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpRegOp1, regOp1, 32);
                    emit->emitIns_R_R_I(INS_srli, EA_8BYTE, tmpRegOp1, tmpRegOp1, 32);
                }
                else
                {
                    emit->emitIns_R_R_I(INS_addiw, EA_8BYTE, tmpRegOp1, regOp1, 0);
                }
                regOp1 = tmpRegOp1;
            }
        }

        switch (cond.GetCode())
        {
            case GenCondition::EQ:
                regs |= ((int)regOp1);
                ins = INS_beq;
                break;
            case GenCondition::NE:
                regs |= ((int)regOp1);
                ins = INS_bne;
                break;
            case GenCondition::UGE:
            case GenCondition::SGE:
                regs |= ((int)regOp1);
                ins = cond.IsUnsigned() ? INS_bgeu : INS_bge;
                break;
            case GenCondition::UGT:
            case GenCondition::SGT:
                regs = imm ? ((((int)regOp1) << 5) | (int)REG_RA) : (((int)regOp1) << 5);
                ins  = cond.IsUnsigned() ? INS_bltu : INS_blt;
                break;
            case GenCondition::ULT:
            case GenCondition::SLT:
                regs |= ((int)regOp1);
                ins = cond.IsUnsigned() ? INS_bltu : INS_blt;
                break;
            case GenCondition::ULE:
            case GenCondition::SLE:
                regs = imm ? ((((int)regOp1) << 5) | (int)REG_RA) : (((int)regOp1) << 5);
                ins  = cond.IsUnsigned() ? INS_bgeu : INS_bge;
                break;
            default:
                NO_WAY("unexpected condition type");
                break;
        }
    }
    else
    {
        regNumber regOp2 = op2->GetRegNum();
        if (cmpSize == EA_4BYTE)
        {
            regNumber tmpRegOp1 = REG_RA;
            regNumber tmpRegOp2 = rsGetRsvdReg();
            assert(regOp1 != tmpRegOp2);
            assert(regOp2 != tmpRegOp2);

            if (cond.IsUnsigned())
            {
                emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpRegOp1, regOp1, 32);
                emit->emitIns_R_R_I(INS_srli, EA_8BYTE, tmpRegOp1, tmpRegOp1, 32);
                emit->emitIns_R_R_I(INS_slli, EA_8BYTE, tmpRegOp2, regOp2, 32);
                emit->emitIns_R_R_I(INS_srli, EA_8BYTE, tmpRegOp2, tmpRegOp2, 32);
            }
            else
            {
                emit->emitIns_R_R_I(INS_slliw, EA_8BYTE, tmpRegOp1, regOp1, 0);
                emit->emitIns_R_R_I(INS_slliw, EA_8BYTE, tmpRegOp2, regOp2, 0);
            }

            regOp1 = tmpRegOp1;
            regOp2 = tmpRegOp2;
        }

        switch (cond.GetCode())
        {
            case GenCondition::EQ:
                regs = (((int)regOp1) << 5) | (int)regOp2;
                ins  = INS_beq;
                break;
            case GenCondition::NE:
                regs = (((int)regOp1) << 5) | (int)regOp2;
                ins  = INS_bne;
                break;
            case GenCondition::UGE:
            case GenCondition::SGE:
                regs = ((int)regOp1 | ((int)regOp2 << 5));
                ins  = cond.IsUnsigned() ? INS_bgeu : INS_bge;
                break;
            case GenCondition::UGT:
            case GenCondition::SGT:
                regs = (((int)regOp1) << 5) | (int)regOp2;
                ins  = cond.IsUnsigned() ? INS_bltu : INS_blt;
                break;
            case GenCondition::ULT:
            case GenCondition::SLT:
                regs = ((int)regOp1 | ((int)regOp2 << 5));
                ins  = cond.IsUnsigned() ? INS_bltu : INS_blt;
                break;
            case GenCondition::ULE:
            case GenCondition::SLE:
                regs = (((int)regOp1) << 5) | (int)regOp2;
                ins  = cond.IsUnsigned() ? INS_bgeu : INS_bge;
                break;
            default:
                NO_WAY("unexpected condition type-regs");
                break;
        }
    }
    assert(ins != INS_invalid);
    assert(regs != 0);

    emit->emitIns_J(ins, compiler->compCurBB->GetTrueTarget(), regs); // 5-bits;

    // If we cannot fall into the false target, emit a jump to it
    BasicBlock* falseTarget = compiler->compCurBB->GetFalseTarget();
    if (!compiler->compCurBB->CanRemoveJumpToTarget(falseTarget, compiler))
    {
        inst_JMP(EJ_jmp, falseTarget);
    }
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return offset from the stack pointer (Initial-SP) to the frame pointer. The frame pointer
// will point to the saved frame pointer slot (i.e., there will be frame pointer chaining).
//
int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    assert(compiler->compCalleeRegsPushed >= 2);

    int delta = compiler->lvaOutgoingArgSpaceSize + (compiler->compCalleeRegsPushed << 3) - 8;

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

    int totalFrameSize = compiler->compCalleeRegsPushed * REGSIZE_BYTES + compiler->compLclFrameSize;

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
    int callerSPtoSPdelta = -genTotalFrameSize();

    assert(callerSPtoSPdelta <= 0);
    return callerSPtoSPdelta;
}

// Produce generic and unoptimized code for loading constant to register and dereferencing it
// at the end
static void emitLoadConstAtAddr(emitter* emit, regNumber dstRegister, ssize_t imm)
{
    ssize_t high = (imm >> 32) & 0xffffffff;
    emit->emitIns_R_I(INS_lui, EA_PTRSIZE, dstRegister, (((high + 0x800) >> 12) & 0xfffff));
    emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, dstRegister, dstRegister, (high & 0xfff));

    ssize_t low = imm & 0xffffffff;
    emit->emitIns_R_R_I(INS_slli, EA_PTRSIZE, dstRegister, dstRegister, 11);
    emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, dstRegister, dstRegister, ((low >> 21) & 0x7ff));

    emit->emitIns_R_R_I(INS_slli, EA_PTRSIZE, dstRegister, dstRegister, 11);
    emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, dstRegister, dstRegister, ((low >> 10) & 0x7ff));
    emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, dstRegister, dstRegister, (low & 0x3ff));
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
        // lui reg, pAddr     #NOTE: this maybe multi-instructions.
        // ld reg, reg
        // jalr reg

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

        if (compiler->opts.compReloc)
        {
            // TODO-RISCV64: here the jal is special flag rather than a real instruction.
            GetEmitter()->emitIns_R_AI(INS_jal, EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
        }
        else
        {
            emitLoadConstAtAddr(GetEmitter(), callTarget, (ssize_t)pAddr);
        }
        regSet.verifyRegUsed(callTarget);

        callType = emitter::EC_INDIR_R;
    }

    GetEmitter()->emitIns_Call(callType, compiler->eeFindHelper(helper), INDEBUG_LDISASM_COMMA(nullptr) addr, argSize,
                               retSize, EA_UNKNOWN, gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur, DebugInfo(), /* IL offset */
                               callTarget,                           /* ireg */
                               REG_NA, 0, 0,                         /* xreg, xmul, disp */
                               false                                 /* isJump */
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
    NYI_RISCV64("genSIMDIntrinsic-----unimplemented/unused on RISCV64 yet----");
}

insOpts CodeGen::genGetSimdInsOpt(emitAttr size, var_types elementType)
{
    NYI_RISCV64("genGetSimdInsOpt-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("getOpForSIMDIntrinsic-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicInit-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicInitN-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicUnOp-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicWiden-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicNarrow-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicBinOp-----unimplemented/unused on RISCV64 yet----");
}

//--------------------------------------------------------------------------------
// genSIMDIntrinsicRelOp: Generate code for a SIMD Intrinsic relational operator
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
    NYI_RISCV64("genSIMDIntrinsicRelOp-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicDotProduct-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicGetItem-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicSetItem-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicUpperSave-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genSIMDIntrinsicUpperRestore-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genStoreIndTypeSIMD12-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genLoadIndTypeSIMD12-----unimplemented/unused on RISCV64 yet----");
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
    NYI_RISCV64("genStoreLclTypeSIMD12-----unimplemented/unused on RISCV64 yet----");
}

#endif // FEATURE_SIMD

void CodeGen::genStackPointerConstantAdjustment(ssize_t spDelta, regNumber regTmp)
{
    assert(spDelta < 0);

    // We assert that the SP change is less than one page. If it's greater, you should have called a
    // function that does a probe, which will in turn call this function.
    assert((target_size_t)(-spDelta) <= compiler->eeGetPageSize());

    if (emitter::isValidSimm12(spDelta))
    {
        GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, spDelta);
    }
    else
    {
        GetEmitter()->emitLoadImmediate(EA_PTRSIZE, regTmp, spDelta);
        GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, regTmp);
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
    GetEmitter()->emitIns_R_R_I(INS_lw, EA_4BYTE, regTmp, REG_SP, 0);
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

        GetEmitter()->emitIns_R_R_I(INS_lw, EA_4BYTE, regTmp, REG_SP, 0);
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
            {
                treeNode->gtOper = GT_CNS_DBL;
            }
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
        case GT_AND_NOT:
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
        case GT_ROL:
            genCodeForShift(treeNode);
            break;

        case GT_CAST:
            genCodeForCast(treeNode->AsOp());
            break;

        case GT_BITCAST:
            genCodeForBitCast(treeNode->AsOp());
            break;

        case GT_LCL_ADDR:
            genCodeForLclAddr(treeNode->AsLclFld());
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
            genIntrinsic(treeNode->AsIntrinsic());
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
            genConsumeOperands(treeNode->AsOp());
            genCodeForCompare(treeNode->AsOp());
            break;

        case GT_JCMP:
            genCodeForJumpCompare(treeNode->AsOpCC());
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

        case GT_PUTARG_SPLIT:
            genPutArgSplit(treeNode->AsPutArgSplit());
            break;

        case GT_CALL:
            genCall(treeNode->AsCall());
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
        case GT_XORR:
        case GT_XAND:
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

        case GT_BOUNDS_CHECK:
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
            emit->emitIns_R_L(INS_ld, EA_PTRSIZE, genPendingCallLabel, targetReg);
            break;

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
            NYI_RISCV64("some node type in genCodeForTreeNode-----unimplemented/unused on RISCV64 yet----");
#endif
        }
        break;
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
    assert(compiler->compGeneratingProlog);

    if (!compiler->getNeedsGSSecurityCookie())
    {
        return;
    }

    if (compiler->opts.IsOSR() && compiler->info.compPatchpointInfo->HasSecurityCookie())
    {
        // Security cookie is on original frame and was initialized there.
        return;
    }

    emitter* emit = GetEmitter();

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
        noway_assert(compiler->gsGlobalSecurityCookieVal != 0);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, compiler->gsGlobalSecurityCookieVal);

        emit->emitIns_S_R(INS_sd, EA_PTRSIZE, initReg, compiler->lvaGSSecurityCookie, 0);
    }
    else
    {
        if (compiler->opts.compReloc)
        {
            emit->emitIns_R_AI(INS_jal, EA_PTR_DSP_RELOC, initReg, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        }
        else
        {
            emit->emitLoadImmediate(EA_PTRSIZE, initReg, ((size_t)compiler->gsGlobalSecurityCookieAddr));
            emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, initReg, initReg, 0);
        }
        regSet.verifyRegUsed(initReg);
        emit->emitIns_S_R(INS_sd, EA_PTRSIZE, initReg, compiler->lvaGSSecurityCookie, 0);
    }

    *pInitRegZeroed = false;
}

//------------------------------------------------------------------------
// genEmitGSCookieCheck: Generate code to check that the GS cookie
// wasn't thrashed by a buffer overrun.
//
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    noway_assert(compiler->gsGlobalSecurityCookieAddr || compiler->gsGlobalSecurityCookieVal);

    // Make sure that the return register is reported as live GC-ref so that any GC that kicks in while
    // executing GS cookie check will not collect the object pointed to by REG_INTRET (A0).
    if (!pushReg && (compiler->info.compRetNativeType == TYP_REF))
    {
        gcInfo.gcRegGCrefSetCur |= RBM_INTRET;
    }

    // We need two temporary registers, to load the GS cookie values and compare them. We can't use
    // any argument registers if 'pushReg' is true (meaning we have a JMP call). They should be
    // callee-trash registers, which should not contain anything interesting at this point.
    // We don't have any IR node representing this check, so LSRA can't communicate registers
    // for us to use.

    regNumber regGSConst = REG_GSCOOKIE_TMP_0;
    regNumber regGSValue = REG_GSCOOKIE_TMP_1;

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
        // load the GS cookie constant into a reg
        //
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, regGSConst, compiler->gsGlobalSecurityCookieVal);
    }
    else
    {
        //// Ngen case - GS cookie constant needs to be accessed through an indirection.
        // instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, regGSConst, (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        // GetEmitter()->emitIns_R_R_I(INS_ld_d, EA_PTRSIZE, regGSConst, regGSConst, 0);
        if (compiler->opts.compReloc)
        {
            GetEmitter()->emitIns_R_AI(INS_jal, EA_PTR_DSP_RELOC, regGSConst,
                                       (ssize_t)compiler->gsGlobalSecurityCookieAddr);
        }
        else
        {
            // TODO-RISCV64: maybe optimize furtherk!
            UINT32 high = ((ssize_t)compiler->gsGlobalSecurityCookieAddr) >> 32;
            if (((high + 0x800) >> 12) != 0)
            {
                GetEmitter()->emitIns_R_I(INS_lui, EA_PTRSIZE, regGSConst, (((high + 0x800) >> 12) & 0xfffff));
            }
            if ((high & 0xFFF) != 0)
            {
                GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, regGSConst, REG_R0, (high & 0xfff));
            }
            UINT32 low = ((ssize_t)compiler->gsGlobalSecurityCookieAddr) & 0xffffffff;
            GetEmitter()->emitIns_R_R_I(INS_slli, EA_PTRSIZE, regGSConst, regGSConst, 11);
            GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, regGSConst, regGSConst, (low >> 21) & 0x7FF);
            GetEmitter()->emitIns_R_R_I(INS_slli, EA_PTRSIZE, regGSConst, regGSConst, 11);
            GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, regGSConst, regGSConst, (low >> 10) & 0x7FF);
            GetEmitter()->emitIns_R_R_I(INS_slli, EA_PTRSIZE, regGSConst, regGSConst, 10);
            GetEmitter()->emitIns_R_R_I(INS_ld, EA_PTRSIZE, regGSConst, regGSConst, low & 0x3FF);
        }
        regSet.verifyRegUsed(regGSConst);
    }
    // Load this method's GS value from the stack frame
    GetEmitter()->emitIns_R_S(INS_ld, EA_PTRSIZE, regGSValue, compiler->lvaGSSecurityCookie, 0);

    // Compare with the GC cookie constant
    BasicBlock* gsCheckBlk = genCreateTempLabel();
    GetEmitter()->emitIns_J_cond_la(INS_beq, gsCheckBlk, regGSConst, regGSValue);

    // regGSConst and regGSValue aren't needed anymore, we can use them for helper call
    genEmitHelperCall(CORINFO_HELP_FAIL_FAST, 0, EA_UNKNOWN, regGSConst);
    genDefineTempLabel(gsCheckBlk);
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
    NYI_RISCV64("genIntrinsic-----unimplemented/unused on RISCV64 yet----");
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
            NYI_RISCV64("SIMD in genPutArgStk-----unimplemented/unused on RISCV64 yet----");
        }

        var_types   slotType  = genActualType(source);
        instruction storeIns  = ins_Store(slotType);
        emitAttr    storeAttr = emitTypeSize(slotType);

        // When passed in registers or on the stack, integer scalars narrower than XLEN bits
        // are widened according to the sign of their type up to 32 bits, then sign-extended to XLEN bits.
        if (EA_SIZE(storeAttr) < EA_PTRSIZE && varTypeUsesIntReg(slotType))
        {
            storeAttr = EA_PTRSIZE;
            storeIns  = INS_sd;
        }

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
            emit->emitIns_S_R(storeIns, storeAttr, source->GetRegNum(), varNumOut, argOffsetOut);
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

            // Setup loReg from the internal registers that we reserved in lower.
            //
            regNumber loReg = treeNode->ExtractTempReg();

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

            noway_assert(srcSize <= MAX_PASS_MULTIREG_BYTES);

            unsigned dstSize = treeNode->GetStackByteSize();

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

            while (remainingSize > 0)
            {
                nextIndex = structOffset / TARGET_POINTER_SIZE;

                var_types type;
                if (remainingSize >= TARGET_POINTER_SIZE)
                {
                    type = layout->GetGCPtrType(nextIndex);
                }
                else // (remainingSize < TARGET_POINTER_SIZE)
                {
                    // the left over size is smaller than a pointer and thus can never be a GC type
                    assert(!layout->IsGCPtr(nextIndex));

                    if (remainingSize >= 4)
                    {
                        type = TYP_INT;
                    }
                    else if (remainingSize >= 2)
                    {
                        type = TYP_USHORT;
                    }
                    else
                    {
                        assert(remainingSize == 1);
                        type = TYP_UBYTE;
                    }
                }

                const emitAttr attr     = emitActualTypeSize(type);
                const unsigned moveSize = genTypeSize(type);

                remainingSize -= moveSize;

                instruction loadIns = ins_Load(type);
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
                instruction storeIns = ins_Store(type);
                emit->emitIns_S_R(storeIns, attr, loReg, varNumOut, argOffsetOut);
                argOffsetOut += moveSize;
                assert(argOffsetOut <= argOffsetMax); // We can't write beyond the outgoing arg area

                structOffset += moveSize;
            }
        }
    }
}

//---------------------------------------------------------------------
// genPutArgReg - generate code for a T_PUTARG_REG node
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

    if (varTypeIsFloating(tree) && emitter::isGeneralRegister(targetReg))
    {
        // Pass the float args by integer register
        targetType = emitActualTypeSize(targetType) == EA_4BYTE ? TYP_INT : TYP_LONG;
    }

    // If child node is not already in the register we need, move it
    GetEmitter()->emitIns_Mov(ins_Copy(op1->GetRegNum(), targetType), emitActualTypeSize(targetType), targetReg,
                              op1->GetRegNum(), true);
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
                inst_Mov(type, argReg, fieldReg, /* canSkip */ true);

                regIndex++;
            }
        }
    }
    else
    {
        var_types targetType = source->TypeGet();
        assert(source->isContained() && varTypeIsStruct(targetType));

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
            layout            = source->AsLclVarCommon()->GetLayout(compiler);
            LclVarDsc* varDsc = compiler->lvaGetDesc(srcLclNum);

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
                allocatedValueReg = treeNode->ExtractTempReg();
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
            // example, if the registers to place are r0, r1, r2=addrReg, r3
            // then we will set firstRegToPlace = 3 (r3) and the loop below
            // will place r3, r0, r1, r2. The last placement will clobber
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
        unsigned structOffset  = treeNode->gtNumRegs * TARGET_POINTER_SIZE;
        unsigned remainingSize = layout->GetSize() - structOffset;
        unsigned argOffsetOut  = treeNode->getArgOffset();

        assert((remainingSize > 0) && (roundUp(remainingSize, TARGET_POINTER_SIZE) == treeNode->GetStackByteSize()));
        while (remainingSize > 0)
        {
            var_types type;
            if (remainingSize >= TARGET_POINTER_SIZE)
            {
                type = layout->GetGCPtrType(structOffset / TARGET_POINTER_SIZE);
            }
            else if (remainingSize >= 4)
            {
                type = TYP_INT;
            }
            else if (remainingSize >= 2)
            {
                type = TYP_USHORT;
            }
            else
            {
                assert(remainingSize == 1);
                type = TYP_UBYTE;
            }

            emitAttr attr     = emitActualTypeSize(type);
            unsigned moveSize = genTypeSize(type);

            instruction loadIns = ins_Load(type);
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
            emit->emitIns_S_R(ins_Store(type), attr, valueReg, varNumOut, argOffsetOut);
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
                emit->emitIns_R_S(ins_Load(type), emitTypeSize(type), targetReg, srcLclNum,
                                  srcLclOffset + structOffset);
            }
            else
            {
                assert((addrReg != targetReg) || (regsPlaced == treeNode->gtNumRegs - 1));

                // Load from our address expression source
                emit->emitIns_R_R_I(ins_Load(type), emitTypeSize(type), targetReg, addrReg, structOffset);
            }

            curRegIndex++;
            structOffset += TARGET_POINTER_SIZE;
        }
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genRangeCheck: generate code for GT_BOUNDS_CHECK node.
//
void CodeGen::genRangeCheck(GenTree* oper)
{
    noway_assert(oper->OperIs(GT_BOUNDS_CHECK));
    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTree*  index     = bndsChk->GetIndex();
    GenTree*  length    = bndsChk->GetArrayLength();
    regNumber indexReg  = index->GetRegNum();
    regNumber lengthReg = length->GetRegNum();

    genConsumeRegs(index);
    genConsumeRegs(length);

    if (genActualType(length) == TYP_INT)
    {
        regNumber tempReg = oper->ExtractTempReg();
        GetEmitter()->emitIns_R_R_I(INS_addiw, EA_4BYTE, tempReg, lengthReg, 0); // sign-extend
        lengthReg = tempReg;
    }
    if (genActualType(index) == TYP_INT)
    {
        regNumber tempReg = oper->GetSingleTempReg();
        GetEmitter()->emitIns_R_R_I(INS_addiw, EA_4BYTE, tempReg, indexReg, 0); // sign-extend
        indexReg = tempReg;
    }

#ifdef DEBUG
    var_types lengthType = genActualType(length);
    var_types indexType  = genActualType(index);
    // Bounds checks can only be 32 or 64 bit sized comparisons.
    assert(lengthType == TYP_INT || lengthType == TYP_LONG);
    assert(indexType == TYP_INT || indexType == TYP_LONG);
#endif // DEBUG

    genJumpToThrowHlpBlk_la(bndsChk->gtThrowKind, INS_bgeu, indexReg, bndsChk->gtIndRngFailBB, lengthReg);
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
        GetEmitter()->emitIns_Mov(ins_Copy(targetType), emitActualTypeSize(targetType), targetReg, tree->gtSrcReg,
                                  false);
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

    genConsumeRegs(tree->gtOp1);

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
    emitAttr size = emitActualTypeSize(tree);

    assert(tree->GetRegNum() != REG_NA);

    genConsumeOperands(tree->AsOp());

    GenTree* operand = tree->gtGetOp1();
    GenTree* shiftBy = tree->gtGetOp2();

    if (tree->OperIs(GT_ROR, GT_ROL))
    {
        regNumber tempReg  = tree->GetSingleTempReg();
        unsigned  immWidth = emitter::getBitWidth(size); // For RISCV64, immWidth will be set to 32 or 64
        if (!shiftBy->IsCnsIntOrI())
        {
            regNumber shiftRight = tree->OperIs(GT_ROR) ? shiftBy->GetRegNum() : tempReg;
            regNumber shiftLeft  = tree->OperIs(GT_ROR) ? tempReg : shiftBy->GetRegNum();
            GetEmitter()->emitIns_R_R_I(INS_addi, size, tempReg, REG_R0, immWidth);
            GetEmitter()->emitIns_R_R_R(INS_sub, size, tempReg, tempReg, shiftBy->GetRegNum());
            if (size == EA_8BYTE)
            {
                GetEmitter()->emitIns_R_R_R(INS_srl, size, REG_RA, operand->GetRegNum(), shiftRight);
                GetEmitter()->emitIns_R_R_R(INS_sll, size, tempReg, operand->GetRegNum(), shiftLeft);
            }
            else
            {
                GetEmitter()->emitIns_R_R_R(INS_srlw, size, REG_RA, operand->GetRegNum(), shiftRight);
                GetEmitter()->emitIns_R_R_R(INS_sllw, size, tempReg, operand->GetRegNum(), shiftLeft);
            }
        }
        else
        {
            unsigned shiftByImm = (unsigned)shiftBy->AsIntCon()->gtIconVal;
            if (shiftByImm >= 32 && shiftByImm < 64)
            {
                immWidth = 64;
            }
            unsigned shiftRight = tree->OperIs(GT_ROR) ? shiftByImm : immWidth - shiftByImm;
            unsigned shiftLeft  = tree->OperIs(GT_ROR) ? immWidth - shiftByImm : shiftByImm;
            if ((shiftByImm >= 32 && shiftByImm < 64) || size == EA_8BYTE)
            {
                GetEmitter()->emitIns_R_R_I(INS_srli, size, REG_RA, operand->GetRegNum(), shiftRight);
                GetEmitter()->emitIns_R_R_I(INS_slli, size, tempReg, operand->GetRegNum(), shiftLeft);
            }
            else
            {
                GetEmitter()->emitIns_R_R_I(INS_srliw, size, REG_RA, operand->GetRegNum(), shiftRight);
                GetEmitter()->emitIns_R_R_I(INS_slliw, size, tempReg, operand->GetRegNum(), shiftLeft);
            }
        }
        GetEmitter()->emitIns_R_R_R(INS_or, size, tree->GetRegNum(), REG_RA, tempReg);
    }
    else
    {
        if (!shiftBy->IsCnsIntOrI())
        {
            instruction ins = genGetInsForOper(tree);
            GetEmitter()->emitIns_R_R_R(ins, size, tree->GetRegNum(), operand->GetRegNum(), shiftBy->GetRegNum());
        }
        else
        {
            instruction ins        = genGetInsForOper(tree);
            unsigned    shiftByImm = (unsigned)shiftBy->AsIntCon()->gtIconVal;

            // should check shiftByImm for riscv64-ins.
            unsigned immWidth = emitter::getBitWidth(size); // For RISCV64, immWidth will be set to 32 or 64
            shiftByImm &= (immWidth - 1);

            if (ins == INS_slliw && shiftByImm >= 32)
            {
                ins = INS_slli;
            }
            else if (ins == INS_slli && shiftByImm >= 32 && shiftByImm < 64)
            {
                ins = INS_slli;
            }
            else if (ins == INS_srai && shiftByImm >= 32 && shiftByImm < 64)
            {
                ins = INS_srai;
            }
            else if (ins == INS_srli && shiftByImm >= 32 && shiftByImm < 64)
            {
                ins = INS_srli;
            }
            GetEmitter()->emitIns_R_R_I(ins, size, tree->GetRegNum(), operand->GetRegNum(), shiftByImm);
        }
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForLclAddr: Generates the code for GT_LCL_ADDR
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

    GetEmitter()->emitIns_R_S(INS_lea, size, targetReg, lclAddrNode->GetLclNum(), lclAddrNode->GetLclOffs());

    genProduceReg(lclAddrNode);
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
// genScaledAdd: A helper for `dest = base + (index << scale)`
//               and maybe optimize the instruction(s) for this operation.
//
void CodeGen::genScaledAdd(
    emitAttr attr, regNumber targetReg, regNumber baseReg, regNumber indexReg, int scale, regNumber scaleTempReg)
{
    assert((scale >> 5) == 0);
    emitter* emit = GetEmitter();
    if (scale == 0)
    {
        instruction ins = attr == EA_4BYTE ? INS_addw : INS_add;
        // target = base + index
        emit->emitIns_R_R_R(ins, attr, targetReg, baseReg, indexReg);
    }
    else
    {
        instruction ins;
        instruction ins2;
        if (attr == EA_4BYTE)
        {
            ins  = INS_slliw;
            ins2 = INS_addw;
        }
        else
        {
            ins  = INS_slli;
            ins2 = INS_add;
        }
        assert(scaleTempReg != REG_NA);
        // target = base + index << scale
        emit->emitIns_R_R_I(ins, attr, scaleTempReg, indexReg, scale);
        emit->emitIns_R_R_R(ins2, attr, targetReg, baseReg, scaleTempReg);
    }
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

    regNumber tempReg = node->GetSingleTempReg();

    // Generate the bounds check if necessary.
    if (node->IsBoundsChecked())
    {
        GetEmitter()->emitIns_R_R_I(INS_lw, EA_4BYTE, tempReg, base->GetRegNum(), node->gtLenOffset);
        // if (index >= tempReg)
        // {
        //   JumpToThrowHlpBlk;
        // }
        //
        // sltu  tempReg, index, tempReg
        // bne  tempReg, zero, RngChkExit
        // IndRngFail:
        // ...
        // RngChkExit:
        genJumpToThrowHlpBlk_la(SCK_RNGCHK_FAIL, INS_bgeu, index->GetRegNum(), node->gtIndRngFailBB, tempReg);
    }

    emitAttr attr = emitActualTypeSize(node);
    // Can we use a shift instruction for multiply ?
    //
    if (isPow2(node->gtElemSize))
    {
        DWORD scale;
        BitScanForward(&scale, node->gtElemSize);

        // dest = base + (index << scale)
        if (node->gtElemSize <= 64)
        {
            genScaledAdd(attr, node->GetRegNum(), base->GetRegNum(), index->GetRegNum(), scale, tempReg);
        }
        else
        {
            GetEmitter()->emitLoadImmediate(EA_PTRSIZE, tempReg, scale);

            instruction ins;
            instruction ins2;
            if (attr == EA_4BYTE)
            {
                ins  = INS_sllw;
                ins2 = INS_addw;
            }
            else
            {
                ins  = INS_sll;
                ins2 = INS_add;
            }
            GetEmitter()->emitIns_R_R_R(ins, attr, tempReg, index->GetRegNum(), tempReg);
            GetEmitter()->emitIns_R_R_R(ins2, attr, node->GetRegNum(), tempReg, base->GetRegNum());
        }
    }
    else // we have to load the element size and use a MADD (multiply-add) instruction
    {
        // tempReg = element size
        instGen_Set_Reg_To_Imm(EA_4BYTE, tempReg, (ssize_t)node->gtElemSize);

        // dest = index * tempReg + base
        instruction ins;
        instruction ins2;
        if (attr == EA_4BYTE)
        {
            ins  = INS_mulw;
            ins2 = INS_addw;
        }
        else
        {
            ins  = INS_mul;
            ins2 = INS_add;
        }
        GetEmitter()->emitIns_R_R_R(ins, EA_PTRSIZE, tempReg, index->GetRegNum(), tempReg);
        GetEmitter()->emitIns_R_R_R(ins2, attr, node->GetRegNum(), tempReg, base->GetRegNum());
    }

    // dest = dest + elemOffs
    GetEmitter()->emitIns_R_R_I(INS_addi, attr, node->GetRegNum(), node->GetRegNum(), node->gtElemOffset);

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
    regNumber   tmpReg    = targetReg;
    emitAttr    attr      = emitActualTypeSize(type);
    int         offset    = 0;

    genConsumeAddress(tree->Addr());

    if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
    {
        instGen_MemoryBarrier(BARRIER_FULL);
    }

    GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), targetReg, tree);

    genProduceReg(tree);
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
        assert(dstAddr->OperIs(GT_LCL_ADDR));
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
            assert(srcAddr->OperIs(GT_LCL_ADDR));
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
        regNumber tempReg2 = REG_RA;

        for (unsigned regSize = 2 * REGSIZE_BYTES; size >= regSize;
             size -= regSize, srcOffset += regSize, dstOffset += regSize)
        {
            if (srcLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_R_S(INS_ld, EA_8BYTE, tempReg, srcLclNum, srcOffset);
                emit->emitIns_R_S(INS_ld, EA_8BYTE, tempReg2, srcLclNum, srcOffset + 8);
            }
            else
            {
                emit->emitIns_R_R_I(INS_ld, EA_8BYTE, tempReg, srcAddrBaseReg, srcOffset);
                emit->emitIns_R_R_I(INS_ld, EA_8BYTE, tempReg2, srcAddrBaseReg, srcOffset + 8);
            }

            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_R(INS_sd, EA_8BYTE, tempReg, dstLclNum, dstOffset);
                emit->emitIns_S_R(INS_sd, EA_8BYTE, tempReg2, dstLclNum, dstOffset + 8);
            }
            else
            {
                emit->emitIns_R_R_I(INS_sd, EA_8BYTE, tempReg, dstAddrBaseReg, dstOffset);
                emit->emitIns_R_R_I(INS_sd, EA_8BYTE, tempReg2, dstAddrBaseReg, dstOffset + 8);
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
                loadIns  = INS_lb;
                storeIns = INS_sb;
                attr     = EA_4BYTE;
                break;
            case 2:
                loadIns  = INS_lh;
                storeIns = INS_sh;
                attr     = EA_4BYTE;
                break;
            case 4:
                loadIns  = INS_lw;
                storeIns = INS_sw;
                attr     = EA_ATTR(regSize);
                break;
            case 8:
                loadIns  = INS_ld;
                storeIns = INS_sd;
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
// genCodeForInitBlkLoop - Generate code for an InitBlk using an inlined for-loop.
//    It's needed for cases when size is too big to unroll and we're not allowed
//    to use memset call due to atomicity requirements.
//
// Arguments:
//    initBlkNode - the GT_STORE_BLK node
//
void CodeGen::genCodeForInitBlkLoop(GenTreeBlk* initBlkNode)
{
    GenTree* const dstNode = initBlkNode->Addr();
    genConsumeReg(dstNode);
    const regNumber dstReg = dstNode->GetRegNum();

    if (initBlkNode->IsVolatile())
    {
        // issue a full memory barrier before a volatile initBlock Operation
        instGen_MemoryBarrier();
    }

    const unsigned size = initBlkNode->GetLayout()->GetSize();
    assert((size >= TARGET_POINTER_SIZE) && ((size % TARGET_POINTER_SIZE) == 0));

    // The loop is reversed - it makes it smaller.
    // Although, we zero the first pointer before the loop (the loop doesn't zero it)
    // it works as a nullcheck, otherwise the first iteration would try to access
    // "null + potentially large offset" and hit AV.
    GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, dstReg, 0);
    if (size > TARGET_POINTER_SIZE)
    {
        // Extend liveness of dstReg in case if it gets killed by the store.
        gcInfo.gcMarkRegPtrVal(dstReg, dstNode->TypeGet());

        const regNumber tempReg = initBlkNode->GetSingleTempReg();
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, tempReg, size - TARGET_POINTER_SIZE);

        // tempReg = dstReg + tempReg (a new interior pointer, but in a nongc region)
        GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, tempReg, dstReg, tempReg);

        BasicBlock* loop = genCreateTempLabel();
        genDefineTempLabel(loop);
        GetEmitter()->emitDisableGC(); // TODO: add gcinfo to tempReg and remove nogc

        // *tempReg = 0
        GetEmitter()->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_R0, tempReg, 0);
        // tempReg = tempReg - 8
        GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, tempReg, tempReg, -8);
        // if (tempReg != dstReg) goto loop;
        GetEmitter()->emitIns_J(INS_bne, loop, (int)tempReg | ((int)dstReg << 5));
        GetEmitter()->emitEnableGC();

        gcInfo.gcMarkRegSetNpt(genRegMask(dstReg));
    }
}

//------------------------------------------------------------------------
// genCall: Produce code for a GT_CALL node
//
void CodeGen::genCall(GenTreeCall* call)
{
    // Consume all the arg regs
    for (CallArg& arg : call->gtArgs.LateArgs())
    {
        CallArgABIInformation& abiInfo = arg.AbiInfo;
        GenTree*               argNode = arg.GetLateNode();

        // GT_RELOAD/GT_COPY use the child node
        argNode = argNode->gtSkipReloadOrCopy();

        if (abiInfo.GetRegNum() == REG_STK)
        {
            continue;
        }

        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
                GenTree* putArgRegNode = use.GetNode();
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                genConsumeReg(putArgRegNode);
            }
        }
        else if (abiInfo.IsSplit())
        {
            assert(compFeatureArgSplit());

            GenTreePutArgSplit* splitNode = argNode->AsPutArgSplit();
            genConsumeArgSplitStruct(splitNode);

            regNumber argReg   = abiInfo.GetRegNum();
            regNumber allocReg = splitNode->GetRegNumByIdx(0);
            var_types regType  = splitNode->GetRegType(0);

            // For RISCV64's ABI, the split is only using the A7 and stack for passing arg.
            assert(argReg == REG_A7);
            assert(emitter::isGeneralRegister(allocReg));
            assert(abiInfo.NumRegs == 1);

            inst_Mov(regType, argReg, allocReg, /* canSkip */ true);
        }
        else
        {
            regNumber argReg = abiInfo.GetRegNum();
            genConsumeReg(argNode);
            var_types dstType = emitter::isFloatReg(argReg) ? TYP_DOUBLE : argNode->TypeGet();
            inst_Mov(dstType, argReg, argNode->GetRegNum(), /* canSkip */ true);
        }
    }

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);

        GetEmitter()->emitIns_R_R_I(INS_lw, EA_4BYTE, REG_R0, regThis, 0);
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

            regNumber tmpReg = call->GetSingleTempReg();
            // Register where we save call address in should not be overridden by epilog.
            assert((genRegMask(tmpReg) & (RBM_INT_CALLEE_TRASH & ~RBM_RA)) == genRegMask(tmpReg));

            regNumber callAddrReg =
                call->IsVirtualStubRelativeIndir() ? compiler->virtualStubParamInfo->GetReg() : REG_R2R_INDIRECT_PARAM;
            GetEmitter()->emitIns_R_R_I(ins_Load(TYP_I_IMPL), emitActualTypeSize(TYP_I_IMPL), tmpReg, callAddrReg, 0);
            // We will use this again when emitting the jump in genCallInstruction in the epilog
            call->gtRsvdRegs |= genRegMask(tmpReg);
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
#endif

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
                returnReg              = pRetTypeDesc->GetABIReturnReg(i);
                regNumber allocatedReg = call->GetRegNumByIdx(i);
                inst_Mov(regType, allocatedReg, returnReg, /* canSkip */ true);
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
                inst_Mov(returnType, call->GetRegNum(), returnReg, /* canSkip */ false);
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
    if (call->gtCallType != CT_HELPER)
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
            for (unsigned j = 0; j < arg.AbiInfo.NumRegs; j++)
            {
                regNumber reg = arg.AbiInfo.GetRegNum(j);
                if ((trashedByEpilog & genRegMask(reg)) != 0)
                {
                    JITDUMP("Tail call node:\n");
                    DISPTREE(call);
                    JITDUMP("Register used: %s\n", getRegName(reg));
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
            regNumber targetAddrReg = call->GetSingleTempReg();
            // For fast tailcalls we have already loaded the call target when processing the call node.
            if (!call->IsFastTailCall())
            {
                GetEmitter()->emitIns_R_R_I(ins_Load(TYP_I_IMPL), emitActualTypeSize(TYP_I_IMPL), targetAddrReg,
                                            callThroughIndirReg, 0);
            }
            else
            {
                // Register where we save call address in should not be overridden by epilog.
                assert((genRegMask(targetAddrReg) & (RBM_INT_CALLEE_TRASH & ~RBM_RA)) == genRegMask(targetAddrReg));
            }

            // We have now generated code loading the target address from the indirection cell into `targetAddrReg`.
            // We just need to emit "bl targetAddrReg" in this case.
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
            assert(call->gtCallType == CT_HELPER || call->gtCallType == CT_USER_FUNC);

            void* addr = nullptr;
#ifdef FEATURE_READYTORUN
            if (call->gtEntryPoint.addr != NULL)
            {
                assert(call->gtEntryPoint.accessType == IAT_VALUE);
                addr = call->gtEntryPoint.addr;
            }
            else
#endif // FEATURE_READYTORUN
                if (call->gtCallType == CT_HELPER)
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
    for (varNum = 0; varNum < compiler->info.compArgsCount; varNum++)
    {
        varDsc = compiler->lvaGetDesc(varNum);

        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaGetDesc(fieldVarNum);
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
        assert(varDsc->IsEnregisterableLcl());
        var_types storeType = varDsc->GetStackSlotHomeType();
        emitAttr  storeSize = emitActualTypeSize(storeType);

        GetEmitter()->emitIns_S_R(ins_Store(storeType), storeSize, varDsc->GetRegNum(), varNum, 0);

        // Update lvRegNum life and GC info to indicate lvRegNum is dead and varDsc stack slot is going live.
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
    for (varNum = 0; varNum < compiler->info.compArgsCount; varNum++)
    {
        varDsc = compiler->lvaGetDesc(varNum);
        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaGetDesc(fieldVarNum);
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

            if (varTypeIsStruct(varDsc))
            {
                // Must be <= 16 bytes or else it wouldn't be passed in registers, except for HFA,
                // which can be bigger (and is handled above).
                noway_assert(EA_SIZE_IN_BYTES(varDsc->lvSize()) <= 16);
                if (emitter::isFloatReg(argReg))
                {
                    loadType = varDsc->lvIs4Field1 ? TYP_FLOAT : TYP_DOUBLE;
                }
                else
                {
                    loadType = varDsc->GetLayout()->GetGCPtrType(0);
                }
            }
            else
            {
                loadType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));
            }
            emitAttr loadSize = emitActualTypeSize(loadType);
            GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argReg, varNum, 0);

            // Update argReg life and GC Info to indicate varDsc stack slot is dead and argReg is going live.
            // Note that we cannot modify varDsc->GetRegNum() here because another basic block may not be
            // expecting it. Therefore manually update life of argReg.  Note that GT_JMP marks the end of
            // the basic block and after which reg life and gc info will be recomputed for the new block
            // in genCodeForBBList().
            regSet.AddMaskVars(genRegMask(argReg));
            gcInfo.gcMarkRegPtrVal(argReg, loadType);

            if (varDsc->GetOtherArgReg() < REG_STK)
            {
                // Restore the second register.
                argRegNext = varDsc->GetOtherArgReg();

                if (emitter::isFloatReg(argRegNext))
                {
                    loadType = varDsc->lvIs4Field2 ? TYP_FLOAT : TYP_DOUBLE;
                }
                else
                {
                    loadType = varDsc->GetLayout()->GetGCPtrType(1);
                }

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
            regNumber tempReg = cast->GetSingleTempReg();
            // We need to check if the value is not greater than 0xFFFFFFFF
            // if the upper 32 bits are zero.
            ssize_t imm = -1;
            GetEmitter()->emitIns_R_R_I(INS_addi, EA_8BYTE, tempReg, REG_R0, imm);

            GetEmitter()->emitIns_R_R_I(INS_slli, EA_8BYTE, tempReg, tempReg, 32);
            GetEmitter()->emitIns_R_R_R(INS_and, EA_8BYTE, tempReg, reg, tempReg);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, tempReg);
        }
        break;

        case GenIntCastDesc::CHECK_POSITIVE_INT_RANGE:
        {
            regNumber tempReg = cast->GetSingleTempReg();
            // We need to check if the value is not greater than 0x7FFFFFFF
            // if the upper 33 bits are zero.
            // instGen_Set_Reg_To_Imm(EA_8BYTE, tempReg, 0xFFFFFFFF80000000LL);
            ssize_t imm = -1;
            GetEmitter()->emitIns_R_R_I(INS_addi, EA_8BYTE, tempReg, REG_R0, imm);

            GetEmitter()->emitIns_R_R_I(INS_slli, EA_8BYTE, tempReg, tempReg, 31);

            GetEmitter()->emitIns_R_R_R(INS_and, EA_8BYTE, tempReg, reg, tempReg);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, tempReg);
        }
        break;

        case GenIntCastDesc::CHECK_INT_RANGE:
        {
            const regNumber tempReg = cast->GetSingleTempReg();
            assert(tempReg != reg);
            GetEmitter()->emitLoadImmediate(EA_8BYTE, tempReg, INT32_MAX);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_blt, tempReg, nullptr, reg);

            GetEmitter()->emitLoadImmediate(EA_8BYTE, tempReg, INT32_MIN);
            genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_blt, reg, nullptr, tempReg);
        }
        break;

        default:
        {
            assert(desc.CheckKind() == GenIntCastDesc::CHECK_SMALL_INT_RANGE);
            const int       castMaxValue = desc.CheckSmallIntMax();
            const int       castMinValue = desc.CheckSmallIntMin();
            const regNumber tempReg      = cast->GetSingleTempReg();
            instruction     ins;

            if (castMaxValue > 2047)
            {
                assert((castMaxValue == 32767) || (castMaxValue == 65535));
                GetEmitter()->emitLoadImmediate(EA_ATTR(desc.CheckSrcSize()), tempReg, castMaxValue + 1);
                ins = castMinValue == 0 ? INS_bgeu : INS_bge;
                genJumpToThrowHlpBlk_la(SCK_OVERFLOW, ins, reg, nullptr, tempReg);
            }
            else
            {
                GetEmitter()->emitIns_R_R_I(INS_addiw, EA_ATTR(desc.CheckSrcSize()), tempReg, REG_R0, castMaxValue);
                ins = castMinValue == 0 ? INS_bltu : INS_blt;
                genJumpToThrowHlpBlk_la(SCK_OVERFLOW, ins, tempReg, nullptr, reg);
            }

            if (castMinValue != 0)
            {
                if (emitter::isValidSimm12(castMinValue))
                {
                    GetEmitter()->emitIns_R_R_I(INS_slti, EA_ATTR(desc.CheckSrcSize()), tempReg, reg, castMinValue);
                }
                else
                {
                    GetEmitter()->emitLoadImmediate(EA_8BYTE, tempReg, castMinValue);
                    GetEmitter()->emitIns_R_R_R(INS_slt, EA_ATTR(desc.CheckSrcSize()), tempReg, reg, tempReg);
                }
                genJumpToThrowHlpBlk_la(SCK_OVERFLOW, INS_bne, tempReg);
            }
        }
        break;
    }
}

void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    genConsumeRegs(cast->gtGetOp1());

    emitter*            emit    = GetEmitter();
    var_types           dstType = cast->CastToType();
    var_types           srcType = genActualType(cast->gtGetOp1()->TypeGet());
    const regNumber     srcReg  = cast->gtGetOp1()->GetRegNum();
    const regNumber     dstReg  = cast->GetRegNum();
    const unsigned char size    = 32;

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
                    emit->emitIns_R_R_I(INS_slli, EA_PTRSIZE, dstReg, srcReg, 64 - 8);
                    emit->emitIns_R_R_I(INS_srli, EA_PTRSIZE, dstReg, dstReg, 64 - 8);
                }
                else
                {

                    emit->emitIns_R_R_I(INS_slli, EA_PTRSIZE, dstReg, srcReg, 64 - 16);
                    emit->emitIns_R_R_I(INS_srli, EA_PTRSIZE, dstReg, dstReg, 64 - 16);
                }
                break;
            case GenIntCastDesc::SIGN_EXTEND_SMALL_INT:
                if (desc.ExtendSrcSize() == 1)
                {
                    emit->emitIns_R_R_I(INS_slli, EA_PTRSIZE, dstReg, srcReg, 64 - 8);
                    emit->emitIns_R_R_I(INS_srai, EA_PTRSIZE, dstReg, dstReg, 64 - 8);
                }
                else
                {
                    emit->emitIns_R_R_I(INS_slli, EA_PTRSIZE, dstReg, srcReg, 64 - 16);
                    emit->emitIns_R_R_I(INS_srai, EA_PTRSIZE, dstReg, dstReg, 64 - 16);
                }
                break;

            case GenIntCastDesc::ZERO_EXTEND_INT:

                emit->emitIns_R_R_I(INS_slli, EA_PTRSIZE, dstReg, srcReg, 32);
                emit->emitIns_R_R_I(INS_srli, EA_PTRSIZE, dstReg, dstReg, 32);
                break;
            case GenIntCastDesc::SIGN_EXTEND_INT:
                emit->emitIns_R_R_I(INS_slliw, EA_4BYTE, dstReg, srcReg, 0);
                break;

            default:
                assert(desc.ExtendKind() == GenIntCastDesc::COPY);
                if (srcType == TYP_INT)
                {
                    emit->emitIns_R_R_I(INS_slliw, EA_4BYTE, dstReg, srcReg, 0);
                }
                else
                {
                    emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, dstReg, srcReg, 0);
                }
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
        instruction ins = (srcType == TYP_FLOAT) ? INS_fcvt_d_s  // convert Single to Double
                                                 : INS_fcvt_s_d; // convert Double to Single

        GetEmitter()->emitIns_R_R(ins, emitActualTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum());
    }
    else if (treeNode->GetRegNum() != op1->GetRegNum())
    {
        // If double to double cast or float to float cast. Emit a move instruction.
        instruction ins = (srcType == TYP_FLOAT) ? INS_fsgnj_s : INS_fsgnj_d;
        GetEmitter()->emitIns_R_R_R(ins, emitActualTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum(),
                                    op1->GetRegNum());
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

#ifdef FEATURE_REMAP_FUNCTION
    if (compiler->opts.compDbgEnC)
    {
        NYI_RISCV64("compDbgEnc in genCreateAndStoreGCInfo-----unimplemented/unused on RISCV64 yet----");
    }
#endif // FEATURE_REMAP_FUNCTION

    if (compiler->opts.IsReversePInvoke())
    {
        unsigned reversePInvokeFrameVarNumber = compiler->lvaReversePInvokeFrameVar;
        assert(reversePInvokeFrameVarNumber != BAD_VAR_NUM);
        const LclVarDsc* reversePInvokeFrameVar = compiler->lvaGetDesc(reversePInvokeFrameVarNumber);
        gcInfoEncoder->SetReversePInvokeFrameSlot(reversePInvokeFrameVar->GetStackOffset());
    }

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
}

//------------------------------------------------------------------------
// genCodeForStoreBlk: Produce code for a GT_STORE_DYN_BLK/GT_STORE_BLK node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForStoreBlk(GenTreeBlk* blkOp)
{
    assert(blkOp->OperIs(GT_STORE_DYN_BLK, GT_STORE_BLK));

    if (blkOp->gtBlkOpGcUnsafe)
    {
        GetEmitter()->emitDisableGC();
    }
    bool isCopyBlk = blkOp->OperIsCopyBlkOp();

    switch (blkOp->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindCpObjUnroll:
            assert(!blkOp->gtBlkOpGcUnsafe);
            genCodeForCpObj(blkOp->AsBlk());
            break;

        case GenTreeBlk::BlkOpKindLoop:
            assert(!isCopyBlk);
            genCodeForInitBlkLoop(blkOp);
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

    // So for the case of a LEA node of the form [Base + Index*Scale + Offset] we will generate:
    // tmpReg = indexReg << scale;
    // destReg = baseReg + tmpReg;
    // destReg = destReg + offset;
    //
    // TODO-RISCV64-CQ: The purpose of the GT_LEA node is to directly reflect a single target architecture
    //             addressing mode instruction.  Currently we're 'cheating' by producing one or more
    //             instructions to generate the addressing mode so we need to modify lowering to
    //             produce LEAs that are a 1:1 relationship to the RISCV64 architecture.
    if (lea->HasBase() && lea->HasIndex())
    {
        GenTree* memBase = lea->Base();
        GenTree* index   = lea->Index();

        DWORD scale;

        assert(isPow2(lea->gtScale));
        BitScanForward(&scale, lea->gtScale);
        assert(scale <= 4);
        regNumber scaleTempReg = scale ? lea->ExtractTempReg() : REG_NA;

        if (offset == 0)
        {
            // Then compute target reg from [base + index*scale]
            genScaledAdd(size, lea->GetRegNum(), memBase->GetRegNum(), index->GetRegNum(), scale, scaleTempReg);
        }
        else
        {
            // When generating fully interruptible code we have to use the "large offset" sequence
            // when calculating a EA_BYREF as we can't report a byref that points outside of the object
            bool useLargeOffsetSeq = compiler->GetInterruptible() && (size == EA_BYREF);

            if (!useLargeOffsetSeq && emitter::isValidSimm12(offset))
            {
                genScaledAdd(size, lea->GetRegNum(), memBase->GetRegNum(), index->GetRegNum(), scale, scaleTempReg);
                instruction ins = size == EA_4BYTE ? INS_addiw : INS_addi;
                emit->emitIns_R_R_I(ins, size, lea->GetRegNum(), lea->GetRegNum(), offset);
            }
            else
            {
                regNumber tmpReg = lea->GetSingleTempReg();

                noway_assert(tmpReg != index->GetRegNum());
                noway_assert(tmpReg != memBase->GetRegNum());

                // compute the large offset.
                instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

                genScaledAdd(EA_PTRSIZE, tmpReg, tmpReg, index->GetRegNum(), scale, scaleTempReg);

                instruction ins = size == EA_4BYTE ? INS_addw : INS_add;
                emit->emitIns_R_R_R(ins, size, lea->GetRegNum(), tmpReg, memBase->GetRegNum());
            }
        }
    }
    else if (lea->HasBase())
    {
        GenTree* memBase = lea->Base();

        if (emitter::isValidSimm12(offset))
        {
            if (offset != 0)
            {
                // Then compute target reg from [memBase + offset]
                emit->emitIns_R_R_I(INS_addi, size, lea->GetRegNum(), memBase->GetRegNum(), offset);
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
            emit->emitLoadImmediate(EA_PTRSIZE, tmpReg, offset);

            // Then compute target reg from [memBase + tmpReg]
            emit->emitIns_R_R_R(INS_add, size, lea->GetRegNum(), memBase->GetRegNum(), tmpReg);
        }
    }
    else if (lea->HasIndex())
    {
        // If we encounter a GT_LEA node without a base it means it came out
        // when attempting to optimize an arbitrary arithmetic expression during lower.
        // This is currently disabled in RISCV64 since we need to adjust lower to account
        // for the simpler instructions RISCV64 supports.
        // TODO-RISCV64-CQ:  Fix this and let LEA optimize arithmetic trees too.
        assert(!"We shouldn't see a baseless address computation during CodeGen for RISCV64");
    }

    genProduceReg(lea);
}

//------------------------------------------------------------------------
// genEstablishFramePointer: Set up the frame pointer by adding an offset to the stack pointer.
//
// Arguments:
//    delta - the offset to add to the current stack pointer to establish the frame pointer
//    reportUnwindData - true if establishing the frame pointer should be reported in the OS unwind data.

void CodeGen::genEstablishFramePointer(int delta, bool reportUnwindData)
{
    assert(compiler->compGeneratingProlog);

    assert(emitter::isValidSimm12(delta));
    GetEmitter()->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_FPBASE, REG_SPBASE, delta);

    if (reportUnwindData)
    {
        compiler->unwindSetFrameReg(REG_FPBASE, delta);
    };
}

//------------------------------------------------------------------------
// genStackProbe: Probe the stack without changing it
//
// Notes:
//      This function is using loop to probe each memory page.
//
// Arguments:
//    frameSize - total frame size
//    rOffset - usually initial register number
//    rLimit - an extra register for comparison
//    rPageSize - register for storing page size
//
void CodeGen::genStackProbe(ssize_t frameSize, regNumber rOffset, regNumber rLimit, regNumber rPageSize)
{
    // make sure frameSize safely fits within 4 bytes
    noway_assert((ssize_t)(int)frameSize == (ssize_t)frameSize);

    const target_size_t pageSize = compiler->eeGetPageSize();

    // According to RISC-V Privileged ISA page size should be equal 4KiB
    noway_assert(pageSize == 0x1000);

    emitter* emit = GetEmitter();

    emit->emitLoadImmediate(EA_PTRSIZE, rLimit, -frameSize);
    regSet.verifyRegUsed(rLimit);

    emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, rLimit, rLimit, REG_SPBASE);

    emit->emitIns_R_I(INS_lui, EA_PTRSIZE, rPageSize, pageSize >> 12);
    regSet.verifyRegUsed(rPageSize);

    emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, rOffset, REG_SPBASE, rPageSize);

    // Loop:
    // tickle the page - Read from the updated SP - this triggers a page fault when on the guard page
    emit->emitIns_R_R_I(INS_lw, EA_4BYTE, REG_R0, rOffset, 0);
    emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, rOffset, rOffset, rPageSize);

    // each instr is 4 bytes
    // if (rOffset >= rLimit) goto Loop;
    emit->emitIns_R_R_I(INS_bge, EA_PTRSIZE, rOffset, rLimit, -2 << 2);
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

    // According to RISC-V Privileged ISA page size should be equal 4KiB
    const target_size_t pageSize = compiler->eeGetPageSize();

    assert(!compiler->info.compPublishStubParam || (REG_SECRET_STUB_PARAM != initReg));

    target_size_t lastTouchDelta = 0;

    emitter* emit = GetEmitter();

    // Emit the following sequence to 'tickle' the pages.
    // Note it is important that stack pointer not change until this is complete since the tickles
    // could cause a stack overflow, and we need to be able to crawl the stack afterward
    // (which means the stack pointer needs to be known).

    if (frameSize < pageSize)
    {
        // no probe needed
        lastTouchDelta = frameSize;
    }
    else if (frameSize < 3 * pageSize)
    {
        // between 1 and 3 pages we will probe each page without a loop,
        // because it is faster that way and doesn't cost us much
        lastTouchDelta = frameSize;

        for (target_size_t probeOffset = pageSize; probeOffset <= frameSize; probeOffset += pageSize)
        {
            emit->emitIns_R_I(INS_lui, EA_PTRSIZE, initReg, probeOffset >> 12);
            regSet.verifyRegUsed(initReg);

            emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, initReg, REG_SPBASE, initReg);
            emit->emitIns_R_R_I(INS_lw, EA_4BYTE, REG_R0, initReg, 0);

            lastTouchDelta -= pageSize;
        }

        assert(pInitRegZeroed != nullptr);
        *pInitRegZeroed = false; // The initReg does not contain zero

        assert(lastTouchDelta == frameSize % pageSize);
        compiler->unwindPadding();
    }
    else
    {
        // probe each page, that we need to allocate large stack frame
        assert(frameSize >= 3 * pageSize);

        regMaskTP availMask = RBM_ALLINT & (regSet.rsGetModifiedRegsMask() | ~RBM_INT_CALLEE_SAVED);
        availMask &= ~maskArgRegsLiveIn;   // Remove all of the incoming argument registers
                                           // as they are currently live
        availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg

        noway_assert(availMask != RBM_NONE);

        regMaskTP regMask = genFindLowestBit(availMask);
        regNumber rLimit  = genRegNumFromMask(regMask);

        availMask &= ~regMask; // Remove rLimit register

        noway_assert(availMask != RBM_NONE);

        regMask             = genFindLowestBit(availMask);
        regNumber rPageSize = genRegNumFromMask(regMask);

        genStackProbe((ssize_t)frameSize, initReg, rLimit, rPageSize);

        assert(pInitRegZeroed != nullptr);
        *pInitRegZeroed = false; // The initReg does not contain zero

        lastTouchDelta = frameSize % pageSize;
        compiler->unwindPadding();
    }

#if STACK_PROBE_BOUNDARY_THRESHOLD_BYTES != 0
    // if the last page was too far, we will make an extra probe at the bottom
    if (lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize)
    {
        assert(lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES < pageSize << 1);

        emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, initReg, REG_R0, frameSize);
        regSet.verifyRegUsed(initReg);

        emit->emitIns_R_R_R(INS_sub, EA_PTRSIZE, initReg, REG_SPBASE, initReg);
        emit->emitIns_R_R_I(INS_lw, EA_4BYTE, REG_R0, initReg, 0);

        assert(pInitRegZeroed != nullptr);
        *pInitRegZeroed = false; // The initReg does not contain zero

        compiler->unwindPadding();
    }
#endif
}

void CodeGen::genJumpToThrowHlpBlk_la(
    SpecialCodeKind codeKind, instruction ins, regNumber reg1, BasicBlock* failBlk, regNumber reg2)
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
            assert(add->acdUsed);
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
            assert(add->acdUsed);
            excpRaisingBlock = add->acdDstBlk;
#if !FEATURE_FIXED_OUT_ARGS
            assert(add->acdStkLvlInit || isFramePointerUsed());
#endif // !FEATURE_FIXED_OUT_ARGS
        }

        noway_assert(excpRaisingBlock != nullptr);

        // Jump to the exception-throwing block on error.
        emit->emitIns_J(ins, excpRaisingBlock, (int)reg1 | ((int)reg2 << 5)); // 5-bits;
    }
    else
    {
        // The code to throw the exception will be generated inline, and
        //  we will jump around it in the normal non-exception case.

        void* pAddr = nullptr;
        void* addr  = compiler->compGetHelperFtn((CorInfoHelpFunc)(compiler->acdHelper(codeKind)), &pAddr);
        emitter::EmitCallType callType;
        regNumber             callTarget;

        // maybe optimize
        // ins = (instruction)(ins^((ins != INS_beq)+(ins != INS_bne)));
        if (ins == INS_blt)
        {
            ins = INS_bge;
        }
        else if (ins == INS_bltu)
        {
            ins = INS_bgeu;
        }
        else if (ins == INS_bge)
        {
            ins = INS_blt;
        }
        else if (ins == INS_bgeu)
        {
            ins = INS_bltu;
        }
        else
        {
            ins = ins == INS_beq ? INS_bne : INS_beq;
        }

        if (addr == nullptr)
        {
            callType   = emitter::EC_INDIR_R;
            callTarget = REG_DEFAULT_HELPER_CALL_TARGET;
            if (compiler->opts.compReloc)
            {
                ssize_t imm = (3 + 1) << 2;
                emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, reg2, imm);
                emit->emitIns_R_AI(INS_jal, EA_PTR_DSP_RELOC, callTarget, (ssize_t)pAddr);
            }
            else
            {
                ssize_t imm = 9 << 2;
                emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, reg2, imm);
                // TODO-RISCV64-CQ: In the future we may consider using emitter::emitLoadImmediate instead,
                // which is less straightforward but offers slightly better codegen.
                emitLoadConstAtAddr(GetEmitter(), callTarget, (ssize_t)pAddr);
            }
            regSet.verifyRegUsed(callTarget);
        }
        else
        { // INS_OPTS_C
            callType   = emitter::EC_FUNC_TOKEN;
            callTarget = REG_NA;

            ssize_t imm = 9 << 2;
            if (compiler->opts.compReloc)
            {
                imm = 3 << 2;
            }

            emit->emitIns_R_R_I(ins, EA_PTRSIZE, reg1, reg2, imm);
        }

        BasicBlock* skipLabel = genCreateTempLabel();

        emit->emitIns_Call(callType, compiler->eeFindHelper(compiler->acdHelper(codeKind)),
                           INDEBUG_LDISASM_COMMA(nullptr) addr, 0, EA_UNKNOWN, EA_UNKNOWN, gcInfo.gcVarPtrSetCur,
                           gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur, DebugInfo(), /* IL offset */
                           callTarget,                                                    /* ireg */
                           REG_NA, 0, 0,                                                  /* xreg, xmul, disp */
                           false                                                          /* isJump */
                           );

        regMaskTP killMask = compiler->compHelperCallKillSet((CorInfoHelpFunc)(compiler->acdHelper(codeKind)));
        regSet.verifyRegistersUsed(killMask);

        // NOTE: here is just defining an `empty` label which will create a new IGroup for updating the gcInfo.
        genDefineTempLabel(skipLabel);
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

    // TODO-RISCV64: Use the exact barrier type depending on the CPU.
    GetEmitter()->emitIns_I(INS_fence, EA_4BYTE, INS_BARRIER_FULL);
}

/*-----------------------------------------------------------------------------
 *
 * Push/Pop any callee-saved registers we have used,
 * For most frames, generatint liking:
 *      addi sp, sp, -#framesz      ; establish the frame
 *
 *      ; save float regs
 *      fsd f8, #offset(sp)
 *      fsd f9, #(offset+8)(sp)
 *      fsd f18, #(offset+16)(sp)
 *      ; ...
 *      fsd f27, #(offset+8*11)(sp)
 *
 *      ; save int regs
 *      sd s1, #offset2(sp)
 *      sd s2, #(offset2+8)(sp)
 *      ; ...
 *      sd s11, #(offset+8*10)(sp)
 *
 *      ; save ra, fp
 *      sd ra, #offset3(sp)         ; save RA (8 bytes)
 *      sd fp, #(offset3+8)(sp)     ; save FP (8 bytes)
 *
 * Notes:
 * 1. FP is always saved, and the first store is FP, RA.
 * 2. General-purpose registers are 8 bytes, floating-point registers are 8 bytes.
 * 3. For frames with varargs, not implemented completely and not tested !
 * 4. We allocate the frame here; no further changes to SP are allowed (except in the body, for localloc).
 *
 * For functions with GS and localloc, we change the frame so the frame pointer and RA are saved at the top
 * of the frame, just under the varargs registers (if any). Note that the funclet frames must follow the same
 * rule, and both main frame and funclet frames (if any) must put PSPSym in the same offset from Caller-SP.
 * Since this frame type is relatively rare, we force using it via stress modes, for additional coverage.
 *
 * The frames look like the following (simplified to only include components that matter for establishing the
 * frames). See also Compiler::lvaAssignFrameOffsets().
 *
 * The RISC-V's frame layout is liking:
 *
 *      |                       |
 *      |-----------------------|
 *      |  incoming arguments   |
 *      +=======================+ <---- Caller's SP
 *      |     Arguments  Or     | // if needed
 *      |  Varargs regs space   | // Only for varargs functions; NYI on RV64
 *      |-----------------------|
 *      |    MonitorAcquired    | // 8 bytes; for synchronized methods
 *      |-----------------------|
 *      |        PSP slot       | // 8 bytes (omitted in NativeAOT ABI)
 *      |-----------------------|
 *      | locals, temps, etc.   |
 *      |-----------------------|
 *      |  possible GS cookie   |
 *      |-----------------------|
 *      |      Saved FP         | // 8 bytes
 *      |-----------------------|
 *      |      Saved RA         | // 8 bytes
 *      |-----------------------|
 *      |Callee saved registers | // not including FP/RA; multiple of 8 bytes
 *      |-----------------------|
 *      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
 *      |-----------------------| <---- Ambient SP
 *      |       |               |
 *      ~       | Stack grows   ~
 *      |       | downward      |
 *              V
 *
 */
void CodeGen::genPushCalleeSavedRegisters(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    // Unlike on x86/x64, we can also push float registers to stack
    regMaskTP rsPushRegs = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

    // On RV64 we always use the FP (frame-pointer)
    assert(isFramePointerUsed());

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
    // Given the limited benefit from this optimization (<10k for SPCL NGen image), the extra complexity
    // is not worth it.
    //

    // we will push callee-saved registers along with fp and ra registers to stack
    regMaskTP rsPushRegsMask = rsPushRegs | RBM_FP | RBM_RA;
    regSet.rsMaskCalleeSaved = rsPushRegsMask;

#ifdef DEBUG
    if (compiler->compCalleeRegsPushed != genCountBits(rsPushRegsMask))
    {
        printf("Error: unexpected number of callee-saved registers to push. Expected: %d. Got: %d ",
               compiler->compCalleeRegsPushed, genCountBits(rsPushRegsMask));
        dspRegMask(rsPushRegsMask);
        printf("\n");
        assert(compiler->compCalleeRegsPushed == genCountBits(rsPushRegsMask));
    }

    if (verbose)
    {
        regMaskTP maskSaveRegsFloat = rsPushRegs & RBM_FLT_CALLEE_SAVED;
        regMaskTP maskSaveRegsInt   = rsPushRegs & RBM_INT_CALLEE_SAVED;

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

    // This will be the starting place for saving the callee-saved registers, in increasing order.
    int offset = compiler->lvaOutgoingArgSpaceSize;

    int totalFrameSize = genTotalFrameSize();

    emitter* emit = GetEmitter();

    // ensure offset of sd/ld
    if (totalFrameSize <= 2040)
    {
        frameType = 1;

        emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, -totalFrameSize);
        compiler->unwindAllocStack(totalFrameSize);

        JITDUMP("Frame type 1. #outsz=%d; #framesz=%d; LclFrameSize=%d\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
                totalFrameSize, compiler->compLclFrameSize);
    }
    else
    {
        frameType = 2;
        // we have to adjust stack pointer; probably using add instead of addi

        JITDUMP("Frame type 2. #outsz=%d; #framesz=%d; LclFrameSize=%d\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
                totalFrameSize, compiler->compLclFrameSize);

        if ((offset + (compiler->compCalleeRegsPushed << 3)) >= 2040)
        {
            offset            = totalFrameSize - compiler->lvaOutgoingArgSpaceSize;
            calleeSaveSPDelta = AlignUp((UINT)offset, STACK_ALIGN);
            offset            = calleeSaveSPDelta - offset;

            genStackPointerAdjustment(-calleeSaveSPDelta, initReg, pInitRegZeroed, /* reportUnwindData */ true);
        }
        else
        {
            genStackPointerAdjustment(-totalFrameSize, initReg, pInitRegZeroed, /* reportUnwindData */ true);
        }
    }

    JITDUMP("    offset=%d, calleeSaveSPDelta=%d\n", offset, calleeSaveSPDelta);

    genSaveCalleeSavedRegistersHelp(rsPushRegs, offset, 0);
    offset += (int)(genCountBits(rsPushRegs) << 3); // each reg has 8 bytes

    emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_RA, REG_SPBASE, offset);
    compiler->unwindSaveReg(REG_RA, offset);

    emit->emitIns_R_R_I(INS_sd, EA_PTRSIZE, REG_FP, REG_SPBASE, offset + 8);
    compiler->unwindSaveReg(REG_FP, offset + 8);

    JITDUMP("    offsetSpToSavedFp=%d\n", offset + 8);
    genEstablishFramePointer(offset + 8, /* reportUnwindData */ true);

    // For varargs, home the incoming arg registers last. Note that there is nothing to unwind here,
    // so we just report "NOP" unwind codes. If there's no more frame setup after this, we don't
    // need to add codes at all.
    if (compiler->info.compIsVarArgs)
    {
        JITDUMP("    compIsVarArgs=true\n");
        NYI_RISCV64("genPushCalleeSavedRegisters unsupports compIsVarArgs");
    }

#ifdef DEBUG
    if (compiler->opts.disAsm)
    {
        printf("DEBUG: RISCV64, frameType:%d\n\n", frameType);
    }
#endif

    if (calleeSaveSPDelta != 0)
    {
        assert(frameType == 2);
        calleeSaveSPDelta = totalFrameSize - calleeSaveSPDelta;
        genStackPointerAdjustment(-calleeSaveSPDelta, initReg, pInitRegZeroed, /* reportUnwindData */ true);
    }
}

void CodeGen::genPopCalleeSavedRegisters(bool jmpEpilog)
{
    assert(compiler->compGeneratingEpilog);

    regMaskTP regsToRestoreMask = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

    // On RV64 we always use the FP (frame-pointer)
    assert(isFramePointerUsed());

    int totalFrameSize     = genTotalFrameSize();
    int remainingSPSize    = totalFrameSize;
    int callerSPtoFPdelta  = 0;
    int calleeSaveSPOffset = 0; // This will be the starting place for restoring
                                // the callee-saved registers, in decreasing order.

    emitter* emit = GetEmitter();

    // ensure offset of sd/ld
    if (totalFrameSize <= 2040)
    {
        JITDUMP("Frame type 1. #outsz=%d; #framesz=%d; localloc? %s\n", unsigned(compiler->lvaOutgoingArgSpaceSize),
                totalFrameSize, dspBool(compiler->compLocallocUsed));

        if (compiler->compLocallocUsed)
        {
            callerSPtoFPdelta = (compiler->compCalleeRegsPushed << 3) - 8 + compiler->lvaOutgoingArgSpaceSize;
        }
        calleeSaveSPOffset = compiler->lvaOutgoingArgSpaceSize;
        // remainingSPSize = totalFrameSize;
    }
    else
    {
        JITDUMP("Frame type 2. #outsz=%d; #framesz=%d; calleeSaveRegsPushed: %d; localloc? %s\n",
                unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, compiler->compCalleeRegsPushed,
                dspBool(compiler->compLocallocUsed));

        if ((compiler->lvaOutgoingArgSpaceSize + (compiler->compCalleeRegsPushed << 3)) >= 2040)
        {
            calleeSaveSPOffset = compiler->lvaOutgoingArgSpaceSize & 0xfffffff0;

            if (compiler->compLocallocUsed)
            {
                callerSPtoFPdelta = (compiler->compCalleeRegsPushed << 3) - 8;
            }
            else
            {
                genStackPointerAdjustment(calleeSaveSPOffset, REG_RA, nullptr, /* reportUnwindData */ true);
            }
            calleeSaveSPOffset = compiler->lvaOutgoingArgSpaceSize - calleeSaveSPOffset;
            remainingSPSize    = remainingSPSize - calleeSaveSPOffset;
        }
        else
        {
            if (compiler->compLocallocUsed)
            {
                callerSPtoFPdelta = (compiler->compCalleeRegsPushed << 3) - 8 + compiler->lvaOutgoingArgSpaceSize;
            }
            calleeSaveSPOffset = compiler->lvaOutgoingArgSpaceSize;
            // remainingSPSize = totalFrameSize;
        }
    }

    if (compiler->compLocallocUsed)
    {
        // restore sp form fp: addi sp, -#callerSPtoFPdelta(fp)
        emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_FPBASE, -callerSPtoFPdelta);
        compiler->unwindSetFrameReg(REG_FPBASE, callerSPtoFPdelta);
    }

    JITDUMP("    calleeSaveSPOffset=%d, callerSPtoFPdelta=%d\n", calleeSaveSPOffset, callerSPtoFPdelta);
    genRestoreCalleeSavedRegistersHelp(regsToRestoreMask, calleeSaveSPOffset, 0);

    // restore ra/fp regs
    calleeSaveSPOffset += (compiler->compCalleeRegsPushed - 2) << 3;

    emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_RA, REG_SPBASE, calleeSaveSPOffset);
    compiler->unwindSaveReg(REG_RA, calleeSaveSPOffset);

    emit->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_FP, REG_SPBASE, calleeSaveSPOffset + 8);
    compiler->unwindSaveReg(REG_FP, calleeSaveSPOffset + 8);

    if (emitter::isValidUimm11(remainingSPSize))
    {
        emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, remainingSPSize);
    }
    else
    {
        regNumber tempReg = rsGetRsvdReg();
        emit->emitLoadImmediate(EA_PTRSIZE, tempReg, remainingSPSize);
        emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, tempReg);
    }
    compiler->unwindAllocStack(remainingSPSize);

    // for OSR we have to adjust SP to remove tier0 frame
    if (compiler->opts.IsOSR())
    {
        const int tier0FrameSize = compiler->info.compPatchpointInfo->TotalFrameSize();
        JITDUMP("Extra SP adjust for OSR to pop off Tier0 frame: %d bytes\n", tier0FrameSize);

        if (emitter::isValidUimm11(tier0FrameSize))
        {
            emit->emitIns_R_R_I(INS_addi, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, tier0FrameSize);
        }
        else
        {
            regNumber tempReg = rsGetRsvdReg();
            emit->emitLoadImmediate(EA_PTRSIZE, tempReg, tier0FrameSize);
            emit->emitIns_R_R_R(INS_add, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, tempReg);
        }
        compiler->unwindAllocStack(tier0FrameSize);
    }
}

void CodeGen::genFnPrologCalleeRegArgs()
{
    assert(!(intRegState.rsCalleeRegArgMaskLiveIn & floatRegState.rsCalleeRegArgMaskLiveIn));

    regMaskTP regArgMaskLive = intRegState.rsCalleeRegArgMaskLiveIn | floatRegState.rsCalleeRegArgMaskLiveIn;

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnPrologCalleeRegArgs() RISCV64:0x%llx.\n", regArgMaskLive);
    }
#endif

    // We should be generating the prolog block when we are called
    assert(compiler->compGeneratingProlog);

    // We expect to have some registers of the type we are doing, that are LiveIn, otherwise we don't need to be called.
    noway_assert(regArgMaskLive != 0);

    unsigned varNum;
    unsigned regArgNum = 0;
    // Process any rearrangements including circular dependencies.
    regNumber regArg[MAX_REG_ARG + MAX_FLOAT_REG_ARG];
    regNumber regArgInit[MAX_REG_ARG + MAX_FLOAT_REG_ARG];
    emitAttr  regArgAttr[MAX_REG_ARG + MAX_FLOAT_REG_ARG];

    for (int i = 0; i < MAX_REG_ARG + MAX_FLOAT_REG_ARG; i++)
    {
        regArg[i] = REG_NA;

#ifdef DEBUG
        regArgInit[i] = REG_NA;
        regArgAttr[i] = EA_UNKNOWN;
#endif
    }

    for (varNum = 0; varNum < compiler->lvaCount; ++varNum)
    {
        LclVarDsc* varDsc = compiler->lvaTable + varNum;

        // Is this variable a register arg?
        if (!varDsc->lvIsParam)
        {
            continue;
        }

        if (!varDsc->lvIsRegArg)
        {
            continue;
        }

        if (varDsc->lvIsInReg())
        {
            assert(genIsValidIntReg(varDsc->GetArgReg()) || genIsValidFloatReg(varDsc->GetArgReg()));
            assert(!(genIsValidIntReg(varDsc->GetOtherArgReg()) || genIsValidFloatReg(varDsc->GetOtherArgReg())));
            if (varDsc->GetArgInitReg() != varDsc->GetArgReg())
            {
                if (genIsValidIntReg(varDsc->GetArgInitReg()))
                {
                    if (varDsc->GetArgInitReg() > REG_ARG_LAST)
                    {
                        bool        isSkip;
                        instruction ins;
                        emitAttr    size;
                        if (genIsValidIntReg(varDsc->GetArgReg()))
                        {
                            ins = INS_mov;
                            if (varDsc->TypeGet() == TYP_INT)
                            {
                                size   = EA_4BYTE;
                                isSkip = false;
                            }
                            else
                            {
                                size   = EA_PTRSIZE;
                                isSkip = true;
                            }
                        }
                        else
                        {
                            ins    = INS_fmv_x_d;
                            size   = EA_PTRSIZE;
                            isSkip = true;
                        }
                        GetEmitter()->emitIns_Mov(ins, size, varDsc->GetArgInitReg(), varDsc->GetArgReg(), isSkip);
                        regArgMaskLive &= ~genRegMask(varDsc->GetArgReg());
                    }
                    else
                    {
                        if (genIsValidIntReg(varDsc->GetArgReg()))
                        {
                            assert(isValidIntArgReg(varDsc->GetArgReg()));
                            regArg[varDsc->GetArgReg() - REG_ARG_FIRST]     = varDsc->GetArgReg();
                            regArgInit[varDsc->GetArgReg() - REG_ARG_FIRST] = varDsc->GetArgInitReg();
                            regArgAttr[varDsc->GetArgReg() - REG_ARG_FIRST] =
                                varDsc->TypeGet() == TYP_INT ? EA_4BYTE : EA_PTRSIZE;
                        }
                        else
                        {
                            assert(isValidFloatArgReg(varDsc->GetArgReg()));
                            regArg[varDsc->GetArgReg() - REG_ARG_FP_FIRST + MAX_REG_ARG]     = varDsc->GetArgReg();
                            regArgInit[varDsc->GetArgReg() - REG_ARG_FP_FIRST + MAX_REG_ARG] = varDsc->GetArgInitReg();
                            regArgAttr[varDsc->GetArgReg() - REG_ARG_FP_FIRST + MAX_REG_ARG] =
                                varDsc->TypeGet() == TYP_FLOAT ? EA_4BYTE : EA_PTRSIZE;
                        }
                        regArgNum++;
                    }
                }
                else
                {
                    assert(genIsValidFloatReg(varDsc->GetArgInitReg()));
                    if (genIsValidIntReg(varDsc->GetArgReg()))
                    {
                        emitAttr size = (varDsc->TypeGet() == TYP_FLOAT) ? EA_4BYTE : EA_PTRSIZE;
                        GetEmitter()->emitIns_Mov(size, varDsc->GetArgInitReg(), varDsc->GetArgReg(), false);
                        regArgMaskLive &= ~genRegMask(varDsc->GetArgReg());
                    }
                    else if (varDsc->GetArgInitReg() > REG_ARG_FP_LAST)
                    {
                        GetEmitter()->emitIns_Mov(INS_fsgnj_d, EA_PTRSIZE, varDsc->GetArgInitReg(), varDsc->GetArgReg(),
                                                  true);
                        regArgMaskLive &= ~genRegMask(varDsc->GetArgReg());
                    }
                    else
                    {
                        assert(isValidFloatArgReg(varDsc->GetArgReg()));
                        regArg[varDsc->GetArgReg() - REG_ARG_FP_FIRST + MAX_REG_ARG]     = varDsc->GetArgReg();
                        regArgInit[varDsc->GetArgReg() - REG_ARG_FP_FIRST + MAX_REG_ARG] = varDsc->GetArgInitReg();
                        regArgAttr[varDsc->GetArgReg() - REG_ARG_FP_FIRST + MAX_REG_ARG] =
                            varDsc->TypeGet() == TYP_FLOAT ? EA_4BYTE : EA_PTRSIZE;
                        regArgNum++;
                    }
                }
            }
            else
            {
                // TODO-RISCV64: should delete this by optimization "struct {long a; int32_t b;};"
                // liking AMD64_ABI within morph.
                if (genIsValidIntReg(varDsc->GetArgReg()) && (varDsc->TypeGet() == TYP_INT))
                {
                    GetEmitter()->emitIns_Mov(INS_mov, EA_4BYTE, varDsc->GetArgInitReg(), varDsc->GetArgReg(), false);
                }
                regArgMaskLive &= ~genRegMask(varDsc->GetArgReg());
            }
#ifdef USING_SCOPE_INFO
            psiMoveToReg(varNum);
#endif // USING_SCOPE_INFO
            if (!varDsc->lvLiveInOutOfHndlr)
            {
                continue;
            }
        }

        // When we have a promoted struct we have two possible LclVars that can represent the incoming argument
        // in the regArgTab[], either the original TYP_STRUCT argument or the introduced lvStructField.
        // We will use the lvStructField if we have a TYPE_INDEPENDENT promoted struct field otherwise
        // use the original TYP_STRUCT argument.
        //
        if (varDsc->lvPromoted || varDsc->lvIsStructField)
        {
            LclVarDsc* parentVarDsc = varDsc;
            if (varDsc->lvIsStructField)
            {
                assert(!varDsc->lvPromoted);
                parentVarDsc = &compiler->lvaTable[varDsc->lvParentLcl];
            }

            Compiler::lvaPromotionType promotionType = compiler->lvaGetPromotionType(parentVarDsc);

            if (promotionType == Compiler::PROMOTION_TYPE_INDEPENDENT)
            {
                // For register arguments that are independent promoted structs we put the promoted field varNum
                // in the regArgTab[]
                if (varDsc->lvPromoted)
                {
                    continue;
                }
            }
            else
            {
                // For register arguments that are not independent promoted structs we put the parent struct varNum
                // in the regArgTab[]
                if (varDsc->lvIsStructField)
                {
                    continue;
                }
            }
        }

        var_types storeType = TYP_UNDEF;
        int       slotSize  = TARGET_POINTER_SIZE;

        if (varTypeIsStruct(varDsc))
        {
            if (emitter::isFloatReg(varDsc->GetArgReg()))
            {
                storeType = varDsc->lvIs4Field1 ? TYP_FLOAT : TYP_DOUBLE;
            }
            else
            {
                assert(emitter::isGeneralRegister(varDsc->GetArgReg()));
                if (varDsc->lvIs4Field1)
                {
                    storeType = TYP_INT;
                }
                else
                {
                    storeType = varDsc->GetLayout()->GetGCPtrType(0);
                }
            }
            slotSize = (int)EA_SIZE(emitActualTypeSize(storeType));

#if FEATURE_MULTIREG_ARGS
            // Must be <= MAX_PASS_MULTIREG_BYTES or else it wouldn't be passed in registers
            noway_assert(varDsc->lvSize() <= MAX_PASS_MULTIREG_BYTES);
#endif
        }
        else // Not a struct type
        {
            storeType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));
            if (emitter::isFloatReg(varDsc->GetArgReg()) != varTypeIsFloating(storeType))
            {
                assert(varTypeIsFloating(storeType));
                storeType = storeType == TYP_DOUBLE ? TYP_I_IMPL : TYP_INT;
            }
        }
        emitAttr size = emitActualTypeSize(storeType);

        regNumber srcRegNum = varDsc->GetArgReg();

        // Stack argument - if the ref count is 0 don't care about it
        if (!varDsc->lvOnFrame)
        {
            noway_assert(varDsc->lvRefCnt() == 0);
            regArgMaskLive &= ~genRegMask(varDsc->GetArgReg());
            if (varDsc->GetOtherArgReg() < REG_STK)
            {
                regArgMaskLive &= ~genRegMask(varDsc->GetOtherArgReg());
            }
        }
        else
        {
            assert(srcRegNum != varDsc->GetOtherArgReg());

            regNumber tmpReg = REG_NA;

            bool FPbased;
            int  baseOffset = compiler->lvaFrameAddress(varNum, &FPbased);

            // First store the `varDsc->GetArgReg()` on stack.
            if (emitter::isValidSimm12(baseOffset))
            {
                GetEmitter()->emitIns_S_R(ins_Store(storeType), size, srcRegNum, varNum, 0);
            }
            else
            {
                assert(tmpReg == REG_NA);

                tmpReg = REG_RA;
                // Prepare tmpReg to possible future use
                GetEmitter()->emitLoadImmediate(EA_PTRSIZE, tmpReg, baseOffset);
                GetEmitter()->emitIns_R_R_R(INS_add, EA_PTRSIZE, tmpReg, tmpReg, FPbased ? REG_FPBASE : REG_SPBASE);
                GetEmitter()->emitIns_S_R_R(ins_Store(storeType), size, srcRegNum, tmpReg, varNum, 0);
            }

            regArgMaskLive &= ~genRegMask(srcRegNum);

            // Then check if varDsc is a struct arg
            if (varTypeIsStruct(varDsc))
            {
                if (emitter::isFloatReg(varDsc->GetOtherArgReg()))
                {
                    srcRegNum = varDsc->GetOtherArgReg();
                    storeType = varDsc->lvIs4Field2 ? TYP_FLOAT : TYP_DOUBLE;
                    size      = EA_SIZE(emitActualTypeSize(storeType));

                    slotSize = slotSize < (int)size ? (int)size : slotSize;
                }
                else if (emitter::isGeneralRegister(varDsc->GetOtherArgReg()))
                {
                    if (varDsc->lvIs4Field2)
                    {
                        storeType = TYP_INT;
                    }
                    else
                    {
                        storeType = varDsc->GetLayout()->GetGCPtrType(1);
                    }

                    srcRegNum = varDsc->GetOtherArgReg();
                    size      = emitActualTypeSize(storeType);

                    slotSize = slotSize < (int)EA_SIZE(size) ? (int)EA_SIZE(size) : slotSize;
                }
                baseOffset += slotSize;

                // if the struct passed by two register, then store the second register `varDsc->GetOtherArgReg()`.
                if (srcRegNum == varDsc->GetOtherArgReg())
                {
                    GetEmitter()->emitIns_S_R_R(ins_Store(storeType), size, srcRegNum, tmpReg, varNum, slotSize);
                    regArgMaskLive &= ~genRegMask(srcRegNum); // maybe do this later is better!
                }
                else if (varDsc->lvIsSplit)
                {
                    // the struct is a split struct.
                    assert(varDsc->GetArgReg() == REG_ARG_LAST && varDsc->GetOtherArgReg() == REG_STK);

                    // For the RISCV64's ABI, the split struct arg will be passed via `A7` and a stack slot on caller.
                    // But if the `A7` is stored on stack on the callee side, the whole split struct should be
                    // stored continuous on the stack on the callee side.
                    // So, after we save `A7` on the stack in prolog, it has to copy the stack slot of the split struct
                    // which was passed by the caller. Here we load the stack slot to `REG_SCRATCH`, and save it
                    // on the stack following the `A7` in prolog.
                    if (emitter::isValidSimm12(genTotalFrameSize()))
                    {
                        GetEmitter()->emitIns_R_R_I(INS_ld, size, REG_SCRATCH, REG_SPBASE, genTotalFrameSize());
                    }
                    else
                    {
                        assert(!EA_IS_RELOC(size));
                        GetEmitter()->emitLoadImmediate(size, REG_SCRATCH, genTotalFrameSize());
                        GetEmitter()->emitIns_R_R_R(INS_add, size, REG_SCRATCH, REG_SCRATCH, REG_SPBASE);
                        GetEmitter()->emitIns_R_R_I(INS_ld, size, REG_SCRATCH, REG_SCRATCH, 0);
                    }

                    GetEmitter()->emitIns_S_R_R(ins_Store(storeType), size, REG_SCRATCH, tmpReg, varNum, slotSize);
                }
            }

#ifdef USING_SCOPE_INFO
            {
                psiMoveToStack(varNum);
            }
#endif // USING_SCOPE_INFO
        }
    }

    if (regArgNum > 0)
    {
        for (int i = MAX_REG_ARG + MAX_FLOAT_REG_ARG - 1; i >= 0; i--)
        {
            if (regArg[i] != REG_NA && !isValidIntArgReg(regArgInit[i]) && !isValidFloatArgReg(regArgInit[i]))
            {
                assert(regArg[i] != regArgInit[i]);
                assert(isValidIntArgReg(regArg[i]) || isValidFloatArgReg(regArg[i]));

                GetEmitter()->emitIns_Mov(regArgAttr[i], regArgInit[i], regArg[i], false);

                regArgMaskLive &= ~genRegMask(regArg[i]);
                regArg[i] = REG_NA;
                regArgNum -= 1;
            }
        }
    }

    if (regArgNum > 0)
    {
        for (int i = MAX_REG_ARG + MAX_FLOAT_REG_ARG - 1; i >= 0; i--)
        {
            if (regArg[i] != REG_NA)
            {
                assert(regArg[i] != regArgInit[i]);

                // regArg indexes list
                unsigned indexList[MAX_REG_ARG + MAX_FLOAT_REG_ARG];
                int      count = 0;     // Number of nodes in list
                bool     loop  = false; // List has a loop

                for (unsigned cur = i; regArg[cur] != REG_NA; count++)
                {
                    if (cur == i && count > 0)
                    {
                        loop = true;
                        break;
                    }

                    indexList[count] = cur;

                    for (int count2 = 0; count2 < count; count2++)
                    {
                        // The list could not have backlinks except last to first case which handled above.
                        assert(cur != indexList[count2] && "Attempt to move several values on same register.");
                    }
                    assert(cur < MAX_REG_ARG + MAX_FLOAT_REG_ARG);
                    assert(isValidIntArgReg(regArg[cur]) || isValidFloatArgReg(regArg[cur]));

                    if (isValidIntArgReg(regArgInit[cur]))
                    {
                        cur = regArgInit[cur] - REG_ARG_FIRST;
                    }
                    else if (isValidFloatArgReg(regArgInit[cur]))
                    {
                        cur = regArgInit[cur] - REG_ARG_FP_FIRST + MAX_REG_ARG;
                    }
                    else
                    {
                        assert(!"Argument register is neither valid float nor valid int argument register");
                    }
                }

                if (loop)
                {
                    unsigned tmpArg = indexList[count - 1];

                    GetEmitter()->emitIns_Mov(regArgAttr[tmpArg], rsGetRsvdReg(), regArg[tmpArg], false);
                    count--; // Decrease count to not access last node which regArgInit points to start node i
                    assert(count > 0);
                }

                for (int cur = count - 1; cur >= 0; cur--)
                {
                    unsigned tmpArg = indexList[cur];

                    GetEmitter()->emitIns_Mov(regArgAttr[tmpArg], regArgInit[tmpArg], regArg[tmpArg], false);

                    regArgMaskLive &= ~genRegMask(regArg[tmpArg]);
                    regArg[tmpArg] = REG_NA;
                    regArgNum--;
                    assert(regArgNum >= 0);
                };

                if (loop)
                {
                    unsigned tmpArg = indexList[count]; // count was decreased for loop case

                    GetEmitter()->emitIns_Mov(regArgAttr[i], regArg[tmpArg], rsGetRsvdReg(), false);

                    regArgMaskLive &= ~genRegMask(regArg[tmpArg]);
                    regArg[tmpArg] = REG_NA;
                    regArgNum--;
                }
                assert(regArgNum >= 0);
            }
        }
    }

    assert(regArgNum == 0);
    assert(!regArgMaskLive);
}

#ifdef PROFILING_SUPPORTED
//-----------------------------------------------------------------------------------
// genProfilingEnterCallback: Generate the profiling function enter callback.
//
// Arguments:
//     initReg        - register to use as scratch register
//     pInitRegZeroed - OUT parameter. *pInitRegZeroed set to 'false' if 'initReg' is
//                      set to non-zero value after this call.
//
void CodeGen::genProfilingEnterCallback(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    ssize_t methHnd = (ssize_t)compiler->compProfilerMethHnd;
    if (compiler->compProfilerMethHndIndirected)
    {
        instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, REG_PROFILER_ENTER_ARG_FUNC_ID, methHnd);
        GetEmitter()->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_PROFILER_ENTER_ARG_FUNC_ID, REG_PROFILER_ENTER_ARG_FUNC_ID,
                                    0);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_PROFILER_ENTER_ARG_FUNC_ID, methHnd);
    }

    ssize_t callerSPOffset = -compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
    genInstrWithConstant(INS_addi, EA_PTRSIZE, REG_PROFILER_ENTER_ARG_CALLER_SP, genFramePointerReg(), callerSPOffset,
                         REG_PROFILER_ENTER_ARG_CALLER_SP);

    genEmitHelperCall(CORINFO_HELP_PROF_FCN_ENTER, 0, EA_UNKNOWN);

    if ((genRegMask(initReg) & RBM_PROFILER_ENTER_TRASH))
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
void CodeGen::genProfilingLeaveCallback(unsigned helper /*= CORINFO_HELP_PROF_FCN_LEAVE*/)
{
    assert((helper == CORINFO_HELP_PROF_FCN_LEAVE) || (helper == CORINFO_HELP_PROF_FCN_TAILCALL));

    if (!compiler->compIsProfilerHookNeeded())
    {
        return;
    }

    compiler->info.compProfilerCallback = true;

    ssize_t methHnd = (ssize_t)compiler->compProfilerMethHnd;
    if (compiler->compProfilerMethHndIndirected)
    {
        instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, REG_PROFILER_LEAVE_ARG_FUNC_ID, methHnd);
        GetEmitter()->emitIns_R_R_I(INS_ld, EA_PTRSIZE, REG_PROFILER_LEAVE_ARG_FUNC_ID, REG_PROFILER_LEAVE_ARG_FUNC_ID,
                                    0);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, REG_PROFILER_LEAVE_ARG_FUNC_ID, methHnd);
    }

    gcInfo.gcMarkRegSetNpt(RBM_PROFILER_LEAVE_ARG_FUNC_ID);

    ssize_t callerSPOffset = -compiler->lvaToCallerSPRelativeOffset(0, isFramePointerUsed());
    genInstrWithConstant(INS_addi, EA_PTRSIZE, REG_PROFILER_LEAVE_ARG_CALLER_SP, genFramePointerReg(), callerSPOffset,
                         REG_PROFILER_LEAVE_ARG_CALLER_SP);

    gcInfo.gcMarkRegSetNpt(RBM_PROFILER_LEAVE_ARG_CALLER_SP);

    genEmitHelperCall(helper, 0, EA_UNKNOWN);
}
#endif // PROFILING_SUPPORTED

#endif // TARGET_RISCV64
