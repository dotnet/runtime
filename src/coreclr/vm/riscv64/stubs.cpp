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

void HelperMethodFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

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
    _ASSERTE(!"RISCV64: not implementation on riscv64!!!");
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

void TransitionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
{
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

void FaultingExceptionFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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

void InlinedCallFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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

void ResumableFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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

void HijackFrame::UpdateRegDisplay(const PREGDISPLAY pRD)
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
    if ((pCode->m_code[0] == 0x00009f97) && // auipc t6, 0
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

    m_code[0] = 0x00009f97; // auipc t6, 0
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
                Emit32((DWORD)(0x00000013 | (constant & 0xFFF) << 20 | (target << 7) | (target << 15))); // addi target, target, constant
            }
        }
        else
        {
            Emit32((DWORD)(0x00000013 | (constant & 0xFFF) << 20 | (target << 7))); // addi target, x0, constant
        }
    }
    else
    {
        UINT32 upper = constant >> 32;
        if (((upper + 0x800) >> 12) != 0)
        {
            Emit32((DWORD)(0x00000037 | (((upper + 0x800) >> 12) << 12) | (target << 7))); // lui target, (upper + 0x800) >> 12
            if ((upper & 0xFFF) != 0)
            {
                Emit32((DWORD)(0x00000013 | (upper & 0xFFF) << 20 | (target << 7) | (target << 15))); // addi target, target, upper 
            }
        }
        else
        {
            Emit32((DWORD)(0x00000013 | (upper & 0xFFF) << 20 | (target << 7))); // addi target, x0, upper 
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
                Emit32((DWORD)(0x00000013 | (current & 0x7FF) << 20 | (target << 7) | (target << 15))); // addi target, target, current
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
    Emit32(0x00000067 | (regTarget << 15));
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

void StubLinkerCPU::EmitLoadFloatRegImm(FloatReg ft, IntReg base, int offset)
{
    // fld ft,base,offset
    _ASSERTE(offset <= 2047 && offset >= -2048);
    Emit32(0x2b800000 | (base.reg << 15) | ((offset & 0xfff)<<20) | (ft.reg << 7));
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
    // On entry a0 holds the delegate instance. Look up the real target address stored in the MethodPtrAux
    // field and saved in t6. Tailcall to the target method after re-arranging the arguments
    // ld  t6, a0, offsetof(DelegateObject, _methodPtrAux)
    EmitLoadStoreRegImm(eLOAD, IntReg(31)/*t6*/, IntReg(10)/*a0*/, DelegateObject::GetOffsetOfMethodPtrAux());
    // addi t5, a0, DelegateObject::GetOffsetOfMethodPtrAux() - load the indirection cell into t5 used by ResolveWorkerAsmStub
    EmitAddImm(30/*t5*/, 10/*a0*/, DelegateObject::GetOffsetOfMethodPtrAux());

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
                _ASSERTE(!"RISCV64: not validated on riscv64!!!");
                // FirstFloatReg is 10;
                int j = 10;
                while (pEntry[j].srcofs & ShuffleEntry::FPREGMASK)
                {
                    j++;
                }
                assert((pEntry->dstofs - pEntry->srcofs) == index);
                assert(8 > index);

                int tmp_reg = 0; // f0.
                ShuffleEntry* tmp_entry = pShuffleEntryArray + delay_index[0];
                while (index)
                {
                    // fld(Ft, sp, offset);
                    _ASSERTE(isValidSimm12(tmp_entry->srcofs << 3));
                    Emit32(0x3007 | (tmp_reg << 15) | (2 << 7/*sp*/) | ((tmp_entry->srcofs << 3) << 20));
                    tmp_reg++;
                    index--;
                    tmp_entry++;
                }

                j -= 1;
                tmp_entry = pEntry + j;
                i += j;
                while (pEntry[j].srcofs & ShuffleEntry::FPREGMASK)
                {
                    if (pEntry[j].dstofs & ShuffleEntry::FPREGMASK)// fsgnj.d fd, fs, fs
                        Emit32(0x22000053 | ((pEntry[j].dstofs & ShuffleEntry::OFSREGMASK) << 7) | ((pEntry[j].srcofs & ShuffleEntry::OFSREGMASK) << 15) | ((pEntry[j].srcofs & ShuffleEntry::OFSREGMASK) << 20));
                    else //// fsd(Ft, Rn, offset);
                    {
                        _ASSERTE(isValidSimm12((pEntry[j].dstofs * sizeof(long))));
                        Emit32(0x3027 | ((pEntry[j].srcofs & ShuffleEntry::OFSREGMASK) << 20) | (2 << 15 /*sp*/) | ((pEntry[j].dstofs * sizeof(long) & 0x1f) << 7) | ((pEntry[j].dstofs * sizeof(long) & 0x7f) << 25));
                    }
                    j--;
                }
                assert(tmp_reg <= 11);
                /*
                while (tmp_reg > 11)
                {
                    tmp_reg--;
                    // fmov.d fd, fs
                    Emit32(0x01149800 | index | (tmp_reg << 5));
                    index++;
                }
                */
                index = 0;
                pEntry = tmp_entry;
            }
            else
            {
                // 10 is the offset of FirstGenArgReg to FirstGenReg
                assert(pEntry->dstofs & ShuffleEntry::REGMASK);
                assert((pEntry->dstofs & ShuffleEntry::OFSMASK) < (pEntry->srcofs & ShuffleEntry::OFSMASK));
                EmitMovReg(IntReg((pEntry->dstofs & ShuffleEntry::OFSMASK) + 10), IntReg((pEntry->srcofs & ShuffleEntry::OFSMASK) + 10));
            }
        }
        else if (pEntry->dstofs & ShuffleEntry::REGMASK)
        {
            // source must be on the stack
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));

            if (pEntry->dstofs & ShuffleEntry::FPREGMASK)
            {
                if (!is_store)
                {
                    delay_index[index++] = i;
                    continue;
                }
                EmitLoadFloatRegImm(FloatReg((pEntry->dstofs & ShuffleEntry::OFSREGMASK) + 10), RegSp, pEntry->srcofs * sizeof(void*));
            }
            else
            {
                assert(pEntry->dstofs & ShuffleEntry::REGMASK);
                EmitLoadStoreRegImm(eLOAD, IntReg((pEntry->dstofs & ShuffleEntry::OFSMASK) + 10), RegSp, pEntry->srcofs * sizeof(void*));
            }
        }
        else
        {
            // source must be on the stack
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));

            // dest must be on the stack
            _ASSERTE(!(pEntry->dstofs & ShuffleEntry::REGMASK));

            EmitLoadStoreRegImm(eLOAD, IntReg(29)/*t4*/, RegSp, pEntry->srcofs * sizeof(void*));
            EmitLoadStoreRegImm(eSTORE, IntReg(29)/*t4*/, RegSp, pEntry->dstofs * sizeof(void*));
        }
    }

    // Tailcall to target
    // jalr x0, 0(t6)
    EmitJumpRegister(31);
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

        EmitMovReg(IntReg((pEntry->dstofs & ShuffleEntry::OFSREGMASK) + 4), IntReg((pEntry->srcofs & ShuffleEntry::OFSREGMASK) + 4));
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
                EmitLoadStoreRegImm(eLOAD, IntReg(regHidden), IntReg(10), 0);
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
