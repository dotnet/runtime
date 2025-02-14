// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "intops.h"

#include <stddef.h>

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

const uint8_t* InterpNextOp(const uint8_t *ip)
{
    int len = g_interpOpLen[*ip];
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
