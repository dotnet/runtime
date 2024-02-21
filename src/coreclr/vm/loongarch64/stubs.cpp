// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubs.cpp
//
// This file contains stub functions for unimplemented features need to
// run on the LOONGARCH64 platform.

#include "common.h"
#include "dllimportcallback.h"
#include "comdelegate.h"
#include "asmconstants.h"
#include "virtualcallstub.h"
#include "jitinterface.h"
#include "ecall.h"



#ifndef DACCESS_COMPILE
//-----------------------------------------------------------------------
// InstructionFormat for B.cond
//-----------------------------------------------------------------------
class ConditionalBranchInstructionFormat : public InstructionFormat
{

    public:
        ConditionalBranchInstructionFormat() : InstructionFormat(InstructionFormat::k32)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(!"LOONGARCH64: not implementation on loongarch64!!!");
            _ASSERTE(refsize == InstructionFormat::k32);

            return 4;
        }

        virtual UINT GetHotSpotOffset(UINT refsize, UINT variationCode)
        {
            WRAPPER_NO_CONTRACT;
            return 0;
        }


        virtual BOOL CanReach(UINT refSize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            _ASSERTE(!fExternal || "LOONGARCH64:NYI - CompareAndBranchInstructionFormat::CanReach external");
            if (fExternal)
                return false;

            if (offset < -1048576 || offset > 1048572)
                return false;
            return true;
        }
        ////TODO: add for LOONGARCH. unused now!
        // B.<cond> <label>
        // Encoding 0|1|0|1|0|1|0|0|imm19|0|cond
        // cond = Bits3-0(variation)
        // imm19 = bits19-0(fixedUpReference/4), will be SignExtended
        virtual VOID EmitInstruction(UINT refSize, __int64 fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            _ASSERTE(!"LOONGARCH64: not implementation on loongarch64!!!");
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(refSize == InstructionFormat::k32);

            if (fixedUpReference < -1048576 || fixedUpReference > 1048572)
                COMPlusThrow(kNotSupportedException);

            _ASSERTE((fixedUpReference & 0x3) == 0);
            DWORD imm19 = (DWORD)(0x7FFFF & (fixedUpReference >> 2));

            pOutBufferRW[0] = static_cast<BYTE>((0x7 & imm19 /* Bits2-0(imm19) */) << 5  | (0xF & variationCode /* cond */));
            pOutBufferRW[1] = static_cast<BYTE>((0x7F8 & imm19 /* Bits10-3(imm19) */) >> 3);
            pOutBufferRW[2] = static_cast<BYTE>((0x7F800 & imm19 /* Bits19-11(imm19) */) >> 11);
            pOutBufferRW[3] = static_cast<BYTE>(0x54);
        }
};

//-----------------------------------------------------------------------
// InstructionFormat for JIRL (unconditional jump)
//-----------------------------------------------------------------------
class BranchInstructionFormat : public InstructionFormat
{
    // Encoding of the VariationCode:
    // bit(0) indicates whether this is a direct or an indirect jump.
    // bit(1) indicates whether this is a branch with link -a.k.a call- jirl $r0/1,$r21,0

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

                UINT32 imm16 = (UINT32)(0xFFF & dataOffset);
                //pcaddi $r21,0
                //ld.d  $r21,$r21,dataOffset
                //ld.d  $r21,$r21,0
                //jirl  $r0/1,$r21,0

                *(DWORD*)pOutBufferRW = 0x18000015 | ((dataOffset >>14) <<5);//pcaddi $r21,0
                *(DWORD*)(pOutBufferRW + 4) = 0x28c002b5 | (imm16 << 10);//ld.d  $r21,$r21,dataOffset-low12
                *(DWORD*)(pOutBufferRW+8) = 0x28c002b5;//ld.d  $r21,$r21,0
                if (IsCall(variationCode))
                {
                    *(DWORD*)(pOutBufferRW+12) = 0x4c0002a1;//jirl  $ra,$r21,0
                }
                else
                {
                    *(DWORD*)(pOutBufferRW+12) = 0x4c0002a0;//jirl  $r0,$r21,0
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
                //pcaddi $r21,0
                //ld.d  $r21,$r21,dataOffset
                //jirl  $r0/1,$r21,0

                *(DWORD*)pOutBufferRW = 0x18000015 | ((dataOffset >>14) <<5);//pcaddi $r21,0
                *(DWORD*)(pOutBufferRW + 4) = 0x28c002b5 | (imm12 << 10);//ld.d  $r21,$r21,dataOffset-low12
                if (IsCall(variationCode))
                {
                    *((DWORD*)(pOutBufferRW+8)) = 0x4c0002a1;//jirl  $ra,$r21,0
                }
                else
                {
                    *((DWORD*)(pOutBufferRW+8)) = 0x4c0002a0;//jirl  $r0,$r21,0
                }

                if (!ClrSafeInt<__int64>::addition(fixedUpReference, (__int64)pOutBufferRX, fixedUpReference))
                    COMPlusThrowArithmetic();
                *((__int64*)pDataBuffer) = fixedUpReference;
            }
        }

};

//-----------------------------------------------------------------------
// InstructionFormat for loading a label to the register (pcaddi/ld.d)
//-----------------------------------------------------------------------
class LoadFromLabelInstructionFormat : public InstructionFormat
{
    public:
        LoadFromLabelInstructionFormat() : InstructionFormat( InstructionFormat::k32)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refSize, UINT variationCode)
        {
            _ASSERTE(!"LOONGARCH64: not implementation on loongarch64!!!");
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
            return fExternal;
        }

        virtual VOID EmitInstruction(UINT refSize, __int64 fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            _ASSERTE(!"LOONGARCH64: not implementation on loongarch64!!!");
            LIMITED_METHOD_CONTRACT;
            // VariationCode is used to indicate the register the label is going to be loaded

            DWORD imm =(DWORD)(fixedUpReference>>12);
            if (imm>>21)
                COMPlusThrow(kNotSupportedException);

            // pcaddi r21, #Page_of_fixedUpReference
            *((DWORD*)pOutBufferRW) = 0;

            // ld.d  r21, [r21, #offset_of_fixedUpReference_to_its_page]
            UINT64 target = (UINT64)(fixedUpReference + pOutBufferRX)>>3;
            *((DWORD*)(pOutBufferRW+4)) = 0;
        }
};



