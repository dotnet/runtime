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

#if defined(TARGET_ARM64)

#if defined(FEATURE_CFI_SUPPORT)
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
        case REG_R16:
            dwarfReg = 16;
            break;
        case REG_R17:
            dwarfReg = 17;
            break;
        case REG_R18:
            dwarfReg = 18;
            break;
        case REG_R19:
            dwarfReg = 19;
            break;
        case REG_R20:
            dwarfReg = 20;
            break;
        case REG_R21:
            dwarfReg = 21;
            break;
        case REG_R22:
            dwarfReg = 22;
            break;
        case REG_R23:
            dwarfReg = 23;
            break;
        case REG_R24:
            dwarfReg = 24;
            break;
        case REG_R25:
            dwarfReg = 25;
            break;
        case REG_R26:
            dwarfReg = 26;
            break;
        case REG_R27:
            dwarfReg = 27;
            break;
        case REG_R28:
            dwarfReg = 28;
            break;
        case REG_R29:
            dwarfReg = 29;
            break;
        case REG_R30:
            dwarfReg = 30;
            break;
        case REG_SP:
            dwarfReg = 31;
            break;
        case REG_V0:
            dwarfReg = 64;
            break;
        case REG_V1:
            dwarfReg = 65;
            break;
        case REG_V2:
            dwarfReg = 66;
            break;
        case REG_V3:
            dwarfReg = 67;
            break;
        case REG_V4:
            dwarfReg = 68;
            break;
        case REG_V5:
            dwarfReg = 69;
            break;
        case REG_V6:
            dwarfReg = 70;
            break;
        case REG_V7:
            dwarfReg = 71;
            break;
        case REG_V8:
            dwarfReg = 72;
            break;
        case REG_V9:
            dwarfReg = 73;
            break;
        case REG_V10:
            dwarfReg = 74;
            break;
        case REG_V11:
            dwarfReg = 75;
            break;
        case REG_V12:
            dwarfReg = 76;
            break;
        case REG_V13:
            dwarfReg = 77;
            break;
        case REG_V14:
            dwarfReg = 78;
            break;
        case REG_V15:
            dwarfReg = 79;
            break;
        case REG_V16:
            dwarfReg = 80;
            break;
        case REG_V17:
            dwarfReg = 81;
            break;
        case REG_V18:
            dwarfReg = 82;
            break;
        case REG_V19:
            dwarfReg = 83;
            break;
        case REG_V20:
            dwarfReg = 84;
            break;
        case REG_V21:
            dwarfReg = 85;
            break;
        case REG_V22:
            dwarfReg = 86;
            break;
        case REG_V23:
            dwarfReg = 87;
            break;
        case REG_V24:
            dwarfReg = 88;
            break;
        case REG_V25:
            dwarfReg = 89;
            break;
        case REG_V26:
            dwarfReg = 90;
            break;
        case REG_V27:
            dwarfReg = 91;
            break;
        case REG_V28:
            dwarfReg = 92;
            break;
        case REG_V29:
            dwarfReg = 93;
            break;
        case REG_V30:
            dwarfReg = 94;
            break;
        case REG_V31:
            dwarfReg = 95;
            break;

        default:
            NYI("CFI codes");
    }

    return dwarfReg;
}
#endif // FEATURE_CFI_SUPPORT

void Compiler::unwindPush(regNumber reg)
{
    unreached(); // use one of the unwindSaveReg* functions instead.
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

    assert(size % 16 == 0);
    unsigned x = size / 16;

    if (x <= 0x1F)
    {
        // alloc_s: 000xxxxx: allocate small stack with size < 128 (2^5 * 16)
        // TODO-Review: should say size < 512

        pu->AddCode((BYTE)x);
    }
    else if (x <= 0x7FF)
    {
        // alloc_m: 11000xxx | xxxxxxxx: allocate large stack with size < 16k (2^11 * 16)
        // TODO-Review: should say size < 32K

        pu->AddCode(0xC0 | (BYTE)(x >> 8), (BYTE)x);
    }
    else
    {
        // alloc_l: 11100000 | xxxxxxxx | xxxxxxxx | xxxxxxxx : allocate large stack with size < 256M (2^24 * 16)
        //
        // For large stack size, the most significant bits
        // are stored first (and next to the opCode) per the unwind spec.

        pu->AddCode(0xE0, (BYTE)(x >> 16), (BYTE)(x >> 8), (BYTE)x);
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

    if (offset == 0)
    {
        assert(reg == REG_FP);

        // set_fp: 11100001 : set up r29 : with : mov r29, sp
        pu->AddCode(0xE1);
    }
    else
    {
        // add_fp: 11100010 | xxxxxxxx : set up r29 with : add r29, sp, #x * 8

        assert(reg == REG_FP);
        assert((offset % 8) == 0);

        unsigned x = offset / 8;
        assert(x <= 0xFF);

        pu->AddCode(0xE2, (BYTE)x);
    }
}

void Compiler::unwindSaveReg(regNumber reg, unsigned offset)
{
    unreached();
}

void Compiler::unwindNop()
{
    UnwindInfo* pu = &funCurrentFunc()->uwi;

#ifdef DEBUG
    if (verbose)
    {
        printf("unwindNop: adding NOP\n");
    }
#endif

    INDEBUG(pu->uwiAddingNOP = true);

    // nop: 11100011: no unwind operation is required.
    pu->AddCode(0xE3);

    INDEBUG(pu->uwiAddingNOP = false);
}

// unwindSaveRegPair: save a register pair to the stack at the specified byte offset (which must be positive,
// a multiple of 8 from 0 to 504). Note that for ARM64 unwind codes, reg2 must be exactly one register higher than reg1,
// except for the case of a pair including LR, in which case reg1 must be either FP or R19/R21/R23/R25/R27 (note that it
// can't be even, such as R20, because that would mean R19 was saved separately, instead of saving <R19,R20> as a pair,
// which we should do instead).
void Compiler::unwindSaveRegPair(regNumber reg1, regNumber reg2, int offset)
{
    // stp reg1, reg2, [sp, #offset]

    // offset for store pair in prolog must be positive and a multiple of 8.
    assert(0 <= offset && offset <= 504);
    assert((offset % 8) == 0);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            FuncInfoDsc*   func     = funCurrentFunc();
            UNATIVE_OFFSET cbProlog = unwindGetCurrentOffset(func);

            createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg1), offset);
            createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg2), offset + 8);
        }

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    int z = offset / 8;
    assert(0 <= z && z <= 0x3F);

    if (reg1 == REG_FP)
    {
        // save_fplr: 01zzzzzz: save <r29,lr> pair at [sp+#Z*8], offset <= 504

        assert(reg2 == REG_LR);

        pu->AddCode(0x40 | (BYTE)z);
    }
    else if (reg2 == REG_LR)
    {
        // save_lrpair: 1101011x | xxzzzzzz: save pair <r19 + 2 * #X, lr> at [sp + #Z * 8], offset <= 504

        assert(REG_R19 <= reg1 && // first legal pair: R19, LR
               reg1 <= REG_R27);  // last legal pair: R27, LR

        BYTE x = (BYTE)(reg1 - REG_R19);
        assert((x % 2) == 0); // only legal reg1: R19, R21, R23, R25, R27
        x /= 2;
        assert(0 <= x && x <= 0x7);

        pu->AddCode(0xD6 | (BYTE)(x >> 2), (BYTE)(x << 6) | (BYTE)z);
    }
    else if (emitter::isGeneralRegister(reg1))
    {
        // save_regp: 110010xx | xxzzzzzz: save r(19 + #X) pair at [sp + #Z * 8], offset <= 504

        assert(REG_NEXT(reg1) == reg2);
        assert(REG_R19 <= reg1 && // first legal pair: R19, R20
               reg1 <= REG_R27);  // last legal pair: R27, R28 (FP is never saved without LR)

        BYTE x = (BYTE)(reg1 - REG_R19);
        assert(0 <= x && x <= 0xF);

        pu->AddCode(0xC8 | (BYTE)(x >> 2), (BYTE)(x << 6) | (BYTE)z);
    }
    else
    {
        // save_fregp: 1101100x | xxzzzzzz : save pair d(8 + #X) at [sp + #Z * 8], offset <= 504

        assert(REG_NEXT(reg1) == reg2);
        assert(REG_V8 <= reg1 && // first legal pair: V8, V9
               reg1 <= REG_V14); // last legal pair: V14, V15

        BYTE x = (BYTE)(reg1 - REG_V8);
        assert(0 <= x && x <= 0x7);

        pu->AddCode(0xD8 | (BYTE)(x >> 2), (BYTE)(x << 6) | (BYTE)z);
    }
}

