// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef STUBLINKERS390X_H_
#define STUBLINKERS390X_H_

//----------------------------------------------------------------------
// StubLinker with extensions for generating S390X code.
//----------------------------------------------------------------------

#define INSTRFMT_K64SMALL
#define INSTRFMT_K64
#include "stublink.h"

struct IntReg
{
    int reg;
    IntReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 16);
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

struct CondMask
{
    int cond;
    CondMask(int cond):cond(cond)
    {
        _ASSERTE(0 <= cond && cond < 16);
    }

    operator int() { return cond; }
};


class StubLinkerCPU : public StubLinker
{
private:
    void EmitBranchOnConditionRegister(CondMask M1, IntReg R2);
    void EmitBranchRegister(IntReg R2);
    void EmitLoadHalfwordImmediate(IntReg R1, int I2);
    void EmitLoadLogicalImmediateLow(IntReg R1, DWORD I2);
    void EmitLoadLogicalImmediateHigh(IntReg R1, DWORD I2);
    void EmitInsertImmediateLow(IntReg R1, DWORD I2);
    void EmitInsertImmediateHigh(IntReg R1, DWORD I2);
    void EmitLoadAddress(IntReg R1, int D2, IntReg X2, IntReg B2);
    void EmitLoadAddress(IntReg R1, int D2, IntReg B2);
    void EmitLoad(IntReg R1, int D2, IntReg X2, IntReg B2);
    void EmitLoad(IntReg R1, int D2, IntReg B2);
    void EmitStore(IntReg R1, int D2, IntReg X2, IntReg B2);
    void EmitStore(IntReg R1, int D2, IntReg B2);
    void EmitStoreFloat(VecReg R1, int D2, IntReg X2, IntReg B2);
    void EmitStoreFloat(VecReg R1, int D2, IntReg B2);
    void EmitLoadMultiple(IntReg R1, IntReg R3, int D2, IntReg B2);
    void EmitStoreMultiple(IntReg R1, IntReg R3, int D2, IntReg B2);

public:
    static void Init();

    void EmitLoadRegister(IntReg target, IntReg source);
    void EmitLoadImmediate(IntReg target, UINT64 constant);

    void EmitCallLabel(CodeLabel *target);

    void EmitSaveIncomingArguments(unsigned int cIntRegArgs, unsigned int cFloatRegArgs);

    void EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg);
    void EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);
};

#endif  // STUBLINKERS390X_H_
