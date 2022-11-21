// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubs.cpp
//
// This file contains stub functions for unimplemented features need to
// run on the ARM64 platform.

#include "common.h"
#include "dllimportcallback.h"
#include "comdelegate.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "jitinterface.h"
#include "ecall.h"


#ifndef DACCESS_COMPILE
//-----------------------------------------------------------------------
// InstructionFormat for JAL/JALR (unconditional jump)
//-----------------------------------------------------------------------
class BranchInstructionFormat : public InstructionFormat
{
    // Encoding of the VariationCode:
    // bit(0) indicates whether this is a direct or an indirect jump.
    // bit(1) indicates whether this is a branch with link -a.k.a call-

    public:
        enum VariationCodes
        {
            BIF_VAR_INDIRECT           = 0x00000001,
            BIF_VAR_CALL               = 0x00000002,

            BIF_VAR_JUMP               = 0x00000000,
            BIF_VAR_INDIRECT_CALL      = 0x00000003
        };
    private:
        BOOL IsIndirect(UINT variationCode)
        {
            return (variationCode & BIF_VAR_INDIRECT) != 0;
        }
        BOOL IsCall(UINT variationCode)
        {
            return (variationCode & BIF_VAR_CALL) != 0;
        }


    public:
        BranchInstructionFormat() : InstructionFormat(InstructionFormat::k64)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refSize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(refSize == InstructionFormat::k64);

            if (IsIndirect(variationCode))
                return 16;
            else
                return 12;
        }

        virtual UINT GetSizeOfData(UINT refSize, UINT variationCode)
        {
            WRAPPER_NO_CONTRACT;
            return 8;
        }


        virtual UINT GetHotSpotOffset(UINT refsize, UINT variationCode)
        {
            WRAPPER_NO_CONTRACT;
            return 0;
        }

        virtual BOOL CanReach(UINT refSize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            if (fExternal)
            {
                // Note that the parameter 'offset' is not an offset but the target address itself (when fExternal is true)
                return (refSize == InstructionFormat::k64);
            }
            else
            {
                return ((offset >= -0x80000000L && offset <= 0x7fffffff) || (refSize == InstructionFormat::k64));
            }
        }

        virtual VOID EmitInstruction(UINT refSize, __int64 fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT;

            if (IsIndirect(variationCode))
            {
                _ASSERTE(((UINT_PTR)pDataBuffer & 7) == 0);

                __int64 dataOffset = pDataBuffer - pOutBufferRW;

                if ((dataOffset < -(0x80000000L)) || (dataOffset > 0x7fffffff))
                    COMPlusThrow(kNotSupportedException);

                UINT32 imm12 = (UINT32)(0xFFF & dataOffset);
                //auipc  t1, dataOffset[31:12]
                //ld  t1, t1, dataOffset[11:0]
                //ld  t1, t1, 0
                //jalr  x0/1, t1,0

                *(DWORD*)pOutBufferRW = 0x00000317 | (((dataOffset + 0x800) >> 12) << 12);// auipc t1, dataOffset[31:12]
                *(DWORD*)(pOutBufferRW + 4) = 0x00033303 | (imm12 << 20); // ld  t1, t1, dataOffset[11:0]
                *(DWORD*)(pOutBufferRW + 8) = 0x00033303; // ld  t1, t1, 0
                if (IsCall(variationCode))
                {
                    *(DWORD*)(pOutBufferRW + 12) = 0x000300e7; // jalr  ra, t1, 0
                }
                else
                {
                    *(DWORD*)(pOutBufferRW + 12) = 0x00030067 ;// jalr  x0, t1,0
                }

                *((__int64*)pDataBuffer) = fixedUpReference + (__int64)pOutBufferRX;
            }
            else
            {
                _ASSERTE(((UINT_PTR)pDataBuffer & 7) == 0);

                __int64 dataOffset = pDataBuffer - pOutBufferRW;

                if ((dataOffset < -(0x80000000L)) || (dataOffset > 0x7fffffff))
                    COMPlusThrow(kNotSupportedException);

                UINT16 imm12 = (UINT16)(0xFFF & dataOffset);
                //auipc  t1, dataOffset[31:12]
                //ld  t1, t1, dataOffset[11:0]
                //jalr  x0/1, t1,0

                *(DWORD*)pOutBufferRW = 0x00000317 | (((dataOffset + 0x800) >> 12) << 12);// auipc t1, dataOffset[31:12]
                *(DWORD*)(pOutBufferRW + 4) = 0x00033303 | (imm12 << 20); // ld  t1, t1, dataOffset[11:0]
                if (IsCall(variationCode))
                {
                    *(DWORD*)(pOutBufferRW + 8) = 0x000300e7; // jalr  ra, t1, 0
                }
                else
                {
                    *(DWORD*)(pOutBufferRW + 8) = 0x00030067 ;// jalr  x0, t1,0
                }

                if (!ClrSafeInt<__int64>::addition(fixedUpReference, (__int64)pOutBufferRX, fixedUpReference))
                    COMPlusThrowArithmetic();
                *((__int64*)pDataBuffer) = fixedUpReference;
            }
        }
};

