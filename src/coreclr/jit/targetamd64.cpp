// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#if defined(TARGET_AMD64)

#include "target.h"

const char*            Target::g_tgtCPUName           = "x64";
const Target::ArgOrder Target::g_tgtArgOrder          = ARG_ORDER_R2L;
const Target::ArgOrder Target::g_tgtUnmanagedArgOrder = ARG_ORDER_R2L;

// clang-format off
#ifdef UNIX_AMD64_ABI
const regNumber intArgRegs [] = { REG_EDI, REG_ESI, REG_EDX, REG_ECX, REG_R8, REG_R9 };
const regMaskTP intArgMasks[] = { RBM_EDI, RBM_ESI, RBM_EDX, RBM_ECX, RBM_R8, RBM_R9 };
const regNumber fltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3, REG_XMM4, REG_XMM5, REG_XMM6, REG_XMM7 };
const regMaskTP fltArgMasks[] = { RBM_XMM0, RBM_XMM1, RBM_XMM2, RBM_XMM3, RBM_XMM4, RBM_XMM5, RBM_XMM6, RBM_XMM7 };
#else // !UNIX_AMD64_ABI
const regNumber intArgRegs [] = { REG_ECX, REG_EDX, REG_R8, REG_R9 };
const regMaskTP intArgMasks[] = { RBM_ECX, RBM_EDX, RBM_R8, RBM_R9 };
const regNumber fltArgRegs [] = { REG_XMM0, REG_XMM1, REG_XMM2, REG_XMM3 };
const regMaskTP fltArgMasks[] = { RBM_XMM0, RBM_XMM1, RBM_XMM2, RBM_XMM3 };
#endif // !UNIX_AMD64_ABI
// clang-format on

#endif // TARGET_AMD64
