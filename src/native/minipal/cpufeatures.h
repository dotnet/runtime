// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUFEATURES_H
#define HAVE_MINIPAL_CPUFEATURES_H

//
// Should match the constants defined in the compiler in HardwareIntrinsicHelpers.cs
//

#if defined(HOST_X86) || defined(HOST_AMD64)
#define XArchIntrinsicConstants_Aes (1LL << 0)
#define XArchIntrinsicConstants_Pclmulqdq (1LL << 1)
#define XArchIntrinsicConstants_Sse3 (1LL << 2)
#define XArchIntrinsicConstants_Ssse3 (1LL << 3)
#define XArchIntrinsicConstants_Sse41 (1LL << 4)
#define XArchIntrinsicConstants_Sse42 (1LL << 5)
#define XArchIntrinsicConstants_Popcnt (1LL << 6)
#define XArchIntrinsicConstants_Avx (1LL << 7)
#define XArchIntrinsicConstants_Fma (1LL << 8)
#define XArchIntrinsicConstants_Avx2 (1LL << 9)
#define XArchIntrinsicConstants_Bmi1 (1LL << 10)
#define XArchIntrinsicConstants_Bmi2 (1LL << 11)
#define XArchIntrinsicConstants_Lzcnt (1LL << 12)
#define XArchIntrinsicConstants_AvxVnni (1LL << 13)
#define XArchIntrinsicConstants_Movbe (1LL << 14)
#define XArchIntrinsicConstants_Avx512 (1LL << 15)
#define XArchIntrinsicConstants_Avx512Vbmi (1LL << 16)
#define XArchIntrinsicConstants_Serialize (1LL << 17)
#define XArchIntrinsicConstants_Avx10v1 (1LL << 18)
#define XArchIntrinsicConstants_Apx (1LL << 19)
#define XArchIntrinsicConstants_Vpclmulqdq (1LL << 20)
#define XArchIntrinsicConstants_Avx10v2 (1LL << 21)
#define XArchIntrinsicConstants_Gfni (1LL << 22)
#define XArchIntrinsicConstants_Avx512Bitalg (1LL << 23)
#define XArchIntrinsicConstants_Avx512Bf16 (1LL << 24)
#define XArchIntrinsicConstants_Avx512Fp16 (1LL << 25)
#define XArchIntrinsicConstants_Avx512Ifma (1LL << 26)
#define XArchIntrinsicConstants_Avx512Vbmi2 (1LL << 27)
#define XArchIntrinsicConstants_Avx512Vnni (1LL << 28)
#define XArchIntrinsicConstants_Avx512Vp2intersect (1LL << 29)
#define XArchIntrinsicConstants_Avx512Vpopcntdq (1LL << 30)
#define XArchIntrinsicConstants_AvxIfma (1LL << 31)
#define XArchIntrinsicConstants_F16c (1LL << 32)
#define XArchIntrinsicConstants_Sha (1LL << 33)
#define XArchIntrinsicConstants_Vaes (1LL << 34)
#define XArchIntrinsicConstants_WaitPkg (1LL << 35)
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
#define ARM64IntrinsicConstants_Aes (1LL << 0)
#define ARM64IntrinsicConstants_Crc32 (1LL << 1)
#define ARM64IntrinsicConstants_Dp (1LL << 2)
#define ARM64IntrinsicConstants_Rdm (1LL << 3)
#define ARM64IntrinsicConstants_Sha1 (1LL << 4)
#define ARM64IntrinsicConstants_Sha256 (1LL << 5)
#define ARM64IntrinsicConstants_Atomics (1LL << 6)
#define ARM64IntrinsicConstants_Rcpc (1LL << 7)
#define ARM64IntrinsicConstants_Rcpc2 (1LL << 8)
#define ARM64IntrinsicConstants_Sve (1LL << 9)
#define ARM64IntrinsicConstants_Sve2 (1LL << 10)

#include <assert.h>

// Bit position for the ARM64IntrinsicConstants_Atomics flags, to be used with tbz / tbnz instructions
#define ARM64_ATOMICS_FEATURE_FLAG_BIT 6
static_assert((1LL << ARM64_ATOMICS_FEATURE_FLAG_BIT) == ARM64IntrinsicConstants_Atomics, "ARM64_ATOMICS_FEATURE_FLAG_BIT must match with ARM64IntrinsicConstants_Atomics");

#endif // HOST_ARM64

#if defined(HOST_RISCV64)
#define RiscV64IntrinsicConstants_Zba (1LL << 0)
#define RiscV64IntrinsicConstants_Zbb (1LL << 1)
#endif // HOST_RISCV64

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

int64_t minipal_getcpufeatures(void);
bool minipal_detect_rosetta(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif
