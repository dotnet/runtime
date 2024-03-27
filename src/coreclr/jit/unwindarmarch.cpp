// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              UnwindInfo                                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_ARM) && defined(FEATURE_CFI_SUPPORT)
short Compiler::mapRegNumToDwarfReg(regNumber reg)
{
    short dwarfReg = DWARF_REG_ILLEGAL;

    switch (reg)
    {
        case REG_R0:
            dwarfReg = 0;
            break;
        case REG_R1:
            dwarfReg = 1;
            break;
        case REG_R2:
            dwarfReg = 2;
            break;
        case REG_R3:
            dwarfReg = 3;
            break;
        case REG_R4:
            dwarfReg = 4;
            break;
        case REG_R5:
            dwarfReg = 5;
            break;
        case REG_R6:
            dwarfReg = 6;
            break;
        case REG_R7:
            dwarfReg = 7;
            break;
        case REG_R8:
            dwarfReg = 8;
            break;
        case REG_R9:
            dwarfReg = 9;
            break;
        case REG_R10:
            dwarfReg = 10;
            break;
        case REG_R11:
            dwarfReg = 11;
            break;
        case REG_R12:
            dwarfReg = 12;
            break;
        case REG_R13:
            dwarfReg = 13;
            break;
        case REG_R14:
            dwarfReg = 14;
            break;
        case REG_R15:
            dwarfReg = 15;
            break;
        case REG_F0:
            dwarfReg = 256;
            break;
        case REG_F2:
            dwarfReg = 257;
            break;
        case REG_F4:
            dwarfReg = 258;
            break;
        case REG_F6:
            dwarfReg = 259;
            break;
        case REG_F8:
            dwarfReg = 260;
            break;
        case REG_F10:
            dwarfReg = 261;
            break;
        case REG_F12:
            dwarfReg = 262;
            break;
        case REG_F14:
            dwarfReg = 263;
            break;
        case REG_F16:
            dwarfReg = 264;
            break;
        case REG_F18:
            dwarfReg = 265;
            break;
        case REG_F20:
            dwarfReg = 266;
            break;
        case REG_F22:
            dwarfReg = 267;
            break;
        case REG_F24:
            dwarfReg = 268;
            break;
        case REG_F26:
            dwarfReg = 269;
            break;
        case REG_F28:
            dwarfReg = 270;
            break;
        case REG_F30:
            dwarfReg = 271;
            break;
        default:
            noway_assert(!"unexpected REG_NUM");
    }

    return dwarfReg;
}
#endif // TARGET_ARM && FEATURE_CFI_SUPPORT

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind APIs                                                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

void Compiler::unwindBegProlog()
{
    assert(compGeneratingProlog);
    assert(!compGeneratingUnwindProlog);
    compGeneratingUnwindProlog = true;

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        unwindBegPrologCFI();
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    FuncInfoDsc* func = funCurrentFunc();

    // There is only one prolog for a function/funclet, and it comes first. So now is
    // a good time to initialize all the unwind data structures.

    emitLocation* startLoc;
    emitLocation* endLoc;
    unwindGetFuncLocations(func, true, &startLoc, &endLoc);

    func->uwi.InitUnwindInfo(this, startLoc, endLoc);
    func->uwi.CaptureLocation();

    func->uwiCold = NULL; // No cold data yet
}

void Compiler::unwindEndProlog()
{
    assert(compGeneratingProlog);
    assert(compGeneratingUnwindProlog);
    compGeneratingUnwindProlog = false;
}

void Compiler::unwindBegEpilog()
{
    assert(compGeneratingEpilog);
    assert(!compGeneratingUnwindEpilog);
    compGeneratingUnwindEpilog = true;

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    funCurrentFunc()->uwi.AddEpilog();
}

void Compiler::unwindEndEpilog()
{
    assert(compGeneratingEpilog);
    assert(compGeneratingUnwindEpilog);
    compGeneratingUnwindEpilog = false;
}

#if defined(TARGET_ARM)

void Compiler::unwindPushPopMaskInt(regMaskTP maskInt, bool useOpsize16)
{
    // floating point registers cannot be specified in 'maskInt'
    assert((maskInt & RBM_ALLFLOAT) == 0);

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    if (useOpsize16)
    {
        // The 16-bit opcode only encode R0-R7 and LR
        assert((maskInt & ~(RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_LR)) == 0);

        bool shortFormat = false;
        BYTE val         = 0;

        if ((maskInt & (RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3)) == 0)
        {
            regMaskTP matchMask = maskInt & (RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7);
            regMaskTP valMask   = RBM_R4;
            while (val < 4)
            {
                if (matchMask == valMask)
                {
                    shortFormat = true;
                    break;
                }

                valMask <<= 1;
                valMask |= RBM_R4;

                val++;
            }
        }

        if (shortFormat)
        {
            // D0-D7 : pop {r4-rX,lr} (X=4-7) (opsize 16)
            pu->AddCode(0xD0 | ((maskInt >> 12) & 0x4) | val);
        }
        else
        {
            // EC-ED : pop {r0-r7,lr} (opsize 16)
            pu->AddCode(0xEC | ((maskInt >> 14) & 0x1), (BYTE)maskInt);
        }
    }
    else
    {
        assert((maskInt &
                ~(RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8 | RBM_R9 | RBM_R10 |
                  RBM_R11 | RBM_R12 | RBM_LR)) == 0);

        bool shortFormat = false;
        BYTE val         = 0;

        if (((maskInt & (RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3)) == 0) &&
            ((maskInt & (RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8)) == (RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8)))
        {
            regMaskTP matchMask = maskInt & (RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8 | RBM_R9 | RBM_R10 | RBM_R11);
            regMaskTP valMask   = RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8;
            while (val < 4)
            {
                if (matchMask == valMask)
                {
                    shortFormat = true;
                    break;
                }

                valMask <<= 1;
                valMask |= RBM_R4;

                val++;
            }
        }

        if (shortFormat)
        {
            // D8-DF : pop {r4-rX,lr} (X=8-11) (opsize 32)
            pu->AddCode(0xD8 | ((maskInt >> 12) & 0x4) | val);
        }
        else
        {
            // 80-BF : pop {r0-r12,lr} (opsize 32)
            pu->AddCode(0x80 | ((maskInt >> 8) & 0x1F) | ((maskInt >> 9) & 0x20), (BYTE)maskInt);
        }
    }
}

void Compiler::unwindPushPopMaskFloat(regMaskTP maskFloat)
{
    // Only floating pointer registers can be specified in 'maskFloat'
    assert((maskFloat & ~RBM_ALLFLOAT) == 0);

    // If the maskFloat is zero there is no unwind code to emit
    //
    if (maskFloat == RBM_NONE)
    {
        return;
    }

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    BYTE      val     = 0;
    regMaskTP valMask = (RBM_F16 | RBM_F17);

    while (maskFloat != valMask)
    {
        valMask <<= 2;
        valMask |= (RBM_F16 | RBM_F17);

        val++;

        if (val == 8)
        {
            noway_assert(!"Illegal maskFloat");
        }
    }

    // E0-E7 : vpop {d8-dX} (X=8-15) (opsize 32)
    assert(0 <= val && val <= 7);
    pu->AddCode(0xE0 | val);
}

void Compiler::unwindPushMaskInt(regMaskTP maskInt)
{
    // Only r0-r12 and lr are supported
    assert((maskInt &
            ~(RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8 | RBM_R9 | RBM_R10 |
              RBM_R11 | RBM_R12 | RBM_LR)) == 0);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        // If we are pushing LR, we should give unwind codes in terms of caller's PC
        unwindPushPopMaskCFI(maskInt, false);
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    bool useOpsize16 = ((maskInt & (RBM_LOW_REGS | RBM_LR)) == maskInt); // Can PUSH use the 16-bit encoding?
    unwindPushPopMaskInt(maskInt, useOpsize16);
}

void Compiler::unwindPushMaskFloat(regMaskTP maskFloat)
{
    // Only floating point registers should be in maskFloat
    assert((maskFloat & RBM_ALLFLOAT) == maskFloat);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        unwindPushPopMaskCFI(maskFloat, true);
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    unwindPushPopMaskFloat(maskFloat);
}

void Compiler::unwindPopMaskInt(regMaskTP maskInt)
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    // Only r0-r12 and lr and pc are supported (pc is mapped to lr when encoding)
    assert((maskInt &
            ~(RBM_R0 | RBM_R1 | RBM_R2 | RBM_R3 | RBM_R4 | RBM_R5 | RBM_R6 | RBM_R7 | RBM_R8 | RBM_R9 | RBM_R10 |
              RBM_R11 | RBM_R12 | RBM_LR | RBM_PC)) == 0);

    bool useOpsize16 = ((maskInt & (RBM_LOW_REGS | RBM_PC)) == maskInt); // Can POP use the 16-bit encoding?

    // If we are popping PC, then we'll return from the function. In this case, we assume
    // the first thing the prolog did was push LR, so give the unwind codes in terms of
    // the LR that was pushed. Note that the epilog unwind codes are meant to reverse
    // the effect of the prolog. For "pop {pc}", the prolog had "push {lr}", so we need
    // an epilog code to model the reverse of that.
    if (maskInt & RBM_PC)
    {
        maskInt = (maskInt & ~RBM_PC) | RBM_LR;
    }
    unwindPushPopMaskInt(maskInt, useOpsize16);
}

void Compiler::unwindPopMaskFloat(regMaskTP maskFloat)
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    // Only floating point registers should be in maskFloat
    assert((maskFloat & RBM_ALLFLOAT) == maskFloat);
    unwindPushPopMaskFloat(maskFloat);
}

void Compiler::unwindAllocStack(unsigned size)
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            unwindAllocStackCFI(size);
        }
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    assert(size % 4 == 0);
    size /= 4;

    if (size <= 0x7F)
    {
        // 00-7F : add sp, sp, #X*4 (opsize 16)
        pu->AddCode((BYTE)size);
    }
    else if (size <= 0x3FF)
    {
        // E8-EB : addw sp, sp, #X*4 (opsize 32)
        pu->AddCode(0xE8 | (BYTE)(size >> 8), (BYTE)size);
    }
    else if (size <= 0xFFFF)
    {
        // F7 : add sp, sp, #X*4 (opsize 16)
        // F9 : add sp, sp, #X*4 (opsize 32)
        //
        // For large stack size, the most significant bits
        // are stored first (and next to the opCode (F9)) per the unwind spec.
        unsigned instrSizeInBytes = pu->GetInstructionSize();
        BYTE     b1               = (instrSizeInBytes == 2) ? 0xF7 : 0xF9;
        pu->AddCode(b1,
                    (BYTE)(size >> 8), // msb
                    (BYTE)size);       // lsb
    }
    else
    {
        // F8 : add sp, sp, #X*4 (opsize 16)
        // FA : add sp, sp, #X*4 (opsize 32)
        //
        // For large stack size, the most significant bits
        // are stored first (and next to the opCode (FA)) per the unwind spec.
        unsigned instrSizeInBytes = pu->GetInstructionSize();
        BYTE     b1               = (instrSizeInBytes == 2) ? 0xF8 : 0xFA;
        pu->AddCode(b1, (BYTE)(size >> 16), (BYTE)(size >> 8), (BYTE)size);
    }
}

void Compiler::unwindSetFrameReg(regNumber reg, unsigned offset)
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            unwindSetFrameRegCFI(reg, offset);
        }
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    // Arm unwind info does not allow offset
    assert(offset == 0);
    assert(0 <= reg && reg <= 15);

    // C0-CF : mov sp, rX (opsize 16)
    pu->AddCode((BYTE)(0xC0 + reg));
}

void Compiler::unwindSaveReg(regNumber reg, unsigned offset)
{
    unreached();
}

void Compiler::unwindBranch16()
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    // TODO-CQ: need to handle changing the exit code from 0xFF to 0xFD. Currently, this will waste an extra 0xFF at the
    // end, automatically added.
    pu->AddCode(0xFD);
}

void Compiler::unwindNop(unsigned codeSizeInBytes) // codeSizeInBytes is 2 or 4 bytes for Thumb2 instruction
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

#ifdef DEBUG
    if (verbose)
    {
        printf("unwindNop: adding NOP for %d byte instruction\n", codeSizeInBytes);
    }
#endif

    INDEBUG(pu->uwiAddingNOP = true);

    if (codeSizeInBytes == 2)
    {
        // FB : nop (opsize 16)
        pu->AddCode(0xFB);
    }
    else
    {
        noway_assert(codeSizeInBytes == 4);

        // FC : nop (opsize 32)
        pu->AddCode(0xFC);
    }

    INDEBUG(pu->uwiAddingNOP = false);
}

#endif // defined(TARGET_ARM)

// The instructions between the last captured "current state" and the current instruction
// are in the prolog but have no effect for unwinding. Emit the appropriate NOP unwind codes
// for them.
void Compiler::unwindPadding()
{
#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;
    GetEmitter()->emitUnwindNopPadding(pu->GetCurrentEmitterLocation(), this);
}

// Ask the VM to reserve space for the unwind information for the function and
// all its funclets.
void Compiler::unwindReserve()
{
    assert(!compGeneratingProlog);
    assert(!compGeneratingEpilog);

    assert(compFuncInfoCount > 0);
    for (unsigned funcIdx = 0; funcIdx < compFuncInfoCount; funcIdx++)
    {
        unwindReserveFunc(funGetFunc(funcIdx));
    }
}

void Compiler::unwindReserveFunc(FuncInfoDsc* func)
{
    BOOL isFunclet          = (func->funKind == FUNC_ROOT) ? FALSE : TRUE;
    bool funcHasColdSection = (fgFirstColdBlock != nullptr);

#ifdef DEBUG
    if (JitConfig.JitFakeProcedureSplitting() && funcHasColdSection)
    {
        funcHasColdSection = false; // "Trick" the VM into thinking we don't have a cold section.
    }
#endif // DEBUG

#ifdef FEATURE_EH_FUNCLETS
    // If hot/cold splitting occurred at fgFirstFuncletBB, then the main body is not split.
    const bool splitAtFirstFunclet = (funcHasColdSection && (fgFirstColdBlock == fgFirstFuncletBB));

    if (!isFunclet && splitAtFirstFunclet)
    {
        funcHasColdSection = false;
    }
#endif // FEATURE_EH_FUNCLETS

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        // Report zero-sized unwind info for cold part of main function
        // so the EE chains unwind info.
        // TODO: Support cold EH funclets.
        DWORD unwindCodeBytes = 0;
        if (funcHasColdSection)
        {
            eeReserveUnwindInfo(isFunclet, true /*isColdCode*/, unwindCodeBytes);
        }

        unwindCodeBytes = (DWORD)(func->cfiCodes->size() * sizeof(CFI_CODE));
        eeReserveUnwindInfo(isFunclet, false /*isColdCode*/, unwindCodeBytes);

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    // If there is cold code, split the unwind data between the hot section and the
    // cold section. This needs to be done before we split into fragments, as each
    // of the hot and cold sections can have multiple fragments.

    if (funcHasColdSection)
    {
        emitLocation* startLoc;
        emitLocation* endLoc;
        unwindGetFuncLocations(func, false, &startLoc, &endLoc);

        func->uwiCold = new (this, CMK_UnwindInfo) UnwindInfo();
        func->uwiCold->InitUnwindInfo(this, startLoc, endLoc);
        func->uwiCold->HotColdSplitCodes(&func->uwi);
    }

    // First we need to split the function or funclet into fragments that are no larger
    // than 512K, so the fragment size will fit in the unwind data "Function Length" field.
    // The ARM Exception Data specification "Function Fragments" section describes this.
    func->uwi.Split();

    // If the function is split, EH funclets are always cold; skip this call for cold funclets.
    if (!isFunclet || !funcHasColdSection)
    {
        func->uwi.Reserve(isFunclet, true);
    }

    // After the hot section, split and reserve the cold section

    if (funcHasColdSection)
    {
        assert(func->uwiCold != NULL);

        func->uwiCold->Split();
        func->uwiCold->Reserve(isFunclet, false);
    }
}

// unwindEmit: Report all the unwind information to the VM.
// Arguments:
//      pHotCode:  Pointer to the beginning of the memory with the function and funclet hot  code
//      pColdCode: Pointer to the beginning of the memory with the function and funclet cold code.

void Compiler::unwindEmit(void* pHotCode, void* pColdCode)
{
    assert(compFuncInfoCount > 0);
    for (unsigned funcIdx = 0; funcIdx < compFuncInfoCount; funcIdx++)
    {
        unwindEmitFunc(funGetFunc(funcIdx), pHotCode, pColdCode);
    }
}

void Compiler::unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode)
{
    // Verify that the JIT enum is in sync with the JIT-EE interface enum
    static_assert_no_msg(FUNC_ROOT == (FuncKind)CORJIT_FUNC_ROOT);
    static_assert_no_msg(FUNC_HANDLER == (FuncKind)CORJIT_FUNC_HANDLER);
    static_assert_no_msg(FUNC_FILTER == (FuncKind)CORJIT_FUNC_FILTER);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        // TODO: Support cold EH funclets.
        unwindEmitFuncCFI(func, pHotCode, pColdCode);
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    // If the function is split, EH funclets are always cold; skip this call for cold funclets.
    if ((func->funKind == FUNC_ROOT) || (func->uwiCold == NULL))
    {
        func->uwi.Allocate((CorJitFuncKind)func->funKind, pHotCode, pColdCode, true);
    }

    if (func->uwiCold != NULL)
    {
        func->uwiCold->Allocate((CorJitFuncKind)func->funKind, pHotCode, pColdCode, false);
    }
}

#if defined(TARGET_ARM)

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind Info Debug helpers                                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef DEBUG

// Return the opcode size of an instruction, in bytes, given the first byte of
// its corresponding unwind code.

unsigned GetOpcodeSizeFromUnwindHeader(BYTE b1)
{
    static BYTE s_UnwindOpsize[256] = {
        // array of opsizes, in bytes (as specified in the ARM unwind specification)
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 00-0F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 10-1F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 20-2F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 30-3F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 40-4F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 50-5F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 60-6F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 70-7F
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, // 80-8F
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, // 90-9F
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, // A0-AF
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, // B0-BF
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // C0-CF
        2, 2, 2, 2, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 4, // D0-DF
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 4, // E0-EF
        0, 0, 0, 0, 0, 4, 4, 2, 2, 4, 4, 2, 4, 2, 4, 0  // F0-FF
    };

    BYTE opsize = s_UnwindOpsize[b1];
    assert(opsize == 2 ||
           opsize == 4); // We shouldn't get a code with no opsize (the 0xFF end code is handled specially)
    return opsize;
}

// Return the size of the unwind code (from 1 to 4 bytes), given the first byte of the unwind bytes

unsigned GetUnwindSizeFromUnwindHeader(BYTE b1)
{
    static BYTE s_UnwindSize[256] = {
        // array of unwind sizes, in bytes (as specified in the ARM unwind specification)
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 00-0F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 10-1F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 20-2F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 30-3F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 40-4F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 50-5F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 60-6F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 70-7F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 80-8F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 90-9F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // A0-AF
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // B0-BF
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // C0-CF
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // D0-DF
        1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, // E0-EF
        1, 1, 1, 1, 1, 2, 2, 3, 4, 3, 4, 1, 1, 1, 1, 1  // F0-FF
    };

    unsigned size = s_UnwindSize[b1];
    assert(1 <= size && size <= 4);
    return size;
}

#endif // DEBUG

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind Info Support Classes                                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindCodesBase
//
///////////////////////////////////////////////////////////////////////////////

#ifdef DEBUG

// Walk the prolog codes and calculate the size of the prolog or epilog, in bytes.
// The 0xFD and 0xFE "end + NOP" codes need to be handled differently between
// the prolog and epilog. They count as pure "end" codes in a prolog, but they
// count as 16 and 32 bit NOPs (respectively), as well as an "end", in an epilog.
unsigned UnwindCodesBase::GetCodeSizeFromUnwindCodes(bool isProlog)
{
    BYTE*    pCodesStart = GetCodes();
    BYTE*    pCodes      = pCodesStart;
    unsigned size        = 0;
    for (;;)
    {
        BYTE b1 = *pCodes;
        if (b1 >= 0xFD)
        {
            // 0xFD, 0xFE, 0xFF are "end" codes

            if (!isProlog && (b1 == 0xFD || b1 == 0xFE))
            {
                // Count the special "end + NOP" code size in the epilog
                size += GetOpcodeSizeFromUnwindHeader(b1);
            }

            break; // We hit an "end" code; we're done
        }
        size += GetOpcodeSizeFromUnwindHeader(b1);
        pCodes += GetUnwindSizeFromUnwindHeader(b1);
        assert(pCodes - pCodesStart < 256); // 255 is the absolute maximum number of code bytes allowed
    }
    return size;
}

#endif // DEBUG

#endif // defined(TARGET_ARM)

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindPrologCodes
//
///////////////////////////////////////////////////////////////////////////////

// We're going to use the prolog codes memory to store the final unwind data.
// Ensure we have enough memory to store everything. If 'epilogBytes' > 0, then
// move the prolog codes so there are 'epilogBytes' bytes after the prolog codes.
// Set the header pointer for future use, adding the header bytes (this pointer
// is updated when a header byte is added), and remember the index that points
// to the beginning of the header.

