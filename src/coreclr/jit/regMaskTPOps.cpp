// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "target.h"

struct regMaskTP;

// ----------------------------------------------------------
//  AddRegNumForType: Adds `reg` to the mask.
//
void regMaskTP::AddRegNumInMask(regNumber reg)
{
    SingleTypeRegSet value = genSingleTypeRegMask(reg);
#ifdef HAS_MORE_THAN_64_REGISTERS
    if (reg < 64)
    {
        low |= value;
    }
    else
    {
        high |= value;
    }
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

//------------------------------------------------------------------------
// AddGprRegs: Adds gprRegs to the mask
//
// Parameters:
//  gprRegs  - Register to check
//
void regMaskTP::AddGprRegs(SingleTypeRegSet gprRegs)
{
    assert((gprRegs == RBM_NONE) || ((gprRegs & RBM_ALLINT) != RBM_NONE));
    low |= gprRegs;
}

//------------------------------------------------------------------------
// AddRegNum: This is similar to AddRegNumInMask(reg, regType) for all platforms
//      except Arm. For Arm, it calls getSingleTypeRegMask() instead of genSingleTypeRegMask()
//      to create a mask that needs to be added.
//
// Parameters:
//  reg  - Register to check
//  type - type of register
//
void regMaskTP::AddRegNum(regNumber reg, var_types type)
{
#ifdef TARGET_ARM
    low |= getSingleTypeRegMask(reg, type);
#else
    AddRegNumInMask(reg);
#endif
}

//------------------------------------------------------------------------
// AddRegsetForType: Add regs of `type` in mask.
//
// Parameters:
//  regsToAdd  - Register to check
//  type       - type of register
//
void regMaskTP::AddRegsetForType(SingleTypeRegSet regsToAdd, var_types type)
{
#ifdef HAS_MORE_THAN_64_REGISTERS
    if (!varTypeIsMask(type))
    {
        low |= regsToAdd;
    }
    else
    {
        high |= regsToAdd;
    }
#else
    low |= regsToAdd;
#endif
}

//------------------------------------------------------------------------
// GetRegSetForType: Get regset for given `type`
//
// Parameters:
//  type       - type of register
//
//  Return: The register set of given type
//
SingleTypeRegSet regMaskTP::GetRegSetForType(var_types type) const
{
#ifdef HAS_MORE_THAN_64_REGISTERS
    if (!varTypeIsMask(type))
    {
        return low;
    }
    else
    {
        return high;
    }
#else
    return low;
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
    if (reg < 64)
    {
        return (low & value) != RBM_NONE;
    }
    else
    {
        return (high & value) != RBM_NONE;
    }
#else
    return (low & value) != RBM_NONE;
#endif
}

// This is similar to IsRegNumInMask(reg, regType) for all platforms
// except Arm. For Arm, it calls getSingleTypeRegMask() instead of genSingleTypeRegMask()
// to create a mask that needs to be added.
bool regMaskTP::IsRegNumPresent(regNumber reg, var_types type) const
{
#ifdef TARGET_ARM
    return (low & getSingleTypeRegMask(reg, type)) != RBM_NONE;
#else
    return IsRegNumInMask(reg);
#endif
}

// RemoveRegNumFromMask: Removes `reg` from the mask
//
// Parameters:
//  reg - Register to remove
//
void regMaskTP::RemoveRegNumFromMask(regNumber reg)
{
    SingleTypeRegSet value = genSingleTypeRegMask(reg);
#ifdef HAS_MORE_THAN_64_REGISTERS
    if (reg < 64)
    {
        low &= ~value;
    }
    else
    {
        high &= ~value;
    }
#else
    low &= ~value;
#endif
}

//------------------------------------------------------------------------
// RemoveRegNum: his is similar to RemoveRegNumFromMask(reg, regType) for all platforms
//      except Arm. For Arm, it calls getSingleTypeRegMask() instead of genSingleTypeRegMask()
//      to create a mask that needs to be added.
// Parameters:
//  reg - Register to remove
//
void regMaskTP::RemoveRegNum(regNumber reg, var_types type)
{
#ifdef TARGET_ARM
    low &= ~getSingleTypeRegMask(reg, type);
#else
    RemoveRegNumFromMask(reg);
#endif
}

void regMaskTP::RemoveRegsetForType(SingleTypeRegSet regsToRemove, var_types type)
{
#ifdef HAS_MORE_THAN_64_REGISTERS
    if (!varTypeIsMask(type))
    {
        low &= ~regsToRemove;
    }
    else
    {
        high &= ~regsToRemove;
    }
#else
    low &= ~regsToRemove;
#endif
}
