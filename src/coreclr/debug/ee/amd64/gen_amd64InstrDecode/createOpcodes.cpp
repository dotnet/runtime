// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
                
#define ARRAYSIZE(a) (sizeof(a)/sizeof((a)[0]))

int main(int argc, char* argv[])
{
    printf("#include <stdio.h>\n");
    printf("#include <inttypes.h>\n");

    const char* postamble = "0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,\n";

    printf("uint8_t opcodes[] = {\n");

    printf("// Primary Opcode\n");
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
            case 0xf0: // Lock
            case 0xf2: // Repne
            case 0xf3: // Rep
                continue;
            default:
                break;
        }
        for (int modrm = 0x5; modrm < 64; modrm += 8)
        {
            printf( "0x%02x, 0x%02x, %s", i, modrm, postamble);
            printf( "0x66, 0x%02x, 0x%02x, %s", i, modrm, postamble);
            // REX
            printf( "0x40, 0x%02x, 0x%02x, %s", i, modrm, postamble);
            printf( "0x66, 0x40, 0x%02x, 0x%02x, %s", i, modrm, postamble);
            // REX.WRXB
            printf( "0x4f, 0x%02x, 0x%02x, %s", i, modrm, postamble);
            printf( "0x66, 0x4f, 0x%02x, 0x%02x, %s", i, modrm, postamble);
        }
        printf("\n");
    }

    // `66 F2` is only used for `0F 38 F*` ("row F")
    const char* const ppString[] = {"", "0x66, ", "0xf3, ", "0xf2, ", "0x66, 0xf2, "};

    printf("// Secondary Opcode\n");
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
                printf( "%s0x0f, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX
                printf( "0x40, %s0x0f, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX.WRXB
                printf( "0x4f, %s0x0f, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
            }
        }
        printf("\n");
    }

    printf("// 0F 38\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 5; ++pp)
        {
            const bool rowF = ((i & 0xF0) == 0xF0);
            if ((pp == 2) && (!rowF)) continue; // No F3 prefix instructions except Fx row
            if ((pp == 3) && (!rowF)) continue; // No F2 prefix instructions except Fx row

            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                printf( "%s0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX
                printf( "%s0x40, 0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX.WRXB
                printf( "%s0x4f, 0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
            }
        }
        printf("\n");
    }

    printf("// 0F 3A\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 2; ++pp) // only 66 prefix is used (no F3, F2) (F2 is used in VEX 0F 3A)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                printf( "%s0x0f, 0x3A, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX
                printf( "%s0x40, 0x0f, 0x3A, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX.WRXB
                printf( "%s0x4f, 0x0f, 0x3A, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
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
                    printf( "0xc4, 0xe1, 0x%02x, 0x%02x, 0x%02x, %s", pp + VexByte2Cases[c],   i, modrm, postamble);
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
                    printf( "0xc4, 0xe2, 0x%02x, 0x%02x, 0x%02x, %s", pp + VexByte2Cases[c],   i, modrm, postamble);
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
                    printf( "0xc4, 0xe3, 0x%02x, 0x%02x, 0x%02x, %s", pp + VexByte2Cases[c],   i, modrm, postamble);
                }
            }
        }
        printf("\n");
    }

    // Interesting cases for the EVEX prefix. Several cases are added below, in the loops, to ensure desired
    // ordering:
    // 1. cases of `mmm` (which defines the opcode decoding map) are the outer loops.
    // 2. cases of `pp`, next inner loops.
    // 3. cases of ModR/M byte, innermost loops.
    //
    // In all cases, we have:
    //    P0:
    //      P[3] = P0[3] = 0 // required by specification
    //      EVEX.R'=1 (inverted)
    //      EVEX.RXB=111 (inverted)
    //    P1:
    //      P[10] = P1[2] = 1 // required by specification
    //    P2:
    //      EVEX.aaa = 0 // opmask register k0 (no masking)
    //      EVEX.V'=1 (inverted)
    //      EVEX.b=0 // no broadcast (REVIEW: need to handle broadcast as it changes the size of the memory operand)
    //      EVEX.z=0 // always merge
    //
    // Note that we don't need to consider disp8*N compressed displacement support since that is not used for
    // RIP-relative addressing, which is all we care about.

    const int evex_p0_base = 0xf0;
    const int evex_p1_base = 0x04;
    const int evex_p2_base = 0x08;

    const int evex_w_cases[] = // EVEX.W in P1
    {
        0,
        1 << 7,
    };
    const size_t evex_w_cases_size = ARRAYSIZE(evex_w_cases);

    const int evex_vvvv_cases[] = // EVEX.vvvv in P1
    {
        0,       // 0000b (xmm15)
        0xf << 3 // 1111b (unused)
    };
    const size_t evex_vvvv_cases_size = ARRAYSIZE(evex_vvvv_cases);

    const int evex_LprimeL_cases[] = // EVEX.L'L in P2
    {
        0,        // 00b = 128-bit vectors
        0x1 << 5, // 01b = 256-bit vectors
        0x2 << 5  // 10b = 512-bit vectors
    };
    const size_t evex_LprimeL_cases_size = ARRAYSIZE(evex_LprimeL_cases);

    const int evex_b_cases[] = // EVEX.b in P2
    {
        0,       // 0b = no broadcast
        0x1 << 4 // 1b = embedded broadcast
    };
    const size_t evex_b_cases_size = ARRAYSIZE(evex_b_cases);

    const size_t total_evex_cases = evex_w_cases_size * evex_vvvv_cases_size * evex_LprimeL_cases_size * evex_b_cases_size;

    struct EvexBytes
    {
        int p0, p1, p2;
    }
    EvexCases[total_evex_cases];
    
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
                    printf( "0x62, 0x%02x, 0x%02x, 0x%02x, 0x%02x, 0x%02x, %s", evex_p0, evex_p1, evex_p2, i, modrm, postamble);
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
                    printf( "0x62, 0x%02x, 0x%02x, 0x%02x, 0x%02x, 0x%02x, %s", evex_p0, evex_p1, evex_p2, i, modrm, postamble);
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
                    printf( "0x62, 0x%02x, 0x%02x, 0x%02x, 0x%02x, 0x%02x, %s", evex_p0, evex_p1, evex_p2, i, modrm, postamble);
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
