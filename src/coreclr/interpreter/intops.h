// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTOPS_H
#define _INTOPS_H

#include "openum.h"
#include <stdint.h>

#include "intopsshared.h"

typedef enum
{
    InterpOpNoArgs,
    InterpOpInt,
    InterpOpLongInt,
    InterpOpFloat,
    InterpOpDouble,
    InterpOpTwoInts,
    InterpOpThreeInts,
    InterpOpBranch,
    InterpOpSwitch,
    InterpOpMethodHandle,
    InterpOpClassHandle,
    InterpOpGenericLookup,
    InterpOpLdPtr,
    InterpOpHelperFtn,
} InterpOpArgType;

extern const uint8_t g_interpOpLen[];
extern const int g_interpOpDVars[];
extern const int g_interpOpSVars[];
extern const InterpOpArgType g_interpOpArgType[];
extern const int32_t* InterpNextOp(const int32_t* ip);

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
extern const uint32_t g_interpOpNameOffsets[];
struct InterpOpNameCharacters;
extern const InterpOpNameCharacters g_interpOpNameCharacters;

const char* InterpOpName(int op);

extern OPCODE_FORMAT const g_CEEOpArgs[];
const char* CEEOpName(OPCODE op);
OPCODE CEEDecodeOpcode(const uint8_t **ip);
int CEEOpcodeSize(const uint8_t *ip, const uint8_t *codeEnd);

#ifdef TARGET_64BIT
#define INTOP_MOV_P INTOP_MOV_8
#define INTOP_LDNULL INTOP_LDC_I8_0
#define INTOP_LDIND_I INTOP_LDIND_I8
#define INTOP_STIND_I INTOP_STIND_I8
#define INTOP_ADD_P_IMM INTOP_ADD_I8_IMM
#define INTOP_LDELEM_I INTOP_LDELEM_I8
#define INTOP_STELEM_I INTOP_STELEM_I8
#else
#define INTOP_MOV_P INTOP_MOV_4
#define INTOP_LDNULL INTOP_LDC_I4_0
#define INTOP_LDIND_I INTOP_LDIND_I4
#define INTOP_STIND_I INTOP_STIND_I4
#define INTOP_ADD_P_IMM INTOP_ADD_I4_IMM
#define INTOP_LDELEM_I INTOP_LDELEM_I4
#define INTOP_STELEM_I INTOP_STELEM_I4
#endif

static inline bool InterpOpIsEmitNop(int32_t opcode)
{
    return opcode >= INTOP_NOP && opcode != INTOP_MOV_SRC_OFF;
}

static inline bool InterpOpIsUncondBranch(int32_t opcode)
{
    return opcode == INTOP_BR;
}

static inline bool InterpOpIsCondBranch(int32_t opcode)
{
    return opcode >= INTOP_BRFALSE_I4 && opcode <= INTOP_BLT_UN_R8;
}

// Helpers for reading data from uint8_t code stream
inline uint16_t getU2LittleEndian(const uint8_t* ptr)
{
    return *ptr | *(ptr + 1) << 8;
}

inline uint32_t getU4LittleEndian(const uint8_t* ptr)
{
    return *ptr | *(ptr + 1) << 8 | *(ptr + 2) << 16 | *(ptr + 3) << 24;
}

inline int16_t getI2LittleEndian(const uint8_t* ptr)
{
    return (int16_t)getU2LittleEndian(ptr);
}

inline int32_t getI4LittleEndian(const uint8_t* ptr)
{
    return (int32_t)getU4LittleEndian(ptr);
}

inline int64_t getI8LittleEndian(const uint8_t* ptr)
{
    return (int64_t)getU4LittleEndian(ptr) | ((int64_t)getI4LittleEndian(ptr + 4)) << 32;
}

inline float getR4LittleEndian(const uint8_t* ptr)
{
    int32_t val = getI4LittleEndian(ptr);
    return *(float*)&val;
}

inline double getR8LittleEndian(const uint8_t* ptr)
{
    int64_t val = getI8LittleEndian(ptr);
    return *(double*)&val;
}

#endif
