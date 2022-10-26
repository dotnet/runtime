// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_RISCV64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "riscv64";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_A0, REG_A1, REG_A2, REG_A3, REG_A4, REG_A5, REG_A6, REG_A7};
const regMaskTP intArgMasks[] = {RBM_A0, RBM_A1, RBM_A2, RBM_A3, RBM_A4, RBM_A5, RBM_A6, RBM_A7};

const regNumber fltArgRegs [] = {REG_FLTARG_0, REG_FLTARG_1, REG_FLTARG_2, REG_FLTARG_3, REG_FLTARG_4, REG_FLTARG_5, REG_FLTARG_6, REG_FLTARG_7 };
const regMaskTP fltArgMasks[] = {RBM_FLTARG_0, RBM_FLTARG_1, RBM_FLTARG_2, RBM_FLTARG_3, RBM_FLTARG_4, RBM_FLTARG_5, RBM_FLTARG_6, RBM_FLTARG_7 };
// clang-format on

#endif // TARGET_RISCV64
