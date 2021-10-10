// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef STUBLINKERX86_H_
#define STUBLINKERX86_H_

#include "stublink.h"

struct ArrayOpScript;
class MetaSig;

extern PCODE GetPreStubEntryPoint();

//=======================================================================

#define X86_INSTR_CALL_REL32    0xE8        // call rel32
#define X86_INSTR_CALL_IND      0x15FF      // call dword ptr[addr32]
#define X86_INSTR_CALL_IND_EAX  0x10FF      // call dword ptr[eax]
#define X86_INSTR_CALL_IND_EAX_OFFSET  0x50FF  // call dword ptr[eax + offset] ; where offset follows these 2 bytes
#define X86_INSTR_CALL_EAX      0xD0FF      // call eax
#define X86_INSTR_JMP_REL32     0xE9        // jmp rel32
#define X86_INSTR_JMP_IND       0x25FF      // jmp dword ptr[addr32]
#define X86_INSTR_JMP_EAX       0xE0FF      // jmp eax
#define X86_INSTR_MOV_EAX_IMM32 0xB8        // mov eax, imm32
#define X86_INSTR_MOV_EAX_ECX_IND 0x018b    // mov eax, [ecx]
#define X86_INSTR_CMP_IND_ECX_IMM32 0x3981  // cmp [ecx], imm32
#define X86_INSTR_MOV_RM_R      0x89        // mov r/m,reg

#define X86_INSTR_MOV_AL        0xB0        // mov al, imm8
#define X86_INSTR_JMP_REL8      0xEB        // jmp short rel8

#define X86_INSTR_NOP           0x90        // nop
#define X86_INSTR_NOP3_1        0x9090      // 1st word of 3-byte nop
#define X86_INSTR_NOP3_3        0x90        // 3rd byte of 3-byte nop
#define X86_INSTR_INT3          0xCC        // int 3
#define X86_INSTR_HLT           0xF4        // hlt

#define X86_INSTR_MOVAPS_R_RM   0x280F      // movaps xmm1, xmm2/mem128
#define X86_INSTR_MOVAPS_RM_R   0x290F      // movaps xmm1/mem128, xmm2
#define X86_INSTR_MOVLPS_R_RM   0x120F      // movlps xmm1, xmm2/mem128
#define X86_INSTR_MOVLPS_RM_R   0x130F      // movlps xmm1/mem128, xmm2
#define X86_INSTR_MOVUPS_R_RM   0x100F      // movups xmm1, xmm2/mem128
#define X86_INSTR_MOVUPS_RM_R   0x110F      // movups xmm1/mem128, xmm2
#define X86_INSTR_XORPS         0x570F      // xorps xmm1, xmm2/mem128

#ifdef TARGET_AMD64
#define X86_INSTR_MOV_R10_IMM64 0xBA49      // mov r10, imm64
#endif

//----------------------------------------------------------------------
// Encodes X86 registers. The numbers are chosen to match Intel's opcode
// encoding.
//----------------------------------------------------------------------
enum X86Reg
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


// Use this only if you are absolutely sure that the instruction format
// handles it. This is not declared as X86Reg so that users are forced
// to add a cast and think about what exactly they are doing.
const int kESP_Unsafe = 4;

//----------------------------------------------------------------------
// Encodes X86 conditional jumps. The numbers are chosen to match
// Intel's opcode encoding.
//----------------------------------------------------------------------
class X86CondCode {
    public:
        enum cc {
            kJA   = 0x7,
            kJAE  = 0x3,
            kJB   = 0x2,
            kJBE  = 0x6,
            kJC   = 0x2,
            kJE   = 0x4,
            kJZ   = 0x4,
            kJG   = 0xf,
            kJGE  = 0xd,
            kJL   = 0xc,
            kJLE  = 0xe,
            kJNA  = 0x6,
            kJNAE = 0x2,
            kJNB  = 0x3,
            kJNBE = 0x7,
            kJNC  = 0x3,
            kJNE  = 0x5,
            kJNG  = 0xe,
            kJNGE = 0xc,
            kJNL  = 0xd,
            kJNLE = 0xf,
            kJNO  = 0x1,
            kJNP  = 0xb,
            kJNS  = 0x9,
            kJNZ  = 0x5,
            kJO   = 0x0,
            kJP   = 0xa,
            kJPE  = 0xa,
            kJPO  = 0xb,
            kJS   = 0x8,
        };
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
        VOID X86EmitAddRegReg(X86Reg destreg, X86Reg srcReg);
        VOID X86EmitSubReg(X86Reg reg, INT32 imm32);
        VOID X86EmitSubRegReg(X86Reg destreg, X86Reg srcReg);

