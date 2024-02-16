// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubs.cpp
//
// This file contains stub functions for unimplemented features need to
// run on the RISCV64 platform.

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

                UINT16 imm12 = (UINT16)(0xFFF & dataOffset);
                // auipc  t1, dataOffset[31:12]
                // ld  t1, t1, dataOffset[11:0]
                // ld  t1, t1, 0
                // jalr  x0/1, t1,0

                *(DWORD*)pOutBufferRW = 0x00000317 | (((dataOffset + 0x800) >> 12) << 12); // auipc t1, dataOffset[31:12]
                *(DWORD*)(pOutBufferRW + 4) = 0x00033303 | (imm12 << 20); // ld  t1, t1, dataOffset[11:0]
                *(DWORD*)(pOutBufferRW + 8) = 0x00033303; // ld  t1, 0(t1)
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
                // auipc  t1, dataOffset[31:12]
                // ld  t1, t1, dataOffset[11:0]
                // jalr  x0/1, t1,0

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
    pRD->volatileCurrContextPointers.R0 = NULL;
    pRD->volatileCurrContextPointers.A0 = NULL;
    pRD->volatileCurrContextPointers.A1 = NULL;
    pRD->volatileCurrContextPointers.A2 = NULL;
    pRD->volatileCurrContextPointers.A3 = NULL;
    pRD->volatileCurrContextPointers.A4 = NULL;
    pRD->volatileCurrContextPointers.A5 = NULL;
    pRD->volatileCurrContextPointers.A6 = NULL;
    pRD->volatileCurrContextPointers.A7 = NULL;
    pRD->volatileCurrContextPointers.T0 = NULL;
    pRD->volatileCurrContextPointers.T1 = NULL;
    pRD->volatileCurrContextPointers.T2 = NULL;
    pRD->volatileCurrContextPointers.T3 = NULL;
    pRD->volatileCurrContextPointers.T4 = NULL;
    pRD->volatileCurrContextPointers.T5 = NULL;
    pRD->volatileCurrContextPointers.T6 = NULL;
}

void LazyMachState::unwindLazyState(LazyMachState* baseState,
                                    MachState* unwoundstate,
                                    DWORD threadId,
                                    int funCallDepth,
                                    HostCallPreference hostCallPreference)
{
    T_CONTEXT context;
    T_KNONVOLATILE_CONTEXT_POINTERS nonVolContextPtrs;

    context.ContextFlags = 0; // Read by PAL_VirtualUnwind.

    context.Fp = unwoundstate->captureCalleeSavedRegisters[0] = baseState->captureCalleeSavedRegisters[0];
    context.S1 = unwoundstate->captureCalleeSavedRegisters[1] = baseState->captureCalleeSavedRegisters[1];
    context.S2 = unwoundstate->captureCalleeSavedRegisters[2] = baseState->captureCalleeSavedRegisters[2];
    context.S3 = unwoundstate->captureCalleeSavedRegisters[3] = baseState->captureCalleeSavedRegisters[3];
    context.S4 = unwoundstate->captureCalleeSavedRegisters[4] = baseState->captureCalleeSavedRegisters[4];
    context.S5 = unwoundstate->captureCalleeSavedRegisters[5] = baseState->captureCalleeSavedRegisters[5];
    context.S6 = unwoundstate->captureCalleeSavedRegisters[6] = baseState->captureCalleeSavedRegisters[6];
    context.S7 = unwoundstate->captureCalleeSavedRegisters[7] = baseState->captureCalleeSavedRegisters[7];
    context.S8 = unwoundstate->captureCalleeSavedRegisters[8] = baseState->captureCalleeSavedRegisters[8];
    context.S9 = unwoundstate->captureCalleeSavedRegisters[9] = baseState->captureCalleeSavedRegisters[9];
    context.S10 = unwoundstate->captureCalleeSavedRegisters[10] = baseState->captureCalleeSavedRegisters[10];
    context.S11 = unwoundstate->captureCalleeSavedRegisters[11] = baseState->captureCalleeSavedRegisters[11];
    context.Gp = unwoundstate->captureCalleeSavedRegisters[12] = baseState->captureCalleeSavedRegisters[12];
    context.Tp = unwoundstate->captureCalleeSavedRegisters[13] = baseState->captureCalleeSavedRegisters[13];
    context.Ra = NULL; // Filled by the unwinder

    context.Sp = baseState->captureSp;
    context.Pc = baseState->captureIp;

#if !defined(DACCESS_COMPILE)
    // For DAC, if we get here, it means that the LazyMachState is uninitialized and we have to unwind it.
    // The API we use to unwind in DAC is StackWalk64(), which does not support the context pointers.
    //
    // Restore the integer registers to KNONVOLATILE_CONTEXT_POINTERS to be used for unwinding.
    nonVolContextPtrs.Fp = &unwoundstate->captureCalleeSavedRegisters[0];
    nonVolContextPtrs.S1 = &unwoundstate->captureCalleeSavedRegisters[1];
    nonVolContextPtrs.S2 = &unwoundstate->captureCalleeSavedRegisters[2];
    nonVolContextPtrs.S3 = &unwoundstate->captureCalleeSavedRegisters[3];
    nonVolContextPtrs.S4 = &unwoundstate->captureCalleeSavedRegisters[4];
    nonVolContextPtrs.S5 = &unwoundstate->captureCalleeSavedRegisters[5];
    nonVolContextPtrs.S6 = &unwoundstate->captureCalleeSavedRegisters[6];
    nonVolContextPtrs.S7 = &unwoundstate->captureCalleeSavedRegisters[7];
    nonVolContextPtrs.S8 = &unwoundstate->captureCalleeSavedRegisters[8];
    nonVolContextPtrs.S9 = &unwoundstate->captureCalleeSavedRegisters[9];
    nonVolContextPtrs.S10 = &unwoundstate->captureCalleeSavedRegisters[10];
    nonVolContextPtrs.S11 = &unwoundstate->captureCalleeSavedRegisters[11];
    nonVolContextPtrs.Gp = &unwoundstate->captureCalleeSavedRegisters[12];
    nonVolContextPtrs.Tp = &unwoundstate->captureCalleeSavedRegisters[13];
    nonVolContextPtrs.Ra = NULL; // Filled by the unwinder

#endif // DACCESS_COMPILE

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    LazyMachState::unwindLazyState(ip:%p,sp:%p)\n", baseState->captureIp, baseState->captureSp));

    PCODE pvControlPc;

    do {

#ifndef TARGET_UNIX
        pvControlPc = Thread::VirtualUnwindCallFrame(&context, &nonVolContextPtrs);
#else // !TARGET_UNIX
#ifdef DACCESS_COMPILE
        HRESULT hr = DacVirtualUnwind(threadId, &context, &nonVolContextPtrs);
        if (FAILED(hr))
        {
            DacError(hr);
        }
#else // DACCESS_COMPILE
        BOOL success = PAL_VirtualUnwind(&context, &nonVolContextPtrs);
        if (!success)
        {
            _ASSERTE(!"unwindLazyState: Unwinding failed");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
#endif // DACCESS_COMPILE
        pvControlPc = GetIP(&context);
#endif // !TARGET_UNIX

        if (funCallDepth > 0)
        {
            funCallDepth--;
            if (funCallDepth == 0)
                break;
        }
        else
        {
            // Determine  whether given IP resides in JITted code. (It returns nonzero in that case.)
            // Use it now to see if we've unwound to managed code yet.
            BOOL fFailedReaderLock = FALSE;
            BOOL fIsManagedCode = ExecutionManager::IsManagedCode(pvControlPc, hostCallPreference, &fFailedReaderLock);
            if (fFailedReaderLock)
            {
                // We don't know if we would have been able to find a JIT
                // manager, because we couldn't enter the reader lock without
                // yielding (and our caller doesn't want us to yield).  So abort
                // now.

                // Invalidate the lazyState we're returning, so the caller knows
                // we aborted before we could fully unwind
                unwoundstate->_isValid = false;
                return;
            }

            if (fIsManagedCode)
                break;

        }
    } while (true);

#ifdef TARGET_UNIX
    unwoundstate->captureCalleeSavedRegisters[0] = context.Fp;
    unwoundstate->captureCalleeSavedRegisters[1] = context.S1;
    unwoundstate->captureCalleeSavedRegisters[2] = context.S2;
    unwoundstate->captureCalleeSavedRegisters[3] = context.S3;
    unwoundstate->captureCalleeSavedRegisters[4] = context.S4;
    unwoundstate->captureCalleeSavedRegisters[5] = context.S5;
    unwoundstate->captureCalleeSavedRegisters[6] = context.S6;
    unwoundstate->captureCalleeSavedRegisters[7] = context.S7;
    unwoundstate->captureCalleeSavedRegisters[8] = context.S8;
    unwoundstate->captureCalleeSavedRegisters[9] = context.S9;
    unwoundstate->captureCalleeSavedRegisters[10] = context.S10;
    unwoundstate->captureCalleeSavedRegisters[11] = context.S11;
    unwoundstate->captureCalleeSavedRegisters[12] = context.Gp;
    unwoundstate->captureCalleeSavedRegisters[13] = context.Tp;
#endif

#ifdef DACCESS_COMPILE
    // For DAC builds, we update the registers directly since we dont have context pointers
    unwoundstate->captureCalleeSavedRegisters[0] = context.Fp;
    unwoundstate->captureCalleeSavedRegisters[1] = context.S1;
    unwoundstate->captureCalleeSavedRegisters[2] = context.S2;
    unwoundstate->captureCalleeSavedRegisters[3] = context.S3;
    unwoundstate->captureCalleeSavedRegisters[4] = context.S4;
    unwoundstate->captureCalleeSavedRegisters[5] = context.S5;
    unwoundstate->captureCalleeSavedRegisters[6] = context.S6;
    unwoundstate->captureCalleeSavedRegisters[7] = context.S7;
    unwoundstate->captureCalleeSavedRegisters[8] = context.S8;
    unwoundstate->captureCalleeSavedRegisters[9] = context.S9;
    unwoundstate->captureCalleeSavedRegisters[10] = context.S10;
    unwoundstate->captureCalleeSavedRegisters[11] = context.S11;
    unwoundstate->captureCalleeSavedRegisters[12] = context.Gp;
    unwoundstate->captureCalleeSavedRegisters[13] = context.Tp;
#else // !DACCESS_COMPILE
    // For non-DAC builds, update the register state from context pointers
    unwoundstate->ptrCalleeSavedRegisters[0] = nonVolContextPtrs.Fp;
    unwoundstate->ptrCalleeSavedRegisters[1] = nonVolContextPtrs.S1;
    unwoundstate->ptrCalleeSavedRegisters[2] = nonVolContextPtrs.S2;
    unwoundstate->ptrCalleeSavedRegisters[3] = nonVolContextPtrs.S3;
    unwoundstate->ptrCalleeSavedRegisters[4] = nonVolContextPtrs.S4;
    unwoundstate->ptrCalleeSavedRegisters[5] = nonVolContextPtrs.S5;
    unwoundstate->ptrCalleeSavedRegisters[6] = nonVolContextPtrs.S6;
    unwoundstate->ptrCalleeSavedRegisters[7] = nonVolContextPtrs.S7;
    unwoundstate->ptrCalleeSavedRegisters[8] = nonVolContextPtrs.S8;
    unwoundstate->ptrCalleeSavedRegisters[9] = nonVolContextPtrs.S9;
    unwoundstate->ptrCalleeSavedRegisters[10] = nonVolContextPtrs.S10;
    unwoundstate->ptrCalleeSavedRegisters[11] = nonVolContextPtrs.S11;
    unwoundstate->ptrCalleeSavedRegisters[12] = nonVolContextPtrs.Gp;
    unwoundstate->ptrCalleeSavedRegisters[13] = nonVolContextPtrs.Tp;
#endif // DACCESS_COMPILE

    unwoundstate->_pc = context.Pc;
    unwoundstate->_sp = context.Sp;

    unwoundstate->_isValid = TRUE;
}

void HelperMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD);
        _ASSERTE(pRD->pCurrentContext->Pc == GetReturnAddress());
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    //
    // Copy the saved state from the frame to the current context.
    //

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    HelperMethodFrame::UpdateRegDisplay cached ip:%p, sp:%p\n", m_MachState._pc, m_MachState._sp));

 #if defined(DACCESS_COMPILE)
    // For DAC, we may get here when the HMF is still uninitialized.
    // So we may need to unwind here.
    if (!m_MachState.isValid())
    {
        // This allocation throws on OOM.
        MachState* pUnwoundState = (MachState*)DacAllocHostOnlyInstance(sizeof(*pUnwoundState), true);

        InsureInit(false, pUnwoundState);

        pRD->pCurrentContext->Pc = pRD->ControlPC = pUnwoundState->_pc;
        pRD->pCurrentContext->Sp = pRD->SP        = pUnwoundState->_sp;
        pRD->pCurrentContext->Fp = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[0]);
        pRD->pCurrentContext->S1 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[1]);
        pRD->pCurrentContext->S2 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[2]);
        pRD->pCurrentContext->S3 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[3]);
        pRD->pCurrentContext->S4 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[4]);
        pRD->pCurrentContext->S5 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[5]);
        pRD->pCurrentContext->S6 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[6]);
        pRD->pCurrentContext->S7 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[7]);
        pRD->pCurrentContext->S8 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[8]);
        pRD->pCurrentContext->S9 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[9]);
        pRD->pCurrentContext->S10 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[10]);
        pRD->pCurrentContext->S11 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[11]);
        pRD->pCurrentContext->Gp = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[12]);
        pRD->pCurrentContext->Tp = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[13]);
        pRD->pCurrentContext->Ra = NULL; // Unwind again to get Caller's PC

        pRD->pCurrentContextPointers->Fp = pUnwoundState->ptrCalleeSavedRegisters[0];
        pRD->pCurrentContextPointers->S1 = pUnwoundState->ptrCalleeSavedRegisters[1];
        pRD->pCurrentContextPointers->S2 = pUnwoundState->ptrCalleeSavedRegisters[2];
        pRD->pCurrentContextPointers->S3 = pUnwoundState->ptrCalleeSavedRegisters[3];
        pRD->pCurrentContextPointers->S4 = pUnwoundState->ptrCalleeSavedRegisters[4];
        pRD->pCurrentContextPointers->S5 = pUnwoundState->ptrCalleeSavedRegisters[5];
        pRD->pCurrentContextPointers->S6 = pUnwoundState->ptrCalleeSavedRegisters[6];
        pRD->pCurrentContextPointers->S7 = pUnwoundState->ptrCalleeSavedRegisters[7];
        pRD->pCurrentContextPointers->S8 = pUnwoundState->ptrCalleeSavedRegisters[8];
        pRD->pCurrentContextPointers->S9 = pUnwoundState->ptrCalleeSavedRegisters[9];
        pRD->pCurrentContextPointers->S10 = pUnwoundState->ptrCalleeSavedRegisters[10];
        pRD->pCurrentContextPointers->S11 = pUnwoundState->ptrCalleeSavedRegisters[11];
        pRD->pCurrentContextPointers->Gp = pUnwoundState->ptrCalleeSavedRegisters[12];
        pRD->pCurrentContextPointers->Tp = pUnwoundState->ptrCalleeSavedRegisters[13];
        pRD->pCurrentContextPointers->Ra = NULL;
        return;
    }
