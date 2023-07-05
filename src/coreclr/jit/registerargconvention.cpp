// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "registerargconvention.h"

unsigned InitVarDscInfo::allocRegArg(var_types type, unsigned numRegs /* = 1 */)
{
    assert(numRegs > 0);

    unsigned resultArgNum = regArgNum(type);
    bool     isBackFilled = false;

#ifdef TARGET_ARM
    // Check for back-filling
    if (varTypeIsFloating(type) &&          // We only back-fill the float registers
        !anyFloatStackArgs &&               // Is it legal to back-fill? (We haven't put any FP args on the stack yet)
        (numRegs == 1) &&                   // Is there a possibility we could back-fill?
        (fltArgSkippedRegMask != RBM_NONE)) // Is there an available back-fill slot?
    {
        // We will never back-fill something greater than a single register
        // (TYP_FLOAT, or TYP_STRUCT HFA with a single float). This is because
        // we don't have any types that require > 2 register alignment, so we
        // can't create a > 1 register alignment hole to back-fill.

        // Back-fill the register
        regMaskTP backFillBitMask = genFindLowestBit(fltArgSkippedRegMask);
        fltArgSkippedRegMask &= ~backFillBitMask; // Remove the back-filled register(s) from the skipped mask
        resultArgNum = genMapFloatRegNumToRegArgNum(genRegNumFromMask(backFillBitMask));
        assert(resultArgNum < MAX_FLOAT_REG_ARG);
        isBackFilled = true;
    }
#endif // TARGET_ARM

    if (!isBackFilled)
    {
#if defined(TARGET_AMD64) && !defined(UNIX_AMD64_ABI)
        // For System V the reg type counters should be independent.
        nextReg(TYP_INT, numRegs);
        nextReg(TYP_FLOAT, numRegs);
#else
        // We didn't back-fill a register (on ARM), so skip the number of registers that we allocated.
        nextReg(type, numRegs);
#endif
    }

    return resultArgNum;
}

bool InitVarDscInfo::enoughAvailRegs(var_types type, unsigned numRegs /* = 1 */)
{
    assert(numRegs > 0);

    unsigned backFillCount = 0;

#ifdef TARGET_ARM
    // Check for back-filling
    if (varTypeIsFloating(type) &&          // We only back-fill the float registers
        !anyFloatStackArgs &&               // Is it legal to back-fill? (We haven't put any FP args on the stack yet)
        (numRegs == 1) &&                   // Is there a possibility we could back-fill?
        (fltArgSkippedRegMask != RBM_NONE)) // Is there an available back-fill slot?
    {
        backFillCount = 1;
    }
#endif // TARGET_ARM

    return regArgNum(type) + numRegs - backFillCount <= maxRegArgNum(type);
}

#ifdef TARGET_ARM
unsigned InitVarDscInfo::alignReg(var_types type, unsigned requiredRegAlignment)
{
    assert(requiredRegAlignment > 0);
    if (requiredRegAlignment == 1)
    {
        return 0; // Everything is always "1" aligned
    }

    assert(requiredRegAlignment == 2); // we don't expect anything else right now

    int alignMask = regArgNum(type) & (requiredRegAlignment - 1);
    if (alignMask == 0)
    {
        return 0; // We're already aligned
    }

    unsigned cAlignSkipped = requiredRegAlignment - alignMask;
    assert(cAlignSkipped == 1); // Alignment is currently only 1 or 2, so misalignment can only be 1.

    if (varTypeIsFloating(type))
    {
        fltArgSkippedRegMask |= genMapFloatRegArgNumToRegMask(floatRegArgNum);
    }

    assert(regArgNum(type) + cAlignSkipped <= maxRegArgNum(type)); // if equal, then we aligned the last slot, and the
                                                                   // arg can't be enregistered
    regArgNum(type) += cAlignSkipped;

    return cAlignSkipped;
}
#endif // TARGET_ARM

bool InitVarDscInfo::canEnreg(var_types type, unsigned numRegs /* = 1 */)
{
    if (!isRegParamType(type))
    {
        return false;
    }

    if (!enoughAvailRegs(type, numRegs))
    {
        return false;
    }

    return true;
}
