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

//This is an abstraction to keep x86/ia64 patch data separate
#define PRD_TYPE                    USHORT

#define MAX_INSTRUCTION_LENGTH 6

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to lend somewhere in a call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 1

#define CORDbg_BREAK_INSTRUCTION_SIZE    2
#define CORDbg_BREAK_INSTRUCTION         (USHORT)0x0001

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
    REGISTER_S390X_R0,
    REGISTER_S390X_R1,
    REGISTER_S390X_R2,
    REGISTER_S390X_R3,
    REGISTER_S390X_R4,
    REGISTER_S390X_R5,
    REGISTER_S390X_R6,
    REGISTER_S390X_R7,
    REGISTER_S390X_R8,
    REGISTER_S390X_R9,
    REGISTER_S390X_R10,
    REGISTER_S390X_R11,
    REGISTER_S390X_R12,
    REGISTER_S390X_R13,
    REGISTER_S390X_R14,
    REGISTER_S390X_R15
};

//
// Mapping from ICorDebugInfo register numbers to CorDebugRegister
// numbers. Note: this must match the order in corinfo.h.
//
inline CorDebugRegister ConvertRegNumToCorDebugRegister(ICorDebugInfo::RegNum reg)
{
    _ASSERTE(reg >= 0);
    _ASSERTE(static_cast<size_t>(reg) < ARRAY_SIZE(g_JITToCorDbgReg));
    return g_JITToCorDbgReg[reg];
}



//
// inline function to access/modify the CONTEXT
//
inline LPVOID CORDbgGetIP(DT_CONTEXT* context)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    return (LPVOID) context->PSWAddr;
}

inline void CORDbgSetIP(DT_CONTEXT* context, LPVOID rip)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    context->PSWAddr = (DWORD64) rip;
}

inline LPVOID CORDbgGetSP(const DT_CONTEXT * context)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    return (LPVOID)context->R15;
}
inline void CORDbgSetSP(DT_CONTEXT *context, LPVOID rsp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(context));
    }
    CONTRACTL_END;

    context->R15 = (UINT_PTR)rsp;
}

// S390X has no frame pointer
#define CORDbgSetFP(context, rbp)
#define CORDbgGetFP(context) 0

// compare the PC and SP
inline BOOL CompareControlRegisters(const DT_CONTEXT * pCtx1, const DT_CONTEXT * pCtx2)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if ((pCtx1->PSWAddr == pCtx2->PSWAddr) &&
        (pCtx1->R15 == pCtx2->R15))
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

/* ========================================================================= */
//
// Routines used by debugger support functions such as codepatch.cpp or
// exception handling code.
//
// GetInstruction, InsertBreakpoint, and SetInstruction all operate on
// a _single_ PRD_TYPE of memory. This is really important. If you only
// save one PRD_TYPE from the instruction stream before placing a breakpoint,
// you need to make sure to only replace one PRD_TYPE later on.
//


inline PRD_TYPE CORDbgGetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return *(PRD_TYPE *)address;
}

inline void CORDbgInsertBreakpoint(UNALIGNED CORDB_ADDRESS_TYPE *address)
{
    LIMITED_METHOD_CONTRACT;

    *((PRD_TYPE *)address) = CORDbg_BREAK_INSTRUCTION;
    FlushInstructionCache(GetCurrentProcess(), address, 2);
}

inline void CORDbgSetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address,
                                 PRD_TYPE instruction)
{
    // In a DAC build, this function assumes the input is an host address.
    LIMITED_METHOD_DAC_CONTRACT;

    *((PRD_TYPE *)address) = instruction;
    FlushInstructionCache(GetCurrentProcess(), address, 2);
}


inline void CORDbgAdjustPCForBreakInstruction(DT_CONTEXT* pContext)
{
    LIMITED_METHOD_CONTRACT;

    pContext->PSWAddr -= 2;
}

inline bool AddressIsBreakpoint(CORDB_ADDRESS_TYPE *address)
{
    LIMITED_METHOD_CONTRACT;

    return *(PRD_TYPE *)address == CORDbg_BREAK_INSTRUCTION;
}

inline void SetSSFlag(DT_CONTEXT *pContext)
{
    _ASSERTE(pContext != NULL);
    _ASSERTE(!"NYI");
}

inline void UnsetSSFlag(DT_CONTEXT *pContext)
{
    _ASSERTE(pContext != NULL);
    _ASSERTE(!"NYI");
}

inline bool IsSSFlagEnabled(DT_CONTEXT * context)
{
    _ASSERTE(context != NULL);
    _ASSERTE(!"NYI");
    return 0;
}


inline bool PRDIsEqual(PRD_TYPE p1, PRD_TYPE p2){
    return p1 == p2;
}
inline void InitializePRD(PRD_TYPE *p1) {
    *p1 = 0;
}

inline bool PRDIsEmpty(PRD_TYPE p1) {
    return p1 == 0;
}

#endif // PRIMITIVES_H_
