// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef NATIVEAOT
#include "CommonMacros.h"
#endif

#include "MiscNativeHelpers.h"
#include <minipal/cpuid.h>

#if defined(TARGET_X86) || defined(TARGET_AMD64)
extern "C" void QCALLTYPE X86Base_CpuId(int cpuInfo[4], int functionId, int subFunctionId)
{
    __cpuidex(cpuInfo, functionId, subFunctionId);
}
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