static BYTE gConditionalBranchIF[sizeof(ConditionalBranchInstructionFormat)];
static BYTE gBranchIF[sizeof(BranchInstructionFormat)];
//static BYTE gLoadFromLabelIF[sizeof(LoadFromLabelInstructionFormat)];

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
    pRD->volatileCurrContextPointers.T7 = NULL;
    pRD->volatileCurrContextPointers.T8 = NULL;
    pRD->volatileCurrContextPointers.X0 = NULL;
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

    context.S0 = unwoundstate->captureCalleeSavedRegisters[0] = baseState->captureCalleeSavedRegisters[0];
    context.S1 = unwoundstate->captureCalleeSavedRegisters[1] = baseState->captureCalleeSavedRegisters[1];
    context.S2 = unwoundstate->captureCalleeSavedRegisters[2] = baseState->captureCalleeSavedRegisters[2];
    context.S3 = unwoundstate->captureCalleeSavedRegisters[3] = baseState->captureCalleeSavedRegisters[3];
    context.S4 = unwoundstate->captureCalleeSavedRegisters[4] = baseState->captureCalleeSavedRegisters[4];
    context.S5 = unwoundstate->captureCalleeSavedRegisters[5] = baseState->captureCalleeSavedRegisters[5];
    context.S6 = unwoundstate->captureCalleeSavedRegisters[6] = baseState->captureCalleeSavedRegisters[6];
    context.S7 = unwoundstate->captureCalleeSavedRegisters[7] = baseState->captureCalleeSavedRegisters[7];
    context.S8 = unwoundstate->captureCalleeSavedRegisters[8] = baseState->captureCalleeSavedRegisters[8];
    context.Fp = unwoundstate->captureCalleeSavedRegisters[9] = baseState->captureCalleeSavedRegisters[9];
    context.Tp = unwoundstate->captureCalleeSavedRegisters[10] = baseState->captureCalleeSavedRegisters[10];
    context.Ra = NULL; // Filled by the unwinder

    context.Sp = baseState->captureSp;
    context.Pc = baseState->captureIp;

#if !defined(DACCESS_COMPILE)
    // For DAC, if we get here, it means that the LazyMachState is uninitialized and we have to unwind it.
    // The API we use to unwind in DAC is StackWalk64(), which does not support the context pointers.
    //
    // Restore the integer registers to KNONVOLATILE_CONTEXT_POINTERS to be used for unwinding.
    nonVolContextPtrs.S0 = &unwoundstate->captureCalleeSavedRegisters[0];
    nonVolContextPtrs.S1 = &unwoundstate->captureCalleeSavedRegisters[1];
    nonVolContextPtrs.S2 = &unwoundstate->captureCalleeSavedRegisters[2];
    nonVolContextPtrs.S3 = &unwoundstate->captureCalleeSavedRegisters[3];
    nonVolContextPtrs.S4 = &unwoundstate->captureCalleeSavedRegisters[4];
    nonVolContextPtrs.S5 = &unwoundstate->captureCalleeSavedRegisters[5];
    nonVolContextPtrs.S6 = &unwoundstate->captureCalleeSavedRegisters[6];
    nonVolContextPtrs.S7 = &unwoundstate->captureCalleeSavedRegisters[7];
    nonVolContextPtrs.S8 = &unwoundstate->captureCalleeSavedRegisters[8];
    nonVolContextPtrs.Fp = &unwoundstate->captureCalleeSavedRegisters[9];
    nonVolContextPtrs.Tp = &unwoundstate->captureCalleeSavedRegisters[10];
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
    unwoundstate->captureCalleeSavedRegisters[0] = context.S0;
    unwoundstate->captureCalleeSavedRegisters[1] = context.S1;
    unwoundstate->captureCalleeSavedRegisters[2] = context.S2;
    unwoundstate->captureCalleeSavedRegisters[3] = context.S3;
    unwoundstate->captureCalleeSavedRegisters[4] = context.S4;
    unwoundstate->captureCalleeSavedRegisters[5] = context.S5;
    unwoundstate->captureCalleeSavedRegisters[6] = context.S6;
    unwoundstate->captureCalleeSavedRegisters[7] = context.S7;
    unwoundstate->captureCalleeSavedRegisters[8] = context.S8;
    unwoundstate->captureCalleeSavedRegisters[9] = context.Fp;
    unwoundstate->captureCalleeSavedRegisters[10] = context.Tp;
#endif

#ifdef DACCESS_COMPILE
    // For DAC builds, we update the registers directly since we dont have context pointers
    unwoundstate->captureCalleeSavedRegisters[0] = context.S0;
    unwoundstate->captureCalleeSavedRegisters[1] = context.S1;
    unwoundstate->captureCalleeSavedRegisters[2] = context.S2;
    unwoundstate->captureCalleeSavedRegisters[3] = context.S3;
    unwoundstate->captureCalleeSavedRegisters[4] = context.S4;
    unwoundstate->captureCalleeSavedRegisters[5] = context.S5;
    unwoundstate->captureCalleeSavedRegisters[6] = context.S6;
    unwoundstate->captureCalleeSavedRegisters[7] = context.S7;
    unwoundstate->captureCalleeSavedRegisters[8] = context.S8;
    unwoundstate->captureCalleeSavedRegisters[9] = context.Fp;
    unwoundstate->captureCalleeSavedRegisters[10] = context.Tp;