void UnwindPrologCodes::SetFinalSize(int headerBytes, int epilogBytes)
{
#ifdef DEBUG
    // We're done adding codes. Check that we didn't accidentally create a bigger prolog.
    unsigned codeSize = GetCodeSizeFromUnwindCodes(true);
    assert(codeSize <= MAX_PROLOG_SIZE_BYTES);
#endif // DEBUG

    int prologBytes = Size();

    EnsureSize(headerBytes + prologBytes + epilogBytes + 3); // 3 = padding bytes for alignment

    upcUnwindBlockSlot = upcCodeSlot - headerBytes - epilogBytes; // Index of the first byte of the unwind header

    assert(upcMemSize == upcUnwindBlockSlot + headerBytes + prologBytes + epilogBytes + 3);

    upcHeaderSlot = upcUnwindBlockSlot - 1; // upcHeaderSlot is always incremented before storing
    assert(upcHeaderSlot >= -1);

    if (epilogBytes > 0)
    {
        // The prolog codes that are already at the end of the array need to get moved to the middle,
        // with space for the non-matching epilog codes to follow.

        memmove_s(&upcMem[upcUnwindBlockSlot + headerBytes], upcMemSize - (upcUnwindBlockSlot + headerBytes),
                  &upcMem[upcCodeSlot], prologBytes);

        // Note that the three UWC_END padding bytes still exist at the end of the array.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        // Zero out the epilog codes memory, to ensure we've copied the right bytes. Don't zero the padding bytes.
        memset(&upcMem[upcUnwindBlockSlot + headerBytes + prologBytes], 0, epilogBytes);
#endif // DEBUG

        upcEpilogSlot =
            upcUnwindBlockSlot + headerBytes + prologBytes; // upcEpilogSlot points to the next epilog location to fill

        // Update upcCodeSlot to point at the new beginning of the prolog codes
        upcCodeSlot = upcUnwindBlockSlot + headerBytes;
    }
}

// Add a header word. Header words are added starting at the beginning, in order: first to last.
// This is in contrast to the prolog unwind codes, which are added in reverse order.
void UnwindPrologCodes::AddHeaderWord(DWORD d)
{
    assert(-1 <= upcHeaderSlot);
    assert(upcHeaderSlot + 4 < upcCodeSlot); // Don't collide with the unwind codes that are already there!

    // Store it byte-by-byte in little-endian format. We've already ensured there is enough space
    // in SetFinalSize().
    upcMem[++upcHeaderSlot] = (BYTE)d;
    upcMem[++upcHeaderSlot] = (BYTE)(d >> 8);
    upcMem[++upcHeaderSlot] = (BYTE)(d >> 16);
    upcMem[++upcHeaderSlot] = (BYTE)(d >> 24);
}

// AppendEpilog: copy the epilog bytes to the next epilog bytes slot
void UnwindPrologCodes::AppendEpilog(UnwindEpilogInfo* pEpi)
{
    assert(upcEpilogSlot != -1);

    int epiSize = pEpi->Size();
    memcpy_s(&upcMem[upcEpilogSlot], upcMemSize - upcEpilogSlot - 3, pEpi->GetCodes(),
             epiSize); // -3 to avoid writing to the alignment padding
    assert(pEpi->GetStartIndex() ==
           upcEpilogSlot - upcCodeSlot); // Make sure we copied it where we expected to copy it.

    upcEpilogSlot += epiSize;
    assert(upcEpilogSlot <= upcMemSize - 3);
}

// GetFinalInfo: return a pointer to the final unwind info to hand to the VM, and the size of this info in bytes
void UnwindPrologCodes::GetFinalInfo(/* OUT */ BYTE** ppUnwindBlock, /* OUT */ ULONG* pUnwindBlockSize)
{
    assert(upcHeaderSlot + 1 == upcCodeSlot); // We better have filled in the header before asking for the final data!

    *ppUnwindBlock = &upcMem[upcUnwindBlockSlot];

    // We put 4 'end' codes at the end for padding, so we can ensure we have an
    // unwind block that is a multiple of 4 bytes in size. Subtract off three 'end'
    // codes (leave one), and then align the size up to a multiple of 4.
    *pUnwindBlockSize = AlignUp((UINT)(upcMemSize - upcUnwindBlockSlot - 3), sizeof(DWORD));
}

// Do the argument unwind codes match our unwind codes?
// If they don't match, return -1. If they do, return the offset into
// our codes at which they match. Note that this means that the
// argument codes can match a subset of our codes. The subset needs to be at
// the end, for the "end" code to match.
//
// This is similar to UnwindEpilogInfo::Match().
//
#if defined(TARGET_ARM)
// Note that if we wanted to handle 0xFD and 0xFE codes, by converting
// an existing 0xFF code to one of those, we might do that here.
#endif // defined(TARGET_ARM)

int UnwindPrologCodes::Match(UnwindEpilogInfo* pEpi)
{
    if (Size() < pEpi->Size())
    {
        return -1;
    }

    int matchIndex = Size() - pEpi->Size();

    if (0 == memcmp(GetCodes() + matchIndex, pEpi->GetCodes(), pEpi->Size()))
    {
        return matchIndex;
    }

    return -1;
}

// Copy the prolog codes from another prolog. The only time this is legal is
// if we are at the initial state and no prolog codes have been added.
// This is used to create the 'phantom' prolog for non-first fragments.

void UnwindPrologCodes::CopyFrom(UnwindPrologCodes* pCopyFrom)
{
    assert(uwiComp == pCopyFrom->uwiComp);
    assert(upcMem == upcMemLocal);
    assert(upcMemSize == UPC_LOCAL_COUNT);
    assert(upcHeaderSlot == -1);
    assert(upcEpilogSlot == -1);

    // Copy the codes
    EnsureSize(pCopyFrom->upcMemSize);
    assert(upcMemSize == pCopyFrom->upcMemSize);
    memcpy_s(upcMem, upcMemSize, pCopyFrom->upcMem, pCopyFrom->upcMemSize);

    // Copy the other data
    upcCodeSlot        = pCopyFrom->upcCodeSlot;
    upcHeaderSlot      = pCopyFrom->upcHeaderSlot;
    upcEpilogSlot      = pCopyFrom->upcEpilogSlot;
    upcUnwindBlockSlot = pCopyFrom->upcUnwindBlockSlot;
}

void UnwindPrologCodes::EnsureSize(int requiredSize)
{
    if (requiredSize > upcMemSize)
    {
        // Reallocate, and copy everything to a new array.

        // Choose the next power of two size. This may or may not be the best choice.
        noway_assert((requiredSize & 0xC0000000) == 0); // too big!
        int newSize;
        for (newSize = upcMemSize << 1; newSize < requiredSize; newSize <<= 1)
        {
            // do nothing
        }

        BYTE* newUnwindCodes = new (uwiComp, CMK_UnwindInfo) BYTE[newSize];
        memcpy_s(newUnwindCodes + newSize - upcMemSize, upcMemSize, upcMem,
                 upcMemSize); // copy the existing data to the end
#ifdef DEBUG
        // Clear the old unwind codes; nobody should be looking at them
        memset(upcMem, 0xFF, upcMemSize);
#endif                           // DEBUG
        upcMem = newUnwindCodes; // we don't free anything that used to be there since we have a no-release allocator
        upcCodeSlot += newSize - upcMemSize;
        upcMemSize = newSize;
    }
}

