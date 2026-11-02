// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Debug-only disassembler for the interpreter bytecode produced by codegeninterp.cpp.
// The opcode tables are generated from the shared intops.def, and the output format
// matches the interpreter library's own dump (DumpCompiledIns / DumpCompiledCode in
// interpreter/compiler.cpp).

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef DEBUG

#include "intopsshared.h"
#include "interpdump.h"

static const char* const s_interpOpNames[] = {
#define OPDEF(name, str, len, dvars, svars, argType) str,
#include "intops.def"
#undef OPDEF
};

static const uint8_t s_interpOpLen[] = {
#define OPDEF(name, str, len, dvars, svars, argType) (uint8_t)(len),
#include "intops.def"
#undef OPDEF
};

static const int s_interpOpDVars[] = {
#define OPDEF(name, str, len, dvars, svars, argType) (dvars),
#include "intops.def"
#undef OPDEF
};

static const int s_interpOpSVars[] = {
#define OPDEF(name, str, len, dvars, svars, argType) (svars),
#include "intops.def"
#undef OPDEF
};

// The byte offset of an instruction, accounting for the InterpByteCodeStart header that
// precedes the opcodes (ConvertOffset in interpreter/compiler.cpp).
static uint32_t interpConvertOffset(int32_t offset)
{
    return offset * sizeof(int32_t) + sizeof(void*);
}

// Print the trailing immediates of an instruction. Branches encode their target as a
// relative offset in the last slot.
static void dumpInterpInsData(int32_t insOffset, const int32_t* p, const int32_t* insEnd, int32_t opcode)
{
    const bool isBranch = (opcode == INTOP_BR) || ((opcode >= INTOP_BRFALSE_I4) && (opcode <= INTOP_BLT_UN_R8));
    if (isBranch && (p < insEnd))
    {
        for (; p < insEnd - 1; p++)
        {
            printf(" %d", *p);
        }
        printf(" IR_%04x", insOffset + *p);
    }
    else
    {
        for (; p < insEnd; p++)
        {
            printf(" %d", *p);
        }
    }
}

static void dumpInterpInst(const int32_t* codeBase, const int32_t* ip)
{
    const int32_t opcode    = *ip;
    const int32_t insOffset = (int32_t)(ip - codeBase);

    printf("%04x: IR_%04x: %-14s", (int32_t)interpConvertOffset(insOffset), insOffset, s_interpOpNames[opcode]);

    const int32_t* p = ip + 1;
    if (s_interpOpDVars[opcode] > 0)
    {
        printf(" [%d <-", *p++);
    }
    else
    {
        printf(" [nil <-");
    }

    if (s_interpOpSVars[opcode] > 0)
    {
        for (int i = 0; i < s_interpOpSVars[opcode]; i++)
        {
            printf(" %d", *p++);
        }
        printf("],");
    }
    else
    {
        printf(" nil],");
    }

    const int32_t  len    = s_interpOpLen[opcode];
    const int32_t* insEnd = ip + ((len != 0) ? len : 1);
    dumpInterpInsData(insOffset, p, insEnd, opcode);
    printf("\n");
}

void dumpInterpCode(const int32_t* code, int32_t codeSizeSlots)
{
    const int32_t* ip  = code;
    const int32_t* end = code + codeSizeSlots;
    while (ip < end)
    {
        dumpInterpInst(code, ip);
        const int32_t len = s_interpOpLen[*ip];
        ip += (len != 0) ? len : 1;
    }
    printf("End of method: %04x: IR_%04x\n", (int32_t)interpConvertOffset((int32_t)(ip - code)), (int32_t)(ip - code));
}

#endif // DEBUG
