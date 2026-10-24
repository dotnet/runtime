// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// The interpreter has no register file, so no register number is ever valid.

bool genIsValidReg(regNumber reg)
{
    return false;
}

bool genIsValidIntReg(regNumber reg)
{
    return false;
}

bool genIsValidIntOrFakeReg(regNumber reg)
{
    return false;
}

bool genIsValidFloatReg(regNumber reg)
{
    return false;
}

const char* getRegName(regNumber reg)
{
    return (reg == REG_STK) ? "STK" : "NA";
}