        VOID X86EmitMovRegReg(X86Reg destReg, X86Reg srcReg);
        VOID X86EmitMovSPReg(X86Reg srcReg);
        VOID X86EmitMovRegSP(X86Reg destReg);

        VOID X86EmitPushReg(X86Reg reg);
        VOID X86EmitPopReg(X86Reg reg);
        VOID X86EmitPushRegs(unsigned regSet);
        VOID X86EmitPopRegs(unsigned regSet);
        VOID X86EmitPushImm32(UINT value);
        VOID X86EmitPushImm32(CodeLabel &pTarget);
        VOID X86EmitPushImm8(BYTE value);
        VOID X86EmitPushImmPtr(LPVOID value BIT64_ARG(X86Reg tmpReg = kR10));

        VOID X86EmitCmpRegImm32(X86Reg reg, INT32 imm32); // cmp reg, imm32
        VOID X86EmitCmpRegIndexImm32(X86Reg reg, INT32 offs, INT32 imm32); // cmp [reg+offs], imm32
#ifdef TARGET_AMD64
        VOID X64EmitCmp32RegIndexImm32(X86Reg reg, INT32 offs, INT32 imm32); // cmp dword ptr [reg+offs], imm32

        VOID X64EmitMovXmmXmm(X86Reg destXmmreg, X86Reg srcXmmReg);
        VOID X64EmitMovdqaFromMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs = 0);
        VOID X64EmitMovdqaToMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs = 0);
        VOID X64EmitMovSDFromMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs = 0);
        VOID X64EmitMovSDToMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs = 0);
        VOID X64EmitMovSSFromMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs = 0);
        VOID X64EmitMovSSToMem(X86Reg Xmmreg, X86Reg baseReg, __int32 ofs = 0);
        VOID X64EmitMovqRegXmm(X86Reg reg, X86Reg Xmmreg);
        VOID X64EmitMovqXmmReg(X86Reg Xmmreg, X86Reg reg);

        VOID X64EmitMovXmmWorker(BYTE prefix, BYTE opcode, X86Reg Xmmreg, X86Reg baseReg, __int32 ofs = 0);
        VOID X64EmitMovqWorker(BYTE opcode, X86Reg Xmmreg, X86Reg reg);
#endif

        VOID X86EmitZeroOutReg(X86Reg reg);
        VOID X86EmitJumpReg(X86Reg reg);

        VOID X86EmitOffsetModRM(BYTE opcode, X86Reg altreg, X86Reg indexreg, __int32 ofs);
        VOID X86EmitOffsetModRmSIB(BYTE opcode, X86Reg opcodeOrReg, X86Reg baseReg, X86Reg indexReg, __int32 scale, __int32 ofs);

        VOID X86EmitTailcallWithESPAdjust(CodeLabel *pTarget, INT32 imm32);
        VOID X86EmitTailcallWithSinglePop(CodeLabel *pTarget, X86Reg reg);

        VOID X86EmitNearJump(CodeLabel *pTarget);
        VOID X86EmitCondJump(CodeLabel *pTarget, X86CondCode::cc condcode);
        VOID X86EmitCall(CodeLabel *target, int iArgBytes);
        VOID X86EmitReturn(WORD wArgBytes);
#ifdef TARGET_AMD64
        VOID X86EmitLeaRIP(CodeLabel *target, X86Reg reg);
#endif

        VOID X86EmitCurrentThreadFetch(X86Reg dstreg, unsigned preservedRegSet);

        VOID X86EmitIndexRegLoad(X86Reg dstreg, X86Reg srcreg, __int32 ofs = 0);
        VOID X86EmitIndexRegStore(X86Reg dstreg, __int32 ofs, X86Reg srcreg);
#if defined(TARGET_AMD64)
        VOID X86EmitIndexRegStoreRSP(__int32 ofs, X86Reg srcreg);
        VOID X86EmitIndexRegStoreR12(__int32 ofs, X86Reg srcreg);
#endif // defined(TARGET_AMD64)

        VOID X86EmitIndexPush(X86Reg srcreg, __int32 ofs);
        VOID X86EmitBaseIndexPush(X86Reg baseReg, X86Reg indexReg, __int32 scale, __int32 ofs);
        VOID X86EmitIndexPop(X86Reg srcreg, __int32 ofs);
        VOID X86EmitIndexLea(X86Reg dstreg, X86Reg srcreg, __int32 ofs);
#if defined(TARGET_AMD64)
        VOID X86EmitIndexLeaRSP(X86Reg dstreg, X86Reg srcreg, __int32 ofs);
#endif // defined(TARGET_AMD64)

        VOID X86EmitSPIndexPush(__int32 ofs);
        VOID X86EmitSubEsp(INT32 imm32);
        VOID X86EmitAddEsp(INT32 imm32);
        VOID X86EmitEspOffset(BYTE opcode,
                              X86Reg altreg,
                              __int32 ofs
                    AMD64_ARG(X86OperandSize OperandSize = k64BitOp)
                              );
        VOID X86EmitPushEBPframe();

#if defined(TARGET_X86)
#if defined(PROFILING_SUPPORTED) && !defined(FEATURE_STUBS_AS_IL)
        // These are used to emit calls to notify the profiler of transitions in and out of
        // managed code through COM->COM+ interop or N/Direct
        VOID EmitProfilerComCallProlog(TADDR pFrameVptr, X86Reg regFrame);
        VOID EmitProfilerComCallEpilog(TADDR pFrameVptr, X86Reg regFrame);
#endif // PROFILING_SUPPORTED && !FEATURE_STUBS_AS_IL
#endif // TARGET_X86



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
                       __int32 ofs = 0,
                       X86Reg  scaledreg = (X86Reg)0,
                       BYTE    scale = 0
             AMD64_ARG(X86OperandSize OperandSize = k32BitOp)
                       );

#ifdef TARGET_AMD64
        FORCEINLINE
        VOID X86EmitOp(WORD    opcode,
                       X86Reg  altreg,
                       X86Reg  basereg,
                       __int32 ofs,
                       X86OperandSize OperandSize
                       )
        {
            X86EmitOp(opcode, altreg, basereg, ofs, (X86Reg)0, 0, OperandSize);
        }
#endif // TARGET_AMD64

        // Emits
        //
        //    opcode altreg, modrmreg
        //
        // or
        //
        //    opcode modrmreg, altreg
        //
        // (the opcode determines which one comes first)
        //
        // For single-operand opcodes, "altreg" actually selects
        // an operation rather than a register.

        VOID X86EmitR2ROp(WORD opcode,
                          X86Reg altreg,
                          X86Reg modrmreg
                AMD64_ARG(X86OperandSize OperandSize = k64BitOp)
                          );

        VOID X86EmitRegLoad(X86Reg reg, UINT_PTR imm);

        VOID X86EmitRegSave(X86Reg altreg, __int32 ofs)
        {
            LIMITED_METHOD_CONTRACT;
            X86EmitEspOffset(0x89, altreg, ofs);
            // X86Reg values never are outside a byte.
            UnwindSavedReg(static_cast<UCHAR>(altreg), ofs);
        }

        VOID X86_64BitOperands ()
        {
            WRAPPER_NO_CONTRACT;
#ifdef TARGET_AMD64
            Emit8(0x48);
#endif
        }

        VOID EmitEnable(CodeLabel *pForwardRef);
        VOID EmitRareEnable(CodeLabel *pRejoinPoint);

        VOID EmitDisable(CodeLabel *pForwardRef, BOOL fCallIn, X86Reg ThreadReg);
        VOID EmitRareDisable(CodeLabel *pRejoinPoint);
        VOID EmitRareDisableHRESULT(CodeLabel *pRejoinPoint, CodeLabel *pExitPoint);

        VOID EmitSetup(CodeLabel *pForwardRef);
        VOID EmitRareSetup(CodeLabel* pRejoinPoint, BOOL fThrow);