#endif // DACCESS_COMPILE

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;
    pRD->ControlPC = GetReturnAddress(); // m_MachState._pc;
    pRD->SP = (DWORD64)(size_t)m_MachState._sp;

    pRD->pCurrentContext->Pc = pRD->ControlPC;
    pRD->pCurrentContext->Sp = pRD->SP;

#ifdef TARGET_UNIX
    pRD->pCurrentContext->Fp = m_MachState.ptrCalleeSavedRegisters[0] ? *m_MachState.ptrCalleeSavedRegisters[0] : m_MachState.captureCalleeSavedRegisters[0];
    pRD->pCurrentContext->S1 = m_MachState.ptrCalleeSavedRegisters[1] ? *m_MachState.ptrCalleeSavedRegisters[1] : m_MachState.captureCalleeSavedRegisters[1];
    pRD->pCurrentContext->S2 = m_MachState.ptrCalleeSavedRegisters[2] ? *m_MachState.ptrCalleeSavedRegisters[2] : m_MachState.captureCalleeSavedRegisters[2];
    pRD->pCurrentContext->S3 = m_MachState.ptrCalleeSavedRegisters[3] ? *m_MachState.ptrCalleeSavedRegisters[3] : m_MachState.captureCalleeSavedRegisters[3];
    pRD->pCurrentContext->S4 = m_MachState.ptrCalleeSavedRegisters[4] ? *m_MachState.ptrCalleeSavedRegisters[4] : m_MachState.captureCalleeSavedRegisters[4];
    pRD->pCurrentContext->S5 = m_MachState.ptrCalleeSavedRegisters[5] ? *m_MachState.ptrCalleeSavedRegisters[5] : m_MachState.captureCalleeSavedRegisters[5];
    pRD->pCurrentContext->S6 = m_MachState.ptrCalleeSavedRegisters[6] ? *m_MachState.ptrCalleeSavedRegisters[6] : m_MachState.captureCalleeSavedRegisters[6];
    pRD->pCurrentContext->S7 = m_MachState.ptrCalleeSavedRegisters[7] ? *m_MachState.ptrCalleeSavedRegisters[7] : m_MachState.captureCalleeSavedRegisters[7];
    pRD->pCurrentContext->S8 = m_MachState.ptrCalleeSavedRegisters[8] ? *m_MachState.ptrCalleeSavedRegisters[8] : m_MachState.captureCalleeSavedRegisters[8];
    pRD->pCurrentContext->S9 = m_MachState.ptrCalleeSavedRegisters[9] ? *m_MachState.ptrCalleeSavedRegisters[9] : m_MachState.captureCalleeSavedRegisters[9];
    pRD->pCurrentContext->S10 = m_MachState.ptrCalleeSavedRegisters[10] ? *m_MachState.ptrCalleeSavedRegisters[10] : m_MachState.captureCalleeSavedRegisters[10];
    pRD->pCurrentContext->S11 = m_MachState.ptrCalleeSavedRegisters[11] ? *m_MachState.ptrCalleeSavedRegisters[11] : m_MachState.captureCalleeSavedRegisters[11];
    pRD->pCurrentContext->Gp = m_MachState.ptrCalleeSavedRegisters[12] ? *m_MachState.ptrCalleeSavedRegisters[12] : m_MachState.captureCalleeSavedRegisters[12];
    pRD->pCurrentContext->Tp = m_MachState.ptrCalleeSavedRegisters[13] ? *m_MachState.ptrCalleeSavedRegisters[13] : m_MachState.captureCalleeSavedRegisters[13];
    pRD->pCurrentContext->Ra = NULL; // Unwind again to get Caller's PC
