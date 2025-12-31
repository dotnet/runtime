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

class PPC64LECall : public InstructionFormat
{
public:
	PPC64LECall (): InstructionFormat(InstructionFormat::k64)
	{
	    LIMITED_METHOD_CONTRACT;
	}

    enum VariationCodes
    {
        DIRECT_TAILCALL           = 0x00000001,    // Direct jump and bcctr
        DIRECT_NON_TAILCALL       = 0x00000002,    // Direct jump and bcctrl

        INDIRECT_TAILCALL         = 0x00000000,    // Indirect jump using ld and bcctr
        INDIRECT_NON_TAILCALL     = 0x00000003     // Indirect jump using ld bcctrl
    };

	virtual UINT GetSizeOfInstruction(UINT refsize, UINT variationCode)
	{
            LIMITED_METHOD_CONTRACT;

            _ASSERTE(refsize == InstructionFormat::k64);

	    if(variationCode == INDIRECT_TAILCALL || variationCode == INDIRECT_NON_TAILCALL)
		    return 32;
	    else
            return 28;
	}

	virtual VOID EmitInstruction(UINT refsize, int64_t fixedUpReference, BYTE *pOutBufferRX, BYTE *pOutBufferRW, UINT variationCode, BYTE *pDataBuffer)
        {
	    UINT64 target = (UINT64)(((INT64)pOutBufferRX) + fixedUpReference + GetSizeOfInstruction(refsize, variationCode));

            // lis r12, <target>
            *((UINT64*)&pOutBufferRW[0]) = ((UINT64)(target) >> 48 & 0xff);
            *((UINT64*)&pOutBufferRW[1]) = ((UINT64)(target) >> 56 & 0xff);
            pOutBufferRW[2] = 0x80;
            pOutBufferRW[3] = 0x3D;

            // ori r12, r12, <target>
            *((UINT64*)&pOutBufferRW[4]) = ((UINT64)(target) >> 32) & 0xff;
            *((UINT64*)&pOutBufferRW[5]) = ((UINT64)(target) >> 40) & 0xff;
            pOutBufferRW[6] = 0x8C;
            pOutBufferRW[7] = 0x61;

            // sldi r12, r12, 32
            pOutBufferRW[8] = 0xC6;
            pOutBufferRW[9] = 0x07;
            pOutBufferRW[10] = 0x8C;
            pOutBufferRW[11] = 0x79;

            // oris r12, r12, <target>
            *((UINT64*)&pOutBufferRW[12]) = ((UINT64)(target) >> 16) & 0xff;
            *((UINT64*)&pOutBufferRW[13]) = ((UINT64)(target) >> 24) & 0xff;
            pOutBufferRW[14] = 0x8C;
            pOutBufferRW[15] = 0x65;

            // ori r12, r12, <target>
            *((UINT64*)&pOutBufferRW[16]) = ((UINT64)(target) >> 0) & 0xff;
            *((UINT64*)&pOutBufferRW[17]) = ((UINT64)(target) >> 8) & 0xff;
            pOutBufferRW[18] = 0x8C;
            pOutBufferRW[19] = 0x61;

	    switch (variationCode)
	    {
                case DIRECT_TAILCALL:
                    // mtctr r12
                    pOutBufferRW[20] = 0xA6;
                    pOutBufferRW[21] = 0x03;
                    pOutBufferRW[22] = 0x89;
                    pOutBufferRW[23] = 0x7D;

                    // bcctrl
                    pOutBufferRW[24] = 0x20;
                    pOutBufferRW[25] = 0x04;
                    pOutBufferRW[26] = 0x80;
                    pOutBufferRW[27] = 0x4E;
                break;

                case DIRECT_NON_TAILCALL:
                    // mtctr r12
                    pOutBufferRW[20] = 0xA6;
                    pOutBufferRW[21] = 0x03;
                    pOutBufferRW[22] = 0x89;
                    pOutBufferRW[23] = 0x7D;

                    // bcctrl
                    pOutBufferRW[24] = 0x21;
                    pOutBufferRW[25] = 0x04;
                    pOutBufferRW[26] = 0x81;
                    pOutBufferRW[27] = 0x4E;
                break;

                case INDIRECT_TAILCALL:
                    // ld r12, 0(r12) e9 8c 00 00
                    pOutBufferRW[20] = 0x00;
                    pOutBufferRW[21] = 0x00;
                    pOutBufferRW[22] = 0x8C;
                    pOutBufferRW[23] = 0xE9;

                    // mtctr r12
                    pOutBufferRW[24] = 0xA6;
                    pOutBufferRW[25] = 0x03;
                    pOutBufferRW[26] = 0x89;
                    pOutBufferRW[27] = 0x7D;

                    // bctr 4e800420
                    pOutBufferRW[28] = 0x20;
                    pOutBufferRW[29] = 0x04;
                    pOutBufferRW[30] = 0x80;
                    pOutBufferRW[31] = 0x4E;
                break;

                case INDIRECT_NON_TAILCALL:
                    // ld r12, 0(r12) e9 8c 00 00
                    pOutBufferRW[20] = 0x00;
                    pOutBufferRW[21] = 0x00;
                    pOutBufferRW[22] = 0x8C;
                    pOutBufferRW[23] = 0xE9;

                    // mtctr r12
                    pOutBufferRW[24] = 0xA6;
                    pOutBufferRW[25] = 0x03;
                    pOutBufferRW[26] = 0x89;
                    pOutBufferRW[27] = 0x7D;

                    // bctr 4e800420
                    pOutBufferRW[28] = 0x21;
                    pOutBufferRW[29] = 0x04;
                    pOutBufferRW[30] = 0x81;
                    pOutBufferRW[31] = 0x4E;
                break;
	    }
	}
	/*virtual BOOL CanReach(UINT refsize, UINT variationCode, BOOL fExternal, INT_PTR offset)
	{
            _ASSERTE(refsize == InstructionFormat::k64);

	    if (fExternal)
		return false;

    	    return FitsInI4(offset);
	}*/


};


