// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>

int main(int argc, char*argv[])
{
    printf("#include <stdio.h>\n");
    printf("#include <inttypes.h>\n");

    // TODO: for EVEX will we ever need 16-byte postamble?
    const char* postamble = "0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,\n";

    printf("uint8_t opcodes[] = {\n");

    printf("// Primary Opcode\n");
    for (int i = 0; i < 256; ++i)
    {
        int last = 64; // REVIEW: this is unused
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
            case 0x64: // FS
            case 0x65: // GS
            case 0x66: // OpSize
            case 0x67: // AddrSize
            case 0xc4: // Vex 3 Byte
            case 0xc5: // Vex 2 Byte
            // TODO: add EVEX prefix 62
            case 0xf0: // Lock
            case 0xf2: // Repne
            case 0xf3: // Rep
                continue;
            case 0x8f: // XOP except modrm.reg == 0
                last = 8;
                break;
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
        if (i == 0xf) // extension: 0F 0F (XOP)
            continue;
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
    int byte3cases[] =
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
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0xc4, 0xe1, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
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
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0xc4, 0xe2, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
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
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0xc4, 0xe3, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
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
