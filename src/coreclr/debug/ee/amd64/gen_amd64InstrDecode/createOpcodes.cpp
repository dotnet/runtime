// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>

int main(int argc, char*argv[])
{
    printf("#include <stdio.h>\n");
    printf("#include <inttypes.h>\n");

    const char* postamble = "0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,\n";

    printf("uint8_t opcodes[] = {\n");

    printf("// Primary Opcode\n");
    for (int i = 0; i < 256; ++i)
    {
        int last = 64;
        switch(i)
        {
            case 0x0f: // Secondary Escape
            case 0x26: // ES
            case 0x2e: // CS
            case 0x36: // SS
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
            case 0xc4: // Vex 1 Byte
            case 0xc5: // VEx 2 Byte
            case 0xf0: // Lock
            case 0xf2: // Repne
            case 0xf3: // Rep
                continue;
            case 0x8f: // XOP except modrm.reg == 0
                last = 8;
                break;
            default:
                break;
        };
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

    const char* const ppString[] = {"", "0x66, ", "0xf3, ", "0xf2, "};
    printf("// Secondary Opcode\n");
    for (int i = 0; i < 256; ++i)
    {
        if (i == 0xf)
            continue;
        if (i == 0x38)
            continue;
        if (i == 0x3A)
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

    printf("// 3D Now\n");
    for (int i = 0; i < 256; ++i)
    {
        printf( "0x0f, 0x0f, 0x05, 0x50, 0x51, 0x52, 0x53, 0x%02x, %s", i, postamble);
        // REX
        printf( "0x40, 0x0f, 0x0f, 0x05, 0x50, 0x51, 0x52, 0x53, 0x%02x, %s", i, postamble);
        // REX.WRXB
        printf( "0x4f, 0x0f, 0x0f, 0x05, 0x50, 0x51, 0x52, 0x53, 0x%02x, %s", i, postamble);
        printf("\n");
    }

    printf("// Ox0f_0x38\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            bool rowF = ((i & 0xF0) == 0xF);
            if (pp == 2) continue;
            if ((pp == 3) & (!rowF)) continue;
            if ((pp == 1) & (rowF)) continue;

            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                printf( "%s0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                if (rowF)
                    printf( "0x66, %s0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX
                printf( "%s0x40, 0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                if (rowF)
                    printf( "0x66, %s0x40, 0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                // REX.WRXB
                printf( "%s0x4f, 0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
                if (rowF)
                    printf( "0x66, %s0x4f, 0x0f, 0x38, 0x%02x, 0x%02x, %s", ppString[pp], i, modrm, postamble);
            }
        }
        printf("\n");
    }

    printf("// Ox0f_0x3A\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 2; ++pp)
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

    int byte3cases[] = { 0x00, 0x04, 0x78, 0x7C, 0x80, 0x84, 0xF8, 0xFC };

    printf("// VEX1\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0xc4, 0x01, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
                }
            }
        }
        printf("\n");
    }

    printf("// VEX2\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0xc4, 0x02, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
                }
            }
        }
        printf("\n");
    }

    printf("// VEX3\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0xc4, 0x03, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
                }
            }
        }
        printf("\n");
    }

    printf("// XOP8\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0x8f, 0x08, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
                }
            }
        }
        printf("\n");
    }

    printf("// XOP9\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0x8f, 0x09, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
                }
            }
        }
        printf("\n");
    }

    printf("// XOPA\n");
    for (int i = 0; i < 256; ++i)
    {
        for (int pp = 0; pp < 4; ++pp)
        {
            for (int modrm = 0x5; modrm < 64; modrm += 8)
            {
                for (int c = 0; c < sizeof(byte3cases)/sizeof(byte3cases[0]); ++c)
                {
                    printf( "0x8f, 0x0A, 0x%02x, 0x%02x, 0x%02x, %s", pp + byte3cases[c],   i, modrm, postamble);
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
