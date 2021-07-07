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

#if !defined(DBI_COMPILE) && !defined(DACCESS_COMPILE)
#include "executableallocator.h"
#endif

typedef const BYTE                  CORDB_ADDRESS_TYPE;
typedef DPTR(CORDB_ADDRESS_TYPE)    PTR_CORDB_ADDRESS_TYPE;

//This is an abstraction to keep x86/ia64 patch data separate
#define PRD_TYPE                               DWORD_PTR

#define MAX_INSTRUCTION_LENGTH 16

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to lend somewhere in a call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 1

#define CORDbg_BREAK_INSTRUCTION_SIZE 1
#define CORDbg_BREAK_INSTRUCTION (BYTE)0xCC

inline CORDB_ADDRESS GetPatchEndAddr(CORDB_ADDRESS patchAddr)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return patchAddr + CORDbg_BREAK_INSTRUCTION_SIZE;
}


#define InitializePRDToBreakInst(_pPRD)       *(_pPRD) = CORDbg_BREAK_INSTRUCTION
#define PRDIsBreakInst(_pPRD)                 (*(_pPRD) == CORDbg_BREAK_INSTRUCTION)

#define CORDbgGetInstructionEx(_buffer, _requestedAddr, _patchAddr, _dummy1, _dummy2)                          \
    CORDbgGetInstruction((CORDB_ADDRESS_TYPE *)((_buffer) + ((_patchAddr) - (_requestedAddr))));

#define CORDbgSetInstructionEx(_buffer, _requestedAddr, _patchAddr, _opcode, _dummy2)                          \
    CORDbgSetInstruction((CORDB_ADDRESS_TYPE *)((_buffer) + ((_patchAddr) - (_requestedAddr))), (_opcode));

#define CORDbgInsertBreakpointEx(_buffer, _requestedAddr, _patchAddr, _dummy1, _dummy2)                        \
    CORDbgInsertBreakpoint((CORDB_ADDRESS_TYPE *)((_buffer) + ((_patchAddr) - (_requestedAddr))));


constexpr CorDebugRegister g_JITToCorDbgReg[] =
{
    REGISTER_X86_EAX,
    REGISTER_X86_ECX,
    REGISTER_X86_EDX,
    REGISTER_X86_EBX,
    REGISTER_X86_ESP,
    REGISTER_X86_EBP,
    REGISTER_X86_ESI,
    REGISTER_X86_EDI
};

//
// Mapping from ICorDebugInfo register numbers to CorDebugRegister
// numbers. Note: this must match the order in corinfo.h.
//
inline CorDebugRegister ConvertRegNumToCorDebugRegister(ICorDebugInfo::RegNum reg)
{
    return g_JITToCorDbgReg[reg];
}


//
// inline function to access/modify the CONTEXT
//
inline LPVOID CORDbgGetIP(DT_CONTEXT *context) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->Eip);
}

inline void CORDbgSetIP(DT_CONTEXT *context, LPVOID eip) {
    LIMITED_METHOD_CONTRACT;

    context->Eip = (UINT32)(size_t)eip;
}

inline LPVOID CORDbgGetSP(const DT_CONTEXT * context) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->Esp);
}

inline void CORDbgSetSP(DT_CONTEXT *context, LPVOID esp) {
    LIMITED_METHOD_CONTRACT;

    context->Esp = (UINT32)(size_t)esp;
}

inline void CORDbgSetFP(DT_CONTEXT *context, LPVOID ebp) {
    LIMITED_METHOD_CONTRACT;

    context->Ebp = (UINT32)(size_t)ebp;
}
inline LPVOID CORDbgGetFP(DT_CONTEXT* context)
{
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(UINT_PTR)context->Ebp;
}

// compare the EIP, ESP, and EBP
inline BOOL CompareControlRegisters(const DT_CONTEXT * pCtx1, const DT_CONTEXT * pCtx2)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if ((pCtx1->Eip == pCtx2->Eip) &&
        (pCtx1->Esp == pCtx2->Esp) &&
        (pCtx1->Ebp == pCtx2->Ebp))
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
// a _single_ byte of memory. This is really important. If you only
// save one byte from the instruction stream before placing a breakpoint,
// you need to make sure to only replace one byte later on.
//


inline PRD_TYPE CORDbgGetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return *address; // retrieving only one byte is important

}

inline void CORDbgInsertBreakpoint(UNALIGNED CORDB_ADDRESS_TYPE *address)
{
    LIMITED_METHOD_CONTRACT;

#if !defined(DBI_COMPILE) && !defined(DACCESS_COMPILE)
    ExecutableWriterHolder<CORDB_ADDRESS_TYPE> breakpointWriterHolder(address, CORDbg_BREAK_INSTRUCTION_SIZE);
    UNALIGNED CORDB_ADDRESS_TYPE* addressRW = breakpointWriterHolder.GetRW();
#else // !DBI_COMPILE && !DACCESS_COMPILE
    UNALIGNED CORDB_ADDRESS_TYPE* addressRW = address;
#endif // !DBI_COMPILE && !DACCESS_COMPILE

    *((unsigned char*)addressRW) = 0xCC; // int 3 (single byte patch)
    FlushInstructionCache(GetCurrentProcess(), address, 1);
}

inline void CORDbgSetInstruction(CORDB_ADDRESS_TYPE* address,
                                 DWORD instruction)
{
    // In a DAC build, this function assumes the input is an host address.
    LIMITED_METHOD_DAC_CONTRACT;

    *((unsigned char*)address)
          = (unsigned char) instruction; // setting one byte is important
    FlushInstructionCache(GetCurrentProcess(), address, 1);
}

// After a breakpoint exception, the CPU points to _after_ the break instruction.
// Adjust the IP so that it points at the break instruction. This lets us patch that
// opcode and re-excute what was underneath the bp.
inline void CORDbgAdjustPCForBreakInstruction(DT_CONTEXT* pContext)
{
    LIMITED_METHOD_CONTRACT;

    pContext->Eip -= 1;
}

inline bool AddressIsBreakpoint(CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return *address == CORDbg_BREAK_INSTRUCTION;
}

// Set the hardware trace flag.
inline void SetSSFlag(DT_CONTEXT *context)
{
    _ASSERTE(context != NULL);
    context->EFlags |= 0x100;
}

// Unset the hardware trace flag.
inline void UnsetSSFlag(DT_CONTEXT *context)
{
    SUPPORTS_DAC;
    _ASSERTE(context != NULL);
    context->EFlags &= ~0x100;
}

// return true if the hardware trace flag applied.
inline bool IsSSFlagEnabled(DT_CONTEXT * context)
{
    _ASSERTE(context != NULL);
    return (context->EFlags & 0x100) != 0;
}



inline bool PRDIsEqual(PRD_TYPE p1, PRD_TYPE p2){
    return p1 == p2;
}

// On x86 opcode 0 is an 8-bit version of ADD.  Do we really want to use 0 to mean empty? (see issue 366221).
inline void InitializePRD(PRD_TYPE *p1) {
    *p1 = 0;
}
inline bool PRDIsEmpty(PRD_TYPE p1) {
    LIMITED_METHOD_CONTRACT;

    return p1 == 0;
}


#endif // PRIMITIVES_H_