static BYTE gPPC64LECall[sizeof(PPC64LECall)];

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
     new (gPPC64LECall) PPC64LECall();
}

/*void StubLinkerCPU::EmitBranchOnConditionRegister(CondMask M1, IntReg R2)
{
}*/

// mtctr %r12
// bcctr
void StubLinkerCPU::EmitBranchToCountRegister(IntReg R12, int BO, int BI)
{
    Emit32((DWORD)((31 << 26) | (R12 << 21) | (288 << 11) | (467 << 1)));			//mtctr %r12
    Emit32((DWORD)((19 << 26) | (BO << 21 )| (BI << 16) | (0 << 11) | (528 << 1) | 0));		//bcctr
}

void StubLinkerCPU::EmitLoadHalfwordImmediate(IntReg R1, int I2)
{
}

void StubLinkerCPU::EmitLoadLogicalImmediateLow(IntReg R1, DWORD I2)
{
}

void StubLinkerCPU::EmitLoadLogicalImmediateHigh(IntReg R1, DWORD I2)
{
}

void StubLinkerCPU::EmitInsertImmediateLow(IntReg R1, DWORD I2)
{
}

void StubLinkerCPU::EmitInsertImmediateHigh(IntReg R1, DWORD I2)
{
}

void StubLinkerCPU::EmitLoadAddress(IntReg R1, int D2, IntReg X2, IntReg B2)
{
}

//stdu %r1, -<stack_size>(%r1) 
void StubLinkerCPU::EmitStoreDoubleWordWithUpdate(IntReg R1, int D2, IntReg R2)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((-524288 <= D2) && (D2 < 524288));

    Emit32((DWORD)((62 << 26) | ((R1) << 21) | ((R2) << 16) | (-(D2) & 0xfffc) | 1));
}

void StubLinkerCPU::EmitLoad(IntReg R1, int D2, IntReg X2, IntReg B2)
{
}

