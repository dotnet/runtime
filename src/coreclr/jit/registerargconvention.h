// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __register_arg_convention__
#define __register_arg_convention__

class LclVarDsc;

struct InitVarDscInfo
{
    LclVarDsc* varDsc;
    unsigned   varNum;

    unsigned intRegArgNum;
    unsigned floatRegArgNum;
    unsigned maxIntRegArgNum;
    unsigned maxFloatRegArgNum;

    bool hasRetBufArg;

#ifdef TARGET_ARM
    // Support back-filling of FP parameters. This is similar to code in gtMorphArgs() that
    // handles arguments.
    regMaskFloat fltArgSkippedRegMask;
    bool         anyFloatStackArgs;
#endif // TARGET_ARM

#if defined(TARGET_ARM) || defined(TARGET_RISCV64)
    bool hasSplitParam;
#endif // TARGET_ARM || TARGET_RISCV64

#if FEATURE_FASTTAILCALL
    // It is used to calculate argument stack size information in byte
    unsigned stackArgSize;
#endif // FEATURE_FASTTAILCALL

public:
    // set to initial values
    void Init(LclVarDsc* lvaTable, bool _hasRetBufArg, unsigned _maxIntRegArgNum, unsigned _maxFloatRegArgNum)
    {
        hasRetBufArg      = _hasRetBufArg;
        varDsc            = &lvaTable[0]; // the first argument LclVar 0
        varNum            = 0;            // the first argument varNum 0
        intRegArgNum      = 0;
        floatRegArgNum    = 0;
        maxIntRegArgNum   = _maxIntRegArgNum;
        maxFloatRegArgNum = _maxFloatRegArgNum;

#ifdef TARGET_ARM
        fltArgSkippedRegMask = RBM_NONE;
        anyFloatStackArgs    = false;
#endif // TARGET_ARM

#if defined(TARGET_ARM) || defined(TARGET_RISCV64)
        hasSplitParam = false;
#endif // TARGET_ARM || TARGET_RISCV64

#if FEATURE_FASTTAILCALL
        stackArgSize = 0;
#endif // FEATURE_FASTTAILCALL
    }

    // return ref to current register arg for this type
    unsigned& regArgNum(var_types type)
    {
        return varTypeUsesFloatArgReg(type) ? floatRegArgNum : intRegArgNum;
    }

    // Allocate a set of contiguous argument registers. "type" is either an integer
    // type, indicating to use the integer registers, or a floating-point type, indicating
    // to use the floating-point registers. The actual type (TYP_FLOAT vs. TYP_DOUBLE) is
    // ignored. "numRegs" is the number of registers to allocate. Thus, on ARM, to allocate
    // a double-precision floating-point register, you need to pass numRegs=2. For an HFA,
    // pass the number of slots/registers needed.
    // This routine handles floating-point register back-filling on ARM.
    // Returns the first argument register of the allocated set.
    unsigned allocRegArg(var_types type, unsigned numRegs = 1);

#ifdef TARGET_ARM
    // We are aligning the register to an ABI-required boundary, such as putting
    // double-precision floats in even-numbered registers, by skipping one register.
    // "requiredRegAlignment" is the amount to align to: 1 for no alignment (everything
    // is 1-aligned), 2 for "double" alignment.
    // Returns the number of registers skipped.
    unsigned alignReg(var_types type, unsigned requiredRegAlignment);
#endif // TARGET_ARM

    // Return true if it is an enregisterable type and there is room.
    // Note that for "type", we only care if it is float or not. In particular,
    // "numRegs" must be "2" to allocate an ARM double-precision floating-point register.
    bool canEnreg(var_types type, unsigned numRegs = 1);

    // Set the fact that we have used up all remaining registers of 'type'
    //
    void setAllRegArgUsed(var_types type)
    {
        regArgNum(type) = maxRegArgNum(type);
    }

#ifdef TARGET_ARM

    void setAnyFloatStackArgs()
    {
        anyFloatStackArgs = true;
    }

    bool existAnyFloatStackArgs()
    {
        return anyFloatStackArgs;
    }

#endif // TARGET_ARM

private:
    // return max register arg for this type
    unsigned maxRegArgNum(var_types type)
    {
        return varTypeUsesFloatArgReg(type) ? maxFloatRegArgNum : maxIntRegArgNum;
    }

    bool enoughAvailRegs(var_types type, unsigned numRegs = 1);

    void nextReg(var_types type, unsigned numRegs = 1)
    {
        regArgNum(type) = min(regArgNum(type) + numRegs, maxRegArgNum(type));
    }
};

#endif // __register_arg_convention__
