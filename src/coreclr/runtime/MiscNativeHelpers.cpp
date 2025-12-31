// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifndef NATIVEAOT
// VM-specific includes
// (none needed for this file)
#else
// NativeAOT-specific includes
#include "CommonMacros.h"
#endif

#include "MiscNativeHelpers.h"
#include <minipal/cpuid.h>

#if defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE X86Base_CpuId(int cpuInfo[4], int functionId, int subFunctionId)
{
#ifndef NATIVEAOT
    QCALL_CONTRACT;

    BEGIN_QCALL;
#endif

    __cpuidex(cpuInfo, functionId, subFunctionId);

#ifndef NATIVEAOT
    END_QCALL;
#endif
}

#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