#else // !DACCESS_COMPILE
    // For non-DAC builds, update the register state from context pointers
    unwoundstate->ptrCalleeSavedRegisters[0] = nonVolContextPtrs.S0;
    unwoundstate->ptrCalleeSavedRegisters[1] = nonVolContextPtrs.S1;
    unwoundstate->ptrCalleeSavedRegisters[2] = nonVolContextPtrs.S2;
    unwoundstate->ptrCalleeSavedRegisters[3] = nonVolContextPtrs.S3;
    unwoundstate->ptrCalleeSavedRegisters[4] = nonVolContextPtrs.S4;
    unwoundstate->ptrCalleeSavedRegisters[5] = nonVolContextPtrs.S5;
    unwoundstate->ptrCalleeSavedRegisters[6] = nonVolContextPtrs.S6;
    unwoundstate->ptrCalleeSavedRegisters[7] = nonVolContextPtrs.S7;
    unwoundstate->ptrCalleeSavedRegisters[8] = nonVolContextPtrs.S8;
    unwoundstate->ptrCalleeSavedRegisters[9] = nonVolContextPtrs.Fp;
    unwoundstate->ptrCalleeSavedRegisters[10] = nonVolContextPtrs.Tp;
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
        pRD->pCurrentContext->S0 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[0]);
        pRD->pCurrentContext->S1 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[1]);
        pRD->pCurrentContext->S2 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[2]);
        pRD->pCurrentContext->S3 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[3]);
        pRD->pCurrentContext->S4 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[4]);
        pRD->pCurrentContext->S5 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[5]);
        pRD->pCurrentContext->S6 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[6]);
        pRD->pCurrentContext->S7 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[7]);
        pRD->pCurrentContext->S8 = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[8]);
        pRD->pCurrentContext->Fp = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[9]);
        pRD->pCurrentContext->Tp = (DWORD64)(pUnwoundState->captureCalleeSavedRegisters[10]);
        pRD->pCurrentContext->Ra = NULL; // Unwind again to get Caller's PC

        pRD->pCurrentContextPointers->S0 = pUnwoundState->ptrCalleeSavedRegisters[0];
        pRD->pCurrentContextPointers->S1 = pUnwoundState->ptrCalleeSavedRegisters[1];
        pRD->pCurrentContextPointers->S2 = pUnwoundState->ptrCalleeSavedRegisters[2];
        pRD->pCurrentContextPointers->S3 = pUnwoundState->ptrCalleeSavedRegisters[3];
        pRD->pCurrentContextPointers->S4 = pUnwoundState->ptrCalleeSavedRegisters[4];
        pRD->pCurrentContextPointers->S5 = pUnwoundState->ptrCalleeSavedRegisters[5];
        pRD->pCurrentContextPointers->S6 = pUnwoundState->ptrCalleeSavedRegisters[6];
        pRD->pCurrentContextPointers->S7 = pUnwoundState->ptrCalleeSavedRegisters[7];
        pRD->pCurrentContextPointers->S8 = pUnwoundState->ptrCalleeSavedRegisters[8];
        pRD->pCurrentContextPointers->Fp = pUnwoundState->ptrCalleeSavedRegisters[9];
        pRD->pCurrentContextPointers->Tp = pUnwoundState->ptrCalleeSavedRegisters[10];
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
    pRD->pCurrentContext->S0 = m_MachState.ptrCalleeSavedRegisters[0] ? *m_MachState.ptrCalleeSavedRegisters[0] : m_MachState.captureCalleeSavedRegisters[0];
    pRD->pCurrentContext->S1 = m_MachState.ptrCalleeSavedRegisters[1] ? *m_MachState.ptrCalleeSavedRegisters[1] : m_MachState.captureCalleeSavedRegisters[1];
    pRD->pCurrentContext->S2 = m_MachState.ptrCalleeSavedRegisters[2] ? *m_MachState.ptrCalleeSavedRegisters[2] : m_MachState.captureCalleeSavedRegisters[2];
    pRD->pCurrentContext->S3 = m_MachState.ptrCalleeSavedRegisters[3] ? *m_MachState.ptrCalleeSavedRegisters[3] : m_MachState.captureCalleeSavedRegisters[3];
    pRD->pCurrentContext->S4 = m_MachState.ptrCalleeSavedRegisters[4] ? *m_MachState.ptrCalleeSavedRegisters[4] : m_MachState.captureCalleeSavedRegisters[4];
    pRD->pCurrentContext->S5 = m_MachState.ptrCalleeSavedRegisters[5] ? *m_MachState.ptrCalleeSavedRegisters[5] : m_MachState.captureCalleeSavedRegisters[5];
    pRD->pCurrentContext->S6 = m_MachState.ptrCalleeSavedRegisters[6] ? *m_MachState.ptrCalleeSavedRegisters[6] : m_MachState.captureCalleeSavedRegisters[6];
    pRD->pCurrentContext->S7 = m_MachState.ptrCalleeSavedRegisters[7] ? *m_MachState.ptrCalleeSavedRegisters[7] : m_MachState.captureCalleeSavedRegisters[7];
    pRD->pCurrentContext->S8 = m_MachState.ptrCalleeSavedRegisters[8] ? *m_MachState.ptrCalleeSavedRegisters[8] : m_MachState.captureCalleeSavedRegisters[8];
    pRD->pCurrentContext->Fp = m_MachState.ptrCalleeSavedRegisters[9] ? *m_MachState.ptrCalleeSavedRegisters[9] : m_MachState.captureCalleeSavedRegisters[9];
    pRD->pCurrentContext->Tp = m_MachState.ptrCalleeSavedRegisters[10] ? *m_MachState.ptrCalleeSavedRegisters[10] : m_MachState.captureCalleeSavedRegisters[10];
    pRD->pCurrentContext->Ra = NULL; // Unwind again to get Caller's PC
#else // TARGET_UNIX
    pRD->pCurrentContext->S0 = *m_MachState.ptrCalleeSavedRegisters[0];
    pRD->pCurrentContext->S1 = *m_MachState.ptrCalleeSavedRegisters[1];
    pRD->pCurrentContext->S2 = *m_MachState.ptrCalleeSavedRegisters[2];
    pRD->pCurrentContext->S3 = *m_MachState.ptrCalleeSavedRegisters[3];
    pRD->pCurrentContext->S4 = *m_MachState.ptrCalleeSavedRegisters[4];
    pRD->pCurrentContext->S5 = *m_MachState.ptrCalleeSavedRegisters[5];
    pRD->pCurrentContext->S6 = *m_MachState.ptrCalleeSavedRegisters[6];
    pRD->pCurrentContext->S7 = *m_MachState.ptrCalleeSavedRegisters[7];
    pRD->pCurrentContext->S8 = *m_MachState.ptrCalleeSavedRegisters[8];
    pRD->pCurrentContext->Fp = *m_MachState.ptrCalleeSavedRegisters[9];
    pRD->pCurrentContext->Tp = *m_MachState.ptrCalleeSavedRegisters[10];
    pRD->pCurrentContext->Ra = NULL; // Unwind again to get Caller's PC
#endif