void StubLinkerCPU::EmitLoad(IntReg R1, int D2, IntReg B2)
{
}

void StubLinkerCPU::EmitStore(IntReg R1, int D2, IntReg X2, IntReg B2)
{
}

void StubLinkerCPU::EmitStore(IntReg R1, int D2, IntReg B2)
{
}

void StubLinkerCPU::EmitStoreFloat(VecReg R1, int D2, IntReg X2, IntReg B2)
{
}

void StubLinkerCPU::EmitStoreFloat(VecReg R1, int D2, IntReg B2)
{
}

void StubLinkerCPU::EmitLoadMultiple(IntReg R1, IntReg R3, int D2, IntReg B2)
{
}

void StubLinkerCPU::EmitStoreMultiple(IntReg R1, IntReg R3, int D2, IntReg B2)
{
}

void StubLinkerCPU::EmitLoadImmediate(IntReg RA, UINT64 Imm)
{
    if (((Imm >> 15) == 0) || ((Imm >> 15) == -1))
    {
	Emit32 ((DWORD)((14 << 26) | (RA << 21) | (0 << 16) | (Imm & 0xffff))); // li %r, Imm
    }
    else if (((Imm >> 31) == 0) || ((Imm >> 31) == -1))
    {
	Emit32((DWORD)((15 << 26) | (RA << 21) | (0 << 16)  | ((Imm >> 16) & 0xffff)));	// lis %r, Imm
	Emit32((DWORD)((24 << 26) | (RA << 21) | (RA << 16)  | (Imm & 0xffff)));	// ori %r, %r, Imm
    } 
    else if (((Imm >> 47) == 0) || ((Imm >> 47) == -1))
    {
	Emit32 ((DWORD)((14 << 26) | (RA << 21) | (0 << 16) | ((Imm >> 32) & 0xffff)));
	Emit32((DWORD)((30 << 26) | (RA << 21) | (RA << 16) | ((32 & 0x1F) << 11) | (((((31) & 0x1F) << 1) | (((31) >> 5) & 0x1)) << 5) | (1 << 2) | ((((32) >> 5) & 0x1) << 1) | 0));
	Emit32((DWORD)((25 << 26) | (RA << 21) | (RA << 16)  | (((Imm >> 16) & 0xffff) & 0xffff)));
	Emit32((DWORD)((24 << 26) | (RA << 21) | (RA << 16)  | ((Imm & 0xffff) & 0xffff)));
	
    } 
    else 
    {
	Emit32((DWORD)((15 << 26) | (RA << 21) | (0 << 16)  | ((Imm >> 48) & 0xffff)));
	Emit32((DWORD)((24 << 26) | (RA << 21) | (RA << 16)  | ((Imm >> 32) & 0xffff)));	// ori %r, %r, Imm
	Emit32((DWORD)((30 << 26) | (RA << 21) | (RA << 16) | ((Imm & 0x1F) << 11) | ((((63 - Imm) & 0x1F) << 1) | (((63 - Imm) >> 5) & 0x1)) | (1 << 2) | (((Imm >> 5) & 0x1) << 1) | 0 ));
	Emit32((DWORD)((25 << 26) | (RA << 21) | (RA << 16)  | (((Imm >> 16) & 0xffff) & 0xffff)));
	Emit32((DWORD)((24 << 26) | (RA << 21) | (RA << 16)  | ((Imm & 0xffff))));
    }
}

void StubLinkerCPU::EmitSaveLR()
{
    //mflr %r0
    IntReg RA = IntReg(0);
    Emit32((DWORD)((31 << 26) | ((RA) << 21) | (256 << 11) | (339 << 1)));

    //std %r0, 16(%r1)
    EmitStoreDoubleWord(IntReg(0), IntReg(1), 16);

    //std %r2, 24(%r1)
    EmitStoreDoubleWord(IntReg(2), IntReg(1), 24);
}

