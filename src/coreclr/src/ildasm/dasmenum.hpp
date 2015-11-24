//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "openum.h"

typedef struct
{
    const char *  pszName;
    
    OPCODE   Ref;   // reference codes
    
    BYTE    Type;   // Inline0 etc.

    BYTE    Len;    // std mapping
    BYTE    Std1;   
    BYTE    Std2;
} opcodeinfo_t;

#ifdef DECLARE_DATA
opcodeinfo_t OpcodeInfo[] =
{
#define OPDEF(c,s,pop,push,args,type,l,s1,s2,ctrl) { s,c,args,l,s1,s2 },
#include "opcode.def"
#undef OPDEF
};
#else
extern opcodeinfo_t OpcodeInfo[];
#endif