#ifndef FEATURE_STUBS_AS_IL
        VOID EmitMethodStubProlog(TADDR pFrameVptr, int transitionBlockOffset);
        VOID EmitMethodStubEpilog(WORD numArgBytes, int transitionBlockOffset);

        VOID EmitCheckGSCookie(X86Reg frameReg, int gsCookieOffset);

#ifdef TARGET_X86
        void EmitComMethodStubProlog(TADDR pFrameVptr, CodeLabel** rgRareLabels,
                                     CodeLabel** rgRejoinLabels, BOOL bShouldProfile);

        void EmitComMethodStubEpilog(TADDR pFrameVptr, CodeLabel** rgRareLabels,
                                     CodeLabel** rgRejoinLabels, BOOL bShouldProfile);
#endif // TARGET_X86
#endif // !FEATURE_STUBS_AS_IL

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

#if defined(FEATURE_COMINTEROP) && defined(TARGET_X86)
        //========================================================================
        //  shared Epilog for stubs that enter managed code from COM
        //  uses a return thunk within the method desc
        void EmitSharedComMethodStubEpilog(TADDR pFrameVptr,
                                           CodeLabel** rgRareLabels,
                                           CodeLabel** rgRejoinLabels,
                                           unsigned offsetReturnThunk,
                                           BOOL bShouldProfile);
#endif // FEATURE_COMINTEROP && TARGET_X86

#ifndef FEATURE_STUBS_AS_IL
        //===========================================================================
        // Computes hash code for MulticastDelegate.Invoke()
        static UINT_PTR HashMulticastInvoke(MetaSig* pSig);

#ifdef TARGET_X86
        //===========================================================================
        // Emits code for Delegate.Invoke() any delegate type
        VOID EmitDelegateInvoke();
#endif // TARGET_X86

#if defined(TARGET_X86) && !defined(FEATURE_MULTICASTSTUB_AS_IL)
        //===========================================================================
        // Emits code for MulticastDelegate.Invoke() - sig specific
        VOID EmitMulticastInvoke(UINT_PTR hash);
#endif // defined(TARGET_X86) && !defined(FEATURE_MULTICASTSTUB_AS_IL)
#endif // !FEATURE_STUBS_AS_IL

        //===========================================================================
        // Emits code to adjust for a static delegate target.
        VOID EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);


#ifndef FEATURE_ARRAYSTUB_AS_IL
        //===========================================================================
        // Emits code to do an array operation.
        VOID EmitArrayOpStub(const ArrayOpScript*);

        //Worker function to emit throw helpers for array ops.
        VOID EmitArrayOpStubThrow(unsigned exConst, unsigned cbRetArg);
#endif

#ifndef FEATURE_STUBS_AS_IL
        //===========================================================================
        // Emits code to break into debugger
        VOID EmitDebugBreak();
#endif // !FEATURE_STUBS_AS_IL

#if defined(_DEBUG) && !defined(TARGET_UNIX)
        //===========================================================================
        // Emits code to log JITHelper access
        void EmitJITHelperLoggingThunk(PCODE pJitHelper, LPVOID helperFuncCount);
#endif

#ifdef _DEBUG
        VOID X86EmitDebugTrashReg(X86Reg reg);
#endif

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
        virtual VOID EmitUnwindInfoCheckWorker (CodeLabel *pCheckLabel);
        virtual VOID EmitUnwindInfoCheckSubfunction();
#endif

    private:
        VOID X86EmitSubEspWorker(INT32 imm32);

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

//------------------------------------------------------------------------
//
// Precode definitions
//
//------------------------------------------------------------------------

EXTERN_C VOID STDCALL PrecodeFixupThunk();

#ifdef HOST_64BIT

#define OFFSETOF_PRECODE_TYPE              0
#define OFFSETOF_PRECODE_TYPE_CALL_OR_JMP  5
#define OFFSETOF_PRECODE_TYPE_MOV_R10     10

#define SIZEOF_PRECODE_BASE               16

#else

EXTERN_C VOID STDCALL PrecodeRemotingThunk();

#define OFFSETOF_PRECODE_TYPE              5
#define OFFSETOF_PRECODE_TYPE_CALL_OR_JMP  5
#define OFFSETOF_PRECODE_TYPE_MOV_RM_R     6

#define SIZEOF_PRECODE_BASE                8

#endif // HOST_64BIT


#include <pshpack1.h>

