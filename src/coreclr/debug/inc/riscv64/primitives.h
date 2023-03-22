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

typedef const BYTE                  CORDB_ADDRESS_TYPE;
typedef DPTR(CORDB_ADDRESS_TYPE)    PTR_CORDB_ADDRESS_TYPE;

// TODO-RISCV64-CQ: Update when it supports c and other extensions
#define MAX_INSTRUCTION_LENGTH 4

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to land at the call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 4

#define PRD_TYPE                               LONG
#define CORDbg_BREAK_INSTRUCTION_SIZE 4
#define CORDbg_BREAK_INSTRUCTION (LONG)0x00100073

inline CORDB_ADDRESS GetPatchEndAddr(CORDB_ADDRESS patchAddr)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return patchAddr + CORDbg_BREAK_INSTRUCTION_SIZE;
}

#define InitializePRDToBreakInst(_pPRD)       *(_pPRD) = CORDbg_BREAK_INSTRUCTION
#define PRDIsBreakInst(_pPRD)                 (*(_pPRD) == CORDbg_BREAK_INSTRUCTION)


#define CORDbgGetInstructionEx(_buffer, _requestedAddr, _patchAddr, _dummy1, _dummy2)                          \
    CORDbgGetInstructionExImpl((CORDB_ADDRESS_TYPE *)((_buffer) + (_patchAddr) - (_requestedAddr)));

#define CORDbgSetInstructionEx(_buffer, _requestedAddr, _patchAddr, _opcode, _dummy2)                          \
    CORDbgSetInstructionExImpl((CORDB_ADDRESS_TYPE *)((_buffer) + (_patchAddr) - (_requestedAddr)), (_opcode));

#define CORDbgInsertBreakpointEx(_buffer, _requestedAddr, _patchAddr, _dummy1, _dummy2)                        \
    CORDbgInsertBreakpointExImpl((CORDB_ADDRESS_TYPE *)((_buffer) + (_patchAddr) - (_requestedAddr)));


constexpr CorDebugRegister g_JITToCorDbgReg[] =
{
    REGISTER_RISCV64_X0, // TODO-RISCV64-CQ: Add X0 to access DbgReg in correct order
    REGISTER_RISCV64_RA,
    REGISTER_RISCV64_SP,
    REGISTER_RISCV64_GP,
    REGISTER_RISCV64_TP,
    REGISTER_RISCV64_T0,
    REGISTER_RISCV64_T1,
    REGISTER_RISCV64_T2,
    REGISTER_RISCV64_FP,
    REGISTER_RISCV64_S1,
    REGISTER_RISCV64_A0,
    REGISTER_RISCV64_A1,
    REGISTER_RISCV64_A2,
    REGISTER_RISCV64_A3,
    REGISTER_RISCV64_A4,
    REGISTER_RISCV64_A5,
    REGISTER_RISCV64_A6,
    REGISTER_RISCV64_A7,
    REGISTER_RISCV64_S2,
    REGISTER_RISCV64_S3,
    REGISTER_RISCV64_S4,
    REGISTER_RISCV64_S5,
    REGISTER_RISCV64_S6,
    REGISTER_RISCV64_S7,
    REGISTER_RISCV64_S8,
    REGISTER_RISCV64_S9,
    REGISTER_RISCV64_S10,
    REGISTER_RISCV64_S11,
    REGISTER_RISCV64_T3,
    REGISTER_RISCV64_T4,
    REGISTER_RISCV64_T5,
    REGISTER_RISCV64_T6,
    REGISTER_RISCV64_PC
};

inline void CORDbgSetIP(DT_CONTEXT *context, LPVOID ip) {
    LIMITED_METHOD_CONTRACT;

    context->PC = (DWORD64)ip;
}

inline LPVOID CORDbgGetSP(const DT_CONTEXT * context) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->SP);
}

inline void CORDbgSetSP(DT_CONTEXT *context, LPVOID esp) {
    LIMITED_METHOD_CONTRACT;

    context->SP = (DWORD64)esp;
}

inline LPVOID CORDbgGetFP(const DT_CONTEXT * context) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->FP);
}

inline void CORDbgSetFP(DT_CONTEXT *context, LPVOID fp) {
    LIMITED_METHOD_CONTRACT;

    context->FP = (DWORD64)fp;
}


