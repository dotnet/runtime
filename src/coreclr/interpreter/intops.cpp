// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "intops.h"

#include <stddef.h>
#include <assert.h>

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
struct InterpOpNameCharacters
{
#define OPDEF(a,b,c,d,e,f) char a[sizeof(b)];
#include "intops.def"
#undef OPDEF
};

const struct InterpOpNameCharacters g_interpOpNameCharacters = {
#define OPDEF(a,b,c,d,e,f) b,
#include "intops.def"
#undef OPDEF
};

const uint32_t g_interpOpNameOffsets[] = {
#define OPDEF(a,b,c,d,e,f) offsetof(InterpOpNameCharacters, a),
#include "intops.def"
#undef OPDEF
};

const uint8_t g_interpOpLen[] = {
#define OPDEF(a,b,c,d,e,f) c,
#include "intops.def"
#undef OPDEF
};

const int g_interpOpSVars[] = {
#define OPDEF(a,b,c,d,e,f) e,
#include "intops.def"
#undef OPDEF
};

const int g_interpOpDVars[] = {
#define OPDEF(a,b,c,d,e,f) d,
#include "intops.def"
#undef OPDEF
};

const InterpOpArgType g_interpOpArgType[] = {
#define OPDEF(a,b,c,d,e,f) f,
#include "intops.def"
#undef OPDEF
};

const int32_t* InterpNextOp(const int32_t *ip)
{
    int len = g_interpOpLen[*ip];
    if (len == 0)
    {
        assert(*ip == INTOP_SWITCH);
        len = 3 + ip[2];
    }

    return ip + len;
}

const char* InterpOpName(int op)
{
    return ((const char*)&g_interpOpNameCharacters) + g_interpOpNameOffsets[op];
}

// Information about IL opcodes

OPCODE_FORMAT const g_CEEOpArgs[] = {
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) args,
#include "opcode.def"
#undef OPDEF
};

struct CEEOpNameCharacters
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) char c[sizeof(s)];
#include "opcode.def"
#undef OPDEF
};

const struct CEEOpNameCharacters g_CEEOpNameCharacters = {
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) s,
#include "opcode.def"
#undef OPDEF
};

const uint32_t g_CEEOpNameOffsets[] = {
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) offsetof(CEEOpNameCharacters, c),
#include "opcode.def"
#undef OPDEF
};

const char* CEEOpName(OPCODE op)
{
    return ((const char*)&g_CEEOpNameCharacters) + g_CEEOpNameOffsets[op];
}

// Also updates ip to skip over prefix, if any
OPCODE CEEDecodeOpcode(const uint8_t **pIp)
{
    OPCODE res;
    const uint8_t *ip = *pIp;

    if (*ip == 0xFE)
    {
        // Double byte encoding, offset
        ip++;
        res = (OPCODE)(*ip + CEE_ARGLIST);
    }
    else
    {
        res = (OPCODE)*ip;
    }
    *pIp = ip;
    return res;
}

int32_t CEEOpcodeSize(const uint8_t *ip, const uint8_t *codeEnd)
{
    const uint8_t *p = ip;
    OPCODE opcode = CEEDecodeOpcode(&p);
    OPCODE_FORMAT opArgs = g_CEEOpArgs[opcode];

    size_t size = 0;

    switch (opArgs)
    {
    case InlineNone:
        size = 1;
        break;
    case InlineString:
    case InlineType:
    case InlineField:
    case InlineMethod:
    case InlineTok:
    case InlineSig:
    case ShortInlineR:
    case InlineI:
    case InlineBrTarget:
        size = 5;
        break;
    case InlineVar:
        size = 3;
        break;
    case ShortInlineVar:
    case ShortInlineI:
    case ShortInlineBrTarget:
        size = 2;
        break;
    case InlineR:
    case InlineI8:
        size = 9;
        break;
    case InlineSwitch: {
        size_t entries = getI4LittleEndian(p + 1);
        size = 5 + 4 * entries;
        break;
    }
    default:
        assert(0);
    }

    if ((ip + size) >= codeEnd)
        return -1;

    return (int32_t)((p - ip) + size);
}