#else // TARGET_UNIX
    pRD->pCurrentContext->Fp = *m_MachState.ptrCalleeSavedRegisters[0];
    pRD->pCurrentContext->S1 = *m_MachState.ptrCalleeSavedRegisters[1];
    pRD->pCurrentContext->S2 = *m_MachState.ptrCalleeSavedRegisters[2];
    pRD->pCurrentContext->S3 = *m_MachState.ptrCalleeSavedRegisters[3];
    pRD->pCurrentContext->S4 = *m_MachState.ptrCalleeSavedRegisters[4];
    pRD->pCurrentContext->S5 = *m_MachState.ptrCalleeSavedRegisters[5];
    pRD->pCurrentContext->S6 = *m_MachState.ptrCalleeSavedRegisters[6];
    pRD->pCurrentContext->S7 = *m_MachState.ptrCalleeSavedRegisters[7];
    pRD->pCurrentContext->S8 = *m_MachState.ptrCalleeSavedRegisters[8];
    pRD->pCurrentContext->S9 = *m_MachState.ptrCalleeSavedRegisters[9];
    pRD->pCurrentContext->S10 = *m_MachState.ptrCalleeSavedRegisters[10];
    pRD->pCurrentContext->S11 = *m_MachState.ptrCalleeSavedRegisters[11];
    pRD->pCurrentContext->Gp = *m_MachState.ptrCalleeSavedRegisters[12];
    pRD->pCurrentContext->Tp = *m_MachState.ptrCalleeSavedRegisters[13];
    pRD->pCurrentContext->Ra = NULL; // Unwind again to get Caller's PC
#endif

#if !defined(DACCESS_COMPILE)
    pRD->pCurrentContextPointers->Fp = m_MachState.ptrCalleeSavedRegisters[0];
    pRD->pCurrentContextPointers->S1 = m_MachState.ptrCalleeSavedRegisters[1];
    pRD->pCurrentContextPointers->S2 = m_MachState.ptrCalleeSavedRegisters[2];
    pRD->pCurrentContextPointers->S3 = m_MachState.ptrCalleeSavedRegisters[3];
    pRD->pCurrentContextPointers->S4 = m_MachState.ptrCalleeSavedRegisters[4];
    pRD->pCurrentContextPointers->S5 = m_MachState.ptrCalleeSavedRegisters[5];
    pRD->pCurrentContextPointers->S6 = m_MachState.ptrCalleeSavedRegisters[6];
    pRD->pCurrentContextPointers->S7 = m_MachState.ptrCalleeSavedRegisters[7];
    pRD->pCurrentContextPointers->S8 = m_MachState.ptrCalleeSavedRegisters[8];
    pRD->pCurrentContextPointers->S9 = m_MachState.ptrCalleeSavedRegisters[9];
    pRD->pCurrentContextPointers->S10 = m_MachState.ptrCalleeSavedRegisters[10];
    pRD->pCurrentContextPointers->S11 = m_MachState.ptrCalleeSavedRegisters[11];
    pRD->pCurrentContextPointers->Gp = m_MachState.ptrCalleeSavedRegisters[12];
    pRD->pCurrentContextPointers->Tp = m_MachState.ptrCalleeSavedRegisters[13];
    pRD->pCurrentContextPointers->Ra = NULL; // Unwind again to get Caller's PC
#endif
    ClearRegDisplayArgumentAndScratchRegisters(pRD);
}

#ifndef DACCESS_COMPILE
void ThisPtrRetBufPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    //Initially
    //a0 -This ptr
    //a1 -ReturnBuffer
    m_rgCode[0] = 0x00050f93; // addi  t6, a0, 0x0
    m_rgCode[1] = 0x00058513; // addi  a0, a1, 0x0
    m_rgCode[2] = 0x000f8593; // addi  a1, t6, 0x0
    m_rgCode[3] = 0x00000f97; // auipc t6, 0
    m_rgCode[4] = 0x00cfbf83; // ld    t6, 12(t6)
    m_rgCode[5] = 0x000f8067; // jalr  x0, 0(t6)

    _ASSERTE((UINT32*)&m_pTarget == &m_rgCode[6]);
    _ASSERTE(6 == ARRAY_SIZE(m_rgCode));

    m_pTarget = GetPreStubEntryPoint();
    m_pMethodDesc = (TADDR)pMD;
}

#endif // !DACCESS_COMPILE

void UpdateRegDisplayFromCalleeSavedRegisters(REGDISPLAY * pRD, CalleeSavedRegisters * pCalleeSaved)
{
    LIMITED_METHOD_CONTRACT;
    pRD->pCurrentContext->S1 = pCalleeSaved->s1;
    pRD->pCurrentContext->S2 = pCalleeSaved->s2;
    pRD->pCurrentContext->S3 = pCalleeSaved->s3;
    pRD->pCurrentContext->S4 = pCalleeSaved->s4;
    pRD->pCurrentContext->S5 = pCalleeSaved->s5;
    pRD->pCurrentContext->S6 = pCalleeSaved->s6;
    pRD->pCurrentContext->S7 = pCalleeSaved->s7;
    pRD->pCurrentContext->S8 = pCalleeSaved->s8;
    pRD->pCurrentContext->S9 = pCalleeSaved->s9;
    pRD->pCurrentContext->S10 = pCalleeSaved->s10;
    pRD->pCurrentContext->S11 = pCalleeSaved->s11;
    pRD->pCurrentContext->Gp = pCalleeSaved->gp;
    pRD->pCurrentContext->Tp = pCalleeSaved->tp;
    pRD->pCurrentContext->Fp  = pCalleeSaved->fp;
    pRD->pCurrentContext->Ra  = pCalleeSaved->ra;

    T_KNONVOLATILE_CONTEXT_POINTERS * pContextPointers = pRD->pCurrentContextPointers;
    pContextPointers->S1 = (PDWORD64)&pCalleeSaved->s1;
    pContextPointers->S2 = (PDWORD64)&pCalleeSaved->s2;
    pContextPointers->S3 = (PDWORD64)&pCalleeSaved->s3;
    pContextPointers->S4 = (PDWORD64)&pCalleeSaved->s4;
    pContextPointers->S5 = (PDWORD64)&pCalleeSaved->s5;
    pContextPointers->S6 = (PDWORD64)&pCalleeSaved->s6;
    pContextPointers->S7 = (PDWORD64)&pCalleeSaved->s7;
    pContextPointers->S8 = (PDWORD64)&pCalleeSaved->s8;
    pContextPointers->S9 = (PDWORD64)&pCalleeSaved->s9;
    pContextPointers->S10 = (PDWORD64)&pCalleeSaved->s10;
    pContextPointers->S11 = (PDWORD64)&pCalleeSaved->s11;
    pContextPointers->Gp = (PDWORD64)&pCalleeSaved->gp;
    pContextPointers->Tp = (PDWORD64)&pCalleeSaved->tp;
    pContextPointers->Fp = (PDWORD64)&pCalleeSaved->fp;
    pContextPointers->Ra  = (PDWORD64)&pCalleeSaved->ra;
}

void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD);
        _ASSERTE(pRD->pCurrentContext->Pc == GetReturnAddress());
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    // copy the callee saved regs
    CalleeSavedRegisters *pCalleeSaved = GetCalleeSavedRegisters();
    UpdateRegDisplayFromCalleeSavedRegisters(pRD, pCalleeSaved);

    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    // copy the control registers
    //pRD->pCurrentContext->Fp = pCalleeSaved->fp;//not needed for duplicated.
    //pRD->pCurrentContext->Ra = pCalleeSaved->ra;//not needed for duplicated.
    pRD->pCurrentContext->Pc = GetReturnAddress();
    pRD->pCurrentContext->Sp = this->GetSP();

    // Finally, syncup the regdisplay with the context
    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay(pc:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));
}

void FaultingExceptionFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    LIMITED_METHOD_DAC_CONTRACT;

    // Copy the context to regdisplay
    memcpy(pRD->pCurrentContext, &m_ctx, sizeof(T_CONTEXT));

    pRD->ControlPC = ::GetIP(&m_ctx);
    pRD->SP = ::GetSP(&m_ctx);

    // Update the integer registers in KNONVOLATILE_CONTEXT_POINTERS from
    // the exception context we have.
    pRD->pCurrentContextPointers->S1 = (PDWORD64)&m_ctx.S1;
    pRD->pCurrentContextPointers->S2 = (PDWORD64)&m_ctx.S2;
    pRD->pCurrentContextPointers->S3 = (PDWORD64)&m_ctx.S3;
    pRD->pCurrentContextPointers->S4 = (PDWORD64)&m_ctx.S4;
    pRD->pCurrentContextPointers->S5 = (PDWORD64)&m_ctx.S5;
    pRD->pCurrentContextPointers->S6 = (PDWORD64)&m_ctx.S6;
    pRD->pCurrentContextPointers->S7 = (PDWORD64)&m_ctx.S7;
    pRD->pCurrentContextPointers->S8 = (PDWORD64)&m_ctx.S8;
    pRD->pCurrentContextPointers->S9 = (PDWORD64)&m_ctx.S9;
    pRD->pCurrentContextPointers->S10 = (PDWORD64)&m_ctx.S10;
    pRD->pCurrentContextPointers->S11 = (PDWORD64)&m_ctx.S11;
    pRD->pCurrentContextPointers->Fp = (PDWORD64)&m_ctx.Fp;
    pRD->pCurrentContextPointers->Gp = (PDWORD64)&m_ctx.Gp;
    pRD->pCurrentContextPointers->Tp = (PDWORD64)&m_ctx.Tp;
    pRD->pCurrentContextPointers->Ra = (PDWORD64)&m_ctx.Ra;

    ClearRegDisplayArgumentAndScratchRegisters(pRD);

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    FaultingExceptionFrame::UpdateRegDisplay(pc:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));
}

void InlinedCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
#ifdef PROFILING_SUPPORTED
        PRECONDITION(CORProfilerStackSnapshotEnabled() || InlinedCallFrame::FrameHasActiveCall(this));
#endif
        HOST_NOCALLS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (!InlinedCallFrame::FrameHasActiveCall(this))
    {
        LOG((LF_CORDB, LL_ERROR, "WARNING: InlinedCallFrame::UpdateRegDisplay called on inactive frame %p\n", this));
        return;
    }

#ifndef DACCESS_COMPILE
    if (updateFloats)
    {
        UpdateFloatingPointRegisters(pRD);
    }
#endif // DACCESS_COMPILE

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;

    pRD->pCurrentContext->Pc = *(DWORD64 *)&m_pCallerReturnAddress;
    pRD->pCurrentContext->Sp = *(DWORD64 *)&m_pCallSiteSP;
    pRD->pCurrentContext->Fp = *(DWORD64 *)&m_pCalleeSavedFP;

    pRD->pCurrentContextPointers->S1 = NULL;
    pRD->pCurrentContextPointers->S2 = NULL;
    pRD->pCurrentContextPointers->S3 = NULL;
    pRD->pCurrentContextPointers->S4 = NULL;
    pRD->pCurrentContextPointers->S5 = NULL;
    pRD->pCurrentContextPointers->S6 = NULL;
    pRD->pCurrentContextPointers->S7 = NULL;
    pRD->pCurrentContextPointers->S8 = NULL;
    pRD->pCurrentContextPointers->S9 = NULL;
    pRD->pCurrentContextPointers->S10 = NULL;
    pRD->pCurrentContextPointers->S11 = NULL;
    pRD->pCurrentContextPointers->Gp = NULL;
    pRD->pCurrentContextPointers->Tp = NULL;

    pRD->ControlPC = m_pCallerReturnAddress;
    pRD->SP = (DWORD64) dac_cast<TADDR>(m_pCallSiteSP);

    // reset pContext; it's only valid for active (top-most) frame
    pRD->pContext = NULL;

    ClearRegDisplayArgumentAndScratchRegisters(pRD);


    // Update the frame pointer in the current context.
    pRD->pCurrentContextPointers->Fp = &m_pCalleeSavedFP;

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    InlinedCallFrame::UpdateRegDisplay(pc:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

#ifdef FEATURE_HIJACK
TADDR ResumableFrame::GetReturnAddressPtr(void)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return dac_cast<TADDR>(m_Regs) + offsetof(T_CONTEXT, Pc);
}

void ResumableFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    CopyMemory(pRD->pCurrentContext, m_Regs, sizeof(T_CONTEXT));

    pRD->ControlPC = m_Regs->Pc;
    pRD->SP = m_Regs->Sp;

    pRD->pCurrentContextPointers->S1 = &m_Regs->S1;
    pRD->pCurrentContextPointers->S2 = &m_Regs->S2;
    pRD->pCurrentContextPointers->S3 = &m_Regs->S3;
    pRD->pCurrentContextPointers->S4 = &m_Regs->S4;
    pRD->pCurrentContextPointers->S5 = &m_Regs->S5;
    pRD->pCurrentContextPointers->S6 = &m_Regs->S6;
    pRD->pCurrentContextPointers->S7 = &m_Regs->S7;
    pRD->pCurrentContextPointers->S8 = &m_Regs->S8;
    pRD->pCurrentContextPointers->S9 = &m_Regs->S9;
    pRD->pCurrentContextPointers->S10 = &m_Regs->S10;
    pRD->pCurrentContextPointers->S11 = &m_Regs->S11;
    pRD->pCurrentContextPointers->Tp = &m_Regs->Tp;
    pRD->pCurrentContextPointers->Gp = &m_Regs->Gp;
    pRD->pCurrentContextPointers->Fp = &m_Regs->Fp;
    pRD->pCurrentContextPointers->Ra = &m_Regs->Ra;

    pRD->volatileCurrContextPointers.R0 = &m_Regs->R0;
    pRD->volatileCurrContextPointers.A0 = &m_Regs->A0;
    pRD->volatileCurrContextPointers.A1 = &m_Regs->A1;
    pRD->volatileCurrContextPointers.A2 = &m_Regs->A2;
    pRD->volatileCurrContextPointers.A3 = &m_Regs->A3;
    pRD->volatileCurrContextPointers.A4 = &m_Regs->A4;
    pRD->volatileCurrContextPointers.A5 = &m_Regs->A5;
    pRD->volatileCurrContextPointers.A6 = &m_Regs->A6;
    pRD->volatileCurrContextPointers.A7 = &m_Regs->A7;
    pRD->volatileCurrContextPointers.T0 = &m_Regs->T0;
    pRD->volatileCurrContextPointers.T1 = &m_Regs->T1;
    pRD->volatileCurrContextPointers.T2 = &m_Regs->T2;
    pRD->volatileCurrContextPointers.T3 = &m_Regs->T3;
    pRD->volatileCurrContextPointers.T4 = &m_Regs->T4;
    pRD->volatileCurrContextPointers.T5 = &m_Regs->T5;
    pRD->volatileCurrContextPointers.T6 = &m_Regs->T6;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    ResumableFrame::UpdateRegDisplay(pc:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));

    RETURN;
}

void HijackFrame::UpdateRegDisplay(const PREGDISPLAY pRD, bool updateFloats)
{
    LIMITED_METHOD_CONTRACT;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;

    pRD->pCurrentContext->Pc = m_ReturnAddress;
    size_t s = sizeof(struct HijackArgs);
    _ASSERTE(s%8 == 0); // HijackArgs contains register values and hence will be a multiple of 8
    // stack must be multiple of 16. So if s is not multiple of 16 then there must be padding of 8 bytes
    s = s + s%16;
    pRD->pCurrentContext->Sp = PTR_TO_TADDR(m_Args) + s ;

    pRD->pCurrentContext->S1 = m_Args->S1;
    pRD->pCurrentContext->S2 = m_Args->S2;
    pRD->pCurrentContext->S3 = m_Args->S3;
    pRD->pCurrentContext->S4 = m_Args->S4;
    pRD->pCurrentContext->S5 = m_Args->S5;
    pRD->pCurrentContext->S6 = m_Args->S6;
    pRD->pCurrentContext->S7 = m_Args->S7;
    pRD->pCurrentContext->S8 = m_Args->S8;
    pRD->pCurrentContext->S9 = m_Args->S9;
    pRD->pCurrentContext->S10 = m_Args->S10;
    pRD->pCurrentContext->S11 = m_Args->S11;
    pRD->pCurrentContext->Gp = m_Args->Gp;
    pRD->pCurrentContext->Tp = m_Args->Tp;
    pRD->pCurrentContext->Fp = m_Args->Fp;
    pRD->pCurrentContext->Ra = m_Args->Ra;

    pRD->pCurrentContextPointers->S1 = &m_Args->S1;
    pRD->pCurrentContextPointers->S2 = &m_Args->S2;
    pRD->pCurrentContextPointers->S3 = &m_Args->S3;
    pRD->pCurrentContextPointers->S4 = &m_Args->S4;
    pRD->pCurrentContextPointers->S5 = &m_Args->S5;
    pRD->pCurrentContextPointers->S6 = &m_Args->S6;
    pRD->pCurrentContextPointers->S7 = &m_Args->S7;
    pRD->pCurrentContextPointers->S8 = &m_Args->S8;
    pRD->pCurrentContextPointers->S9 = &m_Args->S9;
    pRD->pCurrentContextPointers->S10 = &m_Args->S10;
    pRD->pCurrentContextPointers->S11 = &m_Args->S11;
    pRD->pCurrentContextPointers->Gp = &m_Args->Gp;
    pRD->pCurrentContextPointers->Tp = &m_Args->Tp;
    pRD->pCurrentContextPointers->Fp = &m_Args->Fp;
    pRD->pCurrentContextPointers->Ra = NULL;
    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    HijackFrame::UpdateRegDisplay(pc:%p, sp:%p)\n", pRD->ControlPC, pRD->SP));
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
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = GetThreadNULLOk();

    // We may not have a managed thread object. Example is an AV on the helper thread.
    // (perhaps during StubManager::IsStub)
    if (pThread == NULL)
    {
        return FALSE;
    }

    PCODE f_IP = GetIP(pContext);

    StubCodeBlockKind sk = RangeSectionStubManager::GetStubKind(f_IP);

    if (sk == STUB_CODE_BLOCK_VSD_DISPATCH_STUB)
    {
        if (*PTR_DWORD(f_IP - 4) != DISPATCH_STUB_FIRST_DWORD)
        {
            _ASSERTE(!"AV in DispatchStub at unknown instruction");
            return FALSE;
        }
    }
    else
    if (sk == STUB_CODE_BLOCK_VSD_RESOLVE_STUB)
    {
        if (*PTR_DWORD(f_IP) != RESOLVE_STUB_FIRST_DWORD)
        {
            _ASSERTE(!"AV in ResolveStub at unknown instruction");
            return FALSE;
        }
    }
    else
    {
        return FALSE;
    }

    PCODE callsite = GetAdjustedCallAddress(GetRA(pContext));

    // Lr must already have been saved before calling so it should not be necessary to restore Lr

    if (pExceptionRecord != NULL)
    {
        pExceptionRecord->ExceptionAddress = (PVOID)callsite;
    }
    SetIP(pContext, callsite);

    return TRUE;
}
#endif // !DACCESS_COMPILE

