// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "field.h"
#include "stublink.h"

//#include "frames.h"
//#include "excep.h"
//#include "dllimport.h"
//#include "log.h"
#include "comdelegate.h"
//#include "array.h"
//#include "jitinterface.h"
//#include "codeman.h"
//#include "dbginterface.h"
//#include "eeprofinterfaces.h"
//#include "eeconfig.h"
//#include "class.h"
//#include "stublink.inl"


#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------
// InstructionFormat for BRASL.
//-----------------------------------------------------------------------
class S390XCall : public InstructionFormat
{
    public:
        S390XCall ()
            : InstructionFormat(InstructionFormat::k32 | InstructionFormat::k64)
        {
            LIMITED_METHOD_CONTRACT;
        }

        virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
        {
            LIMITED_METHOD_CONTRACT;

            switch (refsize)
            {
            case k32:
                return 6;

            case k64:
                return 6 + 6 + 2;

            default:
                _ASSERTE(!"unexpected refsize");
                return 0;
            }
        }

        virtual VOID EmitInstruction(UINT refsize, int64_t fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
            LIMITED_METHOD_CONTRACT

            switch (refsize)
            {
#if 0
            case k32:
                pOutBufferRW[0] = 0xC0;
                pOutBufferRW[1] = 0xE5;
                *((__int32*)(2+pOutBufferRW)) = (__int32)(fixedUpReference >> 1);
                break;
#endif

            case k64:
            {
                UINT64 target = (UINT64)(((INT64)pOutBufferRX) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));

                // llilf  %r14,LO(target)
                pOutBufferRW[0] = 0xC0;
                pOutBufferRW[1] = 0xEF;
                *((UINT32*)&pOutBufferRW[2]) = (UINT32)(target);

                // iihf  %r14,HI(target)
                pOutBufferRW[6] = 0xC0;
                pOutBufferRW[7] = 0xE8;
                *((UINT32*)&pOutBufferRW[8]) = (UINT32)(target >> 32);

                // basr %r14,%r14
                pOutBufferRW[12] = 0x0D;
                pOutBufferRW[13] = 0xEE;
                break;
            }

            default:
                _ASSERTE(!"unreached");
                break;
            }
        }

        virtual BOOL CanReach(UINT refsize, UINT variationCode, BOOL fExternal, INT_PTR offset)
        {
            if (fExternal)
            {
                switch (refsize)
                {
                case InstructionFormat::k32:
                    // For external, we don't have enough info to predict
                    // the offset.
                    return FALSE;

                case InstructionFormat::k64:
                    // intentional fallthru
                case InstructionFormat::kAllowAlways:
                    return TRUE;

                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
            else
            {
                switch (refsize)
                {
                case InstructionFormat::k32:
                    return FitsInI4(offset >> 1);

                case InstructionFormat::k64:
                    // intentional fallthru
                case InstructionFormat::kAllowAlways:
                    return TRUE;
                default:
                    _ASSERTE(0);
                    return FALSE;
                }
            }
        }
};


#if 0
static BYTE gX86NearJump[sizeof(X86NearJump)];
static BYTE gX86CondJump[sizeof(X86CondJump)];
static BYTE gX86PushImm32[sizeof(X86PushImm32)];
#endif
static BYTE gS390XCall[sizeof(S390XCall)];


/* static */ void StubLinkerCPU::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
#if 0
    new (gX86NearJump) X86NearJump();
    new (gX86CondJump) X86CondJump( InstructionFormat::k8|InstructionFormat::k32);
    new (gX86PushImm32) X86PushImm32(InstructionFormat::k32);
#endif
    new (gS390XCall) S390XCall();
}

//---------------------------------------------------------------
// Emits:
//    bcr M1, R2
//---------------------------------------------------------------
void StubLinkerCPU::EmitBranchOnConditionRegister(CondMask M1, IntReg R2)
{
    STANDARD_VM_CONTRACT;

    Emit16((WORD) (0x0700 | (M1 << 4) | R2));
}
void StubLinkerCPU::EmitBranchRegister(IntReg R2)
{
  EmitBranchOnConditionRegister(CondMask(15), R2);
}

//---------------------------------------------------------------
// Emits:
//    lgr R1, R2
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadRegister(IntReg R1, IntReg R2)
{
    STANDARD_VM_CONTRACT;

    Emit32((DWORD) (0xB9040000 | (R1 << 4) | R2));
}

//---------------------------------------------------------------
// Emits:
//    lghi R1, I2
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadHalfwordImmediate(IntReg R1, int I2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((-32768 <= I2) && (I2 < 32768));
    Emit32((DWORD) (0xA7090000 | (R1 << 20) | (I2 & 0xffff)));
}

//---------------------------------------------------------------
// Emits:
//    llilf R1, I2
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadLogicalImmediateLow(IntReg R1, DWORD I2)
{
    STANDARD_VM_CONTRACT;

    Emit16((WORD) (0xC00F | (R1 << 4)));
    Emit32(I2);
}

//---------------------------------------------------------------
// Emits:
//    llihf R1, I2
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadLogicalImmediateHigh(IntReg R1, DWORD I2)
{
    STANDARD_VM_CONTRACT;

    Emit16((WORD) (0xC00E | (R1 << 4)));
    Emit32(I2);
}

//---------------------------------------------------------------
// Emits:
//    iilf R1, I2
//---------------------------------------------------------------
void StubLinkerCPU::EmitInsertImmediateLow(IntReg R1, DWORD I2)
{
    STANDARD_VM_CONTRACT;

    Emit16((WORD) (0xC009 | (R1 << 4)));
    Emit32(I2);
}

//---------------------------------------------------------------
// Emits:
//    iihf R1, I2
//---------------------------------------------------------------
void StubLinkerCPU::EmitInsertImmediateHigh(IntReg R1, DWORD I2)
{
    STANDARD_VM_CONTRACT;

    Emit16((WORD) (0xC008 | (R1 << 4)));
    Emit32(I2);
}

//---------------------------------------------------------------
// Emits:
//    lay R1, D2(X2,B2)
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadAddress(IntReg R1, int D2, IntReg X2, IntReg B2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((-524288 <= D2) && (D2 < 524288));
    int DL2 = D2 & 0xfff;
    int DH2 = (D2 >> 12) & 0xff;

    Emit32((DWORD) (0xE3000000 | (R1 << 20) | (X2 << 16) | (B2 << 12) | DL2));
    Emit16((WORD) (0x0071 | (DH2 << 8)));
}
void StubLinkerCPU::EmitLoadAddress(IntReg R1, int D2, IntReg B2)
{
    EmitLoadAddress(R1, D2, IntReg(0), B2);
}

//---------------------------------------------------------------
// Emits:
//    lg R1, D2(X2,B2)
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoad(IntReg R1, int D2, IntReg X2, IntReg B2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((-524288 <= D2) && (D2 < 524288));
    int DL2 = D2 & 0xfff;
    int DH2 = (D2 >> 12) & 0xff;

    Emit32((DWORD) (0xE3000000 | (R1 << 20) | (X2 << 16) | (B2 << 12) | DL2));
    Emit16((WORD) (0x0004 | (DH2 << 8)));
}
void StubLinkerCPU::EmitLoad(IntReg R1, int D2, IntReg B2)
{
    EmitLoad(R1, D2, IntReg(0), B2);
}

//---------------------------------------------------------------
// Emits:
//    stg R1, D2(X2,B2)
//---------------------------------------------------------------
void StubLinkerCPU::EmitStore(IntReg R1, int D2, IntReg X2, IntReg B2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((-524288 <= D2) && (D2 < 524288));
    int DL2 = D2 & 0xfff;
    int DH2 = (D2 >> 12) & 0xff;

    Emit32((DWORD) (0xE3000000 | (R1 << 20) | (X2 << 16) | (B2 << 12) | DL2));
    Emit16((WORD) (0x0024 | (DH2 << 8)));
}
void StubLinkerCPU::EmitStore(IntReg R1, int D2, IntReg B2)
{
    EmitStore(R1, D2, IntReg(0), B2);
}

