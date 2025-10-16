// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: primitives.h
//

//
// Platform-specific debugger primitives
//
//*****************************************************************************

#ifndef PRIMITIVES_H_
#define PRIMITIVES_H_

inline CORDB_ADDRESS GetPatchEndAddr(CORDB_ADDRESS patchAddr)
{
    _ASSERTE("The function is not implemented on wasm");
    return patchAddr;
}

typedef const BYTE                  CORDB_ADDRESS_TYPE;
typedef DPTR(CORDB_ADDRESS_TYPE)    PTR_CORDB_ADDRESS_TYPE;

//This is an abstraction to keep x86/ia64 patch data separate
#define PRD_TYPE                               USHORT

#define MAX_INSTRUCTION_LENGTH 2 // update once we have codegen

#define CORDbg_BREAK_INSTRUCTION_SIZE 1
#define CORDbg_BREAK_INSTRUCTION 0 // unreachable intruction

inline bool PRDIsEmpty(PRD_TYPE p1)
{
    LIMITED_METHOD_CONTRACT;

    return p1 == 0;
}

#endif