#ifdef DEBUG
void UnwindPrologCodes::Dump(int indent)
{
    printf("%*sUnwindPrologCodes @0x%08p, size:%d:\n", indent, "", dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  &upcMemLocal[0]: 0x%08p\n", indent, "", dspPtr(&upcMemLocal[0]));
    printf("%*s  upcMem: 0x%08p\n", indent, "", dspPtr(upcMem));
    printf("%*s  upcMemSize: %d\n", indent, "", upcMemSize);
    printf("%*s  upcCodeSlot: %d\n", indent, "", upcCodeSlot);
    printf("%*s  upcHeaderSlot: %d\n", indent, "", upcHeaderSlot);
    printf("%*s  upcEpilogSlot: %d\n", indent, "", upcEpilogSlot);
    printf("%*s  upcUnwindBlockSlot: %d\n", indent, "", upcUnwindBlockSlot);

    if (upcMemSize > 0)
    {
        printf("%*s  codes:", indent, "");
        for (int i = 0; i < upcMemSize; i++)
        {
            printf(" %02x", upcMem[i]);
            if (i == upcCodeSlot)
                printf(" <-C");
            else if (i == upcHeaderSlot)
                printf(" <-H");
            else if (i == upcEpilogSlot)
                printf(" <-E");
            else if (i == upcUnwindBlockSlot)
                printf(" <-U");
        }
        printf("\n");
    }
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindEpilogCodes
//
///////////////////////////////////////////////////////////////////////////////

void UnwindEpilogCodes::EnsureSize(int requiredSize)
{
    if (requiredSize > uecMemSize)
    {
        // Reallocate, and copy everything to a new array.

        // Choose the next power of two size. This may or may not be the best choice.
        noway_assert((requiredSize & 0xC0000000) == 0); // too big!
        int newSize;
        for (newSize = uecMemSize << 1; newSize < requiredSize; newSize <<= 1)
        {
            // do nothing
        }

        BYTE* newUnwindCodes = new (uwiComp, CMK_UnwindInfo) BYTE[newSize];
        memcpy_s(newUnwindCodes, newSize, uecMem, uecMemSize);
#ifdef DEBUG
        // Clear the old unwind codes; nobody should be looking at them
        memset(uecMem, 0xFF, uecMemSize);
#endif                           // DEBUG
        uecMem = newUnwindCodes; // we don't free anything that used to be there since we have a no-release allocator
        // uecCodeSlot stays the same
        uecMemSize = newSize;
    }
}

#ifdef DEBUG
void UnwindEpilogCodes::Dump(int indent)
{
    printf("%*sUnwindEpilogCodes @0x%08p, size:%d:\n", indent, "", dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  &uecMemLocal[0]: 0x%08p\n", indent, "", dspPtr(&uecMemLocal[0]));
    printf("%*s  uecMem: 0x%08p\n", indent, "", dspPtr(uecMem));
    printf("%*s  uecMemSize: %d\n", indent, "", uecMemSize);
    printf("%*s  uecCodeSlot: %d\n", indent, "", uecCodeSlot);
    printf("%*s  uecFinalized: %s\n", indent, "", dspBool(uecFinalized));

    if (uecMemSize > 0)
    {
        printf("%*s  codes:", indent, "");
        for (int i = 0; i < uecMemSize; i++)
        {
            printf(" %02x", uecMem[i]);
            if (i == uecCodeSlot)
                printf(" <-C"); // Indicate the current pointer
        }
        printf("\n");
    }
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindEpilogInfo
//
///////////////////////////////////////////////////////////////////////////////

// Do the current unwind codes match those of the argument epilog?
// If they don't match, return -1. If they do, return the offset into
// our codes at which the argument codes match. Note that this means that
// the argument codes can match a subset of our codes. The subset needs to be at
// the end, for the "end" code to match.
//
// Note that if we wanted to handle 0xFD and 0xFE codes, by converting
// an existing 0xFF code to one of those, we might do that here.

int UnwindEpilogInfo::Match(UnwindEpilogInfo* pEpi)
{
    if (Matches())
    {
        // We are already matched to someone else, and won't provide codes to the final layout
        return -1;
    }

    if (Size() < pEpi->Size())
    {
        return -1;
    }

    int matchIndex = Size() - pEpi->Size();

    if (0 == memcmp(GetCodes() + matchIndex, pEpi->GetCodes(), pEpi->Size()))
    {
        return matchIndex;
    }

    return -1;
}

void UnwindEpilogInfo::CaptureEmitLocation()
{
    noway_assert(epiEmitLocation == NULL); // This function is only called once per epilog
    epiEmitLocation = new (uwiComp, CMK_UnwindInfo) emitLocation();
    epiEmitLocation->CaptureLocation(uwiComp->GetEmitter());
}

void UnwindEpilogInfo::FinalizeOffset()
{
    epiStartOffset = epiEmitLocation->CodeOffset(uwiComp->GetEmitter());
}

#ifdef DEBUG
void UnwindEpilogInfo::Dump(int indent)
{
    printf("%*sUnwindEpilogInfo @0x%08p, size:%d:\n", indent, "", dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  epiNext: 0x%08p\n", indent, "", dspPtr(epiNext));
    printf("%*s  epiEmitLocation: 0x%08p\n", indent, "", dspPtr(epiEmitLocation));
    printf("%*s  epiStartOffset: 0x%x\n", indent, "", epiStartOffset);
    printf("%*s  epiMatches: %s\n", indent, "", dspBool(epiMatches));
    printf("%*s  epiStartIndex: %d\n", indent, "", epiStartIndex);

    epiCodes.Dump(indent + 2);
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindFragmentInfo
//
///////////////////////////////////////////////////////////////////////////////

UnwindFragmentInfo::UnwindFragmentInfo(Compiler* comp, emitLocation* emitLoc, bool hasPhantomProlog)
    : UnwindBase(comp)
    , ufiNext(NULL)
    , ufiEmitLoc(emitLoc)
    , ufiHasPhantomProlog(hasPhantomProlog)
    , ufiPrologCodes(comp)
    , ufiEpilogFirst(comp)
    , ufiEpilogList(NULL)
    , ufiEpilogLast(NULL)
    , ufiCurCodes(&ufiPrologCodes)
    , ufiSize(0)
    , ufiStartOffset(UFI_ILLEGAL_OFFSET)
{
#ifdef DEBUG
    ufiNum         = 1;
    ufiInProlog    = true;
    ufiInitialized = UFI_INITIALIZED_PATTERN;
#endif // DEBUG
}

void UnwindFragmentInfo::FinalizeOffset()
{
    if (ufiEmitLoc == NULL)
    {
        // NULL emit location means the beginning of the code. This is to handle the first fragment prolog.
        ufiStartOffset = 0;
    }
    else
    {
        ufiStartOffset = ufiEmitLoc->CodeOffset(uwiComp->GetEmitter());
    }

    for (UnwindEpilogInfo* pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        pEpi->FinalizeOffset();
    }
}

void UnwindFragmentInfo::AddEpilog()
{
    assert(ufiInitialized == UFI_INITIALIZED_PATTERN);

#ifdef DEBUG
    if (ufiInProlog)
    {
        assert(ufiEpilogList == NULL);
        ufiInProlog = false;
    }
    else
    {
        assert(ufiEpilogList != NULL);
    }
#endif // DEBUG

    // Either allocate a new epilog object, or, for the first one, use the
    // preallocated one that is a member of the UnwindFragmentInfo class.

    UnwindEpilogInfo* newepi;

    if (ufiEpilogList == NULL)
    {
        // Use the epilog that's in the class already. Be sure to initialize it!
        newepi = ufiEpilogList = &ufiEpilogFirst;
    }
    else
    {
        newepi = new (uwiComp, CMK_UnwindInfo) UnwindEpilogInfo(uwiComp);
    }

    // Put the new epilog at the end of the epilog list

    if (ufiEpilogLast != NULL)
    {
        ufiEpilogLast->epiNext = newepi;
    }

    ufiEpilogLast = newepi;

    // What is the starting code offset of the epilog? Store an emitter location
    // so we can ask the emitter later, after codegen.

    newepi->CaptureEmitLocation();

    // Put subsequent unwind codes in this new epilog

    ufiCurCodes = &newepi->epiCodes;
}

// Copy the prolog codes from the 'pCopyFrom' fragment. These prolog codes will
// become 'phantom' prolog codes in this fragment. Note that this fragment should
// not have any prolog codes currently; it is at the initial state.

void UnwindFragmentInfo::CopyPrologCodes(UnwindFragmentInfo* pCopyFrom)
{
    ufiPrologCodes.CopyFrom(&pCopyFrom->ufiPrologCodes);
#ifdef TARGET_ARM64
    ufiPrologCodes.AddCode(UWC_END_C);
#endif
}

// Split the epilog codes that currently exist in 'pSplitFrom'. The ones that represent
// epilogs that start at or after the location represented by 'emitLoc' are removed
// from 'pSplitFrom' and moved to this fragment. Note that this fragment should not have
// any epilog codes currently; it is at the initial state.

void UnwindFragmentInfo::SplitEpilogCodes(emitLocation* emitLoc, UnwindFragmentInfo* pSplitFrom)
{
    UnwindEpilogInfo* pEpiPrev;
    UnwindEpilogInfo* pEpi;

    UNATIVE_OFFSET splitOffset = emitLoc->CodeOffset(uwiComp->GetEmitter());

    for (pEpiPrev = NULL, pEpi = pSplitFrom->ufiEpilogList; pEpi != NULL; pEpiPrev = pEpi, pEpi = pEpi->epiNext)
    {
        pEpi->FinalizeOffset(); // Get the offset of the epilog from the emitter so we can compare it
        if (pEpi->GetStartOffset() >= splitOffset)
        {
            // This epilog and all following epilogs, which must be in order of increasing offsets,
            // get moved to this fragment.

            // Splice in the epilogs to this fragment. Set the head of the epilog
            // list to this epilog.
            ufiEpilogList = pEpi; // In this case, don't use 'ufiEpilogFirst'
            ufiEpilogLast = pSplitFrom->ufiEpilogLast;

            // Splice out the tail of the list from the 'pSplitFrom' epilog list
            pSplitFrom->ufiEpilogLast = pEpiPrev;
            if (pSplitFrom->ufiEpilogLast == NULL)
            {
                pSplitFrom->ufiEpilogList = NULL;
            }
            else
            {
                pSplitFrom->ufiEpilogLast->epiNext = NULL;
            }

            // No more codes should be added once we start splitting
            pSplitFrom->ufiCurCodes = NULL;
            ufiCurCodes             = NULL;

            break;
        }
    }
}

// Is this epilog at the end of an unwind fragment? Ask the emitter.
// Note that we need to know this before all code offsets are finalized,
// so we can determine whether we can omit an epilog scope word for a
// single matching epilog.

bool UnwindFragmentInfo::IsAtFragmentEnd(UnwindEpilogInfo* pEpi)
{
    return uwiComp->GetEmitter()->emitIsFuncEnd(pEpi->epiEmitLocation, (ufiNext == NULL) ? NULL : ufiNext->ufiEmitLoc);
}

// Merge the unwind codes as much as possible.
// This function is called before all offsets are final.
// Also, compute the size of the final unwind block. Store this
// and some other data for later, when we actually emit the
// unwind block.

void UnwindFragmentInfo::MergeCodes()
{
    assert(ufiInitialized == UFI_INITIALIZED_PATTERN);

    unsigned epilogCount     = 0;
    unsigned epilogCodeBytes = 0; // The total number of unwind code bytes used by epilogs that don't match the
                                  // prolog codes
    unsigned epilogIndex = ufiPrologCodes.Size(); // The "Epilog Start Index" for the next non-matching epilog codes
    UnwindEpilogInfo* pEpi;

    for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        ++epilogCount;

        pEpi->FinalizeCodes();

        // Does this epilog match the prolog?
        // NOTE: for the purpose of matching, we don't handle the 0xFD and 0xFE end codes that allow slightly unequal
        // prolog and epilog codes.

        int matchIndex;

        matchIndex = ufiPrologCodes.Match(pEpi);
        if (matchIndex != -1)
        {
            pEpi->SetMatches();
            pEpi->SetStartIndex(matchIndex); // Prolog codes start at zero, so matchIndex is exactly the start index
        }
        else
        {
            // The epilog codes don't match the prolog codes. Do they match any of the epilogs
            // we've seen so far?

            bool matched = false;
            for (UnwindEpilogInfo* pEpi2 = ufiEpilogList; pEpi2 != pEpi; pEpi2 = pEpi2->epiNext)
            {
                matchIndex = pEpi2->Match(pEpi);
                if (matchIndex != -1)
                {
                    // Use the same epilog index as the one we matched, as it has already been set.
                    pEpi->SetMatches();
                    pEpi->SetStartIndex(pEpi2->GetStartIndex() + matchIndex); // We might match somewhere inside pEpi2's
                                                                              // codes, in which case matchIndex > 0
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                pEpi->SetStartIndex(epilogIndex); // We'll copy these codes to the next available location
                epilogCodeBytes += pEpi->Size();
                epilogIndex += pEpi->Size();
            }
        }
    }

    DWORD codeBytes = ufiPrologCodes.Size() + epilogCodeBytes;
    codeBytes       = AlignUp(codeBytes, sizeof(DWORD));

    DWORD codeWords =
        codeBytes / sizeof(DWORD); // This is how many words we need to store all the unwind codes in the unwind block

    // Do we need the 2nd header word for "Extended Code Words" or "Extended Epilog Count"?

    bool needExtendedCodeWordsEpilogCount =
        (codeWords > UW_MAX_CODE_WORDS_COUNT) || (epilogCount > UW_MAX_EPILOG_COUNT);

    // How many epilog scope words do we need?

    bool     setEBit      = false;       // do we need to set the E bit?
    unsigned epilogScopes = epilogCount; // Note that this could be zero if we have no epilogs!

    if (epilogCount == 1)
    {
        assert(ufiEpilogList != NULL);
        assert(ufiEpilogList->epiNext == NULL);

        if (ufiEpilogList->Matches() && (ufiEpilogList->GetStartIndex() == 0) && // The match is with the prolog
            !needExtendedCodeWordsEpilogCount && IsAtFragmentEnd(ufiEpilogList))
        {
            epilogScopes = 0; // Don't need any epilog scope words
            setEBit      = true;
        }
    }

    DWORD headerBytes = (1                                            // Always need first header DWORD
                         + (needExtendedCodeWordsEpilogCount ? 1 : 0) // Do we need the 2nd DWORD for Extended Code
                                                                      // Words or Extended Epilog Count?
                         + epilogScopes                               // One DWORD per epilog scope, for EBit = 0
                         ) *
                        sizeof(DWORD); // convert it to bytes

    DWORD finalSize = headerBytes + codeBytes; // Size of actual unwind codes, aligned up to 4-byte words,
                                               // including end padding if necessary

    // Construct the final unwind information.

    // We re-use the memory for the prolog unwind codes to construct the full unwind data. If all the epilogs
    // match the prolog, this is easy: we just prepend the header. If there are epilog codes that don't match
    // the prolog, we still use the prolog codes memory, but it's a little more complicated, since the
    // unwind info is ordered as: (a) header, (b) prolog codes, (c) non-matching epilog codes. And, the prolog
    // codes array is filled in from end-to-beginning. So, we compute the size of memory we need, ensure we
    // have that much memory, and then copy the prolog codes to the right place, appending the non-matching
    // epilog codes and prepending the header.

    ufiPrologCodes.SetFinalSize(headerBytes, epilogCodeBytes);

    if (epilogCodeBytes != 0)
    {
        // We need to copy the epilog code bytes to their final memory location

        for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
        {
            if (!pEpi->Matches())
            {
                ufiPrologCodes.AppendEpilog(pEpi);
            }
        }
    }

    // Save some data for later

    ufiSize                             = finalSize;
    ufiSetEBit                          = setEBit;
    ufiNeedExtendedCodeWordsEpilogCount = needExtendedCodeWordsEpilogCount;
    ufiCodeWords                        = codeWords;
    ufiEpilogScopes                     = epilogScopes;
}

// Finalize: Prepare the unwind information for the VM. Compute and prepend the unwind header.

void UnwindFragmentInfo::Finalize(UNATIVE_OFFSET functionLength)
{
    assert(ufiInitialized == UFI_INITIALIZED_PATTERN);

#ifdef DEBUG
    if (0 && uwiComp->verbose)
    {
        printf("*************** Before fragment #%d finalize\n", ufiNum);
        Dump();
    }
#endif

// Compute the header

#if defined(TARGET_ARM)
    noway_assert((functionLength & 1) == 0);
    DWORD headerFunctionLength = functionLength / 2;
#elif defined(TARGET_ARM64)
    noway_assert((functionLength & 3) == 0);
    DWORD headerFunctionLength = functionLength / 4;
#endif // TARGET_ARM64

    DWORD headerVers = 0; // Version of the unwind info is zero. No other version number is currently defined.
    DWORD headerXBit = 0; // We never generate "exception data", but the VM might add some.
    DWORD headerEBit;
#if defined(TARGET_ARM)
    DWORD headerFBit = ufiHasPhantomProlog ? 1 : 0; // Is this data a fragment in the sense of the unwind data
                                                    // specification? That is, do the prolog codes represent a real
                                                    // prolog or not?
#endif                                              // defined(TARGET_ARM)
    DWORD headerEpilogCount;                        // This depends on how we set headerEBit.
    DWORD headerCodeWords;
    DWORD headerExtendedEpilogCount = 0; // This depends on how we set headerEBit.
    DWORD headerExtendedCodeWords   = 0;

    if (ufiSetEBit)
    {
        headerEBit        = 1;
        headerEpilogCount = ufiEpilogList->GetStartIndex(); // probably zero -- the start of the prolog codes!
        headerCodeWords   = ufiCodeWords;
    }
    else
    {
        headerEBit = 0;

        if (ufiNeedExtendedCodeWordsEpilogCount)
        {
            headerEpilogCount         = 0;
            headerCodeWords           = 0;
            headerExtendedEpilogCount = ufiEpilogScopes;
            headerExtendedCodeWords   = ufiCodeWords;
        }
        else
        {
            headerEpilogCount = ufiEpilogScopes;
            headerCodeWords   = ufiCodeWords;
        }
    }

    // Start writing the header

    noway_assert(headerFunctionLength <=
                 0x3FFFFU); // We create fragments to prevent this from firing, so if it hits, we have an internal error

    if ((headerEpilogCount > UW_MAX_EPILOG_COUNT) || (headerCodeWords > UW_MAX_CODE_WORDS_COUNT))
    {
        IMPL_LIMITATION("unwind data too large");
    }

#if defined(TARGET_ARM)
    DWORD header = headerFunctionLength | (headerVers << 18) | (headerXBit << 20) | (headerEBit << 21) |
                   (headerFBit << 22) | (headerEpilogCount << 23) | (headerCodeWords << 28);
#elif defined(TARGET_ARM64)
    DWORD header               = headerFunctionLength | (headerVers << 18) | (headerXBit << 20) | (headerEBit << 21) |
                   (headerEpilogCount << 22) | (headerCodeWords << 27);
#endif // defined(TARGET_ARM64)

    ufiPrologCodes.AddHeaderWord(header);

    // Construct the second header word, if needed

    if (ufiNeedExtendedCodeWordsEpilogCount)
    {
        noway_assert(headerEBit == 0);
        noway_assert(headerEpilogCount == 0);
        noway_assert(headerCodeWords == 0);
        noway_assert((headerExtendedEpilogCount > UW_MAX_EPILOG_COUNT) ||
                     (headerExtendedCodeWords > UW_MAX_CODE_WORDS_COUNT));

        if ((headerExtendedEpilogCount > UW_MAX_EXTENDED_EPILOG_COUNT) ||
            (headerExtendedCodeWords > UW_MAX_EXTENDED_CODE_WORDS_COUNT))
        {
            IMPL_LIMITATION("unwind data too large");
        }

        DWORD header2 = headerExtendedEpilogCount | (headerExtendedCodeWords << 16);

        ufiPrologCodes.AddHeaderWord(header2);
    }

    // Construct the epilog scope words, if needed

    if (!ufiSetEBit)
    {
        for (UnwindEpilogInfo* pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
        {
#if defined(TARGET_ARM)
            DWORD headerCondition = 0xE; // The epilog is unconditional. We don't have epilogs under the IT instruction.
#endif                                   // defined(TARGET_ARM)

            // The epilog must strictly follow the prolog. The prolog is in the first fragment of
            // the hot section. If this epilog is at the start of a fragment, it can't be the
            // first fragment in the hot section. We actually don't know if we're processing
            // the hot or cold section (or a funclet), so we can't distinguish these cases. Thus,
            // we just assert that the epilog starts within the fragment.
            assert(pEpi->GetStartOffset() >= GetStartOffset());

            // We report the offset of an epilog as the offset from the beginning of the function/funclet fragment,
            // NOT the offset from the beginning of the main function.
            DWORD headerEpilogStartOffset = pEpi->GetStartOffset() - GetStartOffset();

#if defined(TARGET_ARM)
            noway_assert((headerEpilogStartOffset & 1) == 0);
            headerEpilogStartOffset /= 2; // The unwind data stores the actual offset divided by 2 (since the low bit of
                                          // the actual offset is always zero)
#elif defined(TARGET_ARM64)
            noway_assert((headerEpilogStartOffset & 3) == 0);
            headerEpilogStartOffset /= 4; // The unwind data stores the actual offset divided by 4 (since the low 2 bits
                                          // of the actual offset is always zero)
#endif // defined(TARGET_ARM64)

            DWORD headerEpilogStartIndex = pEpi->GetStartIndex();

            if ((headerEpilogStartOffset > UW_MAX_EPILOG_START_OFFSET) ||
                (headerEpilogStartIndex > UW_MAX_EPILOG_START_INDEX))
            {
                IMPL_LIMITATION("unwind data too large");
            }

#if defined(TARGET_ARM)
            DWORD epilogScopeWord = headerEpilogStartOffset | (headerCondition << 20) | (headerEpilogStartIndex << 24);
#elif defined(TARGET_ARM64)
            DWORD epilogScopeWord = headerEpilogStartOffset | (headerEpilogStartIndex << 22);
#endif // defined(TARGET_ARM64)

            ufiPrologCodes.AddHeaderWord(epilogScopeWord);
        }
    }

    // The unwind code words are already here, following the header, so we're done!
}

void UnwindFragmentInfo::Reserve(bool isFunclet, bool isHotCode)
{
    MergeCodes();

    bool isColdCode = !isHotCode;

    ULONG unwindSize = Size();

#ifdef DEBUG
    if (uwiComp->verbose)
    {
        if (ufiNum != 1)
            printf("reserveUnwindInfo: fragment #%d:\n", ufiNum);
    }
#endif

    uwiComp->eeReserveUnwindInfo(isFunclet, isColdCode, unwindSize);
}

// Allocate the unwind info for a fragment with the VM.
// Arguments:
//      funKind:       funclet kind
//      pHotCode:      hot section code buffer
//      pColdCode:     cold section code buffer
//      funcEndOffset: offset of the end of this function/funclet. Used if this fragment is the last one for a
//                     function/funclet.
//      isHotCode:     are we allocating the unwind info for the hot code section?

void UnwindFragmentInfo::Allocate(
    CorJitFuncKind funKind, void* pHotCode, void* pColdCode, UNATIVE_OFFSET funcEndOffset, bool isHotCode)
{
    UNATIVE_OFFSET startOffset;
    UNATIVE_OFFSET endOffset;
    UNATIVE_OFFSET codeSize;

    // Compute the final size, and start and end offsets of the fragment

    startOffset = GetStartOffset();

    if (ufiNext == NULL)
    {
        // This is the last fragment, so the fragment extends to the end of the function/fragment.
        assert(funcEndOffset != 0);
        endOffset = funcEndOffset;
    }
    else
    {
        // The fragment length is all the code between the beginning of this fragment
        // and the beginning of the next fragment. Note that all fragments have had their
        // offsets computed before any fragment is allocated.
        endOffset = ufiNext->GetStartOffset();
    }

    assert(endOffset > startOffset);
    codeSize = endOffset - startOffset;

    // Finalize the fragment unwind block to hand to the VM

    Finalize(codeSize);

    // Get the final unwind information and hand it to the VM

    ULONG unwindBlockSize;
    BYTE* pUnwindBlock;

    GetFinalInfo(&pUnwindBlock, &unwindBlockSize);

#ifdef DEBUG
    if (uwiComp->opts.dspUnwind)
    {
        DumpUnwindInfo(uwiComp, isHotCode, startOffset, endOffset, pUnwindBlock, unwindBlockSize);
    }
#endif // DEBUG

    // Adjust for cold or hot code:
    // 1. The VM doesn't want the cold code pointer unless this is cold code.
    // 2. The startOffset and endOffset need to be from the base of the hot section for hot code
    //    and from the base of the cold section for cold code

    if (isHotCode)
    {
#ifdef DEBUG
        if (JitConfig.JitFakeProcedureSplitting() && (pColdCode != NULL))
        {
            assert(endOffset <= uwiComp->info.compNativeCodeSize);
        }
        else
#endif // DEBUG
        {
            assert(endOffset <= uwiComp->info.compTotalHotCodeSize);
        }

        pColdCode = NULL;
    }
    else
    {
        assert(startOffset >= uwiComp->info.compTotalHotCodeSize);
        startOffset -= uwiComp->info.compTotalHotCodeSize;
        endOffset -= uwiComp->info.compTotalHotCodeSize;
    }

#ifdef DEBUG
    if (uwiComp->verbose)
    {
        if (ufiNum != 1)
            printf("unwindEmit: fragment #%d:\n", ufiNum);
    }
#endif // DEBUG

    uwiComp->eeAllocUnwindInfo((BYTE*)pHotCode, (BYTE*)pColdCode, startOffset, endOffset, unwindBlockSize, pUnwindBlock,
                               funKind);
}

#ifdef DEBUG
void UnwindFragmentInfo::Dump(int indent)
{
    unsigned          count;
    UnwindEpilogInfo* pEpi;

    count = 0;
    for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        ++count;
    }

    printf("%*sUnwindFragmentInfo #%d, @0x%08p, size:%d:\n", indent, "", ufiNum, dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  ufiNext: 0x%08p\n", indent, "", dspPtr(ufiNext));
    printf("%*s  ufiEmitLoc: 0x%08p ", indent, "", dspPtr(ufiEmitLoc));
    if (ufiEmitLoc != nullptr)
    {
        ufiEmitLoc->Print(uwiComp->compMethodID);
    }
    printf("\n");
    printf("%*s  ufiHasPhantomProlog: %s\n", indent, "", dspBool(ufiHasPhantomProlog));
    printf("%*s  %d epilog%s\n", indent, "", count, (count != 1) ? "s" : "");
    printf("%*s  ufiEpilogList: 0x%08p\n", indent, "", dspPtr(ufiEpilogList));
    printf("%*s  ufiEpilogLast: 0x%08p\n", indent, "", dspPtr(ufiEpilogLast));
    printf("%*s  ufiCurCodes: 0x%08p\n", indent, "", dspPtr(ufiCurCodes));
    printf("%*s  ufiSize: %u\n", indent, "", ufiSize);
    printf("%*s  ufiSetEBit: %s\n", indent, "", dspBool(ufiSetEBit));
    printf("%*s  ufiNeedExtendedCodeWordsEpilogCount: %s\n", indent, "", dspBool(ufiNeedExtendedCodeWordsEpilogCount));
    printf("%*s  ufiCodeWords: %u\n", indent, "", ufiCodeWords);
    printf("%*s  ufiEpilogScopes: %u\n", indent, "", ufiEpilogScopes);
    printf("%*s  ufiStartOffset: 0x%x\n", indent, "", ufiStartOffset);
    printf("%*s  ufiInProlog: %s\n", indent, "", dspBool(ufiInProlog));
    printf("%*s  ufiInitialized: 0x%08x\n", indent, "", ufiInitialized);

    ufiPrologCodes.Dump(indent + 2);

    for (pEpi = ufiEpilogList; pEpi != NULL; pEpi = pEpi->epiNext)
    {
        pEpi->Dump(indent + 2);
    }
}
#endif // DEBUG

///////////////////////////////////////////////////////////////////////////////
//
//  UnwindInfo
//
///////////////////////////////////////////////////////////////////////////////

void UnwindInfo::InitUnwindInfo(Compiler* comp, emitLocation* startLoc, emitLocation* endLoc)
{
    uwiComp = comp;

    // The first fragment is a member of UnwindInfo, so it doesn't need to be allocated.
    // However, its constructor needs to be explicitly called, since the constructor for
    // UnwindInfo is not called.

    new (&uwiFragmentFirst, jitstd::placement_t()) UnwindFragmentInfo(comp, startLoc, false);

    uwiFragmentLast = &uwiFragmentFirst;

    uwiEndLoc = endLoc;

    // Allocate an emitter location object. It is initialized to something
    // invalid: it has a null 'ig' that needs to get set before it can be used.
    // Note that when we create an UnwindInfo for the cold section, this never
    // gets initialized with anything useful, since we never add unwind codes
    // to the cold section; we simply distribute the existing (previously added) codes.
    uwiCurLoc = new (uwiComp, CMK_UnwindInfo) emitLocation();

#ifdef DEBUG
    uwiInitialized = UWI_INITIALIZED_PATTERN;
    uwiAddingNOP   = false;
#endif // DEBUG
}

// Split the unwind codes in 'puwi' into those that are in the hot section (leave them in 'puwi')
// and those that are in the cold section (move them to 'this'). There is exactly one fragment
// in each UnwindInfo; the fragments haven't been split for size, yet.

void UnwindInfo::HotColdSplitCodes(UnwindInfo* puwi)
{
    // Ensure that there is exactly a single fragment in both the hot and the cold sections
    assert(&uwiFragmentFirst == uwiFragmentLast);
    assert(&puwi->uwiFragmentFirst == puwi->uwiFragmentLast);
    assert(uwiFragmentLast->ufiNext == NULL);
    assert(puwi->uwiFragmentLast->ufiNext == NULL);

    // The real prolog is in the hot section, so this, cold, section has a phantom prolog
    uwiFragmentLast->ufiHasPhantomProlog = true;
    uwiFragmentLast->CopyPrologCodes(puwi->uwiFragmentLast);

    // Now split the epilog codes
    uwiFragmentLast->SplitEpilogCodes(uwiFragmentLast->ufiEmitLoc, puwi->uwiFragmentLast);
}

// Split the function or funclet into fragments that are no larger than 512K,
// so the fragment size will fit in the unwind data "Function Length" field.
// The ARM Exception Data specification "Function Fragments" section describes this.
// We split the function so that it is no larger than 512K bytes, or the value of
// the DOTNET_JitSplitFunctionSize value, if defined (and smaller). We must determine
// how to split the function/funclet before we issue the instructions, so we can
// reserve the unwind space with the VM. The instructions issued may shrink (but not
// expand!) during issuing (although this is extremely rare in any case, and may not
// actually occur on ARM), so we don't finalize actual sizes or offsets.
//
// ARM64 has very similar limitations, except functions can be up to 1MB. TODO-ARM64-Bug?: make sure this works!
//
// We don't split any prolog or epilog. Ideally, we might not split an instruction,
// although that doesn't matter because the unwind at any point would still be
// well-defined.

void UnwindInfo::Split()
{
    UNATIVE_OFFSET maxFragmentSize; // The maximum size of a code fragment in bytes

    maxFragmentSize = UW_MAX_FRAGMENT_SIZE_BYTES;

#ifdef DEBUG
    // Consider DOTNET_JitSplitFunctionSize
    unsigned splitFunctionSize = (unsigned)JitConfig.JitSplitFunctionSize();
    if (splitFunctionSize == 0)
    {
        // If the split configuration is not set, then sometimes set it during stress.
        // Use two stress modes: a split size of 4 (extreme) and a split size of 200 (reasonable).
        if (uwiComp->compStressCompile(Compiler::STRESS_UNWIND, 10))
        {
            if (uwiComp->compStressCompile(Compiler::STRESS_UNWIND, 5))
            {
                splitFunctionSize = 4;
            }
            else
            {
                splitFunctionSize = 200;
            }
        }
    }

    if (splitFunctionSize != 0)
        if (splitFunctionSize < maxFragmentSize)
            maxFragmentSize = splitFunctionSize;
#endif // DEBUG

    // Now, there should be exactly one fragment.

    assert(uwiFragmentLast != NULL);
    assert(uwiFragmentLast == &uwiFragmentFirst);
    assert(uwiFragmentLast->ufiNext == NULL);

    // Find the code size of this function/funclet.

    UNATIVE_OFFSET startOffset;
    UNATIVE_OFFSET endOffset;
    UNATIVE_OFFSET codeSize;

    if (uwiFragmentLast->ufiEmitLoc == NULL)
    {
        // NULL emit location means the beginning of the code. This is to handle the first fragment prolog.
        startOffset = 0;
    }
    else
    {
        startOffset = uwiFragmentLast->ufiEmitLoc->CodeOffset(uwiComp->GetEmitter());
    }

    if (uwiEndLoc == NULL)
    {
        // Note that compTotalHotCodeSize and compTotalColdCodeSize are computed before issuing instructions
        // from the emitter instruction group offsets, and will be accurate unless the issued code shrinks.
        // compNativeCodeSize is precise, but is only set after instructions are issued, which is too late
        // for us, since we need to decide how many fragments we need before the code memory is allocated
        // (which is before instruction issuing).
        UNATIVE_OFFSET estimatedTotalCodeSize =
            uwiComp->info.compTotalHotCodeSize + uwiComp->info.compTotalColdCodeSize;
        assert(estimatedTotalCodeSize != 0);
        endOffset = estimatedTotalCodeSize;
    }
    else
    {
        endOffset = uwiEndLoc->CodeOffset(uwiComp->GetEmitter());
    }

    assert(endOffset > startOffset); // there better be at least 1 byte of code
    codeSize = endOffset - startOffset;

    // Now that we know the code size for this section (main function hot or cold, or funclet),
    // figure out how many fragments we're going to need.

    UNATIVE_OFFSET numberOfFragments = (codeSize + maxFragmentSize - 1) / maxFragmentSize; // round up
    assert(numberOfFragments > 0);

    if (numberOfFragments == 1)
    {
        // No need to split; we're done
        return;
    }

    // Now, we're going to commit to splitting the function into "numberOfFragments" fragments,
    // for the purpose of unwind information. We need to do the actual splits so we can figure out
    // the size of each piece of unwind data for the call to reserveUnwindInfo(). We won't know
    // the actual offsets of the splits since we haven't issued the instructions yet, so store
    // an emitter location instead of an offset, and "finalize" the offset in the unwindEmit() phase,
    // like we do for the function length and epilog offsets.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    if (uwiComp->verbose)
    {
        printf("Split unwind info into %d fragments (function/funclet size: %d, maximum fragment size: %d)\n",
               numberOfFragments, codeSize, maxFragmentSize);
    }
#endif // DEBUG

    // Call the emitter to do the split, and call us back for every split point it chooses.
    uwiComp->GetEmitter()->emitSplit(uwiFragmentLast->ufiEmitLoc, uwiEndLoc, maxFragmentSize, (void*)this,
                                     EmitSplitCallback);

#ifdef DEBUG
    // Did the emitter split the function/funclet into as many fragments as we asked for?
    // It might be fewer if the DOTNET_JitSplitFunctionSize was used, but it better not
    // be fewer if we're splitting into 512K blocks!

    unsigned fragCount = 0;
    for (UnwindFragmentInfo* pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        ++fragCount;
    }
    if (fragCount < numberOfFragments)
    {
        if (uwiComp->verbose)
        {
            printf("WARNING: asked the emitter for %d fragments, but only got %d\n", numberOfFragments, fragCount);
        }

        // If this fires, then we split into fewer fragments than we asked for, and we are using
        // the default, unwind-data-defined 512K maximum fragment size. We won't be able to fit
        // this fragment into the unwind data! If you set DOTNET_JitSplitFunctionSize to something
        // small, we might not be able to split into as many fragments as asked for, because we
        // can't split prologs or epilogs.
        assert(maxFragmentSize != UW_MAX_FRAGMENT_SIZE_BYTES);
    }
#endif // DEBUG
}

/*static*/ void UnwindInfo::EmitSplitCallback(void* context, emitLocation* emitLoc)
{
    UnwindInfo* puwi = (UnwindInfo*)context;
    puwi->AddFragment(emitLoc);
}

// Reserve space for the unwind info for all fragments

void UnwindInfo::Reserve(bool isFunclet, bool isHotCode)
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);

    for (UnwindFragmentInfo* pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->Reserve(isFunclet, isHotCode);
    }
}

// Allocate and populate VM unwind info for all fragments

void UnwindInfo::Allocate(CorJitFuncKind funKind, void* pHotCode, void* pColdCode, bool isHotCode)
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);

    UnwindFragmentInfo* pFrag;

    // First, finalize all the offsets (the location of the beginning of fragments, and epilogs),
    // so a fragment can use the finalized offset of the subsequent fragment to determine its code size.

    UNATIVE_OFFSET endOffset;

    if (uwiEndLoc == NULL)
    {
        assert(uwiComp->info.compNativeCodeSize != 0);
        endOffset = uwiComp->info.compNativeCodeSize;
    }
    else
    {
        endOffset = uwiEndLoc->CodeOffset(uwiComp->GetEmitter());
    }

    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->FinalizeOffset();
    }

    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->Allocate(funKind, pHotCode, pColdCode, endOffset, isHotCode);
    }
}

void UnwindInfo::AddEpilog()
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    assert(uwiFragmentLast != NULL);
    uwiFragmentLast->AddEpilog();
    CaptureLocation();
}

