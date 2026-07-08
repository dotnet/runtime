// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUFEATURES_H
#define HAVE_MINIPAL_CPUFEATURES_H

#include <stdint.h>

//
// Should match the constants defined in the compiler in HardwareIntrinsicHelpers.cs
//

// Reserve the last bit to indicate an invalid query, such as if a baseline ISA isn't supported
#define IntrinsicConstants_Invalid (1 << 31)

#if defined(HOST_X86) || defined(HOST_AMD64)
#define XArchIntrinsicConstants_Avx (1 << 0)
#define XArchIntrinsicConstants_Avx2 (1 << 1)
#define XArchIntrinsicConstants_Avx512 (1 << 2)
#define XArchIntrinsicConstants_Avx512v2 (1 << 3)
#define XArchIntrinsicConstants_Avx512v3 (1 << 4)
#define XArchIntrinsicConstants_Avx10v1 (1 << 5)
#define XArchIntrinsicConstants_Avx10v2 (1 << 6)
#define XArchIntrinsicConstants_Apx (1 << 7)
#define XArchIntrinsicConstants_Aes (1 << 8)
#define XArchIntrinsicConstants_Avx512Vp2intersect (1 << 9)
#define XArchIntrinsicConstants_AvxIfma (1 << 10)
#define XArchIntrinsicConstants_AvxVnni (1 << 11)
#define XArchIntrinsicConstants_AvxVnniInt (1 << 12)
#define XArchIntrinsicConstants_Gfni (1 << 13)
#define XArchIntrinsicConstants_Sha (1 << 14)
#define XArchIntrinsicConstants_Vaes (1 << 15)
#define XArchIntrinsicConstants_WaitPkg (1 << 16)
#define XArchIntrinsicConstants_X86Serialize (1 << 17)
#define XArchIntrinsicConstants_Avx512Bmm (1 << 18)
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
#define ARM64IntrinsicConstants_Aes (1 << 0)
#define ARM64IntrinsicConstants_Crc32 (1 << 1)
#define ARM64IntrinsicConstants_Dp (1 << 2)
#define ARM64IntrinsicConstants_Rdm (1 << 3)
#define ARM64IntrinsicConstants_Sha1 (1 << 4)
#define ARM64IntrinsicConstants_Sha256 (1 << 5)
#define ARM64IntrinsicConstants_Atomics (1 << 6)
#define ARM64IntrinsicConstants_Rcpc (1 << 7)
#define ARM64IntrinsicConstants_Rcpc2 (1 << 8)
#define ARM64IntrinsicConstants_Sve (1 << 9)
#define ARM64IntrinsicConstants_Sve2 (1 << 10)
#define ARM64IntrinsicConstants_Sha3 (1 << 11)
#define ARM64IntrinsicConstants_Sm4 (1 << 12)
#define ARM64IntrinsicConstants_SveAes (1 << 13)
#define ARM64IntrinsicConstants_SveSha3 (1 << 14)
#define ARM64IntrinsicConstants_SveSm4 (1 << 15)
// Runtime-only feature (not a JIT hardware intrinsic ISA). Indicates FEAT_WFxT (WFET/WFIT) is
// available together with FEAT_ECV (required for the self-synchronized CNTVCTSS_EL0 counter read).
#define ARM64IntrinsicConstants_Wfxt (1 << 16)

#include <assert.h>

// Bit position for the ARM64IntrinsicConstants_Atomics flags, to be used with tbz / tbnz instructions
#define ARM64_ATOMICS_FEATURE_FLAG_BIT 6
static_assert((1 << ARM64_ATOMICS_FEATURE_FLAG_BIT) == ARM64IntrinsicConstants_Atomics, "ARM64_ATOMICS_FEATURE_FLAG_BIT must match with ARM64IntrinsicConstants_Atomics");
#endif // HOST_ARM64

#if defined(HOST_RISCV64)
#define RiscV64IntrinsicConstants_Zba (1 << 0)
#define RiscV64IntrinsicConstants_Zbb (1 << 1)
#define RiscV64IntrinsicConstants_Zbs (1 << 2)
#endif // HOST_RISCV64

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

int minipal_getcpufeatures(void);
bool minipal_detect_rosetta(void);

#if defined(HOST_ARM64) && !defined(HOST_WINDOWS)
// Non-zero when the runtime has opted into using FEAT_WFxT (WFET) for low-power spin-waits.
// Set once during startup; read on the spin-wait hot path.
extern int g_minipalWfetSpinWaitEnabled;

// Low-power wait for approximately 'ns' nanoseconds using the WFET instruction. The caller must
// ensure ARM64IntrinsicConstants_Wfxt is present (FEAT_WFxT + FEAT_ECV).
void minipal_wfet_wait_ns(uint64_t ns);
#endif // HOST_ARM64 && !HOST_WINDOWS

#ifdef __cplusplus
}
#endif // __cplusplus

#endif
