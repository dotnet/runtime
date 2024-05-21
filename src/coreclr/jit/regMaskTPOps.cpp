// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "target.h"

#ifdef TARGET_ARM64
struct regMaskTP;

void regMaskTP::RemoveRegNumFromMask(regNumber reg)
{
    low &= ~genRegMask(reg);
}

bool regMaskTP::IsRegNumInMask(regNumber reg)
{
    return (low & genRegMask(reg)) != 0;
}
#endif // TARGET_ARM64