void StubLinkerCPU::EmitSaveArguments(unsigned int cIntRegArgs, unsigned int cFloatRegArgs)
{
    _ASSERTE(cIntRegArgs <= 8);
    _ASSERTE(cFloatRegArgs <= 13);

//Prolog:    
	//#define ppc_mfspr(c,D,spr) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((spr) << 11) | (339 << 1))
	//#define  ppc_mflr(c,D)     ppc_mfspr  (c, D, ppc_lr)
    //mflr %r0
    IntReg RA = IntReg(0);
    Emit32((DWORD)((31 << 26) | ((RA) << 21) | (256 << 11) | (339 << 1)));

    //std %r0, 16(%r1)
    EmitStoreDoubleWord(IntReg(0), IntReg(1), 16);

    //std %r2, 24(%r1)
    EmitStoreDoubleWord(IntReg(2), IntReg(1), 24);

    //std %r31, -8(%r1)
    //EmitStoreDoubleWord(IntReg(31), IntReg(1), -8);

    //#define ppc_stdu(c,S,ds,A)  ppc_emit32(c, (62 << 26) | ((S) << 21) | ((A) << 16) | ((guint32)(ds) & 0xfffc) | 1)
    //stdu %r1, -496(%r1)
    IntReg RS = IntReg(1);
    Emit32((DWORD)((62 << 26) | ((RS) << 21) | ((RS) << 16) | (-496 & 0xfffc) | 1));

    // Store integer argument registers (r3-r10)
    int disp = 32;
    for (int i=3; i<=10; i++)
    {
    	EmitStoreDoubleWord(IntReg(i), IntReg(1), disp);
	disp = disp + 8;
    }

    // Store float argument registers(f1-f13)
    for (int i=1; i<=13; i++)
    {
    	EmitStoreFloatingPointDouble(VecReg(i), IntReg(1), disp);
	disp = disp + 8;
    }

    disp = disp + 8; //padding

    // Store call-saved registers (r14-r31)
    for (int i=14; i<=31; i++)
    {
    	EmitStoreDoubleWord(IntReg(i), IntReg(1), disp);
	disp = disp + 8;
    }

    // Store floating-point argument registers(f14-f31)
    for (int i=14; i<=31; i++)
    {
    	EmitStoreFloatingPointDouble(VecReg(i), IntReg(1), disp);
	disp = disp + 8;
    }
}
// std %r3, 32(%r1)
void StubLinkerCPU::EmitStoreDoubleWord(IntReg RS, IntReg RA, int DS)
{
    STANDARD_VM_CONTRACT;
    Emit32((DWORD)((62 << 26) | ((RS) << 21) | ((RA) << 16) | DS));
}

// stfd %f1, 256(%r1)
void StubLinkerCPU::EmitStoreFloatingPointDouble(VecReg RS, IntReg RA, int DS)
{
    STANDARD_VM_CONTRACT;
    Emit32((DWORD)((54 << 26) | ((RS) << 21) | ((RA) << 16) | DS));
}

// mr %r3, %r11
void StubLinkerCPU::EmitMoveRegister(IntReg R1, IntReg R2)
{
    EmitMoveRegister(R2,R1,R2);
}

void StubLinkerCPU::EmitMoveRegister(IntReg RS, IntReg RA, IntReg RB)
{
    STANDARD_VM_CONTRACT;

    Emit32((DWORD)((31 << 26) | ((RS) << 21) | ((RA) << 16) | ((RB) << 11) | 888));
}

// ld %r3, 32(%r1)
void StubLinkerCPU::EmitLoadDoubleWord(IntReg RS, int DS, IntReg RA)
{
    STANDARD_VM_CONTRACT;
    Emit32((DWORD)((58 << 26) | ((RS) << 21) | ((RA) << 16) | (DS & 0xfffc) | 0));		//ld %r0, 16(%r1)
}

// lfd %f1, 240(%r1)
void StubLinkerCPU::EmitLoadFloatingPointDouble(VecReg RS, IntReg RA, int DS)
{
    STANDARD_VM_CONTRACT;
    Emit32((DWORD)((50 << 26) | ((RS) << 21) | ((RA) << 16) | DS));
}

