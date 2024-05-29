// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "target.h"

struct regMaskTP;


//------------------------------------------------------------------------
// encodeForRegisterIndex: Shifts the high-32 bits of float to low-32 bits
//      and return. For gpr and predicate registers, it returns the same value.
//
// Parameters:
//  index - Register type index
//  value - value to encode
//
/* static */ RegSet32 regMaskTP::encodeForRegisterIndex(int index, regMaskSmall value)
{
    int shiftAmount = 32 * (index == 1);
    return (RegSet32)(value >> shiftAmount);
}

//------------------------------------------------------------------------
// decodeForRegisterIndex: Shifts the low-32 bits of float to high-32 bits
//      and return. For gpr and predicate registers, it returns the same value.
//
// Parameters:
//  index - Register type index
//  value - value to encode
//
/* static */ regMaskSmall regMaskTP::decodeForRegisterIndex(int index, RegSet32 value)
{
    int shiftAmount = 32 * (index == 1);
    return ((regMaskSmall)value << shiftAmount);
}


//------------------------------------------------------------------------
// RemoveRegNumFromMask: Removes `reg` from the mask
//
// Parameters:
//  reg - Register to remove
//
void regMaskTP::RemoveRegNumFromMask(regNumber reg)
{
    SingleTypeRegSet value = genSingleTypeRegMask(reg);
#ifdef HAS_MORE_THAN_64_REGISTERS
    int index = getRegisterTypeIndex(reg);
    _registers[index] &= ~encodeForRegisterIndex(index, value);
#else
    low &= ~value;
#endif
}

//------------------------------------------------------------------------
// IsRegNumInMask: Checks if `reg` is in the mask
//
// Parameters:
//  reg - Register to check
//
bool regMaskTP::IsRegNumInMask(regNumber reg)
{
    SingleTypeRegSet value = genSingleTypeRegMask(reg);
#ifdef HAS_MORE_THAN_64_REGISTERS
    int index = getRegisterTypeIndex(reg);
    return (_registers[index] & encodeForRegisterIndex(index, value)) != RBM_NONE;
#else
    return (low & value) != RBM_NONE;
#endif
}

/* static */ int regMaskTP::getRegisterTypeIndex(regNumber reg)
{
    static const BYTE _registerTypeIndex[] = {
#ifdef TARGET_ARM64
#define REGDEF(name, rnum, mask, xname, wname, regTypeTag) regTypeTag,
#else
#define REGDEF(name, rnum, mask, sname, regTypeTag) regTypeTag,
#endif
#include "register.h"
    };

    return _registerTypeIndex[reg];
}
