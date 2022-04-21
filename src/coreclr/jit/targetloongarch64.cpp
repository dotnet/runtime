// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_LOONGARCH64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "loongarch64";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_A0, REG_A1, REG_A2, REG_A3, REG_A4, REG_A5, REG_A6, REG_A7};
const regMaskTP intArgMasks[] = {RBM_A0, RBM_A1, RBM_A2, RBM_A3, RBM_A4, RBM_A5, RBM_A6, RBM_A7};

const regNumber fltArgRegs [] = {REG_F0, REG_F1, REG_F2, REG_F3, REG_F4, REG_F5, REG_F6, REG_F7 };
const regMaskTP fltArgMasks[] = {RBM_F0, RBM_F1, RBM_F2, RBM_F3, RBM_F4, RBM_F5, RBM_F6, RBM_F7 };
// clang-format on

#endif // TARGET_LOONGARCH64
