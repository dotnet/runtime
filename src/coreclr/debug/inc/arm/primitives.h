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

#include "executableallocator.h"

#ifndef THUMB_CODE
#define THUMB_CODE 1
#endif

typedef ULONGLONG                   FPRegister64;
typedef const BYTE                  CORDB_ADDRESS_TYPE;
typedef DPTR(CORDB_ADDRESS_TYPE)    PTR_CORDB_ADDRESS_TYPE;

//This is an abstraction to keep x86/ia64 patch data separate
#define PRD_TYPE                               USHORT

#define MAX_INSTRUCTION_LENGTH 4

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to lend somewhere in a call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 2

#define CORDbg_BREAK_INSTRUCTION_SIZE 2
#ifdef __linux__
#define CORDbg_BREAK_INSTRUCTION (USHORT)0xde01
#else
#define CORDbg_BREAK_INSTRUCTION (USHORT)0xdefe
#endif

inline CORDB_ADDRESS GetPatchEndAddr(CORDB_ADDRESS patchAddr)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return patchAddr + CORDbg_BREAK_INSTRUCTION_SIZE;
}


#define InitializePRDToBreakInst(_pPRD)       *(_pPRD) = CORDbg_BREAK_INSTRUCTION
#define PRDIsBreakInst(_pPRD)                 (*(_pPRD) == CORDbg_BREAK_INSTRUCTION)

template <class T>
inline T _ClearThumbBit(T addr)
{
    return (T)(((CORDB_ADDRESS)addr) & ~THUMB_CODE);
}


#define CORDbgGetInstructionEx(_buffer, _requestedAddr, _patchAddr, _dummy1, _dummy2)                          \
    CORDbgGetInstructionExImpl((CORDB_ADDRESS_TYPE *)((_buffer) + (_ClearThumbBit(_patchAddr) - (_requestedAddr))));

#define CORDbgSetInstructionEx(_buffer, _requestedAddr, _patchAddr, _opcode, _dummy2)                          \
    CORDbgSetInstructionExImpl((CORDB_ADDRESS_TYPE *)((_buffer) + (_ClearThumbBit(_patchAddr) - (_requestedAddr))), (_opcode));

#define CORDbgInsertBreakpointEx(_buffer, _requestedAddr, _patchAddr, _dummy1, _dummy2)                        \
    CORDbgInsertBreakpointExImpl((CORDB_ADDRESS_TYPE *)((_buffer) + (_ClearThumbBit(_patchAddr) - (_requestedAddr))));


constexpr CorDebugRegister g_JITToCorDbgReg[] =
{
    REGISTER_ARM_R0,
    REGISTER_ARM_R1,
    REGISTER_ARM_R2,
    REGISTER_ARM_R3,
    REGISTER_ARM_R4,
    REGISTER_ARM_R5,
    REGISTER_ARM_R6,
    REGISTER_ARM_R7,
    REGISTER_ARM_R8,
    REGISTER_ARM_R9,
    REGISTER_ARM_R10,
    REGISTER_ARM_R11,
    REGISTER_ARM_R12,
    REGISTER_ARM_SP,
    REGISTER_ARM_LR,
    REGISTER_ARM_PC
};

//
// inline function to access/modify the CONTEXT
//
inline void CORDbgSetIP(DT_CONTEXT *context, LPVOID eip) {
    LIMITED_METHOD_CONTRACT;

    context->Pc = (UINT32)(size_t)eip;
}

inline LPVOID CORDbgGetSP(const DT_CONTEXT * context) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->Sp);
}

inline void CORDbgSetSP(DT_CONTEXT *context, LPVOID esp) {
    LIMITED_METHOD_CONTRACT;

    context->Sp = (UINT32)(size_t)esp;
}

inline void CORDbgSetFP(DT_CONTEXT *context, LPVOID ebp) {
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(FALSE); // @ARMTODO
}
inline LPVOID CORDbgGetFP(DT_CONTEXT* context)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(FALSE); // @ARMTODO
    return (LPVOID)(UINT_PTR)0;
}

// compare the EIP, ESP, and EBP
inline BOOL CompareControlRegisters(const DT_CONTEXT * pCtx1, const DT_CONTEXT * pCtx2)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // @ARMTODO: Sort out frame registers

    if ((pCtx1->Pc == pCtx2->Pc) &&
        (pCtx1->Sp == pCtx2->Sp))
    {
        return TRUE;
    }

    return FALSE;
}

/* ========================================================================= */
//
// Routines used by debugger support functions such as codepatch.cpp or
// exception handling code.
//
// GetInstruction, InsertBreakpoint, and SetInstruction all operate on
// a _single_ PRD_TYPE unit of memory. This is really important. If you only
// save one PRD_TYPE from the instruction stream before placing a breakpoint,
// you need to make sure to only replace one PRD_TYPE later on.
//
inline PRD_TYPE CORDbgGetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    CORDB_ADDRESS ptraddr = (CORDB_ADDRESS)address;
    _ASSERTE(ptraddr & THUMB_CODE);
    ptraddr &= ~THUMB_CODE;
    return *(PRD_TYPE *)ptraddr;
}

inline void CORDbgSetInstruction(CORDB_ADDRESS_TYPE* address,
                                 PRD_TYPE instruction)
{
    // In a DAC build, this function assumes the input is an host address.
    LIMITED_METHOD_DAC_CONTRACT;

    ExecutableWriterHolder<void> instructionWriterHolder((LPVOID)address, sizeof(PRD_TYPE));

    CORDB_ADDRESS ptraddr = (CORDB_ADDRESS)instructionWriterHolder.GetRW();
    _ASSERTE(ptraddr & THUMB_CODE);
    ptraddr &= ~THUMB_CODE;

    *(PRD_TYPE *)ptraddr = instruction;
    FlushInstructionCache(GetCurrentProcess(),
                          _ClearThumbBit(address),
                          sizeof(PRD_TYPE));
}

class Thread;
// Enable single stepping.
void SetSSFlag(DT_CONTEXT *pCtx, Thread *pThread);

// Disable single stepping
void UnsetSSFlag(DT_CONTEXT *pCtx, Thread *pThread);

// Check if single stepping is enabled.
bool IsSSFlagEnabled(DT_CONTEXT *pCtx, Thread *pThread);

#include "arm_primitives.h"
#endif // PRIMITIVES_H_