UMEntryThunk * UMEntryThunk::Decode(void *pCallback)
{
    _ASSERTE(offsetof(UMEntryThunkCode, m_code) == 0);
    UMEntryThunkCode * pCode = (UMEntryThunkCode*)pCallback;

    // We may be called with an unmanaged external code pointer instead. So if it doesn't look like one of our
    // stubs (see UMEntryThunkCode::Encode below) then we'll return NULL. Luckily in these scenarios our
    // caller will perform a hash lookup on successful return to verify our result in case random unmanaged
    // code happens to look like ours.
    if ((pCode->m_code[0] == 0x00000f97) && // auipc t6, 0
        (pCode->m_code[1] == 0x018fb383) && // ld    t2, 24(t6)
        (pCode->m_code[2] == 0x010fbf83) && // ld    t6, 16(t6)
        (pCode->m_code[3] == 0x000f8067))   // jalr  x0, 0(t6)
    {
        return (UMEntryThunk*)pCode->m_pvSecretParam;
    }

    return NULL;
}

void UMEntryThunkCode::Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam)
{
    // auipc t6, 0
    // ld    t2, 24(t6)
    // ld    t6, 16(t6)
    // jalr  x0, 0(t6)
    // m_pTargetCode data
    // m_pvSecretParam data

    m_code[0] = 0x00000f97; // auipc t6, 0
    m_code[1] = 0x018fb383; // ld    t2, 24(t6)
    m_code[2] = 0x010fbf83; // ld    t6, 16(t6)
    m_code[3] = 0x000f8067; // jalr  x0, 0(t6)

    m_pTargetCode = (TADDR)pTargetCode;
    m_pvSecretParam = (TADDR)pvSecretParam;
    FlushInstructionCache(GetCurrentProcess(),&pEntryThunkCodeRX->m_code,sizeof(m_code));
}

#ifndef DACCESS_COMPILE

void UMEntryThunkCode::Poison()
{
    ExecutableWriterHolder<UMEntryThunkCode> thunkWriterHolder(this, sizeof(UMEntryThunkCode));
    UMEntryThunkCode *pThisRW = thunkWriterHolder.GetRW();

    pThisRW->m_pTargetCode = (TADDR)UMEntryThunk::ReportViolation;

    // ld   a0, 24(t6)
    pThisRW->m_code[1] = 0x018fb503;

    ClrFlushInstructionCache(&m_code,sizeof(m_code));
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
    // this wouldn't be called in riscv64, just to comply with gchelpers.h
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

void StubLinkerCPU::EmitMovConstant(IntReg reg, UINT64 imm)
{
    // Adaptation of emitLoadImmediate

    if (isValidSimm12(imm))
    {
        EmitAddImm(reg, 0 /* zero register */, imm & 0xFFF);
        return;
    }

    // TODO-RISCV64: maybe optimized via emitDataConst(), check #86790

    UINT32 msb;
    UINT32 high31;

    BitScanReverse64(&msb, imm);

    if (msb > 30)
    {
        high31 = (imm >> (msb - 30)) & 0x7FffFFff;
    }
    else
    {
        high31 = imm & 0x7FffFFff;
    }

    // Since ADDIW use sign extension for immediate
    // we have to adjust higher 19 bit loaded by LUI
    // for case when low part is bigger than 0x800.
    UINT32 high19 = (high31 + 0x800) >> 12;

    EmitLuImm(reg, high19);
    if (high31 & 0x800)
    {
        // EmitAddImm does not allow negative immediate values, so use EmitSubImm.
        EmitSubImm(reg, reg, ~high31 + 1 & 0xFFF);
    }
    else
    {
        EmitAddImm(reg, reg, high31 & 0x7FF);
    }

    // And load remaining part by batches of 11 bits size.
    INT32 remainingShift = msb - 30;

    // shiftAccumulator usage is an optimization allows to exclude `slli addi` iteration
    // if immediate bits `low11` for this iteration are zero.
    UINT32 shiftAccumulator = 0;

    while (remainingShift > 0)
    {
        UINT32 shift = remainingShift >= 11 ? 11 : remainingShift % 11;
        UINT32 mask = 0x7ff >> (11 - shift);
        remainingShift -= shift;
        UINT32 low11 = (imm >> remainingShift) & mask;
        shiftAccumulator += shift;

        if (low11)
        {
            EmitSllImm(reg, reg, shiftAccumulator);
            shiftAccumulator = 0;

            EmitAddImm(reg, reg, low11);
        }
    }

    if (shiftAccumulator)
    {
        EmitSllImm(reg, reg, shiftAccumulator);
    }
}

void StubLinkerCPU::EmitJumpRegister(IntReg regTarget)
{
    Emit32(0x00000067 | (regTarget << 15));
}

void StubLinkerCPU::EmitProlog(unsigned short cIntRegArgs, unsigned short cFpRegArgs, unsigned short cbStackSpace)
{
    _ASSERTE(!m_fProlog);

    unsigned short numberOfEntriesOnStack  = 2 + cIntRegArgs + cFpRegArgs; // 2 for fp, ra

    // Stack needs to be 16 byte aligned. Compute the required padding before saving it
    unsigned short totalPaddedFrameSize = static_cast<unsigned short>(ALIGN_UP(cbStackSpace + numberOfEntriesOnStack * sizeof(void*), 2 * sizeof(void*)));
    // The padding is going to be applied to the local stack
    cbStackSpace =  totalPaddedFrameSize - numberOfEntriesOnStack * sizeof(void*);

    // Record the parameters of this prolog so that we can generate a matching epilog and unwind info.
    DescribeProlog(cIntRegArgs, cFpRegArgs, cbStackSpace);


    // N.B Despite the range of a jump with a sub sp is 4KB, we're limiting to 504 to save from emitting right prolog that's
    // expressable in unwind codes efficiently. The largest offset in typical unwindinfo encodings that we use is 504.
    // so allocations larger than 504 bytes would require setting the SP in multiple strides, which would complicate both
    // prolog and epilog generation as well as unwindinfo generation.
    _ASSERTE((totalPaddedFrameSize <= 504) && "NYI:RISCV64 Implement StubLinker prologs with larger than 504 bytes of frame size");
    if (totalPaddedFrameSize > 504)
        COMPlusThrow(kNotSupportedException);

    // Here is how the stack would look like (Stack grows up)
    // [Low Address]
    //            +------------+
    //      SP -> |            | <-+
    //            :            :   | Stack Frame, (i.e outgoing arguments) including padding
    //            |            | <-+
    //            +------------+
    //            | FP         |
    //            +------------+
    //            | RA         |
    //            +------------+
    //            | F10        | <-+
    //            +------------+   |
    //            :            :   | Fp Args
    //            +------------+   |
    //            | F17        | <-+
    //            +------------+
    //            | X10        | <-+
    //            +------------+   |
    //            :            :   | Int Args
    //            +------------+   |
    //            | X17        | <-+
    //            +------------+
    //  Old SP -> |[Stack Args]|
    // [High Address]

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

    // 2. Store FP/RA
    EmitStore(RegFp, RegSp, cbStackSpace);
    EmitStore(RegRa, RegSp, cbStackSpace + sizeof(void*));

    // 3. Set the frame pointer
    EmitMovReg(RegFp, RegSp);

    // 4. Store floating point argument registers
    _ASSERTE(cFpRegArgs <= 8);
    for (unsigned short i = 0; i < cFpRegArgs; i++)
        EmitStore(FloatReg(i + 10), RegSp, cbOffset + i * sizeof(void*));

    // 5. Store int argument registers
    cbOffset += cFpRegArgs * sizeof(void*);
    _ASSERTE(cIntRegArgs <= 8);
    for (unsigned short i = 0 ; i < cIntRegArgs; i++)
        EmitStore(IntReg(i + 10), RegSp, cbOffset + i * sizeof(void*));
}

void StubLinkerCPU::EmitEpilog()
{
    _ASSERTE(m_fProlog);

    // 5. Restore int argument registers
    //    nop: We don't need to. They are scratch registers

    // 4. Restore floating point argument registers
    //    nop: We don't need to. They are scratch registers

    // 3. Restore the SP from FP
    //    N.B. We're assuming that the stublinker stubs doesn't do alloca, hence nop

    // 2. Restore FP/RA
    EmitLoad(RegFp, RegSp, m_cbStackSpace);
    EmitLoad(RegRa, RegSp, m_cbStackSpace + sizeof(void*));

    // 1. Restore SP
    EmitAddImm(RegSp, RegSp, GetStackFrameSize());

    // jalr x0, 0(ra)
    EmitJumpRegister(RegRa);
}

// Instruction types as per RISC-V Spec, Chapter 24 RV32/64G Instruction Set Listings
static unsigned ITypeInstr(unsigned opcode, unsigned funct3, unsigned rd, unsigned rs1, int imm12)
{
    _ASSERTE(!(opcode >> 7));
    _ASSERTE(!(funct3 >> 3));
    _ASSERTE(!(rd >> 5));
    _ASSERTE(!(rs1 >> 5));
    _ASSERTE(StubLinkerCPU::isValidSimm12(imm12));
    return opcode | (rd << 7) | (funct3 << 12) | (rs1 << 15) | (imm12 << 20);
}

static unsigned STypeInstr(unsigned opcode, unsigned funct3, unsigned rs1, unsigned rs2, int imm12)
{
    _ASSERTE(!(opcode >> 7));
    _ASSERTE(!(funct3 >> 3));
    _ASSERTE(!(rs1 >> 5));
    _ASSERTE(!(rs2 >> 5));
    _ASSERTE(StubLinkerCPU::isValidSimm12(imm12));
    int immLo5 = imm12 & 0x1f;
    int immHi7 = (imm12 >> 5) & 0x7f;
    return opcode | (immLo5 << 7) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) | (immHi7 << 25);
}