#if defined(TARGET_ARM)

unsigned UnwindInfo::GetInstructionSize()
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    return uwiComp->GetEmitter()->emitGetInstructionSize(uwiCurLoc);
}

#endif // defined(TARGET_ARM)

void UnwindInfo::CaptureLocation()
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    assert(uwiCurLoc != NULL);
    uwiCurLoc->CaptureLocation(uwiComp->GetEmitter());
}

void UnwindInfo::AddFragment(emitLocation* emitLoc)
{
    assert(uwiInitialized == UWI_INITIALIZED_PATTERN);
    assert(uwiFragmentLast != NULL);

    UnwindFragmentInfo* newFrag = new (uwiComp, CMK_UnwindInfo) UnwindFragmentInfo(uwiComp, emitLoc, true);

#ifdef DEBUG
    newFrag->ufiNum = uwiFragmentLast->ufiNum + 1;
#endif // DEBUG

    newFrag->CopyPrologCodes(&uwiFragmentFirst);
    newFrag->SplitEpilogCodes(emitLoc, uwiFragmentLast);

    // Link the new fragment in at the end of the fragment list
    uwiFragmentLast->ufiNext = newFrag;
    uwiFragmentLast          = newFrag;
}

#ifdef DEBUG

#if defined(TARGET_ARM)