// Invalid precode type
struct InvalidPrecode {
    // int3
    static const int Type = 0xCC;
};


// Regular precode
struct StubPrecode {

#ifdef HOST_64BIT
    static const BYTE Type = 0xF8;
    // mov r10,pMethodDesc
    // clc
    // jmp Stub
#else
    static const BYTE Type = 0xED;
    // mov eax,pMethodDesc
    // mov ebp,ebp
    // jmp Stub
#endif // HOST_64BIT

    IN_TARGET_64BIT(USHORT m_movR10;)
    IN_TARGET_32BIT(BYTE   m_movEAX;)
    TADDR           m_pMethodDesc;
    IN_TARGET_32BIT(BYTE   m_mov_rm_r;)
    BYTE            m_type;
    BYTE            m_jmp;
    INT32           m_rel32;

    void Init(StubPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator = NULL, BYTE type = StubPrecode::Type, TADDR target = NULL);

    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pMethodDesc;
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return rel32Decode(PTR_HOST_MEMBER_TADDR(StubPrecode, this, m_rel32));
    }
#ifndef DACCESS_COMPILE
    void ResetTargetInterlocked()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        ExecutableWriterHolder<INT32> rel32WriterHolder(&m_rel32, sizeof(INT32));
        rel32SetInterlocked(&m_rel32, rel32WriterHolder.GetRW(), GetPreStubEntryPoint(), (MethodDesc*)GetMethodDesc());
    }

    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        ExecutableWriterHolder<void> rel32Holder(&m_rel32, 4);
        return rel32SetInterlocked(&m_rel32, rel32Holder.GetRW(), target, expected, (MethodDesc*)GetMethodDesc());
    }
#endif // !DACCESS_COMPILE
};
IN_TARGET_64BIT(static_assert_no_msg(offsetof(StubPrecode, m_movR10) == OFFSETOF_PRECODE_TYPE);)
IN_TARGET_64BIT(static_assert_no_msg(offsetof(StubPrecode, m_type) == OFFSETOF_PRECODE_TYPE_MOV_R10);)
IN_TARGET_32BIT(static_assert_no_msg(offsetof(StubPrecode, m_mov_rm_r) == OFFSETOF_PRECODE_TYPE);)
IN_TARGET_32BIT(static_assert_no_msg(offsetof(StubPrecode, m_type) == OFFSETOF_PRECODE_TYPE_MOV_RM_R);)
typedef DPTR(StubPrecode) PTR_StubPrecode;


#ifdef HAS_NDIRECT_IMPORT_PRECODE

// NDirect import precode
// (This is fake precode. VTable slot does not point to it.)
struct NDirectImportPrecode : StubPrecode {

#ifdef HOST_64BIT
    static const int Type = 0xF9;
    // mov r10,pMethodDesc
    // stc
    // jmp NDirectImportThunk
#else
    static const int Type = 0xC0;
    // mov eax,pMethodDesc
    // mov eax,eax
    // jmp NDirectImportThunk
#endif // HOST_64BIT

    void Init(NDirectImportPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    LPVOID GetEntrypoint()
    {
        LIMITED_METHOD_CONTRACT;
        return this;
    }
};
typedef DPTR(NDirectImportPrecode) PTR_NDirectImportPrecode;

#endif // HAS_NDIRECT_IMPORT_PRECODE


#ifdef HAS_FIXUP_PRECODE

// Fixup precode is used in ngen images when the prestub does just one time fixup.
// The fixup precode is simple jump once patched. It does not have the two instruction overhead of regular precode.
struct FixupPrecode {

    static const int TypePrestub = 0x5E;
    // The entrypoint has to be 8-byte aligned so that the "call PrecodeFixupThunk" can be patched to "jmp NativeCode" atomically.
    // call PrecodeFixupThunk
    // db TypePrestub (pop esi)
    // db MethodDescChunkIndex
    // db PrecodeChunkIndex

    static const int Type = 0x5F;
    // After it has been patched to point to native code
    // jmp NativeCode
    // db Type (pop edi)

