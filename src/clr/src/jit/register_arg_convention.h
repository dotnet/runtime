// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __register_arg_convention__
#define __register_arg_convention__

class LclVarDsc;

struct InitVarDscInfo 
{
    LclVarDsc * varDsc;
    unsigned varNum;

    unsigned intRegArgNum;
    unsigned floatRegArgNum;
    unsigned maxIntRegArgNum;
    unsigned maxFloatRegArgNum;

    bool hasRetBuf;

#ifdef _TARGET_ARM_
    // Support back-filling of FP parameters. This is similar to code in gtMorphArgs() that
    // handles arguments.
    regMaskTP fltArgSkippedRegMask;
    bool anyFloatStackArgs;
#endif // _TARGET_ARM_

public:

    // set to initial values
    void Init(LclVarDsc *lvaTable, bool _hasRetBuf)
    {
        hasRetBuf         = _hasRetBuf;
        varDsc            = lvaTable;
        varNum            = 0;
        intRegArgNum      = 0;
        floatRegArgNum    = 0;
        maxIntRegArgNum   = MAX_REG_ARG;
        maxFloatRegArgNum = MAX_FLOAT_REG_ARG;

#ifdef _TARGET_ARM_
        fltArgSkippedRegMask = RBM_NONE;
        anyFloatStackArgs = false;
#endif // _TARGET_ARM_
    }

    // return ref to current register arg for this type
    unsigned& regArgNum(var_types type) 
    { 
        return varTypeIsFloating(type) ? floatRegArgNum : intRegArgNum; 
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

    // We are aligning the register to an ABI-required boundary, such as putting
    // double-precision floats in even-numbered registers, by skipping one register.
    // "requiredRegAlignment" is the amount to align to: 1 for no alignment (everything
    // is 1-aligned), 2 for "double" alignment.
    // Returns the number of registers skipped.
    unsigned alignReg(var_types type, unsigned requiredRegAlignment);

    // Return true if it is an enregisterable type and there is room.
    // Note that for "type", we only care if it is float or not. In particular,
    // "numRegs" must be "2" to allocate an ARM double-precision floating-point register.
    bool canEnreg(var_types type, unsigned numRegs = 1);

#ifdef _TARGET_ARM_

    void setAllRegArgUsed(var_types type)
    {
        regArgNum(type) = maxRegArgNum(type);
    }

    void setAnyFloatStackArgs()
    {
        anyFloatStackArgs = true;
    }

    bool existAnyFloatStackArgs()
    {
        return anyFloatStackArgs;
    }

#endif // _TARGET_ARM_

private:

    // return max register arg for this type
    unsigned maxRegArgNum(var_types type) 
    { 
        return varTypeIsFloating(type) ? maxFloatRegArgNum : maxIntRegArgNum; 
    }

    bool enoughAvailRegs(var_types type, unsigned numRegs = 1);

    void nextReg(var_types type, unsigned numRegs = 1)
    {
        regArgNum(type) = min(regArgNum(type) + numRegs, maxRegArgNum(type));
    }
};

#endif // __register_arg_convention__
