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

#define MAX_INSTRUCTION_LENGTH 4

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to land at the call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 8

#define PRD_TYPE                               LONG
#define CORDbg_BREAK_INSTRUCTION_SIZE 4
#define CORDbg_BREAK_INSTRUCTION (LONG)0x002A0005

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
    REGISTER_LOONGARCH64_RA,
    REGISTER_LOONGARCH64_TP,
    REGISTER_LOONGARCH64_SP,
    REGISTER_LOONGARCH64_A0,
    REGISTER_LOONGARCH64_A1,
    REGISTER_LOONGARCH64_A2,
    REGISTER_LOONGARCH64_A3,
    REGISTER_LOONGARCH64_A4,
    REGISTER_LOONGARCH64_A5,
    REGISTER_LOONGARCH64_A6,
    REGISTER_LOONGARCH64_A7,
    REGISTER_LOONGARCH64_T0,
    REGISTER_LOONGARCH64_T1,
    REGISTER_LOONGARCH64_T2,
    REGISTER_LOONGARCH64_T3,
    REGISTER_LOONGARCH64_T4,
    REGISTER_LOONGARCH64_T5,
    REGISTER_LOONGARCH64_T6,
    REGISTER_LOONGARCH64_T7,
    REGISTER_LOONGARCH64_T8,
    REGISTER_LOONGARCH64_X0,
    REGISTER_LOONGARCH64_FP,
    REGISTER_LOONGARCH64_S0,
    REGISTER_LOONGARCH64_S1,
    REGISTER_LOONGARCH64_S2,
    REGISTER_LOONGARCH64_S3,
    REGISTER_LOONGARCH64_S4,
    REGISTER_LOONGARCH64_S5,
    REGISTER_LOONGARCH64_S6,
    REGISTER_LOONGARCH64_S7,
    REGISTER_LOONGARCH64_S8,
    REGISTER_LOONGARCH64_PC
};

inline void CORDbgSetIP(DT_CONTEXT *context, LPVOID ip) {
    LIMITED_METHOD_CONTRACT;

    context->Pc = (DWORD64)ip;
}

inline LPVOID CORDbgGetSP(const DT_CONTEXT * context) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->Sp);
}

inline void CORDbgSetSP(DT_CONTEXT *context, LPVOID esp) {
    LIMITED_METHOD_CONTRACT;

    context->Sp = (DWORD64)esp;
}

inline LPVOID CORDbgGetFP(const DT_CONTEXT * context) {
    LIMITED_METHOD_CONTRACT;

    return (LPVOID)(size_t)(context->Fp);
}

inline void CORDbgSetFP(DT_CONTEXT *context, LPVOID fp) {
    LIMITED_METHOD_CONTRACT;

    context->Fp = (DWORD64)fp;
}


inline BOOL CompareControlRegisters(const DT_CONTEXT * pCtx1, const DT_CONTEXT * pCtx2)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // TODO-LoongArch64: Sort out frame registers

    if ((pCtx1->Pc == pCtx2->Pc) &&
        (pCtx1->Sp == pCtx2->Sp) &&
        (pCtx1->Fp == pCtx2->Fp))
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

    TADDR ptraddr = dac_cast<TADDR>(address);
    *(PRD_TYPE *)ptraddr = instruction;
    FlushInstructionCache(GetCurrentProcess(),
                          address,
                          sizeof(PRD_TYPE));
}

inline PRD_TYPE CORDbgGetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    TADDR ptraddr = dac_cast<TADDR>(address);
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

    return (LPVOID)(size_t)(context->Pc);
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

    // LoongArch64 appears to leave the PC at the start of the breakpoint.
    return;
}

inline bool AddressIsBreakpoint(CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return CORDbgGetInstruction(address) == CORDbg_BREAK_INSTRUCTION;
}

inline void SetSSFlag(DT_CONTEXT *pContext)
{
    // TODO-LoongArch64: LoongArch64 doesn't support cpsr.
    _ASSERTE(!"unimplemented on LOONGARCH64 yet");
}

inline void UnsetSSFlag(DT_CONTEXT *pContext)
{
    // TODO-LoongArch64: LoongArch64 doesn't support cpsr.
    _ASSERTE(!"unimplemented on LOONGARCH64 yet");
}

inline bool IsSSFlagEnabled(DT_CONTEXT * pContext)
{
    // TODO-LoongArch64: LoongArch64 doesn't support cpsr.
    _ASSERTE(!"unimplemented on LOONGARCH64 yet");
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
