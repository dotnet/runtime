// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_X86)

#include "target.h"

const char*            Target::g_tgtCPUName           = "x86";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_L2R;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_ECX, REG_EDX};
const regMaskTP intArgMasks[] = {RBM_ECX, RBM_EDX};
// clang-format on

#endif // TARGET_X86