//EpiLog
void StubLinkerCPU::EmitRestoreArguments(IntReg R0, IntReg R1)
{
    // Store integer argument registers (r4-r10)
    // ld r4 to r10, r3 contains return value hence not restoring it.
    int disp = 40;
    for (int i=4; i<=10; i++)
    {
    	EmitLoadDoubleWord(IntReg(i), disp, IntReg(1));
	disp = disp + 8;
    }

    // store float arguments (f2-f13)
    // lfd f2 to f31, f1 contains return value hence not restoring it
    disp = disp + 8;
    for (int i=2; i<=13; i++)
    {
    	EmitLoadFloatingPointDouble(VecReg(i), IntReg(1), disp);
	disp = disp + 8;
    }

    disp = disp + 8;  //padding

    // Store callee-saved registers
    // ld r14 to r31
    for (int i=14; i<=31; i++)
    {
    	EmitLoadDoubleWord(IntReg(i), disp, IntReg(1));
	disp = disp + 8;
    }

    // Store float callee-saved registers
    for (int i=14; i<=31; i++)
    {
    	EmitLoadFloatingPointDouble(VecReg(i), IntReg(1), disp);
	disp = disp + 8;
    }

    Emit32((DWORD)((14 << 26) | (R1 << 21) | (R1 << 16) | 496));			//addi %r1, %r1, 496
    EmitLoadDoubleWord(IntReg(2), 24, IntReg(1));					//ld %r2, 24(%r1)
    EmitLoadDoubleWord(IntReg(0), 16, IntReg(1));					//ld %r0, 16(%r1)
    Emit32((DWORD)((31 << 26) | (R0 << 21) | (256 << 11) | (467 << 1)));		//mtlr %r0
    Emit32((DWORD)(0x4e800020));							//blr
}

void StubLinkerCPU::EmitCallLabel(CodeLabel *target, BOOL fTailCall, BOOL fIndirect)
{
    PPC64LECall::VariationCodes variationCode;
    if (!fIndirect)
    {
        if (fTailCall)
        {
            variationCode = PPC64LECall::DIRECT_TAILCALL;
        }
        else
        {
            variationCode = PPC64LECall::DIRECT_NON_TAILCALL;
        }
    }
    else
    {
        if (fTailCall)
        {
            variationCode = PPC64LECall::INDIRECT_TAILCALL;
        }
        else
        {
            variationCode = PPC64LECall::INDIRECT_NON_TAILCALL;
        }
    }
    EmitLabelRef(target, reinterpret_cast<PPC64LECall&>(gPPC64LECall), (UINT)variationCode);
    //_ASSERTE(!"NYI POWERPC64 EmitCallLabel");
}

VOID StubLinkerCPU::EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg)
{
    //_ASSERTE(!"NYI POWERPC64 EmitComputedInstantiatingMethodStub");
    STANDARD_VM_CONTRACT

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
	    _ASSERTE(!"TARGET_POWERPC64:NYI");
	}
	else
	{
	    EmitMoveRegister(IntReg(3 + dstRegIndex), IntReg(3 + srcRegIndex));
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
		EmitLoadDoubleWord(IntReg(3 + paramTypeArgIndex), 0, IntReg(THIS_kREG));
		LOG((LF_CORDB, LL_INFO100, "SL::ECIMS: param/unbox reg:%d\n", 3 + paramTypeArgIndex));
	    }
	}
	else
	{
	    EmitLoadImmediate(IntReg(3 + paramTypeArgIndex), (UINT_PTR)extraArg);
	    LOG((LF_CORDB, LL_INFO100, "SL::ECIMS: param/extra reg:%d\n", 3 + paramTypeArgIndex));
	}
    }

    if (extraArg == NULL)
    {
        // Unboxing stub case
        // Skip over the MethodTable* to find the address of the unboxed value type.
	EmitAddImm(IntReg(THIS_kREG), IntReg(THIS_kREG), sizeof(MethodDesc*));
    }

    PCODE multiCallableAddr = pSharedMD->TryGetMultiCallableAddrOfCode(CORINFO_ACCESS_PREFER_SLOT_OVER_TEMPORARY_ENTRYPOINT);
    // Use direct call if possible.
    if (multiCallableAddr != (PCODE)NULL)
    {
	EmitCallLabel(NewExternalCodeLabel((LPVOID)multiCallableAddr),TRUE, FALSE);
    }
    else
    {
	EmitCallLabel(NewExternalCodeLabel((LPVOID)pSharedMD->GetAddrOfSlot()),TRUE, TRUE);
    }
  
    SetTargetMethod(pSharedMD);
}

