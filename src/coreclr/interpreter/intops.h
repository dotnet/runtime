// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTOPS_H
#define _INTOPS_H

#include "openum.h"
#include <stdint.h>

typedef enum
{
    InterpOpNoArgs,
    InterpOpInt,
} InterpOpArgType;

extern const uint8_t g_interpOpLen[];
extern const int g_interpOpDVars[];
extern const int g_interpOpSVars[];
extern const InterpOpArgType g_interpOpArgType[];
extern const uint8_t* InterpNextOp(const uint8_t* ip);

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
extern const uint32_t g_interpOpNameOffsets[];
struct InterpOpNameCharacters;
extern const InterpOpNameCharacters g_interpOpNameCharacters;

const char* InterpOpName(int op);

extern OPCODE_FORMAT const g_CEEOpArgs[];
const char* CEEOpName(OPCODE op);
OPCODE CEEDecodeOpcode(const uint8_t **ip);

#ifdef TARGET_64BIT
#define INTOP_MOV_P INTOP_MOV_8
#else
#define INTOP_MOV_P INTOP_MOV_4
#endif

#endif
