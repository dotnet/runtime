// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) Loongson Technology. All rights reserved.

//*****************************************************************************
// File: Loongarch64walker.cpp
//

//
// LOONGARCH64 instruction decoding/stepping logic
//
//*****************************************************************************

#include "stdafx.h"
#include "walker.h"
#include "frames.h"
#include "openum.h"

#ifdef TARGET_LOONGARCH64

void NativeWalker::Decode()
{
    _ASSERTE(!"TODO:=====Not implements for LOONGARCH64. -2");
    return;
}


//When control reaches here m_pSharedPatchBypassBuffer has the original instructions in m_pSharedPatchBypassBuffer->PatchBypass
BYTE*  NativeWalker::SetupOrSimulateInstructionForPatchSkip(T_CONTEXT * context, SharedPatchBypassBuffer* m_pSharedPatchBypassBuffer,  const BYTE *address, PRD_TYPE opcode)
{
    _ASSERTE(!"TODO:=====Not implements for LOONGARCH64. -3");
    return NULL;
}

//Decodes PC Relative Branch Instructions
//This code  is shared between the NativeWalker and DebuggerPatchSkip.
//So ENSURE THIS FUNCTION DOES NOT CHANGE ANY STATE OF THE DEBUGEE
//This Function Decodes :
// BL     offset
// B      offset
// B.Cond offset

//Output of the Function are:
//offset - Offset from current PC to which control will go next
//WALK_TYPE

BOOL  NativeWalker::DecodePCRelativeBranchInst(PT_CONTEXT context, const PRD_TYPE& opcode, PCODE& offset, WALK_TYPE& walk)
{
    _ASSERTE(!"TODO:=====Not implements for LOONGARCH64. -4");
    return FALSE;
}

BOOL  NativeWalker::DecodeCallInst(const PRD_TYPE& opcode, int& RegNum, WALK_TYPE& walk)
{
    _ASSERTE(!"TODO:=====Not implements for LOONGARCH64. -5");
    return FALSE;
}
#endif
