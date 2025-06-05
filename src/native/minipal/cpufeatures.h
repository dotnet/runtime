// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUFEATURES_H
#define HAVE_MINIPAL_CPUFEATURES_H

//
// Should match the constants defined in the compiler in HardwareIntrinsicHelpers.cs
//

#if defined(HOST_X86) || defined(HOST_AMD64)
#define XArchIntrinsicConstants_Sse42 (1 << 0)
#define XArchIntrinsicConstants_Avx (1 << 1)
#define XArchIntrinsicConstants_Avx2 (1 << 2)
#define XArchIntrinsicConstants_Avx512 (1 << 3)

#define XArchIntrinsicConstants_Avx512v2 (1 << 4)
#define XArchIntrinsicConstants_Avx512v3 (1 << 5)
#define XArchIntrinsicConstants_Avx10v1 (1 << 6)
#define XArchIntrinsicConstants_Avx10v2 (1 << 7)
#define XArchIntrinsicConstants_Apx (1 << 8)

#define XArchIntrinsicConstants_Aes (1 << 9)
#define XArchIntrinsicConstants_Avx512Vp2intersect (1 << 10)
#define XArchIntrinsicConstants_AvxIfma (1 << 11)
#define XArchIntrinsicConstants_AvxVnni (1 << 12)
#define XArchIntrinsicConstants_Gfni (1 << 13)
#define XArchIntrinsicConstants_Sha (1 << 14)
#define XArchIntrinsicConstants_Vaes (1 << 15)
#define XArchIntrinsicConstants_WaitPkg (1 << 16)
#define XArchIntrinsicConstants_X86Serialize (1 << 17)
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

#include <assert.h>

// Bit position for the ARM64IntrinsicConstants_Atomics flags, to be used with tbz / tbnz instructions
#define ARM64_ATOMICS_FEATURE_FLAG_BIT 6
static_assert((1 << ARM64_ATOMICS_FEATURE_FLAG_BIT) == ARM64IntrinsicConstants_Atomics, "ARM64_ATOMICS_FEATURE_FLAG_BIT must match with ARM64IntrinsicConstants_Atomics");

#endif // HOST_ARM64

#if defined(HOST_RISCV64)
#define RiscV64IntrinsicConstants_Zba (1 << 0)
#define RiscV64IntrinsicConstants_Zbb (1 << 1)
#endif // HOST_RISCV64

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

int minipal_getcpufeatures(void);
bool minipal_detect_rosetta(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif
