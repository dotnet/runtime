// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>

#define ARRAYSIZE(a) (sizeof(a)/sizeof((a)[0]))

void generatePostamble(int bytesEmitted)
{
    // We need a postamble of single-byte instructions so the disassembler can get back on track
    // after a bad instruction. We always pad up to 16 bytes total codes: the maximum x86 instruction
    // size is 15, so the disassembler will find at worst a 15 byte instruction followed by a single byte
    // padding instruction. The minimum byte sequence we generate below is a single opcode plus a modrm,
    // so we need 14 possible postamble/padding bytes.
    const char* postamble[] = {
        "0x50, ",
        "0x51, ",
        "0x52, ",
        "0x53, ",
        "0x54, ",
        "0x55, ",
        "0x56, ",
        "0x57, ",
        "0x58, ",
        "0x59, ",
        "0x59, ",
        "0x59, ",
        "0x59, ",
        "0x59, "
    };

    int bytesToEmit = 16 - bytesEmitted;
    for (int i = 0; i < bytesToEmit; i++)
    {
        printf("%s", postamble[i]);
    }
    printf("\n");
}

int main(int argc, char* argv[])
{
    printf("#include <stdio.h>\n");
    printf("#include <inttypes.h>\n");

    // The sequence of generated codes is important: the tool which reads the disassembled instructions
    // processes one "opcode" at a time. When the opcode changes, it summarizes the opcode and moves on
    // to process the next one. Here, "opcode" means a single instruction. In the x64 encoding, this
    // can be determined by the primary opcode byte, the prefix (0x66, 0xF2, 0xF3) or equivalent "pp" field
    // in the VEX/EVEX prefix, and the "reg/opcode" field of the ModRM byte, which sometimes provides additional
    // "opcode" bits.
    //
    // When generating codes, for every primary opcode, we output the following ModRM bytes:
    //    0x05, 0x0d, 0x15, 0x1d, 0x25, 0x2d, 0x35, 0x3d
    // this corresponds to modrm.rm=0x5 and modrm.reg=0,1,2,3,4,5,6,7. That is, all possible modrm.reg values.
    // modrm.mod=0/modrm.rm=0x5 corresponds to RIP-relative addressing. The purpose of varying modrm.reg
    // is to find all cases where an instruction encoding depends on modrm.reg.
    //
    // Thus, the 'modrm' loop needs to be less nested than the opcode/prefix loop, since varying modrm
    // can change the "instruction".
    //
    // Note: it might be more robust to not have this ordering restriction but that would require the
    // processing tool to save all in-progress calculations, for all instructions -- perhaps using a
    // lot of memory?

    printf("uint8_t opcodes[] = {\n");

    struct byteSequence {
        const char* string;
        int numBytes;
    };

    // Opcodes in legacy map 0 don't change the instruction based on the 0x66 prefix (unlike in
    // other maps), so the 0x66 prefix can vary inside (in a more nested loop) the modrm loop.
    const byteSequence legacyMap0PrefixStrings[] = {
        { "", 0 },
        { "0x66, ", 1 },                // Operand size prefix 0x66
        { "0x40, ", 1 },                // REX
        { "0x66, 0x40, ", 2 },          // Operand size prefix 0x66 + REX
        { "0x4F, ", 1 },                // REX.WRXB
        { "0x66, 0x4F, ", 2 },          // Operand size prefix 0x66 + REX.WRXB
        { "0xD5, 0x00, ", 2 },          // REX2.M0=0.R4=0.X4=0.B4=0.W=0.R3=0.X3=0.B3=0
        { "0x66, 0xD5, 0x00, ", 3 },    // Operand size prefix 0x66 + REX2.M0=0.R4=0.X4=0.B4=0.W=0.R3=0.X3=0.B3=0
        { "0xD5, 0x7F, ", 2 },          // REX2.M0=0.R4=1.X4=1.B4=1.W=1.R3=1.X3=1.B3=1
        { "0x66, 0xD5, 0x7F, ", 3 }     // Operand size prefix 0x66 + REX2.M0=0.R4=1.X4=1.B4=1.W=1.R3=1.X3=1.B3=1
    };

    printf("// Primary Opcode (legacy map 0)\n");
    for (int i = 0; i < 256; ++i)
    {
        switch(i)
        {
            case 0x0f: // Secondary Escape
            case 0x26: // ES
            case 0x2e: // CS
            case 0x36: // SS
            case 0x3e: // DS
            case 0x40: // REX
            case 0x41: // REX
            case 0x42: // REX
            case 0x43: // REX
            case 0x44: // REX
            case 0x45: // REX
            case 0x46: // REX
            case 0x47: // REX
            case 0x48: // REX
            case 0x49: // REX
            case 0x4A: // REX
            case 0x4B: // REX
            case 0x4C: // REX
            case 0x4D: // REX
            case 0x4E: // REX
            case 0x4F: // REX
            case 0x62: // EVEX
            case 0x64: // FS
            case 0x65: // GS
            case 0x66: // OpSize
            case 0x67: // AddrSize
            case 0xc4: // Vex 3 Byte
            case 0xc5: // Vex 2 Byte
            case 0xd5: // REX2
            case 0xf0: // Lock
            case 0xf2: // Repne
            case 0xf3: // Rep
                continue;
            default:
                break;
        }

        for (int modrm = 0x5; modrm < 64; modrm += 8)
        {
            for (int prefixNum = 0; prefixNum < ARRAYSIZE(legacyMap0PrefixStrings); ++prefixNum)
            {
                printf("%s0x%02x, 0x%02x, ", legacyMap0PrefixStrings[prefixNum].string, i, modrm);
                generatePostamble(legacyMap0PrefixStrings[prefixNum].numBytes + 2);
            }
        }
        printf("\n");
    }

    // `66 F2` is only used for `0F 38 F*` ("row F")
    const byteSequence ppString[] = {
        { "", 0 },
        { "0x66, ", 1 },
        { "0xf3, ", 1 },
        { "0xf2, ", 1 },
        { "0x66, 0xf2, ", 2 }
    };

    const byteSequence legacyMap1PrefixStrings[] = {
        { "0x0F, ", 1 },            // Escape prefix
        { "0x40, 0x0F, ", 2 },      // REX
        { "0x4F, 0x0F, ", 2 },      // REX.WRXB
        { "0xD5, 0x80, ", 2 },      // REX2.M0=1.R4=0.X4=0.B4=0.W=0.R3=0.X3=0.B3=0
        { "0xD5, 0xFF, ", 2 }       // REX2.M0=1.R4=1.X4=1.B4=1.W=1.R3=1.X3=1.B3=1
    };

    printf("// Secondary Opcode: 0F (legacy map 1)\n");
    for (int i = 0; i < 256; ++i)
    {
        if (i == 0x38) // extension: 0F 38
            continue;
        if (i == 0x3A) // extension: 0F 3A
            continue;

        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int prefixNum = 0; prefixNum < ARRAYSIZE(legacyMap1PrefixStrings); ++prefixNum)
                {
                    printf("%s%s0x%02x, 0x%02x, ", ppString[pp].string, legacyMap1PrefixStrings[prefixNum].string, i, modrm);
                    generatePostamble(ppString[pp].numBytes + legacyMap1PrefixStrings[prefixNum].numBytes + 2);
                }
            }
        }
        printf("\n");
    }

    const byteSequence legacyMap2PrefixStrings[] = {
        { "0x0F, 0x38, ", 2 },
        { "0x40, 0x0F, 0x38, ", 3 },    // REX
        { "0x4F, 0x0F, 0x38, ", 3 }     // REX.WRXB
    };

    printf("// 0F 38 (legacy map 2)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 5; ++pp)
        {
            const bool rowF = ((i & 0xF0) == 0xF0);
            if ((pp == 2) && (!rowF)) continue; // No F3 prefix instructions except Fx row
            if ((pp == 3) && (!rowF)) continue; // No F2 prefix instructions except Fx row

            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int prefixNum = 0; prefixNum < ARRAYSIZE(legacyMap2PrefixStrings); ++prefixNum)
                {
                    printf("%s%s0x%02x, 0x%02x, ", ppString[pp].string, legacyMap2PrefixStrings[prefixNum].string, i, modrm);
                    generatePostamble(ppString[pp].numBytes + legacyMap2PrefixStrings[prefixNum].numBytes + 2);
                }
            }
        }
        printf("\n");
    }

    const byteSequence legacyMap3PrefixStrings[] = {
        { "0x0F, 0x3A, ", 2 },
        { "0x40, 0x0F, 0x3A, ", 3 },    // REX
        { "0x4F, 0x0F, 0x3A, ", 3 }     // REX.WRXB
    };

    printf("// 0F 3A (legacy map 3)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 2; ++pp) // only 66 prefix is used (no F3, F2) (F2 is used in VEX 0F 3A)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int prefixNum = 0; prefixNum < ARRAYSIZE(legacyMap3PrefixStrings); ++prefixNum)
                {
                    printf("%s%s0x%02x, 0x%02x, ", ppString[pp].string, legacyMap3PrefixStrings[prefixNum].string, i, modrm);
                    generatePostamble(ppString[pp].numBytes + legacyMap3PrefixStrings[prefixNum].numBytes + 2);
                }
            }
        }
        printf("\n");
    }

    // Interesting cases for byte 2 (3rd byte) of 3-byte VEX prefix. Note that all cases of `pp` are added
    // below, in the loops.
    int VexByte2Cases[] =
    {
        0x00, // VEX.W = 0, VEX.vvvv = 0000b (xmm15),  VEX.L = 0
        0x04, // VEX.W = 0, VEX.vvvv = 0000b (xmm15),  VEX.L = 1
        0x78, // VEX.W = 0, VEX.vvvv = 1111b (unused), VEX.L = 0
        0x7C, // VEX.W = 0, VEX.vvvv = 1111b (unused), VEX.L = 1
        0x80, // VEX.W = 1, VEX.vvvv = 0000b (xmm15),  VEX.L = 0
        0x84, // VEX.W = 1, VEX.vvvv = 0000b (xmm15),  VEX.L = 1
        0xF8, // VEX.W = 1, VEX.vvvv = 1111b (unused), VEX.L = 0
        0xFC  // VEX.W = 1, VEX.vvvv = 1111b (unused), VEX.L = 1
    };

    // Note that 2-byte VEX is equivalent to 3-byte VEX with `mmmmm` == 00001b (implied 0F leading opcode byte)
    // since we don't care about R, X, B bits.

    printf("// VEX1: VEX.R=1 (inverted), VEX.X=1 (inverted), VEX.B=1 (inverted), VEX.mmmmm=00001 (0F)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < ARRAYSIZE(VexByte2Cases); ++c)
                {
                    printf("0xc4, 0xe1, 0x%02x, 0x%02x, 0x%02x, ", pp + VexByte2Cases[c], i, modrm);
                    generatePostamble(5);
                }
            }
        }
        printf("\n");
    }

    printf("// VEX2: VEX.R=1 (inverted), VEX.X=1 (inverted), VEX.B=1 (inverted), VEX.mmmmm=00010 (0F 38)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < ARRAYSIZE(VexByte2Cases); ++c)
                {
                    printf("0xc4, 0xe2, 0x%02x, 0x%02x, 0x%02x, ", pp + VexByte2Cases[c], i, modrm);
                    generatePostamble(5);
                }
            }
        }
        printf("\n");
    }

    printf("// VEX3: VEX.R=1 (inverted), VEX.X=1 (inverted), VEX.B=1 (inverted), VEX.mmmmm=00011 (0F 3A)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < ARRAYSIZE(VexByte2Cases); ++c)
                {
                    printf("0xc4, 0xe3, 0x%02x, 0x%02x, 0x%02x, ", pp + VexByte2Cases[c], i, modrm);
                    generatePostamble(5);
                }
            }
        }
        printf("\n");
    }

    // Interesting cases for the EVEX prefix. Several cases are added below, in the loops, to ensure desired
    // ordering:
    // 1. cases of `mmm` (which defines the opcode decoding map) are the outer loops.
    // 2. one-byte instruction opcode, next inner loops.
    // 3. cases of `pp`, next inner loops.
    // 4. cases of ModR/M byte, next inner loops.
    // 5. various EVEX cases, innermost loops.
    // NOTE: 4 & 5 can probably (and possibly should, for consistency with above loops) be swapped.
    //
    // In all cases, we have:
    //    P0:
    //      P[3] = P0[3] = 0 // required by specification
    //         -- For APX, mmm=0b100, P[3] = B4, 0 is ok
    //      EVEX.R'=1 (inverted)
    //         -- For APX, mmm=0b100, EVEX.R' = EVEX.R4 (inverted) = P[4]. 1 (inverted value) is ok
    //      EVEX.RXB=111 (inverted)
    //         -- For APX, mmm=0b100, EVEX.RXB (inverted) = EVEX.R3.X3.B3 (inverted), so 111 is ok.
    //    P1:
    //      P[10] = P1[2] = 1 // required by specification
    //         -- For APX, mmm=0b100, EVEX.X4/1 (inverted) so 1 is ok
    //    P2:
    //      P[18:16] = P2[2:0] = EVEX.aaa = 0 // opmask register k0 (no masking)
    //         -- For APX, mmm=0b100, P2[0] = P2[1] = 0, P2[2] = NF = 0 (same as non-APX)
    //      P[19] = P2[3] = EVEX.V'=1 (inverted)
    //         -- For APX, mmm=0b100, EVEX.V' = EVEX.V4 (inverted), so 1 is ok.
    //      P[23] = P2[7] = EVEX.z=0 // always merge
    //         -- For APX, mmm=0b100, P[23] = 0.
    //
    // Note that we don't need to consider disp8*N compressed displacement support since that is not used for
    // RIP-relative addressing, which is all we care about.

    const int evex_p0_base = 0xf0;
    const int evex_p1_base = 0x04;
    const int evex_p2_base = 0x08;

    const int evex_4_p0_base = 0xf0;
    const int evex_4_p1_base = 0x7c;
    const int evex_4_p2_base = 0x08;

    const int evex_w_cases[] = // EVEX.W in P1
    {
        0,
        1 << 7,
    };
    const size_t evex_w_cases_size = ARRAYSIZE(evex_w_cases);

    // For APX, mmm=0b100, EVEX.vvvv is used to store NDD register if EVEX.ND=1. We never set EVEX.ND=1
    // since it doesn't affect instruction size or RIP-relative memory information.
    const int evex_vvvv_cases[] = // EVEX.vvvv in P1
    {
        0,       // 0000b (xmm15)
        0xf << 3 // 1111b (unused)
    };
    const size_t evex_vvvv_cases_size = ARRAYSIZE(evex_vvvv_cases);

    // For APX, mmm=0b100, P[22:21] = P2[6:5] = EVEX.L'L and must be zero.
    const int evex_LprimeL_cases[] = // EVEX.L'L in P2
    {
        0,        // 00b = 128-bit vectors
        0x1 << 5, // 01b = 256-bit vectors
        0x2 << 5  // 10b = 512-bit vectors
    };
    const size_t evex_LprimeL_cases_size = ARRAYSIZE(evex_LprimeL_cases);

    // -- For APX, mmm=0b100, P[20] = P2[4] = EVEX.b = EVEX.ND, so we keep it zero
    const int evex_b_cases[] = // EVEX.b in P2
    {
        0,       // 0b = no broadcast
        0x1 << 4 // 1b = embedded broadcast
    };
    const size_t evex_b_cases_size = ARRAYSIZE(evex_b_cases);

    const size_t total_evex_cases   = evex_w_cases_size * evex_vvvv_cases_size * evex_LprimeL_cases_size * evex_b_cases_size;
    const size_t total_evex_4_cases = evex_w_cases_size;

    struct EvexBytes
    {
        int p0, p1, p2;
    };

    EvexBytes EvexCases[total_evex_cases];      // cases for mmm=0b001, 0b010, 0b011
    EvexBytes Evex4Cases[total_evex_4_cases];   // cases for mmm=0b100

    size_t evex_case = 0;
    for (size_t i = 0; i < evex_w_cases_size; i++)
    {
        for (size_t j = 0; j < evex_vvvv_cases_size; j++)
        {
            for (size_t k = 0; k < evex_LprimeL_cases_size; k++)
            {
                for (size_t l = 0; l < evex_b_cases_size; l++)
                {
                    EvexCases[evex_case].p0 = evex_p0_base;
                    EvexCases[evex_case].p1 = evex_p1_base | evex_w_cases[i] | evex_vvvv_cases[j];
                    EvexCases[evex_case].p2 = evex_p2_base | evex_b_cases[l] | evex_LprimeL_cases[k];
                    ++evex_case;
                }
            }
        }
    }

    evex_case = 0;
    for (size_t i = 0; i < evex_w_cases_size; i++)
    {
        Evex4Cases[evex_case].p0 = evex_4_p0_base;
        Evex4Cases[evex_case].p1 = evex_4_p1_base | evex_w_cases[i];
        Evex4Cases[evex_case].p2 = evex_4_p2_base;
        ++evex_case;
    }

    printf("// EVEX: mmm=001 (0F)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < ARRAYSIZE(EvexCases); ++c)
                {
                    int evex_p0 = EvexCases[c].p0 | 0x1; // mmm=001 (0F)
                    int evex_p1 = EvexCases[c].p1 | pp;
                    int evex_p2 = EvexCases[c].p2;
                    printf("0x62, 0x%02x, 0x%02x, 0x%02x, 0x%02x, 0x%02x, ", evex_p0, evex_p1, evex_p2, i, modrm);
                    generatePostamble(6);
                }
            }
        }
        printf("\n");
    }

    printf("// EVEX: mmm=010 (0F 38)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < ARRAYSIZE(EvexCases); ++c)
                {
                    int evex_p0 = EvexCases[c].p0 | 0x2; // mmm=010 (0F 38)
                    int evex_p1 = EvexCases[c].p1 | pp;
                    int evex_p2 = EvexCases[c].p2;
                    printf("0x62, 0x%02x, 0x%02x, 0x%02x, 0x%02x, 0x%02x, ", evex_p0, evex_p1, evex_p2, i, modrm);
                    generatePostamble(6);
                }
            }
        }
        printf("\n");
    }

    printf("// EVEX: mmm=011 (0F 3A)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < ARRAYSIZE(EvexCases); ++c)
                {
                    int evex_p0 = EvexCases[c].p0 | 0x3; // mmm=011 (0F 3A)
                    int evex_p1 = EvexCases[c].p1 | pp;
                    int evex_p2 = EvexCases[c].p2;
                    printf("0x62, 0x%02x, 0x%02x, 0x%02x, 0x%02x, 0x%02x, ", evex_p0, evex_p1, evex_p2, i, modrm);
                    generatePostamble(6);
                }
            }
        }
        printf("\n");
    }

    printf("// EVEX: mmm=100 (extended EVEX; APX promoted legacy map 0 instructions)\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < ARRAYSIZE(Evex4Cases); ++c)
                {
                    int evex_p0 = Evex4Cases[c].p0 | 0x4; // mmm=100
                    int evex_p1 = Evex4Cases[c].p1 | pp;
                    int evex_p2 = Evex4Cases[c].p2;
                    printf("0x62, 0x%02x, 0x%02x, 0x%02x, 0x%02x, 0x%02x, ", evex_p0, evex_p1, evex_p2, i, modrm);
                    generatePostamble(6);
                }
            }
        }
        printf("\n");
    }

    printf("0\n");
    printf("};\n");
    printf("\n");
    printf("int main(int argc, char*argv[])\n");
    printf("{\n");
        printf("for (size_t i = 0; i < sizeof(opcodes) ; ++i) printf(\"opcodes[i] = 0x%%02x\\n\", opcodes[i]);");
    printf("};\n");
}
