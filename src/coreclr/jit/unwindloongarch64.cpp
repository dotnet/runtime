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

#if defined(TARGET_LOONGARCH64)

#if defined(FEATURE_CFI_SUPPORT)
short Compiler::mapRegNumToDwarfReg(regNumber reg)
{
    short dwarfReg = DWARF_REG_ILLEGAL;

    switch (reg)
    {
        case REG_R0:
            dwarfReg = 0;
            break;
        case REG_RA:
            dwarfReg = 1;
            break;
        case REG_TP:
            dwarfReg = 2;
            break;
        case REG_SP:
            dwarfReg = 3;
            break;
        case REG_A0:
            dwarfReg = 4;
            break;
        case REG_A1:
            dwarfReg = 5;
            break;
        case REG_A2:
            dwarfReg = 6;
            break;
        case REG_A3:
            dwarfReg = 7;
            break;
        case REG_A4:
            dwarfReg = 8;
            break;
        case REG_A5:
            dwarfReg = 9;
            break;
        case REG_A6:
            dwarfReg = 10;
            break;
        case REG_A7:
            dwarfReg = 11;
            break;
        case REG_T0:
            dwarfReg = 12;
            break;
        case REG_T1:
            dwarfReg = 13;
            break;
        case REG_T2:
            dwarfReg = 14;
            break;
        case REG_T3:
            dwarfReg = 15;
            break;
        case REG_T4:
            dwarfReg = 16;
            break;
        case REG_T5:
            dwarfReg = 17;
            break;
        case REG_T6:
            dwarfReg = 18;
            break;
        case REG_T7:
            dwarfReg = 19;
            break;
        case REG_T8:
            dwarfReg = 20;
            break;
        case REG_X0:
            dwarfReg = 21;
            break;
        case REG_FP:
            dwarfReg = 22;
            break;
        case REG_S0:
            dwarfReg = 23;
            break;
        case REG_S1:
            dwarfReg = 24;
            break;
        case REG_S2:
            dwarfReg = 25;
            break;
        case REG_S3:
            dwarfReg = 26;
            break;
        case REG_S4:
            dwarfReg = 27;
            break;
        case REG_S5:
            dwarfReg = 28;
            break;
        case REG_S6:
            dwarfReg = 29;
            break;
        case REG_S7:
            dwarfReg = 30;
            break;
        case REG_S8:
            dwarfReg = 31;
            break;
        case REG_F0:
            dwarfReg = 64;
            break;
        case REG_F1:
            dwarfReg = 65;
            break;
        case REG_F2:
            dwarfReg = 66;
            break;
        case REG_F3:
            dwarfReg = 67;
            break;
        case REG_F4:
            dwarfReg = 68;
            break;
        case REG_F5:
            dwarfReg = 69;
            break;
        case REG_F6:
            dwarfReg = 70;
            break;
        case REG_F7:
            dwarfReg = 71;
            break;
        case REG_F8:
            dwarfReg = 72;
            break;
        case REG_F9:
            dwarfReg = 73;
            break;
        case REG_F10:
            dwarfReg = 74;
            break;
        case REG_F11:
            dwarfReg = 75;
            break;
        case REG_F12:
            dwarfReg = 76;
            break;
        case REG_F13:
            dwarfReg = 77;
            break;
        case REG_F14:
            dwarfReg = 78;
            break;
        case REG_F15:
            dwarfReg = 79;
            break;
        case REG_F16:
            dwarfReg = 80;
            break;
        case REG_F17:
            dwarfReg = 81;
            break;
        case REG_F18:
            dwarfReg = 82;
            break;
        case REG_F19:
            dwarfReg = 83;
            break;
        case REG_F20:
            dwarfReg = 84;
            break;
        case REG_F21:
            dwarfReg = 85;
            break;
        case REG_F22:
            dwarfReg = 86;
            break;
        case REG_F23:
            dwarfReg = 87;
            break;
        case REG_F24:
            dwarfReg = 88;
            break;
        case REG_F25:
            dwarfReg = 89;
            break;
        case REG_F26:
            dwarfReg = 90;
            break;
        case REG_F27:
            dwarfReg = 91;
            break;
        case REG_F28:
            dwarfReg = 92;
            break;
        case REG_F29:
            dwarfReg = 93;
            break;
        case REG_F30:
            dwarfReg = 94;
            break;
        case REG_F31:
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
    else if (x <= 0x7F)
    {
        // alloc_m: 11000xxx | xxxxxxxx: allocate large stack with size < 2k (2^7 * 16)

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

        // set_fp: 11100001 : set up fp : with : move fp, sp
        pu->AddCode(0xE1);
    }
    else
    {
        // add_fp: 11100010 | 000xxxxx | xxxxxxxx : set up fp with : addi.d fp, sp, #x * 8

        assert(reg == REG_FP);
        assert((offset % 8) == 0);

        unsigned x = offset / 8;
        assert(x <= 0x1FF);

        pu->AddCode(0xE2, (BYTE)(x >> 8), (BYTE)x);
    }
}

void Compiler::unwindSaveReg(regNumber reg, unsigned offset)
{
    unwindSaveReg(reg, (int)offset);
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

void Compiler::unwindSaveReg(regNumber reg, int offset)
{

    // st.d reg, sp, offset

    // offset for store in prolog must be positive and a multiple of 8.
    assert(0 <= offset && offset <= 2047);
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
    // assert(0 <= z && z <= 0xFF);

    UnwindInfo* pu = &funCurrentFunc()->uwi;

    if (emitter::isGeneralRegister(reg))
    {
        // save_reg: 11010000 | 000xxxxx | zzzzzzzz: save reg r(1 + #X) at [sp + #Z * 8], offset <= 2047

        assert(reg == REG_RA || reg == REG_FP ||  // first legal register: RA
               (REG_S0 <= reg && reg <= REG_S8)); // last legal register: S8

        BYTE x = (BYTE)(reg - REG_RA);
        assert(0 <= x && x <= 0x1E);

        pu->AddCode(0xD0, (BYTE)x, (BYTE)z);
    }
    else
    {
        // save_freg: 11011100 | 0xxxzzzz | zzzzzzzz : save reg f(24 + #X) at [sp + #Z * 8], offset <= 2047

        assert(REG_F24 <= reg && // first legal register: F24
               reg <= REG_F31);  // last legal register: F31

        BYTE x = (BYTE)(reg - REG_F24);
        assert(0 <= x && x <= 0x7);

        pu->AddCode(0xDC, (BYTE)(x << 4) | (BYTE)(z >> 8), (BYTE)z);
    }
}

void Compiler::unwindSaveRegPair(regNumber reg1, regNumber reg2, int offset)
{
    assert(!"unused on LOONGARCH64 yet");
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
        // array of unwind sizes, in bytes (as specified in the LOONGARCH unwind specification)
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 00-0F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 10-1F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 20-2F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 30-3F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 40-4F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 50-5F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 60-6F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 70-7F
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 80-8F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 90-9F
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // A0-AF
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // B0-BF
        2, 2, 2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 3, 2, 2, 2, // C0-CF
        3, 2, 2, 2, 2, 2, 3, 2, 3, 2, 3, 2, 3, 2, 2, 1, // D0-DF
        4, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // E0-EF
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
            printf("    %02X          alloc_s #%u (0x%02X); addi.d sp, sp, -%u (0x%03X)\n", b1, x, x, x * 16, x * 16);
        }
#if 0
        else if ((b1 & 0xE0) == 0x20)
        {
            // save_s0s1_x: 001zzzzz: save <s0,s1> pair at [sp-#Z*8]!, pre-indexed offset >= -248
            z = b1 & 0x1F;
            printf("    %02X          save_s0s1_x #%u (0x%02X); Two sd %s, %s, [sp, #-%u]!\n", b1, z, z,
                   getRegName(REG_S0), getRegName(REG_S1), z * 8);
        }
        else if ((b1 & 0xF0) == 0x40)
        {
            // save_fpra: 0100zzzz | zzzzzzzz: save <fp,ra> pair at [sp+#Z*8], offset <= 4080
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            z = ((DWORD)(b1 & 0xF) << 8) | (DWORD)b2;
            printf("    %02X %02X          save_fpra #%u (0x%03X); Two sd %s, %s, [sp, #%u]\n", b1, b2, z, z, getRegName(REG_FP),
                   getRegName(REG_RA), z * 8);
        }
        else if ((b1 & 0xF0) == 0x80)
        {
            // save_fpra_x: 1000zzzz | zzzzzzzz: save <fp,ra> pair at [sp-(#Z+1)*8]!, pre-indexed offset >= -32768
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            z = ((DWORD)(b1 & 0xF) << 8) | (DWORD)b2;
            printf("    %02X %02X          save_fpra_x #%u (0x%03X); Two sd %s, %s, [sp, #-%u]!\n", b1, b2, z, z,
                   getRegName(REG_FP), getRegName(REG_RA), (z + 1) * 8);
        }
#endif
        else if ((b1 & 0xF8) == 0xC0)
        {
            // alloc_m: 11000xxx | xxxxxxxx: allocate large stack with size < 2k (2^7 * 16)
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x7) << 8) | (DWORD)b2;

            printf("    %02X %02X       alloc_m #%u (0x%03X); addi.d sp, sp, -%u (0x%04X)\n", b1, b2, x, x, x * 16,
                   x * 16);
        }
        else if (b1 == 0xD0)
        {
            // save_reg: 11010000 | 000xxxxx | zzzzzzzz: save reg r(1 + #X) at [sp + #Z * 8], offset <= 2047
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = (DWORD)b2;
            z = (DWORD)b3;

            printf("    %02X %02X %02X      save_reg X#%u Z#%u (0x%02X); st.d %s, sp, %u\n", b1, b2, b3, x, z, z,
                   getRegName(REG_RA + x), z * 8);
        }
#if 0
        else if (b1 == 0xC8)
        {
            // save_regp: 11001000 | 0xxxzzzz | zzzzzzzz: save s(0 + #X) pair at [sp + #Z * 8], offset <= 4080
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = (DWORD)(b2 >> 4);
            z = ((DWORD)(b2 & 0xF) << 8) | (DWORD)b3;

            printf("    %02X %02X %02X      save_regp X#%u Z#%u (0x%02X); Two sd %s, %s, [sp, #%u]\n", b1, b2, b3, x, z, z,
                   getRegName(REG_S0 + x), getRegName(REG_S0 + x + 1), z * 8);
        }
        else if (b1 == 0xCC)
        {
            // save_regp_x: 11001100 | 0xxxzzzz | zzzzzzzz: save pair s(0 + #X) at [sp - (#Z + 1) * 8]!, pre-indexed offset >=
            // -32768
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i+= 2;

            x = (DWORD)(b2 >> 4);
            z = ((DWORD)(b2 & 0xF) << 8) | (DWORD)b3;

            printf("    %02X %02X %02X      save_regp_x X#%u Z#%u (0x%02X); Two sd %s, %s, [sp, #-%u]!\n", b1, b2, b3, x, z, z,
                   getRegName(REG_S0 + x), getRegName(REG_S0 + x + 1), (z + 1) * 8);
        }
        else if ((b1 & 0xFE) == 0xD4)
        {
            // save_reg_x: 1101010x | xxxzzzzz: save reg s(0 + #X) at [sp - (#Z + 1) * 8]!, pre-indexed offset >= -16384
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = ((DWORD)(b1 & 0x1) << 3) | (DWORD)(b2 >> 5);
            z = (DWORD)(b2 & 0x1F);

            printf("    %02X %02X       save_reg_x X#%u Z#%u (0x%02X); sd %s, [sp, #-%u]!\n", b1, b2, x, z, z,
                   getRegName(REG_S0 + x), (z + 1) * 8);
        }
        else if (b1 == 0xD6)
        {
            // save_rapair: 11010110 | 0xxxzzzz | zzzzzzzz: save pair <s0 + #X, ra> at [sp + #Z * 8], offset <= 32767
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = (DWORD)(b2 >> 4);
            z = ((DWORD)(b2 & 0xF) << 8) | (DWORD)b3;

            printf("    %02X %02X %02X      save_lrpair X#%u Z#%u (0x%02X); Two sd %s, %s, [sp, #%u]\n", b1, b2, b3, x, z, z,
                   getRegName(REG_S0 + x), getRegName(REG_RA), z * 8);
        }
        else if (b1 == 0xD8)
        {
            // save_fregp: 11011000 | 0xxxzzzz | zzzzzzzz : save pair f(24 + #X) at [sp + #Z * 8], offset <= 32767
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = (DWORD)(b2 >> 4);
            z = ((DWORD)(b2 & 0xF) << 8) | (DWORD)b3;

            printf("    %02X %02X %02X      save_fregp X#%u Z#%u (0x%02X); Two sdc1 %s, %s, [sp, #%u]\n", b1, b2, b3, x, z, z,
                   getRegName(REG_F24 + x, true), getRegName(REG_F24 + x + 1, true), z * 8);
        }
        else if (b1 == 0xDA)
        {
            // save_fregp_x: 11011010 | 0xxxzzzz | zzzzzzzz : save pair f(24 + #X), at [sp - (#Z + 1) * 8]!, pre-indexed offset >=
            // -32768
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = (DWORD)(b2 >> 4);
            z = ((DWORD)(b2 & 0xF) << 8) | (DWORD)b3;

            printf("    %02X %02X %02X      save_fregp_x X#%u Z#%u (0x%02X); Two sdc1 %s, %s, [sp, #-%u]!\n", b1, b2, b3, x, z, z,
                   getRegName(REG_F24 + x, true), getRegName(REG_F24 + x + 1, true), (z + 1) * 8);
        }
#endif
        else if (b1 == 0xDC)
        {
            // save_freg: 11011100 | 0xxxzzzz | zzzzzzzz : save reg f(24 + #X) at [sp + #Z * 8], offset <= 2047
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = (DWORD)(b2 >> 4);
            z = ((DWORD)(b2 & 0xF) << 8) | (DWORD)b3;

            printf("    %02X %02X %02X      save_freg X#%u Z#%u (0x%02X); fst.d %s, [sp, #%u]\n", b1, b2, b3, x, z, z,
                   getRegName(REG_F24 + x), z * 8);
        }
#if 0
        else if (b1 == 0xDE)
        {
            // save_freg_x: 11011110 | xxxzzzzz : save reg f(24 + #X) at [sp - (#Z + 1) * 8]!, pre - indexed offset >=
            // -16384
            assert(i + 1 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            i++;

            x = (DWORD)(b2 >> 5);
            z = (DWORD)(b2 & 0x1F);

            printf("    %02X %02X       save_freg_x X#%u Z#%u (0x%02X); sdc1 %s, [sp, #-%u]!\n", b1, b2, x, z, z,
                   getRegName(REG_F24 + x, true), (z + 1) * 8);
        }
#endif
        else if (b1 == 0xE0)
        {
            // alloc_l: 11100000 | xxxxxxxx | xxxxxxxx | xxxxxxxx : allocate large stack with size < 256M (2^24 * 16)
            assert(i + 3 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            b4 = *pUnwindCode++;
            i += 3;

            x = ((DWORD)b2 << 16) | ((DWORD)b3 << 8) | (DWORD)b4;

            printf("    %02X %02X %02X %02X alloc_l %u (0x%06X); addi.d sp, sp, -%u (%06X)\n", b1, b2, b3, b4, x, x,
                   x * 16, x * 16);
        }
        else if (b1 == 0xE1)
        {
            // set_fp: 11100001 : set up $29 : with : move fp, sp

            printf("    %02X          set_fp; move %s, sp\n", b1, getRegName(REG_FP));
        }
        else if (b1 == 0xE2)
        {
            // add_fp: 11100010 | 000xxxxx | xxxxxxxx : set up fp with : addi.d fp, sp, #x * 8
            assert(i + 2 < countOfUnwindCodes);
            b2 = *pUnwindCode++;
            b3 = *pUnwindCode++;
            i += 2;

            x = ((DWORD)(b2 & 0x1F) << 8) | (DWORD)b3;

            printf("    %02X %02X %02X      add_fp %u (0x%02X); addi.d %s, sp, #%u\n", b1, b2, b3, x, x,
                   getRegName(REG_FP), x * 8);
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
            printf("===========[loongarch64] Unknown / reserved unwind code: %02X\n", b1);
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
}

void Compiler::unwindBegEpilog()
{
    assert(compGeneratingEpilog);

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
}

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
    bool funcHasColdSection = false;

#if defined(FEATURE_CFI_SUPPORT)
    if (generateCFIUnwindCodes())
    {
        DWORD unwindCodeBytes = 0;
        if (fgFirstColdBlock != nullptr)
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

    if (fgFirstColdBlock != NULL)
    {
        assert(!isFunclet); // TODO-CQ: support hot/cold splitting with EH

        emitLocation* startLoc;
        emitLocation* endLoc;
        unwindGetFuncLocations(func, false, &startLoc, &endLoc);

        func->uwiCold = new (this, CMK_UnwindInfo) UnwindInfo();
        func->uwiCold->InitUnwindInfo(this, startLoc, endLoc);
        func->uwiCold->HotColdSplitCodes(&func->uwi);

        funcHasColdSection = true;
    }

    // First we need to split the function or funclet into fragments that are no larger
    // than 512K, so the fragment size will fit in the unwind data "Function Length" field.
    // The LOONGARCH Exception Data specification "Function Fragments" section describes this.
    func->uwi.Split();

    func->uwi.Reserve(isFunclet, true);

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
        unwindEmitFuncCFI(func, pHotCode, pColdCode);
        return;
    }
#endif // FEATURE_CFI_SUPPORT

    func->uwi.Allocate((CorJitFuncKind)func->funKind, pHotCode, pColdCode, true);

    if (func->uwiCold != NULL)
    {
        func->uwiCold->Allocate((CorJitFuncKind)func->funKind, pHotCode, pColdCode, false);
    }
}

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

int UnwindPrologCodes::Match(UnwindEpilogInfo* pEpi)
{
    if (Size() < pEpi->Size())
    {
        return -1;
    }

    int matchIndex = 0; // Size() - pEpi->Size();

    BYTE* pProlog = GetCodes();
    BYTE* pEpilog = pEpi->GetCodes();

    // First check set_fp.
    if (0 < pEpi->Size())
    {
        if (*pProlog == 0xE1)
        {
            pProlog++;
            if (*pEpilog == 0xE1)
            {
                pEpilog++;
            }
            else
            {
                matchIndex = 1;
            }
        }
        else if (*pProlog == 0xE2)
        {
            pProlog += 3;
            if (*pEpilog == 0xE1)
            {
                pEpilog += 3;
            }
            else
            {
                matchIndex = 3;
            }
        }
    }

    if (0 == memcmp(pProlog, pEpilog, pEpi->Size()))
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
    ufiPrologCodes.AddCode(UWC_END_C);
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

    noway_assert((functionLength & 3) == 0);
    DWORD headerFunctionLength = functionLength / 4;

    DWORD headerVers = 0; // Version of the unwind info is zero. No other version number is currently defined.
    DWORD headerXBit = 0; // We never generate "exception data", but the VM might add some.
    DWORD headerEBit;
    DWORD headerEpilogCount; // This depends on how we set headerEBit.
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

    DWORD header = headerFunctionLength | (headerVers << 18) | (headerXBit << 20) | (headerEBit << 21) |
                   (headerEpilogCount << 22) | (headerCodeWords << 27);

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
            // The epilog must strictly follow the prolog. The prolog is in the first fragment of
            // the hot section. If this epilog is at the start of a fragment, it can't be the
            // first fragment in the hot section. We actually don't know if we're processing
            // the hot or cold section (or a funclet), so we can't distinguish these cases. Thus,
            // we just assert that the epilog starts within the fragment.
            assert(pEpi->GetStartOffset() >= GetStartOffset());

            // We report the offset of an epilog as the offset from the beginning of the function/funclet fragment,
            // NOT the offset from the beginning of the main function.
            DWORD headerEpilogStartOffset = pEpi->GetStartOffset() - GetStartOffset();

            noway_assert((headerEpilogStartOffset & 3) == 0);
            headerEpilogStartOffset /= 4; // The unwind data stores the actual offset divided by 4 (since the low 2 bits
                                          // of the actual offset is always zero)

            DWORD headerEpilogStartIndex = pEpi->GetStartIndex();

            if ((headerEpilogStartOffset > UW_MAX_EPILOG_START_OFFSET) ||
                (headerEpilogStartIndex > UW_MAX_EPILOG_START_INDEX))
            {
                IMPL_LIMITATION("unwind data too large");
            }

            DWORD epilogScopeWord = headerEpilogStartOffset | (headerEpilogStartIndex << 22);

            ufiPrologCodes.AddHeaderWord(epilogScopeWord);
        }
    }

    // The unwind code words are already here, following the header, so we're done!
}

void UnwindFragmentInfo::Reserve(bool isFunclet, bool isHotCode)
{
    assert(isHotCode || !isFunclet); // TODO-CQ: support hot/cold splitting in functions with EH

    MergeCodes();

    BOOL isColdCode = isHotCode ? FALSE : TRUE;

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

    // We don't support hot/cold splitting with EH, so if there is cold code, this
    // better not be a funclet!
    // TODO-CQ: support funclets in cold code

    noway_assert(isHotCode || funKind == CORJIT_FUNC_ROOT);

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
        assert(endOffset <= uwiComp->info.compTotalHotCodeSize);
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
    printf("%*s  ufiEmitLoc: 0x%08p\n", indent, "", dspPtr(ufiEmitLoc));
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
// The LOONGARCH Exception Data specification "Function Fragments" section describes this.
// We split the function so that it is no larger than 512K bytes, or the value of
// the COMPlus_JitSplitFunctionSize value, if defined (and smaller). We must determine
// how to split the function/funclet before we issue the instructions, so we can
// reserve the unwind space with the VM. The instructions issued may shrink (but not
// expand!) during issuing (although this is extremely rare in any case, and may not
// actually occur on LOONGARCH), so we don't finalize actual sizes or offsets.
//
// LOONGARCH64 has very similar limitations, except functions can be up to 1MB. TODO-LOONGARCH64-Bug?: make sure this
// works!
//
// We don't split any prolog or epilog. Ideally, we might not split an instruction,
// although that doesn't matter because the unwind at any point would still be
// well-defined.

void UnwindInfo::Split()
{
    UNATIVE_OFFSET maxFragmentSize; // The maximum size of a code fragment in bytes

    maxFragmentSize = UW_MAX_FRAGMENT_SIZE_BYTES;

#ifdef DEBUG
    // Consider COMPlus_JitSplitFunctionSize
    unsigned splitFunctionSize = (unsigned)JitConfig.JitSplitFunctionSize();

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
    // It might be fewer if the COMPlus_JitSplitFunctionSize was used, but it better not
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
        // this fragment into the unwind data! If you set COMPlus_JitSplitFunctionSize to something
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
    assert(isHotCode || !isFunclet);

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

#endif // TARGET_LOONGARCH64