// Given the first byte of the unwind code, check that its opsize matches
// the last instruction added in the emitter.
void UnwindInfo::CheckOpsize(BYTE b1)
{
    // Adding NOP padding goes through the same path, but doesn't update the location to indicate
    // the correct location of the instruction for which we are adding a NOP, so just skip the
    // assert. Should be ok, because the emitter is telling us the size of the instruction for
    // which we are adding the NOP.
    if (uwiAddingNOP)
        return;

    unsigned opsizeInBytes    = GetOpcodeSizeFromUnwindHeader(b1);
    unsigned instrSizeInBytes = GetInstructionSize();
    assert(opsizeInBytes == instrSizeInBytes);
}

#endif // defined(TARGET_ARM)

void UnwindInfo::Dump(bool isHotCode, int indent)
{
    unsigned            count;
    UnwindFragmentInfo* pFrag;

    count = 0;
    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        ++count;
    }

    printf("%*sUnwindInfo %s@0x%08p, size:%d:\n", indent, "", isHotCode ? "" : "COLD ", dspPtr(this), sizeof(*this));
    printf("%*s  uwiComp: 0x%08p\n", indent, "", dspPtr(uwiComp));
    printf("%*s  %d fragment%s\n", indent, "", count, (count != 1) ? "s" : "");
    printf("%*s  uwiFragmentLast: 0x%08p\n", indent, "", dspPtr(uwiFragmentLast));
    printf("%*s  uwiEndLoc: 0x%08p\n", indent, "", dspPtr(uwiEndLoc));
    printf("%*s  uwiInitialized: 0x%08x\n", indent, "", uwiInitialized);

    for (pFrag = &uwiFragmentFirst; pFrag != NULL; pFrag = pFrag->ufiNext)
    {
        pFrag->Dump(indent + 2);
    }
}

