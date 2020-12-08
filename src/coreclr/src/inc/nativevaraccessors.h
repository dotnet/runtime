// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// The following are used to read and write data given NativeVarInfo
// for primitive types. Don't use these for VALUECLASSes.
//*****************************************************************************


#ifndef _NATIVE_VAR_ACCESSORS_H_
#define _NATIVE_VAR_ACCESSORS_H_

#include "corjit.h"

bool operator ==(const ICorDebugInfo::VarLoc &varLoc1,
                 const ICorDebugInfo::VarLoc &varLoc2);

#define MAX_NATIVE_VAR_LOCS 2

SIZE_T GetRegOffsInCONTEXT(ICorDebugInfo::RegNum regNum);

struct NativeVarLocation
{
    ULONG64 addr;
    TADDR size;
    bool contextReg;
};

ULONG NativeVarLocations(const ICorDebugInfo::VarLoc &   varLoc,
                         PT_CONTEXT                      pCtx,
                         ULONG                           numLocs,
                         NativeVarLocation*              locs);

SIZE_T *NativeVarStackAddr(const ICorDebugInfo::VarLoc &   varLoc,
                           PT_CONTEXT                      pCtx);

bool    GetNativeVarVal(const ICorDebugInfo::VarLoc &   varLoc,
                        PT_CONTEXT                      pCtx,
                        SIZE_T                      *   pVal1,
                        SIZE_T                      *   pVal2
                        BIT64_ARG(SIZE_T                cbSize));

bool    SetNativeVarVal(const ICorDebugInfo::VarLoc &   varLoc,
                        PT_CONTEXT                      pCtx,
                        SIZE_T                          val1,
                        SIZE_T                          val2
                        BIT64_ARG(SIZE_T                cbSize));
#endif // #ifndef _NATIVE_VAR_ACCESSORS_H_