// unwindSaveRegPairPreindexed: save a register pair to the stack at the specified byte offset (which must be negative,
// a multiple of 8 from -512 to -8). Note that for ARM64 unwind codes, reg2 must be exactly one register higher than
// reg1.
void Compiler::unwindSaveRegPairPreindexed(regNumber reg1, regNumber reg2, int offset)
{
    // stp reg1, reg2, [sp, #offset]!

    // pre-indexed offset in prolog must be negative and a multiple of 8.
    assert(offset < 0);
    assert((offset % 8) == 0);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            FuncInfoDsc*   func     = funCurrentFunc();
            UNATIVE_OFFSET cbProlog = unwindGetCurrentOffset(func);

            createCfiCode(func, cbProlog, CFI_ADJUST_CFA_OFFSET, DWARF_REG_ILLEGAL, -offset);
            createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg1), 0);
            createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg2), 8);
        }

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    if (reg1 == REG_FP)
    {
        // save_fplr_x: 10zzzzzz: save <r29,lr> pair at [sp-(#Z+1)*8]!, pre-indexed offset >= -512

        assert(-512 <= offset);
        int z = (-offset) / 8 - 1;
        assert(0 <= z && z <= 0x3F);

        assert(reg2 == REG_LR);

        pu->AddCode(0x80 | (BYTE)z);
    }
    else if ((reg1 == REG_R19) &&
             (-256 <= offset)) // If the offset is between -512 and -256, we use the save_regp_x unwind code.
    {
        // save_r19r20_x: 001zzzzz: save <r19,r20> pair at [sp-#Z*8]!, pre-indexed offset >= -248
        // NOTE: I'm not sure why we allow Z==0 here; seems useless, and the calculation of offset is different from the
        // other cases.

        int z = (-offset) / 8;
        assert(0 <= z && z <= 0x1F);

        assert(reg2 == REG_R20);

        pu->AddCode(0x20 | (BYTE)z);
    }
    else if (emitter::isGeneralRegister(reg1))
    {
        // save_regp_x: 110011xx | xxzzzzzz: save pair r(19 + #X) at [sp - (#Z + 1) * 8]!, pre-indexed offset >= -512

        assert(-512 <= offset);
        int z = (-offset) / 8 - 1;
        assert(0 <= z && z <= 0x3F);

        assert(REG_NEXT(reg1) == reg2);
        assert(REG_R19 <= reg1 && // first legal pair: R19, R20
               reg1 <= REG_R27);  // last legal pair: R27, R28 (FP is never saved without LR)

        BYTE x = (BYTE)(reg1 - REG_R19);
        assert(0 <= x && x <= 0xF);

        pu->AddCode(0xCC | (BYTE)(x >> 2), (BYTE)(x << 6) | (BYTE)z);
    }
    else
    {
        // save_fregp_x: 1101101x | xxzzzzzz : save pair d(8 + #X), at [sp - (#Z + 1) * 8]!, pre-indexed offset >= -512

        assert(-512 <= offset);
        int z = (-offset) / 8 - 1;
        assert(0 <= z && z <= 0x3F);

        assert(REG_NEXT(reg1) == reg2);
        assert(REG_V8 <= reg1 && // first legal pair: V8, V9
               reg1 <= REG_V14); // last legal pair: V14, V15

        BYTE x = (BYTE)(reg1 - REG_V8);
        assert(0 <= x && x <= 0x7);

        pu->AddCode(0xDA | (BYTE)(x >> 2), (BYTE)(x << 6) | (BYTE)z);
    }
}

void Compiler::unwindSaveReg(regNumber reg, int offset)
{
    // str reg, [sp, #offset]

    // offset for store in prolog must be positive and a multiple of 8.
    assert(0 <= offset && offset <= 504);
    assert((offset % 8) == 0);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            FuncInfoDsc*   func     = funCurrentFunc();
            UNATIVE_OFFSET cbProlog = unwindGetCurrentOffset(func);

            createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg), offset);
        }

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    int z = offset / 8;
    assert(0 <= z && z <= 0x3F);

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    if (emitter::isGeneralRegister(reg))
    {
        // save_reg: 110100xx | xxzzzzzz: save reg r(19 + #X) at [sp + #Z * 8], offset <= 504

        assert(REG_R19 <= reg && // first legal register: R19
               reg <= REG_LR);   // last legal register: LR

        BYTE x = (BYTE)(reg - REG_R19);
        assert(0 <= x && x <= 0xF);

        pu->AddCode(0xD0 | (BYTE)(x >> 2), (BYTE)(x << 6) | (BYTE)z);
    }
    else
    {
        // save_freg: 1101110x | xxzzzzzz : save reg d(8 + #X) at [sp + #Z * 8], offset <= 504

        assert(REG_V8 <= reg && // first legal register: V8
               reg <= REG_V15); // last legal register: V15

        BYTE x = (BYTE)(reg - REG_V8);
        assert(0 <= x && x <= 0x7);

        pu->AddCode(0xDC | (BYTE)(x >> 2), (BYTE)(x << 6) | (BYTE)z);
    }
}

void Compiler::unwindSaveRegPreindexed(regNumber reg, int offset)
{
    // str reg, [sp, #offset]!

    // pre-indexed offset in prolog must be negative and a multiple of 8.
    assert(-256 <= offset && offset < 0);
    assert((offset % 8) == 0);

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        if (compGeneratingProlog)
        {
            FuncInfoDsc*   func     = funCurrentFunc();
            UNATIVE_OFFSET cbProlog = unwindGetCurrentOffset(func);

            createCfiCode(func, cbProlog, CFI_ADJUST_CFA_OFFSET, DWARF_REG_ILLEGAL, -offset);
            createCfiCode(func, cbProlog, CFI_REL_OFFSET, mapRegNumToDwarfReg(reg), 0);
        }

        return;
    }
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    int z = (-offset) / 8 - 1;
    assert(0 <= z && z <= 0x1F);

    if (emitter::isGeneralRegister(reg))
    {
        // save_reg_x: 1101010x | xxxzzzzz: save reg r(19 + #X) at [sp - (#Z + 1) * 8]!, pre-indexed offset >= -256

        assert(REG_R19 <= reg && // first legal register: R19
               reg <= REG_LR);   // last legal register: LR

        BYTE x = (BYTE)(reg - REG_R19);
        assert(0 <= x && x <= 0xF);

        pu->AddCode(0xD4 | (BYTE)(x >> 3), (BYTE)(x << 5) | (BYTE)z);
    }
    else
    {
        // save_freg_x: 11011110 | xxxzzzzz : save reg d(8 + #X) at [sp - (#Z + 1) * 8]!, pre - indexed offset >= -256

        assert(REG_V8 <= reg && // first legal register: V8
               reg <= REG_V15); // last legal register: V15

        BYTE x = (BYTE)(reg - REG_V8);
        assert(0 <= x && x <= 0x7);

        pu->AddCode(0xDE, (BYTE)(x << 5) | (BYTE)z);
    }
}

void Compiler::unwindSaveNext()
{
#if defined(FEATURE_CFI_SUPPORT)
    // do not use unwindSaveNext when generating CFI codes as there is no code for this
    assert(!generateCFIUnwindCodes());
#endif // FEATURE_CFI_SUPPORT

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    // We're saving the next register pair. The caller is responsible for ensuring this is correct!

    // save_next: 11100110 : save next non - volatile Int or FP register pair.
    pu->AddCode(0xE6);
}

void Compiler::unwindReturn(regNumber reg)
{
    // Nothing to do; we will always have at least one trailing "end" opcode in our padding.
}

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Unwind Info Debug helpers                                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifdef DEBUG

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
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 80-8F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 90-9F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // A0-AF
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // B0-BF
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // C0-CF
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, // D0-DF
        4, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // E0-EF
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1  // F0-FF
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
unsigned UnwindCodesBase::GetCodeSizeFromUnwindCodes(bool isProlog)
{
    BYTE*    pCodesStart = GetCodes();
    BYTE*    pCodes      = pCodesStart;
    unsigned size        = 0;
    for (;;)
    {
        BYTE b1 = *pCodes;
        if (IsEndCode(b1))
        {
            break; // We hit an "end" code; we're done
        }
        size += 4; // All codes represent 4 byte instructions.
        pCodes += GetUnwindSizeFromUnwindHeader(b1);
        assert(pCodes - pCodesStart < 256); // 255 is the absolute maximum number of code bytes allowed
    }
    return size;
}

#endif // DEBUG

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

    DWORD codeWords      = ExtractBits(dw, 27, 5);
    DWORD epilogCount    = ExtractBits(dw, 22, 5);
    DWORD EBit           = ExtractBits(dw, 21, 1);
    DWORD XBit           = ExtractBits(dw, 20, 1);
    DWORD Vers           = ExtractBits(dw, 18, 2);
    DWORD functionLength = ExtractBits(dw, 0, 18);

    printf("  >> Start offset   : 0x%06x (not in unwind data)\n", comp->dspOffset(startOffset));
    printf("  >>   End offset   : 0x%06x (not in unwind data)\n", comp->dspOffset(endOffset));
    printf("  Code Words        : %u\n", codeWords);
    printf("  Epilog Count      : %u\n", epilogCount);
    printf("  E bit             : %u\n", EBit);
    printf("  X bit             : %u\n", XBit);
    printf("  Vers              : %u\n", Vers);
    printf("  Function Length   : %u (0x%05x) Actual length = %u (0x%06x)\n", functionLength, functionLength,
           functionLength * 4, functionLength * 4);

    assert(functionLength * 4 == endOffset - startOffset);

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

    bool epilogStartAt[1024] = {}; // One byte per possible epilog start index; initialized to false

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
                DWORD res               = ExtractBits(dw, 18, 4);
                DWORD epilogStartIndex  = ExtractBits(dw, 22, 10);

                // Note that epilogStartOffset for a funclet is the offset from the beginning
                // of the current funclet, not the offset from the beginning of the main function.
                // To help find it when looking through JitDump output, also show the offset from
                // the beginning of the main function.
                DWORD epilogStartOffsetFromMainFunctionBegin = epilogStartOffset * 4 + startOffset;

                assert(res == 0);

                printf("  ---- Scope %d\n", scope);
                printf("  Epilog Start Offset        : %u (0x%05x) Actual offset = %u (0x%06x) Offset from main "
                       "function begin = %u (0x%06x)\n",
                       comp->dspOffset(epilogStartOffset), comp->dspOffset(epilogStartOffset),
                       comp->dspOffset(epilogStartOffset * 4), comp->dspOffset(epilogStartOffset * 4),
                       comp->dspOffset(epilogStartOffsetFromMainFunctionBegin),
                       comp->dspOffset(epilogStartOffsetFromMainFunctionBegin));
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

    // Dump the unwind codes

    printf("  ---- Unwind codes ----\n");

    DWORD countOfUnwindCodes = codeWords * 4;
    PBYTE pUnwindCode        = (PBYTE)pdw;
    BYTE  b1, b2, b3, b4;
    DWORD x, z;
    for (DWORD i = 0; i < countOfUnwindCodes; i++)
    {
        // Does this byte start an epilog sequence? If so, note that fact.
        if (epilogStartAt[i])
        {
            printf("    ---- Epilog start at index %u ----\n", i);
        }

        b1 = *pUnwindCode++;

        if ((b1 & 0xE0) == 0)
        {
            // alloc_s: 000xxxxx: allocate small stack with size < 128 (2^5 * 16)
            // TODO-Review:should say size < 512
            x = b1 & 0x1F;
            printf("    %02X          alloc_s #%u (0x%02X); sub sp, sp, #%u (0x%03X)\n", b1, x, x, x * 16, x * 16);
        }
        else if ((b1 & 0xE0) == 0x20)
        {
            // save_r19r20_x: 001zzzzz: save <r19,r20> pair at [sp-#Z*8]!, pre-indexed offset >= -248
            z = b1 & 0x1F;
            printf("    %02X          save_r19r20_x #%u (0x%02X); stp %s, %s, [sp, #-%u]!\n", b1, z, z,
                   getRegName(REG_R19), getRegName(REG_R20), z * 8);
        }
        else if ((b1 & 0xC0) == 0x40)
        {
            // save_fplr: 01zzzzzz: save <r29,lr> pair at [sp+#Z*8], offset <= 504
            z = b1 & 0x3F;
            printf("    %02X          save_fplr #%u (0x%02X); stp %s, %s, [sp, #%u]\n", b1, z, z, getRegName(REG_FP),
                   getRegName(REG_LR), z * 8);
        }
        else if ((b1 & 0xC0) == 0x80)
        {
            // save_fplr_x: 10zzzzzz: save <r29,lr> pair at [sp-(#Z+1)*8]!, pre-indexed offset >= -512
            z = b1 & 0x3F;
            printf("    %02X          save_fplr_x #%u (0x%02X); stp %s, %s, [sp, #-%u]!\n", b1, z, z,
                   getRegName(REG_FP), getRegName(REG_LR), (z + 1) * 8);
        }
        else if ((b1 & 0xF8) == 0xC0)
        {
            // alloc_m: 11000xxx | xxxxxxxx: allocate large stack with size < 16k (2^11 * 16)
            // TODO-Review: should save size < 32K
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x7) << 8) | (DWORD)b2;

            printf("    %02X %02X       alloc_m #%u (0x%03X); sub sp, sp, #%u (0x%04X)\n", b1, b2, x, x, x * 16,
                   x * 16);
        }
        else if ((b1 & 0xFC) == 0xC8)
        {
            // save_regp: 110010xx | xxzzzzzz: save r(19 + #X) pair at [sp + #Z * 8], offset <= 504
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x3) << 2) | (DWORD)(b2 >> 6);
            z = (DWORD)(b2 & 0x3F);

            printf("    %02X %02X       save_regp X#%u Z#%u (0x%02X); stp %s, %s, [sp, #%u]\n", b1, b2, x, z, z,
                   getRegName(REG_R19 + x), getRegName(REG_R19 + x + 1), z * 8);
        }
        else if ((b1 & 0xFC) == 0xCC)
        {
            // save_regp_x: 110011xx | xxzzzzzz: save pair r(19 + #X) at [sp - (#Z + 1) * 8]!, pre-indexed offset >=
            // -512
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x3) << 2) | (DWORD)(b2 >> 6);
            z = (DWORD)(b2 & 0x3F);

            printf("    %02X %02X       save_regp_x X#%u Z#%u (0x%02X); stp %s, %s, [sp, #-%u]!\n", b1, b2, x, z, z,
                   getRegName(REG_R19 + x), getRegName(REG_R19 + x + 1), (z + 1) * 8);
        }
        else if ((b1 & 0xFC) == 0xD0)
        {
            // save_reg: 110100xx | xxzzzzzz: save reg r(19 + #X) at [sp + #Z * 8], offset <= 504
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x3) << 2) | (DWORD)(b2 >> 6);
            z = (DWORD)(b2 & 0x3F);

            printf("    %02X %02X       save_reg X#%u Z#%u (0x%02X); str %s, [sp, #%u]\n", b1, b2, x, z, z,
                   getRegName(REG_R19 + x), z * 8);
        }
        else if ((b1 & 0xFE) == 0xD4)
        {
            // save_reg_x: 1101010x | xxxzzzzz: save reg r(19 + #X) at [sp - (#Z + 1) * 8]!, pre-indexed offset >= -256
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x1) << 3) | (DWORD)(b2 >> 5);
            z = (DWORD)(b2 & 0x1F);

            printf("    %02X %02X       save_reg_x X#%u Z#%u (0x%02X); str %s, [sp, #-%u]!\n", b1, b2, x, z, z,
                   getRegName(REG_R19 + x), (z + 1) * 8);
        }
        else if ((b1 & 0xFE) == 0xD6)
        {
            // save_lrpair: 1101011x | xxzzzzzz: save pair <r19 + 2 * #X, lr> at [sp + #Z * 8], offset <= 504
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x1) << 2) | (DWORD)(b2 >> 6);
            z = (DWORD)(b2 & 0x3F);

            printf("    %02X %02X       save_lrpair X#%u Z#%u (0x%02X); stp %s, %s, [sp, #%u]\n", b1, b2, x, z, z,
                   getRegName(REG_R19 + 2 * x), getRegName(REG_LR), z * 8);
        }
        else if ((b1 & 0xFE) == 0xD8)
        {
            // save_fregp: 1101100x | xxzzzzzz : save pair d(8 + #X) at [sp + #Z * 8], offset <= 504
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x1) << 2) | (DWORD)(b2 >> 6);
            z = (DWORD)(b2 & 0x3F);

            printf("    %02X %02X       save_fregp X#%u Z#%u (0x%02X); stp %s, %s, [sp, #%u]\n", b1, b2, x, z, z,
                   getRegName(REG_V8 + x), getRegName(REG_V8 + x + 1), z * 8);
        }
        else if ((b1 & 0xFE) == 0xDA)
        {
            // save_fregp_x: 1101101x | xxzzzzzz : save pair d(8 + #X), at [sp - (#Z + 1) * 8]!, pre-indexed offset >=
            // -512
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x1) << 2) | (DWORD)(b2 >> 6);
            z = (DWORD)(b2 & 0x3F);

            printf("    %02X %02X       save_fregp_x X#%u Z#%u (0x%02X); stp %s, %s, [sp, #-%u]!\n", b1, b2, x, z, z,
                   getRegName(REG_V8 + x), getRegName(REG_V8 + x + 1), (z + 1) * 8);
        }
        else if ((b1 & 0xFE) == 0xDC)
        {
            // save_freg: 1101110x | xxzzzzzz : save reg d(8 + #X) at [sp + #Z * 8], offset <= 504
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x1) << 2) | (DWORD)(b2 >> 6);
            z = (DWORD)(b2 & 0x3F);

            printf("    %02X %02X       save_freg X#%u Z#%u (0x%02X); str %s, [sp, #%u]\n", b1, b2, x, z, z,
                   getRegName(REG_V8 + x), z * 8);
        }
        else if (b1 == 0xDE)
        {
            // save_freg_x: 11011110 | xxxzzzzz : save reg d(8 + #X) at [sp - (#Z + 1) * 8]!, pre - indexed offset >=
            // -256
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = (DWORD)(b2 >> 5);
            z = (DWORD)(b2 & 0x1F);

            printf("    %02X %02X       save_freg_x X#%u Z#%u (0x%02X); str %s, [sp, #-%u]!\n", b1, b2, x, z, z,
                   getRegName(REG_V8 + x), (z + 1) * 8);
        }
        else if (b1 == 0xE0)
        {
            // alloc_l: 11100000 | xxxxxxxx | xxxxxxxx | xxxxxxxx : allocate large stack with size < 256M (2^24 * 16)
            assert(i + 3 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            b4 = *pUnwindCode++;
            i += 3;

            x = ((DWORD)b2 << 16) | ((DWORD)b3 << 8) | (DWORD)b4;

            printf("    %02X %02X %02X %02X alloc_l %u (0x%06X); sub sp, sp, #%u (%06X)\n", b1, b2, b3, b4, x, x,
                   x * 16, x * 16);
        }
        else if (b1 == 0xE1)
        {
            // set_fp: 11100001 : set up r29 : with : mov r29, sp

            printf("    %02X          set_fp; mov %s, sp\n", b1, getRegName(REG_FP));
        }
        else if (b1 == 0xE2)
        {
            // add_fp: 11100010 | xxxxxxxx : set up r29 with : add r29, sp, #x * 8
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = (DWORD)b2;

            printf("    %02X %02X       add_fp %u (0x%02X); add %s, sp, #%u\n", b1, b2, x, x, getRegName(REG_FP),
                   x * 8);
        }
        else if (b1 == 0xE3)
        {
            // nop: 11100011: no unwind operation is required.

            printf("    %02X          nop\n", b1);
        }
        else if (b1 == 0xE4)
        {
            // end: 11100100 : end of unwind code

            printf("    %02X          end\n", b1);
        }
        else if (b1 == 0xE5)
        {
            // end_c: 11100101 : end of unwind code in current chained scope.

            printf("    %02X          end_c\n", b1);
        }
        else if (b1 == 0xE6)
        {
            // save_next: 11100110 : save next non - volatile Int or FP register pair.

            printf("    %02X          save_next\n", b1);
        }
        else
        {
            // Unknown / reserved unwind code
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

#endif // TARGET_ARM64
