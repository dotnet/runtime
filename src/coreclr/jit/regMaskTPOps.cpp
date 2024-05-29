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


// ----------------------------------------------------------
//  AddRegNumForType: Adds `reg` to the mask.
//
void regMaskTP::AddRegNumInMask(regNumber reg)
{
    SingleTypeRegSet value = genSingleTypeRegMask(reg);
#ifdef HAS_MORE_THAN_64_REGISTERS
    int index = getRegisterTypeIndex(reg);
    _registers[index] |= encodeForRegisterIndex(index, value);
#else
    low |= value;
#endif
}

#ifdef TARGET_ARM
// ----------------------------------------------------------
//  AddRegNumForType: Adds `reg` to the mask. It is same as AddRegNumInMask(reg) except
//  that it takes `type` as an argument and adds `reg` to the mask for that type.
//
void regMaskTP::AddRegNumInMask(regNumber reg, var_types type)
{
    low |= genSingleTypeRegMask(reg, type);
}

// ----------------------------------------------------------
//  RemoveRegNumFromMask: Removes `reg` from the mask. It is same as RemoveRegNumFromMask(reg) except
//  that it takes `type` as an argument and adds `reg` to the mask for that type.
//
void regMaskTP::RemoveRegNumFromMask(regNumber reg, var_types type)
{
    low &= ~genSingleTypeRegMask(reg, type);
}

// ----------------------------------------------------------
//  IsRegNumInMask: Removes `reg` from the mask. It is same as IsRegNumInMask(reg) except
//  that it takes `type` as an argument and adds `reg` to the mask for that type.
//
bool regMaskTP::IsRegNumInMask(regNumber reg, var_types type) const
{
    return (low & genSingleTypeRegMask(reg, type)) != RBM_NONE;
}
#endif

// This is similar to AddRegNumInMask(reg, regType) for all platforms
// except Arm. For Arm, it calls getRegMask() instead of genRegMask()
// to create a mask that needs to be added.
void regMaskTP::AddRegNum(regNumber reg, var_types type)
{
#ifdef TARGET_ARM
    low |= getRegMask(reg, type);
#else
    AddRegNumInMask(reg);
#endif
}

//------------------------------------------------------------------------
// IsRegNumInMask: Checks if `reg` is in the mask
//
// Parameters:
//  reg - Register to check
//
bool regMaskTP::IsRegNumInMask(regNumber reg) const
{
    SingleTypeRegSet value = genSingleTypeRegMask(reg);
#ifdef HAS_MORE_THAN_64_REGISTERS
    int index = getRegisterTypeIndex(reg);
    return (_registers[index] & encodeForRegisterIndex(index, value)) != RBM_NONE;
#else
    return (low & value) != RBM_NONE;
#endif
}

// This is similar to IsRegNumInMask(reg, regType) for all platforms
// except Arm. For Arm, it calls getRegMask() instead of genRegMask()
// to create a mask that needs to be added.
bool regMaskTP::IsRegNumPresent(regNumber reg, var_types type) const
{
#ifdef TARGET_ARM
    return (low & getRegMask(reg, type)) != RBM_NONE;
#else
    return IsRegNumInMask(reg);
#endif
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
// RemoveRegNum: his is similar to RemoveRegNumFromMask(reg, regType) for all platforms
//      except Arm. For Arm, it calls getRegMask() instead of genRegMask()
//      to create a mask that needs to be added.
// Parameters:
//  reg - Register to remove
//
void regMaskTP::RemoveRegNum(regNumber reg, var_types type)
{
#ifdef TARGET_ARM
    low &= ~getRegMask(reg, type);
#else
    RemoveRegNumFromMask(reg);
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
