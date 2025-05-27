// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_CPUFEATURES_H
#define HAVE_MINIPAL_CPUFEATURES_H

//
// Should match the constants defined in the compiler in HardwareIntrinsicHelpers.cs
//

#if defined(HOST_X86) || defined(HOST_AMD64)
const int64_t XArchIntrinsicConstants_Aes = (1LL << 0);
const int64_t XArchIntrinsicConstants_Pclmulqdq = (1LL << 1);
const int64_t XArchIntrinsicConstants_Sse3 = (1LL << 2);
const int64_t XArchIntrinsicConstants_Ssse3 = (1LL << 3);
const int64_t XArchIntrinsicConstants_Sse41 = (1LL << 4);
const int64_t XArchIntrinsicConstants_Sse42 = (1LL << 5);
const int64_t XArchIntrinsicConstants_Popcnt = (1LL << 6);
const int64_t XArchIntrinsicConstants_Avx = (1LL << 7);
const int64_t XArchIntrinsicConstants_Fma = (1LL << 8);
const int64_t XArchIntrinsicConstants_Avx2 = (1LL << 9);
const int64_t XArchIntrinsicConstants_Bmi1 = (1LL << 10);
const int64_t XArchIntrinsicConstants_Bmi2 = (1LL << 11);
const int64_t XArchIntrinsicConstants_Lzcnt = (1LL << 12);
const int64_t XArchIntrinsicConstants_AvxVnni = (1LL << 13);
const int64_t XArchIntrinsicConstants_Movbe = (1LL << 14);
const int64_t XArchIntrinsicConstants_Avx512 = (1LL << 15);
const int64_t XArchIntrinsicConstants_Avx512Vbmi = (1LL << 16);
const int64_t XArchIntrinsicConstants_Serialize = (1LL << 17);
const int64_t XArchIntrinsicConstants_Avx10v1 = (1LL << 18);
const int64_t XArchIntrinsicConstants_Apx = (1LL << 19);
const int64_t XArchIntrinsicConstants_Vpclmulqdq = (1LL << 20);
const int64_t XArchIntrinsicConstants_Avx10v2 = (1LL << 21);
const int64_t XArchIntrinsicConstants_Gfni = (1LL << 22);
const int64_t XArchIntrinsicConstants_Avx512Bitalg = (1LL << 23);
const int64_t XArchIntrinsicConstants_Avx512Bf16 = (1LL << 24);
const int64_t XArchIntrinsicConstants_Avx512Fp16 = (1LL << 25);
const int64_t XArchIntrinsicConstants_Avx512Ifma = (1LL << 26);
const int64_t XArchIntrinsicConstants_Avx512Vbmi2 = (1LL << 27);
const int64_t XArchIntrinsicConstants_Avx512Vnni = (1LL << 28);
const int64_t XArchIntrinsicConstants_Avx512Vp2intersect = (1LL << 29);
const int64_t XArchIntrinsicConstants_Avx512Vpopcntdq = (1LL << 30);
const int64_t XArchIntrinsicConstants_AvxIfma = (1LL << 31);
const int64_t XArchIntrinsicConstants_F16c = (1LL << 32);
const int64_t XArchIntrinsicConstants_Sha = (1LL << 33);
const int64_t XArchIntrinsicConstants_Vaes = (1LL << 34);
const int64_t XArchIntrinsicConstants_WaitPkg = (1LL << 35);
#endif // HOST_X86 || HOST_AMD64

#if defined(HOST_ARM64)
const int64_t ARM64IntrinsicConstants_Aes = (1LL << 0),
const int64_t ARM64IntrinsicConstants_Crc32 = (1LL << 1),
const int64_t ARM64IntrinsicConstants_Dp = (1LL << 2),
const int64_t ARM64IntrinsicConstants_Rdm = (1LL << 3),
const int64_t ARM64IntrinsicConstants_Sha1 = (1LL << 4),
const int64_t ARM64IntrinsicConstants_Sha256 = (1LL << 5),
const int64_t ARM64IntrinsicConstants_Atomics = (1LL << 6),
const int64_t ARM64IntrinsicConstants_Rcpc = (1LL << 7),
const int64_t ARM64IntrinsicConstants_Rcpc2 = (1LL << 8),
const int64_t ARM64IntrinsicConstants_Sve = (1LL << 9),
const int64_t ARM64IntrinsicConstants_Sve2 = (1LL << 10),

#include <assert.h>

// Bit position for the ARM64IntrinsicConstants_Atomics flags, to be used with tbz / tbnz instructions
#define ARM64_ATOMICS_FEATURE_FLAG_BIT 6
static_assert((1 << ARM64_ATOMICS_FEATURE_FLAG_BIT) == ARM64IntrinsicConstants_Atomics, "ARM64_ATOMICS_FEATURE_FLAG_BIT must match with ARM64IntrinsicConstants_Atomics");

#endif // HOST_ARM64

#if defined(HOST_RISCV64)
const int64_t RiscV64IntrinsicConstants_Zba = (1LL << 0);
const int64_t RiscV64IntrinsicConstants_Zbb = (1LL << 1);
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