static BYTE gBranchIF[sizeof(BranchInstructionFormat)];

#endif

void ClearRegDisplayArgumentAndScratchRegisters(REGDISPLAY * pRD)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* unwoundstate,
                                    DWORD threadId,
                                    int funCallDepth,
                                    HostCallPreference hostCallPreference)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void HelperMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

#ifndef DACCESS_COMPILE
void ThisPtrRetBufPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

#endif // !DACCESS_COMPILE

void UpdateRegDisplayFromCalleeSavedRegisters(REGDISPLAY * pRD, CalleeSavedRegisters * pCalleeSaved)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}


void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}



void FaultingExceptionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void InlinedCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    RETURN;
}

#ifdef FEATURE_HIJACK
TADDR ResumableFrame::GetReturnAddressPtr(void)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(T_CONTEXT, Pc);
}

void ResumableFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    RETURN;
}

void HijackFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}
#endif // FEATURE_HIJACK

#ifdef FEATURE_COMINTEROP

void emitCOMStubCall (ComCallMethodDesc *pCOMMethodRX, ComCallMethodDesc *pCOMMethodRW, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}
#endif // FEATURE_COMINTEROP

void JIT_TailCall()
{
    _ASSERTE(!"RISCV64:NYI");
}

#if !defined(DACCESS_COMPILE)
EXTERN_C void JIT_UpdateWriteBarrierState(bool skipEphemeralCheck, size_t writeableOffset);

extern "C" void STDCALL JIT_PatchedCodeStart();
extern "C" void STDCALL JIT_PatchedCodeLast();

static void UpdateWriteBarrierState(bool skipEphemeralCheck)
{
    BYTE *writeBarrierCodeStart = GetWriteBarrierCodeLocation((void*)JIT_PatchedCodeStart);
    BYTE *writeBarrierCodeStartRW = writeBarrierCodeStart;
    ExecutableWriterHolderNoLog<BYTE> writeBarrierWriterHolder;
    if (IsWriteBarrierCopyEnabled())
    {
        writeBarrierWriterHolder.AssignExecutableWriterHolder(writeBarrierCodeStart, (BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart);
        writeBarrierCodeStartRW = writeBarrierWriterHolder.GetRW();
    }
    JIT_UpdateWriteBarrierState(GCHeapUtilities::IsServerHeap(), writeBarrierCodeStartRW - writeBarrierCodeStart);
}

void InitJITHelpers1()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 0);

    // Allocation helpers, faster but non-logging
    if (!((TrackAllocationsEnabled()) ||
        (LoggingOn(LF_GCALLOC, LL_INFO10))
#ifdef _DEBUG
        || (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP) != 0)
#endif // _DEBUG
        ))
    {
        if (GCHeapUtilities::UseThreadAllocationContexts())
        {
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_NewS_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_NewS_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_MP_FastPortable);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateString_MP_FastPortable), ECall::FastAllocateString);
        }
    }

    UpdateWriteBarrierState(GCHeapUtilities::IsServerHeap());
}

