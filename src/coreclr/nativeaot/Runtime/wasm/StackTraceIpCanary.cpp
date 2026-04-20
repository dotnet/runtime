// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommonTypes.h"
#include "CommonMacros.h"

// TODO-LLVM: define "RhpStackTraceIpCanary" here via inline asm. Right now it has
// to be defined by ILC due to https://github.com/llvm/llvm-project/issues/100733.
extern "C" int RhpStackTraceIpCanary;

FCIMPL0(int, RhpGetStackTraceIpCanary)
{
    return RhpStackTraceIpCanary;
}
FCIMPLEND