//---------------------------------------------------------------
// Emits:
//    std R1, D2(X2,B2)
//---------------------------------------------------------------
void StubLinkerCPU::EmitStoreFloat(VecReg R1, int D2, IntReg X2, IntReg B2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((0 <= D2) && (D2 < 4096));

    Emit32((DWORD) (0x60000000 | (R1 << 20) | (X2 << 16) | (B2 << 12) | D2));
}
void StubLinkerCPU::EmitStoreFloat(VecReg R1, int D2, IntReg B2)
{
    EmitStoreFloat(R1, D2, IntReg(0), B2);
}

//---------------------------------------------------------------
// Emits:
//    lmg R1, R3, D2(B2)
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadMultiple(IntReg R1, IntReg R3, int D2, IntReg B2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((-524288 <= D2) && (D2 < 524288));
    int DL2 = D2 & 0xfff;
    int DH2 = (D2 >> 12) & 0xff;

    Emit32((DWORD) (0xEB000000 | (R1 << 20) | (R3 << 16) | (B2 << 12) | DL2));
    Emit16((WORD) (0x0004 | (DH2 << 8)));
}

//---------------------------------------------------------------
// Emits:
//    stmg R1, R3, D2(B2)
//---------------------------------------------------------------
void StubLinkerCPU::EmitStoreMultiple(IntReg R1, IntReg R3, int D2, IntReg B2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((-524288 <= D2) && (D2 < 524288));
    int DL2 = D2 & 0xfff;
    int DH2 = (D2 >> 12) & 0xff;

    Emit32((DWORD) (0xEB000000 | (R1 << 20) | (R3 << 16) | (B2 << 12) | DL2));
    Emit16((WORD) (0x0024 | (DH2 << 8)));
}

//---------------------------------------------------------------
// Emits load of an immediate 64-bit constant.
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadImmediate(IntReg target, UINT64 constant)
{
    if ((INT64)constant == (((INT64)constant & 0xffff) ^ 0x8000) - 0x8000)
    {
      EmitLoadHalfwordImmediate(target, (int) constant);
    }
    else
    {
      DWORD LowPart = (DWORD) constant;
      DWORD HighPart = (DWORD) (constant >> 32);

      EmitLoadLogicalImmediateLow(target, LowPart);
      if (HighPart)
          EmitInsertImmediateHigh(target, HighPart);
    }
}

//---------------------------------------------------------------
// Emits code to save incoming arguments to the save area
//---------------------------------------------------------------
void StubLinkerCPU::EmitSaveIncomingArguments(unsigned int cIntRegArgs, unsigned int cFloatRegArgs)
{
    _ASSERTE(cIntRegArgs <= 5);
    _ASSERTE(cFloatRegArgs <= 4);

    // Store integer argument registers and call-saved registers
    EmitStoreMultiple(IntReg(2), IntReg(15), 2*8, IntReg(15));

    // Store floating-point argument registers
    for (int i = 0; i < cFloatRegArgs; i++)
    {
      EmitStoreFloat(VecReg(2*i), (16+i)*8, IntReg(15));
    }
}

#if 0
//---------------------------------------------------------------
// Emits Prolog
//---------------------------------------------------------------
void StubLinkerCPU::EmitProlog(unsigned short cIntRegArgs, unsigned short cVecRegArgs, unsigned short cCalleeSavedRegs, unsigned short cbStackSpace)
{
    STANDARD_VM_CONTRACT;

    // Store argument registers and call-saved registers
    EmitStoreMultiple(IntReg(2), IntReg(15), 2*8, IntReg(15));

    EmitLoadRegister(IntReg(1), IntReg(15));

    // Allocate stack space
    EmitLoadAddress(IntReg(15), -160, IntReg(15));

    EmitStore(IntReg(1), 0, IntReg(15));
}

