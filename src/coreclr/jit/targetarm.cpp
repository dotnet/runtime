// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_ARM)

#include "target.h"

const char*            Target::g_tgtCPUName           = "arm";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
const regNumber intArgRegs [] = {REG_R0, REG_R1, REG_R2, REG_R3};
const regMaskTP intArgMasks[] = {RBM_R0, RBM_R1, RBM_R2, RBM_R3};

const regNumber fltArgRegs [] = {REG_F0, REG_F1, REG_F2, REG_F3, REG_F4, REG_F5, REG_F6, REG_F7, REG_F8, REG_F9, REG_F10, REG_F11, REG_F12, REG_F13, REG_F14, REG_F15 };
const regMaskTP fltArgMasks[] = {RBM_F0, RBM_F1, RBM_F2, RBM_F3, RBM_F4, RBM_F5, RBM_F6, RBM_F7, RBM_F8, RBM_F9, RBM_F10, RBM_F11, RBM_F12, RBM_F13, RBM_F14, RBM_F15 };
// clang-format on

static_assert_no_msg(RBM_ALLDOUBLE == (RBM_ALLDOUBLE_HIGH >> 1));

#endif // TARGET_ARM