#if !defined(DACCESS_COMPILE)
    pRD->pCurrentContextPointers->S0 = m_MachState.ptrCalleeSavedRegisters[0];
    pRD->pCurrentContextPointers->S1 = m_MachState.ptrCalleeSavedRegisters[1];
    pRD->pCurrentContextPointers->S2 = m_MachState.ptrCalleeSavedRegisters[2];
    pRD->pCurrentContextPointers->S3 = m_MachState.ptrCalleeSavedRegisters[3];
    pRD->pCurrentContextPointers->S4 = m_MachState.ptrCalleeSavedRegisters[4];
    pRD->pCurrentContextPointers->S5 = m_MachState.ptrCalleeSavedRegisters[5];
    pRD->pCurrentContextPointers->S6 = m_MachState.ptrCalleeSavedRegisters[6];
    pRD->pCurrentContextPointers->S7 = m_MachState.ptrCalleeSavedRegisters[7];
    pRD->pCurrentContextPointers->S8 = m_MachState.ptrCalleeSavedRegisters[8];
    pRD->pCurrentContextPointers->Fp = m_MachState.ptrCalleeSavedRegisters[9];
    pRD->pCurrentContextPointers->Tp = m_MachState.ptrCalleeSavedRegisters[10];
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
    m_rgCode[0] = 0x18000055; //pcaddi  r21,2
    m_rgCode[1] = 0x28c042b5; //ld.d  r21,16(r21)
    m_rgCode[2] = 0x0380008f; //ori  r15,a0,0x0
    m_rgCode[3] = 0x038000a4; //ori  a0,a1,0x0
    m_rgCode[4] = 0x038001e5; //ori  a1,r15,0x0
    m_rgCode[5] = 0x4c0002a0; //jirl  r0,r21,0

    _ASSERTE((UINT32*)&m_pTarget == &m_rgCode[6]);
    _ASSERTE(6 == ARRAY_SIZE(m_rgCode));

    m_pTarget = GetPreStubEntryPoint();
    m_pMethodDesc = (TADDR)pMD;
}

#endif // !DACCESS_COMPILE

void UpdateRegDisplayFromCalleeSavedRegisters(REGDISPLAY * pRD, CalleeSavedRegisters * pCalleeSaved)
{
    LIMITED_METHOD_CONTRACT;
    pRD->pCurrentContext->S0 = pCalleeSaved->s0;
    pRD->pCurrentContext->S1 = pCalleeSaved->s1;
    pRD->pCurrentContext->S2 = pCalleeSaved->s2;
    pRD->pCurrentContext->S3 = pCalleeSaved->s3;
    pRD->pCurrentContext->S4 = pCalleeSaved->s4;
    pRD->pCurrentContext->S5 = pCalleeSaved->s5;
    pRD->pCurrentContext->S6 = pCalleeSaved->s6;
    pRD->pCurrentContext->S7 = pCalleeSaved->s7;
    pRD->pCurrentContext->S8 = pCalleeSaved->s8;
    pRD->pCurrentContext->Tp = pCalleeSaved->tp;
    pRD->pCurrentContext->Fp  = pCalleeSaved->fp;
    pRD->pCurrentContext->Ra  = pCalleeSaved->ra;

    T_KNONVOLATILE_CONTEXT_POINTERS * pContextPointers = pRD->pCurrentContextPointers;
    pContextPointers->S0 = (PDWORD64)&pCalleeSaved->s0;
    pContextPointers->S1 = (PDWORD64)&pCalleeSaved->s1;
    pContextPointers->S2 = (PDWORD64)&pCalleeSaved->s2;
    pContextPointers->S3 = (PDWORD64)&pCalleeSaved->s3;
    pContextPointers->S4 = (PDWORD64)&pCalleeSaved->s4;
    pContextPointers->S5 = (PDWORD64)&pCalleeSaved->s5;
    pContextPointers->S6 = (PDWORD64)&pCalleeSaved->s6;
    pContextPointers->S7 = (PDWORD64)&pCalleeSaved->s7;
    pContextPointers->S8 = (PDWORD64)&pCalleeSaved->s8;
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
    pRD->pCurrentContextPointers->S0 = (PDWORD64)&m_ctx.S0;
    pRD->pCurrentContextPointers->S1 = (PDWORD64)&m_ctx.S1;
    pRD->pCurrentContextPointers->S2 = (PDWORD64)&m_ctx.S2;
    pRD->pCurrentContextPointers->S3 = (PDWORD64)&m_ctx.S3;
    pRD->pCurrentContextPointers->S4 = (PDWORD64)&m_ctx.S4;
    pRD->pCurrentContextPointers->S5 = (PDWORD64)&m_ctx.S5;
    pRD->pCurrentContextPointers->S6 = (PDWORD64)&m_ctx.S6;
    pRD->pCurrentContextPointers->S7 = (PDWORD64)&m_ctx.S7;
    pRD->pCurrentContextPointers->S8 = (PDWORD64)&m_ctx.S8;
    pRD->pCurrentContextPointers->Fp = (PDWORD64)&m_ctx.Fp;
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

    pRD->pCurrentContextPointers->S0 = NULL;
    pRD->pCurrentContextPointers->S1 = NULL;
    pRD->pCurrentContextPointers->S2 = NULL;
    pRD->pCurrentContextPointers->S3 = NULL;
    pRD->pCurrentContextPointers->S4 = NULL;
    pRD->pCurrentContextPointers->S5 = NULL;
    pRD->pCurrentContextPointers->S6 = NULL;
    pRD->pCurrentContextPointers->S7 = NULL;
    pRD->pCurrentContextPointers->S8 = NULL;
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

    pRD->pCurrentContextPointers->S0 = &m_Regs->S0;
    pRD->pCurrentContextPointers->S1 = &m_Regs->S1;
    pRD->pCurrentContextPointers->S2 = &m_Regs->S2;
    pRD->pCurrentContextPointers->S3 = &m_Regs->S3;
    pRD->pCurrentContextPointers->S4 = &m_Regs->S4;
    pRD->pCurrentContextPointers->S5 = &m_Regs->S5;
    pRD->pCurrentContextPointers->S6 = &m_Regs->S6;
    pRD->pCurrentContextPointers->S7 = &m_Regs->S7;
    pRD->pCurrentContextPointers->S8 = &m_Regs->S8;
    pRD->pCurrentContextPointers->Tp = &m_Regs->Tp;
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
    pRD->volatileCurrContextPointers.T7 = &m_Regs->T7;
    pRD->volatileCurrContextPointers.T8 = &m_Regs->T8;
    pRD->volatileCurrContextPointers.X0 = &m_Regs->X0;

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

    pRD->pCurrentContext->S0 = m_Args->S0;
    pRD->pCurrentContext->S1 = m_Args->S1;
    pRD->pCurrentContext->S2 = m_Args->S2;
    pRD->pCurrentContext->S3 = m_Args->S3;
    pRD->pCurrentContext->S4 = m_Args->S4;
    pRD->pCurrentContext->S5 = m_Args->S5;
    pRD->pCurrentContext->S6 = m_Args->S6;
    pRD->pCurrentContext->S7 = m_Args->S7;
    pRD->pCurrentContext->S8 = m_Args->S8;
    pRD->pCurrentContext->Tp = m_Args->Tp;
    pRD->pCurrentContext->Fp = m_Args->Fp;
    pRD->pCurrentContext->Ra = m_Args->Ra;

    pRD->pCurrentContextPointers->S0 = &m_Args->S0;
    pRD->pCurrentContextPointers->S1 = &m_Args->S1;
    pRD->pCurrentContextPointers->S2 = &m_Args->S2;
    pRD->pCurrentContextPointers->S3 = &m_Args->S3;
    pRD->pCurrentContextPointers->S4 = &m_Args->S4;
    pRD->pCurrentContextPointers->S5 = &m_Args->S5;
    pRD->pCurrentContextPointers->S6 = &m_Args->S6;
    pRD->pCurrentContextPointers->S7 = &m_Args->S7;
    pRD->pCurrentContextPointers->S8 = &m_Args->S8;
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
    WRAPPER_NO_CONTRACT;

    // pcaddi  $r21,0
	// ld.d  $t2, label_comCallMethodDesc
	// ld.d  $r21, label_target
	// jirl  $r0,$r21,0
	// label_target:
    // target address (8 bytes)
    // label_comCallMethodDesc:
    DWORD rgCode[] = {
        0x0,
        0x0,
        0x0
    };

    _ASSERTE(!"LOONGARCH64: not implementation on loongarch64!!!");

    BYTE *pBufferRX = (BYTE*)pCOMMethodRX - COMMETHOD_CALL_PRESTUB_SIZE;
    BYTE *pBufferRW = (BYTE*)pCOMMethodRW - COMMETHOD_CALL_PRESTUB_SIZE;

    memcpy(pBufferRW, rgCode, sizeof(rgCode));
    *((PCODE*)(pBufferRW + sizeof(rgCode) + 4)) = target;

    // Ensure that the updated instructions get actually written
    ClrFlushInstructionCache(pBufferRX, COMMETHOD_CALL_PRESTUB_SIZE);

    _ASSERTE(IS_ALIGNED(pBufferRX + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET, sizeof(void*)) &&
             *((PCODE*)(pBufferRX + COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET)) == target);
}
#endif // FEATURE_COMINTEROP