static unsigned RTypeInstr(unsigned opcode, unsigned funct3, unsigned funct7, unsigned rd, unsigned rs1, unsigned rs2)
{
    _ASSERTE(!(opcode >> 7));
    _ASSERTE(!(funct3 >> 3));
    _ASSERTE(!(funct7 >> 7));
    _ASSERTE(!(rd >> 5));
    _ASSERTE(!(rs1 >> 5));
    _ASSERTE(!(rs2 >> 5));
    return opcode | (rd << 7) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) | (funct7 << 25);
}

static unsigned UTypeInstr(unsigned opcode, unsigned rd, int imm20)
{
    _ASSERTE(!(opcode >> 7));
    _ASSERTE(!(rd >> 5));
    _ASSERTE(StubLinkerCPU::isValidUimm20(imm20));
    return opcode | (rd << 7) | (imm20 << 12);
}

static unsigned BTypeInstr(unsigned opcode, unsigned funct3, unsigned rs1, unsigned rs2, int imm13)
{
    _ASSERTE(!(opcode >> 7));
    _ASSERTE(!(funct3 >> 3));
    _ASSERTE(!(rs1 >> 5));
    _ASSERTE(!(rs2 >> 5));
    _ASSERTE(StubLinkerCPU::isValidSimm13(imm13));
    _ASSERTE(IS_ALIGNED(imm13, 2));
    int immLo1 = (imm13 >> 11) & 0x1;
    int immLo4 = (imm13 >> 1) & 0xf;
    int immHi6 = (imm13 >> 5) & 0x3f;
    int immHi1 = (imm13 >> 12) & 0x1;
    return opcode | (immLo4 << 8) | (funct3 << 12) | (rs1 << 15) | (rs2 << 20) | (immHi6 << 25) | (immLo1 << 7) | (immHi1 << 31);
}

void StubLinkerCPU::EmitLoad(IntReg dest, IntReg srcAddr, int offset)
{
    Emit32(ITypeInstr(0x3, 0x3, dest, srcAddr, offset));  // ld
}
void StubLinkerCPU::EmitLoad(FloatReg dest, IntReg srcAddr, int offset)
{
    Emit32(ITypeInstr(0x7, 0x3, dest, srcAddr, offset));  // fld
}

void StubLinkerCPU:: EmitStore(IntReg src, IntReg destAddr, int offset)
{
    Emit32(STypeInstr(0x23, 0x3, destAddr, src, offset));  // sd
}
void StubLinkerCPU::EmitStore(FloatReg src, IntReg destAddr, int offset)
{
    Emit32(STypeInstr(0x27, 0x3, destAddr, src, offset));  // fsd
}

void StubLinkerCPU::EmitMovReg(IntReg Xd, IntReg Xm)
{
    EmitAddImm(Xd, Xm, 0);
}
void StubLinkerCPU::EmitMovReg(FloatReg dest, FloatReg source)
{
    Emit32(RTypeInstr(0x53, 0, 0x11, dest, source, source));  // fsgnj.d
}

void StubLinkerCPU::EmitSubImm(IntReg Xd, IntReg Xn, unsigned int value)
{
    _ASSERTE(value <= 0x800);
    EmitAddImm(Xd, Xn, ~value + 0x1);
}
void StubLinkerCPU::EmitAddImm(IntReg Xd, IntReg Xn, unsigned int value)
{
    Emit32(ITypeInstr(0x13, 0, Xd, Xn, value));  // addi
}
void StubLinkerCPU::EmitSllImm(IntReg Xd, IntReg Xn, unsigned int value)
{
    _ASSERTE(!(value >> 6));
    Emit32(ITypeInstr(0x13, 0x1, Xd, Xn, value));  // slli
}
void StubLinkerCPU::EmitLuImm(IntReg Xd, unsigned int value)
{
    _ASSERTE(value <= 0xFFFFF);
    Emit32((DWORD)(0x00000037 | (value << 12) | (Xd << 7))); // lui Xd, value
}

void StubLinkerCPU::Init()
{
    new (gBranchIF) BranchInstructionFormat();
}

// Emits code to adjust arguments for static delegate target.
VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    static const int argRegBase = 10;  // first argument register: a0, fa0
    static const IntReg t6 = 31, t5 = 30, a0 = argRegBase + 0;
    // On entry a0 holds the delegate instance. Look up the real target address stored in the MethodPtrAux
    // field and saved in t6. Tailcall to the target method after re-arranging the arguments
    EmitLoad(t6, a0, DelegateObject::GetOffsetOfMethodPtrAux());
    // load the indirection cell into t5 used by ResolveWorkerAsmStub
    EmitAddImm(t5, a0, DelegateObject::GetOffsetOfMethodPtrAux());

    int delay_index[8] = {-1};
    bool is_store = false;
    UINT16 index = 0;
    int i = 0;
    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++, i++)
    {
        if (pEntry->srcofs & ShuffleEntry::REGMASK)
        {
            // Source in register, destination in register

            // Both the srcofs and dstofs must be of the same kind of registers - float or general purpose.
            // If source is present in register then destination may be a stack-slot.
            _ASSERTE(((pEntry->dstofs & ShuffleEntry::FPREGMASK) == (pEntry->srcofs & ShuffleEntry::FPREGMASK)) || !(pEntry->dstofs & (ShuffleEntry::FPREGMASK | ShuffleEntry::REGMASK)));
            _ASSERTE((pEntry->dstofs & ShuffleEntry::OFSREGMASK) <= 8);//should amend for offset!
            _ASSERTE((pEntry->srcofs & ShuffleEntry::OFSREGMASK) <= 8);

            if (pEntry->srcofs & ShuffleEntry::FPREGMASK)
            {
                int j = 1;
                while (pEntry[j].srcofs & ShuffleEntry::FPREGMASK)
                {
                    j++;
                }
                assert((pEntry->dstofs - pEntry->srcofs) == index);
                assert(8 > index);

                int tmp_reg = 0; // ft0.
                ShuffleEntry* tmp_entry = pShuffleEntryArray + delay_index[0];
                while (index)
                {
                    EmitLoad(FloatReg(tmp_reg), RegSp, tmp_entry->srcofs * sizeof(void*));
                    tmp_reg++;
                    index--;
                    tmp_entry++;
                }

                j -= 1;
                tmp_entry = pEntry + j;
                i += j;
                while (pEntry[j].srcofs & ShuffleEntry::FPREGMASK)
                {
                    FloatReg src = (pEntry[j].srcofs & ShuffleEntry::OFSREGMASK) + argRegBase;
                    if (pEntry[j].dstofs & ShuffleEntry::FPREGMASK) {
                        FloatReg dst = (pEntry[j].dstofs & ShuffleEntry::OFSREGMASK) + argRegBase;
                        EmitMovReg(dst, src);
                    }
                    else
                    {
                        EmitStore(src, RegSp, pEntry[j].dstofs * sizeof(void*));
                    }
                    j--;
                }

                assert(tmp_reg <= 7);
                while (tmp_reg > 0)
                {
                    tmp_reg--;
                    EmitMovReg(FloatReg(index + argRegBase), FloatReg(tmp_reg));
                    index++;
                }
                index = 0;
                pEntry = tmp_entry;
            }
            else
            {
                assert(pEntry->dstofs & ShuffleEntry::REGMASK);
                IntReg dst = (pEntry->dstofs & ShuffleEntry::OFSREGMASK) + argRegBase;
                IntReg src = (pEntry->srcofs & ShuffleEntry::OFSREGMASK) + argRegBase;
                assert(dst < src);
                EmitMovReg(dst, src);
            }
        }
        else if (pEntry->dstofs & ShuffleEntry::REGMASK)
        {
            // source must be on the stack
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));

            int dstReg = (pEntry->dstofs & ShuffleEntry::OFSREGMASK) + argRegBase;
            int srcOfs = (pEntry->srcofs & ShuffleEntry::OFSMASK) * sizeof(void*);
            if (pEntry->dstofs & ShuffleEntry::FPREGMASK)
            {
                if (!is_store)
                {
                    delay_index[index++] = i;
                    continue;
                }
                EmitLoad(FloatReg(dstReg), RegSp, srcOfs);
            }
            else
            {
                EmitLoad(IntReg(dstReg), RegSp, srcOfs);
            }
        }
        else
        {
            // source & dest must be on the stack
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));
            _ASSERTE(!(pEntry->dstofs & ShuffleEntry::REGMASK));

            IntReg t4 = 29;
            EmitLoad(t4, RegSp, pEntry->srcofs * sizeof(void*));
            EmitStore(t4, RegSp, pEntry->dstofs * sizeof(void*));
        }
    }
    // Tailcall to target
    // jalr x0, 0(t6)
    EmitJumpRegister(t6);
}

