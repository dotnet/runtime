// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
 
#ifndef STUBLINKERPPC64LE_H_
#define STUBLINKERPPC64LE_H_
 
//----------------------------------------------------------------------
// StubLinker with extensions for generating PPC64LE code.
//----------------------------------------------------------------------
 
#define INSTRFMT_K64SMALL
#define INSTRFMT_K64
#include "stublink.h"
 
struct IntReg
{
    int reg;
    IntReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 32);
    }
 
    operator int () { return reg; }
    operator int () const { return reg; }
    int operator == (IntReg other) { return reg == other.reg; }
    int operator != (IntReg other) { return reg != other.reg; }
};

struct VecReg
{
    int reg;
    VecReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 32);
    }

    operator int() { return reg; }
    int operator == (VecReg other) { return reg == other.reg; }
    int operator != (VecReg other) { return reg != other.reg; }
};

const IntReg RegSp  = IntReg(1);

class StubLinkerCPU : public StubLinker
{
private:
     //void EmitBranchOnConditionRegister(CondMask M1, IntReg R2);
     void EmitBranchRegister(IntReg R2);
     void EmitLoadHalfwordImmediate(IntReg R1, int I2);
     void EmitLoadLogicalImmediateLow(IntReg R1, DWORD I2);
     void EmitLoadLogicalImmediateHigh(IntReg R1, DWORD I2);
     void EmitInsertImmediateLow(IntReg R1, DWORD I2);
     void EmitInsertImmediateHigh(IntReg R1, DWORD I2);
     void EmitLoadAddress(IntReg R1, int D2, IntReg X2, IntReg B2);
     void EmitLoadAddress(IntReg R1, int D2, IntReg B2);
     void EmitStore(IntReg R1, int D2, IntReg X2, IntReg B2);
     void EmitStore(IntReg R1, int D2, IntReg B2);
     void EmitStoreFloat(VecReg R1, int D2, IntReg X2, IntReg B2);
     void EmitStoreFloat(VecReg R1, int D2, IntReg B2);
     void EmitLoadMultiple(IntReg R1, IntReg R3, int D2, IntReg B2);
     void EmitStoreMultiple(IntReg R1, IntReg R3, int D2, IntReg B2);

     //ppc64le:
     void EmitStoreDoubleWord(IntReg RS, IntReg RA, int DS);
     void EmitStoreFloatingPointDouble(VecReg RS, IntReg RA, int DS);
     void EmitLoadFloatingPointDouble(VecReg RS, IntReg RA, int DS);
     void EmitBranchToCountRegister(IntReg R12, int BO, int BI);
     void EmitStoreDoubleWordWithUpdate(IntReg R1, int D2, IntReg R2);
 public:
     static void Init();

     void EmitLoadDoubleWord(IntReg RS, int DS, IntReg RA); 
     void EmitMoveRegister(IntReg target, IntReg source);
 
     //void EmitCallLabel(CodeLabel *target);
     void EmitCallLabel(CodeLabel *target, BOOL fTailCall, BOOL fIndirect);

     void EmitSaveLR();
     void EmitSaveArguments(unsigned int cIntRegArgs, unsigned int cFloatRegArgs);
 
     void EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg);
     void EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);
     void EmitMovReg(IntReg R1, IntReg R2);
     void EmitMovConstant(IntReg R1, int I2);
     void EmitAddImm(IntReg R1, IntReg R2, unsigned int I3);
     unsigned int GetSavedRegArgsOffset();
     void EmitLoad(IntReg R1, int D2, IntReg X2, IntReg B2);
     void EmitLoad(IntReg R1, int D2, IntReg B2);
     // ppc64le
     void EmitMoveRegister(IntReg RS, IntReg RA, IntReg RB);
     void EmitLoadImmediate(IntReg target, UINT64 constant);
     void EmitRestoreArguments(IntReg R0, IntReg R1);
};

#endif  // STUBLINKERPPC64LE_H_
