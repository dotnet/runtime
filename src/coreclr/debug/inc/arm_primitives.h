// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: arm_primitives.h
//

//
// ARM/ARM64-specific debugger primitives
//
//*****************************************************************************

#ifndef ARM_PRIMITIVES_H_
#define ARM_PRIMITIVES_H_

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

#if defined(TARGET_ARM64)
    pContext->Pc -= CORDbg_BREAK_INSTRUCTION_SIZE;
#else
    // @ARMTODO: ARM appears to leave the PC at the start of the breakpoint (at least according to Windbg,
    // which may be adjusting the view).
#endif
    return;
}

inline bool AddressIsBreakpoint(CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return CORDbgGetInstruction(address) == CORDbg_BREAK_INSTRUCTION;
}

class Thread;
// Enable single stepping.
void SetSSFlag(DT_CONTEXT *pCtx, Thread *pThread);

// Disable single stepping
void UnsetSSFlag(DT_CONTEXT *pCtx, Thread *pThread);

// Check if single stepping is enabled.
bool IsSSFlagEnabled(DT_CONTEXT *pCtx, Thread *pThread);

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

#endif // ARM_PRIMITIVES_H_