#else
void UpdateWriteBarrierState(bool) {}
#endif // !defined(DACCESS_COMPILE)

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_DISPATCHER_CONTEXT * pDispatcherContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    DWORD64 stackSlot = pDispatcherContext->EstablisherFrame + REDIRECTSTUB_SP_OFFSET_CONTEXT;
    PTR_PTR_CONTEXT ppContext = dac_cast<PTR_PTR_CONTEXT>((TADDR)stackSlot);
    return *ppContext;
}

PTR_CONTEXT GetCONTEXTFromRedirectedStubStackFrame(T_CONTEXT * pContext)
{
    LIMITED_METHOD_DAC_CONTRACT;

    DWORD64 stackSlot = pContext->Sp + REDIRECTSTUB_SP_OFFSET_CONTEXT;
    PTR_PTR_CONTEXT ppContext = dac_cast<PTR_PTR_CONTEXT>((TADDR)stackSlot);
    return *ppContext;
}

void RedirectForThreadAbort()
{
    // ThreadAbort is not supported in .net core
    throw "NYI";
}

#if !defined(DACCESS_COMPILE)
FaultingExceptionFrame *GetFrameFromRedirectedStubStackFrame (DISPATCHER_CONTEXT *pDispatcherContext)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    LIMITED_METHOD_CONTRACT;

    return (FaultingExceptionFrame*)NULL;
}


BOOL
AdjustContextForVirtualStub(
        EXCEPTION_RECORD *pExceptionRecord,
        CONTEXT *pContext)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return TRUE;
}
#endif // !DACCESS_COMPILE

UMEntryThunk * UMEntryThunk::Decode(void *pCallback)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

void UMEntryThunkCode::Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

#ifndef DACCESS_COMPILE

void UMEntryThunkCode::Poison()
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

#endif // DACCESS_COMPILE

#if !defined(DACCESS_COMPILE)
VOID ResetCurrentContext()
{
    LIMITED_METHOD_CONTRACT;
}
#endif

LONG CLRNoCatchHandler(EXCEPTION_POINTERS* pExceptionInfo, PVOID pv)
{
    return EXCEPTION_CONTINUE_SEARCH;
}

void FlushWriteBarrierInstructionCache()
{
    // this wouldn't be called in arm64, just to comply with gchelpers.h
}

int StompWriteBarrierEphemeral(bool isRuntimeSuspended)
{
    UpdateWriteBarrierState(GCHeapUtilities::IsServerHeap());
    return SWB_PASS;
}

int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    UpdateWriteBarrierState(GCHeapUtilities::IsServerHeap());
    return SWB_PASS;
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
int SwitchToWriteWatchBarrier(bool isRuntimeSuspended)
{
    UpdateWriteBarrierState(GCHeapUtilities::IsServerHeap());
    return SWB_PASS;
}

int SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended)
{
    UpdateWriteBarrierState(GCHeapUtilities::IsServerHeap());
    return SWB_PASS;
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifdef DACCESS_COMPILE
BOOL GetAnyThunkTarget (T_CONTEXT *pctx, TADDR *pTarget, TADDR *pTargetMethodDesc)
{
    _ASSERTE(!"RISCV64:NYI");
    return FALSE;
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
// ----------------------------------------------------------------
// StubLinkerCPU methods
// ----------------------------------------------------------------

void StubLinkerCPU::EmitMovConstant(IntReg target, UINT64 constant)
{
    if (0 == ((constant + 0x800) >> 32)) {
        if (((constant + 0x800) >> 12) != 0)
        {
            Emit32((DWORD)(0x00000037 | (((constant + 0x800) >> 12) << 12) | (target << 7))); // lui target, (constant + 0x800) >> 12
            if ((constant & 0xFFF) != 0)
            {
                Emit32((DWORD)(0x00000013 | (constant & 0xFFF) << 20 | (target << 7))); // addi target, constant
            }
        }
        else
        {
            Emit32((DWORD)(0x00000013 | (constant & 0xFFF) << 20 | (target << 7))); // addi target, constant
        }
    }
    else
    {
        UINT32 upper = constant >> 32;
        if (((upper + 0x800) >> 12) != 0)
        {
            Emit32((DWORD)(0x00000037 | (((upper + 0x800) >> 12) << 12) | (target << 7))); // lui target, (constant + 0x800) >> 12
        }
        if ((upper & 0xFFF) != 0)
        {
            Emit32((DWORD)(0x00000013 | (upper & 0xFFF) << 20 | (target << 7))); // addi target, constant
        }
        UINT32 lower = (constant << 32) >> 32;
        UINT32 shift = 0;
        for (int i = 32; i >= 0; i -= 11)
        {
            shift += i > 11 ? 11 : i;
            UINT32 current = lower >> (i < 11 ? 0 : i - 11);
            if (current != 0)
            {
                Emit32((DWORD)(0x00001013 | (shift << 20) | (target << 7) | (target << 15))); // slli target, target, shift
                Emit32((DWORD)(0x00000013 | (current & 0x7FF) << 20 | (target << 7))); // addi target, current
                shift = 0;
            }
        }
        if (shift)
        {
            Emit32((DWORD)(0x00001013 | (shift << 20) | (target << 7) | (target << 15))); // slli target, target, shift
        }
    }
}

void StubLinkerCPU::EmitCmpImm(IntReg reg, int imm)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void StubLinkerCPU::EmitCmpReg(IntReg Xn, IntReg Xm)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void StubLinkerCPU::EmitCondFlagJump(CodeLabel * target, UINT cond)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void StubLinkerCPU::EmitJumpRegister(IntReg regTarget)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void StubLinkerCPU::EmitProlog(unsigned short cIntRegArgs, unsigned short cFloatRegArgs, unsigned short cCalleeSavedRegs, unsigned short cbStackSpace)
{
    _ASSERTE(!m_fProlog);

    unsigned short numberOfEntriesOnStack  = 2 + cIntRegArgs + cFloatRegArgs + cCalleeSavedRegs; // 2 for fp, ra

    // Stack needs to be 16 byte (2 qword) aligned. Compute the required padding before saving it
    unsigned short totalPaddedFrameSize = static_cast<unsigned short>(ALIGN_UP(cbStackSpace + numberOfEntriesOnStack *sizeof(void*), 2 * sizeof(void*)));
    // The padding is going to be applied to the local stack
    cbStackSpace =  totalPaddedFrameSize - numberOfEntriesOnStack * sizeof(void*);

    // Record the parameters of this prolog so that we can generate a matching epilog and unwind info.
    DescribeProlog(cIntRegArgs, cFloatRegArgs, cCalleeSavedRegs, cbStackSpace);


    // N.B Despite the range of a jump with a sub sp is 4KB, we're limiting to 504 to save from emitting right prolog that's
    // expressable in unwind codes efficiently. The largest offset in typical unwindinfo encodings that we use is 504.
    // so allocations larger than 504 bytes would require setting the SP in multiple strides, which would complicate both
    // prolog and epilog generation as well as unwindinfo generation.
    _ASSERTE((totalPaddedFrameSize <= 504) && "NYI:RISCV64 Implement StubLinker prologs with larger than 504 bytes of frame size");
    if (totalPaddedFrameSize > 504)
        COMPlusThrow(kNotSupportedException);

    // Regarding the order of operations in the prolog and epilog;
    // If the prolog and the epilog matches each other we can simplify emitting the unwind codes and save a few
    // bytes of unwind codes by making prolog and epilog share the same unwind codes.
    // In order to do that we need to make the epilog be the reverse of the prolog.
    // But we wouldn't want to add restoring of the argument registers as that's completely unnecessary.
    // Besides, saving argument registers cannot be expressed by the unwind code encodings.
    // So, we'll push saving the argument registers to the very last in the prolog, skip restoring it in epilog,
    // and also skip reporting it to the OS.
    //
    // Another bit that we can save is resetting the frame pointer.
    // This is not necessary when the SP doesn't get modified beyond prolog and epilog. (i.e no alloca/localloc)
    // And in that case we don't need to report setting up the FP either.


    // 1. Relocate SP
    EmitSubImm(RegSp, RegSp, totalPaddedFrameSize);

    unsigned cbOffset = 2 * sizeof(void*) + cbStackSpace; // 2 is for fp, ra

    // 2. Store callee-saved registers
#if 0
    _ASSERTE(cCalleeSavedRegs <= 13);
    if (cCalleeSavedRegs != 0)
    {
        EmitLoadStoreRegPairImm(eSTORE, IntReg(3), IntReg(4), RegSp, cbOffset);
        EmitLoadStoreRegPairImm(eSTORE, IntReg(9), IntReg(18), RegSp, cbOffset + 2 * sizeof(void*));
        EmitLoadStoreRegPairImm(eSTORE, IntReg(19), IntReg(20), RegSp, cbOffset + 4 * sizeof(void*));
        EmitLoadStoreRegPairImm(eSTORE, IntReg(21), IntReg(22), RegSp, cbOffset + 6 * sizeof(void*));
        EmitLoadStoreRegPairImm(eSTORE, IntReg(23), IntReg(24), RegSp, cbOffset + 8 * sizeof(void*));
        EmitLoadStoreRegPairImm(eSTORE, IntReg(25), IntReg(26), RegSp, cbOffset + 10 * sizeof(void*));
        EmitLoadStoreRegImm(eSTORE, IntReg(27), RegSp, cbOffset + 12 * sizeof(void*));
    }
#endif

    // 3. Store FP/RA
    EmitLoadStoreRegPairImm(eSTORE, RegFp, RegRa, RegSp, cbStackSpace);

    // 4. Set the frame pointer
    EmitMovReg(RegFp, RegSp);

    // 5. Store floating point argument registers
    cbOffset += cCalleeSavedRegs * sizeof(void*);
    _ASSERTE(cFloatRegArgs <= 8);
    for (unsigned short i = 0; i < (cFloatRegArgs / 2) * 2; i += 2)
        EmitLoadStoreRegPairImm(eSTORE, FloatReg(i + 10), FloatReg(i + 11), RegSp, cbOffset + i * sizeof(void*));
    if ((cFloatRegArgs % 2) == 1)
        EmitLoadStoreRegImm(eSTORE, FloatReg(cFloatRegArgs - 1 + 10), RegSp, cbOffset + (cFloatRegArgs - 1) * sizeof(void*));

    // 6. Store int argument registers
    cbOffset += cFloatRegArgs * sizeof(void*);
    _ASSERTE(cIntRegArgs <= 8);
    for (unsigned short i = 0 ; i < (cIntRegArgs / 2) * 2; i += 2)
        EmitLoadStoreRegPairImm(eSTORE, IntReg(i + 10), IntReg(i + 11), RegSp, cbOffset + i * sizeof(void*));
    if ((cIntRegArgs % 2) == 1)
        EmitLoadStoreRegImm(eSTORE,IntReg(cIntRegArgs-1 + 10), RegSp, cbOffset + (cIntRegArgs - 1) * sizeof(void*));
}

void StubLinkerCPU::EmitEpilog()
{
    _ASSERTE(m_fProlog);

    // 6. Restore int argument registers
    //    nop: We don't need to. They are scratch registers

    // 5. Restore floating point argument registers
    //    nop: We don't need to. They are scratch registers

    // 4. Restore the SP from FP
    //    N.B. We're assuming that the stublinker stubs doesn't do alloca, hence nop

    // 3. Restore FP/RA
    EmitLoadStoreRegPairImm(eLOAD, RegFp, RegRa, RegSp, m_cbStackSpace);

    // 2. restore the calleeSavedRegisters
    unsigned cbOffset = 2*sizeof(void*) + m_cbStackSpace; // 2 is for fp,lr
    if ((m_cCalleeSavedRegs % 2) ==1)
        EmitLoadStoreRegImm(eLOAD, IntReg(m_cCalleeSavedRegs - 1), RegSp, cbOffset + (m_cCalleeSavedRegs - 1) * sizeof(void*));
    for (int i = (m_cCalleeSavedRegs / 2) * 2 - 2; i >= 0; i -= 2)
        EmitLoadStoreRegPairImm(eLOAD, IntReg(19 + i), IntReg(19 + i + 1), RegSp, cbOffset + i * sizeof(void*));

    // 1. Restore SP
    EmitAddImm(RegSp, RegSp, GetStackFrameSize());
    EmitRet(RegRa);
;
}

void StubLinkerCPU::EmitRet(IntReg Xn)
{
    Emit32((DWORD)(0x00000067 | (Xn << 15))); // jalr X0, 0(Xn)
}

void StubLinkerCPU::EmitLoadStoreRegPairImm(DWORD flags, IntReg Xt1, IntReg Xt2, IntReg Xn, int offset)
{
    _ASSERTE((-1024 <= offset) && (offset <= 1015));
    _ASSERTE((offset & 7) == 0);

    BOOL isLoad = flags & 1;
    if (isLoad) {
        // ld Xt1, offset(Xn));
        Emit32((DWORD)(0x00003003 | (Xt1 << 7) | (Xn << 15) | (offset << 20)));
        // ld Xt2, (offset+8)(Xn));
        Emit32((DWORD)(0x00003003 | (Xt2 << 7) | (Xn << 15) | ((offset + 8) << 20)));
    } else {
        // sd Xt1, offset(Xn)
        Emit32((DWORD)(0x00003023 | (Xt1 << 20) | (Xn << 15) | (offset & 0xF) << 7 | (((offset >> 4) & 0xFF) << 25)));
        // sd Xt1, (offset + 8)(Xn)
        Emit32((DWORD)(0x00003023 | (Xt2 << 20) | (Xn << 15) | ((offset + 8) & 0xF) << 7 | ((((offset + 8) >> 4) & 0xFF) << 25)));
    }
}

void StubLinkerCPU::EmitLoadStoreRegPairImm(DWORD flags, FloatReg Ft1, FloatReg Ft2, IntReg Xn, int offset)
{
    _ASSERTE((-1024 <= offset) && (offset <= 1015));
    _ASSERTE((offset & 7) == 0);

    BOOL isLoad = flags & 1;
    if (isLoad) {
        // fld Ft, Xn, offset
        Emit32((DWORD)(0x00003007 | (Xn << 15) | (Ft1 << 7) | (offset << 20)));
        // fld Ft, Xn, offset + 8
        Emit32((DWORD)(0x00003007 | (Xn << 15) | (Ft2 << 7) | ((offset + 8) << 20)));
    } else {
        // fsd Ft, offset(Xn)
        Emit32((WORD)(0x00003027 | (Xn << 15) | (Ft1 << 20) | (offset & 0xF) << 7 | ((offset >> 4) & 0xFF)));
        // fsd Ft, (offset + 8)(Xn)
        Emit32((WORD)(0x00003027 | (Xn << 15) | (Ft2 << 20) | ((offset + 8) & 0xF) << 7 | (((offset + 8) >> 4) & 0xFF)));
    }
}

void StubLinkerCPU::EmitLoadStoreRegImm(DWORD flags, IntReg Xt, IntReg Xn, int offset)
{
    BOOL isLoad    = flags & 1;
    if (isLoad) {
        // ld regNum, offset(Xn);
        Emit32((DWORD)(0x00003003 | (Xt << 7) | (Xn << 15) | (offset << 20)));
    } else {
        // sd regNum, offset(Xn)
        Emit32((DWORD)(0x00003023 | (Xt << 20) | (Xn << 15) | (offset & 0xF) << 7 | (((offset >> 4) & 0xFF) << 25)));
    }
}

void StubLinkerCPU::EmitLoadStoreRegImm(DWORD flags, FloatReg Ft, IntReg Xn, int offset)
{
    BOOL isLoad    = flags & 1;
    if (isLoad) {
        // fld Ft, Xn, offset
        Emit32((DWORD)(0x00003007 | (Xn << 15) | (Ft << 7) | (offset << 20)));
    } else {
        // fsd Ft, offset(Xn)
        Emit32((WORD)(0x00003027 | (Xn << 15) | (Ft << 20) | (offset & 0xF) << 7 | ((offset >> 4) & 0xFF)));
    }
}

// Load Register (Register Offset)
void StubLinkerCPU::EmitLoadRegReg(IntReg Xt, IntReg Xn, IntReg Xm, DWORD option)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void StubLinkerCPU::EmitMovReg(IntReg Xd, IntReg Xm)
{
    Emit32(0x00000013 | (Xm << 15) | (Xd << 7));
}

void StubLinkerCPU::EmitSubImm(IntReg Xd, IntReg Xn, unsigned int value)
{
    _ASSERTE((0 <= value) && (value <= 0x7FF));
    Emit32((DWORD)(0x00000013 | (((~value + 0x1) & 0xFFF) << 20) | (Xn << 15) | (Xd << 7))); // addi Xd, Xn, (~value + 0x1) & 0xFFF
}

void StubLinkerCPU::EmitAddImm(IntReg Xd, IntReg Xn, unsigned int value)
{
    _ASSERTE((0 <= value) && (value <= 0x7FF));
    Emit32((DWORD)(0x00000013 | (value << 20) | (Xn << 15) | (Xd << 7))); // addi Xd, Xn, value
}

void StubLinkerCPU::EmitCallRegister(IntReg reg)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void StubLinkerCPU::Init()
{
    new (gBranchIF) BranchInstructionFormat();
}

// Emits code to adjust arguments for static delegate target.
VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

// Emits code to adjust arguments for static delegate target.
VOID StubLinkerCPU::EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

void StubLinkerCPU::EmitCallLabel(CodeLabel *target, BOOL fTailCall, BOOL fIndirect)
{
    BranchInstructionFormat::VariationCodes variationCode = BranchInstructionFormat::VariationCodes::BIF_VAR_JUMP;
    if (!fTailCall)
        variationCode = static_cast<BranchInstructionFormat::VariationCodes>(variationCode | BranchInstructionFormat::VariationCodes::BIF_VAR_CALL);
    if (fIndirect)
        variationCode = static_cast<BranchInstructionFormat::VariationCodes>(variationCode | BranchInstructionFormat::VariationCodes::BIF_VAR_INDIRECT);

    EmitLabelRef(target, reinterpret_cast<BranchInstructionFormat&>(gBranchIF), (UINT)variationCode);
}

void StubLinkerCPU::EmitCallManagedMethod(MethodDesc *pMD, BOOL fTailCall)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}


#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//

#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");

// Uses x8 as scratch register to store address of data label
// After load x8 is increment to point to next data
// only accepts positive offsets
static void LoadRegPair(BYTE* p, int reg1, int reg2, UINT32 offset)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

// Caller must ensure sufficient byte are allocated including padding (if applicable)
void DynamicHelpers::EmitHelperWithArg(BYTE*& p, size_t rxOffset, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateReturn(LoaderAllocator * pAllocator)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
    return NULL;
}
#endif // FEATURE_READYTORUN


#endif // #ifndef DACCESS_COMPILE