VOID StubLinkerCPU::EmitShuffleThunk(ShuffleEntry *pShuffleEntryArray)
{
    // On entry THIS_kREG (i.e. r3) holds the delegate instance. Look up the real target address stored in the MethodPtrAux
    // field and save it in r12. Tailcall to the target method after re-arranging the arguments
    // ld r12, offsetof(DelegateObject, _methodPtrAux)(THIS_kREG)
    EmitLoadDoubleWord(IntReg(12), DelegateObject::GetOffsetOfMethodPtrAux(), IntReg(THIS_kREG));

    // load the indirection cell into x11 used by ResolveWorkerAsmStub
    // addi %r11, %r3, DelegateObject::GetOffsetOfMethodPtrAux() 
    EmitAddImm(IntReg(11), IntReg(THIS_kREG), DelegateObject::GetOffsetOfMethodPtrAux());

    for (ShuffleEntry* pEntry = pShuffleEntryArray; pEntry->srcofs != ShuffleEntry::SENTINEL; pEntry++)
    {
	if (pEntry->srcofs == ShuffleEntry::HELPERREG)
        {
            _ASSERTE(!"POWERPC:NYI");
        }
        else if (pEntry->dstofs == ShuffleEntry::HELPERREG)
        {
            _ASSERTE(!"POWERPC:NYI");
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
            	    _ASSERTE(!"POWERPC NYI");
                }
                else
                {
	    	    EmitMoveRegister(IntReg(3 + dstRegIndex), IntReg(3 + srcRegIndex));
                }
	    }
	    else
	    {
            	_ASSERTE(!"POWERPC NYI");
	    }
	}
	else if (pEntry->dstofs & ShuffleEntry::REGMASK)
	{
            // Source on stack, destination in register
            _ASSERTE(!(pEntry->srcofs & ShuffleEntry::REGMASK));
            
	    int dstRegIndex = pEntry->dstofs & ShuffleEntry::OFSREGMASK;
	    int srcOffset = 32 + pEntry->srcofs * sizeof(void*); //need to check - vikas

	    if (pEntry->dstofs & ShuffleEntry::FPREGMASK)
	    {
            	_ASSERTE(!"POWERPC NYI");
	    }
	    else
	    {
    		EmitLoadDoubleWord(IntReg(3 + dstRegIndex), srcOffset, IntReg(1));
	    }
	}
	else
	{
            _ASSERTE(!"POWERPC:NYI");
	}
    }

    // Tailcall to target
    EmitBranchToCountRegister(IntReg(12), 20/*branch always*/, 0);
}

void StubLinkerCPU::EmitMovReg(IntReg R1, IntReg R2)
{
    _ASSERTE(!"NYI POWERPC64 EmitMovReg");
}

void StubLinkerCPU::EmitMovConstant(IntReg R1, int I2)
{
    _ASSERTE(!"NYI POWERPC64 EmitMovConstant");
}

//addi %r1, %r2, imm
void StubLinkerCPU::EmitAddImm(IntReg R1, IntReg R2, unsigned int Imm)
{
    Emit32((DWORD)((14 << 26) | (R1 << 21) | (R2 << 16) | Imm));
}

unsigned int StubLinkerCPU::GetSavedRegArgsOffset()
{
    _ASSERTE(!"NYI POWERPC64 GetSavedRegArgsOffset");
    return 0;
}

#endif // !DACCESS_COMPILE
