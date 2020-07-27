// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_X86)

#include "target.h"

const char*            Target::g_tgtCPUName  = "x86";
const Target::ArgOrder Target::g_tgtArgOrder = ARG_ORDER_L2R;

const regNumber intArgRegs [] = {REG_ECX, REG_EDX};
const regMaskTP intArgMasks[] = {RBM_ECX, RBM_EDX};
const regNumber fltArgRegs [] = {REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3};
const regMaskTP fltArgMasks[] = {RBM_XMM0, RBM_XMM1, RBM_XMM2, RBM_XMM3};

#endif // TARGET_X86
