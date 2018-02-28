// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(MISC);

#if defined(_ARM64_)
#define _TARGET_ARM64_
#endif

#include "../../../inc/corjitflags.h"

#if HAVE_AUXV_HWCAP_H
#include <sys/auxv.h>
#include <asm/hwcap.h>
#endif

PALIMPORT
VOID
PALAPI
PAL_GetJitCpuCapabilityFlags(CORJIT_FLAGS *flags)
{
    _ASSERTE(flags);

#if defined(_ARM64_)
#if HAVE_AUXV_HWCAP_H
    unsigned long hwCap = getauxval(AT_HWCAP);

    CORJIT_FLAGS &CPUCompileFlags = *flags;
// HWCAP_* flags are introduced by ARM into the Linux kernel as new extensions are published.
// For a given kernel, some of these flags may not be present yet.
// Use ifdef for each to allow for compilation with any vintage kernel.
// From a single binary distribution perspective, compiling with latest kernel asm/hwcap.h should
// include all published flags.  Given flags are merged to kernel and published before silicon is
// available, using the latest kernel for release should be sufficient.
#ifdef HWCAP_AES
    if (hwCap & HWCAP_AES)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_AES);
#endif
#ifdef HWCAP_ATOMICS
    if (hwCap & HWCAP_ATOMICS)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_ATOMICS);
#endif
#ifdef HWCAP_CRC32
    if (hwCap & HWCAP_CRC32)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_CRC32);
#endif
#ifdef HWCAP_DCPOP
    if (hwCap & HWCAP_DCPOP)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_DCPOP);
#endif
#ifdef HWCAP_ASIMDDP
    if (hwCap & HWCAP_ASIMDDP)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_DP);
#endif
#ifdef HWCAP_FCMA
    if (hwCap & HWCAP_FCMA)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FCMA);
#endif
#ifdef HWCAP_FP
    if (hwCap & HWCAP_FP)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP);
#endif
#ifdef HWCAP_FPHP
    if (hwCap & HWCAP_FPHP)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP16);
#endif
#ifdef HWCAP_JSCVT
    if (hwCap & HWCAP_JSCVT)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_JSCVT);
#endif
#ifdef HWCAP_LRCPC
    if (hwCap & HWCAP_LRCPC)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_LRCPC);
#endif
#ifdef HWCAP_PMULL
    if (hwCap & HWCAP_PMULL)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_PMULL);
#endif
#ifdef HWCAP_SHA1
    if (hwCap & HWCAP_SHA1)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA1);
#endif
#ifdef HWCAP_SHA2
    if (hwCap & HWCAP_SHA2)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA256);
#endif
#ifdef HWCAP_SHA512
    if (hwCap & HWCAP_SHA512)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA512);
#endif
#ifdef HWCAP_SHA3
    if (hwCap & HWCAP_SHA3)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SHA3);
#endif
#ifdef HWCAP_ASIMD
    if (hwCap & HWCAP_ASIMD)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SIMD);
#endif
#ifdef HWCAP_ASIMDRDM
    if (hwCap & HWCAP_ASIMDRDM)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SIMD_V81);
#endif
#ifdef HWCAP_ASIMDHP
    if (hwCap & HWCAP_ASIMDHP)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SIMD_FP16);
#endif
#ifdef HWCAP_SM3
    if (hwCap & HWCAP_SM3)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SM3);
#endif
#ifdef HWCAP_SM4
    if (hwCap & HWCAP_SM4)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SM4);
#endif
#ifdef HWCAP_SVE
    if (hwCap & HWCAP_SVE)
        CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SVE);
#endif
#else // !HAVE_AUXV_HWCAP_H
    // CoreCLR SIMD and FP support is included in ARM64 baseline
    // On exceptional basis platforms may leave out support, but CoreCLR does not 
    // yet support such platforms
    // Set baseline flags if OS has not exposed mechanism for us to determine CPU capabilities
    CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_SIMD);
    CPUCompileFlags.Set(CORJIT_FLAGS::CORJIT_FLAG_HAS_ARM64_FP);
#endif // HAVE_AUXV_HWCAP_H
#endif // defined(_ARM64_)
}