//---------------------------------------------------------------
// Emits Epilog
//---------------------------------------------------------------
void StubLinkerCPU::EmitEpilog()
{
    STANDARD_VM_CONTRACT;

    // Restore call-saved registers registers
    EmitLoadMultiple(IntReg(6), IntReg(15), 160 + 6*8, IntReg(15));

    // Return
    EmitBranchRegister(IntReg(14));
}

//---------------------------------------------------------------
// Emits code to load the address of the incoming save area
//---------------------------------------------------------------
void StubLinkerCPU::EmitLoadIncomingSaveArea(IntReg target)
{
    EmitLoadAddress(target, 160, IntReg(15));
}
#endif

//---------------------------------------------------------------
// Emits code to call a label
//---------------------------------------------------------------
void StubLinkerCPU::EmitCallLabel(CodeLabel *target)
{
    EmitLabelRef(target, reinterpret_cast<S390XCall&>(gS390XCall), 0);
}


VOID StubLinkerCPU::EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg)
{
    STANDARD_VM_CONTRACT;

    LOG((LF_CORDB, LL_INFO100, "SL::ECIMS: pSharedMD:%p extraArg:%p\n", pSharedMD, extraArg));

    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++)
    {
        _ASSERTE((pEntry->srcofs & ShuffleEntry::REGMASK) && (pEntry->dstofs & ShuffleEntry::REGMASK));
        // Source in a general purpose or float register, destination in the same kind of a register or on stack
        int srcRegIndex = pEntry->srcofs & ShuffleEntry::OFSREGMASK;

        // Both the srcofs and dstofs must be of the same kind of registers - float or general purpose.
        _ASSERTE((pEntry->dstofs & ShuffleEntry::FPREGMASK) == (pEntry->srcofs & ShuffleEntry::FPREGMASK));
        int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;

        if (pEntry->srcofs & ShuffleEntry::FPREGMASK)
        {
            _ASSERTE(!"S390X:NYI");
            // X64EmitMovXmmXmm((X86Reg)(kXMM0 + dstRegIndex), (X86Reg)(kXMM0 + srcRegIndex));
        }
        else
        {
            EmitLoadRegister(IntReg(2 + dstRegIndex), IntReg(2 + srcRegIndex));
        }
    }

    MetaSig msig(pSharedMD);
    ArgIterator argit(&msig);

    if (argit.HasParamType())
    {
        int paramTypeArgOffset = argit.GetParamTypeArgOffset();
        int paramTypeArgIndex = TransitionBlock::GetArgumentIndexFromOffset(paramTypeArgOffset);

        if (extraArg == NULL)
        {
            if (pSharedMD->RequiresInstMethodTableArg())
            {
                // Unboxing stub case
                // Extract MethodTable pointer (the hidden arg) from the object instance.
                EmitLoad(IntReg(2 + paramTypeArgIndex), 0, IntReg(THIS_kREG));
                LOG((LF_CORDB, LL_INFO100, "SL::ECIMS: param/unbox reg:%d\n", 2 + paramTypeArgIndex));
            }
        }
        else
        {
            EmitLoadImmediate(IntReg(2 + paramTypeArgIndex), (UINT_PTR)extraArg);
            LOG((LF_CORDB, LL_INFO100, "SL::ECIMS: param/extra reg:%d\n", 2 + paramTypeArgIndex));
        }
    }

    if (extraArg == NULL)
    {
        // Unboxing stub case
        // Skip over the MethodTable* to find the address of the unboxed value type.
        EmitLoadAddress(IntReg(THIS_kREG), sizeof(void*), IntReg(THIS_kREG));
    }

    PCODE multiCallableAddr = pSharedMD->TryGetMultiCallableAddrOfCode(CORINFO_ACCESS_PREFER_SLOT_OVER_TEMPORARY_ENTRYPOINT);

    // Use direct call if possible.
    if (multiCallableAddr != (PCODE)NULL)
    {
        EmitLoadImmediate(IntReg(1), (UINT_PTR)multiCallableAddr);
    }
    else
    {
        EmitLoadImmediate(IntReg(1), (UINT_PTR)pSharedMD->GetAddrOfSlot());
        EmitLoad(IntReg(1), 0, IntReg(1));
    }
    EmitBranchRegister(IntReg(1));

    SetTargetMethod(pSharedMD);
}

VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    STANDARD_VM_CONTRACT;

    // On entry THIS_kREG holds the delegate instance. Look up the real target address stored in the MethodPtrAux
    // field and save it in %r1. Tailcall to the target method after re-arranging the arguments
    // lg %r1, #offsetof(DelegateObject, _methodPtrAux)(THIS_kREG)
    EmitLoad(IntReg(1), DelegateObject::GetOffsetOfMethodPtrAux(), IntReg(THIS_kREG));
    // Load the indirection cell into 8(%r15) used by ResolveWorkerAsmStub
    // lay %r0, #offsetof(DelegateObject, _methodPtrAux)(THIS_kREG)
    // stg %r0, 8(%r15)
    EmitLoadAddress(IntReg(0), DelegateObject::GetOffsetOfMethodPtrAux(), IntReg(THIS_kREG));
    EmitStore(IntReg(0), 8, IntReg(15));

    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++)
    {
        if (pEntry->srcofs == ShuffleEntry::HELPERREG)
        {
            _ASSERTE(!"S390X:NYI");
        }
        else if (pEntry->dstofs == ShuffleEntry::HELPERREG)
        {
            _ASSERTE(!"S390X:NYI");
        }
        else if (pEntry->srcofs & ShuffleEntry::REGMASK)
        {
            // Source in a general purpose or float register, destination in the same kind of a register or on stack
            int srcRegIndex = pEntry->srcofs & ShuffleEntry::OFSREGMASK;

            if (pEntry->dstofs & ShuffleEntry::REGMASK)
            {
                // Source in register, destination in register

                // Both the srcofs and dstofs must be of the same kind of registers - float or general purpose.
                _ASSERTE((pEntry->dstofs & ShuffleEntry::FPREGMASK) == (pEntry->srcofs & ShuffleEntry::FPREGMASK));
                int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;

                if (pEntry->srcofs & ShuffleEntry::FPREGMASK)
                {
                    _ASSERTE(!"S390X:NYI");
                }
                else
                {
                    EmitLoadRegister(IntReg(2 + dstRegIndex), IntReg(2 + srcRegIndex));
                }
            }
            else
            {
                // Source in register, destination on stack
                int dstOffset = 160 + pEntry->dstofs * sizeof(void*);

                if (pEntry->srcofs & ShuffleEntry::FPREGMASK)
                {
                    _ASSERTE(!"S390X:NYI");
                }
                else
                {
                    EmitStore(IntReg(2 + srcRegIndex), dstOffset, IntReg(15));
                }
            }
        }
        else if (pEntry->dstofs & ShuffleEntry::REGMASK)
        {
            // Source on stack, destination in register
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));

            int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;
            int srcOffset = 160 + pEntry->srcofs * sizeof(void*);

            if (pEntry->dstofs & ShuffleEntry::FPREGMASK)
            {
                _ASSERTE(!"S390X:NYI");
            }
            else
            {
                EmitLoad(IntReg(2 + dstRegIndex), srcOffset, IntReg(15));
            }
        }
        else
        {
            _ASSERTE(!"S390X:NYI");
        }
    }

    // Tailcall to target
    EmitBranchRegister(IntReg(1));
}

#endif // !DACCESS_COMPILE