inline BOOL CompareControlRegisters(const DT_CONTEXT * pCtx1, const DT_CONTEXT * pCtx2)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // TODO-RISCV64: Sort out frame registers

    if ((pCtx1->PC == pCtx2->PC) &&
        (pCtx1->SP == pCtx2->SP) &&
        (pCtx1->FP == pCtx2->FP))
    {
        return TRUE;
    }

    return FALSE;
}

inline void CORDbgSetInstruction(CORDB_ADDRESS_TYPE* address,
                                 PRD_TYPE instruction)
{
    // In a DAC build, this function assumes the input is an host address.
    LIMITED_METHOD_DAC_CONTRACT;

    ULONGLONG ptraddr = dac_cast<ULONGLONG>(address);
    *(PRD_TYPE *)ptraddr = instruction;
    FlushInstructionCache(GetCurrentProcess(),
                          address,
                          sizeof(PRD_TYPE));
}

inline PRD_TYPE CORDbgGetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    ULONGLONG ptraddr = dac_cast<ULONGLONG>(address);
    return *(PRD_TYPE *)ptraddr;
}

//
// Mapping from ICorDebugInfo register numbers to CorDebugRegister
// numbers. Note: this must match the order in corinfo.h.
//
inline CorDebugRegister ConvertRegNumToCorDebugRegister(ICorDebugInfo::RegNum reg)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(reg >= 0);
    _ASSERTE(static_cast<size_t>(reg) < ARRAY_SIZE(g_JITToCorDbgReg));
    return g_JITToCorDbgReg[reg];
}

inline LPVOID CORDbgGetIP(DT_CONTEXT *context)
{
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->PC);
}

inline void CORDbgSetInstructionExImpl(CORDB_ADDRESS_TYPE* address,
                                 PRD_TYPE instruction)
{
    LIMITED_METHOD_DAC_CONTRACT;

    *(PRD_TYPE *)address = instruction;
    FlushInstructionCache(GetCurrentProcess(),
                          address,
                          sizeof(PRD_TYPE));
}

inline PRD_TYPE CORDbgGetInstructionExImpl(UNALIGNED CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return *(PRD_TYPE *)address;
}

inline void CORDbgInsertBreakpoint(UNALIGNED CORDB_ADDRESS_TYPE *address)
{
    LIMITED_METHOD_CONTRACT;

    CORDbgSetInstruction(address, CORDbg_BREAK_INSTRUCTION);
}

inline void CORDbgInsertBreakpointExImpl(UNALIGNED CORDB_ADDRESS_TYPE *address)
{
    LIMITED_METHOD_CONTRACT;

    CORDbgSetInstruction(address, CORDbg_BREAK_INSTRUCTION);
}

// After a breakpoint exception, the CPU points to _after_ the break instruction.
// Adjust the IP so that it points at the break instruction. This lets us patch that
// opcode and re-execute what was underneath the bp.
inline void CORDbgAdjustPCForBreakInstruction(DT_CONTEXT* pContext)
{
    LIMITED_METHOD_CONTRACT;

    // RISCV64 appears to leave the PC at the start of the breakpoint.
    return;
}

inline bool AddressIsBreakpoint(CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return CORDbgGetInstruction(address) == CORDbg_BREAK_INSTRUCTION;
}

inline void SetSSFlag(DT_CONTEXT *pContext)
{
    // TODO-RISCV64: RISCV64 doesn't support cpsr.
    _ASSERTE(!"unimplemented on RISCV64 yet");
}

inline void UnsetSSFlag(DT_CONTEXT *pContext)
{
    // TODO-RISCV64: RISCV64 doesn't support cpsr.
    _ASSERTE(!"unimplemented on RISCV64 yet");
}

inline bool IsSSFlagEnabled(DT_CONTEXT * pContext)
{
    // TODO-RISCV64: RISCV64 doesn't support cpsr.
    _ASSERTE(!"unimplemented on RISCV64 yet");
    return false;
}


inline bool PRDIsEqual(PRD_TYPE p1, PRD_TYPE p2)
{
    return p1 == p2;
}

inline void InitializePRD(PRD_TYPE *p1)
{
    *p1 = 0;
}

inline bool PRDIsEmpty(PRD_TYPE p1)
{
    LIMITED_METHOD_CONTRACT;

    return p1 == 0;
}

#endif // PRIMITIVES_H_