#endif // DEBUG

#if defined(TARGET_ARM)

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Debug dumpers                                                            XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef DEBUG

// start is 0-based index from LSB, length is number of bits
DWORD ExtractBits(DWORD dw, DWORD start, DWORD length)
{
    return (dw >> start) & ((1 << length) - 1);
}

// Dump an integer register set. 'x' is an array of bits where bit 0 = r0, bit 1 = r1, etc.
// The highest register considered is r12.
// If 'lr' is non-zero, the "lr" register is emitted last.
// Returns the number of characters printed.
DWORD DumpIntRegSet(DWORD x, DWORD lr)
{
    assert(x != 0 || lr != 0); // we must have one
    assert((x & 0xE000) == 0); // don't handle r13 (sp), r14 (lr), r15 (pc) in 'x'
    DWORD printed = 0;

    printf("{");
    ++printed;
    bool  first   = true;
    DWORD bitMask = 1;
    for (DWORD bitNum = 0; bitNum < 12; bitNum++)
    {
        if (x & bitMask)
        {
            if (!first)
            {
                printf(",");
                ++printed;
            }
            printf("r%u", bitNum);
            printed += (bitNum < 10) ? 2 : 3;
            first = false;
        }
        bitMask <<= 1;
    }
    if (lr)
    {
        if (!first)
        {
            printf(",");
            ++printed;
        }
        printf("lr");
        printed += 2;
    }
    printf("}");
    ++printed;

    return printed;
}

// Dump a register set range from register 'start' to register 'end'.
// rtype should be "r" or "d" to indicate register type.
// If 'lr' is non-zero, the "lr" register is emitted last. (Note that
// 'lr' should be zero for rtype == "d".)
// Returns the number of characters printed.
DWORD DumpRegSetRange(const char* const rtype, DWORD start, DWORD end, DWORD lr)
{
    assert(start <= end);
    DWORD printed  = 0;
    DWORD rtypeLen = (DWORD)strlen(rtype);

    printf("{");
    ++printed;
    bool first = true;
    for (DWORD reg = start; reg <= end; reg++)
    {
        if (!first)
        {
            printf(",");
            ++printed;
        }
        printf("%s%u", rtype, reg);
        printed += rtypeLen + ((reg < 10) ? 1 : 2);
        first = false;
    }
    if (lr)
    {
        assert(!first); // If 'lr' is set, it can't be first, since we require a non-empty range
        printf(",lr");
        printed += 3;
    }
    printf("}");
    ++printed;

    return printed;
}

// Dump the opsize.
// Returns the number of characters printed.
DWORD DumpOpsize(DWORD padding, DWORD opsize)
{
    if (padding > 100) // underflow?
        padding   = 4;
    DWORD printed = padding;
    for (; padding > 0; padding--)
        printf(" ");
    printf("; opsize %d\n", opsize);
    return printed + 11; // assumes opsize is always 2 digits
}

// Dump the unwind data.
// Arguments:
//      isHotCode:          true if this unwind data is for the hot section
//      startOffset:        byte offset of the code start that this unwind data represents
//      endOffset:          byte offset of the code end   that this unwind data represents
//      pHeader:            pointer to the unwind data blob
//      unwindBlockSize:    size in bytes of the unwind data blob

