// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef STUBLINKERX86_H_
#define STUBLINKERX86_H_

#include "stublink.h"

class MetaSig;

extern PCODE GetPreStubEntryPoint();

//=======================================================================

#define X86_INSTR_CALL_REL32    0xE8        // call rel32
#define X86_INSTR_CALL_IND      0x15FF      // call dword ptr[addr32]
#define X86_INSTR_CALL_IND_EAX  0x10FF      // call dword ptr[eax]
#define X86_INSTR_JMP_REL32     0xE9        // jmp rel32
#define X86_INSTR_JMP_IND       0x25FF      // jmp dword ptr[addr32]
#define X86_INSTR_JMP_EAX       0xE0FF      // jmp eax
#define X86_INSTR_MOV_EAX_IMM32 0xB8        // mov eax, imm32
#define X86_INSTR_MOV_EAX_ECX_IND 0x018b    // mov eax, [ecx]
#define X86_INSTR_CMP_IND_ECX_IMM32 0x3981  // cmp [ecx], imm32

#define X86_INSTR_NOP3_1        0x9090      // 1st word of 3-byte nop
#define X86_INSTR_NOP3_3        0x90        // 3rd byte of 3-byte nop
#define X86_INSTR_INT3          0xCC        // int 3

//----------------------------------------------------------------------
// Encodes X86 registers. The numbers are chosen to match Intel's opcode
// encoding.
//----------------------------------------------------------------------
enum X86Reg : UCHAR
{
    kEAX = 0,
    kECX = 1,
    kEDX = 2,
    kEBX = 3,
    // kESP intentionally omitted because of its irregular treatment in MOD/RM
    kEBP = 5,
    kESI = 6,
    kEDI = 7,

#ifdef TARGET_X86
    NumX86Regs = 8,
#endif // TARGET_X86

    kXMM0 = 0,
    kXMM1 = 1,
    kXMM2 = 2,
    kXMM3 = 3,
    kXMM4 = 4,
    kXMM5 = 5,
#if defined(TARGET_AMD64)
    kXMM6 = 6,
    kXMM7 = 7,
    kXMM8 = 8,
    kXMM9 = 9,
    kXMM10 = 10,
    kXMM11 = 11,
    kXMM12 = 12,
    kXMM13 = 13,
    kXMM14 = 14,
    kXMM15 = 15,
    // Integer registers commence here
    kRAX = 0,
    kRCX = 1,
    kRDX = 2,
    kRBX = 3,
    // kRSP intentionally omitted because of its irregular treatment in MOD/RM
    kRBP = 5,
    kRSI = 6,
    kRDI = 7,
    kR8  = 8,
    kR9  = 9,
    kR10 = 10,
    kR11 = 11,
    kR12 = 12,
    kR13 = 13,
    kR14 = 14,
    kR15 = 15,
    NumX86Regs = 16,

#endif // TARGET_AMD64

    // We use "push ecx" instead of "sub esp, sizeof(LPVOID)"
    kDummyPushReg = kECX
};


//----------------------------------------------------------------------
// StubLinker with extensions for generating X86 code.
//----------------------------------------------------------------------
class StubLinkerCPU : public StubLinker
{
    public:

#ifdef TARGET_AMD64
        enum X86OperandSize
        {
            k32BitOp,
            k64BitOp,
        };
#endif

        VOID X86EmitAddReg(X86Reg reg, INT32 imm32);

        VOID X86EmitMovRegReg(X86Reg destReg, X86Reg srcReg);

#ifdef TARGET_X86
        VOID X86EmitPushReg(X86Reg reg);
        VOID X86EmitPopReg(X86Reg reg);
        VOID X86EmitPushImm32(UINT value);
        VOID X86EmitPushImmPtr(LPVOID value BIT64_ARG(X86Reg tmpReg = kR10));
#endif

#ifdef TARGET_AMD64
        VOID X64EmitMovXmmXmm(X86Reg destXmmreg, X86Reg srcXmmReg);
        VOID X64EmitMovSDFromMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs = 0);
        VOID X64EmitMovSDToMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs = 0);
        VOID X64EmitMovSSFromMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs = 0);
        VOID X64EmitMovSSToMem(X86Reg Xmmreg, X86Reg baseReg, int32_t ofs = 0);
        VOID X64EmitMovqRegXmm(X86Reg reg, X86Reg Xmmreg);
        VOID X64EmitMovqXmmReg(X86Reg Xmmreg, X86Reg reg);

        VOID X64EmitMovXmmWorker(BYTE prefix, BYTE opcode, X86Reg Xmmreg, X86Reg baseReg, int32_t ofs = 0);
        VOID X64EmitMovqWorker(BYTE opcode, X86Reg Xmmreg, X86Reg reg);