    BYTE            m_op;
    INT32           m_rel32;
    BYTE            m_type;
    BYTE            m_MethodDescChunkIndex;
    BYTE            m_PrecodeChunkIndex;
#ifdef HAS_FIXUP_PRECODE_CHUNKS
    // Fixup precode chunk is associated with MethodDescChunk. The layout of the fixup precode chunk is:
    //
    // FixupPrecode     Entrypoint PrecodeChunkIndex = 2
    // FixupPrecode     Entrypoint PrecodeChunkIndex = 1
    // FixupPrecode     Entrypoint PrecodeChunkIndex = 0
    // TADDR            Base of MethodDescChunk
#else
    TADDR           m_pMethodDesc;
#endif

    void Init(FixupPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator, int iMethodDescChunkIndex = 0, int iPrecodeChunkIndex = 0);

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    TADDR GetBase()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<TADDR>(this) + (m_PrecodeChunkIndex + 1) * sizeof(FixupPrecode);
    }

    size_t GetSizeRW()
    {
        LIMITED_METHOD_CONTRACT;

        return GetBase() + sizeof(void*) - dac_cast<TADDR>(this);
    }

    TADDR GetMethodDesc();
#else // HAS_FIXUP_PRECODE_CHUNKS
    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pMethodDesc;
    }
#endif // HAS_FIXUP_PRECODE_CHUNKS

#ifdef FIXUP_PRECODE_PREALLOCATE_DYNAMIC_METHOD_JUMP_STUBS
    PCODE GetDynamicMethodPrecodeFixupJumpStub();
    PCODE GetDynamicMethodEntryJumpStub();
#endif // FIXUP_PRECODE_PREALLOCATE_DYNAMIC_METHOD_JUMP_STUBS

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return rel32Decode(PTR_HOST_MEMBER_TADDR(FixupPrecode, this, m_rel32));
    }

    void ResetTargetInterlocked();
    BOOL SetTargetInterlocked(TADDR target, TADDR expected);

    static BOOL IsFixupPrecodeByASM(TADDR addr)
    {
        LIMITED_METHOD_CONTRACT;

        return *dac_cast<PTR_BYTE>(addr) == X86_INSTR_JMP_REL32;
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};
IN_TARGET_32BIT(static_assert_no_msg(offsetof(FixupPrecode, m_type) == OFFSETOF_PRECODE_TYPE));
IN_TARGET_64BIT(static_assert_no_msg(offsetof(FixupPrecode, m_op)   == OFFSETOF_PRECODE_TYPE);)
IN_TARGET_64BIT(static_assert_no_msg(offsetof(FixupPrecode, m_type) == OFFSETOF_PRECODE_TYPE_CALL_OR_JMP);)

typedef DPTR(FixupPrecode) PTR_FixupPrecode;

#endif // HAS_FIXUP_PRECODE

#ifdef HAS_THISPTR_RETBUF_PRECODE

// Precode to stuffle this and retbuf for closed delegates over static methods with return buffer
struct ThisPtrRetBufPrecode {

#ifdef HOST_64BIT
    static const int Type = 0x90;
#else
    static const int Type = 0xC2;
#endif // HOST_64BIT

    // mov regScratch,regArg0
    // mov regArg0,regArg1
    // mov regArg1,regScratch
    // nop
    // jmp EntryPoint
    // dw pMethodDesc

    IN_TARGET_64BIT(BYTE   m_nop1;)
    IN_TARGET_64BIT(BYTE   m_prefix1;)
    WORD            m_movScratchArg0;
    IN_TARGET_64BIT(BYTE   m_prefix2;)
    WORD            m_movArg0Arg1;
    IN_TARGET_64BIT(BYTE   m_prefix3;)
    WORD            m_movArg1Scratch;
    BYTE            m_nop2;
    BYTE            m_jmp;
    INT32           m_rel32;
    TADDR           m_pMethodDesc;

    void Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return m_pMethodDesc;
    }

    PCODE GetTarget();

    BOOL SetTargetInterlocked(TADDR target, TADDR expected);
};
IN_TARGET_32BIT(static_assert_no_msg(offsetof(ThisPtrRetBufPrecode, m_movArg1Scratch) + 1 == OFFSETOF_PRECODE_TYPE);)
typedef DPTR(ThisPtrRetBufPrecode) PTR_ThisPtrRetBufPrecode;

#endif // HAS_THISPTR_RETBUF_PRECODE

#include <poppack.h>

#endif  // STUBLINKERX86_H_