// Emits code to adjust arguments for static delegate target.
VOID StubLinkerCPU::EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg)
{
    STANDARD_VM_CONTRACT;

    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++)
    {
        _ASSERTE(pEntry->dstofs & ShuffleEntry::REGMASK);
        _ASSERTE(pEntry->srcofs & ShuffleEntry::REGMASK);
        _ASSERTE(!(pEntry->dstofs & ShuffleEntry::FPREGMASK));
        _ASSERTE(!(pEntry->srcofs & ShuffleEntry::FPREGMASK));
        _ASSERTE(pEntry->dstofs != ShuffleEntry::HELPERREG);
        _ASSERTE(pEntry->srcofs != ShuffleEntry::HELPERREG);

        EmitMovReg(IntReg((pEntry->dstofs & ShuffleEntry::OFSREGMASK) + 10), IntReg((pEntry->srcofs & ShuffleEntry::OFSREGMASK) + 10));
    }

    MetaSig msig(pSharedMD);
    ArgIterator argit(&msig);

    if (argit.HasParamType())
    {
        ArgLocDesc sInstArgLoc;
        argit.GetParamTypeLoc(&sInstArgLoc);
        int regHidden = sInstArgLoc.m_idxGenReg;
        _ASSERTE(regHidden != -1);
        regHidden += 10;//NOTE: RISCV64 should start at a0=10;

        if (extraArg == NULL)
        {
            if (pSharedMD->RequiresInstMethodTableArg())
            {
                // Unboxing stub case
                // Fill param arg with methodtable of this pointer
                // ld regHidden, a0, 0
                EmitLoad(IntReg(regHidden), IntReg(10));
            }
        }
        else
        {
            EmitMovConstant(IntReg(regHidden), (UINT64)extraArg);
        }
    }

    if (extraArg == NULL)
    {
        // Unboxing stub case
        // Address of the value type is address of the boxed instance plus sizeof(MethodDesc*).
        //  addi a0, a0, sizeof(MethodDesc*)
        EmitAddImm(IntReg(10), IntReg(10), sizeof(MethodDesc*));
    }

    // Tail call the real target.
    EmitCallManagedMethod(pSharedMD, TRUE /* tail call */);
    SetTargetMethod(pSharedMD);
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
    // Use direct call if possible.
    if (pMD->HasStableEntryPoint())
    {
        EmitCallLabel(NewExternalCodeLabel((LPVOID)pMD->GetStableEntryPoint()), fTailCall, FALSE);
    }
    else
    {
        EmitCallLabel(NewExternalCodeLabel((LPVOID)pMD->GetAddrOfSlot()), fTailCall, TRUE);
    }
}


#ifdef FEATURE_READYTORUN

//
// Allocation of dynamic helpers
//
#define DYNAMIC_HELPER_ALIGNMENT sizeof(TADDR)

#define BEGIN_DYNAMIC_HELPER_EMIT(size) \
    SIZE_T cb = size; \
    SIZE_T cbAligned = ALIGN_UP(cb, DYNAMIC_HELPER_ALIGNMENT); \
    BYTE * pStartRX = (BYTE *)(void*)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(cbAligned, DYNAMIC_HELPER_ALIGNMENT); \
    ExecutableWriterHolder<BYTE> startWriterHolder(pStartRX, cbAligned); \
    BYTE * pStart = startWriterHolder.GetRW(); \
    size_t rxOffset = pStartRX - pStart; \
    BYTE * p = pStart;

#define END_DYNAMIC_HELPER_EMIT() \
    _ASSERTE(pStart + cb == p); \
    while (p < pStart + cbAligned) { *(DWORD*)p = 0xffffff0f/*badcode*/; p += 4; }\
    ClrFlushInstructionCache(pStart, cbAligned); \
    return (PCODE)pStart
// Uses x8 as scratch register to store address of data label
// After load x8 is increment to point to next data
// only accepts positive offsets
static void LoadRegPair(BYTE* p, int reg1, int reg2, UINT32 offset)
{
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(32);

    const IntReg RegR0 = 0, RegT0 = 5, RegA0 = 10;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA0, RegT0, 16);// ld a0, 16(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT0, RegT0, 24);// ld t0, 24(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegT0, 0);// jalr zero, 0(t0)
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;
    // target
    *(PCODE*)p = target;
    p += 8;

    END_DYNAMIC_HELPER_EMIT();
}

// Caller must ensure sufficient byte are allocated including padding (if applicable)
void DynamicHelpers::EmitHelperWithArg(BYTE*& p, size_t rxOffset, LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    const IntReg RegR0 = 0, RegT0 = 5, RegA1 = 11;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA1, RegT0, 16);// ld a1, 16(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT0, RegT0, 24);// ld t0, 24(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegT0, 0);// jalr zero, 0(t0)
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;
    // target
    *(PCODE*)p = target;
    p += 8;
}

