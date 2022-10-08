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

#ifndef CORDB_ADDRESS_TYPE
typedef const BYTE                  CORDB_ADDRESS_TYPE;
typedef DPTR(CORDB_ADDRESS_TYPE)    PTR_CORDB_ADDRESS_TYPE;
#endif
//This is an abstraction to keep x86/ia64 patch data separate
#ifndef PRD_TYPE
#define PRD_TYPE                               DWORD_PTR
#endif

typedef M128A FPRegister64;

// From section 1.1 of AMD64 Programmers Manual Vol 3.
#define MAX_INSTRUCTION_LENGTH                 15

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to lend somewhere in a call instruction.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 1

#define CORDbg_BREAK_INSTRUCTION_SIZE          1
#define CORDbg_BREAK_INSTRUCTION         (BYTE)0xCC

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
    REGISTER_AMD64_RAX,
    REGISTER_AMD64_RCX,
    REGISTER_AMD64_RDX,
    REGISTER_AMD64_RBX,
    REGISTER_AMD64_RSP,
    REGISTER_AMD64_RBP,
    REGISTER_AMD64_RSI,
    REGISTER_AMD64_RDI,
    REGISTER_AMD64_R8,
    REGISTER_AMD64_R9,
    REGISTER_AMD64_R10,
    REGISTER_AMD64_R11,
    REGISTER_AMD64_R12,
    REGISTER_AMD64_R13,
    REGISTER_AMD64_R14,
    REGISTER_AMD64_R15
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

    return (LPVOID) context->Rip;
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

    context->Rip = (DWORD64) rip;
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

    return (LPVOID)context->Rsp;
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

    context->Rsp = (UINT_PTR)rsp;
}

// AMD64 has no frame pointer stored in RBP
#define CORDbgSetFP(context, rbp)
#define CORDbgGetFP(context) 0

// compare the RIP, RSP, and RBP
inline BOOL CompareControlRegisters(const DT_CONTEXT * pCtx1, const DT_CONTEXT * pCtx2)
{
    LIMITED_METHOD_DAC_CONTRACT;

    if ((pCtx1->Rip == pCtx2->Rip) &&
        (pCtx1->Rsp == pCtx2->Rsp) &&
        (pCtx1->Rbp == pCtx2->Rbp))
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
// a _single_ byte of memory. This is really important. If you only
// save one byte from the instruction stream before placing a breakpoint,
// you need to make sure to only replace one byte later on.
//


inline PRD_TYPE CORDbgGetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address)
{
    LIMITED_METHOD_CONTRACT;

    return *address;                    // retrieving only one byte is important
}

inline void CORDbgInsertBreakpoint(UNALIGNED CORDB_ADDRESS_TYPE *address)
{
    LIMITED_METHOD_CONTRACT;

    *((unsigned char*)address) = 0xCC; // int 3 (single byte patch)
    FlushInstructionCache(GetCurrentProcess(), address, 1);

}

inline void CORDbgSetInstruction(UNALIGNED CORDB_ADDRESS_TYPE* address,
                                 PRD_TYPE instruction)
{
    // In a DAC build, this function assumes the input is an host address.
    LIMITED_METHOD_DAC_CONTRACT;

    *((unsigned char*)address) =
        (unsigned char) instruction;    // setting one byte is important
    FlushInstructionCache(GetCurrentProcess(), address, 1);

}


inline void CORDbgAdjustPCForBreakInstruction(DT_CONTEXT* pContext)
{
    LIMITED_METHOD_CONTRACT;

    pContext->Rip -= 1;
}

inline bool AddressIsBreakpoint(CORDB_ADDRESS_TYPE *address)
{
    LIMITED_METHOD_CONTRACT;

    return *address == CORDbg_BREAK_INSTRUCTION;
}

inline void SetSSFlag(DT_CONTEXT *pContext)
{
    _ASSERTE(pContext != NULL);
    pContext->EFlags |= 0x100;
}

inline void UnsetSSFlag(DT_CONTEXT *pContext)
{
    _ASSERTE(pContext != NULL);
    pContext->EFlags &= ~0x100;
}

inline bool IsSSFlagEnabled(DT_CONTEXT * context)
{
    _ASSERTE(context != NULL);
    return (context->EFlags & 0x100) != 0;
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
