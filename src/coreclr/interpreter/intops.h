// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTOPS_H
#define _INTOPS_H

#include <stdint.h>

typedef enum
{
    InterpOpNoArgs,
    InterpOpInt,
} InterpOpArgType;

extern uint8_t const g_interpOpLen[];
extern int const g_interpOpDVars[];
extern int const g_interpOpSVars[];
extern InterpOpArgType const g_interpOpArgType[];
extern const uint8_t* InterpNextOp(const uint8_t* ip);

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
extern const uint32_t g_interpOpNameOffsets[];
struct InterpOpNameCharacters;
extern const InterpOpNameCharacters g_interpOpNameCharacters;

const char* InterpOpName(int op);

#endif