#endif

        VOID X86EmitOffsetModRM(BYTE opcode, X86Reg altreg, X86Reg indexreg, int32_t ofs);

#ifdef TARGET_X86
        VOID X86EmitNearJump(CodeLabel *pTarget);
#endif

        VOID X86EmitIndexRegLoad(X86Reg dstreg, X86Reg srcreg, int32_t ofs = 0);
        VOID X86EmitIndexRegStore(X86Reg dstreg, int32_t ofs, X86Reg srcreg);

#ifdef TARGET_X86
        VOID X86EmitIndexPush(X86Reg srcreg, int32_t ofs);

        VOID X86EmitAddEsp(INT32 imm32);
        VOID X86EmitEspOffset(BYTE opcode,
                              X86Reg altreg,
                              int32_t ofs);
#endif

        // Emits the most efficient form of the operation:
        //
        //    opcode   altreg, [basereg + scaledreg*scale + ofs]
        //
        // or
        //
        //    opcode   [basereg + scaledreg*scale + ofs], altreg
        //
        // (the opcode determines which comes first.)
        //
        //
        // Limitations:
        //
        //    scale must be 0,1,2,4 or 8.
        //    if scale == 0, scaledreg is ignored.
        //    basereg and altreg may be equal to 4 (ESP) but scaledreg cannot
        //    for some opcodes, "altreg" may actually select an operation
        //      rather than a second register argument.
        //

        VOID X86EmitOp(WORD    opcode,
                       X86Reg  altreg,
                       X86Reg  basereg,
                       int32_t ofs = 0,
                       X86Reg  scaledreg = (X86Reg)0,
                       BYTE    scale = 0
             AMD64_ARG(X86OperandSize OperandSize = k32BitOp)
                       );

#ifdef TARGET_AMD64
        FORCEINLINE
        VOID X86EmitOp(WORD    opcode,
                       X86Reg  altreg,
                       X86Reg  basereg,
                       int32_t ofs,
                       X86OperandSize OperandSize
                       )
        {
            X86EmitOp(opcode, altreg, basereg, ofs, (X86Reg)0, 0, OperandSize);
        }
#endif // TARGET_AMD64

        VOID X86EmitRegLoad(X86Reg reg, UINT_PTR imm);

        VOID X86_64BitOperands ()
        {
            WRAPPER_NO_CONTRACT;
#ifdef TARGET_AMD64
            Emit8(0x48);
#endif
        }

#ifdef TARGET_X86
        VOID EmitUnboxMethodStub(MethodDesc* pRealMD);
#endif // TARGET_X86
        VOID EmitTailJumpToMethod(MethodDesc *pMD);
#ifdef TARGET_AMD64
        VOID EmitLoadMethodAddressIntoAX(MethodDesc *pMD);
#endif

#if defined(FEATURE_SHARE_GENERIC_CODE)
        VOID EmitInstantiatingMethodStub(MethodDesc* pSharedMD, void* extra);
#endif // FEATURE_SHARE_GENERIC_CODE
        VOID EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg);

        //===========================================================================
        // Emits code to adjust for a static delegate target.
        VOID EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);

    public:
        static void Init();

};

inline TADDR rel32Decode(/*PTR_INT32*/ TADDR pRel32)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return pRel32 + 4 + *PTR_INT32(pRel32);
}

void rel32SetInterlocked(/*PINT32*/ PVOID pRel32, /*PINT32*/ PVOID pRel32RW, TADDR target, MethodDesc* pMD);
BOOL rel32SetInterlocked(/*PINT32*/ PVOID pRel32, /*PINT32*/ PVOID pRel32RW, TADDR target, TADDR expected, MethodDesc* pMD);

#endif  // STUBLINKERX86_H_
