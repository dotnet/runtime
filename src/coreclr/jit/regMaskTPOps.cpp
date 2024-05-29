// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "target.h"

struct regMaskTP;

//------------------------------------------------------------------------
// RemoveRegNumFromMask: Removes `reg` from the mask
//
// Parameters:
//  reg - Register to remove
//
void regMaskTP::RemoveRegNumFromMask(regNumber reg)
{
    low &= ~genSingleTypeRegMask(reg);
}

//------------------------------------------------------------------------
// IsRegNumInMask: Checks if `reg` is in the mask
//
// Parameters:
//  reg - Register to check
//
bool regMaskTP::IsRegNumInMask(regNumber reg)
{
    return (low & genRegMask(reg)) != 0;
}

/* static */ const int regMaskTP::getRegisterTypeIndex(regNumber reg)
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
