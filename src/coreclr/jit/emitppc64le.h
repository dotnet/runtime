// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_POWERPC64)

// The POWERPC64 instructions are all 32 bits in size.
// we use an unsigned int to hold the encoded instructions.
// This typedef defines the type that we use to hold encoded instructions.

//TODO POWERPC64

inline static bool isFloatReg(regNumber reg)
{
    abort();
}

inline static bool isGeneralRegister(regNumber reg)
{
    abort();
} // Excludes REG_ZR

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);


#endif