void JIT_TailCall()
{
    _ASSERTE(!"LOONGARCH64:NYI");
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
    LIMITED_METHOD_CONTRACT;

    return (FaultingExceptionFrame*)((TADDR)pDispatcherContext->ContextRecord->S0);
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
    if ((pCode->m_code[0] == 0x18000095) && //pcaddi  $r21,4
        (pCode->m_code[1] == 0x28c022ae) && //ld.d  $t2,$r21,8
        (pCode->m_code[2] == 0x28c002b5) && //ld.d  $r21,$r21,0
        (pCode->m_code[3] == 0x4c0002a0))   //jirl  $r0,$r21,0
    {
        return (UMEntryThunk*)pCode->m_pvSecretParam;
    }

    return NULL;
}

void UMEntryThunkCode::Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam)
{
    // pcaddi  $r21,4
    // ld.d  $t2,$r21,8
    // ld.d  $r21,$r21,0
    // jirl  $r0,$r21,0
    // m_pTargetCode data
    // m_pvSecretParam data

    m_code[0] = 0x18000095; //pcaddi  $r21,4
    m_code[1] = 0x28c022ae; //ld.d  $t2,$r21,8
    m_code[2] = 0x28c002b5; //ld.d  $r21,$r21,0
    m_code[3] = 0x4c0002a0; //jirl  $r0,$r21,0

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

    // ld.d $a0,$r21,8
    pThisRW->m_code[1] = 0x28c022a4;

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
    // this wouldn't be called in loongarch64, just to comply with gchelpers.h
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
    _ASSERTE(!"LOONGARCH64:NYI");
    return FALSE;
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
// ----------------------------------------------------------------
// StubLinkerCPU methods
// ----------------------------------------------------------------

void StubLinkerCPU::EmitMovConstant(IntReg target, UINT64 constant)
{
    // Move the 64bit constant into targetReg.

    _ASSERTE((0 <= target) && ( target <= 31));

    Emit32((DWORD)(0x02c00000 | target)); // addi.d  target, $r0, 0

    //TODO: maybe optimize further.
    if (0 == (constant >> 12))
    {
        Emit32((DWORD)(0x03800000 | (constant & 0xFFF)<<10 | target<<5 | target)); // ori  target, target, constant
    }
    else if (0 == (constant >> 32))
    {
        Emit32((DWORD)(0x14000000 | ((constant>>12) & 0xFFFFF)<<5 | target));      // lu12i.w  target, constant>>12
        Emit32((DWORD)(0x03800000 | (constant & 0xFFF)<<10 | target<<5 | target)); // ori  target, target, constant

        if (constant & 0x80000000)
        {
            Emit32((DWORD)(0x00DF0000 | target<<5 | target)); // bstrpick.d  target, target ,31 ,0
        }
    }
    else if (0 == (constant >> 52))
    {
        Emit32((DWORD)(0x14000000 | ((constant>>12) & 0xFFFFF)<<5 | target));      // lu12i.w  target, constant>>12
        Emit32((DWORD)(0x03800000 | (constant & 0xFFF)<<10 | target<<5 | target)); // ori  target, target, constant
        Emit32((DWORD)(0x16000000 | ((constant>>32) & 0xFFFFF)<<5 | target));      // lu32i.d  target, constant>>32

        if ((constant>>32) & 0x80000)
        {
            Emit32((DWORD)(0x00F30000 | target<<5 | target));  // bstrpick.d  target, target ,51 ,0
        }
    }
    else
    {
        Emit32((DWORD)(0x14000000 | ((constant>>12) & 0xFFFFF)<<5 | target));      // lu12i.w  target, constant>>12
        Emit32((DWORD)(0x03800000 | (constant & 0xFFF)<<10 | target<<5 | target)); // ori  target, target, constant
        Emit32((DWORD)(0x16000000 | ((constant>>32) & 0xFFFFF)<<5 | target));      // lu32i.d  target, constant>>32
        Emit32((DWORD)(0x03000000 | ((constant>>52)<<10) | target<<5 | target));   // lu52i.d  target, target, constant>>52
    }
}

void StubLinkerCPU::EmitJumpRegister(IntReg regTarget)
{
    // jirl $r0,$regTarget,0
    Emit32(0x4c000000 | (regTarget.reg << 5));
}

void StubLinkerCPU::EmitLoadStoreRegPairImm(DWORD flags, IntReg Rt1, IntReg Rt2, IntReg Rn, int offset)
{
    EmitLoadStoreRegPairImm(flags, (int)Rt1, (int)Rt2, Rn, offset, FALSE);
}

void StubLinkerCPU::EmitLoadStoreRegPairImm(DWORD flags, VecReg Vt1, VecReg Vt2, IntReg Rn, int offset)
{
    EmitLoadStoreRegPairImm(flags, (int)Vt1, (int)Vt2, Rn, offset, TRUE);
}

void StubLinkerCPU::EmitLoadStoreRegPairImm(DWORD flags, int regNum1, int regNum2, IntReg Rn, int offset, BOOL isVec)
{
    _ASSERTE(isVec == FALSE); // TODO: VecReg not supported yet
    _ASSERTE((-2048 <= offset) && (offset < 2047));
    _ASSERTE((offset & 7) == 0);

    BOOL isLoad = flags & 1;
    if (isLoad) {
        // ld.d(regNum1, Rn, offset);
        Emit32(emitIns_O_R_R_I(0xa3, regNum1, Rn, offset));
        // ld.d(regNum2, Rn, offset + 8);
        Emit32(emitIns_O_R_R_I(0xa3, regNum2, Rn, offset + 8));
    } else {
        // st.d(regNum1, Rn, offset);
        Emit32(emitIns_O_R_R_I(0xa7, regNum1, Rn, offset));
        // st.d(regNum2, Rn, offset + 8);
        Emit32(emitIns_O_R_R_I(0xa7, regNum2, Rn, offset + 8));
    }
}

void StubLinkerCPU::EmitLoadStoreRegImm(DWORD flags, IntReg Rt, IntReg Rn, int offset, int log2Size)
{
    EmitLoadStoreRegImm(flags, (int)Rt, Rn, offset, FALSE, log2Size);
}

void StubLinkerCPU::EmitFloatLoadStoreRegImm(DWORD flags, FloatReg Ft, IntReg Rn, int offset)
{
    BOOL isLoad    = flags & 1;
    if (isLoad) {
        // fld.d(Ft, Rn, offset);
        Emit32(emitIns_O_R_R_I(0xae, (int)Ft & 0x1f, Rn, offset));
    } else {
        // fst.d(Ft, Rn, offset);
        Emit32(emitIns_O_R_R_I(0xaf, (int)Ft & 0x1f, Rn, offset));
    }
}

void StubLinkerCPU::EmitLoadStoreRegImm(DWORD flags, int regNum, IntReg Rn, int offset, BOOL isVec, int log2Size)
{
    _ASSERTE((log2Size & ~0x3ULL) == 0);

    _ASSERTE(isVec == FALSE); // TODO: VecReg not supported yet
    BOOL isLoad    = flags & 1;
    if (isLoad) {
        // ld.d(regNum, Rn, offset);
        Emit32(emitIns_O_R_R_I(0xa3, regNum, Rn, offset));
    } else {
        // st.d(regNum, Rn, offset);
        Emit32(emitIns_O_R_R_I(0xa7, regNum, Rn, offset));
    }
}

void StubLinkerCPU::EmitLoadFloatRegImm(FloatReg ft, IntReg base, int offset)
{
    // fld.d ft,base,offset
    _ASSERTE(offset <= 2047 && offset >= -2048);
    Emit32(0x2b800000 | (base.reg << 5) | ((offset & 0xfff)<<10) | ft.reg);
}

void StubLinkerCPU::EmitMovReg(IntReg Rd, IntReg Rm)
{
    // ori(Rd, Rm, 0);
    Emit32(0x03800000 | (Rm.reg << 5) | Rd.reg);
}

void StubLinkerCPU::EmitMovFloatReg(FloatReg Fd, FloatReg Fs)
{
    // fmov.d fd, fs
    Emit32(0x01149800 | Fd.reg | (Fs.reg << 5));
}

void StubLinkerCPU::EmitSubImm(IntReg Rd, IntReg Rn, unsigned int value)
{
    _ASSERTE(value <= 2047);
    int tmp_value = -(int)value;
    // addi.d(Rd, Rn, -value);
    Emit32(0x02c00000 | (Rn.reg << 5) | Rd.reg | ((tmp_value & 0xfff)<<10));
}

void StubLinkerCPU::EmitAddImm(IntReg Rd, IntReg Rn, unsigned int value)
{
    _ASSERTE(value <= 2047);
    // addi.d(Rd, Rn, value);
    Emit32(0x02c00000 | (Rn.reg << 5) | Rd.reg | ((value & 0xfff)<<10));
}

void StubLinkerCPU::Init()
{
    new (gConditionalBranchIF) ConditionalBranchInstructionFormat();
    new (gBranchIF) BranchInstructionFormat();
    //new (gLoadFromLabelIF) LoadFromLabelInstructionFormat();
}

// Emits code to adjust arguments for static delegate target.
VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    // On entry a0 holds the delegate instance. Look up the real target address stored in the MethodPtrAux
    // field and saved in $r21. Tailcall to the target method after re-arranging the arguments
    // ld.d  $r21, $r4, offsetof(DelegateObject, _methodPtrAux)
    EmitLoadStoreRegImm(eLOAD, IntReg(21)/*$r21*/, IntReg(4)/*$a0*/, DelegateObject::GetOffsetOfMethodPtrAux());
    // addi.d  t8, a0, DelegateObject::GetOffsetOfMethodPtrAux() - load the indirection cell into t8 used by ResolveWorkerAsmStub
    EmitAddImm(20/*$t8*/, 4, DelegateObject::GetOffsetOfMethodPtrAux());

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
                // FirstFloatReg is 0;
                int j = 1;
                while (pEntry[j].srcofs & ShuffleEntry::FPREGMASK)
                {
                    j++;
                }
                assert((pEntry->dstofs - pEntry->srcofs) == index);
                assert(8 > index);

                int tmp_reg = 11;//f11.
                ShuffleEntry* tmp_entry = pShuffleEntryArray + delay_index[0];
                while (index)
                {
                    // fld.d/s(Ft, sp, offset);
                    Emit32(emitIns_O_R_R_I(0xae, tmp_reg++, 3/*sp*/, tmp_entry->srcofs << 3));

                    index--;
                    tmp_entry++;
                }

                j -= 1;
                tmp_entry = pEntry + j;
                i += j;
                while (pEntry[j].srcofs & ShuffleEntry::FPREGMASK)
                {
                    if (pEntry[j].dstofs & ShuffleEntry::FPREGMASK)// fmov.d fd, fs
                        Emit32(0x01149800 | (pEntry[j].dstofs & ShuffleEntry::OFSREGMASK) | ((pEntry[j].srcofs & ShuffleEntry::OFSREGMASK) << 5));
                    else //// fst.d(Ft, Rn, offset);
                        Emit32(emitIns_O_R_R_I(0xaf, (pEntry[j].srcofs & ShuffleEntry::OFSREGMASK), 3, pEntry[j].dstofs * sizeof(long)));
                    j--;
                }
                while (tmp_reg > 11)
                {
                    tmp_reg--;
                    // fmov.d fd, fs
                    Emit32(0x01149800 | index | (tmp_reg << 5));
                    index++;
                }
                index = 0;
                pEntry = tmp_entry;
            }
            else
            {
                // 4 is the offset of FirstGenArgReg to FirstGenReg
                assert(pEntry->dstofs & ShuffleEntry::REGMASK);
                assert((pEntry->dstofs & ShuffleEntry::OFSMASK) < (pEntry->srcofs & ShuffleEntry::OFSMASK));
                EmitMovReg(IntReg((pEntry->dstofs & ShuffleEntry::OFSMASK) + 4), IntReg((pEntry->srcofs & ShuffleEntry::OFSMASK) + 4));
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
                EmitLoadFloatRegImm(FloatReg((pEntry->dstofs & ShuffleEntry::OFSREGMASK)), RegSp, pEntry->srcofs * sizeof(void*));
            }
            else
            {
                assert(pEntry->dstofs & ShuffleEntry::REGMASK);
                EmitLoadStoreRegImm(eLOAD, IntReg((pEntry->dstofs & ShuffleEntry::OFSMASK) + 4), RegSp, pEntry->srcofs * sizeof(void*));
            }
        }
        else
        {
            // source must be on the stack
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));

            // dest must be on the stack
            _ASSERTE(!(pEntry->dstofs & ShuffleEntry::REGMASK));

            EmitLoadStoreRegImm(eLOAD, IntReg(16)/*t4*/, RegSp, pEntry->srcofs * sizeof(void*));
            EmitLoadStoreRegImm(eSTORE, IntReg(16)/*t4*/, RegSp, pEntry->dstofs * sizeof(void*));
        }
    }

    // Tailcall to target
    // jirl  $r0,$r21,0
    EmitJumpRegister(21);
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
        regHidden += 4;//NOTE: LOONGARCH64 should start at a0=4;

        if (extraArg == NULL)
        {
            if (pSharedMD->RequiresInstMethodTableArg())
            {
                // Unboxing stub case
                // Fill param arg with methodtable of this pointer
                // ld.d regHidden, a0, 0
                EmitLoadStoreRegImm(eLOAD, IntReg(regHidden), IntReg(4), 0);
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
        //  addi.d a0, a0, sizeof(MethodDesc*)
        EmitAddImm(IntReg(4), IntReg(4), sizeof(MethodDesc*));
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
    ClrFlushInstructionCache(pStartRX, cbAligned); \
    return (PCODE)pStartRX

PCODE DynamicHelpers::CreateHelper(LoaderAllocator * pAllocator, TADDR arg, PCODE target)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(32);

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x28c042a4;// ld.d  $a0,$r21,16
    p += 4;
    *(DWORD*)p = 0x28c062b5;// ld.d  $r21,$r21,24
    p += 4;
    *(DWORD*)p = 0x4c0002a0;// jirl  $r0,$r21,0
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

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x28c042a5;// ld.d  $a1,$r21,16
    p += 4;
    *(DWORD*)p = 0x28c062b5;// ld.d  $r21,$r21,24
    p += 4;
    *(DWORD*)p = 0x4c0002a0;// jirl  $r0,$r21,0
    p += 4;

    _ASSERTE(!((uintptr_t)p & 0x7));

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

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x28c062a4;// ld.d  $a0,$r21,24
    p += 4;
    *(DWORD*)p = 0x28c082a5;// ld.d  $a1,$r21,32
    p += 4;
    *(DWORD*)p = 0x28c0a2b5;// ld.d  $r21,$r21,40
    p += 4;
    *(DWORD*)p = 0x4c0002a0;// jirl  $r0,$r21,0
    p += 4;

    // nop, padding to make 8 byte aligned
    *(DWORD*)p = 0x03400000;
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

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x03800085;// ori  $a1,$a0,0
    p += 4;
    *(DWORD*)p = 0x28c062a4;// ld.d  $a0,$r21,24
    p += 4;
    *(DWORD*)p = 0x28c082b5;// ld.d  $r21,$r21,32
    p += 4;
    *(DWORD*)p = 0x4c0002a0;// jirl  $r0,$r21,0
    p += 4;

    // nop, padding to make 8 byte aligned
    *(DWORD*)p = 0x03400000;
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

    *(DWORD*)p = 0x4c000020;// jirl  $r0,$ra,0
    p += 4;

    END_DYNAMIC_HELPER_EMIT();
}

PCODE DynamicHelpers::CreateReturnConst(LoaderAllocator * pAllocator, TADDR arg)
{
    STANDARD_VM_CONTRACT;

    BEGIN_DYNAMIC_HELPER_EMIT(24);

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x28c042a4;// ld.d  $a0,$r21,16
    p += 4;
    *(DWORD*)p = 0x4c000020;// jirl  $r0,$ra,0
    p += 4;
    *(DWORD*)p = 0x03400000;// nop, padding to make 8 byte aligned
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

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x28c062a4;// ld.d  $a0,$r21,24
    p += 4;
    *(DWORD*)p = 0x28c00084;// ld.d  $a0,$a0,0
    p += 4;
    *(DWORD*)p = 0x02c00084 | ((offset & 0xfff)<<10);// addi.d  $a0,$a0,offset
    p += 4;
    *(DWORD*)p = 0x4c000020;// jirl  $r0,$ra,0
    p += 4;
    *(DWORD*)p = 0x03400000;// nop, padding to make 8 byte aligned
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

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x28c042a6;// ld.d  $a2,$r21,16
    p += 4;
    *(DWORD*)p = 0x28c062b5;// ld.d  $r21,$r21,24
    p += 4;
    *(DWORD*)p = 0x4c0002a0;// jirl  $r0,$r21,0
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

    *(DWORD*)p = 0x18000015;// pcaddi  $r21,0
    p += 4;
    *(DWORD*)p = 0x28c062a6;// ld.d  $a2,$r21,24
    p += 4;
    *(DWORD*)p = 0x28c082a7;// ld.d  $a3,$r21,32
    p += 4;
    *(DWORD*)p = 0x28c0a2b5;// ld.d  $r21,$r21,40
    p += 4;
    *(DWORD*)p = 0x4c0002a0;// jirl  $r0,$r21,0
    p += 4;
    *(DWORD*)p = 0xffffff0f;// badcode, padding to make 8 byte aligned
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
            codeSize += 4;
        }

        for (WORD i = 0; i < pLookup->indirections; i++) {
            _ASSERTE(pLookup->offsets[i] >= 0);
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                // if( > 2047) (4*5 bytes) else 4*4 bytes for instructions.
                codeSize += (pLookup->sizeOffset > 2047 ? 20 : 16);
                indirectionsDataSize += (pLookup->sizeOffset > 2047 ? 4 : 0);
            }

            // if( > 2047) (8 bytes) else 4 bytes for instructions.
            codeSize += (pLookup->offsets[i] > 2047 ? 8 : 4);
            indirectionsDataSize += (pLookup->offsets[i] > 2047 ? 4 : 0); // 4 bytes for storing indirection offset values
        }

        codeSize += indirectionsDataSize ? 4 : 0; // pcaddi

        if (pLookup->testForNull)
        {
            codeSize += 12; // ori-beq-jr

            //padding for 8-byte align (required by EmitHelperWithArg)
            if (codeSize & 0x7)
                codeSize += 4;

            codeSize += 32; // size of EmitHelperWithArg
        }
        else
        {
            codeSize += 4; /* jilr */
        }

        // the offset value of data_label.
        uint dataOffset = codeSize;

        codeSize += indirectionsDataSize;

        BEGIN_DYNAMIC_HELPER_EMIT(codeSize);

        BYTE * old_p = p;

        if (indirectionsDataSize)
        {
            _ASSERTE(indirectionsDataSize < 2047);
            _ASSERTE(dataOffset < 0x80000);

            // get the first dataOffset's addr.
            // pcaddi  $r21,0
            *(DWORD*)p = 0x18000015 | (dataOffset << 3); // dataOffset is 4byte aligned.
            p += 4;
            dataOffset = 0;
        }

        if (pLookup->testForNull || pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
        {
            // ori  $t3,$a0,0
            *(DWORD*)p = 0x0380008f;
            p += 4;
        }

        BYTE* pBLECall = NULL;

        for (WORD i = 0; i < pLookup->indirections; i++)
        {
            if (i == pLookup->indirections - 1 && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                _ASSERTE(pLookup->testForNull && i > 0);

                if (pLookup->sizeOffset > 2047)
                {
                    // ld.wu  $t4,$r21,0
                    *(DWORD*)p = 0x2a8002b0 | (dataOffset << 10); p += 4;
                    // ldx.d  $t5,$a0,$t4
                    *(DWORD*)p = 0x380c4091; p += 4;

                    // move to next indirection offset data
                    dataOffset += 4;
                }
                else
                {
                    // ld.d $t5, $a0, #(pLookup->sizeOffset)
                    *(DWORD*)p = 0x28c00091 | ((UINT32)pLookup->sizeOffset << 10); p += 4;
                }

                // lu12i.w $t4, (slotOffset&0xfffff000)>>12
                *(DWORD*)p = 0x14000010 | ((((UINT32)slotOffset & 0xfffff000) >> 12) << 5); p += 4;
                // ori $t4, $t4, slotOffset&0xfff
                *(DWORD*)p = 0x03800210 | (((UINT32)slotOffset & 0xfff) << 10); p += 4;

                // bge $t4,$t5, // CALL HELPER:
                pBLECall = p;       // Offset filled later
                *(DWORD*)p = 0x64000211; p += 4;
            }

            if(pLookup->offsets[i] > 2047)
            {
                // ld.wu  $t4,$r21,0
                *(DWORD*)p = 0x2a8002b0 | (dataOffset << 10);
                p += 4;
                // ldx.d  $a0,$a0,$t4
                *(DWORD*)p = 0x380c4084;
                p += 4;

                // move to next indirection offset data
                dataOffset += 4;
            }
            else
            {
                // offset must be 8 byte aligned
                _ASSERTE((pLookup->offsets[i] & 0x7) == 0);

                // ld.d  $a0,$a0,pLookup->offsets[i]
                *(DWORD*)p = 0x28c00084 | ((pLookup->offsets[i] & 0xfff)<<10);
                p += 4;
            }
        }

        _ASSERTE((codeSize == dataOffset) || (indirectionsDataSize == dataOffset));

        // No null test required
        if (!pLookup->testForNull)
        {
            _ASSERTE(pLookup->sizeOffset == CORINFO_NO_SIZE_CHECK);
            // jirl  $r0,$ra,0
            *(DWORD*)p = 0x4c000020;
            p += 4;
        }
        else
        {
            // beq $a0,$zero, // CALL HELPER:
            *(DWORD*)p = 0x58000880;
            p += 4;

            // jirl  $r0,$ra,0
            *(DWORD*)p = 0x4c000020;
            p += 4;

            // CALL HELPER:
            if(pBLECall != NULL)
                *(DWORD*)pBLECall |= ((UINT32)(p - pBLECall) << 8);

            // ori  $a0,$t3,0
            *(DWORD*)p = 0x038001e4;
            p += 4;
            if ((uintptr_t)(p - old_p) & 0x7)
            {
                // nop, padding for 8-byte align (required by EmitHelperWithArg)
                *(DWORD*)p = 0x03400000;
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
            if(pLookup->offsets[i] > 2047)
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