PCODE DynamicHelpers::CreateHelperWithArg(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(32);

    EmitHelperWithArg(p, rxOffset, pAllocator, arg, target);

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(48);

    const IntReg RegR0 = 0, RegT0 = 5, RegA0 = 10, RegA1 = 11;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA0, RegT0, 24);// ld a0, 24(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA1, RegT0, 32);// ld a1, 32(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT0, RegT0, 40);// ld t0, 40(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegT0, 0);// jalr x0, 0(t0)
    p += 4;

    *(DWORD*)p = ITypeInstr(0x13, 0, RegR0, RegR0, 0);// nop, padding to make 8 byte aligned
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;
    // arg2
    *(TADDR*)p = arg2;
    p += 8;
    // target
    *(TADDR*)p = target;
    p += 8;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperArgMove(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(40);

    const IntReg RegR0 = 0, RegT0 = 5, RegA0 = 10, RegA1 = 11;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x13, 0, RegA1, RegA0, 0);// addi a1, a0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA0, RegT0, 24);// ld a0, 24(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT0, RegT0, 32);// ld t0, 32(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegT0, 0);// jalr x0, 0(t0)
    p += 4;

    *(DWORD*)p = ITypeInstr(0x13, 0, RegR0, RegR0, 0);// nop, padding to make 8 byte aligned
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;
    // target
    *(TADDR*)p = target;
    p += 8;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturn(LoaderAllocator * pAllocator)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(4);

    const IntReg RegR0 = 0, RegRa = 1;

    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegRa, 0);// ret
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(24);

    const IntReg RegR0 = 0, RegRa = 1, RegT0 = 5, RegA0 = 10;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA0, RegT0, 16);// ld a0, 16(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegRa, 0);// ret
    p += 4;
    *(DWORD*)p = ITypeInstr(0x13, 0, RegR0, RegR0, 0);// nop, padding to make 8 byte aligned
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnIndirConst(LoaderAllocator * pAllocator, TADDR arg, INT8 offset)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(32);

    const IntReg RegR0 = 0, RegRa = 1, RegT0 = 5, RegA0 = 10;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA0, RegT0, 24);// ld a0, 24(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA0, RegA0, 0);// ld a0,0(a0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x13, 0, RegA0, RegA0, offset & 0xfff);// addi a0, a0, offset
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegRa, 0);// ret
    p += 4;
    *(DWORD*)p = ITypeInstr(0x13, 0, RegR0, RegR0, 0);// nop, padding to make 8 byte aligned
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(32);

    const IntReg RegR0 = 0, RegT0 = 5, RegA2 = 12;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA2, RegT0, 16);// ld a2,16(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT0, RegT0, 24);// ld t0,24(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegT0, 0);// jalr x0, 0(t0)
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;

    // target
    *(TADDR*)p = target;
    p += 8;
    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateHelperWithTwoArgs(LoaderAllocator * pAllocator, TADDR arg, TADDR arg2, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(48);

    const IntReg RegR0 = 0, RegT0 = 5, RegA2 = 12, RegA3 = 13;

    *(DWORD*)p = UTypeInstr(0x17, RegT0, 0);// auipc t0, 0
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA2, RegT0, 24);// ld a2,24(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA3, RegT0, 32);// ld a3,32(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT0, RegT0, 40);// ld t0,40(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegT0, 0);// jalr x0, 0(t0)
    p += 4;
    *(DWORD*)p = ITypeInstr(0x13, 0, RegR0, RegR0, 0);// nop, padding to make 8 byte aligned
    p += 4;

    // label:
    // arg
    *(TADDR*)p = arg;
    p += 8;
    // arg2
    *(TADDR*)p = arg2;
    p += 8;
    // target
    *(TADDR*)p = target;
    p += 8;
    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    STANDARD_VM_CONTRACT;
    const IntReg RegR0 = 0, RegA0 = 10, RegT2 = 7, RegT4 = 29, RegT5 = 30;

    PCODE helperAddress = (pLookup->helper == CORINFO_HELP_RUNTIMEHANDLE_METHOD ?
        GetEEFuncEntryPoint(JIT_GenericHandleMethodWithSlotAndModule) :
        GetEEFuncEntryPoint(JIT_GenericHandleClassWithSlotAndModule));

    GenericHandleArgs * pArgs = (GenericHandleArgs *)(void *)pAllocator->GetDynamicHelpersHeap()->AllocAlignedMem(sizeof(GenericHandleArgs), DYNAMIC_HELPER_ALIGNMENT);
    ExecutableWriterHolder<GenericHandleArgs> argsWriterHolder(pArgs, sizeof(GenericHandleArgs));
    argsWriterHolder.GetRW()->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
    argsWriterHolder.GetRW()->signature = pLookup->signature;
    argsWriterHolder.GetRW()->module = (CORINFO_MODULE_HANDLE)pModule;

    WORD slotOffset = (WORD)(dictionaryIndexAndSlot & 0xFFFF) * sizeof(Dictionary*);

    // It's available only via the run-time helper function
    if (pLookup->indirections == CORINFO_USEHELPER)
    {
        BEGIN_DYNAMIC_HELPER_EMIT(32);

        // a0 already contains generic context parameter
        // reuse EmitHelperWithArg for below two operations
        // a1 <- pArgs
        // branch to helperAddress
        EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);

        END_DYNAMIC_HELPER_EMIT();
    }
    else
    {
        int codeSize = 0;
        int indirectionsDataSize = 0;
        if (pLookup->testForNull || pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
        {
            //mv t2, a0
            codeSize += 4;
        }

        for (WORD i = 0; i < pLookup->indirections; i++) {
            _ASSERTE(pLookup->offsets[i] >= 0);
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                codeSize += (pLookup->sizeOffset > 2047 ? 24 : 16);
                indirectionsDataSize += (pLookup->sizeOffset > 2047 ? 4 : 0);
            }

            codeSize += (pLookup->offsets[i] > 2047 ? 12 : 4); // if( > 2047) (12 bytes) else 4 bytes for instructions.
            indirectionsDataSize += (pLookup->offsets[i] > 2047 ? 4 : 0); // 4 bytes for storing indirection offset values
        }

        codeSize += indirectionsDataSize ? 4 : 0; // auipc

        if (pLookup->testForNull)
        {
            codeSize += 12; // beq-ret-addi

            //padding for 8-byte align (required by EmitHelperWithArg)
            codeSize = ALIGN_UP(codeSize, 8);

            codeSize += 32; // size of EmitHelperWithArg
        }
        else
        {
            codeSize += 4; /* jalr */
        }

        // the offset value of data_label.
        uint dataOffset = codeSize;

        codeSize += indirectionsDataSize;

        BEGIN_DYNAMIC_HELPER_EMIT(codeSize);

        BYTE * old_p = p;

        if (indirectionsDataSize)
        {
            _ASSERTE(codeSize < 2047);

            //auipc t4, 0
            *(DWORD*)p = UTypeInstr(0x17, RegT4, 0);
            p += 4;
        }

        if (pLookup->testForNull || pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
        {
            *(DWORD*)p = ITypeInstr(0x13, 0, RegT2, RegA0, 0);// addi t2, a0, 0
            p += 4;
        }

        BYTE* pBLTCall = NULL;

        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                _ASSERTE(pLookup->testForNull && i > 0);

                if (pLookup->sizeOffset > 2047)
                {
                    *(DWORD*)p = ITypeInstr(0x3, 0x2, RegT4, RegT4, dataOffset);// lw  t4, dataOffset(t4)
                    p += 4;
                    *(DWORD*)p = RTypeInstr(0x33, 0, 0, RegT5, RegA0, RegT4);// add  t5, a0, t4
                    p += 4;
                    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT5, RegT5, 0);// ld  t5, 0(t5)
                    p += 4;

                    // move to next indirection offset data
                    dataOffset += 4;
                }
                else
                {
                    *(DWORD*)p = ITypeInstr(0x3, 0x3, RegT5, RegA0, (UINT32)pLookup->sizeOffset);// ld  t5, #(pLookup->sizeOffset)(a0)
                    p += 4;
                }
                // lui  t4, (slotOffset&0xfffff000)>>12
                *(DWORD*)p = UTypeInstr(0x37, RegT4, (((UINT32)slotOffset & 0xfffff000) >> 12));
                p += 4;
                *(DWORD*)p = ITypeInstr(0x13, 0, RegT4, RegT4, slotOffset & 0xfff);// addi  t4, t4, (slotOffset&0xfff)
                p += 4;
                // blt  t4, t5, CALL HELPER
                pBLTCall = p;       // Offset filled later
                p += 4;
            }

            if (pLookup->offsets[i] > 2047)
            {
                _ASSERTE(dataOffset < 2047);
                *(DWORD*)p = ITypeInstr(0x3, 0x2, RegT4, RegT4, dataOffset & 0xfff);// lw  t4, dataOffset(t4)
                p += 4;
                *(DWORD*)p = RTypeInstr(0x33, 0, 0, RegA0, RegA0, RegT4);// add  a0, a0, t4
                p += 4;
                *(DWORD*)p = ITypeInstr(0x3, 0x2, RegA0, RegA0, 0);// lw  a0, 0(a0)
                p += 4;
                // move to next indirection offset data
                dataOffset += 4; // add 4 as next data is at 4 bytes from previous data
            }
            else
            {
                // offset must be 8 byte aligned
                _ASSERTE((pLookup->offsets[i] & 0x7) == 0);
                *(DWORD*)p = ITypeInstr(0x3, 0x3, RegA0, RegA0, (UINT32)pLookup->offsets[i]);// ld  a0, #(pLookup->offsets[i])(a0)
                p += 4;
            }
        }

        // No null test required
        if (!pLookup->testForNull)
        {
            _ASSERTE(pLookup->sizeOffset == CORINFO_NO_SIZE_CHECK);
            *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegRa, 0);// ret
            p += 4;
        }
        else
        {
            // beq  a0, x0, CALL HELPER:
            *(DWORD*)p = BTypeInstr(0x63, 0, RegA0, RegR0, 8);
            p += 4;

            *(DWORD*)p = ITypeInstr(0x67, 0, RegR0, RegRa, 0);// ret
            p += 4;

            // CALL HELPER:
            if (pBLTCall != NULL)
                *(DWORD*)pBLTCall = BTypeInstr(0x63, 0x4, RegT4, RegT5, (UINT32)(p - pBLTCall));

            *(DWORD*)p = ITypeInstr(0x13, 0, RegA0, RegT2, 0);// addi  a0, t2, 0
            p += 4;
            if ((uintptr_t)(p - old_p) & 0x7)
            {
                // nop, padding for 8-byte align (required by EmitHelperWithArg)
                *(DWORD*)p = ITypeInstr(0x13, 0, RegR0, RegR0, 0);
                p += 4;
            }

            // reuse EmitHelperWithArg for below two operations
            // a1 <- pArgs
            // branch to helperAddress
            EmitHelperWithArg(p, rxOffset, pAllocator, (TADDR)pArgs, helperAddress);
        }

        // data_label:
        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK && pLookup->sizeOffset > 2047)
            {
                *(UINT32*)p = (UINT32)pLookup->sizeOffset;
                p += 4;
            }
            if (pLookup->offsets[i] > 2047)
            {
                *(UINT32*)p = (UINT32)pLookup->offsets[i];
                p += 4;
            }
        }

        END_DYNAMIC_HELPER_EMIT();
    }
}
#endif // FEATURE_READYTORUN


#endif // #ifndef DACCESS_COMPILE