void DumpUnwindInfo(Compiler*         comp,
                    bool              isHotCode,
                    UNATIVE_OFFSET    startOffset,
                    UNATIVE_OFFSET    endOffset,
                    const BYTE* const pHeader,
                    ULONG             unwindBlockSize)
{
    printf("Unwind Info%s:\n", isHotCode ? "" : " COLD");

    // pHeader is not guaranteed to be aligned. We put four 0xFF end codes at the end
    // to provide padding, and round down to get a multiple of 4 bytes in size.
    DWORD UNALIGNED* pdw = (DWORD UNALIGNED*)pHeader;
    DWORD dw;

    dw = *pdw++;

    DWORD codeWords      = ExtractBits(dw, 28, 4);
    DWORD epilogCount    = ExtractBits(dw, 23, 5);
    DWORD FBit           = ExtractBits(dw, 22, 1);
    DWORD EBit           = ExtractBits(dw, 21, 1);
    DWORD XBit           = ExtractBits(dw, 20, 1);
    DWORD Vers           = ExtractBits(dw, 18, 2);
    DWORD functionLength = ExtractBits(dw, 0, 18);

    printf("  >> Start offset   : 0x%06x (not in unwind data)\n", comp->dspOffset(startOffset));
    printf("  >>   End offset   : 0x%06x (not in unwind data)\n", comp->dspOffset(endOffset));
    printf("  Code Words        : %u\n", codeWords);
    printf("  Epilog Count      : %u\n", epilogCount);
    printf("  F bit             : %u\n", FBit);
    printf("  E bit             : %u\n", EBit);
    printf("  X bit             : %u\n", XBit);
    printf("  Vers              : %u\n", Vers);
    printf("  Function Length   : %u (0x%05x) Actual length = %u (0x%06x)\n", functionLength, functionLength,
           functionLength * 2, functionLength * 2);

    assert(functionLength * 2 == endOffset - startOffset);

    if (codeWords == 0 && epilogCount == 0)
    {
        // We have an extension word specifying a larger number of Code Words or Epilog Counts
        // than can be specified in the header word.

        dw = *pdw++;

        codeWords   = ExtractBits(dw, 16, 8);
        epilogCount = ExtractBits(dw, 0, 16);
        assert((dw & 0xF0000000) == 0); // reserved field should be zero

        printf("  ---- Extension word ----\n");
        printf("  Extended Code Words        : %u\n", codeWords);
        printf("  Extended Epilog Count      : %u\n", epilogCount);
    }

    bool epilogStartAt[256] = {}; // One byte per possible epilog start index; initialized to false

    if (EBit == 0)
    {
        // We have an array of epilog scopes

        printf("  ---- Epilog scopes ----\n");
        if (epilogCount == 0)
        {
            printf("  No epilogs\n");
        }
        else
        {
            for (DWORD scope = 0; scope < epilogCount; scope++)
            {
                dw = *pdw++;

                DWORD epilogStartOffset = ExtractBits(dw, 0, 18);
                DWORD res               = ExtractBits(dw, 18, 2);
                DWORD condition         = ExtractBits(dw, 20, 4);
                DWORD epilogStartIndex  = ExtractBits(dw, 24, 8);

                // Note that epilogStartOffset for a funclet is the offset from the beginning
                // of the current funclet, not the offset from the beginning of the main function.
                // To help find it when looking through JitDump output, also show the offset from
                // the beginning of the main function.
                DWORD epilogStartOffsetFromMainFunctionBegin = epilogStartOffset * 2 + startOffset;

                assert(res == 0);

                printf("  ---- Scope %d\n", scope);
                printf("  Epilog Start Offset        : %u (0x%05x) Actual offset = %u (0x%06x) Offset from main "
                       "function begin = %u (0x%06x)\n",
                       comp->dspOffset(epilogStartOffset), comp->dspOffset(epilogStartOffset),
                       comp->dspOffset(epilogStartOffset * 2), comp->dspOffset(epilogStartOffset * 2),
                       comp->dspOffset(epilogStartOffsetFromMainFunctionBegin),
                       comp->dspOffset(epilogStartOffsetFromMainFunctionBegin));
                printf("  Condition                  : %u (0x%x)%s\n", condition, condition,
                       (condition == 0xE) ? " (always)" : "");
                printf("  Epilog Start Index         : %u (0x%02x)\n", epilogStartIndex, epilogStartIndex);

                epilogStartAt[epilogStartIndex] = true; // an epilog starts at this offset in the unwind codes
            }
        }
    }
    else
    {
        printf("  --- One epilog, unwind codes at %u\n", epilogCount);
        assert(epilogCount < ArrLen(epilogStartAt));
        epilogStartAt[epilogCount] = true; // the one and only epilog starts its unwind codes at this offset
    }

    if (FBit)
    {
        printf("  ---- Note: 'F' bit is set. Prolog codes are for a 'phantom' prolog.\n");
    }

    // Dump the unwind codes

    printf("  ---- Unwind codes ----\n");

    DWORD countOfUnwindCodes = codeWords * 4;
    PBYTE pUnwindCode        = (PBYTE)pdw;
    BYTE  b1, b2, b3, b4;
    DWORD x, y;
    DWORD opsize;
    DWORD opCol = 52;
    DWORD printed;
    for (DWORD i = 0; i < countOfUnwindCodes; i++)
    {
        // Does this byte start an epilog sequence? If so, note that fact.
        if (epilogStartAt[i])
        {
            printf("    ---- Epilog start at index %u ----\n", i);
        }

        b1 = *pUnwindCode++;

        if ((b1 & 0x80) == 0)
        {
            // 00-7F : add sp, sp, #X*4 (opsize 16)
            x = b1 & 0x7F;
            printf("    %02X          add sp, sp, #%-8d", b1, x * 4);
            DumpOpsize(opCol - 37, 16);
        }
        else if ((b1 & 0xC0) == 0x80)
        {
            // 80-BF : pop {r0-r12,lr} (X = bitmask) (opsize 32)
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            DWORD LBit = ExtractBits(b1, 5, 1);
            x          = ((DWORD)(b1 & 0x1F) << 8) | (DWORD)b2;

            printf("    %02X %02X       pop ", b1, b2);
            printed = 20;
            printed += DumpIntRegSet(x, LBit);
            DumpOpsize(opCol - printed, 32);
        }
        else if ((b1 & 0xF0) == 0xC0)
        {
            // C0-CF : mov sp, rX (X=0-15) (opsize 16)
            x = b1 & 0xF;
            printf("    %02X          mov sp, r%u", b1, x);
            printed = 25 + ((x > 10) ? 2 : 1);
            DumpOpsize(opCol - printed, 16);
        }
        else if ((b1 & 0xF8) == 0xD0)
        {
            // D0-D7 : pop {r4-rX,lr} (X=4-7) (opsize 16)
            x          = b1 & 0x3;
            DWORD LBit = b1 & 0x4;
            printf("    %02X          pop ", b1);
            printed = 20;
            printed += DumpRegSetRange("r", 4, x + 4, LBit);
            DumpOpsize(opCol - printed, 16);
        }
        else if ((b1 & 0xF8) == 0xD8)
        {
            // D8-DF : pop {r4-rX,lr} (X=8-11) (opsize 32)
            x          = b1 & 0x3;
            DWORD LBit = b1 & 0x4;
            printf("    %02X          pop ", b1);
            printed = 20;
            printed += DumpRegSetRange("r", 4, x + 8, LBit);
            DumpOpsize(opCol - printed, 32);
        }
        else if ((b1 & 0xF8) == 0xE0)
        {
            // E0-E7 : vpop {d8-dX} (X=8-15) (opsize 32)
            x = b1 & 0x7;
            printf("    %02X          vpop ", b1);
            printed = 21;
            printed += DumpRegSetRange("d", 8, x + 8, 0);
            DumpOpsize(opCol - printed, 32);
        }
        else if ((b1 & 0xFC) == 0xE8)
        {
            // E8-EB : addw sp, sp, #X*4 (opsize 32)
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x3) << 8) | (DWORD)b2;

            printf("    %02X %02X       addw sp, sp, #%-8u", b1, b2, x * 4);
            DumpOpsize(opCol - 38, 32);
        }
        else if ((b1 & 0xFE) == 0xEC)
        {
            // EC-ED : pop {r0-r7,lr} (X = bitmask) (opsize 16)
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            DWORD LBit = ExtractBits(b1, 0, 1);
            x          = (DWORD)b2;

            printf("    %02X %02X       pop ", b1, b2);
            printed = 20;
            printed += DumpIntRegSet(x, LBit);
            DumpOpsize(opCol - printed, 16);
        }
        else if (b1 == 0xEE)
        {
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            if ((b2 & 0xF0) == 0)
            {
                // EE/0x (opsize 16)
                x = b2 & 0xF;
                printf("    %02X %02X       Microsoft-specific (x = %02X)", b1, b2, x);
                DumpOpsize(4, 16);
            }
            else
            {
                // EE/xy (opsize 16)
                x = ExtractBits(b2, 4, 4);
                y = ExtractBits(b2, 0, 4);
                printf("    %02X %02X       Available (x = %02X, y = %02X)", b1, b2, x, y);
                DumpOpsize(4, 16);
            }
        }
        else if (b1 == 0xEF)
        {
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            if ((b2 & 0xF0) == 0)
            {
                // EF/0x : ldr lr, [sp], #X*4 (opsize 32)
                x = b2 & 0xF;
                printf("    %02X %02X       ldr lr, [sp], #%-8u", b1, b2, x * 4);
                DumpOpsize(opCol - 39, 32);
            }
            else
            {
                // EF/xy (opsize 32)
                x = ExtractBits(b2, 4, 4);
                y = ExtractBits(b2, 0, 4);
                printf("    %02X %02X       Available (x = %02X, y = %02X)", b1, b2, x, y);
                DumpOpsize(4, 32);
            }
        }
        else if ((b1 >= 0xF0) && (b1 <= 0xF4))
        {
            // F0-F4
            x = b1 & 0x7;
            printf("    %02X          Available (x = %02X)\n", b1, x);
        }
        else if (b1 == 0xF5)
        {
            // F5 : vpop {dS-dE} (opsize 32)

            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            DWORD s = ExtractBits(b2, 4, 4);
            DWORD e = ExtractBits(b2, 0, 4);

            printf("    %02X %02X       vpop ", b1, b2);
            printed = 21;
            printed += DumpRegSetRange("d", s, e, 0);
            DumpOpsize(opCol - printed, 32);
        }
        else if (b1 == 0xF6)
        {
            // F6 : vpop {d(S+16)-d(E+16)} (opsize 32)

            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            DWORD s = ExtractBits(b2, 4, 4);
            DWORD e = ExtractBits(b2, 0, 4);

            printf("    %02X %02X       vpop ", b1, b2);
            printed = 21;
            printed += DumpRegSetRange("d", s + 16, e + 16, 0);
            DumpOpsize(opCol - printed, 32);
        }
        else if (b1 == 0xF7 || b1 == 0xF9)
        {
            // F7, F9 : add sp, sp, #X*4
            // 0xF7 has opsize 16, 0xF9 has opsize 32

            assert(i + 2 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = ((DWORD)b2 << 8) | (DWORD)b3;

            opsize = (b1 == 0xF7) ? 16 : 32;

            printf("    %02X %02X %02X    add sp, sp, #%-8u", b1, b2, b3, x * 4, opsize);
            DumpOpsize(opCol - 37, opsize);
        }
        else if (b1 == 0xF8 || b1 == 0xFA)
        {
            // F8, FA : add sp, sp, #X*4
            // 0xF8 has opsize 16, 0xFA has opsize 32

            assert(i + 3 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            b4 = *pUnwindCode++;
            i += 3;

            x = ((DWORD)b2 << 16) | ((DWORD)b3 << 8) | (DWORD)b4;

            opsize = (b1 == 0xF8) ? 16 : 32;

            printf("    %02X %02X %02X %02X add sp, sp, #%-8u", b1, b2, b3, b4, x * 4, opsize);
            DumpOpsize(opCol - 37, opsize);
        }
        else if (b1 == 0xFB || b1 == 0xFC)
        {
            // FB, FC : nop
            // 0xFB has opsize 16, 0xFC has opsize 32

            opsize = (b1 == 0xFB) ? 16 : 32;

            printf("    %02X          nop", b1, opsize);
            DumpOpsize(opCol - 19, opsize);
        }
        else if (b1 == 0xFD || b1 == 0xFE)
        {
            // FD, FE : end + nop
            // 0xFD has opsize 16, 0xFE has opsize 32

            opsize = (b1 == 0xFD) ? 16 : 32;

            printf("    %02X          end + nop", b1, opsize);
            DumpOpsize(opCol - 25, opsize);
        }
        else if (b1 == 0xFF)
        {
            // FF : end

            printf("    %02X          end\n", b1);
        }
        else
        {
            assert(!"Internal error decoding unwind codes");
        }
    }

    pdw += codeWords;
    assert((PBYTE)pdw == pUnwindCode);
    assert((PBYTE)pdw == pHeader + unwindBlockSize);

    assert(XBit == 0); // We don't handle the case where exception data is present, such as the Exception Handler RVA

    printf("\n");
}

#endif // DEBUG

#endif // defined(TARGET_ARM)
